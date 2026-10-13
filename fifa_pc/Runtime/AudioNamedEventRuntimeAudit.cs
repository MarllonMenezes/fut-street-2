using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioNamedEventRuntimeAuditRow(
    string EventPath,
    string HeaderPath,
    string DataPath,
    string EventName,
    int HeaderRowIndex,
    long DataOffset,
    long DataLength,
    bool CandidateMetadataValidated,
    bool RuntimePrepared,
    bool WaveHeaderValid,
    int WaveBytes,
    long PcmBytes,
    string PcmSha256,
    string Status,
    string Provenance,
    string OriginalSemantics,
    string Issue);

public sealed record AudioNamedEventRuntimeAuditSummary(
    int CandidateCount,
    int CandidateMetadataValidated,
    int RuntimePrepared,
    int WaveHeadersValid,
    long PcmBytes,
    string Status,
    string Provenance,
    string OriginalSemantics);

public sealed record AudioNamedEventRuntimeAuditResult(
    IReadOnlyList<AudioNamedEventRuntimeAuditRow> Rows,
    AudioNamedEventRuntimeAuditSummary Summary);

/// <summary>
/// Verifies that each exact named EVT payload candidate can be prepared by the
/// managed audio runtime. This joins only the observed event name, its
/// parallel data range and the runtime decoder. It does not bind a gameplay
/// action to the event, select an ABKC sample, or claim original semantics.
/// </summary>
public static class AudioNamedEventRuntimeAudit
{
    public const string Provenance =
        "exact-EVT-name+parallel-data-range; managed-runtime-named-payload-only";
    public const string OriginalSemantics = "not-claimed";

    public static AudioNamedEventRuntimeAuditResult Audit(
        GameAssetCatalog catalog,
        Func<string, Stream> opener)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);

        using var mixer = new CatalogAudioMixer(catalog, opener);
        mixer.Initialize();

        var rows = new List<AudioNamedEventRuntimeAuditRow>(catalog.AudioEventCandidates.Count);
        foreach (var candidate in catalog.AudioEventCandidates
                     .OrderBy(item => item.EventPath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Candidate.HeaderRowIndex)
                     .ThenBy(item => item.Candidate.EventName, StringComparer.OrdinalIgnoreCase))
        {
            var dataRow = candidate.Candidate.ParallelDataRow;
            var metadataValidated = candidate.HeaderValidated &&
                                     dataRow is { FirstColumn: >= 0, SecondColumn: >= 8 } &&
                                     (candidate.Info is { Channels: > 0 } ||
                                      candidate.PayloadChannelLayoutCandidate is not null);
            var dataOffset = dataRow?.FirstColumn ?? -1;
            var dataLength = dataRow?.SecondColumn ?? 0;
            var runtimePrepared = false;
            var waveHeaderValid = false;
            var waveBytes = 0;
            long pcmBytes = 0;
            var pcmSha256 = string.Empty;
            var issue = candidate.Issue ?? string.Empty;

            if (metadataValidated)
            {
                try
                {
                    runtimePrepared = mixer.TryPrepareNamedEvent(
                        candidate.Candidate.EventName,
                        out var wave);
                    waveHeaderValid = runtimePrepared && IsWaveHeader(wave);
                    waveBytes = runtimePrepared ? wave.Length : 0;
                    pcmBytes = waveHeaderValid ? wave.Length - 44L : 0;
                    pcmSha256 = waveHeaderValid
                        ? Convert.ToHexString(SHA256.HashData(wave.AsSpan(44)))
                        : string.Empty;
                    if (!runtimePrepared)
                    {
                        issue = "managed runtime did not prepare the exact named payload";
                    }
                    else if (!waveHeaderValid)
                    {
                        issue = "managed runtime returned a non-WAVE payload";
                    }
                }
                catch (InvalidDataException exception)
                {
                    issue = exception.Message;
                }
                catch (NotSupportedException exception)
                {
                    issue = exception.Message;
                }
                catch (EndOfStreamException exception)
                {
                    issue = exception.Message;
                }
                catch (OverflowException exception)
                {
                    issue = exception.Message;
                }
            }
            else if (issue.Length == 0)
            {
                issue = "named EVT candidate metadata is incomplete";
            }

            var verified = metadataValidated && runtimePrepared && waveHeaderValid;
            rows.Add(new AudioNamedEventRuntimeAuditRow(
                candidate.EventPath,
                candidate.HeaderPath,
                candidate.DataPath,
                candidate.Candidate.EventName,
                candidate.Candidate.HeaderRowIndex,
                dataOffset,
                dataLength,
                metadataValidated,
                runtimePrepared,
                waveHeaderValid,
                waveBytes,
                pcmBytes,
                pcmSha256,
                verified ? "verified-runtime-named-payload" : "runtime-named-payload-mismatch",
                Provenance,
                OriginalSemantics,
                verified ? string.Empty : issue));
        }

        return new AudioNamedEventRuntimeAuditResult(rows, Summarize(rows));
    }

    public static AudioNamedEventRuntimeAuditSummary Summarize(
        IReadOnlyList<AudioNamedEventRuntimeAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var verified = rows.Count(row => row.Status == "verified-runtime-named-payload");
        return new AudioNamedEventRuntimeAuditSummary(
            rows.Count,
            rows.Count(row => row.CandidateMetadataValidated),
            rows.Count(row => row.RuntimePrepared),
            rows.Count(row => row.WaveHeaderValid),
            rows.Sum(row => row.PcmBytes),
            rows.Count > 0 && verified == rows.Count
                ? "verified-runtime-named-payload"
                : "incomplete-runtime-named-payload",
            Provenance,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioNamedEventRuntimeAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "event_path",
                "header_path",
                "data_path",
                "event_name",
                "header_row",
                "data_offset",
                "data_length",
                "candidate_metadata_validated",
                "runtime_prepared",
                "wave_header_valid",
                "wave_bytes",
                "pcm_bytes",
                "pcm_sha256",
                "status",
                "provenance",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.EventPath),
            Escape(row.HeaderPath),
            Escape(row.DataPath),
            Escape(row.EventName),
            row.HeaderRowIndex.ToString(CultureInfo.InvariantCulture),
            row.DataOffset.ToString(CultureInfo.InvariantCulture),
            row.DataLength.ToString(CultureInfo.InvariantCulture),
            row.CandidateMetadataValidated.ToString(),
            row.RuntimePrepared.ToString(),
            row.WaveHeaderValid.ToString(),
            row.WaveBytes.ToString(CultureInfo.InvariantCulture),
            row.PcmBytes.ToString(CultureInfo.InvariantCulture),
            Escape(row.PcmSha256),
            Escape(row.Status),
            Escape(row.Provenance),
            Escape(row.OriginalSemantics),
            Escape(row.Issue),
        })));

        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AudioNamedEventRuntimeAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);

        var lines = new[]
        {
            "field\tvalue",
            Line("candidate_count", summary.CandidateCount),
            Line("candidate_metadata_validated", summary.CandidateMetadataValidated),
            Line("runtime_prepared", summary.RuntimePrepared),
            Line("wave_headers_valid", summary.WaveHeadersValid),
            Line("pcm_bytes", summary.PcmBytes),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("original_semantics", summary.OriginalSemantics),
        };
        WriteLines(path, lines);
    }

    private static bool IsWaveHeader(ReadOnlySpan<byte> wave) =>
        wave.Length >= 44 &&
        wave[..4].SequenceEqual("RIFF"u8) &&
        wave[8..12].SequenceEqual("WAVE"u8) &&
        wave[12..16].SequenceEqual("fmt "u8) &&
        wave[36..40].SequenceEqual("data"u8);

    private static string Line<T>(string field, T value) =>
        $"{field}\t{Convert.ToString(value, CultureInfo.InvariantCulture)}";

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
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
