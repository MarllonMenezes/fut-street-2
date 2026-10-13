using System.Globalization;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Compares the candidate textured raster with the same candidate mesh after a
/// deterministic perturbation of the opaque slot-1 values.  A verified row
/// proves only that this managed candidate path consumes those values while
/// sampling the supplied candidate texture; it does not identify UVs, prove a
/// material binding, or recover original GPU semantics.
/// </summary>
public sealed record CandidateAttributeRasterInfluenceRow(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    int CompleteAttributeGroups,
    int BaselineDrawnGroups,
    int PerturbedDrawnGroups,
    long DifferingRasterPixels,
    ulong BaselineChecksum,
    ulong PerturbedChecksum,
    bool GeometryFiniteAndIndexed,
    bool AttributeCardinalityValid,
    bool AttributePerturbsRaster,
    string Status,
    string Issue);

public sealed record CandidateAttributeRasterInfluenceSummary(
    int CandidateCount,
    int VerifiedCount,
    int GeometryValidCount,
    int AttributeCardinalityValidCount,
    int AttributePerturbationCount,
    int DistinctBaselineChecksums,
    int DistinctPerturbedChecksums,
    string Status,
    string OriginalSemantics);

public sealed record CandidateAttributeRasterInfluenceAuditResult(
    CandidateAttributeRasterInfluenceSummary Summary,
    IReadOnlyList<CandidateAttributeRasterInfluenceRow> Rows);

public static class CandidateAttributeRasterInfluenceAudit
{
    public const string Header =
        "kind\tmodel_archive\tmodel_object\tshape_name\tcomplete_attribute_groups\tbaseline_drawn_groups\tperturbed_drawn_groups\tdiffering_raster_pixels\tbaseline_checksum\tperturbed_checksum\tgeometry_finite_and_indexed\tattribute_cardinality_valid\tattribute_perturbs_raster\tstatus\toriginal_semantics\tissue";

    public static EaglCandidateMesh PerturbOpaqueSlot1Values(EaglCandidateMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var groups = mesh.Groups
            .Select(group => group with
            {
                Slot1Attributes = group.Slot1Attributes
                    .Select(value => new EaglFloat2(1f - value.X, value.Y))
                    .ToArray(),
            })
            .ToArray();
        return mesh with { Groups = groups };
    }

    public static CandidateAttributeRasterInfluenceAuditResult Audit(
        IEnumerable<CandidateAttributeRasterInfluenceRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var materialized = rows.ToArray();
        var verified = materialized.Count(row => row.Status == "verified-candidate-attribute-raster-influence");
        var geometryValid = materialized.Count(row => row.GeometryFiniteAndIndexed);
        var cardinalityValid = materialized.Count(row => row.AttributeCardinalityValid);
        var perturbationCount = materialized.Count(row => row.AttributePerturbsRaster);
        var summary = new CandidateAttributeRasterInfluenceSummary(
            materialized.Length,
            verified,
            geometryValid,
            cardinalityValid,
            perturbationCount,
            materialized.Where(row => row.BaselineChecksum != 0).Select(row => row.BaselineChecksum).Distinct().Count(),
            materialized.Where(row => row.PerturbedChecksum != 0).Select(row => row.PerturbedChecksum).Distinct().Count(),
            materialized.Length > 0 && verified == materialized.Length
                ? "verified-candidate-attribute-raster-influence"
                : "incomplete-candidate-attribute-raster-influence",
            "not-claimed");
        return new CandidateAttributeRasterInfluenceAuditResult(summary, materialized);
    }

    public static void Write(string path, CandidateAttributeRasterInfluenceAuditResult audit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(audit);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string> { Header };
        lines.AddRange(audit.Rows.Select(row => string.Join('\t', new[]
        {
            "candidate",
            Escape(row.ModelArchiveName),
            Escape(row.ModelObjectName),
            Escape(row.ShapeName),
            row.CompleteAttributeGroups.ToString(CultureInfo.InvariantCulture),
            row.BaselineDrawnGroups.ToString(CultureInfo.InvariantCulture),
            row.PerturbedDrawnGroups.ToString(CultureInfo.InvariantCulture),
            row.DifferingRasterPixels.ToString(CultureInfo.InvariantCulture),
            row.BaselineChecksum.ToString("X16", CultureInfo.InvariantCulture),
            row.PerturbedChecksum.ToString("X16", CultureInfo.InvariantCulture),
            row.GeometryFiniteAndIndexed.ToString(),
            row.AttributeCardinalityValid.ToString(),
            row.AttributePerturbsRaster.ToString(),
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
            audit.Summary.AttributeCardinalityValidCount.ToString(CultureInfo.InvariantCulture),
            string.Empty,
            string.Empty,
            string.Empty,
            audit.Summary.DistinctBaselineChecksums.ToString(CultureInfo.InvariantCulture),
            audit.Summary.DistinctPerturbedChecksums.ToString(CultureInfo.InvariantCulture),
            audit.Summary.GeometryValidCount.ToString(CultureInfo.InvariantCulture),
            audit.Summary.AttributeCardinalityValidCount.ToString(CultureInfo.InvariantCulture),
            audit.Summary.AttributePerturbationCount.ToString(CultureInfo.InvariantCulture),
            audit.Summary.Status,
            audit.Summary.OriginalSemantics,
            $"verified={audit.Summary.VerifiedCount.ToString(CultureInfo.InvariantCulture)}",
        }));
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\t", "\\t", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);
}
