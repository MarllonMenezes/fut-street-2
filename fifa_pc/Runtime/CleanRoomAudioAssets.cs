namespace FifaStreetPc;

/// <summary>
/// Authored audio-cue manifest for the managed clean-room runtime. The cue
/// names are candidate labels already observed by bounded decoders; these
/// identifiers describe PC-owned presentation assets and do not claim the
/// original EVT/ABKC/sample selection.
/// </summary>
public readonly record struct CleanRoomAudioAssetManifest(
    string IntroAsset,
    string AttackGeneralAsset,
    string PannaAsset,
    string AttackGoalAsset,
    string GoalAsset,
    string GamebreakerGoalAsset,
    string TackleAsset,
    string OutroAsset)
{
    public static CleanRoomAudioAssetManifest Default { get; } = new(
        "clean-room-audio-intro-v1",
        "clean-room-audio-attackgen-v1",
        "clean-room-audio-panna-v1",
        "clean-room-audio-attackgoal-v1",
        "clean-room-audio-goal-v1",
        "clean-room-audio-gb-goal-v1",
        "clean-room-audio-tackle-v1",
        "clean-room-audio-outro-v1");

    public string ManifestId => "clean-room-audio-manifest-v1";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(IntroAsset) &&
        !string.IsNullOrWhiteSpace(AttackGeneralAsset) &&
        !string.IsNullOrWhiteSpace(PannaAsset) &&
        !string.IsNullOrWhiteSpace(AttackGoalAsset) &&
        !string.IsNullOrWhiteSpace(GoalAsset) &&
        !string.IsNullOrWhiteSpace(GamebreakerGoalAsset) &&
        !string.IsNullOrWhiteSpace(TackleAsset) &&
        !string.IsNullOrWhiteSpace(OutroAsset);

    public bool TryResolveCandidate(string candidateName, out string assetId)
    {
        ArgumentNullException.ThrowIfNull(candidateName);

        assetId = candidateName switch
        {
            "intro" => IntroAsset,
            "attackgen" => AttackGeneralAsset,
            "panna" => PannaAsset,
            "attackgoal" => AttackGoalAsset,
            "goal" => GoalAsset,
            "gb_goal" => GamebreakerGoalAsset,
            "tackle" => TackleAsset,
            "outro" => OutroAsset,
            _ => string.Empty,
        };

        return assetId.Length > 0;
    }
}
