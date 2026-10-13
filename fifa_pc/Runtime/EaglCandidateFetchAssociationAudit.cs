using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglCandidateFetchAssociationRow(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    int LayerIndex,
    uint PrimitiveIndex,
    string PrimitiveType,
    string StateNames,
    bool CommandStreamComplete,
    int CommandCount,
    string CommandBytesSha256,
    int FetchSlotCount,
    int FetchBindingCount,
    bool CompleteFiveSlotLayout,
    int MeshGroupCount,
    string SlotRecords,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglCandidateFetchAssociationSummary(
    string ModelArchivePath,
    int CandidateCount,
    int CandidateAssetsLoaded,
    int CandidateAssetsWithCompleteFiveSlotLayouts,
    int PrimitiveRows,
    int CompleteFiveSlotPrimitiveRows,
    int SlotRows,
    int ValidSlotRows,
    int RejectedCandidates,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Audits the exact structural relationship between observed fetch-slot
/// records and validated __VB stream metadata in the managed candidate path.
/// Slot numbers, strides, lengths and symbol text are preserved as evidence;
/// no slot is named as a UV, material, normal, weight, skinning or GPU
/// semantic.
/// </summary>
public static class EaglCandidateFetchAssociationAudit
{
    public const string Status = "verified-bounded-candidate-fetch-record-association";
    public const string Provenance =
        "same-trunk-EAGL-object+validated-__VB-records+observed-fetch-slot-metadata;candidate-only;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    public static (IReadOnlyList<EaglCandidateFetchAssociationRow> Rows,
        EaglCandidateFetchAssociationSummary Summary) Audit(
        GameAssetStore store,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var matches = EaglShapeMaterialLoader.LoadSameTrunkMatches(store, modelArchivePath)
            .OrderBy(match => match.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.ModelObjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(match => match.ShapeName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var rows = new List<EaglCandidateFetchAssociationRow>();
        var loaded = 0;
        var completeAssets = 0;
        var rejected = 0;
        foreach (var match in matches)
        {
            try
            {
                var candidate = EaglCandidateAssetLoader.Load(
                    store,
                    modelArchivePath,
                    match.ModelArchiveName,
                    match.ModelObjectName);
                loaded++;
                var candidateRows = AuditCandidate(match, candidate);
                rows.AddRange(candidateRows);
                if (candidateRows.Count > 0 && candidateRows.All(row => row.CompleteFiveSlotLayout))
                {
                    completeAssets++;
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                EndOfStreamException or
                IOException or
                OverflowException or
                ArgumentOutOfRangeException or
                FileNotFoundException)
            {
                rejected++;
                rows.Add(new EaglCandidateFetchAssociationRow(
                    match.ModelArchiveName,
                    match.ModelObjectName,
                    match.ShapeName,
                    -1,
                    0,
                    "none",
                    string.Empty,
                    false,
                    0,
                    string.Empty,
                    0,
                    0,
                    false,
                    0,
                    string.Empty,
                    "rejected-candidate",
                    OriginalSemantics,
                    exception.Message));
            }
        }

        var validSlotRows = rows.Sum(row => CountValidSlotRecords(row.SlotRecords));
        var slotRows = rows.Sum(row => CountSlotRecords(row.SlotRecords));
        var summary = new EaglCandidateFetchAssociationSummary(
            modelArchivePath,
            matches.Length,
            loaded,
            completeAssets,
            rows.Count,
            rows.Count(row => row.CompleteFiveSlotLayout),
            slotRows,
            validSlotRows,
            rejected,
            matches.Length > 0 && rejected == 0 && loaded == matches.Length &&
            rows.Count > 0 && validSlotRows == slotRows
                ? Status
                : "incomplete-candidate-fetch-record-association",
            Provenance,
            OriginalSemantics);
        return (rows, summary);
    }

    public static IReadOnlyList<EaglCandidateFetchAssociationRow> AuditCandidate(
        EaglShapeMaterialAsset match,
        EaglCandidateAsset candidate)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(candidate);

        return candidate.Primitives
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .Select(primitive => BuildRow(match, candidate, primitive))
            .ToArray();
    }

    public static bool IsCompleteFiveSlotLayout(EaglModelPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        var slots = primitive.VertexFetchSlots
            .OrderBy(slot => slot.Slot)
            .ToArray();
        return primitive.RenderCommandStream.IsComplete &&
               slots.Length == 5 &&
               slots.Select(slot => slot.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
               primitive.VertexFetchBindings.Count == 5 &&
               primitive.VertexFetchBindings.All(binding =>
                   binding.FetchSlot.Stride == binding.Stream.Stride &&
                   binding.Stream.ByteLength > 0 &&
                   binding.Stream.ElementCount > 0);
    }

    public static int CountSlotRecords(string slotRecords) =>
        string.IsNullOrWhiteSpace(slotRecords)
            ? 0
            : slotRecords.Split('|', StringSplitOptions.RemoveEmptyEntries).Length;

    public static int CountValidSlotRecords(string slotRecords) =>
        string.IsNullOrWhiteSpace(slotRecords)
            ? 0
            : slotRecords.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Count(record => record.EndsWith(";valid=True", StringComparison.Ordinal));

    public static void WriteReport(
        string path,
        IEnumerable<EaglCandidateFetchAssociationRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "model_object",
                "shape_name",
                "layer",
                "primitive",
                "primitive_type",
                "state_names",
                "command_stream_complete",
                "command_count",
                "command_bytes_sha256",
                "fetch_slot_count",
                "fetch_binding_count",
                "complete_five_slot_layout",
                "mesh_group_count",
                "slot_records",
                "status",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            Field(row.ShapeName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            Field(row.PrimitiveType),
            Field(row.StateNames),
                row.CommandStreamComplete.ToString(),
                row.CommandCount.ToString(CultureInfo.InvariantCulture),
                row.CommandBytesSha256,
                row.FetchSlotCount.ToString(CultureInfo.InvariantCulture),
            row.FetchBindingCount.ToString(CultureInfo.InvariantCulture),
            row.CompleteFiveSlotLayout.ToString(),
            row.MeshGroupCount.ToString(CultureInfo.InvariantCulture),
            Field(row.SlotRecords),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglCandidateFetchAssociationSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var lines = new[]
        {
            string.Join("\t", new[]
            {
                "model_archive_path",
                "candidate_count",
                "candidate_assets_loaded",
                "candidate_assets_with_complete_five_slot_layouts",
                "primitive_rows",
                "complete_five_slot_primitive_rows",
                "slot_rows",
                "valid_slot_rows",
                "rejected_candidates",
                "status",
                "provenance",
                "original_semantics",
            }),
            string.Join("\t", new[]
            {
                Field(summary.ModelArchivePath),
                summary.CandidateCount.ToString(CultureInfo.InvariantCulture),
                summary.CandidateAssetsLoaded.ToString(CultureInfo.InvariantCulture),
                summary.CandidateAssetsWithCompleteFiveSlotLayouts.ToString(CultureInfo.InvariantCulture),
                summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture),
                summary.CompleteFiveSlotPrimitiveRows.ToString(CultureInfo.InvariantCulture),
                summary.SlotRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidSlotRows.ToString(CultureInfo.InvariantCulture),
                summary.RejectedCandidates.ToString(CultureInfo.InvariantCulture),
                Field(summary.Status),
                Field(summary.Provenance),
                Field(summary.OriginalSemantics),
            }),
        };
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static EaglCandidateFetchAssociationRow BuildRow(
        EaglShapeMaterialAsset match,
        EaglCandidateAsset candidate,
        EaglModelPrimitive primitive)
    {
        var bindings = primitive.VertexFetchBindings
            .OrderBy(binding => binding.FetchSlot.Slot)
            .ToArray();
        var slotRecords = bindings.Select(binding =>
        {
            var valid = primitive.VertexFetchSlots.Any(slot =>
                slot.Slot == binding.FetchSlot.Slot &&
                slot.Stride == binding.FetchSlot.Stride &&
                binding.Stream.Stride == binding.FetchSlot.Stride &&
                binding.Stream.ByteLength > 0 &&
                binding.Stream.ElementCount > 0);
            return string.Join(";", new[]
            {
                $"slot={binding.FetchSlot.Slot.ToString(CultureInfo.InvariantCulture)}",
                $"aux=0x{binding.FetchSlot.AuxiliaryValue:X8}",
                $"command_stride={binding.FetchSlot.Stride.ToString(CultureInfo.InvariantCulture)}",
                $"stream_stride={binding.Stream.Stride.ToString(CultureInfo.InvariantCulture)}",
                $"stream_bytes={binding.Stream.ByteLength.ToString(CultureInfo.InvariantCulture)}",
                $"elements={binding.Stream.ElementCount.ToString(CultureInfo.InvariantCulture)}",
                $"stream={Field(binding.Stream.SymbolName)}",
                $"valid={valid}",
            });
        });
        var complete = IsCompleteFiveSlotLayout(primitive);
        var commandBytesSha256 = Convert.ToHexString(SHA256.HashData(
            EaglModelReader.GetRawCommandStreamBytes(new[] { primitive })));
        var meshGroupCount = candidate.Mesh.Groups.Count(group =>
            group.LayerIndex == primitive.LayerIndex &&
            group.PrimitiveIndex == primitive.PrimitiveIndex);
        return new EaglCandidateFetchAssociationRow(
            match.ModelArchiveName,
            match.ModelObjectName,
            match.ShapeName,
            primitive.LayerIndex,
            primitive.PrimitiveIndex,
            primitive.PrimitiveType ?? "none",
            string.Join("|", primitive.StateAssignments.Select(assignment =>
                $"{assignment.Name}={assignment.Value}")),
            primitive.RenderCommandStream.IsComplete,
            primitive.RenderCommandStream.Commands.Count,
            commandBytesSha256,
            primitive.VertexFetchSlots.Count,
            primitive.VertexFetchBindings.Count,
            complete,
            meshGroupCount,
            string.Join("|", slotRecords),
            complete ? Status : "opaque-incomplete-fetch-record-association",
            OriginalSemantics,
            string.Empty);
    }

    private static string Field(string value) =>
        (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
