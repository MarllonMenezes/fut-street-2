using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglOriginalCandidateSlotProfileRow(
    string ModelArchive,
    string ModelObject,
    int Layer,
    uint Primitive,
    bool CandidateCompleteFiveSlotLayout,
    bool CandidateSlot1RecordPresent,
    string CandidateSlot1Stream,
    bool OriginalSymbolMatch,
    bool OriginalObjectMatch,
    int OriginalCategoryKey,
    bool OriginalFiniteFloat2Profile,
    bool OriginalNearUnitRangeFloat2Profile,
    bool StreamStrideMatch,
    bool ByteLengthMatch,
    bool ElementCountMatch,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalCandidateSlotProfileSummary(
    string OriginalStreamReport,
    string CandidateFetchReport,
    int OriginalStreamRows,
    int CandidatePrimitiveRows,
    int CandidateCompleteFiveSlotRows,
    int CandidateSlot1Rows,
    int SymbolMatchRows,
    int ObjectMatchRows,
    int Category11Rows,
    int FiniteFloat2Rows,
    int NearUnitRangeFloat2Rows,
    int StreamStrideMatchRows,
    int ByteLengthMatchRows,
    int ElementCountMatchRows,
    int CompleteControlRows,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Independently joins the candidate fetch-record report to the original
/// numeric __VB stream report. The observed slot-1/category-11 relation is
/// retained as an opaque numeric/profile control only. It does not name the
/// stream as UV, material, normal, weight, skinning, rigging, animation,
/// morph or GPU state.
/// </summary>
public static class EaglOriginalCandidateSlotProfileAudit
{
    public const string Status =
        "verified-independent-original-candidate-slot1-category11-profile-control";
    public const string Provenance =
        "independent-TSV-reparse+candidate-slot-record/original-__VB-symbol-join+category11+stride+byte-length+element-count+float2-profile-controls;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    public static (IReadOnlyList<EaglOriginalCandidateSlotProfileRow> Rows,
        EaglOriginalCandidateSlotProfileSummary Summary) Audit(
        string originalStreamReportPath,
        string candidateFetchReportPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalStreamReportPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateFetchReportPath);

        var originalRows = ReadOriginalRows(originalStreamReportPath);
        var originalBySymbol = originalRows
            .GroupBy(row => MakeKey(row.ModelArchive, row.ModelObject, row.SymbolName), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Single(), StringComparer.Ordinal);
        var candidateRows = ReadCandidateRows(candidateFetchReportPath);
        var output = new List<EaglOriginalCandidateSlotProfileRow>(candidateRows.Count);
        foreach (var candidate in candidateRows)
        {
            var slot1 = ParseSlot1(candidate.SlotRecords);
            if (slot1 is null)
            {
                output.Add(new(
                    candidate.ModelArchive,
                    candidate.ModelObject,
                    candidate.Layer,
                    candidate.Primitive,
                    candidate.CompleteFiveSlotLayout,
                    false,
                    string.Empty,
                    false,
                    false,
                    -1,
                    false,
                    false,
                    false,
                    false,
                    false,
                    "inconclusive-" + Status,
                    OriginalSemantics,
                    "candidate fetch report has no parseable slot-1 record"));
                continue;
            }

            originalBySymbol.TryGetValue(
                MakeKey(candidate.ModelArchive, candidate.ModelObject, slot1.Value.StreamName),
                out var original);
            var symbolMatch = original is not null;
            var objectMatch = symbolMatch &&
                              string.Equals(original!.ModelArchive, candidate.ModelArchive, StringComparison.OrdinalIgnoreCase) &&
                              string.Equals(original.ModelObject, candidate.ModelObject, StringComparison.OrdinalIgnoreCase);
            var category11 = symbolMatch && original!.CategoryKey == 11;
            var finiteFloat2 = symbolMatch && original!.FiniteFloat2Profile;
            var nearUnit = symbolMatch && original!.NearUnitRangeFloat2Profile;
            var strideMatch = symbolMatch && original!.StreamStride == slot1.Value.StreamStride;
            var byteLengthMatch = symbolMatch && original!.ByteLength == slot1.Value.ByteLength;
            var elementCountMatch = symbolMatch && original!.ElementCount == slot1.Value.ElementCount;
            var valid = candidate.CompleteFiveSlotLayout &&
                        symbolMatch &&
                        objectMatch &&
                        category11 &&
                        finiteFloat2 &&
                        nearUnit &&
                        strideMatch &&
                        byteLengthMatch &&
                        elementCountMatch;
            output.Add(new(
                candidate.ModelArchive,
                candidate.ModelObject,
                candidate.Layer,
                candidate.Primitive,
                candidate.CompleteFiveSlotLayout,
                true,
                slot1.Value.StreamName,
                symbolMatch,
                objectMatch,
                symbolMatch ? original!.CategoryKey : -1,
                finiteFloat2,
                nearUnit,
                strideMatch,
                byteLengthMatch,
                elementCountMatch,
                valid ? Status : "inconclusive-" + Status,
                OriginalSemantics,
                valid ? string.Empty : "slot-1/category-11 numeric/profile control is incomplete"));
        }

        var completeControlRows = output.Count(row => row.Status == Status);
        var summaryStatus = candidateRows.Count > 0 &&
                            originalRows.Count > 0 &&
                            completeControlRows == output.Count(row => row.CandidateCompleteFiveSlotLayout) &&
                            completeControlRows > 0
            ? Status
            : "incomplete-" + Status;
        return (output, new(
            originalStreamReportPath,
            candidateFetchReportPath,
            originalRows.Count,
            candidateRows.Count,
            candidateRows.Count(row => row.CompleteFiveSlotLayout),
            output.Count(row => row.CandidateSlot1RecordPresent),
            output.Count(row => row.OriginalSymbolMatch),
            output.Count(row => row.OriginalObjectMatch),
            output.Count(row => row.OriginalCategoryKey == 11),
            output.Count(row => row.OriginalFiniteFloat2Profile),
            output.Count(row => row.OriginalNearUnitRangeFloat2Profile),
            output.Count(row => row.StreamStrideMatch),
            output.Count(row => row.ByteLengthMatch),
            output.Count(row => row.ElementCountMatch),
            completeControlRows,
            summaryStatus,
            Provenance,
            OriginalSemantics));
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalCandidateSlotProfileRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive",
                "candidate_complete_five_slot_layout", "candidate_slot1_record_present",
                "candidate_slot1_stream", "original_symbol_match", "original_object_match",
                "original_category_key", "original_finite_float2_profile",
                "original_near_unit_range_float2_profile", "stream_stride_match",
                "byte_length_match", "element_count_match", "status",
                "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchive), Field(row.ModelObject),
            row.Layer.ToString(CultureInfo.InvariantCulture),
            row.Primitive.ToString(CultureInfo.InvariantCulture),
            row.CandidateCompleteFiveSlotLayout.ToString(),
            row.CandidateSlot1RecordPresent.ToString(), Field(row.CandidateSlot1Stream),
            row.OriginalSymbolMatch.ToString(), row.OriginalObjectMatch.ToString(),
            row.OriginalCategoryKey.ToString(CultureInfo.InvariantCulture),
            row.OriginalFiniteFloat2Profile.ToString(),
            row.OriginalNearUnitRangeFloat2Profile.ToString(),
            row.StreamStrideMatch.ToString(), row.ByteLengthMatch.ToString(),
            row.ElementCountMatch.ToString(), Field(row.Status),
            Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalCandidateSlotProfileSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var headers = new[]
        {
            "original_stream_report", "candidate_fetch_report", "original_stream_rows",
            "candidate_primitive_rows", "candidate_complete_five_slot_rows",
            "candidate_slot1_rows", "symbol_match_rows", "object_match_rows",
            "category11_rows", "finite_float2_rows", "near_unit_range_float2_rows",
            "stream_stride_match_rows", "byte_length_match_rows", "element_count_match_rows",
            "complete_control_rows", "status", "provenance", "original_semantics",
        };
        var values = new[]
        {
            Field(summary.OriginalStreamReport), Field(summary.CandidateFetchReport),
            summary.OriginalStreamRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidatePrimitiveRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateCompleteFiveSlotRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateSlot1Rows.ToString(CultureInfo.InvariantCulture),
            summary.SymbolMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.ObjectMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.Category11Rows.ToString(CultureInfo.InvariantCulture),
            summary.FiniteFloat2Rows.ToString(CultureInfo.InvariantCulture),
            summary.NearUnitRangeFloat2Rows.ToString(CultureInfo.InvariantCulture),
            summary.StreamStrideMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.ByteLengthMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.ElementCountMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.CompleteControlRows.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status), Field(summary.Provenance), Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[] { string.Join("\t", headers), string.Join("\t", values) }, new UTF8Encoding(false));
    }

    private sealed record OriginalRow(
        string ModelArchive,
        string ModelObject,
        string SymbolName,
        int CategoryKey,
        uint StreamStride,
        uint ByteLength,
        uint ElementCount,
        bool FiniteFloat2Profile,
        bool NearUnitRangeFloat2Profile);

    private sealed record CandidateRow(
        string ModelArchive,
        string ModelObject,
        int Layer,
        uint Primitive,
        bool CompleteFiveSlotLayout,
        string SlotRecords);

    private readonly record struct Slot1(
        string StreamName,
        uint StreamStride,
        uint ByteLength,
        uint ElementCount);

    private static IReadOnlyList<OriginalRow> ReadOriginalRows(string path)
    {
        var rows = ReadTsv(path);
        return rows.Select(row => new OriginalRow(
            Required(row, "model_archive"),
            Required(row, "model_object"),
            Required(row, "symbol_name"),
            ParseInt(row, "category_key"),
            ParseUInt(row, "stream_stride"),
            ParseUInt(row, "byte_length"),
            ParseUInt(row, "element_count"),
            ParseBool(row, "finite_float2_profile"),
            ParseBool(row, "near_unit_range_float2_profile"))).ToArray();
    }

    private static IReadOnlyList<CandidateRow> ReadCandidateRows(string path)
    {
        var rows = ReadTsv(path);
        return rows.Select(row => new CandidateRow(
            Required(row, "model_archive"),
            Required(row, "model_object"),
            ParseInt(row, "layer"),
            ParseUInt(row, "primitive"),
            ParseBool(row, "complete_five_slot_layout"),
            row.TryGetValue("slot_records", out var slotRecords) ? slotRecords : string.Empty)).ToArray();
    }

    private static Slot1? ParseSlot1(string value)
    {
        var record = value.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .SingleOrDefault(item => item.StartsWith("slot=1;", StringComparison.Ordinal));
        if (record is null || !record.EndsWith(";valid=True", StringComparison.Ordinal))
        {
            return null;
        }

        var streamMatch = System.Text.RegularExpressions.Regex.Match(
            record, "stream=([^;]+);valid=True$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        var metadataMatch = System.Text.RegularExpressions.Regex.Match(
            record,
            "stream_stride=(\\d+);stream_bytes=(\\d+);elements=(\\d+);",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        return !streamMatch.Success || !metadataMatch.Success
            ? null
            : new Slot1(
                streamMatch.Groups[1].Value,
                uint.Parse(metadataMatch.Groups[1].Value, CultureInfo.InvariantCulture),
                uint.Parse(metadataMatch.Groups[2].Value, CultureInfo.InvariantCulture),
                uint.Parse(metadataMatch.Groups[3].Value, CultureInfo.InvariantCulture));
    }

    private static List<Dictionary<string, string>> ReadTsv(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"TSV report is empty: {path}");
        }

        var headers = lines[0].Split('\t');
        var rows = new List<Dictionary<string, string>>();
        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length != headers.Length)
            {
                throw new InvalidDataException($"TSV column count mismatch in {path}.");
            }

            var row = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < headers.Length; index++)
            {
                row[headers[index]] = fields[index];
            }

            rows.Add(row);
        }

        return rows;
    }

    private static string Required(IReadOnlyDictionary<string, string> row, string key) =>
        row.TryGetValue(key, out var value) && value.Length > 0
            ? value
            : throw new InvalidDataException($"Missing TSV value: {key}");

    private static string MakeKey(string modelArchive, string modelObject, string streamName) =>
        string.Join("\u001F", modelArchive, modelObject, streamName);

    private static int ParseInt(IReadOnlyDictionary<string, string> row, string key) =>
        int.Parse(Required(row, key), CultureInfo.InvariantCulture);

    private static uint ParseUInt(IReadOnlyDictionary<string, string> row, string key) =>
        uint.Parse(Required(row, key), CultureInfo.InvariantCulture);

    private static bool ParseBool(IReadOnlyDictionary<string, string> row, string key) =>
        bool.Parse(Required(row, key));

    private static string Field(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);

    private static void EnsureParent(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
