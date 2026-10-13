using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AudioEventNameTextSearchPattern(
    string Name,
    int RawOccurrences,
    IReadOnlyList<long> FirstOffsets);

public sealed record AudioEventNameTextSearchInput(
    string AssetPath,
    long AssetBytes,
    bool Valid,
    IReadOnlyList<AudioEventNameTextSearchPattern> Patterns,
    string Issue);

public sealed record AudioEventNameTextSearchRow(
    string AssetPath,
    long AssetBytes,
    string EventName,
    int RawOccurrences,
    string FirstOffsets,
    bool Valid,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics,
    string Issue);

public sealed record AudioEventNameTextSearchSummary(
    string EventPath,
    int CandidateCount,
    int ScannedAssetCount,
    int ValidAssetCount,
    long ScannedBytes,
    int ExcludedSourceRows,
    int AssetNameHitPairs,
    int AssetsWithNameHits,
    int RawOccurrences,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(AudioEventNameTextSearchSummary summary)
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
            Line("asset_name_hit_pairs", summary.AssetNameHitPairs),
            Line("assets_with_name_hits", summary.AssetsWithNameHits),
            Line("raw_occurrences", summary.RawOccurrences),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, AudioEventNameTextSearchSummary summary)
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

public sealed record AudioEventNameTextSearchResult(
    IReadOnlyList<AudioEventNameTextSearchRow> Rows,
    AudioEventNameTextSearchSummary Summary);

/// <summary>
/// Searches every audio asset for the exact printable names recovered from
/// the named EVT index. A text occurrence is only a namespace observation: it
/// never selects an ABKC/AST/BNK sample and never claims original playback.
/// </summary>
public static class AudioEventNameAssetTextSearchAudit
{
    public const string EventPath = "data/audio/eventspa.evt";
    public const string Status = "verified-independent-audio-event-name-asset-text-search-control";
    public const string Provenance =
        "named-EVT-index+all-data/audio-assets;streaming-exact-ASCII-name-search;no-selection-promotion";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static AudioEventNameTextSearchResult Audit(IXboxFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        using var eventStream = fileSystem.OpenReadOnly(EventPath);
        var eventData = ReadBounded(eventStream, 16 * 1024 * 1024);
        var candidates = ExtractCandidates(eventData);
        var names = candidates
            .Select(candidate => candidate)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var inputs = new List<AudioEventNameTextSearchInput>();
        var excludedSourceRows = 0;
        foreach (var assetPath in fileSystem
                     .Enumerate("data/audio")
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
                inputs.Add(new AudioEventNameTextSearchInput(
                    assetPath,
                    assetStream.Length,
                    true,
                    Search(assetStream, names),
                    string.Empty));
            }
            catch (Exception exception) when (
                exception is IOException or InvalidDataException or EndOfStreamException or OverflowException)
            {
                inputs.Add(new AudioEventNameTextSearchInput(
                    assetPath,
                    0,
                    false,
                    Array.Empty<AudioEventNameTextSearchPattern>(),
                    exception.Message));
            }
        }

        return Analyze(names.Length, inputs, excludedSourceRows);
    }

    public static AudioEventNameTextSearchResult Analyze(
        int candidateCount,
        IEnumerable<AudioEventNameTextSearchInput> inputs,
        int excludedSourceRows)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        if (candidateCount < 0 || excludedSourceRows < 0)
        {
            throw new ArgumentOutOfRangeException();
        }

        var inputRows = inputs
            .OrderBy(input => input.AssetPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var duplicatePaths = inputRows
            .GroupBy(input => input.AssetPath, StringComparer.OrdinalIgnoreCase)
            .Any(group => group.Count() != 1);
        var rows = inputRows
            .SelectMany(input => input.Patterns
                .OrderBy(pattern => pattern.Name, StringComparer.Ordinal)
                .Select(pattern => new AudioEventNameTextSearchRow(
                    input.AssetPath,
                    input.AssetBytes,
                    pattern.Name,
                    pattern.RawOccurrences,
                    string.Join(",", pattern.FirstOffsets.Select(offset => $"0x{offset:X}")),
                    input.Valid,
                    input.Valid
                        ? (pattern.RawOccurrences > 0 ? "raw-name-hit" : "no-raw-name-hit")
                        : "invalid-asset",
                    DirectSelectionBinding,
                    OriginalSemantics,
                    input.Issue)))
            .ToArray();
        var validAssetCount = inputRows.Count(input =>
            input.Valid &&
            input.AssetBytes >= 0 &&
            input.Patterns.Count == candidateCount &&
            input.Patterns.Select(pattern => pattern.Name).Distinct(StringComparer.Ordinal).Count() == candidateCount);
        var assetNameHitPairs = rows.Count(row => row.Valid && row.RawOccurrences > 0);
        var assetsWithNameHits = rows
            .Where(row => row.Valid && row.RawOccurrences > 0)
            .Select(row => row.AssetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var summary = new AudioEventNameTextSearchSummary(
            EventPath,
            candidateCount,
            inputRows.Length,
            validAssetCount,
            inputRows.Where(input => input.Valid).Sum(input => input.AssetBytes),
            excludedSourceRows,
            assetNameHitPairs,
            assetsWithNameHits,
            rows.Where(row => row.Valid).Sum(row => row.RawOccurrences),
            !duplicatePaths &&
            excludedSourceRows == 1 &&
            candidateCount > 0 &&
            inputRows.Length > 0 &&
            validAssetCount == inputRows.Length
                ? Status
                : "incomplete-" + Status,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new AudioEventNameTextSearchResult(rows, summary);
    }

    public static void WriteReport(string path, IEnumerable<AudioEventNameTextSearchRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "asset_path",
                "asset_bytes",
                "event_name",
                "raw_occurrences",
                "first_offsets",
                "valid",
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
            Escape(row.EventName),
            row.RawOccurrences.ToString(CultureInfo.InvariantCulture),
            Escape(row.FirstOffsets),
            row.Valid.ToString(),
            Escape(row.Status),
            Escape(row.DirectSelectionBinding),
            Escape(row.OriginalSemantics),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, AudioEventNameTextSearchSummary summary) =>
        AudioEventNameTextSearchSummary.Write(path, summary);

    internal static IReadOnlyList<AudioEventNameTextSearchPattern> Search(
        Stream source,
        IReadOnlyList<string> names,
        int chunkBytes = 1024 * 1024)
    {
        if (chunkBytes < 1 || chunkBytes > 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(chunkBytes));
        var patterns = names.ToDictionary(
            name => name,
            name => Encoding.ASCII.GetBytes(name),
            StringComparer.Ordinal);
        var counts = names.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
        var offsets = names.ToDictionary(name => name, _ => new List<long>(), StringComparer.Ordinal);
        var maximumPatternLength = patterns.Values.Select(value => value.Length).DefaultIfEmpty(1).Max();
        var pending = Array.Empty<byte>();
        var buffer = new byte[chunkBytes];
        void Scan(byte[] bytes, int stableLimit, string name, byte[] pattern, long baseOffset)
        {
            var offset = 0;
            while (offset < stableLimit)
            {
                // Byte-span IndexOf uses the runtime's optimized exact search.
                // Advance by one byte so overlapping occurrences remain counted.
                var relative = bytes.AsSpan(offset).IndexOf(pattern);
                if (relative < 0) break;
                offset += relative;
                if (offset >= stableLimit) break;
                counts[name]++;
                if (offsets[name].Count < 8) offsets[name].Add(baseOffset + offset);
                offset++;
            }
        }
        long processed = 0;
        while (true)
        {
            var read = source.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            var combined = new byte[pending.Length + read];
            pending.CopyTo(combined, 0);
            buffer.AsSpan(0, read).CopyTo(combined.AsSpan(pending.Length));
            var baseOffset = processed - pending.Length;
            var stableBoundary = combined.Length - Math.Min(maximumPatternLength - 1, combined.Length);
            foreach (var pair in patterns)
            {
                Scan(combined, stableBoundary, pair.Key, pair.Value, baseOffset);
            }

            var pendingLength = Math.Min(maximumPatternLength - 1, combined.Length);
            pending = combined[^pendingLength..].ToArray();
            processed += read;
        }

        var finalBaseOffset = processed - pending.Length;
        foreach (var pair in patterns)
        {
            Scan(pending, pending.Length, pair.Key, pair.Value, finalBaseOffset);
        }

        return names
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name => new AudioEventNameTextSearchPattern(name, counts[name], offsets[name]))
            .ToArray();
    }

    private static IReadOnlyList<string> ExtractCandidates(ReadOnlySpan<byte> eventData)
    {
        var evidence = AudioAssetReader.InspectEventFile(eventData);
        var tableRun = evidence.TextRuns.FirstOrDefault(run =>
            run.Value.Contains("panna_goal", StringComparison.Ordinal));
        if (tableRun is null)
        {
            return Array.Empty<string>();
        }

        return AudioAssetReader.InspectStringIndex(eventData, tableRun.Offset)
            .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
            .GroupBy(entry => entry.StringOffset)
            .Select(group => group.First())
            .Select(entry => entry.Value)
            .ToArray();
    }

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        using (stream)
        {
            if (stream.Length > maximumBytes)
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

        File.WriteAllLines(fullPath, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
