using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Compares exact APT/.const/companion profile keys between the FE and IG
/// package reports. It measures package-profile recurrence only; it does not
/// assign timing, pose, frame, morph, rig, material, or animation meaning.
/// </summary>
public sealed record AptCrossPackageProfileAuditRow(
    string ProfileKeySha256,
    string AptHeaderPattern,
    string ConstantHeaderPattern,
    string CompanionExtensions,
    int FeAptRows,
    int IgAptRows,
    int FeObjectValidRows,
    int IgObjectValidRows,
    int FeShapeValidRows,
    int IgShapeValidRows,
    int FeAlignedComparedWords,
    int IgAlignedComparedWords,
    int FeAlignedEqualWords,
    int IgAlignedEqualWords,
    int FeMorphSymbolRows,
    int IgMorphSymbolRows,
    bool SharedProfileKey);

public sealed record AptCrossPackageProfileAuditSummary(
    int FeProfileKeys,
    int IgProfileKeys,
    int SharedProfileKeys,
    int FeOnlyProfileKeys,
    int IgOnlyProfileKeys,
    int SharedKeysWithAlignedEqualWords,
    int SharedKeysWithMorphSymbolRows,
    int FeAlignedComparedWords,
    int IgAlignedComparedWords,
    int FeAlignedEqualWords,
    int IgAlignedEqualWords);

public static class AptCrossPackageProfileAudit
{
    private static readonly string[] RequiredColumns =
    {
        "apt_header_pattern",
        "constant_header_pattern",
        "companion_extensions",
        "row_count",
        "rows_with_aligned_equal_words",
        "total_aligned_compared_words",
        "total_aligned_equal_words",
        "total_aligned_different_words",
        "apt_fields_within_asset_rows",
        "constant_fields_within_asset_rows",
        "object_valid_rows",
        "shape_valid_rows",
        "morph_symbol_rows",
        "distinct_object_structural_signature_hashes",
        "distinct_shape_structural_signature_hashes",
    };

    public static IReadOnlyList<AptStructuralProfile> ReadProfiles(string path)
    {
        var lines = File.ReadAllLines(path);
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"APT profile report is empty: {path}");
        }

        var headers = lines[0].Split('\t');
        var indexes = RequiredColumns.ToDictionary(
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
                $"APT profile report is missing columns: {string.Join(",", missing)}: {path}");
        }

        var rows = new List<AptStructuralProfile>();
        for (var lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            if (string.IsNullOrWhiteSpace(lines[lineIndex]))
            {
                continue;
            }

            var fields = lines[lineIndex].Split('\t');
            rows.Add(new AptStructuralProfile(
                ReadField(fields, indexes["apt_header_pattern"], lineIndex, path),
                ReadField(fields, indexes["constant_header_pattern"], lineIndex, path),
                ReadField(fields, indexes["companion_extensions"], lineIndex, path),
                ReadInt(fields, indexes["row_count"], lineIndex, path),
                ReadInt(fields, indexes["rows_with_aligned_equal_words"], lineIndex, path),
                ReadInt(fields, indexes["total_aligned_compared_words"], lineIndex, path),
                ReadInt(fields, indexes["total_aligned_equal_words"], lineIndex, path),
                ReadInt(fields, indexes["total_aligned_different_words"], lineIndex, path),
                ReadInt(fields, indexes["apt_fields_within_asset_rows"], lineIndex, path),
                ReadInt(fields, indexes["constant_fields_within_asset_rows"], lineIndex, path),
                ReadInt(fields, indexes["object_valid_rows"], lineIndex, path),
                ReadInt(fields, indexes["shape_valid_rows"], lineIndex, path),
                ReadInt(fields, indexes["morph_symbol_rows"], lineIndex, path),
                ReadInt(fields, indexes["distinct_object_structural_signature_hashes"], lineIndex, path),
                ReadInt(fields, indexes["distinct_shape_structural_signature_hashes"], lineIndex, path)));
        }

        return rows;
    }

    public static IReadOnlyList<AptCrossPackageProfileAuditRow> Audit(
        IEnumerable<AptStructuralProfile> feProfiles,
        IEnumerable<AptStructuralProfile> igProfiles)
    {
        ArgumentNullException.ThrowIfNull(feProfiles);
        ArgumentNullException.ThrowIfNull(igProfiles);

        var feByKey = IndexProfiles(feProfiles, "fe");
        var igByKey = IndexProfiles(igProfiles, "ig");
        return feByKey.Keys
            .Concat(igByKey.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .Select(key =>
            {
                feByKey.TryGetValue(key, out var fe);
                igByKey.TryGetValue(key, out var ig);
                var parts = key.Split('\u001F');
                return new AptCrossPackageProfileAuditRow(
                    Sha256(key),
                    parts[0],
                    parts[1],
                    parts[2],
                    fe?.RowCount ?? 0,
                    ig?.RowCount ?? 0,
                    fe?.ObjectValidRows ?? 0,
                    ig?.ObjectValidRows ?? 0,
                    fe?.ShapeValidRows ?? 0,
                    ig?.ShapeValidRows ?? 0,
                    fe?.TotalAlignedComparedWords ?? 0,
                    ig?.TotalAlignedComparedWords ?? 0,
                    fe?.TotalAlignedEqualWords ?? 0,
                    ig?.TotalAlignedEqualWords ?? 0,
                    fe?.MorphSymbolRows ?? 0,
                    ig?.MorphSymbolRows ?? 0,
                    fe is not null && ig is not null);
            })
            .ToArray();
    }

    public static AptCrossPackageProfileAuditSummary Summarize(
        IEnumerable<AptCrossPackageProfileAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var all = rows.ToArray();
        var shared = all.Where(row => row.SharedProfileKey).ToArray();
        return new AptCrossPackageProfileAuditSummary(
            all.Count(row => row.FeAptRows > 0),
            all.Count(row => row.IgAptRows > 0),
            shared.Length,
            all.Count(row => row.FeAptRows > 0 && row.IgAptRows == 0),
            all.Count(row => row.FeAptRows == 0 && row.IgAptRows > 0),
            shared.Count(row => row.FeAlignedEqualWords > 0 || row.IgAlignedEqualWords > 0),
            shared.Count(row => row.FeMorphSymbolRows > 0 || row.IgMorphSymbolRows > 0),
            all.Sum(row => row.FeAlignedComparedWords),
            all.Sum(row => row.IgAlignedComparedWords),
            all.Sum(row => row.FeAlignedEqualWords),
            all.Sum(row => row.IgAlignedEqualWords));
    }

    public static void WriteReports(
        string auditPath,
        string summaryPath,
        IEnumerable<AptCrossPackageProfileAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(auditPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(summaryPath);
        ArgumentNullException.ThrowIfNull(rows);

        var materialized = rows.ToArray();
        WriteLines(auditPath, new[]
        {
            "profile_key_sha256",
            "apt_header_pattern",
            "constant_header_pattern",
            "companion_extensions",
            "fe_apt_rows",
            "ig_apt_rows",
            "fe_object_valid_rows",
            "ig_object_valid_rows",
            "fe_shape_valid_rows",
            "ig_shape_valid_rows",
            "fe_aligned_compared_words",
            "ig_aligned_compared_words",
            "fe_aligned_equal_words",
            "ig_aligned_equal_words",
            "fe_morph_symbol_rows",
            "ig_morph_symbol_rows",
            "shared_profile_key",
        }, materialized.Select(ToFields));

        var summary = Summarize(materialized);
        WriteLines(summaryPath, new[]
        {
            "fe_profile_keys",
            "ig_profile_keys",
            "shared_profile_keys",
            "fe_only_profile_keys",
            "ig_only_profile_keys",
            "shared_keys_with_aligned_equal_words",
            "shared_keys_with_morph_symbol_rows",
            "fe_aligned_compared_words",
            "ig_aligned_compared_words",
            "fe_aligned_equal_words",
            "ig_aligned_equal_words",
        }, new[] { SummaryFields(summary) });
    }

    private static Dictionary<string, AptStructuralProfile> IndexProfiles(
        IEnumerable<AptStructuralProfile> profiles,
        string source)
    {
        var indexed = new Dictionary<string, AptStructuralProfile>(StringComparer.Ordinal);
        foreach (var profile in profiles)
        {
            var key = GetKey(profile);
            if (!indexed.TryAdd(key, profile))
            {
                throw new InvalidDataException(
                    $"APT {source} profile report contains a duplicate exact profile key.");
            }
        }

        return indexed;
    }

    private static string GetKey(AptStructuralProfile profile) =>
        string.Join(
            '\u001F',
            profile.AptHeaderPattern,
            profile.ConstantHeaderPattern,
            profile.CompanionExtensions);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static IEnumerable<string> ToFields(AptCrossPackageProfileAuditRow row)
    {
        return new[]
        {
            row.ProfileKeySha256,
            Escape(row.AptHeaderPattern),
            Escape(row.ConstantHeaderPattern),
            Escape(row.CompanionExtensions),
            row.FeAptRows.ToString(CultureInfo.InvariantCulture),
            row.IgAptRows.ToString(CultureInfo.InvariantCulture),
            row.FeObjectValidRows.ToString(CultureInfo.InvariantCulture),
            row.IgObjectValidRows.ToString(CultureInfo.InvariantCulture),
            row.FeShapeValidRows.ToString(CultureInfo.InvariantCulture),
            row.IgShapeValidRows.ToString(CultureInfo.InvariantCulture),
            row.FeAlignedComparedWords.ToString(CultureInfo.InvariantCulture),
            row.IgAlignedComparedWords.ToString(CultureInfo.InvariantCulture),
            row.FeAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
            row.IgAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
            row.FeMorphSymbolRows.ToString(CultureInfo.InvariantCulture),
            row.IgMorphSymbolRows.ToString(CultureInfo.InvariantCulture),
            row.SharedProfileKey.ToString(),
        };
    }

    private static IEnumerable<string> SummaryFields(AptCrossPackageProfileAuditSummary summary)
    {
        return new[]
        {
            summary.FeProfileKeys.ToString(CultureInfo.InvariantCulture),
            summary.IgProfileKeys.ToString(CultureInfo.InvariantCulture),
            summary.SharedProfileKeys.ToString(CultureInfo.InvariantCulture),
            summary.FeOnlyProfileKeys.ToString(CultureInfo.InvariantCulture),
            summary.IgOnlyProfileKeys.ToString(CultureInfo.InvariantCulture),
            summary.SharedKeysWithAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
            summary.SharedKeysWithMorphSymbolRows.ToString(CultureInfo.InvariantCulture),
            summary.FeAlignedComparedWords.ToString(CultureInfo.InvariantCulture),
            summary.IgAlignedComparedWords.ToString(CultureInfo.InvariantCulture),
            summary.FeAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
            summary.IgAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
        };
    }

    private static string ReadField(string[] fields, int index, int lineIndex, string path)
    {
        if (index < 0 || index >= fields.Length)
        {
            throw new InvalidDataException($"APT profile row at line {lineIndex + 1} has too few columns: {path}");
        }

        return fields[index];
    }

    private static int ReadInt(string[] fields, int index, int lineIndex, string path)
    {
        var value = ReadField(fields, index, lineIndex, path);
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidDataException($"Invalid APT profile integer at line {lineIndex + 1}: {value}");
        }

        return parsed;
    }

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

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
