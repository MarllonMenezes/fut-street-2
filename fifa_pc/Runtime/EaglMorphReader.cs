using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglMorphPointerEvidence(
    uint FieldOffset,
    uint TargetOffset,
    string? TargetString);

public sealed record EaglMorphPayloadWindow(
    uint StartOffset,
    byte[] Bytes,
    IReadOnlyList<uint> RelocationFieldOffsets);

public sealed record EaglMorphNamedString(uint Offset, string Value);
public sealed record EaglMorphShapePointer(uint FieldOffset, string ShapeName);
public sealed record EaglMorphAttributePointer(uint FieldOffset, string AttributeName);

public sealed record EaglMorphInspection(
    bool HasMorphSymbol,
    string? MorphSymbol,
    IReadOnlyList<EaglMorphNamedString> NamedStrings,
    IReadOnlyList<string> ShapeNames,
    IReadOnlyList<string> AttributeNames,
    IReadOnlyList<EaglMorphPointerEvidence> InternalPointers,
    IReadOnlyList<EaglMorphShapePointer> ShapePointers,
    IReadOnlyList<EaglMorphAttributePointer> AttributePointers,
    IReadOnlyList<string> PosePaletteBankSymbols)
{
    public IReadOnlyList<ElfSymbol> PosePaletteBankSymbolRecords { get; init; } = Array.Empty<ElfSymbol>();
}

/// <summary>
/// Canonical records produced by the conservative cross-file numeric scan.
/// The canonical text is an observed offset/value/category list; it is not a
/// decoded field table or an asset-semantic binding.
/// </summary>
public sealed record EaglCompanionNumericMatchAnalysis(
    int MetadataValueCount,
    int ScannedWords,
    int MaskedWords,
    int MatchWords,
    string CategorySummary,
    string CanonicalMatches,
    string CanonicalRecordBoundarySignature,
    string CanonicalMatchValueCategorySignature)
{
    public string Signature =>
        $"metadata_values={MetadataValueCount};scanned_words={ScannedWords};masked_words={MaskedWords};match_words={MatchWords};categories={CategorySummary};sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(CanonicalMatches)))}";
}

/// <summary>
/// Reads only named/relocated evidence from standalone EAGL morph objects.
/// It deliberately does not assign a layout to the binary payload or turn
/// repeated words into vertex, bone, or pose counts.
/// </summary>
public static class EaglMorphReader
{
    private const string MorphMarker = "__Morph:::";

    /// <summary>
    /// Returns only a normalized filename candidate for a geometry sibling.
    /// The suffix rule is an observed archive naming pattern, not an asset or
    /// animation binding.
    /// </summary>
    public static bool TryGetNormalizedSiblingStem(string morphObjectName, out string stem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(morphObjectName);
        stem = string.Empty;
        if (!string.Equals(Path.GetExtension(morphObjectName), ".o", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var baseName = Path.GetFileNameWithoutExtension(morphObjectName);
        const string morphSuffix = "_morph";
        if (!baseName.EndsWith(morphSuffix, StringComparison.OrdinalIgnoreCase) ||
            baseName.Length == morphSuffix.Length)
        {
            return false;
        }

        stem = baseName[..^morphSuffix.Length];
        return true;
    }

    public static string GetRelocationFieldSignature(EaglMorphInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        return string.Join(",", GetRelocationFieldOffsets(inspection)
            .Select(offset => $"0x{offset:X}"));
    }

    public static string GetRelativeRelocationFieldSignature(EaglMorphInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        var offsets = GetRelocationFieldOffsets(inspection);
        if (offsets.Length == 0)
        {
            return string.Empty;
        }

        var first = offsets[0];
        return string.Join(",", offsets
            .Select(offset => $"0x{offset - first:X}"));
    }

    /// <summary>
    /// Produces a layout-only comparison key from section and named-pointer
    /// positions. The key intentionally normalizes BS_* names to the observed
    /// name class and keeps Coordinates/Normals as literal tokens; it does not
    /// assign either class a vertex, attribute, or animation meaning.
    /// </summary>
    public static string GetStructuralLayoutSignature(
        ElfInspection elf,
        EaglMorphInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(inspection);
        if (!inspection.HasMorphSymbol)
        {
            return string.Empty;
        }

        var morphSymbol = elf.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith(MorphMarker, StringComparison.Ordinal));
        if (morphSymbol is null || morphSymbol.SectionIndex == 0 ||
            morphSymbol.SectionIndex >= elf.Sections.Count)
        {
            throw new InvalidDataException("The EAGL morph symbol is not backed by a section.");
        }

        var section = elf.Sections[morphSymbol.SectionIndex];
        var fieldOffsets = GetRelocationFieldOffsets(inspection);
        var firstField = fieldOffsets.Length == 0 ? 0u : fieldOffsets[0];
        var namedStrings = string.Join(";", inspection.NamedStrings.Select(item =>
            $"{NormalizeNamedString(item.Value)}@0x{item.Offset:X}/{item.Value.Length}"));
        var relocations = string.Join(",", fieldOffsets.Select(offset =>
            $"0x{offset - firstField:X}"));
        var targets = string.Join(";", inspection.InternalPointers
            .OrderBy(pointer => pointer.FieldOffset)
            .ThenBy(pointer => pointer.TargetOffset)
            .Select(pointer =>
                $"0x{pointer.FieldOffset - firstField:X}->0x{pointer.TargetOffset:X}:{NormalizeNamedString(pointer.TargetString)}"));

        return $"section=0x{section.Size:X};symbol=0x{morphSymbol.Value:X}/0x{morphSymbol.Size:X};strings={namedStrings};relocs={relocations};targets={targets}";
    }

    private static uint[] GetRelocationFieldOffsets(EaglMorphInspection inspection) => inspection.InternalPointers
            .Select(pointer => pointer.FieldOffset)
            .Distinct()
            .OrderBy(offset => offset)
            .ToArray();

    private static IReadOnlyList<(uint Start, uint End)> BuildMaskedRanges(
        ElfSection section,
        EaglMorphInspection inspection)
    {
        var ranges = new List<(uint Start, uint End)>();
        foreach (var namedString in inspection.NamedStrings)
        {
            if (namedString.Offset >= section.Size || namedString.Value.Length == 0)
            {
                continue;
            }

            ranges.Add((namedString.Offset,
                Math.Min(section.Size, checked(namedString.Offset + (uint)namedString.Value.Length))));
        }

        foreach (var pointer in inspection.InternalPointers)
        {
            if (pointer.FieldOffset >= section.Size)
            {
                continue;
            }

            ranges.Add((pointer.FieldOffset,
                Math.Min(section.Size, checked(pointer.FieldOffset + sizeof(uint)))));
        }

        var merged = new List<(uint Start, uint End)>();
        foreach (var range in ranges.OrderBy(range => range.Start).ThenBy(range => range.End))
        {
            if (range.End <= range.Start)
            {
                continue;
            }

            if (merged.Count == 0 || range.Start > merged[^1].End)
            {
                merged.Add(range);
            }
            else if (range.End > merged[^1].End)
            {
                merged[^1] = (merged[^1].Start, range.End);
            }
        }

        return merged;
    }

    private static string NormalizeNamedString(string? value)
    {
        if (value is null)
        {
            return "?";
        }

        return value.StartsWith("BS_", StringComparison.Ordinal) ? "BS_*" : value;
    }

    /// <summary>
    /// Produces a reproducible raw-byte comparison key for the morph section.
    /// Named strings and the four-byte words at observed relocation fields are
    /// masked; all other bytes remain raw. The hash is structural evidence
    /// only and is not a decoded morph record.
    /// </summary>
    public static string GetNormalizedPayloadHash(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglMorphInspection inspection)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(inspection);
        if (!inspection.HasMorphSymbol)
        {
            return string.Empty;
        }

        var morphSymbol = elf.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith(MorphMarker, StringComparison.Ordinal));
        if (morphSymbol is null || morphSymbol.SectionIndex == 0 ||
            morphSymbol.SectionIndex >= elf.Sections.Count)
        {
            throw new InvalidDataException("The EAGL morph symbol is not backed by a section.");
        }

        var section = elf.Sections[morphSymbol.SectionIndex];
        if (section.Type == 8 || section.Offset > data.Length ||
            section.Size > (ulong)data.Length - section.Offset || section.Size > int.MaxValue)
        {
            throw new InvalidDataException("The EAGL morph section is not fully file-backed.");
        }

        var normalized = data
            .Slice(checked((int)section.Offset), checked((int)section.Size))
            .ToArray();
        foreach (var namedString in inspection.NamedStrings)
        {
            if (namedString.Offset >= normalized.Length)
            {
                continue;
            }

            var length = Math.Min(namedString.Value.Length, normalized.Length - checked((int)namedString.Offset));
            normalized.AsSpan(checked((int)namedString.Offset), length).Fill(0xA5);
        }

        foreach (var pointer in inspection.InternalPointers)
        {
            if (pointer.FieldOffset >= normalized.Length ||
                normalized.Length - checked((int)pointer.FieldOffset) < sizeof(uint))
            {
                continue;
            }

            normalized.AsSpan(checked((int)pointer.FieldOffset), sizeof(uint)).Fill(0x5A);
        }

        return Convert.ToHexString(SHA256.HashData(normalized));
    }

    /// <summary>
    /// Produces a multiset signature for bounded payload neighborhoods around
    /// observed relocation fields. The same conservative masks as the whole
    /// payload hash are applied to named strings and relocation words; every
    /// other byte remains part of the local hash. Window positions are not
    /// included in the key so repeated local byte neighborhoods can be
    /// compared across objects without assigning them a record meaning.
    /// </summary>
    public static string GetNormalizedPayloadWindowHashSignature(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglMorphInspection inspection,
        int radius = 16)
    {
        ArgumentNullException.ThrowIfNull(elf);
        ArgumentNullException.ThrowIfNull(inspection);
        if (!inspection.HasMorphSymbol)
        {
            return string.Empty;
        }

        var windows = InspectPayloadWindows(data, elf, radius);
        if (windows.Count == 0)
        {
            return string.Empty;
        }

        var hashes = new List<string>(windows.Count);
        foreach (var window in windows)
        {
            var normalized = window.Bytes.ToArray();
            foreach (var namedString in inspection.NamedStrings)
            {
                if (namedString.Offset < window.StartOffset ||
                    namedString.Offset >= window.StartOffset + (uint)normalized.Length)
                {
                    continue;
                }

                var localOffset = checked((int)(namedString.Offset - window.StartOffset));
                var length = Math.Min(namedString.Value.Length, normalized.Length - localOffset);
                normalized.AsSpan(localOffset, length).Fill(0xA5);
            }

            foreach (var fieldOffset in window.RelocationFieldOffsets)
            {
                if (fieldOffset < window.StartOffset ||
                    fieldOffset - window.StartOffset > (uint)(normalized.Length - sizeof(uint)))
                {
                    continue;
                }

                var localOffset = checked((int)(fieldOffset - window.StartOffset));
                normalized.AsSpan(localOffset, sizeof(uint)).Fill(0x5A);
            }

            hashes.Add(Convert.ToHexString(SHA256.HashData(normalized)));
        }

        hashes.Sort(StringComparer.Ordinal);
        var distinctHashCount = hashes.Distinct(StringComparer.Ordinal).Count();
        return $"windows={windows.Count};distinct={distinctHashCount};hashes={string.Join(",", hashes)}";
    }

    /// <summary>
    /// Reports exact little-endian word coincidences between an observed morph
    /// section and already validated numeric metadata from its same-package
    /// geometry companion. Named-string bytes and words at observed relocation
    /// fields are excluded. The result is cross-file evidence only: a matching
    /// word is not promoted to a vertex count, index, weight, pose, frame, or
    /// any other asset semantic.
    /// </summary>
    public static string GetCompanionNumericMatchSignature(
        ReadOnlySpan<byte> morphData,
        ElfInspection morphElf,
        EaglMorphInspection morphInspection,
        EaglObjectInspection siblingInspection,
        IReadOnlyList<EaglVertexBufferStream> siblingVertexStreams,
        IReadOnlyList<EaglModelPrimitive> siblingPrimitives,
        EaglCandidateMesh? siblingCandidateMesh)
    {
        return GetCompanionNumericMatchAnalysis(
                morphData,
                morphElf,
                morphInspection,
                siblingInspection,
                siblingVertexStreams,
                siblingPrimitives,
                siblingCandidateMesh)
            .Signature;
    }

    public static EaglCompanionNumericMatchAnalysis GetCompanionNumericMatchAnalysis(
        ReadOnlySpan<byte> morphData,
        ElfInspection morphElf,
        EaglMorphInspection morphInspection,
        EaglObjectInspection siblingInspection,
        IReadOnlyList<EaglVertexBufferStream> siblingVertexStreams,
        IReadOnlyList<EaglModelPrimitive> siblingPrimitives,
        EaglCandidateMesh? siblingCandidateMesh)
    {
        ArgumentNullException.ThrowIfNull(morphElf);
        ArgumentNullException.ThrowIfNull(morphInspection);
        ArgumentNullException.ThrowIfNull(siblingInspection);
        ArgumentNullException.ThrowIfNull(siblingVertexStreams);
        ArgumentNullException.ThrowIfNull(siblingPrimitives);

        if (!morphInspection.HasMorphSymbol)
        {
            return new EaglCompanionNumericMatchAnalysis(
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty);
        }

        var morphSymbol = morphElf.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith(MorphMarker, StringComparison.Ordinal));
        if (morphSymbol is null || morphSymbol.SectionIndex == 0 ||
            morphSymbol.SectionIndex >= morphElf.Sections.Count)
        {
            throw new InvalidDataException("The EAGL morph symbol is not backed by a section.");
        }

        var section = morphElf.Sections[morphSymbol.SectionIndex];
        if (section.Type == 8 || section.Offset > morphData.Length ||
            section.Size > (ulong)morphData.Length - section.Offset || section.Size > uint.MaxValue)
        {
            throw new InvalidDataException("The EAGL morph section is not fully file-backed.");
        }

        var metadata = new Dictionary<string, HashSet<uint>>(StringComparer.Ordinal);
        void Add(string name, ulong value)
        {
            if (value == 0 || value > uint.MaxValue)
            {
                return;
            }

            if (!metadata.TryGetValue(name, out var values))
            {
                values = new HashSet<uint>();
                metadata.Add(name, values);
            }

            values.Add((uint)value);
        }

        Add("companion.sections", (ulong)siblingInspection.Elf.Sections.Count);
        Add("companion.file_bytes", siblingInspection.Elf.Sections
            .Where(sectionInfo => sectionInfo.Type != 8)
            .Aggregate<ElfSection, ulong>(0, (total, sectionInfo) => total + sectionInfo.Size));
        Add("companion.symbols", (ulong)siblingInspection.Elf.Symbols.Count);
        Add("companion.relocations", (ulong)siblingInspection.Elf.Relocations.Count);
        Add("companion.geometry_symbols", (ulong)siblingInspection.GeometrySymbols.Count);

        foreach (var stream in siblingVertexStreams)
        {
            Add("vb.record_offset", stream.BufferRecordOffset);
            Add("vb.descriptor_offset", stream.DescriptorOffset);
            Add("vb.data_offset", stream.DataOffset);
            Add("vb.byte_length", stream.ByteLength);
            Add("vb.stride", stream.Stride);
            Add("vb.flags", stream.Flags);
            Add("vb.element_count", stream.ElementCount);
        }

        foreach (var primitive in siblingPrimitives)
        {
            Add("primitive.layer", (ulong)primitive.LayerIndex);
            Add("primitive.index", primitive.PrimitiveIndex);
            Add("primitive.descriptor_offset", primitive.RenderDescriptorOffset);
            Add("primitive.method_offset", primitive.RenderMethodOffset);
            Add("primitive.global_offset", primitive.GlobalParametersOffset);
            Add("primitive.command_count", (ulong)primitive.RenderCommandStream.Commands.Count);
            Add("primitive.vertex_slot_count", (ulong)primitive.VertexFetchSlots.Count);
        }

        if (siblingCandidateMesh is not null)
        {
            Add("candidate.groups", (ulong)siblingCandidateMesh.Groups.Count);
            Add("candidate.vertices", (ulong)siblingCandidateMesh.VertexCount);
            Add("candidate.triangles", (ulong)siblingCandidateMesh.TriangleCount);
            Add("candidate.skipped_groups", (ulong)siblingCandidateMesh.SkippedPrimitiveGroups);
        }

        var metadataByValue = metadata
            .SelectMany(pair => pair.Value.Select(value => (Name: pair.Key, Value: value)))
            .GroupBy(item => item.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal).ToArray());
        var maskRanges = BuildMaskedRanges(section, morphInspection);
        var sectionBytes = morphData.Slice(checked((int)section.Offset), checked((int)section.Size));
        var scannedWords = 0;
        var maskedWords = 0;
        var matches = new List<string>();
        for (var offset = 0; offset <= sectionBytes.Length - sizeof(uint); offset += sizeof(uint))
        {
            scannedWords++;
            var fieldStart = checked((uint)offset);
            if (maskRanges.Any(range => fieldStart < range.End && fieldStart + sizeof(uint) > range.Start))
            {
                maskedWords++;
                continue;
            }

            var value = BinaryPrimitives.ReadUInt32LittleEndian(sectionBytes.Slice(offset, sizeof(uint)));
            if (!metadataByValue.TryGetValue(value, out var categories))
            {
                continue;
            }

            matches.Add($"0x{fieldStart:X}:0x{value:X8}:{string.Join(",", categories)}");
        }

        var categoryCounts = matches
            .SelectMany(match =>
            {
                var separator = match.LastIndexOf(':');
                return separator < 0
                    ? Array.Empty<string>()
                    : match[(separator + 1)..].Split(',', StringSplitOptions.RemoveEmptyEntries);
            })
            .GroupBy(category => category, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => $"{group.Key}={group.Count()}");
        var canonicalMatches = string.Join("|", matches);
        var recordBoundaryTokens = matches
            .Select(match =>
            {
                var separator = match.IndexOf(':');
                return separator < 0 ? match : match[..separator];
            })
            .ToArray();
        var valueCategoryTokens = matches
            .Select(match =>
            {
                var separator = match.IndexOf(':');
                return separator < 0 ? string.Empty : match[(separator + 1)..];
            })
            .ToArray();
        var canonicalRecordBoundaries = string.Join("|", recordBoundaryTokens);
        var canonicalValueCategories = string.Join("|", valueCategoryTokens);
        return new EaglCompanionNumericMatchAnalysis(
            metadataByValue.Count,
            scannedWords,
            maskedWords,
            matches.Count,
            string.Join(",", categoryCounts),
            canonicalMatches,
            $"records={matches.Count};offsets={canonicalRecordBoundaries};sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRecordBoundaries)))}",
            $"records={matches.Count};value_categories={canonicalValueCategories};sha256={Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValueCategories)))}");
    }

    public static EaglMorphInspection Inspect(ReadOnlySpan<byte> data, ElfInspection elf)
    {
        ArgumentNullException.ThrowIfNull(elf);

        var morphSymbol = elf.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith(MorphMarker, StringComparison.Ordinal));
        if (morphSymbol is null)
        {
            return new EaglMorphInspection(
                false,
                null,
                Array.Empty<EaglMorphNamedString>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<EaglMorphPointerEvidence>(),
                Array.Empty<EaglMorphShapePointer>(),
                Array.Empty<EaglMorphAttributePointer>(),
                Array.Empty<string>());
        }

        if (morphSymbol.SectionIndex == 0 || morphSymbol.SectionIndex >= elf.Sections.Count)
        {
            throw new InvalidDataException("The EAGL morph symbol is not backed by a section.");
        }

        var section = elf.Sections[morphSymbol.SectionIndex];
        if (section.Type == 8 || section.Offset > data.Length || section.Size > (ulong)data.Length - section.Offset)
        {
            throw new InvalidDataException("The EAGL morph section is not fully file-backed.");
        }

        var namedStrings = ReadNamedStrings(data, section);
        var posePaletteBankSymbolRecords = elf.Symbols
            .Where(symbol => symbol.Name.StartsWith("__PosePaletteBank:::", StringComparison.Ordinal))
            .ToArray();
        var posePaletteBankSymbols = posePaletteBankSymbolRecords
            .Select(symbol => symbol.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var pointers = new List<EaglMorphPointerEvidence>();
        foreach (var relocation in elf.Relocations.Where(relocation =>
                     relocation.TargetSectionIndex == morphSymbol.SectionIndex))
        {
            if (relocation.Offset > section.Size || sizeof(uint) > section.Size - relocation.Offset)
            {
                continue;
            }

            var fileOffset = checked((int)((ulong)section.Offset + relocation.Offset));
            var targetOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(fileOffset, sizeof(uint)));
            var targetString = TryReadNamedString(data, section, targetOffset);
            pointers.Add(new EaglMorphPointerEvidence(
                relocation.Offset,
                targetOffset,
                targetString));
        }

        var shapePointers = pointers
            .Where(pointer => pointer.TargetString is not null &&
                              pointer.TargetString.StartsWith("BS_", StringComparison.Ordinal))
            .Select(pointer => new EaglMorphShapePointer(pointer.FieldOffset, pointer.TargetString!))
            .ToArray();
        var attributePointers = pointers
            .Where(pointer => pointer.TargetString is "Coordinates" or "Normals")
            .Select(pointer => new EaglMorphAttributePointer(pointer.FieldOffset, pointer.TargetString!))
            .ToArray();

        return new EaglMorphInspection(
            true,
            morphSymbol.Name,
            namedStrings,
            namedStrings
                .Where(item => item.Value.StartsWith("BS_", StringComparison.Ordinal))
                .Select(item => item.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            namedStrings
                .Where(item => string.Equals(item.Value, "Coordinates", StringComparison.Ordinal) ||
                               string.Equals(item.Value, "Normals", StringComparison.Ordinal))
                .Select(item => item.Value)
                .Distinct(StringComparer.Ordinal)
                .ToArray(),
            pointers,
            shapePointers,
            attributePointers,
            posePaletteBankSymbols)
        {
            PosePaletteBankSymbolRecords = posePaletteBankSymbolRecords,
        };
    }

    /// <summary>
    /// Returns bounded raw-byte neighborhoods around relocated morph fields.
    /// The offsets remain section-relative and the bytes are intentionally not
    /// assigned to a guessed shape, vertex, bone, or animation record.
    /// </summary>
    public static IReadOnlyList<EaglMorphPayloadWindow> InspectPayloadWindows(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        int radius = 16)
    {
        ArgumentNullException.ThrowIfNull(elf);
        radius = Math.Clamp(radius, 0, 256);
        var morphSymbol = elf.Symbols.FirstOrDefault(symbol =>
            symbol.Name.StartsWith(MorphMarker, StringComparison.Ordinal));
        if (morphSymbol is null || morphSymbol.SectionIndex == 0 ||
            morphSymbol.SectionIndex >= elf.Sections.Count)
        {
            return Array.Empty<EaglMorphPayloadWindow>();
        }

        var section = elf.Sections[morphSymbol.SectionIndex];
        if (section.Type == 8 || section.Offset > data.Length ||
            section.Size > (ulong)data.Length - section.Offset)
        {
            return Array.Empty<EaglMorphPayloadWindow>();
        }

        var fields = elf.Relocations
            .Where(relocation => relocation.TargetSectionIndex == morphSymbol.SectionIndex &&
                                 relocation.Offset < section.Size)
            .Select(relocation => relocation.Offset)
            .Distinct()
            .OrderBy(offset => offset)
            .ToArray();
        var windows = new List<EaglMorphPayloadWindow>();
        foreach (var field in fields)
        {
            var start = field > (uint)radius ? field - (uint)radius : 0;
            var end = Math.Min(section.Size, checked(field + sizeof(uint) + (uint)radius));
            var length = checked((int)(end - start));
            var fileOffset = checked((int)((ulong)section.Offset + start));
            var bytes = data.Slice(fileOffset, length).ToArray();
            windows.Add(new EaglMorphPayloadWindow(
                start,
                bytes,
                fields.Where(candidate => candidate >= start && candidate < end).ToArray()));
        }

        return windows;
    }

    private static string? TryReadNamedString(
        ReadOnlySpan<byte> data,
        ElfSection section,
        uint targetOffset)
    {
        if (targetOffset >= section.Size)
        {
            return null;
        }

        var start = checked((int)((ulong)section.Offset + targetOffset));
        var remaining = data.Slice(start, checked((int)(section.Size - targetOffset)));
        var terminator = remaining.IndexOf((byte)0);
        if (terminator < 0)
        {
            return null;
        }

        var value = Encoding.ASCII.GetString(remaining[..terminator]);
        return value.StartsWith("BS_", StringComparison.Ordinal) ||
               string.Equals(value, "Coordinates", StringComparison.Ordinal) ||
               string.Equals(value, "Normals", StringComparison.Ordinal)
            ? value
            : null;
    }

    private static IReadOnlyList<EaglMorphNamedString> ReadNamedStrings(
        ReadOnlySpan<byte> data,
        ElfSection section)
    {
        var strings = new List<EaglMorphNamedString>();
        var sectionBytes = data.Slice(checked((int)section.Offset), checked((int)section.Size));
        var start = -1;
        for (var index = 0; index <= sectionBytes.Length; index++)
        {
            var printable = index < sectionBytes.Length &&
                            sectionBytes[index] >= 0x20 &&
                            sectionBytes[index] <= 0x7E;
            if (printable && start < 0)
            {
                start = index;
            }
            else if (!printable && start >= 0)
            {
                var value = Encoding.ASCII.GetString(sectionBytes[start..index]);
                if (value.StartsWith("BS_", StringComparison.Ordinal) ||
                    string.Equals(value, "Coordinates", StringComparison.Ordinal) ||
                    string.Equals(value, "Normals", StringComparison.Ordinal))
                {
                    strings.Add(new EaglMorphNamedString(checked((uint)start), value));
                }
                start = -1;
            }
        }

        return strings;
    }
}
