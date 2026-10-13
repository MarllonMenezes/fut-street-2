using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalVertexRecordAssociationRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int FetchSlotCount,
    string FetchSlotStrides,
    int SameOrdinalRecordCount,
    int UniqueOpaqueSequenceCount,
    int MatchedRecordCount,
    string RecordKeys,
    bool CompleteFiveSlotFetchSequence,
    bool UniqueOpaqueSequence,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalVertexRecordAssociationSummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ValidatedVertexStreamRows,
    int ObjectsWithValidatedVertexStreams,
    int PrimitiveRows,
    int RowsWithOpaqueOrdinalRecords,
    int RowsWithUniqueOpaqueSequences,
    int CompleteFiveSlotFetchRows,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Audits only an opaque structural join in the original EAGL path. A
/// primitive's observed five fetch-slot stride sequence is compared with the
/// unique consecutive category/ordinal sequence in validated __VB symbol
/// records. The result does not name an attribute, decode a vertex field, or
/// promote the join to geometry, UV, material, skinning, rigging, animation,
/// morph or GPU semantics.
/// </summary>
public static class EaglOriginalVertexRecordAssociationAudit
{
    public const string Status = "verified-bounded-original-opaque-vertex-record-association";
    public const string Provenance =
        "same-trunk-raw-EAGL-objects+opaque-fetch-slot-strides+validated-__VB-symbol-record-keys;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalVertexRecordAssociationSummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalVertexRecordAssociationRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalVertexRecordAssociationRow>();
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

        var packagesLoaded = 0;
        var objectCandidates = 0;
        var objectsLoaded = 0;
        var validatedVertexStreamRows = 0;
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
                    continue;
                }

                try
                {
                    var objectData = BigArchiveReader.ReadEntry(expandedModel, objectEntry.Name);
                    var companionData = BigArchiveReader.ReadEntry(expandedModel, companionEntry.Name);
                    var inspection = EaglObjectReader.Inspect(objectData, companionData);
                    var streams = EaglVertexBufferReader.Inspect(objectData, inspection.Elf);
                    validatedVertexStreamRows += streams.Count;
                    objectsWithValidatedVertexStreams += streams.Count > 0 ? 1 : 0;
                    var primitives = EaglModelReader.Inspect(
                        objectData,
                        inspection.Elf,
                        objectBounds: null,
                        vertexStreams: Array.Empty<EaglVertexBufferStream>());
                    foreach (var primitive in primitives)
                    {
                        outputRows.Add(InspectPrimitive(
                            modelEntry.Name,
                            objectEntry.Name,
                            primitive,
                            streams));
                    }
                    objectsLoaded++;
                }
                catch (Exception exception) when (IsBoundedReadFailure(exception))
                {
                    rejectedObjects++;
                }
            }
        }

        var associationRows = outputRows.Where(row => row.UniqueOpaqueSequence).ToArray();
        return new EaglOriginalVertexRecordAssociationSummary(
            modelArchivePath,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            validatedVertexStreamRows,
            objectsWithValidatedVertexStreams,
            outputRows.Count,
            outputRows.Count(row => row.SameOrdinalRecordCount > 0),
            associationRows.Length,
            outputRows.Count(row => row.CompleteFiveSlotFetchSequence),
            rejectedPackages,
            rejectedObjects,
            modelEntries.Length > 0 &&
            packagesLoaded == modelEntries.Length &&
            objectCandidates > 0 &&
            objectsLoaded > 0 &&
            outputRows.Count > 0 &&
            associationRows.Length > 0 &&
            rejectedPackages == 0 &&
            rejectedObjects == 0
                ? Status
                : "incomplete-original-opaque-vertex-record-association",
            Provenance,
            OriginalSemantics);
    }

    public static EaglOriginalVertexRecordAssociationRow InspectPrimitive(
        string modelArchiveName,
        string modelObjectName,
        EaglModelPrimitive primitive,
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(streams);

        var keyedStreams = streams
            .Select(stream =>
            {
                return TryParseOpaqueVertexRecordKey(stream.SymbolName, out var category, out var ordinal)
                    ? new KeyedStream(stream, category, ordinal)
                    : null;
            })
            .Where(item => item is not null && item.Ordinal == primitive.PrimitiveIndex)
            .Select(item => item!)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        var slots = primitive.VertexFetchSlots
            .OrderBy(slot => slot.Slot)
            .ToArray();
        var completeFiveSlotFetch = primitive.RenderCommandStream.IsComplete &&
                                    slots.Length == 5 &&
                                    slots.Select(slot => slot.Slot)
                                        .SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
                                    slots.All(slot => slot.Stride > 0);
        var sequences = FindUniqueOpaqueSequences(slots, keyedStreams);
        var matched = sequences.Count == 1 ? sequences[0] : Array.Empty<KeyedStream>();
        var recordKeys = string.Join("|", keyedStreams.Select(item =>
            $"category={item.Category};ordinal={item.Ordinal};stride={item.Stream.Stride};bytes={item.Stream.ByteLength};symbol={Field(item.Stream.SymbolName)}"));
        var unique = completeFiveSlotFetch && sequences.Count == 1;
        return new EaglOriginalVertexRecordAssociationRow(
            modelArchiveName,
            modelObjectName,
            primitive.LayerIndex,
            primitive.PrimitiveIndex,
            slots.Length,
            string.Join(",", slots.Select(slot => slot.Stride.ToString(CultureInfo.InvariantCulture))),
            keyedStreams.Length,
            sequences.Count,
            matched.Length,
            recordKeys,
            completeFiveSlotFetch,
            unique,
            unique ? Status : "opaque-incomplete-original-vertex-record-association",
            OriginalSemantics,
            unique ? string.Empty : "no unique opaque category/ordinal sequence with the observed slot strides");
    }

    public static bool TryParseOpaqueVertexRecordKey(
        string symbolName,
        out int category,
        out uint ordinal)
    {
        category = 0;
        ordinal = 0;
        var marker = symbolName.IndexOf("__GPvb_", StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var categoryStart = marker + "__GPvb_".Length;
        var categoryEnd = symbolName.IndexOf('_', categoryStart);
        if (categoryEnd <= categoryStart ||
            !int.TryParse(
                symbolName.AsSpan(categoryStart, categoryEnd - categoryStart),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out category))
        {
            return false;
        }

        var ordinalStart = categoryEnd + 1;
        var ordinalEnd = symbolName.IndexOf('_', ordinalStart);
        return ordinalEnd > ordinalStart &&
               uint.TryParse(
                   symbolName.AsSpan(ordinalStart, ordinalEnd - ordinalStart),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out ordinal);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalVertexRecordAssociationRow> rows)
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
                "fetch_slot_count",
                "fetch_slot_strides",
                "same_ordinal_record_count",
                "unique_opaque_sequence_count",
                "matched_record_count",
                "record_keys",
                "complete_five_slot_fetch_sequence",
                "unique_opaque_sequence",
                "status",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.FetchSlotCount.ToString(CultureInfo.InvariantCulture),
            Field(row.FetchSlotStrides),
            row.SameOrdinalRecordCount.ToString(CultureInfo.InvariantCulture),
            row.UniqueOpaqueSequenceCount.ToString(CultureInfo.InvariantCulture),
            row.MatchedRecordCount.ToString(CultureInfo.InvariantCulture),
            Field(row.RecordKeys),
            row.CompleteFiveSlotFetchSequence.ToString(),
            row.UniqueOpaqueSequence.ToString(),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalVertexRecordAssociationSummary summary)
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
                "validated_vertex_stream_rows",
                "objects_with_validated_vertex_streams",
                "primitive_rows",
                "rows_with_opaque_ordinal_records",
                "rows_with_unique_opaque_sequences",
                "complete_five_slot_fetch_rows",
                "rejected_packages",
                "rejected_objects",
                "status",
                "provenance",
                "original_semantics",
            }),
            string.Join("\t", new[]
            {
                Field(summary.ModelArchivePath),
                summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
                summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture),
                summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
                summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture),
                summary.ValidatedVertexStreamRows.ToString(CultureInfo.InvariantCulture),
                summary.ObjectsWithValidatedVertexStreams.ToString(CultureInfo.InvariantCulture),
                summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithOpaqueOrdinalRecords.ToString(CultureInfo.InvariantCulture),
                summary.RowsWithUniqueOpaqueSequences.ToString(CultureInfo.InvariantCulture),
                summary.CompleteFiveSlotFetchRows.ToString(CultureInfo.InvariantCulture),
                summary.RejectedPackages.ToString(CultureInfo.InvariantCulture),
                summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
                Field(summary.Status),
                Field(summary.Provenance),
                Field(summary.OriginalSemantics),
            }),
        };
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static IReadOnlyList<KeyedStream[]> FindUniqueOpaqueSequences(
        IReadOnlyList<EaglVertexFetchSlot> slots,
        IReadOnlyList<KeyedStream> keyedStreams)
    {
        if (slots.Count != 5 ||
            !slots.Select(slot => slot.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }))
        {
            return Array.Empty<KeyedStream[]>();
        }

        var sequences = new List<KeyedStream[]>();
        foreach (var baseCategory in keyedStreams.Select(item => item.Category).Distinct().OrderBy(value => value))
        {
            var sequence = new List<KeyedStream>(5);
            var valid = true;
            for (var slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                var category = baseCategory + slotIndex;
                var matches = keyedStreams
                    .Where(item => item.Category == category && item.Stream.Stride == slots[slotIndex].Stride)
                    .ToArray();
                if (matches.Length != 1)
                {
                    valid = false;
                    break;
                }
                sequence.Add(matches[0]);
            }

            if (valid)
            {
                sequences.Add(sequence.ToArray());
            }
        }

        return sequences;
    }

    private sealed record KeyedStream(
        EaglVertexBufferStream Stream,
        int Category,
        uint Ordinal);

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
