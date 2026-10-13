using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioNamedEventCandidateDecodeResult(
    string EventPath,
    string EventName,
    int HeaderRowIndex,
    int EventOccurrenceCount,
    long DataOffset,
    long DataLength,
    bool HeaderValidated,
    int Channels,
    int CandidateChannels,
    int SampleRate,
    uint DeclaredSamples,
    bool DecodeValidated,
    int WaveBytes,
    string? Issue);

/// <summary>
/// Validates named EVT/header candidates by decoding their associated data
/// ranges when the observed SCHl metadata has usable channels or when a
/// separately validated payload channel-layout candidate exists. This is an
/// audio-payload evidence pass only; it does not bind event names to gameplay.
/// </summary>
public static class AudioNamedEventCandidateDecoder
{
    public static IReadOnlyList<AudioNamedEventCandidateDecodeResult> Decode(
        IEnumerable<AudioEventCandidateAsset> candidates,
        Func<string, Stream> opener)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(opener);

        return candidates.Select(candidate =>
        {
            var dataRow = candidate.Candidate.ParallelDataRow;
            var info = candidate.Info;
            var dataOffset = dataRow?.FirstColumn ?? -1;
            var dataLength = dataRow?.SecondColumn ?? 0;
            var common = new
            {
                candidate.EventPath,
                candidate.Candidate.EventName,
                candidate.Candidate.HeaderRowIndex,
                EventOccurrenceCount = candidate.Candidate.EventOccurrenceCount,
                DataOffset = dataOffset,
                DataLength = dataLength,
                HeaderValidated = candidate.HeaderValidated,
                Channels = info?.Channels ?? 0,
                CandidateChannels = candidate.PayloadChannelLayoutCandidate ?? info?.Channels ?? 0,
                SampleRate = info?.SampleRate ?? 0,
                DeclaredSamples = info?.DeclaredSamples ?? 0u,
            };

            if (dataRow is null || dataLength < 8)
            {
                return new AudioNamedEventCandidateDecodeResult(
                    common.EventPath,
                    common.EventName,
                    common.HeaderRowIndex,
                    common.EventOccurrenceCount,
                    common.DataOffset,
                    common.DataLength,
                    common.HeaderValidated,
                    common.Channels,
                    common.CandidateChannels,
                    common.SampleRate,
                    common.DeclaredSamples,
                    false,
                    0,
                    candidate.Issue ?? "parallel data range is unavailable or too short");
            }

            if (info is null)
            {
                return new AudioNamedEventCandidateDecodeResult(
                    common.EventPath,
                    common.EventName,
                    common.HeaderRowIndex,
                    common.EventOccurrenceCount,
                    common.DataOffset,
                    common.DataLength,
                    false,
                    common.Channels,
                    common.CandidateChannels,
                    common.SampleRate,
                    common.DeclaredSamples,
                    false,
                    0,
                    candidate.Issue ?? "SCHl header inspection was not validated");
            }

            if (info.Channels <= 0 && candidate.PayloadChannelLayoutCandidate is null)
            {
                return new AudioNamedEventCandidateDecodeResult(
                    common.EventPath,
                    common.EventName,
                    common.HeaderRowIndex,
                    common.EventOccurrenceCount,
                    common.DataOffset,
                    common.DataLength,
                    true,
                    common.Channels,
                    common.CandidateChannels,
                    common.SampleRate,
                    common.DeclaredSamples,
                    false,
                    0,
                    "observed SCHl header has zero channels");
            }

            try
            {
                using var dataStream = opener(candidate.DataPath);
                using var range = new AudioRangeStream(
                    dataStream,
                    dataRow.FirstColumn,
                    dataRow.SecondColumn);
                var decodeChannels = info.Channels > 0
                    ? info.Channels
                    : candidate.PayloadChannelLayoutCandidate!.Value;
                var wave = info.Channels > 0
                    ? EaSchlAudioDecoder.DecodeWave(range)
                    : EaSchlAudioDecoder.DecodeWaveWithChannelLayoutCandidate(range, decodeChannels);
                return new AudioNamedEventCandidateDecodeResult(
                    common.EventPath,
                    common.EventName,
                    common.HeaderRowIndex,
                    common.EventOccurrenceCount,
                    common.DataOffset,
                    common.DataLength,
                    true,
                    common.Channels,
                    common.CandidateChannels,
                    common.SampleRate,
                    common.DeclaredSamples,
                    true,
                    wave.Length,
                    null);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException or NotSupportedException)
            {
                return new AudioNamedEventCandidateDecodeResult(
                    common.EventPath,
                    common.EventName,
                    common.HeaderRowIndex,
                    common.EventOccurrenceCount,
                    common.DataOffset,
                    common.DataLength,
                    true,
                    common.Channels,
                    common.CandidateChannels,
                    common.SampleRate,
                    common.DeclaredSamples,
                    false,
                    0,
                    exception.Message);
            }
        }).ToArray();
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioNamedEventCandidateDecodeResult> results)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(results);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "event_path",
                "event_name",
                "header_row",
                "event_occurrences",
                "data_offset",
                "data_length",
                "header_validated",
                "channels",
                "candidate_channels",
                "sample_rate",
                "declared_samples",
                "decode_validated",
                "wave_bytes",
                "issue",
            }),
        };
        lines.AddRange(results.Select(result => string.Join("\t", new[]
        {
            Escape(result.EventPath),
            Escape(result.EventName),
            result.HeaderRowIndex.ToString(CultureInfo.InvariantCulture),
            result.EventOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            result.DataOffset.ToString(CultureInfo.InvariantCulture),
            result.DataLength.ToString(CultureInfo.InvariantCulture),
            result.HeaderValidated.ToString(),
            result.Channels.ToString(CultureInfo.InvariantCulture),
            result.CandidateChannels.ToString(CultureInfo.InvariantCulture),
            result.SampleRate.ToString(CultureInfo.InvariantCulture),
            result.DeclaredSamples.ToString(CultureInfo.InvariantCulture),
            result.DecodeValidated.ToString(),
            result.WaveBytes.ToString(CultureInfo.InvariantCulture),
            Escape(result.Issue ?? string.Empty),
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

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
