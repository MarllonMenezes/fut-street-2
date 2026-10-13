using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankCatalogTailRangeObservation(
    string CatalogEntry,
    string BankName,
    int CatalogFileCount,
    bool ElfValid,
    uint DataSize,
    uint AnimationBankSymbolValue,
    uint AnimationBankSymbolSize,
    int TailByteCount,
    uint TailHeaderSize,
    uint TailEntryCount,
    uint TailRangeStart,
    uint TailRangeEnd,
    uint TailRangeByteCount,
    uint ExpectedCatalogStrideBytes,
    bool TailHeaderShapeValid,
    bool TailEntryCountMatchesCatalogDouble,
    bool TailRangeWithinData,
    bool TailRangeAligned,
    bool TailRangeLengthMatchesCatalogStride,
    int RelocationsInTailRange,
    bool TailRangeRelocationCountMatchesCatalogDouble,
    string TailRangeSha256,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

public sealed record AnimationBankCatalogTailRangeSummary(
    int CatalogBanks,
    int ValidatedBanks,
    int CatalogRows,
    int TailHeaderShapeValidRows,
    int TailEntryCountMatchRows,
    int TailRangeWithinDataRows,
    int TailRangeAlignedRows,
    int TailRangeLengthMatchRows,
    int TailRangeRelocationCountMatchRows,
    int RowsWithNoRelocationsInTailRange,
    int VerifiedStructuralRows,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

/// <summary>
/// Audits the fixed-size bytes immediately after the __AnimationBank symbol.
/// The observed count/range arithmetic is retained as structural evidence only:
/// the range is not interpreted as an animation record table and no frame,
/// pose, rig, timing, skinning, morph or playback binding is assigned.
/// </summary>
public static class AnimationBankCatalogTailRangeAudit
{
    public const string Status = "verified-structural-animation-bank-tail-range";
    public const string CandidateRecordBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static AnimationBankCatalogTailRangeObservation Build(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> combinedElfData,
        ElfInspection inspection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogEntry);
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

        var symbol = inspection.Symbols.FirstOrDefault(candidate =>
            candidate.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal));
        if (dataSection is null || symbol is null ||
            (ulong)dataSection.Offset + dataSection.Size > (ulong)combinedElfData.Length ||
            symbol.Value > dataSection.Size ||
            symbol.Size > dataSection.Size - symbol.Value)
        {
            return CreateInvalid(catalogEntry, catalog.BankName, catalog.Files.Count);
        }

        var data = combinedElfData
            .Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size));
        var tail = data.Slice(checked((int)symbol.Value));
        var enoughTail = tail.Length >= 32;
        var tailHeaderSize = enoughTail ? BinaryPrimitives.ReadUInt32LittleEndian(tail) : 0;
        var tailEntryCount = enoughTail
            ? BinaryPrimitives.ReadUInt32LittleEndian(tail[4..])
            : 0;
        var tailRangeStart = enoughTail
            ? BinaryPrimitives.ReadUInt32LittleEndian(tail[16..])
            : 0;
        var tailRangeEnd = enoughTail
            ? BinaryPrimitives.ReadUInt32LittleEndian(tail[20..])
            : 0;
        var expectedCatalogStrideBytes = checked((uint)catalog.Files.Count * 8u);
        var tailRangeByteCount = tailRangeEnd >= tailRangeStart
            ? tailRangeEnd - tailRangeStart
            : 0;
        var tailHeaderShapeValid = enoughTail &&
            tailHeaderSize == 28 &&
            tail.Length == 32 &&
            IsZero(tail[8..16]) &&
            IsZero(tail[24..32]);
        var tailEntryCountMatchesCatalogDouble = enoughTail &&
            tailEntryCount == checked((uint)catalog.Files.Count * 2u);
        var tailRangeWithinData = enoughTail &&
            tailRangeStart <= tailRangeEnd &&
            tailRangeEnd <= dataSection.Size;
        var tailRangeAligned = enoughTail &&
            (tailRangeStart & 3u) == 0 &&
            (tailRangeEnd & 3u) == 0;
        var tailRangeLengthMatchesCatalogStride = enoughTail &&
            tailRangeByteCount == expectedCatalogStrideBytes;
        var relocationsInTailRange = inspection.Relocations.Count(relocation =>
            relocation.TargetSectionIndex == dataSectionIndex &&
            relocation.Offset >= tailRangeStart &&
            relocation.Offset < tailRangeEnd);
        var tailRangeRelocationCountMatchesCatalogDouble = enoughTail &&
            relocationsInTailRange == tailEntryCount;
        var rangeHash = tailRangeWithinData
            ? Convert.ToHexString(SHA256.HashData(
                data.Slice(checked((int)tailRangeStart), checked((int)tailRangeByteCount))))
            : string.Empty;
        var verified = tailHeaderShapeValid &&
            tailEntryCountMatchesCatalogDouble &&
            tailRangeWithinData &&
            tailRangeAligned &&
            tailRangeLengthMatchesCatalogStride &&
            tailRangeRelocationCountMatchesCatalogDouble;

        return new AnimationBankCatalogTailRangeObservation(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            true,
            dataSection.Size,
            symbol.Value,
            symbol.Size,
            tail.Length,
            tailHeaderSize,
            tailEntryCount,
            tailRangeStart,
            tailRangeEnd,
            tailRangeByteCount,
            expectedCatalogStrideBytes,
            tailHeaderShapeValid,
            tailEntryCountMatchesCatalogDouble,
            tailRangeWithinData,
            tailRangeAligned,
            tailRangeLengthMatchesCatalogStride,
            relocationsInTailRange,
            tailRangeRelocationCountMatchesCatalogDouble,
            rangeHash,
            verified ? Status : "incomplete-animation-bank-tail-range",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static AnimationBankCatalogTailRangeObservation CreateInvalid(
        string catalogEntry,
        string bankName,
        int catalogFileCount)
    {
        return new AnimationBankCatalogTailRangeObservation(
            catalogEntry,
            bankName,
            catalogFileCount,
            false,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            checked((uint)catalogFileCount * 8u),
            false,
            false,
            false,
            false,
            false,
            0,
            false,
            string.Empty,
            "invalid-animation-bank-tail-range",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static AnimationBankCatalogTailRangeSummary Summarize(
        IEnumerable<AnimationBankCatalogTailRangeObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var rows = observations.ToArray();
        return new AnimationBankCatalogTailRangeSummary(
            rows.Length,
            rows.Count(row => row.ElfValid),
            rows.Sum(row => row.CatalogFileCount),
            rows.Count(row => row.TailHeaderShapeValid),
            rows.Count(row => row.TailEntryCountMatchesCatalogDouble),
            rows.Count(row => row.TailRangeWithinData),
            rows.Count(row => row.TailRangeAligned),
            rows.Count(row => row.TailRangeLengthMatchesCatalogStride),
            rows.Count(row => row.TailRangeRelocationCountMatchesCatalogDouble),
            rows.Count(row => row.ElfValid && row.RelocationsInTailRange == 0),
            rows.Count(row => row.Status == Status),
            rows.Length > 0 && rows.All(row => row.Status == Status)
                ? Status
                : "incomplete-animation-bank-tail-range",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankCatalogTailRangeObservation> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(observations);
        var lines = new List<string>
        {
            string.Join('\t', new[]
            {
                "catalog_entry",
                "bank_name",
                "catalog_file_count",
                "elf_valid",
                "data_size",
                "animation_bank_symbol_value",
                "animation_bank_symbol_size",
                "tail_byte_count",
                "tail_header_size",
                "tail_entry_count",
                "tail_range_start",
                "tail_range_end",
                "tail_range_byte_count",
                "expected_catalog_stride_bytes",
                "tail_header_shape_valid",
                "tail_entry_count_matches_catalog_double",
                "tail_range_within_data",
                "tail_range_aligned",
                "tail_range_length_matches_catalog_stride",
                "relocations_in_tail_range",
                "tail_range_relocation_count_matches_catalog_double",
                "tail_range_sha256",
                "status",
                "candidate_record_binding",
                "original_semantics",
            }),
        };
        lines.AddRange(observations.Select(row => string.Join('\t', new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            row.ElfValid.ToString(),
            row.DataSize.ToString(CultureInfo.InvariantCulture),
            $"0x{row.AnimationBankSymbolValue:X}",
            row.AnimationBankSymbolSize.ToString(CultureInfo.InvariantCulture),
            row.TailByteCount.ToString(CultureInfo.InvariantCulture),
            row.TailHeaderSize.ToString(CultureInfo.InvariantCulture),
            row.TailEntryCount.ToString(CultureInfo.InvariantCulture),
            $"0x{row.TailRangeStart:X}",
            $"0x{row.TailRangeEnd:X}",
            row.TailRangeByteCount.ToString(CultureInfo.InvariantCulture),
            row.ExpectedCatalogStrideBytes.ToString(CultureInfo.InvariantCulture),
            row.TailHeaderShapeValid.ToString(),
            row.TailEntryCountMatchesCatalogDouble.ToString(),
            row.TailRangeWithinData.ToString(),
            row.TailRangeAligned.ToString(),
            row.TailRangeLengthMatchesCatalogStride.ToString(),
            row.RelocationsInTailRange.ToString(CultureInfo.InvariantCulture),
            row.TailRangeRelocationCountMatchesCatalogDouble.ToString(),
            row.TailRangeSha256,
            row.Status,
            row.CandidateRecordBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AnimationBankCatalogTailRangeSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new[]
        {
            "field\tvalue",
            $"catalog_banks\t{summary.CatalogBanks.ToString(CultureInfo.InvariantCulture)}",
            $"validated_banks\t{summary.ValidatedBanks.ToString(CultureInfo.InvariantCulture)}",
            $"catalog_rows\t{summary.CatalogRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_header_shape_valid_rows\t{summary.TailHeaderShapeValidRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_entry_count_match_rows\t{summary.TailEntryCountMatchRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_range_within_data_rows\t{summary.TailRangeWithinDataRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_range_aligned_rows\t{summary.TailRangeAlignedRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_range_length_match_rows\t{summary.TailRangeLengthMatchRows.ToString(CultureInfo.InvariantCulture)}",
            $"tail_range_relocation_count_match_rows\t{summary.TailRangeRelocationCountMatchRows.ToString(CultureInfo.InvariantCulture)}",
            $"rows_with_no_relocations_in_tail_range\t{summary.RowsWithNoRelocationsInTailRange.ToString(CultureInfo.InvariantCulture)}",
            $"verified_structural_rows\t{summary.VerifiedStructuralRows.ToString(CultureInfo.InvariantCulture)}",
            $"status\t{summary.Status}",
            $"candidate_record_binding\t{summary.CandidateRecordBinding}",
            $"original_semantics\t{summary.OriginalSemantics}",
        };
        WriteLines(path, lines);
    }

    private static bool IsZero(ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            if (value != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static string Tsv(string value) => value
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
