using System.Security.Cryptography;

namespace FifaStreetPc;

public sealed class HeadlessRenderer : IRenderer
{
    public void Initialize() { }
    public void BeginFrame() { }
    public void EndFrame() { }
}

public sealed class SoftwareGameplayRenderer : IGameplayRenderer
{
    private readonly PcTextureResource? titleTexture;
    private readonly EaglCandidateMesh? candidateMesh;
    private readonly PcShapeImageTexture? candidateTexture;
    private readonly bool cleanRoomCharacters;
    private readonly HashSet<ulong> frameChecksums = new();

    public SoftwareGameplayRenderer(
        int width = 960,
        int height = 540,
        PcTextureResource? titleTexture = null,
        EaglCandidateMesh? candidateMesh = null,
        PcShapeImageTexture? candidateTexture = null,
        bool cleanRoomCharacters = false,
        bool candidateHomeCharacter = false,
        bool candidateAwayCharacter = false)
    {
        Frame = new SoftwareFrameBuffer(width, height);
        this.titleTexture = titleTexture;
        this.candidateMesh = candidateMesh;
        this.candidateTexture = candidateTexture;
        this.cleanRoomCharacters = cleanRoomCharacters;
        CandidateHomeCharacter = candidateHomeCharacter;
        CandidateAwayCharacter = candidateAwayCharacter;
    }

    public SoftwareFrameBuffer Frame { get; }
    public int RenderedFrameCount { get; private set; }
    public ulong LastFrameChecksum { get; private set; }
    public int AuthoredCharacterAssetFrames { get; private set; }
    public int AuthoredSceneAssetFrames { get; private set; }
    public bool CandidateHomeCharacter { get; }
    public bool CandidateAwayCharacter { get; }

    public int DistinctFrameChecksumCount => frameChecksums.Count;

    public void Initialize()
    {
        frameChecksums.Clear();
        RenderedFrameCount = 0;
        LastFrameChecksum = 0;
        AuthoredCharacterAssetFrames = 0;
        AuthoredSceneAssetFrames = 0;
    }
    public void BeginFrame() { }

    public void Render(StreetMatch match)
    {
        PreviewRenderer.Render(
            match,
            Frame,
            titleTexture,
            candidateMesh,
            candidateTexture,
            cleanRoomCharacters: cleanRoomCharacters,
            candidateHomeCharacter: CandidateHomeCharacter,
            candidateAwayCharacter: CandidateAwayCharacter);
        if (cleanRoomCharacters &&
            (candidateMesh is null || (!CandidateHomeCharacter || !CandidateAwayCharacter)))
        {
            AuthoredCharacterAssetFrames++;
        }
        if (CleanRoomSceneAssetManifest.Default.IsComplete)
        {
            AuthoredSceneAssetFrames++;
        }
        LastFrameChecksum = Frame.ComputeChecksum();
        frameChecksums.Add(LastFrameChecksum);
        RenderedFrameCount++;
    }

    public void EndFrame() { }
}

public sealed class CatalogAudioMixer : IAudioMixer, IGameplayAudioSink, IDisposable
{
    private readonly GameAssetCatalog catalog;
    private readonly Func<string, Stream>? audioOpener;
    private readonly bool enablePlayback;
    private readonly bool prepareObservedBankCandidates;
    private readonly Dictionary<string, byte[]> preparedWaves = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> preparedBankWaves = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, byte[]> preparedEventWaves = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<CleanRoomGameplayAudioRoute> gameplayAudioRoutes = new();
    private readonly MemoryStream gameplayAudioPcm = new();
    private IDisposable? activeGameplayPlayback;

    public CatalogAudioMixer(
        GameAssetCatalog catalog,
        Func<string, Stream>? audioOpener = null,
        bool enablePlayback = false,
        bool prepareObservedBankCandidates = false)
    {
        this.catalog = catalog;
        this.audioOpener = audioOpener;
        this.enablePlayback = enablePlayback;
        this.prepareObservedBankCandidates = prepareObservedBankCandidates;
    }

    public int MountedStreamCount { get; private set; }
    public int RejectedStreamCount { get; private set; }
    public int PreparedStreamCount => preparedWaves.Count;
    public long PreparedPcmBytes { get; private set; }
    public int PreparedBankStreamCount => preparedBankWaves.Count;
    public long PreparedBankPcmBytes { get; private set; }
    public int PreparedNamedEventCandidateCount => preparedEventWaves.Count;
    public long PreparedNamedEventCandidatePcmBytes { get; private set; }
    public int NamedEventCandidateDecodeRejectedCount { get; private set; }
    public IReadOnlyList<AudioBankBatchDecodeResult> ObservedBankCandidateResults { get; private set; } =
        Array.Empty<AudioBankBatchDecodeResult>();
    public int DecodeRejectedStreamCount { get; private set; }
    public int BankDecodeRejectedStreamCount { get; private set; }
    public int GameplayEventCount { get; private set; }
    public string? LastGameplayEvent { get; private set; }
    public int PreparedGameplayAudioCount { get; private set; }
    public int PlayedGameplayAudioCount { get; private set; }
    public long GameplayAudioPcmBytes => gameplayAudioPcm.Length;
    public string GameplayAudioPcmSha256 =>
        Convert.ToHexString(SHA256.HashData(gameplayAudioPcm.ToArray()));
    public string AuthoredAudioAssetManifest =>
        CleanRoomAudioAssetManifest.Default.ManifestId;
    public int GameplayAudioDecodeRejectedCount { get; private set; }
    public int GameplayAudioUnmappedEventCount { get; private set; }
    public string? LastGameplayAudioName { get; private set; }
    public string? LastGameplayAudioRoute { get; private set; }
    public IReadOnlyList<CleanRoomGameplayAudioRoute> GameplayAudioRoutes =>
        gameplayAudioRoutes.AsReadOnly();

    public void Dispose()
    {
        activeGameplayPlayback?.Dispose();
        activeGameplayPlayback = null;
    }

    public void Initialize()
    {
        MountedStreamCount = catalog.AudioStreams.Count(stream => stream.HeaderValidated);
        RejectedStreamCount = catalog.AudioStreams.Count(stream => !stream.HeaderValidated);
        preparedWaves.Clear();
        preparedBankWaves.Clear();
        preparedEventWaves.Clear();
        ObservedBankCandidateResults = Array.Empty<AudioBankBatchDecodeResult>();
        activeGameplayPlayback?.Dispose();
        activeGameplayPlayback = null;
        PreparedGameplayAudioCount = 0;
        PlayedGameplayAudioCount = 0;
        GameplayAudioDecodeRejectedCount = 0;
        GameplayAudioUnmappedEventCount = 0;
        LastGameplayAudioName = null;
        LastGameplayAudioRoute = null;
        gameplayAudioRoutes.Clear();
        gameplayAudioPcm.SetLength(0);
        PreparedPcmBytes = 0;
        PreparedBankPcmBytes = 0;
        PreparedNamedEventCandidatePcmBytes = 0;
        NamedEventCandidateDecodeRejectedCount = 0;
        DecodeRejectedStreamCount = 0;
        BankDecodeRejectedStreamCount = 0;
        GameplayAudioDecodeRejectedCount = 0;
        if (audioOpener is null)
        {
            return;
        }

        foreach (var asset in catalog.AudioStreams.Where(stream => stream.HeaderValidated))
        {
            try
            {
                using var stream = audioOpener(asset.Path);
                var wave = EaSchlAudioDecoder.DecodeWave(stream);
                preparedWaves.Add(asset.Path, wave);
                PreparedPcmBytes += wave.Length - 44L;
            }
            catch (Exception exception) when (
                exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
            {
                DecodeRejectedStreamCount++;
            }
        }

        foreach (var candidate in catalog.AudioEventCandidates.Where(item =>
                     (item.Info is { Channels: > 0 } || item.PayloadChannelLayoutCandidate is not null) &&
                     item.Candidate.ParallelDataRow is { SecondColumn: >= 8 }))
        {
            TryPrepareNamedEventCandidate(candidate, out _, countGameplayRejection: false);
        }

        if (prepareObservedBankCandidates)
        {
            ObservedBankCandidateResults = AudioBankBatchDecoder.DecodeFirstAvailable(
                catalog,
                this);
        }
    }

    public void Update(double deltaSeconds) { }

    public void OnGameplayEvent(string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        GameplayEventCount++;
        var sequence = GameplayEventCount;
        LastGameplayEvent = action;
        if (!CleanRoomAudioCuePolicy.TryResolve(action, out var eventName))
        {
            GameplayAudioUnmappedEventCount++;
            LastGameplayAudioRoute = "unmapped-clean-room-event";
            gameplayAudioRoutes.Add(new CleanRoomGameplayAudioRoute(
                sequence,
                action,
                string.Empty,
                LastGameplayAudioRoute));
            return;
        }

        if (!CleanRoomAudioAssetManifest.Default.TryResolveCandidate(eventName, out _))
        {
            GameplayAudioUnmappedEventCount++;
            LastGameplayAudioRoute = "unmapped-clean-room-asset";
            gameplayAudioRoutes.Add(new CleanRoomGameplayAudioRoute(
                sequence,
                action,
                eventName,
                LastGameplayAudioRoute));
            return;
        }

        var rejectedBefore = GameplayAudioDecodeRejectedCount;
        if (!TryPrepareNamedEvent(eventName, out var wave))
        {
            LastGameplayAudioRoute = GameplayAudioDecodeRejectedCount > rejectedBefore
                ? "candidate-decode-rejected"
                : "candidate-unavailable";
            gameplayAudioRoutes.Add(new CleanRoomGameplayAudioRoute(
                sequence,
                action,
                eventName,
                LastGameplayAudioRoute));
            return;
        }

        PreparedGameplayAudioCount++;
        AppendGameplayAudioPcm(wave);
        LastGameplayAudioName = eventName;
        if (enablePlayback)
        {
            activeGameplayPlayback?.Dispose();
            activeGameplayPlayback = Win32WavePlayer.PlayAsynchronously(wave);
            PlayedGameplayAudioCount++;
            LastGameplayAudioRoute = "played-clean-room-candidate";
        }
        else
        {
            LastGameplayAudioRoute = "prepared-clean-room-candidate";
        }
        gameplayAudioRoutes.Add(new CleanRoomGameplayAudioRoute(
            sequence,
            action,
            eventName,
            LastGameplayAudioRoute,
            wave.Length,
            Math.Max(0L, wave.Length - 44L),
            Convert.ToHexString(SHA256.HashData(wave.AsSpan(44)))));
    }

    public bool TryGetPreparedWave(string streamPath, out byte[] wave) =>
        preparedWaves.TryGetValue(streamPath, out wave!);

    public bool TryPrepareNamedEvent(string eventName, out byte[] wave)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        wave = Array.Empty<byte>();
        if (audioOpener is null)
        {
            return false;
        }

        var candidate = catalog.AudioEventCandidates.FirstOrDefault(item =>
            string.Equals(item.Candidate.EventName, eventName, StringComparison.OrdinalIgnoreCase) &&
            (item.Info is { Channels: > 0 } || item.PayloadChannelLayoutCandidate is not null));
        if (candidate is null || candidate.Candidate.ParallelDataRow is not { } dataRow ||
            dataRow.SecondColumn < 8)
        {
            return false;
        }

        var key = $"{candidate.EventPath}#name={candidate.Candidate.EventName}#row={candidate.Candidate.HeaderRowIndex}";
        if (preparedEventWaves.TryGetValue(key, out wave!))
        {
            return true;
        }

        return TryPrepareNamedEventCandidate(candidate, out wave, countGameplayRejection: true);
    }

    private bool TryPrepareNamedEventCandidate(
        AudioEventCandidateAsset candidate,
        out byte[] wave,
        bool countGameplayRejection)
    {
        wave = Array.Empty<byte>();
        if (audioOpener is null || candidate.Candidate.ParallelDataRow is not { } dataRow ||
            dataRow.SecondColumn < 8)
        {
            return false;
        }

        var key = GetNamedEventCandidateKey(candidate);
        if (preparedEventWaves.TryGetValue(key, out wave!))
        {
            return true;
        }

        try
        {
            using var dataStream = audioOpener(candidate.DataPath);
            using var range = new AudioRangeStream(dataStream, dataRow.FirstColumn, dataRow.SecondColumn);
            wave = candidate.Info is { Channels: > 0 }
                ? EaSchlAudioDecoder.DecodeWave(range)
                : EaSchlAudioDecoder.DecodeWaveWithChannelLayoutCandidate(
                    range,
                    candidate.PayloadChannelLayoutCandidate!.Value);
            preparedEventWaves.Add(key, wave);
            PreparedNamedEventCandidatePcmBytes += Math.Max(0, wave.Length - 44L);
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            NamedEventCandidateDecodeRejectedCount++;
            if (countGameplayRejection)
            {
                GameplayAudioDecodeRejectedCount++;
            }

            return false;
        }
    }

    private static string GetNamedEventCandidateKey(AudioEventCandidateAsset candidate) =>
        $"{candidate.EventPath}#name={candidate.Candidate.EventName}#row={candidate.Candidate.HeaderRowIndex}";

    private void AppendGameplayAudioPcm(byte[] wave)
    {
        if (wave.Length <= 44)
        {
            return;
        }

        gameplayAudioPcm.Write(wave, 44, wave.Length - 44);
    }

    public bool TryPrepareBankSound(
        AudioBankAsset bank,
        int soundIndex,
        bool loopSegment,
        out byte[] wave)
    {
        return TryPrepareBankSound(
            bank,
            soundIndex,
            loopSegment,
            out wave,
            out _);
    }

    public bool TryPrepareBankSound(
        AudioBankAsset bank,
        int soundIndex,
        bool loopSegment,
        out byte[] wave,
        out int? channelLayoutCandidate)
    {
        ArgumentNullException.ThrowIfNull(bank);
        wave = Array.Empty<byte>();
        channelLayoutCandidate = null;
        if (audioOpener is null || !bank.StructureValidated)
        {
            return false;
        }

        if (bank.AstPath is null)
        {
            return !loopSegment && TryPrepareBankRamSound(bank, soundIndex, out wave);
        }

        var range = bank.AstRanges.FirstOrDefault(candidate =>
            candidate.SoundIndex == soundIndex && candidate.LoopSegment == loopSegment);
        if (range is null || range.Length < 8)
        {
            return false;
        }

        var key = $"{bank.Path}#sound={soundIndex}#loop={loopSegment}";
        if (preparedBankWaves.TryGetValue(key, out wave!))
        {
            return true;
        }

        using var astStream = audioOpener(bank.AstPath);
        try
        {
            using var astRange = new AudioRangeStream(astStream, range.Offset, range.Length);
            wave = EaSchlAudioDecoder.DecodeWave(astRange);
            preparedBankWaves.Add(key, wave);
            PreparedBankPcmBytes += wave.Length - 44L;
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            if (TryPrepareAstChannelLayoutCandidate(
                    astStream,
                    range,
                    out wave,
                    out var candidateChannels))
            {
                channelLayoutCandidate = candidateChannels;
                preparedBankWaves.Add(key, wave);
                PreparedBankPcmBytes += wave.Length - 44L;
                return true;
            }

            BankDecodeRejectedStreamCount++;
            return false;
        }
    }

    private static bool TryPrepareAstChannelLayoutCandidate(
        Stream astStream,
        AudioAbkAstRange range,
        out byte[] wave,
        out int? candidateChannels)
    {
        wave = Array.Empty<byte>();
        candidateChannels = null;
        try
        {
            using (var observedRange = new AudioRangeStream(astStream, range.Offset, range.Length))
            {
                var observedInfo = EaSchlAudioDecoder.Inspect(observedRange);
                if (observedInfo.Channels != 0)
                {
                    return false;
                }
            }

            var validCandidates = new List<int>();
            foreach (var channels in new[] { 1, 2 })
            {
                using var candidateRange = new AudioRangeStream(astStream, range.Offset, range.Length);
                if (EaSchlAudioDecoder.TryValidateChannelLayoutCandidate(
                        candidateRange,
                        channels,
                        out _,
                        out _))
                {
                    validCandidates.Add(channels);
                }
            }

            if (validCandidates.Count != 1)
            {
                return false;
            }

            var selectedChannels = validCandidates[0];
            using var decodeRange = new AudioRangeStream(astStream, range.Offset, range.Length);
            wave = EaSchlAudioDecoder.DecodeWaveWithChannelLayoutCandidate(
                decodeRange,
                selectedChannels);
            candidateChannels = selectedChannels;
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            wave = Array.Empty<byte>();
            candidateChannels = null;
            return false;
        }
    }

    public bool TryPrepareBankRamSound(
        AudioBankAsset bank,
        int soundIndex,
        out byte[] wave)
    {
        ArgumentNullException.ThrowIfNull(bank);
        wave = Array.Empty<byte>();
        if (audioOpener is null || !bank.StructureValidated || !bank.BnkPresent ||
            !bank.BnkStructureValidated)
        {
            return false;
        }

        var key = $"{bank.Path}#sound={soundIndex}#ram";
        if (preparedBankWaves.TryGetValue(key, out wave!))
        {
            return true;
        }

        try
        {
            using var bankStream = audioOpener(bank.Path);
            if (bankStream.Length > 64L * 1024 * 1024 || bankStream.Length > int.MaxValue)
            {
                throw new InvalidDataException("ABKC bank exceeds the managed RAM-sound safety limit.");
            }

            var bankData = new byte[checked((int)bankStream.Length)];
            bankStream.ReadExactly(bankData);
            var inspection = AudioAbkReader.Inspect(bankData);
            var sound = inspection.Sounds.FirstOrDefault(candidate =>
                candidate.SoundIndex == soundIndex && !candidate.IsDummy && candidate.SoundType == 0);
            if (sound is null || sound.DataOffset > int.MaxValue)
            {
                return false;
            }

            var bnk = AudioAbkReader.InspectEmbeddedBnk(inspection, bankData);
            if (bnk is null || sound.DataOffset >= (uint)bnk.Entries.Count)
            {
                return false;
            }

            var bnkEntry = bnk.Entries[checked((int)sound.DataOffset)];
            if (bnkEntry.IsDummy || !bnkEntry.HeaderWithinAsset)
            {
                return false;
            }

            var header = AudioAbkReader.InspectEmbeddedBnkHeaders(bnk, bankData)
                .FirstOrDefault(candidate => candidate.EntryIndex == bnkEntry.EntryIndex);
            if (header is null || !AudioAbkReader.TryGetBnkStreamDescriptor(header, out var descriptor) ||
                descriptor is null || descriptor.Codec != 0x14)
            {
                return false;
            }

            var absoluteOffsets = descriptor.RelativeChannelOffsets
                .Select(offset => checked(bnk.Offset + offset))
                .ToArray();
            wave = XboxImaAudioDecoder.DecodeWave(
                bankData,
                descriptor.Channels,
                descriptor.SampleRate,
                descriptor.SampleFrames,
                absoluteOffsets);
            preparedBankWaves.Add(key, wave);
            PreparedBankPcmBytes += wave.Length - 44L;
            return true;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            BankDecodeRejectedStreamCount++;
            return false;
        }
    }
}

public sealed class HeadlessInputSource : IInputSource
{
    public MatchInput Poll() => new();
}

/// <summary>
/// Deterministic integration script for the managed headless runtime. It
/// exercises possession, trick, pass completion, recovery and a goal without
/// assigning any original-game event or audio meaning. The optional aimed-shot
/// mode is a clean-room control scenario and changes only the shot's MoveY. The
/// optional gamebreaker and juggle modes are separate authored action scenarios.
/// </summary>
public sealed class GameplayDemoInputSource : IInputSource
{
    private readonly bool aimedShot;
    private readonly bool gamebreaker;
    private readonly bool juggle;
    private int frame;

    public GameplayDemoInputSource(
        bool aimedShot = false,
        bool gamebreaker = false,
        bool juggle = false)
    {
        this.aimedShot = aimedShot;
        this.gamebreaker = gamebreaker;
        this.juggle = juggle;
    }

    public MatchInput Poll()
        {
            var current = frame++;
        if (gamebreaker)
        {
            return current switch
            {
                <= 20 => new MatchInput(1, 0, false, false, false),
                21 or 23 or 25 or 27 => new MatchInput(0, 0, false, false, true),
                29 => new MatchInput(0, 0, false, false, false, false, true),
                _ => new MatchInput(),
            };
        }

        if (juggle)
        {
            return current switch
            {
                <= 20 => new MatchInput(1, 0, false, false, false),
                21 or 23 or 25 or 27 => new MatchInput(0, 0, false, false, false, false, false, true),
                45 => new MatchInput(0, 0, false, true, false),
                _ => new MatchInput(),
            };
        }

        return current switch
        {
            <= 20 => new MatchInput(1, 0, false, false, false),
            21 => new MatchInput(0, 0, false, false, true),
            23 => new MatchInput(0, 0, true, false, false),
            >= 30 and <= 66 => new MatchInput(1, 0, false, false, false),
            67 => aimedShot
                ? new MatchInput(0, 0.75f, false, true, false)
                : new MatchInput(0, 0, false, true, false),
            _ => new MatchInput(),
        };
    }
}

/// <summary>
/// Deterministic authored defensive-control scenario. The home player keeps
/// moving into the opponent press and emits one explicit TACKLE edge after
/// the opponent has taken possession. This is PC-owned clean-room behavior;
/// it is not a decoded original control or collision rule.
/// </summary>
public sealed class TackleDemoInputSource : IInputSource
{
    private int frame;

    public MatchInput Poll()
    {
        var current = frame++;
        return current switch
        {
            <= 40 => new MatchInput(1, 0, false, false, false),
            41 => new MatchInput(0, 0, false, false, false, false, false, false, true),
            _ => new MatchInput(),
        };
    }
}

/// <summary>
/// Deterministic authored control scenario that lets the opponent press the
/// home possessor until the PC-only STEAL transition occurs. It is a runtime
/// coverage fixture, not an extraction of original AI or possession rules.
/// </summary>
public sealed class StealDemoInputSource : IInputSource
{
    private int frame;

    public MatchInput Poll()
    {
        var current = frame++;
        return current <= 20
            ? new MatchInput(1, 0, false, false, false)
            : new MatchInput();
    }
}
