using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AudioEventAudioAssetHashSearchInput(
    string AssetPath,
    long AssetBytes,
    IReadOnlyList<AudioAlignedWordSearchResult> Matches);

public sealed record AudioEventAudioAssetHashSearchRow(
    string AssetPath,
    long AssetBytes,
    int CandidateCount,
    int AlignedOccurrences,
    int HashWordsWithHits,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics,
    string Issue);

public sealed record AudioEventAudioAssetHashSearchSummary(
    string EventPath,
    int CandidateCount,
    int ScannedAssetCount,
    int ValidAssetCount,
    long ScannedBytes,
    int ExcludedSourceRows,
    int AssetsWithHits,
    int AssetHashHitPairs,
    int AlignedOccurrences,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(AudioEventAudioAssetHashSearchSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("event_path", summary.EventPath),
            Line("candidate_count", summary.CandidateCount),
            Line("scanned_asset_count", summary.ScannedAssetCount),
            Line("valid_asset_count", summary.ValidAssetCount),
            Line("scanned_bytes", summary.ScannedBytes),
            Line("excluded_source_rows", summary.ExcludedSourceRows),
            Line("assets_with_hits", summary.AssetsWithHits),
            Line("asset_hash_hit_pairs", summary.AssetHashHitPairs),
            Line("aligned_occurrences", summary.AlignedOccurrences),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, AudioEventAudioAssetHashSearchSummary summary)
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

public sealed record AudioEventAudioAssetHashSearchResult(
    IReadOnlyList<AudioEventAudioAssetHashSearchRow> Rows,
    AudioEventAudioAssetHashSearchSummary Summary);

/// <summary>
/// Re-scans every file under data/audio except the source eventspa.evt for the
/// aligned EVT string-index words. Raw hits are asset-universe evidence only;
/// this audit never assigns event IDs, samples, gameplay events or playback.
/// </summary>
public static class AudioEventAudioAssetHashSearchAudit
{
    public const string Status = "verified-independent-audio-event-audio-asset-hash-universe-control";
    public const string Provenance =
        "eventspa.evt-string-index-words+all-data/audio-assets-excluding-source-EVT;aligned-little-endian-word-reparse;no-selection-promotion";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";
    public const string EventPath = "data/audio/eventspa.evt";

    public static AudioEventAudioAssetHashSearchResult Audit(IXboxFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        using var eventStream = fileSystem.OpenReadOnly(EventPath);
        var eventData = ReadBounded(eventStream, 16 * 1024 * 1024);
        var candidates = ExtractCandidates(eventData);
        var values = candidates
            .Select(candidate => candidate.HashWord)
            .ToHashSet();
        var inputs = new List<AudioEventAudioAssetHashSearchInput>();
        foreach (var assetPath in fileSystem
                     .Enumerate("data/audio")
                     .Where(path => !string.Equals(path, EventPath, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            using var assetStream = fileSystem.OpenReadOnly(assetPath);
            var matches = AudioAssetReader.SearchAlignedWords(assetStream, values);
            inputs.Add(new AudioEventAudioAssetHashSearchInput(assetPath, assetStream.Length, matches));
        }

        return Analyze(candidates.Count, inputs, excludedSourceRows: 1);
    }

    public static AudioEventAudioAssetHashSearchResult Analyze(
        int candidateCount,
        IEnumerable<AudioEventAudioAssetHashSearchInput> inputs,
        int excludedSourceRows)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (candidateCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateCount));
        }

        var inputRows = inputs.ToArray();
        var duplicatePaths = inputRows
            .GroupBy(input => input.AssetPath, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() != 1);
        var rows = inputRows
            .Select(input =>
            {
                var matches = input.Matches ?? Array.Empty<AudioAlignedWordSearchResult>();
                var alignedOccurrences = matches.Sum(match => match.Count);
                var hashWordsWithHits = matches.Count(match => match.Count > 0);
                return new AudioEventAudioAssetHashSearchRow(
                    input.AssetPath,
                    input.AssetBytes,
                    candidateCount,
                    alignedOccurrences,
                    hashWordsWithHits,
                    Status,
                    DirectSelectionBinding,
                    OriginalSemantics,
                    string.Empty);
            })
            .ToArray();
        var validAssetCount = rows.Count(row =>
            row.AssetBytes >= 0 &&
            row.CandidateCount == candidateCount &&
            row.Status == Status &&
            row.DirectSelectionBinding == DirectSelectionBinding &&
            row.OriginalSemantics == OriginalSemantics &&
            string.IsNullOrEmpty(row.Issue));
        var summary = new AudioEventAudioAssetHashSearchSummary(
            EventPath,
            candidateCount,
            rows.Length,
            validAssetCount,
            rows.Sum(row => row.AssetBytes),
            excludedSourceRows,
            rows.Count(row => row.HashWordsWithHits > 0),
            rows.Sum(row => row.HashWordsWithHits),
            rows.Sum(row => row.AlignedOccurrences),
            !duplicatePaths &&
            excludedSourceRows == 1 &&
            candidateCount > 0 &&
            rows.Length > 0 &&
            validAssetCount == rows.Length
                ? Status
                : "incomplete-" + Status,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new AudioEventAudioAssetHashSearchResult(rows, summary);
    }

    public static void WriteReport(string path, IEnumerable<AudioEventAudioAssetHashSearchRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "asset_path",
                "asset_bytes",
                "candidate_count",
                "aligned_occurrences",
                "hash_words_with_hits",
                "status",
                "direct_selection_binding",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.AssetPath),
            row.AssetBytes.ToString(CultureInfo.InvariantCulture),
            row.CandidateCount.ToString(CultureInfo.InvariantCulture),
            row.AlignedOccurrences.ToString(CultureInfo.InvariantCulture),
            row.HashWordsWithHits.ToString(CultureInfo.InvariantCulture),
            Escape(row.Status),
            Escape(row.DirectSelectionBinding),
            Escape(row.OriginalSemantics),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, AudioEventAudioAssetHashSearchSummary summary) =>
        AudioEventAudioAssetHashSearchSummary.Write(path, summary);

    private static IReadOnlyList<AudioEventStringIndexEntry> ExtractCandidates(ReadOnlySpan<byte> eventData)
    {
        var eventEvidence = AudioAssetReader.InspectEventFile(eventData);
        var tableRun = eventEvidence.TextRuns.FirstOrDefault(run =>
            run.Value.Contains("panna_goal", StringComparison.Ordinal));
        if (tableRun is null)
        {
            throw new InvalidDataException("The EVT string-table marker was not found.");
        }

        return AudioAssetReader.InspectStringIndex(eventData, tableRun.Offset)
            .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
            .GroupBy(entry => entry.StringOffset)
            .Select(group => group.First())
            .OrderBy(entry => entry.StringOffset)
            .ToArray();
    }

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException("The bounded EVT input exceeds the audit limit.");
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

        File.WriteAllLines(fullPath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
