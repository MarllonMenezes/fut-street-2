using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyRow(
    string ModelArchiveName,
    string ModelObjectName,
    int LayerIndex,
    uint PrimitiveIndex,
    int CommandSectionIndex,
    uint CommandStreamOffset,
    int CandidateOrdinalCount,
    int IdentityCandidateCount,
    string IdentityCandidateOrdinals,
    string IdentityCandidateCategories,
    int ExactAdjacencyStreamCount,
    string ExactAdjacencyStreamCategories,
    string ExactAdjacencyStreamStrides,
    bool CurrentOrdinalPresent,
    bool CurrentOrdinalIdentityMatch,
    bool CurrentOrdinalUniquelyIdentified,
    bool ShiftedOrdinalIdentityMatch,
    string Status,
    string OriginalSemantics,
    string Issue);

public sealed record EaglOriginalPrimitiveStreamCommandDescriptorAdjacencySummary(
    string ModelArchivePath,
    uint CommandDescriptorDelta,
    int ModelPackageCandidates,
    int ModelPackagesLoaded,
    int ObjectCandidates,
    int ObjectsLoaded,
    int ObjectsWithValidatedStreams,
    int PrimitiveRows,
    int CompleteCommandRows,
    int CandidateOrdinalRows,
    int IdentityCandidateRows,
    int CurrentOrdinalRows,
    int CurrentOrdinalIdentityRows,
    int UniqueCurrentOrdinalIdentityRows,
    int ShiftedOrdinalIdentityRows,
    int SameSectionIdentityRows,
    int ExactAdjacencyStreamRows,
    string ExactAdjacencyCategoryHistogram,
    string ExactAdjacencyStrideHistogram,
    int RejectedPackages,
    int RejectedObjects,
    string Status,
    string Provenance,
    string OriginalSemantics);

/// <summary>
/// Joins only raw section-relative offsets from the original EAGL command
/// stream and validated __VB descriptors. The fixed delta is an observed
/// byte adjacency control; it is not assigned to a field, vertex attribute,
/// draw operation, material, UV, skinning, rigging, animation, morph or GPU
/// semantic.
/// </summary>
public static class EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit
{
    public const uint CommandDescriptorDelta = 0x0C;
    public const string Status =
        "verified-bounded-original-primitive-stream-command-descriptor-adjacency";
    public const string Provenance =
        "same-trunk-raw-EAGL-command-stream-section-offset+validated-__VB-descriptor-section-offset;exact-observed-byte-adjacency;no-semantic-promotion";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumCompressedBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;
    private const int MaximumModelPackages = 512;
    private const int MaximumObjectEntriesPerPackage = 4096;

    public static EaglOriginalPrimitiveStreamCommandDescriptorAdjacencySummary Audit(
        GameAssetStore store,
        string modelArchivePath,
        IList<EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyRow>? rows = null)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var outputRows = rows ?? new List<EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyRow>();
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
        var completeCommandRows = 0;
        var rejectedPackages = 0;
        var rejectedObjects = 0;
        var exactAdjacencyCategoryHistogram = new Dictionary<int, int>();
        var exactAdjacencyStrideHistogram = new Dictionary<uint, int>();

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
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderBy(item => item.Category)
                                .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
                                .ToArray());

                    foreach (var primitive in primitives
                                 .OrderBy(item => item.LayerIndex)
                                 .ThenBy(item => item.PrimitiveIndex))
                    {
                        primitiveRows++;
                        var commands = primitive.RenderCommandStream.Commands
                            .OrderBy(command => command.Index)
                            .ToArray();
                        var commandOffset = commands.Length == 0 ? 0u : commands[0].SectionOffset;
                        var completeCommand = primitive.RenderCommandStream.IsComplete &&
                                              commands.Length > 0;
                        if (completeCommand)
                        {
                            completeCommandRows++;
                        }

                        var identityCandidates = streamGroups
                            .OrderBy(group => group.Key)
                            .Select(group => (Ordinal: group.Key, Streams: group.Value))
                            .Where(candidate => HasExactAdjacency(
                                primitive,
                                commandOffset,
                                candidate.Streams))
                            .ToArray();
                        var exactAdjacencyStreams = identityCandidates
                            .SelectMany(candidate => FindExactAdjacencyStreams(
                                primitive,
                                commandOffset,
                                candidate.Streams)
                                .Select(stream => (candidate.Ordinal, Stream: stream)))
                            .ToArray();
                        foreach (var (_, stream) in exactAdjacencyStreams)
                        {
                            exactAdjacencyCategoryHistogram[stream.Category] =
                                exactAdjacencyCategoryHistogram.GetValueOrDefault(stream.Category) + 1;
                            exactAdjacencyStrideHistogram[stream.Stream.Stride] =
                                exactAdjacencyStrideHistogram.GetValueOrDefault(stream.Stream.Stride) + 1;
                        }
                        var current = identityCandidates.SingleOrDefault(candidate =>
                            candidate.Ordinal == primitive.PrimitiveIndex);
                        var currentPresent = streamGroups.ContainsKey(primitive.PrimitiveIndex);
                        var currentMatch = current.Streams is not null;
                        var shiftedMatch = primitive.PrimitiveIndex < uint.MaxValue &&
                                           identityCandidates.Any(candidate =>
                                               candidate.Ordinal == primitive.PrimitiveIndex + 1);
                        var rowStatus = completeCommand && currentMatch
                            ? Status
                            : "inconclusive-original-primitive-stream-command-descriptor-adjacency";
                        outputRows.Add(new(
                            modelEntry.Name,
                            objectEntry.Name,
                            primitive.LayerIndex,
                            primitive.PrimitiveIndex,
                            primitive.SourceSectionIndex,
                            commandOffset,
                            streamGroups.Count,
                            identityCandidates.Length,
                            JoinOrdinals(identityCandidates),
                            JoinCategories(identityCandidates),
                            exactAdjacencyStreams.Length,
                            JoinExactCategories(exactAdjacencyStreams),
                            JoinExactStrides(exactAdjacencyStreams),
                            currentPresent,
                            currentMatch,
                            currentMatch && identityCandidates.Length == 1,
                            shiftedMatch,
                            rowStatus,
                            OriginalSemantics,
                            rowStatus == Status
                                ? string.Empty
                                : "command stream incomplete or exact descriptor adjacency was absent"));
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
                     completeCommandRows == primitiveRows &&
                     outputRows.Count == primitiveRows &&
                     outputRows.All(row => row.CurrentOrdinalIdentityMatch &&
                                           row.CurrentOrdinalUniquelyIdentified &&
                                           !row.ShiftedOrdinalIdentityMatch) &&
                     rejectedPackages == 0 &&
                     rejectedObjects == 0
            ? Status
            : "incomplete-original-primitive-stream-command-descriptor-adjacency";

        return new(
            modelArchivePath,
            CommandDescriptorDelta,
            modelEntries.Length,
            packagesLoaded,
            objectCandidates,
            objectsLoaded,
            objectsWithValidatedStreams,
            primitiveRows,
            completeCommandRows,
            outputRows.Sum(row => row.CandidateOrdinalCount),
            outputRows.Sum(row => row.IdentityCandidateCount),
            outputRows.Count(row => row.CurrentOrdinalPresent),
            outputRows.Count(row => row.CurrentOrdinalIdentityMatch),
            outputRows.Count(row => row.CurrentOrdinalUniquelyIdentified),
            outputRows.Count(row => row.ShiftedOrdinalIdentityMatch),
            outputRows.Count(row => row.CurrentOrdinalIdentityMatch),
            outputRows.Sum(row => row.ExactAdjacencyStreamCount),
            FormatHistogram(exactAdjacencyCategoryHistogram),
            FormatHistogram(exactAdjacencyStrideHistogram),
            rejectedPackages,
            rejectedObjects,
            status,
            Provenance,
            OriginalSemantics);
    }

    public static bool HasExactAdjacency(
        EaglModelPrimitive primitive,
        uint commandStreamOffset,
        IReadOnlyList<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream> streams)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(streams);
        if (!primitive.RenderCommandStream.IsComplete ||
            primitive.SourceSectionIndex <= 0 ||
            streams.Count != 5 ||
            streams.Any(item => item.Stream.SectionIndex != primitive.SourceSectionIndex))
        {
            return false;
        }

        var ordered = streams
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select((item, index) => item.Category == ordered[0].Category + index).All(value => value))
        {
            return false;
        }

        return FindExactAdjacencyStreams(primitive, commandStreamOffset, ordered).Count > 0;
    }

    public static IReadOnlyList<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream> FindExactAdjacencyStreams(
        EaglModelPrimitive primitive,
        uint commandStreamOffset,
        IReadOnlyList<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream> streams)
    {
        ArgumentNullException.ThrowIfNull(primitive);
        ArgumentNullException.ThrowIfNull(streams);
        if (!primitive.RenderCommandStream.IsComplete ||
            primitive.SourceSectionIndex <= 0 ||
            streams.Count != 5 ||
            streams.Any(item => item.Stream.SectionIndex != primitive.SourceSectionIndex))
        {
            return Array.Empty<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream>();
        }

        var ordered = streams
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Stream.SymbolName, StringComparer.Ordinal)
            .ToArray();
        if (!ordered.Select((item, index) => item.Category == ordered[0].Category + index).All(value => value))
        {
            return Array.Empty<EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream>();
        }

        return ordered.Where(item =>
            commandStreamOffset >= item.Stream.DescriptorOffset &&
            commandStreamOffset - item.Stream.DescriptorOffset == CommandDescriptorDelta).ToArray();
    }

    private static string JoinOrdinals(
        IEnumerable<(uint Ordinal, EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream[] Streams)> candidates) =>
        string.Join(',', candidates.Select(candidate => candidate.Ordinal.ToString(CultureInfo.InvariantCulture)));

    private static string JoinCategories(
        IEnumerable<(uint Ordinal, EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream[] Streams)> candidates) =>
        string.Join('|', candidates.Select(candidate =>
            $"{candidate.Ordinal.ToString(CultureInfo.InvariantCulture)}:" +
            string.Join(',', candidate.Streams.Select(stream =>
                stream.Category.ToString(CultureInfo.InvariantCulture)))));

    private static string JoinExactCategories(
        IEnumerable<(uint Ordinal, EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream Stream)> matches) =>
        string.Join(',', matches.Select(match =>
            $"{match.Ordinal.ToString(CultureInfo.InvariantCulture)}:{match.Stream.Category.ToString(CultureInfo.InvariantCulture)}"));

    private static string JoinExactStrides(
        IEnumerable<(uint Ordinal, EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream Stream)> matches) =>
        string.Join(',', matches.Select(match =>
            $"{match.Ordinal.ToString(CultureInfo.InvariantCulture)}:{match.Stream.Stream.Stride.ToString(CultureInfo.InvariantCulture)}"));

    private static string FormatHistogram<T>(IReadOnlyDictionary<T, int> histogram)
        where T : notnull =>
        string.Join(',', histogram.OrderBy(pair => pair.Key).Select(pair =>
            $"{pair.Key}:{pair.Value.ToString(CultureInfo.InvariantCulture)}"));

    public static void WriteReport(
        string path,
        IEnumerable<EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "model_archive", "model_object", "layer", "primitive",
                "command_section_index", "command_stream_offset",
                "candidate_ordinal_count", "identity_candidate_count",
                "identity_candidate_ordinals", "identity_candidate_categories",
                "exact_adjacency_stream_count", "exact_adjacency_stream_categories",
                "exact_adjacency_stream_strides",
                "current_ordinal_present", "current_ordinal_identity_match",
                "current_ordinal_uniquely_identified", "shifted_ordinal_identity_match",
                "status", "original_semantics", "issue",
            }),
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Field(row.ModelArchiveName), Field(row.ModelObjectName),
            row.LayerIndex.ToString(CultureInfo.InvariantCulture),
            row.PrimitiveIndex.ToString(CultureInfo.InvariantCulture),
            row.CommandSectionIndex.ToString(CultureInfo.InvariantCulture),
            $"0x{row.CommandStreamOffset:X}",
            row.CandidateOrdinalCount.ToString(CultureInfo.InvariantCulture),
            row.IdentityCandidateCount.ToString(CultureInfo.InvariantCulture),
            Field(row.IdentityCandidateOrdinals), Field(row.IdentityCandidateCategories),
            row.ExactAdjacencyStreamCount.ToString(CultureInfo.InvariantCulture),
            Field(row.ExactAdjacencyStreamCategories), Field(row.ExactAdjacencyStreamStrides),
            row.CurrentOrdinalPresent.ToString(), row.CurrentOrdinalIdentityMatch.ToString(),
            row.CurrentOrdinalUniquelyIdentified.ToString(),
            row.ShiftedOrdinalIdentityMatch.ToString(),
            Field(row.Status), Field(row.OriginalSemantics), Field(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglOriginalPrimitiveStreamCommandDescriptorAdjacencySummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var fields = new[]
        {
            "model_archive_path", "command_descriptor_delta",
            "model_package_candidates", "model_packages_loaded",
            "object_candidates", "objects_loaded", "objects_with_validated_streams",
            "primitive_rows", "complete_command_rows", "candidate_ordinal_rows",
            "identity_candidate_rows", "current_ordinal_rows",
            "current_ordinal_identity_rows", "unique_current_ordinal_identity_rows",
            "shifted_ordinal_identity_rows", "same_section_identity_rows",
            "exact_adjacency_stream_rows", "exact_adjacency_category_histogram",
            "exact_adjacency_stride_histogram",
            "rejected_packages", "rejected_objects", "status", "provenance",
            "original_semantics",
        };
        var values = new[]
        {
            Field(summary.ModelArchivePath),
            summary.CommandDescriptorDelta.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackageCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ModelPackagesLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ObjectCandidates.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsLoaded.ToString(CultureInfo.InvariantCulture),
            summary.ObjectsWithValidatedStreams.ToString(CultureInfo.InvariantCulture),
            summary.PrimitiveRows.ToString(CultureInfo.InvariantCulture),
            summary.CompleteCommandRows.ToString(CultureInfo.InvariantCulture),
            summary.CandidateOrdinalRows.ToString(CultureInfo.InvariantCulture),
            summary.IdentityCandidateRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalRows.ToString(CultureInfo.InvariantCulture),
            summary.CurrentOrdinalIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.UniqueCurrentOrdinalIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.ShiftedOrdinalIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.SameSectionIdentityRows.ToString(CultureInfo.InvariantCulture),
            summary.ExactAdjacencyStreamRows.ToString(CultureInfo.InvariantCulture),
            Field(summary.ExactAdjacencyCategoryHistogram),
            Field(summary.ExactAdjacencyStrideHistogram),
            summary.RejectedPackages.ToString(CultureInfo.InvariantCulture),
            summary.RejectedObjects.ToString(CultureInfo.InvariantCulture),
            Field(summary.Status), Field(summary.Provenance),
            Field(summary.OriginalSemantics),
        };
        File.WriteAllLines(path, new[]
        {
            string.Join("\t", fields),
            string.Join("\t", values),
        }, new UTF8Encoding(false));
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
