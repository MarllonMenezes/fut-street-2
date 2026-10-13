using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalPrimitiveStreamCommandPayloadIdentityRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int FetchCommandCount,
    int CandidateOrdinalCount,
    int RawIdentityCandidateCount,
    int DistinctRawIdentityCount,
    int DuplicateRawIdentityCount,
    int CommandPayloadExactCandidateCount,
    string RawIdentityHashes,
    string CommandPayloadExactOrdinals,
    bool CurrentOrdinalPresent,
    bool CurrentOrdinalRawIdentityUnique,
    bool CurrentOrdinalCommandPayloadExact,
    bool CurrentOrdinalUniquelyDiscriminatedByCommandPayload,
    bool ShiftedOrdinalCommandPayloadExact,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalPrimitiveStreamCommandPayloadIdentitySummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ObjectsWithValidatedStreams,
    int PrimitiveRows,
    int CompleteFetchRows,
    int CandidateOrdinalRows,
    int RawIdentityCandidateRows,
    int DistinctRawIdentityRows,
    int DuplicateRawIdentityRows,
    int CommandPayloadExactCandidateRows,
    int CurrentOrdinalRows,
    int CurrentOrdinalRawIdentityUniqueRows,
    int CurrentOrdinalCommandPayloadExactRows,
    int CurrentOrdinalUniquelyDiscriminatedByCommandPayloadRows,
    int ShiftedOrdinalCommandPayloadExactRows,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Compares complete raw command-9 packets with bounded raw identities for
/// repeated opaque __VB ordinal candidates. This is an identity control only;
/// it does not assign any field, stream or command rendering semantics.
/// </summary>
public static class EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit
{
    public const string Status =
        "verified-bounded-original-primitive-stream-command-payload-identity-control";
    public const string Provenance =
        "same-trunk-raw-EAGL-command-9-packets+validated-__VB-record-descriptor-and-bounded-payload-head-tail-identities;exact-byte-containment-negative-control;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;
    private const int PayloadIdentityHeadBytes = 512;
    private const int PayloadIdentityTailBytes = 64;

    public static EaglOriginalPrimitiveStreamCommandPayloadIdentitySummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalPrimitiveStreamCommandPayloadIdentityRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalPrimitiveStreamCommandPayloadIdentityRow>();
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
                                 .OrderBy(item => item.LayerIndex)
                                 .ThenBy(item => item.PrimitiveIndex))
                    {
                        primitiveRows++;
                        var commands = ReadFetchCommands(primitive);
                        var candidates = streamGroups
                            .OrderBy(group => group.Key)
                            .Select(group => EvaluateCandidate(
                                combinedData,
                                inspection.Elf,
                                commands,
                                group.Key,
                                group.Value))
                            .ToArray();
                        if (commands.Count == 5 && commands.Select(command => command.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }))
                        {
                            completeFetchRows++;
                        }

                        var distinctIdentityCount = candidates
                            .Select(candidate => candidate.IdentityHash)
                            .Distinct(StringComparer.Ordinal)
                            .Count();
                        var duplicateIdentityCount = Math.Max(0, candidates.Length - distinctIdentityCount);
                        var exactCandidates = candidates.Where(candidate => candidate.CommandPayloadExact).ToArray();
                        var current = candidates.SingleOrDefault(candidate => candidate.Ordinal == primitive.PrimitiveIndex);
                        var currentPresent = current is not null;
                        var currentIdentityUnique = currentPresent && candidates.Count(candidate =>
                            string.Equals(candidate.IdentityHash, current!.IdentityHash, StringComparison.Ordinal)) == 1;
                        var currentExact = current?.CommandPayloadExact == true;
                        var currentUniqueExact = currentExact && exactCandidates.Length == 1;
                        var shiftedExact = primitive.PrimitiveIndex < uint.MaxValue &&
                                           candidates.Any(candidate =>
                                               candidate.Ordinal == primitive.PrimitiveIndex + 1 &&
                                               candidate.CommandPayloadExact);
                        var rowStatus = currentPresent && commands.Count > 0 && candidates.Length > 0
                            ? Status
                            : "inconclusive-original-primitive-stream-command-payload-identity-control";
                        outputRows.Add(new(
                            modelEntry.Name,
                            objectEntry.Name,
                            primitive.LayerIndex,
                            primitive.PrimitiveIndex,
                            commands.Count,
                            candidates.Length,
                            candidates.Length,
                            distinctIdentityCount,
                            duplicateIdentityCount,
                            exactCandidates.Length,
                            JoinIdentityHashes(candidates),
                            JoinOrdinals(exactCandidates),
                            currentPresent,
                            currentIdentityUnique,
                            currentExact,
                            currentUniqueExact,
                            shiftedExact,
                            rowStatus,
                            OriginalSemantics,
                            rowStatus == Status ? string.Empty : "no current ordinal/raw identity candidate was available"));
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
                     outputRows.All(row => row.Status == Status) &&
                     rejectedPackages == 0 &&
                     rejectedObjects == 0
            ? Status
            : "incomplete-original-primitive-stream-command-payload-identity-control";

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
            outputRows.Sum(row => row.RawIdentityCandidateCount),
            outputRows.Sum(row => row.DistinctRawIdentityCount),
            outputRows.Sum(row => row.DuplicateRawIdentityCount),
            outputRows.Sum(row => row.CommandPayloadExactCandidateCount),
            outputRows.Count(row => row.CurrentOrdinalPresent),
            outputRows.Count(row => row.CurrentOrdinalRawIdentityUnique),
            outputRows.Count(row => row.CurrentOrdinalCommandPayloadExact),
            outputRows.Count(row => row.CurrentOrdinalUniquelyDiscriminatedByCommandPayload),
            outputRows.Count(row => row.ShiftedOrdinalCommandPayloadExact),
            rejectedPackages,
            rejectedObjects,
            status,
            Provenance,
            OriginalSemantics);
    }

    public static IReadOnlyList<RawFetchCommand> ReadFetchCommands(EaglModelPrimitive primitive)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        return primitive.RenderCommandStream.Commands
            .Where(command => command.CommandId == 9 && command.RawBytes.Length >= 16)
            .Select(command => new RawFetchCommand(
                BinaryPrimitives.ReadUInt32LittleEndian(command.RawBytes.AsSpan(8, 4)),
                command.RawBytes.ToArray()))
            .OrderBy(command => command.Slot)
            .ToArray();
    }

    public static bool ContainsBytes(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> needle)
    {
        if (needle.Length == 0)
        {
            return true;
        }

        if (needle.Length > bytes.Length)
        {
            return false;
        }

        for (var offset = 0; offset <= bytes.Length - needle.Length; offset++)
        {
            if (bytes.Slice(offset, needle.Length).SequenceEqual(needle))
            {
                return true;
            }
        }

        return false;
    }

    private static CandidateEvaluation EvaluateCandidate(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        IReadOnlyList<RawFetchCommand> commands,
        uint ordinal,
        IReadOnlyList<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream> streams)
    {
        var orderedStreams = streams
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        var categorySequenceValid = orderedStreams.Length == 5 &&
                                    orderedStreams.Select((item, index) => item.Category == orderedStreams[0].Category + index).All(value => value);
        var commandSlotsValid = commands.Count == 5 &&
                                commands.Select(command => command.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 });
        var identities = new StreamIdentity[orderedStreams.Length];
        for (var index = 0; index < orderedStreams.Length; index++)
        {
            identities[index] = BuildStreamIdentity(data, elf, orderedStreams[index].Stream);
        }
        var bundleBytes = identities.SelectMany(identity => identity.Bytes).ToArray();
        var bundleHash = Convert.ToHexString(SHA256.HashData(bundleBytes));
        var exact = categorySequenceValid && commandSlotsValid &&
                    commands.Select((command, index) => ContainsBytes(identities[index].Bytes, command.RawBytes)).All(value => value);
        return new(ordinal, bundleHash, exact);
    }

    private static StreamIdentity BuildStreamIdentity(
        ReadOnlySpan<byte> data,
        ElfInspection elf,
        EaglVertexBufferStream stream)
    {
        var section = elf.Sections[stream.SectionIndex];
        var recordFileOffset = checked((int)((ulong)section.Offset + stream.BufferRecordOffset));
        var descriptorFileOffset = checked((int)((ulong)section.Offset + stream.DescriptorOffset));
        var dataFileOffset = checked((int)((ulong)section.Offset + stream.DataOffset));
        var payloadLength = checked((int)Math.Min(stream.ByteLength, (uint)(PayloadIdentityHeadBytes + PayloadIdentityTailBytes)));
        var bytes = new List<byte>(20 + 8 + payloadLength + 32);
        AppendUInt32(bytes, stream.BufferRecordOffset);
        AppendUInt32(bytes, stream.DescriptorOffset);
        AppendUInt32(bytes, stream.DataOffset);
        AppendUInt32(bytes, stream.ByteLength);
        AppendUInt32(bytes, stream.Stride);
        AppendUInt32(bytes, stream.Flags);
        var record = data.Slice(recordFileOffset, 20).ToArray();
        var descriptor = data.Slice(descriptorFileOffset, 8).ToArray();
        var payload = data.Slice(dataFileOffset, checked((int)stream.ByteLength));
        bytes.AddRange(record);
        bytes.AddRange(descriptor);
        bytes.AddRange(payload.Slice(0, Math.Min(PayloadIdentityHeadBytes, payload.Length)).ToArray());
        if (payload.Length > PayloadIdentityHeadBytes)
        {
            bytes.AddRange(payload.Slice(Math.Max(PayloadIdentityHeadBytes, payload.Length - PayloadIdentityTailBytes)).ToArray());
        }

        return new(bytes.ToArray(), Convert.ToHexString(SHA256.HashData(bytes.ToArray())));
    }

    private static void AppendUInt32(List<byte> bytes, uint value)
    {
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
        bytes.AddRange(buffer.ToArray());
    }

    private static string JoinIdentityHashes(IEnumerable<CandidateEvaluation> candidates) =>
        string.Join('|', candidates.Select(candidate =>
            $"{candidate.Ordinal.ToString(CultureInfo.InvariantCulture)}:{candidate.IdentityHash}"));

    private static string JoinOrdinals(IEnumerable<CandidateEvaluation> candidates) =>
        string.Join(',', candidates.Select(candidate => candidate.Ordinal.ToString(CultureInfo.InvariantCulture)));

    public sealed record RawFetchCommand(uint Slot, byte[] RawBytes);

    private sealed record StreamIdentity(byte[] Bytes, string Hash);

    private sealed record CandidateEvaluation(
        uint Ordinal,
        string IdentityHash,
        bool CommandPayloadExact);

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalPrimitiveStreamCommandPayloadIdentityRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive", "fetch_command_count",
                "candidate_ordinal_count", "raw_identity_candidate_count", "distinct_raw_identity_count",
                "duplicate_raw_identity_count", "command_payload_exact_candidate_count", "raw_identity_hashes",
                "command_payload_exact_ordinals", "current_ordinal_present", "current_ordinal_raw_identity_unique",
                "current_ordinal_command_payload_exact", "current_ordinal_uniquely_discriminated_by_command_payload",
                "shifted_ordinal_command_payload_exact", "status", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName), Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.FetchCommandCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateOrdinalCount.ToString(CultureInfo.InvariantCulture),
            row.RawIdentityCandidateCount.ToString(CultureInfo.InvariantCulture),
            row.DistinctRawIdentityCount.ToString(CultureInfo.InvariantCulture),
            row.DuplicateRawIdentityCount.ToString(CultureInfo.InvariantCulture),
            row.CommandPayloadExactCandidateCount.ToString(CultureInfo.InvariantCulture),
            Field(row.RawIdentityHashes), Field(row.CommandPayloadExactOrdinals),
            row.CurrentOrdinalPresent.ToString(), row.CurrentOrdinalRawIdentityUnique.ToString(),
            row.CurrentOrdinalCommandPayloadExact.ToString(),
            row.CurrentOrdinalUniquelyDiscriminatedByCommandPayload.ToString(),
            row.ShiftedOrdinalCommandPayloadExact.ToString(),
            Field(row.Status), Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalPrimitiveStreamCommandPayloadIdentitySummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive_path", "model_package_candidates", "model_packages_loaded", "object_candidates",
            "objects_loaded", "objects_with_validated_streams", "primitive_rows", "complete_fetch_rows",
            "candidate_ordinal_rows", "raw_identity_candidate_rows", "distinct_raw_identity_rows",
            "duplicate_raw_identity_rows", "command_payload_exact_candidate_rows", "current_ordinal_rows",
            "current_ordinal_raw_identity_unique_rows", "current_ordinal_command_payload_exact_rows",
            "current_ordinal_uniquely_discriminated_by_command_payload_rows", "shifted_ordinal_command_payload_exact_rows",
            "rejected_packages", "rejected_objects", "status", "provenance", "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath), summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture), summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture), summary.ObjectsWithValidatedStreams.ToString(CultureInfo.InvariantCulture),
            summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture), summary.CompleteFetchRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateOrdinalRows.ToString(CultureInfo.InvariantCulture), summary.RawIdentityCandidateRows.ToString(CultureInfo.InvariantCulture),
            summary.DistinctRawIdentityRows.ToString(CultureInfo.InvariantCulture), summary.DuplicateRawIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.CommandPayloadExactCandidateRows.ToString(CultureInfo.InvariantCulture), summary.CurrentOrdinalRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalRawIdentityUniqueRows.ToString(CultureInfo.InvariantCulture), summary.CurrentOrdinalCommandPayloadExactRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalUniquelyDiscriminatedByCommandPayloadRows.ToString(CultureInfo.InvariantCulture),
            summary.ShiftedOrdinalCommandPayloadExactRows.ToString(CultureInfo.InvariantCulture),
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
        exception is InvalidDataException or EndOfStreamException or IOException or
        OverflowException or ArgumentException or ArgumentOutOfRangeException or
        IndexOutOfRangeException or FileNotFoundException;

    private static void EnsureParent(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }
    }

    private static string Field(string value) =>
        (value ?? string.Empty).Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
}
