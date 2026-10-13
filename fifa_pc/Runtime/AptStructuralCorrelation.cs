using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// Raw, aligned header-word observations for one APT package. The words are
/// retained as observed values; this type assigns no timing, pose, frame, or
/// other animation meaning.
/// </summary>
public sealed record AptStructuralObservation(
    string AptHeaderPattern,
    string ConstantHeaderPattern,
    string CompanionExtensions,
    bool AptFieldsWithinAsset,
    bool ConstantFieldsWithinAsset,
    bool ObjectValid,
    bool ShapeValid,
    bool ObjectHasMorphSymbol,
    IReadOnlyList<uint> AptFields,
    IReadOnlyList<uint> ConstantFields,
    string ObjectStructuralSignatureHash,
    string ShapeStructuralSignatureHash);

/// <summary>
/// Aggregate structural profiles for APT and same-stem constant/object/shape
/// observations. Equal aligned words are reported only as byte-level
/// recurrence; they are not promoted to animation fields.
/// </summary>
public sealed record AptStructuralProfile(
    string AptHeaderPattern,
    string ConstantHeaderPattern,
    string CompanionExtensions,
    int RowCount,
    int RowsWithAlignedEqualWords,
    int TotalAlignedComparedWords,
    int TotalAlignedEqualWords,
    int TotalAlignedDifferentWords,
    int AptFieldsWithinAssetRows,
    int ConstantFieldsWithinAssetRows,
    int ObjectValidRows,
    int ShapeValidRows,
    int MorphSymbolRows,
    int DistinctObjectStructuralSignatureHashes,
    int DistinctShapeStructuralSignatureHashes)
{
    public string AlignedEqualWordRatio =>
        TotalAlignedComparedWords == 0
            ? string.Empty
            : (TotalAlignedEqualWords / (double)TotalAlignedComparedWords)
                .ToString("0.####", CultureInfo.InvariantCulture);
}

public static class AptStructuralCorrelation
{
    public static IReadOnlyList<AptStructuralProfile> Summarize(
        IReadOnlyList<AptStructuralObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);

        return observations
            .GroupBy(observation => string.Join(
                "\u001F",
                observation.AptHeaderPattern,
                observation.ConstantHeaderPattern,
                observation.CompanionExtensions),
                StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var rows = group.ToArray();
                var alignedComparisons = rows
                    .Select(row =>
                    {
                        var compared = Math.Min(row.AptFields.Count, row.ConstantFields.Count);
                        var equal = Enumerable.Range(0, compared)
                            .Count(index => row.AptFields[index] == row.ConstantFields[index]);
                        return (Compared: compared, Equal: equal);
                    })
                    .ToArray();
                var aptPattern = rows[0].AptHeaderPattern;
                var constantPattern = rows[0].ConstantHeaderPattern;
                var companionExtensions = rows[0].CompanionExtensions;
                return new AptStructuralProfile(
                    aptPattern,
                    constantPattern,
                    companionExtensions,
                    rows.Length,
                    alignedComparisons.Count(item => item.Equal > 0),
                    alignedComparisons.Sum(item => item.Compared),
                    alignedComparisons.Sum(item => item.Equal),
                    alignedComparisons.Sum(item => item.Compared - item.Equal),
                    rows.Count(row => row.AptFieldsWithinAsset),
                    rows.Count(row => row.ConstantFieldsWithinAsset),
                    rows.Count(row => row.ObjectValid),
                    rows.Count(row => row.ShapeValid),
                    rows.Count(row => row.ObjectHasMorphSymbol),
                    rows.Select(row => row.ObjectStructuralSignatureHash)
                        .Where(hash => hash.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .Count(),
                    rows.Select(row => row.ShapeStructuralSignatureHash)
                        .Where(hash => hash.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .Count());
            })
            .ToArray();
    }
}
