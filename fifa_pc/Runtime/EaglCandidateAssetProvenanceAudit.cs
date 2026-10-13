using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

/// <summary>
/// Re-reads the raw nested BIG entries behind the existing same-trunk
/// candidate matches and records source/payload/pixel hashes. This is an
/// asset provenance check only; it does not assign GPU material, UV,
/// skinning, rigging, animation or original runtime semantics.
/// </summary>
public sealed record EaglCandidateAssetProvenanceAuditRow(
    string ModelArchivePath,
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    string ShapeImageArchiveName,
    bool SameTrunk,
    bool ModelArchiveEntryRead,
    bool ModelObjectEntryRead,
    bool ModelCompanionEntryRead,
    bool CandidateBoundsValid,
    int CandidateGeometryGroups,
    long CandidateVertices,
    long CandidateTriangles,
    int CandidatePrimitiveCount,
    int CandidatePrimitiveShapeStateCount,
    int CandidatePrimitiveShapeStateStreamsCompleteCount,
    bool CandidateGeometryStreamsComplete,
    bool CandidateShapeStateStreamsComplete,
    bool CandidateGeometryFiniteAndIndexed,
    string ModelObjectEntrySha256,
    string ModelCompanionEntrySha256,
    string CandidateMeshSignature,
    string CandidateStructuralStatus,
    bool ShapeArchiveEntryRead,
    bool ShapePayloadValid,
    int ShapePackageEntryCount,
    bool ShapeEntryExact,
    bool ShapeEntryBoundsValid,
    int ShapePayloadBytes,
    uint ShapeEntryOffset,
    uint ShapeEntrySize,
    int TextureWidth,
    int TextureHeight,
    string PixelFormat,
    string ModelArchiveEntrySha256,
    string ShapeArchiveEntrySha256,
    string ShapePayloadSha256,
    string SourceDecodedRgbaSha256,
    string CandidateDecodedRgbaSha256,
    bool DecodedPixelsMatch,
    string Status,
    string CandidateProvenance,
    string OriginalSemantics,
    string Issue);

public sealed record EaglCandidateAssetProvenanceAuditSummary(
    string ModelArchivePath,
    int CandidateCount,
    int SourceChainVerified,
    int ModelArchiveEntriesRead,
    int ShapeArchiveEntriesRead,
    int ShapePayloadsValid,
    int ExactShapeEntries,
    int DecodedPixelMatches,
    int CandidateStructuralVerified,
    int ModelObjectEntriesRead,
    int ModelCompanionEntriesRead,
    int CandidateBoundsValid,
    int CandidateGeometryFiniteAndIndexed,
    int CandidatePrimitiveShapeStateRows,
    int CandidateGeometryStreamsComplete,
    int CandidateShapeStateStreamsComplete,
    string Status,
    string CandidateProvenance,
    string OriginalSemantics);

public static class EaglCandidateAssetProvenanceAudit
{
    public const string CandidateProvenance = "same-trunk-source-chain; candidate-only";
    public const string OriginalSemantics = "not-claimed";

    private const int MaximumNestedEntryBytes = 64 * 1024 * 1024;
    private const uint MaximumExpandedBytes = 64u * 1024u * 1024u;

    public static IReadOnlyList<EaglCandidateAssetProvenanceAuditRow> Audit(
        GameAssetStore store,
        string modelArchivePath)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);

        var matches = EaglShapeMaterialLoader.LoadSameTrunkMatches(store, modelArchivePath);
        var rows = new List<EaglCandidateAssetProvenanceAuditRow>(matches.Count);
        foreach (var match in matches)
        {
            byte[] modelArchiveEntry = Array.Empty<byte>();
            byte[] shapeArchiveEntry = Array.Empty<byte>();
            try
            {
                modelArchiveEntry = ReadBounded(
                    store.OpenBigEntry(modelArchivePath, match.ModelArchiveName));
                shapeArchiveEntry = ReadBounded(
                    store.OpenBigEntry(modelArchivePath, match.ShapeImageArchiveName));
                rows.Add(AuditMatch(
                    modelArchivePath,
                    match,
                    modelArchiveEntry,
                    shapeArchiveEntry));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                EndOfStreamException or
                IOException or
                OverflowException or
                NotSupportedException)
            {
                rows.Add(CreateFailure(
                    modelArchivePath,
                    match,
                    modelArchiveEntry,
                    shapeArchiveEntry,
                    exception.Message));
            }
        }

        return rows;
    }

    public static EaglCandidateAssetProvenanceAuditRow AuditMatch(
        string modelArchivePath,
        EaglShapeMaterialAsset match,
        ReadOnlySpan<byte> modelArchiveEntry,
        ReadOnlySpan<byte> shapeArchiveEntry)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentNullException.ThrowIfNull(match);

        var sameTrunk = string.Equals(
            Path.GetFileNameWithoutExtension(match.ModelArchiveName),
            Path.GetFileNameWithoutExtension(match.ShapeImageArchiveName),
            StringComparison.OrdinalIgnoreCase);
        var modelHash = Hash(modelArchiveEntry);
        var shapeArchiveHash = Hash(shapeArchiveEntry);
        var payload = NormalizeShapePackage(shapeArchiveEntry);
        var payloadHash = Hash(payload);
        var package = ShapeImagePackageLoader.Load(payload);
        var sourceEntry = package.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, match.ShapeName, StringComparison.OrdinalIgnoreCase));
        var entryExact = sourceEntry is not null &&
                         sourceEntry.Name == match.ShapeImageEntry.Name &&
                         sourceEntry.Offset == match.ShapeImageEntry.Offset &&
                         sourceEntry.Size == match.ShapeImageEntry.Size;
        var entryBoundsValid = sourceEntry is not null &&
                               (ulong)sourceEntry.Offset + sourceEntry.Size <= (ulong)payload.Length;
        PcShapeImageTexture? sourceTexture = null;
        if (entryExact && entryBoundsValid)
        {
            sourceTexture = ShapeImageTextureLoader.DecodeNamed(payload, sourceEntry!.Name);
        }

        var sourceRgbaHash = sourceTexture is null ? string.Empty : Hash(sourceTexture.DecodedRgba);
        var candidateRgbaHash = Hash(match.Texture.DecodedRgba);
        var pixelsMatch = sourceTexture is not null &&
                          sourceTexture.Width == match.Texture.Width &&
                          sourceTexture.Height == match.Texture.Height &&
                          string.Equals(sourceTexture.PixelFormat, match.Texture.PixelFormat, StringComparison.Ordinal) &&
                          sourceTexture.DecodedRgba.AsSpan().SequenceEqual(match.Texture.DecodedRgba);
        var candidateObject = AuditCandidateObject(
            modelArchiveEntry,
            match.ModelObjectName,
            match.ShapeName);
        var verified = modelArchiveEntry.Length > 0 &&
                       shapeArchiveEntry.Length > 0 &&
                       sameTrunk &&
                       entryExact &&
                       entryBoundsValid &&
                       pixelsMatch;

        return new EaglCandidateAssetProvenanceAuditRow(
            modelArchivePath,
            match.ModelArchiveName,
            match.ModelObjectName,
            match.ShapeName,
            match.ShapeImageArchiveName,
            sameTrunk,
            modelArchiveEntry.Length > 0,
            candidateObject.ModelObjectEntryRead,
            candidateObject.ModelCompanionEntryRead,
            candidateObject.CandidateBoundsValid,
            candidateObject.CandidateGeometryGroups,
            candidateObject.CandidateVertices,
            candidateObject.CandidateTriangles,
            candidateObject.CandidatePrimitiveCount,
            candidateObject.CandidatePrimitiveShapeStateCount,
            candidateObject.CandidatePrimitiveShapeStateStreamsCompleteCount,
            candidateObject.CandidateGeometryStreamsComplete,
            candidateObject.CandidateShapeStateStreamsComplete,
            candidateObject.CandidateGeometryFiniteAndIndexed,
            candidateObject.ModelObjectEntrySha256,
            candidateObject.ModelCompanionEntrySha256,
            candidateObject.CandidateMeshSignature,
            candidateObject.CandidateStructuralStatus,
            shapeArchiveEntry.Length > 0,
            true,
            package.Entries.Count,
            entryExact,
            entryBoundsValid,
            payload.Length,
            match.ShapeImageEntry.Offset,
            match.ShapeImageEntry.Size,
            match.Texture.Width,
            match.Texture.Height,
            match.Texture.PixelFormat,
            modelHash,
            shapeArchiveHash,
            payloadHash,
            sourceRgbaHash,
            candidateRgbaHash,
            pixelsMatch,
            verified ? "verified-candidate-source-chain" : "candidate-source-chain-mismatch",
            CandidateProvenance,
            OriginalSemantics,
            verified
                ? candidateObject.Issue
                : "same-trunk source entry or decoded pixels did not verify" +
                  (candidateObject.Issue.Length == 0 ? string.Empty : $"; {candidateObject.Issue}"));
    }

    public static EaglCandidateAssetProvenanceAuditSummary Summarize(
        string modelArchivePath,
        IReadOnlyList<EaglCandidateAssetProvenanceAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelArchivePath);
        ArgumentNullException.ThrowIfNull(rows);

        var verified = rows.Count(row => row.Status == "verified-candidate-source-chain");
        return new EaglCandidateAssetProvenanceAuditSummary(
            modelArchivePath,
            rows.Count,
            verified,
            rows.Count(row => row.ModelArchiveEntryRead),
            rows.Count(row => row.ShapeArchiveEntryRead),
            rows.Count(row => row.ShapePayloadValid),
            rows.Count(row => row.ShapeEntryExact),
            rows.Count(row => row.DecodedPixelsMatch),
            rows.Count(row => row.CandidateStructuralStatus == "verified-candidate-mesh-state-crosscheck"),
            rows.Count(row => row.ModelObjectEntryRead),
            rows.Count(row => row.ModelCompanionEntryRead),
            rows.Count(row => row.CandidateBoundsValid),
            rows.Count(row => row.CandidateGeometryFiniteAndIndexed),
            rows.Count(row => row.CandidatePrimitiveShapeStateCount > 0),
            rows.Count(row => row.CandidateGeometryStreamsComplete),
            rows.Count(row => row.CandidateShapeStateStreamsComplete),
            rows.Count > 0 && verified == rows.Count
                ? "verified-candidate-source-chain"
                : "incomplete-candidate-source-chain",
            CandidateProvenance,
            OriginalSemantics);
    }

    public static void WriteReport(
        string path,
        IReadOnlyList<EaglCandidateAssetProvenanceAuditRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(rows);
        EnsureParent(path);
        var lines = new List<string>
        {
            "model_archive_path\tmodel_archive_name\tmodel_object_name\tshape_name\tshape_image_archive_name\tsame_trunk\tmodel_archive_entry_read\tmodel_object_entry_read\tmodel_companion_entry_read\tcandidate_bounds_valid\tcandidate_geometry_groups\tcandidate_vertices\tcandidate_triangles\tcandidate_primitive_count\tcandidate_primitive_shape_state_count\tcandidate_primitive_shape_state_streams_complete_count\tcandidate_geometry_streams_complete\tcandidate_shape_state_streams_complete\tcandidate_geometry_finite_and_indexed\tmodel_object_entry_sha256\tmodel_companion_entry_sha256\tcandidate_mesh_signature\tcandidate_structural_status\tshape_archive_entry_read\tshape_payload_valid\tshape_package_entry_count\tshape_entry_exact\tshape_entry_bounds_valid\tshape_payload_bytes\tshape_entry_offset\tshape_entry_size\ttexture_width\ttexture_height\tpixel_format\tmodel_archive_entry_sha256\tshape_archive_entry_sha256\tshape_payload_sha256\tsource_decoded_rgba_sha256\tcandidate_decoded_rgba_sha256\tdecoded_pixels_match\tstatus\tcandidate_provenance\toriginal_semantics\tissue",
        };
        lines.AddRange(rows.Select(row => string.Join("\t", new[]
        {
            Escape(row.ModelArchivePath),
            Escape(row.ModelArchiveName),
            Escape(row.ModelObjectName),
            Escape(row.ShapeName),
            Escape(row.ShapeImageArchiveName),
            row.SameTrunk.ToString(),
            row.ModelArchiveEntryRead.ToString(),
            row.ModelObjectEntryRead.ToString(),
            row.ModelCompanionEntryRead.ToString(),
            row.CandidateBoundsValid.ToString(),
            row.CandidateGeometryGroups.ToString(CultureInfo.InvariantCulture),
            row.CandidateVertices.ToString(CultureInfo.InvariantCulture),
            row.CandidateTriangles.ToString(CultureInfo.InvariantCulture),
            row.CandidatePrimitiveCount.ToString(CultureInfo.InvariantCulture),
            row.CandidatePrimitiveShapeStateCount.ToString(CultureInfo.InvariantCulture),
            row.CandidatePrimitiveShapeStateStreamsCompleteCount.ToString(CultureInfo.InvariantCulture),
            row.CandidateGeometryStreamsComplete.ToString(),
            row.CandidateShapeStateStreamsComplete.ToString(),
            row.CandidateGeometryFiniteAndIndexed.ToString(),
            row.ModelObjectEntrySha256,
            row.ModelCompanionEntrySha256,
            Escape(row.CandidateMeshSignature),
            row.CandidateStructuralStatus,
            row.ShapeArchiveEntryRead.ToString(),
            row.ShapePayloadValid.ToString(),
            row.ShapePackageEntryCount.ToString(CultureInfo.InvariantCulture),
            row.ShapeEntryExact.ToString(),
            row.ShapeEntryBoundsValid.ToString(),
            row.ShapePayloadBytes.ToString(CultureInfo.InvariantCulture),
            row.ShapeEntryOffset.ToString(CultureInfo.InvariantCulture),
            row.ShapeEntrySize.ToString(CultureInfo.InvariantCulture),
            row.TextureWidth.ToString(CultureInfo.InvariantCulture),
            row.TextureHeight.ToString(CultureInfo.InvariantCulture),
            Escape(row.PixelFormat),
            row.ModelArchiveEntrySha256,
            row.ShapeArchiveEntrySha256,
            row.ShapePayloadSha256,
            row.SourceDecodedRgbaSha256,
            row.CandidateDecodedRgbaSha256,
            row.DecodedPixelsMatch.ToString(),
            row.Status,
            Escape(row.CandidateProvenance),
            row.OriginalSemantics,
            Escape(row.Issue),
        })));
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    public static void WriteSummary(
        string path,
        EaglCandidateAssetProvenanceAuditSummary summary)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(summary);
        EnsureParent(path);
        var lines = new[]
        {
            "field\tvalue",
            $"model_archive_path\t{Escape(summary.ModelArchivePath)}",
            $"candidate_count\t{summary.CandidateCount.ToString(CultureInfo.InvariantCulture)}",
            $"source_chain_verified\t{summary.SourceChainVerified.ToString(CultureInfo.InvariantCulture)}",
            $"model_archive_entries_read\t{summary.ModelArchiveEntriesRead.ToString(CultureInfo.InvariantCulture)}",
            $"shape_archive_entries_read\t{summary.ShapeArchiveEntriesRead.ToString(CultureInfo.InvariantCulture)}",
            $"shape_payloads_valid\t{summary.ShapePayloadsValid.ToString(CultureInfo.InvariantCulture)}",
            $"exact_shape_entries\t{summary.ExactShapeEntries.ToString(CultureInfo.InvariantCulture)}",
            $"decoded_pixel_matches\t{summary.DecodedPixelMatches.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_structural_verified\t{summary.CandidateStructuralVerified.ToString(CultureInfo.InvariantCulture)}",
            $"model_object_entries_read\t{summary.ModelObjectEntriesRead.ToString(CultureInfo.InvariantCulture)}",
            $"model_companion_entries_read\t{summary.ModelCompanionEntriesRead.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_bounds_valid\t{summary.CandidateBoundsValid.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_geometry_finite_and_indexed\t{summary.CandidateGeometryFiniteAndIndexed.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_primitive_shape_state_rows\t{summary.CandidatePrimitiveShapeStateRows.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_geometry_streams_complete\t{summary.CandidateGeometryStreamsComplete.ToString(CultureInfo.InvariantCulture)}",
            $"candidate_shape_state_streams_complete\t{summary.CandidateShapeStateStreamsComplete.ToString(CultureInfo.InvariantCulture)}",
            $"status\t{summary.Status}",
            $"candidate_provenance\t{Escape(summary.CandidateProvenance)}",
            $"original_semantics\t{summary.OriginalSemantics}",
        };
        File.WriteAllLines(path, lines, new UTF8Encoding(false));
    }

    private static EaglCandidateAssetProvenanceAuditRow CreateFailure(
        string modelArchivePath,
        EaglShapeMaterialAsset match,
        ReadOnlySpan<byte> modelArchiveEntry,
        ReadOnlySpan<byte> shapeArchiveEntry,
        string issue) =>
        new(
            modelArchivePath,
            match.ModelArchiveName,
            match.ModelObjectName,
            match.ShapeName,
             match.ShapeImageArchiveName,
             string.Equals(
                Path.GetFileNameWithoutExtension(match.ModelArchiveName),
                Path.GetFileNameWithoutExtension(match.ShapeImageArchiveName),
                 StringComparison.OrdinalIgnoreCase),
             modelArchiveEntry.Length > 0,
             false,
             false,
             false,
             0,
             0,
             0,
             0,
             0,
             0,
             false,
             false,
             false,
             string.Empty,
             string.Empty,
             string.Empty,
             "missing-candidate-mesh-state-crosscheck",
             shapeArchiveEntry.Length > 0,
            false,
            0,
            false,
            false,
            0,
            match.ShapeImageEntry.Offset,
            match.ShapeImageEntry.Size,
            match.Texture.Width,
            match.Texture.Height,
            match.Texture.PixelFormat,
            Hash(modelArchiveEntry),
            Hash(shapeArchiveEntry),
            string.Empty,
            string.Empty,
            Hash(match.Texture.DecodedRgba),
            false,
            "candidate-source-chain-error",
            CandidateProvenance,
            OriginalSemantics,
             issue);

    private sealed record CandidateObjectEvidence(
        bool ModelObjectEntryRead,
        bool ModelCompanionEntryRead,
        bool CandidateBoundsValid,
        int CandidateGeometryGroups,
        long CandidateVertices,
        long CandidateTriangles,
        int CandidatePrimitiveCount,
        int CandidatePrimitiveShapeStateCount,
        int CandidatePrimitiveShapeStateStreamsCompleteCount,
        bool CandidateGeometryStreamsComplete,
        bool CandidateShapeStateStreamsComplete,
        bool CandidateGeometryFiniteAndIndexed,
        string ModelObjectEntrySha256,
        string ModelCompanionEntrySha256,
        string CandidateMeshSignature,
        string CandidateStructuralStatus,
        string Issue);

    private static CandidateObjectEvidence AuditCandidateObject(
        ReadOnlySpan<byte> compressedModelArchive,
        string modelObjectName,
        string shapeName)
    {
        try
        {
            var expandedModel = ExpandModelArchive(compressedModelArchive);
            var archive = BigArchiveReader.Inspect(expandedModel);
            var objectEntry = archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.Name, modelObjectName, StringComparison.OrdinalIgnoreCase));
            if (objectEntry is null ||
                !string.Equals(Path.GetExtension(objectEntry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
            {
                return MissingCandidateObject("candidate .ord entry was not found");
            }

            var companionName = Path.ChangeExtension(objectEntry.Name, ".orl");
            var companionEntry = archive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
            if (companionEntry is null)
            {
                return MissingCandidateObject("candidate .orl companion was not found", objectEntryData: BigArchiveReader.ReadEntry(expandedModel, objectEntry.Name));
            }

            var objectData = BigArchiveReader.ReadEntry(expandedModel, objectEntry.Name);
            var companionData = BigArchiveReader.ReadEntry(expandedModel, companionEntry.Name);
            var inspection = EaglObjectReader.Inspect(objectData, companionData);
            var objectRead = objectData.Length > 0;
            var companionRead = companionData.Length > 0;
            if (inspection.Bounds is null)
            {
                return new CandidateObjectEvidence(
                    objectRead,
                    companionRead,
                    false,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    false,
                    Hash(objectData),
                    Hash(companionData),
                    string.Empty,
                    "incomplete-candidate-mesh-state-crosscheck",
                    "candidate object has no valid bounds");
            }

            var primitives = EaglModelReader.Inspect(objectData, inspection.Elf, inspection.Bounds);
            var mesh = EaglCandidateMeshBuilder.Build(objectData, inspection.Elf, inspection.Bounds);
            var geometry = EaglCandidateGeometryAuditor.Measure(mesh);
            var primitiveShapeStateCount = primitives.Count(primitive =>
                primitive.StateAssignments.Any(assignment =>
                    string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                    string.Equals(
                        assignment.Value.Split(',', 2)[0].Trim(),
                        shapeName,
                        StringComparison.OrdinalIgnoreCase)));
            var shapeStatePrimitives = primitives
                .Where(primitive => primitive.StateAssignments.Any(assignment =>
                    string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal) &&
                    string.Equals(
                        assignment.Value.Split(',', 2)[0].Trim(),
                        shapeName,
                        StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            var geometryStreamsComplete = mesh.Groups.Count > 0 &&
                mesh.Groups.All(group => primitives.Any(primitive =>
                    primitive.LayerIndex == group.LayerIndex &&
                    primitive.PrimitiveIndex == group.PrimitiveIndex &&
                    primitive.RenderCommandStream.IsComplete));
            var shapeStateStreamsCompleteCount = shapeStatePrimitives.Count(primitive =>
                primitive.RenderCommandStream.IsComplete);
            var shapeStateStreamsComplete = shapeStatePrimitives.Length > 0 &&
                shapeStateStreamsCompleteCount == shapeStatePrimitives.Length;
            var finiteAndIndexed = geometry.HasOnlyFiniteInRangeGeometry;
            var verified = objectRead &&
                           companionRead &&
                           mesh.Groups.Count > 0 &&
                           primitives.Count > 0 &&
                           primitiveShapeStateCount > 0 &&
                           geometryStreamsComplete &&
                           shapeStateStreamsComplete &&
                           finiteAndIndexed;
            return new CandidateObjectEvidence(
                objectRead,
                companionRead,
                true,
                mesh.Groups.Count,
                mesh.VertexCount,
                mesh.TriangleCount,
                primitives.Count,
                primitiveShapeStateCount,
                shapeStateStreamsCompleteCount,
                geometryStreamsComplete,
                shapeStateStreamsComplete,
                finiteAndIndexed,
                Hash(objectData),
                Hash(companionData),
                EaglCandidateMeshBuilder.GetStructuralSignature(mesh),
                verified
                    ? "verified-candidate-mesh-state-crosscheck"
                    : "incomplete-candidate-mesh-state-crosscheck",
                verified
                    ? string.Empty
                    : "candidate object, mesh, state, or finite/indexed geometry evidence is incomplete");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or
            EndOfStreamException or
            IOException or
            OverflowException or
            NotSupportedException)
        {
            return MissingCandidateObject($"candidate mesh/state cross-check failed: {exception.Message}");
        }
    }

    private static CandidateObjectEvidence MissingCandidateObject(
        string issue,
        byte[]? objectEntryData = null) =>
        new(
            objectEntryData is { Length: > 0 },
            false,
            false,
            0,
            0,
            0,
            0,
            0,
            0,
            false,
            false,
            false,
            Hash(objectEntryData ?? Array.Empty<byte>()),
            string.Empty,
            string.Empty,
            "missing-candidate-mesh-state-crosscheck",
            issue);

    private static byte[] ExpandModelArchive(ReadOnlySpan<byte> compressed)
    {
        var compressedData = compressed.ToArray();
        if (compressedData.Length == 0)
        {
            throw new InvalidDataException("Compressed model archive is empty.");
        }

        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressedData);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded model archive exceeds the 64 MiB safety limit.");
        }

        using var compressedStream = new MemoryStream(compressedData, writable: false);
        return RefPackDecoder.Decode(compressedStream, expandedSize).Data;
    }

    private static byte[] NormalizeShapePackage(ReadOnlySpan<byte> compressed)
    {
        if (compressed.Length >= 4 && compressed[..4].SequenceEqual("SHPX"u8))
        {
            return compressed.ToArray();
        }

        var compressedData = compressed.ToArray();
        using var compressedStream = new MemoryStream(compressedData, writable: false);
        var expandedSize = RefPackDecoder.ReadDeclaredSize(compressedData);
        if (expandedSize > MaximumExpandedBytes)
        {
            throw new InvalidDataException("Expanded SHPX package exceeds the 64 MiB safety limit.");
        }

        compressedStream.Position = 0;
        return RefPackDecoder.Decode(compressedStream, expandedSize).Data;
    }

    private static byte[] ReadBounded(Stream stream)
    {
        using (stream)
        {
            if (stream.Length > MaximumNestedEntryBytes)
            {
                throw new InvalidDataException("Nested candidate asset exceeds the 64 MiB safety limit.");
            }

            var data = new byte[checked((int)stream.Length)];
            stream.ReadExactly(data);
            return data;
        }
    }

    private static string Hash(ReadOnlySpan<byte> bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes));

    private static void EnsureParent(string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string Escape(string value) =>
        value.Replace("\t", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
