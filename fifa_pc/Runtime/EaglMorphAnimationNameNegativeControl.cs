using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglMorphAnimationNameNegativeControlSummary(
    int MorphRows,
    int CompleteSiblingRows,
    int RowsWithMorphShapeNames,
    int DistinctMorphShapeNames,
    int AnimationNameIndexRows,
    int DistinctAnimationFileNames,
    int DistinctAnimationBankNames,
    int RowsWithExactAnimationFileNameMatch,
    int ExactAnimationFileNameMatchCount,
    int RowsWithExactAnimationBankNameMatch,
    int ExactAnimationBankNameMatchCount,
    int DistinctExactAnimationFileNameMatches,
    int DistinctExactAnimationBankNameMatches,
    string ExactAnimationFileNameMatches,
    string ExactAnimationBankNameMatches,
    string ExactAnimationFileNameMatchesSha256,
    string ExactAnimationBankNameMatchesSha256);

/// <summary>
/// Compares complete morph shape-name strings with observed animation catalog
/// and file names. This is an exact lexical negative control only; it does not
/// normalize names, infer timing, or assign morph, pose, rig, frame, skinning,
/// material, UV, GPU, or animation semantics.
/// </summary>
public static class EaglMorphAnimationNameNegativeControl
{
    private static readonly string[] AnimationNameColumns =
    {
        "bank_name",
        "file_name",
    };

    public static EaglMorphAnimationNameNegativeControlSummary Evaluate(
        IReadOnlyList<EaglMorphNameStateCatalogRow> morphRows,
        int animationNameIndexRows,
        IReadOnlySet<string> animationBankNames,
        IReadOnlySet<string> animationFileNames)
    {
        ArgumentNullException.ThrowIfNull(morphRows);
        ArgumentNullException.ThrowIfNull(animationBankNames);
        ArgumentNullException.ThrowIfNull(animationFileNames);

        var shapeNameRows = morphRows
            .Select(row => row.MorphShapeNames
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray())
            .ToArray();
        var distinctShapeNames = shapeNameRows
            .SelectMany(names => names)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var fileNameMatches = distinctShapeNames
            .Where(animationFileNames.Contains)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var bankNameMatches = distinctShapeNames
            .Where(animationBankNames.Contains)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new EaglMorphAnimationNameNegativeControlSummary(
            morphRows.Count,
            morphRows.Count(row => row.HasCompleteSibling),
            shapeNameRows.Count(names => names.Length > 0),
            distinctShapeNames.Length,
            animationNameIndexRows,
            animationFileNames.Count,
            animationBankNames.Count,
            shapeNameRows.Count(names => names.Any(animationFileNames.Contains)),
            shapeNameRows.Sum(names => names.Count(animationFileNames.Contains)),
            shapeNameRows.Count(names => names.Any(animationBankNames.Contains)),
            shapeNameRows.Sum(names => names.Count(animationBankNames.Contains)),
            fileNameMatches.Length,
            bankNameMatches.Length,
            string.Join("|", fileNameMatches),
            string.Join("|", bankNameMatches),
            HashText(string.Join("\n", fileNameMatches)),
            HashText(string.Join("\n", bankNameMatches)));
    }

    public static (int RowCount, IReadOnlySet<string> BankNames, IReadOnlySet<string> FileNames)
        ReadAnimationNameIndex(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Animation name index is empty.");
        }

        var headers = lines[0].Split('\t');
        var indexes = AnimationNameColumns.ToDictionary(
            column => column,
            column => Array.FindIndex(headers, value =>
                string.Equals(value, column, StringComparison.Ordinal)),
            StringComparer.Ordinal);
        var missing = indexes
            .Where(pair => pair.Value < 0)
            .Select(pair => pair.Key)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Animation name index is missing columns: {string.Join(",", missing)}.");
        }

        var bankNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rowCount = 0;
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
                    $"Animation name index row has {fields.Length} fields; expected {headers.Length}.");
            }

            rowCount++;
            if (fields[indexes["bank_name"]].Length > 0)
            {
                bankNames.Add(fields[indexes["bank_name"]]);
            }

            if (fields[indexes["file_name"]].Length > 0)
            {
                fileNames.Add(fields[indexes["file_name"]]);
            }
        }

        return (rowCount, bankNames, fileNames);
    }

    public static void WriteReport(
        string path,
        EaglMorphAnimationNameNegativeControlSummary summary)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "morph_rows",
                "complete_sibling_rows",
                "rows_with_morph_shape_names",
                "distinct_morph_shape_names",
                "animation_name_index_rows",
                "distinct_animation_file_names",
                "distinct_animation_bank_names",
                "rows_with_exact_animation_file_name_match",
                "exact_animation_file_name_match_count",
                "rows_with_exact_animation_bank_name_match",
                "exact_animation_bank_name_match_count",
                "distinct_exact_animation_file_name_matches",
                "distinct_exact_animation_bank_name_matches",
                "exact_animation_file_name_matches",
                "exact_animation_bank_name_matches",
                "exact_animation_file_name_matches_sha256",
                "exact_animation_bank_name_matches_sha256",
            }),
            string.Join("\t", new[]
            {
                summary.MorphRows.ToString(CultureInfo.InvariantCulture),
                summary.CompleteSiblingRows.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithMorphShapeNames.ToString(CultureInfo.InvariantCulture),
                summary.DistinctMorphShapeNames.ToString(CultureInfo.InvariantCulture),
                summary.AnimationNameIndexRows.ToString(CultureInfo.InvariantCulture),
                summary.DistinctAnimationFileNames.ToString(CultureInfo.InvariantCulture),
                summary.DistinctAnimationBankNames.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithExactAnimationFileNameMatch.ToString(CultureInfo.InvariantCulture),
                summary.ExactAnimationFileNameMatchCount.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithExactAnimationBankNameMatch.ToString(CultureInfo.InvariantCulture),
                summary.ExactAnimationBankNameMatchCount.ToString(CultureInfo.InvariantCulture),
                summary.DistinctExactAnimationFileNameMatches.ToString(CultureInfo.InvariantCulture),
                summary.DistinctExactAnimationBankNameMatches.ToString(CultureInfo.InvariantCulture),
                Tsv(summary.ExactAnimationFileNameMatches),
                Tsv(summary.ExactAnimationBankNameMatches),
                summary.ExactAnimationFileNameMatchesSha256,
                summary.ExactAnimationBankNameMatchesSha256,
            }),
        };
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string HashText(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private static string Tsv(string value) => value
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');
}
