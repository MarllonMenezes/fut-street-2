using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record MorphElfHeaderObservation(
    string NestedBig,
    string MorphObject,
    string LayoutKey,
    bool Validated,
    bool HasMorphSymbol,
    string HeaderCoreSignature,
    string HeaderRangeSignature,
    string MorphSymbolMetadataSignature,
    string? Issue);

public sealed record MorphElfHeaderAuditRow(
    string LayoutKey,
    string LayoutKeySha256,
    int CatalogRows,
    int ObservedRows,
    int ValidatedRows,
    int MorphSymbolRows,
    int InvalidRows,
    int HeaderCoreProfiles,
    int HeaderRangeProfiles,
    int MorphSymbolMetadataProfiles,
    bool HeaderStructureVariable,
    bool MorphSymbolMetadataVariable,
    bool LayoutAmbiguous,
    bool ReportRowCountMatches,
    string HeaderProfileSetSha256,
    string InvalidObjects,
    string SampleMembers);

public sealed record MorphElfHeaderAuditSummaryRow(
    string Scope,
    int Layouts,
    int CatalogRows,
    int ObservedRows,
    int ValidatedRows,
    int InvalidRows,
    int HeaderVariableLayouts,
    int HeaderVariableAmbiguousLayouts,
    int HeaderVariableStableLayouts,
    int SymbolMetadataVariableLayouts,
    int ObservationRowCountMismatches,
    int ReportRowCountMismatches,
    string HeaderCoreProfileCountDistribution);

/// <summary>
/// Compares only ELF header and morph-symbol metadata for the already
/// catalogued morph objects. It does not interpret section payloads,
/// relocations, named strings, poses, bones, weights, frames or GPU commands.
/// </summary>
public static class MorphElfHeaderAudit
{
    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;

    public static IReadOnlyList<MorphElfHeaderObservation> Scan(
        GameAssetStore store,
        string modelArchivePath,
        IEnumerable<MorphShpxCatalogRow> catalogRows)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentNullException.ThrowIfNull(catalogRows);

        var materialized = catalogRows.ToArray();
        var topLevel = BigArchiveReader.Inspect(store.OpenRaw(modelArchivePath));
        var nestedByName = topLevel.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(entry => entry.Name, entry => entry, StringComparer.OrdinalIgnoreCase);
        var observations = new List<MorphElfHeaderObservation>(materialized.Length);

        foreach (var nestedGroup in materialized.GroupBy(row => row.NestedBig, StringComparer.OrdinalIgnoreCase))
        {
            if (!nestedByName.ContainsKey(nestedGroup.Key))
            {
                foreach (var row in nestedGroup)
                {
                    observations.Add(Invalid(row, "nested BIG entry was not found"));
                }
                continue;
            }

            try
            {
                var expanded = ReadExpandedNested(store, modelArchivePath, nestedGroup.Key);
                var nestedArchive = BigArchiveReader.Inspect(expanded);
                var entries = nestedArchive.Entries
                    .ToDictionary(entry => entry.Name, entry => entry, StringComparer.OrdinalIgnoreCase);
                foreach (var row in nestedGroup)
                {
                    if (!entries.TryGetValue(row.MorphObject, out var morphEntry))
                    {
                        observations.Add(Invalid(row, "morph object entry was not found"));
                        continue;
                    }

                    try
                    {
                        var morphData = BigArchiveReader.ReadEntry(expanded, morphEntry.Name);
                        var elf = ElfReader.Inspect(morphData);
                        var morphSymbols = elf.Symbols
                            .Where(symbol => symbol.Name.StartsWith("__Morph:::", StringComparison.Ordinal))
                            .ToArray();
                        var morphSymbolSignature = string.Join("|", morphSymbols
                            .OrderBy(symbol => symbol.TableSectionIndex)
                            .ThenBy(symbol => symbol.Index)
                            .Select(symbol =>
                                $"table={symbol.TableSectionIndex};index={symbol.Index};section={symbol.SectionIndex};value=0x{symbol.Value:X};size=0x{symbol.Size:X};info=0x{symbol.Info:X2}"));
                        observations.Add(new MorphElfHeaderObservation(
                            row.NestedBig,
                            row.MorphObject,
                            row.LayoutKey,
                            true,
                            morphSymbols.Length > 0,
                            GetHeaderCoreSignature(elf),
                            GetHeaderRangeSignature(elf),
                            morphSymbolSignature,
                            morphSymbols.Length > 0 ? null : "__Morph::: symbol was not found"));
                    }
                    catch (Exception exception) when (IsReadFailure(exception))
                    {
                        observations.Add(Invalid(row, exception.Message));
                    }
                }
            }
            catch (Exception exception) when (IsReadFailure(exception))
            {
                foreach (var row in nestedGroup)
                {
                    observations.Add(Invalid(row, exception.Message));
                }
            }
        }

        return observations;
    }

    public static IReadOnlyList<MorphElfHeaderAuditRow> Audit(
        IEnumerable<MorphShpxCatalogRow> catalogRows,
        IEnumerable<MorphAmbiguityReportRow> ambiguityRows,
        IEnumerable<MorphElfHeaderObservation> observations)
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
                : Array.Empty<MorphElfHeaderObservation>();
            var coreProfiles = observed
                .Where(row => row.Validated)
                .Select(row => row.HeaderCoreSignature)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var rangeProfiles = observed
                .Where(row => row.Validated)
                .Select(row => row.HeaderRangeSignature)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var symbolProfiles = observed
                .Where(row => row.Validated && row.HasMorphSymbol)
                .Select(row => row.MorphSymbolMetadataSignature)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var report = ambiguity.Length == 1 ? ambiguity[0] : null;
            return new MorphElfHeaderAuditRow(
                layoutKey,
                Sha256(layoutKey),
                catalog.Length,
                observed.Length,
                observed.Count(row => row.Validated),
                observed.Count(row => row.HasMorphSymbol),
                observed.Count(row => !row.Validated || !row.HasMorphSymbol),
                coreProfiles.Length,
                rangeProfiles.Length,
                symbolProfiles.Length,
                coreProfiles.Length > 1 || rangeProfiles.Length > 1,
                symbolProfiles.Length > 1,
                report?.LayoutAmbiguous ?? false,
                report is not null && report.LayoutRows == catalog.Length,
                SetSha256(coreProfiles.Concat(rangeProfiles)),
                string.Join(",", observed
                    .Where(row => !row.Validated || !row.HasMorphSymbol)
                    .Select(row => $"{row.NestedBig}/{row.MorphObject}")
                    .Distinct(StringComparer.OrdinalIgnoreCase)),
                string.Join(",", observed
                    .Take(3)
                    .Select(row => $"{row.NestedBig}/{row.MorphObject}")));
        }).ToArray();
    }

    public static IReadOnlyList<MorphElfHeaderAuditSummaryRow> Summarize(
        IEnumerable<MorphElfHeaderAuditRow> rows)
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
        IEnumerable<MorphElfHeaderAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        WriteLines(reportPath, new[]
        {
            "morph_structural_layout",
            "layout_key_sha256",
            "catalog_rows",
            "observed_rows",
            "validated_rows",
            "morph_symbol_rows",
            "invalid_rows",
            "header_core_profiles",
            "header_range_profiles",
            "morph_symbol_metadata_profiles",
            "header_structure_variable",
            "morph_symbol_metadata_variable",
            "layout_ambiguous",
            "report_row_count_matches",
            "header_profile_set_sha256",
            "invalid_objects",
            "sample_members",
        }, materialized.Select(ToFields));
        WriteLines(summaryPath, new[]
        {
            "scope",
            "layouts",
            "catalog_rows",
            "observed_rows",
            "validated_rows",
            "invalid_rows",
            "header_variable_layouts",
            "header_variable_ambiguous_layouts",
            "header_variable_stable_layouts",
            "symbol_metadata_variable_layouts",
            "observation_row_count_mismatches",
            "report_row_count_mismatches",
            "header_core_profile_count_distribution",
        }, Summarize(materialized).Select(ToSummaryFields));
    }

    private static MorphElfHeaderObservation Invalid(MorphShpxCatalogRow row, string issue) =>
        new(row.NestedBig, row.MorphObject, row.LayoutKey, false, false, string.Empty, string.Empty, string.Empty, issue);

    private static byte[] ReadExpandedNested(GameAssetStore store, string archivePath, string entryName)
    {
        using var stream = store.OpenBigEntry(archivePath, entryName);
        if (stream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed nested BIG exceeds the 64 MiB safety limit.");
        }

        var compressed = new byte[checked((int)stream.Length)];
        stream.ReadExactly(compressed);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded nested BIG exceeds the 64 MiB safety limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        return RefPackDecoder.Decode(compressedMemory, expandedSize).Data;
    }

    private static string GetHeaderCoreSignature(ElfInspection elf) => string.Join(";", new[]
    {
        $"class={(elf.Is32Bit ? "ELF32" : "other")}",
        $"endian={(elf.IsLittleEndian ? "LE" : "other")}",
        $"type=0x{elf.Type:X4}",
        $"machine=0x{elf.Machine:X4}",
        $"section_size={elf.SectionHeaderSize}",
        $"section_count={elf.SectionCount}",
        $"table_within={elf.SectionTableWithinAsset}",
    });

    private static string GetHeaderRangeSignature(ElfInspection elf) =>
        $"section_offset=0x{elf.SectionHeaderOffset:X};section_bytes=0x{(uint)elf.SectionHeaderSize * elf.SectionCount:X}";

    private static MorphElfHeaderAuditSummaryRow CreateSummary(
        string scope,
        IReadOnlyList<MorphElfHeaderAuditRow> rows) =>
        new(
            scope,
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Sum(row => row.ObservedRows),
            rows.Sum(row => row.ValidatedRows),
            rows.Sum(row => row.InvalidRows),
            rows.Count(row => row.HeaderStructureVariable),
            rows.Count(row => row.HeaderStructureVariable && row.LayoutAmbiguous),
            rows.Count(row => row.HeaderStructureVariable && !row.LayoutAmbiguous),
            rows.Count(row => row.MorphSymbolMetadataVariable),
            rows.Count(row => row.ObservedRows != row.CatalogRows),
            rows.Count(row => !row.ReportRowCountMatches),
            Distribution(rows.Select(row => row.HeaderCoreProfiles)));

    private static string[] ToFields(MorphElfHeaderAuditRow row) => new[]
    {
        Escape(row.LayoutKey), row.LayoutKeySha256,
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ObservedRows.ToString(CultureInfo.InvariantCulture),
        row.ValidatedRows.ToString(CultureInfo.InvariantCulture),
        row.MorphSymbolRows.ToString(CultureInfo.InvariantCulture),
        row.InvalidRows.ToString(CultureInfo.InvariantCulture),
        row.HeaderCoreProfiles.ToString(CultureInfo.InvariantCulture),
        row.HeaderRangeProfiles.ToString(CultureInfo.InvariantCulture),
        row.MorphSymbolMetadataProfiles.ToString(CultureInfo.InvariantCulture),
        row.HeaderStructureVariable.ToString(),
        row.MorphSymbolMetadataVariable.ToString(),
        row.LayoutAmbiguous.ToString(),
        row.ReportRowCountMatches.ToString(),
        row.HeaderProfileSetSha256,
        Escape(row.InvalidObjects), Escape(row.SampleMembers),
    };

    private static string[] ToSummaryFields(MorphElfHeaderAuditSummaryRow row) => new[]
    {
        row.Scope,
        row.Layouts.ToString(CultureInfo.InvariantCulture),
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ObservedRows.ToString(CultureInfo.InvariantCulture),
        row.ValidatedRows.ToString(CultureInfo.InvariantCulture),
        row.InvalidRows.ToString(CultureInfo.InvariantCulture),
        row.HeaderVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.HeaderVariableAmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
        row.HeaderVariableStableLayouts.ToString(CultureInfo.InvariantCulture),
        row.SymbolMetadataVariableLayouts.ToString(CultureInfo.InvariantCulture),
        row.ObservationRowCountMismatches.ToString(CultureInfo.InvariantCulture),
        row.ReportRowCountMismatches.ToString(CultureInfo.InvariantCulture),
        Escape(row.HeaderCoreProfileCountDistribution),
    };

    private static void WriteLines(string path, IEnumerable<string> header, IEnumerable<string[]> rows)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        File.WriteAllLines(path, new[] { string.Join("\t", header) }
            .Concat(rows.Select(row => string.Join("\t", row))),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool IsReadFailure(Exception exception) => exception is
        InvalidDataException or EndOfStreamException or OverflowException or
        ArgumentOutOfRangeException or IOException or FileNotFoundException;

    private static string Distribution(IEnumerable<int> values) => string.Join(",", values
        .GroupBy(value => value)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}"));

    private static string SetSha256(IEnumerable<string> values) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", values.OrderBy(value => value, StringComparer.Ordinal)))));

    private static string Sha256(string value) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Escape(string value) => value
        .Replace("\t", "\\t", StringComparison.Ordinal)
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);
}
