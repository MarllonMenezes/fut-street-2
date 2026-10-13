using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Reports the candidate parser's per-primitive SHAPENAME population. This is
/// an index/order control only. It does not bind an original state primitive to
/// vertices, UVs, materials, skinning, rigging, animation, morphs, commands or
/// GPU operations.
/// </summary>
public sealed record EaglCandidateShapeStatePrimitiveRow(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    int CandidateShapeStateCount,
    string CandidateShapeStateIndices,
    string CandidateShapeStateLayers,
    string CandidateShapeStateNames,
    int CandidateShapeStateStreamsCompleteCount,
    bool CandidateShapeStateStreamsComplete,
    string Status,
    string Provenance,
    string OriginalSemantics,
    string Issue);

public sealed record EaglCandidateShapeStatePrimitiveSummary(
    string CandidateSourceReport,
    int CandidateRows,
    int ValidRows,
    int CompleteRows,
    int DistinctObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

public static class EaglCandidateShapeStatePrimitiveAudit
{
    public const string Status = "verified-candidate-shape-state-index-control";
    public const string Provenance =
        "same-trunk-SHAPENAME-candidate-parser+primitive-index/layer/order-reparse;" +
        "no-original-binding-or-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    public static IReadOnlyList<EaglCandidateShapeStatePrimitiveRow> Audit(
        GameAssetStore store,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var matches = EaglShapeMaterialLoader.LoadSameTrunkMatches(store, modelArchivePath);
        var rows = new List<EaglCandidateShapeStatePrimitiveRow>();
        foreach (var objectGroup in matches
                     .GroupBy(match => (match.ModelArchiveName, match.ModelObjectName))
                     .OrderBy(group => group.Key.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(group => group.Key.ModelObjectName, StringComparer.OrdinalIgnoreCase))
        {
            EaglCandidateAsset candidate;
            try
            {
                candidate = EaglCandidateAssetLoader.Load(
                    store,
                    modelArchivePath,
                    objectGroup.Key.ModelArchiveName,
                    objectGroup.Key.ModelObjectName);
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or
                OverflowException or ArgumentOutOfRangeException or FileNotFoundException)
            {
                foreach (var match in objectGroup)
                {
                    rows.Add(Rejected(match, exception.Message));
                }

                continue;
            }

            foreach (var match in objectGroup.OrderBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
            {
                var shapeStates = candidate.Primitives
                    .Where(primitive => primitive.StateAssignments.Any(assignment =>
                        string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                        string.Equals(
                            assignment.Value.Split(',', 2)[0].Trim(),
                            match.ShapeName,
                            StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(primitive => primitive.LayerIndex)
                    .ThenBy(primitive => primitive.PrimitiveIndex)
                    .ToArray();
                var completeCount = shapeStates.Count(primitive => primitive.RenderCommandStream.IsComplete);
                rows.Add(new EaglCandidateShapeStatePrimitiveRow(
                    match.ModelArchiveName,
                    match.ModelObjectName,
                    match.ShapeName,
                    shapeStates.Length,
                    string.Join(",", shapeStates.Select(primitive => primitive.PrimitiveIndex)),
                    string.Join(",", shapeStates.Select(primitive => primitive.LayerIndex)),
                    string.Join("|", shapeStates.SelectMany(primitive => primitive.StateAssignments
                        .Where(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal))
                        .Select(assignment => $"{assignment.Name}={assignment.Value}"))),
                    completeCount,
                    completeCount == shapeStates.Length,
                    Status,
                    Provenance,
                    OriginalSemantics,
                    string.Empty));
            }
        }

        return rows;
    }

    public static EaglCandidateShapeStatePrimitiveSummary Summarize(
        string candidateSourceReport,
        IReadOnlyList<EaglCandidateShapeStatePrimitiveRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateSourceReport);
        ArgumentNullException.ThrowIfNull(rows);
        var validRows = rows.Count(row => row.Status == Status && row.OriginalSemantics == OriginalSemantics);
        var completeRows = rows.Count(row => row.CandidateShapeStateStreamsComplete);
        var distinctObjects = rows
            .Select(row => (row.ModelArchiveName, row.ModelObjectName))
            .Distinct()
            .Count();
        var complete = rows.Count == 24 && validRows == 24 && completeRows == 24 && distinctObjects == 24;
        return new EaglCandidateShapeStatePrimitiveSummary(
            candidateSourceReport,
            rows.Count,
            validRows,
            completeRows,
            distinctObjects,
            complete ? Status : $"inconclusive-{Status}",
            Provenance,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglCandidateShapeStatePrimitiveRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "shape_name", "candidate_shape_state_count",
                "candidate_shape_state_indices", "candidate_shape_state_layers", "candidate_shapename_values",
                "candidate_shape_state_streams_complete_count", "candidate_shape_state_streams_complete",
                "status", "provenance", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName), Field(row.ModelObjectName), Field(row.ShapeName),
            row.CandidateShapeStateCount.ToString(CultureInfo.InvariantCulture),
            Field(row.CandidateShapeStateIndices), Field(row.CandidateShapeStateLayers),
            Field(row.CandidateShapeStateNames),
            row.CandidateShapeStateStreamsCompleteCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateShapeStateStreamsComplete.ToString(), Field(row.Status),
            Field(row.Provenance), Field(row.OriginalSemantics), Field(row.Issue),
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(string path, EaglCandidateShapeStatePrimitiveSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "candidate_source_report", "candidate_rows", "valid_rows", "complete_rows",
                "distinct_objects", "status", "provenance", "original_semantics",
            }),
            string.Join("\t", new[]
            {
                Field(summary.CandidateSourceReport),
                summary.CandidateRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidRows.ToString(CultureInfo.InvariantCulture),
                summary.CompleteRows.ToString(CultureInfo.InvariantCulture),
                summary.DistinctObjects.ToString(CultureInfo.InvariantCulture),
                Field(summary.Status), Field(summary.Provenance), Field(summary.OriginalSemantics),
            }),
        };
        WriteLines(path, lines);
    }

    private static EaglCandidateShapeStatePrimitiveRow Rejected(
        EaglShapeMaterialAsset match,
        string issue) =>
        new(
            match.ModelArchiveName,
            match.ModelObjectName,
            match.ShapeName,
            0,
            string.Empty,
            string.Empty,
            string.Empty,
            0,
            false,
            "rejected-candidate",
            Provenance,
            OriginalSemantics,
            issue);

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}
