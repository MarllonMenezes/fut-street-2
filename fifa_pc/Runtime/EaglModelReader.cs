using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record EaglModelPrimitive(
    int LayerIndex,
    uint PrimitiveIndex,
    uint RenderDescriptorOffset,
    uint RenderMethodOffset,
    uint GlobalParametersOffset,
    string? PrimitiveStateSymbol,
    IReadOnlyList<EaglStateAssignment> StateAssignments,
    string? PrimitiveType,
    IReadOnlyList<EaglVertexBufferStream> BoundsMatchingVertexBuffers,
    IReadOnlyList<EaglVertexFetchSlot> VertexFetchSlots,
    IReadOnlyList<EaglVertexFetchBinding> VertexFetchBindings,
    bool HasConsistentVertexLayout,
    EaglRenderCommandStream RenderCommandStream)
{
    public int SourceSectionIndex { get; init; }
}

public sealed record EaglVertexFetchSlot(uint Slot, uint AuxiliaryValue, uint Stride);

public sealed record EaglVertexFetchBinding(
    EaglVertexFetchSlot FetchSlot,
    EaglVertexBufferStream Stream);

/// <summary>
/// Lossless name/value evidence extracted from an EAGL state symbol. The
/// assignment is not interpreted as a PC renderer state here.
/// </summary>
public sealed record EaglStateAssignment(string Name, string Value);

/// <summary>
/// Lossless evidence for one EAGL render command. CommandId is intentionally
/// not mapped to a guessed semantic; raw bytes and relocations remain visible.
/// </summary>
public sealed record EaglRenderCommandEvidence(
    int Index,
    uint SectionOffset,
    ushort CommandId,
    ushort SizeWords,
    byte[] RawBytes,
    IReadOnlyList<string> RelocationSymbols);

public sealed record EaglRenderCommandStream(
    IReadOnlyList<EaglRenderCommandEvidence> Commands,
    bool IsComplete,
    string? Issue);

/// <summary>
/// Inspects the EAGL model/layer/render-descriptor chain and correlates
/// primitive-numbered VB symbols with model bounds. It reports candidates;
/// it does not assume their sequence is a valid triangle/index topology.
/// </summary>
public static class EaglModelReader
{
    private const uint ModelStructSize = 0xD0;
    private const uint MaximumLayerCount = 128;
    private const uint MaximumPrimitiveCount = 1024;

    public static IReadOnlyList<EaglModelPrimitive> Inspect(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglAxisAlignedBounds? objectBounds,
        IReadOnlyList<EaglVertexBufferStream>? vertexStreams = null)
    {
        var streams = vertexStreams ?? EaglVertexBufferReader.Inspect(data, elf);
        var primitives = new List<EaglModelPrimitive>();
        foreach (var model in elf.Symbols.Where(symbol =>
                     symbol.Name.StartsWith("__Model:::", StringComparison.Ordinal)))
        {
            if (model.SectionIndex == 0 || model.SectionIndex >= elf.Sections.Count)
            {
                continue;
            }

            var section = elf.Sections[model.SectionIndex];
            if (section.Type == 8 || model.Value > section.Size || ModelStructSize > section.Size - model.Value)
            {
                continue;
            }

            if (!TryReadWord(data, section, model.Value + 0x9C, out var layerCount) ||
                layerCount == 0 || layerCount > MaximumLayerCount ||
                !TryReadWord(data, section, model.Value + 0xCC, out var layersOffset) ||
                !TryReadWord(data, section, layersOffset, out var layerHeader))
            {
                continue;
            }

            var oldLayerLayout = layerHeader == 0xA0000000;
            var modelName = model.Name["__Model:::".Length..];
            var boundsMatchingParts = new Dictionary<uint, List<EaglVertexBufferStream>>();
            if (objectBounds is not null)
            {
                foreach (var stream in streams)
                {
                    if (!string.IsNullOrEmpty(modelName) &&
                        !stream.SymbolName.Contains(modelName, StringComparison.Ordinal) ||
                        !TryGetPrimitiveOrdinal(stream.SymbolName, out var primitiveOrdinal) ||
                        !EaglVertexBufferReader.TryMeasureFloat3(data, elf, stream, out var streamBounds) ||
                        streamBounds is null || !EaglVertexBufferReader.FitsBounds(streamBounds, objectBounds))
                    {
                        continue;
                    }

                    if (!boundsMatchingParts.TryGetValue(primitiveOrdinal, out var candidates))
                    {
                        candidates = new List<EaglVertexBufferStream>();
                        boundsMatchingParts.Add(primitiveOrdinal, candidates);
                    }
                    candidates.Add(stream);
                }
            }

            var initialLayerCursor = (ulong)layersOffset + 4u + (oldLayerLayout ? 8u : 0u);
            if (initialLayerCursor > uint.MaxValue)
            {
                continue;
            }
            var layerCursor = (uint)initialLayerCursor;
            for (var layerIndex = 0u; layerIndex < layerCount; layerIndex++)
            {
                if (oldLayerLayout)
                {
                    if (layerCursor > uint.MaxValue - 8u)
                    {
                        break;
                    }
                    layerCursor += 8u;
                }

                if (!TryReadWord(data, section, layerCursor, out var primitiveCount) ||
                    primitiveCount > MaximumPrimitiveCount)
                {
                    break;
                }

                if (layerCursor > uint.MaxValue - 4u)
                {
                    break;
                }
                var descriptorTable = layerCursor + 4u;
                var descriptorStride = oldLayerLayout ? 8u : 4u;
                for (var primitiveIndex = 0u; primitiveIndex < primitiveCount; primitiveIndex++)
                {
                    var descriptorFieldWide = (ulong)descriptorTable +
                                              (ulong)primitiveIndex * descriptorStride +
                                              (oldLayerLayout ? 4u : 0u);
                    if (descriptorFieldWide > uint.MaxValue)
                    {
                        break;
                    }
                    var descriptorField = (uint)descriptorFieldWide;
                    if (!TryReadWord(data, section, descriptorField, out var descriptorOffset) ||
                        descriptorOffset > uint.MaxValue - 4u ||
                        !TryReadWord(data, section, descriptorOffset, out var renderMethodOffset) ||
                        !TryReadWord(data, section, descriptorOffset + 4u, out _) ||
                        renderMethodOffset > uint.MaxValue - 8u ||
                        !TryReadWord(data, section, renderMethodOffset, out var commandStreamOffset) ||
                        !TryReadWord(data, section, renderMethodOffset + 8u, out _))
                    {
                        continue;
                    }

                    var globalParametersOffset = descriptorOffset + 4u;
                    if (globalParametersOffset > section.Size || 16u > section.Size - globalParametersOffset)
                    {
                        continue;
                    }

                    var stateRelocation = elf.Relocations.FirstOrDefault(relocation =>
                        relocation.TargetSectionIndex == model.SectionIndex &&
                        relocation.Offset == globalParametersOffset + 4u &&
                        relocation.Type == 2);
                    var stateSymbol = stateRelocation?.SymbolName;
                    var stateAssignments = ParseStateAssignments(stateSymbol);
                    var type = ParsePrimitiveType(stateSymbol);
                    var matchingStreams = boundsMatchingParts.TryGetValue(primitiveIndex, out var names)
                        ? names.ToArray()
                        : Array.Empty<EaglVertexBufferStream>();
                    var commandStream = InspectCommandStream(
                        data,
                        elf,
                        model.SectionIndex,
                        commandStreamOffset);
                    var vertexFetchSlots = ReadVertexFetchSlots(commandStream);
                    var vertexFetchBindings = matchingStreams.Length == 1
                        ? ResolveVertexFetchBindings(
                            streams,
                            modelName,
                            primitiveIndex,
                            matchingStreams[0],
                            vertexFetchSlots)
                        : Array.Empty<EaglVertexFetchBinding>();
                    var hasConsistentVertexLayout = vertexFetchBindings.Count == 5;
                    primitives.Add(new EaglModelPrimitive(
                        checked((int)layerIndex),
                        primitiveIndex,
                        descriptorOffset,
                        renderMethodOffset,
                        globalParametersOffset,
                        stateSymbol,
                        stateAssignments,
                        type,
                        matchingStreams,
                        vertexFetchSlots,
                        vertexFetchBindings,
                        hasConsistentVertexLayout,
                        commandStream)
                    {
                        SourceSectionIndex = model.SectionIndex,
                    });
                }

                var nextLayer = (ulong)descriptorTable + (ulong)primitiveCount * descriptorStride;
                if (nextLayer > uint.MaxValue)
                {
                    break;
                }
                layerCursor = (uint)nextLayer;
            }
        }

        return primitives;
    }

    /// <summary>
    /// Produces an opaque structural signature for the inspected primitive and
    /// command-stream records. The canonical input keeps primitive offsets,
    /// observed state text, command IDs/sizes/offsets, raw-command hashes and
    /// relocation text. It deliberately does not map any command or assignment
    /// to a GPU operation, material, vertex attribute, or animation meaning.
    /// </summary>
    public static string GetStructuralCommandStreamSignature(
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentNullException.ThrowIfNull(primitives);
        var primitiveText = primitives
            .OrderBy(primitive => primitive.LayerIndex)
            .ThenBy(primitive => primitive.PrimitiveIndex)
            .Select(primitive =>
            {
                var assignments = string.Join("|", primitive.StateAssignments
                    .Select(assignment => $"{assignment.Name}={assignment.Value}"));
                var commands = string.Join("|", primitive.RenderCommandStream.Commands
                    .Select(command =>
                    {
                        var rawHash = Convert.ToHexString(SHA256.HashData(command.RawBytes));
                        var relocations = string.Join(",", command.RelocationSymbols);
                        return $"index={command.Index};offset=0x{command.SectionOffset:X};id={command.CommandId};words={command.SizeWords};raw={rawHash};reloc={relocations}";
                    }));
                return $"layer={primitive.LayerIndex};primitive={primitive.PrimitiveIndex};descriptor=0x{primitive.RenderDescriptorOffset:X};method=0x{primitive.RenderMethodOffset:X};global=0x{primitive.GlobalParametersOffset:X};state={primitive.PrimitiveStateSymbol ?? "none"};assignments={assignments};type={primitive.PrimitiveType ?? "none"};complete={primitive.RenderCommandStream.IsComplete};issue={primitive.RenderCommandStream.Issue ?? "none"};commands={commands}";
            });
        var canonical = string.Join("||", primitiveText);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var commandIds = primitives
            .SelectMany(primitive => primitive.RenderCommandStream.Commands)
            .GroupBy(command => command.CommandId)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key}={group.Count()}");
        return $"primitives={primitives.Count};complete_streams={primitives.Count(primitive => primitive.RenderCommandStream.IsComplete)};incomplete_streams={primitives.Count(primitive => !primitive.RenderCommandStream.IsComplete)};commands={primitives.Sum(primitive => primitive.RenderCommandStream.Commands.Count)};ids={string.Join(",", commandIds)};sha256={hash}";
    }

    /// <summary>
    /// Returns the raw command bytes in the same stable primitive/command order
    /// used by the opaque command-stream inspection. Headers and payloads are
    /// preserved exactly; command IDs are not interpreted.
    /// </summary>
    public static byte[] GetRawCommandStreamBytes(
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentNullException.ThrowIfNull(primitives);
        using var stream = new MemoryStream();
        foreach (var primitive in primitives
                     .OrderBy(item => item.LayerIndex)
                     .ThenBy(item => item.PrimitiveIndex))
        {
            foreach (var command in primitive.RenderCommandStream.Commands
                         .OrderBy(item => item.Index))
            {
                stream.Write(command.RawBytes, 0, command.RawBytes.Length);
            }
        }

        return stream.ToArray();
    }

    /// <summary>
    /// Returns a structural signature for the observed primitive partitions of
    /// the raw command stream. It records only segment order, command count,
    /// byte length, completion and raw-byte hashes; command IDs and payload
    /// fields remain uninterpreted.
    /// </summary>
    public static string GetRawCommandStreamBoundarySignature(
        IReadOnlyList<EaglModelPrimitive> primitives)
    {
        ArgumentNullException.ThrowIfNull(primitives);
        var segments = new List<string>();
        var segmentIndex = 0;
        foreach (var primitive in primitives
                     .OrderBy(item => item.LayerIndex)
                     .ThenBy(item => item.PrimitiveIndex))
        {
            var commands = primitive.RenderCommandStream.Commands
                .OrderBy(item => item.Index)
                .ToArray();
            using var segmentStream = new MemoryStream();
            foreach (var command in commands)
            {
                segmentStream.Write(command.RawBytes, 0, command.RawBytes.Length);
            }

            var segmentBytes = segmentStream.ToArray();
            segments.Add(
                $"{segmentIndex}:{commands.Length}:{segmentBytes.Length}:{primitive.RenderCommandStream.IsComplete}:{Convert.ToHexString(SHA256.HashData(segmentBytes))}");
            segmentIndex++;
        }

        return $"segments={segments.Count};partitions={string.Join("|", segments)}";
    }

    public static IReadOnlyList<EaglStateAssignment> ParseStateAssignments(string? symbolName)
    {
        if (string.IsNullOrEmpty(symbolName))
        {
            return Array.Empty<EaglStateAssignment>();
        }

        var separator = symbolName.IndexOf(":::", StringComparison.Ordinal);
        var stateText = separator < 0 ? symbolName : symbolName[(separator + 3)..];
        var assignments = new List<EaglStateAssignment>();
        foreach (var token in stateText.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = token.IndexOf('=');
            if (equals <= 0 || equals == token.Length - 1)
            {
                continue;
            }

            assignments.Add(new EaglStateAssignment(
                token[..equals].Trim(),
                token[(equals + 1)..].Trim()));
        }

        return assignments;
    }

    private static string? ParsePrimitiveType(string? symbolName)
    {
        if (symbolName is null)
        {
            return null;
        }

        const string marker = "SetPrimitiveType=EAGL::";
        var markerOffset = symbolName.IndexOf(marker, StringComparison.Ordinal);
        if (markerOffset < 0)
        {
            return null;
        }

        var start = markerOffset + marker.Length;
        var end = symbolName.IndexOf(';', start);
        if (end < 0)
        {
            end = symbolName.Length;
        }
        return end == start ? null : symbolName[start..end];
    }

    private static bool TryGetPrimitiveOrdinal(string symbolName, out uint ordinal)
    {
        ordinal = 0;
        var marker = symbolName.IndexOf("__GPvb_", StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var categoryEnd = symbolName.IndexOf('_', marker + 7);
        if (categoryEnd < 0)
        {
            return false;
        }

        var ordinalStart = categoryEnd + 1;
        var ordinalEnd = symbolName.IndexOf('_', ordinalStart);
        if (ordinalEnd < 0)
        {
            return false;
        }

        return uint.TryParse(
            symbolName.AsSpan(ordinalStart, ordinalEnd - ordinalStart),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out ordinal);
    }

    private static bool TryGetStreamKey(string symbolName, out int category, out uint ordinal)
    {
        category = 0;
        ordinal = 0;
        var marker = symbolName.IndexOf("__GPvb_", StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var categoryStart = marker + 7;
        var categoryEnd = symbolName.IndexOf('_', categoryStart);
        if (categoryEnd < 0 ||
            !int.TryParse(symbolName.AsSpan(categoryStart, categoryEnd - categoryStart),
                NumberStyles.None, CultureInfo.InvariantCulture, out category))
        {
            return false;
        }

        var ordinalStart = categoryEnd + 1;
        var ordinalEnd = symbolName.IndexOf('_', ordinalStart);
        return ordinalEnd >= 0 && uint.TryParse(
            symbolName.AsSpan(ordinalStart, ordinalEnd - ordinalStart),
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out ordinal);
    }

    private static IReadOnlyList<EaglVertexFetchBinding> ResolveVertexFetchBindings(
        IReadOnlyList<EaglVertexBufferStream> streams,
        string modelName,
        uint primitiveIndex,
        EaglVertexBufferStream positionCandidate,
        IReadOnlyList<EaglVertexFetchSlot> fetchSlots)
    {
        if (!TryGetStreamKey(positionCandidate.SymbolName, out var baseCategory, out var positionOrdinal) ||
            positionOrdinal != primitiveIndex || fetchSlots.Count != 5 ||
            fetchSlots.Select(slot => slot.Slot).Distinct().Count() != 5)
        {
            return Array.Empty<EaglVertexFetchBinding>();
        }

        var orderedSlots = fetchSlots.OrderBy(slot => slot.Slot).ToArray();
        var bindings = new List<EaglVertexFetchBinding>(orderedSlots.Length);
        for (var slotIndex = 0; slotIndex < orderedSlots.Length; slotIndex++)
        {
            var slot = orderedSlots[slotIndex];
            if (slot.Slot != slotIndex || baseCategory > int.MaxValue - slotIndex)
            {
                return Array.Empty<EaglVertexFetchBinding>();
            }

            var expectedCategory = baseCategory + slotIndex;
            var matches = streams.Where(stream =>
            {
                if (!string.IsNullOrEmpty(modelName) &&
                    !stream.SymbolName.Contains(modelName, StringComparison.Ordinal))
                {
                    return false;
                }

                return TryGetStreamKey(stream.SymbolName, out var category, out var ordinal) &&
                       category == expectedCategory && ordinal == primitiveIndex;
            }).ToArray();
            if (matches.Length != 1 || matches[0].Stride != slot.Stride ||
                matches[0].ElementCount < positionCandidate.ElementCount ||
                matches[0].ElementCount > positionCandidate.ElementCount + 1u)
            {
                return Array.Empty<EaglVertexFetchBinding>();
            }

            if (slotIndex == 0 && matches[0] != positionCandidate)
            {
                return Array.Empty<EaglVertexFetchBinding>();
            }

            bindings.Add(new EaglVertexFetchBinding(slot, matches[0]));
        }

        return bindings;
    }

    public static EaglRenderCommandStream InspectCommandStream(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        int sectionIndex,
        uint commandStreamOffset,
        int maximumCommands = 256)
    {
        if (sectionIndex <= 0 || sectionIndex >= elf.Sections.Count)
        {
            return new EaglRenderCommandStream(
                Array.Empty<EaglRenderCommandEvidence>(),
                false,
                $"section index {sectionIndex} is outside the ELF section table");
        }

        var section = elf.Sections[sectionIndex];
        if (section.Type == 8)
        {
            return new EaglRenderCommandStream(
                Array.Empty<EaglRenderCommandEvidence>(),
                false,
                "command stream points to an SHT_NOBITS section");
        }

        if (commandStreamOffset > section.Size || (ulong)section.Offset + commandStreamOffset > (ulong)data.Length)
        {
            return new EaglRenderCommandStream(
                Array.Empty<EaglRenderCommandEvidence>(),
                false,
                $"command stream offset 0x{commandStreamOffset:X} is outside the section");
        }

        maximumCommands = Math.Clamp(maximumCommands, 1, 4096);
        var commands = new List<EaglRenderCommandEvidence>();
        var cursor = (ulong)commandStreamOffset;
        for (var commandIndex = 0; commandIndex < maximumCommands; commandIndex++)
        {
            if (cursor + 4u > section.Size)
            {
                return new EaglRenderCommandStream(commands, false, "command header extends beyond the ELF section");
            }

            var headerFileOffset = (ulong)section.Offset + cursor;
            if (headerFileOffset + 4u > (ulong)data.Length)
            {
                return new EaglRenderCommandStream(commands, false, "command header extends beyond the object bytes");
            }

            var header = data.Slice(checked((int)headerFileOffset), 4);
            var commandSize = BinaryPrimitives.ReadUInt16LittleEndian(header[..2]);
            var commandId = BinaryPrimitives.ReadUInt16LittleEndian(header[2..4]);
            if (commandId == 0 && commandSize == 0)
            {
                return new EaglRenderCommandStream(commands, true, null);
            }

            if (commandSize == 0)
            {
                return new EaglRenderCommandStream(commands, false, $"command {commandIndex} has zero size with id {commandId}");
            }

            var commandBytes = (ulong)commandSize * 4u;
            if (commandBytes < 4u || commandBytes > section.Size - cursor)
            {
                return new EaglRenderCommandStream(commands, false, $"command {commandIndex} exceeds the ELF section");
            }

            var commandEnd = cursor + commandBytes;
            if ((ulong)section.Offset + commandEnd > (ulong)data.Length)
            {
                return new EaglRenderCommandStream(commands, false, $"command {commandIndex} exceeds the object bytes");
            }

            var rawBytes = data.Slice(
                checked((int)headerFileOffset),
                checked((int)commandBytes)).ToArray();
            var relocations = elf.Relocations
                .Where(relocation => relocation.TargetSectionIndex == sectionIndex &&
                                     relocation.Offset >= cursor &&
                                     (ulong)relocation.Offset < commandEnd)
                .Select(relocation => $"0x{relocation.Offset:X}:{relocation.SymbolName}:type={relocation.Type}")
                .ToArray();
            commands.Add(new EaglRenderCommandEvidence(
                commandIndex,
                checked((uint)cursor),
                commandId,
                commandSize,
                rawBytes,
                relocations));
            cursor = commandEnd;
        }

        return new EaglRenderCommandStream(
            commands,
            false,
            $"command stream exceeded the {maximumCommands}-command safety limit");
    }

    private static IReadOnlyList<EaglVertexFetchSlot> ReadVertexFetchSlots(
        EaglRenderCommandStream commandStream)
    {
        var slots = new List<EaglVertexFetchSlot>();
        foreach (var command in commandStream.Commands)
        {
            if (command.CommandId != 9)
            {
                continue;
            }

            if (command.RawBytes.Length < 16)
            {
                return Array.Empty<EaglVertexFetchSlot>();
            }

            var auxiliaryValue = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(4, 4));
            var slot = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(8, 4));
            var stride = BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(12, 4));
            if (slot > 16 || stride == 0 || stride > 256)
            {
                return Array.Empty<EaglVertexFetchSlot>();
            }
            slots.Add(new EaglVertexFetchSlot(slot, auxiliaryValue, stride));
        }

        return commandStream.IsComplete ? slots : Array.Empty<EaglVertexFetchSlot>();
    }

    private static bool TryReadWord(ReadOnlySpan<byte> data, ElfSection section, uint sectionOffset, out uint value)
    {
        value = 0;
        if (section.Type == 8 || sectionOffset > section.Size || 4u > section.Size - sectionOffset)
        {
            return false;
        }

        var fileOffset = (ulong)section.Offset + sectionOffset;
        if (fileOffset + 4u > (ulong)data.Length)
        {
            return false;
        }

        value = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(checked((int)fileOffset), 4));
        return true;
    }
}
