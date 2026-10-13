using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Audits decoded SHPX image entries as an asset-level observation. The table
/// order and decoded pixel hashes are preserved, but no timing, pose, rig,
/// material, UV, skinning, morph, or GPU meaning is assigned.
/// </summary>
public sealed record ShpxImageEntryAuditRow(
    string AssetPath,
    string SourceSha256,
    int SourceBytes,
    uint DeclaredSize,
    bool DeclaredSizeMatches,
    string FormatVersion,
    uint TableEnd,
    int EntryCount,
    int EntryIndex,
    string EntryName,
    uint Offset,
    uint Size,
    string DecodeStatus,
    int Width,
    int Height,
    string PixelFormat,
    int DecodedRgbaBytes,
    string DecodedRgbaSha256,
    string Error,
    string AuditStatus,
    string OrderedEntryTableSha256,
    string OrderedDecodedEntrySha256,
    string TimingEvidence,
    string OriginalSemantics);

public static class ShpxImageEntryAudit
{
    public static IReadOnlyList<ShpxImageEntryAuditRow> Audit(string assetPath, ReadOnlySpan<byte> asset)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);

        var inspection = XshReader.Inspect(asset);
        var package = ShapeImagePackageLoader.Load(asset);
        var decoded = ShapeImageTextureLoader.DecodeSupported(asset);
        if (decoded.Count != package.Entries.Count)
        {
            throw new InvalidDataException(
                $"SHPX decode count {decoded.Count} does not match package entry count {package.Entries.Count}.");
        }

        var sourceBytes = asset.Length;
        var sourceSha256 = Convert.ToHexString(SHA256.HashData(asset));
        var orderedTable = string.Join("|", package.Entries.Select((entry, index) =>
            $"{index}:{entry.Name}:{entry.Offset:X8}:{entry.Size:X8}"));
        var orderedTableSha256 = HashText(orderedTable);
        var orderedDecoded = string.Join("|", decoded.Select((result, index) =>
        {
            if (result.Texture is null)
            {
                return $"{index}:{result.Entry.Name}:unsupported:{result.UnsupportedReason}";
            }

            var texture = result.Texture;
            return $"{index}:{texture.Name}:{texture.PixelFormat}:{texture.Width}x{texture.Height}:{HashBytes(texture.DecodedRgba)}";
        }));
        var orderedDecodedSha256 = HashText(orderedDecoded);
        var auditStatus = decoded.All(result => result.Texture is not null) &&
                          inspection.DeclaredSizeMatches &&
                          inspection.OffsetsMonotonic
            ? "verified-image-entries"
            : "partial-image-entries";

        return decoded.Select((result, index) =>
        {
            var texture = result.Texture;
            return new ShpxImageEntryAuditRow(
                assetPath,
                sourceSha256,
                sourceBytes,
                inspection.DeclaredSize,
                inspection.DeclaredSizeMatches,
                inspection.FormatVersion,
                inspection.TableEnd,
                checked((int)inspection.EntryCount),
                index,
                result.Entry.Name,
                result.Entry.Offset,
                result.Entry.Size,
                texture is null ? "unsupported" : "decoded",
                texture?.Width ?? 0,
                texture?.Height ?? 0,
                texture?.PixelFormat ?? string.Empty,
                texture?.DecodedRgba.Length ?? 0,
                texture is null ? string.Empty : HashBytes(texture.DecodedRgba),
                result.UnsupportedReason ?? string.Empty,
                auditStatus,
                orderedTableSha256,
                orderedDecodedSha256,
                "not-proven",
                "not-claimed");
        }).ToArray();
    }

    public static void WriteReport(string path, IReadOnlyList<ShpxImageEntryAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        if (rows.Count == 0)
        {
            throw new InvalidDataException("SHPX image-entry audit has no rows.");
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "asset_path",
                "source_sha256",
                "source_bytes",
                "declared_size",
                "declared_size_matches",
                "format_version",
                "table_end",
                "entry_count",
                "entry_index",
                "entry_name",
                "offset",
                "size",
                "decode_status",
                "width",
                "height",
                "pixel_format",
                "decoded_rgba_bytes",
                "decoded_rgba_sha256",
                "error",
                "audit_status",
                "ordered_entry_table_sha256",
                "ordered_decoded_entry_sha256",
                "timing_evidence",
                "original_semantics",
            }),
        };

        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.AssetPath),
            row.SourceSha256,
            row.SourceBytes.ToString(CultureInfo.InvariantCulture),
            row.DeclaredSize.ToString(CultureInfo.InvariantCulture),
            row.DeclaredSizeMatches.ToString(),
            Escape(row.FormatVersion),
            $"0x{row.TableEnd:X}",
            row.EntryCount.ToString(CultureInfo.InvariantCulture),
            row.EntryIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.EntryName),
            $"0x{row.Offset:X}",
            row.Size.ToString(CultureInfo.InvariantCulture),
            row.DecodeStatus,
            row.Width.ToString(CultureInfo.InvariantCulture),
            row.Height.ToString(CultureInfo.InvariantCulture),
            row.PixelFormat,
            row.DecodedRgbaBytes.ToString(CultureInfo.InvariantCulture),
            row.DecodedRgbaSha256,
            Escape(row.Error),
            row.AuditStatus,
            row.OrderedEntryTableSha256,
            row.OrderedDecodedEntrySha256,
            row.TimingEvidence,
            row.OriginalSemantics,
        })));

        File.WriteAllLines(
            path,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string HashBytes(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string HashText(string text) =>
        HashBytes(Encoding.UTF8.GetBytes(text));

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
             .Replace("\r", " ", StringComparison.Ordinal)
             .Replace("\n", " ", StringComparison.Ordinal);
}
