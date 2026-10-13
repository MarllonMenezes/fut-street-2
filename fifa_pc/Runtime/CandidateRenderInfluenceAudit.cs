using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Records a bounded comparison between the managed candidate-texture path and
/// the same candidate mesh rendered with the solid fallback.  A verified row
/// proves only that the candidate texture changes this clean-room rasterizer's
/// output, either in the complete scene or in the isolated candidate comparison
/// when scene composition hides the change. It does not prove original UV, material, skinning, rigging,
/// animation, draw-command or GPU semantics.
/// </summary>
public sealed record CandidateRenderInfluenceRow(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    int TexturedCandidateGroups,
    long RasterVisibleTriangles,
    int TextureDistinctRgbaColors,
    int TextureNonTransparentPixels,
    long DifferingRasterPixels,
    long IsolatedDifferingRasterPixels,
    bool IsolatedTextureInfluencesRaster,
    bool GeometryFiniteAndIndexed,
    bool PrimitiveShapeStateExact,
    bool MeshShapeStateExact,
    ulong TexturedChecksum,
    ulong SolidChecksum,
    bool CandidateTextureInfluencesRaster,
    string Status,
    string Issue);

public sealed record CandidateRenderInfluenceSummary(
    int CandidateCount,
    int VerifiedCount,
    int GeometryValidCount,
    int TextureInfluenceCount,
    int IsolatedTextureInfluenceCount,
    int DistinctTexturedChecksums,
    int DistinctSolidChecksums,
    string Status,
    string OriginalSemantics);

public sealed record CandidateRenderInfluenceAuditResult(
    CandidateRenderInfluenceSummary Summary,
    IReadOnlyList<CandidateRenderInfluenceRow> Rows);

public static class CandidateRenderInfluenceAudit
{
    public const string Header =
        "kind\tmodel_archive\tmodel_object\tshape_name\ttextured_candidate_groups\traster_visible_triangles\ttexture_distinct_rgba_colors\ttexture_nontransparent_pixels\tdiffering_raster_pixels\tisolated_differing_raster_pixels\tisolated_texture_influences_raster\tgeometry_finite_and_indexed\tprimitive_shape_state_exact\tmesh_shape_state_exact\ttextured_checksum\tsolid_checksum\tcandidate_texture_influences_raster\tstatus\toriginal_semantics\tissue";

    public static CandidateRenderInfluenceAuditResult Audit(
        IEnumerable<CandidateRenderInfluenceRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var verified = materialized.Count(row => row.Status == "verified-candidate-raster-influence");
        var geometryValid = materialized.Count(row => row.GeometryFiniteAndIndexed);
        var influenceCount = materialized.Count(row => row.CandidateTextureInfluencesRaster);
        var isolatedInfluenceCount = materialized.Count(row => row.IsolatedTextureInfluencesRaster);
        var summary = new CandidateRenderInfluenceSummary(
            materialized.Length,
            verified,
            geometryValid,
            influenceCount,
            isolatedInfluenceCount,
            materialized
                .Where(row => row.TexturedChecksum != 0)
                .Select(row => row.TexturedChecksum)
                .Distinct()
                .Count(),
            materialized
                .Where(row => row.SolidChecksum != 0)
                .Select(row => row.SolidChecksum)
                .Distinct()
                .Count(),
            materialized.Length > 0 && verified == materialized.Length
                ? "verified-authored"
                : "incomplete",
            "not-claimed");
        return new CandidateRenderInfluenceAuditResult(summary, materialized);
    }

    public static IReadOnlyList<string> BuildLines(
        CandidateRenderInfluenceAuditResult audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        var lines = new List<string> { Header };
        lines.AddRange(audit.Rows.Select(row => string.Join('\t', new[]
        {
            "candidate",
            Escape(row.ModelArchiveName),
            Escape(row.ModelObjectName),
            Escape(row.ShapeName),
            row.TexturedCandidateGroups.ToString(CultureInfo.InvariantCulture),
            row.RasterVisibleTriangles.ToString(CultureInfo.InvariantCulture),
            row.TextureDistinctRgbaColors.ToString(CultureInfo.InvariantCulture),
            row.TextureNonTransparentPixels.ToString(CultureInfo.InvariantCulture),
            row.DifferingRasterPixels.ToString(CultureInfo.InvariantCulture),
            row.IsolatedDifferingRasterPixels.ToString(CultureInfo.InvariantCulture),
            row.IsolatedTextureInfluencesRaster.ToString(),
            row.GeometryFiniteAndIndexed.ToString(),
            row.PrimitiveShapeStateExact.ToString(),
            row.MeshShapeStateExact.ToString(),
            row.TexturedChecksum.ToString("X16", CultureInfo.InvariantCulture),
            row.SolidChecksum.ToString("X16", CultureInfo.InvariantCulture),
            row.CandidateTextureInfluencesRaster.ToString(),
            Escape(row.Status),
            audit.Summary.OriginalSemantics,
            Escape(row.Issue),
        })));
        lines.Add(string.Join('\t', new[]
        {
            "summary",
            "all",
            string.Empty,
            string.Empty,
            audit.Summary.CandidateCount.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            audit.Summary.IsolatedTextureInfluenceCount.ToString(CultureInfo.InvariantCulture),
            audit.Summary.GeometryValidCount.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            audit.Summary.DistinctTexturedChecksums.ToString(CultureInfo.InvariantCulture),
            audit.Summary.DistinctSolidChecksums.ToString(CultureInfo.InvariantCulture),
            audit.Summary.TextureInfluenceCount.ToString(CultureInfo.InvariantCulture),
            audit.Summary.Status,
            audit.Summary.OriginalSemantics,
            $"verified={audit.Summary.VerifiedCount.ToString(CultureInfo.InvariantCulture)}",
        }));
        return lines.AsReadOnly();
    }

    public static void Write(string path, CandidateRenderInfluenceAuditResult audit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(audit);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllLines(
            path,
            BuildLines(audit),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
