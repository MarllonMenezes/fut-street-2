using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// A validated, frame-indexed input trace for the managed clean-room match.
/// It describes authored PC controls only; it is not an extraction of input
/// events or rules from the original game.
/// </summary>
public sealed record MatchInputScript(IReadOnlyList<MatchInput> Inputs)
{
    public int FrameCount => Inputs.Count;
}

public static class MatchInputScriptReader
{
    public static MatchInputScript Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = File.ReadAllLines(Path.GetFullPath(path));
        var inputs = new List<MatchInput>();
        var expectedFrame = 0;
        var sawHeader = false;
        var hasPannaColumn = false;
        var hasGamebreakerColumn = false;
        var hasJuggleColumn = false;
        var hasTackleColumn = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var columns = line.Split('\t');
            if (!sawHeader && inputs.Count == 0 &&
                string.Equals(columns[0], "frame", StringComparison.OrdinalIgnoreCase))
            {
                if ((columns.Length != 6 && columns.Length != 7 && columns.Length != 8 && columns.Length != 9 && columns.Length != 10) ||
                    !string.Equals(columns[1], "move_x", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(columns[2], "move_y", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(columns[3], "pass", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(columns[4], "shoot", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(columns[5], "trick", StringComparison.OrdinalIgnoreCase) ||
                     (columns.Length >= 7 &&
                      !string.Equals(columns[6], "panna", StringComparison.OrdinalIgnoreCase)) ||
                     (columns.Length >= 8 &&
                      !string.Equals(columns[7], "gamebreaker", StringComparison.OrdinalIgnoreCase)) ||
                     (columns.Length >= 9 &&
                      !string.Equals(columns[8], "juggle", StringComparison.OrdinalIgnoreCase)) ||
                     (columns.Length == 10 &&
                      !string.Equals(columns[9], "tackle", StringComparison.OrdinalIgnoreCase)))
                {
                    throw new InvalidDataException("Input script header must be frame, move_x, move_y, pass, shoot, trick, optionally panna, gamebreaker, juggle and tackle.");
                }

                hasPannaColumn = columns.Length >= 7;
                hasGamebreakerColumn = columns.Length >= 8;
                hasJuggleColumn = columns.Length >= 9;
                hasTackleColumn = columns.Length == 10;
                sawHeader = true;
                continue;
            }

            var expectedColumns = hasTackleColumn ? 10 : hasJuggleColumn ? 9 : hasGamebreakerColumn ? 8 : hasPannaColumn ? 7 : 6;
            if (columns.Length != expectedColumns)
            {
                throw new InvalidDataException(
                    $"Input script row {expectedFrame} must contain exactly {expectedColumns} tab-separated columns.");
            }

            if (!int.TryParse(columns[0], NumberStyles.None, CultureInfo.InvariantCulture, out var frame) ||
                frame != expectedFrame)
            {
                throw new InvalidDataException(
                    $"Input script frame must be contiguous and start at zero; expected {expectedFrame}.");
            }

            var moveX = ParseAxis(columns[1], expectedFrame, "move_x");
            var moveY = ParseAxis(columns[2], expectedFrame, "move_y");
            var pass = ParseBoolean(columns[3], expectedFrame, "pass");
            var shoot = ParseBoolean(columns[4], expectedFrame, "shoot");
            var trick = ParseBoolean(columns[5], expectedFrame, "trick");
            var panna = hasPannaColumn && ParseBoolean(columns[6], expectedFrame, "panna");
            var gamebreaker = hasGamebreakerColumn && ParseBoolean(columns[7], expectedFrame, "gamebreaker");
            var juggle = hasJuggleColumn && ParseBoolean(columns[8], expectedFrame, "juggle");
            var tackle = hasTackleColumn && ParseBoolean(columns[9], expectedFrame, "tackle");
            inputs.Add(new MatchInput(moveX, moveY, pass, shoot, trick, panna, gamebreaker, juggle, tackle));
            expectedFrame++;
        }

        if (inputs.Count == 0)
        {
            throw new InvalidDataException("Input script contains no frame rows.");
        }

        return new MatchInputScript(inputs.AsReadOnly());
    }

    private static float ParseAxis(string value, int frame, string column)
    {
        if (!float.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsed) ||
            float.IsNaN(parsed) ||
            float.IsInfinity(parsed) ||
            parsed < -1.0f ||
            parsed > 1.0f)
        {
            throw new InvalidDataException(
                $"Input script {column} at frame {frame} must be a finite value in [-1,1].");
        }

        return parsed;
    }

    private static bool ParseBoolean(string value, int frame, string column) =>
        value.Trim().ToLowerInvariant() switch
        {
            "0" => false,
            "1" => true,
            "false" => false,
            "true" => true,
            _ => throw new InvalidDataException(
                $"Input script {column} at frame {frame} must be 0, 1, false or true."),
        };
}

public sealed class MatchInputScriptSource : IInputSource
{
    private readonly IReadOnlyList<MatchInput> inputs;
    private int frame;

    public MatchInputScriptSource(MatchInputScript script)
    {
        ArgumentNullException.ThrowIfNull(script);
        inputs = script.Inputs;
    }

    public int FrameCount => inputs.Count;

    public MatchInput Poll() => frame < inputs.Count ? inputs[frame++] : new MatchInput();
}
