using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Aggregates only the already observed ambiguity labels from the morph
/// structural report. It does not infer a field meaning or a binding.
/// </summary>
public sealed record MorphAmbiguityReportRow(
    string LayoutKey,
    int LayoutRows,
    bool LayoutAmbiguous,
    string VariationFields,
    string StableControlOverlapFields,
    int PairedStructuralProfiles = 0,
    int PayloadWindowProfiles = 0,
    int CompanionLayoutProfiles = 0,
    int CommandStreamProfiles = 0,
    int VertexRecordProfiles = 0,
    int NumericDescriptorProfiles = 0,
    int CandidateMeshProfiles = 0,
    int CompanionNumericMatchProfiles = 0,
    bool LayoutStable = false);

public sealed record MorphAmbiguitySummaryRow(
    string VariationFields,
    int AmbiguousLayouts,
    int AmbiguousRows,
    string StableControlOverlapLayoutCounts,
    string LayoutKeySha256s);

public static class MorphAmbiguitySummarizer
{
    private static readonly string[] RequiredColumns =
    {
        "morph_structural_layout",
        "layout_rows",
        "layout_ambiguous",
        "variation_fields",
        "stable_control_overlap_fields",
        "paired_structural_profiles",
        "payload_window_profiles",
        "companion_layout_profiles",
        "command_stream_profiles",
        "vertex_record_profiles",
        "numeric_descriptor_profiles",
        "candidate_mesh_profiles",
        "companion_numeric_match_profiles",
        "layout_stable",
    };

    public static IReadOnlyList<MorphAmbiguityReportRow> ReadReport(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph ambiguity report is empty.");
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
            throw new InvalidDataException($"Morph ambiguity report is missing columns: {missing}.");
        }

        var rows = new List<MorphAmbiguityReportRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            var layoutKey = ReadField(fields, indexes["morph_structural_layout"], lineIndex);
            var layoutRowsText = ReadField(fields, indexes["layout_rows"], lineIndex);
            var layoutAmbiguousText = ReadField(fields, indexes["layout_ambiguous"], lineIndex);
            if (!int.TryParse(layoutRowsText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var layoutRows) ||
                !bool.TryParse(layoutAmbiguousText, out var layoutAmbiguous))
            {
                throw new InvalidDataException($"Invalid morph ambiguity row at line {lineIndex + 1}.");
            }

            rows.Add(new MorphAmbiguityReportRow(
                layoutKey,
                layoutRows,
                layoutAmbiguous,
                ReadField(fields, indexes["variation_fields"], lineIndex),
                ReadField(fields, indexes["stable_control_overlap_fields"], lineIndex),
                ReadInt(fields, indexes["paired_structural_profiles"], lineIndex),
                ReadInt(fields, indexes["payload_window_profiles"], lineIndex),
                ReadInt(fields, indexes["companion_layout_profiles"], lineIndex),
                ReadInt(fields, indexes["command_stream_profiles"], lineIndex),
                ReadInt(fields, indexes["vertex_record_profiles"], lineIndex),
                ReadInt(fields, indexes["numeric_descriptor_profiles"], lineIndex),
                ReadInt(fields, indexes["candidate_mesh_profiles"], lineIndex),
                ReadInt(fields, indexes["companion_numeric_match_profiles"], lineIndex),
                ReadBool(fields, indexes["layout_stable"], lineIndex)));
        }

        return rows;
    }

    public static IReadOnlyList<MorphAmbiguitySummaryRow> Summarize(
        IEnumerable<MorphAmbiguityReportRow> reportRows)
    {
        ArgumentNullException.ThrowIfNull(reportRows);

        return reportRows
            .Where(row => row.LayoutAmbiguous)
            .GroupBy(row => NormalizeFieldList(row.VariationFields), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var overlapCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var row in group)
                {
                    foreach (var field in ParseFields(row.StableControlOverlapFields))
                    {
                        overlapCounts[field] = overlapCounts.GetValueOrDefault(field) + 1;
                    }
                }

                var layoutHashes = group
                    .Select(row => Convert.ToHexString(
                        SHA256.HashData(Encoding.UTF8.GetBytes(row.LayoutKey))))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(hash => hash, StringComparer.Ordinal);

                return new MorphAmbiguitySummaryRow(
                    group.Key,
                    group.Count(),
                    group.Sum(row => row.LayoutRows),
                    string.Join(",", overlapCounts
                        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .Select(pair => $"{pair.Key}={pair.Value.ToString(CultureInfo.InvariantCulture)}")),
                    string.Join(",", layoutHashes));
            })
            .ToArray();
    }

    public static string NormalizeFieldList(string value)
    {
        var fields = ParseFields(value).ToArray();
        return fields.Length == 0 ? "none" : string.Join(",", fields);
    }

    private static IEnumerable<string> ParseFields(string value)
    {
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(field => field, StringComparer.Ordinal);
    }

    public static string GetLayoutKeySha256(string layoutKey) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(layoutKey)));

    private static string ReadField(string[] fields, int index, int lineIndex)
    {
        if (index < 0 || index >= fields.Length)
        {
            throw new InvalidDataException($"Morph ambiguity row at line {lineIndex + 1} has too few columns.");
        }

        return fields[index];
    }

    private static int ReadInt(string[] fields, int index, int lineIndex)
    {
        var value = ReadField(fields, index, lineIndex);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Invalid integer in morph ambiguity row at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }

    private static bool ReadBool(string[] fields, int index, int lineIndex)
    {
        var value = ReadField(fields, index, lineIndex);
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidDataException($"Invalid boolean in morph ambiguity row at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }
}
