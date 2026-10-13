using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankDataRelocationAuditRow(
    string CatalogEntry,
    string BankName,
    int CatalogFileCount,
    bool ElfValid,
    uint DataSize,
    int DataRelocationCount,
    int DistinctRelativeOffsetCount,
    uint FirstOffset,
    uint LastOffset,
    int DistinctDeltaCount,
    uint MinimumDelta,
    uint MaximumDelta,
    int RepeatedDeltaCount,
    string DeltaHistogram,
    string OffsetProfileSha256);

/// <summary>
/// Audits only the relative offsets and adjacent gaps of relocations targeting
/// the ELF .data section of an animation bank. The report is a layout
/// observation: it does not inspect payload words or assign frame, pose, rig,
/// timing, skinning, material, UV, GPU, or command semantics.
/// </summary>
public static class AnimationBankDataRelocationAudit
{
    public static AnimationBankDataRelocationAuditRow Build(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ElfInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(inspection);

        var dataSectionIndex = -1;
        ElfSection? dataSection = null;
        for (var index = 0; index < inspection.Sections.Count; index++)
        {
            if (string.Equals(inspection.Sections[index].Name, ".data", StringComparison.Ordinal))
            {
                dataSectionIndex = index;
                dataSection = inspection.Sections[index];
                break;
            }
        }

        if (dataSection is null)
        {
            return new AnimationBankDataRelocationAuditRow(
                catalogEntry,
                catalog.BankName,
                catalog.Files.Count,
                true,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                HashText(string.Empty));
        }

        var relocations = inspection.Relocations
            .Where(relocation => relocation.TargetSectionIndex == dataSectionIndex)
            .OrderBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .ToArray();
        var offsets = relocations
            .Select(relocation => relocation.Offset)
            .ToArray();
        var deltas = offsets
            .Zip(offsets.Skip(1), (first, second) => second - first)
            .ToArray();
        var deltaGroups = deltas
            .GroupBy(delta => delta)
            .OrderBy(group => group.Key)
            .ToArray();
        var offsetProfile = string.Join("|", relocations
            .Select(relocation => $"offset=0x{relocation.Offset:X};type=0x{relocation.Type:X}"));

        return new AnimationBankDataRelocationAuditRow(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            true,
            dataSection.Size,
            relocations.Length,
            offsets.Distinct().Count(),
            offsets.Length == 0 ? 0 : offsets[0],
            offsets.Length == 0 ? 0 : offsets[^1],
            deltaGroups.Length,
            deltas.Length == 0 ? 0 : deltas.Min(),
            deltas.Length == 0 ? 0 : deltas.Max(),
            deltaGroups.Count(group => group.Count() > 1),
            string.Join(",", deltaGroups.Select(group => $"0x{group.Key:X}={group.Count()}")),
            HashText(offsetProfile));
    }

    public static AnimationBankDataRelocationAuditRow CreateInvalid(
        string catalogEntry,
        string bankName,
        int catalogFileCount)
    {
        return new AnimationBankDataRelocationAuditRow(
            catalogEntry,
            bankName,
            catalogFileCount,
            false,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            string.Empty,
            string.Empty);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankDataRelocationAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "catalog_file_count",
                "elf_valid",
                "data_size",
                "data_relocation_count",
                "distinct_relative_offset_count",
                "first_offset",
                "last_offset",
                "distinct_delta_count",
                "minimum_delta",
                "maximum_delta",
                "repeated_delta_count",
                "delta_histogram",
                "offset_profile_sha256",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            row.ElfValid.ToString(),
            row.DataSize.ToString(CultureInfo.InvariantCulture),
            row.DataRelocationCount.ToString(CultureInfo.InvariantCulture),
            row.DistinctRelativeOffsetCount.ToString(CultureInfo.InvariantCulture),
            $"0x{row.FirstOffset:X}",
            $"0x{row.LastOffset:X}",
            row.DistinctDeltaCount.ToString(CultureInfo.InvariantCulture),
            $"0x{row.MinimumDelta:X}",
            $"0x{row.MaximumDelta:X}",
            row.RepeatedDeltaCount.ToString(CultureInfo.InvariantCulture),
            Tsv(row.DeltaHistogram),
            row.OffsetProfileSha256,
        })));
        WriteLines(path, lines);
    }

    private static string HashText(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Tsv(string? value) => value is null
        ? string.Empty
        : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
