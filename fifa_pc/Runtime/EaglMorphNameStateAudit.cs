using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglMorphNameStateCatalogRow(
    string NestedBig,
    string MorphObject,
    string? OrdSibling,
    bool HasCompleteSibling,
    IReadOnlyList<string> MorphShapeNames,
    IReadOnlyList<string> MorphAttributeNames,
    IReadOnlyList<string> SiblingShapeNameStateValues);

public sealed record EaglMorphNameStateAuditRow(
    string NestedBig,
    string MorphObject,
    string? OrdSibling,
    bool HasCompleteSibling,
    int MorphShapeNameCount,
    int MorphAttributeNameCount,
    int SiblingShapeNameStateCount,
    int ExactRawShapeStateMatchCount,
    int NormalizedShapeStateMatchCount,
    string MorphShapeNames,
    string MorphAttributeNames,
    string SiblingShapeNameStateValues,
    string NormalizedSiblingShapeNameStateValues)
{
    public bool HasNormalizedShapeStateMatch => NormalizedShapeStateMatchCount > 0;
}

public sealed record EaglMorphNameStateAuditSummary(
    int Rows,
    int CompleteSiblingRows,
    int RowsWithMorphShapeNames,
    int RowsWithSiblingShapeNameState,
    int RowsWithExactRawShapeStateMatch,
    int RowsWithNormalizedShapeStateMatch,
    int ExactRawShapeStateMatchCount,
    int NormalizedShapeStateMatchCount,
    int DistinctMorphShapeNames,
    int DistinctSiblingShapeNameStateValues);

/// <summary>
/// Compares preserved morph object names with preserved companion SHAPENAME
/// state text. The comparison is lexical evidence only: it does not bind a
/// morph string to a mesh group, material, UV, weight, bone, pose, frame,
/// animation, or GPU command.
/// </summary>
public static class EaglMorphNameStateAudit
{
    private static readonly string[] RequiredColumns =
    {
        "nested_big",
        "morph_object",
        "ord_sibling",
        "has_complete_sibling",
        "morph_shape_names",
        "morph_attribute_names",
        "sibling_shapename_state_values",
    };

    public static IReadOnlyList<EaglMorphNameStateCatalogRow> ReadCatalog(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException("Morph structural catalog is empty.");
        }

        var header = lines[0].Split('\t');
        var indexes = RequiredColumns.ToDictionary(
            column => column,
            column => Array.FindIndex(header, value =>
                string.Equals(value, column, StringComparison.Ordinal)),
            StringComparer.Ordinal);
        var missing = indexes
            .Where(pair => pair.Value < 0)
            .Select(pair => pair.Key)
            .ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidDataException(
                $"Morph structural catalog is missing required name/state columns: {string.Join(",", missing)}.");
        }

        var rows = new List<EaglMorphNameStateCatalogRow>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new EaglMorphNameStateCatalogRow(
                ReadField(fields, indexes["nested_big"], lineIndex),
                ReadField(fields, indexes["morph_object"], lineIndex),
                NullIfEmpty(ReadField(fields, indexes["ord_sibling"], lineIndex)),
                ReadBool(fields, indexes["has_complete_sibling"], lineIndex),
                ReadList(fields, indexes["morph_shape_names"], lineIndex),
                ReadList(fields, indexes["morph_attribute_names"], lineIndex),
                ReadList(fields, indexes["sibling_shapename_state_values"], lineIndex)));
        }

        return rows;
    }

    public static IReadOnlyList<EaglMorphNameStateAuditRow> Audit(
        IEnumerable<EaglMorphNameStateCatalogRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return rows.Select(row =>
        {
            var rawStateValues = row.SiblingShapeNameStateValues
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var normalizedStateValues = rawStateValues
                .Select(NormalizeStateValue)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var rawSet = rawStateValues.ToHashSet(StringComparer.Ordinal);
            var normalizedSet = normalizedStateValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var exactRawMatches = row.MorphShapeNames
                .Distinct(StringComparer.Ordinal)
                .Count(rawSet.Contains);
            var normalizedMatches = row.MorphShapeNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(name => normalizedSet.Contains(name));

            return new EaglMorphNameStateAuditRow(
                row.NestedBig,
                row.MorphObject,
                row.OrdSibling,
                row.HasCompleteSibling,
                row.MorphShapeNames.Count,
                row.MorphAttributeNames.Count,
                rawStateValues.Length,
                exactRawMatches,
                normalizedMatches,
                string.Join("|", row.MorphShapeNames),
                string.Join("|", row.MorphAttributeNames),
                string.Join("|", rawStateValues),
                string.Join("|", normalizedStateValues));
        }).ToArray();
    }

    public static EaglMorphNameStateAuditSummary Summarize(
        IEnumerable<EaglMorphNameStateAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        return new EaglMorphNameStateAuditSummary(
            materialized.Length,
            materialized.Count(row => row.HasCompleteSibling),
            materialized.Count(row => row.MorphShapeNameCount > 0),
            materialized.Count(row => row.SiblingShapeNameStateCount > 0),
            materialized.Count(row => row.ExactRawShapeStateMatchCount > 0),
            materialized.Count(row => row.HasNormalizedShapeStateMatch),
            materialized.Sum(row => row.ExactRawShapeStateMatchCount),
            materialized.Sum(row => row.NormalizedShapeStateMatchCount),
            materialized
                .SelectMany(row => SplitList(row.MorphShapeNames))
                .Distinct(StringComparer.Ordinal)
                .Count(),
            materialized
                .SelectMany(row => SplitList(row.SiblingShapeNameStateValues))
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglMorphNameStateAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "nested_big",
                "morph_object",
                "ord_sibling",
                "has_complete_sibling",
                "morph_shape_name_count",
                "morph_attribute_name_count",
                "sibling_shapename_state_count",
                "exact_raw_shape_state_match_count",
                "normalized_shape_state_match_count",
                "morph_shape_names",
                "morph_attribute_names",
                "sibling_shapename_state_values",
                "normalized_sibling_shapename_state_values",
            }),
        };
        lines.AddRange(materialized.Select(row => string.Join("\t", new[]
        {
            Tsv(row.NestedBig),
            Tsv(row.MorphObject),
            Tsv(row.OrdSibling),
            row.HasCompleteSibling.ToString(),
            row.MorphShapeNameCount.ToString(CultureInfo.InvariantCulture),
            row.MorphAttributeNameCount.ToString(CultureInfo.InvariantCulture),
            row.SiblingShapeNameStateCount.ToString(CultureInfo.InvariantCulture),
            row.ExactRawShapeStateMatchCount.ToString(CultureInfo.InvariantCulture),
            row.NormalizedShapeStateMatchCount.ToString(CultureInfo.InvariantCulture),
            Tsv(row.MorphShapeNames),
            Tsv(row.MorphAttributeNames),
            Tsv(row.SiblingShapeNameStateValues),
            Tsv(row.NormalizedSiblingShapeNameStateValues),
        })));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string NormalizeStateValue(string value) =>
        value.Split(',', 2)[0].Trim();

    private static IReadOnlyList<string> ReadList(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex) =>
        ReadField(fields, index, lineIndex)
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    private static string ReadField(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex)
    {
        if (index < 0 || index >= fields.Count)
        {
            throw new InvalidDataException(
                $"Morph name/state catalog row {lineIndex + 1} is missing a field.");
        }

        return fields[index];
    }

    private static bool ReadBool(
        IReadOnlyList<string> fields,
        int index,
        int lineIndex)
    {
        var field = ReadField(fields, index, lineIndex);
        if (!bool.TryParse(field, out var value))
        {
            throw new InvalidDataException(
                $"Morph name/state catalog row {lineIndex + 1} has invalid boolean '{field}'.");
        }

        return value;
    }

    private static string? NullIfEmpty(string value) =>
        value.Length == 0 ? null : value;

    private static IEnumerable<string> SplitList(string value) =>
        value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Tsv(string? value) =>
        value is null ? string.Empty : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
