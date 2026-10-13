using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglCandidateRawStreamSlot(
    int Slot,
    string StreamName,
    uint StreamStride,
    uint StreamBytes,
    uint ElementCount,
    bool Valid);

public sealed record EaglOriginalCandidateRawStreamIdentityRow(
    string ModelArchive,
    string ModelObject,
    int Layer,
    uint Primitive,
    bool CandidateCompleteFiveSlotLayout,
    int CandidateSlotRows,
    int RawValidSlotRows,
    int RawMetadataExactRows,
    int RawPayloadHashRows,
    int DistinctRawPayloadHashes,
    string Slot1RawPayloadSha256,
    string RawPayloadHashes,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalCandidateRawStreamIdentitySummary(
    string ModelArchivePath,
    string CandidateFetchReport,
    int CandidateObjectCount,
    int CandidatePrimitiveRows,
    int CandidateCompleteFiveSlotRows,
    int CandidateSlotRows,
    int RawObjectsLoaded,
    int RawStreamsLoaded,
    int RawValidSlotRows,
    int RawMetadataExactRows,
    int RawPayloadHashRows,
    int CompleteIdentityRows,
    int ExpectedUnmaterializedRows,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Re-reads the candidate fetch symbols from the same ISO and hashes the
/// validated raw __VB payload ranges behind every candidate fetch slot. This
/// proves only bounded source/payload identity for the candidate path. It does
/// not assign slot, payload or state semantics to position, UV, material,
/// normal, weight, skinning, rigging, animation, morph or GPU behavior.
/// </summary>
public static class EaglOriginalCandidateRawStreamIdentityAudit
{
    public const string Status =
        "verified-independent-original-candidate-raw-stream-identity-control";
    public const string ExpectedUnmaterializedStatus =
        "verified-expected-unmaterialized-candidate-fetch-control";
    public const string Provenance =
        "same-ISO-read-only+candidate-fetch-report+revalidated-EAGL-__VB-ranges+full-payload-SHA256;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalCandidateRawStreamIdentitySummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        string candidateFetchReportPath,
        IList<EaglOriginalCandidateRawStreamIdentityRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(candidateFetchReportPath);

        var candidateRows = ReadCandidateRows(candidateFetchReportPath);
        var outputRows = rows ?? new List<EaglOriginalCandidateRawStreamIdentityRow>();
        var candidateObjectKeys = candidateRows
            .Select(row => MakeObjectKey(row.ModelArchiveName, row.ModelObjectName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rawStreamsByObject = new Dictionary<string, IReadOnlyDictionary<string, RawStream>>(
            StringComparer.OrdinalIgnoreCase);
        var rejectedObjects = 0;

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
            }
            catch (Exception exception) when (IsBoundedReadFailure(exception))
            {
                foreach (var key in candidateObjectKeys.Where(key =>
                             key.StartsWith(modelEntry.Name + "\u001F", StringComparison.OrdinalIgnoreCase)))
                {
                    rejectedObjects++;
                }

                continue;
            }

            BigArchiveInspection nestedArchive;
            try
            {
                nestedArchive = BigArchiveReader.Inspect(expandedModel);
            }
            catch (Exception exception) when (IsBoundedReadFailure(exception))
            {
                foreach (var key in candidateObjectKeys.Where(key =>
                             key.StartsWith(modelEntry.Name + "\u001F", StringComparison.OrdinalIgnoreCase)))
                {
                    rejectedObjects++;
                }

                continue;
            }

            foreach (var objectEntry in nestedArchive.Entries
                         .Where(entry => string.Equals(
                             Path.GetExtension(entry.Name),
                             ".ord",
                             StringComparison.OrdinalIgnoreCase))
                         .Take(MaximumObjectEntriesPerPackage))
            {
                var objectKey = MakeObjectKey(modelEntry.Name, objectEntry.Name);
                if (!candidateObjectKeys.Contains(objectKey))
                {
                    continue;
                }

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
                    rawStreamsByObject[objectKey] = streams
                        .GroupBy(stream => stream.SymbolName, StringComparer.Ordinal)
                        .ToDictionary(
                            group => group.Key,
                            group => new RawStream(objectData, inspection.Elf, group.Single()),
                            StringComparer.Ordinal);
                }
                catch (Exception exception) when (IsBoundedReadFailure(exception))
                {
                    rejectedObjects++;
                }
            }
        }

        foreach (var candidate in candidateRows)
        {
            var slots = ParseSlotRecords(candidate.SlotRecords);
            if (slots.Count == 0 &&
                candidate.FetchSlotCount == 4 &&
                candidate.FetchBindingCount == 0 &&
                !candidate.CompleteFiveSlotLayout)
            {
                outputRows.Add(new(
                    candidate.ModelArchiveName,
                    candidate.ModelObjectName,
                    candidate.LayerIndex,
                    candidate.PrimitiveIndex,
                    false,
                    0,
                    0,
                    0,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    ExpectedUnmaterializedStatus,
                    OriginalSemantics,
                    "input candidate primitive has the observed four-slot/unmaterialized shape"));
                continue;
            }

            rawStreamsByObject.TryGetValue(
                MakeObjectKey(candidate.ModelArchiveName, candidate.ModelObjectName),
                out var rawStreams);
            var hashes = new List<string>();
            var rawValid = 0;
            var metadataExact = 0;
            var payloadHashRows = 0;
            foreach (var slot in slots)
            {
                if (!slot.Valid || rawStreams is null ||
                    !rawStreams.TryGetValue(slot.StreamName, out var raw))
                {
                    continue;
                }

                rawValid++;
                if (raw.Stream.Stride == slot.StreamStride &&
                    raw.Stream.ByteLength == slot.StreamBytes &&
                    raw.Stream.ElementCount == slot.ElementCount)
                {
                    metadataExact++;
                }

                var hash = raw.PayloadSha256;
                hashes.Add($"slot={slot.Slot}:{hash}");
                payloadHashRows++;
            }

            var slot1Hash = hashes
                .FirstOrDefault(value => value.StartsWith("slot=1:", StringComparison.Ordinal))?
                .Split(':', 2)[1] ?? string.Empty;
            var completeIdentity = slots.Count == candidate.FetchSlotCount &&
                                   candidate.FetchSlotCount == 5 &&
                                   rawValid == 5 &&
                                   metadataExact == 5 &&
                                   payloadHashRows == 5;
            outputRows.Add(new(
                candidate.ModelArchiveName,
                candidate.ModelObjectName,
                candidate.LayerIndex,
                candidate.PrimitiveIndex,
                candidate.CompleteFiveSlotLayout,
                slots.Count,
                rawValid,
                metadataExact,
                payloadHashRows,
                hashes.Select(value => value.Split(':', 2)[1]).Distinct(StringComparer.Ordinal).Count(),
                slot1Hash,
                string.Join("|", hashes),
                completeIdentity ? Status : "inconclusive-" + Status,
                OriginalSemantics,
                completeIdentity ? string.Empty : "candidate fetch slots did not rejoin to five validated raw payload ranges"));
        }

        var completeRows = outputRows.Count(row => row.Status == Status);
        var expectedUnmaterializedRows = outputRows.Count(row => row.Status == ExpectedUnmaterializedStatus);
        var rawValidSlotRows = outputRows.Sum(row => row.RawValidSlotRows);
        var rawMetadataExactRows = outputRows.Sum(row => row.RawMetadataExactRows);
        var rawPayloadHashRows = outputRows.Sum(row => row.RawPayloadHashRows);
        var summaryStatus = candidateRows.Count > 0 &&
                            outputRows.Count == candidateRows.Count &&
                            rawStreamsByObject.Count == candidateObjectKeys.Count &&
                            rejectedObjects == 0 &&
                            completeRows + expectedUnmaterializedRows == candidateRows.Count
            ? Status
            : "incomplete-" + Status;
        return new(
            modelArchivePath,
            candidateFetchReportPath,
            candidateObjectKeys.Count,
            candidateRows.Count,
            candidateRows.Count(row => row.CompleteFiveSlotLayout),
            candidateRows.Sum(row => ParseSlotRecords(row.SlotRecords).Count),
            rawStreamsByObject.Count,
            rawStreamsByObject.Values.Sum(streams => streams.Count),
            rawValidSlotRows,
            rawMetadataExactRows,
            rawPayloadHashRows,
            completeRows,
            expectedUnmaterializedRows,
            rejectedObjects,
            summaryStatus,
            Provenance,
            OriginalSemantics);
    }

    public static IReadOnlyList<EaglCandidateRawStreamSlot> ParseSlotRecords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<EaglCandidateRawStreamSlot>();
        }

        var output = new List<EaglCandidateRawStreamSlot>();
        foreach (var record in value.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = record.Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(field => field.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
            if (!TryParseInt(fields, "slot", out var slot) ||
                !TryParseUInt(fields, "stream_stride", out var streamStride) ||
                !TryParseUInt(fields, "stream_bytes", out var streamBytes) ||
                !TryParseUInt(fields, "elements", out var elementCount) ||
                !fields.TryGetValue("stream", out var streamName) ||
                !fields.TryGetValue("valid", out var validText) ||
                !bool.TryParse(validText, out var valid))
            {
                continue;
            }

            output.Add(new(slot, streamName, streamStride, streamBytes, elementCount, valid));
        }

        return output.OrderBy(slot => slot.Slot).ToArray();
    }

    public static IReadOnlyList<EaglCandidateFetchAssociationRow> ReadCandidateRows(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var lines = File.ReadAllLines(path, Encoding.UTF8)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        if (lines.Length == 0)
        {
            throw new InvalidDataException($"Candidate fetch report is empty: {path}");
        }

        var headers = lines[0].Split('\t');
        var rows = new List<EaglCandidateFetchAssociationRow>();
        foreach (var line in lines.Skip(1))
        {
            var fields = line.Split('\t');
            if (fields.Length != headers.Length)
            {
                throw new InvalidDataException($"Candidate fetch report column count mismatch: {path}");
            }

            var values = headers
                .Select((header, index) => (header, value: fields[index]))
                .ToDictionary(item => item.header, item => item.value, StringComparer.Ordinal);
            rows.Add(new(
                Required(values, "model_archive"),
                Required(values, "model_object"),
                Required(values, "shape_name"),
                ParseInt(values, "layer"),
                ParseUInt(values, "primitive"),
                Required(values, "primitive_type"),
                Required(values, "state_names"),
                ParseBool(values, "command_stream_complete"),
                ParseInt(values, "command_count"),
                Required(values, "command_bytes_sha256"),
                ParseInt(values, "fetch_slot_count"),
                ParseInt(values, "fetch_binding_count"),
                ParseBool(values, "complete_five_slot_layout"),
                ParseInt(values, "mesh_group_count"),
                Required(values, "slot_records"),
                Required(values, "status"),
                Required(values, "original_semantics"),
                values.GetValueOrDefault("issue", string.Empty)));
        }

        return rows;
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalCandidateRawStreamIdentityRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive",
                "candidate_complete_five_slot_layout", "candidate_slot_rows",
                "raw_valid_slot_rows", "raw_metadata_exact_rows", "raw_payload_hash_rows",
                "distinct_raw_payload_hashes", "slot1_raw_payload_sha256",
                "raw_payload_hashes", "status", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchive), Field(row.ModelObject),
            row.Layer.ToString(CultureInfo.InvariantCulture),
            row.Primitive.ToString(CultureInfo.InvariantCulture),
            row.CandidateCompleteFiveSlotLayout.ToString(),
            row.CandidateSlotRows.ToString(CultureInfo.InvariantCulture),
            row.RawValidSlotRows.ToString(CultureInfo.InvariantCulture),
            row.RawMetadataExactRows.ToString(CultureInfo.InvariantCulture),
            row.RawPayloadHashRows.ToString(CultureInfo.InvariantCulture),
            row.DistinctRawPayloadHashes.ToString(CultureInfo.InvariantCulture),
            Field(row.Slot1RawPayloadSha256), Field(row.RawPayloadHashes),
            Field(row.Status), Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalCandidateRawStreamIdentitySummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var headers = new[]
        {
            "model_archive_path", "candidate_fetch_report", "candidate_object_count",
            "candidate_primitive_rows", "candidate_complete_five_slot_rows", "candidate_slot_rows",
            "raw_objects_loaded", "raw_streams_loaded", "raw_valid_slot_rows",
            "raw_metadata_exact_rows", "raw_payload_hash_rows", "complete_identity_rows",
            "expected_unmaterialized_rows", "rejected_objects", "status", "provenance", "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath), Field(summary.CandidateFetchReport),
            summary.CandidateObjectCount.ToString(CultureInfo.InvariantCulture),
            summary.CandidatePrimitiveRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateCompleteFiveSlotRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateSlotRows.ToString(CultureInfo.InvariantCulture),
            summary.RawObjectsLoaded.ToString(CultureInfo.InvariantCulture),
            summary.RawStreamsLoaded.ToString(CultureInfo.InvariantCulture),
            summary.RawValidSlotRows.ToString(CultureInfo.InvariantCulture),
            summary.RawMetadataExactRows.ToString(CultureInfo.InvariantCulture),
            summary.RawPayloadHashRows.ToString(CultureInfo.InvariantCulture),
            summary.CompleteIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.ExpectedUnmaterializedRows.ToString(CultureInfo.InvariantCulture),
            summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status), Field(summary.Provenance), Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[] { string.Join("\t", headers), string.Join("\t", values) }, new UTF8Encoding(false));
    }

    private sealed record RawStream(
        byte[] Data,
        ElfInspection Elf,
        EaglVertexBufferStream Stream)
    {
        public string PayloadSha256
        {
            get
            {
                var section = Elf.Sections[Stream.SectionIndex];
                var fileOffset = checked((int)((ulong)section.Offset + Stream.DataOffset));
                return Convert.ToHexString(SHA256.HashData(Data.AsSpan(fileOffset, checked((int)Stream.ByteLength))));
            }
        }
    }

    private static string MakeObjectKey(string modelArchive, string modelObject) =>
        string.Join("\u001F", modelArchive, modelObject);

    private static byte[] ReadAll(Stream input)
    {
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }

    private static bool IsBoundedReadFailure(Exception exception) =>
        exception is InvalidDataException or EndOfStreamException or IOException or OverflowException or ArgumentOutOfRangeException;

    private static bool TryParseInt(IReadOnlyDictionary<string, string> values, string key, out int value)
    {
        if (values.TryGetValue(key, out var raw) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseUInt(IReadOnlyDictionary<string, string> values, string key, out uint value)
    {
        if (values.TryGetValue(key, out var raw) &&
            uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
    }

    private static string Required(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : throw new InvalidDataException($"Missing candidate fetch field: {key}");

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key) =>
        int.Parse(Required(values, key), CultureInfo.InvariantCulture);

    private static uint ParseUInt(IReadOnlyDictionary<string, string> values, string key) =>
        uint.Parse(Required(values, key), CultureInfo.InvariantCulture);

    private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key) =>
        bool.Parse(Required(values, key));

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
