using System.Globalization;

namespace FifaStreetPc;

/// <summary>
/// Objective quality measurements for the current EAGL mesh candidate path.
/// These measurements describe the managed candidate representation and its
/// software projection only; they do not identify vertex semantics, UVs,
/// materials, skinning, rigging, animation or original GPU commands.
/// </summary>
public sealed record EaglCandidateGeometryAudit(
    int GroupCount,
    long VertexCount,
    long TriangleCount,
    long InvalidIndexTriangles,
    long NonFiniteVertices,
    long NonFiniteReferencedTriangles,
    long RepeatedIndexTriangles,
    long ExactZeroArea3dTriangles,
    long ExactZeroAreaProjectedTriangles,
    long RasterDegenerateTriangles,
    long RasterVisibleTriangles,
    double MinEdgeLength,
    double MedianEdgeLength,
    double MaxEdgeLength,
    double MaxEdgeToMedianEdgeRatio)
{
    public bool HasOnlyFiniteInRangeGeometry =>
        InvalidIndexTriangles == 0 &&
        NonFiniteVertices == 0 &&
        NonFiniteReferencedTriangles == 0;
}

public static class EaglCandidateGeometryAuditor
{
    /// <summary>
    /// Serializes the measured candidate geometry without assigning a meaning
    /// to any vertex stream, index sequence, material, UV, skinning, rigging,
    /// animation or GPU command. This signature is intended for repeatability
    /// comparisons between already bounded candidate meshes.
    /// </summary>
    public static string GetEvidenceSignature(EaglCandidateGeometryAudit audit)
    {
        ArgumentNullException.ThrowIfNull(audit);
        return string.Join(';', new[]
        {
            $"groups={audit.GroupCount.ToString(CultureInfo.InvariantCulture)}",
            $"vertices={audit.VertexCount.ToString(CultureInfo.InvariantCulture)}",
            $"triangles={audit.TriangleCount.ToString(CultureInfo.InvariantCulture)}",
            $"invalid_index={audit.InvalidIndexTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"nonfinite_vertices={audit.NonFiniteVertices.ToString(CultureInfo.InvariantCulture)}",
            $"nonfinite_referenced={audit.NonFiniteReferencedTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"repeated_index={audit.RepeatedIndexTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"zero_area_3d={audit.ExactZeroArea3dTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"zero_area_projected={audit.ExactZeroAreaProjectedTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"raster_degenerate={audit.RasterDegenerateTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"raster_visible={audit.RasterVisibleTriangles.ToString(CultureInfo.InvariantCulture)}",
            $"min_edge={audit.MinEdgeLength.ToString("R", CultureInfo.InvariantCulture)}",
            $"median_edge={audit.MedianEdgeLength.ToString("R", CultureInfo.InvariantCulture)}",
            $"max_edge={audit.MaxEdgeLength.ToString("R", CultureInfo.InvariantCulture)}",
            $"max_edge_to_median={audit.MaxEdgeToMedianEdgeRatio.ToString("R", CultureInfo.InvariantCulture)}",
        });
    }

    /// <summary>
    /// Measures the candidate triangles before rasterization and after the
    /// same integer X/Y projection used by <see cref="EaglCandidateMeshRenderer"/>.
    /// A zero-area count is evidence about this representation/projection only.
    /// </summary>
    public static EaglCandidateGeometryAudit Measure(
        EaglCandidateMesh mesh,
        int pixelSize = 390)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        if (pixelSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelSize));
        }

        var allVertices = mesh.Groups
            .SelectMany(group => group.Vertices)
            .ToArray();
        var nonFiniteVertices = allVertices.LongCount(vertex =>
            !float.IsFinite(vertex.X) ||
            !float.IsFinite(vertex.Y) ||
            !float.IsFinite(vertex.Z));
        var finiteVertices = allVertices
            .Where(vertex =>
                float.IsFinite(vertex.X) &&
                float.IsFinite(vertex.Y) &&
                float.IsFinite(vertex.Z))
            .ToArray();

        var hasProjection = finiteVertices.Length > 0;
        var minX = hasProjection ? finiteVertices.Min(vertex => vertex.X) : 0f;
        var maxX = hasProjection ? finiteVertices.Max(vertex => vertex.X) : 0f;
        var minY = hasProjection ? finiteVertices.Min(vertex => vertex.Y) : 0f;
        var maxY = hasProjection ? finiteVertices.Max(vertex => vertex.Y) : 0f;
        var spanX = Math.Max(0.0001f, maxX - minX);
        var spanY = Math.Max(0.0001f, maxY - minY);
        var scale = pixelSize / Math.Max(spanX, spanY);
        var centerMeshX = (minX + maxX) / 2f;
        var centerMeshY = (minY + maxY) / 2f;

        long invalidIndexTriangles = 0;
        long nonFiniteReferencedTriangles = 0;
        long repeatedIndexTriangles = 0;
        long exactZeroArea3dTriangles = 0;
        long exactZeroAreaProjectedTriangles = 0;
        long rasterDegenerateTriangles = 0;
        long rasterVisibleTriangles = 0;
        var edgeLengths = new List<double>();

        foreach (var group in mesh.Groups)
        {
            foreach (var triangle in group.Triangles)
            {
                if ((uint)triangle.A >= (uint)group.Vertices.Count ||
                    (uint)triangle.B >= (uint)group.Vertices.Count ||
                    (uint)triangle.C >= (uint)group.Vertices.Count)
                {
                    invalidIndexTriangles++;
                    continue;
                }

                if (triangle.A == triangle.B ||
                    triangle.A == triangle.C ||
                    triangle.B == triangle.C)
                {
                    repeatedIndexTriangles++;
                }

                var a = group.Vertices[triangle.A];
                var b = group.Vertices[triangle.B];
                var c = group.Vertices[triangle.C];
                if (!IsFinite(a) || !IsFinite(b) || !IsFinite(c))
                {
                    nonFiniteReferencedTriangles++;
                    continue;
                }

                var abX = (double)b.X - a.X;
                var abY = (double)b.Y - a.Y;
                var abZ = (double)b.Z - a.Z;
                var acX = (double)c.X - a.X;
                var acY = (double)c.Y - a.Y;
                var acZ = (double)c.Z - a.Z;
                var crossX = abY * acZ - abZ * acY;
                var crossY = abZ * acX - abX * acZ;
                var crossZ = abX * acY - abY * acX;
                if (crossX == 0d && crossY == 0d && crossZ == 0d)
                {
                    exactZeroArea3dTriangles++;
                }

                var projectedArea = (double)(b.X - a.X) * (c.Y - a.Y) -
                                    (double)(b.Y - a.Y) * (c.X - a.X);
                if (projectedArea == 0d)
                {
                    exactZeroAreaProjectedTriangles++;
                }

                var projectedA = Project(a, scale, centerMeshX, centerMeshY);
                var projectedB = Project(b, scale, centerMeshX, centerMeshY);
                var projectedC = Project(c, scale, centerMeshX, centerMeshY);
                var rasterArea = (long)(projectedB.X - projectedA.X) *
                                 (projectedC.Y - projectedA.Y) -
                                 (long)(projectedB.Y - projectedA.Y) *
                                 (projectedC.X - projectedA.X);
                if (rasterArea == 0)
                {
                    rasterDegenerateTriangles++;
                }
                else
                {
                    rasterVisibleTriangles++;
                }

                edgeLengths.Add(Math.Sqrt(abX * abX + abY * abY + abZ * abZ));
                edgeLengths.Add(Math.Sqrt(acX * acX + acY * acY + acZ * acZ));
                var bcX = (double)c.X - b.X;
                var bcY = (double)c.Y - b.Y;
                var bcZ = (double)c.Z - b.Z;
                edgeLengths.Add(Math.Sqrt(bcX * bcX + bcY * bcY + bcZ * bcZ));
            }
        }

        edgeLengths.Sort();
        var minEdgeLength = edgeLengths.Count == 0 ? 0d : edgeLengths[0];
        var medianEdgeLength = edgeLengths.Count == 0
            ? 0d
            : edgeLengths[edgeLengths.Count / 2];
        var maxEdgeLength = edgeLengths.Count == 0 ? 0d : edgeLengths[^1];
        var maxEdgeToMedianEdgeRatio = medianEdgeLength == 0d
            ? 0d
            : maxEdgeLength / medianEdgeLength;

        return new EaglCandidateGeometryAudit(
            mesh.Groups.Count,
            allVertices.LongLength,
            mesh.Groups.Sum(group => (long)group.Triangles.Count),
            invalidIndexTriangles,
            nonFiniteVertices,
            nonFiniteReferencedTriangles,
            repeatedIndexTriangles,
            exactZeroArea3dTriangles,
            exactZeroAreaProjectedTriangles,
            rasterDegenerateTriangles,
            rasterVisibleTriangles,
            minEdgeLength,
            medianEdgeLength,
            maxEdgeLength,
            maxEdgeToMedianEdgeRatio);
    }

    private static bool IsFinite(EaglMeshVertex vertex) =>
        float.IsFinite(vertex.X) &&
        float.IsFinite(vertex.Y) &&
        float.IsFinite(vertex.Z);

    private static (int X, int Y) Project(
        EaglMeshVertex vertex,
        float scale,
        float centerMeshX,
        float centerMeshY) =>
        (
            (int)MathF.Round((vertex.X - centerMeshX) * scale),
            -(int)MathF.Round((vertex.Y - centerMeshY) * scale));
}
