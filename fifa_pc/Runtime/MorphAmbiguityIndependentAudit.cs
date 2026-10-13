using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Re-groups the raw morph structural catalog independently from the existing
/// ambiguity report. The comparison is limited to observed signatures and
/// boolean validation flags; it does not assign meaning to any field.
/// </summary>
public sealed record MorphAmbiguityIndependentCatalogRow(
    string NestedBig,
    string MorphObject,
    string OrdSibling,
    bool HasCompleteSibling,
    bool SiblingElfValidated,
    bool SiblingBounds,
    string LayoutKey,
    string PayloadWindowSignature,
    string CompanionLayoutSignature,
    string CommandStreamSignature,
    string VertexRecordSignature,
    string NumericDescriptorSignature,
    string CandidateMeshSignature,
    string CompanionNumericMatchSignature,
    string PairedStructuralSignature);

public sealed record MorphAmbiguityIndependentAuditRow(
    string LayoutKey,
    string LayoutKeySha256,
    int CatalogRows,
    int ReportRows,
    int ReportLayoutRows,
    bool ReportRowCountMatches,
    bool LayoutAmbiguous,
    bool LayoutStable,
    bool MeasuredAmbiguous,
    bool AmbiguityLabelMatchesMeasured,
    string VariationFields,
    int PairedStructuralProfilesMeasured,
    int PairedStructuralProfilesReported,
    int PayloadWindowProfilesMeasured,
    int PayloadWindowProfilesReported,
    int CompanionLayoutProfilesMeasured,
    int CompanionLayoutProfilesReported,
    int CommandStreamProfilesMeasured,
    int CommandStreamProfilesReported,
    int VertexRecordProfilesMeasured,
    int VertexRecordProfilesReported,
    int NumericDescriptorProfilesMeasured,
    int NumericDescriptorProfilesReported,
    int CandidateMeshProfilesMeasured,
    int CandidateMeshProfilesReported,
    int CompanionNumericMatchProfilesMeasured,
    int CompanionNumericMatchProfilesReported,
    int IndependentVariableLayerCount,
    bool CandidateMeshStableCompanionVariable,
    bool CandidateMeshVariable,
    bool AnyIndependentLayerVariable,
    bool AllCompleteSiblingRows,
    bool AllElfValidatedRows,
    bool AllBoundsRows,
    string CandidateMeshSetSha256,
    string CompanionLayoutSetSha256,
    string SampleMembers,
    bool ReportMetadataMatchesMeasuredProfiles);

public sealed record MorphAmbiguityIndependentAuditSummaryRow(
    string Scope,
    int Layouts,
    int CatalogRows,
    int AmbiguousLayouts,
    int StableLayouts,
    int MetadataMismatches,
    int ReportRowCountMismatches,
    int AmbiguityLabelMismatches,
    int CandidateMeshStableCompanionVariableLayouts,
    int CandidateMeshVariableLayouts,
    int AnyIndependentLayerVariableLayouts,
    int AllIndependentLayersStableLayouts,
    int AllCompleteSiblingLayouts,
    int AllElfValidatedLayouts,
    int AllBoundsLayouts,
    string CandidateMeshProfileCountDistribution,
    string IndependentVariableLayerCountDistribution);

public static class MorphAmbiguityIndependentAudit
{
    private static readonly string[] CatalogRequiredColumns =
    {
        "nested_big",
        "morph_object",
        "ord_sibling",
        "has_complete_sibling",
        "sibling_elf_validated",
        "sibling_bounds",
        "morph_structural_layout",
        "payload_window_signature",
        "companion_structural_layout",
        "command_stream_signature",
        "vertex_record_signature",
        "numeric_descriptor_signature",
        "candidate_mesh_signature",
        "companion_numeric_match_signature",
        "paired_structural_signature",
    };

    public static IReadOnlyList<MorphAmbiguityIndependentCatalogRow> ReadCatalog(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph structural catalog is empty.");
        }

        var indexes = ReadIndexes(lines[0], CatalogRequiredColumns, "morph structural catalog");
        var rows = new List<MorphAmbiguityIndependentCatalogRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphAmbiguityIndependentCatalogRow(
                ReadField(fields, indexes["nested_big"], lineIndex, "catalog"),
                ReadField(fields, indexes["morph_object"], lineIndex, "catalog"),
                ReadField(fields, indexes["ord_sibling"], lineIndex, "catalog"),
                ReadBool(fields, indexes["has_complete_sibling"], lineIndex, "catalog"),
                ReadBool(fields, indexes["sibling_elf_validated"], lineIndex, "catalog"),
                ReadBool(fields, indexes["sibling_bounds"], lineIndex, "catalog"),
                ReadField(fields, indexes["morph_structural_layout"], lineIndex, "catalog"),
                ReadField(fields, indexes["payload_window_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["companion_structural_layout"], lineIndex, "catalog"),
                ReadField(fields, indexes["command_stream_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["vertex_record_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["numeric_descriptor_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["candidate_mesh_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["companion_numeric_match_signature"], lineIndex, "catalog"),
                ReadField(fields, indexes["paired_structural_signature"], lineIndex, "catalog")));
        }

        return rows;
    }

    public static IReadOnlyList<MorphAmbiguityIndependentAuditRow> Audit(
        IEnumerable<MorphAmbiguityIndependentCatalogRow> catalogRows,
        IEnumerable<MorphAmbiguityReportRow> reportRows)
    {
        ArgumentNullException.ThrowIfNull(catalogRows);
        ArgumentNullException.ThrowIfNull(reportRows);

        var catalogByLayout = catalogRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var reportByLayout = reportRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);

        var keys = catalogByLayout.Keys
            .Concat(reportByLayout.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);

        return keys.Select(layoutKey =>
        {
            var catalog = catalogByLayout.TryGetValue(layoutKey, out var matchedCatalog)
                ? matchedCatalog
                : Array.Empty<MorphAmbiguityIndependentCatalogRow>();
            var reports = reportByLayout.TryGetValue(layoutKey, out var matchedReports)
                ? matchedReports
                : Array.Empty<MorphAmbiguityReportRow>();
            var firstReport = reports.FirstOrDefault();
            var measured = Measure(catalog);
            var reportRowCountMatches = reports.Length == 1 &&
                                        firstReport is not null &&
                                        firstReport.LayoutRows == catalog.Length;
            var reportMetadataMatches = reports.Length == 1 &&
                                         firstReport is not null &&
                                         firstReport.PairedStructuralProfiles == measured.PairedStructuralProfiles &&
                                         firstReport.PayloadWindowProfiles == measured.PayloadWindowProfiles &&
                                         firstReport.CompanionLayoutProfiles == measured.CompanionLayoutProfiles &&
                                         firstReport.CommandStreamProfiles == measured.CommandStreamProfiles &&
                                         firstReport.VertexRecordProfiles == measured.VertexRecordProfiles &&
                                         firstReport.NumericDescriptorProfiles == measured.NumericDescriptorProfiles &&
                                         firstReport.CandidateMeshProfiles == measured.CandidateMeshProfiles &&
                                         firstReport.CompanionNumericMatchProfiles == measured.CompanionNumericMatchProfiles;
            var profileCounts = new[]
            {
                measured.PairedStructuralProfiles,
                measured.PayloadWindowProfiles,
                measured.CompanionLayoutProfiles,
                measured.CommandStreamProfiles,
                measured.VertexRecordProfiles,
                measured.NumericDescriptorProfiles,
                measured.CandidateMeshProfiles,
                measured.CompanionNumericMatchProfiles,
            };
            var measuredAmbiguous = profileCounts.Any(value => value > 1);
            var measuredStable = profileCounts.All(value => value == 1);
            var ambiguityLabelMatchesMeasured = firstReport is not null &&
                                                firstReport.LayoutAmbiguous == measuredAmbiguous &&
                                                firstReport.LayoutStable == measuredStable;

            return new MorphAmbiguityIndependentAuditRow(
                layoutKey,
                Sha256(layoutKey),
                catalog.Length,
                reports.Length,
                firstReport?.LayoutRows ?? 0,
                reportRowCountMatches,
                firstReport?.LayoutAmbiguous ?? false,
                firstReport?.LayoutStable ?? false,
                measuredAmbiguous,
                ambiguityLabelMatchesMeasured,
                firstReport is null
                    ? string.Empty
                    : MorphAmbiguitySummarizer.NormalizeFieldList(firstReport.VariationFields),
                measured.PairedStructuralProfiles,
                firstReport?.PairedStructuralProfiles ?? 0,
                measured.PayloadWindowProfiles,
                firstReport?.PayloadWindowProfiles ?? 0,
                measured.CompanionLayoutProfiles,
                firstReport?.CompanionLayoutProfiles ?? 0,
                measured.CommandStreamProfiles,
                firstReport?.CommandStreamProfiles ?? 0,
                measured.VertexRecordProfiles,
                firstReport?.VertexRecordProfiles ?? 0,
                measured.NumericDescriptorProfiles,
                firstReport?.NumericDescriptorProfiles ?? 0,
                measured.CandidateMeshProfiles,
                firstReport?.CandidateMeshProfiles ?? 0,
                measured.CompanionNumericMatchProfiles,
                firstReport?.CompanionNumericMatchProfiles ?? 0,
                profileCounts.Count(value => value > 1),
                measured.CandidateMeshProfiles == 1 && measured.CompanionLayoutProfiles > 1,
                measured.CandidateMeshProfiles > 1,
                profileCounts.Any(value => value > 1),
                catalog.Length > 0 && catalog.All(row => row.HasCompleteSibling),
                catalog.Length > 0 && catalog.All(row => row.SiblingElfValidated),
                catalog.Length > 0 && catalog.All(row => row.SiblingBounds),
                SetSha256(catalog.Select(row => row.CandidateMeshSignature)),
                SetSha256(catalog.Select(row => row.CompanionLayoutSignature)),
                string.Join(",", catalog
                    .Take(3)
                    .Select(row => $"{row.NestedBig}/{row.MorphObject}")),
                reportMetadataMatches);
        }).ToArray();
    }

    public static IReadOnlyList<MorphAmbiguityIndependentAuditSummaryRow> Summarize(
        IEnumerable<MorphAmbiguityIndependentAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var all = rows.ToArray();
        return new[]
        {
            CreateSummary("all", all),
            CreateSummary("ambiguous", all.Where(row => row.LayoutAmbiguous).ToArray()),
            CreateSummary("stable", all.Where(row => !row.LayoutAmbiguous).ToArray()),
        };
    }

    public static void WriteReports(
        string auditPath,
        string summaryPath,
        IEnumerable<MorphAmbiguityIndependentAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);

        var materialized = rows.ToArray();
        WriteLines(auditPath, new[]
        {
            "morph_structural_layout",
            "layout_key_sha256",
            "catalog_rows",
            "report_rows",
            "report_layout_rows",
            "report_row_count_matches",
            "layout_ambiguous",
            "layout_stable",
            "measured_ambiguous",
            "ambiguity_label_matches_measured",
            "variation_fields",
            "paired_structural_profiles_measured",
            "paired_structural_profiles_reported",
            "payload_window_profiles_measured",
            "payload_window_profiles_reported",
            "companion_layout_profiles_measured",
            "companion_layout_profiles_reported",
            "command_stream_profiles_measured",
            "command_stream_profiles_reported",
            "vertex_record_profiles_measured",
            "vertex_record_profiles_reported",
            "numeric_descriptor_profiles_measured",
            "numeric_descriptor_profiles_reported",
            "candidate_mesh_profiles_measured",
            "candidate_mesh_profiles_reported",
            "companion_numeric_match_profiles_measured",
            "companion_numeric_match_profiles_reported",
            "independent_variable_layer_count",
            "candidate_mesh_stable_companion_variable",
            "candidate_mesh_variable",
            "any_independent_layer_variable",
            "all_complete_sibling_rows",
            "all_elf_validated_rows",
            "all_bounds_rows",
            "candidate_mesh_set_sha256",
            "companion_layout_set_sha256",
            "sample_members",
            "report_metadata_matches_measured_profiles",
        }, materialized.Select(ToFields));

        WriteLines(summaryPath, new[]
        {
            "scope",
            "layouts",
            "catalog_rows",
            "ambiguous_layouts",
            "stable_layouts",
            "metadata_mismatches",
            "report_row_count_mismatches",
            "ambiguity_label_mismatches",
            "candidate_mesh_stable_companion_variable_layouts",
            "candidate_mesh_variable_layouts",
            "any_independent_layer_variable_layouts",
            "all_independent_layers_stable_layouts",
            "all_complete_sibling_layouts",
            "all_elf_validated_layouts",
            "all_bounds_layouts",
            "candidate_mesh_profile_count_distribution",
            "independent_variable_layer_count_distribution",
        }, Summarize(materialized).Select(ToFields));
    }

    private static (int PairedStructuralProfiles,
        int PayloadWindowProfiles,
        int CompanionLayoutProfiles,
        int CommandStreamProfiles,
        int VertexRecordProfiles,
        int NumericDescriptorProfiles,
        int CandidateMeshProfiles,
        int CompanionNumericMatchProfiles) Measure(
        IReadOnlyList<MorphAmbiguityIndependentCatalogRow> rows)
    {
        return (
            DistinctCount(rows.Select(row => row.PairedStructuralSignature)),
            DistinctCount(rows.Select(row => row.PayloadWindowSignature)),
            DistinctCount(rows.Select(row => row.CompanionLayoutSignature)),
            DistinctCount(rows.Select(row => row.CommandStreamSignature)),
            DistinctCount(rows.Select(row => row.VertexRecordSignature)),
            DistinctCount(rows.Select(row => row.NumericDescriptorSignature)),
            DistinctCount(rows.Select(row => row.CandidateMeshSignature)),
            DistinctCount(rows.Select(row => row.CompanionNumericMatchSignature)));
    }

    private static MorphAmbiguityIndependentAuditSummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphAmbiguityIndependentAuditRow> rows)
    {
        return new MorphAmbiguityIndependentAuditSummaryRow(
            scope,
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Count(row => row.LayoutAmbiguous),
            rows.Count(row => !row.LayoutAmbiguous),
            rows.Count(row => !row.ReportMetadataMatchesMeasuredProfiles),
            rows.Count(row => !row.ReportRowCountMatches),
            rows.Count(row => !row.AmbiguityLabelMatchesMeasured),
            rows.Count(row => row.LayoutAmbiguous && row.CandidateMeshStableCompanionVariable),
            rows.Count(row => row.LayoutAmbiguous && row.CandidateMeshVariable),
            rows.Count(row => row.LayoutAmbiguous && row.AnyIndependentLayerVariable),
            rows.Count(row => row.LayoutAmbiguous && row.IndependentVariableLayerCount == 0),
            rows.Count(row => row.AllCompleteSiblingRows),
            rows.Count(row => row.AllElfValidatedRows),
            rows.Count(row => row.AllBoundsRows),
            Distribution(rows.Select(row => row.CandidateMeshProfilesMeasured)),
            Distribution(rows.Select(row => row.IndependentVariableLayerCount)));
    }

    private static IEnumerable<string> ToFields(MorphAmbiguityIndependentAuditRow row)
    {
        return new[]
        {
            Escape(row.LayoutKey),
            row.LayoutKeySha256,
            row.CatalogRows.ToString(CultureInfo.InvariantCulture),
            row.ReportRows.ToString(CultureInfo.InvariantCulture),
            row.ReportLayoutRows.ToString(CultureInfo.InvariantCulture),
            row.ReportRowCountMatches.ToString(),
            row.LayoutAmbiguous.ToString(),
            row.LayoutStable.ToString(),
            row.MeasuredAmbiguous.ToString(),
            row.AmbiguityLabelMatchesMeasured.ToString(),
            Escape(row.VariationFields),
            row.PairedStructuralProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.PairedStructuralProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.PayloadWindowProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.PayloadWindowProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.CompanionLayoutProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.CompanionLayoutProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.CommandStreamProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.CommandStreamProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.VertexRecordProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.VertexRecordProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.NumericDescriptorProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.NumericDescriptorProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.CompanionNumericMatchProfilesMeasured.ToString(CultureInfo.InvariantCulture),
            row.CompanionNumericMatchProfilesReported.ToString(CultureInfo.InvariantCulture),
            row.IndependentVariableLayerCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshStableCompanionVariable.ToString(),
            row.CandidateMeshVariable.ToString(),
            row.AnyIndependentLayerVariable.ToString(),
            row.AllCompleteSiblingRows.ToString(),
            row.AllElfValidatedRows.ToString(),
            row.AllBoundsRows.ToString(),
            row.CandidateMeshSetSha256,
            row.CompanionLayoutSetSha256,
            Escape(row.SampleMembers),
            row.ReportMetadataMatchesMeasuredProfiles.ToString(),
        };
    }

    private static IEnumerable<string> ToFields(MorphAmbiguityIndependentAuditSummaryRow row)
    {
        return new[]
        {
            row.Scope,
            row.Layouts.ToString(CultureInfo.InvariantCulture),
            row.CatalogRows.ToString(CultureInfo.InvariantCulture),
            row.AmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
            row.StableLayouts.ToString(CultureInfo.InvariantCulture),
            row.MetadataMismatches.ToString(CultureInfo.InvariantCulture),
            row.ReportRowCountMismatches.ToString(CultureInfo.InvariantCulture),
            row.AmbiguityLabelMismatches.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshStableCompanionVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.AnyIndependentLayerVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllIndependentLayersStableLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllCompleteSiblingLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllElfValidatedLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllBoundsLayouts.ToString(CultureInfo.InvariantCulture),
            Escape(row.CandidateMeshProfileCountDistribution),
            Escape(row.IndependentVariableLayerCountDistribution),
        };
    }

    private static void WriteLines(
        string path,
        IReadOnlyList<string> header,
        IEnumerable<IEnumerable<string>> rows)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string> { string.Join("\t", header) };
        lines.AddRange(rows.Select(row => string.Join("\t", row)));
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static Dictionary<string, int> ReadIndexes(
        string headerLine,
        IEnumerable<string> requiredColumns,
        string reportName)
    {
        var header = headerLine.Split('\t');
        var indexes = requiredColumns.ToDictionary(
            column => column,
            column => Array.FindIndex(header, value =>
                string.Equals(value, column, StringComparison.Ordinal)),
            StringComparer.Ordinal);
        var missing = indexes
            .Where(pair => pair.Value < 0)
            .Select(pair => pair.Key)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"{reportName} is missing columns: {string.Join(",", missing)}.");
        }

        return indexes;
    }

    private static string ReadField(string[] fields, int index, int lineIndex, string reportName)
    {
        if (index < 0 || index >= fields.Length)
        {
            throw new InvalidDataException(
                $"{reportName} row at line {lineIndex + 1} has too few columns.");
        }

        return fields[index];
    }

    private static bool ReadBool(string[] fields, int index, int lineIndex, string reportName)
    {
        var value = ReadField(fields, index, lineIndex, reportName);
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidDataException(
                $"Invalid boolean in {reportName} row at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }

    private static int DistinctCount(IEnumerable<string> values) =>
        values.Distinct(StringComparer.Ordinal).Count();

    private static string SetSha256(IEnumerable<string> values)
    {
        var canonical = string.Join("\n", values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal));
        return Sha256(canonical);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Distribution(IEnumerable<int> values)
    {
        return string.Join(",", values
            .GroupBy(value => value)
            .OrderBy(group => group.Key)
            .Select(group =>
                $"{group.Key.ToString(CultureInfo.InvariantCulture)}={group.Count().ToString(CultureInfo.InvariantCulture)}"));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
