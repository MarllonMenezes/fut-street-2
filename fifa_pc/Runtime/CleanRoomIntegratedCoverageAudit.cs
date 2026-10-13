using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record CleanRoomIntegratedCoverageScenarioSpec(
    string Name,
    string IntegrationReportPath,
    string MatchTracePath,
    string MatchReplayPath,
    string InputTracePath,
    string InputReplayPath,
    string AnimationTracePath,
    string AnimationReplayPath,
    string AudioTracePath,
    string AudioReplayPath,
    IReadOnlyList<string> RequiredActions,
    string FrameOutputPath = "");

public sealed record CleanRoomIntegratedCoverageScenarioResult(
    string Name,
    string IntegrationReportPath,
    string IntegrationSha256,
    bool IntegrationExists,
    bool IntegrationValid,
    int RenderedFrames,
    int AuthoredCharacterAssetFrames,
    int AuthoredSceneAssetFrames,
    int AnimationSamples,
    int AudioEvents,
    int AudioPrepared,
    int AudioPlayed,
    int AudioUnmapped,
    bool TraceFilesValid,
    bool ReplayMatched,
    bool RequiredActionsPresent,
    string RequiredActions,
    string MissingActions,
    string Status,
    string Issue,
    string FrameOutputPath = "",
    string FrameOutputSha256 = "",
    bool FrameOutputValid = false,
    int FrameVisiblePixels = 0);

public sealed record CleanRoomIntegratedCoverageSummary(
    int ScenarioCount,
    int VerifiedScenarioCount,
    int TotalRenderedFrames,
    int TotalAudioEvents,
    int TotalAnimationSamples,
    string Status,
    string OriginalSemantics);

public sealed record CleanRoomIntegratedCoverageAuditResult(
    CleanRoomIntegratedCoverageSummary Summary,
    IReadOnlyList<CleanRoomIntegratedCoverageScenarioResult> Scenarios);

/// <summary>
/// Verifies that each authored clean-room scenario exercises the managed
/// renderer, authored character/scene assets, authored procedural animation,
/// authored audio and deterministic traces together. It is a PC-runtime
/// integration gate only; it is not evidence of original asset or game
/// semantics.
/// </summary>
public static class CleanRoomIntegratedCoverageAudit
{
    public const string Header =
        "kind\tname\tintegration_report\tintegration_sha256\tintegration_exists\tintegration_valid\trendered_frames\tauthored_character_asset_frames\tauthored_scene_asset_frames\tanimation_samples\taudio_events\taudio_prepared\taudio_played\taudio_unmapped\ttrace_files_valid\treplay_matched\trequired_actions_present\trequired_actions\tmissing_actions\tstatus\tscenario_count\tverified_scenario_count\ttotal_rendered_frames\ttotal_audio_events\ttotal_animation_samples\toriginal_semantics\tissue\tframe_output_path\tframe_output_sha256\tframe_output_valid\tframe_visible_pixels";

    public static IReadOnlyList<CleanRoomIntegratedCoverageScenarioSpec> DefaultScenarios(
        string workspaceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        var root = Path.Combine(Path.GetFullPath(workspaceDirectory), "fifa_pc");
        return new[]
        {
            Spec(root, "full-stack", "KICKOFF", "SHOT_AWAY", "GOAL_AWAY"),
            Spec(root, "aimed", "KICKOFF", "SHOT", "GOAL_HOME"),
            Spec(root, "round", "KICKOFF", "GOAL_HOME", "RESTART"),
            Spec(root, "gamebreaker", "KICKOFF", "GAMEBREAKER", "GOAL_HOME"),
            Spec(root, "juggle", "KICKOFF", "JUGGLE", "SHOT", "GOAL_HOME"),
            Spec(root, "panna", "KICKOFF", "PANNA", "FULL_TIME"),
            Spec(root, "full-time", "KICKOFF", "FULL_TIME"),
            Spec(root, "steal", "KICKOFF", "STEAL"),
            Spec(root, "tackle", "KICKOFF", "STEAL", "TACKLE"),
            Spec(root, "first-to-goals", "KICKOFF", "GOAL_HOME", "FULL_TIME"),
        };
    }

    public static CleanRoomIntegratedCoverageAuditResult Audit(
        IEnumerable<CleanRoomIntegratedCoverageScenarioSpec> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        var rows = scenarios.Select(AuditScenario).ToArray();
        var verifiedScenarioCount = rows.Count(row => row.Status == "verified-authored");
        var summary = new CleanRoomIntegratedCoverageSummary(
            rows.Length,
            verifiedScenarioCount,
            rows.Sum(row => row.RenderedFrames),
            rows.Sum(row => row.AudioEvents),
            rows.Sum(row => row.AnimationSamples),
            rows.Length > 0 && verifiedScenarioCount == rows.Length
                ? "verified-authored"
                : "incomplete",
            "not-claimed");
        return new CleanRoomIntegratedCoverageAuditResult(summary, rows);
    }

    public static IReadOnlyList<string> BuildLines(
        CleanRoomIntegratedCoverageAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var summary = audit.Summary;
        var lines = new List<string> { Header };
        lines.AddRange(audit.Scenarios.Select(row => string.Join('\t', new[]
        {
            "scenario",
            row.Name,
            row.IntegrationReportPath,
            row.IntegrationSha256,
            row.IntegrationExists.ToString(),
            row.IntegrationValid.ToString(),
            row.RenderedFrames.ToString(CultureInfo.InvariantCulture),
            row.AuthoredCharacterAssetFrames.ToString(CultureInfo.InvariantCulture),
            row.AuthoredSceneAssetFrames.ToString(CultureInfo.InvariantCulture),
            row.AnimationSamples.ToString(CultureInfo.InvariantCulture),
            row.AudioEvents.ToString(CultureInfo.InvariantCulture),
            row.AudioPrepared.ToString(CultureInfo.InvariantCulture),
            row.AudioPlayed.ToString(CultureInfo.InvariantCulture),
            row.AudioUnmapped.ToString(CultureInfo.InvariantCulture),
            row.TraceFilesValid.ToString(),
            row.ReplayMatched.ToString(),
            row.RequiredActionsPresent.ToString(),
            row.RequiredActions,
            row.MissingActions,
            row.Status,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "not-claimed",
            row.Issue,
            row.FrameOutputPath,
            row.FrameOutputSha256,
            row.FrameOutputValid.ToString(),
            row.FrameVisiblePixels.ToString(CultureInfo.InvariantCulture),
        }.Select(Escape))));
        lines.Add(string.Join('\t', new[]
        {
            "summary",
            "all",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            summary.Status,
            summary.ScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.VerifiedScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.TotalRenderedFrames.ToString(CultureInfo.InvariantCulture),
            summary.TotalAudioEvents.ToString(CultureInfo.InvariantCulture),
            summary.TotalAnimationSamples.ToString(CultureInfo.InvariantCulture),
            summary.OriginalSemantics,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
        }.Select(Escape)));
        return lines.AsReadOnly();
    }

    public static void Write(
        string path,
        CleanRoomIntegratedCoverageAuditResult audit)
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

    private static CleanRoomIntegratedCoverageScenarioSpec Spec(
        string root,
        string name,
        params string[] requiredActions)
    {
        var prefix = $"gameplay-authored-{name}";
        return new CleanRoomIntegratedCoverageScenarioSpec(
            name,
            Path.Combine(root, $"{prefix}-integration.tsv"),
            Path.Combine(root, $"{prefix}-match-trace.tsv"),
            Path.Combine(root, $"{prefix}-match-replayed.tsv"),
            Path.Combine(root, $"{prefix}-input-trace.tsv"),
            Path.Combine(root, $"{prefix}-input-replayed.tsv"),
            Path.Combine(root, $"{prefix}-animation-trace.tsv"),
            Path.Combine(root, $"{prefix}-animation-replayed.tsv"),
            Path.Combine(root, $"{prefix}-audio-trace.tsv"),
            Path.Combine(root, $"{prefix}-audio-replayed.tsv"),
            requiredActions,
            Path.Combine(root, $"{prefix}-frame.ppm"));
    }

    private static CleanRoomIntegratedCoverageScenarioResult AuditScenario(
        CleanRoomIntegratedCoverageScenarioSpec spec)
    {
        var integrationExists = File.Exists(spec.IntegrationReportPath);
        var integrationSha256 = integrationExists
            ? ComputeSha256(spec.IntegrationReportPath)
            : string.Empty;
        var integration = integrationExists
            ? ReadKeyValueTsv(spec.IntegrationReportPath)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var renderedFrames = ReadInt(integration, "rendered_frames");
        var authoredCharacterAssetFrames = ReadInt(integration, "authored_character_asset_frames");
        var authoredSceneAssetFrames = ReadInt(integration, "authored_scene_asset_frames");
        var animationSamples = ReadInt(integration, "animation_samples");
        var audioEvents = ReadInt(integration, "audio_events");
        var audioPrepared = ReadInt(integration, "audio_prepared");
        var audioPlayed = ReadInt(integration, "audio_played");
        var audioUnmapped = ReadInt(integration, "audio_unmapped");

        var integrationValid = integrationExists &&
            string.Equals(Get(integration, "integration_scope"), "authored-clean-room-runtime", StringComparison.Ordinal) &&
            string.Equals(
                Get(integration, CleanRoomIntegrationReport.RuntimeAssemblySha256Field),
                CleanRoomIntegrationReport.CurrentAssemblySha256,
                StringComparison.OrdinalIgnoreCase) &&
            IsSha256Hex(CleanRoomIntegrationReport.CurrentAssemblySha256) &&
            string.Equals(Get(integration, "candidate_home_enabled"), "False", StringComparison.Ordinal) &&
            string.Equals(Get(integration, "candidate_away_enabled"), "False", StringComparison.Ordinal) &&
            renderedFrames > 0 &&
            authoredCharacterAssetFrames == renderedFrames &&
            authoredSceneAssetFrames == renderedFrames &&
            animationSamples == renderedFrames * 2 &&
            audioEvents > 0 &&
            audioPrepared == audioEvents &&
            audioPlayed == audioEvents &&
            audioUnmapped == 0 &&
            ReadLong(integration, "gameplay_audio_pcm_bytes") > 0 &&
            IsSha256Hex(Get(integration, "gameplay_audio_pcm_sha256")) &&
            string.Equals(Get(integration, "authored_character_asset_manifest"), CleanRoomCharacterAssetManifest.Default.ManifestId, StringComparison.Ordinal) &&
            string.Equals(Get(integration, "authored_scene_asset_manifest"), CleanRoomSceneAssetManifest.Default.ManifestId, StringComparison.Ordinal) &&
            string.Equals(Get(integration, "authored_animation_asset_manifest"), CleanRoomAnimationAssetManifest.Default.ManifestId, StringComparison.Ordinal) &&
            string.Equals(Get(integration, "authored_audio_asset_manifest"), CleanRoomAudioAssetManifest.Default.ManifestId, StringComparison.Ordinal) &&
            string.Equals(Get(integration, "match_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integration, "input_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integration, "animation_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integration, "audio_trace_written"), "True", StringComparison.Ordinal) &&
            string.Equals(Get(integration, "original_semantics"), "not-claimed", StringComparison.Ordinal);

        var matchTrace = ReadMatchTrace(spec.MatchTracePath, out var actions, out var matchTraceIssue);
        var matchReplay = ReadTrace(spec.MatchReplayPath, MatchTraceSerializer.Header, out var matchReplayIssue);
        var inputTrace = ReadTrace(spec.InputTracePath, CleanRoomInputTraceSerializer.Header, out var inputTraceIssue);
        var inputReplay = ReadTrace(spec.InputReplayPath, CleanRoomInputTraceSerializer.Header, out var inputReplayIssue);
        var animationTrace = ReadTrace(spec.AnimationTracePath, CleanRoomAuthoredAnimationTraceSerializer.Header, out var animationTraceIssue);
        var animationReplay = ReadTrace(spec.AnimationReplayPath, CleanRoomAuthoredAnimationTraceSerializer.Header, out var animationReplayIssue);
        var audioTrace = ReadTrace(spec.AudioTracePath, GameplayAudioTraceSerializer.Header, out var audioTraceIssue);
        var audioReplay = ReadTrace(spec.AudioReplayPath, GameplayAudioTraceSerializer.Header, out var audioReplayIssue);
        var frameOutputRequested = !string.IsNullOrWhiteSpace(spec.FrameOutputPath);
        var frameOutputSha256 = string.Empty;
        var frameVisiblePixels = 0;
        var frameOutputIssue = string.Empty;
        var frameOutputValid = true;
        if (frameOutputRequested)
        {
            frameOutputValid = TryReadFrameOutput(
                spec.FrameOutputPath,
                out frameOutputSha256,
                out frameVisiblePixels,
                out frameOutputIssue);
        }
        var traceFilesValid = matchTrace && matchReplay && inputTrace && inputReplay && animationTrace && animationReplay && audioTrace && audioReplay &&
            CountRows(spec.MatchTracePath) == ReadInt(integration, "match_events") &&
            CountRows(spec.InputTracePath) == ReadInt(integration, "input_samples") &&
            CountRows(spec.AnimationTracePath) == animationSamples &&
            CountRows(spec.AudioTracePath) == audioEvents;
        var replayMatched = traceFilesValid &&
            FilesEqual(spec.MatchTracePath, spec.MatchReplayPath) &&
            FilesEqual(spec.InputTracePath, spec.InputReplayPath) &&
            FilesEqual(spec.AnimationTracePath, spec.AnimationReplayPath) &&
            FilesEqual(spec.AudioTracePath, spec.AudioReplayPath);
        var coveredActions = actions.ToHashSet(StringComparer.Ordinal);
        var missingActions = spec.RequiredActions
            .Where(action => !coveredActions.Contains(action))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var requiredActionsPresent = missingActions.Length == 0;
        var verified = integrationValid && traceFilesValid && replayMatched && requiredActionsPresent && frameOutputValid;
        var issues = new[]
        {
            integrationValid ? string.Empty : "integration report does not satisfy the authored cross-system contract",
            traceFilesValid ? string.Empty : "one or more authored traces are missing, malformed or count-inconsistent",
            replayMatched ? string.Empty : "one or more authored replays differ",
            requiredActionsPresent ? string.Empty : $"missing required actions: {string.Join(',', missingActions)}",
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
        return new CleanRoomIntegratedCoverageScenarioResult(
            spec.Name,
            spec.IntegrationReportPath,
            integrationSha256,
            integrationExists,
            integrationValid,
            renderedFrames,
            authoredCharacterAssetFrames,
            authoredSceneAssetFrames,
            animationSamples,
            audioEvents,
            audioPrepared,
            audioPlayed,
            audioUnmapped,
            traceFilesValid,
            replayMatched,
            requiredActionsPresent,
            string.Join(',', spec.RequiredActions),
            string.Join(',', missingActions),
            verified ? "verified-authored" : "incomplete",
            string.Join("; ", issues),
            spec.FrameOutputPath,
            frameOutputSha256,
            frameOutputValid,
            frameVisiblePixels);
    }

    private static bool ReadMatchTrace(
        string path,
        out IReadOnlyList<string> actions,
        out string issue)
    {
        actions = Array.Empty<string>();
        if (!ReadTrace(path, MatchTraceSerializer.Header, out issue))
        {
            return false;
        }

        var parsed = new List<string>();
        foreach (var line in File.ReadLines(path).Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length != 6 || string.IsNullOrWhiteSpace(fields[2]))
            {
                issue = "match trace row shape is invalid";
                return false;
            }
            parsed.Add(fields[2]);
        }
        actions = parsed;
        issue = parsed.Count == 0 ? "match trace has no events" : string.Empty;
        return parsed.Count > 0;
    }

    private static bool ReadTrace(string path, string expectedHeader, out string issue)
    {
        if (!File.Exists(path))
        {
            issue = "trace is missing";
            return false;
        }
        var lines = File.ReadAllLines(path);
        if (lines.Length < 2 || !string.Equals(lines[0], expectedHeader, StringComparison.Ordinal))
        {
            issue = "trace header or rows are invalid";
            return false;
        }
        issue = string.Empty;
        return true;
    }

    private static bool TryReadFrameOutput(
        string path,
        out string sha256,
        out int visiblePixels,
        out string issue)
    {
        sha256 = string.Empty;
        visiblePixels = 0;
        issue = string.Empty;
        if (!File.Exists(path))
        {
            issue = "authored frame output is missing";
            return false;
        }

        var bytes = File.ReadAllBytes(path);
        var header = Encoding.ASCII.GetBytes("P6\n960 540\n255\n");
        var expectedLength = checked(header.Length + 960 * 540 * 3);
        if (bytes.Length != expectedLength ||
            !bytes.AsSpan(0, Math.Min(header.Length, bytes.Length)).SequenceEqual(header))
        {
            issue = "authored frame output has an unexpected PPM header or payload length";
            return false;
        }

        var payloadOffset = header.Length;
        for (var offset = payloadOffset; offset < bytes.Length; offset += 3)
        {
            if (bytes[offset] != 18 || bytes[offset + 1] != 24 || bytes[offset + 2] != 38)
            {
                visiblePixels++;
            }
        }

        sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        if (visiblePixels == 0)
        {
            issue = "authored frame output contains only the clear color";
            return false;
        }
        return true;
    }

    private static int CountRows(string path) =>
        File.Exists(path) ? Math.Max(0, File.ReadLines(path).Count() - 1) : 0;

    private static bool FilesEqual(string left, string right)
    {
        if (!File.Exists(left) || !File.Exists(right))
        {
            return false;
        }
        return File.ReadAllBytes(left).AsSpan().SequenceEqual(File.ReadAllBytes(right));
    }

    private static Dictionary<string, string> ReadKeyValueTsv(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0 || !string.Equals(lines[0], CleanRoomIntegrationReport.Header, StringComparison.Ordinal))
        {
            return values;
        }
        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length == 2 && !values.ContainsKey(fields[0]))
            {
                values.Add(fields[0], fields[1]);
            }
        }
        return values;
    }

    private static string Get(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : string.Empty;

    private static int ReadInt(IReadOnlyDictionary<string, string> values, string key) =>
        int.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static long ReadLong(IReadOnlyDictionary<string, string> values, string key) =>
        long.TryParse(Get(values, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : 0;

    private static bool IsSha256Hex(string value) =>
        value.Length == 64 && value.All(IsHexDigit);

    private static bool IsHexDigit(char value) =>
        value is >= '0' and <= '9' or >= 'A' and <= 'F' or >= 'a' and <= 'f';

    private static string ComputeSha256(string path)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static string Escape(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
