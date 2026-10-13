using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankTailPairRelocationObservation(
    string CatalogEntry,
    string BankName,
    int CatalogFileCount,
    bool ElfValid,
    int PairOrdinalRows,
    int PairRowsWithTwoRelocations,
    int PairRowsWithExpectedOffsets,
    int RowsWithUnexpectedRelocationMultiplicity,
    int FirstRawWordsWithinData,
    int SecondRawWordsWithinData,
    int FirstRawWordsAtKnownSymbols,
    int SecondRawWordsAtKnownSymbols,
    int DistinctRawWordPairs,
    string RawWordPairSha256,
    string FirstPairRawTargetSignature,
    string LastPairRawTargetSignature,
    int DistinctPairSymbolSignatures,
    string PairSymbolSignatureSha256,
    string FirstPairSymbolSignature,
    string LastPairSymbolSignature,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

public sealed record AnimationBankTailPairRelocationSummary(
    int CatalogBanks,
    int ValidatedBanks,
    int CatalogRows,
    int PairOrdinalRows,
    int PairRowsWithTwoRelocations,
    int PairRowsWithExpectedOffsets,
    int RowsWithUnexpectedRelocationMultiplicity,
    int FirstRawWordsWithinData,
    int SecondRawWordsWithinData,
    int FirstRawWordsAtKnownSymbols,
    int SecondRawWordsAtKnownSymbols,
    int DistinctRawWordPairs,
    int BanksWithCompletePairLayout,
    int DistinctPairSymbolSignatures,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

public sealed record AnimationBankTailPairRelocationDetail(
    string CatalogEntry,
    string BankName,
    int PairOrdinal,
    uint FirstRelocationOffset,
    uint SecondRelocationOffset,
    uint FirstRawWord,
    uint SecondRawWord,
    bool FirstTargetWithinData,
    bool SecondTargetWithinData,
    string FirstTargetPrefixHex,
    string SecondTargetPrefixHex,
    int FirstTargetRelocationCount,
    int SecondTargetRelocationCount,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

/// <summary>
/// Reconstructs the two-relocation shape observed at each ordinal inside the
/// __AnimationBank tail range. Symbol names and raw words are retained as
/// opaque structural signatures. The ordinal is not joined to the catalog's
/// file entries and no frame, pose, rig, timing, skinning, morph or playback
/// meaning is assigned.
/// </summary>
public static class AnimationBankTailPairRelocationAudit
{
    public const string Status = "verified-structural-animation-bank-tail-pair-relocations";
    public const string CandidateRecordBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static IReadOnlyList<AnimationBankTailPairRelocationDetail> BuildDetails(
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
            return Array.Empty<AnimationBankTailPairRelocationDetail>();
        }

        var data = combinedElfData
            .Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size));
        var tail = data.Slice(checked((int)symbol.Value));
        if (tail.Length != 32)
        {
            return Array.Empty<AnimationBankTailPairRelocationDetail>();
        }

        var rangeStart = BinaryPrimitives.ReadUInt32LittleEndian(tail[16..]);
        var rangeEnd = BinaryPrimitives.ReadUInt32LittleEndian(tail[20..]);
        if (rangeStart > rangeEnd || rangeEnd > data.Length || rangeEnd - rangeStart != (uint)catalog.Files.Count * 8u)
        {
            return Array.Empty<AnimationBankTailPairRelocationDetail>();
        }

        var relocations = inspection.Relocations
            .Where(relocation =>
                relocation.TargetSectionIndex == dataSectionIndex &&
                relocation.Offset >= rangeStart &&
                relocation.Offset < rangeEnd)
            .ToArray();
        var details = new List<AnimationBankTailPairRelocationDetail>(catalog.Files.Count);
        for (var ordinal = 0; ordinal < catalog.Files.Count; ordinal++)
        {
            var firstOffset = checked(rangeStart + (uint)ordinal * 8u);
            var secondOffset = checked(firstOffset + 4u);
            var firstWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)firstOffset), 4));
            var secondWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)secondOffset), 4));
            var firstWithin = firstWord + 4u <= data.Length;
            var secondWithin = secondWord + 4u <= data.Length;
            var firstCount = relocations.Count(relocation => relocation.Offset == firstOffset);
            var secondCount = relocations.Count(relocation => relocation.Offset == secondOffset);
            details.Add(new AnimationBankTailPairRelocationDetail(
                catalogEntry,
                catalog.BankName,
                ordinal,
                firstOffset,
                secondOffset,
                firstWord,
                secondWord,
                firstWithin,
                secondWithin,
                PrefixHex(data, firstWord),
                PrefixHex(data, secondWord),
                firstCount,
                secondCount,
                firstCount == 1 && secondCount == 1 && firstWithin && secondWithin
                    ? Status
                    : "incomplete-animation-bank-tail-pair-relocation-detail",
                CandidateRecordBinding,
                OriginalSemantics));
        }

        return details;
    }

    public static void WriteDetails(
        string path,
        IEnumerable<AnimationBankTailPairRelocationDetail> details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(details);
        var lines = new List<string>
        {
            "catalog_entry\tbank_name\tpair_ordinal\tfirst_relocation_offset\tsecond_relocation_offset\tfirst_raw_word\tsecond_raw_word\tfirst_target_within_data\tsecond_target_within_data\tfirst_target_prefix_hex\tsecond_target_prefix_hex\tfirst_target_relocation_count\tsecond_target_relocation_count\tstatus\tcandidate_record_binding\toriginal_semantics",
        };
        lines.AddRange(details.Select(detail => string.Join('\t', new[]
        {
            Tsv(detail.CatalogEntry),
            Tsv(detail.BankName),
            detail.PairOrdinal.ToString(CultureInfo.InvariantCulture),
            $"0x{detail.FirstRelocationOffset:X}",
            $"0x{detail.SecondRelocationOffset:X}",
            $"0x{detail.FirstRawWord:X8}",
            $"0x{detail.SecondRawWord:X8}",
            detail.FirstTargetWithinData.ToString(),
            detail.SecondTargetWithinData.ToString(),
            detail.FirstTargetPrefixHex,
            detail.SecondTargetPrefixHex,
            detail.FirstTargetRelocationCount.ToString(CultureInfo.InvariantCulture),
            detail.SecondTargetRelocationCount.ToString(CultureInfo.InvariantCulture),
            detail.Status,
            detail.CandidateRecordBinding,
            detail.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    public static AnimationBankTailPairRelocationObservation Build(
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
            return Invalid(catalogEntry, catalog.BankName, catalog.Files.Count);
        }

        var data = combinedElfData
            .Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size));
        var tail = data.Slice(checked((int)symbol.Value));
        if (tail.Length != 32)
        {
            return Invalid(catalogEntry, catalog.BankName, catalog.Files.Count);
        }

        var tailRangeStart = BinaryPrimitives.ReadUInt32LittleEndian(tail[16..]);
        var tailRangeEnd = BinaryPrimitives.ReadUInt32LittleEndian(tail[20..]);
        var expectedBytes = checked((uint)catalog.Files.Count * 8u);
        if (tailRangeStart > tailRangeEnd ||
            tailRangeEnd > dataSection.Size ||
            tailRangeEnd - tailRangeStart != expectedBytes ||
            (tailRangeStart & 3u) != 0 ||
            (tailRangeEnd & 3u) != 0)
        {
            return Invalid(catalogEntry, catalog.BankName, catalog.Files.Count);
        }

        var relocations = inspection.Relocations
            .Where(relocation =>
                relocation.TargetSectionIndex == dataSectionIndex &&
                relocation.Offset >= tailRangeStart &&
                relocation.Offset < tailRangeEnd)
            .GroupBy(relocation => relocation.Offset)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var signatures = new List<string>(catalog.Files.Count);
        var pairRowsWithTwoRelocations = 0;
        var pairRowsWithExpectedOffsets = 0;
        var unexpectedMultiplicity = 0;
        var firstRawWordsWithinData = 0;
        var secondRawWordsWithinData = 0;
        var firstRawWordsAtKnownSymbols = 0;
        var secondRawWordsAtKnownSymbols = 0;
        var rawWordPairs = new List<string>(catalog.Files.Count);
        var firstRawTargetSignature = string.Empty;
        var lastRawTargetSignature = string.Empty;
        string firstSignature = string.Empty;
        string lastSignature = string.Empty;
        for (var ordinal = 0; ordinal < catalog.Files.Count; ordinal++)
        {
            var pairStart = checked(tailRangeStart + (uint)ordinal * 8u);
            var pairSecond = checked(pairStart + 4u);
            var first = relocations.TryGetValue(pairStart, out var firstRelocations)
                ? firstRelocations
                : Array.Empty<ElfRelocation>();
            var second = relocations.TryGetValue(pairSecond, out var secondRelocations)
                ? secondRelocations
                : Array.Empty<ElfRelocation>();
            var pairRelocationCount = first.Length + second.Length;
            if (pairRelocationCount == 2)
            {
                pairRowsWithTwoRelocations++;
            }

            var unexpected = inspection.Relocations.Count(relocation =>
                relocation.TargetSectionIndex == dataSectionIndex &&
                relocation.Offset >= pairStart &&
                relocation.Offset < pairStart + 8u) != 2;
            if (unexpected)
            {
                unexpectedMultiplicity++;
            }

            var expected = first.Length == 1 && second.Length == 1 && !unexpected;
            if (!expected)
            {
                continue;
            }

            pairRowsWithExpectedOffsets++;
            var firstWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)pairStart), 4));
            var secondWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)pairSecond), 4));
            var firstWithinData = firstWord + 4u <= data.Length;
            var secondWithinData = secondWord + 4u <= data.Length;
            firstRawWordsWithinData += firstWithinData ? 1 : 0;
            secondRawWordsWithinData += secondWithinData ? 1 : 0;
            var firstTargetSymbols = inspection.Symbols
                .Where(candidate => candidate.Value == firstWord)
                .Select(candidate => candidate.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var secondTargetSymbols = inspection.Symbols
                .Where(candidate => candidate.Value == secondWord)
                .Select(candidate => candidate.Name)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            firstRawWordsAtKnownSymbols += firstTargetSymbols.Length > 0 ? 1 : 0;
            secondRawWordsAtKnownSymbols += secondTargetSymbols.Length > 0 ? 1 : 0;
            var rawTargetSignature = string.Join(
                "|",
                $"0x{firstWord:X8}",
                string.Join(",", firstTargetSymbols),
                $"0x{secondWord:X8}",
                string.Join(",", secondTargetSymbols));
            rawWordPairs.Add(rawTargetSignature);
            firstRawTargetSignature = firstRawTargetSignature.Length == 0
                ? rawTargetSignature
                : firstRawTargetSignature;
            lastRawTargetSignature = rawTargetSignature;
            var signature = string.Join(
                "|",
                ordinal.ToString(CultureInfo.InvariantCulture),
                first[0].SymbolName,
                second[0].SymbolName,
                $"0x{firstWord:X8}",
                $"0x{secondWord:X8}");
            signatures.Add(signature);
            firstSignature = firstSignature.Length == 0 ? signature : firstSignature;
            lastSignature = signature;
        }

        var signatureText = string.Join("\n", signatures);
        var valid = pairRowsWithTwoRelocations == catalog.Files.Count &&
                    pairRowsWithExpectedOffsets == catalog.Files.Count &&
                    unexpectedMultiplicity == 0;
        return new AnimationBankTailPairRelocationObservation(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            true,
            catalog.Files.Count,
            pairRowsWithTwoRelocations,
            pairRowsWithExpectedOffsets,
            unexpectedMultiplicity,
            firstRawWordsWithinData,
            secondRawWordsWithinData,
            firstRawWordsAtKnownSymbols,
            secondRawWordsAtKnownSymbols,
            rawWordPairs.Distinct(StringComparer.Ordinal).Count(),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", rawWordPairs)))),
            firstRawTargetSignature,
            lastRawTargetSignature,
            signatures.Select(signature => signature.Split('|', 4)[1] + "|" + signature.Split('|', 4)[2])
                .Distinct(StringComparer.Ordinal)
                .Count(),
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(signatureText))),
            firstSignature,
            lastSignature,
            valid ? Status : "incomplete-animation-bank-tail-pair-relocations",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static AnimationBankTailPairRelocationObservation Invalid(
        string catalogEntry,
        string bankName,
        int catalogFileCount) =>
        new(
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
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            "invalid-animation-bank-tail-pair-relocations",
            CandidateRecordBinding,
            OriginalSemantics);

    public static AnimationBankTailPairRelocationSummary Summarize(
        IEnumerable<AnimationBankTailPairRelocationObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var rows = observations.ToArray();
        return new AnimationBankTailPairRelocationSummary(
            rows.Length,
            rows.Count(row => row.ElfValid),
            rows.Sum(row => row.CatalogFileCount),
            rows.Sum(row => row.PairOrdinalRows),
            rows.Sum(row => row.PairRowsWithTwoRelocations),
            rows.Sum(row => row.PairRowsWithExpectedOffsets),
            rows.Sum(row => row.RowsWithUnexpectedRelocationMultiplicity),
            rows.Sum(row => row.FirstRawWordsWithinData),
            rows.Sum(row => row.SecondRawWordsWithinData),
            rows.Sum(row => row.FirstRawWordsAtKnownSymbols),
            rows.Sum(row => row.SecondRawWordsAtKnownSymbols),
            rows.Sum(row => row.DistinctRawWordPairs),
            rows.Count(row => row.Status == Status),
            rows.Sum(row => row.DistinctPairSymbolSignatures),
            rows.Length > 0 && rows.All(row => row.Status == Status)
                ? Status
                : "incomplete-animation-bank-tail-pair-relocations",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankTailPairRelocationObservation> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(observations);
        var lines = new List<string>
        {
            "catalog_entry\tbank_name\tcatalog_file_count\telf_valid\tpair_ordinal_rows\tpair_rows_with_two_relocations\tpair_rows_with_expected_offsets\trows_with_unexpected_relocation_multiplicity\tfirst_raw_words_within_data\tsecond_raw_words_within_data\tfirst_raw_words_at_known_symbols\tsecond_raw_words_at_known_symbols\tdistinct_raw_word_pairs\traw_word_pair_sha256\tfirst_pair_raw_target_signature\tlast_pair_raw_target_signature\tdistinct_pair_symbol_signatures\tpair_symbol_signature_sha256\tfirst_pair_symbol_signature\tlast_pair_symbol_signature\tstatus\tcandidate_record_binding\toriginal_semantics",
        };
        lines.AddRange(observations.Select(row => string.Join('\t', new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            row.ElfValid.ToString(),
            row.PairOrdinalRows.ToString(CultureInfo.InvariantCulture),
            row.PairRowsWithTwoRelocations.ToString(CultureInfo.InvariantCulture),
            row.PairRowsWithExpectedOffsets.ToString(CultureInfo.InvariantCulture),
            row.RowsWithUnexpectedRelocationMultiplicity.ToString(CultureInfo.InvariantCulture),
            row.FirstRawWordsWithinData.ToString(CultureInfo.InvariantCulture),
            row.SecondRawWordsWithinData.ToString(CultureInfo.InvariantCulture),
            row.FirstRawWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture),
            row.SecondRawWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture),
            row.DistinctRawWordPairs.ToString(CultureInfo.InvariantCulture),
            row.RawWordPairSha256,
            Tsv(row.FirstPairRawTargetSignature),
            Tsv(row.LastPairRawTargetSignature),
            row.DistinctPairSymbolSignatures.ToString(CultureInfo.InvariantCulture),
            row.PairSymbolSignatureSha256,
            Tsv(row.FirstPairSymbolSignature),
            Tsv(row.LastPairSymbolSignature),
            row.Status,
            row.CandidateRecordBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AnimationBankTailPairRelocationSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        WriteLines(path, new[]
        {
            "field\tvalue",
            $"catalog_banks\t{summary.CatalogBanks.ToString(CultureInfo.InvariantCulture)}",
            $"validated_banks\t{summary.ValidatedBanks.ToString(CultureInfo.InvariantCulture)}",
            $"catalog_rows\t{summary.CatalogRows.ToString(CultureInfo.InvariantCulture)}",
            $"pair_ordinal_rows\t{summary.PairOrdinalRows.ToString(CultureInfo.InvariantCulture)}",
            $"pair_rows_with_two_relocations\t{summary.PairRowsWithTwoRelocations.ToString(CultureInfo.InvariantCulture)}",
            $"pair_rows_with_expected_offsets\t{summary.PairRowsWithExpectedOffsets.ToString(CultureInfo.InvariantCulture)}",
            $"rows_with_unexpected_relocation_multiplicity\t{summary.RowsWithUnexpectedRelocationMultiplicity.ToString(CultureInfo.InvariantCulture)}",
            $"first_raw_words_within_data\t{summary.FirstRawWordsWithinData.ToString(CultureInfo.InvariantCulture)}",
            $"second_raw_words_within_data\t{summary.SecondRawWordsWithinData.ToString(CultureInfo.InvariantCulture)}",
            $"first_raw_words_at_known_symbols\t{summary.FirstRawWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture)}",
            $"second_raw_words_at_known_symbols\t{summary.SecondRawWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture)}",
            $"distinct_raw_word_pairs\t{summary.DistinctRawWordPairs.ToString(CultureInfo.InvariantCulture)}",
            $"banks_with_complete_pair_layout\t{summary.BanksWithCompletePairLayout.ToString(CultureInfo.InvariantCulture)}",
            $"distinct_pair_symbol_signatures\t{summary.DistinctPairSymbolSignatures.ToString(CultureInfo.InvariantCulture)}",
            $"status\t{summary.Status}",
            $"candidate_record_binding\t{summary.CandidateRecordBinding}",
            $"original_semantics\t{summary.OriginalSemantics}",
        });
    }

    private static string Tsv(string value) => value
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private static string PrefixHex(ReadOnlySpan<byte> data, uint offset)
    {
        if (offset >= data.Length)
        {
            return string.Empty;
        }

        var length = Math.Min(32, data.Length - checked((int)offset));
        return Convert.ToHexString(data.Slice(checked((int)offset), length));
    }

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
