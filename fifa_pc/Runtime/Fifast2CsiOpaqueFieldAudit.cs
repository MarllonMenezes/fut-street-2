using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record Fifast2CsiOpaqueFieldRow(
    int CsiRecordIndex,
    string CsiName,
    uint CsiOpaqueWord,
    bool CsiValid,
    int PcpahdrColumn1MatchCount,
    int PcpahdrColumn2MatchCount,
    int PcpahdrColumn3MatchCount,
    int PcpahdrRowOrdinalMatchCount,
    int PcpadatColumn1MatchCount,
    int PcpadatColumn2MatchCount,
    int PcpadatColumn3MatchCount,
    int PcpadatRowOrdinalMatchCount,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record Fifast2CsiOpaqueFieldAuditSummary(
    int CsiRecordCount,
    int CsiValidRecordCount,
    int PcpahdrRowCount,
    bool PcpahdrTableStructureValid,
    bool PcpahdrPartitionCoverage,
    int PcpadatRowCount,
    bool PcpadatTableStructureValid,
    bool PcpadatPartitionCoverage,
    int PcpahdrAnyColumnMatchCount,
    int PcpahdrColumn1MatchCount,
    int PcpahdrColumn2MatchCount,
    int PcpahdrColumn3MatchCount,
    int PcpahdrRowOrdinalMatchCount,
    int PcpadatAnyColumnMatchCount,
    int PcpadatColumn1MatchCount,
    int PcpadatColumn2MatchCount,
    int PcpadatColumn3MatchCount,
    int PcpadatRowOrdinalMatchCount,
    string OpaqueWordSemantics,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(Fifast2CsiOpaqueFieldAuditSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("csi_record_count", summary.CsiRecordCount),
            Line("csi_valid_record_count", summary.CsiValidRecordCount),
            Line("pcpahdr_row_count", summary.PcpahdrRowCount),
            Line("pcpahdr_table_structure_valid", summary.PcpahdrTableStructureValid),
            Line("pcpahdr_partition_coverage", summary.PcpahdrPartitionCoverage),
            Line("pcpadat_row_count", summary.PcpadatRowCount),
            Line("pcpadat_table_structure_valid", summary.PcpadatTableStructureValid),
            Line("pcpadat_partition_coverage", summary.PcpadatPartitionCoverage),
            Line("pcpahdr_any_column_match_count", summary.PcpahdrAnyColumnMatchCount),
            Line("pcpahdr_column1_match_count", summary.PcpahdrColumn1MatchCount),
            Line("pcpahdr_column2_match_count", summary.PcpahdrColumn2MatchCount),
            Line("pcpahdr_column3_match_count", summary.PcpahdrColumn3MatchCount),
            Line("pcpahdr_row_ordinal_match_count", summary.PcpahdrRowOrdinalMatchCount),
            Line("pcpadat_any_column_match_count", summary.PcpadatAnyColumnMatchCount),
            Line("pcpadat_column1_match_count", summary.PcpadatColumn1MatchCount),
            Line("pcpadat_column2_match_count", summary.PcpadatColumn2MatchCount),
            Line("pcpadat_column3_match_count", summary.PcpadatColumn3MatchCount),
            Line("pcpadat_row_ordinal_match_count", summary.PcpadatRowOrdinalMatchCount),
            Line("opaque_word_semantics", summary.OpaqueWordSemantics),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, Fifast2CsiOpaqueFieldAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        WriteLines(path, BuildLines(summary));
    }

    private static string Line<T>(string field, T value) =>
        $"{field}\t{Convert.ToString(value, CultureInfo.InvariantCulture)}";

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
}

public sealed record Fifast2CsiOpaqueFieldAuditResult(
    IReadOnlyList<Fifast2CsiOpaqueFieldRow> Rows,
    Fifast2CsiOpaqueFieldAuditSummary Summary);

/// <summary>
/// Compares CSI opaque words with numeric columns and row ordinals exposed by
/// the independently parsed .off tables. Column names are positional only;
/// this audit does not assign offset, length, header, data, sample or playback
/// semantics to any value.
/// </summary>
public static class Fifast2CsiOpaqueFieldAudit
{
    public const string Status = "verified-structural-csi-opaque-off-field-control";
    public const string Provenance =
        "FIFAST2.csi+pcpahdr.off+pcpadat.off;numeric-column-equality-and-row-ordinal-control;no-binding";
    public const string OpaqueWordSemantics = "not-decoded";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";

    public static Fifast2CsiOpaqueFieldAuditResult Analyze(
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
        var validStructure =
            csiAudit.Summary.ValidRecordCount == csiAudit.Summary.CandidateRecordCount &&
            pcpahdrTable.StructureValid &&
            pcpadatTable.StructureValid;
        var summary = new Fifast2CsiOpaqueFieldAuditSummary(
            csiAudit.Summary.CandidateRecordCount,
            csiAudit.Summary.ValidRecordCount,
            pcpahdrTable.Rows.Count,
            pcpahdrTable.StructureValid,
            IsPartitionCoverage(pcpahdrTable, pcpahdrCompanionLength),
            pcpadatTable.Rows.Count,
            pcpadatTable.StructureValid,
            IsPartitionCoverage(pcpadatTable, pcpadatCompanionLength),
            rows.Count(row => HasPcpahdrColumnMatch(row)),
            rows.Count(row => row.PcpahdrColumn1MatchCount > 0),
            rows.Count(row => row.PcpahdrColumn2MatchCount > 0),
            rows.Count(row => row.PcpahdrColumn3MatchCount > 0),
            rows.Count(row => row.PcpahdrRowOrdinalMatchCount > 0),
            rows.Count(row => HasPcpadatColumnMatch(row)),
            rows.Count(row => row.PcpadatColumn1MatchCount > 0),
            rows.Count(row => row.PcpadatColumn2MatchCount > 0),
            rows.Count(row => row.PcpadatColumn3MatchCount > 0),
            rows.Count(row => row.PcpadatRowOrdinalMatchCount > 0),
            OpaqueWordSemantics,
            validStructure ? Status : "partial-structural-csi-opaque-off-field-control",
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new Fifast2CsiOpaqueFieldAuditResult(rows, summary);
    }

    public static void WriteReport(string path, IEnumerable<Fifast2CsiOpaqueFieldRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            "csi_record_index\tcsi_name\tcsi_opaque_word\tcsi_valid\tpcpahdr_column1_match_count\tpcpahdr_column2_match_count\tpcpahdr_column3_match_count\tpcpahdr_row_ordinal_match_count\tpcpadat_column1_match_count\tpcpadat_column2_match_count\tpcpadat_column3_match_count\tpcpadat_row_ordinal_match_count\tstatus\tdirect_selection_binding\toriginal_semantics",
        };
        lines.AddRange(rows.Select(row => string.Join('\t', new[]
        {
            row.CsiRecordIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.CsiName),
            $"0x{row.CsiOpaqueWord:X8}",
            row.CsiValid.ToString(),
            row.PcpahdrColumn1MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpahdrColumn2MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpahdrColumn3MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpahdrRowOrdinalMatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpadatColumn1MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpadatColumn2MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpadatColumn3MatchCount.ToString(CultureInfo.InvariantCulture),
            row.PcpadatRowOrdinalMatchCount.ToString(CultureInfo.InvariantCulture),
            row.Status,
            row.DirectSelectionBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    private static Fifast2CsiOpaqueFieldRow BuildRow(
        Fifast2CsiNameRecord csiRow,
        IReadOnlyList<AudioOffsetRow> pcpahdrRows,
        IReadOnlyList<AudioOffsetRow> pcpadatRows)
    {
        var pcpahdrColumn1 = pcpahdrRows.Count(row => row.FirstColumn == csiRow.OpaqueWord);
        var pcpahdrColumn2 = pcpahdrRows.Count(row => row.SecondColumn == csiRow.OpaqueWord);
        var pcpahdrColumn3 = pcpahdrRows.Count(row => row.ThirdColumn == csiRow.OpaqueWord);
        var pcpahdrOrdinal = pcpahdrRows.Count(row => row.LineNumber - 1 == csiRow.OpaqueWord);
        var pcpadatColumn1 = pcpadatRows.Count(row => row.FirstColumn == csiRow.OpaqueWord);
        var pcpadatColumn2 = pcpadatRows.Count(row => row.SecondColumn == csiRow.OpaqueWord);
        var pcpadatColumn3 = pcpadatRows.Count(row => row.ThirdColumn == csiRow.OpaqueWord);
        var pcpadatOrdinal = pcpadatRows.Count(row => row.LineNumber - 1 == csiRow.OpaqueWord);
        var any = pcpahdrColumn1 > 0 || pcpahdrColumn2 > 0 || pcpahdrColumn3 > 0 ||
                  pcpahdrOrdinal > 0 || pcpadatColumn1 > 0 || pcpadatColumn2 > 0 ||
                  pcpadatColumn3 > 0 || pcpadatOrdinal > 0;
        return new Fifast2CsiOpaqueFieldRow(
            csiRow.RecordIndex,
            csiRow.Name,
            csiRow.OpaqueWord,
            csiRow.Valid,
            pcpahdrColumn1,
            pcpahdrColumn2,
            pcpahdrColumn3,
            pcpahdrOrdinal,
            pcpadatColumn1,
            pcpadatColumn2,
            pcpadatColumn3,
            pcpadatOrdinal,
            any ? "off-column-or-row-ordinal-coincidence" : "no-off-column-or-row-ordinal-coincidence",
            DirectSelectionBinding,
            OriginalSemantics);
    }

    private static bool HasPcpahdrColumnMatch(Fifast2CsiOpaqueFieldRow row) =>
        row.PcpahdrColumn1MatchCount > 0 ||
        row.PcpahdrColumn2MatchCount > 0 ||
        row.PcpahdrColumn3MatchCount > 0;

    private static bool HasPcpadatColumnMatch(Fifast2CsiOpaqueFieldRow row) =>
        row.PcpadatColumn1MatchCount > 0 ||
        row.PcpadatColumn2MatchCount > 0 ||
        row.PcpadatColumn3MatchCount > 0;

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

            expected = checked(row.FirstColumn + row.SecondColumn);
        }

        return expected == companionLength;
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
