using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FifaPcCompat.Runtime;

public sealed record XbeSectionInspection(
    string Name,
    uint Flags,
    uint VirtualAddress,
    uint VirtualSize,
    uint FileAddress,
    uint FileSize,
    bool Executable,
    bool Writable,
    bool HashMatches);

public sealed record XbeInspection(
    uint BaseAddress,
    uint EntryPoint,
    uint KernelThunkAddress,
    int SectionCount,
    int KernelThunkCount,
    IReadOnlyList<XbeSectionInspection> Sections);

public static class XbeInspector
{
    private const uint RetailEntryKey = 0xA8FC57AB;
    private const uint RetailKernelKey = 0x5B6D40B6;
    private const int SectionSize = 0x38;

    public static XbeInspection Inspect(byte[] data)
    {
        if (data.Length < 0x178 || !data.AsSpan(0, 4).SequenceEqual("XBEH"u8))
        {
            throw new InvalidDataException("The embedded file is not an XBE.");
        }

        var baseAddress = U32(data, 0x104);
        var sectionCount = checked((int)U32(data, 0x11C));
        var sectionPointer = U32(data, 0x120);
        var entryPoint = U32(data, 0x128) ^ RetailEntryKey;
        var kernelPointer = U32(data, 0x158) ^ RetailKernelKey;
        var sections = new List<XbeSectionInspection>();

        for (var index = 0; index < sectionCount; index++)
        {
            var offset = PointerToOffset(sectionPointer, baseAddress, data.Length) + index * SectionSize;
            var flags = U32(data, offset);
            var virtualAddress = U32(data, offset + 4);
            var virtualSize = U32(data, offset + 8);
            var fileAddress = U32(data, offset + 12);
            var fileSize = U32(data, offset + 16);
            var namePointer = U32(data, offset + 0x14);
            var nameOffset = PointerToOffset(namePointer, baseAddress, data.Length);
            var name = ReadAsciiZ(data, nameOffset);
            var digest = data.AsSpan(offset + 0x24, 20).ToArray();
            var prefix = BitConverter.GetBytes(fileSize);
            var sectionBytes = data.AsSpan(checked((int)fileAddress), checked((int)fileSize));
            var hashed = SHA1.HashData(prefix.Concat(sectionBytes.ToArray()).ToArray());
            sections.Add(new XbeSectionInspection(
                name,
                flags,
                virtualAddress,
                virtualSize,
                fileAddress,
                fileSize,
                (flags & 4) != 0,
                (flags & 1) != 0,
                CryptographicOperations.FixedTimeEquals(digest, hashed)));
        }

        var thunkOffset = VirtualToOffset(kernelPointer, sections, data.Length);
        var thunkCount = 0;
        while (thunkOffset + thunkCount * 4 + 4 <= data.Length)
        {
            var value = U32(data, thunkOffset + thunkCount * 4);
            if (value == 0)
            {
                break;
            }
            thunkCount++;
        }

        return new XbeInspection(baseAddress, entryPoint, kernelPointer, sectionCount, thunkCount, sections);
    }

    private static int PointerToOffset(uint pointer, uint baseAddress, int dataLength)
    {
        var offset = checked((long)pointer - baseAddress);
        if (offset < 0 || offset >= dataLength)
        {
            throw new InvalidDataException($"XBE pointer 0x{pointer:X8} is outside the image.");
        }
        return checked((int)offset);
    }

    private static int VirtualToOffset(uint address, IReadOnlyList<XbeSectionInspection> sections, int dataLength)
    {
        foreach (var section in sections)
        {
            if (section.VirtualAddress <= address && address < section.VirtualAddress + section.VirtualSize)
            {
                var offset = checked((long)section.FileAddress + address - section.VirtualAddress);
                if (offset >= 0 && offset < dataLength)
                {
                    return checked((int)offset);
                }
            }
        }
        throw new InvalidDataException($"XBE virtual address 0x{address:X8} is not mapped.");
    }

    private static uint U32(byte[] data, int offset) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset, 4));

    private static string ReadAsciiZ(byte[] data, int offset)
    {
        var end = offset;
        while (end < data.Length && data[end] != 0)
        {
            end++;
        }
        return Encoding.ASCII.GetString(data, offset, end - offset);
    }
}
