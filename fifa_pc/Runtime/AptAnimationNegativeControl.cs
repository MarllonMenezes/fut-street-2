using System.Globalization;

namespace FifaStreetPc;

public sealed record AptAnimationNegativeControlSummary(
    string Source,
    int AptRows,
    int RowsWithValidObject,
    int RowsWithMorphSymbol,
    int RowsWithMorphSuffixCandidate,
    int RowsWithCompleteStructuralCompanions,
    int DistinctStems,
    int ExactStemMatchesAnimationBankNames,
    int ExactStemMatchesAnimationFileNames,
    int ExactMorphStemMatchesAnimationBankNames,
    int ExactMorphStemMatchesAnimationFileNames,
    int DistinctPackageStructuralSignatures,
    int DistinctShapeStructuralSignatures);

public static class AptAnimationNegativeControl
{
    public static IReadOnlyList<AptAnimationNegativeControlSummary> Evaluate(
        IReadOnlyList<(string Source, string Path)> aptReports,
        string animationNameIndexPath)
    {
        ArgumentNullException.ThrowIfNull(aptReports);
        ArgumentException.ThrowIfNullOrWhiteSpace(animationNameIndexPath);

        var animationRows = ReadRows(animationNameIndexPath);
        var animationBankNames = animationRows
            .Select(row => GetValue(row, "bank_name"))
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var animationFileNames = animationRows
            .Select(row => GetValue(row, "file_name"))
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return aptReports
            .Select(report => EvaluateReport(
                report.Source,
                report.Path,
                animationBankNames,
                animationFileNames))
            .ToArray();
    }

    public static int GetAnimationRowCount(string animationNameIndexPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(animationNameIndexPath);
        return ReadRows(animationNameIndexPath).Count;
    }

    public static IReadOnlyList<string> ToTsv(
        IReadOnlyList<AptAnimationNegativeControlSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "source",
                "apt_rows",
                "rows_with_valid_object",
                "rows_with_morph_symbol",
                "rows_with_morph_suffix_candidate",
                "rows_with_complete_structural_companions",
                "distinct_stems",
                "exact_stem_matches_animation_bank_names",
                "exact_stem_matches_animation_file_names",
                "exact_morph_stem_matches_animation_bank_names",
                "exact_morph_stem_matches_animation_file_names",
                "distinct_package_structural_signatures",
                "distinct_shape_structural_signatures",
            }),
        };
        lines.AddRange(summaries.Select(summary => string.Join("\t", new[]
        {
            summary.Source,
            summary.AptRows.ToString(CultureInfo.InvariantCulture),
            summary.RowsWithValidObject.ToString(CultureInfo.InvariantCulture),
            summary.RowsWithMorphSymbol.ToString(CultureInfo.InvariantCulture),
            summary.RowsWithMorphSuffixCandidate.ToString(CultureInfo.InvariantCulture),
            summary.RowsWithCompleteStructuralCompanions.ToString(CultureInfo.InvariantCulture),
            summary.DistinctStems.ToString(CultureInfo.InvariantCulture),
            summary.ExactStemMatchesAnimationBankNames.ToString(CultureInfo.InvariantCulture),
            summary.ExactStemMatchesAnimationFileNames.ToString(CultureInfo.InvariantCulture),
            summary.ExactMorphStemMatchesAnimationBankNames.ToString(CultureInfo.InvariantCulture),
            summary.ExactMorphStemMatchesAnimationFileNames.ToString(CultureInfo.InvariantCulture),
            summary.DistinctPackageStructuralSignatures.ToString(CultureInfo.InvariantCulture),
            summary.DistinctShapeStructuralSignatures.ToString(CultureInfo.InvariantCulture),
        })));
        return lines;
    }

    private static AptAnimationNegativeControlSummary EvaluateReport(
        string source,
        string path,
        ISet<string> animationBankNames,
        ISet<string> animationFileNames)
    {
        var rows = ReadRows(path);
        var stems = rows
            .Select(row => GetValue(row, "stem"))
            .Where(value => value.Length > 0)
            .ToArray();
        var morphStems = rows
            .Select(row => GetValue(row, "normalized_morph_sibling_stem_candidate"))
            .Where(value => value.Length > 0)
            .ToArray();
        return new AptAnimationNegativeControlSummary(
            source,
            rows.Count,
            rows.Count(row => IsTrue(row, "object_valid")),
            rows.Count(row => IsTrue(row, "object_has_morph_symbol")),
            morphStems.Length,
            rows.Count(row => IsTrue(row, "constant_valid") &&
                              IsTrue(row, "object_valid") &&
                              IsTrue(row, "shape_valid")),
            stems.Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            stems.Count(animationBankNames.Contains),
            stems.Count(animationFileNames.Contains),
            morphStems.Count(animationBankNames.Contains),
            morphStems.Count(animationFileNames.Contains),
            rows.Select(row => GetValue(row, "package_structural_signature_sha256"))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Count(),
            rows.Select(row => GetValue(row, "shape_structural_signature_sha256"))
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    private static IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"TSV report is empty: {path}");
        }

        var headers = lines[0].Split('\t');
        if (headers.Length == 0 || headers.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidDataException($"TSV report has an invalid header: {path}");
        }

        var rows = new List<IReadOnlyDictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t');
            if (fields.Length != headers.Length)
            {
                throw new InvalidDataException(
                    $"TSV report row has {fields.Length} fields; expected {headers.Length}: {path}");
            }

            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = fields[index];
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string GetValue(
        IReadOnlyDictionary<string, string> row,
        string column) => row.TryGetValue(column, out var value) ? value : string.Empty;

    private static bool IsTrue(
        IReadOnlyDictionary<string, string> row,
        string column) => string.Equals(GetValue(row, column), "True", StringComparison.OrdinalIgnoreCase);
}
