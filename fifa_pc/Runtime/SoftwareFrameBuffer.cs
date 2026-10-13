using System.Text;

namespace FifaStreetPc;

public readonly record struct RgbColor(byte R, byte G, byte B);

public sealed class SoftwareFrameBuffer
{
    private readonly byte[] pixels;

    public SoftwareFrameBuffer(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        Width = width;
        Height = height;
        pixels = new byte[checked(width * height * 3)];
    }

    public int Width { get; }
    public int Height { get; }

    /// <summary>
    /// Counts RGB pixels that differ from another buffer of the same size.
    /// This is a renderer-output diagnostic; it assigns no meaning to the
    /// source attributes used to produce either frame.
    /// </summary>
    public long CountDifferingPixels(SoftwareFrameBuffer other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (Width != other.Width || Height != other.Height)
        {
            throw new ArgumentException("Framebuffers must have identical dimensions.", nameof(other));
        }

        long differingPixels = 0;
        for (var offset = 0; offset < pixels.Length; offset += 3)
        {
            if (pixels[offset] != other.pixels[offset] ||
                pixels[offset + 1] != other.pixels[offset + 1] ||
                pixels[offset + 2] != other.pixels[offset + 2])
            {
                differingPixels++;
            }
        }

        return differingPixels;
    }

    public void Clear(RgbColor color)
    {
        FillRect(0, 0, Width, Height, color);
    }

    public void FillRect(int x, int y, int width, int height, RgbColor color)
    {
        var left = Math.Max(0, x);
        var top = Math.Max(0, y);
        var right = Math.Min(Width, checked(x + width));
        var bottom = Math.Min(Height, checked(y + height));
        if (left >= right || top >= bottom)
        {
            return;
        }

        for (var row = top; row < bottom; row++)
        {
            for (var column = left; column < right; column++)
            {
                SetPixel(column, row, color);
            }
        }
    }

    public void DrawLine(int x0, int y0, int x1, int y1, RgbColor color, int thickness = 1)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;
        while (true)
        {
            FillRect(x0 - thickness / 2, y0 - thickness / 2, thickness, thickness, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var doubled = 2 * error;
            if (doubled >= dy)
            {
                error += dy;
                x0 += sx;
            }
            if (doubled <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    public void FillTriangle(
        int ax,
        int ay,
        int bx,
        int by,
        int cx,
        int cy,
        RgbColor color)
    {
        var left = Math.Max(0, Math.Min(ax, Math.Min(bx, cx)));
        var top = Math.Max(0, Math.Min(ay, Math.Min(by, cy)));
        var right = Math.Min(Width - 1, Math.Max(ax, Math.Max(bx, cx)));
        var bottom = Math.Min(Height - 1, Math.Max(ay, Math.Max(by, cy)));
        if (left > right || top > bottom)
        {
            return;
        }

        static long Edge(int x0, int y0, int x1, int y1, int x, int y) =>
            (long)(x - x0) * (y1 - y0) - (long)(y - y0) * (x1 - x0);

        var area = Edge(ax, ay, bx, by, cx, cy);
        if (area == 0)
        {
            return;
        }

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                var edgeA = Edge(bx, by, cx, cy, x, y);
                var edgeB = Edge(cx, cy, ax, ay, x, y);
                var edgeC = Edge(ax, ay, bx, by, x, y);
                var inside = area > 0
                    ? edgeA >= 0 && edgeB >= 0 && edgeC >= 0
                    : edgeA <= 0 && edgeB <= 0 && edgeC <= 0;
                if (inside)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }

    /// <summary>
    /// Rasterizes a candidate triangle while linearly sampling a decoded RGBA
    /// image. The caller owns the semantic decision that the supplied float2
    /// values are texture coordinates; this framebuffer only performs the
    /// bounded software operation.
    /// </summary>
    public void FillTexturedTriangle(
        int ax,
        int ay,
        int bx,
        int by,
        int cx,
        int cy,
        EaglFloat2 auv,
        EaglFloat2 buv,
        EaglFloat2 cuv,
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int sourceHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidth));
        }
        if (source.Length < checked(sourceWidth * sourceHeight * 4))
        {
            throw new ArgumentException("Source does not contain the full RGBA image.", nameof(source));
        }

        var left = Math.Max(0, Math.Min(ax, Math.Min(bx, cx)));
        var top = Math.Max(0, Math.Min(ay, Math.Min(by, cy)));
        var right = Math.Min(Width - 1, Math.Max(ax, Math.Max(bx, cx)));
        var bottom = Math.Min(Height - 1, Math.Max(ay, Math.Max(by, cy)));
        if (left > right || top > bottom)
        {
            return;
        }

        static long Edge(int x0, int y0, int x1, int y1, int x, int y) =>
            (long)(x - x0) * (y1 - y0) - (long)(y - y0) * (x1 - x0);

        var area = Edge(ax, ay, bx, by, cx, cy);
        if (area == 0)
        {
            return;
        }

        for (var y = top; y <= bottom; y++)
        {
            for (var x = left; x <= right; x++)
            {
                var edgeA = Edge(bx, by, cx, cy, x, y);
                var edgeB = Edge(cx, cy, ax, ay, x, y);
                var edgeC = Edge(ax, ay, bx, by, x, y);
                var inside = area > 0
                    ? edgeA >= 0 && edgeB >= 0 && edgeC >= 0
                    : edgeA <= 0 && edgeB <= 0 && edgeC <= 0;
                if (!inside)
                {
                    continue;
                }

                var weightA = edgeA / (double)area;
                var weightB = edgeB / (double)area;
                var weightC = edgeC / (double)area;
                var u = Math.Clamp((float)(auv.X * weightA + buv.X * weightB + cuv.X * weightC), 0f, 1f);
                var v = Math.Clamp((float)(auv.Y * weightA + buv.Y * weightB + cuv.Y * weightC), 0f, 1f);
                var sourceX = Math.Clamp((int)MathF.Floor(u * sourceWidth), 0, sourceWidth - 1);
                var sourceY = Math.Clamp((int)MathF.Floor(v * sourceHeight), 0, sourceHeight - 1);
                var sourceOffset = checked((sourceY * sourceWidth + sourceX) * 4);
                BlendPixel(x, y, source[sourceOffset], source[sourceOffset + 1],
                    source[sourceOffset + 2], source[sourceOffset + 3]);
            }
        }
    }

    public void DrawCircle(int centerX, int centerY, int radius, RgbColor color)
    {
        if (radius <= 0)
        {
            SetPixel(centerX, centerY, color);
            return;
        }

        var radiusSquared = radius * radius;
        for (var y = -radius; y <= radius; y++)
        {
            for (var x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= radiusSquared)
                {
                    SetPixel(centerX + x, centerY + y, color);
                }
            }
        }
    }

    public void BlitRgba(
        ReadOnlySpan<byte> source,
        int sourceWidth,
        int sourceHeight,
        int destinationX,
        int destinationY,
        int destinationWidth,
        int destinationHeight)
    {
        if (sourceWidth <= 0 || sourceHeight <= 0 || destinationWidth <= 0 || destinationHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceWidth));
        }
        if (source.Length < checked(sourceWidth * sourceHeight * 4))
        {
            throw new ArgumentException("Source does not contain the full RGBA image.", nameof(source));
        }

        for (var y = 0; y < destinationHeight; y++)
        {
            var sourceY = y * sourceHeight / destinationHeight;
            for (var x = 0; x < destinationWidth; x++)
            {
                var sourceX = x * sourceWidth / destinationWidth;
                var sourceOffset = checked((sourceY * sourceWidth + sourceX) * 4);
                var alpha = source[sourceOffset + 3];
                if (alpha == 0)
                {
                    continue;
                }

                var targetX = destinationX + x;
                var targetY = destinationY + y;
                if ((uint)targetX >= (uint)Width || (uint)targetY >= (uint)Height)
                {
                    continue;
                }

                if (alpha == byte.MaxValue)
                {
                    SetPixel(targetX, targetY, new RgbColor(
                        source[sourceOffset],
                        source[sourceOffset + 1],
                        source[sourceOffset + 2]));
                    continue;
                }

                var existing = GetPixel(targetX, targetY);
                var inverseAlpha = 255 - alpha;
                SetPixel(targetX, targetY, new RgbColor(
                    (byte)((source[sourceOffset] * alpha + existing.R * inverseAlpha) / 255),
                    (byte)((source[sourceOffset + 1] * alpha + existing.G * inverseAlpha) / 255),
                    (byte)((source[sourceOffset + 2] * alpha + existing.B * inverseAlpha) / 255)));
            }
        }
    }

    public RgbColor GetPixel(int x, int y)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            throw new ArgumentOutOfRangeException();
        }

        var offset = checked((y * Width + x) * 3);
        return new RgbColor(pixels[offset], pixels[offset + 1], pixels[offset + 2]);
    }

    public void SavePpm(string path)
    {
        var fullPath = Path.GetFullPath(path);
        using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var header = Encoding.ASCII.GetBytes($"P6\n{Width} {Height}\n255\n");
        output.Write(header);
        output.Write(pixels);
    }

    public void CopyToBgra32(Span<byte> destination)
    {
        var requiredLength = checked(Width * Height * 4);
        if (destination.Length < requiredLength)
        {
            throw new ArgumentException("Destination is smaller than the framebuffer.", nameof(destination));
        }

        var sourceOffset = 0;
        var destinationOffset = 0;
        while (sourceOffset < pixels.Length)
        {
            destination[destinationOffset] = pixels[sourceOffset + 2];
            destination[destinationOffset + 1] = pixels[sourceOffset + 1];
            destination[destinationOffset + 2] = pixels[sourceOffset];
            destination[destinationOffset + 3] = byte.MaxValue;
            sourceOffset += 3;
            destinationOffset += 4;
        }
    }

    public ulong ComputeChecksum()
    {
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        ulong hash = offsetBasis;
        foreach (var pixel in pixels)
        {
            hash ^= pixel;
            hash *= prime;
        }
        return hash;
    }

    private void SetPixel(int x, int y, RgbColor color)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
        {
            return;
        }

        var offset = checked((y * Width + x) * 3);
        pixels[offset] = color.R;
        pixels[offset + 1] = color.G;
        pixels[offset + 2] = color.B;
    }

    private void BlendPixel(int x, int y, byte red, byte green, byte blue, byte alpha)
    {
        if ((uint)x >= (uint)Width || (uint)y >= (uint)Height || alpha == 0)
        {
            return;
        }

        if (alpha == byte.MaxValue)
        {
            SetPixel(x, y, new RgbColor(red, green, blue));
            return;
        }

        var existing = GetPixel(x, y);
        var inverseAlpha = 255 - alpha;
        SetPixel(x, y, new RgbColor(
            (byte)((red * alpha + existing.R * inverseAlpha) / 255),
            (byte)((green * alpha + existing.G * inverseAlpha) / 255),
            (byte)((blue * alpha + existing.B * inverseAlpha) / 255)));
    }
}
