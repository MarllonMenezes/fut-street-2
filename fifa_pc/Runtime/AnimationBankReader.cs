using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationCatalogEntry(
    string Name,
    uint Timestamp,
    string SourceLine);

public sealed record AnimationBankCatalog(
    string VersionTag,
    string BankName,
    string Directory,
    int DeclaredFileCount,
    IReadOnlyList<AnimationCatalogEntry> Files)
{
    public bool DeclaredFileCountMatches => DeclaredFileCount == Files.Count;
}

/// <summary>
/// Parses the text catalog sidecars in data/anims.big. The catalog and
/// __AnimationBank symbol establish bank identity only; file entries are not
/// interpreted as frame, pose, rig, timing, or animation records.
/// </summary>
public static class AnimationBankReader
{
    private const string AnimationBankSymbolPrefix = "__AnimationBank:::";

    public static AnimationBankCatalog Parse(ReadOnlySpan<byte> data)
    {
        var text = Encoding.ASCII.GetString(data);
        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        var versionLine = FindRequiredLine(lines, "[Version ", "version");
        var versionTag = versionLine.Trim()[9..^1].Trim();
        var bankLine = FindRequiredLine(lines, "[Bank]", "bank");
        var bankIndex = Array.IndexOf(lines, bankLine);
        var bankName = FindNextValue(lines, bankIndex + 1, "bank name");
        var directoryLine = FindRequiredLine(lines, "[Directory]", "directory");
        var directoryIndex = Array.IndexOf(lines, directoryLine);
        var directory = FindNextValue(lines, directoryIndex + 1, "directory");
        var filesLineIndex = FindLineIndex(lines, "[Files]", "files");
        var filesLine = lines[filesLineIndex].Trim();
        var declaredFileCountText = filesLine["[Files]".Length..].Trim();
        if (!int.TryParse(
                declaredFileCountText,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var declaredFileCount) ||
            declaredFileCount < 0)
        {
            throw new InvalidDataException("Animation catalog has an invalid [Files] count.");
        }

        var files = new List<AnimationCatalogEntry>(declaredFileCount);
        for (var index = filesLineIndex + 1; index < lines.Length; index++)
        {
            var sourceLine = lines[index].Trim();
            if (sourceLine.Length == 0)
            {
                continue;
            }

            var columns = sourceLine.Split(
                [' ', '\t'],
                StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length < 2 ||
                !uint.TryParse(
                    columns[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var timestamp) ||
                columns[0].StartsWith("[", StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Animation catalog file row is malformed: {sourceLine}");
            }

            files.Add(new AnimationCatalogEntry(columns[0], timestamp, sourceLine));
        }

        return new AnimationBankCatalog(
            versionTag,
            bankName,
            directory,
            declaredFileCount,
            files);
    }

    public static IReadOnlyList<string> ParseBankList(ReadOnlySpan<byte> data)
    {
        var text = Encoding.ASCII.GetString(data);
        return text.Split(
                [' ', '\t', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> GetAnimationBankSymbolNames(
        IEnumerable<ElfSymbol> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        return symbols
            .Select(symbol => symbol.Name)
            .Where(name => name.StartsWith(AnimationBankSymbolPrefix, StringComparison.Ordinal))
            .Select(name => name[AnimationBankSymbolPrefix.Length..])
            .Where(name => name.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Hashes only ELF section metadata and relocation layout. It deliberately
    /// omits payload words and assigns no meaning to any relocation target.
    /// </summary>
    public static string GetStructuralLayoutSignature(ElfInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var sectionText = string.Join("|", inspection.Sections.Select((section, index) =>
            $"{index}:{section.Name};type=0x{section.Type:X};flags=0x{section.Flags:X};offset=0x{section.Offset:X};size=0x{section.Size:X};link={section.Link};info={section.Info};entry=0x{section.EntrySize:X}"));
        var relativeBases = inspection.Relocations
            .GroupBy(relocation => relocation.TargetSectionIndex)
            .ToDictionary(group => group.Key, group => group.Min(relocation => relocation.Offset));
        var relocationText = string.Join("|", inspection.Relocations
            .OrderBy(relocation => relocation.TargetSectionIndex)
            .ThenBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .Select(relocation =>
                $"target={relocation.TargetSectionIndex};offset=0x{relocation.Offset - relativeBases[relocation.TargetSectionIndex]:X};type=0x{relocation.Type:X}"));
        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes($"sections={sectionText};relocations={relocationText}")));
    }

    /// <summary>
    /// Hashes the validated .data section after masking only four-byte words
    /// addressed by relocations targeting that section. This is a byte-level
    /// normalization, not a decoder of the bank payload.
    /// </summary>
    public static string GetNormalizedDataPayloadSignature(
        ReadOnlySpan<byte> combinedData,
        ElfInspection inspection)
    {
        return Convert.ToHexString(GetNormalizedDataPayload(combinedData, inspection));
    }

    /// <summary>
    /// Returns the validated .data bytes after masking only the four-byte words
    /// addressed by relocations targeting that section. The result is useful for
    /// aligned byte/word comparison only; it is not an animation payload decoder.
    /// </summary>
    public static byte[] GetNormalizedDataPayload(
        ReadOnlySpan<byte> combinedData,
        ElfInspection inspection)
    {
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

        if (dataSection is null ||
            (ulong)dataSection.Offset + dataSection.Size > (ulong)combinedData.Length)
        {
            throw new InvalidDataException("Animation bank .data section is outside the ELF payload.");
        }

        var payload = combinedData
            .Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size))
            .ToArray();
        foreach (var relocation in inspection.Relocations.Where(item =>
                     item.TargetSectionIndex == dataSectionIndex))
        {
            if ((ulong)relocation.Offset + sizeof(uint) > (ulong)payload.Length)
            {
                throw new InvalidDataException("Animation bank relocation is outside .data.");
            }

            payload.AsSpan(checked((int)relocation.Offset), sizeof(uint)).Clear();
        }

        return payload;
    }

    /// <summary>
    /// Measures the longest contiguous run of equal fixed-size units in two
    /// aligned payloads. This is a repeatability metric only: it does not
    /// identify record boundaries or assign frame, pose, rig or timing
    /// semantics to the matching bytes.
    /// </summary>
    public static int GetLongestEqualRun(
        ReadOnlySpan<byte> first,
        ReadOnlySpan<byte> second,
        int unitSize)
    {
        if (unitSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(unitSize));
        }

        var unitCount = Math.Min(first.Length, second.Length) / unitSize;
        var currentRun = 0;
        var longestRun = 0;
        for (var unitIndex = 0; unitIndex < unitCount; unitIndex++)
        {
            var offset = checked(unitIndex * unitSize);
            if (first.Slice(offset, unitSize).SequenceEqual(second.Slice(offset, unitSize)))
            {
                currentRun++;
                longestRun = Math.Max(longestRun, currentRun);
            }
            else
            {
                currentRun = 0;
            }
        }

        return longestRun;
    }

    private static string FindRequiredLine(
        IReadOnlyList<string> lines,
        string prefix,
        string label)
    {
        var index = FindLineIndex(lines, prefix, label);
        return lines[index];
    }

    private static int FindLineIndex(
        IReadOnlyList<string> lines,
        string prefix,
        string label)
    {
        for (var index = 0; index < lines.Count; index++)
        {
            if (lines[index].TrimStart().StartsWith(prefix, StringComparison.Ordinal))
            {
                return index;
            }
        }

        throw new InvalidDataException($"Animation catalog has no {label} section.");
    }

    private static string FindNextValue(
        IReadOnlyList<string> lines,
        int startIndex,
        string label)
    {
        for (var index = startIndex; index < lines.Count; index++)
        {
            var value = lines[index].Trim();
            if (value.Length > 0)
            {
                return value;
            }
        }

        throw new InvalidDataException($"Animation catalog has no {label} value.");
    }
}
