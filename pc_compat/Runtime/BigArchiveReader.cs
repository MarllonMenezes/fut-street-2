using System.Buffers.Binary;
using System.Text;

namespace FifaPcCompat.Runtime;

public sealed record BigArchiveEntry(string Name, uint Offset, uint Size);

public sealed record BigArchiveInspection(
    string Magic,
    uint ArchiveSize,
    uint FileCount,
    uint IndexEnd,
    bool ArchiveSizeMatches,
    IReadOnlyList<BigArchiveEntry> Entries);

public static class BigArchiveReader
{
    public static Stream OpenEntry(IXboxFileSystem fileSystem, string archivePath, string entryName)
    {
        var inspection = Inspect(fileSystem, archivePath);
        var entry = inspection.Entries.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new FileNotFoundException($"BIG entry not found: {entryName}", entryName);
        }

        var archive = fileSystem.OpenReadOnly(archivePath);
        archive.Seek(entry.Offset, SeekOrigin.Begin);
        return new BoundedStream(archive, entry.Size);
    }

    public static byte[] ReadEntry(byte[] archiveData, string entryName)
    {
        ArgumentNullException.ThrowIfNull(archiveData);
        var inspection = Inspect(archiveData);
        var entry = inspection.Entries.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, entryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new FileNotFoundException($"BIG entry not found: {entryName}", entryName);
        }

        var content = new byte[checked((int)entry.Size)];
        Buffer.BlockCopy(archiveData, checked((int)entry.Offset), content, 0, content.Length);
        return content;
    }

    public static BigArchiveInspection Inspect(IXboxFileSystem fileSystem, string virtualPath)
    {
        using var stream = fileSystem.OpenReadOnly(virtualPath);
        return Inspect(stream);
    }

    public static BigArchiveInspection Inspect(byte[] archiveData)
    {
        ArgumentNullException.ThrowIfNull(archiveData);
        using var stream = new MemoryStream(archiveData, writable: false);
        return Inspect(stream);
    }

    public static BigArchiveInspection Inspect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("BIG inspection requires a seekable stream.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var header = ReadExactly(stream, 16);
        var magic = Encoding.ASCII.GetString(header, 0, 4);
        if (magic is not ("BIG4" or "BIGF"))
        {
            throw new InvalidDataException($"Unsupported BIG magic: {magic}");
        }

        var archiveSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));
        var fileCount = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8, 4));
        var indexEnd = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(12, 4));
        if (indexEnd < 16 || indexEnd > stream.Length || indexEnd > int.MaxValue)
        {
            throw new InvalidDataException("BIG index is outside the archive.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var index = ReadExactly(stream, checked((int)indexEnd));
        var entries = new List<BigArchiveEntry>(checked((int)Math.Min(fileCount, 1_000_000)));
        var cursor = 16;
        for (var indexNumber = 0u; indexNumber < fileCount; indexNumber++)
        {
            if (cursor + 8 > index.Length)
            {
                throw new InvalidDataException($"BIG entry {indexNumber} is truncated.");
            }

            var offset = BinaryPrimitives.ReadUInt32BigEndian(index.AsSpan(cursor, 4));
            var size = BinaryPrimitives.ReadUInt32BigEndian(index.AsSpan(cursor + 4, 4));
            var nameEnd = Array.IndexOf(index, (byte)0, cursor + 8);
            if (nameEnd < 0)
            {
                throw new InvalidDataException($"BIG entry {indexNumber} has no terminator.");
            }
            var name = Encoding.ASCII.GetString(index, cursor + 8, nameEnd - cursor - 8);
            if ((ulong)offset + size > (ulong)stream.Length || offset < indexEnd)
            {
                throw new InvalidDataException($"BIG entry {indexNumber} is outside the archive.");
            }
            entries.Add(new BigArchiveEntry(name, offset, size));
            cursor = nameEnd + 1;
        }

        return new BigArchiveInspection(magic, archiveSize, fileCount, indexEnd,
            archiveSize == stream.Length, entries);
    }

    private static byte[] ReadExactly(Stream stream, int count)
    {
        var data = new byte[count];
        var offset = 0;
        while (offset < count)
        {
            var read = stream.Read(data, offset, count - offset);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of BIG archive.");
            }
            offset += read;
        }
        return data;
    }

    private sealed class BoundedStream : Stream
    {
        private readonly Stream source;
        private readonly long sourceStart;
        private readonly long length;
        private long position;

        public BoundedStream(Stream source, long length)
        {
            this.source = source;
            sourceStart = source.Position;
            this.length = length;
        }

        public override bool CanRead => source.CanRead;
        public override bool CanSeek => source.CanSeek;
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
            if ((uint)offset > (uint)buffer.Length || count < 0 || buffer.Length - offset < count)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var available = length - position;
            if (available <= 0 || count == 0)
            {
                return 0;
            }

            var requested = (int)Math.Min(count, available);
            source.Position = sourceStart + position;
            var read = source.Read(buffer, offset, requested);
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
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            if (next < 0 || next > length)
            {
                throw new IOException("Seek outside BIG entry.");
            }

            position = next;
            return position;
        }

        public override void Flush() => throw new NotSupportedException();
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
