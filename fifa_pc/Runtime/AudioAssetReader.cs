using System.Buffers.Binary;

namespace FifaStreetPc;

public sealed record AudioBankInspection(
    uint HeaderWord4,
    uint HeaderWord8,
    uint DeclaredSize,
    uint HeaderWord24,
    uint HeaderWord28,
    bool DeclaredSizeMatches,
    bool HeaderFieldsWithinAsset);

public sealed record AudioStreamInspection(
    uint FirstChunkSize,
    bool FirstChunkWithinAsset,
    bool HasSchlSignature);

public sealed record AudioSchlEnvelopeEvidence(
    uint HeaderSize,
    uint Platform,
    bool HasSchlSignature,
    bool HeaderWithinRange,
    bool HasPtHeader);

public sealed record AudioSchlMetadataWalk(
    bool HeaderFullyAvailable,
    bool ReachedTerminator,
    bool ReachedHeaderBoundaryWithoutTerminator,
    bool BoundaryViolation,
    int BoundaryViolationOffset,
    int TokenCount,
    int SingleByteControlCount,
    int LengthPrefixedTokenCount);

public static partial class AudioAssetReader
{
    public static AudioBankInspection InspectBank(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32 || !data[..4].SequenceEqual("ABKC"u8))
        {
            throw new InvalidDataException("Unsupported ABKC audio bank.");
        }

        var fields = new uint[]
        {
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[20..24]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[24..28]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[28..32])
        };
        var withinAsset = true;
        foreach (var field in fields[2..])
        {
            withinAsset &= field <= data.Length;
        }

        return new AudioBankInspection(
            fields[0],
            fields[1],
            fields[2],
            fields[3],
            fields[4],
            fields[2] == data.Length,
            withinAsset);
    }

    public static AudioStreamInspection InspectStream(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8 || !data[..4].SequenceEqual("SCHl"u8))
        {
            throw new InvalidDataException("Unsupported SCHl audio stream.");
        }

        var firstChunkSize = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        return new AudioStreamInspection(
            firstChunkSize,
            firstChunkSize >= 8 && firstChunkSize <= data.Length,
            true);
    }

    public static AudioSchlEnvelopeEvidence InspectSchlEnvelope(
        ReadOnlySpan<byte> data,
        long rangeLength)
    {
        if (rangeLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rangeLength));
        }
        if (data.Length < 8 || !data[..4].SequenceEqual("SCHl"u8))
        {
            return new AudioSchlEnvelopeEvidence(0, 0, false, false, false);
        }

        var headerSize = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        var headerWithinRange = headerSize >= 12 && headerSize <= rangeLength;
        var hasPtHeader = data.Length >= 12 && data[8..10].SequenceEqual("PT"u8);
        var platform = hasPtHeader
            ? BinaryPrimitives.ReadUInt16LittleEndian(data[10..12])
            : 0u;
        return new AudioSchlEnvelopeEvidence(
            headerSize,
            platform,
            true,
            headerWithinRange,
            hasPtHeader);
    }

    public static AudioSchlMetadataWalk InspectSchlMetadata(
        ReadOnlySpan<byte> data,
        uint headerSize)
    {
        var headerFullyAvailable = headerSize <= data.Length;
        var boundary = (int)Math.Min(headerSize, (uint)data.Length);
        var cursor = Math.Min(12, boundary);
        var tokenCount = 0;
        var singleByteControlCount = 0;
        var lengthPrefixedTokenCount = 0;
        var boundaryViolation = false;
        var boundaryViolationOffset = -1;
        var reachedTerminator = false;

        while (cursor < boundary)
        {
            var tokenOffset = cursor;
            var tag = data[cursor++];
            tokenCount++;
            if (tag is 0xFF or 0xFE)
            {
                reachedTerminator = true;
                break;
            }
            if (tag is 0xFC or 0xFD)
            {
                singleByteControlCount++;
                continue;
            }
            if (cursor >= boundary)
            {
                boundaryViolation = true;
                boundaryViolationOffset = tokenOffset;
                break;
            }

            var length = data[cursor++];
            lengthPrefixedTokenCount++;
            if (length == 0xFF)
            {
                if (cursor > boundary - 4)
                {
                    boundaryViolation = true;
                    boundaryViolationOffset = tokenOffset;
                    break;
                }

                var extendedLength = BinaryPrimitives.ReadUInt32BigEndian(data[cursor..]);
                cursor += 4;
                if (extendedLength > (uint)(boundary - cursor))
                {
                    boundaryViolation = true;
                    boundaryViolationOffset = tokenOffset;
                    break;
                }

                cursor += checked((int)extendedLength);
                continue;
            }

            if (length > boundary - cursor)
            {
                boundaryViolation = true;
                boundaryViolationOffset = tokenOffset;
                break;
            }

            cursor += length;
        }

        return new AudioSchlMetadataWalk(
            headerFullyAvailable,
            reachedTerminator,
            !reachedTerminator && !boundaryViolation && cursor == boundary,
            boundaryViolation,
            boundaryViolationOffset,
            tokenCount,
            singleByteControlCount,
            lengthPrefixedTokenCount);
    }
}
