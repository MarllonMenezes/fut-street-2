using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglCandidateGeometryStreamRow(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    int LayerIndex,
    uint PrimitiveIndex,
    int CandidateSourceSlot,
    string CandidateStreamName,
    int CandidateVertexCount,
    int CandidateTriangleCount,
    uint CandidateStreamStride,
    uint CandidateStreamByteLength,
    uint CandidateStreamElementCount,
    bool CandidatePrimitiveComplete,
    bool CandidateGeometryFiniteAndIndexed,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglCandidateGeometryStreamSummary(
    string ModelArchivePath,
    int CandidateCount,
    int CandidateAssetsLoaded,
    int CandidateGroups,
    int CandidateGroupsWithPrimitive,
    int CandidateSourceSlotZeroRows,
    int CandidateCompletePrimitiveRows,
    int CandidateFiniteAndIndexedRows,
    int RejectedCandidates,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Emits per-group metadata for the managed candidate geometry path. The
/// builder's source-slot choice and the raw stream measurements are preserved
/// as candidate evidence only. No stream is named as position, UV, material,
/// normal, weight, skinning, rigging, animation, morph or GPU state.
/// </summary>
public static class EaglCandidateGeometryStreamAudit
{
    public const string Status = "verified-bounded-candidate-geometry-stream-audit";
    public const string Provenance =
        "same-trunk-managed-candidate-mesh-groups+slot-0-stream-metadata+finite-indexed-geometry;candidate-only;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    public static (IReadOnlyList<EaglCandidateGeometryStreamRow> Rows,
        EaglCandidateGeometryStreamSummary Summary) Audit(
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
        var rows = new List<EaglCandidateGeometryStreamRow>();
        var loaded = 0;
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
                foreach (var group in candidate.Mesh.Groups
                             .OrderBy(group => group.LayerIndex)
                             .ThenBy(group => group.PrimitiveIndex))
                {
                    var primitive = candidate.Primitives.FirstOrDefault(item =>
                        item.LayerIndex == group.LayerIndex &&
                        item.PrimitiveIndex == group.PrimitiveIndex);
                    if (primitive is null)
                    {
                        rows.Add(RejectedRow(match, group.LayerIndex, group.PrimitiveIndex,
                            "candidate mesh group has no matching inspected primitive"));
                        continue;
                    }

                    var sourceBinding = primitive.VertexFetchBindings.FirstOrDefault(binding =>
                        binding.FetchSlot.Slot == 0);
                    var finiteAndIndexed = HasFiniteAndIndexedGeometry(group);
                    rows.Add(new EaglCandidateGeometryStreamRow(
                        match.ModelArchiveName,
                        match.ModelObjectName,
                        match.ShapeName,
                        group.LayerIndex,
                        group.PrimitiveIndex,
                        0,
                        group.StreamName,
                        group.Vertices.Count,
                        group.Triangles.Count,
                        sourceBinding?.Stream.Stride ?? 0,
                        sourceBinding?.Stream.ByteLength ?? 0,
                        sourceBinding?.Stream.ElementCount ?? 0,
                        EaglCandidateFetchAssociationAudit.IsCompleteFiveSlotLayout(primitive),
                        finiteAndIndexed,
                        sourceBinding is not null && finiteAndIndexed
                            ? Status
                            : $"inconclusive-{Status}",
                        OriginalSemantics,
                        sourceBinding is null
                            ? "candidate primitive has no slot-0 stream binding"
                            : finiteAndIndexed
                                ? string.Empty
                                : "candidate group contains nonfinite vertices or invalid indices"));
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
                rows.Add(RejectedRow(match, -1, 0, exception.Message));
            }
        }

        var summary = new EaglCandidateGeometryStreamSummary(
            modelArchivePath,
            matches.Length,
            loaded,
            rows.Count(row => row.LayerIndex >= 0),
            rows.Count(row => row.LayerIndex >= 0 && row.CandidateStreamName.Length > 0),
            rows.Count(row => row.LayerIndex >= 0 && row.CandidateSourceSlot == 0),
            rows.Count(row => row.LayerIndex >= 0 && row.CandidatePrimitiveComplete),
            rows.Count(row => row.LayerIndex >= 0 && row.CandidateGeometryFiniteAndIndexed),
            rejected,
            matches.Length > 0 && rejected == 0 && loaded == matches.Length &&
            rows.Count > 0 && rows.All(row => row.Status == Status)
                ? Status
                : $"inconclusive-{Status}",
            Provenance,
            OriginalSemantics);
        return (rows, summary);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglCandidateGeometryStreamRow> rows)
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
                "candidate_source_slot",
                "candidate_stream_name",
                "candidate_vertex_count",
                "candidate_triangle_count",
                "candidate_stream_stride",
                "candidate_stream_byte_length",
                "candidate_stream_element_count",
                "candidate_primitive_complete",
                "candidate_geometry_finite_and_indexed",
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
            row.CandidateSourceSlot.ToString(CultureInfo.InvariantCulture),
            Field(row.CandidateStreamName),
            row.CandidateVertexCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateTriangleCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateStreamStride.ToString(CultureInfo.InvariantCulture),
            row.CandidateStreamByteLength.ToString(CultureInfo.InvariantCulture),
            row.CandidateStreamElementCount.ToString(CultureInfo.InvariantCulture),
            row.CandidatePrimitiveComplete.ToString(),
            row.CandidateGeometryFiniteAndIndexed.ToString(),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglCandidateGeometryStreamSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        File.WriteAllLines(path, new[]
        {
            string.Join("\t", new[]
            {
                "model_archive_path",
                "candidate_count",
                "candidate_assets_loaded",
                "candidate_groups",
                "candidate_groups_with_primitive",
                "candidate_source_slot_zero_rows",
                "candidate_complete_primitive_rows",
                "candidate_finite_and_indexed_rows",
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
                summary.CandidateGroups.ToString(CultureInfo.InvariantCulture),
                summary.CandidateGroupsWithPrimitive.ToString(CultureInfo.InvariantCulture),
                summary.CandidateSourceSlotZeroRows.ToString(CultureInfo.InvariantCulture),
                summary.CandidateCompletePrimitiveRows.ToString(CultureInfo.InvariantCulture),
                summary.CandidateFiniteAndIndexedRows.ToString(CultureInfo.InvariantCulture),
                summary.RejectedCandidates.ToString(CultureInfo.InvariantCulture),
                Field(summary.Status),
                Field(summary.Provenance),
                Field(summary.OriginalSemantics),
            }),
        }, new UTF8Encoding(false));
    }

    private static EaglCandidateGeometryStreamRow RejectedRow(
        EaglShapeMaterialAsset match,
        int layer,
        uint primitive,
        string issue) =>
        new(
            match.ModelArchiveName,
            match.ModelObjectName,
            match.ShapeName,
            layer,
            primitive,
            -1,
            string.Empty,
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            $"inconclusive-{Status}",
            OriginalSemantics,
            issue);

    private static bool HasFiniteAndIndexedGeometry(EaglCandidateMeshGroup group)
    {
        if (group.Vertices.Any(vertex =>
                !float.IsFinite(vertex.X) ||
                !float.IsFinite(vertex.Y) ||
                !float.IsFinite(vertex.Z)))
        {
            return false;
        }

        return group.Triangles.All(triangle =>
            triangle.A >= 0 && triangle.B >= 0 && triangle.C >= 0 &&
            triangle.A < group.Vertices.Count &&
            triangle.B < group.Vertices.Count &&
            triangle.C < group.Vertices.Count);
    }

    private static void EnsureParent(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string Field(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
