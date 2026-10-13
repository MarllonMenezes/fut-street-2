using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record Fifast2CsiNameRecord(
    int RecordIndex,
    int RecordOffset,
    uint NameOffset,
    uint OpaqueWord,
    uint ReservedWord,
    string Name,
    bool Valid);

public sealed record Fifast2CsiAuditSummary(
    int AssetLength,
    string Magic,
    uint HeaderWordAt4,
    uint HeaderWordAt8,
    uint HeaderWordAt10,
    ushort HeaderCandidateCount,
    ushort HeaderAuxiliaryCount,
    int CandidateRecordOffset,
    int CandidateRecordStride,
    int CandidateRecordCount,
    int ValidRecordCount,
    int UniqueNameCount,
    int NameTableOffset,
    int OpaqueWordsWithinAsset,
    int ReservedZeroCount,
    int OpaqueWordsNonDecreasingPairs,
    int EventTextNameCount,
    int EventExactNameOverlapCount,
    int EventSubstringOverlapCount,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record Fifast2CsiAuditResult(
    IReadOnlyList<Fifast2CsiNameRecord> Rows,
    Fifast2CsiAuditSummary Summary);

/// <summary>
/// Reads only the repeatable MOIR/name-index structure observed in
/// data/audio/FIFAST2.csi. The words after each name offset remain opaque;
/// this reader does not assign trigger, event, sample, bank or playback
/// semantics to them.
/// </summary>
public static class Fifast2CsiReader
{
    private const int HeaderSize = 0x14;
    private const int CandidateRecordOffset = 0x2C;
    private const int CandidateRecordStride = 12;

    public static Fifast2CsiAuditResult Audit(
        ReadOnlySpan<byte> data,
        IReadOnlyCollection<string>? eventTextNames = null)
    {
        if (data.Length < HeaderSize || !data[..4].SequenceEqual("MOIR"u8))
        {
            throw new InvalidDataException("Unsupported FIFAST2 CSI asset.");
        }

        var headerCandidateCount = BinaryPrimitives.ReadUInt16LittleEndian(data[0x0C..0x0E]);
        var headerAuxiliaryCount = BinaryPrimitives.ReadUInt16LittleEndian(data[0x0E..0x10]);
        if (headerCandidateCount == 0 ||
            CandidateRecordOffset + (long)headerCandidateCount * CandidateRecordStride > data.Length)
        {
            throw new InvalidDataException("FIFAST2 CSI name-index candidate range is unavailable.");
        }

        var rows = new List<Fifast2CsiNameRecord>(headerCandidateCount);
        for (var index = 0; index < headerCandidateCount; index++)
        {
            var recordOffset = checked(CandidateRecordOffset + index * CandidateRecordStride);
            var nameOffset = BinaryPrimitives.ReadUInt32LittleEndian(data[recordOffset..]);
            var opaqueWord = BinaryPrimitives.ReadUInt32LittleEndian(data[(recordOffset + 4)..]);
            var reservedWord = BinaryPrimitives.ReadUInt32LittleEndian(data[(recordOffset + 8)..]);
            var valid = TryReadAsciiName(data, nameOffset, out var name);
            rows.Add(new Fifast2CsiNameRecord(
                index,
                recordOffset,
                nameOffset,
                opaqueWord,
                reservedWord,
                name,
                valid));
        }

        var assetLength = data.Length;
        var validRows = rows.Where(row => row.Valid).ToArray();
        var uniqueNames = validRows
            .Select(row => row.Name)
            .Distinct(StringComparer.Ordinal)
            .Count();
        var nameTableOffset = validRows.Length == 0
            ? -1
            : checked((int)validRows.Min(row => row.NameOffset));
        var opaqueWordsWithinAsset = rows.Count(row => row.OpaqueWord < assetLength);
        var reservedZeroCount = rows.Count(row => row.ReservedWord == 0);
        var opaqueWordsNonDecreasingPairs = rows
            .Zip(rows.Skip(1), (previous, current) => current.OpaqueWord >= previous.OpaqueWord)
            .Count(isNonDecreasing => isNonDecreasing);

        var eventNames = (eventTextNames ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var eventNameSet = eventNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var exactOverlap = validRows.Count(row => eventNameSet.Contains(row.Name));
        var substringOverlap = validRows.Count(row =>
            eventNames.Any(eventName =>
                row.Name.Contains(eventName, StringComparison.OrdinalIgnoreCase) ||
                eventName.Contains(row.Name, StringComparison.OrdinalIgnoreCase)));

        var status = validRows.Length == rows.Count &&
                     uniqueNames == validRows.Length &&
                     reservedZeroCount == rows.Count
            ? "verified-structural-fifast2-csi-name-index-candidate"
            : "partial-structural-fifast2-csi-name-index-candidate";
        var summary = new Fifast2CsiAuditSummary(
            data.Length,
            "MOIR",
            BinaryPrimitives.ReadUInt32LittleEndian(data[4..8]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[8..12]),
            BinaryPrimitives.ReadUInt32LittleEndian(data[0x10..0x14]),
            headerCandidateCount,
            headerAuxiliaryCount,
            CandidateRecordOffset,
            CandidateRecordStride,
            rows.Count,
            validRows.Length,
            uniqueNames,
            nameTableOffset,
            opaqueWordsWithinAsset,
            reservedZeroCount,
            opaqueWordsNonDecreasingPairs,
            eventNames.Length,
            exactOverlap,
            substringOverlap,
            status,
            "not-proven",
            "not-claimed");
        return new Fifast2CsiAuditResult(rows, summary);
    }

    public static void WriteReport(string path, Fifast2CsiAuditResult result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(result);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string>
        {
            "record_index\trecord_offset\tname_offset\topaque_word\treserved_word\tname\tvalid",
        };
        lines.AddRange(result.Rows.Select(row => string.Join('\t', new[]
        {
            row.RecordIndex.ToString(CultureInfo.InvariantCulture),
            $"0x{row.RecordOffset:X}",
            $"0x{row.NameOffset:X}",
            $"0x{row.OpaqueWord:X8}",
            $"0x{row.ReservedWord:X8}",
            Escape(row.Name),
            row.Valid.ToString(),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static void WriteSummary(string path, Fifast2CsiAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var fields = new (string Name, string Value)[]
        {
            ("asset_length", summary.AssetLength.ToString(CultureInfo.InvariantCulture)),
            ("magic", summary.Magic),
            ("header_word_at_4", $"0x{summary.HeaderWordAt4:X8}"),
            ("header_word_at_8", $"0x{summary.HeaderWordAt8:X8}"),
            ("header_word_at_10", $"0x{summary.HeaderWordAt10:X8}"),
            ("header_candidate_count", summary.HeaderCandidateCount.ToString(CultureInfo.InvariantCulture)),
            ("header_auxiliary_count", summary.HeaderAuxiliaryCount.ToString(CultureInfo.InvariantCulture)),
            ("candidate_record_offset", $"0x{summary.CandidateRecordOffset:X}"),
            ("candidate_record_stride", summary.CandidateRecordStride.ToString(CultureInfo.InvariantCulture)),
            ("candidate_record_count", summary.CandidateRecordCount.ToString(CultureInfo.InvariantCulture)),
            ("valid_record_count", summary.ValidRecordCount.ToString(CultureInfo.InvariantCulture)),
            ("unique_name_count", summary.UniqueNameCount.ToString(CultureInfo.InvariantCulture)),
            ("name_table_offset", $"0x{summary.NameTableOffset:X}"),
            ("opaque_words_within_asset", summary.OpaqueWordsWithinAsset.ToString(CultureInfo.InvariantCulture)),
            ("reserved_zero_count", summary.ReservedZeroCount.ToString(CultureInfo.InvariantCulture)),
            ("opaque_words_non_decreasing_pairs", summary.OpaqueWordsNonDecreasingPairs.ToString(CultureInfo.InvariantCulture)),
            ("event_text_name_count", summary.EventTextNameCount.ToString(CultureInfo.InvariantCulture)),
            ("event_exact_name_overlap_count", summary.EventExactNameOverlapCount.ToString(CultureInfo.InvariantCulture)),
            ("event_substring_overlap_count", summary.EventSubstringOverlapCount.ToString(CultureInfo.InvariantCulture)),
            ("status", summary.Status),
            ("direct_selection_binding", summary.DirectSelectionBinding),
            ("original_semantics", summary.OriginalSemantics),
        };
        File.WriteAllLines(
            path,
            new[] { "field\tvalue" }.Concat(fields.Select(field => $"{field.Name}\t{Escape(field.Value)}")),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool TryReadAsciiName(ReadOnlySpan<byte> data, uint offset, out string name)
    {
        name = string.Empty;
        if (offset >= data.Length)
        {
            return false;
        }

        var start = checked((int)offset);
        var cursor = start;
        while (cursor < data.Length && data[cursor] is >= 0x20 and <= 0x7E)
        {
            cursor++;
        }

        if (cursor == start || cursor >= data.Length || data[cursor] != 0)
        {
            return false;
        }

        name = Encoding.ASCII.GetString(data[start..cursor]);
        return true;
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
