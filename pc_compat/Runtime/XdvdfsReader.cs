using System.Buffers.Binary;
using System.Text;

namespace FifaPcCompat.Runtime;

public sealed record XdvdfsEntry(
    string Path,
    string Name,
    bool IsDirectory,
    uint Size,
    uint StartSector,
    long? ImageOffset);

public sealed class XdvdfsReader : IDisposable
{
    private const int SectorSize = 2048;
    private const int HeaderOffset = 0x10000;
    private const int DirectoryAttribute = 0x10;
    private const ushort PaddingWord = 0xFFFF;
    private static readonly byte[] MediaMarker = "MICROSOFT*XBOX*MEDIA"u8.ToArray();
    private static readonly int[] PartitionCandidates = [0, 0x0FD90000, 0x02080000, 0x18300000];

    private readonly FileStream stream;
    private readonly string imagePath;
    private readonly long imageSize;
    private readonly long partitionOffset;
    private readonly uint rootSector;
    private readonly uint rootSize;

    public XdvdfsReader(string imagePath)
    {
        this.imagePath = imagePath;
        stream = File.OpenRead(imagePath);
        imageSize = stream.Length;
        (partitionOffset, rootSector, rootSize) = ReadVolume();
    }

    public long PartitionOffset => partitionOffset;
    public uint RootSector => rootSector;
    public uint RootSize => rootSize;

    public void Dispose() => stream.Dispose();

    public byte[] ReadFile(string path)
    {
        var entry = Entries().SingleOrDefault(item =>
            string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));
        if (entry is null || entry.IsDirectory || entry.ImageOffset is null)
        {
            throw new FileNotFoundException($"XDVDFS file not found: {path}");
        }

        return ReadAt(entry.ImageOffset.Value, checked((int)entry.Size));
    }

    public Stream OpenFileStream(string path)
    {
        var entry = Entries().SingleOrDefault(item =>
            string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase));
        if (entry is null || entry.IsDirectory || entry.ImageOffset is null)
        {
            throw new FileNotFoundException($"XDVDFS file not found: {path}");
        }

        return new XdvdfsSliceStream(File.OpenRead(imagePath), entry.ImageOffset.Value, entry.Size);
    }

    public IReadOnlyList<XdvdfsEntry> Entries()
    {
        var rootOffset = checked(partitionOffset + (long)rootSector * SectorSize);
        var rootEntries = WalkDirectory(rootOffset, rootSize);
        var result = new List<XdvdfsEntry>();

        void Descend(IEnumerable<XdvdfsEntry> entries, string prefix)
        {
            foreach (var entry in entries)
            {
                var fullPath = prefix + entry.Name;
                var current = entry with { Path = fullPath };
                result.Add(current);
                if (entry.IsDirectory && entry.Size > 0)
                {
                    var directoryOffset = checked(partitionOffset + (long)entry.StartSector * SectorSize);
                    Descend(WalkDirectory(directoryOffset, entry.Size), fullPath + "/");
                }
            }
        }

        Descend(rootEntries, string.Empty);
        return result;
    }

    private (long PartitionOffset, uint RootSector, uint RootSize) ReadVolume()
    {
        foreach (var candidate in PartitionCandidates)
        {
            var markerOffset = (long)candidate + HeaderOffset;
            if (markerOffset + 28 > imageSize)
            {
                continue;
            }

            var header = ReadAt(markerOffset, 28);
            if (!header.AsSpan(0, MediaMarker.Length).SequenceEqual(MediaMarker))
            {
                continue;
            }

            var rootSector = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(20, 4));
            var rootSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(24, 4));
            var rootOffset = checked((long)candidate + (long)rootSector * SectorSize);
            if (rootSize == 0 || rootOffset + rootSize > imageSize)
            {
                throw new InvalidDataException("XDVDFS root directory is outside the image.");
            }

            return (candidate, rootSector, rootSize);
        }

        throw new InvalidDataException("XDVDFS volume marker not found.");
    }

    private List<XdvdfsEntry> WalkDirectory(long directoryOffset, uint directorySize)
    {
        var result = new List<XdvdfsEntry>();
        var visited = new HashSet<int>();

        void Walk(int nodeOffset, int depth)
        {
            if (!visited.Add(nodeOffset))
            {
                return;
            }
            if (depth > 4096 || nodeOffset < 0 || nodeOffset + 14 > directorySize)
            {
                throw new InvalidDataException("XDVDFS directory tree is invalid.");
            }

            var fixedPart = ReadAt(directoryOffset + nodeOffset, 14);
            if (BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(0, 2)) == PaddingWord)
            {
                return;
            }

            var left = BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(0, 2)) * 4;
            var right = BinaryPrimitives.ReadUInt16LittleEndian(fixedPart.AsSpan(2, 2)) * 4;
            var startSector = BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(4, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(fixedPart.AsSpan(8, 4));
            var attributes = fixedPart[12];
            var nameLength = fixedPart[13];
            if (nameLength == 0 || nameLength > 255 || nodeOffset + 14 + nameLength > directorySize)
            {
                throw new InvalidDataException("XDVDFS directory entry has an invalid name.");
            }

            var nameBytes = ReadAt(directoryOffset + nodeOffset + 14, nameLength);
            var name = Encoding.ASCII.GetString(nameBytes);
            var isDirectory = (attributes & DirectoryAttribute) != 0;
            long? imageOffset = isDirectory ? null : checked(partitionOffset + (long)startSector * SectorSize);
            if (imageOffset is not null && imageOffset.Value + size > imageSize)
            {
                throw new InvalidDataException($"XDVDFS entry outside image: {name}");
            }

            Walk(left, depth + 1);
            result.Add(new XdvdfsEntry(name, name, isDirectory, size, startSector, imageOffset));
            Walk(right, depth + 1);
        }

        Walk(0, 0);
        return result;
    }

    private byte[] ReadAt(long offset, int count)
    {
        if (offset < 0 || count < 0 || offset + count > imageSize)
        {
            throw new InvalidDataException($"Read outside image: 0x{offset:X}+{count:X}");
        }

        var buffer = new byte[count];
        stream.Seek(offset, SeekOrigin.Begin);
        var read = 0;
        while (read < count)
        {
            var chunk = stream.Read(buffer, read, count - read);
            if (chunk == 0)
            {
                throw new EndOfStreamException($"Short read at 0x{offset:X}.");
            }
            read += chunk;
        }
        return buffer;
    }

    private sealed class XdvdfsSliceStream(FileStream source, long start, long length) : Stream
    {
        private long position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position
        {
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            var available = Math.Min(count, checked((int)Math.Min(length - position, int.MaxValue)));
            if (available <= 0)
            {
                return 0;
            }
            source.Seek(start + position, SeekOrigin.Begin);
            var read = source.Read(buffer, offset, available);
            position += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var next = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => position + offset,
                SeekOrigin.End => length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin)),
            };
            if (next < 0 || next > length)
            {
                throw new IOException("Seek outside XDVDFS file extent.");
            }
            position = next;
            return position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                source.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
