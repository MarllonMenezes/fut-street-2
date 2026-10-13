namespace FifaStreetPc;

public static class PreviewRenderer
{
    private static readonly RgbColor Sky = new(18, 24, 38);
    private static readonly RgbColor CourtBorder = new(233, 233, 218);
    private static readonly RgbColor Home = new(231, 73, 66);
    private static readonly RgbColor Away = new(62, 143, 231);
    private static readonly RgbColor Ball = new(248, 194, 62);

    public static void Render(StreetMatch match, string outputPath, int width = 960, int height = 540)
    {
        var frame = new SoftwareFrameBuffer(width, height);
        Render(match, frame);
        frame.SavePpm(outputPath);
    }

    public static void Render(
        StreetMatch match,
        SoftwareFrameBuffer frame,
        PcTextureResource? titleTexture = null,
        EaglCandidateMesh? candidateMesh = null,
        PcShapeImageTexture? candidateTexture = null,
        bool slot1AttributeDiagnostic = false,
        bool cleanRoomCharacters = false,
        bool candidateHomeCharacter = false,
        bool candidateAwayCharacter = false,
        bool showAuthoredHud = false)
    {
        var width = frame.Width;
        var height = frame.Height;
        frame.Clear(Sky);

        var left = width / 12;
        var top = height / 8;
        var courtWidth = width - left * 2;
        var courtHeight = height - top * 2;
        _ = CleanRoomSceneRenderer.DrawCourt(
            frame,
            CleanRoomSceneAssetManifest.Default,
            left,
            top,
            courtWidth,
            courtHeight);
        // Candidate meshes are rigid, opt-in presentation assets. They are
        // deliberately not passed through the authored animator because no
        // rig, skinning, morph or animation binding has been proven. With no
        // explicit away flag, preserve the historical candidate-material
        // diagnostic overlay in the home slot.
        var candidateHomeSlot = candidateMesh is not null &&
            (candidateHomeCharacter || !candidateAwayCharacter);
        var candidateAwaySlot = candidateMesh is not null && candidateAwayCharacter;
        var drawProceduralHome = !candidateHomeCharacter || candidateMesh is null;
        var drawProceduralAway = !candidateAwayCharacter || candidateMesh is null;
        if (cleanRoomCharacters)
        {
            if (drawProceduralHome)
            {
                DrawCleanRoomPlayer(
                    frame,
                    match,
                    match.HomePosition,
                    left,
                    top,
                    courtWidth,
                    courtHeight,
                    Home,
                    BallOwner.Home);
            }
            if (drawProceduralAway)
            {
                DrawCleanRoomPlayer(
                    frame,
                    match,
                    match.AwayPosition,
                    left,
                    top,
                    courtWidth,
                    courtHeight,
                    Away,
                    BallOwner.Away);
            }
        }
        else
        {
            if (drawProceduralHome)
            {
                DrawPlayer(frame, match.HomePosition, left, top, courtWidth, courtHeight, Home);
            }
            if (drawProceduralAway)
            {
                DrawPlayer(frame, match.AwayPosition, left, top, courtWidth, courtHeight, Away);
            }
        }
        var ballColor = match.Possession switch
        {
            BallOwner.Home => Home,
            BallOwner.Away => Away,
            _ => Ball,
        };
        if (match.BallHeight > 0)
        {
            var ballX = left + (int)Math.Round(match.BallPosition.X * courtWidth);
            var groundY = top + (int)Math.Round(match.BallPosition.Y / 0.62f * courtHeight);
            var lift = (int)Math.Round(match.BallHeight * courtHeight * 0.42f);
            _ = CleanRoomSceneRenderer.DrawElevatedBall(
                frame,
                CleanRoomSceneAssetManifest.Default,
                ballX,
                groundY,
                lift,
                ballColor);
        }
        else
        {
            var ballX = left + (int)Math.Round(match.BallPosition.X * courtWidth);
            var ballY = top + (int)Math.Round(match.BallPosition.Y / 0.62f * courtHeight);
            _ = CleanRoomSceneRenderer.DrawGroundBall(
                frame,
                CleanRoomSceneAssetManifest.Default,
                ballX,
                ballY,
                ballColor,
                8);
        }

        if (candidateHomeSlot)
        {
            DrawCandidateMesh(
                frame,
                candidateMesh!,
                candidateTexture,
                match.HomePosition,
                left,
                top,
                courtWidth,
                courtHeight,
                Home,
                slot1AttributeDiagnostic);
        }
        if (candidateAwaySlot)
        {
            DrawCandidateMesh(
                frame,
                candidateMesh!,
                candidateTexture,
                match.AwayPosition,
                left,
                top,
                courtWidth,
                courtHeight,
                Away,
                slot1AttributeDiagnostic);
        }

        if (titleTexture is not null)
        {
            frame.BlitRgba(
                titleTexture.DecodedRgba,
                titleTexture.Width,
                titleTexture.Height,
                16,
                16,
                Math.Min(160, width / 4),
                Math.Min(160, height / 4));
        }

        if (showAuthoredHud)
        {
            DrawAuthoredHud(frame, match);
        }
    }

    private static void DrawAuthoredHud(SoftwareFrameBuffer frame, StreetMatch match)
    {
        DrawAuthoredText(
            frame,
            $"HOME {match.HomeScore} - {match.AwayScore} AWAY",
            210,
            16,
            3,
            CourtBorder);
        DrawAuthoredText(
            frame,
            FormattableString.Invariant($"TIME {match.RemainingSeconds:000.0}"),
            210,
            38,
            2,
            CourtBorder);

        const int margin = 16;
        const int barHeight = 12;
        var barWidth = Math.Min(240, Math.Max(0, frame.Width - margin * 2));
        if (barWidth < 8 || frame.Height < barHeight + margin * 2)
        {
            return;
        }

        var x = margin;
        var y = frame.Height - margin - barHeight;
        frame.FillRect(x - 2, y - 2, barWidth + 4, barHeight + 4, new RgbColor(12, 16, 24));
        frame.FillRect(x, y, barWidth, barHeight, new RgbColor(72, 78, 92));
        var filledWidth = (int)Math.Round(
            barWidth * Math.Clamp(match.GamebreakerMeter, 0, StreetMatch.MaximumGamebreakerMeter) /
            (float)StreetMatch.MaximumGamebreakerMeter);
        if (filledWidth > 0)
        {
            frame.FillRect(
                x,
                y,
                filledWidth,
                barHeight,
                match.GamebreakerMeter >= StreetMatch.MaximumGamebreakerMeter
                    ? new RgbColor(248, 194, 62)
                    : new RgbColor(85, 210, 126));
        }
    }

    private static void DrawCandidateMesh(
        SoftwareFrameBuffer frame,
        EaglCandidateMesh candidateMesh,
        PcShapeImageTexture? candidateTexture,
        (float X, float Y) position,
        int left,
        int top,
        int courtWidth,
        int courtHeight,
        RgbColor wireColor,
        bool slot1AttributeDiagnostic)
    {
        var meshCenterX = left + (int)Math.Round(position.X * courtWidth);
        var meshCenterY = top + (int)Math.Round(position.Y / 0.62f * courtHeight);
        if (candidateTexture is not null)
        {
            if (EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
                    frame,
                    candidateMesh,
                    candidateTexture,
                    meshCenterX,
                    meshCenterY,
                    46) == 0)
            {
                EaglCandidateMeshRenderer.DrawSolid(
                    frame,
                    candidateMesh,
                    meshCenterX,
                    meshCenterY,
                    46,
                    new RgbColor(166, 42, 48));
            }
        }
        else if (!slot1AttributeDiagnostic ||
                 EaglCandidateMeshRenderer.DrawSlot1AttributeProfile(
                     frame,
                     candidateMesh,
                     meshCenterX,
                     meshCenterY,
                     46) == 0)
        {
            EaglCandidateMeshRenderer.DrawSolid(
                frame,
                candidateMesh,
                meshCenterX,
                meshCenterY,
                46,
                new RgbColor(166, 42, 48));
        }
        EaglCandidateMeshRenderer.DrawWireframe(
            frame,
            candidateMesh,
            meshCenterX,
            meshCenterY,
            46,
            wireColor);
    }

    private static void DrawAuthoredText(
        SoftwareFrameBuffer frame,
        string text,
        int x,
        int y,
        int scale,
        RgbColor color)
    {
        foreach (var character in text)
        {
            if (character == ' ')
            {
                x += 4 * scale;
                continue;
            }

            var glyph = GetAuthoredGlyph(character);
            for (var row = 0; row < glyph.Length; row++)
            {
                for (var column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] == '#')
                    {
                        frame.FillRect(x + column * scale, y + row * scale, scale, scale, color);
                    }
                }
            }

            x += (glyph[0].Length + 1) * scale;
        }
    }

    private static string[] GetAuthoredGlyph(char character) => character switch
    {
        'A' => new[] { ".#.", "#.#", "###", "#.#", "#.#" },
        'E' => new[] { "###", "#..", "##.", "#..", "###" },
        'H' => new[] { "#.#", "#.#", "###", "#.#", "#.#" },
        'I' => new[] { "###", ".#.", ".#.", ".#.", "###" },
        'M' => new[] { "#.#", "###", "###", "#.#", "#.#" },
        'O' => new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
        'T' => new[] { "###", ".#.", ".#.", ".#.", ".#." },
        'W' => new[] { "#.#", "#.#", "#.#", "###", "#.#" },
        'Y' => new[] { "#.#", "#.#", ".#.", ".#.", ".#." },
        '0' => new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
        '1' => new[] { "##.", ".#.", ".#.", ".#.", "###" },
        '2' => new[] { "##.", "..#", ".#.", "#..", "###" },
        '3' => new[] { "##.", "..#", ".##", "..#", "##." },
        '4' => new[] { "#.#", "#.#", "###", "..#", "..#" },
        '5' => new[] { "###", "#..", "##.", "..#", "##." },
        '6' => new[] { ".##", "#..", "###", "#.#", ".#." },
        '7' => new[] { "###", "..#", ".#.", ".#.", ".#." },
        '8' => new[] { ".#.", "#.#", ".#.", "#.#", ".#." },
        '9' => new[] { ".#.", "#.#", ".##", "..#", "##." },
        '-' => new[] { "...", "...", "###", "...", "..." },
        '.' => new[] { "...", "...", "...", "...", ".#." },
        _ => new[] { "...", "...", "...", "...", "..." },
    };

    private static void DrawPlayer(
        SoftwareFrameBuffer frame,
        (float X, float Y) position,
        int left,
        int top,
        int courtWidth,
        int courtHeight,
        RgbColor color,
        int radius = 15)
    {
        var x = left + (int)Math.Round(position.X * courtWidth);
        var y = top + (int)Math.Round(position.Y / 0.62f * courtHeight);
        frame.DrawCircle(x, y, radius + 3, new RgbColor(20, 25, 30));
        frame.DrawCircle(x, y, radius, color);
    }

    private static void DrawCleanRoomPlayer(
        SoftwareFrameBuffer frame,
        StreetMatch match,
        (float X, float Y) position,
        int left,
        int top,
        int courtWidth,
        int courtHeight,
        RgbColor color,
        BallOwner owner)
    {
        var x = left + (int)Math.Round(position.X * courtWidth);
        var y = top + (int)Math.Round(position.Y / 0.62f * courtHeight);
        var pose = CleanRoomPlayerAnimator.Evaluate(
            match.Frame,
            match.LastAction,
            owner,
            match.Possession == owner);
        CleanRoomCharacterRenderer.Draw(
            frame,
            x,
            y,
            pose,
            new CleanRoomCharacterPalette(
                color,
                Darken(color),
                new RgbColor(235, 188, 146),
                new RgbColor(20, 25, 30),
                new RgbColor(235, 188, 146)));
    }

    private static RgbColor Darken(RgbColor color) => new(
        (byte)(color.R * 0.65f),
        (byte)(color.G * 0.65f),
        (byte)(color.B * 0.65f));
}
