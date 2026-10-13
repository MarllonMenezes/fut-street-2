using System.Buffers.Binary;
using System.Text;

namespace FifaStreetPc;

public sealed record VivFRecord(
    uint Offset,
    uint CompressedSize,
    uint ExpandedSize,
    uint Checksum);

public sealed record VivFInspection(
    uint DeclaredSize,
    uint EntryCount,
    uint IndexEnd,
    bool DeclaredSizeMatches,
    bool OffsetsMonotonic,
    IReadOnlyList<VivFRecord> Records);

public sealed record VivFCompanionInspection(
    VivFInspection Package,
    bool RangesWithinCompanion,
    bool SentinelMatchesCompanionLength);

public static class VivFReader
{
    private const int HeaderSize = 16;
    private const int RecordSize = 16;

    public static VivFInspection Inspect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanSeek)
        {
            throw new NotSupportedException("VivF inspection requires a seekable stream.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var header = ReadExactly(stream, HeaderSize);
        var magic = Encoding.ASCII.GetString(header, 0, 4);
        if (magic != "VivF")
        {
            throw new InvalidDataException($"Unsupported VivF magic: {magic}");
        }

        var declaredSize = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));
        var entryCount = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(8, 4));
        var indexEnd = BinaryPrimitives.ReadUInt32BigEndian(header.AsSpan(12, 4));
        var recordCount = checked(entryCount + 1u);
        var expectedIndexEnd = checked((uint)((ulong)HeaderSize + (ulong)recordCount * RecordSize));
        if (indexEnd != expectedIndexEnd || indexEnd > stream.Length || indexEnd > int.MaxValue)
        {
            throw new InvalidDataException(
                $"VivF index has invalid end 0x{indexEnd:X}; expected 0x{expectedIndexEnd:X}.");
        }

        stream.Seek(HeaderSize, SeekOrigin.Begin);
        var records = new List<VivFRecord>(checked((int)recordCount));
        uint previousOffset = 0;
        for (var index = 0u; index < recordCount; index++)
        {
            var record = ReadExactly(stream, RecordSize);
            var current = new VivFRecord(
                BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(0, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(4, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(8, 4)),
                BinaryPrimitives.ReadUInt32BigEndian(record.AsSpan(12, 4)));
            if (index > 0 && current.Offset < previousOffset)
            {
                throw new InvalidDataException("VivF record offsets are not monotonic.");
            }
            records.Add(current);
            previousOffset = current.Offset;
        }

        return new VivFInspection(
            declaredSize,
            entryCount,
            indexEnd,
            declaredSize == stream.Length,
            true,
            records);
    }

    public static VivFCompanionInspection BindToCompanion(
        VivFInspection package,
        Stream companionStream)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(companionStream);

        var dataRecords = package.Records.Take(checked((int)package.EntryCount));
        var rangesWithinCompanion = dataRecords.All(record =>
            (ulong)record.Offset + record.CompressedSize <= (ulong)companionStream.Length);
        var sentinel = package.Records[checked((int)package.EntryCount)];
        var sentinelMatchesCompanionLength = sentinel.Offset == companionStream.Length;
        return new VivFCompanionInspection(
            package,
            rangesWithinCompanion,
            sentinelMatchesCompanionLength);
    }

    public static Stream OpenBlock(
        Stream companionStream,
        VivFInspection package,
        int blockIndex)
    {
        ArgumentNullException.ThrowIfNull(companionStream);
        ArgumentNullException.ThrowIfNull(package);
        if (blockIndex < 0 || blockIndex >= package.EntryCount)
        {
            throw new ArgumentOutOfRangeException(nameof(blockIndex));
        }
        if (!companionStream.CanSeek)
        {
            throw new NotSupportedException("VivF block access requires a seekable companion stream.");
        }

        var record = package.Records[blockIndex];
        if ((ulong)record.Offset + record.CompressedSize > (ulong)companionStream.Length)
        {
            throw new InvalidDataException("VivF block lies outside its companion container.");
        }

        companionStream.Seek(record.Offset, SeekOrigin.Begin);
        return new BoundedStream(companionStream, record.CompressedSize);
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
                throw new EndOfStreamException("Unexpected end of VivF package.");
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
                throw new IOException("Seek outside VivF block.");
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
