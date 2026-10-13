using System.Buffers.Binary;

namespace FifaStreetPc;

public sealed record AudioAbkSoundEntry(
    int SoundIndex,
    int ModuleIndex,
    int PlayerIndex,
    int SampleTableIndex,
    int EntryIndex,
    byte SoundType,
    byte Priority,
    uint DataOffset,
    uint LoopOffset,
    bool IsDummy);

public sealed record AudioAbkSampleTable(
    int TableIndex,
    uint Offset,
    uint SoundCount,
    IReadOnlyList<AudioAbkSoundEntry> Entries);

public sealed record AudioAbkModule(
    int ModuleIndex,
    uint Offset,
    byte PlayerCount,
    byte ControllerCount,
    uint ModuleDataOffset,
    IReadOnlyList<uint> PlayerOffsets,
    IReadOnlyList<uint> SampleTableOffsets);

public sealed record AudioAbkInspection(
    int AssetLength,
    bool LittleEndian,
    uint ModuleTableOffset,
    uint BnkOffset,
    int ModuleCount,
    IReadOnlyList<AudioAbkModule> Modules,
    IReadOnlyList<AudioAbkSampleTable> SampleTables,
    IReadOnlyList<AudioAbkSoundEntry> Sounds)
{
    public int NonDummySoundCount => Sounds.Count(sound => !sound.IsDummy);
    public int DummySoundCount => Sounds.Count(sound => sound.IsDummy);
    public int RamSoundCount => Sounds.Count(sound => !sound.IsDummy && sound.SoundType == 0);
    public int StreamSoundCount => Sounds.Count(sound => !sound.IsDummy && sound.SoundType is 1 or 2);
    public int LoopedStreamSoundCount => Sounds.Count(sound => !sound.IsDummy && sound.SoundType == 2);
}

public sealed record AudioAbkAstReferenceEvidence(
    int StreamSoundCount,
    int DataOffsetWithinAstCount,
    int DataOffsetSchlCount,
    int LoopOffsetWithinAstCount,
    int LoopOffsetSchlCount,
    IReadOnlyList<string> Issues)
{
    public bool AllDataOffsetsStartSchl =>
        StreamSoundCount == DataOffsetSchlCount;

    public bool AllLoopOffsetsStartSchl =>
        LoopOffsetSchlCount == LoopOffsetWithinAstCount;
}

public sealed record AudioAbkAstRange(
    int SoundIndex,
    bool LoopSegment,
    uint Offset,
    long Length);

public sealed record AudioAbkBnkEntry(
    int EntryIndex,
    uint RelativeOffset,
    uint HeaderOffset,
    bool IsDummy,
    bool HeaderWithinAsset);

public sealed record AudioAbkBnkTagEvidence(
    byte Tag,
    byte[] Payload,
    uint? BigEndianValue);

public sealed record AudioAbkBnkHeaderEvidence(
    int EntryIndex,
    uint HeaderOffset,
    string Prefix,
    ushort? PlatformWord,
    IReadOnlyList<AudioAbkBnkTagEvidence> Tags,
    uint EndOffset,
    bool HeaderTerminated,
    string? Issue)
{
    public bool Parsed => Issue is null && HeaderTerminated;

    public AudioAbkBnkTagEvidence? FindTag(byte tag) =>
        Tags.FirstOrDefault(candidate => candidate.Tag == tag);
}

public sealed record AudioAbkBnkStreamDescriptor(
    int EntryIndex,
    uint HeaderOffset,
    uint Codec,
    int Channels,
    int SampleRate,
    uint SampleFrames,
    IReadOnlyList<uint> RelativeChannelOffsets);

public sealed record AudioAbkBnkStreamValidation(
    int EntryIndex,
    AudioAbkBnkStreamDescriptor Descriptor,
    bool Valid,
    XboxImaAudioInfo? Info,
    string? Issue);

public sealed record AudioAbkBnkInspection(
    uint Offset,
    bool LittleEndian,
    byte Version,
    ushort SoundCount,
    uint TableOffset,
    uint DeclaredSize,
    IReadOnlyList<AudioAbkBnkEntry> Entries)
{
    public int RealSoundCount => Entries.Count(entry => !entry.IsDummy);
    public int ValidHeaderCount => Entries.Count(entry => entry.HeaderWithinAsset);
}

/// <summary>
/// Conservative reader for the observed EA ABKC module/sample-table chain.
/// It validates structure and optional .ast offsets, but does not assign event
/// names, playback semantics, or meanings to unknown header fields.
/// </summary>
public static class AudioAbkReader
{
    private const int ModuleFixedSize = 0x3C;
    private const int PlayerPointerSize = 4;
    private const int SampleEntrySize = 0x0C;
    private const int MaximumModules = 0x400;
    private const int MaximumTables = 0x400;
    private const int MaximumSoundsPerTable = 0x100000;

    public static AudioAbkInspection Inspect(ReadOnlySpan<byte> data)
    {
        if (data.Length < 0x24 || !data[..4].SequenceEqual("ABKC"u8))
        {
            throw new InvalidDataException("Unsupported ABKC audio bank.");
        }

        var littleEndian = DetectLittleEndian(data);
        var moduleTableOffset = ReadUInt32(data, 0x1C, littleEndian);
        var bnkOffset = ReadUInt32(data, 0x20, littleEndian);
        var moduleCount = ReadUInt16(data, 0x0A, littleEndian);
        if (moduleCount > MaximumModules)
        {
            throw new InvalidDataException($"ABKC module count {moduleCount} exceeds the safety limit.");
        }

        var modules = new List<AudioAbkModule>(moduleCount);
        var tablesByOffset = new Dictionary<uint, AudioAbkSampleTable>();
        var sounds = new List<AudioAbkSoundEntry>();
        var moduleOffset = moduleTableOffset;
        for (var moduleIndex = 0; moduleIndex < moduleCount; moduleIndex++)
        {
            EnsureRange(data, moduleOffset, ModuleFixedSize, "ABKC module header");
            var playerCount = data[checked((int)moduleOffset + 0x24)];
            var controllerCount = data[checked((int)moduleOffset + 0x27)];
            var moduleDataOffset = ReadUInt32(data, checked((int)moduleOffset + 0x2C), littleEndian);
            var pointerCount = checked(playerCount + controllerCount);
            var pointerBytes = checked(pointerCount * PlayerPointerSize);
            EnsureRange(data, moduleOffset, checked(ModuleFixedSize + pointerBytes), "ABKC module pointers");

            var playerOffsets = new List<uint>(playerCount);
            var sampleTableOffsets = new List<uint>(playerCount);
            for (var playerIndex = 0; playerIndex < playerCount; playerIndex++)
            {
                var playerOffset = ReadUInt32(
                    data,
                    checked((int)moduleOffset + ModuleFixedSize + playerIndex * PlayerPointerSize),
                    littleEndian);
                playerOffsets.Add(playerOffset);

                var playerDataOffset = checked(moduleDataOffset + playerOffset);
                EnsureRange(data, playerDataOffset, 8, "ABKC player data");
                var tableOffset = ReadUInt32(data, checked((int)playerDataOffset + 4), littleEndian);
                sampleTableOffsets.Add(tableOffset);
                if (!tablesByOffset.ContainsKey(tableOffset))
                {
                    if (tablesByOffset.Count >= MaximumTables)
                    {
                        throw new InvalidDataException("ABKC sample-table count exceeds the safety limit.");
                    }
                    tablesByOffset.Add(tableOffset, ParseSampleTable(
                        data,
                        tableOffset,
                        moduleIndex,
                        playerIndex,
                        tablesByOffset.Count,
                        littleEndian,
                        sounds));
                }
            }

            modules.Add(new AudioAbkModule(
                moduleIndex,
                moduleOffset,
                playerCount,
                controllerCount,
                moduleDataOffset,
                playerOffsets,
                sampleTableOffsets));
            moduleOffset = checked(moduleOffset + (uint)(ModuleFixedSize + pointerBytes));
        }

        return new AudioAbkInspection(
            data.Length,
            littleEndian,
            moduleTableOffset,
            bnkOffset,
            moduleCount,
            modules,
            tablesByOffset.Values.OrderBy(table => table.TableIndex).ToArray(),
            sounds);
    }

    public static AudioAbkAstReferenceEvidence InspectAstReferences(
        AudioAbkInspection inspection,
        ReadOnlySpan<byte> astData)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var data = astData.ToArray();
        return InspectAstReferencesCore(
            inspection,
            offset => HasRange(data, offset, 4)
                ? (true, data.AsSpan(checked((int)offset), 4).SequenceEqual("SCHl"u8))
                : (false, false));
    }

    public static IReadOnlyList<AudioAbkAstRange> BuildAstRanges(
        AudioAbkInspection inspection,
        long astLength)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        if (astLength < 0 || astLength > uint.MaxValue + 1L)
        {
            throw new ArgumentOutOfRangeException(nameof(astLength));
        }

        var offsets = inspection.Sounds
            .Where(sound => !sound.IsDummy && sound.SoundType is 1 or 2)
            .SelectMany(sound => sound.SoundType == 2
                ? new[] { sound.DataOffset, sound.LoopOffset }
                : new[] { sound.DataOffset })
            .Distinct()
            .OrderBy(offset => offset)
            .ToArray();
        var ranges = new List<AudioAbkAstRange>();
        foreach (var sound in inspection.Sounds.Where(sound => !sound.IsDummy && sound.SoundType is 1 or 2))
        {
            AddRange(sound.SoundIndex, false, sound.DataOffset);
            if (sound.SoundType == 2)
            {
                AddRange(sound.SoundIndex, true, sound.LoopOffset);
            }
        }

        return ranges;

        void AddRange(int soundIndex, bool loopSegment, uint offset)
        {
            if (offset >= astLength)
            {
                ranges.Add(new AudioAbkAstRange(soundIndex, loopSegment, offset, 0));
                return;
            }

            var next = offsets.FirstOrDefault(candidate => candidate > offset);
            var end = next > offset ? next : (ulong)astLength;
            ranges.Add(new AudioAbkAstRange(soundIndex, loopSegment, offset, checked((long)end - offset)));
        }
    }

    public static AudioAbkBnkInspection? InspectEmbeddedBnk(
        AudioAbkInspection inspection,
        ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        if (inspection.BnkOffset == 0)
        {
            return null;
        }

        EnsureRange(data, inspection.BnkOffset, 0x14, "ABKC embedded BNK header");
        var bnkOffset = inspection.BnkOffset;
        var littleEndian = data.Slice(checked((int)bnkOffset), 4).SequenceEqual("BNKl"u8);
        var bigEndian = data.Slice(checked((int)bnkOffset), 4).SequenceEqual("BNKb"u8);
        if (!littleEndian && !bigEndian)
        {
            throw new InvalidDataException($"ABKC embedded BNK at 0x{bnkOffset:X} has an unknown signature.");
        }

        var version = data[checked((int)bnkOffset + 4)];
        var soundCount = ReadUInt16(data, checked((int)bnkOffset + 6), littleEndian);
        var tableOffset = version == 2 ? 0x0Cu : 0x14u;
        if (version is not (2 or 4 or 5))
        {
            throw new InvalidDataException($"Unsupported embedded BNK version 0x{version:X2}.");
        }

        var declaredSize = version == 2
            ? ReadUInt32(data, checked((int)bnkOffset + 8), littleEndian)
            : (uint)(data.Length - (int)bnkOffset);
        var tableBytes = checked(4u * soundCount);
        EnsureRange(data, checked(bnkOffset + tableOffset), checked((int)tableBytes), "ABKC embedded BNK table");
        var entries = new List<AudioAbkBnkEntry>(soundCount);
        for (var index = 0; index < soundCount; index++)
        {
            var entryOffset = checked(bnkOffset + tableOffset + (uint)index * 4u);
            var relativeOffset = ReadUInt32(data, checked((int)entryOffset), littleEndian);
            var headerOffset = relativeOffset == 0 ? 0 : checked(entryOffset + relativeOffset);
            var headerWithinAsset = relativeOffset != 0 && HasRange(data, headerOffset, 8);
            entries.Add(new AudioAbkBnkEntry(
                index,
                relativeOffset,
                headerOffset,
                relativeOffset == 0,
                headerWithinAsset));
        }

        return new AudioAbkBnkInspection(
            bnkOffset,
            littleEndian,
            version,
            soundCount,
            tableOffset,
            declaredSize,
            entries);
    }

    /// <summary>
    /// Parses only the bounded variable-header envelope of each embedded BNK
    /// entry. Tag IDs and payload bytes remain evidence; no codec or playback
    /// semantics are assigned by this method.
    /// </summary>
    public static IReadOnlyList<AudioAbkBnkHeaderEvidence> InspectEmbeddedBnkHeaders(
        AudioAbkBnkInspection inspection,
        ReadOnlySpan<byte> data,
        int maximumHeaders = 4096,
        int maximumHeaderBytes = 64 * 1024)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        maximumHeaders = Math.Clamp(maximumHeaders, 1, 16384);
        maximumHeaderBytes = Math.Clamp(maximumHeaderBytes, 16, 1024 * 1024);

        var entries = inspection.Entries
            .Where(entry => !entry.IsDummy && entry.HeaderWithinAsset)
            .Take(maximumHeaders)
            .OrderBy(entry => entry.HeaderOffset)
            .ToArray();
        var result = new List<AudioAbkBnkHeaderEvidence>(entries.Length);
        for (var index = 0; index < entries.Length; index++)
        {
            var entry = entries[index];
            var absoluteEnd = data.Length;
            if (index + 1 < entries.Length && entries[index + 1].HeaderOffset > entry.HeaderOffset)
            {
                absoluteEnd = checked((int)Math.Min((uint)data.Length, entries[index + 1].HeaderOffset));
            }

            var available = absoluteEnd - checked((int)entry.HeaderOffset);
            var limit = Math.Min(available, maximumHeaderBytes);
            result.Add(ParseBnkHeaderEvidence(data, entry.EntryIndex, entry.HeaderOffset, limit));
        }

        return result;
    }

    public static bool TryGetBnkStreamDescriptor(
        AudioAbkBnkHeaderEvidence header,
        out AudioAbkBnkStreamDescriptor? descriptor)
    {
        ArgumentNullException.ThrowIfNull(header);
        descriptor = null;
        if (!header.Parsed || header.FindTag(0xA0)?.BigEndianValue is not { } codec ||
            header.FindTag(0x85)?.BigEndianValue is not { } sampleFrames ||
            header.FindTag(0x88)?.BigEndianValue is not { } firstOffset)
        {
            return false;
        }

        var channelValue = header.FindTag(0x82)?.BigEndianValue ?? 1u;
        var sampleRate = header.FindTag(0x84)?.BigEndianValue ??
                         (header.PlatformWord == 7 ? 24000u : 0u);
        if (channelValue is < 1 or > 2 || sampleRate is < 8000 or > 192000 || sampleFrames == 0)
        {
            return false;
        }

        var offsets = new List<uint> { firstOffset };
        if (channelValue == 2)
        {
            if (header.FindTag(0x89)?.BigEndianValue is not { } secondOffset)
            {
                return false;
            }
            offsets.Add(secondOffset);
        }

        descriptor = new AudioAbkBnkStreamDescriptor(
            header.EntryIndex,
            header.HeaderOffset,
            codec,
            checked((int)channelValue),
            checked((int)sampleRate),
            sampleFrames,
            offsets);
        return true;
    }

    /// <summary>
    /// Validates bounded Xbox IMA data ranges referenced by parsed BNK
    /// descriptors. This validates block headers only and does not allocate
    /// decoded PCM or select samples for events.
    /// </summary>
    public static IReadOnlyList<AudioAbkBnkStreamValidation> ValidateEmbeddedBnkStreams(
        AudioAbkBnkInspection inspection,
        ReadOnlySpan<byte> data)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var headers = InspectEmbeddedBnkHeaders(inspection, data);
        var result = new List<AudioAbkBnkStreamValidation>();
        foreach (var header in headers)
        {
            if (!TryGetBnkStreamDescriptor(header, out var descriptor) || descriptor is null)
            {
                continue;
            }

            if (descriptor.Codec != 0x14)
            {
                result.Add(new AudioAbkBnkStreamValidation(
                    descriptor.EntryIndex,
                    descriptor,
                    false,
                    null,
                    $"Unsupported embedded BNK codec 0x{descriptor.Codec:X}."));
                continue;
            }

            try
            {
                var absoluteOffsets = descriptor.RelativeChannelOffsets
                    .Select(offset => checked(inspection.Offset + offset))
                    .ToArray();
                var info = XboxImaAudioDecoder.Validate(
                    data,
                    descriptor.Channels,
                    descriptor.SampleRate,
                    descriptor.SampleFrames,
                    absoluteOffsets);
                result.Add(new AudioAbkBnkStreamValidation(
                    descriptor.EntryIndex,
                    descriptor,
                    true,
                    info,
                    null));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or OverflowException or ArgumentException)
            {
                result.Add(new AudioAbkBnkStreamValidation(
                    descriptor.EntryIndex,
                    descriptor,
                    false,
                    null,
                    exception.Message));
            }
        }

        return result;
    }

    public static AudioAbkAstReferenceEvidence InspectAstReferences(
        AudioAbkInspection inspection,
        Stream astStream)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(astStream);
        if (!astStream.CanSeek)
        {
            throw new ArgumentException("The AST stream must be seekable.", nameof(astStream));
        }

        return InspectAstReferencesCore(inspection, offset =>
        {
            if (astStream.Length < 4 || offset > (ulong)(astStream.Length - 4))
            {
                return (false, false);
            }

            var signature = new byte[4];
            astStream.Seek(offset, SeekOrigin.Begin);
            astStream.ReadExactly(signature);
            return (true, signature.AsSpan().SequenceEqual("SCHl"u8));
        });
    }

    private static AudioAbkAstReferenceEvidence InspectAstReferencesCore(
        AudioAbkInspection inspection,
        Func<uint, (bool Within, bool Schl)> probe)
    {
        var streamSounds = inspection.Sounds
            .Where(sound => !sound.IsDummy && sound.SoundType is 1 or 2)
            .ToArray();
        var issues = new List<string>();
        var dataWithin = 0;
        var dataSchl = 0;
        var loopWithin = 0;
        var loopSchl = 0;
        foreach (var sound in streamSounds)
        {
            var dataProbe = probe(sound.DataOffset);
            if (dataProbe.Within)
            {
                dataWithin++;
                if (dataProbe.Schl)
                {
                    dataSchl++;
                }
                else
                {
                    issues.Add($"sound={sound.SoundIndex}:data=0x{sound.DataOffset:X}:not-SCHl");
                }
            }
            else
            {
                issues.Add($"sound={sound.SoundIndex}:data=0x{sound.DataOffset:X}:outside-ast");
            }

            if (sound.SoundType != 2)
            {
                continue;
            }

            var loopProbe = probe(sound.LoopOffset);
            if (loopProbe.Within)
            {
                loopWithin++;
                if (loopProbe.Schl)
                {
                    loopSchl++;
                }
                else
                {
                    issues.Add($"sound={sound.SoundIndex}:loop=0x{sound.LoopOffset:X}:not-SCHl");
                }
            }
            else
            {
                issues.Add($"sound={sound.SoundIndex}:loop=0x{sound.LoopOffset:X}:outside-ast");
            }
        }

        return new AudioAbkAstReferenceEvidence(
            streamSounds.Length,
            dataWithin,
            dataSchl,
            loopWithin,
            loopSchl,
            issues);
    }

    private static AudioAbkSampleTable ParseSampleTable(
        ReadOnlySpan<byte> data,
        uint tableOffset,
        int moduleIndex,
        int playerIndex,
        int tableIndex,
        bool littleEndian,
        List<AudioAbkSoundEntry> sounds)
    {
        EnsureRange(data, tableOffset, 4, "ABKC sample table header");
        var soundCount = ReadUInt32(data, checked((int)tableOffset), littleEndian);
        if (soundCount > MaximumSoundsPerTable)
        {
            throw new InvalidDataException($"ABKC sound count {soundCount} exceeds the safety limit.");
        }
        var tableBytes = checked(4u + soundCount * SampleEntrySize);
        EnsureRange(data, tableOffset, checked((int)tableBytes), "ABKC sample table entries");

        var entries = new List<AudioAbkSoundEntry>(checked((int)soundCount));
        for (var entryIndex = 0u; entryIndex < soundCount; entryIndex++)
        {
            var entryOffset = checked(tableOffset + 4u + (uint)entryIndex * SampleEntrySize);
            var soundType = data[checked((int)entryOffset)];
            var priority = data[checked((int)entryOffset + 1)];
            var dataOffset = ReadUInt32(data, checked((int)entryOffset + 4), littleEndian);
            var loopOffset = ReadUInt32(data, checked((int)entryOffset + 8), littleEndian);
            var isDummy = soundType == 0 && dataOffset == 0;
            var soundIndex = isDummy ? 0 : sounds.Count(sound => !sound.IsDummy) + 1;
            var sound = new AudioAbkSoundEntry(
                soundIndex,
                moduleIndex,
                playerIndex,
                tableIndex,
                checked((int)entryIndex),
                soundType,
                priority,
                dataOffset,
                loopOffset,
                isDummy);
            entries.Add(sound);
            sounds.Add(sound);
        }

        return new AudioAbkSampleTable(tableIndex, tableOffset, soundCount, entries);
    }

    private static AudioAbkBnkHeaderEvidence ParseBnkHeaderEvidence(
        ReadOnlySpan<byte> data,
        int entryIndex,
        uint headerOffset,
        int availableLength)
    {
        if (availableLength < 4 || !HasRange(data, headerOffset, availableLength))
        {
            return new AudioAbkBnkHeaderEvidence(
                entryIndex,
                headerOffset,
                "unknown",
                null,
                Array.Empty<AudioAbkBnkTagEvidence>(),
                headerOffset,
                false,
                "BNK variable header is shorter than its prefix.");
        }

        var bytes = data.Slice(checked((int)headerOffset), availableLength);
        var prefix = bytes[..4] switch
        {
            var value when value.SequenceEqual("GSTR"u8) => "GSTR",
            var value when value[0] == (byte)'P' && value[1] == (byte)'T' => "PT",
            _ => "unknown",
        };
        if (prefix == "unknown")
        {
            return new AudioAbkBnkHeaderEvidence(
                entryIndex,
                headerOffset,
                prefix,
                null,
                Array.Empty<AudioAbkBnkTagEvidence>(),
                headerOffset,
                false,
                "BNK variable header has no observed GSTR/PT prefix.");
        }

        var cursor = prefix == "GSTR" ? 8 : 4;
        if (cursor > bytes.Length)
        {
            return new AudioAbkBnkHeaderEvidence(
                entryIndex,
                headerOffset,
                prefix,
                null,
                Array.Empty<AudioAbkBnkTagEvidence>(),
                checked(headerOffset + (uint)cursor),
                false,
                "BNK variable header prefix extends beyond the entry range.");
        }

        ushort? platform = null;
        if (prefix == "PT")
        {
            platform = BinaryPrimitives.ReadUInt16LittleEndian(bytes[2..4]);
        }

        var tags = new List<AudioAbkBnkTagEvidence>();
        while (cursor < bytes.Length)
        {
            var tag = bytes[cursor++];
            if (tag is 0xFF or 0xFE)
            {
                return new AudioAbkBnkHeaderEvidence(
                    entryIndex,
                    headerOffset,
                    prefix,
                    platform,
                    tags,
                    checked(headerOffset + (uint)cursor),
                    true,
                    null);
            }

            if (tag is 0xFC or 0xFD)
            {
                tags.Add(new AudioAbkBnkTagEvidence(tag, Array.Empty<byte>(), 0));
                continue;
            }

            if (cursor >= bytes.Length)
            {
                break;
            }

            var lengthByte = bytes[cursor++];
            uint payloadLength = lengthByte;
            if (lengthByte == 0xFF)
            {
                if (bytes.Length - cursor < sizeof(uint))
                {
                    break;
                }

                payloadLength = BinaryPrimitives.ReadUInt32BigEndian(bytes[cursor..]);
                cursor += sizeof(uint);
            }

            if (payloadLength > (uint)(bytes.Length - cursor))
            {
                break;
            }

            var payload = bytes.Slice(cursor, checked((int)payloadLength)).ToArray();
            uint? value = payload.Length <= sizeof(uint)
                ? ReadBigEndianValue(payload)
                : null;
            tags.Add(new AudioAbkBnkTagEvidence(tag, payload, value));
            cursor += checked((int)payloadLength);
        }

        return new AudioAbkBnkHeaderEvidence(
            entryIndex,
            headerOffset,
            prefix,
            platform,
            tags,
            checked(headerOffset + (uint)cursor),
            false,
            "BNK variable header ended before an observed terminator.");
    }

    private static uint ReadBigEndianValue(ReadOnlySpan<byte> payload)
    {
        var value = 0u;
        foreach (var item in payload)
        {
            value = checked((value << 8) | item);
        }

        return value;
    }

    private static bool DetectLittleEndian(ReadOnlySpan<byte> data)
    {
        var little = BinaryPrimitives.ReadUInt32LittleEndian(data[0x1C..0x20]);
        var big = BinaryPrimitives.ReadUInt32BigEndian(data[0x1C..0x20]);
        return little <= (uint)data.Length && (little == 0 || little % 4 == 0) ||
               big > (uint)data.Length;
    }

    private static ushort ReadUInt16(ReadOnlySpan<byte> data, int offset, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt16LittleEndian(data[offset..])
            : BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);

    private static uint ReadUInt32(ReadOnlySpan<byte> data, int offset, bool littleEndian) =>
        littleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(data[offset..])
            : BinaryPrimitives.ReadUInt32BigEndian(data[offset..]);

    private static void EnsureRange(ReadOnlySpan<byte> data, uint offset, int length, string label)
    {
        if (!HasRange(data, offset, length))
        {
            throw new InvalidDataException($"{label} range 0x{offset:X}+0x{length:X} exceeds the ABKC asset.");
        }
    }

    private static bool HasRange(ReadOnlySpan<byte> data, uint offset, int length)
    {
        if (length < 0 || offset > (uint)data.Length)
        {
            return false;
        }

        return length <= data.Length - (int)offset;
    }
}
