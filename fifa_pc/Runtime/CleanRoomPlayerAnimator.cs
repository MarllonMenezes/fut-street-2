namespace FifaStreetPc;

/// <summary>
/// Authored PC presentation pose. This is intentionally independent from the
/// observed AXT/morph payloads; it must not be mistaken for an animation or
/// skinning decoder.
/// </summary>
public readonly record struct CleanRoomPlayerPose(
    float Facing,
    float BodyLean,
    float LegPhase,
    float ArmReach,
    float ActionLift,
    float ActionPulse,
    float TorsoLift,
    float HeadTurn);

public static class CleanRoomPlayerAnimator
{
    public static CleanRoomPlayerPose Evaluate(
        int frame,
        string lastAction,
        BallOwner owner,
        bool hasPossession)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(lastAction);

        var authoredProfile = CleanRoomAnimationAssetManifest.Default.ResolveProfile(lastAction);
        var facing = owner == BallOwner.Away ? -1.0f : 1.0f;
        var phase = frame * 0.18f + (owner == BallOwner.Away ? 0.7f : 0.0f);
        var actionLift = authoredProfile switch
        {
            CleanRoomAnimationAssetProfile.TrickPanna => 0.85f,
            CleanRoomAnimationAssetProfile.Juggle => 0.55f,
            CleanRoomAnimationAssetProfile.Shot => 0.65f,
            CleanRoomAnimationAssetProfile.Pass => 0.35f,
            // Authored PC tackle profile for the clean-room opponent route;
            // this is not a decoded original animation or rig binding.
            CleanRoomAnimationAssetProfile.Steal => 0.60f,
            CleanRoomAnimationAssetProfile.Goal => 0.90f,
            // Authored PC celebration profile. This is not an observed pose
            // name, frame, rig, skinning or animation binding.
            CleanRoomAnimationAssetProfile.Gamebreaker => 1.05f,
            CleanRoomAnimationAssetProfile.Restart => 0.20f,
            CleanRoomAnimationAssetProfile.FullTime => 0.15f,
            _ => 0.0f,
        };
        var actionPhaseOffset = authoredProfile switch
        {
            CleanRoomAnimationAssetProfile.TrickPanna => 0.85f,
            CleanRoomAnimationAssetProfile.Juggle => 1.40f,
            CleanRoomAnimationAssetProfile.Pass => 0.35f,
            CleanRoomAnimationAssetProfile.Shot => 1.15f,
            CleanRoomAnimationAssetProfile.Steal => 1.85f,
            CleanRoomAnimationAssetProfile.Goal => 1.65f,
            CleanRoomAnimationAssetProfile.Gamebreaker => 2.10f,
            CleanRoomAnimationAssetProfile.Restart => 0.55f,
            CleanRoomAnimationAssetProfile.FullTime => 0.0f,
            _ => 0.0f,
        };
        var actionPulse = MathF.Sin(phase + actionPhaseOffset);
        var torsoLift = authoredProfile switch
        {
            CleanRoomAnimationAssetProfile.TrickPanna => 0.30f,
            CleanRoomAnimationAssetProfile.Juggle => 0.22f,
            CleanRoomAnimationAssetProfile.Shot => 0.42f,
            CleanRoomAnimationAssetProfile.Steal => 0.34f,
            CleanRoomAnimationAssetProfile.Goal => 0.55f,
            CleanRoomAnimationAssetProfile.Gamebreaker => 0.72f,
            CleanRoomAnimationAssetProfile.Restart => 0.16f,
            _ => 0.0f,
        };
        var headTurn = authoredProfile switch
        {
            CleanRoomAnimationAssetProfile.Pass => 0.35f,
            CleanRoomAnimationAssetProfile.Juggle => 0.45f,
            CleanRoomAnimationAssetProfile.Shot => 0.70f,
            CleanRoomAnimationAssetProfile.Steal => 0.65f,
            CleanRoomAnimationAssetProfile.Goal => 0.50f,
            CleanRoomAnimationAssetProfile.Gamebreaker => 0.80f,
            CleanRoomAnimationAssetProfile.FullTime => 0.0f,
            _ => 0.0f,
        };
        var bodyLean = hasPossession ? 0.10f * facing : 0.0f;
        var armReach = hasPossession ? 0.65f : 0.45f;
        if (lastAction is "SHOT" or "SHOT_AWAY")
        {
            armReach = 0.90f;
        }
        else if (lastAction is "STEAL" or "TACKLE")
        {
            armReach = 0.85f;
        }
        else if (lastAction == "GAMEBREAKER")
        {
            armReach = 1.0f;
        }

        if (authoredProfile == CleanRoomAnimationAssetProfile.Gamebreaker)
        {
            bodyLean = 0.18f * facing;
        }

        return new CleanRoomPlayerPose(
            facing,
            bodyLean,
            phase + actionPhaseOffset,
            armReach,
            actionLift,
            actionPulse,
            torsoLift,
            headTurn);
    }
}
