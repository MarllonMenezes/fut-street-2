using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglIndexedDrawSourceRow(
    string Archive, string Object, int Section, int Layer, uint Primitive, int DrawCommand,
    uint? CountParameterCursor, uint? IndexParameterCursor, uint? CountOffset, uint? IndexOffset,
    uint? IndexCount, int IndexBytes, uint? MinimumIndex, uint? MaximumIndex, string IndexSha256,
    int BufferBaseCount, int RuntimeSelectedBuffers, uint? MinimumBaseElements, string BaseRangeStatus,
    string TarReference, string GeoReference, string ObjectSha256, string Status, string Issue);

/// <summary>
/// Joins the preceding count (2), index pointer (14), and conditional indexed
/// submission (26) within one serialized descriptor. No state is inherited
/// across descriptors. XBE instruction contracts are validated separately.
/// A buffer base/range check does not select its dynamic copy or an attribute.
/// </summary>
public static class EaglIndexedDrawSourceAudit
{
    public const string Verified = "verified-local-indexed-draw-source";
    public const string Unresolved = "unresolved-indexed-draw-source";
    public const string Rejected = "rejected-indexed-draw-source";
    public const string Status = "verified-bounded-indexed-draw-source-inventory";

    public static IReadOnlyList<EaglIndexedDrawSourceRow> Inspect(EaglRawModelObject obj)
    {
        var result = new List<EaglIndexedDrawSourceRow>();
        var parameters = EaglRenderParameterAudit.Inspect(obj);
        var buffers = EaglVertexBufferReader.Inspect(obj.Data, obj.Elf);
        var objectHash = Convert.ToHexString(SHA256.HashData(obj.Data));
        foreach (var group in parameters.GroupBy(r => (r.Section, r.Layer, r.Primitive)))
        foreach (var draw in group.Where(r => r.CommandId == 26))
        {
            var before = group.Where(r => r.CommandIndex < draw.CommandIndex).ToArray();
            var count = before.LastOrDefault(r => r.CommandId == 2);
            var indices = before.LastOrDefault(r => r.CommandId == 14);
            var tar = before.LastOrDefault(r => r.CommandId == 15)?.Relocations ?? "";
            var geo = before.LastOrDefault(r => r.CommandId == 16)?.Relocations ?? "";
            var fetches = before.Where(r => r.CommandId == 9).GroupBy(r => r.Slot)
                .Select(g => g.Last()).ToArray();
            var bases = fetches.Select(f => buffers.SingleOrDefault(b => b.SymbolName == f.VertexBufferBaseSymbol &&
                b.SectionIndex == f.Section && (ulong)b.BufferRecordOffset + 20 == f.Word1)).ToArray();
            uint? minimumElements = bases.Length > 0 && bases.All(b => b is not null)
                ? bases.Min(b => b!.ElementCount) : null;
            uint? countValue = null, min = null, max = null;
            var bytes = 0;
            var hash = "";
            var issue = "";
            var status = Unresolved;
            var baseRange = "not-checked";
            if (before.Append(draw).Any(r => r.Status != EaglRenderParameterAudit.Status))
                issue = "invalid-parameter-layout";
            else if (draw.DrawCondition == 0)
                issue = "conditional-submission-disabled";
            else if (count is null || indices is null)
                issue = "count-or-index-pointer-not-present-in-this-descriptor";
            else if (count.AddressScope != "local-base-independent-of-runtime-selector" ||
                     indices.AddressScope != "local-base-independent-of-runtime-selector")
                issue = "external-reference-or-runtime-selector-unresolved";
            else if (count.Word1 is not uint countOffset || indices.Word1 is not uint indexOffset)
                issue = "parameter-pointer-missing";
            else if (!TrySlice(obj, group.Key.Section, countOffset, 4, out var countData) ||
                     HasRelocation(obj, group.Key.Section, countOffset, 4))
            {
                status = Rejected; issue = "count-target-outside-section-or-relocated";
            }
            else
            {
                countValue = BinaryPrimitives.ReadUInt32LittleEndian(countData);
                // Bounded local data decoder, not GPU/CPU state execution.
                if (countValue > 1_048_576 || (indexOffset & 1) != 0 ||
                    !TrySlice(obj, group.Key.Section, indexOffset, (ulong)countValue * 2, out var payload) ||
                    HasRelocation(obj, group.Key.Section, indexOffset, (ulong)countValue * 2))
                {
                    status = Rejected; issue = "index-count-range-alignment-or-relocation-invalid";
                }
                else
                {
                    bytes = payload.Length;
                    hash = Convert.ToHexString(SHA256.HashData(payload));
                    for (var i = 0; i < payload.Length; i += 2)
                    {
                        var value = BinaryPrimitives.ReadUInt16LittleEndian(payload[i..]);
                        min = min.HasValue ? Math.Min(min.Value, value) : value;
                        max = max.HasValue ? Math.Max(max.Value, value) : value;
                    }
                    status = Verified;
                    baseRange = minimumElements is null ? "buffer-base-unresolved" :
                        max is null || max < minimumElements ? "compatible-all-base-records" : "exceeds-base-record-range";
                }
            }
            result.Add(new(obj.ArchiveName, obj.ObjectName, group.Key.Section, group.Key.Layer, group.Key.Primitive,
                draw.CommandIndex, count?.ParameterCursor, indices?.ParameterCursor, count?.Word1, indices?.Word1,
                countValue, bytes, min, max, hash, bases.Count(b => b is not null),
                fetches.Count(f => f.Auxiliary != 0), minimumElements, baseRange, tar, geo, objectHash, status, issue));
        }
        return result;
    }

    private static bool HasRelocation(EaglRawModelObject obj, int section, uint offset, ulong bytes) =>
        obj.Elf.Relocations.Any(r => r.TargetSectionIndex == section &&
            (ulong)r.Offset < (ulong)offset + bytes && (ulong)r.Offset + 4 > offset);

    private static bool TrySlice(EaglRawModelObject obj, int section, uint offset, ulong bytes, out ReadOnlySpan<byte> slice)
    {
        slice = default;
        if (section <= 0 || section >= obj.Elf.Sections.Count) return false;
        var source = obj.Elf.Sections[section];
        if (source.Type == 8 || bytes > int.MaxValue || (ulong)offset + bytes > source.Size ||
            (ulong)source.Offset + offset + bytes > (ulong)obj.Data.Length) return false;
        slice = obj.Data.AsSpan(checked((int)((ulong)source.Offset + offset)), (int)bytes);
        return true;
    }

    public static int Run(string image, string report, string summary)
    {
        var paths = new[] { image, report, summary }.Select(Path.GetFullPath).ToArray();
        if (paths.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 3)
            throw new ArgumentException("Input image and reports must be distinct.");
        using var store = new GameAssetStore(image);
        var rows = new List<EaglIndexedDrawSourceRow>();
        var chain = EaglOriginalRenderChainAudit.Audit(store, "data/models.big", obj => rows.AddRange(Inspect(obj)));
        Directory.CreateDirectory(Path.GetDirectoryName(paths[1])!);
        using (var writer = new StreamWriter(report, false, new UTF8Encoding(false)))
        {
            writer.WriteLine("model_archive\tmodel_object\tsection\tlayer\tprimitive\tdraw_command\tcount_parameter_cursor\tindex_parameter_cursor\tcount_offset\tindex_offset\tindex_count\tindex_bytes\tminimum_index\tmaximum_index\tindex_sha256\tbuffer_base_count\truntime_selected_buffers\tminimum_base_elements\tbase_range_status\ttar_reference\tgeo_reference\tobject_sha256\tstatus\toriginal_semantics\tissue");
            foreach (var r in rows)
                writer.WriteLine(string.Join("\t", new object?[] { r.Archive, r.Object, r.Section, r.Layer, r.Primitive,
                    r.DrawCommand, Hex(r.CountParameterCursor), Hex(r.IndexParameterCursor), Hex(r.CountOffset), Hex(r.IndexOffset),
                    r.IndexCount, r.IndexBytes, r.MinimumIndex, r.MaximumIndex, r.IndexSha256, r.BufferBaseCount,
                    r.RuntimeSelectedBuffers, r.MinimumBaseElements, r.BaseRangeStatus, r.TarReference, r.GeoReference,
                    r.ObjectSha256, r.Status, "not-claimed", r.Issue }.Select(v =>
                    (Convert.ToString(v, CultureInfo.InvariantCulture) ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '))));
        }
        var values = new Dictionary<string, string>
        {
            ["draw_rows"] = rows.Count.ToString(),
            ["verified_local_sources"] = rows.Count(r => r.Status == Verified).ToString(),
            ["unresolved_sources"] = rows.Count(r => r.Status == Unresolved).ToString(),
            ["rejected_sources"] = rows.Count(r => r.Status == Rejected).ToString(),
            ["index_bytes"] = rows.Sum(r => (long)r.IndexBytes).ToString(),
            ["base_range_compatible"] = rows.Count(r => r.BaseRangeStatus == "compatible-all-base-records").ToString(),
            ["base_range_exceeded"] = rows.Count(r => r.BaseRangeStatus == "exceeds-base-record-range").ToString(),
            ["objects_loaded"] = chain.Summary.ObjectsLoaded.ToString(),
            ["report_sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(report))),
            ["status"] = rows.Count > 0 && rows.All(r => r.Status != Rejected) &&
                chain.Summary.Status == EaglOriginalRenderChainAudit.Status ? Status : "incomplete-indexed-draw-source-inventory",
            ["original_semantics"] = "not-claimed",
        };
        EaglRenderParameterAudit.WriteSummary(summary, values);
        foreach (var (key, value) in values) Console.WriteLine($"{key}: {value}");
        return values["status"] == Status ? 0 : 1;
    }

    private static string Hex(uint? v) => v.HasValue ? $"0x{v:X8}" : "";
}
