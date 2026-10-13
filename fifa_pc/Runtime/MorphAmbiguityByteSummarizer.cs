using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// Joins the existing morph ambiguity labels with the existing byte-level
/// report by exact structural-layout key. All values remain report metrics;
/// no byte or token receives a semantic interpretation.
/// </summary>
public sealed record MorphAmbiguityByteReportRow(
    string LayoutKey,
    int CommandControlExactMatches,
    int CommandBoundaryExactMatches,
    int CommandStructuralSignatureExactMatches,
    int CompanionNumericMatchControlExactMatches,
    int CompanionNumericMatchSignatureExactMatches,
    int CompanionNumericMatchBoundaryExactMatches,
    int CompanionNumericMatchValueCategoryExactMatches,
    int CommandMinTotalDelta,
    int CommandMinAlignedDifference,
    int CommandMinLengthDeltaAbs,
    int CommandMaxCommonPrefix);

public sealed record MorphAmbiguityByteSummaryRow(
    string VariationFields,
    int AmbiguousLayouts,
    int AmbiguousRows,
    int ByteReportRows,
    int MissingByteReportRows,
    int CommandExactRows,
    int CommandBoundaryExactRows,
    int CommandStructuralExactRows,
    int NumericRecordExactRows,
    int NumericSignatureExactRows,
    int NumericBoundaryExactRows,
    int NumericValueCategoryExactRows,
    string CommandMinTotalDeltaRange,
    string CommandMinAlignedDifferenceRange,
    string CommandMinLengthDeltaRange,
    string CommandMaxCommonPrefixRange,
    string LayoutKeySha256s);

public static class MorphAmbiguityByteSummarizer
{
    private static readonly string[] RequiredColumns =
    {
        "morph_structural_layout",
        "command_control_exact_matches",
        "command_boundary_exact_matches",
        "command_structural_signature_exact_matches",
        "companion_numeric_match_control_exact_matches",
        "companion_numeric_match_signature_exact_matches",
        "companion_numeric_match_boundary_exact_matches",
        "companion_numeric_match_value_category_exact_matches",
        "command_min_total_delta",
        "command_min_aligned_difference",
        "command_min_length_delta_abs",
        "command_max_common_prefix",
    };

    public static IReadOnlyList<MorphAmbiguityByteReportRow> ReadReport(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph byte comparison report is empty.");
        }

        var header = lines[0].Split('\t');
        var indexes = RequiredColumns.ToDictionary(
            column => column,
            column => Array.FindIndex(header, value =>
                string.Equals(value, column, StringComparison.Ordinal)),
            StringComparer.Ordinal);
        if (indexes.Any(pair => pair.Value < 0))
        {
            var missing = string.Join(",", indexes
                .Where(pair => pair.Value < 0)
                .Select(pair => pair.Key));
            throw new InvalidDataException($"Morph byte comparison report is missing columns: {missing}.");
        }

        var rows = new List<MorphAmbiguityByteReportRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphAmbiguityByteReportRow(
                ReadField(fields, indexes["morph_structural_layout"], lineIndex),
                ReadInt(fields, indexes["command_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["command_boundary_exact_matches"], lineIndex),
                ReadInt(fields, indexes["command_structural_signature_exact_matches"], lineIndex),
                ReadInt(fields, indexes["companion_numeric_match_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["companion_numeric_match_signature_exact_matches"], lineIndex),
                ReadInt(fields, indexes["companion_numeric_match_boundary_exact_matches"], lineIndex),
                ReadInt(fields, indexes["companion_numeric_match_value_category_exact_matches"], lineIndex),
                ReadInt(fields, indexes["command_min_total_delta"], lineIndex),
                ReadInt(fields, indexes["command_min_aligned_difference"], lineIndex),
                ReadInt(fields, indexes["command_min_length_delta_abs"], lineIndex),
                ReadInt(fields, indexes["command_max_common_prefix"], lineIndex)));
        }

        return rows;
    }

    public static IReadOnlyList<MorphAmbiguityByteSummaryRow> Summarize(
        IEnumerable<MorphAmbiguityReportRow> ambiguityRows,
        IEnumerable<MorphAmbiguityByteReportRow> byteRows)
    {
        ArgumentNullException.ThrowIfNull(ambiguityRows);
        ArgumentNullException.ThrowIfNull(byteRows);

        var bytesByLayout = byteRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        return ambiguityRows
            .Where(row => row.LayoutAmbiguous)
            .GroupBy(row => MorphAmbiguitySummarizer.NormalizeFieldList(row.VariationFields), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var joined = group
                    .SelectMany(row => bytesByLayout.TryGetValue(row.LayoutKey, out var matches)
                        ? matches
                        : Array.Empty<MorphAmbiguityByteReportRow>())
                    .ToArray();
                var ambiguousRows = group.Sum(row => row.LayoutRows);
                return new MorphAmbiguityByteSummaryRow(
                    group.Key,
                    group.Count(),
                    ambiguousRows,
                    joined.Length,
                    Math.Max(0, ambiguousRows - joined.Length),
                    joined.Count(row => row.CommandControlExactMatches > 0),
                    joined.Count(row => row.CommandBoundaryExactMatches > 0),
                    joined.Count(row => row.CommandStructuralSignatureExactMatches > 0),
                    joined.Count(row => row.CompanionNumericMatchControlExactMatches > 0),
                    joined.Count(row => row.CompanionNumericMatchSignatureExactMatches > 0),
                    joined.Count(row => row.CompanionNumericMatchBoundaryExactMatches > 0),
                    joined.Count(row => row.CompanionNumericMatchValueCategoryExactMatches > 0),
                    FormatRange(joined.Select(row => row.CommandMinTotalDelta)),
                    FormatRange(joined.Select(row => row.CommandMinAlignedDifference)),
                    FormatRange(joined.Select(row => row.CommandMinLengthDeltaAbs)),
                    FormatRange(joined.Select(row => row.CommandMaxCommonPrefix)),
                    string.Join(",", group
                        .Select(row => MorphAmbiguitySummarizer.GetLayoutKeySha256(row.LayoutKey))
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(hash => hash, StringComparer.Ordinal)));
            })
            .ToArray();
    }

    private static string FormatRange(IEnumerable<int> values)
    {
        var materialized = values.ToArray();
        return materialized.Length == 0
            ? "none"
            : $"{materialized.Min().ToString(CultureInfo.InvariantCulture)}..{materialized.Max().ToString(CultureInfo.InvariantCulture)}";
    }

    private static string ReadField(string[] fields, int index, int lineIndex)
    {
        if (index < 0 || index >= fields.Length)
        {
            throw new InvalidDataException($"Morph byte comparison row at line {lineIndex + 1} has too few columns.");
        }

        return fields[index];
    }

    private static int ReadInt(string[] fields, int index, int lineIndex)
    {
        var value = ReadField(fields, index, lineIndex);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Invalid integer in morph byte comparison row at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }
}
