using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Structural measurements of the two printable payload-name anchors already
/// observed for each catalog row. The intervals are deliberately not treated
/// as record boundaries or as frame, pose, rig, timing, skinning or decoder
/// fields.
/// </summary>
public sealed record AnimationBankCatalogPayloadAnchorAuditRow(
    string BankName,
    int CatalogRows,
    int TaggedRows,
    int RowsWithTwoAnchors,
    int AnchorCount,
    int WithinNameGapCount,
    int CrossNameGapCount,
    int MinWithinNameGap,
    int MedianWithinNameGap,
    int MaxWithinNameGap,
    int MinCrossNameGap,
    int MedianCrossNameGap,
    int MaxCrossNameGap,
    bool StrictlyIncreasing,
    string Status,
    string Issue);

public sealed record AnimationBankCatalogPayloadAnchorAuditSummary(
    int BankCount,
    int CatalogRows,
    int TaggedRows,
    int RowsWithTwoAnchors,
    int AnchorCount,
    int WithinNameGapCount,
    int CrossNameGapCount,
    int BanksStrictlyIncreasing,
    int BanksWithCompletePairs,
    string Status,
    string OriginalSemantics);

public static class AnimationBankCatalogPayloadAnchorAudit
{
    private sealed record AnchorRow(int RowIndex, int FirstOffset, int SecondOffset);
    private sealed record Anchor(int RowIndex, int Offset);

    public static IReadOnlyList<AnimationBankCatalogPayloadAnchorAuditRow> ReadAndAudit(
        string detailReportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detailReportPath);
        if (!File.Exists(detailReportPath))
        {
            return Array.Empty<AnimationBankCatalogPayloadAnchorAuditRow>();
        }

        var lines = File.ReadAllLines(detailReportPath);
        if (lines.Length == 0)
        {
            return Array.Empty<AnimationBankCatalogPayloadAnchorAuditRow>();
        }

        var headers = lines[0].Split('\t');
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = line.Split('\t');
            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = index < fields.Length ? fields[index] : string.Empty;
            }
            rows.Add(row);
        }

        return Audit(rows);
    }

    public static IReadOnlyList<AnimationBankCatalogPayloadAnchorAuditRow> Audit(
        IEnumerable<IReadOnlyDictionary<string, string>> detailRows)
    {
        ArgumentNullException.ThrowIfNull(detailRows);
        var grouped = detailRows
            .GroupBy(row => Get(row, "bank_name"), StringComparer.Ordinal)
            .Where(group => group.Key.Length > 0)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        var result = new List<AnimationBankCatalogPayloadAnchorAuditRow>(grouped.Length);
        foreach (var group in grouped)
        {
            var catalogRows = group.Count();
            var taggedRows = group.Count(row => string.Equals(Get(row, "match_kind"), "tagged", StringComparison.Ordinal));
            var anchorRows = new List<AnchorRow>();
            var rowIndex = 0;
            foreach (var row in group)
            {
                if (!string.Equals(Get(row, "match_kind"), "tagged", StringComparison.Ordinal))
                {
                    rowIndex++;
                    continue;
                }

                var offsets = ParseOffsets(Get(row, "payload_offsets"));
                if (offsets.Count >= 2)
                {
                    anchorRows.Add(new AnchorRow(rowIndex, offsets[0], offsets[1]));
                }
                rowIndex++;
            }

            var anchors = anchorRows
                .SelectMany(row => new[]
                {
                    new Anchor(row.RowIndex, row.FirstOffset),
                    new Anchor(row.RowIndex, row.SecondOffset),
                })
                .OrderBy(anchor => anchor.Offset)
                .ThenBy(anchor => anchor.RowIndex)
                .ToArray();
            var strictlyIncreasing = anchors
                .Zip(anchors.Skip(1), (first, second) => second.Offset > first.Offset)
                .All(value => value);
            var withinGaps = anchorRows
                .Select(row => row.SecondOffset - row.FirstOffset)
                .Where(gap => gap >= 0)
                .OrderBy(value => value)
                .ToArray();
            var crossGaps = anchors
                .Zip(anchors.Skip(1), (first, second) =>
                    (first, second, Gap: second.Offset - first.Offset))
                .Where(pair => pair.first.RowIndex != pair.second.RowIndex && pair.Gap >= 0)
                .Select(pair => pair.Gap)
                .OrderBy(value => value)
                .ToArray();
            var complete = taggedRows == catalogRows &&
                           anchorRows.Count == catalogRows &&
                           anchors.Length == catalogRows * 2 &&
                           strictlyIncreasing;
            result.Add(new AnimationBankCatalogPayloadAnchorAuditRow(
                group.Key,
                catalogRows,
                taggedRows,
                anchorRows.Count,
                anchors.Length,
                withinGaps.Length,
                crossGaps.Length,
                Min(withinGaps),
                Median(withinGaps),
                Max(withinGaps),
                Min(crossGaps),
                Median(crossGaps),
                Max(crossGaps),
                strictlyIncreasing,
                complete ? "verified-structural" : "incomplete",
                complete
                    ? string.Empty
                    : "not every tagged catalog row provided two strictly ordered payload anchors"));
        }
        return result;
    }

    public static AnimationBankCatalogPayloadAnchorAuditSummary Summarize(
        IReadOnlyList<AnimationBankCatalogPayloadAnchorAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        return new AnimationBankCatalogPayloadAnchorAuditSummary(
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Sum(row => row.TaggedRows),
            rows.Sum(row => row.RowsWithTwoAnchors),
            rows.Sum(row => row.AnchorCount),
            rows.Sum(row => row.WithinNameGapCount),
            rows.Sum(row => row.CrossNameGapCount),
            rows.Count(row => row.StrictlyIncreasing),
            rows.Count(row => row.RowsWithTwoAnchors == row.CatalogRows),
            rows.Count > 0 && rows.All(row => row.Status == "verified-structural")
                ? "verified-structural"
                : "incomplete",
            "not-claimed");
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankCatalogPayloadAnchorAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "bank_name",
                "catalog_rows",
                "tagged_rows",
                "rows_with_two_anchors",
                "anchor_count",
                "within_name_gap_count",
                "cross_name_gap_count",
                "min_within_name_gap",
                "median_within_name_gap",
                "max_within_name_gap",
                "min_cross_name_gap",
                "median_cross_name_gap",
                "max_cross_name_gap",
                "strictly_increasing",
                "status",
                "issue",
            }),
        };
        lines.AddRange(materialized.Select(row => string.Join("\t", new[]
        {
            Escape(row.BankName),
            row.CatalogRows.ToString(CultureInfo.InvariantCulture),
            row.TaggedRows.ToString(CultureInfo.InvariantCulture),
            row.RowsWithTwoAnchors.ToString(CultureInfo.InvariantCulture),
            row.AnchorCount.ToString(CultureInfo.InvariantCulture),
            row.WithinNameGapCount.ToString(CultureInfo.InvariantCulture),
            row.CrossNameGapCount.ToString(CultureInfo.InvariantCulture),
            row.MinWithinNameGap.ToString(CultureInfo.InvariantCulture),
            row.MedianWithinNameGap.ToString(CultureInfo.InvariantCulture),
            row.MaxWithinNameGap.ToString(CultureInfo.InvariantCulture),
            row.MinCrossNameGap.ToString(CultureInfo.InvariantCulture),
            row.MedianCrossNameGap.ToString(CultureInfo.InvariantCulture),
            row.MaxCrossNameGap.ToString(CultureInfo.InvariantCulture),
            row.StrictlyIncreasing.ToString(),
            Escape(row.Status),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AnimationBankCatalogPayloadAnchorAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new[]
        {
            string.Join("\t", new[]
            {
                "bank_count",
                "catalog_rows",
                "tagged_rows",
                "rows_with_two_anchors",
                "anchor_count",
                "within_name_gap_count",
                "cross_name_gap_count",
                "banks_strictly_increasing",
                "banks_with_complete_pairs",
                "status",
                "original_semantics",
            }),
            string.Join("\t", new[]
            {
                summary.BankCount.ToString(CultureInfo.InvariantCulture),
                summary.CatalogRows.ToString(CultureInfo.InvariantCulture),
                summary.TaggedRows.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithTwoAnchors.ToString(CultureInfo.InvariantCulture),
                summary.AnchorCount.ToString(CultureInfo.InvariantCulture),
                summary.WithinNameGapCount.ToString(CultureInfo.InvariantCulture),
                summary.CrossNameGapCount.ToString(CultureInfo.InvariantCulture),
                summary.BanksStrictlyIncreasing.ToString(CultureInfo.InvariantCulture),
                summary.BanksWithCompletePairs.ToString(CultureInfo.InvariantCulture),
                Escape(summary.Status),
                Escape(summary.OriginalSemantics),
            }),
        };
        WriteLines(path, lines);
    }

    private static IReadOnlyList<int> ParseOffsets(string value)
    {
        var offsets = new List<int>();
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var hex = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? token[2..]
                : token;
            if (int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var offset) &&
                offset >= 0)
            {
                offsets.Add(offset);
            }
        }
        return offsets.OrderBy(offset => offset).ToArray();
    }

    private static int Min(IReadOnlyList<int> values) => values.Count == 0 ? 0 : values[0];
    private static int Max(IReadOnlyList<int> values) => values.Count == 0 ? 0 : values[^1];
    private static int Median(IReadOnlyList<int> values) => values.Count == 0 ? 0 : values[values.Count / 2];

    private static string Get(IReadOnlyDictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) ? value : string.Empty;

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
