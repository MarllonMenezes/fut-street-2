using System.Text;

namespace FifaStreetPc;

public static class TexturePreviewWriter
{
    public static void SavePpm(PcTextureResource texture, string path)
    {
        var fullPath = Path.GetFullPath(path);
        using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var header = Encoding.ASCII.GetBytes($"P6\n{texture.Width} {texture.Height}\n255\n");
        output.Write(header);
        for (var pixel = 0; pixel < texture.Width * texture.Height; pixel++)
        {
            var offset = pixel * 4;
            output.WriteByte(texture.DecodedRgba[offset]);
            output.WriteByte(texture.DecodedRgba[offset + 1]);
            output.WriteByte(texture.DecodedRgba[offset + 2]);
        }
    }

    public static void SavePpm(PcShapeImageTexture texture, string path)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (texture.Width <= 0 || texture.Height <= 0 ||
            texture.DecodedRgba.Length != checked(texture.Width * texture.Height * 4))
        {
            throw new InvalidDataException("SHPX texture dimensions do not match its RGBA buffer.");
        }

        var fullPath = Path.GetFullPath(path);
        using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var header = Encoding.ASCII.GetBytes($"P6\n{texture.Width} {texture.Height}\n255\n");
        output.Write(header);
        for (var y = 0; y < texture.Height; y++)
        {
            for (var x = 0; x < texture.Width; x++)
            {
                var sourceOffset = checked((y * texture.Width + x) * 4);
                var alpha = texture.DecodedRgba[sourceOffset + 3];
                var background = ((x / 8 + y / 8) & 1) == 0 ? (byte)192 : (byte)128;
                for (var channel = 0; channel < 3; channel++)
                {
                    var foreground = texture.DecodedRgba[sourceOffset + channel];
                    output.WriteByte((byte)((foreground * alpha + background * (255 - alpha) + 127) / 255));
                }
            }
        }
    }
}
