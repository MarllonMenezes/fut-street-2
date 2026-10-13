using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

public sealed record AnimationBankCatalogProfile(
    string CatalogEntry,
    string BankName,
    string VersionTag,
    string Directory,
    int DeclaredFileCount,
    int FileCount,
    bool DeclaredFileCountMatches,
    int UniqueFileNameCount,
    int DuplicateFileNameOccurrences,
    int UniqueTimestampCount,
    int DuplicateTimestampOccurrences,
    string MinimumTimestamp,
    string MaximumTimestamp,
    string OrderedEntrySha256,
    string DistinctFileNameSetSha256,
    string FirstFileName,
    string LastFileName);

public sealed record AnimationBankCatalogPair(
    string BankA,
    string BankB,
    int FileCountA,
    int FileCountB,
    int SharedFileNameCount,
    int SharedFileNameOccurrenceCount,
    int SharedTimestampValueCount,
    int SamePositionFileNameCount,
    int SamePositionTimestampCount,
    int SharedNameTimestampExactOccurrenceCount,
    int SharedNameTimestampDifferentOccurrenceCount,
    int CommonOrderedNamePrefixCount,
    string SharedFileNameSetSha256,
    bool ExactDistinctFileNameSets);

/// <summary>
/// Compares only observed .axt catalog metadata. Names and timestamps are
/// retained as catalog evidence; they are not interpreted as animation
/// records, frames, poses, bones, timing, or selectors.
/// </summary>
public static class AnimationBankCatalogComparison
{
    public static IReadOnlyList<AnimationBankCatalogProfile> BuildProfiles(
        IEnumerable<(string CatalogEntry, AnimationBankCatalog Catalog)> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        return catalogs
            .Select(item => BuildProfile(item.CatalogEntry, item.Catalog))
            .OrderBy(profile => profile.BankName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(profile => profile.CatalogEntry, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<AnimationBankCatalogPair> BuildPairs(
        IEnumerable<(string CatalogEntry, AnimationBankCatalog Catalog)> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var materialized = catalogs
            .Select(item => new CatalogObservation(item.CatalogEntry, item.Catalog))
            .OrderBy(item => item.Catalog.BankName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.CatalogEntry, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var pairs = new List<AnimationBankCatalogPair>(materialized.Length * (materialized.Length - 1) / 2);
        for (var firstIndex = 0; firstIndex < materialized.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < materialized.Length; secondIndex++)
            {
                pairs.Add(BuildPair(materialized[firstIndex], materialized[secondIndex]));
            }
        }

        return pairs;
    }

    public static void WriteProfiles(
        string path,
        IEnumerable<AnimationBankCatalogProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(profiles);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "catalog_entry",
                "bank_name",
                "version_tag",
                "directory",
                "declared_file_count",
                "file_count",
                "declared_file_count_matches",
                "unique_file_name_count",
                "duplicate_file_name_occurrences",
                "unique_timestamp_count",
                "duplicate_timestamp_occurrences",
                "minimum_timestamp",
                "maximum_timestamp",
                "ordered_entry_sha256",
                "distinct_file_name_set_sha256",
                "first_file_name",
                "last_file_name",
            }),
        };
        lines.AddRange(profiles.Select(profile => string.Join("\t", new[]
        {
            Tsv(profile.CatalogEntry),
            Tsv(profile.BankName),
            Tsv(profile.VersionTag),
            Tsv(profile.Directory),
            profile.DeclaredFileCount.ToString(CultureInfo.InvariantCulture),
            profile.FileCount.ToString(CultureInfo.InvariantCulture),
            profile.DeclaredFileCountMatches.ToString(),
            profile.UniqueFileNameCount.ToString(CultureInfo.InvariantCulture),
            profile.DuplicateFileNameOccurrences.ToString(CultureInfo.InvariantCulture),
            profile.UniqueTimestampCount.ToString(CultureInfo.InvariantCulture),
            profile.DuplicateTimestampOccurrences.ToString(CultureInfo.InvariantCulture),
            Tsv(profile.MinimumTimestamp),
            Tsv(profile.MaximumTimestamp),
            profile.OrderedEntrySha256,
            profile.DistinctFileNameSetSha256,
            Tsv(profile.FirstFileName),
            Tsv(profile.LastFileName),
        })));
        WriteLines(path, lines);
    }

    public static void WritePairs(
        string path,
        IEnumerable<AnimationBankCatalogPair> pairs)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(pairs);
        var lines = new List<string>
        {
            string.Join("\t", new[]
            {
                "bank_a",
                "bank_b",
                "file_count_a",
                "file_count_b",
                "shared_file_name_count",
                "shared_file_name_occurrence_count",
                "shared_timestamp_value_count",
                "same_position_file_name_count",
                "same_position_timestamp_count",
                "shared_name_timestamp_exact_occurrence_count",
                "shared_name_timestamp_different_occurrence_count",
                "common_ordered_name_prefix_count",
                "shared_file_name_set_sha256",
                "exact_distinct_file_name_sets",
            }),
        };
        lines.AddRange(pairs.Select(pair => string.Join("\t", new[]
        {
            Tsv(pair.BankA),
            Tsv(pair.BankB),
            pair.FileCountA.ToString(CultureInfo.InvariantCulture),
            pair.FileCountB.ToString(CultureInfo.InvariantCulture),
            pair.SharedFileNameCount.ToString(CultureInfo.InvariantCulture),
            pair.SharedFileNameOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            pair.SharedTimestampValueCount.ToString(CultureInfo.InvariantCulture),
            pair.SamePositionFileNameCount.ToString(CultureInfo.InvariantCulture),
            pair.SamePositionTimestampCount.ToString(CultureInfo.InvariantCulture),
            pair.SharedNameTimestampExactOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            pair.SharedNameTimestampDifferentOccurrenceCount.ToString(CultureInfo.InvariantCulture),
            pair.CommonOrderedNamePrefixCount.ToString(CultureInfo.InvariantCulture),
            pair.SharedFileNameSetSha256,
            pair.ExactDistinctFileNameSets.ToString(),
        })));
        WriteLines(path, lines);
    }

    private static AnimationBankCatalogProfile BuildProfile(
        string catalogEntry,
        AnimationBankCatalog catalog)
    {
        var names = catalog.Files
            .Select(file => file.Name)
            .ToArray();
        var distinctNames = names
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var timestamps = catalog.Files
            .Select(file => file.Timestamp)
            .ToArray();
        var distinctTimestamps = timestamps.Distinct().OrderBy(value => value).ToArray();
        return new AnimationBankCatalogProfile(
            catalogEntry,
            catalog.BankName,
            catalog.VersionTag,
            catalog.Directory,
            catalog.DeclaredFileCount,
            catalog.Files.Count,
            catalog.DeclaredFileCountMatches,
            distinctNames.Length,
            names.Length - distinctNames.Length,
            distinctTimestamps.Length,
            timestamps.Length - distinctTimestamps.Length,
            distinctTimestamps.Length == 0
                ? string.Empty
                : distinctTimestamps[0].ToString(CultureInfo.InvariantCulture),
            distinctTimestamps.Length == 0
                ? string.Empty
                : distinctTimestamps[^1].ToString(CultureInfo.InvariantCulture),
            HashEntries(catalog.Files),
            HashStrings(distinctNames),
            names.FirstOrDefault() ?? string.Empty,
            names.LastOrDefault() ?? string.Empty);
    }

    private static AnimationBankCatalogPair BuildPair(
        CatalogObservation first,
        CatalogObservation second)
    {
        var firstNames = first.Catalog.Files
            .Select(file => file.Name)
            .ToArray();
        var secondNames = second.Catalog.Files
            .Select(file => file.Name)
            .ToArray();
        var firstNameSet = firstNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var secondNameSet = secondNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var sharedNames = firstNameSet
            .Intersect(secondNameSet, StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var firstTimestampSet = first.Catalog.Files.Select(file => file.Timestamp).ToHashSet();
        var secondTimestampSet = second.Catalog.Files.Select(file => file.Timestamp).ToHashSet();
        var secondByName = second.Catalog.Files
            .GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(file => file.Timestamp).ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var exactTimestampOccurrences = 0;
        var differentTimestampOccurrences = 0;
        foreach (var file in first.Catalog.Files)
        {
            if (!secondByName.TryGetValue(file.Name, out var matchingTimestamps))
            {
                continue;
            }

            if (matchingTimestamps.Contains(file.Timestamp))
            {
                exactTimestampOccurrences++;
            }
            else
            {
                differentTimestampOccurrences++;
            }
        }

        var samePositionFileNameCount = 0;
        var samePositionTimestampCount = 0;
        var commonOrderedNamePrefixCount = 0;
        var positionCount = Math.Min(first.Catalog.Files.Count, second.Catalog.Files.Count);
        for (var index = 0; index < positionCount; index++)
        {
            var firstFile = first.Catalog.Files[index];
            var secondFile = second.Catalog.Files[index];
            var sameName = string.Equals(firstFile.Name, secondFile.Name, StringComparison.OrdinalIgnoreCase);
            if (sameName)
            {
                samePositionFileNameCount++;
            }

            if (firstFile.Timestamp == secondFile.Timestamp)
            {
                samePositionTimestampCount++;
            }

            if (commonOrderedNamePrefixCount == index && sameName)
            {
                commonOrderedNamePrefixCount++;
            }
        }

        return new AnimationBankCatalogPair(
            first.Catalog.BankName,
            second.Catalog.BankName,
            first.Catalog.Files.Count,
            second.Catalog.Files.Count,
            sharedNames.Length,
            CountSharedNameOccurrences(first.Catalog.Files, second.Catalog.Files),
            firstTimestampSet.Intersect(secondTimestampSet).Count(),
            samePositionFileNameCount,
            samePositionTimestampCount,
            exactTimestampOccurrences,
            differentTimestampOccurrences,
            commonOrderedNamePrefixCount,
            HashStrings(sharedNames),
            firstNameSet.SetEquals(secondNameSet));
    }

    private static int CountSharedNameOccurrences(
        IReadOnlyList<AnimationCatalogEntry> first,
        IReadOnlyList<AnimationCatalogEntry> second)
    {
        var firstCounts = first
            .GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var secondCounts = second
            .GroupBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        return firstCounts
            .Where(item => secondCounts.ContainsKey(item.Key))
            .Sum(item => Math.Min(item.Value, secondCounts[item.Key]));
    }

    private static string HashEntries(IReadOnlyList<AnimationCatalogEntry> entries)
    {
        var text = string.Join("\n", entries.Select(entry =>
            $"{entry.Name}\t{entry.Timestamp.ToString(CultureInfo.InvariantCulture)}"));
        return HashText(text);
    }

    private static string HashStrings(IEnumerable<string> values)
    {
        return HashText(string.Join("\n", values));
    }

    private static string HashText(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string Tsv(string? value) =>
        value is null ? string.Empty : value.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

    private static void WriteLines(string path, IReadOnlyList<string> lines)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private sealed record CatalogObservation(string CatalogEntry, AnimationBankCatalog Catalog);
}
