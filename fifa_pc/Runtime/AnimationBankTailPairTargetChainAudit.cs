using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankTailPairTargetChainDetail(
    string CatalogEntry,
    string BankName,
    int PairOrdinal,
    uint FirstRawWord,
    uint SecondRawWord,
    int FirstTargetRelocationCount,
    int SecondTargetRelocationCount,
    uint FirstTargetRelocationOffset,
    uint SecondTargetRelocationOffset,
    uint FirstSecondLevelRawWord,
    uint SecondSecondLevelRawWord,
    bool FirstSecondLevelWithinData,
    bool SecondSecondLevelWithinData,
    int FirstSecondLevelKnownSymbolCount,
    int SecondSecondLevelKnownSymbolCount,
    string FirstSecondLevelSymbolNames,
    string SecondSecondLevelSymbolNames,
    string FirstSecondLevelTargetPrefixHex,
    string SecondSecondLevelTargetPrefixHex,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

public sealed record AnimationBankTailPairTargetChainObservation(
    string CatalogEntry,
    string BankName,
    int CatalogFileCount,
    bool ElfValid,
    int PairRows,
    int FirstTargetRowsWithOneRelocation,
    int SecondTargetRowsWithOneRelocation,
    int FirstSecondLevelWordsWithinData,
    int SecondSecondLevelWordsWithinData,
    int FirstSecondLevelWordsAtKnownSymbols,
    int SecondSecondLevelWordsAtKnownSymbols,
    int RowsWithCompleteTargetChain,
    int DistinctFirstSecondLevelRawWords,
    int DistinctSecondSecondLevelRawWords,
    string FirstSecondLevelRawWordSha256,
    string SecondSecondLevelRawWordSha256,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

public sealed record AnimationBankTailPairTargetChainSummary(
    int CatalogBanks,
    int ValidatedBanks,
    int CatalogRows,
    int PairRows,
    int FirstTargetRowsWithOneRelocation,
    int SecondTargetRowsWithOneRelocation,
    int FirstSecondLevelWordsWithinData,
    int SecondSecondLevelWordsWithinData,
    int FirstSecondLevelWordsAtKnownSymbols,
    int SecondSecondLevelWordsAtKnownSymbols,
    int RowsWithCompleteTargetChain,
    int BanksWithCompleteTargetChain,
    int DistinctFirstSecondLevelRawWords,
    int DistinctSecondSecondLevelRawWords,
    string Status,
    string CandidateRecordBinding,
    string OriginalSemantics);

/// <summary>
/// Audits a bounded relocation window from each opaque __AnimationBank tail
/// target. Window multiplicity is retained as raw structural evidence; a
/// window with more than one relocation is not reduced to a single hop and is
/// not interpreted as a record, frame, pose, rig, timing or catalog binding.
/// </summary>
public static class AnimationBankTailPairTargetChainAudit
{
    public const string Status = "verified-structural-animation-bank-tail-pair-target-chain";
    public const string CandidateRecordBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";
    private const int TargetWindowBytes = 32;
    private const int PrefixBytes = 32;

    public static IReadOnlyList<AnimationBankTailPairTargetChainDetail> BuildDetails(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> combinedElfData,
        ElfInspection inspection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(inspection);

        if (!TryGetDataAndTail(catalog, combinedElfData, inspection, out var data, out var dataSectionIndex, out var rangeStart, out var rangeEnd))
        {
            return Array.Empty<AnimationBankTailPairTargetChainDetail>();
        }

        var details = new List<AnimationBankTailPairTargetChainDetail>(catalog.Files.Count);
        for (var ordinal = 0; ordinal < catalog.Files.Count; ordinal++)
        {
            var pairStart = checked(rangeStart + (uint)ordinal * 8u);
            var firstRawWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)pairStart), 4));
            var secondRawWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)pairStart + 4), 4));
            var first = InspectTarget(data, inspection, dataSectionIndex, firstRawWord);
            var second = InspectTarget(data, inspection, dataSectionIndex, secondRawWord);
            var complete = first.RelocationCount > 0 && second.RelocationCount > 0;
            details.Add(new AnimationBankTailPairTargetChainDetail(
                catalogEntry,
                catalog.BankName,
                ordinal,
                firstRawWord,
                secondRawWord,
                first.RelocationCount,
                second.RelocationCount,
                first.RelocationOffset,
                second.RelocationOffset,
                first.SecondLevelRawWord,
                second.SecondLevelRawWord,
                first.SecondLevelWithinData,
                second.SecondLevelWithinData,
                first.KnownSymbolCount,
                second.KnownSymbolCount,
                first.SymbolNames,
                second.SymbolNames,
                first.SecondLevelPrefixHex,
                second.SecondLevelPrefixHex,
                complete ? Status : "incomplete-animation-bank-tail-pair-target-chain",
                CandidateRecordBinding,
                OriginalSemantics));
        }

        return details;
    }

    public static AnimationBankTailPairTargetChainObservation Build(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> combinedElfData,
        ElfInspection inspection)
    {
        var details = BuildDetails(catalogEntry, catalog, combinedElfData, inspection);
        if (details.Count != catalog.Files.Count)
        {
            return Invalid(catalogEntry, catalog.BankName, catalog.Files.Count);
        }

        return new AnimationBankTailPairTargetChainObservation(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            true,
            details.Count,
            details.Count(detail => detail.FirstTargetRelocationCount == 1),
            details.Count(detail => detail.SecondTargetRelocationCount == 1),
            details.Count(detail => detail.FirstSecondLevelWithinData),
            details.Count(detail => detail.SecondSecondLevelWithinData),
            details.Count(detail => detail.FirstSecondLevelKnownSymbolCount > 0),
            details.Count(detail => detail.SecondSecondLevelKnownSymbolCount > 0),
            details.Count(detail => detail.Status == Status),
            details.Select(detail => detail.FirstSecondLevelRawWord).Distinct().Count(),
            details.Select(detail => detail.SecondSecondLevelRawWord).Distinct().Count(),
            HashWords(details.Select(detail => detail.FirstSecondLevelRawWord)),
            HashWords(details.Select(detail => detail.SecondSecondLevelRawWord)),
            details.All(detail => detail.Status == Status) ? Status : "incomplete-animation-bank-tail-pair-target-chain",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static AnimationBankTailPairTargetChainObservation Invalid(
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
            0,
            string.Empty,
            string.Empty,
            "invalid-animation-bank-tail-pair-target-chain",
            CandidateRecordBinding,
            OriginalSemantics);

    public static AnimationBankTailPairTargetChainSummary Summarize(
        IEnumerable<AnimationBankTailPairTargetChainObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var rows = observations.ToArray();
        return new AnimationBankTailPairTargetChainSummary(
            rows.Length,
            rows.Count(row => row.ElfValid),
            rows.Sum(row => row.CatalogFileCount),
            rows.Sum(row => row.PairRows),
            rows.Sum(row => row.FirstTargetRowsWithOneRelocation),
            rows.Sum(row => row.SecondTargetRowsWithOneRelocation),
            rows.Sum(row => row.FirstSecondLevelWordsWithinData),
            rows.Sum(row => row.SecondSecondLevelWordsWithinData),
            rows.Sum(row => row.FirstSecondLevelWordsAtKnownSymbols),
            rows.Sum(row => row.SecondSecondLevelWordsAtKnownSymbols),
            rows.Sum(row => row.RowsWithCompleteTargetChain),
            rows.Count(row => row.Status == Status),
            rows.Sum(row => row.DistinctFirstSecondLevelRawWords),
            rows.Sum(row => row.DistinctSecondSecondLevelRawWords),
            rows.Length > 0 && rows.All(row => row.Status == Status)
                ? Status
                : "incomplete-animation-bank-tail-pair-target-chain",
            CandidateRecordBinding,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankTailPairTargetChainObservation> observations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(observations);
        var lines = new List<string>
        {
            "catalog_entry\tbank_name\tcatalog_file_count\telf_valid\tpair_rows\tfirst_target_rows_with_one_relocation\tsecond_target_rows_with_one_relocation\tfirst_second_level_words_within_data\tsecond_second_level_words_within_data\tfirst_second_level_words_at_known_symbols\tsecond_second_level_words_at_known_symbols\trows_with_complete_target_chain\tdistinct_first_second_level_raw_words\tdistinct_second_second_level_raw_words\tfirst_second_level_raw_word_sha256\tsecond_second_level_raw_word_sha256\tstatus\tcandidate_record_binding\toriginal_semantics",
        };
        lines.AddRange(observations.Select(row => string.Join('\t', new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.CatalogFileCount.ToString(CultureInfo.InvariantCulture),
            row.ElfValid.ToString(),
            row.PairRows.ToString(CultureInfo.InvariantCulture),
            row.FirstTargetRowsWithOneRelocation.ToString(CultureInfo.InvariantCulture),
            row.SecondTargetRowsWithOneRelocation.ToString(CultureInfo.InvariantCulture),
            row.FirstSecondLevelWordsWithinData.ToString(CultureInfo.InvariantCulture),
            row.SecondSecondLevelWordsWithinData.ToString(CultureInfo.InvariantCulture),
            row.FirstSecondLevelWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture),
            row.SecondSecondLevelWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture),
            row.RowsWithCompleteTargetChain.ToString(CultureInfo.InvariantCulture),
            row.DistinctFirstSecondLevelRawWords.ToString(CultureInfo.InvariantCulture),
            row.DistinctSecondSecondLevelRawWords.ToString(CultureInfo.InvariantCulture),
            row.FirstSecondLevelRawWordSha256,
            row.SecondSecondLevelRawWordSha256,
            row.Status,
            row.CandidateRecordBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AnimationBankTailPairTargetChainSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string> { "field\tvalue" };
        foreach (var pair in new[]
        {
            ("catalog_banks", summary.CatalogBanks.ToString(CultureInfo.InvariantCulture)),
            ("validated_banks", summary.ValidatedBanks.ToString(CultureInfo.InvariantCulture)),
            ("catalog_rows", summary.CatalogRows.ToString(CultureInfo.InvariantCulture)),
            ("pair_rows", summary.PairRows.ToString(CultureInfo.InvariantCulture)),
            ("first_target_rows_with_one_relocation", summary.FirstTargetRowsWithOneRelocation.ToString(CultureInfo.InvariantCulture)),
            ("second_target_rows_with_one_relocation", summary.SecondTargetRowsWithOneRelocation.ToString(CultureInfo.InvariantCulture)),
            ("first_second_level_words_within_data", summary.FirstSecondLevelWordsWithinData.ToString(CultureInfo.InvariantCulture)),
            ("second_second_level_words_within_data", summary.SecondSecondLevelWordsWithinData.ToString(CultureInfo.InvariantCulture)),
            ("first_second_level_words_at_known_symbols", summary.FirstSecondLevelWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture)),
            ("second_second_level_words_at_known_symbols", summary.SecondSecondLevelWordsAtKnownSymbols.ToString(CultureInfo.InvariantCulture)),
            ("rows_with_complete_target_chain", summary.RowsWithCompleteTargetChain.ToString(CultureInfo.InvariantCulture)),
            ("banks_with_complete_target_chain", summary.BanksWithCompleteTargetChain.ToString(CultureInfo.InvariantCulture)),
            ("distinct_first_second_level_raw_words", summary.DistinctFirstSecondLevelRawWords.ToString(CultureInfo.InvariantCulture)),
            ("distinct_second_second_level_raw_words", summary.DistinctSecondSecondLevelRawWords.ToString(CultureInfo.InvariantCulture)),
            ("status", summary.Status),
            ("candidate_record_binding", summary.CandidateRecordBinding),
            ("original_semantics", summary.OriginalSemantics),
        })
        {
            lines.Add($"{pair.Item1}\t{pair.Item2}");
        }
        WriteLines(path, lines);
    }

    public static void WriteDetails(
        string path,
        IEnumerable<AnimationBankTailPairTargetChainDetail> details)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(details);
        var lines = new List<string>
        {
            "catalog_entry\tbank_name\tpair_ordinal\tfirst_raw_word\tsecond_raw_word\tfirst_target_relocation_count\tsecond_target_relocation_count\tfirst_target_relocation_offset\tsecond_target_relocation_offset\tfirst_second_level_raw_word\tsecond_second_level_raw_word\tfirst_second_level_within_data\tsecond_second_level_within_data\tfirst_second_level_known_symbol_count\tsecond_second_level_known_symbol_count\tfirst_second_level_symbol_names\tsecond_second_level_symbol_names\tfirst_second_level_target_prefix_hex\tsecond_second_level_target_prefix_hex\tstatus\tcandidate_record_binding\toriginal_semantics",
        };
        lines.AddRange(details.Select(detail => string.Join('\t', new[]
        {
            Tsv(detail.CatalogEntry),
            Tsv(detail.BankName),
            detail.PairOrdinal.ToString(CultureInfo.InvariantCulture),
            $"0x{detail.FirstRawWord:X8}",
            $"0x{detail.SecondRawWord:X8}",
            detail.FirstTargetRelocationCount.ToString(CultureInfo.InvariantCulture),
            detail.SecondTargetRelocationCount.ToString(CultureInfo.InvariantCulture),
            $"0x{detail.FirstTargetRelocationOffset:X}",
            $"0x{detail.SecondTargetRelocationOffset:X}",
            $"0x{detail.FirstSecondLevelRawWord:X8}",
            $"0x{detail.SecondSecondLevelRawWord:X8}",
            detail.FirstSecondLevelWithinData.ToString(),
            detail.SecondSecondLevelWithinData.ToString(),
            detail.FirstSecondLevelKnownSymbolCount.ToString(CultureInfo.InvariantCulture),
            detail.SecondSecondLevelKnownSymbolCount.ToString(CultureInfo.InvariantCulture),
            Tsv(detail.FirstSecondLevelSymbolNames),
            Tsv(detail.SecondSecondLevelSymbolNames),
            detail.FirstSecondLevelTargetPrefixHex,
            detail.SecondSecondLevelTargetPrefixHex,
            detail.Status,
            detail.CandidateRecordBinding,
            detail.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    private static TargetInspection InspectTarget(
        ReadOnlySpan<byte> data,
        ElfInspection inspection,
        int dataSectionIndex,
        uint rawWord)
    {
        var within = (ulong)rawWord + 4u <= (ulong)data.Length;
        if (!within)
        {
            return new TargetInspection(0, 0, 0, false, 0, string.Empty, string.Empty);
        }

        var start = (ulong)rawWord;
        var end = Math.Min((ulong)data.Length, start + TargetWindowBytes);
        var relocations = inspection.Relocations
            .Where(relocation => relocation.TargetSectionIndex == dataSectionIndex &&
                                 (ulong)relocation.Offset >= start &&
                                 (ulong)relocation.Offset < end)
            .OrderBy(relocation => relocation.Offset)
            .ToArray();
        if (relocations.Length != 1)
        {
            return new TargetInspection(relocations.Length, 0, 0, false, 0, string.Empty, PrefixHex(data, rawWord));
        }

        var relocationOffset = relocations[0].Offset;
        var secondLevelRawWord = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)relocationOffset), 4));
        var secondLevelWithin = (ulong)secondLevelRawWord + 4u <= (ulong)data.Length;
        var symbols = inspection.Symbols
            .Where(symbol => symbol.Value == secondLevelRawWord)
            .Select(symbol => symbol.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        return new TargetInspection(
            relocations.Length,
            relocationOffset,
            secondLevelRawWord,
            secondLevelWithin,
            symbols.Length,
            string.Join(",", symbols),
            PrefixHex(data, secondLevelRawWord));
    }

    private static bool TryGetDataAndTail(
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> combinedElfData,
        ElfInspection inspection,
        out ReadOnlySpan<byte> data,
        out int dataSectionIndex,
        out uint rangeStart,
        out uint rangeEnd)
    {
        data = default;
        dataSectionIndex = -1;
        rangeStart = 0;
        rangeEnd = 0;
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
            return false;
        }

        data = combinedElfData.Slice(checked((int)dataSection.Offset), checked((int)dataSection.Size));
        var tail = data.Slice(checked((int)symbol.Value));
        if (tail.Length != 32)
        {
            return false;
        }

        rangeStart = BinaryPrimitives.ReadUInt32LittleEndian(tail[16..]);
        rangeEnd = BinaryPrimitives.ReadUInt32LittleEndian(tail[20..]);
        return rangeStart <= rangeEnd &&
               rangeEnd <= data.Length &&
               rangeEnd - rangeStart == (uint)catalog.Files.Count * 8u;
    }

    private static string PrefixHex(ReadOnlySpan<byte> data, uint offset)
    {
        if (offset > data.Length)
        {
            return string.Empty;
        }

        var length = Math.Min(PrefixBytes, data.Length - checked((int)offset));
        return Convert.ToHexString(data.Slice(checked((int)offset), length));
    }

    private static string HashWords(IEnumerable<uint> words)
    {
        var bytes = words.SelectMany(word => BitConverter.GetBytes(word)).ToArray();
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    private static string Tsv(string value) => value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private sealed record TargetInspection(
        int RelocationCount,
        uint RelocationOffset,
        uint SecondLevelRawWord,
        bool SecondLevelWithinData,
        int KnownSymbolCount,
        string SymbolNames,
        string SecondLevelPrefixHex);
}
