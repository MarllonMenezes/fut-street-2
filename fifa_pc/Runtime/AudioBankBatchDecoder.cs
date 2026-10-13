using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioBankBatchDecodeResult(
    string BankPath,
    string SourceKind,
    int CandidateSoundCount,
    int AttemptCount,
    int SelectedSoundIndex,
    bool DecodeValidated,
    int WaveBytes,
    string Issue,
    int? SelectedChannelLayoutCandidate);

/// <summary>
/// Tries one bounded set of already observed sound-index candidates per ABKC
/// bank. It validates the AST/BNK payload path without assigning a sound to an
/// EVT name, gameplay action, material, animation or GPU command.
/// </summary>
public static class AudioBankBatchDecoder
{
    public static IReadOnlyList<AudioBankBatchDecodeResult> DecodeFirstAvailable(
        GameAssetCatalog catalog,
        Func<string, Stream> opener,
        int maximumAttemptsPerBank = 8)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);
        if (maximumAttemptsPerBank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttemptsPerBank));
        }

        using var mixer = new CatalogAudioMixer(catalog, opener);
        mixer.Initialize();
        return DecodeFirstAvailable(catalog, mixer, maximumAttemptsPerBank);
    }

    internal static IReadOnlyList<AudioBankBatchDecodeResult> DecodeFirstAvailable(
        GameAssetCatalog catalog,
        CatalogAudioMixer mixer,
        int maximumAttemptsPerBank = 8)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(mixer);
        if (maximumAttemptsPerBank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumAttemptsPerBank));
        }

        var rows = new List<AudioBankBatchDecodeResult>(catalog.AudioBanks.Count);
        foreach (var bank in catalog.AudioBanks.OrderBy(bank => bank.Path, StringComparer.OrdinalIgnoreCase))
        {
            var candidates = BuildCandidates(bank)
                .Take(maximumAttemptsPerBank)
                .ToArray();
            var attemptCount = 0;
            var decoded = false;
            var selectedIndex = 0;
            var waveBytes = 0;
            int? selectedChannelLayoutCandidate = null;
            var selectedKind = candidates.FirstOrDefault().Kind ?? "none";
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

                decoded = true;
                selectedIndex = candidate.SoundIndex;
                selectedKind = candidate.Kind;
                waveBytes = wave.Length;
                selectedChannelLayoutCandidate = channelLayoutCandidate;
                break;
            }

            rows.Add(new AudioBankBatchDecodeResult(
                bank.Path,
                selectedKind,
                candidates.Length,
                attemptCount,
                selectedIndex,
                decoded,
                waveBytes,
                decoded
                    ? string.Empty
                    : candidates.Length == 0
                        ? "no bounded sound-index candidate was available"
                        : "all bounded candidate sound ranges were rejected by the managed decoder",
                selectedChannelLayoutCandidate));
        }

        return rows;
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioBankBatchDecodeResult> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "bank_path",
                "source_kind",
                "candidate_sound_count",
                "attempt_count",
                "selected_sound_index",
                "decode_validated",
                "wave_bytes",
                "issue",
                "channel_layout_candidate",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.BankPath),
            Escape(row.SourceKind),
            row.CandidateSoundCount.ToString(CultureInfo.InvariantCulture),
            row.AttemptCount.ToString(CultureInfo.InvariantCulture),
            row.SelectedSoundIndex.ToString(CultureInfo.InvariantCulture),
            row.DecodeValidated.ToString(),
            row.WaveBytes.ToString(CultureInfo.InvariantCulture),
            Escape(row.Issue),
            row.SelectedChannelLayoutCandidate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
        })));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static IEnumerable<(string Kind, int SoundIndex)> BuildCandidates(AudioBankAsset bank)
    {
        foreach (var soundIndex in bank.AstRanges
                     .Where(range => !range.LoopSegment)
                     .Select(range => range.SoundIndex)
                     .Distinct()
                     .OrderBy(index => index))
        {
            yield return ("ast", soundIndex);
        }

        if (!bank.BnkPresent)
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

        var bnkCandidateCount = Math.Max(bank.BnkSoundCount, bank.SoundCount);
        for (var soundIndex = 1; soundIndex <= bnkCandidateCount; soundIndex++)
        {
            yield return ("bnk", soundIndex);
        }
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
