using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankCatalogTimestampAuditRow(
    string CatalogEntry,
    string BankName,
    int CatalogRows,
    int DisplayDateParseableRows,
    int ExactTimestampDateRows,
    int TimestampDateMismatchRows,
    int InvalidTimestampRows,
    string InferredUtcOffsetMinutes,
    string MinimumTimestamp,
    string MaximumTimestamp,
    string Status,
    string EvidenceClass,
    string SemanticClaim);

public sealed record AnimationBankCatalogTimestampAuditSummary(
    int BankCount,
    int CatalogRows,
    int DisplayDateParseableRows,
    int ExactTimestampDateRows,
    int TimestampDateMismatchRows,
    int InvalidTimestampRows,
    int BanksWithExactTimestampDate,
    string InferredUtcOffsetMinutes,
    string Status,
    string DirectTimingBinding,
    string OriginalSemantics);

/// <summary>
/// Checks whether the numeric catalog timestamp is reproduced by the calendar
/// text on the same source line. This is catalog metadata evidence only. It
/// does not interpret the value as animation duration, frame timing, pose,
/// rig, skinning, morph, material, UV, GPU or command data.
/// </summary>
public static class AnimationBankCatalogTimestampAudit
{
    public const string ReportHeader =
        "catalog_entry\tbank_name\tcatalog_rows\tdisplay_date_parseable_rows\texact_timestamp_date_rows\ttimestamp_date_mismatch_rows\tinvalid_timestamp_rows\tinferred_utc_offset_minutes\tminimum_timestamp\tmaximum_timestamp\tstatus\tevidence_class\tsemantic_claim";
    public const string SummaryHeader =
        "bank_count\tcatalog_rows\tdisplay_date_parseable_rows\texact_timestamp_date_rows\ttimestamp_date_mismatch_rows\tinvalid_timestamp_rows\tbanks_with_exact_timestamp_date\tinferred_utc_offset_minutes\tstatus\tdirect_timing_binding\toriginal_semantics";

    private const long MinimumUnixTimestamp = 315532800; // 1980-01-01 UTC.
    private const long MaximumUnixTimestamp = 4102444800; // 2100-01-01 UTC.

    public static IReadOnlyList<AnimationBankCatalogTimestampAuditRow> Audit(
        IEnumerable<(string CatalogEntry, AnimationBankCatalog Catalog)> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        return catalogs
            .Select(item => AuditOne(item.CatalogEntry, item.Catalog))
            .OrderBy(row => row.BankName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.CatalogEntry, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static AnimationBankCatalogTimestampAuditSummary Summarize(
        IReadOnlyList<AnimationBankCatalogTimestampAuditRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var offsets = rows
            .SelectMany(row => row.InferredUtcOffsetMinutes
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var complete = rows.Count > 0 && rows.All(row =>
            row.CatalogRows > 0 &&
            row.DisplayDateParseableRows == row.CatalogRows &&
            row.ExactTimestampDateRows == row.CatalogRows &&
            row.TimestampDateMismatchRows == 0 &&
            row.InvalidTimestampRows == 0);
        return new AnimationBankCatalogTimestampAuditSummary(
            rows.Count,
            rows.Sum(row => row.CatalogRows),
            rows.Sum(row => row.DisplayDateParseableRows),
            rows.Sum(row => row.ExactTimestampDateRows),
            rows.Sum(row => row.TimestampDateMismatchRows),
            rows.Sum(row => row.InvalidTimestampRows),
            rows.Count(row => row.ExactTimestampDateRows == row.CatalogRows && row.CatalogRows > 0),
            string.Join(',', offsets),
            complete ? "verified-structural" : "incomplete",
            "not-proven",
            "not-claimed");
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankCatalogTimestampAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            ReportHeader,
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            row.CatalogRows.ToString(CultureInfo.InvariantCulture),
            row.DisplayDateParseableRows.ToString(CultureInfo.InvariantCulture),
            row.ExactTimestampDateRows.ToString(CultureInfo.InvariantCulture),
            row.TimestampDateMismatchRows.ToString(CultureInfo.InvariantCulture),
            row.InvalidTimestampRows.ToString(CultureInfo.InvariantCulture),
            Tsv(row.InferredUtcOffsetMinutes),
            Tsv(row.MinimumTimestamp),
            Tsv(row.MaximumTimestamp),
            Tsv(row.Status),
            Tsv(row.EvidenceClass),
            Tsv(row.SemanticClaim),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        AnimationBankCatalogTimestampAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new[]
        {
            SummaryHeader,
            string.Join("\t", new[]
            {
                summary.BankCount.ToString(CultureInfo.InvariantCulture),
                summary.CatalogRows.ToString(CultureInfo.InvariantCulture),
                summary.DisplayDateParseableRows.ToString(CultureInfo.InvariantCulture),
                summary.ExactTimestampDateRows.ToString(CultureInfo.InvariantCulture),
                summary.TimestampDateMismatchRows.ToString(CultureInfo.InvariantCulture),
                summary.InvalidTimestampRows.ToString(CultureInfo.InvariantCulture),
                summary.BanksWithExactTimestampDate.ToString(CultureInfo.InvariantCulture),
                Tsv(summary.InferredUtcOffsetMinutes),
                Tsv(summary.Status),
                Tsv(summary.DirectTimingBinding),
                Tsv(summary.OriginalSemantics),
            }),
        };
        WriteLines(path, lines);
    }

    private static AnimationBankCatalogTimestampAuditRow AuditOne(
        string catalogEntry,
        AnimationBankCatalog catalog)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);
        var parseable = 0;
        var exact = 0;
        var mismatch = 0;
        var invalid = 0;
        var offsets = new HashSet<int>();
        var timestamps = new List<uint>();
        foreach (var entry in catalog.Files)
        {
            timestamps.Add(entry.Timestamp);
            if (!TryReadDisplayDate(entry, out var displayDate) ||
                entry.Timestamp < MinimumUnixTimestamp ||
                entry.Timestamp > MaximumUnixTimestamp)
            {
                invalid++;
                continue;
            }

            parseable++;
            var displayAsUtc = new DateTimeOffset(displayDate, TimeSpan.Zero);
            var localEpochSeconds = displayAsUtc.ToUnixTimeSeconds();
            var offsetSeconds = checked((long)entry.Timestamp - localEpochSeconds);
            if (offsetSeconds % 60 != 0 || Math.Abs(offsetSeconds) > 14 * 60 * 60)
            {
                mismatch++;
                continue;
            }

            // The source-line date is local time: actual UTC epoch minus the
            // same wall-clock value interpreted as UTC is the negated local
            // UTC offset (for example, +7 hours means UTC-07:00).
            offsets.Add(checked((int)(-offsetSeconds / 60)));
            exact++;
        }

        var complete = catalog.Files.Count > 0 &&
                       exact == catalog.Files.Count &&
                       invalid == 0 &&
                       mismatch == 0;
        return new AnimationBankCatalogTimestampAuditRow(
            catalogEntry,
            catalog.BankName,
            catalog.Files.Count,
            parseable,
            exact,
            mismatch,
            invalid,
            string.Join(',', offsets.OrderBy(value => value)),
            timestamps.Count == 0 ? string.Empty : timestamps.Min().ToString(CultureInfo.InvariantCulture),
            timestamps.Count == 0 ? string.Empty : timestamps.Max().ToString(CultureInfo.InvariantCulture),
            complete ? "verified-structural" : "incomplete",
            "catalog-source-line-date-correspondence",
            "timestamp/date metadata correspondence only; no animation timing claim");
    }

    private static bool TryReadDisplayDate(
        AnimationCatalogEntry entry,
        out DateTime displayDate)
    {
        displayDate = default;
        var timestampText = entry.Timestamp.ToString(CultureInfo.InvariantCulture);
        var timestampIndex = entry.SourceLine.IndexOf(timestampText, StringComparison.Ordinal);
        if (timestampIndex < 0)
        {
            return false;
        }

        var dateText = entry.SourceLine[(timestampIndex + timestampText.Length)..].Trim();
        return DateTime.TryParseExact(
            dateText,
            new[] { "MMM d yyyy HH:mm:ss", "MMM dd yyyy HH:mm:ss" },
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out displayDate);
    }

    private static string Tsv(string? value) => value is null
        ? string.Empty
        : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
