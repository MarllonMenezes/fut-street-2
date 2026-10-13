using System.Buffers.Binary;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public static class SelfTestRunner
{
    public static int Run()
    {
        var tests = new (string Name, Action Test)[]
        {
            ("product-identity", TestProductIdentity),
            ("refpack", TestRefPack),
            ("big", TestBig),
            ("xsh", TestXsh),
            ("xsh-pal8", TestXshPal8),
            ("xsh-pal8-swizzle", TestXshPal8Swizzle),
            ("shpx-image-entry-audit", TestShpxImageEntryAudit),
            ("shpx-export-crosscheck", TestShpxExportCrosscheck),
            ("eagl-candidate-asset-provenance-audit", TestEaglCandidateAssetProvenanceAudit),
            ("eagl-original-render-chain-audit", TestEaglOriginalRenderChainAudit),
            ("eagl-render-parameter-audit", TestEaglRenderParameterAudit),
            ("eagl-indexed-draw-source-audit", TestEaglIndexedDrawSourceAudit),
            ("eagl-material-declaration-audit", TestEaglMaterialDeclarationAudit),
            ("eagl-vertex-program-audit", TestEaglVertexProgramAudit),
            ("eagl-vertex-program-readiness", TestEaglVertexProgramReadiness),
            ("eagl-model-material-reference-audit", TestEaglModelMaterialReferenceAudit),
            ("eagl-model-material-reference-readiness", TestEaglModelMaterialReferenceReadiness),
            ("eagl-original-vertex-record-association-audit", TestEaglOriginalVertexRecordAssociationAudit),
            ("eagl-original-primitive-stream-topology-audit", TestEaglOriginalPrimitiveStreamTopologyAudit),
            ("eagl-original-primitive-stream-opaque-discrimination-audit", TestEaglOriginalPrimitiveStreamOpaqueDiscriminationAudit),
            ("eagl-original-primitive-stream-command-payload-identity-audit", TestEaglOriginalPrimitiveStreamCommandPayloadIdentityAudit),
            ("eagl-original-primitive-stream-command-descriptor-adjacency-audit", TestEaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit),
            ("eagl-original-vertex-payload-audit", TestEaglOriginalVertexPayloadAudit),
            ("eagl-original-state-token-audit", TestEaglOriginalStateTokenAudit),
            ("eagl-candidate-fetch-association-audit", TestEaglCandidateFetchAssociationAudit),
            ("eagl-original-candidate-slot-profile-audit", TestEaglOriginalCandidateSlotProfileAudit),
            ("eagl-original-candidate-raw-stream-identity-audit", TestEaglOriginalCandidateRawStreamIdentityAudit),
            ("apt", TestApt),
            ("apt-correlation", TestAptStructuralCorrelation),
            ("apt-negative-control", TestAptAnimationNegativeControl),
            ("apt-cross-package-profile-audit", TestAptCrossPackageProfileAudit),
            ("morph-ambiguity-summary", TestMorphAmbiguitySummary),
            ("morph-ambiguity-byte-summary", TestMorphAmbiguityByteSummary),
            ("morph-ambiguity-numeric-summary", TestMorphAmbiguityNumericSummary),
            ("morph-ambiguity-group-metadata", TestMorphAmbiguityGroupMetadata),
            ("morph-ambiguity-independent-audit", TestMorphAmbiguityIndependentAudit),
            ("morph-name-state-audit", TestEaglMorphNameStateAudit),
            ("animation-bank", TestAnimationBankReader),
            ("animation-catalog-compare", TestAnimationBankCatalogComparison),
            ("animation-catalog-payload-name-audit", TestAnimationBankCatalogPayloadNameAudit),
            ("animation-catalog-payload-anchor-audit", TestAnimationBankCatalogPayloadAnchorAudit),
            ("animation-catalog-timestamp-audit", TestAnimationBankCatalogTimestampAudit),
            ("external-animation-timing-evidence-audit", TestExternalAnimationTimingEvidenceAudit),
            ("animation-elf-structure", TestAnimationBankElfStructure),
            ("animation-morph-relocation-profile-audit", TestAnimationMorphRelocationProfileAudit),
            ("animation-tail-compare", TestAnimationBankTailComparison),
            ("animation-catalog-tail-range-audit", TestAnimationBankCatalogTailRangeAudit),
            ("animation-tail-pair-relocation-audit", TestAnimationBankTailPairRelocationAudit),
            ("animation-tail-pair-target-chain-audit", TestAnimationBankTailPairTargetChainAudit),
            ("animation-sidecar-boundary-audit", TestAnimationBankSidecarBoundaryAudit),
            ("morph-sidecar-boundary-audit", TestEaglMorphSidecarBoundaryAudit),
            ("morph-shpx-structure-audit", TestMorphShpxStructureAudit),
            ("morph-elf-header-audit", TestMorphElfHeaderAudit),
            ("elf", TestElf),
            ("eagl-object", TestEaglObject),
            ("eagl-morph", TestEaglMorph),
            ("eagl-vertex-buffer", TestEaglVertexBuffer),
            ("byte-compare", TestByteSequenceComparer),
            ("numeric-gap-correlation", TestNumericGapCorrelation),
            ("xpr", TestXpr),
            ("audio", TestAudio),
            ("audio-event-index-evidence-report", TestAudioEventIndexEvidenceReport),
            ("fifast2-csi-reader", TestFifast2CsiReader),
            ("fifast2-csi-tuna-audit", TestFifast2CsiTunaAudit),
            ("fifast2-csi-opaque-range-audit", TestFifast2CsiOpaqueRangeAudit),
            ("fifast2-csi-opaque-field-audit", TestFifast2CsiOpaqueFieldAudit),
            ("audio-named-event-decode", TestAudioNamedEventCandidateDecoder),
            ("audio-named-event-runtime-audit", TestAudioNamedEventRuntimeAudit),
            ("audio-event-parallel-range-audit", TestAudioEventParallelRangeAudit),
            ("audio-event-xbe-hash-search-audit", TestAudioEventXbeHashSearchAudit),
            ("audio-event-audio-asset-hash-universe-audit", TestAudioEventAudioAssetHashSearchAudit),
            ("audio-event-data-asset-hash-universe-audit", TestAudioEventDataAssetHashSearchAudit),
            ("audio-event-name-asset-text-search-audit", TestAudioEventNameAssetTextSearchAudit),
            ("audio-name-streaming-search", TestAudioNameStreamingSearch),
            ("audio-bank-batch-decode", TestAudioBankBatchDecoder),
            ("xbox-ima", TestXboxIma),
            ("audio-backend", TestAudioBackend),
            ("clean-room-audio-assets", TestCleanRoomAudioAssets),
            ("audio-event-payload-wave-correlation-audit", TestAudioEventPayloadWaveCorrelationAudit),
            ("audio-event-payload-wave-correlation-exhaustive-audit", TestAudioEventPayloadWaveCorrelationExhaustiveAudit),
            ("audio-event-abk-raw-identity-audit", TestAudioEventAbkRawIdentityAudit),
            ("gameplay-audio-trace", TestGameplayAudioTrace),
            ("gameplay-audio-coverage-audit", TestGameplayAudioCoverageAudit),
            ("gameplay-coverage-audit", TestGameplayCoverageAudit),
            ("clean-room-gameplay-invariant-audit", TestCleanRoomGameplayInvariantAudit),
            ("clean-room-integrated-coverage-audit", TestCleanRoomIntegratedCoverageAudit),
            ("candidate-integrated-runtime-audit", TestCandidateIntegratedRuntimeAudit),
            ("clean-room-integration-report", TestCleanRoomIntegrationReport),
            ("match", TestMatch),
            ("clean-room-player-animation", TestCleanRoomPlayerAnimator),
            ("clean-room-animation-assets", TestCleanRoomAnimationAssets),
            ("clean-room-character-asset", TestCleanRoomCharacterAsset),
            ("clean-room-character-assets", TestCleanRoomCharacterAssets),
        ("clean-room-authored-animation-trace", TestCleanRoomAuthoredAnimationTrace),
            ("framebuffer", TestFramebuffer),
            ("preview", TestPreview),
            ("eagl-geometry-audit", TestEaglGeometryAudit),
            ("eagl-candidate-render-influence-audit", TestCandidateRenderInfluenceAudit),
            ("morph-candidate-geometry-audit", TestMorphCandidateGeometryAudit),
            ("morph-candidate-geometry-ambiguity-audit", TestMorphCandidateGeometryAmbiguityAudit),
            ("eagl-attribute-audit", TestEaglCandidateAttributeAudit),
            ("eagl-state-correspondence", TestEaglPrimitiveStateCorrespondence),
            ("eagl-state-geometry-profile-audit", TestEaglStateGeometryProfileAudit),
            ("eagl-state-geometry-order-audit", TestEaglStateGeometryOrderAudit),
            ("project-readiness-audit", TestProjectReadinessAudit),
        };

        foreach (var test in tests)
        {
            test.Test();
            Console.WriteLine($"self_test: {test.Name} ok");
        }
        Console.WriteLine($"self_test: {tests.Length} passed");
        return 0;
    }

    private static void TestProductIdentity()
    {
        Assert(GameIdentity.Name == "Fut Street 2", "Public product name changed.");
        Assert(GameIdentity.RuntimeTitle == "Fut Street 2 PC clean-room runtime", "Runtime title changed.");
        Assert(GameIdentity.WindowTitle == "Fut Street 2 PC — clean-room runtime", "Window title changed.");
        var assembly = typeof(GameIdentity).Assembly;
        Assert(assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyTitleAttribute), false)
            .Cast<System.Reflection.AssemblyTitleAttribute>().Single().Title == GameIdentity.Name,
            "Assembly title must match the public product name.");
        Assert(assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyProductAttribute), false)
            .Cast<System.Reflection.AssemblyProductAttribute>().Single().Product == GameIdentity.Name,
            "Assembly product must match the public product name.");
        Assert(assembly.GetName().Name == "FifaStreetPc", "Existing CLI/runner assembly compatibility changed.");
    }

    private static void TestRefPack()
    {
        var input = new byte[]
        {
            0x10, 0xFB, 0x00, 0x00, 0x08, 0xE1,
            (byte)'A', (byte)'B', (byte)'C', (byte)'D',
            (byte)'E', (byte)'F', (byte)'G', (byte)'H', 0xFC
        };
        using var stream = new MemoryStream(input, writable: false);
        Assert(RefPackDecoder.ReadDeclaredSize(input) == 8, "RefPack declared size mismatch.");
        var decoded = RefPackDecoder.Decode(stream, 8);
        Assert(decoded.Data.AsSpan().SequenceEqual("ABCDEFGH"u8), "RefPack output mismatch.");

        var extendedHeader = new byte[input.Length + 1];
        extendedHeader[0] = 0x90;
        extendedHeader[1] = 0xFB;
        BinaryPrimitives.WriteUInt32BigEndian(extendedHeader.AsSpan(2, 4), 8);
        input.AsSpan(5).CopyTo(extendedHeader.AsSpan(6));
        using var extendedStream = new MemoryStream(extendedHeader, writable: false);
        var extendedDecoded = RefPackDecoder.Decode(
            extendedStream,
            RefPackDecoder.ReadDeclaredSize(extendedHeader));
        Assert(extendedDecoded.Data.AsSpan().SequenceEqual("ABCDEFGH"u8), "RefPack extended header decode mismatch.");
    }

    private static void TestBig()
    {
        var data = new byte[40];
        Encoding.ASCII.GetBytes("BIGF").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 40);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(12, 4), 32);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16, 4), 32);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(20, 4), 8);
        Encoding.ASCII.GetBytes("test\0").CopyTo(data, 24);
        Encoding.ASCII.GetBytes("PAYLOAD!").CopyTo(data, 32);
        var inspection = BigArchiveReader.Inspect(data);
        Assert(inspection.FileCount == 1 && inspection.Entries[0].Name == "test", "BIG fixture mismatch.");
        Assert(BigArchiveReader.ReadEntry(data, "test").AsSpan().SequenceEqual("PAYLOAD!"u8), "BIG entry mismatch.");
    }

    private static void TestXsh()
    {
        var data = new byte[32];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("02  ").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 24);
        var package = ShapeImagePackageLoader.Load(data);
        Assert(package.FormatVersion == "G359", "XSH version fixture mismatch.");
        Assert(package.Entries.Count == 1 && package.Entries[0].Name == "02" && package.Entries[0].Size == 8,
            "XSH entry fixture mismatch.");
        Assert(ShapeImagePackageLoader.GetStructuralSignature(package) ==
               "format=G359;entries=1;ranges=True;names=02;offsets=0x18;sizes=0x8",
            "XSH structural package signature mismatch.");
        Assert(GameAssetInventory.Describe("test.xsh", data.Length, data).Kind == AssetKind.ShapeImagePackage,
            "SHPX was not classified as an image package.");
    }

    private static void TestXshPal8()
    {
        const int entryOffset = 24;
        const int imageChunkSize = 20;
        const int paletteChunkSize = 1040;
        var data = new byte[entryOffset + imageChunkSize + paletteChunkSize];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("frm1").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), entryOffset);

        var image = data.AsSpan(entryOffset, imageChunkSize);
        image[0] = 0x7B;
        image[1] = checked((byte)imageChunkSize);
        BinaryPrimitives.WriteUInt16LittleEndian(image[4..6], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(image[6..8], 2);
        new byte[] { 1, 2, 3, 4 }.AsSpan().CopyTo(image[16..20]);

        var palette = data.AsSpan(entryOffset + imageChunkSize, paletteChunkSize);
        palette[0] = 0x2A;
        palette[1] = 0x10;
        palette[2] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(palette[4..6], 256);
        BinaryPrimitives.WriteUInt16LittleEndian(palette[6..8], 1);
        new byte[] { 10, 20, 30, 40 }.AsSpan().CopyTo(palette[20..24]);
        new byte[] { 50, 60, 70, 80 }.AsSpan().CopyTo(palette[24..28]);
        new byte[] { 90, 100, 110, 120 }.AsSpan().CopyTo(palette[28..32]);
        new byte[] { 130, 140, 150, 160 }.AsSpan().CopyTo(palette[32..36]);

        var decoded = ShapeImageTextureLoader.DecodeSupported(data);
        Assert(decoded.Count == 1 && decoded[0].UnsupportedReason is null, "SHPX PAL8 fixture was not decoded.");
        var texture = decoded[0].Texture;
        Assert(texture is not null && texture.PixelFormat == "PAL8", "SHPX PAL8 output metadata mismatch.");
        Assert(texture!.DecodedRgba.AsSpan(0, 16).SequenceEqual(new byte[]
        {
            30, 20, 10, 40,
            70, 60, 50, 80,
            110, 100, 90, 120,
            150, 140, 130, 160,
        }), "SHPX PAL8 palette expansion mismatch.");
        var namedTexture = ShapeImageTextureLoader.DecodeNamed(data, "frm1");
        Assert(namedTexture.PixelFormat == "PAL8" && namedTexture.Width == 2 && namedTexture.Height == 2,
            "Named SHPX entry decoding mismatch.");
    }

    private static void TestXshPal8Swizzle()
    {
        const int entryOffset = 24;
        const int imageChunkSize = 24;
        const int paletteChunkSize = 1040;
        var data = new byte[entryOffset + imageChunkSize + paletteChunkSize];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("frm1").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), entryOffset);

        var image = data.AsSpan(entryOffset, imageChunkSize);
        image[0] = 0x7B;
        image[1] = imageChunkSize;
        BinaryPrimitives.WriteUInt16LittleEndian(image[4..6], 4);
        BinaryPrimitives.WriteUInt16LittleEndian(image[6..8], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(image[12..14], 0x2000);
        new byte[] { 1, 2, 5, 6, 3, 4, 7, 8 }.AsSpan().CopyTo(image[16..24]);

        var palette = data.AsSpan(entryOffset + imageChunkSize, paletteChunkSize);
        palette[0] = 0x2A;
        palette[1] = 0x10;
        palette[2] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(palette[4..6], 256);
        BinaryPrimitives.WriteUInt16LittleEndian(palette[6..8], 1);
        for (var color = 1; color <= 8; color++)
        {
            var offset = 16 + color * 4;
            palette[offset] = 0;
            palette[offset + 1] = 0;
            palette[offset + 2] = checked((byte)color);
            palette[offset + 3] = 255;
        }

        var decoded = ShapeImageTextureLoader.DecodeSupported(data);
        var texture = decoded[0].Texture;
        Assert(texture is not null, "SHPX swizzled PAL8 fixture was not decoded.");
        Assert(texture!.DecodedRgba.AsSpan(0, 32).SequenceEqual(new byte[]
        {
            1, 0, 0, 255, 2, 0, 0, 255, 3, 0, 0, 255, 4, 0, 0, 255,
            5, 0, 0, 255, 6, 0, 0, 255, 7, 0, 0, 255, 8, 0, 0, 255,
        }), "SHPX PAL8 Morton deswizzle mismatch.");
    }

    private static void TestShpxImageEntryAudit()
    {
        const int entryOffset = 24;
        const int imageChunkSize = 20;
        const int paletteChunkSize = 1040;
        var data = new byte[entryOffset + imageChunkSize + paletteChunkSize];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("frm1").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), entryOffset);

        var image = data.AsSpan(entryOffset, imageChunkSize);
        image[0] = 0x7B;
        image[1] = checked((byte)imageChunkSize);
        BinaryPrimitives.WriteUInt16LittleEndian(image[4..6], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(image[6..8], 2);
        new byte[] { 1, 2, 3, 4 }.AsSpan().CopyTo(image[16..20]);

        var palette = data.AsSpan(entryOffset + imageChunkSize, paletteChunkSize);
        palette[0] = 0x2A;
        palette[1] = 0x10;
        palette[2] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(palette[4..6], 256);
        BinaryPrimitives.WriteUInt16LittleEndian(palette[6..8], 1);
        for (var color = 1; color <= 4; color++)
        {
            var offset = 16 + color * 4;
            palette[offset] = checked((byte)color);
            palette[offset + 1] = 20;
            palette[offset + 2] = 30;
            palette[offset + 3] = 255;
        }

        var rows = ShpxImageEntryAudit.Audit("fixture/player1.xsh", data);
        Assert(rows.Count == 1 &&
               rows[0].EntryIndex == 0 &&
               rows[0].EntryName == "frm1" &&
               rows[0].DecodeStatus == "decoded" &&
               rows[0].Width == 2 &&
               rows[0].Height == 2 &&
               rows[0].PixelFormat == "PAL8" &&
               rows[0].DecodedRgbaBytes == 16 &&
               rows[0].AuditStatus == "verified-image-entries" &&
               rows[0].TimingEvidence == "not-proven" &&
               rows[0].OriginalSemantics == "not-claimed" &&
               rows[0].OrderedEntryTableSha256.Length == 64 &&
               rows[0].OrderedDecodedEntrySha256.Length == 64,
            "SHPX image-entry audit did not preserve decoded structural evidence.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-shpx-image-entry-audit-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            ShpxImageEntryAudit.WriteReport(reportPath, rows);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0].StartsWith("asset_path\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("verified-image-entries", StringComparison.Ordinal),
                "SHPX image-entry audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestShpxExportCrosscheck()
    {
        const int entryOffset = 24;
        const int imageChunkSize = 20;
        const int paletteChunkSize = 1040;
        var data = new byte[entryOffset + imageChunkSize + paletteChunkSize];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("frm1").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), entryOffset);

        var image = data.AsSpan(entryOffset, imageChunkSize);
        image[0] = 0x7B;
        image[1] = checked((byte)imageChunkSize);
        BinaryPrimitives.WriteUInt16LittleEndian(image[4..6], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(image[6..8], 2);
        new byte[] { 1, 2, 3, 4 }.AsSpan().CopyTo(image[16..20]);

        var palette = data.AsSpan(entryOffset + imageChunkSize, paletteChunkSize);
        palette[0] = 0x2A;
        palette[1] = 0x10;
        palette[2] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(palette[4..6], 256);
        BinaryPrimitives.WriteUInt16LittleEndian(palette[6..8], 1);
        for (var color = 1; color <= 4; color++)
        {
            var offset = 16 + color * 4;
            palette[offset] = checked((byte)color);
            palette[offset + 1] = 20;
            palette[offset + 2] = 30;
            palette[offset + 3] = 255;
        }

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-shpx-export-crosscheck-");
        try
        {
            var export = ShapeImageTextureWriter.ExportAll(data, temporaryDirectory.FullName);
            Assert(export.WrittenFiles.Count == 1 && export.Unsupported.Count == 0,
                "SHPX export fixture did not produce exactly one TGA.");

            var rows = ShpxExportCrosscheck.Audit(
                "fixture/player1.xsh",
                data,
                temporaryDirectory.FullName);
            Assert(rows.Count == 1 &&
                   rows[0].EntryIndex == 0 &&
                   rows[0].EntryName == "frm1" &&
                   rows[0].ExportExists &&
                   rows[0].TgaHeaderValid &&
                   rows[0].ExpectedWidth == 2 &&
                   rows[0].ExpectedHeight == 2 &&
                   rows[0].ExportWidth == 2 &&
                   rows[0].ExportHeight == 2 &&
                   rows[0].ExpectedPixelFormat == "PAL8" &&
                   rows[0].PixelBytesMatch &&
                   rows[0].Status == "verified-export-pixel-match" &&
                   rows[0].ExportProvenance == ShpxExportCrosscheck.ExportProvenance &&
                   rows[0].OriginalSemantics == "not-claimed" &&
                   rows[0].ExpectedRgbaSha256.Length == 64 &&
                   rows[0].ExportRgbaSha256 == rows[0].ExpectedRgbaSha256 &&
                   rows[0].ExportTgaSha256.Length == 64,
                "SHPX export cross-check did not verify persisted TGA pixels.");

            var summary = ShpxExportCrosscheck.Summarize(
                "fixture/player1.xsh",
                temporaryDirectory.FullName,
                rows);
            Assert(summary.EntryCount == 1 &&
                   summary.ExportFilesPresent == 1 &&
                   summary.HeaderValid == 1 &&
                   summary.PixelMatches == 1 &&
                   summary.Status == "verified-export-pixel-match" &&
                   summary.ExportProvenance == ShpxExportCrosscheck.ExportProvenance &&
                   summary.OriginalSemantics == "not-claimed",
                "SHPX export cross-check summary mismatch.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            ShpxExportCrosscheck.WriteReport(reportPath, rows);
            ShpxExportCrosscheck.WriteSummary(summaryPath, summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0].StartsWith("asset_path\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("verified-export-pixel-match", StringComparison.Ordinal) &&
                   summaryLines.Any(line => line == "pixel_matches\t1"),
                "SHPX export cross-check TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglCandidateAssetProvenanceAudit()
    {
        const int entryOffset = 24;
        const int imageChunkSize = 20;
        const int paletteChunkSize = 1040;
        var data = new byte[entryOffset + imageChunkSize + paletteChunkSize];
        Encoding.ASCII.GetBytes("SHPX").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 1);
        Encoding.ASCII.GetBytes("G359").CopyTo(data, 12);
        Encoding.ASCII.GetBytes("frm1").CopyTo(data, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), entryOffset);

        var image = data.AsSpan(entryOffset, imageChunkSize);
        image[0] = 0x7B;
        image[1] = checked((byte)imageChunkSize);
        BinaryPrimitives.WriteUInt16LittleEndian(image[4..6], 2);
        BinaryPrimitives.WriteUInt16LittleEndian(image[6..8], 2);
        new byte[] { 1, 2, 3, 4 }.AsSpan().CopyTo(image[16..20]);

        var palette = data.AsSpan(entryOffset + imageChunkSize, paletteChunkSize);
        palette[0] = 0x2A;
        palette[1] = 0x10;
        palette[2] = 0x04;
        BinaryPrimitives.WriteUInt16LittleEndian(palette[4..6], 256);
        BinaryPrimitives.WriteUInt16LittleEndian(palette[6..8], 1);
        for (var color = 1; color <= 4; color++)
        {
            var offset = 16 + color * 4;
            palette[offset] = checked((byte)color);
            palette[offset + 1] = 20;
            palette[offset + 2] = 30;
            palette[offset + 3] = 255;
        }

        var package = ShapeImagePackageLoader.Load(data);
        var texture = ShapeImageTextureLoader.DecodeNamed(data, "frm1");
        var match = new EaglShapeMaterialAsset(
            "data/models.big",
            "tops0030.big",
            "tops0030.ord",
            "frm1",
            "tops0030.xsh",
            package.Entries.Single(),
            texture);
        var row = EaglCandidateAssetProvenanceAudit.AuditMatch(
            "data/models.big",
            match,
            new byte[] { 1, 2, 3 },
            data);
        Assert(row.SameTrunk &&
               row.ModelArchiveEntryRead &&
               row.ShapeArchiveEntryRead &&
               row.ShapePayloadValid &&
               row.ShapePackageEntryCount == 1 &&
               row.ShapeEntryExact &&
               row.ShapeEntryBoundsValid &&
               row.DecodedPixelsMatch &&
               !row.ModelObjectEntryRead &&
               row.CandidateStructuralStatus == "missing-candidate-mesh-state-crosscheck" &&
               row.Status == "verified-candidate-source-chain" &&
               row.CandidateProvenance == EaglCandidateAssetProvenanceAudit.CandidateProvenance &&
               row.OriginalSemantics == "not-claimed" &&
               row.ModelArchiveEntrySha256.Length == 64 &&
               row.ShapePayloadSha256.Length == 64,
            "Candidate asset provenance audit did not preserve the verified source chain.");

        var summary = EaglCandidateAssetProvenanceAudit.Summarize(
            "data/models.big",
            new[] { row });
        Assert(summary.CandidateCount == 1 &&
               summary.SourceChainVerified == 1 &&
               summary.ExactShapeEntries == 1 &&
               summary.DecodedPixelMatches == 1 &&
               summary.CandidateStructuralVerified == 0 &&
               summary.Status == "verified-candidate-source-chain" &&
               summary.OriginalSemantics == "not-claimed",
            "Candidate asset provenance audit summary mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-candidate-provenance-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            EaglCandidateAssetProvenanceAudit.WriteReport(reportPath, new[] { row });
            EaglCandidateAssetProvenanceAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Any(line => line == "source_chain_verified\t1"),
                "Candidate asset provenance audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglOriginalRenderChainAudit()
    {
        var primitive = new EaglModelPrimitive(
            2,
            7,
            0x30,
            0x40,
            0x50,
            "__EAGL::TAR:::SHAPENAME=fixture,1;SetPrimitiveType=EAGL::PT_TRIANGLESTRIP",
            new[]
            {
                new EaglStateAssignment("SHAPENAME", "fixture,1"),
                new EaglStateAssignment("SetPrimitiveType", "EAGL::PT_TRIANGLESTRIP"),
            },
            "PT_TRIANGLESTRIP",
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(
                        0,
                        0x80,
                        9,
                        4,
                        new byte[16],
                        Array.Empty<string>()),
                },
                true,
                null));

        Assert(EaglOriginalRenderChainAudit.IsStructurallyValid(primitive),
            "Valid original EAGL render-chain fixture was rejected.");
        var row = EaglOriginalRenderChainAudit.InspectPrimitive(
            "fixture.big",
            "fixture.ord",
            primitive);
        Assert(row.Status == EaglOriginalRenderChainAudit.Status &&
               row.RenderDescriptorOffset == 0x30 &&
               row.RenderMethodOffset == 0x40 &&
               row.GlobalParametersOffset == 0x50 &&
               row.CommandStreamOffset == 0x80 &&
               row.CommandCount == 1 &&
               row.CommandIds == "9" &&
               row.CommandBytes == 16 &&
               row.CommandStreamComplete &&
               row.StateRelocationPresent &&
               row.HasShapeNameAssignment &&
               row.HasPrimitiveTypeAssignment &&
               row.StateShapePrimitiveTypeOverlap &&
               row.OriginalSemantics == "not-claimed",
            "Original EAGL render-chain row did not preserve opaque structural evidence.");

        var summary = new EaglOriginalRenderChainSummary(
            "data/models.big",
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            0,
            0,
            0,
            1,
            0,
            0,
            EaglOriginalRenderChainAudit.Status,
            EaglOriginalRenderChainAudit.Provenance,
            "not-claimed",
            "inconclusive-original-state-population-control",
            "inconclusive-original-vertex-record-structure");
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-original-render-chain-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            EaglOriginalRenderChainAudit.WriteReport(reportPath, new[] { row });
            EaglOriginalRenderChainAudit.WriteSummary(summaryPath, summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0].StartsWith("model_archive\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("\tTrue\tTrue\t__EAGL::TAR", StringComparison.Ordinal) &&
                   reportLines[0].EndsWith("\tcommand_ids", StringComparison.Ordinal) &&
                   reportLines[1].EndsWith("\t9", StringComparison.Ordinal) &&
                   summaryLines.Length == 2 &&
                   summaryLines[0].StartsWith("model_archive_path\t", StringComparison.Ordinal) &&
                   summaryLines[1].Contains(EaglOriginalRenderChainAudit.Status, StringComparison.Ordinal),
                "Original EAGL render-chain TSV output mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglRenderParameterAudit()
    {
        var data = new byte[512];
        void Word(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);
        Word(0x44, 1); Word(0x48, 0); // external state relocation
        Word(0x4c, 4); Word(0x50, 0x94); // __VB base + 20
        Word(0x80, 0xc0); Word(0x84, 48); Word(0x88, 12); Word(0x8c, 1); Word(0x90, 0x100);
        Word(0xc0, 1); Word(0xc4, 0x100);
        var elf = new ElfInspection(true, true, 1, 3, 0, 40, 2, true,
            new[] { new ElfSection("", 0, 0, 0, 0, 0, 0, 0), new ElfSection(".data", 1, 3, 0, 512, 0, 0, 0) },
            new[] { new ElfSymbol(0, 0, "__VB:::fixture", 0x80, 20, 0, 1) },
            new[] { new ElfRelocation(".rel.data", 1, 0x48, 2, "__EAGL::GeoPrimState:::fixture"),
                    new ElfRelocation(".rel.data", 1, 0x50, 2, "") });
        EaglRenderCommandEvidence Command(int index, ushort id, params uint[] payload)
        {
            var raw = new byte[(payload.Length + 1) * 4];
            BinaryPrimitives.WriteUInt32LittleEndian(raw, (uint)id << 16 | (uint)(payload.Length + 1));
            for (var i = 0; i < payload.Length; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan((i + 1) * 4), payload[i]);
            return new(index, (uint)(0x140 + index * 24), id, (ushort)(payload.Length + 1), raw, Array.Empty<string>());
        }
        var commands = new[] { Command(0, 4, 0), Command(1, 16, 0), Command(2, 9, 48, 0, 12), Command(3, 26, 0, 5) };
        var primitive = new EaglModelPrimitive(0, 0, 0x40, 0x20, 0x44, null,
            Array.Empty<EaglStateAssignment>(), null, Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(), Array.Empty<EaglVertexFetchBinding>(), false,
            new EaglRenderCommandStream(commands, true, null)) { SourceSectionIndex = 1 };
        var obj = new EaglRawModelObject("fixture.big", "fixture.ord", data, elf, new[] { primitive });
        var rows = EaglRenderParameterAudit.Inspect(obj);
        Assert(rows.Count == 4 && rows.All(r => r.Status == EaglRenderParameterAudit.Status) &&
               rows[0].ParameterCursor == 0x44 && rows[1].ParameterCursor == 0x44 &&
               rows[2].ParameterCursor == 0x4c && rows[3].ParameterCursor == 0x54 &&
               rows[2].VertexBufferBaseSymbol == "__VB:::fixture" && rows[2].BufferStride == 12 &&
               rows[2].AddressScope == "local-base-only-runtime-selector-unresolved" &&
               rows[1].RawTargetWords == "" && rows[1].TargetSymbols == "" && rows[3].DrawCondition == 5,
            "Static parameter layout, external relocation or unresolved selector was misinterpreted.");

        var unknown = commands.ToArray(); unknown[1] = Command(1, 500, 0);
        var rejected = EaglRenderParameterAudit.Inspect(obj with { Primitives = new[] {
            primitive with { RenderCommandStream = new EaglRenderCommandStream(unknown, true, null) } } });
        Assert(rejected.Skip(1).All(r => r.Status != EaglRenderParameterAudit.Status && r.Word1 is null) &&
               rejected[2].ParameterCursor == 0x44, "Unknown command must stop parameter cursor recovery.");
        var neighbor = primitive with { PrimitiveIndex = 1, RenderDescriptorOffset = 0x50,
            RenderCommandStream = new EaglRenderCommandStream(Array.Empty<EaglRenderCommandEvidence>(), true, null) };
        var bounded = EaglRenderParameterAudit.Inspect(obj with { Primitives = new[] { primitive, neighbor } });
        Assert(bounded[2].Status != EaglRenderParameterAudit.Status && bounded[2].Word1 is null,
            "Parameter parser read across the next descriptor boundary.");
        var malformed = commands.ToArray(); malformed[2] = commands[2] with { RawBytes = new byte[16] };
        Assert(EaglRenderParameterAudit.Inspect(obj with { Primitives = new[] { primitive with {
            RenderCommandStream = new EaglRenderCommandStream(malformed, true, null) } } })[2].Status != EaglRenderParameterAudit.Status,
            "Mismatched raw command header was accepted.");

        var directory = Directory.CreateTempSubdirectory("fifa-parameter-readiness-");
        try
        {
            Assert(!ProjectReadinessAudit.ReadRenderParameterControl(directory.FullName).GatePassed,
                "Missing parameter evidence unexpectedly passed readiness.");
            var evidence = new Dictionary<string, string>();
            foreach (var token in ("primitive_rows=2364 command_rows=36779 parameter_pairs=32051 " +
                     "fetch_base_record_matches=11818 fetch_stride_matches=11818 fetch_runtime_selector_unresolved=7092 " +
                     "draw_commands=2364 draw_condition_nonzero=2364 tar_state_parameters=2364 geo_state_parameters=1612 " +
                     "primitives_with_both_state_parameters=1612 static_spans_validated=20 static_sites_validated=106 " +
                     "gpu_method_headers_verified=3 negative_controls_passed=6").Split(' '))
            {
                var pair = token.Split('='); evidence[pair[0]] = pair[1];
            }
            foreach (var (name, field) in new[] {
                ("eagl-render-parameter-audit.tsv", "parameter_report_sha256"),
                ("eagl-original-render-chain.tsv", "chain_report_sha256"),
                ("eagl-render-parameter-static-control.tsv", "static_report_sha256"),
            })
            {
                var bytes = Encoding.UTF8.GetBytes("fixture report " + name);
                File.WriteAllBytes(Path.Combine(directory.FullName, name), bytes);
                evidence[field] = Convert.ToHexString(SHA256.HashData(bytes));
            }
            evidence["xbe_sha256"] = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B";
            evidence["status"] = "verified-static-eagl-parameter-and-indexed-draw-control";
            evidence["draw_opcode_binding"] = "verified-static-command26-conditional-indexed-submission";
            evidence["runtime_selector"] = "unresolved";
            evidence["vertex_attribute_binding"] = "not-proven";
            evidence["full_rendering_binding"] = "not-proven";
            evidence["original_semantics"] = "not-claimed";
            EaglRenderParameterAudit.WriteSummary(Path.Combine(directory.FullName,
                "eagl-render-parameter-static-control-summary.tsv"), evidence);
            Assert(ProjectReadinessAudit.ReadRenderParameterControl(directory.FullName).GatePassed,
                "Valid limited static evidence was rejected.");
            File.AppendAllText(Path.Combine(directory.FullName, "eagl-render-parameter-audit.tsv"), "changed");
            Assert(!ProjectReadinessAudit.ReadRenderParameterControl(directory.FullName).GatePassed,
                "Stale parameter report hash incorrectly passed readiness.");
        }
        finally
        {
            Directory.Delete(directory.FullName, recursive: true);
        }
    }

    private static void TestEaglIndexedDrawSourceAudit()
    {
        var data = new byte[512];
        void Word(int offset, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(offset, 4), value);
        Word(0x44, 1); Word(0x48, 0x100); Word(0x4c, 3); Word(0x50, 0x110);
        Word(0x100, 3); Word(0x110, 0xFFFF0000); Word(0x114, 7);
        var elf = new ElfInspection(true, true, 1, 3, 0, 40, 2, true,
            new[] { new ElfSection("", 0, 0, 0, 0, 0, 0, 0), new ElfSection(".data", 1, 3, 0, 512, 0, 0, 0) },
            Array.Empty<ElfSymbol>(), new[] { new ElfRelocation(".rel.data", 1, 0x48, 2, ""),
                new ElfRelocation(".rel.data", 1, 0x50, 2, "") });
        EaglRenderCommandEvidence Command(int i, ushort id, params uint[] payload)
        {
            var raw = new byte[(payload.Length + 1) * 4];
            BinaryPrimitives.WriteUInt32LittleEndian(raw, (uint)id << 16 | (uint)(payload.Length + 1));
            for (var n = 0; n < payload.Length; n++)
                BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan((n + 1) * 4), payload[n]);
            return new(i, (uint)(0x140 + i * 12), id, (ushort)(payload.Length + 1), raw, Array.Empty<string>());
        }
        var commands = new[] { Command(0, 2, 0), Command(1, 14, 0), Command(2, 26, 0, 5) };
        var primitive = new EaglModelPrimitive(0, 0, 0x40, 0x20, 0x44, null,
            Array.Empty<EaglStateAssignment>(), null, Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(), Array.Empty<EaglVertexFetchBinding>(), false,
            new EaglRenderCommandStream(commands, true, null)) { SourceSectionIndex = 1 };
        var obj = new EaglRawModelObject("fixture.big", "fixture.ord", data, elf, new[] { primitive });
        EaglIndexedDrawSourceRow Inspect(EaglRawModelObject source) => EaglIndexedDrawSourceAudit.Inspect(source).Single();
        var row = Inspect(obj);
        Assert(row.Status == EaglIndexedDrawSourceAudit.Verified && row.IndexCount == 3 && row.IndexBytes == 6 &&
               row.MinimumIndex == 0 && row.MaximumIndex == 65535 &&
               row.IndexSha256 == Convert.ToHexString(SHA256.HashData(data.AsSpan(0x110, 6))),
            "Descriptor-local count/pointer or unsigned little-endian indices were misread.");
        var missing = primitive with { PrimitiveIndex = 1, RenderDescriptorOffset = 0x80, GlobalParametersOffset = 0x84,
            RenderCommandStream = new EaglRenderCommandStream(new[] { Command(0, 26, 0, 5) }, true, null) };
        Assert(EaglIndexedDrawSourceAudit.Inspect(obj with { Primitives = new[] { primitive, missing } })[1].Status ==
               EaglIndexedDrawSourceAudit.Unresolved, "A neighboring descriptor inherited an unproven count/index source.");
        var dynamic = commands.ToArray(); dynamic[0] = Command(0, 2, 16);
        Assert(Inspect(obj with { Primitives = new[] { primitive with { RenderCommandStream =
            new EaglRenderCommandStream(dynamic, true, null) } } }).Status == EaglIndexedDrawSourceAudit.Unresolved,
            "Runtime selector was fixed to zero.");
        Word(0x100, uint.MaxValue);
        Assert(Inspect(obj).Status == EaglIndexedDrawSourceAudit.Rejected, "Oversized index count accepted.");
        Word(0x100, 3); Word(0x50, 0x1fe);
        Assert(Inspect(obj).Status == EaglIndexedDrawSourceAudit.Rejected, "Truncated index payload accepted.");
        Word(0x50, 0x111);
        Assert(Inspect(obj).Status == EaglIndexedDrawSourceAudit.Rejected, "Odd index address accepted.");
        Word(0x50, 0x110);
        Assert(Inspect(obj with { Elf = elf with { Relocations = elf.Relocations.Append(
            new ElfRelocation(".rel.data", 1, 0x112, 2, "external")).ToArray() } }).Status ==
            EaglIndexedDrawSourceAudit.Rejected, "Relocated data was decoded as raw indices.");
        Assert(Inspect(obj with { Elf = elf with { Relocations = new[] {
            new ElfRelocation(".rel.data", 1, 0x48, 2, "external"), elf.Relocations[1] } } }).Status ==
            EaglIndexedDrawSourceAudit.Unresolved, "Named external count was treated as a local pointer.");
        var directory = Directory.CreateTempSubdirectory("fifa-indexed-source-readiness-");
        try
        {
            Assert(!ProjectReadinessAudit.ReadIndexedDrawSourceControl(directory.FullName).GatePassed,
                "Missing indexed source evidence passed readiness.");
            var evidence = new Dictionary<string, string>
            {
                ["draw_rows"] = "2364", ["verified_local_sources"] = "2363", ["unresolved_sources"] = "1",
                ["rejected_sources"] = "0", ["independent_index_bytes"] = "1739618", ["static_spans"] = "5",
                ["static_sites"] = "37", ["negative_controls"] = "4",
                ["xbe_sha256"] = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B",
                ["status"] = "verified-independent-local-indexed-draw-source-control",
                ["source_binding"] = "descriptor-local-count-and-index-pointer-to-command26",
                ["vertex_attributes"] = "not-proven", ["runtime_selector"] = "unresolved",
                ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed",
            };
            foreach (var (file, field) in new[] {
                ("eagl-indexed-draw-source-audit.tsv", "draw_report_sha256"),
                ("eagl-render-parameter-audit.tsv", "parameter_report_sha256"),
                ("eagl-indexed-draw-source-control.tsv", "static_report_sha256") })
            {
                var path = Path.Combine(directory.FullName, file);
                File.WriteAllText(path, "bounded-fixture");
                evidence[field] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            }
            EaglRenderParameterAudit.WriteSummary(Path.Combine(directory.FullName,
                "eagl-indexed-draw-source-control-summary.tsv"), evidence);
            Assert(ProjectReadinessAudit.ReadIndexedDrawSourceControl(directory.FullName).GatePassed,
                "Valid limited indexed source control was rejected.");
            File.AppendAllText(Path.Combine(directory.FullName, "eagl-indexed-draw-source-audit.tsv"), "altered");
            Assert(!ProjectReadinessAudit.ReadIndexedDrawSourceControl(directory.FullName).GatePassed,
                "Altered indexed source report passed readiness.");
        }
        finally { Directory.Delete(directory.FullName, recursive: true); }
    }

    private static void TestEaglModelMaterialReferenceAudit()
    {
        var data = new byte[512];
        void Word(int at, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(at), v);
        Word(0x20, 0x180); Word(0x24, 1); Word(0x28, 0x80); Word(0x2c, 0x70); Word(0x34, 0x74); Word(0x48, uint.MaxValue);
        var tokens = new uint[] { 0x20000000, 0x40320000, 0x20000001, 0x40220001, 0x20000002, 0x40400002,
            0x20000003, 0x40320003, 0x20000004, 0x40150004, uint.MaxValue };
        for (var i = 0; i < tokens.Length; i++) Word(0x80 + 4 * i, tokens[i]);
        var vs = EaglMaterialDeclarationAudit.VertexProgram;
        const string ps = "FifastCharacterSkin0__EAGLMicroCodePS";
        const string parent = "ParentRM_FifastCharacterSkin";
        var elf = new ElfInspection(true, true, 1, 8, 0, 40, 2, true,
            new[] { new ElfSection("", 0, 0, 0, 0, 0, 0, 0), new ElfSection(".data", 1, 3, 0, 512, 0, 0, 0) },
            Array.Empty<ElfSymbol>(),
            new[] { new ElfRelocation(".rel.data", 1, 0x20, 2, ""), new ElfRelocation(".rel.data", 1, 0x28, 2, ""),
                new ElfRelocation(".rel.data", 1, 0x2c, 2, ""), new ElfRelocation(".rel.data", 1, 0x34, 2, ""),
                new ElfRelocation(".rel.data", 1, 0x70, 2, vs), new ElfRelocation(".rel.data", 1, 0x74, 2, ps),
                new ElfRelocation(".rel.data", 1, 0x3c, 2, parent), new ElfRelocation(".rel.data", 1, 0x4c, 2, parent) });
        var primitive = new EaglModelPrimitive(0, 0, 0x60, 0x20, 0x64, null, Array.Empty<EaglStateAssignment>(), null,
            Array.Empty<EaglVertexBufferStream>(), Array.Empty<EaglVertexFetchSlot>(), Array.Empty<EaglVertexFetchBinding>(), false,
            new EaglRenderCommandStream(new[] { new EaglRenderCommandEvidence(0, 0x180, 4, 2, new byte[8], Array.Empty<string>()) }, true, null))
            { SourceSectionIndex = 1 };
        var obj = new EaglRawModelObject("fixture.big", "fixture.ord", data, elf, new[] { primitive });
        var material = new EaglMaterialDeclaration(EaglMaterialDeclarationAudit.Decode(data.AsSpan(0x80, 44)), 1, 0x1458, 0x1400,
            EaglModelMaterialReferenceAudit.DeclarationHash, 0x1490, 0x2078, 46, 740, EaglVertexProgramAudit.ProgramSha256,
            EaglMaterialDeclarationAudit.SourceSha256);
        var defs = new Dictionary<string, EaglMaterialSourceDefinition> { [vs] = new("Skin", parent, vs, ps, material) };
        var row = EaglModelMaterialReferenceAudit.InspectPrimitive(obj, primitive, defs);
        Assert(row["status"] == EaglModelMaterialReferenceAudit.Status && row["parent_1c_reference"] == parent &&
            row["parent_2c_reference"] == parent && row["declaration_sha256"] == EaglModelMaterialReferenceAudit.DeclarationHash &&
            row["runtime_loader_resolution"] == "not-proven" && row["selected_buffers"] == "not-proven",
            "Explicit model references were lost or promoted to runtime semantics.");
        void Reject(EaglRawModelObject source, EaglModelPrimitive p)
        {
            var rejected = false;
            try { EaglModelMaterialReferenceAudit.InspectPrimitive(source, p, defs); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Invalid model material reference was accepted.");
        }
        foreach (var at in new[] { 0x20, 0x24, 0x28, 0x30, 0x38, 0x44, 0x48, 0x4c, 0x70, 0x80 })
        {
            var changed = (byte[])data.Clone(); changed[at] ^= 1;
            Reject(obj with { Data = changed }, primitive);
        }
        foreach (var index in new[] { 0, 1, 4, 6, 7 })
        {
            var refs = elf.Relocations.ToArray(); refs[index] = refs[index] with { SymbolName = "wrong-or-external" };
            Reject(obj with { Elf = elf with { Relocations = refs } }, primitive);
        }
        Reject(obj with { Elf = elf with { Relocations = elf.Relocations.Append(elf.Relocations[4]).ToArray() } }, primitive);
        Reject(obj with { Elf = elf with { Relocations = elf.Relocations.Append(new ElfRelocation(".rel.data", 1, 0x84, 2, "")).ToArray() } }, primitive);
        Reject(obj, primitive with { RenderMethodOffset = uint.MaxValue - 3 });
        Reject(obj, primitive with { SourceSectionIndex = 0 });
        Reject(obj, primitive with { RenderCommandStream = primitive.RenderCommandStream with { IsComplete = false } });
        Reject(obj, primitive with { RenderCommandStream = primitive.RenderCommandStream with { Commands = Array.Empty<EaglRenderCommandEvidence>() } });
    }

    private static void TestEaglModelMaterialReferenceReadiness()
    {
        var directory = Directory.CreateTempSubdirectory("fifa-material-reference-readiness-");
        try
        {
            Assert(!ProjectReadinessAudit.ReadModelMaterialReferenceControl(directory.FullName).GatePassed, "Missing model material evidence passed.");
            var fields = new Dictionary<string, string> {
                ["model_rows"] = "2364", ["model_objects"] = "883", ["material_definitions"] = "3", ["declarations_matched"] = "2364",
                ["parent_1c_2c_same_reference_rows"] = "2364", ["skin_rows"] = "1416", ["cloth_rows"] = "594", ["eye_rows"] = "354",
                ["static_spans"] = "4", ["static_sites"] = "53", ["negative_controls"] = "16",
                ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
                ["declaration_sha256"] = EaglModelMaterialReferenceAudit.DeclarationHash,
                ["xbe_sha256"] = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B",
                ["status"] = "verified-independent-model-material-reference-control", ["reference_scope"] = "serialized-reference-to-exact-named-program-definition",
                ["runtime_loader_resolution"] = "not-proven", ["selected_buffers"] = "not-proven", ["full_rendering_binding"] = "not-proven",
                ["original_semantics"] = "not-claimed" };
            foreach (var (file, field) in new[] { ("eagl-model-material-reference-audit.tsv", "report_sha256"),
                ("eagl-model-material-reference-control.tsv", "static_report_sha256") })
            {
                var path = Path.Combine(directory.FullName, file); File.WriteAllText(path, "synthetic evidence");
                fields[field] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            }
            var pathSummary = Path.Combine(directory.FullName, "eagl-model-material-reference-control-summary.tsv");
            EaglRenderParameterAudit.WriteSummary(pathSummary, fields);
            Assert(ProjectReadinessAudit.ReadModelMaterialReferenceControl(directory.FullName).GatePassed, "Complete model reference control rejected.");
            foreach (var key in new[] { "skin_rows", "declaration_sha256", "reference_scope", "runtime_loader_resolution", "selected_buffers", "full_rendering_binding", "original_semantics" })
            {
                var original = fields[key]; fields[key] = "changed-or-promoted"; EaglRenderParameterAudit.WriteSummary(pathSummary, fields);
                Assert(!ProjectReadinessAudit.ReadModelMaterialReferenceControl(directory.FullName).GatePassed, "Changed/promoted model references passed.");
                fields[key] = original;
            }
            EaglRenderParameterAudit.WriteSummary(pathSummary, fields);
            File.AppendAllText(Path.Combine(directory.FullName, "eagl-model-material-reference-audit.tsv"), "stale");
            Assert(!ProjectReadinessAudit.ReadModelMaterialReferenceControl(directory.FullName).GatePassed, "Stale model reference hash passed.");
        }
        finally { directory.Delete(recursive: true); }
    }

    private static void TestEaglVertexProgramAudit()
    {
        byte[] Program(params uint[] words)
        {
            var bytes = new byte[4 + 4 * words.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(bytes, 0x2078);
            BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(2), (ushort)(words.Length / 4));
            for (var n = 0; n < words.Length; n++) BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4 + n * 4), words[n]);
            return bytes;
        }
        void Reject(byte[] bytes)
        {
            var rejected = false;
            try { EaglVertexProgramAudit.Decode(bytes); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Invalid static vertex program was accepted.");
        }
        var payload = Program(0, 0x01A00800, 0x0836106C, 0x20700FF8,
            0, 0x02318255, 0x0C36106C, 0x2120F84C,
            0, 0x0087601B, 0xC400286C, 0x3070E801);
        var parsed = EaglVertexProgramAudit.Decode(payload);
        Assert(parsed.Count == 3 && parsed[0].Mac == 13 && parsed[0].Input == 4 && parsed[0].MacSources == 1 &&
            parsed[0].ActiveSources.Count() == 1, "ARL counted unused B/C input muxes.");
        Assert(parsed[1].Mac == 1 && parsed[1].Ilu == 1 && parsed[1].Input == 1 &&
            parsed[1].A.Mux == 3 && parsed[1].A.Swizzle == "yyyy" && parsed[1].C.Mux == 2 &&
            parsed[1].C.Swizzle == "xyzw" && parsed[1].OutputScalar && parsed[1].OutputAddress == 9 &&
            parsed[1].OutputBank == 1 && parsed[1].OutputMask == 15,
            "Paired instructions/input/output DWORD fields were decoded incorrectly.");
        Assert(parsed[2].A.Temporary == 12 && parsed[2].B.Temporary == 1 && parsed[2].C.Mux == 3 &&
            parsed[2].Constant == 59 && parsed[2].Final, "Split-word source register or constant/final fields were misread.");
        var add = EaglVertexProgramAudit.Decode(Program(0, 0x0071A01B, 0xA436106C, 0x3070F819))[0];
        Assert(add.MacSources == 5 && add.ActiveSources.Count() == 2 &&
            EaglVertexProgramAudit.Row(add, "test")["b_operand"] == "inactive", "ADD did not use A/C only.");
        Assert(EaglVertexProgramAudit.Decode(Program(0, 0x00200000, 0x04000000, 1))[0].ActiveSources.Count() == 1,
            "Inactive operand fields were validated as reads.");
        foreach (var change in new (int At, byte Value)[] { (0, 0), (2, 0), (4, 1), (11, 255), (16, 249), (48, 0) })
        {
            var altered = (byte[])payload.Clone(); altered[change.At] = change.Value; Reject(altered);
        }
        Reject(payload[..^1]); Reject(payload.Concat(new byte[] { 0 }).ToArray());
        Reject(Program(0, 0x00200000, 0, 1));
        Reject(Program(0, 0x00200000, 0xD4000000, 1));
        Reject(Program(0, 0x003FE000, 0x0C000000, 1));
        Reject(Program(0, 0, 0, 0x01000001));
        Reject(Program(0, 0x00200000, 0x04000000, 0x01D00001));
        Reject(Program(0, 0, 0, 0x0000F801));
        Reject(Program(0, 0x00200000, 0x04000000, 0x0000F809));
        Reject(Program(0, 0x01C00000, 0x04000000, 1));
        Reject(Program(new uint[137 * 4]));
        // Another representation exercises all 256 swizzles and C's split register.
        for (var swizzle = 0; swizzle < 256; swizzle++)
        {
            var raw = (uint)(1 | (11 << 2) | (swizzle << 6) | ((swizzle & 1) << 14));
            UInt128 packed = ((UInt128)raw << 28) | ((UInt128)1 << 89) | 1;
            var item = EaglVertexProgramAudit.Decode(Program(0, (uint)(packed >> 64), (uint)(packed >> 32), (uint)packed))[0];
            var wanted = string.Concat(new[] { 6, 4, 2, 0 }.Select(b => "xyzw"[(swizzle >> b) & 3]));
            Assert(item.C.Raw == raw && item.C.Temporary == 11 && item.C.Swizzle == wanted &&
                item.C.Negate == ((swizzle & 1) != 0), "Split C operand/swizzle/negate failed independent packing.");
        }
    }

    private static void TestEaglVertexProgramReadiness()
    {
        var directory = Directory.CreateTempSubdirectory("fifa-vs-readiness-");
        try
        {
            Assert(!ProjectReadinessAudit.ReadVertexProgramControl(directory.FullName).GatePassed, "Missing VS evidence passed.");
            var fields = new Dictionary<string, string> {
                ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
                ["vertex_program_sha256"] = EaglVertexProgramAudit.ProgramSha256,
                ["xbe_sha256"] = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B",
                ["instruction_count"] = "46", ["active_input_slot_references"] = "8", ["relative_constant_instructions"] = "6",
                ["output_write_instructions"] = "9", ["paired_instructions"] = "6", ["static_spans"] = "5",
                ["static_sites"] = "65", ["negative_controls"] = "18", ["active_input_registers"] = "0,1,3,4",
                ["active_output_addresses"] = "0,3,4,9", ["status"] = "verified-independent-vertex-program-opcode-operands-control",
                ["operand_scope"] = "opcode-active-source-slots-not-component-liveness", ["parent_inheritance_binding"] = "not-proven",
                ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
            foreach (var (file, field) in new[] { ("eagl-vertex-program-audit.tsv", "report_sha256"),
                ("eagl-vertex-program-audit-summary.tsv", "managed_summary_sha256"), ("eagl-vertex-program-control.tsv", "static_report_sha256") })
            {
                var path = Path.Combine(directory.FullName, file);
                File.WriteAllText(path, "synthetic test evidence");
                fields[field] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            }
            var summary = Path.Combine(directory.FullName, "eagl-vertex-program-control-summary.tsv");
            EaglRenderParameterAudit.WriteSummary(summary, fields);
            Assert(ProjectReadinessAudit.ReadVertexProgramControl(directory.FullName).GatePassed, "Complete bounded VS control rejected.");
            foreach (var key in new[] { "active_input_registers", "active_output_addresses", "instruction_count", "vertex_program_sha256",
                "operand_scope", "parent_inheritance_binding", "full_rendering_binding", "original_semantics" })
            {
                var original = fields[key]; fields[key] = "changed-or-promoted";
                EaglRenderParameterAudit.WriteSummary(summary, fields);
                Assert(!ProjectReadinessAudit.ReadVertexProgramControl(directory.FullName).GatePassed, "Changed/promoted VS evidence passed.");
                fields[key] = original;
            }
            EaglRenderParameterAudit.WriteSummary(summary, fields);
            File.AppendAllText(Path.Combine(directory.FullName, "eagl-vertex-program-audit.tsv"), "stale");
            Assert(!ProjectReadinessAudit.ReadVertexProgramControl(directory.FullName).GatePassed, "Stale VS hash passed.");
        }
        finally { directory.Delete(recursive: true); }
    }

    private static void TestEaglMaterialDeclarationAudit()
    {
        byte[] Tokens(params uint[] words)
        {
            var bytes = new byte[words.Length * 4];
            for (var i = 0; i < words.Length; i++)
                BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(i * 4), words[i]);
            return bytes;
        }
        void Reject(Action action)
        {
            var rejected = false;
            try { action(); } catch (InvalidDataException) { rejected = true; }
            Assert(rejected, "Invalid material declaration/envelope was accepted.");
        }
        var source = Tokens(0x20000000, 0x40320000, 0x20000001, 0x40220001,
            0x20000002, 0x40400002, 0x20000003, 0x40320003, 0x20000004, 0x40150004, uint.MaxValue);
        var layout = EaglMaterialDeclarationAudit.Decode(source);
        Assert(layout.ConsumedBytes == 44 && layout.Elements.Count == 5 &&
            layout.Elements.Select(e => e.Stream).SequenceEqual(new[] { 0, 1, 2, 3, 4 }) &&
            layout.Elements.Select(e => e.Register).SequenceEqual(new[] { 0, 1, 2, 3, 4 }) &&
            layout.Elements.Select(e => e.ElementBytes).SequenceEqual(new[] { 12, 8, 4, 12, 2 }) &&
            layout.Elements.All(e => e.ByteOffset == 0), "Observed declaration bit fields/widths were misread.");
        var interleaved = EaglMaterialDeclarationAudit.Decode(Tokens(0x20000002, 0x40320000, 0x40220001, uint.MaxValue));
        Assert(interleaved.Elements[1].ByteOffset == 12 && interleaved.Elements[1].Stream == 2,
            "Register offset did not accumulate within one stream.");
        Reject(() => EaglMaterialDeclarationAudit.Decode(source.AsSpan(0, 40)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x40320000, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x20000000, 0x40320000, 0x40220000, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x20000000, 0x40320010, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x20000000, 0x40160000, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x20000010, 0x40320000, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x20000000, 0x50010000, uint.MaxValue)));
        Reject(() => EaglMaterialDeclarationAudit.Decode(Tokens(0x80000000, uint.MaxValue)));
        var data = new byte[1024];
        void Word(int at, uint v) => BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(at), v);
        Word(0x24, 1); Word(0x28, 0x80); Word(0x2c, 0x70); Word(0x100, 0x002E2078);
        source.CopyTo(data, 0x80);
        var elf = new ElfInspection(true, true, 1, 8, 0, 40, 2, true,
            new[] { new ElfSection("", 0, 0, 0, 0, 0, 0, 0), new ElfSection(".data", 1, 3, 0, 1024, 0, 0, 0) },
            new[] { new ElfSymbol(2, 1, EaglMaterialDeclarationAudit.Parent, 0x20, 4, 0, 1),
                new ElfSymbol(2, 2, EaglMaterialDeclarationAudit.VertexProgram, 0x100, 4, 0, 1) },
            new[] { new ElfRelocation(".rel.data", 1, 0x28, 2, ""), new ElfRelocation(".rel.data", 1, 0x2c, 2, ""),
                new ElfRelocation(".rel.data", 1, 0x70, 2, EaglMaterialDeclarationAudit.VertexProgram) });
        var parsed = EaglMaterialDeclarationAudit.Inspect(data, elf);
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Machine = 3 }));
        Assert(parsed.VertexProgramBytes == 740 && parsed.VertexInstructionCount == 46 &&
            parsed.VertexProgramSha256 == Convert.ToHexString(SHA256.HashData(data.AsSpan(0x100, 740))),
            "Vertex envelope incorrectly used the symbol's declared four-byte size.");
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Symbols = elf.Symbols.Append(elf.Symbols[0]).ToArray() }));
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Relocations = elf.Relocations.Append(
            new ElfRelocation(".rel.data", 1, 0x80, 2, "unresolved")).ToArray() }));
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Relocations = elf.Relocations.Append(
            new ElfRelocation(".rel.data", 1, 0x104, 2, "unresolved")).ToArray() }));
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Relocations = elf.Relocations.Select(r =>
            r.Offset == 0x28 ? r with { SymbolName = "external" } : r).ToArray() }));
        Word(0x100, 0x002EFFFF); Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf));
        Word(0x100, 0x01012078); Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf));
        Word(0x100, 0x00402078); Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf));
        Word(0x100, 0x002E2078); Word(0x28, 0x81); Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf));
        Word(0x28, 0x80); Word(0x70, 1); Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf));
        Word(0x70, 0);
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Symbols = new[] {
            elf.Symbols[0] with { Value = 0xFFFFFFFC }, elf.Symbols[1] } }));
        Reject(() => EaglMaterialDeclarationAudit.Inspect(data, elf with { Relocations = elf.Relocations.Append(
            new ElfRelocation(".rel.data", 1, 0x24, 2, "external")).ToArray() }));
        var directory = Directory.CreateTempSubdirectory("fifa-material-layout-readiness-");
        try
        {
            Assert(!ProjectReadinessAudit.ReadMaterialDeclarationControl(directory.FullName).GatePassed,
                "Missing material layout control passed readiness.");
            var evidence = new Dictionary<string, string> {
                ["source_path"] = EaglMaterialDeclarationAudit.Source, ["source_sha256"] = EaglMaterialDeclarationAudit.SourceSha256,
                ["xbe_sha256"] = "5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B",
                ["declaration_elements"] = "5", ["declaration_bytes"] = "44", ["vertex_instruction_count"] = "46",
                ["vertex_program_bytes"] = "740", ["static_spans"] = "4", ["static_sites"] = "51", ["negative_controls"] = "7",
                ["size_tables_address"] = "0x0029C760",
                ["size_tables_sha256"] = "2F6CF9E3BD99EB8EEB79080DD0867A4E0A68D14B7A3220A07BA704F8882FBB3D",
                ["status"] = "verified-independent-material-declaration-and-vs-envelope-control", ["attribute_semantics"] = "not-proven",
                ["parent_inheritance_binding"] = "not-proven", ["full_rendering_binding"] = "not-proven", ["original_semantics"] = "not-claimed" };
            foreach (var (file, field) in new[] {
                ("eagl-material-declaration-audit.tsv", "declaration_report_sha256"),
                ("eagl-material-declaration-control.tsv", "static_report_sha256") })
            {
                var path = Path.Combine(directory.FullName, file);
                File.WriteAllText(path, "bounded-fixture");
                evidence[field] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            }
            var summary = Path.Combine(directory.FullName, "eagl-material-declaration-control-summary.tsv");
            EaglRenderParameterAudit.WriteSummary(summary, evidence);
            Assert(ProjectReadinessAudit.ReadMaterialDeclarationControl(directory.FullName).GatePassed,
                "Valid limited material layout evidence was rejected.");
            evidence["attribute_semantics"] = "position-uv-skinning";
            EaglRenderParameterAudit.WriteSummary(summary, evidence);
            Assert(!ProjectReadinessAudit.ReadMaterialDeclarationControl(directory.FullName).GatePassed,
                "Unproven attribute semantics were promoted by readiness.");
            evidence["attribute_semantics"] = "not-proven";
            EaglRenderParameterAudit.WriteSummary(summary, evidence);
            File.AppendAllText(Path.Combine(directory.FullName, "eagl-material-declaration-audit.tsv"), "altered");
            Assert(!ProjectReadinessAudit.ReadMaterialDeclarationControl(directory.FullName).GatePassed,
                "Stale material layout report passed readiness.");
        }
        finally { Directory.Delete(directory.FullName, recursive: true); }
    }

    private static void TestEaglOriginalVertexRecordAssociationAudit()
    {
        Assert(EaglOriginalVertexRecordAssociationAudit.TryParseOpaqueVertexRecordKey(
                   "__VB:::__GPvb_10_3_fixture",
                   out var category,
                   out var ordinal) &&
               category == 10 && ordinal == 3,
            "Opaque original __VB category/ordinal key was not parsed.");
        Assert(!EaglOriginalVertexRecordAssociationAudit.TryParseOpaqueVertexRecordKey(
                   "__VB:::malformed",
                   out _,
                   out _),
            "Malformed opaque original __VB key was accepted.");

        var slots = Enumerable.Range(0, 5)
            .Select(index => new EaglVertexFetchSlot((uint)index, (uint)(0x100 + index), (uint)(8 + index * 4)))
            .ToArray();
        var streams = slots
            .Select((slot, index) => new EaglVertexBufferStream(
                $"__VB:::__GPvb_{10 + index}_3_fixture",
                1,
                (uint)(0x20 + index * 0x20),
                (uint)(0x100 + index * 8),
                (uint)(0x200 + index * 0x40),
                checked(slot.Stride * 3),
                slot.Stride,
                0x10))
            .ToArray();
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            null,
            Array.Empty<EaglStateAssignment>(),
            null,
            Array.Empty<EaglVertexBufferStream>(),
            slots,
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(
                        0,
                        0,
                        9,
                        4,
                        new byte[16],
                        Array.Empty<string>()),
                },
                true,
                null));
        var row = EaglOriginalVertexRecordAssociationAudit.InspectPrimitive(
            "fixture.big",
            "fixture.ord",
            primitive,
            streams);
        Assert(row.SameOrdinalRecordCount == 5 &&
               row.UniqueOpaqueSequenceCount == 1 &&
               row.MatchedRecordCount == 5 &&
               row.CompleteFiveSlotFetchSequence &&
               row.UniqueOpaqueSequence &&
               row.Status == EaglOriginalVertexRecordAssociationAudit.Status &&
               row.OriginalSemantics == "not-claimed",
            "Original opaque __VB record association fixture was not recognized.");
    }

    private static void TestEaglOriginalVertexPayloadAudit()
    {
        var data = new byte[128];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 48);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(32, 4), 80);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(48, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(52, 4), 80);
        var values = new[] { 1f, 2f, 3f, -4f, 5f, 6f };
        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                data.AsSpan(80 + index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(values[index]));
        }

        var section = new ElfSection(".data", 1, 0, 0, checked((uint)data.Length), 0, 0, 0);
        var elf = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[] { new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0), section },
            new[] { new ElfSymbol(0, 0, "__VB:::payload_fixture", 16, 20, 0, 1) },
            Array.Empty<ElfRelocation>());
        var stream = EaglVertexBufferReader.Inspect(data, elf).Single();
        var inspection = new EaglObjectInspection(
            elf,
            new[] { "__VB:::payload_fixture" },
            new EaglAxisAlignedBounds(
                new EaglVector3(-5, 1, 2),
                new EaglVector3(2, 6, 7)),
            "__BBOX:::payload_fixture",
            null);
        var row = EaglOriginalVertexPayloadAudit.InspectObject(
            "fixture.big",
            "fixture.ord",
            data,
            inspection,
            new[] { stream });
        Assert(row.ValidatedStreamCount == 1 &&
               row.FinitePackedFloatProfileCount == 1 &&
               row.FiniteFloat3ProfileCount == 1 &&
               row.DeclaredBoundsContainmentFloat3Count == 1 &&
               row.DistinctPayloadRangeCount == 1 &&
               row.OverlapPayloadPairCount == 0 &&
               row.Status == EaglOriginalVertexPayloadAudit.Status &&
               row.OriginalSemantics == "not-claimed",
               "Original numeric __VB payload fixture was not measured structurally.");
    }

    private static void TestEaglOriginalPrimitiveStreamTopologyAudit()
    {
        var slots = Enumerable.Range(0, 5)
            .Select(index => new EaglVertexFetchSlot((uint)index, (uint)(0x100 + index), (uint)(12 - index * 2)))
            .ToArray();
        var streams = slots
            .Select((slot, index) => new EaglVertexBufferStream(
                $"__VB:::__GPvb_{10 + index}_3_fixture",
                1,
                (uint)(0x20 + index * 0x20),
                (uint)(0x100 + index * 8),
                (uint)(0x200 + index * 0x40),
                checked(slot.Stride * 3),
                slot.Stride,
                0x10))
            .ToArray();
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            null,
            Array.Empty<EaglStateAssignment>(),
            null,
            Array.Empty<EaglVertexBufferStream>(),
            slots,
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(
                        0,
                        0,
                        9,
                        4,
                        new byte[16],
                        Array.Empty<string>()),
                },
                true,
                null));
        var keyed = streams
            .Select((stream, index) => new EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream(
                stream,
                10 + index,
                3u))
            .ToArray();
        var rows = EaglOriginalPrimitiveStreamTopologyAudit.InspectPrimitive(
            "fixture.big",
            "fixture.ord",
            primitive,
            keyed);
        Assert(rows.Count == 5 &&
               rows.All(row => row.StructuralMatch &&
                               row.KeyOrdinalMatchesPrimitive &&
                               row.FetchStrideMatchesStreamStride &&
                               row.ValidPayloadRange &&
                               row.OriginalSemantics == "not-claimed") &&
               EaglOriginalPrimitiveStreamTopologyAudit.MatchesFetchStrideSequence(primitive, keyed),
               "Original primitive/__VB stream topology fixture was not recognized structurally.");
    }

    private static void TestEaglOriginalPrimitiveStreamOpaqueDiscriminationAudit()
    {
        var raw = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(4, 4), 0x11223344);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(8, 4), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(12, 4), 8);
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            null,
            Array.Empty<EaglStateAssignment>(),
            null,
            Array.Empty<EaglVertexBufferStream>(),
            new[] { new EaglVertexFetchSlot(2, 0x11223344, 8) },
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 4, raw, Array.Empty<string>()),
                },
                true,
                null));
        var words = EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.ReadFetchWords(primitive);
        Assert(words.Count == 1 &&
               words[0].AuxiliaryValue == 0x11223344 &&
               words[0].Slot == 2 &&
               words[0].Stride == 8 &&
               EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.ContainsAlignedWord(raw, 0x11223344) &&
               !EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.ContainsAlignedWord(raw, 0x55667788),
            "Original primitive/__VB opaque discrimination fixture did not preserve raw fetch-word controls.");
    }

    private static void TestEaglOriginalPrimitiveStreamCommandPayloadIdentityAudit()
    {
        var raw = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(raw.AsSpan(8, 4), 2);
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            null,
            Array.Empty<EaglStateAssignment>(),
            null,
            Array.Empty<EaglVertexBufferStream>(),
            new[] { new EaglVertexFetchSlot(2, 0, 8) },
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 4, raw, Array.Empty<string>()),
                },
                true,
                null));
        var commands = EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.ReadFetchCommands(primitive);
        var larger = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE };
        Assert(commands.Count == 1 &&
               commands[0].Slot == 2 &&
               commands[0].RawBytes.SequenceEqual(raw) &&
               EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.ContainsBytes(larger, new byte[] { 0xBB, 0xCC }) &&
               !EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.ContainsBytes(larger, new byte[] { 0xBB, 0xCC, 0xEF }) &&
               EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.ContainsBytes(larger, ReadOnlySpan<byte>.Empty),
            "Original primitive command/payload identity fixture did not preserve exact raw command controls.");
    }

    private static void TestEaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit()
    {
        var slots = Enumerable.Range(0, 5)
            .Select(index => new EaglVertexFetchSlot((uint)index, 0, (uint)(4 + index)))
            .ToArray();
        var streams = slots
            .Select((slot, index) => new EaglVertexBufferStream(
                $"__VB:::__GPvb_{10 + index}_3_fixture",
                2,
                (uint)(0x80 + index * 0x20),
                (uint)(0x180 + index * 0x20),
                (uint)(0x280 + index * 0x20),
                checked(slot.Stride * 3),
                slot.Stride,
                1))
            .ToArray();
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            null,
            Array.Empty<EaglStateAssignment>(),
            null,
            Array.Empty<EaglVertexBufferStream>(),
            slots,
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(
                        0,
                        0x20C,
                        4,
                        2,
                        new byte[8],
                        Array.Empty<string>()),
                },
                true,
                null))
        {
            SourceSectionIndex = 2,
        };
        var keyed = streams
            .Select((stream, index) => new EaglOriginalPrimitiveStreamTopologyAudit.KeyedStream(
                stream,
                10 + index,
                3u))
            .ToArray();
        var exact = EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.FindExactAdjacencyStreams(
            primitive,
            0x20C,
            keyed);
        Assert(
            exact.Count == 1 &&
            exact[0].Category == 14 &&
            exact[0].Stream.Stride == 8 &&
            EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.HasExactAdjacency(
                primitive,
                0x20C,
                keyed) &&
            !EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.HasExactAdjacency(
                primitive,
                0x20D,
                keyed),
            "Original command/descriptor adjacency fixture did not preserve exact same-section offset identity.");
    }

    private static void TestEaglOriginalStateTokenAudit()
    {
        static EaglOriginalRenderChainRow Row(
            uint primitive,
            string assignments,
            bool hasShapeName,
            bool hasPrimitiveType) =>
            new(
                "fixture.big",
                "fixture.ord",
                0,
                primitive,
                0x10,
                0x20,
                0x30,
                0x40,
                1,
                4,
                true,
                true,
                "__EAGL::TAR:::fixture",
                assignments.Split(';', StringSplitOptions.RemoveEmptyEntries).Length,
                assignments,
                hasShapeName,
                hasPrimitiveType,
                5,
                "0,1,2,3,4",
                true,
                hasShapeName,
                false,
                "fixture-command-hash",
                EaglOriginalRenderChainAudit.Status,
                EaglOriginalRenderChainAudit.OriginalSemantics,
                string.Empty);

        var result = EaglOriginalStateTokenAudit.Analyze(
            "fixture.big",
            new[]
            {
                Row(
                    0,
                    "RUNTIME_ALLOC::UID=1;SHAPENAME=fixture,1;XBOXEXTOBJ_SetClampU=EAGL::XBOXCM_CLAMP;XBOXEXTOBJ_SetClampV=EAGL::XBOXCM_CLAMP",
                    true,
                    false),
                Row(
                    1,
                    "RUNTIME_ALLOC::UID=1;SetPrimitiveType=EAGL::PT_TRIANGLESTRIP",
                    false,
                    true),
            });
        Assert(result.Summary.InputRows == 2 &&
               result.Summary.StateRows == 2 &&
               result.Summary.ValidInputRows == 2 &&
               result.Summary.ShapeNameRows == 1 &&
               result.Summary.ClampURows == 1 &&
               result.Summary.ClampVRows == 1 &&
               result.Summary.PrimitiveTypeRows == 1 &&
               result.Summary.TextureTokenRows == 0 &&
               result.Summary.MaterialTokenRows == 0 &&
               result.Summary.UvCoordinateTokenRows == 0 &&
               result.Summary.VertexAttributeTokenRows == 0 &&
               result.Summary.DeformationTokenRows == 0 &&
               result.Summary.Status == EaglOriginalStateTokenAudit.Status &&
               result.Summary.OriginalSemantics == "not-claimed",
            "Original state-token negative-control fixture was not classified structurally.");

        var tokenResult = EaglOriginalStateTokenAudit.Analyze(
            "fixture.big",
            new[]
            {
                Row(
                    2,
                    "SetTexture=fixture",
                    false,
                    false),
            });
        Assert(tokenResult.Summary.TextureTokenRows == 1 &&
               tokenResult.Summary.Status == "inconclusive-original-state-token-control" &&
               tokenResult.Rows.Single().Issue.Contains("semantic review", StringComparison.Ordinal),
            "Original state-token semantic-control fixture did not reject a literal token.");
    }

    private static void TestEaglCandidateFetchAssociationAudit()
    {
        var slots = Enumerable.Range(0, 5)
            .Select(index => new EaglVertexFetchSlot((uint)index, (uint)(0x100 + index), (uint)(8 + index * 4)))
            .ToArray();
        var streams = slots
            .Select((slot, index) => new EaglVertexBufferStream(
                $"__VB_fixture_{index}",
                1,
                (uint)(0x20 + index * 0x20),
                (uint)(0x100 + index * 8),
                (uint)(0x200 + index * 0x40),
                (uint)(slot.Stride * 3),
                slot.Stride,
                0x10))
            .ToArray();
        var bindings = slots
            .Select((slot, index) => new EaglVertexFetchBinding(slot, streams[index]))
            .ToArray();
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0x30,
            0x40,
            0x50,
            "__EAGL::TAR:::SHAPENAME=fixture,1",
            new[] { new EaglStateAssignment("SHAPENAME", "fixture,1") },
            "PT_TRIANGLESTRIP",
            new[] { streams[0] },
            slots,
            bindings,
            true,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(
                        0,
                        0,
                        9,
                        4,
                        new byte[16],
                        Array.Empty<string>()),
                },
                true,
                null));
        Assert(EaglCandidateFetchAssociationAudit.IsCompleteFiveSlotLayout(primitive),
            "Candidate fetch association fixture was not recognized as a complete five-slot layout.");

        var texture = new PcShapeImageTexture(
            "fixture",
            0x7B,
            1,
            1,
            "PAL8",
            new byte[] { 1, 2, 3, 255 });
        var match = new EaglShapeMaterialAsset(
            "data/models.big",
            "fixture.big",
            "fixture.ord",
            "fixture",
            "fixture.xsh",
            new ShapeImageEntry("fixture", 0, 1),
            texture);
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    3,
                    streams[0].SymbolName,
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);
        var candidate = new EaglCandidateAsset(
            mesh,
            new EaglAxisAlignedBounds(
                new EaglVector3(0, 0, 0),
                new EaglVector3(1, 1, 1)),
            "fixture.ord",
            new[] { primitive });
        var rows = EaglCandidateFetchAssociationAudit.AuditCandidate(match, candidate);
        Assert(rows.Count == 1 &&
               rows[0].CompleteFiveSlotLayout &&
               rows[0].FetchSlotCount == 5 &&
               rows[0].FetchBindingCount == 5 &&
               EaglCandidateFetchAssociationAudit.CountSlotRecords(rows[0].SlotRecords) == 5 &&
               EaglCandidateFetchAssociationAudit.CountValidSlotRecords(rows[0].SlotRecords) == 5 &&
               rows[0].Status == EaglCandidateFetchAssociationAudit.Status &&
               rows[0].OriginalSemantics == "not-claimed",
            "Candidate fetch association report did not preserve opaque slot/record evidence.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-candidate-fetch-association-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            EaglCandidateFetchAssociationAudit.WriteReport(reportPath, rows);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   reportLines[1].Contains("\t5\t5\tTrue\t1\t", StringComparison.Ordinal) &&
                   reportLines[0].StartsWith("model_archive\t", StringComparison.Ordinal),
                "Candidate fetch association TSV header/row mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglOriginalCandidateSlotProfileAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-original-candidate-slot-profile-");
        try
        {
            var originalPath = Path.Combine(temporaryDirectory.FullName, "original.tsv");
            var candidatePath = Path.Combine(temporaryDirectory.FullName, "candidate.tsv");
            File.WriteAllLines(
                originalPath,
                new[]
                {
                    "model_archive\tmodel_object\tsymbol_name\tcategory_key\tstream_stride\tbyte_length\telement_count\tfinite_float2_profile\tnear_unit_range_float2_profile",
                    "fixture.big\tfixture.ord\t__VB:::__GPvb_11_0_fixture\t11\t8\t24\t3\tTrue\tTrue",
                });
            File.WriteAllLines(
                candidatePath,
                new[]
                {
                    "model_archive\tmodel_object\tlayer\tprimitive\tcomplete_five_slot_layout\tslot_records",
                    "fixture.big\tfixture.ord\t0\t0\tTrue\tslot=1;aux=0x18;command_stride=8;stream_stride=8;stream_bytes=24;elements=3;stream=__VB:::__GPvb_11_0_fixture;valid=True",
                    "fixture.big\tfixture.ord\t0\t1\tFalse\t",
                });

            var audit = EaglOriginalCandidateSlotProfileAudit.Audit(originalPath, candidatePath);
            Assert(audit.Summary.Status == EaglOriginalCandidateSlotProfileAudit.Status &&
                   audit.Summary.OriginalStreamRows == 1 &&
                   audit.Summary.CandidatePrimitiveRows == 2 &&
                   audit.Summary.CandidateCompleteFiveSlotRows == 1 &&
                   audit.Summary.CandidateSlot1Rows == 1 &&
                   audit.Summary.SymbolMatchRows == 1 &&
                   audit.Summary.ObjectMatchRows == 1 &&
                   audit.Summary.Category11Rows == 1 &&
                   audit.Summary.FiniteFloat2Rows == 1 &&
                   audit.Summary.NearUnitRangeFloat2Rows == 1 &&
                   audit.Summary.StreamStrideMatchRows == 1 &&
                   audit.Summary.ByteLengthMatchRows == 1 &&
                   audit.Summary.ElementCountMatchRows == 1 &&
                   audit.Summary.CompleteControlRows == 1 &&
                   audit.Summary.OriginalSemantics == "not-claimed" &&
                   audit.Rows.Count == 2 &&
                   audit.Rows[0].Status == EaglOriginalCandidateSlotProfileAudit.Status &&
                   audit.Rows[1].Issue.Contains("no parseable slot-1", StringComparison.Ordinal),
                "Original/candidate slot-1 profile control fixture was not classified conservatively.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            EaglOriginalCandidateSlotProfileAudit.WriteReport(reportPath, audit.Rows);
            EaglOriginalCandidateSlotProfileAudit.WriteSummary(summaryPath, audit.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("model_archive\t", StringComparison.Ordinal),
                "Original/candidate slot-1 profile control TSV shape mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglOriginalCandidateRawStreamIdentityAudit()
    {
        var slotRecords = string.Join("|", Enumerable.Range(0, 5).Select(slot =>
            $"slot={slot};aux=0x0;command_stride={(slot == 0 ? 12 : 8)};stream_stride={(slot == 0 ? 12 : 8)};stream_bytes={(slot == 0 ? 36 : 24)};elements=3;stream=__VB:::__GPvb_{10 + slot}_0_fixture;valid=True"));
        var parsed = EaglOriginalCandidateRawStreamIdentityAudit.ParseSlotRecords(slotRecords);
        Assert(parsed.Count == 5 &&
               parsed.Select(slot => slot.Slot).SequenceEqual(new[] { 0, 1, 2, 3, 4 }) &&
               parsed.All(slot => slot.Valid) &&
               parsed[1].StreamName == "__VB:::__GPvb_11_0_fixture" &&
               EaglOriginalCandidateRawStreamIdentityAudit.ParseSlotRecords(string.Empty).Count == 0,
            "Original/candidate raw stream identity slot parser mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-original-candidate-raw-stream-identity-");
        try
        {
            var candidatePath = Path.Combine(temporaryDirectory.FullName, "candidate.tsv");
            File.WriteAllLines(
                candidatePath,
                new[]
                {
                    "model_archive\tmodel_object\tshape_name\tlayer\tprimitive\tprimitive_type\tstate_names\tcommand_stream_complete\tcommand_count\tcommand_bytes_sha256\tfetch_slot_count\tfetch_binding_count\tcomplete_five_slot_layout\tmesh_group_count\tslot_records\tstatus\toriginal_semantics\tissue",
                    $"fixture.big\tfixture.ord\ttops\t0\t0\tPT_TRIANGLESTRIP\tstate\tTrue\t17\t{new string('A', 64)}\t5\t5\tTrue\t1\t{slotRecords}\tverified\tnot-claimed\t",
                });
            var parsedRows = EaglOriginalCandidateRawStreamIdentityAudit.ReadCandidateRows(candidatePath);
            Assert(parsedRows.Count == 1 && parsedRows[0].FetchSlotCount == 5,
                "Original/candidate raw stream identity candidate report parser mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestApt()
    {
        var data = new byte[32];
        Encoding.ASCII.GetBytes("Apt Data:").CopyTo(data, 0);
        data[9] = (byte)'7';
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12, 4), 28);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 28);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 28);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 28);
        var inspection = AptReader.Inspect(data);
        Assert(inspection.FieldsWithinAsset && inspection.VersionTag == "7" &&
               inspection.HeaderFields.SequenceEqual(new uint[] { 28, 28, 28, 0 }),
            "APT fixture mismatch.");
        var companionSignature = AptReader.GetStructuralSignature(
            inspection,
            new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
            {
                [".apt"] = 0x20,
                [".const"] = 0x20,
                [".o"] = 0xAD6,
                [".xsh"] = 0x1100,
            });
        Assert(companionSignature ==
               "format=Apt Data;version=7;header=0x1C;fields=0x1C,0x1C,0x1C,0x0;within=True;companions=.apt=0x20,.const=0x20,.o=0xAD6,.xsh=0x1100",
            "APT companion structural signature mismatch.");

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 64);
        Assert(!AptReader.Inspect(data).FieldsWithinAsset,
            "APT out-of-range field was not reported.");

        var constant = new byte[32];
        Encoding.ASCII.GetBytes("Apt constant file").CopyTo(constant, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(constant.AsSpan(20, 4), 28);
        BinaryPrimitives.WriteUInt32LittleEndian(constant.AsSpan(28, 4), 32);
        var constantInspection = AptReader.Inspect(constant);
        Assert(constantInspection.Format == "Apt constant" &&
               constantInspection.FieldsWithinAsset,
            "APT constant fixture mismatch.");
        var companionSignatureWithConstant = AptReader.GetCompanionStructuralSignature(
            inspection,
            constantInspection,
            new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
            {
                [".apt"] = 0x20,
                [".const"] = 0x20,
                [".o"] = 0xAD6,
                [".xsh"] = 0x1100,
            });
        Assert(companionSignatureWithConstant ==
               "format=Apt Data;version=7;header=0x1C;fields=0x1C,0x1C,0x1C,0x0;within=True;companions=.apt=0x20,.const=0x20,.o=0xAD6,.xsh=0x1100;constant=format=Apt constant;version=;header=0x1C;fields=0x1C,0x20;within=True",
            "APT combined companion structural signature mismatch.");
        var packageSignature = AptReader.GetPackageStructuralSignature(
            inspection,
            constantInspection,
            new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase)
            {
                [".apt"] = 0x20,
                [".const"] = 0x20,
                [".o"] = 0xAD6,
                [".xsh"] = 0x1100,
            },
            "format=G359;entries=1;ranges=True;names=02;offsets=0x18;sizes=0x8",
            "sections=1;file_sections=1;file_bytes=0x20;symbols=0;relocations=0;geometry=;bounds=none");
        Assert(packageSignature.EndsWith(
                   ";object=sections=1;file_sections=1;file_bytes=0x20;symbols=0;relocations=0;geometry=;bounds=none;shape=format=G359;entries=1;ranges=True;names=02;offsets=0x18;sizes=0x8",
                   StringComparison.Ordinal),
            "APT EAGL/SHPX companion structural signature mismatch.");
    }

    private static void TestAptStructuralCorrelation()
    {
        var observations = new[]
        {
            new AptStructuralObservation(
                "28:28,28,28,0:True",
                "28:28,32:True",
                ".apt,.const,.o,.xsh",
                true,
                true,
                true,
                true,
                false,
                new uint[] { 28, 28, 28, 0 },
                new uint[] { 28, 32 },
                "object-a",
                "shape-a"),
            new AptStructuralObservation(
                "28:28,28,28,0:True",
                "28:28,32:True",
                ".apt,.const,.o,.xsh",
                true,
                true,
                true,
                false,
                false,
                new uint[] { 28, 28, 28, 0 },
                new uint[] { 28, 32 },
                "object-a",
                string.Empty),
            new AptStructuralObservation(
                "16:9,1,2,3:False",
                string.Empty,
                ".apt,.const",
                false,
                false,
                false,
                false,
                false,
                new uint[] { 9, 1, 2, 3 },
                Array.Empty<uint>(),
                string.Empty,
                string.Empty),
        };
        var profiles = AptStructuralCorrelation.Summarize(observations);
        Assert(profiles.Count == 2, "APT structural correlation profile count mismatch.");
        var first = profiles.Single(profile => profile.AptHeaderPattern.StartsWith("28:", StringComparison.Ordinal));
        Assert(first.RowCount == 2 &&
               first.RowsWithAlignedEqualWords == 2 &&
               first.TotalAlignedComparedWords == 4 &&
               first.TotalAlignedEqualWords == 2 &&
               first.TotalAlignedDifferentWords == 2 &&
               first.AptFieldsWithinAssetRows == 2 &&
               first.ConstantFieldsWithinAssetRows == 2 &&
               first.ObjectValidRows == 2 &&
               first.ShapeValidRows == 1 &&
               first.MorphSymbolRows == 0 &&
               first.DistinctObjectStructuralSignatureHashes == 1 &&
               first.DistinctShapeStructuralSignatureHashes == 1 &&
               first.AlignedEqualWordRatio == "0.5",
             "APT structural correlation aggregation mismatch.");
    }

    private static void TestAptAnimationNegativeControl()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-apt-negative-control-");
        try
        {
            var aptReportPath = Path.Combine(temporaryDirectory.FullName, "apt.tsv");
            var animationReportPath = Path.Combine(temporaryDirectory.FullName, "animation.tsv");
            File.WriteAllLines(
                aptReportPath,
                new[]
                {
                    "stem\tobject_valid\tobject_has_morph_symbol\tnormalized_morph_sibling_stem_candidate\tconstant_valid\tshape_valid\tpackage_structural_signature_sha256\tshape_structural_signature_sha256",
                    "Alpha\tTrue\tFalse\t\tTrue\tTrue\tpackage-a\tshape-a",
                    "BANK\tTrue\tFalse\t\tTrue\tTrue\tpackage-b\tshape-b",
                });
            File.WriteAllLines(
                animationReportPath,
                new[]
                {
                    "bank_name\tfile_name",
                    "BANK\tidle",
                });

            var summaries = AptAnimationNegativeControl.Evaluate(
                new[] { (Source: "test", Path: aptReportPath) },
                animationReportPath);
            var summary = summaries.Single();
            Assert(summary.AptRows == 2 &&
                   summary.RowsWithValidObject == 2 &&
                   summary.RowsWithMorphSymbol == 0 &&
                   summary.RowsWithMorphSuffixCandidate == 0 &&
                   summary.RowsWithCompleteStructuralCompanions == 2 &&
                   summary.DistinctStems == 2 &&
                   summary.ExactStemMatchesAnimationBankNames == 1 &&
                   summary.ExactStemMatchesAnimationFileNames == 0 &&
                   summary.DistinctPackageStructuralSignatures == 2 &&
                   summary.DistinctShapeStructuralSignatures == 2,
                "APT animation negative-control summary mismatch.");
            Assert(AptAnimationNegativeControl.GetAnimationRowCount(animationReportPath) == 1 &&
                   AptAnimationNegativeControl.ToTsv(summaries).Count == 2,
                "APT animation negative-control TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAptCrossPackageProfileAudit()
    {
        const string header =
            "apt_header_pattern\tconstant_header_pattern\tcompanion_extensions\trow_count\t" +
            "rows_with_aligned_equal_words\ttotal_aligned_compared_words\ttotal_aligned_equal_words\t" +
            "total_aligned_different_words\tapt_fields_within_asset_rows\tconstant_fields_within_asset_rows\t" +
            "object_valid_rows\tshape_valid_rows\tmorph_symbol_rows\tdistinct_object_structural_signature_hashes\t" +
            "distinct_shape_structural_signature_hashes\taligned_equal_word_ratio";
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-apt-cross-profile-");
        try
        {
            var fePath = Path.Combine(temporaryDirectory.FullName, "fe.tsv");
            var igPath = Path.Combine(temporaryDirectory.FullName, "ig.tsv");
            var auditPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            File.WriteAllLines(
                fePath,
                new[]
                {
                    header,
                    "A\tB\t.apt,.const,.o,.xsh\t2\t1\t4\t1\t3\t2\t2\t2\t1\t0\t2\t1\t0.25",
                    "FE-only\tC\t.apt,.const,.o,.xsh\t1\t0\t2\t0\t2\t1\t1\t1\t1\t0\t1\t1\t0",
                });
            File.WriteAllLines(
                igPath,
                new[]
                {
                    header,
                    "A\tB\t.apt,.const,.o,.xsh\t3\t0\t6\t0\t6\t3\t3\t3\t2\t0\t3\t2\t0",
                    "IG-only\tD\t.apt,.const,.o,.xsh\t1\t0\t2\t0\t2\t1\t1\t1\t1\t0\t1\t1\t0",
                });

            var feProfiles = AptCrossPackageProfileAudit.ReadProfiles(fePath);
            var igProfiles = AptCrossPackageProfileAudit.ReadProfiles(igPath);
            var rows = AptCrossPackageProfileAudit.Audit(feProfiles, igProfiles);
            var summary = AptCrossPackageProfileAudit.Summarize(rows);
            Assert(summary.FeProfileKeys == 2 &&
                   summary.IgProfileKeys == 2 &&
                   summary.SharedProfileKeys == 1 &&
                   summary.FeOnlyProfileKeys == 1 &&
                   summary.IgOnlyProfileKeys == 1 &&
                   summary.SharedKeysWithAlignedEqualWords == 1 &&
                   summary.SharedKeysWithMorphSymbolRows == 0 &&
                   summary.FeAlignedComparedWords == 6 &&
                   summary.IgAlignedComparedWords == 8 &&
                   summary.FeAlignedEqualWords == 1 &&
                   summary.IgAlignedEqualWords == 0,
                "APT cross-package profile summary mismatch.");

            AptCrossPackageProfileAudit.WriteReports(auditPath, summaryPath, rows);
            Assert(File.ReadAllLines(auditPath).Length == 4 &&
                   File.ReadAllLines(summaryPath).Length == 2,
                "APT cross-package profile TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestMorphAmbiguitySummary()
    {
        var rows = new[]
        {
            new MorphAmbiguityReportRow(
                "layout-a",
                3,
                true,
                "candidate_mesh,payload_window",
                "command_stream,payload_window"),
            new MorphAmbiguityReportRow(
                "layout-b",
                2,
                true,
                "payload_window,candidate_mesh",
                "payload_window"),
            new MorphAmbiguityReportRow(
                "layout-stable",
                9,
                false,
                string.Empty,
                string.Empty),
        };

        var summaries = MorphAmbiguitySummarizer.Summarize(rows);
        Assert(summaries.Count == 1 &&
               summaries[0].VariationFields == "candidate_mesh,payload_window" &&
               summaries[0].AmbiguousLayouts == 2 &&
               summaries[0].AmbiguousRows == 5 &&
               summaries[0].StableControlOverlapLayoutCounts == "command_stream=1,payload_window=2" &&
               summaries[0].LayoutKeySha256s.Split(',', StringSplitOptions.RemoveEmptyEntries).Length == 2,
            "Morph ambiguity layer summary mismatch.");
    }

    private static void TestMorphAmbiguityByteSummary()
    {
        var ambiguityRows = new[]
        {
            new MorphAmbiguityReportRow(
                "layout-a",
                2,
                true,
                "payload_window,command_stream",
                string.Empty),
            new MorphAmbiguityReportRow(
                "layout-b",
                1,
                true,
                "command_stream,payload_window",
                string.Empty),
        };
        var byteRows = new[]
        {
            new MorphAmbiguityByteReportRow("layout-a", 1, 1, 0, 0, 0, 0, 1, 8, 4, 2, 3),
            new MorphAmbiguityByteReportRow("layout-a", 0, 0, 0, 0, 0, 0, 0, 4, 2, 1, 1),
            new MorphAmbiguityByteReportRow("layout-b", 2, 2, 0, 0, 0, 0, 1, 6, 3, 2, 2),
        };

        var summaries = MorphAmbiguityByteSummarizer.Summarize(ambiguityRows, byteRows);
        Assert(summaries.Count == 1 &&
               summaries[0].VariationFields == "command_stream,payload_window" &&
               summaries[0].AmbiguousLayouts == 2 &&
               summaries[0].AmbiguousRows == 3 &&
               summaries[0].ByteReportRows == 3 &&
               summaries[0].MissingByteReportRows == 0 &&
               summaries[0].CommandExactRows == 2 &&
               summaries[0].CommandBoundaryExactRows == 2 &&
               summaries[0].NumericValueCategoryExactRows == 2 &&
               summaries[0].CommandMinTotalDeltaRange == "4..8" &&
               summaries[0].CommandMinAlignedDifferenceRange == "2..4" &&
               summaries[0].CommandMinLengthDeltaRange == "1..2" &&
               summaries[0].CommandMaxCommonPrefixRange == "1..3",
            "Morph ambiguity byte-level summary mismatch.");
    }

    private static void TestMorphAmbiguityNumericSummary()
    {
        var ambiguityRows = new[]
        {
            new MorphAmbiguityReportRow("layout-a", 2, true, "command_stream,payload_window", string.Empty),
            new MorphAmbiguityReportRow("layout-b", 1, true, "command_stream,payload_window", string.Empty),
            new MorphAmbiguityReportRow("layout-stable", 4, false, string.Empty, string.Empty),
        };
        var gapRows = new[]
        {
            new MorphAmbiguityNumericGapRow("layout-a", 0, 0, 1, 0, 1, 8, 21, 2, 6, 2),
            new MorphAmbiguityNumericGapRow("layout-b", 0, 0, 0, 0, 0, 16, 58, 8, 0, 0),
        };

        var summaries = MorphAmbiguityNumericSummarizer.Summarize(ambiguityRows, gapRows);
        Assert(summaries.Count == 1 &&
               summaries[0].VariationFields == "command_stream,payload_window" &&
               summaries[0].AmbiguousLayouts == 2 &&
               summaries[0].GapRows == 2 &&
               summaries[0].DistinctGapLayouts == 2 &&
               summaries[0].MissingGapLayouts == 0 &&
               summaries[0].RelativeBoundaryExactRows == 1 &&
               summaries[0].PayloadWindowExactRows == 1 &&
               summaries[0].CanonicalWordDifferenceRange == "8..16" &&
               summaries[0].CanonicalOffsetDifferenceRange == "21..58" &&
               summaries[0].PayloadWindowDifferenceRange == "2..8" &&
               summaries[0].PayloadWindowEqualRange == "0..6" &&
               summaries[0].PayloadWindowCommonPrefixRange == "0..2",
             "Morph ambiguity numeric summary mismatch.");
    }

    private static void TestMorphAmbiguityGroupMetadata()
    {
        var ambiguityRows = new[]
        {
            new MorphAmbiguityReportRow(
                "layout-a",
                2,
                true,
                "payload_window,command_stream",
                "payload_window",
                3,
                2,
                4,
                2,
                1,
                2,
                2,
                1,
                false),
            new MorphAmbiguityReportRow(
                "layout-b",
                1,
                true,
                "command_stream",
                string.Empty,
                2,
                1,
                1,
                2,
                1,
                1,
                1,
                1,
                false),
            new MorphAmbiguityReportRow(
                "layout-stable",
                4,
                false,
                string.Empty,
                string.Empty,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                true),
        };
        var byteRows = new[]
        {
            new MorphAmbiguityByteReportRow("layout-a", 1, 1, 0, 0, 0, 0, 1, 8, 4, 2, 3),
            new MorphAmbiguityByteReportRow("layout-a", 0, 0, 0, 0, 0, 0, 0, 4, 2, 1, 1),
            new MorphAmbiguityByteReportRow("layout-b", 2, 0, 0, 0, 0, 0, 0, 6, 3, 2, 2),
        };
        var numericRows = new[]
        {
            new MorphAmbiguityNumericGapRow("layout-a", 0, 0, 1, 0, 1, 8, 21, 2, 6, 2),
            new MorphAmbiguityNumericGapRow("layout-b", 0, 0, 0, 0, 0, 16, 58, 8, 0, 0),
        };

        var rows = MorphAmbiguityGroupSummarizer.Summarize(
            ambiguityRows,
            byteRows,
            numericRows);
        Assert(rows.Count == 2 &&
               rows[0].LayoutKey == "layout-b" &&
               rows[0].ByteReportRows == 1 &&
               rows[0].CommandExactRows == 1 &&
               rows[0].NumericGapRows == 1 &&
               rows[0].NumericPayloadWindowZeroDifferenceRows == 0 &&
               rows[1].LayoutKey == "layout-a" &&
               rows[1].ByteReportRows == 2 &&
               rows[1].CommandExactRows == 1 &&
               rows[1].CommandBoundaryExactRows == 1 &&
               rows[1].NumericGapRows == 1 &&
               rows[1].RelativeBoundaryExactRows == 1 &&
               rows[1].NumericPayloadWindowExactRows == 1 &&
               rows.All(row => row.LayoutKeySha256.Length == 64),
            "Morph ambiguity group metadata mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-group-metadata-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "metadata.tsv");
            MorphAmbiguityGroupSummarizer.WriteReport(outputPath, rows);
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 3 &&
                   outputLines[0].StartsWith("morph_structural_layout\t", StringComparison.Ordinal) &&
                   outputLines[1].Contains("layout-b", StringComparison.Ordinal),
                "Morph ambiguity group metadata TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestMorphAmbiguityIndependentAudit()
    {
        static MorphAmbiguityIndependentCatalogRow CatalogRow(
            string layout,
            string objectName,
            string candidateMesh,
            string companionLayout,
            string commandStream)
        {
            return new MorphAmbiguityIndependentCatalogRow(
                "test.big",
                objectName,
                "test.ord",
                true,
                true,
                true,
                layout,
                $"payload={layout}",
                companionLayout,
                commandStream,
                $"vertex={commandStream}",
                "numeric=stable",
                candidateMesh,
                $"numeric-match={commandStream}",
                $"paired={layout}");
        }

        var catalogRows = new[]
        {
            CatalogRow("layout-stable", "stable-a.o", "mesh-a", "companion-a", "command-a"),
            CatalogRow("layout-stable", "stable-b.o", "mesh-a", "companion-a", "command-a"),
            CatalogRow("layout-ambiguous", "ambiguous-a.o", "mesh-b", "companion-b", "command-b"),
            CatalogRow("layout-ambiguous", "ambiguous-b.o", "mesh-b", "companion-c", "command-c"),
        };
        var reportRows = new[]
        {
            new MorphAmbiguityReportRow(
                "layout-stable",
                2,
                false,
                string.Empty,
                string.Empty,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                true),
            new MorphAmbiguityReportRow(
                "layout-ambiguous",
                2,
                true,
                "companion_layout,command_stream",
                string.Empty,
                1,
                1,
                2,
                2,
                2,
                1,
                1,
                2,
                false),
        };

        var auditRows = MorphAmbiguityIndependentAudit.Audit(catalogRows, reportRows);
        var ambiguous = auditRows.Single(row => row.LayoutKey == "layout-ambiguous");
        var summaries = MorphAmbiguityIndependentAudit.Summarize(auditRows);
        var ambiguousSummary = summaries.Single(row => row.Scope == "ambiguous");
        Assert(auditRows.Count == 2 &&
               ambiguous.ReportRowCountMatches &&
               ambiguous.ReportMetadataMatchesMeasuredProfiles &&
               ambiguous.MeasuredAmbiguous &&
               ambiguous.AmbiguityLabelMatchesMeasured &&
               ambiguous.CompanionLayoutProfilesMeasured == 2 &&
               ambiguous.CommandStreamProfilesMeasured == 2 &&
               ambiguous.CandidateMeshProfilesMeasured == 1 &&
               ambiguous.CandidateMeshStableCompanionVariable &&
               ambiguous.IndependentVariableLayerCount == 4 &&
               ambiguous.AnyIndependentLayerVariable &&
               ambiguous.CandidateMeshSetSha256.Length == 64 &&
               ambiguous.CompanionLayoutSetSha256.Length == 64 &&
               ambiguousSummary.CandidateMeshStableCompanionVariableLayouts == 1 &&
               ambiguousSummary.AnyIndependentLayerVariableLayouts == 1 &&
               ambiguousSummary.AmbiguityLabelMismatches == 0 &&
               ambiguousSummary.AllIndependentLayersStableLayouts == 0,
            "Morph independent ambiguity audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-independent-audit-test-");
        try
        {
            var auditPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            MorphAmbiguityIndependentAudit.WriteReports(auditPath, summaryPath, auditRows);
            var auditLines = File.ReadAllLines(auditPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(auditLines.Length == 3 &&
                   summaryLines.Length == 4 &&
                   auditLines[0].StartsWith("morph_structural_layout\t", StringComparison.Ordinal) &&
                   summaryLines[0].StartsWith("scope\t", StringComparison.Ordinal),
                "Morph independent ambiguity audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankReader()
    {
        var catalog = AnimationBankReader.Parse(Encoding.ASCII.GetBytes(
            "[Version 0.03]\r\n" +
            "\t123\r\n\r\n" +
            "[Bank]\r\n" +
            "\tDEF_BANK\r\n\r\n" +
            "[Directory]\r\n" +
            "\tC:/assets/art/anims\r\n\r\n" +
            "[Files] 2\r\n" +
            "\tidle 100 Jan 01 2000 00:00:00\r\n" +
            "\tgoal 200 Jan 02 2000 00:00:00\r\n"));
        Assert(
            catalog.VersionTag == "0.03" &&
            catalog.BankName == "DEF_BANK" &&
            catalog.Directory == "C:/assets/art/anims" &&
            catalog.DeclaredFileCount == 2 &&
            catalog.Files.Count == 2 &&
            catalog.DeclaredFileCountMatches &&
            catalog.Files[0].Name == "idle" &&
            catalog.Files[1].Timestamp == 200,
            "Animation catalog parser mismatch.");

        var bankList = AnimationBankReader.ParseBankList(
            Encoding.ASCII.GetBytes("DEF_BANK DEFBANK2 DEF_BANK\r\n"));
        Assert(
            bankList.SequenceEqual(new[] { "DEF_BANK", "DEFBANK2" }),
            "Animation bank list parser mismatch.");

        var symbols = new[]
        {
            new ElfSymbol(1, 0, "__AnimationBank:::DEF_BANK", 0, 0, 0, 1),
            new ElfSymbol(1, 1, "__EAGL_TOOLLIB_VERSION:::EAGL_TOOLLIB_VERSION-4", 0, 0, 0, 1),
            new ElfSymbol(1, 2, "__AnimationBank:::DEF_BANK", 0, 0, 0, 1),
        };
        var bankSymbols = AnimationBankReader.GetAnimationBankSymbolNames(symbols);
        Assert(
            bankSymbols.SequenceEqual(new[] { "DEF_BANK" }),
            "Animation bank symbol extraction mismatch.");

        var layout = AnimationBankReader.GetStructuralLayoutSignature(
            new ElfInspection(
                true,
                true,
                1,
                8,
                0,
                40,
                3,
                true,
                new[]
                {
                    new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                    new ElfSection(".data", 1, 3, 64, 32, 0, 0, 0),
                    new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
                },
                Array.Empty<ElfSymbol>(),
                new[]
                {
                    new ElfRelocation(".rel.data", 1, 12, 2, string.Empty),
                    new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
                }));
        Assert(layout.Length == 64 && layout.All(char.IsAsciiHexDigit),
            "Animation bank structural layout signature mismatch.");

        var payloadA = new byte[128];
        var payloadB = new byte[128];
        payloadA[76] = 0x11;
        payloadB[76] = 0x22;
        payloadA[80] = 0x33;
        payloadB[80] = 0x33;
        var animationPayloadInspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            3,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 64, 32, 0, 0, 0),
                new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
            },
            Array.Empty<ElfSymbol>(),
            new[]
            {
                new ElfRelocation(".rel.data", 1, 12, 2, string.Empty),
            });
        var normalizedA = AnimationBankReader.GetNormalizedDataPayloadSignature(
            payloadA,
            animationPayloadInspection);
        var normalizedB = AnimationBankReader.GetNormalizedDataPayloadSignature(
            payloadB,
            animationPayloadInspection);
        var normalizedBytesA = AnimationBankReader.GetNormalizedDataPayload(
            payloadA,
            animationPayloadInspection);
        var normalizedBytesB = AnimationBankReader.GetNormalizedDataPayload(
            payloadB,
            animationPayloadInspection);
        Assert(
            normalizedA == normalizedB &&
            normalizedA.Length == 64 &&
            normalizedA.All(char.IsAsciiHexDigit) &&
            normalizedBytesA.SequenceEqual(normalizedBytesB) &&
            normalizedBytesA.Length == 32 &&
            normalizedBytesA.AsSpan(12, sizeof(uint)).IndexOfAnyExcept((byte)0) < 0,
            "Animation bank normalized payload signature mismatch.");
        Assert(AnimationBankReader.GetLongestEqualRun(
                   new byte[] { 1, 2, 3, 9, 8, 7 },
                   new byte[] { 1, 2, 4, 9, 8, 0 },
                   1) == 2 &&
               AnimationBankReader.GetLongestEqualRun(
                   new byte[] { 1, 0, 0, 0, 2, 0, 0, 0 },
                   new byte[] { 1, 0, 0, 0, 3, 0, 0, 0 },
                   sizeof(uint)) == 1,
            "Animation bank contiguous payload-run metric mismatch.");
    }

    private static void TestAnimationBankCatalogComparison()
    {
        var firstCatalog = new AnimationBankCatalog(
            "0.03",
            "BANK_A",
            "fixture",
            3,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
                new AnimationCatalogEntry("idle", 300, "idle 300"),
            });
        var secondCatalog = new AnimationBankCatalog(
            "0.03",
            "BANK_B",
            "fixture",
            3,
            new[]
            {
                new AnimationCatalogEntry("idle", 999, "idle 999"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
                new AnimationCatalogEntry("other", 400, "other 400"),
            });
        var thirdCatalog = new AnimationBankCatalog(
            "0.03",
            "BANK_C",
            "fixture",
            1,
            new[]
            {
                new AnimationCatalogEntry("only", 500, "only 500"),
            });
        var inputs = new[]
        {
            (CatalogEntry: "a.axt", Catalog: firstCatalog),
            (CatalogEntry: "b.axt", Catalog: secondCatalog),
            (CatalogEntry: "c.axt", Catalog: thirdCatalog),
        };
        var profiles = AnimationBankCatalogComparison.BuildProfiles(inputs);
        Assert(profiles.Count == 3 &&
               profiles[0].BankName == "BANK_A" &&
               profiles[0].UniqueFileNameCount == 2 &&
               profiles[0].DuplicateFileNameOccurrences == 1 &&
               profiles[0].UniqueTimestampCount == 3 &&
               profiles[0].DuplicateTimestampOccurrences == 0 &&
               profiles[0].OrderedEntrySha256.Length == 64 &&
               profiles[0].DistinctFileNameSetSha256.Length == 64,
            "Animation catalog profile comparison mismatch.");

        var pairs = AnimationBankCatalogComparison.BuildPairs(inputs);
        var firstPair = pairs.Single(pair => pair.BankA == "BANK_A" && pair.BankB == "BANK_B");
        Assert(pairs.Count == 3 &&
               firstPair.SharedFileNameCount == 2 &&
               firstPair.SharedFileNameOccurrenceCount == 2 &&
               firstPair.SharedTimestampValueCount == 1 &&
               firstPair.SamePositionFileNameCount == 2 &&
               firstPair.SamePositionTimestampCount == 1 &&
               firstPair.SharedNameTimestampExactOccurrenceCount == 1 &&
               firstPair.SharedNameTimestampDifferentOccurrenceCount == 2 &&
               firstPair.CommonOrderedNamePrefixCount == 2 &&
               !firstPair.ExactDistinctFileNameSets &&
               firstPair.SharedFileNameSetSha256.Length == 64,
            "Animation catalog pair comparison mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-catalog-compare-test-");
        try
        {
            var profilePath = Path.Combine(temporaryDirectory.FullName, "profiles.tsv");
            var pairPath = Path.Combine(temporaryDirectory.FullName, "pairs.tsv");
            AnimationBankCatalogComparison.WriteProfiles(profilePath, profiles);
            AnimationBankCatalogComparison.WritePairs(pairPath, pairs);
            Assert(File.ReadAllLines(profilePath).Length == 4 &&
                   File.ReadAllLines(pairPath).Length == 4 &&
                   File.ReadAllLines(profilePath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(pairPath)[1].Contains("BANK_A\tBANK_B", StringComparison.Ordinal),
                "Animation catalog comparison TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankCatalogPayloadNameAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            3,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
                new AnimationCatalogEntry("missing", 300, "missing 300"),
            });
        var payload = Encoding.ASCII.GetBytes("\0idle_e\0idle_r\0goal\0goal_extra\0");
        var runs = AnimationBankCatalogPayloadNameAudit.ExtractAsciiRuns(payload);
        var rows = AnimationBankCatalogPayloadNameAudit.Audit("bank.axt", catalog, payload);
        var summary = AnimationBankCatalogPayloadNameAudit.Summarize("BANK", rows, runs.Count);
        var idle = rows.Single(row => row.FileName == "idle");
        var goal = rows.Single(row => row.FileName == "goal");
        var missing = rows.Single(row => row.FileName == "missing");
        Assert(runs.Count == 4 &&
               idle.MatchKind == "tagged" && idle.MatchCount == 2 &&
               goal.MatchKind == "exact" && goal.MatchCount == 1 &&
               missing.MatchKind == "missing" && missing.MatchCount == 0 &&
               summary.CatalogRows == 3 &&
               summary.TaggedRows == 1 &&
               summary.ExactRows == 1 &&
               summary.MissingRows == 1 &&
               summary.AmbiguousRows == 1 &&
               summary.TaggedOccurrences == 2 &&
               summary.MissingNamesSha256.Length == 64,
            "Animation catalog-to-payload name audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-payload-name-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationBankCatalogPayloadNameAudit.WriteReport(reportPath, rows);
            AnimationBankCatalogPayloadNameAudit.WriteSummary(summaryPath, new[] { summary });
            Assert(File.ReadAllLines(reportPath).Length == 4 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(reportPath)[1].Contains("tagged\t2", StringComparison.Ordinal),
                "Animation catalog-to-payload name TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankCatalogPayloadAnchorAudit()
    {
        var detailRows = new IReadOnlyDictionary<string, string>[]
        {
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bank_name"] = "BANK",
                ["match_kind"] = "tagged",
                ["payload_offsets"] = "0x10,0x18",
            },
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["bank_name"] = "BANK",
                ["match_kind"] = "tagged",
                ["payload_offsets"] = "0x30,0x40",
            },
        };
        var rows = AnimationBankCatalogPayloadAnchorAudit.Audit(detailRows);
        var summary = AnimationBankCatalogPayloadAnchorAudit.Summarize(rows);
        Assert(rows.Count == 1 &&
               rows[0].CatalogRows == 2 &&
               rows[0].TaggedRows == 2 &&
               rows[0].RowsWithTwoAnchors == 2 &&
               rows[0].AnchorCount == 4 &&
               rows[0].WithinNameGapCount == 2 &&
               rows[0].CrossNameGapCount == 1 &&
               rows[0].StrictlyIncreasing &&
               rows[0].Status == "verified-structural" &&
               summary.CatalogRows == 2 &&
               summary.AnchorCount == 4 &&
               summary.BanksStrictlyIncreasing == 1 &&
               summary.BanksWithCompletePairs == 1 &&
               summary.Status == "verified-structural" &&
               summary.OriginalSemantics == "not-claimed",
            "Animation catalog payload-anchor audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-payload-anchor-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationBankCatalogPayloadAnchorAudit.WriteReport(reportPath, rows);
            AnimationBankCatalogPayloadAnchorAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("bank_name\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath)[1].Contains("not-claimed", StringComparison.Ordinal),
                "Animation catalog payload-anchor TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankCatalogTimestampAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry(
                    "f_stand",
                    1117080357,
                    "f_stand                    1117080357 May 25 2005 21:05:57"),
                new AnimationCatalogEntry(
                    "gk_casual_idle2",
                    1134180888,
                    "gk_casual_idle2            1134180888 Dec 09 2005 18:14:48"),
            });
        var rows = AnimationBankCatalogTimestampAudit.Audit(new[]
        {
            (CatalogEntry: "bank.axt", Catalog: catalog),
        });
        var summary = AnimationBankCatalogTimestampAudit.Summarize(rows);
        Assert(rows.Count == 1 &&
               rows[0].CatalogRows == 2 &&
               rows[0].DisplayDateParseableRows == 2 &&
               rows[0].ExactTimestampDateRows == 2 &&
               rows[0].TimestampDateMismatchRows == 0 &&
               rows[0].InvalidTimestampRows == 0 &&
               rows[0].InferredUtcOffsetMinutes == "-480,-420" &&
               rows[0].Status == "verified-structural" &&
               rows[0].EvidenceClass == "catalog-source-line-date-correspondence" &&
               summary.CatalogRows == 2 &&
               summary.ExactTimestampDateRows == 2 &&
               summary.BanksWithExactTimestampDate == 1 &&
               summary.InferredUtcOffsetMinutes == "-420,-480" &&
               summary.Status == "verified-structural" &&
               summary.DirectTimingBinding == "not-proven" &&
               summary.OriginalSemantics == "not-claimed",
            "Animation catalog timestamp audit mismatch.");

        var mismatchCatalog = new AnimationBankCatalog(
            "0.03",
            "BAD",
            "fixture",
            1,
            new[]
            {
                new AnimationCatalogEntry(
                    "bad",
                    1117080358,
                    "bad                         1117080358 May 25 2005 21:05:57"),
            });
        var mismatchRows = AnimationBankCatalogTimestampAudit.Audit(new[]
        {
            (CatalogEntry: "bad.axt", Catalog: mismatchCatalog),
        });
        Assert(mismatchRows[0].DisplayDateParseableRows == 1 &&
               mismatchRows[0].ExactTimestampDateRows == 0 &&
               mismatchRows[0].TimestampDateMismatchRows == 1 &&
               mismatchRows[0].Status == "incomplete",
            "Animation catalog timestamp mismatch control failed.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-timestamp-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationBankCatalogTimestampAudit.WriteReport(reportPath, rows);
            AnimationBankCatalogTimestampAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath)[1].Contains("not-proven", StringComparison.Ordinal),
                "Animation catalog timestamp TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestExternalAnimationTimingEvidenceAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-external-timing-test-");
        try
        {
            var inputPath = Path.Combine(temporaryDirectory.FullName, "observations.tsv");
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            File.WriteAllLines(
                inputPath,
                new[]
                {
                    "capture_id\tsource_id\tbank_name\tasset_name\tsample_index\ttime_ms\tobservation_tag",
                    "capture-a\tcapture.sha256\tbank_a\tidle\t0\t0.0\tstanding",
                    "capture-a\tcapture.sha256\tbank_a\tidle\t1\t16.7\tstanding",
                    "capture-b\tcapture.sha256\tbank_a\tidle\t0\t0.0\tstanding",
                    "capture-b\tcapture.sha256\tbank_a\tidle\t1\t16.7\tstanding",
                },
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            var result = ExternalAnimationTimingEvidenceAudit.Audit(inputPath);
            Assert(result.Summary.Status == "verified-external-observation" &&
                   result.Summary.ObservationRows == 4 &&
                   result.Summary.ValidRows == 4 &&
                   result.Summary.InvalidRows == 0 &&
                   result.Summary.DistinctCaptureIds == 2 &&
                   result.Summary.BankAssetPairs == 1 &&
                   result.Summary.PairsWithMultipleCaptures == 1 &&
                   result.Summary.DirectTimingBinding == "not-proven" &&
                   result.Summary.OriginalSemantics == "not-claimed",
                "External timing evidence summary mismatch.");

            ExternalAnimationTimingEvidenceAudit.WriteReport(reportPath, result.Rows);
            ExternalAnimationTimingEvidenceAudit.WriteSummary(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 5 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[0] == ExternalAnimationTimingEvidenceAudit.ReportHeader &&
                   File.ReadAllLines(summaryPath)[1].Contains("not-proven", StringComparison.Ordinal),
                "External timing evidence TSV mismatch.");

            var invalidPath = Path.Combine(temporaryDirectory.FullName, "invalid.tsv");
            File.WriteAllLines(
                invalidPath,
                new[]
                {
                    "capture_id\tsource_id\tbank_name\tasset_name\tsample_index\ttime_ms\tobservation_tag",
                    "capture-a\tcapture.sha256\tbank_a\tidle\t1\t16.7\tstanding",
                    "capture-a\tcapture.sha256\tbank_a\tidle\t0\t0.0\tstanding",
                },
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            var invalid = ExternalAnimationTimingEvidenceAudit.Audit(invalidPath);
            Assert(invalid.Summary.Status == "invalid" &&
                   invalid.Summary.ValidRows == 1 &&
                   invalid.Summary.InvalidRows == 1 &&
                   invalid.Rows.Any(row => row.Issue.Contains("non-monotonic-sequence", StringComparison.Ordinal)),
                "External timing ordering control failed.");

            var missing = ExternalAnimationTimingEvidenceAudit.Audit(
                Path.Combine(temporaryDirectory.FullName, "does-not-exist.tsv"));
            Assert(missing.Summary.Status == "missing-evidence" &&
                   missing.Summary.ObservationRows == 0 &&
                   missing.Summary.DirectTimingBinding == "not-proven",
                "External timing missing-evidence control failed.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankElfStructure()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            3,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 64, 32, 0, 0, 0),
                new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
            },
            new[]
            {
                new ElfSymbol(1, 0, "__AnimationBank:::BANK", 20, 4, 0x11, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 12, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
            });

        var report = AnimationBankElfStructureReporter.Build("bank.axt", catalog, inspection);
        Assert(report.Profile.ElfValid &&
               report.Profile.BankName == "BANK" &&
               report.Profile.CatalogFileCount == 2 &&
               report.Profile.SectionCount == 3 &&
               report.Profile.SymbolCount == 1 &&
               report.Profile.RelocationCount == 2 &&
               report.Profile.FileBackedSectionCount == 3 &&
               report.Profile.FileBackedBytes == 48 &&
               report.Profile.DataBytes == 32 &&
               report.Profile.AnimationBankSymbolTailDistance == "8" &&
               report.Profile.SectionProfileSha256.Length == 64 &&
               report.Profile.RelocationProfileSha256.Length == 64 &&
               report.Sections.Count == 3 &&
               report.Sections.Single(section => section.SectionName == ".data").RelocationCount == 2 &&
               report.Sections.Single(section => section.SectionName == ".data").RelocationOffsetRange == "0xC-0x14",
            "Animation bank ELF structure report mismatch.");

        var dataRelocationAudit = AnimationBankDataRelocationAudit.Build("bank.axt", catalog, inspection);
        Assert(dataRelocationAudit.ElfValid &&
               dataRelocationAudit.DataSize == 32 &&
               dataRelocationAudit.DataRelocationCount == 2 &&
               dataRelocationAudit.DistinctRelativeOffsetCount == 2 &&
               dataRelocationAudit.FirstOffset == 12 &&
               dataRelocationAudit.LastOffset == 20 &&
               dataRelocationAudit.DistinctDeltaCount == 1 &&
               dataRelocationAudit.MinimumDelta == 8 &&
               dataRelocationAudit.MaximumDelta == 8 &&
               dataRelocationAudit.RepeatedDeltaCount == 0 &&
               dataRelocationAudit.DeltaHistogram == "0x8=1" &&
               dataRelocationAudit.OffsetProfileSha256.Length == 64,
            "Animation bank .data relocation audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-elf-structure-test-");
        try
        {
            var profilePath = Path.Combine(temporaryDirectory.FullName, "profiles.tsv");
            var sectionPath = Path.Combine(temporaryDirectory.FullName, "sections.tsv");
            var dataRelocationPath = Path.Combine(temporaryDirectory.FullName, "data-relocations.tsv");
            AnimationBankElfStructureReporter.WriteProfiles(profilePath, new[] { report.Profile });
            AnimationBankElfStructureReporter.WriteSections(sectionPath, report.Sections);
            AnimationBankDataRelocationAudit.WriteReport(dataRelocationPath, new[] { dataRelocationAudit });
            Assert(File.ReadAllLines(profilePath).Length == 2 &&
                   File.ReadAllLines(sectionPath).Length == 4 &&
                   File.ReadAllLines(profilePath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(sectionPath)[2].Contains(".data", StringComparison.Ordinal) &&
                   File.ReadAllLines(dataRelocationPath).Length == 2 &&
                   File.ReadAllLines(dataRelocationPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal),
                "Animation bank ELF structure TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationMorphRelocationProfileAudit()
    {
        var morphRows = new[]
        {
            new MorphRelocationProfileInput(
                "morph-a.big",
                "morph-a.o",
                "section=0x10;relocs=0x0,0x4,0xC;targets=opaque"),
            new MorphRelocationProfileInput(
                "morph-b.big",
                "morph-b.o",
                "section=0x10;relocs=0x0,0x4,0xC;targets=opaque"),
            new MorphRelocationProfileInput(
                "morph-c.big",
                "morph-c.o",
                "section=0x10;relocs=0x0,0x4,0x8,0x18;targets=opaque"),
        };
        var animationRows = new[]
        {
            new AnimationRelocationProfileInput("BANK_A", true, "0x4=2,0x8=1"),
            new AnimationRelocationProfileInput("BANK_B", true, "0x4=1,0x8=1,0x10=1"),
            new AnimationRelocationProfileInput("BANK_BAD", false, "0x4=1"),
        };

        var auditRows = AnimationMorphRelocationProfileAudit.Audit(morphRows, animationRows);
        var first = auditRows.Single(row => row.CatalogRows == 2);
        var second = auditRows.Single(row => row.CatalogRows == 1);
        Assert(auditRows.Count == 2 &&
               first.ParseValid &&
               first.MorphRelocationCount == 3 &&
               first.DistinctMorphGapCount == 2 &&
               first.MorphGapSet == "0x4,0x8" &&
               first.MorphGapHistogram == "0x4=1,0x8=1" &&
               first.ContainingAnimationBankCount == 2 &&
               first.ExactGapSetBankCount == 1 &&
               first.ExactGapSetBanks == "BANK_A" &&
               first.ExactGapHistogramBankCount == 0 &&
               second.ParseValid &&
               second.DistinctMorphGapCount == 2 &&
               second.ContainingAnimationBankCount == 1 &&
               second.ExactGapSetBankCount == 0,
            "Morph/animation relocation-gap overlap audit mismatch.");

        var summary = AnimationMorphRelocationProfileAudit.Summarize(auditRows, 2);
        Assert(summary.MorphCatalogRows == 3 &&
               summary.MorphLayouts == 2 &&
               summary.ParseValidLayouts == 2 &&
               summary.InvalidLayouts == 0 &&
               summary.AnimationBanks == 2 &&
               summary.LayoutsWithContainingAnimationBank == 2 &&
               summary.LayoutsWithExactGapSetBank == 1 &&
               summary.ContainingPairs == 3 &&
               summary.ExactGapSetPairs == 1 &&
               summary.LayoutsWithExactGapHistogramBank == 0 &&
               summary.ExactGapHistogramPairs == 0,
            "Morph/animation relocation-gap overlap summary mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-morph-relocation-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationMorphRelocationProfileAudit.WriteReports(reportPath, summaryPath, auditRows, 2);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 3 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   reportLines[0].StartsWith("layout_key_sha256\t", StringComparison.Ordinal) &&
                   reportLines.Skip(1).Any(line => line.Contains("0x4,0x8", StringComparison.Ordinal)) &&
                   reportLines.Skip(1).Any(line => line.Contains("0x4=1,0x8=1", StringComparison.Ordinal)),
                "Morph/animation relocation-gap overlap TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankTailComparison()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            3,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 64, 32, 0, 0, 0),
                new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
            },
            new[]
            {
                new ElfSymbol(1, 0, "__AnimationBank:::BANK", 20, 4, 0x11, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
            });
        var firstData = new byte[128];
        for (var index = 0; index < 32; index++)
        {
            firstData[64 + index] = checked((byte)(0x40 + index));
        }

        firstData[64 + 20] = 0x1C;
        firstData[64 + 20 + 4] = 0x04;
        firstData[64 + 20 + 5] = 0x00;

        var secondData = firstData.ToArray();
        secondData[64 + 20 + 4] = 0xEE;
        var first = AnimationBankTailComparison.Build("a.axt", catalog, firstData, inspection);
        var second = AnimationBankTailComparison.Build("b.axt", catalog, secondData, inspection);
        var summary = AnimationBankTailComparison.Summarize(new[] { first, second });
        Assert(first.TailByteCount == 12 &&
               first.RawTailHex.Length == 24 &&
               first.NormalizedTailHex.StartsWith("00000000", StringComparison.Ordinal) &&
               first.TailOffset4LittleEndian == 4 &&
               first.DeclaredFileCount == 2 &&
               first.CatalogFileCount == 2 &&
               first.DeclaredFileCountMatchesCatalog &&
               first.CatalogFileCountTimesTwo == 4 &&
               first.TailOffset4EqualsCatalogFileCountTimesTwo &&
               first.DeclaredFileCountTimesTwo == 4 &&
               first.TailOffset4EqualsDeclaredFileCountTimesTwo &&
               !second.TailOffset4EqualsCatalogFileCountTimesTwo &&
               !second.TailOffset4EqualsDeclaredFileCountTimesTwo &&
               first.RelocationOffsets == "0x0:0x2" &&
               first.RawTailSha256.Length == 64 &&
               first.NormalizedTailSha256.Length == 64 &&
               summary.ObservationCount == 2 &&
               summary.MinimumTailByteCount == 12 &&
               summary.ExactRawTailPairs == 0 &&
               summary.ExactNormalizedTailPairs == 0 &&
               summary.DistinctRawTailHashes == 2 &&
               summary.DistinctNormalizedTailHashes == 2 &&
               summary.CommonRawOffsetCount == 11 &&
               summary.CommonNormalizedOffsetCount == 11 &&
               summary.CommonRawPrefixByteCount == 4 &&
               summary.CommonNormalizedPrefixByteCount == 4 &&
               summary.DeclaredFileCountMatchCount == 2 &&
               summary.CatalogFileCountTimesTwoMatchCount == 1 &&
               summary.DeclaredFileCountTimesTwoMatchCount == 1,
            "Animation bank symbol-anchored tail comparison mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-tail-compare-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "tails.tsv");
            AnimationBankTailComparison.WriteReport(outputPath, new[] { first, second });
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 3 &&
                   outputLines[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   outputLines[1].Contains("0x0:0x2", StringComparison.Ordinal),
                "Animation bank tail comparison TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankCatalogTailRangeAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var data = new byte[128];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 0x11111111);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 0x22222222);
        var tail = data.AsSpan(96, 32);
        BinaryPrimitives.WriteUInt32LittleEndian(tail, 28);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[4..], 4);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[16..], 16);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[20..], 32);
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 0, 128, 0, 0, 0),
            },
            new[]
            {
                new ElfSymbol(1, 0, "__AnimationBank:::BANK", 96, 4, 0, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 16, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 24, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 28, 2, string.Empty),
            });
        var row = AnimationBankCatalogTailRangeAudit.Build(
            "bank.axt",
            catalog,
            data,
            inspection);
        var summary = AnimationBankCatalogTailRangeAudit.Summarize(new[] { row });
        Assert(row.ElfValid &&
               row.TailHeaderShapeValid &&
               row.TailEntryCountMatchesCatalogDouble &&
               row.TailRangeWithinData &&
               row.TailRangeAligned &&
               row.TailRangeLengthMatchesCatalogStride &&
               row.TailRangeByteCount == 16 &&
               row.ExpectedCatalogStrideBytes == 16 &&
               row.RelocationsInTailRange == 4 &&
               row.TailRangeRelocationCountMatchesCatalogDouble &&
               row.Status == AnimationBankCatalogTailRangeAudit.Status &&
               row.CandidateRecordBinding == "not-proven" &&
               row.OriginalSemantics == "not-claimed" &&
               summary.CatalogBanks == 1 &&
               summary.ValidatedBanks == 1 &&
               summary.CatalogRows == 2 &&
               summary.TailRangeRelocationCountMatchRows == 1 &&
               summary.VerifiedStructuralRows == 1 &&
               summary.Status == AnimationBankCatalogTailRangeAudit.Status,
            "Animation bank catalog tail-range audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-tail-range-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationBankCatalogTailRangeAudit.WriteReport(reportPath, new[] { row });
            AnimationBankCatalogTailRangeAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 15 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath)[^1].EndsWith("not-claimed", StringComparison.Ordinal),
                "Animation bank catalog tail-range TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankTailPairRelocationAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var data = new byte[128];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 40);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 48);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 56);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28, 4), 64);
        var tail = data.AsSpan(96, 32);
        BinaryPrimitives.WriteUInt32LittleEndian(tail, 28);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[4..], 4);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[16..], 16);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[20..], 32);
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 0, 128, 0, 0, 0),
            },
            new[]
            {
                new ElfSymbol(1, 0, "__AnimationBank:::BANK", 96, 4, 0, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 16, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 24, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 28, 2, string.Empty),
            });
        var row = AnimationBankTailPairRelocationAudit.Build(
            "bank.axt",
            catalog,
            data,
            inspection);
        var details = AnimationBankTailPairRelocationAudit.BuildDetails(
            "bank.axt",
            catalog,
            data,
            inspection);
        var summary = AnimationBankTailPairRelocationAudit.Summarize(new[] { row });
        Assert(row.ElfValid &&
               row.PairOrdinalRows == 2 &&
               row.PairRowsWithTwoRelocations == 2 &&
               row.PairRowsWithExpectedOffsets == 2 &&
               row.RowsWithUnexpectedRelocationMultiplicity == 0 &&
               row.FirstRawWordsWithinData == 2 &&
               row.SecondRawWordsWithinData == 2 &&
               row.FirstRawWordsAtKnownSymbols == 0 &&
               row.SecondRawWordsAtKnownSymbols == 0 &&
               row.DistinctRawWordPairs == 2 &&
               row.Status == AnimationBankTailPairRelocationAudit.Status &&
               row.CandidateRecordBinding == "not-proven" &&
               row.OriginalSemantics == "not-claimed" &&
               details.Count == 2 &&
               details.All(detail => detail.FirstTargetWithinData && detail.SecondTargetWithinData) &&
               summary.CatalogBanks == 1 &&
               summary.CatalogRows == 2 &&
               summary.PairRowsWithExpectedOffsets == 2 &&
               summary.FirstRawWordsWithinData == 2 &&
               summary.SecondRawWordsWithinData == 2 &&
               summary.DistinctRawWordPairs == 2 &&
               summary.BanksWithCompletePairLayout == 1 &&
               summary.Status == AnimationBankTailPairRelocationAudit.Status,
            "Animation bank tail pair-relocation audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-tail-pair-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            var detailPath = Path.Combine(temporaryDirectory.FullName, "detail.tsv");
            AnimationBankTailPairRelocationAudit.WriteReport(reportPath, new[] { row });
            AnimationBankTailPairRelocationAudit.WriteSummary(summaryPath, summary);
            AnimationBankTailPairRelocationAudit.WriteDetails(detailPath, details);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 18 &&
                   File.ReadAllLines(detailPath).Length == 3 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(detailPath)[^1].EndsWith("not-claimed", StringComparison.Ordinal),
                "Animation bank tail pair-relocation TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankTailPairTargetChainAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var data = new byte[256];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 40);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 80);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 120);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28, 4), 160);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(44, 4), 200);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(84, 4), 204);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(124, 4), 208);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(164, 4), 212);
        var tail = data.AsSpan(224, 32);
        BinaryPrimitives.WriteUInt32LittleEndian(tail, 28);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[4..], 4);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[16..], 16);
        BinaryPrimitives.WriteUInt32LittleEndian(tail[20..], 32);
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
            new ElfSection(".data", 1, 3, 0, 256, 0, 0, 0),
            },
            new[]
            {
                new ElfSymbol(1, 0, "__AnimationBank:::BANK", 224, 4, 0, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 16, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 20, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 24, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 28, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 44, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 84, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 124, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 164, 2, string.Empty),
            });
        var row = AnimationBankTailPairTargetChainAudit.Build(
            "bank.axt",
            catalog,
            data,
            inspection);
        var details = AnimationBankTailPairTargetChainAudit.BuildDetails(
            "bank.axt",
            catalog,
            data,
            inspection);
        var summary = AnimationBankTailPairTargetChainAudit.Summarize(new[] { row });
        Assert(row.ElfValid &&
               row.PairRows == 2 &&
               row.FirstTargetRowsWithOneRelocation == 2 &&
               row.SecondTargetRowsWithOneRelocation == 2 &&
               row.FirstSecondLevelWordsWithinData == 2 &&
               row.SecondSecondLevelWordsWithinData == 2 &&
               row.FirstSecondLevelWordsAtKnownSymbols == 0 &&
               row.SecondSecondLevelWordsAtKnownSymbols == 0 &&
               row.RowsWithCompleteTargetChain == 2 &&
               row.DistinctFirstSecondLevelRawWords == 2 &&
               row.DistinctSecondSecondLevelRawWords == 2 &&
               row.Status == AnimationBankTailPairTargetChainAudit.Status &&
               details.Count == 2 &&
               details.All(detail => detail.FirstTargetRelocationCount == 1 &&
                                     detail.SecondTargetRelocationCount == 1 &&
                                     detail.FirstSecondLevelWithinData &&
                                     detail.SecondSecondLevelWithinData) &&
               summary.CatalogBanks == 1 &&
               summary.CatalogRows == 2 &&
               summary.RowsWithCompleteTargetChain == 2 &&
               summary.BanksWithCompleteTargetChain == 1 &&
               summary.Status == AnimationBankTailPairTargetChainAudit.Status,
            "Animation bank tail pair target-chain audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-tail-chain-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            var detailPath = Path.Combine(temporaryDirectory.FullName, "detail.tsv");
            AnimationBankTailPairTargetChainAudit.WriteReport(reportPath, new[] { row });
            AnimationBankTailPairTargetChainAudit.WriteSummary(summaryPath, summary);
            AnimationBankTailPairTargetChainAudit.WriteDetails(detailPath, details);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 18 &&
                   File.ReadAllLines(detailPath).Length == 3 &&
                   File.ReadAllLines(reportPath)[0].StartsWith("catalog_entry\t", StringComparison.Ordinal) &&
                   File.ReadAllLines(detailPath)[^1].EndsWith("not-claimed", StringComparison.Ordinal),
                "Animation bank tail pair target-chain TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAnimationBankSidecarBoundaryAudit()
    {
        var catalog = new AnimationBankCatalog(
            "0.03",
            "BANK",
            "fixture",
            2,
            new[]
            {
                new AnimationCatalogEntry("idle", 100, "idle 100"),
                new AnimationCatalogEntry("goal", 200, "goal 200"),
            });
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            160,
            40,
            4,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 64, 16, 0, 0, 0),
                new ElfSection(".symtab", 2, 0, 80, 16, 0, 0, 16),
                new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
            },
            Array.Empty<ElfSymbol>(),
            Array.Empty<ElfRelocation>());
        var combinedData = new byte[320];
        for (var index = 0; index < combinedData.Length; index++)
        {
            combinedData[index] = (byte)(index & 0xFF);
        }

        var observation = AnimationBankSidecarBoundaryAudit.Build(
            "bank.axt",
            catalog,
            combinedData,
            96,
            224,
            inspection);
        var summary = AnimationBankSidecarBoundaryAudit.Summarize(new[] { observation });
        Assert(observation.CatalogFileCount == 2 &&
               observation.OrdByteCount == 96 &&
               observation.OrlByteCount == 224 &&
               observation.CombinedByteCount == 320 &&
               observation.FileBackedSectionCount == 3 &&
               observation.NonRelocationFileBackedSectionCount == 2 &&
               observation.NonRelocationSectionsWhollyBeforeOrdBoundary == 2 &&
               observation.NonRelocationSectionsCrossingOrdBoundary == 0 &&
               observation.NonRelocationSectionsWhollyAtOrAfterOrdBoundary == 0 &&
               observation.RelocationSectionCount == 1 &&
               observation.RelocationSectionsWhollyAtOrAfterOrdBoundary == 1 &&
               observation.RelocationSectionsStartingAtOrdBoundary == 1 &&
               observation.NonRelocationFileBackedSectionsEndAtOrdBoundary &&
               observation.AllRelocationSectionsAtOrAfterOrdBoundary &&
               observation.SectionHeaderLocation == "after" &&
               observation.SectionHeaderByteCount == 160 &&
               observation.SectionHeaderEndDistanceToCombinedEnd == 0 &&
               observation.BoundaryBeforeHex == "505152535455565758595A5B5C5D5E5F" &&
               observation.BoundaryAfterHex == "606162636465666768696A6B6C6D6E6F" &&
               summary.ObservationCount == 1 &&
               summary.NonRelocationSectionsEndAtOrdBoundaryCount == 1 &&
               summary.AllRelocationSectionsAtOrAfterOrdBoundaryCount == 1 &&
               summary.RelocationSectionsStartingAtOrdBoundaryCount == 1 &&
               summary.SectionHeadersInOrlCount == 1 &&
               summary.SectionHeadersCrossingBoundaryCount == 0 &&
               summary.DistinctOrdByteCounts == "96" &&
               summary.DistinctOrlByteCounts == "224" &&
               summary.DistinctSectionHeaderLocations == "after",
            "Animation bank sidecar boundary audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-animation-sidecar-boundary-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "boundary.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AnimationBankSidecarBoundaryAudit.WriteReport(reportPath, new[] { observation });
            AnimationBankSidecarBoundaryAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[1].Contains("after", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath)[1].Contains("96\t224", StringComparison.Ordinal),
                "Animation bank sidecar boundary TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglMorphSidecarBoundaryAudit()
    {
        var inspection = new ElfInspection(
            true,
            true,
            1,
            8,
            160,
            40,
            4,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 3, 64, 16, 0, 0, 0),
                new ElfSection(".symtab", 2, 0, 80, 16, 0, 0, 16),
                new ElfSection(".rel.data", 9, 0, 96, 16, 0, 1, 8),
            },
            Array.Empty<ElfSymbol>(),
            Array.Empty<ElfRelocation>());
        var combinedData = new byte[320];
        for (var index = 0; index < combinedData.Length; index++)
        {
            combinedData[index] = (byte)(index & 0xFF);
        }

        var observation = EaglMorphSidecarBoundaryAudit.Build(
            "fixture.big",
            "actor_morph.o",
            "actor.ord",
            combinedData,
            96,
            224,
            inspection);
        var summary = EaglMorphSidecarBoundaryAudit.Summarize(new[] { observation });
        Assert(observation.NestedBig == "fixture.big" &&
               observation.MorphObject == "actor_morph.o" &&
               observation.OrdSibling == "actor.ord" &&
               observation.FileBackedSectionCount == 3 &&
               observation.FileBackedSectionLocations == ".data=before|.symtab=before|.rel.data=after" &&
               observation.NonRelocationFileBackedSectionCount == 2 &&
               observation.NonRelocationSectionsWhollyBeforeOrdBoundary == 2 &&
               observation.NonRelocationSectionsCrossingOrdBoundary == 0 &&
               observation.NonRelocationSectionsWhollyAtOrAfterOrdBoundary == 0 &&
               observation.RelocationSectionCount == 1 &&
               observation.RelocationSectionsWhollyAtOrAfterOrdBoundary == 1 &&
               observation.RelocationSectionsStartingAtOrdBoundary == 1 &&
               observation.NonRelocationFileBackedSectionsEndAtOrdBoundary &&
               observation.AllRelocationSectionsAtOrAfterOrdBoundary &&
               observation.SectionHeaderLocation == "after" &&
               observation.BoundaryBeforeHex == "505152535455565758595A5B5C5D5E5F" &&
               observation.BoundaryAfterHex == "606162636465666768696A6B6C6D6E6F" &&
               summary.ObservationCount == 1 &&
               summary.NestedArchiveCount == 1 &&
               summary.SectionHeadersInOrlCount == 1 &&
               summary.SectionHeadersCrossingBoundaryCount == 0 &&
               summary.DistinctOrdByteCounts == "96" &&
               summary.DistinctOrlByteCounts == "224" &&
               summary.DistinctSectionHeaderLocations == "after",
            "EAGL morph sidecar boundary audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-sidecar-boundary-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "boundary.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            EaglMorphSidecarBoundaryAudit.WriteReport(reportPath, new[] { observation });
            EaglMorphSidecarBoundaryAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 2 &&
                   File.ReadAllLines(summaryPath).Length == 2 &&
                   File.ReadAllLines(reportPath)[1].Contains("actor_morph.o", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath)[1].Contains("96\t224", StringComparison.Ordinal),
                "EAGL morph sidecar boundary TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestMorphShpxStructureAudit()
    {
        var catalogRows = new[]
        {
            new MorphShpxCatalogRow("one.big", "one-head_morph.o", "layout-ambiguous"),
            new MorphShpxCatalogRow("two.big", "two-head_morph.o", "layout-ambiguous"),
            new MorphShpxCatalogRow("three.big", "three-head_morph.o", "layout-stable"),
        };
        var ambiguityRows = new[]
        {
            new MorphAmbiguityReportRow("layout-ambiguous", 2, true, "candidate_mesh", string.Empty),
            new MorphAmbiguityReportRow("layout-stable", 1, false, string.Empty, string.Empty),
        };
        var observations = new[]
        {
            new MorphShpxStructureObservation(
                "one.big", "one-head_morph.o", "layout-ambiguous", "one.xsh", "one.xsh", true, true,
                "E", 2, "package-a", null),
            new MorphShpxStructureObservation(
                "two.big", "two-head_morph.o", "layout-ambiguous", "two.xsh", "two.xsh", true, true,
                "E", 3, "package-b", null),
            new MorphShpxStructureObservation(
                "three.big", "three-head_morph.o", "layout-stable", "three.xsh", "three.xsh", true, true,
                "E", 2, "package-a", null),
        };

        var auditRows = MorphShpxStructureAudit.Audit(catalogRows, ambiguityRows, observations);
        var ambiguous = auditRows.Single(row => row.LayoutKey == "layout-ambiguous");
        var stable = auditRows.Single(row => row.LayoutKey == "layout-stable");
        var summaries = MorphShpxStructureAudit.Summarize(auditRows);
        var all = summaries.Single(row => row.Scope == "all");
        Assert(auditRows.Count == 2 &&
               ambiguous.XshPackageProfiles == 2 &&
               ambiguous.XshEntryCountProfiles == 2 &&
               ambiguous.XshStructureVariable &&
               ambiguous.LayoutAmbiguous &&
               ambiguous.ReportRowCountMatches &&
               stable.XshPackageProfiles == 1 &&
               !stable.XshStructureVariable &&
               all.XshVariableLayouts == 1 &&
               all.XshVariableAmbiguousLayouts == 1 &&
               all.ObservationRowCountMismatches == 0 &&
               all.ReportRowCountMismatches == 0,
            "Morph SHPX structure audit did not preserve independent package profiles.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-shpx-structure-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            MorphShpxStructureAudit.WriteReports(reportPath, summaryPath, auditRows);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(reportLines.Length == 3 &&
                   summaryLines.Length == 4 &&
                   reportLines[0].Contains("xsh_package_profiles", StringComparison.Ordinal) &&
                   reportLines[1].Contains("layout-ambiguous", StringComparison.Ordinal) &&
                   summaryLines[0].Contains("xsh_variable_ambiguous_layouts", StringComparison.Ordinal),
                "Morph SHPX structure audit TSV mismatch.");

            var geometryCatalog = new[]
            {
                new MorphShpxGeometryCatalogRow("layout-ambiguous", "geometry-a"),
                new MorphShpxGeometryCatalogRow("layout-ambiguous", "geometry-b"),
                new MorphShpxGeometryCatalogRow("layout-stable", "geometry-c"),
            };
            var packageRows = new[]
            {
                new MorphShpxGeometryPackageRow("layout-ambiguous", 2, true, true, "shpx-set-a"),
                new MorphShpxGeometryPackageRow("layout-stable", 1, false, false, "shpx-set-b"),
            };
            var correlationRows = MorphShpxGeometryCorrelationAudit.Audit(geometryCatalog, packageRows);
            var correlationAmbiguous = correlationRows.Single(row => row.LayoutKey == "layout-ambiguous");
            var correlationStable = correlationRows.Single(row => row.LayoutKey == "layout-stable");
            var correlationSummary = MorphShpxGeometryCorrelationAudit.Summarize(correlationRows)
                .Single(row => row.Scope == "all");
            Assert(correlationAmbiguous.BothVariable &&
                   correlationAmbiguous.CandidateGeometryProfiles == 2 &&
                   correlationAmbiguous.XshPackageProfiles == 2 &&
                   correlationStable.BothStable &&
                   correlationSummary.BothVariableLayouts == 1 &&
                   correlationSummary.BothStableLayouts == 1,
                "SHPX/geometry correlation did not preserve layout-level co-variation.");
            var correlationReportPath = Path.Combine(temporaryDirectory.FullName, "geometry-correlation.tsv");
            var correlationSummaryPath = Path.Combine(temporaryDirectory.FullName, "geometry-correlation-summary.tsv");
            MorphShpxGeometryCorrelationAudit.WriteReports(
                correlationReportPath,
                correlationSummaryPath,
                correlationRows);
            Assert(File.ReadAllLines(correlationReportPath)[0].Contains("classification", StringComparison.Ordinal) &&
                   File.ReadAllLines(correlationSummaryPath)[1].Contains("both-variable", StringComparison.Ordinal),
                "SHPX/geometry correlation TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestMorphElfHeaderAudit()
    {
        var catalogRows = new[]
        {
            new MorphShpxCatalogRow("one.big", "one-head_morph.o", "layout-ambiguous"),
            new MorphShpxCatalogRow("two.big", "two-head_morph.o", "layout-ambiguous"),
            new MorphShpxCatalogRow("three.big", "three-head_morph.o", "layout-stable"),
        };
        var ambiguityRows = new[]
        {
            new MorphAmbiguityReportRow("layout-ambiguous", 2, true, "header", string.Empty),
            new MorphAmbiguityReportRow("layout-stable", 1, false, string.Empty, string.Empty),
        };
        var observations = new[]
        {
            new MorphElfHeaderObservation("one.big", "one-head_morph.o", "layout-ambiguous", true, true, "core", "range-a", "symbol-a", null),
            new MorphElfHeaderObservation("two.big", "two-head_morph.o", "layout-ambiguous", true, true, "core", "range-b", "symbol-b", null),
            new MorphElfHeaderObservation("three.big", "three-head_morph.o", "layout-stable", true, true, "core", "range-a", "symbol-a", null),
        };

        var auditRows = MorphElfHeaderAudit.Audit(catalogRows, ambiguityRows, observations);
        var ambiguous = auditRows.Single(row => row.LayoutKey == "layout-ambiguous");
        var stable = auditRows.Single(row => row.LayoutKey == "layout-stable");
        var all = MorphElfHeaderAudit.Summarize(auditRows).Single(row => row.Scope == "all");
        Assert(auditRows.Count == 2 &&
               ambiguous.HeaderCoreProfiles == 1 &&
               ambiguous.HeaderRangeProfiles == 2 &&
               ambiguous.MorphSymbolMetadataProfiles == 2 &&
               ambiguous.HeaderStructureVariable &&
               ambiguous.MorphSymbolMetadataVariable &&
               ambiguous.LayoutAmbiguous &&
               ambiguous.ReportRowCountMatches &&
               stable.HeaderCoreProfiles == 1 &&
               !stable.HeaderStructureVariable &&
               all.HeaderVariableLayouts == 1 &&
               all.HeaderVariableAmbiguousLayouts == 1 &&
               all.SymbolMetadataVariableLayouts == 1 &&
               all.InvalidRows == 0,
            "Morph ELF header audit did not preserve independent header profiles.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-elf-header-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            MorphElfHeaderAudit.WriteReports(reportPath, summaryPath, auditRows);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(reportLines.Length == 3 &&
                   summaryLines.Length == 4 &&
                   reportLines[0].Contains("header_core_profiles", StringComparison.Ordinal) &&
                   reportLines[1].Contains("layout-ambiguous", StringComparison.Ordinal) &&
                   summaryLines[0].Contains("symbol_metadata_variable_layouts", StringComparison.Ordinal),
                "Morph ELF header audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglMorphNameStateAudit()
    {
        var catalogRows = new[]
        {
            new EaglMorphNameStateCatalogRow(
                "one.big",
                "one-head_morph.o",
                "one-head.ord",
                true,
                new[] { "BS_Fat" },
                new[] { "Coordinates", "Normals" },
                new[] { "head,1" }),
            new EaglMorphNameStateCatalogRow(
                "two.big",
                "two-shape_morph.o",
                "two-shape.ord",
                false,
                new[] { "head", "BS_Fat" },
                new[] { "Coordinates" },
                new[] { "head,1", "BS_Fat" }),
        };

        var auditRows = EaglMorphNameStateAudit.Audit(catalogRows);
        var first = auditRows[0];
        var second = auditRows[1];
        var summary = EaglMorphNameStateAudit.Summarize(auditRows);
        Assert(first.ExactRawShapeStateMatchCount == 0 &&
               first.NormalizedShapeStateMatchCount == 0 &&
               second.ExactRawShapeStateMatchCount == 1 &&
               second.NormalizedShapeStateMatchCount == 2 &&
               summary.Rows == 2 &&
               summary.CompleteSiblingRows == 1 &&
               summary.RowsWithExactRawShapeStateMatch == 1 &&
               summary.RowsWithNormalizedShapeStateMatch == 1 &&
               summary.ExactRawShapeStateMatchCount == 1 &&
               summary.NormalizedShapeStateMatchCount == 2 &&
               summary.DistinctMorphShapeNames == 2 &&
               summary.DistinctSiblingShapeNameStateValues == 2,
            "EAGL morph name/state audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-name-state-audit-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "name-state.tsv");
            EaglMorphNameStateAudit.WriteReport(outputPath, auditRows);
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 3 &&
                   outputLines[0].StartsWith("nested_big\t", StringComparison.Ordinal) &&
                   outputLines[2].Contains("BS_Fat", StringComparison.Ordinal),
                "EAGL morph name/state audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }

        var animationNameNegativeControl = EaglMorphAnimationNameNegativeControl.Evaluate(
            catalogRows,
            4,
            new HashSet<string>(new[] { "head" }, StringComparer.OrdinalIgnoreCase),
            new HashSet<string>(new[] { "BS_Fat", "idle" }, StringComparer.OrdinalIgnoreCase));
        Assert(animationNameNegativeControl.MorphRows == 2 &&
               animationNameNegativeControl.CompleteSiblingRows == 1 &&
               animationNameNegativeControl.DistinctMorphShapeNames == 2 &&
               animationNameNegativeControl.AnimationNameIndexRows == 4 &&
               animationNameNegativeControl.RowsWithExactAnimationFileNameMatch == 2 &&
               animationNameNegativeControl.ExactAnimationFileNameMatchCount == 2 &&
               animationNameNegativeControl.DistinctExactAnimationFileNameMatches == 1 &&
               animationNameNegativeControl.RowsWithExactAnimationBankNameMatch == 1 &&
               animationNameNegativeControl.ExactAnimationBankNameMatchCount == 1 &&
               animationNameNegativeControl.DistinctExactAnimationBankNameMatches == 1 &&
               animationNameNegativeControl.ExactAnimationFileNameMatches == "BS_Fat" &&
               animationNameNegativeControl.ExactAnimationBankNameMatches == "head",
            "EAGL morph/animation name negative control mismatch.");
    }

    private static void TestElf()
    {
        var data = new byte[52];
        data[0] = 0x7F;
        data[1] = (byte)'E';
        data[2] = (byte)'L';
        data[3] = (byte)'F';
        data[4] = 1;
        data[5] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(16, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(18, 2), 8);
        var inspection = ElfReader.Inspect(data);
        Assert(inspection.Is32Bit && inspection.IsLittleEndian && inspection.Type == 1, "ELF fixture mismatch.");
    }

    private static void TestEaglObject()
    {
        var inspection = EaglObjectReader.Inspect(BuildEaglObjectFixture(-1, -2, -3, 4, 5, 6));
        Assert(inspection.BoundsSymbol == "__BBOX:::fixture", "EAGL bounding-box symbol mismatch.");
        Assert(inspection.Bounds is not null, "EAGL bounding box was not decoded.");
        Assert(inspection.Bounds!.Minimum == new EaglVector3(-1, -2, -3) &&
               inspection.Bounds.Maximum == new EaglVector3(4, 5, 6), "EAGL bounding-box values mismatch.");
        Assert(inspection.GeometrySymbols.Contains("__BBOX:::fixture", StringComparer.Ordinal),
            "EAGL geometry symbol inventory mismatch.");
        var structuralSignature = EaglObjectReader.GetStructuralSignature(inspection);
        Assert(structuralSignature.Contains("geometry=__BBOX*=1", StringComparison.Ordinal) &&
               structuralSignature.EndsWith("bounds=present", StringComparison.Ordinal),
            "EAGL companion structural signature mismatch.");
        var structuralLayoutSignature = EaglObjectReader.GetStructuralLayoutSignature(inspection);
        Assert(structuralLayoutSignature.StartsWith(
                   $"sections={inspection.Elf.Sections.Count};geometry_symbols=1;relocations=0;sha256=",
                   StringComparison.Ordinal) &&
               structuralLayoutSignature == EaglObjectReader.GetStructuralLayoutSignature(inspection),
            "EAGL companion structural layout signature mismatch.");

        var fixtureData = BuildEaglObjectFixture(-1, -2, -3, 4, 5, 6);
        var companionOffset = checked((int)inspection.Elf.Sections.Single(section => section.Name == ".shstrtab").Offset);
        var splitInspection = EaglObjectReader.Inspect(
            fixtureData.AsSpan(0, companionOffset),
            fixtureData.AsSpan(companionOffset));
        Assert(splitInspection.Bounds == inspection.Bounds, "Split EAGL object/companion parse mismatch.");

        var invalid = EaglObjectReader.Inspect(BuildEaglObjectFixture(2, 0, 0, 1, 1, 1));
        Assert(invalid.Bounds is null && invalid.BoundsIssue is not null,
            "Inverted EAGL bounds were accepted.");
    }

    private static void TestEaglMorph()
    {
        var data = new byte[128];
        var strings = Encoding.ASCII.GetBytes(
            "BS_FatShape\0BS_HardShape\0Coordinates\0Normals\0");
        strings.CopyTo(data, 8);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(80, 4), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(84, 4), 20);
        var elf = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 0, 0, checked((uint)data.Length), 0, 0, 0),
            },
            new[]
            {
                new ElfSymbol(0, 1, "__Morph:::fixture", 0, 4, 0, 1),
                new ElfSymbol(0, 1, "__PosePaletteBank:::fixture", 0, 4, 0, 1),
            },
            new[]
            {
                new ElfRelocation(".rel.data", 1, 80, 2, string.Empty),
                new ElfRelocation(".rel.data", 1, 84, 2, string.Empty),
            });

        var inspection = EaglMorphReader.Inspect(data, elf);
        Assert(inspection.HasMorphSymbol &&
               inspection.MorphSymbol == "__Morph:::fixture" &&
               inspection.ShapeNames.SequenceEqual(new[] { "BS_FatShape", "BS_HardShape" }) &&
               inspection.AttributeNames.SequenceEqual(new[] { "Coordinates", "Normals" }) &&
               inspection.InternalPointers.Count == 2 &&
               inspection.ShapePointers.Count == 2 &&
               inspection.AttributePointers.Count == 0 &&
               inspection.InternalPointers[0].TargetString == "BS_FatShape" &&
               inspection.InternalPointers[1].TargetString == "BS_HardShape" &&
               inspection.PosePaletteBankSymbols.SequenceEqual(new[] { "__PosePaletteBank:::fixture" }),
               $"EAGL morph named-string and relocation evidence mismatch: morph={inspection.MorphSymbol}, named={string.Join(",", inspection.NamedStrings.Select(item => $"0x{item.Offset:X}:{item.Value}"))}, shapes={string.Join(",", inspection.ShapeNames)}, attrs={string.Join(",", inspection.AttributeNames)}, pointers={string.Join(",", inspection.InternalPointers.Select(pointer => $"0x{pointer.TargetOffset:X}:{pointer.TargetString}"))}.");
        Assert(inspection.PosePaletteBankSymbolRecords.Count == 1 &&
               inspection.PosePaletteBankSymbolRecords[0].TableSectionIndex == 0 &&
               inspection.PosePaletteBankSymbolRecords[0].Index == 1 &&
               inspection.PosePaletteBankSymbolRecords[0].SectionIndex == 1 &&
               inspection.PosePaletteBankSymbolRecords[0].Size == 4,
            "EAGL morph raw pose-palette ELF symbol metadata mismatch.");
        var windows = EaglMorphReader.InspectPayloadWindows(data, elf, radius: 4);
        Assert(windows.Count == 2 &&
               windows[0].StartOffset == 76 &&
               windows[0].RelocationFieldOffsets.SequenceEqual(new uint[] { 80, 84 }) &&
               windows[0].Bytes.Length == 12 &&
               windows[1].StartOffset == 80 &&
               windows[1].RelocationFieldOffsets.SequenceEqual(new uint[] { 80, 84 }),
            "EAGL morph bounded payload-window evidence mismatch.");
        Assert(EaglMorphReader.TryGetNormalizedSiblingStem("star0001-head_morph.o", out var siblingStem) &&
               siblingStem == "star0001-head" &&
               !EaglMorphReader.TryGetNormalizedSiblingStem("star0001-head.ord", out _) &&
               EaglMorphReader.GetRelocationFieldSignature(inspection) == "0x50,0x54" &&
               EaglMorphReader.GetRelativeRelocationFieldSignature(inspection) == "0x0,0x4",
            "EAGL morph sibling candidate or relocation signature mismatch.");
        var normalizedPayloadHash = EaglMorphReader.GetNormalizedPayloadHash(data, elf, inspection);
        var relocationTargetVariant = data.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(relocationTargetVariant.AsSpan(80, 4), 20);
        Assert(normalizedPayloadHash.Length == 64 &&
               normalizedPayloadHash == EaglMorphReader.GetNormalizedPayloadHash(
                   relocationTargetVariant,
                   elf,
                   inspection),
            "EAGL morph normalized payload hash did not mask the observed relocation word.");
        var normalizedPayloadWindowHashSignature =
            EaglMorphReader.GetNormalizedPayloadWindowHashSignature(
                data,
                elf,
                inspection,
                radius: 4);
        var relocationWindowTargetVariant = data.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(
            relocationWindowTargetVariant.AsSpan(84, 4),
            28);
        Assert(normalizedPayloadWindowHashSignature.StartsWith(
                   "windows=2;distinct=",
                   StringComparison.Ordinal) &&
               normalizedPayloadWindowHashSignature ==
               EaglMorphReader.GetNormalizedPayloadWindowHashSignature(
                   relocationWindowTargetVariant,
                   elf,
                   inspection,
                   radius: 4),
            "EAGL morph normalized payload-window signature did not mask the observed relocation words.");
        Assert(EaglMorphReader.GetStructuralLayoutSignature(elf, inspection) ==
               "section=0x80;symbol=0x0/0x4;strings=BS_*@0x8/11;BS_*@0x14/12;Coordinates@0x21/11;Normals@0x2D/7;relocs=0x0,0x4;targets=0x0->0x8:BS_*;0x4->0x14:BS_*",
            "EAGL morph structural layout signature mismatch.");

        var pairedEvidence = new EaglMorphSiblingEvidence(
            "fixture_morph.o",
            "fixture",
            "fixture.ord",
            true,
            true,
            1,
            true,
            "companion-structural",
            "companion-layout",
            "command-stream",
            "vertex-descriptor",
            "vertex-record",
            "numeric-profile",
            "candidate-mesh",
            inspection.ShapeNames.Count,
            inspection.AttributeNames.Count,
            inspection.PosePaletteBankSymbols.Count,
            EaglMorphReader.GetRelocationFieldSignature(inspection),
            EaglMorphReader.GetRelativeRelocationFieldSignature(inspection),
            EaglMorphReader.GetStructuralLayoutSignature(elf, inspection),
            normalizedPayloadHash,
            normalizedPayloadWindowHashSignature,
            "numeric-match",
            null);
        Assert(EaglMorphSiblingReader.GetPairedStructuralSignature(pairedEvidence) ==
               string.Join(
                   "||",
                   EaglMorphReader.GetStructuralLayoutSignature(elf, inspection),
                   normalizedPayloadWindowHashSignature,
                   "companion-layout",
                   "command-stream",
                   "vertex-record",
                   "numeric-profile",
                   "candidate-mesh",
                   "numeric-match"),
            "EAGL morph/sibling paired structural signature mismatch.");

        var siblingInspectionFixture = new EaglObjectInspection(
            elf,
            new[] { "__BBOX:::fixture" },
            null,
            null,
            null);
        var companionNumericMatchSignature =
            EaglMorphReader.GetCompanionNumericMatchSignature(
                data,
                elf,
                inspection,
                siblingInspectionFixture,
                Array.Empty<EaglVertexBufferStream>(),
                Array.Empty<EaglModelPrimitive>(),
                null);
        Assert(companionNumericMatchSignature.StartsWith(
                   "metadata_values=",
                   StringComparison.Ordinal) &&
               companionNumericMatchSignature ==
               EaglMorphReader.GetCompanionNumericMatchSignature(
                   data,
                   elf,
                   inspection,
                   siblingInspectionFixture,
                   Array.Empty<EaglVertexBufferStream>(),
                   Array.Empty<EaglModelPrimitive>(),
                   null),
            "EAGL morph/companion numeric-match signature was not deterministic.");
        var companionNumericMatchAnalysis =
            EaglMorphReader.GetCompanionNumericMatchAnalysis(
                data,
                elf,
                inspection,
                siblingInspectionFixture,
                Array.Empty<EaglVertexBufferStream>(),
                Array.Empty<EaglModelPrimitive>(),
                null);
        Assert(companionNumericMatchAnalysis.CanonicalRecordBoundarySignature.StartsWith(
                   "records=",
                   StringComparison.Ordinal) &&
               companionNumericMatchAnalysis.CanonicalMatchValueCategorySignature.StartsWith(
                   "records=",
                   StringComparison.Ordinal),
            "EAGL morph numeric-match normalized record signatures were not produced.");
    }

    private static void TestByteSequenceComparer()
    {
        var exact = ByteSequenceComparer.Compare(
            new byte[] { 0x10, 0x20, 0x30 },
            new byte[] { 0x10, 0x20, 0x30 });
        Assert(exact.IsExact &&
               exact.EqualBytes == 3 &&
               exact.DifferentBytes == 0 &&
               exact.CommonPrefixLength == 3,
            "Byte sequence exact comparison mismatch.");

        var different = ByteSequenceComparer.Compare(
            new byte[] { 0x10, 0x20, 0x30, 0x40 },
            new byte[] { 0x10, 0x22, 0x30 });
        Assert(!different.IsExact &&
               different.LeftLength == 4 &&
               different.RightLength == 3 &&
               different.ComparedLength == 3 &&
               different.EqualBytes == 2 &&
               different.DifferentBytes == 1 &&
               different.CommonPrefixLength == 1 &&
               different.LengthDelta == -1,
            "Byte sequence aligned comparison mismatch.");

        var tokenExact = TokenSequenceComparer.Compare(
            new[] { "a", "b", "c" },
            new[] { "a", "b", "c" });
        Assert(tokenExact.IsExact &&
               tokenExact.EqualTokens == 3 &&
               tokenExact.DifferentTokens == 0 &&
               tokenExact.CommonPrefixLength == 3,
            "Token sequence exact comparison mismatch.");

        var tokenDifferent = TokenSequenceComparer.Compare(
            new[] { "a", "x", "c", "d" },
            new[] { "a", "b", "c" });
        Assert(!tokenDifferent.IsExact &&
               tokenDifferent.LengthDelta == -1 &&
               tokenDifferent.EqualTokens == 2 &&
               tokenDifferent.DifferentTokens == 1 &&
               tokenDifferent.CommonPrefixLength == 1 &&
               tokenDifferent.DifferentTokenIndexes.SequenceEqual(new[] { 1 }),
            "Token sequence difference comparison mismatch.");

        var delimited = TokenSequenceComparer.CompareDelimited(
            new[] { "0x01:a", "0x02:b", "0x03:c" },
            new[] { "0x01:x", "0x09:b", "0x03:c" },
            ':');
        Assert(delimited.DifferentTokens == 2 &&
               delimited.PrefixFieldDifferences == 1 &&
               delimited.SuffixFieldDifferences == 1 &&
               delimited.CommonPrefixLength == 0,
            "Delimited token field comparison mismatch.");

        var numericLeft = NumericTokenSequenceComparer.ParseCanonical(
            "0x0:0x01020304:a|0x4:0x05060708:b");
        var numericRight = NumericTokenSequenceComparer.ParseCanonical(
            "0x8:0x01020305:x|0x4:0x05060708:b");
        var numericComparison = NumericTokenSequenceComparer.Compare(
            numericLeft,
            numericRight);
        Assert(numericComparison.ComparedCount == 2 &&
               numericComparison.DifferentOffsets == 1 &&
               numericComparison.DifferentWords == 1 &&
               numericComparison.DifferentSuffixes == 1 &&
               numericComparison.DifferentLittleEndianBytes == 1 &&
               numericComparison.CommonPrefixLength == 0 &&
               numericComparison.OffsetDeltaSignature == "-8,0",
            "Numeric token raw-word comparison mismatch.");

        var boundaryLeft = NumericTokenSequenceComparer.ParseCanonical(
            "0x10:0x00000001:a|0x18:0x00000002:b|0x28:0x00000003:c");
        var boundaryRight = NumericTokenSequenceComparer.ParseCanonical(
            "0x100:0x00000011:x|0x108:0x00000012:y|0x118:0x00000013:z");
        var boundaryLeftSignature = NumericTokenSequenceComparer.GetRelativeOffsetBoundarySignature(
            boundaryLeft);
        Assert(boundaryLeftSignature ==
               "records=3;span=24;adjacent_deltas=8,16;delta_histogram=8=1,16=1" &&
               boundaryLeftSignature == NumericTokenSequenceComparer.GetRelativeOffsetBoundarySignature(
                   boundaryRight),
            "Relative numeric boundary signature mismatch.");
    }

    private static void TestNumericGapCorrelation()
    {
        var observations = new[]
        {
            new NumericGapCorrelationObservation(
                "-8,0",
                "layout-a",
                "a.big",
                "a_morph.o",
                "windows=2;distinct=2;hashes=AA,BB",
                "parts=1;sha256=boundary-a",
                2,
                2,
                2,
                1,
                2,
                1,
                1),
            new NumericGapCorrelationObservation(
                "-8,0",
                "layout-a",
                "b.big",
                "b_morph.o",
                "windows=2;distinct=2;hashes=AA,BB",
                "parts=1;sha256=boundary-a",
                0,
                2,
                2,
                0,
                0,
                1,
                0),
            new NumericGapCorrelationObservation(
                "4",
                "layout-b",
                "c.big",
                "c_morph.o",
                "windows=2;distinct=1;hashes=CC,CC",
                "parts=1;sha256=boundary-b",
                1,
                2,
                1),
        };
        var profiles = NumericGapCorrelation.Summarize(observations);
        Assert(profiles.Count == 2, "Numeric gap correlation profile count mismatch.");
        var first = profiles.Single(profile => profile.OffsetDeltaSignature == "-8,0");
        Assert(first.GapRowCount == 2 &&
               first.LayoutCount == 1 &&
               first.PayloadWindowProfileCount == 1 &&
               first.RepeatedPayloadWindowProfileCount == 1 &&
               first.RawCommandBoundaryProfileCount == 1 &&
               first.RepeatedRawCommandBoundaryProfileCount == 1 &&
               first.RowsWithStableRawBoundaryMatch == 1 &&
               first.MaxStableRawBoundaryExactMatches == 2 &&
               first.TotalStableRawBoundaryExactMatches == 2 &&
               first.RowsWithRelativeBoundaryMatch == 1 &&
               first.MaxRelativeBoundaryExactMatches == 1 &&
               first.TotalRelativeBoundaryExactMatches == 1 &&
               first.RowsWithNumericBoundaryMatch == 1 &&
               first.TotalNumericBoundaryExactMatches == 2 &&
               first.RowsWithPayloadWindowMatch == 2 &&
               first.TotalPayloadWindowExactMatches == 2 &&
               first.RowsWithCanonicalOffsetMatch == 1 &&
               first.TotalCanonicalOffsetExactMatches == 1 &&
               first.WindowHashCountValues == "2" &&
               first.DistinctWindowHashCountValues == "2" &&
               first.GapRows == "a.big/a_morph.o,b.big/b_morph.o",
            "Numeric gap correlation aggregation mismatch.");
    }

    private static void TestEaglVertexBuffer()
    {
        var data = new byte[128];
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(16, 4), 48);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(20, 4), 24);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(28, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(32, 4), 80);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(48, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(52, 4), 80);
        var values = new[] { 1f, 2f, 3f, -4f, 5f, 6f };
        for (var index = 0; index < values.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                data.AsSpan(80 + index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(values[index]));
        }

        var section = new ElfSection(".data", 1, 0, 0, checked((uint)data.Length), 0, 0, 0);
        var elf = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[] { new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0), section },
            new[] { new ElfSymbol(0, 0, "__VB:::fixture", 16, 20, 0, 1) },
            Array.Empty<ElfRelocation>());

        Assert(EaglVertexBufferReader.TryRead(data, elf, elf.Symbols[0], out var stream) && stream is not null,
            "Valid EAGL vertex-buffer record was rejected.");
        Assert(EaglVertexBufferReader.Inspect(data, elf).Count == 1,
            "EAGL vertex-buffer inventory did not return the valid stream.");
        Assert(stream!.ElementCount == 2 && stream.Stride == 12 && stream.DataOffset == 80,
            "EAGL vertex-buffer metadata mismatch.");
        Assert(EaglVertexBufferReader.GetStructuralSignature(new[] { stream }) ==
               "streams=1;bytes=0x18;elements=2;strides=12=1;flags=0x1=1;sections=1=1",
            "EAGL vertex-buffer descriptor structural signature mismatch.");
        Assert(EaglVertexBufferReader.GetRelativeRecordStructuralSignature(new[] { stream }) ==
               "streams=1;sha256=DE54112AFB01F3D99398A9B9D8132F1CDF6815637ACCF5081663093695039337",
            "EAGL vertex-buffer relative record structural signature mismatch.");
        Assert(EaglVertexBufferReader.GetNumericStructuralSignature(
                   data,
                   elf,
                   new[] { stream },
                   new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7))) ==
               "streams=1;finite_packed=1;finite_stride8=0;finite_stride12=1;components=3x12=1;bounds_matching_stride12=1",
            "EAGL vertex-buffer numeric structural signature mismatch.");
        Assert(EaglVertexBufferReader.TryMeasureFloat3(data, elf, stream, out var bounds) && bounds is not null,
            "Finite float3 stream was not measured.");
        Assert(bounds!.Minimum == new EaglVector3(-4, 2, 3) &&
               bounds.Maximum == new EaglVector3(1, 5, 6), "EAGL float3 bounds mismatch.");
        Assert(EaglVertexBufferReader.TryMeasurePackedFloatProfile(data, elf, stream, out var packedProfile) &&
               packedProfile is not null && packedProfile.ComponentCount == 3 &&
               packedProfile.Components[0] == new EaglFloatComponentRange(-4, 1),
            "Packed EAGL float profile was not measured.");
        Assert(EaglVertexBufferReader.FitsBounds(
                bounds,
                new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7))),
            "Contained EAGL float3 stream did not match the declared bounds.");

        using var pointCloud = new MemoryStream();
        var export = EaglPositionCloudExporter.WriteObj(
            pointCloud,
            data,
            elf,
            new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7)));
        var objText = Encoding.UTF8.GetString(pointCloud.ToArray());
        Assert(export.StreamCount == 1 && export.VertexCount == 2 &&
               objText.Contains("v 1 2 3", StringComparison.Ordinal) &&
               objText.Contains("p 1", StringComparison.Ordinal) &&
               !objText.Contains("\nf ", StringComparison.Ordinal),
            "EAGL diagnostic OBJ point-cloud export mismatch.");

        var modelData = new byte[1024];
        var attributeStrides = new uint[] { 12, 8, 4, 12, 2 };
        var bufferRecordOffsets = new uint[] { 16, 36, 56, 76, 96 };
        // Keep VB owner descriptors clear of the synthetic render-command stream at 448..528.
        var bufferDescriptorOffsets = new uint[] { 800, 808, 816, 824, 832 };
        var bufferDataOffsets = new uint[] { 576, 624, 656, 672, 720 };
        var vertexSymbols = new List<ElfSymbol>();
        for (var slot = 0; slot < attributeStrides.Length; slot++)
        {
            var byteLength = checked(4u * attributeStrides[slot]);
            var recordOffset = checked((int)bufferRecordOffsets[slot]);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(recordOffset, 4), bufferDescriptorOffsets[slot]);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(recordOffset + 4, 4), byteLength);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(recordOffset + 8, 4), attributeStrides[slot]);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(recordOffset + 12, 4), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(recordOffset + 16, 4), bufferDataOffsets[slot]);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(checked((int)bufferDescriptorOffsets[slot]), 4), 1);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(checked((int)bufferDescriptorOffsets[slot] + 4), 4), bufferDataOffsets[slot]);
            vertexSymbols.Add(new ElfSymbol(
                0,
                slot,
                $"__VB:::__GPvb_{10 + slot}_0_fixture",
                bufferRecordOffsets[slot],
                20,
                0,
                1));
        }
        var modelValues = new[] { 1f, 2f, 3f, -4f, 5f, 6f, 0f, 3f, 4f, 1f, 4f, 5f };
        for (var index = 0; index < modelValues.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                modelData.AsSpan(checked((int)bufferDataOffsets[0]) + index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(modelValues[index]));
        }
        var float2Values = new[] { 0f, 0f, 1f, 0f, 0f, 1f, 1f, 1f };
        for (var index = 0; index < float2Values.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                modelData.AsSpan(checked((int)bufferDataOffsets[1]) + index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(float2Values[index]));
        }
        for (var commandIndex = 0; commandIndex < attributeStrides.Length; commandIndex++)
        {
            var commandOffset = 448 + commandIndex * 16;
            BinaryPrimitives.WriteUInt16LittleEndian(modelData.AsSpan(commandOffset, 2), 4);
            BinaryPrimitives.WriteUInt16LittleEndian(modelData.AsSpan(commandOffset + 2, 2), 9);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(commandOffset + 4, 4), 0);
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(commandOffset + 8, 4), checked((uint)commandIndex));
            BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(commandOffset + 12, 4), attributeStrides[commandIndex]);
        }
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(128 + 0x9C, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(128 + 0xCC, 4), 384);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(384, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(388, 4), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(392, 4), 400);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(400, 4), 416);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(416, 4), 448);
        BinaryPrimitives.WriteUInt32LittleEndian(modelData.AsSpan(424, 4), 544);
        var modelElf = new ElfInspection(
            true,
            true,
            1,
            8,
            0,
            40,
            2,
            true,
            new[]
            {
                new ElfSection(string.Empty, 0, 0, 0, 0, 0, 0, 0),
                new ElfSection(".data", 1, 0, 0, checked((uint)modelData.Length), 0, 0, 0),
            },
            vertexSymbols.Append(new ElfSymbol(0, 5, "__Model:::fixture", 128, 0xD0, 0, 1)).ToArray(),
            new[]
            {
                new ElfRelocation(".rel.data", 1, 408, 2,
                    "__EAGL::GeoPrimState:::SetPrimitiveType=EAGL::PT_TRIANGLESTRIP;"),
            });
        var modelVertexStreams = EaglVertexBufferReader.Inspect(modelData, modelElf);
        var modelFloat2Stream = modelVertexStreams.Single(stream => stream.Stride == 8);
        Assert(EaglVertexBufferReader.TryMeasureFloat2(modelData, modelElf, modelFloat2Stream, out var float2Range) &&
               float2Range is not null && float2Range.MinimumX == 0f && float2Range.MinimumY == 0f &&
               float2Range.MaximumX == 1f && float2Range.MaximumY == 1f &&
               float2Range.IsNearUnitRange(),
            "Finite float2 profile was not measured or classified as near-unit-range.");
        var modelPrimitives = EaglModelReader.Inspect(
            modelData,
            modelElf,
            new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7)),
            modelVertexStreams);
        var modelCommandSignature = EaglModelReader.GetStructuralCommandStreamSignature(modelPrimitives);
        Assert(modelCommandSignature.StartsWith(
                   "primitives=1;complete_streams=1;incomplete_streams=0;commands=5;ids=9=5;sha256=",
                   StringComparison.Ordinal) &&
               modelCommandSignature == EaglModelReader.GetStructuralCommandStreamSignature(modelPrimitives),
            "EAGL opaque command-stream structural signature mismatch.");
        var modelCommandBoundarySignature =
            EaglModelReader.GetRawCommandStreamBoundarySignature(modelPrimitives);
        Assert(modelCommandBoundarySignature.StartsWith(
                   "segments=1;partitions=0:5:80:True:",
                   StringComparison.Ordinal) &&
               modelCommandBoundarySignature ==
               EaglModelReader.GetRawCommandStreamBoundarySignature(modelPrimitives),
            "EAGL raw command-stream boundary signature mismatch.");
        var modelPrimitive = modelPrimitives.SingleOrDefault();
        Assert(modelPrimitive is not null && modelPrimitive.PrimitiveIndex == 0 &&
               modelPrimitive.PrimitiveType == "PT_TRIANGLESTRIP" &&
               modelPrimitive.StateAssignments.Count == 1 &&
               modelPrimitive.StateAssignments[0].Name == "SetPrimitiveType" &&
               modelPrimitive.StateAssignments[0].Value == "EAGL::PT_TRIANGLESTRIP" &&
               modelPrimitive.HasConsistentVertexLayout &&
               modelPrimitive.VertexFetchSlots.Count == 5 &&
               modelPrimitive.VertexFetchBindings.Count == 5 &&
               modelPrimitive.VertexFetchBindings.Select(binding => binding.FetchSlot.Slot).SequenceEqual(new uint[] { 0, 1, 2, 3, 4 }) &&
               modelPrimitive.RenderCommandStream.IsComplete &&
               modelPrimitive.RenderCommandStream.Commands.Count == 5 &&
               modelPrimitive.RenderCommandStream.Commands.All(command => command.CommandId == 9) &&
               modelPrimitive.RenderCommandStream.Commands[0].RawBytes.Length == 16 &&
               BinaryPrimitives.ReadUInt32LittleEndian(
                   modelPrimitive.RenderCommandStream.Commands[1].RawBytes.AsSpan(4, 4)) == 0 &&
               modelPrimitive.BoundsMatchingVertexBuffers.Any(stream =>
                   string.Equals(stream.SymbolName, "__VB:::__GPvb_10_0_fixture", StringComparison.Ordinal)),
            $"EAGL model-to-primitive candidate mapping mismatch: streamsSeen={modelVertexStreams.Count}, count={modelPrimitives.Count}, type={modelPrimitive?.PrimitiveType}, layout={modelPrimitive?.HasConsistentVertexLayout}, slots={modelPrimitive?.VertexFetchSlots.Count}, bindings={modelPrimitive?.VertexFetchBindings.Count}, matches={string.Join(",", modelPrimitive?.BoundsMatchingVertexBuffers.Select(stream => stream.SymbolName) ?? Array.Empty<string>())}.");
        var slot1Binding = modelPrimitive!.VertexFetchBindings.Single(binding => binding.FetchSlot.Slot == 1);
        Assert(EaglVertexBufferReader.TryMeasureFloat2(modelData, modelElf, slot1Binding.Stream, out var slot1Range) &&
               slot1Range is not null && slot1Range.IsNearUnitRange(),
            "Bound EAGL slot-1 float2 profile was not measurable.");
        var malformedCommandData = modelData.ToArray();
        BinaryPrimitives.WriteUInt16LittleEndian(malformedCommandData.AsSpan(448, 2), 0);
        var malformedCommandStream = EaglModelReader.InspectCommandStream(
            malformedCommandData,
            modelElf,
            1,
            448);
        Assert(!malformedCommandStream.IsComplete &&
               malformedCommandStream.Commands.Count == 0 &&
               malformedCommandStream.Issue is not null,
            "Malformed EAGL command stream was accepted.");
        var candidateMesh = EaglCandidateMeshBuilder.Build(
            modelData,
            modelElf,
            new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7)));
        Assert(candidateMesh.Groups.Count == 1 && candidateMesh.VertexCount == 4 &&
               candidateMesh.TriangleCount == 2 &&
               candidateMesh.Groups[0].Slot1Attributes.Count == 4 &&
               candidateMesh.Groups[0].BoundFloatProfiles.Count == 4 &&
               candidateMesh.Groups[0].BoundFloatProfiles.Single(profile => profile.Slot == 0)
                   .Profile.ComponentCount == 3 &&
               candidateMesh.Groups[0].BoundFloatProfiles.Single(profile => profile.Slot == 1)
                   .Profile.ComponentCount == 2 &&
               candidateMesh.Groups[0].StateAssignments.Count == 1 &&
               candidateMesh.Groups[0].StateAssignments[0].Name == "SetPrimitiveType" &&
               candidateMesh.Groups[0].RenderCommandIds.SequenceEqual(new ushort[] { 9, 9, 9, 9, 9 }) &&
               candidateMesh.Groups[0].Slot1Attributes[0] == new EaglFloat2(0, 0) &&
               candidateMesh.Groups[0].Slot1Attributes[3] == new EaglFloat2(1, 1) &&
               candidateMesh.Groups[0].Triangles[0] == new EaglMeshTriangle(0, 1, 2) &&
               candidateMesh.Groups[0].Triangles[1] == new EaglMeshTriangle(2, 1, 3),
            "EAGL candidate mesh builder mismatch.");
        var shapeStateCandidateMesh = candidateMesh with
        {
            Groups = candidateMesh.Groups
                .Select(group => group with
                {
                    StateAssignments = group.StateAssignments
                        .Append(new EaglStateAssignment("SHAPENAME", "tops,1"))
                        .ToArray(),
                })
                .ToArray(),
        };
        Assert(EaglShapeMaterialLoader.HasExactShapeState(shapeStateCandidateMesh, "tops") &&
               EaglShapeMaterialLoader.HasExactShapeState(shapeStateCandidateMesh, "TOPS") &&
               !EaglShapeMaterialLoader.HasExactShapeState(shapeStateCandidateMesh, "bottoms"),
            "EAGL candidate SHAPENAME state gate mismatch.");
        var shapeStatePrimitives = modelPrimitives
            .Select(primitive => primitive with
            {
                StateAssignments = primitive.StateAssignments
                    .Append(new EaglStateAssignment("SHAPENAME", "tops,1"))
                    .ToArray(),
            })
            .ToArray();
        Assert(EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(shapeStatePrimitives, "tops") &&
               !EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(shapeStatePrimitives, "bottoms"),
            "EAGL candidate object SHAPENAME state gate mismatch.");
        Assert(EaglCandidateMeshBuilder.GetStructuralSignature(candidateMesh) ==
               "groups=1;vertices=4;triangles=2;skipped=0;topology=4/2=1",
            "EAGL candidate mesh structural signature mismatch.");
        using var stripMesh = new MemoryStream();
        var stripExport = EaglTriangleStripExporter.WriteObj(
            stripMesh,
            modelData,
            modelElf,
            new EaglAxisAlignedBounds(new EaglVector3(-5, 1, 2), new EaglVector3(2, 6, 7)));
        var stripObjText = Encoding.UTF8.GetString(stripMesh.ToArray());
        Assert(stripExport.PrimitiveGroups == 1 && stripExport.VertexCount == 4 &&
               stripExport.TriangleCount == 2 && stripObjText.Contains("f 1 2 3", StringComparison.Ordinal) &&
               stripObjText.Contains("f 3 2 4", StringComparison.Ordinal),
            "EAGL provisional triangle-strip export mismatch.");

        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(48, 4), 0);
        Assert(!EaglVertexBufferReader.TryRead(data, elf, elf.Symbols[0], out _),
            "Mismatched duplicated EAGL descriptors were accepted.");
    }

    private static byte[] BuildEaglObjectFixture(float minX, float minY, float minZ, float maxX, float maxY, float maxZ)
    {
        const int dataOffset = 64;
        const int dataSize = 24;
        var symbolNames = Encoding.ASCII.GetBytes("\0__BBOX:::fixture\0");
        var sectionNames = Encoding.ASCII.GetBytes("\0.data\0.strtab\0.symtab\0.shstrtab\0");
        var stringTableOffset = dataOffset + dataSize;
        var symbolTableOffset = Align4(stringTableOffset + symbolNames.Length);
        const int symbolTableSize = 32;
        var sectionNamesOffset = symbolTableOffset + symbolTableSize;
        var sectionHeadersOffset = Align4(sectionNamesOffset + sectionNames.Length);
        var file = new byte[sectionHeadersOffset + 5 * 40];

        file[0] = 0x7F;
        file[1] = (byte)'E';
        file[2] = (byte)'L';
        file[3] = (byte)'F';
        file[4] = 1;
        file[5] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(16, 2), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(18, 2), 8);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(32, 4), checked((uint)sectionHeadersOffset));
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(46, 2), 40);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(48, 2), 5);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(50, 2), 4);

        var coordinates = new[] { minX, minY, minZ, maxX, maxY, maxZ };
        for (var index = 0; index < coordinates.Length; index++)
        {
            BinaryPrimitives.WriteInt32LittleEndian(
                file.AsSpan(dataOffset + index * sizeof(float), sizeof(float)),
                BitConverter.SingleToInt32Bits(coordinates[index]));
        }
        symbolNames.CopyTo(file, stringTableOffset);
        sectionNames.CopyTo(file, sectionNamesOffset);

        var symbol = file.AsSpan(symbolTableOffset + 16, 16);
        BinaryPrimitives.WriteUInt32LittleEndian(symbol[..4], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(symbol[4..8], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(symbol[8..12], 4);
        symbol[12] = 0x11;
        BinaryPrimitives.WriteUInt16LittleEndian(symbol[14..16], 1);

        WriteSectionHeader(file, sectionHeadersOffset + 40, 1, 1, dataOffset, dataSize, 0, 0, 4, 0);
        WriteSectionHeader(file, sectionHeadersOffset + 80, 7, 3, stringTableOffset, symbolNames.Length, 0, 0, 1, 0);
        WriteSectionHeader(file, sectionHeadersOffset + 120, 15, 2, symbolTableOffset, symbolTableSize, 2, 1, 4, 16);
        WriteSectionHeader(file, sectionHeadersOffset + 160, 23, 3, sectionNamesOffset, sectionNames.Length, 0, 0, 1, 0);
        return file;
    }

    private static void WriteSectionHeader(
        byte[] file,
        int offset,
        uint name,
        uint type,
        int fileOffset,
        int size,
        uint link,
        uint info,
        uint alignment,
        uint entrySize)
    {
        var header = file.AsSpan(offset, 40);
        BinaryPrimitives.WriteUInt32LittleEndian(header[..4], name);
        BinaryPrimitives.WriteUInt32LittleEndian(header[4..8], type);
        BinaryPrimitives.WriteUInt32LittleEndian(header[16..20], checked((uint)fileOffset));
        BinaryPrimitives.WriteUInt32LittleEndian(header[20..24], checked((uint)size));
        BinaryPrimitives.WriteUInt32LittleEndian(header[24..28], link);
        BinaryPrimitives.WriteUInt32LittleEndian(header[28..32], info);
        BinaryPrimitives.WriteUInt32LittleEndian(header[32..36], alignment);
        BinaryPrimitives.WriteUInt32LittleEndian(header[36..40], entrySize);
    }

    private static int Align4(int value) => checked((value + 3) & ~3);

    private static void TestXpr()
    {
        var data = new byte[40];
        Encoding.ASCII.GetBytes("XPR0").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 40);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 32);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12, 4), 0x00040001);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), 0x22210C29);
        data[32] = 0x00;
        data[33] = 0xF8;
        data[34] = 0xE0;
        data[35] = 0x07;
        var texture = TextureAssetLoader.Load(data);
        Assert(texture.Width == 4 && texture.Height == 4 && texture.PixelFormatName == "DXT1", "XPR metadata mismatch.");
        Assert(texture.RawResource.AsSpan().SequenceEqual(new byte[] { 0, 248, 224, 7, 0, 0, 0, 0 }), "XPR fixture mismatch.");
        Assert(texture.DecodedRgba[0] > 200 && texture.DecodedRgba[1] < 32 && texture.DecodedRgba[2] < 32, "XPR DXT1 decode mismatch.");

        var rgb565 = TextureAssetLoader.Load(BuildXpr(0x05, 2, 1, new byte[]
        {
            0x00, 0xF8,
            0xE0, 0x07,
        }));
        Assert(rgb565.PixelFormatName == "R5G6B5" &&
               rgb565.DecodedRgba.AsSpan().SequenceEqual(new byte[]
               {
                   255, 0, 0, 255,
                   0, 255, 0, 255,
               }), "XPR R5G6B5 decode mismatch.");

        var argb = TextureAssetLoader.Load(BuildXpr(0x06, 1, 1, new byte[]
        {
            0x10, 0x20, 0xE0, 0x80,
        }));
        Assert(argb.PixelFormatName == "A8R8G8B8" &&
               argb.DecodedRgba.AsSpan().SequenceEqual(new byte[] { 0xE0, 0x20, 0x10, 0x80 }),
            "XPR A8R8G8B8 decode mismatch.");

        var xrgb = TextureAssetLoader.Load(BuildXpr(0x07, 1, 1, new byte[]
        {
            0x10, 0x20, 0xE0, 0x00,
        }));
        Assert(xrgb.PixelFormatName == "X8R8G8B8" &&
               xrgb.DecodedRgba.AsSpan().SequenceEqual(new byte[] { 0xE0, 0x20, 0x10, 0xFF }),
            "XPR X8R8G8B8 decode mismatch.");

        var dxt3Resource = new byte[16];
        dxt3Resource[0] = 0x0F;
        dxt3Resource[8] = 0x00;
        dxt3Resource[9] = 0xF8;
        dxt3Resource[10] = 0x00;
        dxt3Resource[11] = 0xF8;
        var dxt3 = TextureAssetLoader.Load(BuildXpr(0x0E, 4, 4, dxt3Resource));
        Assert(dxt3.PixelFormatName == "DXT3" &&
               dxt3.DecodedRgba.AsSpan(0, 8).SequenceEqual(new byte[]
               {
                   255, 0, 0, 255,
                   255, 0, 0, 0,
               }), "XPR DXT3 decode mismatch.");

        var dxt5Resource = new byte[16];
        dxt5Resource[0] = 255;
        dxt5Resource[1] = 0;
        dxt5Resource[2] = 0x08;
        dxt5Resource[8] = 0x00;
        dxt5Resource[9] = 0xF8;
        dxt5Resource[10] = 0x00;
        dxt5Resource[11] = 0xF8;
        var dxt5 = TextureAssetLoader.Load(BuildXpr(0x0F, 4, 4, dxt5Resource));
        Assert(dxt5.PixelFormatName == "DXT5" &&
               dxt5.DecodedRgba.AsSpan(0, 8).SequenceEqual(new byte[]
               {
                   255, 0, 0, 255,
                   255, 0, 0, 0,
               }), "XPR DXT5 decode mismatch.");

        static byte[] BuildXpr(uint format, int width, int height, byte[] resource)
        {
            var data = new byte[checked(32 + resource.Length)];
            Encoding.ASCII.GetBytes("XPR0").CopyTo(data, 0);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), checked((uint)data.Length));
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 32);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(12, 4), 0x00040001);
            var widthLog2 = checked((uint)BitOperations.Log2((uint)width));
            var heightLog2 = checked((uint)BitOperations.Log2((uint)height));
            var headerWord24 = (format << 8) | (widthLog2 << 20) | (heightLog2 << 24);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(24, 4), headerWord24);
            resource.CopyTo(data, 32);
            return data;
        }
    }

    private static void TestAudio()
    {
        var bank = new byte[32];
        Encoding.ASCII.GetBytes("ABKC").CopyTo(bank, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bank.AsSpan(20, 4), 32);
        var bankInspection = AudioAssetReader.InspectBank(bank);
        Assert(bankInspection.DeclaredSizeMatches && bankInspection.HeaderFieldsWithinAsset, "ABKC fixture mismatch.");

        var evidenceBank = new byte[0x5C];
        Encoding.ASCII.GetBytes("ABKC").CopyTo(evidenceBank, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(evidenceBank.AsSpan(20, 4), (uint)evidenceBank.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(evidenceBank.AsSpan(0x1C, 4), 0x5C);
        var bankEvidence = AudioAssetReader.InspectBankEvidence(evidenceBank);
        Assert(bankEvidence.WordAt20MatchesAssetLength && bankEvidence.WordAt1C == 0x5C && bankEvidence.WordAt1CWithinAsset,
            "ABKC structural evidence fixture mismatch.");

        var validOffsets = Encoding.ASCII.GetBytes("0,16,0,\r\n16,8,0,\r\n");
        var offsetInspection = AudioAssetReader.InspectOffsetTable(validOffsets, companionLength: 24);
        Assert(offsetInspection.StructureValid && offsetInspection.FirstRowStartsAtZero && offsetInspection.Rows.Count == 2,
            "Audio offset table fixture mismatch.");
        var companionFixtureBytes = new byte[24];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(companionFixtureBytes, 0);
        Encoding.ASCII.GetBytes("DATA").CopyTo(companionFixtureBytes, 16);
        using var companionFixture = new MemoryStream(companionFixtureBytes, writable: false);
        var companionEvidence = AudioAssetReader.InspectOffsetCompanion(companionFixture, offsetInspection);
        Assert(companionEvidence.AllRangesReadable && companionEvidence.SchlPrefixCount == 1 &&
               !companionEvidence.AllRangesStartWithSchl,
            "Audio companion prefix fixture mismatch.");
        var rawMatchFixture = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(rawMatchFixture.AsSpan(0, 4), 16);
        var rawMatches = AudioAssetReader.FindAlignedRawWordMatches(rawMatchFixture, offsetInspection.Rows);
        Assert(rawMatches.Count == 1 && rawMatches[0].Value == 16 && rawMatches[0].Count == 1 &&
               rawMatches[0].FirstWordOffset == 0,
            "Audio raw-word comparison fixture mismatch.");
        var leadingWords = AudioAssetReader.ReadCompanionLeadingWords(companionFixture, offsetInspection);
        Assert(leadingWords.Count == 2 && leadingWords[0] == 0x4353 && leadingWords[1] == 0x4144,
            "Audio companion leading-word fixture mismatch.");
        using var rangeStream = new AudioRangeStream(companionFixture, 16, 8);
        var rangeBytes = new byte[4];
        Assert(rangeStream.Read(rangeBytes, 0, rangeBytes.Length) == 4 &&
               Encoding.ASCII.GetString(rangeBytes) == "DATA" &&
               rangeStream.Position == 4,
            "Audio bounded range stream fixture mismatch.");
        var leadingWordEvent = new byte[8];
        BinaryPrimitives.WriteUInt32LittleEndian(leadingWordEvent.AsSpan(0, 4), 0x4144);
        var leadingMatches = AudioAssetReader.FindAlignedRawWordMatches(leadingWordEvent, leadingWords);
        Assert(leadingMatches.Count == 1 && leadingMatches[0].Value == 0x4144,
            "Audio leading-word comparison fixture mismatch.");
        var leadingOccurrences = AudioAssetReader.FindAlignedRawWordOccurrences(
            leadingWordEvent, leadingWords.Select(value => (uint)value));
        Assert(leadingOccurrences.Count == 1 && leadingOccurrences[0].WordOffset == 0,
            "Audio leading-word occurrence fixture mismatch.");
        using var headerCandidateCompanion = new MemoryStream(companionFixtureBytes, writable: false);
        var eventHeaderCandidates = AudioAssetReader.CorrelateEventToHeaderRows(
            leadingWordEvent,
            headerCandidateCompanion,
            offsetInspection,
            offsetInspection);
        Assert(eventHeaderCandidates.Count == 1 &&
               eventHeaderCandidates[0].EventWordOffset == 0 &&
               eventHeaderCandidates[0].HeaderRowIndex == 1 &&
               eventHeaderCandidates[0].HeaderLeadingWord == 0x4144 &&
               eventHeaderCandidates[0].ParallelDataRow?.FirstColumn == 16,
            "Audio event-to-header candidate fixture mismatch.");
        var relativeStats = AudioAssetReader.InspectRelativeWordStats(
            leadingWordEvent, leadingOccurrences, radiusWords: 1);
        Assert(relativeStats.Count == 3 && relativeStats.Single(stat => stat.RelativeByteOffset == 0).MostCommonValue == 0x4144,
            "Audio relative-word fixture mismatch.");
        var duplicateWordEvent = new byte[32];
        BinaryPrimitives.WriteUInt32LittleEndian(duplicateWordEvent.AsSpan(8, 4), 0x4144);
        BinaryPrimitives.WriteUInt32LittleEndian(duplicateWordEvent.AsSpan(16, 4), 0x4144);
        var duplicateOccurrences = AudioAssetReader.FindAlignedRawWordOccurrences(
            duplicateWordEvent, new uint[] { 0x4144 });
        var duplicateEvidence = AudioAssetReader.InspectDuplicateOccurrenceContexts(
            duplicateWordEvent, duplicateOccurrences, radiusWords: 1);
        Assert(duplicateEvidence.DuplicateValueCount == 1 &&
               duplicateEvidence.DuplicateOccurrenceCount == 2 &&
               duplicateEvidence.DuplicateValuesWithSingleContext == 1,
            "Audio duplicate occurrence context fixture mismatch.");
        var spacing = AudioAssetReader.InspectOccurrenceSpacing(
            new AudioRawWordOccurrence[]
            {
                new(0x4144, 4),
                new(0x4144, 12),
                new(0x4145, 32),
            });
        Assert(spacing.OccurrenceCount == 3 && spacing.DistinctValueCount == 2 &&
               spacing.FirstOffset == 4 && spacing.LastOffset == 32 &&
               spacing.MinGap == 8 && spacing.MaxGap == 20 &&
               spacing.DistinctGapCount == 2 && spacing.MostCommonGap == 8,
            "Audio occurrence spacing fixture mismatch.");
        using var parallelHeader = new MemoryStream(companionFixtureBytes, writable: false);
        using var parallelData = new MemoryStream(companionFixtureBytes, writable: false);
        var parallel = AudioAssetReader.InspectParallelCompanions(
            parallelHeader, offsetInspection, parallelData, offsetInspection);
        Assert(parallel.SameRangeCount && parallel.HeaderRangesReadable && parallel.DataRangesReadable &&
               parallel.DataSchlPrefixCount == 1 && !parallel.AllDataRangesStartWithSchl,
            "Audio parallel companion fixture mismatch.");
        using var comparisonHeader = new MemoryStream(companionFixtureBytes, writable: false);
        using var comparisonData = new MemoryStream(companionFixtureBytes, writable: false);
        var comparison = AudioAssetReader.CompareIndexedCompanions(
            comparisonHeader, offsetInspection, comparisonData, offsetInspection);
        Assert(comparison.PairCount == 2 && comparison.SameRangeCount &&
               comparison.HeaderDataLengthEqualCount == 2 &&
               comparison.DataSchlPrefixCount == 1 && comparison.Samples.Count == 2,
            "Audio indexed companion comparison fixture mismatch.");
        var indexedSchlFixture = BuildPcmSchlFixture();
        var indexedSchlBytes = new byte[indexedSchlFixture.Length * 2];
        indexedSchlFixture.CopyTo(indexedSchlBytes, 0);
        indexedSchlFixture.CopyTo(indexedSchlBytes, indexedSchlFixture.Length);
        var indexedSchlOffsets = Encoding.ASCII.GetBytes(
            $"0,{indexedSchlFixture.Length},0,\r\n{indexedSchlFixture.Length},{indexedSchlFixture.Length},0,\r\n");
        var indexedSchlInspection = AudioAssetReader.InspectOffsetTable(
            indexedSchlOffsets, indexedSchlBytes.Length);
        using var schlHeader = new MemoryStream(indexedSchlBytes, writable: false);
        using var schlData = new MemoryStream(indexedSchlBytes, writable: false);
        var schlComparison = AudioAssetReader.CompareIndexedSchlHeaders(
            schlHeader, indexedSchlInspection, schlData, indexedSchlInspection);
        Assert(schlComparison.PairCount == 2 && schlComparison.SchlParsedCount == 2 &&
               schlComparison.SchlParseFailureCount == 0 &&
               schlComparison.Samples.Count == 2,
            "Audio indexed SCHl comparison fixture mismatch.");
        var invalidOffsets = Encoding.ASCII.GetBytes("16,8,0,\r\n8,20,0,\r\n");
        var invalidOffsetInspection = AudioAssetReader.InspectOffsetTable(invalidOffsets, companionLength: 24);
        Assert(!invalidOffsetInspection.StructureValid && !invalidOffsetInspection.FirstColumnMonotonic &&
               !invalidOffsetInspection.RangesWithinCompanion,
            "Invalid audio offset table was accepted.");

        var eventFixture = new byte[32];
        new byte[] { 0x03, 0x12, 0x3C, 0x07 }.AsSpan().CopyTo(eventFixture);
        BinaryPrimitives.WriteUInt32LittleEndian(eventFixture.AsSpan(4, 4), 12);
        Encoding.ASCII.GetBytes("name").CopyTo(eventFixture, 16);
        var eventEvidence = AudioAssetReader.InspectEventFile(eventFixture);
        Assert(eventEvidence.HasObservedPrefix && !eventEvidence.WordAt4MatchesAssetLength && eventEvidence.WordAt4WithinAsset,
            "Event structural evidence fixture mismatch.");
        Assert(eventEvidence.TextRuns.Count == 1 && eventEvidence.TextRuns[0].Value == "name",
            "Event text-run fixture mismatch.");
        var namedEventFixture = new byte[32];
        new byte[] { 0x03, 0x12, 0x3C, 0x07 }.AsSpan().CopyTo(namedEventFixture);
        BinaryPrimitives.WriteUInt32LittleEndian(namedEventFixture.AsSpan(4, 4), 12);
        BinaryPrimitives.WriteUInt32LittleEndian(namedEventFixture.AsSpan(8, 4), 16);
        Encoding.ASCII.GetBytes("name\0").CopyTo(namedEventFixture, 16);
        var namedOffsetFixture = Encoding.ASCII.GetBytes("0,4,1,\r\n");
        var namedTable = AudioAssetReader.InspectOffsetTable(namedOffsetFixture, 4);
        var namedHeaderBytes = new byte[4];
        BinaryPrimitives.WriteUInt16LittleEndian(namedHeaderBytes.AsSpan(0, 2), 16);
        using var namedHeaderStream = new MemoryStream(namedHeaderBytes, writable: false);
        var namedCandidates = AudioAssetReader.CorrelateEventTextStartsToHeaderRows(
            namedEventFixture,
            namedHeaderStream,
            namedTable,
            namedTable,
            "name");
        Assert(namedCandidates.Count == 1 && namedCandidates[0].EventName == "name" &&
               namedCandidates[0].EventStringOffset == 16 && namedCandidates[0].HeaderRowIndex == 0 &&
               namedCandidates[0].ParallelDataRow == namedTable.Rows[0] &&
               namedCandidates[0].EventOccurrenceCount == 1,
            "Named event/header candidate correlation mismatch.");
        var stringIndexFixture = new byte[32];
        BinaryPrimitives.WriteUInt32LittleEndian(stringIndexFixture.AsSpan(0, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(stringIndexFixture.AsSpan(4, 4), 0x34214F6B);
        BinaryPrimitives.WriteUInt32LittleEndian(stringIndexFixture.AsSpan(8, 4), 5);
        BinaryPrimitives.WriteUInt32LittleEndian(stringIndexFixture.AsSpan(12, 4), 0x64F64F6B);
        Encoding.ASCII.GetBytes("goal\0beat\0").CopyTo(stringIndexFixture, 16);
        var stringIndexEvidence = AudioAssetReader.InspectStringIndex(stringIndexFixture, 16);
        Assert(stringIndexEvidence.Count == 2 &&
               stringIndexEvidence[0].StringOffset == 0 &&
               stringIndexEvidence[0].Value == "goal" &&
               stringIndexEvidence[1].StringOffset == 5 &&
               stringIndexEvidence[1].HashWord == 0x64F64F6B,
            "Event string-index fixture mismatch.");
        var alignedWordFixture = new byte[24];
        BinaryPrimitives.WriteUInt32LittleEndian(alignedWordFixture.AsSpan(0, 4), 0x34214F6B);
        BinaryPrimitives.WriteUInt32LittleEndian(alignedWordFixture.AsSpan(8, 4), 0x34214F6B);
        BinaryPrimitives.WriteUInt32LittleEndian(alignedWordFixture.AsSpan(12, 4), 0x64F64F6B);
        using var alignedWordStream = new MemoryStream(alignedWordFixture, writable: false);
        alignedWordStream.Position = 4;
        var alignedWordSearch = AudioAssetReader.SearchAlignedWords(
            alignedWordStream,
            new HashSet<uint> { 0x34214F6B, 0x64F64F6B });
        Assert(alignedWordStream.Position == 4 &&
               alignedWordSearch.Single(result => result.Value == 0x34214F6B).Count == 2 &&
               alignedWordSearch.Single(result => result.Value == 0x34214F6B).FirstOffsets.SequenceEqual(new long[] { 0, 8 }) &&
               alignedWordSearch.Single(result => result.Value == 0x64F64F6B).Count == 1 &&
               alignedWordSearch.Single(result => result.Value == 0x64F64F6B).FirstOffsets.SequenceEqual(new long[] { 12 }),
            "Aligned event index word search mismatch.");
        var abkFixture = BuildAbkFixture();
        var abkInspection = AudioAbkReader.Inspect(abkFixture);
        var astFixture = new byte[64];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(astFixture, 0x10);
        var astReferences = AudioAbkReader.InspectAstReferences(abkInspection, astFixture);
        var astRanges = AudioAbkReader.BuildAstRanges(abkInspection, astFixture.Length);
        Assert(abkInspection.ModuleCount == 1 &&
               abkInspection.SampleTables.Count == 1 &&
               abkInspection.Sounds.Count == 2 &&
               abkInspection.NonDummySoundCount == 1 &&
               abkInspection.StreamSoundCount == 1 &&
               astReferences.AllDataOffsetsStartSchl &&
               astRanges.Count == 1 && astRanges[0].Offset == 0x10 && astRanges[0].Length == 48,
            "ABKC module/sample-table fixture mismatch.");
        var bnkFixture = new byte[0xA0];
        Encoding.ASCII.GetBytes("BNKl").CopyTo(bnkFixture, 0x20);
        bnkFixture[0x24] = 5;
        BinaryPrimitives.WriteUInt16LittleEndian(bnkFixture.AsSpan(0x26, 2), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(bnkFixture.AsSpan(0x34, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bnkFixture.AsSpan(0x38, 4), 0x20);
        Encoding.ASCII.GetBytes("PT\u0007\u0000").CopyTo(bnkFixture, 0x58);
        bnkFixture[0x5C] = 0xA0;
        bnkFixture[0x5D] = 1;
        bnkFixture[0x5E] = 0x14;
        bnkFixture[0x5F] = 0x84;
        bnkFixture[0x60] = 3;
        bnkFixture[0x61] = 0x00;
        bnkFixture[0x62] = 0x56;
        bnkFixture[0x63] = 0x22;
        bnkFixture[0x64] = 0x85;
        bnkFixture[0x65] = 1;
        bnkFixture[0x66] = 65;
        bnkFixture[0x67] = 0x88;
        bnkFixture[0x68] = 4;
        bnkFixture[0x69] = 0;
        bnkFixture[0x6A] = 0;
        bnkFixture[0x6B] = 0;
        bnkFixture[0x6C] = 0x60;
        bnkFixture[0x6D] = 0xFF;
        var bnkInspection = new AudioAbkInspection(
            bnkFixture.Length,
            true,
            0,
            0x20,
            0,
            Array.Empty<AudioAbkModule>(),
            Array.Empty<AudioAbkSampleTable>(),
            Array.Empty<AudioAbkSoundEntry>());
        var bnkEvidence = AudioAbkReader.InspectEmbeddedBnk(bnkInspection, bnkFixture);
        Assert(bnkEvidence is not null && bnkEvidence.Version == 5 &&
               bnkEvidence.SoundCount == 2 && bnkEvidence.RealSoundCount == 1 &&
               bnkEvidence.ValidHeaderCount == 1,
            "Embedded BNK table fixture mismatch.");
        var bnkHeaders = AudioAbkReader.InspectEmbeddedBnkHeaders(bnkEvidence!, bnkFixture);
        Assert(bnkHeaders.Count == 1 && bnkHeaders[0].Parsed &&
               AudioAbkReader.TryGetBnkStreamDescriptor(bnkHeaders[0], out var bnkDescriptor) &&
               bnkDescriptor!.Codec == 0x14 && bnkDescriptor.Channels == 1 &&
               bnkDescriptor.SampleRate == 22050 && bnkDescriptor.SampleFrames == 65 &&
               bnkDescriptor.RelativeChannelOffsets.SequenceEqual(new uint[] { 0x60 }),
            "Embedded BNK variable-header fixture mismatch.");
        var schlFieldFixture = BuildPcmSchlFixture();
        var schlFieldOffsets = Encoding.ASCII.GetBytes($"0,{schlFieldFixture.Length},0,\r\n");
        var schlFieldTable = AudioAssetReader.InspectOffsetTable(schlFieldOffsets, schlFieldFixture.Length);
        using var schlFieldStream = new MemoryStream(schlFieldFixture, writable: false);
        var schlFieldComparison = AudioAssetReader.CompareEventToIndexedSchlFields(
            eventFixture, schlFieldStream, schlFieldTable);
        Assert(schlFieldComparison.DataRangeCount == 1 &&
               schlFieldComparison.ParsedStreamCount == 1 &&
               schlFieldComparison.Fields.Count == 7,
            "Event SCHl field comparison fixture mismatch.");

        var stream = new byte[48];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(stream, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(stream.AsSpan(4, 4), 48);
        var streamInspection = AudioAssetReader.InspectStream(stream);
        Assert(streamInspection.FirstChunkWithinAsset, "SCHl fixture mismatch.");
        var envelope = AudioAssetReader.InspectSchlEnvelope(stream, stream.Length);
        Assert(envelope.HasSchlSignature && envelope.HeaderSize == 48 &&
               !envelope.HasPtHeader && envelope.HeaderWithinRange,
            "SCHl envelope fixture mismatch.");

        using var pcmStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var metadataWalk = AudioAssetReader.InspectSchlMetadata(BuildPcmSchlFixture(), 26);
        Assert(metadataWalk.HeaderFullyAvailable && metadataWalk.ReachedTerminator &&
               !metadataWalk.BoundaryViolation && metadataWalk.TokenCount > 0,
            "SCHl metadata walk fixture mismatch.");
        var extendedSchlHeader = new byte[24];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(extendedSchlHeader, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(extendedSchlHeader.AsSpan(4, 4), 24);
        Encoding.ASCII.GetBytes("PT").CopyTo(extendedSchlHeader, 8);
        BinaryPrimitives.WriteUInt16LittleEndian(extendedSchlHeader.AsSpan(10, 2), 7);
        extendedSchlHeader[12] = 0x14;
        extendedSchlHeader[13] = 0xFF;
        BinaryPrimitives.WriteUInt32BigEndian(extendedSchlHeader.AsSpan(14, 4), 4);
        extendedSchlHeader[18] = 1;
        extendedSchlHeader[19] = 2;
        extendedSchlHeader[20] = 3;
        extendedSchlHeader[21] = 4;
        extendedSchlHeader[22] = 0xFF;
        using var extendedSchlStream = new MemoryStream(extendedSchlHeader, writable: false);
        var extendedSchlInfo = EaSchlAudioDecoder.Inspect(extendedSchlStream);
        Assert(extendedSchlInfo.HeaderSize == 24 && extendedSchlInfo.Platform == 7,
            "SCHl extended metadata fixture mismatch.");
        extendedSchlStream.Position = 0;
        var extendedFields = EaSchlAudioDecoder.InspectHeaderFields(extendedSchlStream);
        Assert(extendedFields.Count == 0,
            "SCHl extended metadata field evidence fixture mismatch.");
        using var pcmFieldStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var pcmFields = EaSchlAudioDecoder.InspectHeaderFields(pcmFieldStream);
        Assert(pcmFields.Any(field => field.Tag == 0x82 && field.Value == 1) &&
               pcmFields.Any(field => field.Tag == 0xA0 && field.Value == 0x0A),
            "SCHl audio metadata field evidence fixture mismatch.");
        var decodedWave = EaSchlAudioDecoder.DecodeWave(pcmStream);
        Assert(decodedWave.Length == 46, "In-memory WAV size mismatch.");
        Assert(decodedWave.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
               decodedWave.AsSpan(8, 4).SequenceEqual("WAVE"u8), "In-memory WAV header mismatch.");
        Assert(decodedWave.AsSpan(44, 2).SequenceEqual(new byte[] { 0x34, 0x12 }),
            "In-memory PCM sample mismatch.");
        using var validationStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var validatedAudio = EaSchlAudioDecoder.Validate(validationStream);
        Assert(validatedAudio.SampleFrames == 1 && validatedAudio.Blocks == 1 &&
               validatedAudio.DataBytes == 2 && validatedAudio.Stream.Channels == 1,
            $"Full SCHl validation fixture mismatch: frames={validatedAudio.SampleFrames}, blocks={validatedAudio.Blocks}, bytes={validatedAudio.DataBytes}, channels={validatedAudio.Stream.Channels}.");
        using var candidateLayoutStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var candidateLayoutValid = EaSchlAudioDecoder.TryValidateChannelLayoutCandidate(
            candidateLayoutStream,
            1,
            out var candidateLayout,
            out var candidateLayoutIssue);
        Assert(candidateLayoutValid && candidateLayout is not null &&
               candidateLayout.SampleFrames == 1 && candidateLayout.Stream.Channels == 1 &&
               candidateLayoutIssue is null,
            $"SCHl channel-layout candidate fixture mismatch: valid={candidateLayoutValid}, issue={candidateLayoutIssue}");
        using var firstChunkStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var firstChunk = EaSchlAudioDecoder.InspectFirstDataChunk(firstChunkStream);
        Assert(firstChunk.Offset == 26 && firstChunk.Type == "SCDl" &&
               firstChunk.Size == 77 && firstChunk.WithinStream,
            "EA-XA first-chunk evidence fixture mismatch.");
        using var chunkSequenceStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
        var chunkSequence = EaSchlAudioDecoder.InspectChunkSequence(chunkSequenceStream, maximumChunks: 2, payloadPrefixBytes: 4);
        Assert(chunkSequence.Count == 1 && chunkSequence[0].Type == "SCDl" &&
               chunkSequence[0].PayloadPrefix.Length == 4 && chunkSequence[0].WithinStream,
            "EA-XA chunk-sequence evidence fixture mismatch.");
        using var scclValidationStream = new MemoryStream(BuildScclThenPcmSchlFixture(), writable: false);
        var scclValidation = EaSchlAudioDecoder.Validate(scclValidationStream);
        Assert(scclValidation.SampleFrames == 1 && scclValidation.Blocks == 1,
            "SCCl envelope was not transparently skipped before SCDl validation.");
        scclValidationStream.Position = 0;
        var scclSequence = EaSchlAudioDecoder.InspectChunkSequence(scclValidationStream, maximumChunks: 2, payloadPrefixBytes: 4);
        Assert(scclSequence.Count == 2 && scclSequence[0].Type == "SCCl" &&
               scclSequence[0].Size == 12 && scclSequence[1].Type == "SCDl" &&
               BinaryPrimitives.ReadUInt32LittleEndian(scclSequence[0].PayloadPrefix) == 0x26,
            "SCCl envelope sequence fixture mismatch.");
        var scclFixture = BuildPcmSchlFixture();
        Encoding.ASCII.GetBytes("SCCl").CopyTo(scclFixture, 26);
        using var scclStream = new MemoryStream(scclFixture, writable: false);
        var scclChunk = EaSchlAudioDecoder.InspectFirstDataChunk(scclStream);
        Assert(scclChunk.Type == "SCCl" && scclChunk.WithinStream,
            "SCCl first-chunk evidence fixture mismatch.");
        var sizeLimitRejected = false;
        try
        {
            using var limitedStream = new MemoryStream(BuildPcmSchlFixture(), writable: false);
            _ = EaSchlAudioDecoder.DecodeWave(limitedStream, maximumWaveBytes: 45);
        }
        catch (InvalidDataException)
        {
            sizeLimitRejected = true;
        }
        Assert(sizeLimitRejected, "In-memory WAV limit was not enforced.");
    }

    private static void TestAudioNamedEventCandidateDecoder()
    {
        var fixture = BuildPcmSchlFixture();
        using var infoStream = new MemoryStream(fixture, writable: false);
        var info = EaSchlAudioDecoder.Inspect(infoStream);
        var row = new AudioOffsetRow(1, 0, fixture.Length, 0);
        var validCandidate = new AudioEventCandidateAsset(
            "event.evt",
            "header.big",
            "data.big",
            new AudioEventNamedHeaderCandidate("goal", 16, 0, row, row, 1),
            info,
            null);
        var invalidCandidate = validCandidate with
        {
            Candidate = validCandidate.Candidate with { EventName = "zero-channel" },
            Info = info with { Channels = 0 },
        };
        var candidateLayoutCandidate = invalidCandidate with
        {
            Candidate = invalidCandidate.Candidate with { EventName = "mono-layout-candidate" },
            PayloadChannelLayoutCandidate = 1,
            PayloadChannelLayoutFrames = 1,
        };

        var results = AudioNamedEventCandidateDecoder.Decode(
            new[] { validCandidate, invalidCandidate, candidateLayoutCandidate },
            _ => new MemoryStream(fixture, writable: false));
        var valid = results.Single(result => result.EventName == "goal");
        var invalid = results.Single(result => result.EventName == "zero-channel");
        var candidateLayout = results.Single(result => result.EventName == "mono-layout-candidate");
        Assert(valid.DecodeValidated &&
               valid.WaveBytes == 46 &&
               valid.Channels == 1 &&
               valid.CandidateChannels == 1 &&
               !invalid.DecodeValidated &&
               invalid.Issue == "observed SCHl header has zero channels" &&
               candidateLayout.DecodeValidated &&
               candidateLayout.Channels == 0 &&
               candidateLayout.CandidateChannels == 1 &&
               candidateLayout.WaveBytes == 46,
            "Named event candidate decode evidence mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-named-decode-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "named-events.tsv");
            AudioNamedEventCandidateDecoder.WriteReport(reportPath, results);
            var lines = File.ReadAllLines(reportPath);
            Assert(lines.Length == 4 &&
                   lines[0].StartsWith("event_path\t", StringComparison.Ordinal) &&
                   lines[1].Contains("goal", StringComparison.Ordinal) &&
                   lines[2].Contains("zero-channel", StringComparison.Ordinal) &&
                   lines[3].Contains("mono-layout-candidate", StringComparison.Ordinal),
                "Named event candidate decode TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioNamedEventRuntimeAudit()
    {
        var fixture = BuildPcmSchlFixture();
        using var infoStream = new MemoryStream(fixture, writable: false);
        var info = EaSchlAudioDecoder.Inspect(infoStream);
        var row = new AudioOffsetRow(1, 0, fixture.Length, 0);
        var candidates = new[]
        {
            new AudioEventCandidateAsset(
                "event.evt",
                "header.big",
                "data.big",
                new AudioEventNamedHeaderCandidate("goal", 16, 0, row, row, 1),
                info,
                null),
            new AudioEventCandidateAsset(
                "event.evt",
                "header.big",
                "data.big",
                new AudioEventNamedHeaderCandidate("intro", 32, 1, row, row, 1),
                info,
                null),
        };
        var catalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioEventCandidates: candidates);

        var audit = AudioNamedEventRuntimeAudit.Audit(
            catalog,
            _ => new MemoryStream(fixture, writable: false));
        Assert(audit.Summary.CandidateCount == 2 &&
               audit.Summary.CandidateMetadataValidated == 2 &&
               audit.Summary.RuntimePrepared == 2 &&
               audit.Summary.WaveHeadersValid == 2 &&
               audit.Summary.PcmBytes == 4 &&
               audit.Summary.Status == "verified-runtime-named-payload" &&
               audit.Summary.Provenance == AudioNamedEventRuntimeAudit.Provenance &&
               audit.Summary.OriginalSemantics == "not-claimed" &&
               audit.Rows.All(rowResult =>
                   rowResult.Status == "verified-runtime-named-payload" &&
                   rowResult.PcmBytes == 2 &&
                   rowResult.PcmSha256.Length == 64 &&
                   rowResult.Issue.Length == 0),
            "Named EVT runtime audit fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-named-runtime-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "named-runtime.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "named-runtime-summary.tsv");
            AudioNamedEventRuntimeAudit.WriteReport(reportPath, audit.Rows);
            AudioNamedEventRuntimeAudit.WriteSummary(summaryPath, audit.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0].StartsWith("event_path\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("goal", StringComparison.Ordinal) &&
                   reportLines[2].Contains("intro", StringComparison.Ordinal) &&
                   summaryText.Contains("runtime_prepared\t2", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Named EVT runtime audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventParallelRangeAudit()
    {
        var eventData = new byte[64];
        Encoding.ASCII.GetBytes("goal").CopyTo(eventData, 32);
        var companion = new byte[8];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(companion, 0);
        var offsetData = Encoding.ASCII.GetBytes("0,8,1\n");
        var row = new AudioOffsetRow(1, 0, 8, 1);
        var candidate = new AudioEventCandidateAsset(
            "event.evt",
            "header.big",
            "data.big",
            new AudioEventNamedHeaderCandidate("goal", 32, 0, row, row, 1),
            null,
            null);
        var catalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioEventCandidates: new[] { candidate });

        Stream Open(string path) =>
            path switch
            {
                "event.evt" => new MemoryStream(eventData, writable: false),
                "header.big" or "data.big" => new MemoryStream(companion, writable: false),
                "header.off" or "data.off" => new MemoryStream(offsetData, writable: false),
                _ => throw new InvalidOperationException($"unexpected fixture path: {path}"),
            };

        var audit = AudioEventParallelRangeAudit.Audit(catalog, Open);
        Assert(audit.Summary.CandidateCount == 1 &&
               audit.Summary.EventStringAnchorValidCount == 1 &&
               audit.Summary.HeaderRangeValidCount == 1 &&
               audit.Summary.DataRangeValidCount == 1 &&
               audit.Summary.ParallelRowIndexValidCount == 1 &&
               audit.Summary.DataSchlPrefixCount == 1 &&
               audit.Summary.UniqueEventNameCount == 1 &&
               audit.Summary.UniqueEventStringOffsetCount == 1 &&
               audit.Summary.UniqueHeaderRowCount == 1 &&
               audit.Summary.HeaderTableRows == 1 &&
               audit.Summary.DataTableRows == 1 &&
               audit.Summary.VerifiedRowCount == 1 &&
               audit.Summary.Status == AudioEventParallelRangeAudit.Status &&
               audit.Summary.Provenance == AudioEventParallelRangeAudit.Provenance &&
               audit.Summary.DirectSelectionBinding == "not-proven" &&
               audit.Summary.OriginalSemantics == "not-claimed" &&
               audit.Rows.Count == 1 &&
               audit.Rows[0].EventStringOffset == 32 &&
               audit.Rows[0].Status == AudioEventParallelRangeAudit.Status &&
               audit.Rows[0].Issue.Length == 0,
            "Audio named EVT parallel-range audit fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-parallel-range-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "parallel-range.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "parallel-range-summary.tsv");
            AudioEventParallelRangeAudit.WriteReport(reportPath, audit.Rows);
            AudioEventParallelRangeAuditSummary.Write(summaryPath, audit.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0].StartsWith("event_path\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("goal", StringComparison.Ordinal) &&
                   summaryText.Contains("verified_rows\t1", StringComparison.Ordinal) &&
                   summaryText.Contains("direct_selection_binding\tnot-proven", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Audio named EVT parallel-range TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventXbeHashSearchAudit()
    {
        var eventData = new byte[64];
        BinaryPrimitives.WriteUInt32LittleEndian(eventData.AsSpan(0, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(eventData.AsSpan(4, 4), 0x12344F6B);
        Encoding.ASCII.GetBytes("panna_goal").CopyTo(eventData, 32);
        var xbeData = new byte[16];
        BinaryPrimitives.WriteUInt32LittleEndian(xbeData.AsSpan(4, 4), 0x12344F6B);

        var audit = AudioEventXbeHashSearchAudit.Analyze(eventData, xbeData);
        Assert(audit.Summary.CandidateCount == 1 &&
               audit.Summary.UniqueEventNames == 1 &&
               audit.Summary.UniqueHashWords == 1 &&
               audit.Summary.XbeBytes == 16 &&
               audit.Summary.AlignedOccurrences == 1 &&
               audit.Summary.UnalignedOccurrences == 1 &&
               audit.Summary.HashWordsWithAlignedHits == 1 &&
               audit.Summary.HashWordsWithUnalignedHits == 1 &&
               audit.Summary.Status == AudioEventXbeHashSearchAudit.Status &&
               audit.Summary.Provenance == AudioEventXbeHashSearchAudit.Provenance &&
               audit.Summary.DirectSelectionBinding == "not-proven" &&
               audit.Summary.OriginalSemantics == "not-claimed" &&
               audit.Rows.Count == 1 &&
               audit.Rows[0].EventName == "panna_goal" &&
               audit.Rows[0].HashWord == 0x12344F6B &&
               audit.Rows[0].AlignedFirstOffsets == "0x4" &&
               audit.Rows[0].UnalignedFirstOffsets == "0x4" &&
               audit.Rows[0].Status == AudioEventXbeHashSearchAudit.Status,
            "Audio EVT/XBE hash-search audit fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-xbe-hash-search-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "hash-search.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "hash-search-summary.tsv");
            AudioEventXbeHashSearchAudit.WriteReport(reportPath, audit.Rows);
            AudioEventXbeHashSearchSummary.Write(summaryPath, audit.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0].StartsWith("event_name\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("panna_goal", StringComparison.Ordinal) &&
                   reportLines[1].Contains("0x12344F6B", StringComparison.Ordinal) &&
                   summaryText.Contains("aligned_occurrences\t1", StringComparison.Ordinal) &&
                   summaryText.Contains("direct_selection_binding\tnot-proven", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Audio EVT/XBE hash-search TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventAudioAssetHashSearchAudit()
    {
        var input = new[]
        {
            new AudioEventAudioAssetHashSearchInput(
                "data/audio/one.abk",
                32,
                new[]
                {
                    new AudioAlignedWordSearchResult(0x12344F6B, 0, Array.Empty<long>()),
                }),
            new AudioEventAudioAssetHashSearchInput(
                "data/audio/two.ast",
                48,
                new[]
                {
                    new AudioAlignedWordSearchResult(0x12344F6B, 1, new[] { 4L }),
                    new AudioAlignedWordSearchResult(0x22344F6B, 2, new[] { 8L, 12L }),
                }),
        };
        var result = AudioEventAudioAssetHashSearchAudit.Analyze(2, input, excludedSourceRows: 1);
        Assert(result.Rows.Count == 2 &&
               result.Summary.EventPath == "data/audio/eventspa.evt" &&
               result.Summary.CandidateCount == 2 &&
               result.Summary.ScannedAssetCount == 2 &&
               result.Summary.ValidAssetCount == 2 &&
               result.Summary.ScannedBytes == 80 &&
               result.Summary.ExcludedSourceRows == 1 &&
               result.Summary.AssetsWithHits == 1 &&
               result.Summary.AssetHashHitPairs == 2 &&
               result.Summary.AlignedOccurrences == 3 &&
               result.Summary.Status == AudioEventAudioAssetHashSearchAudit.Status &&
               result.Summary.Provenance == AudioEventAudioAssetHashSearchAudit.Provenance &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed" &&
               result.Rows[1].AssetPath == "data/audio/two.ast" &&
               result.Rows[1].HashWordsWithHits == 2,
            "Audio EVT complete-audio-asset hash-universe fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-asset-hash-universe-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "universe.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "universe-summary.tsv");
            AudioEventAudioAssetHashSearchAudit.WriteReport(reportPath, result.Rows);
            AudioEventAudioAssetHashSearchAudit.WriteSummary(summaryPath, result.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0].StartsWith("asset_path\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("data/audio/two.ast", StringComparison.Ordinal) &&
                   summaryText.Contains("scanned_bytes\t80", StringComparison.Ordinal) &&
                   summaryText.Contains("asset_hash_hit_pairs\t2", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Audio EVT complete-audio-asset hash-universe TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventDataAssetHashSearchAudit()
    {
        var input = new[]
        {
            new AudioEventDataAssetHashSearchInput(
                "data/audio/one.abk",
                32,
                true,
                new[]
                {
                    new AudioAlignedWordSearchResult(0x12344F6B, 1, new[] { 0L }),
                },
                string.Empty),
            new AudioEventDataAssetHashSearchInput(
                "data/scripts/two.bin",
                48,
                true,
                new[]
                {
                    new AudioAlignedWordSearchResult(0x12344F6B, 0, Array.Empty<long>()),
                    new AudioAlignedWordSearchResult(0x22344F6B, 2, new[] { 8L, 12L }),
                },
                string.Empty),
        };
        var result = AudioEventDataAssetHashSearchAudit.Analyze(2, input, excludedSourceRows: 1);
        Assert(result.Rows.Count == 2 &&
               result.Summary.AssetRoot == "data" &&
               result.Summary.EventPath == "data/audio/eventspa.evt" &&
               result.Summary.CandidateCount == 2 &&
               result.Summary.ScannedAssetCount == 2 &&
               result.Summary.ValidAssetCount == 2 &&
               result.Summary.ScannedBytes == 80 &&
               result.Summary.ExcludedSourceRows == 1 &&
               result.Summary.AssetsWithHits == 2 &&
               result.Summary.AssetHashHitPairs == 2 &&
               result.Summary.AlignedOccurrences == 3 &&
               result.Summary.Status == AudioEventDataAssetHashSearchAudit.Status &&
               result.Summary.Provenance == AudioEventDataAssetHashSearchAudit.Provenance &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed" &&
               result.Rows[1].AssetPath == "data/scripts/two.bin" &&
               result.Rows[1].HashWordsWithHits == 1 &&
               result.Rows[1].HashHitDetails.Contains("0x22344F6B:count=2:offsets=0x8,0xC", StringComparison.Ordinal),
            "Audio EVT complete-data-asset hash-universe fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-data-hash-universe-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "universe.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "universe-summary.tsv");
            AudioEventDataAssetHashSearchAudit.WriteReport(reportPath, result.Rows);
            AudioEventDataAssetHashSearchAudit.WriteSummary(summaryPath, result.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0].StartsWith("asset_path\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("data/scripts/two.bin", StringComparison.Ordinal) &&
                   summaryText.Contains("asset_root\tdata", StringComparison.Ordinal) &&
                   summaryText.Contains("aligned_occurrences\t3", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Audio EVT complete-data-asset hash-universe TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioNameStreamingSearch()
    {
        var names = new[] { "A", "AA", "AAA", "ABABA", "panna_goal", "goal", "not-present" };
        var cases = new List<byte[]> {
            Array.Empty<byte>(), Encoding.ASCII.GetBytes("AAAAABABABA panna_goalgoal"),
            Enumerable.Repeat((byte)'A', 80).ToArray(), Encoding.ASCII.GetBytes("panna_go"),
        };
        var random = new Random(17027);
        for (var n = 0; n < 24; n++)
        {
            var bytes = new byte[random.Next(1, 4096)];
            random.NextBytes(bytes);
            if (bytes.Length >= 16) Encoding.ASCII.GetBytes("panna_goal").CopyTo(bytes, bytes.Length - 10);
            cases.Add(bytes);
        }
        foreach (var data in cases)
        foreach (var chunk in new[] { 1, 2, 3, 7, 16, 1024, 1024 * 1024 })
        {
            using var source = new MemoryStream(data, writable: false);
            var actual = AudioEventNameAssetTextSearchAudit.Search(source, names, chunk);
            foreach (var row in actual)
            {
                var pattern = Encoding.ASCII.GetBytes(row.Name);
                var positions = new List<long>();
                for (var i = 0; i + pattern.Length <= data.Length; i++)
                    if (data.AsSpan(i, pattern.Length).SequenceEqual(pattern)) positions.Add(i);
                Assert(row.RawOccurrences == positions.Count && row.FirstOffsets.SequenceEqual(positions.Take(8)),
                    $"Exact streaming search lost/duplicated a boundary or overlapping match: chunk={chunk}, name={row.Name}.");
            }
        }
        // Also cover the production 1 MiB boundary and the eight-offset limit.
        var large = new byte[1024 * 1024 + 32];
        Encoding.ASCII.GetBytes("panna_goal").CopyTo(large, 1024 * 1024 - 4);
        using var largeSource = new MemoryStream(large, writable: false);
        var crossing = AudioEventNameAssetTextSearchAudit.Search(largeSource, new[] { "panna_goal" }).Single();
        Assert(crossing.RawOccurrences == 1 && crossing.FirstOffsets.Single() == 1024 * 1024 - 4,
            "Production chunk-boundary match changed.");
    }

    private static void TestAudioEventNameAssetTextSearchAudit()
    {
        var input = new[]
        {
            new AudioEventNameTextSearchInput(
                "data/audio/one.abk",
                32,
                true,
                new[]
                {
                    new AudioEventNameTextSearchPattern("3panna_goal", 0, Array.Empty<long>()),
                    new AudioEventNameTextSearchPattern("crowd_cheer", 0, Array.Empty<long>()),
                },
                string.Empty),
            new AudioEventNameTextSearchInput(
                "data/audio/two.ast",
                48,
                true,
                new[]
                {
                    new AudioEventNameTextSearchPattern("3panna_goal", 0, Array.Empty<long>()),
                    new AudioEventNameTextSearchPattern("crowd_cheer", 0, Array.Empty<long>()),
                },
                string.Empty),
        };
        var result = AudioEventNameAssetTextSearchAudit.Analyze(2, input, excludedSourceRows: 1);
        Assert(result.Rows.Count == 4 &&
               result.Summary.EventPath == "data/audio/eventspa.evt" &&
               result.Summary.CandidateCount == 2 &&
               result.Summary.ScannedAssetCount == 2 &&
               result.Summary.ValidAssetCount == 2 &&
               result.Summary.ScannedBytes == 80 &&
               result.Summary.ExcludedSourceRows == 1 &&
               result.Summary.AssetNameHitPairs == 0 &&
               result.Summary.AssetsWithNameHits == 0 &&
               result.Summary.RawOccurrences == 0 &&
               result.Summary.Status == AudioEventNameAssetTextSearchAudit.Status &&
               result.Summary.Provenance == AudioEventNameAssetTextSearchAudit.Provenance &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed" &&
               result.Rows[0].EventName == "3panna_goal" &&
               result.Rows[0].Status == "no-raw-name-hit",
            "Audio EVT exact-name asset-text search fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-name-text-search-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "names.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "names-summary.tsv");
            AudioEventNameAssetTextSearchAudit.WriteReport(reportPath, result.Rows);
            AudioEventNameAssetTextSearchAudit.WriteSummary(summaryPath, result.Summary);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryText = File.ReadAllText(summaryPath);
            Assert(reportLines.Length == 5 &&
                   reportLines[0].StartsWith("asset_path\t", StringComparison.Ordinal) &&
                   summaryText.Contains("asset_name_hit_pairs\t0", StringComparison.Ordinal) &&
                   summaryText.Contains("original_semantics\tnot-claimed", StringComparison.Ordinal),
                "Audio EVT exact-name asset-text search TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioBankBatchDecoder()
    {
        var pcmFixture = BuildPcmSchlFixture();
        var ramFixture = BuildRamAbkFixture();
        var catalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioBanks: new[]
            {
                new AudioBankAsset(
                    "bank.ast.abk",
                    100,
                    true,
                    1,
                    1,
                    1,
                    1,
                    false,
                    false,
                    0,
                    0,
                    1,
                    1,
                    new[] { new AudioAbkAstRange(1, false, 0, pcmFixture.Length) },
                    "bank.ast",
                    null),
                new AudioBankAsset(
                    "ram.abk",
                    ramFixture.Length,
                    true,
                    1,
                    1,
                    1,
                    0,
                    true,
                    true,
                    2,
                    1,
                    0,
                    0,
                    Array.Empty<AudioAbkAstRange>(),
                    null,
                    null)
                {
                    SoundEntries = new[]
                    {
                        new AudioAbkSoundEntry(
                            SoundIndex: 1,
                            ModuleIndex: 0,
                            PlayerIndex: 0,
                            SampleTableIndex: 0,
                            EntryIndex: 0,
                            SoundType: 0,
                            Priority: 255,
                            DataOffset: 1,
                            LoopOffset: 1,
                            IsDummy: false),
                    },
                },
            });

        var rows = AudioBankBatchDecoder.DecodeFirstAvailable(
            catalog,
            path => new MemoryStream(
                string.Equals(path, "ram.abk", StringComparison.Ordinal)
                    ? ramFixture
                    : pcmFixture,
                writable: false));
        Assert(rows.Count == 2 &&
               rows.Single(row => row.BankPath == "bank.ast.abk").DecodeValidated &&
               rows.Single(row => row.BankPath == "bank.ast.abk").SourceKind == "ast" &&
               rows.Single(row => row.BankPath == "bank.ast.abk").WaveBytes == 46 &&
               rows.Single(row => row.BankPath == "ram.abk").DecodeValidated &&
               rows.Single(row => row.BankPath == "ram.abk").SourceKind == "bnk" &&
               rows.Single(row => row.BankPath == "ram.abk").CandidateSoundCount == 1 &&
               rows.Single(row => row.BankPath == "ram.abk").WaveBytes == 174,
            "Audio bank batch decode evidence mismatch.");

        using var integratedMixer = new CatalogAudioMixer(
            catalog,
            path => new MemoryStream(
                string.Equals(path, "ram.abk", StringComparison.Ordinal)
                    ? ramFixture
                    : pcmFixture,
                writable: false),
            prepareObservedBankCandidates: true);
        integratedMixer.Initialize();
        Assert(integratedMixer.ObservedBankCandidateResults.Count == 2 &&
               integratedMixer.ObservedBankCandidateResults.All(row => row.DecodeValidated) &&
               integratedMixer.PreparedBankStreamCount == 2 &&
               integratedMixer.PreparedBankPcmBytes == 132,
            "Catalog audio mixer did not mount bounded ABKC candidates.");

        var channellessAstFixture = BuildPcmSchlFixture();
        channellessAstFixture[17] = 0;
        var channellessCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioBanks: new[]
            {
                new AudioBankAsset(
                    "channel-candidate.abk",
                    channellessAstFixture.Length,
                    true,
                    1,
                    1,
                    1,
                    1,
                    false,
                    false,
                    0,
                    0,
                    1,
                    0,
                    new[] { new AudioAbkAstRange(1, false, 0, channellessAstFixture.Length) },
                    "channel-candidate.ast",
                    null),
            });
        using var channellessMixer = new CatalogAudioMixer(
            channellessCatalog,
            path => new MemoryStream(channellessAstFixture, writable: false));
        channellessMixer.Initialize();
        Assert(channellessMixer.TryPrepareBankSound(
                   channellessCatalog.AudioBanks[0],
                   1,
                   loopSegment: false,
                   out var channelCandidateWave,
                   out var channelCandidateChannels) &&
               channelCandidateChannels == 1 &&
               channelCandidateWave.Length == 46,
            "Catalog audio mixer did not preserve the unique candidate channel layout.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-bank-batch-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "banks.tsv");
            AudioBankBatchDecoder.WriteReport(reportPath, rows);
            var lines = File.ReadAllLines(reportPath);
            Assert(lines.Length == 3 &&
                   lines[0].StartsWith("bank_path\t", StringComparison.Ordinal) &&
                   lines.Any(line => line.Contains("bank.ast.abk", StringComparison.Ordinal)) &&
                   lines.Any(line => line.Contains("ram.abk", StringComparison.Ordinal)),
                "Audio bank batch decode TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestXboxIma()
    {
        var mono = new byte[0x24];
        BinaryPrimitives.WriteInt16LittleEndian(mono.AsSpan(0, 2), 1000);
        mono[2] = 0;
        var monoInfo = XboxImaAudioDecoder.Validate(mono, 1, 36000, 65, new uint[] { 0 });
        Assert(monoInfo.Blocks == 1 && monoInfo.EncodedBytes == 0x24 && monoInfo.PcmBytes == 130,
            "Xbox IMA mono validation mismatch.");
        var monoWave = XboxImaAudioDecoder.DecodeWave(mono, 1, 36000, 65, new uint[] { 0 });
        Assert(monoWave.Length == 44 + 130 && monoWave.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
               BinaryPrimitives.ReadInt16LittleEndian(monoWave.AsSpan(44, 2)) == 1000 &&
               BinaryPrimitives.ReadInt16LittleEndian(monoWave.AsSpan(44 + 64 * 2, 2)) == 1000,
            "Xbox IMA mono decode mismatch.");

        var stereo = new byte[0x48];
        BinaryPrimitives.WriteInt16LittleEndian(stereo.AsSpan(0, 2), 100);
        BinaryPrimitives.WriteInt16LittleEndian(stereo.AsSpan(0x24, 2), -100);
        var stereoWave = XboxImaAudioDecoder.DecodeWave(
            stereo,
            2,
            24000,
            1,
            new uint[] { 0, 0x24 });
        Assert(stereoWave.Length == 48 &&
               BinaryPrimitives.ReadInt16LittleEndian(stereoWave.AsSpan(44, 2)) == 100 &&
               BinaryPrimitives.ReadInt16LittleEndian(stereoWave.AsSpan(46, 2)) == -100,
            "Xbox IMA stereo interleave mismatch.");
    }

    private static void TestAudioBackend()
    {
        var catalog = CreateAudioCatalogFixture();
        var mixer = new CatalogAudioMixer(catalog);
        mixer.Initialize();
        Assert(mixer.MountedStreamCount == 1 && mixer.RejectedStreamCount == 1,
            "Catalog audio mixer did not mount validated streams correctly.");
        var preparedMixer = new CatalogAudioMixer(
            catalog,
            _ => new MemoryStream(BuildPcmSchlFixture(), writable: false));
        preparedMixer.Initialize();
        Assert(preparedMixer.PreparedStreamCount == 1 && preparedMixer.PreparedPcmBytes == 2 &&
               preparedMixer.DecodeRejectedStreamCount == 0 &&
               preparedMixer.TryGetPreparedWave("valid.ast", out var preparedWave) &&
               preparedWave.AsSpan(0, 4).SequenceEqual("RIFF"u8),
            "Catalog audio mixer did not pre-decode a validated stream.");
        var namedAudioFixture = BuildPcmSchlFixture();
        using var namedAudioInfoStream = new MemoryStream(namedAudioFixture, writable: false);
        var namedAudioInfo = EaSchlAudioDecoder.Inspect(namedAudioInfoStream);
        var namedEventCandidate = new AudioEventNamedHeaderCandidate(
            "goal",
            16,
            0,
            new AudioOffsetRow(1, 0, namedAudioFixture.Length, 0),
            new AudioOffsetRow(1, 0, namedAudioFixture.Length, 0),
            1);
        var monoLayoutEventCandidate = new AudioEventCandidateAsset(
            "event.evt",
            "header.big",
            "data.big",
            namedEventCandidate with { EventName = "mono-layout-candidate" },
            namedAudioInfo with { Channels = 0 },
            null)
        {
            PayloadChannelLayoutCandidate = 1,
            PayloadChannelLayoutFrames = 1,
        };
        var namedEventCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioEventCandidates: new[]
            {
                new AudioEventCandidateAsset(
                    "event.evt",
                    "header.big",
                    "data.big",
                    namedEventCandidate,
                    namedAudioInfo,
                    null),
                monoLayoutEventCandidate,
                monoLayoutEventCandidate with
                {
                    Candidate = monoLayoutEventCandidate.Candidate with { EventName = "panna" },
                },
            });
        var namedEventMixer = new CatalogAudioMixer(
            namedEventCatalog,
            _ => new MemoryStream(namedAudioFixture, writable: false));
        namedEventMixer.Initialize();
        Assert(namedEventMixer.PreparedNamedEventCandidateCount == 3 &&
               namedEventMixer.PreparedNamedEventCandidatePcmBytes == 6 &&
               namedEventMixer.NamedEventCandidateDecodeRejectedCount == 0,
            "Catalog audio mixer did not pre-prepare the validated named event candidate.");
        namedEventMixer.OnGameplayEvent("GOAL_HOME");
        var namedWaveAvailable = namedEventMixer.TryPrepareNamedEvent("goal", out var namedWave);
        Assert(namedEventMixer.PreparedGameplayAudioCount == 1 &&
               namedEventMixer.LastGameplayAudioName == "goal" &&
               namedEventMixer.LastGameplayAudioRoute == "prepared-clean-room-candidate" &&
               namedWaveAvailable &&
               namedWave.AsSpan(0, 4).SequenceEqual("RIFF"u8),
            "Named gameplay audio candidate was not selected and prepared.");
        namedEventMixer.OnGameplayEvent("GOAL_AWAY");
        Assert(namedEventMixer.PreparedGameplayAudioCount == 2 &&
               namedEventMixer.LastGameplayAudioName == "goal" &&
               namedEventMixer.GameplayAudioUnmappedEventCount == 0 &&
               namedEventMixer.LastGameplayAudioRoute == "prepared-clean-room-candidate",
            "Clean-room away-goal audio symmetry did not reuse the validated goal candidate.");
        namedEventMixer.OnGameplayEvent("TRICK");
        Assert(namedEventMixer.PreparedGameplayAudioCount == 3 &&
               namedEventMixer.LastGameplayAudioName == "panna" &&
               namedEventMixer.GameplayAudioUnmappedEventCount == 0 &&
               namedEventMixer.LastGameplayAudioRoute == "prepared-clean-room-candidate",
            "Clean-room trick audio did not use the explicitly authored panna candidate route.");
        namedEventMixer.OnGameplayEvent("PANNA");
        Assert(namedEventMixer.PreparedGameplayAudioCount == 4 &&
               namedEventMixer.LastGameplayAudioName == "panna" &&
               namedEventMixer.GameplayAudioUnmappedEventCount == 0 &&
               namedEventMixer.LastGameplayAudioRoute == "prepared-clean-room-candidate",
            "Explicit authored panna audio did not reuse the candidate route.");
        var expectedGameplayPcm = Enumerable.Repeat(namedWave.AsSpan(44).ToArray(), 4)
            .SelectMany(bytes => bytes)
            .ToArray();
        Assert(namedEventMixer.GameplayAudioPcmBytes == expectedGameplayPcm.Length &&
               namedEventMixer.GameplayAudioPcmSha256 ==
               Convert.ToHexString(SHA256.HashData(expectedGameplayPcm)),
            $"Catalog audio mixer did not retain the deterministic authored gameplay PCM digest (bytes={namedEventMixer.GameplayAudioPcmBytes}/{expectedGameplayPcm.Length}, sha={namedEventMixer.GameplayAudioPcmSha256}).");
        Assert(namedEventMixer.AuthoredAudioAssetManifest ==
               CleanRoomAudioAssetManifest.Default.ManifestId,
            "Catalog audio mixer did not expose the authored audio asset manifest.");
        var bankCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioBanks: new[]
            {
                new AudioBankAsset(
                    "bank.abk",
                    100,
                    true,
                    1,
                    1,
                    1,
                    1,
                    false,
                    false,
                    0,
                    0,
                    1,
                    1,
                    new[] { new AudioAbkAstRange(1, false, 0, BuildPcmSchlFixture().Length) },
                    "bank.ast",
                    null),
            });
        var bankMixer = new CatalogAudioMixer(
            bankCatalog,
            _ => new MemoryStream(BuildPcmSchlFixture(), writable: false));
        bankMixer.Initialize();
        Assert(bankMixer.TryPrepareBankSound(
                   bankCatalog.AudioBanks[0],
                   1,
                   false,
                   out var preparedBankWave) &&
               bankMixer.PreparedBankStreamCount == 1 &&
               bankMixer.PreparedBankPcmBytes == 2 &&
               preparedBankWave.AsSpan(0, 4).SequenceEqual("RIFF"u8),
            "Catalog audio mixer did not lazily prepare an ABKC-referenced AST range.");
        var ramBankFixture = BuildRamAbkFixture();
        var ramBankCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioBanks: new[]
            {
                new AudioBankAsset(
                    "ram.abk",
                    ramBankFixture.Length,
                    true,
                    1,
                    1,
                    1,
                    0,
                    true,
                    true,
                    2,
                    1,
                    0,
                    0,
                    Array.Empty<AudioAbkAstRange>(),
                    null,
                    null),
            });
        var ramBankMixer = new CatalogAudioMixer(
            ramBankCatalog,
            _ => new MemoryStream(ramBankFixture, writable: false));
        ramBankMixer.Initialize();
        Assert(ramBankMixer.TryPrepareBankRamSound(
                   ramBankCatalog.AudioBanks[0],
                   1,
                   out var preparedRamWave) &&
               ramBankMixer.PreparedBankStreamCount == 1 &&
               ramBankMixer.PreparedBankPcmBytes == 130 &&
               BinaryPrimitives.ReadInt16LittleEndian(preparedRamWave.AsSpan(44, 2)) == 1000,
            "Catalog audio mixer did not decode an embedded Xbox IMA RAM sound.");
        var ramInspection = AudioAbkReader.Inspect(ramBankFixture);
        var ramBnk = AudioAbkReader.InspectEmbeddedBnk(ramInspection, ramBankFixture);
        Assert(ramBnk is not null, "RAM fixture BNK should parse.");
        var ramValidations = AudioAbkReader.ValidateEmbeddedBnkStreams(ramBnk!, ramBankFixture);
        Assert(ramValidations.Count == 1 && ramValidations[0].Valid,
            "RAM fixture Xbox IMA data should validate.");
        var runtime = new GameRuntime(
            catalog,
            new HeadlessRenderer(),
            mixer,
            new HeadlessInputSource());
        runtime.Start();
        mixer.OnGameplayEvent("UNKNOWN");
        Assert(mixer.GameplayEventCount == 1 && mixer.LastGameplayEvent == "UNKNOWN" &&
               mixer.GameplayAudioUnmappedEventCount == 1 &&
               mixer.LastGameplayAudioRoute == "unmapped-clean-room-event",
            "Catalog audio mixer did not accept gameplay events.");
        var scriptedRuntime = new GameRuntime(
            catalog,
            new HeadlessRenderer(),
            mixer,
            new ScriptedInputSource(
                new MatchInput(1, 0, false, false, false),
                new MatchInput(0, 0, false, false, true),
                new MatchInput(0, 0, false, false, false),
                new MatchInput(0, 0, false, false, true)));
        scriptedRuntime.Start();
        scriptedRuntime.Tick(0.25);
        scriptedRuntime.Tick(1.0 / 60.0);
        Assert(mixer.GameplayEventCount == 3 && mixer.LastGameplayEvent == "TRICK" &&
               mixer.LastGameplayAudioRoute == "candidate-unavailable",
            "GameRuntime did not route the gameplay action to audio.");
        scriptedRuntime.Tick(1.0 / 60.0);
        Assert(mixer.GameplayEventCount == 3,
            "GameRuntime emitted an audio event without a new gameplay action.");
        scriptedRuntime.Tick(1.0 / 60.0);
        scriptedRuntime.Tick(1.0 / 60.0);
        Assert(mixer.GameplayEventCount == 4 && mixer.LastGameplayEvent == "TRICK" &&
               mixer.LastGameplayAudioRoute == "candidate-unavailable",
            "GameRuntime did not route a repeated gameplay action.");
        Assert(scriptedRuntime.InputHistory.Count == 5 &&
               scriptedRuntime.InputHistory[0].Frame == 0 &&
               scriptedRuntime.InputHistory[1].Input.Trick &&
               scriptedRuntime.InputHistory[4].Frame == 4,
            "GameRuntime did not retain the authored input samples in frame order.");

        var multiEventInputs = Enumerable.Repeat(
                new MatchInput(1, 0, false, false, false),
                30)
            .Append(new MatchInput(0, 0, false, false, false, true))
            .ToArray();
        var multiEventMixer = new CatalogAudioMixer(
            namedEventCatalog,
            _ => new MemoryStream(namedAudioFixture, writable: false));
        var multiEventRuntime = new GameRuntime(
            namedEventCatalog,
            new HeadlessRenderer(),
            multiEventMixer,
            new ScriptedInputSource(multiEventInputs),
            endCondition: CleanRoomMatchEndCondition.GoalsOrPanna,
            targetGoals: 9,
            targetPannas: 1);
        multiEventRuntime.Start();
        for (var frame = 0; frame < multiEventInputs.Length; frame++)
        {
            multiEventRuntime.Tick(1.0 / 60.0);
        }
        Assert(multiEventRuntime.Match.LastAction == "FULL_TIME" &&
               multiEventMixer.GameplayEventCount == 3 &&
               multiEventMixer.PreparedGameplayAudioCount == 1 &&
               multiEventMixer.GameplayAudioUnmappedEventCount == 0 &&
               multiEventMixer.LastGameplayAudioRoute == "candidate-unavailable",
            $"GameRuntime did not drain multiple authored events emitted in one tick to audio (events={multiEventMixer.GameplayEventCount}/3, prepared={multiEventMixer.PreparedGameplayAudioCount}/1, unmapped={multiEventMixer.GameplayAudioUnmappedEventCount}/0, route={multiEventMixer.LastGameplayAudioRoute}, last={multiEventMixer.LastGameplayEvent}, action={multiEventRuntime.Match.LastAction}).");

        var authoredRouteNames = new[]
        {
            "intro",
            "attackgen",
            "panna",
            "attackgoal",
            "goal",
            "gb_goal",
            "tackle",
            "outro",
        };
        Assert(CleanRoomAudioCuePolicy.Routes.Count == 15 &&
               CleanRoomAudioCuePolicy.TryResolve("KICKOFF", out var kickoffCue) &&
               kickoffCue == "intro" &&
               CleanRoomAudioCuePolicy.TryResolve("TACKLE", out var tackleCue) &&
               tackleCue == "tackle" &&
               CleanRoomAudioCuePolicy.TryResolve("FULL_TIME", out var fullTimeCue) &&
               fullTimeCue == "outro" &&
               !CleanRoomAudioCuePolicy.TryResolve("UNKNOWN", out _),
            "Authored clean-room audio cue policy manifest mismatch.");
        var authoredRouteCandidates = authoredRouteNames
            .Select((eventName, index) => new AudioEventCandidateAsset(
                "event.evt",
                "header.big",
                "data.big",
                namedEventCandidate with
                {
                    EventName = eventName,
                    HeaderRowIndex = 100 + index,
                },
                namedAudioInfo,
                null))
            .ToArray();
        var authoredRouteCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioEventCandidates: authoredRouteCandidates);
        var authoredRouteMixer = new CatalogAudioMixer(
            authoredRouteCatalog,
            _ => new MemoryStream(namedAudioFixture, writable: false));
        authoredRouteMixer.Initialize();
        var authoredRouteActions = new[]
        {
            "KICKOFF",
            "PASS",
            "PASS_COMPLETE",
            "TRICK",
            "PANNA",
            "JUGGLE",
            "SHOT",
            "SHOT_AWAY",
            "GOAL_HOME",
            "GOAL_AWAY",
            "GAMEBREAKER",
            "STEAL",
            "RESTART",
            "FULL_TIME",
        };
        foreach (var action in authoredRouteActions)
        {
            authoredRouteMixer.OnGameplayEvent(action);
        }
        Assert(authoredRouteMixer.PreparedNamedEventCandidateCount == authoredRouteNames.Length &&
               authoredRouteMixer.PreparedGameplayAudioCount == authoredRouteActions.Length &&
               authoredRouteMixer.GameplayAudioUnmappedEventCount == 0 &&
               authoredRouteMixer.GameplayAudioDecodeRejectedCount == 0 &&
               authoredRouteMixer.LastGameplayAudioRoute == "prepared-clean-room-candidate" &&
               authoredRouteMixer.GameplayAudioRoutes.Count == authoredRouteActions.Length &&
               authoredRouteMixer.GameplayAudioRoutes[0].Sequence == 1 &&
               authoredRouteMixer.GameplayAudioRoutes[0].Action == "KICKOFF" &&
               authoredRouteMixer.GameplayAudioRoutes[0].CandidateName == "intro" &&
               authoredRouteMixer.GameplayAudioRoutes[^1].Action == "FULL_TIME" &&
               authoredRouteMixer.GameplayAudioRoutes[^1].CandidateName == "outro",
            "Authored clean-room audio route policy did not cover all supported gameplay actions.");
    }

    private static void TestGameplayAudioTrace()
    {
        var routes = new CleanRoomGameplayAudioRoute[]
        {
            new(1, "KICKOFF", "intro", "prepared-clean-room-candidate"),
            new(2, "UNKNOWN", string.Empty, "unmapped-clean-room-event"),
        };
        var lines = GameplayAudioTraceSerializer.BuildLines(routes);
        Assert(lines.Count == 3 &&
               lines[0] == GameplayAudioTraceSerializer.Header &&
               lines[1] == "1\tKICKOFF\tintro\tprepared-clean-room-candidate\t0\t0\t" &&
               lines[2] == "2\tUNKNOWN\t\tunmapped-clean-room-event\t0\t0\t" &&
               GameplayAudioTraceSerializer.FindFirstDifference(lines, lines) == -1,
            "Gameplay audio trace serializer did not preserve authored route fields.");

        var altered = lines.ToArray();
        altered[2] = altered[2].Replace("unmapped-clean-room-event", "candidate-unavailable", StringComparison.Ordinal);
        Assert(GameplayAudioTraceSerializer.FindFirstDifference(lines, altered) == 2,
            "Gameplay audio trace serializer did not identify the first route difference.");
    }

    private static void TestGameplayAudioCoverageAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-gameplay-audio-coverage-test-");
        try
        {
            var defaultScenarios = GameplayAudioCoverageAudit.DefaultScenarios(temporaryDirectory.FullName);
            Assert(defaultScenarios.Count == 10 &&
                   defaultScenarios.Any(scenario =>
                       scenario.Name == "aimed" &&
                       scenario.TracePath.EndsWith("gameplay-authored-aimed-audio-trace.tsv", StringComparison.Ordinal) &&
                       scenario.ReplayPath.EndsWith("gameplay-authored-aimed-audio-replayed.tsv", StringComparison.Ordinal)) &&
                   defaultScenarios.Any(scenario =>
                       scenario.Name == "tackle" &&
                       scenario.TracePath.EndsWith("gameplay-authored-tackle-audio-trace.tsv", StringComparison.Ordinal)),
                "Gameplay audio coverage defaults did not include the authored aimed scenario.");

            var routes = CleanRoomAudioCuePolicy.Routes
                .Select((route, index) => new CleanRoomGameplayAudioRoute(
                    index,
                    route.Action,
                    route.CandidateName,
                    "played-clean-room-candidate"))
                .ToArray();
            var tracePath = Path.Combine(temporaryDirectory.FullName, "trace.tsv");
            var replayPath = Path.Combine(temporaryDirectory.FullName, "replayed.tsv");
            File.WriteAllLines(tracePath, GameplayAudioTraceSerializer.BuildLines(routes));
            File.WriteAllLines(replayPath, GameplayAudioTraceSerializer.BuildLines(routes));

            var audit = GameplayAudioCoverageAudit.Audit(new[]
            {
                new GameplayAudioCoverageScenarioSpec("fixture", tracePath, replayPath),
            });
            Assert(audit.Summary.Status == "verified-authored" &&
                   audit.Summary.ScenarioCount == 1 &&
                   audit.Summary.VerifiedScenarioCount == 1 &&
                   audit.Summary.TotalEvents == CleanRoomAudioCuePolicy.Routes.Count &&
                   audit.Summary.TotalPrepared == CleanRoomAudioCuePolicy.Routes.Count &&
                   audit.Summary.TotalPlayed == CleanRoomAudioCuePolicy.Routes.Count &&
                   audit.Summary.TotalUnmapped == 0 &&
                   audit.Summary.MissingPolicyActions == string.Empty &&
                   audit.Summary.UniqueCandidateCount == 8,
                "Gameplay audio coverage audit did not verify all authored policy routes.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "coverage.tsv");
            GameplayAudioCoverageAudit.Write(reportPath, audit);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0] == GameplayAudioCoverageAudit.Header &&
                   reportLines[1].StartsWith("scenario\tfixture\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("verified-authored", StringComparison.Ordinal) &&
                   reportLines[2].Contains("not-claimed", StringComparison.Ordinal),
                "Gameplay audio coverage report TSV shape or provenance mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestGameplayCoverageAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-gameplay-coverage-test-");
        try
        {
            var defaultScenarios = GameplayCoverageAudit.DefaultScenarios(temporaryDirectory.FullName);
            Assert(defaultScenarios.Count == 10 &&
                   defaultScenarios.Any(scenario =>
                       scenario.Name == "gamebreaker" &&
                       scenario.RequiredActions.Contains("GAMEBREAKER") &&
                       scenario.TracePath.EndsWith("gameplay-clean-room-gamebreaker-trace.tsv", StringComparison.Ordinal)) &&
                   defaultScenarios.Any(scenario =>
                       scenario.Name == "tackle" &&
                       scenario.RequiredActions.Contains("TACKLE") &&
                       scenario.TracePath.EndsWith("gameplay-authored-tackle-match-trace.tsv", StringComparison.Ordinal)),
                "Gameplay coverage defaults did not include the authored gamebreaker scenario.");

            var tracePath = Path.Combine(temporaryDirectory.FullName, "trace.tsv");
            var replayPath = Path.Combine(temporaryDirectory.FullName, "replayed.tsv");
            var events = new[]
            {
                new CleanRoomMatchEvent(0, 0, "KICKOFF", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(1, 5, "SHOT", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(2, 9, "GOAL_HOME", 1, 0, BallOwner.None),
            };
            File.WriteAllLines(tracePath, MatchTraceSerializer.BuildLines(events));
            File.WriteAllLines(replayPath, MatchTraceSerializer.BuildLines(events));

            var audit = GameplayCoverageAudit.Audit(new[]
            {
                new GameplayCoverageScenarioSpec(
                    "fixture",
                    tracePath,
                    replayPath,
                    new[] { "KICKOFF", "SHOT", "GOAL_HOME" }),
            });
            Assert(audit.Summary.Status == "verified-authored" &&
                   audit.Summary.ScenarioCount == 1 &&
                   audit.Summary.VerifiedScenarioCount == 1 &&
                   audit.Summary.TotalEvents == 3 &&
                   audit.Summary.MissingActions == string.Empty &&
                   audit.Scenarios[0].ReplayMatched &&
                   audit.Scenarios[0].RequiredActionsPresent &&
                   audit.Scenarios[0].Actions == "GOAL_HOME,KICKOFF,SHOT",
                "Gameplay coverage audit did not verify the authored trace/replay fixture.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "coverage.tsv");
            GameplayCoverageAudit.Write(reportPath, audit);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0] == GameplayCoverageAudit.Header &&
                   reportLines[1].StartsWith("scenario\tfixture\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("verified-authored", StringComparison.Ordinal) &&
                   reportLines[2].Contains("not-claimed", StringComparison.Ordinal),
                "Gameplay coverage report TSV shape or provenance mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestCleanRoomGameplayInvariantAudit()
    {
        var inputs = Enumerable.Repeat(new MatchInput(), 4).ToArray();
        var audit = CleanRoomGameplayInvariantAudit.Audit(new[]
        {
            new CleanRoomGameplayInvariantScenarioSpec(
                "fixture",
                4,
                () => new MatchInputScriptSource(new MatchInputScript(inputs)),
                false,
                false,
                300.0,
                CleanRoomMatchEndCondition.Timed,
                0,
                0,
                false,
                new[] { "KICKOFF" }),
        });
        var row = audit.Scenarios.Single();
        Assert(audit.Summary.Status == "verified-authored" &&
               audit.Summary.ScenarioCount == 1 &&
               audit.Summary.VerifiedScenarioCount == 1 &&
               audit.Summary.TotalFrames == 4 &&
               audit.Summary.TotalEvents == 1 &&
               row.FiniteState &&
               row.PositionsBounded &&
               row.BallHeightBounded &&
               row.ClockMonotonic &&
               row.ScoresMonotonic &&
               row.EventSequenceMonotonic &&
               row.EndConditionSatisfied &&
               row.RequiredActionsPresent &&
               row.Status == "verified-authored",
            "Clean-room gameplay invariant audit did not verify the fixture.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-clean-room-gameplay-invariant-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "invariants.tsv");
            CleanRoomGameplayInvariantAudit.Write(reportPath, audit);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0] == CleanRoomGameplayInvariantAudit.Header &&
                   reportLines[1].StartsWith("scenario\tfixture\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("verified-authored", StringComparison.Ordinal) &&
                   reportLines[2].Contains("not-claimed", StringComparison.Ordinal),
                "Clean-room gameplay invariant report TSV shape or provenance mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestCleanRoomIntegratedCoverageAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-clean-room-integrated-coverage-test-");
        try
        {
            var defaults = CleanRoomIntegratedCoverageAudit.DefaultScenarios(temporaryDirectory.FullName);
            Assert(defaults.Count == 10 &&
                   defaults.Any(scenario =>
                       scenario.Name == "juggle" &&
                       scenario.RequiredActions.Contains("JUGGLE") &&
                       scenario.IntegrationReportPath.EndsWith(
                           "gameplay-authored-juggle-integration.tsv",
                           StringComparison.Ordinal)),
                "Integrated authored coverage defaults did not include the expected scenario matrix.");

            var integrationPath = Path.Combine(temporaryDirectory.FullName, "integration.tsv");
            CleanRoomIntegrationReport.Write(
                integrationPath,
                new CleanRoomIntegrationReportData(
                    CandidateMaterialValidated: false,
                    CandidateHomeEnabled: false,
                    CandidateAwayEnabled: false,
                    RenderedFrames: 4,
                    RenderChecksum: "0123456789ABCDEF",
                    DistinctRenderChecksums: 4,
                    AuthoredCharacterAssetFrames: 4,
                    AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
                    AuthoredSceneAssetFrames: 4,
                    AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
                    AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
                    InputSamples: 4,
                    AnimationSamples: 8,
                    MatchEvents: 3,
                    AudioEvents: 2,
                    AudioPrepared: 2,
                    AudioPlayed: 2,
                    AudioUnmapped: 0,
                    GameplayAudioPcmBytes: 64,
                    GameplayAudioPcmSha256: new string('A', 64),
                    AuthoredAudioAssetManifest: CleanRoomAudioAssetManifest.Default.ManifestId,
                    MatchTraceWritten: true,
                    InputTraceWritten: true,
                    AnimationTraceWritten: true,
                    AudioTraceWritten: true));

            var matchPath = Path.Combine(temporaryDirectory.FullName, "match.tsv");
            var matchReplayPath = Path.Combine(temporaryDirectory.FullName, "match-replayed.tsv");
            var matchLines = MatchTraceSerializer.BuildLines(new[]
            {
                new CleanRoomMatchEvent(0, 0, "KICKOFF", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(1, 1, "SHOT", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(2, 2, "GOAL_HOME", 1, 0, BallOwner.None),
            });
            File.WriteAllLines(matchPath, matchLines);
            File.WriteAllLines(matchReplayPath, matchLines);

            var inputPath = Path.Combine(temporaryDirectory.FullName, "input.tsv");
            var inputReplayPath = Path.Combine(temporaryDirectory.FullName, "input-replayed.tsv");
            var inputLines = new[]
            {
                CleanRoomInputTraceSerializer.Header,
                "0\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "1\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "2\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "3\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
            };
            File.WriteAllLines(inputPath, inputLines);
            File.WriteAllLines(inputReplayPath, inputLines);

            var animationPath = Path.Combine(temporaryDirectory.FullName, "animation.tsv");
            var animationReplayPath = Path.Combine(temporaryDirectory.FullName, "animation-replayed.tsv");
            var animationLines = new[]
            {
                CleanRoomAuthoredAnimationTraceSerializer.Header,
            }.Concat(Enumerable.Repeat(
                "0\tNone\tKICKOFF\tFalse\t0\t0\t0\t0\t0\t0\t0\t0",
                8)).ToArray();
            File.WriteAllLines(animationPath, animationLines);
            File.WriteAllLines(animationReplayPath, animationLines);

            var audioPath = Path.Combine(temporaryDirectory.FullName, "audio.tsv");
            var audioReplayPath = Path.Combine(temporaryDirectory.FullName, "audio-replayed.tsv");
            var audioLines = GameplayAudioTraceSerializer.BuildLines(new[]
            {
                new CleanRoomGameplayAudioRoute(0, "KICKOFF", "intro", "played-clean-room-candidate"),
                new CleanRoomGameplayAudioRoute(1, "GOAL_HOME", "goal", "played-clean-room-candidate"),
            });
            File.WriteAllLines(audioPath, audioLines);
            File.WriteAllLines(audioReplayPath, audioLines);

            var frameOutputPath = Path.Combine(temporaryDirectory.FullName, "frame.ppm");
            var frameOutput = new SoftwareFrameBuffer(960, 540);
            frameOutput.Clear(new RgbColor(18, 24, 38));
            frameOutput.DrawCircle(480, 270, 12, new RgbColor(231, 73, 66));
            frameOutput.SavePpm(frameOutputPath);

            var audit = CleanRoomIntegratedCoverageAudit.Audit(new[]
            {
                new CleanRoomIntegratedCoverageScenarioSpec(
                    "fixture",
                    integrationPath,
                    matchPath,
                    matchReplayPath,
                    inputPath,
                    inputReplayPath,
                    animationPath,
                    animationReplayPath,
                    audioPath,
                    audioReplayPath,
                    new[] { "KICKOFF", "SHOT", "GOAL_HOME" },
                    frameOutputPath),
            });
            Assert(audit.Summary.Status == "verified-authored" &&
                   audit.Summary.ScenarioCount == 1 &&
                   audit.Summary.VerifiedScenarioCount == 1 &&
                   audit.Summary.TotalRenderedFrames == 4 &&
                   audit.Summary.TotalAudioEvents == 2 &&
                   audit.Summary.TotalAnimationSamples == 8 &&
                   audit.Scenarios[0].IntegrationValid &&
                   audit.Scenarios[0].TraceFilesValid &&
                   audit.Scenarios[0].ReplayMatched &&
                   audit.Scenarios[0].RequiredActionsPresent &&
                   audit.Scenarios[0].FrameOutputValid &&
                   audit.Scenarios[0].FrameVisiblePixels > 0 &&
                   audit.Scenarios[0].FrameOutputSha256.Length == 64,
                "Integrated authored coverage audit did not verify the cross-system fixture.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "integrated-coverage.tsv");
            CleanRoomIntegratedCoverageAudit.Write(reportPath, audit);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 3 &&
                   reportLines[0] == CleanRoomIntegratedCoverageAudit.Header &&
                   reportLines[1].StartsWith("scenario\tfixture\t", StringComparison.Ordinal) &&
                   reportLines[2].Contains("verified-authored", StringComparison.Ordinal) &&
                   reportLines[2].Contains("not-claimed", StringComparison.Ordinal),
                "Integrated authored coverage report TSV shape or provenance mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestCandidateIntegratedRuntimeAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-candidate-integrated-runtime-test-");
        try
        {
            var scenario = CandidateIntegratedRuntimeAudit.DefaultScenario(temporaryDirectory.FullName);
            Directory.CreateDirectory(Path.GetDirectoryName(scenario.IntegrationReportPath)!);
            var integrationData = new CleanRoomIntegrationReportData(
                CandidateMaterialValidated: true,
                CandidateHomeEnabled: true,
                CandidateAwayEnabled: true,
                RenderedFrames: 4,
                RenderChecksum: "0123456789ABCDEF",
                DistinctRenderChecksums: 4,
                AuthoredCharacterAssetFrames: 0,
                AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
                AuthoredSceneAssetFrames: 4,
                AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
                AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
                InputSamples: 4,
                AnimationSamples: 8,
                MatchEvents: 3,
                AudioEvents: 2,
                AudioPrepared: 2,
                AudioPlayed: 2,
                AudioUnmapped: 0,
                GameplayAudioPcmBytes: 64,
                GameplayAudioPcmSha256: new string('A', 64),
                AuthoredAudioAssetManifest: CleanRoomAudioAssetManifest.Default.ManifestId,
                MatchTraceWritten: true,
                InputTraceWritten: true,
                AnimationTraceWritten: true,
                AudioTraceWritten: true);
            CleanRoomIntegrationReport.Write(scenario.IntegrationReportPath, integrationData);
            CleanRoomIntegrationReport.Write(scenario.ReplayIntegrationReportPath, integrationData);
            File.WriteAllLines(
                scenario.CandidateAssetProvenanceSummaryPath,
                new[]
                {
                    "field\tvalue",
                    "candidate_count\t1",
                    "source_chain_verified\t1",
                    "decoded_pixel_matches\t1",
                    "candidate_structural_verified\t1",
                    "model_object_entries_read\t1",
                    "model_companion_entries_read\t1",
                    "candidate_bounds_valid\t1",
                    "candidate_geometry_finite_and_indexed\t1",
                    "candidate_primitive_shape_state_rows\t1",
                    "candidate_geometry_streams_complete\t1",
                    "candidate_shape_state_streams_complete\t1",
                    "status\tverified-candidate-source-chain",
                    $"candidate_provenance\t{EaglCandidateAssetProvenanceAudit.CandidateProvenance}",
                    "original_semantics\tnot-claimed",
                });

            var matchLines = MatchTraceSerializer.BuildLines(new[]
            {
                new CleanRoomMatchEvent(0, 0, "KICKOFF", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(1, 1, "SHOT_AWAY", 0, 0, BallOwner.None),
                new CleanRoomMatchEvent(2, 2, "GOAL_AWAY", 0, 0, BallOwner.None),
            });
            File.WriteAllLines(scenario.MatchTracePath, matchLines);
            File.WriteAllLines(scenario.MatchReplayPath, matchLines);

            var inputLines = new[]
            {
                CleanRoomInputTraceSerializer.Header,
                "0\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "1\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "2\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
                "3\t0\t0\tFalse\tFalse\tFalse\tFalse\tFalse\tFalse",
            };
            File.WriteAllLines(scenario.InputTracePath, inputLines);
            File.WriteAllLines(scenario.InputReplayPath, inputLines);

            var animationLines = new[]
            {
                CleanRoomAuthoredAnimationTraceSerializer.Header,
            }.Concat(Enumerable.Repeat(
                "0\tNone\tKICKOFF\tFalse\t0\t0\t0\t0\t0\t0\t0\t0",
                8)).ToArray();
            File.WriteAllLines(scenario.AnimationTracePath, animationLines);
            File.WriteAllLines(scenario.AnimationReplayPath, animationLines);

            var audioLines = GameplayAudioTraceSerializer.BuildLines(new[]
            {
                new CleanRoomGameplayAudioRoute(0, "KICKOFF", "intro", "played-clean-room-candidate"),
                new CleanRoomGameplayAudioRoute(1, "GOAL_AWAY", "goal", "played-clean-room-candidate"),
            });
            File.WriteAllLines(scenario.AudioTracePath, audioLines);
            File.WriteAllLines(scenario.AudioReplayPath, audioLines);

            var frameOutput = new SoftwareFrameBuffer(32, 24);
            frameOutput.Clear(new RgbColor(18, 24, 38));
            frameOutput.DrawCircle(16, 12, 4, new RgbColor(231, 73, 66));
            frameOutput.SavePpm(scenario.FrameOutputPath);

            var audit = CandidateIntegratedRuntimeAudit.Audit(scenario);
            Assert(audit.Status == "verified-candidate" &&
                   audit.CandidateFlagsValid &&
                   audit.ProvenanceValid &&
                   audit.CandidateAssetProvenanceValid &&
                   audit.CandidateMeshStateCrosscheckValid &&
                   audit.CandidateAssetCount == 1 &&
                   audit.CandidateGeometryStreamsComplete &&
                   audit.CandidateShapeStateStreamsComplete &&
                   audit.IntegrationReportsMatch &&
                   audit.TraceFilesValid &&
                   audit.ReplayMatched &&
                   audit.RequiredActionsPresent &&
                   audit.FrameOutputValid &&
                   audit.FrameVisiblePixels > 0 &&
                   audit.OriginalSemantics == "not-claimed",
                "Candidate integrated runtime audit did not verify the fixture.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "candidate-audit.tsv");
            CandidateIntegratedRuntimeAudit.Write(reportPath, audit);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == 2 &&
                   reportLines[0] == CandidateIntegratedRuntimeAudit.Header &&
                   reportLines[1].Contains("verified-candidate", StringComparison.Ordinal) &&
                   reportLines[1].Contains("not-claimed", StringComparison.Ordinal),
                "Candidate integrated runtime audit report TSV shape or provenance mismatch.");

            var alteredPath = Path.Combine(temporaryDirectory.FullName, "altered-integration.tsv");
            var alteredLines = File.ReadAllLines(scenario.IntegrationReportPath)
                .Select(line => line == "candidate_home_enabled\tTrue"
                    ? "candidate_home_enabled\tFalse"
                    : line)
                .ToArray();
            File.WriteAllLines(alteredPath, alteredLines);
            var invalidAudit = CandidateIntegratedRuntimeAudit.Audit(
                scenario with { IntegrationReportPath = alteredPath });
            Assert(invalidAudit.Status == "incomplete" && !invalidAudit.CandidateFlagsValid,
                "Candidate integrated runtime audit did not reject a disabled candidate flag.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventIndexEvidenceReport()
    {
        var data = new AudioEventIndexEvidenceReportData(
            EventPath: "data/audio/eventspa.evt",
            StringTableOffset: 0x2090,
            CandidatePairs: 1061,
            StringHashFamilyCandidates: 17,
            DistinctStringOffsets: 17,
            DistinctFollowingWords: 17,
            AbkBanksScanned: 16,
            AbkAlignedOccurrences: 0,
            AbkValuesWithHits: 0,
            HeaderAlignedOccurrences: 0,
            HeaderValuesWithHits: 0,
            DataSearchRequested: true,
            DataAlignedOccurrences: 0,
            DataValuesWithHits: 0,
            CandidateNames: "goal,intro,tackle",
            Status: "no-direct-selection-binding",
            DirectSelectionBinding: "not-proven",
            OriginalSemantics: "not-claimed");
        var lines = AudioEventIndexEvidenceReport.BuildLines(data);
        Assert(lines[0] == AudioEventIndexEvidenceReport.Header &&
               lines.Contains("string_table_offset\t0x2090") &&
               lines.Contains("candidate_pairs\t1061") &&
               lines.Contains("abk_aligned_occurrences\t0") &&
               lines.Contains("pcpahdr_big_values_with_hits\t0") &&
               lines.Contains("pcpadat_big_search_requested\tTrue") &&
               lines.Contains("direct_selection_binding\tnot-proven") &&
               lines.Contains("original_semantics\tnot-claimed"),
            "Audio event-index evidence report did not preserve bounded negative evidence.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-event-index-report-test-");
        try
        {
            var path = Path.Combine(temporaryDirectory.FullName, "index.tsv");
            AudioEventIndexEvidenceReport.Write(path, data);
            Assert(File.ReadAllLines(path).SequenceEqual(lines),
                "Audio event-index evidence report write/read roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestFifast2CsiReader()
    {
        var data = new byte[0x90];
        Encoding.ASCII.GetBytes("MOIR").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), 0x300);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(8, 4), 0x00090003);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x0C, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x0E, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x10, 4), 0x1AF8);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x2C, 4), 0x70);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x30, 4), 0x10);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x38, 4), 0x76);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x3C, 4), 0x20);
        Encoding.ASCII.GetBytes("Alpha\0Beta\0").CopyTo(data, 0x70);

        var result = Fifast2CsiReader.Audit(data, new[] { "Alpha", "goal" });
        Assert(result.Rows.Count == 2 &&
               result.Summary.Status == "verified-structural-fifast2-csi-name-index-candidate" &&
               result.Summary.ValidRecordCount == 2 &&
               result.Summary.UniqueNameCount == 2 &&
               result.Summary.ReservedZeroCount == 2 &&
               result.Summary.OpaqueWordsWithinAsset == 2 &&
               result.Summary.OpaqueWordsNonDecreasingPairs == 1 &&
               result.Summary.EventExactNameOverlapCount == 1 &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed" &&
               result.Rows[0].Name == "Alpha" &&
               result.Rows[1].Name == "Beta",
            "FIFAST2 CSI structural name-index fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-fifast2-csi-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "csi.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "csi-summary.tsv");
            Fifast2CsiReader.WriteReport(reportPath, result);
            Fifast2CsiReader.WriteSummary(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Contains("status\tverified-structural-fifast2-csi-name-index-candidate"),
                "FIFAST2 CSI report roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestFifast2CsiTunaAudit()
    {
        var csiData = new byte[0x90];
        Encoding.ASCII.GetBytes("MOIR").CopyTo(csiData, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(4, 4), 0x300);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(8, 4), 0x00090003);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0C, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0E, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x10, 4), 0x1AF8);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x2C, 4), 0x70);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x30, 4), 0x10);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x38, 4), 0x76);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x3C, 4), 0x20);
        Encoding.ASCII.GetBytes("Alpha\0Beta\0").CopyTo(csiData, 0x70);

        var tunaText = string.Join(
            "\r\n",
            $"0x{Fifast2CsiTunaAudit.ComputeHash("Alpha"):X8}(Alpha),0,0,127",
            $"0x{Fifast2CsiTunaAudit.ComputeHash("Beta"):X8}(Beta),0,0,127") + "\r\n";
        var tunaPayload = Encoding.ASCII.GetBytes(tunaText);
        const string entryName = "tunavars.txt";
        var indexEnd = 16 + 8 + entryName.Length + 1;
        var tunaArchive = new byte[indexEnd + tunaPayload.Length];
        Encoding.ASCII.GetBytes("BIGF").CopyTo(tunaArchive, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(tunaArchive.AsSpan(4, 4), (uint)tunaArchive.Length);
        BinaryPrimitives.WriteUInt32BigEndian(tunaArchive.AsSpan(8, 4), 1);
        BinaryPrimitives.WriteUInt32BigEndian(tunaArchive.AsSpan(12, 4), (uint)indexEnd);
        BinaryPrimitives.WriteUInt32BigEndian(tunaArchive.AsSpan(16, 4), (uint)indexEnd);
        BinaryPrimitives.WriteUInt32BigEndian(tunaArchive.AsSpan(20, 4), (uint)tunaPayload.Length);
        Encoding.ASCII.GetBytes(entryName).CopyTo(tunaArchive, 24);
        tunaPayload.CopyTo(tunaArchive, indexEnd);

        var result = Fifast2CsiTunaAudit.Analyze(tunaArchive, csiData, new byte[0x30]);
        Assert(result.Summary.Status == Fifast2CsiTunaAudit.Status &&
               result.Summary.TunaRowCount == 2 &&
               result.Summary.TunaHashMatchCount == 2 &&
               result.Summary.CsiNameOverlapCount == 2 &&
               result.Summary.CsiRowsWithTunaHashMatch == 2 &&
               result.Summary.CsiOpaqueHashMatchCount == 0 &&
               result.Summary.CsiOpaqueWordsWithinPcpahdrAsset == 2 &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed" &&
               result.Rows.All(row => row.ComputedHashMatchesTuna && row.TunaNamePresent),
            "FIFAST2 CSI/tuna structural hash audit fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-fifast2-csi-tuna-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "crosscheck.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            Fifast2CsiTunaAudit.WriteReport(reportPath, result.Rows);
            Fifast2CsiTunaAuditSummary.Write(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Contains("status\t" + Fifast2CsiTunaAudit.Status),
                "FIFAST2 CSI/tuna structural hash audit report roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestFifast2CsiOpaqueRangeAudit()
    {
        var csiData = new byte[0x90];
        Encoding.ASCII.GetBytes("MOIR").CopyTo(csiData, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0C, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0E, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x2C, 4), 0x70);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x30, 4), 0x04);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x38, 4), 0x76);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x3C, 4), 0x0C);
        Encoding.ASCII.GetBytes("Alpha\0Beta\0").CopyTo(csiData, 0x70);

        var pcpahdrOff = Encoding.ASCII.GetBytes("0,8,0\r\n8,8,0\r\n");
        var pcpadatOff = Encoding.ASCII.GetBytes("0,16,0\r\n16,16,0\r\n");
        var result = Fifast2CsiOpaqueRangeAudit.Analyze(
            csiData,
            pcpahdrOff,
            16,
            pcpadatOff,
            32);
        Assert(result.Summary.Status == Fifast2CsiOpaqueRangeAudit.Status &&
               result.Summary.CsiValidRecordCount == 2 &&
               result.Summary.PcpahdrTableStructureValid &&
               result.Summary.PcpahdrPartitionCoverage &&
               result.Summary.PcpadatTableStructureValid &&
               result.Summary.PcpadatPartitionCoverage &&
               result.Summary.CsiOpaqueWithinPcpahdrCount == 2 &&
               result.Summary.CsiOpaquePcpahdrExactStartCount == 0 &&
               result.Summary.CsiOpaquePcpahdrExactEndCount == 0 &&
               result.Summary.CsiOpaqueWithinPcpadatCount == 2 &&
               result.Summary.CsiOpaquePcpadatExactStartCount == 0 &&
               result.Summary.CsiOpaquePcpadatExactEndCount == 0 &&
               result.Summary.DistinctPcpahdrContainingRows == 2 &&
               result.Summary.DistinctPcpadatContainingRows == 1 &&
               result.Summary.OpaqueWordSemantics == "not-decoded" &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed",
            "FIFAST2 CSI opaque range/boundary control fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-fifast2-csi-range-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "range.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "range-summary.tsv");
            Fifast2CsiOpaqueRangeAudit.WriteReport(reportPath, result.Rows);
            Fifast2CsiOpaqueRangeAuditSummary.Write(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Contains("status\t" + Fifast2CsiOpaqueRangeAudit.Status),
                "FIFAST2 CSI opaque range/boundary control report roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestFifast2CsiOpaqueFieldAudit()
    {
        var csiData = new byte[0x90];
        Encoding.ASCII.GetBytes("MOIR").CopyTo(csiData, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0C, 2), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(csiData.AsSpan(0x0E, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x2C, 4), 0x70);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x30, 4), 0x04);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x38, 4), 0x76);
        BinaryPrimitives.WriteUInt32LittleEndian(csiData.AsSpan(0x3C, 4), 0x0C);
        Encoding.ASCII.GetBytes("Alpha\0Beta\0").CopyTo(csiData, 0x70);

        var pcpahdrOff = Encoding.ASCII.GetBytes("0,8,4\r\n8,8,9\r\n");
        var pcpadatOff = Encoding.ASCII.GetBytes("0,16,0\r\n16,16,12\r\n");
        var result = Fifast2CsiOpaqueFieldAudit.Analyze(
            csiData,
            pcpahdrOff,
            16,
            pcpadatOff,
            32);
        Assert(result.Summary.Status == Fifast2CsiOpaqueFieldAudit.Status &&
               result.Summary.CsiValidRecordCount == 2 &&
               result.Summary.PcpahdrTableStructureValid &&
               result.Summary.PcpahdrPartitionCoverage &&
               result.Summary.PcpadatTableStructureValid &&
               result.Summary.PcpadatPartitionCoverage &&
               result.Summary.PcpahdrAnyColumnMatchCount == 1 &&
               result.Summary.PcpahdrColumn3MatchCount == 1 &&
               result.Summary.PcpadatAnyColumnMatchCount == 1 &&
               result.Summary.PcpadatColumn3MatchCount == 1 &&
               result.Summary.PcpahdrRowOrdinalMatchCount == 0 &&
               result.Summary.PcpadatRowOrdinalMatchCount == 0 &&
               result.Summary.OpaqueWordSemantics == "not-decoded" &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed",
            "FIFAST2 CSI opaque .off field control fixture mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-fifast2-csi-field-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "field.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "field-summary.tsv");
            Fifast2CsiOpaqueFieldAudit.WriteReport(reportPath, result.Rows);
            Fifast2CsiOpaqueFieldAuditSummary.Write(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Contains("status\t" + Fifast2CsiOpaqueFieldAudit.Status),
                "FIFAST2 CSI opaque .off field control report roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventPayloadWaveCorrelationAudit()
    {
        var rows = new[]
        {
            new AudioEventPayloadWaveCorrelationRow(
                "event",
                "event.evt",
                "goal",
                2,
                16,
                32,
                1,
                1,
                true,
                76,
                "ABC",
                1,
                "bank.abk",
                "decoded-payload-correlation",
                string.Empty),
            new AudioEventPayloadWaveCorrelationRow(
                "bank",
                "bank.abk",
                "bnk",
                1,
                0,
                0,
                1,
                1,
                true,
                76,
                "ABC",
                0,
                string.Empty,
                "decoded-payload",
                string.Empty),
        };
        var summary = AudioEventPayloadWaveCorrelationAudit.Summarize(rows, 1, 1);
        Assert(summary.EventCandidates == 1 &&
               summary.EventDecoded == 1 &&
               summary.BankCandidates == 1 &&
               summary.BankDecoded == 1 &&
               summary.ExactPcmHashPairs == 1 &&
               summary.EventHashesWithMatch == 1 &&
               summary.Status == "verified-structural" &&
               summary.DirectSelectionBinding == "not-proven" &&
               summary.OriginalSemantics == "not-claimed" &&
               summary.MaximumBankAttempts == AudioEventPayloadWaveCorrelationAudit.DefaultMaximumBankAttempts,
            "Audio event payload-wave correlation summary mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-payload-wave-correlation-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AudioEventPayloadWaveCorrelationAudit.WriteReport(reportPath, rows);
            AudioEventPayloadWaveCorrelationAudit.WriteSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Length == 11 &&
                   File.ReadAllLines(reportPath)[1].Contains("decoded-payload-correlation", StringComparison.Ordinal) &&
                   File.ReadAllLines(summaryPath).Contains("direct_selection_binding\tnot-proven") &&
                   File.ReadAllLines(summaryPath).Contains("bounded_policy\tfirst_8_bank_candidates"),
                "Audio event payload-wave correlation TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventPayloadWaveCorrelationExhaustiveAudit()
    {
        var rows = new[]
        {
            new AudioEventPayloadWaveCorrelationRow(
                "event",
                "event.evt",
                "goal",
                2,
                16,
                32,
                1,
                1,
                true,
                76,
                "ABC",
                1,
                "bank.abk#1",
                "decoded-payload-correlation",
                string.Empty),
            new AudioEventPayloadWaveCorrelationRow(
                "bank",
                "bank.abk",
                "bnk",
                1,
                0,
                0,
                2,
                1,
                true,
                76,
                "ABC",
                0,
                string.Empty,
                "decoded-payload",
                string.Empty),
            new AudioEventPayloadWaveCorrelationRow(
                "bank",
                "bank.abk",
                "bnk",
                2,
                0,
                0,
                2,
                2,
                false,
                0,
                string.Empty,
                0,
                string.Empty,
                "unvalidated",
                "bounded candidate rejected by managed decoder"),
        };
        var summary = AudioEventPayloadWaveCorrelationAudit.SummarizeExhaustive(rows, 1, 1, 2);
        Assert(summary.EventCandidates == 1 &&
               summary.EventDecoded == 1 &&
               summary.BankCount == 1 &&
               summary.BankSoundCandidates == 2 &&
               summary.BankSoundDecoded == 1 &&
               summary.ExactPcmHashPairs == 1 &&
               summary.EventHashesWithMatch == 1 &&
               summary.Status == "verified-structural" &&
               summary.DirectSelectionBinding == "not-proven" &&
               summary.OriginalSemantics == "not-claimed" &&
               summary.MaximumSoundCandidatesPerBank == 2,
            "Exhaustive audio event payload-wave correlation summary mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-payload-wave-exhaustive-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AudioEventPayloadWaveCorrelationAudit.WriteReport(reportPath, rows);
            AudioEventPayloadWaveCorrelationAudit.WriteExhaustiveSummary(summaryPath, summary);
            Assert(File.ReadAllLines(reportPath).Length == 4 &&
                   File.ReadAllLines(summaryPath).Length == 12 &&
                   File.ReadAllLines(summaryPath).Contains("bank_sound_candidates\t2") &&
                   File.ReadAllLines(summaryPath).Contains("bounded_policy\tfirst_2_sound_candidates_per_bank"),
                "Exhaustive audio event payload-wave correlation TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestAudioEventAbkRawIdentityAudit()
    {
        var fixture = BuildPcmSchlFixture();
        var eventRow = new AudioOffsetRow(1, 0, fixture.Length, 0);
        var eventCandidate = new AudioEventCandidateAsset(
            "event.evt",
            "header.big",
            "data.big",
            new AudioEventNamedHeaderCandidate("goal", 16, 0, eventRow, eventRow, 1),
            new EaSchlAudioInfo(12, 1, 1, 0, 1, 22050, 1),
            null);
        var bank = new AudioBankAsset(
            "bank.abk",
            fixture.Length,
            true,
            1,
            1,
            1,
            1,
            false,
            false,
            0,
            0,
            1,
            0,
            new[] { new AudioAbkAstRange(1, false, 0, fixture.Length) },
            "bank.ast",
            null)
        {
            SoundEntries = new[]
            {
                new AudioAbkSoundEntry(1, 0, 0, 0, 0, 1, 0, 0, 0, false),
            },
        };
        var catalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            audioBanks: new[] { bank },
            audioEventCandidates: new[] { eventCandidate });
        var result = AudioEventAbkRawIdentityAudit.Audit(
            catalog,
            _ => new MemoryStream(fixture, writable: false));
        Assert(result.Summary.EventCandidates == 1 &&
               result.Summary.EventRawRows == 1 &&
               result.Summary.EventDistinctRawHashes == 1 &&
               result.Summary.BankCount == 1 &&
               result.Summary.BankNonDummySoundEntries == 1 &&
               result.Summary.BankAstRangeRows == 1 &&
               result.Summary.BankAstSchlRows == 1 &&
               result.Summary.FullRawHashPairs == 1 &&
               result.Summary.EventHashesWithFullRawMatch == 1 &&
               result.Summary.RejectedRows == 0 &&
               result.Summary.Status == AudioEventAbkRawIdentityAudit.Status &&
               result.Summary.DirectSelectionBinding == "not-proven" &&
               result.Summary.OriginalSemantics == "not-claimed",
            "Raw EVT/ABKC/AST identity audit fixture mismatch.");
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-audio-raw-identity-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "report.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            AudioEventAbkRawIdentityAudit.WriteReport(reportPath, result.Rows);
            AudioEventAbkRawIdentityAudit.WriteSummary(summaryPath, result.Summary);
            Assert(File.ReadAllLines(reportPath).Length == 3 &&
                   File.ReadAllLines(summaryPath).Contains("status\t" + AudioEventAbkRawIdentityAudit.Status) &&
                   File.ReadAllLines(reportPath).Any(line => line.Contains("raw-range-validated-full-match-observation", StringComparison.Ordinal)),
                "Raw EVT/ABKC/AST identity TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestCleanRoomAudioAssets()
    {
        var manifest = CleanRoomAudioAssetManifest.Default;
        var knownCandidates = new[]
        {
            "intro",
            "attackgen",
            "panna",
            "attackgoal",
            "goal",
            "gb_goal",
            "tackle",
            "outro",
        };
        Assert(manifest.IsComplete &&
               manifest.ManifestId == "clean-room-audio-manifest-v1" &&
               knownCandidates.All(candidate => manifest.TryResolveCandidate(candidate, out _)) &&
               !manifest.TryResolveCandidate("unknown", out _),
            "Authored audio asset manifest did not preserve known/unknown candidate separation.");
    }

    private static void TestCleanRoomIntegrationReport()
    {
        var data = new CleanRoomIntegrationReportData(
            CandidateMaterialValidated: true,
            CandidateHomeEnabled: true,
            CandidateAwayEnabled: true,
            RenderedFrames: 140,
            RenderChecksum: "088EC5D10096A738",
            DistinctRenderChecksums: 7,
            AuthoredCharacterAssetFrames: 0,
            AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
            AuthoredSceneAssetFrames: 140,
            AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
            AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
            InputSamples: 140,
            AnimationSamples: 280,
            MatchEvents: 8,
            AudioEvents: 8,
            AudioPrepared: 8,
            AudioPlayed: 8,
            AudioUnmapped: 0,
            NamedEventCandidateWavesPrepared: 17,
            NamedEventCandidatePcmBytesPrepared: 4321,
            NamedEventCandidateDecodeRejected: 0,
            ObservedBankCandidateCount: 16,
            ObservedBankCandidateDecoded: 15,
            ObservedBankCandidateRejected: 1,
            GameplayAudioPcmBytes: 1234,
            GameplayAudioPcmSha256: new string('A', 64),
            AuthoredAudioAssetManifest: CleanRoomAudioAssetManifest.Default.ManifestId,
            MatchTraceWritten: true,
            InputTraceWritten: true,
            AnimationTraceWritten: true,
            AudioTraceWritten: true,
            OpponentAiEnabled: true);
        var lines = CleanRoomIntegrationReport.BuildLines(data);
            Assert(lines[0] == CleanRoomIntegrationReport.Header &&
               lines.Contains(
                   CleanRoomIntegrationReport.RuntimeAssemblySha256Field + "\t" +
                   CleanRoomIntegrationReport.CurrentAssemblySha256) &&
               lines.Contains("candidate_material_validated\tTrue") &&
               lines.Contains("candidate_home_enabled\tTrue") &&
               lines.Contains("candidate_away_enabled\tTrue") &&
               lines.Contains("rendered_frames\t140") &&
               lines.Contains("render_checksum\t088EC5D10096A738") &&
               lines.Contains("distinct_render_checksums\t7") &&
               lines.Contains("authored_character_asset_frames\t0") &&
               lines.Contains("authored_character_asset_manifest\t" + CleanRoomCharacterAssetManifest.Default.ManifestId) &&
               lines.Contains("authored_scene_asset_frames\t140") &&
               lines.Contains("authored_scene_asset_manifest\t" + CleanRoomSceneAssetManifest.Default.ManifestId) &&
               lines.Contains("authored_animation_asset_manifest\t" + CleanRoomAnimationAssetManifest.Default.ManifestId) &&
               lines.Contains("authored_audio_asset_manifest\t" + CleanRoomAudioAssetManifest.Default.ManifestId) &&
               lines.Contains("input_samples\t140") &&
               lines.Contains("animation_samples\t280") &&
               lines.Contains("audio_played\t8") &&
               lines.Contains("audio_unmapped\t0") &&
               lines.Contains("named_event_candidate_waves_prepared\t17") &&
               lines.Contains("named_event_candidate_pcm_bytes_prepared\t4321") &&
               lines.Contains("named_event_candidate_decode_rejected\t0") &&
               lines.Contains("observed_bank_candidate_count\t16") &&
               lines.Contains("observed_bank_candidate_decoded\t15") &&
               lines.Contains("observed_bank_candidate_rejected\t1") &&
               lines.Contains("gameplay_audio_pcm_bytes\t1234") &&
               lines.Contains("gameplay_audio_pcm_sha256\t" + new string('A', 64)) &&
               lines.Contains("input_trace_written\tTrue") &&
               lines.Contains("animation_trace_written\tTrue") &&
               lines.Contains("opponent_ai_enabled\tTrue") &&
               lines.Contains("original_semantics\tnot-claimed"),
            "Clean-room integration report did not preserve cross-system evidence and provenance.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-integration-report-test-");
        try
        {
            var path = Path.Combine(temporaryDirectory.FullName, "integration.tsv");
            CleanRoomIntegrationReport.Write(path, data);
            Assert(File.ReadAllLines(path).SequenceEqual(lines),
                "Clean-room integration report write/read roundtrip mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static GameAssetCatalog CreateAudioCatalogFixture()
    {
        return GameAssetCatalog.CreateForTest(
            new AudioStreamAsset[]
            {
                new("valid.ast", 100, true, new EaSchlAudioInfo(44, 3, 7, 10, 2, 22050, 1000), null),
                new("invalid.ast", 100, false, null, "fixture"),
            });
    }

    private static byte[] BuildPcmSchlFixture()
    {
        const int headerSize = 26;
        const int blockSize = 77;
        var data = new byte[headerSize + blockSize];
        Encoding.ASCII.GetBytes("SCHl").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(4, 4), headerSize);
        Encoding.ASCII.GetBytes("PT").CopyTo(data, 8);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(10, 2), 7);
        new byte[] { 0x80, 1, 3 }.AsSpan().CopyTo(data.AsSpan(12, 3));
        new byte[] { 0x82, 1, 1 }.AsSpan().CopyTo(data.AsSpan(15, 3));
        new byte[] { 0x84, 2, 0x1F, 0x40 }.AsSpan().CopyTo(data.AsSpan(18, 4));
        new byte[] { 0xA0, 1, 0x0A }.AsSpan().CopyTo(data.AsSpan(22, 3));
        data[25] = 0xFF;

        var chunk = data.AsSpan(headerSize, blockSize);
        Encoding.ASCII.GetBytes("SCDl").CopyTo(chunk);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk[4..8], blockSize);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk[8..12], 1);
        BinaryPrimitives.WriteUInt32LittleEndian(chunk[12..16], 0);
        chunk[16] = 0xEE;
        chunk[21] = 0x12;
        chunk[22] = 0x34;
        return data;
    }

    private static byte[] BuildAbkFixture()
    {
        var data = new byte[0x140];
        Encoding.ASCII.GetBytes("ABKC").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x0A, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x14, 4), (uint)data.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x1C, 4), 0x5C);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x20, 4), 0);
        data[0x5C + 0x24] = 1;
        data[0x5C + 0x27] = 0;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x5C + 0x2C, 4), 0xE0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x5C + 0x3C, 4), 0x20);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x100 + 4, 4), 0x120);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x120, 4), 2);
        data[0x124] = 1;
        data[0x125] = 4;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x128, 4), 0x10);
        data[0x130] = 0;
        data[0x131] = 4;
        return data;
    }

    private static byte[] BuildRamAbkFixture()
    {
        var data = new byte[0x1C0];
        Encoding.ASCII.GetBytes("ABKC").CopyTo(data, 0);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x0A, 2), 1);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x14, 4), (uint)data.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x1C, 4), 0x5C);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x20, 4), 0x160);
        data[0x5C + 0x24] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x5C + 0x2C, 4), 0xE0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x5C + 0x3C, 4), 0x20);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x100 + 4, 4), 0x120);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x120, 4), 1);
        data[0x124] = 0;
        data[0x125] = 255;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x128, 4), 1);

        Encoding.ASCII.GetBytes("BNKl").CopyTo(data, 0x160);
        data[0x164] = 5;
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x166, 2), 2);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x174, 4), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x178, 4), 0x20);
        Encoding.ASCII.GetBytes("PT\u0007\u0000").CopyTo(data, 0x198);
        data[0x19C] = 0xA0;
        data[0x19D] = 1;
        data[0x19E] = 0x14;
        data[0x19F] = 0x84;
        data[0x1A0] = 2;
        data[0x1A1] = 0x56;
        data[0x1A2] = 0x22;
        data[0x1A3] = 0x85;
        data[0x1A4] = 1;
        data[0x1A5] = 65;
        data[0x1A6] = 0x88;
        data[0x1A7] = 4;
        data[0x1A8] = 0;
        data[0x1A9] = 0;
        data[0x1AA] = 0;
        data[0x1AB] = 0x20;
        data[0x1AC] = 0xFF;
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(0x180, 2), 1000);
        data[0x182] = 0;
        return data;
    }

    private static byte[] BuildScclThenPcmSchlFixture()
    {
        var pcm = BuildPcmSchlFixture();
        const int headerSize = 26;
        const int scclSize = 12;
        var result = new byte[pcm.Length + scclSize];
        pcm.AsSpan(0, headerSize).CopyTo(result);
        var sccl = result.AsSpan(headerSize, scclSize);
        Encoding.ASCII.GetBytes("SCCl").CopyTo(sccl);
        BinaryPrimitives.WriteUInt32LittleEndian(sccl[4..8], scclSize);
        BinaryPrimitives.WriteUInt32LittleEndian(sccl[8..12], 0x26);
        pcm.AsSpan(headerSize).CopyTo(result.AsSpan(headerSize + scclSize));
        return result;
    }

    private static void TestMatch()
    {
        var match = new StreetMatch();
        Assert(match.EventHistory.Count == 1 &&
               match.EventHistory[0].Sequence == 0 &&
               match.EventHistory[0].Frame == 0 &&
               match.EventHistory[0].Action == "KICKOFF" &&
               match.EventHistory[0].Possession == BallOwner.None,
            "Clean-room match did not seed its authored event trace with KICKOFF.");
        var initialX = match.HomePosition.X;
        match.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        Assert(match.Frame == 1, "Match frame did not advance.");
        Assert(match.HomePosition.X > initialX, "Match input did not move the home player.");
        Assert(match.RemainingSeconds < 300.0, "Match clock did not advance.");
        Assert(match.HudText().Contains("HOME 0 - 0 AWAY", StringComparison.Ordinal), "Match HUD mismatch.");

        var inputScriptDirectory = Directory.CreateTempSubdirectory("fifa-input-script-test-");
        try
        {
            var inputScriptPath = Path.Combine(inputScriptDirectory.FullName, "valid.tsv");
            File.WriteAllLines(
                inputScriptPath,
                new[]
                {
                    "# authored clean-room input fixture",
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick",
                    "0\t1\t0\t0\t0\t0",
                    "1\t0\t-0.5\tfalse\t0\tTRUE",
                    "2\t0\t0\t1\t0\t0",
                });
            var script = MatchInputScriptReader.Read(inputScriptPath);
            var scriptSource = new MatchInputScriptSource(script);
            Assert(script.FrameCount == 3 && scriptSource.FrameCount == 3,
                "Input script reader did not preserve its frame count.");
            Assert(scriptSource.Poll() == new MatchInput(1, 0, false, false, false) &&
                   scriptSource.Poll() == new MatchInput(0, -0.5f, false, false, true) &&
                   scriptSource.Poll() == new MatchInput(0, 0, true, false, false) &&
                   scriptSource.Poll() == new MatchInput(),
                "Input script source did not emit validated rows in order or default after EOF.");

            var pannaInputScriptPath = Path.Combine(inputScriptDirectory.FullName, "panna.tsv");
            File.WriteAllLines(
                pannaInputScriptPath,
                new[]
                {
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick\tpanna",
                    "0\t0\t0\t0\t0\t0\t1",
                });
            var pannaScript = MatchInputScriptReader.Read(pannaInputScriptPath);
            Assert(pannaScript.FrameCount == 1 && pannaScript.Inputs[0].Panna,
                "Input script reader did not preserve the optional authored panna column.");

            var gamebreakerInputScriptPath = Path.Combine(inputScriptDirectory.FullName, "gamebreaker.tsv");
            File.WriteAllLines(
                gamebreakerInputScriptPath,
                new[]
                {
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick\tpanna\tgamebreaker",
                    "0\t0\t0\t0\t0\t0\t0\t1",
                });
            var gamebreakerScript = MatchInputScriptReader.Read(gamebreakerInputScriptPath);
            Assert(gamebreakerScript.FrameCount == 1 && gamebreakerScript.Inputs[0].Gamebreaker,
                "Input script reader did not preserve the optional authored gamebreaker column.");

            var juggleInputScriptPath = Path.Combine(inputScriptDirectory.FullName, "juggle.tsv");
            File.WriteAllLines(
                juggleInputScriptPath,
                new[]
                {
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick\tpanna\tgamebreaker\tjuggle",
                    "0\t0\t0\t0\t0\t0\t0\t0\t1",
                });
            var juggleScript = MatchInputScriptReader.Read(juggleInputScriptPath);
            Assert(juggleScript.FrameCount == 1 && juggleScript.Inputs[0].Juggle,
                "Input script reader did not preserve the optional authored juggle column.");

            var tackleInputScriptPath = Path.Combine(inputScriptDirectory.FullName, "tackle.tsv");
            File.WriteAllLines(
                tackleInputScriptPath,
                new[]
                {
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick\tpanna\tgamebreaker\tjuggle\ttackle",
                    "0\t0\t0\t0\t0\t0\t0\t0\t0\t1",
                });
            var tackleScript = MatchInputScriptReader.Read(tackleInputScriptPath);
            Assert(tackleScript.FrameCount == 1 && tackleScript.Inputs[0].Tackle,
                "Input script reader did not preserve the optional authored tackle column.");

            var invalidInputScriptPath = Path.Combine(inputScriptDirectory.FullName, "invalid.tsv");
            File.WriteAllLines(
                invalidInputScriptPath,
                new[]
                {
                    "frame\tmove_x\tmove_y\tpass\tshoot\ttrick",
                    "1\t0\t0\t0\t0\t0",
                });
            var invalidScriptRejected = false;
            try
            {
                _ = MatchInputScriptReader.Read(invalidInputScriptPath);
            }
            catch (InvalidDataException)
            {
                invalidScriptRejected = true;
            }

            Assert(invalidScriptRejected,
                "Input script reader accepted a non-contiguous frame sequence.");
        }
        finally
        {
            inputScriptDirectory.Delete(recursive: true);
        }

        var fullTimeMatch = new StreetMatch(matchDurationSeconds: 0.1);
        fullTimeMatch.Tick(0.1, new MatchInput(1, 0, false, false, false));
        Assert(fullTimeMatch.IsEnded &&
               fullTimeMatch.LastAction == "FULL_TIME" &&
               fullTimeMatch.Result == CleanRoomMatchResult.Draw &&
               fullTimeMatch.ActionCounts.TryGetValue("FULL_TIME", out var fullTimeCount) &&
               fullTimeCount == 1 &&
               fullTimeMatch.Frame == 1,
            "Clean-room match clock did not emit the deterministic FULL_TIME transition.");
        fullTimeMatch.Tick(1.0, new MatchInput(1, 0, false, false, false));
        Assert(fullTimeMatch.Frame == 1 && fullTimeMatch.ActionCounts["FULL_TIME"] == 1,
            "Ended clean-room match advanced after FULL_TIME.");

        var firstToGoalsMatch = new StreetMatch(
            endCondition: CleanRoomMatchEndCondition.FirstToGoals,
            targetGoals: 1);
        firstToGoalsMatch.Tick(0.25, new MatchInput(1, 0, false, false, false));
        firstToGoalsMatch.Tick(0.25, new MatchInput(1, 0, false, false, false));
        for (var frame = 0; frame < 8; frame++)
        {
            firstToGoalsMatch.Tick(
                1.0 / 60.0,
                new MatchInput(0, 0, false, frame == 0, false));
        }
        Assert(firstToGoalsMatch.EndCondition == CleanRoomMatchEndCondition.FirstToGoals &&
               firstToGoalsMatch.TargetGoals == 1 &&
               firstToGoalsMatch.HomeScore == 1 &&
               firstToGoalsMatch.IsEnded &&
               firstToGoalsMatch.LastAction == "FULL_TIME" &&
               firstToGoalsMatch.Result == CleanRoomMatchResult.HomeWin &&
               firstToGoalsMatch.RemainingSeconds > 0 &&
               firstToGoalsMatch.ActionCounts.TryGetValue("FULL_TIME", out var firstToGoalsFullTimeCount) &&
               firstToGoalsFullTimeCount == 1,
            "Authored first-to-goals rule did not end at its target without consuming the clock.");

        var goalsOrPannaMatch = new StreetMatch(
            endCondition: CleanRoomMatchEndCondition.GoalsOrPanna,
            targetGoals: 9,
            targetPannas: 1);
        for (var frame = 0; frame < 30; frame++)
        {
            goalsOrPannaMatch.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        }
        goalsOrPannaMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, false, false, false, true));
        Assert(goalsOrPannaMatch.EndCondition == CleanRoomMatchEndCondition.GoalsOrPanna &&
               goalsOrPannaMatch.PannaCount == 1 &&
               goalsOrPannaMatch.HomeScore == 0 &&
               goalsOrPannaMatch.IsEnded &&
               goalsOrPannaMatch.LastAction == "FULL_TIME" &&
               goalsOrPannaMatch.Result == CleanRoomMatchResult.Draw &&
               goalsOrPannaMatch.RemainingSeconds > 0,
            "Authored goals-or-panna rule did not end on the explicit panna input.");

        var possessionFollowMatch = new StreetMatch();
        for (var frame = 0; frame < 40; frame++)
        {
            possessionFollowMatch.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        }
        Assert(possessionFollowMatch.Possession == BallOwner.Home &&
               MathF.Abs(possessionFollowMatch.BallPosition.X - possessionFollowMatch.HomePosition.X) < 0.0001f &&
               MathF.Abs(possessionFollowMatch.BallPosition.Y - possessionFollowMatch.HomePosition.Y) < 0.0001f,
            "Ball did not follow the home possessor outside flight.");

        match.Tick(0.25, new MatchInput(1, 0, false, false, false));
        match.Tick(0.25, new MatchInput(1, 0, false, false, false));
        for (var frame = 0; frame < 7; frame++)
        {
            match.Tick(1.0 / 60.0, new MatchInput(0, 0, false, true, false));
        }
        Assert(match.HomeScore == 1, "Match shot did not produce a goal.");
        Assert(match.ActionCounts.TryGetValue("SHOT", out var shotCount) && shotCount == 1,
            "Match shot telemetry did not record exactly one shot.");
        Assert(match.ActionCounts.TryGetValue("GOAL_HOME", out var goalCount) && goalCount == 1,
            "Match goal telemetry did not record exactly one home goal.");
        match.Tick(300.0, new MatchInput());
        Assert(match.Result == CleanRoomMatchResult.HomeWin &&
               match.LastAction == "FULL_TIME",
            "Clean-room match result did not resolve the home win at FULL_TIME.");
        Assert(match.EventHistory.Count == match.ActionSequence + 1 &&
               match.EventHistory[^1].Sequence == match.ActionSequence &&
               match.EventHistory[^1].Action == "FULL_TIME" &&
               match.EventHistory.Zip(
                       match.EventHistory.Skip(1),
                       (previous, current) => current.Sequence == previous.Sequence + 1)
                   .All(isContiguous => isContiguous),
              "Clean-room match event trace lost an authored action or sequence transition.");

        var gamebreakerMatch = new StreetMatch();
        var gamebreakerInputSource = new GameplayDemoInputSource(gamebreaker: true);
        for (var frame = 0; frame < 40; frame++)
        {
            gamebreakerMatch.Tick(1.0 / 60.0, gamebreakerInputSource.Poll());
        }
        Assert(gamebreakerMatch.TrickCount == 4 &&
               gamebreakerMatch.GamebreakerMeter == 0 &&
               gamebreakerMatch.GamebreakerCount == 1 &&
               gamebreakerMatch.HomeScore == 1 &&
               gamebreakerMatch.ActionCounts.TryGetValue("GAMEBREAKER", out var gamebreakerCount) &&
               gamebreakerCount == 1 &&
               gamebreakerMatch.ActionCounts.TryGetValue("GOAL_HOME", out var gamebreakerGoalCount) &&
               gamebreakerGoalCount == 1,
            "Authored gamebreaker scenario did not charge, trigger and score deterministically.");

        var stealMatch = new StreetMatch(enableOpponentAi: true);
        var stealInputSource = new StealDemoInputSource();
        for (var frame = 0; frame < 60; frame++)
        {
            stealMatch.Tick(1.0 / 60.0, stealInputSource.Poll());
        }
        Assert(stealMatch.Possession == BallOwner.Away &&
               stealMatch.ActionCounts.TryGetValue("STEAL", out var stealCount) &&
               stealCount == 1 &&
               stealMatch.EventHistory.Any(matchEvent => matchEvent.Action == "STEAL"),
            "Authored opponent-pressure scenario did not produce a deterministic STEAL transition.");

        var juggleMatch = new StreetMatch();
        for (var frame = 0; frame < 28; frame++)
        {
            var input = frame <= 20
                ? new MatchInput(1, 0, false, false, false)
                : frame is 21 or 23 or 25 or 27
                    ? new MatchInput(0, 0, false, false, false, false, false, true)
                    : new MatchInput();
            juggleMatch.Tick(1.0 / 60.0, input);
        }
        Assert(juggleMatch.JuggleCount == 4 &&
               juggleMatch.JuggleChain == 4 &&
               juggleMatch.BallHeight > 0 &&
               juggleMatch.ActionCounts.TryGetValue("JUGGLE", out var juggleCount) &&
               juggleCount == 4 &&
               juggleMatch.GamebreakerMeter == 20,
            "Authored juggle scenario did not preserve its chain, height and meter deterministically.");
        juggleMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, false, true, false));
        Assert(juggleMatch.LastAction == "SHOT" && juggleMatch.BallHeight == 0 && juggleMatch.JuggleChain == 0,
            "Authored juggle state did not clear when the ball entered a shot.");

        var tackleMatch = new StreetMatch(enableOpponentAi: true);
        var tackleInputSource = new TackleDemoInputSource();
        for (var frame = 0; frame < 100; frame++)
        {
            tackleMatch.Tick(1.0 / 60.0, tackleInputSource.Poll());
        }
        Assert(tackleMatch.TackleCount == 1 &&
               tackleMatch.ActionCounts.TryGetValue("TACKLE", out var tackleCount) &&
               tackleCount == 1 &&
               tackleMatch.EventHistory.Any(matchEvent => matchEvent.Action == "TACKLE"),
            "Authored defensive-control scenario did not produce a deterministic TACKLE transition.");

        var windowedGamebreakerInput = Win32GameHost.BuildAuthoredInput(key => key == 'G');
        Assert(windowedGamebreakerInput == new MatchInput(0, 0, false, false, false, false, true),
            "Win32 authored input mapping did not expose the G gamebreaker control.");
        var windowedPannaInput = Win32GameHost.BuildAuthoredInput(key => key == 'N');
        Assert(windowedPannaInput == new MatchInput(0, 0, false, false, false, true, false),
            "Win32 authored input mapping did not expose the N panna control.");
        var windowedJuggleInput = Win32GameHost.BuildAuthoredInput(key => key == 'J');
        Assert(windowedJuggleInput == new MatchInput(0, 0, false, false, false, false, false, true),
            "Win32 authored input mapping did not expose the J juggle control.");
        var windowedTackleInput = Win32GameHost.BuildAuthoredInput(key => key == 'K');
        Assert(windowedTackleInput == new MatchInput(0, 0, false, false, false, false, false, false, true),
            "Win32 authored input mapping did not expose the K tackle control.");
        var serializedMatchTrace = MatchTraceSerializer.BuildLines(match.EventHistory);
        var alteredMatchTrace = serializedMatchTrace.ToArray();
        alteredMatchTrace[1] += "\tALTERED";
        Assert(serializedMatchTrace[0] == MatchTraceSerializer.Header &&
               MatchTraceSerializer.FindFirstDifference(serializedMatchTrace, serializedMatchTrace) == -1 &&
               MatchTraceSerializer.FindFirstDifference(serializedMatchTrace, alteredMatchTrace) == 1 &&
               MatchTraceSerializer.FindFirstDifference(serializedMatchTrace, serializedMatchTrace.Take(1).ToArray()) == 1,
            "Authored match trace serializer did not preserve exact comparison behavior.");
        var inputSamples = new[]
        {
            new CleanRoomInputSample(0, new MatchInput(1, 0, false, false, false)),
            new CleanRoomInputSample(67, new MatchInput(0, 0.75f, false, true, false)),
        };
        var serializedInputTrace = CleanRoomInputTraceSerializer.BuildLines(inputSamples);
        var alteredInputTrace = serializedInputTrace.ToArray();
        alteredInputTrace[2] = alteredInputTrace[2].Replace("0.75", "0.5", StringComparison.Ordinal);
        Assert(serializedInputTrace[0] == CleanRoomInputTraceSerializer.Header &&
               serializedInputTrace[2].Contains("0.75\tFalse\tTrue", StringComparison.Ordinal) &&
               CleanRoomInputTraceSerializer.FindFirstDifference(serializedInputTrace, serializedInputTrace) == -1 &&
               CleanRoomInputTraceSerializer.FindFirstDifference(serializedInputTrace, alteredInputTrace) == 2 &&
               CleanRoomInputTraceSerializer.FindFirstDifference(serializedInputTrace, serializedInputTrace.Take(1).ToArray()) == 1,
            "Clean-room input trace serializer did not preserve the authored aimed input fields.");

        var roundResetMatch = new StreetMatch(enableCleanRoomRoundReset: true);
        roundResetMatch.Tick(0.25, new MatchInput(1, 0, false, false, false));
        roundResetMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, false, true, false));
        for (var frame = 0; frame < 8; frame++)
        {
            roundResetMatch.Tick(1.0 / 60.0, new MatchInput());
        }
        Assert(roundResetMatch.HomeScore == 1 && roundResetMatch.RestartPending,
            "Clean-room round reset did not enter its deterministic restart interval.");
        for (var frame = 0; frame < 31; frame++)
        {
            roundResetMatch.Tick(1.0 / 60.0, new MatchInput());
        }
        Assert(roundResetMatch.RoundNumber == 2 &&
               !roundResetMatch.RestartPending &&
               roundResetMatch.Possession == BallOwner.Home &&
               roundResetMatch.LastAction == "RESTART" &&
               MathF.Abs(roundResetMatch.BallPosition.X - roundResetMatch.HomePosition.X) < 0.0001f,
            "Clean-room round reset did not restart possession and ball synchronization.");

        var aimedShotMatch = new StreetMatch();
        aimedShotMatch.Tick(0.25, new MatchInput(1, 0, false, false, false));
        aimedShotMatch.Tick(1.0 / 60.0, new MatchInput(0, 0.75f, false, true, false));
        Assert(aimedShotMatch.LastAction == "SHOT" &&
               aimedShotMatch.BallPosition.Y > 0.31f,
            "Clean-room shot aim did not convert vertical input into a rising trajectory.");

        var aimedDemoInputSource = new GameplayDemoInputSource(aimedShot: true);
        MatchInput aimedDemoShotInput = default;
        for (var frame = 0; frame <= 67; frame++)
        {
            aimedDemoShotInput = aimedDemoInputSource.Poll();
        }
        Assert(aimedDemoShotInput.Shoot &&
               MathF.Abs(aimedDemoShotInput.MoveY - 0.75f) < 0.0001f,
            "Aimed clean-room demo did not emit the documented vertical shot input.");

        var controlMatch = new StreetMatch();
        controlMatch.Tick(0.25, new MatchInput(1, 0, false, false, false));
        controlMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, false, false, true));
        Assert(controlMatch.TrickCount == 1 && controlMatch.LastAction == "TRICK",
            "Match trick action did not update state.");
        Assert(controlMatch.ActionCounts.TryGetValue("TRICK", out var trickCount) && trickCount == 1,
            "Match trick telemetry did not record exactly one trick.");
        controlMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, true, false, false));
        Assert(controlMatch.Possession == BallOwner.None && controlMatch.LastAction == "PASS",
            "Match pass did not enter flight state.");
        Assert(controlMatch.ActionCounts.TryGetValue("PASS", out var passCount) && passCount == 1,
            "Match pass telemetry did not record exactly one pass.");
        for (var frame = 0; frame < 20 && controlMatch.Possession != BallOwner.Away; frame++)
        {
            controlMatch.Tick(1.0 / 60.0, new MatchInput(0, 0, false, false, false));
        }
        Assert(controlMatch.Possession == BallOwner.Away && controlMatch.LastAction == "PASS_COMPLETE",
            "Match pass did not complete at the target player.");
        Assert(controlMatch.ActionCounts.TryGetValue("PASS_COMPLETE", out var passCompleteCount) && passCompleteCount == 1,
            "Match pass telemetry did not record exactly one completed pass.");

        var aiMatch = new StreetMatch(enableOpponentAi: true);
        var initialAwayPosition = aiMatch.AwayPosition;
        for (var frame = 0; frame < 40; frame++)
        {
            aiMatch.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        }
        Assert(aiMatch.OpponentAiEnabled, "Opponent AI mode was not enabled.");
        Assert(aiMatch.AwayPosition.X < initialAwayPosition.X &&
               (aiMatch.Possession == BallOwner.Home || aiMatch.ActionCounts.ContainsKey("STEAL")),
            "Opponent AI did not pressure the home player after observed possession.");

        var aiTurnoverMatch = new StreetMatch(enableOpponentAi: true);
        for (var frame = 0; frame < 120; frame++)
        {
            aiTurnoverMatch.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        }
        for (var frame = 0; frame < 60; frame++)
        {
            aiTurnoverMatch.Tick(1.0 / 60.0, new MatchInput());
        }
        Assert(aiTurnoverMatch.ActionCounts.ContainsKey("STEAL") &&
               aiTurnoverMatch.ActionCounts.ContainsKey("SHOT_AWAY") &&
               aiTurnoverMatch.ActionCounts.ContainsKey("GOAL_AWAY"),
            $"Opponent AI did not complete the deterministic turnover and away shot path: " +
            $"actions={string.Join(",", aiTurnoverMatch.ActionCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}; " +
            $"home=({aiTurnoverMatch.HomePosition.X:0.000},{aiTurnoverMatch.HomePosition.Y:0.000}); " +
            $"away=({aiTurnoverMatch.AwayPosition.X:0.000},{aiTurnoverMatch.AwayPosition.Y:0.000}); " +
            $"ball=({aiTurnoverMatch.BallPosition.X:0.000},{aiTurnoverMatch.BallPosition.Y:0.000}); " +
            $"possession={aiTurnoverMatch.Possession}");
    }

    private static void TestCleanRoomPlayerAnimator()
    {
        var idle = CleanRoomPlayerAnimator.Evaluate(10, "KICKOFF", BallOwner.Home, false);
        var trick = CleanRoomPlayerAnimator.Evaluate(10, "TRICK", BallOwner.Home, true);
        var shot = CleanRoomPlayerAnimator.Evaluate(10, "SHOT", BallOwner.Home, false);
        var juggle = CleanRoomPlayerAnimator.Evaluate(10, "JUGGLE", BallOwner.Home, true);
        var steal = CleanRoomPlayerAnimator.Evaluate(10, "STEAL", BallOwner.Away, false);
        var gamebreaker = CleanRoomPlayerAnimator.Evaluate(10, "GAMEBREAKER", BallOwner.Home, false);
        var fullTime = CleanRoomPlayerAnimator.Evaluate(10, "FULL_TIME", BallOwner.Home, false);
        var away = CleanRoomPlayerAnimator.Evaluate(10, "KICKOFF", BallOwner.Away, false);
        Assert(idle.Facing == 1.0f && away.Facing == -1.0f,
            "Clean-room player animator did not preserve deterministic team facing.");
        Assert(trick.ActionLift > idle.ActionLift &&
               trick.ArmReach > idle.ArmReach &&
               trick.TorsoLift > idle.TorsoLift &&
               MathF.Abs(trick.BodyLean) > 0,
            "Clean-room player animator did not apply authored possession/action pose changes.");
        Assert(shot.HeadTurn > idle.HeadTurn &&
               MathF.Abs(shot.ActionPulse) <= 1.0f &&
               fullTime.ActionLift > idle.ActionLift,
            "Clean-room player animator did not expose deterministic authored action profiles.");
        Assert(juggle.ActionLift > idle.ActionLift &&
               juggle.ArmReach > idle.ArmReach &&
               juggle.TorsoLift > idle.TorsoLift,
            "Clean-room player animator did not expose the authored juggle profile.");
        Assert(steal.Facing < 0 &&
               steal.ActionLift > idle.ActionLift &&
               steal.ArmReach > idle.ArmReach &&
               steal.TorsoLift > idle.TorsoLift &&
               steal.HeadTurn > idle.HeadTurn,
            "Clean-room player animator did not expose the authored tackle profile.");
        Assert(gamebreaker.ActionLift > trick.ActionLift &&
               gamebreaker.ArmReach > shot.ArmReach &&
               gamebreaker.TorsoLift > shot.TorsoLift &&
               MathF.Abs(gamebreaker.BodyLean) > 0,
            "Clean-room player animator did not expose the authored gamebreaker presentation profile.");
        Assert(CleanRoomPlayerAnimator.Evaluate(10, "TRICK", BallOwner.Home, true) == trick,
            "Clean-room player animator was not deterministic for the same runtime state.");

        var staticFrame = new SoftwareFrameBuffer(160, 100);
        var animatedFrame = new SoftwareFrameBuffer(160, 100);
        var authoredHudFrame = new SoftwareFrameBuffer(160, 100);
        var match = new StreetMatch();
        PreviewRenderer.Render(match, staticFrame);
        PreviewRenderer.Render(match, animatedFrame, cleanRoomCharacters: true);
        PreviewRenderer.Render(match, authoredHudFrame, showAuthoredHud: true);
        Assert(staticFrame.ComputeChecksum() != animatedFrame.ComputeChecksum(),
            "Clean-room character renderer did not produce a distinct authored presentation path.");
        Assert(staticFrame.ComputeChecksum() != authoredHudFrame.ComputeChecksum(),
            "Authored gamebreaker HUD did not produce a distinct Win32 presentation path.");

        var historyRenderer = new SoftwareGameplayRenderer(160, 100, cleanRoomCharacters: true);
        var historyMatch = new StreetMatch();
        historyRenderer.Initialize();
        historyRenderer.Render(historyMatch);
        historyMatch.Tick(1.0 / 60.0, new MatchInput(1, 0, false, false, false));
        historyRenderer.Render(historyMatch);
        Assert(historyRenderer.DistinctFrameChecksumCount > 1,
            "Clean-room runtime did not record variation across authored presentation frames.");
    }

    private static void TestCleanRoomAnimationAssets()
    {
        var manifest = CleanRoomAnimationAssetManifest.Default;
        Assert(manifest.IsComplete &&
               manifest.ManifestId == "clean-room-animation-manifest-v1" &&
               manifest.ResolveProfile("KICKOFF") == CleanRoomAnimationAssetProfile.Idle &&
               manifest.ResolveProfile("PANNA") == CleanRoomAnimationAssetProfile.TrickPanna &&
               manifest.ResolveProfile("SHOT_AWAY") == CleanRoomAnimationAssetProfile.Shot &&
               manifest.ResolveProfile("GOAL_HOME") == CleanRoomAnimationAssetProfile.Goal &&
               manifest.ResolveProfile("unknown") == CleanRoomAnimationAssetProfile.Unknown,
            "Authored animation asset manifest profile resolution mismatch.");
        Assert(manifest.TryResolveAsset("GAMEBREAKER", out var gamebreakerAsset) &&
               gamebreakerAsset == manifest.GamebreakerAsset &&
               !manifest.TryResolveAsset("unknown", out _),
            "Authored animation asset manifest did not preserve known/unknown separation.");
    }

    private static void TestCleanRoomCharacterAsset()
    {
        var palette = new CleanRoomCharacterPalette(
            new RgbColor(231, 73, 66),
            new RgbColor(150, 47, 43),
            new RgbColor(235, 188, 146),
            new RgbColor(20, 25, 30),
            new RgbColor(235, 188, 146));
        var idle = new SoftwareFrameBuffer(96, 96);
        var trick = new SoftwareFrameBuffer(96, 96);
        var repeat = new SoftwareFrameBuffer(96, 96);
        var idlePose = CleanRoomPlayerAnimator.Evaluate(10, "KICKOFF", BallOwner.Home, false);
        var trickPose = CleanRoomPlayerAnimator.Evaluate(10, "TRICK", BallOwner.Home, true);
        CleanRoomCharacterRenderer.Draw(idle, 48, 55, idlePose, palette);
        CleanRoomCharacterRenderer.Draw(trick, 48, 55, trickPose, palette);
        CleanRoomCharacterRenderer.Draw(repeat, 48, 55, idlePose, palette);
        Assert(idle.ComputeChecksum() == repeat.ComputeChecksum() &&
               idle.ComputeChecksum() != trick.ComputeChecksum() &&
               idle.CountDifferingPixels(trick) > 0,
            "Authored clean-room character asset was not deterministic or pose-sensitive.");
    }

    private static void TestCleanRoomCharacterAssets()
    {
        var manifest = CleanRoomCharacterAssetManifest.Default;
        Assert(manifest.IsComplete &&
               manifest.ManifestId == "clean-room-character-manifest-v1" &&
               manifest.SilhouetteAsset == "clean-room-character-silhouette-v1" &&
               manifest.PaletteAsset == "clean-room-character-palette-v1",
            "Authored character asset manifest is incomplete or unstable.");
    }

    private static void TestCleanRoomAuthoredAnimationTrace()
    {
        var homePose = CleanRoomPlayerAnimator.Evaluate(4, "TRICK", BallOwner.Home, true);
        var awayPose = CleanRoomPlayerAnimator.Evaluate(4, "TRICK", BallOwner.Away, false);
        var samples = new[]
        {
            new CleanRoomAuthoredAnimationSample(4, BallOwner.Home, "TRICK", true, homePose),
            new CleanRoomAuthoredAnimationSample(4, BallOwner.Away, "TRICK", false, awayPose),
        };
        var lines = CleanRoomAuthoredAnimationTraceSerializer.BuildLines(samples);
        var altered = lines.ToArray();
        altered[1] = altered[1].Replace("\t1.000000\t", "\t0.900000\t", StringComparison.Ordinal);
        Assert(lines[0] == CleanRoomAuthoredAnimationTraceSerializer.Header &&
               lines.Count == 3 &&
               lines[1].Contains("4\tHome\tTRICK\tTrue", StringComparison.Ordinal) &&
               lines[2].Contains("4\tAway\tTRICK\tFalse", StringComparison.Ordinal) &&
               CleanRoomAuthoredAnimationTraceSerializer.FindFirstDifference(lines, lines) == -1 &&
               CleanRoomAuthoredAnimationTraceSerializer.FindFirstDifference(lines, altered) == 1 &&
               CleanRoomAuthoredAnimationTraceSerializer.FindFirstDifference(lines, lines.Take(1).ToArray()) == 1,
            "Authored clean-room animation trace serialization/replay control failed.");
    }

    private static void TestFramebuffer()
    {
        var frame = new SoftwareFrameBuffer(4, 3);
        frame.Clear(new RgbColor(1, 2, 3));
        frame.FillRect(1, 1, 2, 1, new RgbColor(10, 20, 30));
        Assert(frame.GetPixel(0, 0) == new RgbColor(1, 2, 3), "Framebuffer clear mismatch.");
        Assert(frame.GetPixel(1, 1) == new RgbColor(10, 20, 30), "Framebuffer fill mismatch.");
        var bgra = new byte[4 * 3 * 4];
        frame.CopyToBgra32(bgra);
        Assert(bgra[0] == 3 && bgra[1] == 2 && bgra[2] == 1 && bgra[3] == 255, "Framebuffer BGRA conversion mismatch.");

        var rgba = new byte[]
        {
            255, 0, 0, 255, 0, 255, 0, 255,
            0, 0, 255, 255, 255, 255, 255, 255,
        };
        frame.BlitRgba(rgba, 2, 2, 2, 0, 2, 2);
        Assert(frame.GetPixel(2, 0) == new RgbColor(255, 0, 0), "Framebuffer RGBA blit mismatch.");

        var comparison = new SoftwareFrameBuffer(4, 3);
        comparison.Clear(new RgbColor(1, 2, 3));
        comparison.FillRect(1, 1, 2, 1, new RgbColor(10, 20, 30));
        comparison.FillRect(0, 0, 1, 1, new RgbColor(9, 9, 9));
        Assert(frame.CountDifferingPixels(comparison) == 5,
            "Framebuffer pixel-difference diagnostic mismatch.");
    }

    private static void TestPreview()
    {
        var texture = new PcTextureResource(
            0,
            0,
            1,
            1,
            "fixture",
            new byte[] { 255, 0, 0, 255 },
            new byte[] { 255, 0, 0, 255 });
        var frame = new SoftwareFrameBuffer(32, 24);
        PreviewRenderer.Render(new StreetMatch(), frame, texture);
        Assert(frame.GetPixel(16, 16) == new RgbColor(255, 0, 0),
            "Preview did not blit the supplied texture over the scene.");

        var candidateMesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    0,
                    "fixture",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);
        var candidateOverlayFrame = new SoftwareFrameBuffer(96, 64);
        var candidateHomeFrame = new SoftwareFrameBuffer(96, 64);
        var candidateAwayFrame = new SoftwareFrameBuffer(96, 64);
        var candidateBothFrame = new SoftwareFrameBuffer(96, 64);
        var candidateScene = new StreetMatch();
        PreviewRenderer.Render(
            candidateScene,
            candidateOverlayFrame,
            candidateMesh: candidateMesh,
            cleanRoomCharacters: true);
        PreviewRenderer.Render(
            candidateScene,
            candidateHomeFrame,
            candidateMesh: candidateMesh,
            cleanRoomCharacters: true,
            candidateHomeCharacter: true);
        PreviewRenderer.Render(
            candidateScene,
            candidateAwayFrame,
            candidateMesh: candidateMesh,
            cleanRoomCharacters: true,
            candidateAwayCharacter: true);
        PreviewRenderer.Render(
            candidateScene,
            candidateBothFrame,
            candidateMesh: candidateMesh,
            cleanRoomCharacters: true,
            candidateHomeCharacter: true,
            candidateAwayCharacter: true);
        Assert(candidateOverlayFrame.ComputeChecksum() != candidateHomeFrame.ComputeChecksum(),
            "Rigid candidate-home presentation did not differ from the diagnostic overlay path.");
        Assert(candidateHomeFrame.ComputeChecksum() != candidateAwayFrame.ComputeChecksum() &&
               candidateHomeFrame.ComputeChecksum() != candidateBothFrame.ComputeChecksum() &&
               candidateAwayFrame.ComputeChecksum() != candidateBothFrame.ComputeChecksum(),
            "Rigid candidate slot presentations did not remain distinguishable.");
        var meshFrame = new SoftwareFrameBuffer(64, 64);
        meshFrame.Clear(new RgbColor(0, 0, 0));
        EaglCandidateMeshRenderer.DrawSolid(meshFrame, candidateMesh, 32, 32, 20, new RgbColor(4, 5, 6));
        Assert(meshFrame.GetPixel(25, 34) == new RgbColor(4, 5, 6),
            "Candidate mesh solid rasterizer did not fill the projected triangle.");
        var xzMesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    0,
                    "fixture-xz",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 0, 1),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);
        var xzFrame = new SoftwareFrameBuffer(64, 64);
        xzFrame.Clear(new RgbColor(0, 0, 0));
        EaglCandidateMeshRenderer.DrawSolidProjection(
            xzFrame,
            xzMesh,
            32,
            32,
            20,
            new RgbColor(7, 8, 9),
            EaglCandidateProjectionPlane.XZ);
        EaglCandidateMeshRenderer.DrawWireframeProjection(
            xzFrame,
            xzMesh,
            32,
            32,
            20,
            new RgbColor(10, 11, 12),
            EaglCandidateProjectionPlane.XZ);
        Assert(xzFrame.GetPixel(25, 34) == new RgbColor(7, 8, 9),
            "Candidate mesh Cartesian projection did not reach the framebuffer.");
        EaglCandidateMeshRenderer.DrawWireframe(meshFrame, candidateMesh, 32, 32, 20, new RgbColor(9, 8, 7));
        Assert(meshFrame.GetPixel(32, 42) == new RgbColor(9, 8, 7),
            "Candidate mesh wireframe did not reach the framebuffer.");
        var profileFrame = new SoftwareFrameBuffer(64, 64);
        profileFrame.Clear(new RgbColor(0, 0, 0));
        var profileMesh = candidateMesh with
        {
            Groups = new[]
            {
                candidateMesh.Groups[0] with
                {
                    Slot1Attributes = new[]
                    {
                        new EaglFloat2(0, 0),
                        new EaglFloat2(1, 0),
                        new EaglFloat2(0, 1),
                    },
                },
            },
        };
        Assert(EaglCandidateMeshRenderer.DrawSlot1AttributeProfile(profileFrame, profileMesh, 32, 32, 20) == 1 &&
               profileFrame.ComputeChecksum() != new SoftwareFrameBuffer(64, 64).ComputeChecksum(),
            "Candidate mesh slot-1 attribute diagnostic did not reach the framebuffer.");
        var materialTexture = new PcShapeImageTexture(
            "fixture",
            0x7B,
            2,
            2,
            "PAL8",
            new byte[]
            {
                255, 0, 0, 255, 0, 255, 0, 255,
                0, 0, 255, 255, 255, 255, 255, 255,
            });
        var materialFrame = new SoftwareFrameBuffer(64, 64);
        materialFrame.Clear(new RgbColor(2, 3, 4));
        Assert(EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(materialFrame, profileMesh, materialTexture, 32, 32, 20) == 1 &&
               materialFrame.GetPixel(25, 34) != new RgbColor(2, 3, 4),
            "Candidate mesh texture sampling did not reach the framebuffer.");

        var renderCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            animationBanks: new[]
            {
                new AnimationBankAsset(
                    "BANK.axt",
                    "BANK",
                    1,
                    1,
                    true,
                    "BANK.ord",
                    "BANK.orl",
                    16,
                    8,
                    true,
                    true,
                    "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF",
                    null)
                {
                    VersionTag = "fixture-version",
                    Directory = "fixture-directory",
                    CatalogFiles = new[]
                    {
                        new AnimationCatalogEntry("fixture.ord", 7, "fixture.ord 7"),
                    },
                },
            });
        renderCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            animationBanks: renderCatalog.AnimationBanks,
            aptPackages: new[]
            {
                new AptPackageAsset(
                    "data/aptanims/fixture.bh",
                    "data/aptanims/fixture.big",
                    1,
                    4,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    1,
                    0,
                    true,
                    true,
                    true,
                    null),
            });
        renderCatalog = GameAssetCatalog.CreateForTest(
            Array.Empty<AudioStreamAsset>(),
            animationBanks: renderCatalog.AnimationBanks,
            aptPackages: renderCatalog.AptPackages,
            candidateMaterials: new[]
            {
                new CandidateMaterialAsset(
                    "tops0030.big",
                    "tops0030.ord",
                    "tops",
                    "tops0030.xsh",
                    256,
                    256,
                    "PAL8",
                    256 * 256 * 4),
            },
            morphSiblings: new[]
            {
                new MorphSiblingAsset(
                    "fixture.big",
                    2,
                    2,
                    2,
                    2,
                    2,
                    2,
                    1,
                    null),
            });
        var gameplayRenderer = new SoftwareGameplayRenderer(64, 48, texture);
        var renderRuntime = new GameRuntime(
            renderCatalog,
            gameplayRenderer,
            new CatalogAudioMixer(renderCatalog),
            new HeadlessInputSource());
        renderRuntime.Start();
        renderRuntime.Tick(1.0 / 60.0);
        Assert(gameplayRenderer.RenderedFrameCount == 1 && gameplayRenderer.LastFrameChecksum != 0,
            "GameRuntime did not dispatch the match to the software gameplay renderer.");
        var runtimeReport = renderRuntime.Report();
        Assert(runtimeReport.Contains("animation_banks: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_complete_pairs: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_elf_validated: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_symbol_matches: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_catalog_file_entries: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_catalog_unique_file_names: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_catalog_version_tags: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("animation_banks_distinct_normalized_payloads: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("apt_packages: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("apt_valid_entries: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("apt_complete_companion_sets: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("apt_structural_signatures: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("candidate_material_matches: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("candidate_material_textures_decoded: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("candidate_material_pal8: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("morph_sibling_nested_archives: 1", StringComparison.Ordinal) &&
               runtimeReport.Contains("morph_sibling_complete_pairs: 2", StringComparison.Ordinal) &&
               runtimeReport.Contains("morph_pose_palette_bank_symbols: 1", StringComparison.Ordinal),
            "GameRuntime did not report structural animation/material catalog evidence.");

        var solidCandidateRenderer = new SoftwareGameplayRenderer(
            128,
            96,
            candidateMesh: profileMesh);
        var texturedCandidateRenderer = new SoftwareGameplayRenderer(
            128,
            96,
            candidateMesh: profileMesh,
            candidateTexture: materialTexture);
        var solidRuntime = new GameRuntime(
            renderCatalog,
            solidCandidateRenderer,
            new CatalogAudioMixer(renderCatalog),
            new HeadlessInputSource());
        var texturedRuntime = new GameRuntime(
            renderCatalog,
            texturedCandidateRenderer,
            new CatalogAudioMixer(renderCatalog),
            new HeadlessInputSource());
        solidRuntime.Start();
        texturedRuntime.Start();
        solidRuntime.Tick(1.0 / 60.0);
        texturedRuntime.Tick(1.0 / 60.0);
        Assert(texturedCandidateRenderer.RenderedFrameCount == 1 &&
               texturedCandidateRenderer.LastFrameChecksum != solidCandidateRenderer.LastFrameChecksum,
            "Software gameplay renderer did not keep the candidate texture path opt-in and distinct from solid fallback.");
    }

    private static void TestEaglGeometryAudit()
    {
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    0,
                    "fixture",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                        new EaglMeshVertex(0.001f, 0.001f, 0),
                    },
                    new[]
                    {
                        new EaglMeshTriangle(0, 1, 2),
                        new EaglMeshTriangle(0, 0, 3),
                        new EaglMeshTriangle(0, 1, 99),
                    }),
            },
            0);

        var audit = EaglCandidateGeometryAuditor.Measure(mesh, pixelSize: 20);
        Assert(audit.GroupCount == 1 &&
               audit.VertexCount == 4 &&
               audit.TriangleCount == 3 &&
               audit.InvalidIndexTriangles == 1 &&
               audit.RepeatedIndexTriangles == 1 &&
               audit.ExactZeroArea3dTriangles == 1 &&
               audit.ExactZeroAreaProjectedTriangles == 1 &&
               audit.RasterDegenerateTriangles == 1 &&
               audit.RasterVisibleTriangles == 1 &&
               audit.NonFiniteVertices == 0 &&
               !audit.HasOnlyFiniteInRangeGeometry,
             "EAGL candidate geometry audit did not preserve objective invalid/degenerate counts.");
    }

    private static void TestCandidateRenderInfluenceAudit()
    {
        var audit = CandidateRenderInfluenceAudit.Audit(new[]
        {
            new CandidateRenderInfluenceRow(
                "fixture.big",
                "fixture.ord",
                "tops",
                2,
                1,
                4,
                3,
                7,
                3,
                true,
                true,
                true,
                false,
                0x10UL,
                0x20UL,
                true,
                "verified-candidate-raster-influence",
                string.Empty),
        });
        var lines = CandidateRenderInfluenceAudit.BuildLines(audit);
        Assert(audit.Summary.Status == "verified-authored" &&
               audit.Summary.CandidateCount == 1 &&
               audit.Summary.VerifiedCount == 1 &&
               audit.Summary.GeometryValidCount == 1 &&
               audit.Summary.TextureInfluenceCount == 1 &&
               audit.Summary.IsolatedTextureInfluenceCount == 1 &&
               audit.Summary.OriginalSemantics == "not-claimed" &&
               lines.Count == 3 &&
               lines[0] == CandidateRenderInfluenceAudit.Header &&
               lines[1].Contains("verified-candidate-raster-influence", StringComparison.Ordinal) &&
               lines[2].Contains("summary\tall", StringComparison.Ordinal),
            "Candidate render influence audit did not preserve candidate-only raster evidence.");
    }

    private static void TestMorphCandidateGeometryAudit()
    {
        static EaglMorphSiblingEvidence Evidence(string layout, string mesh, string geometry) =>
            new(
                "fixture_morph.o",
                "fixture",
                "fixture.ord",
                true,
                true,
                1,
                true,
                "companion",
                "companion-layout",
                "command",
                "vertex",
                "record",
                "numeric",
                mesh,
                1,
                2,
                0,
                "0x0",
                "0x0",
                layout,
                "payload",
                "windows=1",
                "numeric-match",
                null)
            {
                SiblingCandidateGeometrySignature = geometry,
            };

        var rows = MorphCandidateGeometryAudit.Audit(new[]
        {
            new MorphCandidateGeometryObservation("a.big", Evidence("layout", "mesh-a", "geometry-a")),
            new MorphCandidateGeometryObservation("b.big", Evidence("layout", "mesh-a", "geometry-b")),
            new MorphCandidateGeometryObservation("c.big", Evidence("stable", "mesh-b", "geometry-c")),
        });
        var summaries = MorphCandidateGeometryAudit.Summarize(rows);
        var variable = rows.Single(row => row.LayoutKey == "layout");
        var all = summaries.Single(row => row.Scope == "all");
        var correlationRows = MorphCandidateGeometryAudit.Correlate(new[]
        {
            new MorphCandidateGeometryObservation("a.big", Evidence("layout", "mesh-a", "geometry-a")),
            new MorphCandidateGeometryObservation("b.big", Evidence("layout", "mesh-a", "geometry-b")),
            new MorphCandidateGeometryObservation("c.big", Evidence("stable", "mesh-b", "geometry-c")),
        });
        var correlation = correlationRows.Single(row => row.LayoutKey == "layout");
        var correlationSummary = MorphCandidateGeometryAudit.SummarizeCorrelation(correlationRows)
            .Single(row => row.Scope == "all");
        Assert(rows.Count == 2 &&
               variable.CandidateMeshProfiles == 1 &&
               variable.CandidateGeometryProfiles == 2 &&
               variable.CandidateMeshStableGeometryVariable &&
               variable.CandidateGeometryVariable &&
               variable.CandidateGeometrySetSha256.Length == 64 &&
               all.Layouts == 2 &&
               all.Rows == 3 &&
               all.CandidateGeometryVariableLayouts == 1 &&
               all.CandidateMeshStableGeometryVariableLayouts == 1 &&
               all.MissingGeometryLayouts == 0 &&
               correlation.PayloadWindowProfiles == 1 &&
               correlation.CandidateGeometryProfiles == 2 &&
               correlation.PayloadGeometryPairProfiles == 2 &&
               correlation.PayloadStableGeometryVariable &&
               !correlation.BothPayloadAndGeometryVariable &&
               correlationSummary.PayloadStableGeometryVariableLayouts == 1 &&
               correlationSummary.PayloadGeometryPairVariableLayouts == 1,
            "Morph candidate geometry audit did not separate topology and numeric geometry profiles.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-candidate-geometry-test-");
        try
        {
            var reportPath = Path.Combine(temporaryDirectory.FullName, "geometry.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            var correlationReportPath = Path.Combine(temporaryDirectory.FullName, "correlation.tsv");
            var correlationSummaryPath = Path.Combine(temporaryDirectory.FullName, "correlation-summary.tsv");
            MorphCandidateGeometryAudit.WriteReports(reportPath, summaryPath, rows);
            MorphCandidateGeometryAudit.WriteCorrelationReports(
                correlationReportPath,
                correlationSummaryPath,
                correlationRows);
            var reportLines = File.ReadAllLines(reportPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            var correlationReportLines = File.ReadAllLines(correlationReportPath);
            var correlationSummaryLines = File.ReadAllLines(correlationSummaryPath);
            Assert(reportLines.Length == 3 &&
                   summaryLines.Length == 4 &&
                   correlationReportLines.Length == 3 &&
                   correlationSummaryLines.Length == 4 &&
                   reportLines[0].StartsWith("morph_structural_layout\t", StringComparison.Ordinal) &&
                   reportLines[1].Contains("a.big/fixture_morph.o", StringComparison.Ordinal) &&
                   correlationReportLines[0].Contains("payload_geometry_pair_profiles", StringComparison.Ordinal),
                "Morph candidate geometry audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestMorphCandidateGeometryAmbiguityAudit()
    {
        var catalogRows = new[]
        {
            new MorphCandidateGeometryAmbiguityCatalogRow("stable-a.big", "stable-a.o", "layout-stable", "geometry-stable"),
            new MorphCandidateGeometryAmbiguityCatalogRow("stable-b.big", "stable-b.o", "layout-stable", "geometry-stable"),
            new MorphCandidateGeometryAmbiguityCatalogRow("ambiguous-a.big", "ambiguous-a.o", "layout-ambiguous", "geometry-a"),
            new MorphCandidateGeometryAmbiguityCatalogRow("ambiguous-b.big", "ambiguous-b.o", "layout-ambiguous", "geometry-b"),
        };
        var reportRows = new[]
        {
            new MorphAmbiguityReportRow("layout-stable", 2, false, string.Empty, string.Empty),
            new MorphAmbiguityReportRow("layout-ambiguous", 2, true, "candidate_geometry", string.Empty),
        };

        var auditRows = MorphCandidateGeometryAmbiguityAudit.Audit(catalogRows, reportRows);
        var summaries = MorphCandidateGeometryAmbiguityAudit.Summarize(auditRows);
        var ambiguous = auditRows.Single(row => row.LayoutKey == "layout-ambiguous");
        var all = summaries.Single(row => row.Scope == "all");
        Assert(auditRows.Count == 2 &&
               ambiguous.CandidateGeometryProfiles == 2 &&
               ambiguous.CandidateGeometryVariable &&
               ambiguous.GeometryVariableMatchesAmbiguityLabel &&
               ambiguous.ReportRowCountMatches &&
               all.GeometryVariableLayouts == 1 &&
               all.GeometryVariableAmbiguousLayouts == 1 &&
               all.GeometryVariableStableLayouts == 0 &&
               all.AmbiguousWithoutGeometryVariableLayouts == 0 &&
               all.GeometryVariableLabelMismatches == 0 &&
               all.MissingGeometryLayouts == 0,
            "Morph candidate geometry ambiguity audit did not preserve label comparison.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-morph-candidate-geometry-ambiguity-test-");
        try
        {
            var auditPath = Path.Combine(temporaryDirectory.FullName, "audit.tsv");
            var summaryPath = Path.Combine(temporaryDirectory.FullName, "summary.tsv");
            MorphCandidateGeometryAmbiguityAudit.WriteReports(auditPath, summaryPath, auditRows);
            var auditLines = File.ReadAllLines(auditPath);
            var summaryLines = File.ReadAllLines(summaryPath);
            Assert(auditLines.Length == 3 &&
                   summaryLines.Length == 4 &&
                   auditLines[0].Contains("candidate_geometry_variable", StringComparison.Ordinal) &&
                   summaryLines[0].Contains("geometry_variable_ambiguous_layouts", StringComparison.Ordinal),
                "Morph candidate geometry ambiguity audit TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglCandidateAttributeAudit()
    {
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    3,
                    "fixture",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                    },
                    Array.Empty<EaglMeshTriangle>())
                {
                    Slot1Attributes = new[]
                    {
                        new EaglFloat2(0, 0),
                        new EaglFloat2(1, 1),
                    },
                },
            },
            0);
        var primitive = new EaglModelPrimitive(
            0,
            3,
            0,
            0,
            0,
            null,
            new[]
            {
                new EaglStateAssignment("SHAPENAME", "tops,1"),
                new EaglStateAssignment("XBOXEXTOBJ_SetClampU", "EAGL::XBOXCM_CLAMP"),
            },
            null,
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            true,
            new EaglRenderCommandStream(
                Array.Empty<EaglRenderCommandEvidence>(),
                true,
                null));
        var row = EaglCandidateAttributeAuditor.Measure(
            "fixture.big",
            "fixture.ord",
            "tops",
            mesh,
            new[] { primitive });

        Assert(row.Groups == 1 &&
               row.Slot1CompleteGroups == 1 &&
               row.Slot1FiniteGroups == 1 &&
               row.Slot1NearUnitGroups == 1 &&
               row.Slot1ValueCount == 2 &&
               row.GroupShapeStateCount == 0 &&
               row.GroupClampStateCount == 0 &&
               row.PrimitiveShapeStateCount == 1 &&
               row.PrimitiveClampStateCount == 1 &&
               row.Status == "complete-slot1-profile" &&
               row.Slot1ValueSha256.Length == 64,
            "EAGL candidate attribute audit did not preserve separate slot/state populations.");
    }

    private static void TestEaglPrimitiveStateCorrespondence()
    {
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    2,
                    "fixture",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);
        var geometry = new EaglModelPrimitive(
            0,
            2,
            0,
            0,
            0,
            null,
            Array.Empty<EaglStateAssignment>(),
            "PT_TRIANGLESTRIP",
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 1, new byte[] { 1, 2, 3, 4 }, new[] { "0x10:SharedState:type=2" }),
                },
                true,
                null));
        var state = new EaglModelPrimitive(
            0,
            7,
            0,
            0,
            0,
            "__EAGL::TAR:::SHAPENAME=tops,1",
            new[] { new EaglStateAssignment("SHAPENAME", "tops,1") },
            null,
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 1, new byte[] { 1, 2, 3, 8 }, new[] { "0x20:SharedState:type=2" }),
                },
                true,
                null));

        var rows = EaglPrimitiveStateCorrespondence.Compare(
            "fixture.big",
            "fixture.ord",
            mesh,
            new[] { geometry, state });
        Assert(rows.Count == 1 &&
               rows[0].GeometryPrimitiveIndex == 2 &&
               rows[0].StatePrimitiveIndex == 7 &&
               rows[0].StateNames == "SHAPENAME=tops,1" &&
               rows[0].CommonPrefixBytes == 3 &&
               !rows[0].ExactRawBytes &&
               rows[0].ExactBoundaryShape &&
               rows[0].SharedCommandIdCount == 1 &&
               rows[0].SharedCommandIds == "9=1" &&
               rows[0].SharedRelocationSymbolCount == 1 &&
               rows[0].SharedRelocationSymbols == "SharedState=1" &&
               EaglPrimitiveStateCorrespondence.GetPairSetSha256(rows).Length == 64,
            "EAGL primitive/state correspondence mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-state-correspondence-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "correspondence.tsv");
            EaglPrimitiveStateCorrespondence.WriteReport(outputPath, rows);
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 2 &&
                   outputLines[0].StartsWith("model_archive\t", StringComparison.Ordinal) &&
                   outputLines[1].Contains("fixture.ord", StringComparison.Ordinal),
                "EAGL primitive/state correspondence TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglStateGeometryProfileAudit()
    {
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    2,
                    "fixture",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);
        var geometry = new EaglModelPrimitive(
            0,
            2,
            0,
            0,
            0,
            null,
            Array.Empty<EaglStateAssignment>(),
            "PT_TRIANGLESTRIP",
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 1, new byte[] { 1, 2, 3, 4 }, Array.Empty<string>()),
                },
                true,
                null));
        var matchingState = new EaglModelPrimitive(
            0,
            7,
            0,
            0,
            0,
            "__EAGL::TAR:::SHAPENAME=tops,1",
            new[] { new EaglStateAssignment("SHAPENAME", "tops,1") },
            null,
            Array.Empty<EaglVertexBufferStream>(),
            Array.Empty<EaglVertexFetchSlot>(),
            Array.Empty<EaglVertexFetchBinding>(),
            false,
            new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 9, 1, new byte[] { 1, 2, 3, 8 }, Array.Empty<string>()),
                },
                true,
                null));
        var differentState = matchingState with
        {
            PrimitiveIndex = 8,
            RenderCommandStream = new EaglRenderCommandStream(
                new[]
                {
                    new EaglRenderCommandEvidence(0, 0, 10, 1, new byte[] { 1, 2, 3, 8 }, Array.Empty<string>()),
                },
                true,
                null),
        };

        var audit = EaglStateGeometryProfileAudit.Measure(
            "fixture.big",
            "fixture.ord",
            "tops",
            mesh,
            new[] { geometry, matchingState, differentState });
        Assert(audit.Rows.Count == 2 &&
               audit.Summary.GeometryGroups == 1 &&
               audit.Summary.StatePrimitives == 2 &&
               audit.Summary.SameLayerPairs == 2 &&
               audit.Summary.SamePrimitiveIndexPairs == 0 &&
               audit.Summary.NeighborPrimitiveIndexPairs == 0 &&
               audit.Summary.ExactCommandShapePairs == 1 &&
               audit.Summary.UniqueStateProfileRows == 2 &&
               audit.Rows[0].CommonPrefixBytes == 3 &&
               audit.Rows[0].StateProfileTieCount == 1 &&
               audit.Rows[0].Status == "opaque-profile-control",
            "EAGL state/geometry opaque profile audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-state-geometry-profile-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "profile.tsv");
            EaglStateGeometryProfileAudit.WriteReport(outputPath, audit.Rows);
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 3 &&
                   outputLines[0].StartsWith("model_archive\t", StringComparison.Ordinal) &&
                   outputLines[1].Contains("fixture.ord", StringComparison.Ordinal),
                "EAGL state/geometry opaque profile TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestEaglStateGeometryOrderAudit()
    {
        var mesh = new EaglCandidateMesh(
            new[]
            {
                new EaglCandidateMeshGroup(
                    0,
                    0,
                    "fixture-0",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 0),
                        new EaglMeshVertex(1, 0, 0),
                        new EaglMeshVertex(0, 1, 0),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
                new EaglCandidateMeshGroup(
                    0,
                    4,
                    "fixture-4",
                    new[]
                    {
                        new EaglMeshVertex(0, 0, 1),
                        new EaglMeshVertex(1, 0, 1),
                        new EaglMeshVertex(0, 1, 1),
                    },
                    new[] { new EaglMeshTriangle(0, 1, 2) }),
            },
            0);

        static EaglModelPrimitive Primitive(
            uint index,
            string? stateSymbol,
            bool complete = true) =>
            new(
                0,
                index,
                0,
                0,
                0,
                stateSymbol,
                EaglModelReader.ParseStateAssignments(stateSymbol),
                null,
                Array.Empty<EaglVertexBufferStream>(),
                Array.Empty<EaglVertexFetchSlot>(),
                Array.Empty<EaglVertexFetchBinding>(),
                false,
                new EaglRenderCommandStream(
                    new[]
                    {
                        new EaglRenderCommandEvidence(
                            0,
                            0,
                            9,
                            1,
                            new byte[] { 1, 2, 3, checked((byte)index) },
                            Array.Empty<string>()),
                    },
                    complete,
                    complete ? null : "synthetic incomplete stream"));

        var primitives = new[]
        {
            Primitive(0, null),
            Primitive(1, "__EAGL::TAR:::SHAPENAME=tops,1"),
            Primitive(2, "__EAGL::TAR:::SHAPENAME=tops,1"),
            Primitive(3, "__EAGL::TAR:::SHAPENAME=other,1"),
            Primitive(4, null),
            Primitive(5, "__EAGL::TAR:::SHAPENAME=tops,1", complete: false),
        };
        var audit = EaglStateGeometryOrderAudit.Measure(
            "fixture.big",
            "fixture.ord",
            "tops",
            mesh,
            primitives);

        Assert(audit.Rows.Count == 3 &&
               audit.Summary.GeometryCandidateCount == 2 &&
               audit.Summary.StatePrimitiveCount == 3 &&
               audit.Summary.IntervalCount == 3 &&
               audit.Summary.NonEmptyIntervalCount == 2 &&
               audit.Summary.StatePrimitivesPartitioned == 3 &&
               audit.Summary.StatePrimitivesUnpartitioned == 0 &&
               audit.Summary.CompleteStateIntervals == 1 &&
               audit.Rows[0].IntervalKind == "before-first-geometry" &&
               audit.Rows[1].IntervalKind == "between-geometry-candidates" &&
               audit.Rows[1].StatePrimitiveIndices == "1,2" &&
               audit.Rows[1].AllStateStreamsComplete &&
               audit.Rows[2].IntervalKind == "after-last-geometry" &&
               audit.Rows[2].StatePrimitiveIndices == "5" &&
               !audit.Rows[2].AllStateStreamsComplete &&
               audit.Rows.All(row => row.Status == "ordered-index-control"),
            "EAGL state/geometry ordered-run audit mismatch.");

        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-state-geometry-order-test-");
        try
        {
            var outputPath = Path.Combine(temporaryDirectory.FullName, "order.tsv");
            EaglStateGeometryOrderAudit.WriteReport(outputPath, audit.Rows);
            var outputLines = File.ReadAllLines(outputPath);
            Assert(outputLines.Length == 4 &&
                   outputLines[0].StartsWith("model_archive\t", StringComparison.Ordinal) &&
                   outputLines[2].Contains("between-geometry-candidates", StringComparison.Ordinal),
                "EAGL state/geometry ordered-run TSV mismatch.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private static void TestProjectReadinessAudit()
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory("fifa-readiness-test-");
        try
        {
            var isoPath = Path.Combine(temporaryDirectory.FullName, "fixture.iso");
            File.WriteAllBytes(isoPath, new byte[] { 1, 2, 3, 4 });
            var audioPayloadFixture = BuildPcmSchlFixture();
            using var audioPayloadFixtureStream = new MemoryStream(audioPayloadFixture, writable: false);
            var audioPayloadInfo = EaSchlAudioDecoder.Inspect(audioPayloadFixtureStream);
            var audioPayloadRow = new AudioOffsetRow(1, 0, audioPayloadFixture.Length, 0);
            var audioPayloadCandidate = new AudioEventCandidateAsset(
                "event.evt",
                "header.big",
                "data.big",
                new AudioEventNamedHeaderCandidate("fixture-event", 16, 0, audioPayloadRow, audioPayloadRow, 1),
                audioPayloadInfo,
                null);
            var catalog = GameAssetCatalog.CreateForTest(
                Array.Empty<AudioStreamAsset>(),
                audioBanks: new[]
                {
                    new AudioBankAsset(
                        "bank.abk",
                        1,
                        true,
                        1,
                        1,
                        1,
                        0,
                        false,
                        false,
                        0,
                        0,
                        0,
                        0,
                        Array.Empty<AudioAbkAstRange>(),
                        null,
                        null),
                },
                audioEventCandidates: new[] { audioPayloadCandidate },
                candidateMaterials: new[]
                {
                    new CandidateMaterialAsset("model.big", "object.ord", "tops", "texture.xsh", 2, 2, "PAL8", 16),
                },
                animationBanks: new[]
                {
                    new AnimationBankAsset("bank.axt", "bank", 1, 1, true, "bank.ord", "bank.orl", 4, 4, true, true, "sig", null)
                    {
                        CatalogFiles = new[]
                        {
                            new AnimationCatalogEntry("idle", 100, "idle 100"),
                        },
                    },
                });

            var summary = ProjectReadinessAudit.Build(catalog, isoPath, temporaryDirectory.FullName);
            Assert(!summary.Ready, "Readiness audit unexpectedly passed an incomplete fixture.");
            Assert(summary.Rows.Any(row => row.Area == "rendering" && !row.GatePassed),
                "Readiness audit did not retain the unproven rendering gate.");
            Assert(summary.Rows.Any(row => row.Area == "rendering-authored-windowed-runtime" && !row.GatePassed),
                "Readiness audit unexpectedly accepted a missing Win32 framebuffer capture.");
            Assert(summary.Rows.Any(row => row.Area == "animation-deformation" && row.Status == "structural-only"),
                "Readiness audit did not preserve structural-only animation status.");
            var animationSummaryDirectory = Path.Combine(temporaryDirectory.FullName, "fifa_pc");
            Directory.CreateDirectory(animationSummaryDirectory);
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-named-event-runtime-summary.tsv"),
                new[]
                {
                    "field\tvalue",
                    "candidate_count\t1",
                    "candidate_metadata_validated\t1",
                    "runtime_prepared\t1",
                    "wave_headers_valid\t1",
                    "pcm_bytes\t2",
                    "status\tverified-runtime-named-payload",
                    $"provenance\t{AudioNamedEventRuntimeAudit.Provenance}",
                    $"original_semantics\t{AudioNamedEventRuntimeAudit.OriginalSemantics}",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-parallel-range-summary.tsv"),
                new[]
                {
                    AudioEventParallelRangeAuditSummary.Header,
                    "candidate_count\t1",
                    "event_string_anchor_valid\t1",
                    "positive_occurrence_count\t1",
                    "header_range_valid\t1",
                    "data_range_valid\t1",
                    "parallel_row_index_valid\t1",
                    "data_schl_prefix\t1",
                    "unique_event_names\t1",
                    "unique_event_string_offsets\t1",
                    "unique_header_rows\t1",
                    "header_table_rows\t1",
                    "data_table_rows\t1",
                    "header_table_structure_valid\tTrue",
                    "data_table_structure_valid\tTrue",
                    "verified_rows\t1",
                    $"status\t{AudioEventParallelRangeAudit.Status}",
                    $"provenance\t{AudioEventParallelRangeAudit.Provenance}",
                    $"direct_selection_binding\t{AudioEventParallelRangeAudit.DirectSelectionBinding}",
                    $"original_semantics\t{AudioEventParallelRangeAudit.OriginalSemantics}",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-xbe-hash-search-summary.tsv"),
                new[]
                {
                    AudioEventXbeHashSearchSummary.Header,
                    "candidate_count\t1",
                    "unique_event_names\t1",
                    "unique_hash_words\t1",
                    "xbe_bytes\t16",
                    "aligned_occurrences\t0",
                    "unaligned_occurrences\t0",
                    "hash_words_with_aligned_hits\t0",
                    "hash_words_with_unaligned_hits\t0",
                    $"status\t{AudioEventXbeHashSearchAudit.Status}",
                    $"provenance\t{AudioEventXbeHashSearchAudit.Provenance}",
                    $"direct_selection_binding\t{AudioEventXbeHashSearchAudit.DirectSelectionBinding}",
                    $"original_semantics\t{AudioEventXbeHashSearchAudit.OriginalSemantics}",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-partition-control-summary.tsv"),
                new[]
                {
                    "adjacency_source_report\tprofile_source_report\torder_source_report\tadjacency_rows\tprofile_rows\torder_rows\tjoined_object_rows\tadjacency_input_status_valid_rows\tprofile_input_status_valid_rows\torder_input_status_valid_rows\tobject_state_exact_rows\tgeometry_group_state_exact_rows\tprofile_geometry_pair_rows\tprofile_state_pair_rows\tprofile_rows_reconciled\torder_partitioned_states\tadjacency_states\torder_states\tstate_index_set_match_rows\tinterval_index_sequence_match_rows\tinterval_boundary_shape_match_rows\tcomplete_profile_stream_rows\texact_command_shape_rows\texact_command_shape_expected_zero\tstatus\tprovenance\toriginal_semantics",
                    "adjacency.tsv\tprofile.tsv\torder.tsv\t1\t2\t2\t1\t1\t2\t2\t1\t0\t1\t1\t1\t3\t3\t3\t1\t1\t1\t2\t0\tTrue\tverified-independent-original-state-geometry-partition-control\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-global-assignment-control-summary.tsv"),
                new[]
                {
                    "order_source_report\tpartition_source_report\torder_rows\tpartition_rows\tjoined_object_rows\tmissing_partition_objects\torder_input_status_valid_rows\tpartition_input_status_valid_rows\tnonempty_interval_rows\tambiguous_interval_rows\tobjects_with_multiple_assignment_models\tobjects_with_unique_assignment_model\tobjects_with_one_to_one_unique_model\tunconstrained_assignment_model_sum\tone_to_one_assignment_model_sum\tstate_index_set_reconciled_rows\tunique_state_geometry_assignment_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "order.tsv\tpartition.tsv\t2\t1\t1\t0\t2\t1\t1\t0\t0\t1\t1\t1\t1\t1\t0\t1\tverified-independent-original-state-geometry-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-uv-material-global-assignment-control-summary.tsv"),
                new[]
                {
                    "global_source_report\tuv_source_report\tmaterial_source_report\tglobal_rows\tuv_rows\tmaterial_rows\tjoined_object_rows\tmissing_uv_objects\tmissing_material_objects\tglobal_input_status_valid_rows\tuv_candidate_attribute_valid_rows\tuv_candidate_raster_influence_valid_rows\tuv_complete_group_rows\tuv_incomplete_group_rows\tuv_slot1_near_unit_group_rows\tuv_other_slot_near_unit_group_rows\tmaterial_input_status_valid_rows\tmaterial_candidate_asset_valid_rows\tmaterial_state_candidate_source_complete_rows\tobjects_with_multiple_geometry_assignment_models\tobjects_with_single_geometry_assignment_model\tobjects_with_single_candidate_uv_role_domain\tobjects_with_single_candidate_material_domain\tstructural_cross_product_model_sum\tobjects_with_multiple_structural_cross_product_models\tobjects_with_single_structural_cross_product_model\tunique_state_geometry_uv_material_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "global.tsv\tuv.tsv\tmaterial.tsv\t2\t1\t2\t1\t0\t0\t1\t1\t1\t1\t0\t1\t0\t1\t1\t1\t0\t1\t1\t1\t1\t0\t1\t0\t1\tverified-independent-original-state-geometry-uv-material-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-candidate-render-global-assignment-control-summary.tsv"),
                new[]
                {
                    "global_source_report\truntime_source_report\tcandidate_source_report\tpayload_join_source_report\tglobal_rows\truntime_rows\tcandidate_rows\tpayload_join_rows\tjoined_object_rows\tmissing_runtime_objects\tmissing_candidate_objects\tmissing_payload_join_objects\tglobal_input_status_valid_rows\truntime_input_status_valid_rows\tcandidate_input_status_valid_rows\tpayload_join_input_status_valid_rows\tgeometry_candidate_count_sum\tcandidate_geometry_group_sum\tgeometry_candidate_cardinality_match_rows\tobject_state_exact_rows\trender_group_state_match_rows\tcandidate_source_chain_valid_rows\tpayload_object_join_valid_rows\tobjects_with_multiple_geometry_assignment_models\tobjects_with_single_geometry_assignment_models\tunconstrained_assignment_model_sum\tone_to_one_assignment_model_sum\tunique_state_geometry_candidate_render_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "global.tsv\truntime.tsv\tcandidate.tsv\tpayload.tsv\t2\t2\t2\t2\t1\t0\t0\t0\t1\t1\t1\t1\t2\t2\t1\t1\t0\t1\t1\t0\t1\t1\t1\t0\t1\tverified-independent-original-state-geometry-candidate-render-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-sample-global-assignment-control-summary.tsv"),
                new[]
                {
                    "payload_source_report\tcsi_source_report\tparallel_source_report\tnegative_synthesis_source_report\tauthored_route_source_report\tevent_rows\tbank_rows\tdecoded_event_rows\tdecoded_bank_rows\tevent_input_status_valid_rows\tcsi_input_status_valid_rows\tparallel_input_status_valid_rows\tnegative_synthesis_valid_source_count\tauthored_route_rows\tbank_candidate_pool_size\tevent_exact_pcm_match_rows\tevent_hashes_with_match\tevt_rows_with_zero_exact_sample_models\tevt_rows_with_single_weak_name_models\tevt_rows_with_multiple_weak_name_models\tevt_rows_with_zero_weak_name_models\tweak_name_domain_sum\texact_identity_model_sum\tstructural_cross_product_model_sum\tunique_evt_abkc_sample_binding_proven_rows\tvalid_event_rows\tstatus\tsemantic_binding\tprovenance\tdirect_selection_binding\toriginal_semantics",
                    "payload.tsv\tcsi.tsv\tparallel.tsv\tnegative.tsv\tauthored.tsv\t17\t242\t17\t241\t17\t17\t17\t5\t15\t241\t0\t0\t17\t3\t4\t10\t38\t0\t0\t0\t17\tverified-independent-original-audio-event-sample-global-assignment-control\tnot-proven\tindependent-test\tnot-proven\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-static-context-global-assignment-control-summary.tsv"),
                new[]
                {
                    "static_report\tstatic_summary_report\tevent_path_summary_report\tevent_payload_summary_report\tstatic_audio_xref_rows\tunique_audio_literals\taudio_context_rows\taudio_context_with_executable_calls\tdistinct_executable_call_targets\taudio_targets_with_multiple_literals\tnested_audio_literal_matches\txdvdfs_audio_literal_matches\taudio_event_literal_xref_rows\tevent_path_candidate_name_count\tevent_path_candidate_pairs\tevent_path_literal_rows\tevent_path_literal_occurrences\tevent_path_literal_xref_count\tevent_path_matched_rows\tevent_path_distinct_matched_paths\tevent_rows\tbank_candidate_pool_size\tevent_exact_pcm_match_rows\tevent_hashes_with_match\tunique_evt_abkc_sample_binding_proven_rows\tevent_rows_without_static_event_xref\tstatic_event_context_target_pool_size\tevent_static_candidate_model_sum_before\tevent_static_candidate_model_sum_after\tevent_static_context_reduced_rows\tunique_event_static_selector_proven_rows\tvalid_event_rows\tstatus\tsemantic_binding\tdirect_selection_binding\tprovenance\toriginal_semantics",
                    "static.tsv\tstatic-summary.tsv\tpath.tsv\tpayload.tsv\t58\t23\t147\t108\t56\t14\t1\t10\t0\t17\t1061\t4\t9\t0\t4\t1\t17\t241\t0\t0\t0\t17\t56\t952\t952\t0\t0\t17\tverified-independent-audio-event-static-context-global-assignment-control\tnot-proven\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-payload-identity-global-assignment-control-summary.tsv"),
                new[]
                {
                    "global_source_report\tpayload_identity_source_report\tglobal_rows\tpayload_identity_rows\tjoined_object_rows\tmissing_payload_identity_objects\tglobal_input_status_valid_rows\tpayload_identity_input_status_valid_rows\tgeometry_candidate_count_sum\tpayload_identity_geometry_row_sum\tpayload_identity_complete_geometry_sum\tpayload_identity_raw_hash_row_sum\tpayload_identity_cardinality_match_rows\tpayload_identity_reduced_state_domain_rows\tobjects_with_multiple_assignment_models\tobjects_with_single_assignment_models\tassignment_model_sum_before_payload_identity\tassignment_model_sum_after_payload_identity\tone_to_one_model_sum_before_payload_identity\tone_to_one_model_sum_after_payload_identity\tunique_state_geometry_payload_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "global.tsv\tpayload.tsv\t2\t2\t1\t0\t1\t1\t2\t2\t2\t10\t1\t0\t0\t1\t1\t1\t1\t1\t0\t1\tverified-independent-original-state-geometry-payload-identity-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-draw-classification-global-assignment-control-summary.tsv"),
                new[]
                {
                    "global_source_report\tdraw_classification_source_report\tpayload_identity_source_report\tglobal_rows\tdraw_classification_rows\tpayload_identity_rows\tjoined_object_rows\tmissing_draw_classification_objects\tmissing_payload_identity_objects\tglobal_input_status_valid_rows\tdraw_classification_input_status_valid_rows\tpayload_identity_input_status_valid_rows\tgeometry_candidate_count_sum\tdraw_classification_geometry_marker_sum\tpayload_identity_geometry_row_sum\tclassification_match_rows\tgeometry_draw_payload_identity_join_rows\tstate_marker_rows\tstate_rows_without_payload_identity\tobjects_with_state_over_geometry_rows\tassignment_model_sum_before_draw_classification\tassignment_model_sum_after_draw_classification\tone_to_one_model_sum_before_draw_classification\tone_to_one_model_sum_after_draw_classification\tdraw_classification_reduced_state_domain_rows\tunique_state_geometry_draw_binding_proven_rows\tvalid_object_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "global.tsv\tdraw.tsv\tpayload.tsv\t2\t2\t2\t1\t0\t0\t1\t2\t2\t2\t2\t2\t2\t2\t1\t1\t1\t1\t1\t1\t1\t0\t0\t1\tverified-independent-original-state-geometry-draw-classification-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-state-geometry-payload-join-control-summary.tsv"),
                new[]
                {
                    "adjacency_source_report\tjoin_source_report\tadjacency_rows\tjoin_rows\tjoined_object_rows\tmissing_join_objects\tadjacency_input_status_valid_rows\tjoin_input_status_valid_rows\tidentity_join_match_rows\trender_status_valid_rows\tpayload_status_valid_rows\tstructural_counts_valid_rows\tprimitive_count_match_rows\tcomplete_command_count_match_rows\tfetch_count_match_rows\tfive_slot_payload_count_match_rows\tdistinct_payload_range_count_match_rows\tpayload_overlap_zero_rows\tstatus\tprovenance\toriginal_semantics",
                    "adjacency.tsv\tjoin.tsv\t1\t2\t1\t0\t1\t2\t1\t1\t1\t1\t1\t1\t1\t1\t1\t1\tverified-independent-original-state-geometry-payload-join-control\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "animation-catalog-pose-timing-global-assignment-control-summary.tsv"),
                new[]
                {
                    "timestamp_source_report\tanchor_source_report\tpose_source_report\tmorph_source_report\tcatalog_bank_count\tcatalog_rows\tcatalog_timestamp_valid_rows\tanchor_bank_rows\tanchor_complete_bank_rows\tanchor_count\tpose_rows\tdistinct_pose_archives\tdistinct_pose_symbols\tdistinct_normalized_names\tmorph_layout_rows\tmorph_parse_valid_rows\tmorph_containing_layouts\tmorph_containing_pairs\tmorph_unique_containment_layouts\tmorph_ambiguous_containment_layouts\tmorph_exact_gap_set_pairs\tpose_rows_without_catalog_join_key\tpose_rows_without_morph_join_key\tpose_bank_domain_size_before\tpose_bank_domain_size_after\tglobal_assignment_models_before\tglobal_assignment_models_after\tpose_bank_domain_reduced_rows\tunique_pose_animation_binding_proven_rows\tvalid_pose_rows\tstatus\tsemantic_binding\tprovenance\toriginal_semantics",
                    "timestamp.tsv\tanchor.tsv\tpose.tsv\tmorph.tsv\t19\t1584\t1584\t19\t19\t3168\t391\t391\t1\t1\t393\t393\t53\t159\t8\t45\t0\t391\t391\t19\t19\t7429\t7429\t0\t0\t391\tverified-independent-animation-catalog-pose-timing-global-assignment-control\tnot-proven\tindependent-test\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-payload-policy-control-summary.tsv"),
                new[]
                {
                    "bounded_source_report\tall_source_report\tbounded_rows\tall_rows\tjoined_rows\tbounded_event_rows\tall_event_rows\tbounded_bank_rows\tall_bank_rows\tbounded_input_status_rows\tall_input_status_rows\tidentity_match_rows\tstatus_match_rows\tdecode_match_rows\twave_bytes_match_rows\tpcm_hash_match_rows\texact_match_count_match_rows\texact_pcm_match_rows\tstatus\tprovenance\toriginal_semantics",
                    "bounded.tsv\tall.tsv\t2\t2\t2\t1\t1\t1\t1\t2\t2\t2\t2\t2\t2\t2\t2\t0\tverified-independent-audio-event-payload-policy-control\tindependent-test\tnot-claimed",
                });
            var animationSummaryPath = Path.Combine(
                animationSummaryDirectory,
                "animation-morph-relocation-profile-audit-summary.tsv");
            File.WriteAllLines(animationSummaryPath, new[]
            {
                "morph_catalog_rows\tmorph_layouts\tparse_valid_layouts\tinvalid_layouts\tanimation_banks\tlayouts_with_containing_animation_bank\tlayouts_with_exact_gap_set_bank\tcontaining_pairs\texact_gap_set_pairs\tlayouts_with_exact_gap_histogram_bank\texact_gap_histogram_pairs",
                "594\t393\t393\t0\t19\t53\t0\t159\t0\t0\t0",
            });
            var animationPayloadNameSummaryPath = Path.Combine(
                animationSummaryDirectory,
                "animation-bank-catalog-payload-name-audit-summary.tsv");
            File.WriteAllLines(animationPayloadNameSummaryPath, new[]
            {
                "bank_name\tcatalog_rows\tdistinct_catalog_names\tpayload_string_count\texact_rows\ttagged_rows\tprefix_rows\tmissing_rows\tambiguous_rows\texact_occurrences\ttagged_occurrences\tmissing_names_sha256",
                "bank\t1\t1\t4\t0\t1\t0\t0\t1\t0\t2\tE3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855",
            });
            var animationPayloadAnchorSummaryPath = Path.Combine(
                animationSummaryDirectory,
                "animation-bank-catalog-payload-anchor-audit-summary.tsv");
            File.WriteAllLines(animationPayloadAnchorSummaryPath, new[]
            {
                "bank_count\tcatalog_rows\ttagged_rows\trows_with_two_anchors\tanchor_count\twithin_name_gap_count\tcross_name_gap_count\tbanks_strictly_increasing\tbanks_with_complete_pairs\tstatus\toriginal_semantics",
                "1\t1\t1\t1\t2\t1\t0\t1\t1\tverified-structural\tnot-claimed",
            });
            AudioEventIndexEvidenceReport.Write(
                Path.Combine(animationSummaryDirectory, "audio-event-index-evidence.tsv"),
                new AudioEventIndexEvidenceReportData(
                    EventPath: "data/audio/eventspa.evt",
                    StringTableOffset: 0x2090,
                    CandidatePairs: 1061,
                    StringHashFamilyCandidates: 17,
                    DistinctStringOffsets: 17,
                    DistinctFollowingWords: 17,
                    AbkBanksScanned: 16,
                    AbkAlignedOccurrences: 0,
                    AbkValuesWithHits: 0,
                    HeaderAlignedOccurrences: 0,
                    HeaderValuesWithHits: 0,
                    DataSearchRequested: true,
                    DataAlignedOccurrences: 0,
                    DataValuesWithHits: 0,
                    CandidateNames: "goal,intro,tackle",
                    Status: "no-direct-selection-binding",
                    DirectSelectionBinding: "not-proven",
                    OriginalSemantics: "not-claimed"));
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-payload-wave-correlation-summary.tsv"),
                new[]
                {
                    "field\tvalue",
                    "event_candidates\t1",
                    "event_decoded\t1",
                    "bank_candidates\t1",
                    "bank_decoded\t1",
                    "exact_pcm_hash_pairs\t0",
                    "event_hashes_with_match\t0",
                    "status\tverified-structural",
                    "direct_selection_binding\tnot-proven",
                    "original_semantics\tnot-claimed",
                    "bounded_policy\tfirst_8_bank_candidates",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-payload-wave-correlation-exhaustive-summary.tsv"),
                new[]
                {
                    "field\tvalue",
                    "event_candidates\t1",
                    "event_decoded\t1",
                    "bank_count\t1",
                    "bank_sound_candidates\t2",
                    "bank_sound_decoded\t1",
                    "exact_pcm_hash_pairs\t0",
                    "event_hashes_with_match\t0",
                    "status\tverified-structural",
                    "direct_selection_binding\tnot-proven",
                    "original_semantics\tnot-claimed",
                    "bounded_policy\tfirst_512_sound_candidates_per_bank",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "audio-event-payload-wave-correlation-all-summary.tsv"),
                new[]
                {
                    "field\tvalue",
                    "event_candidates\t1",
                    "event_decoded\t1",
                    "bank_count\t1",
                    "bank_sound_candidates\t2",
                    "bank_sound_decoded\t1",
                    "exact_pcm_hash_pairs\t0",
                    "event_hashes_with_match\t0",
                    "status\tverified-structural",
                    "direct_selection_binding\tnot-proven",
                    "original_semantics\tnot-claimed",
                    "bounded_policy\tfirst_100000_sound_candidates_per_bank",
                });
            var coverageRoutes = CleanRoomAudioCuePolicy.Routes
                .Select((route, index) => new CleanRoomGameplayAudioRoute(
                    index,
                    route.Action,
                    route.CandidateName,
                    "played-clean-room-candidate"))
                .ToArray();
            var coverageTracePath = Path.Combine(animationSummaryDirectory, "coverage-fixture-trace.tsv");
            var coverageReplayPath = Path.Combine(animationSummaryDirectory, "coverage-fixture-replayed.tsv");
            File.WriteAllLines(coverageTracePath, GameplayAudioTraceSerializer.BuildLines(coverageRoutes));
            File.WriteAllLines(coverageReplayPath, GameplayAudioTraceSerializer.BuildLines(coverageRoutes));
            GameplayAudioCoverageAudit.Write(
                Path.Combine(animationSummaryDirectory, "gameplay-audio-coverage.tsv"),
                GameplayAudioCoverageAudit.Audit(new[]
                {
                    new GameplayAudioCoverageScenarioSpec("fixture", coverageTracePath, coverageReplayPath),
                }));
            var gameplayCoverageTracePath = Path.Combine(animationSummaryDirectory, "gameplay-coverage-fixture-trace.tsv");
            var gameplayCoverageReplayPath = Path.Combine(animationSummaryDirectory, "gameplay-coverage-fixture-replayed.tsv");
            var gameplayCoverageLines = MatchTraceSerializer.BuildLines(new[]
            {
                new CleanRoomMatchEvent(0, 0, "KICKOFF", 0, 0, BallOwner.None),
            });
            File.WriteAllLines(gameplayCoverageTracePath, gameplayCoverageLines);
            File.WriteAllLines(gameplayCoverageReplayPath, gameplayCoverageLines);
            GameplayCoverageAudit.Write(
                Path.Combine(animationSummaryDirectory, "gameplay-coverage.tsv"),
                GameplayCoverageAudit.Audit(new[]
                {
                    new GameplayCoverageScenarioSpec(
                        "fixture",
                        gameplayCoverageTracePath,
                        gameplayCoverageReplayPath,
                        new[] { "KICKOFF" }),
                }));
            var integratedCoverageRows = Enumerable.Range(0, 9)
                .Select(index => new CleanRoomIntegratedCoverageScenarioResult(
                    $"fixture-{index}",
                    "fixture-integration.tsv",
                    new string('B', 64),
                    true,
                    true,
                    4,
                    4,
                    4,
                    8,
                    2,
                    2,
                    2,
                    0,
                    true,
                    true,
                    true,
                    "KICKOFF",
                    string.Empty,
                    "verified-authored",
                    string.Empty))
                .ToArray();
            CleanRoomIntegratedCoverageAudit.Write(
                Path.Combine(animationSummaryDirectory, "clean-room-integrated-coverage.tsv"),
                new CleanRoomIntegratedCoverageAuditResult(
                    new CleanRoomIntegratedCoverageSummary(
                        9,
                        9,
                        36,
                        18,
                        72,
                        "verified-authored",
                        "not-claimed"),
                    integratedCoverageRows));
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "clean-room-gameplay-invariants.tsv"),
                new[]
                {
                    CleanRoomGameplayInvariantAudit.Header,
                    "scenario\tfixture\t4\t1\tTrue\tTrue\tTrue\tTrue\tTrue\tTrue\tTrue\tTrue\tKICKOFF\t\tverified-authored\t\t\t\t\tnot-claimed\t",
                    "summary\tall\t\t\t\t\t\t\t\t\t\t\t\t\tverified-authored\t1\t1\t4\t1\tnot-claimed\t",
                });
            var windowedCapturePath = Path.Combine(animationSummaryDirectory, "windowed-runtime-capture.ppm");
            var windowedHeader = Encoding.ASCII.GetBytes("P6\n960 540\n255\n");
            var windowedCapture = new byte[windowedHeader.Length + (960 * 540 * 3)];
            Buffer.BlockCopy(windowedHeader, 0, windowedCapture, 0, windowedHeader.Length);
            windowedCapture[windowedHeader.Length] = 0;
            windowedCapture[windowedHeader.Length + 1] = 1;
            File.WriteAllBytes(windowedCapturePath, windowedCapture);
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-static-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\trender_xref_rows\tunique_render_literals\trender_context_rows\trender_context_with_executable_calls\tdistinct_executable_call_targets\trender_targets_with_multiple_literals\tcategory_counts\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\t2\t2\t2\t2\t1\t1\td3d-or-render-api-token:1|eagl-geometry-token:1\tverified-static-render-literal-context\tretail-XBE-static-xref+local-call-context; no-execution; no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-target-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tdirect_call_count\tindirect_call_count\tconditional_branch_count\tabsolute_memory_operand_count\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\ttop_2_render_call_clusters_by_unique_render_literals\t2\t2\t2\t1\t2\t1\t3\t1\tverified-static-render-target-windows\tsame-ISO-retail-XBE+bounded-x86-disassembly-of-ranked-render-context; no-execution; no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-cfg-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tsame_top_2_render_target_windows; bounded_cfg_bytes=2048; direct_calls_not_followed\t2\t2\t2\t2\t8\t7\t3\t2\t1\t2\t0\t2\t2\t1\tverified-static-render-cfg-slices\tsame-ISO-retail-XBE+bounded-reachability-over-ranked-render-windows; no-execution; no-function-boundary-claim; no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-call-target-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tsource_render_target_windows\tsource_windows_with_direct_calls\tunique_direct_call_targets\tmapped_executable_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tdirect-call-targets-from-bounded-render-cfg; max_bytes=256; no-recursive-following\t2\t2\t2\t2\t2\t2\t1\tverified-static-render-call-target-slices\tsame-ISO-retail-XBE+one-hop-direct-call-targets-from-bounded-render-CFG; no-execution; no-function-boundary-claim; no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-call-graph-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tseed_targets\tseed_mapped_targets\tmax_depth\tmax_nodes\tdiscovered_nodes\tmapped_executable_nodes\tdisassembled_nodes\tret_terminated_nodes\tnodes_with_direct_calls\tunique_edges\ttotal_edge_occurrences\ttruncated_by_max_nodes\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tseeded-by-call-target-report; max_depth=2; max_bytes=256; max_nodes=4\t2\t2\t2\t4\t3\t3\t3\t3\t2\t2\t3\tFalse\tverified-static-bounded-render-call-graph\tsame-ISO-retail-XBE+bounded-direct-call-graph-seeded-by-render-CFG-targets; no-execution; no-function-boundary-claim; no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-transfer-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_indirect_transfers\ttotal_indirect_transfers\tindirect_calls\tindirect_jumps\tregister_operands\tmemory_operands\tabsolute_memory_operands\tdistinct_operand_shapes\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\treachable indirect call/jump operands in bounded render CFG slices\t2\t2\t2\t3\t2\t1\t1\t2\t1\t3\tverified-static-render-indirect-transfer-inventory\tsame-ISO-retail-XBE+bounded-x86-disassembly-of-reachable-render-CFG;no-execution;no-dereference;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-reference-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindirect_rows\treference_rows\tabsolute_memory_rows\tindexed_table_base_rows\tbounded_reference_reads\treference_words_valid\treference_words_in_executable_sections\tdistinct_reference_addresses\tdistinct_raw_words\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded four-byte reads at base-less absolute indirect render references; no target dereference\t3\t2\t1\t1\t2\t2\t2\t2\t2\tverified-static-render-indirect-reference-words\tsame-ISO-retail-XBE+bounded-read-at-observed-indirect-memory-references;no-execution;no-call;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-reference-slice-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\treference_rows\tdistinct_raw_words\texecutable_raw_words\tdisassembled_candidate_words\tret_terminated_candidate_words\tprologue_like_candidate_words\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded executable-section disassembly at raw words from reference report; max_bytes=256; no execution\t2\t2\t2\t2\t2\t1\tverified-static-render-indirect-reference-word-slices\tsame-ISO-retail-XBE+bounded-disassembly-of-raw-words-from-reference-audit;no-execution;no-call;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-window-join-summary.tsv"),
                new[]
                {
                    "target_windows\ttargets_with_indirect_transfers\ttotal_indirect_transfers\tindirect_calls\tindirect_jumps\ttargets_with_references\treference_rows\ttargets_with_transfer_reference_instruction_overlap\texact_transfer_reference_instruction_rows\tdistinct_raw_words\texecutable_raw_words\tdisassembled_candidate_words\tret_terminated_candidate_words\tprologue_like_candidate_words\tstatus\toriginal_semantics",
                    "2\t2\t3\t2\t1\t1\t2\t1\t2\t2\t2\t2\t2\t1\tverified-static-render-indirect-window-join\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-table-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_rows\tdistinct_table_bases\ttables_scan_capped\ttables_terminated_after_non_executable_words\treadable_table_words\texecutable_table_words\tnon_executable_table_words\tunreadable_table_words\tdistinct_executable_words\ttables_with_contiguous_executable_prefix\tmax_contiguous_executable_prefix\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded indexed-table scan; max_entries=8; stop_after_two_non_executable_words; no execution\t1\t1\t0\t1\t4\t2\t2\t0\t2\t1\t2\tverified-bounded-static-render-indirect-indexed-table\tsame-ISO-retail-XBE+bounded-read-at-indexed-indirect-table-base;no-execution;no-call;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-target-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_rows\tcandidate_entry_occurrences\tdistinct_candidate_targets\tmapped_executable_targets\tdisassembled_candidate_targets\tret_terminated_candidate_targets\tprologue_like_candidate_targets\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded executable-section disassembly at indexed-table candidates; max_bytes=256; no execution; no function-boundary claim\t1\t3\t2\t2\t2\t2\t1\tverified-bounded-static-render-indirect-indexed-target-slices\tsame-ISO-retail-XBE+bounded-disassembly-of-indexed-table-candidates;no-execution;no-call;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-accessor-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_rows\tcandidate_entry_occurrences\tdistinct_candidate_targets\tmapped_executable_targets\tdisassembled_candidate_targets\tret_terminated_candidate_targets\tfallback_pop_pop_ret_shapes\tsingle_mov_memory_ret_shapes\tlea_push_call_ret_shapes\tother_shapes\tdistinct_memory_displacements\tdistinct_direct_call_targets\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded instruction shape through first observed ret; max_bytes=256; no execution; no function-boundary claim\t1\t3\t2\t2\t2\t2\t1\t1\t0\t0\t1\t1\tverified-bounded-static-render-indirect-indexed-accessor-shapes\tsame-ISO-retail-XBE+bounded-observation-of-indexed-table-entry-instruction-shapes;no-execution;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded CFG over indexed-table candidate windows; max_bytes=256; direct calls not followed\t2\t2\t2\t2\t8\t7\t3\t2\t1\t2\t0\t2\t2\t1\t1\tverified-bounded-static-render-indirect-indexed-cfg-slices\tsame-ISO-retail-XBE+bounded-reachability-over-indexed-table-candidates;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-call-target-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcfg_direct_call_occurrences\tcfg_unique_direct_call_targets_sum\tdistinct_direct_call_targets\tmapped_executable_targets\tdisassembled_targets\tret_terminated_targets\tprologue_like_targets\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tdirect-call-targets-from-bounded-indexed-CFG; max_bytes=256; no-call-following\t2\t2\t1\t1\t1\t1\t0\tverified-bounded-static-render-indirect-indexed-cfg-call-target-slices\tsame-ISO-retail-XBE+bounded-indexed-CFG-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-call-site-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tsource_cfg_candidates\tdirect_call_occurrences\tdistinct_call_targets\tcandidate_targets_with_call_sites\tcall_sites_with_push_edx_before_lea\tcall_sites_with_lea_ecx_base_before_call\tcall_sites_with_ret_imm_4_after_call\tdistinct_lea_displacements\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\treachable immediate calls from bounded indexed-CFG slices; max_bytes=256; local context only\t2\t2\t1\t2\t2\t2\t2\t2\tverified-bounded-static-render-indirect-indexed-call-sites\tsame-ISO-retail-XBE+bounded-indexed-CFG-reachable-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-call-target-cfg-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded CFG over indexed-CFG direct-call destination windows; max_bytes=256; calls not followed\t1\t1\t1\t1\t6\t4\t2\t1\t0\t1\t0\t1\t1\t1\t0\tverified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices\tsame-ISO-retail-XBE+bounded-CFG-over-indexed-CFG-call-target-slices;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-nested-call-target-cfg-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tparent_target_rows\tparent_direct_call_occurrences\tdistinct_nested_targets\tanalyzed_targets\ttargets_with_reachable_cfg\ttargets_with_reachable_ret\ttotal_linear_instructions\ttotal_reachable_instructions\ttotal_basic_blocks\ttotal_conditional_branches\ttotal_unconditional_branches\ttotal_internal_branch_targets\ttotal_out_of_window_branch_targets\ttotal_direct_calls\ttotal_unique_direct_call_targets\tdistinct_nested_direct_call_targets\ttotal_indirect_control_transfers\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded CFG over one-hop nested direct-call destinations; max_bytes=256; calls not followed\t1\t1\t1\t1\t1\t1\t6\t4\t2\t1\t0\t1\t0\t0\t0\t0\t0\tverified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices\tsame-ISO-retail-XBE+bounded-CFG-over-one-hop-nested-direct-call-destinations;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-nested-call-site-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tparent_cfg_target_rows\tnested_call_target_rows\tdistinct_nested_call_targets\tnested_call_sites_with_push_edx_before_lea\tnested_call_sites_with_lea_ecx_base_before_call\tnested_call_sites_with_ret_imm_4_after_call\tdistinct_nested_lea_displacements\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded local context for one-hop nested direct calls; max_bytes=256; no semantic promotion\t1\t1\t1\t0\t0\t0\t0\tverified-bounded-static-render-indirect-indexed-nested-call-sites\tsame-ISO-retail-XBE+bounded-one-hop-nested-call-site-context;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-call-site-join-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tindexed_table_target_rows\tdistinct_table_bases\tcall_site_rows\tjoined_call_site_rows\tdistinct_call_targets\tdistinct_table_entry_indices\ttable_entry_occurrences_for_joined_call_sites\tduplicate_table_occurrences_over_call_sites\tcall_sites_with_complete_local_shape\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded join of indexed-table target occurrences with reachable CFG call-site context; no semantic promotion\t2\t1\t2\t2\t1\t3\t3\t1\t2\tverified-bounded-static-render-indirect-indexed-call-site-join\tsame-ISO-retail-XBE+bounded-indexed-table-target-and-CFG-call-site-join;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-target-join-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcandidate_targets\tdirect_call_targets\trender_window_targets\tcandidate_targets_in_direct_call_targets\tcandidate_targets_in_render_window_targets\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tset intersection of bounded indexed-table candidates with direct-call and render-window reports; no binding\t2\t2\t2\t0\t0\tverified-structural-negative\tsame-ISO-retail-XBE+bounded-address-set-intersection;no-execution;no-call;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "xbe-render-indirect-indexed-cfg-join-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\tselection_policy\tcfg_direct_call_targets\trender_direct_call_targets\trender_window_targets\trender_call_graph_nodes\tcfg_targets_in_render_direct_call_targets\tcfg_targets_in_render_window_targets\tcfg_targets_in_render_call_graph_nodes\tstatus\tprovenance\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\txbe\t10\tbounded address-set intersection of indexed-CFG direct-call targets with render target and call-graph reports; no binding\t1\t2\t2\t3\t0\t0\t0\tverified-structural-negative\tsame-ISO-retail-XBE+bounded-indexed-CFG-call-target-set-intersection;no-execution;no-call-following;no-function-boundary-claim;no-binding\tnot-claimed",
                });
            File.WriteAllLines(
                Path.Combine(animationSummaryDirectory, "eagl-original-render-command-dispatch-control-summary.tsv"),
                new[]
                {
                    "image\tinternal_path\txbe_sha256\txbe_size\teagl_report_sha256\teagl_rows\tcomplete_command_stream_rows\tcommand_occurrences\tunique_observed_command_ids\tmapped_observed_command_ids\texecutable_dispatch_table_prefix_entries\tdistinct_dispatch_targets\tdispatch_table_base\tdispatch_table_file_offset\tfirst_non_executable_index\tfirst_non_executable_word\thandler_table_scan_capped\tdispatcher_instruction_addresses_validated\tstatus\tprovenance\tdispatch_binding\tsemantic_binding\toriginal_semantics",
                    "fixture.iso\tdefault.xbe\t5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B\t3293184\tAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\t2364\t2364\t36779\t12\t12\t42\t39\t0x0031A930\t0x0030A350\t42\t0x00000001\tFalse\t7\tverified-static-eagl-command-id-dispatch-control\tfixture-independent-static-control\tverified-static-command-header-index-to-handler-table-slot\tnot-proven\tnot-claimed",
                });
            var structuralSummary = ProjectReadinessAudit.Build(catalog, isoPath, temporaryDirectory.FullName);
            var originalCommandDispatchRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-original-command-id-dispatch-control");
            Assert(originalCommandDispatchRow.GatePassed &&
                   originalCommandDispatchRow.Status == "verified-structural" &&
                   originalCommandDispatchRow.Observed == 12 &&
                   originalCommandDispatchRow.Expected == 12 &&
                   originalCommandDispatchRow.Notes.Contains("command_occurrences=36779", StringComparison.Ordinal) &&
                   originalCommandDispatchRow.Notes.Contains("dispatch_binding=verified-static-command-header-index-to-handler-table-slot", StringComparison.Ordinal) &&
                   originalCommandDispatchRow.Notes.Contains("semantic_binding=not-proven", StringComparison.Ordinal) &&
                   originalCommandDispatchRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the structural original command-dispatch control.");
            var windowedRuntimeRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-authored-windowed-runtime");
            Assert(windowedRuntimeRow.GatePassed &&
                   windowedRuntimeRow.Status == "verified-authored" &&
                   windowedRuntimeRow.Observed == 1 &&
                   windowedRuntimeRow.Notes.Contains("bytes=1555215", StringComparison.Ordinal) &&
                   windowedRuntimeRow.Notes.Contains("nonuniform=True", StringComparison.Ordinal) &&
                   windowedRuntimeRow.Notes.Contains("provenance=managed-software-framebuffer", StringComparison.Ordinal) &&
                   windowedRuntimeRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the managed Win32 framebuffer capture.");
            var staticRenderRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-context");
            Assert(staticRenderRow.GatePassed &&
                   staticRenderRow.Status == "verified-structural" &&
                   staticRenderRow.Observed == 2 &&
                   staticRenderRow.Notes.Contains("render_xref_rows=2", StringComparison.Ordinal) &&
                   staticRenderRow.Notes.Contains("distinct_executable_call_targets=1", StringComparison.Ordinal) &&
                   staticRenderRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the static render-context evidence.");
            var staticRenderTargetRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-target-windows");
            Assert(staticRenderTargetRow.GatePassed &&
                   staticRenderTargetRow.Status == "verified-structural" &&
                   staticRenderTargetRow.Observed == 2 &&
                   staticRenderTargetRow.Notes.Contains("ret_terminated_targets=2", StringComparison.Ordinal) &&
                   staticRenderTargetRow.Notes.Contains("prologue_like_targets=1", StringComparison.Ordinal) &&
                   staticRenderTargetRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the bounded render-target windows.");
            var staticRenderCfgRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-cfg-slices");
            Assert(staticRenderCfgRow.GatePassed &&
                   staticRenderCfgRow.Status == "verified-structural" &&
                   staticRenderCfgRow.Observed == 2 &&
                   staticRenderCfgRow.Notes.Contains("targets_with_reachable_cfg=2", StringComparison.Ordinal) &&
                   staticRenderCfgRow.Notes.Contains("total_basic_blocks=3", StringComparison.Ordinal) &&
                   staticRenderCfgRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the bounded render CFG slices.");
            var staticRenderCallTargetRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-call-target-slices");
            Assert(staticRenderCallTargetRow.GatePassed &&
                   staticRenderCallTargetRow.Status == "verified-structural" &&
                   staticRenderCallTargetRow.Observed == 2 &&
                   staticRenderCallTargetRow.Notes.Contains("unique_direct_call_targets=2", StringComparison.Ordinal) &&
                   staticRenderCallTargetRow.Notes.Contains("ret_terminated_targets=2", StringComparison.Ordinal) &&
                   staticRenderCallTargetRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the one-hop render call-target slices.");
            var staticRenderCallGraphRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-call-graph");
            Assert(staticRenderCallGraphRow.GatePassed &&
                   staticRenderCallGraphRow.Status == "verified-structural" &&
                   staticRenderCallGraphRow.Observed == 3 &&
                   staticRenderCallGraphRow.Notes.Contains("unique_edges=2", StringComparison.Ordinal) &&
                   staticRenderCallGraphRow.Notes.Contains("truncated_by_max_nodes=False", StringComparison.Ordinal) &&
                   staticRenderCallGraphRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the bounded render call graph.");
            var staticRenderIndirectTransferRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-transfers");
            Assert(staticRenderIndirectTransferRow.GatePassed &&
                   staticRenderIndirectTransferRow.Status == "verified-structural" &&
                   staticRenderIndirectTransferRow.Observed == 3 &&
                   staticRenderIndirectTransferRow.Notes.Contains("indirect_calls=2", StringComparison.Ordinal) &&
                   staticRenderIndirectTransferRow.Notes.Contains("indirect_jumps=1", StringComparison.Ordinal) &&
                   staticRenderIndirectTransferRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the bounded render indirect-transfer inventory.");
            var staticRenderIndirectReferenceRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-reference-words");
            Assert(staticRenderIndirectReferenceRow.GatePassed &&
                   staticRenderIndirectReferenceRow.Status == "verified-structural" &&
                   staticRenderIndirectReferenceRow.Observed == 2 &&
                   staticRenderIndirectReferenceRow.Notes.Contains("reference_words_valid=2", StringComparison.Ordinal) &&
                   staticRenderIndirectReferenceRow.Notes.Contains("reference_words_in_executable_sections=2", StringComparison.Ordinal) &&
                   staticRenderIndirectReferenceRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the bounded render indirect-reference words.");
            var staticRenderIndirectReferenceSliceRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-reference-word-slices");
            Assert(staticRenderIndirectReferenceSliceRow.GatePassed &&
                   staticRenderIndirectReferenceSliceRow.Status == "verified-structural" &&
                   staticRenderIndirectReferenceSliceRow.Observed == 2 &&
                   staticRenderIndirectReferenceSliceRow.Notes.Contains("distinct_raw_words=2", StringComparison.Ordinal) &&
                   staticRenderIndirectReferenceSliceRow.Notes.Contains("disassembled_candidate_words=2", StringComparison.Ordinal) &&
                   staticRenderIndirectReferenceSliceRow.Notes.Contains("ret_terminated_candidate_words=2", StringComparison.Ordinal) &&
                   staticRenderIndirectReferenceSliceRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indirect-reference word slices.");
            var staticRenderIndirectWindowJoinRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-window-join");
            Assert(staticRenderIndirectWindowJoinRow.GatePassed &&
                   staticRenderIndirectWindowJoinRow.Status == "verified-structural" &&
                   staticRenderIndirectWindowJoinRow.Observed == 2 &&
                   staticRenderIndirectWindowJoinRow.Notes.Contains("total_indirect_transfers=3", StringComparison.Ordinal) &&
                   staticRenderIndirectWindowJoinRow.Notes.Contains("reference_rows=2", StringComparison.Ordinal) &&
                   staticRenderIndirectWindowJoinRow.Notes.Contains("exact_transfer_reference_instruction_rows=2", StringComparison.Ordinal) &&
                   staticRenderIndirectWindowJoinRow.Notes.Contains("disassembled_candidate_words=2", StringComparison.Ordinal) &&
                   staticRenderIndirectWindowJoinRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the exact structural render indirect-window join.");
            var staticRenderIndirectIndexedTableRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-table");
            Assert(staticRenderIndirectIndexedTableRow.GatePassed &&
                   staticRenderIndirectIndexedTableRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedTableRow.Observed == 2 &&
                   staticRenderIndirectIndexedTableRow.Notes.Contains("indexed_rows=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTableRow.Notes.Contains("tables_scan_capped=0", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTableRow.Notes.Contains("max_contiguous_executable_prefix=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTableRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed indirect-table control.");
            var staticRenderIndirectIndexedTargetRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-targets");
            Assert(staticRenderIndirectIndexedTargetRow.GatePassed &&
                   staticRenderIndirectIndexedTargetRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedTargetRow.Observed == 2 &&
                   staticRenderIndirectIndexedTargetRow.Notes.Contains("candidate_entry_occurrences=3", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTargetRow.Notes.Contains("distinct_candidate_targets=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTargetRow.Notes.Contains("ret_terminated_candidate_targets=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTargetRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-table candidate target slices.");
            var staticRenderIndirectIndexedAccessorRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-accessor-shapes");
            Assert(staticRenderIndirectIndexedAccessorRow.GatePassed &&
                   staticRenderIndirectIndexedAccessorRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedAccessorRow.Observed == 2 &&
                   staticRenderIndirectIndexedAccessorRow.Notes.Contains("fallback_pop_pop_ret_shapes=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedAccessorRow.Notes.Contains("single_mov_memory_ret_shapes=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedAccessorRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-table accessor-shape control.");
            var staticRenderIndirectIndexedCfgRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg");
            Assert(staticRenderIndirectIndexedCfgRow.GatePassed &&
                   staticRenderIndirectIndexedCfgRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgRow.Observed == 2 &&
                   staticRenderIndirectIndexedCfgRow.Notes.Contains("targets_with_reachable_cfg=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgRow.Notes.Contains("targets_with_reachable_ret=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgRow.Notes.Contains("total_reachable_instructions=7", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgRow.Notes.Contains("distinct_direct_call_targets=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-table CFG slices.");
            var staticRenderIndirectIndexedCfgCallTargetRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-call-targets");
            Assert(staticRenderIndirectIndexedCfgCallTargetRow.GatePassed &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Observed == 1 &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Notes.Contains("cfg_direct_call_occurrences=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Notes.Contains("distinct_direct_call_targets=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Notes.Contains("ret_terminated_targets=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallTargetRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-CFG call-target slices.");
            var staticRenderIndirectIndexedCfgCallSiteRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-call-sites");
            Assert(staticRenderIndirectIndexedCfgCallSiteRow.GatePassed &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Observed == 2 &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Notes.Contains("direct_call_occurrences=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Notes.Contains("call_sites_with_lea_ecx_base_before_call=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Notes.Contains("distinct_lea_displacements=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallSiteRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-CFG call-site context.");
            var staticRenderIndirectIndexedCfgCallTargetCfgRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-call-target-cfg");
            Assert(staticRenderIndirectIndexedCfgCallTargetCfgRow.GatePassed &&
                   staticRenderIndirectIndexedCfgCallTargetCfgRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgCallTargetCfgRow.Observed == 1 &&
                   staticRenderIndirectIndexedCfgCallTargetCfgRow.Notes.Contains("total_reachable_instructions=4", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallTargetCfgRow.Notes.Contains("total_conditional_branches=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallTargetCfgRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded indexed-CFG destination CFG.");
            var staticRenderIndirectIndexedCfgNestedCallTargetCfgRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-nested-call-target-cfg");
            Assert(staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.GatePassed &&
                   staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.Observed == 1 &&
                   staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.Notes.Contains("total_reachable_instructions=4", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.Notes.Contains("total_conditional_branches=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgNestedCallTargetCfgRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded nested indexed-CFG destination CFG.");
            var staticRenderIndirectIndexedCfgNestedCallSiteRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-nested-call-sites");
            Assert(staticRenderIndirectIndexedCfgNestedCallSiteRow.GatePassed &&
                   staticRenderIndirectIndexedCfgNestedCallSiteRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgNestedCallSiteRow.Observed == 1 &&
                   staticRenderIndirectIndexedCfgNestedCallSiteRow.Notes.Contains("nested_call_target_rows=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgNestedCallSiteRow.Notes.Contains("nested_call_sites_with_ret_imm_4_after_call=0", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgNestedCallSiteRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the bounded nested indexed-CFG call-site context.");
            var staticRenderIndirectIndexedCfgCallSiteJoinRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-call-site-join");
            Assert(staticRenderIndirectIndexedCfgCallSiteJoinRow.GatePassed &&
                   staticRenderIndirectIndexedCfgCallSiteJoinRow.Status == "verified-structural" &&
                   staticRenderIndirectIndexedCfgCallSiteJoinRow.Observed == 2 &&
                   staticRenderIndirectIndexedCfgCallSiteJoinRow.Notes.Contains("joined_call_site_rows=2", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallSiteJoinRow.Notes.Contains("duplicate_table_occurrences_over_call_sites=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgCallSiteJoinRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the indexed CFG call-site join.");
            var staticRenderIndirectIndexedCfgJoinRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-cfg-join");
            Assert(staticRenderIndirectIndexedCfgJoinRow.GatePassed &&
                   staticRenderIndirectIndexedCfgJoinRow.Status == "verified-structural-negative" &&
                   staticRenderIndirectIndexedCfgJoinRow.Observed == 1 &&
                   staticRenderIndirectIndexedCfgJoinRow.Notes.Contains("cfg_direct_call_targets=1", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgJoinRow.Notes.Contains("cfg_targets_in_render_call_graph_nodes=0", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedCfgJoinRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the indexed-CFG direct-call negative control.");
            var staticRenderIndirectIndexedTargetJoinRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-static-xbe-indirect-indexed-target-join");
            Assert(staticRenderIndirectIndexedTargetJoinRow.GatePassed &&
                   staticRenderIndirectIndexedTargetJoinRow.Status == "verified-structural-negative" &&
                   staticRenderIndirectIndexedTargetJoinRow.Observed == 2 &&
                   staticRenderIndirectIndexedTargetJoinRow.Notes.Contains("candidate_targets_in_direct_call_targets=0", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTargetJoinRow.Notes.Contains("candidate_targets_in_render_window_targets=0", StringComparison.Ordinal) &&
                   staticRenderIndirectIndexedTargetJoinRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                 "Readiness audit did not consume the indexed-table target negative control.");
            var animationNotes = structuralSummary.Rows
                .Single(row => row.Area == "animation-deformation")
                .Notes;
            Assert(animationNotes.Contains("relocation_profile_summary=verified-structural-control", StringComparison.Ordinal) &&
                   animationNotes.Contains("relocation_layouts=393", StringComparison.Ordinal) &&
                   animationNotes.Contains("relocation_containing_pairs=159", StringComparison.Ordinal) &&
                   animationNotes.Contains("relocation_exact_pairs=0", StringComparison.Ordinal) &&
                   animationNotes.Contains("no pose/rig/skinning binding", StringComparison.Ordinal),
                "Readiness audit did not retain the structural animation/morph relocation control.");
            var animationPayloadNameRow = structuralSummary.Rows
                .Single(row => row.Area == "animation-catalog-payload-names");
            Assert(animationPayloadNameRow.GatePassed &&
                   animationPayloadNameRow.Status == "verified-structural" &&
                   animationPayloadNameRow.Observed == 1 &&
                   animationPayloadNameRow.Notes.Contains("tagged_rows=1", StringComparison.Ordinal) &&
                   animationPayloadNameRow.Notes.Contains("missing_rows=0", StringComparison.Ordinal) &&
                   animationPayloadNameRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the catalog-to-payload name control.");
            var animationPayloadAnchorRow = structuralSummary.Rows
                .Single(row => row.Area == "animation-catalog-payload-anchors");
            Assert(animationPayloadAnchorRow.GatePassed &&
                   animationPayloadAnchorRow.Status == "verified-structural" &&
                   animationPayloadAnchorRow.Observed == 1 &&
                   animationPayloadAnchorRow.Notes.Contains("rows_with_two_anchors=1", StringComparison.Ordinal) &&
                   animationPayloadAnchorRow.Notes.Contains("banks_strictly_increasing=1", StringComparison.Ordinal) &&
                   animationPayloadAnchorRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the payload-anchor interval control.");
            var animationCatalogPoseTimingRow = structuralSummary.Rows
                .Single(row => row.Area == "animation-catalog-pose-timing-global-assignment-control");
            Assert(animationCatalogPoseTimingRow.GatePassed &&
                   animationCatalogPoseTimingRow.Status == "verified-structural-negative" &&
                   animationCatalogPoseTimingRow.Observed == 391 &&
                   animationCatalogPoseTimingRow.Notes.Contains("catalog_rows=1584", StringComparison.Ordinal) &&
                   animationCatalogPoseTimingRow.Notes.Contains("pose_bank_domain_size_before=19", StringComparison.Ordinal) &&
                   animationCatalogPoseTimingRow.Notes.Contains("pose_bank_domain_size_after=19", StringComparison.Ordinal) &&
                   animationCatalogPoseTimingRow.Notes.Contains("pose_bank_domain_reduced_rows=0", StringComparison.Ordinal) &&
                   animationCatalogPoseTimingRow.Notes.Contains("unique_pose_animation_binding_proven_rows=0", StringComparison.Ordinal) &&
                   animationCatalogPoseTimingRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the catalog/pose timing global-assignment control.");
            var audioNotes = structuralSummary.Rows
                .Single(row => row.Area == "audio")
                .Notes;
            Assert(audioNotes.Contains("KICKOFF->intro", StringComparison.Ordinal) &&
                   audioNotes.Contains("JUGGLE->attackgen", StringComparison.Ordinal) &&
                   audioNotes.Contains("TACKLE->tackle", StringComparison.Ordinal) &&
                   audioNotes.Contains("FULL_TIME->outro", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_route_count=15", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_unique_cues=8", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_routes_available=0", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_routes_missing=15", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_event_index_report=verified-structural-negative", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_event_index_candidate_pairs=1061", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_event_index_abk_occurrences=0", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_event_index_direct_binding=not-proven", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_exhaustive_report=verified-structural-negative", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_exhaustive_bank_sound_candidates=2", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_exhaustive_exact_pcm_hash_pairs=0", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_all_report=verified-structural-negative", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_all_bank_sound_candidates=2", StringComparison.Ordinal) &&
                   audioNotes.Contains("audio_payload_all_exact_pcm_hash_pairs=0", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_audio_coverage_report=verified-authored", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_audio_coverage_scenarios=1", StringComparison.Ordinal) &&
                   audioNotes.Contains("authored_audio_coverage_missing_actions=", StringComparison.Ordinal),
                "Readiness audit did not record the authored clean-room audio route policy.");
            var namedEventRuntimeRow = structuralSummary.Rows
                .Single(row => row.Area == "audio-named-event-runtime");
            Assert(namedEventRuntimeRow.GatePassed &&
                   namedEventRuntimeRow.Status == "verified-candidate" &&
                   namedEventRuntimeRow.Observed == 1 &&
                   namedEventRuntimeRow.Expected == 1 &&
                   namedEventRuntimeRow.Notes.Contains("runtime_prepared=1", StringComparison.Ordinal) &&
                   namedEventRuntimeRow.Notes.Contains("wave_headers_valid=1", StringComparison.Ordinal) &&
                   namedEventRuntimeRow.Notes.Contains(AudioNamedEventRuntimeAudit.Provenance, StringComparison.Ordinal) &&
                   namedEventRuntimeRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the named EVT runtime subgate.");
            var audioParallelRangeRow = structuralSummary.Rows
                .Single(row => row.Area == "audio-event-parallel-range");
            Assert(audioParallelRangeRow.GatePassed &&
                   audioParallelRangeRow.Status == "verified-structural" &&
                   audioParallelRangeRow.Observed == 1 &&
                   audioParallelRangeRow.Expected == 1 &&
                   audioParallelRangeRow.Notes.Contains("event_string_anchor_valid=1", StringComparison.Ordinal) &&
                   audioParallelRangeRow.Notes.Contains("parallel_row_index_valid=1", StringComparison.Ordinal) &&
                   audioParallelRangeRow.Notes.Contains("data_schl_prefix=1", StringComparison.Ordinal) &&
                   audioParallelRangeRow.Notes.Contains(AudioEventParallelRangeAudit.Provenance, StringComparison.Ordinal) &&
                   audioParallelRangeRow.Notes.Contains("direct_selection_binding=not-proven", StringComparison.Ordinal) &&
                   audioParallelRangeRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the named EVT parallel-range control.");
            var audioXbeHashSearchRow = structuralSummary.Rows
                .Single(row => row.Area == "audio-event-xbe-hash-search");
            Assert(audioXbeHashSearchRow.GatePassed &&
                   audioXbeHashSearchRow.Status == "verified-structural" &&
                   audioXbeHashSearchRow.Observed == 1 &&
                   audioXbeHashSearchRow.Expected == 1 &&
                   audioXbeHashSearchRow.Notes.Contains("unique_event_names=1", StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains("unique_hash_words=1", StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains("aligned_occurrences=0", StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains("unaligned_occurrences=0", StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains(AudioEventXbeHashSearchAudit.Provenance, StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains("direct_selection_binding=not-proven", StringComparison.Ordinal) &&
                   audioXbeHashSearchRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the EVT/XBE hash-search control.");
            var audioPayloadPolicyControlRow = structuralSummary.Rows
                .Single(row => row.Area == "audio-event-payload-policy-control");
            Assert(audioPayloadPolicyControlRow.GatePassed &&
                   audioPayloadPolicyControlRow.Status == "verified-structural-negative" &&
                   audioPayloadPolicyControlRow.Observed == 2 &&
                   audioPayloadPolicyControlRow.Expected == 2 &&
                   audioPayloadPolicyControlRow.Notes.Contains("joined_rows=2", StringComparison.Ordinal) &&
                   audioPayloadPolicyControlRow.Notes.Contains("pcm_hash_match_rows=2", StringComparison.Ordinal) &&
                   audioPayloadPolicyControlRow.Notes.Contains("exact_pcm_match_rows=0", StringComparison.Ordinal) &&
                   audioPayloadPolicyControlRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the audio payload policy control.");
            var stateGeometryPartitionControlRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-partition-control");
            Assert(stateGeometryPartitionControlRow.GatePassed &&
                   stateGeometryPartitionControlRow.Status == "verified-structural-negative" &&
                   stateGeometryPartitionControlRow.Observed == 1 &&
                   stateGeometryPartitionControlRow.Expected == 1 &&
                   stateGeometryPartitionControlRow.Notes.Contains("joined_object_rows=1", StringComparison.Ordinal) &&
                   stateGeometryPartitionControlRow.Notes.Contains("state_index_set_match_rows=1", StringComparison.Ordinal) &&
                   stateGeometryPartitionControlRow.Notes.Contains("geometry_group_state_exact_rows=0", StringComparison.Ordinal) &&
                   stateGeometryPartitionControlRow.Notes.Contains("exact_command_shape_rows=0", StringComparison.Ordinal) &&
                   stateGeometryPartitionControlRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the state/geometry partition control.");
            var stateGeometryPayloadJoinControlRow = structuralSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-payload-join-control");
            Assert(stateGeometryPayloadJoinControlRow.GatePassed &&
                   stateGeometryPayloadJoinControlRow.Status == "verified-structural-negative" &&
                   stateGeometryPayloadJoinControlRow.Observed == 1 &&
                   stateGeometryPayloadJoinControlRow.Expected == 1 &&
                   stateGeometryPayloadJoinControlRow.Notes.Contains("joined_object_rows=1", StringComparison.Ordinal) &&
                   stateGeometryPayloadJoinControlRow.Notes.Contains("missing_join_objects=0", StringComparison.Ordinal) &&
                   stateGeometryPayloadJoinControlRow.Notes.Contains("primitive_count_match_rows=1", StringComparison.Ordinal) &&
                   stateGeometryPayloadJoinControlRow.Notes.Contains("five_slot_payload_count_match_rows=1", StringComparison.Ordinal) &&
                   stateGeometryPayloadJoinControlRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not consume the state/geometry payload join control.");
            var audioPayloadCorrelationRow = structuralSummary.Rows
                .Single(row => row.Area == "audio-event-payload-correlation");
            Assert(audioPayloadCorrelationRow.GatePassed &&
                   audioPayloadCorrelationRow.Status == "verified-structural" &&
                   audioPayloadCorrelationRow.Observed == 2 &&
                   audioPayloadCorrelationRow.Notes.Contains("exact_pcm_hash_pairs=0", StringComparison.Ordinal) &&
                   audioPayloadCorrelationRow.Notes.Contains("bounded_policy=first_8_bank_candidates", StringComparison.Ordinal) &&
                   audioPayloadCorrelationRow.Notes.Contains("direct_selection_binding=not-proven", StringComparison.Ordinal) &&
                   audioPayloadCorrelationRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not retain the audio payload correlation control.");
            var authoredIntegratedCoverageRow = structuralSummary.Rows
                .Single(row => row.Area == "authored-integrated-coverage");
            Assert(authoredIntegratedCoverageRow.GatePassed &&
                   authoredIntegratedCoverageRow.Status == "verified-authored" &&
                   authoredIntegratedCoverageRow.Observed == 9 &&
                   authoredIntegratedCoverageRow.Expected == 9 &&
                   authoredIntegratedCoverageRow.Notes.Contains("total_rendered_frames=36", StringComparison.Ordinal) &&
                   authoredIntegratedCoverageRow.Notes.Contains("total_audio_events=18", StringComparison.Ordinal) &&
                   authoredIntegratedCoverageRow.Notes.Contains("total_animation_samples=72", StringComparison.Ordinal) &&
                   authoredIntegratedCoverageRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the authored integrated scenario coverage gate.");
            var authoredRuntimeRow = structuralSummary.Rows
                .Single(row => row.Area == "clean-room-authored-runtime");
            Assert(authoredRuntimeRow.GatePassed &&
                   authoredRuntimeRow.Status == "verified-authored" &&
                   authoredRuntimeRow.Observed == 1 &&
                   authoredRuntimeRow.Expected == 1 &&
                   authoredRuntimeRow.Notes.Contains("gameplay_coverage=True", StringComparison.Ordinal) &&
                   authoredRuntimeRow.Notes.Contains("invariant_coverage=True", StringComparison.Ordinal) &&
                   authoredRuntimeRow.Notes.Contains("integrated_coverage=True", StringComparison.Ordinal) &&
                   authoredRuntimeRow.Notes.Contains("audio_coverage=True", StringComparison.Ordinal) &&
                   authoredRuntimeRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the aggregate authored runtime gate.");

            var reportPath = Path.Combine(temporaryDirectory.FullName, "readiness.tsv");
            ProjectReadinessAudit.WriteReport(reportPath, summary);
            var reportLines = File.ReadAllLines(reportPath);
            Assert(reportLines.Length == summary.Rows.Count + 1 &&
                   reportLines[0].StartsWith("area\tstatus\tgate_passed\t", StringComparison.Ordinal),
                "Readiness audit TSV shape mismatch.");

            var runtimeReportDirectory = animationSummaryDirectory;
            var integrationReportPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-full-stack-integration.tsv");
            CleanRoomIntegrationReport.Write(
                integrationReportPath,
                new CleanRoomIntegrationReportData(
                    CandidateMaterialValidated: true,
                    CandidateHomeEnabled: true,
                    CandidateAwayEnabled: true,
                    RenderedFrames: 140,
                    RenderChecksum: "088EC5D10096A738",
                    DistinctRenderChecksums: 7,
                    AuthoredCharacterAssetFrames: 0,
                    AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
                    AuthoredSceneAssetFrames: 140,
                    AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
                    AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
                    InputSamples: 140,
                    AnimationSamples: 280,
                    MatchEvents: 8,
                    AudioEvents: 8,
                    AudioPrepared: 8,
                    AudioPlayed: 8,
                    AudioUnmapped: 0,
                    GameplayAudioPcmBytes: 1234,
                    GameplayAudioPcmSha256: new string('A', 64),
                    AuthoredAudioAssetManifest: CleanRoomAudioAssetManifest.Default.ManifestId,
                    MatchTraceWritten: true,
                    InputTraceWritten: true,
                    AnimationTraceWritten: true,
                    AudioTraceWritten: true,
                    OpponentAiEnabled: true));
            CandidateIntegratedRuntimeAudit.Write(
                Path.Combine(runtimeReportDirectory, "candidate-integrated-runtime-audit.tsv"),
                new CandidateIntegratedRuntimeAuditResult(
                    Name: "candidate-full-stack",
                    IntegrationReportPath: integrationReportPath,
                    IntegrationSha256: new string('B', 64),
                    IntegrationExists: true,
                    ReplayIntegrationReportPath: Path.Combine(runtimeReportDirectory, "gameplay-full-stack-replayed-integration.tsv"),
                    ReplayIntegrationSha256: new string('B', 64),
                    ReplayIntegrationExists: true,
                    CandidateFlagsValid: true,
                    ProvenanceValid: true,
                    CandidateAssetProvenanceValid: true,
                    CandidateMeshStateCrosscheckValid: true,
                    CandidateAssetCount: 24,
                    CandidateGeometryStreamsComplete: true,
                    CandidateShapeStateStreamsComplete: true,
                    IntegrationReportsMatch: true,
                    TraceFilesValid: true,
                    ReplayMatched: true,
                    RequiredActionsPresent: true,
                    RenderedFrames: 140,
                    AnimationSamples: 280,
                    AudioEvents: 8,
                    AudioPrepared: 8,
                    AudioPlayed: 8,
                    AudioUnmapped: 0,
                    FrameOutputValid: true,
                    FrameVisiblePixels: 123,
                    Status: "verified-candidate",
                    OriginalSemantics: "not-claimed",
                    Issue: string.Empty));
            var authoredCharacterIntegrationPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-character-integration.tsv");
            CleanRoomIntegrationReport.Write(
                authoredCharacterIntegrationPath,
                new CleanRoomIntegrationReportData(
                    CandidateMaterialValidated: false,
                    CandidateHomeEnabled: false,
                    CandidateAwayEnabled: false,
                    RenderedFrames: 140,
                    RenderChecksum: "243EF331249601A5",
                    DistinctRenderChecksums: 7,
                    AuthoredCharacterAssetFrames: 140,
                    AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
                    AuthoredSceneAssetFrames: 140,
                    AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
                    AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
                    InputSamples: 140,
                    AnimationSamples: 280,
                    MatchEvents: 8,
                    AudioEvents: 8,
                    AudioPrepared: 8,
                    AudioPlayed: 8,
                    AudioUnmapped: 0,
                    GameplayAudioPcmBytes: 1234,
                    GameplayAudioPcmSha256: new string('A', 64),
                    AuthoredAudioAssetManifest: CleanRoomAudioAssetManifest.Default.ManifestId,
                    MatchTraceWritten: false,
                    InputTraceWritten: false,
                    AnimationTraceWritten: false,
                    AudioTraceWritten: false));
            var authoredAudioTrace = new[]
            {
                GameplayAudioTraceSerializer.Header,
                "0\tKICKOFF\tintro\tplayed-clean-room-candidate",
                "1\tTRICK\tpanna\tplayed-clean-room-candidate",
                "2\tPASS\tattackgen\tplayed-clean-room-candidate",
                "3\tPASS_COMPLETE\tattackgen\tplayed-clean-room-candidate",
                "4\tSHOT_AWAY\tattackgoal\tplayed-clean-room-candidate",
                "5\tGOAL_AWAY\tgoal\tplayed-clean-room-candidate",
                "6\tSHOT_AWAY\tattackgoal\tplayed-clean-room-candidate",
                "7\tGOAL_AWAY\tgoal\tplayed-clean-room-candidate",
            };
            var authoredAudioTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-full-stack-audio-trace.tsv");
            var authoredAudioReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-full-stack-audio-replayed.tsv");
            File.WriteAllLines(authoredAudioTracePath, authoredAudioTrace);
            File.WriteAllLines(authoredAudioReplayPath, authoredAudioTrace);
            var authoredStealAudioTrace = new[]
            {
                GameplayAudioTraceSerializer.Header,
                "0\tKICKOFF\tintro\tplayed-clean-room-candidate",
                "1\tSTEAL\ttackle\tplayed-clean-room-candidate",
            };
            var authoredStealAudioTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-steal-audio-trace.tsv");
            var authoredStealAudioReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-steal-audio-replayed.tsv");
            File.WriteAllLines(authoredStealAudioTracePath, authoredStealAudioTrace);
            File.WriteAllLines(authoredStealAudioReplayPath, authoredStealAudioTrace);
            var authoredAimedAudioTrace = new[]
            {
                GameplayAudioTraceSerializer.Header,
                "0\tKICKOFF\tintro\tplayed-clean-room-candidate",
                "1\tTRICK\tpanna\tplayed-clean-room-candidate",
                "2\tPASS\tattackgen\tplayed-clean-room-candidate",
                "3\tPASS_COMPLETE\tattackgen\tplayed-clean-room-candidate",
                "4\tSHOT\tattackgoal\tplayed-clean-room-candidate",
                "5\tGOAL_HOME\tgoal\tplayed-clean-room-candidate",
            };
            var authoredAimedAudioTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-aimed-audio-trace.tsv");
            var authoredAimedAudioReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-aimed-audio-replayed.tsv");
            File.WriteAllLines(authoredAimedAudioTracePath, authoredAimedAudioTrace);
            File.WriteAllLines(authoredAimedAudioReplayPath, authoredAimedAudioTrace);
            var audioTraceSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var globalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-global-assignment-control");
            Assert(globalAssignmentRow.GatePassed &&
                   globalAssignmentRow.Status == "verified-structural-negative" &&
                   globalAssignmentRow.Notes.Contains("nonempty_interval_rows=1", StringComparison.Ordinal) &&
                   globalAssignmentRow.Notes.Contains("unique_state_geometry_assignment_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global state/geometry assignment control.");
            var globalUvMaterialAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-uv-material-global-assignment-control");
            Assert(globalUvMaterialAssignmentRow.GatePassed &&
                   globalUvMaterialAssignmentRow.Status == "verified-structural-negative" &&
                   globalUvMaterialAssignmentRow.Notes.Contains("structural_cross_product_model_sum=1", StringComparison.Ordinal) &&
                   globalUvMaterialAssignmentRow.Notes.Contains("unique_state_geometry_uv_material_binding_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global state/geometry/UV/material assignment control.");
            var candidateRenderGlobalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-candidate-render-global-assignment-control");
            Assert(candidateRenderGlobalAssignmentRow.GatePassed &&
                   candidateRenderGlobalAssignmentRow.Status == "verified-structural-negative" &&
                   candidateRenderGlobalAssignmentRow.Notes.Contains("render_group_state_match_rows=0", StringComparison.Ordinal) &&
                   candidateRenderGlobalAssignmentRow.Notes.Contains("unique_state_geometry_candidate_render_binding_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global state/geometry candidate-render assignment control.");
            var payloadIdentityGlobalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-payload-identity-global-assignment-control");
            Assert(payloadIdentityGlobalAssignmentRow.GatePassed &&
                   payloadIdentityGlobalAssignmentRow.Status == "verified-structural-negative" &&
                   payloadIdentityGlobalAssignmentRow.Notes.Contains("payload_identity_reduced_state_domain_rows=0", StringComparison.Ordinal) &&
                   payloadIdentityGlobalAssignmentRow.Notes.Contains("unique_state_geometry_payload_binding_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global state/geometry payload identity assignment control.");
            var drawClassificationGlobalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-original-state-geometry-draw-classification-global-assignment-control");
            Assert(drawClassificationGlobalAssignmentRow.GatePassed &&
                   drawClassificationGlobalAssignmentRow.Status == "verified-structural-negative" &&
                   drawClassificationGlobalAssignmentRow.Notes.Contains("draw_classification_reduced_state_domain_rows=0", StringComparison.Ordinal) &&
                   drawClassificationGlobalAssignmentRow.Notes.Contains("unique_state_geometry_draw_binding_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global state/geometry draw-classification assignment control.");
            var audioSampleGlobalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-event-sample-global-assignment-control");
            Assert(audioSampleGlobalAssignmentRow.GatePassed &&
                   audioSampleGlobalAssignmentRow.Status == "verified-structural-negative" &&
                   audioSampleGlobalAssignmentRow.Notes.Contains("bank_candidate_pool_size=241", StringComparison.Ordinal) &&
                   audioSampleGlobalAssignmentRow.Notes.Contains("structural_cross_product_model_sum=0", StringComparison.Ordinal) &&
                   audioSampleGlobalAssignmentRow.Notes.Contains("unique_evt_abkc_sample_binding_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the global audio EVT-to-sample assignment control.");
            var audioStaticContextGlobalAssignmentRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-event-static-context-global-assignment-control");
            Assert(audioStaticContextGlobalAssignmentRow.GatePassed &&
                   audioStaticContextGlobalAssignmentRow.Status == "verified-structural-negative" &&
                   audioStaticContextGlobalAssignmentRow.Notes.Contains("audio_event_literal_xref_rows=0", StringComparison.Ordinal) &&
                   audioStaticContextGlobalAssignmentRow.Notes.Contains("event_path_literal_xref_count=0", StringComparison.Ordinal) &&
                   audioStaticContextGlobalAssignmentRow.Notes.Contains("event_static_context_reduced_rows=0", StringComparison.Ordinal) &&
                   audioStaticContextGlobalAssignmentRow.Notes.Contains("unique_event_static_selector_proven_rows=0", StringComparison.Ordinal),
                "Readiness audit did not verify the static audio event-context assignment control.");
            var integrationRow = audioTraceSummary.Rows
                .Single(row => row.Area == "full-stack-integration");
            Assert(integrationRow.GatePassed &&
                   integrationRow.Status == "verified-authored" &&
                   integrationRow.Notes.Contains("rendered_frames=140", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("render_checksum=088EC5D10096A738", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("input_samples=140", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("animation_samples=280", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("audio_played=8", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains(
                       "authored_character_asset_manifest=" + CleanRoomCharacterAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains(
                       "authored_audio_asset_manifest=" + CleanRoomAudioAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("input_trace_written=True", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("animation_trace_written=True", StringComparison.Ordinal) &&
                   integrationRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not verify the authored full-stack integration report.");
            var authoredCharacterRow = audioTraceSummary.Rows
                .Single(row => row.Area == "character-authored-asset-runtime");
            Assert(authoredCharacterRow.GatePassed &&
                   authoredCharacterRow.Status == "verified-authored" &&
                   authoredCharacterRow.Observed == 140 &&
                   authoredCharacterRow.Notes.Contains("authored_character_asset_frames=140", StringComparison.Ordinal) &&
                   authoredCharacterRow.Notes.Contains(
                       "authored_character_asset_manifest=" + CleanRoomCharacterAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   authoredCharacterRow.Notes.Contains("manifest_complete=True", StringComparison.Ordinal) &&
                   authoredCharacterRow.Notes.Contains("candidate_home_enabled=False", StringComparison.Ordinal) &&
                   authoredCharacterRow.Notes.Contains("candidate_away_enabled=False", StringComparison.Ordinal),
                "Readiness audit did not verify the authored character asset runtime path.");
            var authoredCharacterAssetsRow = audioTraceSummary.Rows
                .Single(row => row.Area == "character-authored-assets");
            Assert(authoredCharacterAssetsRow.GatePassed &&
                   authoredCharacterAssetsRow.Status == "verified-authored" &&
                   authoredCharacterAssetsRow.Notes.Contains(
                       "authored_character_asset_manifest=" + CleanRoomCharacterAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   authoredCharacterAssetsRow.Notes.Contains("manifest_complete=True", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored character asset manifest.");
            var authoredAssetsRow = audioTraceSummary.Rows
                .Single(row => row.Area == "assets-candidate-runtime");
            Assert(authoredAssetsRow.GatePassed &&
                   authoredAssetsRow.Status == "verified-candidate" &&
                   authoredAssetsRow.Notes.Contains("candidate_material_validated=True", StringComparison.Ordinal) &&
                   authoredAssetsRow.Notes.Contains("candidate_home_enabled=True", StringComparison.Ordinal) &&
                   authoredAssetsRow.Notes.Contains("candidate_away_enabled=True", StringComparison.Ordinal) &&
                   authoredAssetsRow.Notes.Contains("managed_candidate_audit=verified-candidate", StringComparison.Ordinal) &&
                   authoredAssetsRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not separate the verified candidate asset runtime path.");
            var candidateRenderingRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-candidate-runtime");
            Assert(candidateRenderingRow.GatePassed &&
                   candidateRenderingRow.Status == "verified-candidate" &&
                   candidateRenderingRow.Observed == 140 &&
                   candidateRenderingRow.Notes.Contains("candidate_asset_count=24", StringComparison.Ordinal) &&
                   candidateRenderingRow.Notes.Contains("replay_matched=True", StringComparison.Ordinal) &&
                   candidateRenderingRow.Notes.Contains("candidate_render_influence_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the verified candidate rendering runtime path.");
            var authoredRenderingRow = audioTraceSummary.Rows
                .Single(row => row.Area == "rendering-authored-runtime");
            Assert(authoredRenderingRow.GatePassed &&
                   authoredRenderingRow.Status == "verified-authored" &&
                   authoredRenderingRow.Observed == 140 &&
                   authoredRenderingRow.Notes.Contains("distinct_render_checksums=7", StringComparison.Ordinal) &&
                   authoredRenderingRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored rendering runtime path.");
            var authoredGameplayRow = audioTraceSummary.Rows
                .Single(row => row.Area == "gameplay-authored-runtime");
            Assert(authoredGameplayRow.GatePassed &&
                   authoredGameplayRow.Status == "verified-authored" &&
                   authoredGameplayRow.Notes.Contains("match_events=8", StringComparison.Ordinal) &&
                   authoredGameplayRow.Notes.Contains("match_trace_written=True", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored gameplay runtime path.");
            var authoredGameplayInvariantRow = audioTraceSummary.Rows
                .Single(row => row.Area == "gameplay-authored-invariants");
            Assert(authoredGameplayInvariantRow.GatePassed &&
                   authoredGameplayInvariantRow.Status == "verified-authored" &&
                   authoredGameplayInvariantRow.Observed == 1 &&
                   authoredGameplayInvariantRow.Expected == 1 &&
                   authoredGameplayInvariantRow.Notes.Contains("total_frames=4", StringComparison.Ordinal) &&
                   authoredGameplayInvariantRow.Notes.Contains("total_events=1", StringComparison.Ordinal) &&
                   authoredGameplayInvariantRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the authored gameplay invariant gate.");
            var authoredAudioRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-authored-runtime");
            Assert(authoredAudioRow.GatePassed &&
                   authoredAudioRow.Status == "verified-authored" &&
                   authoredAudioRow.Notes.Contains("audio_events=8", StringComparison.Ordinal) &&
                   authoredAudioRow.Notes.Contains("audio_prepared=8", StringComparison.Ordinal) &&
                   authoredAudioRow.Notes.Contains("audio_played=8", StringComparison.Ordinal) &&
                   authoredAudioRow.Notes.Contains("audio_unmapped=0", StringComparison.Ordinal) &&
                   authoredAudioRow.Notes.Contains(
                       "authored_audio_asset_manifest=" + CleanRoomAudioAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal),
                "Readiness audit did not record the verified authored audio runtime path.");
            var candidateAudioRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-candidate-runtime");
            Assert(candidateAudioRow.GatePassed &&
                   candidateAudioRow.Status == "verified-candidate" &&
                   candidateAudioRow.Observed == 8 &&
                   candidateAudioRow.Notes.Contains("candidate_audio_events=8", StringComparison.Ordinal) &&
                   candidateAudioRow.Notes.Contains("audio_prepared=8", StringComparison.Ordinal) &&
                   candidateAudioRow.Notes.Contains("audio_played=8", StringComparison.Ordinal) &&
                   candidateAudioRow.Notes.Contains("audio_unmapped=0", StringComparison.Ordinal) &&
                   candidateAudioRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the verified candidate audio runtime path.");
            var authoredAudioAssetsRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-authored-assets");
            var authoredAudioCoverageRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-authored-coverage");
            Assert(authoredAudioCoverageRow.GatePassed &&
                   authoredAudioCoverageRow.Status == "verified-authored" &&
                   authoredAudioCoverageRow.Observed == 1 &&
                   authoredAudioCoverageRow.Expected == 1 &&
                   authoredAudioCoverageRow.Notes.Contains("total_events=15", StringComparison.Ordinal) &&
                   authoredAudioCoverageRow.Notes.Contains("total_prepared=15", StringComparison.Ordinal) &&
                   authoredAudioCoverageRow.Notes.Contains("total_played=15", StringComparison.Ordinal) &&
                   authoredAudioCoverageRow.Notes.Contains("total_unmapped=0", StringComparison.Ordinal) &&
                   authoredAudioCoverageRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not record the authored audio coverage gate.");
            Assert(authoredAudioAssetsRow.GatePassed &&
                   authoredAudioAssetsRow.Status == "verified-authored" &&
                   authoredAudioAssetsRow.Notes.Contains(
                       "authored_audio_asset_manifest=" + CleanRoomAudioAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   authoredAudioAssetsRow.Notes.Contains("manifest_complete=True", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored audio asset manifest.");
            var authoredAnimationRow = audioTraceSummary.Rows
                .Single(row => row.Area == "animation-authored-runtime");
            Assert(authoredAnimationRow.GatePassed &&
                   authoredAnimationRow.Status == "verified-authored" &&
                   authoredAnimationRow.Notes.Contains("animation_samples=280", StringComparison.Ordinal) &&
                   authoredAnimationRow.Notes.Contains("animation_trace_written=True", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored animation runtime path.");
            var authoredAnimationAssetsRow = audioTraceSummary.Rows
                .Single(row => row.Area == "animation-authored-assets");
            Assert(authoredAnimationAssetsRow.GatePassed &&
                   authoredAnimationAssetsRow.Status == "verified-authored" &&
                   authoredAnimationAssetsRow.Notes.Contains(
                       "authored_animation_asset_manifest=" + CleanRoomAnimationAssetManifest.Default.ManifestId,
                       StringComparison.Ordinal) &&
                   authoredAnimationAssetsRow.Notes.Contains("manifest_complete=True", StringComparison.Ordinal),
                "Readiness audit did not record the verified authored animation asset manifest.");
            var audioTraceNotes = audioTraceSummary.Rows
                .Single(row => row.Area == "audio")
                .Notes;
            Assert(audioTraceNotes.Contains("authored_audio_trace_replay=verified", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_audio_trace_events=8", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_audio_trace_has_kickoff=True", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_audio_trace_all_played=True", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_steal_audio_trace_replay=verified", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_steal_audio_trace_events=2", StringComparison.Ordinal) &&
                   audioTraceNotes.Contains("authored_steal_audio_trace_has_tackle=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored gameplay audio traces.");
            var aimedAudioTraceRow = audioTraceSummary.Rows
                .Single(row => row.Area == "audio-aimed-trace");
            Assert(aimedAudioTraceRow.GatePassed &&
                   aimedAudioTraceRow.Status == "verified-authored" &&
                   aimedAudioTraceRow.Notes.Contains("authored_aimed_audio_trace_replay=verified", StringComparison.Ordinal) &&
                   aimedAudioTraceRow.Notes.Contains("authored_aimed_audio_trace_events=6", StringComparison.Ordinal) &&
                   aimedAudioTraceRow.Notes.Contains("authored_aimed_audio_trace_has_shot=True", StringComparison.Ordinal) &&
                   aimedAudioTraceRow.Notes.Contains("authored_aimed_audio_trace_has_goal=True", StringComparison.Ordinal) &&
                   aimedAudioTraceRow.Notes.Contains("authored_aimed_audio_trace_all_played=True", StringComparison.Ordinal) &&
                   aimedAudioTraceRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not verify the authored aimed audio trace gate.");

            var authoredRoundTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t21\tTRICK\t0\t0\tHome",
                "2\t23\tPASS\t0\t0\tNone",
                "3\t25\tPASS_COMPLETE\t0\t0\tAway",
                "4\t67\tSHOT\t0\t0\tNone",
                "5\t69\tGOAL_HOME\t1\t0\tNone",
                "6\t100\tRESTART\t1\t0\tHome",
            };
            var authoredRoundTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-script-round-trace.tsv");
            var authoredRoundReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-script-round-replayed.tsv");
            File.WriteAllLines(authoredRoundTracePath, authoredRoundTrace);
            File.WriteAllLines(authoredRoundReplayPath, authoredRoundTrace);

            var authoredAimedTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t21\tTRICK\t0\t0\tHome",
                "2\t23\tPASS\t0\t0\tNone",
                "3\t25\tPASS_COMPLETE\t0\t0\tAway",
                "4\t67\tSHOT\t0\t0\tNone",
                "5\t69\tGOAL_HOME\t1\t0\tNone",
            };
            var authoredAimedTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-aimed-match-trace.tsv");
            var authoredAimedReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-aimed-match-replayed.tsv");
            File.WriteAllLines(authoredAimedTracePath, authoredAimedTrace);
            File.WriteAllLines(authoredAimedReplayPath, authoredAimedTrace);
            var authoredAimedInputSamples = Enumerable.Range(0, 80)
                .Select(frame => new CleanRoomInputSample(
                    frame,
                    frame == 67
                        ? new MatchInput(0, 0.75f, false, true, false)
                        : new MatchInput()))
                .ToArray();
            var authoredAimedInputTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-clean-room-aimed-input-trace.tsv");
            var authoredAimedInputReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-clean-room-aimed-input-replayed.tsv");
            File.WriteAllLines(
                authoredAimedInputTracePath,
                CleanRoomInputTraceSerializer.BuildLines(authoredAimedInputSamples));
            File.WriteAllLines(
                authoredAimedInputReplayPath,
                CleanRoomInputTraceSerializer.BuildLines(authoredAimedInputSamples));

            var verifiedSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var aimedTraceRow = verifiedSummary.Rows
                .Single(row => row.Area == "gameplay-aimed-trace");
            Assert(aimedTraceRow.GatePassed &&
                   aimedTraceRow.Status == "verified-authored" &&
                   aimedTraceRow.Notes.Contains("authored_aimed_trace_replay=verified", StringComparison.Ordinal) &&
                   aimedTraceRow.Notes.Contains("authored_aimed_trace_events=6", StringComparison.Ordinal) &&
                   aimedTraceRow.Notes.Contains("authored_aimed_trace_has_shot=True", StringComparison.Ordinal) &&
                   aimedTraceRow.Notes.Contains("authored_aimed_trace_has_goal=True", StringComparison.Ordinal) &&
                   aimedTraceRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not verify the authored aimed-demo trace gate.");
            var aimedInputTraceRow = verifiedSummary.Rows
                .Single(row => row.Area == "gameplay-aimed-input-trace");
            Assert(aimedInputTraceRow.GatePassed &&
                   aimedInputTraceRow.Status == "verified-authored" &&
                   aimedInputTraceRow.Notes.Contains("authored_aimed_input_trace_replay=verified", StringComparison.Ordinal) &&
                   aimedInputTraceRow.Notes.Contains("authored_aimed_input_trace_samples=80", StringComparison.Ordinal) &&
                   aimedInputTraceRow.Notes.Contains("authored_aimed_input_trace_has_vertical_shot=True", StringComparison.Ordinal) &&
                   aimedInputTraceRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not verify the authored aimed input trace gate.");
            var gameplayNotes = verifiedSummary.Rows
                .Single(row => row.Area == "gameplay")
                .Notes;
            Assert(gameplayNotes.Contains("authored_round_trace_replay=verified", StringComparison.Ordinal) &&
                   gameplayNotes.Contains("authored_round_trace_events=7", StringComparison.Ordinal) &&
                   gameplayNotes.Contains("authored_round_trace_has_goal=True", StringComparison.Ordinal) &&
                   gameplayNotes.Contains("authored_round_trace_has_restart=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored round trace gate.");

            var authoredGamebreakerTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t21\tTRICK\t0\t0\tHome",
                "2\t23\tTRICK\t0\t0\tHome",
                "3\t25\tTRICK\t0\t0\tHome",
                "4\t27\tTRICK\t0\t0\tHome",
                "5\t29\tGAMEBREAKER\t0\t0\tNone",
                "6\t34\tGOAL_HOME\t1\t0\tNone",
            };
            var authoredGamebreakerTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-gamebreaker-match-trace.tsv");
            var authoredGamebreakerReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-gamebreaker-match-replayed.tsv");
            File.WriteAllLines(authoredGamebreakerTracePath, authoredGamebreakerTrace);
            File.WriteAllLines(authoredGamebreakerReplayPath, authoredGamebreakerTrace);

            var gamebreakerSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var gamebreakerNotes = gamebreakerSummary.Rows
                .Single(row => row.Area == "gameplay")
                .Notes;
            Assert(gamebreakerNotes.Contains("authored_gamebreaker_trace_replay=verified", StringComparison.Ordinal) &&
                   gamebreakerNotes.Contains("authored_gamebreaker_trace_events=7", StringComparison.Ordinal) &&
                   gamebreakerNotes.Contains("authored_gamebreaker_trace_has_action=True", StringComparison.Ordinal) &&
                   gamebreakerNotes.Contains("authored_gamebreaker_trace_has_goal=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored gamebreaker trace gate.");

            var authoredJuggleTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t21\tJUGGLE\t0\t0\tHome",
                "2\t23\tJUGGLE\t0\t0\tHome",
                "3\t25\tJUGGLE\t0\t0\tHome",
                "4\t27\tJUGGLE\t0\t0\tHome",
                "5\t45\tSHOT\t0\t0\tNone",
                "6\t52\tGOAL_HOME\t1\t0\tNone",
            };
            var authoredJuggleTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-juggle-match-trace.tsv");
            var authoredJuggleReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-juggle-match-replayed.tsv");
            File.WriteAllLines(authoredJuggleTracePath, authoredJuggleTrace);
            File.WriteAllLines(authoredJuggleReplayPath, authoredJuggleTrace);

            var juggleSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var juggleNotes = juggleSummary.Rows
                .Single(row => row.Area == "gameplay")
                .Notes;
            Assert(juggleNotes.Contains("authored_juggle_trace_replay=verified", StringComparison.Ordinal) &&
                   juggleNotes.Contains("authored_juggle_trace_events=7", StringComparison.Ordinal) &&
                   juggleNotes.Contains("authored_juggle_trace_has_action=True", StringComparison.Ordinal) &&
                   juggleNotes.Contains("authored_juggle_trace_has_goal=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored juggle trace gate.");

            var authoredPannaTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t30\tPANNA\t0\t0\tHome",
                "2\t31\tFULL_TIME\t0\t0\tHome",
            };
            var authoredPannaTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-panna-match-trace.tsv");
            var authoredPannaReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-panna-match-replayed.tsv");
            File.WriteAllLines(authoredPannaTracePath, authoredPannaTrace);
            File.WriteAllLines(authoredPannaReplayPath, authoredPannaTrace);

            var authoredFullTimeTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t16\tFULL_TIME\t0\t0\tHome",
            };
            var authoredFullTimeTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-full-time-match-trace.tsv");
            var authoredFullTimeReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-full-time-match-replayed.tsv");
            File.WriteAllLines(authoredFullTimeTracePath, authoredFullTimeTrace);
            File.WriteAllLines(authoredFullTimeReplayPath, authoredFullTimeTrace);

            var extendedGameplaySummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var extendedGameplayNotes = extendedGameplaySummary.Rows
                .Single(row => row.Area == "gameplay")
                .Notes;
            Assert(extendedGameplayNotes.Contains("authored_panna_trace_replay=verified", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_panna_trace_events=3", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_panna_trace_has_action=True", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_panna_trace_has_full_time=True", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_full_time_trace_replay=verified", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_full_time_trace_events=2", StringComparison.Ordinal) &&
                   extendedGameplayNotes.Contains("authored_full_time_trace_has_action=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored panna and FULL_TIME trace gates.");

            var authoredStealTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t33\tSTEAL\t0\t0\tAway",
            };
            var authoredStealTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-steal-match-trace.tsv");
            var authoredStealReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-steal-match-replayed.tsv");
            File.WriteAllLines(authoredStealTracePath, authoredStealTrace);
            File.WriteAllLines(authoredStealReplayPath, authoredStealTrace);

            var stealSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var stealNotes = stealSummary.Rows
                .Single(row => row.Area == "gameplay")
                .Notes;
            Assert(stealNotes.Contains("authored_steal_trace_replay=verified", StringComparison.Ordinal) &&
                   stealNotes.Contains("authored_steal_trace_events=2", StringComparison.Ordinal) &&
                   stealNotes.Contains("authored_steal_trace_has_action=True", StringComparison.Ordinal),
                "Readiness audit did not verify the authored STEAL trace gate.");

            var authoredTackleTrace = new[]
            {
                MatchTraceSerializer.Header,
                "0\t0\tKICKOFF\t0\t0\tNone",
                "1\t33\tSTEAL\t0\t0\tAway",
                "2\t47\tTACKLE\t0\t0\tHome",
                "3\t61\tSTEAL\t0\t0\tAway",
                "4\t80\tSHOT_AWAY\t0\t0\tNone",
            };
            var authoredTackleTracePath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-tackle-match-trace.tsv");
            var authoredTackleReplayPath = Path.Combine(
                runtimeReportDirectory,
                "gameplay-authored-tackle-match-replayed.tsv");
            File.WriteAllLines(authoredTackleTracePath, authoredTackleTrace);
            File.WriteAllLines(authoredTackleReplayPath, authoredTackleTrace);

            var tackleSummary = ProjectReadinessAudit.Build(
                catalog,
                isoPath,
                temporaryDirectory.FullName);
            var tackleRow = tackleSummary.Rows
                .Single(row => row.Area == "gameplay-tackle-trace");
            Assert(tackleRow.GatePassed &&
                   tackleRow.Status == "verified-authored" &&
                   tackleRow.Observed == 5 &&
                   tackleRow.Notes.Contains("authored_tackle_trace_replay=verified", StringComparison.Ordinal) &&
                   tackleRow.Notes.Contains("authored_tackle_trace_has_action=True", StringComparison.Ordinal) &&
                   tackleRow.Notes.Contains("original_semantics=not-claimed", StringComparison.Ordinal),
                "Readiness audit did not verify the authored TACKLE trace gate.");
        }
        finally
        {
            Directory.Delete(temporaryDirectory.FullName, recursive: true);
        }
    }

    private sealed class ScriptedInputSource : IInputSource
    {
        private readonly Queue<MatchInput> inputs;

        public ScriptedInputSource(params MatchInput[] inputs)
        {
            this.inputs = new Queue<MatchInput>(inputs);
        }

        public MatchInput Poll() => inputs.Count > 0 ? inputs.Dequeue() : new MatchInput();
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidDataException(message);
        }
    }
}
