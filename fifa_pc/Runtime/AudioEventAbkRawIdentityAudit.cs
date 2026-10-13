using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioEventAbkRawIdentityRow(
    string Kind,
    string SourcePath,
    string NameOrKey,
    int RowOrIndex,
    long Offset,
    long Length,
    string RawSha256,
    string RawPrefixSha256,
    bool SchlSignature,
    int FullHashMatchCount,
    string FullHashMatchPaths,
    string Status,
    string Issue);

public sealed record AudioEventAbkRawIdentitySummary(
    int EventCandidates,
    int EventRawRows,
    int EventDistinctRawHashes,
    int BankCount,
    int BankNonDummySoundEntries,
    int BankAstRangeRows,
    int BankAstSchlRows,
    int FullRawHashPairs,
    int EventHashesWithFullRawMatch,
    int RawPrefixPairs,
    int RejectedRows,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record AudioEventAbkRawIdentityResult(
    IReadOnlyList<AudioEventAbkRawIdentityRow> Rows,
    AudioEventAbkRawIdentitySummary Summary);

/// <summary>
/// Compares the raw bytes of the named EVT data ranges with every bounded
/// SCHl range exposed by the observed ABKC/AST chain. Exact raw equality is
/// payload-reuse evidence only; this audit never assigns an event selector,
/// sample index, gameplay route or original playback meaning.
/// </summary>
public static class AudioEventAbkRawIdentityAudit
{
    public const string Status =
        "verified-bounded-audio-event-abk-raw-identity-negative-control";
    public const string Provenance =
        "same-ISO-read-only+named-EVT-parallel-data-ranges+validated-ABKC-SoundEntries+AST-SCHl-ranges;full-raw-hash-and-prefix-controls;no-selection-promotion";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static AudioEventAbkRawIdentityResult Audit(
        GameAssetCatalog catalog,
        Func<string, Stream> opener)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);

        var astRows = new List<AudioEventAbkRawIdentityRow>();
        var rejectedRows = 0;
        var bankNonDummySoundEntries = 0;
        foreach (var bank in catalog.AudioBanks.OrderBy(
                     item => item.Path,
                     StringComparer.OrdinalIgnoreCase))
        {
            bankNonDummySoundEntries += bank.SoundEntries.Count;
            if (string.IsNullOrWhiteSpace(bank.AstPath))
            {
                continue;
            }

            foreach (var range in bank.AstRanges
                         .OrderBy(item => item.Offset)
                         .ThenBy(item => item.SoundIndex)
                         .ThenBy(item => item.LoopSegment))
            {
                var key = $"{bank.Path}#sound={range.SoundIndex.ToString(CultureInfo.InvariantCulture)}#loop={range.LoopSegment.ToString()}";
                if (range.Length < 8 || range.Length > int.MaxValue)
                {
                    rejectedRows++;
                    astRows.Add(new AudioEventAbkRawIdentityRow(
                        "ast-range",
                        bank.AstPath,
                        key,
                        range.SoundIndex,
                        range.Offset,
                        range.Length,
                        string.Empty,
                        string.Empty,
                        false,
                        0,
                        string.Empty,
                        "rejected",
                        "AST range is outside the bounded raw-identity policy"));
                    continue;
                }

                try
                {
                    using var source = opener(bank.AstPath);
                    var raw = ReadRange(source, range.Offset, range.Length);
                    var prefixLength = Math.Min(16, raw.Length);
                    astRows.Add(new AudioEventAbkRawIdentityRow(
                        "ast-range",
                        bank.AstPath,
                        key,
                        range.SoundIndex,
                        range.Offset,
                        range.Length,
                        Sha256(raw),
                        Sha256(raw.AsSpan(0, prefixLength)),
                        raw.AsSpan().StartsWith("SCHl"u8),
                        0,
                        string.Empty,
                        "raw-range-validated",
                        string.Empty));
                }
                catch (Exception exception) when (
                    exception is IOException or InvalidDataException or EndOfStreamException or
                    OverflowException or ArgumentOutOfRangeException)
                {
                    rejectedRows++;
                    astRows.Add(new AudioEventAbkRawIdentityRow(
                        "ast-range",
                        bank.AstPath,
                        key,
                        range.SoundIndex,
                        range.Offset,
                        range.Length,
                        string.Empty,
                        string.Empty,
                        false,
                        0,
                        string.Empty,
                        "rejected",
                        exception.Message));
                }
            }
        }

        var validatedAstRows = astRows
            .Where(row => row.RawSha256.Length > 0)
            .ToArray();
        var astByHash = validatedAstRows
            .GroupBy(row => row.RawSha256, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(row => row.SourcePath + "#" + row.NameOrKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var astByPrefix = validatedAstRows
            .GroupBy(row => row.RawPrefixSha256, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Count(),
                StringComparer.OrdinalIgnoreCase);

        var eventRows = new List<AudioEventAbkRawIdentityRow>();
        foreach (var candidate in catalog.AudioEventCandidates
                     .OrderBy(item => item.EventPath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Candidate.HeaderRowIndex)
                     .ThenBy(item => item.Candidate.EventName, StringComparer.OrdinalIgnoreCase))
        {
            var dataRow = candidate.Candidate.ParallelDataRow;
            if (dataRow is null || dataRow.SecondColumn < 8 || dataRow.SecondColumn > int.MaxValue)
            {
                rejectedRows++;
                eventRows.Add(new AudioEventAbkRawIdentityRow(
                    "event",
                    candidate.EventPath,
                    candidate.Candidate.EventName,
                    candidate.Candidate.HeaderRowIndex,
                    dataRow?.FirstColumn ?? -1,
                    dataRow?.SecondColumn ?? 0,
                    string.Empty,
                    string.Empty,
                    false,
                    0,
                    string.Empty,
                    "rejected",
                    "named EVT candidate has no bounded raw data range"));
                continue;
            }

            try
            {
                using var source = opener(candidate.DataPath);
                var raw = ReadRange(source, dataRow.FirstColumn, dataRow.SecondColumn);
                var rawHash = Sha256(raw);
                var prefixLength = Math.Min(16, raw.Length);
                var prefixHash = Sha256(raw.AsSpan(0, prefixLength));
                var matches = astByHash.TryGetValue(rawHash, out var matchingRanges)
                    ? matchingRanges
                    : Array.Empty<string>();
                eventRows.Add(new AudioEventAbkRawIdentityRow(
                    "event",
                    candidate.DataPath,
                    candidate.Candidate.EventName,
                    candidate.Candidate.HeaderRowIndex,
                    dataRow.FirstColumn,
                    dataRow.SecondColumn,
                    rawHash,
                    prefixHash,
                    raw.AsSpan().StartsWith("SCHl"u8),
                    matches.Length,
                    string.Join('|', matches),
                    matches.Length == 0
                        ? "raw-range-validated-no-full-match"
                        : "raw-range-validated-full-match-observation",
                    string.Empty));
            }
            catch (Exception exception) when (
                exception is IOException or InvalidDataException or EndOfStreamException or
                OverflowException or ArgumentOutOfRangeException)
            {
                rejectedRows++;
                eventRows.Add(new AudioEventAbkRawIdentityRow(
                    "event",
                    candidate.DataPath,
                    candidate.Candidate.EventName,
                    candidate.Candidate.HeaderRowIndex,
                    dataRow.FirstColumn,
                    dataRow.SecondColumn,
                    string.Empty,
                    string.Empty,
                    false,
                    0,
                    string.Empty,
                    "rejected",
                    exception.Message));
            }
        }

        var validatedEvents = eventRows
            .Where(row => row.RawSha256.Length > 0)
            .ToArray();
        var fullHashPairs = validatedEvents.Sum(row => row.FullHashMatchCount);
        var prefixPairs = validatedEvents.Sum(row =>
            astByPrefix.TryGetValue(row.RawPrefixSha256, out var count) ? count : 0);
        var summary = new AudioEventAbkRawIdentitySummary(
            catalog.AudioEventCandidates.Count,
            validatedEvents.Length,
            validatedEvents.Select(row => row.RawSha256)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            catalog.AudioBanks.Count,
            bankNonDummySoundEntries,
            validatedAstRows.Length,
            validatedAstRows.Count(row => row.SchlSignature),
            fullHashPairs,
            validatedEvents.Count(row => row.FullHashMatchCount > 0),
            prefixPairs,
            rejectedRows,
            rejectedRows == 0 &&
            validatedEvents.Length == catalog.AudioEventCandidates.Count &&
            catalog.AudioBanks.Count > 0 &&
            validatedAstRows.All(row => row.SchlSignature)
                ? Status
                : "incomplete-" + Status,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);

        return new AudioEventAbkRawIdentityResult(
            eventRows.Concat(astRows).ToArray(),
            summary);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioEventAbkRawIdentityRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            "kind\tsource_path\tname_or_key\trow_or_index\toffset\tlength\traw_sha256\traw_prefix_sha256\tschl_signature\tfull_hash_match_count\tfull_hash_match_paths\tstatus\tissue",
        };
        lines.AddRange(rows.Select(row => string.Join('\t', new[]
        {
            Field(row.Kind),
            Field(row.SourcePath),
            Field(row.NameOrKey),
            row.RowOrIndex.ToString(CultureInfo.InvariantCulture),
            row.Offset.ToString(CultureInfo.InvariantCulture),
            row.Length.ToString(CultureInfo.InvariantCulture),
            row.RawSha256,
            row.RawPrefixSha256,
            row.SchlSignature.ToString(),
            row.FullHashMatchCount.ToString(CultureInfo.InvariantCulture),
            Field(row.FullHashMatchPaths),
            Field(row.Status),
            Field(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, AudioEventAbkRawIdentitySummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var fields = new (string Name, string Value)[]
        {
            ("event_candidates", summary.EventCandidates.ToString(CultureInfo.InvariantCulture)),
            ("event_raw_rows", summary.EventRawRows.ToString(CultureInfo.InvariantCulture)),
            ("event_distinct_raw_hashes", summary.EventDistinctRawHashes.ToString(CultureInfo.InvariantCulture)),
            ("bank_count", summary.BankCount.ToString(CultureInfo.InvariantCulture)),
            ("bank_non_dummy_sound_entries", summary.BankNonDummySoundEntries.ToString(CultureInfo.InvariantCulture)),
            ("bank_ast_range_rows", summary.BankAstRangeRows.ToString(CultureInfo.InvariantCulture)),
            ("bank_ast_schl_rows", summary.BankAstSchlRows.ToString(CultureInfo.InvariantCulture)),
            ("full_raw_hash_pairs", summary.FullRawHashPairs.ToString(CultureInfo.InvariantCulture)),
            ("event_hashes_with_full_raw_match", summary.EventHashesWithFullRawMatch.ToString(CultureInfo.InvariantCulture)),
            ("raw_prefix_pairs", summary.RawPrefixPairs.ToString(CultureInfo.InvariantCulture)),
            ("rejected_rows", summary.RejectedRows.ToString(CultureInfo.InvariantCulture)),
            ("status", summary.Status),
            ("provenance", summary.Provenance),
            ("direct_selection_binding", summary.DirectSelectionBinding),
            ("original_semantics", summary.OriginalSemantics),
        };
        WriteLines(
            path,
            new[] { "field\tvalue" }.Concat(fields.Select(field =>
                $"{field.Name}\t{Field(field.Value)}")));
    }

    public static byte[] ReadRange(Stream source, long offset, long length)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (offset < 0 || length < 0 || length > int.MaxValue ||
            offset > source.Length || length > source.Length - offset)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        source.Position = offset;
        var bytes = new byte[checked((int)length)];
        source.ReadExactly(bytes);
        return bytes;
    }

    public static string Sha256(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IEnumerable<string> lines)
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
}
