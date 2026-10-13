using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Joins the existing ambiguity, byte-level, and numeric-gap reports by their
/// exact structural-layout key. This is a report of observed variation and
/// boundary metadata only; it does not assign meaning to any field.
/// </summary>
public sealed record MorphAmbiguityGroupMetadataRow(
    string LayoutKey,
    string LayoutKeySha256,
    int LayoutRows,
    string VariationFields,
    string StableControlOverlapFields,
    int PairedStructuralProfiles,
    int PayloadWindowProfiles,
    int CompanionLayoutProfiles,
    int CommandStreamProfiles,
    int VertexRecordProfiles,
    int NumericDescriptorProfiles,
    int CandidateMeshProfiles,
    int CompanionNumericMatchProfiles,
    bool LayoutStable,
    bool LayoutAmbiguous,
    int ByteReportRows,
    int CommandExactRows,
    int CommandBoundaryExactRows,
    int CommandStructuralExactRows,
    int NumericValueCategoryExactRows,
    int NumericGapRows,
    int NumericBoundaryExactRows,
    int RelativeBoundaryExactRows,
    int NumericPayloadWindowExactRows,
    int NumericCanonicalOffsetExactRows,
    int NumericPayloadWindowZeroDifferenceRows);

public static class MorphAmbiguityGroupSummarizer
{
    private sealed record AmbiguityMetadata(
        string LayoutKey,
        int LayoutRows,
        string VariationFields,
        string StableControlOverlapFields,
        int PairedStructuralProfiles,
        int PayloadWindowProfiles,
        int CompanionLayoutProfiles,
        int CommandStreamProfiles,
        int VertexRecordProfiles,
        int NumericDescriptorProfiles,
        int CandidateMeshProfiles,
        int CompanionNumericMatchProfiles,
        bool LayoutStable,
        bool LayoutAmbiguous);

    public static IReadOnlyList<MorphAmbiguityGroupMetadataRow> Summarize(
        IEnumerable<MorphAmbiguityReportRow> ambiguityRows,
        IEnumerable<MorphAmbiguityByteReportRow> byteRows,
        IEnumerable<MorphAmbiguityNumericGapRow> numericRows)
    {
        ArgumentNullException.ThrowIfNull(ambiguityRows);
        ArgumentNullException.ThrowIfNull(byteRows);
        ArgumentNullException.ThrowIfNull(numericRows);

        var ambiguityByLayout = ambiguityRows
            .Where(row => row.LayoutAmbiguous)
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .Select(group =>
            {
                var rows = group.ToArray();
                var first = rows[0];
                if (rows.Any(row =>
                        row.LayoutRows != first.LayoutRows ||
                        !string.Equals(row.VariationFields, first.VariationFields, StringComparison.Ordinal) ||
                        !string.Equals(row.StableControlOverlapFields, first.StableControlOverlapFields, StringComparison.Ordinal)))
                {
                    throw new InvalidDataException(
                        $"Ambiguity report has inconsistent metadata for layout: {group.Key}");
                }

                return new AmbiguityMetadata(
                    first.LayoutKey,
                    first.LayoutRows,
                    MorphAmbiguitySummarizer.NormalizeFieldList(first.VariationFields),
                    MorphAmbiguitySummarizer.NormalizeFieldList(first.StableControlOverlapFields),
                    first.PairedStructuralProfiles,
                    first.PayloadWindowProfiles,
                    first.CompanionLayoutProfiles,
                    first.CommandStreamProfiles,
                    first.VertexRecordProfiles,
                    first.NumericDescriptorProfiles,
                    first.CandidateMeshProfiles,
                    first.CompanionNumericMatchProfiles,
                    first.LayoutStable,
                    first.LayoutAmbiguous);
            })
            .OrderBy(row => row.VariationFields, StringComparer.Ordinal)
            .ThenBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToArray();

        var bytesByLayout = byteRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var numericByLayout = numericRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        return ambiguityByLayout
            .Select(ambiguity =>
            {
                var byteMatches = bytesByLayout.TryGetValue(ambiguity.LayoutKey, out var matchedBytes)
                    ? matchedBytes
                    : Array.Empty<MorphAmbiguityByteReportRow>();
                var numericMatches = numericByLayout.TryGetValue(ambiguity.LayoutKey, out var matchedNumeric)
                    ? matchedNumeric
                    : Array.Empty<MorphAmbiguityNumericGapRow>();
                return new MorphAmbiguityGroupMetadataRow(
                    ambiguity.LayoutKey,
                    Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ambiguity.LayoutKey))),
                    ambiguity.LayoutRows,
                    ambiguity.VariationFields,
                    ambiguity.StableControlOverlapFields,
                    ambiguity.PairedStructuralProfiles,
                    ambiguity.PayloadWindowProfiles,
                    ambiguity.CompanionLayoutProfiles,
                    ambiguity.CommandStreamProfiles,
                    ambiguity.VertexRecordProfiles,
                    ambiguity.NumericDescriptorProfiles,
                    ambiguity.CandidateMeshProfiles,
                    ambiguity.CompanionNumericMatchProfiles,
                    ambiguity.LayoutStable,
                    ambiguity.LayoutAmbiguous,
                    byteMatches.Length,
                    byteMatches.Count(row => row.CommandControlExactMatches > 0),
                    byteMatches.Count(row => row.CommandBoundaryExactMatches > 0),
                    byteMatches.Count(row => row.CommandStructuralSignatureExactMatches > 0),
                    byteMatches.Count(row => row.CompanionNumericMatchValueCategoryExactMatches > 0),
                    numericMatches.Length,
                    numericMatches.Count(row => row.BoundaryControlExactMatches > 0),
                    numericMatches.Count(row => row.RelativeBoundaryControlExactMatches > 0),
                    numericMatches.Count(row => row.PayloadWindowControlExactMatches > 0),
                    numericMatches.Count(row => row.CanonicalOffsetControlExactMatches > 0),
                    numericMatches.Count(row => row.PayloadWindowHashDifferences == 0));
            })
            .ToArray();
    }

    public static void WriteReport(
        string path,
        IEnumerable<MorphAmbiguityGroupMetadataRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "morph_structural_layout",
                "layout_key_sha256",
                "layout_rows",
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
                "layout_ambiguous",
                "byte_report_rows",
                "command_exact_rows",
                "command_boundary_exact_rows",
                "command_structural_exact_rows",
                "numeric_value_category_exact_rows",
                "numeric_gap_rows",
                "numeric_boundary_exact_rows",
                "relative_boundary_exact_rows",
                "numeric_payload_window_exact_rows",
                "numeric_canonical_offset_exact_rows",
                "numeric_payload_window_zero_difference_rows",
            }),
        };

        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.LayoutKey),
            row.LayoutKeySha256,
            row.LayoutRows.ToString(CultureInfo.InvariantCulture),
            Escape(row.VariationFields),
            Escape(row.StableControlOverlapFields),
            row.PairedStructuralProfiles.ToString(CultureInfo.InvariantCulture),
            row.PayloadWindowProfiles.ToString(CultureInfo.InvariantCulture),
            row.CompanionLayoutProfiles.ToString(CultureInfo.InvariantCulture),
            row.CommandStreamProfiles.ToString(CultureInfo.InvariantCulture),
            row.VertexRecordProfiles.ToString(CultureInfo.InvariantCulture),
            row.NumericDescriptorProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshProfiles.ToString(CultureInfo.InvariantCulture),
            row.CompanionNumericMatchProfiles.ToString(CultureInfo.InvariantCulture),
            row.LayoutStable.ToString(),
            row.LayoutAmbiguous.ToString(),
            row.ByteReportRows.ToString(CultureInfo.InvariantCulture),
            row.CommandExactRows.ToString(CultureInfo.InvariantCulture),
            row.CommandBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
            row.CommandStructuralExactRows.ToString(CultureInfo.InvariantCulture),
            row.NumericValueCategoryExactRows.ToString(CultureInfo.InvariantCulture),
            row.NumericGapRows.ToString(CultureInfo.InvariantCulture),
            row.NumericBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
            row.RelativeBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
            row.NumericPayloadWindowExactRows.ToString(CultureInfo.InvariantCulture),
            row.NumericCanonicalOffsetExactRows.ToString(CultureInfo.InvariantCulture),
            row.NumericPayloadWindowZeroDifferenceRows.ToString(CultureInfo.InvariantCulture),
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

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
