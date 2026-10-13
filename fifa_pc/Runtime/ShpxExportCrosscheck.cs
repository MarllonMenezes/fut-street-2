using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FifaStreetPc;

/// <summary>
/// Cross-checks persisted workspace TGA exports against the managed SHPX
/// pixel decode. This validates image-byte preservation only; it does not
/// assign frame timing, pose, animation, material, UV, skinning, morph or
/// GPU meaning. The export provenance is recorded as workspace-generated.
/// </summary>
public sealed record ShpxExportCrosscheckRow(
    string AssetPath,
    string SourceSha256,
    int EntryIndex,
    string EntryName,
    string ExportPath,
    bool ExportExists,
    bool TgaHeaderValid,
    int ExpectedWidth,
    int ExpectedHeight,
    int ExportWidth,
    int ExportHeight,
    string ExpectedPixelFormat,
    int ExpectedRgbaBytes,
    int ExportRgbaBytes,
    string ExpectedRgbaSha256,
    string ExportRgbaSha256,
    string ExportTgaSha256,
    bool PixelBytesMatch,
    string Status,
    string ExportProvenance,
    string OriginalSemantics,
    string Error);

public sealed record ShpxTgaComparison(
    bool HeaderValid,
    int Width,
    int Height,
    byte[] Rgba,
    string TgaSha256,
    string Error);

public sealed record ShpxExportCrosscheckSummary(
    string AssetPath,
    string ExportDirectory,
    string SourceSha256,
    int EntryCount,
    int ExportFilesPresent,
    int HeaderValid,
    int PixelMatches,
    int MissingExports,
    int InvalidExports,
    string Status,
    string ExportProvenance,
    string OriginalSemantics);

public static class ShpxExportCrosscheck
{
    public const string ExportProvenance = "workspace-generated-TGA; provenance-not-independent";
    public const string OriginalSemantics = "not-claimed";

    public static IReadOnlyList<ShpxExportCrosscheckRow> Audit(
        string assetPath,
        ReadOnlySpan<byte> asset,
        string exportDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(exportDirectory);

        var package = ShapeImagePackageLoader.Load(asset);
        var decoded = ShapeImageTextureLoader.DecodeSupported(asset);
        if (decoded.Count != package.Entries.Count)
        {
            throw new InvalidDataException(
                $"SHPX decode count {decoded.Count} does not match package entry count {package.Entries.Count}.");
        }

        var fullDirectory = Path.GetFullPath(exportDirectory);
        var sourceSha256 = Hash(asset);
        var rows = new List<ShpxExportCrosscheckRow>(decoded.Count);
        for (var index = 0; index < decoded.Count; index++)
        {
            var result = decoded[index];
            var exportPath = Path.Combine(
                fullDirectory,
                $"{index:D3}-{SafeName(result.Entry.Name)}.tga");
            if (result.Texture is null)
            {
                rows.Add(new ShpxExportCrosscheckRow(
                    assetPath,
                    sourceSha256,
                    index,
                    result.Entry.Name,
                    exportPath,
                    File.Exists(exportPath),
                    false,
                    0,
                    0,
                    0,
                    0,
                    string.Empty,
                    0,
                    0,
                    string.Empty,
                    string.Empty,
                    File.Exists(exportPath) ? Hash(File.ReadAllBytes(exportPath)) : string.Empty,
                    false,
                    "unsupported-shpx-entry",
                    ExportProvenance,
                    OriginalSemantics,
                    result.UnsupportedReason ?? "managed SHPX decode is unsupported"));
                continue;
            }

            var texture = result.Texture;
            if (!File.Exists(exportPath))
            {
                rows.Add(RowForMissing(assetPath, sourceSha256, index, texture, exportPath));
                continue;
            }

            try
            {
                var comparison = CompareTga(texture, exportPath);
                var expectedHash = Hash(texture.DecodedRgba);
                var exportHash = Hash(comparison.Rgba);
                var match = comparison.HeaderValid &&
                            texture.Width == comparison.Width &&
                            texture.Height == comparison.Height &&
                            texture.DecodedRgba.AsSpan().SequenceEqual(comparison.Rgba);
                rows.Add(new ShpxExportCrosscheckRow(
                    assetPath,
                    sourceSha256,
                    index,
                    texture.Name,
                    exportPath,
                    true,
                    comparison.HeaderValid,
                    texture.Width,
                    texture.Height,
                    comparison.Width,
                    comparison.Height,
                    texture.PixelFormat,
                    texture.DecodedRgba.Length,
                    comparison.Rgba.Length,
                    expectedHash,
                    exportHash,
                    comparison.TgaSha256,
                    match,
                    match ? "verified-export-pixel-match" : "export-mismatch",
                    ExportProvenance,
                    OriginalSemantics,
                    comparison.Error));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or IOException)
            {
                rows.Add(new ShpxExportCrosscheckRow(
                    assetPath,
                    sourceSha256,
                    index,
                    texture.Name,
                    exportPath,
                    true,
                    false,
                    texture.Width,
                    texture.Height,
                    0,
                    0,
                    texture.PixelFormat,
                    texture.DecodedRgba.Length,
                    0,
                    Hash(texture.DecodedRgba),
                    string.Empty,
                    Hash(File.ReadAllBytes(exportPath)),
                    false,
                    "invalid-tga-export",
                    ExportProvenance,
                    OriginalSemantics,
                    exception.Message));
            }
        }

        return rows;
    }

    public static ShpxTgaComparison CompareTga(PcShapeImageTexture texture, string path)
    {
        ArgumentNullException.ThrowIfNull(texture);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var data = File.ReadAllBytes(path);
        if (data.Length < 18)
        {
            throw new InvalidDataException("TGA export is shorter than its header.");
        }

        var idLength = data[0];
        var colorMapType = data[1];
        var imageType = data[2];
        var width = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(12, 2));
        var height = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(14, 2));
        var bitsPerPixel = data[16];
        var descriptor = data[17];
        var pixelOffset = checked(18 + idLength);
        var pixelBytes = checked((int)width * height * 4);
        var headerValid = colorMapType == 0 &&
                          imageType == 2 &&
                          bitsPerPixel == 32 &&
                          descriptor == 0x28 &&
                          pixelOffset + pixelBytes == data.Length;
        if (!headerValid)
        {
            return new ShpxTgaComparison(
                false,
                width,
                height,
                Array.Empty<byte>(),
                Hash(data),
                "TGA header/payload is not an uncompressed top-left 32-bit BGRA export.");
        }

        var rgba = new byte[pixelBytes];
        for (var pixel = 0; pixel < width * height; pixel++)
        {
            var source = pixelOffset + pixel * 4;
            var target = pixel * 4;
            rgba[target] = data[source + 2];
            rgba[target + 1] = data[source + 1];
            rgba[target + 2] = data[source];
            rgba[target + 3] = data[source + 3];
        }

        return new ShpxTgaComparison(true, width, height, rgba, Hash(data), string.Empty);
    }

    public static ShpxExportCrosscheckSummary Summarize(
        string assetPath,
        string exportDirectory,
        IReadOnlyList<ShpxExportCrosscheckRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var matches = rows.Count(row => row.PixelBytesMatch);
        var missing = rows.Count(row => !row.ExportExists);
        var invalid = rows.Count(row => row.ExportExists && !row.TgaHeaderValid);
        var status = rows.Count > 0 && matches == rows.Count
            ? "verified-export-pixel-match"
            : "incomplete-export-crosscheck";
        return new ShpxExportCrosscheckSummary(
            assetPath,
            Path.GetFullPath(exportDirectory),
            rows.FirstOrDefault()?.SourceSha256 ?? string.Empty,
            rows.Count,
            rows.Count(row => row.ExportExists),
            rows.Count(row => row.TgaHeaderValid),
            matches,
            missing,
            invalid,
            status,
            ExportProvenance,
            OriginalSemantics);
    }

    public static void WriteReport(string path, IReadOnlyList<ShpxExportCrosscheckRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new List<string>
        {
            "asset_path\tsource_sha256\tentry_index\tentry_name\texport_path\texport_exists\ttga_header_valid\texpected_width\texpected_height\texport_width\texport_height\texpected_pixel_format\texpected_rgba_bytes\texport_rgba_bytes\texpected_rgba_sha256\texport_rgba_sha256\texport_tga_sha256\tpixel_bytes_match\tstatus\texport_provenance\toriginal_semantics\terror",
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.AssetPath),
            row.SourceSha256,
            row.EntryIndex.ToString(CultureInfo.InvariantCulture),
            Escape(row.EntryName),
            Escape(row.ExportPath),
            row.ExportExists.ToString(),
            row.TgaHeaderValid.ToString(),
            row.ExpectedWidth.ToString(CultureInfo.InvariantCulture),
            row.ExpectedHeight.ToString(CultureInfo.InvariantCulture),
            row.ExportWidth.ToString(CultureInfo.InvariantCulture),
            row.ExportHeight.ToString(CultureInfo.InvariantCulture),
            Escape(row.ExpectedPixelFormat),
            row.ExpectedRgbaBytes.ToString(CultureInfo.InvariantCulture),
            row.ExportRgbaBytes.ToString(CultureInfo.InvariantCulture),
            row.ExpectedRgbaSha256,
            row.ExportRgbaSha256,
            row.ExportTgaSha256,
            row.PixelBytesMatch.ToString(),
            row.Status,
            Escape(row.ExportProvenance),
            row.OriginalSemantics,
            Escape(row.Error),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(string path, ShpxExportCrosscheckSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new[]
        {
            "field\tvalue",
            $"asset_path\t{Escape(summary.AssetPath)}",
            $"export_directory\t{Escape(summary.ExportDirectory)}",
            $"source_sha256\t{summary.SourceSha256}",
            $"entry_count\t{summary.EntryCount.ToString(CultureInfo.InvariantCulture)}",
            $"export_files_present\t{summary.ExportFilesPresent.ToString(CultureInfo.InvariantCulture)}",
            $"tga_header_valid\t{summary.HeaderValid.ToString(CultureInfo.InvariantCulture)}",
            $"pixel_matches\t{summary.PixelMatches.ToString(CultureInfo.InvariantCulture)}",
            $"missing_exports\t{summary.MissingExports.ToString(CultureInfo.InvariantCulture)}",
            $"invalid_exports\t{summary.InvalidExports.ToString(CultureInfo.InvariantCulture)}",
            $"status\t{summary.Status}",
            $"export_provenance\t{Escape(summary.ExportProvenance)}",
            $"original_semantics\t{summary.OriginalSemantics}",
        };
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static ShpxExportCrosscheckRow RowForMissing(
        string assetPath,
        string sourceSha256,
        int index,
        PcShapeImageTexture texture,
        string exportPath) =>
        new(
            assetPath,
            sourceSha256,
            index,
            texture.Name,
            exportPath,
            false,
            false,
            texture.Width,
            texture.Height,
            0,
            0,
            texture.PixelFormat,
            texture.DecodedRgba.Length,
            0,
            Hash(texture.DecodedRgba),
            string.Empty,
            string.Empty,
            false,
            "missing-export",
            ExportProvenance,
            OriginalSemantics,
            "TGA export was not found.");

    private static string SafeName(string name)
    {
        var safe = new string(name.Select(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' ? character : '_').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "image" : safe;
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
