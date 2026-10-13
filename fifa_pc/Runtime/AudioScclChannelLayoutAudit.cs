using System.Globalization;

namespace FifaStreetPc;

public sealed record AudioScclChannelLayoutAuditRow(
    int DataRowIndex,
    long HeaderOffset,
    long HeaderLength,
    long DataOffset,
    long DataLength,
    int ObservedChannels,
    int SampleRate,
    uint DeclaredSamples,
    string ScclPayloadPrefix,
    bool MonoValid,
    long MonoFrames,
    bool StereoValid,
    long StereoFrames,
    string UniqueCandidateChannels,
    string MonoIssue,
    string StereoIssue);

public sealed record AudioScclChannelLayoutAuditSummary(
    int ObservationCount,
    int MonoValidCount,
    int StereoValidCount,
    int UniqueMonoCount,
    int UniqueStereoCount,
    int BothValidCount,
    int NeitherValidCount,
    string DistinctScclPayloadPrefixes);

/// <summary>
/// Tests only mono/stereo SCDl layout hypotheses for bounded SCHl ranges whose
/// observed header has zero channels. A successful hypothesis remains a
/// payload candidate; it is never promoted to an EVT/ABKC/gameplay binding.
/// </summary>
public static class AudioScclChannelLayoutAuditor
{
    public static bool TryGetUniqueCandidate(
        Stream dataStream,
        AudioOffsetRow dataRow,
        out int candidateChannels,
        out long sampleFrames)
    {
        ArgumentNullException.ThrowIfNull(dataStream);
        candidateChannels = 0;
        sampleFrames = 0;
        var observedInfo = InspectRange(dataStream, dataRow);
        if (observedInfo.Channels != 0)
        {
            return false;
        }

        var mono = TryValidate(dataStream, dataRow, 1);
        var stereo = TryValidate(dataStream, dataRow, 2);
        if (mono.Valid == stereo.Valid)
        {
            return false;
        }

        candidateChannels = mono.Valid ? 1 : 2;
        sampleFrames = mono.Valid ? mono.Frames : stereo.Frames;
        return true;
    }

    public static IReadOnlyList<AudioScclChannelLayoutAuditRow> Audit(
        IEnumerable<AudioEventHeaderCandidate> candidates,
        Stream dataStream)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(dataStream);

        return candidates
            .Where(candidate => candidate.ParallelDataRow is not null)
            .GroupBy(candidate => candidate.HeaderRowIndex)
            .Select(group => group.First())
            .OrderBy(candidate => candidate.HeaderRowIndex)
            .Select(candidate => new
            {
                Candidate = candidate,
                ObservedInfo = InspectRange(dataStream, candidate.ParallelDataRow!),
            })
            .Where(item => item.ObservedInfo.Channels == 0)
            .Select(item => AuditRow(item.Candidate, dataStream))
            .ToArray();
    }

    public static AudioScclChannelLayoutAuditSummary Summarize(
        IEnumerable<AudioScclChannelLayoutAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var prefixes = materialized
            .Select(row => row.ScclPayloadPrefix)
            .Where(prefix => prefix.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(prefix => prefix, StringComparer.Ordinal)
            .ToArray();
        return new AudioScclChannelLayoutAuditSummary(
            materialized.Length,
            materialized.Count(row => row.MonoValid),
            materialized.Count(row => row.StereoValid),
            materialized.Count(row => row.UniqueCandidateChannels == "1"),
            materialized.Count(row => row.UniqueCandidateChannels == "2"),
            materialized.Count(row => row.UniqueCandidateChannels == "1|2"),
            materialized.Count(row => row.UniqueCandidateChannels == "none"),
            string.Join(",", prefixes));
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioScclChannelLayoutAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "data_row",
                "header_offset",
                "header_length",
                "data_offset",
                "data_length",
                "observed_channels",
                "sample_rate",
                "declared_samples",
                "sccl_payload_prefix",
                "mono_valid",
                "mono_frames",
                "stereo_valid",
                "stereo_frames",
                "unique_candidate_channels",
                "mono_issue",
                "stereo_issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            row.DataRowIndex.ToString(CultureInfo.InvariantCulture),
            row.HeaderOffset.ToString(CultureInfo.InvariantCulture),
            row.HeaderLength.ToString(CultureInfo.InvariantCulture),
            row.DataOffset.ToString(CultureInfo.InvariantCulture),
            row.DataLength.ToString(CultureInfo.InvariantCulture),
            row.ObservedChannels.ToString(CultureInfo.InvariantCulture),
            row.SampleRate.ToString(CultureInfo.InvariantCulture),
            row.DeclaredSamples.ToString(CultureInfo.InvariantCulture),
            row.ScclPayloadPrefix,
            row.MonoValid.ToString(),
            row.MonoFrames.ToString(CultureInfo.InvariantCulture),
            row.StereoValid.ToString(),
            row.StereoFrames.ToString(CultureInfo.InvariantCulture),
            row.UniqueCandidateChannels,
            Escape(row.MonoIssue),
            Escape(row.StereoIssue),
        })));
        WriteLines(path, lines);
    }

    private static AudioScclChannelLayoutAuditRow AuditRow(
        AudioEventHeaderCandidate candidate,
        Stream dataStream)
    {
        var dataRow = candidate.ParallelDataRow!;
        var headerRow = candidate.HeaderRow;
        var observedInfo = InspectRange(dataStream, dataRow);
        var chunks = InspectChunks(dataStream, dataRow);
        var scclPayloadPrefix = chunks.Count > 0 && chunks[0].Type == "SCCl"
            ? Convert.ToHexString(chunks[0].PayloadPrefix)
            : string.Empty;
        var mono = TryValidate(dataStream, dataRow, 1);
        var stereo = TryValidate(dataStream, dataRow, 2);
        var uniqueCandidateChannels = (mono.Valid, stereo.Valid) switch
        {
            (true, false) => "1",
            (false, true) => "2",
            (true, true) => "1|2",
            _ => "none",
        };
        return new AudioScclChannelLayoutAuditRow(
            candidate.HeaderRowIndex,
            headerRow.FirstColumn,
            headerRow.SecondColumn,
            dataRow.FirstColumn,
            dataRow.SecondColumn,
            observedInfo.Channels,
            observedInfo.SampleRate,
            observedInfo.DeclaredSamples,
            scclPayloadPrefix,
            mono.Valid,
            mono.Frames,
            stereo.Valid,
            stereo.Frames,
            uniqueCandidateChannels,
            mono.Issue,
            stereo.Issue);
    }

    private static EaSchlAudioInfo InspectRange(Stream dataStream, AudioOffsetRow dataRow)
    {
        using var range = new AudioRangeStream(dataStream, dataRow.FirstColumn, dataRow.SecondColumn);
        return EaSchlAudioDecoder.Inspect(range);
    }

    private static IReadOnlyList<EaSchlChunkEvidence> InspectChunks(
        Stream dataStream,
        AudioOffsetRow dataRow)
    {
        using var range = new AudioRangeStream(dataStream, dataRow.FirstColumn, dataRow.SecondColumn);
        return EaSchlAudioDecoder.InspectChunkSequence(range, maximumChunks: 1, payloadPrefixBytes: 16);
    }

    private static (bool Valid, long Frames, string Issue) TryValidate(
        Stream dataStream,
        AudioOffsetRow dataRow,
        int channels)
    {
        using var range = new AudioRangeStream(dataStream, dataRow.FirstColumn, dataRow.SecondColumn);
        var valid = EaSchlAudioDecoder.TryValidateChannelLayoutCandidate(
            range,
            channels,
            out var exportInfo,
            out var issue);
        return (valid, valid ? exportInfo!.SampleFrames : 0, issue ?? string.Empty);
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
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
