using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalPrimitiveStreamOpaqueDiscriminationRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int FetchWordCount,
    int CandidateOrdinalCount,
    int StrideMatchingCandidateCount,
    int MetadataMatchingCandidateCount,
    int PayloadPrefixMatchingCandidateCount,
    string StrideMatchingOrdinals,
    string MetadataMatchingOrdinals,
    string PayloadPrefixMatchingOrdinals,
    bool CurrentOrdinalPresent,
    bool CurrentOrdinalMetadataMatch,
    bool CurrentOrdinalPayloadPrefixMatch,
    bool CurrentOrdinalUniquelyDiscriminatedByMetadata,
    bool CurrentOrdinalUniquelyDiscriminatedByPayloadPrefix,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalPrimitiveStreamOpaqueDiscriminationSummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ObjectsWithValidatedStreams,
    int PrimitiveRows,
    int CompleteFetchRows,
    int CandidateOrdinalRows,
    int StrideMatchingCandidateRows,
    int MetadataMatchingCandidateRows,
    int PayloadPrefixMatchingCandidateRows,
    int CurrentOrdinalRows,
    int CurrentOrdinalMetadataMatchingRows,
    int CurrentOrdinalPayloadPrefixMatchingRows,
    int UniquelyDiscriminatedByMetadataRows,
    int UniquelyDiscriminatedByPayloadPrefixRows,
    int ShiftedStrideCandidateRows,
    int ShiftedMetadataMatchingRows,
    int ShiftedPayloadPrefixMatchingRows,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Compares opaque fetch-command words with raw validated __VB record,
/// descriptor and bounded payload-prefix bytes for every ordinal candidate.
/// The comparison is a byte-level control only; it does not assign a field,
/// stream or command any rendering or vertex-attribute meaning.
/// </summary>
public static class EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit
{
    public const string Status = "verified-bounded-original-primitive-stream-opaque-discrimination";
    public const string Provenance =
        "same-trunk-raw-EAGL-fetch-command-words+validated-__VB-record-descriptor-and-bounded-payload-prefix-bytes;opaque-word-overlap-controls;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;
    private const int PayloadPrefixBytes = 256;

    public static EaglOriginalPrimitiveStreamOpaqueDiscriminationSummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalPrimitiveStreamOpaqueDiscriminationRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalPrimitiveStreamOpaqueDiscriminationRow>();
        using var topLevelStream = store.OpenRaw(modelArchivePath);
        var topLevelArchive = BigArchiveReader.Inspect(topLevelStream);
        var xshStems = topLevelArchive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".xsh", StringComparison.OrdinalIgnoreCase))
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
        var completeFetchRows = 0;
        var shiftedStrideCandidateRows = 0;
        var shiftedMetadataMatchingRows = 0;
        var shiftedPayloadPrefixMatchingRows = 0;
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
                         .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
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

                    var streamGroups = streams
                        .Select(stream =>
                        {
                            return EaglOriginalVertexRecordAssociationAudit.TryParseOpaqueVertexRecordKey(
                                stream.SymbolName,
                                out var category,
                                out var ordinal)
                                ? new EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream(stream, category, ordinal)
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
                        var fetchWords = ReadFetchWords(primitive);
                        var candidates = streamGroups
                            .OrderBy(group => group.Key)
                            .Select(group => EvaluateCandidate(
                                combinedData,
                                inspection.Elf,
                                fetchWords,
                                primitive,
                                group.Key,
                                group.Value))
                            .ToArray();
                        if (fetchWords.Count == 5 && fetchWords.Select(word => word.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }))
                        {
                            completeFetchRows++;
                        }

                        var strideMatches = candidates.Where(candidate => candidate.StrideMatch).ToArray();
                        var metadataMatches = strideMatches.Where(candidate => candidate.MetadataMatch).ToArray();
                        var payloadPrefixMatches = strideMatches.Where(candidate => candidate.PayloadPrefixMatch).ToArray();
                        if (primitive.PrimitiveIndex < uint.MaxValue &&
                            candidates.SingleOrDefault(candidate => candidate.Ordinal == primitive.PrimitiveIndex + 1) is { } shifted)
                        {
                            shiftedStrideCandidateRows += shifted.StrideMatch ? 1 : 0;
                            shiftedMetadataMatchingRows += shifted.MetadataMatch ? 1 : 0;
                            shiftedPayloadPrefixMatchingRows += shifted.PayloadPrefixMatch ? 1 : 0;
                        }
                        var current = candidates.SingleOrDefault(candidate => candidate.Ordinal == primitive.PrimitiveIndex);
                        var currentPresent = current is not null;
                        var currentMetadata = current?.MetadataMatch == true;
                        var currentPayload = current?.PayloadPrefixMatch == true;
                        var metadataUnique = currentMetadata && metadataMatches.Length == 1;
                        var payloadUnique = currentPayload && payloadPrefixMatches.Length == 1;
                        var rowStatus = currentPresent && strideMatches.Length > 0
                            ? Status
                            : "inconclusive-original-primitive-stream-opaque-discrimination";
                        outputRows.Add(new(
                            modelEntry.Name,
                            objectEntry.Name,
                            primitive.LayerIndex,
                            primitive.PrimitiveIndex,
                            fetchWords.Count,
                            candidates.Length,
                            strideMatches.Length,
                            metadataMatches.Length,
                            payloadPrefixMatches.Length,
                            JoinOrdinals(strideMatches),
                            JoinOrdinals(metadataMatches),
                            JoinOrdinals(payloadPrefixMatches),
                            currentPresent,
                            currentMetadata,
                            currentPayload,
                            metadataUnique,
                            payloadUnique,
                            rowStatus,
                            OriginalSemantics,
                            rowStatus == Status ? string.Empty : "no complete current ordinal/stride candidate was available"));
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
                     completeFetchRows > 0 &&
                     outputRows.Count == primitiveRows &&
                     rejectedPackages == 0 &&
                     rejectedObjects == 0
            ? Status
            : "incomplete-original-primitive-stream-opaque-discrimination";

        return new(
            modelArchivePath,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            objectsWithValidatedStreams,
            primitiveRows,
            completeFetchRows,
            outputRows.Sum(row => row.CandidateOrdinalCount),
            outputRows.Sum(row => row.StrideMatchingCandidateCount),
            outputRows.Sum(row => row.MetadataMatchingCandidateCount),
            outputRows.Sum(row => row.PayloadPrefixMatchingCandidateCount),
            outputRows.Count(row => row.CurrentOrdinalPresent),
            outputRows.Count(row => row.CurrentOrdinalMetadataMatch),
            outputRows.Count(row => row.CurrentOrdinalPayloadPrefixMatch),
            outputRows.Count(row => row.CurrentOrdinalUniquelyDiscriminatedByMetadata),
            outputRows.Count(row => row.CurrentOrdinalUniquelyDiscriminatedByPayloadPrefix),
            shiftedStrideCandidateRows,
            shiftedMetadataMatchingRows,
            shiftedPayloadPrefixMatchingRows,
            rejectedPackages,
            rejectedObjects,
            status,
            Provenance,
            OriginalSemantics);
    }

    public static IReadOnlyList<OpaqueFetchWord> ReadFetchWords(EaglModelPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        return primitive.RenderCommandStream.Commands
            .Where(command => command.CommandId == 9 && command.RawBytes.Length >= 16)
            .Select(command => new OpaqueFetchWord(
                BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(4, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(8, 4)),
                BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(12, 4))))
            .OrderBy(word => word.Slot)
            .ToArray();
    }

    public static bool ContainsAlignedWord(ReadOnlySpan<byte> bytes, uint word)
    {
        for (var offset = 0; offset + sizeof(uint) <= bytes.Length; offset += sizeof(uint))
        {
            if (BinaryPrimitives.ReadUInt32LittleEndian(bytes.Slice(offset, sizeof(uint))) == word)
            {
                return true;
            }
        }

        return false;
    }

    private static CandidateEvaluation EvaluateCandidate(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        IReadOnlyList<OpaqueFetchWord> fetchWords,
        EaglModelPrimitive primitive,
        uint ordinal,
        IReadOnlyList<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream> streams)
    {
        var orderedStreams = streams
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        var categorySequenceValid = orderedStreams.Length == 5 &&
                                    orderedStreams.Select((item, index) => item.Category == orderedStreams[0].Category + index).All(value => value);
        var slotStructureValid = fetchWords.Count == 5 &&
                                 fetchWords.Select(word => word.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
                                 fetchWords.All(word => word.Stride > 0);
        var strideMatch = categorySequenceValid && slotStructureValid &&
                          fetchWords.Select((word, index) => word.Stride == orderedStreams[index].Stream.Stride).All(value => value);
        var metadataMatch = false;
        var payloadPrefixMatch = false;
        if (strideMatch)
        {
            metadataMatch = true;
            payloadPrefixMatch = true;
            for (var index = 0; index < fetchWords.Count; index++)
            {
                var word = fetchWords[index].AuxiliaryValue;
                if (word == 0)
                {
                    continue;
                }

                var stream = orderedStreams[index].Stream;
                var metadata = ReadMetadataBytes(data, elf, stream);
                var payloadPrefix = ReadPayloadPrefix(data, elf, stream);
                metadataMatch &= ContainsAlignedWord(metadata, word);
                payloadPrefixMatch &= ContainsAlignedWord(payloadPrefix, word);
            }
        }

        return new(ordinal, strideMatch, metadataMatch, payloadPrefixMatch);
    }

    private static byte[] ReadMetadataBytes(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream)
    {
        var section = elf.Sections[stream.SectionIndex];
        var metadata = new byte[20 + 8];
        var recordFileOffset = checked((int)((ulong)section.Offset + stream.BufferRecordOffset));
        var descriptorFileOffset = checked((int)((ulong)section.Offset + stream.DescriptorOffset));
        data.Slice(recordFileOffset, 20).CopyTo(metadata);
        data.Slice(descriptorFileOffset, 8).CopyTo(metadata.AsSpan(20));
        return metadata;
    }

    private static byte[] ReadPayloadPrefix(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream)
    {
        var section = elf.Sections[stream.SectionIndex];
        var length = checked((int)Math.Min(stream.ByteLength, PayloadPrefixBytes));
        var fileOffset = checked((int)((ulong)section.Offset + stream.DataOffset));
        return data.Slice(fileOffset, length).ToArray();
    }

    private static string JoinOrdinals(IEnumerable<CandidateEvaluation> candidates) =>
        string.Join(',', candidates.Select(candidate => candidate.Ordinal.ToString(CultureInfo.InvariantCulture)));

    public sealed record OpaqueFetchWord(uint AuxiliaryValue, uint Slot, uint Stride);

    private sealed record CandidateEvaluation(
        uint Ordinal,
        bool StrideMatch,
        bool MetadataMatch,
        bool PayloadPrefixMatch);

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalPrimitiveStreamOpaqueDiscriminationRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive", "fetch_word_count",
                "candidate_ordinal_count", "stride_matching_candidate_count",
                "metadata_matching_candidate_count", "payload_prefix_matching_candidate_count",
                "stride_matching_ordinals", "metadata_matching_ordinals", "payload_prefix_matching_ordinals",
                "current_ordinal_present", "current_ordinal_metadata_match",
                "current_ordinal_payload_prefix_match", "current_ordinal_uniquely_discriminated_by_metadata",
                "current_ordinal_uniquely_discriminated_by_payload_prefix", "status", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName), Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.FetchWordCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateOrdinalCount.ToString(CultureInfo.InvariantCulture),
            row.StrideMatchingCandidateCount.ToString(CultureInfo.InvariantCulture),
            row.MetadataMatchingCandidateCount.ToString(CultureInfo.InvariantCulture),
            row.PayloadPrefixMatchingCandidateCount.ToString(CultureInfo.InvariantCulture),
            Field(row.StrideMatchingOrdinals), Field(row.MetadataMatchingOrdinals), Field(row.PayloadPrefixMatchingOrdinals),
            row.CurrentOrdinalPresent.ToString(), row.CurrentOrdinalMetadataMatch.ToString(),
            row.CurrentOrdinalPayloadPrefixMatch.ToString(),
            row.CurrentOrdinalUniquelyDiscriminatedByMetadata.ToString(),
            row.CurrentOrdinalUniquelyDiscriminatedByPayloadPrefix.ToString(),
            Field(row.Status), Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalPrimitiveStreamOpaqueDiscriminationSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive_path", "model_package_candidates", "model_packages_loaded", "object_candidates",
            "objects_loaded", "objects_with_validated_streams", "primitive_rows", "complete_fetch_rows",
            "candidate_ordinal_rows", "stride_matching_candidate_rows", "metadata_matching_candidate_rows",
            "payload_prefix_matching_candidate_rows", "current_ordinal_rows", "current_ordinal_metadata_matching_rows",
            "current_ordinal_payload_prefix_matching_rows", "uniquely_discriminated_by_metadata_rows",
            "uniquely_discriminated_by_payload_prefix_rows", "shifted_stride_candidate_rows",
            "shifted_metadata_matching_rows", "shifted_payload_prefix_matching_rows", "rejected_packages",
            "rejected_objects", "status", "provenance", "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath), summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture), summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture), summary.ObjectsWithValidatedStreams.ToString(CultureInfo.InvariantCulture),
            summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture), summary.CompleteFetchRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateOrdinalRows.ToString(CultureInfo.InvariantCulture), summary.StrideMatchingCandidateRows.ToString(CultureInfo.InvariantCulture),
            summary.MetadataMatchingCandidateRows.ToString(CultureInfo.InvariantCulture), summary.PayloadPrefixMatchingCandidateRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalRows.ToString(CultureInfo.InvariantCulture), summary.CurrentOrdinalMetadataMatchingRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalPayloadPrefixMatchingRows.ToString(CultureInfo.InvariantCulture), summary.UniquelyDiscriminatedByMetadataRows.ToString(CultureInfo.InvariantCulture),
            summary.UniquelyDiscriminatedByPayloadPrefixRows.ToString(CultureInfo.InvariantCulture), summary.ShiftedStrideCandidateRows.ToString(CultureInfo.InvariantCulture),
            summary.ShiftedMetadataMatchingRows.ToString(CultureInfo.InvariantCulture), summary.ShiftedPayloadPrefixMatchingRows.ToString(CultureInfo.InvariantCulture),
            summary.RejectedPackages.ToString(CultureInfo.InvariantCulture), summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status), Field(summary.Provenance), Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[] { string.Join("\t", fields), string.Join("\t", values) }, new UTF8Encoding(false));
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
