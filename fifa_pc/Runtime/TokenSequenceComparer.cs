namespace FifaStreetPc;

/// <summary>
/// Compares ordered textual tokens without assigning semantic meaning to them.
/// The result is a structural triage measure for observed record-like
/// sequences, not a decoder.
/// </summary>
public sealed record TokenSequenceComparison(
    int LeftCount,
    int RightCount,
    int ComparedCount,
    int EqualTokens,
    int DifferentTokens,
    int CommonPrefixLength,
    int PrefixFieldDifferences = 0,
    int SuffixFieldDifferences = 0)
{
    public IReadOnlyList<int> DifferentTokenIndexes { get; init; } = Array.Empty<int>();

    public int LengthDelta => RightCount - LeftCount;

    public bool IsExact =>
        LeftCount == RightCount &&
        DifferentTokens == 0;
}

public static class TokenSequenceComparer
{
    public static TokenSequenceComparison Compare(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var comparedCount = Math.Min(left.Count, right.Count);
        var equalTokens = 0;
        var differentTokens = 0;
        var commonPrefixLength = 0;
        var differentTokenIndexes = new List<int>();
        var prefixStillEqual = true;
        for (var index = 0; index < comparedCount; index++)
        {
            if (string.Equals(left[index], right[index], StringComparison.Ordinal))
            {
                equalTokens++;
                if (prefixStillEqual)
                {
                    commonPrefixLength++;
                }
            }
            else
            {
                differentTokens++;
                differentTokenIndexes.Add(index);
                prefixStillEqual = false;
            }
        }

        return new TokenSequenceComparison(
            left.Count,
            right.Count,
            comparedCount,
            equalTokens,
            differentTokens,
            commonPrefixLength)
        {
            DifferentTokenIndexes = differentTokenIndexes,
        };
    }

    /// <summary>
    /// Compares the two textual portions around an observed delimiter while
    /// retaining the same positional, non-semantic interpretation. The
    /// portions are intentionally called prefix and suffix; they are not
    /// promoted to value, category, or asset fields.
    /// </summary>
    public static TokenSequenceComparison CompareDelimited(
        IReadOnlyList<string> left,
        IReadOnlyList<string> right,
        char delimiter)
    {
        var comparison = Compare(left, right);
        var prefixFieldDifferences = 0;
        var suffixFieldDifferences = 0;
        for (var index = 0; index < comparison.ComparedCount; index++)
        {
            var leftParts = SplitToken(left[index], delimiter);
            var rightParts = SplitToken(right[index], delimiter);
            if (!string.Equals(leftParts.Prefix, rightParts.Prefix, StringComparison.Ordinal))
            {
                prefixFieldDifferences++;
            }
            if (!string.Equals(leftParts.Suffix, rightParts.Suffix, StringComparison.Ordinal))
            {
                suffixFieldDifferences++;
            }
        }

        return comparison with
        {
            PrefixFieldDifferences = prefixFieldDifferences,
            SuffixFieldDifferences = suffixFieldDifferences,
        };
    }

    private static (string Prefix, string Suffix) SplitToken(string token, char delimiter)
    {
        var delimiterIndex = token.IndexOf(delimiter);
        return delimiterIndex < 0
            ? (token, string.Empty)
            : (token[..delimiterIndex], token[(delimiterIndex + 1)..]);
    }
}
