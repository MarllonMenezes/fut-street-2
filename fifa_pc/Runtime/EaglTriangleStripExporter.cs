using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglTriangleStripExportResult(
    int PrimitiveGroups,
    int SkippedPrimitiveGroups,
    long VertexCount,
    long TriangleCount);

/// <summary>
/// Emits provisional OBJ triangle strips only when the EAGL state explicitly
/// declares PT_TRIANGLESTRIP and exactly one bounds-matching VB stream maps to
/// that primitive number. The export remains a candidate until draw binding is
/// independently verified.
/// </summary>
public static class EaglTriangleStripExporter
{
    public static EaglTriangleStripExportResult WriteObj(
        Stream destination,
        ReadOnlySpan<byte> objectData,
        ElfInspection elf,
        EaglAxisAlignedBounds declaredBounds)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(declaredBounds);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("Destination stream must be writable.", nameof(destination));
        }

        var mesh = EaglCandidateMeshBuilder.Build(objectData, elf, declaredBounds);
        if (mesh.Groups.Count == 0)
        {
            throw new InvalidDataException("No unambiguous, bounds-matching PT_TRIANGLESTRIP candidates were found.");
        }

        using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
        writer.WriteLine("# Provisional EAGL triangle-strip candidates.");
        writer.WriteLine("# Faces use stream order; draw-command buffer binding is not yet proven.");
        long vertexIndex = 1;

        foreach (var group in mesh.Groups)
        {
            var firstVertex = vertexIndex;
            writer.WriteLine($"o layer{group.LayerIndex}_primitive{group.PrimitiveIndex}_{SanitizeName(group.StreamName)}");
            foreach (var vertex in group.Vertices)
            {
                writer.Write("v ");
                writer.Write(vertex.X.ToString("G9", CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.Write(vertex.Y.ToString("G9", CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.WriteLine(vertex.Z.ToString("G9", CultureInfo.InvariantCulture));
            }

            foreach (var triangle in group.Triangles)
            {
                writer.Write("f ");
                writer.Write((firstVertex + triangle.A).ToString(CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.Write((firstVertex + triangle.B).ToString(CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.WriteLine((firstVertex + triangle.C).ToString(CultureInfo.InvariantCulture));
            }

            vertexIndex += group.Vertices.Count;
        }

        writer.Flush();
        return new EaglTriangleStripExportResult(
            mesh.Groups.Count,
            mesh.SkippedPrimitiveGroups,
            mesh.VertexCount,
            mesh.TriangleCount);
    }

    private static string SanitizeName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'
                ? character
                : '_');
        }
        return builder.ToString();
    }

}
