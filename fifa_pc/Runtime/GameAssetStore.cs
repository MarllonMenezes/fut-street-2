using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed class VivFBlock : IDisposable
{
    private readonly Stream data;

    internal VivFBlock(
        string packagePath,
        string companionPath,
        int index,
        VivFRecord record,
        Stream data)
    {
        PackagePath = packagePath;
        CompanionPath = companionPath;
        Index = index;
        Record = record;
        this.data = data;
    }

    public string PackagePath { get; }
    public string CompanionPath { get; }
    public int Index { get; }
    public VivFRecord Record { get; }
    public Stream Data => data;

    public void Dispose() => data.Dispose();
}

public sealed class GameAssetStore : IDisposable
{
    private readonly XdvdfsFileSystem fileSystem;

    public GameAssetStore(string imagePath)
    {
        fileSystem = new XdvdfsFileSystem(imagePath);
    }

    public Stream OpenRaw(string virtualPath) => fileSystem.OpenReadOnly(virtualPath);

    public Stream OpenBigEntry(string archivePath, string entryName) =>
        BigArchiveReader.OpenEntry(fileSystem, archivePath, entryName);

    public VivFInspection InspectVivF(string packagePath)
    {
        using var packageStream = fileSystem.OpenReadOnly(packagePath);
        return VivFReader.Inspect(packageStream);
    }

    public VivFBlock OpenVivFBlock(string packagePath, int blockIndex)
    {
        var companionPath = FindCompanionPath(packagePath);
        if (companionPath is null)
        {
            throw new FileNotFoundException($"VivF companion not found: {packagePath}", packagePath);
        }

        using var packageStream = fileSystem.OpenReadOnly(packagePath);
        var package = VivFReader.Inspect(packageStream);
        if (blockIndex < 0 || blockIndex >= package.EntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        }

        var companionStream = fileSystem.OpenReadOnly(companionPath);
        try
        {
            var blockStream = VivFReader.OpenBlock(companionStream, package, blockIndex);
            return new VivFBlock(packagePath, companionPath, blockIndex, package.Records[blockIndex], blockStream);
        }
        catch
        {
            companionStream.Dispose();
            throw;
        }
    }

    public void Dispose() => fileSystem.Dispose();

    private string? FindCompanionPath(string packagePath)
    {
        var paths = fileSystem.Enumerate("data").ToHashSet(StringComparer.OrdinalIgnoreCase);
        var stem = Path.ChangeExtension(packagePath, null);
        var bigPath = stem + ".big";
        if (paths.Contains(bigPath))
        {
            return paths.First(path => string.Equals(path, bigPath, StringComparison.OrdinalIgnoreCase));
        }

        var vivPath = stem + ".viv";
        if (paths.Contains(vivPath))
        {
            return paths.First(path => string.Equals(path, vivPath, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}
