using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

/// <summary>
/// A catalog row projected into the independent SHPX comparison. The source
/// layout and object names are already observed evidence; this audit does not
/// infer a morph-to-image relationship from them.
/// </summary>
public sealed record MorphShpxCatalogRow(
    string NestedBig,
    string MorphObject,
    string LayoutKey);

/// <summary>
/// One same-trunk SHPX package observation for one already catalogued morph
/// row. Package signatures contain only SHPX header/table metadata.
/// </summary>
public sealed record MorphShpxStructureObservation(
    string NestedBig,
    string MorphObject,
    string LayoutKey,
    string ExpectedXshName,
    string XshArchiveName,
    bool XshFound,
    bool XshValidated,
    string FormatVersion,
    int EntryCount,
    string PackageStructuralSignature,
    string? Issue);

public sealed record MorphShpxStructureAuditRow(
    string LayoutKey,
    string LayoutKeySha256,
    int CatalogRows,
    int ObservedRows,
    int ObservedNestedBigs,
    int XshFoundRows,
    int XshValidatedRows,
    int MissingXshRows,
    int InvalidXshRows,
    int XshPackageProfiles,
    int XshEntryCountProfiles,
    int XshFormatProfiles,
    bool XshStructureVariable,
    bool LayoutAmbiguous,
    bool ReportRowCountMatches,
    string XshPackageSetSha256,
    string MissingXshNames,
    string InvalidXshNames,
    string SampleMembers);

public sealed record MorphShpxStructureAuditSummaryRow(
    string Scope,
    int Layouts,
    int CatalogRows,
    int ObservedRows,
    int XshValidatedRows,
    int MissingXshRows,
    int InvalidXshRows,
    int XshVariableLayouts,
    int XshVariableAmbiguousLayouts,
    int XshVariableStableLayouts,
    int ObservationRowCountMismatches,
    int ReportRowCountMismatches,
    string XshPackageProfileCountDistribution);

/// <summary>
/// Compares morph structural layouts with the independent, same-trunk SHPX
/// package table. It reports only package presence, format, entry counts and
/// the existing structural signature. It does not assign texture entries to
/// morphs, vertices, UVs, materials, rigging, animation or GPU commands.
/// </summary>
public static class MorphShpxStructureAudit
{
    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;

    public static IReadOnlyList<MorphShpxStructureObservation> Scan(
        GameAssetStore store,
        string modelArchivePath,
        IEnumerable<MorphShpxCatalogRow> catalogRows)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentNullException.ThrowIfNull(catalogRows);

        var materializedCatalog = catalogRows.ToArray();
        var package = BigArchiveReader.Inspect(store.OpenRaw(modelArchivePath));
        var xshByStem = package.Entries
            .Where(entry => string.Equals(
                Path.GetExtension(entry.Name),
                ".xsh",
                StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var cache = new Dictionary<string, MorphShpxPackageObservation>(StringComparer.OrdinalIgnoreCase);
        var observations = new List<MorphShpxStructureObservation>(materializedCatalog.Length);

        foreach (var catalogRow in materializedCatalog)
        {
            var expectedXshName = Path.ChangeExtension(catalogRow.NestedBig, ".xsh");
            var stem = Path.GetFileNameWithoutExtension(catalogRow.NestedBig);
            if (!xshByStem.TryGetValue(stem, out var xshEntry))
            {
                observations.Add(new MorphShpxStructureObservation(
                    catalogRow.NestedBig,
                    catalogRow.MorphObject,
                    catalogRow.LayoutKey,
                    expectedXshName,
                    string.Empty,
                    false,
                    false,
                    string.Empty,
                    0,
                    string.Empty,
                    "same-stem .xsh entry not found"));
                continue;
            }

            if (!cache.TryGetValue(xshEntry.Name, out var packageObservation))
            {
                packageObservation = InspectXsh(store, modelArchivePath, xshEntry.Name);
                cache.Add(xshEntry.Name, packageObservation);
            }

            observations.Add(new MorphShpxStructureObservation(
                catalogRow.NestedBig,
                catalogRow.MorphObject,
                catalogRow.LayoutKey,
                expectedXshName,
                xshEntry.Name,
                true,
                packageObservation.Validated,
                packageObservation.FormatVersion,
                packageObservation.EntryCount,
                packageObservation.StructuralSignature,
                packageObservation.Issue));
        }

        return observations;
    }

    public static IReadOnlyList<MorphShpxStructureAuditRow> Audit(
        IEnumerable<MorphShpxCatalogRow> catalogRows,
        IEnumerable<MorphAmbiguityReportRow> ambiguityRows,
        IEnumerable<MorphShpxStructureObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(catalogRows);
        ArgumentNullException.ThrowIfNull(ambiguityRows);
        ArgumentNullException.ThrowIfNull(observations);

        var catalogByLayout = catalogRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var ambiguityByLayout = ambiguityRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var observationsByLayout = observations
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var keys = catalogByLayout.Keys
            .Concat(ambiguityByLayout.Keys)
            .Concat(observationsByLayout.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal);

        return keys.Select(layoutKey =>
        {
            var catalog = catalogByLayout.TryGetValue(layoutKey, out var matchedCatalog)
                ? matchedCatalog
                : Array.Empty<MorphShpxCatalogRow>();
            var ambiguity = ambiguityByLayout.TryGetValue(layoutKey, out var matchedAmbiguity)
                ? matchedAmbiguity
                : Array.Empty<MorphAmbiguityReportRow>();
            var observed = observationsByLayout.TryGetValue(layoutKey, out var matchedObservations)
                ? matchedObservations
                : Array.Empty<MorphShpxStructureObservation>();
            var xshSignatures = observed
                .Where(row => row.XshValidated && row.PackageStructuralSignature.Length > 0)
                .Select(row => row.PackageStructuralSignature)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var entryCounts = observed
                .Where(row => row.XshValidated)
                .Select(row => row.EntryCount)
                .Distinct()
                .ToArray();
            var formats = observed
                .Where(row => row.XshValidated)
                .Select(row => row.FormatVersion)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var missingXshNames = string.Join(",", observed
                .Where(row => !row.XshFound)
                .Select(row => row.ExpectedXshName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
            var invalidXshNames = string.Join(",", observed
                .Where(row => row.XshFound && !row.XshValidated)
                .Select(row => row.XshArchiveName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
            var report = ambiguity.Length == 1 ? ambiguity[0] : null;
            return new MorphShpxStructureAuditRow(
                layoutKey,
                Sha256(layoutKey),
                catalog.Length,
                observed.Length,
                observed.Select(row => row.NestedBig).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                observed.Count(row => row.XshFound),
                observed.Count(row => row.XshValidated),
                observed.Count(row => !row.XshFound),
                observed.Count(row => row.XshFound && !row.XshValidated),
                xshSignatures.Length,
                entryCounts.Length,
                formats.Length,
                xshSignatures.Length > 1 || entryCounts.Length > 1 || formats.Length > 1,
                report?.LayoutAmbiguous ?? false,
                report is not null && report.LayoutRows == catalog.Length,
                SetSha256(xshSignatures),
                missingXshNames,
                invalidXshNames,
                string.Join(",", observed
                    .Take(3)
                    .Select(row => $"{row.NestedBig}/{row.MorphObject}")));
        }).ToArray();
    }

    public static IReadOnlyList<MorphShpxStructureAuditSummaryRow> Summarize(
        IEnumerable<MorphShpxStructureAuditRow> rows)
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
        IEnumerable<MorphShpxStructureAuditRow> rows)
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
                "observed_rows",
                "observed_nested_bigs",
                "xsh_found_rows",
                "xsh_validated_rows",
                "missing_xsh_rows",
                "invalid_xsh_rows",
                "xsh_package_profiles",
                "xsh_entry_count_profiles",
                "xsh_format_profiles",
                "xsh_structure_variable",
                "layout_ambiguous",
                "report_row_count_matches",
                "xsh_package_set_sha256",
                "missing_xsh_names",
                "invalid_xsh_names",
                "sample_members",
            },
            materialized.Select(ToFields));

        WriteLines(
            summaryPath,
            new[]
            {
                "scope",
                "layouts",
                "catalog_rows",
                "observed_rows",
                "xsh_validated_rows",
                "missing_xsh_rows",
                "invalid_xsh_rows",
                "xsh_variable_layouts",
                "xsh_variable_ambiguous_layouts",
                "xsh_variable_stable_layouts",
                "observation_row_count_mismatches",
                "report_row_count_mismatches",
                "xsh_package_profile_count_distribution",
            },
            Summarize(materialized).Select(ToSummaryFields));
    }

    private static MorphShpxPackageObservation InspectXsh(
        GameAssetStore store,
        string modelArchivePath,
        string xshEntryName)
    {
        try
        {
            using var stream = store.OpenBigEntry(modelArchivePath, xshEntryName);
            if (stream.Length > MaximumCompressedBytes)
            {
                throw new InvalidDataException("Compressed SHPX package exceeds the 64 MiB safety limit.");
            }

            var compressed = new byte[checked((int)stream.Length)];
            stream.ReadExactly(compressed);
            byte[] packageData;
            if (compressed.Length >= 4 && compressed.AsSpan(0, 4).SequenceEqual("SHPX"u8))
            {
                packageData = compressed;
            }
            else
            {
                var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
                if (expandedSize > MaximumExpandedBytes)
                {
                    throw new InvalidDataException("Expanded SHPX package exceeds the 64 MiB safety limit.");
                }

                using var compressedMemory = new MemoryStream(compressed, writable: false);
                packageData = RefPackDecoder.Decode(compressedMemory, expandedSize).Data;
            }

            var package = ShapeImagePackageLoader.Load(packageData);
            return new MorphShpxPackageObservation(
                true,
                package.FormatVersion,
                package.Entries.Count,
                ShapeImagePackageLoader.GetStructuralSignature(package),
                null);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            EndOfStreamException or
            OverflowException or
            ArgumentOutOfRangeException or
            IOException)
        {
            return new MorphShpxPackageObservation(
                false,
                string.Empty,
                0,
                string.Empty,
                exception.Message);
        }
    }

    private static MorphShpxStructureAuditSummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphShpxStructureAuditRow> rows)
    {
        return new MorphShpxStructureAuditSummaryRow(
            scope,
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Sum(row => row.ObservedRows),
            rows.Sum(row => row.XshValidatedRows),
            rows.Sum(row => row.MissingXshRows),
            rows.Sum(row => row.InvalidXshRows),
            rows.Count(row => row.XshStructureVariable),
            rows.Count(row => row.XshStructureVariable && row.LayoutAmbiguous),
            rows.Count(row => row.XshStructureVariable && !row.LayoutAmbiguous),
            rows.Count(row => row.ObservedRows != row.CatalogRows),
            rows.Count(row => !row.ReportRowCountMatches),
            Distribution(rows.Select(row => row.XshPackageProfiles)));
    }

    private static string[] ToFields(MorphShpxStructureAuditRow row) => new[]
    {
        Escape(row.LayoutKey),
        row.LayoutKeySha256,
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ObservedRows.ToString(CultureInfo.InvariantCulture),
        row.ObservedNestedBigs.ToString(CultureInfo.InvariantCulture),
        row.XshFoundRows.ToString(CultureInfo.InvariantCulture),
        row.XshValidatedRows.ToString(CultureInfo.InvariantCulture),
        row.MissingXshRows.ToString(CultureInfo.InvariantCulture),
        row.InvalidXshRows.ToString(CultureInfo.InvariantCulture),
        row.XshPackageProfiles.ToString(CultureInfo.InvariantCulture),
        row.XshEntryCountProfiles.ToString(CultureInfo.InvariantCulture),
        row.XshFormatProfiles.ToString(CultureInfo.InvariantCulture),
        row.XshStructureVariable.ToString(),
        row.LayoutAmbiguous.ToString(),
        row.ReportRowCountMatches.ToString(),
        row.XshPackageSetSha256,
        Escape(row.MissingXshNames),
        Escape(row.InvalidXshNames),
        Escape(row.SampleMembers),
    };

    private static string[] ToSummaryFields(MorphShpxStructureAuditSummaryRow row) => new[]
    {
        row.Scope,
        row.Layouts.ToString(CultureInfo.InvariantCulture),
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ObservedRows.ToString(CultureInfo.InvariantCulture),
        row.XshValidatedRows.ToString(CultureInfo.InvariantCulture),
        row.MissingXshRows.ToString(CultureInfo.InvariantCulture),
        row.InvalidXshRows.ToString(CultureInfo.InvariantCulture),
        row.XshVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.XshVariableAmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
        row.XshVariableStableLayouts.ToString(CultureInfo.InvariantCulture),
        row.ObservationRowCountMismatches.ToString(CultureInfo.InvariantCulture),
        row.ReportRowCountMismatches.ToString(CultureInfo.InvariantCulture),
        Escape(row.XshPackageProfileCountDistribution),
    };

    private static void WriteLines(
        string path,
        IEnumerable<string> header,
        IEnumerable<string[]> rows)
    {
        var directory = Path.GetDirectoryName(path);
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

    private static string Distribution(IEnumerable<int> values) => string.Join(",", values
        .GroupBy(value => value)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}"));

    private static string SetSha256(IEnumerable<string> values)
    {
        var normalized = string.Join("\n", values
            .OrderBy(value => value, StringComparer.Ordinal));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Escape(string value) =>
        value.Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

    private sealed record MorphShpxPackageObservation(
        bool Validated,
        string FormatVersion,
        int EntryCount,
        string StructuralSignature,
        string? Issue);
}
