using System.Buffers.Binary;
using System.Text;

namespace FifaStreetPc;

public sealed record ElfInspection(
    bool Is32Bit,
    bool IsLittleEndian,
    ushort Type,
    ushort Machine,
    uint SectionHeaderOffset,
    ushort SectionHeaderSize,
    ushort SectionCount,
    bool SectionTableWithinAsset,
    IReadOnlyList<ElfSection> Sections,
    IReadOnlyList<ElfSymbol> Symbols,
    IReadOnlyList<ElfRelocation> Relocations);

public sealed record ElfSection(string Name, uint Type, uint Flags, uint Offset, uint Size, uint Link, uint Info, uint EntrySize);
public sealed record ElfSymbol(int TableSectionIndex, int Index, string Name, uint Value, uint Size, byte Info, ushort SectionIndex);
public sealed record ElfRelocation(string SectionName, int TargetSectionIndex, uint Offset, uint Type, string SymbolName);

public static class ElfReader
{
    private const int HeaderSize32 = 52;

    public static ElfInspection Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize32 || data[0] != 0x7F || data[1] != 'E' ||
            data[2] != 'L' || data[3] != 'F')
        {
            throw new InvalidDataException("Unsupported ELF object.");
        }
        if (data[4] != 1 || data[5] != 1)
        {
            throw new InvalidDataException("Only ELF32 little-endian objects are supported.");
        }

        var sectionOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[32..36]);
        var sectionSize = BinaryPrimitives.ReadUInt16LittleEndian(data[46..48]);
        var sectionCount = BinaryPrimitives.ReadUInt16LittleEndian(data[48..50]);
        var stringTableIndex = BinaryPrimitives.ReadUInt16LittleEndian(data[50..52]);
        var tableSize = (ulong)sectionSize * sectionCount;
        var tableWithinAsset = sectionOffset == 0 ||
                               sectionSize >= 40 &&
                               (ulong)sectionOffset + tableSize <= (ulong)data.Length;
        if (!tableWithinAsset)
        {
            throw new InvalidDataException("ELF section table is outside the object.");
        }

        var sections = new List<ElfSection>(sectionCount);
        if (sectionCount > 0 && sectionOffset > 0)
        {
            if (stringTableIndex >= sectionCount && stringTableIndex != 0)
            {
                throw new InvalidDataException("ELF section-name table index is outside the section table.");
            }

            for (var index = 0; index < sectionCount; index++)
            {
                var header = data.Slice(checked((int)sectionOffset + index * sectionSize), 40);
                var type = BinaryPrimitives.ReadUInt32LittleEndian(header[4..8]);
                var sectionFileOffset = BinaryPrimitives.ReadUInt32LittleEndian(header[16..20]);
                var sectionFileSize = BinaryPrimitives.ReadUInt32LittleEndian(header[20..24]);
                if (type != 8 && (ulong)sectionFileOffset + sectionFileSize > (ulong)data.Length)
                {
                    throw new InvalidDataException("ELF file-backed section is outside the object.");
                }

                sections.Add(new ElfSection(
                    string.Empty,
                    type,
                    BinaryPrimitives.ReadUInt32LittleEndian(header[8..12]),
                    sectionFileOffset,
                    sectionFileSize,
                    BinaryPrimitives.ReadUInt32LittleEndian(header[24..28]),
                    BinaryPrimitives.ReadUInt32LittleEndian(header[28..32]),
                    BinaryPrimitives.ReadUInt32LittleEndian(header[36..40])));
            }

            if (stringTableIndex != 0)
            {
                var nameTable = sections[stringTableIndex];
                if (nameTable.Type != 3)
                {
                    throw new InvalidDataException("ELF section-name table is not a string table.");
                }
                var names = data.Slice(checked((int)nameTable.Offset), checked((int)nameTable.Size));
                for (var index = 0; index < sectionCount; index++)
                {
                    var header = data.Slice(checked((int)sectionOffset + index * sectionSize), 40);
                    var nameOffset = BinaryPrimitives.ReadUInt32LittleEndian(header[..4]);
                    if (nameOffset >= names.Length)
                    {
                        throw new InvalidDataException("ELF section name points outside the string table.");
                    }
                    var nameBytes = names[(int)nameOffset..];
                    var terminator = nameBytes.IndexOf((byte)0);
                    if (terminator < 0)
                    {
                        throw new InvalidDataException("ELF section name is not null-terminated.");
                    }
                    sections[index] = sections[index] with { Name = Encoding.ASCII.GetString(nameBytes[..terminator]) };
                }
            }
        }

        var symbols = ReadSymbols(data, sections);
        var relocations = ReadRelocations(data, sections, symbols);

        return new ElfInspection(
            Is32Bit: true,
            IsLittleEndian: true,
            Type: BinaryPrimitives.ReadUInt16LittleEndian(data[16..18]),
            Machine: BinaryPrimitives.ReadUInt16LittleEndian(data[18..20]),
            SectionHeaderOffset: sectionOffset,
            SectionHeaderSize: sectionSize,
            SectionCount: sectionCount,
            SectionTableWithinAsset: tableWithinAsset,
            Sections: sections,
            Symbols: symbols,
            Relocations: relocations);
    }

    private static List<ElfSymbol> ReadSymbols(ReadOnlySpan<byte> data, IReadOnlyList<ElfSection> sections)
    {
        var symbols = new List<ElfSymbol>();
        for (var tableIndex = 0; tableIndex < sections.Count; tableIndex++)
        {
            var table = sections[tableIndex];
            if (table.Type is not (2 or 11))
            {
                continue;
            }
            if (table.EntrySize < 16 || table.Size % table.EntrySize != 0 || table.Link >= sections.Count)
            {
                throw new InvalidDataException("ELF symbol table has an invalid entry size or string-table link.");
            }
            var stringsSection = sections[checked((int)table.Link)];
            if (stringsSection.Type != 3)
            {
                throw new InvalidDataException("ELF symbol-name link does not point to a string table.");
            }
            var strings = data.Slice(checked((int)stringsSection.Offset), checked((int)stringsSection.Size));
            var entryCount = checked((int)(table.Size / table.EntrySize));
            for (var symbolIndex = 0; symbolIndex < entryCount; symbolIndex++)
            {
                var offset = checked((int)table.Offset + symbolIndex * checked((int)table.EntrySize));
                var entry = data.Slice(offset, 16);
                symbols.Add(new ElfSymbol(
                    tableIndex,
                    symbolIndex,
                    ReadString(strings, BinaryPrimitives.ReadUInt32LittleEndian(entry[..4])),
                    BinaryPrimitives.ReadUInt32LittleEndian(entry[4..8]),
                    BinaryPrimitives.ReadUInt32LittleEndian(entry[8..12]),
                    entry[12],
                    BinaryPrimitives.ReadUInt16LittleEndian(entry[14..16])));
            }
        }
        return symbols;
    }

    private static List<ElfRelocation> ReadRelocations(
        ReadOnlySpan<byte> data,
        IReadOnlyList<ElfSection> sections,
        IReadOnlyList<ElfSymbol> symbols)
    {
        var relocations = new List<ElfRelocation>();
        foreach (var section in sections)
        {
            if (section.Type is not (4 or 9))
            {
                continue;
            }
            var minimumEntrySize = section.Type == 9 ? 8u : 12u;
            if (section.EntrySize < minimumEntrySize || section.Size % section.EntrySize != 0 ||
                section.Link >= sections.Count || section.Info >= sections.Count)
            {
                throw new InvalidDataException("ELF relocation section has invalid entry sizes or links.");
            }
            var entryCount = checked((int)(section.Size / section.EntrySize));
            for (var index = 0; index < entryCount; index++)
            {
                var offset = checked((int)section.Offset + index * checked((int)section.EntrySize));
                var entry = data.Slice(offset, checked((int)minimumEntrySize));
                var relocationOffset = BinaryPrimitives.ReadUInt32LittleEndian(entry[..4]);
                var info = BinaryPrimitives.ReadUInt32LittleEndian(entry[4..8]);
                var symbolIndex = checked((int)(info >> 8));
                var symbol = symbols.FirstOrDefault(candidate =>
                    candidate.TableSectionIndex == (int)section.Link && candidate.Index == symbolIndex);
                relocations.Add(new ElfRelocation(
                    section.Name,
                    checked((int)section.Info),
                    relocationOffset,
                    info & 0xFF,
                    symbol?.Name ?? $"<symbol {symbolIndex}>") );
            }
        }
        return relocations;
    }

    private static string ReadString(ReadOnlySpan<byte> table, uint offset)
    {
        if (offset >= table.Length)
        {
            throw new InvalidDataException("ELF string offset is outside its table.");
        }
        var value = table[(int)offset..];
        var terminator = value.IndexOf((byte)0);
        if (terminator < 0)
        {
            throw new InvalidDataException("ELF string is not null-terminated.");
        }
        return Encoding.ASCII.GetString(value[..terminator]);
    }
}
