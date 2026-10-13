using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record CleanRoomGameplayInvariantScenarioSpec(
    string Name,
    int FrameCount,
    Func<IInputSource> InputFactory,
    bool OpponentAiEnabled,
    bool CleanRoomRoundResetEnabled,
    double MatchDurationSeconds,
    CleanRoomMatchEndCondition EndCondition,
    int TargetGoals,
    int TargetPannas,
    bool ExpectEnded,
    IReadOnlyList<string> RequiredActions);

public sealed record CleanRoomGameplayInvariantScenarioResult(
    string Name,
    int FrameCount,
    int EventCount,
    bool FiniteState,
    bool PositionsBounded,
    bool BallHeightBounded,
    bool ClockMonotonic,
    bool ScoresMonotonic,
    bool EventSequenceMonotonic,
    bool EndConditionSatisfied,
    bool RequiredActionsPresent,
    string RequiredActions,
    string MissingActions,
    string Status,
    string Issue);

public sealed record CleanRoomGameplayInvariantSummary(
    int ScenarioCount,
    int VerifiedScenarioCount,
    int TotalFrames,
    int TotalEvents,
    string Status,
    string OriginalSemantics);

public sealed record CleanRoomGameplayInvariantAuditResult(
    CleanRoomGameplayInvariantSummary Summary,
    IReadOnlyList<CleanRoomGameplayInvariantScenarioResult> Scenarios);

/// <summary>
/// Checks authored clean-room gameplay invariants over deterministic runtime
/// scenarios. It does not decode or assert equivalence with original rules.
/// </summary>
public static class CleanRoomGameplayInvariantAudit
{
    public const string Header =
        "kind\tname\tframe_count\tevent_count\tfinite_state\tpositions_bounded\tball_height_bounded\tclock_monotonic\tscores_monotonic\tevent_sequence_monotonic\tend_condition_satisfied\trequired_actions_present\trequired_actions\tmissing_actions\tstatus\tscenario_count\tverified_scenario_count\ttotal_frames\ttotal_events\toriginal_semantics\tissue";

    public static IReadOnlyList<CleanRoomGameplayInvariantScenarioSpec> DefaultScenarios(
        string workspaceDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceDirectory);
        var root = Path.Combine(Path.GetFullPath(workspaceDirectory), "fifa_pc");
        return new[]
        {
            new CleanRoomGameplayInvariantScenarioSpec(
                "full-stack",
                140,
                () => new GameplayDemoInputSource(),
                true,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "PASS", "PASS_COMPLETE", "TRICK", "SHOT_AWAY", "GOAL_AWAY" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "aimed",
                80,
                () => new GameplayDemoInputSource(aimedShot: true),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "SHOT", "GOAL_HOME" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "round",
                140,
                () => ScriptSource(Path.Combine(root, "gameplay-clean-room-round-input.tsv")),
                false,
                true,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "GOAL_HOME", "RESTART" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "gamebreaker",
                40,
                () => new GameplayDemoInputSource(gamebreaker: true),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "GAMEBREAKER", "GOAL_HOME" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "juggle",
                56,
                () => new GameplayDemoInputSource(juggle: true),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "JUGGLE", "SHOT", "GOAL_HOME" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "panna",
                31,
                () => ScriptSource(Path.Combine(root, "gameplay-clean-room-panna-input.tsv")),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.GoalsOrPanna,
                9,
                1,
                true,
                new[] { "KICKOFF", "PANNA", "FULL_TIME" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "full-time",
                30,
                () => new GameplayDemoInputSource(),
                false,
                false,
                0.25,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                true,
                new[] { "KICKOFF", "FULL_TIME" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "steal",
                80,
                () => new StealDemoInputSource(),
                true,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "STEAL" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "tackle",
                100,
                () => new TackleDemoInputSource(),
                true,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF", "STEAL", "TACKLE" }),
            new CleanRoomGameplayInvariantScenarioSpec(
                "first-to-goals",
                80,
                () => ScriptSource(Path.Combine(root, "gameplay-clean-room-demo-input.tsv")),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.FirstToGoals,
                1,
                0,
                true,
                new[] { "KICKOFF", "GOAL_HOME", "FULL_TIME" }),
        };
    }

    public static CleanRoomGameplayInvariantAuditResult Audit(
        IEnumerable<CleanRoomGameplayInvariantScenarioSpec> scenarios)
    {
        ArgumentNullException.ThrowIfNull(scenarios);
        var rows = scenarios.Select(AuditScenario).ToArray();
        var verified = rows.Count(row => row.Status == "verified-authored");
        var summary = new CleanRoomGameplayInvariantSummary(
            rows.Length,
            verified,
            rows.Sum(row => row.FrameCount),
            rows.Sum(row => row.EventCount),
            rows.Length > 0 && verified == rows.Length
                ? "verified-authored"
                : "incomplete",
            "not-claimed");
        return new CleanRoomGameplayInvariantAuditResult(summary, rows);
    }

    public static IReadOnlyList<string> BuildLines(
        CleanRoomGameplayInvariantAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var summary = audit.Summary;
        var lines = new List<string> { Header };
        lines.AddRange(audit.Scenarios.Select(row => string.Join('\t', new[]
        {
            "scenario",
            row.Name,
            row.FrameCount.ToString(CultureInfo.InvariantCulture),
            row.EventCount.ToString(CultureInfo.InvariantCulture),
            row.FiniteState.ToString(),
            row.PositionsBounded.ToString(),
            row.BallHeightBounded.ToString(),
            row.ClockMonotonic.ToString(),
            row.ScoresMonotonic.ToString(),
            row.EventSequenceMonotonic.ToString(),
            row.EndConditionSatisfied.ToString(),
            row.RequiredActionsPresent.ToString(),
            row.RequiredActions,
            row.MissingActions,
            row.Status,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "not-claimed",
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
            summary.TotalFrames.ToString(CultureInfo.InvariantCulture),
            summary.TotalEvents.ToString(CultureInfo.InvariantCulture),
            summary.OriginalSemantics,
            string.Empty,
        }.Select(Escape)));
        return lines.AsReadOnly();
    }

    public static void Write(
        string path,
        CleanRoomGameplayInvariantAuditResult audit)
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

    private static CleanRoomGameplayInvariantScenarioResult AuditScenario(
        CleanRoomGameplayInvariantScenarioSpec spec)
    {
        if (spec.FrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spec.FrameCount));
        }
        ArgumentNullException.ThrowIfNull(spec.InputFactory);
        ArgumentNullException.ThrowIfNull(spec.RequiredActions);

        var input = spec.InputFactory();
        var match = new StreetMatch(
            spec.OpponentAiEnabled,
            spec.CleanRoomRoundResetEnabled,
            spec.MatchDurationSeconds,
            spec.EndCondition,
            spec.TargetGoals,
            spec.TargetPannas);
        var finiteState = true;
        var positionsBounded = true;
        var ballHeightBounded = true;
        var clockMonotonic = true;
        var scoresMonotonic = true;
        var eventSequenceMonotonic = true;
        var previousRemaining = match.RemainingSeconds;
        var previousHomeScore = match.HomeScore;
        var previousAwayScore = match.AwayScore;
        var previousSequence = match.ActionSequence;

        for (var frame = 0; frame < spec.FrameCount; frame++)
        {
            match.Tick(1.0 / 60.0, input.Poll());
            finiteState &= IsFinite(match.HomePosition.X) &&
                IsFinite(match.HomePosition.Y) &&
                IsFinite(match.AwayPosition.X) &&
                IsFinite(match.AwayPosition.Y) &&
                IsFinite(match.BallPosition.X) &&
                IsFinite(match.BallPosition.Y) &&
                IsFinite(match.BallHeight) &&
                !double.IsNaN(match.RemainingSeconds) &&
                !double.IsInfinity(match.RemainingSeconds);
            positionsBounded &= InRange(match.HomePosition.X, 0.0f, 1.0f) &&
                InRange(match.HomePosition.Y, 0.0f, 0.62f) &&
                InRange(match.AwayPosition.X, 0.0f, 1.0f) &&
                InRange(match.AwayPosition.Y, 0.0f, 0.62f) &&
                InRange(match.BallPosition.X, 0.0f, 1.0f) &&
                InRange(match.BallPosition.Y, 0.0f, 0.62f);
            ballHeightBounded &= InRange(match.BallHeight, 0.0f, StreetMatch.MaximumBallHeight);
            clockMonotonic &= match.RemainingSeconds >= -float.Epsilon &&
                match.RemainingSeconds <= previousRemaining + 1e-9;
            scoresMonotonic &= match.HomeScore >= previousHomeScore &&
                match.AwayScore >= previousAwayScore;
            eventSequenceMonotonic &= match.ActionSequence >= previousSequence &&
                match.EventHistory.Count == match.ActionSequence + 1 &&
                match.EventHistory[^1].Sequence == match.ActionSequence;
            previousRemaining = match.RemainingSeconds;
            previousHomeScore = match.HomeScore;
            previousAwayScore = match.AwayScore;
            previousSequence = match.ActionSequence;
        }

        var actions = match.EventHistory
            .Select(matchEvent => matchEvent.Action)
            .ToHashSet(StringComparer.Ordinal);
        var missingActions = spec.RequiredActions
            .Where(action => !actions.Contains(action))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(action => action, StringComparer.Ordinal)
            .ToArray();
        var requiredActionsPresent = missingActions.Length == 0;
        var endConditionSatisfied = match.IsEnded == spec.ExpectEnded;
        var verified = finiteState &&
            positionsBounded &&
            ballHeightBounded &&
            clockMonotonic &&
            scoresMonotonic &&
            eventSequenceMonotonic &&
            endConditionSatisfied &&
            requiredActionsPresent;
        var issues = new[]
        {
            finiteState ? string.Empty : "state contains a non-finite value",
            positionsBounded ? string.Empty : "position or ball coordinates escaped authored bounds",
            ballHeightBounded ? string.Empty : "ball height escaped authored bounds",
            clockMonotonic ? string.Empty : "remaining clock increased or became negative",
            scoresMonotonic ? string.Empty : "score decreased",
            eventSequenceMonotonic ? string.Empty : "event sequence or history count is inconsistent",
            endConditionSatisfied ? string.Empty : $"expected ended={spec.ExpectEnded} but observed ended={match.IsEnded}",
            requiredActionsPresent ? string.Empty : $"missing required actions: {string.Join(',', missingActions)}",
        }.Where(issue => !string.IsNullOrWhiteSpace(issue));
        return new CleanRoomGameplayInvariantScenarioResult(
            spec.Name,
            spec.FrameCount,
            match.EventHistory.Count,
            finiteState,
            positionsBounded,
            ballHeightBounded,
            clockMonotonic,
            scoresMonotonic,
            eventSequenceMonotonic,
            endConditionSatisfied,
            requiredActionsPresent,
            string.Join(',', spec.RequiredActions),
            string.Join(',', missingActions),
            verified ? "verified-authored" : "incomplete",
            string.Join("; ", issues));
    }

    private static IInputSource ScriptSource(string path) =>
        new MatchInputScriptSource(MatchInputScriptReader.Read(path));

    private static bool IsFinite(float value) =>
        !float.IsNaN(value) && !float.IsInfinity(value);

    private static bool InRange(float value, float minimum, float maximum) =>
        IsFinite(value) && value >= minimum - 1e-5f && value <= maximum + 1e-5f;

    private static string Escape(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
