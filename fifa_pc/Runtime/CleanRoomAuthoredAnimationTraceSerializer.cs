using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Serializes the authored procedural presentation pose consumed by the PC
/// renderer. These samples are a deterministic runtime trace; they are not
/// decoded AXT/morph frames and do not claim rigging or skinning semantics.
/// </summary>
public readonly record struct CleanRoomAuthoredAnimationSample(
    int Frame,
    BallOwner Owner,
    string LastAction,
    bool HasPossession,
    CleanRoomPlayerPose Pose);

public static class CleanRoomAuthoredAnimationTraceSerializer
{
    public const string Header =
        "frame\towner\taction\tpossession\tfacing\tbody_lean\tleg_phase\tarm_reach\taction_lift\taction_pulse\ttorso_lift\thead_turn";

    public static IReadOnlyList<string> BuildLines(
        IEnumerable<CleanRoomAuthoredAnimationSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);

        var lines = new List<string> { Header };
        lines.AddRange(samples.Select(sample => string.Join(
            '\t',
            sample.Frame.ToString(CultureInfo.InvariantCulture),
            sample.Owner.ToString(),
            Escape(sample.LastAction),
            sample.HasPossession ? "True" : "False",
            Format(sample.Pose.Facing),
            Format(sample.Pose.BodyLean),
            Format(sample.Pose.LegPhase),
            Format(sample.Pose.ArmReach),
            Format(sample.Pose.ActionLift),
            Format(sample.Pose.ActionPulse),
            Format(sample.Pose.TorsoLift),
            Format(sample.Pose.HeadTurn))));
        return lines.AsReadOnly();
    }

    public static int FindFirstDifference(
        IReadOnlyList<string> expected,
        IReadOnlyList<string> actual)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        var commonCount = Math.Min(expected.Count, actual.Count);
        for (var index = 0; index < commonCount; index++)
        {
            if (!string.Equals(expected[index], actual[index], StringComparison.Ordinal))
            {
                return index;
            }
        }

        return expected.Count == actual.Count ? -1 : commonCount;
    }

    public static void Write(
        string path,
        IEnumerable<CleanRoomAuthoredAnimationSample> samples)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            BuildLines(samples),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Format(float value) =>
        value.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string Escape(string value) =>
        (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
}
