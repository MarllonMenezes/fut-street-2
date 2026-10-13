using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioEventPayloadWaveCorrelationRow(
    string Kind,
    string SourcePath,
    string Name,
    int RowOrSoundIndex,
    long DataOffset,
    long DataLength,
    int CandidateCount,
    int AttemptCount,
    bool DecodeValidated,
    int WaveBytes,
    string PcmSha256,
    int ExactPcmMatchCount,
    string ExactPcmMatchPaths,
    string Status,
    string Issue);

public sealed record AudioEventPayloadWaveCorrelationSummary(
    int EventCandidates,
    int EventDecoded,
    int BankCandidates,
    int BankDecoded,
    int ExactPcmHashPairs,
    int EventHashesWithMatch,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics,
    int MaximumBankAttempts);

public sealed record AudioEventPayloadWaveCorrelationResult(
    IReadOnlyList<AudioEventPayloadWaveCorrelationRow> Rows,
    AudioEventPayloadWaveCorrelationSummary Summary);

public sealed record AudioEventPayloadWaveCorrelationExhaustiveSummary(
    int EventCandidates,
    int EventDecoded,
    int BankCount,
    int BankSoundCandidates,
    int BankSoundDecoded,
    int ExactPcmHashPairs,
    int EventHashesWithMatch,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics,
    int MaximumSoundCandidatesPerBank);

public sealed record AudioEventPayloadWaveCorrelationExhaustiveResult(
    IReadOnlyList<AudioEventPayloadWaveCorrelationRow> Rows,
    AudioEventPayloadWaveCorrelationExhaustiveSummary Summary);

/// <summary>
/// Compares bounded decoded PCM payloads across the already observed EVT and
/// ABKC/AST candidate paths. An exact hash is payload reuse evidence only; it
/// does not identify an event selector, sample index or gameplay meaning.
/// </summary>
public static class AudioEventPayloadWaveCorrelationAudit
{
    public const int DefaultMaximumBankAttempts = 8;
    public const int DefaultMaximumSoundCandidatesPerBank = 512;

    public static AudioEventPayloadWaveCorrelationResult Audit(
        GameAssetCatalog catalog,
        Func<string, Stream> opener,
        int maximumBankAttempts = DefaultMaximumBankAttempts)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);
        if (maximumBankAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBankAttempts));
        }

        var decodedEvents = new List<(AudioEventPayloadWaveCorrelationRow Row, string Hash)>();
        var eventRows = new List<AudioEventPayloadWaveCorrelationRow>();
        foreach (var candidate in catalog.AudioEventCandidates)
        {
            if (!TryDecodeEvent(candidate, opener, out var wave, out var issue))
            {
                eventRows.Add(new AudioEventPayloadWaveCorrelationRow(
                    "event",
                    candidate.EventPath,
                    candidate.Candidate.EventName,
                    candidate.Candidate.HeaderRowIndex,
                    candidate.Candidate.ParallelDataRow?.FirstColumn ?? -1,
                    candidate.Candidate.ParallelDataRow?.SecondColumn ?? 0,
                    1,
                    1,
                    false,
                    0,
                    string.Empty,
                    0,
                    string.Empty,
                    "unvalidated",
                    issue));
                continue;
            }

            var hash = HashPcm(wave);
            var row = new AudioEventPayloadWaveCorrelationRow(
                "event",
                candidate.EventPath,
                candidate.Candidate.EventName,
                candidate.Candidate.HeaderRowIndex,
                candidate.Candidate.ParallelDataRow!.FirstColumn,
                candidate.Candidate.ParallelDataRow!.SecondColumn,
                1,
                1,
                true,
                wave.Length,
                hash,
                0,
                string.Empty,
                "decoded-payload",
                string.Empty);
            decodedEvents.Add((row, hash));
            eventRows.Add(row);
        }

        using var mixer = new CatalogAudioMixer(catalog, opener);
        mixer.Initialize();
        var bankRows = new List<AudioEventPayloadWaveCorrelationRow>();
        var decodedBanks = new List<(AudioEventPayloadWaveCorrelationRow Row, string Hash)>();
        foreach (var bank in catalog.AudioBanks.OrderBy(bank => bank.Path, StringComparer.OrdinalIgnoreCase))
        {
            var candidates = BuildBankCandidates(bank).Take(maximumBankAttempts).ToArray();
            var attemptCount = 0;
            AudioEventPayloadWaveCorrelationRow? decodedRow = null;
            string? decodedHash = null;
            foreach (var candidate in candidates)
            {
                attemptCount++;
                if (!mixer.TryPrepareBankSound(
                        bank,
                        candidate.SoundIndex,
                        loopSegment: false,
                        out var wave,
                        out var channelLayoutCandidate))
                {
                    continue;
                }

                decodedHash = HashPcm(wave);
                decodedRow = new AudioEventPayloadWaveCorrelationRow(
                    "bank",
                    bank.Path,
                    candidate.Kind,
                    candidate.SoundIndex,
                    0,
                    0,
                    candidates.Length,
                    attemptCount,
                    true,
                    wave.Length,
                    decodedHash,
                    0,
                    string.Empty,
                    channelLayoutCandidate is null
                        ? "decoded-payload"
                        : "decoded-payload-channel-layout-candidate",
                    string.Empty);
                break;
            }

            if (decodedRow is null)
            {
                decodedRow = new AudioEventPayloadWaveCorrelationRow(
                    "bank",
                    bank.Path,
                    candidates.FirstOrDefault().Kind ?? "none",
                    candidates.FirstOrDefault().SoundIndex,
                    0,
                    0,
                    candidates.Length,
                    attemptCount,
                    false,
                    0,
                    string.Empty,
                    0,
                    string.Empty,
                    "unvalidated",
                    candidates.Length == 0
                        ? "no bounded sound-index candidate"
                        : "bounded candidates rejected by managed decoder");
            }

            bankRows.Add(decodedRow);
            if (decodedHash is not null)
            {
                decodedBanks.Add((decodedRow, decodedHash));
            }
        }

        var rows = new List<AudioEventPayloadWaveCorrelationRow>(eventRows.Count + bankRows.Count);
        var eventHashesWithMatch = 0;
        var exactPairs = 0;
        foreach (var eventRow in eventRows)
        {
            if (!eventRow.DecodeValidated)
            {
                rows.Add(eventRow);
                continue;
            }

            var eventHash = eventRow.PcmSha256;
            var matches = decodedBanks
                .Where(bank => string.Equals(bank.Hash, eventHash, StringComparison.OrdinalIgnoreCase))
                .Select(bank => bank.Row.SourcePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            exactPairs += matches.Length;
            if (matches.Length > 0)
            {
                eventHashesWithMatch++;
            }

            rows.Add(eventRow with
            {
                ExactPcmMatchCount = matches.Length,
                ExactPcmMatchPaths = string.Join('|', matches),
                Status = matches.Length > 0 ? "decoded-payload-correlation" : "decoded-payload-no-match",
            });
        }
        rows.AddRange(bankRows);

        var summary = new AudioEventPayloadWaveCorrelationSummary(
            eventRows.Count,
            eventRows.Count(row => row.DecodeValidated),
            bankRows.Count,
            bankRows.Count(row => row.DecodeValidated),
            exactPairs,
            eventHashesWithMatch,
            eventRows.Count == catalog.AudioEventCandidates.Count &&
            bankRows.Count == catalog.AudioBanks.Count
                ? "verified-structural"
                : "incomplete",
            "not-proven",
            "not-claimed",
            maximumBankAttempts);
        return new AudioEventPayloadWaveCorrelationResult(rows, summary);
    }

    /// <summary>
    /// Decodes every bounded sound-index candidate from every observed audio
    /// bank. This is a stronger payload comparison than <see cref="Audit"/>,
    /// which intentionally stops at the first decodable candidate per bank.
    /// It remains structural evidence only: a PCM match does not identify an
    /// event selector, sample binding, gameplay route or original semantics.
    /// </summary>
    public static AudioEventPayloadWaveCorrelationExhaustiveResult AuditAllBankCandidates(
        GameAssetCatalog catalog,
        Func<string, Stream> opener,
        int maximumSoundCandidatesPerBank = DefaultMaximumSoundCandidatesPerBank)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);
        if (maximumSoundCandidatesPerBank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSoundCandidatesPerBank));
        }

        var eventRows = new List<AudioEventPayloadWaveCorrelationRow>();
        var decodedEvents = new List<(AudioEventPayloadWaveCorrelationRow Row, string Hash)>();
        foreach (var candidate in catalog.AudioEventCandidates)
        {
            if (!TryDecodeEvent(candidate, opener, out var wave, out var issue))
            {
                eventRows.Add(new AudioEventPayloadWaveCorrelationRow(
                    "event",
                    candidate.EventPath,
                    candidate.Candidate.EventName,
                    candidate.Candidate.HeaderRowIndex,
                    candidate.Candidate.ParallelDataRow?.FirstColumn ?? -1,
                    candidate.Candidate.ParallelDataRow?.SecondColumn ?? 0,
                    1,
                    1,
                    false,
                    0,
                    string.Empty,
                    0,
                    string.Empty,
                    "unvalidated",
                    issue));
                continue;
            }

            var hash = HashPcm(wave);
            var row = new AudioEventPayloadWaveCorrelationRow(
                "event",
                candidate.EventPath,
                candidate.Candidate.EventName,
                candidate.Candidate.HeaderRowIndex,
                candidate.Candidate.ParallelDataRow!.FirstColumn,
                candidate.Candidate.ParallelDataRow.SecondColumn,
                1,
                1,
                true,
                wave.Length,
                hash,
                0,
                string.Empty,
                "decoded-payload",
                string.Empty);
            decodedEvents.Add((row, hash));
            eventRows.Add(row);
        }

        using var mixer = new CatalogAudioMixer(catalog, opener);
        mixer.Initialize();
        var bankRows = new List<AudioEventPayloadWaveCorrelationRow>();
        var decodedBanks = new List<(AudioEventPayloadWaveCorrelationRow Row, string Hash)>();
        var bankSoundCandidateCount = 0;
        foreach (var bank in catalog.AudioBanks.OrderBy(bank => bank.Path, StringComparer.OrdinalIgnoreCase))
        {
            var candidates = BuildBankCandidates(bank)
                .Take(maximumSoundCandidatesPerBank)
                .ToArray();
            bankSoundCandidateCount += candidates.Length;
            if (candidates.Length == 0)
            {
                bankRows.Add(new AudioEventPayloadWaveCorrelationRow(
                    "bank",
                    bank.Path,
                    "none",
                    -1,
                    0,
                    0,
                    0,
                    0,
                    false,
                    0,
                    string.Empty,
                    0,
                    string.Empty,
                    "unvalidated",
                    "no bounded sound-index candidate"));
                continue;
            }

            var attemptCount = 0;
            foreach (var candidate in candidates)
            {
                attemptCount++;
                if (!mixer.TryPrepareBankSound(
                        bank,
                        candidate.SoundIndex,
                        loopSegment: false,
                        out var wave,
                        out var channelLayoutCandidate))
                {
                    bankRows.Add(new AudioEventPayloadWaveCorrelationRow(
                        "bank",
                        bank.Path,
                        candidate.Kind,
                        candidate.SoundIndex,
                        0,
                        0,
                        candidates.Length,
                        attemptCount,
                        false,
                        0,
                        string.Empty,
                        0,
                        string.Empty,
                        "unvalidated",
                        "bounded candidate rejected by managed decoder"));
                    continue;
                }

                var hash = HashPcm(wave);
                var row = new AudioEventPayloadWaveCorrelationRow(
                    "bank",
                    bank.Path,
                    candidate.Kind,
                    candidate.SoundIndex,
                    0,
                    0,
                    candidates.Length,
                    attemptCount,
                    true,
                    wave.Length,
                    hash,
                    0,
                    string.Empty,
                    channelLayoutCandidate is null
                        ? "decoded-payload"
                        : "decoded-payload-channel-layout-candidate",
                    string.Empty);
                bankRows.Add(row);
                decodedBanks.Add((row, hash));
            }
        }

        var rows = new List<AudioEventPayloadWaveCorrelationRow>(eventRows.Count + bankRows.Count);
        var eventHashesWithMatch = 0;
        var exactPairs = 0;
        foreach (var eventRow in eventRows)
        {
            if (!eventRow.DecodeValidated)
            {
                rows.Add(eventRow);
                continue;
            }

            var matches = decodedBanks
                .Where(bank => string.Equals(bank.Hash, eventRow.PcmSha256, StringComparison.OrdinalIgnoreCase))
                .Select(bank => bank.Row.SourcePath + "#" + bank.Row.RowOrSoundIndex.ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            exactPairs += matches.Length;
            if (matches.Length > 0)
            {
                eventHashesWithMatch++;
            }

            rows.Add(eventRow with
            {
                ExactPcmMatchCount = matches.Length,
                ExactPcmMatchPaths = string.Join('|', matches),
                Status = matches.Length > 0 ? "decoded-payload-correlation" : "decoded-payload-no-match",
            });
        }
        rows.AddRange(bankRows);

        var summary = new AudioEventPayloadWaveCorrelationExhaustiveSummary(
            eventRows.Count,
            eventRows.Count(row => row.DecodeValidated),
            catalog.AudioBanks.Count,
            bankSoundCandidateCount,
            bankRows.Count(row => row.Kind == "bank" && row.DecodeValidated),
            exactPairs,
            eventHashesWithMatch,
            eventRows.Count == catalog.AudioEventCandidates.Count &&
            bankRows.Count > 0 &&
            catalog.AudioBanks.Count == bankRows
                .Select(row => row.SourcePath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count()
                ? "verified-structural"
                : "incomplete",
            "not-proven",
            "not-claimed",
            maximumSoundCandidatesPerBank);
        return new AudioEventPayloadWaveCorrelationExhaustiveResult(rows, summary);
    }

    public static AudioEventPayloadWaveCorrelationExhaustiveSummary SummarizeExhaustive(
        IEnumerable<AudioEventPayloadWaveCorrelationRow> rows,
        int expectedEvents,
        int expectedBanks,
        int maximumSoundCandidatesPerBank = DefaultMaximumSoundCandidatesPerBank)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (maximumSoundCandidatesPerBank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSoundCandidatesPerBank));
        }
        var materialized = rows.ToArray();
        var eventRows = materialized.Where(row => row.Kind == "event").ToArray();
        var bankRows = materialized.Where(row => row.Kind == "bank").ToArray();
        return new AudioEventPayloadWaveCorrelationExhaustiveSummary(
            eventRows.Length,
            eventRows.Count(row => row.DecodeValidated),
            bankRows.Select(row => row.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            bankRows
                .GroupBy(row => row.SourcePath, StringComparer.OrdinalIgnoreCase)
                .Sum(group => group.Max(row => row.CandidateCount)),
            bankRows.Count(row => row.DecodeValidated),
            eventRows.Sum(row => row.ExactPcmMatchCount),
            eventRows.Count(row => row.ExactPcmMatchCount > 0),
            eventRows.Length == expectedEvents &&
            bankRows.Select(row => row.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == expectedBanks
                ? "verified-structural"
                : "incomplete",
            "not-proven",
            "not-claimed",
            maximumSoundCandidatesPerBank);
    }

    public static AudioEventPayloadWaveCorrelationSummary Summarize(
        IEnumerable<AudioEventPayloadWaveCorrelationRow> rows,
        int expectedEvents,
        int expectedBanks,
        int maximumBankAttempts = DefaultMaximumBankAttempts)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (maximumBankAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBankAttempts));
        }
        var materialized = rows.ToArray();
        var eventRows = materialized.Where(row => row.Kind == "event").ToArray();
        var bankRows = materialized.Where(row => row.Kind == "bank").ToArray();
        return new AudioEventPayloadWaveCorrelationSummary(
            eventRows.Length,
            eventRows.Count(row => row.DecodeValidated),
            bankRows.Length,
            bankRows.Count(row => row.DecodeValidated),
            eventRows.Sum(row => row.ExactPcmMatchCount),
            eventRows.Count(row => row.ExactPcmMatchCount > 0),
            eventRows.Length == expectedEvents && bankRows.Length == expectedBanks
                ? "verified-structural"
                : "incomplete",
            "not-proven",
            "not-claimed",
            maximumBankAttempts);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioEventPayloadWaveCorrelationRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "kind",
                "source_path",
                "name",
                "row_or_sound_index",
                "data_offset",
                "data_length",
                "candidate_count",
                "attempt_count",
                "decode_validated",
                "wave_bytes",
                "pcm_sha256",
                "exact_pcm_match_count",
                "exact_pcm_match_paths",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.Kind),
            Escape(row.SourcePath),
            Escape(row.Name),
            row.RowOrSoundIndex.ToString(CultureInfo.InvariantCulture),
            row.DataOffset.ToString(CultureInfo.InvariantCulture),
            row.DataLength.ToString(CultureInfo.InvariantCulture),
            row.CandidateCount.ToString(CultureInfo.InvariantCulture),
            row.AttemptCount.ToString(CultureInfo.InvariantCulture),
            row.DecodeValidated.ToString(),
            row.WaveBytes.ToString(CultureInfo.InvariantCulture),
            Escape(row.PcmSha256),
            row.ExactPcmMatchCount.ToString(CultureInfo.InvariantCulture),
            Escape(row.ExactPcmMatchPaths),
            Escape(row.Status),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AudioEventPayloadWaveCorrelationSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new[]
        {
            "field\tvalue",
            Line("event_candidates", summary.EventCandidates),
            Line("event_decoded", summary.EventDecoded),
            Line("bank_candidates", summary.BankCandidates),
            Line("bank_decoded", summary.BankDecoded),
            Line("exact_pcm_hash_pairs", summary.ExactPcmHashPairs),
            Line("event_hashes_with_match", summary.EventHashesWithMatch),
            Line("status", summary.Status),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
            Line("bounded_policy", $"first_{summary.MaximumBankAttempts}_bank_candidates"),
        };
        WriteLines(path, lines);
    }

    public static void WriteExhaustiveSummary(
        string path,
        AudioEventPayloadWaveCorrelationExhaustiveSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new[]
        {
            "field\tvalue",
            Line("event_candidates", summary.EventCandidates),
            Line("event_decoded", summary.EventDecoded),
            Line("bank_count", summary.BankCount),
            Line("bank_sound_candidates", summary.BankSoundCandidates),
            Line("bank_sound_decoded", summary.BankSoundDecoded),
            Line("exact_pcm_hash_pairs", summary.ExactPcmHashPairs),
            Line("event_hashes_with_match", summary.EventHashesWithMatch),
            Line("status", summary.Status),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
            Line("bounded_policy", $"first_{summary.MaximumSoundCandidatesPerBank}_sound_candidates_per_bank"),
        };
        WriteLines(path, lines);
    }

    private static bool TryDecodeEvent(
        AudioEventCandidateAsset candidate,
        Func<string, Stream> opener,
        out byte[] wave,
        out string issue)
    {
        wave = Array.Empty<byte>();
        issue = string.Empty;
        if (candidate.Candidate.ParallelDataRow is not { } dataRow || dataRow.SecondColumn < 8)
        {
            issue = "parallel data range is unavailable or too short";
            return false;
        }
        if (candidate.Info is null && candidate.PayloadChannelLayoutCandidate is null)
        {
            issue = candidate.Issue ?? "no validated channel layout";
            return false;
        }

        try
        {
            using var dataStream = opener(candidate.DataPath);
            using var range = new AudioRangeStream(dataStream, dataRow.FirstColumn, dataRow.SecondColumn);
            wave = candidate.Info is { Channels: > 0 }
                ? EaSchlAudioDecoder.DecodeWave(range)
                : EaSchlAudioDecoder.DecodeWaveWithChannelLayoutCandidate(
                    range,
                    candidate.PayloadChannelLayoutCandidate!.Value);
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or NotSupportedException)
        {
            issue = exception.Message;
            return false;
        }
    }

    private static string HashPcm(byte[] wave)
    {
        var pcm = wave.Length > 44 ? wave.AsSpan(44) : wave.AsSpan();
        return Convert.ToHexString(SHA256.HashData(pcm));
    }

    private static IEnumerable<(string Kind, int SoundIndex)> BuildBankCandidates(AudioBankAsset bank)
    {
        foreach (var soundIndex in bank.AstRanges
                     .Where(range => !range.LoopSegment)
                     .Select(range => range.SoundIndex)
                     .Distinct()
                     .OrderBy(index => index))
        {
            yield return ("ast", soundIndex);
        }

        if (bank.AstPath is not null || !bank.BnkPresent)
        {
            yield break;
        }

        var observedRamSoundIndices = bank.SoundEntries
            .Where(sound => !sound.IsDummy && sound.SoundType == 0)
            .Select(sound => sound.SoundIndex)
            .Distinct()
            .OrderBy(index => index)
            .ToArray();
        if (observedRamSoundIndices.Length > 0)
        {
            foreach (var soundIndex in observedRamSoundIndices)
            {
                yield return ("bnk", soundIndex);
            }

            yield break;
        }

        var count = Math.Max(bank.BnkSoundCount, bank.SoundCount);
        for (var soundIndex = 1; soundIndex <= count; soundIndex++)
        {
            yield return ("bnk", soundIndex);
        }
    }

    private static string Line(string field, int value) =>
        $"{field}\t{value.ToString(CultureInfo.InvariantCulture)}";

    private static string Line(string field, string value) => $"{field}\t{Escape(value)}";

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
