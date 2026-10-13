using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglPositionCloudExportResult(
    long VertexCount,
    int StreamCount,
    IReadOnlyList<string> StreamNames);

/// <summary>
/// Exports bbox-matching float3 EAGL streams as OBJ point primitives. It does
/// not infer faces or claim that the selected streams form a complete mesh.
/// </summary>
public static class EaglPositionCloudExporter
{
    public static EaglPositionCloudExportResult WriteObj(
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

        var candidates = new List<EaglVertexBufferStream>();
        foreach (var stream in EaglVertexBufferReader.Inspect(objectData, elf))
        {
            if (EaglVertexBufferReader.TryMeasureFloat3(objectData, elf, stream, out var candidateBounds) &&
                candidateBounds is not null && EaglVertexBufferReader.FitsBounds(candidateBounds, declaredBounds))
            {
                candidates.Add(stream);
            }
        }

        if (candidates.Count == 0)
        {
            throw new InvalidDataException("No finite float3 vertex-buffer streams matched the object's bounds.");
        }

        using var writer = new StreamWriter(destination, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 4096, leaveOpen: true);
        writer.WriteLine("# EAGL position candidates; point cloud only, no faces or topology inferred.");
        writer.WriteLine("# Candidate selection: finite float3 streams contained by the object's __BBOX.");

        long vertexIndex = 1;
        foreach (var stream in candidates)
        {
            var section = elf.Sections[stream.SectionIndex];
            var dataOffset = checked((int)((ulong)section.Offset + stream.DataOffset));
            writer.WriteLine($"o {SanitizeName(stream.SymbolName)}");
            for (var vertex = 0u; vertex < stream.ElementCount; vertex++)
            {
                var vertexOffset = checked(dataOffset + (int)(vertex * stream.Stride));
                var bytes = objectData.Slice(vertexOffset, 3 * sizeof(float));
                var x = ReadFloat(bytes[..4]);
                var y = ReadFloat(bytes.Slice(4, 4));
                var z = ReadFloat(bytes.Slice(8, 4));
                writer.Write("v ");
                writer.Write(x.ToString("G9", CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.Write(y.ToString("G9", CultureInfo.InvariantCulture));
                writer.Write(' ');
                writer.WriteLine(z.ToString("G9", CultureInfo.InvariantCulture));
                writer.Write("p ");
                writer.WriteLine(vertexIndex.ToString(CultureInfo.InvariantCulture));
                vertexIndex++;
            }
        }

        writer.Flush();
        return new EaglPositionCloudExportResult(
            vertexIndex - 1,
            candidates.Count,
            candidates.Select(stream => stream.SymbolName).ToArray());
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

    private static float ReadFloat(ReadOnlySpan<byte> bytes) =>
        BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(bytes));
}
