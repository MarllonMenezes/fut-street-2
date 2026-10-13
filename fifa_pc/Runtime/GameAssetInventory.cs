using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public enum AssetKind
{
    Unknown,
    BigArchive,
    AudioBank,
    AudioStream,
    AnimationPackage,
    ShapeImagePackage,
    TextureImage,
    Movie,
    Database,
    Metadata,
}

public sealed record GameAssetDescriptor(
    string Path,
    long Size,
    string Signature,
    AssetKind Kind);

public static class GameAssetInventory
{
    public static GameAssetDescriptor Describe(IXboxFileSystem fileSystem, string virtualPath)
    {
        using var stream = fileSystem.OpenReadOnly(virtualPath);
        var header = new byte[(int)Math.Min(16L, stream.Length)];
        var read = stream.Read(header, 0, header.Length);
        return Describe(virtualPath, stream.Length, header.AsSpan(0, read));
    }

    public static GameAssetDescriptor Describe(string virtualPath, long size, ReadOnlySpan<byte> header)
    {
        var signature = FormatSignature(header);
        return new GameAssetDescriptor(
            virtualPath,
            size,
            signature,
            Classify(virtualPath, signature));
    }

    public static IReadOnlyList<GameAssetDescriptor> Scan(
        IXboxFileSystem fileSystem,
        IEnumerable<string> virtualPaths)
    {
        return virtualPaths
            .Select(path => Describe(fileSystem, path))
            .OrderBy(asset => asset.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static AssetKind Classify(string virtualPath, string signature)
    {
        var extension = Path.GetExtension(virtualPath).ToLowerInvariant();
        return extension switch
        {
            ".big" or ".viv" when signature is "BIG4" or "BIGF" => AssetKind.BigArchive,
            ".big" or ".ast" when signature == "SCHl" => AssetKind.AudioStream,
            ".abk" when signature == "ABKC" => AssetKind.AudioBank,
            ".bh" when signature == "VivF" => AssetKind.AnimationPackage,
            ".xsh" when signature == "SHPX" => AssetKind.ShapeImagePackage,
            ".xbx" when signature == "XPR0" => AssetKind.TextureImage,
            ".vp6" when signature is "MVhd" or "SCHl" => AssetKind.Movie,
            ".csi" or ".evt" or ".mpf" or ".off" or ".pmb" => AssetKind.Metadata,
            _ => AssetKind.Unknown,
        };
    }

    private static string FormatSignature(ReadOnlySpan<byte> header)
    {
        if (header.Length == 0)
        {
            return string.Empty;
        }

        var fourCcLength = Math.Min(4, header.Length);
        var fourCc = Encoding.ASCII.GetString(header[..fourCcLength]);
        if (fourCc.Length == 4 && fourCc.All(character => character is >= ' ' and <= '~'))
        {
            return fourCc;
        }

        return Convert.ToHexString(header[..fourCcLength]);
    }
}
