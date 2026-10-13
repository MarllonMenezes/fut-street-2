using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record AudioEventParallelRangeAuditRow(
    string EventPath,
    string HeaderPath,
    string DataPath,
    string EventName,
    uint EventStringOffset,
    int EventOccurrenceCount,
    int HeaderRowIndex,
    long HeaderOffset,
    long HeaderLength,
    long HeaderThirdColumn,
    long DataOffset,
    long DataLength,
    long DataThirdColumn,
    bool EventStringAnchorValid,
    bool HeaderRangeValid,
    bool DataRangeValid,
    bool ParallelRowIndexValid,
    bool DataStartsWithSchl,
    bool UniqueEventName,
    bool UniqueEventStringOffset,
    bool UniqueHeaderRow,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics,
    string Issue);

public sealed record AudioEventParallelRangeAuditSummary(
    int CandidateCount,
    int EventStringAnchorValidCount,
    int PositiveOccurrenceCount,
    int HeaderRangeValidCount,
    int DataRangeValidCount,
    int ParallelRowIndexValidCount,
    int DataSchlPrefixCount,
    int UniqueEventNameCount,
    int UniqueEventStringOffsetCount,
    int UniqueHeaderRowCount,
    int HeaderTableRows,
    int DataTableRows,
    bool HeaderTableStructureValid,
    bool DataTableStructureValid,
    int VerifiedRowCount,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(AudioEventParallelRangeAuditSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("candidate_count", summary.CandidateCount),
            Line("event_string_anchor_valid", summary.EventStringAnchorValidCount),
            Line("positive_occurrence_count", summary.PositiveOccurrenceCount),
            Line("header_range_valid", summary.HeaderRangeValidCount),
            Line("data_range_valid", summary.DataRangeValidCount),
            Line("parallel_row_index_valid", summary.ParallelRowIndexValidCount),
            Line("data_schl_prefix", summary.DataSchlPrefixCount),
            Line("unique_event_names", summary.UniqueEventNameCount),
            Line("unique_event_string_offsets", summary.UniqueEventStringOffsetCount),
            Line("unique_header_rows", summary.UniqueHeaderRowCount),
            Line("header_table_rows", summary.HeaderTableRows),
            Line("data_table_rows", summary.DataTableRows),
            Line("header_table_structure_valid", summary.HeaderTableStructureValid),
            Line("data_table_structure_valid", summary.DataTableStructureValid),
            Line("verified_rows", summary.VerifiedRowCount),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, AudioEventParallelRangeAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            BuildLines(summary),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Line<T>(string field, T value) =>
        $"{field}\t{Convert.ToString(value, CultureInfo.InvariantCulture)}";
}

/// <summary>
/// Rechecks the exact named EVT text anchor against the parallel PA offset
/// tables and their data companion. This is a structural range/prefix control;
/// it does not assign gameplay events, ABKC samples, playback semantics, or
/// any rendering/material/UV/skinning/rigging/animation meaning.
/// </summary>
public static class AudioEventParallelRangeAudit
{
    public const string Status = "verified-structural-named-parallel-range";
    public const string InconclusiveStatus = "inconclusive-structural-named-parallel-range";
    public const string Provenance =
        "exact-EVT-text-start+parallel-pcpahdr/pcpadat-row-join;range/prefix-control;no-event-sample-promotion";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static AudioEventParallelRangeAuditResult Audit(
        GameAssetCatalog catalog,
        Func<string, Stream> opener)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(opener);

        var candidates = catalog.AudioEventCandidates
            .OrderBy(candidate => candidate.EventPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.Candidate.EventStringOffset)
            .ThenBy(candidate => candidate.Candidate.HeaderRowIndex)
            .ThenBy(candidate => candidate.Candidate.EventName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (candidates.Length == 0)
        {
            return new AudioEventParallelRangeAuditResult(
                Array.Empty<AudioEventParallelRangeAuditRow>(),
                BuildSummary(
                    Array.Empty<AudioEventParallelRangeAuditRow>(),
                    0,
                    0,
                    false,
                    false,
                    0,
                    InconclusiveStatus));
        }

        var first = candidates[0];
        var pathSetValid = candidates.All(candidate =>
            string.Equals(candidate.EventPath, first.EventPath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.HeaderPath, first.HeaderPath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.DataPath, first.DataPath, StringComparison.OrdinalIgnoreCase));

        using var eventStream = opener(first.EventPath);
        using var headerOffsetStream = opener(ChangeExtension(first.HeaderPath, ".off"));
        using var headerStream = opener(first.HeaderPath);
        using var dataOffsetStream = opener(ChangeExtension(first.DataPath, ".off"));
        using var dataStream = opener(first.DataPath);

        var eventData = ReadBounded(eventStream);
        var eventEvidence = AudioAssetReader.InspectEventFile(eventData);
        var headerTable = AudioAssetReader.InspectOffsetTable(
            ReadBounded(headerOffsetStream),
            headerStream.Length);
        var dataTable = AudioAssetReader.InspectOffsetTable(
            ReadBounded(dataOffsetStream),
            dataStream.Length);
        var textRunsByOffset = eventEvidence.TextRuns
            .GroupBy(run => run.Offset)
            .ToDictionary(group => group.Key, group => group.ToArray());

        var nameCounts = candidates
            .GroupBy(candidate => candidate.Candidate.EventName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var stringOffsetCounts = candidates
            .GroupBy(candidate => candidate.Candidate.EventStringOffset)
            .ToDictionary(group => group.Key, group => group.Count());
        var headerRowCounts = candidates
            .GroupBy(candidate => candidate.Candidate.HeaderRowIndex)
            .ToDictionary(group => group.Key, group => group.Count());

        var rows = candidates
            .Select(candidate => BuildRow(
                candidate,
                textRunsByOffset,
                headerTable,
                dataTable,
                headerStream,
                dataStream,
                pathSetValid,
                nameCounts,
                stringOffsetCounts,
                headerRowCounts))
            .ToArray();
        var summary = BuildSummary(
            rows,
            headerTable.Rows.Count,
            dataTable.Rows.Count,
            headerTable.StructureValid,
            dataTable.StructureValid,
            candidates.Length,
            rows.All(row => row.Status == Status) ? Status : InconclusiveStatus);
        return new AudioEventParallelRangeAuditResult(rows, summary);
    }

    public static void WriteReport(
        string path,
        IEnumerable<AudioEventParallelRangeAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "event_path",
                "header_path",
                "data_path",
                "event_name",
                "event_string_offset",
                "event_occurrence_count",
                "header_row",
                "header_offset",
                "header_length",
                "header_third_column",
                "data_offset",
                "data_length",
                "data_third_column",
                "event_string_anchor_valid",
                "header_range_valid",
                "data_range_valid",
                "parallel_row_index_valid",
                "data_starts_with_schl",
                "unique_event_name",
                "unique_event_string_offset",
                "unique_header_row",
                "status",
                "provenance",
                "direct_selection_binding",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.EventPath),
            Escape(row.HeaderPath),
            Escape(row.DataPath),
            Escape(row.EventName),
            row.EventStringOffset.ToString(CultureInfo.InvariantCulture),
            row.EventOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            row.HeaderRowIndex.ToString(CultureInfo.InvariantCulture),
            row.HeaderOffset.ToString(CultureInfo.InvariantCulture),
            row.HeaderLength.ToString(CultureInfo.InvariantCulture),
            row.HeaderThirdColumn.ToString(CultureInfo.InvariantCulture),
            row.DataOffset.ToString(CultureInfo.InvariantCulture),
            row.DataLength.ToString(CultureInfo.InvariantCulture),
            row.DataThirdColumn.ToString(CultureInfo.InvariantCulture),
            row.EventStringAnchorValid.ToString(),
            row.HeaderRangeValid.ToString(),
            row.DataRangeValid.ToString(),
            row.ParallelRowIndexValid.ToString(),
            row.DataStartsWithSchl.ToString(),
            row.UniqueEventName.ToString(),
            row.UniqueEventStringOffset.ToString(),
            row.UniqueHeaderRow.ToString(),
            Escape(row.Status),
            Escape(row.Provenance),
            Escape(row.DirectSelectionBinding),
            Escape(row.OriginalSemantics),
            Escape(row.Issue),
        })));
        WriteLines(path, lines);
    }

    private static AudioEventParallelRangeAuditRow BuildRow(
        AudioEventCandidateAsset candidate,
        IReadOnlyDictionary<int, AudioEventTextRun[]> textRunsByOffset,
        AudioOffsetTableInspection headerTable,
        AudioOffsetTableInspection dataTable,
        Stream headerStream,
        Stream dataStream,
        bool pathSetValid,
        IReadOnlyDictionary<string, int> nameCounts,
        IReadOnlyDictionary<uint, int> stringOffsetCounts,
        IReadOnlyDictionary<int, int> headerRowCounts)
    {
        var named = candidate.Candidate;
        var headerRowIndexValid = named.HeaderRowIndex >= 0 && named.HeaderRowIndex < headerTable.Rows.Count;
        var dataRowIndexValid = named.HeaderRowIndex >= 0 && named.HeaderRowIndex < dataTable.Rows.Count;
        var headerRow = headerRowIndexValid ? headerTable.Rows[named.HeaderRowIndex] : null;
        var dataRow = dataRowIndexValid ? dataTable.Rows[named.HeaderRowIndex] : null;
        var candidateHeaderMatches = headerRow is not null &&
            SameRow(headerRow, named.HeaderRow);
        var candidateDataMatches = dataRow is not null &&
            named.ParallelDataRow is not null &&
            SameRow(dataRow, named.ParallelDataRow);
        var eventAnchorValid = named.EventStringOffset <= int.MaxValue &&
            textRunsByOffset.TryGetValue(checked((int)named.EventStringOffset), out var runs) &&
            runs.Any(run => string.Equals(run.Value, named.EventName, StringComparison.Ordinal));
        var headerRangeValid = candidateHeaderMatches &&
            IsRangeValid(headerRow!, headerStream.Length);
        var dataRangeValid = candidateDataMatches &&
            IsRangeValid(dataRow!, dataStream.Length);
        var dataStartsWithSchl = dataRangeValid && HasPrefix(dataStream, dataRow!.FirstColumn, "SCHl"u8);
        var parallelRowIndexValid = named.ParallelDataRow is not null &&
            dataRowIndexValid &&
            candidateDataMatches;
        var uniqueName = nameCounts.TryGetValue(named.EventName, out var nameCount) && nameCount == 1;
        var uniqueStringOffset = stringOffsetCounts.TryGetValue(named.EventStringOffset, out var offsetCount) && offsetCount == 1;
        var uniqueHeaderRow = headerRowCounts.TryGetValue(named.HeaderRowIndex, out var rowCount) && rowCount == 1;
        var valid =
            pathSetValid &&
            eventAnchorValid &&
            named.EventOccurrenceCount > 0 &&
            headerRangeValid &&
            dataRangeValid &&
            parallelRowIndexValid &&
            dataStartsWithSchl &&
            uniqueName &&
            uniqueStringOffset &&
            uniqueHeaderRow;
        var issue = valid
            ? string.Empty
            : string.Join(
                ";",
                new[]
                {
                    pathSetValid ? string.Empty : "source paths differ",
                    eventAnchorValid ? string.Empty : "EVT text anchor mismatch",
                    named.EventOccurrenceCount > 0 ? string.Empty : "non-positive event occurrence count",
                    headerRangeValid ? string.Empty : "header row/range mismatch",
                    dataRangeValid ? string.Empty : "data row/range mismatch",
                    parallelRowIndexValid ? string.Empty : "parallel data row index mismatch",
                    dataStartsWithSchl ? string.Empty : "data range does not start with SCHl",
                    uniqueName ? string.Empty : "event name is not unique",
                    uniqueStringOffset ? string.Empty : "event string offset is not unique",
                    uniqueHeaderRow ? string.Empty : "header row is not unique",
                }.Where(value => value.Length > 0));
        return new AudioEventParallelRangeAuditRow(
            candidate.EventPath,
            candidate.HeaderPath,
            candidate.DataPath,
            named.EventName,
            named.EventStringOffset,
            named.EventOccurrenceCount,
            named.HeaderRowIndex,
            named.HeaderRow.FirstColumn,
            named.HeaderRow.SecondColumn,
            named.HeaderRow.ThirdColumn,
            named.ParallelDataRow?.FirstColumn ?? -1,
            named.ParallelDataRow?.SecondColumn ?? 0,
            named.ParallelDataRow?.ThirdColumn ?? -1,
            eventAnchorValid,
            headerRangeValid,
            dataRangeValid,
            parallelRowIndexValid,
            dataStartsWithSchl,
            uniqueName,
            uniqueStringOffset,
            uniqueHeaderRow,
            valid ? Status : InconclusiveStatus,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics,
            issue);
    }

    private static AudioEventParallelRangeAuditSummary BuildSummary(
        IReadOnlyList<AudioEventParallelRangeAuditRow> rows,
        int headerTableRows,
        int dataTableRows,
        bool headerTableStructureValid,
        bool dataTableStructureValid,
        int candidateCount,
        string status)
    {
        return new AudioEventParallelRangeAuditSummary(
            candidateCount,
            rows.Count(row => row.EventStringAnchorValid),
            rows.Count(row => row.EventOccurrenceCount > 0),
            rows.Count(row => row.HeaderRangeValid),
            rows.Count(row => row.DataRangeValid),
            rows.Count(row => row.ParallelRowIndexValid),
            rows.Count(row => row.DataStartsWithSchl),
            rows.Count(row => row.UniqueEventName),
            rows.Count(row => row.UniqueEventStringOffset),
            rows.Count(row => row.UniqueHeaderRow),
            headerTableRows,
            dataTableRows,
            headerTableStructureValid,
            dataTableStructureValid,
            rows.Count(row => row.Status == Status),
            status,
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
    }

    private static bool SameRow(AudioOffsetRow left, AudioOffsetRow right) =>
        left.FirstColumn == right.FirstColumn &&
        left.SecondColumn == right.SecondColumn &&
        left.ThirdColumn == right.ThirdColumn;

    private static bool IsRangeValid(AudioOffsetRow row, long streamLength) =>
        row.FirstColumn >= 0 &&
        row.SecondColumn >= 0 &&
        row.FirstColumn <= streamLength &&
        row.SecondColumn <= streamLength - row.FirstColumn;

    private static bool HasPrefix(Stream stream, long offset, ReadOnlySpan<byte> prefix)
    {
        if (offset < 0 || offset > stream.Length - prefix.Length)
        {
            return false;
        }

        stream.Seek(offset, SeekOrigin.Begin);
        Span<byte> buffer = stackalloc byte[4];
        stream.ReadExactly(buffer);
        return buffer.SequenceEqual(prefix);
    }

    private static byte[] ReadBounded(Stream stream)
    {
        if (stream.Length > 16 * 1024 * 1024)
        {
            throw new InvalidDataException("Audio structural companion exceeds the bounded audit input size.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return data;
    }

    private static string ChangeExtension(string path, string extension)
    {
        var changed = Path.ChangeExtension(path, extension);
        return changed ?? throw new InvalidDataException($"Cannot derive companion path for {path}.");
    }

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            fullPath,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}

public sealed record AudioEventParallelRangeAuditResult(
    IReadOnlyList<AudioEventParallelRangeAuditRow> Rows,
    AudioEventParallelRangeAuditSummary Summary);
