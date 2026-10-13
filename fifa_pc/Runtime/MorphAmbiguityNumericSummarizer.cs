using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// Joins the existing numeric-gap report to ambiguity compositions by the
/// exact structural-layout key. The values are kept as comparison metrics and
/// boundary observations; no numeric field is given a runtime meaning.
/// </summary>
public sealed record MorphAmbiguityNumericGapRow(
    string LayoutKey,
    int BoundaryControlExactMatches,
    int ValueCategoryControlExactMatches,
    int PayloadWindowControlExactMatches,
    int CanonicalOffsetControlExactMatches,
    int RelativeBoundaryControlExactMatches,
    int CanonicalWordDifferences,
    int CanonicalOffsetDifferences,
    int PayloadWindowHashDifferences,
    int PayloadWindowHashEqual,
    int PayloadWindowHashCommonPrefix);

public sealed record MorphAmbiguityNumericSummaryRow(
    string VariationFields,
    int AmbiguousLayouts,
    int GapRows,
    int DistinctGapLayouts,
    int MissingGapLayouts,
    int BoundaryExactRows,
    int RelativeBoundaryExactRows,
    int ValueCategoryExactRows,
    int PayloadWindowExactRows,
    int CanonicalOffsetExactRows,
    int PayloadWindowZeroDifferenceRows,
    string CanonicalWordDifferenceRange,
    string CanonicalOffsetDifferenceRange,
    string PayloadWindowDifferenceRange,
    string PayloadWindowEqualRange,
    string PayloadWindowCommonPrefixRange,
    string LayoutKeySha256s);

public static class MorphAmbiguityNumericSummarizer
{
    private static readonly string[] RequiredColumns =
    {
        "morph_structural_layout",
        "boundary_control_exact_matches",
        "value_category_control_exact_matches",
        "payload_window_control_exact_matches",
        "canonical_offset_control_exact_matches",
        "relative_boundary_control_exact_matches",
        "nearest_control_canonical_word_differences",
        "nearest_control_canonical_offset_differences",
        "nearest_control_payload_window_hash_differences",
        "nearest_control_payload_window_hash_equal",
        "nearest_control_payload_window_hash_common_prefix",
    };

    public static IReadOnlyList<MorphAmbiguityNumericGapRow> ReadReport(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph numeric-gap report is empty.");
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
            throw new InvalidDataException($"Morph numeric-gap report is missing columns: {missing}.");
        }

        var rows = new List<MorphAmbiguityNumericGapRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphAmbiguityNumericGapRow(
                ReadField(fields, indexes["morph_structural_layout"], lineIndex),
                ReadInt(fields, indexes["boundary_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["value_category_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["payload_window_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["canonical_offset_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["relative_boundary_control_exact_matches"], lineIndex),
                ReadInt(fields, indexes["nearest_control_canonical_word_differences"], lineIndex),
                ReadInt(fields, indexes["nearest_control_canonical_offset_differences"], lineIndex),
                ReadInt(fields, indexes["nearest_control_payload_window_hash_differences"], lineIndex),
                ReadInt(fields, indexes["nearest_control_payload_window_hash_equal"], lineIndex),
                ReadInt(fields, indexes["nearest_control_payload_window_hash_common_prefix"], lineIndex)));
        }

        return rows;
    }

    public static IReadOnlyList<MorphAmbiguityNumericSummaryRow> Summarize(
        IEnumerable<MorphAmbiguityReportRow> ambiguityRows,
        IEnumerable<MorphAmbiguityNumericGapRow> gapRows)
    {
        ArgumentNullException.ThrowIfNull(ambiguityRows);
        ArgumentNullException.ThrowIfNull(gapRows);

        var gapsByLayout = gapRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        return ambiguityRows
            .Where(row => row.LayoutAmbiguous)
            .GroupBy(row => MorphAmbiguitySummarizer.NormalizeFieldList(row.VariationFields), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var joined = group
                    .SelectMany(row => gapsByLayout.TryGetValue(row.LayoutKey, out var matches)
                        ? matches
                        : Array.Empty<MorphAmbiguityNumericGapRow>())
                    .ToArray();
                var expectedRows = joined
                    .Select(row => row.LayoutKey)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                return new MorphAmbiguityNumericSummaryRow(
                    group.Key,
                    group.Count(),
                    joined.Length,
                    expectedRows,
                    group.Count() - expectedRows,
                    joined.Count(row => row.BoundaryControlExactMatches > 0),
                    joined.Count(row => row.RelativeBoundaryControlExactMatches > 0),
                    joined.Count(row => row.ValueCategoryControlExactMatches > 0),
                    joined.Count(row => row.PayloadWindowControlExactMatches > 0),
                    joined.Count(row => row.CanonicalOffsetControlExactMatches > 0),
                    joined.Count(row => row.PayloadWindowHashDifferences == 0),
                    FormatRange(joined.Select(row => row.CanonicalWordDifferences)),
                    FormatRange(joined.Select(row => row.CanonicalOffsetDifferences)),
                    FormatRange(joined.Select(row => row.PayloadWindowHashDifferences)),
                    FormatRange(joined.Select(row => row.PayloadWindowHashEqual)),
                    FormatRange(joined.Select(row => row.PayloadWindowHashCommonPrefix)),
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
            throw new InvalidDataException($"Morph numeric-gap row at line {lineIndex + 1} has too few columns.");
        }

        return fields[index];
    }

    private static int ReadInt(string[] fields, int index, int lineIndex)
    {
        var value = ReadField(fields, index, lineIndex);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Invalid integer in morph numeric-gap row at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }
}
