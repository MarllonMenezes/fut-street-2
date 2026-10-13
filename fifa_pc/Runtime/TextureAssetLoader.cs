namespace FifaStreetPc;

public sealed record PcTextureResource(
    uint FormatWord,
    uint HeaderWord24,
    int Width,
    int Height,
    string PixelFormatName,
    byte[] RawResource,
    byte[] DecodedRgba);

public static class TextureAssetLoader
{
    public static PcTextureResource Load(ReadOnlySpan<byte> asset)
    {
        var inspection = XprReader.Inspect(asset);
        var resource = asset.Slice(
            checked((int)inspection.DataOffset),
            checked((int)inspection.ResourceSize)).ToArray();
        var decoded = XprTextureDecoder.Decode(inspection, resource);
        return new PcTextureResource(
            inspection.ResourceFormat,
            inspection.HeaderWord24,
            inspection.Width,
            inspection.Height,
            inspection.PixelFormatName,
            resource,
            decoded);
    }
}
