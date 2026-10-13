namespace FifaStreetPc;

/// <summary>
/// Authored procedural animation-profile manifest for the managed clean-room
/// runtime. These identifiers name PC presentation profiles only; they are not
/// decoded AXT records and do not claim original frames, timing, rigging,
/// skinning or morph semantics.
/// </summary>
public enum CleanRoomAnimationAssetProfile
{
    Unknown,
    Idle,
    TrickPanna,
    Juggle,
    Shot,
    Pass,
    Steal,
    Goal,
    Gamebreaker,
    Restart,
    FullTime,
}

public readonly record struct CleanRoomAnimationAssetManifest(
    string IdleAsset,
    string TrickPannaAsset,
    string JuggleAsset,
    string ShotAsset,
    string PassAsset,
    string StealAsset,
    string GoalAsset,
    string GamebreakerAsset,
    string RestartAsset,
    string FullTimeAsset)
{
    public static CleanRoomAnimationAssetManifest Default { get; } = new(
        "clean-room-animation-idle-v1",
        "clean-room-animation-trick-panna-v1",
        "clean-room-animation-juggle-v1",
        "clean-room-animation-shot-v1",
        "clean-room-animation-pass-v1",
        "clean-room-animation-steal-v1",
        "clean-room-animation-goal-v1",
        "clean-room-animation-gamebreaker-v1",
        "clean-room-animation-restart-v1",
        "clean-room-animation-full-time-v1");

    public string ManifestId => "clean-room-animation-manifest-v1";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(IdleAsset) &&
        !string.IsNullOrWhiteSpace(TrickPannaAsset) &&
        !string.IsNullOrWhiteSpace(JuggleAsset) &&
        !string.IsNullOrWhiteSpace(ShotAsset) &&
        !string.IsNullOrWhiteSpace(PassAsset) &&
        !string.IsNullOrWhiteSpace(StealAsset) &&
        !string.IsNullOrWhiteSpace(GoalAsset) &&
        !string.IsNullOrWhiteSpace(GamebreakerAsset) &&
        !string.IsNullOrWhiteSpace(RestartAsset) &&
        !string.IsNullOrWhiteSpace(FullTimeAsset);

    public CleanRoomAnimationAssetProfile ResolveProfile(string action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return action switch
        {
            "KICKOFF" => CleanRoomAnimationAssetProfile.Idle,
            "TRICK" or "PANNA" => CleanRoomAnimationAssetProfile.TrickPanna,
            "JUGGLE" => CleanRoomAnimationAssetProfile.Juggle,
            "SHOT" or "SHOT_AWAY" => CleanRoomAnimationAssetProfile.Shot,
            "PASS" or "PASS_COMPLETE" => CleanRoomAnimationAssetProfile.Pass,
            "STEAL" or "TACKLE" => CleanRoomAnimationAssetProfile.Steal,
            "GOAL_HOME" or "GOAL_AWAY" => CleanRoomAnimationAssetProfile.Goal,
            "GAMEBREAKER" => CleanRoomAnimationAssetProfile.Gamebreaker,
            "RESTART" => CleanRoomAnimationAssetProfile.Restart,
            "FULL_TIME" => CleanRoomAnimationAssetProfile.FullTime,
            _ => CleanRoomAnimationAssetProfile.Unknown,
        };
    }

    public bool TryResolveAsset(string action, out string assetId)
    {
        ArgumentNullException.ThrowIfNull(action);

        assetId = ResolveProfile(action) switch
        {
            CleanRoomAnimationAssetProfile.Idle => IdleAsset,
            CleanRoomAnimationAssetProfile.TrickPanna => TrickPannaAsset,
            CleanRoomAnimationAssetProfile.Juggle => JuggleAsset,
            CleanRoomAnimationAssetProfile.Shot => ShotAsset,
            CleanRoomAnimationAssetProfile.Pass => PassAsset,
            CleanRoomAnimationAssetProfile.Steal => StealAsset,
            CleanRoomAnimationAssetProfile.Goal => GoalAsset,
            CleanRoomAnimationAssetProfile.Gamebreaker => GamebreakerAsset,
            CleanRoomAnimationAssetProfile.Restart => RestartAsset,
            CleanRoomAnimationAssetProfile.FullTime => FullTimeAsset,
            _ => string.Empty,
        };

        return assetId.Length > 0;
    }
}
