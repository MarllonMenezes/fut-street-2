using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Persists the bounded EVT string-index search as structural evidence. The
/// report intentionally records only aligned-word observations and explicit
/// negative results; it never assigns event IDs, sample selection, or
/// original playback semantics.
/// </summary>
public sealed record AudioEventIndexEvidenceReportData(
    string EventPath,
    int StringTableOffset,
    int CandidatePairs,
    int StringHashFamilyCandidates,
    int DistinctStringOffsets,
    int DistinctFollowingWords,
    int AbkBanksScanned,
    int AbkAlignedOccurrences,
    int AbkValuesWithHits,
    int HeaderAlignedOccurrences,
    int HeaderValuesWithHits,
    bool DataSearchRequested,
    int DataAlignedOccurrences,
    int DataValuesWithHits,
    string CandidateNames,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(AudioEventIndexEvidenceReportData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        return new[]
        {
            Header,
            Line("event_path", data.EventPath),
            Line("string_table_offset", $"0x{data.StringTableOffset:X}"),
            Line("candidate_pairs", data.CandidatePairs.ToString(CultureInfo.InvariantCulture)),
            Line("string_hash_family_candidates", data.StringHashFamilyCandidates.ToString(CultureInfo.InvariantCulture)),
            Line("distinct_string_offsets", data.DistinctStringOffsets.ToString(CultureInfo.InvariantCulture)),
            Line("distinct_following_words", data.DistinctFollowingWords.ToString(CultureInfo.InvariantCulture)),
            Line("abk_banks_scanned", data.AbkBanksScanned.ToString(CultureInfo.InvariantCulture)),
            Line("abk_aligned_occurrences", data.AbkAlignedOccurrences.ToString(CultureInfo.InvariantCulture)),
            Line("abk_values_with_hits", data.AbkValuesWithHits.ToString(CultureInfo.InvariantCulture)),
            Line("pcpahdr_big_aligned_occurrences", data.HeaderAlignedOccurrences.ToString(CultureInfo.InvariantCulture)),
            Line("pcpahdr_big_values_with_hits", data.HeaderValuesWithHits.ToString(CultureInfo.InvariantCulture)),
            Line("pcpadat_big_search_requested", data.DataSearchRequested.ToString()),
            Line("pcpadat_big_aligned_occurrences", data.DataAlignedOccurrences.ToString(CultureInfo.InvariantCulture)),
            Line("pcpadat_big_values_with_hits", data.DataValuesWithHits.ToString(CultureInfo.InvariantCulture)),
            Line("candidate_names", data.CandidateNames),
            Line("status", data.Status),
            Line("direct_selection_binding", data.DirectSelectionBinding),
            Line("original_semantics", data.OriginalSemantics),
        };
    }

    public static void Write(string path, AudioEventIndexEvidenceReportData data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            BuildLines(data),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Line(string field, string value) =>
        $"{field}\t{Escape(value)}";

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}

public static class AudioEventIndexEvidenceReport
{
    public const string Header = AudioEventIndexEvidenceReportData.Header;

    public static IReadOnlyList<string> BuildLines(AudioEventIndexEvidenceReportData data) =>
        AudioEventIndexEvidenceReportData.BuildLines(data);

    public static void Write(string path, AudioEventIndexEvidenceReportData data) =>
        AudioEventIndexEvidenceReportData.Write(path, data);
}
