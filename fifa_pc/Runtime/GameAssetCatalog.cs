using FifaPcCompat.Runtime;

namespace FifaStreetPc;

public sealed record AssetArchive(string Path, string Magic, int EntryCount, bool IndexValidated);
public sealed record AudioStreamAsset(
    string Path,
    long Size,
    bool HeaderValidated,
    EaSchlAudioInfo? Info,
    string? Issue);
public sealed record AudioBankAsset(
    string Path,
    long Size,
    bool StructureValidated,
    int ModuleCount,
    int SampleTableCount,
    int SoundCount,
    int StreamSoundCount,
    bool BnkPresent,
    bool BnkStructureValidated,
    int BnkSoundCount,
    int BnkValidHeaderCount,
    int AstDataSchlReferenceCount,
    int AstLoopSchlReferenceCount,
    IReadOnlyList<AudioAbkAstRange> AstRanges,
    string? AstPath,
    string? Issue)
{
    /// <summary>
    /// Validated ABKC sound entries. This is raw table evidence only; it is
    /// not an EVT selector or gameplay/audio semantic binding.
    /// </summary>
    public IReadOnlyList<AudioAbkSoundEntry> SoundEntries { get; init; } =
        Array.Empty<AudioAbkSoundEntry>();
    public int BnkParsedHeaderCount { get; init; }
    public int BnkXboxImaHeaderCount { get; init; }
    public int BnkXboxImaDataValidatedCount { get; init; }
    public int BnkXboxImaDataRejectedCount { get; init; }
}
public sealed record AnimationBankAsset(
    string CatalogEntry,
    string BankName,
    int DeclaredFileCount,
    int CatalogFileCount,
    bool CatalogFileCountMatches,
    string? OrdEntry,
    string? OrlEntry,
    long OrdSize,
    long OrlSize,
    bool ElfValidated,
    bool SymbolMatchesCatalog,
    string NormalizedDataPayloadSignature,
    string? Issue)
{
    public string VersionTag { get; init; } = string.Empty;
    public string Directory { get; init; } = string.Empty;
    public IReadOnlyList<AnimationCatalogEntry> CatalogFiles { get; init; } = Array.Empty<AnimationCatalogEntry>();
    public IReadOnlyList<ElfSymbol> AnimationBankSymbolRecords { get; init; } = Array.Empty<ElfSymbol>();
}
public sealed record CandidateMaterialAsset(
    string ModelArchiveName,
    string ModelObjectName,
    string ShapeName,
    string ShapeImageArchiveName,
    int Width,
    int Height,
    string PixelFormat,
    int DecodedRgbaBytes);
public sealed record MorphSiblingAsset(
    string NestedArchiveName,
    int MorphEvidenceCount,
    int NormalizedSiblingCandidateCount,
    int CompleteSiblingCount,
    int SiblingElfValidatedCount,
    int SiblingGeometryCount,
    int SiblingBoundsCount,
    int PosePaletteBankSymbolCount,
    string? Issue)
{
    public IReadOnlyList<string> PosePaletteBankSymbols { get; init; } = Array.Empty<string>();
    public IReadOnlyList<ElfSymbol> PosePaletteBankSymbolRecords { get; init; } = Array.Empty<ElfSymbol>();
}
public sealed record AudioEventCandidateAsset(
    string EventPath,
    string HeaderPath,
    string DataPath,
    AudioEventNamedHeaderCandidate Candidate,
    EaSchlAudioInfo? Info,
    string? Issue)
{
    public bool HeaderValidated => Info is not null;
    public int? PayloadChannelLayoutCandidate { get; init; }
    public long? PayloadChannelLayoutFrames { get; init; }
}
public sealed record TextureImageAsset(
    string Path,
    long Size,
    bool HeaderValidated,
    bool DecodeValidated,
    XprInspection? Inspection,
    string? Issue);
public sealed record VivFPackage(
    string Path,
    string CompanionPath,
    int EntryCount,
    bool IndexValidated,
    bool CompanionRangesValid,
    bool SentinelMatchesCompanion);

public sealed class GameAssetCatalog
{
    private GameAssetCatalog(
        IReadOnlyList<string> dataFiles,
        IReadOnlyList<AssetArchive> archives,
        IReadOnlyList<GameAssetDescriptor> assets,
        IReadOnlyList<VivFPackage> vivfPackages,
        IReadOnlyList<AudioBankAsset> audioBanks,
        IReadOnlyList<AnimationBankAsset> animationBanks,
        IReadOnlyList<AptPackageAsset> aptPackages,
        IReadOnlyList<CandidateMaterialAsset> candidateMaterials,
        IReadOnlyList<MorphSiblingAsset> morphSiblings,
        IReadOnlyList<AudioStreamAsset> audioStreams,
        IReadOnlyList<TextureImageAsset> textureImages,
        IReadOnlyList<AudioEventCandidateAsset> audioEventCandidates)
    {
        DataFiles = dataFiles;
        Archives = archives;
        Assets = assets;
        VivfPackages = vivfPackages;
        AudioBanks = audioBanks;
        AnimationBanks = animationBanks;
        AptPackages = aptPackages;
        CandidateMaterials = candidateMaterials;
        MorphSiblings = morphSiblings;
        AudioStreams = audioStreams;
        TextureImages = textureImages;
        AudioEventCandidates = audioEventCandidates;
    }

    public IReadOnlyList<string> DataFiles { get; }
    public IReadOnlyList<AssetArchive> Archives { get; }
    public IReadOnlyList<GameAssetDescriptor> Assets { get; }
    public IReadOnlyList<VivFPackage> VivfPackages { get; }
    public IReadOnlyList<AudioBankAsset> AudioBanks { get; }
    public IReadOnlyList<AnimationBankAsset> AnimationBanks { get; }
    public IReadOnlyList<AptPackageAsset> AptPackages { get; }
    public IReadOnlyList<CandidateMaterialAsset> CandidateMaterials { get; }
    public IReadOnlyList<MorphSiblingAsset> MorphSiblings { get; }
    public IReadOnlyList<AudioStreamAsset> AudioStreams { get; }
    public IReadOnlyList<TextureImageAsset> TextureImages { get; }
    public IReadOnlyList<AudioEventCandidateAsset> AudioEventCandidates { get; }

    internal static GameAssetCatalog CreateForTest(
        IReadOnlyList<AudioStreamAsset> audioStreams,
        IReadOnlyList<TextureImageAsset>? textureImages = null,
        IReadOnlyList<AudioBankAsset>? audioBanks = null,
        IReadOnlyList<AnimationBankAsset>? animationBanks = null,
        IReadOnlyList<AptPackageAsset>? aptPackages = null,
        IReadOnlyList<CandidateMaterialAsset>? candidateMaterials = null,
        IReadOnlyList<MorphSiblingAsset>? morphSiblings = null,
        IReadOnlyList<AudioEventCandidateAsset>? audioEventCandidates = null) =>
        new(
            Array.Empty<string>(),
            Array.Empty<AssetArchive>(),
            Array.Empty<GameAssetDescriptor>(),
            Array.Empty<VivFPackage>(),
            audioBanks ?? Array.Empty<AudioBankAsset>(),
            animationBanks ?? Array.Empty<AnimationBankAsset>(),
            aptPackages ?? Array.Empty<AptPackageAsset>(),
            candidateMaterials ?? Array.Empty<CandidateMaterialAsset>(),
            morphSiblings ?? Array.Empty<MorphSiblingAsset>(),
            audioStreams,
            textureImages ?? Array.Empty<TextureImageAsset>(),
            audioEventCandidates ?? Array.Empty<AudioEventCandidateAsset>());

    public static GameAssetCatalog Scan(IXboxFileSystem fileSystem)
    {
        var dataFiles = fileSystem.Enumerate("data")
            .Where(path => !path.EndsWith("/", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var assets = GameAssetInventory.Scan(fileSystem, dataFiles);
        var archives = new List<AssetArchive>();
        var vivfPackages = new List<VivFPackage>();
        var audioBanks = new List<AudioBankAsset>();
        var animationBanks = new List<AnimationBankAsset>();
        var aptPackages = new List<AptPackageAsset>();
        var candidateMaterials = new List<CandidateMaterialAsset>();
        var morphSiblings = new List<MorphSiblingAsset>();
        var audioStreams = new List<AudioStreamAsset>();
        var textureImages = new List<TextureImageAsset>();
        var audioEventCandidates = new List<AudioEventCandidateAsset>();
        var dataFileSet = dataFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in assets.Where(asset => asset.Signature == "VivF"))
        {
            var companionPath = FindVivFCompanion(asset.Path, dataFileSet);
            using var packageStream = fileSystem.OpenReadOnly(asset.Path);
            var package = VivFReader.Inspect(packageStream);
            var rangesValid = false;
            var sentinelValid = false;
            if (companionPath is not null)
            {
                using var companionStream = fileSystem.OpenReadOnly(companionPath);
                var bound = VivFReader.BindToCompanion(package, companionStream);
                rangesValid = bound.RangesWithinCompanion;
                sentinelValid = bound.SentinelMatchesCompanionLength;
            }
            vivfPackages.Add(new VivFPackage(
                asset.Path,
                companionPath ?? "(not found)",
                checked((int)package.EntryCount),
                package.DeclaredSizeMatches && package.OffsetsMonotonic,
                rangesValid,
                sentinelValid));
        }

        foreach (var asset in assets.Where(asset =>
                     asset.Path.EndsWith(".big", StringComparison.OrdinalIgnoreCase) &&
                     (asset.Signature is "BIG4" or "BIGF")))
        {
            try
            {
                var archive = BigArchiveReader.Inspect(fileSystem, asset.Path);
                archives.Add(new AssetArchive(asset.Path, archive.Magic, checked((int)archive.FileCount), archive.ArchiveSizeMatches));
            }
            catch (InvalidDataException exception)
            {
                throw new InvalidDataException($"Invalid BIG archive '{asset.Path}'.", exception);
            }
        }

        foreach (var asset in assets.Where(asset =>
                     asset.Kind == AssetKind.AudioStream &&
                     asset.Path.EndsWith(".ast", StringComparison.OrdinalIgnoreCase)))
        {
            using var stream = fileSystem.OpenReadOnly(asset.Path);
            try
            {
                var info = EaSchlAudioDecoder.Inspect(stream);
                audioStreams.Add(new AudioStreamAsset(asset.Path, asset.Size, true, info, null));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                audioStreams.Add(new AudioStreamAsset(asset.Path, asset.Size, false, null, exception.Message));
            }
        }

        foreach (var asset in assets.Where(asset =>
                     asset.Kind == AssetKind.AudioBank &&
                     asset.Path.EndsWith(".abk", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                if (asset.Size > 16 * 1024 * 1024)
                {
                    throw new InvalidDataException("ABKC asset exceeds the catalog validation limit.");
                }

                using var bankStream = fileSystem.OpenReadOnly(asset.Path);
                var bankData = new byte[checked((int)bankStream.Length)];
                bankStream.ReadExactly(bankData);
                var inspection = AudioAbkReader.Inspect(bankData);
                var astPath = asset.Path[..^4] + ".ast";
                var dataSchlReferences = 0;
                var loopSchlReferences = 0;
                var astRanges = Array.Empty<AudioAbkAstRange>();
                string? resolvedAstPath = null;
                var bnkPresent = inspection.BnkOffset != 0;
                var bnkStructureValidated = !bnkPresent;
                var bnkSoundCount = 0;
                var bnkValidHeaderCount = 0;
                var bnkParsedHeaderCount = 0;
                var bnkXboxImaHeaderCount = 0;
                var bnkXboxImaDataValidatedCount = 0;
                var bnkXboxImaDataRejectedCount = 0;
                if (bnkPresent)
                {
                    var bnk = AudioAbkReader.InspectEmbeddedBnk(inspection, bankData);
                    bnkStructureValidated = bnk is not null;
                    bnkSoundCount = bnk?.SoundCount ?? 0;
                    bnkValidHeaderCount = bnk?.ValidHeaderCount ?? 0;
                    if (bnk is not null)
                    {
                        var headers = AudioAbkReader.InspectEmbeddedBnkHeaders(bnk, bankData);
                        bnkParsedHeaderCount = headers.Count(header => header.Parsed);
                        bnkXboxImaHeaderCount = headers.Count(header =>
                            AudioAbkReader.TryGetBnkStreamDescriptor(header, out var descriptor) &&
                            descriptor?.Codec == 0x14);
                        var validations = AudioAbkReader.ValidateEmbeddedBnkStreams(bnk, bankData);
                        bnkXboxImaDataValidatedCount = validations.Count(validation => validation.Valid);
                        bnkXboxImaDataRejectedCount = validations.Count(validation => !validation.Valid);
                    }
                }
                if (dataFileSet.Contains(astPath))
                {
                    using var astStream = fileSystem.OpenReadOnly(astPath);
                    var references = AudioAbkReader.InspectAstReferences(inspection, astStream);
                    dataSchlReferences = references.DataOffsetSchlCount;
                    loopSchlReferences = references.LoopOffsetSchlCount;
                    astRanges = AudioAbkReader.BuildAstRanges(inspection, astStream.Length).ToArray();
                    resolvedAstPath = astPath;
                }

                audioBanks.Add(new AudioBankAsset(
                    asset.Path,
                    asset.Size,
                    true,
                    inspection.ModuleCount,
                    inspection.SampleTables.Count,
                    inspection.Sounds.Count,
                    inspection.StreamSoundCount,
                    bnkPresent,
                    bnkStructureValidated,
                    bnkSoundCount,
                    bnkValidHeaderCount,
                    dataSchlReferences,
                    loopSchlReferences,
                    astRanges,
                    resolvedAstPath,
                    null)
                {
                    SoundEntries = inspection.Sounds
                        .Where(sound => !sound.IsDummy)
                        .OrderBy(sound => sound.SoundIndex)
                        .ToArray(),
                    BnkParsedHeaderCount = bnkParsedHeaderCount,
                    BnkXboxImaHeaderCount = bnkXboxImaHeaderCount,
                    BnkXboxImaDataValidatedCount = bnkXboxImaDataValidatedCount,
                    BnkXboxImaDataRejectedCount = bnkXboxImaDataRejectedCount,
                });
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                audioBanks.Add(new AudioBankAsset(
                    asset.Path,
                    asset.Size,
                    false,
                    0,
                    0,
                    0,
                    0,
                    false,
                    false,
                    0,
                    0,
                    0,
                    0,
                    Array.Empty<AudioAbkAstRange>(),
                    null,
                    exception.Message));
            }
        }

        ScanAnimationBanks(fileSystem, dataFileSet, animationBanks);
        const string modelArchivePath = "data/models.big";
        if (dataFileSet.Contains(modelArchivePath))
        {
            ScanMorphSiblings(fileSystem, modelArchivePath, morphSiblings);
            try
            {
                foreach (var match in EaglShapeMaterialLoader.LoadSameTrunkMatches(
                             fileSystem,
                             modelArchivePath))
                {
                    candidateMaterials.Add(new CandidateMaterialAsset(
                        match.ModelArchiveName,
                        match.ModelObjectName,
                        match.ShapeName,
                        match.ShapeImageArchiveName,
                        match.Texture.Width,
                        match.Texture.Height,
                        match.Texture.PixelFormat,
                        match.Texture.DecodedRgba.Length));
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                // Candidate material inventory remains empty when the model
                // package cannot be inspected; no global binding is inferred.
            }
        }
        foreach (var asset in assets.Where(asset =>
                     asset.Kind == AssetKind.AnimationPackage &&
                     asset.Path.Contains("/aptanims/", StringComparison.OrdinalIgnoreCase)))
        {
            aptPackages.Add(AptPackageCatalogReader.Inspect(
                fileSystem,
                asset.Path,
                FindVivFCompanion(asset.Path, dataFileSet)));
        }

        foreach (var asset in assets.Where(asset => asset.Kind == AssetKind.TextureImage))
        {
            try
            {
                if (asset.Size > 64 * 1024 * 1024)
                {
                    throw new InvalidDataException("XPR0 asset exceeds the catalog validation limit.");
                }

                using var stream = fileSystem.OpenReadOnly(asset.Path);
                var data = new byte[checked((int)stream.Length)];
                stream.ReadExactly(data);
                var inspection = XprReader.Inspect(data);
                _ = TextureAssetLoader.Load(data);
                textureImages.Add(new TextureImageAsset(asset.Path, asset.Size, true, true, inspection, null));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                textureImages.Add(new TextureImageAsset(asset.Path, asset.Size, false, false, null, exception.Message));
            }
        }

        const string eventPath = "data/audio/eventspa.evt";
        const string eventHeaderPath = "data/audio/pcpahdr.big";
        const string eventHeaderOffPath = "data/audio/pcpahdr.off";
        const string eventDataPath = "data/audio/pcpadat.big";
        const string eventDataOffPath = "data/audio/pcpadat.off";
        if (dataFileSet.IsSupersetOf(new[]
            { eventPath, eventHeaderPath, eventHeaderOffPath, eventDataPath, eventDataOffPath }))
        {
            try
            {
                using var eventStream = fileSystem.OpenReadOnly(eventPath);
                var eventData = ReadBounded(eventStream, 1 * 1024 * 1024);
                using var headerOffStream = fileSystem.OpenReadOnly(eventHeaderOffPath);
                using var headerBigStream = fileSystem.OpenReadOnly(eventHeaderPath);
                var headerTable = AudioAssetReader.InspectOffsetTable(
                    ReadBounded(headerOffStream, 1 * 1024 * 1024),
                    headerBigStream.Length);
                using var dataOffStream = fileSystem.OpenReadOnly(eventDataOffPath);
                using var dataBigStream = fileSystem.OpenReadOnly(eventDataPath);
                var dataTable = AudioAssetReader.InspectOffsetTable(
                    ReadBounded(dataOffStream, 1 * 1024 * 1024),
                    dataBigStream.Length);
                using var headerCompanion = fileSystem.OpenReadOnly(eventHeaderPath);
                var namedCandidates = AudioAssetReader.CorrelateEventTextStartsToHeaderRows(
                    eventData,
                    headerCompanion,
                    headerTable,
                    dataTable);
                using var dataCompanion = fileSystem.OpenReadOnly(eventDataPath);
                foreach (var candidate in namedCandidates)
                {
                    if (candidate.ParallelDataRow is not { } dataRow || dataRow.SecondColumn < 8)
                    {
                        audioEventCandidates.Add(new AudioEventCandidateAsset(
                            eventPath,
                            eventHeaderPath,
                            eventDataPath,
                            candidate,
                            null,
                            "parallel PA data row is unavailable or too short"));
                        continue;
                    }

                    try
                    {
                        using var range = new AudioRangeStream(
                            dataCompanion,
                            dataRow.FirstColumn,
                            dataRow.SecondColumn);
                        var info = EaSchlAudioDecoder.Inspect(range);
                        var candidateAsset = new AudioEventCandidateAsset(
                            eventPath,
                            eventHeaderPath,
                            eventDataPath,
                            candidate,
                            info,
                            null);
                        if (info.Channels == 0)
                        {
                            try
                            {
                                if (AudioScclChannelLayoutAuditor.TryGetUniqueCandidate(
                                        dataCompanion,
                                        dataRow,
                                        out var candidateChannels,
                                        out var candidateFrames))
                                {
                                    candidateAsset = candidateAsset with
                                    {
                                        PayloadChannelLayoutCandidate = candidateChannels,
                                        PayloadChannelLayoutFrames = candidateFrames,
                                    };
                                }
                            }
                            catch (Exception exception) when (
                                exception is InvalidDataException or EndOfStreamException or OverflowException or NotSupportedException)
                            {
                                // Keep the observed zero-channel header and
                                // leave the candidate layout unproven.
                            }
                        }

                        audioEventCandidates.Add(candidateAsset);
                    }
                    catch (Exception exception) when (
                        exception is InvalidDataException or EndOfStreamException or OverflowException)
                    {
                        audioEventCandidates.Add(new AudioEventCandidateAsset(
                            eventPath,
                            eventHeaderPath,
                            eventDataPath,
                            candidate,
                            null,
                            exception.Message));
                    }
                }
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                audioEventCandidates.Clear();
            }
        }

        return new GameAssetCatalog(
            dataFiles,
            archives,
            assets,
            vivfPackages,
            audioBanks,
            animationBanks,
            aptPackages,
            candidateMaterials,
            morphSiblings,
            audioStreams,
            textureImages,
            audioEventCandidates);
    }

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        if (stream.Length > maximumBytes)
        {
            throw new InvalidDataException("Asset exceeds the bounded catalog read limit.");
        }

        var data = new byte[checked((int)stream.Length)];
        stream.ReadExactly(data);
        return data;
    }

    private static void ScanMorphSiblings(
        IXboxFileSystem fileSystem,
        string modelArchivePath,
        ICollection<MorphSiblingAsset> destination)
    {
        BigArchiveInspection archive;
        try
        {
            archive = BigArchiveReader.Inspect(fileSystem, modelArchivePath);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            destination.Add(new MorphSiblingAsset(
                modelArchivePath,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                exception.Message));
            return;
        }

        foreach (var nestedEntry in archive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                using var compressedStream = BigArchiveReader.OpenEntry(
                    fileSystem,
                    modelArchivePath,
                    nestedEntry.Name);
                var compressed = ReadBounded(compressedStream, 64 * 1024 * 1024);
                var expandedSize = RefPackDecoder.ReadDeclaredSize(compressed);
                if (expandedSize > 64 * 1024 * 1024)
                {
                    throw new InvalidDataException("Expanded EAGL archive exceeds the 64 MiB safety limit.");
                }

                using var compressedMemory = new MemoryStream(compressed, writable: false);
                var expanded = RefPackDecoder.Decode(compressedMemory, expandedSize);
                var evidence = EaglMorphSiblingReader.Inspect(expanded.Data, nestedEntry.Name);
                var posePaletteBankSymbols = evidence
                    .SelectMany(item => item.PosePaletteBankSymbols)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(symbol => symbol, StringComparer.Ordinal)
                    .ToArray();
                var posePaletteBankSymbolRecords = evidence
                    .SelectMany(item => item.PosePaletteBankSymbolRecords)
                    .Distinct()
                    .OrderBy(symbol => symbol.Name, StringComparer.Ordinal)
                    .ThenBy(symbol => symbol.SectionIndex)
                    .ThenBy(symbol => symbol.Value)
                    .ToArray();
                destination.Add(new MorphSiblingAsset(
                    nestedEntry.Name,
                    evidence.Count,
                    evidence.Count(item => item.NormalizedSiblingStem is not null && item.OrdSiblingName is not null),
                    evidence.Count(item => item.HasCompleteSibling),
                    evidence.Count(item => item.SiblingElfValidated),
                    evidence.Count(item => item.SiblingGeometrySymbolCount > 0),
                    evidence.Count(item => item.SiblingHasBounds),
                    evidence.Sum(item => item.PosePaletteBankSymbolCount),
                    null)
                {
                    PosePaletteBankSymbols = posePaletteBankSymbols,
                    PosePaletteBankSymbolRecords = posePaletteBankSymbolRecords,
                });
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
            {
                destination.Add(new MorphSiblingAsset(
                    nestedEntry.Name,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    exception.Message));
            }
        }
    }

    private static void ScanAnimationBanks(
        IXboxFileSystem fileSystem,
        ISet<string> dataFileSet,
        ICollection<AnimationBankAsset> destination)
    {
        const string archivePath = "data/anims.big";
        if (!dataFileSet.Contains(archivePath))
        {
            return;
        }

        BigArchiveInspection archive;
        try
        {
            archive = BigArchiveReader.Inspect(fileSystem, archivePath);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            destination.Add(new AnimationBankAsset(
                archivePath,
                string.Empty,
                0,
                0,
                false,
                null,
                null,
                0,
                0,
                false,
                false,
                string.Empty,
                exception.Message));
            return;
        }

        var ordByStem = archive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var orlByStem = archive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
            .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var catalogEntries = archive.Entries
            .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var catalogEntry in catalogEntries)
        {
            if (string.Equals(catalogEntry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            AnimationBankCatalog catalog;
            try
            {
                using var catalogStream = BigArchiveReader.OpenEntry(fileSystem, archivePath, catalogEntry.Name);
                catalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
            }
            catch (Exception exception) when (
                exception is InvalidDataException or EndOfStreamException or OverflowException)
            {
                destination.Add(new AnimationBankAsset(
                    catalogEntry.Name,
                    string.Empty,
                    0,
                    0,
                    false,
                    null,
                    null,
                    0,
                    0,
                    false,
                    false,
                    string.Empty,
                    exception.Message));
                continue;
            }

            ordByStem.TryGetValue(catalog.BankName, out var ordEntry);
            orlByStem.TryGetValue(catalog.BankName, out var orlEntry);
            var elfValidated = false;
            var symbolMatchesCatalog = false;
            var normalizedPayloadSignature = string.Empty;
            var animationBankSymbolRecords = Array.Empty<ElfSymbol>();
            string? issue = null;
            if (ordEntry is null || orlEntry is null)
            {
                issue = "same-bank .ord/.orl pair is incomplete";
            }
            else
            {
                try
                {
                    using var ordStream = BigArchiveReader.OpenEntry(fileSystem, archivePath, ordEntry.Name);
                    using var orlStream = BigArchiveReader.OpenEntry(fileSystem, archivePath, orlEntry.Name);
                    var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
                    var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
                    var combinedData = EaglObjectReader.Combine(ordData, orlData);
                    var elf = ElfReader.Inspect(combinedData);
                    elfValidated = true;
                    normalizedPayloadSignature =
                        AnimationBankReader.GetNormalizedDataPayloadSignature(combinedData, elf);
                    animationBankSymbolRecords = elf.Symbols
                        .Where(symbol => symbol.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal))
                        .ToArray();
                    var symbols = animationBankSymbolRecords
                        .Select(symbol => symbol.Name["__AnimationBank:::".Length..])
                        .Where(name => name.Length > 0)
                        .Distinct(StringComparer.Ordinal)
                        .ToArray();
                    symbolMatchesCatalog = symbols.Length == 1 &&
                        string.Equals(symbols[0], catalog.BankName, StringComparison.OrdinalIgnoreCase);
                    if (!symbolMatchesCatalog)
                    {
                        issue = "__AnimationBank symbol does not match the catalog name";
                    }
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or EndOfStreamException or OverflowException)
                {
                    issue = exception.Message;
                }
            }

            destination.Add(new AnimationBankAsset(
                catalogEntry.Name,
                catalog.BankName,
                catalog.DeclaredFileCount,
                catalog.Files.Count,
                catalog.DeclaredFileCountMatches,
                ordEntry?.Name,
                orlEntry?.Name,
                ordEntry?.Size ?? 0,
                orlEntry?.Size ?? 0,
                elfValidated,
                symbolMatchesCatalog,
                normalizedPayloadSignature,
                issue)
            {
                VersionTag = catalog.VersionTag,
                Directory = catalog.Directory,
                CatalogFiles = catalog.Files.ToArray(),
                AnimationBankSymbolRecords = animationBankSymbolRecords,
            });
        }
    }

    private static string? FindVivFCompanion(string packagePath, ISet<string> dataFiles)
    {
        var stem = Path.ChangeExtension(packagePath, null);
        var bigPath = stem + ".big";
        if (dataFiles.Contains(bigPath))
        {
            return dataFiles.First(path => string.Equals(path, bigPath, StringComparison.OrdinalIgnoreCase));
        }

        var vivPath = stem + ".viv";
        if (dataFiles.Contains(vivPath))
        {
            return dataFiles.First(path => string.Equals(path, vivPath, StringComparison.OrdinalIgnoreCase));
        }

        return null;
    }
}
