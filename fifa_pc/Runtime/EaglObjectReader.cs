using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglVector3(float X, float Y, float Z);
public sealed record EaglAxisAlignedBounds(EaglVector3 Minimum, EaglVector3 Maximum);

public sealed record EaglObjectInspection(
    ElfInspection Elf,
    IReadOnlyList<string> GeometrySymbols,
    EaglAxisAlignedBounds? Bounds,
    string? BoundsSymbol,
    string? BoundsIssue);

/// <summary>
/// Reads geometry metadata that is explicitly named in EA EAGL object symbols.
/// This is not a mesh or animation decoder.
/// </summary>
public static class EaglObjectReader
{
    private const int BoundsByteSize = 6 * sizeof(float);
    private const int MaximumCombinedObjectSize = 256 * 1024 * 1024;

    /// <summary>
    /// Returns a compact signature of the parsed companion ELF. It contains
    /// only section/symbol/relocation counts, byte totals, named geometry
    /// symbol classes, and bounds presence; it does not bind a morph to this
    /// geometry or assign meaning to any unknown field.
    /// </summary>
    public static string GetStructuralSignature(EaglObjectInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var fileBackedSections = inspection.Elf.Sections.Where(section => section.Type != 8).ToArray();
        var fileBackedBytes = fileBackedSections.Aggregate<ElfSection, ulong>(
            0,
            (total, section) => total + section.Size);
        var geometryClasses = inspection.GeometrySymbols
            .Select(GetGeometrySymbolClass)
            .GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}={group.Count()}");
        return $"sections={inspection.Elf.Sections.Count};file_sections={fileBackedSections.Length};file_bytes=0x{fileBackedBytes:X};symbols={inspection.Elf.Symbols.Count};relocations={inspection.Elf.Relocations.Count};geometry={string.Join(",", geometryClasses)};bounds={(inspection.Bounds is null ? "none" : "present")}";
    }

    /// <summary>
    /// Returns a layout-only hash of the validated ELF metadata. The canonical
    /// input contains section headers, named geometry-symbol classes with raw
    /// positions/sizes, and relocation target sections with relative offsets
    /// and types. Symbol values are not decoded as commands, attributes, bones,
    /// or animation records.
    /// </summary>
    public static string GetStructuralLayoutSignature(EaglObjectInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var sections = string.Join("|", inspection.Elf.Sections.Select((section, index) =>
            $"{index}:type=0x{section.Type:X};flags=0x{section.Flags:X};offset=0x{section.Offset:X};size=0x{section.Size:X};link={section.Link};info={section.Info};entry=0x{section.EntrySize:X}"));
        var geometrySymbols = inspection.Elf.Symbols
            .Where(symbol => IsGeometrySymbol(symbol.Name))
            .OrderBy(symbol => symbol.SectionIndex)
            .ThenBy(symbol => symbol.Value)
            .ThenBy(symbol => symbol.Name, StringComparer.Ordinal)
            .Select(symbol =>
                $"{GetGeometrySymbolClass(symbol.Name)}@section={symbol.SectionIndex};value=0x{symbol.Value:X};size=0x{symbol.Size:X}");
        var relocations = inspection.Elf.Relocations
            .GroupBy(relocation => relocation.TargetSectionIndex)
            .ToDictionary(group => group.Key, group => group.Min(relocation => relocation.Offset));
        var relocationLayout = inspection.Elf.Relocations
            .OrderBy(relocation => relocation.TargetSectionIndex)
            .ThenBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .Select(relocation =>
                $"target={relocation.TargetSectionIndex};offset=0x{relocation.Offset - relocations[relocation.TargetSectionIndex]:X};type=0x{relocation.Type:X}");
        var canonical = $"sections={sections};geometry={string.Join("|", geometrySymbols)};relocations={string.Join("|", relocationLayout)}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return $"sections={inspection.Elf.Sections.Count};geometry_symbols={inspection.Elf.Symbols.Where(symbol => IsGeometrySymbol(symbol.Name)).Count()};relocations={inspection.Elf.Relocations.Count};sha256={hash}";
    }

    private static string GetGeometrySymbolClass(string symbolName) =>
        symbolName.StartsWith("__BBOX", StringComparison.Ordinal) ? "__BBOX*" :
        symbolName.StartsWith("__VB", StringComparison.Ordinal) ? "__VB*" :
        symbolName.StartsWith("__RM", StringComparison.Ordinal) ? "__RM*" :
        symbolName.StartsWith("__Model", StringComparison.Ordinal) ? "__Model*" :
        "other";

    private static bool IsGeometrySymbol(string symbolName) =>
        symbolName.StartsWith("__BBOX", StringComparison.Ordinal) ||
        symbolName.StartsWith("__VB", StringComparison.Ordinal) ||
        symbolName.StartsWith("__RM", StringComparison.Ordinal) ||
        symbolName.StartsWith("__Model", StringComparison.Ordinal);

    /// <summary>
    /// Inspects an EAGL object whose ELF section table and link metadata are
    /// stored in a companion payload appended to the primary object.
    /// </summary>
    public static EaglObjectInspection Inspect(ReadOnlySpan<byte> objectData, ReadOnlySpan<byte> companionData)
    {
        return Inspect(Combine(objectData, companionData));
    }

    public static byte[] Combine(ReadOnlySpan<byte> objectData, ReadOnlySpan<byte> companionData)
    {
        var combinedLength = (long)objectData.Length + companionData.Length;
        if (combinedLength > MaximumCombinedObjectSize)
        {
            throw new InvalidDataException("Combined EAGL object exceeds the supported size limit.");
        }

        var combined = new byte[checked((int)combinedLength)];
        objectData.CopyTo(combined);
        companionData.CopyTo(combined.AsSpan(objectData.Length));
        return combined;
    }

    public static EaglObjectInspection Inspect(ReadOnlySpan<byte> data)
    {
        var elf = ElfReader.Inspect(data);
        var geometrySymbols = elf.Symbols
            .Where(symbol => IsGeometrySymbol(symbol.Name))
            .Select(symbol => symbol.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var bboxSymbols = elf.Symbols.Where(symbol =>
            symbol.Name.StartsWith("__BBOX", StringComparison.Ordinal));
        string? firstBoundsIssue = null;
        foreach (var symbol in bboxSymbols)
        {
            if (TryReadBounds(data, elf, symbol, out var bounds, out var issue))
            {
                return new EaglObjectInspection(elf, geometrySymbols, bounds, symbol.Name, null);
            }

            firstBoundsIssue ??= issue;
        }

        return new EaglObjectInspection(
            elf,
            geometrySymbols,
            null,
            null,
            firstBoundsIssue);
    }

    private static bool TryReadBounds(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        ElfSymbol symbol,
        out EaglAxisAlignedBounds? bounds,
        out string issue)
    {
        bounds = null;
        if (symbol.SectionIndex == 0 || symbol.SectionIndex >= elf.Sections.Count)
        {
            issue = "__BBOX does not reference a file-backed section.";
            return false;
        }

        var section = elf.Sections[symbol.SectionIndex];
        if (section.Type == 8 || (ulong)symbol.Value + BoundsByteSize > section.Size)
        {
            issue = "__BBOX does not have six readable floats in its section.";
            return false;
        }

        var fileOffset = (ulong)section.Offset + symbol.Value;
        if (fileOffset + BoundsByteSize > (ulong)data.Length)
        {
            issue = "__BBOX data is outside the ELF object.";
            return false;
        }

        var values = new float[6];
        var bytes = data.Slice(checked((int)fileOffset), BoundsByteSize);
        for (var index = 0; index < values.Length; index++)
        {
            var bits = BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(index * sizeof(float), sizeof(float)));
            values[index] = BitConverter.Int32BitsToSingle(bits);
            if (!float.IsFinite(values[index]))
            {
                issue = "__BBOX contains a non-finite coordinate.";
                return false;
            }
        }

        if (values[0] > values[3] || values[1] > values[4] || values[2] > values[5])
        {
            issue = "__BBOX minimum exceeds its maximum.";
            return false;
        }

        bounds = new EaglAxisAlignedBounds(
            new EaglVector3(values[0], values[1], values[2]),
            new EaglVector3(values[3], values[4], values[5]));
        issue = string.Empty;
        return true;
    }
}
