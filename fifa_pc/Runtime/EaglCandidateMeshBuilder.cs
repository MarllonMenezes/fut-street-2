using System.Buffers.Binary;

namespace FifaStreetPc;

public readonly record struct EaglMeshVertex(float X, float Y, float Z);
public readonly record struct EaglMeshTriangle(int A, int B, int C);
public sealed record EaglBoundFloatProfile(
    uint Slot,
    string StreamName,
    EaglPackedFloatProfile Profile);

public sealed record EaglCandidateMeshGroup(
    int LayerIndex,
    uint PrimitiveIndex,
    string StreamName,
    IReadOnlyList<EaglMeshVertex> Vertices,
    IReadOnlyList<EaglMeshTriangle> Triangles)
{
    /// <summary>
    /// Generic two-component values from fetch slot 1. They are kept separate
    /// from position vertices because their texture-coordinate semantics are
    /// not proven by the clean-room reader.
    /// </summary>
    public IReadOnlyList<EaglFloat2> Slot1Attributes { get; init; } = Array.Empty<EaglFloat2>();

    /// <summary>
    /// Lossless numeric profiles for bound streams whose complete records are
    /// finite packed floats. These profiles do not identify normals, weights,
    /// UVs or any other semantic.
    /// </summary>
    public IReadOnlyList<EaglBoundFloatProfile> BoundFloatProfiles { get; init; } =
        Array.Empty<EaglBoundFloatProfile>();

    /// <summary>
    /// Exact state assignments preserved from the primitive that produced this
    /// candidate. They are evidence attached to the group, not renderer
    /// instructions; names such as SHAPENAME are not resolved to materials.
    /// </summary>
    public IReadOnlyList<EaglStateAssignment> StateAssignments { get; init; } =
        Array.Empty<EaglStateAssignment>();

    /// <summary>
    /// Command identifiers in the complete render stream for this primitive.
    /// Payload words remain available through model inspection and are not
    /// given guessed meanings here.
    /// </summary>
    public IReadOnlyList<ushort> RenderCommandIds { get; init; } =
        Array.Empty<ushort>();
}

public sealed record EaglCandidateMesh(
    IReadOnlyList<EaglCandidateMeshGroup> Groups,
    int SkippedPrimitiveGroups)
{
    public long VertexCount => Groups.Sum(group => (long)group.Vertices.Count);
    public long TriangleCount => Groups.Sum(group => (long)group.Triangles.Count);
}

/// <summary>
/// Converts only EAGL strip candidates with an explicit five-slot vertex-fetch
/// binding into a managed mesh representation. It preserves the candidate
/// status: draw binding beyond vertex fetch, materials, skinning and the
/// semantic meaning of non-position attributes are not inferred here.
/// </summary>
public static class EaglCandidateMeshBuilder
{
    /// <summary>
    /// Returns a topology-only signature for a candidate mesh. The signature
    /// preserves group/vertex/triangle counts and skipped groups without
    /// claiming that the candidate is the original draw topology or that any
    /// morph payload applies to it.
    /// </summary>
    public static string GetStructuralSignature(EaglCandidateMesh mesh)
    {
        ArgumentNullException.ThrowIfNull(mesh);
        var topologyProfiles = mesh.Groups
            .GroupBy(group => (Vertices: group.Vertices.Count, Triangles: group.Triangles.Count))
            .OrderBy(group => group.Key.Vertices)
            .ThenBy(group => group.Key.Triangles)
            .Select(group => $"{group.Key.Vertices}/{group.Key.Triangles}={group.Count()}");
        return $"groups={mesh.Groups.Count};vertices={mesh.VertexCount};triangles={mesh.TriangleCount};skipped={mesh.SkippedPrimitiveGroups};topology={string.Join(",", topologyProfiles)}";
    }

    public static EaglCandidateMesh Build(
        ReadOnlySpan<byte> objectData,
        ElfInspection elf,
        EaglAxisAlignedBounds declaredBounds)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(declaredBounds);

        var primitives = EaglModelReader.Inspect(objectData, elf, declaredBounds);
        var groups = new List<EaglCandidateMeshGroup>();
        foreach (var primitive in primitives)
        {
            if (!string.Equals(primitive.PrimitiveType, "PT_TRIANGLESTRIP", StringComparison.Ordinal) ||
                primitive.BoundsMatchingVertexBuffers.Count != 1 ||
                !primitive.HasConsistentVertexLayout ||
                primitive.VertexFetchBindings.Count != 5)
            {
                continue;
            }

            var stream = primitive.VertexFetchBindings
                .Single(binding => binding.FetchSlot.Slot == 0)
                .Stream;
            var slot1Stream = primitive.VertexFetchBindings
                .Single(binding => binding.FetchSlot.Slot == 1)
                .Stream;
            if (stream.ElementCount < 3 ||
                !EaglVertexBufferReader.TryMeasureFloat3(objectData, elf, stream, out var measuredBounds) ||
                measuredBounds is null ||
                !EaglVertexBufferReader.FitsBounds(measuredBounds, declaredBounds))
            {
                continue;
            }

            var section = elf.Sections[stream.SectionIndex];
            var dataOffset = checked((int)((ulong)section.Offset + stream.DataOffset));
            var vertices = new EaglMeshVertex[checked((int)stream.ElementCount)];
            for (var vertex = 0u; vertex < stream.ElementCount; vertex++)
            {
                var vertexOffset = checked(dataOffset + (int)(vertex * stream.Stride));
                var coordinates = objectData.Slice(vertexOffset, 3 * sizeof(float));
                vertices[vertex] = new EaglMeshVertex(
                    ReadFloat(coordinates[..4]),
                    ReadFloat(coordinates.Slice(4, 4)),
                    ReadFloat(coordinates.Slice(8, 4)));
            }

            var triangles = new EaglMeshTriangle[checked((int)stream.ElementCount - 2)];
            for (var third = 2u; third < stream.ElementCount; third++)
            {
                var first = checked((int)third - 2);
                var second = checked((int)third - 1);
                if ((third & 1) != 0)
                {
                    (first, second) = (second, first);
                }

                triangles[checked((int)third - 2)] = new EaglMeshTriangle(first, second, checked((int)third));
            }

            var group = new EaglCandidateMeshGroup(
                primitive.LayerIndex,
                primitive.PrimitiveIndex,
                stream.SymbolName,
                vertices,
                triangles);
            var boundFloatProfiles = new List<EaglBoundFloatProfile>();
            foreach (var binding in primitive.VertexFetchBindings)
            {
                if (EaglVertexBufferReader.TryMeasurePackedFloatProfile(
                        objectData,
                        elf,
                        binding.Stream,
                        out var profile) &&
                    profile is not null)
                {
                    boundFloatProfiles.Add(new EaglBoundFloatProfile(
                        binding.FetchSlot.Slot,
                        binding.Stream.SymbolName,
                        profile));
                }
            }

            group = group with
            {
                BoundFloatProfiles = boundFloatProfiles,
                StateAssignments = primitive.StateAssignments,
                RenderCommandIds = primitive.RenderCommandStream.Commands
                    .Select(command => command.CommandId)
                    .ToArray(),
            };
            if (EaglVertexBufferReader.TryReadFloat2Values(
                    objectData,
                    elf,
                    slot1Stream,
                    out var slot1Attributes) &&
                slot1Attributes.Count == vertices.Length)
            {
                group = group with { Slot1Attributes = slot1Attributes };
            }

            groups.Add(group);
        }

        return new EaglCandidateMesh(groups, primitives.Count - groups.Count);
    }

    private static float ReadFloat(ReadOnlySpan<byte> bytes) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
}
