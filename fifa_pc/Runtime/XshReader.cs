using System.Buffers.Binary;
using System.Text;

namespace FifaStreetPc;

public sealed record XshEntry(string Name, uint Offset);

public sealed record XshInspection(
    uint DeclaredSize,
    uint EntryCount,
    string FormatVersion,
    uint TableEnd,
    bool DeclaredSizeMatches,
    bool OffsetsMonotonic,
    IReadOnlyList<XshEntry> Entries);

public static class XshReader
{
    private const int HeaderSize = 16;
    private const int EntrySize = 8;

    public static XshInspection Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize || Encoding.ASCII.GetString(data[..4]) != "SHPX")
        {
            throw new InvalidDataException("Unsupported XSH signature.");
        }

        var declaredSize = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        var entryCount = BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]);
        if (entryCount == 0 || entryCount > 1_000_000)
        {
            throw new InvalidDataException("XSH entry count is invalid.");
        }

        var tableEnd = checked((uint)((ulong)HeaderSize + (ulong)entryCount * EntrySize));
        if (tableEnd > data.Length)
        {
            throw new InvalidDataException("XSH entry table is outside the asset.");
        }

        var formatVersion = Encoding.ASCII.GetString(data[12..16]);
        var entries = new List<XshEntry>(checked((int)entryCount));
        for (var index = 0u; index < entryCount; index++)
        {
            var cursor = checked(HeaderSize + (int)index * EntrySize);
            entries.Add(new XshEntry(
                ReadName(data[cursor..(cursor + 4)]),
                BinaryPrimitives.ReadUInt32LittleEndian(data[(cursor + 4)..(cursor + 8)])));
        }

        var monotonic = true;
        var previousOffset = 0u;
        foreach (var entry in entries)
        {
            if (entry.Offset < tableEnd || entry.Offset > data.Length || entry.Offset < previousOffset)
            {
                throw new InvalidDataException("XSH entry offset is outside the asset or not monotonic.");
            }
            previousOffset = entry.Offset;
        }

        return new XshInspection(
            declaredSize,
            entryCount,
            formatVersion,
            tableEnd,
            declaredSize == data.Length,
            monotonic,
            entries);
    }

    private static string ReadName(ReadOnlySpan<byte> bytes)
    {
        return Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ');
    }
}
