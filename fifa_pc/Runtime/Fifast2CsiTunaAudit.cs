using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record Fifast2CsiTunaCrosscheckRow(
    int CsiRecordIndex,
    string CsiName,
    uint CsiOpaqueWord,
    bool TunaNamePresent,
    uint? TunaHashWord,
    uint ComputedHashWord,
    bool ComputedHashMatchesTuna,
    bool CsiOpaqueMatchesTunaHash,
    bool OpaqueWordWithinPcpahdrAsset,
    string Status,
    string DirectSelectionBinding,
    string OriginalSemantics);

public sealed record Fifast2CsiTunaAuditSummary(
    string TunaArchiveMagic,
    uint TunaArchiveSize,
    uint TunaArchiveFileCount,
    uint TunaArchiveIndexEnd,
    bool TunaArchiveSizeMatches,
    string TunaEntryName,
    int TunaRowCount,
    int TunaHashMatchCount,
    int TunaUniqueNameCount,
    int CsiRecordCount,
    int CsiValidRecordCount,
    int CsiNameOverlapCount,
    int CsiNamesMissingTunaCount,
    int TunaNamesMissingCsiCount,
    int CsiRowsWithTunaHashMatch,
    int CsiOpaqueHashMatchCount,
    int CsiOpaqueWordsWithinPcpahdrAsset,
    string HashFunctionCandidate,
    string OpaqueWordSemantics,
    string Status,
    string Provenance,
    string DirectSelectionBinding,
    string OriginalSemantics)
{
    public const string Header = "field\tvalue";

    public static IReadOnlyList<string> BuildLines(Fifast2CsiTunaAuditSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new[]
        {
            Header,
            Line("tuna_archive_magic", summary.TunaArchiveMagic),
            Line("tuna_archive_size", summary.TunaArchiveSize),
            Line("tuna_archive_file_count", summary.TunaArchiveFileCount),
            Line("tuna_archive_index_end", summary.TunaArchiveIndexEnd),
            Line("tuna_archive_size_matches", summary.TunaArchiveSizeMatches),
            Line("tuna_entry_name", summary.TunaEntryName),
            Line("tuna_row_count", summary.TunaRowCount),
            Line("tuna_hash_match_count", summary.TunaHashMatchCount),
            Line("tuna_unique_name_count", summary.TunaUniqueNameCount),
            Line("csi_record_count", summary.CsiRecordCount),
            Line("csi_valid_record_count", summary.CsiValidRecordCount),
            Line("csi_name_overlap_count", summary.CsiNameOverlapCount),
            Line("csi_names_missing_tuna_count", summary.CsiNamesMissingTunaCount),
            Line("tuna_names_missing_csi_count", summary.TunaNamesMissingCsiCount),
            Line("csi_rows_with_tuna_hash_match", summary.CsiRowsWithTunaHashMatch),
            Line("csi_opaque_hash_match_count", summary.CsiOpaqueHashMatchCount),
            Line("csi_opaque_words_within_pcpahdr_asset", summary.CsiOpaqueWordsWithinPcpahdrAsset),
            Line("hash_function_candidate", summary.HashFunctionCandidate),
            Line("opaque_word_semantics", summary.OpaqueWordSemantics),
            Line("status", summary.Status),
            Line("provenance", summary.Provenance),
            Line("direct_selection_binding", summary.DirectSelectionBinding),
            Line("original_semantics", summary.OriginalSemantics),
        };
    }

    public static void Write(string path, Fifast2CsiTunaAuditSummary summary)
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

public sealed record Fifast2CsiTunaAuditResult(
    IReadOnlyList<Fifast2CsiTunaCrosscheckRow> Rows,
    Fifast2CsiTunaAuditSummary Summary);

/// <summary>
/// Cross-checks the observed CSI names against the named hash variables in
/// tuna.big. The name/hash algorithm is measured independently, while the CSI
/// words after each name offset remain opaque. This audit never chooses an
/// EVT, ABKC, sample, gameplay cue or playback route.
/// </summary>
public static class Fifast2CsiTunaAudit
{
    public const string Status = "verified-structural-tuna-csi-name-hash-crosscheck";
    public const string Provenance =
        "tuna.big/tunavars.txt+FIFAST2.csi+pcpahdr.big;no-execution;no-binding";
    public const string DirectSelectionBinding = "not-proven";
    public const string OriginalSemantics = "not-claimed";
    public const string HashFunctionCandidate =
        "djb2-like-seed-0x1505-multiply-0x21";

    private const string TunaEntryName = "tunavars.txt";
    private static readonly Regex TunaRowPattern = new(
        @"^\s*0x(?<hash>[0-9A-Fa-f]+)\((?<name>[^()\r\n]+)\),",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static Fifast2CsiTunaAuditResult Audit(IXboxFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        using var tunaStream = fileSystem.OpenReadOnly("data/audio/tuna.big");
        using var csiStream = fileSystem.OpenReadOnly("data/audio/FIFAST2.csi");
        using var pcpahdrStream = fileSystem.OpenReadOnly("data/audio/pcpahdr.big");
        return Analyze(
            ReadBounded(tunaStream, 16 * 1024 * 1024),
            ReadBounded(csiStream, 16 * 1024 * 1024),
            ReadBounded(pcpahdrStream, 16 * 1024 * 1024));
    }

    public static Fifast2CsiTunaAuditResult Analyze(
        ReadOnlySpan<byte> tunaArchiveData,
        ReadOnlySpan<byte> csiData,
        ReadOnlySpan<byte> pcpahdrData)
    {
        var archiveBytes = tunaArchiveData.ToArray();
        var archive = BigArchiveReader.Inspect(archiveBytes);
        var tunaEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, TunaEntryName, StringComparison.OrdinalIgnoreCase));
        if (tunaEntry is null)
        {
            throw new InvalidDataException("tuna.big does not contain tunavars.txt.");
        }

        var tunaText = BigArchiveReader.ReadEntry(archiveBytes, tunaEntry.Name);
        var tunaRows = ParseTunaRows(tunaText);
        if (tunaRows.Count == 0)
        {
            throw new InvalidDataException("tunavars.txt contains no named hash rows.");
        }

        var tunaByName = tunaRows
            .GroupBy(row => row.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var csiAudit = Fifast2CsiReader.Audit(csiData);
        var pcpahdrLength = pcpahdrData.Length;
        var rows = csiAudit.Rows
            .Select(csiRow =>
            {
                var computedHash = ComputeHash(csiRow.Name);
                var present = tunaByName.TryGetValue(csiRow.Name, out var tunaRow);
                var computedMatches = present && computedHash == tunaRow!.EncodedHash;
                var opaqueMatches = present && csiRow.OpaqueWord == tunaRow!.EncodedHash;
                return new Fifast2CsiTunaCrosscheckRow(
                    csiRow.RecordIndex,
                    csiRow.Name,
                    csiRow.OpaqueWord,
                    present,
                    present ? tunaRow!.EncodedHash : null,
                    computedHash,
                    computedMatches,
                    opaqueMatches,
                    csiRow.OpaqueWord < pcpahdrLength,
                    present && computedMatches
                        ? "verified-name-hash-crosscheck"
                        : "no-tuna-name-crosscheck",
                    DirectSelectionBinding,
                    OriginalSemantics);
            })
            .ToArray();

        var tunaHashMatchCount = tunaRows.Count(row => row.EncodedHash == ComputeHash(row.Name));
        var csiNameOverlapCount = rows.Count(row => row.TunaNamePresent);
        var summary = new Fifast2CsiTunaAuditSummary(
            archive.Magic,
            archive.ArchiveSize,
            archive.FileCount,
            archive.IndexEnd,
            archive.ArchiveSizeMatches,
            tunaEntry.Name,
            tunaRows.Count,
            tunaHashMatchCount,
            tunaByName.Count,
            csiAudit.Summary.CandidateRecordCount,
            csiAudit.Summary.ValidRecordCount,
            csiNameOverlapCount,
            rows.Length - csiNameOverlapCount,
            tunaByName.Keys.Count(name => !csiAudit.Rows.Any(row =>
                string.Equals(row.Name, name, StringComparison.Ordinal))),
            rows.Count(row => row.TunaNamePresent && row.ComputedHashMatchesTuna),
            rows.Count(row => row.CsiOpaqueMatchesTunaHash),
            rows.Count(row => row.OpaqueWordWithinPcpahdrAsset),
            HashFunctionCandidate,
            "not-decoded",
            tunaHashMatchCount == tunaRows.Count &&
            csiAudit.Summary.ValidRecordCount == csiAudit.Summary.CandidateRecordCount
                ? Status
                : "partial-structural-tuna-csi-name-hash-crosscheck",
            Provenance,
            DirectSelectionBinding,
            OriginalSemantics);
        return new Fifast2CsiTunaAuditResult(rows, summary);
    }

    public static uint ComputeHash(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var hash = 0x1505u;
        foreach (var valueByte in Encoding.ASCII.GetBytes(value))
        {
            hash = unchecked(hash * 0x21u + valueByte);
        }

        return hash;
    }

    public static void WriteReport(string path, IEnumerable<Fifast2CsiTunaCrosscheckRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            "csi_record_index\tcsi_name\tcsi_opaque_word\ttuna_name_present\ttuna_hash_word\tcomputed_hash_word\tcomputed_hash_matches_tuna\tcsi_opaque_matches_tuna_hash\topaque_word_within_pcpahdr_asset\tstatus\tdirect_selection_binding\toriginal_semantics",
        };
        lines.AddRange(rows.Select(row => string.Join('\t', new[]
        {
            row.CsiRecordIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.CsiName),
            $"0x{row.CsiOpaqueWord:X8}",
            row.TunaNamePresent.ToString(),
            row.TunaHashWord.HasValue ? $"0x{row.TunaHashWord.Value:X8}" : "",
            $"0x{row.ComputedHashWord:X8}",
            row.ComputedHashMatchesTuna.ToString(),
            row.CsiOpaqueMatchesTunaHash.ToString(),
            row.OpaqueWordWithinPcpahdrAsset.ToString(),
            row.Status,
            row.DirectSelectionBinding,
            row.OriginalSemantics,
        })));
        WriteLines(path, lines);
    }

    private sealed record TunaRow(uint EncodedHash, string Name);

    private static IReadOnlyList<TunaRow> ParseTunaRows(ReadOnlySpan<byte> data)
    {
        var text = Encoding.ASCII.GetString(data);
        var rows = new List<TunaRow>();
        foreach (var line in text.Split('\n'))
        {
            var match = TunaRowPattern.Match(line);
            if (!match.Success ||
                !uint.TryParse(match.Groups["hash"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hash))
            {
                continue;
            }

            var name = match.Groups["name"].Value.Trim();
            if (name.Length > 0)
            {
                rows.Add(new TunaRow(hash, name));
            }
        }

        return rows;
    }

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException("The bounded tuna/CSI/PCPAHDR input exceeds the audit limit.");
        }

        stream.Seek(0, SeekOrigin.Begin);
        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return data;
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
