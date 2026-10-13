using System.Globalization;

namespace FifaStreetPc;

public sealed record EaglOriginalStateTokenRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int AssignmentCount,
    string AssignmentNames,
    bool HasShapeNameAssignment,
    bool HasClampUAssignment,
    bool HasClampVAssignment,
    bool HasPrimitiveTypeAssignment,
    int TextureTokenHits,
    int MaterialTokenHits,
    int UvCoordinateTokenHits,
    int VertexAttributeTokenHits,
    int DeformationTokenHits,
    string InputStatus,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalStateTokenSummary(
    string ModelArchivePath,
    int InputRows,
    int StateRows,
    int ValidInputRows,
    int ShapeNameRows,
    int ClampURows,
    int ClampVRows,
    int PrimitiveTypeRows,
    int TextureTokenRows,
    int MaterialTokenRows,
    int UvCoordinateTokenRows,
    int VertexAttributeTokenRows,
    int DeformationTokenRows,
    string AssignmentNameCatalog,
    string Status,
    string Provenance,
    string OriginalSemantics);

public sealed record EaglOriginalStateTokenAuditResult(
    IReadOnlyList<EaglOriginalStateTokenRow> Rows,
    EaglOriginalStateTokenSummary Summary);

/// <summary>
/// Inventories literal assignment names in the raw original EAGL render-chain
/// state symbols. The vocabulary is kept as text and is only used for a
/// negative control: a literal token is not promoted to a material, UV,
/// vertex-attribute, skinning, rigging, animation, morph or GPU binding.
/// </summary>
public static class EaglOriginalStateTokenAudit
{
    public const string Status = "verified-bounded-original-state-token-negative-control";
    public const string Provenance =
        "same-trunk-raw-EAGL-render-chain-state-symbols+literal-vocabulary-inventory+negative-control;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private static readonly string[] TextureTokens =
    {
        "texture", "tex", "sampler", "image", "diffuse", "albedo",
    };

    private static readonly string[] MaterialTokens =
    {
        "material", "shader", "surface", "diffuse", "albedo",
    };

    private static readonly string[] UvCoordinateTokens =
    {
        "uv", "texcoord", "tex_coord", "texturecoordinate", "texture_coordinate",
    };

    private static readonly string[] VertexAttributeTokens =
    {
        "vertex", "position", "normal", "tangent", "binormal", "color", "attribute",
    };

    private static readonly string[] DeformationTokens =
    {
        "skin", "weight", "bone", "rig", "morph", "deform", "palette",
    };

    public static EaglOriginalStateTokenAuditResult Audit(
        GameAssetStore store,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var renderChain = EaglOriginalRenderChainAudit.Audit(store, modelArchivePath);
        return Analyze(modelArchivePath, renderChain.Rows);
    }

    public static EaglOriginalStateTokenAuditResult Analyze(
        string modelArchivePath,
        IReadOnlyList<EaglOriginalRenderChainRow> inputRows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentNullException.ThrowIfNull(inputRows);

        var rows = inputRows.Select(BuildRow).ToArray();
        var validInputRows = rows.Count(row =>
            string.Equals(row.InputStatus, EaglOriginalRenderChainAudit.Status, StringComparison.Ordinal));
        var stateRows = rows.Count(row => row.AssignmentCount > 0);
        var shapeNameRows = rows.Count(row => row.HasShapeNameAssignment);
        var clampURows = rows.Count(row => row.HasClampUAssignment);
        var clampVRows = rows.Count(row => row.HasClampVAssignment);
        var primitiveTypeRows = rows.Count(row => row.HasPrimitiveTypeAssignment);
        var textureTokenRows = rows.Count(row => row.TextureTokenHits > 0);
        var materialTokenRows = rows.Count(row => row.MaterialTokenHits > 0);
        var uvCoordinateTokenRows = rows.Count(row => row.UvCoordinateTokenHits > 0);
        var vertexAttributeTokenRows = rows.Count(row => row.VertexAttributeTokenHits > 0);
        var deformationTokenRows = rows.Count(row => row.DeformationTokenHits > 0);
        var names = rows
            .SelectMany(row => row.AssignmentNames.Split(",", StringSplitOptions.RemoveEmptyEntries))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal);
        var status = rows.Length > 0 &&
                     validInputRows == rows.Length &&
                     stateRows == rows.Length &&
                     textureTokenRows == 0 &&
                     materialTokenRows == 0 &&
                     uvCoordinateTokenRows == 0 &&
                     vertexAttributeTokenRows == 0 &&
                     deformationTokenRows == 0
            ? Status
            : "inconclusive-original-state-token-control";
        var summary = new EaglOriginalStateTokenSummary(
            modelArchivePath,
            rows.Length,
            stateRows,
            validInputRows,
            shapeNameRows,
            clampURows,
            clampVRows,
            primitiveTypeRows,
            textureTokenRows,
            materialTokenRows,
            uvCoordinateTokenRows,
            vertexAttributeTokenRows,
            deformationTokenRows,
            string.Join(",", names),
            status,
            Provenance,
            OriginalSemantics);
        return new(rows, summary);
    }

    public static void WriteReport(string path, IEnumerable<EaglOriginalStateTokenRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "model_object",
                "layer",
                "primitive",
                "assignment_count",
                "assignment_names",
                "has_shapename_assignment",
                "has_clamp_u_assignment",
                "has_clamp_v_assignment",
                "has_primitive_type_assignment",
                "texture_token_hits",
                "material_token_hits",
                "uv_coordinate_token_hits",
                "vertex_attribute_token_hits",
                "deformation_token_hits",
                "input_status",
                "status",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.AssignmentCount.ToString(CultureInfo.InvariantCulture),
            Field(row.AssignmentNames),
            row.HasShapeNameAssignment.ToString(),
            row.HasClampUAssignment.ToString(),
            row.HasClampVAssignment.ToString(),
            row.HasPrimitiveTypeAssignment.ToString(),
            row.TextureTokenHits.ToString(CultureInfo.InvariantCulture),
            row.MaterialTokenHits.ToString(CultureInfo.InvariantCulture),
            row.UvCoordinateTokenHits.ToString(CultureInfo.InvariantCulture),
            row.VertexAttributeTokenHits.ToString(CultureInfo.InvariantCulture),
            row.DeformationTokenHits.ToString(CultureInfo.InvariantCulture),
            Field(row.InputStatus),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
    }

    public static void WriteSummary(string path, EaglOriginalStateTokenSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var lines = new[]
        {
            string.Join("\t", new[]
            {
                "model_archive_path",
                "input_rows",
                "state_rows",
                "valid_input_rows",
                "shapename_rows",
                "clamp_u_rows",
                "clamp_v_rows",
                "primitive_type_rows",
                "texture_token_rows",
                "material_token_rows",
                "uv_coordinate_token_rows",
                "vertex_attribute_token_rows",
                "deformation_token_rows",
                "assignment_name_catalog",
                "status",
                "provenance",
                "original_semantics",
            }),
            string.Join("\t", new[]
            {
                Field(summary.ModelArchivePath),
                summary.InputRows.ToString(CultureInfo.InvariantCulture),
                summary.StateRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidInputRows.ToString(CultureInfo.InvariantCulture),
                summary.ShapeNameRows.ToString(CultureInfo.InvariantCulture),
                summary.ClampURows.ToString(CultureInfo.InvariantCulture),
                summary.ClampVRows.ToString(CultureInfo.InvariantCulture),
                summary.PrimitiveTypeRows.ToString(CultureInfo.InvariantCulture),
                summary.TextureTokenRows.ToString(CultureInfo.InvariantCulture),
                summary.MaterialTokenRows.ToString(CultureInfo.InvariantCulture),
                summary.UvCoordinateTokenRows.ToString(CultureInfo.InvariantCulture),
                summary.VertexAttributeTokenRows.ToString(CultureInfo.InvariantCulture),
                summary.DeformationTokenRows.ToString(CultureInfo.InvariantCulture),
                Field(summary.AssignmentNameCatalog),
                Field(summary.Status),
                Field(summary.Provenance),
                Field(summary.OriginalSemantics),
            }),
        };
        File.WriteAllLines(path, lines, new System.Text.UTF8Encoding(false));
    }

    private static EaglOriginalStateTokenRow BuildRow(EaglOriginalRenderChainRow input)
    {
        var assignments = input.StateAssignments
            .Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(value => value.Split('=', 2)[0])
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var assignmentText = string.Join(",", assignments);
        var sourceText = input.StateAssignments.ToLowerInvariant();
        var textureHits = CountTokenHits(sourceText, TextureTokens);
        var materialHits = CountTokenHits(sourceText, MaterialTokens);
        var uvHits = CountTokenHits(sourceText, UvCoordinateTokens);
        var vertexHits = CountTokenHits(sourceText, VertexAttributeTokens);
        var deformationHits = CountTokenHits(sourceText, DeformationTokens);
        var issue = textureHits == 0 &&
                    materialHits == 0 &&
                    uvHits == 0 &&
                    vertexHits == 0 &&
                    deformationHits == 0
            ? string.Empty
            : "literal token requires semantic review; no binding assigned";
        var status = string.Equals(input.Status, EaglOriginalRenderChainAudit.Status, StringComparison.Ordinal) &&
                     issue.Length == 0
            ? Status
            : "inconclusive-original-state-token-control";
        return new(
            input.ModelArchiveName,
            input.ModelObjectName,
            input.LayerIndex,
            input.PrimitiveIndex,
            assignments.Length,
            assignmentText,
            input.HasShapeNameAssignment,
            assignments.Contains("XBOXEXTOBJ_SetClampU", StringComparer.Ordinal),
            assignments.Contains("XBOXEXTOBJ_SetClampV", StringComparer.Ordinal),
            input.HasPrimitiveTypeAssignment,
            textureHits,
            materialHits,
            uvHits,
            vertexHits,
            deformationHits,
            input.Status,
            status,
            OriginalSemantics,
            issue);
    }

    private static int CountTokenHits(string text, IEnumerable<string> tokens) =>
        tokens.Count(text.Contains);

    private static string Field(string value) =>
        (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
