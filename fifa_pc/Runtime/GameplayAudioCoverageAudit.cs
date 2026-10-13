using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record GameplayAudioCoverageScenarioSpec(
    string Name,
    string TracePath,
    string ReplayPath);

public sealed record GameplayAudioCoverageScenarioResult(
    string Name,
    string TracePath,
    string ReplayPath,
    bool TraceExists,
    bool ReplayExists,
    bool TraceValid,
    bool ReplayMatched,
    int EventCount,
    int PreparedCount,
    int PlayedCount,
    int UnmappedCount,
    bool AllPrepared,
    bool AllPlayed,
    string Actions,
    string Candidates,
    string TraceSha256,
    string ReplaySha256,
    string Status,
    string Issue);

public sealed record GameplayAudioCoverageSummary(
    int ScenarioCount,
    int VerifiedScenarioCount,
    int TotalEvents,
    int TotalPrepared,
    int TotalPlayed,
    int TotalUnmapped,
    string CoveredPolicyActions,
    string MissingPolicyActions,
    int UniqueCandidateCount,
    string Status,
    string OriginalSemantics);

public sealed record GameplayAudioCoverageAuditResult(
    GameplayAudioCoverageSummary Summary,
    IReadOnlyList<GameplayAudioCoverageScenarioResult> Scenarios);

/// <summary>
/// Audits authored gameplay-to-audio traces across the implemented scenarios.
/// This is a runtime coverage report only: candidate names and routes are the
/// clean-room policy and are not decoded original event or sample bindings.
/// </summary>
public static class GameplayAudioCoverageAudit
{
    public const string Header =
        "kind\tname\ttrace_path\treplay_path\ttrace_sha256\treplay_sha256\ttrace_exists\treplay_exists\ttrace_valid\treplay_matched\tevent_count\tprepared_count\tplayed_count\tunmapped_count\tall_prepared\tall_played\tactions\tcandidates\tstatus\tcovered_policy_actions\tmissing_policy_actions\tunique_candidate_count\tscenario_count\tverified_scenario_count\ttotal_events\ttotal_prepared\ttotal_played\ttotal_unmapped\toriginal_semantics\tissue";

    public static IReadOnlyList<GameplayAudioCoverageScenarioSpec> DefaultScenarios(
        string workspaceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        var root = Path.Combine(Path.GetFullPath(workspaceDirectory), "fifa_pc");
        return new[]
        {
            Spec(root, "full-stack", "gameplay-authored-full-stack-audio-trace.tsv", "gameplay-authored-full-stack-audio-replayed.tsv"),
            Spec(root, "aimed", "gameplay-authored-aimed-audio-trace.tsv", "gameplay-authored-aimed-audio-replayed.tsv"),
            Spec(root, "round", "gameplay-authored-round-audio-trace.tsv", "gameplay-authored-round-audio-replayed.tsv"),
            Spec(root, "gamebreaker", "gameplay-authored-gamebreaker-audio-trace.tsv", "gameplay-authored-gamebreaker-audio-replayed.tsv"),
            Spec(root, "juggle", "gameplay-authored-juggle-audio-trace.tsv", "gameplay-authored-juggle-audio-replayed.tsv"),
            Spec(root, "panna", "gameplay-authored-panna-audio-trace.tsv", "gameplay-authored-panna-audio-replayed.tsv"),
            Spec(root, "full-time", "gameplay-authored-full-time-audio-trace.tsv", "gameplay-authored-full-time-audio-replayed.tsv"),
            Spec(root, "steal", "gameplay-authored-steal-audio-trace.tsv", "gameplay-authored-steal-audio-replayed.tsv"),
            Spec(root, "tackle", "gameplay-authored-tackle-audio-trace.tsv", "gameplay-authored-tackle-audio-replayed.tsv"),
            Spec(root, "first-to-goals", "gameplay-authored-first-to-goals-audio-trace.tsv", "gameplay-authored-first-to-goals-audio-replayed.tsv"),
        };
    }

    public static GameplayAudioCoverageAuditResult Audit(
        IEnumerable<GameplayAudioCoverageScenarioSpec> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        var rows = scenarios.Select(AuditScenario).ToArray();
        var expectedActions = CleanRoomAudioCuePolicy.Routes
            .Select(route => route.Action)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var coveredActions = rows
            .SelectMany(row => SplitValues(row.Actions))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var missingActions = expectedActions
            .Except(coveredActions, StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var verifiedScenarioCount = rows.Count(row =>
            row.Status == "verified-authored");
        var summaryStatus = rows.Length > 0 &&
                            verifiedScenarioCount == rows.Length &&
                            missingActions.Length == 0
            ? "verified-authored"
            : "incomplete";
        var uniqueCandidates = rows
            .SelectMany(row => SplitValues(row.Candidates))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();
        var summary = new GameplayAudioCoverageSummary(
            rows.Length,
            verifiedScenarioCount,
            rows.Sum(row => row.EventCount),
            rows.Sum(row => row.PreparedCount),
            rows.Sum(row => row.PlayedCount),
            rows.Sum(row => row.UnmappedCount),
            string.Join(",", coveredActions),
            string.Join(",", missingActions),
            uniqueCandidates,
            summaryStatus,
            "not-claimed");
        return new GameplayAudioCoverageAuditResult(summary, rows);
    }

    public static IReadOnlyList<string> BuildLines(GameplayAudioCoverageAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var summary = audit.Summary;
        var lines = new List<string> { Header };
        lines.AddRange(audit.Scenarios.Select(row => string.Join('\t', new[]
        {
            "scenario",
            row.Name,
            row.TracePath,
            row.ReplayPath,
            row.TraceSha256,
            row.ReplaySha256,
            row.TraceExists.ToString(),
            row.ReplayExists.ToString(),
            row.TraceValid.ToString(),
            row.ReplayMatched.ToString(),
            row.EventCount.ToString(CultureInfo.InvariantCulture),
            row.PreparedCount.ToString(CultureInfo.InvariantCulture),
            row.PlayedCount.ToString(CultureInfo.InvariantCulture),
            row.UnmappedCount.ToString(CultureInfo.InvariantCulture),
            row.AllPrepared.ToString(),
            row.AllPlayed.ToString(),
            row.Actions,
            row.Candidates,
            row.Status,
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
            row.Issue,
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
            summary.Status,
            summary.CoveredPolicyActions,
            summary.MissingPolicyActions,
            summary.UniqueCandidateCount.ToString(CultureInfo.InvariantCulture),
            summary.ScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.VerifiedScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.TotalEvents.ToString(CultureInfo.InvariantCulture),
            summary.TotalPrepared.ToString(CultureInfo.InvariantCulture),
            summary.TotalPlayed.ToString(CultureInfo.InvariantCulture),
            summary.TotalUnmapped.ToString(CultureInfo.InvariantCulture),
            summary.OriginalSemantics,
            string.Empty,
        }.Select(Escape)));
        return lines.AsReadOnly();
    }

    public static void Write(string path, GameplayAudioCoverageAuditResult audit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(audit);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllLines(
            path,
            BuildLines(audit),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static GameplayAudioCoverageScenarioSpec Spec(
        string root,
        string name,
        string traceName,
        string replayName) =>
        new(name, Path.Combine(root, traceName), Path.Combine(root, replayName));

    private static GameplayAudioCoverageScenarioResult AuditScenario(
        GameplayAudioCoverageScenarioSpec spec)
    {
        var traceExists = File.Exists(spec.TracePath);
        var replayExists = File.Exists(spec.ReplayPath);
        if (!traceExists || !replayExists)
        {
            return new GameplayAudioCoverageScenarioResult(
                spec.Name,
                spec.TracePath,
                spec.ReplayPath,
                traceExists,
                replayExists,
                false,
                false,
                0,
                0,
                0,
                0,
                false,
                false,
                string.Empty,
                string.Empty,
                traceExists ? ComputeSha256(spec.TracePath) : string.Empty,
                replayExists ? ComputeSha256(spec.ReplayPath) : string.Empty,
                "missing",
                !traceExists && !replayExists
                    ? "trace and replay are missing"
                    : !traceExists
                        ? "trace is missing"
                        : "replay is missing");
        }

        var trace = TryRead(spec.TracePath, out var traceRoutes, out var traceIssue);
        var replay = TryRead(spec.ReplayPath, out _, out var replayIssue);
        var replayMatched = trace &&
                            replay &&
                            GameplayAudioTraceSerializer.FindFirstDifference(
                                File.ReadAllLines(spec.TracePath),
                                File.ReadAllLines(spec.ReplayPath)) < 0;
        if (!trace || !replay)
        {
            return new GameplayAudioCoverageScenarioResult(
                spec.Name,
                spec.TracePath,
                spec.ReplayPath,
                true,
                true,
                trace,
                replayMatched,
                0,
                0,
                0,
                0,
                false,
                false,
                string.Empty,
                string.Empty,
                ComputeSha256(spec.TracePath),
                ComputeSha256(spec.ReplayPath),
                "invalid",
                string.Join("; ", new[] { traceIssue, replayIssue }
                    .Where(issue => !string.IsNullOrWhiteSpace(issue))));
        }

        var eventCount = traceRoutes.Count;
        var preparedCount = traceRoutes.Count(route => IsPrepared(route.Route));
        var playedCount = traceRoutes.Count(route => IsPlayed(route.Route));
        var unmappedCount = traceRoutes.Count(route =>
            route.Route.Contains("unmapped", StringComparison.OrdinalIgnoreCase) ||
            route.Route.Contains("unavailable", StringComparison.OrdinalIgnoreCase) ||
            route.Route.Contains("rejected", StringComparison.OrdinalIgnoreCase));
        var allPrepared = eventCount > 0 && preparedCount == eventCount;
        var allPlayed = eventCount > 0 && playedCount == eventCount;
        var status = replayMatched && allPrepared && allPlayed && unmappedCount == 0
            ? "verified-authored"
            : "incomplete";
        return new GameplayAudioCoverageScenarioResult(
            spec.Name,
            spec.TracePath,
            spec.ReplayPath,
            true,
            true,
            true,
            replayMatched,
            eventCount,
            preparedCount,
            playedCount,
            unmappedCount,
            allPrepared,
            allPlayed,
            string.Join(",", traceRoutes.Select(route => route.Action).Distinct(StringComparer.Ordinal)),
            string.Join(",", traceRoutes.Select(route => route.CandidateName).Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase)),
            ComputeSha256(spec.TracePath),
            ComputeSha256(spec.ReplayPath),
            status,
            status == "verified-authored"
                ? string.Empty
                : "replay, preparation, playback or route coverage is incomplete");
    }

    private static bool TryRead(
        string path,
        out IReadOnlyList<CleanRoomGameplayAudioRoute> routes,
        out string issue)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0 || !string.Equals(lines[0], GameplayAudioTraceSerializer.Header, StringComparison.Ordinal))
        {
            routes = Array.Empty<CleanRoomGameplayAudioRoute>();
            issue = "trace header is invalid";
            return false;
        }

        var parsed = new List<CleanRoomGameplayAudioRoute>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }
            var fields = lines[lineIndex].Split('\t');
            if (fields.Length != 4 && fields.Length != 7 ||
                !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sequence))
            {
                routes = Array.Empty<CleanRoomGameplayAudioRoute>();
                issue = $"invalid trace row at line {lineIndex + 1}";
                return false;
            }

            var waveBytes = 0;
            long pcmBytes = 0;
            var pcmSha256 = string.Empty;
            if (fields.Length == 7 &&
                (!int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out waveBytes) ||
                 !long.TryParse(fields[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out pcmBytes)))
            {
                routes = Array.Empty<CleanRoomGameplayAudioRoute>();
                issue = $"invalid audio payload identity at line {lineIndex + 1}";
                return false;
            }
            if (fields.Length == 7)
            {
                pcmSha256 = fields[6];
            }
            parsed.Add(new CleanRoomGameplayAudioRoute(
                sequence,
                fields[1],
                fields[2],
                fields[3],
                waveBytes,
                pcmBytes,
                pcmSha256));
        }

        routes = parsed.AsReadOnly();
        issue = string.Empty;
        return true;
    }

    private static bool IsPrepared(string route) =>
        route.StartsWith("prepared-", StringComparison.OrdinalIgnoreCase) ||
        route.StartsWith("played-", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlayed(string route) =>
        route.StartsWith("played-", StringComparison.OrdinalIgnoreCase);

    private static IEnumerable<string> SplitValues(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string ComputeSha256(string path)
    {
        using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Convert.ToHexString(SHA256.HashData(input));
    }

    private static string Escape(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
