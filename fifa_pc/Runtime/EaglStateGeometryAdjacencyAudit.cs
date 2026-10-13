using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Records only the observed relationship between geometry-gated primitive
/// indices and primitives carrying a textual SHAPENAME assignment. The
/// counts are a control for possible adjacency heuristics; they never attach
/// a state primitive to a mesh group or assign GPU/material semantics.
/// </summary>
public sealed record EaglStateGeometryAdjacencyRow(
    string ModelArchiveName,
    string ObjectName,
    string ShapeName,
    int GeometryPrimitiveCount,
    int StatePrimitiveCount,
    int SamePrimitiveIndexPairs,
    int AdjacentPrimitivePairs,
    int SameLayerPairs,
    bool ObjectStateExact,
    bool GeometryGroupStateExact,
    string GeometryPrimitiveIndices,
    string StatePrimitiveIndices,
    string StateNames,
    string Status,
    string Issue);

public static class EaglStateGeometryAdjacencyAudit
{
    public static EaglStateGeometryAdjacencyRow Measure(
        string modelArchiveName,
        string objectName,
        string shapeName,
        EaglCandidateMesh candidateMesh,
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentNullException.ThrowIfNull(candidateMesh);
        ArgumentNullException.ThrowIfNull(primitives);

        var geometry = candidateMesh.Groups
            .GroupBy(group => (group.LayerIndex, group.PrimitiveIndex))
            .Select(group => group.Key)
            .OrderBy(key => key.LayerIndex)
            .ThenBy(key => key.PrimitiveIndex)
            .ToArray();
        var state = primitives
            .Where(primitive => primitive.StateAssignments.Any(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                string.Equals(
                    assignment.Value.Split(',', 2)[0].Trim(),
                    shapeName,
                    StringComparison.OrdinalIgnoreCase)))
            .Select(primitive => (primitive.LayerIndex, primitive.PrimitiveIndex))
            .Distinct()
            .OrderBy(key => key.LayerIndex)
            .ThenBy(key => key.PrimitiveIndex)
            .ToArray();

        var pairs = geometry.SelectMany(geometryKey => state.Select(stateKey =>
            (Geometry: geometryKey, State: stateKey))).ToArray();
        return new EaglStateGeometryAdjacencyRow(
            modelArchiveName,
            objectName,
            shapeName,
            geometry.Length,
            state.Length,
            pairs.Count(pair => pair.Geometry.PrimitiveIndex == pair.State.PrimitiveIndex),
            pairs.Count(pair => Math.Abs((long)pair.Geometry.PrimitiveIndex - pair.State.PrimitiveIndex) <= 1),
            pairs.Count(pair => pair.Geometry.LayerIndex == pair.State.LayerIndex),
            EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(primitives, shapeName),
            EaglShapeMaterialLoader.HasExactShapeState(candidateMesh, shapeName),
            string.Join(",", geometry.Select(key => $"{key.LayerIndex}:{key.PrimitiveIndex}")),
            string.Join(",", state.Select(key => $"{key.LayerIndex}:{key.PrimitiveIndex}")),
            string.Join("|", primitives
                .Where(primitive => state.Any(key =>
                    key.LayerIndex == primitive.LayerIndex &&
                    key.PrimitiveIndex == primitive.PrimitiveIndex))
                .SelectMany(primitive => primitive.StateAssignments
                    .Where(assignment => string.Equals(
                        assignment.Name,
                        "SHAPENAME",
                        StringComparison.Ordinal))
                    .Select(assignment => $"{assignment.Name}={assignment.Value}"))),
            "verified-control",
            string.Empty);
    }

    public static EaglStateGeometryAdjacencyRow Rejected(
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
            0,
            0,
            0,
            false,
            false,
            string.Empty,
            string.Empty,
            string.Empty,
            "rejected-candidate",
            issue);

    public static void WriteReport(
        string path,
        IEnumerable<EaglStateGeometryAdjacencyRow> rows)
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
                "geometry_primitive_count",
                "state_primitive_count",
                "same_primitive_index_pairs",
                "adjacent_primitive_pairs",
                "same_layer_pairs",
                "object_state_exact",
                "geometry_group_state_exact",
                "geometry_primitive_indices",
                "state_primitive_indices",
                "state_names",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ObjectName),
            Field(row.ShapeName),
            row.GeometryPrimitiveCount.ToString(CultureInfo.InvariantCulture),
            row.StatePrimitiveCount.ToString(CultureInfo.InvariantCulture),
            row.SamePrimitiveIndexPairs.ToString(CultureInfo.InvariantCulture),
            row.AdjacentPrimitivePairs.ToString(CultureInfo.InvariantCulture),
            row.SameLayerPairs.ToString(CultureInfo.InvariantCulture),
            row.ObjectStateExact.ToString(),
            row.GeometryGroupStateExact.ToString(),
            Field(row.GeometryPrimitiveIndices),
            Field(row.StatePrimitiveIndices),
            Field(row.StateNames),
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

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
