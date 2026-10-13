using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankTailObservation(
    string CatalogEntry,
    string BankName,
    int DeclaredFileCount,
    int CatalogFileCount,
    bool DeclaredFileCountMatchesCatalog,
    uint DataSectionSize,
    uint AnimationBankSymbolValue,
    uint AnimationBankSymbolSize,
    int TailByteCount,
    ushort TailOffset4LittleEndian,
    uint CatalogFileCountTimesTwo,
    bool TailOffset4EqualsCatalogFileCountTimesTwo,
    uint DeclaredFileCountTimesTwo,
    bool TailOffset4EqualsDeclaredFileCountTimesTwo,
    string RelocationOffsets,
    string RawTailHex,
    string NormalizedTailHex,
    string RawTailSha256,
    string NormalizedTailSha256);

public sealed record AnimationBankTailComparisonSummary(
    int ObservationCount,
    int MinimumTailByteCount,
    int ExactRawTailPairs,
    int ExactNormalizedTailPairs,
    int DistinctRawTailHashes,
    int DistinctNormalizedTailHashes,
    int CommonRawOffsetCount,
    int CommonNormalizedOffsetCount,
    int CommonRawPrefixByteCount,
    int CommonNormalizedPrefixByteCount,
    int DeclaredFileCountMatchCount,
    int CatalogFileCountTimesTwoMatchCount,
    int DeclaredFileCountTimesTwoMatchCount,
    string CommonRawOffsetValues,
    string CommonNormalizedOffsetValues);

/// <summary>
/// Compares the data-section tail beginning at the observed
/// __AnimationBank::: symbol. Only section bounds, raw bytes, relocation-masked
/// bytes, and repeatability metrics are retained; no tail field receives
/// header, record, frame, pose, rig, timing, or selector semantics.
/// </summary>
public static class AnimationBankTailComparison
{
    public static AnimationBankTailObservation Build(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> combinedElfData,
        ElfInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(inspection);

        var dataSectionIndex = -1;
        ElfSection? dataSection = null;
        for (var index = 0; index < inspection.Sections.Count; index++)
        {
            if (string.Equals(inspection.Sections[index].Name, ".data", StringComparison.Ordinal))
            {
                dataSectionIndex = index;
                dataSection = inspection.Sections[index];
                break;
            }
        }

        var animationBankSymbol = inspection.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal));
        if (dataSection is null || animationBankSymbol is null ||
            (ulong)dataSection.Offset + dataSection.Size > (ulong)combinedElfData.Length ||
            animationBankSymbol.Value > dataSection.Size ||
            animationBankSymbol.Size > dataSection.Size - animationBankSymbol.Value)
        {
            throw new InvalidDataException("Animation bank symbol or .data section is outside the ELF payload.");
        }

        var data = combinedElfData
            .Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size));
        var tail = data
            .Slice(checked((int)animationBankSymbol.Value))
            .ToArray();
        var normalizedTail = tail.ToArray();
        var relocationOffsets = inspection.Relocations
            .Where(relocation => relocation.TargetSectionIndex == dataSectionIndex &&
                                 relocation.Offset < dataSection.Size &&
                                 relocation.Offset >= animationBankSymbol.Value)
            .OrderBy(relocation => relocation.Offset)
            .ThenBy(relocation => relocation.Type)
            .ToArray();
        foreach (var relocation in relocationOffsets)
        {
            var relativeOffset = relocation.Offset - animationBankSymbol.Value;
            if (relativeOffset > (uint)normalizedTail.Length ||
                sizeof(uint) > normalizedTail.Length - relativeOffset)
            {
                throw new InvalidDataException("Animation bank tail relocation exceeds the observed tail.");
            }

            normalizedTail.AsSpan(checked((int)relativeOffset), sizeof(uint)).Clear();
        }

        var tailOffset4LittleEndian = tail.Length >= 6
            ? BinaryPrimitives.ReadUInt16LittleEndian(tail.AsSpan(4, sizeof(ushort)))
            : (ushort)0;
        var catalogFileCountTimesTwo = checked((uint)catalog.Files.Count * 2u);
        var declaredFileCountTimesTwo = checked((uint)catalog.DeclaredFileCount * 2u);

        return new AnimationBankTailObservation(
            catalogEntry,
            catalog.BankName,
            catalog.DeclaredFileCount,
            catalog.Files.Count,
            catalog.DeclaredFileCountMatches,
            dataSection.Size,
            animationBankSymbol.Value,
            animationBankSymbol.Size,
            tail.Length,
            tailOffset4LittleEndian,
            catalogFileCountTimesTwo,
            tailOffset4LittleEndian == catalogFileCountTimesTwo,
            declaredFileCountTimesTwo,
            tailOffset4LittleEndian == declaredFileCountTimesTwo,
            string.Join(",", relocationOffsets.Select(relocation =>
                $"0x{relocation.Offset - animationBankSymbol.Value:X}:0x{relocation.Type:X}")),
            Convert.ToHexString(tail),
            Convert.ToHexString(normalizedTail),
            Hash(tail),
            Hash(normalizedTail));
    }

    public static AnimationBankTailComparisonSummary Summarize(
        IEnumerable<AnimationBankTailObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var rows = observations.ToArray();
        var minimumTailByteCount = rows.Length == 0 ? 0 : rows.Min(row => row.TailByteCount);
        var exactRawPairs = 0;
        var exactNormalizedPairs = 0;
        for (var firstIndex = 0; firstIndex < rows.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < rows.Length; secondIndex++)
            {
                if (string.Equals(rows[firstIndex].RawTailHex, rows[secondIndex].RawTailHex, StringComparison.Ordinal))
                {
                    exactRawPairs++;
                }

                if (string.Equals(rows[firstIndex].NormalizedTailHex, rows[secondIndex].NormalizedTailHex, StringComparison.Ordinal))
                {
                    exactNormalizedPairs++;
                }
            }
        }

        var commonRawOffsetValues = new List<byte>();
        var commonNormalizedOffsetValues = new List<byte>();
        for (var offset = 0; offset < minimumTailByteCount; offset++)
        {
            var rawValue = ParseHexByte(rows[0].RawTailHex, offset);
            var normalizedValue = ParseHexByte(rows[0].NormalizedTailHex, offset);
            if (rows.All(row => ParseHexByte(row.RawTailHex, offset) == rawValue))
            {
                commonRawOffsetValues.Add(rawValue);
            }

            if (rows.All(row => ParseHexByte(row.NormalizedTailHex, offset) == normalizedValue))
            {
                commonNormalizedOffsetValues.Add(normalizedValue);
            }
        }

        return new AnimationBankTailComparisonSummary(
            rows.Length,
            minimumTailByteCount,
            exactRawPairs,
            exactNormalizedPairs,
            rows.Select(row => row.RawTailSha256).Distinct(StringComparer.Ordinal).Count(),
            rows.Select(row => row.NormalizedTailSha256).Distinct(StringComparer.Ordinal).Count(),
            commonRawOffsetValues.Count,
            commonNormalizedOffsetValues.Count,
            GetCommonPrefixByteCount(rows.Select(row => row.RawTailHex).ToArray()),
            GetCommonPrefixByteCount(rows.Select(row => row.NormalizedTailHex).ToArray()),
            rows.Count(row => row.DeclaredFileCountMatchesCatalog),
            rows.Count(row => row.TailOffset4EqualsCatalogFileCountTimesTwo),
            rows.Count(row => row.TailOffset4EqualsDeclaredFileCountTimesTwo),
            string.Join(",", commonRawOffsetValues.Select(value => $"0x{value:X2}")),
            string.Join(",", commonNormalizedOffsetValues.Select(value => $"0x{value:X2}")));
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankTailObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(observations);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "declared_file_count",
                "catalog_file_count",
                "declared_file_count_matches_catalog",
                "data_section_size",
                "animation_bank_symbol_value",
                "animation_bank_symbol_size",
                "tail_byte_count",
                "tail_offset_4_little_endian",
                "catalog_file_count_times_two",
                "tail_offset_4_equals_catalog_file_count_times_two",
                "declared_file_count_times_two",
                "tail_offset_4_equals_declared_file_count_times_two",
                "relocation_offsets",
                "raw_tail_hex",
                "normalized_tail_hex",
                "raw_tail_sha256",
                "normalized_tail_sha256",
            }),
        };
        lines.AddRange(observations.Select(row => string.Join("\t", new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.DeclaredFileCount.ToString(CultureInfo.InvariantCulture),
            row.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            row.DeclaredFileCountMatchesCatalog.ToString(),
            row.DataSectionSize.ToString(CultureInfo.InvariantCulture),
            row.AnimationBankSymbolValue.ToString(CultureInfo.InvariantCulture),
            row.AnimationBankSymbolSize.ToString(CultureInfo.InvariantCulture),
            row.TailByteCount.ToString(CultureInfo.InvariantCulture),
            row.TailOffset4LittleEndian.ToString(CultureInfo.InvariantCulture),
            row.CatalogFileCountTimesTwo.ToString(CultureInfo.InvariantCulture),
            row.TailOffset4EqualsCatalogFileCountTimesTwo.ToString(),
            row.DeclaredFileCountTimesTwo.ToString(CultureInfo.InvariantCulture),
            row.TailOffset4EqualsDeclaredFileCountTimesTwo.ToString(),
            Tsv(row.RelocationOffsets),
            row.RawTailHex,
            row.NormalizedTailHex,
            row.RawTailSha256,
            row.NormalizedTailSha256,
        })));
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static int GetCommonPrefixByteCount(IReadOnlyList<string> hexRows)
    {
        if (hexRows.Count == 0)
        {
            return 0;
        }

        var minimumHexLength = hexRows.Min(row => row.Length);
        var prefixLength = 0;
        while (prefixLength < minimumHexLength &&
               hexRows.All(row => row[prefixLength] == hexRows[0][prefixLength]))
        {
            prefixLength++;
        }

        return prefixLength / 2;
    }

    private static byte ParseHexByte(string hex, int byteOffset) =>
        byte.Parse(hex.AsSpan(checked(byteOffset * 2), 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string Tsv(string? value) =>
        value is null ? string.Empty : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
