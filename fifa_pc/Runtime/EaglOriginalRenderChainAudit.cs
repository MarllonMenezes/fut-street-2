using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalRenderChainRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    uint RenderDescriptorOffset,
    uint RenderMethodOffset,
    uint GlobalParametersOffset,
    uint CommandStreamOffset,
    int CommandCount,
    int CommandBytes,
    bool CommandStreamComplete,
    bool StateRelocationPresent,
    string StateSymbol,
    int StateAssignmentCount,
    string StateAssignments,
    bool HasShapeNameAssignment,
    bool HasPrimitiveTypeAssignment,
    int FetchSlotCount,
    string FetchSlotIds,
    bool HasCompleteFiveSlotFetchSequence,
    bool StateFetchOverlap,
    bool StateShapePrimitiveTypeOverlap,
    string CommandBytesSha256,
    string Status,
    string OriginalSemantics,
    string Issue)
{
    public int CommandSectionIndex { get; init; }
    public string CommandIds { get; init; } = string.Empty;
}

public sealed record EaglOriginalRenderChainSummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ModelSymbolRows,
    int BoundsSymbolRows,
    int RenderMethodSymbolRows,
    int VertexBufferSymbolRows,
    int ValidatedVertexStreamRows,
    int ObjectsWithBounds,
    int ObjectsWithValidatedVertexStreams,
    int PrimitiveRows,
    int ValidChainRows,
    int CompleteCommandStreams,
    int StateRelocationRows,
    int ShapeNameStateRows,
    int PrimitiveTypeStateRows,
    int FetchSlotRows,
    int CompleteFiveSlotFetchRows,
    int StateFetchOverlapRows,
    int StateShapePrimitiveTypeOverlapRows,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics,
    string StateFetchControlStatus,
    string VertexRecordControlStatus);

public sealed record EaglOriginalRenderChainAuditResult(
    IReadOnlyList<EaglOriginalRenderChainRow> Rows,
    EaglOriginalRenderChainSummary Summary);

public sealed record EaglRawModelObject(
    string ArchiveName,
    string ObjectName,
    byte[] Data,
    ElfInspection Elf,
    IReadOnlyList<EaglModelPrimitive> Primitives);

/// <summary>
/// Audits the raw model/descriptor/render-method/global-parameter/command
/// chain in the split EAGL objects. It deliberately does not load the
/// candidate mesh, SHPX texture, or any candidate material path. Offsets,
/// state symbol text and command bytes remain structural evidence; command
/// IDs, state assignments and fetch slots are retained only as bounded raw
/// structure. They are not assigned to vertex attributes, materials, UVs,
/// skinning, rigging, animation, morphs or GPU operations here.
/// </summary>
public static class EaglOriginalRenderChainAudit
{
    public const string Status = "verified-bounded-original-render-chain-structure";
    public const string Provenance =
        "same-trunk-raw-EAGL-objects+model-descriptor-chain+opaque-command-streams;candidate-mesh-not-used;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";
    public const string StateFetchControlStatus =
        "verified-bounded-original-state-population-negative-control";
    public const string VertexRecordControlStatus =
        "verified-bounded-original-vertex-record-structure";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalRenderChainAuditResult Audit(
        GameAssetStore store,
        string modelArchivePath,
        Action<EaglRawModelObject>? inspectObject = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        using var topLevelStream = store.OpenRaw(modelArchivePath);
        var topLevelArchive = BigArchiveReader.Inspect(topLevelStream);
        var xshStems = topLevelArchive.Entries
            .Where(entry => string.Equals(
                Path.GetExtension(entry.Name),
                ".xsh",
                StringComparison.OrdinalIgnoreCase))
            .Select(entry => Path.GetFileNameWithoutExtension(entry.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var modelEntries = topLevelArchive.Entries
            .Where(entry =>
                string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase) &&
                xshStems.Contains(Path.GetFileNameWithoutExtension(entry.Name)))
            .OrderBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(MaximumModelPackages)
            .ToArray();

        var rows = new List<EaglOriginalRenderChainRow>();
        var packagesLoaded = 0;
        var objectCandidates = 0;
        var objectsLoaded = 0;
        var modelSymbolRows = 0;
        var boundsSymbolRows = 0;
        var renderMethodSymbolRows = 0;
        var vertexBufferSymbolRows = 0;
        var validatedVertexStreamRows = 0;
        var objectsWithBounds = 0;
        var objectsWithValidatedVertexStreams = 0;
        var rejectedPackages = 0;
        var rejectedObjects = 0;
        foreach (var modelEntry in modelEntries)
        {
            byte[] expandedModel;
            try
            {
                using var compressedStream = store.OpenBigEntry(modelArchivePath, modelEntry.Name);
                if (compressedStream.Length > MaximumCompressedBytes)
                {
                    throw new InvalidDataException("Compressed model archive exceeds the 64 MiB safety limit.");
                }

                var compressed = ReadAll(compressedStream);
                var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
                if (expandedSize > MaximumExpandedBytes)
                {
                    throw new InvalidDataException("Expanded model archive exceeds the 64 MiB safety limit.");
                }

                using var compressedMemory = new MemoryStream(compressed, writable: false);
                expandedModel = RefPackDecoder.Decode(compressedMemory, expandedSize).Data;
                packagesLoaded++;
            }
            catch (Exception exception) when (IsBoundedReadFailure(exception))
            {
                rejectedPackages++;
                rows.Add(RejectedRow(modelEntry.Name, string.Empty, exception.Message));
                continue;
            }

            BigArchiveInspection nestedArchive;
            try
            {
                nestedArchive = BigArchiveReader.Inspect(expandedModel);
            }
            catch (Exception exception) when (IsBoundedReadFailure(exception))
            {
                rejectedPackages++;
                rows.Add(RejectedRow(modelEntry.Name, string.Empty, exception.Message));
                continue;
            }

            foreach (var objectEntry in nestedArchive.Entries
                         .Where(entry => string.Equals(
                             Path.GetExtension(entry.Name),
                             ".ord",
                             StringComparison.OrdinalIgnoreCase))
                         .Take(MaximumObjectEntriesPerPackage))
            {
                objectCandidates++;
                var companionName = Path.ChangeExtension(objectEntry.Name, ".orl");
                var companionEntry = nestedArchive.Entries.FirstOrDefault(entry =>
                    string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
                if (companionEntry is null)
                {
                    rejectedObjects++;
                    rows.Add(RejectedRow(modelEntry.Name, objectEntry.Name, $"EAGL companion is missing: {companionName}"));
                    continue;
                }

                try
                {
                    var objectData = BigArchiveReader.ReadEntry(expandedModel, objectEntry.Name);
                    var companionData = BigArchiveReader.ReadEntry(expandedModel, companionEntry.Name);
                    var inspection = EaglObjectReader.Inspect(objectData, companionData);
                    var modelSymbols = inspection.Elf.Symbols.Count(symbol =>
                        symbol.Name.StartsWith("__Model", StringComparison.Ordinal));
                    var boundsSymbols = inspection.Elf.Symbols.Count(symbol =>
                        symbol.Name.StartsWith("__BBOX", StringComparison.Ordinal));
                    var renderMethodSymbols = inspection.Elf.Symbols.Count(symbol =>
                        symbol.Name.StartsWith("__RM", StringComparison.Ordinal));
                    var vertexBufferSymbols = inspection.Elf.Symbols.Count(symbol =>
                        symbol.Name.StartsWith("__VB", StringComparison.Ordinal));
                    var vertexStreams = EaglVertexBufferReader.Inspect(objectData, inspection.Elf);
                    modelSymbolRows += modelSymbols;
                    boundsSymbolRows += boundsSymbols;
                    renderMethodSymbolRows += renderMethodSymbols;
                    vertexBufferSymbolRows += vertexBufferSymbols;
                    validatedVertexStreamRows += vertexStreams.Count;
                    objectsWithBounds += inspection.Bounds is not null ? 1 : 0;
                    objectsWithValidatedVertexStreams += vertexStreams.Count > 0 ? 1 : 0;
                    var primitives = EaglModelReader.Inspect(objectData, inspection.Elf, null);
                    inspectObject?.Invoke(new EaglRawModelObject(
                        modelEntry.Name, objectEntry.Name, objectData, inspection.Elf, primitives));
                    objectsLoaded++;
                    rows.AddRange(primitives
                        .OrderBy(primitive => primitive.LayerIndex)
                        .ThenBy(primitive => primitive.PrimitiveIndex)
                        .Select(primitive => BuildRow(modelEntry.Name, objectEntry.Name, primitive)));
                }
                catch (Exception exception) when (IsBoundedReadFailure(exception))
                {
                    rejectedObjects++;
                    rows.Add(RejectedRow(modelEntry.Name, objectEntry.Name, exception.Message));
                }
            }
        }

        var validRows = rows.Count(row => row.Status == Status);
        var completeStreams = rows.Count(row => row.CommandStreamComplete);
        var stateRows = rows.Count(row => row.StateRelocationPresent);
        var shapeRows = rows.Count(row => row.HasShapeNameAssignment);
        var primitiveTypeRows = rows.Count(row => row.HasPrimitiveTypeAssignment);
        var fetchSlotRows = rows.Count(row => row.FetchSlotCount > 0);
        var completeFiveSlotFetchRows = rows.Count(row => row.HasCompleteFiveSlotFetchSequence);
        var stateFetchOverlapRows = rows.Count(row => row.StateFetchOverlap);
        var stateShapePrimitiveTypeOverlapRows = rows.Count(row => row.StateShapePrimitiveTypeOverlap);
        var stateFetchControlStatus = rows.Count > 0 &&
                                      validRows == rows.Count &&
                                      stateShapePrimitiveTypeOverlapRows == 0
            ? StateFetchControlStatus
            : "inconclusive-original-state-fetch-control";
        var summaryStatus = modelEntries.Length > 0 &&
                            packagesLoaded == modelEntries.Length &&
                            objectCandidates > 0 &&
                            objectsLoaded > 0 &&
                            modelSymbolRows > 0 &&
                            vertexBufferSymbolRows > 0 &&
                            validatedVertexStreamRows > 0 &&
                            rows.Count > 0 &&
                            validRows == rows.Count &&
                            rejectedPackages == 0 &&
                            rejectedObjects == 0
            ? Status
            : "incomplete-original-render-chain-structure";
        var summary = new EaglOriginalRenderChainSummary(
            modelArchivePath,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            modelSymbolRows,
            boundsSymbolRows,
            renderMethodSymbolRows,
            vertexBufferSymbolRows,
            validatedVertexStreamRows,
            objectsWithBounds,
            objectsWithValidatedVertexStreams,
            rows.Count,
            validRows,
            completeStreams,
            stateRows,
            shapeRows,
            primitiveTypeRows,
            fetchSlotRows,
            completeFiveSlotFetchRows,
            stateFetchOverlapRows,
            stateShapePrimitiveTypeOverlapRows,
            rejectedPackages,
            rejectedObjects,
            summaryStatus,
            Provenance,
            OriginalSemantics,
            stateFetchControlStatus,
            validatedVertexStreamRows > 0 &&
            objectsWithValidatedVertexStreams > 0 &&
            modelSymbolRows > 0 &&
            vertexBufferSymbolRows > 0
                ? VertexRecordControlStatus
                : "inconclusive-original-vertex-record-structure");
        return new EaglOriginalRenderChainAuditResult(rows, summary);
    }

    public static bool IsStructurallyValid(EaglModelPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        if (!primitive.RenderCommandStream.IsComplete ||
            primitive.RenderCommandStream.Issue is not null ||
            primitive.RenderCommandStream.Commands.Count == 0)
        {
            return false;
        }

        uint previousOffset = 0;
        var first = true;
        foreach (var command in primitive.RenderCommandStream.Commands.OrderBy(command => command.Index))
        {
            if (command.SizeWords == 0 ||
                command.RawBytes.Length != checked(command.SizeWords * 4) ||
                (!first && command.SectionOffset < previousOffset))
            {
                return false;
            }

            previousOffset = command.SectionOffset;
            first = false;
        }

        return true;
    }

    public static EaglOriginalRenderChainRow InspectPrimitive(
        string modelArchiveName,
        string modelObjectName,
        EaglModelPrimitive primitive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentNullException.ThrowIfNull(primitive);
        return BuildRow(modelArchiveName, modelObjectName, primitive);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalRenderChainRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive",
                "model_object",
                "layer",
                "primitive",
                "render_descriptor_offset",
                "render_method_offset",
                "global_parameters_offset",
                "command_stream_offset",
                "command_section_index",
                "command_count",
                "command_bytes",
                "command_stream_complete",
                "state_relocation_present",
                "state_symbol",
                "state_assignment_count",
                "state_assignments",
                "has_shapename_assignment",
                "has_primitive_type_assignment",
                "fetch_slot_count",
                "fetch_slot_ids",
                "complete_five_slot_fetch_sequence",
                "state_fetch_overlap",
                "state_shapename_primitive_type_overlap",
                "command_bytes_sha256",
                "status",
                "original_semantics",
                "issue",
                "command_ids",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            $"0x{row.RenderDescriptorOffset:X}",
            $"0x{row.RenderMethodOffset:X}",
            $"0x{row.GlobalParametersOffset:X}",
            $"0x{row.CommandStreamOffset:X}",
            row.CommandSectionIndex.ToString(CultureInfo.InvariantCulture),
            row.CommandCount.ToString(CultureInfo.InvariantCulture),
            row.CommandBytes.ToString(CultureInfo.InvariantCulture),
            row.CommandStreamComplete.ToString(),
            row.StateRelocationPresent.ToString(),
            Field(row.StateSymbol),
            row.StateAssignmentCount.ToString(CultureInfo.InvariantCulture),
            Field(row.StateAssignments),
            row.HasShapeNameAssignment.ToString(),
            row.HasPrimitiveTypeAssignment.ToString(),
            row.FetchSlotCount.ToString(CultureInfo.InvariantCulture),
            Field(row.FetchSlotIds),
            row.HasCompleteFiveSlotFetchSequence.ToString(),
            row.StateFetchOverlap.ToString(),
            row.StateShapePrimitiveTypeOverlap.ToString(),
            Field(row.CommandBytesSha256),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
            Field(row.CommandIds),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalRenderChainSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var lines = new[]
        {
            string.Join("\t", new[]
            {
                "model_archive_path",
                "model_package_candidates",
                "model_packages_loaded",
                "object_candidates",
                "objects_loaded",
                "model_symbol_rows",
                "bounds_symbol_rows",
                "render_method_symbol_rows",
                "vertex_buffer_symbol_rows",
                "validated_vertex_stream_rows",
                "objects_with_bounds",
                "objects_with_validated_vertex_streams",
                "primitive_rows",
                "valid_chain_rows",
                "complete_command_streams",
                "state_relocation_rows",
                "shapename_state_rows",
                "primitive_type_state_rows",
                "fetch_slot_rows",
                "complete_five_slot_fetch_rows",
                "state_fetch_overlap_rows",
                "state_shapename_primitive_type_overlap_rows",
                "rejected_packages",
                "rejected_objects",
                "status",
                "provenance",
                "original_semantics",
                "state_fetch_control_status",
                "vertex_record_control_status",
            }),
            string.Join("\t", new[]
            {
                Field(summary.ModelArchivePath),
                summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
                summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture),
                summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
                summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture),
                summary.ModelSymbolRows.ToString(CultureInfo.InvariantCulture),
                summary.BoundsSymbolRows.ToString(CultureInfo.InvariantCulture),
                summary.RenderMethodSymbolRows.ToString(CultureInfo.InvariantCulture),
                summary.VertexBufferSymbolRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidatedVertexStreamRows.ToString(CultureInfo.InvariantCulture),
                summary.ObjectsWithBounds.ToString(CultureInfo.InvariantCulture),
                summary.ObjectsWithValidatedVertexStreams.ToString(CultureInfo.InvariantCulture),
                summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture),
                summary.ValidChainRows.ToString(CultureInfo.InvariantCulture),
                summary.CompleteCommandStreams.ToString(CultureInfo.InvariantCulture),
                summary.StateRelocationRows.ToString(CultureInfo.InvariantCulture),
                summary.ShapeNameStateRows.ToString(CultureInfo.InvariantCulture),
                summary.PrimitiveTypeStateRows.ToString(CultureInfo.InvariantCulture),
                summary.FetchSlotRows.ToString(CultureInfo.InvariantCulture),
                summary.CompleteFiveSlotFetchRows.ToString(CultureInfo.InvariantCulture),
                summary.StateFetchOverlapRows.ToString(CultureInfo.InvariantCulture),
                summary.StateShapePrimitiveTypeOverlapRows.ToString(CultureInfo.InvariantCulture),
                summary.RejectedPackages.ToString(CultureInfo.InvariantCulture),
                summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
                Field(summary.Status),
                Field(summary.Provenance),
                Field(summary.OriginalSemantics),
                Field(summary.StateFetchControlStatus),
                Field(summary.VertexRecordControlStatus),
            }),
        };
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static EaglOriginalRenderChainRow BuildRow(
        string modelArchiveName,
        string modelObjectName,
        EaglModelPrimitive primitive)
    {
        var commands = primitive.RenderCommandStream.Commands
            .OrderBy(command => command.Index)
            .ToArray();
        var commandBytes = commands.SelectMany(command => command.RawBytes).ToArray();
        var stateAssignments = string.Join(";", primitive.StateAssignments.Select(assignment =>
            $"{assignment.Name}={assignment.Value}"));
        var hasShapeName = primitive.StateAssignments.Any(assignment =>
            string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal));
        var hasPrimitiveType = primitive.StateAssignments.Any(assignment =>
            string.Equals(assignment.Name, "SetPrimitiveType", StringComparison.Ordinal));
        var fetchSlots = primitive.VertexFetchSlots
            .OrderBy(slot => slot.Slot)
            .ToArray();
        var hasCompleteFiveSlotFetchSequence =
            primitive.RenderCommandStream.IsComplete &&
            fetchSlots.Length == 5 &&
            fetchSlots.Select(slot => slot.Slot)
                .SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
            fetchSlots.All(slot => slot.Stride > 0);
        var stateFetchOverlap = hasShapeName && hasCompleteFiveSlotFetchSequence;
        var stateShapePrimitiveTypeOverlap = hasShapeName && hasPrimitiveType;
        var valid = IsStructurallyValid(primitive);
        return new EaglOriginalRenderChainRow(
            modelArchiveName,
            modelObjectName,
            primitive.LayerIndex,
            primitive.PrimitiveIndex,
            primitive.RenderDescriptorOffset,
            primitive.RenderMethodOffset,
            primitive.GlobalParametersOffset,
            commands.Length == 0 ? 0u : commands[0].SectionOffset,
            commands.Length,
            commandBytes.Length,
            primitive.RenderCommandStream.IsComplete,
            !string.IsNullOrEmpty(primitive.PrimitiveStateSymbol),
            primitive.PrimitiveStateSymbol ?? string.Empty,
            primitive.StateAssignments.Count,
            stateAssignments,
            hasShapeName,
            hasPrimitiveType,
            fetchSlots.Length,
            string.Join(",", fetchSlots.Select(slot => slot.Slot.ToString(CultureInfo.InvariantCulture))),
            hasCompleteFiveSlotFetchSequence,
            stateFetchOverlap,
            stateShapePrimitiveTypeOverlap,
            Convert.ToHexString(SHA256.HashData(commandBytes)),
            valid ? Status : "incomplete-original-render-chain-structure",
            OriginalSemantics,
            primitive.RenderCommandStream.Issue ?? string.Empty)
        {
            CommandSectionIndex = primitive.SourceSectionIndex,
            CommandIds = string.Join(",", commands.Select(command => command.CommandId.ToString(CultureInfo.InvariantCulture))),
        };
    }

    private static EaglOriginalRenderChainRow RejectedRow(
        string modelArchiveName,
        string modelObjectName,
        string issue) =>
        new(
            modelArchiveName,
            modelObjectName,
            -1,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            string.Empty,
            0,
            string.Empty,
            false,
            false,
            0,
            string.Empty,
            false,
            false,
            false,
            string.Empty,
            "rejected-original-render-chain",
            OriginalSemantics,
            issue);

    private static byte[] ReadAll(Stream stream)
    {
        if (stream.Length > int.MaxValue)
        {
            throw new InvalidDataException("Input stream is too large.");
        }

        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return data;
    }

    private static bool IsBoundedReadFailure(Exception exception) =>
        exception is InvalidDataException or
        EndOfStreamException or
        IOException or
        OverflowException or
        ArgumentOutOfRangeException or
        FileNotFoundException;

    private static string Field(string value) =>
        (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }
}
