namespace FifaStreetPc;

public sealed record AuthoredAudioCueRoute(string Action, string CandidateName);

/// <summary>
/// The PC runtime's explicit authored audio policy. Candidate names are
/// independently decoded EVT payload names; the action-to-candidate choices
/// are new clean-room behavior and do not describe the original event table.
/// </summary>
public static class CleanRoomAudioCuePolicy
{
    public static IReadOnlyList<AuthoredAudioCueRoute> Routes { get; } =
        new AuthoredAudioCueRoute[]
        {
            new("KICKOFF", "intro"),
            new("RESTART", "intro"),
            new("PASS", "attackgen"),
            new("PASS_COMPLETE", "attackgen"),
            new("TRICK", "panna"),
            new("PANNA", "panna"),
            new("JUGGLE", "attackgen"),
            new("SHOT", "attackgoal"),
            new("SHOT_AWAY", "attackgoal"),
            new("GOAL_HOME", "goal"),
            new("GOAL_AWAY", "goal"),
            new("GAMEBREAKER", "gb_goal"),
            new("STEAL", "tackle"),
            new("TACKLE", "tackle"),
            new("FULL_TIME", "outro"),
        };

    private static readonly IReadOnlyDictionary<string, string> RoutesByAction =
        Routes.ToDictionary(route => route.Action, route => route.CandidateName, StringComparer.Ordinal);

    public static bool TryResolve(string action, out string candidateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        return RoutesByAction.TryGetValue(action, out candidateName!);
    }

    public static string FormatRoutes() =>
        string.Join(",", Routes.Select(route => $"{route.Action}->{route.CandidateName}"));
}
