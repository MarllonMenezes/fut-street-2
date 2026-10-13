using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record MorphRelocationProfileInput(
    string NestedBig,
    string MorphObject,
    string LayoutKey);

public sealed record AnimationRelocationProfileInput(
    string BankName,
    bool ElfValid,
    string DeltaHistogram);

public sealed record AnimationMorphRelocationProfileAuditRow(
    string LayoutKeySha256,
    int CatalogRows,
    bool ParseValid,
    int MorphRelocationCount,
    int DistinctMorphGapCount,
    string MorphGapSet,
    string MorphGapHistogram,
    int ContainingAnimationBankCount,
    int ExactGapSetBankCount,
    int ExactGapHistogramBankCount,
    string ContainingAnimationBanks,
    string ExactGapSetBanks,
    string ExactGapHistogramBanks,
    string SampleMembers);

public sealed record AnimationMorphRelocationProfileAuditSummary(
    int MorphCatalogRows,
    int MorphLayouts,
    int ParseValidLayouts,
    int InvalidLayouts,
    int AnimationBanks,
    int LayoutsWithContainingAnimationBank,
    int LayoutsWithExactGapSetBank,
    int ContainingPairs,
    int ExactGapSetPairs,
    int LayoutsWithExactGapHistogramBank,
    int ExactGapHistogramPairs,
    string MorphGapSetSizeDistribution,
    string ContainingBankCountDistribution);

/// <summary>
/// Compares only opaque relocation-gap sets and multiplicity histograms from two
/// already validated structural reports. This is a cross-report recurrence
/// control. It does not
/// compare payload words or assign frame, pose, rig, timing, skinning,
/// material, UV, GPU, command, or animation semantics.
/// </summary>
public static class AnimationMorphRelocationProfileAudit
{
    public static IReadOnlyList<MorphRelocationProfileInput> ReadMorphCatalog(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"Morph structural catalog is empty: {path}");
        }

        var indexes = ReadIndexes(
            lines[0],
            new[] { "nested_big", "morph_object", "morph_structural_layout" },
            "morph structural catalog");
        var rows = new List<MorphRelocationProfileInput>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new MorphRelocationProfileInput(
                ReadField(fields, indexes["nested_big"], lineIndex, path),
                ReadField(fields, indexes["morph_object"], lineIndex, path),
                ReadField(fields, indexes["morph_structural_layout"], lineIndex, path)));
        }

        return rows;
    }

    public static IReadOnlyList<AnimationRelocationProfileInput> ReadAnimationReport(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"Animation relocation report is empty: {path}");
        }

        var indexes = ReadIndexes(
            lines[0],
            new[] { "bank_name", "elf_valid", "delta_histogram" },
            "animation relocation report");
        var rows = new List<AnimationRelocationProfileInput>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new AnimationRelocationProfileInput(
                ReadField(fields, indexes["bank_name"], lineIndex, path),
                ReadBool(fields, indexes["elf_valid"], lineIndex, path),
                ReadField(fields, indexes["delta_histogram"], lineIndex, path)));
        }

        return rows;
    }

    public static IReadOnlyList<AnimationMorphRelocationProfileAuditRow> Audit(
        IEnumerable<MorphRelocationProfileInput> morphRows,
        IEnumerable<AnimationRelocationProfileInput> animationRows)
    {
        ArgumentNullException.ThrowIfNull(morphRows);
        ArgumentNullException.ThrowIfNull(animationRows);

        var banks = animationRows
            .Where(row => row.ElfValid)
            .Select(row => new ParsedAnimationBank(row.BankName, ParseDeltaHistogram(row.DeltaHistogram)))
            .GroupBy(row => row.BankName, StringComparer.Ordinal)
            .Select(group => group.Single())
            .OrderBy(row => row.BankName, StringComparer.Ordinal)
            .ToArray();

        return morphRows
            .GroupBy(row => row.LayoutKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => BuildRow(group.Key, group.ToArray(), banks))
            .ToArray();
    }

    public static AnimationMorphRelocationProfileAuditSummary Summarize(
        IEnumerable<AnimationMorphRelocationProfileAuditRow> rows,
        int animationBankCount)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        return new AnimationMorphRelocationProfileAuditSummary(
            materialized.Sum(row => row.CatalogRows),
            materialized.Length,
            materialized.Count(row => row.ParseValid),
            materialized.Count(row => !row.ParseValid),
            animationBankCount,
            materialized.Count(row => row.ContainingAnimationBankCount > 0),
            materialized.Count(row => row.ExactGapSetBankCount > 0),
            materialized.Sum(row => row.ContainingAnimationBankCount),
            materialized.Sum(row => row.ExactGapSetBankCount),
            materialized.Count(row => row.ExactGapHistogramBankCount > 0),
            materialized.Sum(row => row.ExactGapHistogramBankCount),
            Distribution(materialized.Select(row => row.DistinctMorphGapCount)),
            Distribution(materialized.Select(row => row.ContainingAnimationBankCount)));
    }

    public static void WriteReports(
        string reportPath,
        string summaryPath,
        IEnumerable<AnimationMorphRelocationProfileAuditRow> rows,
        int animationBankCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);

        var materialized = rows.ToArray();
        WriteLines(reportPath, new[]
        {
            "layout_key_sha256",
            "catalog_rows",
            "parse_valid",
            "morph_relocation_count",
            "distinct_morph_gap_count",
            "morph_gap_set",
            "morph_gap_histogram",
            "containing_animation_bank_count",
            "exact_gap_set_bank_count",
            "exact_gap_histogram_bank_count",
            "containing_animation_banks",
            "exact_gap_set_banks",
            "exact_gap_histogram_banks",
            "sample_members",
        }, materialized.Select(ToFields));

        var summary = Summarize(materialized, animationBankCount);
        WriteLines(summaryPath, new[]
        {
            "morph_catalog_rows",
            "morph_layouts",
            "parse_valid_layouts",
            "invalid_layouts",
            "animation_banks",
            "layouts_with_containing_animation_bank",
            "layouts_with_exact_gap_set_bank",
            "containing_pairs",
            "exact_gap_set_pairs",
            "layouts_with_exact_gap_histogram_bank",
            "exact_gap_histogram_pairs",
            "morph_gap_set_size_distribution",
            "containing_bank_count_distribution",
        }, new[] { SummaryFields(summary) });
    }

    private static AnimationMorphRelocationProfileAuditRow BuildRow(
        string layoutKey,
        IReadOnlyList<MorphRelocationProfileInput> members,
        IReadOnlyList<ParsedAnimationBank> banks)
    {
        if (!TryParseMorphRelocationOffsets(layoutKey, out var offsets))
        {
            return new AnimationMorphRelocationProfileAuditRow(
                Sha256(layoutKey),
                members.Count,
                false,
                0,
                0,
                string.Empty,
                string.Empty,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                SampleMembers(members));
        }

        var gaps = offsets
            .Zip(offsets.Skip(1), (first, second) => second - first)
            .Distinct()
            .OrderBy(value => value)
            .ToArray();
        var gapSet = FormatSet(gaps);
        var gapHistogram = offsets
            .Zip(offsets.Skip(1), (first, second) => second - first)
            .GroupBy(value => value)
            .OrderBy(group => group.Key)
            .ToDictionary(group => group.Key, group => group.Count());
        var gapHistogramText = FormatHistogram(gapHistogram);
        var containing = banks
            .Where(bank => gaps.All(gap => bank.Histogram.ContainsKey(gap)))
            .Select(bank => bank.BankName)
            .ToArray();
        var exact = banks
            .Where(bank => bank.Histogram.Keys.ToHashSet().SetEquals(gaps))
            .Select(bank => bank.BankName)
            .ToArray();
        var exactHistogram = banks
            .Where(bank => HistogramsEqual(bank.Histogram, gapHistogram))
            .Select(bank => bank.BankName)
            .ToArray();

        return new AnimationMorphRelocationProfileAuditRow(
            Sha256(layoutKey),
            members.Count,
            true,
            offsets.Length,
            gaps.Length,
            gapSet,
            gapHistogramText,
            containing.Length,
            exact.Length,
            exactHistogram.Length,
            string.Join(",", containing),
            string.Join(",", exact),
            string.Join(",", exactHistogram),
            SampleMembers(members));
    }

    private static bool TryParseMorphRelocationOffsets(string layoutKey, out uint[] offsets)
    {
        offsets = Array.Empty<uint>();
        const string marker = "relocs=";
        var start = layoutKey.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        start += marker.Length;
        var end = layoutKey.IndexOf(';', start);
        var payload = end < 0 ? layoutKey[start..] : layoutKey[start..end];
        if (payload.Length == 0)
        {
            return false;
        }

        var values = new List<uint>();
        foreach (var token in payload.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!token.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                !uint.TryParse(token[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            {
                return false;
            }

            values.Add(value);
        }

        if (values.Count < 2 || values.Zip(values.Skip(1), (first, second) => second >= first).Any(equalOrDescending => !equalOrDescending))
        {
            return false;
        }

        offsets = values.ToArray();
        return true;
    }

    private static Dictionary<uint, int> ParseDeltaHistogram(string histogram)
    {
        var values = new Dictionary<uint, int>();
        if (histogram.Length == 0)
        {
            return values;
        }

        foreach (var item in histogram.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = item.IndexOf('=');
            if (separator <= 2 ||
                !item.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
                !uint.TryParse(item[2..separator], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) ||
                !int.TryParse(item[(separator + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ||
                count <= 0)
            {
                throw new InvalidDataException($"Invalid animation relocation delta histogram token: {item}");
            }

            values[value] = count;
        }

        return values;
    }

    private sealed record ParsedAnimationBank(string BankName, Dictionary<uint, int> Histogram);

    private static string FormatHistogram(IReadOnlyDictionary<uint, int> histogram) =>
        string.Join(",", histogram
            .OrderBy(pair => pair.Key)
            .Select(pair => $"0x{pair.Key:X}={pair.Value.ToString(CultureInfo.InvariantCulture)}"));

    private static bool HistogramsEqual(
        IReadOnlyDictionary<uint, int> first,
        IReadOnlyDictionary<uint, int> second) =>
        first.Count == second.Count &&
        first.All(pair => second.TryGetValue(pair.Key, out var count) && count == pair.Value);

    private static Dictionary<string, int> ReadIndexes(
        string headerLine,
        IEnumerable<string> requiredColumns,
        string reportName)
    {
        var headers = headerLine.Split('\t');
        var indexes = requiredColumns.ToDictionary(
            column => column,
            column => Array.FindIndex(headers, value => string.Equals(value, column, StringComparison.Ordinal)),
            StringComparer.Ordinal);
        var missing = indexes.Where(pair => pair.Value < 0).Select(pair => pair.Key).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException($"{reportName} is missing columns: {string.Join(",", missing)}");
        }

        return indexes;
    }

    private static string ReadField(string[] fields, int index, int lineIndex, string path)
    {
        if (index < 0 || index >= fields.Length)
        {
            throw new InvalidDataException($"TSV row at line {lineIndex + 1} has too few columns: {path}");
        }

        return fields[index];
    }

    private static bool ReadBool(string[] fields, int index, int lineIndex, string path)
    {
        var value = ReadField(fields, index, lineIndex, path);
        if (!bool.TryParse(value, out var parsed))
        {
            throw new InvalidDataException($"Invalid boolean at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }

    private static string[] ToFields(AnimationMorphRelocationProfileAuditRow row) => new[]
    {
        row.LayoutKeySha256,
        row.CatalogRows.ToString(CultureInfo.InvariantCulture),
        row.ParseValid.ToString(),
        row.MorphRelocationCount.ToString(CultureInfo.InvariantCulture),
        row.DistinctMorphGapCount.ToString(CultureInfo.InvariantCulture),
        Escape(row.MorphGapSet),
        Escape(row.MorphGapHistogram),
        row.ContainingAnimationBankCount.ToString(CultureInfo.InvariantCulture),
        row.ExactGapSetBankCount.ToString(CultureInfo.InvariantCulture),
        row.ExactGapHistogramBankCount.ToString(CultureInfo.InvariantCulture),
        Escape(row.ContainingAnimationBanks),
        Escape(row.ExactGapSetBanks),
        Escape(row.ExactGapHistogramBanks),
        Escape(row.SampleMembers),
    };

    private static string[] SummaryFields(AnimationMorphRelocationProfileAuditSummary summary) => new[]
    {
        summary.MorphCatalogRows.ToString(CultureInfo.InvariantCulture),
        summary.MorphLayouts.ToString(CultureInfo.InvariantCulture),
        summary.ParseValidLayouts.ToString(CultureInfo.InvariantCulture),
        summary.InvalidLayouts.ToString(CultureInfo.InvariantCulture),
        summary.AnimationBanks.ToString(CultureInfo.InvariantCulture),
        summary.LayoutsWithContainingAnimationBank.ToString(CultureInfo.InvariantCulture),
        summary.LayoutsWithExactGapSetBank.ToString(CultureInfo.InvariantCulture),
        summary.ContainingPairs.ToString(CultureInfo.InvariantCulture),
        summary.ExactGapSetPairs.ToString(CultureInfo.InvariantCulture),
        summary.LayoutsWithExactGapHistogramBank.ToString(CultureInfo.InvariantCulture),
        summary.ExactGapHistogramPairs.ToString(CultureInfo.InvariantCulture),
        Escape(summary.MorphGapSetSizeDistribution),
        Escape(summary.ContainingBankCountDistribution),
    };

    private static string FormatSet(IEnumerable<uint> values) =>
        string.Join(",", values.Select(value => $"0x{value:X}"));

    private static string Distribution(IEnumerable<int> values) =>
        string.Join(",", values
            .GroupBy(value => value)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key.ToString(CultureInfo.InvariantCulture)}={group.Count().ToString(CultureInfo.InvariantCulture)}"));

    private static string SampleMembers(IEnumerable<MorphRelocationProfileInput> members) =>
        string.Join(",", members.Take(3).Select(member => $"{member.NestedBig}/{member.MorphObject}"));

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Escape(string value) => value
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private static void WriteLines(
        string path,
        IReadOnlyList<string> header,
        IEnumerable<IEnumerable<string>> rows)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string> { string.Join("\t", header) };
        lines.AddRange(rows.Select(row => string.Join("\t", row)));
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
