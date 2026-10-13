namespace FifaPcCompat.Runtime;

public sealed class XdvdfsFileSystem : IXboxFileSystem, IDisposable
{
    private readonly XdvdfsReader reader;

    public XdvdfsFileSystem(string imagePath)
    {
        reader = new XdvdfsReader(imagePath);
    }

    public Stream OpenReadOnly(string virtualPath) => reader.OpenFileStream(virtualPath);

    public IEnumerable<string> Enumerate(string virtualDirectory)
    {
        var prefix = virtualDirectory.Trim('/');
        if (prefix.Length > 0)
        {
            prefix += "/";
        }

        return reader.Entries()
            .Where(entry => entry.Path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Where(entry => !entry.IsDirectory)
            .Select(entry => entry.Path)
            .ToArray();
    }

    public void Dispose() => reader.Dispose();
}
