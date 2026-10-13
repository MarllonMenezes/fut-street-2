using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Serializes the input samples consumed by the managed clean-room runtime.
/// These rows document authored control delivery only; they are not decoded
/// controller bindings or original-game input semantics.
/// </summary>
public readonly record struct CleanRoomInputSample(int Frame, MatchInput Input);

public static class CleanRoomInputTraceSerializer
{
    public const string Header =
        "frame\tmove_x\tmove_y\tpass\tshoot\ttrick\tpanna\tgamebreaker\tjuggle\ttackle";

    public static IReadOnlyList<string> BuildLines(
        IReadOnlyList<CleanRoomInputSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var lines = new List<string>(samples.Count + 1) { Header };
        lines.AddRange(samples.Select(sample => string.Join(
            '\t',
            sample.Frame.ToString(CultureInfo.InvariantCulture),
            FormatFloat(sample.Input.MoveX),
            FormatFloat(sample.Input.MoveY),
            sample.Input.Pass.ToString(),
            sample.Input.Shoot.ToString(),
            sample.Input.Trick.ToString(),
            sample.Input.Panna.ToString(),
            sample.Input.Gamebreaker.ToString(),
            sample.Input.Juggle.ToString(),
            sample.Input.Tackle.ToString())));
        return lines.AsReadOnly();
    }

    public static void Write(
        string path,
        IReadOnlyList<CleanRoomInputSample> samples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllLines(
            Path.GetFullPath(path),
            BuildLines(samples),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    public static int FindFirstDifference(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var comparedCount = Math.Min(expected.Count, actual.Count);
        for (var index = 0; index < comparedCount; index++)
        {
            if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal))
            {
                return index;
            }
        }

        return expected.Count == actual.Count ? -1 : comparedCount;
    }

    private static string FormatFloat(float value) =>
        value.ToString("R", CultureInfo.InvariantCulture);
}
