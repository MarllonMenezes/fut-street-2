using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioBankEvidence(
    int AssetLength,
    uint WordAt4,
    uint WordAt8,
    uint WordAt20,
    uint WordAt1C,
    bool WordAt20MatchesAssetLength,
    bool WordAt1CWithinAsset,
    IReadOnlyList<uint> PrefixWords,
    int SchlSignatureCount,
    int ScdlSignatureCount);

public sealed record AudioOffsetRow(
    int LineNumber,
    long FirstColumn,
    long SecondColumn,
    long ThirdColumn);

public sealed record AudioOffsetTableInspection(
    int AssetLength,
    long? CompanionLength,
    IReadOnlyList<AudioOffsetRow> Rows,
    bool IsAsciiCsv,
    bool RowsWellFormed,
    bool NonNegativeColumns,
    bool FirstColumnMonotonic,
    bool RangesWithinCompanion,
    bool FirstRowStartsAtZero)
{
    public bool StructureValid =>
        IsAsciiCsv &&
        RowsWellFormed &&
        NonNegativeColumns &&
        FirstColumnMonotonic &&
        RangesWithinCompanion;
}

public sealed record AudioCompanionEvidence(
    int RangeCount,
    IReadOnlyDictionary<string, int> FourBytePrefixCounts,
    bool AllRangesReadable,
    bool AllRangesStartWithSchl)
{
    public int SchlPrefixCount =>
        FourBytePrefixCounts.TryGetValue("SCHl", out var count) ? count : 0;
}

public sealed record AudioParallelCompanionEvidence(
    int HeaderRangeCount,
    int DataRangeCount,
    bool SameRangeCount,
    bool HeaderRangesReadable,
    bool DataRangesReadable,
    int DataSchlPrefixCount)
{
    public bool AllDataRangesStartWithSchl =>
        DataRangesReadable && DataRangeCount > 0 && DataSchlPrefixCount == DataRangeCount;
}

public sealed record AudioIndexedPairSample(
    int RowIndex,
    long HeaderOffset,
    long HeaderLength,
    long DataOffset,
    long DataLength,
    string HeaderPrefixHex,
    string DataPrefixHex,
    int SharedNonZeroAlignedWords);

public sealed record AudioIndexedPairComparison(
    int PairCount,
    bool SameRangeCount,
    bool HeaderRangesReadable,
    bool DataRangesReadable,
    int DataSchlPrefixCount,
    int HeaderDataLengthEqualCount,
    int HeaderDataThirdColumnEqualCount,
    int HeaderPrefixEqualCount,
    int PairsWithSharedNonZeroAlignedWords,
    IReadOnlyList<AudioIndexedPairSample> Samples);

public sealed record AudioIndexedSchlSample(
    int RowIndex,
    int HeaderSize,
    int Platform,
    int Version,
    int Codec,
    int Channels,
    int SampleRate,
    uint DeclaredSamples,
    int HeaderMatchesSchlHeaderSize,
    int HeaderMatchesPlatform,
    int HeaderMatchesVersion,
    int HeaderMatchesCodec,
    int HeaderMatchesChannels,
    int HeaderMatchesSampleRate,
    int HeaderMatchesDeclaredSamples,
    bool MetadataReachedTerminator,
    bool MetadataBoundaryViolation,
    int MetadataTokenCount,
    int MetadataBoundaryViolationOffset);

public sealed record AudioIndexedSchlComparison(
    int PairCount,
    int SchlEnvelopeValidCount,
    int SchlPtHeaderCount,
    int SchlPlatform7Count,
    int SchlMetadataHeaderAvailableCount,
    int SchlMetadataTerminatedCount,
    int SchlMetadataBoundaryViolationCount,
    int SchlParsedCount,
    int SchlParseFailureCount,
    int HeaderContainsSchlHeaderSizeCount,
    int HeaderContainsPlatformCount,
    int HeaderContainsVersionCount,
    int HeaderContainsCodecCount,
    int HeaderContainsChannelsCount,
    int HeaderContainsSampleRateCount,
    int HeaderContainsDeclaredSamplesCount,
    IReadOnlyDictionary<string, int> SchlParseFailureReasons,
    IReadOnlyDictionary<int, int> PlatformWordOffsets,
    IReadOnlyDictionary<int, int> VersionWordOffsets,
    IReadOnlyList<AudioIndexedSchlSample> Samples);

public sealed record AudioEventSchlFieldMatch(
    string FieldName,
    int CandidateCount,
    int DistinctMatchedCount,
    int OccurrenceCount);

public sealed record AudioEventSchlFieldComparison(
    int DataRangeCount,
    int ParsedStreamCount,
    IReadOnlyList<AudioEventSchlFieldMatch> Fields);

public sealed record AudioEventTextRun(int Offset, string Value);

public sealed record AudioEventStringIndexEntry(
    int RecordOffset,
    uint StringOffset,
    uint HashWord,
    string Value);

public sealed record AudioRawWordMatch(
    uint Value,
    int Count,
    int FirstWordOffset);

public sealed record AudioRawWordOccurrence(uint Value, int WordOffset);

public sealed record AudioAlignedWordSearchResult(
    uint Value,
    int Count,
    IReadOnlyList<long> FirstOffsets);

public sealed record AudioEventHeaderCandidate(
    int EventWordOffset,
    uint EventWord,
    int HeaderRowIndex,
    ushort HeaderLeadingWord,
    AudioOffsetRow HeaderRow,
    AudioOffsetRow? ParallelDataRow);

public sealed record AudioEventNamedHeaderCandidate(
    string EventName,
    uint EventStringOffset,
    int HeaderRowIndex,
    AudioOffsetRow HeaderRow,
    AudioOffsetRow? ParallelDataRow,
    int EventOccurrenceCount);

public sealed record AudioEventRelativeWordStats(
    int RelativeByteOffset,
    int SampleCount,
    int DistinctWordCount,
    int DistinctNonZeroWordCount,
    uint MostCommonValue,
    int MostCommonCount);

public sealed record AudioDuplicateOccurrenceContextEvidence(
    int DuplicateValueCount,
    int DuplicateOccurrenceCount,
    int DuplicateValuesWithSingleContext,
    int DuplicateValuesWithMultipleContexts,
    int ValidContextOccurrenceCount,
    int DistinctContextCount,
    int ContextFamiliesWithMultipleOccurrences,
    int MostCommonContextCount);

public sealed record AudioOccurrenceSpacingEvidence(
    int OccurrenceCount,
    int DistinctValueCount,
    int FirstOffset,
    int LastOffset,
    int MinGap,
    int MaxGap,
    int DistinctGapCount,
    int MostCommonGap,
    int MostCommonGapCount);

public sealed record EventFileEvidence(
    int AssetLength,
    string PrefixHex,
    bool HasObservedPrefix,
    uint WordAt4,
    uint WordAt8,
    uint WordAtC,
    bool WordAt4MatchesAssetLength,
    bool WordAt4WithinAsset,
    IReadOnlyList<uint> PrefixWords,
    IReadOnlyList<AudioEventTextRun> TextRuns);

public static partial class AudioAssetReader
{
    private static readonly byte[] ObservedEventPrefix = [0x03, 0x12, 0x3C, 0x07];

    public static AudioBankEvidence InspectBankEvidence(ReadOnlySpan<byte> data)
    {
        if (data.Length < 32 || !data[..4].SequenceEqual("ABKC"u8))
        {
            throw new InvalidDataException("Unsupported ABKC audio bank.");
        }

        var words = ReadPrefixWords(data, 0x5C);
        var wordAt4 = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        var wordAt8 = BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]);
        var wordAt20 = BinaryPrimitives.ReadUInt32LittleEndian(data[20..24]);
        var wordAt1C = BinaryPrimitives.ReadUInt32LittleEndian(data[0x1C..0x20]);
        return new AudioBankEvidence(
            data.Length,
            wordAt4,
            wordAt8,
            wordAt20,
            wordAt1C,
            wordAt20 == data.Length,
            wordAt1C >= 0x20 && wordAt1C <= data.Length,
            words,
            CountSignature(data, "SCHl"u8),
            CountSignature(data, "SCDl"u8));
    }

    public static AudioOffsetTableInspection InspectOffsetTable(
        ReadOnlySpan<byte> data,
        long? companionLength = null)
    {
        var isAscii = true;
        foreach (var value in data)
        {
            if (value is not (0x09 or 0x0A or 0x0D or 0x20 or 0x2C or 0x2D or >= 0x30 and <= 0x39))
            {
                isAscii = false;
                break;
            }
        }
        var rows = new List<AudioOffsetRow>();
        var wellFormed = isAscii;
        var nonNegative = true;
        var lineNumber = 0;
        var text = Encoding.ASCII.GetString(data);
        var lines = text.Split('\n');
        foreach (var rawLine in lines)
        {
            lineNumber++;
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                if (lineNumber == lines.Length)
                {
                    continue;
                }

                wellFormed = false;
                continue;
            }

            var columns = line.Split(',');
            var hasTrailingEmptyColumn = columns.Length == 4 && columns[3].Length == 0;
            if ((columns.Length != 3 && !hasTrailingEmptyColumn) ||
                !long.TryParse(columns[0], NumberStyles.None, CultureInfo.InvariantCulture, out var first) ||
                !long.TryParse(columns[1], NumberStyles.None, CultureInfo.InvariantCulture, out var second) ||
                !long.TryParse(columns[2], NumberStyles.None, CultureInfo.InvariantCulture, out var third))
            {
                wellFormed = false;
                continue;
            }

            nonNegative &= first >= 0 && second >= 0 && third >= 0;
            rows.Add(new AudioOffsetRow(lineNumber, first, second, third));
        }

        var monotonic = true;
        var rangesWithinCompanion = true;
        for (var index = 0; index < rows.Count; index++)
        {
            if (index > 0 && rows[index].FirstColumn < rows[index - 1].FirstColumn)
            {
                monotonic = false;
            }

            if (companionLength is not null)
            {
                var row = rows[index];
                rangesWithinCompanion &=
                    row.FirstColumn <= companionLength.Value &&
                    row.SecondColumn <= companionLength.Value - row.FirstColumn;
            }
        }

        return new AudioOffsetTableInspection(
            data.Length,
            companionLength,
            rows,
            isAscii,
            wellFormed && rows.Count > 0,
            nonNegative,
            monotonic,
            rangesWithinCompanion,
            rows.Count > 0 && rows[0].FirstColumn == 0);
    }

    public static EventFileEvidence InspectEventFile(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            throw new InvalidDataException("Event file is shorter than the observed prefix.");
        }

        var prefixWords = ReadPrefixWords(data, 0x40);
        var wordAt4 = BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]);
        return new EventFileEvidence(
            data.Length,
            Convert.ToHexString(data[..Math.Min(data.Length, 16)]),
            data[..4].SequenceEqual(ObservedEventPrefix),
            wordAt4,
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[12..16]),
            wordAt4 == data.Length,
            wordAt4 <= data.Length,
            prefixWords,
            FindTextRuns(data));
    }

    /// <summary>
    /// Finds aligned pairs of a relative string-table offset and a following
    /// word. The second word is deliberately called HashWord only as a neutral
    /// binary label; this method does not assign an ID or audio meaning.
    /// </summary>
    public static IReadOnlyList<AudioEventStringIndexEntry> InspectStringIndex(
        ReadOnlySpan<byte> data,
        int stringTableOffset,
        int searchStart = 0,
        int? searchEnd = null)
    {
        if (stringTableOffset < 0 || stringTableOffset >= data.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(stringTableOffset));
        }
        searchStart = Math.Max(0, searchStart);
        var end = Math.Min(data.Length - 8, searchEnd ?? stringTableOffset);
        if (end < searchStart)
        {
            return Array.Empty<AudioEventStringIndexEntry>();
        }

        var entries = new List<AudioEventStringIndexEntry>();
        for (var offset = searchStart; offset <= end; offset += sizeof(uint))
        {
            var stringOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset, 4));
            if (stringOffset > (uint)(data.Length - stringTableOffset - 1))
            {
                continue;
            }

            var stringStart = checked(stringTableOffset + (int)stringOffset);
            var cursor = stringStart;
            while (cursor < data.Length && data[cursor] is >= 0x20 and <= 0x7E)
            {
                if (cursor - stringStart > 128)
                {
                    cursor = -1;
                    break;
                }
                cursor++;
            }
            if (cursor < 0 || cursor == stringStart || cursor >= data.Length || data[cursor] != 0)
            {
                continue;
            }

            var value = Encoding.ASCII.GetString(data.Slice(stringStart, cursor - stringStart));
            var hashWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 4, 4));
            entries.Add(new AudioEventStringIndexEntry(offset, stringOffset, hashWord, value));
        }

        return entries;
    }

    public static AudioCompanionEvidence InspectOffsetCompanion(
        Stream companion,
        AudioOffsetTableInspection table)
    {
        ArgumentNullException.ThrowIfNull(companion);
        var prefixCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var allReadable = table.RowsWellFormed && table.RangesWithinCompanion;
        var buffer = new byte[4];
        foreach (var row in table.Rows)
        {
            if (row.FirstColumn < 0 || row.FirstColumn > companion.Length - 4)
            {
                allReadable = false;
                continue;
            }

            companion.Seek(row.FirstColumn, SeekOrigin.Begin);
            var read = 0;
            while (read < buffer.Length)
            {
                var chunk = companion.Read(buffer, read, buffer.Length - read);
                if (chunk == 0)
                {
                    break;
                }

                read += chunk;
            }
            if (read != buffer.Length)
            {
                allReadable = false;
                continue;
            }

            var prefix = Encoding.ASCII.GetString(buffer);
            prefixCounts[prefix] = prefixCounts.TryGetValue(prefix, out var count) ? count + 1 : 1;
        }

        var schlCount = prefixCounts.TryGetValue("SCHl", out var matchingCount) ? matchingCount : 0;
        return new AudioCompanionEvidence(
            table.Rows.Count,
            prefixCounts,
            allReadable,
            allReadable && table.Rows.Count > 0 && schlCount == table.Rows.Count);
    }

    public static IReadOnlyList<AudioRawWordMatch> FindAlignedRawWordMatches(
        ReadOnlySpan<byte> eventData,
        IReadOnlyList<AudioOffsetRow> rows)
    {
        return FindAlignedRawWordMatches(
            eventData,
            rows
                .Select(row => row.FirstColumn)
                .Where(value => value > 0 && value <= uint.MaxValue)
                .Select(value => (uint)value));
    }

    public static IReadOnlyList<AudioRawWordMatch> FindAlignedRawWordMatches(
        ReadOnlySpan<byte> eventData,
        IEnumerable<uint> candidateValues)
    {
        var candidates = candidateValues.ToHashSet();
        var matches = new Dictionary<uint, (int Count, int FirstOffset)>();
        var wordCount = eventData.Length / sizeof(uint);
        for (var index = 0; index < wordCount; index++)
        {
            var offset = index * sizeof(uint);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(eventData[offset..]);
            if (!candidates.Contains(value))
            {
                continue;
            }

            if (matches.TryGetValue(value, out var previous))
            {
                matches[value] = (previous.Count + 1, previous.FirstOffset);
            }
            else
            {
                matches[value] = (1, offset);
            }
        }

        return matches
            .OrderBy(pair => pair.Key)
            .Select(pair => new AudioRawWordMatch(pair.Key, pair.Value.Count, pair.Value.FirstOffset))
            .ToArray();
    }

    public static IReadOnlyList<AudioRawWordOccurrence> FindAlignedRawWordOccurrences(
        ReadOnlySpan<byte> eventData,
        IEnumerable<uint> candidateValues)
    {
        var candidates = candidateValues.ToHashSet();
        var occurrences = new List<AudioRawWordOccurrence>();
        var wordCount = eventData.Length / sizeof(uint);
        for (var index = 0; index < wordCount; index++)
        {
            var offset = index * sizeof(uint);
            var value = BinaryPrimitives.ReadUInt32LittleEndian(eventData[offset..]);
            if (candidates.Contains(value))
            {
                occurrences.Add(new AudioRawWordOccurrence(value, offset));
            }
        }

        return occurrences;
    }

    public static IReadOnlyList<AudioEventRelativeWordStats> InspectRelativeWordStats(
        ReadOnlySpan<byte> eventData,
        IReadOnlyList<AudioRawWordOccurrence> occurrences,
        int radiusWords = 4)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        if (radiusWords < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusWords));
        }

        var result = new List<AudioEventRelativeWordStats>((radiusWords * 2) + 1);
        for (var relativeWord = -radiusWords; relativeWord <= radiusWords; relativeWord++)
        {
            var counts = new Dictionary<uint, int>();
            foreach (var occurrence in occurrences)
            {
                var offset = occurrence.WordOffset + relativeWord * sizeof(uint);
                if (offset < 0 || offset + sizeof(uint) > eventData.Length)
                {
                    continue;
                }

                var value = BinaryPrimitives.ReadUInt32LittleEndian(eventData[offset..]);
                counts[value] = counts.TryGetValue(value, out var count) ? count + 1 : 1;
            }

            var mostCommon = counts
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .FirstOrDefault();
            result.Add(new AudioEventRelativeWordStats(
                relativeWord * sizeof(uint),
                counts.Values.Sum(),
                counts.Count,
                counts.Keys.Count(value => value != 0),
                mostCommon.Key,
                mostCommon.Value));
        }

        return result;
    }

    public static AudioDuplicateOccurrenceContextEvidence InspectDuplicateOccurrenceContexts(
        ReadOnlySpan<byte> eventData,
        IReadOnlyList<AudioRawWordOccurrence> occurrences,
        int radiusWords = 4)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        if (radiusWords < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(radiusWords));
        }

        var occurrenceCounts = new Dictionary<uint, int>();
        var contexts = new Dictionary<uint, HashSet<string>>();
        var allContextCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var occurrence in occurrences)
        {
            occurrenceCounts[occurrence.Value] = occurrenceCounts.TryGetValue(occurrence.Value, out var count)
                ? count + 1
                : 1;
            var start = occurrence.WordOffset - radiusWords * sizeof(uint);
            var length = (radiusWords * 2 + 1) * sizeof(uint);
            if (start < 0 || start + length > eventData.Length)
            {
                continue;
            }

            var context = Convert.ToHexString(eventData.Slice(start, length));
            if (!contexts.TryGetValue(occurrence.Value, out var contextSet))
            {
                contextSet = new HashSet<string>(StringComparer.Ordinal);
                contexts.Add(occurrence.Value, contextSet);
            }

            contextSet.Add(context);
            allContextCounts[context] = allContextCounts.TryGetValue(context, out var contextCount)
                ? contextCount + 1
                : 1;
        }

        var duplicateValues = occurrenceCounts.Where(pair => pair.Value > 1).Select(pair => pair.Key).ToArray();
        var singleContext = duplicateValues.Count(value => contexts.TryGetValue(value, out var set) && set.Count == 1);
        var multipleContexts = duplicateValues.Count(value => contexts.TryGetValue(value, out var set) && set.Count > 1);
        return new AudioDuplicateOccurrenceContextEvidence(
            duplicateValues.Length,
            duplicateValues.Sum(value => occurrenceCounts[value]),
            singleContext,
            multipleContexts,
            allContextCounts.Values.Sum(),
            allContextCounts.Count,
            allContextCounts.Count(pair => pair.Value > 1),
            allContextCounts.Values.DefaultIfEmpty(0).Max());
    }

    public static AudioOccurrenceSpacingEvidence InspectOccurrenceSpacing(
        IReadOnlyList<AudioRawWordOccurrence> occurrences)
    {
        ArgumentNullException.ThrowIfNull(occurrences);
        var sorted = occurrences
            .Select(occurrence => occurrence.WordOffset)
            .OrderBy(offset => offset)
            .ToArray();
        var gaps = sorted
            .Zip(sorted.Skip(1), (previous, current) => current - previous)
            .ToArray();
        var commonGap = gaps
            .GroupBy(gap => gap)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .FirstOrDefault();
        return new AudioOccurrenceSpacingEvidence(
            occurrences.Count,
            occurrences.Select(occurrence => occurrence.Value).Distinct().Count(),
            sorted.FirstOrDefault(-1),
            sorted.LastOrDefault(-1),
            gaps.DefaultIfEmpty(0).Min(),
            gaps.DefaultIfEmpty(0).Max(),
            gaps.Distinct().Count(),
            commonGap?.Key ?? 0,
            commonGap?.Count() ?? 0);
    }

    public static IReadOnlyList<uint> ReadCompanionLeadingWords(
        Stream companion,
        AudioOffsetTableInspection table)
    {
        ArgumentNullException.ThrowIfNull(companion);
        var result = new List<uint>(table.Rows.Count);
        var buffer = new byte[2];
        foreach (var row in table.Rows)
        {
            if (row.FirstColumn < 0 || row.FirstColumn > companion.Length - buffer.Length)
            {
                continue;
            }

            companion.Seek(row.FirstColumn, SeekOrigin.Begin);
            var read = 0;
            while (read < buffer.Length)
            {
                var chunk = companion.Read(buffer, read, buffer.Length - read);
                if (chunk == 0)
                {
                    break;
                }

                read += chunk;
            }

            if (read == buffer.Length)
            {
                result.Add(BinaryPrimitives.ReadUInt16LittleEndian(buffer));
            }
        }

        return result;
    }

    /// <summary>
    /// Correlates aligned event words with the exact two-byte leading words of
    /// header ranges. Duplicate leading words intentionally produce multiple
    /// candidates; no row is silently selected. A parallel data row is exposed
    /// only when the caller supplied a table with the same row cardinality.
    /// </summary>
    public static IReadOnlyList<AudioEventHeaderCandidate> CorrelateEventToHeaderRows(
        ReadOnlySpan<byte> eventData,
        Stream headerCompanion,
        AudioOffsetTableInspection headerTable,
        AudioOffsetTableInspection? parallelDataTable = null)
    {
        ArgumentNullException.ThrowIfNull(headerCompanion);
        ArgumentNullException.ThrowIfNull(headerTable);

        var rowsByLeadingWord = new Dictionary<ushort, List<(int Index, ushort Word)>>();
        var leadingBuffer = new byte[sizeof(ushort)];
        for (var rowIndex = 0; rowIndex < headerTable.Rows.Count; rowIndex++)
        {
            var row = headerTable.Rows[rowIndex];
            if (!TryReadAt(headerCompanion, row.FirstColumn, leadingBuffer))
            {
                continue;
            }

            var leadingWord = BinaryPrimitives.ReadUInt16LittleEndian(leadingBuffer);
            if (!rowsByLeadingWord.TryGetValue(leadingWord, out var rows))
            {
                rows = new List<(int Index, ushort Word)>();
                rowsByLeadingWord.Add(leadingWord, rows);
            }
            rows.Add((rowIndex, leadingWord));
        }

        var candidates = new List<AudioEventHeaderCandidate>();
        for (var wordOffset = 0; wordOffset + sizeof(uint) <= eventData.Length; wordOffset += sizeof(uint))
        {
            var eventWord = BinaryPrimitives.ReadUInt32LittleEndian(eventData[wordOffset..]);
            if (eventWord > ushort.MaxValue || !rowsByLeadingWord.TryGetValue((ushort)eventWord, out var rows))
            {
                continue;
            }

            foreach (var rowMatch in rows)
            {
                var parallelRow = parallelDataTable is not null &&
                                  parallelDataTable.Rows.Count == headerTable.Rows.Count &&
                                  rowMatch.Index < parallelDataTable.Rows.Count
                    ? parallelDataTable.Rows[rowMatch.Index]
                    : null;
                candidates.Add(new AudioEventHeaderCandidate(
                    wordOffset,
                    eventWord,
                    rowMatch.Index,
                    rowMatch.Word,
                    headerTable.Rows[rowMatch.Index],
                    parallelRow));
            }
        }

        return candidates;
    }

    public static IReadOnlyList<AudioEventNamedHeaderCandidate> CorrelateEventTextStartsToHeaderRows(
        ReadOnlySpan<byte> eventData,
        Stream headerCompanion,
        AudioOffsetTableInspection headerTable,
        AudioOffsetTableInspection? parallelDataTable = null,
        string stringTableMarker = "panna_goal")
    {
        ArgumentNullException.ThrowIfNull(headerCompanion);
        ArgumentNullException.ThrowIfNull(headerTable);
        ArgumentException.ThrowIfNullOrWhiteSpace(stringTableMarker);

        var evidence = InspectEventFile(eventData);
        var stringTable = evidence.TextRuns.FirstOrDefault(run =>
            run.Value.Contains(stringTableMarker, StringComparison.Ordinal));
        if (stringTable is null)
        {
            return Array.Empty<AudioEventNamedHeaderCandidate>();
        }

        var namesByOffset = evidence.TextRuns
            .Where(run => run.Offset >= stringTable.Offset)
            .GroupBy(run => (uint)run.Offset)
            .ToDictionary(group => group.Key, group => group.First().Value);
        var candidates = CorrelateEventToHeaderRows(
            eventData,
            headerCompanion,
            headerTable,
            parallelDataTable);
        return candidates
            .Where(candidate => namesByOffset.ContainsKey(candidate.EventWord))
            .GroupBy(candidate => (candidate.EventWord, candidate.HeaderRowIndex))
            .Select(group =>
            {
                var first = group.First();
                return new AudioEventNamedHeaderCandidate(
                    namesByOffset[group.Key.EventWord],
                    group.Key.EventWord,
                    first.HeaderRowIndex,
                    first.HeaderRow,
                    first.ParallelDataRow,
                    group.Count());
            })
            .OrderBy(candidate => candidate.HeaderRowIndex)
            .ThenBy(candidate => candidate.EventName, StringComparer.Ordinal)
            .ToArray();
    }

    public static IReadOnlyList<AudioAlignedWordSearchResult> SearchAlignedWords(
        Stream source,
        IReadOnlySet<uint> values,
        int maximumOffsetsPerValue = 4)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(values);
        if (!source.CanSeek || source.Length < 0)
        {
            throw new ArgumentException("The source must be a seekable stream.", nameof(source));
        }
        if (maximumOffsetsPerValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumOffsetsPerValue));
        }

        var counts = values.ToDictionary(value => value, _ => 0);
        var offsets = values.ToDictionary(value => value, _ => new List<long>());
        var originalPosition = source.Position;
        source.Position = 0;
        try
        {
            const int BufferSize = 1024 * 1024;
            var buffer = new byte[BufferSize];
            var remaining = source.Length;
            long absoluteOffset = 0;
            while (remaining >= sizeof(uint))
            {
                var requested = checked((int)Math.Min((long)buffer.Length, remaining));
                if (requested > sizeof(uint))
                {
                    requested -= requested % sizeof(uint);
                }

                source.ReadExactly(buffer.AsSpan(0, requested));
                for (var offset = 0; offset + sizeof(uint) <= requested; offset += sizeof(uint))
                {
                    var value = BinaryPrimitives.ReadUInt32LittleEndian(buffer.AsSpan(offset, sizeof(uint)));
                    if (!counts.ContainsKey(value))
                    {
                        continue;
                    }

                    counts[value]++;
                    if (offsets[value].Count < maximumOffsetsPerValue)
                    {
                        offsets[value].Add(absoluteOffset + offset);
                    }
                }

                absoluteOffset += requested;
                remaining -= requested;
            }
        }
        finally
        {
            source.Position = originalPosition;
        }

        return values
            .OrderBy(value => value)
            .Select(value => new AudioAlignedWordSearchResult(value, counts[value], offsets[value]))
            .ToArray();
    }

    public static AudioParallelCompanionEvidence InspectParallelCompanions(
        Stream headerCompanion,
        AudioOffsetTableInspection headerTable,
        Stream dataCompanion,
        AudioOffsetTableInspection dataTable)
    {
        ArgumentNullException.ThrowIfNull(headerCompanion);
        ArgumentNullException.ThrowIfNull(dataCompanion);
        var headerReadable = headerTable.RowsWellFormed && headerTable.RangesWithinCompanion;
        var dataReadable = dataTable.RowsWellFormed && dataTable.RangesWithinCompanion;
        var schlCount = 0;
        var headerBuffer = new byte[2];
        var dataBuffer = new byte[4];
        var rowCount = Math.Min(headerTable.Rows.Count, dataTable.Rows.Count);
        for (var index = 0; index < rowCount; index++)
        {
            var headerRow = headerTable.Rows[index];
            if (!TryReadAt(headerCompanion, headerRow.FirstColumn, headerBuffer))
            {
                headerReadable = false;
            }

            var dataRow = dataTable.Rows[index];
            if (!TryReadAt(dataCompanion, dataRow.FirstColumn, dataBuffer))
            {
                dataReadable = false;
            }
            else if (dataBuffer.AsSpan().SequenceEqual("SCHl"u8))
            {
                schlCount++;
            }
        }

        return new AudioParallelCompanionEvidence(
            headerTable.Rows.Count,
            dataTable.Rows.Count,
            headerTable.Rows.Count == dataTable.Rows.Count,
            headerReadable,
            dataReadable,
            schlCount);
    }

    public static AudioIndexedPairComparison CompareIndexedCompanions(
        Stream headerCompanion,
        AudioOffsetTableInspection headerTable,
        Stream dataCompanion,
        AudioOffsetTableInspection dataTable,
        int sampleLimit = 4)
    {
        ArgumentNullException.ThrowIfNull(headerCompanion);
        ArgumentNullException.ThrowIfNull(dataCompanion);
        if (sampleLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit));
        }

        var headerReadable = headerTable.RowsWellFormed && headerTable.RangesWithinCompanion;
        var dataReadable = dataTable.RowsWellFormed && dataTable.RangesWithinCompanion;
        var sameRangeCount = headerTable.Rows.Count == dataTable.Rows.Count;
        var pairCount = Math.Min(headerTable.Rows.Count, dataTable.Rows.Count);
        var dataSchlCount = 0;
        var headerDataLengthEqualCount = 0;
        var headerDataThirdColumnEqualCount = 0;
        var headerPrefixEqualCount = 0;
        var pairsWithSharedWords = 0;
        var samples = new List<AudioIndexedPairSample>(Math.Min(sampleLimit, pairCount));

        for (var index = 0; index < pairCount; index++)
        {
            var headerRow = headerTable.Rows[index];
            var dataRow = dataTable.Rows[index];
            headerDataLengthEqualCount += headerRow.SecondColumn == dataRow.SecondColumn ? 1 : 0;
            headerDataThirdColumnEqualCount += headerRow.ThirdColumn == dataRow.ThirdColumn ? 1 : 0;

            var headerReadLength = checked((int)Math.Min(headerRow.SecondColumn, 4096));
            var dataReadLength = checked((int)Math.Min(dataRow.SecondColumn, 64));
            var headerBytes = new byte[headerReadLength];
            var dataBytes = new byte[dataReadLength];
            var headerRead = TryReadAt(headerCompanion, headerRow.FirstColumn, headerBytes);
            var dataRead = TryReadAt(dataCompanion, dataRow.FirstColumn, dataBytes);
            headerReadable &= headerRead;
            dataReadable &= dataRead;
            if (!headerRead || !dataRead)
            {
                continue;
            }

            if (dataBytes.AsSpan().StartsWith("SCHl"u8))
            {
                dataSchlCount++;
            }

            var headerPrefixLength = Math.Min(headerBytes.Length, 16);
            var dataPrefixLength = Math.Min(dataBytes.Length, 16);
            headerPrefixEqualCount += headerBytes.AsSpan(0, headerPrefixLength)
                .SequenceEqual(dataBytes.AsSpan(0, dataPrefixLength)) &&
                headerPrefixLength == dataPrefixLength ? 1 : 0;

            var sharedWords = CountSharedNonZeroAlignedWords(headerBytes, dataBytes);
            pairsWithSharedWords += sharedWords > 0 ? 1 : 0;
            if (samples.Count < sampleLimit)
            {
                samples.Add(new AudioIndexedPairSample(
                    index,
                    headerRow.FirstColumn,
                    headerRow.SecondColumn,
                    dataRow.FirstColumn,
                    dataRow.SecondColumn,
                    Convert.ToHexString(headerBytes.AsSpan(0, headerPrefixLength)),
                    Convert.ToHexString(dataBytes.AsSpan(0, dataPrefixLength)),
                    sharedWords));
            }
        }

        return new AudioIndexedPairComparison(
            pairCount,
            sameRangeCount,
            headerReadable,
            dataReadable,
            dataSchlCount,
            headerDataLengthEqualCount,
            headerDataThirdColumnEqualCount,
            headerPrefixEqualCount,
            pairsWithSharedWords,
            samples);
    }

    public static AudioIndexedSchlComparison CompareIndexedSchlHeaders(
        Stream headerCompanion,
        AudioOffsetTableInspection headerTable,
        Stream dataCompanion,
        AudioOffsetTableInspection dataTable,
        int sampleLimit = 4)
    {
        ArgumentNullException.ThrowIfNull(headerCompanion);
        ArgumentNullException.ThrowIfNull(dataCompanion);
        if (sampleLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sampleLimit));
        }

        var pairCount = Math.Min(headerTable.Rows.Count, dataTable.Rows.Count);
        var envelopeValidCount = 0;
        var ptHeaderCount = 0;
        var platform7Count = 0;
        var metadataHeaderAvailableCount = 0;
        var metadataTerminatedCount = 0;
        var metadataBoundaryViolationCount = 0;
        var parsedCount = 0;
        var parseFailureCount = 0;
        var headerContainsHeaderSizeCount = 0;
        var headerContainsPlatformCount = 0;
        var headerContainsVersionCount = 0;
        var headerContainsCodecCount = 0;
        var headerContainsChannelsCount = 0;
        var headerContainsSampleRateCount = 0;
        var headerContainsDeclaredSamplesCount = 0;
        var parseFailureReasons = new Dictionary<string, int>(StringComparer.Ordinal);
        var platformWordOffsets = new Dictionary<int, int>();
        var versionWordOffsets = new Dictionary<int, int>();
        var samples = new List<AudioIndexedSchlSample>(Math.Min(sampleLimit, pairCount));

        for (var index = 0; index < pairCount; index++)
        {
            var headerRow = headerTable.Rows[index];
            var dataRow = dataTable.Rows[index];
            var headerBytes = new byte[checked((int)Math.Min(headerRow.SecondColumn, 4096))];
            var dataBytes = new byte[checked((int)Math.Min(dataRow.SecondColumn, 4096))];
            if (!TryReadAt(headerCompanion, headerRow.FirstColumn, headerBytes) ||
                !TryReadAt(dataCompanion, dataRow.FirstColumn, dataBytes))
            {
                parseFailureCount++;
                continue;
            }

            var envelope = AudioAssetReader.InspectSchlEnvelope(dataBytes, dataRow.SecondColumn);
            envelopeValidCount += envelope.HasSchlSignature && envelope.HeaderWithinRange ? 1 : 0;
            ptHeaderCount += envelope.HasPtHeader ? 1 : 0;
            platform7Count += envelope.HasPtHeader && envelope.Platform == 7 ? 1 : 0;
            if (envelope.HeaderWithinRange && envelope.HeaderSize > dataBytes.Length &&
                envelope.HeaderSize <= 1024 * 1024)
            {
                var expandedDataBytes = new byte[checked((int)envelope.HeaderSize)];
                if (!TryReadAt(dataCompanion, dataRow.FirstColumn, expandedDataBytes))
                {
                    parseFailureCount++;
                    AddFailureReason(parseFailureReasons, "data_header_read_failed");
                    continue;
                }

                dataBytes = expandedDataBytes;
            }

            var metadataWalk = AudioAssetReader.InspectSchlMetadata(dataBytes, envelope.HeaderSize);
            metadataHeaderAvailableCount += metadataWalk.HeaderFullyAvailable ? 1 : 0;
            metadataTerminatedCount += metadataWalk.ReachedTerminator ? 1 : 0;
            metadataBoundaryViolationCount += metadataWalk.BoundaryViolation ? 1 : 0;

            EaSchlAudioInfo info;
            try
            {
                using var dataPrefix = new MemoryStream(dataBytes, writable: false);
                info = EaSchlAudioDecoder.Inspect(dataPrefix);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                parseFailureCount++;
                AddFailureReason(parseFailureReasons, exception.Message);
                continue;
            }

            parsedCount++;
            var matchesHeaderSize = ContainsAlignedWord(headerBytes, checked((uint)info.HeaderSize));
            var matchesPlatform = ContainsAlignedPositiveInt(headerBytes, info.Platform);
            var matchesVersion = ContainsAlignedPositiveInt(headerBytes, info.Version);
            var matchesCodec = ContainsAlignedPositiveInt(headerBytes, info.Codec);
            var matchesChannels = ContainsAlignedPositiveInt(headerBytes, info.Channels);
            var matchesSampleRate = ContainsAlignedPositiveInt(headerBytes, info.SampleRate);
            var matchesDeclaredSamples = ContainsAlignedWord(headerBytes, info.DeclaredSamples);
            headerContainsHeaderSizeCount += matchesHeaderSize ? 1 : 0;
            headerContainsPlatformCount += matchesPlatform ? 1 : 0;
            headerContainsVersionCount += matchesVersion ? 1 : 0;
            headerContainsCodecCount += matchesCodec ? 1 : 0;
            headerContainsChannelsCount += matchesChannels ? 1 : 0;
            headerContainsSampleRateCount += matchesSampleRate ? 1 : 0;
            headerContainsDeclaredSamplesCount += matchesDeclaredSamples ? 1 : 0;
            if (info.Platform > 0)
            {
                AddWordOffsets(platformWordOffsets, FindAlignedWordOffsets(headerBytes, checked((uint)info.Platform)));
            }
            if (info.Version > 0)
            {
                AddWordOffsets(versionWordOffsets, FindAlignedWordOffsets(headerBytes, checked((uint)info.Version)));
            }

            if (samples.Count < sampleLimit)
            {
                samples.Add(new AudioIndexedSchlSample(
                    index,
                    info.HeaderSize,
                    info.Platform,
                    info.Version,
                    info.Codec,
                    info.Channels,
                    info.SampleRate,
                    info.DeclaredSamples,
                    matchesHeaderSize ? 1 : 0,
                    matchesPlatform ? 1 : 0,
                    matchesVersion ? 1 : 0,
                    matchesCodec ? 1 : 0,
                    matchesChannels ? 1 : 0,
                    matchesSampleRate ? 1 : 0,
                    matchesDeclaredSamples ? 1 : 0,
                    metadataWalk.ReachedTerminator,
                    metadataWalk.BoundaryViolation,
                    metadataWalk.TokenCount,
                    metadataWalk.BoundaryViolationOffset));
            }
        }

        return new AudioIndexedSchlComparison(
            pairCount,
            envelopeValidCount,
            ptHeaderCount,
            platform7Count,
            metadataHeaderAvailableCount,
            metadataTerminatedCount,
            metadataBoundaryViolationCount,
            parsedCount,
            parseFailureCount,
            headerContainsHeaderSizeCount,
            headerContainsPlatformCount,
            headerContainsVersionCount,
            headerContainsCodecCount,
            headerContainsChannelsCount,
            headerContainsSampleRateCount,
            headerContainsDeclaredSamplesCount,
            parseFailureReasons,
            platformWordOffsets,
            versionWordOffsets,
            samples);
    }

    public static AudioEventSchlFieldComparison CompareEventToIndexedSchlFields(
        ReadOnlySpan<byte> eventData,
        Stream dataCompanion,
        AudioOffsetTableInspection dataTable)
    {
        ArgumentNullException.ThrowIfNull(dataCompanion);
        var eventBytes = eventData.ToArray();
        var fields = new Dictionary<string, HashSet<uint>>(StringComparer.Ordinal)
        {
            ["header_size"] = [],
            ["platform"] = [],
            ["version"] = [],
            ["codec"] = [],
            ["channels"] = [],
            ["sample_rate"] = [],
            ["declared_samples"] = [],
        };
        var parsedCount = 0;
        foreach (var row in dataTable.Rows)
        {
            var dataBytes = new byte[checked((int)Math.Min(row.SecondColumn, 4096))];
            if (!TryReadAt(dataCompanion, row.FirstColumn, dataBytes))
            {
                continue;
            }

            var envelope = AudioAssetReader.InspectSchlEnvelope(dataBytes, row.SecondColumn);
            if (!envelope.HasSchlSignature || !envelope.HeaderWithinRange)
            {
                continue;
            }
            if (envelope.HeaderSize > dataBytes.Length && envelope.HeaderSize <= 1024 * 1024)
            {
                var expandedDataBytes = new byte[checked((int)envelope.HeaderSize)];
                if (!TryReadAt(dataCompanion, row.FirstColumn, expandedDataBytes))
                {
                    continue;
                }

                dataBytes = expandedDataBytes;
            }

            try
            {
                using var dataPrefix = new MemoryStream(dataBytes, writable: false);
                var info = EaSchlAudioDecoder.Inspect(dataPrefix);
                parsedCount++;
                AddPositiveCandidate(fields["header_size"], info.HeaderSize);
                AddPositiveCandidate(fields["platform"], info.Platform);
                AddPositiveCandidate(fields["version"], info.Version);
                AddPositiveCandidate(fields["codec"], info.Codec);
                AddPositiveCandidate(fields["channels"], info.Channels);
                AddPositiveCandidate(fields["sample_rate"], info.SampleRate);
                if (info.DeclaredSamples > 0)
                {
                    fields["declared_samples"].Add(info.DeclaredSamples);
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                continue;
            }
        }

        var matches = fields
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair =>
            {
                var rawMatches = FindAlignedRawWordMatches(eventBytes, pair.Value);
                return new AudioEventSchlFieldMatch(
                    pair.Key,
                    pair.Value.Count,
                    rawMatches.Count,
                    rawMatches.Sum(match => match.Count));
            })
            .ToArray();
        return new AudioEventSchlFieldComparison(dataTable.Rows.Count, parsedCount, matches);
    }

    private static IReadOnlyList<uint> ReadPrefixWords(ReadOnlySpan<byte> data, int maximumBytes)
    {
        var wordCount = Math.Min(data.Length, maximumBytes) / sizeof(uint);
        var words = new uint[wordCount];
        for (var index = 0; index < wordCount; index++)
        {
            words[index] = BinaryPrimitives.ReadUInt32LittleEndian(data[(index * sizeof(uint))..]);
        }

        return words;
    }

    private static int CountSignature(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature)
    {
        var count = 0;
        for (var offset = 0; offset <= data.Length - signature.Length; offset++)
        {
            if (data.Slice(offset, signature.Length).SequenceEqual(signature))
            {
                count++;
            }
        }

        return count;
    }

    private static int CountSharedNonZeroAlignedWords(
        ReadOnlySpan<byte> headerData,
        ReadOnlySpan<byte> dataPrefix)
    {
        var headerWords = ReadPrefixWords(headerData, headerData.Length)
            .Where(value => value != 0)
            .ToHashSet();
        var dataWords = ReadPrefixWords(dataPrefix, dataPrefix.Length)
            .Where(value => value != 0)
            .ToHashSet();
        return headerWords.Intersect(dataWords).Count();
    }

    private static bool ContainsAlignedWord(ReadOnlySpan<byte> data, uint candidate)
    {
        if (candidate == 0)
        {
            return false;
        }

        for (var offset = 0; offset + sizeof(uint) <= data.Length; offset += sizeof(uint))
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) == candidate)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAlignedPositiveInt(ReadOnlySpan<byte> data, int candidate) =>
        candidate > 0 && ContainsAlignedWord(data, checked((uint)candidate));

    private static IReadOnlyList<int> FindAlignedWordOffsets(ReadOnlySpan<byte> data, uint candidate)
    {
        if (candidate == 0)
        {
            return Array.Empty<int>();
        }

        var offsets = new List<int>();
        for (var offset = 0; offset + sizeof(uint) <= data.Length; offset += sizeof(uint))
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) == candidate)
            {
                offsets.Add(offset);
            }
        }

        return offsets;
    }

    private static void AddWordOffsets(Dictionary<int, int> counts, IReadOnlyList<int> offsets)
    {
        foreach (var offset in offsets.Distinct())
        {
            counts[offset] = counts.TryGetValue(offset, out var count) ? count + 1 : 1;
        }
    }

    private static void AddPositiveCandidate(HashSet<uint> values, int value)
    {
        if (value > 0)
        {
            values.Add(checked((uint)value));
        }
    }

    private static void AddFailureReason(Dictionary<string, int> reasons, string reason)
    {
        reasons[reason] = reasons.TryGetValue(reason, out var count) ? count + 1 : 1;
    }

    private static IReadOnlyList<AudioEventTextRun> FindTextRuns(ReadOnlySpan<byte> data)
    {
        var runs = new List<AudioEventTextRun>();
        var start = -1;
        for (var offset = 0; offset <= data.Length; offset++)
        {
            var isPrintable = offset < data.Length && data[offset] is >= 0x20 and <= 0x7E;
            if (isPrintable && start < 0)
            {
                start = offset;
            }
            else if (!isPrintable && start >= 0)
            {
                if (offset - start >= 4)
                {
                    runs.Add(new AudioEventTextRun(start, Encoding.ASCII.GetString(data[start..offset])));
                }

                start = -1;
            }
        }

        return runs;
    }

    private static bool TryReadAt(Stream stream, long offset, byte[] buffer)
    {
        if (offset < 0 || offset > stream.Length - buffer.Length)
        {
            return false;
        }

        stream.Seek(offset, SeekOrigin.Begin);
        var read = 0;
        while (read < buffer.Length)
        {
            var chunk = stream.Read(buffer, read, buffer.Length - read);
            if (chunk == 0)
            {
                return false;
            }

            read += chunk;
        }

        return true;
    }
}
