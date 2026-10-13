namespace FifaStreetPc;

/// <summary>
/// Manifest for the authored PC scene primitives. These identifiers describe
/// assets created by the clean-room runtime; they are not names or bindings
/// decoded from the original render pipeline.
/// </summary>
public readonly record struct CleanRoomSceneAssetManifest(
    string CourtAsset,
    string GoalAsset,
    string BallAsset)
{
    public static CleanRoomSceneAssetManifest Default { get; } = new(
        "clean-room-court-v1",
        "clean-room-goal-v1",
        "clean-room-ball-v1");

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(CourtAsset) &&
        !string.IsNullOrWhiteSpace(GoalAsset) &&
        !string.IsNullOrWhiteSpace(BallAsset);

    public string ManifestId =>
        $"{CourtAsset}|{GoalAsset}|{BallAsset}";
}

/// <summary>
/// Draws the authored court primitives used by the managed PC runtime. The
/// geometry and palette are deliberately independent of EAGL, SHPX, UV,
/// material, skinning, rigging, animation and GPU command interpretation.
/// </summary>
public static class CleanRoomSceneRenderer
{
    private static readonly RgbColor Court = new(40, 116, 67);
    private static readonly RgbColor CourtBorder = new(233, 233, 218);

    public static bool DrawCourt(
        SoftwareFrameBuffer frame,
        CleanRoomSceneAssetManifest manifest,
        int left,
        int top,
        int courtWidth,
        int courtHeight)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!manifest.IsComplete)
        {
            return false;
        }

        frame.FillRect(left, top, courtWidth, courtHeight, Court);
        frame.DrawLine(left, top, left + courtWidth, top, CourtBorder, 3);
        frame.DrawLine(left, top + courtHeight, left + courtWidth, top + courtHeight, CourtBorder, 3);
        frame.DrawLine(left, top, left, top + courtHeight, CourtBorder, 3);
        frame.DrawLine(left + courtWidth, top, left + courtWidth, top + courtHeight, CourtBorder, 3);
        frame.DrawLine(left + courtWidth / 2, top, left + courtWidth / 2, top + courtHeight, CourtBorder, 2);
        frame.DrawCircle(left + courtWidth / 2, top + courtHeight / 2, courtHeight / 8, CourtBorder);
        frame.DrawCircle(left + courtWidth / 2, top + courtHeight / 2, courtHeight / 8 - 3, Court);

        DrawGoal(frame, left, top + courtHeight / 2, courtHeight);
        DrawGoal(frame, left + courtWidth, top + courtHeight / 2, courtHeight);
        return true;
    }

    public static bool DrawGroundBall(
        SoftwareFrameBuffer frame,
        CleanRoomSceneAssetManifest manifest,
        int x,
        int y,
        RgbColor color,
        int radius)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!manifest.IsComplete)
        {
            return false;
        }

        frame.DrawCircle(x, y, radius + 3, new RgbColor(20, 25, 30));
        frame.DrawCircle(x, y, radius, color);
        return true;
    }

    public static bool DrawElevatedBall(
        SoftwareFrameBuffer frame,
        CleanRoomSceneAssetManifest manifest,
        int x,
        int groundY,
        int lift,
        RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!manifest.IsComplete)
        {
            return false;
        }

        frame.DrawCircle(x, groundY, 5, new RgbColor(20, 25, 30));
        frame.DrawCircle(x, groundY - lift, 8, new RgbColor(20, 25, 30));
        frame.DrawCircle(x, groundY - lift, 6, color);
        return true;
    }

    private static void DrawGoal(SoftwareFrameBuffer frame, int x, int centerY, int courtHeight)
    {
        var direction = x < frame.Width / 2 ? 1 : -1;
        var goalWidth = Math.Max(8, frame.Width / 55);
        var goalHeight = Math.Max(24, courtHeight / 5);
        frame.FillRect(x - (direction < 0 ? goalWidth : 0), centerY - goalHeight / 2, goalWidth, goalHeight, CourtBorder);
    }
}
