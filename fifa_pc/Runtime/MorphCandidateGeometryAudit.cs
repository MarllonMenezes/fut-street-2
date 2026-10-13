using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// One already-inspected morph/sibling pair. The geometry signature is a
/// measurement of the bounded candidate mesh representation only; it is not
/// a morph, material, UV, skinning, rigging, animation or GPU binding.
/// </summary>
public sealed record MorphCandidateGeometryObservation(
    string NestedBig,
    EaglMorphSiblingEvidence Evidence);

public sealed record MorphCandidateGeometryAuditRow(
    string LayoutKey,
    string LayoutKeySha256,
    int LayoutRows,
    int CandidateMeshProfiles,
    int CandidateGeometryProfiles,
    int CandidateGeometryMissingRows,
    bool CandidateMeshStableGeometryVariable,
    bool CandidateGeometryVariable,
    bool AllCompleteSiblingRows,
    bool AllElfValidatedRows,
    bool AllBoundsRows,
    string CandidateGeometrySetSha256,
    string SampleMembers);

public sealed record MorphCandidateGeometryAuditSummaryRow(
    string Scope,
    int Layouts,
    int Rows,
    int CandidateGeometryVariableLayouts,
    int CandidateMeshStableGeometryVariableLayouts,
    int MissingGeometryLayouts,
    int AllCompleteSiblingLayouts,
    int AllElfValidatedLayouts,
    int AllBoundsLayouts,
    string CandidateGeometryProfileCountDistribution);

public sealed record MorphCandidateGeometryCorrelationRow(
    string LayoutKey,
    string LayoutKeySha256,
    int LayoutRows,
    int PayloadWindowProfiles,
    int CandidateMeshProfiles,
    int CandidateGeometryProfiles,
    int PayloadGeometryPairProfiles,
    bool PayloadWindowVariable,
    bool CandidateGeometryVariable,
    bool PayloadStableGeometryVariable,
    bool GeometryStablePayloadVariable,
    bool BothPayloadAndGeometryVariable,
    int CandidateGeometryMissingRows,
    bool AllCompleteSiblingRows,
    bool AllElfValidatedRows,
    bool AllBoundsRows,
    string PayloadWindowSetSha256,
    string CandidateGeometrySetSha256,
    string PayloadGeometryPairSetSha256,
    string SampleMembers);

public sealed record MorphCandidateGeometryCorrelationSummaryRow(
    string Scope,
    int Layouts,
    int Rows,
    int PayloadGeometryPairVariableLayouts,
    int CandidateGeometryVariableLayouts,
    int PayloadStableGeometryVariableLayouts,
    int GeometryStablePayloadVariableLayouts,
    int BothPayloadAndGeometryVariableLayouts,
    int MissingGeometryLayouts,
    string PayloadGeometryPairProfileCountDistribution);

/// <summary>
/// Compares a second, numeric geometry profile for each candidate mesh inside
/// the existing morph structural layouts. It deliberately does not change the
/// existing ambiguity labels, because this is an additional QA layer rather
/// than a decoder of the underlying asset semantics.
/// </summary>
public static class MorphCandidateGeometryAudit
{
    public static IReadOnlyList<MorphCandidateGeometryAuditRow> Audit(
        IEnumerable<MorphCandidateGeometryObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        return observations
            .GroupBy(observation => observation.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var items = group.ToArray();
                var candidateMeshProfiles = items
                    .Select(item => item.Evidence.SiblingCandidateMeshSignature)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                var geometryValues = items
                    .Select(item => item.Evidence.SiblingCandidateGeometrySignature)
                    .ToArray();
                var geometryProfiles = geometryValues
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                var missingGeometryRows = geometryValues.Count(value => value.Length == 0);
                return new MorphCandidateGeometryAuditRow(
                    group.Key,
                    Sha256(group.Key),
                    items.Length,
                    candidateMeshProfiles,
                    geometryProfiles,
                    missingGeometryRows,
                    candidateMeshProfiles == 1 && geometryProfiles > 1,
                    geometryProfiles > 1,
                    items.Length > 0 && items.All(item => item.Evidence.HasCompleteSibling),
                    items.Length > 0 && items.All(item => item.Evidence.SiblingElfValidated),
                    items.Length > 0 && items.All(item => item.Evidence.SiblingHasBounds),
                    SetSha256(geometryValues),
                    string.Join(",", items
                        .Take(3)
                        .Select(item => $"{item.NestedBig}/{item.Evidence.MorphObjectName}")));
            })
            .ToArray();
    }

    public static IReadOnlyList<MorphCandidateGeometryAuditSummaryRow> Summarize(
        IEnumerable<MorphCandidateGeometryAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var all = rows.ToArray();
        return new[]
        {
            CreateSummary("all", all),
            CreateSummary("geometry_variable", all.Where(row => row.CandidateGeometryVariable).ToArray()),
            CreateSummary("mesh_stable_geometry_variable", all.Where(row => row.CandidateMeshStableGeometryVariable).ToArray()),
        };
    }

    public static IReadOnlyList<MorphCandidateGeometryCorrelationRow> Correlate(
        IEnumerable<MorphCandidateGeometryObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        return observations
            .GroupBy(observation => observation.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var items = group.ToArray();
                var payloadValues = items
                    .Select(item => item.Evidence.NormalizedPayloadWindowHashSignature)
                    .ToArray();
                var geometryValues = items
                    .Select(item => item.Evidence.SiblingCandidateGeometrySignature)
                    .ToArray();
                var payloadGeometryPairs = items
                    .Select(item => string.Join("||", item.Evidence.NormalizedPayloadWindowHashSignature,
                        item.Evidence.SiblingCandidateGeometrySignature))
                    .ToArray();
                var payloadProfiles = payloadValues.Distinct(StringComparer.Ordinal).Count();
                var meshProfiles = items
                    .Select(item => item.Evidence.SiblingCandidateMeshSignature)
                    .Distinct(StringComparer.Ordinal)
                    .Count();
                var geometryProfiles = geometryValues.Distinct(StringComparer.Ordinal).Count();
                return new MorphCandidateGeometryCorrelationRow(
                    group.Key,
                    Sha256(group.Key),
                    items.Length,
                    payloadProfiles,
                    meshProfiles,
                    geometryProfiles,
                    payloadGeometryPairs.Distinct(StringComparer.Ordinal).Count(),
                    payloadProfiles > 1,
                    geometryProfiles > 1,
                    payloadProfiles == 1 && geometryProfiles > 1,
                    payloadProfiles > 1 && geometryProfiles == 1,
                    payloadProfiles > 1 && geometryProfiles > 1,
                    geometryValues.Count(value => value.Length == 0),
                    items.Length > 0 && items.All(item => item.Evidence.HasCompleteSibling),
                    items.Length > 0 && items.All(item => item.Evidence.SiblingElfValidated),
                    items.Length > 0 && items.All(item => item.Evidence.SiblingHasBounds),
                    SetSha256(payloadValues),
                    SetSha256(geometryValues),
                    SetSha256(payloadGeometryPairs),
                    string.Join(",", items
                        .Take(3)
                        .Select(item => $"{item.NestedBig}/{item.Evidence.MorphObjectName}")));
            })
            .ToArray();
    }

    public static IReadOnlyList<MorphCandidateGeometryCorrelationSummaryRow> SummarizeCorrelation(
        IEnumerable<MorphCandidateGeometryCorrelationRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var all = rows.ToArray();
        return new[]
        {
            CreateCorrelationSummary("all", all),
            CreateCorrelationSummary("geometry_variable", all.Where(row => row.CandidateGeometryVariable).ToArray()),
            CreateCorrelationSummary("payload_stable_geometry_variable", all.Where(row => row.PayloadStableGeometryVariable).ToArray()),
        };
    }

    public static void WriteReports(
        string reportPath,
        string summaryPath,
        IEnumerable<MorphCandidateGeometryAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);

        var materialized = rows.ToArray();
        WriteLines(
            reportPath,
            new[]
            {
                "morph_structural_layout",
                "layout_key_sha256",
                "layout_rows",
                "candidate_mesh_profiles",
                "candidate_geometry_profiles",
                "candidate_geometry_missing_rows",
                "candidate_mesh_stable_geometry_variable",
                "candidate_geometry_variable",
                "all_complete_sibling_rows",
                "all_elf_validated_rows",
                "all_bounds_rows",
                "candidate_geometry_set_sha256",
                "sample_members",
            },
            materialized.Select(ToFields));

        WriteLines(
            summaryPath,
            new[]
            {
                "scope",
                "layouts",
                "rows",
                "candidate_geometry_variable_layouts",
                "candidate_mesh_stable_geometry_variable_layouts",
                "missing_geometry_layouts",
                "all_complete_sibling_layouts",
                "all_elf_validated_layouts",
                "all_bounds_layouts",
                "candidate_geometry_profile_count_distribution",
            },
            Summarize(materialized).Select(ToFields));
    }

    public static void WriteCorrelationReports(
        string reportPath,
        string summaryPath,
        IEnumerable<MorphCandidateGeometryCorrelationRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);

        var materialized = rows.ToArray();
        WriteLines(
            reportPath,
            new[]
            {
                "morph_structural_layout",
                "layout_key_sha256",
                "layout_rows",
                "payload_window_profiles",
                "candidate_mesh_profiles",
                "candidate_geometry_profiles",
                "payload_geometry_pair_profiles",
                "payload_window_variable",
                "candidate_geometry_variable",
                "payload_stable_geometry_variable",
                "geometry_stable_payload_variable",
                "both_payload_and_geometry_variable",
                "candidate_geometry_missing_rows",
                "all_complete_sibling_rows",
                "all_elf_validated_rows",
                "all_bounds_rows",
                "payload_window_set_sha256",
                "candidate_geometry_set_sha256",
                "payload_geometry_pair_set_sha256",
                "sample_members",
            },
            materialized.Select(ToCorrelationFields));

        WriteLines(
            summaryPath,
            new[]
            {
                "scope",
                "layouts",
                "rows",
                "payload_geometry_pair_variable_layouts",
                "candidate_geometry_variable_layouts",
                "payload_stable_geometry_variable_layouts",
                "geometry_stable_payload_variable_layouts",
                "both_payload_and_geometry_variable_layouts",
                "missing_geometry_layouts",
                "payload_geometry_pair_profile_count_distribution",
            },
            SummarizeCorrelation(materialized).Select(ToCorrelationSummaryFields));
    }

    private static MorphCandidateGeometryAuditSummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphCandidateGeometryAuditRow> rows)
    {
        return new MorphCandidateGeometryAuditSummaryRow(
            scope,
            rows.Count,
            rows.Sum(row => row.LayoutRows),
            rows.Count(row => row.CandidateGeometryVariable),
            rows.Count(row => row.CandidateMeshStableGeometryVariable),
            rows.Count(row => row.CandidateGeometryMissingRows > 0),
            rows.Count(row => row.AllCompleteSiblingRows),
            rows.Count(row => row.AllElfValidatedRows),
            rows.Count(row => row.AllBoundsRows),
            Distribution(rows.Select(row => row.CandidateGeometryProfiles)));
    }

    private static MorphCandidateGeometryCorrelationSummaryRow CreateCorrelationSummary(
        string scope,
        IReadOnlyList<MorphCandidateGeometryCorrelationRow> rows)
    {
        return new MorphCandidateGeometryCorrelationSummaryRow(
            scope,
            rows.Count,
            rows.Sum(row => row.LayoutRows),
            rows.Count(row => row.PayloadGeometryPairProfiles > 1),
            rows.Count(row => row.CandidateGeometryVariable),
            rows.Count(row => row.PayloadStableGeometryVariable),
            rows.Count(row => row.GeometryStablePayloadVariable),
            rows.Count(row => row.BothPayloadAndGeometryVariable),
            rows.Count(row => row.CandidateGeometryMissingRows > 0),
            Distribution(rows.Select(row => row.PayloadGeometryPairProfiles)));
    }

    private static IEnumerable<string> ToFields(MorphCandidateGeometryAuditRow row)
    {
        return new[]
        {
            Escape(row.LayoutKey),
            row.LayoutKeySha256,
            row.LayoutRows.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryMissingRows.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshStableGeometryVariable.ToString(),
            row.CandidateGeometryVariable.ToString(),
            row.AllCompleteSiblingRows.ToString(),
            row.AllElfValidatedRows.ToString(),
            row.AllBoundsRows.ToString(),
            row.CandidateGeometrySetSha256,
            Escape(row.SampleMembers),
        };
    }

    private static IEnumerable<string> ToFields(MorphCandidateGeometryAuditSummaryRow row)
    {
        return new[]
        {
            row.Scope,
            row.Layouts.ToString(CultureInfo.InvariantCulture),
            row.Rows.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshStableGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.MissingGeometryLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllCompleteSiblingLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllElfValidatedLayouts.ToString(CultureInfo.InvariantCulture),
            row.AllBoundsLayouts.ToString(CultureInfo.InvariantCulture),
            Escape(row.CandidateGeometryProfileCountDistribution),
        };
    }

    private static IEnumerable<string> ToCorrelationFields(MorphCandidateGeometryCorrelationRow row)
    {
        return new[]
        {
            Escape(row.LayoutKey),
            row.LayoutKeySha256,
            row.LayoutRows.ToString(CultureInfo.InvariantCulture),
            row.PayloadWindowProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateMeshProfiles.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryProfiles.ToString(CultureInfo.InvariantCulture),
            row.PayloadGeometryPairProfiles.ToString(CultureInfo.InvariantCulture),
            row.PayloadWindowVariable.ToString(),
            row.CandidateGeometryVariable.ToString(),
            row.PayloadStableGeometryVariable.ToString(),
            row.GeometryStablePayloadVariable.ToString(),
            row.BothPayloadAndGeometryVariable.ToString(),
            row.CandidateGeometryMissingRows.ToString(CultureInfo.InvariantCulture),
            row.AllCompleteSiblingRows.ToString(),
            row.AllElfValidatedRows.ToString(),
            row.AllBoundsRows.ToString(),
            row.PayloadWindowSetSha256,
            row.CandidateGeometrySetSha256,
            row.PayloadGeometryPairSetSha256,
            Escape(row.SampleMembers),
        };
    }

    private static IEnumerable<string> ToCorrelationSummaryFields(
        MorphCandidateGeometryCorrelationSummaryRow row)
    {
        return new[]
        {
            row.Scope,
            row.Layouts.ToString(CultureInfo.InvariantCulture),
            row.Rows.ToString(CultureInfo.InvariantCulture),
            row.PayloadGeometryPairVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.PayloadStableGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.GeometryStablePayloadVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.BothPayloadAndGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
            row.MissingGeometryLayouts.ToString(CultureInfo.InvariantCulture),
            Escape(row.PayloadGeometryPairProfileCountDistribution),
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

    private static string SetSha256(IEnumerable<string> values)
    {
        var canonical = string.Join("\n", values
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal));
        return Sha256(canonical);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Distribution(IEnumerable<int> values) =>
        string.Join(",", values
            .GroupBy(value => value)
            .OrderBy(group => group.Key)
            .Select(group =>
                $"{group.Key.ToString(CultureInfo.InvariantCulture)}={group.Count().ToString(CultureInfo.InvariantCulture)}"));

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
