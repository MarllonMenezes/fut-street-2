using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record CandidateIntegratedRuntimeScenarioSpec(
    string Name,
    string IntegrationReportPath,
    string ReplayIntegrationReportPath,
    string MatchTracePath,
    string MatchReplayPath,
    string InputTracePath,
    string InputReplayPath,
    string AnimationTracePath,
    string AnimationReplayPath,
    string AudioTracePath,
    string AudioReplayPath,
    IReadOnlyList<string> RequiredActions,
    string FrameOutputPath,
    string CandidateAssetProvenanceSummaryPath);

public sealed record CandidateIntegratedRuntimeAuditResult(
    string Name,
    string IntegrationReportPath,
    string IntegrationSha256,
    bool IntegrationExists,
    string ReplayIntegrationReportPath,
    string ReplayIntegrationSha256,
    bool ReplayIntegrationExists,
    bool CandidateFlagsValid,
    bool ProvenanceValid,
    bool CandidateAssetProvenanceValid,
    bool CandidateMeshStateCrosscheckValid,
    int CandidateAssetCount,
    bool CandidateGeometryStreamsComplete,
    bool CandidateShapeStateStreamsComplete,
    bool IntegrationReportsMatch,
    bool TraceFilesValid,
    bool ReplayMatched,
    bool RequiredActionsPresent,
    int RenderedFrames,
    int AnimationSamples,
    int AudioEvents,
    int AudioPrepared,
    int AudioPlayed,
    int AudioUnmapped,
    bool FrameOutputValid,
    int FrameVisiblePixels,
    string Status,
    string OriginalSemantics,
    string Issue);

/// <summary>
/// Verifies the opt-in candidate full-stack runtime as a managed, reproducible
/// integration artifact. This is candidate-runtime evidence only: it does not
/// decode or promote original material, UV, skinning, rigging, animation,
/// GPU-command, audio-event or gameplay semantics.
/// </summary>
public static class CandidateIntegratedRuntimeAudit
{
    public const string Header =
        "kind\tname\tintegration_report\tintegration_sha256\tintegration_exists\treplay_integration_report\treplay_integration_sha256\treplay_integration_exists\tcandidate_flags_valid\tprovenance_valid\tcandidate_asset_provenance_valid\tcandidate_mesh_state_crosscheck_valid\tcandidate_asset_count\tcandidate_geometry_streams_complete\tcandidate_shape_state_streams_complete\tintegration_reports_match\ttrace_files_valid\treplay_matched\trequired_actions_present\trendered_frames\tanimation_samples\taudio_events\taudio_prepared\taudio_played\taudio_unmapped\tframe_output_valid\tframe_visible_pixels\tstatus\toriginal_semantics\tissue";

    public static CandidateIntegratedRuntimeScenarioSpec DefaultScenario(
        string workspaceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        var root = Path.Combine(Path.GetFullPath(workspaceDirectory), "fifa_pc");
        return new CandidateIntegratedRuntimeScenarioSpec(
            "candidate-full-stack",
            Path.Combine(root, "gameplay-full-stack-integration.tsv"),
            Path.Combine(root, "gameplay-full-stack-replayed-integration.tsv"),
            Path.Combine(root, "gameplay-full-stack-match-trace.tsv"),
            Path.Combine(root, "gameplay-full-stack-match-replayed.tsv"),
            Path.Combine(root, "gameplay-full-stack-input-trace.tsv"),
            Path.Combine(root, "gameplay-full-stack-input-replayed.tsv"),
            Path.Combine(root, "gameplay-full-stack-animation-trace.tsv"),
            Path.Combine(root, "gameplay-full-stack-animation-replayed.tsv"),
            Path.Combine(root, "gameplay-full-stack-audio-trace.tsv"),
            Path.Combine(root, "gameplay-full-stack-audio-replayed.tsv"),
            new[] { "KICKOFF", "SHOT_AWAY", "GOAL_AWAY" },
            Path.Combine(root, "gameplay-full-stack-frame.ppm"),
            Path.Combine(root, "eagl-candidate-asset-provenance-audit-summary.tsv"));
    }

    public static CandidateIntegratedRuntimeAuditResult Audit(
        CandidateIntegratedRuntimeScenarioSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);

        var integrationExists = File.Exists(spec.IntegrationReportPath);
        var replayIntegrationExists = File.Exists(spec.ReplayIntegrationReportPath);
        var integrationSha256 = integrationExists ? ComputeSha256(spec.IntegrationReportPath) : string.Empty;
        var replayIntegrationSha256 = replayIntegrationExists
            ? ComputeSha256(spec.ReplayIntegrationReportPath)
            : string.Empty;
        var integration = integrationExists
            ? ReadKeyValueTsv(spec.IntegrationReportPath)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var replayIntegration = replayIntegrationExists
            ? ReadKeyValueTsv(spec.ReplayIntegrationReportPath)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        var candidateFlagsValid = HasCandidateFlags(integration) && HasCandidateFlags(replayIntegration);
        var provenanceValid = IsValidCandidateReport(integration) && IsValidCandidateReport(replayIntegration);
        var candidateAssetSummaryExists = File.Exists(spec.CandidateAssetProvenanceSummaryPath);
        var candidateAssetSummary = candidateAssetSummaryExists
            ? ReadKeyValueTsv(spec.CandidateAssetProvenanceSummaryPath)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var candidateAssetCount = ReadInt(candidateAssetSummary, "candidate_count");
        var candidateAssetProvenanceValid = candidateAssetSummaryExists &&
            string.Equals(Get(candidateAssetSummary, "status"), "verified-candidate-source-chain", StringComparison.Ordinal) &&
            string.Equals(Get(candidateAssetSummary, "candidate_provenance"), EaglCandidateAssetProvenanceAudit.CandidateProvenance, StringComparison.Ordinal) &&
            string.Equals(Get(candidateAssetSummary, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            candidateAssetCount > 0 &&
            ReadInt(candidateAssetSummary, "source_chain_verified") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "decoded_pixel_matches") == candidateAssetCount;
        var candidateMeshStateCrosscheckValid = candidateAssetProvenanceValid &&
            ReadInt(candidateAssetSummary, "candidate_structural_verified") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "model_object_entries_read") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "model_companion_entries_read") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "candidate_bounds_valid") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "candidate_geometry_finite_and_indexed") == candidateAssetCount &&
            ReadInt(candidateAssetSummary, "candidate_primitive_shape_state_rows") == candidateAssetCount;
        var candidateGeometryStreamsComplete = candidateAssetProvenanceValid &&
            ReadInt(candidateAssetSummary, "candidate_geometry_streams_complete") == candidateAssetCount;
        var candidateShapeStateStreamsComplete = candidateAssetProvenanceValid &&
            ReadInt(candidateAssetSummary, "candidate_shape_state_streams_complete") == candidateAssetCount;
        var integrationReportsMatch = integrationExists &&
            replayIntegrationExists &&
            string.Equals(integrationSha256, replayIntegrationSha256, StringComparison.OrdinalIgnoreCase);

        var renderedFrames = ReadInt(integration, "rendered_frames");
        var animationSamples = ReadInt(integration, "animation_samples");
        var audioEvents = ReadInt(integration, "audio_events");
        var audioPrepared = ReadInt(integration, "audio_prepared");
        var audioPlayed = ReadInt(integration, "audio_played");
        var audioUnmapped = ReadInt(integration, "audio_unmapped");

        var matchTraceIssue = string.Empty;
        var matchReplayIssue = string.Empty;
        var inputTraceIssue = string.Empty;
        var inputReplayIssue = string.Empty;
        var animationTraceIssue = string.Empty;
        var animationReplayIssue = string.Empty;
        var audioTraceIssue = string.Empty;
        var audioReplayIssue = string.Empty;
        var traceFilesValid = ReadTrace(
                spec.MatchTracePath,
                MatchTraceSerializer.Header,
                ReadInt(integration, "match_events"),
                out matchTraceIssue) &&
            ReadTrace(
                spec.MatchReplayPath,
                MatchTraceSerializer.Header,
                ReadInt(integration, "match_events"),
                out matchReplayIssue) &&
            ReadTrace(
                spec.InputTracePath,
                CleanRoomInputTraceSerializer.Header,
                ReadInt(integration, "input_samples"),
                out inputTraceIssue) &&
            ReadTrace(
                spec.InputReplayPath,
                CleanRoomInputTraceSerializer.Header,
                ReadInt(integration, "input_samples"),
                out inputReplayIssue) &&
            ReadTrace(
                spec.AnimationTracePath,
                CleanRoomAuthoredAnimationTraceSerializer.Header,
                animationSamples,
                out animationTraceIssue) &&
            ReadTrace(
                spec.AnimationReplayPath,
                CleanRoomAuthoredAnimationTraceSerializer.Header,
                animationSamples,
                out animationReplayIssue) &&
            ReadTrace(
                spec.AudioTracePath,
                GameplayAudioTraceSerializer.Header,
                audioEvents,
                out audioTraceIssue) &&
            ReadTrace(
                spec.AudioReplayPath,
                GameplayAudioTraceSerializer.Header,
                audioEvents,
                out audioReplayIssue);

        var replayMatched = traceFilesValid &&
            FilesEqual(spec.MatchTracePath, spec.MatchReplayPath) &&
            FilesEqual(spec.InputTracePath, spec.InputReplayPath) &&
            FilesEqual(spec.AnimationTracePath, spec.AnimationReplayPath) &&
            FilesEqual(spec.AudioTracePath, spec.AudioReplayPath);
        var requiredActionsPresent = ReadMatchActions(spec.MatchTracePath)
            .ToHashSet(StringComparer.Ordinal)
            .IsSupersetOf(spec.RequiredActions);
        var frameOutputValid = TryReadPpm(
            spec.FrameOutputPath,
            out var frameVisiblePixels,
            out var frameOutputIssue);

        var issues = new[]
        {
            candidateFlagsValid ? string.Empty : "candidate flags are not enabled in both integration reports",
            provenanceValid ? string.Empty : "candidate report provenance or current assembly hash is invalid",
            candidateAssetProvenanceValid ? string.Empty : "candidate asset provenance summary is missing or invalid",
            candidateMeshStateCrosscheckValid ? string.Empty : "candidate mesh/state structural cross-check is missing or invalid",
            candidateGeometryStreamsComplete ? string.Empty : "candidate geometry command streams are incomplete",
            candidateShapeStateStreamsComplete ? string.Empty : "candidate shape-state command streams are incomplete",
            integrationReportsMatch ? string.Empty : "generated and replay integration reports differ",
            traceFilesValid ? string.Empty : "one or more candidate traces are missing, malformed or count-inconsistent",
            replayMatched ? string.Empty : "one or more candidate replay traces differ",
            requiredActionsPresent ? string.Empty : "candidate match trace is missing one or more required actions",
            frameOutputValid ? string.Empty : frameOutputIssue,
            matchTraceIssue,
            matchReplayIssue,
            inputTraceIssue,
            inputReplayIssue,
            animationTraceIssue,
            animationReplayIssue,
            audioTraceIssue,
            audioReplayIssue,
        }.Where(issue => !string.IsNullOrWhiteSpace(issue));

        var verified = integrationExists &&
            replayIntegrationExists &&
            candidateFlagsValid &&
            provenanceValid &&
            candidateAssetProvenanceValid &&
            candidateMeshStateCrosscheckValid &&
            candidateGeometryStreamsComplete &&
            candidateShapeStateStreamsComplete &&
            integrationReportsMatch &&
            traceFilesValid &&
            replayMatched &&
            requiredActionsPresent &&
            frameOutputValid;
        return new CandidateIntegratedRuntimeAuditResult(
            spec.Name,
            spec.IntegrationReportPath,
            integrationSha256,
            integrationExists,
            spec.ReplayIntegrationReportPath,
            replayIntegrationSha256,
            replayIntegrationExists,
            candidateFlagsValid,
            provenanceValid,
            candidateAssetProvenanceValid,
            candidateMeshStateCrosscheckValid,
            candidateAssetCount,
            candidateGeometryStreamsComplete,
            candidateShapeStateStreamsComplete,
            integrationReportsMatch,
            traceFilesValid,
            replayMatched,
            requiredActionsPresent,
            renderedFrames,
            animationSamples,
            audioEvents,
            audioPrepared,
            audioPlayed,
            audioUnmapped,
            frameOutputValid,
            frameVisiblePixels,
            verified ? "verified-candidate" : "incomplete",
            "not-claimed",
            string.Join("; ", issues));
    }

    public static IReadOnlyList<string> BuildLines(
        CandidateIntegratedRuntimeAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var row = new[]
        {
            "scenario",
            audit.Name,
            audit.IntegrationReportPath,
            audit.IntegrationSha256,
            audit.IntegrationExists.ToString(),
            audit.ReplayIntegrationReportPath,
            audit.ReplayIntegrationSha256,
            audit.ReplayIntegrationExists.ToString(),
            audit.CandidateFlagsValid.ToString(),
            audit.ProvenanceValid.ToString(),
            audit.CandidateAssetProvenanceValid.ToString(),
            audit.CandidateMeshStateCrosscheckValid.ToString(),
            audit.CandidateAssetCount.ToString(CultureInfo.InvariantCulture),
            audit.CandidateGeometryStreamsComplete.ToString(),
            audit.CandidateShapeStateStreamsComplete.ToString(),
            audit.IntegrationReportsMatch.ToString(),
            audit.TraceFilesValid.ToString(),
            audit.ReplayMatched.ToString(),
            audit.RequiredActionsPresent.ToString(),
            audit.RenderedFrames.ToString(CultureInfo.InvariantCulture),
            audit.AnimationSamples.ToString(CultureInfo.InvariantCulture),
            audit.AudioEvents.ToString(CultureInfo.InvariantCulture),
            audit.AudioPrepared.ToString(CultureInfo.InvariantCulture),
            audit.AudioPlayed.ToString(CultureInfo.InvariantCulture),
            audit.AudioUnmapped.ToString(CultureInfo.InvariantCulture),
            audit.FrameOutputValid.ToString(),
            audit.FrameVisiblePixels.ToString(CultureInfo.InvariantCulture),
            audit.Status,
            audit.OriginalSemantics,
            audit.Issue,
        };
        return new[]
        {
            Header,
            string.Join('\t', row.Select(Escape)),
        };
    }

    public static void Write(
        string path,
        CandidateIntegratedRuntimeAuditResult audit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(audit);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            BuildLines(audit),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool HasCandidateFlags(IReadOnlyDictionary<string, string> values) =>
        string.Equals(Get(values, "candidate_material_validated"), "True", StringComparison.Ordinal) &&
        string.Equals(Get(values, "candidate_home_enabled"), "True", StringComparison.Ordinal) &&
        string.Equals(Get(values, "candidate_away_enabled"), "True", StringComparison.Ordinal);

    private static bool IsValidCandidateReport(IReadOnlyDictionary<string, string> values)
    {
        var renderedFrames = ReadInt(values, "rendered_frames");
        var inputSamples = ReadInt(values, "input_samples");
        var animationSamples = ReadInt(values, "animation_samples");
        var audioEvents = ReadInt(values, "audio_events");
        var assemblySha256 = Get(values, CleanRoomIntegrationReport.RuntimeAssemblySha256Field);
        return string.Equals(Get(values, "integration_scope"), "authored-clean-room-runtime", StringComparison.Ordinal) &&
            IsSha256Hex(assemblySha256) &&
            string.Equals(assemblySha256, CleanRoomIntegrationReport.CurrentAssemblySha256, StringComparison.OrdinalIgnoreCase) &&
            HasCandidateFlags(values) &&
            string.Equals(Get(values, "original_semantics"), "not-claimed", StringComparison.Ordinal) &&
            renderedFrames > 0 &&
            inputSamples == renderedFrames &&
            animationSamples == renderedFrames * 2 &&
            audioEvents > 0 &&
            ReadInt(values, "audio_prepared") == audioEvents &&
            ReadInt(values, "audio_played") == audioEvents &&
            ReadInt(values, "audio_unmapped") == 0 &&
            IsSha256Hex(Get(values, "gameplay_audio_pcm_sha256")) &&
            ReadLong(values, "gameplay_audio_pcm_bytes") > 0 &&
            string.Equals(Get(values, "match_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(values, "input_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(values, "animation_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(values, "audio_trace_written"), "True", StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ReadKeyValueTsv(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var fields = line.Split('\t', 2);
            if (fields.Length == 2)
            {
                values[fields[0]] = fields[1];
            }
        }

        return values;
    }

    private static bool ReadTrace(
        string path,
        string expectedHeader,
        int expectedDataRows,
        out string issue)
    {
        issue = string.Empty;
        if (!File.Exists(path))
        {
            issue = $"missing trace: {Path.GetFileName(path)}";
            return false;
        }

        var lines = File.ReadAllLines(path);
        if (lines.Length == 0 || !string.Equals(lines[0], expectedHeader, StringComparison.Ordinal))
        {
            issue = $"invalid trace header: {Path.GetFileName(path)}";
            return false;
        }

        var dataRows = lines.Skip(1).Count(line => !string.IsNullOrWhiteSpace(line));
        if (dataRows != expectedDataRows)
        {
            issue = $"trace row count mismatch: {Path.GetFileName(path)} expected={expectedDataRows} actual={dataRows}";
            return false;
        }

        return dataRows > 0;
    }

    private static IReadOnlyList<string> ReadMatchActions(string path)
    {
        if (!File.Exists(path))
        {
            return Array.Empty<string>();
        }

        return File.ReadLines(path)
            .Skip(1)
            .Select(line => line.Split('\t'))
            .Where(fields => fields.Length >= 3)
            .Select(fields => fields[2])
            .Where(action => !string.IsNullOrWhiteSpace(action))
            .ToArray();
    }

    private static bool FilesEqual(string firstPath, string secondPath) =>
        File.Exists(firstPath) &&
        File.Exists(secondPath) &&
        string.Equals(ComputeSha256(firstPath), ComputeSha256(secondPath), StringComparison.OrdinalIgnoreCase);

    private static bool TryReadPpm(
        string path,
        out int visiblePixels,
        out string issue)
    {
        visiblePixels = 0;
        issue = string.Empty;
        if (!File.Exists(path))
        {
            issue = $"missing frame output: {Path.GetFileName(path)}";
            return false;
        }

        try
        {
            var bytes = File.ReadAllBytes(path);
            var offset = 0;
            if (!TryReadPpmToken(bytes, ref offset, out var magic) || magic != "P6" ||
                !TryReadPpmToken(bytes, ref offset, out var widthText) ||
                !TryReadPpmToken(bytes, ref offset, out var heightText) ||
                !TryReadPpmToken(bytes, ref offset, out var maximumText) ||
                !int.TryParse(widthText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width) ||
                !int.TryParse(heightText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var height) ||
                !int.TryParse(maximumText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var maximum) ||
                width <= 0 || height <= 0 || maximum != 255)
            {
                issue = $"invalid P6 frame header: {Path.GetFileName(path)}";
                return false;
            }

            while (offset < bytes.Length && bytes[offset] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                offset++;
            }

            var payloadLength = checked(width * height * 3L);
            if (payloadLength <= 0 || bytes.Length - offset < payloadLength)
            {
                issue = $"truncated P6 frame payload: {Path.GetFileName(path)}";
                return false;
            }

            var payloadEnd = checked(offset + (int)payloadLength);
            for (var index = offset; index < payloadEnd; index += 3)
            {
                if (bytes[index] != 0 || bytes[index + 1] != 0 || bytes[index + 2] != 0)
                {
                    visiblePixels++;
                }
            }

            if (visiblePixels == 0)
            {
                issue = $"frame has no non-black pixels: {Path.GetFileName(path)}";
                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or OverflowException)
        {
            issue = $"frame read failed: {exception.Message}";
            return false;
        }
    }

    private static bool TryReadPpmToken(
        byte[] bytes,
        ref int offset,
        out string token)
    {
        while (offset < bytes.Length)
        {
            if (bytes[offset] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            {
                offset++;
                continue;
            }

            if (bytes[offset] == (byte)'#')
            {
                while (offset < bytes.Length && bytes[offset] != (byte)'\n')
                {
                    offset++;
                }

                continue;
            }

            break;
        }

        var start = offset;
        while (offset < bytes.Length && bytes[offset] is not ((byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n'))
        {
            offset++;
        }

        token = start == offset
            ? string.Empty
            : Encoding.ASCII.GetString(bytes, start, offset - start);
        return token.Length > 0;
    }

    private static int ReadInt(IReadOnlyDictionary<string, string> values, string key) =>
        int.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static long ReadLong(IReadOnlyDictionary<string, string> values, string key) =>
        long.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static string Get(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : string.Empty;

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(character => Uri.IsHexDigit(character));

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string Escape(string value) =>
        (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
