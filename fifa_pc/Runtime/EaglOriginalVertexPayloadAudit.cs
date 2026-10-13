using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalVertexPayloadRow(
    string ModelArchiveName,
    string ModelObjectName,
    int ValidatedStreamCount,
    int FinitePackedFloatProfileCount,
    int FiniteFloat2ProfileCount,
    int NearUnitRangeFloat2ProfileCount,
    int FiniteFloat3ProfileCount,
    int DeclaredBoundsContainmentFloat3Count,
    int DistinctPayloadRangeCount,
    int OverlapPayloadPairCount,
    string RelativeRecordSignature,
    string NumericSignature,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalVertexStreamNumericRow(
    string ModelArchiveName,
    string ModelObjectName,
    string SymbolName,
    int CategoryKey,
    uint OrdinalKey,
    bool OpaqueKeyValid,
    int SectionIndex,
    uint BufferRecordOffset,
    uint DescriptorOffset,
    uint DataOffset,
    uint ByteLength,
    uint StreamStride,
    uint ElementCount,
    uint Flags,
    bool FinitePackedFloatProfile,
    bool FiniteFloat2Profile,
    bool NearUnitRangeFloat2Profile,
    bool FiniteFloat3Profile,
    bool DeclaredBoundsContainmentFloat3,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalVertexPayloadSummary(
    string ModelArchivePath,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ValidatedVertexStreamRows,
    int ObjectsWithValidatedVertexStreams,
    int ObjectsWithFinitePackedFloatProfiles,
    int ObjectsWithFiniteFloat2Profiles,
    int ObjectsWithDeclaredBoundsContainment,
    int PayloadRows,
    int ObjectsWithDistinctPayloadRanges,
    int ObjectsWithOverlappingPayloadRanges,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Measures only already validated original EAGL __VB payload metadata and
/// finite numeric profiles. It does not assign a stream to a position, UV,
/// material, skinning, rigging, animation, morph or GPU operation.
/// </summary>
public static class EaglOriginalVertexPayloadAudit
{
    public const string Status = "verified-bounded-original-numeric-payload-structure";
    public const string Provenance =
        "same-trunk-raw-EAGL-objects+validated-__VB-payload-bytes+numeric-only-profiles+payload-range-topology;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalVertexPayloadSummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalVertexPayloadRow>? rows = null,
        IList<EaglOriginalVertexStreamNumericRow>? streamRows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalVertexPayloadRow>();
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
                    var combinedData = EaglObjectReader.Combine(objectData, companionData);
                    var inspection = EaglObjectReader.Inspect(combinedData);
                    var streams = EaglVertexBufferReader.Inspect(combinedData, inspection.Elf);
                    validatedVertexStreamRows += streams.Count;
                    if (streams.Count > 0)
                    {
                        objectsWithValidatedVertexStreams++;
                    }

                    if (streamRows is not null)
                    {
                        foreach (var streamRow in InspectStreams(
                                     modelEntry.Name,
                                     objectEntry.Name,
                                     combinedData,
                                     inspection,
                                     streams))
                        {
                            streamRows.Add(streamRow);
                        }
                    }

                    outputRows.Add(InspectObject(
                        modelEntry.Name,
                        objectEntry.Name,
                        combinedData,
                        inspection,
                        streams));
                    objectsLoaded++;
                }
                catch (Exception exception) when (IsBoundedReadFailure(exception))
                {
                    rejectedObjects++;
                }
            }
        }

        var finitePackedRows = outputRows.Count(row => row.FinitePackedFloatProfileCount > 0);
        var finiteFloat2Rows = outputRows.Count(row => row.FiniteFloat2ProfileCount > 0);
        var containmentRows = outputRows.Count(row => row.DeclaredBoundsContainmentFloat3Count > 0);
        var distinctRangeRows = outputRows.Count(row => row.DistinctPayloadRangeCount > 0);
        var overlapRows = outputRows.Count(row => row.OverlapPayloadPairCount > 0);
        var status = modelEntries.Length > 0 &&
                     packagesLoaded == modelEntries.Length &&
                     objectCandidates > 0 &&
                     objectsLoaded > 0 &&
                     validatedVertexStreamRows > 0 &&
                     finitePackedRows > 0 &&
                     finiteFloat2Rows > 0 &&
                     containmentRows > 0 &&
                     distinctRangeRows > 0 &&
                     rejectedPackages == 0 &&
                     rejectedObjects == 0
            ? Status
            : "incomplete-original-numeric-payload-structure";
        return new(
            modelArchivePath,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            validatedVertexStreamRows,
            objectsWithValidatedVertexStreams,
            finitePackedRows,
            finiteFloat2Rows,
            containmentRows,
            outputRows.Count,
            distinctRangeRows,
            overlapRows,
            rejectedPackages,
            rejectedObjects,
            status,
            Provenance,
            OriginalSemantics);
    }

    public static EaglOriginalVertexPayloadRow InspectObject(
        string modelArchiveName,
        string modelObjectName,
        ReadOnlySpan<byte> data,
        EaglObjectInspection inspection,
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(streams);

        var finitePacked = 0;
        var finiteFloat2 = 0;
        var nearUnitFloat2 = 0;
        var finiteFloat3 = 0;
        var boundsContainment = 0;
        foreach (var stream in streams)
        {
            if (EaglVertexBufferReader.TryMeasurePackedFloatProfile(
                    data,
                    inspection.Elf,
                    stream,
                    out var packedProfile) &&
                packedProfile is not null)
            {
                finitePacked++;
            }

            if (EaglVertexBufferReader.TryMeasureFloat2(
                    data,
                    inspection.Elf,
                    stream,
                    out var float2Range) &&
                float2Range is not null)
            {
                finiteFloat2++;
                nearUnitFloat2 += float2Range.IsNearUnitRange() ? 1 : 0;
            }

            if (EaglVertexBufferReader.TryMeasureFloat3(
                    data,
                    inspection.Elf,
                    stream,
                    out var float3Bounds) &&
                float3Bounds is not null)
            {
                finiteFloat3++;
                boundsContainment += inspection.Bounds is not null &&
                                     EaglVertexBufferReader.FitsBounds(float3Bounds, inspection.Bounds)
                    ? 1
                    : 0;
            }
        }

        var ranges = streams
            .Select(stream => (stream.SectionIndex, Start: stream.DataOffset, End: checked(stream.DataOffset + stream.ByteLength)))
            .ToArray();
        var distinctRanges = ranges.Distinct().Count();
        var overlapPairs = 0;
        for (var left = 0; left < ranges.Length; left++)
        {
            for (var right = left + 1; right < ranges.Length; right++)
            {
                if (ranges[left].SectionIndex == ranges[right].SectionIndex &&
                    ranges[left].Start < ranges[right].End &&
                    ranges[right].Start < ranges[left].End)
                {
                    overlapPairs++;
                }
            }
        }

        var relativeSignature = EaglVertexBufferReader.GetRelativeRecordStructuralSignature(streams);
        var numericSignature = EaglVertexBufferReader.GetNumericStructuralSignature(
            data,
            inspection.Elf,
            streams,
            inspection.Bounds);
        var valid = streams.Count > 0 && distinctRanges > 0;
        return new(
            modelArchiveName,
            modelObjectName,
            streams.Count,
            finitePacked,
            finiteFloat2,
            nearUnitFloat2,
            finiteFloat3,
            boundsContainment,
            distinctRanges,
            overlapPairs,
            relativeSignature,
            numericSignature,
            valid ? Status : "incomplete-original-numeric-payload-structure",
            OriginalSemantics,
            valid ? string.Empty : "no validated __VB payload range was available");
    }

    public static IReadOnlyList<EaglOriginalVertexStreamNumericRow> InspectStreams(
        string modelArchiveName,
        string modelObjectName,
        ReadOnlyMemory<byte> data,
        EaglObjectInspection inspection,
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchiveName);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelObjectName);
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(streams);

        return streams
            .OrderBy(stream => stream.BufferRecordOffset)
            .ThenBy(stream => stream.DescriptorOffset)
            .Select(stream =>
            {
                var opaqueKeyValid = EaglOriginalVertexRecordAssociationAudit.TryParseOpaqueVertexRecordKey(
                    stream.SymbolName,
                    out var category,
                    out var ordinal);
                var finitePacked = EaglVertexBufferReader.TryMeasurePackedFloatProfile(
                    data.Span,
                    inspection.Elf,
                    stream,
                    out var packedProfile) &&
                    packedProfile is not null;
                var finiteFloat2 = EaglVertexBufferReader.TryMeasureFloat2(
                    data.Span,
                    inspection.Elf,
                    stream,
                    out var float2Range) &&
                    float2Range is not null;
                var finiteFloat3 = EaglVertexBufferReader.TryMeasureFloat3(
                    data.Span,
                    inspection.Elf,
                    stream,
                    out var float3Bounds) &&
                    float3Bounds is not null;
                var boundsContainment = finiteFloat3 &&
                    inspection.Bounds is not null &&
                    EaglVertexBufferReader.FitsBounds(float3Bounds!, inspection.Bounds);
                var valid = stream.ByteLength > 0 &&
                            stream.Stride > 0 &&
                            stream.ByteLength % stream.Stride == 0 &&
                            stream.ElementCount > 0;
                return new EaglOriginalVertexStreamNumericRow(
                    modelArchiveName,
                    modelObjectName,
                    stream.SymbolName,
                    opaqueKeyValid ? category : -1,
                    opaqueKeyValid ? ordinal : uint.MaxValue,
                    opaqueKeyValid,
                    stream.SectionIndex,
                    stream.BufferRecordOffset,
                    stream.DescriptorOffset,
                    stream.DataOffset,
                    stream.ByteLength,
                    stream.Stride,
                    stream.ElementCount,
                    stream.Flags,
                    finitePacked,
                    finiteFloat2,
                    finiteFloat2 && float2Range!.IsNearUnitRange(),
                    finiteFloat3,
                    boundsContainment,
                    valid ? "verified-bounded-original-numeric-stream-profile" : "incomplete-original-numeric-stream-profile",
                    OriginalSemantics,
                    valid ? string.Empty : "validated stream metadata did not form a positive payload range");
            })
            .ToArray();
    }

    public static void WriteReport(string path, IEnumerable<EaglOriginalVertexPayloadRow> rows)
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
                "validated_stream_count",
                "finite_packed_float_profile_count",
                "finite_float2_profile_count",
                "near_unit_range_float2_profile_count",
                "finite_float3_profile_count",
                "declared_bounds_containment_float3_count",
                "distinct_payload_range_count",
                "overlap_payload_pair_count",
                "relative_record_signature",
                "numeric_signature",
                "status",
                "original_semantics",
                "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            row.ValidatedStreamCount.ToString(CultureInfo.InvariantCulture),
            row.FinitePackedFloatProfileCount.ToString(CultureInfo.InvariantCulture),
            row.FiniteFloat2ProfileCount.ToString(CultureInfo.InvariantCulture),
            row.NearUnitRangeFloat2ProfileCount.ToString(CultureInfo.InvariantCulture),
            row.FiniteFloat3ProfileCount.ToString(CultureInfo.InvariantCulture),
            row.DeclaredBoundsContainmentFloat3Count.ToString(CultureInfo.InvariantCulture),
            row.DistinctPayloadRangeCount.ToString(CultureInfo.InvariantCulture),
            row.OverlapPayloadPairCount.ToString(CultureInfo.InvariantCulture),
            Field(row.RelativeRecordSignature),
            Field(row.NumericSignature),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(string path, EaglOriginalVertexPayloadSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive_path",
            "model_package_candidates",
            "model_packages_loaded",
            "object_candidates",
            "objects_loaded",
            "validated_vertex_stream_rows",
            "objects_with_validated_vertex_streams",
            "objects_with_finite_packed_float_profiles",
            "objects_with_finite_float2_profiles",
            "objects_with_declared_bounds_containment",
            "payload_rows",
            "objects_with_distinct_payload_ranges",
            "objects_with_overlapping_payload_ranges",
            "rejected_packages",
            "rejected_objects",
            "status",
            "provenance",
            "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath),
            summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ValidatedVertexStreamRows.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithValidatedVertexStreams.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithFinitePackedFloatProfiles.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithFiniteFloat2Profiles.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithDeclaredBoundsContainment.ToString(CultureInfo.InvariantCulture),
            summary.PayloadRows.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithDistinctPayloadRanges.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithOverlappingPayloadRanges.ToString(CultureInfo.InvariantCulture),
            summary.RejectedPackages.ToString(CultureInfo.InvariantCulture),
            summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status),
            Field(summary.Provenance),
            Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[] { string.Join("\t", fields), string.Join("\t", values) }, new UTF8Encoding(false));
    }

    public static void WriteStreamReport(
        string path,
        IEnumerable<EaglOriginalVertexStreamNumericRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive", "model_object", "symbol_name", "category_key", "ordinal_key",
            "opaque_key_valid", "section_index", "buffer_record_offset", "descriptor_offset",
            "data_offset", "byte_length", "stream_stride", "element_count", "flags",
            "finite_packed_float_profile", "finite_float2_profile", "near_unit_range_float2_profile",
            "finite_float3_profile", "declared_bounds_containment_float3", "status",
            "original_semantics", "issue",
        };
        var lines = new List<string> { string.Join("\t", fields) };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName),
            Field(row.ModelObjectName),
            Field(row.SymbolName),
            row.CategoryKey.ToString(CultureInfo.InvariantCulture),
            row.OrdinalKey.ToString(CultureInfo.InvariantCulture),
            row.OpaqueKeyValid.ToString(),
            row.SectionIndex.ToString(CultureInfo.InvariantCulture),
            $"0x{row.BufferRecordOffset:X}",
            $"0x{row.DescriptorOffset:X}",
            $"0x{row.DataOffset:X}",
            row.ByteLength.ToString(CultureInfo.InvariantCulture),
            row.StreamStride.ToString(CultureInfo.InvariantCulture),
            row.ElementCount.ToString(CultureInfo.InvariantCulture),
            $"0x{row.Flags:X8}",
            row.FinitePackedFloatProfile.ToString(),
            row.FiniteFloat2Profile.ToString(),
            row.NearUnitRangeFloat2Profile.ToString(),
            row.FiniteFloat3Profile.ToString(),
            row.DeclaredBoundsContainmentFloat3.ToString(),
            Field(row.Status),
            Field(row.OriginalSemantics),
            Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
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
