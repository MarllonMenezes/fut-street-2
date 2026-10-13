using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Machine-readable evidence that the authored PC runtime exercised the
/// candidate render path, managed gameplay, authored input, authored procedural
/// animation and authored audio in one run.
/// This is an integration smoke artifact, not proof of original asset
/// bindings or original game semantics.
/// </summary>
public readonly record struct CleanRoomIntegrationReportData(
    bool CandidateMaterialValidated,
    bool CandidateHomeEnabled,
    bool CandidateAwayEnabled,
    int RenderedFrames,
    string RenderChecksum,
    int DistinctRenderChecksums,
    int AuthoredCharacterAssetFrames,
    string AuthoredCharacterAssetManifest,
    int AuthoredSceneAssetFrames,
    string AuthoredSceneAssetManifest,
    string AuthoredAnimationAssetManifest,
    int InputSamples,
    int AnimationSamples,
    int MatchEvents,
    int AudioEvents,
    int AudioPrepared,
    int AudioPlayed,
    int AudioUnmapped,
    long GameplayAudioPcmBytes,
    string GameplayAudioPcmSha256,
    string AuthoredAudioAssetManifest,
    bool MatchTraceWritten,
    bool InputTraceWritten,
    bool AnimationTraceWritten,
    bool AudioTraceWritten,
    bool OpponentAiEnabled = false,
    int NamedEventCandidateWavesPrepared = 0,
    long NamedEventCandidatePcmBytesPrepared = 0,
    int NamedEventCandidateDecodeRejected = 0,
    int ObservedBankCandidateCount = 0,
    int ObservedBankCandidateDecoded = 0,
    int ObservedBankCandidateRejected = 0);

public static class CleanRoomIntegrationReport
{
    public const string Header = "field\tvalue";
    public const string RuntimeAssemblySha256Field = "runtime_assembly_sha256";
    public static string CurrentAssemblySha256 { get; } = ComputeCurrentAssemblySha256();

    public static IReadOnlyList<string> BuildLines(CleanRoomIntegrationReportData data)
    {
        var lines = new List<string>
        {
            Header,
            Line("integration_scope", "authored-clean-room-runtime"),
            Line(RuntimeAssemblySha256Field, CurrentAssemblySha256),
            Line("candidate_material_validated", data.CandidateMaterialValidated),
            Line("candidate_home_enabled", data.CandidateHomeEnabled),
            Line("candidate_away_enabled", data.CandidateAwayEnabled),
            Line("rendered_frames", data.RenderedFrames),
            Line("render_checksum", data.RenderChecksum),
            Line("distinct_render_checksums", data.DistinctRenderChecksums),
            Line("authored_character_asset_frames", data.AuthoredCharacterAssetFrames),
            Line("authored_character_asset_manifest", data.AuthoredCharacterAssetManifest),
            Line("authored_scene_asset_frames", data.AuthoredSceneAssetFrames),
            Line("authored_scene_asset_manifest", data.AuthoredSceneAssetManifest),
            Line("authored_animation_asset_manifest", data.AuthoredAnimationAssetManifest),
            Line("input_samples", data.InputSamples),
            Line("animation_samples", data.AnimationSamples),
            Line("match_events", data.MatchEvents),
            Line("audio_events", data.AudioEvents),
            Line("audio_prepared", data.AudioPrepared),
            Line("audio_played", data.AudioPlayed),
            Line("audio_unmapped", data.AudioUnmapped),
            Line("named_event_candidate_waves_prepared", data.NamedEventCandidateWavesPrepared),
            Line("named_event_candidate_pcm_bytes_prepared", data.NamedEventCandidatePcmBytesPrepared),
            Line("named_event_candidate_decode_rejected", data.NamedEventCandidateDecodeRejected),
            Line("observed_bank_candidate_count", data.ObservedBankCandidateCount),
            Line("observed_bank_candidate_decoded", data.ObservedBankCandidateDecoded),
            Line("observed_bank_candidate_rejected", data.ObservedBankCandidateRejected),
            Line("gameplay_audio_pcm_bytes", data.GameplayAudioPcmBytes),
            Line("gameplay_audio_pcm_sha256", data.GameplayAudioPcmSha256),
            Line("authored_audio_asset_manifest", data.AuthoredAudioAssetManifest),
            Line("match_trace_written", data.MatchTraceWritten),
            Line("input_trace_written", data.InputTraceWritten),
            Line("animation_trace_written", data.AnimationTraceWritten),
            Line("audio_trace_written", data.AudioTraceWritten),
            Line("opponent_ai_enabled", data.OpponentAiEnabled),
            Line("original_semantics", "not-claimed"),
        };
        return lines.AsReadOnly();
    }

    public static void Write(string path, CleanRoomIntegrationReportData data)
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
            BuildLines(data),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Line(string field, bool value) =>
        Line(field, value ? "True" : "False");

    private static string Line(string field, int value) =>
        Line(field, value.ToString(CultureInfo.InvariantCulture));

    private static string Line(string field, long value) =>
        Line(field, value.ToString(CultureInfo.InvariantCulture));

    private static string Line(string field, string value) =>
        $"{field}\t{Escape(value)}";

    private static string Escape(string value) =>
        (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');

    private static string ComputeCurrentAssemblySha256()
    {
        var assemblyPath = typeof(CleanRoomIntegrationReport).Assembly.Location;
        if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
        {
            return string.Empty;
        }

        using var stream = File.OpenRead(assemblyPath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
