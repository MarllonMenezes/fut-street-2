using System.Buffers.Binary;
using System.Numerics;

namespace FifaStreetPc;

public sealed record PcShapeImageTexture(
    string Name,
    byte RecordId,
    int Width,
    int Height,
    string PixelFormat,
    byte[] DecodedRgba);

public sealed record ShapeImageConversionResult(
    ShapeImageEntry Entry,
    PcShapeImageTexture? Texture,
    string? UnsupportedReason);

public static class ShapeImageTextureLoader
{
    private const int OldImageHeaderSize = 16;
    private const byte IndexedImageType = 0x7B;
    private const byte PaletteChunkType = 0x2A;
    private const ushort XboxSwizzledFlag = 0x2000;

    public static IReadOnlyList<ShapeImageConversionResult> DecodeSupported(ReadOnlySpan<byte> asset)
    {
        var package = ShapeImagePackageLoader.Load(asset);
        var results = new List<ShapeImageConversionResult>(package.Entries.Count);
        foreach (var entry in package.Entries)
        {
            try
            {
                results.Add(new ShapeImageConversionResult(entry, DecodeEntry(asset, entry), null));
            }
            catch (NotSupportedException exception)
            {
                results.Add(new ShapeImageConversionResult(entry, null, exception.Message));
            }
        }
        return results;
    }

    public static PcShapeImageTexture DecodeNamed(ReadOnlySpan<byte> asset, string entryName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entryName);
        var result = DecodeSupported(asset).FirstOrDefault(candidate =>
            string.Equals(candidate.Entry.Name, entryName, StringComparison.OrdinalIgnoreCase));
        if (result is null)
        {
            throw new FileNotFoundException($"SHPX entry was not found: {entryName}", entryName);
        }
        if (result.Texture is null)
        {
            throw new NotSupportedException(result.UnsupportedReason ??
                $"SHPX entry '{entryName}' could not be decoded.");
        }
        return result.Texture;
    }

    private static PcShapeImageTexture DecodeEntry(ReadOnlySpan<byte> asset, ShapeImageEntry entry)
    {
        if (entry.Size < OldImageHeaderSize)
        {
            throw new InvalidDataException($"SHPX image entry '{entry.Name}' is shorter than its header.");
        }

        var entryStart = checked((int)entry.Offset);
        var record = asset.Slice(entryStart, checked((int)entry.Size));
        var recordId = record[0];
        if ((recordId & 0x80) != 0)
        {
            throw new NotSupportedException($"SHPX entry '{entry.Name}' uses a compressed image payload.");
        }

        var blockSize = (uint)(record[1] | (record[2] << 8) | (record[3] << 16));
        var width = BinaryPrimitives.ReadUInt16LittleEndian(record[4..6]);
        var height = BinaryPrimitives.ReadUInt16LittleEndian(record[6..8]);
        if (blockSize < OldImageHeaderSize || blockSize > entry.Size)
        {
            throw new InvalidDataException($"SHPX image entry '{entry.Name}' declares an invalid block size.");
        }
        if (width is 0 or > 8192 || height is 0 or > 8192)
        {
            throw new InvalidDataException($"SHPX image entry '{entry.Name}' has invalid dimensions {width}x{height}.");
        }

        var pixelFormat = recordId & 0x7F;
        var payload = record.Slice(OldImageHeaderSize, checked((int)blockSize - OldImageHeaderSize));
        var (formatName, rgba) = pixelFormat switch
        {
            96 => ("DXT1", XprTextureDecoder.DecodeDxt1Payload(payload, width, height)),
            97 => ("DXT3", DecodeDxt3(payload, width, height)),
            IndexedImageType => ("PAL8", DecodePal8(record, entry.Name, blockSize, width, height)),
            _ => throw new NotSupportedException(
                $"SHPX entry '{entry.Name}' uses image type 0x{pixelFormat:X2}; palette/format decoding is not available yet."),
        };

        return new PcShapeImageTexture(entry.Name, recordId, width, height, formatName, rgba);
    }

    private static byte[] DecodePal8(ReadOnlySpan<byte> record, string name, uint paletteOffset, int width, int height)
    {
        if (paletteOffset < OldImageHeaderSize || paletteOffset > record.Length - 16)
        {
            throw new InvalidDataException($"SHPX PAL8 entry '{name}' has no complete palette attachment.");
        }

        var indexedPixels = checked(width * height);
        var imageData = record.Slice(OldImageHeaderSize, checked((int)paletteOffset - OldImageHeaderSize));
        if (imageData.Length < indexedPixels)
        {
            throw new InvalidDataException($"SHPX PAL8 entry '{name}' is shorter than its indexed pixel data.");
        }

        var palette = record[checked((int)paletteOffset)..];
        if (palette[0] != PaletteChunkType)
        {
            throw new InvalidDataException(
                $"SHPX PAL8 entry '{name}' expected palette chunk 0x{PaletteChunkType:X2}, found 0x{palette[0]:X2}.");
        }

        var paletteSize = (uint)(palette[1] | (palette[2] << 8) | (palette[3] << 16));
        var paletteWidth = BinaryPrimitives.ReadUInt16LittleEndian(palette[4..6]);
        var paletteHeight = BinaryPrimitives.ReadUInt16LittleEndian(palette[6..8]);
        var paletteBytesRequired = checked(OldImageHeaderSize + paletteWidth * paletteHeight * 4);
        if (paletteSize < paletteBytesRequired || paletteSize > palette.Length || paletteWidth != 256 || paletteHeight != 1)
        {
            throw new InvalidDataException($"SHPX PAL8 entry '{name}' has an invalid 256-color BGRA palette attachment.");
        }

        var swizzleFlags = BinaryPrimitives.ReadUInt16LittleEndian(record[12..14]);
        if (swizzleFlags is not 0 and not XboxSwizzledFlag)
        {
            throw new NotSupportedException(
                $"SHPX PAL8 entry '{name}' uses unsupported swizzle flags 0x{swizzleFlags:X4}.");
        }

        var rgba = new byte[checked(indexedPixels * 4)];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixelIndex = swizzleFlags == XboxSwizzledFlag
                    ? GetMortonIndex(x, y, width, height)
                    : y * width + x;
                var paletteIndex = imageData[pixelIndex];
                var colorOffset = checked(OldImageHeaderSize + paletteIndex * 4);
                var outputOffset = checked((y * width + x) * 4);
                rgba[outputOffset] = palette[colorOffset + 2];
                rgba[outputOffset + 1] = palette[colorOffset + 1];
                rgba[outputOffset + 2] = palette[colorOffset];
                rgba[outputOffset + 3] = palette[colorOffset + 3];
            }
        }
        return rgba;
    }

    private static int GetMortonIndex(int x, int y, int width, int height)
    {
        if ((width & (width - 1)) != 0 || (height & (height - 1)) != 0)
        {
            throw new NotSupportedException("SHPX PAL8 swizzled images require power-of-two dimensions.");
        }

        var xBits = BitOperations.Log2((uint)width);
        var yBits = BitOperations.Log2((uint)height);
        var result = 0;
        var destinationBit = 0;
        for (var sourceBit = 0; sourceBit < Math.Max(xBits, yBits); sourceBit++)
        {
            if (sourceBit < xBits)
            {
                result |= ((x >> sourceBit) & 1) << destinationBit++;
            }
            if (sourceBit < yBits)
            {
                result |= ((y >> sourceBit) & 1) << destinationBit++;
            }
        }
        return result;
    }

    private static byte[] DecodeDxt3(ReadOnlySpan<byte> data, int width, int height)
    {
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var required = checked(blocksWide * blocksHigh * 16);
        if (data.Length < required)
        {
            throw new InvalidDataException("SHPX DXT3 payload is shorter than its base level.");
        }

        var rgba = new byte[checked(width * height * 4)];
        var colors = new Rgba[4];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var blockOffset = checked((blockY * blocksWide + blockX) * 16);
                var alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(data[blockOffset..]);
                var color0 = BinaryPrimitives.ReadUInt16LittleEndian(data[(blockOffset + 8)..]);
                var color1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(blockOffset + 10)..]);
                var colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(data[(blockOffset + 12)..]);
                colors[0] = DecodeRgb565(color0);
                colors[1] = DecodeRgb565(color1);
                colors[2] = Lerp(colors[0], colors[1], 2, 1);
                colors[3] = Lerp(colors[0], colors[1], 1, 2);

                for (var pixel = 0; pixel < 16; pixel++)
                {
                    var x = blockX * 4 + pixel % 4;
                    var y = blockY * 4 + pixel / 4;
                    if (x >= width || y >= height)
                    {
                        continue;
                    }

                    var colorIndex = (int)((colorIndices >> (pixel * 2)) & 0x03);
                    var alpha = (byte)(((alphaBits >> (pixel * 4)) & 0x0F) * 17);
                    var offset = checked((y * width + x) * 4);
                    rgba[offset] = colors[colorIndex].R;
                    rgba[offset + 1] = colors[colorIndex].G;
                    rgba[offset + 2] = colors[colorIndex].B;
                    rgba[offset + 3] = alpha;
                }
            }
        }
        return rgba;
    }

    private static Rgba DecodeRgb565(ushort value) => new(
        Expand((value >> 11) & 0x1F, 31),
        Expand((value >> 5) & 0x3F, 63),
        Expand(value & 0x1F, 31));

    private static Rgba Lerp(Rgba first, Rgba second, int firstWeight, int secondWeight)
    {
        var divisor = firstWeight + secondWeight;
        return new Rgba(
            (byte)((first.R * firstWeight + second.R * secondWeight) / divisor),
            (byte)((first.G * firstWeight + second.G * secondWeight) / divisor),
            (byte)((first.B * firstWeight + second.B * secondWeight) / divisor));
    }

    private static byte Expand(int value, int maximum) => (byte)((value * 255 + maximum / 2) / maximum);

    private readonly record struct Rgba(byte R, byte G, byte B);
}

public static class ShapeImageTextureWriter
{
    public static (IReadOnlyList<string> WrittenFiles, IReadOnlyList<ShapeImageConversionResult> Unsupported) ExportAll(
        ReadOnlySpan<byte> asset,
        string directory)
    {
        var results = ShapeImageTextureLoader.DecodeSupported(asset);
        var writtenFiles = new List<string>();
        var unsupported = new List<ShapeImageConversionResult>();
        for (var index = 0; index < results.Count; index++)
        {
            var result = results[index];
            if (result.Texture is null)
            {
                unsupported.Add(result);
                continue;
            }
            writtenFiles.Add(SaveTga(result.Texture, directory, index));
        }
        return (writtenFiles, unsupported);
    }

    public static string SaveTga(PcShapeImageTexture texture, string directory, int index)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (texture.Width > ushort.MaxValue || texture.Height > ushort.MaxValue ||
            texture.DecodedRgba.Length != checked(texture.Width * texture.Height * 4))
        {
            throw new InvalidDataException("SHPX texture cannot be represented as a 32-bit TGA image.");
        }

        var fullDirectory = Path.GetFullPath(directory);
        Directory.CreateDirectory(fullDirectory);
        var safeName = new string(texture.Name.Select(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '_').ToArray());
        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "image";
        }
        var path = Path.Combine(fullDirectory, $"{index:D3}-{safeName}.tga");

        using var output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        Span<byte> header = stackalloc byte[18];
        header.Clear();
        header[2] = 2;
        BinaryPrimitives.WriteUInt16LittleEndian(header[12..14], checked((ushort)texture.Width));
        BinaryPrimitives.WriteUInt16LittleEndian(header[14..16], checked((ushort)texture.Height));
        header[16] = 32;
        header[17] = 0x28;
        output.Write(header);

        Span<byte> bgra = stackalloc byte[4];
        for (var pixel = 0; pixel < texture.Width * texture.Height; pixel++)
        {
            var offset = pixel * 4;
            bgra[0] = texture.DecodedRgba[offset + 2];
            bgra[1] = texture.DecodedRgba[offset + 1];
            bgra[2] = texture.DecodedRgba[offset];
            bgra[3] = texture.DecodedRgba[offset + 3];
            output.Write(bgra);
        }
        return path;
    }
}
