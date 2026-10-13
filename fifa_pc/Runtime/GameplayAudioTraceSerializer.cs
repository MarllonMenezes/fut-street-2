using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Authored clean-room observation of the managed gameplay-to-audio route.
/// CandidateName and Route describe the PC policy only; they are not decoded
/// EVT, ABKC or original gameplay bindings.
/// </summary>
public readonly record struct CleanRoomGameplayAudioRoute(
    int Sequence,
    string Action,
    string CandidateName,
    string Route,
    int WaveBytes = 0,
    long PcmBytes = 0,
    string PcmSha256 = "");

public static class GameplayAudioTraceSerializer
{
    public const string Header =
        "sequence\taction\tcandidate\troute\twave_bytes\tpcm_bytes\tpcm_sha256";

    public static IReadOnlyList<string> BuildLines(
        IReadOnlyList<CleanRoomGameplayAudioRoute> routes)
    {
        ArgumentNullException.ThrowIfNull(routes);

        var lines = new List<string>(routes.Count + 1) { Header };
        lines.AddRange(routes.Select(route => string.Join(
            '\t',
            route.Sequence.ToString(CultureInfo.InvariantCulture),
            Escape(route.Action),
            Escape(route.CandidateName),
            Escape(route.Route),
            route.WaveBytes.ToString(CultureInfo.InvariantCulture),
            route.PcmBytes.ToString(CultureInfo.InvariantCulture),
            Escape(route.PcmSha256))));
        return lines.AsReadOnly();
    }

    public static void Write(
        string path,
        IReadOnlyList<CleanRoomGameplayAudioRoute> routes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        File.WriteAllLines(
            Path.GetFullPath(path),
            BuildLines(routes),
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

    private static string Escape(string? value) =>
        (value ?? string.Empty)
            .Replace('\t', ' ')
            .Replace('\r', ' ')
            .Replace('\n', ' ');
}
