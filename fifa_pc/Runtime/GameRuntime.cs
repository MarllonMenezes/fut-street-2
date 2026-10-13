namespace FifaStreetPc;

public enum GamePhase
{
    Created,
    AssetsMounted,
    Running,
}

public interface IRenderer
{
    void Initialize();
    void BeginFrame();
    void EndFrame();
}

public interface IGameplayRenderer : IRenderer
{
    void Render(StreetMatch match);
}

public interface IAudioMixer
{
    void Initialize();
    void Update(double deltaSeconds);
}

public interface IGameplayAudioSink
{
    void OnGameplayEvent(string action);
}

public interface IInputSource
{
    MatchInput Poll();
}

public sealed class GameRuntime
{
    private readonly GameAssetCatalog catalog;
    private readonly IRenderer renderer;
    private readonly IAudioMixer audio;
    private readonly IInputSource input;
    private readonly StreetMatch match;
    private readonly List<CleanRoomInputSample> inputHistory = new();
    private readonly List<CleanRoomAuthoredAnimationSample> authoredAnimationHistory = new();
    private int lastAudioActionSequence;
    private int frames;
    private double elapsed;

    public GameRuntime(
        GameAssetCatalog catalog,
        IRenderer renderer,
        IAudioMixer audio,
        IInputSource input,
        bool enableOpponentAi = false,
        bool enableCleanRoomRoundReset = false,
        double matchDurationSeconds = 300.0,
        CleanRoomMatchEndCondition endCondition = CleanRoomMatchEndCondition.Timed,
        int targetGoals = 0,
        int targetPannas = 0)
    {
        this.catalog = catalog;
        this.renderer = renderer;
        this.audio = audio;
        this.input = input;
        match = new StreetMatch(
            enableOpponentAi,
            enableCleanRoomRoundReset,
            matchDurationSeconds,
            endCondition,
            targetGoals,
            targetPannas);
        Phase = GamePhase.Created;
    }

    public GamePhase Phase { get; private set; }
    public StreetMatch Match => match;
    public IAudioMixer Audio => audio;
    public bool OpponentAiEnabled => match.OpponentAiEnabled;
    public IReadOnlyList<CleanRoomInputSample> InputHistory => inputHistory;
    public IReadOnlyList<CleanRoomAuthoredAnimationSample> AuthoredAnimationHistory => authoredAnimationHistory;

    public void Start()
    {
        renderer.Initialize();
        audio.Initialize();
        // The initial KICKOFF event has sequence 0. Start below it so the
        // authored audio sink receives the complete event history, including
        // that initial event.
        lastAudioActionSequence = -1;
        Phase = GamePhase.AssetsMounted;
        Phase = GamePhase.Running;
    }

    public void Tick(double deltaSeconds)
    {
        if (Phase != GamePhase.Running)
        {
            throw new InvalidOperationException("GameRuntime must be started before Tick.");
        }
        var polledInput = input.Poll();
        inputHistory.Add(new CleanRoomInputSample(frames, polledInput));
        match.Tick(deltaSeconds, polledInput);
        if (audio is IGameplayAudioSink gameplayAudio)
        {
            foreach (var matchEvent in match.EventHistory.Where(matchEvent =>
                         matchEvent.Sequence > lastAudioActionSequence))
            {
                gameplayAudio.OnGameplayEvent(matchEvent.Action);
            }
            lastAudioActionSequence = match.ActionSequence;
        }
        audio.Update(deltaSeconds);
        authoredAnimationHistory.Add(new CleanRoomAuthoredAnimationSample(
            match.Frame,
            BallOwner.Home,
            match.LastAction,
            match.Possession == BallOwner.Home,
            CleanRoomPlayerAnimator.Evaluate(
                match.Frame,
                match.LastAction,
                BallOwner.Home,
                match.Possession == BallOwner.Home)));
        authoredAnimationHistory.Add(new CleanRoomAuthoredAnimationSample(
            match.Frame,
            BallOwner.Away,
            match.LastAction,
            match.Possession == BallOwner.Away,
            CleanRoomPlayerAnimator.Evaluate(
                match.Frame,
                match.LastAction,
                BallOwner.Away,
                match.Possession == BallOwner.Away)));
        renderer.BeginFrame();
        if (renderer is IGameplayRenderer gameplayRenderer)
        {
            gameplayRenderer.Render(match);
        }
        renderer.EndFrame();
        frames++;
        elapsed += deltaSeconds;
    }

    public string Report() => string.Join(Environment.NewLine,
        GameIdentity.RuntimeTitle,
        $"phase: {Phase}",
        $"data_files: {catalog.DataFiles.Count}",
        $"validated_big_archives: {catalog.Archives.Count}",
        $"validated_vivf_packages: {catalog.VivfPackages.Count}",
        $"bound_vivf_companions: {catalog.VivfPackages.Count(package => package.CompanionRangesValid && package.SentinelMatchesCompanion)}",
        $"audio_banks: {catalog.AudioBanks.Count}",
        $"audio_banks_structured: {catalog.AudioBanks.Count(bank => bank.StructureValidated)}",
        $"audio_abk_stream_references: {catalog.AudioBanks.Sum(bank => bank.AstDataSchlReferenceCount)}",
        $"audio_abk_stream_ranges: {catalog.AudioBanks.Sum(bank => bank.AstRanges.Count)}",
        $"audio_abk_banks_with_bnk: {catalog.AudioBanks.Count(bank => bank.BnkPresent)}",
        $"audio_abk_bnk_headers_validated: {catalog.AudioBanks.Sum(bank => bank.BnkValidHeaderCount)}",
        $"audio_abk_bnk_variable_headers_parsed: {catalog.AudioBanks.Sum(bank => bank.BnkParsedHeaderCount)}",
        $"audio_abk_xbox_ima_headers: {catalog.AudioBanks.Sum(bank => bank.BnkXboxImaHeaderCount)}",
        $"audio_abk_xbox_ima_data_validated: {catalog.AudioBanks.Sum(bank => bank.BnkXboxImaDataValidatedCount)}",
        $"audio_abk_xbox_ima_data_rejected: {catalog.AudioBanks.Sum(bank => bank.BnkXboxImaDataRejectedCount)}",
        $"animation_banks: {catalog.AnimationBanks.Count}",
        $"animation_banks_catalog_counts_validated: {catalog.AnimationBanks.Count(bank => bank.CatalogFileCountMatches)}",
        $"animation_banks_complete_pairs: {catalog.AnimationBanks.Count(bank => bank.OrdEntry is not null && bank.OrlEntry is not null)}",
        $"animation_banks_elf_validated: {catalog.AnimationBanks.Count(bank => bank.ElfValidated)}",
        $"animation_banks_symbol_matches: {catalog.AnimationBanks.Count(bank => bank.SymbolMatchesCatalog)}",
        $"animation_banks_catalog_file_entries: {catalog.AnimationBanks.Sum(bank => bank.CatalogFiles.Count)}",
        $"animation_banks_catalog_unique_file_names: {catalog.AnimationBanks
            .SelectMany(bank => bank.CatalogFiles)
            .Select(file => file.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()}",
        $"animation_banks_symbol_records: {catalog.AnimationBanks.Sum(bank => bank.AnimationBankSymbolRecords.Count)}",
        $"animation_banks_catalog_version_tags: {catalog.AnimationBanks
            .Select(bank => bank.VersionTag)
            .Where(version => version.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count()}",
        $"animation_banks_distinct_normalized_payloads: {catalog.AnimationBanks
            .Select(bank => bank.NormalizedDataPayloadSignature)
            .Where(signature => signature.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count()}",
        $"apt_packages: {catalog.AptPackages.Count}",
        $"apt_nested_archives: {catalog.AptPackages.Sum(package => package.NestedArchiveCount)}",
        $"apt_nested_entries: {catalog.AptPackages.Sum(package => package.NestedEntryCount)}",
        $"apt_valid_entries: {catalog.AptPackages.Sum(package => package.ValidAptEntryCount)}",
        $"apt_valid_constants: {catalog.AptPackages.Sum(package => package.ValidConstantEntryCount)}",
        $"apt_valid_objects: {catalog.AptPackages.Sum(package => package.ValidObjectEntryCount)}",
        $"apt_valid_shapes: {catalog.AptPackages.Sum(package => package.ValidShapeEntryCount)}",
        $"apt_complete_companion_sets: {catalog.AptPackages.Sum(package => package.CompleteCompanionSetCount)}",
        $"apt_structural_signatures: {catalog.AptPackages.Sum(package => package.StructuralSignatureCount)}",
        $"apt_morph_symbol_rows: {catalog.AptPackages.Sum(package => package.MorphSymbolCount)}",
        $"candidate_material_matches: {catalog.CandidateMaterials.Count}",
        $"candidate_material_model_archives: {catalog.CandidateMaterials
            .Select(material => material.ModelArchiveName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()}",
        $"candidate_material_objects: {catalog.CandidateMaterials
            .Select(material => $"{material.ModelArchiveName}/{material.ModelObjectName}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()}",
        $"candidate_material_shapes: {catalog.CandidateMaterials
            .Select(material => material.ShapeName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count()}",
        $"candidate_material_textures_decoded: {catalog.CandidateMaterials.Count(material => material.DecodedRgbaBytes > 0)}",
        $"candidate_material_pal8: {catalog.CandidateMaterials.Count(material => string.Equals(material.PixelFormat, "PAL8", StringComparison.OrdinalIgnoreCase))}",
        $"morph_sibling_nested_archives: {catalog.MorphSiblings.Count}",
        $"morph_sibling_nested_archives_validated: {catalog.MorphSiblings.Count(morph => morph.Issue is null)}",
        $"morph_sibling_evidence_objects: {catalog.MorphSiblings.Sum(morph => morph.MorphEvidenceCount)}",
        $"morph_sibling_normalized_candidates: {catalog.MorphSiblings.Sum(morph => morph.NormalizedSiblingCandidateCount)}",
        $"morph_sibling_complete_pairs: {catalog.MorphSiblings.Sum(morph => morph.CompleteSiblingCount)}",
        $"morph_sibling_elf_validated: {catalog.MorphSiblings.Sum(morph => morph.SiblingElfValidatedCount)}",
        $"morph_sibling_geometry_candidates: {catalog.MorphSiblings.Sum(morph => morph.SiblingGeometryCount)}",
        $"morph_sibling_bounds_candidates: {catalog.MorphSiblings.Sum(morph => morph.SiblingBoundsCount)}",
        $"morph_pose_palette_bank_symbols: {catalog.MorphSiblings.Sum(morph => morph.PosePaletteBankSymbolCount)}",
        $"morph_pose_palette_bank_symbol_names: {catalog.MorphSiblings
        .SelectMany(morph => morph.PosePaletteBankSymbols)
            .Distinct(StringComparer.Ordinal)
            .Count()}",
        $"morph_pose_palette_bank_symbol_records: {catalog.MorphSiblings.Sum(morph => morph.PosePaletteBankSymbolRecords.Count)}",
        $"audio_streams: {catalog.AudioStreams.Count}",
        $"audio_stream_headers_validated: {catalog.AudioStreams.Count(stream => stream.HeaderValidated)}",
        $"audio_clips_prepared: {(audio as CatalogAudioMixer)?.PreparedStreamCount ?? 0}",
        $"audio_pcm_bytes_prepared: {(audio as CatalogAudioMixer)?.PreparedPcmBytes ?? 0}",
        $"audio_abk_clips_prepared: {(audio as CatalogAudioMixer)?.PreparedBankStreamCount ?? 0}",
        $"audio_abk_pcm_bytes_prepared: {(audio as CatalogAudioMixer)?.PreparedBankPcmBytes ?? 0}",
        $"audio_abk_candidate_banks: {(audio as CatalogAudioMixer)?.ObservedBankCandidateResults.Count ?? 0}",
        $"audio_abk_candidate_banks_decoded: {(audio as CatalogAudioMixer)?.ObservedBankCandidateResults.Count(row => row.DecodeValidated) ?? 0}",
        $"audio_abk_candidate_banks_rejected: {(audio as CatalogAudioMixer)?.ObservedBankCandidateResults.Count(row => !row.DecodeValidated) ?? 0}",
        $"audio_abk_candidate_sources: {FormatAudioBankCandidateSources(audio as CatalogAudioMixer)}",
        $"gameplay_audio_events: {(audio as CatalogAudioMixer)?.GameplayEventCount ?? 0}",
        $"last_gameplay_audio_event: {(audio as CatalogAudioMixer)?.LastGameplayEvent ?? "none"}",
        $"audio_named_event_candidates: {catalog.AudioEventCandidates.Count}",
        $"audio_named_event_candidates_validated: {catalog.AudioEventCandidates.Count(candidate => candidate.HeaderValidated)}",
        $"audio_named_event_candidates_with_valid_channels: {catalog.AudioEventCandidates.Count(candidate => candidate.Info is { Channels: > 0 })}",
        $"audio_named_event_candidates_with_payload_channel_layout: {catalog.AudioEventCandidates.Count(candidate => candidate.PayloadChannelLayoutCandidate is not null)}",
        $"audio_named_event_candidate_waves_prepared: {(audio as CatalogAudioMixer)?.PreparedNamedEventCandidateCount ?? 0}",
        $"audio_named_event_candidate_pcm_bytes_prepared: {(audio as CatalogAudioMixer)?.PreparedNamedEventCandidatePcmBytes ?? 0}",
        $"audio_named_event_candidate_decode_rejected: {(audio as CatalogAudioMixer)?.NamedEventCandidateDecodeRejectedCount ?? 0}",
        $"gameplay_audio_prepared: {(audio as CatalogAudioMixer)?.PreparedGameplayAudioCount ?? 0}",
        $"gameplay_audio_played: {(audio as CatalogAudioMixer)?.PlayedGameplayAudioCount ?? 0}",
        $"gameplay_audio_pcm_bytes: {(audio as CatalogAudioMixer)?.GameplayAudioPcmBytes ?? 0}",
        $"gameplay_audio_pcm_sha256: {(audio as CatalogAudioMixer)?.GameplayAudioPcmSha256 ?? "none"}",
        $"gameplay_audio_decode_rejected: {(audio as CatalogAudioMixer)?.GameplayAudioDecodeRejectedCount ?? 0}",
        $"gameplay_audio_unmapped_events: {(audio as CatalogAudioMixer)?.GameplayAudioUnmappedEventCount ?? 0}",
        $"last_gameplay_audio_name: {(audio as CatalogAudioMixer)?.LastGameplayAudioName ?? "none"}",
        $"last_gameplay_audio_route: {(audio as CatalogAudioMixer)?.LastGameplayAudioRoute ?? "none"}",
        $"texture_images: {catalog.TextureImages.Count}",
        $"texture_headers_validated: {catalog.TextureImages.Count(texture => texture.HeaderValidated)}",
        $"texture_decodes_validated: {catalog.TextureImages.Count(texture => texture.DecodeValidated)}",
        $"rendered_frames: {(renderer as SoftwareGameplayRenderer)?.RenderedFrameCount ?? 0}",
        $"render_frame_checksum: {(renderer as SoftwareGameplayRenderer)?.LastFrameChecksum.ToString("X16") ?? "none"}",
        $"frames: {frames}",
        $"elapsed_seconds: {elapsed:0.000}",
         $"match: {match.HudText()}",
         $"match_duration_seconds: {match.MatchDurationSeconds:0.000}",
         $"match_end_condition: {FormatMatchEndCondition(match.EndCondition)}",
         $"match_target_goals: {match.TargetGoals}",
         $"match_target_pannas: {match.TargetPannas}",
         $"match_ended: {match.IsEnded}",
        $"match_result: {FormatMatchResult(match.Result)}",
        $"opponent_ai: {(match.OpponentAiEnabled ? "enabled" : "disabled")}",
        $"match_frame: {match.Frame}",
        $"match_action_sequence: {match.ActionSequence}",
        $"match_last_action: {match.LastAction}",
        $"match_action_counts: {string.Join(",", match.ActionCounts.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))}",
        $"match_possession: {match.Possession}",
         $"match_tricks: {match.TrickCount}",
         $"match_pannas: {match.PannaCount}",
         $"match_gamebreakers: {match.GamebreakerCount}",
         $"match_gamebreaker_meter: {match.GamebreakerMeter}/{StreetMatch.MaximumGamebreakerMeter}",
         $"match_juggles: {match.JuggleCount}",
         $"match_juggle_chain: {match.JuggleChain}",
         $"match_tackles: {match.TackleCount}",
         $"match_ball_height: {match.BallHeight:0.000}",
         $"match_round: {match.RoundNumber}",
        $"match_restart_pending: {match.RestartPending}",
        $"clean_room_round_reset: {(match.CleanRoomRoundResetEnabled ? "enabled" : "disabled")}",
        $"match_home_position: {FormatPosition(match.HomePosition)}",
        $"match_away_position: {FormatPosition(match.AwayPosition)}",
        $"match_ball_position: {FormatPosition(match.BallPosition)}",
        "source: original ISO, read-only",
        "execution: new managed PC code");

    private static string FormatPosition((float X, float Y) position) =>
        FormattableString.Invariant($"({position.X:0.000},{position.Y:0.000})");

    private static string FormatMatchResult(CleanRoomMatchResult result) => result switch
    {
        CleanRoomMatchResult.InProgress => "IN_PROGRESS",
        CleanRoomMatchResult.Draw => "DRAW",
        CleanRoomMatchResult.HomeWin => "HOME_WIN",
        CleanRoomMatchResult.AwayWin => "AWAY_WIN",
        _ => "UNKNOWN",
    };

    private static string FormatMatchEndCondition(CleanRoomMatchEndCondition condition) => condition switch
    {
        CleanRoomMatchEndCondition.Timed => "TIMED",
        CleanRoomMatchEndCondition.FirstToGoals => "FIRST_TO_GOALS",
        CleanRoomMatchEndCondition.GoalsOrPanna => "GOALS_OR_PANNA",
        _ => "UNKNOWN",
    };

    private static string FormatAudioBankCandidateSources(CatalogAudioMixer? mixer)
    {
        if (mixer is null || mixer.ObservedBankCandidateResults.Count == 0)
        {
            return "none";
        }

        return string.Join(
            ",",
            mixer.ObservedBankCandidateResults
                .GroupBy(row => row.SourceKind, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"{group.Key}={group.Count()}"));
    }
}
