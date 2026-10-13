using System.Security.Cryptography;

namespace FifaStreetPc;

/// <summary>
/// Compares two observed byte sequences without assigning meaning to their
/// fields. The distance is deliberately limited to aligned bytes and length;
/// it is a triage measure, not a parser or semantic decoder.
/// </summary>
public sealed record ByteSequenceComparison(
    int LeftLength,
    int RightLength,
    int ComparedLength,
    int EqualBytes,
    int DifferentBytes,
    int CommonPrefixLength,
    string LeftSha256,
    string RightSha256)
{
    public int LengthDelta => RightLength - LeftLength;

    public bool IsExact =>
        LeftLength == RightLength &&
        DifferentBytes == 0;
}

public static class ByteSequenceComparer
{
    public static ByteSequenceComparison Compare(
        ReadOnlySpan<byte> left,
        ReadOnlySpan<byte> right)
    {
        var comparedLength = Math.Min(left.Length, right.Length);
        var equalBytes = 0;
        var differentBytes = 0;
        var commonPrefixLength = 0;
        var prefixStillEqual = true;
        for (var index = 0; index < comparedLength; index++)
        {
            if (left[index] == right[index])
            {
                equalBytes++;
                if (prefixStillEqual)
                {
                    commonPrefixLength++;
                }
            }
            else
            {
                differentBytes++;
                prefixStillEqual = false;
            }
        }

        return new ByteSequenceComparison(
            left.Length,
            right.Length,
            comparedLength,
            equalBytes,
            differentBytes,
            commonPrefixLength,
            Convert.ToHexString(SHA256.HashData(left)),
            Convert.ToHexString(SHA256.HashData(right)));
    }
}
