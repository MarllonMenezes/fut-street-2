namespace FifaStreetPc;

/// <summary>
/// Small authored presentation asset for the managed PC runtime. The palette
/// and silhouette are new code; they are not extracted material, UV, mesh,
/// rigging, skinning or GPU state from the ISO.
/// </summary>
public readonly record struct CleanRoomCharacterPalette(
    RgbColor Jersey,
    RgbColor JerseyShadow,
    RgbColor Skin,
    RgbColor Outline,
    RgbColor Shoe);

public static class CleanRoomCharacterRenderer
{
    public static void Draw(
        SoftwareFrameBuffer frame,
        int centerX,
        int groundY,
        CleanRoomPlayerPose pose,
        CleanRoomCharacterPalette palette)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!CleanRoomCharacterAssetManifest.Default.IsComplete)
        {
            throw new InvalidOperationException("The authored character asset manifest is incomplete.");
        }

        var facing = pose.Facing < 0 ? -1 : 1;
        var lean = (int)MathF.Round(pose.BodyLean * 8.0f);
        var bodyX = centerX + lean;
        var bodyY = groundY - 5 - (int)MathF.Round(pose.TorsoLift * 4.0f);
        var footY = bodyY + 18;
        var legSwing = MathF.Sin(pose.LegPhase) * (5.0f + MathF.Abs(pose.ActionPulse) * 2.0f);
        var armSwing = MathF.Cos(pose.LegPhase) * 3.0f + pose.ActionPulse * 2.0f;
        var armReach = 7 + (int)MathF.Round(Math.Clamp(pose.ArmReach, 0.0f, 1.0f) * 5.0f);
        var handY = bodyY - (int)MathF.Round(pose.ActionLift * 6.0f);
        var leftHandY = handY - (int)MathF.Round(pose.ActionPulse * 2.0f);
        var rightHandY = handY + (int)MathF.Round(pose.ActionPulse * 2.0f);

        // Authored ground shadow and silhouette geometry.
        frame.DrawCircle(centerX, groundY + 11, 13, new RgbColor(26, 35, 31));
        DrawQuad(
            frame,
            bodyX - 9,
            bodyY - 4,
            bodyX + 9,
            bodyY - 4,
            bodyX + 8,
            bodyY + 11,
            bodyX - 8,
            bodyY + 11,
            palette.Outline);
        DrawQuad(
            frame,
            bodyX - 6,
            bodyY - 2,
            bodyX + 6,
            bodyY - 2,
            bodyX + 6,
            bodyY + 9,
            bodyX - 6,
            bodyY + 9,
            palette.Jersey);
        DrawQuad(
            frame,
            bodyX - 7,
            bodyY + 8,
            bodyX + 7,
            bodyY + 8,
            bodyX + 6,
            bodyY + 15,
            bodyX - 6,
            bodyY + 15,
            palette.JerseyShadow);

        frame.DrawLine(bodyX - 4, bodyY + 10, bodyX - 7 + (int)MathF.Round(legSwing), footY, palette.Outline, 4);
        frame.DrawLine(bodyX + 4, bodyY + 10, bodyX + 7 - (int)MathF.Round(legSwing), footY, palette.Outline, 4);
        frame.DrawLine(bodyX - 7, footY, bodyX - 7 + facing * 3, footY, palette.Shoe, 3);
        frame.DrawLine(bodyX + 7, footY, bodyX + 7 + facing * 3, footY, palette.Shoe, 3);

        frame.DrawLine(bodyX - 7, bodyY - 1, bodyX - facing * armReach + (int)MathF.Round(armSwing), leftHandY, palette.Outline, 4);
        frame.DrawLine(bodyX + 7, bodyY - 1, bodyX + facing * armReach - (int)MathF.Round(armSwing), rightHandY, palette.Outline, 4);
        frame.DrawCircle(bodyX, bodyY - 7, 5, palette.Outline);
        frame.DrawCircle(bodyX, bodyY - 7, 3, palette.Skin);

        var headX = bodyX + (int)MathF.Round(facing * (1.0f + pose.HeadTurn * 2.0f));
        frame.DrawCircle(bodyX, bodyY - 13, 6, palette.Outline);
        frame.DrawCircle(headX, bodyY - 13, 4, palette.Skin);
    }

    private static void DrawQuad(
        SoftwareFrameBuffer frame,
        int ax,
        int ay,
        int bx,
        int by,
        int cx,
        int cy,
        int dx,
        int dy,
        RgbColor color)
    {
        frame.FillTriangle(ax, ay, bx, by, cx, cy, color);
        frame.FillTriangle(ax, ay, cx, cy, dx, dy, color);
    }
}
