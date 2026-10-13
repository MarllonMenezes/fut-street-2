using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglShapeMaterialAsset(
    string ModelArchivePath,
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    string ShapeImageArchiveName,
    ShapeImageEntry ShapeImageEntry,
    PcShapeImageTexture Texture);

/// <summary>
/// Loads a clean-room material asset only when the model state, the same-trunk
/// compressed SHPX package, and its internal entry agree textually. The
/// result is an asset binding candidate; it does not assign GPU material or
/// draw-command semantics to the original IDs.
/// </summary>
public static class EaglShapeMaterialLoader
{
    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;

    /// <summary>
    /// Confirms that the candidate mesh still carries an exact SHAPENAME state
    /// assignment for the same shape that was matched in the SHPX package.
    /// This is a mesh/state evidence gate only; it does not interpret the
    /// assignment as a material, UV binding, GPU command or renderer state.
    /// </summary>
    public static bool HasExactShapeState(
        EaglCandidateMesh candidateMesh,
        string shapeName)
    {
        ArgumentNullException.ThrowIfNull(candidateMesh);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return candidateMesh.Groups
            .SelectMany(group => group.StateAssignments)
            .Where(assignment => string.Equals(
                assignment.Name,
                "SHAPENAME",
                StringComparison.Ordinal))
            .Select(assignment => assignment.Value.Split(',', 2)[0].Trim())
            .Any(value => string.Equals(value, shapeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Confirms that the loaded split object preserves an exact SHAPENAME
    /// assignment in its inspected primitive-state records. The separate
    /// renderable mesh groups are not assumed to inherit that state: that
    /// cross-record association remains unproven until independently decoded.
    /// </summary>
    public static bool HasExactShapeStateInPrimitives(
        IReadOnlyList<EaglModelPrimitive> primitives,
        string shapeName)
    {
        ArgumentNullException.ThrowIfNull(primitives);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        return primitives
            .SelectMany(primitive => primitive.StateAssignments)
            .Where(assignment => string.Equals(
                assignment.Name,
                "SHAPENAME",
                StringComparison.Ordinal))
            .Select(assignment => assignment.Value.Split(',', 2)[0].Trim())
            .Any(value => string.Equals(value, shapeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds only same-trunk matches whose normalized SHAPENAME is an exact
    /// internal SHPX entry name. This is a batch evidence pass; it does not
    /// promote the result to a global material table.
    /// </summary>
    public static IReadOnlyList<EaglShapeMaterialAsset> LoadSameTrunkMatches(
        GameAssetStore store,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(store);
        return LoadSameTrunkMatches(
            store.OpenRaw,
            store.OpenBigEntry,
            modelArchivePath);
    }

    internal static IReadOnlyList<EaglShapeMaterialAsset> LoadSameTrunkMatches(
        IXboxFileSystem fileSystem,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        return LoadSameTrunkMatches(
            fileSystem.OpenReadOnly,
            (archivePath, entryName) => BigArchiveReader.OpenEntry(
                fileSystem,
                archivePath,
                entryName),
            modelArchivePath);
    }

    private static IReadOnlyList<EaglShapeMaterialAsset> LoadSameTrunkMatches(
        Func<string, Stream> openRaw,
        Func<string, string, Stream> openBigEntry,
        string modelArchivePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        using var topLevelStream = openRaw(modelArchivePath);
        var topLevelArchive = BigArchiveReader.Inspect(topLevelStream);
        var xshNames = topLevelArchive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".xsh", StringComparison.OrdinalIgnoreCase))
            .Select(entry => Path.GetFileNameWithoutExtension(entry.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matches = new List<EaglShapeMaterialAsset>();

        foreach (var modelEntry in topLevelArchive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
        {
            var modelBaseName = Path.GetFileNameWithoutExtension(modelEntry.Name);
            if (!xshNames.Contains(modelBaseName))
            {
                continue;
            }

            try
            {
                var expandedModel = ReadExpandedEntry(openBigEntry, modelArchivePath, modelEntry.Name);
                var modelArchive = BigArchiveReader.Inspect(expandedModel);
                var shapeImageArchiveName = modelBaseName + ".xsh";
                var shapePayload = ReadCompressedShapePackage(openBigEntry, modelArchivePath, shapeImageArchiveName);
                var shapePackage = ShapeImagePackageLoader.Load(shapePayload);
                var shapeEntries = shapePackage.Entries.ToDictionary(
                    entry => entry.Name,
                    StringComparer.OrdinalIgnoreCase);

                foreach (var objectEntry in modelArchive.Entries.Where(entry =>
                             string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase)))
                {
                    var companionName = Path.ChangeExtension(objectEntry.Name, ".orl");
                    var companionEntry = modelArchive.Entries.FirstOrDefault(entry =>
                        string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
                    if (companionEntry is null)
                    {
                        continue;
                    }

                    try
                    {
                        var objectData = BigArchiveReader.ReadEntry(expandedModel, objectEntry.Name);
                        var companionData = BigArchiveReader.ReadEntry(expandedModel, companionEntry.Name);
                        var inspection = EaglObjectReader.Inspect(objectData, companionData);
                        var primitives = EaglModelReader.Inspect(objectData, inspection.Elf, inspection.Bounds);
                        var matchedNames = primitives
                            .SelectMany(primitive => primitive.StateAssignments)
                            .Where(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal))
                            .Select(assignment => assignment.Value.Split(',', 2)[0].Trim())
                            .Where(shapeEntries.ContainsKey)
                            .Distinct(StringComparer.OrdinalIgnoreCase);

                        foreach (var shapeName in matchedNames)
                        {
                            try
                            {
                                var shapeEntry = shapeEntries[shapeName];
                                var texture = ShapeImageTextureLoader.DecodeNamed(shapePayload, shapeEntry.Name);
                                matches.Add(new EaglShapeMaterialAsset(
                                    modelArchivePath,
                                    modelEntry.Name,
                                    objectEntry.Name,
                                    shapeEntry.Name,
                                    shapeImageArchiveName,
                                    shapeEntry,
                                    texture));
                            }
                            catch (InvalidDataException)
                            {
                                // Keep the batch result limited to verified decodes.
                            }
                            catch (NotSupportedException)
                            {
                                // Keep unsupported image formats out of the verified set.
                            }
                        }
                    }
                    catch (InvalidDataException)
                    {
                        // One malformed split object must not hide other matches.
                    }
                }
            }
            catch (InvalidDataException)
            {
                // The batch is an inventory of verified matches, not a claim
                // that every same-trunk archive is a supported model package.
            }
        }

        return matches;
    }

    public static EaglShapeMaterialAsset LoadFirstForShape(
        GameAssetStore store,
        string modelArchivePath,
        string modelArchiveName,
        string shapeName)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        using var compressedStream = store.OpenBigEntry(modelArchivePath, modelArchiveName);
        if (compressedStream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed model archive exceeds the supported size limit.");
        }

        var compressed = new byte[checked((int)compressedStream.Length)];
        compressedStream.ReadExactly(compressed);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded model archive exceeds the supported size limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        var expanded = RefPackDecoder.Decode(compressedMemory, expandedSize);
        var archive = BigArchiveReader.Inspect(expanded.Data);
        foreach (var objectEntry in archive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                return LoadForObject(store, modelArchivePath, modelArchiveName, objectEntry.Name, shapeName);
            }
            catch (InvalidDataException)
            {
                // Continue until the split object carrying the requested state is found.
            }
        }

        throw new InvalidDataException(
            $"No split EAGL object in '{modelArchiveName}' carries SHAPENAME '{shapeName}'.");
    }

    public static EaglShapeMaterialAsset LoadForObject(
        GameAssetStore store,
        string modelArchivePath,
        string modelArchiveName,
        string modelObjectName,
        string shapeName)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentException.ThrowIfNullOrWhiteSpace(shapeName);

        var modelBaseName = Path.GetFileNameWithoutExtension(modelArchiveName);
        var shapeImageArchiveName = modelBaseName + ".xsh";
        var (objectData, companionData) = ReadSplitObject(
            store,
            modelArchivePath,
            modelArchiveName,
            modelObjectName);
        var inspection = EaglObjectReader.Inspect(objectData, companionData);
        var shapeAssignment = EaglModelReader.Inspect(objectData, inspection.Elf, inspection.Bounds)
            .SelectMany(primitive => primitive.StateAssignments)
            .FirstOrDefault(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                string.Equals(assignment.Value.Split(',', 2)[0].Trim(), shapeName, StringComparison.OrdinalIgnoreCase));
        if (shapeAssignment is null)
        {
            throw new InvalidDataException(
                $"Model object '{modelObjectName}' has no SHAPENAME assignment for '{shapeName}'.");
        }

        var shapePayload = ReadCompressedShapePackage(
            store,
            modelArchivePath,
            shapeImageArchiveName);
        var shapePackage = ShapeImagePackageLoader.Load(shapePayload);
        var shapeEntry = shapePackage.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, shapeName, StringComparison.OrdinalIgnoreCase));
        if (shapeEntry is null)
        {
            throw new InvalidDataException(
                $"SHPX package '{shapeImageArchiveName}' has no internal entry '{shapeName}'.");
        }

        var texture = ShapeImageTextureLoader.DecodeNamed(shapePayload, shapeEntry.Name);
        return new EaglShapeMaterialAsset(
            modelArchivePath,
            modelArchiveName,
            modelObjectName,
            shapeName,
            shapeImageArchiveName,
            shapeEntry,
            texture);
    }

    private static (byte[] ObjectData, byte[] CompanionData) ReadSplitObject(
        GameAssetStore store,
        string modelArchivePath,
        string modelArchiveName,
        string modelObjectName)
    {
        using var compressedStream = store.OpenBigEntry(modelArchivePath, modelArchiveName);
        if (compressedStream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed model archive exceeds the supported size limit.");
        }

        var compressed = new byte[checked((int)compressedStream.Length)];
        compressedStream.ReadExactly(compressed);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded model archive exceeds the supported size limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        var expanded = RefPackDecoder.Decode(compressedMemory, expandedSize);
        var archive = BigArchiveReader.Inspect(expanded.Data);
        var objectEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, modelObjectName, StringComparison.OrdinalIgnoreCase));
        if (objectEntry is null ||
            !string.Equals(Path.GetExtension(objectEntry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        {
            throw new FileNotFoundException($"Split EAGL object was not found: {modelObjectName}", modelObjectName);
        }

        var companionName = Path.ChangeExtension(objectEntry.Name, ".orl");
        var companionEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
        if (companionEntry is null)
        {
            throw new FileNotFoundException($"EAGL companion was not found: {companionName}", companionName);
        }

        return (
            BigArchiveReader.ReadEntry(expanded.Data, objectEntry.Name),
            BigArchiveReader.ReadEntry(expanded.Data, companionEntry.Name));
    }

    private static byte[] ReadCompressedShapePackage(
        GameAssetStore store,
        string modelArchivePath,
        string shapeImageArchiveName)
    {
        return ReadCompressedShapePackage(
            store.OpenBigEntry,
            modelArchivePath,
            shapeImageArchiveName);
    }

    private static byte[] ReadCompressedShapePackage(
        Func<string, string, Stream> openBigEntry,
        string modelArchivePath,
        string shapeImageArchiveName)
    {
        using var compressedStream = openBigEntry(modelArchivePath, shapeImageArchiveName);
        if (compressedStream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed SHPX package exceeds the supported size limit.");
        }

        var compressed = new byte[checked((int)compressedStream.Length)];
        compressedStream.ReadExactly(compressed);
        if (compressed.Length >= 4 && compressed.AsSpan(0, 4).SequenceEqual("SHPX"u8))
        {
            return compressed;
        }

        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded SHPX package exceeds the supported size limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        return RefPackDecoder.Decode(compressedMemory, expandedSize).Data;
    }

    private static byte[] ReadExpandedEntry(
        GameAssetStore store,
        string archivePath,
        string entryName)
    {
        return ReadExpandedEntry(store.OpenBigEntry, archivePath, entryName);
    }

    private static byte[] ReadExpandedEntry(
        Func<string, string, Stream> openBigEntry,
        string archivePath,
        string entryName)
    {
        using var compressedStream = openBigEntry(archivePath, entryName);
        if (compressedStream.Length > MaximumCompressedBytes)
        {
            throw new InvalidDataException("Compressed EAGL archive exceeds the supported size limit.");
        }

        var compressed = new byte[checked((int)compressedStream.Length)];
        compressedStream.ReadExactly(compressed);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded EAGL archive exceeds the supported size limit.");
        }

        using var compressedMemory = new MemoryStream(compressed, writable: false);
        return RefPackDecoder.Decode(compressedMemory, expandedSize).Data;
    }
}
