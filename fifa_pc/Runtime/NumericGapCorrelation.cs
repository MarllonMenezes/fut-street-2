using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// One row of the structural-only cross-correlation between a numeric gap,
/// its selected textual control, the normalized payload-window profile, and
/// the raw command-boundary profile. None of these fields has asset semantics.
/// </summary>
public sealed record NumericGapCorrelationObservation(
    string OffsetDeltaSignature,
    string StructuralLayoutSignature,
    string NestedArchiveName,
    string MorphObjectName,
    string PayloadWindowSignature,
    string RawCommandBoundarySignature,
    int StableRawBoundaryExactMatches,
    int WindowHashCount,
    int DistinctWindowHashCount,
    int RelativeBoundaryExactMatches = 0,
    int NumericBoundaryExactMatches = 0,
    int PayloadWindowExactMatches = 0,
    int CanonicalOffsetExactMatches = 0);

/// <summary>
/// Groups observed numeric-gap rows by their aligned signed offset-delta
/// sequence. The resulting counts and profile hashes are triage evidence only;
/// they do not identify records, vertices, bones, poses, frames, materials, or
/// GPU commands.
/// </summary>
public static class NumericGapCorrelation
{
    public static IReadOnlyList<NumericGapCorrelationProfile> Summarize(
        IReadOnlyList<NumericGapCorrelationObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        return observations
            .GroupBy(observation => observation.OffsetDeltaSignature, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var rows = group.ToArray();
                var payloadWindowGroups = rows
                    .GroupBy(row => row.PayloadWindowSignature, StringComparer.Ordinal)
                    .ToArray();
                var rawBoundaryGroups = rows
                    .GroupBy(row => row.RawCommandBoundarySignature, StringComparer.Ordinal)
                    .ToArray();
                return new NumericGapCorrelationProfile(
                    group.Key,
                    rows.Length,
                    rows.Select(row => row.StructuralLayoutSignature)
                        .Distinct(StringComparer.Ordinal)
                        .Count(),
                    payloadWindowGroups.Length,
                    payloadWindowGroups.Count(profile => profile.Count() > 1),
                    rawBoundaryGroups.Length,
                    rawBoundaryGroups.Count(profile => profile.Count() > 1),
                    rows.Count(row => row.StableRawBoundaryExactMatches > 0),
                    rows.Length == 0 ? 0 : rows.Max(row => row.StableRawBoundaryExactMatches),
                    rows.Sum(row => row.StableRawBoundaryExactMatches),
                    rows.Count(row => row.RelativeBoundaryExactMatches > 0),
                    rows.Length == 0 ? 0 : rows.Max(row => row.RelativeBoundaryExactMatches),
                    rows.Sum(row => row.RelativeBoundaryExactMatches),
                    rows.Count(row => row.NumericBoundaryExactMatches > 0),
                    rows.Sum(row => row.NumericBoundaryExactMatches),
                    rows.Count(row => row.PayloadWindowExactMatches > 0),
                    rows.Sum(row => row.PayloadWindowExactMatches),
                    rows.Count(row => row.CanonicalOffsetExactMatches > 0),
                    rows.Sum(row => row.CanonicalOffsetExactMatches),
                    JoinIntValues(rows.Select(row => row.WindowHashCount)),
                    JoinIntValues(rows.Select(row => row.DistinctWindowHashCount)),
                    JoinProfileHashes(payloadWindowGroups.Select(profile => profile.Key)),
                    JoinProfileHashes(rawBoundaryGroups.Select(profile => profile.Key)),
                    string.Join(",", rows
                        .OrderBy(row => row.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(row => row.MorphObjectName, StringComparer.OrdinalIgnoreCase)
                        .Select(row => NormalizeField(
                            $"{row.NestedArchiveName}/{row.MorphObjectName}"))));
            })
            .ToArray();
    }

    private static string JoinIntValues(IEnumerable<int> values) =>
        string.Join(",", values
            .Distinct()
            .OrderBy(value => value)
            .Select(value => value.ToString(CultureInfo.InvariantCulture)));

    private static string JoinProfileHashes(IEnumerable<string> profiles) =>
        string.Join(",", profiles
            .Select(profile => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(profile ?? string.Empty))))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(hash => hash, StringComparer.Ordinal));

    private static string NormalizeField(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}

/// <summary>
/// Aggregate structural observations for one aligned offset-delta group.
/// Profile hashes identify observed signatures without copying large opaque
/// payloads into the TSV report.
/// </summary>
public sealed record NumericGapCorrelationProfile(
    string OffsetDeltaSignature,
    int GapRowCount,
    int LayoutCount,
    int PayloadWindowProfileCount,
    int RepeatedPayloadWindowProfileCount,
    int RawCommandBoundaryProfileCount,
    int RepeatedRawCommandBoundaryProfileCount,
    int RowsWithStableRawBoundaryMatch,
    int MaxStableRawBoundaryExactMatches,
    int TotalStableRawBoundaryExactMatches,
    int RowsWithRelativeBoundaryMatch,
    int MaxRelativeBoundaryExactMatches,
    int TotalRelativeBoundaryExactMatches,
    int RowsWithNumericBoundaryMatch,
    int TotalNumericBoundaryExactMatches,
    int RowsWithPayloadWindowMatch,
    int TotalPayloadWindowExactMatches,
    int RowsWithCanonicalOffsetMatch,
    int TotalCanonicalOffsetExactMatches,
    string WindowHashCountValues,
    string DistinctWindowHashCountValues,
    string PayloadWindowProfileHashes,
    string RawCommandBoundaryProfileHashes,
    string GapRows);
