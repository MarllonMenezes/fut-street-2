using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankElfSectionRow(
    string CatalogEntry,
    string BankName,
    int SectionIndex,
    string SectionName,
    uint Type,
    uint Flags,
    uint Offset,
    uint Size,
    uint Link,
    uint Info,
    uint EntrySize,
    int RelocationCount,
    string RelocationOffsetRange,
    string RelocationTypes);

public sealed record AnimationBankElfStructureProfile(
    string CatalogEntry,
    string BankName,
    int CatalogFileCount,
    bool ElfValid,
    ushort Machine,
    int SectionCount,
    int SymbolCount,
    int RelocationCount,
    int FileBackedSectionCount,
    ulong FileBackedBytes,
    ulong TextBytes,
    ulong RodataBytes,
    ulong DataBytes,
    ulong BssBytes,
    string SectionProfileSha256,
    string RelocationProfileSha256,
    string NamedSectionSizes,
    string RelocationTargetCounts,
    string AnimationBankSymbolMetadata,
    string AnimationBankSymbolTailDistance);

public sealed record AnimationBankElfStructureReport(
    AnimationBankElfStructureProfile Profile,
    IReadOnlyList<AnimationBankElfSectionRow> Sections);

/// <summary>
/// Materializes ELF section-header and relocation metadata for animation
/// banks. It deliberately does not inspect payload words or assign meaning to
/// sections, symbols, relocations, catalog names, frames, poses, or timing.
/// </summary>
public static class AnimationBankElfStructureReporter
{
    public static AnimationBankElfStructureReport Build(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ElfInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(inspection);

        var sectionRows = inspection.Sections
            .Select((section, index) => BuildSectionRow(catalogEntry, catalog.BankName, index, section, inspection))
            .ToArray();
        var fileBackedSections = inspection.Sections
            .Where(section => section.Type != 8)
            .ToArray();
        var namedSectionSizes = string.Join("|", inspection.Sections
            .Where(section => section.Name.Length > 0)
            .Select(section => $"{section.Name}=0x{section.Size:X}"));
        var relocationTargetCounts = string.Join("|", inspection.Relocations
            .GroupBy(relocation => relocation.TargetSectionIndex)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var sectionName = group.Key >= 0 && group.Key < inspection.Sections.Count
                    ? inspection.Sections[group.Key].Name
                    : string.Empty;
                return $"{group.Key}:{sectionName}={group.Count()}";
            }));
        var bankSymbols = inspection.Symbols
            .Where(symbol => symbol.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal))
            .OrderBy(symbol => symbol.SectionIndex)
            .ThenBy(symbol => symbol.Value)
            .ThenBy(symbol => symbol.Name, StringComparer.Ordinal)
            .Select(symbol =>
                $"{symbol.Name};section={symbol.SectionIndex};value=0x{symbol.Value:X};size=0x{symbol.Size:X};info=0x{symbol.Info:X2}")
            .ToArray();
        var dataSection = inspection.Sections.FirstOrDefault(section =>
            string.Equals(section.Name, ".data", StringComparison.Ordinal));
        var bankSymbol = inspection.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal));
        var symbolTailDistance = string.Empty;
        if (dataSection is not null && bankSymbol is not null &&
            bankSymbol.Value <= dataSection.Size &&
            bankSymbol.Size <= dataSection.Size - bankSymbol.Value)
        {
            symbolTailDistance = (dataSection.Size - bankSymbol.Value - bankSymbol.Size)
                .ToString(CultureInfo.InvariantCulture);
        }

        var textBytes = GetSectionSize(inspection, ".text");
        var rodataBytes = GetSectionSize(inspection, ".rodata");
        var dataBytes = GetSectionSize(inspection, ".data");
        var bssBytes = inspection.Sections
            .Where(section => section.Type == 8 || string.Equals(section.Name, ".bss", StringComparison.Ordinal))
            .Aggregate<ElfSection, ulong>(0, (total, section) => total + section.Size);
        var profile = new AnimationBankElfStructureProfile(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            true,
            inspection.Machine,
            inspection.Sections.Count,
            inspection.Symbols.Count,
            inspection.Relocations.Count,
            fileBackedSections.Length,
            fileBackedSections.Aggregate<ElfSection, ulong>(0, (total, section) => total + section.Size),
            textBytes,
            rodataBytes,
            dataBytes,
            bssBytes,
            HashSectionProfile(inspection),
            HashRelocationProfile(inspection),
            namedSectionSizes,
            relocationTargetCounts,
            string.Join("|", bankSymbols),
            symbolTailDistance);
        return new AnimationBankElfStructureReport(profile, sectionRows);
    }

    public static AnimationBankElfStructureProfile CreateInvalidProfile(
        string catalogEntry,
        string bankName,
        int catalogFileCount)
    {
        return new AnimationBankElfStructureProfile(
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
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty);
    }

    public static void WriteProfiles(
        string path,
        IEnumerable<AnimationBankElfStructureProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(profiles);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "catalog_file_count",
                "elf_valid",
                "elf_machine",
                "section_count",
                "symbol_count",
                "relocation_count",
                "file_backed_section_count",
                "file_backed_bytes",
                "text_bytes",
                "rodata_bytes",
                "data_bytes",
                "bss_bytes",
                "section_profile_sha256",
                "relocation_profile_sha256",
                "named_section_sizes",
                "relocation_target_counts",
                "animation_bank_symbol_metadata",
                "animation_bank_symbol_tail_distance",
            }),
        };
        lines.AddRange(profiles.Select(profile => string.Join("\t", new[]
        {
            Tsv(profile.CatalogEntry),
            Tsv(profile.BankName),
            profile.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            profile.ElfValid.ToString(),
            profile.ElfValid ? profile.Machine.ToString(CultureInfo.InvariantCulture) : string.Empty,
            profile.SectionCount.ToString(CultureInfo.InvariantCulture),
            profile.SymbolCount.ToString(CultureInfo.InvariantCulture),
            profile.RelocationCount.ToString(CultureInfo.InvariantCulture),
            profile.FileBackedSectionCount.ToString(CultureInfo.InvariantCulture),
            profile.FileBackedBytes.ToString(CultureInfo.InvariantCulture),
            profile.TextBytes.ToString(CultureInfo.InvariantCulture),
            profile.RodataBytes.ToString(CultureInfo.InvariantCulture),
            profile.DataBytes.ToString(CultureInfo.InvariantCulture),
            profile.BssBytes.ToString(CultureInfo.InvariantCulture),
            profile.SectionProfileSha256,
            profile.RelocationProfileSha256,
            Tsv(profile.NamedSectionSizes),
            Tsv(profile.RelocationTargetCounts),
            Tsv(profile.AnimationBankSymbolMetadata),
            Tsv(profile.AnimationBankSymbolTailDistance),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSections(
        string path,
        IEnumerable<AnimationBankElfSectionRow> sections)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(sections);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "section_index",
                "section_name",
                "type",
                "flags",
                "offset",
                "size",
                "link",
                "info",
                "entry_size",
                "relocation_count",
                "relocation_offset_range",
                "relocation_types",
            }),
        };
        lines.AddRange(sections.Select(section => string.Join("\t", new[]
        {
            Tsv(section.CatalogEntry),
            Tsv(section.BankName),
            section.SectionIndex.ToString(CultureInfo.InvariantCulture),
            Tsv(section.SectionName),
            $"0x{section.Type:X}",
            $"0x{section.Flags:X}",
            $"0x{section.Offset:X}",
            $"0x{section.Size:X}",
            section.Link.ToString(CultureInfo.InvariantCulture),
            section.Info.ToString(CultureInfo.InvariantCulture),
            $"0x{section.EntrySize:X}",
            section.RelocationCount.ToString(CultureInfo.InvariantCulture),
            Tsv(section.RelocationOffsetRange),
            Tsv(section.RelocationTypes),
        })));
        WriteLines(path, lines);
    }

    private static AnimationBankElfSectionRow BuildSectionRow(
        string catalogEntry,
        string bankName,
        int sectionIndex,
        ElfSection section,
        ElfInspection inspection)
    {
        var relocations = inspection.Relocations
            .Where(relocation => relocation.TargetSectionIndex == sectionIndex)
            .OrderBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .ToArray();
        var range = relocations.Length == 0
            ? string.Empty
            : $"0x{relocations[0].Offset:X}-0x{relocations[^1].Offset:X}";
        var types = string.Join(",", relocations
            .Select(relocation => $"0x{relocation.Type:X}")
            .Distinct(StringComparer.Ordinal));
        return new AnimationBankElfSectionRow(
            catalogEntry,
            bankName,
            sectionIndex,
            section.Name,
            section.Type,
            section.Flags,
            section.Offset,
            section.Size,
            section.Link,
            section.Info,
            section.EntrySize,
            relocations.Length,
            range,
            types);
    }

    private static ulong GetSectionSize(ElfInspection inspection, string sectionName) =>
        inspection.Sections
            .Where(section => string.Equals(section.Name, sectionName, StringComparison.Ordinal))
            .Aggregate<ElfSection, ulong>(0, (total, section) => total + section.Size);

    private static string HashSectionProfile(ElfInspection inspection)
    {
        var text = string.Join("|", inspection.Sections.Select((section, index) =>
            $"{index}:{section.Name};type=0x{section.Type:X};flags=0x{section.Flags:X};offset=0x{section.Offset:X};size=0x{section.Size:X};link={section.Link};info={section.Info};entry=0x{section.EntrySize:X}"));
        return HashText(text);
    }

    private static string HashRelocationProfile(ElfInspection inspection)
    {
        var relativeBases = inspection.Relocations
            .GroupBy(relocation => relocation.TargetSectionIndex)
            .ToDictionary(group => group.Key, group => group.Min(relocation => relocation.Offset));
        var text = string.Join("|", inspection.Relocations
            .OrderBy(relocation => relocation.TargetSectionIndex)
            .ThenBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .Select(relocation =>
                $"target={relocation.TargetSectionIndex};offset=0x{relocation.Offset - relativeBases[relocation.TargetSectionIndex]:X};type=0x{relocation.Type:X}"));
        return HashText(text);
    }

    private static string HashText(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Tsv(string? value) =>
        value is null ? string.Empty : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

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
