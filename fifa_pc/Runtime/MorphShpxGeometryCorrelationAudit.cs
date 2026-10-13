using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// A layout-level join between the independently generated SHPX structure
/// report and the candidate-geometry signatures in the morph catalog. It
/// measures co-variation only; it does not pair a texture entry with a mesh,
/// morph, UV, material, pose, rig, skinning or GPU command.
/// </summary>
public sealed record MorphShpxGeometryCatalogRow(
    string LayoutKey,
    string CandidateGeometrySignature);

public sealed record MorphShpxGeometryPackageRow(
    string LayoutKey,
    int XshPackageProfiles,
    bool XshStructureVariable,
    bool LayoutAmbiguous,
    string XshPackageSetSha256);

public sealed record MorphShpxGeometryCorrelationRow(
    string LayoutKey,
    string LayoutKeySha256,
    int CatalogRows,
    int ShpxAuditRows,
    int CandidateGeometryProfiles,
    int CandidateGeometryMissingRows,
    bool CandidateGeometryVariable,
    int XshPackageProfiles,
    bool XshStructureVariable,
    bool LayoutAmbiguous,
    bool BothVariable,
    bool GeometryVariableOnly,
    bool ShpxVariableOnly,
    bool BothStable,
    bool Joined,
    string CandidateGeometrySetSha256,
    string XshPackageSetSha256,
    string Classification);

public sealed record MorphShpxGeometryCorrelationSummaryRow(
    string Scope,
    int Layouts,
    int CatalogRows,
    int ShpxAuditRows,
    int JoinedLayouts,
    int MissingCatalogLayouts,
    int MissingShpxLayouts,
    int CandidateGeometryVariableLayouts,
    int ShpxVariableLayouts,
    int BothVariableLayouts,
    int GeometryVariableOnlyLayouts,
    int ShpxVariableOnlyLayouts,
    int BothStableLayouts,
    int CandidateGeometryMissingLayouts,
    string ClassificationCounts);

public static class MorphShpxGeometryCorrelationAudit
{
    public static IReadOnlyList<MorphShpxGeometryCatalogRow> ReadCatalog(string path)
    {
        var lines = File.ReadAllLines(path);
        var indexes = ReadIndexes(
            lines,
            new[] { "morph_structural_layout", "candidate_geometry_signature" },
            "morph structural catalog");
        var rows = new List<MorphShpxGeometryCatalogRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphShpxGeometryCatalogRow(
                ReadField(fields, indexes["morph_structural_layout"], lineIndex, "catalog"),
                ReadField(fields, indexes["candidate_geometry_signature"], lineIndex, "catalog")));
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("Morph structural catalog has no data rows.");
        }

        return rows;
    }

    public static IReadOnlyList<MorphShpxGeometryPackageRow> ReadShpxReport(string path)
    {
        var lines = File.ReadAllLines(path);
        var indexes = ReadIndexes(
            lines,
            new[]
            {
                "morph_structural_layout",
                "xsh_package_profiles",
                "xsh_structure_variable",
                "layout_ambiguous",
                "xsh_package_set_sha256",
            },
            "SHPX structure report");
        var rows = new List<MorphShpxGeometryPackageRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphShpxGeometryPackageRow(
                ReadField(fields, indexes["morph_structural_layout"], lineIndex, "SHPX report"),
                ReadInt(fields, indexes["xsh_package_profiles"], lineIndex, "SHPX report"),
                ReadBool(fields, indexes["xsh_structure_variable"], lineIndex, "SHPX report"),
                ReadBool(fields, indexes["layout_ambiguous"], lineIndex, "SHPX report"),
                ReadField(fields, indexes["xsh_package_set_sha256"], lineIndex, "SHPX report")));
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("SHPX structure report has no data rows.");
        }

        return rows;
    }

    public static IReadOnlyList<MorphShpxGeometryCorrelationRow> Audit(
        IEnumerable<MorphShpxGeometryCatalogRow> catalogRows,
        IEnumerable<MorphShpxGeometryPackageRow> shpxRows)
    {
        ArgumentNullException.ThrowIfNull(catalogRows);
        ArgumentNullException.ThrowIfNull(shpxRows);

        var catalogByLayout = catalogRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var shpxByLayout = shpxRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var keys = catalogByLayout.Keys
            .Concat(shpxByLayout.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);

        return keys.Select(layoutKey =>
        {
            var catalog = catalogByLayout.TryGetValue(layoutKey, out var matchedCatalog)
                ? matchedCatalog
                : Array.Empty<MorphShpxGeometryCatalogRow>();
            var shpx = shpxByLayout.TryGetValue(layoutKey, out var matchedShpx)
                ? matchedShpx
                : Array.Empty<MorphShpxGeometryPackageRow>();
            var geometryValues = catalog
                .Select(row => row.CandidateGeometrySignature)
                .ToArray();
            var geometryProfiles = geometryValues
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var candidateGeometryVariable = geometryProfiles > 1;
            var geometryMissingRows = geometryValues.Count(value => value.Length == 0);
            var packageProfiles = shpx.Length == 1
                ? shpx[0].XshPackageProfiles
                : 0;
            var shpxVariable = shpx.Length == 1 && shpx[0].XshStructureVariable;
            var layoutAmbiguous = shpx.Length == 1 && shpx[0].LayoutAmbiguous;
            var joined = catalog.Length > 0 && shpx.Length == 1;
            var bothVariable = joined && candidateGeometryVariable && shpxVariable;
            var geometryOnly = joined && candidateGeometryVariable && !shpxVariable;
            var shpxOnly = joined && !candidateGeometryVariable && shpxVariable;
            var bothStable = joined && !candidateGeometryVariable && !shpxVariable;
            var classification = !joined
                ? catalog.Length == 0 ? "missing-catalog" : "missing-shpx-report"
                : bothVariable
                    ? "both-variable"
                    : geometryOnly
                        ? "geometry-variable-only"
                        : shpxOnly
                            ? "shpx-variable-only"
                            : "both-stable";
            return new MorphShpxGeometryCorrelationRow(
                layoutKey,
                Sha256(layoutKey),
                catalog.Length,
                shpx.Length,
                geometryProfiles,
                geometryMissingRows,
                candidateGeometryVariable,
                packageProfiles,
                shpxVariable,
                layoutAmbiguous,
                bothVariable,
                geometryOnly,
                shpxOnly,
                bothStable,
                joined,
                SetSha256(geometryValues),
                shpx.Length == 1 ? shpx[0].XshPackageSetSha256 : string.Empty,
                classification);
        }).ToArray();
    }

    public static IReadOnlyList<MorphShpxGeometryCorrelationSummaryRow> Summarize(
        IEnumerable<MorphShpxGeometryCorrelationRow> rows)
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
        string reportPath,
        string summaryPath,
        IEnumerable<MorphShpxGeometryCorrelationRow> rows)
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
                "catalog_rows",
                "shpx_audit_rows",
                "candidate_geometry_profiles",
                "candidate_geometry_missing_rows",
                "candidate_geometry_variable",
                "xsh_package_profiles",
                "xsh_structure_variable",
                "layout_ambiguous",
                "both_variable",
                "geometry_variable_only",
                "shpx_variable_only",
                "both_stable",
                "joined",
                "candidate_geometry_set_sha256",
                "xsh_package_set_sha256",
                "classification",
            },
            materialized.Select(ToFields));
        WriteLines(
            summaryPath,
            new[]
            {
                "scope",
                "layouts",
                "catalog_rows",
                "shpx_audit_rows",
                "joined_layouts",
                "missing_catalog_layouts",
                "missing_shpx_layouts",
                "candidate_geometry_variable_layouts",
                "shpx_variable_layouts",
                "both_variable_layouts",
                "geometry_variable_only_layouts",
                "shpx_variable_only_layouts",
                "both_stable_layouts",
                "candidate_geometry_missing_layouts",
                "classification_counts",
            },
            Summarize(materialized).Select(ToSummaryFields));
    }

    private static MorphShpxGeometryCorrelationSummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphShpxGeometryCorrelationRow> rows) =>
        new(
            scope,
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Sum(row => row.ShpxAuditRows),
            rows.Count(row => row.Joined),
            rows.Count(row => row.Classification == "missing-catalog"),
            rows.Count(row => row.Classification == "missing-shpx-report"),
            rows.Count(row => row.CandidateGeometryVariable),
            rows.Count(row => row.XshStructureVariable),
            rows.Count(row => row.BothVariable),
            rows.Count(row => row.GeometryVariableOnly),
            rows.Count(row => row.ShpxVariableOnly),
            rows.Count(row => row.BothStable),
            rows.Count(row => row.CandidateGeometryMissingRows > 0),
            string.Join(",", rows
                .GroupBy(row => row.Classification, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key}={group.Count().ToString(CultureInfo.InvariantCulture)}")));

    private static string[] ToFields(MorphShpxGeometryCorrelationRow row) => new[]
    {
        Escape(row.LayoutKey),
        row.LayoutKeySha256,
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ShpxAuditRows.ToString(CultureInfo.InvariantCulture),
        row.CandidateGeometryProfiles.ToString(CultureInfo.InvariantCulture),
        row.CandidateGeometryMissingRows.ToString(CultureInfo.InvariantCulture),
        row.CandidateGeometryVariable.ToString(),
        row.XshPackageProfiles.ToString(CultureInfo.InvariantCulture),
        row.XshStructureVariable.ToString(),
        row.LayoutAmbiguous.ToString(),
        row.BothVariable.ToString(),
        row.GeometryVariableOnly.ToString(),
        row.ShpxVariableOnly.ToString(),
        row.BothStable.ToString(),
        row.Joined.ToString(),
        row.CandidateGeometrySetSha256,
        row.XshPackageSetSha256,
        row.Classification,
    };

    private static string[] ToSummaryFields(MorphShpxGeometryCorrelationSummaryRow row) => new[]
    {
        row.Scope,
        row.Layouts.ToString(CultureInfo.InvariantCulture),
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ShpxAuditRows.ToString(CultureInfo.InvariantCulture),
        row.JoinedLayouts.ToString(CultureInfo.InvariantCulture),
        row.MissingCatalogLayouts.ToString(CultureInfo.InvariantCulture),
        row.MissingShpxLayouts.ToString(CultureInfo.InvariantCulture),
        row.CandidateGeometryVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.ShpxVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.BothVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.GeometryVariableOnlyLayouts.ToString(CultureInfo.InvariantCulture),
        row.ShpxVariableOnlyLayouts.ToString(CultureInfo.InvariantCulture),
        row.BothStableLayouts.ToString(CultureInfo.InvariantCulture),
        row.CandidateGeometryMissingLayouts.ToString(CultureInfo.InvariantCulture),
        row.ClassificationCounts,
    };

    private static IReadOnlyDictionary<string, int> ReadIndexes(
        IReadOnlyList<string> lines,
        IReadOnlyList<string> required,
        string source)
    {
        if (lines.Count == 0)
        {
            throw new InvalidDataException($"{source} is empty.");
        }

        var indexes = lines[0]
            .Split('\t')
            .Select((name, index) => (Name: name, Index: index))
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.Ordinal);
        foreach (var column in required)
        {
            if (!indexes.ContainsKey(column))
            {
                throw new InvalidDataException($"{source} is missing required column '{column}'.");
            }
        }

        return indexes;
    }

    private static string ReadField(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex,
        string source)
    {
        if ((uint)index >= (uint)fields.Count)
        {
            throw new InvalidDataException($"{source} row {lineIndex} is missing column {index}.");
        }

        return fields[index];
    }

    private static int ReadInt(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex,
        string source)
    {
        var value = ReadField(fields, index, lineIndex, source);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) || parsed < 0)
        {
            throw new InvalidDataException($"{source} row {lineIndex} has an invalid non-negative integer.");
        }

        return parsed;
    }

    private static bool ReadBool(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex,
        string source)
    {
        var value = ReadField(fields, index, lineIndex, source);
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidDataException($"{source} row {lineIndex} has an invalid boolean.");
        }

        return parsed;
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

        File.WriteAllLines(
            path,
            new[] { string.Join("\t", header) }
                .Concat(rows.Select(row => string.Join("\t", row))),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
