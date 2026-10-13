using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record GameplayCoverageScenarioSpec(
    string Name,
    string TracePath,
    string ReplayPath,
    IReadOnlyList<string> RequiredActions);

public sealed record GameplayCoverageScenarioResult(
    string Name,
    string TracePath,
    string ReplayPath,
    bool TraceExists,
    bool ReplayExists,
    bool TraceValid,
    bool ReplayMatched,
    bool RequiredActionsPresent,
    int EventCount,
    string Actions,
    string MissingActions,
    string TraceSha256,
    string ReplaySha256,
    string Status,
    string Issue);

public sealed record GameplayCoverageSummary(
    int ScenarioCount,
    int VerifiedScenarioCount,
    int TotalEvents,
    string CoveredActions,
    string MissingActions,
    string Status,
    string OriginalSemantics);

public sealed record GameplayCoverageAuditResult(
    GameplayCoverageSummary Summary,
    IReadOnlyList<GameplayCoverageScenarioResult> Scenarios);

/// <summary>
/// Audits authored match traces across the implemented clean-room scenarios.
/// It validates trace/replay identity and required authored action coverage only;
/// it is not evidence of original FIFA Street rules or event semantics.
/// </summary>
public static class GameplayCoverageAudit
{
    public const string Header =
        "kind\tname\ttrace_path\treplay_path\ttrace_sha256\treplay_sha256\ttrace_exists\treplay_exists\ttrace_valid\treplay_matched\trequired_actions_present\tevent_count\tactions\tmissing_actions\tstatus\tscenario_count\tverified_scenario_count\ttotal_events\tcovered_actions\tall_missing_actions\toriginal_semantics\tissue";

    public static IReadOnlyList<GameplayCoverageScenarioSpec> DefaultScenarios(
        string workspaceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        var root = Path.Combine(Path.GetFullPath(workspaceDirectory), "fifa_pc");
        return new[]
        {
            Spec(root, "full-stack", "gameplay-full-stack-match-trace.tsv", "gameplay-full-stack-match-replayed.tsv", "KICKOFF", "SHOT_AWAY", "GOAL_AWAY"),
            Spec(root, "aimed", "gameplay-clean-room-aimed-trace.tsv", "gameplay-clean-room-aimed-replay.tsv", "KICKOFF", "SHOT", "GOAL_HOME"),
            Spec(root, "round", "gameplay-script-round-trace.tsv", "gameplay-script-round-replayed.tsv", "KICKOFF", "GOAL_HOME", "RESTART"),
            Spec(root, "gamebreaker", "gameplay-clean-room-gamebreaker-trace.tsv", "gameplay-clean-room-gamebreaker-replayed.tsv", "KICKOFF", "GAMEBREAKER", "GOAL_HOME"),
            Spec(root, "juggle", "gameplay-clean-room-juggle-trace.tsv", "gameplay-clean-room-juggle-replayed.tsv", "KICKOFF", "JUGGLE", "SHOT", "GOAL_HOME"),
            Spec(root, "panna", "gameplay-clean-room-panna-trace.tsv", "gameplay-clean-room-panna-replayed.tsv", "KICKOFF", "PANNA", "FULL_TIME"),
            Spec(root, "full-time", "gameplay-clean-room-full-time-trace.tsv", "gameplay-clean-room-full-time-replayed.tsv", "KICKOFF", "FULL_TIME"),
            Spec(root, "steal", "gameplay-clean-room-steal-trace.tsv", "gameplay-clean-room-steal-replayed.tsv", "KICKOFF", "STEAL"),
            Spec(root, "tackle", "gameplay-authored-tackle-match-trace.tsv", "gameplay-authored-tackle-match-replayed.tsv", "KICKOFF", "STEAL", "TACKLE"),
            Spec(root, "first-to-goals", "gameplay-authored-first-to-goals-match-trace.tsv", "gameplay-authored-first-to-goals-match-replayed.tsv", "KICKOFF", "GOAL_HOME", "FULL_TIME"),
        };
    }

    public static GameplayCoverageAuditResult Audit(
        IEnumerable<GameplayCoverageScenarioSpec> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        var rows = scenarios.Select(AuditScenario).ToArray();
        var coveredActions = rows
            .SelectMany(row => SplitValues(row.Actions))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var missingActions = rows
            .SelectMany(row => SplitValues(row.MissingActions))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var verifiedScenarioCount = rows.Count(row => row.Status == "verified-authored");
        var summary = new GameplayCoverageSummary(
            rows.Length,
            verifiedScenarioCount,
            rows.Sum(row => row.EventCount),
            string.Join(",", coveredActions),
            string.Join(",", missingActions),
            rows.Length > 0 && verifiedScenarioCount == rows.Length && missingActions.Length == 0
                ? "verified-authored"
                : "incomplete",
            "not-claimed");
        return new GameplayCoverageAuditResult(summary, rows);
    }

    public static IReadOnlyList<string> BuildLines(GameplayCoverageAuditResult audit)
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
            row.RequiredActionsPresent.ToString(),
            row.EventCount.ToString(CultureInfo.InvariantCulture),
            row.Actions,
            row.MissingActions,
            row.Status,
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
            summary.Status,
            summary.ScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.VerifiedScenarioCount.ToString(CultureInfo.InvariantCulture),
            summary.TotalEvents.ToString(CultureInfo.InvariantCulture),
            summary.CoveredActions,
            summary.MissingActions,
            summary.OriginalSemantics,
            string.Empty,
        }.Select(Escape)));
        return lines.AsReadOnly();
    }

    public static void Write(string path, GameplayCoverageAuditResult audit)
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

    private static GameplayCoverageScenarioSpec Spec(
        string root,
        string name,
        string traceName,
        string replayName,
        params string[] requiredActions) =>
        new(name, Path.Combine(root, traceName), Path.Combine(root, replayName), requiredActions);

    private static GameplayCoverageScenarioResult AuditScenario(
        GameplayCoverageScenarioSpec spec)
    {
        var traceExists = File.Exists(spec.TracePath);
        var replayExists = File.Exists(spec.ReplayPath);
        var traceSha256 = traceExists ? ComputeSha256(spec.TracePath) : string.Empty;
        var replaySha256 = replayExists ? ComputeSha256(spec.ReplayPath) : string.Empty;
        if (!traceExists || !replayExists)
        {
            return Result(
                spec,
                traceExists,
                replayExists,
                false,
                false,
                false,
                Array.Empty<string>(),
                spec.RequiredActions,
                traceSha256,
                replaySha256,
                "missing",
                !traceExists && !replayExists ? "trace and replay are missing" :
                    !traceExists ? "trace is missing" : "replay is missing");
        }

        var traceLines = File.ReadAllLines(spec.TracePath);
        var replayLines = File.ReadAllLines(spec.ReplayPath);
        if (!TryReadActions(traceLines, out var actions))
        {
            return Result(spec, true, true, false, false, false, Array.Empty<string>(),
                spec.RequiredActions, traceSha256, replaySha256, "invalid", "trace header or row shape is invalid");
        }

        var replayMatched = MatchTraceSerializer.FindFirstDifference(traceLines, replayLines) < 0;
        var covered = actions.ToHashSet(StringComparer.Ordinal);
        var missing = spec.RequiredActions
            .Where(action => !covered.Contains(action))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var requiredPresent = missing.Length == 0;
        var status = replayMatched && requiredPresent ? "verified-authored" : "incomplete";
        var issue = status == "verified-authored"
            ? string.Empty
            : string.Join("; ", new[]
            {
                replayMatched ? string.Empty : "replay differs",
                requiredPresent ? string.Empty : $"missing required actions: {string.Join(',', missing)}",
            }.Where(value => !string.IsNullOrWhiteSpace(value)));
        return Result(spec, true, true, true, replayMatched, requiredPresent, actions, missing,
            traceSha256, replaySha256, status, issue);
    }

    private static GameplayCoverageScenarioResult Result(
        GameplayCoverageScenarioSpec spec,
        bool traceExists,
        bool replayExists,
        bool traceValid,
        bool replayMatched,
        bool requiredActionsPresent,
        IReadOnlyList<string> actions,
        IReadOnlyList<string> missingActions,
        string traceSha256,
        string replaySha256,
        string status,
        string issue) =>
        new(
            spec.Name,
            spec.TracePath,
            spec.ReplayPath,
            traceExists,
            replayExists,
            traceValid,
            replayMatched,
            requiredActionsPresent,
            Math.Max(0, actions.Count),
            string.Join(",", actions.Distinct(StringComparer.Ordinal).OrderBy(action => action, StringComparer.Ordinal)),
            string.Join(",", missingActions),
            traceSha256,
            replaySha256,
            status,
            issue);

    private static bool TryReadActions(
        IReadOnlyList<string> lines,
        out IReadOnlyList<string> actions)
    {
        actions = Array.Empty<string>();
        if (lines.Count < 2 || !string.Equals(lines[0], MatchTraceSerializer.Header, StringComparison.Ordinal))
        {
            return false;
        }

        var parsed = new List<string>(lines.Count - 1);
        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length != 6 || string.IsNullOrWhiteSpace(fields[2]))
            {
                return false;
            }
            parsed.Add(fields[2]);
        }
        actions = parsed;
        return parsed.Count > 0;
    }

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
