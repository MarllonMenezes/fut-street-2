using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglCandidateAsset(
    EaglCandidateMesh Mesh,
    EaglAxisAlignedBounds Bounds,
    string ObjectName,
    IReadOnlyList<EaglModelPrimitive> Primitives);

public static class EaglCandidateAssetLoader
{
    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const int MaximumExpandedBytes = 64 * 1024 * 1024;

    public static EaglCandidateAsset Load(
        GameAssetStore store,
        string archivePath,
        string nestedArchiveName,
        string objectName)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(archivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(nestedArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectName);

        using var compressedStream = store.OpenBigEntry(archivePath, nestedArchiveName);
        if (compressedStream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed EAGL archive exceeds the 64 MiB safety limit.");
        }

        var compressed = new byte[checked((int)compressedStream.Length)];
        compressedStream.ReadExactly(compressed);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded EAGL archive exceeds the 64 MiB safety limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        var expanded = RefPackDecoder.Decode(compressedMemory, expandedSize);
        var archive = BigArchiveReader.Inspect(expanded.Data);
        var objectEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, objectName, StringComparison.OrdinalIgnoreCase));
        if (objectEntry is null ||
            !string.Equals(Path.GetExtension(objectEntry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"Split EAGL object was not found: {objectName}");
        }

        var companionName = Path.ChangeExtension(objectEntry.Name, ".orl");
        var companionEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
        if (companionEntry is null)
        {
            throw new InvalidDataException($"EAGL companion is missing: {companionName}");
        }

        var objectData = BigArchiveReader.ReadEntry(expanded.Data, objectEntry.Name);
        var companionData = BigArchiveReader.ReadEntry(expanded.Data, companionEntry.Name);
        var inspection = EaglObjectReader.Inspect(objectData, companionData);
        if (inspection.Bounds is null)
        {
            throw new InvalidDataException("The EAGL object has no valid __BBOX.");
        }

        var mesh = EaglCandidateMeshBuilder.Build(objectData, inspection.Elf, inspection.Bounds);
        if (mesh.Groups.Count == 0)
        {
            throw new InvalidDataException("The EAGL object has no gated strip candidate groups.");
        }

        var primitives = EaglModelReader.Inspect(objectData, inspection.Elf, inspection.Bounds);
        return new EaglCandidateAsset(mesh, inspection.Bounds, objectEntry.Name, primitives);
    }
}
