using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Joins the candidate-geometry signature with the already published morph
/// ambiguity labels. This is an independent consistency check over observed
/// TSV fields; it does not decode morph, topology, rigging, animation, UV,
/// material or GPU semantics.
/// </summary>
public sealed record MorphCandidateGeometryAmbiguityCatalogRow(
    string NestedBig,
    string MorphObject,
    string LayoutKey,
    string CandidateGeometrySignature);

public sealed record MorphCandidateGeometryAmbiguityAuditRow(
    string LayoutKey,
    string LayoutKeySha256,
    int CatalogRows,
    int ReportRows,
    int ReportLayoutRows,
    bool ReportRowCountMatches,
    bool LayoutAmbiguous,
    bool LayoutStable,
    int CandidateGeometryProfiles,
    int CandidateGeometryMissingRows,
    bool CandidateGeometryVariable,
    bool GeometryVariableMatchesAmbiguityLabel,
    string CandidateGeometrySetSha256,
    string SampleMembers);

public sealed record MorphCandidateGeometryAmbiguitySummaryRow(
    string Scope,
    int Layouts,
    int CatalogRows,
    int AmbiguousLayouts,
    int StableLayouts,
    int GeometryVariableLayouts,
    int GeometryVariableAmbiguousLayouts,
    int GeometryVariableStableLayouts,
    int AmbiguousWithoutGeometryVariableLayouts,
    int GeometryVariableLabelMismatches,
    int ReportRowCountMismatches,
    int MissingGeometryLayouts,
    string GeometryProfileCountDistribution);

public static class MorphCandidateGeometryAmbiguityAudit
{
    private static readonly string[] RequiredCatalogColumns =
    {
        "nested_big",
        "morph_object",
        "morph_structural_layout",
        "candidate_geometry_signature",
    };

    public static IReadOnlyList<MorphCandidateGeometryAmbiguityCatalogRow> ReadCatalog(
        string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph structural catalog is empty.");
        }

        var indexes = ReadIndexes(lines[0], RequiredCatalogColumns, "morph structural catalog");
        var rows = new List<MorphCandidateGeometryAmbiguityCatalogRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphCandidateGeometryAmbiguityCatalogRow(
                ReadField(fields, indexes["nested_big"], lineIndex, "morph structural catalog"),
                ReadField(fields, indexes["morph_object"], lineIndex, "morph structural catalog"),
                ReadField(fields, indexes["morph_structural_layout"], lineIndex, "morph structural catalog"),
                ReadField(fields, indexes["candidate_geometry_signature"], lineIndex, "morph structural catalog")));
        }

        return rows;
    }

    public static IReadOnlyList<MorphCandidateGeometryAmbiguityAuditRow> Audit(
        IEnumerable<MorphCandidateGeometryAmbiguityCatalogRow> catalogRows,
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
                : Array.Empty<MorphCandidateGeometryAmbiguityCatalogRow>();
            var reports = reportByLayout.TryGetValue(layoutKey, out var matchedReports)
                ? matchedReports
                : Array.Empty<MorphAmbiguityReportRow>();
            var firstReport = reports.FirstOrDefault();
            var geometryValues = catalog
                .Select(row => row.CandidateGeometrySignature)
                .ToArray();
            var geometryProfiles = geometryValues
                .Distinct(StringComparer.Ordinal)
                .Count();
            var geometryMissingRows = geometryValues.Count(value => value.Length == 0);
            var geometryVariable = geometryProfiles > 1;
            var labelMatches = firstReport is not null &&
                                firstReport.LayoutAmbiguous == geometryVariable;

            return new MorphCandidateGeometryAmbiguityAuditRow(
                layoutKey,
                Sha256(layoutKey),
                catalog.Length,
                reports.Length,
                firstReport?.LayoutRows ?? 0,
                reports.Length == 1 && firstReport is not null &&
                firstReport.LayoutRows == catalog.Length,
                firstReport?.LayoutAmbiguous ?? false,
                firstReport?.LayoutStable ?? false,
                geometryProfiles,
                geometryMissingRows,
                geometryVariable,
                labelMatches,
                SetSha256(geometryValues),
                string.Join(",", catalog
                    .Take(3)
                    .Select(row => $"{row.NestedBig}/{row.MorphObject}")));
        }).ToArray();
    }

    public static IReadOnlyList<MorphCandidateGeometryAmbiguitySummaryRow> Summarize(
        IEnumerable<MorphCandidateGeometryAmbiguityAuditRow> rows)
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
        IEnumerable<MorphCandidateGeometryAmbiguityAuditRow> rows)
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
            "candidate_geometry_profiles",
            "candidate_geometry_missing_rows",
            "candidate_geometry_variable",
            "geometry_variable_matches_ambiguity_label",
            "candidate_geometry_set_sha256",
            "sample_members",
        }, materialized.Select(ToFields));

        WriteLines(summaryPath, new[]
        {
            "scope",
            "layouts",
            "catalog_rows",
            "ambiguous_layouts",
            "stable_layouts",
            "geometry_variable_layouts",
            "geometry_variable_ambiguous_layouts",
            "geometry_variable_stable_layouts",
            "ambiguous_without_geometry_variable_layouts",
            "geometry_variable_label_mismatches",
            "report_row_count_mismatches",
            "missing_geometry_layouts",
            "geometry_profile_count_distribution",
        }, Summarize(materialized).Select(ToFields));
    }

    private static MorphCandidateGeometryAmbiguitySummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphCandidateGeometryAmbiguityAuditRow> rows)
    {
        return new MorphCandidateGeometryAmbiguitySummaryRow(
            scope,
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Count(row => row.LayoutAmbiguous),
            rows.Count(row => !row.LayoutAmbiguous),
            rows.Count(row => row.CandidateGeometryVariable),
            rows.Count(row => row.CandidateGeometryVariable && row.LayoutAmbiguous),
            rows.Count(row => row.CandidateGeometryVariable && !row.LayoutAmbiguous),
            rows.Count(row => row.LayoutAmbiguous && !row.CandidateGeometryVariable),
            rows.Count(row => !row.GeometryVariableMatchesAmbiguityLabel),
            rows.Count(row => !row.ReportRowCountMatches),
            rows.Count(row => row.CandidateGeometryMissingRows > 0),
            Distribution(rows.Select(row => row.CandidateGeometryProfiles)));
    }

    private static IEnumerable<string> ToFields(
        MorphCandidateGeometryAmbiguityAuditRow row)
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
            row.CandidateGeometryProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryMissingRows.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryVariable.ToString(),
            row.GeometryVariableMatchesAmbiguityLabel.ToString(),
            row.CandidateGeometrySetSha256,
            Escape(row.SampleMembers),
        };
    }

    private static IEnumerable<string> ToFields(
        MorphCandidateGeometryAmbiguitySummaryRow row)
    {
        return new[]
        {
            row.Scope,
            row.Layouts.ToString(CultureInfo.InvariantCulture),
            row.CatalogRows.ToString(CultureInfo.InvariantCulture),
            row.AmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
            row.StableLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryVariableAmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryVariableStableLayouts.ToString(CultureInfo.InvariantCulture),
            row.AmbiguousWithoutGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryVariableLabelMismatches.ToString(CultureInfo.InvariantCulture),
            row.ReportRowCountMismatches.ToString(CultureInfo.InvariantCulture),
            row.MissingGeometryLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryProfileCountDistribution,
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
