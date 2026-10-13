using System.Globalization;

namespace FifaStreetPc;

public sealed record AnimationBankCatalogPayloadNameMatch(
    string CatalogEntry,
    string BankName,
    string FileName,
    uint Timestamp,
    string MatchKind,
    int MatchCount,
    string PayloadOffsets,
    string PayloadValues);

public sealed record AnimationBankCatalogPayloadNameSummary(
    string BankName,
    int CatalogRows,
    int DistinctCatalogNames,
    int PayloadStringCount,
    int ExactRows,
    int TaggedRows,
    int PrefixRows,
    int MissingRows,
    int AmbiguousRows,
    int ExactOccurrences,
    int TaggedOccurrences,
    string MissingNamesSha256);

public readonly record struct AnimationPayloadAsciiRun(int Offset, string Value);

/// <summary>
/// Cross-checks catalog file names against printable strings in the validated
/// animation-bank .data section. A matching string is payload identity evidence
/// only; it is not a record-boundary, frame, pose, rig, timing, skinning or
/// decoder binding.
/// </summary>
public static class AnimationBankCatalogPayloadNameAudit
{
    public static IReadOnlyList<AnimationPayloadAsciiRun> ExtractAsciiRuns(
        ReadOnlySpan<byte> payload,
        int minimumLength = 3)
    {
        if (minimumLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumLength));
        }

        var runs = new List<AnimationPayloadAsciiRun>();
        var start = -1;
        for (var index = 0; index <= payload.Length; index++)
        {
            var printable = index < payload.Length &&
                payload[index] >= 0x20 && payload[index] <= 0x7E;
            if (printable)
            {
                start = start < 0 ? index : start;
                continue;
            }

            if (start >= 0)
            {
                var length = index - start;
                if (length >= minimumLength)
                {
                    runs.Add(new AnimationPayloadAsciiRun(
                        start,
                        System.Text.Encoding.ASCII.GetString(payload.Slice(start, length))));
                }

                start = -1;
            }
        }

        return runs;
    }

    public static IReadOnlyList<AnimationBankCatalogPayloadNameMatch> Audit(
        string catalogEntry,
        AnimationBankCatalog catalog,
        ReadOnlySpan<byte> dataPayload,
        int minimumPayloadStringLength = 3)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(catalogEntry);
        ArgumentNullException.ThrowIfNull(catalog);

        var runs = ExtractAsciiRuns(dataPayload, minimumPayloadStringLength);
        return catalog.Files
            .Select(file => BuildMatch(catalogEntry, catalog.BankName, file, runs))
            .ToArray();
    }

    public static AnimationBankCatalogPayloadNameSummary Summarize(
        string bankName,
        IEnumerable<AnimationBankCatalogPayloadNameMatch> rows,
        int payloadStringCount)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bankName);
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var missingNames = materialized
            .Where(row => string.Equals(row.MatchKind, "missing", StringComparison.Ordinal))
            .Select(row => row.FileName)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var missingText = string.Join("\n", missingNames);
        return new AnimationBankCatalogPayloadNameSummary(
            bankName,
            materialized.Length,
            materialized.Select(row => row.FileName).Distinct(StringComparer.Ordinal).Count(),
            payloadStringCount,
            materialized.Count(row => string.Equals(row.MatchKind, "exact", StringComparison.Ordinal)),
            materialized.Count(row => string.Equals(row.MatchKind, "tagged", StringComparison.Ordinal)),
            materialized.Count(row => string.Equals(row.MatchKind, "prefix", StringComparison.Ordinal)),
            materialized.Count(row => string.Equals(row.MatchKind, "missing", StringComparison.Ordinal)),
            materialized.Count(row => row.MatchCount > 1),
            materialized
                .Where(row => string.Equals(row.MatchKind, "exact", StringComparison.Ordinal))
                .Sum(row => row.MatchCount),
            materialized
                .Where(row => string.Equals(row.MatchKind, "tagged", StringComparison.Ordinal))
                .Sum(row => row.MatchCount),
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(missingText))));
    }

    public static void WriteReport(
        string path,
        IEnumerable<AnimationBankCatalogPayloadNameMatch> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "file_name",
                "timestamp",
                "match_kind",
                "match_count",
                "payload_offsets",
                "payload_values",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Tsv(row.CatalogEntry),
            Tsv(row.BankName),
            Tsv(row.FileName),
            row.Timestamp.ToString(CultureInfo.InvariantCulture),
            row.MatchKind,
            row.MatchCount.ToString(CultureInfo.InvariantCulture),
            Tsv(row.PayloadOffsets),
            Tsv(row.PayloadValues),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        IEnumerable<AnimationBankCatalogPayloadNameSummary> summaries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summaries);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "bank_name",
                "catalog_rows",
                "distinct_catalog_names",
                "payload_string_count",
                "exact_rows",
                "tagged_rows",
                "prefix_rows",
                "missing_rows",
                "ambiguous_rows",
                "exact_occurrences",
                "tagged_occurrences",
                "missing_names_sha256",
            }),
        };
        lines.AddRange(summaries.Select(summary => string.Join("\t", new[]
        {
            Tsv(summary.BankName),
            summary.CatalogRows.ToString(CultureInfo.InvariantCulture),
            summary.DistinctCatalogNames.ToString(CultureInfo.InvariantCulture),
            summary.PayloadStringCount.ToString(CultureInfo.InvariantCulture),
            summary.ExactRows.ToString(CultureInfo.InvariantCulture),
            summary.TaggedRows.ToString(CultureInfo.InvariantCulture),
            summary.PrefixRows.ToString(CultureInfo.InvariantCulture),
            summary.MissingRows.ToString(CultureInfo.InvariantCulture),
            summary.AmbiguousRows.ToString(CultureInfo.InvariantCulture),
            summary.ExactOccurrences.ToString(CultureInfo.InvariantCulture),
            summary.TaggedOccurrences.ToString(CultureInfo.InvariantCulture),
            summary.MissingNamesSha256,
        })));
        WriteLines(path, lines);
    }

    private static AnimationBankCatalogPayloadNameMatch BuildMatch(
        string catalogEntry,
        string bankName,
        AnimationCatalogEntry file,
        IReadOnlyList<AnimationPayloadAsciiRun> runs)
    {
        var exact = runs
            .Where(run => string.Equals(run.Value, file.Name, StringComparison.Ordinal))
            .ToArray();
        var tagged = runs
            .Where(run => IsTaggedName(run.Value, file.Name))
            .ToArray();
        var prefix = runs
            .Where(run => run.Value.StartsWith(file.Name, StringComparison.Ordinal))
            .ToArray();
        var matches = exact.Length > 0 ? exact : tagged.Length > 0 ? tagged : prefix;
        var matchKind = exact.Length > 0
            ? "exact"
            : tagged.Length > 0
                ? "tagged"
                : prefix.Length > 0
                    ? "prefix"
                    : "missing";
        return new AnimationBankCatalogPayloadNameMatch(
            catalogEntry,
            bankName,
            file.Name,
            file.Timestamp,
            matchKind,
            matches.Length,
            string.Join(",", matches.Select(run => $"0x{run.Offset:X}")),
            string.Join("|", matches.Select(run => run.Value)));
    }

    private static bool IsTaggedName(string value, string fileName) =>
        string.Equals(value, fileName + "_e", StringComparison.Ordinal) ||
        string.Equals(value, fileName + "_r", StringComparison.Ordinal);

    private static string Tsv(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static void WriteLines(string path, IEnumerable<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            lines,
            new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
