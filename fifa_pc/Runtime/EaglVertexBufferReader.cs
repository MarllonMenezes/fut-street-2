using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglVertexBufferStream(
    string SymbolName,
    int SectionIndex,
    uint BufferRecordOffset,
    uint DescriptorOffset,
    uint DataOffset,
    uint ByteLength,
    uint Stride,
    uint Flags)
{
    public uint ElementCount => ByteLength / Stride;
}

public sealed record EaglFloat2Range(float MinimumX, float MinimumY, float MaximumX, float MaximumY)
{
    public bool IsNearUnitRange(float tolerance = 0.01f) =>
        MinimumX >= -tolerance && MinimumY >= -tolerance &&
        MaximumX <= 1f + tolerance && MaximumY <= 1f + tolerance;
}

public readonly record struct EaglFloat2(float X, float Y);

public sealed record EaglFloatComponentRange(float Minimum, float Maximum);

public sealed record EaglPackedFloatProfile(
    int ComponentCount,
    uint Stride,
    uint ElementCount,
    IReadOnlyList<EaglFloatComponentRange> Components);

/// <summary>
/// Reads EAGL vertex-buffer records found in FIFA Street 2's split ELF
/// objects. The referenced owner header is checked against its flags/data
/// pointer before exposing the buffer bytes.
/// </summary>
public static class EaglVertexBufferReader
{
    private const uint MaximumSupportedStride = 256;
    private const uint MaximumSupportedStreamBytes = 64 * 1024 * 1024;
    private const int BufferRecordSize = 20;
    private const int BufferDescriptorSize = 8;

    public static IReadOnlyList<EaglVertexBufferStream> Inspect(
        ReadOnlySpan<byte> data,
        ElfInspection elf)
    {
        var streams = new List<EaglVertexBufferStream>();
        foreach (var symbol in elf.Symbols)
        {
            if (!symbol.Name.StartsWith("__VB", StringComparison.Ordinal) ||
                !TryRead(data, elf, symbol, out var stream))
            {
                continue;
            }

            streams.Add(stream!);
        }

        return streams;
    }

    /// <summary>
    /// Returns a descriptor-only profile. Strides, byte totals, element totals,
    /// flags, and section indices are preserved as raw validated metadata; no
    /// stream is assigned a position, normal, UV, weight, or other attribute.
    /// </summary>
    public static string GetStructuralSignature(
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        ArgumentNullException.ThrowIfNull(streams);
        var strideProfiles = streams
            .GroupBy(stream => stream.Stride)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}");
        var flagProfiles = streams
            .GroupBy(stream => stream.Flags)
            .OrderBy(group => group.Key)
            .Select(group => $"0x{group.Key:X}={group.Count()}");
        var sectionProfiles = streams
            .GroupBy(stream => stream.SectionIndex)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}");
        var totalBytes = streams.Aggregate<EaglVertexBufferStream, ulong>(
            0,
            (total, stream) => total + stream.ByteLength);
        var totalElements = streams.Aggregate<EaglVertexBufferStream, ulong>(
            0,
            (total, stream) => total + stream.ElementCount);
        return $"streams={streams.Count};bytes=0x{totalBytes:X};elements={totalElements};strides={string.Join(",", strideProfiles)};flags={string.Join(",", flagProfiles)};sections={string.Join(",", sectionProfiles)}";
    }

    /// <summary>
    /// Returns a relative-order signature for the validated buffer records.
    /// Only raw record metadata participates: section, relative record offset,
    /// descriptor/data offsets, byte length, stride, and flags. The record
    /// sequence is deliberately not assigned a vertex attribute or skinning
    /// meaning.
    /// </summary>
    public static string GetRelativeRecordStructuralSignature(
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        ArgumentNullException.ThrowIfNull(streams);
        var ordered = streams
            .OrderBy(stream => stream.BufferRecordOffset)
            .ThenBy(stream => stream.DescriptorOffset)
            .ToArray();
        var firstRecordOffset = ordered.Length == 0
            ? 0u
            : ordered[0].BufferRecordOffset;
        var canonical = string.Join("|", ordered.Select(stream =>
            $"section={stream.SectionIndex};record=0x{stream.BufferRecordOffset - firstRecordOffset:X};descriptor=0x{stream.DescriptorOffset:X};data=0x{stream.DataOffset:X};bytes=0x{stream.ByteLength:X};stride=0x{stream.Stride:X};flags=0x{stream.Flags:X}"));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        return $"streams={streams.Count};sha256={hash}";
    }

    /// <summary>
    /// Hashes one already validated raw __VB payload range. The digest is a
    /// byte-identity check only; it does not assign an attribute or rendering
    /// semantic to the stream.
    /// </summary>
    public static string GetPayloadSha256(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(stream);
        if (stream.SectionIndex <= 0 || stream.SectionIndex >= elf.Sections.Count)
        {
            throw new InvalidDataException("Vertex stream section index is outside the ELF section table.");
        }

        var section = elf.Sections[stream.SectionIndex];
        var fileOffset = (ulong)section.Offset + stream.DataOffset;
        if (section.Type == 8 ||
            stream.DataOffset > section.Size ||
            stream.ByteLength == 0 ||
            stream.ByteLength > section.Size - stream.DataOffset ||
            fileOffset + stream.ByteLength > (ulong)data.Length)
        {
            throw new InvalidDataException("Vertex stream payload range is invalid.");
        }

        return Convert.ToHexString(SHA256.HashData(
            data.Slice(checked((int)fileOffset), checked((int)stream.ByteLength))));
    }

    /// <summary>
    /// Returns a numeric-only profile for already validated streams. It counts
    /// finite packed-float layouts and the explicitly checked containment of
    /// 12-byte streams in declared bounds. No component count is promoted to
    /// an attribute or deformation semantic.
    /// </summary>
    public static string GetNumericStructuralSignature(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        IReadOnlyList<EaglVertexBufferStream> streams,
        EaglAxisAlignedBounds? declaredBounds)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(streams);
        var finiteProfiles = new List<EaglPackedFloatProfile>();
        var finiteStride12 = 0;
        var finiteStride8 = 0;
        var boundsMatchingStride12 = 0;
        foreach (var stream in streams)
        {
            if (TryMeasurePackedFloatProfile(data, elf, stream, out var profile) && profile is not null)
            {
                finiteProfiles.Add(profile);
                if (profile.Stride == 12)
                {
                    finiteStride12++;
                }
                else if (profile.Stride == 8)
                {
                    finiteStride8++;
                }
            }

            if (stream.Stride == 12 &&
                declaredBounds is not null &&
                TryMeasureFloat3(data, elf, stream, out var measuredBounds) &&
                measuredBounds is not null &&
                FitsBounds(measuredBounds, declaredBounds))
            {
                boundsMatchingStride12++;
            }
        }

        var componentProfiles = finiteProfiles
            .GroupBy(profile => (profile.ComponentCount, profile.Stride))
            .OrderBy(group => group.Key.ComponentCount)
            .ThenBy(group => group.Key.Stride)
            .Select(group => $"{group.Key.ComponentCount}x{group.Key.Stride}={group.Count()}");
        return $"streams={streams.Count};finite_packed={finiteProfiles.Count};finite_stride8={finiteStride8};finite_stride12={finiteStride12};components={string.Join(",", componentProfiles)};bounds_matching_stride12={boundsMatchingStride12}";
    }

    public static bool TryRead(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        ElfSymbol symbol,
        out EaglVertexBufferStream? stream)
    {
        stream = null;
        if (!symbol.Name.StartsWith("__VB", StringComparison.Ordinal) ||
            symbol.SectionIndex == 0 || symbol.SectionIndex >= elf.Sections.Count)
        {
            return false;
        }

        var section = elf.Sections[symbol.SectionIndex];
        if (section.Type == 8 || symbol.Value > section.Size ||
            BufferRecordSize > section.Size - symbol.Value)
        {
            return false;
        }

        var recordFileOffset = (ulong)section.Offset + symbol.Value;
        if (recordFileOffset + BufferRecordSize > (ulong)data.Length)
        {
            return false;
        }

        var record = data.Slice(checked((int)recordFileOffset), BufferRecordSize);
        var descriptorOffset = BinaryPrimitives.ReadUInt32LittleEndian(record[..4]);
        var byteLength = BinaryPrimitives.ReadUInt32LittleEndian(record[4..8]);
        var stride = BinaryPrimitives.ReadUInt32LittleEndian(record[8..12]);
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(record[12..16]);
        var dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(record[16..20]);

        if (descriptorOffset > section.Size || BufferDescriptorSize > section.Size - descriptorOffset ||
            byteLength == 0 || byteLength > MaximumSupportedStreamBytes ||
            stride == 0 || stride > MaximumSupportedStride || byteLength % stride != 0 ||
            dataOffset > section.Size || byteLength > section.Size - dataOffset)
        {
            return false;
        }

        var descriptorFileOffset = (ulong)section.Offset + descriptorOffset;
        var dataFileOffset = (ulong)section.Offset + dataOffset;
        if (descriptorFileOffset + BufferDescriptorSize > (ulong)data.Length ||
            dataFileOffset + byteLength > (ulong)data.Length)
        {
            return false;
        }

        var descriptor = data.Slice(checked((int)descriptorFileOffset), BufferDescriptorSize);
        if (BinaryPrimitives.ReadUInt32LittleEndian(descriptor[..4]) != flags ||
            BinaryPrimitives.ReadUInt32LittleEndian(descriptor[4..8]) != dataOffset)
        {
            return false;
        }

        stream = new EaglVertexBufferStream(
            symbol.Name,
            symbol.SectionIndex,
            symbol.Value,
            descriptorOffset,
            dataOffset,
            byteLength,
            stride,
            flags);
        return true;
    }

    public static bool TryMeasureFloat3(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream,
        out EaglAxisAlignedBounds? bounds)
    {
        bounds = null;
        if (stream.Stride != 3 * sizeof(float) ||
            stream.SectionIndex <= 0 || stream.SectionIndex >= elf.Sections.Count)
        {
            return false;
        }

        var section = elf.Sections[stream.SectionIndex];
        var fileOffset = (ulong)section.Offset + stream.DataOffset;
        if (section.Type == 8 || stream.DataOffset > section.Size || stream.ByteLength == 0 ||
            stream.ByteLength > section.Size - stream.DataOffset ||
            fileOffset + stream.ByteLength > (ulong)data.Length)
        {
            return false;
        }

        var bytes = data.Slice(checked((int)fileOffset), checked((int)stream.ByteLength));
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var minZ = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;
        var maxZ = float.NegativeInfinity;
        for (var offset = 0; offset < bytes.Length; offset += 3 * sizeof(float))
        {
            var x = ReadFloat(bytes.Slice(offset, sizeof(float)));
            var y = ReadFloat(bytes.Slice(offset + sizeof(float), sizeof(float)));
            var z = ReadFloat(bytes.Slice(offset + 2 * sizeof(float), sizeof(float)));
            if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
            {
                return false;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            minZ = Math.Min(minZ, z);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
            maxZ = Math.Max(maxZ, z);
        }

        bounds = new EaglAxisAlignedBounds(
            new EaglVector3(minX, minY, minZ),
            new EaglVector3(maxX, maxY, maxZ));
        return true;
    }

    /// <summary>
    /// Measures finite tightly packed float2 records. This is intentionally a
    /// numeric profile only; the reader does not assign a semantic such as UVs.
    /// </summary>
    public static bool TryMeasureFloat2(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream,
        out EaglFloat2Range? range)
    {
        range = null;
        if (stream.Stride != 2 * sizeof(float) ||
            stream.SectionIndex <= 0 || stream.SectionIndex >= elf.Sections.Count)
        {
            return false;
        }

        var section = elf.Sections[stream.SectionIndex];
        var fileOffset = (ulong)section.Offset + stream.DataOffset;
        if (section.Type == 8 || stream.DataOffset > section.Size || stream.ByteLength == 0 ||
            stream.ByteLength > section.Size - stream.DataOffset ||
            fileOffset + stream.ByteLength > (ulong)data.Length)
        {
            return false;
        }

        var bytes = data.Slice(checked((int)fileOffset), checked((int)stream.ByteLength));
        var minX = float.PositiveInfinity;
        var minY = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;
        for (var offset = 0; offset < bytes.Length; offset += 2 * sizeof(float))
        {
            var x = ReadFloat(bytes.Slice(offset, sizeof(float)));
            var y = ReadFloat(bytes.Slice(offset + sizeof(float), sizeof(float)));
            if (!float.IsFinite(x) || !float.IsFinite(y))
            {
                return false;
            }

            minX = Math.Min(minX, x);
            minY = Math.Min(minY, y);
            maxX = Math.Max(maxX, x);
            maxY = Math.Max(maxY, y);
        }

        range = new EaglFloat2Range(minX, minY, maxX, maxY);
        return true;
    }

    /// <summary>
    /// Measures a tightly packed stream whose complete stride consists of
    /// finite little-endian float words. This is a numeric profile only; no
    /// vertex attribute semantic is assigned to any component.
    /// </summary>
    public static bool TryMeasurePackedFloatProfile(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream,
        out EaglPackedFloatProfile? profile)
    {
        profile = null;
        if (stream.Stride == 0 || stream.Stride % sizeof(float) != 0 ||
            stream.Stride > 64 || stream.SectionIndex <= 0 ||
            stream.SectionIndex >= elf.Sections.Count)
        {
            return false;
        }

        var section = elf.Sections[stream.SectionIndex];
        var fileOffset = (ulong)section.Offset + stream.DataOffset;
        if (section.Type == 8 || stream.DataOffset > section.Size || stream.ByteLength == 0 ||
            stream.ByteLength > section.Size - stream.DataOffset ||
            fileOffset + stream.ByteLength > (ulong)data.Length)
        {
            return false;
        }

        var componentCount = checked((int)(stream.Stride / sizeof(float)));
        var minimums = Enumerable.Repeat(float.PositiveInfinity, componentCount).ToArray();
        var maximums = Enumerable.Repeat(float.NegativeInfinity, componentCount).ToArray();
        var bytes = data.Slice(checked((int)fileOffset), checked((int)stream.ByteLength));
        for (var vertexOffset = 0; vertexOffset < bytes.Length; vertexOffset += checked((int)stream.Stride))
        {
            for (var component = 0; component < componentCount; component++)
            {
                var value = ReadFloat(bytes.Slice(
                    vertexOffset + component * sizeof(float),
                    sizeof(float)));
                if (!float.IsFinite(value))
                {
                    return false;
                }

                minimums[component] = Math.Min(minimums[component], value);
                maximums[component] = Math.Max(maximums[component], value);
            }
        }

        var ranges = new EaglFloatComponentRange[componentCount];
        for (var component = 0; component < componentCount; component++)
        {
            ranges[component] = new EaglFloatComponentRange(
                minimums[component],
                maximums[component]);
        }

        profile = new EaglPackedFloatProfile(
            componentCount,
            stream.Stride,
            stream.ElementCount,
            ranges);
        return true;
    }

    public static bool TryReadFloat2Values(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream,
        out IReadOnlyList<EaglFloat2> values)
    {
        values = Array.Empty<EaglFloat2>();
        if (!TryMeasureFloat2(data, elf, stream, out var range) || range is null)
        {
            return false;
        }

        var section = elf.Sections[stream.SectionIndex];
        var fileOffset = (ulong)section.Offset + stream.DataOffset;
        var bytes = data.Slice(checked((int)fileOffset), checked((int)stream.ByteLength));
        var decoded = new EaglFloat2[checked((int)stream.ElementCount)];
        for (var index = 0; index < decoded.Length; index++)
        {
            var offset = checked(index * 2 * sizeof(float));
            decoded[index] = new EaglFloat2(
                ReadFloat(bytes.Slice(offset, sizeof(float))),
                ReadFloat(bytes.Slice(offset + sizeof(float), sizeof(float))));
        }

        values = decoded;
        return true;
    }

    public static bool FitsBounds(EaglAxisAlignedBounds candidate, EaglAxisAlignedBounds declared)
    {
        var tolerance = Math.Max(1f,
            Math.Max(declared.Maximum.X - declared.Minimum.X,
                Math.Max(declared.Maximum.Y - declared.Minimum.Y,
                    declared.Maximum.Z - declared.Minimum.Z))) * 0.01f;

        return candidate.Minimum.X >= declared.Minimum.X - tolerance &&
               candidate.Minimum.Y >= declared.Minimum.Y - tolerance &&
               candidate.Minimum.Z >= declared.Minimum.Z - tolerance &&
               candidate.Maximum.X <= declared.Maximum.X + tolerance &&
               candidate.Maximum.Y <= declared.Maximum.Y + tolerance &&
               candidate.Maximum.Z <= declared.Maximum.Z + tolerance;
    }

    private static float ReadFloat(ReadOnlySpan<byte> bytes) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
}
