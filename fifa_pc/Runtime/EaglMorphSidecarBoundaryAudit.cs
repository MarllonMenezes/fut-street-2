using System.Globalization;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record EaglMorphSidecarBoundaryObservation(
    string NestedBig,
    string MorphObject,
    string OrdSibling,
    int OrdByteCount,
    int OrlByteCount,
    int CombinedByteCount,
    int FileBackedSectionCount,
    string FileBackedSectionLocations,
    int NonRelocationFileBackedSectionCount,
    int NonRelocationSectionsWhollyBeforeOrdBoundary,
    int NonRelocationSectionsCrossingOrdBoundary,
    int NonRelocationSectionsWhollyAtOrAfterOrdBoundary,
    int RelocationSectionCount,
    int RelocationSectionsWhollyAtOrAfterOrdBoundary,
    int RelocationSectionsStartingAtOrdBoundary,
    bool NonRelocationFileBackedSectionsEndAtOrdBoundary,
    bool AllRelocationSectionsAtOrAfterOrdBoundary,
    string SectionHeaderLocation,
    uint SectionHeaderOffset,
    uint SectionHeaderByteCount,
    long SectionHeaderEndDistanceToCombinedEnd,
    string BoundaryBeforeHex,
    string BoundaryAfterHex);

public sealed record EaglMorphSidecarBoundarySummary(
    int ObservationCount,
    int NestedArchiveCount,
    int NonRelocationSectionsEndAtOrdBoundaryCount,
    int AllRelocationSectionsAtOrAfterOrdBoundaryCount,
    int RelocationSectionsStartingAtOrdBoundaryCount,
    int SectionHeadersInOrlCount,
    int SectionHeadersCrossingBoundaryCount,
    string DistinctOrdByteCounts,
    string DistinctOrlByteCounts,
    string DistinctSectionHeaderLocations);

/// <summary>
/// Measures the physical .ord/.orl split for EAGL morph siblings. It only
/// records ELF section ranges, sidecar lengths, and bounded bytes around the
/// split. It does not assign payload, relocation, symbol, geometry, morph,
/// pose, rig, weight, frame, timing, material, UV, skinning, GPU, or command
/// semantics.
/// </summary>
public static class EaglMorphSidecarBoundaryAudit
{
    public static IReadOnlyList<EaglMorphSidecarBoundaryObservation> Inspect(
        byte[] expandedArchive,
        string nestedBig)
    {
        ArgumentNullException.ThrowIfNull(expandedArchive);
        ArgumentException.ThrowIfNullOrWhiteSpace(nestedBig);
        var archive = BigArchiveReader.Inspect(expandedArchive);
        var ordByStem = archive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var entryNames = archive.Entries
            .Select(entry => entry.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var observations = new List<EaglMorphSidecarBoundaryObservation>();
        foreach (var morphEntry in archive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".o", StringComparison.OrdinalIgnoreCase)))
        {
            var morphData = BigArchiveReader.ReadEntry(expandedArchive, morphEntry.Name);
            var morphInspection = EaglObjectReader.Inspect(morphData);
            var morph = EaglMorphReader.Inspect(morphData, morphInspection.Elf);
            if (!morph.HasMorphSymbol)
            {
                continue;
            }

            if (!EaglMorphReader.TryGetNormalizedSiblingStem(morphEntry.Name, out var siblingStem) ||
                !ordByStem.TryGetValue(siblingStem, out var ordEntry))
            {
                continue;
            }

            var orlName = Path.ChangeExtension(ordEntry.Name, ".orl");
            if (!entryNames.Contains(orlName))
            {
                continue;
            }

            var ordData = BigArchiveReader.ReadEntry(expandedArchive, ordEntry.Name);
            var orlData = BigArchiveReader.ReadEntry(expandedArchive, orlName);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var inspection = ElfReader.Inspect(combinedData);
            observations.Add(Build(
                nestedBig,
                morphEntry.Name,
                ordEntry.Name,
                combinedData,
                ordData.Length,
                orlData.Length,
                inspection));
        }

        return observations;
    }

    public static EaglMorphSidecarBoundaryObservation Build(
        string nestedBig,
        string morphObject,
        string ordSibling,
        ReadOnlySpan<byte> combinedData,
        int ordByteCount,
        int orlByteCount,
        ElfInspection inspection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nestedBig);
        ArgumentException.ThrowIfNullOrWhiteSpace(morphObject);
        ArgumentException.ThrowIfNullOrWhiteSpace(ordSibling);
        ArgumentNullException.ThrowIfNull(inspection);
        if (ordByteCount < 0 || orlByteCount < 0 ||
            (long)ordByteCount + orlByteCount != combinedData.Length)
        {
            throw new InvalidDataException("EAGL morph sidecar lengths do not match the combined ELF payload.");
        }

        var fileBackedSections = inspection.Sections
            .Select(section => new SectionRange(section))
            .Where(item => item.Section.Type != 8 && item.Section.Size > 0)
            .ToArray();
        var nonRelocationSections = fileBackedSections
            .Where(item => !IsRelocationSection(item.Section))
            .ToArray();
        var relocationSections = fileBackedSections
            .Where(item => IsRelocationSection(item.Section))
            .ToArray();
        var nonRelocationEnd = nonRelocationSections.Length == 0
            ? -1L
            : checked((long)nonRelocationSections.Max(item => item.End));
        var sectionHeaderByteCount = checked((uint)inspection.SectionHeaderSize * inspection.SectionCount);
        var sectionHeaderLocation = inspection.SectionHeaderOffset == 0 || sectionHeaderByteCount == 0
            ? "absent"
            : Classify(
                inspection.SectionHeaderOffset,
                checked((ulong)inspection.SectionHeaderOffset + sectionHeaderByteCount),
                ordByteCount).ToString().ToLowerInvariant();
        var sectionHeaderEnd = checked((long)inspection.SectionHeaderOffset + sectionHeaderByteCount);
        var fileBackedSectionLocations = string.Join("|", fileBackedSections
            .Select(item => $"{item.Section.Name}={Classify(item.Start, item.End, ordByteCount).ToString().ToLowerInvariant()}"));
        var boundaryBefore = GetBoundarySlice(
            combinedData,
            Math.Max(0, ordByteCount - 16),
            Math.Min(16, ordByteCount));
        var boundaryAfter = GetBoundarySlice(
            combinedData,
            ordByteCount,
            Math.Min(16, combinedData.Length - ordByteCount));

        return new EaglMorphSidecarBoundaryObservation(
            nestedBig,
            morphObject,
            ordSibling,
            ordByteCount,
            orlByteCount,
            combinedData.Length,
            fileBackedSections.Length,
            fileBackedSectionLocations,
            nonRelocationSections.Length,
            nonRelocationSections.Count(item =>
                Classify(item.Start, item.End, ordByteCount) == BoundaryLocation.Before),
            nonRelocationSections.Count(item =>
                Classify(item.Start, item.End, ordByteCount) == BoundaryLocation.Crossing),
            nonRelocationSections.Count(item =>
                Classify(item.Start, item.End, ordByteCount) == BoundaryLocation.After),
            relocationSections.Length,
            relocationSections.Count(item =>
                Classify(item.Start, item.End, ordByteCount) == BoundaryLocation.After),
            relocationSections.Count(item => item.Start == (ulong)ordByteCount),
            nonRelocationEnd == ordByteCount,
            relocationSections.All(item =>
                Classify(item.Start, item.End, ordByteCount) == BoundaryLocation.After),
            sectionHeaderLocation,
            inspection.SectionHeaderOffset,
            sectionHeaderByteCount,
            checked(sectionHeaderEnd - combinedData.Length),
            boundaryBefore,
            boundaryAfter);
    }

    public static EaglMorphSidecarBoundarySummary Summarize(
        IEnumerable<EaglMorphSidecarBoundaryObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(observations);
        var rows = observations.ToArray();
        return new EaglMorphSidecarBoundarySummary(
            rows.Length,
            rows.Select(row => row.NestedBig).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            rows.Count(row => row.NonRelocationFileBackedSectionsEndAtOrdBoundary),
            rows.Count(row => row.AllRelocationSectionsAtOrAfterOrdBoundary),
            rows.Sum(row => row.RelocationSectionsStartingAtOrdBoundary),
            rows.Count(row => string.Equals(row.SectionHeaderLocation, "after", StringComparison.Ordinal)),
            rows.Count(row => string.Equals(row.SectionHeaderLocation, "crossing", StringComparison.Ordinal)),
            JoinDistinct(rows.Select(row => row.OrdByteCount)),
            JoinDistinct(rows.Select(row => row.OrlByteCount)),
            string.Join(",", rows.Select(row => row.SectionHeaderLocation)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)));
    }

    public static void WriteReport(
        string path,
        IEnumerable<EaglMorphSidecarBoundaryObservation> observations)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(observations);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "nested_big",
                "morph_object",
                "ord_sibling",
                "ord_byte_count",
                "orl_byte_count",
                "combined_byte_count",
                "file_backed_section_count",
                "file_backed_section_locations",
                "non_relocation_file_backed_section_count",
                "non_relocation_sections_wholly_before_ord_boundary",
                "non_relocation_sections_crossing_ord_boundary",
                "non_relocation_sections_wholly_at_or_after_ord_boundary",
                "relocation_section_count",
                "relocation_sections_wholly_at_or_after_ord_boundary",
                "relocation_sections_starting_at_ord_boundary",
                "non_relocation_file_backed_sections_end_at_ord_boundary",
                "all_relocation_sections_at_or_after_ord_boundary",
                "section_header_location",
                "section_header_offset",
                "section_header_byte_count",
                "section_header_end_distance_to_combined_end",
                "boundary_before_hex",
                "boundary_after_hex",
            }),
        };
        lines.AddRange(observations.Select(row => string.Join("\t", new[]
        {
            Tsv(row.NestedBig),
            Tsv(row.MorphObject),
            Tsv(row.OrdSibling),
            row.OrdByteCount.ToString(CultureInfo.InvariantCulture),
            row.OrlByteCount.ToString(CultureInfo.InvariantCulture),
            row.CombinedByteCount.ToString(CultureInfo.InvariantCulture),
            row.FileBackedSectionCount.ToString(CultureInfo.InvariantCulture),
            Tsv(row.FileBackedSectionLocations),
            row.NonRelocationFileBackedSectionCount.ToString(CultureInfo.InvariantCulture),
            row.NonRelocationSectionsWhollyBeforeOrdBoundary.ToString(CultureInfo.InvariantCulture),
            row.NonRelocationSectionsCrossingOrdBoundary.ToString(CultureInfo.InvariantCulture),
            row.NonRelocationSectionsWhollyAtOrAfterOrdBoundary.ToString(CultureInfo.InvariantCulture),
            row.RelocationSectionCount.ToString(CultureInfo.InvariantCulture),
            row.RelocationSectionsWhollyAtOrAfterOrdBoundary.ToString(CultureInfo.InvariantCulture),
            row.RelocationSectionsStartingAtOrdBoundary.ToString(CultureInfo.InvariantCulture),
            row.NonRelocationFileBackedSectionsEndAtOrdBoundary.ToString(),
            row.AllRelocationSectionsAtOrAfterOrdBoundary.ToString(),
            row.SectionHeaderLocation,
            row.SectionHeaderOffset.ToString(CultureInfo.InvariantCulture),
            row.SectionHeaderByteCount.ToString(CultureInfo.InvariantCulture),
            row.SectionHeaderEndDistanceToCombinedEnd.ToString(CultureInfo.InvariantCulture),
            row.BoundaryBeforeHex,
            row.BoundaryAfterHex,
        })));
        WriteLines(path, lines);
    }

    public static void WriteSummary(
        string path,
        EaglMorphSidecarBoundarySummary summary)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(summary);
        WriteLines(path, new[]
        {
            string.Join("\t", new[]
            {
                "observation_count",
                "nested_archive_count",
                "non_relocation_sections_end_at_ord_boundary_count",
                "all_relocation_sections_at_or_after_ord_boundary_count",
                "relocation_sections_starting_at_ord_boundary_count",
                "section_headers_in_orl_count",
                "section_headers_crossing_boundary_count",
                "distinct_ord_byte_counts",
                "distinct_orl_byte_counts",
                "distinct_section_header_locations",
            }),
            string.Join("\t", new[]
            {
                summary.ObservationCount.ToString(CultureInfo.InvariantCulture),
                summary.NestedArchiveCount.ToString(CultureInfo.InvariantCulture),
                summary.NonRelocationSectionsEndAtOrdBoundaryCount.ToString(CultureInfo.InvariantCulture),
                summary.AllRelocationSectionsAtOrAfterOrdBoundaryCount.ToString(CultureInfo.InvariantCulture),
                summary.RelocationSectionsStartingAtOrdBoundaryCount.ToString(CultureInfo.InvariantCulture),
                summary.SectionHeadersInOrlCount.ToString(CultureInfo.InvariantCulture),
                summary.SectionHeadersCrossingBoundaryCount.ToString(CultureInfo.InvariantCulture),
                Tsv(summary.DistinctOrdByteCounts),
                Tsv(summary.DistinctOrlByteCounts),
                Tsv(summary.DistinctSectionHeaderLocations),
            }),
        });
    }

    private static BoundaryLocation Classify(ulong start, ulong end, int ordByteCount)
    {
        if (end <= (ulong)ordByteCount)
        {
            return BoundaryLocation.Before;
        }

        if (start >= (ulong)ordByteCount)
        {
            return BoundaryLocation.After;
        }

        return BoundaryLocation.Crossing;
    }

    private static bool IsRelocationSection(ElfSection section) => section.Type is 4 or 9;

    private static string GetBoundarySlice(ReadOnlySpan<byte> data, int start, int length) =>
        Convert.ToHexString(data.Slice(start, length));

    private static string JoinDistinct(IEnumerable<int> values) =>
        string.Join(",", values.Distinct().OrderBy(value => value)
            .Select(value => value.ToString(CultureInfo.InvariantCulture)));

    private static string Tsv(string value) => value.Replace("\t", " ", StringComparison.Ordinal)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal);

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private enum BoundaryLocation
    {
        Before,
        Crossing,
        After,
    }

    private readonly record struct SectionRange(ElfSection Section)
    {
        public ulong Start => Section.Offset;

        public ulong End => checked((ulong)Section.Offset + Section.Size);
    }
}
