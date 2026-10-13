using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AudioEventXbeHashSearchRow(
    string EventName,
    uint HashWord,
    int AlignedOccurrenceCount,
    int UnalignedOccurrenceCount,
    string AlignedFirstOffsets,
    string UnalignedFirstOffsets,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record AudioEventXbeHashSearchSummary(
    int CandidateCount,
    int UniqueEventNames,
    int UniqueHashWords,
    int XbeBytes,
    int AlignedOccurrences,
    int UnalignedOccurrences,
    int HashWordsWithAlignedHits,
    int HashWordsWithUnalignedHits,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(AudioEventXbeHashSearchSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("candidate_count", summary.CandidateCount),
            Line("unique_event_names", summary.UniqueEventNames),
            Line("unique_hash_words", summary.UniqueHashWords),
            Line("xbe_bytes", summary.XbeBytes),
            Line("aligned_occurrences", summary.AlignedOccurrences),
            Line("unaligned_occurrences", summary.UnalignedOccurrences),
            Line("hash_words_with_aligned_hits", summary.HashWordsWithAlignedHits),
            Line("hash_words_with_unaligned_hits", summary.HashWordsWithUnalignedHits),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, AudioEventXbeHashSearchSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            BuildLines(summary),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Line<T>(string field, T value) =>
        $"{field}\t{Convert.ToString(value, CultureInfo.InvariantCulture)}";
}

public sealed record AudioEventXbeHashSearchResult(
    IReadOnlyList<AudioEventXbeHashSearchRow> Rows,
    AudioEventXbeHashSearchSummary Summary);

/// <summary>
/// Searches the retail XBE bytes for the bounded EVT string-index words.
/// Occurrences are raw binary evidence only: this control does not recover
/// functions, execute the XBE, assign IDs, select ABKC samples, or claim
/// gameplay/audio semantics.
/// </summary>
public static class AudioEventXbeHashSearchAudit
{
    public const string Status = "verified-structural-xbe-hash-search";
    public const string Provenance =
        "eventspa.evt-string-index-words+default.xbe-aligned-and-byte-search;no-execution;no-binding";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";
    private const string EventPath = "data/audio/eventspa.evt";

    public static AudioEventXbeHashSearchResult Audit(IXboxFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        using var eventStream = fileSystem.OpenReadOnly(EventPath);
        using var xbeStream = fileSystem.OpenReadOnly("default.xbe");
        var eventData = ReadBounded(eventStream, 16 * 1024 * 1024);
        var xbeData = ReadBounded(xbeStream, 64 * 1024 * 1024);
        return Analyze(eventData, xbeData);
    }

    public static AudioEventXbeHashSearchResult Analyze(
        ReadOnlySpan<byte> eventData,
        ReadOnlySpan<byte> xbeData)
    {
        var eventEvidence = AudioAssetReader.InspectEventFile(eventData);
        var tableRun = eventEvidence.TextRuns.FirstOrDefault(run =>
            run.Value.Contains("panna_goal", StringComparison.Ordinal));
        if (tableRun is null)
        {
            throw new InvalidDataException("The EVT string-table marker was not found.");
        }

        var candidates = AudioAssetReader.InspectStringIndex(eventData, tableRun.Offset)
            .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
            .GroupBy(entry => entry.StringOffset)
            .Select(group => group.First())
            .OrderBy(entry => entry.StringOffset)
            .ToArray();
        var xbeBytes = xbeData.ToArray();
        var rows = candidates
            .Select(candidate =>
            {
                var aligned = FindOccurrences(xbeBytes, candidate.HashWord, alignedOnly: true);
                var unaligned = FindOccurrences(xbeBytes, candidate.HashWord, alignedOnly: false);
                return new AudioEventXbeHashSearchRow(
                    candidate.Value,
                    candidate.HashWord,
                    aligned.Count,
                    unaligned.Count,
                    FormatOffsets(aligned),
                    FormatOffsets(unaligned),
                    Status,
                    DirectSelectionBinding,
                    OriginalSemantics);
            })
            .ToArray();
        var summary = new AudioEventXbeHashSearchSummary(
            rows.Length,
            rows.Select(row => row.EventName).Distinct(StringComparer.Ordinal).Count(),
            rows.Select(row => row.HashWord).Distinct().Count(),
            xbeBytes.Length,
            rows.Sum(row => row.AlignedOccurrenceCount),
            rows.Sum(row => row.UnalignedOccurrenceCount),
            rows.Count(row => row.AlignedOccurrenceCount > 0),
            rows.Count(row => row.UnalignedOccurrenceCount > 0),
            rows.Length == candidates.Length ? Status : "incomplete-structural-xbe-hash-search",
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new AudioEventXbeHashSearchResult(rows, summary);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioEventXbeHashSearchRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "event_name",
                "hash_word",
                "aligned_occurrence_count",
                "unaligned_occurrence_count",
                "aligned_first_offsets",
                "unaligned_first_offsets",
                "status",
                "direct_selection_binding",
                "original_semantics",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.EventName),
            $"0x{row.HashWord:X8}",
            row.AlignedOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            row.UnalignedOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            Escape(row.AlignedFirstOffsets),
            Escape(row.UnalignedFirstOffsets),
            Escape(row.Status),
            Escape(row.DirectSelectionBinding),
            Escape(row.OriginalSemantics),
        })));
        WriteLines(path, lines);
    }

    private static IReadOnlyList<int> FindOccurrences(
        ReadOnlySpan<byte> data,
        uint value,
        bool alignedOnly)
    {
        var firstOffsets = new List<int>();
        var step = alignedOnly ? sizeof(uint) : 1;
        for (var offset = 0; offset + sizeof(uint) <= data.Length; offset += step)
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(data[offset..]) == value)
            {
                firstOffsets.Add(offset);
            }
        }

        return firstOffsets;
    }

    private static string FormatOffsets(IReadOnlyList<int> offsets) =>
        string.Join(",", offsets.Take(16).Select(offset => $"0x{offset:X}"));

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException("The bounded XBE/EVT input exceeds the audit limit.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return data;
    }

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
