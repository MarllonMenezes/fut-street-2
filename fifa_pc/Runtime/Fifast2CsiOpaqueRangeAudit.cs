using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record Fifast2CsiOpaqueRangeRow(
    int CsiRecordIndex,
    string CsiName,
    uint CsiOpaqueWord,
    bool CsiValid,
    int PcpahdrContainingRowCount,
    int PcpahdrFirstContainingRow,
    bool PcpahdrExactStart,
    bool PcpahdrExactEnd,
    int PcpadatContainingRowCount,
    int PcpadatFirstContainingRow,
    bool PcpadatExactStart,
    bool PcpadatExactEnd,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record Fifast2CsiOpaqueRangeAuditSummary(
    int CsiRecordCount,
    int CsiValidRecordCount,
    int PcpahdrRowCount,
    long PcpahdrCompanionLength,
    bool PcpahdrTableStructureValid,
    bool PcpahdrPartitionCoverage,
    int PcpadatRowCount,
    long PcpadatCompanionLength,
    bool PcpadatTableStructureValid,
    bool PcpadatPartitionCoverage,
    int CsiOpaqueWithinPcpahdrCount,
    int CsiOpaquePcpahdrExactStartCount,
    int CsiOpaquePcpahdrExactEndCount,
    int CsiOpaqueWithinPcpadatCount,
    int CsiOpaquePcpadatExactStartCount,
    int CsiOpaquePcpadatExactEndCount,
    int DistinctPcpahdrContainingRows,
    int DistinctPcpadatContainingRows,
    string OpaqueWordSemantics,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(Fifast2CsiOpaqueRangeAuditSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("csi_record_count", summary.CsiRecordCount),
            Line("csi_valid_record_count", summary.CsiValidRecordCount),
            Line("pcpahdr_row_count", summary.PcpahdrRowCount),
            Line("pcpahdr_companion_length", summary.PcpahdrCompanionLength),
            Line("pcpahdr_table_structure_valid", summary.PcpahdrTableStructureValid),
            Line("pcpahdr_partition_coverage", summary.PcpahdrPartitionCoverage),
            Line("pcpadat_row_count", summary.PcpadatRowCount),
            Line("pcpadat_companion_length", summary.PcpadatCompanionLength),
            Line("pcpadat_table_structure_valid", summary.PcpadatTableStructureValid),
            Line("pcpadat_partition_coverage", summary.PcpadatPartitionCoverage),
            Line("csi_opaque_within_pcpahdr_count", summary.CsiOpaqueWithinPcpahdrCount),
            Line("csi_opaque_pcpahdr_exact_start_count", summary.CsiOpaquePcpahdrExactStartCount),
            Line("csi_opaque_pcpahdr_exact_end_count", summary.CsiOpaquePcpahdrExactEndCount),
            Line("csi_opaque_within_pcpadat_count", summary.CsiOpaqueWithinPcpadatCount),
            Line("csi_opaque_pcpadat_exact_start_count", summary.CsiOpaquePcpadatExactStartCount),
            Line("csi_opaque_pcpadat_exact_end_count", summary.CsiOpaquePcpadatExactEndCount),
            Line("distinct_pcpahdr_containing_rows", summary.DistinctPcpahdrContainingRows),
            Line("distinct_pcpadat_containing_rows", summary.DistinctPcpadatContainingRows),
            Line("opaque_word_semantics", summary.OpaqueWordSemantics),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, Fifast2CsiOpaqueRangeAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
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

public sealed record Fifast2CsiOpaqueRangeAuditResult(
    IReadOnlyList<Fifast2CsiOpaqueRangeRow> Rows,
    Fifast2CsiOpaqueRangeAuditSummary Summary);

/// <summary>
/// Compares CSI opaque words with independently parsed .off ranges only.
/// A containing range or boundary coincidence is reported as structural
/// evidence and never promoted to a header, sample, bank or playback binding.
/// </summary>
public static class Fifast2CsiOpaqueRangeAudit
{
    public const string Status = "verified-structural-csi-opaque-range-boundary-control";
    public const string Provenance =
        "FIFAST2.csi+pcpahdr.off/pcpahdr.big+pcpadat.off/pcpadat.big;no-execution;no-binding";
    public const string OpaqueWordSemantics = "not-decoded";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static Fifast2CsiOpaqueRangeAuditResult Analyze(
        ReadOnlySpan<byte> csiData,
        ReadOnlySpan<byte> pcpahdrOffData,
        long pcpahdrCompanionLength,
        ReadOnlySpan<byte> pcpadatOffData,
        long pcpadatCompanionLength)
    {
        if (pcpahdrCompanionLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pcpahdrCompanionLength));
        }

        if (pcpadatCompanionLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pcpadatCompanionLength));
        }

        var csiAudit = Fifast2CsiReader.Audit(csiData);
        var pcpahdrTable = AudioAssetReader.InspectOffsetTable(pcpahdrOffData, pcpahdrCompanionLength);
        var pcpadatTable = AudioAssetReader.InspectOffsetTable(pcpadatOffData, pcpadatCompanionLength);
        var rows = csiAudit.Rows
            .Select(row => BuildRow(row, pcpahdrTable.Rows, pcpadatTable.Rows))
            .ToArray();

        var pcpahdrContainingRows = rows
            .Where(row => row.PcpahdrFirstContainingRow >= 0)
            .Select(row => row.PcpahdrFirstContainingRow)
            .ToHashSet();
        var pcpadatContainingRows = rows
            .Where(row => row.PcpadatFirstContainingRow >= 0)
            .Select(row => row.PcpadatFirstContainingRow)
            .ToHashSet();
        var pcpahdrPartitionCoverage = IsPartitionCoverage(pcpahdrTable, pcpahdrCompanionLength);
        var pcpadatPartitionCoverage = IsPartitionCoverage(pcpadatTable, pcpadatCompanionLength);
        var validStructure =
            csiAudit.Summary.ValidRecordCount == csiAudit.Summary.CandidateRecordCount &&
            pcpahdrTable.StructureValid &&
            pcpadatTable.StructureValid;
        var summary = new Fifast2CsiOpaqueRangeAuditSummary(
            csiAudit.Summary.CandidateRecordCount,
            csiAudit.Summary.ValidRecordCount,
            pcpahdrTable.Rows.Count,
            pcpahdrCompanionLength,
            pcpahdrTable.StructureValid,
            pcpahdrPartitionCoverage,
            pcpadatTable.Rows.Count,
            pcpadatCompanionLength,
            pcpadatTable.StructureValid,
            pcpadatPartitionCoverage,
            rows.Count(row => row.PcpahdrContainingRowCount > 0),
            rows.Count(row => row.PcpahdrExactStart),
            rows.Count(row => row.PcpahdrExactEnd),
            rows.Count(row => row.PcpadatContainingRowCount > 0),
            rows.Count(row => row.PcpadatExactStart),
            rows.Count(row => row.PcpadatExactEnd),
            pcpahdrContainingRows.Count,
            pcpadatContainingRows.Count,
            OpaqueWordSemantics,
            validStructure ? Status : "partial-structural-csi-opaque-range-boundary-control",
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new Fifast2CsiOpaqueRangeAuditResult(rows, summary);
    }

    public static void WriteReport(string path, IEnumerable<Fifast2CsiOpaqueRangeRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            "csi_record_index\tcsi_name\tcsi_opaque_word\tcsi_valid\tpcpahdr_containing_row_count\tpcpahdr_first_containing_row\tpcpahdr_exact_start\tpcpahdr_exact_end\tpcpadat_containing_row_count\tpcpadat_first_containing_row\tpcpadat_exact_start\tpcpadat_exact_end\tstatus\tdirect_selection_binding\toriginal_semantics",
        };
        lines.AddRange(rows.Select(row => string.Join('\t', new[]
        {
            row.CsiRecordIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.CsiName),
            $"0x{row.CsiOpaqueWord:X8}",
            row.CsiValid.ToString(),
            row.PcpahdrContainingRowCount.ToString(CultureInfo.InvariantCulture),
            row.PcpahdrFirstContainingRow.ToString(CultureInfo.InvariantCulture),
            row.PcpahdrExactStart.ToString(),
            row.PcpahdrExactEnd.ToString(),
            row.PcpadatContainingRowCount.ToString(CultureInfo.InvariantCulture),
            row.PcpadatFirstContainingRow.ToString(CultureInfo.InvariantCulture),
            row.PcpadatExactStart.ToString(),
            row.PcpadatExactEnd.ToString(),
            row.Status,
            row.DirectSelectionBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    private static Fifast2CsiOpaqueRangeRow BuildRow(
        Fifast2CsiNameRecord csiRow,
        IReadOnlyList<AudioOffsetRow> pcpahdrRows,
        IReadOnlyList<AudioOffsetRow> pcpadatRows)
    {
        var pcpahdrMatches = FindContainingRows(pcpahdrRows, csiRow.OpaqueWord);
        var pcpadatMatches = FindContainingRows(pcpadatRows, csiRow.OpaqueWord);
        var pcpahdrExactStart = pcpahdrRows.Any(row => row.FirstColumn == csiRow.OpaqueWord);
        var pcpahdrExactEnd = pcpahdrRows.Any(row => End(row) == csiRow.OpaqueWord);
        var pcpadatExactStart = pcpadatRows.Any(row => row.FirstColumn == csiRow.OpaqueWord);
        var pcpadatExactEnd = pcpadatRows.Any(row => End(row) == csiRow.OpaqueWord);
        return new Fifast2CsiOpaqueRangeRow(
            csiRow.RecordIndex,
            csiRow.Name,
            csiRow.OpaqueWord,
            csiRow.Valid,
            pcpahdrMatches.Count,
            pcpahdrMatches.Count == 0 ? -1 : pcpahdrMatches[0].LineNumber,
            pcpahdrExactStart,
            pcpahdrExactEnd,
            pcpadatMatches.Count,
            pcpadatMatches.Count == 0 ? -1 : pcpadatMatches[0].LineNumber,
            pcpadatExactStart,
            pcpadatExactEnd,
            pcpahdrMatches.Count > 0 || pcpadatMatches.Count > 0
                ? "range-membership-only"
                : "outside-offset-ranges",
            DirectSelectionBinding,
            OriginalSemantics);
    }

    private static IReadOnlyList<AudioOffsetRow> FindContainingRows(
        IReadOnlyList<AudioOffsetRow> rows,
        uint value)
    {
        var valueAsLong = (long)value;
        return rows
            .Where(row => row.FirstColumn <= valueAsLong && End(row) > valueAsLong)
            .ToArray();
    }

    private static bool IsPartitionCoverage(AudioOffsetTableInspection table, long companionLength)
    {
        if (!table.StructureValid || table.Rows.Count == 0 || !table.FirstRowStartsAtZero)
        {
            return false;
        }

        var expected = 0L;
        foreach (var row in table.Rows)
        {
            if (row.FirstColumn != expected)
            {
                return false;
            }

            expected = End(row);
        }

        return expected == companionLength;
    }

    private static long End(AudioOffsetRow row) => checked(row.FirstColumn + row.SecondColumn);

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
