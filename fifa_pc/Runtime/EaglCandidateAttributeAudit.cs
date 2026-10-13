using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Per-object measurements for the current EAGL candidate path. The report
/// keeps slot-1 values, state assignments and their separate populations
/// visible without naming slot 1 as UV, material, skinning or GPU state.
/// </summary>
public sealed record EaglCandidateAttributeAuditRow(
    string ModelArchive,
    string ModelObject,
    string ShapeName,
    int Groups,
    long Vertices,
    long Triangles,
    int Slot1CompleteGroups,
    int Slot1FiniteGroups,
    int Slot1NearUnitGroups,
    long Slot1ValueCount,
    int GroupShapeStateCount,
    int GroupClampStateCount,
    int PrimitiveCount,
    int PrimitiveShapeStateCount,
    int PrimitiveClampStateCount,
    string Slot1RangeSignature,
    string Slot1ValueSha256,
    string Status,
    string Issue);

public static class EaglCandidateAttributeAuditor
{
    public static EaglCandidateAttributeAuditRow Measure(
        string modelArchive,
        string modelObject,
        string shapeName,
        EaglCandidateMesh mesh,
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchive);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObject);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);
        ArgumentNullException.ThrowIfNull(mesh);
        ArgumentNullException.ThrowIfNull(primitives);

        var completeGroups = mesh.Groups
            .Where(group => group.Slot1Attributes.Count == group.Vertices.Count)
            .ToArray();
        var finiteGroups = completeGroups
            .Where(group => group.Slot1Attributes.All(IsFinite))
            .ToArray();
        var nearUnitGroups = finiteGroups
            .Where(group => IsNearUnitRange(group.Slot1Attributes))
            .ToArray();
        var groupShapeStateCount = mesh.Groups.Count(group =>
            HasExactShapeState(group.StateAssignments, shapeName));
        var groupClampStateCount = mesh.Groups.Count(group =>
            HasClampState(group.StateAssignments));
        var primitiveShapeStateCount = primitives.Count(primitive =>
            HasExactShapeState(primitive.StateAssignments, shapeName));
        var primitiveClampStateCount = primitives.Count(primitive =>
            HasClampState(primitive.StateAssignments));

        var rangeSignature = string.Join(
            "|",
            completeGroups
                .OrderBy(group => group.LayerIndex)
                .ThenBy(group => group.PrimitiveIndex)
                .Select(group =>
                {
                    var range = GetRange(group.Slot1Attributes);
                    return FormattableString.Invariant(
                        $"layer={group.LayerIndex};primitive={group.PrimitiveIndex};count={group.Slot1Attributes.Count};min={range.MinX:R},{range.MinY:R};max={range.MaxX:R},{range.MaxY:R}");
                }));
        var valueTokens = completeGroups
            .OrderBy(group => group.LayerIndex)
            .ThenBy(group => group.PrimitiveIndex)
            .SelectMany(group => group.Slot1Attributes.Select((value, index) =>
                $"{group.LayerIndex}:{group.PrimitiveIndex}:{index}:{BitConverter.SingleToInt32Bits(value.X):X8}:{BitConverter.SingleToInt32Bits(value.Y):X8}"));
        var valueHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", valueTokens))));
        var status = completeGroups.Length == mesh.Groups.Count
            ? "complete-slot1-profile"
            : completeGroups.Length > 0
                ? "partial-slot1-profile"
                : "no-slot1-profile";
        var issue = completeGroups.Length == mesh.Groups.Count
            ? string.Empty
            : "not every candidate geometry group has a complete slot-1 value profile";

        return new EaglCandidateAttributeAuditRow(
            modelArchive,
            modelObject,
            shapeName,
            mesh.Groups.Count,
            mesh.VertexCount,
            mesh.TriangleCount,
            completeGroups.Length,
            finiteGroups.Length,
            nearUnitGroups.Length,
            completeGroups.Sum(group => (long)group.Slot1Attributes.Count),
            groupShapeStateCount,
            groupClampStateCount,
            primitives.Count,
            primitiveShapeStateCount,
            primitiveClampStateCount,
            rangeSignature,
            valueHash,
            status,
            issue);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglCandidateAttributeAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);

        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "model_object",
                "shape_name",
                "groups",
                "vertices",
                "triangles",
                "slot1_complete_groups",
                "slot1_finite_groups",
                "slot1_near_unit_groups",
                "slot1_value_count",
                "group_shape_state_count",
                "group_clamp_state_count",
                "primitive_count",
                "primitive_shape_state_count",
                "primitive_clamp_state_count",
                "slot1_range_signature",
                "slot1_value_sha256",
                "status",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.ModelArchive),
            Escape(row.ModelObject),
            Escape(row.ShapeName),
            row.Groups.ToString(CultureInfo.InvariantCulture),
            row.Vertices.ToString(CultureInfo.InvariantCulture),
            row.Triangles.ToString(CultureInfo.InvariantCulture),
            row.Slot1CompleteGroups.ToString(CultureInfo.InvariantCulture),
            row.Slot1FiniteGroups.ToString(CultureInfo.InvariantCulture),
            row.Slot1NearUnitGroups.ToString(CultureInfo.InvariantCulture),
            row.Slot1ValueCount.ToString(CultureInfo.InvariantCulture),
            row.GroupShapeStateCount.ToString(CultureInfo.InvariantCulture),
            row.GroupClampStateCount.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveCount.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveShapeStateCount.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveClampStateCount.ToString(CultureInfo.InvariantCulture),
            Escape(row.Slot1RangeSignature),
            Escape(row.Slot1ValueSha256),
            Escape(row.Status),
            Escape(row.Issue),
        })));

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(
            path,
            lines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static bool HasExactShapeState(
        IEnumerable<EaglStateAssignment> assignments,
        string shapeName) =>
        assignments.Any(assignment =>
            string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
            string.Equals(
                assignment.Value.Split(',', 2)[0].Trim(),
                shapeName,
                StringComparison.OrdinalIgnoreCase));

    private static bool HasClampState(IEnumerable<EaglStateAssignment> assignments) =>
        assignments.Any(assignment =>
            string.Equals(assignment.Name, "XBOXEXTOBJ_SetClampU", StringComparison.Ordinal) ||
            string.Equals(assignment.Name, "XBOXEXTOBJ_SetClampV", StringComparison.Ordinal));

    private static bool IsFinite(EaglFloat2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static bool IsNearUnitRange(IReadOnlyList<EaglFloat2> values)
    {
        var range = GetRange(values);
        return range.MinX >= -0.01f &&
               range.MinY >= -0.01f &&
               range.MaxX <= 1.01f &&
               range.MaxY <= 1.01f;
    }

    private static (float MinX, float MinY, float MaxX, float MaxY) GetRange(
        IReadOnlyList<EaglFloat2> values)
    {
        if (values.Count == 0)
        {
            return (0f, 0f, 0f, 0f);
        }

        return (
            values.Min(value => value.X),
            values.Min(value => value.Y),
            values.Max(value => value.X),
            values.Max(value => value.Y));
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
