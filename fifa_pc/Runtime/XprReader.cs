using System.Buffers.Binary;

namespace FifaStreetPc;

public sealed record XprInspection(
    uint DeclaredSize,
    uint HeaderSize,
    uint ResourceSize,
    uint ResourceType,
    uint ResourceFormat,
    uint HeaderWord24,
    uint DataOffset,
    int Width,
    int Height,
    string PixelFormatName,
    bool DeclaredSizeMatches,
    bool PayloadWithinAsset);

public static class XprReader
{
    private const int HeaderSize = 32;

    public static XprInspection Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length < HeaderSize || !data[..4].SequenceEqual("XPR0"u8))
        {
            throw new InvalidDataException("Unsupported XPR0 texture.");
        }

        var declaredSize = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]);
        var resourceType = BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]);
        var headerWord24 = BinaryPrimitives.ReadUInt32LittleEndian(data[24..28]);
        if (headerSize < HeaderSize || headerSize > declaredSize)
        {
            throw new InvalidDataException("XPR0 header size is outside the asset.");
        }

        var resourceSize = declaredSize - headerSize;
        var dataOffset = headerSize;
        var payloadWithinAsset = (ulong)dataOffset + resourceSize <= (ulong)data.Length;
        if (!payloadWithinAsset)
        {
            throw new InvalidDataException("XPR0 resource payload is outside the asset.");
        }

        var resourceFormat = (headerWord24 >> 8) & 0xFF;
        var widthLog2 = (int)((headerWord24 >> 20) & 0x0F);
        var heightLog2 = (int)((headerWord24 >> 24) & 0x0F);
        var width = 1 << widthLog2;
        var height = 1 << heightLog2;
        var pixelFormatName = resourceFormat switch
        {
            0x05 => "R5G6B5",
            0x06 => "A8R8G8B8",
            0x07 => "X8R8G8B8",
            0x0C => "DXT1",
            0x0E => "DXT3",
            0x0F => "DXT5",
            _ => $"Unknown(0x{resourceFormat:X2})",
        };

        return new XprInspection(
            declaredSize,
            headerSize,
            resourceSize,
            resourceType,
            resourceFormat,
            headerWord24,
            dataOffset,
            width,
            height,
            pixelFormatName,
            declaredSize == data.Length,
            payloadWithinAsset);
    }
}
