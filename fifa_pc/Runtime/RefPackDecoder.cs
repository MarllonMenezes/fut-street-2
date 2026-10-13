using System.Buffers.Binary;

namespace FifaStreetPc;

public sealed record RefPackDecodedAsset(
    byte[] Data,
    int DeclaredSize,
    int CompressedBytes,
    int Commands);

public static class RefPackDecoder
{
    private const byte Signature = 0xFB;
    private const int MaxOutputSize = 256 * 1024 * 1024;

    public static RefPackDecodedAsset Decode(Stream compressedStream, int expectedSize)
    {
        ArgumentNullException.ThrowIfNull(compressedStream);
        if (!compressedStream.CanRead)
        {
            throw new NotSupportedException("RefPack input must be readable.");
        }
        if (expectedSize < 0 || expectedSize > MaxOutputSize)
        {
            throw new InvalidDataException($"RefPack output size is invalid: {expectedSize}.");
        }

        var input = ReadAll(compressedStream);
        if (input.Length < 5 || input[1] != Signature || input[0] is not (0x10 or 0x90))
        {
            throw new InvalidDataException("Unsupported RefPack header.");
        }

        var declaredSize = ReadDeclaredSize(input);
        if (declaredSize != expectedSize)
        {
            throw new InvalidDataException(
                $"RefPack size mismatch: header={declaredSize}, VivF={expectedSize}.");
        }

        var output = new byte[declaredSize];
        var source = HeaderSize(input[0]);
        var destination = 0;
        var commands = 0;
        var stopped = false;
        while (source < input.Length)
        {
            var control = input[source++];
            commands++;
            if (control <= 0x7F)
            {
                RequireInput(input, source, 1);
                var b1 = input[source++];
                var literals = control & 0x03;
                var copyLength = ((control & 0x1C) >> 2) + 3;
                var copyOffset = ((control & 0x60) << 3) + b1 + 1;
                CopyLiterals(input, ref source, output, ref destination, literals);
                CopyHistory(output, ref destination, copyOffset, copyLength);
            }
            else if (control <= 0xBF)
            {
                RequireInput(input, source, 2);
                var b1 = input[source++];
                var b2 = input[source++];
                var literals = (b1 & 0xC0) >> 6;
                var copyLength = (control & 0x3F) + 4;
                var copyOffset = ((b1 & 0x3F) << 8) + b2 + 1;
                CopyLiterals(input, ref source, output, ref destination, literals);
                CopyHistory(output, ref destination, copyOffset, copyLength);
            }
            else if (control <= 0xDF)
            {
                RequireInput(input, source, 3);
                var b1 = input[source++];
                var b2 = input[source++];
                var b3 = input[source++];
                var literals = control & 0x03;
                var copyLength = ((control & 0x0C) << 6) + b3 + 5;
                var copyOffset = ((control & 0x10) << 12) + (b1 << 8) + b2 + 1;
                CopyLiterals(input, ref source, output, ref destination, literals);
                CopyHistory(output, ref destination, copyOffset, copyLength);
            }
            else if (control <= 0xFB)
            {
                var literals = ((control & 0x1F) << 2) + 4;
                CopyLiterals(input, ref source, output, ref destination, literals);
            }
            else
            {
                var literals = control & 0x03;
                CopyLiterals(input, ref source, output, ref destination, literals);
                stopped = true;
                break;
            }
        }

        if (!stopped)
        {
            throw new InvalidDataException("RefPack stream has no stop command.");
        }
        if (destination != declaredSize)
        {
            throw new InvalidDataException(
                $"RefPack output mismatch: produced={destination}, expected={declaredSize}.");
        }

        return new RefPackDecodedAsset(output, declaredSize, input.Length, commands);
    }

    private static int HeaderSize(byte flags) => (flags & 0x80) == 0 ? 5 : 6;

    public static int ReadDeclaredSize(ReadOnlySpan<byte> input)
    {
        if (input.Length < 5 || input[1] != Signature || input[0] is not (0x10 or 0x90))
        {
            throw new InvalidDataException("Unsupported RefPack header.");
        }
        if ((input[0] & 0x80) != 0 && input.Length < 6)
        {
            throw new InvalidDataException("RefPack size header is truncated.");
        }

        int declaredSize;
        if ((input[0] & 0x80) == 0)
        {
            declaredSize = (input[2] << 16) | (input[3] << 8) | input[4];
        }
        else
        {
            var extendedSize = BinaryPrimitives.ReadUInt32BigEndian(input[2..6]);
            if (extendedSize > MaxOutputSize)
            {
                throw new InvalidDataException($"RefPack output size is invalid: {extendedSize}.");
            }
            declaredSize = checked((int)extendedSize);
        }
        if (declaredSize > MaxOutputSize)
        {
            throw new InvalidDataException($"RefPack output size is invalid: {declaredSize}.");
        }
        return declaredSize;
    }

    private static byte[] ReadAll(Stream stream)
    {
        if (stream.Length > int.MaxValue)
        {
            throw new InvalidDataException("RefPack input is too large.");
        }

        var input = new byte[checked((int)stream.Length)];
        var position = 0;
        while (position < input.Length)
        {
            var read = stream.Read(input, position, input.Length - position);
            if (read == 0)
            {
                throw new EndOfStreamException("Unexpected end of RefPack stream.");
            }
            position += read;
        }
        return input;
    }

    private static void RequireInput(byte[] input, int position, int count)
    {
        if (position < 0 || count < 0 || position > input.Length - count)
        {
            throw new InvalidDataException("RefPack command exceeds the compressed block.");
        }
    }

    private static void CopyLiterals(
        byte[] input,
        ref int source,
        byte[] output,
        ref int destination,
        int count)
    {
        RequireInput(input, source, count);
        RequireOutput(output, destination, count);
        Buffer.BlockCopy(input, source, output, destination, count);
        source += count;
        destination += count;
    }

    private static void CopyHistory(
        byte[] output,
        ref int destination,
        int offset,
        int count)
    {
        if (offset <= 0 || offset > destination)
        {
            throw new InvalidDataException("RefPack back-reference is outside the output history.");
        }
        RequireOutput(output, destination, count);
        for (var index = 0; index < count; index++)
        {
            output[destination] = output[destination - offset];
            destination++;
        }
    }

    private static void RequireOutput(byte[] output, int position, int count)
    {
        if (position < 0 || count < 0 || position > output.Length - count)
        {
            throw new InvalidDataException("RefPack command exceeds the declared output size.");
        }
    }
}
