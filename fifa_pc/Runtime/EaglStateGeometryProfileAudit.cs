using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Compares opaque, per-primitive command-shape profiles for candidate
/// geometry and SHAPENAME-bearing state records. The profiles preserve only
/// observed sequence/count/byte relationships. They are diagnostic evidence,
/// not a state-to-mesh binding or a GPU/material interpretation.
/// </summary>
public sealed record EaglStateGeometryProfileRow(
    string ModelArchiveName,
    string ObjectName,
    string ShapeName,
    int GeometryLayerIndex,
    uint GeometryPrimitiveIndex,
    int GeometryVertexCount,
    int GeometryTriangleCount,
    int GeometrySlot1ValueCount,
    int GeometryCommandCount,
    int GeometryCommandBytes,
    string GeometryCommandShapeSha256,
    int StateLayerIndex,
    uint StatePrimitiveIndex,
    int StateCommandCount,
    int StateCommandBytes,
    string StateCommandShapeSha256,
    int CommonPrefixBytes,
    bool SameLayer,
    bool SamePrimitiveIndex,
    bool NeighborPrimitiveIndex,
    bool ExactCommandShape,
    int StateProfileTieCount,
    bool GeometryStreamComplete,
    bool StateStreamComplete,
    string Status,
    string Issue);

public sealed record EaglStateGeometryProfileSummary(
    int GeometryGroups,
    int StatePrimitives,
    int PairRows,
    int SameLayerPairs,
    int SamePrimitiveIndexPairs,
    int NeighborPrimitiveIndexPairs,
    int ExactCommandShapePairs,
    int UniqueStateProfileRows,
    int DistinctGeometryCommandShapes,
    int DistinctStateCommandShapes);

public sealed record EaglStateGeometryProfileAuditResult(
    IReadOnlyList<EaglStateGeometryProfileRow> Rows,
    EaglStateGeometryProfileSummary Summary);

public static class EaglStateGeometryProfileAudit
{
    public static EaglStateGeometryProfileAuditResult Measure(
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

        var geometryByKey = primitives
            .Where(primitive => mesh.Groups.Any(group =>
                group.LayerIndex == primitive.LayerIndex &&
                group.PrimitiveIndex == primitive.PrimitiveIndex))
            .GroupBy(primitive => (primitive.LayerIndex, primitive.PrimitiveIndex))
            .ToDictionary(group => group.Key, group => group.First());
        var statePrimitives = primitives
            .Where(primitive => primitive.StateAssignments.Any(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                string.Equals(
                    assignment.Value.Split(',', 2)[0].Trim(),
                    shapeName,
                    StringComparison.OrdinalIgnoreCase)))
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .ToArray();
        var stateProfileCounts = statePrimitives
            .GroupBy(GetProfileKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var rows = new List<EaglStateGeometryProfileRow>();

        foreach (var group in mesh.Groups
                     .OrderBy(group => group.LayerIndex)
                     .ThenBy(group => group.PrimitiveIndex))
        {
            if (!geometryByKey.TryGetValue((group.LayerIndex, group.PrimitiveIndex), out var geometry))
            {
                continue;
            }

            var geometryShape = GetCommandShapeSha256(geometry);
            var geometryBytes = GetRawCommandBytes(geometry);
            foreach (var state in statePrimitives)
            {
                var stateShape = GetCommandShapeSha256(state);
                var stateBytes = GetRawCommandBytes(state);
                rows.Add(new EaglStateGeometryProfileRow(
                    modelArchiveName,
                    objectName,
                    shapeName,
                    geometry.LayerIndex,
                    geometry.PrimitiveIndex,
                    group.Vertices.Count,
                    group.Triangles.Count,
                    group.Slot1Attributes.Count,
                    geometry.RenderCommandStream.Commands.Count,
                    geometryBytes.Length,
                    geometryShape,
                    state.LayerIndex,
                    state.PrimitiveIndex,
                    state.RenderCommandStream.Commands.Count,
                    stateBytes.Length,
                    stateShape,
                    GetCommonPrefixLength(geometryBytes, stateBytes),
                    geometry.LayerIndex == state.LayerIndex,
                    geometry.PrimitiveIndex == state.PrimitiveIndex,
                    Math.Abs((long)geometry.PrimitiveIndex - state.PrimitiveIndex) == 1,
                    string.Equals(geometryShape, stateShape, StringComparison.Ordinal),
                    stateProfileCounts[GetProfileKey(state)],
                    geometry.RenderCommandStream.IsComplete,
                    state.RenderCommandStream.IsComplete,
                    "opaque-profile-control",
                    string.Empty));
            }
        }

        var summary = new EaglStateGeometryProfileSummary(
            mesh.Groups.Count,
            statePrimitives.Length,
            rows.Count,
            rows.Count(row => row.SameLayer),
            rows.Count(row => row.SamePrimitiveIndex),
            rows.Count(row => row.NeighborPrimitiveIndex),
            rows.Count(row => row.ExactCommandShape),
            rows.Count(row => row.StateProfileTieCount == 1),
            rows.Select(row => row.GeometryCommandShapeSha256)
                .Distinct(StringComparer.Ordinal)
                .Count(),
            rows.Select(row => row.StateCommandShapeSha256)
                .Distinct(StringComparer.Ordinal)
                .Count());
        return new EaglStateGeometryProfileAuditResult(rows, summary);
    }

    public static EaglStateGeometryProfileRow Rejected(
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
            0,
            0,
            string.Empty,
            0,
            0,
            0,
            0,
            string.Empty,
            0,
            false,
            false,
            false,
            false,
            0,
            false,
            false,
            "rejected-candidate",
            issue);

    public static void WriteReport(
        string path,
        IEnumerable<EaglStateGeometryProfileRow> rows)
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
                "geometry_layer",
                "geometry_primitive",
                "geometry_vertex_count",
                "geometry_triangle_count",
                "geometry_slot1_value_count",
                "geometry_command_count",
                "geometry_command_bytes",
                "geometry_command_shape_sha256",
                "state_layer",
                "state_primitive",
                "state_command_count",
                "state_command_bytes",
                "state_command_shape_sha256",
                "common_prefix_bytes",
                "same_layer",
                "same_primitive_index",
                "neighbor_primitive_index",
                "exact_command_shape",
                "state_profile_tie_count",
                "geometry_stream_complete",
                "state_stream_complete",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ObjectName),
            Field(row.ShapeName),
            row.GeometryLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.GeometryPrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.GeometryVertexCount.ToString(CultureInfo.InvariantCulture),
            row.GeometryTriangleCount.ToString(CultureInfo.InvariantCulture),
            row.GeometrySlot1ValueCount.ToString(CultureInfo.InvariantCulture),
            row.GeometryCommandCount.ToString(CultureInfo.InvariantCulture),
            row.GeometryCommandBytes.ToString(CultureInfo.InvariantCulture),
            row.GeometryCommandShapeSha256,
            row.StateLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.StatePrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.StateCommandCount.ToString(CultureInfo.InvariantCulture),
            row.StateCommandBytes.ToString(CultureInfo.InvariantCulture),
            row.StateCommandShapeSha256,
            row.CommonPrefixBytes.ToString(CultureInfo.InvariantCulture),
            row.SameLayer.ToString(),
            row.SamePrimitiveIndex.ToString(),
            row.NeighborPrimitiveIndex.ToString(),
            row.ExactCommandShape.ToString(),
            row.StateProfileTieCount.ToString(CultureInfo.InvariantCulture),
            row.GeometryStreamComplete.ToString(),
            row.StateStreamComplete.ToString(),
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

    private static string GetProfileKey(EaglModelPrimitive primitive) =>
        string.Join("|", new[]
        {
            GetCommandShapeSha256(primitive),
            primitive.RenderCommandStream.Commands.Count.ToString(CultureInfo.InvariantCulture),
            GetRawCommandBytes(primitive).Length.ToString(CultureInfo.InvariantCulture),
            primitive.RenderCommandStream.IsComplete.ToString(),
        });

    private static string GetCommandShapeSha256(EaglModelPrimitive primitive)
    {
        var canonical = string.Join("|", primitive.RenderCommandStream.Commands
            .OrderBy(command => command.Index)
            .Select(command => string.Join(":", new[]
            {
                command.Index.ToString(CultureInfo.InvariantCulture),
                command.CommandId.ToString(CultureInfo.InvariantCulture),
                command.SizeWords.ToString(CultureInfo.InvariantCulture),
            })));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static byte[] GetRawCommandBytes(EaglModelPrimitive primitive)
    {
        using var stream = new MemoryStream();
        foreach (var command in primitive.RenderCommandStream.Commands.OrderBy(command => command.Index))
        {
            stream.Write(command.RawBytes, 0, command.RawBytes.Length);
        }

        return stream.ToArray();
    }

    private static int GetCommonPrefixLength(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var length = Math.Min(left.Length, right.Length);
        var index = 0;
        while (index < length && left[index] == right[index])
        {
            index++;
        }

        return index;
    }

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
