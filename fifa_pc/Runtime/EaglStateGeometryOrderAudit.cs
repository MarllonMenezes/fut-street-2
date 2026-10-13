using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Records the ordering of candidate geometry primitive indices and exact
/// SHAPENAME-bearing primitive indices in each observed layer. The intervals
/// are an opaque ordering control only: they do not attach a state run to a
/// mesh group and do not assign material, UV, skinning, rigging, animation or
/// GPU command semantics.
/// </summary>
public sealed record EaglStateGeometryOrderRow(
    string ModelArchiveName,
    string ObjectName,
    string ShapeName,
    int LayerIndex,
    int IntervalIndex,
    string IntervalKind,
    string PreviousGeometryPrimitiveIndex,
    string NextGeometryPrimitiveIndex,
    int GeometryCandidateCountInLayer,
    int StatePrimitiveCount,
    string StatePrimitiveIndices,
    int StateCommandCount,
    int StateCommandBytes,
    string StateCommandProfileSha256,
    bool AllStateStreamsComplete,
    string Status,
    string Issue);

public sealed record EaglStateGeometryOrderSummary(
    int GeometryCandidateCount,
    int StatePrimitiveCount,
    int IntervalCount,
    int NonEmptyIntervalCount,
    int StatePrimitivesPartitioned,
    int StatePrimitivesUnpartitioned,
    int CompleteStateIntervals,
    int DistinctStateRunProfiles);

public sealed record EaglStateGeometryOrderAuditResult(
    IReadOnlyList<EaglStateGeometryOrderRow> Rows,
    EaglStateGeometryOrderSummary Summary);

public static class EaglStateGeometryOrderAudit
{
    public static EaglStateGeometryOrderAuditResult Measure(
        string modelArchiveName,
        string objectName,
        string shapeName,
        EaglCandidateMesh mesh,
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(primitives);

        var geometryKeys = mesh.Groups
            .GroupBy(group => (group.LayerIndex, group.PrimitiveIndex))
            .Select(group => group.Key)
            .OrderBy(key => key.LayerIndex)
            .ThenBy(key => key.PrimitiveIndex)
            .ToArray();
        var geometryKeysByLayer = geometryKeys
            .GroupBy(key => key.LayerIndex)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(key => key.PrimitiveIndex).ToArray());
        var shapeStates = primitives
            .Where(primitive => primitive.StateAssignments.Any(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                string.Equals(
                    assignment.Value.Split(',', 2)[0].Trim(),
                    shapeName,
                    StringComparison.OrdinalIgnoreCase)))
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .ToArray();

        var rows = new List<EaglStateGeometryOrderRow>();
        var intervalIndex = 0;
        foreach (var layerGroup in shapeStates
                     .Select(state => state.LayerIndex)
                     .Concat(geometryKeysByLayer.Keys)
                     .Distinct()
                     .OrderBy(layer => layer))
        {
            var layerGeometry = geometryKeysByLayer.TryGetValue(layerGroup, out var candidates)
                ? candidates
                : Array.Empty<(int LayerIndex, uint PrimitiveIndex)>();
            var layerStates = shapeStates
                .Where(state => state.LayerIndex == layerGroup)
                .OrderBy(state => state.PrimitiveIndex)
                .ToArray();

            if (layerGeometry.Length == 0)
            {
                rows.Add(BuildRow(
                    modelArchiveName,
                    objectName,
                    shapeName,
                    layerGroup,
                    intervalIndex++,
                    "state-layer-without-geometry",
                    string.Empty,
                    string.Empty,
                    0,
                    layerStates));
                continue;
            }

            for (var boundary = 0; boundary <= layerGeometry.Length; boundary++)
            {
                var previous = boundary == 0 ? (uint?)null : layerGeometry[boundary - 1].PrimitiveIndex;
                var next = boundary == layerGeometry.Length ? (uint?)null : layerGeometry[boundary].PrimitiveIndex;
                var intervalStates = layerStates
                    .Where(state =>
                        (!previous.HasValue || state.PrimitiveIndex > previous.Value) &&
                        (!next.HasValue || state.PrimitiveIndex < next.Value))
                    .ToArray();
                var intervalKind = boundary == 0
                    ? "before-first-geometry"
                    : boundary == layerGeometry.Length
                        ? "after-last-geometry"
                        : "between-geometry-candidates";
                rows.Add(BuildRow(
                    modelArchiveName,
                    objectName,
                    shapeName,
                    layerGroup,
                    intervalIndex++,
                    intervalKind,
                    FormatIndex(previous),
                    FormatIndex(next),
                    layerGeometry.Length,
                    intervalStates));
            }
        }

        var partitionedStatePrimitives = rows.Sum(row => row.StatePrimitiveCount);
        var summary = new EaglStateGeometryOrderSummary(
            geometryKeys.Length,
            shapeStates.Length,
            rows.Count,
            rows.Count(row => row.StatePrimitiveCount > 0),
            partitionedStatePrimitives,
            Math.Max(0, shapeStates.Length - partitionedStatePrimitives),
            rows.Count(row => row.StatePrimitiveCount > 0 && row.AllStateStreamsComplete),
            rows.Select(row => row.StateCommandProfileSha256)
                .Where(value => !string.IsNullOrEmpty(value))
                .Distinct(StringComparer.Ordinal)
                .Count());
        return new EaglStateGeometryOrderAuditResult(rows, summary);
    }

    public static EaglStateGeometryOrderRow Rejected(
        string modelArchiveName,
        string objectName,
        string shapeName,
        string issue) =>
        new(
            modelArchiveName,
            objectName,
            shapeName,
            0,
            0,
            "rejected-candidate",
            string.Empty,
            string.Empty,
            0,
            0,
            string.Empty,
            0,
            0,
            string.Empty,
            false,
            "rejected-candidate",
            issue);

    public static void WriteReport(
        string path,
        IEnumerable<EaglStateGeometryOrderRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "object_name",
                "shape_name",
                "layer",
                "interval_index",
                "interval_kind",
                "previous_geometry_primitive",
                "next_geometry_primitive",
                "geometry_candidate_count_in_layer",
                "state_primitive_count",
                "state_primitive_indices",
                "state_command_count",
                "state_command_bytes",
                "state_command_profile_sha256",
                "all_state_streams_complete",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ObjectName),
            Field(row.ShapeName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.IntervalIndex.ToString(CultureInfo.InvariantCulture),
            Field(row.IntervalKind),
            Field(row.PreviousGeometryPrimitiveIndex),
            Field(row.NextGeometryPrimitiveIndex),
            row.GeometryCandidateCountInLayer.ToString(CultureInfo.InvariantCulture),
            row.StatePrimitiveCount.ToString(CultureInfo.InvariantCulture),
            Field(row.StatePrimitiveIndices),
            row.StateCommandCount.ToString(CultureInfo.InvariantCulture),
            row.StateCommandBytes.ToString(CultureInfo.InvariantCulture),
            row.StateCommandProfileSha256,
            row.AllStateStreamsComplete.ToString(),
            Field(row.Status),
            Field(row.Issue),
        })));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static EaglStateGeometryOrderRow BuildRow(
        string modelArchiveName,
        string objectName,
        string shapeName,
        int layerIndex,
        int intervalIndex,
        string intervalKind,
        string previousGeometryPrimitiveIndex,
        string nextGeometryPrimitiveIndex,
        int geometryCandidateCountInLayer,
        IReadOnlyList<EaglModelPrimitive> states)
    {
        var orderedStates = states
            .OrderBy(state => state.PrimitiveIndex)
            .ToArray();
        var canonical = string.Join("|", orderedStates.Select(state => string.Join(":", new[]
        {
            state.LayerIndex.ToString(CultureInfo.InvariantCulture),
            state.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            state.RenderCommandStream.Commands.Count.ToString(CultureInfo.InvariantCulture),
            GetCommandBytes(state).Length.ToString(CultureInfo.InvariantCulture),
            state.RenderCommandStream.IsComplete.ToString(),
        })));
        var profileHash = orderedStates.Length == 0
            ? string.Empty
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return new EaglStateGeometryOrderRow(
            modelArchiveName,
            objectName,
            shapeName,
            layerIndex,
            intervalIndex,
            intervalKind,
            previousGeometryPrimitiveIndex,
            nextGeometryPrimitiveIndex,
            geometryCandidateCountInLayer,
            orderedStates.Length,
            string.Join(",", orderedStates.Select(state =>
                state.PrimitiveIndex.ToString(CultureInfo.InvariantCulture))),
            orderedStates.Sum(state => state.RenderCommandStream.Commands.Count),
            orderedStates.Sum(state => GetCommandBytes(state).Length),
            profileHash,
            orderedStates.All(state => state.RenderCommandStream.IsComplete),
            "ordered-index-control",
            string.Empty);
    }

    private static byte[] GetCommandBytes(EaglModelPrimitive primitive) =>
        EaglModelReader.GetRawCommandStreamBytes(new[] { primitive });

    private static string FormatIndex(uint? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
