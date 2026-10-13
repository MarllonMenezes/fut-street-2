using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Compares candidate geometry primitives with state-bearing primitives using
/// only raw command bytes, observed primitive boundaries, command counts and
/// command IDs. The result is evidence about two record populations; it never
/// attaches SHAPENAME state to a mesh group or assigns command semantics.
/// </summary>
public sealed record EaglPrimitiveStateCorrespondenceRow(
    string ModelArchiveName,
    string ObjectName,
    int GeometryLayerIndex,
    uint GeometryPrimitiveIndex,
    int StateLayerIndex,
    uint StatePrimitiveIndex,
    string StateNames,
    int GeometryCommandCount,
    int StateCommandCount,
    int GeometryCommandBytes,
    int StateCommandBytes,
    int CommonPrefixBytes,
    bool ExactRawBytes,
    bool ExactBoundaryShape,
    bool GeometryStreamComplete,
    bool StateStreamComplete,
    int SharedCommandIdCount,
    string SharedCommandIds,
    int SharedRelocationSymbolCount,
    string SharedRelocationSymbols);

public static class EaglPrimitiveStateCorrespondence
{
    public static IReadOnlyList<EaglPrimitiveStateCorrespondenceRow> Compare(
        string modelArchiveName,
        string objectName,
        EaglCandidateMesh mesh,
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(primitives);

        var candidatePrimitiveIndices = mesh.Groups
            .Select(group => group.PrimitiveIndex)
            .Distinct()
            .ToHashSet();
        var geometryPrimitives = primitives
            .Where(primitive => candidatePrimitiveIndices.Contains(primitive.PrimitiveIndex))
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .ToArray();
        var statePrimitives = primitives
            .Where(primitive => primitive.StateAssignments.Any(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal)))
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .ToArray();

        var rows = new List<EaglPrimitiveStateCorrespondenceRow>(
            checked(geometryPrimitives.Length * statePrimitives.Length));
        foreach (var geometry in geometryPrimitives)
        {
            var geometryBytes = GetRawCommandBytes(geometry);
            foreach (var state in statePrimitives)
            {
                var stateBytes = GetRawCommandBytes(state);
                var sharedIds = GetSharedCommandIds(geometry, state);
                var sharedRelocations = GetSharedRelocationSymbols(geometry, state);
                rows.Add(new EaglPrimitiveStateCorrespondenceRow(
                    modelArchiveName,
                    objectName,
                    geometry.LayerIndex,
                    geometry.PrimitiveIndex,
                    state.LayerIndex,
                    state.PrimitiveIndex,
                    string.Join(";", state.StateAssignments
                        .Where(assignment => string.Equals(
                            assignment.Name,
                            "SHAPENAME",
                            StringComparison.Ordinal))
                        .Select(assignment => $"{assignment.Name}={assignment.Value}")),
                    geometry.RenderCommandStream.Commands.Count,
                    state.RenderCommandStream.Commands.Count,
                    geometryBytes.Length,
                    stateBytes.Length,
                    GetCommonPrefixLength(geometryBytes, stateBytes),
                    geometryBytes.AsSpan().SequenceEqual(stateBytes),
                    geometry.RenderCommandStream.Commands.Count == state.RenderCommandStream.Commands.Count &&
                    geometryBytes.Length == stateBytes.Length &&
                    geometry.RenderCommandStream.IsComplete == state.RenderCommandStream.IsComplete,
                    geometry.RenderCommandStream.IsComplete,
                    state.RenderCommandStream.IsComplete,
                    sharedIds.Count,
                    string.Join(",", sharedIds.Select(pair =>
                        $"{pair.CommandId.ToString(CultureInfo.InvariantCulture)}={pair.Count.ToString(CultureInfo.InvariantCulture)}")),
                    sharedRelocations.Count,
                    string.Join(",", sharedRelocations.Select(pair =>
                        $"{pair.Symbol}={pair.Count.ToString(CultureInfo.InvariantCulture)}"))));
            }
        }

        return rows;
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglPrimitiveStateCorrespondenceRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "object_name",
                "geometry_layer",
                "geometry_primitive",
                "state_layer",
                "state_primitive",
                "state_names",
                "geometry_command_count",
                "state_command_count",
                "geometry_command_bytes",
                "state_command_bytes",
                "common_prefix_bytes",
                "exact_raw_bytes",
                "exact_boundary_shape",
                "geometry_stream_complete",
                "state_stream_complete",
                "shared_command_id_count",
                "shared_command_ids",
                "shared_relocation_symbol_count",
                "shared_relocation_symbols",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.ModelArchiveName),
            Escape(row.ObjectName),
            row.GeometryLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.GeometryPrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.StateLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.StatePrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.StateNames),
            row.GeometryCommandCount.ToString(CultureInfo.InvariantCulture),
            row.StateCommandCount.ToString(CultureInfo.InvariantCulture),
            row.GeometryCommandBytes.ToString(CultureInfo.InvariantCulture),
            row.StateCommandBytes.ToString(CultureInfo.InvariantCulture),
            row.CommonPrefixBytes.ToString(CultureInfo.InvariantCulture),
            row.ExactRawBytes.ToString(),
            row.ExactBoundaryShape.ToString(),
            row.GeometryStreamComplete.ToString(),
            row.StateStreamComplete.ToString(),
            row.SharedCommandIdCount.ToString(CultureInfo.InvariantCulture),
            Escape(row.SharedCommandIds),
            row.SharedRelocationSymbolCount.ToString(CultureInfo.InvariantCulture),
            Escape(row.SharedRelocationSymbols),
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

    public static string GetPairSetSha256(IEnumerable<EaglPrimitiveStateCorrespondenceRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var canonical = string.Join("\n", rows.Select(row => string.Join("|", new[]
        {
            row.ModelArchiveName,
            row.ObjectName,
            row.GeometryLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.GeometryPrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.StateLayerIndex.ToString(CultureInfo.InvariantCulture),
            row.StatePrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.CommonPrefixBytes.ToString(CultureInfo.InvariantCulture),
            row.ExactRawBytes.ToString(),
            row.ExactBoundaryShape.ToString(),
            row.SharedCommandIds,
            row.SharedRelocationSymbols,
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

    private static IReadOnlyList<(ushort CommandId, int Count)> GetSharedCommandIds(
        EaglModelPrimitive geometry,
        EaglModelPrimitive state)
    {
        var geometryCounts = geometry.RenderCommandStream.Commands
            .GroupBy(command => command.CommandId)
            .ToDictionary(group => group.Key, group => group.Count());
        var stateCounts = state.RenderCommandStream.Commands
            .GroupBy(command => command.CommandId)
            .ToDictionary(group => group.Key, group => group.Count());
        return geometryCounts
            .Where(pair => stateCounts.ContainsKey(pair.Key))
            .OrderBy(pair => pair.Key)
            .Select(pair => (pair.Key, Math.Min(pair.Value, stateCounts[pair.Key])))
            .ToArray();
    }

    private static IReadOnlyList<(string Symbol, int Count)> GetSharedRelocationSymbols(
        EaglModelPrimitive geometry,
        EaglModelPrimitive state)
    {
        var geometrySymbols = geometry.RenderCommandStream.Commands
            .SelectMany(command => command.RelocationSymbols)
            .Select(ParseRelocationSymbol)
            .Where(symbol => symbol is not null)
            .Cast<string>()
            .GroupBy(symbol => symbol, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var stateSymbols = state.RenderCommandStream.Commands
            .SelectMany(command => command.RelocationSymbols)
            .Select(ParseRelocationSymbol)
            .Where(symbol => symbol is not null)
            .Cast<string>()
            .GroupBy(symbol => symbol, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        return geometrySymbols
            .Where(pair => stateSymbols.ContainsKey(pair.Key))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => (pair.Key, Math.Min(pair.Value, stateSymbols[pair.Key])))
            .ToArray();
    }

    private static string? ParseRelocationSymbol(string evidence)
    {
        var firstSeparator = evidence.IndexOf(':');
        var typeSeparator = evidence.LastIndexOf(":type=", StringComparison.Ordinal);
        if (firstSeparator < 0 || typeSeparator <= firstSeparator + 1)
        {
            return null;
        }

        return evidence[(firstSeparator + 1)..typeSeparator];
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
