using System.Buffers.Binary;
using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// Represents one observed little-endian word match from the morph section.
/// Offset, word and suffix are preserved as observations; none is assigned an
/// asset-level meaning.
/// </summary>
public sealed record ObservedNumericWordToken(
    uint Offset,
    uint Word,
    string Suffix);

/// <summary>
/// Compares aligned observed words and their textual suffixes. The byte count
/// is the difference count in the four little-endian bytes of each compared
/// word, not a decoded vertex, index, pose, or GPU field.
/// </summary>
public sealed record NumericTokenSequenceComparison(
    int LeftCount,
    int RightCount,
    int ComparedCount,
    int DifferentOffsets,
    int DifferentWords,
    int DifferentSuffixes,
    int DifferentLittleEndianBytes,
    int CommonPrefixLength,
    string OffsetDeltaSignature = "")
{
    public int LengthDelta => RightCount - LeftCount;
}

public static class NumericTokenSequenceComparer
{
    public static IReadOnlyList<ObservedNumericWordToken> ParseCanonical(string? canonical)
    {
        if (string.IsNullOrEmpty(canonical))
        {
            return Array.Empty<ObservedNumericWordToken>();
        }

        return canonical
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseToken)
            .ToArray();
    }

    public static NumericTokenSequenceComparison Compare(
        IReadOnlyList<ObservedNumericWordToken> left,
        IReadOnlyList<ObservedNumericWordToken> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var comparedCount = Math.Min(left.Count, right.Count);
        var differentOffsets = 0;
        var differentWords = 0;
        var differentSuffixes = 0;
        var differentLittleEndianBytes = 0;
        var commonPrefixLength = 0;
        var offsetDeltas = new long[comparedCount];
        Span<byte> leftBytes = stackalloc byte[sizeof(uint)];
        Span<byte> rightBytes = stackalloc byte[sizeof(uint)];
        var prefixStillEqual = true;
        for (var index = 0; index < comparedCount; index++)
        {
            var leftToken = left[index];
            var rightToken = right[index];
            var offsetsEqual = leftToken.Offset == rightToken.Offset;
            offsetDeltas[index] = (long)leftToken.Offset - rightToken.Offset;
            var wordsEqual = leftToken.Word == rightToken.Word;
            var suffixesEqual = string.Equals(
                leftToken.Suffix,
                rightToken.Suffix,
                StringComparison.Ordinal);
            if (!offsetsEqual)
            {
                differentOffsets++;
            }
            if (!wordsEqual)
            {
                differentWords++;
            }
            if (!suffixesEqual)
            {
                differentSuffixes++;
            }

            BinaryPrimitives.WriteUInt32LittleEndian(leftBytes, leftToken.Word);
            BinaryPrimitives.WriteUInt32LittleEndian(rightBytes, rightToken.Word);
            for (var byteIndex = 0; byteIndex < leftBytes.Length; byteIndex++)
            {
                if (leftBytes[byteIndex] != rightBytes[byteIndex])
                {
                    differentLittleEndianBytes++;
                }
            }

            if (prefixStillEqual && offsetsEqual && wordsEqual && suffixesEqual)
            {
                commonPrefixLength++;
            }
            else
            {
                prefixStillEqual = false;
            }
        }

        return new NumericTokenSequenceComparison(
            left.Count,
            right.Count,
            comparedCount,
            differentOffsets,
            differentWords,
            differentSuffixes,
            differentLittleEndianBytes,
            commonPrefixLength,
            string.Join(",", offsetDeltas.Select(delta =>
                delta.ToString(CultureInfo.InvariantCulture))));
    }

    /// <summary>
    /// Returns a base-independent profile of the observed token offsets. Only
    /// adjacent offset deltas are retained; the profile does not assign those
    /// boundaries to records, vertices, bones, poses, frames, or GPU fields.
    /// </summary>
    public static string GetRelativeOffsetBoundarySignature(
        IReadOnlyList<ObservedNumericWordToken> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        if (tokens.Count == 0)
        {
            return "records=0;span=0;adjacent_deltas=;delta_histogram=";
        }

        var deltas = new long[Math.Max(0, tokens.Count - 1)];
        for (var index = 1; index < tokens.Count; index++)
        {
            deltas[index - 1] = (long)tokens[index].Offset - tokens[index - 1].Offset;
        }

        var histogram = deltas
            .GroupBy(delta => delta)
            .OrderBy(group => group.Key)
            .Select(group =>
                $"{group.Key.ToString(CultureInfo.InvariantCulture)}={group.Count()}");
        var span = (long)tokens[^1].Offset - tokens[0].Offset;
        return string.Join(
            ";",
            $"records={tokens.Count.ToString(CultureInfo.InvariantCulture)}",
            $"span={span.ToString(CultureInfo.InvariantCulture)}",
            $"adjacent_deltas={string.Join(",", deltas.Select(delta => delta.ToString(CultureInfo.InvariantCulture)))}",
            $"delta_histogram={string.Join(",", histogram)}");
    }

    private static ObservedNumericWordToken ParseToken(string token)
    {
        var offsetSeparator = token.IndexOf(':');
        var wordSeparator = offsetSeparator < 0
            ? -1
            : token.IndexOf(':', offsetSeparator + 1);
        if (offsetSeparator <= 0 || wordSeparator <= offsetSeparator)
        {
            throw new FormatException($"Invalid canonical numeric token: {token}");
        }

        return new ObservedNumericWordToken(
            ParseHex(token.AsSpan(0, offsetSeparator)),
            ParseHex(token.AsSpan(offsetSeparator + 1, wordSeparator - offsetSeparator - 1)),
            token[(wordSeparator + 1)..]);
    }

    private static uint ParseHex(ReadOnlySpan<char> value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value[2..];
        }
        return uint.Parse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
    }
}
