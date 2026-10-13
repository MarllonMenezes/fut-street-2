using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglMorphSiblingEvidence(
    string MorphObjectName,
    string? NormalizedSiblingStem,
    string? OrdSiblingName,
    bool HasCompleteSibling,
    bool SiblingElfValidated,
    int SiblingGeometrySymbolCount,
    bool SiblingHasBounds,
    string SiblingStructuralSignature,
    string SiblingStructuralLayoutSignature,
    string SiblingCommandStreamSignature,
    string SiblingVertexDescriptorSignature,
    string SiblingVertexRecordSignature,
    string SiblingNumericDescriptorSignature,
    string SiblingCandidateMeshSignature,
    int ShapeNameCount,
    int AttributeNameCount,
    int PosePaletteBankSymbolCount,
    string RelocationSignature,
    string RelativeRelocationSignature,
    string StructuralLayoutSignature,
    string NormalizedPayloadHash,
    string NormalizedPayloadWindowHashSignature,
    string SiblingCompanionNumericMatchSignature,
    string? Issue,
    byte[]? SiblingCommandStreamBytes = null,
    string? SiblingCompanionNumericMatchCanonical = null,
    string? SiblingCommandStreamBoundarySignature = null,
    string? SiblingCompanionNumericMatchBoundarySignature = null,
    string? SiblingCompanionNumericMatchValueCategorySignature = null)
{
    public IReadOnlyList<string> PosePaletteBankSymbols { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ElfSymbol> PosePaletteBankSymbolRecords { get; init; } = Array.Empty<ElfSymbol>();
    public IReadOnlyList<string> MorphShapeNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MorphAttributeNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> SiblingShapeNameStateValues { get; init; } = Array.Empty<string>();
    public string SiblingCandidateGeometrySignature { get; init; } = string.Empty;
}

/// <summary>
/// Correlates only the observed filename pattern between a standalone morph
/// object and a split geometry object in the same nested BIG. It does not
/// assign the morph payload to vertices, bones, poses, or animation frames.
/// </summary>
public static class EaglMorphSiblingReader
{
    /// <summary>
    /// Combines independent, already validated structural observations for a
    /// morph object and its same-nested-BIG geometry sibling. This is a
    /// repeatability key only: the components remain opaque layout, byte,
    /// candidate-topology, and cross-file numeric-coincidence evidence and are
    /// not converted into a morph, rig, pose, weight, frame, material, UV, or
    /// GPU binding.
    /// </summary>
    public static string GetPairedStructuralSignature(EaglMorphSiblingEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return string.Join(
            "||",
            evidence.StructuralLayoutSignature,
            evidence.NormalizedPayloadWindowHashSignature,
            evidence.SiblingStructuralLayoutSignature,
            evidence.SiblingCommandStreamSignature,
            evidence.SiblingVertexRecordSignature,
            evidence.SiblingNumericDescriptorSignature,
            evidence.SiblingCandidateMeshSignature,
            evidence.SiblingCompanionNumericMatchSignature);
    }

    public static IReadOnlyList<EaglMorphSiblingEvidence> Inspect(
        byte[] expandedArchive,
        string nestedArchiveName)
    {
        ArgumentNullException.ThrowIfNull(expandedArchive);
        ArgumentException.ThrowIfNullOrWhiteSpace(nestedArchiveName);
        var archive = BigArchiveReader.Inspect(expandedArchive);
        var ordByStem = archive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var entryNames = archive.Entries
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var evidence = new List<EaglMorphSiblingEvidence>();

        foreach (var morphEntry in archive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".o", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var morphData = BigArchiveReader.ReadEntry(expandedArchive, morphEntry.Name);
                var morphInspection = EaglObjectReader.Inspect(morphData);
                var morph = EaglMorphReader.Inspect(morphData, morphInspection.Elf);
                if (!morph.HasMorphSymbol)
                {
                    continue;
                }

                var signature = EaglMorphReader.GetRelocationFieldSignature(morph);
                var structuralLayoutSignature = EaglMorphReader.GetStructuralLayoutSignature(
                    morphInspection.Elf,
                    morph);
                var normalizedPayloadHash = EaglMorphReader.GetNormalizedPayloadHash(
                    morphData,
                    morphInspection.Elf,
                    morph);
                var normalizedPayloadWindowHashSignature =
                    EaglMorphReader.GetNormalizedPayloadWindowHashSignature(
                        morphData,
                        morphInspection.Elf,
                        morph);
                if (!EaglMorphReader.TryGetNormalizedSiblingStem(morphEntry.Name, out var siblingStem) ||
                    !ordByStem.TryGetValue(siblingStem, out var ordEntry))
                {
                    evidence.Add(new EaglMorphSiblingEvidence(
                        morphEntry.Name,
                        siblingStem is { Length: > 0 } ? siblingStem : null,
                        null,
                        false,
                        false,
                        0,
                        false,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        morph.ShapeNames.Count,
                        morph.AttributeNames.Count,
                        morph.PosePaletteBankSymbols.Count,
                        signature,
                        EaglMorphReader.GetRelativeRelocationFieldSignature(morph),
                        structuralLayoutSignature,
                        normalizedPayloadHash,
                        normalizedPayloadWindowHashSignature,
                        string.Empty,
                        "no normalized .ord sibling")
                    {
                        PosePaletteBankSymbols = morph.PosePaletteBankSymbols,
                        PosePaletteBankSymbolRecords = morph.PosePaletteBankSymbolRecords,
                        MorphShapeNames = morph.ShapeNames,
                        MorphAttributeNames = morph.AttributeNames,
                    });
                    continue;
                }

                var orlName = Path.ChangeExtension(ordEntry.Name, ".orl");
                if (!entryNames.Contains(orlName))
                {
                    evidence.Add(new EaglMorphSiblingEvidence(
                        morphEntry.Name,
                        siblingStem,
                        ordEntry.Name,
                        false,
                        false,
                        0,
                        false,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        morph.ShapeNames.Count,
                        morph.AttributeNames.Count,
                        morph.PosePaletteBankSymbols.Count,
                        signature,
                        EaglMorphReader.GetRelativeRelocationFieldSignature(morph),
                        structuralLayoutSignature,
                        normalizedPayloadHash,
                        normalizedPayloadWindowHashSignature,
                        string.Empty,
                        "normalized .ord sibling has no .orl")
                    {
                        PosePaletteBankSymbols = morph.PosePaletteBankSymbols,
                        PosePaletteBankSymbolRecords = morph.PosePaletteBankSymbolRecords,
                        MorphShapeNames = morph.ShapeNames,
                        MorphAttributeNames = morph.AttributeNames,
                    });
                    continue;
                }

                try
                {
                    var siblingObjectData = BigArchiveReader.ReadEntry(expandedArchive, ordEntry.Name);
                    var siblingLinkData = BigArchiveReader.ReadEntry(expandedArchive, orlName);
                    var siblingCombinedData = EaglObjectReader.Combine(siblingObjectData, siblingLinkData);
                    var siblingInspection = EaglObjectReader.Inspect(siblingCombinedData);
                    var siblingVertexStreams = EaglVertexBufferReader.Inspect(
                        siblingCombinedData,
                        siblingInspection.Elf);
                    var siblingPrimitives = EaglModelReader.Inspect(
                        siblingCombinedData,
                        siblingInspection.Elf,
                        siblingInspection.Bounds,
                        siblingVertexStreams);
                    var siblingCommandStreamSignature =
                        EaglModelReader.GetStructuralCommandStreamSignature(siblingPrimitives);
                    var siblingCommandStreamBytes =
                        EaglModelReader.GetRawCommandStreamBytes(siblingPrimitives);
                    var siblingCommandStreamBoundarySignature =
                        EaglModelReader.GetRawCommandStreamBoundarySignature(siblingPrimitives);
                    var siblingShapeNameStateValues = siblingPrimitives
                        .SelectMany(primitive => primitive.StateAssignments)
                        .Where(assignment => string.Equals(
                            assignment.Name,
                            "SHAPENAME",
                            StringComparison.Ordinal))
                        .Select(assignment => assignment.Value)
                        .Distinct(StringComparer.Ordinal)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    var siblingCandidateMeshSignature = string.Empty;
                    var siblingCandidateGeometrySignature = string.Empty;
                    EaglCandidateMesh? siblingCandidateMesh = null;
                    if (siblingInspection.Bounds is not null)
                    {
                        try
                        {
                            siblingCandidateMesh = EaglCandidateMeshBuilder.Build(
                                siblingCombinedData,
                                siblingInspection.Elf,
                                siblingInspection.Bounds);
                            siblingCandidateMeshSignature = EaglCandidateMeshBuilder.GetStructuralSignature(
                                siblingCandidateMesh);
                            siblingCandidateGeometrySignature =
                                EaglCandidateGeometryAuditor.GetEvidenceSignature(
                                    EaglCandidateGeometryAuditor.Measure(siblingCandidateMesh));
                        }
                        catch (Exception exception) when (
                            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
                        {
                            siblingCandidateMeshSignature = string.Empty;
                        }
                    }
                    var siblingCompanionNumericMatchAnalysis =
                        EaglMorphReader.GetCompanionNumericMatchAnalysis(
                            morphData,
                            morphInspection.Elf,
                            morph,
                            siblingInspection,
                            siblingVertexStreams,
                            siblingPrimitives,
                            siblingCandidateMesh);
                    evidence.Add(new EaglMorphSiblingEvidence(
                        morphEntry.Name,
                        siblingStem,
                        ordEntry.Name,
                        true,
                        true,
                        siblingInspection.GeometrySymbols.Count,
                        siblingInspection.Bounds is not null,
                        EaglObjectReader.GetStructuralSignature(siblingInspection),
                        EaglObjectReader.GetStructuralLayoutSignature(siblingInspection),
                        siblingCommandStreamSignature,
                        EaglVertexBufferReader.GetStructuralSignature(siblingVertexStreams),
                        EaglVertexBufferReader.GetRelativeRecordStructuralSignature(siblingVertexStreams),
                        EaglVertexBufferReader.GetNumericStructuralSignature(
                            siblingCombinedData,
                            siblingInspection.Elf,
                            siblingVertexStreams,
                            siblingInspection.Bounds),
                        siblingCandidateMeshSignature,
                        morph.ShapeNames.Count,
                        morph.AttributeNames.Count,
                        morph.PosePaletteBankSymbols.Count,
                        signature,
                        EaglMorphReader.GetRelativeRelocationFieldSignature(morph),
                        structuralLayoutSignature,
                        normalizedPayloadHash,
                        normalizedPayloadWindowHashSignature,
                        siblingCompanionNumericMatchAnalysis.Signature,
                        null,
                        siblingCommandStreamBytes,
                        siblingCompanionNumericMatchAnalysis.CanonicalMatches,
                        siblingCommandStreamBoundarySignature,
                        siblingCompanionNumericMatchAnalysis.CanonicalRecordBoundarySignature,
                        siblingCompanionNumericMatchAnalysis.CanonicalMatchValueCategorySignature)
                    {
                        PosePaletteBankSymbols = morph.PosePaletteBankSymbols,
                        PosePaletteBankSymbolRecords = morph.PosePaletteBankSymbolRecords,
                        MorphShapeNames = morph.ShapeNames,
                        MorphAttributeNames = morph.AttributeNames,
                        SiblingShapeNameStateValues = siblingShapeNameStateValues,
                        SiblingCandidateGeometrySignature = siblingCandidateGeometrySignature,
                    });
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
                {
                    evidence.Add(new EaglMorphSiblingEvidence(
                        morphEntry.Name,
                        siblingStem,
                        ordEntry.Name,
                        true,
                        false,
                        0,
                        false,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        morph.ShapeNames.Count,
                        morph.AttributeNames.Count,
                        morph.PosePaletteBankSymbols.Count,
                        signature,
                        EaglMorphReader.GetRelativeRelocationFieldSignature(morph),
                        structuralLayoutSignature,
                        normalizedPayloadHash,
                        normalizedPayloadWindowHashSignature,
                        string.Empty,
                        $"sibling invalid: {exception.Message}")
                    {
                        PosePaletteBankSymbols = morph.PosePaletteBankSymbols,
                        PosePaletteBankSymbolRecords = morph.PosePaletteBankSymbolRecords,
                        MorphShapeNames = morph.ShapeNames,
                        MorphAttributeNames = morph.AttributeNames,
                    });
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
            {
                evidence.Add(new EaglMorphSiblingEvidence(
                    morphEntry.Name,
                    null,
                    null,
                    false,
                    false,
                    0,
                    false,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    0,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    exception.Message));
            }
        }

        return evidence;
    }
}
