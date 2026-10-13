namespace FifaStreetPc;

/// <summary>
/// Read-only view over one validated range of an audio companion file.
/// It never exposes bytes outside the CSV-defined range and leaves the source
/// stream open for callers that scan several rows.
/// </summary>
public sealed class AudioRangeStream : Stream
{
    private readonly Stream source;
    private readonly long sourceOffset;
    private readonly long rangeLength;
    private readonly bool leaveOpen;
    private long position;

    public AudioRangeStream(Stream source, long sourceOffset, long rangeLength, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead || !source.CanSeek)
        {
            throw new ArgumentException("The source stream must support reading and seeking.", nameof(source));
        }
        if (sourceOffset < 0 || rangeLength < 0 || sourceOffset > source.Length - rangeLength)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceOffset), "The requested audio range is outside the source stream.");
        }

        this.source = source;
        this.sourceOffset = sourceOffset;
        this.rangeLength = rangeLength;
        this.leaveOpen = leaveOpen;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => rangeLength;

    public override long Position
    {
        get => position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (offset < 0 || count < 0 || offset > buffer.Length - count)
        {
            throw new ArgumentOutOfRangeException(nameof(offset));
        }
        if (position == rangeLength || count == 0)
        {
            return 0;
        }

        var requested = (int)Math.Min(count, rangeLength - position);
        source.Seek(sourceOffset + position, SeekOrigin.Begin);
        var read = source.Read(buffer, offset, requested);
        if (read < 0 || read > requested)
        {
            throw new IOException("The source stream returned an invalid read length.");
        }
        position += read;
        return read;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var next = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => position + offset,
            SeekOrigin.End => rangeLength + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin)),
        };
        if (next < 0 || next > rangeLength)
        {
            throw new IOException("The requested position is outside the audio range.");
        }
        position = next;
        return position;
    }

    public override void Flush() { }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !leaveOpen)
        {
            source.Dispose();
        }
        base.Dispose(disposing);
    }
}
