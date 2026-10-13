using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AudioEventDataAssetHashSearchInput(
    string AssetPath,
    long AssetBytes,
    bool Valid,
    IReadOnlyList<AudioAlignedWordSearchResult> Matches,
    string Issue);

public sealed record AudioEventDataAssetHashSearchRow(
    string AssetPath,
    long AssetBytes,
    bool Valid,
    int HashWordsWithHits,
    int AssetHashHitPairs,
    int AlignedOccurrences,
    string HashHitDetails,
    string Status,
    string Issue);

public sealed record AudioEventDataAssetHashSearchSummary(
    string AssetRoot,
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

    public static IReadOnlyList<string> BuildLines(AudioEventDataAssetHashSearchSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("asset_root", summary.AssetRoot),
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

    public static void Write(string path, AudioEventDataAssetHashSearchSummary summary)
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

    private static string Line(string field, int value) =>
        $"{field}\t{value.ToString(CultureInfo.InvariantCulture)}";

    private static string Line(string field, long value) =>
        $"{field}\t{value.ToString(CultureInfo.InvariantCulture)}";

    private static string Line(string field, string value) =>
        $"{field}\t{value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ')}";
}

public sealed record AudioEventDataAssetHashSearchResult(
    IReadOnlyList<AudioEventDataAssetHashSearchRow> Rows,
    AudioEventDataAssetHashSearchSummary Summary);

/// <summary>
/// Re-scans the complete data-file universe, excluding only the source EVT,
/// for the aligned words observed in the EVT string index. Hits are retained
/// as structural observations only; this audit never assigns an event ID,
/// ABKC/AST/BNK sample, gameplay event or playback meaning.
/// </summary>
public static class AudioEventDataAssetHashSearchAudit
{
    public const string AssetRoot = "data";
    public const string EventPath = "data/audio/eventspa.evt";
    public const string Status = "verified-independent-audio-event-data-asset-hash-universe-control";
    public const string InconclusiveStatus = "inconclusive-independent-audio-event-data-asset-hash-universe-control";
    public const string Provenance =
        "eventspa.evt-string-index-words+all-data-assets-excluding-source-EVT;aligned-little-endian-word-reparse;no-selection-promotion";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static AudioEventDataAssetHashSearchResult Audit(IXboxFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        var eventData = ReadBounded(fileSystem.OpenReadOnly(EventPath));
        var candidates = ExtractCandidates(eventData);
        var values = candidates.Select(candidate => candidate.HashWord).ToHashSet();
        var candidateNames = candidates
            .GroupBy(candidate => candidate.HashWord)
            .ToDictionary(group => group.Key, group => group.First().Value);
        var inputs = new List<AudioEventDataAssetHashSearchInput>();
        var excludedSourceRows = 0;
        foreach (var assetPath in fileSystem.Enumerate(AssetRoot)
                     .Where(path => !path.EndsWith("/", StringComparison.Ordinal))
                     .OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            if (string.Equals(assetPath, EventPath, StringComparison.OrdinalIgnoreCase))
            {
                excludedSourceRows++;
                continue;
            }

            try
            {
                using var assetStream = fileSystem.OpenReadOnly(assetPath);
                var matches = AudioAssetReader.SearchAlignedWords(assetStream, values);
                inputs.Add(new AudioEventDataAssetHashSearchInput(
                    assetPath,
                    assetStream.Length,
                    true,
                    matches,
                    string.Empty));
            }
            catch (Exception exception) when (
                exception is IOException or InvalidDataException or EndOfStreamException or OverflowException)
            {
                inputs.Add(new AudioEventDataAssetHashSearchInput(
                    assetPath,
                    0,
                    false,
                    Array.Empty<AudioAlignedWordSearchResult>(),
                    exception.Message));
            }
        }

        return Analyze(candidates.Count, inputs, excludedSourceRows, candidateNames);
    }

    public static AudioEventDataAssetHashSearchResult Analyze(
        int candidateCount,
        IEnumerable<AudioEventDataAssetHashSearchInput> inputs,
        int excludedSourceRows,
        IReadOnlyDictionary<uint, string>? candidateNames = null)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (candidateCount < 0 || excludedSourceRows < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        var rows = inputs
            .OrderBy(input => input.AssetPath, StringComparer.OrdinalIgnoreCase)
            .Select(input =>
            {
                var hashWordsWithHits = input.Matches.Count(match => match.Count > 0);
                var hitPairs = input.Matches.Sum(match => match.Count > 0 ? 1 : 0);
                var occurrences = input.Matches.Sum(match => match.Count);
                var hitDetails = string.Join(
                    "|",
                    input.Matches
                        .Where(match => match.Count > 0)
                        .Select(match =>
                        {
                            var offsets = string.Join(",", match.FirstOffsets.Select(offset => $"0x{offset:X}"));
                            var name = candidateNames is not null &&
                                       candidateNames.TryGetValue(match.Value, out var candidateName)
                                ? $":name={candidateName}"
                                : string.Empty;
                            return $"0x{match.Value:X8}{name}:count={match.Count}:offsets={offsets}";
                        }));
                return new AudioEventDataAssetHashSearchRow(
                    input.AssetPath,
                    input.AssetBytes,
                    input.Valid,
                    hashWordsWithHits,
                    hitPairs,
                    occurrences,
                    hitDetails,
                    input.Valid
                        ? (hashWordsWithHits > 0 ? "aligned-hash-hit" : "no-aligned-hash-hit")
                        : "invalid-asset",
                    input.Issue);
            })
            .ToArray();
        var summary = new AudioEventDataAssetHashSearchSummary(
            AssetRoot,
            EventPath,
            candidateCount,
            rows.Length,
            rows.Count(row => row.Valid),
            rows.Where(row => row.Valid).Sum(row => row.AssetBytes),
            excludedSourceRows,
            rows.Count(row => row.HashWordsWithHits > 0),
            rows.Sum(row => row.AssetHashHitPairs),
            rows.Sum(row => row.AlignedOccurrences),
            candidateCount > 0 &&
            rows.Length > 0 &&
            rows.All(row => row.Valid) &&
            excludedSourceRows == 1
                ? Status
                : InconclusiveStatus,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new AudioEventDataAssetHashSearchResult(rows, summary);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioEventDataAssetHashSearchRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "asset_path",
                "asset_bytes",
                "valid",
                "hash_words_with_hits",
                "asset_hash_hit_pairs",
                "aligned_occurrences",
                "hash_hit_details",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.AssetPath),
            row.AssetBytes.ToString(CultureInfo.InvariantCulture),
            row.Valid.ToString(),
            row.HashWordsWithHits.ToString(CultureInfo.InvariantCulture),
            row.AssetHashHitPairs.ToString(CultureInfo.InvariantCulture),
            row.AlignedOccurrences.ToString(CultureInfo.InvariantCulture),
            Escape(row.HashHitDetails),
            Escape(row.Status),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, AudioEventDataAssetHashSearchSummary summary) =>
        AudioEventDataAssetHashSearchSummary.Write(path, summary);

    private static IReadOnlyList<AudioEventStringIndexEntry> ExtractCandidates(ReadOnlySpan<byte> eventData)
    {
        var evidence = AudioAssetReader.InspectEventFile(eventData);
        var stringTable = evidence.TextRuns.FirstOrDefault(run =>
            run.Value.Contains("panna_goal", StringComparison.Ordinal));
        if (stringTable is null)
        {
            return Array.Empty<AudioEventStringIndexEntry>();
        }

        return AudioAssetReader.InspectStringIndex(eventData, stringTable.Offset)
            .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
            .GroupBy(entry => entry.StringOffset)
            .Select(group => group.First())
            .OrderBy(entry => entry.StringOffset)
            .ToArray();
    }

    private static byte[] ReadBounded(Stream stream)
    {
        using (stream)
        {
            if (stream.Length > 16 * 1024 * 1024)
            {
                throw new InvalidDataException("Event asset exceeds the bounded audit input size.");
            }

            stream.Position = 0;
            var data = new byte[checked((int)stream.Length)];
            stream.ReadExactly(data);
            return data;
        }
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
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
