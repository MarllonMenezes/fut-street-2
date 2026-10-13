using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Serializes and compares authored clean-room match telemetry. The rows are
/// runtime evidence for the managed simulation only; they are not decoded
/// original-game events.
/// </summary>
public static class MatchTraceSerializer
{
    public const string Header = "sequence\tframe\taction\thome_score\taway_score\tpossession";

    public static IReadOnlyList<string> BuildLines(IReadOnlyList<CleanRoomMatchEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var lines = new List<string>(events.Count + 1) { Header };
        lines.AddRange(events.Select(matchEvent => string.Join(
            '\t',
            matchEvent.Sequence.ToString(CultureInfo.InvariantCulture),
            matchEvent.Frame.ToString(CultureInfo.InvariantCulture),
            Escape(matchEvent.Action),
            matchEvent.HomeScore.ToString(CultureInfo.InvariantCulture),
            matchEvent.AwayScore.ToString(CultureInfo.InvariantCulture),
            matchEvent.Possession.ToString())));
        return lines.AsReadOnly();
    }

    public static void Write(string path, IReadOnlyList<CleanRoomMatchEvent> events)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllLines(
            Path.GetFullPath(path),
            BuildLines(events),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    /// <summary>
    /// Returns the zero-based line index of the first exact difference, or -1
    /// when both line sequences match. The comparison intentionally remains
    /// textual and does not decode or reinterpret event fields.
    /// </summary>
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

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
}
