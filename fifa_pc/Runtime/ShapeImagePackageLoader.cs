namespace FifaStreetPc;

public sealed record ShapeImageEntry(string Name, uint Offset, uint Size);

public sealed record PcShapeImagePackage(
    string FormatVersion,
    IReadOnlyList<ShapeImageEntry> Entries,
    bool EntryRangesValid);

public static class ShapeImagePackageLoader
{
    /// <summary>
    /// Returns only package-table metadata for a validated SHPX asset. Entry
    /// names, relative offsets, and byte ranges are preserved as structural
    /// evidence; they are not interpreted as materials, UVs, or animation
    /// frames.
    /// </summary>
    public static string GetStructuralSignature(PcShapeImagePackage package)
    {
        ArgumentNullException.ThrowIfNull(package);
        var names = string.Join(",", package.Entries.Select(entry => entry.Name));
        var offsets = string.Join(",", package.Entries.Select(entry => $"0x{entry.Offset:X}"));
        var sizes = string.Join(",", package.Entries.Select(entry => $"0x{entry.Size:X}"));
        return $"format={package.FormatVersion};entries={package.Entries.Count};ranges={package.EntryRangesValid};names={names};offsets={offsets};sizes={sizes}";
    }

    public static PcShapeImagePackage Load(ReadOnlySpan<byte> asset)
    {
        var inspection = XshReader.Inspect(asset);
        var entries = new List<ShapeImageEntry>(inspection.Entries.Count);
        for (var index = 0; index < inspection.Entries.Count; index++)
        {
            var entry = inspection.Entries[index];
            var end = index + 1 < inspection.Entries.Count
                ? inspection.Entries[index + 1].Offset
                : (uint)asset.Length;
            if (end < entry.Offset || end > asset.Length)
            {
                throw new InvalidDataException("SHPX entry range is outside the asset.");
            }

            entries.Add(new ShapeImageEntry(entry.Name, entry.Offset, end - entry.Offset));
        }

        return new PcShapeImagePackage(inspection.FormatVersion, entries, true);
    }
}
