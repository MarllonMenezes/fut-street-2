using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AptPackageAsset(
    string Path,
    string CompanionPath,
    int NestedArchiveCount,
    int NestedEntryCount,
    int AptEntryCount,
    int ConstantEntryCount,
    int ObjectEntryCount,
    int ShapeEntryCount,
    int ValidAptEntryCount,
    int ValidConstantEntryCount,
    int ValidObjectEntryCount,
    int ValidShapeEntryCount,
    int CompleteCompanionSetCount,
    int StructuralSignatureCount,
    int MorphSymbolCount,
    bool PackageIndexValidated,
    bool CompanionRangesValid,
    bool SentinelMatchesCompanion,
    string? Issue);

/// <summary>
/// Builds a bounded structural summary of the APT VivF packages. It records
/// only validated container, companion, header, ELF and SHPX observations;
/// it does not interpret any field as timing, pose, morph, frame or material.
/// </summary>
public static class AptPackageCatalogReader
{
    private const int MaximumExpandedBlockSize = 64 * 1024 * 1024;

    public static AptPackageAsset Inspect(
        IXboxFileSystem fileSystem,
        string packagePath,
        string? companionPath)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

        if (companionPath is null)
        {
            return new AptPackageAsset(
                packagePath,
                string.Empty,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                "APT package companion was not found.");
        }

        try
        {
            using var packageStream = fileSystem.OpenReadOnly(packagePath);
            var package = VivFReader.Inspect(packageStream);
            using var companionStream = fileSystem.OpenReadOnly(companionPath);
            var bound = VivFReader.BindToCompanion(package, companionStream);
            var nestedArchiveCount = 0;
            var nestedEntryCount = 0;
            var aptEntryCount = 0;
            var constantEntryCount = 0;
            var objectEntryCount = 0;
            var shapeEntryCount = 0;
            var validAptEntryCount = 0;
            var validConstantEntryCount = 0;
            var validObjectEntryCount = 0;
            var validShapeEntryCount = 0;
            var morphSymbolCount = 0;
            var completeCompanionSetCount = 0;
            var structuralSignatures = new HashSet<string>(StringComparer.Ordinal);
            var aptInspections = new Dictionary<string, AptInspection>(StringComparer.OrdinalIgnoreCase);
            var constantInspections = new Dictionary<string, AptInspection>(StringComparer.OrdinalIgnoreCase);
            var objectStructuralSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var shapeStructuralSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var companionSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var companionSizes = new Dictionary<string, Dictionary<string, uint>>(StringComparer.OrdinalIgnoreCase);

            for (var blockIndex = 0; blockIndex < checked((int)package.EntryCount); blockIndex++)
            {
                var record = package.Records[blockIndex];
                // VivFReader.OpenBlock returns an owning bounded stream. Use a
                // fresh read-only companion source for each block so disposing
                // that bounded stream cannot close a source needed by a later
                // block.
                using var blockCompanionStream = fileSystem.OpenReadOnly(companionPath);
                using var compressedBlock = VivFReader.OpenBlock(
                    blockCompanionStream,
                    package,
                    blockIndex);
                if (record.ExpandedSize > MaximumExpandedBlockSize)
                {
                    throw new InvalidDataException("APT RefPack block exceeds the catalog validation limit.");
                }

                var expanded = RefPackDecoder.Decode(
                    compressedBlock,
                    checked((int)record.ExpandedSize));
                var nestedArchive = BigArchiveReader.Inspect(expanded.Data);
                nestedArchiveCount++;
                nestedEntryCount += nestedArchive.Entries.Count;

                foreach (var nestedEntry in nestedArchive.Entries)
                {
                    var extension = Path.GetExtension(nestedEntry.Name).ToLowerInvariant();
                    if (extension is not (".apt" or ".const" or ".o" or ".xsh"))
                    {
                        continue;
                    }

                    var companionKey = $"{blockIndex}:{Path.GetFileNameWithoutExtension(nestedEntry.Name)}";
                    if (!companionSets.TryGetValue(companionKey, out var extensions))
                    {
                        extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        companionSets.Add(companionKey, extensions);
                    }

                    extensions.Add(extension);
                    if (!companionSizes.TryGetValue(companionKey, out var sizes))
                    {
                        sizes = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                        companionSizes.Add(companionKey, sizes);
                    }

                    sizes[extension] = nestedEntry.Size;
                    switch (extension)
                    {
                        case ".apt":
                            aptEntryCount++;
                            TryInspectApt(
                                expanded.Data,
                                nestedEntry,
                                aptInspections,
                                companionKey,
                                ref validAptEntryCount);
                            break;
                        case ".const":
                            constantEntryCount++;
                            TryInspectConstant(
                                expanded.Data,
                                nestedEntry,
                                constantInspections,
                                companionKey,
                                ref validConstantEntryCount);
                            break;
                        case ".o":
                            objectEntryCount++;
                            TryInspectObject(
                                expanded.Data,
                                nestedEntry,
                                objectStructuralSignatures,
                                companionKey,
                                ref validObjectEntryCount,
                                ref morphSymbolCount);
                            break;
                        case ".xsh":
                            shapeEntryCount++;
                            TryInspectShape(
                                expanded.Data,
                                nestedEntry,
                                shapeStructuralSignatures,
                                companionKey,
                                ref validShapeEntryCount);
                            break;
                    }
                }
            }

            foreach (var pair in aptInspections)
            {
                if (companionSets.TryGetValue(pair.Key, out var extensions) &&
                    extensions.IsSupersetOf(new[] { ".apt", ".const", ".o", ".xsh" }))
                {
                    completeCompanionSetCount++;
                }

                structuralSignatures.Add(AptReader.GetPackageStructuralSignature(
                    pair.Value,
                    constantInspections.GetValueOrDefault(pair.Key),
                    companionSizes.GetValueOrDefault(pair.Key) ??
                        new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase),
                    shapeStructuralSignatures.GetValueOrDefault(pair.Key),
                    objectStructuralSignatures.GetValueOrDefault(pair.Key)));
            }

            return new AptPackageAsset(
                packagePath,
                companionPath,
                nestedArchiveCount,
                nestedEntryCount,
                aptEntryCount,
                constantEntryCount,
                objectEntryCount,
                shapeEntryCount,
                validAptEntryCount,
                validConstantEntryCount,
                validObjectEntryCount,
                validShapeEntryCount,
                completeCompanionSetCount,
                structuralSignatures.Count,
                morphSymbolCount,
                package.DeclaredSizeMatches && package.OffsetsMonotonic,
                bound.RangesWithinCompanion,
                bound.SentinelMatchesCompanionLength,
                null);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException or NotSupportedException)
        {
            return new AptPackageAsset(
                packagePath,
                companionPath,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                exception.Message);
        }
    }

    private static void TryInspectApt(
        byte[] expandedData,
        BigArchiveEntry entry,
        IDictionary<string, AptInspection> inspections,
        string key,
        ref int validCount)
    {
        try
        {
            inspections[key] = AptReader.Inspect(BigArchiveReader.ReadEntry(expandedData, entry.Name));
            validCount++;
        }
        catch (InvalidDataException)
        {
        }
    }

    private static void TryInspectConstant(
        byte[] expandedData,
        BigArchiveEntry entry,
        IDictionary<string, AptInspection> inspections,
        string key,
        ref int validCount)
    {
        try
        {
            inspections[key] = AptReader.Inspect(BigArchiveReader.ReadEntry(expandedData, entry.Name));
            validCount++;
        }
        catch (InvalidDataException)
        {
        }
    }

    private static void TryInspectObject(
        byte[] expandedData,
        BigArchiveEntry entry,
        IDictionary<string, string> structuralSignatures,
        string key,
        ref int validCount,
        ref int morphSymbolCount)
    {
        try
        {
            var data = BigArchiveReader.ReadEntry(expandedData, entry.Name);
            var inspection = EaglObjectReader.Inspect(data);
            structuralSignatures[key] = EaglObjectReader.GetStructuralSignature(inspection);
            validCount++;
            try
            {
                if (EaglMorphReader.Inspect(data, inspection.Elf).HasMorphSymbol)
                {
                    morphSymbolCount++;
                }
            }
            catch (InvalidDataException)
            {
            }
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException)
        {
        }
    }

    private static void TryInspectShape(
        byte[] expandedData,
        BigArchiveEntry entry,
        IDictionary<string, string> structuralSignatures,
        string key,
        ref int validCount)
    {
        try
        {
            var package = ShapeImagePackageLoader.Load(BigArchiveReader.ReadEntry(expandedData, entry.Name));
            structuralSignatures[key] = ShapeImagePackageLoader.GetStructuralSignature(package);
            validCount++;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException)
        {
        }
    }
}
