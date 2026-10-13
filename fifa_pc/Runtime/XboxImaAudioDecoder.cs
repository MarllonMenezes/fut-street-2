using System.Buffers.Binary;

namespace FifaStreetPc;

public sealed record XboxImaAudioInfo(
    int Channels,
    int SampleRate,
    uint SampleFrames,
    int Blocks,
    long EncodedBytes,
    long PcmBytes);

/// <summary>
/// Decodes the Xbox IMA blocks observed in the embedded EA BNK headers.
/// Offsets are absolute within the supplied asset buffer; the caller must
/// obtain them from the BNK header rather than guessing a layout.
/// </summary>
public static class XboxImaAudioDecoder
{
    private const int BlockBytes = 0x24;
    private const int SamplesPerBlock = 65;
    private const int MaximumChannels = 2;
    private const int MaximumSampleRate = 192000;
    private const uint MaximumSampleFrames = 10_000_000;
    private const int MaximumWaveBytes = 32 * 1024 * 1024;

    private static readonly int[] StepTable =
    [
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31,
        34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118, 130, 143,
        157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544,
        598, 658, 724, 796, 876, 963, 1060, 1166, 1282, 1411, 1552, 1707,
        1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871,
        5358, 5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635,
        13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767,
    ];

    private static readonly int[] IndexTable = [-1, -1, -1, -1, 2, 4, 6, 8,
        -1, -1, -1, -1, 2, 4, 6, 8];

    public static XboxImaAudioInfo Validate(
        ReadOnlySpan<byte> data,
        int channels,
        int sampleRate,
        uint sampleFrames,
        IReadOnlyList<uint> channelOffsets)
    {
        ValidateArguments(data, channels, sampleRate, sampleFrames, channelOffsets);
        var blocks = checked((int)((sampleFrames + SamplesPerBlock - 1u) / SamplesPerBlock));
        var encodedBytes = checked((long)blocks * BlockBytes * channels);
        var pcmBytes = checked((long)sampleFrames * channels * sizeof(short));
        if (pcmBytes > MaximumWaveBytes - 44L)
        {
            throw new InvalidDataException("Xbox IMA PCM output exceeds the in-memory WAV limit.");
        }

        for (var channel = 0; channel < channels; channel++)
        {
            var offset = channelOffsets[channel];
            var channelBytes = checked((long)blocks * BlockBytes);
            if (offset > (ulong)data.Length || channelBytes > data.Length - (long)offset)
            {
                throw new InvalidDataException(
                    $"Xbox IMA channel {channel} range 0x{offset:X}+0x{channelBytes:X} exceeds the asset.");
            }

            ValidateBlockHeaders(data, checked((int)offset), blocks);
        }

        return new XboxImaAudioInfo(channels, sampleRate, sampleFrames, blocks, encodedBytes, pcmBytes);
    }

    public static byte[] DecodeWave(
        ReadOnlySpan<byte> data,
        int channels,
        int sampleRate,
        uint sampleFrames,
        IReadOnlyList<uint> channelOffsets,
        int maximumWaveBytes = MaximumWaveBytes)
    {
        if (maximumWaveBytes < 44)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumWaveBytes));
        }

        var info = Validate(data, channels, sampleRate, sampleFrames, channelOffsets);
        var waveBytes = checked(info.PcmBytes + 44L);
        if (waveBytes > maximumWaveBytes)
        {
            throw new InvalidDataException(
                $"Xbox IMA WAV requires {waveBytes} bytes; the configured limit is {maximumWaveBytes}.");
        }

        var decodedChannels = new short[channels][];
        for (var channel = 0; channel < channels; channel++)
        {
            decodedChannels[channel] = DecodeChannel(
                data,
                checked((int)channelOffsets[channel]),
                info.Blocks,
                checked((int)sampleFrames));
        }

        using var output = new MemoryStream(checked((int)waveBytes));
        WriteWaveHeader(output, channels, sampleRate, checked((uint)info.PcmBytes));
        var sample = new byte[2];
        for (var frame = 0; frame < sampleFrames; frame++)
        {
            for (var channel = 0; channel < channels; channel++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(sample, decodedChannels[channel][frame]);
                output.Write(sample);
            }
        }

        return output.ToArray();
    }

    private static void ValidateArguments(
        ReadOnlySpan<byte> data,
        int channels,
        int sampleRate,
        uint sampleFrames,
        IReadOnlyList<uint> channelOffsets)
    {
        ArgumentNullException.ThrowIfNull(channelOffsets);
        if (channels is < 1 or > MaximumChannels || channelOffsets.Count != channels)
        {
            throw new InvalidDataException($"Xbox IMA requires 1-{MaximumChannels} channels and one offset per channel.");
        }
        if (sampleRate is < 8000 or > MaximumSampleRate)
        {
            throw new InvalidDataException($"Invalid Xbox IMA sample rate {sampleRate}.");
        }
        if (sampleFrames is 0 or > MaximumSampleFrames)
        {
            throw new InvalidDataException($"Invalid Xbox IMA sample count {sampleFrames}.");
        }
        if (data.Length == 0)
        {
            throw new InvalidDataException("Xbox IMA asset is empty.");
        }
    }

    private static void ValidateBlockHeaders(ReadOnlySpan<byte> data, int offset, int blocks)
    {
        for (var block = 0; block < blocks; block++)
        {
            var header = data.Slice(checked(offset + block * BlockBytes), 4);
            var stepIndex = header[2];
            if (stepIndex >= StepTable.Length)
            {
                throw new InvalidDataException(
                    $"Xbox IMA block {block} has an invalid step index {stepIndex}.");
            }
        }
    }

    private static short[] DecodeChannel(
        ReadOnlySpan<byte> data,
        int offset,
        int blocks,
        int sampleFrames)
    {
        var samples = new short[sampleFrames];
        var output = 0;
        for (var block = 0; block < blocks && output < sampleFrames; block++)
        {
            var blockOffset = checked(offset + block * BlockBytes);
            var predictor = BinaryPrimitives.ReadInt16LittleEndian(data.Slice(blockOffset, 2));
            var stepIndex = data[blockOffset + 2];
            if (stepIndex >= StepTable.Length)
            {
                throw new InvalidDataException(
                    $"Xbox IMA block {block} has an invalid step index {stepIndex}.");
            }

            samples[output++] = predictor;
            for (var nibble = 0; nibble < 64 && output < sampleFrames; nibble++)
            {
                var packed = data[blockOffset + 4 + (nibble >> 1)];
                var code = (nibble & 1) == 0 ? packed & 0x0F : packed >> 4;
                var step = StepTable[stepIndex];
                var difference = step >> 3;
                if ((code & 1) != 0) difference += step >> 2;
                if ((code & 2) != 0) difference += step >> 1;
                if ((code & 4) != 0) difference += step;
                predictor = (short)Math.Clamp(
                    predictor + (((code & 8) != 0) ? -difference : difference),
                    short.MinValue,
                    short.MaxValue);
                stepIndex = (byte)Math.Clamp(stepIndex + IndexTable[code], 0, StepTable.Length - 1);
                samples[output++] = predictor;
            }
        }

        if (output != sampleFrames)
        {
            throw new InvalidDataException("Xbox IMA block sequence ended before the declared sample count.");
        }

        return samples;
    }

    private static void WriteWaveHeader(Stream output, int channels, int sampleRate, uint dataBytes)
    {
        Span<byte> header = stackalloc byte[44];
        header.Clear();
        "RIFF"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..], checked(dataBytes + 36));
        "WAVE"u8.CopyTo(header[8..]);
        "fmt "u8.CopyTo(header[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(header[20..], 1);
        BinaryPrimitives.WriteUInt16LittleEndian(header[22..], checked((ushort)channels));
        BinaryPrimitives.WriteUInt32LittleEndian(header[24..], checked((uint)sampleRate));
        BinaryPrimitives.WriteUInt32LittleEndian(header[28..], checked((uint)(sampleRate * channels * sizeof(short))));
        BinaryPrimitives.WriteUInt16LittleEndian(header[32..], checked((ushort)(channels * sizeof(short))));
        BinaryPrimitives.WriteUInt16LittleEndian(header[34..], 16);
        "data"u8.CopyTo(header[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(header[40..], dataBytes);
        output.Write(header);
    }
}
