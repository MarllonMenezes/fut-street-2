using System.Buffers.Binary;
using System.Text;

namespace FifaStreetPc;

public sealed record EaSchlAudioInfo(
    int HeaderSize,
    int Version,
    int Platform,
    int Codec,
    int Channels,
    int SampleRate,
    uint DeclaredSamples);

public sealed record EaWaveExportInfo(
    EaSchlAudioInfo Stream,
    long SampleFrames,
    long DataBytes,
    int Blocks);

public sealed record EaSchlFirstDataChunk(
    long Offset,
    string Type,
    uint Size,
    bool WithinStream);

public sealed record EaSchlChunkEvidence(
    long Offset,
    string Type,
    uint Size,
    bool WithinStream,
    byte[] PayloadPrefix);

public sealed record EaSchlHeaderFieldEvidence(
    int Offset,
    byte Tag,
    int Length,
    uint Value);

public static class EaSchlAudioDecoder
{
    private const int MaximumInMemoryWaveBytes = 32 * 1024 * 1024;
    private const int SamplesPerFrame = 28;
    private const int AdpcmFrameBytes = 15;
    private const int PcmFrameBytes = 61;
    private const int MaximumBlockSamples = 1_000_000;
    private static readonly int[] Coefficient1 = [0, 240, 460, 392, 0, 0, -208, -220, 0, 1, 3, 4, 7, 8, 10, 11];
    private static readonly int[] Coefficient2 = [0, 0, -208, -220, 0, 0, 0, 0, 7, 8, 10, 11, 0, -1, -3, -4];

    public static EaSchlAudioInfo Inspect(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        var originalPosition = input.Position;
        try
        {
            input.Position = 0;
            return ReadHeader(input);
        }
        finally
        {
            input.Position = originalPosition;
        }
    }

    public static EaSchlFirstDataChunk InspectFirstDataChunk(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        var info = Inspect(input);
        input.Position = info.HeaderSize;
        if (input.Length - input.Position < 8)
        {
            throw new InvalidDataException("SCHl stream has no complete first data chunk header.");
        }

        Span<byte> chunkHeader = stackalloc byte[8];
        input.ReadExactly(chunkHeader);
        var chunkType = Encoding.ASCII.GetString(chunkHeader[..4]);
        var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..]);
        var withinStream = chunkSize >= 8 && chunkSize <= input.Length - info.HeaderSize;
        return new EaSchlFirstDataChunk(info.HeaderSize, chunkType, chunkSize, withinStream);
    }

    /// <summary>
    /// Inspects chunk headers and a bounded payload prefix without decoding any variant.
    /// This is intentionally evidence-only: callers must not treat an unknown chunk as
    /// EA-XA data merely because a later chunk has an SCDl signature.
    /// </summary>
    public static IReadOnlyList<EaSchlChunkEvidence> InspectChunkSequence(
        Stream input,
        int maximumChunks = 8,
        int payloadPrefixBytes = 16)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }
        if (maximumChunks < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumChunks));
        }
        if (payloadPrefixBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadPrefixBytes));
        }

        var info = Inspect(input);
        var chunks = new List<EaSchlChunkEvidence>(Math.Min(maximumChunks, 16));
        var chunkHeader = new byte[8];
        input.Position = info.HeaderSize;
        while (chunks.Count < maximumChunks && input.Position <= input.Length - 8)
        {
            var offset = input.Position;
            input.ReadExactly(chunkHeader);
            var chunkType = Encoding.ASCII.GetString(chunkHeader, 0, 4);
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader.AsSpan(4, 4));
            var remaining = input.Length - offset;
            var withinStream = chunkSize >= 8 && chunkSize <= remaining;
            var payloadLength = chunkSize >= 8
                ? Math.Min((long)payloadPrefixBytes, chunkSize - 8)
                : 0;
            var prefix = new byte[checked((int)payloadLength)];
            if (prefix.Length > 0 && input.Position <= input.Length - prefix.Length)
            {
                input.ReadExactly(prefix);
            }

            chunks.Add(new EaSchlChunkEvidence(offset, chunkType, chunkSize, withinStream, prefix));
            if (!withinStream)
            {
                break;
            }

            input.Position = checked(offset + chunkSize);
        }

        return chunks;
    }

    /// <summary>
    /// Returns bounded SCHl metadata fields that use the same tag/value rules as
    /// the decoder. Unknown or extended fields are skipped and never assigned a
    /// semantic name by this evidence API.
    /// </summary>
    public static IReadOnlyList<EaSchlHeaderFieldEvidence> InspectHeaderFields(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        var info = Inspect(input);
        var fields = new List<EaSchlHeaderFieldEvidence>();
        var extendedLength = new byte[4];
        var valueBytes = new byte[4];
        input.Position = 12;
        while (input.Position < info.HeaderSize)
        {
            var fieldOffset = checked((int)input.Position);
            var tag = input.ReadByte();
            if (tag < 0 || tag is 0xFF or 0xFE)
            {
                break;
            }
            if (tag is 0xFC or 0xFD)
            {
                continue;
            }

            var length = input.ReadByte();
            if (length < 0)
            {
                throw new EndOfStreamException("Unexpected end inside an SCHl metadata field.");
            }
            if (length == 0xFF)
            {
                input.ReadExactly(extendedLength);
                var extendedSize = BinaryPrimitives.ReadUInt32BigEndian(extendedLength);
                SkipWithin(input, extendedSize, info.HeaderSize);
                continue;
            }
            if (length > 4)
            {
                SkipWithin(input, length, info.HeaderSize);
                continue;
            }

            input.ReadExactly(valueBytes.AsSpan(0, length));
            var value = 0u;
            for (var index = 0; index < length; index++)
            {
                value = (value << 8) | valueBytes[index];
            }
            fields.Add(new EaSchlHeaderFieldEvidence(fieldOffset, checked((byte)tag), length, value));
        }

        return fields;
    }

    public static EaWaveExportInfo ExportWave(Stream input, string outputPath)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        input.Position = 0;
        var info = ReadHeader(input);
        EnsureSupported(info);
        var dataStart = info.HeaderSize;
        var scan = WalkBlocks(input, info, dataStart, null);
        var dataBytes = checked(scan.SampleFrames * info.Channels * sizeof(short));
        if (dataBytes > uint.MaxValue - 36L)
        {
            throw new InvalidDataException("Decoded audio exceeds the 32-bit RIFF/WAVE size limit.");
        }

        var fullPath = Path.GetFullPath(outputPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException("Could not resolve the output directory.");
        }
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            using (var output = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                WriteWaveHeader(output, info, checked((uint)dataBytes));
                input.Position = dataStart;
                var written = WalkBlocks(input, info, dataStart, (channels, samples) => WriteInterleaved(output, channels, samples));
                if (written.SampleFrames != scan.SampleFrames || written.Blocks != scan.Blocks)
                {
                    throw new InvalidDataException("The EA audio stream changed while it was being decoded.");
                }
                output.Flush(true);
            }

            File.Move(temporaryPath, fullPath, true);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
            throw;
        }

        return new EaWaveExportInfo(info, scan.SampleFrames, dataBytes, scan.Blocks);
    }

    public static EaWaveExportInfo Validate(Stream input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        input.Position = 0;
        var info = ReadHeader(input);
        EnsureSupported(info);
        var scan = WalkBlocks(input, info, info.HeaderSize, null);
        var dataBytes = checked(scan.SampleFrames * info.Channels * sizeof(short));
        if (dataBytes > uint.MaxValue - 36L)
        {
            throw new InvalidDataException("Decoded audio exceeds the 32-bit RIFF/WAVE size limit.");
        }

        return new EaWaveExportInfo(info, scan.SampleFrames, dataBytes, scan.Blocks);
    }

    /// <summary>
    /// Validates a channel-count hypothesis against the bounded SCDl layout and
    /// all decoded frames while preserving the observed SCHl header unchanged.
    /// This is an evidence-only candidate path for streams whose header omits
    /// tag 0x82; it does not rewrite metadata or authorize event playback.
    /// </summary>
    public static bool TryValidateChannelLayoutCandidate(
        Stream input,
        int candidateChannels,
        out EaWaveExportInfo? exportInfo,
        out string? issue)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }

        exportInfo = null;
        issue = null;
        try
        {
            input.Position = 0;
            var observedInfo = ReadHeader(input);
            var candidateInfo = observedInfo with { Channels = candidateChannels };
            EnsureSupported(candidateInfo);
            var scan = WalkBlocks(
                input,
                candidateInfo,
                candidateInfo.HeaderSize,
                static (_, _) => { });
            var dataBytes = checked(scan.SampleFrames * candidateChannels * sizeof(short));
            if (dataBytes > uint.MaxValue - 36L)
            {
                throw new InvalidDataException("Decoded audio exceeds the 32-bit RIFF/WAVE size limit.");
            }

            exportInfo = new EaWaveExportInfo(candidateInfo, scan.SampleFrames, dataBytes, scan.Blocks);
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or NotSupportedException)
        {
            issue = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Decodes a stream using a channel-layout candidate that was separately
    /// validated by <see cref="TryValidateChannelLayoutCandidate"/>. The
    /// observed SCHl metadata remains untouched and the caller must keep this
    /// path explicit; it is not automatic event or gameplay routing.
    /// </summary>
    public static byte[] DecodeWaveWithChannelLayoutCandidate(
        Stream input,
        int candidateChannels,
        int maximumWaveBytes = MaximumInMemoryWaveBytes)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }
        if (maximumWaveBytes < 44)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumWaveBytes), "The in-memory WAV limit must include its 44-byte header.");
        }

        input.Position = 0;
        var observedInfo = ReadHeader(input);
        var candidateInfo = observedInfo with { Channels = candidateChannels };
        EnsureSupported(candidateInfo);
        var dataStart = candidateInfo.HeaderSize;
        var scan = WalkBlocks(input, candidateInfo, dataStart, null);
        var dataBytes = checked(scan.SampleFrames * candidateChannels * sizeof(short));
        var waveBytes = checked(dataBytes + 44);
        if (waveBytes > maximumWaveBytes)
        {
            throw new InvalidDataException(
                $"Decoded WAV requires {waveBytes} bytes; in-memory playback is limited to {maximumWaveBytes} bytes.");
        }

        using var output = new MemoryStream(checked((int)waveBytes));
        WriteWaveHeader(output, candidateInfo, checked((uint)dataBytes));
        input.Position = dataStart;
        var decoded = WalkBlocks(
            input,
            candidateInfo,
            dataStart,
            (channels, samples) => WriteInterleaved(output, channels, samples));
        if (decoded.SampleFrames != scan.SampleFrames || decoded.Blocks != scan.Blocks)
        {
            throw new InvalidDataException("The EA-XA candidate stream changed while it was being decoded.");
        }

        return output.ToArray();
    }

    public static byte[] DecodeWave(Stream input, int maximumWaveBytes = MaximumInMemoryWaveBytes)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!input.CanRead || !input.CanSeek)
        {
            throw new ArgumentException("The EA audio stream must be readable and seekable.", nameof(input));
        }
        if (maximumWaveBytes < 44)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumWaveBytes), "The in-memory WAV limit must include its 44-byte header.");
        }

        input.Position = 0;
        var info = ReadHeader(input);
        EnsureSupported(info);
        var dataStart = info.HeaderSize;
        var scan = WalkBlocks(input, info, dataStart, null);
        var dataBytes = checked(scan.SampleFrames * info.Channels * sizeof(short));
        var waveBytes = checked(dataBytes + 44);
        if (waveBytes > maximumWaveBytes)
        {
            throw new InvalidDataException(
                $"Decoded WAV requires {waveBytes} bytes; in-memory playback is limited to {maximumWaveBytes} bytes.");
        }

        using var output = new MemoryStream(checked((int)waveBytes));
        WriteWaveHeader(output, info, checked((uint)dataBytes));
        input.Position = dataStart;
        var decoded = WalkBlocks(input, info, dataStart, (channels, samples) => WriteInterleaved(output, channels, samples));
        if (decoded.SampleFrames != scan.SampleFrames || decoded.Blocks != scan.Blocks)
        {
            throw new InvalidDataException("The EA audio stream changed while it was being decoded.");
        }
        return output.ToArray();
    }

    private static EaSchlAudioInfo ReadHeader(Stream input)
    {
        if (input.Length < 16)
        {
            throw new InvalidDataException("SCHl stream is shorter than its header.");
        }

        Span<byte> fixedHeader = stackalloc byte[8];
        input.ReadExactly(fixedHeader);
        if (!fixedHeader[..4].SequenceEqual("SCHl"u8))
        {
            throw new InvalidDataException("Unsupported EA audio stream signature; expected SCHl.");
        }

        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(fixedHeader[4..]);
        if (headerSize < 12 || headerSize > input.Length || headerSize > int.MaxValue)
        {
            throw new InvalidDataException("SCHl header size is outside the stream.");
        }

        var platformHeader = new byte[4];
        input.ReadExactly(platformHeader);
        if (!platformHeader.AsSpan(0, 2).SequenceEqual("PT"u8))
        {
            throw new InvalidDataException("SCHl stream does not contain an EA PT platform header.");
        }
        var platform = BinaryPrimitives.ReadUInt16LittleEndian(platformHeader.AsSpan(2));

        var version = -1;
        var codec = -1;
        var channels = 0;
        var sampleRate = 0;
        uint declaredSamples = 0;
        Span<byte> extendedLength = stackalloc byte[4];
        Span<byte> valueBytes = stackalloc byte[4];
        while (input.Position < headerSize)
        {
            var tag = input.ReadByte();
            if (tag < 0)
            {
                throw new EndOfStreamException("Unexpected end inside the SCHl metadata.");
            }
            if (tag is 0xFF or 0xFE)
            {
                break;
            }
            if (tag is 0xFC or 0xFD)
            {
                continue;
            }

            var patchLength = input.ReadByte();
            if (patchLength < 0)
            {
                throw new EndOfStreamException("Unexpected end inside an SCHl metadata patch.");
            }
            if (patchLength == 0xFF)
            {
                input.ReadExactly(extendedLength);
                var extraBytes = BinaryPrimitives.ReadUInt32BigEndian(extendedLength);
                SkipWithin(input, extraBytes, headerSize);
                continue;
            }
            if (patchLength > 4)
            {
                SkipWithin(input, patchLength, headerSize);
                continue;
            }

            input.ReadExactly(valueBytes[..patchLength]);
            var value = 0u;
            for (var index = 0; index < patchLength; index++)
            {
                value = (value << 8) | valueBytes[index];
            }

            switch (tag)
            {
                case 0x80:
                    version = checked((int)value);
                    break;
                case 0x82:
                    channels = checked((int)value);
                    break;
                case 0x84:
                    sampleRate = checked((int)value);
                    break;
                case 0x85:
                    declaredSamples = value;
                    break;
                case 0xA0:
                    codec = checked((int)value);
                    break;
            }
        }

        input.Position = headerSize;
        if (sampleRate == 0 && platform == 7)
        {
            sampleRate = 24000;
        }
        return new EaSchlAudioInfo(checked((int)headerSize), version, platform, codec, channels, sampleRate, declaredSamples);
    }

    private static void EnsureSupported(EaSchlAudioInfo info)
    {
        if (info.Platform != 7 || info.Version != 3 || info.Codec != 0x0A)
        {
            throw new NotSupportedException(
                $"Only EA-XA v3/Xbox streams are decoded currently (platform={info.Platform}, version={info.Version}, codec=0x{info.Codec:X2}).");
        }
        if (info.Channels is < 1 or > 2 || info.SampleRate is < 8000 or > 192000)
        {
            throw new InvalidDataException($"Invalid EA-XA audio format: {info.Channels} channels at {info.SampleRate} Hz.");
        }
    }

    private static (long SampleFrames, int Blocks) WalkBlocks(
        Stream input,
        EaSchlAudioInfo info,
        long dataStart,
        Action<short[][], int>? decodedBlock)
    {
        input.Position = dataStart;
        long totalFrames = 0;
        var blocks = 0;
        Span<byte> chunkHeader = stackalloc byte[8];
        Span<byte> sampleHeader = stackalloc byte[4];
        while (input.Position < input.Length)
        {
            var chunkStart = input.Position;
            if (input.Length - chunkStart < chunkHeader.Length)
            {
                throw new InvalidDataException("Truncated EA audio chunk header.");
            }
            input.ReadExactly(chunkHeader);
            var chunkType = Encoding.ASCII.GetString(chunkHeader[..4]);
            var chunkSize = BinaryPrimitives.ReadUInt32LittleEndian(chunkHeader[4..]);
            if (chunkSize < 8 || chunkSize > input.Length - chunkStart)
            {
                throw new InvalidDataException($"Invalid {chunkType} chunk size at offset {chunkStart}.");
            }
            var chunkEnd = checked(chunkStart + chunkSize);

            if (chunkType == "SCEl")
            {
                break;
            }
            if (chunkType == "SCCl")
            {
                // The Xbox catalog places a small control/envelope chunk before
                // decodable SCDl blocks. Its payload semantics are not required
                // for the validated SCDl stream, so preserve it structurally and
                // advance without interpreting its value.
                input.Position = chunkEnd;
                continue;
            }
            if (chunkType == "SCDl")
            {
                if (chunkSize < 12)
                {
                    throw new InvalidDataException("SCDl block is shorter than its sample-count header.");
                }
                input.ReadExactly(sampleHeader);
                var sampleCount = BinaryPrimitives.ReadUInt32LittleEndian(sampleHeader);
                if (sampleCount > MaximumBlockSamples)
                {
                    throw new InvalidDataException("SCDl block declares an unreasonable sample count.");
                }

                if (decodedBlock is not null && sampleCount > 0)
                {
                    var decoded = DecodeBlock(input, chunkStart, chunkSize, checked((int)sampleCount), info.Channels);
                    decodedBlock(decoded, checked((int)sampleCount));
                }
                else if (sampleCount > 0)
                {
                    ValidateBlockTable(input, chunkStart, chunkSize, info.Channels);
                }
                totalFrames = checked(totalFrames + sampleCount);
                blocks++;
            }

            input.Position = chunkEnd;
        }

        if (blocks == 0 || totalFrames == 0)
        {
            throw new InvalidDataException("The SCHl stream contains no decoded SCDl audio blocks.");
        }
        return (totalFrames, blocks);
    }

    private static short[][] DecodeBlock(Stream input, long chunkStart, uint chunkSize, int sampleCount, int channels)
    {
        var starts = ReadChannelStarts(input, chunkStart, chunkSize, channels);
        var result = new short[channels][];
        for (var channel = 0; channel < channels; channel++)
        {
            var channelStart = starts[channel];
            var channelEnd = chunkStart + chunkSize;
            for (var other = 0; other < starts.Length; other++)
            {
                if (starts[other] > channelStart && starts[other] < channelEnd)
                {
                    channelEnd = starts[other];
                }
            }
            result[channel] = DecodeChannel(input, channelStart, channelEnd, sampleCount);
        }
        return result;
    }

    private static long[] ReadChannelStarts(Stream input, long chunkStart, uint chunkSize, int channels)
    {
        var tableEnd = checked(12 + channels * 4);
        if (chunkSize < tableEnd)
        {
            throw new InvalidDataException("SCDl channel-offset table exceeds the block.");
        }
        var offsets = new uint[channels];
        Span<byte> offsetBytes = stackalloc byte[4];
        for (var channel = 0; channel < channels; channel++)
        {
            input.Position = chunkStart + 12L + channel * 4;
            input.ReadExactly(offsetBytes);
            offsets[channel] = BinaryPrimitives.ReadUInt32LittleEndian(offsetBytes);
        }

        var starts = new long[channels];
        for (var channel = 0; channel < channels; channel++)
        {
            var relative = checked((ulong)tableEnd + offsets[channel]);
            if (relative >= chunkSize)
            {
                throw new InvalidDataException("SCDl channel offset points outside its block.");
            }
            starts[channel] = checked(chunkStart + (long)relative);
        }
        return starts;
    }

    private static void ValidateBlockTable(Stream input, long chunkStart, uint chunkSize, int channels)
    {
        _ = ReadChannelStarts(input, chunkStart, chunkSize, channels);
    }

    private static short[] DecodeChannel(Stream input, long start, long end, int sampleCount)
    {
        var samples = new short[sampleCount];
        var cursor = start;
        var written = 0;
        var history1 = 0;
        var history2 = 0;
        Span<byte> frame = stackalloc byte[PcmFrameBytes];

        while (written < sampleCount)
        {
            if (cursor >= end)
            {
                throw new InvalidDataException("EA-XA channel data ended before its declared sample count.");
            }
            input.Position = cursor;
            var frameInfo = input.ReadByte();
            if (frameInfo < 0)
            {
                throw new EndOfStreamException("Unexpected end in EA-XA frame.");
            }
            var pcmFrame = frameInfo == 0xEE;
            var frameSize = pcmFrame ? PcmFrameBytes : AdpcmFrameBytes;
            if (frameSize > end - cursor)
            {
                throw new InvalidDataException("EA-XA frame crosses the channel data boundary.");
            }
            frame[0] = (byte)frameInfo;
            input.ReadExactly(frame[1..frameSize]);
            var inFrame = Math.Min(SamplesPerFrame, sampleCount - written);

            if (pcmFrame)
            {
                history1 = BinaryPrimitives.ReadInt16BigEndian(frame[1..3]);
                history2 = BinaryPrimitives.ReadInt16BigEndian(frame[3..5]);
                for (var index = 0; index < inFrame; index++)
                {
                    samples[written + index] = BinaryPrimitives.ReadInt16BigEndian(frame.Slice(5 + index * 2, 2));
                }
            }
            else
            {
                var filter = frameInfo >> 4;
                if (filter >= Coefficient1.Length)
                {
                    throw new InvalidDataException($"EA-XA frame uses unsupported predictor {filter}.");
                }
                var shift = (frameInfo & 0x0F) + 8;
                for (var index = 0; index < inFrame; index++)
                {
                    var packed = frame[1 + index / 2];
                    var nibble = (index & 1) == 0 ? packed >> 4 : packed & 0x0F;
                    var signedNibble = nibble >= 8 ? nibble - 16 : nibble;
                    var expanded = (signedNibble << 28) >> shift;
                    var decoded = (expanded + Coefficient1[filter] * history1 + Coefficient2[filter] * history2) >> 8;
                    decoded = Math.Clamp(decoded, short.MinValue, short.MaxValue);
                    samples[written + index] = (short)decoded;
                    history2 = history1;
                    history1 = decoded;
                }
            }

            written += inFrame;
            cursor += frameSize;
        }

        return samples;
    }

    private static void WriteInterleaved(Stream output, short[][] channels, int sampleCount)
    {
        var interleaved = new byte[checked(sampleCount * channels.Length * sizeof(short))];
        var cursor = 0;
        for (var sample = 0; sample < sampleCount; sample++)
        {
            for (var channel = 0; channel < channels.Length; channel++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(interleaved.AsSpan(cursor, 2), channels[channel][sample]);
                cursor += 2;
            }
        }
        output.Write(interleaved);
    }

    private static void WriteWaveHeader(Stream output, EaSchlAudioInfo info, uint dataBytes)
    {
        Span<byte> header = stackalloc byte[44];
        "RIFF"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..8], checked(dataBytes + 36));
        "WAVEfmt "u8.CopyTo(header[8..16]);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..20], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(header[20..22], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..24], checked((ushort)info.Channels));
        BinaryPrimitives.WriteUInt32LittleEndian(header[24..28], checked((uint)info.SampleRate));
        var blockAlign = checked((ushort)(info.Channels * sizeof(short)));
        BinaryPrimitives.WriteUInt32LittleEndian(header[28..32], checked((uint)(info.SampleRate * blockAlign)));
        BinaryPrimitives.WriteUInt16LittleEndian(header[32..34], blockAlign);
        BinaryPrimitives.WriteUInt16LittleEndian(header[34..36], 16);
        "data"u8.CopyTo(header[36..40]);
        BinaryPrimitives.WriteUInt32LittleEndian(header[40..44], dataBytes);
        output.Write(header);
    }

    private static void SkipWithin(Stream input, long count, long boundary)
    {
        var destination = checked(input.Position + count);
        if (destination > boundary)
        {
            throw new InvalidDataException("SCHl metadata patch exceeds its header boundary.");
        }
        input.Position = destination;
    }
}
