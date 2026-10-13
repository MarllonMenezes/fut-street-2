using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalPrimitiveStreamTopologyRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int FetchSlot,
    uint FetchAuxiliaryValue,
    uint FetchStride,
    int Category,
    uint Ordinal,
    string SymbolName,
    int SectionIndex,
    uint BufferRecordOffset,
    uint DescriptorOffset,
    uint DataOffset,
    uint ByteLength,
    uint StreamStride,
    uint ElementCount,
    uint Flags,
    string PayloadSha256,
    bool CategorySequenceValid,
    bool KeyOrdinalMatchesPrimitive,
    bool FetchSlotStructureValid,
    bool FetchStrideMatchesStreamStride,
    bool ValidPayloadRange,
    bool StructuralMatch,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalPrimitiveStreamTopologySummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ObjectsWithValidatedStreams,
    int PrimitiveRows,
    int StreamRows,
    int PrimitiveRowsWithOrdinalMatchedStreams,
    int StreamRowsWithOrdinalMatch,
    int StreamRowsWithStrideMatch,
    int StreamRowsWithValidPayloadRange,
    int CompletePrimitiveRows,
    int ObjectsWithNonOverlappingPayloadRanges,
    int ReversedSlotMatchRows,
    int ShiftedOrdinalMatchRows,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Emits an object-local, slot-to-opaque-__VB topology audit. The report keeps
/// category/ordinal keys, raw offsets, range lengths, strides and flags. It
/// does not name any vertex attribute and does not assign position, geometry,
/// UV, material, skinning, rigging, animation, morph or GPU semantics.
/// </summary>
public static class EaglOriginalPrimitiveStreamTopologyAudit
{
    public const string Status = "verified-bounded-original-primitive-stream-topology";
    public const string Provenance =
        "same-trunk-raw-EAGL-command-fetches+validated-__VB-records+payload-range-metadata;primitive-stream-topology-only;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalPrimitiveStreamTopologySummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalPrimitiveStreamTopologyRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalPrimitiveStreamTopologyRow>();
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
        var objectsWithValidatedStreams = 0;
        var primitiveRows = 0;
        var rejectedPackages = 0;
        var rejectedObjects = 0;
        var completePrimitiveRows = 0;
        var objectsWithNonOverlappingRanges = 0;
        var reversedSlotMatchRows = 0;
        var shiftedOrdinalMatchRows = 0;

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
                    var combinedData = EaglObjectReader.Combine(objectData, companionData);
                    var inspection = EaglObjectReader.Inspect(combinedData);
                    var streams = EaglVertexBufferReader.Inspect(combinedData, inspection.Elf);
                    var primitives = EaglModelReader.Inspect(
                        combinedData,
                        inspection.Elf,
                        objectBounds: null,
                        vertexStreams: Array.Empty<EaglVertexBufferStream>());
                    objectsLoaded++;
                    if (streams.Count > 0)
                    {
                        objectsWithValidatedStreams++;
                    }

                    var objectRows = new List<EaglOriginalPrimitiveStreamTopologyRow>();
                    var streamGroups = streams
                        .Select(stream =>
                        {
                            return EaglOriginalVertexRecordAssociationAudit.TryParseOpaqueVertexRecordKey(
                                stream.SymbolName,
                                out var category,
                                out var ordinal)
                                ? new KeyedStream(stream, category, ordinal)
                                : null;
                        })
                        .Where(item => item is not null)
                        .Select(item => item!)
                        .GroupBy(item => item.Ordinal)
                        .ToDictionary(group => group.Key, group => group
                            .OrderBy(item => item.Category)
                            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
                            .ToArray());

                    foreach (var primitive in primitives
                                 .OrderBy(primitive => primitive.LayerIndex)
                                 .ThenBy(primitive => primitive.PrimitiveIndex))
                    {
                        primitiveRows++;
                        var primitiveRowsForObject = InspectPrimitive(
                            modelEntry.Name,
                            objectEntry.Name,
                            primitive,
                            combinedData,
                            inspection.Elf,
                            streamGroups.TryGetValue(primitive.PrimitiveIndex, out var keyedStreams)
                                ? keyedStreams
                                : Array.Empty<KeyedStream>());
                        foreach (var row in primitiveRowsForObject)
                        {
                            outputRows.Add(row);
                        }
                        objectRows.AddRange(primitiveRowsForObject);

                        if (primitiveRowsForObject.Count > 0 &&
                            primitiveRowsForObject.All(row => row.StructuralMatch))
                        {
                            completePrimitiveRows++;
                        }

                        if (primitiveRowsForObject.Count == 5 &&
                            primitiveRowsForObject.All(row => row.KeyOrdinalMatchesPrimitive))
                        {
                            if (primitiveRowsForObject
                                .OrderBy(row => row.Category)
                                .Select(row => row.FetchStride)
                                .SequenceEqual(primitiveRowsForObject
                                    .OrderByDescending(row => row.Category)
                                    .Select(row => row.StreamStride)))
                            {
                                reversedSlotMatchRows++;
                            }
                        }

                        if (primitive.PrimitiveIndex < uint.MaxValue &&
                            streamGroups.TryGetValue(primitive.PrimitiveIndex + 1, out var shiftedStreams) &&
                            MatchesFetchStrideSequence(primitive, shiftedStreams))
                        {
                            shiftedOrdinalMatchRows++;
                        }
                    }

                    if (HasNonOverlappingPayloadRanges(streams))
                    {
                        objectsWithNonOverlappingRanges++;
                    }
                }
                catch (Exception exception) when (IsBoundedReadFailure(exception))
                {
                    rejectedObjects++;
                }
            }
        }

        var status = modelEntries.Length > 0 &&
                     packagesLoaded == modelEntries.Length &&
                     objectCandidates > 0 &&
                     objectsLoaded > 0 &&
                     objectsWithValidatedStreams == objectsLoaded &&
                     primitiveRows > 0 &&
                     outputRows.Count > 0 &&
                     completePrimitiveRows > 0 &&
                     objectsWithNonOverlappingRanges == objectsLoaded &&
                     reversedSlotMatchRows == 0 &&
                     rejectedPackages == 0 &&
                     rejectedObjects == 0
            ? Status
            : "incomplete-original-primitive-stream-topology";
        return new(
            modelArchivePath,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            objectsWithValidatedStreams,
            primitiveRows,
            outputRows.Count,
            outputRows
                .GroupBy(row => (row.ModelArchiveName, row.ModelObjectName, row.LayerIndex, row.PrimitiveIndex))
                .Count(group => group.Any(row => row.KeyOrdinalMatchesPrimitive)),
            outputRows.Count(row => row.KeyOrdinalMatchesPrimitive),
            outputRows.Count(row => row.FetchStrideMatchesStreamStride),
            outputRows.Count(row => row.ValidPayloadRange),
            completePrimitiveRows,
            objectsWithNonOverlappingRanges,
            reversedSlotMatchRows,
            shiftedOrdinalMatchRows,
            rejectedPackages,
            rejectedObjects,
            status,
            Provenance,
            OriginalSemantics);
    }

    public static IReadOnlyList<EaglOriginalPrimitiveStreamTopologyRow> InspectPrimitive(
        string modelArchiveName,
        string modelObjectName,
        EaglModelPrimitive primitive,
        IReadOnlyList<KeyedStream> keyedStreams)
    {
        return InspectPrimitive(
            modelArchiveName,
            modelObjectName,
            primitive,
            ReadOnlySpan<byte>.Empty,
            null,
            keyedStreams);
    }

    private static IReadOnlyList<EaglOriginalPrimitiveStreamTopologyRow> InspectPrimitive(
        string modelArchiveName,
        string modelObjectName,
        EaglModelPrimitive primitive,
        ReadOnlySpan<byte> data,
        ElfInspection? elf,
        IReadOnlyList<KeyedStream> keyedStreams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(keyedStreams);

        var slots = primitive.VertexFetchSlots
            .OrderBy(slot => slot.Slot)
            .ToArray();
        var orderedStreams = keyedStreams
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        var categorySequenceValid = orderedStreams.Length == 5 &&
                                    orderedStreams
                                        .Select((item, index) => item.Category == orderedStreams[0].Category + index)
                                        .All(value => value);
        var completeSlotStructure = primitive.RenderCommandStream.IsComplete &&
                                    slots.Length == 5 &&
                                    slots.Select(slot => slot.Slot)
                                        .SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
                                    slots.All(slot => slot.Stride > 0);
        var rows = new List<EaglOriginalPrimitiveStreamTopologyRow>(orderedStreams.Length);
        for (var index = 0; index < orderedStreams.Length; index++)
        {
            var keyed = orderedStreams[index];
            var hasSlot = index < slots.Length;
            var slot = hasSlot ? slots[index] : new EaglVertexFetchSlot(uint.MaxValue, 0, 0);
            var keyOrdinalMatches = keyed.Ordinal == primitive.PrimitiveIndex;
            var fetchSlotStructureValid = hasSlot && slot.Slot == index;
            var fetchStrideMatches = hasSlot && slot.Stride == keyed.Stream.Stride;
            var validPayloadRange = keyed.Stream.ByteLength > 0 &&
                                     keyed.Stream.Stride > 0 &&
                                     keyed.Stream.ByteLength % keyed.Stream.Stride == 0 &&
                                     keyed.Stream.ElementCount > 0;
            var structuralMatch = completeSlotStructure &&
                                  categorySequenceValid &&
                                  keyOrdinalMatches &&
                                  fetchSlotStructureValid &&
                                  fetchStrideMatches &&
                                  validPayloadRange;
            rows.Add(new(
                modelArchiveName,
                modelObjectName,
                primitive.LayerIndex,
                primitive.PrimitiveIndex,
                hasSlot ? checked((int)slot.Slot) : -1,
                hasSlot ? slot.AuxiliaryValue : 0,
                hasSlot ? slot.Stride : 0,
                keyed.Category,
                keyed.Ordinal,
                keyed.Stream.SymbolName,
                keyed.Stream.SectionIndex,
                keyed.Stream.BufferRecordOffset,
                keyed.Stream.DescriptorOffset,
                keyed.Stream.DataOffset,
                keyed.Stream.ByteLength,
                keyed.Stream.Stride,
                keyed.Stream.ElementCount,
                keyed.Stream.Flags,
                data.IsEmpty || elf is null
                    ? string.Empty
                    : EaglVertexBufferReader.GetPayloadSha256(data, elf, keyed.Stream),
                categorySequenceValid,
                keyOrdinalMatches,
                fetchSlotStructureValid,
                fetchStrideMatches,
                validPayloadRange,
                structuralMatch,
                structuralMatch ? Status : "inconclusive-original-primitive-stream-topology",
                OriginalSemantics,
                structuralMatch
                    ? string.Empty
                    : "primitive slot, category/ordinal key or validated payload metadata did not form the complete opaque topology"));
        }

        return rows;
    }

    public sealed record KeyedStream(EaglVertexBufferStream Stream, int Category, uint Ordinal);

    public static bool MatchesFetchStrideSequence(
        EaglModelPrimitive primitive,
        IReadOnlyList<KeyedStream> keyedStreams)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(keyedStreams);
        var slots = primitive.VertexFetchSlots.OrderBy(slot => slot.Slot).ToArray();
        var streams = keyedStreams.OrderBy(item => item.Category).ToArray();
        return slots.Length == 5 &&
               streams.Length == 5 &&
               slots.Select((slot, index) => slot.Stride == streams[index].Stream.Stride).All(value => value);
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalPrimitiveStreamTopologyRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive", "fetch_slot",
                "fetch_auxiliary_value", "fetch_stride", "category", "ordinal", "symbol_name",
                "section_index", "buffer_record_offset", "descriptor_offset", "data_offset",
                "byte_length", "stream_stride", "element_count", "flags", "category_sequence_valid",
                "key_ordinal_matches_primitive", "fetch_slot_structure_valid",
                "fetch_stride_matches_stream_stride", "valid_payload_range", "structural_match",
                "payload_sha256",
                "status", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName), Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.FetchSlot.ToString(CultureInfo.InvariantCulture),
            $"0x{row.FetchAuxiliaryValue:X8}", row.FetchStride.ToString(CultureInfo.InvariantCulture),
            row.Category.ToString(CultureInfo.InvariantCulture), row.Ordinal.ToString(CultureInfo.InvariantCulture),
            Field(row.SymbolName), row.SectionIndex.ToString(CultureInfo.InvariantCulture),
            $"0x{row.BufferRecordOffset:X}", $"0x{row.DescriptorOffset:X}", $"0x{row.DataOffset:X}",
            row.ByteLength.ToString(CultureInfo.InvariantCulture),
            row.StreamStride.ToString(CultureInfo.InvariantCulture),
            row.ElementCount.ToString(CultureInfo.InvariantCulture), $"0x{row.Flags:X8}",
            row.CategorySequenceValid.ToString(), row.KeyOrdinalMatchesPrimitive.ToString(),
            row.FetchSlotStructureValid.ToString(), row.FetchStrideMatchesStreamStride.ToString(),
            row.ValidPayloadRange.ToString(), row.StructuralMatch.ToString(), Field(row.PayloadSha256),
            Field(row.Status),
            Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalPrimitiveStreamTopologySummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive_path", "model_package_candidates", "model_packages_loaded",
            "object_candidates", "objects_loaded", "objects_with_validated_streams", "primitive_rows",
            "stream_rows", "primitive_rows_with_ordinal_matched_streams", "stream_rows_with_ordinal_match",
            "stream_rows_with_stride_match", "stream_rows_with_valid_payload_range", "complete_primitive_rows",
            "objects_with_non_overlapping_payload_ranges", "reversed_slot_match_rows",
            "shifted_ordinal_match_rows", "rejected_packages", "rejected_objects", "status",
            "provenance", "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath), summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithValidatedStreams.ToString(CultureInfo.InvariantCulture),
            summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture),
            summary.StreamRows.ToString(CultureInfo.InvariantCulture),
            summary.PrimitiveRowsWithOrdinalMatchedStreams.ToString(CultureInfo.InvariantCulture),
            summary.StreamRowsWithOrdinalMatch.ToString(CultureInfo.InvariantCulture),
            summary.StreamRowsWithStrideMatch.ToString(CultureInfo.InvariantCulture),
            summary.StreamRowsWithValidPayloadRange.ToString(CultureInfo.InvariantCulture),
            summary.CompletePrimitiveRows.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithNonOverlappingPayloadRanges.ToString(CultureInfo.InvariantCulture),
            summary.ReversedSlotMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.ShiftedOrdinalMatchRows.ToString(CultureInfo.InvariantCulture),
            summary.RejectedPackages.ToString(CultureInfo.InvariantCulture),
            summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status), Field(summary.Provenance), Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[] { string.Join("\t", fields), string.Join("\t", values) }, new UTF8Encoding(false));
    }

    private static bool HasNonOverlappingPayloadRanges(IReadOnlyList<EaglVertexBufferStream> streams)
    {
        var ranges = streams
            .Select(stream => (stream.SectionIndex, Start: stream.DataOffset, End: checked(stream.DataOffset + stream.ByteLength)))
            .ToArray();
        for (var left = 0; left < ranges.Length; left++)
        {
            for (var right = left + 1; right < ranges.Length; right++)
            {
                if (ranges[left].SectionIndex == ranges[right].SectionIndex &&
                    ranges[left].Start < ranges[right].End &&
                    ranges[right].Start < ranges[left].End)
                {
                    return false;
                }
            }
        }

        return ranges.Length > 0;
    }

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
        exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentException;

    private static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }

    private static string Field(string value) =>
        value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
