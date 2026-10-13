using System.Buffers.Binary;

namespace FifaStreetPc;

public static class XprTextureDecoder
{
    internal static byte[] DecodeDxt1Payload(ReadOnlySpan<byte> resource, int width, int height) =>
        DecodeDxt1(resource, width, height);

    public static byte[] Decode(XprInspection inspection, ReadOnlySpan<byte> resource)
    {
        return inspection.ResourceFormat switch
        {
            0x05 => DecodeRgb565(resource, inspection.Width, inspection.Height),
            0x06 => DecodeA8R8G8B8(resource, inspection.Width, inspection.Height),
            0x07 => DecodeX8R8G8B8(resource, inspection.Width, inspection.Height),
            0x0C => DecodeDxt1(resource, inspection.Width, inspection.Height),
            0x0E => DecodeDxt3(resource, inspection.Width, inspection.Height),
            0x0F => DecodeDxt5(resource, inspection.Width, inspection.Height),
            _ => throw new InvalidDataException(
                $"XPR0 pixel format {inspection.PixelFormatName} is not implemented yet."),
        };
    }

    private static byte[] DecodeRgb565(ReadOnlySpan<byte> data, int width, int height)
    {
        var required = checked(width * height * 2);
        if (data.Length < required)
        {
            throw new InvalidDataException("R5G6B5 resource is shorter than its base level.");
        }

        var rgba = new byte[checked(width * height * 4)];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var color = DecodeRgb565(BinaryPrimitives.ReadUInt16LittleEndian(data[(pixel * 2)..]));
            WritePixel(rgba, pixel * 4, color);
        }
        return rgba;
    }

    // The XPR format codes identify the Direct3D packed formats. The byte
    // order below is the little-endian memory order of A8R8G8B8/X8R8G8B8;
    // this is payload decoding only and does not infer a material or GPU
    // binding for any game object.
    private static byte[] DecodeA8R8G8B8(ReadOnlySpan<byte> data, int width, int height)
    {
        var required = checked(width * height * 4);
        if (data.Length < required)
        {
            throw new InvalidDataException("A8R8G8B8 resource is shorter than its base level.");
        }

        var rgba = new byte[required];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var source = pixel * 4;
            var destination = source;
            rgba[destination] = data[source + 2];
            rgba[destination + 1] = data[source + 1];
            rgba[destination + 2] = data[source];
            rgba[destination + 3] = data[source + 3];
        }
        return rgba;
    }

    private static byte[] DecodeX8R8G8B8(ReadOnlySpan<byte> data, int width, int height)
    {
        var rgba = DecodeA8R8G8B8(data, width, height);
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            rgba[pixel * 4 + 3] = 255;
        }
        return rgba;
    }

    private static byte[] DecodeDxt1(ReadOnlySpan<byte> data, int width, int height)
    {
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var required = checked(blocksWide * blocksHigh * 8);
        if (data.Length < required)
        {
            throw new InvalidDataException("DXT1 resource is shorter than its base level.");
        }

        var rgba = new byte[checked(width * height * 4)];
        var colors = new Rgba[4];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var blockOffset = checked((blockY * blocksWide + blockX) * 8);
                var color0 = BinaryPrimitives.ReadUInt16LittleEndian(data[blockOffset..]);
                var color1 = BinaryPrimitives.ReadUInt16LittleEndian(data[(blockOffset + 2)..]);
                var indices = BinaryPrimitives.ReadUInt32LittleEndian(data[(blockOffset + 4)..]);
                colors[0] = DecodeRgb565(color0);
                colors[1] = DecodeRgb565(color1);
                if (color0 > color1)
                {
                    colors[2] = Lerp(colors[0], colors[1], 2, 1);
                    colors[3] = Lerp(colors[0], colors[1], 1, 2);
                }
                else
                {
                    colors[2] = Lerp(colors[0], colors[1], 1, 1);
                    colors[3] = new Rgba(0, 0, 0, 0);
                }

                for (var row = 0; row < 4; row++)
                {
                    for (var column = 0; column < 4; column++)
                    {
                        var x = blockX * 4 + column;
                        var y = blockY * 4 + row;
                        if (x >= width || y >= height)
                        {
                            continue;
                        }

                        var colorIndex = (int)((indices >> (2 * (row * 4 + column))) & 0x03);
                        var pixelOffset = checked((y * width + x) * 4);
                        var color = colors[colorIndex];
                        WritePixel(rgba, pixelOffset, color);
                    }
                }
            }
        }

        return rgba;
    }

    private static byte[] DecodeDxt3(ReadOnlySpan<byte> data, int width, int height)
    {
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var required = checked(blocksWide * blocksHigh * 16);
        if (data.Length < required)
        {
            throw new InvalidDataException("DXT3 resource is shorter than its base level.");
        }

        var rgba = new byte[checked(width * height * 4)];
        var colors = new Rgba[4];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var blockOffset = checked((blockY * blocksWide + blockX) * 16);
                var alphaBits = BinaryPrimitives.ReadUInt64LittleEndian(data[blockOffset..]);
                DecodeFourColorBlock(data[blockOffset..], colors);
                for (var pixel = 0; pixel < 16; pixel++)
                {
                    WriteCompressedPixel(
                        rgba,
                        width,
                        height,
                        blockX,
                        blockY,
                        pixel,
                        colors[(BinaryPrimitives.ReadUInt32LittleEndian(data[(blockOffset + 12)..]) >> (pixel * 2)) & 0x03],
                        (byte)(((alphaBits >> (pixel * 4)) & 0x0F) * 17));
                }
            }
        }
        return rgba;
    }

    private static byte[] DecodeDxt5(ReadOnlySpan<byte> data, int width, int height)
    {
        var blocksWide = (width + 3) / 4;
        var blocksHigh = (height + 3) / 4;
        var required = checked(blocksWide * blocksHigh * 16);
        if (data.Length < required)
        {
            throw new InvalidDataException("DXT5 resource is shorter than its base level.");
        }

        var rgba = new byte[checked(width * height * 4)];
        var colors = new Rgba[4];
        var alphas = new byte[8];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                var blockOffset = checked((blockY * blocksWide + blockX) * 16);
                DecodeFourColorBlock(data[blockOffset..], colors);
                var alpha0 = data[blockOffset];
                var alpha1 = data[blockOffset + 1];
                DecodeDxt5AlphaPalette(alpha0, alpha1, alphas);
                ulong alphaIndices = 0;
                for (var byteIndex = 0; byteIndex < 6; byteIndex++)
                {
                    alphaIndices |= (ulong)data[blockOffset + 2 + byteIndex] << (byteIndex * 8);
                }

                var colorIndices = BinaryPrimitives.ReadUInt32LittleEndian(data[(blockOffset + 12)..]);
                for (var pixel = 0; pixel < 16; pixel++)
                {
                    var colorIndex = (int)((colorIndices >> (pixel * 2)) & 0x03);
                    var alphaIndex = (int)((alphaIndices >> (pixel * 3)) & 0x07);
                    WriteCompressedPixel(
                        rgba,
                        width,
                        height,
                        blockX,
                        blockY,
                        pixel,
                        colors[colorIndex],
                        alphas[alphaIndex]);
                }
            }
        }
        return rgba;
    }

    private static void DecodeFourColorBlock(ReadOnlySpan<byte> block, Rgba[] colors)
    {
        var color0 = BinaryPrimitives.ReadUInt16LittleEndian(block[8..]);
        var color1 = BinaryPrimitives.ReadUInt16LittleEndian(block[10..]);
        colors[0] = DecodeRgb565(color0);
        colors[1] = DecodeRgb565(color1);
        colors[2] = Lerp(colors[0], colors[1], 2, 1);
        colors[3] = Lerp(colors[0], colors[1], 1, 2);
    }

    private static void DecodeDxt5AlphaPalette(byte alpha0, byte alpha1, byte[] alphas)
    {
        alphas[0] = alpha0;
        alphas[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var index = 2; index < 8; index++)
            {
                alphas[index] = (byte)(((8 - index) * alpha0 + (index - 1) * alpha1) / 7);
            }
        }
        else
        {
            for (var index = 2; index < 6; index++)
            {
                alphas[index] = (byte)(((6 - index) * alpha0 + (index - 1) * alpha1) / 5);
            }
            alphas[6] = 0;
            alphas[7] = 255;
        }
    }

    private static void WriteCompressedPixel(
        byte[] rgba,
        int width,
        int height,
        int blockX,
        int blockY,
        int pixel,
        Rgba color,
        byte alpha)
    {
        var x = blockX * 4 + pixel % 4;
        var y = blockY * 4 + pixel / 4;
        if (x >= width || y >= height)
        {
            return;
        }

        var offset = checked((y * width + x) * 4);
        WritePixel(rgba, offset, color with { A = alpha });
    }

    private static void WritePixel(byte[] rgba, int offset, Rgba color)
    {
        rgba[offset] = color.R;
        rgba[offset + 1] = color.G;
        rgba[offset + 2] = color.B;
        rgba[offset + 3] = color.A;
    }

    private static Rgba DecodeRgb565(ushort value) => new(
        Expand((value >> 11) & 0x1F, 31),
        Expand((value >> 5) & 0x3F, 63),
        Expand(value & 0x1F, 31),
        255);

    private static Rgba Lerp(Rgba first, Rgba second, int firstWeight, int secondWeight)
    {
        var divisor = firstWeight + secondWeight;
        return new Rgba(
            (byte)((first.R * firstWeight + second.R * secondWeight) / divisor),
            (byte)((first.G * firstWeight + second.G * secondWeight) / divisor),
            (byte)((first.B * firstWeight + second.B * secondWeight) / divisor),
            255);
    }

    private static byte Expand(int value, int maximum) => (byte)((value * 255 + maximum / 2) / maximum);

    private readonly record struct Rgba(byte R, byte G, byte B, byte A);
}
