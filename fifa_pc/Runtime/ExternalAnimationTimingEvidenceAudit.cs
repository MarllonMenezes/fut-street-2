using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Validates an explicitly supplied external timing observation file without
/// assigning any of its columns to original animation semantics.
/// </summary>
public static class ExternalAnimationTimingEvidenceAudit
{
    public const string ReportHeader =
        "line_number\tcapture_id\tsource_id\tbank_name\tasset_name\tsample_index\ttime_ms\tobservation_tag\tvalid\tissue";

    public const string SummaryHeader =
        "status\tinput_path\tevidence_file_sha256\tobservation_rows\tvalid_rows\tinvalid_rows\tdistinct_capture_ids\tbank_asset_pairs\tpairs_with_multiple_captures\tdirect_timing_binding\toriginal_semantics";

    private const string RequiredHeader =
        "capture_id\tsource_id\tbank_name\tasset_name\tsample_index\ttime_ms\tobservation_tag";

    public sealed record ObservationRow(
        int LineNumber,
        string CaptureId,
        string SourceId,
        string BankName,
        string AssetName,
        int? SampleIndex,
        decimal? TimeMilliseconds,
        string ObservationTag,
        bool Valid,
        string Issue);

    public sealed record Summary(
        string Status,
        string InputPath,
        string EvidenceFileSha256,
        int ObservationRows,
        int ValidRows,
        int InvalidRows,
        int DistinctCaptureIds,
        int BankAssetPairs,
        int PairsWithMultipleCaptures,
        string DirectTimingBinding,
        string OriginalSemantics);

    public sealed record Result(IReadOnlyList<ObservationRow> Rows, Summary Summary);

    public static Result Audit(string inputPath)
    {
        var fullPath = Path.GetFullPath(inputPath);
        if (!File.Exists(fullPath))
        {
            return new Result(
                Array.Empty<ObservationRow>(),
                new Summary(
                    "missing-evidence",
                    fullPath,
                    string.Empty,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    "not-proven",
                    "not-claimed"));
        }

        var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
        var rows = new List<ObservationRow>();
        var headerFound = false;
        for (var index = 0; index < lines.Length; index++)
        {
            var lineNumber = index + 1;
            var line = lines[index];
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (!headerFound)
            {
                headerFound = true;
                if (!string.Equals(line.Trim(), RequiredHeader, StringComparison.Ordinal))
                {
                    rows.Add(new ObservationRow(
                        lineNumber,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        null,
                        null,
                        string.Empty,
                        false,
                        "header-mismatch"));
                }

                continue;
            }

            rows.Add(ParseRow(lineNumber, line));
        }

        var validRows = rows.Where(row => row.Valid).ToArray();
        var groupedRows = validRows
            .GroupBy(row => $"{row.CaptureId}\u001f{row.BankName}\u001f{row.AssetName}", StringComparer.Ordinal)
            .ToArray();
        var invalidRows = rows.Count(row => !row.Valid);
        var orderedIssues = ValidateOrdering(groupedRows);
        if (orderedIssues.Count > 0)
        {
            foreach (var issue in orderedIssues)
            {
                var rowIndex = rows.FindIndex(row => row.LineNumber == issue.LineNumber);
                if (rowIndex < 0)
                {
                    continue;
                }

                var row = rows[rowIndex];
                rows[rowIndex] = row with
                {
                    Valid = false,
                    Issue = CombineIssue(row.Issue, issue.Message),
                };
            }

            validRows = rows.Where(row => row.Valid).ToArray();
            invalidRows = rows.Count(row => !row.Valid);
        }

        var bankAssetPairs = validRows
            .Select(row => $"{row.BankName}\u001f{row.AssetName}")
            .Distinct(StringComparer.Ordinal)
            .Count();
        var pairsWithMultipleCaptures = validRows
            .GroupBy(row => $"{row.BankName}\u001f{row.AssetName}", StringComparer.Ordinal)
            .Count(group => group.Select(row => row.CaptureId).Distinct(StringComparer.Ordinal).Count() > 1);

        var status = !headerFound
            ? "invalid"
            : invalidRows == 0 && rows.Count > 0
                ? "verified-external-observation"
                : invalidRows > 0
                    ? "invalid"
                    : "missing-evidence";
        return new Result(
            rows,
            new Summary(
                status,
                fullPath,
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(fullPath))),
                rows.Count,
                validRows.Length,
                invalidRows,
                validRows.Select(row => row.CaptureId).Distinct(StringComparer.Ordinal).Count(),
                bankAssetPairs,
                pairsWithMultipleCaptures,
                "not-proven",
                "not-claimed"));
    }

    public static void WriteReport(string path, IReadOnlyList<ObservationRow> rows)
    {
        var lines = new List<string> { ReportHeader };
        lines.AddRange(rows.Select(row => string.Join('\t', new[]
        {
            row.LineNumber.ToString(CultureInfo.InvariantCulture),
            Field(row.CaptureId),
            Field(row.SourceId),
            Field(row.BankName),
            Field(row.AssetName),
            row.SampleIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            row.TimeMilliseconds?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            Field(row.ObservationTag),
            row.Valid ? "True" : "False",
            Field(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, Summary summary)
    {
        WriteLines(path, new[]
        {
            SummaryHeader,
            string.Join('\t', new[]
            {
                Field(summary.Status),
                Field(summary.InputPath),
                Field(summary.EvidenceFileSha256),
                summary.ObservationRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidRows.ToString(CultureInfo.InvariantCulture),
                summary.InvalidRows.ToString(CultureInfo.InvariantCulture),
                summary.DistinctCaptureIds.ToString(CultureInfo.InvariantCulture),
                summary.BankAssetPairs.ToString(CultureInfo.InvariantCulture),
                summary.PairsWithMultipleCaptures.ToString(CultureInfo.InvariantCulture),
                Field(summary.DirectTimingBinding),
                Field(summary.OriginalSemantics),
            }),
        });
    }

    private static ObservationRow ParseRow(int lineNumber, string line)
    {
        var fields = line.Split('\t');
        if (fields.Length != 7)
        {
            return InvalidRow(lineNumber, "column-count");
        }

        var captureId = fields[0].Trim();
        var sourceId = fields[1].Trim();
        var bankName = fields[2].Trim();
        var assetName = fields[3].Trim();
        var observationTag = fields[6].Trim();
        if (captureId.Length == 0 || sourceId.Length == 0 || bankName.Length == 0 ||
            assetName.Length == 0 || observationTag.Length == 0)
        {
            return new ObservationRow(
                lineNumber,
                captureId,
                sourceId,
                bankName,
                assetName,
                null,
                null,
                observationTag,
                false,
                "empty-required-field");
        }

        if (!int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var sampleIndex) || sampleIndex < 0)
        {
            return new ObservationRow(
                lineNumber,
                captureId,
                sourceId,
                bankName,
                assetName,
                null,
                null,
                observationTag,
                false,
                "invalid-sample-index");
        }

        if (!decimal.TryParse(fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var timeMilliseconds) || timeMilliseconds < 0)
        {
            return new ObservationRow(
                lineNumber,
                captureId,
                sourceId,
                bankName,
                assetName,
                sampleIndex,
                null,
                observationTag,
                false,
                "invalid-time-ms");
        }

        return new ObservationRow(
            lineNumber,
            captureId,
            sourceId,
            bankName,
            assetName,
            sampleIndex,
            timeMilliseconds,
            observationTag,
            true,
            string.Empty);
    }

    private static ObservationRow InvalidRow(int lineNumber, string issue) =>
        new(lineNumber, string.Empty, string.Empty, string.Empty, string.Empty, null, null, string.Empty, false, issue);

    private static List<(int LineNumber, string Message)> ValidateOrdering(
        IReadOnlyList<IGrouping<string, ObservationRow>> groups)
    {
        var issues = new List<(int LineNumber, string Message)>();
        foreach (var group in groups)
        {
            ObservationRow? previous = null;
            foreach (var row in group.OrderBy(row => row.LineNumber))
            {
                if (previous is not null &&
                    (row.SampleIndex <= previous.SampleIndex || row.TimeMilliseconds < previous.TimeMilliseconds))
                {
                    issues.Add((row.LineNumber, "non-monotonic-sequence"));
                }

                previous = row;
            }
        }

        return issues;
    }

    private static string CombineIssue(string first, string second) =>
        string.IsNullOrEmpty(first) ? second : $"{first};{second}";

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
