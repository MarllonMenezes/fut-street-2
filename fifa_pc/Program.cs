using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FifaPcCompat.Runtime;
using FifaStreetPc;

if (args.Length == 1 && string.Equals(args[0], "self-test", StringComparison.OrdinalIgnoreCase))
{
    return SelfTestRunner.Run();
}

if (args.Length > 0 && string.Equals(args[0], "project-readiness-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc project-readiness-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    var readinessIsoPath = Path.GetFullPath(args[1]);
    var readinessReportPath = Path.Combine(
        Directory.GetParent(readinessIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "project-readiness.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            readinessReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    using var readinessFileSystem = new XdvdfsFileSystem(readinessIsoPath);
    var readinessCatalog = GameAssetCatalog.Scan(readinessFileSystem);
    var readinessSummary = ProjectReadinessAudit.Build(
        readinessCatalog,
        readinessIsoPath,
        Directory.GetParent(readinessIsoPath)?.FullName ?? Directory.GetCurrentDirectory());
    ProjectReadinessAudit.WriteReport(readinessReportPath, readinessSummary);

    Console.WriteLine($"{GameIdentity.Name} PC clean-room project readiness audit");
    Console.WriteLine($"readiness: {(readinessSummary.Ready ? "READY" : "NOT_READY")}");
    Console.WriteLine($"iso_length: {readinessSummary.IsoLength}");
    Console.WriteLine($"iso_sha256: {readinessSummary.IsoSha256}");
    Console.WriteLine($"iso_attributes: {readinessSummary.IsoAttributes}");
    foreach (var row in readinessSummary.Rows)
    {
        Console.WriteLine($"gate_{row.Area}: status={row.Status} | passed={row.GatePassed} | observed={row.Observed} | expected={row.Expected}");
        Console.WriteLine($"  evidence: {row.Evidence}");
        Console.WriteLine($"  next_required_evidence: {row.NextRequiredEvidence}");
        Console.WriteLine($"  notes: {row.Notes}");
    }
    Console.WriteLine($"report: {Path.GetFullPath(readinessReportPath)}");
    Console.WriteLine("status: audit only; candidate and structural evidence remain explicitly separate from original semantics");
    Console.WriteLine("mode: ISO read-only, managed catalog scan, no XBE execution");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "morph-ambiguity-summary", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: morph-ambiguity-summary <ambiguity-report.tsv> <summary.tsv>");
        return 2;
    }

    var ambiguityInputPath = Path.GetFullPath(args[1]);
    var ambiguitySummaryPath = Path.GetFullPath(args[2]);
    if (string.Equals(ambiguityInputPath, ambiguitySummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O resumo de ambiguidade não pode sobrescrever o relatório de entrada.");
        return 2;
    }

    var ambiguityRows = MorphAmbiguitySummarizer.ReadReport(ambiguityInputPath);
    var ambiguitySummaries = MorphAmbiguitySummarizer.Summarize(ambiguityRows);
    var ambiguitySummaryLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "variation_fields",
            "ambiguous_layouts",
            "ambiguous_rows",
            "stable_control_overlap_layout_counts",
            "layout_key_sha256s",
        }),
    };
    ambiguitySummaryLines.AddRange(ambiguitySummaries.Select(summary => string.Join("\t", new[]
    {
        CatalogField(summary.VariationFields),
        summary.AmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
        summary.AmbiguousRows.ToString(CultureInfo.InvariantCulture),
        CatalogField(summary.StableControlOverlapLayoutCounts),
        CatalogField(summary.LayoutKeySha256s),
    })));

    var ambiguitySummaryDirectory = Path.GetDirectoryName(ambiguitySummaryPath);
    if (!string.IsNullOrEmpty(ambiguitySummaryDirectory))
    {
        Directory.CreateDirectory(ambiguitySummaryDirectory);
    }

    File.WriteAllLines(
        ambiguitySummaryPath,
        ambiguitySummaryLines,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"morph_ambiguity_summary: {ambiguitySummaryPath} | input_rows={ambiguityRows.Count} | ambiguous_layouts={ambiguityRows.Count(row => row.LayoutAmbiguous)} | variation_compositions={ambiguitySummaries.Count}");
    Console.WriteLine("status: observed ambiguity-layer aggregation only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "gameplay-audio-coverage-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc gameplay-audio-coverage-audit <workspace> [--report <saida.tsv>]");
        return 2;
    }

    var coverageWorkspace = Path.GetFullPath(args[1]);
    var coverageReportPath = Path.Combine(
        coverageWorkspace,
        "fifa_pc",
        "gameplay-audio-coverage.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            coverageReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    var coverageAudit = GameplayAudioCoverageAudit.Audit(
        GameplayAudioCoverageAudit.DefaultScenarios(coverageWorkspace));
    GameplayAudioCoverageAudit.Write(coverageReportPath, coverageAudit);
    Console.WriteLine($"{GameIdentity.Name} PC authored gameplay-audio coverage audit");
    Console.WriteLine($"scenarios: {coverageAudit.Summary.ScenarioCount}");
    Console.WriteLine($"verified_scenarios: {coverageAudit.Summary.VerifiedScenarioCount}");
    Console.WriteLine($"events: {coverageAudit.Summary.TotalEvents}");
    Console.WriteLine($"prepared: {coverageAudit.Summary.TotalPrepared}");
    Console.WriteLine($"played: {coverageAudit.Summary.TotalPlayed}");
    Console.WriteLine($"unmapped: {coverageAudit.Summary.TotalUnmapped}");
    Console.WriteLine($"covered_policy_actions: {coverageAudit.Summary.CoveredPolicyActions}");
    Console.WriteLine($"missing_policy_actions: {coverageAudit.Summary.MissingPolicyActions}");
    Console.WriteLine($"unique_candidates: {coverageAudit.Summary.UniqueCandidateCount}");
    Console.WriteLine($"status: {coverageAudit.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(coverageReportPath)}");
    Console.WriteLine("mode: authored clean-room trace audit; candidate routes are not original event or sample bindings");
    return coverageAudit.Summary.Status == "verified-authored" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "gameplay-coverage-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc gameplay-coverage-audit <workspace> [--report <saida.tsv>]");
        return 2;
    }

    var gameplayCoverageWorkspace = Path.GetFullPath(args[1]);
    var gameplayCoverageReportPath = Path.Combine(
        gameplayCoverageWorkspace,
        "fifa_pc",
        "gameplay-coverage.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            gameplayCoverageReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    var gameplayCoverageAudit = GameplayCoverageAudit.Audit(
        GameplayCoverageAudit.DefaultScenarios(gameplayCoverageWorkspace));
    GameplayCoverageAudit.Write(gameplayCoverageReportPath, gameplayCoverageAudit);
    Console.WriteLine($"{GameIdentity.Name} PC authored gameplay coverage audit");
    Console.WriteLine($"scenarios: {gameplayCoverageAudit.Summary.ScenarioCount}");
    Console.WriteLine($"verified_scenarios: {gameplayCoverageAudit.Summary.VerifiedScenarioCount}");
    Console.WriteLine($"events: {gameplayCoverageAudit.Summary.TotalEvents}");
    Console.WriteLine($"covered_actions: {gameplayCoverageAudit.Summary.CoveredActions}");
    Console.WriteLine($"missing_actions: {gameplayCoverageAudit.Summary.MissingActions}");
    Console.WriteLine($"status: {gameplayCoverageAudit.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(gameplayCoverageReportPath)}");
    Console.WriteLine("mode: authored clean-room trace/replay audit; original rule semantics are not claimed");
    return gameplayCoverageAudit.Summary.Status == "verified-authored" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "clean-room-gameplay-invariant-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc clean-room-gameplay-invariant-audit <workspace> [--report <saida.tsv>]");
        return 2;
    }

    var gameplayInvariantWorkspace = Path.GetFullPath(args[1]);
    var gameplayInvariantReportPath = Path.Combine(
        gameplayInvariantWorkspace,
        "fifa_pc",
        "clean-room-gameplay-invariants.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            gameplayInvariantReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    var gameplayInvariantAudit = CleanRoomGameplayInvariantAudit.Audit(
        CleanRoomGameplayInvariantAudit.DefaultScenarios(gameplayInvariantWorkspace));
    CleanRoomGameplayInvariantAudit.Write(gameplayInvariantReportPath, gameplayInvariantAudit);
    Console.WriteLine($"{GameIdentity.Name} PC clean-room gameplay invariant audit");
    Console.WriteLine($"scenarios: {gameplayInvariantAudit.Summary.ScenarioCount}");
    Console.WriteLine($"verified_scenarios: {gameplayInvariantAudit.Summary.VerifiedScenarioCount}");
    Console.WriteLine($"frames: {gameplayInvariantAudit.Summary.TotalFrames}");
    Console.WriteLine($"events: {gameplayInvariantAudit.Summary.TotalEvents}");
    Console.WriteLine($"status: {gameplayInvariantAudit.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(gameplayInvariantReportPath)}");
    Console.WriteLine("mode: authored clean-room invariant audit; original gameplay rules and semantics are not claimed");
    return gameplayInvariantAudit.Summary.Status == "verified-authored" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "clean-room-integrated-coverage-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc clean-room-integrated-coverage-audit <workspace> [--report <saida.tsv>]");
        return 2;
    }

    var integratedCoverageWorkspace = Path.GetFullPath(args[1]);
    var integratedCoverageReportPath = Path.Combine(
        integratedCoverageWorkspace,
        "fifa_pc",
        "clean-room-integrated-coverage.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            integratedCoverageReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    var integratedCoverageAudit = CleanRoomIntegratedCoverageAudit.Audit(
        CleanRoomIntegratedCoverageAudit.DefaultScenarios(integratedCoverageWorkspace));
    CleanRoomIntegratedCoverageAudit.Write(integratedCoverageReportPath, integratedCoverageAudit);
    Console.WriteLine($"{GameIdentity.Name} PC authored integrated scenario coverage audit");
    Console.WriteLine($"scenarios: {integratedCoverageAudit.Summary.ScenarioCount}");
    Console.WriteLine($"verified_scenarios: {integratedCoverageAudit.Summary.VerifiedScenarioCount}");
    Console.WriteLine($"rendered_frames: {integratedCoverageAudit.Summary.TotalRenderedFrames}");
    Console.WriteLine($"audio_events: {integratedCoverageAudit.Summary.TotalAudioEvents}");
    Console.WriteLine($"animation_samples: {integratedCoverageAudit.Summary.TotalAnimationSamples}");
    Console.WriteLine($"status: {integratedCoverageAudit.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(integratedCoverageReportPath)}");
    Console.WriteLine("mode: authored clean-room cross-system trace audit; original assets and semantics are not claimed");
    return integratedCoverageAudit.Summary.Status == "verified-authored" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "candidate-integrated-runtime-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc candidate-integrated-runtime-audit <workspace> [--report <saida.tsv>]");
        return 2;
    }

    var candidateRuntimeWorkspace = Path.GetFullPath(args[1]);
    var candidateRuntimeReportPath = Path.Combine(
        candidateRuntimeWorkspace,
        "fifa_pc",
        "candidate-integrated-runtime-audit.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            candidateRuntimeReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    var candidateRuntimeAudit = CandidateIntegratedRuntimeAudit.Audit(
        CandidateIntegratedRuntimeAudit.DefaultScenario(candidateRuntimeWorkspace));
    CandidateIntegratedRuntimeAudit.Write(candidateRuntimeReportPath, candidateRuntimeAudit);
    Console.WriteLine($"{GameIdentity.Name} PC candidate integrated runtime audit");
    Console.WriteLine($"candidate_flags_valid: {candidateRuntimeAudit.CandidateFlagsValid}");
    Console.WriteLine($"provenance_valid: {candidateRuntimeAudit.ProvenanceValid}");
    Console.WriteLine($"candidate_asset_provenance_valid: {candidateRuntimeAudit.CandidateAssetProvenanceValid}");
    Console.WriteLine($"candidate_mesh_state_crosscheck_valid: {candidateRuntimeAudit.CandidateMeshStateCrosscheckValid}");
    Console.WriteLine($"candidate_asset_count: {candidateRuntimeAudit.CandidateAssetCount}");
    Console.WriteLine($"integration_reports_match: {candidateRuntimeAudit.IntegrationReportsMatch}");
    Console.WriteLine($"trace_files_valid: {candidateRuntimeAudit.TraceFilesValid}");
    Console.WriteLine($"replay_matched: {candidateRuntimeAudit.ReplayMatched}");
    Console.WriteLine($"required_actions_present: {candidateRuntimeAudit.RequiredActionsPresent}");
    Console.WriteLine($"rendered_frames: {candidateRuntimeAudit.RenderedFrames}");
    Console.WriteLine($"animation_samples: {candidateRuntimeAudit.AnimationSamples}");
    Console.WriteLine($"audio_events: {candidateRuntimeAudit.AudioEvents}");
    Console.WriteLine($"audio_unmapped: {candidateRuntimeAudit.AudioUnmapped}");
    Console.WriteLine($"frame_visible_pixels: {candidateRuntimeAudit.FrameVisiblePixels}");
    Console.WriteLine($"status: {candidateRuntimeAudit.Status}");
    Console.WriteLine($"original_semantics: {candidateRuntimeAudit.OriginalSemantics}");
    if (!string.IsNullOrWhiteSpace(candidateRuntimeAudit.Issue))
    {
        Console.WriteLine($"issue: {candidateRuntimeAudit.Issue}");
    }
    Console.WriteLine($"report: {Path.GetFullPath(candidateRuntimeReportPath)}");
    Console.WriteLine("mode: candidate opt-in managed runtime evidence; original materials, bindings and semantics are not claimed");
    return candidateRuntimeAudit.Status == "verified-candidate" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "audio-event-payload-wave-correlation-exhaustive-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-event-payload-wave-correlation-exhaustive-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--max-sound-candidates <n>]");
        return 2;
    }

    var exhaustiveAudioIsoPath = Path.GetFullPath(args[1]);
    var exhaustiveAudioReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-payload-wave-correlation-exhaustive.tsv");
    var exhaustiveAudioSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-payload-wave-correlation-exhaustive-summary.tsv");
    var exhaustiveAudioMaximumSoundCandidates =
        AudioEventPayloadWaveCorrelationAudit.DefaultMaximumSoundCandidatesPerBank;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            exhaustiveAudioReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            exhaustiveAudioSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--max-sound-candidates", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(
                    args[argumentIndex + 1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out exhaustiveAudioMaximumSoundCandidates) ||
                exhaustiveAudioMaximumSoundCandidates <= 0)
            {
                Console.Error.WriteLine("--max-sound-candidates requer um inteiro positivo.");
                return 2;
            }
        }
    }

    using var exhaustiveAudioFileSystem = new XdvdfsFileSystem(exhaustiveAudioIsoPath);
    var exhaustiveAudioCatalog = GameAssetCatalog.Scan(exhaustiveAudioFileSystem);
    var exhaustiveAudioCorrelation = AudioEventPayloadWaveCorrelationAudit.AuditAllBankCandidates(
        exhaustiveAudioCatalog,
        exhaustiveAudioFileSystem.OpenReadOnly,
        exhaustiveAudioMaximumSoundCandidates);
    AudioEventPayloadWaveCorrelationAudit.WriteReport(
        exhaustiveAudioReportPath,
        exhaustiveAudioCorrelation.Rows);
    AudioEventPayloadWaveCorrelationAudit.WriteExhaustiveSummary(
        exhaustiveAudioSummaryPath,
        exhaustiveAudioCorrelation.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC exhaustive audio event payload-wave correlation audit");
    Console.WriteLine($"event_candidates: {exhaustiveAudioCorrelation.Summary.EventCandidates}");
    Console.WriteLine($"event_decoded: {exhaustiveAudioCorrelation.Summary.EventDecoded}");
    Console.WriteLine($"bank_count: {exhaustiveAudioCorrelation.Summary.BankCount}");
    Console.WriteLine($"bank_sound_candidates: {exhaustiveAudioCorrelation.Summary.BankSoundCandidates}");
    Console.WriteLine($"bank_sound_decoded: {exhaustiveAudioCorrelation.Summary.BankSoundDecoded}");
    Console.WriteLine($"exact_pcm_hash_pairs: {exhaustiveAudioCorrelation.Summary.ExactPcmHashPairs}");
    Console.WriteLine($"event_hashes_with_match: {exhaustiveAudioCorrelation.Summary.EventHashesWithMatch}");
    Console.WriteLine($"maximum_sound_candidates_per_bank: {exhaustiveAudioCorrelation.Summary.MaximumSoundCandidatesPerBank}");
    Console.WriteLine($"report: {exhaustiveAudioReportPath}");
    Console.WriteLine($"summary: {exhaustiveAudioSummaryPath}");
    Console.WriteLine($"status: {exhaustiveAudioCorrelation.Summary.Status}");
    Console.WriteLine("mode: ISO read-only, all bounded EVT/ABKC/AST sound-index candidates and PCM hashes; no event, sample, gameplay or original selection semantics assigned");
    return exhaustiveAudioCorrelation.Summary.Status == "verified-structural" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "audio-named-event-runtime-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-named-event-runtime-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var namedEventRuntimeIsoPath = Path.GetFullPath(args[1]);
    var namedEventRuntimeReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-named-event-runtime.tsv");
    var namedEventRuntimeSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-named-event-runtime-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            namedEventRuntimeReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            namedEventRuntimeSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    using var namedEventRuntimeFileSystem = new XdvdfsFileSystem(namedEventRuntimeIsoPath);
    var namedEventRuntimeCatalog = GameAssetCatalog.Scan(namedEventRuntimeFileSystem);
    var namedEventRuntimeAudit = AudioNamedEventRuntimeAudit.Audit(
        namedEventRuntimeCatalog,
        namedEventRuntimeFileSystem.OpenReadOnly);
    AudioNamedEventRuntimeAudit.WriteReport(
        namedEventRuntimeReportPath,
        namedEventRuntimeAudit.Rows);
    AudioNamedEventRuntimeAudit.WriteSummary(
        namedEventRuntimeSummaryPath,
        namedEventRuntimeAudit.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC named EVT payload runtime audit");
    Console.WriteLine($"candidate_count: {namedEventRuntimeAudit.Summary.CandidateCount}");
    Console.WriteLine($"candidate_metadata_validated: {namedEventRuntimeAudit.Summary.CandidateMetadataValidated}");
    Console.WriteLine($"runtime_prepared: {namedEventRuntimeAudit.Summary.RuntimePrepared}");
    Console.WriteLine($"wave_headers_valid: {namedEventRuntimeAudit.Summary.WaveHeadersValid}");
    Console.WriteLine($"pcm_bytes: {namedEventRuntimeAudit.Summary.PcmBytes}");
    Console.WriteLine($"report: {Path.GetFullPath(namedEventRuntimeReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(namedEventRuntimeSummaryPath)}");
    Console.WriteLine($"status: {namedEventRuntimeAudit.Summary.Status}");
    Console.WriteLine($"provenance: {namedEventRuntimeAudit.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {namedEventRuntimeAudit.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, exact named EVT payload preparation only; no gameplay event or original sample-selection binding assigned");
    return namedEventRuntimeAudit.Summary.Status == "verified-runtime-named-payload" ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "audio-event-parallel-range-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-event-parallel-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var parallelRangeIsoPath = Path.GetFullPath(args[1]);
    var parallelRangeReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-parallel-range.tsv");
    var parallelRangeSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-parallel-range-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            parallelRangeReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            parallelRangeSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }
    if (string.Equals(parallelRangeIsoPath, parallelRangeReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(parallelRangeIsoPath, parallelRangeSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório do controle de ranges não pode sobrescrever a ISO.");
        return 2;
    }

    using var parallelRangeFileSystem = new XdvdfsFileSystem(parallelRangeIsoPath);
    var parallelRangeCatalog = GameAssetCatalog.Scan(parallelRangeFileSystem);
    var parallelRangeAudit = AudioEventParallelRangeAudit.Audit(
        parallelRangeCatalog,
        parallelRangeFileSystem.OpenReadOnly);
    AudioEventParallelRangeAudit.WriteReport(parallelRangeReportPath, parallelRangeAudit.Rows);
    AudioEventParallelRangeAuditSummary.Write(parallelRangeSummaryPath, parallelRangeAudit.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC named EVT parallel PA range control");
    Console.WriteLine($"candidate_count: {parallelRangeAudit.Summary.CandidateCount}");
    Console.WriteLine($"event_string_anchor_valid: {parallelRangeAudit.Summary.EventStringAnchorValidCount}");
    Console.WriteLine($"header_range_valid: {parallelRangeAudit.Summary.HeaderRangeValidCount}");
    Console.WriteLine($"data_range_valid: {parallelRangeAudit.Summary.DataRangeValidCount}");
    Console.WriteLine($"parallel_row_index_valid: {parallelRangeAudit.Summary.ParallelRowIndexValidCount}");
    Console.WriteLine($"data_schl_prefix: {parallelRangeAudit.Summary.DataSchlPrefixCount}");
    Console.WriteLine($"unique_event_names: {parallelRangeAudit.Summary.UniqueEventNameCount}");
    Console.WriteLine($"unique_event_string_offsets: {parallelRangeAudit.Summary.UniqueEventStringOffsetCount}");
    Console.WriteLine($"unique_header_rows: {parallelRangeAudit.Summary.UniqueHeaderRowCount}");
    Console.WriteLine($"header_table_rows: {parallelRangeAudit.Summary.HeaderTableRows}");
    Console.WriteLine($"data_table_rows: {parallelRangeAudit.Summary.DataTableRows}");
    Console.WriteLine($"verified_rows: {parallelRangeAudit.Summary.VerifiedRowCount}");
    Console.WriteLine($"report: {Path.GetFullPath(parallelRangeReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(parallelRangeSummaryPath)}");
    Console.WriteLine($"status: {parallelRangeAudit.Summary.Status}");
    Console.WriteLine($"provenance: {parallelRangeAudit.Summary.Provenance}");
    Console.WriteLine($"direct_selection_binding: {parallelRangeAudit.Summary.DirectSelectionBinding}");
    Console.WriteLine($"original_semantics: {parallelRangeAudit.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, exact EVT text-start and offset-table range control; no gameplay, ABKC or sample-selection binding");
    return parallelRangeAudit.Summary.Status == AudioEventParallelRangeAudit.Status ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "audio-event-xbe-hash-search-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-event-xbe-hash-search-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var xbeHashSearchIsoPath = Path.GetFullPath(args[1]);
    var xbeHashSearchReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-xbe-hash-search.tsv");
    var xbeHashSearchSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-xbe-hash-search-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            xbeHashSearchReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            xbeHashSearchSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }
    if (string.Equals(xbeHashSearchIsoPath, xbeHashSearchReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(xbeHashSearchIsoPath, xbeHashSearchSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório do hash-search não pode sobrescrever a ISO.");
        return 2;
    }

    using var xbeHashSearchFileSystem = new XdvdfsFileSystem(xbeHashSearchIsoPath);
    var xbeHashSearchAudit = AudioEventXbeHashSearchAudit.Audit(xbeHashSearchFileSystem);
    AudioEventXbeHashSearchAudit.WriteReport(xbeHashSearchReportPath, xbeHashSearchAudit.Rows);
    AudioEventXbeHashSearchSummary.Write(xbeHashSearchSummaryPath, xbeHashSearchAudit.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC EVT hash words in retail XBE");
    Console.WriteLine($"candidate_count: {xbeHashSearchAudit.Summary.CandidateCount}");
    Console.WriteLine($"unique_event_names: {xbeHashSearchAudit.Summary.UniqueEventNames}");
    Console.WriteLine($"unique_hash_words: {xbeHashSearchAudit.Summary.UniqueHashWords}");
    Console.WriteLine($"xbe_bytes: {xbeHashSearchAudit.Summary.XbeBytes}");
    Console.WriteLine($"aligned_occurrences: {xbeHashSearchAudit.Summary.AlignedOccurrences}");
    Console.WriteLine($"unaligned_occurrences: {xbeHashSearchAudit.Summary.UnalignedOccurrences}");
    Console.WriteLine($"hash_words_with_aligned_hits: {xbeHashSearchAudit.Summary.HashWordsWithAlignedHits}");
    Console.WriteLine($"hash_words_with_unaligned_hits: {xbeHashSearchAudit.Summary.HashWordsWithUnalignedHits}");
    Console.WriteLine($"report: {Path.GetFullPath(xbeHashSearchReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(xbeHashSearchSummaryPath)}");
    Console.WriteLine($"status: {xbeHashSearchAudit.Summary.Status}");
    Console.WriteLine($"provenance: {xbeHashSearchAudit.Summary.Provenance}");
    Console.WriteLine($"direct_selection_binding: {xbeHashSearchAudit.Summary.DirectSelectionBinding}");
    Console.WriteLine($"original_semantics: {xbeHashSearchAudit.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, bounded raw XBE word search; no execution or event/sample binding");
    return xbeHashSearchAudit.Summary.Status == AudioEventXbeHashSearchAudit.Status ? 0 : 1;
}

if (args.Length > 0 && string.Equals(args[0], "morph-ambiguity-byte-summary", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: morph-ambiguity-byte-summary <ambiguity-report.tsv> <byte-report.tsv> <summary.tsv>");
        return 2;
    }

    var byteSummaryAmbiguityPath = Path.GetFullPath(args[1]);
    var byteSummaryByteReportPath = Path.GetFullPath(args[2]);
    var byteSummaryOutputPath = Path.GetFullPath(args[3]);
    if (new[] { byteSummaryAmbiguityPath, byteSummaryByteReportPath }
        .Any(path => string.Equals(path, byteSummaryOutputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O resumo byte-level não pode sobrescrever um relatório de entrada.");
        return 2;
    }

    var byteSummaryAmbiguityRows = MorphAmbiguitySummarizer.ReadReport(byteSummaryAmbiguityPath);
    var byteSummaryByteRows = MorphAmbiguityByteSummarizer.ReadReport(byteSummaryByteReportPath);
    var byteSummaries = MorphAmbiguityByteSummarizer.Summarize(
        byteSummaryAmbiguityRows,
        byteSummaryByteRows);
    var byteSummaryLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "variation_fields",
            "ambiguous_layouts",
            "ambiguous_rows",
            "byte_report_rows",
            "missing_byte_report_rows",
            "command_exact_rows",
            "command_boundary_exact_rows",
            "command_structural_exact_rows",
            "numeric_record_exact_rows",
            "numeric_signature_exact_rows",
            "numeric_boundary_exact_rows",
            "numeric_value_category_exact_rows",
            "command_min_total_delta_range",
            "command_min_aligned_difference_range",
            "command_min_length_delta_range",
            "command_max_common_prefix_range",
            "layout_key_sha256s",
        }),
    };
    byteSummaryLines.AddRange(byteSummaries.Select(summary => string.Join("\t", new[]
    {
        CatalogField(summary.VariationFields),
        summary.AmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
        summary.AmbiguousRows.ToString(CultureInfo.InvariantCulture),
        summary.ByteReportRows.ToString(CultureInfo.InvariantCulture),
        summary.MissingByteReportRows.ToString(CultureInfo.InvariantCulture),
        summary.CommandExactRows.ToString(CultureInfo.InvariantCulture),
        summary.CommandBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
        summary.CommandStructuralExactRows.ToString(CultureInfo.InvariantCulture),
        summary.NumericRecordExactRows.ToString(CultureInfo.InvariantCulture),
        summary.NumericSignatureExactRows.ToString(CultureInfo.InvariantCulture),
        summary.NumericBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
        summary.NumericValueCategoryExactRows.ToString(CultureInfo.InvariantCulture),
        CatalogField(summary.CommandMinTotalDeltaRange),
        CatalogField(summary.CommandMinAlignedDifferenceRange),
        CatalogField(summary.CommandMinLengthDeltaRange),
        CatalogField(summary.CommandMaxCommonPrefixRange),
        CatalogField(summary.LayoutKeySha256s),
    })));

    var byteSummaryDirectory = Path.GetDirectoryName(byteSummaryOutputPath);
    if (!string.IsNullOrEmpty(byteSummaryDirectory))
    {
        Directory.CreateDirectory(byteSummaryDirectory);
    }

    File.WriteAllLines(
        byteSummaryOutputPath,
        byteSummaryLines,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"morph_ambiguity_byte_summary: {byteSummaryOutputPath} | ambiguity_rows={byteSummaryAmbiguityRows.Count} | byte_rows={byteSummaryByteRows.Count} | compositions={byteSummaries.Count} | missing_rows={byteSummaries.Sum(summary => summary.MissingByteReportRows)}");
    Console.WriteLine("status: exact structural-layout join of observed ambiguity labels and byte metrics only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "morph-ambiguity-numeric-summary", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: morph-ambiguity-numeric-summary <ambiguity-report.tsv> <numeric-gap-report.tsv> <summary.tsv>");
        return 2;
    }

    var numericSummaryAmbiguityPath = Path.GetFullPath(args[1]);
    var numericSummaryGapReportPath = Path.GetFullPath(args[2]);
    var numericSummaryOutputPath = Path.GetFullPath(args[3]);
    if (new[] { numericSummaryAmbiguityPath, numericSummaryGapReportPath }
        .Any(path => string.Equals(path, numericSummaryOutputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O resumo numérico não pode sobrescrever um relatório de entrada.");
        return 2;
    }

    var numericSummaryAmbiguityRows = MorphAmbiguitySummarizer.ReadReport(numericSummaryAmbiguityPath);
    var numericSummaryGapRows = MorphAmbiguityNumericSummarizer.ReadReport(numericSummaryGapReportPath);
    var numericSummaries = MorphAmbiguityNumericSummarizer.Summarize(
        numericSummaryAmbiguityRows,
        numericSummaryGapRows);
    var numericSummaryLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "variation_fields",
            "ambiguous_layouts",
            "gap_rows",
            "distinct_gap_layouts",
            "missing_gap_layouts",
            "boundary_exact_rows",
            "relative_boundary_exact_rows",
            "value_category_exact_rows",
            "payload_window_exact_rows",
            "canonical_offset_exact_rows",
            "payload_window_zero_difference_rows",
            "canonical_word_difference_range",
            "canonical_offset_difference_range",
            "payload_window_difference_range",
            "payload_window_equal_range",
            "payload_window_common_prefix_range",
            "layout_key_sha256s",
        }),
    };
    numericSummaryLines.AddRange(numericSummaries.Select(summary => string.Join("\t", new[]
    {
        CatalogField(summary.VariationFields),
        summary.AmbiguousLayouts.ToString(CultureInfo.InvariantCulture),
        summary.GapRows.ToString(CultureInfo.InvariantCulture),
        summary.DistinctGapLayouts.ToString(CultureInfo.InvariantCulture),
        summary.MissingGapLayouts.ToString(CultureInfo.InvariantCulture),
        summary.BoundaryExactRows.ToString(CultureInfo.InvariantCulture),
        summary.RelativeBoundaryExactRows.ToString(CultureInfo.InvariantCulture),
        summary.ValueCategoryExactRows.ToString(CultureInfo.InvariantCulture),
        summary.PayloadWindowExactRows.ToString(CultureInfo.InvariantCulture),
        summary.CanonicalOffsetExactRows.ToString(CultureInfo.InvariantCulture),
        summary.PayloadWindowZeroDifferenceRows.ToString(CultureInfo.InvariantCulture),
        CatalogField(summary.CanonicalWordDifferenceRange),
        CatalogField(summary.CanonicalOffsetDifferenceRange),
        CatalogField(summary.PayloadWindowDifferenceRange),
        CatalogField(summary.PayloadWindowEqualRange),
        CatalogField(summary.PayloadWindowCommonPrefixRange),
        CatalogField(summary.LayoutKeySha256s),
    })));

    var numericSummaryDirectory = Path.GetDirectoryName(numericSummaryOutputPath);
    if (!string.IsNullOrEmpty(numericSummaryDirectory))
    {
        Directory.CreateDirectory(numericSummaryDirectory);
    }

    File.WriteAllLines(
        numericSummaryOutputPath,
        numericSummaryLines,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"morph_ambiguity_numeric_summary: {numericSummaryOutputPath} | ambiguity_rows={numericSummaryAmbiguityRows.Count} | gap_rows={numericSummaryGapRows.Count} | compositions={numericSummaries.Count} | missing_gap_layouts={numericSummaries.Sum(summary => summary.MissingGapLayouts)}");
    Console.WriteLine("status: exact structural-layout join of observed ambiguity labels and numeric boundary metrics only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "morph-ambiguity-group-metadata", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5)
    {
        Console.Error.WriteLine("Uso: morph-ambiguity-group-metadata <ambiguity-report.tsv> <byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>");
        return 2;
    }

    var groupMetadataAmbiguityPath = Path.GetFullPath(args[1]);
    var groupMetadataBytePath = Path.GetFullPath(args[2]);
    var groupMetadataNumericPath = Path.GetFullPath(args[3]);
    var groupMetadataOutputPath = Path.GetFullPath(args[4]);
    if (new[] { groupMetadataAmbiguityPath, groupMetadataBytePath, groupMetadataNumericPath }
        .Any(path => string.Equals(path, groupMetadataOutputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("A matriz de metadata não pode sobrescrever um relatório de entrada.");
        return 2;
    }

    var groupMetadataAmbiguityRows = MorphAmbiguitySummarizer.ReadReport(groupMetadataAmbiguityPath);
    var groupMetadataByteRows = MorphAmbiguityByteSummarizer.ReadReport(groupMetadataBytePath);
    var groupMetadataNumericRows = MorphAmbiguityNumericSummarizer.ReadReport(groupMetadataNumericPath);
    var groupMetadataRows = MorphAmbiguityGroupSummarizer.Summarize(
        groupMetadataAmbiguityRows,
        groupMetadataByteRows,
        groupMetadataNumericRows);
    MorphAmbiguityGroupSummarizer.WriteReport(groupMetadataOutputPath, groupMetadataRows);
    Console.WriteLine($"morph_ambiguity_group_metadata: {groupMetadataOutputPath} | ambiguity_rows={groupMetadataAmbiguityRows.Count} | byte_rows={groupMetadataByteRows.Count} | numeric_gap_rows={groupMetadataNumericRows.Count} | ambiguous_layouts={groupMetadataRows.Count}");
    Console.WriteLine($"observed_join_coverage: byte_rows={groupMetadataRows.Sum(row => row.ByteReportRows)} | numeric_gap_rows={groupMetadataRows.Sum(row => row.NumericGapRows)} | missing_byte_layouts={groupMetadataRows.Count(row => row.ByteReportRows == 0)} | missing_numeric_gap_layouts={groupMetadataRows.Count(row => row.NumericGapRows == 0)}");
    Console.WriteLine("status: grouped observed variation, boundary, and metadata counts only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "morph-candidate-geometry-ambiguity-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: morph-candidate-geometry-ambiguity-audit <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var geometryAmbiguityCatalogPath = Path.GetFullPath(args[1]);
    var geometryAmbiguityReportPath = Path.GetFullPath(args[2]);
    var geometryAmbiguityAuditPath = Path.GetFullPath(args[3]);
    var geometryAmbiguitySummaryPath = Path.Combine(
        Path.GetDirectoryName(geometryAmbiguityAuditPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(geometryAmbiguityAuditPath) + "-summary.tsv");
    for (var argumentIndex = 4; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            geometryAmbiguitySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { geometryAmbiguityCatalogPath, geometryAmbiguityReportPath }
        .Any(path => string.Equals(path, geometryAmbiguityAuditPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, geometryAmbiguitySummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(geometryAmbiguityAuditPath, geometryAmbiguitySummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria geométrica independente não pode sobrescrever os relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var geometryAmbiguityCatalogRows = MorphCandidateGeometryAmbiguityAudit.ReadCatalog(
        geometryAmbiguityCatalogPath);
    var geometryAmbiguityReportRows = MorphAmbiguitySummarizer.ReadReport(
        geometryAmbiguityReportPath);
    var geometryAmbiguityAuditRows = MorphCandidateGeometryAmbiguityAudit.Audit(
        geometryAmbiguityCatalogRows,
        geometryAmbiguityReportRows);
    MorphCandidateGeometryAmbiguityAudit.WriteReports(
        geometryAmbiguityAuditPath,
        geometryAmbiguitySummaryPath,
        geometryAmbiguityAuditRows);
    var geometryAmbiguitySummaryRows = MorphCandidateGeometryAmbiguityAudit.Summarize(
        geometryAmbiguityAuditRows);
    var geometryAmbiguityAll = geometryAmbiguitySummaryRows.Single(row =>
        string.Equals(row.Scope, "all", StringComparison.Ordinal));
    Console.WriteLine($"morph_candidate_geometry_ambiguity_audit: {geometryAmbiguityAuditPath} | summary={geometryAmbiguitySummaryPath} | catalog_rows={geometryAmbiguityCatalogRows.Count} | layouts={geometryAmbiguityAll.Layouts}");
    Console.WriteLine($"geometry_label_consistency: geometry_variable_layouts={geometryAmbiguityAll.GeometryVariableLayouts} | geometry_variable_ambiguous_layouts={geometryAmbiguityAll.GeometryVariableAmbiguousLayouts} | geometry_variable_stable_layouts={geometryAmbiguityAll.GeometryVariableStableLayouts} | ambiguous_without_geometry_variable={geometryAmbiguityAll.AmbiguousWithoutGeometryVariableLayouts} | label_mismatches={geometryAmbiguityAll.GeometryVariableLabelMismatches}");
    Console.WriteLine($"report_consistency: row_count_mismatches={geometryAmbiguityAll.ReportRowCountMismatches} | missing_geometry_layouts={geometryAmbiguityAll.MissingGeometryLayouts}");
    Console.WriteLine("status: independent candidate-geometry versus ambiguity-label audit only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "morph-ambiguity-independent-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: morph-ambiguity-independent-audit <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var independentCatalogPath = Path.GetFullPath(args[1]);
    var independentAmbiguityPath = Path.GetFullPath(args[2]);
    var independentAuditPath = Path.GetFullPath(args[3]);
    var independentSummaryPath = Path.Combine(
        Path.GetDirectoryName(independentAuditPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(independentAuditPath) + "-summary.tsv");
    for (var argumentIndex = 4; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            independentSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { independentCatalogPath, independentAmbiguityPath }
        .Any(path => string.Equals(path, independentAuditPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, independentSummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(independentAuditPath, independentSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria independente não pode sobrescrever os relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var independentCatalogRows = MorphAmbiguityIndependentAudit.ReadCatalog(independentCatalogPath);
    var independentAmbiguityRows = MorphAmbiguitySummarizer.ReadReport(independentAmbiguityPath);
    var independentAuditRows = MorphAmbiguityIndependentAudit.Audit(
        independentCatalogRows,
        independentAmbiguityRows);
    MorphAmbiguityIndependentAudit.WriteReports(
        independentAuditPath,
        independentSummaryPath,
        independentAuditRows);
    var independentSummaryRows = MorphAmbiguityIndependentAudit.Summarize(independentAuditRows);
    var independentAll = independentSummaryRows.Single(row =>
        string.Equals(row.Scope, "all", StringComparison.Ordinal));
    var independentAmbiguous = independentSummaryRows.Single(row =>
        string.Equals(row.Scope, "ambiguous", StringComparison.Ordinal));
    Console.WriteLine($"morph_ambiguity_independent_audit: {independentAuditPath} | summary={independentSummaryPath} | catalog_rows={independentCatalogRows.Count} | layouts={independentAll.Layouts} | ambiguous_layouts={independentAmbiguous.Layouts}");
    Console.WriteLine($"independent_layers: candidate_mesh_stable_companion_variable={independentAmbiguous.CandidateMeshStableCompanionVariableLayouts} | candidate_mesh_variable={independentAmbiguous.CandidateMeshVariableLayouts} | any_layer_variable={independentAmbiguous.AnyIndependentLayerVariableLayouts} | all_layers_stable={independentAmbiguous.AllIndependentLayersStableLayouts}");
    Console.WriteLine($"report_consistency: metadata_mismatches={independentAll.MetadataMismatches} | row_count_mismatches={independentAll.ReportRowCountMismatches} | ambiguity_label_mismatches={independentAll.AmbiguityLabelMismatches}");
    Console.WriteLine("status: cross-layer audit of observed structural signatures only; no morph, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "eagl-morph-name-state-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: eagl-morph-name-state-audit <morph-structural-catalog.tsv> <saida.tsv>");
        return 2;
    }

    var nameStateCatalogPath = Path.GetFullPath(args[1]);
    var nameStateReportPath = Path.GetFullPath(args[2]);
    if (string.Equals(nameStateCatalogPath, nameStateReportPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria de nomes/estado não pode sobrescrever o catálogo de entrada.");
        return 2;
    }

    var nameStateCatalogRows = EaglMorphNameStateAudit.ReadCatalog(nameStateCatalogPath);
    var nameStateAuditRows = EaglMorphNameStateAudit.Audit(nameStateCatalogRows);
    EaglMorphNameStateAudit.WriteReport(nameStateReportPath, nameStateAuditRows);
    var nameStateSummary = EaglMorphNameStateAudit.Summarize(nameStateAuditRows);
    Console.WriteLine($"eagl_morph_name_state_audit: {nameStateReportPath} | rows={nameStateSummary.Rows} | complete_siblings={nameStateSummary.CompleteSiblingRows}");
    Console.WriteLine($"morph_shape_names: rows={nameStateSummary.RowsWithMorphShapeNames} | distinct={nameStateSummary.DistinctMorphShapeNames}");
    Console.WriteLine($"sibling_shapename_state: rows={nameStateSummary.RowsWithSiblingShapeNameState} | distinct={nameStateSummary.DistinctSiblingShapeNameStateValues}");
    Console.WriteLine($"lexical_matches: exact_raw_rows={nameStateSummary.RowsWithExactRawShapeStateMatch} | normalized_rows={nameStateSummary.RowsWithNormalizedShapeStateMatch} | exact_raw_count={nameStateSummary.ExactRawShapeStateMatchCount} | normalized_count={nameStateSummary.NormalizedShapeStateMatchCount}");
    Console.WriteLine("status: preserved morph-name and companion SHAPENAME text comparison only; no morph, mesh-group, material, UV, skinning, rigging, animation, or GPU binding assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "eagl-morph-animation-name-negative-control", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: eagl-morph-animation-name-negative-control <morph-structural-catalog.tsv> <animation-bank-name-index.tsv> <saida.tsv>");
        return 2;
    }

    var morphAnimationNameCatalogPath = Path.GetFullPath(args[1]);
    var morphAnimationNameIndexPath = Path.GetFullPath(args[2]);
    var morphAnimationNameReportPath = Path.GetFullPath(args[3]);
    if (new[] { morphAnimationNameCatalogPath, morphAnimationNameIndexPath }
        .Any(path => string.Equals(path, morphAnimationNameReportPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O controle negativo de nomes não pode sobrescrever os relatórios de entrada.");
        return 2;
    }

    var morphAnimationNameRows = EaglMorphNameStateAudit.ReadCatalog(morphAnimationNameCatalogPath);
    var animationNameIndex = EaglMorphAnimationNameNegativeControl.ReadAnimationNameIndex(
        morphAnimationNameIndexPath);
    var morphAnimationNameSummary = EaglMorphAnimationNameNegativeControl.Evaluate(
        morphAnimationNameRows,
        animationNameIndex.RowCount,
        animationNameIndex.BankNames,
        animationNameIndex.FileNames);
    EaglMorphAnimationNameNegativeControl.WriteReport(
        morphAnimationNameReportPath,
        morphAnimationNameSummary);
    Console.WriteLine($"eagl_morph_animation_name_negative_control: {morphAnimationNameReportPath}");
    Console.WriteLine($"morph_rows: {morphAnimationNameSummary.MorphRows} | complete_siblings={morphAnimationNameSummary.CompleteSiblingRows} | shape_names={morphAnimationNameSummary.DistinctMorphShapeNames}");
    Console.WriteLine($"animation_names: rows={morphAnimationNameSummary.AnimationNameIndexRows} | files={morphAnimationNameSummary.DistinctAnimationFileNames} | banks={morphAnimationNameSummary.DistinctAnimationBankNames}");
    Console.WriteLine($"exact_file_name_matches: rows={morphAnimationNameSummary.RowsWithExactAnimationFileNameMatch} | occurrences={morphAnimationNameSummary.ExactAnimationFileNameMatchCount} | distinct={morphAnimationNameSummary.DistinctExactAnimationFileNameMatches}");
    Console.WriteLine($"exact_bank_name_matches: rows={morphAnimationNameSummary.RowsWithExactAnimationBankNameMatch} | occurrences={morphAnimationNameSummary.ExactAnimationBankNameMatchCount} | distinct={morphAnimationNameSummary.DistinctExactAnimationBankNameMatches}");
    Console.WriteLine("status: exact lexical negative control only; no morph, animation, pose, rig, timing, frame, skinning, material, UV, GPU, or command binding assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (args.Length > 0 && string.Equals(args[0], "eagl-morph-shpx-structure-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 6)
    {
        Console.Error.WriteLine("Uso: eagl-morph-shpx-structure-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var shpxAuditIsoPath = Path.GetFullPath(args[1]);
    var shpxAuditArchivePath = args[2];
    var shpxAuditCatalogPath = Path.GetFullPath(args[3]);
    var shpxAuditAmbiguityPath = Path.GetFullPath(args[4]);
    var shpxAuditReportPath = Path.GetFullPath(args[5]);
    var shpxAuditSummaryPath = Path.Combine(
        Path.GetDirectoryName(shpxAuditReportPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(shpxAuditReportPath) + "-summary.tsv");
    for (var argumentIndex = 6; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            shpxAuditSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { shpxAuditCatalogPath, shpxAuditAmbiguityPath }
        .Any(path => string.Equals(path, shpxAuditReportPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, shpxAuditSummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(shpxAuditReportPath, shpxAuditSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria SHPX não pode sobrescrever os relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var shpxAuditCatalog = MorphAmbiguityIndependentAudit.ReadCatalog(shpxAuditCatalogPath)
        .Select(row => new MorphShpxCatalogRow(row.NestedBig, row.MorphObject, row.LayoutKey))
        .ToArray();
    var shpxAuditAmbiguity = MorphAmbiguitySummarizer.ReadReport(shpxAuditAmbiguityPath);
    using var shpxAuditStore = new GameAssetStore(shpxAuditIsoPath);
    var shpxAuditObservations = MorphShpxStructureAudit.Scan(
        shpxAuditStore,
        shpxAuditArchivePath,
        shpxAuditCatalog);
    var shpxAuditRows = MorphShpxStructureAudit.Audit(
        shpxAuditCatalog,
        shpxAuditAmbiguity,
        shpxAuditObservations);
    MorphShpxStructureAudit.WriteReports(
        shpxAuditReportPath,
        shpxAuditSummaryPath,
        shpxAuditRows);
    var shpxAuditSummaryRows = MorphShpxStructureAudit.Summarize(shpxAuditRows);
    var shpxAuditAll = shpxAuditSummaryRows.Single(row =>
        string.Equals(row.Scope, "all", StringComparison.Ordinal));
    var shpxAuditAmbiguous = shpxAuditSummaryRows.Single(row =>
        string.Equals(row.Scope, "ambiguous", StringComparison.Ordinal));
    Console.WriteLine($"eagl_morph_shpx_structure_audit: {shpxAuditReportPath} | summary={shpxAuditSummaryPath} | catalog_rows={shpxAuditAll.CatalogRows} | observed_rows={shpxAuditAll.ObservedRows} | layouts={shpxAuditAll.Layouts}");
    Console.WriteLine($"shpx_validation: found_rows={shpxAuditAll.ObservedRows - shpxAuditAll.MissingXshRows} | validated_rows={shpxAuditAll.XshValidatedRows} | missing_rows={shpxAuditAll.MissingXshRows} | invalid_rows={shpxAuditAll.InvalidXshRows} | variable_layouts={shpxAuditAll.XshVariableLayouts} | variable_ambiguous_layouts={shpxAuditAmbiguous.XshVariableLayouts}");
    Console.WriteLine($"report_consistency: observation_row_count_mismatches={shpxAuditAll.ObservationRowCountMismatches} | ambiguity_report_row_count_mismatches={shpxAuditAll.ReportRowCountMismatches}");
    Console.WriteLine("status: independent same-trunk SHPX header/table structure only; no morph, mesh, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, SHPX package-table inspection");
    return shpxAuditAll.CatalogRows == shpxAuditAll.ObservedRows &&
           shpxAuditAll.XshValidatedRows + shpxAuditAll.MissingXshRows == shpxAuditAll.ObservedRows &&
           shpxAuditAll.InvalidXshRows == 0 &&
           shpxAuditAll.ReportRowCountMismatches == 0
        ? 0
        : 1;
}

if (args.Length > 0 && string.Equals(args[0], "eagl-morph-shpx-geometry-correlation-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: eagl-morph-shpx-geometry-correlation-audit <structural-catalog.tsv> <shpx-audit.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var shpxGeometryCatalogPath = Path.GetFullPath(args[1]);
    var shpxGeometryShpxPath = Path.GetFullPath(args[2]);
    var shpxGeometryReportPath = Path.GetFullPath(args[3]);
    var shpxGeometrySummaryPath = Path.Combine(
        Path.GetDirectoryName(shpxGeometryReportPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(shpxGeometryReportPath) + "-summary.tsv");
    for (var argumentIndex = 4; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            shpxGeometrySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { shpxGeometryCatalogPath, shpxGeometryShpxPath }
        .Any(path => string.Equals(path, shpxGeometryReportPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, shpxGeometrySummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(shpxGeometryReportPath, shpxGeometrySummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A correlação SHPX/geometria não pode sobrescrever relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var shpxGeometryCatalog = MorphShpxGeometryCorrelationAudit.ReadCatalog(shpxGeometryCatalogPath);
    var shpxGeometryShpx = MorphShpxGeometryCorrelationAudit.ReadShpxReport(shpxGeometryShpxPath);
    var shpxGeometryRows = MorphShpxGeometryCorrelationAudit.Audit(
        shpxGeometryCatalog,
        shpxGeometryShpx);
    MorphShpxGeometryCorrelationAudit.WriteReports(
        shpxGeometryReportPath,
        shpxGeometrySummaryPath,
        shpxGeometryRows);
    var shpxGeometrySummaryRows = MorphShpxGeometryCorrelationAudit.Summarize(shpxGeometryRows);
    var shpxGeometryAll = shpxGeometrySummaryRows.Single(row =>
        string.Equals(row.Scope, "all", StringComparison.Ordinal));
    var shpxGeometryAmbiguous = shpxGeometrySummaryRows.Single(row =>
        string.Equals(row.Scope, "ambiguous", StringComparison.Ordinal));
    Console.WriteLine($"eagl_morph_shpx_geometry_correlation_audit: {shpxGeometryReportPath} | summary={shpxGeometrySummaryPath} | layouts={shpxGeometryAll.Layouts} | joined={shpxGeometryAll.JoinedLayouts}");
    Console.WriteLine($"co_variation: geometry_variable={shpxGeometryAll.CandidateGeometryVariableLayouts} | shpx_variable={shpxGeometryAll.ShpxVariableLayouts} | both_variable={shpxGeometryAll.BothVariableLayouts} | geometry_only={shpxGeometryAll.GeometryVariableOnlyLayouts} | shpx_only={shpxGeometryAll.ShpxVariableOnlyLayouts} | both_stable={shpxGeometryAll.BothStableLayouts} | ambiguous_both_variable={shpxGeometryAmbiguous.BothVariableLayouts}");
    Console.WriteLine($"join_consistency: missing_catalog={shpxGeometryAll.MissingCatalogLayouts} | missing_shpx={shpxGeometryAll.MissingShpxLayouts} | geometry_missing_layouts={shpxGeometryAll.CandidateGeometryMissingLayouts}");
    Console.WriteLine("status: layout-level SHPX/candidate-geometry co-variation only; no texture-entry, morph, mesh, UV, material, pose, rig, skinning, animation, timing, or GPU binding assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access; SHPX and geometry reports remain separate sources");
    return shpxGeometryAll.MissingCatalogLayouts == 0 &&
           shpxGeometryAll.MissingShpxLayouts == 0
        ? 0
        : 1;
}

if (args.Length > 0 && string.Equals(args[0], "eagl-morph-elf-header-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 6)
    {
        Console.Error.WriteLine("Uso: eagl-morph-elf-header-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var elfAuditIsoPath = Path.GetFullPath(args[1]);
    var elfAuditArchivePath = args[2];
    var elfAuditCatalogPath = Path.GetFullPath(args[3]);
    var elfAuditAmbiguityPath = Path.GetFullPath(args[4]);
    var elfAuditReportPath = Path.GetFullPath(args[5]);
    var elfAuditSummaryPath = Path.Combine(
        Path.GetDirectoryName(elfAuditReportPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(elfAuditReportPath) + "-summary.tsv");
    for (var argumentIndex = 6; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            elfAuditSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { elfAuditCatalogPath, elfAuditAmbiguityPath }
        .Any(path => string.Equals(path, elfAuditReportPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, elfAuditSummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(elfAuditReportPath, elfAuditSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria ELF de header não pode sobrescrever os relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var elfAuditCatalog = MorphAmbiguityIndependentAudit.ReadCatalog(elfAuditCatalogPath)
        .Select(row => new MorphShpxCatalogRow(row.NestedBig, row.MorphObject, row.LayoutKey))
        .ToArray();
    var elfAuditAmbiguity = MorphAmbiguitySummarizer.ReadReport(elfAuditAmbiguityPath);
    using var elfAuditStore = new GameAssetStore(elfAuditIsoPath);
    var elfAuditObservations = MorphElfHeaderAudit.Scan(
        elfAuditStore,
        elfAuditArchivePath,
        elfAuditCatalog);
    var elfAuditRows = MorphElfHeaderAudit.Audit(
        elfAuditCatalog,
        elfAuditAmbiguity,
        elfAuditObservations);
    MorphElfHeaderAudit.WriteReports(
        elfAuditReportPath,
        elfAuditSummaryPath,
        elfAuditRows);
    var elfAuditSummaryRows = MorphElfHeaderAudit.Summarize(elfAuditRows);
    var elfAuditAll = elfAuditSummaryRows.Single(row =>
        string.Equals(row.Scope, "all", StringComparison.Ordinal));
    var elfAuditAmbiguous = elfAuditSummaryRows.Single(row =>
        string.Equals(row.Scope, "ambiguous", StringComparison.Ordinal));
    Console.WriteLine($"eagl_morph_elf_header_audit: {elfAuditReportPath} | summary={elfAuditSummaryPath} | catalog_rows={elfAuditAll.CatalogRows} | observed_rows={elfAuditAll.ObservedRows} | layouts={elfAuditAll.Layouts}");
    Console.WriteLine($"elf_validation: validated_rows={elfAuditAll.ValidatedRows} | invalid_rows={elfAuditAll.InvalidRows} | header_variable_layouts={elfAuditAll.HeaderVariableLayouts} | header_variable_ambiguous_layouts={elfAuditAmbiguous.HeaderVariableLayouts} | symbol_metadata_variable_layouts={elfAuditAll.SymbolMetadataVariableLayouts}");
    Console.WriteLine($"report_consistency: observation_row_count_mismatches={elfAuditAll.ObservationRowCountMismatches} | ambiguity_report_row_count_mismatches={elfAuditAll.ReportRowCountMismatches}");
    Console.WriteLine("status: independent ELF header and __Morph::: symbol metadata only; no morph, mesh, pose, rig, weight, frame, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG, managed ELF header inspection");
    return elfAuditAll.CatalogRows == elfAuditAll.ObservedRows &&
           elfAuditAll.ValidatedRows == elfAuditAll.ObservedRows &&
           elfAuditAll.InvalidRows == 0 &&
           elfAuditAll.ReportRowCountMismatches == 0
        ? 0
        : 1;
}

if (args.Length < 2)
{
    Console.Error.WriteLine("Uso: FifaStreetPc run-headless <caminho-do-iso> [frames] [--demo|--aimed-demo|--round-demo|--gamebreaker-demo|--juggle-demo|--steal-demo|--tackle-demo] [--input-script <entrada.tsv>] [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--frame-output <saida.ppm>] [--match-trace-output <saida.tsv>] [--match-trace-expected <entrada.tsv>] [--input-trace-output <saida.tsv>] [--input-trace-expected <entrada.tsv>] [--animation-trace-output <saida.tsv>] [--animation-trace-expected <entrada.tsv>] [--audio-trace-output <saida.tsv>] [--audio-trace-expected <entrada.tsv>] [--integration-report <saida.tsv>] [--match-seconds <segundos>] [--match-rule timed|first-to-goals|goals-or-panna] [--target-goals <gols>] [--target-pannas <pannas>] [--opponent-ai] [--play-audio] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc project-readiness-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc asset-smoke <caminho-do-iso> <arquivo.big> <entrada>");
    Console.Error.WriteLine("   ou: FifaStreetPc inventory <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-inventory <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-payload-compare <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-payload-name-audit <caminho-do-iso> [--report <saida.tsv>] [--min-length <n>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-payload-anchor-audit <payload-name-audit.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-runtime-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-name-index <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-timestamp-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc external-animation-timing-audit <observacoes.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-compare <caminho-do-iso> [--profile-report <saida.tsv>] [--pair-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-elf-structure <caminho-do-iso> [--profile-report <saida.tsv>] [--section-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-data-relocation-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-morph-relocation-profile-audit <morph-catalog.tsv> <animation-relocations.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-tail-compare <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-tail-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-tail-pair-relocation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--detail-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-sidecar-boundary-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-runtime-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sidecar-boundary-audit <caminho-do-iso> <arquivo.big> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-shpx-structure-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-shpx-geometry-correlation-audit <structural-catalog.tsv> <shpx-audit.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-elf-header-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-pose-bank-index <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-animation-name-negative-control <morph-structural-catalog.tsv> <animation-bank-name-index.tsv> <saida.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-block-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-decode-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-validate-smoke <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-big-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-big-validate <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc apt-structure-inventory <caminho-do-iso> <arquivo.bh> [--report <saida.tsv>] [--profile-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc apt-animation-negative-control <apt-fe.tsv> <apt-ig.tsv> <animation-bank-name-index.tsv> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc apt-cross-package-profile-audit <apt-fe-profiles.tsv> <apt-ig-profiles.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-summary <ambiguity-report.tsv> <summary.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-byte-summary <ambiguity-report.tsv> <byte-report.tsv> <summary.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-numeric-summary <ambiguity-report.tsv> <numeric-gap-report.tsv> <summary.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-group-metadata <ambiguity-report.tsv> <byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-candidate-geometry-ambiguity-audit <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-independent-audit <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-name-state-audit <morph-structural-catalog.tsv> <saida.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-model-inventory <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc big-model-inventory <caminho-do-iso> <arquivo.big> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-shapename-index <caminho-do-iso> <arquivo.big>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-smoke <caminho-do-iso> <nested.big> <SHAPENAME> [objeto.ord]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-batch-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-runtime-batch-smoke <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-asset-provenance-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-render-influence-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-geometry-audit <caminho-do-iso> [--report <saida.tsv>] [--pixel-size <pixels>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-attribute-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-geometry-stream-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-render-parameter-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-indexed-draw-source-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-declaration-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-vertex-program-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-model-material-reference-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-render-chain-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-vertex-record-association-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-topology-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-opaque-discrimination-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-command-payload-identity-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-command-descriptor-adjacency-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-vertex-payload-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-candidate-slot-profile-audit <original-stream-numeric.tsv> <candidate-fetch-association.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-candidate-raw-stream-identity-audit <caminho-do-iso> <candidate-fetch-association.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-state-token-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-projection-preview <caminho-do-iso> <nested.big> <SHAPENAME> <diretório-de-saída> [objeto.ord]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-batch-preview <caminho-do-iso> <diretório-de-saída>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-preview <caminho-do-iso> <nested.big> <SHAPENAME> <saida.ppm> [objeto.ord]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-position-cloud <caminho-do-iso> <arquivo.big> <nested.big> <objeto.ord> [saida.obj]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-strip-candidate-export <caminho-do-iso> <arquivo.big> <nested.big> <objeto.ord> [saida.obj]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-smoke <caminho-do-iso> [--commands]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-object-smoke <caminho-do-iso> <nested.big> <objeto.ord> [--commands]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-state-correspondence <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-adjacency-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-profile-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-order-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-preview <caminho-do-iso> <saida.ppm>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-object-symbol-smoke <caminho-do-iso> <arquivo.big> <nested.big> <entrada> [--payload|--raw]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-smoke <caminho-do-iso> <nested.big> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-inventory <caminho-do-iso> <arquivo.big> [--catalog <saida.tsv>] [--ambiguity-report <saida.tsv>] [--byte-report <saida.tsv>] [--numeric-gap-report <saida.tsv>] [--numeric-gap-correlation-report <saida.tsv>] [--geometry-report <saida.tsv>] [--geometry-summary <saida.tsv>] [--geometry-correlation-report <saida.tsv>] [--geometry-correlation-summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-asset-smoke <caminho-do-iso> <arquivo.bh> <índice> <entrada>");
    Console.Error.WriteLine("   ou: FifaStreetPc asset-format-smoke <caminho-do-iso> <arquivo> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-structure-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-inspect <caminho-do-iso> [arquivo.abk]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-decode <caminho-do-iso> <arquivo.abk> <índice-do-som> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-play <caminho-do-iso> <arquivo.abk> <índice-do-som>");
    Console.Error.WriteLine("       audio-abk-inspect aceita --validate-streams e --all-bnk");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-candidate-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-text-smoke <caminho-do-iso> [bytes-de-contexto]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-index-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-index-search <caminho-do-iso> [--data] [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-audio-asset-hash-universe-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-data-asset-hash-universe-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-payload-wave-correlation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--max-bank-attempts <n>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-payload-wave-correlation-exhaustive-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--max-sound-candidates <n>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-abk-raw-identity-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-named-event-runtime-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-parallel-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-xbe-hash-search-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-candidate-decode <caminho-do-iso> <row> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-candidate-validate <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-named-candidates <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc fifast2-csi-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc fifast2-csi-tuna-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc fifast2-csi-opaque-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc fifast2-csi-opaque-field-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-named-decode-smoke <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-batch-decode-smoke <caminho-do-iso> [--report <saida.tsv>] [--max-attempts <n>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-sccl-channel-layout-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc gameplay-audio-coverage-audit <workspace> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc gameplay-coverage-audit <workspace> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc clean-room-gameplay-invariant-audit <workspace> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc clean-room-integrated-coverage-audit <workspace> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc candidate-integrated-runtime-audit <workspace> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-sccl-inspect <caminho-do-iso> [amostras] [--rejected]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-validate <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc texture-preview <caminho-do-iso> <arquivo.xbx> <saida.ppm>");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-export <caminho-do-iso> <arquivo.xsh> <diretório>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-shpx-export <caminho-do-iso> <arquivo.bh> <índice> <entrada.xsh> <diretório>");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-frame-preview <caminho-do-iso> <arquivo.xsh> <índice-do-quadro> <saida.ppm>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-export <caminho-do-iso> <arquivo.ast> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-play <caminho-do-iso> <arquivo.ast>");
    Console.Error.WriteLine("   ou: FifaStreetPc render-preview <caminho-do-iso> <saida.ppm> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc run-windowed <caminho-do-iso> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--round-reset] [--match-seconds <segundos>] [--run-seconds <segundos>] [--capture-frame <saida.ppm>] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc self-test");
    return 2;
}

if (string.Equals(args[0], "asset-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc asset-smoke <caminho-do-iso> <arquivo.big> <entrada>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var asset = assetStore.OpenBigEntry(args[2], args[3]);
    var bytes = new byte[Math.Min(16, checked((int)asset.Length))];
    var read = asset.Read(bytes, 0, bytes.Length);
    Console.WriteLine($"{GameIdentity.Name} PC asset smoke");
    Console.WriteLine($"archive: {args[2]}");
    Console.WriteLine($"entry: {args[3]}");
    Console.WriteLine($"size: {asset.Length}");
    Console.WriteLine($"first_bytes: {Convert.ToHexString(bytes, 0, read)}");
    Console.WriteLine("mode: ISO read-only, bounded BIG entry stream");
    return 0;
}

if (string.Equals(args[0], "animation-bank-inventory", StringComparison.OrdinalIgnoreCase))
{
    string? animationBankReportPath = null;
    var animationBankReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (animationBankReportOptionIndex >= 0)
    {
        if (animationBankReportOptionIndex + 1 >= args.Length ||
            args[animationBankReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-inventory ... --report <saida.tsv>");
            return 2;
        }

        animationBankReportPath = Path.GetFullPath(args[animationBankReportOptionIndex + 1]);
        if (string.Equals(
                animationBankReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de bancos não pode sobrescrever a ISO de entrada.");
            return 2;
        }
    }

    using var animationBankStore = new GameAssetStore(args[1]);
    using var animationArchiveStream = animationBankStore.OpenRaw("data/anims.big");
    var animationArchive = BigArchiveReader.Inspect(animationArchiveStream);
    var animationCatalogEntries = animationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationOrdEntries = animationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationOrlEntries = animationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationReportLines = animationBankReportPath is null
        ? null
        : new List<string>
        {
            string.Join("\t", new[]
            {
                "axt_entry",
                "bank_name",
                "declared_file_count",
                "catalog_file_count",
                "catalog_file_count_matches",
                "ord_entry",
                "orl_entry",
                "ord_size",
                "orl_size",
                "elf_valid",
                "elf_machine",
                "elf_section_count",
                "elf_symbol_count",
                "elf_relocation_count",
                "data_section_name",
                "data_section_offset",
                "data_section_size",
                "relocation_layout_sha256",
                "normalized_data_payload_sha256",
                "animation_bank_symbols",
                "animation_bank_symbol_section",
                "animation_bank_symbol_value",
                "animation_bank_symbol_size",
                "animation_bank_symbol_tail_distance",
                "animation_bank_symbol_matches_catalog",
            }),
        };
    var bankListNames = Array.Empty<string>();
    var catalogCount = 0;
    var catalogFileCount = 0L;
    var completePairCount = 0;
    var validElfCount = 0;
    var animationBankSymbolMatchCount = 0;
    var invalidCatalogCount = 0;
    var animationLayoutSignatures = new HashSet<string>(StringComparer.Ordinal);
    var animationPayloadSignatures = new HashSet<string>(StringComparer.Ordinal);

    foreach (var catalogEntry in animationCatalogEntries)
    {
        using var catalogStream = animationBankStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        var catalogData = ReadBounded(catalogStream, 4 * 1024 * 1024);
        if (string.Equals(catalogEntry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        {
            bankListNames = AnimationBankReader.ParseBankList(catalogData).ToArray();
            continue;
        }

        catalogCount++;
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(catalogData);
        }
        catch (InvalidDataException)
        {
            invalidCatalogCount++;
            continue;
        }

        catalogFileCount += animationCatalog.Files.Count;
        var ordEntry = animationOrdEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                animationCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationOrlEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                animationCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        var elfValid = false;
        ushort elfMachine = 0;
        var elfSectionCount = 0;
        var elfSymbolCount = 0;
        var elfRelocationCount = 0;
        string? dataSectionName = null;
        uint dataSectionOffset = 0;
        uint dataSectionSize = 0;
        var relocationLayoutSignature = string.Empty;
        var normalizedDataPayloadSignature = string.Empty;
        var animationBankSymbols = Array.Empty<string>();
        var animationBankSymbolSection = string.Empty;
        var animationBankSymbolValue = string.Empty;
        var animationBankSymbolSize = string.Empty;
        var animationBankSymbolTailDistance = string.Empty;
        var symbolMatchesCatalog = false;
        if (ordEntry is not null && orlEntry is not null)
        {
            completePairCount++;
            using var ordStream = animationBankStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationBankStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            try
            {
                var elf = ElfReader.Inspect(EaglObjectReader.Combine(ordData, orlData));
                elfValid = true;
                validElfCount++;
                elfMachine = elf.Machine;
                elfSectionCount = elf.Sections.Count;
                elfSymbolCount = elf.Symbols.Count;
                elfRelocationCount = elf.Relocations.Count;
                var dataSection = elf.Sections.FirstOrDefault(section =>
                    string.Equals(section.Name, ".data", StringComparison.Ordinal));
                if (dataSection is not null)
                {
                    dataSectionName = dataSection.Name;
                    dataSectionOffset = dataSection.Offset;
                    dataSectionSize = dataSection.Size;
                }
                relocationLayoutSignature = AnimationBankReader.GetStructuralLayoutSignature(elf);
                animationLayoutSignatures.Add(relocationLayoutSignature);
                normalizedDataPayloadSignature =
                    AnimationBankReader.GetNormalizedDataPayloadSignature(
                        EaglObjectReader.Combine(ordData, orlData),
                        elf);
                animationPayloadSignatures.Add(normalizedDataPayloadSignature);
                animationBankSymbols = AnimationBankReader.GetAnimationBankSymbolNames(elf.Symbols).ToArray();
                var animationBankSymbol = elf.Symbols.FirstOrDefault(symbol =>
                    symbol.Name.StartsWith("__AnimationBank:::", StringComparison.Ordinal));
                if (animationBankSymbol is not null)
                {
                    animationBankSymbolSection = animationBankSymbol.SectionIndex.ToString(CultureInfo.InvariantCulture);
                    animationBankSymbolValue = animationBankSymbol.Value.ToString(CultureInfo.InvariantCulture);
                    animationBankSymbolSize = animationBankSymbol.Size.ToString(CultureInfo.InvariantCulture);
                    if (dataSection is not null &&
                        animationBankSymbol.Value <= dataSection.Size &&
                        animationBankSymbol.Size <= dataSection.Size - animationBankSymbol.Value)
                    {
                        animationBankSymbolTailDistance = (
                            dataSection.Size - animationBankSymbol.Value - animationBankSymbol.Size)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                }
                symbolMatchesCatalog = animationBankSymbols.Length == 1 &&
                    string.Equals(animationBankSymbols[0], animationCatalog.BankName, StringComparison.OrdinalIgnoreCase);
                if (symbolMatchesCatalog)
                {
                    animationBankSymbolMatchCount++;
                }
            }
            catch (InvalidDataException)
            {
                // Preserve an explicit invalid ELF state in the report.
            }
        }

        if (animationReportLines is not null)
        {
            animationReportLines.Add(string.Join("\t", new[]
            {
                CatalogField(catalogEntry.Name),
                CatalogField(animationCatalog.BankName),
                animationCatalog.DeclaredFileCount.ToString(CultureInfo.InvariantCulture),
                animationCatalog.Files.Count.ToString(CultureInfo.InvariantCulture),
                animationCatalog.DeclaredFileCountMatches.ToString(),
                CatalogField(ordEntry?.Name),
                CatalogField(orlEntry?.Name),
                ordEntry?.Size.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                orlEntry?.Size.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                elfValid.ToString(),
                elfValid ? elfMachine.ToString(CultureInfo.InvariantCulture) : string.Empty,
                elfValid ? elfSectionCount.ToString(CultureInfo.InvariantCulture) : string.Empty,
                elfValid ? elfSymbolCount.ToString(CultureInfo.InvariantCulture) : string.Empty,
                elfValid ? elfRelocationCount.ToString(CultureInfo.InvariantCulture) : string.Empty,
                CatalogField(dataSectionName),
                elfValid ? dataSectionOffset.ToString(CultureInfo.InvariantCulture) : string.Empty,
                elfValid ? dataSectionSize.ToString(CultureInfo.InvariantCulture) : string.Empty,
                CatalogField(relocationLayoutSignature),
                CatalogField(normalizedDataPayloadSignature),
                CatalogField(string.Join("|", animationBankSymbols)),
                CatalogField(animationBankSymbolSection),
                CatalogField(animationBankSymbolValue),
                CatalogField(animationBankSymbolSize),
                CatalogField(animationBankSymbolTailDistance),
                symbolMatchesCatalog.ToString(),
            }));
        }
    }

    Console.WriteLine($"{GameIdentity.Name} PC animation bank catalog inventory");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"axt_entries: {animationCatalogEntries.Length}");
    Console.WriteLine($"banklist_names: {bankListNames.Length}");
    Console.WriteLine($"catalog_banks: {catalogCount}");
    Console.WriteLine($"catalog_file_entries: {catalogFileCount}");
    Console.WriteLine($"invalid_catalogs: {invalidCatalogCount}");
    Console.WriteLine($"complete_ord_orl_pairs: {completePairCount}");
    Console.WriteLine($"valid_split_elfs: {validElfCount}");
    Console.WriteLine($"animation_bank_symbol_matches: {animationBankSymbolMatchCount}");
    Console.WriteLine($"distinct_elf_layout_signatures: {animationLayoutSignatures.Count}");
    Console.WriteLine($"distinct_normalized_data_payloads: {animationPayloadSignatures.Count}");
    if (animationBankReportPath is not null && animationReportLines is not null)
    {
        var reportDirectory = Path.GetDirectoryName(animationBankReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        File.WriteAllLines(
            animationBankReportPath,
            animationReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"report: {animationBankReportPath} | rows={animationReportLines.Count - 1}");
    }
    Console.WriteLine("status: AXT catalog, same-bank .ord/.orl pairing, and __AnimationBank identity only; frame, pose, rig, timing, and skinning semantics remain undecoded");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection");
    return 0;
}

if (string.Equals(args[0], "animation-bank-payload-compare", StringComparison.OrdinalIgnoreCase))
{
    string? animationPayloadReportPath = null;
    var animationPayloadReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (animationPayloadReportOptionIndex >= 0)
    {
        if (animationPayloadReportOptionIndex + 1 >= args.Length ||
            args[animationPayloadReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-payload-compare ... --report <saida.tsv>");
            return 2;
        }

        animationPayloadReportPath = Path.GetFullPath(args[animationPayloadReportOptionIndex + 1]);
        if (string.Equals(
                animationPayloadReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de payloads não pode sobrescrever a ISO de entrada.");
            return 2;
        }
    }

    using var animationPayloadStore = new GameAssetStore(args[1]);
    using var animationPayloadArchiveStream = animationPayloadStore.OpenRaw("data/anims.big");
    var animationPayloadArchive = BigArchiveReader.Inspect(animationPayloadArchiveStream);
    var animationPayloadCatalogEntries = animationPayloadArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPayloadOrdEntries = animationPayloadArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPayloadOrlEntries = animationPayloadArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPayloadObservations = new List<(
        string BankName,
        string CatalogEntry,
        byte[] RawData,
        byte[] NormalizedData,
        int DataRelocationCount)>();

    foreach (var catalogEntry in animationPayloadCatalogEntries)
    {
        using var catalogStream = animationPayloadStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            continue;
        }

        var ordEntry = animationPayloadOrdEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                animationCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationPayloadOrlEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                animationCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            continue;
        }

        using var ordStream = animationPayloadStore.OpenBigEntry("data/anims.big", ordEntry.Name);
        using var orlStream = animationPayloadStore.OpenBigEntry("data/anims.big", orlEntry.Name);
        var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
        var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
        var combinedData = EaglObjectReader.Combine(ordData, orlData);
        try
        {
            var elf = ElfReader.Inspect(combinedData);
            var dataSectionIndex = -1;
            ElfSection? dataSection = null;
            for (var sectionIndex = 0; sectionIndex < elf.Sections.Count; sectionIndex++)
            {
                if (string.Equals(elf.Sections[sectionIndex].Name, ".data", StringComparison.Ordinal))
                {
                    dataSectionIndex = sectionIndex;
                    dataSection = elf.Sections[sectionIndex];
                    break;
                }
            }

            if (dataSection is null ||
                (ulong)dataSection.Offset + dataSection.Size > (ulong)combinedData.Length)
            {
                continue;
            }

            var rawData = combinedData
                .AsSpan(checked((int)dataSection.Offset), checked((int)dataSection.Size))
                .ToArray();
            var normalizedData = AnimationBankReader.GetNormalizedDataPayload(combinedData, elf);
            var dataRelocationCount = elf.Relocations.Count(relocation =>
                relocation.TargetSectionIndex == dataSectionIndex);
            animationPayloadObservations.Add((
                animationCatalog.BankName,
                catalogEntry.Name,
                rawData,
                normalizedData,
                dataRelocationCount));
        }
        catch (InvalidDataException)
        {
            // Keep the comparison limited to independently validated ELF/data sections.
        }
    }

    var animationPayloadPairs = new List<(
        string BankA,
        string BankB,
        int DataSizeA,
        int DataSizeB,
        int OverlapBytes,
        int RawEqualBytes,
        int NormalizedEqualBytes,
        int OverlapWords,
        int RawEqualWords,
        int NormalizedEqualWords,
        int RawEqualByteRun,
        int NormalizedEqualByteRun,
        int RawEqualWordRun,
        int NormalizedEqualWordRun,
        int DataRelocationsA,
        int DataRelocationsB,
        bool ExactRaw,
        bool ExactNormalized)>();
    for (var firstIndex = 0; firstIndex < animationPayloadObservations.Count; firstIndex++)
    {
        var first = animationPayloadObservations[firstIndex];
        for (var secondIndex = firstIndex + 1; secondIndex < animationPayloadObservations.Count; secondIndex++)
        {
            var second = animationPayloadObservations[secondIndex];
            var overlapBytes = Math.Min(first.RawData.Length, second.RawData.Length);
            var rawEqualBytes = 0;
            var normalizedEqualBytes = 0;
            for (var offset = 0; offset < overlapBytes; offset++)
            {
                if (first.RawData[offset] == second.RawData[offset])
                {
                    rawEqualBytes++;
                }

                if (first.NormalizedData[offset] == second.NormalizedData[offset])
                {
                    normalizedEqualBytes++;
                }
            }

            var overlapWords = overlapBytes / sizeof(uint);
            var rawEqualWords = 0;
            var normalizedEqualWords = 0;
            for (var wordIndex = 0; wordIndex < overlapWords; wordIndex++)
            {
                var offset = wordIndex * sizeof(uint);
                if (first.RawData.AsSpan(offset, sizeof(uint)).SequenceEqual(
                        second.RawData.AsSpan(offset, sizeof(uint))))
                {
                    rawEqualWords++;
                }

                if (first.NormalizedData.AsSpan(offset, sizeof(uint)).SequenceEqual(
                        second.NormalizedData.AsSpan(offset, sizeof(uint))))
                {
                    normalizedEqualWords++;
                }
            }

            animationPayloadPairs.Add((
                first.BankName,
                second.BankName,
                first.RawData.Length,
                second.RawData.Length,
                overlapBytes,
                rawEqualBytes,
                normalizedEqualBytes,
                overlapWords,
                rawEqualWords,
                normalizedEqualWords,
                AnimationBankReader.GetLongestEqualRun(first.RawData, second.RawData, 1),
                AnimationBankReader.GetLongestEqualRun(first.NormalizedData, second.NormalizedData, 1),
                AnimationBankReader.GetLongestEqualRun(first.RawData, second.RawData, sizeof(uint)),
                AnimationBankReader.GetLongestEqualRun(first.NormalizedData, second.NormalizedData, sizeof(uint)),
                first.DataRelocationCount,
                second.DataRelocationCount,
                first.RawData.AsSpan().SequenceEqual(second.RawData),
                first.NormalizedData.AsSpan().SequenceEqual(second.NormalizedData)));
        }
    }

    Console.WriteLine($"{GameIdentity.Name} PC animation bank aligned payload comparison");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"validated_payloads: {animationPayloadObservations.Count}");
    Console.WriteLine($"pair_comparisons: {animationPayloadPairs.Count}");
    Console.WriteLine($"exact_raw_payload_pairs: {animationPayloadPairs.Count(pair => pair.ExactRaw)}");
    Console.WriteLine($"exact_normalized_payload_pairs: {animationPayloadPairs.Count(pair => pair.ExactNormalized)}");
    Console.WriteLine($"maximum_normalized_equal_bytes: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.NormalizedEqualBytes)}");
    Console.WriteLine($"maximum_normalized_equal_words: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.NormalizedEqualWords)}");
    Console.WriteLine($"maximum_raw_equal_byte_run: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.RawEqualByteRun)}");
    Console.WriteLine($"maximum_normalized_equal_byte_run: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.NormalizedEqualByteRun)}");
    Console.WriteLine($"maximum_raw_equal_word_run: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.RawEqualWordRun)}");
    Console.WriteLine($"maximum_normalized_equal_word_run: {animationPayloadPairs.DefaultIfEmpty().Max(pair => pair.NormalizedEqualWordRun)}");
    if (animationPayloadReportPath is not null)
    {
        var reportLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "bank_a",
                "bank_b",
                "data_size_a",
                "data_size_b",
                "overlap_bytes",
                "raw_equal_bytes",
                "normalized_equal_bytes",
                "overlap_words",
                "raw_equal_words",
                "normalized_equal_words",
                "raw_equal_byte_run",
                "normalized_equal_byte_run",
                "raw_equal_word_run",
                "normalized_equal_word_run",
                "data_relocations_a",
                "data_relocations_b",
                "exact_raw_payload",
                "exact_normalized_payload",
            }),
        };
        reportLines.AddRange(animationPayloadPairs.Select(pair => string.Join("\t", new[]
        {
            CatalogField(pair.BankA),
            CatalogField(pair.BankB),
            pair.DataSizeA.ToString(CultureInfo.InvariantCulture),
            pair.DataSizeB.ToString(CultureInfo.InvariantCulture),
            pair.OverlapBytes.ToString(CultureInfo.InvariantCulture),
            pair.RawEqualBytes.ToString(CultureInfo.InvariantCulture),
            pair.NormalizedEqualBytes.ToString(CultureInfo.InvariantCulture),
            pair.OverlapWords.ToString(CultureInfo.InvariantCulture),
            pair.RawEqualWords.ToString(CultureInfo.InvariantCulture),
            pair.NormalizedEqualWords.ToString(CultureInfo.InvariantCulture),
            pair.RawEqualByteRun.ToString(CultureInfo.InvariantCulture),
            pair.NormalizedEqualByteRun.ToString(CultureInfo.InvariantCulture),
            pair.RawEqualWordRun.ToString(CultureInfo.InvariantCulture),
            pair.NormalizedEqualWordRun.ToString(CultureInfo.InvariantCulture),
            pair.DataRelocationsA.ToString(CultureInfo.InvariantCulture),
            pair.DataRelocationsB.ToString(CultureInfo.InvariantCulture),
            pair.ExactRaw.ToString(),
            pair.ExactNormalized.ToString(),
        })));
        var reportDirectory = Path.GetDirectoryName(animationPayloadReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        File.WriteAllLines(
            animationPayloadReportPath,
            reportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"report: {animationPayloadReportPath} | rows={reportLines.Count - 1}");
    }

    Console.WriteLine("status: aligned raw/relocation-masked .data byte and word evidence only; no frame, pose, rig, timing, or skinning semantics decoded");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection");
    return 0;
}

if (string.Equals(args[0], "animation-bank-catalog-payload-name-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-catalog-payload-name-audit <caminho-do-iso> [--report <saida.tsv>] [--min-length <n>]");
        return 2;
    }

    var payloadNameAuditIsoPath = Path.GetFullPath(args[1]);
    var payloadNameAuditReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "animation-bank-catalog-payload-name-audit.tsv");
    var payloadNameAuditMinimumLength = 3;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            payloadNameAuditReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--min-length", StringComparison.OrdinalIgnoreCase) &&
                 int.TryParse(args[argumentIndex + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedMinimumLength) &&
                 parsedMinimumLength > 0)
        {
            payloadNameAuditMinimumLength = parsedMinimumLength;
        }
    }

    var payloadNameAuditSummaryPath = Path.Combine(
        Path.GetDirectoryName(payloadNameAuditReportPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(payloadNameAuditReportPath) + "-summary.tsv");
    using var payloadNameAuditStore = new GameAssetStore(payloadNameAuditIsoPath);
    using var payloadNameAuditArchiveStream = payloadNameAuditStore.OpenRaw("data/anims.big");
    var payloadNameAuditArchive = BigArchiveReader.Inspect(payloadNameAuditArchiveStream);
    var payloadNameAuditCatalogEntries = payloadNameAuditArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var payloadNameAuditOrdEntries = payloadNameAuditArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var payloadNameAuditOrlEntries = payloadNameAuditArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var payloadNameAuditRows = new List<AnimationBankCatalogPayloadNameMatch>();
    var payloadNameAuditSummaries = new List<AnimationBankCatalogPayloadNameSummary>();
    var payloadNameAuditInvalidBanks = new List<string>();
    foreach (var catalogEntry in payloadNameAuditCatalogEntries)
    {
        AnimationBankCatalog payloadNameAuditCatalog;
        try
        {
            using var catalogStream = payloadNameAuditStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
            payloadNameAuditCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            // The animation archive also carries a bank-list AXT entry. It is
            // not a catalog with [Bank]/[Files] sections and is intentionally
            // excluded from the bank-level audit.
            continue;
        }

        var ordEntry = payloadNameAuditOrdEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                payloadNameAuditCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        var orlEntry = payloadNameAuditOrlEntries.FirstOrDefault(entry =>
            string.Equals(
                Path.GetFileNameWithoutExtension(entry.Name),
                payloadNameAuditCatalog.BankName,
                StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            payloadNameAuditInvalidBanks.Add(payloadNameAuditCatalog.BankName);
            continue;
        }

        try
        {
            using var ordStream = payloadNameAuditStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = payloadNameAuditStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var elf = ElfReader.Inspect(combinedData);
            var dataSection = elf.Sections.FirstOrDefault(section =>
                string.Equals(section.Name, ".data", StringComparison.Ordinal));
            if (dataSection is null ||
                (ulong)dataSection.Offset + dataSection.Size > (ulong)combinedData.Length)
            {
                payloadNameAuditInvalidBanks.Add(payloadNameAuditCatalog.BankName);
                continue;
            }

            var dataPayload = combinedData.AsSpan(
                checked((int)dataSection.Offset),
                checked((int)dataSection.Size));
            var asciiRuns = AnimationBankCatalogPayloadNameAudit.ExtractAsciiRuns(
                dataPayload,
                payloadNameAuditMinimumLength);
            var rows = AnimationBankCatalogPayloadNameAudit.Audit(
                catalogEntry.Name,
                payloadNameAuditCatalog,
                dataPayload,
                payloadNameAuditMinimumLength);
            payloadNameAuditRows.AddRange(rows);
            payloadNameAuditSummaries.Add(
                AnimationBankCatalogPayloadNameAudit.Summarize(
                    payloadNameAuditCatalog.BankName,
                    rows,
                    asciiRuns.Count));
        }
        catch (InvalidDataException)
        {
            payloadNameAuditInvalidBanks.Add(payloadNameAuditCatalog.BankName);
        }
    }

    AnimationBankCatalogPayloadNameAudit.WriteReport(payloadNameAuditReportPath, payloadNameAuditRows);
    AnimationBankCatalogPayloadNameAudit.WriteSummary(payloadNameAuditSummaryPath, payloadNameAuditSummaries);
    var payloadNameAuditMissing = payloadNameAuditSummaries.Sum(summary => summary.MissingRows);
    var payloadNameAuditTagged = payloadNameAuditSummaries.Sum(summary => summary.TaggedRows);
    var payloadNameAuditExact = payloadNameAuditSummaries.Sum(summary => summary.ExactRows);
    var payloadNameAuditAmbiguous = payloadNameAuditSummaries.Sum(summary => summary.AmbiguousRows);
    Console.WriteLine($"{GameIdentity.Name} PC animation catalog-to-payload name audit");
    Console.WriteLine($"catalog_banks: {payloadNameAuditCatalogEntries.Length}");
    Console.WriteLine($"validated_banks: {payloadNameAuditSummaries.Count}");
    Console.WriteLine($"catalog_rows: {payloadNameAuditRows.Count}");
    Console.WriteLine($"exact_rows: {payloadNameAuditExact}");
    Console.WriteLine($"tagged_rows: {payloadNameAuditTagged}");
    Console.WriteLine($"missing_rows: {payloadNameAuditMissing}");
    Console.WriteLine($"ambiguous_rows: {payloadNameAuditAmbiguous}");
    Console.WriteLine($"invalid_banks: {payloadNameAuditInvalidBanks.Count}");
    Console.WriteLine($"report: {payloadNameAuditReportPath}");
    Console.WriteLine($"summary: {payloadNameAuditSummaryPath}");
    Console.WriteLine("status: catalog-name and printable .data payload-string evidence only; no frame, pose, rig, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded same-bank .ord entries, managed ELF .data scan");
    return payloadNameAuditInvalidBanks.Count == 0 ? 0 : 1;
}

if (string.Equals(args[0], "animation-bank-catalog-payload-anchor-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-catalog-payload-anchor-audit <payload-name-audit.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var anchorInputPath = Path.GetFullPath(args[1]);
    var anchorReportPath = Path.Combine(
        Path.GetDirectoryName(anchorInputPath) ?? Directory.GetCurrentDirectory(),
        "animation-bank-catalog-payload-anchor-audit.tsv");
    var anchorSummaryPath = Path.Combine(
        Path.GetDirectoryName(anchorInputPath) ?? Directory.GetCurrentDirectory(),
        "animation-bank-catalog-payload-anchor-audit-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            anchorReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            anchorSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    var anchorRows = AnimationBankCatalogPayloadAnchorAudit.ReadAndAudit(anchorInputPath);
    var anchorSummary = AnimationBankCatalogPayloadAnchorAudit.Summarize(anchorRows);
    AnimationBankCatalogPayloadAnchorAudit.WriteReport(anchorReportPath, anchorRows);
    AnimationBankCatalogPayloadAnchorAudit.WriteSummary(anchorSummaryPath, anchorSummary);
    Console.WriteLine($"{GameIdentity.Name} PC animation catalog payload-anchor interval audit");
    Console.WriteLine($"banks: {anchorSummary.BankCount}");
    Console.WriteLine($"catalog_rows: {anchorSummary.CatalogRows}");
    Console.WriteLine($"tagged_rows: {anchorSummary.TaggedRows}");
    Console.WriteLine($"rows_with_two_anchors: {anchorSummary.RowsWithTwoAnchors}");
    Console.WriteLine($"anchors: {anchorSummary.AnchorCount}");
    Console.WriteLine($"within_name_gaps: {anchorSummary.WithinNameGapCount}");
    Console.WriteLine($"cross_name_gaps: {anchorSummary.CrossNameGapCount}");
    Console.WriteLine($"banks_strictly_increasing: {anchorSummary.BanksStrictlyIncreasing}");
    Console.WriteLine($"banks_with_complete_pairs: {anchorSummary.BanksWithCompletePairs}");
    Console.WriteLine($"report: {anchorReportPath}");
    Console.WriteLine($"summary: {anchorSummaryPath}");
    Console.WriteLine("status: printable payload-anchor interval evidence only; intervals are not record boundaries and no frame, pose, rig, timing, skinning, material, UV, or GPU semantics are assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return anchorSummary.Status == "verified-structural" ? 0 : 1;
}

if (string.Equals(args[0], "animation-bank-runtime-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-runtime-smoke <caminho-do-iso>");
        return 2;
    }

    using var animationRuntimeFileSystem = new XdvdfsFileSystem(args[1]);
    var animationRuntimeCatalog = GameAssetCatalog.Scan(animationRuntimeFileSystem);
    var animationRuntimeBanks = animationRuntimeCatalog.AnimationBanks;
    var invalidRuntimeBanks = animationRuntimeBanks
        .Where(bank =>
            !bank.CatalogFileCountMatches ||
            bank.OrdEntry is null ||
            bank.OrlEntry is null ||
            !bank.ElfValidated ||
            !bank.SymbolMatchesCatalog ||
            bank.CatalogFiles.Count != bank.CatalogFileCount ||
            bank.VersionTag.Length == 0)
        .Select(bank => bank.BankName.Length == 0 ? bank.CatalogEntry : bank.BankName)
        .ToArray();
    var observedFileNames = animationRuntimeBanks
        .SelectMany(bank => bank.CatalogFiles)
        .Select(file => file.Name)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();
    var observedVersionTags = animationRuntimeBanks
        .Select(bank => bank.VersionTag)
        .Where(version => version.Length > 0)
        .Distinct(StringComparer.Ordinal)
        .Count();
    var animationRuntimeSymbolMetadata = animationRuntimeBanks
        .SelectMany(bank => bank.AnimationBankSymbolRecords)
        .Select(GetElfSymbolMetadataKey)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(key => key, StringComparer.Ordinal)
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC animation bank runtime smoke");
    Console.WriteLine($"animation_banks: {animationRuntimeBanks.Count}");
    Console.WriteLine($"catalog_file_entries: {animationRuntimeBanks.Sum(bank => bank.CatalogFiles.Count)}");
    Console.WriteLine($"catalog_unique_file_names: {observedFileNames}");
    Console.WriteLine($"catalog_version_tags: {observedVersionTags}");
    Console.WriteLine($"catalog_counts_validated: {animationRuntimeBanks.Count(bank => bank.CatalogFileCountMatches)}");
    Console.WriteLine($"complete_ord_orl_pairs: {animationRuntimeBanks.Count(bank => bank.OrdEntry is not null && bank.OrlEntry is not null)}");
    Console.WriteLine($"elf_validated: {animationRuntimeBanks.Count(bank => bank.ElfValidated)}");
    Console.WriteLine($"symbol_matches: {animationRuntimeBanks.Count(bank => bank.SymbolMatchesCatalog)}");
    Console.WriteLine($"symbol_records: {animationRuntimeBanks.Sum(bank => bank.AnimationBankSymbolRecords.Count)}");
    Console.WriteLine($"distinct_symbol_metadata_records: {animationRuntimeSymbolMetadata.Length}");
    Console.WriteLine($"symbol_metadata_keys: {string.Join(";", animationRuntimeSymbolMetadata)}");
    if (invalidRuntimeBanks.Length > 0)
    {
        Console.WriteLine($"invalid_banks: {string.Join(",", invalidRuntimeBanks)}");
        return 1;
    }

    Console.WriteLine("status: runtime catalog preserved exact AXT names/timestamps and same-bank structural evidence only");
    Console.WriteLine("mode: ISO read-only, no frame/pose/rig/timing/skinning semantics assigned");
    return 0;
}

if (string.Equals(args[0], "animation-bank-name-index", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-name-index <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? animationNameIndexReportPath = null;
    var animationNameIndexReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (animationNameIndexReportOption >= 0)
    {
        if (animationNameIndexReportOption + 1 >= args.Length ||
            args[animationNameIndexReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-name-index ... --report <saida.tsv>");
            return 2;
        }

        animationNameIndexReportPath = Path.GetFullPath(args[animationNameIndexReportOption + 1]);
    }

    using var animationNameIndexFileSystem = new XdvdfsFileSystem(args[1]);
    var animationNameIndexCatalog = GameAssetCatalog.Scan(animationNameIndexFileSystem);
    var animationNameIndexBanks = animationNameIndexCatalog.AnimationBanks;
    var animationNameIndexFiles = animationNameIndexBanks
        .SelectMany(bank => bank.CatalogFiles.Select(file => (Bank: bank, File: file)))
        .OrderBy(item => item.Bank.BankName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(item => item.File.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(item => item.File.Timestamp)
        .ToArray();
    var animationNameIndexBankCounts = animationNameIndexFiles
        .GroupBy(item => item.File.Name, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group
            .Select(item => item.Bank.BankName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(), StringComparer.OrdinalIgnoreCase);
    var animationNameIndexReportLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "bank_name",
            "catalog_entry",
            "version_tag",
            "directory",
            "ord_entry",
            "orl_entry",
            "elf_validated",
            "symbol_matches_catalog",
            "file_name",
            "timestamp",
            "banks_with_same_name",
            "source_line",
        }),
    };
    foreach (var item in animationNameIndexFiles)
    {
        animationNameIndexReportLines.Add(string.Join("\t", new[]
        {
            CatalogField(item.Bank.BankName),
            CatalogField(item.Bank.CatalogEntry),
            CatalogField(item.Bank.VersionTag),
            CatalogField(item.Bank.Directory),
            CatalogField(item.Bank.OrdEntry),
            CatalogField(item.Bank.OrlEntry),
            item.Bank.ElfValidated.ToString(),
            item.Bank.SymbolMatchesCatalog.ToString(),
            CatalogField(item.File.Name),
            item.File.Timestamp.ToString(CultureInfo.InvariantCulture),
            animationNameIndexBankCounts[item.File.Name].ToString(CultureInfo.InvariantCulture),
            CatalogField(item.File.SourceLine),
        }));
    }

    if (animationNameIndexReportPath is not null)
    {
        var animationNameIndexReportDirectory = Path.GetDirectoryName(animationNameIndexReportPath);
        if (!string.IsNullOrEmpty(animationNameIndexReportDirectory))
        {
            Directory.CreateDirectory(animationNameIndexReportDirectory);
        }

        File.WriteAllLines(
            animationNameIndexReportPath,
            animationNameIndexReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    var animationNameIndexSharedNames = animationNameIndexBankCounts.Count(pair => pair.Value > 1);
    Console.WriteLine($"{GameIdentity.Name} PC animation bank catalog name index");
    Console.WriteLine($"animation_banks: {animationNameIndexBanks.Count}");
    Console.WriteLine($"indexed_catalog_file_rows: {animationNameIndexFiles.Length}");
    Console.WriteLine($"unique_catalog_file_names: {animationNameIndexBankCounts.Count}");
    Console.WriteLine($"names_in_multiple_banks: {animationNameIndexSharedNames}");
    Console.WriteLine($"validated_banks: {animationNameIndexBanks.Count(bank => bank.CatalogFileCountMatches && bank.ElfValidated && bank.SymbolMatchesCatalog)}");
    if (animationNameIndexReportPath is not null)
    {
        Console.WriteLine($"report: {animationNameIndexReportPath} | rows={animationNameIndexReportLines.Count - 1}");
    }
    Console.WriteLine("status: catalog names/timestamps and same-bank identity metadata only; no frame, pose, rig, timing, or skinning semantics assigned");
    Console.WriteLine("mode: ISO read-only, runtime catalog, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "animation-bank-catalog-timestamp-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-catalog-timestamp-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var timestampAuditReportPath = Path.GetFullPath(
        "fifa_pc/animation-bank-catalog-timestamp-audit.tsv");
    var timestampAuditSummaryPath = Path.GetFullPath(
        "fifa_pc/animation-bank-catalog-timestamp-audit-summary.tsv");
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-catalog-timestamp-audit ... --report <saida.tsv>");
                return 2;
            }

            timestampAuditReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-catalog-timestamp-audit ... --summary <saida.tsv>");
                return 2;
            }

            timestampAuditSummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine("Uso: animation-bank-catalog-timestamp-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
            return 2;
        }
    }

    using var timestampAuditFileSystem = new XdvdfsFileSystem(args[1]);
    var timestampAuditAssetCatalog = GameAssetCatalog.Scan(timestampAuditFileSystem);
    var timestampAuditCatalogs = timestampAuditAssetCatalog.AnimationBanks
        .Select(bank =>
        (
            CatalogEntry: bank.CatalogEntry,
            Catalog: new AnimationBankCatalog(
                bank.VersionTag,
                bank.BankName,
                bank.Directory,
                bank.DeclaredFileCount,
                bank.CatalogFiles)));
    var timestampAuditRows = AnimationBankCatalogTimestampAudit.Audit(timestampAuditCatalogs);
    var timestampAuditSummary = AnimationBankCatalogTimestampAudit.Summarize(timestampAuditRows);
    AnimationBankCatalogTimestampAudit.WriteReport(timestampAuditReportPath, timestampAuditRows);
    AnimationBankCatalogTimestampAudit.WriteSummary(timestampAuditSummaryPath, timestampAuditSummary);

    Console.WriteLine($"{GameIdentity.Name} PC animation catalog timestamp audit");
    Console.WriteLine($"animation_banks: {timestampAuditSummary.BankCount}");
    Console.WriteLine($"catalog_rows: {timestampAuditSummary.CatalogRows}");
    Console.WriteLine($"display_date_parseable_rows: {timestampAuditSummary.DisplayDateParseableRows}");
    Console.WriteLine($"exact_timestamp_date_rows: {timestampAuditSummary.ExactTimestampDateRows}");
    Console.WriteLine($"timestamp_date_mismatch_rows: {timestampAuditSummary.TimestampDateMismatchRows}");
    Console.WriteLine($"invalid_timestamp_rows: {timestampAuditSummary.InvalidTimestampRows}");
    Console.WriteLine($"inferred_utc_offset_minutes: {timestampAuditSummary.InferredUtcOffsetMinutes}");
    Console.WriteLine($"report: {timestampAuditReportPath}");
    Console.WriteLine($"summary: {timestampAuditSummaryPath}");
    Console.WriteLine($"status: {timestampAuditSummary.Status}; direct_timing_binding={timestampAuditSummary.DirectTimingBinding}; original_semantics={timestampAuditSummary.OriginalSemantics}");
    Console.WriteLine("evidence: numeric catalog timestamps reproduce the calendar date on the same source line; no animation timing semantics assigned");
    return timestampAuditSummary.Status == "verified-structural" ? 0 : 1;
}

if (string.Equals(args[0], "external-animation-timing-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: external-animation-timing-audit <observacoes.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var externalTimingInputPath = Path.GetFullPath(args[1]);
    var externalTimingReportPath = Path.GetFullPath("fifa_pc/external-animation-timing-audit.tsv");
    var externalTimingSummaryPath = Path.GetFullPath("fifa_pc/external-animation-timing-audit-summary.tsv");
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: external-animation-timing-audit ... --report <saida.tsv>");
                return 2;
            }

            externalTimingReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: external-animation-timing-audit ... --summary <saida.tsv>");
                return 2;
            }

            externalTimingSummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine("Uso: external-animation-timing-audit <observacoes.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
            return 2;
        }
    }

    var externalTimingAudit = ExternalAnimationTimingEvidenceAudit.Audit(externalTimingInputPath);
    ExternalAnimationTimingEvidenceAudit.WriteReport(externalTimingReportPath, externalTimingAudit.Rows);
    ExternalAnimationTimingEvidenceAudit.WriteSummary(externalTimingSummaryPath, externalTimingAudit.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC external animation timing evidence audit");
    Console.WriteLine($"status: {externalTimingAudit.Summary.Status}");
    Console.WriteLine($"observation_rows: {externalTimingAudit.Summary.ObservationRows}");
    Console.WriteLine($"valid_rows: {externalTimingAudit.Summary.ValidRows}");
    Console.WriteLine($"invalid_rows: {externalTimingAudit.Summary.InvalidRows}");
    Console.WriteLine($"distinct_capture_ids: {externalTimingAudit.Summary.DistinctCaptureIds}");
    Console.WriteLine($"bank_asset_pairs: {externalTimingAudit.Summary.BankAssetPairs}");
    Console.WriteLine($"pairs_with_multiple_captures: {externalTimingAudit.Summary.PairsWithMultipleCaptures}");
    Console.WriteLine($"evidence_file_sha256: {externalTimingAudit.Summary.EvidenceFileSha256}");
    Console.WriteLine($"direct_timing_binding: {externalTimingAudit.Summary.DirectTimingBinding}");
    Console.WriteLine($"original_semantics: {externalTimingAudit.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {Path.GetFullPath(externalTimingReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(externalTimingSummaryPath)}");
    Console.WriteLine("status_note: external observations are provenance-checked only; no frame, pose, rig, skinning, morph, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: evidence TSV only, no ISO access");
    return externalTimingAudit.Summary.Status == "invalid" ? 1 : 0;
}

if (string.Equals(args[0], "animation-bank-catalog-compare", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-catalog-compare <caminho-do-iso> [--profile-report <saida.tsv>] [--pair-report <saida.tsv>]");
        return 2;
    }

    string? catalogProfileReportPath = null;
    string? catalogPairReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--profile-report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-catalog-compare ... --profile-report <saida.tsv>");
                return 2;
            }

            catalogProfileReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--pair-report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-catalog-compare ... --pair-report <saida.tsv>");
                return 2;
            }

            catalogPairReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var catalogComparisonInputPath = Path.GetFullPath(args[1]);
    if (string.Equals(catalogProfileReportPath, catalogComparisonInputPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(catalogPairReportPath, catalogComparisonInputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios de catálogo não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    using var catalogComparisonFileSystem = new XdvdfsFileSystem(args[1]);
    var catalogComparisonCatalog = GameAssetCatalog.Scan(catalogComparisonFileSystem);
    var catalogComparisonBanks = catalogComparisonCatalog.AnimationBanks
        .Where(bank => bank.BankName.Length > 0 && bank.CatalogFiles.Count > 0)
        .Select(bank => (
            CatalogEntry: bank.CatalogEntry,
            Catalog: new AnimationBankCatalog(
                bank.VersionTag,
                bank.BankName,
                bank.Directory,
                bank.DeclaredFileCount,
                bank.CatalogFiles)))
        .ToArray();
    var catalogProfiles = AnimationBankCatalogComparison.BuildProfiles(catalogComparisonBanks);
    var catalogPairs = AnimationBankCatalogComparison.BuildPairs(catalogComparisonBanks);
    if (catalogProfileReportPath is not null)
    {
        AnimationBankCatalogComparison.WriteProfiles(catalogProfileReportPath, catalogProfiles);
    }

    if (catalogPairReportPath is not null)
    {
        AnimationBankCatalogComparison.WritePairs(catalogPairReportPath, catalogPairs);
    }

    var catalogNameBankCounts = catalogComparisonBanks
        .SelectMany(item => item.Catalog.Files.Select(file => (item.Catalog.BankName, file.Name)))
        .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.Select(item => item.BankName).Distinct(StringComparer.OrdinalIgnoreCase).Count())
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC animation bank catalog-only comparison");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"catalog_banks: {catalogComparisonBanks.Length}");
    Console.WriteLine($"catalog_file_entries: {catalogComparisonBanks.Sum(item => item.Catalog.Files.Count)}");
    Console.WriteLine($"unique_catalog_file_names: {catalogComparisonBanks.SelectMany(item => item.Catalog.Files).Select(file => file.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
    Console.WriteLine($"names_in_multiple_banks: {catalogNameBankCounts.Count(count => count > 1)}");
    Console.WriteLine($"profiles_with_duplicate_names: {catalogProfiles.Count(profile => profile.DuplicateFileNameOccurrences > 0)}");
    Console.WriteLine($"pair_comparisons: {catalogPairs.Count}");
    Console.WriteLine($"exact_distinct_name_set_pairs: {catalogPairs.Count(pair => pair.ExactDistinctFileNameSets)}");
    Console.WriteLine($"maximum_shared_file_name_count: {(catalogPairs.Count == 0 ? 0 : catalogPairs.Max(pair => pair.SharedFileNameCount))}");
    Console.WriteLine($"maximum_same_position_file_name_count: {(catalogPairs.Count == 0 ? 0 : catalogPairs.Max(pair => pair.SamePositionFileNameCount))}");
    Console.WriteLine($"maximum_common_ordered_name_prefix_count: {(catalogPairs.Count == 0 ? 0 : catalogPairs.Max(pair => pair.CommonOrderedNamePrefixCount))}");
    Console.WriteLine($"total_shared_name_timestamp_exact_occurrences: {catalogPairs.Sum(pair => pair.SharedNameTimestampExactOccurrenceCount)}");
    Console.WriteLine($"total_shared_name_timestamp_different_occurrences: {catalogPairs.Sum(pair => pair.SharedNameTimestampDifferentOccurrenceCount)}");
    if (catalogProfileReportPath is not null)
    {
        Console.WriteLine($"profile_report: {catalogProfileReportPath} | rows={catalogProfiles.Count}");
    }

    if (catalogPairReportPath is not null)
    {
        Console.WriteLine($"pair_report: {catalogPairReportPath} | rows={catalogPairs.Count}");
    }

    Console.WriteLine("status: exact .axt catalog names, timestamps, order, and overlap metrics only; no frame, pose, rig, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, runtime catalog, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "animation-bank-elf-structure", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-elf-structure <caminho-do-iso> [--profile-report <saida.tsv>] [--section-report <saida.tsv>]");
        return 2;
    }

    string? animationElfProfileReportPath = null;
    string? animationElfSectionReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--profile-report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-elf-structure ... --profile-report <saida.tsv>");
                return 2;
            }

            animationElfProfileReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--section-report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-elf-structure ... --section-report <saida.tsv>");
                return 2;
            }

            animationElfSectionReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var animationElfInputPath = Path.GetFullPath(args[1]);
    if (string.Equals(animationElfProfileReportPath, animationElfInputPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(animationElfSectionReportPath, animationElfInputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios ELF não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    using var animationElfStore = new GameAssetStore(args[1]);
    using var animationElfArchiveStream = animationElfStore.OpenRaw("data/anims.big");
    var animationElfArchive = BigArchiveReader.Inspect(animationElfArchiveStream);
    var animationElfCatalogEntries = animationElfArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationElfOrdEntries = animationElfArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationElfOrlEntries = animationElfArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationElfProfiles = new List<AnimationBankElfStructureProfile>();
    var animationElfSections = new List<AnimationBankElfSectionRow>();
    foreach (var catalogEntry in animationElfCatalogEntries)
    {
        using var catalogStream = animationElfStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            continue;
        }

        var ordEntry = animationElfOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationElfOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            animationElfProfiles.Add(AnimationBankElfStructureReporter.CreateInvalidProfile(
                catalogEntry.Name,
                animationCatalog.BankName,
                animationCatalog.Files.Count));
            continue;
        }

        try
        {
            using var ordStream = animationElfStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationElfStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var inspection = ElfReader.Inspect(EaglObjectReader.Combine(ordData, orlData));
            var report = AnimationBankElfStructureReporter.Build(catalogEntry.Name, animationCatalog, inspection);
            animationElfProfiles.Add(report.Profile);
            animationElfSections.AddRange(report.Sections);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            animationElfProfiles.Add(AnimationBankElfStructureReporter.CreateInvalidProfile(
                catalogEntry.Name,
                animationCatalog.BankName,
                animationCatalog.Files.Count));
        }
    }

    if (animationElfProfileReportPath is not null)
    {
        AnimationBankElfStructureReporter.WriteProfiles(animationElfProfileReportPath, animationElfProfiles);
    }

    if (animationElfSectionReportPath is not null)
    {
        AnimationBankElfStructureReporter.WriteSections(animationElfSectionReportPath, animationElfSections);
    }

    var validAnimationElfProfiles = animationElfProfiles.Where(profile => profile.ElfValid).ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC animation bank ELF section/relocation structure");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"catalog_banks: {animationElfProfiles.Count}");
    Console.WriteLine($"elf_validated: {validAnimationElfProfiles.Length}");
    Console.WriteLine($"section_rows: {animationElfSections.Count}");
    Console.WriteLine($"distinct_section_profile_sha256: {validAnimationElfProfiles.Select(profile => profile.SectionProfileSha256).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"distinct_relocation_profile_sha256: {validAnimationElfProfiles.Select(profile => profile.RelocationProfileSha256).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"distinct_section_counts: {validAnimationElfProfiles.Select(profile => profile.SectionCount).Distinct().Count()}");
    Console.WriteLine($"distinct_catalog_file_counts: {validAnimationElfProfiles.Select(profile => profile.CatalogFileCount).Distinct().Count()}");
    Console.WriteLine($"distinct_data_sizes: {validAnimationElfProfiles.Select(profile => profile.DataBytes).Distinct().Count()}");
    Console.WriteLine($"banks_with_animation_symbol_metadata: {validAnimationElfProfiles.Count(profile => profile.AnimationBankSymbolMetadata.Length > 0)}");
    Console.WriteLine($"banks_with_symbol_tail_distance: {validAnimationElfProfiles.Count(profile => profile.AnimationBankSymbolTailDistance.Length > 0)}");
    if (animationElfProfileReportPath is not null)
    {
        Console.WriteLine($"profile_report: {animationElfProfileReportPath} | rows={animationElfProfiles.Count}");
    }

    if (animationElfSectionReportPath is not null)
    {
        Console.WriteLine($"section_report: {animationElfSectionReportPath} | rows={animationElfSections.Count}");
    }

    Console.WriteLine("status: ELF section headers, sizes, symbol metadata and relocation-target layout only; no payload, frame, pose, rig, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection");
    return validAnimationElfProfiles.Length == animationElfProfiles.Count ? 0 : 1;
}

if (string.Equals(args[0], "animation-bank-data-relocation-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-data-relocation-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? animationDataRelocationReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (!string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase) ||
            optionIndex + 1 >= args.Length ||
            args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-data-relocation-audit <caminho-do-iso> [--report <saida.tsv>]");
            return 2;
        }

        animationDataRelocationReportPath = Path.GetFullPath(args[++optionIndex]);
    }

    var animationDataRelocationInputPath = Path.GetFullPath(args[1]);
    if (string.Equals(animationDataRelocationReportPath, animationDataRelocationInputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de relocations não pode sobrescrever a ISO de entrada.");
        return 2;
    }

    using var animationDataRelocationStore = new GameAssetStore(args[1]);
    using var animationDataRelocationArchiveStream = animationDataRelocationStore.OpenRaw("data/anims.big");
    var animationDataRelocationArchive = BigArchiveReader.Inspect(animationDataRelocationArchiveStream);
    var animationDataRelocationCatalogEntries = animationDataRelocationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationDataRelocationOrdEntries = animationDataRelocationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationDataRelocationOrlEntries = animationDataRelocationArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationDataRelocationRows = new List<AnimationBankDataRelocationAuditRow>();
    foreach (var catalogEntry in animationDataRelocationCatalogEntries)
    {
        using var catalogStream = animationDataRelocationStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            animationDataRelocationRows.Add(
                AnimationBankDataRelocationAudit.CreateInvalid(catalogEntry.Name, string.Empty, 0));
            continue;
        }

        var ordEntry = animationDataRelocationOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationDataRelocationOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            animationDataRelocationRows.Add(AnimationBankDataRelocationAudit.CreateInvalid(
                catalogEntry.Name,
                animationCatalog.BankName,
                animationCatalog.Files.Count));
            continue;
        }

        try
        {
            using var ordStream = animationDataRelocationStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationDataRelocationStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var inspection = ElfReader.Inspect(EaglObjectReader.Combine(ordData, orlData));
            animationDataRelocationRows.Add(AnimationBankDataRelocationAudit.Build(
                catalogEntry.Name,
                animationCatalog,
                inspection));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            animationDataRelocationRows.Add(AnimationBankDataRelocationAudit.CreateInvalid(
                catalogEntry.Name,
                animationCatalog.BankName,
                animationCatalog.Files.Count));
        }
    }

    if (animationDataRelocationReportPath is not null)
    {
        AnimationBankDataRelocationAudit.WriteReport(animationDataRelocationReportPath, animationDataRelocationRows);
    }

    var validAnimationDataRelocationRows = animationDataRelocationRows
        .Where(row => row.ElfValid)
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC animation bank .data relocation offset/gap audit");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"catalog_banks: {animationDataRelocationRows.Count}");
    Console.WriteLine($"elf_validated: {validAnimationDataRelocationRows.Length}");
    Console.WriteLine($"data_relocation_rows: {validAnimationDataRelocationRows.Sum(row => row.DataRelocationCount)}");
    Console.WriteLine($"banks_with_data_relocations: {validAnimationDataRelocationRows.Count(row => row.DataRelocationCount > 0)}");
    Console.WriteLine($"distinct_data_sizes: {validAnimationDataRelocationRows.Select(row => row.DataSize).Distinct().Count()}");
    Console.WriteLine($"distinct_data_relocation_counts: {validAnimationDataRelocationRows.Select(row => row.DataRelocationCount).Distinct().Count()}");
    Console.WriteLine($"distinct_delta_counts: {validAnimationDataRelocationRows.Select(row => row.DistinctDeltaCount).Distinct().Count()}");
    Console.WriteLine($"maximum_distinct_relative_offsets: {(validAnimationDataRelocationRows.Length == 0 ? 0 : validAnimationDataRelocationRows.Max(row => row.DistinctRelativeOffsetCount))}");
    Console.WriteLine($"maximum_distinct_deltas: {(validAnimationDataRelocationRows.Length == 0 ? 0 : validAnimationDataRelocationRows.Max(row => row.DistinctDeltaCount))}");
    Console.WriteLine($"maximum_repeated_delta_values: {(validAnimationDataRelocationRows.Length == 0 ? 0 : validAnimationDataRelocationRows.Max(row => row.RepeatedDeltaCount))}");
    if (animationDataRelocationReportPath is not null)
    {
        Console.WriteLine($"report: {animationDataRelocationReportPath} | rows={animationDataRelocationRows.Count}");
    }

    Console.WriteLine("status: .data relocation offsets, adjacent gaps, and structural hashes only; no payload, frame, pose, rig, timing, skinning, material, UV, GPU, or command semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection, TSV diagnostics");
    return validAnimationDataRelocationRows.Length == animationDataRelocationRows.Count ? 0 : 1;
}

if (string.Equals(args[0], "animation-morph-relocation-profile-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: animation-morph-relocation-profile-audit <morph-catalog.tsv> <animation-relocations.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var morphRelocationProfileMorphPath = Path.GetFullPath(args[1]);
    var morphRelocationProfileAnimationPath = Path.GetFullPath(args[2]);
    var morphRelocationProfileReportPath = Path.GetFullPath(args[3]);
    var morphRelocationProfileSummaryPath = Path.Combine(
        Path.GetDirectoryName(morphRelocationProfileReportPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(morphRelocationProfileReportPath) + "-summary.tsv");
    for (var optionIndex = 4; optionIndex < args.Length; optionIndex++)
    {
        if (!string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase) ||
            optionIndex + 1 >= args.Length ||
            args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-morph-relocation-profile-audit <morph-catalog.tsv> <animation-relocations.tsv> <audit.tsv> [--summary <summary.tsv>]");
            return 2;
        }

        morphRelocationProfileSummaryPath = Path.GetFullPath(args[++optionIndex]);
    }

    if (new[]
        {
            morphRelocationProfileMorphPath,
            morphRelocationProfileAnimationPath,
        }.Contains(morphRelocationProfileReportPath, StringComparer.OrdinalIgnoreCase) ||
        new[]
        {
            morphRelocationProfileMorphPath,
            morphRelocationProfileAnimationPath,
            morphRelocationProfileReportPath,
        }.Contains(morphRelocationProfileSummaryPath, StringComparer.OrdinalIgnoreCase) ||
        string.Equals(morphRelocationProfileReportPath, morphRelocationProfileSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios de perfil não podem sobrescrever um arquivo de entrada ou um ao outro.");
        return 2;
    }

    var morphRelocationProfileMorphRows = AnimationMorphRelocationProfileAudit.ReadMorphCatalog(morphRelocationProfileMorphPath);
    var morphRelocationProfileAnimationRows = AnimationMorphRelocationProfileAudit.ReadAnimationReport(morphRelocationProfileAnimationPath);
    var morphRelocationProfileAuditRows = AnimationMorphRelocationProfileAudit.Audit(
        morphRelocationProfileMorphRows,
        morphRelocationProfileAnimationRows);
    AnimationMorphRelocationProfileAudit.WriteReports(
        morphRelocationProfileReportPath,
        morphRelocationProfileSummaryPath,
        morphRelocationProfileAuditRows,
        morphRelocationProfileAnimationRows.Count(row => row.ElfValid));
    var morphRelocationProfileSummary = AnimationMorphRelocationProfileAudit.Summarize(
        morphRelocationProfileAuditRows,
        morphRelocationProfileAnimationRows.Count(row => row.ElfValid));

    Console.WriteLine($"{GameIdentity.Name} PC morph/animation relocation-gap profile overlap audit");
    Console.WriteLine($"morph_catalog_rows: {morphRelocationProfileSummary.MorphCatalogRows}");
    Console.WriteLine($"morph_layouts: {morphRelocationProfileSummary.MorphLayouts}");
    Console.WriteLine($"parse_valid_layouts: {morphRelocationProfileSummary.ParseValidLayouts}");
    Console.WriteLine($"invalid_layouts: {morphRelocationProfileSummary.InvalidLayouts}");
    Console.WriteLine($"animation_banks: {morphRelocationProfileSummary.AnimationBanks}");
    Console.WriteLine($"layouts_with_containing_animation_bank: {morphRelocationProfileSummary.LayoutsWithContainingAnimationBank}");
    Console.WriteLine($"layouts_with_exact_gap_set_bank: {morphRelocationProfileSummary.LayoutsWithExactGapSetBank}");
    Console.WriteLine($"containing_pairs: {morphRelocationProfileSummary.ContainingPairs}");
    Console.WriteLine($"exact_gap_set_pairs: {morphRelocationProfileSummary.ExactGapSetPairs}");
    Console.WriteLine($"report: {morphRelocationProfileReportPath}");
    Console.WriteLine($"summary: {morphRelocationProfileSummaryPath}");
    Console.WriteLine("status: opaque relocation-gap overlap control only; no payload, frame, pose, rig, timing, skinning, material, UV, GPU, command, or animation semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return morphRelocationProfileSummary.InvalidLayouts == 0 &&
           morphRelocationProfileSummary.AnimationBanks > 0
        ? 0
        : 1;
}

if (string.Equals(args[0], "animation-bank-tail-compare", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-tail-compare <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? animationTailReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (!string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase) ||
            optionIndex + 1 >= args.Length ||
            args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-tail-compare <caminho-do-iso> [--report <saida.tsv>]");
            return 2;
        }

        animationTailReportPath = Path.GetFullPath(args[++optionIndex]);
    }

    var animationTailInputPath = Path.GetFullPath(args[1]);
    if (string.Equals(animationTailReportPath, animationTailInputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de cauda não pode sobrescrever a ISO de entrada.");
        return 2;
    }

    using var animationTailStore = new GameAssetStore(args[1]);
    using var animationTailArchiveStream = animationTailStore.OpenRaw("data/anims.big");
    var animationTailArchive = BigArchiveReader.Inspect(animationTailArchiveStream);
    var animationTailCatalogEntries = animationTailArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailOrdEntries = animationTailArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailOrlEntries = animationTailArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailObservations = new List<AnimationBankTailObservation>();
    foreach (var catalogEntry in animationTailCatalogEntries)
    {
        using var catalogStream = animationTailStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            continue;
        }

        var ordEntry = animationTailOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationTailOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            continue;
        }

        try
        {
            using var ordStream = animationTailStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationTailStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var inspection = ElfReader.Inspect(combinedData);
            animationTailObservations.Add(AnimationBankTailComparison.Build(
                catalogEntry.Name,
                animationCatalog,
                combinedData,
                inspection));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            // Keep this report limited to independently bounded ELF/data observations.
        }
    }

    var animationTailSummary = AnimationBankTailComparison.Summarize(animationTailObservations);
    if (animationTailReportPath is not null)
    {
        AnimationBankTailComparison.WriteReport(animationTailReportPath, animationTailObservations);
    }

    Console.WriteLine($"{GameIdentity.Name} PC animation bank symbol-anchored data-tail comparison");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"validated_tail_observations: {animationTailSummary.ObservationCount}");
    Console.WriteLine($"minimum_tail_byte_count: {animationTailSummary.MinimumTailByteCount}");
    Console.WriteLine($"exact_raw_tail_pairs: {animationTailSummary.ExactRawTailPairs}");
    Console.WriteLine($"exact_normalized_tail_pairs: {animationTailSummary.ExactNormalizedTailPairs}");
    Console.WriteLine($"distinct_raw_tail_hashes: {animationTailSummary.DistinctRawTailHashes}");
    Console.WriteLine($"distinct_normalized_tail_hashes: {animationTailSummary.DistinctNormalizedTailHashes}");
    Console.WriteLine($"common_raw_offset_count: {animationTailSummary.CommonRawOffsetCount}");
    Console.WriteLine($"common_normalized_offset_count: {animationTailSummary.CommonNormalizedOffsetCount}");
    Console.WriteLine($"common_raw_prefix_byte_count: {animationTailSummary.CommonRawPrefixByteCount}");
    Console.WriteLine($"common_normalized_prefix_byte_count: {animationTailSummary.CommonNormalizedPrefixByteCount}");
    Console.WriteLine($"declared_file_count_match_count: {animationTailSummary.DeclaredFileCountMatchCount}");
    Console.WriteLine($"catalog_file_count_times_two_match_count: {animationTailSummary.CatalogFileCountTimesTwoMatchCount}");
    Console.WriteLine($"declared_file_count_times_two_match_count: {animationTailSummary.DeclaredFileCountTimesTwoMatchCount}");
    Console.WriteLine($"common_raw_offset_values: {animationTailSummary.CommonRawOffsetValues}");
    Console.WriteLine($"common_normalized_offset_values: {animationTailSummary.CommonNormalizedOffsetValues}");
    if (animationTailReportPath is not null)
    {
        Console.WriteLine($"report: {animationTailReportPath} | rows={animationTailObservations.Count}");
    }

    Console.WriteLine("status: symbol-anchored raw and relocation-masked data-tail bytes only; no header, record, frame, pose, rig, timing, skinning, material, UV, or GPU semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection");
    return animationTailObservations.Count == animationTailCatalogEntries.Length ? 0 : 1;
}

if (string.Equals(args[0], "animation-bank-catalog-tail-range-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-catalog-tail-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var animationTailRangeReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "animation-bank-catalog-tail-range-audit.tsv");
    var animationTailRangeSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "animation-bank-catalog-tail-range-audit-summary.tsv");
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-catalog-tail-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
            return 2;
        }

        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            animationTailRangeReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            animationTailRangeSummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var animationTailRangeInputPath = Path.GetFullPath(args[1]);
    if (new[] { animationTailRangeReportPath, animationTailRangeSummaryPath }
        .Any(path => string.Equals(path, animationTailRangeInputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios de intervalo de cauda não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    if (string.Equals(animationTailRangeReportPath, animationTailRangeSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório e o resumo de intervalo de cauda devem ser arquivos diferentes.");
        return 2;
    }

    using var animationTailRangeStore = new GameAssetStore(animationTailRangeInputPath);
    using var animationTailRangeArchiveStream = animationTailRangeStore.OpenRaw("data/anims.big");
    var animationTailRangeArchive = BigArchiveReader.Inspect(animationTailRangeArchiveStream);
    var animationTailRangeCatalogEntries = animationTailRangeArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailRangeOrdEntries = animationTailRangeArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailRangeOrlEntries = animationTailRangeArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationTailRangeObservations = new List<AnimationBankCatalogTailRangeObservation>();
    foreach (var catalogEntry in animationTailRangeCatalogEntries)
    {
        AnimationBankCatalog animationCatalog;
        try
        {
            using var catalogStream = animationTailRangeStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            animationTailRangeObservations.Add(
                AnimationBankCatalogTailRangeAudit.CreateInvalid(catalogEntry.Name, string.Empty, 0));
            continue;
        }

        var ordEntry = animationTailRangeOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationTailRangeOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            animationTailRangeObservations.Add(
                AnimationBankCatalogTailRangeAudit.CreateInvalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
            continue;
        }

        try
        {
            using var ordStream = animationTailRangeStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationTailRangeStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var inspection = ElfReader.Inspect(combinedData);
            animationTailRangeObservations.Add(AnimationBankCatalogTailRangeAudit.Build(
                catalogEntry.Name,
                animationCatalog,
                combinedData,
                inspection));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            animationTailRangeObservations.Add(
                AnimationBankCatalogTailRangeAudit.CreateInvalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
        }
    }

    var animationTailRangeSummary = AnimationBankCatalogTailRangeAudit.Summarize(animationTailRangeObservations);
    AnimationBankCatalogTailRangeAudit.WriteReport(animationTailRangeReportPath, animationTailRangeObservations);
    AnimationBankCatalogTailRangeAudit.WriteSummary(animationTailRangeSummaryPath, animationTailRangeSummary);

    Console.WriteLine($"{GameIdentity.Name} PC animation bank catalog tail-range structural audit");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"catalog_banks: {animationTailRangeSummary.CatalogBanks}");
    Console.WriteLine($"validated_banks: {animationTailRangeSummary.ValidatedBanks}");
    Console.WriteLine($"catalog_rows: {animationTailRangeSummary.CatalogRows}");
    Console.WriteLine($"tail_header_shape_valid_rows: {animationTailRangeSummary.TailHeaderShapeValidRows}");
    Console.WriteLine($"tail_entry_count_match_rows: {animationTailRangeSummary.TailEntryCountMatchRows}");
    Console.WriteLine($"tail_range_within_data_rows: {animationTailRangeSummary.TailRangeWithinDataRows}");
    Console.WriteLine($"tail_range_aligned_rows: {animationTailRangeSummary.TailRangeAlignedRows}");
    Console.WriteLine($"tail_range_length_match_rows: {animationTailRangeSummary.TailRangeLengthMatchRows}");
    Console.WriteLine($"tail_range_relocation_count_match_rows: {animationTailRangeSummary.TailRangeRelocationCountMatchRows}");
    Console.WriteLine($"rows_with_no_relocations_in_tail_range: {animationTailRangeSummary.RowsWithNoRelocationsInTailRange}");
    Console.WriteLine($"verified_structural_rows: {animationTailRangeSummary.VerifiedStructuralRows}");
    Console.WriteLine($"report: {Path.GetFullPath(animationTailRangeReportPath)} | rows={animationTailRangeObservations.Count}");
    Console.WriteLine($"summary: {Path.GetFullPath(animationTailRangeSummaryPath)}");
    Console.WriteLine($"status: {animationTailRangeSummary.Status}");
    Console.WriteLine($"candidate_record_binding: {animationTailRangeSummary.CandidateRecordBinding}");
    Console.WriteLine($"original_semantics: {animationTailRangeSummary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF .data inspection; no record/frame/pose/rig/timing semantics assigned");
    return animationTailRangeSummary.VerifiedStructuralRows == animationTailRangeSummary.CatalogBanks ? 0 : 1;
}

if (string.Equals(args[0], "animation-bank-tail-pair-relocation-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-tail-pair-relocation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--detail-report <saida.tsv>]");
        return 2;
    }

    var animationPairReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "animation-bank-tail-pair-relocation-audit.tsv");
    var animationPairSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "animation-bank-tail-pair-relocation-audit-summary.tsv");
    string? animationPairDetailReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: animation-bank-tail-pair-relocation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--detail-report <saida.tsv>]");
            return 2;
        }

        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            animationPairReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            animationPairSummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--detail-report", StringComparison.OrdinalIgnoreCase))
        {
            animationPairDetailReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var animationPairTargetChainReportPath = Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(animationPairReportPath)) ?? Directory.GetCurrentDirectory(),
        "animation-bank-tail-target-chain-audit.tsv");
    var animationPairTargetChainSummaryPath = Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(animationPairSummaryPath)) ?? Directory.GetCurrentDirectory(),
        "animation-bank-tail-target-chain-audit-summary.tsv");
    var animationPairTargetChainDetailReportPath = Path.Combine(
        Path.GetDirectoryName(Path.GetFullPath(animationPairSummaryPath)) ?? Directory.GetCurrentDirectory(),
        "animation-bank-tail-target-chain-detail.tsv");
    var animationPairInputPath = Path.GetFullPath(args[1]);
    if (new[] { animationPairReportPath, animationPairSummaryPath, animationPairDetailReportPath }
        .Where(path => path is not null)
        .Any(path => string.Equals(path, animationPairInputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios de pares não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    if (string.Equals(animationPairReportPath, animationPairSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório e o resumo de pares devem ser arquivos diferentes.");
        return 2;
    }

    if (animationPairDetailReportPath is not null &&
        new[] { animationPairReportPath, animationPairSummaryPath }
            .Any(path => string.Equals(path, animationPairDetailReportPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O relatório detalhado deve ser um arquivo diferente do relatório e do resumo.");
        return 2;
    }

    using var animationPairStore = new GameAssetStore(animationPairInputPath);
    using var animationPairArchiveStream = animationPairStore.OpenRaw("data/anims.big");
    var animationPairArchive = BigArchiveReader.Inspect(animationPairArchiveStream);
    var animationPairCatalogEntries = animationPairArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPairOrdEntries = animationPairArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPairOrlEntries = animationPairArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationPairObservations = new List<AnimationBankTailPairRelocationObservation>();
    var animationPairDetails = new List<AnimationBankTailPairRelocationDetail>();
    var animationPairTargetChainObservations = new List<AnimationBankTailPairTargetChainObservation>();
    var animationPairTargetChainDetails = new List<AnimationBankTailPairTargetChainDetail>();
    foreach (var catalogEntry in animationPairCatalogEntries)
    {
        AnimationBankCatalog animationCatalog;
        try
        {
            using var catalogStream = animationPairStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            animationPairObservations.Add(
                AnimationBankTailPairRelocationAudit.Invalid(catalogEntry.Name, string.Empty, 0));
            animationPairTargetChainObservations.Add(
                AnimationBankTailPairTargetChainAudit.Invalid(catalogEntry.Name, string.Empty, 0));
            continue;
        }

        var ordEntry = animationPairOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationPairOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            animationPairObservations.Add(
                AnimationBankTailPairRelocationAudit.Invalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
            animationPairTargetChainObservations.Add(
                AnimationBankTailPairTargetChainAudit.Invalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
            continue;
        }

        try
        {
            using var ordStream = animationPairStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationPairStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var inspection = ElfReader.Inspect(combinedData);
            animationPairObservations.Add(AnimationBankTailPairRelocationAudit.Build(
                catalogEntry.Name,
                animationCatalog,
                combinedData,
                inspection));
            animationPairTargetChainObservations.Add(AnimationBankTailPairTargetChainAudit.Build(
                catalogEntry.Name,
                animationCatalog,
                combinedData,
                inspection));
            if (animationPairDetailReportPath is not null)
            {
                animationPairDetails.AddRange(AnimationBankTailPairRelocationAudit.BuildDetails(
                    catalogEntry.Name,
                    animationCatalog,
                    combinedData,
                    inspection));
                animationPairTargetChainDetails.AddRange(AnimationBankTailPairTargetChainAudit.BuildDetails(
                    catalogEntry.Name,
                    animationCatalog,
                    combinedData,
                    inspection));
            }
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            animationPairObservations.Add(
                AnimationBankTailPairRelocationAudit.Invalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
            animationPairTargetChainObservations.Add(
                AnimationBankTailPairTargetChainAudit.Invalid(
                    catalogEntry.Name,
                    animationCatalog.BankName,
                    animationCatalog.Files.Count));
        }
    }

    var animationPairSummary = AnimationBankTailPairRelocationAudit.Summarize(animationPairObservations);
    var animationPairTargetChainSummary = AnimationBankTailPairTargetChainAudit.Summarize(animationPairTargetChainObservations);
    AnimationBankTailPairRelocationAudit.WriteReport(animationPairReportPath, animationPairObservations);
    AnimationBankTailPairRelocationAudit.WriteSummary(animationPairSummaryPath, animationPairSummary);
    AnimationBankTailPairTargetChainAudit.WriteReport(animationPairTargetChainReportPath, animationPairTargetChainObservations);
    AnimationBankTailPairTargetChainAudit.WriteSummary(animationPairTargetChainSummaryPath, animationPairTargetChainSummary);
    if (animationPairDetailReportPath is not null)
    {
        AnimationBankTailPairRelocationAudit.WriteDetails(animationPairDetailReportPath, animationPairDetails);
        AnimationBankTailPairTargetChainAudit.WriteDetails(animationPairTargetChainDetailReportPath, animationPairTargetChainDetails);
    }

    Console.WriteLine($"{GameIdentity.Name} PC animation bank tail pair-relocation structural audit");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"catalog_banks: {animationPairSummary.CatalogBanks}");
    Console.WriteLine($"validated_banks: {animationPairSummary.ValidatedBanks}");
    Console.WriteLine($"catalog_rows: {animationPairSummary.CatalogRows}");
    Console.WriteLine($"pair_ordinal_rows: {animationPairSummary.PairOrdinalRows}");
    Console.WriteLine($"pair_rows_with_two_relocations: {animationPairSummary.PairRowsWithTwoRelocations}");
    Console.WriteLine($"pair_rows_with_expected_offsets: {animationPairSummary.PairRowsWithExpectedOffsets}");
    Console.WriteLine($"rows_with_unexpected_relocation_multiplicity: {animationPairSummary.RowsWithUnexpectedRelocationMultiplicity}");
    Console.WriteLine($"banks_with_complete_pair_layout: {animationPairSummary.BanksWithCompletePairLayout}");
    Console.WriteLine($"distinct_pair_symbol_signatures: {animationPairSummary.DistinctPairSymbolSignatures}");
    Console.WriteLine($"report: {Path.GetFullPath(animationPairReportPath)} | rows={animationPairObservations.Count}");
    Console.WriteLine($"summary: {Path.GetFullPath(animationPairSummaryPath)}");
    if (animationPairDetailReportPath is not null)
    {
        Console.WriteLine($"detail_report: {Path.GetFullPath(animationPairDetailReportPath)} | rows={animationPairDetails.Count}");
    }
    Console.WriteLine($"status: {animationPairSummary.Status}");
    Console.WriteLine($"candidate_record_binding: {animationPairSummary.CandidateRecordBinding}");
    Console.WriteLine($"original_semantics: {animationPairSummary.OriginalSemantics}");
    Console.WriteLine($"target_chain_report: {Path.GetFullPath(animationPairTargetChainReportPath)} | rows={animationPairTargetChainObservations.Count}");
    Console.WriteLine($"target_chain_summary: {Path.GetFullPath(animationPairTargetChainSummaryPath)}");
    if (animationPairDetailReportPath is not null)
    {
        Console.WriteLine($"target_chain_detail_report: {Path.GetFullPath(animationPairTargetChainDetailReportPath)} | rows={animationPairTargetChainDetails.Count}");
    }
    Console.WriteLine($"target_chain_first_rows_with_one_relocation: {animationPairTargetChainSummary.FirstTargetRowsWithOneRelocation}");
    Console.WriteLine($"target_chain_second_rows_with_one_relocation: {animationPairTargetChainSummary.SecondTargetRowsWithOneRelocation}");
    Console.WriteLine($"target_chain_rows_with_complete_chain: {animationPairTargetChainSummary.RowsWithCompleteTargetChain}");
    Console.WriteLine($"target_chain_first_words_at_known_symbols: {animationPairTargetChainSummary.FirstSecondLevelWordsAtKnownSymbols}");
    Console.WriteLine($"target_chain_second_words_at_known_symbols: {animationPairTargetChainSummary.SecondSecondLevelWordsAtKnownSymbols}");
    Console.WriteLine($"target_chain_status: {animationPairTargetChainSummary.Status}");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF relocation inspection; ordinal pair signatures remain opaque");
    return animationPairSummary.BanksWithCompletePairLayout == animationPairSummary.CatalogBanks &&
           animationPairTargetChainSummary.BanksWithCompleteTargetChain == animationPairTargetChainSummary.CatalogBanks
        ? 0
        : 1;
}

if (string.Equals(args[0], "animation-bank-sidecar-boundary-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: animation-bank-sidecar-boundary-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? animationBoundaryReportPath = null;
    string? animationBoundarySummaryPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-sidecar-boundary-audit ... --report <saida.tsv>");
                return 2;
            }

            animationBoundaryReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: animation-bank-sidecar-boundary-audit ... --summary <saida.tsv>");
                return 2;
            }

            animationBoundarySummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var animationBoundaryInputPath = Path.GetFullPath(args[1]);
    if (new[] { animationBoundaryReportPath, animationBoundarySummaryPath }
        .Where(path => path is not null)
        .Any(path => string.Equals(path, animationBoundaryInputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios de fronteira não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    if (animationBoundaryReportPath is not null &&
        animationBoundarySummaryPath is not null &&
        string.Equals(animationBoundaryReportPath, animationBoundarySummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório e o resumo de fronteira devem ser arquivos diferentes.");
        return 2;
    }

    using var animationBoundaryStore = new GameAssetStore(args[1]);
    using var animationBoundaryArchiveStream = animationBoundaryStore.OpenRaw("data/anims.big");
    var animationBoundaryArchive = BigArchiveReader.Inspect(animationBoundaryArchiveStream);
    var animationBoundaryCatalogEntries = animationBoundaryArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".axt", StringComparison.OrdinalIgnoreCase))
        .Where(entry => !string.Equals(entry.Name, "banklist.axt", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationBoundaryOrdEntries = animationBoundaryArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationBoundaryOrlEntries = animationBoundaryArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".orl", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var animationBoundaryObservations = new List<AnimationBankSidecarBoundaryObservation>();
    foreach (var catalogEntry in animationBoundaryCatalogEntries)
    {
        using var catalogStream = animationBoundaryStore.OpenBigEntry("data/anims.big", catalogEntry.Name);
        AnimationBankCatalog animationCatalog;
        try
        {
            animationCatalog = AnimationBankReader.Parse(ReadBounded(catalogStream, 4 * 1024 * 1024));
        }
        catch (InvalidDataException)
        {
            continue;
        }

        var ordEntry = animationBoundaryOrdEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        var orlEntry = animationBoundaryOrlEntries.FirstOrDefault(entry =>
            string.Equals(Path.GetFileNameWithoutExtension(entry.Name), animationCatalog.BankName, StringComparison.OrdinalIgnoreCase));
        if (ordEntry is null || orlEntry is null)
        {
            continue;
        }

        try
        {
            using var ordStream = animationBoundaryStore.OpenBigEntry("data/anims.big", ordEntry.Name);
            using var orlStream = animationBoundaryStore.OpenBigEntry("data/anims.big", orlEntry.Name);
            var ordData = ReadBounded(ordStream, 64 * 1024 * 1024);
            var orlData = ReadBounded(orlStream, 64 * 1024 * 1024);
            var combinedData = EaglObjectReader.Combine(ordData, orlData);
            var inspection = ElfReader.Inspect(combinedData);
            animationBoundaryObservations.Add(AnimationBankSidecarBoundaryAudit.Build(
                catalogEntry.Name,
                animationCatalog,
                combinedData,
                ordData.Length,
                orlData.Length,
                inspection));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            // Keep this report limited to independently bounded sidecar/ELF observations.
        }
    }

    var animationBoundarySummary = AnimationBankSidecarBoundaryAudit.Summarize(animationBoundaryObservations);
    if (animationBoundaryReportPath is not null)
    {
        AnimationBankSidecarBoundaryAudit.WriteReport(animationBoundaryReportPath, animationBoundaryObservations);
    }

    if (animationBoundarySummaryPath is not null)
    {
        AnimationBankSidecarBoundaryAudit.WriteSummary(animationBoundarySummaryPath, animationBoundarySummary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC animation bank .ord/.orl physical boundary audit");
    Console.WriteLine("archive: data/anims.big");
    Console.WriteLine($"validated_boundary_observations: {animationBoundarySummary.ObservationCount}");
    Console.WriteLine($"non_relocation_sections_end_at_ord_boundary: {animationBoundarySummary.NonRelocationSectionsEndAtOrdBoundaryCount}");
    Console.WriteLine($"all_relocation_sections_at_or_after_ord_boundary: {animationBoundarySummary.AllRelocationSectionsAtOrAfterOrdBoundaryCount}");
    Console.WriteLine($"relocation_sections_starting_at_ord_boundary: {animationBoundarySummary.RelocationSectionsStartingAtOrdBoundaryCount}");
    Console.WriteLine($"section_headers_in_orl: {animationBoundarySummary.SectionHeadersInOrlCount}");
    Console.WriteLine($"section_headers_crossing_boundary: {animationBoundarySummary.SectionHeadersCrossingBoundaryCount}");
    Console.WriteLine($"distinct_ord_byte_counts: {animationBoundarySummary.DistinctOrdByteCounts}");
    Console.WriteLine($"distinct_orl_byte_counts: {animationBoundarySummary.DistinctOrlByteCounts}");
    Console.WriteLine($"distinct_section_header_locations: {animationBoundarySummary.DistinctSectionHeaderLocations}");
    if (animationBoundaryReportPath is not null)
    {
        Console.WriteLine($"report: {animationBoundaryReportPath} | rows={animationBoundaryObservations.Count}");
    }

    if (animationBoundarySummaryPath is not null)
    {
        Console.WriteLine($"summary: {animationBoundarySummaryPath}");
    }

    Console.WriteLine("status: .ord/.orl byte boundary, ELF section ranges and bounded boundary context only; no payload, relocation, symbol, frame, pose, rig, timing, skinning, material, UV, GPU, or command semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded BIG entries, managed ELF inspection");
    return animationBoundaryObservations.Count == animationBoundaryCatalogEntries.Length ? 0 : 1;
}

if (string.Equals(args[0], "eagl-morph-sibling-runtime-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: eagl-morph-sibling-runtime-smoke <caminho-do-iso>");
        return 2;
    }

    using var morphRuntimeFileSystem = new XdvdfsFileSystem(args[1]);
    var morphRuntimeCatalog = GameAssetCatalog.Scan(morphRuntimeFileSystem);
    var morphRuntimeEvidence = morphRuntimeCatalog.MorphSiblings;
    var invalidMorphArchives = morphRuntimeEvidence
        .Where(morph => morph.Issue is not null)
        .Select(morph => morph.NestedArchiveName)
        .ToArray();
    var morphEvidenceCount = morphRuntimeEvidence.Sum(morph => morph.MorphEvidenceCount);
    var completeSiblingCount = morphRuntimeEvidence.Sum(morph => morph.CompleteSiblingCount);
    var siblingElfCount = morphRuntimeEvidence.Sum(morph => morph.SiblingElfValidatedCount);
    var siblingGeometryCount = morphRuntimeEvidence.Sum(morph => morph.SiblingGeometryCount);
    var siblingBoundsCount = morphRuntimeEvidence.Sum(morph => morph.SiblingBoundsCount);
    var posePaletteSymbolCount = morphRuntimeEvidence.Sum(morph => morph.PosePaletteBankSymbolCount);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL morph sibling runtime smoke");
    Console.WriteLine($"nested_archives: {morphRuntimeEvidence.Count}");
    Console.WriteLine($"nested_archives_validated: {morphRuntimeEvidence.Count(morph => morph.Issue is null)}");
    Console.WriteLine($"morph_evidence_objects: {morphEvidenceCount}");
    Console.WriteLine($"complete_ord_orl_pairs: {completeSiblingCount}");
    Console.WriteLine($"sibling_elf_validated: {siblingElfCount}");
    Console.WriteLine($"sibling_geometry_candidates: {siblingGeometryCount}");
    Console.WriteLine($"sibling_bounds_candidates: {siblingBoundsCount}");
    Console.WriteLine($"pose_palette_bank_symbols: {posePaletteSymbolCount}");
    if (invalidMorphArchives.Length > 0)
    {
        Console.WriteLine($"invalid_nested_archives: {string.Join(",", invalidMorphArchives)}");
        return 1;
    }

    Console.WriteLine("status: same-nested-BIG morph/geometry sibling evidence only; no morph, pose, rig, weight, frame, or skinning decoder");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed ELF/EAGL inspection");
    return 0;
}

if (string.Equals(args[0], "eagl-morph-sidecar-boundary-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: eagl-morph-sidecar-boundary-audit <caminho-do-iso> <arquivo.big> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? morphBoundaryReportPath = null;
    string? morphBoundarySummaryPath = null;
    for (var optionIndex = 3; optionIndex < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: eagl-morph-sidecar-boundary-audit ... --report <saida.tsv>");
                return 2;
            }

            morphBoundaryReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            if (optionIndex + 1 >= args.Length || args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Uso: eagl-morph-sidecar-boundary-audit ... --summary <saida.tsv>");
                return 2;
            }

            morphBoundarySummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
        else
        {
            Console.Error.WriteLine($"Opção desconhecida: {args[optionIndex]}");
            return 2;
        }
    }

    var morphBoundaryInputPath = Path.GetFullPath(args[1]);
    if (new[] { morphBoundaryReportPath, morphBoundarySummaryPath }
        .Where(path => path is not null)
        .Any(path => string.Equals(path, morphBoundaryInputPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios de fronteira morph não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    if (morphBoundaryReportPath is not null && morphBoundarySummaryPath is not null &&
        string.Equals(morphBoundaryReportPath, morphBoundarySummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório e o resumo de fronteira morph devem ser arquivos diferentes.");
        return 2;
    }

    using var morphBoundaryStore = new GameAssetStore(args[1]);
    using var morphBoundaryTopLevelStream = morphBoundaryStore.OpenRaw(args[2]);
    var morphBoundaryTopLevel = BigArchiveReader.Inspect(morphBoundaryTopLevelStream);
    var morphBoundaryObservations = new List<EaglMorphSidecarBoundaryObservation>();
    var morphBoundaryNestedCount = 0;
    var morphBoundaryValidNestedCount = 0;
    var morphBoundaryInvalidNestedCount = 0;
    foreach (var nestedEntry in morphBoundaryTopLevel.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
    {
        morphBoundaryNestedCount++;
        try
        {
            using var compressedStream = morphBoundaryStore.OpenBigEntry(args[2], nestedEntry.Name);
            if (compressedStream.Length > 64L * 1024 * 1024)
            {
                throw new InvalidDataException("Nested EAGL archive exceeds the 64 MiB safety limit.");
            }

            var compressed = new byte[checked((int)compressedStream.Length)];
            compressedStream.ReadExactly(compressed);
            var declaredSize = RefPackDecoder.ReadDeclaredSize(compressed);
            if (declaredSize > 64 * 1024 * 1024)
            {
                throw new InvalidDataException("Expanded EAGL archive exceeds the 64 MiB safety limit.");
            }

            using var compressedMemory = new MemoryStream(compressed, writable: false);
            var expanded = RefPackDecoder.Decode(compressedMemory, declaredSize);
            morphBoundaryValidNestedCount++;
            morphBoundaryObservations.AddRange(
                EaglMorphSidecarBoundaryAudit.Inspect(expanded.Data, nestedEntry.Name));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            morphBoundaryInvalidNestedCount++;
            Console.WriteLine($"invalid_nested_big: {nestedEntry.Name} | {exception.Message}");
        }
    }

    var morphBoundarySummary = EaglMorphSidecarBoundaryAudit.Summarize(morphBoundaryObservations);
    if (morphBoundaryReportPath is not null)
    {
        EaglMorphSidecarBoundaryAudit.WriteReport(morphBoundaryReportPath, morphBoundaryObservations);
    }

    if (morphBoundarySummaryPath is not null)
    {
        EaglMorphSidecarBoundaryAudit.WriteSummary(morphBoundarySummaryPath, morphBoundarySummary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL morph sibling .ord/.orl physical boundary audit");
    Console.WriteLine($"archive: {args[2]}");
    Console.WriteLine($"nested_big_candidates: {morphBoundaryNestedCount}");
    Console.WriteLine($"valid_nested_big: {morphBoundaryValidNestedCount}");
    Console.WriteLine($"invalid_nested_big: {morphBoundaryInvalidNestedCount}");
    Console.WriteLine($"validated_boundary_observations: {morphBoundarySummary.ObservationCount}");
    Console.WriteLine($"non_relocation_sections_end_at_ord_boundary: {morphBoundarySummary.NonRelocationSectionsEndAtOrdBoundaryCount}");
    Console.WriteLine($"all_relocation_sections_at_or_after_ord_boundary: {morphBoundarySummary.AllRelocationSectionsAtOrAfterOrdBoundaryCount}");
    Console.WriteLine($"relocation_sections_starting_at_ord_boundary: {morphBoundarySummary.RelocationSectionsStartingAtOrdBoundaryCount}");
    Console.WriteLine($"section_headers_in_orl: {morphBoundarySummary.SectionHeadersInOrlCount}");
    Console.WriteLine($"section_headers_crossing_boundary: {morphBoundarySummary.SectionHeadersCrossingBoundaryCount}");
    Console.WriteLine($"distinct_ord_byte_counts: {morphBoundarySummary.DistinctOrdByteCounts}");
    Console.WriteLine($"distinct_orl_byte_counts: {morphBoundarySummary.DistinctOrlByteCounts}");
    Console.WriteLine($"distinct_section_header_locations: {morphBoundarySummary.DistinctSectionHeaderLocations}");
    if (morphBoundaryReportPath is not null)
    {
        Console.WriteLine($"report: {morphBoundaryReportPath} | rows={morphBoundaryObservations.Count}");
    }

    if (morphBoundarySummaryPath is not null)
    {
        Console.WriteLine($"summary: {morphBoundarySummaryPath}");
    }

    Console.WriteLine("status: morph sibling .ord/.orl byte boundary, ELF section ranges and bounded boundary context only; no payload, geometry, morph, pose, rig, weight, frame, timing, skinning, material, UV, GPU, or command semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG, managed ELF inspection");
    return morphBoundaryInvalidNestedCount == 0 && morphBoundaryObservations.Count > 0 ? 0 : 1;
}

if (string.Equals(args[0], "eagl-morph-pose-bank-index", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: eagl-morph-pose-bank-index <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? morphPoseBankReportPath = null;
    var morphPoseBankReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (morphPoseBankReportOption >= 0)
    {
        if (morphPoseBankReportOption + 1 >= args.Length ||
            args[morphPoseBankReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-pose-bank-index ... --report <saida.tsv>");
            return 2;
        }

        morphPoseBankReportPath = Path.GetFullPath(args[morphPoseBankReportOption + 1]);
    }

    using var morphPoseBankFileSystem = new XdvdfsFileSystem(args[1]);
    var morphPoseBankCatalog = GameAssetCatalog.Scan(morphPoseBankFileSystem);
    const string posePaletteBankMarker = "__PosePaletteBank:::";
    var animationBanksByName = morphPoseBankCatalog.AnimationBanks
        .Where(bank => bank.BankName.Length > 0)
        .GroupBy(bank => bank.BankName, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(
            group => group.Key,
            group => string.Join(
                "|",
                group.Select(bank => bank.CatalogEntry)
                    .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)),
            StringComparer.OrdinalIgnoreCase);
    var animationBankSymbolRecords = morphPoseBankCatalog.AnimationBanks
        .SelectMany(bank => bank.AnimationBankSymbolRecords.Select(symbol => (bank, symbol)))
        .ToArray();
    var morphPoseBankRows = morphPoseBankCatalog.MorphSiblings
        .SelectMany(morph => morph.PosePaletteBankSymbolRecords.Select(symbolRecord =>
        {
            var symbol = symbolRecord.Name;
            var normalizedName = symbol.StartsWith(posePaletteBankMarker, StringComparison.Ordinal)
                ? symbol[posePaletteBankMarker.Length..]
                : symbol;
            var matchesAnimationBank = animationBanksByName.TryGetValue(normalizedName, out var catalogEntries);
            var matchingAnimationSymbolBanks = animationBankSymbolRecords
                .Where(item => string.Equals(
                    GetElfSymbolMetadataKey(item.symbol),
                    GetElfSymbolMetadataKey(symbolRecord),
                    StringComparison.Ordinal))
                .Select(item => item.bank.BankName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new
            {
                morph.NestedArchiveName,
                Symbol = symbol,
                SymbolTableSectionIndex = symbolRecord.TableSectionIndex,
                SymbolIndex = symbolRecord.Index,
                SymbolSectionIndex = symbolRecord.SectionIndex,
                SymbolValue = symbolRecord.Value,
                SymbolSize = symbolRecord.Size,
                SymbolInfo = symbolRecord.Info,
                NormalizedName = normalizedName,
                MatchesAnimationBank = matchesAnimationBank,
                CatalogEntries = matchesAnimationBank ? catalogEntries : string.Empty,
                MatchesAnimationSymbolMetadata = matchingAnimationSymbolBanks.Length > 0,
                MatchingAnimationSymbolBanks = string.Join("|", matchingAnimationSymbolBanks),
            };
        }))
        .DistinctBy(row => (row.NestedArchiveName, row.Symbol, row.SymbolTableSectionIndex,
            row.SymbolIndex, row.SymbolSectionIndex, row.SymbolValue, row.SymbolSize, row.SymbolInfo))
        .OrderBy(row => row.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(row => row.Symbol, StringComparer.Ordinal)
        .ToArray();
    var morphPoseBankReportLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "nested_archive",
            "pose_palette_symbol",
            "symbol_table_section_index",
            "symbol_index",
            "symbol_section_index",
            "symbol_value",
            "symbol_size",
            "symbol_info",
            "normalized_name",
            "matches_animation_bank_name",
            "matching_catalog_entries",
            "matches_animation_symbol_metadata",
            "matching_animation_symbol_banks",
        }),
    };
    foreach (var row in morphPoseBankRows)
    {
        morphPoseBankReportLines.Add(string.Join("\t", new[]
        {
            CatalogField(row.NestedArchiveName),
            CatalogField(row.Symbol),
            row.SymbolTableSectionIndex.ToString(CultureInfo.InvariantCulture),
            row.SymbolIndex.ToString(CultureInfo.InvariantCulture),
            row.SymbolSectionIndex.ToString(CultureInfo.InvariantCulture),
            row.SymbolValue.ToString(CultureInfo.InvariantCulture),
            row.SymbolSize.ToString(CultureInfo.InvariantCulture),
            row.SymbolInfo.ToString(CultureInfo.InvariantCulture),
            CatalogField(row.NormalizedName),
            row.MatchesAnimationBank.ToString(),
            CatalogField(row.CatalogEntries),
            row.MatchesAnimationSymbolMetadata.ToString(),
            CatalogField(row.MatchingAnimationSymbolBanks),
        }));
    }

    if (morphPoseBankReportPath is not null)
    {
        var morphPoseBankReportDirectory = Path.GetDirectoryName(morphPoseBankReportPath);
        if (!string.IsNullOrEmpty(morphPoseBankReportDirectory))
        {
            Directory.CreateDirectory(morphPoseBankReportDirectory);
        }

        File.WriteAllLines(
            morphPoseBankReportPath,
            morphPoseBankReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    var matchingMorphPoseBankRows = morphPoseBankRows.Count(row => row.MatchesAnimationBank);
    var matchingMorphPoseBankNames = morphPoseBankRows
        .Where(row => row.MatchesAnimationBank)
        .Select(row => row.NormalizedName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();
    var matchingMorphPoseBankArchives = morphPoseBankRows
        .Where(row => row.MatchesAnimationBank)
        .Select(row => row.NestedArchiveName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();
    Console.WriteLine($"{GameIdentity.Name} PC EAGL morph pose-palette bank name index");
    Console.WriteLine($"nested_archives: {morphPoseBankCatalog.MorphSiblings.Count}");
    Console.WriteLine($"pose_palette_symbol_rows: {morphPoseBankRows.Length}");
    Console.WriteLine($"distinct_pose_palette_symbols: {morphPoseBankRows.Select(row => row.Symbol).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"distinct_pose_palette_symbol_records: {morphPoseBankRows.Select(row => row.Symbol + "|" + row.SymbolSectionIndex + "|" + row.SymbolValue + "|" + row.SymbolSize + "|" + row.SymbolInfo).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"rows_matching_animation_symbol_metadata: {morphPoseBankRows.Count(row => row.MatchesAnimationSymbolMetadata)}");
    Console.WriteLine($"distinct_matching_animation_symbol_banks: {string.Join("|", morphPoseBankRows
        .SelectMany(row => row.MatchingAnimationSymbolBanks.Split('|', StringSplitOptions.RemoveEmptyEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}");
    Console.WriteLine($"rows_matching_animation_bank_name: {matchingMorphPoseBankRows}");
    Console.WriteLine($"names_matching_animation_bank_name: {matchingMorphPoseBankNames}");
    Console.WriteLine($"archives_with_matching_name: {matchingMorphPoseBankArchives}");
    if (morphPoseBankReportPath is not null)
    {
        Console.WriteLine($"report: {morphPoseBankReportPath} | rows={morphPoseBankReportLines.Count - 1}");
    }
    Console.WriteLine("status: exact symbol/name and raw ELF metadata recurrence only; no pose, rig, weight, frame, timing, or skinning binding assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, runtime catalog, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "vivf-block-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4 || !int.TryParse(args[3], out var blockIndex))
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-block-smoke <caminho-do-iso> <arquivo.bh> <índice>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var block = assetStore.OpenVivFBlock(args[2], blockIndex);
    var bytes = new byte[Math.Min(16, checked((int)block.Data.Length))];
    var read = block.Data.Read(bytes, 0, bytes.Length);
    Console.WriteLine($"{GameIdentity.Name} PC VivF block smoke");
    Console.WriteLine($"package: {block.PackagePath}");
    Console.WriteLine($"companion: {block.CompanionPath}");
    Console.WriteLine($"block_index: {block.Index}");
    Console.WriteLine($"offset: {block.Record.Offset}");
    Console.WriteLine($"compressed_size: {block.Record.CompressedSize}");
    Console.WriteLine($"expanded_size: {block.Record.ExpandedSize}");
    Console.WriteLine($"first_bytes: {Convert.ToHexString(bytes, 0, read)}");
    Console.WriteLine("mode: ISO read-only, bounded compressed block stream");
    return 0;
}

if (string.Equals(args[0], "vivf-decode-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4 || !int.TryParse(args[3], out var decodeBlockIndex))
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-decode-smoke <caminho-do-iso> <arquivo.bh> <índice>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var block = assetStore.OpenVivFBlock(args[2], decodeBlockIndex);
    var decoded = RefPackDecoder.Decode(block.Data, checked((int)block.Record.ExpandedSize));
    var bytes = decoded.Data.AsSpan(0, Math.Min(16, decoded.Data.Length));
    Console.WriteLine($"{GameIdentity.Name} PC VivF RefPack decode smoke");
    Console.WriteLine($"package: {block.PackagePath}");
    Console.WriteLine($"block_index: {block.Index}");
    Console.WriteLine($"compressed_bytes: {decoded.CompressedBytes}");
    Console.WriteLine($"decoded_bytes: {decoded.Data.Length}");
    Console.WriteLine($"commands: {decoded.Commands}");
    Console.WriteLine($"first_bytes: {Convert.ToHexString(bytes)}");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode");
    return 0;
}

if (string.Equals(args[0], "vivf-validate-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-validate-smoke <caminho-do-iso> <arquivo.bh>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    var package = assetStore.InspectVivF(args[2]);
    long decodedBytes = 0;
    long compressedBytes = 0;
    long commandCount = 0;
    for (var index = 0; index < checked((int)package.EntryCount); index++)
    {
        using var block = assetStore.OpenVivFBlock(args[2], index);
        var decoded = RefPackDecoder.Decode(block.Data, checked((int)block.Record.ExpandedSize));
        decodedBytes += decoded.Data.Length;
        compressedBytes += decoded.CompressedBytes;
        commandCount += decoded.Commands;
    }

    Console.WriteLine($"{GameIdentity.Name} PC VivF RefPack batch smoke");
    Console.WriteLine($"package: {args[2]}");
    Console.WriteLine($"blocks: {package.EntryCount}");
    Console.WriteLine($"compressed_bytes: {compressedBytes}");
    Console.WriteLine($"decoded_bytes: {decodedBytes}");
    Console.WriteLine($"commands: {commandCount}");
    Console.WriteLine("status: every block decoded at its declared expanded size");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode");
    return 0;
}

if (string.Equals(args[0], "vivf-nested-big-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4 || !int.TryParse(args[3], out var nestedBlockIndex))
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-nested-big-smoke <caminho-do-iso> <arquivo.bh> <índice>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var block = assetStore.OpenVivFBlock(args[2], nestedBlockIndex);
    var decoded = RefPackDecoder.Decode(block.Data, checked((int)block.Record.ExpandedSize));
    var archive = BigArchiveReader.Inspect(decoded.Data);
    Console.WriteLine($"{GameIdentity.Name} PC nested BIG smoke");
    Console.WriteLine($"package: {block.PackagePath}");
    Console.WriteLine($"block_index: {block.Index}");
    Console.WriteLine($"magic: {archive.Magic}");
    Console.WriteLine($"archive_size: {archive.ArchiveSize}");
    Console.WriteLine($"entries: {archive.FileCount}");
    Console.WriteLine($"index_end: {archive.IndexEnd}");
    foreach (var entry in archive.Entries.Take(8))
    {
        Console.WriteLine($"entry: {entry.Name} | offset={entry.Offset} | size={entry.Size}");
    }
    Console.WriteLine("mode: ISO read-only, RefPack decode, nested BIG inspection");
    return 0;
}

if (string.Equals(args[0], "vivf-nested-big-validate", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-nested-big-validate <caminho-do-iso> <arquivo.bh>");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    var package = assetStore.InspectVivF(args[2]);
    long totalEntries = 0;
    for (var index = 0; index < checked((int)package.EntryCount); index++)
    {
        using var block = assetStore.OpenVivFBlock(args[2], index);
        var decoded = RefPackDecoder.Decode(block.Data, checked((int)block.Record.ExpandedSize));
        var archive = BigArchiveReader.Inspect(decoded.Data);
        totalEntries += archive.FileCount;
    }

    Console.WriteLine($"{GameIdentity.Name} PC nested BIG batch smoke");
    Console.WriteLine($"package: {args[2]}");
    Console.WriteLine($"blocks: {package.EntryCount}");
    Console.WriteLine($"nested_archives: {package.EntryCount}");
    Console.WriteLine($"nested_entries: {totalEntries}");
    Console.WriteLine("status: every decoded block is a valid BIG archive");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG inspection");
    return 0;
}

if (string.Equals(args[0], "apt-structure-inventory", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc apt-structure-inventory <caminho-do-iso> <arquivo.bh> [--report <saida.tsv>] [--profile-report <saida.tsv>]");
        return 2;
    }

    string? aptStructuralReportPath = null;
    string? aptStructuralProfileReportPath = null;
    var aptStructuralReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (aptStructuralReportOptionIndex >= 0)
    {
        if (aptStructuralReportOptionIndex + 1 >= args.Length ||
            args[aptStructuralReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: apt-structure-inventory ... --report <saida.tsv>");
            return 2;
        }

        aptStructuralReportPath = Path.GetFullPath(args[aptStructuralReportOptionIndex + 1]);
        if (string.Equals(
                aptStructuralReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório APT não pode sobrescrever a ISO de entrada.");
            return 2;
        }
    }
    var aptStructuralProfileReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--profile-report", StringComparison.OrdinalIgnoreCase));
    if (aptStructuralProfileReportOptionIndex >= 0)
    {
        if (aptStructuralProfileReportOptionIndex + 1 >= args.Length ||
            args[aptStructuralProfileReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: apt-structure-inventory ... --profile-report <saida.tsv>");
            return 2;
        }

        aptStructuralProfileReportPath = Path.GetFullPath(args[aptStructuralProfileReportOptionIndex + 1]);
        if (string.Equals(
                aptStructuralProfileReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                aptStructuralProfileReportPath,
                aptStructuralReportPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de perfis APT não pode sobrescrever a ISO ou o relatório por pacote.");
            return 2;
        }
    }

    using var aptStore = new GameAssetStore(args[1]);
    var aptPackage = aptStore.InspectVivF(args[2]);
    var nestedArchiveCount = 0;
    var nestedEntryCount = 0L;
    var aptCount = 0;
    var constantCount = 0;
    var objectCount = 0;
    var shapeCount = 0;
    var validAptCount = 0;
    var invalidAptCount = 0;
    var validConstantCount = 0;
    var invalidConstantCount = 0;
    var aptFieldsWithinAssetCount = 0;
    var aptFieldsOutsideAssetCount = 0;
    var constantFieldsWithinAssetCount = 0;
    var constantFieldsOutsideAssetCount = 0;
    var aptFormatCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    var aptHeaderPatterns = new Dictionary<string, int>(StringComparer.Ordinal);
    var aptHeaderPatternCompanions = new Dictionary<string, (int Total, int FieldsWithin, int WithConstant, int WithObject, int WithShape)>(StringComparer.Ordinal);
    var companionSets = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    var companionSizeProfiles = new Dictionary<string, Dictionary<string, uint>>(StringComparer.OrdinalIgnoreCase);
    var constantInspections = new Dictionary<string, AptInspection>(StringComparer.OrdinalIgnoreCase);
    var objectStructuralSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var objectMorphInspections = new Dictionary<string, EaglMorphInspection>(StringComparer.OrdinalIgnoreCase);
    var invalidObjectStructuralCount = 0;
    var shapePackageSignatures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    var invalidShapePackageCount = 0;
    var aptCompanionStructuralSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var sampleCount = 0;
    var aptStructuralObservations = new List<AptStructuralObservation>();
    var aptStructuralReportLines = aptStructuralReportPath is null
        ? null
        : new List<string>
        {
            string.Join("\t", new[]
            {
                "block_index",
                "apt_entry",
                "stem",
                "apt_size",
                "apt_format",
                "apt_version",
                "apt_header_size",
                "apt_header_fields",
                "apt_fields_within_asset",
                "companion_extensions",
                "constant_valid",
                "constant_format",
                "constant_header_size",
                "constant_header_fields",
                "constant_fields_within_asset",
                "object_valid",
                "shape_valid",
                "object_structural_signature_sha256",
                "shape_structural_signature_sha256",
                "object_has_morph_symbol",
                "object_morph_symbol",
                "object_morph_shape_name_count",
                "object_morph_attribute_name_count",
                "object_pose_palette_bank_symbol_count",
                "normalized_morph_sibling_stem_candidate",
                "package_structural_signature_sha256",
            }),
        };
    var aptStructuralReportMorphRows = 0;

    for (var blockIndex = 0; blockIndex < checked((int)aptPackage.EntryCount); blockIndex++)
    {
        using var aptBlock = aptStore.OpenVivFBlock(args[2], blockIndex);
        var expandedAptBlock = RefPackDecoder.Decode(
            aptBlock.Data,
            checked((int)aptBlock.Record.ExpandedSize));
        var nestedAptArchive = BigArchiveReader.Inspect(expandedAptBlock.Data);
        nestedArchiveCount++;
        nestedEntryCount += nestedAptArchive.Entries.Count;

        foreach (var nestedEntry in nestedAptArchive.Entries)
        {
            var extension = Path.GetExtension(nestedEntry.Name).ToLowerInvariant();
            if (extension is not (".apt" or ".const" or ".o" or ".xsh"))
            {
                continue;
            }

            var companionKey = $"{blockIndex}:{Path.GetFileNameWithoutExtension(nestedEntry.Name)}";
            if (!companionSets.TryGetValue(companionKey, out var extensions))
            {
                extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                companionSets.Add(companionKey, extensions);
            }
            extensions.Add(extension);
            if (!companionSizeProfiles.TryGetValue(companionKey, out var sizes))
            {
                sizes = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
                companionSizeProfiles.Add(companionKey, sizes);
            }
            sizes[extension] = nestedEntry.Size;
            if (extension == ".const")
            {
                try
                {
                    var constantData = BigArchiveReader.ReadEntry(expandedAptBlock.Data, nestedEntry.Name);
                    constantInspections[companionKey] = AptReader.Inspect(constantData);
                }
                catch (InvalidDataException)
                {
                    // The inventory below reports the invalid entry separately;
                    // the combined signature keeps the state explicit.
                }
            }
            else if (extension == ".o")
            {
                try
                {
                    var objectData = BigArchiveReader.ReadEntry(expandedAptBlock.Data, nestedEntry.Name);
                    var objectInspection = EaglObjectReader.Inspect(objectData);
                    objectStructuralSignatures[companionKey] =
                        EaglObjectReader.GetStructuralSignature(objectInspection);
                    if (aptStructuralReportPath is not null)
                    {
                        objectMorphInspections[companionKey] =
                            EaglMorphReader.Inspect(objectData, objectInspection.Elf);
                    }
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
                {
                    // Keep malformed EAGL objects explicit instead of
                    // converting a failed parse into a package binding.
                    invalidObjectStructuralCount++;
                }
            }
            else if (extension == ".xsh")
            {
                try
                {
                    var shapeData = BigArchiveReader.ReadEntry(expandedAptBlock.Data, nestedEntry.Name);
                    var shapePackage = ShapeImagePackageLoader.Load(shapeData);
                    shapePackageSignatures[companionKey] =
                        ShapeImagePackageLoader.GetStructuralSignature(shapePackage);
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
                {
                    // The inventory keeps malformed SHPX explicit by leaving
                    // the package signature absent.
                    invalidShapePackageCount++;
                }
            }
        }

        foreach (var nestedEntry in nestedAptArchive.Entries)
        {
            var extension = Path.GetExtension(nestedEntry.Name).ToLowerInvariant();
            var stem = Path.GetFileNameWithoutExtension(nestedEntry.Name);
            if (extension is ".apt" or ".const" or ".o" or ".xsh")
            {
                var companionKey = $"{blockIndex}:{stem}";
                if (!companionSets.TryGetValue(companionKey, out var extensions))
                {
                    extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    companionSets.Add(companionKey, extensions);
                }
                extensions.Add(extension);
            }

            if (extension is not (".apt" or ".const"))
            {
                if (extension == ".o")
                {
                    objectCount++;
                }
                else if (extension == ".xsh")
                {
                    shapeCount++;
                }
                continue;
            }

            var entryData = BigArchiveReader.ReadEntry(expandedAptBlock.Data, nestedEntry.Name);
            AptInspection inspection;
            try
            {
                inspection = AptReader.Inspect(entryData);
                if (extension == ".apt")
                {
                    validAptCount++;
                    if (inspection.FieldsWithinAsset)
                    {
                        aptFieldsWithinAssetCount++;
                    }
                    else
                    {
                        aptFieldsOutsideAssetCount++;
                    }
                }
                else
                {
                    validConstantCount++;
                    if (inspection.FieldsWithinAsset)
                    {
                        constantFieldsWithinAssetCount++;
                    }
                    else
                    {
                        constantFieldsOutsideAssetCount++;
                    }
                }
            }
            catch (InvalidDataException)
            {
                if (extension == ".apt")
                {
                    invalidAptCount++;
                }
                else
                {
                    invalidConstantCount++;
                }
                continue;
            }

            if (extension == ".apt")
            {
                aptCount++;
                aptFormatCounts[inspection.Format] = aptFormatCounts.GetValueOrDefault(inspection.Format) + 1;
                var pattern = $"{inspection.HeaderSize}:{string.Join(",", inspection.HeaderFields)}";
                aptHeaderPatterns[pattern] = aptHeaderPatterns.GetValueOrDefault(pattern) + 1;
                var companions = companionSets[$"{blockIndex}:{stem}"];
                var evidence = aptHeaderPatternCompanions.GetValueOrDefault(pattern);
                aptHeaderPatternCompanions[pattern] = (
                    evidence.Total + 1,
                    evidence.FieldsWithin + (inspection.FieldsWithinAsset ? 1 : 0),
                    evidence.WithConstant + (companions.Contains(".const") ? 1 : 0),
                    evidence.WithObject + (companions.Contains(".o") ? 1 : 0),
                    evidence.WithShape + (companions.Contains(".xsh") ? 1 : 0));
                var companionKey = $"{blockIndex}:{stem}";
                var companionStructuralSignature = AptReader.GetPackageStructuralSignature(
                    inspection,
                    constantInspections.GetValueOrDefault(companionKey),
                    companionSizeProfiles[companionKey],
                    shapePackageSignatures.GetValueOrDefault(companionKey),
                    objectStructuralSignatures.GetValueOrDefault(companionKey));
                aptCompanionStructuralSignatures[companionStructuralSignature] =
                    aptCompanionStructuralSignatures.GetValueOrDefault(companionStructuralSignature) + 1;
                objectMorphInspections.TryGetValue(companionKey, out var objectMorphInspection);
                var normalizedMorphSiblingStemCandidate =
                    EaglMorphReader.TryGetNormalizedSiblingStem($"{stem}.o", out var normalizedMorphStem)
                        ? normalizedMorphStem
                        : string.Empty;
                var hasConstantInspection = constantInspections.TryGetValue(
                    companionKey,
                    out var constantInspection);
                var hasObjectStructuralSignature = objectStructuralSignatures.TryGetValue(
                    companionKey,
                    out var objectStructuralSignature);
                var hasShapeStructuralSignature = shapePackageSignatures.TryGetValue(
                    companionKey,
                    out var shapePackageSignature);
                if (aptStructuralProfileReportPath is not null)
                {
                    var constantPattern = constantInspection is null
                        ? string.Empty
                        : $"{constantInspection.HeaderSize}:{string.Join(",", constantInspection.HeaderFields)}:{constantInspection.FieldsWithinAsset}";
                    aptStructuralObservations.Add(new AptStructuralObservation(
                        $"{pattern}:{inspection.FieldsWithinAsset}",
                        constantPattern,
                        string.Join(",", companions.OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase)),
                        inspection.FieldsWithinAsset,
                        constantInspection?.FieldsWithinAsset ?? false,
                        hasObjectStructuralSignature,
                        hasShapeStructuralSignature,
                        objectMorphInspection?.HasMorphSymbol == true,
                        inspection.HeaderFields,
                        constantInspection?.HeaderFields ?? Array.Empty<uint>(),
                        hasObjectStructuralSignature
                            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(objectStructuralSignature!)))
                            : string.Empty,
                        hasShapeStructuralSignature
                            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shapePackageSignature!)))
                            : string.Empty));
                }
                if (aptStructuralReportLines is not null)
                {
                    if (objectMorphInspection?.HasMorphSymbol == true)
                    {
                        aptStructuralReportMorphRows++;
                    }

                    aptStructuralReportLines.Add(string.Join("\t", new[]
                    {
                        blockIndex.ToString(CultureInfo.InvariantCulture),
                        CatalogField(nestedEntry.Name),
                        CatalogField(stem),
                        entryData.Length.ToString(CultureInfo.InvariantCulture),
                        CatalogField(inspection.Format),
                        CatalogField(inspection.VersionTag),
                        inspection.HeaderSize.ToString(CultureInfo.InvariantCulture),
                        CatalogField(string.Join(",", inspection.HeaderFields.Select(field => field.ToString(CultureInfo.InvariantCulture)))),
                        inspection.FieldsWithinAsset.ToString(),
                        CatalogField(string.Join(",", companions.OrderBy(extension => extension, StringComparer.OrdinalIgnoreCase))),
                        hasConstantInspection.ToString(),
                        CatalogField(constantInspection?.Format),
                        constantInspection is null
                            ? string.Empty
                            : constantInspection.HeaderSize.ToString(CultureInfo.InvariantCulture),
                        CatalogField(constantInspection is null
                            ? string.Empty
                            : string.Join(",", constantInspection.HeaderFields.Select(field => field.ToString(CultureInfo.InvariantCulture)))),
                        constantInspection?.FieldsWithinAsset.ToString() ?? string.Empty,
                        hasObjectStructuralSignature.ToString(),
                        hasShapeStructuralSignature.ToString(),
                        hasObjectStructuralSignature
                            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(objectStructuralSignature!)))
                            : string.Empty,
                        hasShapeStructuralSignature
                            ? Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(shapePackageSignature!)))
                            : string.Empty,
                        (objectMorphInspection?.HasMorphSymbol == true).ToString(),
                        CatalogField(objectMorphInspection?.MorphSymbol),
                        (objectMorphInspection?.ShapeNames.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                        (objectMorphInspection?.AttributeNames.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                        (objectMorphInspection?.PosePaletteBankSymbols.Count ?? 0).ToString(CultureInfo.InvariantCulture),
                        CatalogField(normalizedMorphSiblingStemCandidate),
                        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(companionStructuralSignature))),
                    }));
                }
                if (sampleCount < 8)
                {
                    Console.WriteLine($"sample_apt: block={blockIndex} | entry={nestedEntry.Name} | size={entryData.Length} | version={inspection.VersionTag} | header_size={inspection.HeaderSize} | fields={string.Join(",", inspection.HeaderFields)}");
                    sampleCount++;
                }
            }
            else
            {
                constantCount++;
            }
        }
    }

    var completeCompanionSetCount = companionSets.Values.Count(extensions =>
        extensions.SetEquals([".apt", ".const", ".o", ".xsh"]));
    var aptWithConstantCount = companionSets.Values.Count(extensions => extensions.Contains(".apt") && extensions.Contains(".const"));
    var aptWithObjectCount = companionSets.Values.Count(extensions => extensions.Contains(".apt") && extensions.Contains(".o"));
    var aptWithShapeCount = companionSets.Values.Count(extensions => extensions.Contains(".apt") && extensions.Contains(".xsh"));

    Console.WriteLine($"{GameIdentity.Name} PC APT structural inventory");
    Console.WriteLine($"package: {args[2]}");
    Console.WriteLine($"vivf_blocks: {aptPackage.EntryCount}");
    Console.WriteLine($"nested_archives: {nestedArchiveCount}");
    Console.WriteLine($"nested_entries: {nestedEntryCount}");
    Console.WriteLine($"apt_entries: {aptCount}");
    Console.WriteLine($"constant_entries: {constantCount}");
    Console.WriteLine($"object_entries: {objectCount}");
    Console.WriteLine($"shape_entries: {shapeCount}");
    Console.WriteLine($"valid_apt_entries: {validAptCount}");
    Console.WriteLine($"invalid_apt_entries: {invalidAptCount}");
    Console.WriteLine($"apt_fields_within_asset: {aptFieldsWithinAssetCount}");
    Console.WriteLine($"apt_fields_outside_asset: {aptFieldsOutsideAssetCount}");
    Console.WriteLine($"valid_constant_entries: {validConstantCount}");
    Console.WriteLine($"invalid_constant_entries: {invalidConstantCount}");
    Console.WriteLine($"constant_fields_within_asset: {constantFieldsWithinAssetCount}");
    Console.WriteLine($"constant_fields_outside_asset: {constantFieldsOutsideAssetCount}");
    Console.WriteLine($"apt_formats: {string.Join(",", aptFormatCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"apt_header_pattern_count: {aptHeaderPatterns.Count}");
    Console.WriteLine($"apt_companion_structural_signature_count: {aptCompanionStructuralSignatures.Count}");
    Console.WriteLine($"valid_o_structural_objects: {objectStructuralSignatures.Count}");
    Console.WriteLine($"invalid_o_structural_objects: {invalidObjectStructuralCount}");
    Console.WriteLine($"valid_xsh_structural_packages: {shapePackageSignatures.Count}");
    Console.WriteLine($"invalid_xsh_structural_packages: {invalidShapePackageCount}");
    Console.WriteLine($"apt_companion_structural_signatures: {string.Join(" | ", aptCompanionStructuralSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(16)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"apt_header_pattern_companions: {string.Join(" | ", aptHeaderPatternCompanions
        .OrderByDescending(pair => pair.Value.Total)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(16)
        .Select(pair => $"{pair.Value.Total}x[{pair.Key};within={pair.Value.FieldsWithin};const={pair.Value.WithConstant};o={pair.Value.WithObject};xsh={pair.Value.WithShape}]"))}");
    Console.WriteLine($"apt_header_patterns: {string.Join(" | ", aptHeaderPatterns.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"companion_sets: {companionSets.Count}");
    Console.WriteLine($"complete_companion_sets: {completeCompanionSetCount}");
    Console.WriteLine($"apt_with_const: {aptWithConstantCount}");
    Console.WriteLine($"apt_with_o: {aptWithObjectCount}");
    Console.WriteLine($"apt_with_xsh: {aptWithShapeCount}");
    if (aptStructuralReportPath is not null && aptStructuralReportLines is not null)
    {
        var aptStructuralReportDirectory = Path.GetDirectoryName(aptStructuralReportPath);
        if (!string.IsNullOrEmpty(aptStructuralReportDirectory))
        {
            Directory.CreateDirectory(aptStructuralReportDirectory);
        }

        File.WriteAllLines(
            aptStructuralReportPath,
            aptStructuralReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"apt_structural_report: {aptStructuralReportPath} | rows={aptStructuralReportLines.Count - 1} | morph_symbol_rows={aptStructuralReportMorphRows}");
    }
    if (aptStructuralProfileReportPath is not null)
    {
        var profiles = AptStructuralCorrelation.Summarize(aptStructuralObservations);
        var profileLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "apt_header_pattern",
                "constant_header_pattern",
                "companion_extensions",
                "row_count",
                "rows_with_aligned_equal_words",
                "total_aligned_compared_words",
                "total_aligned_equal_words",
                "total_aligned_different_words",
                "apt_fields_within_asset_rows",
                "constant_fields_within_asset_rows",
                "object_valid_rows",
                "shape_valid_rows",
                "morph_symbol_rows",
                "distinct_object_structural_signature_hashes",
                "distinct_shape_structural_signature_hashes",
                "aligned_equal_word_ratio",
            }),
        };
        foreach (var profile in profiles)
        {
            profileLines.Add(string.Join("\t", new[]
            {
                CatalogField(profile.AptHeaderPattern),
                CatalogField(profile.ConstantHeaderPattern),
                CatalogField(profile.CompanionExtensions),
                profile.RowCount.ToString(CultureInfo.InvariantCulture),
                profile.RowsWithAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
                profile.TotalAlignedComparedWords.ToString(CultureInfo.InvariantCulture),
                profile.TotalAlignedEqualWords.ToString(CultureInfo.InvariantCulture),
                profile.TotalAlignedDifferentWords.ToString(CultureInfo.InvariantCulture),
                profile.AptFieldsWithinAssetRows.ToString(CultureInfo.InvariantCulture),
                profile.ConstantFieldsWithinAssetRows.ToString(CultureInfo.InvariantCulture),
                profile.ObjectValidRows.ToString(CultureInfo.InvariantCulture),
                profile.ShapeValidRows.ToString(CultureInfo.InvariantCulture),
                profile.MorphSymbolRows.ToString(CultureInfo.InvariantCulture),
                profile.DistinctObjectStructuralSignatureHashes.ToString(CultureInfo.InvariantCulture),
                profile.DistinctShapeStructuralSignatureHashes.ToString(CultureInfo.InvariantCulture),
                CatalogField(profile.AlignedEqualWordRatio),
            }));
        }

        var profileDirectory = Path.GetDirectoryName(aptStructuralProfileReportPath);
        if (!string.IsNullOrEmpty(profileDirectory))
        {
            Directory.CreateDirectory(profileDirectory);
        }

        File.WriteAllLines(
            aptStructuralProfileReportPath,
            profileLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"apt_structural_profile_report: {aptStructuralProfileReportPath} | rows={aptStructuralObservations.Count} | profiles={profiles.Count} | aligned_equal_words={profiles.Sum(profile => profile.TotalAlignedEqualWords)} | aligned_different_words={profiles.Sum(profile => profile.TotalAlignedDifferentWords)}");
    }
    Console.WriteLine("note: APT fields and companion names remain structural evidence; no animation timing, pose, morph, or material semantics are assigned.");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG inspection");
    return 0;
}

if (string.Equals(args[0], "apt-animation-negative-control", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc apt-animation-negative-control <apt-fe.tsv> <apt-ig.tsv> <animation-bank-name-index.tsv> [--report <saida.tsv>]");
        return 2;
    }

    string? negativeControlReportPath = null;
    var negativeControlReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (negativeControlReportOptionIndex >= 0)
    {
        if (negativeControlReportOptionIndex + 1 >= args.Length ||
            args[negativeControlReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: apt-animation-negative-control ... --report <saida.tsv>");
            return 2;
        }

        negativeControlReportPath = Path.GetFullPath(args[negativeControlReportOptionIndex + 1]);
        if (string.Equals(negativeControlReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(negativeControlReportPath, Path.GetFullPath(args[2]), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(negativeControlReportPath, Path.GetFullPath(args[3]), StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de controle negativo não pode sobrescrever um relatório de entrada.");
            return 2;
        }
    }

    var negativeControlAnimationRows = AptAnimationNegativeControl.GetAnimationRowCount(args[3]);
    var negativeControlSummaries = AptAnimationNegativeControl.Evaluate(
        new[]
        {
            (Source: "fe", Path: args[1]),
            (Source: "ig", Path: args[2]),
        },
        args[3]);
    var negativeControlTotal = new AptAnimationNegativeControlSummary(
        "total",
        negativeControlSummaries.Sum(summary => summary.AptRows),
        negativeControlSummaries.Sum(summary => summary.RowsWithValidObject),
        negativeControlSummaries.Sum(summary => summary.RowsWithMorphSymbol),
        negativeControlSummaries.Sum(summary => summary.RowsWithMorphSuffixCandidate),
        negativeControlSummaries.Sum(summary => summary.RowsWithCompleteStructuralCompanions),
        negativeControlSummaries.Sum(summary => summary.DistinctStems),
        negativeControlSummaries.Sum(summary => summary.ExactStemMatchesAnimationBankNames),
        negativeControlSummaries.Sum(summary => summary.ExactStemMatchesAnimationFileNames),
        negativeControlSummaries.Sum(summary => summary.ExactMorphStemMatchesAnimationBankNames),
        negativeControlSummaries.Sum(summary => summary.ExactMorphStemMatchesAnimationFileNames),
        negativeControlSummaries.Sum(summary => summary.DistinctPackageStructuralSignatures),
        negativeControlSummaries.Sum(summary => summary.DistinctShapeStructuralSignatures));
    var negativeControlOutput = negativeControlSummaries
        .Append(negativeControlTotal)
        .ToArray();

    if (negativeControlReportPath is not null)
    {
        var negativeControlDirectory = Path.GetDirectoryName(negativeControlReportPath);
        if (!string.IsNullOrEmpty(negativeControlDirectory))
        {
            Directory.CreateDirectory(negativeControlDirectory);
        }

        File.WriteAllLines(
            negativeControlReportPath,
            AptAnimationNegativeControl.ToTsv(negativeControlOutput),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    Console.WriteLine($"{GameIdentity.Name} PC APT versus animation negative control");
    Console.WriteLine($"animation_name_index_rows: {negativeControlAnimationRows}");
    foreach (var summary in negativeControlOutput)
    {
        Console.WriteLine(
            $"{summary.Source}: apt_rows={summary.AptRows} | valid_objects={summary.RowsWithValidObject} | " +
            $"morph_symbols={summary.RowsWithMorphSymbol} | morph_suffix_candidates={summary.RowsWithMorphSuffixCandidate} | " +
            $"complete_structural_companions={summary.RowsWithCompleteStructuralCompanions} | " +
            $"stem_bank_name_matches={summary.ExactStemMatchesAnimationBankNames} | " +
            $"stem_animation_file_matches={summary.ExactStemMatchesAnimationFileNames} | " +
            $"morph_stem_bank_name_matches={summary.ExactMorphStemMatchesAnimationBankNames} | " +
            $"morph_stem_animation_file_matches={summary.ExactMorphStemMatchesAnimationFileNames}");
    }
    if (negativeControlReportPath is not null)
    {
        Console.WriteLine($"report: {negativeControlReportPath}");
    }
    Console.WriteLine("status: headers, constants, SHPX tables and textual name overlap only; no timing, pose, frame, morph or animation binding assigned");
    Console.WriteLine("mode: read-only TSV evidence control");
    return negativeControlTotal.RowsWithMorphSymbol == 0 &&
           negativeControlTotal.RowsWithMorphSuffixCandidate == 0 &&
           negativeControlTotal.ExactStemMatchesAnimationBankNames == 0 &&
           negativeControlTotal.ExactStemMatchesAnimationFileNames == 0 &&
           negativeControlTotal.ExactMorphStemMatchesAnimationBankNames == 0 &&
           negativeControlTotal.ExactMorphStemMatchesAnimationFileNames == 0 ? 0 : 1;
}

if (string.Equals(args[0], "apt-cross-package-profile-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc apt-cross-package-profile-audit <apt-fe-profiles.tsv> <apt-ig-profiles.tsv> <audit.tsv> [--summary <summary.tsv>]");
        return 2;
    }

    var crossProfileFePath = Path.GetFullPath(args[1]);
    var crossProfileIgPath = Path.GetFullPath(args[2]);
    var crossProfileAuditPath = Path.GetFullPath(args[3]);
    var crossProfileSummaryPath = Path.Combine(
        Path.GetDirectoryName(crossProfileAuditPath) ?? Directory.GetCurrentDirectory(),
        Path.GetFileNameWithoutExtension(crossProfileAuditPath) + "-summary.tsv");
    for (var argumentIndex = 4; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            crossProfileSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (new[] { crossProfileFePath, crossProfileIgPath }
        .Any(path => string.Equals(path, crossProfileAuditPath, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(path, crossProfileSummaryPath, StringComparison.OrdinalIgnoreCase)) ||
        string.Equals(crossProfileAuditPath, crossProfileSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria de perfis APT não pode sobrescrever relatórios de entrada nem usar o mesmo caminho para auditoria e resumo.");
        return 2;
    }

    var crossProfileFe = AptCrossPackageProfileAudit.ReadProfiles(crossProfileFePath);
    var crossProfileIg = AptCrossPackageProfileAudit.ReadProfiles(crossProfileIgPath);
    var crossProfileRows = AptCrossPackageProfileAudit.Audit(crossProfileFe, crossProfileIg);
    AptCrossPackageProfileAudit.WriteReports(
        crossProfileAuditPath,
        crossProfileSummaryPath,
        crossProfileRows);
    var crossProfileSummary = AptCrossPackageProfileAudit.Summarize(crossProfileRows);
    Console.WriteLine($"apt_cross_package_profile_audit: {crossProfileAuditPath} | summary={crossProfileSummaryPath} | profile_keys={crossProfileRows.Count}");
    Console.WriteLine($"profiles: fe={crossProfileSummary.FeProfileKeys} | ig={crossProfileSummary.IgProfileKeys} | shared={crossProfileSummary.SharedProfileKeys} | fe_only={crossProfileSummary.FeOnlyProfileKeys} | ig_only={crossProfileSummary.IgOnlyProfileKeys}");
    Console.WriteLine($"shared_controls: aligned_equal_words={crossProfileSummary.SharedKeysWithAlignedEqualWords} | morph_symbol_rows={crossProfileSummary.SharedKeysWithMorphSymbolRows}");
    Console.WriteLine($"aligned_words: fe_compared={crossProfileSummary.FeAlignedComparedWords} | ig_compared={crossProfileSummary.IgAlignedComparedWords} | fe_equal={crossProfileSummary.FeAlignedEqualWords} | ig_equal={crossProfileSummary.IgAlignedEqualWords}");
    Console.WriteLine("status: exact APT/.const/companion profile recurrence only; no timing, pose, frame, morph, rig, material, UV, skinning, animation, or GPU semantics assigned");
    Console.WriteLine("mode: TSV evidence only, no ISO access");
    return 0;
}

if (string.Equals(args[0], "vivf-model-inventory", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-model-inventory <caminho-do-iso> <arquivo.bh>");
        return 2;
    }

    using var modelStore = new GameAssetStore(args[1]);
    var modelPackage = modelStore.InspectVivF(args[2]);
    var objectCount = 0;
    var validElfCount = 0;
    var geometrySymbolCount = 0;
    var boundsCount = 0;
    var boundsRejectedCount = 0;
    var invalidObjectCount = 0;
    for (var blockIndex = 0; blockIndex < checked((int)modelPackage.EntryCount); blockIndex++)
    {
        using var modelBlock = modelStore.OpenVivFBlock(args[2], blockIndex);
        var expandedModelBlock = RefPackDecoder.Decode(
            modelBlock.Data,
            checked((int)modelBlock.Record.ExpandedSize));
        var nestedModelArchive = BigArchiveReader.Inspect(expandedModelBlock.Data);
        foreach (var objectEntry in nestedModelArchive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".o", StringComparison.OrdinalIgnoreCase)))
        {
            objectCount++;
            try
            {
                var objectData = BigArchiveReader.ReadEntry(expandedModelBlock.Data, objectEntry.Name);
                var objectInspection = EaglObjectReader.Inspect(objectData);
                validElfCount++;
                if (objectInspection.GeometrySymbols.Count > 0)
                {
                    geometrySymbolCount++;
                }
                if (objectInspection.Bounds is { } objectBounds)
                {
                    boundsCount++;
                    if (boundsCount <= 12)
                    {
                        var min = objectBounds.Minimum;
                        var max = objectBounds.Maximum;
                        Console.WriteLine(FormattableString.Invariant(
                            $"bbox: block={blockIndex} | {objectEntry.Name} | min=({min.X:G6},{min.Y:G6},{min.Z:G6}) | max=({max.X:G6},{max.Y:G6},{max.Z:G6}) | symbol={objectInspection.BoundsSymbol}"));
                    }
                }
                else if (objectInspection.BoundsIssue is not null)
                {
                    boundsRejectedCount++;
                }
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
            {
                invalidObjectCount++;
                if (invalidObjectCount <= 8)
                {
                    Console.WriteLine($"invalid_o: block={blockIndex} | {objectEntry.Name} | {exception.Message}");
                }
            }
        }
    }

    Console.WriteLine($"{GameIdentity.Name} EAGL object metadata inventory");
    Console.WriteLine($"package: {args[2]}");
    Console.WriteLine($"blocks: {modelPackage.EntryCount}");
    Console.WriteLine($"o_entries: {objectCount}");
    Console.WriteLine($"valid_elf32_objects: {validElfCount}");
    Console.WriteLine($"objects_with_geometry_symbols: {geometrySymbolCount}");
    Console.WriteLine($"decoded_bounding_boxes: {boundsCount}");
    Console.WriteLine($"rejected_bounding_boxes: {boundsRejectedCount}");
    Console.WriteLine($"invalid_objects: {invalidObjectCount}");
    Console.WriteLine("note: __BBOX yields bounds metadata only; this does not decode vertices, rigging, or animation.");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG inspection");
    return 0;
}

if (string.Equals(args[0], "big-model-inventory", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc big-model-inventory <caminho-do-iso> <arquivo.big> [--verbose]");
        return 2;
    }

    using var bigModelStore = new GameAssetStore(args[1]);
    using var bigModelArchiveStream = bigModelStore.OpenRaw(args[2]);
    var bigModelArchive = BigArchiveReader.Inspect(bigModelArchiveStream);
    var verboseModelInventory = args.Length >= 4 && string.Equals(args[3], "--verbose", StringComparison.OrdinalIgnoreCase);
    var extensionCounts = bigModelArchive.Entries
        .GroupBy(entry => Path.GetExtension(entry.Name).ToLowerInvariant() is { Length: > 0 } extension ? extension : "(sem extensão)")
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.Ordinal)
        .Select(group => $"{group.Key}={group.Count()}");
    if (verboseModelInventory)
    {
        Console.WriteLine($"entry_extensions: {string.Join(", ", extensionCounts)}");
        foreach (var entry in bigModelArchive.Entries.Take(12))
        {
            Console.WriteLine($"entry: {entry.Name} | size={entry.Size}");
        }
    }
    var objectCount = 0;
    var validElfCount = 0;
    var elfMachineCounts = new Dictionary<ushort, int>();
    var geometrySymbolCount = 0;
    var boundsCount = 0;
    var boundsRejectedCount = 0;
    var vertexBufferStreamCount = 0;
    var float3StreamCount = 0;
    var bboxMatchedFloat3Count = 0;
    var float2StreamCount = 0;
    var float2NearUnitRangeCount = 0;
    var bboxMatchedStreamsByType = new Dictionary<string, int>(StringComparer.Ordinal);
    var printedVertexBufferSample = false;
    var modelPrimitiveCount = 0;
    var modelPrimitivesWithPositionCandidates = 0;
    var primitivesWithUniquePositionCandidate = 0;
    var primitivesWithConsistentVertexLayout = 0;
    var primitivesWithExplicitVertexFetchBindings = 0;
    var boundFloatProfileCount = 0;
    var boundFloatProfilesBySlot = new Dictionary<uint, int>();
    var boundFloatProfilesBySlotAndComponents = new Dictionary<(uint Slot, int Components), int>();
    var primitivesWithFiniteFloat2Slot1 = 0;
    var primitivesWithNearUnitFloat2Slot1 = 0;
    var primitivesWithClampStateAndFloat2Slot1 = 0;
    var primitivesWithShapeNameAndFloat2Slot1 = 0;
    var primitivesWithStateAssignments = 0;
    var triangleStripPrimitivesWithCandidates = 0;
    var triangleStripPrimitivesWithUniqueCandidate = 0;
    var triangleStripPrimitivesWithConsistentLayout = 0;
    var primitiveTypesWithCandidates = new Dictionary<string, int>(StringComparer.Ordinal);
    var completeRenderCommandStreams = 0;
    var incompleteRenderCommandStreams = 0;
    var renderCommandCount = 0;
    var renderCommandIds = new Dictionary<ushort, int>();
    var morphObjectCount = 0;
    var morphShapeNameCount = 0;
    var morphAttributeNameCount = 0;
    var morphShapePointerCount = 0;
    var morphAttributePointerCount = 0;
    var morphPointerCount = 0;
    var morphResolvedPointerCount = 0;
    var morphPayloadWindowCount = 0;
    var morphRelocationFieldSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var printedModelPrimitiveSample = false;
    var oversizedCount = 0;
    var invalidObjectCount = 0;
    var nestedBigCount = 0;
    var validNestedBigCount = 0;
    var nestedEntryCount = 0;
    var invalidNestedBigCount = 0;
    var nestedExtensions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    IReadOnlyList<EaglVertexBufferStream> CountVertexStreams(
        byte[] data,
        EaglObjectInspection inspection)
    {
        var streams = EaglVertexBufferReader.Inspect(data, inspection.Elf);
        vertexBufferStreamCount += streams.Count;
        var measured = new List<(EaglVertexBufferStream Stream, EaglAxisAlignedBounds Bounds, bool MatchesBounds)>();
        var measuredFloat2 = new List<(EaglVertexBufferStream Stream, EaglFloat2Range Range, bool NearUnitRange)>();
        foreach (var stream in streams)
        {
            if (EaglVertexBufferReader.TryMeasureFloat2(data, inspection.Elf, stream, out var float2Range) &&
                float2Range is not null)
            {
                float2StreamCount++;
                var nearUnitRange = float2Range.IsNearUnitRange();
                if (nearUnitRange)
                {
                    float2NearUnitRangeCount++;
                }
                measuredFloat2.Add((stream, float2Range, nearUnitRange));
            }

            if (!EaglVertexBufferReader.TryMeasureFloat3(data, inspection.Elf, stream, out var streamBounds) ||
                streamBounds is null)
            {
                continue;
            }

            float3StreamCount++;
            var matchesBounds = inspection.Bounds is not null &&
                                EaglVertexBufferReader.FitsBounds(streamBounds, inspection.Bounds);
            if (matchesBounds)
            {
                bboxMatchedFloat3Count++;
                var marker = stream.SymbolName.IndexOf("__GPvb_", StringComparison.Ordinal);
                var separator = marker < 0 ? -1 : stream.SymbolName.IndexOf('_', marker + 7);
                var streamType = separator < 0
                    ? "other"
                    : stream.SymbolName[(marker + 7)..separator];
                var category = separator < 0 ? "other" : $"__GPvb_{streamType}";
                bboxMatchedStreamsByType[category] = bboxMatchedStreamsByType.GetValueOrDefault(category) + 1;
            }
            measured.Add((stream, streamBounds, matchesBounds));
        }

        if (verboseModelInventory && !printedVertexBufferSample && streams.Count > 0)
        {
            printedVertexBufferSample = true;
            Console.WriteLine($"sample_vertex_buffers: validated={streams.Count} finite_float2={measuredFloat2.Count} near_unit_range_float2={measuredFloat2.Count(item => item.NearUnitRange)} finite_float3={measured.Count} bbox_matches={measured.Count(item => item.MatchesBounds)}");
            foreach (var stream in streams.Take(64))
            {
                Console.WriteLine(FormattableString.Invariant(
                    $"sample_vb_descriptor: {stream.SymbolName} | elements={stream.ElementCount} | stride={stream.Stride} | flags=0x{stream.Flags:X} | record=0x{stream.BufferRecordOffset:X} | descriptor=0x{stream.DescriptorOffset:X} | data=0x{stream.DataOffset:X}"));
            }
            foreach (var item in measured.Take(16))
            {
                var min = item.Bounds.Minimum;
                var max = item.Bounds.Maximum;
                Console.WriteLine(FormattableString.Invariant(
                    $"sample_vertex_stream: {item.Stream.SymbolName} | elements={item.Stream.ElementCount} | stride={item.Stream.Stride} | data=0x{item.Stream.DataOffset:X} | min=({min.X:G6},{min.Y:G6},{min.Z:G6}) | max=({max.X:G6},{max.Y:G6},{max.Z:G6}) | bbox_match={item.MatchesBounds}"));
            }
            foreach (var item in measuredFloat2.Take(16))
            {
                Console.WriteLine(FormattableString.Invariant(
                    $"sample_float2_stream: {item.Stream.SymbolName} | elements={item.Stream.ElementCount} | stride={item.Stream.Stride} | data=0x{item.Stream.DataOffset:X} | min=({item.Range.MinimumX:G6},{item.Range.MinimumY:G6}) | max=({item.Range.MaximumX:G6},{item.Range.MaximumY:G6}) | near_unit_range={item.NearUnitRange}"));
            }
        }

        return streams;
    }

    void CountModelPrimitiveCandidates(
        byte[] data,
        EaglObjectInspection inspection,
        IReadOnlyList<EaglVertexBufferStream> streams)
    {
        var primitives = EaglModelReader.Inspect(data, inspection.Elf, inspection.Bounds, streams);
        modelPrimitiveCount += primitives.Count;
        foreach (var primitive in primitives)
        {
            if (primitive.RenderCommandStream.IsComplete)
            {
                completeRenderCommandStreams++;
            }
            else
            {
                incompleteRenderCommandStreams++;
            }
            renderCommandCount += primitive.RenderCommandStream.Commands.Count;
            foreach (var command in primitive.RenderCommandStream.Commands)
            {
                renderCommandIds[command.CommandId] = renderCommandIds.GetValueOrDefault(command.CommandId) + 1;
            }

            if (primitive.BoundsMatchingVertexBuffers.Count == 0)
            {
                continue;
            }

            modelPrimitivesWithPositionCandidates++;
            if (primitive.BoundsMatchingVertexBuffers.Count == 1)
            {
                primitivesWithUniquePositionCandidate++;
            }
            if (primitive.HasConsistentVertexLayout)
            {
                primitivesWithConsistentVertexLayout++;
            }
            if (primitive.VertexFetchBindings.Count == 5)
            {
                primitivesWithExplicitVertexFetchBindings++;
                foreach (var binding in primitive.VertexFetchBindings)
                {
                    if (EaglVertexBufferReader.TryMeasurePackedFloatProfile(
                            data,
                            inspection.Elf,
                            binding.Stream,
                            out var profile) &&
                        profile is not null)
                    {
                        boundFloatProfileCount++;
                        boundFloatProfilesBySlot[binding.FetchSlot.Slot] =
                            boundFloatProfilesBySlot.GetValueOrDefault(binding.FetchSlot.Slot) + 1;
                        var key = (binding.FetchSlot.Slot, profile.ComponentCount);
                        boundFloatProfilesBySlotAndComponents[key] =
                            boundFloatProfilesBySlotAndComponents.GetValueOrDefault(key) + 1;
                    }
                }
                var slot1Binding = primitive.VertexFetchBindings.SingleOrDefault(binding => binding.FetchSlot.Slot == 1);
                if (slot1Binding is not null &&
                    EaglVertexBufferReader.TryMeasureFloat2(data, inspection.Elf, slot1Binding.Stream, out var slot1Range) &&
                    slot1Range is not null)
                {
                    primitivesWithFiniteFloat2Slot1++;
                    if (slot1Range.IsNearUnitRange())
                    {
                        primitivesWithNearUnitFloat2Slot1++;
                        var hasClampState = primitive.StateAssignments.Any(assignment =>
                            string.Equals(assignment.Name, "XBOXEXTOBJ_SetClampU", StringComparison.Ordinal) ||
                            string.Equals(assignment.Name, "XBOXEXTOBJ_SetClampV", StringComparison.Ordinal));
                        if (hasClampState)
                        {
                            primitivesWithClampStateAndFloat2Slot1++;
                        }
                        if (primitive.StateAssignments.Any(assignment =>
                                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal)))
                        {
                            primitivesWithShapeNameAndFloat2Slot1++;
                        }
                    }
                }
            }
            if (primitive.StateAssignments.Count > 0)
            {
                primitivesWithStateAssignments++;
            }
            if (primitive.PrimitiveType is { } type)
            {
                primitiveTypesWithCandidates[type] = primitiveTypesWithCandidates.GetValueOrDefault(type) + 1;
                if (string.Equals(type, "PT_TRIANGLESTRIP", StringComparison.Ordinal))
                {
                    triangleStripPrimitivesWithCandidates++;
                    if (primitive.BoundsMatchingVertexBuffers.Count == 1)
                    {
                        triangleStripPrimitivesWithUniqueCandidate++;
                    }
                    if (primitive.HasConsistentVertexLayout)
                    {
                        triangleStripPrimitivesWithConsistentLayout++;
                    }
                }
            }
        }

        if (verboseModelInventory && !printedModelPrimitiveSample && primitives.Count > 0)
        {
            printedModelPrimitiveSample = true;
            foreach (var primitive in primitives.Take(32))
            {
                Console.WriteLine(FormattableString.Invariant(
                    $"sample_model_primitive: layer={primitive.LayerIndex} primitive={primitive.PrimitiveIndex} | type={primitive.PrimitiveType ?? "unknown"} | layout={primitive.HasConsistentVertexLayout} | fetch_slots={string.Join(",", primitive.VertexFetchSlots.OrderBy(slot => slot.Slot).Select(slot => $"{slot.Slot}:aux=0x{slot.AuxiliaryValue:X}:stride={slot.Stride}"))} | bindings={string.Join(",", primitive.VertexFetchBindings.OrderBy(binding => binding.FetchSlot.Slot).Select(binding => $"{binding.FetchSlot.Slot}:{binding.Stream.SymbolName}:data=0x{binding.Stream.DataOffset:X}:bytes=0x{binding.Stream.ByteLength:X}:flags=0x{binding.Stream.Flags:X}"))} | state_assignments={string.Join(";", primitive.StateAssignments.Select(assignment => $"{assignment.Name}={assignment.Value}"))} | state={primitive.PrimitiveStateSymbol ?? "none"} | vb_candidates={string.Join(",", primitive.BoundsMatchingVertexBuffers.Select(stream => stream.SymbolName))}"));
            }
        }
    }

    void CountMorphEvidence(byte[] data, EaglObjectInspection inspection)
    {
        var morph = EaglMorphReader.Inspect(data, inspection.Elf);
        if (!morph.HasMorphSymbol)
        {
            return;
        }

        morphObjectCount++;
        morphShapeNameCount += morph.ShapeNames.Count;
        morphAttributeNameCount += morph.AttributeNames.Count;
        morphShapePointerCount += morph.ShapePointers.Count;
        morphAttributePointerCount += morph.AttributePointers.Count;
        morphPointerCount += morph.InternalPointers.Count;
        morphResolvedPointerCount += morph.InternalPointers.Count(pointer => pointer.TargetString is not null);
        var payloadWindows = EaglMorphReader.InspectPayloadWindows(data, inspection.Elf);
        morphPayloadWindowCount += payloadWindows.Count;
        var relocationSignature = string.Join(",", payloadWindows
            .SelectMany(window => window.RelocationFieldOffsets)
            .Distinct()
            .OrderBy(offset => offset)
            .Select(offset => $"0x{offset:X}"));
        morphRelocationFieldSignatures[relocationSignature] =
            morphRelocationFieldSignatures.GetValueOrDefault(relocationSignature) + 1;
    }

    foreach (var objectEntry in bigModelArchive.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".o", StringComparison.OrdinalIgnoreCase)))
    {
        objectCount++;
        if (objectEntry.Size > 64 * 1024 * 1024)
        {
            oversizedCount++;
            continue;
        }

        try
        {
            using var objectStream = bigModelStore.OpenBigEntry(args[2], objectEntry.Name);
            var objectData = new byte[checked((int)objectStream.Length)];
            objectStream.ReadExactly(objectData);
            var objectInspection = EaglObjectReader.Inspect(objectData);
            validElfCount++;
            elfMachineCounts[objectInspection.Elf.Machine] = elfMachineCounts.GetValueOrDefault(objectInspection.Elf.Machine) + 1;
            CountMorphEvidence(objectData, objectInspection);
            var objectVertexStreams = CountVertexStreams(objectData, objectInspection);
            CountModelPrimitiveCandidates(objectData, objectInspection, objectVertexStreams);
            if (objectInspection.GeometrySymbols.Count > 0)
            {
                geometrySymbolCount++;
            }
            if (objectInspection.Bounds is { } objectBounds)
            {
                boundsCount++;
                if (boundsCount <= 12)
                {
                    var min = objectBounds.Minimum;
                    var max = objectBounds.Maximum;
                    Console.WriteLine(FormattableString.Invariant(
                        $"bbox: {objectEntry.Name} | min=({min.X:G6},{min.Y:G6},{min.Z:G6}) | max=({max.X:G6},{max.Y:G6},{max.Z:G6}) | symbol={objectInspection.BoundsSymbol}"));
                }
            }
            else if (objectInspection.BoundsIssue is not null)
            {
                boundsRejectedCount++;
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
        {
            invalidObjectCount++;
            if (invalidObjectCount <= 8)
            {
                Console.WriteLine($"invalid_o: {objectEntry.Name} | {exception.Message}");
            }
        }
    }

    foreach (var nestedBigEntry in bigModelArchive.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
    {
        nestedBigCount++;
        if (nestedBigEntry.Size > 64 * 1024 * 1024)
        {
            oversizedCount++;
            continue;
        }

        try
        {
            using var nestedBigStream = bigModelStore.OpenBigEntry(args[2], nestedBigEntry.Name);
            var compressedNestedData = new byte[checked((int)nestedBigStream.Length)];
            nestedBigStream.ReadExactly(compressedNestedData);
            using var compressedNestedStream = new MemoryStream(compressedNestedData, writable: false);
            var decodedNested = RefPackDecoder.Decode(
                compressedNestedStream,
                RefPackDecoder.ReadDeclaredSize(compressedNestedData));
            var nestedArchive = BigArchiveReader.Inspect(decodedNested.Data);
            validNestedBigCount++;
            nestedEntryCount += nestedArchive.Entries.Count;
            if (verboseModelInventory && validNestedBigCount == 1)
            {
                foreach (var sampleEntry in nestedArchive.Entries)
                {
                    var sampleBytes = BigArchiveReader.ReadEntry(decodedNested.Data, sampleEntry.Name);
                    Console.WriteLine($"sample_nested_asset: {nestedBigEntry.Name}/{sampleEntry.Name} | size={sampleEntry.Size} | first_bytes={Convert.ToHexString(sampleBytes.AsSpan(0, Math.Min(64, sampleBytes.Length)))}");
                }
            }
            foreach (var nestedEntry in nestedArchive.Entries)
            {
                var extension = Path.GetExtension(nestedEntry.Name);
                extension = extension.Length == 0 ? "(sem extensão)" : extension.ToLowerInvariant();
                nestedExtensions[extension] = nestedExtensions.GetValueOrDefault(extension) + 1;
                if (!string.Equals(extension, ".o", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(extension, ".ord", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                objectCount++;
                try
                {
                    var objectData = BigArchiveReader.ReadEntry(decodedNested.Data, nestedEntry.Name);
                    EaglObjectInspection objectInspection;
                    if (string.Equals(extension, ".ord", StringComparison.OrdinalIgnoreCase))
                    {
                        var linkName = Path.ChangeExtension(nestedEntry.Name, ".orl");
                        var linkEntry = nestedArchive.Entries.FirstOrDefault(entry =>
                            string.Equals(entry.Name, linkName, StringComparison.OrdinalIgnoreCase));
                        if (linkEntry is null)
                        {
                            throw new InvalidDataException($"EAGL companion is missing: {linkName}");
                        }
                        var linkData = BigArchiveReader.ReadEntry(decodedNested.Data, linkEntry.Name);
                        objectInspection = EaglObjectReader.Inspect(objectData, linkData);
                    }
                    else
                    {
                        objectInspection = EaglObjectReader.Inspect(objectData);
                    }
                    validElfCount++;
                    elfMachineCounts[objectInspection.Elf.Machine] = elfMachineCounts.GetValueOrDefault(objectInspection.Elf.Machine) + 1;
                    CountMorphEvidence(objectData, objectInspection);
                    var objectVertexStreams = CountVertexStreams(objectData, objectInspection);
                    CountModelPrimitiveCandidates(objectData, objectInspection, objectVertexStreams);
                    if (verboseModelInventory && validElfCount == 1)
                    {
                        Console.WriteLine($"sample_elf: {nestedBigEntry.Name}/{nestedEntry.Name} | machine={objectInspection.Elf.Machine} | sections={objectInspection.Elf.SectionCount} | symbols={objectInspection.Elf.Symbols.Count} | relocations={objectInspection.Elf.Relocations.Count}");
                        foreach (var section in objectInspection.Elf.Sections.Take(10))
                        {
                            Console.WriteLine($"sample_section: {section.Name} | type={section.Type} | size={section.Size}");
                        }
                        foreach (var symbol in objectInspection.Elf.Symbols
                                     .Where(symbol => !string.IsNullOrEmpty(symbol.Name))
                                     .Take(20))
                        {
                            Console.WriteLine($"sample_symbol: {symbol.Name} | value=0x{symbol.Value:X} | size={symbol.Size} | section={symbol.SectionIndex}");
                        }
                        foreach (var modelSymbol in objectInspection.Elf.Symbols
                                     .Where(symbol => symbol.Name.StartsWith("__Model:::", StringComparison.Ordinal))
                                     .Take(1))
                        {
                            if (modelSymbol.SectionIndex == 0 || modelSymbol.SectionIndex >= objectInspection.Elf.Sections.Count)
                            {
                                continue;
                            }
                            var modelSection = objectInspection.Elf.Sections[modelSymbol.SectionIndex];
                            var modelOffset = (ulong)modelSection.Offset + modelSymbol.Value;
                            if (modelSection.Type == 8 || (ulong)modelSymbol.Value + 0xD0 > modelSection.Size ||
                                modelOffset + 0xD0 > (ulong)objectData.Length)
                            {
                                continue;
                            }
                            var layerCount = BinaryPrimitives.ReadUInt32LittleEndian(objectData.AsSpan(checked((int)modelOffset + 0x9C), 4));
                            var layerNamesPointer = BinaryPrimitives.ReadUInt32LittleEndian(objectData.AsSpan(checked((int)modelOffset + 0xA0), 4));
                            var layersPointer = BinaryPrimitives.ReadUInt32LittleEndian(objectData.AsSpan(checked((int)modelOffset + 0xCC), 4));
                            var modelRelocations = objectInspection.Elf.Relocations
                                .Where(relocation => relocation.TargetSectionIndex == modelSymbol.SectionIndex &&
                                                     relocation.Offset >= modelSymbol.Value + 0x9C &&
                                                     relocation.Offset <= modelSymbol.Value + 0xCC)
                                .Select(relocation => $"0x{relocation.Offset:X}:{relocation.SymbolName}");
                            Console.WriteLine(FormattableString.Invariant(
                                $"sample_model_layout: {modelSymbol.Name} | value=0x{modelSymbol.Value:X} | layers={layerCount} | layerNames=0x{layerNamesPointer:X} | layerData=0x{layersPointer:X} | refs={string.Join(";", modelRelocations)}"));
                            if (layersPointer < modelSection.Size)
                            {
                                var layerDataFileOffset = checked((int)((ulong)modelSection.Offset + layersPointer));
                                var layerPreviewLength = checked((int)Math.Min(64UL, (ulong)modelSection.Size - layersPointer));
                                Console.WriteLine($"sample_model_layer_data: {Convert.ToHexString(objectData.AsSpan(layerDataFileOffset, layerPreviewLength))}");

                                var layersHeader = ReadObjectSectionWord(objectData, modelSection, layersPointer);
                                var oldLayerLayout = layersHeader == 0xA0000000;
                                var layerCursor = layersPointer + 4u + (oldLayerLayout ? 8u : 0u);
                                Console.WriteLine($"sample_model_layer_format: {(oldLayerLayout ? "legacy" : "current")} | header=0x{layersHeader:X8}");
                                for (var layerIndex = 0u; layerIndex < Math.Min(layerCount, 8u); layerIndex++)
                                {
                                    if (oldLayerLayout)
                                    {
                                        layerCursor = checked(layerCursor + 8u);
                                    }
                                    var primitiveCount = ReadObjectSectionWord(objectData, modelSection, layerCursor);
                                    layerCursor = checked(layerCursor + 4u);
                                    Console.WriteLine($"sample_model_layer: index={layerIndex} | primitives={primitiveCount}");
                                    for (var primitiveIndex = 0u; primitiveIndex < Math.Min(primitiveCount, 16u); primitiveIndex++)
                                    {
                                        var descriptorField = checked(layerCursor + (oldLayerLayout ? primitiveIndex * 8u + 4u : primitiveIndex * 4u));
                                        var descriptorOffset = ReadObjectSectionWord(objectData, modelSection, descriptorField);
                                        var renderMethodOffset = ReadObjectSectionWord(objectData, modelSection, descriptorOffset);
                                        var globalParametersOffset = checked(descriptorOffset + 4u);
                                        var commandStreamOffset = ReadObjectSectionWord(objectData, modelSection, renderMethodOffset);
                                        var codeReferenceOffset = ReadObjectSectionWord(objectData, modelSection, checked(renderMethodOffset + 8u));
                                        var parameterWords = Enumerable.Range(0, 4)
                                            .Select(wordIndex => ReadObjectSectionWord(objectData, modelSection, checked(globalParametersOffset + (uint)(wordIndex * 4))))
                                            .Select(value => $"0x{value:X}");
                                        Console.WriteLine(FormattableString.Invariant(
                                            $"sample_render_descriptor: layer={layerIndex} primitive={primitiveIndex} | descriptor=0x{descriptorOffset:X} | rm=0x{renderMethodOffset:X} | gp=0x{globalParametersOffset:X} | commands=0x{commandStreamOffset:X} | codeRef=0x{codeReferenceOffset:X} | gpWords={string.Join(",", parameterWords)}"));
                                        var globalParameterRelocations = objectInspection.Elf.Relocations
                                            .Where(relocation => relocation.TargetSectionIndex == modelSymbol.SectionIndex &&
                                                                 relocation.Offset >= globalParametersOffset &&
                                                                 relocation.Offset < globalParametersOffset + 16)
                                            .Select(relocation => $"0x{relocation.Offset:X}:{relocation.SymbolName}:type={relocation.Type}");
                                        Console.WriteLine($"sample_render_gp_relocations: layer={layerIndex} primitive={primitiveIndex} | refs={string.Join(";", globalParameterRelocations)}");

                                        var commandOffset = 0u;
                                        for (var commandIndex = 0; commandIndex < 24; commandIndex++)
                                        {
                                            var commandHeaderOffset = checked(commandStreamOffset + commandOffset);
                                            var commandFileOffset = (ulong)modelSection.Offset + commandHeaderOffset;
                                            if (commandHeaderOffset + 4u > modelSection.Size || commandFileOffset + 4u > (ulong)objectData.Length)
                                            {
                                                break;
                                            }
                                            var commandHeader = objectData.AsSpan(checked((int)commandFileOffset), 4);
                                            var commandSize = BinaryPrimitives.ReadUInt16LittleEndian(commandHeader[..2]);
                                            var commandId = BinaryPrimitives.ReadUInt16LittleEndian(commandHeader[2..4]);
                                            Console.WriteLine($"sample_render_command: layer={layerIndex} primitive={primitiveIndex} | index={commandIndex} | id={commandId} | size_words={commandSize}");
                                            if (commandId == 0 || commandSize == 0)
                                            {
                                                break;
                                            }
                                            var commandByteLength = checked((uint)commandSize * 4u);
                                            if (commandHeaderOffset + commandByteLength <= modelSection.Size &&
                                                commandFileOffset + commandByteLength <= (ulong)objectData.Length)
                                            {
                                                var commandBytes = objectData.AsSpan(
                                                    checked((int)commandFileOffset),
                                                    checked((int)Math.Min(commandByteLength, 32u)));
                                                var commandRelocations = objectInspection.Elf.Relocations
                                                    .Where(relocation => relocation.TargetSectionIndex == modelSymbol.SectionIndex &&
                                                                         relocation.Offset >= commandHeaderOffset &&
                                                                         relocation.Offset < commandHeaderOffset + commandByteLength)
                                                    .Select(relocation => $"0x{relocation.Offset:X}:{relocation.SymbolName}");
                                                Console.WriteLine($"sample_render_command_data: layer={layerIndex} primitive={primitiveIndex} | raw={Convert.ToHexString(commandBytes)} | reloc={string.Join(";", commandRelocations)}");
                                            }
                                            commandOffset = checked(commandOffset + commandSize * 4u);
                                        }
                                        if (oldLayerLayout && primitiveIndex + 1 < primitiveCount)
                                        {
                                            continue;
                                        }
                                    }
                                    layerCursor = checked(layerCursor + primitiveCount * (oldLayerLayout ? 8u : 4u));
                                }
                            }
                        }
                        foreach (var symbol in objectInspection.Elf.Symbols
                                     .Where(symbol => symbol.Name.StartsWith("__VB", StringComparison.Ordinal) ||
                                                      symbol.Name.StartsWith("__RM", StringComparison.Ordinal))
                                     .Take(12))
                        {
                            if (symbol.SectionIndex == 0 || symbol.SectionIndex >= objectInspection.Elf.Sections.Count)
                            {
                                continue;
                            }
                            var symbolSection = objectInspection.Elf.Sections[symbol.SectionIndex];
                            var symbolFileOffset = (ulong)symbolSection.Offset + symbol.Value;
                            if (symbolSection.Type == 8 || (ulong)symbol.Value >= symbolSection.Size ||
                                symbolFileOffset >= (ulong)objectData.Length)
                            {
                                continue;
                            }
                            var sampleLength = checked((int)Math.Min(32UL,
                                Math.Min((ulong)objectData.Length - symbolFileOffset, (ulong)symbolSection.Size - symbol.Value)));
                            var raw = Convert.ToHexString(objectData.AsSpan(checked((int)symbolFileOffset), sampleLength));
                            var nearbyRelocations = objectInspection.Elf.Relocations
                                .Where(relocation => relocation.TargetSectionIndex == symbol.SectionIndex &&
                                                     relocation.Offset >= symbol.Value &&
                                                     (ulong)relocation.Offset < (ulong)symbol.Value + 32)
                                .Select(relocation => $"0x{relocation.Offset:X}:{relocation.SymbolName}");
                            Console.WriteLine($"sample_geometry_record: {symbol.Name} | value=0x{symbol.Value:X} | raw={raw} | reloc={string.Join(";", nearbyRelocations)}");
                            if (symbol.Name.StartsWith("__VB", StringComparison.Ordinal) && sampleLength >= 20)
                            {
                                var descriptor = objectData.AsSpan(checked((int)symbolFileOffset), 20);
                                var vertexOffset = BinaryPrimitives.ReadUInt32LittleEndian(descriptor[..4]);
                                var vertexBytes = BinaryPrimitives.ReadUInt32LittleEndian(descriptor[4..8]);
                                var vertexStride = BinaryPrimitives.ReadUInt32LittleEndian(descriptor[8..12]);
                                var vertexFlags = BinaryPrimitives.ReadUInt32LittleEndian(descriptor[12..16]);
                                if (vertexOffset < symbolSection.Size &&
                                    vertexBytes <= symbolSection.Size - vertexOffset &&
                                    vertexStride > 0)
                                {
                                    var vertexFileOffset = checked((int)((ulong)symbolSection.Offset + vertexOffset));
                                    var vertexPreviewLength = checked((int)Math.Min(36u, vertexBytes));
                                    var vertexPreview = Convert.ToHexString(objectData.AsSpan(vertexFileOffset, vertexPreviewLength));
                                    Console.WriteLine(FormattableString.Invariant(
                                        $"sample_vb_payload: {symbol.Name} | offset=0x{vertexOffset:X} | bytes={vertexBytes} | stride={vertexStride} | count={vertexBytes / vertexStride} | flags={vertexFlags} | first={vertexPreview}"));
                                    var secondaryOffset = BinaryPrimitives.ReadUInt32LittleEndian(descriptor[16..20]);
                                    if (secondaryOffset < symbolSection.Size)
                                    {
                                        var secondaryFileOffset = checked((int)((ulong)symbolSection.Offset + secondaryOffset));
                                        var secondaryLength = checked((int)Math.Min(36UL, (ulong)symbolSection.Size - secondaryOffset));
                                        Console.WriteLine($"sample_vb_secondary: {symbol.Name} | offset=0x{secondaryOffset:X} | first={Convert.ToHexString(objectData.AsSpan(secondaryFileOffset, secondaryLength))}");
                                    }
                                }
                            }
                        }
                    }
                    if (objectInspection.GeometrySymbols.Count > 0)
                    {
                        geometrySymbolCount++;
                    }
                    if (objectInspection.Bounds is { } objectBounds)
                    {
                        boundsCount++;
                        if (boundsCount <= 12)
                        {
                            var min = objectBounds.Minimum;
                            var max = objectBounds.Maximum;
                            Console.WriteLine(FormattableString.Invariant(
                                $"bbox: {nestedBigEntry.Name}/{nestedEntry.Name} | min=({min.X:G6},{min.Y:G6},{min.Z:G6}) | max=({max.X:G6},{max.Y:G6},{max.Z:G6}) | symbol={objectInspection.BoundsSymbol}"));
                        }
                    }
                    else if (objectInspection.BoundsIssue is not null)
                    {
                        boundsRejectedCount++;
                    }
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
                {
                    invalidObjectCount++;
                    if (invalidObjectCount <= 8)
                    {
                        Console.WriteLine($"invalid_o: {nestedBigEntry.Name}/{nestedEntry.Name} | {exception.Message}");
                    }
                }
            }
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
        {
            invalidNestedBigCount++;
            if (invalidNestedBigCount <= 8)
            {
                Console.WriteLine($"invalid_nested_big: {nestedBigEntry.Name} | {exception.Message}");
            }
        }
    }

    Console.WriteLine($"{GameIdentity.Name} EAGL object metadata inventory");
    Console.WriteLine($"archive: {args[2]} | magic={bigModelArchive.Magic} | entries={bigModelArchive.FileCount}");
    Console.WriteLine($"nested_big_candidates: {nestedBigCount}");
    Console.WriteLine($"valid_nested_big: {validNestedBigCount}");
    Console.WriteLine($"nested_entries: {nestedEntryCount}");
    Console.WriteLine($"nested_entry_extensions: {string.Join(", ", nestedExtensions.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"model_elf_entries: {objectCount}");
    Console.WriteLine($"valid_elf32_objects: {validElfCount}");
    Console.WriteLine($"elf_machines: {string.Join(", ", elfMachineCounts.OrderBy(pair => pair.Key).Select(pair => $"0x{pair.Key:X4}={pair.Value}"))}");
    Console.WriteLine($"objects_with_geometry_symbols: {geometrySymbolCount}");
    Console.WriteLine($"decoded_bounding_boxes: {boundsCount}");
    Console.WriteLine($"rejected_bounding_boxes: {boundsRejectedCount}");
    Console.WriteLine($"validated_vertex_buffer_streams: {vertexBufferStreamCount}");
    Console.WriteLine($"finite_float3_streams: {float3StreamCount}");
    Console.WriteLine($"float3_streams_matching_object_bounds: {bboxMatchedFloat3Count}");
    Console.WriteLine($"finite_float2_streams: {float2StreamCount}");
    Console.WriteLine($"near_unit_range_float2_streams: {float2NearUnitRangeCount}");
    Console.WriteLine($"bbox_matched_float3_by_type: {string.Join(", ", bboxMatchedStreamsByType.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"model_render_primitives: {modelPrimitiveCount}");
    Console.WriteLine($"primitives_with_bounds_matching_vertex_candidates: {modelPrimitivesWithPositionCandidates}");
    Console.WriteLine($"primitives_with_unique_position_candidate: {primitivesWithUniquePositionCandidate}");
    Console.WriteLine($"primitives_with_consistent_vertex_layout: {primitivesWithConsistentVertexLayout}");
    Console.WriteLine($"primitives_with_explicit_vertex_fetch_bindings: {primitivesWithExplicitVertexFetchBindings}");
    Console.WriteLine($"bound_float_profiles: {boundFloatProfileCount}");
    Console.WriteLine($"bound_float_profiles_by_slot: {string.Join(",", boundFloatProfilesBySlot
        .OrderBy(pair => pair.Key)
        .Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"bound_float_profiles_by_slot_components: {string.Join(",", boundFloatProfilesBySlotAndComponents
        .OrderBy(pair => pair.Key.Slot)
        .ThenBy(pair => pair.Key.Components)
        .Select(pair => $"{pair.Key.Slot}/{pair.Key.Components}={pair.Value}"))}");
    Console.WriteLine($"primitives_with_finite_float2_slot1: {primitivesWithFiniteFloat2Slot1}");
    Console.WriteLine($"primitives_with_near_unit_float2_slot1: {primitivesWithNearUnitFloat2Slot1}");
    Console.WriteLine($"primitives_with_clamp_state_and_float2_slot1: {primitivesWithClampStateAndFloat2Slot1}");
    Console.WriteLine($"primitives_with_shapename_and_float2_slot1: {primitivesWithShapeNameAndFloat2Slot1}");
    Console.WriteLine($"primitives_with_state_assignments: {primitivesWithStateAssignments}");
    Console.WriteLine($"triangle_strip_primitives_with_candidates: {triangleStripPrimitivesWithCandidates}");
    Console.WriteLine($"triangle_strip_primitives_with_unique_candidate: {triangleStripPrimitivesWithUniqueCandidate}");
    Console.WriteLine($"triangle_strip_primitives_with_consistent_layout: {triangleStripPrimitivesWithConsistentLayout}");
    Console.WriteLine($"candidate_primitives_by_type: {string.Join(", ", primitiveTypesWithCandidates.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"complete_render_command_streams: {completeRenderCommandStreams}");
    Console.WriteLine($"incomplete_render_command_streams: {incompleteRenderCommandStreams}");
    Console.WriteLine($"render_commands: {renderCommandCount}");
    Console.WriteLine($"render_command_ids: {string.Join(",", renderCommandIds.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"morph_objects_with_named_symbol: {morphObjectCount}");
    Console.WriteLine($"morph_shape_names: {morphShapeNameCount}");
    Console.WriteLine($"morph_attribute_names: {morphAttributeNameCount}");
    Console.WriteLine($"morph_shape_pointers: {morphShapePointerCount}");
    Console.WriteLine($"morph_attribute_pointers: {morphAttributePointerCount}");
    Console.WriteLine($"morph_internal_pointers: {morphPointerCount}");
    Console.WriteLine($"morph_resolved_internal_pointers: {morphResolvedPointerCount}");
    Console.WriteLine($"morph_payload_windows: {morphPayloadWindowCount}");
    Console.WriteLine($"morph_relocation_field_signatures: {string.Join(" | ", morphRelocationFieldSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(8)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"oversized_objects_skipped: {oversizedCount}");
    Console.WriteLine($"invalid_objects: {invalidObjectCount}");
    Console.WriteLine($"invalid_nested_big: {invalidNestedBigCount}");
    Console.WriteLine("note: float2 profiles are numeric evidence only and are not assigned UV semantics; matching float3 streams are position candidates only; draw/index association, rigging, and animation are not yet decoded.");
    Console.WriteLine("mode: ISO read-only, bounded BIG entry streams");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-object-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-object-smoke <caminho-do-iso> <nested.big> <objeto.ord>");
        return 2;
    }

    using var objectCandidateStore = new GameAssetStore(args[1]);
    var objectCandidate = EaglCandidateAssetLoader.Load(
        objectCandidateStore,
        "data/models.big",
        args[2],
        args[3]);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL object candidate smoke");
    Console.WriteLine($"object: {objectCandidate.ObjectName}");
    Console.WriteLine($"groups: {objectCandidate.Mesh.Groups.Count}");
    Console.WriteLine($"vertices: {objectCandidate.Mesh.VertexCount}");
    Console.WriteLine($"triangles: {objectCandidate.Mesh.TriangleCount}");
    Console.WriteLine($"render_primitives: {objectCandidate.Primitives.Count}");
    Console.WriteLine($"states_with_shapename: {objectCandidate.Primitives.Count(primitive => primitive.StateAssignments.Any(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal)))}");
    Console.WriteLine($"state_names: {string.Join(",", objectCandidate.Primitives
        .SelectMany(primitive => primitive.StateAssignments)
        .Where(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal))
        .Select(assignment => assignment.Value)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))}");
    if (args.Any(argument => string.Equals(argument, "--commands", StringComparison.OrdinalIgnoreCase)))
    {
        foreach (var primitive in objectCandidate.Primitives)
        {
            var assignments = string.Join(";", primitive.StateAssignments
                .Select(assignment => $"{assignment.Name}={assignment.Value}"));
            Console.WriteLine($"eagl_commands: layer={primitive.LayerIndex} primitive={primitive.PrimitiveIndex} state={primitive.PrimitiveStateSymbol ?? "none"} assignments={assignments} type={primitive.PrimitiveType ?? "none"} complete={primitive.RenderCommandStream.IsComplete} issue={primitive.RenderCommandStream.Issue ?? "none"}");
            foreach (var command in primitive.RenderCommandStream.Commands)
            {
                Console.WriteLine($"eagl_command: primitive={primitive.PrimitiveIndex} index={command.Index} offset=0x{command.SectionOffset:X} id={command.CommandId} words={command.SizeWords} raw={Convert.ToHexString(command.RawBytes)} relocations={string.Join(",", command.RelocationSymbols)}");
            }
        }
    }
    Console.WriteLine("status: candidate geometry only; state-to-SHPX material is validated by eagl-material-smoke but not applied to GPU draw commands");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG, ELF/EAGL managed mesh");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-state-correspondence", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-state-correspondence <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? correspondenceReportPath = null;
    var correspondenceReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (correspondenceReportOption >= 0)
    {
        if (correspondenceReportOption + 1 >= args.Length ||
            args[correspondenceReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-candidate-state-correspondence ... --report <saida.tsv>");
            return 2;
        }

        correspondenceReportPath = Path.GetFullPath(args[correspondenceReportOption + 1]);
    }

    using var correspondenceStore = new GameAssetStore(args[1]);
    var correspondenceMaterials = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        correspondenceStore,
        "data/models.big");
    var correspondenceRows = new List<EaglPrimitiveStateCorrespondenceRow>();
    var correspondenceValidatedObjects = 0;
    var correspondenceRejectedObjects = 0;
    var correspondenceGeometryPrimitives = 0;
    var correspondenceStatePrimitives = 0;
    var correspondenceObjectSummaries = new List<(string ModelArchive, string ObjectName, int GeometryPrimitives, int StatePrimitives, int PairRows)>();
    foreach (var material in correspondenceMaterials
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                correspondenceStore,
                "data/models.big",
                material.ModelArchiveName,
                material.ModelObjectName);
            var objectRows = EaglPrimitiveStateCorrespondence.Compare(
                material.ModelArchiveName,
                material.ModelObjectName,
                candidate.Mesh,
                candidate.Primitives);
            var geometryCount = candidate.Mesh.Groups
                .Select(group => group.PrimitiveIndex)
                .Distinct()
                .Count();
            var stateCount = candidate.Primitives.Count(primitive => primitive.StateAssignments.Any(assignment =>
                string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal)));
            correspondenceValidatedObjects++;
            correspondenceGeometryPrimitives += geometryCount;
            correspondenceStatePrimitives += stateCount;
            correspondenceRows.AddRange(objectRows);
            correspondenceObjectSummaries.Add((
                material.ModelArchiveName,
                material.ModelObjectName,
                geometryCount,
                stateCount,
                objectRows.Count));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            correspondenceRejectedObjects++;
            Console.WriteLine($"invalid_object: {material.ModelArchiveName}/{material.ModelObjectName} | {exception.Message}");
        }
    }

    if (correspondenceReportPath is not null)
    {
        EaglPrimitiveStateCorrespondence.WriteReport(correspondenceReportPath, correspondenceRows);
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate/state correspondence smoke");
    Console.WriteLine("mode: ISO read-only, raw command/boundary comparison only");
    Console.WriteLine($"candidate_material_objects: {correspondenceMaterials.Count}");
    Console.WriteLine($"validated_objects: {correspondenceValidatedObjects}");
    Console.WriteLine($"rejected_objects: {correspondenceRejectedObjects}");
    Console.WriteLine($"geometry_primitives: {correspondenceGeometryPrimitives}");
    Console.WriteLine($"state_primitives_with_shapename: {correspondenceStatePrimitives}");
    Console.WriteLine($"pair_rows: {correspondenceRows.Count}");
    Console.WriteLine($"exact_raw_pairs: {correspondenceRows.Count(row => row.ExactRawBytes)}");
    Console.WriteLine($"exact_boundary_shape_pairs: {correspondenceRows.Count(row => row.ExactBoundaryShape)}");
    Console.WriteLine($"pairs_with_shared_relocation_symbols: {correspondenceRows.Count(row => row.SharedRelocationSymbolCount > 0)}");
    Console.WriteLine($"maximum_shared_relocation_symbols: {correspondenceRows.DefaultIfEmpty().Max(row => row?.SharedRelocationSymbolCount ?? 0)}");
    Console.WriteLine($"complete_geometry_streams: {correspondenceRows.Count(row => row.GeometryStreamComplete)}");
    Console.WriteLine($"complete_state_streams: {correspondenceRows.Count(row => row.StateStreamComplete)}");
    Console.WriteLine($"maximum_common_prefix_bytes: {correspondenceRows.DefaultIfEmpty().Max(row => row?.CommonPrefixBytes ?? 0)}");
    Console.WriteLine($"pair_set_sha256: {EaglPrimitiveStateCorrespondence.GetPairSetSha256(correspondenceRows)}");
    Console.WriteLine($"objects_without_state_or_geometry_pair_rows: {correspondenceObjectSummaries.Count(summary => summary.PairRows == 0)}");
    if (correspondenceReportPath is not null)
    {
        Console.WriteLine($"report: {correspondenceReportPath} | rows={correspondenceRows.Count}");
    }
    Console.WriteLine("status: correspondence evidence only; no SHAPENAME-to-mesh, material, UV, skinning, rigging, animation, or GPU binding assigned");
    return 0;
}

if (string.Equals(args[0], "eagl-state-geometry-adjacency-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-state-geometry-adjacency-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? adjacencyReportPath = null;
    var adjacencyReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (adjacencyReportOption >= 0)
    {
        if (adjacencyReportOption + 1 >= args.Length ||
            args[adjacencyReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-state-geometry-adjacency-audit ... --report <saida.tsv>");
            return 2;
        }

        adjacencyReportPath = Path.GetFullPath(args[adjacencyReportOption + 1]);
    }

    if (adjacencyReportPath is not null &&
        string.Equals(adjacencyReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de adjacência não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var adjacencyStore = new GameAssetStore(args[1]);
    var adjacencyMaterials = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        adjacencyStore,
        "data/models.big");
    var adjacencyRows = new List<EaglStateGeometryAdjacencyRow>();
    var adjacencyValidatedObjects = 0;
    var adjacencyRejectedObjects = 0;
    foreach (var material in adjacencyMaterials
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                adjacencyStore,
                "data/models.big",
                material.ModelArchiveName,
                material.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    material.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{material.ShapeName}'.");
            }

            adjacencyRows.Add(EaglStateGeometryAdjacencyAudit.Measure(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                candidate.Mesh,
                candidate.Primitives));
            adjacencyValidatedObjects++;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException or FileNotFoundException)
        {
            adjacencyRejectedObjects++;
            adjacencyRows.Add(EaglStateGeometryAdjacencyAudit.Rejected(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                exception.Message));
            Console.WriteLine($"invalid_object: {material.ModelArchiveName}/{material.ModelObjectName} | {exception.Message}");
        }
    }

    if (adjacencyReportPath is not null)
    {
        EaglStateGeometryAdjacencyAudit.WriteReport(adjacencyReportPath, adjacencyRows);
    }

    var adjacencyVerifiedRows = adjacencyRows.Where(row =>
        string.Equals(row.Status, "verified-control", StringComparison.Ordinal)).ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC EAGL state/geometry adjacency control");
    Console.WriteLine("mode: ISO read-only, primitive-index/layer observations only");
    Console.WriteLine($"candidate_material_objects: {adjacencyMaterials.Count}");
    Console.WriteLine($"validated_objects: {adjacencyValidatedObjects}");
    Console.WriteLine($"rejected_objects: {adjacencyRejectedObjects}");
    Console.WriteLine($"verified_rows: {adjacencyVerifiedRows.Length}");
    Console.WriteLine($"same_primitive_index_pairs: {adjacencyVerifiedRows.Sum(row => row.SamePrimitiveIndexPairs)}");
    Console.WriteLine($"adjacent_primitive_pairs: {adjacencyVerifiedRows.Sum(row => row.AdjacentPrimitivePairs)}");
    Console.WriteLine($"same_layer_pairs: {adjacencyVerifiedRows.Sum(row => row.SameLayerPairs)}");
    Console.WriteLine($"object_state_exact_rows: {adjacencyVerifiedRows.Count(row => row.ObjectStateExact)}");
    Console.WriteLine($"geometry_group_state_exact_rows: {adjacencyVerifiedRows.Count(row => row.GeometryGroupStateExact)}");
    if (adjacencyReportPath is not null)
    {
        Console.WriteLine($"report: {adjacencyReportPath} | rows={adjacencyRows.Count}");
    }
    Console.WriteLine("status: negative control only; adjacency is not promoted to SHAPENAME-to-mesh, material, UV, skinning, rigging, animation, or GPU binding");
    return adjacencyRejectedObjects == 0 && adjacencyValidatedObjects == adjacencyMaterials.Count ? 0 : 1;
}

if (string.Equals(args[0], "eagl-state-geometry-profile-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-state-geometry-profile-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? profileReportPath = null;
    var profileReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (profileReportOption >= 0)
    {
        if (profileReportOption + 1 >= args.Length ||
            args[profileReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-state-geometry-profile-audit ... --report <saida.tsv>");
            return 2;
        }

        profileReportPath = Path.GetFullPath(args[profileReportOption + 1]);
    }

    if (profileReportPath is not null &&
        string.Equals(profileReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de perfil não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var profileStore = new GameAssetStore(args[1]);
    var profileMaterials = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        profileStore,
        "data/models.big");
    var profileRows = new List<EaglStateGeometryProfileRow>();
    var profileSummaries = new List<EaglStateGeometryProfileSummary>();
    var profileValidatedObjects = 0;
    var profileRejectedObjects = 0;
    foreach (var material in profileMaterials
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                profileStore,
                "data/models.big",
                material.ModelArchiveName,
                material.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    material.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{material.ShapeName}'.");
            }

            var audit = EaglStateGeometryProfileAudit.Measure(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                candidate.Mesh,
                candidate.Primitives);
            profileRows.AddRange(audit.Rows);
            profileSummaries.Add(audit.Summary);
            profileValidatedObjects++;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException or FileNotFoundException)
        {
            profileRejectedObjects++;
            profileRows.Add(EaglStateGeometryProfileAudit.Rejected(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                exception.Message));
            Console.WriteLine($"invalid_object: {material.ModelArchiveName}/{material.ModelObjectName} | {exception.Message}");
        }
    }

    if (profileReportPath is not null)
    {
        EaglStateGeometryProfileAudit.WriteReport(profileReportPath, profileRows);
    }

    var profileVerifiedRows = profileRows
        .Where(row => string.Equals(row.Status, "opaque-profile-control", StringComparison.Ordinal))
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC EAGL state/geometry opaque profile control");
    Console.WriteLine("mode: ISO read-only, command sequence/count/byte observations only");
    Console.WriteLine($"candidate_material_objects: {profileMaterials.Count}");
    Console.WriteLine($"validated_objects: {profileValidatedObjects}");
    Console.WriteLine($"rejected_objects: {profileRejectedObjects}");
    Console.WriteLine($"geometry_groups: {profileSummaries.Sum(summary => summary.GeometryGroups)}");
    Console.WriteLine($"state_primitives_with_shapename: {profileSummaries.Sum(summary => summary.StatePrimitives)}");
    Console.WriteLine($"pair_rows: {profileVerifiedRows.Length}");
    Console.WriteLine($"same_layer_pairs: {profileVerifiedRows.Count(row => row.SameLayer)}");
    Console.WriteLine($"same_primitive_index_pairs: {profileVerifiedRows.Count(row => row.SamePrimitiveIndex)}");
    Console.WriteLine($"neighbor_primitive_index_pairs: {profileVerifiedRows.Count(row => row.NeighborPrimitiveIndex)}");
    Console.WriteLine($"exact_command_shape_pairs: {profileVerifiedRows.Count(row => row.ExactCommandShape)}");
    Console.WriteLine($"unique_state_profile_rows: {profileVerifiedRows.Count(row => row.StateProfileTieCount == 1)}");
    Console.WriteLine($"distinct_geometry_command_shapes: {profileVerifiedRows.Select(row => row.GeometryCommandShapeSha256).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"distinct_state_command_shapes: {profileVerifiedRows.Select(row => row.StateCommandShapeSha256).Distinct(StringComparer.Ordinal).Count()}");
    if (profileReportPath is not null)
    {
        Console.WriteLine($"report: {profileReportPath} | rows={profileRows.Count}");
    }
    Console.WriteLine("status: opaque profile control only; no SHAPENAME-to-mesh, material, UV, skinning, rigging, animation, or GPU binding assigned");
    return profileRejectedObjects == 0 && profileValidatedObjects == profileMaterials.Count ? 0 : 1;
}

if (string.Equals(args[0], "eagl-state-geometry-order-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-state-geometry-order-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? orderReportPath = null;
    var orderReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (orderReportOption >= 0)
    {
        if (orderReportOption + 1 >= args.Length ||
            args[orderReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-state-geometry-order-audit ... --report <saida.tsv>");
            return 2;
        }

        orderReportPath = Path.GetFullPath(args[orderReportOption + 1]);
    }

    if (orderReportPath is not null &&
        string.Equals(orderReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria de ordem não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var orderStore = new GameAssetStore(args[1]);
    var orderMaterials = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        orderStore,
        "data/models.big");
    var orderRows = new List<EaglStateGeometryOrderRow>();
    var orderSummaries = new List<EaglStateGeometryOrderSummary>();
    var orderValidatedObjects = 0;
    var orderRejectedObjects = 0;
    foreach (var material in orderMaterials
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                orderStore,
                "data/models.big",
                material.ModelArchiveName,
                material.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    material.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{material.ShapeName}'.");
            }

            var audit = EaglStateGeometryOrderAudit.Measure(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                candidate.Mesh,
                candidate.Primitives);
            orderRows.AddRange(audit.Rows);
            orderSummaries.Add(audit.Summary);
            orderValidatedObjects++;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException or FileNotFoundException)
        {
            orderRejectedObjects++;
            orderRows.Add(EaglStateGeometryOrderAudit.Rejected(
                material.ModelArchiveName,
                material.ModelObjectName,
                material.ShapeName,
                exception.Message));
            Console.WriteLine($"invalid_object: {material.ModelArchiveName}/{material.ModelObjectName} | {exception.Message}");
        }
    }

    if (orderReportPath is not null)
    {
        EaglStateGeometryOrderAudit.WriteReport(orderReportPath, orderRows);
    }

    var verifiedOrderRows = orderRows
        .Where(row => string.Equals(row.Status, "ordered-index-control", StringComparison.Ordinal))
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC EAGL state/geometry ordered-run control");
    Console.WriteLine("mode: ISO read-only, primitive-index interval observations only");
    Console.WriteLine($"candidate_material_objects: {orderMaterials.Count}");
    Console.WriteLine($"validated_objects: {orderValidatedObjects}");
    Console.WriteLine($"rejected_objects: {orderRejectedObjects}");
    Console.WriteLine($"geometry_candidate_groups: {orderSummaries.Sum(summary => summary.GeometryCandidateCount)}");
    Console.WriteLine($"state_primitives_with_shapename: {orderSummaries.Sum(summary => summary.StatePrimitiveCount)}");
    Console.WriteLine($"interval_rows: {verifiedOrderRows.Length}");
    Console.WriteLine($"nonempty_interval_rows: {verifiedOrderRows.Count(row => row.StatePrimitiveCount > 0)}");
    Console.WriteLine($"state_primitives_partitioned: {orderSummaries.Sum(summary => summary.StatePrimitivesPartitioned)}");
    Console.WriteLine($"state_primitives_unpartitioned: {orderSummaries.Sum(summary => summary.StatePrimitivesUnpartitioned)}");
    Console.WriteLine($"complete_state_intervals: {verifiedOrderRows.Count(row => row.StatePrimitiveCount > 0 && row.AllStateStreamsComplete)}");
    Console.WriteLine($"distinct_state_run_profiles: {verifiedOrderRows.Select(row => row.StateCommandProfileSha256).Where(value => !string.IsNullOrEmpty(value)).Distinct(StringComparer.Ordinal).Count()}");
    if (orderReportPath is not null)
    {
        Console.WriteLine($"report: {orderReportPath} | rows={orderRows.Count}");
    }
    Console.WriteLine("status: ordered-run control only; no SHAPENAME-to-mesh, material, UV, skinning, rigging, animation, or GPU binding assigned");
    return orderRejectedObjects == 0 && orderValidatedObjects == orderMaterials.Count ? 0 : 1;
}

if (string.Equals(args[0], "eagl-material-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-smoke <caminho-do-iso> <nested.big> <SHAPENAME> [objeto.ord]");
        return 2;
    }

    using var materialStore = new GameAssetStore(args[1]);
    var materialAsset = args.Length >= 5
        ? EaglShapeMaterialLoader.LoadForObject(
            materialStore,
            "data/models.big",
            args[2],
            args[4],
            args[3])
        : EaglShapeMaterialLoader.LoadFirstForShape(
            materialStore,
            "data/models.big",
            args[2],
            args[3]);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL shape material smoke");
    Console.WriteLine($"model_archive: {materialAsset.ModelArchiveName}");
    Console.WriteLine($"model_object: {materialAsset.ModelObjectName}");
    Console.WriteLine($"shapename: {materialAsset.ShapeName}");
    Console.WriteLine($"shape_image_archive: {materialAsset.ShapeImageArchiveName}");
    Console.WriteLine($"shpx_entry: {materialAsset.ShapeImageEntry.Name} | offset={materialAsset.ShapeImageEntry.Offset} | size={materialAsset.ShapeImageEntry.Size}");
    Console.WriteLine($"texture: {materialAsset.Texture.Width}x{materialAsset.Texture.Height} | format={materialAsset.Texture.PixelFormat} | rgba_bytes={materialAsset.Texture.DecodedRgba.Length}");
    Console.WriteLine("binding: same-trunk model state -> compressed SHPX -> exact internal entry; GPU material semantics remain unassigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, split ELF/EAGL and SHPX validation");
    return 0;
}

if (string.Equals(args[0], "eagl-material-batch-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-batch-smoke <caminho-do-iso>");
        return 2;
    }

    using var materialBatchStore = new GameAssetStore(args[1]);
    var materialBatchMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        materialBatchStore,
        "data/models.big");
    Console.WriteLine($"{GameIdentity.Name} PC EAGL same-trunk material batch smoke");
    Console.WriteLine($"verified_matches: {materialBatchMatches.Count}");
    Console.WriteLine($"distinct_model_archives: {materialBatchMatches.Select(match => match.ModelArchiveName).Distinct(StringComparer.OrdinalIgnoreCase).Count()}");
    Console.WriteLine($"distinct_shape_names: {string.Join(",", materialBatchMatches
        .Select(match => match.ShapeName)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(name => name, StringComparer.OrdinalIgnoreCase))}");
    foreach (var match in materialBatchMatches.Take(24))
    {
        Console.WriteLine($"match: {match.ModelArchiveName} | object={match.ModelObjectName} | shape={match.ShapeName} | texture={match.Texture.Width}x{match.Texture.Height} {match.Texture.PixelFormat}");
    }
    Console.WriteLine("rule: same-trunk .big/.xsh, normalized SHAPENAME and exact internal SHPX name; texture decode verified");
    Console.WriteLine("status: candidate material evidence only; no global UV/material/GPU draw semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, split ELF/EAGL and SHPX validation");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-asset-provenance-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-asset-provenance-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var candidateProvenanceIsoPath = Path.GetFullPath(args[1]);
    var candidateProvenanceReportPath = Path.Combine(
        Directory.GetParent(candidateProvenanceIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "eagl-candidate-asset-provenance-audit.tsv");
    var candidateProvenanceSummaryPath = Path.Combine(
        Directory.GetParent(candidateProvenanceIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "eagl-candidate-asset-provenance-audit-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            candidateProvenanceReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            candidateProvenanceSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if (string.Equals(candidateProvenanceIsoPath, candidateProvenanceReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(candidateProvenanceIsoPath, candidateProvenanceSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria de proveniência candidate não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var candidateProvenanceStore = new GameAssetStore(candidateProvenanceIsoPath);
    var candidateProvenanceRows = EaglCandidateAssetProvenanceAudit.Audit(
        candidateProvenanceStore,
        "data/models.big");
    var candidateProvenanceSummary = EaglCandidateAssetProvenanceAudit.Summarize(
        "data/models.big",
        candidateProvenanceRows);
    EaglCandidateAssetProvenanceAudit.WriteReport(candidateProvenanceReportPath, candidateProvenanceRows);
    EaglCandidateAssetProvenanceAudit.WriteSummary(candidateProvenanceSummaryPath, candidateProvenanceSummary);
    Console.WriteLine($"eagl_candidate_asset_provenance_audit: {candidateProvenanceReportPath} | summary={candidateProvenanceSummaryPath}");
    Console.WriteLine($"candidates: {candidateProvenanceSummary.CandidateCount} | source_chain_verified: {candidateProvenanceSummary.SourceChainVerified}");
    Console.WriteLine($"model_entries_read: {candidateProvenanceSummary.ModelArchiveEntriesRead} | shape_entries_read: {candidateProvenanceSummary.ShapeArchiveEntriesRead} | payloads_valid: {candidateProvenanceSummary.ShapePayloadsValid}");
    Console.WriteLine($"exact_shape_entries: {candidateProvenanceSummary.ExactShapeEntries} | decoded_pixel_matches: {candidateProvenanceSummary.DecodedPixelMatches}");
    Console.WriteLine($"candidate_structural_verified: {candidateProvenanceSummary.CandidateStructuralVerified} | model_object_entries_read: {candidateProvenanceSummary.ModelObjectEntriesRead} | model_companion_entries_read: {candidateProvenanceSummary.ModelCompanionEntriesRead}");
    Console.WriteLine($"candidate_bounds_valid: {candidateProvenanceSummary.CandidateBoundsValid} | candidate_geometry_finite_and_indexed: {candidateProvenanceSummary.CandidateGeometryFiniteAndIndexed} | candidate_primitive_shape_state_rows: {candidateProvenanceSummary.CandidatePrimitiveShapeStateRows}");
    Console.WriteLine($"candidate_geometry_streams_complete: {candidateProvenanceSummary.CandidateGeometryStreamsComplete} | candidate_shape_state_streams_complete: {candidateProvenanceSummary.CandidateShapeStateStreamsComplete}");
    Console.WriteLine($"status: {candidateProvenanceSummary.Status}; candidate source chain only; provenance={candidateProvenanceSummary.CandidateProvenance}; original semantics={candidateProvenanceSummary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, nested BIG/SHPX bounded re-read, source/payload/pixel hashes; GPU material, UV, skinning, rigging, animation, morph and original bindings not assigned");
    return string.Equals(candidateProvenanceSummary.Status, "verified-candidate-source-chain", StringComparison.Ordinal) ? 0 : 1;
}

if (string.Equals(args[0], "eagl-candidate-shape-state-primitive-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-shape-state-primitive-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var candidateShapeStateIsoPath = Path.GetFullPath(args[1]);
    var candidateShapeStateReportPath = Path.Combine(
        Directory.GetParent(candidateShapeStateIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "eagl-candidate-shape-state-primitive.tsv");
    var candidateShapeStateSummaryPath = Path.Combine(
        Directory.GetParent(candidateShapeStateIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "eagl-candidate-shape-state-primitive-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            candidateShapeStateReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            candidateShapeStateSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if (string.Equals(candidateShapeStateIsoPath, candidateShapeStateReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(candidateShapeStateIsoPath, candidateShapeStateSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria de shape-state candidate não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var candidateShapeStateStore = new GameAssetStore(candidateShapeStateIsoPath);
    var candidateShapeStateRows = EaglCandidateShapeStatePrimitiveAudit.Audit(
        candidateShapeStateStore,
        "data/models.big");
    var candidateShapeStateSummary = EaglCandidateShapeStatePrimitiveAudit.Summarize(
        candidateShapeStateReportPath,
        candidateShapeStateRows);
    EaglCandidateShapeStatePrimitiveAudit.WriteReport(candidateShapeStateReportPath, candidateShapeStateRows);
    EaglCandidateShapeStatePrimitiveAudit.WriteSummary(candidateShapeStateSummaryPath, candidateShapeStateSummary);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate SHAPENAME primitive index control");
    Console.WriteLine("mode: ISO read-only, same-trunk candidate parser, primitive index/layer/order only");
    Console.WriteLine($"candidate_rows: {candidateShapeStateSummary.CandidateRows}");
    Console.WriteLine($"valid_rows: {candidateShapeStateSummary.ValidRows}");
    Console.WriteLine($"complete_rows: {candidateShapeStateSummary.CompleteRows}");
    Console.WriteLine($"distinct_objects: {candidateShapeStateSummary.DistinctObjects}");
    Console.WriteLine($"status: {candidateShapeStateSummary.Status}; provenance={candidateShapeStateSummary.Provenance}; original semantics={candidateShapeStateSummary.OriginalSemantics}");
    Console.WriteLine($"report: {candidateShapeStateReportPath} | summary={candidateShapeStateSummaryPath}");
    Console.WriteLine("rule: primitive index/order is observed candidate structure only; no vertex, UV, material, skinning, rigging, animation, morph, command or GPU binding assigned");
    return string.Equals(candidateShapeStateSummary.Status, EaglCandidateShapeStatePrimitiveAudit.Status, StringComparison.Ordinal) ? 0 : 1;
}

if (string.Equals(args[0], "eagl-candidate-attribute-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-attribute-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? attributeAuditReportPath = null;
    var attributeAuditReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (attributeAuditReportOption >= 0)
    {
        if (attributeAuditReportOption + 1 >= args.Length ||
            args[attributeAuditReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-candidate-attribute-audit ... --report <saida.tsv>");
            return 2;
        }

        attributeAuditReportPath = Path.GetFullPath(args[attributeAuditReportOption + 1]);
    }

    using var attributeAuditStore = new GameAssetStore(args[1]);
    var attributeAuditMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        attributeAuditStore,
        "data/models.big");
    var attributeAuditRows = new List<EaglCandidateAttributeAuditRow>();
    foreach (var match in attributeAuditMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                attributeAuditStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            attributeAuditRows.Add(EaglCandidateAttributeAuditor.Measure(
                match.ModelArchiveName,
                match.ModelObjectName,
                match.ShapeName,
                candidate.Mesh,
                candidate.Primitives));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            attributeAuditRows.Add(new EaglCandidateAttributeAuditRow(
                match.ModelArchiveName,
                match.ModelObjectName,
                match.ShapeName,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                "rejected",
                exception.Message));
        }
    }

    if (attributeAuditReportPath is not null)
    {
        EaglCandidateAttributeAuditor.WriteReport(
            attributeAuditReportPath,
            attributeAuditRows);
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate attribute audit");
    Console.WriteLine($"report: {attributeAuditReportPath ?? "none"}");
    Console.WriteLine($"candidate_material_matches: {attributeAuditMatches.Count}");
    Console.WriteLine($"audited_objects: {attributeAuditRows.Count(row => row.Status != "rejected")}");
    Console.WriteLine($"rejected_objects: {attributeAuditRows.Count(row => row.Status == "rejected")}");
    Console.WriteLine($"candidate_groups: {attributeAuditRows.Sum(row => row.Groups)}");
    Console.WriteLine($"slot1_complete_groups: {attributeAuditRows.Sum(row => row.Slot1CompleteGroups)}");
    Console.WriteLine($"slot1_finite_groups: {attributeAuditRows.Sum(row => row.Slot1FiniteGroups)}");
    Console.WriteLine($"slot1_near_unit_groups: {attributeAuditRows.Sum(row => row.Slot1NearUnitGroups)}");
    Console.WriteLine($"slot1_values: {attributeAuditRows.Sum(row => row.Slot1ValueCount)}");
    Console.WriteLine($"group_shape_state_count: {attributeAuditRows.Sum(row => row.GroupShapeStateCount)}");
    Console.WriteLine($"group_clamp_state_count: {attributeAuditRows.Sum(row => row.GroupClampStateCount)}");
    Console.WriteLine($"primitive_shape_state_count: {attributeAuditRows.Sum(row => row.PrimitiveShapeStateCount)}");
    Console.WriteLine($"primitive_clamp_state_count: {attributeAuditRows.Sum(row => row.PrimitiveClampStateCount)}");
    Console.WriteLine("status: slot-1 numeric and state-population evidence only; no UV, material, skinning, rigging, animation, or GPU binding assigned");
    Console.WriteLine("mode: ISO read-only, same-trunk candidate objects, managed EAGL inspection, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-geometry-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-geometry-audit <caminho-do-iso> [--report <saida.tsv>] [--pixel-size <pixels>]");
        return 2;
    }

    string? geometryAuditReportPath = null;
    var geometryAuditReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (geometryAuditReportOptionIndex >= 0)
    {
        if (geometryAuditReportOptionIndex + 1 >= args.Length ||
            args[geometryAuditReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-candidate-geometry-audit ... --report <saida.tsv>");
            return 2;
        }

        geometryAuditReportPath = Path.GetFullPath(args[geometryAuditReportOptionIndex + 1]);
        if (string.Equals(
                geometryAuditReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("A auditoria geométrica não pode sobrescrever a ISO de entrada.");
            return 2;
        }
    }

    var geometryAuditPixelSize = 390;
    var geometryAuditPixelOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--pixel-size", StringComparison.OrdinalIgnoreCase));
    if (geometryAuditPixelOptionIndex >= 0)
    {
        if (geometryAuditPixelOptionIndex + 1 >= args.Length ||
            !int.TryParse(args[geometryAuditPixelOptionIndex + 1], NumberStyles.None, CultureInfo.InvariantCulture, out geometryAuditPixelSize) ||
            geometryAuditPixelSize <= 0)
        {
            Console.Error.WriteLine("--pixel-size deve ser um inteiro positivo.");
            return 2;
        }
    }

    using var geometryAuditStore = new GameAssetStore(args[1]);
    var geometryAuditMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        geometryAuditStore,
        "data/models.big");
    var geometryAuditLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "model_archive",
            "model_object",
            "shape_name",
            "object_state",
            "render_group_state",
            "groups",
            "vertices",
            "triangles",
            "invalid_index_triangles",
            "nonfinite_vertices",
            "nonfinite_referenced_triangles",
            "repeated_index_triangles",
            "exact_zero_area_3d_triangles",
            "exact_zero_area_projected_triangles",
            "raster_degenerate_triangles",
            "raster_visible_triangles",
            "min_edge_length",
            "median_edge_length",
            "max_edge_length",
            "max_edge_to_median_edge_ratio",
            "status",
            "issue",
        }),
    };
    var geometryAuditVerified = 0;
    var geometryAuditRejected = 0;
    long geometryAuditInvalidIndexTriangles = 0;
    long geometryAuditNonFiniteVertices = 0;
    long geometryAuditNonFiniteReferencedTriangles = 0;
    long geometryAuditRepeatedIndexTriangles = 0;
    long geometryAuditExactZeroArea3dTriangles = 0;
    long geometryAuditExactZeroAreaProjectedTriangles = 0;
    long geometryAuditRasterDegenerateTriangles = 0;
    long geometryAuditRasterVisibleTriangles = 0;
    foreach (var match in geometryAuditMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                geometryAuditStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    match.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{match.ShapeName}'.");
            }

            var audit = EaglCandidateGeometryAuditor.Measure(candidate.Mesh, geometryAuditPixelSize);
            var renderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
                candidate.Mesh,
                match.ShapeName);
            geometryAuditInvalidIndexTriangles += audit.InvalidIndexTriangles;
            geometryAuditNonFiniteVertices += audit.NonFiniteVertices;
            geometryAuditNonFiniteReferencedTriangles += audit.NonFiniteReferencedTriangles;
            geometryAuditRepeatedIndexTriangles += audit.RepeatedIndexTriangles;
            geometryAuditExactZeroArea3dTriangles += audit.ExactZeroArea3dTriangles;
            geometryAuditExactZeroAreaProjectedTriangles += audit.ExactZeroAreaProjectedTriangles;
            geometryAuditRasterDegenerateTriangles += audit.RasterDegenerateTriangles;
            geometryAuditRasterVisibleTriangles += audit.RasterVisibleTriangles;
            geometryAuditVerified++;
            geometryAuditLines.Add(string.Join("\t", new[]
            {
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                "exact",
                renderGroupState ? "exact" : "unproven",
                audit.GroupCount.ToString(CultureInfo.InvariantCulture),
                audit.VertexCount.ToString(CultureInfo.InvariantCulture),
                audit.TriangleCount.ToString(CultureInfo.InvariantCulture),
                audit.InvalidIndexTriangles.ToString(CultureInfo.InvariantCulture),
                audit.NonFiniteVertices.ToString(CultureInfo.InvariantCulture),
                audit.NonFiniteReferencedTriangles.ToString(CultureInfo.InvariantCulture),
                audit.RepeatedIndexTriangles.ToString(CultureInfo.InvariantCulture),
                audit.ExactZeroArea3dTriangles.ToString(CultureInfo.InvariantCulture),
                audit.ExactZeroAreaProjectedTriangles.ToString(CultureInfo.InvariantCulture),
                audit.RasterDegenerateTriangles.ToString(CultureInfo.InvariantCulture),
                audit.RasterVisibleTriangles.ToString(CultureInfo.InvariantCulture),
                audit.MinEdgeLength.ToString("R", CultureInfo.InvariantCulture),
                audit.MedianEdgeLength.ToString("R", CultureInfo.InvariantCulture),
                audit.MaxEdgeLength.ToString("R", CultureInfo.InvariantCulture),
                audit.MaxEdgeToMedianEdgeRatio.ToString("R", CultureInfo.InvariantCulture),
                "verified-candidate",
                string.Empty,
            }));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException or FileNotFoundException)
        {
            geometryAuditRejected++;
            geometryAuditLines.Add(string.Join("\t", new[]
            {
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                "unknown",
                "unknown",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "rejected-candidate",
                CatalogField(exception.Message),
            }));
        }
    }

    if (geometryAuditReportPath is not null)
    {
        var geometryAuditReportDirectory = Path.GetDirectoryName(geometryAuditReportPath);
        if (!string.IsNullOrEmpty(geometryAuditReportDirectory))
        {
            Directory.CreateDirectory(geometryAuditReportDirectory);
        }

        File.WriteAllLines(
            geometryAuditReportPath,
            geometryAuditLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate geometry audit");
    Console.WriteLine($"candidate_material_matches: {geometryAuditMatches.Count}");
    Console.WriteLine($"audited_geometry_rows: {geometryAuditVerified}");
    Console.WriteLine($"rejected_geometry_rows: {geometryAuditRejected}");
    Console.WriteLine($"invalid_index_triangles: {geometryAuditInvalidIndexTriangles}");
    Console.WriteLine($"nonfinite_vertices: {geometryAuditNonFiniteVertices}");
    Console.WriteLine($"nonfinite_referenced_triangles: {geometryAuditNonFiniteReferencedTriangles}");
    Console.WriteLine($"repeated_index_triangles: {geometryAuditRepeatedIndexTriangles}");
    Console.WriteLine($"exact_zero_area_3d_triangles: {geometryAuditExactZeroArea3dTriangles}");
    Console.WriteLine($"exact_zero_area_projected_triangles: {geometryAuditExactZeroAreaProjectedTriangles}");
    Console.WriteLine($"raster_degenerate_triangles: {geometryAuditRasterDegenerateTriangles}");
    Console.WriteLine($"raster_visible_triangles: {geometryAuditRasterVisibleTriangles}");
    if (geometryAuditReportPath is not null)
    {
        Console.WriteLine($"report: {geometryAuditReportPath}");
    }
    Console.WriteLine($"pixel_size: {geometryAuditPixelSize}");
    Console.WriteLine("status: objective candidate geometry/projection audit only; no vertex, UV, material, skinning, rigging, animation or GPU command semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed EAGL candidate runtime");
    return geometryAuditRejected == 0 && geometryAuditVerified == geometryAuditMatches.Count ? 0 : 1;
}

if (string.Equals(args[0], "eagl-model-material-reference-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 6 || args[2] != "--report" || args[4] != "--summary")
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-model-material-reference-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
        return 2;
    }
    return EaglModelMaterialReferenceAudit.Run(args[1], args[3], args[5]);
}

if (string.Equals(args[0], "eagl-vertex-program-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 6 || args[2] != "--report" || args[4] != "--summary")
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-vertex-program-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
        return 2;
    }
    return EaglVertexProgramAudit.Run(args[1], args[3], args[5]);
}

if (string.Equals(args[0], "eagl-material-declaration-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 6 || args[2] != "--report" || args[4] != "--summary")
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-declaration-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
        return 2;
    }
    return EaglMaterialDeclarationAudit.Run(args[1], args[3], args[5]);
}

if (string.Equals(args[0], "eagl-indexed-draw-source-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 6 || args[2] != "--report" || args[4] != "--summary")
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-indexed-draw-source-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
        return 2;
    }
    return EaglIndexedDrawSourceAudit.Run(args[1], args[3], args[5]);
}

if (string.Equals(args[0], "eagl-render-parameter-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length != 6 || args[2] != "--report" || args[4] != "--summary")
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-render-parameter-audit <iso> --report <saida.tsv> --summary <resumo.tsv>");
        return 2;
    }
    return EaglRenderParameterAudit.Run(args[1], args[3], args[5]);
}

if (string.Equals(args[0], "eagl-original-render-chain-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-render-chain-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? originalRenderChainReportPath = null;
    string? originalRenderChainSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            originalRenderChainReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            originalRenderChainSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((originalRenderChainReportPath is not null &&
         string.Equals(originalRenderChainReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (originalRenderChainSummaryPath is not null &&
         string.Equals(originalRenderChainSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios do encadeamento de renderização não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var originalRenderChainStore = new GameAssetStore(args[1]);
    var originalRenderChain = EaglOriginalRenderChainAudit.Audit(
        originalRenderChainStore,
        "data/models.big");
    if (originalRenderChainReportPath is not null)
    {
        EaglOriginalRenderChainAudit.WriteReport(
            originalRenderChainReportPath,
            originalRenderChain.Rows);
    }
    if (originalRenderChainSummaryPath is not null)
    {
        EaglOriginalRenderChainAudit.WriteSummary(
            originalRenderChainSummaryPath,
            originalRenderChain.Summary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original EAGL render-chain structure audit");
    Console.WriteLine($"model_package_candidates: {originalRenderChain.Summary.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {originalRenderChain.Summary.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {originalRenderChain.Summary.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {originalRenderChain.Summary.ObjectsLoaded}");
    Console.WriteLine($"model_symbol_rows: {originalRenderChain.Summary.ModelSymbolRows}");
    Console.WriteLine($"bounds_symbol_rows: {originalRenderChain.Summary.BoundsSymbolRows}");
    Console.WriteLine($"render_method_symbol_rows: {originalRenderChain.Summary.RenderMethodSymbolRows}");
    Console.WriteLine($"vertex_buffer_symbol_rows: {originalRenderChain.Summary.VertexBufferSymbolRows}");
    Console.WriteLine($"validated_vertex_stream_rows: {originalRenderChain.Summary.ValidatedVertexStreamRows}");
    Console.WriteLine($"objects_with_bounds: {originalRenderChain.Summary.ObjectsWithBounds}");
    Console.WriteLine($"objects_with_validated_vertex_streams: {originalRenderChain.Summary.ObjectsWithValidatedVertexStreams}");
    Console.WriteLine($"primitive_rows: {originalRenderChain.Summary.PrimitiveRows}");
    Console.WriteLine($"valid_chain_rows: {originalRenderChain.Summary.ValidChainRows}");
    Console.WriteLine($"complete_command_streams: {originalRenderChain.Summary.CompleteCommandStreams}");
    Console.WriteLine($"state_relocation_rows: {originalRenderChain.Summary.StateRelocationRows}");
    Console.WriteLine($"shapename_state_rows: {originalRenderChain.Summary.ShapeNameStateRows}");
    Console.WriteLine($"primitive_type_state_rows: {originalRenderChain.Summary.PrimitiveTypeStateRows}");
    Console.WriteLine($"fetch_slot_rows: {originalRenderChain.Summary.FetchSlotRows}");
    Console.WriteLine($"complete_five_slot_fetch_rows: {originalRenderChain.Summary.CompleteFiveSlotFetchRows}");
    Console.WriteLine($"state_fetch_overlap_rows: {originalRenderChain.Summary.StateFetchOverlapRows}");
    Console.WriteLine($"state_shapename_primitive_type_overlap_rows: {originalRenderChain.Summary.StateShapePrimitiveTypeOverlapRows}");
    Console.WriteLine($"rejected_packages: {originalRenderChain.Summary.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {originalRenderChain.Summary.RejectedObjects}");
    if (originalRenderChainReportPath is not null)
    {
        Console.WriteLine($"report: {originalRenderChainReportPath}");
    }
    if (originalRenderChainSummaryPath is not null)
    {
        Console.WriteLine($"summary: {originalRenderChainSummaryPath}");
    }
    Console.WriteLine($"status: {originalRenderChain.Summary.Status}");
    Console.WriteLine($"provenance: {originalRenderChain.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {originalRenderChain.Summary.OriginalSemantics}");
    Console.WriteLine($"state_fetch_control_status: {originalRenderChain.Summary.StateFetchControlStatus}");
    Console.WriteLine($"vertex_record_control_status: {originalRenderChain.Summary.VertexRecordControlStatus}");
    Console.WriteLine("mode: ISO read-only, raw EAGL model-chain inspection; candidate mesh, SHPX texture and command/GPU semantics are not used; fetch slots remain structural only");
    return string.Equals(
        originalRenderChain.Summary.Status,
        EaglOriginalRenderChainAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-vertex-record-association-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-vertex-record-association-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? originalVertexAssociationReportPath = null;
    string? originalVertexAssociationSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            originalVertexAssociationReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            originalVertexAssociationSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((originalVertexAssociationReportPath is not null &&
         string.Equals(originalVertexAssociationReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (originalVertexAssociationSummaryPath is not null &&
         string.Equals(originalVertexAssociationSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios da associação original de registros não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var originalVertexAssociationStore = new GameAssetStore(args[1]);
    var originalVertexAssociationRows = new List<EaglOriginalVertexRecordAssociationRow>();
    var originalVertexAssociation = EaglOriginalVertexRecordAssociationAudit.Audit(
        originalVertexAssociationStore,
        "data/models.big",
        originalVertexAssociationRows);
    if (originalVertexAssociationReportPath is not null)
    {
        EaglOriginalVertexRecordAssociationAudit.WriteReport(
            originalVertexAssociationReportPath,
            originalVertexAssociationRows);
    }
    if (originalVertexAssociationSummaryPath is not null)
    {
        EaglOriginalVertexRecordAssociationAudit.WriteSummary(
            originalVertexAssociationSummaryPath,
            originalVertexAssociation);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original opaque __VB record association audit");
    Console.WriteLine($"model_package_candidates: {originalVertexAssociation.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {originalVertexAssociation.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {originalVertexAssociation.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {originalVertexAssociation.ObjectsLoaded}");
    Console.WriteLine($"validated_vertex_stream_rows: {originalVertexAssociation.ValidatedVertexStreamRows}");
    Console.WriteLine($"objects_with_validated_vertex_streams: {originalVertexAssociation.ObjectsWithValidatedVertexStreams}");
    Console.WriteLine($"primitive_rows: {originalVertexAssociation.PrimitiveRows}");
    Console.WriteLine($"rows_with_opaque_ordinal_records: {originalVertexAssociation.RowsWithOpaqueOrdinalRecords}");
    Console.WriteLine($"rows_with_unique_opaque_sequences: {originalVertexAssociation.RowsWithUniqueOpaqueSequences}");
    Console.WriteLine($"complete_five_slot_fetch_rows: {originalVertexAssociation.CompleteFiveSlotFetchRows}");
    Console.WriteLine($"rejected_packages: {originalVertexAssociation.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {originalVertexAssociation.RejectedObjects}");
    if (originalVertexAssociationReportPath is not null)
    {
        Console.WriteLine($"report: {originalVertexAssociationReportPath}");
    }
    if (originalVertexAssociationSummaryPath is not null)
    {
        Console.WriteLine($"summary: {originalVertexAssociationSummaryPath}");
    }
    Console.WriteLine($"status: {originalVertexAssociation.Status}");
    Console.WriteLine($"provenance: {originalVertexAssociation.Provenance}");
    Console.WriteLine($"original_semantics: {originalVertexAssociation.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, raw original EAGL objects; opaque category/ordinal and stride joins only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        originalVertexAssociation.Status,
        EaglOriginalVertexRecordAssociationAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-primitive-stream-topology-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-primitive-stream-topology-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? originalPrimitiveStreamTopologyReportPath = null;
    string? originalPrimitiveStreamTopologySummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            originalPrimitiveStreamTopologyReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            originalPrimitiveStreamTopologySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((originalPrimitiveStreamTopologyReportPath is not null &&
         string.Equals(originalPrimitiveStreamTopologyReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (originalPrimitiveStreamTopologySummaryPath is not null &&
         string.Equals(originalPrimitiveStreamTopologySummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O relatório de topologia primitiva-stream original não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var originalPrimitiveStreamTopologyStore = new GameAssetStore(args[1]);
    var originalPrimitiveStreamTopologyRows = new List<EaglOriginalPrimitiveStreamTopologyRow>();
    var originalPrimitiveStreamTopology = EaglOriginalPrimitiveStreamTopologyAudit.Audit(
        originalPrimitiveStreamTopologyStore,
        "data/models.big",
        originalPrimitiveStreamTopologyRows);
    if (originalPrimitiveStreamTopologyReportPath is not null)
    {
        EaglOriginalPrimitiveStreamTopologyAudit.WriteReport(
            originalPrimitiveStreamTopologyReportPath,
            originalPrimitiveStreamTopologyRows);
    }
    if (originalPrimitiveStreamTopologySummaryPath is not null)
    {
        EaglOriginalPrimitiveStreamTopologyAudit.WriteSummary(
            originalPrimitiveStreamTopologySummaryPath,
            originalPrimitiveStreamTopology);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original primitive/__VB stream topology audit");
    Console.WriteLine($"model_package_candidates: {originalPrimitiveStreamTopology.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {originalPrimitiveStreamTopology.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {originalPrimitiveStreamTopology.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {originalPrimitiveStreamTopology.ObjectsLoaded}");
    Console.WriteLine($"objects_with_validated_streams: {originalPrimitiveStreamTopology.ObjectsWithValidatedStreams}");
    Console.WriteLine($"primitive_rows: {originalPrimitiveStreamTopology.PrimitiveRows}");
    Console.WriteLine($"stream_rows: {originalPrimitiveStreamTopology.StreamRows}");
    Console.WriteLine($"primitive_rows_with_ordinal_matched_streams: {originalPrimitiveStreamTopology.PrimitiveRowsWithOrdinalMatchedStreams}");
    Console.WriteLine($"stream_rows_with_ordinal_match: {originalPrimitiveStreamTopology.StreamRowsWithOrdinalMatch}");
    Console.WriteLine($"stream_rows_with_stride_match: {originalPrimitiveStreamTopology.StreamRowsWithStrideMatch}");
    Console.WriteLine($"stream_rows_with_valid_payload_range: {originalPrimitiveStreamTopology.StreamRowsWithValidPayloadRange}");
    Console.WriteLine($"complete_primitive_rows: {originalPrimitiveStreamTopology.CompletePrimitiveRows}");
    Console.WriteLine($"objects_with_non_overlapping_payload_ranges: {originalPrimitiveStreamTopology.ObjectsWithNonOverlappingPayloadRanges}");
    Console.WriteLine($"reversed_slot_match_rows: {originalPrimitiveStreamTopology.ReversedSlotMatchRows}");
    Console.WriteLine($"shifted_ordinal_match_rows: {originalPrimitiveStreamTopology.ShiftedOrdinalMatchRows}");
    Console.WriteLine($"rejected_packages: {originalPrimitiveStreamTopology.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {originalPrimitiveStreamTopology.RejectedObjects}");
    if (originalPrimitiveStreamTopologyReportPath is not null)
    {
        Console.WriteLine($"report: {originalPrimitiveStreamTopologyReportPath}");
    }
    if (originalPrimitiveStreamTopologySummaryPath is not null)
    {
        Console.WriteLine($"summary: {originalPrimitiveStreamTopologySummaryPath}");
    }
    Console.WriteLine($"status: {originalPrimitiveStreamTopology.Status}");
    Console.WriteLine($"provenance: {originalPrimitiveStreamTopology.Provenance}");
    Console.WriteLine($"original_semantics: {originalPrimitiveStreamTopology.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, opaque primitive/fetch/stream topology only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        originalPrimitiveStreamTopology.Status,
        EaglOriginalPrimitiveStreamTopologyAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-primitive-stream-opaque-discrimination-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-primitive-stream-opaque-discrimination-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? opaqueDiscriminationReportPath = null;
    string? opaqueDiscriminationSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            opaqueDiscriminationReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            opaqueDiscriminationSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((opaqueDiscriminationReportPath is not null &&
         string.Equals(opaqueDiscriminationReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (opaqueDiscriminationSummaryPath is not null &&
         string.Equals(opaqueDiscriminationSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O relatório de discriminação opaca não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var opaqueDiscriminationStore = new GameAssetStore(args[1]);
    var opaqueDiscriminationRows = new List<EaglOriginalPrimitiveStreamOpaqueDiscriminationRow>();
    var opaqueDiscrimination = EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.Audit(
        opaqueDiscriminationStore,
        "data/models.big",
        opaqueDiscriminationRows);
    if (opaqueDiscriminationReportPath is not null)
    {
        EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.WriteReport(
            opaqueDiscriminationReportPath,
            opaqueDiscriminationRows);
    }
    if (opaqueDiscriminationSummaryPath is not null)
    {
        EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.WriteSummary(
            opaqueDiscriminationSummaryPath,
            opaqueDiscrimination);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original primitive/__VB opaque-word discrimination audit");
    Console.WriteLine($"model_package_candidates: {opaqueDiscrimination.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {opaqueDiscrimination.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {opaqueDiscrimination.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {opaqueDiscrimination.ObjectsLoaded}");
    Console.WriteLine($"objects_with_validated_streams: {opaqueDiscrimination.ObjectsWithValidatedStreams}");
    Console.WriteLine($"primitive_rows: {opaqueDiscrimination.PrimitiveRows}");
    Console.WriteLine($"complete_fetch_rows: {opaqueDiscrimination.CompleteFetchRows}");
    Console.WriteLine($"candidate_ordinal_rows: {opaqueDiscrimination.CandidateOrdinalRows}");
    Console.WriteLine($"stride_matching_candidate_rows: {opaqueDiscrimination.StrideMatchingCandidateRows}");
    Console.WriteLine($"metadata_matching_candidate_rows: {opaqueDiscrimination.MetadataMatchingCandidateRows}");
    Console.WriteLine($"payload_prefix_matching_candidate_rows: {opaqueDiscrimination.PayloadPrefixMatchingCandidateRows}");
    Console.WriteLine($"current_ordinal_rows: {opaqueDiscrimination.CurrentOrdinalRows}");
    Console.WriteLine($"current_ordinal_metadata_matching_rows: {opaqueDiscrimination.CurrentOrdinalMetadataMatchingRows}");
    Console.WriteLine($"current_ordinal_payload_prefix_matching_rows: {opaqueDiscrimination.CurrentOrdinalPayloadPrefixMatchingRows}");
    Console.WriteLine($"uniquely_discriminated_by_metadata_rows: {opaqueDiscrimination.UniquelyDiscriminatedByMetadataRows}");
    Console.WriteLine($"uniquely_discriminated_by_payload_prefix_rows: {opaqueDiscrimination.UniquelyDiscriminatedByPayloadPrefixRows}");
    Console.WriteLine($"shifted_stride_candidate_rows: {opaqueDiscrimination.ShiftedStrideCandidateRows}");
    Console.WriteLine($"shifted_metadata_matching_rows: {opaqueDiscrimination.ShiftedMetadataMatchingRows}");
    Console.WriteLine($"shifted_payload_prefix_matching_rows: {opaqueDiscrimination.ShiftedPayloadPrefixMatchingRows}");
    Console.WriteLine($"rejected_packages: {opaqueDiscrimination.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {opaqueDiscrimination.RejectedObjects}");
    if (opaqueDiscriminationReportPath is not null)
    {
        Console.WriteLine($"report: {opaqueDiscriminationReportPath}");
    }
    if (opaqueDiscriminationSummaryPath is not null)
    {
        Console.WriteLine($"summary: {opaqueDiscriminationSummaryPath}");
    }
    Console.WriteLine($"status: {opaqueDiscrimination.Status}");
    Console.WriteLine($"provenance: {opaqueDiscrimination.Provenance}");
    Console.WriteLine($"original_semantics: {opaqueDiscrimination.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, opaque fetch-command word and validated __VB byte-overlap controls only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        opaqueDiscrimination.Status,
        EaglOriginalPrimitiveStreamOpaqueDiscriminationAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-primitive-stream-command-payload-identity-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-primitive-stream-command-payload-identity-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? commandPayloadIdentityReportPath = null;
    string? commandPayloadIdentitySummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            commandPayloadIdentityReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            commandPayloadIdentitySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    var commandPayloadIdentityIsoPath = Path.GetFullPath(args[1]);
    if ((commandPayloadIdentityReportPath is not null &&
         string.Equals(commandPayloadIdentityReportPath, commandPayloadIdentityIsoPath, StringComparison.OrdinalIgnoreCase)) ||
        (commandPayloadIdentitySummaryPath is not null &&
         string.Equals(commandPayloadIdentitySummaryPath, commandPayloadIdentityIsoPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O relatório de identidade command/payload não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var commandPayloadIdentityStore = new GameAssetStore(args[1]);
    var commandPayloadIdentityRows =
        new List<EaglOriginalPrimitiveStreamCommandPayloadIdentityRow>();
    var commandPayloadIdentity =
        EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.Audit(
            commandPayloadIdentityStore,
            "data/models.big",
            commandPayloadIdentityRows);
    if (commandPayloadIdentityReportPath is not null)
    {
        EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.WriteReport(
            commandPayloadIdentityReportPath,
            commandPayloadIdentityRows);
    }
    if (commandPayloadIdentitySummaryPath is not null)
    {
        EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.WriteSummary(
            commandPayloadIdentitySummaryPath,
            commandPayloadIdentity);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original primitive/__VB command-payload identity control");
    Console.WriteLine($"model_package_candidates: {commandPayloadIdentity.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {commandPayloadIdentity.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {commandPayloadIdentity.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {commandPayloadIdentity.ObjectsLoaded}");
    Console.WriteLine($"objects_with_validated_streams: {commandPayloadIdentity.ObjectsWithValidatedStreams}");
    Console.WriteLine($"primitive_rows: {commandPayloadIdentity.PrimitiveRows}");
    Console.WriteLine($"complete_fetch_rows: {commandPayloadIdentity.CompleteFetchRows}");
    Console.WriteLine($"candidate_ordinal_rows: {commandPayloadIdentity.CandidateOrdinalRows}");
    Console.WriteLine($"raw_identity_candidate_rows: {commandPayloadIdentity.RawIdentityCandidateRows}");
    Console.WriteLine($"distinct_raw_identity_rows: {commandPayloadIdentity.DistinctRawIdentityRows}");
    Console.WriteLine($"duplicate_raw_identity_rows: {commandPayloadIdentity.DuplicateRawIdentityRows}");
    Console.WriteLine($"command_payload_exact_candidate_rows: {commandPayloadIdentity.CommandPayloadExactCandidateRows}");
    Console.WriteLine($"current_ordinal_rows: {commandPayloadIdentity.CurrentOrdinalRows}");
    Console.WriteLine($"current_ordinal_raw_identity_unique_rows: {commandPayloadIdentity.CurrentOrdinalRawIdentityUniqueRows}");
    Console.WriteLine($"current_ordinal_command_payload_exact_rows: {commandPayloadIdentity.CurrentOrdinalCommandPayloadExactRows}");
    Console.WriteLine($"current_ordinal_uniquely_discriminated_by_command_payload_rows: {commandPayloadIdentity.CurrentOrdinalUniquelyDiscriminatedByCommandPayloadRows}");
    Console.WriteLine($"shifted_ordinal_command_payload_exact_rows: {commandPayloadIdentity.ShiftedOrdinalCommandPayloadExactRows}");
    Console.WriteLine($"rejected_packages: {commandPayloadIdentity.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {commandPayloadIdentity.RejectedObjects}");
    if (commandPayloadIdentityReportPath is not null)
    {
        Console.WriteLine($"report: {commandPayloadIdentityReportPath}");
    }
    if (commandPayloadIdentitySummaryPath is not null)
    {
        Console.WriteLine($"summary: {commandPayloadIdentitySummaryPath}");
    }
    Console.WriteLine($"status: {commandPayloadIdentity.Status}");
    Console.WriteLine($"provenance: {commandPayloadIdentity.Provenance}");
    Console.WriteLine($"original_semantics: {commandPayloadIdentity.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, raw command/payload identity control only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        commandPayloadIdentity.Status,
        EaglOriginalPrimitiveStreamCommandPayloadIdentityAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-primitive-stream-command-descriptor-adjacency-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-primitive-stream-command-descriptor-adjacency-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? commandDescriptorAdjacencyReportPath = null;
    string? commandDescriptorAdjacencySummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            commandDescriptorAdjacencyReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            commandDescriptorAdjacencySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    var commandDescriptorIsoPath = Path.GetFullPath(args[1]);
    if ((commandDescriptorAdjacencyReportPath is not null &&
         string.Equals(commandDescriptorAdjacencyReportPath, commandDescriptorIsoPath, StringComparison.OrdinalIgnoreCase)) ||
        (commandDescriptorAdjacencySummaryPath is not null &&
         string.Equals(commandDescriptorAdjacencySummaryPath, commandDescriptorIsoPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("O relatório de adjacência comando/descritor não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var commandDescriptorStore = new GameAssetStore(args[1]);
    var commandDescriptorRows =
        new List<EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyRow>();
    var commandDescriptorAdjacency =
        EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.Audit(
            commandDescriptorStore,
            "data/models.big",
            commandDescriptorRows);
    if (commandDescriptorAdjacencyReportPath is not null)
    {
        EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.WriteReport(
            commandDescriptorAdjacencyReportPath,
            commandDescriptorRows);
    }
    if (commandDescriptorAdjacencySummaryPath is not null)
    {
        EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.WriteSummary(
            commandDescriptorAdjacencySummaryPath,
            commandDescriptorAdjacency);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original primitive/__VB command-descriptor adjacency audit");
    Console.WriteLine($"command_descriptor_delta: 0x{commandDescriptorAdjacency.CommandDescriptorDelta:X}");
    Console.WriteLine($"model_package_candidates: {commandDescriptorAdjacency.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {commandDescriptorAdjacency.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {commandDescriptorAdjacency.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {commandDescriptorAdjacency.ObjectsLoaded}");
    Console.WriteLine($"objects_with_validated_streams: {commandDescriptorAdjacency.ObjectsWithValidatedStreams}");
    Console.WriteLine($"primitive_rows: {commandDescriptorAdjacency.PrimitiveRows}");
    Console.WriteLine($"complete_command_rows: {commandDescriptorAdjacency.CompleteCommandRows}");
    Console.WriteLine($"candidate_ordinal_rows: {commandDescriptorAdjacency.CandidateOrdinalRows}");
    Console.WriteLine($"identity_candidate_rows: {commandDescriptorAdjacency.IdentityCandidateRows}");
    Console.WriteLine($"current_ordinal_rows: {commandDescriptorAdjacency.CurrentOrdinalRows}");
    Console.WriteLine($"current_ordinal_identity_rows: {commandDescriptorAdjacency.CurrentOrdinalIdentityRows}");
    Console.WriteLine($"unique_current_ordinal_identity_rows: {commandDescriptorAdjacency.UniqueCurrentOrdinalIdentityRows}");
    Console.WriteLine($"shifted_ordinal_identity_rows: {commandDescriptorAdjacency.ShiftedOrdinalIdentityRows}");
    Console.WriteLine($"same_section_identity_rows: {commandDescriptorAdjacency.SameSectionIdentityRows}");
    Console.WriteLine($"rejected_packages: {commandDescriptorAdjacency.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {commandDescriptorAdjacency.RejectedObjects}");
    if (commandDescriptorAdjacencyReportPath is not null)
    {
        Console.WriteLine($"report: {commandDescriptorAdjacencyReportPath}");
    }
    if (commandDescriptorAdjacencySummaryPath is not null)
    {
        Console.WriteLine($"summary: {commandDescriptorAdjacencySummaryPath}");
    }
    Console.WriteLine($"status: {commandDescriptorAdjacency.Status}");
    Console.WriteLine($"provenance: {commandDescriptorAdjacency.Provenance}");
    Console.WriteLine($"original_semantics: {commandDescriptorAdjacency.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, exact same-section command/descriptor byte-adjacency control only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        commandDescriptorAdjacency.Status,
        EaglOriginalPrimitiveStreamCommandDescriptorAdjacencyAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-vertex-payload-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
    Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-vertex-payload-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--stream-report <saida.tsv>]");
        return 2;
    }

    string? originalVertexPayloadReportPath = null;
    string? originalVertexPayloadSummaryPath = null;
    string? originalVertexStreamReportPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            originalVertexPayloadReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            originalVertexPayloadSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--stream-report", StringComparison.OrdinalIgnoreCase))
        {
            originalVertexStreamReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((originalVertexPayloadReportPath is not null &&
         string.Equals(originalVertexPayloadReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (originalVertexPayloadSummaryPath is not null &&
         string.Equals(originalVertexPayloadSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios do payload original de vértices não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var originalVertexPayloadStore = new GameAssetStore(args[1]);
    var originalVertexPayloadRows = new List<EaglOriginalVertexPayloadRow>();
    var originalVertexStreamRows = new List<EaglOriginalVertexStreamNumericRow>();
    var originalVertexPayload = EaglOriginalVertexPayloadAudit.Audit(
        originalVertexPayloadStore,
        "data/models.big",
        originalVertexPayloadRows,
        originalVertexStreamReportPath is null ? null : originalVertexStreamRows);
    if (originalVertexPayloadReportPath is not null)
    {
        EaglOriginalVertexPayloadAudit.WriteReport(
            originalVertexPayloadReportPath,
            originalVertexPayloadRows);
    }
    if (originalVertexPayloadSummaryPath is not null)
    {
        EaglOriginalVertexPayloadAudit.WriteSummary(
            originalVertexPayloadSummaryPath,
            originalVertexPayload);
    }
    if (originalVertexStreamReportPath is not null)
    {
        EaglOriginalVertexPayloadAudit.WriteStreamReport(
            originalVertexStreamReportPath,
            originalVertexStreamRows);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original numeric __VB payload audit");
    Console.WriteLine($"model_package_candidates: {originalVertexPayload.ModelPackageCandidates}");
    Console.WriteLine($"model_packages_loaded: {originalVertexPayload.ModelPackagesLoaded}");
    Console.WriteLine($"object_candidates: {originalVertexPayload.ObjectCandidates}");
    Console.WriteLine($"objects_loaded: {originalVertexPayload.ObjectsLoaded}");
    Console.WriteLine($"validated_vertex_stream_rows: {originalVertexPayload.ValidatedVertexStreamRows}");
    Console.WriteLine($"objects_with_validated_vertex_streams: {originalVertexPayload.ObjectsWithValidatedVertexStreams}");
    Console.WriteLine($"objects_with_finite_packed_float_profiles: {originalVertexPayload.ObjectsWithFinitePackedFloatProfiles}");
    Console.WriteLine($"objects_with_finite_float2_profiles: {originalVertexPayload.ObjectsWithFiniteFloat2Profiles}");
    Console.WriteLine($"objects_with_declared_bounds_containment: {originalVertexPayload.ObjectsWithDeclaredBoundsContainment}");
    Console.WriteLine($"payload_rows: {originalVertexPayload.PayloadRows}");
    Console.WriteLine($"objects_with_distinct_payload_ranges: {originalVertexPayload.ObjectsWithDistinctPayloadRanges}");
    Console.WriteLine($"objects_with_overlapping_payload_ranges: {originalVertexPayload.ObjectsWithOverlappingPayloadRanges}");
    Console.WriteLine($"rejected_packages: {originalVertexPayload.RejectedPackages}");
    Console.WriteLine($"rejected_objects: {originalVertexPayload.RejectedObjects}");
    if (originalVertexPayloadReportPath is not null)
    {
        Console.WriteLine($"report: {originalVertexPayloadReportPath}");
    }
    if (originalVertexPayloadSummaryPath is not null)
    {
        Console.WriteLine($"summary: {originalVertexPayloadSummaryPath}");
    }
    if (originalVertexStreamReportPath is not null)
    {
        Console.WriteLine($"stream_report: {originalVertexStreamReportPath}");
    }
    Console.WriteLine($"status: {originalVertexPayload.Status}");
    Console.WriteLine($"provenance: {originalVertexPayload.Provenance}");
    Console.WriteLine($"original_semantics: {originalVertexPayload.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, raw original EAGL __VB payload bytes; numeric/range structure only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        originalVertexPayload.Status,
        EaglOriginalVertexPayloadAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-candidate-slot-profile-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-candidate-slot-profile-audit <original-stream-numeric.tsv> <candidate-fetch-association.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var slotProfileOriginalPath = Path.GetFullPath(args[1]);
    var slotProfileCandidatePath = Path.GetFullPath(args[2]);
    string? slotProfileReportPath = null;
    string? slotProfileSummaryPath = null;
    for (var argumentIndex = 3; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            slotProfileReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            slotProfileSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((slotProfileReportPath is not null &&
         (string.Equals(slotProfileReportPath, slotProfileOriginalPath, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(slotProfileReportPath, slotProfileCandidatePath, StringComparison.OrdinalIgnoreCase))) ||
        (slotProfileSummaryPath is not null &&
         (string.Equals(slotProfileSummaryPath, slotProfileOriginalPath, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(slotProfileSummaryPath, slotProfileCandidatePath, StringComparison.OrdinalIgnoreCase))))
    {
        Console.Error.WriteLine("Os relatórios do controle slot-1/categoria-11 não podem sobrescrever os relatórios de entrada.");
        return 2;
    }

    var slotProfileAudit = EaglOriginalCandidateSlotProfileAudit.Audit(
        slotProfileOriginalPath,
        slotProfileCandidatePath);
    if (slotProfileReportPath is not null)
    {
        EaglOriginalCandidateSlotProfileAudit.WriteReport(
            slotProfileReportPath,
            slotProfileAudit.Rows);
    }
    if (slotProfileSummaryPath is not null)
    {
        EaglOriginalCandidateSlotProfileAudit.WriteSummary(
            slotProfileSummaryPath,
            slotProfileAudit.Summary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original/candidate slot-1 category-11 numeric profile control");
    Console.WriteLine($"original_stream_rows: {slotProfileAudit.Summary.OriginalStreamRows}");
    Console.WriteLine($"candidate_primitive_rows: {slotProfileAudit.Summary.CandidatePrimitiveRows}");
    Console.WriteLine($"candidate_complete_five_slot_rows: {slotProfileAudit.Summary.CandidateCompleteFiveSlotRows}");
    Console.WriteLine($"candidate_slot1_rows: {slotProfileAudit.Summary.CandidateSlot1Rows}");
    Console.WriteLine($"symbol_match_rows: {slotProfileAudit.Summary.SymbolMatchRows}");
    Console.WriteLine($"object_match_rows: {slotProfileAudit.Summary.ObjectMatchRows}");
    Console.WriteLine($"category11_rows: {slotProfileAudit.Summary.Category11Rows}");
    Console.WriteLine($"finite_float2_rows: {slotProfileAudit.Summary.FiniteFloat2Rows}");
    Console.WriteLine($"near_unit_range_float2_rows: {slotProfileAudit.Summary.NearUnitRangeFloat2Rows}");
    Console.WriteLine($"stream_stride_match_rows: {slotProfileAudit.Summary.StreamStrideMatchRows}");
    Console.WriteLine($"byte_length_match_rows: {slotProfileAudit.Summary.ByteLengthMatchRows}");
    Console.WriteLine($"element_count_match_rows: {slotProfileAudit.Summary.ElementCountMatchRows}");
    Console.WriteLine($"complete_control_rows: {slotProfileAudit.Summary.CompleteControlRows}");
    if (slotProfileReportPath is not null)
    {
        Console.WriteLine($"report: {slotProfileReportPath}");
    }
    if (slotProfileSummaryPath is not null)
    {
        Console.WriteLine($"summary: {slotProfileSummaryPath}");
    }
    Console.WriteLine($"status: {slotProfileAudit.Summary.Status}");
    Console.WriteLine($"provenance: {slotProfileAudit.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {slotProfileAudit.Summary.OriginalSemantics}");
    Console.WriteLine("mode: independent TSV reparse only; slot-1/category-11 numeric/profile relation remains opaque; no UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        slotProfileAudit.Summary.Status,
        EaglOriginalCandidateSlotProfileAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-candidate-raw-stream-identity-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-candidate-raw-stream-identity-audit <caminho-do-iso> <candidate-fetch-association.tsv> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var rawIdentityIsoPath = Path.GetFullPath(args[1]);
    var rawIdentityCandidatePath = Path.GetFullPath(args[2]);
    string? rawIdentityReportPath = null;
    string? rawIdentitySummaryPath = null;
    for (var argumentIndex = 3; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            rawIdentityReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            rawIdentitySummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((rawIdentityReportPath is not null &&
         (string.Equals(rawIdentityReportPath, rawIdentityIsoPath, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(rawIdentityReportPath, rawIdentityCandidatePath, StringComparison.OrdinalIgnoreCase))) ||
        (rawIdentitySummaryPath is not null &&
         (string.Equals(rawIdentitySummaryPath, rawIdentityIsoPath, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(rawIdentitySummaryPath, rawIdentityCandidatePath, StringComparison.OrdinalIgnoreCase))))
    {
        Console.Error.WriteLine("Os relatórios de identidade de payload bruto não podem sobrescrever a ISO nem o relatório de entrada.");
        return 2;
    }

    using var rawIdentityStore = new GameAssetStore(rawIdentityIsoPath);
    var rawIdentityRows = new List<EaglOriginalCandidateRawStreamIdentityRow>();
    var rawIdentity = EaglOriginalCandidateRawStreamIdentityAudit.Audit(
        rawIdentityStore,
        "data/models.big",
        rawIdentityCandidatePath,
        rawIdentityRows);
    if (rawIdentityReportPath is not null)
    {
        EaglOriginalCandidateRawStreamIdentityAudit.WriteReport(
            rawIdentityReportPath,
            rawIdentityRows);
    }
    if (rawIdentitySummaryPath is not null)
    {
        EaglOriginalCandidateRawStreamIdentityAudit.WriteSummary(
            rawIdentitySummaryPath,
            rawIdentity);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original/candidate raw __VB payload identity control");
    Console.WriteLine($"candidate_object_count: {rawIdentity.CandidateObjectCount}");
    Console.WriteLine($"candidate_primitive_rows: {rawIdentity.CandidatePrimitiveRows}");
    Console.WriteLine($"candidate_complete_five_slot_rows: {rawIdentity.CandidateCompleteFiveSlotRows}");
    Console.WriteLine($"candidate_slot_rows: {rawIdentity.CandidateSlotRows}");
    Console.WriteLine($"raw_objects_loaded: {rawIdentity.RawObjectsLoaded}");
    Console.WriteLine($"raw_streams_loaded: {rawIdentity.RawStreamsLoaded}");
    Console.WriteLine($"raw_valid_slot_rows: {rawIdentity.RawValidSlotRows}");
    Console.WriteLine($"raw_metadata_exact_rows: {rawIdentity.RawMetadataExactRows}");
    Console.WriteLine($"raw_payload_hash_rows: {rawIdentity.RawPayloadHashRows}");
    Console.WriteLine($"complete_identity_rows: {rawIdentity.CompleteIdentityRows}");
    Console.WriteLine($"rejected_objects: {rawIdentity.RejectedObjects}");
    if (rawIdentityReportPath is not null)
    {
        Console.WriteLine($"report: {rawIdentityReportPath}");
    }
    if (rawIdentitySummaryPath is not null)
    {
        Console.WriteLine($"summary: {rawIdentitySummaryPath}");
    }
    Console.WriteLine($"status: {rawIdentity.Status}");
    Console.WriteLine($"provenance: {rawIdentity.Provenance}");
    Console.WriteLine($"original_semantics: {rawIdentity.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, full raw __VB payload hashes only; slot and state semantics remain opaque; no UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        rawIdentity.Status,
        EaglOriginalCandidateRawStreamIdentityAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-original-state-token-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-original-state-token-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? originalStateTokenReportPath = null;
    string? originalStateTokenSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            originalStateTokenReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            originalStateTokenSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((originalStateTokenReportPath is not null &&
         string.Equals(originalStateTokenReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (originalStateTokenSummaryPath is not null &&
         string.Equals(originalStateTokenSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios dos tokens de estado original não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var originalStateTokenStore = new GameAssetStore(args[1]);
    var originalStateToken = EaglOriginalStateTokenAudit.Audit(
        originalStateTokenStore,
        "data/models.big");
    if (originalStateTokenReportPath is not null)
    {
        EaglOriginalStateTokenAudit.WriteReport(
            originalStateTokenReportPath,
            originalStateToken.Rows);
    }
    if (originalStateTokenSummaryPath is not null)
    {
        EaglOriginalStateTokenAudit.WriteSummary(
            originalStateTokenSummaryPath,
            originalStateToken.Summary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC original EAGL state-token negative control");
    Console.WriteLine($"input_rows: {originalStateToken.Summary.InputRows}");
    Console.WriteLine($"state_rows: {originalStateToken.Summary.StateRows}");
    Console.WriteLine($"valid_input_rows: {originalStateToken.Summary.ValidInputRows}");
    Console.WriteLine($"shapename_rows: {originalStateToken.Summary.ShapeNameRows}");
    Console.WriteLine($"clamp_u_rows: {originalStateToken.Summary.ClampURows}");
    Console.WriteLine($"clamp_v_rows: {originalStateToken.Summary.ClampVRows}");
    Console.WriteLine($"primitive_type_rows: {originalStateToken.Summary.PrimitiveTypeRows}");
    Console.WriteLine($"texture_token_rows: {originalStateToken.Summary.TextureTokenRows}");
    Console.WriteLine($"material_token_rows: {originalStateToken.Summary.MaterialTokenRows}");
    Console.WriteLine($"uv_coordinate_token_rows: {originalStateToken.Summary.UvCoordinateTokenRows}");
    Console.WriteLine($"vertex_attribute_token_rows: {originalStateToken.Summary.VertexAttributeTokenRows}");
    Console.WriteLine($"deformation_token_rows: {originalStateToken.Summary.DeformationTokenRows}");
    Console.WriteLine($"assignment_name_catalog: {originalStateToken.Summary.AssignmentNameCatalog}");
    if (originalStateTokenReportPath is not null)
    {
        Console.WriteLine($"report: {originalStateTokenReportPath}");
    }
    if (originalStateTokenSummaryPath is not null)
    {
        Console.WriteLine($"summary: {originalStateTokenSummaryPath}");
    }
    Console.WriteLine($"status: {originalStateToken.Summary.Status}");
    Console.WriteLine($"provenance: {originalStateToken.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {originalStateToken.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, raw original EAGL state symbols; literal vocabulary and negative control only; no geometry, UV, material, skinning, rigging, animation, morph or GPU semantics");
    return string.Equals(
        originalStateToken.Summary.Status,
        EaglOriginalStateTokenAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-candidate-fetch-association-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-fetch-association-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? fetchAssociationReportPath = null;
    string? fetchAssociationSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            fetchAssociationReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            fetchAssociationSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if ((fetchAssociationReportPath is not null &&
         string.Equals(fetchAssociationReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)) ||
        (fetchAssociationSummaryPath is not null &&
         string.Equals(fetchAssociationSummaryPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios da associação de fetch não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var fetchAssociationStore = new GameAssetStore(args[1]);
    var fetchAssociation = EaglCandidateFetchAssociationAudit.Audit(
        fetchAssociationStore,
        "data/models.big");
    if (fetchAssociationReportPath is not null)
    {
        EaglCandidateFetchAssociationAudit.WriteReport(
            fetchAssociationReportPath,
            fetchAssociation.Rows);
    }
    if (fetchAssociationSummaryPath is not null)
    {
        EaglCandidateFetchAssociationAudit.WriteSummary(
            fetchAssociationSummaryPath,
            fetchAssociation.Summary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC candidate fetch-record association audit");
    Console.WriteLine($"candidate_count: {fetchAssociation.Summary.CandidateCount}");
    Console.WriteLine($"candidate_assets_loaded: {fetchAssociation.Summary.CandidateAssetsLoaded}");
    Console.WriteLine($"candidate_assets_with_complete_five_slot_layouts: {fetchAssociation.Summary.CandidateAssetsWithCompleteFiveSlotLayouts}");
    Console.WriteLine($"primitive_rows: {fetchAssociation.Summary.PrimitiveRows}");
    Console.WriteLine($"complete_five_slot_primitive_rows: {fetchAssociation.Summary.CompleteFiveSlotPrimitiveRows}");
    Console.WriteLine($"slot_rows: {fetchAssociation.Summary.SlotRows}");
    Console.WriteLine($"valid_slot_rows: {fetchAssociation.Summary.ValidSlotRows}");
    Console.WriteLine($"rejected_candidates: {fetchAssociation.Summary.RejectedCandidates}");
    if (fetchAssociationReportPath is not null)
    {
        Console.WriteLine($"report: {fetchAssociationReportPath}");
    }
    if (fetchAssociationSummaryPath is not null)
    {
        Console.WriteLine($"summary: {fetchAssociationSummaryPath}");
    }
    Console.WriteLine($"status: {fetchAssociation.Summary.Status}");
    Console.WriteLine($"provenance: {fetchAssociation.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {fetchAssociation.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, same-trunk managed candidate objects; slot and __VB metadata remain semantically opaque");
    return string.Equals(
        fetchAssociation.Summary.Status,
        EaglCandidateFetchAssociationAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-candidate-geometry-stream-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-geometry-stream-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    string? geometryStreamReportPath = null;
    string? geometryStreamSummaryPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            geometryStreamReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            geometryStreamSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    var geometryStreamIsoPath = Path.GetFullPath(args[1]);
    if ((geometryStreamReportPath is not null &&
         string.Equals(geometryStreamReportPath, geometryStreamIsoPath, StringComparison.OrdinalIgnoreCase)) ||
        (geometryStreamSummaryPath is not null &&
         string.Equals(geometryStreamSummaryPath, geometryStreamIsoPath, StringComparison.OrdinalIgnoreCase)))
    {
        Console.Error.WriteLine("Os relatórios de geometry-stream não podem sobrescrever a ISO de origem.");
        return 2;
    }

    using var geometryStreamStore = new GameAssetStore(geometryStreamIsoPath);
    var geometryStreamAudit = EaglCandidateGeometryStreamAudit.Audit(
        geometryStreamStore,
        "data/models.big");
    if (geometryStreamReportPath is not null)
    {
        EaglCandidateGeometryStreamAudit.WriteReport(
            geometryStreamReportPath,
            geometryStreamAudit.Rows);
    }
    if (geometryStreamSummaryPath is not null)
    {
        EaglCandidateGeometryStreamAudit.WriteSummary(
            geometryStreamSummaryPath,
            geometryStreamAudit.Summary);
    }

    Console.WriteLine($"{GameIdentity.Name} PC candidate geometry-stream audit");
    Console.WriteLine($"candidate_count: {geometryStreamAudit.Summary.CandidateCount}");
    Console.WriteLine($"candidate_assets_loaded: {geometryStreamAudit.Summary.CandidateAssetsLoaded}");
    Console.WriteLine($"candidate_groups: {geometryStreamAudit.Summary.CandidateGroups}");
    Console.WriteLine($"candidate_groups_with_primitive: {geometryStreamAudit.Summary.CandidateGroupsWithPrimitive}");
    Console.WriteLine($"candidate_source_slot_zero_rows: {geometryStreamAudit.Summary.CandidateSourceSlotZeroRows}");
    Console.WriteLine($"candidate_complete_primitive_rows: {geometryStreamAudit.Summary.CandidateCompletePrimitiveRows}");
    Console.WriteLine($"candidate_finite_and_indexed_rows: {geometryStreamAudit.Summary.CandidateFiniteAndIndexedRows}");
    Console.WriteLine($"rejected_candidates: {geometryStreamAudit.Summary.RejectedCandidates}");
    if (geometryStreamReportPath is not null)
    {
        Console.WriteLine($"report: {geometryStreamReportPath}");
    }
    if (geometryStreamSummaryPath is not null)
    {
        Console.WriteLine($"summary: {geometryStreamSummaryPath}");
    }
    Console.WriteLine($"status: {geometryStreamAudit.Summary.Status}");
    Console.WriteLine($"provenance: {geometryStreamAudit.Summary.Provenance}");
    Console.WriteLine($"original_semantics: {geometryStreamAudit.Summary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only; candidate source-slot and stream metadata only; no position, UV, material, skinning, rigging, animation, morph or GPU binding assigned");
    return string.Equals(
        geometryStreamAudit.Summary.Status,
        EaglCandidateGeometryStreamAudit.Status,
        StringComparison.Ordinal)
        ? 0
        : 1;
}

if (string.Equals(args[0], "eagl-material-runtime-batch-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-runtime-batch-smoke <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? runtimeBatchReportPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            runtimeBatchReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (runtimeBatchReportPath is not null &&
        string.Equals(runtimeBatchReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório do runtime candidato não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var runtimeBatchStore = new GameAssetStore(args[1]);
    using var runtimeBatchTitleStream = runtimeBatchStore.OpenRaw("data/titleimage.xbx");
    using var runtimeBatchTitleMemory = new MemoryStream();
    runtimeBatchTitleStream.CopyTo(runtimeBatchTitleMemory);
    var runtimeBatchTitleTexture = TextureAssetLoader.Load(runtimeBatchTitleMemory.ToArray());
    var runtimeBatchMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        runtimeBatchStore,
        "data/models.big");
    var runtimeBatchChecksums = new HashSet<ulong>();
    var runtimeBatchTexturedGroups = 0;
    var runtimeBatchVerified = 0;
    var runtimeBatchObjectStateMatches = 0;
    var runtimeBatchRenderGroupStateMatches = 0;
    var runtimeBatchHomeSlotVerified = 0;
    var runtimeBatchAwaySlotVerified = 0;
    var runtimeBatchBothSlotVerified = 0;
    var runtimeBatchHomeSlotChecksums = new HashSet<ulong>();
    var runtimeBatchAwaySlotChecksums = new HashSet<ulong>();
    var runtimeBatchBothSlotChecksums = new HashSet<ulong>();
    var runtimeBatchRejected = 0;
    var runtimeBatchReportLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "model_archive",
            "model_object",
            "shape_name",
            "texture_width",
            "texture_height",
            "texture_format",
            "groups",
            "vertices",
            "triangles",
            "textured_groups",
            "frame_checksum",
            "candidate_home_slot_checksum",
            "candidate_away_slot_checksum",
            "candidate_both_slot_checksum",
            "object_state",
            "render_group_state",
            "status",
            "issue",
        }),
    };
    foreach (var match in runtimeBatchMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                runtimeBatchStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    match.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{match.ShapeName}'.");
            }

            runtimeBatchObjectStateMatches++;
            if (EaglShapeMaterialLoader.HasExactShapeState(candidate.Mesh, match.ShapeName))
            {
                runtimeBatchRenderGroupStateMatches++;
            }

            var renderer = new SoftwareGameplayRenderer(
                titleTexture: runtimeBatchTitleTexture,
                candidateMesh: candidate.Mesh,
                candidateTexture: match.Texture);
            renderer.Initialize();
            renderer.BeginFrame();
            renderer.Render(new StreetMatch());
            renderer.EndFrame();
            if (renderer.RenderedFrameCount != 1 || renderer.LastFrameChecksum == 0)
            {
                throw new InvalidDataException("Integrated candidate frame was not produced.");
            }

            runtimeBatchChecksums.Add(renderer.LastFrameChecksum);
            var texturedGroups = candidate.Mesh.Groups.Count(group =>
                group.Slot1Attributes.Count == group.Vertices.Count);
            runtimeBatchTexturedGroups += texturedGroups;

            var homeSlotRenderer = new SoftwareGameplayRenderer(
                titleTexture: runtimeBatchTitleTexture,
                candidateMesh: candidate.Mesh,
                candidateTexture: match.Texture,
                cleanRoomCharacters: true,
                candidateHomeCharacter: true);
            var awaySlotRenderer = new SoftwareGameplayRenderer(
                titleTexture: runtimeBatchTitleTexture,
                candidateMesh: candidate.Mesh,
                candidateTexture: match.Texture,
                cleanRoomCharacters: true,
                candidateAwayCharacter: true);
            var bothSlotRenderer = new SoftwareGameplayRenderer(
                titleTexture: runtimeBatchTitleTexture,
                candidateMesh: candidate.Mesh,
                candidateTexture: match.Texture,
                cleanRoomCharacters: true,
                candidateHomeCharacter: true,
                candidateAwayCharacter: true);
            homeSlotRenderer.Render(new StreetMatch());
            awaySlotRenderer.Render(new StreetMatch());
            bothSlotRenderer.Render(new StreetMatch());
            if (homeSlotRenderer.RenderedFrameCount != 1 || homeSlotRenderer.LastFrameChecksum == 0 ||
                awaySlotRenderer.RenderedFrameCount != 1 || awaySlotRenderer.LastFrameChecksum == 0 ||
                bothSlotRenderer.RenderedFrameCount != 1 || bothSlotRenderer.LastFrameChecksum == 0)
            {
                throw new InvalidDataException("Candidate slot scene frames were not produced.");
            }

            runtimeBatchHomeSlotVerified++;
            runtimeBatchAwaySlotVerified++;
            runtimeBatchBothSlotVerified++;
            runtimeBatchHomeSlotChecksums.Add(homeSlotRenderer.LastFrameChecksum);
            runtimeBatchAwaySlotChecksums.Add(awaySlotRenderer.LastFrameChecksum);
            runtimeBatchBothSlotChecksums.Add(bothSlotRenderer.LastFrameChecksum);
            runtimeBatchVerified++;
            runtimeBatchReportLines.Add(string.Join("\t", new[]
            {
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                match.Texture.Width.ToString(CultureInfo.InvariantCulture),
                match.Texture.Height.ToString(CultureInfo.InvariantCulture),
                CatalogField(match.Texture.PixelFormat),
                candidate.Mesh.Groups.Count.ToString(CultureInfo.InvariantCulture),
                candidate.Mesh.VertexCount.ToString(CultureInfo.InvariantCulture),
                candidate.Mesh.TriangleCount.ToString(CultureInfo.InvariantCulture),
                texturedGroups.ToString(CultureInfo.InvariantCulture),
                renderer.LastFrameChecksum.ToString("X16", CultureInfo.InvariantCulture),
                homeSlotRenderer.LastFrameChecksum.ToString("X16", CultureInfo.InvariantCulture),
                awaySlotRenderer.LastFrameChecksum.ToString("X16", CultureInfo.InvariantCulture),
                bothSlotRenderer.LastFrameChecksum.ToString("X16", CultureInfo.InvariantCulture),
                "exact",
                EaglShapeMaterialLoader.HasExactShapeState(candidate.Mesh, match.ShapeName)
                    ? "exact"
                    : "unproven",
                "verified-candidate",
                string.Empty,
            }));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException or FileNotFoundException)
        {
            runtimeBatchRejected++;
            Console.WriteLine($"rejected: {match.ModelArchiveName} | object={match.ModelObjectName} | {exception.Message}");
            runtimeBatchReportLines.Add(string.Join("\t", new[]
            {
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                match.Texture.Width.ToString(CultureInfo.InvariantCulture),
                match.Texture.Height.ToString(CultureInfo.InvariantCulture),
                CatalogField(match.Texture.PixelFormat),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "unknown",
                "unknown",
                "rejected-candidate",
                CatalogField(exception.Message),
            }));
        }
    }

    if (runtimeBatchReportPath is not null)
    {
        var reportDirectory = Path.GetDirectoryName(runtimeBatchReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        File.WriteAllLines(
            runtimeBatchReportPath,
            runtimeBatchReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL same-trunk integrated render batch smoke");
    Console.WriteLine($"verified_material_matches: {runtimeBatchMatches.Count}");
    Console.WriteLine($"verified_runtime_frames: {runtimeBatchVerified}");
    Console.WriteLine($"verified_object_state_matches: {runtimeBatchObjectStateMatches}");
    Console.WriteLine($"verified_render_group_state_matches: {runtimeBatchRenderGroupStateMatches}");
    Console.WriteLine($"verified_candidate_home_slot_frames: {runtimeBatchHomeSlotVerified}");
    Console.WriteLine($"verified_candidate_away_slot_frames: {runtimeBatchAwaySlotVerified}");
    Console.WriteLine($"verified_candidate_both_slot_frames: {runtimeBatchBothSlotVerified}");
    Console.WriteLine($"rejected_runtime_frames: {runtimeBatchRejected}");
    Console.WriteLine($"distinct_runtime_checksums: {runtimeBatchChecksums.Count}");
    Console.WriteLine($"distinct_candidate_home_slot_checksums: {runtimeBatchHomeSlotChecksums.Count}");
    Console.WriteLine($"distinct_candidate_away_slot_checksums: {runtimeBatchAwaySlotChecksums.Count}");
    Console.WriteLine($"distinct_candidate_both_slot_checksums: {runtimeBatchBothSlotChecksums.Count}");
    Console.WriteLine($"textured_candidate_groups: {runtimeBatchTexturedGroups}");
    if (runtimeBatchReportPath is not null)
    {
        Console.WriteLine($"report: {runtimeBatchReportPath}");
    }
    Console.WriteLine("path: titleimage.xbx + candidate mesh/texture -> SoftwareGameplayRenderer -> PreviewRenderer");
    Console.WriteLine("status: candidate integration only; no global UV/material/GPU draw semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software runtime");
    return runtimeBatchRejected == 0 &&
           runtimeBatchVerified == runtimeBatchMatches.Count &&
           runtimeBatchObjectStateMatches == runtimeBatchMatches.Count &&
           runtimeBatchHomeSlotVerified == runtimeBatchMatches.Count &&
           runtimeBatchAwaySlotVerified == runtimeBatchMatches.Count &&
           runtimeBatchBothSlotVerified == runtimeBatchMatches.Count ? 0 : 1;
}

if (string.Equals(args[0], "eagl-candidate-render-influence-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-render-influence-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? renderInfluenceReportPath = null;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            renderInfluenceReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (renderInfluenceReportPath is not null &&
        string.Equals(renderInfluenceReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de influência do rasterizador não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var renderInfluenceStore = new GameAssetStore(args[1]);
    using var renderInfluenceTitleStream = renderInfluenceStore.OpenRaw("data/titleimage.xbx");
    using var renderInfluenceTitleMemory = new MemoryStream();
    renderInfluenceTitleStream.CopyTo(renderInfluenceTitleMemory);
    var renderInfluenceTitleTexture = TextureAssetLoader.Load(renderInfluenceTitleMemory.ToArray());
    var renderInfluenceMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        renderInfluenceStore,
        "data/models.big");
    var renderInfluenceRows = new List<CandidateRenderInfluenceRow>();
    foreach (var match in renderInfluenceMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                renderInfluenceStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            var geometry = EaglCandidateGeometryAuditor.Measure(candidate.Mesh);
            var textureColors = new HashSet<uint>();
            var nonTransparentPixels = 0;
            for (var pixelOffset = 0; pixelOffset + 3 < match.Texture.DecodedRgba.Length; pixelOffset += 4)
            {
                textureColors.Add(BinaryPrimitives.ReadUInt32LittleEndian(
                    match.Texture.DecodedRgba.AsSpan(pixelOffset, 4)));
                if (match.Texture.DecodedRgba[pixelOffset + 3] != 0)
                {
                    nonTransparentPixels++;
                }
            }
            var primitiveShapeStateExact = EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                candidate.Primitives,
                match.ShapeName);
            var meshShapeStateExact = EaglShapeMaterialLoader.HasExactShapeState(
                candidate.Mesh,
                match.ShapeName);
            var texturedRenderer = new SoftwareGameplayRenderer(
                titleTexture: renderInfluenceTitleTexture,
                candidateMesh: candidate.Mesh,
                candidateTexture: match.Texture,
                cleanRoomCharacters: true,
                candidateHomeCharacter: true);
            var solidRenderer = new SoftwareGameplayRenderer(
                titleTexture: renderInfluenceTitleTexture,
                candidateMesh: candidate.Mesh,
                cleanRoomCharacters: true,
                candidateHomeCharacter: true);
            var matchState = new StreetMatch();
            texturedRenderer.Render(matchState);
            solidRenderer.Render(new StreetMatch());
            var texturedGroups = candidate.Mesh.Groups.Count(group =>
                group.Slot1Attributes.Count == group.Vertices.Count);
            var differingRasterPixels = texturedRenderer.Frame.CountDifferingPixels(solidRenderer.Frame);
            var textureInfluencesRaster = texturedRenderer.RenderedFrameCount == 1 &&
                                          solidRenderer.RenderedFrameCount == 1 &&
                                          texturedRenderer.LastFrameChecksum != 0 &&
                                          solidRenderer.LastFrameChecksum != 0 &&
                                          differingRasterPixels > 0 &&
                                          texturedRenderer.LastFrameChecksum != solidRenderer.LastFrameChecksum;
            var isolatedTexturedFrame = new SoftwareFrameBuffer(512, 512);
            var isolatedSolidFrame = new SoftwareFrameBuffer(512, 512);
            isolatedTexturedFrame.Clear(new RgbColor(12, 16, 24));
            isolatedSolidFrame.Clear(new RgbColor(12, 16, 24));
            EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
                isolatedTexturedFrame,
                candidate.Mesh,
                match.Texture,
                256,
                256,
                390);
            EaglCandidateMeshRenderer.DrawSolid(
                isolatedSolidFrame,
                candidate.Mesh,
                256,
                256,
                390,
                new RgbColor(166, 42, 48));
            var isolatedDifferingRasterPixels = isolatedTexturedFrame.CountDifferingPixels(isolatedSolidFrame);
            var isolatedTextureInfluencesRaster = isolatedDifferingRasterPixels > 0;
            // Full-scene composition can hide a texture change through draw
            // ordering or occlusion. The isolated comparison is bounded
            // candidate evidence for that case, not an original binding claim.
            var candidateTextureInfluencesRaster = textureInfluencesRaster ||
                                                   isolatedTextureInfluencesRaster;
            var verified = geometry.HasOnlyFiniteInRangeGeometry &&
                           texturedGroups > 0 &&
                           candidateTextureInfluencesRaster;
            renderInfluenceRows.Add(new CandidateRenderInfluenceRow(
                match.ModelArchiveName,
                match.ModelObjectName,
                match.ShapeName,
                texturedGroups,
                geometry.RasterVisibleTriangles,
                textureColors.Count,
                nonTransparentPixels,
                differingRasterPixels,
                isolatedDifferingRasterPixels,
                isolatedTextureInfluencesRaster,
                geometry.HasOnlyFiniteInRangeGeometry,
                primitiveShapeStateExact,
                meshShapeStateExact,
                texturedRenderer.LastFrameChecksum,
                solidRenderer.LastFrameChecksum,
                candidateTextureInfluencesRaster,
                verified
                    ? "verified-candidate-raster-influence"
                    : geometry.HasOnlyFiniteInRangeGeometry &&
                      texturedGroups > 0 &&
                      differingRasterPixels == 0 &&
                      texturedRenderer.LastFrameChecksum == solidRenderer.LastFrameChecksum
                        ? "observed-candidate-raster-unchanged"
                        : "incomplete-candidate-raster-influence",
                verified
                    ? string.Empty
                    : "finite/indexed geometry, complete generic slot-1 profile, or full/isolated distinct raster output was not observed"));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException or FileNotFoundException)
        {
            renderInfluenceRows.Add(new CandidateRenderInfluenceRow(
                ModelArchiveName: match.ModelArchiveName,
                ModelObjectName: match.ModelObjectName,
                ShapeName: match.ShapeName,
                TexturedCandidateGroups: 0,
                RasterVisibleTriangles: 0,
                TextureDistinctRgbaColors: 0,
                TextureNonTransparentPixels: 0,
                DifferingRasterPixels: 0,
                IsolatedDifferingRasterPixels: 0,
                IsolatedTextureInfluencesRaster: false,
                GeometryFiniteAndIndexed: false,
                PrimitiveShapeStateExact: false,
                MeshShapeStateExact: false,
                TexturedChecksum: 0,
                SolidChecksum: 0,
                CandidateTextureInfluencesRaster: false,
                Status: "rejected-candidate-raster-influence",
                Issue: exception.Message));
        }
    }

    var renderInfluenceAudit = CandidateRenderInfluenceAudit.Audit(renderInfluenceRows);
    if (renderInfluenceReportPath is not null)
    {
        CandidateRenderInfluenceAudit.Write(renderInfluenceReportPath, renderInfluenceAudit);
    }

    Console.WriteLine($"{GameIdentity.Name} PC managed candidate render influence audit");
    Console.WriteLine($"candidate_matches: {renderInfluenceAudit.Summary.CandidateCount}");
    Console.WriteLine($"verified_candidate_raster_influence: {renderInfluenceAudit.Summary.VerifiedCount}");
    Console.WriteLine($"geometry_finite_and_indexed: {renderInfluenceAudit.Summary.GeometryValidCount}");
    Console.WriteLine($"candidate_texture_influences_raster: {renderInfluenceAudit.Summary.TextureInfluenceCount}");
    Console.WriteLine($"full_scene_candidate_texture_influences_raster: {renderInfluenceRows.Count(row => row.DifferingRasterPixels > 0 && row.TexturedChecksum != row.SolidChecksum)}");
    Console.WriteLine($"isolated_candidate_texture_influences_raster: {renderInfluenceAudit.Summary.IsolatedTextureInfluenceCount}");
    Console.WriteLine($"distinct_textured_checksums: {renderInfluenceAudit.Summary.DistinctTexturedChecksums}");
    Console.WriteLine($"distinct_solid_checksums: {renderInfluenceAudit.Summary.DistinctSolidChecksums}");
    Console.WriteLine($"status: {renderInfluenceAudit.Summary.Status}; original_semantics={renderInfluenceAudit.Summary.OriginalSemantics}");
    if (renderInfluenceReportPath is not null)
    {
        Console.WriteLine($"report: {renderInfluenceReportPath}");
    }
    Console.WriteLine("path: same-trunk candidate mesh/texture -> textured managed raster vs identical solid fallback");
    Console.WriteLine("scope: candidate raster influence only; generic slot-1 values are not promoted to UV/material/GPU semantics");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software runtime");
    return renderInfluenceRows.Count == renderInfluenceMatches.Count &&
           renderInfluenceRows.All(row => row.Status != "rejected-candidate-raster-influence") ? 0 : 1;
}

if (string.Equals(args[0], "eagl-candidate-attribute-raster-influence-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-attribute-raster-influence-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? attributeRasterInfluenceReportPath = null;
    var attributeRasterInfluenceReportOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--report", StringComparison.OrdinalIgnoreCase));
    if (attributeRasterInfluenceReportOption >= 0)
    {
        if (attributeRasterInfluenceReportOption + 1 >= args.Length ||
            args[attributeRasterInfluenceReportOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-attribute-raster-influence-audit ... --report <saida.tsv>");
            return 2;
        }

        attributeRasterInfluenceReportPath = Path.GetFullPath(args[attributeRasterInfluenceReportOption + 1]);
    }

    if (attributeRasterInfluenceReportPath is not null &&
        string.Equals(attributeRasterInfluenceReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de influência de atributo não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var attributeRasterInfluenceStore = new GameAssetStore(args[1]);
    var attributeRasterInfluenceMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        attributeRasterInfluenceStore,
        "data/models.big");
    var attributeRasterInfluenceRows = new List<CandidateAttributeRasterInfluenceRow>();
    foreach (var match in attributeRasterInfluenceMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                attributeRasterInfluenceStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            var geometry = EaglCandidateGeometryAuditor.Measure(candidate.Mesh);
            var completeAttributeGroups = candidate.Mesh.Groups.Count(group =>
                group.Slot1Attributes.Count == group.Vertices.Count);
            var attributeCardinalityValid = candidate.Mesh.Groups.Count > 0 &&
                                            completeAttributeGroups == candidate.Mesh.Groups.Count;
            var perturbedMesh = CandidateAttributeRasterInfluenceAudit.PerturbOpaqueSlot1Values(candidate.Mesh);
            var baselineFrame = new SoftwareFrameBuffer(512, 512);
            var perturbedFrame = new SoftwareFrameBuffer(512, 512);
            baselineFrame.Clear(new RgbColor(12, 16, 24));
            perturbedFrame.Clear(new RgbColor(12, 16, 24));
            var baselineDrawnGroups = EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
                baselineFrame,
                candidate.Mesh,
                match.Texture,
                256,
                256,
                390);
            var perturbedDrawnGroups = EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
                perturbedFrame,
                perturbedMesh,
                match.Texture,
                256,
                256,
                390);
            var differingRasterPixels = baselineFrame.CountDifferingPixels(perturbedFrame);
            var baselineChecksum = baselineFrame.ComputeChecksum();
            var perturbedChecksum = perturbedFrame.ComputeChecksum();
            var attributePerturbsRaster = differingRasterPixels > 0 &&
                                          baselineChecksum != perturbedChecksum;
            var verified = geometry.HasOnlyFiniteInRangeGeometry &&
                           attributeCardinalityValid &&
                           baselineDrawnGroups == completeAttributeGroups &&
                           perturbedDrawnGroups == completeAttributeGroups &&
                           attributePerturbsRaster;
            attributeRasterInfluenceRows.Add(new CandidateAttributeRasterInfluenceRow(
                match.ModelArchiveName,
                match.ModelObjectName,
                match.ShapeName,
                completeAttributeGroups,
                baselineDrawnGroups,
                perturbedDrawnGroups,
                differingRasterPixels,
                baselineChecksum,
                perturbedChecksum,
                geometry.HasOnlyFiniteInRangeGeometry,
                attributeCardinalityValid,
                attributePerturbsRaster,
                verified
                    ? "verified-candidate-attribute-raster-influence"
                    : "incomplete-candidate-attribute-raster-influence",
                verified
                    ? string.Empty
                    : "finite/indexed geometry, complete opaque attribute cardinality, or perturbation raster difference was not observed"));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or
            ArgumentOutOfRangeException or ArgumentException or FileNotFoundException)
        {
            attributeRasterInfluenceRows.Add(new CandidateAttributeRasterInfluenceRow(
                match.ModelArchiveName,
                match.ModelObjectName,
                match.ShapeName,
                0,
                0,
                0,
                0,
                0,
                0,
                false,
                false,
                false,
                "rejected-candidate-attribute-raster-influence",
                exception.Message));
        }
    }

    var attributeRasterInfluenceAudit = CandidateAttributeRasterInfluenceAudit.Audit(attributeRasterInfluenceRows);
    if (attributeRasterInfluenceReportPath is not null)
    {
        CandidateAttributeRasterInfluenceAudit.Write(attributeRasterInfluenceReportPath, attributeRasterInfluenceAudit);
    }

    Console.WriteLine($"{GameIdentity.Name} PC managed candidate attribute raster influence audit");
    Console.WriteLine($"candidate_matches: {attributeRasterInfluenceAudit.Summary.CandidateCount}");
    Console.WriteLine($"verified_candidate_attribute_raster_influence: {attributeRasterInfluenceAudit.Summary.VerifiedCount}");
    Console.WriteLine($"geometry_finite_and_indexed: {attributeRasterInfluenceAudit.Summary.GeometryValidCount}");
    Console.WriteLine($"attribute_cardinality_valid: {attributeRasterInfluenceAudit.Summary.AttributeCardinalityValidCount}");
    Console.WriteLine($"attribute_perturbs_raster: {attributeRasterInfluenceAudit.Summary.AttributePerturbationCount}");
    Console.WriteLine($"distinct_baseline_checksums: {attributeRasterInfluenceAudit.Summary.DistinctBaselineChecksums}");
    Console.WriteLine($"distinct_perturbed_checksums: {attributeRasterInfluenceAudit.Summary.DistinctPerturbedChecksums}");
    Console.WriteLine($"status: {attributeRasterInfluenceAudit.Summary.Status}; original_semantics={attributeRasterInfluenceAudit.Summary.OriginalSemantics}");
    if (attributeRasterInfluenceReportPath is not null)
    {
        Console.WriteLine($"report: {attributeRasterInfluenceReportPath}");
    }
    Console.WriteLine("path: same-trunk candidate mesh/texture -> baseline textured raster vs deterministic opaque-attribute perturbation");
    Console.WriteLine("scope: candidate managed raster influence only; slot-1 values are not promoted to UV/material/GPU semantics");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software runtime");
    return attributeRasterInfluenceRows.Count == attributeRasterInfluenceMatches.Count &&
           attributeRasterInfluenceRows.All(row => row.Status == "verified-candidate-attribute-raster-influence") ? 0 : 1;
}

if (string.Equals(args[0], "eagl-material-batch-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-batch-preview <caminho-do-iso> <diretório-de-saída>");
        return 2;
    }

    var batchPreviewOutputDirectory = Path.GetFullPath(args[2]);
    if (string.Equals(
            batchPreviewOutputDirectory,
            Path.GetFullPath(args[1]),
            StringComparison.OrdinalIgnoreCase) ||
        File.Exists(batchPreviewOutputDirectory))
    {
        Console.Error.WriteLine("O diretório de prévias não pode ser a ISO ou um arquivo existente.");
        return 2;
    }

    Directory.CreateDirectory(batchPreviewOutputDirectory);
    using var batchPreviewStore = new GameAssetStore(args[1]);
    var batchPreviewMatches = EaglShapeMaterialLoader.LoadSameTrunkMatches(
        batchPreviewStore,
        "data/models.big");
    var batchPreviewLines = new List<string>
    {
        string.Join("\t", new[]
        {
            "output",
            "model_archive",
            "model_object",
            "shape_name",
            "texture_width",
            "texture_height",
            "texture_format",
            "groups",
            "vertices",
            "triangles",
            "textured_groups",
            "frame_checksum",
            "object_state",
            "render_group_state",
            "status",
            "issue",
        }),
    };
    var batchPreviewVerified = 0;
    var batchPreviewObjectStateMatches = 0;
    var batchPreviewRenderGroupStateMatches = 0;
    var batchPreviewRejected = 0;
    foreach (var match in batchPreviewMatches
                 .OrderBy(item => item.ModelArchiveName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ModelObjectName, StringComparer.OrdinalIgnoreCase)
                 .ThenBy(item => item.ShapeName, StringComparer.OrdinalIgnoreCase))
    {
        var outputName = $"{Path.GetFileNameWithoutExtension(match.ModelArchiveName)}-{Path.GetFileNameWithoutExtension(match.ModelObjectName)}-{SafeFilePart(match.ShapeName)}.ppm";
        var outputPath = Path.Combine(batchPreviewOutputDirectory, outputName);
        try
        {
            var candidate = EaglCandidateAssetLoader.Load(
                batchPreviewStore,
                "data/models.big",
                match.ModelArchiveName,
                match.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidate.Primitives,
                    match.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{match.ShapeName}'.");
            }

            batchPreviewObjectStateMatches++;
            var renderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
                candidate.Mesh,
                match.ShapeName);
            if (renderGroupState)
            {
                batchPreviewRenderGroupStateMatches++;
            }

            var frame = new SoftwareFrameBuffer(960, 540);
            frame.Clear(new RgbColor(13, 18, 27));
            var texturedGroups = EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
                frame,
                candidate.Mesh,
                match.Texture,
                480,
                270,
                390);
            EaglCandidateMeshRenderer.DrawWireframe(
                frame,
                candidate.Mesh,
                480,
                270,
                390,
                new RgbColor(235, 235, 235));
            frame.SavePpm(outputPath);
            batchPreviewLines.Add(string.Join("\t", new[]
            {
                CatalogField(outputPath),
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                match.Texture.Width.ToString(CultureInfo.InvariantCulture),
                match.Texture.Height.ToString(CultureInfo.InvariantCulture),
                CatalogField(match.Texture.PixelFormat),
                candidate.Mesh.Groups.Count.ToString(CultureInfo.InvariantCulture),
                candidate.Mesh.VertexCount.ToString(CultureInfo.InvariantCulture),
                candidate.Mesh.TriangleCount.ToString(CultureInfo.InvariantCulture),
                texturedGroups.ToString(CultureInfo.InvariantCulture),
                frame.ComputeChecksum().ToString("X16", CultureInfo.InvariantCulture),
                "exact",
                renderGroupState ? "exact" : "unproven",
                "verified-candidate",
                string.Empty,
            }));
            batchPreviewVerified++;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            batchPreviewLines.Add(string.Join("\t", new[]
            {
                CatalogField(outputPath),
                CatalogField(match.ModelArchiveName),
                CatalogField(match.ModelObjectName),
                CatalogField(match.ShapeName),
                match.Texture.Width.ToString(CultureInfo.InvariantCulture),
                match.Texture.Height.ToString(CultureInfo.InvariantCulture),
                CatalogField(match.Texture.PixelFormat),
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                "unknown",
                "unknown",
                "rejected-candidate",
                CatalogField(exception.Message),
            }));
            batchPreviewRejected++;
        }
    }

    var batchPreviewManifestPath = Path.Combine(batchPreviewOutputDirectory, "manifest.tsv");
    File.WriteAllLines(
        batchPreviewManifestPath,
        batchPreviewLines,
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    Console.WriteLine($"{GameIdentity.Name} PC EAGL same-trunk material batch candidate preview");
    Console.WriteLine($"verified_material_matches: {batchPreviewMatches.Count}");
    Console.WriteLine($"verified_candidate_previews: {batchPreviewVerified}");
    Console.WriteLine($"verified_object_state_matches: {batchPreviewObjectStateMatches}");
    Console.WriteLine($"verified_render_group_state_matches: {batchPreviewRenderGroupStateMatches}");
    Console.WriteLine($"rejected_candidate_previews: {batchPreviewRejected}");
    Console.WriteLine($"output_directory: {batchPreviewOutputDirectory}");
    Console.WriteLine($"manifest: {batchPreviewManifestPath}");
    Console.WriteLine("status: candidate mesh plus same-trunk texture previews only; UV, skinning, deformation and original GPU draw semantics remain unverified");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software framebuffer");
    return 0;
}

if (string.Equals(args[0], "eagl-material-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-material-preview <caminho-do-iso> <nested.big> <SHAPENAME> <saida.ppm> [objeto.ord]");
        return 2;
    }

    using var materialPreviewStore = new GameAssetStore(args[1]);
    var materialPreviewAsset = args.Length >= 6
        ? EaglShapeMaterialLoader.LoadForObject(
            materialPreviewStore,
            "data/models.big",
            args[2],
            args[5],
            args[3])
        : EaglShapeMaterialLoader.LoadFirstForShape(
            materialPreviewStore,
            "data/models.big",
            args[2],
            args[3]);
    var materialPreviewMesh = EaglCandidateAssetLoader.Load(
        materialPreviewStore,
        "data/models.big",
        materialPreviewAsset.ModelArchiveName,
        materialPreviewAsset.ModelObjectName);
    if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
            materialPreviewMesh.Primitives,
            materialPreviewAsset.ShapeName))
    {
        throw new InvalidDataException(
            $"Candidate object has no exact SHAPENAME state for '{materialPreviewAsset.ShapeName}'.");
    }

    var materialPreviewRenderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
        materialPreviewMesh.Mesh,
        materialPreviewAsset.ShapeName);
    var materialPreviewFrame = new SoftwareFrameBuffer(960, 540);
    materialPreviewFrame.Clear(new RgbColor(13, 18, 27));
    var texturedGroups = EaglCandidateMeshRenderer.DrawSlot1TextureCandidate(
        materialPreviewFrame,
        materialPreviewMesh.Mesh,
        materialPreviewAsset.Texture,
        480,
        270,
        390);
    EaglCandidateMeshRenderer.DrawWireframe(
        materialPreviewFrame,
        materialPreviewMesh.Mesh,
        480,
        270,
        390,
        new RgbColor(235, 235, 235));
    materialPreviewFrame.SavePpm(args[4]);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL material candidate preview");
    Console.WriteLine($"output: {Path.GetFullPath(args[4])}");
    Console.WriteLine($"model_archive: {materialPreviewAsset.ModelArchiveName}");
    Console.WriteLine($"model_object: {materialPreviewAsset.ModelObjectName}");
    Console.WriteLine($"shapename: {materialPreviewAsset.ShapeName}");
    Console.WriteLine($"texture: {materialPreviewAsset.Texture.Width}x{materialPreviewAsset.Texture.Height} | format={materialPreviewAsset.Texture.PixelFormat}");
    Console.WriteLine($"mesh: groups={materialPreviewMesh.Mesh.Groups.Count} | vertices={materialPreviewMesh.Mesh.VertexCount} | triangles={materialPreviewMesh.Mesh.TriangleCount}");
    Console.WriteLine("object_state: exact-shapename");
    Console.WriteLine($"render_group_state: {(materialPreviewRenderGroupState ? "exact-shapename" : "unproven")}");
    Console.WriteLine($"textured_groups: {texturedGroups}");
    Console.WriteLine($"frame_checksum: {materialPreviewFrame.ComputeChecksum():X16}");
    Console.WriteLine("status: candidate slot-1 texture sampling plus wireframe; UV, skinning, deformation and original GPU draw semantics remain unverified");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software framebuffer");
    return 0;
}

if (string.Equals(args[0], "eagl-shapename-index", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-shapename-index <caminho-do-iso> <arquivo.big>");
        return 2;
    }

    using var shapeIndexStore = new GameAssetStore(args[1]);
    using var shapeIndexArchiveStream = shapeIndexStore.OpenRaw(args[2]);
    var shapeIndexArchive = BigArchiveReader.Inspect(shapeIndexArchiveStream);
    var shapeEntries = shapeIndexArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".xsh", StringComparison.OrdinalIgnoreCase))
        .ToArray();
    var shapeBaseNames = shapeEntries
        .Select(entry => Path.GetFileNameWithoutExtension(entry.Name))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var xshFilenamePrefixes = shapeEntries
        .Select(entry => Path.GetFileNameWithoutExtension(entry.Name))
        .GroupBy(name => name.Length >= 4 ? name[..4] : name, StringComparer.OrdinalIgnoreCase)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
        .Take(64)
        .Select(group => $"{group.Key}={group.Count()}")
        .ToArray();
    var shapeNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var prefixShapeNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var nestedArchiveCount = 0;
    var splitObjectCount = 0;
    var validSplitObjectCount = 0;
    var primitiveCount = 0;
    var shapeNameOccurrenceCount = 0;
    var exactFullMatchCount = 0;
    var exactPrefixMatchCount = 0;
    var exactFullMatchedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var exactPrefixMatchedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var decodedShapePackageCount = 0;
    var rejectedShapePackageCount = 0;
    var compressedShapePackageCount = 0;
    var internalShapeEntryCount = 0L;
    var internalShapeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var internalShapeNameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    var modelShapeNames = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
    var internalShapeNamesByPackage = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

    foreach (var nestedBigEntry in shapeIndexArchive.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
    {
        nestedArchiveCount++;
        if (nestedBigEntry.Size > 64 * 1024 * 1024)
        {
            continue;
        }

        using var nestedShapeStream = shapeIndexStore.OpenBigEntry(args[2], nestedBigEntry.Name);
        var compressedShapeData = new byte[checked((int)nestedShapeStream.Length)];
        nestedShapeStream.ReadExactly(compressedShapeData);
        var expandedShapeSize = RefPackDecoder.ReadDeclaredSize(compressedShapeData);
        if (expandedShapeSize > 64 * 1024 * 1024)
        {
            continue;
        }

        using var compressedShapeMemory = new MemoryStream(compressedShapeData, writable: false);
        var expandedShape = RefPackDecoder.Decode(compressedShapeMemory, expandedShapeSize);
        var nestedShapeArchive = BigArchiveReader.Inspect(expandedShape.Data);
        foreach (var nestedEntry in nestedShapeArchive.Entries.Where(entry =>
                     string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase)))
        {
            splitObjectCount++;
            var companionName = Path.ChangeExtension(nestedEntry.Name, ".orl");
            var companionEntry = nestedShapeArchive.Entries.FirstOrDefault(entry =>
                string.Equals(entry.Name, companionName, StringComparison.OrdinalIgnoreCase));
            if (companionEntry is null)
            {
                continue;
            }

            try
            {
                var objectData = BigArchiveReader.ReadEntry(expandedShape.Data, nestedEntry.Name);
                var companionData = BigArchiveReader.ReadEntry(expandedShape.Data, companionEntry.Name);
                var inspection = EaglObjectReader.Inspect(objectData, companionData);
                validSplitObjectCount++;
                var primitives = EaglModelReader.Inspect(objectData, inspection.Elf, inspection.Bounds);
                primitiveCount += primitives.Count;
                foreach (var assignment in primitives.SelectMany(primitive => primitive.StateAssignments)
                             .Where(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal)))
                {
                    shapeNameOccurrenceCount++;
                    shapeNameCounts[assignment.Value] = shapeNameCounts.GetValueOrDefault(assignment.Value) + 1;
                    var prefix = assignment.Value.Split(',', 2)[0].Trim();
                    prefixShapeNameCounts[prefix] = prefixShapeNameCounts.GetValueOrDefault(prefix) + 1;
                    var modelBaseName = Path.GetFileNameWithoutExtension(nestedBigEntry.Name);
                    if (!modelShapeNames.TryGetValue(modelBaseName, out var modelNames))
                    {
                        modelNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        modelShapeNames.Add(modelBaseName, modelNames);
                    }
                    modelNames.Add(assignment.Value);
                    if (shapeBaseNames.Contains(assignment.Value) && exactFullMatchedValues.Add(assignment.Value))
                    {
                        exactFullMatchCount++;
                    }
                    if (shapeBaseNames.Contains(prefix) && exactPrefixMatchedValues.Add(prefix))
                    {
                        exactPrefixMatchCount++;
                    }
                }
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
            {
                // Keep this index diagnostic bounded; the main inventory reports invalid objects separately.
            }
        }
    }

    foreach (var shapeEntry in shapeEntries)
    {
        try
        {
            using var shapeStream = shapeIndexStore.OpenBigEntry(args[2], shapeEntry.Name);
            if (shapeStream.Length > 64L * 1024 * 1024)
            {
                rejectedShapePackageCount++;
                continue;
            }

            var compressedShape = new byte[checked((int)shapeStream.Length)];
            shapeStream.ReadExactly(compressedShape);
            byte[] shapePayload;
            if (compressedShape.Length >= 4 &&
                compressedShape[0] == 0x53 && compressedShape[1] == 0x48 &&
                compressedShape[2] == 0x50 && compressedShape[3] == 0x58)
            {
                shapePayload = compressedShape;
            }
            else
            {
                compressedShapePackageCount++;
                var expandedShapeSize = RefPackDecoder.ReadDeclaredSize(compressedShape);
                if (expandedShapeSize > 64 * 1024 * 1024)
                {
                    rejectedShapePackageCount++;
                    continue;
                }

                using var compressedShapeMemory = new MemoryStream(compressedShape, writable: false);
                shapePayload = RefPackDecoder.Decode(compressedShapeMemory, expandedShapeSize).Data;
            }

            var shapePackage = ShapeImagePackageLoader.Load(shapePayload);
            decodedShapePackageCount++;
            internalShapeEntryCount += shapePackage.Entries.Count;
            foreach (var internalEntry in shapePackage.Entries)
            {
                internalShapeNames.Add(internalEntry.Name);
                internalShapeNameCounts[internalEntry.Name] = internalShapeNameCounts.GetValueOrDefault(internalEntry.Name) + 1;
            }
            internalShapeNamesByPackage[Path.GetFileNameWithoutExtension(shapeEntry.Name)] =
                shapePackage.Entries.Select(entry => entry.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
        catch (Exception exception) when (exception is InvalidDataException or OverflowException or ArgumentOutOfRangeException)
        {
            rejectedShapePackageCount++;
        }
    }

    var internalExactMatches = shapeNameCounts.Keys.Count(value => internalShapeNames.Contains(value));
    var internalPrefixMatches = prefixShapeNameCounts.Keys.Count(value => internalShapeNames.Contains(value));

    Console.WriteLine($"{GameIdentity.Name} PC EAGL SHAPENAME index");
    Console.WriteLine($"archive: {args[2]} | magic={shapeIndexArchive.Magic} | entries={shapeIndexArchive.FileCount}");
    Console.WriteLine($"top_level_xsh_entries: {shapeEntries.Length}");
    Console.WriteLine($"distinct_xsh_base_names: {shapeBaseNames.Count}");
    Console.WriteLine($"xsh_filename_prefixes: {string.Join(",", xshFilenamePrefixes)}");
    Console.WriteLine($"compressed_xsh_packages: {compressedShapePackageCount}");
    Console.WriteLine($"decoded_xsh_packages: {decodedShapePackageCount}");
    Console.WriteLine($"rejected_xsh_packages: {rejectedShapePackageCount}");
    Console.WriteLine($"internal_shpx_entries: {internalShapeEntryCount}");
    Console.WriteLine($"distinct_internal_shpx_names: {internalShapeNames.Count}");
    Console.WriteLine($"nested_big_archives: {nestedArchiveCount}");
    Console.WriteLine($"split_ord_entries: {splitObjectCount}");
    Console.WriteLine($"valid_ord_orl_pairs: {validSplitObjectCount}");
    Console.WriteLine($"render_primitives: {primitiveCount}");
    Console.WriteLine($"shapename_occurrences: {shapeNameOccurrenceCount}");
    Console.WriteLine($"distinct_shapename_values: {shapeNameCounts.Count}");
    Console.WriteLine($"distinct_shapename_values_exact_xsh_base_match: {exactFullMatchCount}");
    Console.WriteLine($"distinct_shapename_prefix_exact_xsh_base_match: {exactPrefixMatchCount}");
    Console.WriteLine($"distinct_shapename_values_exact_internal_shpx_match: {internalExactMatches}");
    Console.WriteLine($"distinct_shapename_prefix_exact_internal_shpx_match: {internalPrefixMatches}");
    Console.WriteLine($"xsh_base_names_starting_with_shapename_prefix: {string.Join(",", prefixShapeNameCounts
        .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        .Select(pair => $"{pair.Key}={shapeBaseNames.Count(name => name.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))}"))}");
    var xshNamesByShapePrefix = string.Join(" | ", prefixShapeNameCounts
        .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
        .Select(pair =>
        {
            var names = shapeEntries
                .Select(entry => Path.GetFileNameWithoutExtension(entry.Name))
                .Where(name => name.StartsWith(pair.Key, StringComparison.OrdinalIgnoreCase))
                .Take(16);
            return $"{pair.Key}=[{string.Join(",", names)}]";
        }));
    Console.WriteLine($"xsh_names_by_shapename_prefix: {xshNamesByShapePrefix}");
    Console.WriteLine($"shapename_values: {string.Join(",", shapeNameCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Take(32).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"shapename_prefix_values: {string.Join(",", prefixShapeNameCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Take(32).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"internal_shpx_name_sample: {string.Join(",", internalShapeNameCounts.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Take(32).Select(pair => $"{pair.Key}={pair.Value}"))}");
    foreach (var model in modelShapeNames
                 .Where(pair => internalShapeNamesByPackage.ContainsKey(pair.Key))
                 .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                 .Take(24))
    {
        Console.WriteLine($"model_xsh_state_sample: {model.Key} | states={string.Join(",", model.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))} | internal={string.Join(",", internalShapeNamesByPackage[model.Key])}");
    }
    foreach (var model in modelShapeNames
                 .Where(pair => pair.Value.Any(value => value.StartsWith("tops,", StringComparison.OrdinalIgnoreCase)) &&
                                internalShapeNamesByPackage.ContainsKey(pair.Key))
                 .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                 .Take(24))
    {
        Console.WriteLine($"model_xsh_tops_sample: {model.Key} | states={string.Join(",", model.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase))} | internal={string.Join(",", internalShapeNamesByPackage[model.Key])}");
    }
    Console.WriteLine($"xsh_sample_names: {string.Join(",", shapeEntries.Take(24).Select(entry => entry.Name))}");
    Console.WriteLine("note: exact text matches are only a name-index correlation; no SHAPENAME value is bound to a texture or material.");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, split ELF/EAGL state inspection");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-projection-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-projection-preview <caminho-do-iso> <nested.big> <SHAPENAME> <diretório-de-saída> [objeto.ord]");
        return 2;
    }

    var projectionOutputDirectory = Path.GetFullPath(args[4]);
    if (string.Equals(
            projectionOutputDirectory,
            Path.GetFullPath(args[1]),
            StringComparison.OrdinalIgnoreCase) ||
        File.Exists(projectionOutputDirectory))
    {
        Console.Error.WriteLine("O diretório de projeções não pode ser a ISO ou um arquivo existente.");
        return 2;
    }

    Directory.CreateDirectory(projectionOutputDirectory);
    using var projectionStore = new GameAssetStore(args[1]);
    var projectionAsset = args.Length >= 6
        ? EaglShapeMaterialLoader.LoadForObject(
            projectionStore,
            "data/models.big",
            args[2],
            args[5],
            args[3])
        : EaglShapeMaterialLoader.LoadFirstForShape(
            projectionStore,
            "data/models.big",
            args[2],
            args[3]);
    var projectionMesh = EaglCandidateAssetLoader.Load(
        projectionStore,
        "data/models.big",
        projectionAsset.ModelArchiveName,
        projectionAsset.ModelObjectName);
    if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
            projectionMesh.Primitives,
            projectionAsset.ShapeName))
    {
        throw new InvalidDataException(
            $"Candidate object has no exact SHAPENAME state for '{projectionAsset.ShapeName}'.");
    }

    var projectionRenderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
        projectionMesh.Mesh,
        projectionAsset.ShapeName);
    var projectionBaseName = string.Join("-", new[]
    {
        SafeFilePart(Path.GetFileNameWithoutExtension(projectionAsset.ModelArchiveName)),
        SafeFilePart(Path.GetFileNameWithoutExtension(projectionAsset.ModelObjectName)),
        SafeFilePart(projectionAsset.ShapeName),
    });
    var projectionOutputs = new List<(EaglCandidateProjectionPlane Plane, string Path, ulong Checksum)>();
    foreach (var plane in Enum.GetValues<EaglCandidateProjectionPlane>())
    {
        var outputPath = Path.Combine(
            projectionOutputDirectory,
            $"{projectionBaseName}-{plane.ToString().ToLowerInvariant()}.ppm");
        var frame = new SoftwareFrameBuffer(960, 540);
        frame.Clear(new RgbColor(13, 18, 27));
        EaglCandidateMeshRenderer.DrawSolidProjection(
            frame,
            projectionMesh.Mesh,
            480,
            270,
            390,
            new RgbColor(166, 42, 48),
            plane);
        EaglCandidateMeshRenderer.DrawWireframeProjection(
            frame,
            projectionMesh.Mesh,
            480,
            270,
            390,
            new RgbColor(235, 235, 235),
            plane);
        frame.SavePpm(outputPath);
        projectionOutputs.Add((plane, outputPath, frame.ComputeChecksum()));
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate Cartesian projection preview");
    Console.WriteLine($"model_archive: {projectionAsset.ModelArchiveName}");
    Console.WriteLine($"model_object: {projectionAsset.ModelObjectName}");
    Console.WriteLine($"shapename: {projectionAsset.ShapeName}");
    Console.WriteLine($"mesh: groups={projectionMesh.Mesh.Groups.Count} | vertices={projectionMesh.Mesh.VertexCount} | triangles={projectionMesh.Mesh.TriangleCount}");
    Console.WriteLine("object_state: exact-shapename");
    Console.WriteLine($"render_group_state: {(projectionRenderGroupState ? "exact-shapename" : "unproven")}");
    foreach (var output in projectionOutputs)
    {
        Console.WriteLine($"projection_{output.Plane.ToString().ToLowerInvariant()}: {output.Path} | checksum={output.Checksum:X16}");
    }
    Console.WriteLine("status: Cartesian projection comparison of the candidate mesh only; no axis, camera, vertex, UV, material, skinning, rigging, animation or GPU command semantics assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, managed software framebuffer");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-preview <caminho-do-iso> <saida.ppm>");
        return 2;
    }

    using var candidatePreviewStore = new GameAssetStore(args[1]);
    var candidatePreviewAsset = EaglCandidateAssetLoader.Load(
        candidatePreviewStore,
        "data/models.big",
        "star0001.big",
        "star0001-head.ord");
    using var candidateTitleStream = candidatePreviewStore.OpenRaw("data/titleimage.xbx");
    using var candidateTitleMemory = new MemoryStream();
    candidateTitleStream.CopyTo(candidateTitleMemory);
    var candidateTitle = TextureAssetLoader.Load(candidateTitleMemory.ToArray());
    var candidateFrame = new SoftwareFrameBuffer(960, 540);
    PreviewRenderer.Render(
        new StreetMatch(),
        candidateFrame,
        candidateTitle,
        candidatePreviewAsset.Mesh,
        slot1AttributeDiagnostic: args.Any(argument =>
            string.Equals(argument, "--slot1-profile", StringComparison.OrdinalIgnoreCase)));
    candidateFrame.SavePpm(args[2]);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate render preview");
    Console.WriteLine($"output: {Path.GetFullPath(args[2])}");
    Console.WriteLine($"mesh: groups={candidatePreviewAsset.Mesh.Groups.Count} | vertices={candidatePreviewAsset.Mesh.VertexCount} | triangles={candidatePreviewAsset.Mesh.TriangleCount}");
    Console.WriteLine($"status: candidate {(args.Any(argument => string.Equals(argument, "--slot1-profile", StringComparison.OrdinalIgnoreCase)) ? "slot1 attribute profile" : "solid fill")} plus wireframe over managed gameplay scene; material/skin/deformation remain unverified");
    Console.WriteLine("mode: ISO read-only, managed software framebuffer");
    return 0;
}

if (string.Equals(args[0], "eagl-candidate-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-candidate-smoke <caminho-do-iso> [--commands]");
        return 2;
    }

    using var candidateStore = new GameAssetStore(args[1]);
    var candidateAsset = EaglCandidateAssetLoader.Load(
        candidateStore,
        "data/models.big",
        "star0001.big",
        "star0001-head.ord");
    Console.WriteLine($"{GameIdentity.Name} PC EAGL candidate mesh smoke");
    Console.WriteLine($"object: {candidateAsset.ObjectName}");
    Console.WriteLine($"groups: {candidateAsset.Mesh.Groups.Count}");
    Console.WriteLine($"vertices: {candidateAsset.Mesh.VertexCount}");
    Console.WriteLine($"triangles: {candidateAsset.Mesh.TriangleCount}");
    Console.WriteLine($"groups_with_complete_slot1_attribute_profile: {candidateAsset.Mesh.Groups.Count(group => group.Slot1Attributes.Count == group.Vertices.Count)}");
    Console.WriteLine($"slot1_attribute_values: {candidateAsset.Mesh.Groups.Sum(group => group.Slot1Attributes.Count)}");
    Console.WriteLine($"bound_float_profiles: {candidateAsset.Mesh.Groups.Sum(group => group.BoundFloatProfiles.Count)}");
    Console.WriteLine($"bound_float_profile_slots: {string.Join(",", candidateAsset.Mesh.Groups
        .SelectMany(group => group.BoundFloatProfiles)
        .GroupBy(profile => profile.Slot)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}"))}");
    Console.WriteLine($"groups_with_preserved_state: {candidateAsset.Mesh.Groups.Count(group => group.StateAssignments.Count > 0)}");
    Console.WriteLine($"candidate_state_names: {string.Join(",", candidateAsset.Mesh.Groups
        .SelectMany(group => group.StateAssignments)
        .Where(assignment => string.Equals(assignment.Name, "SHAPENAME", StringComparison.Ordinal))
        .Select(assignment => assignment.Value)
        .Distinct(StringComparer.Ordinal)
        .OrderBy(value => value, StringComparer.Ordinal))}");
    Console.WriteLine($"candidate_groups_with_complete_command_ids: {candidateAsset.Mesh.Groups.Count(group => group.RenderCommandIds.Count > 0)}");
    Console.WriteLine($"skipped_primitive_groups: {candidateAsset.Mesh.SkippedPrimitiveGroups}");
    Console.WriteLine($"render_primitives: {candidateAsset.Primitives.Count}");
    Console.WriteLine($"complete_command_streams: {candidateAsset.Primitives.Count(primitive => primitive.RenderCommandStream.IsComplete)}");
    Console.WriteLine($"incomplete_command_streams: {candidateAsset.Primitives.Count(primitive => !primitive.RenderCommandStream.IsComplete)}");
    Console.WriteLine($"render_command_ids: {string.Join(",", candidateAsset.Primitives
        .SelectMany(primitive => primitive.RenderCommandStream.Commands)
        .GroupBy(command => command.CommandId)
        .OrderBy(group => group.Key)
        .Select(group => $"{group.Key}={group.Count()}"))}");
    if (args.Any(argument => string.Equals(argument, "--commands", StringComparison.OrdinalIgnoreCase)))
    {
        foreach (var primitive in candidateAsset.Primitives)
        {
            Console.WriteLine($"eagl_commands: layer={primitive.LayerIndex} primitive={primitive.PrimitiveIndex} state={primitive.PrimitiveStateSymbol ?? "none"}");
            foreach (var command in primitive.RenderCommandStream.Commands)
            {
                Console.WriteLine($"eagl_command: primitive={primitive.PrimitiveIndex} index={command.Index} offset=0x{command.SectionOffset:X} id={command.CommandId} words={command.SizeWords} raw={Convert.ToHexString(command.RawBytes)} relocations={string.Join(",", command.RelocationSymbols)}");
            }
        }
    }
    Console.WriteLine("status: candidate geometry only; draw binding/materials/animation remain unverified");
    Console.WriteLine("mode: ISO read-only, RefPack decode, nested BIG, ELF/EAGL managed mesh");
    return 0;
}

if (string.Equals(args[0], "eagl-object-symbol-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-object-symbol-smoke <caminho-do-iso> <arquivo.big> <nested.big> <entrada> [--payload|--raw]");
        return 2;
    }

    using var symbolStore = new GameAssetStore(args[1]);
    using var compressedSymbolStream = symbolStore.OpenBigEntry(args[2], args[3]);
    if (compressedSymbolStream.Length > 64L * 1024 * 1024)
    {
        throw new InvalidDataException("Compressed nested model archive exceeds the 64 MiB safety limit.");
    }

    var compressedSymbolData = new byte[checked((int)compressedSymbolStream.Length)];
    compressedSymbolStream.ReadExactly(compressedSymbolData);
    var symbolExpandedSize = RefPackDecoder.ReadDeclaredSize(compressedSymbolData);
    if (symbolExpandedSize > 64 * 1024 * 1024)
    {
        throw new InvalidDataException("Expanded nested model archive exceeds the 64 MiB safety limit.");
    }
    using var compressedSymbolMemory = new MemoryStream(compressedSymbolData, writable: false);
    var symbolExpanded = RefPackDecoder.Decode(compressedSymbolMemory, symbolExpandedSize);
    var symbolArchive = BigArchiveReader.Inspect(symbolExpanded.Data);
    var symbolEntry = symbolArchive.Entries.FirstOrDefault(entry =>
        string.Equals(entry.Name, args[4], StringComparison.OrdinalIgnoreCase));
    if (symbolEntry is null)
    {
        throw new InvalidDataException($"EAGL entry was not found in the nested archive: {args[4]}");
    }

    var symbolData = BigArchiveReader.ReadEntry(symbolExpanded.Data, symbolEntry.Name);
    if (args.Any(argument => string.Equals(argument, "--raw", StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"{GameIdentity.Name} PC EAGL nested-entry raw smoke");
        Console.WriteLine($"entry: {args[2]}/{symbolEntry.Name}");
        Console.WriteLine($"bytes: {symbolData.Length}");
        Console.WriteLine($"first_bytes: {Convert.ToHexString(symbolData.AsSpan(0, Math.Min(512, symbolData.Length)))}");
        try
        {
            var rawInspection = EaglObjectReader.Inspect(symbolData);
            Console.WriteLine($"format: ELF | machine={rawInspection.Elf.Machine} | sections={rawInspection.Elf.Sections.Count} | symbols={rawInspection.Elf.Symbols.Count} | relocations={rawInspection.Elf.Relocations.Count}");
        }
        catch (InvalidDataException exception)
        {
            Console.WriteLine($"format: unsupported-by-ELF-reader | issue={exception.Message}");
        }
        Console.WriteLine("status: raw nested-entry evidence only; no link-record semantics were assigned");
        Console.WriteLine("mode: ISO read-only, RefPack decode, nested BIG");
        return 0;
    }
    var symbolInspection = EaglObjectReader.Inspect(symbolData);
    Console.WriteLine($"{GameIdentity.Name} PC EAGL object symbol smoke");
    Console.WriteLine($"entry: {args[2]}/{symbolEntry.Name}");
    Console.WriteLine($"bytes: {symbolData.Length}");
    Console.WriteLine($"elf_machine: {symbolInspection.Elf.Machine}");
    Console.WriteLine($"sections: {symbolInspection.Elf.Sections.Count}");
    Console.WriteLine($"symbols: {symbolInspection.Elf.Symbols.Count}");
    Console.WriteLine($"relocations: {symbolInspection.Elf.Relocations.Count}");
    Console.WriteLine($"geometry_symbols: {symbolInspection.GeometrySymbols.Count}");
    Console.WriteLine($"bounds: {(symbolInspection.Bounds is null ? "none" : "present")}");
    foreach (var section in symbolInspection.Elf.Sections.Where(section => !string.IsNullOrEmpty(section.Name)))
    {
        Console.WriteLine($"section: {section.Name} | type={section.Type} | offset=0x{section.Offset:X} | size=0x{section.Size:X}");
    }
    foreach (var symbol in symbolInspection.Elf.Symbols.Where(symbol => !string.IsNullOrEmpty(symbol.Name)))
    {
        Console.WriteLine($"symbol: {symbol.Name} | value=0x{symbol.Value:X} | size={symbol.Size} | section={symbol.SectionIndex}");
    }
    var morphInspection = EaglMorphReader.Inspect(symbolData, symbolInspection.Elf);
    Console.WriteLine($"morph_symbol: {morphInspection.MorphSymbol ?? "none"}");
    Console.WriteLine($"morph_named_strings: {string.Join(",", morphInspection.NamedStrings.Select(item => $"0x{item.Offset:X}:{item.Value}"))}");
    Console.WriteLine($"morph_shape_names: {string.Join(",", morphInspection.ShapeNames)}");
    Console.WriteLine($"morph_attribute_names: {string.Join(",", morphInspection.AttributeNames)}");
    Console.WriteLine($"morph_shape_pointers: {string.Join(",", morphInspection.ShapePointers.Select(pointer => $"0x{pointer.FieldOffset:X}:{pointer.ShapeName}"))}");
    Console.WriteLine($"morph_attribute_pointers: {string.Join(",", morphInspection.AttributePointers.Select(pointer => $"0x{pointer.FieldOffset:X}:{pointer.AttributeName}"))}");
    Console.WriteLine($"morph_internal_pointers: {string.Join(",", morphInspection.InternalPointers.Select(pointer => $"0x{pointer.FieldOffset:X}->0x{pointer.TargetOffset:X}:{pointer.TargetString ?? "unresolved"}"))}");
    if (args.Any(argument => string.Equals(argument, "--payload", StringComparison.OrdinalIgnoreCase)))
    {
        var windows = EaglMorphReader.InspectPayloadWindows(symbolData, symbolInspection.Elf);
        Console.WriteLine($"morph_payload_windows: {windows.Count}");
        foreach (var window in windows)
        {
            Console.WriteLine($"morph_payload_window: start=0x{window.StartOffset:X} | bytes={window.Bytes.Length} | relocations={string.Join(",", window.RelocationFieldOffsets.Select(offset => $"0x{offset:X}"))} | raw={Convert.ToHexString(window.Bytes)}");
        }
    }
    Console.WriteLine("status: symbol/section and bounded payload evidence only; morph records are not decoded");
    Console.WriteLine("mode: ISO read-only, RefPack decode, nested BIG, managed ELF inspection");
    return 0;
}

if (string.Equals(args[0], "eagl-morph-sibling-inventory", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-morph-sibling-inventory <caminho-do-iso> <arquivo.big> [--catalog <saida.tsv>] [--ambiguity-report <saida.tsv>] [--byte-report <saida.tsv>] [--numeric-gap-report <saida.tsv>] [--numeric-gap-correlation-report <saida.tsv>] [--geometry-report <saida.tsv>] [--geometry-summary <saida.tsv>] [--geometry-correlation-report <saida.tsv>] [--geometry-correlation-summary <saida.tsv>]");
        return 2;
    }

    string? morphCatalogPath = null;
    string? morphAmbiguityReportPath = null;
    var morphCatalogOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--catalog", StringComparison.OrdinalIgnoreCase));
    if (morphCatalogOptionIndex >= 0)
    {
        if (morphCatalogOptionIndex + 1 >= args.Length ||
            args[morphCatalogOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-sibling-inventory ... --catalog <saida.tsv>");
            return 2;
        }

        morphCatalogPath = Path.GetFullPath(args[morphCatalogOptionIndex + 1]);
        if (string.Equals(
                morphCatalogPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O catálogo não pode sobrescrever a ISO de entrada.");
            return 2;
        }
    }
    var morphAmbiguityReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--ambiguity-report", StringComparison.OrdinalIgnoreCase));
    if (morphAmbiguityReportOptionIndex >= 0)
    {
        if (morphAmbiguityReportOptionIndex + 1 >= args.Length ||
            args[morphAmbiguityReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-sibling-inventory ... --ambiguity-report <saida.tsv>");
            return 2;
        }

        morphAmbiguityReportPath = Path.GetFullPath(args[morphAmbiguityReportOptionIndex + 1]);
        if (string.Equals(
                morphAmbiguityReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphAmbiguityReportPath,
                morphCatalogPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de ambiguidade não pode sobrescrever a ISO ou o catálogo.");
            return 2;
        }
    }
    string? morphByteComparisonReportPath = null;
    string? morphNumericGapReportPath = null;
    string? morphNumericGapCorrelationReportPath = null;
    string? morphGeometryReportPath = null;
    string? morphGeometrySummaryPath = null;
    string? morphGeometryCorrelationReportPath = null;
    string? morphGeometryCorrelationSummaryPath = null;
    var morphByteComparisonReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--byte-report", StringComparison.OrdinalIgnoreCase));
    if (morphByteComparisonReportOptionIndex >= 0)
    {
        if (morphByteComparisonReportOptionIndex + 1 >= args.Length ||
            args[morphByteComparisonReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-sibling-inventory ... --byte-report <saida.tsv>");
            return 2;
        }

        morphByteComparisonReportPath = Path.GetFullPath(args[morphByteComparisonReportOptionIndex + 1]);
        if (string.Equals(
                morphByteComparisonReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphByteComparisonReportPath,
                morphCatalogPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphByteComparisonReportPath,
                morphAmbiguityReportPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório byte-level não pode sobrescrever a ISO, o catálogo ou o relatório de ambiguidade.");
            return 2;
        }
    }
    var morphNumericGapReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--numeric-gap-report", StringComparison.OrdinalIgnoreCase));
    if (morphNumericGapReportOptionIndex >= 0)
    {
        if (morphNumericGapReportOptionIndex + 1 >= args.Length ||
            args[morphNumericGapReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-sibling-inventory ... --numeric-gap-report <saida.tsv>");
            return 2;
        }

        morphNumericGapReportPath = Path.GetFullPath(args[morphNumericGapReportOptionIndex + 1]);
        if (string.Equals(
                morphNumericGapReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapReportPath,
                morphCatalogPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapReportPath,
                morphAmbiguityReportPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapReportPath,
                morphByteComparisonReportPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de lacunas numéricas não pode sobrescrever a ISO ou outro relatório.");
            return 2;
        }
    }
    var morphNumericGapCorrelationReportOptionIndex = Array.FindIndex(args, argument =>
        string.Equals(argument, "--numeric-gap-correlation-report", StringComparison.OrdinalIgnoreCase));
    if (morphNumericGapCorrelationReportOptionIndex >= 0)
    {
        if (morphNumericGapCorrelationReportOptionIndex + 1 >= args.Length ||
            args[morphNumericGapCorrelationReportOptionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: eagl-morph-sibling-inventory ... --numeric-gap-correlation-report <saida.tsv>");
            return 2;
        }

        morphNumericGapCorrelationReportPath = Path.GetFullPath(args[morphNumericGapCorrelationReportOptionIndex + 1]);
        if (string.Equals(
                morphNumericGapCorrelationReportPath,
                Path.GetFullPath(args[1]),
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapCorrelationReportPath,
                morphCatalogPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapCorrelationReportPath,
                morphAmbiguityReportPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapCorrelationReportPath,
                morphByteComparisonReportPath,
                StringComparison.OrdinalIgnoreCase) ||
            string.Equals(
                morphNumericGapCorrelationReportPath,
                morphNumericGapReportPath,
                StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine("O relatório de correlação numérica não pode sobrescrever a ISO ou outro relatório.");
            return 2;
        }
    }
    foreach (var (option, label, assign) in new[]
             {
                 ("--geometry-report", "relatório geométrico", 0),
                 ("--geometry-summary", "resumo geométrico", 1),
                 ("--geometry-correlation-report", "relatório de correlação geométrica", 2),
                 ("--geometry-correlation-summary", "resumo de correlação geométrica", 3),
             })
    {
        var optionIndex = Array.FindIndex(args, argument =>
            string.Equals(argument, option, StringComparison.OrdinalIgnoreCase));
        if (optionIndex < 0)
        {
            continue;
        }

        if (optionIndex + 1 >= args.Length ||
            args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"Uso: eagl-morph-sibling-inventory ... {option} <saida.tsv>");
            return 2;
        }

        var outputPath = Path.GetFullPath(args[optionIndex + 1]);
        var inputPaths = new[]
        {
            Path.GetFullPath(args[1]),
            morphCatalogPath,
            morphAmbiguityReportPath,
            morphByteComparisonReportPath,
            morphNumericGapReportPath,
            morphNumericGapCorrelationReportPath,
            morphGeometryReportPath,
            morphGeometrySummaryPath,
            morphGeometryCorrelationReportPath,
            morphGeometryCorrelationSummaryPath,
        };
        if (inputPaths.Any(path => path is not null &&
                                   string.Equals(path, outputPath, StringComparison.OrdinalIgnoreCase)))
        {
            Console.Error.WriteLine($"O {label} não pode sobrescrever a ISO ou outro relatório.");
            return 2;
        }

        if (assign == 0)
        {
            morphGeometryReportPath = outputPath;
        }
        else if (assign == 1)
        {
            morphGeometrySummaryPath = outputPath;
        }
        else if (assign == 2)
        {
            morphGeometryCorrelationReportPath = outputPath;
        }
        else
        {
            morphGeometryCorrelationSummaryPath = outputPath;
        }
    }

    using var morphInventoryStore = new GameAssetStore(args[1]);
    using var morphInventoryTopLevelStream = morphInventoryStore.OpenRaw(args[2]);
    var morphInventoryTopLevel = BigArchiveReader.Inspect(morphInventoryTopLevelStream);
    var allMorphEvidence = new List<(string NestedArchiveName, EaglMorphSiblingEvidence Evidence)>();
    var morphInventoryNestedCount = 0;
    var morphInventoryValidNestedCount = 0;
    var morphInventoryInvalidNestedCount = 0;
    foreach (var nestedEntry in morphInventoryTopLevel.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".big", StringComparison.OrdinalIgnoreCase)))
    {
        morphInventoryNestedCount++;
        try
        {
            using var compressedStream = morphInventoryStore.OpenBigEntry(args[2], nestedEntry.Name);
            if (compressedStream.Length > 64L * 1024 * 1024)
            {
                throw new InvalidDataException("Nested EAGL archive exceeds the 64 MiB safety limit.");
            }

            var compressed = new byte[checked((int)compressedStream.Length)];
            compressedStream.ReadExactly(compressed);
            var declaredSize = RefPackDecoder.ReadDeclaredSize(compressed);
            if (declaredSize > 64 * 1024 * 1024)
            {
                throw new InvalidDataException("Expanded EAGL archive exceeds the 64 MiB safety limit.");
            }

            using var compressedMemory = new MemoryStream(compressed, writable: false);
            var expanded = RefPackDecoder.Decode(compressedMemory, declaredSize);
            morphInventoryValidNestedCount++;
            allMorphEvidence.AddRange(
                EaglMorphSiblingReader.Inspect(expanded.Data, nestedEntry.Name)
                    .Select(evidence => (nestedEntry.Name, evidence)));
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            morphInventoryInvalidNestedCount++;
            Console.WriteLine($"invalid_nested_big: {nestedEntry.Name} | {exception.Message}");
        }
    }

    var signatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.RelocationSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(12)
        .ToArray();
    var relativeSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.RelativeRelocationSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(12)
        .ToArray();
    var normalizedPayloadHashCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.NormalizedPayloadHash, StringComparer.Ordinal)
        .Select(group => (Hash: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Hash, StringComparer.Ordinal)
        .Take(12)
        .ToArray();
    var normalizedPayloadWindowHashSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.NormalizedPayloadWindowHashSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var structuralLayoutSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(12)
        .ToArray();
    var companionStructuralSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingStructuralSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var companionStructuralLayoutSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingStructuralLayoutSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var commandStreamSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCommandStreamSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var commandStreamBoundarySignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCommandStreamBoundarySignature ?? string.Empty, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var vertexDescriptorSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingVertexDescriptorSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var vertexRecordSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingVertexRecordSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var numericDescriptorSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingNumericDescriptorSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var candidateMeshSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCandidateMeshSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var companionNumericMatchSignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCompanionNumericMatchSignature, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var companionNumericMatchBoundarySignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCompanionNumericMatchBoundarySignature ?? string.Empty, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var companionNumericMatchValueCategorySignatureCounts = allMorphEvidence
        .GroupBy(item => item.Evidence.SiblingCompanionNumericMatchValueCategorySignature ?? string.Empty, StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var pairedStructuralProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingStructuralSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCompanionStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingStructuralSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedStructuralLayoutProfiles = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingStructuralLayoutSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCompanionLayoutStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingStructuralLayoutSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCommandStreamProfiles = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCommandStreamSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCommandStreamStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCommandStreamSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCommandStreamBoundaryProfiles = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCommandStreamBoundarySignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCommandStreamBoundaryStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCommandStreamBoundarySignature ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutVertexDescriptorProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingVertexDescriptorSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleVertexDescriptorStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingVertexDescriptorSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutVertexRecordProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingVertexRecordSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleVertexRecordStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingVertexRecordSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutNumericProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingNumericDescriptorSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleNumericStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingNumericDescriptorSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCandidateMeshProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCandidateMeshSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCandidateMeshStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCandidateMeshSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutPayloadWindowProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.NormalizedPayloadWindowHashSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultiplePayloadWindowStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.NormalizedPayloadWindowHashSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCompanionNumericMatchProfileCount = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCompanionNumericMatchSignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCompanionNumericMatchStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCompanionNumericMatchSignature)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCompanionNumericMatchBoundaryProfiles = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCompanionNumericMatchBoundarySignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCompanionNumericMatchBoundaryStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCompanionNumericMatchBoundarySignature ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedLayoutCompanionNumericMatchValueCategoryProfiles = allMorphEvidence
        .Select(item => $"{item.Evidence.StructuralLayoutSignature}||{item.Evidence.SiblingCompanionNumericMatchValueCategorySignature}")
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultipleCompanionNumericMatchValueCategoryStructures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => item.Evidence.SiblingCompanionNumericMatchValueCategorySignature ?? string.Empty)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var pairedStructuralSignatureCounts = allMorphEvidence
        .GroupBy(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence), StringComparer.Ordinal)
        .Select(group => (Signature: group.Key, Count: group.Count()))
        .OrderByDescending(item => item.Count)
        .ThenBy(item => item.Signature, StringComparer.Ordinal)
        .Take(8)
        .ToArray();
    var pairedStructuralSignatureCount = allMorphEvidence
        .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
        .Distinct(StringComparer.Ordinal)
        .Count();
    var layoutProfilesWithMultiplePairedStructuralSignatures = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
    var layoutProfilesWithStablePairedStructuralSignature = allMorphEvidence
        .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
        .Count(group => group
            .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
            .Distinct(StringComparer.Ordinal)
            .Count() == 1);
    var signatureProfiles = allMorphEvidence
        .GroupBy(item => item.Evidence.RelocationSignature, StringComparer.Ordinal)
        .OrderByDescending(group => group.Count())
        .ThenBy(group => group.Key, StringComparer.Ordinal)
        .Take(12)
        .Select(group =>
        {
            var shapeCounts = string.Join(",", group
                .Select(item => item.Evidence.ShapeNameCount)
                .Distinct()
                .OrderBy(value => value));
            var attributeCounts = string.Join(",", group
                .Select(item => item.Evidence.AttributeNameCount)
                .Distinct()
                .OrderBy(value => value));
            var poseSymbolCounts = string.Join(",", group
                .Select(item => item.Evidence.PosePaletteBankSymbolCount)
                .Distinct()
                .OrderBy(value => value));
            return $"{group.Count()}x[{group.Key}] shapes={shapeCounts} attrs={attributeCounts} pose_symbols={poseSymbolCounts} bounds={group.Count(item => item.Evidence.SiblingHasBounds)}";
        })
        .ToArray();
    Console.WriteLine($"{GameIdentity.Name} PC EAGL morph/geometry sibling inventory");
    Console.WriteLine($"archive: {args[2]} | entries={morphInventoryTopLevel.FileCount}");
    Console.WriteLine($"nested_big_candidates: {morphInventoryNestedCount}");
    Console.WriteLine($"valid_nested_big: {morphInventoryValidNestedCount}");
    Console.WriteLine($"invalid_nested_big: {morphInventoryInvalidNestedCount}");
    Console.WriteLine($"morph_sibling_evidence: {allMorphEvidence.Count}");
    Console.WriteLine($"with_normalized_ord_sibling: {allMorphEvidence.Count(item => item.Evidence.OrdSiblingName is not null)}");
    Console.WriteLine($"with_complete_ord_orl_sibling: {allMorphEvidence.Count(item => item.Evidence.HasCompleteSibling)}");
    Console.WriteLine($"valid_sibling_elfs: {allMorphEvidence.Count(item => item.Evidence.SiblingElfValidated)}");
    Console.WriteLine($"sibling_geometry_candidates: {allMorphEvidence.Count(item => item.Evidence.SiblingGeometrySymbolCount > 0)}");
    Console.WriteLine($"sibling_bounds_candidates: {allMorphEvidence.Count(item => item.Evidence.SiblingHasBounds)}");
    Console.WriteLine($"morph_shape_names: {allMorphEvidence.Sum(item => item.Evidence.ShapeNameCount)}");
    Console.WriteLine($"morph_attribute_names: {allMorphEvidence.Sum(item => item.Evidence.AttributeNameCount)}");
    Console.WriteLine($"morph_pose_palette_bank_symbols: {allMorphEvidence.Sum(item => item.Evidence.PosePaletteBankSymbolCount)}");
    Console.WriteLine($"issues: {allMorphEvidence.Count(item => item.Evidence.Issue is not null)}");
    Console.WriteLine($"relocation_signatures: {string.Join(" | ", signatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"relative_relocation_signatures: {string.Join(" | ", relativeSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"normalized_payload_hashes_distinct: {allMorphEvidence.Select(item => item.Evidence.NormalizedPayloadHash).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"normalized_payload_hashes: {string.Join(" | ", normalizedPayloadHashCounts.Select(item => $"{item.Count}x[{item.Hash}]"))}");
    Console.WriteLine($"normalized_payload_window_hash_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.NormalizedPayloadWindowHashSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"normalized_payload_window_hash_signatures: {string.Join(" | ", normalizedPayloadWindowHashSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_payload_window_profiles_distinct: {pairedLayoutPayloadWindowProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_payload_window_structures: {layoutProfilesWithMultiplePayloadWindowStructures}");
    Console.WriteLine($"paired_structural_signatures_distinct: {pairedStructuralSignatureCount}");
    Console.WriteLine($"paired_structural_signatures: {string.Join(" | ", pairedStructuralSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"layout_profiles_with_multiple_paired_structural_signatures: {layoutProfilesWithMultiplePairedStructuralSignatures}");
    Console.WriteLine($"layout_profiles_with_stable_paired_structural_signature: {layoutProfilesWithStablePairedStructuralSignature}");
    Console.WriteLine($"structural_layout_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.StructuralLayoutSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"structural_layout_signatures: {string.Join(" | ", structuralLayoutSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"companion_structural_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingStructuralSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"companion_structural_signatures: {string.Join(" | ", companionStructuralSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_structural_profiles_distinct: {pairedStructuralProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_companion_structures: {layoutProfilesWithMultipleCompanionStructures}");
    Console.WriteLine($"companion_structural_layout_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingStructuralLayoutSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"companion_structural_layout_signatures: {string.Join(" | ", companionStructuralLayoutSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_structural_layout_profiles_distinct: {pairedStructuralLayoutProfiles}");
    Console.WriteLine($"layout_profiles_with_multiple_companion_layout_structures: {layoutProfilesWithMultipleCompanionLayoutStructures}");
    Console.WriteLine($"sibling_command_stream_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCommandStreamSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_command_stream_signatures: {string.Join(" | ", commandStreamSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_command_stream_profiles_distinct: {pairedLayoutCommandStreamProfiles}");
    Console.WriteLine($"layout_profiles_with_multiple_command_stream_structures: {layoutProfilesWithMultipleCommandStreamStructures}");
    Console.WriteLine($"sibling_command_stream_boundary_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCommandStreamBoundarySignature ?? string.Empty).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_command_stream_boundary_signatures: {string.Join(" | ", commandStreamBoundarySignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_command_stream_boundary_profiles_distinct: {pairedLayoutCommandStreamBoundaryProfiles}");
    Console.WriteLine($"layout_profiles_with_multiple_command_stream_boundary_structures: {layoutProfilesWithMultipleCommandStreamBoundaryStructures}");
    Console.WriteLine($"sibling_vertex_descriptor_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingVertexDescriptorSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_vertex_descriptor_signatures: {string.Join(" | ", vertexDescriptorSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_vertex_descriptor_profiles_distinct: {pairedLayoutVertexDescriptorProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_vertex_descriptor_structures: {layoutProfilesWithMultipleVertexDescriptorStructures}");
    Console.WriteLine($"sibling_vertex_record_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingVertexRecordSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_vertex_record_signatures: {string.Join(" | ", vertexRecordSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_vertex_record_profiles_distinct: {pairedLayoutVertexRecordProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_vertex_record_structures: {layoutProfilesWithMultipleVertexRecordStructures}");
    Console.WriteLine($"sibling_numeric_descriptor_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingNumericDescriptorSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_numeric_descriptor_signatures: {string.Join(" | ", numericDescriptorSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_numeric_profiles_distinct: {pairedLayoutNumericProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_numeric_structures: {layoutProfilesWithMultipleNumericStructures}");
    Console.WriteLine($"sibling_candidate_mesh_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCandidateMeshSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_candidate_mesh_signatures_present: {allMorphEvidence.Count(item => item.Evidence.SiblingCandidateMeshSignature.Length > 0)}");
    Console.WriteLine($"sibling_candidate_mesh_signatures: {string.Join(" | ", candidateMeshSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_candidate_mesh_profiles_distinct: {pairedLayoutCandidateMeshProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_candidate_mesh_structures: {layoutProfilesWithMultipleCandidateMeshStructures}");
    Console.WriteLine($"sibling_companion_numeric_match_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCompanionNumericMatchSignature).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_companion_numeric_match_signatures: {string.Join(" | ", companionNumericMatchSignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_companion_numeric_match_profiles_distinct: {pairedLayoutCompanionNumericMatchProfileCount}");
    Console.WriteLine($"layout_profiles_with_multiple_companion_numeric_match_structures: {layoutProfilesWithMultipleCompanionNumericMatchStructures}");
    Console.WriteLine($"sibling_companion_numeric_match_boundary_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCompanionNumericMatchBoundarySignature ?? string.Empty).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_companion_numeric_match_boundary_signatures: {string.Join(" | ", companionNumericMatchBoundarySignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_companion_numeric_match_boundary_profiles_distinct: {pairedLayoutCompanionNumericMatchBoundaryProfiles}");
    Console.WriteLine($"layout_profiles_with_multiple_companion_numeric_match_boundary_structures: {layoutProfilesWithMultipleCompanionNumericMatchBoundaryStructures}");
    Console.WriteLine($"sibling_companion_numeric_match_value_category_signatures_distinct: {allMorphEvidence.Select(item => item.Evidence.SiblingCompanionNumericMatchValueCategorySignature ?? string.Empty).Distinct(StringComparer.Ordinal).Count()}");
    Console.WriteLine($"sibling_companion_numeric_match_value_category_signatures: {string.Join(" | ", companionNumericMatchValueCategorySignatureCounts.Select(item => $"{item.Count}x[{item.Signature}]"))}");
    Console.WriteLine($"paired_layout_companion_numeric_match_value_category_profiles_distinct: {pairedLayoutCompanionNumericMatchValueCategoryProfiles}");
    Console.WriteLine($"layout_profiles_with_multiple_companion_numeric_match_value_category_structures: {layoutProfilesWithMultipleCompanionNumericMatchValueCategoryStructures}");
    if (morphCatalogPath is not null)
    {
        var layoutGroups = allMorphEvidence
            .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var pairedGroups = allMorphEvidence
            .GroupBy(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var catalogLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "nested_big",
                "morph_object",
                "ord_sibling",
                "has_complete_sibling",
                "sibling_elf_validated",
                "sibling_bounds",
                "morph_layout_count",
                "paired_profile_count",
                "paired_layout_stable",
                "paired_layout_ambiguous",
                "morph_structural_layout",
                "payload_window_signature",
                "companion_structural_layout",
                "command_stream_signature",
                "command_stream_boundary_signature",
                "vertex_record_signature",
                "numeric_descriptor_signature",
                "candidate_mesh_signature",
                "candidate_geometry_signature",
                "companion_numeric_match_signature",
                "paired_structural_signature",
                "morph_shape_names",
                "morph_attribute_names",
                "sibling_shapename_state_values",
            }),
        };
        foreach (var item in allMorphEvidence
                     .OrderBy(item => item.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(item => item.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase))
        {
            var evidence = item.Evidence;
            var pairedSignature = EaglMorphSiblingReader.GetPairedStructuralSignature(evidence);
            var layoutGroup = layoutGroups[evidence.StructuralLayoutSignature];
            var stable = layoutGroup
                .Select(groupItem => EaglMorphSiblingReader.GetPairedStructuralSignature(groupItem.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count() == 1;
            catalogLines.Add(string.Join("\t", new[]
            {
                CatalogField(item.NestedArchiveName),
                CatalogField(evidence.MorphObjectName),
                CatalogField(evidence.OrdSiblingName),
                evidence.HasCompleteSibling.ToString(),
                evidence.SiblingElfValidated.ToString(),
                evidence.SiblingHasBounds.ToString(),
                layoutGroup.Length.ToString(CultureInfo.InvariantCulture),
                pairedGroups[pairedSignature].ToString(CultureInfo.InvariantCulture),
                stable.ToString(),
                (!stable).ToString(),
                CatalogField(evidence.StructuralLayoutSignature),
                CatalogField(evidence.NormalizedPayloadWindowHashSignature),
                CatalogField(evidence.SiblingStructuralLayoutSignature),
                CatalogField(evidence.SiblingCommandStreamSignature),
                CatalogField(evidence.SiblingCommandStreamBoundarySignature),
                CatalogField(evidence.SiblingVertexRecordSignature),
                CatalogField(evidence.SiblingNumericDescriptorSignature),
                CatalogField(evidence.SiblingCandidateMeshSignature),
                CatalogField(evidence.SiblingCandidateGeometrySignature),
                CatalogField(evidence.SiblingCompanionNumericMatchSignature),
                CatalogField(pairedSignature),
                CatalogField(string.Join("|", evidence.MorphShapeNames)),
                CatalogField(string.Join("|", evidence.MorphAttributeNames)),
                CatalogField(string.Join("|", evidence.SiblingShapeNameStateValues)),
            }));
        }

        var catalogDirectory = Path.GetDirectoryName(morphCatalogPath);
        if (!string.IsNullOrEmpty(catalogDirectory))
        {
            Directory.CreateDirectory(catalogDirectory);
        }

        File.WriteAllLines(morphCatalogPath, catalogLines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"catalog: {morphCatalogPath} | rows={allMorphEvidence.Count} | stable_layouts={layoutProfilesWithStablePairedStructuralSignature} | ambiguous_layouts={layoutProfilesWithMultiplePairedStructuralSignatures}");
    }
    if (morphAmbiguityReportPath is not null)
    {
        var reportGroups = allMorphEvidence
            .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .ToArray();
        var stableControlItems = reportGroups
            .Where(group => group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count() == 1)
            .Select(group => group.First())
            .ToArray();
        var stableControlPayloadWindows = stableControlItems
            .Select(item => item.Evidence.NormalizedPayloadWindowHashSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlCompanionLayouts = stableControlItems
            .Select(item => item.Evidence.SiblingStructuralLayoutSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlCommandStreams = stableControlItems
            .Select(item => item.Evidence.SiblingCommandStreamSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlVertexRecords = stableControlItems
            .Select(item => item.Evidence.SiblingVertexRecordSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlNumericDescriptors = stableControlItems
            .Select(item => item.Evidence.SiblingNumericDescriptorSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlCandidateMeshes = stableControlItems
            .Select(item => item.Evidence.SiblingCandidateMeshSignature)
            .ToHashSet(StringComparer.Ordinal);
        var stableControlNumericMatches = stableControlItems
            .Select(item => item.Evidence.SiblingCompanionNumericMatchSignature)
            .ToHashSet(StringComparer.Ordinal);
        var reportLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "morph_structural_layout",
                "layout_rows",
                "paired_structural_profiles",
                "payload_window_profiles",
                "companion_layout_profiles",
                "command_stream_profiles",
                "vertex_record_profiles",
                "numeric_descriptor_profiles",
                "candidate_mesh_profiles",
                "companion_numeric_match_profiles",
                "stable_control_payload_window_values",
                "stable_control_companion_layout_values",
                "stable_control_command_stream_values",
                "stable_control_vertex_record_values",
                "stable_control_numeric_descriptor_values",
                "stable_control_candidate_mesh_values",
                "stable_control_numeric_match_values",
                "stable_control_overlap_fields",
                "layout_stable",
                "layout_ambiguous",
                "variation_fields",
                "members",
            }),
        };
        var ambiguousLayoutCount = 0;
        var ambiguousRowCount = 0;
        var stableControlOverlapCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var group in reportGroups)
        {
            var pairedProfiles = group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count();
            var payloadWindowProfiles = group
                .Select(item => item.Evidence.NormalizedPayloadWindowHashSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var companionLayoutProfiles = group
                .Select(item => item.Evidence.SiblingStructuralLayoutSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var commandStreamProfiles = group
                .Select(item => item.Evidence.SiblingCommandStreamSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var vertexRecordProfiles = group
                .Select(item => item.Evidence.SiblingVertexRecordSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var numericDescriptorProfiles = group
                .Select(item => item.Evidence.SiblingNumericDescriptorSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var candidateMeshProfiles = group
                .Select(item => item.Evidence.SiblingCandidateMeshSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var companionNumericMatchProfiles = group
                .Select(item => item.Evidence.SiblingCompanionNumericMatchSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();
            var stableControlPayloadWindowValues = group
                .Select(item => item.Evidence.NormalizedPayloadWindowHashSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlPayloadWindows.Contains);
            var stableControlCompanionLayoutValues = group
                .Select(item => item.Evidence.SiblingStructuralLayoutSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlCompanionLayouts.Contains);
            var stableControlCommandStreamValues = group
                .Select(item => item.Evidence.SiblingCommandStreamSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlCommandStreams.Contains);
            var stableControlVertexRecordValues = group
                .Select(item => item.Evidence.SiblingVertexRecordSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlVertexRecords.Contains);
            var stableControlNumericDescriptorValues = group
                .Select(item => item.Evidence.SiblingNumericDescriptorSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlNumericDescriptors.Contains);
            var stableControlCandidateMeshValues = group
                .Select(item => item.Evidence.SiblingCandidateMeshSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlCandidateMeshes.Contains);
            var stableControlNumericMatchValues = group
                .Select(item => item.Evidence.SiblingCompanionNumericMatchSignature)
                .Distinct(StringComparer.Ordinal)
                .Count(stableControlNumericMatches.Contains);
            var variationFields = new List<string>();
            if (payloadWindowProfiles > 1)
            {
                variationFields.Add("payload_window");
            }
            if (companionLayoutProfiles > 1)
            {
                variationFields.Add("companion_layout");
            }
            if (commandStreamProfiles > 1)
            {
                variationFields.Add("command_stream");
            }
            if (vertexRecordProfiles > 1)
            {
                variationFields.Add("vertex_record");
            }
            if (numericDescriptorProfiles > 1)
            {
                variationFields.Add("numeric_descriptor");
            }
            if (candidateMeshProfiles > 1)
            {
                variationFields.Add("candidate_mesh");
            }
            if (companionNumericMatchProfiles > 1)
            {
                variationFields.Add("companion_numeric_match");
            }
            var stableControlOverlapFields = new List<string>();
            if (stableControlPayloadWindowValues > 0)
            {
                stableControlOverlapFields.Add("payload_window");
            }
            if (stableControlCompanionLayoutValues > 0)
            {
                stableControlOverlapFields.Add("companion_layout");
            }
            if (stableControlCommandStreamValues > 0)
            {
                stableControlOverlapFields.Add("command_stream");
            }
            if (stableControlVertexRecordValues > 0)
            {
                stableControlOverlapFields.Add("vertex_record");
            }
            if (stableControlNumericDescriptorValues > 0)
            {
                stableControlOverlapFields.Add("numeric_descriptor");
            }
            if (stableControlCandidateMeshValues > 0)
            {
                stableControlOverlapFields.Add("candidate_mesh");
            }
            if (stableControlNumericMatchValues > 0)
            {
                stableControlOverlapFields.Add("companion_numeric_match");
            }

            var layoutAmbiguous = pairedProfiles > 1;
            if (layoutAmbiguous)
            {
                ambiguousLayoutCount++;
                ambiguousRowCount += group.Count();
                foreach (var field in stableControlOverlapFields)
                {
                    stableControlOverlapCounts[field] =
                        stableControlOverlapCounts.GetValueOrDefault(field) + 1;
                }
            }

            var members = string.Join(",", group
                .OrderBy(item => item.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase)
                .Select(item => $"{item.NestedArchiveName}/{item.Evidence.MorphObjectName}"));
            reportLines.Add(string.Join("\t", new[]
            {
                CatalogField(group.Key),
                group.Count().ToString(CultureInfo.InvariantCulture),
                pairedProfiles.ToString(CultureInfo.InvariantCulture),
                payloadWindowProfiles.ToString(CultureInfo.InvariantCulture),
                companionLayoutProfiles.ToString(CultureInfo.InvariantCulture),
                commandStreamProfiles.ToString(CultureInfo.InvariantCulture),
                vertexRecordProfiles.ToString(CultureInfo.InvariantCulture),
                numericDescriptorProfiles.ToString(CultureInfo.InvariantCulture),
                candidateMeshProfiles.ToString(CultureInfo.InvariantCulture),
                companionNumericMatchProfiles.ToString(CultureInfo.InvariantCulture),
                stableControlPayloadWindowValues.ToString(CultureInfo.InvariantCulture),
                stableControlCompanionLayoutValues.ToString(CultureInfo.InvariantCulture),
                stableControlCommandStreamValues.ToString(CultureInfo.InvariantCulture),
                stableControlVertexRecordValues.ToString(CultureInfo.InvariantCulture),
                stableControlNumericDescriptorValues.ToString(CultureInfo.InvariantCulture),
                stableControlCandidateMeshValues.ToString(CultureInfo.InvariantCulture),
                stableControlNumericMatchValues.ToString(CultureInfo.InvariantCulture),
                CatalogField(string.Join(",", stableControlOverlapFields)),
                (!layoutAmbiguous).ToString(),
                layoutAmbiguous.ToString(),
                CatalogField(string.Join(",", variationFields)),
                CatalogField(members),
            }));
        }

        var reportDirectory = Path.GetDirectoryName(morphAmbiguityReportPath);
        if (!string.IsNullOrEmpty(reportDirectory))
        {
            Directory.CreateDirectory(reportDirectory);
        }

        File.WriteAllLines(morphAmbiguityReportPath, reportLines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"ambiguity_report: {morphAmbiguityReportPath} | layouts={reportGroups.Length} | stable_layouts={reportGroups.Length - ambiguousLayoutCount} | ambiguous_layouts={ambiguousLayoutCount} | ambiguous_rows={ambiguousRowCount}");
        Console.WriteLine($"stable_control_overlap_fields: {string.Join(",", stableControlOverlapCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => $"{pair.Key}={pair.Value}"))}");
    }
    if (morphByteComparisonReportPath is not null)
    {
        var byteReportGroups = allMorphEvidence
            .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .ToArray();
        var byteReportStableControls = byteReportGroups
            .Where(group => group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count() == 1)
            .Select(group => group.First())
            .ToArray();
        var byteReportLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "morph_structural_layout",
                "nested_big",
                "morph_object",
                "command_stream_bytes",
                "command_stream_sha256",
                "command_stream_boundary_signature",
                "command_control_exact_matches",
                "command_boundary_exact_matches",
                "command_structural_signature_exact_matches",
                "command_min_total_delta",
                "command_min_aligned_difference",
                "command_min_length_delta_abs",
                "command_max_common_prefix",
                "command_nearest_controls",
                "companion_numeric_match_record_bytes",
                "companion_numeric_match_record_sha256",
                "companion_numeric_match_boundary_signature",
                "companion_numeric_match_value_category_signature",
                "companion_numeric_match_control_exact_matches",
                "companion_numeric_match_signature_exact_matches",
                "companion_numeric_match_boundary_exact_matches",
                "companion_numeric_match_value_category_exact_matches",
                "companion_numeric_match_min_total_delta",
                "companion_numeric_match_min_aligned_difference",
                "companion_numeric_match_min_length_delta_abs",
                "companion_numeric_match_max_common_prefix",
                "companion_numeric_match_nearest_controls",
            }),
        };
        var byteReportAmbiguousRows = 0;
        var commandExactRows = 0;
        var commandBoundaryExactRows = 0;
        var commandStructuralExactRows = 0;
        var companionNumericExactRows = 0;
        var companionNumericSignatureExactRows = 0;
        var companionNumericBoundaryExactRows = 0;
        var companionNumericValueCategoryExactRows = 0;
        foreach (var group in byteReportGroups)
        {
            var pairedProfiles = group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (pairedProfiles <= 1)
            {
                continue;
            }

            foreach (var item in group
                         .OrderBy(item => item.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase))
            {
                byteReportAmbiguousRows++;
                var commandBytes = item.Evidence.SiblingCommandStreamBytes ?? Array.Empty<byte>();
                var companionNumericMatchBytes = Encoding.UTF8.GetBytes(
                    item.Evidence.SiblingCompanionNumericMatchCanonical ?? string.Empty);
                var commandComparisons = byteReportStableControls
                    .Select(control =>
                    {
                        var comparison = ByteSequenceComparer.Compare(
                            commandBytes,
                            control.Evidence.SiblingCommandStreamBytes ?? Array.Empty<byte>());
                        return (
                            Control: control,
                            Comparison: comparison,
                            Score: comparison.DifferentBytes + Math.Abs(comparison.LengthDelta));
                    })
                    .ToArray();
                var companionNumericComparisons = byteReportStableControls
                    .Select(control =>
                    {
                        var controlBytes = Encoding.UTF8.GetBytes(
                            control.Evidence.SiblingCompanionNumericMatchCanonical ?? string.Empty);
                        var comparison = ByteSequenceComparer.Compare(
                            companionNumericMatchBytes,
                            controlBytes);
                        return (
                            Control: control,
                            Comparison: comparison,
                            Score: comparison.DifferentBytes + Math.Abs(comparison.LengthDelta));
                    })
                    .ToArray();
                var commandExactMatches = commandComparisons.Count(item => item.Comparison.IsExact);
                var companionNumericExactMatches = companionNumericComparisons.Count(item => item.Comparison.IsExact);
                var commandBoundaryExactMatches = byteReportStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCommandStreamBoundarySignature,
                        control.Evidence.SiblingCommandStreamBoundarySignature,
                        StringComparison.Ordinal));
                var commandStructuralExactMatches = byteReportStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCommandStreamSignature,
                        control.Evidence.SiblingCommandStreamSignature,
                        StringComparison.Ordinal));
                var companionNumericSignatureExactMatches = byteReportStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCompanionNumericMatchSignature,
                        control.Evidence.SiblingCompanionNumericMatchSignature,
                        StringComparison.Ordinal));
                var companionNumericBoundaryExactMatches = byteReportStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCompanionNumericMatchBoundarySignature,
                        control.Evidence.SiblingCompanionNumericMatchBoundarySignature,
                        StringComparison.Ordinal));
                var companionNumericValueCategoryExactMatches = byteReportStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCompanionNumericMatchValueCategorySignature,
                        control.Evidence.SiblingCompanionNumericMatchValueCategorySignature,
                        StringComparison.Ordinal));
                commandExactRows += commandExactMatches > 0 ? 1 : 0;
                commandBoundaryExactRows += commandBoundaryExactMatches > 0 ? 1 : 0;
                commandStructuralExactRows += commandStructuralExactMatches > 0 ? 1 : 0;
                companionNumericExactRows += companionNumericExactMatches > 0 ? 1 : 0;
                companionNumericSignatureExactRows += companionNumericSignatureExactMatches > 0 ? 1 : 0;
                companionNumericBoundaryExactRows += companionNumericBoundaryExactMatches > 0 ? 1 : 0;
                companionNumericValueCategoryExactRows += companionNumericValueCategoryExactMatches > 0 ? 1 : 0;

                ((string NestedArchiveName, EaglMorphSiblingEvidence Evidence) Control,
                    ByteSequenceComparison Comparison,
                    int Score)? commandMinimum = commandComparisons.Length == 0
                    ? null
                    : commandComparisons.MinBy(item => item.Score);
                ((string NestedArchiveName, EaglMorphSiblingEvidence Evidence) Control,
                    ByteSequenceComparison Comparison,
                    int Score)? companionNumericMinimum = companionNumericComparisons.Length == 0
                    ? null
                    : companionNumericComparisons.MinBy(item => item.Score);
                var commandMinimumScore = commandMinimum?.Score.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var commandMinimumDifference = commandMinimum?.Comparison.DifferentBytes.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var commandMinimumLengthDelta = commandMinimum is null
                    ? string.Empty
                    : Math.Abs(commandMinimum.Value.Comparison.LengthDelta).ToString(CultureInfo.InvariantCulture);
                var commandMaximumPrefix = commandComparisons.Length == 0
                    ? string.Empty
                    : commandComparisons.Max(item => item.Comparison.CommonPrefixLength).ToString(CultureInfo.InvariantCulture);
                var commandNearestControls = commandMinimum is null
                    ? string.Empty
                    : string.Join(",", commandComparisons
                        .Where(candidate => candidate.Score == commandMinimum.Value.Score)
                        .OrderBy(candidate => candidate.Control.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(candidate => candidate.Control.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .Select(candidate => CatalogField($"{candidate.Control.NestedArchiveName}/{candidate.Control.Evidence.MorphObjectName}")));
                var companionNumericMinimumScore = companionNumericMinimum?.Score.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var companionNumericMinimumDifference = companionNumericMinimum?.Comparison.DifferentBytes.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var companionNumericMinimumLengthDelta = companionNumericMinimum is null
                    ? string.Empty
                    : Math.Abs(companionNumericMinimum.Value.Comparison.LengthDelta).ToString(CultureInfo.InvariantCulture);
                var companionNumericMaximumPrefix = companionNumericComparisons.Length == 0
                    ? string.Empty
                    : companionNumericComparisons.Max(item => item.Comparison.CommonPrefixLength).ToString(CultureInfo.InvariantCulture);
                var companionNumericNearestControls = companionNumericMinimum is null
                    ? string.Empty
                    : string.Join(",", companionNumericComparisons
                        .Where(candidate => candidate.Score == companionNumericMinimum.Value.Score)
                        .OrderBy(candidate => candidate.Control.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(candidate => candidate.Control.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .Select(candidate => CatalogField($"{candidate.Control.NestedArchiveName}/{candidate.Control.Evidence.MorphObjectName}")));

                byteReportLines.Add(string.Join("\t", new[]
                {
                    CatalogField(item.Evidence.StructuralLayoutSignature),
                    CatalogField(item.NestedArchiveName),
                    CatalogField(item.Evidence.MorphObjectName),
                    commandBytes.Length.ToString(CultureInfo.InvariantCulture),
                    Convert.ToHexString(SHA256.HashData(commandBytes)),
                    CatalogField(item.Evidence.SiblingCommandStreamBoundarySignature),
                    commandExactMatches.ToString(CultureInfo.InvariantCulture),
                    commandBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    commandStructuralExactMatches.ToString(CultureInfo.InvariantCulture),
                    commandMinimumScore,
                    commandMinimumDifference,
                    commandMinimumLengthDelta,
                    commandMaximumPrefix,
                    commandNearestControls,
                    companionNumericMatchBytes.Length.ToString(CultureInfo.InvariantCulture),
                    Convert.ToHexString(SHA256.HashData(companionNumericMatchBytes)),
                    CatalogField(item.Evidence.SiblingCompanionNumericMatchBoundarySignature),
                    CatalogField(item.Evidence.SiblingCompanionNumericMatchValueCategorySignature),
                    companionNumericExactMatches.ToString(CultureInfo.InvariantCulture),
                    companionNumericSignatureExactMatches.ToString(CultureInfo.InvariantCulture),
                    companionNumericBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    companionNumericValueCategoryExactMatches.ToString(CultureInfo.InvariantCulture),
                    companionNumericMinimumScore,
                    companionNumericMinimumDifference,
                    companionNumericMinimumLengthDelta,
                    companionNumericMaximumPrefix,
                    companionNumericNearestControls,
                }));
            }
        }

        var byteReportDirectory = Path.GetDirectoryName(morphByteComparisonReportPath);
        if (!string.IsNullOrEmpty(byteReportDirectory))
        {
            Directory.CreateDirectory(byteReportDirectory);
        }

        File.WriteAllLines(
            morphByteComparisonReportPath,
            byteReportLines,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        Console.WriteLine($"byte_comparison_report: {morphByteComparisonReportPath} | ambiguous_rows={byteReportAmbiguousRows} | stable_control_rows={byteReportStableControls.Length} | command_exact_rows={commandExactRows} | command_boundary_exact_rows={commandBoundaryExactRows} | command_structural_exact_rows={commandStructuralExactRows} | companion_numeric_match_exact_rows={companionNumericExactRows} | companion_numeric_match_signature_exact_rows={companionNumericSignatureExactRows} | companion_numeric_match_boundary_exact_rows={companionNumericBoundaryExactRows} | companion_numeric_match_value_category_exact_rows={companionNumericValueCategoryExactRows}");
    }
    if (morphNumericGapReportPath is not null || morphNumericGapCorrelationReportPath is not null)
    {
        var numericGapGroups = allMorphEvidence
            .GroupBy(item => item.Evidence.StructuralLayoutSignature, StringComparer.Ordinal)
            .ToArray();
        var numericGapStableControls = numericGapGroups
            .Where(group => group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count() == 1)
            .Select(group => group.First())
            .ToArray();
        var numericGapLines = new List<string>
        {
            string.Join("\t", new[]
            {
                "morph_structural_layout",
                "nested_big",
                "morph_object",
                "companion_numeric_match_boundary_signature",
                "companion_numeric_match_value_category_signature",
                "boundary_control_exact_matches",
                "value_category_control_exact_matches",
                "payload_window_control_exact_matches",
                "canonical_offset_control_exact_matches",
                "value_category_token_count",
                "value_category_min_token_score",
                "value_category_min_token_differences",
                "value_category_min_token_prefix_differences",
                "value_category_min_token_suffix_differences",
                "value_category_min_token_difference_indexes",
                "value_category_min_token_count_delta_abs",
                "value_category_max_common_prefix_tokens",
                "value_category_nearest_token_controls",
                "nearest_control_canonical_word_token_count",
                "nearest_control_canonical_word_differences",
                "nearest_control_canonical_offset_differences",
                "nearest_control_canonical_suffix_differences",
                "nearest_control_canonical_little_endian_byte_differences",
                "nearest_control_canonical_common_prefix_words",
                "nearest_control_canonical_offset_delta_signature",
                "relative_offset_boundary_signature",
                "relative_boundary_control_exact_matches",
                "nearest_control_relative_offset_boundary_signature",
                "nearest_control_payload_window_hash_count",
                "nearest_control_payload_window_hash_differences",
                "nearest_control_payload_window_hash_equal",
                "nearest_control_payload_window_hash_common_prefix",
            }),
        };
        var numericGapObservations = new List<NumericGapCorrelationObservation>();
        var numericGapRowCount = 0;
        var numericGapLayoutNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in numericGapGroups)
        {
            var pairedProfiles = group
                .Select(item => EaglMorphSiblingReader.GetPairedStructuralSignature(item.Evidence))
                .Distinct(StringComparer.Ordinal)
                .Count();
            if (pairedProfiles <= 1)
            {
                continue;
            }

            foreach (var item in group
                         .OrderBy(item => item.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(item => item.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase))
            {
                var valueCategoryPayload = GetNumericValueCategoryPayload(
                    item.Evidence.SiblingCompanionNumericMatchValueCategorySignature);
                var valueCategoryTokens = GetNumericValueCategoryTokens(valueCategoryPayload);
                var canonicalTokens = NumericTokenSequenceComparer.ParseCanonical(
                    item.Evidence.SiblingCompanionNumericMatchCanonical);
                var relativeBoundarySignature =
                    NumericTokenSequenceComparer.GetRelativeOffsetBoundarySignature(canonicalTokens);
                var payloadWindowHashes = GetNormalizedPayloadWindowHashes(
                    item.Evidence.NormalizedPayloadWindowHashSignature);
                var valueCategoryComparisons = numericGapStableControls
                    .Select(control =>
                    {
                        var controlTokens = GetNumericValueCategoryTokens(
                            GetNumericValueCategoryPayload(
                                control.Evidence.SiblingCompanionNumericMatchValueCategorySignature));
                        var comparison = TokenSequenceComparer.CompareDelimited(
                            valueCategoryTokens,
                            controlTokens,
                            ':');
                        var controlCanonicalTokens = NumericTokenSequenceComparer.ParseCanonical(
                            control.Evidence.SiblingCompanionNumericMatchCanonical);
                        var canonicalComparison = NumericTokenSequenceComparer.Compare(
                            canonicalTokens,
                            controlCanonicalTokens);
                        var controlPayloadWindowHashes = GetNormalizedPayloadWindowHashes(
                            control.Evidence.NormalizedPayloadWindowHashSignature);
                        var payloadWindowComparison = TokenSequenceComparer.Compare(
                            payloadWindowHashes,
                            controlPayloadWindowHashes);
                        return (
                            Control: control,
                            Comparison: comparison,
                            CanonicalComparison: canonicalComparison,
                            PayloadWindowComparison: payloadWindowComparison,
                            Score: comparison.DifferentTokens + Math.Abs(comparison.LengthDelta));
                    })
                    .ToArray();
                var valueCategoryExactMatches = valueCategoryComparisons.Count(
                    candidate => candidate.Comparison.IsExact);
                if (valueCategoryExactMatches > 0)
                {
                    continue;
                }

                numericGapRowCount++;
                numericGapLayoutNames.Add(group.Key);
                var boundaryExactMatches = numericGapStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCompanionNumericMatchBoundarySignature,
                        control.Evidence.SiblingCompanionNumericMatchBoundarySignature,
                        StringComparison.Ordinal));
                var rawCommandBoundaryExactMatches = numericGapStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.SiblingCommandStreamBoundarySignature,
                        control.Evidence.SiblingCommandStreamBoundarySignature,
                        StringComparison.Ordinal));
                ((string NestedArchiveName, EaglMorphSiblingEvidence Evidence) Control,
                    TokenSequenceComparison Comparison,
                    NumericTokenSequenceComparison CanonicalComparison,
                    TokenSequenceComparison PayloadWindowComparison,
                    int Score)? minimum = valueCategoryComparisons.Length == 0
                    ? null
                    : valueCategoryComparisons.MinBy(candidate => candidate.Score);
                var minimumScore = minimum?.Score.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var minimumDifference = minimum?.Comparison.DifferentTokens.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var minimumPrefixDifference = minimum?.Comparison.PrefixFieldDifferences.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var minimumSuffixDifference = minimum?.Comparison.SuffixFieldDifferences.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
                var minimumDifferenceIndexes = minimum is null
                    ? string.Empty
                    : string.Join(",", minimum.Value.Comparison.DifferentTokenIndexes
                        .Select(index => index.ToString(CultureInfo.InvariantCulture)));
                var minimumLengthDelta = minimum is null
                    ? string.Empty
                    : Math.Abs(minimum.Value.Comparison.LengthDelta).ToString(CultureInfo.InvariantCulture);
                var maximumPrefix = valueCategoryComparisons.Length == 0
                    ? string.Empty
                    : valueCategoryComparisons.Max(candidate => candidate.Comparison.CommonPrefixLength)
                        .ToString(CultureInfo.InvariantCulture);
                var nearestTokenControls = minimum is null
                    ? string.Empty
                    : string.Join(",", valueCategoryComparisons
                        .Where(candidate => candidate.Score == minimum.Value.Score)
                        .OrderBy(candidate => candidate.Control.NestedArchiveName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(candidate => candidate.Control.Evidence.MorphObjectName, StringComparer.OrdinalIgnoreCase)
                        .Take(3)
                        .Select(candidate => CatalogField(
                            $"{candidate.Control.NestedArchiveName}/{candidate.Control.Evidence.MorphObjectName}")));
                var nearestCanonicalWordTokenCount = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.ComparedCount.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalWordDifferences = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.DifferentWords.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalOffsetDifferences = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.DifferentOffsets.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalSuffixDifferences = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.DifferentSuffixes.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalByteDifferences = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.DifferentLittleEndianBytes.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalCommonPrefix = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.CommonPrefixLength.ToString(CultureInfo.InvariantCulture);
                var nearestCanonicalOffsetDeltaSignature = minimum is null
                    ? string.Empty
                    : minimum.Value.CanonicalComparison.OffsetDeltaSignature;
                var relativeBoundaryExactMatches = numericGapStableControls.Count(control =>
                    string.Equals(
                        relativeBoundarySignature,
                        NumericTokenSequenceComparer.GetRelativeOffsetBoundarySignature(
                            NumericTokenSequenceComparer.ParseCanonical(
                                control.Evidence.SiblingCompanionNumericMatchCanonical)),
                        StringComparison.Ordinal));
                var payloadWindowExactMatches = numericGapStableControls.Count(control =>
                    string.Equals(
                        item.Evidence.NormalizedPayloadWindowHashSignature,
                        control.Evidence.NormalizedPayloadWindowHashSignature,
                        StringComparison.Ordinal));
                var canonicalOffsetExactMatches = valueCategoryComparisons.Count(candidate =>
                    candidate.CanonicalComparison.LeftCount == candidate.CanonicalComparison.RightCount &&
                    candidate.CanonicalComparison.DifferentOffsets == 0);
                var nearestRelativeBoundarySignature = minimum is null
                    ? string.Empty
                    : NumericTokenSequenceComparer.GetRelativeOffsetBoundarySignature(
                        NumericTokenSequenceComparer.ParseCanonical(
                            minimum.Value.Control.Evidence.SiblingCompanionNumericMatchCanonical));
                var nearestPayloadWindowHashCount = minimum is null
                    ? string.Empty
                    : minimum.Value.PayloadWindowComparison.ComparedCount.ToString(CultureInfo.InvariantCulture);
                var nearestPayloadWindowHashDifferences = minimum is null
                    ? string.Empty
                    : minimum.Value.PayloadWindowComparison.DifferentTokens.ToString(CultureInfo.InvariantCulture);
                var nearestPayloadWindowHashEqual = minimum is null
                    ? string.Empty
                    : minimum.Value.PayloadWindowComparison.EqualTokens.ToString(CultureInfo.InvariantCulture);
                var nearestPayloadWindowHashCommonPrefix = minimum is null
                    ? string.Empty
                    : minimum.Value.PayloadWindowComparison.CommonPrefixLength.ToString(CultureInfo.InvariantCulture);
                numericGapObservations.Add(new NumericGapCorrelationObservation(
                    minimum?.CanonicalComparison.OffsetDeltaSignature ?? string.Empty,
                    item.Evidence.StructuralLayoutSignature,
                    item.NestedArchiveName,
                    item.Evidence.MorphObjectName,
                    item.Evidence.NormalizedPayloadWindowHashSignature,
                    item.Evidence.SiblingCommandStreamBoundarySignature ?? string.Empty,
                    rawCommandBoundaryExactMatches,
                    payloadWindowHashes.Count,
                    payloadWindowHashes.Distinct(StringComparer.Ordinal).Count(),
                    relativeBoundaryExactMatches,
                    boundaryExactMatches,
                    payloadWindowExactMatches,
                    canonicalOffsetExactMatches));
                numericGapLines.Add(string.Join("\t", new[]
                {
                    CatalogField(item.Evidence.StructuralLayoutSignature),
                    CatalogField(item.NestedArchiveName),
                    CatalogField(item.Evidence.MorphObjectName),
                    CatalogField(item.Evidence.SiblingCompanionNumericMatchBoundarySignature),
                    CatalogField(item.Evidence.SiblingCompanionNumericMatchValueCategorySignature),
                    boundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    valueCategoryExactMatches.ToString(CultureInfo.InvariantCulture),
                    payloadWindowExactMatches.ToString(CultureInfo.InvariantCulture),
                    canonicalOffsetExactMatches.ToString(CultureInfo.InvariantCulture),
                    valueCategoryTokens.Count.ToString(CultureInfo.InvariantCulture),
                    minimumScore,
                    minimumDifference,
                    minimumPrefixDifference,
                    minimumSuffixDifference,
                    minimumDifferenceIndexes,
                    minimumLengthDelta,
                    maximumPrefix,
                    nearestTokenControls,
                    nearestCanonicalWordTokenCount,
                    nearestCanonicalWordDifferences,
                    nearestCanonicalOffsetDifferences,
                    nearestCanonicalSuffixDifferences,
                    nearestCanonicalByteDifferences,
                    nearestCanonicalCommonPrefix,
                    nearestCanonicalOffsetDeltaSignature,
                    CatalogField(relativeBoundarySignature),
                    relativeBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    CatalogField(nearestRelativeBoundarySignature),
                    nearestPayloadWindowHashCount,
                    nearestPayloadWindowHashDifferences,
                    nearestPayloadWindowHashEqual,
                    nearestPayloadWindowHashCommonPrefix,
                }));
            }
        }

        if (morphNumericGapReportPath is not null)
        {
            var numericGapDirectory = Path.GetDirectoryName(morphNumericGapReportPath);
            if (!string.IsNullOrEmpty(numericGapDirectory))
            {
                Directory.CreateDirectory(numericGapDirectory);
            }

            File.WriteAllLines(
                morphNumericGapReportPath,
                numericGapLines,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"numeric_gap_report: {morphNumericGapReportPath} | rows={numericGapRowCount} | layouts={numericGapLayoutNames.Count} | stable_control_rows={numericGapStableControls.Length} | value_category_exact_rows=0");
        }

        if (morphNumericGapCorrelationReportPath is not null)
        {
            var correlationProfiles = NumericGapCorrelation.Summarize(numericGapObservations);
            var correlationLines = new List<string>
            {
                string.Join("\t", new[]
                {
                    "offset_delta_signature",
                    "gap_row_count",
                    "layout_count",
                    "payload_window_profile_count",
                    "repeated_payload_window_profile_count",
                    "raw_command_boundary_profile_count",
                    "repeated_raw_command_boundary_profile_count",
                    "rows_with_stable_raw_boundary_match",
                    "max_stable_raw_boundary_exact_matches",
                    "total_stable_raw_boundary_exact_matches",
                    "rows_with_relative_boundary_match",
                    "max_relative_boundary_exact_matches",
                    "total_relative_boundary_exact_matches",
                    "rows_with_numeric_boundary_match",
                    "total_numeric_boundary_exact_matches",
                    "rows_with_payload_window_match",
                    "total_payload_window_exact_matches",
                    "rows_with_canonical_offset_match",
                    "total_canonical_offset_exact_matches",
                    "window_hash_count_values",
                    "distinct_window_hash_count_values",
                    "payload_window_profile_hashes",
                    "raw_command_boundary_profile_hashes",
                    "gap_rows",
                }),
            };
            foreach (var profile in correlationProfiles)
            {
                correlationLines.Add(string.Join("\t", new[]
                {
                    CatalogField(profile.OffsetDeltaSignature),
                    profile.GapRowCount.ToString(CultureInfo.InvariantCulture),
                    profile.LayoutCount.ToString(CultureInfo.InvariantCulture),
                    profile.PayloadWindowProfileCount.ToString(CultureInfo.InvariantCulture),
                    profile.RepeatedPayloadWindowProfileCount.ToString(CultureInfo.InvariantCulture),
                    profile.RawCommandBoundaryProfileCount.ToString(CultureInfo.InvariantCulture),
                    profile.RepeatedRawCommandBoundaryProfileCount.ToString(CultureInfo.InvariantCulture),
                    profile.RowsWithStableRawBoundaryMatch.ToString(CultureInfo.InvariantCulture),
                    profile.MaxStableRawBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.TotalStableRawBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.RowsWithRelativeBoundaryMatch.ToString(CultureInfo.InvariantCulture),
                    profile.MaxRelativeBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.TotalRelativeBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.RowsWithNumericBoundaryMatch.ToString(CultureInfo.InvariantCulture),
                    profile.TotalNumericBoundaryExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.RowsWithPayloadWindowMatch.ToString(CultureInfo.InvariantCulture),
                    profile.TotalPayloadWindowExactMatches.ToString(CultureInfo.InvariantCulture),
                    profile.RowsWithCanonicalOffsetMatch.ToString(CultureInfo.InvariantCulture),
                    profile.TotalCanonicalOffsetExactMatches.ToString(CultureInfo.InvariantCulture),
                    CatalogField(profile.WindowHashCountValues),
                    CatalogField(profile.DistinctWindowHashCountValues),
                    CatalogField(profile.PayloadWindowProfileHashes),
                    CatalogField(profile.RawCommandBoundaryProfileHashes),
                    CatalogField(profile.GapRows),
                }));
            }

            var correlationDirectory = Path.GetDirectoryName(morphNumericGapCorrelationReportPath);
            if (!string.IsNullOrEmpty(correlationDirectory))
            {
                Directory.CreateDirectory(correlationDirectory);
            }

            File.WriteAllLines(
                morphNumericGapCorrelationReportPath,
                correlationLines,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Console.WriteLine($"numeric_gap_correlation_report: {morphNumericGapCorrelationReportPath} | rows={numericGapObservations.Count} | offset_delta_profiles={correlationProfiles.Count} | payload_window_profiles={correlationProfiles.Sum(profile => profile.PayloadWindowProfileCount)} | raw_command_boundary_profiles={correlationProfiles.Sum(profile => profile.RawCommandBoundaryProfileCount)}");
        }
    }
    if (morphGeometryReportPath is not null || morphGeometrySummaryPath is not null)
    {
        if (morphGeometryReportPath is null || morphGeometrySummaryPath is null)
        {
            Console.Error.WriteLine("Os relatórios geométrico e resumo geométrico devem ser fornecidos juntos.");
            return 2;
        }

        var geometryObservations = allMorphEvidence
            .Select(item => new MorphCandidateGeometryObservation(item.NestedArchiveName, item.Evidence));
        var geometryRows = MorphCandidateGeometryAudit.Audit(geometryObservations);
        MorphCandidateGeometryAudit.WriteReports(
            morphGeometryReportPath,
            morphGeometrySummaryPath,
            geometryRows);
        var geometrySummary = MorphCandidateGeometryAudit.Summarize(geometryRows)
            .Single(row => row.Scope == "all");
        Console.WriteLine($"candidate_geometry_report: {morphGeometryReportPath} | summary={morphGeometrySummaryPath} | layouts={geometryRows.Count} | geometry_variable_layouts={geometrySummary.CandidateGeometryVariableLayouts} | mesh_stable_geometry_variable_layouts={geometrySummary.CandidateMeshStableGeometryVariableLayouts} | missing_geometry_layouts={geometrySummary.MissingGeometryLayouts}");
    }
    if (morphGeometryCorrelationReportPath is not null || morphGeometryCorrelationSummaryPath is not null)
    {
        if (morphGeometryCorrelationReportPath is null || morphGeometryCorrelationSummaryPath is null)
        {
            Console.Error.WriteLine("Os relatórios de correlação geométrica e resumo devem ser fornecidos juntos.");
            return 2;
        }

        var correlationObservations = allMorphEvidence
            .Select(item => new MorphCandidateGeometryObservation(item.NestedArchiveName, item.Evidence));
        var correlationRows = MorphCandidateGeometryAudit.Correlate(correlationObservations);
        MorphCandidateGeometryAudit.WriteCorrelationReports(
            morphGeometryCorrelationReportPath,
            morphGeometryCorrelationSummaryPath,
            correlationRows);
        var correlationSummary = MorphCandidateGeometryAudit.SummarizeCorrelation(correlationRows)
            .Single(row => row.Scope == "all");
        Console.WriteLine($"candidate_geometry_correlation_report: {morphGeometryCorrelationReportPath} | summary={morphGeometryCorrelationSummaryPath} | layouts={correlationRows.Count} | pair_variable_layouts={correlationSummary.PayloadGeometryPairVariableLayouts} | payload_stable_geometry_variable_layouts={correlationSummary.PayloadStableGeometryVariableLayouts} | both_variable_layouts={correlationSummary.BothPayloadAndGeometryVariableLayouts}");
    }
    Console.WriteLine($"relocation_signature_profiles: {string.Join(" | ", signatureProfiles)}");
    Console.WriteLine("status: same-nested-BIG filename sibling, ELF/bounds, and candidate-geometry evidence only; no morph, rig, pose, weight, frame, animation, material, UV, or GPU binding was assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIGs, managed ELF inspection");
    return 0;
}

if (string.Equals(args[0], "eagl-morph-sibling-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-morph-sibling-smoke <caminho-do-iso> <nested.big> [--verbose]");
        return 2;
    }

    using var morphSiblingStore = new GameAssetStore(args[1]);
    using var morphSiblingCompressedStream = morphSiblingStore.OpenBigEntry("data/models.big", args[2]);
    if (morphSiblingCompressedStream.Length > 64L * 1024 * 1024)
    {
        throw new InvalidDataException("Nested EAGL archive exceeds the 64 MiB safety limit.");
    }

    var morphSiblingCompressed = new byte[checked((int)morphSiblingCompressedStream.Length)];
    morphSiblingCompressedStream.ReadExactly(morphSiblingCompressed);
    using var morphSiblingCompressedMemory = new MemoryStream(morphSiblingCompressed, writable: false);
    var morphSiblingExpanded = RefPackDecoder.Decode(
        morphSiblingCompressedMemory,
        RefPackDecoder.ReadDeclaredSize(morphSiblingCompressed));
    var morphSiblingArchive = BigArchiveReader.Inspect(morphSiblingExpanded.Data);
    var ordByStem = morphSiblingArchive.Entries
        .Where(entry => string.Equals(Path.GetExtension(entry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
        .GroupBy(entry => Path.GetFileNameWithoutExtension(entry.Name), StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    var entryNames = morphSiblingArchive.Entries
        .Select(entry => entry.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var verboseMorphSibling = args.Any(argument =>
        string.Equals(argument, "--verbose", StringComparison.OrdinalIgnoreCase));
    var morphEntryCount = 0;
    var validMorphCount = 0;
    var malformedMorphCount = 0;
    var normalizedSiblingCount = 0;
    var completeSiblingCount = 0;
    var validSiblingElfCount = 0;
    var siblingGeometryCount = 0;
    var siblingBoundsCount = 0;
    var unmatchedNamingCount = 0;
    var morphShapeNameCount = 0;
    var morphAttributeNameCount = 0;
    var morphPosePaletteBankSymbolCount = 0;
    var relocationSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var relativeRelocationSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var structuralLayoutSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var vertexDescriptorSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var vertexRecordSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var companionStructuralLayoutSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var commandStreamSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var numericDescriptorSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var candidateMeshSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var companionNumericMatchSignatures = new Dictionary<string, int>(StringComparer.Ordinal);
    var normalizedPayloadHashes = new Dictionary<string, int>(StringComparer.Ordinal);
    var normalizedPayloadWindowHashSignatures = new Dictionary<string, int>(StringComparer.Ordinal);

    foreach (var morphEntry in morphSiblingArchive.Entries.Where(entry =>
                 string.Equals(Path.GetExtension(entry.Name), ".o", StringComparison.OrdinalIgnoreCase)))
    {
        morphEntryCount++;
        try
        {
            var morphData = BigArchiveReader.ReadEntry(morphSiblingExpanded.Data, morphEntry.Name);
            var morphInspection = EaglObjectReader.Inspect(morphData);
            var morph = EaglMorphReader.Inspect(morphData, morphInspection.Elf);
            if (!morph.HasMorphSymbol)
            {
                continue;
            }

            validMorphCount++;
            morphShapeNameCount += morph.ShapeNames.Count;
            morphAttributeNameCount += morph.AttributeNames.Count;
            morphPosePaletteBankSymbolCount += morph.PosePaletteBankSymbols.Count;
            var signature = EaglMorphReader.GetRelocationFieldSignature(morph);
            relocationSignatures[signature] = relocationSignatures.GetValueOrDefault(signature) + 1;
            var relativeSignature = EaglMorphReader.GetRelativeRelocationFieldSignature(morph);
            relativeRelocationSignatures[relativeSignature] = relativeRelocationSignatures.GetValueOrDefault(relativeSignature) + 1;
            var structuralLayoutSignature = EaglMorphReader.GetStructuralLayoutSignature(
                morphInspection.Elf,
                morph);
            structuralLayoutSignatures[structuralLayoutSignature] = structuralLayoutSignatures.GetValueOrDefault(structuralLayoutSignature) + 1;
            var normalizedPayloadHash = EaglMorphReader.GetNormalizedPayloadHash(
                morphData,
                morphInspection.Elf,
                morph);
            normalizedPayloadHashes[normalizedPayloadHash] = normalizedPayloadHashes.GetValueOrDefault(normalizedPayloadHash) + 1;
            var normalizedPayloadWindowHashSignature =
                EaglMorphReader.GetNormalizedPayloadWindowHashSignature(
                    morphData,
                    morphInspection.Elf,
                    morph);
            normalizedPayloadWindowHashSignatures[normalizedPayloadWindowHashSignature] =
                normalizedPayloadWindowHashSignatures.GetValueOrDefault(normalizedPayloadWindowHashSignature) + 1;

            if (!EaglMorphReader.TryGetNormalizedSiblingStem(morphEntry.Name, out var siblingStem) ||
                !ordByStem.TryGetValue(siblingStem, out var ordEntry))
            {
                unmatchedNamingCount++;
                if (verboseMorphSibling)
                {
                    Console.WriteLine($"morph_candidate: {morphEntry.Name} | sibling=none | signature={signature}");
                }
                continue;
            }

            normalizedSiblingCount++;
            var orlName = Path.ChangeExtension(ordEntry.Name, ".orl");
            if (!entryNames.Contains(orlName))
            {
                if (verboseMorphSibling)
                {
                    Console.WriteLine($"morph_candidate: {morphEntry.Name} | sibling={ordEntry.Name} | orl=missing | signature={signature}");
                }
                continue;
            }

            completeSiblingCount++;
            var siblingObjectData = BigArchiveReader.ReadEntry(morphSiblingExpanded.Data, ordEntry.Name);
            var siblingLinkData = BigArchiveReader.ReadEntry(morphSiblingExpanded.Data, orlName);
            var siblingCombinedData = EaglObjectReader.Combine(siblingObjectData, siblingLinkData);
            var siblingInspection = EaglObjectReader.Inspect(siblingCombinedData);
            var companionStructuralLayoutSignature =
                EaglObjectReader.GetStructuralLayoutSignature(siblingInspection);
            companionStructuralLayoutSignatures[companionStructuralLayoutSignature] =
                companionStructuralLayoutSignatures.GetValueOrDefault(companionStructuralLayoutSignature) + 1;
            var siblingVertexStreams = EaglVertexBufferReader.Inspect(
                siblingCombinedData,
                siblingInspection.Elf);
            var siblingPrimitives = EaglModelReader.Inspect(
                siblingCombinedData,
                siblingInspection.Elf,
                siblingInspection.Bounds,
                siblingVertexStreams);
            var commandStreamSignature =
                EaglModelReader.GetStructuralCommandStreamSignature(siblingPrimitives);
            commandStreamSignatures[commandStreamSignature] =
                commandStreamSignatures.GetValueOrDefault(commandStreamSignature) + 1;
            var vertexDescriptorSignature = EaglVertexBufferReader.GetStructuralSignature(siblingVertexStreams);
            vertexDescriptorSignatures[vertexDescriptorSignature] = vertexDescriptorSignatures.GetValueOrDefault(vertexDescriptorSignature) + 1;
            var vertexRecordSignature = EaglVertexBufferReader.GetRelativeRecordStructuralSignature(siblingVertexStreams);
            vertexRecordSignatures[vertexRecordSignature] = vertexRecordSignatures.GetValueOrDefault(vertexRecordSignature) + 1;
            var numericDescriptorSignature = EaglVertexBufferReader.GetNumericStructuralSignature(
                siblingCombinedData,
                siblingInspection.Elf,
                siblingVertexStreams,
                siblingInspection.Bounds);
            numericDescriptorSignatures[numericDescriptorSignature] = numericDescriptorSignatures.GetValueOrDefault(numericDescriptorSignature) + 1;
            var candidateMeshSignature = string.Empty;
            EaglCandidateMesh? candidateMesh = null;
            if (siblingInspection.Bounds is not null)
            {
                try
                {
                    candidateMesh = EaglCandidateMeshBuilder.Build(
                        siblingCombinedData,
                        siblingInspection.Elf,
                        siblingInspection.Bounds);
                    candidateMeshSignature = EaglCandidateMeshBuilder.GetStructuralSignature(candidateMesh);
                }
                catch (Exception exception) when (
                    exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
                {
                    candidateMeshSignature = string.Empty;
                }
            }
            candidateMeshSignatures[candidateMeshSignature] = candidateMeshSignatures.GetValueOrDefault(candidateMeshSignature) + 1;
            var companionNumericMatchSignature = EaglMorphReader.GetCompanionNumericMatchSignature(
                morphData,
                morphInspection.Elf,
                morph,
                siblingInspection,
                siblingVertexStreams,
                siblingPrimitives,
                candidateMesh);
            companionNumericMatchSignatures[companionNumericMatchSignature] =
                companionNumericMatchSignatures.GetValueOrDefault(companionNumericMatchSignature) + 1;
            validSiblingElfCount++;
            if (siblingInspection.GeometrySymbols.Count > 0)
            {
                siblingGeometryCount++;
            }
            if (siblingInspection.Bounds is not null)
            {
                siblingBoundsCount++;
            }
            if (verboseMorphSibling)
            {
                Console.WriteLine(
                    $"morph_candidate: {morphEntry.Name} | sibling={ordEntry.Name}+{orlName} | " +
                    $"geometry_symbols={siblingInspection.GeometrySymbols.Count} | bounds={(siblingInspection.Bounds is null ? "none" : "present")} | signature={signature}");
            }
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or ArgumentOutOfRangeException)
        {
            malformedMorphCount++;
            if (verboseMorphSibling)
            {
                Console.WriteLine($"morph_invalid: {morphEntry.Name} | {exception.Message}");
            }
        }
    }

    Console.WriteLine($"{GameIdentity.Name} PC EAGL morph/geometry sibling smoke");
    Console.WriteLine($"nested_big: {args[2]} | entries={morphSiblingArchive.FileCount}");
    Console.WriteLine($"morph_object_entries: {morphEntryCount}");
    Console.WriteLine($"valid_morph_objects: {validMorphCount}");
    Console.WriteLine($"morph_shape_names: {morphShapeNameCount}");
    Console.WriteLine($"morph_attribute_names: {morphAttributeNameCount}");
    Console.WriteLine($"morph_pose_palette_bank_symbols: {morphPosePaletteBankSymbolCount}");
    Console.WriteLine($"normalized_ord_sibling_candidates: {normalizedSiblingCount}");
    Console.WriteLine($"complete_ord_orl_sibling_candidates: {completeSiblingCount}");
    Console.WriteLine($"valid_sibling_elfs: {validSiblingElfCount}");
    Console.WriteLine($"sibling_geometry_candidates: {siblingGeometryCount}");
    Console.WriteLine($"sibling_bounds_candidates: {siblingBoundsCount}");
    Console.WriteLine($"morph_without_normalized_sibling: {unmatchedNamingCount}");
    Console.WriteLine($"malformed_morph_entries: {malformedMorphCount}");
    Console.WriteLine($"relocation_signatures: {string.Join(" | ", relocationSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"relative_relocation_signatures: {string.Join(" | ", relativeRelocationSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"structural_layout_signatures_distinct: {structuralLayoutSignatures.Count}");
    Console.WriteLine($"structural_layout_signatures: {string.Join(" | ", structuralLayoutSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"companion_structural_layout_signatures_distinct: {companionStructuralLayoutSignatures.Count}");
    Console.WriteLine($"companion_structural_layout_signatures: {string.Join(" | ", companionStructuralLayoutSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_command_stream_signatures_distinct: {commandStreamSignatures.Count}");
    Console.WriteLine($"sibling_command_stream_signatures: {string.Join(" | ", commandStreamSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_vertex_descriptor_signatures_distinct: {vertexDescriptorSignatures.Count}");
    Console.WriteLine($"sibling_vertex_descriptor_signatures: {string.Join(" | ", vertexDescriptorSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_vertex_record_signatures_distinct: {vertexRecordSignatures.Count}");
    Console.WriteLine($"sibling_vertex_record_signatures: {string.Join(" | ", vertexRecordSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_numeric_descriptor_signatures_distinct: {numericDescriptorSignatures.Count}");
    Console.WriteLine($"sibling_numeric_descriptor_signatures: {string.Join(" | ", numericDescriptorSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_candidate_mesh_signatures_distinct: {candidateMeshSignatures.Count}");
    Console.WriteLine($"sibling_candidate_mesh_signatures: {string.Join(" | ", candidateMeshSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"sibling_companion_numeric_match_signatures_distinct: {companionNumericMatchSignatures.Count}");
    Console.WriteLine($"sibling_companion_numeric_match_signatures: {string.Join(" | ", companionNumericMatchSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"normalized_payload_hashes_distinct: {normalizedPayloadHashes.Count}");
    Console.WriteLine($"normalized_payload_hashes: {string.Join(" | ", normalizedPayloadHashes
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(12)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine($"normalized_payload_window_hash_signatures_distinct: {normalizedPayloadWindowHashSignatures.Count}");
    Console.WriteLine($"normalized_payload_window_hash_signatures: {string.Join(" | ", normalizedPayloadWindowHashSignatures
        .OrderByDescending(pair => pair.Value)
        .ThenBy(pair => pair.Key, StringComparer.Ordinal)
        .Take(4)
        .Select(pair => $"{pair.Value}x[{pair.Key}]"))}");
    Console.WriteLine("status: normalized filename sibling and ELF/bounds evidence only; no morph, rig, pose, weight, frame, or animation binding was assigned");
    Console.WriteLine("mode: ISO read-only, bounded RefPack decode, nested BIG, managed ELF inspection");
    return 0;
}

if (string.Equals(args[0], "eagl-position-cloud", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length is < 5 or > 6)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-position-cloud <caminho-do-iso> <arquivo.big> <nested.big> <objeto.ord> [saida.obj]");
        return 2;
    }

    using var cloudStore = new GameAssetStore(args[1]);
    using var compressedCloudStream = cloudStore.OpenBigEntry(args[2], args[3]);
    if (compressedCloudStream.Length > 64L * 1024 * 1024)
    {
        throw new InvalidDataException("Compressed nested model archive exceeds the 64 MiB safety limit.");
    }

    var compressedCloudData = new byte[checked((int)compressedCloudStream.Length)];
    compressedCloudStream.ReadExactly(compressedCloudData);
    var cloudExpandedSize = RefPackDecoder.ReadDeclaredSize(compressedCloudData);
    if (cloudExpandedSize > 64 * 1024 * 1024)
    {
        throw new InvalidDataException("Expanded nested model archive exceeds the 64 MiB safety limit.");
    }
    using var compressedCloudMemory = new MemoryStream(compressedCloudData, writable: false);
    var cloudExpanded = RefPackDecoder.Decode(compressedCloudMemory, cloudExpandedSize);
    var cloudArchive = BigArchiveReader.Inspect(cloudExpanded.Data);
    var cloudObjectEntry = cloudArchive.Entries.FirstOrDefault(entry =>
        string.Equals(entry.Name, args[4], StringComparison.OrdinalIgnoreCase));
    if (cloudObjectEntry is null || !string.Equals(Path.GetExtension(cloudObjectEntry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidDataException($"Split EAGL object was not found in the nested archive: {args[4]}");
    }

    var cloudLinkName = Path.ChangeExtension(cloudObjectEntry.Name, ".orl");
    var cloudLinkEntry = cloudArchive.Entries.FirstOrDefault(entry =>
        string.Equals(entry.Name, cloudLinkName, StringComparison.OrdinalIgnoreCase));
    if (cloudLinkEntry is null)
    {
        throw new InvalidDataException($"EAGL companion is missing: {cloudLinkName}");
    }

    var cloudObjectData = BigArchiveReader.ReadEntry(cloudExpanded.Data, cloudObjectEntry.Name);
    var cloudLinkData = BigArchiveReader.ReadEntry(cloudExpanded.Data, cloudLinkEntry.Name);
    var cloudInspection = EaglObjectReader.Inspect(cloudObjectData, cloudLinkData);
    if (cloudInspection.Bounds is null)
    {
        throw new InvalidDataException("The split EAGL object has no valid __BBOX to verify point candidates.");
    }

    var cloudOutputPath = args.Length == 6
        ? Path.GetFullPath(args[5])
        : Path.GetFullPath(Path.Combine(
            "exports_fifa_mesh_candidates",
            Path.GetFileNameWithoutExtension(cloudObjectEntry.Name) + "-points.obj"));
    var cloudOutputDirectory = Path.GetDirectoryName(cloudOutputPath);
    if (!string.IsNullOrEmpty(cloudOutputDirectory))
    {
        Directory.CreateDirectory(cloudOutputDirectory);
    }
    using var cloudOutputStream = new FileStream(cloudOutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    var cloudResult = EaglPositionCloudExporter.WriteObj(
        cloudOutputStream,
        cloudObjectData,
        cloudInspection.Elf,
        cloudInspection.Bounds);
    Console.WriteLine($"point_cloud_obj: {cloudOutputPath}");
    Console.WriteLine($"candidate_streams: {cloudResult.StreamCount}");
    Console.WriteLine($"point_vertices: {cloudResult.VertexCount}");
    Console.WriteLine($"streams: {string.Join(", ", cloudResult.StreamNames)}");
    Console.WriteLine("topology: point primitives only; no faces/triangles were inferred");
    return 0;
}

if (string.Equals(args[0], "eagl-strip-candidate-export", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length is < 5 or > 6)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc eagl-strip-candidate-export <caminho-do-iso> <arquivo.big> <nested.big> <objeto.ord> [saida.obj]");
        return 2;
    }

    using var stripStore = new GameAssetStore(args[1]);
    using var compressedStripStream = stripStore.OpenBigEntry(args[2], args[3]);
    if (compressedStripStream.Length > 64L * 1024 * 1024)
    {
        throw new InvalidDataException("Compressed nested model archive exceeds the 64 MiB safety limit.");
    }

    var compressedStripData = new byte[checked((int)compressedStripStream.Length)];
    compressedStripStream.ReadExactly(compressedStripData);
    var stripExpandedSize = RefPackDecoder.ReadDeclaredSize(compressedStripData);
    if (stripExpandedSize > 64 * 1024 * 1024)
    {
        throw new InvalidDataException("Expanded nested model archive exceeds the 64 MiB safety limit.");
    }
    using var compressedStripMemory = new MemoryStream(compressedStripData, writable: false);
    var stripExpanded = RefPackDecoder.Decode(compressedStripMemory, stripExpandedSize);
    var stripArchive = BigArchiveReader.Inspect(stripExpanded.Data);
    var stripObjectEntry = stripArchive.Entries.FirstOrDefault(entry =>
        string.Equals(entry.Name, args[4], StringComparison.OrdinalIgnoreCase));
    if (stripObjectEntry is null || !string.Equals(Path.GetExtension(stripObjectEntry.Name), ".ord", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidDataException($"Split EAGL object was not found in the nested archive: {args[4]}");
    }

    var stripLinkName = Path.ChangeExtension(stripObjectEntry.Name, ".orl");
    var stripLinkEntry = stripArchive.Entries.FirstOrDefault(entry =>
        string.Equals(entry.Name, stripLinkName, StringComparison.OrdinalIgnoreCase));
    if (stripLinkEntry is null)
    {
        throw new InvalidDataException($"EAGL companion is missing: {stripLinkName}");
    }

    var stripObjectData = BigArchiveReader.ReadEntry(stripExpanded.Data, stripObjectEntry.Name);
    var stripLinkData = BigArchiveReader.ReadEntry(stripExpanded.Data, stripLinkEntry.Name);
    var stripInspection = EaglObjectReader.Inspect(stripObjectData, stripLinkData);
    if (stripInspection.Bounds is null)
    {
        throw new InvalidDataException("The split EAGL object has no valid __BBOX for candidate filtering.");
    }

    var stripOutputPath = args.Length == 6
        ? Path.GetFullPath(args[5])
        : Path.GetFullPath(Path.Combine(
            "exports_fifa_mesh_candidates",
            Path.GetFileNameWithoutExtension(stripObjectEntry.Name) + "-strips.obj"));
    var stripOutputDirectory = Path.GetDirectoryName(stripOutputPath);
    if (!string.IsNullOrEmpty(stripOutputDirectory))
    {
        Directory.CreateDirectory(stripOutputDirectory);
    }
    using var stripOutputStream = new FileStream(stripOutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    var stripResult = EaglTriangleStripExporter.WriteObj(
        stripOutputStream,
        stripObjectData,
        stripInspection.Elf,
        stripInspection.Bounds);
    Console.WriteLine($"candidate_mesh_obj: {stripOutputPath}");
    Console.WriteLine($"explicit_unique_strip_groups: {stripResult.PrimitiveGroups}");
    Console.WriteLine($"skipped_primitive_groups: {stripResult.SkippedPrimitiveGroups}");
    Console.WriteLine($"vertices: {stripResult.VertexCount}");
    Console.WriteLine($"candidate_triangles: {stripResult.TriangleCount}");
    Console.WriteLine("validation: explicit primitive state + unique ordinal-matched VB + object bounds; draw buffer binding remains unverified");
    return 0;
}

if (string.Equals(args[0], "vivf-nested-asset-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5 || !int.TryParse(args[3], out var nestedAssetBlockIndex))
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-nested-asset-smoke <caminho-do-iso> <arquivo.bh> <índice> <entrada> [--hex|--words]");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var block = assetStore.OpenVivFBlock(args[2], nestedAssetBlockIndex);
    var decoded = RefPackDecoder.Decode(block.Data, checked((int)block.Record.ExpandedSize));
    var entryData = BigArchiveReader.ReadEntry(decoded.Data, args[4]);
    var extension = Path.GetExtension(args[4]);
    Console.WriteLine($"{GameIdentity.Name} PC nested asset smoke");
    Console.WriteLine($"package: {block.PackagePath}");
    Console.WriteLine($"block_index: {block.Index}");
    Console.WriteLine($"entry: {args[4]}");
    Console.WriteLine($"size: {entryData.Length}");
    if (args.Any(argument => string.Equals(argument, "--hex", StringComparison.OrdinalIgnoreCase)))
    {
        Console.WriteLine($"hex_prefix: {Convert.ToHexString(entryData.AsSpan(0, Math.Min(512, entryData.Length)))}");
    }
    if (args.Any(argument => string.Equals(argument, "--words", StringComparison.OrdinalIgnoreCase)))
    {
        for (var wordOffset = 0; wordOffset + sizeof(uint) <= entryData.Length && wordOffset < 512; wordOffset += sizeof(uint))
        {
            Console.WriteLine($"word_0x{wordOffset:X}: 0x{BinaryPrimitives.ReadUInt32LittleEndian(entryData.AsSpan(wordOffset, sizeof(uint))):X8}");
        }
    }

    switch (extension.ToLowerInvariant())
    {
        case ".xsh":
        {
            var shape = ShapeImagePackageLoader.Load(entryData);
            Console.WriteLine($"format: SHPX | version={shape.FormatVersion} | entries={shape.Entries.Count}");
            Console.WriteLine($"entry_ranges_valid: {shape.EntryRangesValid}");
            foreach (var entry in shape.Entries.Take(4))
            {
                Console.WriteLine($"shape_entry: {entry.Name} | offset={entry.Offset} | size={entry.Size}");
                Console.WriteLine($"shape_entry_header: {Convert.ToHexString(entryData.AsSpan(checked((int)entry.Offset), Math.Min(24, checked((int)entry.Size))))}");
            }
            break;
        }
        case ".apt":
        case ".const":
        {
            var apt = AptReader.Inspect(entryData);
            Console.WriteLine($"format: {apt.Format} | header_size={apt.HeaderSize}");
            Console.WriteLine($"fields_within_asset: {apt.FieldsWithinAsset}");
            for (var fieldIndex = 0; fieldIndex < apt.HeaderFields.Count; fieldIndex++)
            {
                Console.WriteLine($"apt_header_field_{fieldIndex}: {apt.HeaderFields[fieldIndex]}");
            }
            break;
        }
        case ".o":
        {
            var elf = ElfReader.Inspect(entryData);
            Console.WriteLine($"format: ELF32 | type={elf.Type} | machine={elf.Machine}");
            Console.WriteLine($"sections: {elf.SectionCount} | section_table_offset={elf.SectionHeaderOffset}");
            foreach (var section in elf.Sections)
            {
                Console.WriteLine($"elf_section: {section.Name} | type={section.Type} | flags=0x{section.Flags:X} | offset={section.Offset} | size={section.Size}");
            }
            foreach (var symbol in elf.Symbols.Where(symbol => !string.IsNullOrEmpty(symbol.Name)))
            {
                Console.WriteLine($"elf_symbol: {symbol.Name} | value=0x{symbol.Value:X} | size={symbol.Size} | section={symbol.SectionIndex}");
            }
            foreach (var relocation in elf.Relocations)
            {
                Console.WriteLine($"elf_relocation: {relocation.SectionName} | target_section={relocation.TargetSectionIndex} | offset=0x{relocation.Offset:X} | type={relocation.Type} | symbol={relocation.SymbolName}");
            }
            break;
        }
        case ".xbx":
        {
            var texture = TextureAssetLoader.Load(entryData);
            Console.WriteLine($"format: XPR0 | dimensions={texture.Width}x{texture.Height} | pixel_format={texture.PixelFormatName} | resource_size={texture.RawResource.Length}");
            Console.WriteLine($"header_word_24=0x{texture.HeaderWord24:X8} | decoded_rgba_bytes={texture.DecodedRgba.Length} | payload_first_bytes={Convert.ToHexString(texture.RawResource.AsSpan(0, Math.Min(16, texture.RawResource.Length)))}");
            break;
        }
        default:
            Console.WriteLine($"first_bytes: {Convert.ToHexString(entryData.AsSpan(0, Math.Min(16, entryData.Length)))}");
            break;
    }
    Console.WriteLine("mode: ISO read-only, RefPack decode, nested asset inspection");
    return 0;
}

if (string.Equals(args[0], "asset-format-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc asset-format-smoke <caminho-do-iso> <arquivo> [--verbose]");
        return 2;
    }

    using var assetStore = new GameAssetStore(args[1]);
    using var rawStream = assetStore.OpenRaw(args[2]);
    var extension = Path.GetExtension(args[2]).ToLowerInvariant();
    byte[] rawAsset;
    if (extension == ".ast")
    {
        // SCHl validation only needs the stream header. Do not materialize a
        // retail music stream that can be hundreds of megabytes long.
        var prefixLength = checked((int)Math.Min(rawStream.Length, 64 * 1024));
        rawAsset = new byte[prefixLength];
        var prefixRead = rawStream.Read(rawAsset, 0, rawAsset.Length);
        if (prefixRead != rawAsset.Length)
        {
            Array.Resize(ref rawAsset, prefixRead);
        }
    }
    else
    {
        if (rawStream.Length > 64 * 1024 * 1024)
        {
            Console.Error.WriteLine("Asset grande demais para o smoke test; use o acesso por blocos.");
            return 2;
        }
        using var memory = new MemoryStream();
        rawStream.CopyTo(memory);
        rawAsset = memory.ToArray();
    }
    Console.WriteLine($"{GameIdentity.Name} PC asset format smoke");
    Console.WriteLine($"path: {args[2]}");
    Console.WriteLine($"size: {rawAsset.Length}");

    switch (extension)
    {
        case ".xsh":
        {
            var shape = ShapeImagePackageLoader.Load(rawAsset);
            Console.WriteLine($"format: SHPX | version={shape.FormatVersion} | entries={shape.Entries.Count}");
            Console.WriteLine($"entry_ranges_valid: {shape.EntryRangesValid}");
            foreach (var entry in shape.Entries.Take(4))
            {
                Console.WriteLine($"shape_entry: {entry.Name} | offset={entry.Offset} | size={entry.Size}");
                Console.WriteLine($"shape_entry_header: {Convert.ToHexString(rawAsset.AsSpan(checked((int)entry.Offset), Math.Min(24, checked((int)entry.Size))))}");
            }
            break;
        }
        case ".xbx":
        {
            var texture = TextureAssetLoader.Load(rawAsset);
            Console.WriteLine($"format: XPR0 | dimensions={texture.Width}x{texture.Height} | pixel_format={texture.PixelFormatName} | resource_size={texture.RawResource.Length}");
            Console.WriteLine($"header_word_24=0x{texture.HeaderWord24:X8} | decoded_rgba_bytes={texture.DecodedRgba.Length} | payload_first_bytes={Convert.ToHexString(texture.RawResource.AsSpan(0, Math.Min(16, texture.RawResource.Length)))}");
            break;
        }
        case ".abk":
        {
            var bank = AudioAssetReader.InspectBank(rawAsset);
            Console.WriteLine($"format: ABKC | declared_size={bank.DeclaredSize} | header_fields_valid={bank.HeaderFieldsWithinAsset}");
            Console.WriteLine($"header_word_4=0x{bank.HeaderWord4:X8} | header_word_8=0x{bank.HeaderWord8:X8}");
            if (args.Length >= 4 && string.Equals(args[3], "--verbose", StringComparison.OrdinalIgnoreCase))
            {
                var probeEnd = Math.Min(rawAsset.Length & ~3, 0x100);
                for (var offset = 0; offset < probeEnd; offset += sizeof(uint))
                {
                    var value = BinaryPrimitives.ReadUInt32LittleEndian(rawAsset.AsSpan(offset, sizeof(uint)));
                    Console.WriteLine($"abk_probe_word_0x{offset:X2}=0x{value:X8} ({value})");
                }
            }
            break;
        }
        case ".ast":
        {
            var audio = AudioAssetReader.InspectStream(rawAsset);
            Console.WriteLine($"format: SCHl | first_chunk_size={audio.FirstChunkSize} | chunk_valid={audio.FirstChunkWithinAsset}");
            break;
        }
        default:
            Console.WriteLine($"first_bytes: {Convert.ToHexString(rawAsset.AsSpan(0, Math.Min(16, rawAsset.Length)))}");
            break;
    }
    Console.WriteLine("mode: ISO read-only, managed parser");
    return 0;
}

if (string.Equals(args[0], "audio-validate", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-validate <caminho-do-iso>");
        return 2;
    }

    using var validationFileSystem = new XdvdfsFileSystem(args[1]);
    var validationCatalog = GameAssetCatalog.Scan(validationFileSystem);
    var validatedCount = 0;
    var rejectedCount = 0;
    Console.WriteLine($"{GameIdentity.Name} PC full SCHl validation");
    Console.WriteLine("mode: ISO read-only, EA-XA block/table validation, no WAV allocation");
    foreach (var asset in validationCatalog.AudioStreams)
    {
        try
        {
            using var stream = validationFileSystem.OpenReadOnly(asset.Path);
            var result = EaSchlAudioDecoder.Validate(stream);
            validatedCount++;
            Console.WriteLine($"audio: {asset.Path} | bytes={asset.Size} | frames={result.SampleFrames} | pcm_bytes={result.DataBytes} | blocks={result.Blocks} | channels={result.Stream.Channels} | rate={result.Stream.SampleRate}");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            rejectedCount++;
            Console.WriteLine($"audio_invalid: {asset.Path} | {exception.Message}");
        }
    }

    Console.WriteLine($"validated_audio_streams: {validatedCount}");
    Console.WriteLine($"rejected_audio_streams: {rejectedCount}");
    return rejectedCount == 0 ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-candidate-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-candidate-smoke <caminho-do-iso> [--text]");
        return 2;
    }

    using var eventCandidateFileSystem = new XdvdfsFileSystem(args[1]);
    const string eventPath = "data/audio/eventspa.evt";
    const string headerOffPath = "data/audio/pcpahdr.off";
    const string headerBigPath = "data/audio/pcpahdr.big";
    const string dataOffPath = "data/audio/pcpadat.off";
    const string dataBigPath = "data/audio/pcpadat.big";
    if (!eventCandidateFileSystem.Enumerate("data/audio").ToHashSet(StringComparer.OrdinalIgnoreCase)
            .IsSupersetOf(new[] { eventPath, headerOffPath, headerBigPath, dataOffPath, dataBigPath }))
    {
        Console.Error.WriteLine("Os companions PA necessários não estão completos na ISO.");
        return 2;
    }

    using var eventStream = eventCandidateFileSystem.OpenReadOnly(eventPath);
    var eventData = ReadBounded(eventStream, 1 * 1024 * 1024);
    using var headerOffStream = eventCandidateFileSystem.OpenReadOnly(headerOffPath);
    using var headerLengthStream = eventCandidateFileSystem.OpenReadOnly(headerBigPath);
    var headerTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(headerOffStream, 1 * 1024 * 1024),
        headerLengthStream.Length);
    using var dataOffStream = eventCandidateFileSystem.OpenReadOnly(dataOffPath);
    using var dataLengthStream = eventCandidateFileSystem.OpenReadOnly(dataBigPath);
    var dataTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(dataOffStream, 1 * 1024 * 1024),
        dataLengthStream.Length);
    using var headerCompanion = eventCandidateFileSystem.OpenReadOnly(headerBigPath);
    var candidates = AudioAssetReader.CorrelateEventToHeaderRows(
        eventData,
        headerCompanion,
        headerTable,
        dataTable);
    using var dataCompanion = eventCandidateFileSystem.OpenReadOnly(dataBigPath);
    var dataEvidence = AudioAssetReader.InspectOffsetCompanion(dataCompanion, dataTable);
    Console.WriteLine($"{GameIdentity.Name} PC audio event/header candidate smoke");
    Console.WriteLine("mode: ISO read-only, exact aligned word match, no playback");
    Console.WriteLine($"event: {eventPath} | bytes={eventData.Length}");
    Console.WriteLine($"header_rows: {headerTable.Rows.Count}");
    Console.WriteLine($"data_rows: {dataTable.Rows.Count}");
    Console.WriteLine($"candidate_matches: {candidates.Count}");
    Console.WriteLine($"distinct_event_words: {candidates.Select(candidate => candidate.EventWord).Distinct().Count()}");
    Console.WriteLine($"distinct_header_rows: {candidates.Select(candidate => candidate.HeaderRowIndex).Distinct().Count()}");
    Console.WriteLine($"parallel_data_rows_available: {candidates.Count(candidate => candidate.ParallelDataRow is not null)}");
    Console.WriteLine($"parallel_data_schl_prefixes: {dataEvidence.SchlPrefixCount}/{dataEvidence.RangeCount}");
    var namedEventCandidates = AudioAssetReader.CorrelateEventTextStartsToHeaderRows(
        eventData,
        headerCompanion,
        headerTable,
        dataTable);
    var includeCandidateText = args.Any(argument => string.Equals(argument, "--text", StringComparison.OrdinalIgnoreCase));
    Console.WriteLine($"event_words_equal_text_run_start: {namedEventCandidates.Count}");
    foreach (var target in namedEventCandidates)
    {
        Console.WriteLine($"event_text_target: header_row={target.HeaderRowIndex} | value={target.EventName} | occurrences={target.EventOccurrenceCount} | data_row={(target.ParallelDataRow is null ? "unavailable" : target.HeaderRowIndex.ToString(CultureInfo.InvariantCulture))}");
    }
    var eventCandidateTextEvidence = AudioAssetReader.InspectEventFile(eventData);
    var eventCandidateStringTable = eventCandidateTextEvidence.TextRuns.FirstOrDefault(run =>
        run.Value.Contains("panna_goal", StringComparison.Ordinal));
    foreach (var candidate in candidates.Take(16))
    {
        var dataRow = candidate.ParallelDataRow;
        var textTarget = "none";
        if (includeCandidateText)
        {
            var containingRun = eventCandidateTextEvidence.TextRuns.FirstOrDefault(run =>
                eventCandidateStringTable is not null &&
                run.Offset >= eventCandidateStringTable.Offset &&
                candidate.EventWord >= (uint)run.Offset &&
                candidate.EventWord < (uint)(run.Offset + run.Value.Length));
            if (containingRun is not null)
            {
                var relative = checked((int)candidate.EventWord - containingRun.Offset);
                textTarget = $"{containingRun.Value}@+{relative}=>{containingRun.Value[relative..]}";
            }
        }
        Console.WriteLine(FormattableString.Invariant(
            $"candidate: event_offset=0x{candidate.EventWordOffset:X} | word=0x{candidate.EventWord:X8} | header_row={candidate.HeaderRowIndex} | header_range={candidate.HeaderRow.FirstColumn}:{candidate.HeaderRow.SecondColumn}:{candidate.HeaderRow.ThirdColumn} | data_range={(dataRow is null ? "unavailable" : $"{dataRow.FirstColumn}:{dataRow.SecondColumn}:{dataRow.ThirdColumn}")} | text_target={textTarget}"));
    }
    Console.WriteLine("status: exact header-word candidates plus optional internal-text evidence; event names and sample selection remain unverified");
    return 0;
}

if (string.Equals(args[0], "audio-event-text-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-text-smoke <caminho-do-iso> [bytes-de-contexto]");
        return 2;
    }

    var contextBytes = args.Length >= 3 && int.TryParse(args[2], out var requestedContext)
        ? Math.Clamp(requestedContext, 4, 128)
        : 32;
    using var eventTextFileSystem = new XdvdfsFileSystem(args[1]);
    const string eventTextPath = "data/audio/eventspa.evt";
    using var eventTextStream = eventTextFileSystem.OpenReadOnly(eventTextPath);
    var eventTextData = ReadBounded(eventTextStream, 1 * 1024 * 1024);
    var eventEvidence = AudioAssetReader.InspectEventFile(eventTextData);
    Console.WriteLine($"{GameIdentity.Name} PC event text context smoke");
    Console.WriteLine("mode: ISO read-only, bounded hex context, no event-name or sample binding");
    Console.WriteLine($"event: {eventTextPath} | bytes={eventEvidence.AssetLength} | text_runs={eventEvidence.TextRuns.Count}");
    foreach (var textRun in eventEvidence.TextRuns)
    {
        var beforeStart = Math.Max(0, textRun.Offset - contextBytes);
        var afterStart = Math.Min(eventTextData.Length, checked(textRun.Offset + textRun.Value.Length));
        var afterEnd = Math.Min(eventTextData.Length, afterStart + contextBytes);
        var before = Convert.ToHexString(eventTextData.AsSpan(beforeStart, textRun.Offset - beforeStart));
        var after = Convert.ToHexString(eventTextData.AsSpan(afterStart, afterEnd - afterStart));
        Console.WriteLine($"text_run: offset=0x{textRun.Offset:X} value={textRun.Value} | before=0x{before} | after=0x{after}");
    }
    Console.WriteLine("status: text and neighboring bytes exposed; event records, IDs and sample selection remain unverified");
    return 0;
}

if (string.Equals(args[0], "audio-event-index-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-index-smoke <caminho-do-iso>");
        return 2;
    }

    using var eventIndexFileSystem = new XdvdfsFileSystem(args[1]);
    const string eventIndexPath = "data/audio/eventspa.evt";
    using var eventIndexStream = eventIndexFileSystem.OpenReadOnly(eventIndexPath);
    var eventIndexData = ReadBounded(eventIndexStream, 1 * 1024 * 1024);
    var eventIndexEvidence = AudioAssetReader.InspectEventFile(eventIndexData);
    var tableRun = eventIndexEvidence.TextRuns.FirstOrDefault(run =>
        run.Value.Contains("panna_goal", StringComparison.Ordinal));
    if (tableRun is null)
    {
        Console.Error.WriteLine("A string table PA não foi localizada pela evidência textual.");
        return 2;
    }

    var stringIndex = AudioAssetReader.InspectStringIndex(eventIndexData, tableRun.Offset);
    var indexedWords = stringIndex
        .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
        .GroupBy(entry => entry.StringOffset)
        .Select(group => group.First())
        .OrderBy(entry => entry.StringOffset)
        .ToArray();
    var bankHits = new Dictionary<uint, List<(string Path, int Count, int FirstOffset)>>();
    foreach (var bankPath in eventIndexFileSystem
        .Enumerate("data/audio")
        .Where(path => path.EndsWith(".abk", StringComparison.OrdinalIgnoreCase)))
    {
        using var bankStream = eventIndexFileSystem.OpenReadOnly(bankPath);
        var bankData = ReadBounded(bankStream, 64 * 1024 * 1024);
        foreach (var indexedEntry in indexedWords)
        {
            var count = 0;
            var firstOffset = -1;
            for (var offset = 0; offset <= bankData.Length - sizeof(uint); offset += sizeof(uint))
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(bankData.AsSpan(offset, sizeof(uint))) != indexedEntry.HashWord)
                {
                    continue;
                }

                count++;
                firstOffset = firstOffset < 0 ? offset : firstOffset;
            }

            if (count > 0)
            {
                if (!bankHits.TryGetValue(indexedEntry.HashWord, out var hits))
                {
                    hits = new List<(string Path, int Count, int FirstOffset)>();
                    bankHits[indexedEntry.HashWord] = hits;
                }
                hits.Add((bankPath, count, firstOffset));
            }
        }
    }

    Console.WriteLine($"{GameIdentity.Name} PC event string-index smoke");
    Console.WriteLine("mode: ISO read-only, aligned relative-offset/word evidence, no ID or audio semantics");
    Console.WriteLine($"event: {eventIndexPath} | string_table_offset=0x{tableRun.Offset:X} | candidate_pairs={stringIndex.Count}");
    Console.WriteLine($"string_hash_family_candidates: {indexedWords.Length}");
    Console.WriteLine($"distinct_string_offsets: {indexedWords.Length}");
    Console.WriteLine($"distinct_following_words: {indexedWords.Select(entry => entry.HashWord).Distinct().Count()}");
    Console.WriteLine($"following_word_low16: {string.Join(",", indexedWords
        .GroupBy(entry => entry.HashWord & 0xFFFF)
        .OrderByDescending(group => group.Count())
        .Take(8)
        .Select(group => $"0x{group.Key:X4}x{group.Count()}"))}");
    foreach (var entry in indexedWords)
    {
        var hits = bankHits.TryGetValue(entry.HashWord, out var matchingBanks)
            ? string.Join(",", matchingBanks.Select(hit => $"{hit.Path}:count={hit.Count}:first=0x{hit.FirstOffset:X}"))
            : "none";
        Console.WriteLine($"string_index: record=0x{entry.RecordOffset:X} | relative=0x{entry.StringOffset:X} | word=0x{entry.HashWord:X8} | value={entry.Value} | abk_hits={hits}");
    }
    Console.WriteLine("status: observed 0x4F6B low-word family and ABK matches exposed as evidence; IDs, event semantics and sample selection remain unverified");
    return 0;
}

if (string.Equals(args[0], "audio-event-index-search", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-index-search <caminho-do-iso> [--data] [--report <saida.tsv>]");
        return 2;
    }

    var indexSearchIsoPath = Path.GetFullPath(args[1]);
    var indexSearchReportPath = Path.Combine(
        Directory.GetParent(indexSearchIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-index-evidence.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            indexSearchReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }
    if (string.Equals(indexSearchIsoPath, indexSearchReportPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório do índice de eventos não pode sobrescrever a ISO.");
        return 2;
    }

    using var indexSearchFileSystem = new XdvdfsFileSystem(indexSearchIsoPath);
    const string indexSearchEventPath = "data/audio/eventspa.evt";
    const string indexSearchHeaderPath = "data/audio/pcpahdr.big";
    const string indexSearchDataPath = "data/audio/pcpadat.big";
    using var indexSearchEventStream = indexSearchFileSystem.OpenReadOnly(indexSearchEventPath);
    var indexSearchEventData = ReadBounded(indexSearchEventStream, 1 * 1024 * 1024);
    var indexSearchEvidence = AudioAssetReader.InspectEventFile(indexSearchEventData);
    var indexSearchTableRun = indexSearchEvidence.TextRuns.FirstOrDefault(run =>
        run.Value.Contains("panna_goal", StringComparison.Ordinal));
    if (indexSearchTableRun is null)
    {
        Console.Error.WriteLine("A string table PA não foi localizada pela evidência textual.");
        return 2;
    }

    var indexSearchEntries = AudioAssetReader.InspectStringIndex(indexSearchEventData, indexSearchTableRun.Offset);
    var indexedWords = indexSearchEntries
        .Where(entry => (entry.HashWord & 0xFFFF) == 0x4F6B)
        .GroupBy(entry => entry.StringOffset)
        .Select(group => group.First())
        .OrderBy(entry => entry.StringOffset)
        .ToArray();
    var indexSearchValues = indexedWords
        .Select(entry => entry.HashWord)
        .ToHashSet();
    var searchPaths = new List<string> { indexSearchHeaderPath };
    var dataSearchRequested = args.Any(argument => string.Equals(argument, "--data", StringComparison.OrdinalIgnoreCase));
    if (dataSearchRequested)
    {
        searchPaths.Add(indexSearchDataPath);
    }

    Console.WriteLine($"{GameIdentity.Name} PC event string-index aligned-word search");
    Console.WriteLine("mode: ISO read-only, streaming little-endian word search, no event or sample semantics");
    Console.WriteLine($"event: {indexSearchEventPath} | string_table_offset=0x{indexSearchTableRun.Offset:X} | candidate_pairs={indexSearchEntries.Count} | distinct_hash_words={indexSearchValues.Count}");
    var alignedSearchSummaries = new Dictionary<string, (int TotalOccurrences, int ValuesWithHits)>(StringComparer.OrdinalIgnoreCase);
    foreach (var searchPath in searchPaths)
    {
        using var searchStream = indexSearchFileSystem.OpenReadOnly(searchPath);
        var matches = AudioAssetReader.SearchAlignedWords(searchStream, indexSearchValues);
        var totalOccurrences = matches.Sum(match => match.Count);
        var valuesWithHits = matches.Count(match => match.Count > 0);
        alignedSearchSummaries[searchPath] = (totalOccurrences, valuesWithHits);
        Console.WriteLine($"search: {searchPath} | bytes={searchStream.Length} | total_occurrences={totalOccurrences} | values_with_hits={valuesWithHits}");
        foreach (var match in matches.Where(match => match.Count > 0))
        {
            Console.WriteLine($"search_hit: path={searchPath} | word=0x{match.Value:X8} | count={match.Count} | offsets={string.Join(",", match.FirstOffsets.Select(offset => $"0x{offset:X}"))}");
        }
    }
    var abkAlignedOccurrences = 0;
    var abkValuesWithHits = 0;
    var abkBanksScanned = 0;
    foreach (var bankPath in indexSearchFileSystem
        .Enumerate("data/audio")
        .Where(path => path.EndsWith(".abk", StringComparison.OrdinalIgnoreCase)))
    {
        using var bankStream = indexSearchFileSystem.OpenReadOnly(bankPath);
        var matches = AudioAssetReader.SearchAlignedWords(bankStream, indexSearchValues);
        abkBanksScanned++;
        abkAlignedOccurrences += matches.Sum(match => match.Count);
        abkValuesWithHits += matches.Count(match => match.Count > 0);
    }

    var headerSummary = alignedSearchSummaries[indexSearchHeaderPath];
    var dataSummary = alignedSearchSummaries.TryGetValue(indexSearchDataPath, out var capturedDataSummary)
        ? capturedDataSummary
        : (TotalOccurrences: 0, ValuesWithHits: 0);
    var indexReport = new AudioEventIndexEvidenceReportData(
        EventPath: indexSearchEventPath,
        StringTableOffset: indexSearchTableRun.Offset,
        CandidatePairs: indexSearchEntries.Count,
        StringHashFamilyCandidates: indexedWords.Length,
        DistinctStringOffsets: indexedWords.Select(entry => entry.StringOffset).Distinct().Count(),
        DistinctFollowingWords: indexedWords.Select(entry => entry.HashWord).Distinct().Count(),
        AbkBanksScanned: abkBanksScanned,
        AbkAlignedOccurrences: abkAlignedOccurrences,
        AbkValuesWithHits: abkValuesWithHits,
        HeaderAlignedOccurrences: headerSummary.TotalOccurrences,
        HeaderValuesWithHits: headerSummary.ValuesWithHits,
        DataSearchRequested: dataSearchRequested,
        DataAlignedOccurrences: dataSummary.TotalOccurrences,
        DataValuesWithHits: dataSummary.ValuesWithHits,
        CandidateNames: string.Join(",", indexedWords.Select(entry => entry.Value)),
        Status: "no-direct-selection-binding",
        DirectSelectionBinding: "not-proven",
        OriginalSemantics: "not-claimed");
    AudioEventIndexEvidenceReport.Write(indexSearchReportPath, indexReport);
    Console.WriteLine($"report: {Path.GetFullPath(indexSearchReportPath)}");
    Console.WriteLine($"abk_scan: banks={abkBanksScanned} | total_occurrences={abkAlignedOccurrences} | values_with_hits={abkValuesWithHits}");
    Console.WriteLine("status: raw aligned-word hits are evidence only; no event-name, row, ABKC or playback binding was created");
    return 0;
}

if (string.Equals(args[0], "audio-event-audio-asset-hash-universe-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-audio-asset-hash-universe-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var audioAssetHashIsoPath = Path.GetFullPath(args[1]);
    var audioAssetHashReportPath = Path.Combine(
        Directory.GetParent(audioAssetHashIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-audio-asset-hash-universe.tsv");
    var audioAssetHashSummaryPath = Path.Combine(
        Directory.GetParent(audioAssetHashIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-audio-asset-hash-universe-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            audioAssetHashReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            audioAssetHashSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }
    if (string.Equals(audioAssetHashIsoPath, audioAssetHashReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(audioAssetHashIsoPath, audioAssetHashSummaryPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(audioAssetHashReportPath, audioAssetHashSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios do universo de hashes não podem sobrescrever a ISO nem um ao outro.");
        return 2;
    }

    using var audioAssetHashFileSystem = new XdvdfsFileSystem(audioAssetHashIsoPath);
    var audioAssetHashResult = AudioEventAudioAssetHashSearchAudit.Audit(audioAssetHashFileSystem);
    AudioEventAudioAssetHashSearchAudit.WriteReport(audioAssetHashReportPath, audioAssetHashResult.Rows);
    AudioEventAudioAssetHashSearchAudit.WriteSummary(audioAssetHashSummaryPath, audioAssetHashResult.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC EVT hash words in complete audio asset universe");
    Console.WriteLine("mode: ISO read-only, aligned little-endian words, source EVT excluded; no selection or playback binding");
    Console.WriteLine($"event_path: {audioAssetHashResult.Summary.EventPath}");
    Console.WriteLine($"candidate_count: {audioAssetHashResult.Summary.CandidateCount}");
    Console.WriteLine($"scanned_asset_count: {audioAssetHashResult.Summary.ScannedAssetCount}");
    Console.WriteLine($"valid_asset_count: {audioAssetHashResult.Summary.ValidAssetCount}");
    Console.WriteLine($"scanned_bytes: {audioAssetHashResult.Summary.ScannedBytes}");
    Console.WriteLine($"excluded_source_rows: {audioAssetHashResult.Summary.ExcludedSourceRows}");
    Console.WriteLine($"assets_with_hits: {audioAssetHashResult.Summary.AssetsWithHits}");
    Console.WriteLine($"asset_hash_hit_pairs: {audioAssetHashResult.Summary.AssetHashHitPairs}");
    Console.WriteLine($"aligned_occurrences: {audioAssetHashResult.Summary.AlignedOccurrences}");
    Console.WriteLine($"status: {audioAssetHashResult.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(audioAssetHashReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(audioAssetHashSummaryPath)}");
    Console.WriteLine($"direct_selection_binding: {audioAssetHashResult.Summary.DirectSelectionBinding}");
    Console.WriteLine($"original_semantics: {audioAssetHashResult.Summary.OriginalSemantics}");
    return audioAssetHashResult.Summary.Status == AudioEventAudioAssetHashSearchAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-data-asset-hash-universe-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-data-asset-hash-universe-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var dataAssetHashIsoPath = Path.GetFullPath(args[1]);
    var dataAssetHashReportPath = Path.Combine(
        Directory.GetParent(dataAssetHashIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-data-asset-hash-universe.tsv");
    var dataAssetHashSummaryPath = Path.Combine(
        Directory.GetParent(dataAssetHashIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-data-asset-hash-universe-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            dataAssetHashReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            dataAssetHashSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }
    if (string.Equals(dataAssetHashIsoPath, dataAssetHashReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dataAssetHashIsoPath, dataAssetHashSummaryPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(dataAssetHashReportPath, dataAssetHashSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios do universo de dados não podem sobrescrever a ISO nem um ao outro.");
        return 2;
    }

    using var dataAssetHashFileSystem = new XdvdfsFileSystem(dataAssetHashIsoPath);
    var dataAssetHashResult = AudioEventDataAssetHashSearchAudit.Audit(dataAssetHashFileSystem);
    AudioEventDataAssetHashSearchAudit.WriteReport(dataAssetHashReportPath, dataAssetHashResult.Rows);
    AudioEventDataAssetHashSearchAudit.WriteSummary(dataAssetHashSummaryPath, dataAssetHashResult.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC EVT hash words in complete data asset universe");
    Console.WriteLine("mode: ISO read-only, aligned little-endian words, source EVT excluded; no selection or playback binding");
    Console.WriteLine($"asset_root: {dataAssetHashResult.Summary.AssetRoot}");
    Console.WriteLine($"event_path: {dataAssetHashResult.Summary.EventPath}");
    Console.WriteLine($"candidate_count: {dataAssetHashResult.Summary.CandidateCount}");
    Console.WriteLine($"scanned_asset_count: {dataAssetHashResult.Summary.ScannedAssetCount}");
    Console.WriteLine($"valid_asset_count: {dataAssetHashResult.Summary.ValidAssetCount}");
    Console.WriteLine($"scanned_bytes: {dataAssetHashResult.Summary.ScannedBytes}");
    Console.WriteLine($"excluded_source_rows: {dataAssetHashResult.Summary.ExcludedSourceRows}");
    Console.WriteLine($"assets_with_hits: {dataAssetHashResult.Summary.AssetsWithHits}");
    Console.WriteLine($"asset_hash_hit_pairs: {dataAssetHashResult.Summary.AssetHashHitPairs}");
    Console.WriteLine($"aligned_occurrences: {dataAssetHashResult.Summary.AlignedOccurrences}");
    Console.WriteLine($"status: {dataAssetHashResult.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(dataAssetHashReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(dataAssetHashSummaryPath)}");
    Console.WriteLine($"direct_selection_binding: {dataAssetHashResult.Summary.DirectSelectionBinding}");
    Console.WriteLine($"original_semantics: {dataAssetHashResult.Summary.OriginalSemantics}");
    return dataAssetHashResult.Summary.Status == AudioEventDataAssetHashSearchAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-name-asset-text-search-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-name-asset-text-search-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var eventNameTextIsoPath = Path.GetFullPath(args[1]);
    var eventNameTextReportPath = Path.Combine(
        Directory.GetParent(eventNameTextIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-name-asset-text-search.tsv");
    var eventNameTextSummaryPath = Path.Combine(
        Directory.GetParent(eventNameTextIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "audio-event-name-asset-text-search-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            eventNameTextReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            eventNameTextSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    if (string.Equals(eventNameTextIsoPath, eventNameTextReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(eventNameTextIsoPath, eventNameTextSummaryPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(eventNameTextReportPath, eventNameTextSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios de nomes EVT não podem sobrescrever a ISO nem um ao outro.");
        return 2;
    }

    using var eventNameTextFileSystem = new XdvdfsFileSystem(eventNameTextIsoPath);
    var eventNameTextResult = AudioEventNameAssetTextSearchAudit.Audit(eventNameTextFileSystem);
    AudioEventNameAssetTextSearchAudit.WriteReport(eventNameTextReportPath, eventNameTextResult.Rows);
    AudioEventNameAssetTextSearchAudit.WriteSummary(eventNameTextSummaryPath, eventNameTextResult.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC named EVT text in complete audio asset universe");
    Console.WriteLine("mode: ISO read-only, streaming exact ASCII-name search; no sample or playback binding");
    Console.WriteLine($"event_path: {eventNameTextResult.Summary.EventPath}");
    Console.WriteLine($"candidate_count: {eventNameTextResult.Summary.CandidateCount}");
    Console.WriteLine($"scanned_asset_count: {eventNameTextResult.Summary.ScannedAssetCount}");
    Console.WriteLine($"valid_asset_count: {eventNameTextResult.Summary.ValidAssetCount}");
    Console.WriteLine($"scanned_bytes: {eventNameTextResult.Summary.ScannedBytes}");
    Console.WriteLine($"excluded_source_rows: {eventNameTextResult.Summary.ExcludedSourceRows}");
    Console.WriteLine($"asset_name_hit_pairs: {eventNameTextResult.Summary.AssetNameHitPairs}");
    Console.WriteLine($"assets_with_name_hits: {eventNameTextResult.Summary.AssetsWithNameHits}");
    Console.WriteLine($"raw_occurrences: {eventNameTextResult.Summary.RawOccurrences}");
    Console.WriteLine($"status: {eventNameTextResult.Summary.Status}");
    Console.WriteLine($"report: {Path.GetFullPath(eventNameTextReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(eventNameTextSummaryPath)}");
    Console.WriteLine($"direct_selection_binding: {eventNameTextResult.Summary.DirectSelectionBinding}");
    Console.WriteLine($"original_semantics: {eventNameTextResult.Summary.OriginalSemantics}");
    return eventNameTextResult.Summary.Status == AudioEventNameAssetTextSearchAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "fifast2-csi-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc fifast2-csi-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var csiAuditIsoPath = Path.GetFullPath(args[1]);
    var csiAuditReportPath = Path.Combine(
        Directory.GetParent(csiAuditIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-name-index.tsv");
    var csiAuditSummaryPath = Path.Combine(
        Directory.GetParent(csiAuditIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-name-index-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            csiAuditReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            csiAuditSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    if (string.Equals(csiAuditIsoPath, csiAuditReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(csiAuditIsoPath, csiAuditSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios CSI não podem sobrescrever a ISO.");
        return 2;
    }

    using var csiAuditFileSystem = new XdvdfsFileSystem(csiAuditIsoPath);
    const string csiPath = "data/audio/FIFAST2.csi";
    const string csiEventPath = "data/audio/eventspa.evt";
    using var csiStream = csiAuditFileSystem.OpenReadOnly(csiPath);
    var csiData = ReadBounded(csiStream, 16 * 1024 * 1024);
    using var csiEventStream = csiAuditFileSystem.OpenReadOnly(csiEventPath);
    var csiEventData = ReadBounded(csiEventStream, 1 * 1024 * 1024);
    var csiEventEvidence = AudioAssetReader.InspectEventFile(csiEventData);
    var csiEventTableRun = csiEventEvidence.TextRuns.FirstOrDefault(run =>
        run.Value.Contains("panna_goal", StringComparison.Ordinal));
    var csiEventNames = csiEventTableRun is null
        ? Array.Empty<string>()
        : csiEventEvidence.TextRuns
            .Where(run => run.Offset >= csiEventTableRun.Offset)
            .Select(run => run.Value)
            .ToArray();
    var csiAudit = Fifast2CsiReader.Audit(csiData, csiEventNames);
    Fifast2CsiReader.WriteReport(csiAuditReportPath, csiAudit);
    Fifast2CsiReader.WriteSummary(csiAuditSummaryPath, csiAudit.Summary);

    Console.WriteLine($"{GameIdentity.Name} PC FIFAST2.csi structural audit");
    Console.WriteLine("mode: ISO read-only, MOIR name-index candidate only; opaque words and semantics preserved");
    Console.WriteLine($"asset: {csiPath} | bytes={csiAudit.Summary.AssetLength} | magic={csiAudit.Summary.Magic}");
    Console.WriteLine($"records: candidate={csiAudit.Summary.CandidateRecordCount} | valid={csiAudit.Summary.ValidRecordCount} | unique_names={csiAudit.Summary.UniqueNameCount}");
    Console.WriteLine($"opaque_words: within_asset={csiAudit.Summary.OpaqueWordsWithinAsset} | reserved_zero={csiAudit.Summary.ReservedZeroCount} | non_decreasing_pairs={csiAudit.Summary.OpaqueWordsNonDecreasingPairs}");
    Console.WriteLine($"event_text_overlap: names={csiAudit.Summary.EventTextNameCount} | exact={csiAudit.Summary.EventExactNameOverlapCount} | substring={csiAudit.Summary.EventSubstringOverlapCount}");
    Console.WriteLine($"status: {csiAudit.Summary.Status}");
    Console.WriteLine($"direct_selection_binding: {csiAudit.Summary.DirectSelectionBinding} | original_semantics: {csiAudit.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {Path.GetFullPath(csiAuditReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(csiAuditSummaryPath)}");
    return csiAudit.Summary.Status == "verified-structural-fifast2-csi-name-index-candidate" ? 0 : 1;
}

if (string.Equals(args[0], "fifast2-csi-tuna-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc fifast2-csi-tuna-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var csiTunaAuditIsoPath = Path.GetFullPath(args[1]);
    var csiTunaAuditReportPath = Path.Combine(
        Directory.GetParent(csiTunaAuditIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-tuna-hash-crosscheck.tsv");
    var csiTunaAuditSummaryPath = Path.Combine(
        Directory.GetParent(csiTunaAuditIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-tuna-hash-crosscheck-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            csiTunaAuditReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            csiTunaAuditSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    if (string.Equals(csiTunaAuditIsoPath, csiTunaAuditReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(csiTunaAuditIsoPath, csiTunaAuditSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios CSI/tuna não podem sobrescrever a ISO.");
        return 2;
    }

    using var csiTunaAuditFileSystem = new XdvdfsFileSystem(csiTunaAuditIsoPath);
    using var csiTunaArchiveStream = csiTunaAuditFileSystem.OpenReadOnly("data/audio/tuna.big");
    using var csiTunaCsiStream = csiTunaAuditFileSystem.OpenReadOnly("data/audio/FIFAST2.csi");
    using var csiTunaPcpahdrStream = csiTunaAuditFileSystem.OpenReadOnly("data/audio/pcpahdr.big");
    var csiTunaAudit = Fifast2CsiTunaAudit.Analyze(
        ReadBounded(csiTunaArchiveStream, 16 * 1024 * 1024),
        ReadBounded(csiTunaCsiStream, 16 * 1024 * 1024),
        ReadBounded(csiTunaPcpahdrStream, 16 * 1024 * 1024));
    Fifast2CsiTunaAudit.WriteReport(csiTunaAuditReportPath, csiTunaAudit.Rows);
    Fifast2CsiTunaAuditSummary.Write(csiTunaAuditSummaryPath, csiTunaAudit.Summary);

    Console.WriteLine($"{GameIdentity.Name} PC FIFAST2.csi/tuna structural hash cross-check");
    Console.WriteLine("mode: ISO read-only; named hash namespace cross-check only; CSI opaque words remain undecoded");
    Console.WriteLine($"tuna: magic={csiTunaAudit.Summary.TunaArchiveMagic} | entry={csiTunaAudit.Summary.TunaEntryName} | rows={csiTunaAudit.Summary.TunaRowCount} | hash_matches={csiTunaAudit.Summary.TunaHashMatchCount}");
    Console.WriteLine($"csi_namespace: records={csiTunaAudit.Summary.CsiRecordCount} | valid={csiTunaAudit.Summary.CsiValidRecordCount} | tuna_name_overlap={csiTunaAudit.Summary.CsiNameOverlapCount}");
    Console.WriteLine($"opaque_words: tuna_hash_matches={csiTunaAudit.Summary.CsiOpaqueHashMatchCount} | within_pcpahdr_asset={csiTunaAudit.Summary.CsiOpaqueWordsWithinPcpahdrAsset} | semantics={csiTunaAudit.Summary.OpaqueWordSemantics}");
    Console.WriteLine($"hash_function_candidate: {csiTunaAudit.Summary.HashFunctionCandidate}");
    Console.WriteLine($"status: {csiTunaAudit.Summary.Status}");
    Console.WriteLine($"direct_selection_binding: {csiTunaAudit.Summary.DirectSelectionBinding} | original_semantics: {csiTunaAudit.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {Path.GetFullPath(csiTunaAuditReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(csiTunaAuditSummaryPath)}");
    return csiTunaAudit.Summary.Status == Fifast2CsiTunaAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "fifast2-csi-opaque-range-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc fifast2-csi-opaque-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var csiOpaqueRangeIsoPath = Path.GetFullPath(args[1]);
    var csiOpaqueRangeReportPath = Path.Combine(
        Directory.GetParent(csiOpaqueRangeIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-opaque-range-audit.tsv");
    var csiOpaqueRangeSummaryPath = Path.Combine(
        Directory.GetParent(csiOpaqueRangeIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-opaque-range-audit-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            csiOpaqueRangeReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            csiOpaqueRangeSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    if (string.Equals(csiOpaqueRangeIsoPath, csiOpaqueRangeReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(csiOpaqueRangeIsoPath, csiOpaqueRangeSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório CSI/range não pode sobrescrever a ISO.");
        return 2;
    }

    using var csiOpaqueRangeFileSystem = new XdvdfsFileSystem(csiOpaqueRangeIsoPath);
    using var csiOpaqueRangeCsiStream = csiOpaqueRangeFileSystem.OpenReadOnly("data/audio/FIFAST2.csi");
    using var csiOpaqueRangeHeaderOffStream = csiOpaqueRangeFileSystem.OpenReadOnly("data/audio/pcpahdr.off");
    using var csiOpaqueRangeHeaderBigLengthStream = csiOpaqueRangeFileSystem.OpenReadOnly("data/audio/pcpahdr.big");
    using var csiOpaqueRangeDataOffStream = csiOpaqueRangeFileSystem.OpenReadOnly("data/audio/pcpadat.off");
    using var csiOpaqueRangeDataBigLengthStream = csiOpaqueRangeFileSystem.OpenReadOnly("data/audio/pcpadat.big");
    var csiOpaqueRangeAudit = Fifast2CsiOpaqueRangeAudit.Analyze(
        ReadBounded(csiOpaqueRangeCsiStream, 16 * 1024 * 1024),
        ReadBounded(csiOpaqueRangeHeaderOffStream, 16 * 1024 * 1024),
        csiOpaqueRangeHeaderBigLengthStream.Length,
        ReadBounded(csiOpaqueRangeDataOffStream, 16 * 1024 * 1024),
        csiOpaqueRangeDataBigLengthStream.Length);
    Fifast2CsiOpaqueRangeAudit.WriteReport(csiOpaqueRangeReportPath, csiOpaqueRangeAudit.Rows);
    Fifast2CsiOpaqueRangeAuditSummary.Write(csiOpaqueRangeSummaryPath, csiOpaqueRangeAudit.Summary);

    Console.WriteLine($"{GameIdentity.Name} PC FIFAST2.csi opaque range/boundary control");
    Console.WriteLine("mode: ISO read-only; .off range comparison only; opaque words remain undecoded");
    Console.WriteLine($"tables: pcpahdr_rows={csiOpaqueRangeAudit.Summary.PcpahdrRowCount} | pcpahdr_partition={csiOpaqueRangeAudit.Summary.PcpahdrPartitionCoverage} | pcpadat_rows={csiOpaqueRangeAudit.Summary.PcpadatRowCount} | pcpadat_partition={csiOpaqueRangeAudit.Summary.PcpadatPartitionCoverage}");
    Console.WriteLine($"pcpahdr: within={csiOpaqueRangeAudit.Summary.CsiOpaqueWithinPcpahdrCount} | exact_start={csiOpaqueRangeAudit.Summary.CsiOpaquePcpahdrExactStartCount} | exact_end={csiOpaqueRangeAudit.Summary.CsiOpaquePcpahdrExactEndCount} | containing_rows={csiOpaqueRangeAudit.Summary.DistinctPcpahdrContainingRows}");
    Console.WriteLine($"pcpadat: within={csiOpaqueRangeAudit.Summary.CsiOpaqueWithinPcpadatCount} | exact_start={csiOpaqueRangeAudit.Summary.CsiOpaquePcpadatExactStartCount} | exact_end={csiOpaqueRangeAudit.Summary.CsiOpaquePcpadatExactEndCount} | containing_rows={csiOpaqueRangeAudit.Summary.DistinctPcpadatContainingRows}");
    Console.WriteLine($"status: {csiOpaqueRangeAudit.Summary.Status}");
    Console.WriteLine($"opaque_word_semantics: {csiOpaqueRangeAudit.Summary.OpaqueWordSemantics} | direct_selection_binding: {csiOpaqueRangeAudit.Summary.DirectSelectionBinding} | original_semantics: {csiOpaqueRangeAudit.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {Path.GetFullPath(csiOpaqueRangeReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(csiOpaqueRangeSummaryPath)}");
    return csiOpaqueRangeAudit.Summary.Status == Fifast2CsiOpaqueRangeAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "fifast2-csi-opaque-field-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc fifast2-csi-opaque-field-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var csiOpaqueFieldIsoPath = Path.GetFullPath(args[1]);
    var csiOpaqueFieldReportPath = Path.Combine(
        Directory.GetParent(csiOpaqueFieldIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-opaque-field-audit.tsv");
    var csiOpaqueFieldSummaryPath = Path.Combine(
        Directory.GetParent(csiOpaqueFieldIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "fifast2-csi-opaque-field-audit-summary.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            csiOpaqueFieldReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            csiOpaqueFieldSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
    }

    if (string.Equals(csiOpaqueFieldIsoPath, csiOpaqueFieldReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(csiOpaqueFieldIsoPath, csiOpaqueFieldSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório CSI/campo não pode sobrescrever a ISO.");
        return 2;
    }

    using var csiOpaqueFieldFileSystem = new XdvdfsFileSystem(csiOpaqueFieldIsoPath);
    using var csiOpaqueFieldCsiStream = csiOpaqueFieldFileSystem.OpenReadOnly("data/audio/FIFAST2.csi");
    using var csiOpaqueFieldHeaderOffStream = csiOpaqueFieldFileSystem.OpenReadOnly("data/audio/pcpahdr.off");
    using var csiOpaqueFieldHeaderBigLengthStream = csiOpaqueFieldFileSystem.OpenReadOnly("data/audio/pcpahdr.big");
    using var csiOpaqueFieldDataOffStream = csiOpaqueFieldFileSystem.OpenReadOnly("data/audio/pcpadat.off");
    using var csiOpaqueFieldDataBigLengthStream = csiOpaqueFieldFileSystem.OpenReadOnly("data/audio/pcpadat.big");
    var csiOpaqueFieldAudit = Fifast2CsiOpaqueFieldAudit.Analyze(
        ReadBounded(csiOpaqueFieldCsiStream, 16 * 1024 * 1024),
        ReadBounded(csiOpaqueFieldHeaderOffStream, 16 * 1024 * 1024),
        csiOpaqueFieldHeaderBigLengthStream.Length,
        ReadBounded(csiOpaqueFieldDataOffStream, 16 * 1024 * 1024),
        csiOpaqueFieldDataBigLengthStream.Length);
    Fifast2CsiOpaqueFieldAudit.WriteReport(csiOpaqueFieldReportPath, csiOpaqueFieldAudit.Rows);
    Fifast2CsiOpaqueFieldAuditSummary.Write(csiOpaqueFieldSummaryPath, csiOpaqueFieldAudit.Summary);

    Console.WriteLine($"{GameIdentity.Name} PC FIFAST2.csi opaque off-column/row-ordinal control");
    Console.WriteLine("mode: ISO read-only; positional .off column equality only; opaque words remain undecoded");
    Console.WriteLine($"tables: pcpahdr_rows={csiOpaqueFieldAudit.Summary.PcpahdrRowCount} | pcpahdr_partition={csiOpaqueFieldAudit.Summary.PcpahdrPartitionCoverage} | pcpadat_rows={csiOpaqueFieldAudit.Summary.PcpadatRowCount} | pcpadat_partition={csiOpaqueFieldAudit.Summary.PcpadatPartitionCoverage}");
    Console.WriteLine($"pcpahdr: any_column={csiOpaqueFieldAudit.Summary.PcpahdrAnyColumnMatchCount} | column1={csiOpaqueFieldAudit.Summary.PcpahdrColumn1MatchCount} | column2={csiOpaqueFieldAudit.Summary.PcpahdrColumn2MatchCount} | column3={csiOpaqueFieldAudit.Summary.PcpahdrColumn3MatchCount} | row_ordinal={csiOpaqueFieldAudit.Summary.PcpahdrRowOrdinalMatchCount}");
    Console.WriteLine($"pcpadat: any_column={csiOpaqueFieldAudit.Summary.PcpadatAnyColumnMatchCount} | column1={csiOpaqueFieldAudit.Summary.PcpadatColumn1MatchCount} | column2={csiOpaqueFieldAudit.Summary.PcpadatColumn2MatchCount} | column3={csiOpaqueFieldAudit.Summary.PcpadatColumn3MatchCount} | row_ordinal={csiOpaqueFieldAudit.Summary.PcpadatRowOrdinalMatchCount}");
    Console.WriteLine($"status: {csiOpaqueFieldAudit.Summary.Status}");
    Console.WriteLine($"opaque_word_semantics: {csiOpaqueFieldAudit.Summary.OpaqueWordSemantics} | direct_selection_binding: {csiOpaqueFieldAudit.Summary.DirectSelectionBinding} | original_semantics: {csiOpaqueFieldAudit.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {Path.GetFullPath(csiOpaqueFieldReportPath)}");
    Console.WriteLine($"summary: {Path.GetFullPath(csiOpaqueFieldSummaryPath)}");
    return csiOpaqueFieldAudit.Summary.Status == Fifast2CsiOpaqueFieldAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-named-candidates", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-named-candidates <caminho-do-iso>");
        return 2;
    }

    using var namedCandidateFileSystem = new XdvdfsFileSystem(args[1]);
    const string namedEventPath = "data/audio/eventspa.evt";
    const string namedHeaderOffPath = "data/audio/pcpahdr.off";
    const string namedHeaderBigPath = "data/audio/pcpahdr.big";
    const string namedDataOffPath = "data/audio/pcpadat.off";
    const string namedDataBigPath = "data/audio/pcpadat.big";
    using var namedEventStream = namedCandidateFileSystem.OpenReadOnly(namedEventPath);
    var namedEventData = ReadBounded(namedEventStream, 1 * 1024 * 1024);
    using var namedHeaderOffStream = namedCandidateFileSystem.OpenReadOnly(namedHeaderOffPath);
    using var namedHeaderBigLengthStream = namedCandidateFileSystem.OpenReadOnly(namedHeaderBigPath);
    var namedHeaderTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(namedHeaderOffStream, 1 * 1024 * 1024),
        namedHeaderBigLengthStream.Length);
    using var namedDataOffStream = namedCandidateFileSystem.OpenReadOnly(namedDataOffPath);
    using var namedDataBigLengthStream = namedCandidateFileSystem.OpenReadOnly(namedDataBigPath);
    var namedDataTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(namedDataOffStream, 1 * 1024 * 1024),
        namedDataBigLengthStream.Length);
    using var namedHeaderCompanion = namedCandidateFileSystem.OpenReadOnly(namedHeaderBigPath);
    var namedCandidates = AudioAssetReader.CorrelateEventTextStartsToHeaderRows(
        namedEventData,
        namedHeaderCompanion,
        namedHeaderTable,
        namedDataTable);
    using var namedDataCompanion = namedCandidateFileSystem.OpenReadOnly(namedDataBigPath);
    var namedValidated = 0;
    var namedRejected = 0;
    Console.WriteLine($"{GameIdentity.Name} PC named event/header audio candidates");
    Console.WriteLine("mode: ISO read-only, EVT string-table pointer evidence, SCHl header inspection, no playback");
    Console.WriteLine($"named_candidates: {namedCandidates.Count}");
    foreach (var candidate in namedCandidates)
    {
        var dataRow = candidate.ParallelDataRow;
        if (dataRow is null || dataRow.SecondColumn < 8)
        {
            namedRejected++;
            Console.WriteLine($"named_candidate: name={candidate.EventName} | header_row={candidate.HeaderRowIndex} | data=unavailable | status=rejected");
            continue;
        }

        try
        {
            using var range = new AudioRangeStream(
                namedDataCompanion,
                dataRow.FirstColumn,
                dataRow.SecondColumn);
            var info = EaSchlAudioDecoder.Inspect(range);
            namedValidated++;
            Console.WriteLine(FormattableString.Invariant(
                $"named_candidate: name={candidate.EventName} | header_row={candidate.HeaderRowIndex} | occurrences={candidate.EventOccurrenceCount} | data_offset={dataRow.FirstColumn} | data_bytes={dataRow.SecondColumn} | codec={info.Codec} | channels={info.Channels} | rate={info.SampleRate} | frames={info.DeclaredSamples} | status=validated"));
        }
        catch (Exception exception) when (exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            namedRejected++;
            Console.WriteLine($"named_candidate: name={candidate.EventName} | header_row={candidate.HeaderRowIndex} | data_offset={dataRow.FirstColumn} | data_bytes={dataRow.SecondColumn} | status=rejected | reason={exception.Message}");
        }
    }
    Console.WriteLine($"named_validated: {namedValidated}");
    Console.WriteLine($"named_rejected: {namedRejected}");
    Console.WriteLine("status: names remain explicit EVT candidates; no automatic gameplay or ABKC binding was created");
    return namedRejected == 0 ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-payload-wave-correlation-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-event-payload-wave-correlation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--max-bank-attempts <n>]");
        return 2;
    }

    var audioWaveCorrelationIsoPath = Path.GetFullPath(args[1]);
    var audioWaveCorrelationReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-payload-wave-correlation.tsv");
    var audioWaveCorrelationSummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-payload-wave-correlation-summary.tsv");
    var audioWaveCorrelationMaximumBankAttempts = AudioEventPayloadWaveCorrelationAudit.DefaultMaximumBankAttempts;
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            audioWaveCorrelationReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            audioWaveCorrelationSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
        }
        else if (string.Equals(args[argumentIndex], "--max-bank-attempts", StringComparison.OrdinalIgnoreCase))
        {
            if (!int.TryParse(
                    args[argumentIndex + 1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out audioWaveCorrelationMaximumBankAttempts) ||
                audioWaveCorrelationMaximumBankAttempts <= 0)
            {
                Console.Error.WriteLine("--max-bank-attempts requer um inteiro positivo.");
                return 2;
            }
        }
    }

    using var audioWaveCorrelationFileSystem = new XdvdfsFileSystem(audioWaveCorrelationIsoPath);
    var audioWaveCorrelationCatalog = GameAssetCatalog.Scan(audioWaveCorrelationFileSystem);
    var audioWaveCorrelation = AudioEventPayloadWaveCorrelationAudit.Audit(
        audioWaveCorrelationCatalog,
        audioWaveCorrelationFileSystem.OpenReadOnly,
        audioWaveCorrelationMaximumBankAttempts);
    AudioEventPayloadWaveCorrelationAudit.WriteReport(
        audioWaveCorrelationReportPath,
        audioWaveCorrelation.Rows);
    AudioEventPayloadWaveCorrelationAudit.WriteSummary(
        audioWaveCorrelationSummaryPath,
        audioWaveCorrelation.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC audio event payload-wave correlation audit");
    Console.WriteLine($"event_candidates: {audioWaveCorrelation.Summary.EventCandidates}");
    Console.WriteLine($"event_decoded: {audioWaveCorrelation.Summary.EventDecoded}");
    Console.WriteLine($"bank_candidates: {audioWaveCorrelation.Summary.BankCandidates}");
    Console.WriteLine($"bank_decoded: {audioWaveCorrelation.Summary.BankDecoded}");
    Console.WriteLine($"exact_pcm_hash_pairs: {audioWaveCorrelation.Summary.ExactPcmHashPairs}");
    Console.WriteLine($"event_hashes_with_match: {audioWaveCorrelation.Summary.EventHashesWithMatch}");
    Console.WriteLine($"maximum_bank_attempts: {audioWaveCorrelation.Summary.MaximumBankAttempts}");
    Console.WriteLine($"report: {audioWaveCorrelationReportPath}");
    Console.WriteLine($"summary: {audioWaveCorrelationSummaryPath}");
    Console.WriteLine($"status: {audioWaveCorrelation.Summary.Status}");
    Console.WriteLine("mode: ISO read-only, bounded EVT/ABKC/AST payload decode and PCM hash comparison; no event, sample, gameplay or original selection semantics assigned");
    return audioWaveCorrelation.Summary.Status == "verified-structural" ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-abk-raw-identity-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: audio-event-abk-raw-identity-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <summary.tsv>]");
        return 2;
    }

    var rawIdentityIsoPath = Path.GetFullPath(args[1]);
    var rawIdentityReportPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-abk-raw-identity.tsv");
    var rawIdentitySummaryPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "audio-event-abk-raw-identity-summary.tsv");
    for (var optionIndex = 2; optionIndex + 1 < args.Length; optionIndex++)
    {
        if (string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            rawIdentityReportPath = Path.GetFullPath(args[++optionIndex]);
        }
        else if (string.Equals(args[optionIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            rawIdentitySummaryPath = Path.GetFullPath(args[++optionIndex]);
        }
    }

    if (string.Equals(rawIdentityReportPath, rawIdentityIsoPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(rawIdentitySummaryPath, rawIdentityIsoPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("Os relatórios de identidade bruta não podem sobrescrever a ISO de entrada.");
        return 2;
    }

    using var rawIdentityFileSystem = new XdvdfsFileSystem(rawIdentityIsoPath);
    var rawIdentityCatalog = GameAssetCatalog.Scan(rawIdentityFileSystem);
    var rawIdentity = AudioEventAbkRawIdentityAudit.Audit(
        rawIdentityCatalog,
        rawIdentityFileSystem.OpenReadOnly);
    AudioEventAbkRawIdentityAudit.WriteReport(rawIdentityReportPath, rawIdentity.Rows);
    AudioEventAbkRawIdentityAudit.WriteSummary(rawIdentitySummaryPath, rawIdentity.Summary);
    Console.WriteLine($"{GameIdentity.Name} PC raw EVT/ABKC/AST identity audit");
    Console.WriteLine("mode: ISO read-only, full raw-range hashes and bounded AST SCHl ranges; no event/sample/playback promotion");
    Console.WriteLine($"event_candidates: {rawIdentity.Summary.EventCandidates}");
    Console.WriteLine($"event_raw_rows: {rawIdentity.Summary.EventRawRows}");
    Console.WriteLine($"event_distinct_raw_hashes: {rawIdentity.Summary.EventDistinctRawHashes}");
    Console.WriteLine($"bank_count: {rawIdentity.Summary.BankCount}");
    Console.WriteLine($"bank_non_dummy_sound_entries: {rawIdentity.Summary.BankNonDummySoundEntries}");
    Console.WriteLine($"bank_ast_range_rows: {rawIdentity.Summary.BankAstRangeRows}");
    Console.WriteLine($"bank_ast_schl_rows: {rawIdentity.Summary.BankAstSchlRows}");
    Console.WriteLine($"full_raw_hash_pairs: {rawIdentity.Summary.FullRawHashPairs}");
    Console.WriteLine($"event_hashes_with_full_raw_match: {rawIdentity.Summary.EventHashesWithFullRawMatch}");
    Console.WriteLine($"raw_prefix_pairs: {rawIdentity.Summary.RawPrefixPairs}");
    Console.WriteLine($"rejected_rows: {rawIdentity.Summary.RejectedRows}");
    Console.WriteLine($"status: {rawIdentity.Summary.Status}");
    Console.WriteLine($"direct_selection_binding: {rawIdentity.Summary.DirectSelectionBinding} | original_semantics: {rawIdentity.Summary.OriginalSemantics}");
    Console.WriteLine($"report: {rawIdentityReportPath}");
    Console.WriteLine($"summary: {rawIdentitySummaryPath}");
    return rawIdentity.Summary.Status == AudioEventAbkRawIdentityAudit.Status ? 0 : 1;
}

if (string.Equals(args[0], "audio-abk-batch-decode-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-abk-batch-decode-smoke <caminho-do-iso> [--report <saida.tsv>] [--max-attempts <n>]");
        return 2;
    }

    var bankBatchImagePath = Path.GetFullPath(args[1]);
    var bankBatchReportPath = Path.Combine(
        Path.GetDirectoryName(bankBatchImagePath) ?? Directory.GetCurrentDirectory(),
        "audio-abk-batch-decode.tsv");
    var bankBatchMaximumAttempts = 8;
    for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase) &&
            argumentIndex + 1 < args.Length)
        {
            bankBatchReportPath = Path.GetFullPath(args[++argumentIndex]);
        }
        else if (string.Equals(args[argumentIndex], "--max-attempts", StringComparison.OrdinalIgnoreCase) &&
                 argumentIndex + 1 < args.Length &&
                 int.TryParse(args[++argumentIndex], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAttempts))
        {
            bankBatchMaximumAttempts = parsedAttempts;
        }
    }

    using var bankBatchFileSystem = new XdvdfsFileSystem(bankBatchImagePath);
    var bankBatchCatalog = GameAssetCatalog.Scan(bankBatchFileSystem);
    var bankBatchRows = AudioBankBatchDecoder.DecodeFirstAvailable(
        bankBatchCatalog,
        bankBatchFileSystem.OpenReadOnly,
        bankBatchMaximumAttempts);
    AudioBankBatchDecoder.WriteReport(bankBatchReportPath, bankBatchRows);
    Console.WriteLine($"audio_abk_batch_decode_smoke: {bankBatchReportPath} | banks={bankBatchRows.Count} | decoded_banks={bankBatchRows.Count(row => row.DecodeValidated)} | rejected_banks={bankBatchRows.Count(row => !row.DecodeValidated)} | decoded_wave_bytes={bankBatchRows.Sum(row => row.WaveBytes)}");
    Console.WriteLine($"sources: {string.Join(",", bankBatchRows.GroupBy(row => row.SourceKind, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => $"{group.Key}={group.Count()}"))}");
    Console.WriteLine("status: bounded ABKC/AST/BNK payload decode evidence only; no EVT, gameplay, material, UV, skinning, rigging, animation, or GPU binding assigned");
    Console.WriteLine($"mode: ISO read-only, up to {bankBatchMaximumAttempts} bounded candidates per bank, first valid result selected, managed EA-XA/Xbox IMA decode, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "audio-event-named-decode-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-named-decode-smoke <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    var namedDecodeImagePath = Path.GetFullPath(args[1]);
    var namedDecodeReportPath = Path.Combine(
        Path.GetDirectoryName(namedDecodeImagePath) ?? Directory.GetCurrentDirectory(),
        "audio-named-event-decode.tsv");
    for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            namedDecodeReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    using var namedDecodeFileSystem = new XdvdfsFileSystem(namedDecodeImagePath);
    var namedDecodeCatalog = GameAssetCatalog.Scan(namedDecodeFileSystem);
    var namedDecodeResults = AudioNamedEventCandidateDecoder.Decode(
        namedDecodeCatalog.AudioEventCandidates,
        namedDecodeFileSystem.OpenReadOnly);
    AudioNamedEventCandidateDecoder.WriteReport(namedDecodeReportPath, namedDecodeResults);
    Console.WriteLine($"audio_event_named_decode_smoke: {namedDecodeReportPath} | candidates={namedDecodeResults.Count} | header_validated={namedDecodeResults.Count(result => result.HeaderValidated)} | decode_validated={namedDecodeResults.Count(result => result.DecodeValidated)} | rejected={namedDecodeResults.Count(result => !result.DecodeValidated)}");
    Console.WriteLine($"decoded_wave_bytes: {namedDecodeResults.Where(result => result.DecodeValidated).Sum(result => result.WaveBytes)}");
    Console.WriteLine("status: named EVT/header/data payload evidence only; no gameplay event, ABKC, material, UV, skinning, rigging, animation, or GPU binding assigned");
    Console.WriteLine("mode: ISO read-only, bounded ranges, managed EA-XA decoder, TSV diagnostics");
    return 0;
}

if (string.Equals(args[0], "audio-event-candidate-validate", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-candidate-validate <caminho-do-iso>");
        return 2;
    }

    using var eventValidationFileSystem = new XdvdfsFileSystem(args[1]);
    const string validationEventPath = "data/audio/eventspa.evt";
    const string validationHeaderOffPath = "data/audio/pcpahdr.off";
    const string validationHeaderBigPath = "data/audio/pcpahdr.big";
    const string validationDataOffPath = "data/audio/pcpadat.off";
    const string validationDataBigPath = "data/audio/pcpadat.big";
    var validationAudioPaths = eventValidationFileSystem.Enumerate("data/audio")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    if (!validationAudioPaths.IsSupersetOf(new[]
        {
            validationEventPath,
            validationHeaderOffPath,
            validationHeaderBigPath,
            validationDataOffPath,
            validationDataBigPath,
        }))
    {
        Console.Error.WriteLine("Os companions PA necessários não estão completos na ISO.");
        return 2;
    }

    using var validationEventStream = eventValidationFileSystem.OpenReadOnly(validationEventPath);
    var validationEventData = ReadBounded(validationEventStream, 1 * 1024 * 1024);
    using var validationHeaderOffStream = eventValidationFileSystem.OpenReadOnly(validationHeaderOffPath);
    using var validationHeaderLengthStream = eventValidationFileSystem.OpenReadOnly(validationHeaderBigPath);
    var validationHeaderTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(validationHeaderOffStream, 1 * 1024 * 1024),
        validationHeaderLengthStream.Length);
    using var validationDataOffStream = eventValidationFileSystem.OpenReadOnly(validationDataOffPath);
    using var validationDataLengthStream = eventValidationFileSystem.OpenReadOnly(validationDataBigPath);
    var validationDataTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(validationDataOffStream, 1 * 1024 * 1024),
        validationDataLengthStream.Length);
    using var validationHeaderCompanion = eventValidationFileSystem.OpenReadOnly(validationHeaderBigPath);
    var validationCandidates = AudioAssetReader.CorrelateEventToHeaderRows(
        validationEventData,
        validationHeaderCompanion,
        validationHeaderTable,
        validationDataTable);
    var validationRows = validationCandidates
        .Select(candidate => candidate.HeaderRowIndex)
        .Distinct()
        .Where(index => index >= 0 && index < validationDataTable.Rows.Count)
        .OrderBy(index => index)
        .ToArray();
    using var validationDataCompanion = eventValidationFileSystem.OpenReadOnly(validationDataBigPath);
    var validatedRanges = 0;
    var rejectedRanges = 0;
    var rejectionChunkTypes = new Dictionary<string, int>(StringComparer.Ordinal);
    long totalFrames = 0;
    long totalBlocks = 0;
    foreach (var rowIndex in validationRows)
    {
        var row = validationDataTable.Rows[rowIndex];
        using var range = new AudioRangeStream(
            validationDataCompanion,
            row.FirstColumn,
            row.SecondColumn);
        try
        {
            var info = EaSchlAudioDecoder.Validate(range);
            validatedRanges++;
            totalFrames += info.SampleFrames;
            totalBlocks += info.Blocks;
        }
        catch (Exception exception) when (
            exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
        {
            rejectedRanges++;
            var chunkType = "unknown";
            try
            {
                chunkType = EaSchlAudioDecoder.InspectFirstDataChunk(range).Type;
            }
            catch (Exception)
            {
            }
            rejectionChunkTypes[chunkType] = rejectionChunkTypes.GetValueOrDefault(chunkType) + 1;
            if (rejectedRanges <= 8)
            {
                var chain = "none";
                try
                {
                    var firstChunk = EaSchlAudioDecoder.InspectFirstDataChunk(range);
                    if (string.Equals(firstChunk.Type, "SCCl", StringComparison.Ordinal) &&
                        firstChunk.Size >= 12 && firstChunk.Offset + firstChunk.Size + 8 <= range.Length)
                    {
                        range.Position = firstChunk.Offset + 8;
                        var payload = new byte[sizeof(uint)];
                        range.ReadExactly(payload);
                        range.Position = firstChunk.Offset + firstChunk.Size;
                        var nextHeader = new byte[8];
                        range.ReadExactly(nextHeader);
                        var nextType = Encoding.ASCII.GetString(nextHeader.AsSpan(0, 4));
                        var nextSize = BinaryPrimitives.ReadUInt32LittleEndian(nextHeader.AsSpan(4, 4));
                        chain = $"SCCl_payload=0x{BinaryPrimitives.ReadUInt32LittleEndian(payload):X8}->next={nextType}:{nextSize}";
                    }
                }
                catch (Exception)
                {
                }
                Console.WriteLine($"audio_candidate_invalid: row={rowIndex} | chunk={chunkType} | {chain} | {exception.Message}");
            }
        }
    }

    Console.WriteLine($"{GameIdentity.Name} PC audio event candidate range validation");
    Console.WriteLine("mode: ISO read-only, exact event/header candidates, EA-XA validation, no WAV allocation");
    Console.WriteLine($"candidate_occurrences: {validationCandidates.Count}");
    Console.WriteLine($"candidate_rows: {validationRows.Length}");
    Console.WriteLine($"validated_candidate_ranges: {validatedRanges}");
    Console.WriteLine($"rejected_candidate_ranges: {rejectedRanges}");
    Console.WriteLine($"rejection_chunk_types: {string.Join(",", rejectionChunkTypes.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"validated_frames: {totalFrames}");
    Console.WriteLine($"validated_blocks: {totalBlocks}");
    Console.WriteLine("status: SCDl blocks after the neutral SCCl envelope are decodable when SCHl channels are valid; 0-channel ranges remain rejected; event-name binding and gameplay playback remain unverified");
    return rejectedRanges == 0 ? 0 : 1;
}

if (string.Equals(args[0], "audio-event-candidate-decode", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4 || !int.TryParse(args[2], out var requestedEventRow) || requestedEventRow < 0)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-event-candidate-decode <caminho-do-iso> <row> <saida.wav>");
        return 2;
    }

    using var eventDecodeFileSystem = new XdvdfsFileSystem(args[1]);
    const string eventDecodeDataOffPath = "data/audio/pcpadat.off";
    const string eventDecodeDataBigPath = "data/audio/pcpadat.big";
    using var eventDecodeOffStream = eventDecodeFileSystem.OpenReadOnly(eventDecodeDataOffPath);
    using var eventDecodeBigStream = eventDecodeFileSystem.OpenReadOnly(eventDecodeDataBigPath);
    var eventDecodeTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(eventDecodeOffStream, 1 * 1024 * 1024),
        eventDecodeBigStream.Length);
    if (!eventDecodeTable.RowsWellFormed || requestedEventRow >= eventDecodeTable.Rows.Count)
    {
        Console.Error.WriteLine($"Row de áudio inválida: {requestedEventRow}.");
        return 2;
    }

    var eventDecodeRow = eventDecodeTable.Rows[requestedEventRow];
    using var eventDecodeRange = new AudioRangeStream(
        eventDecodeBigStream,
        eventDecodeRow.FirstColumn,
        eventDecodeRow.SecondColumn);
    EaSchlAudioInfo eventDecodeInfo;
    byte[] eventDecodeWave;
    try
    {
        eventDecodeInfo = EaSchlAudioDecoder.Inspect(eventDecodeRange);
        eventDecodeRange.Position = 0;
        eventDecodeWave = EaSchlAudioDecoder.DecodeWave(eventDecodeRange);
    }
    catch (Exception exception) when (
        exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
    {
        Console.Error.WriteLine($"Row de áudio {requestedEventRow} rejeitada pelo decoder: {exception.Message}");
        return 1;
    }
    var eventDecodeOutput = Path.GetFullPath(args[3]);
    var eventDecodeDirectory = Path.GetDirectoryName(eventDecodeOutput);
    if (string.IsNullOrEmpty(eventDecodeDirectory))
    {
        Console.Error.WriteLine("A saída WAV precisa de um caminho válido.");
        return 2;
    }

    Directory.CreateDirectory(eventDecodeDirectory);
    try
    {
        using var output = new FileStream(eventDecodeOutput, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        output.Write(eventDecodeWave);
        output.Flush(true);
    }
    catch (IOException exception)
    {
        Console.Error.WriteLine($"Não foi possível criar a saída WAV sem sobrescrever um arquivo existente: {exception.Message}");
        return 2;
    }

    Console.WriteLine($"{GameIdentity.Name} PC explicit indexed event-candidate decode");
    Console.WriteLine($"data_row: {requestedEventRow}");
    Console.WriteLine($"range: offset={eventDecodeRow.FirstColumn} | bytes={eventDecodeRow.SecondColumn}");
    Console.WriteLine($"format: PCM16 | channels={eventDecodeInfo.Channels} | rate={eventDecodeInfo.SampleRate} | declared_frames={eventDecodeInfo.DeclaredSamples}");
    Console.WriteLine($"wav: {eventDecodeOutput} | bytes={eventDecodeWave.Length}");
    Console.WriteLine("mode: ISO read-only, explicit indexed pcpadat row, EA-XA managed decode; no EVT name binding");
    return 0;
}

if (string.Equals(args[0], "audio-sccl-channel-layout-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-sccl-channel-layout-audit <caminho-do-iso> [--report <saida.tsv>]");
        return 2;
    }

    string? scclLayoutReportPath = null;
    for (var optionIndex = 2; optionIndex < args.Length; optionIndex++)
    {
        if (!string.Equals(args[optionIndex], "--report", StringComparison.OrdinalIgnoreCase) ||
            optionIndex + 1 >= args.Length ||
            args[optionIndex + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Uso: FifaStreetPc audio-sccl-channel-layout-audit <caminho-do-iso> [--report <saida.tsv>]");
            return 2;
        }

        scclLayoutReportPath = Path.GetFullPath(args[++optionIndex]);
    }

    var scclLayoutInputPath = Path.GetFullPath(args[1]);
    if (scclLayoutReportPath is not null &&
        string.Equals(scclLayoutInputPath, scclLayoutReportPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O relatório de layout SCCl não pode sobrescrever a ISO de entrada.");
        return 2;
    }

    using var scclLayoutFileSystem = new XdvdfsFileSystem(args[1]);
    const string scclLayoutEventPath = "data/audio/eventspa.evt";
    const string scclLayoutHeaderOffPath = "data/audio/pcpahdr.off";
    const string scclLayoutHeaderBigPath = "data/audio/pcpahdr.big";
    const string scclLayoutDataOffPath = "data/audio/pcpadat.off";
    const string scclLayoutDataBigPath = "data/audio/pcpadat.big";
    using var scclLayoutEventStream = scclLayoutFileSystem.OpenReadOnly(scclLayoutEventPath);
    using var scclLayoutHeaderOffStream = scclLayoutFileSystem.OpenReadOnly(scclLayoutHeaderOffPath);
    using var scclLayoutHeaderBigStream = scclLayoutFileSystem.OpenReadOnly(scclLayoutHeaderBigPath);
    using var scclLayoutDataOffStream = scclLayoutFileSystem.OpenReadOnly(scclLayoutDataOffPath);
    using var scclLayoutDataBigStream = scclLayoutFileSystem.OpenReadOnly(scclLayoutDataBigPath);
    var scclLayoutHeaderTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(scclLayoutHeaderOffStream, 1 * 1024 * 1024), scclLayoutHeaderBigStream.Length);
    var scclLayoutDataTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(scclLayoutDataOffStream, 1 * 1024 * 1024), scclLayoutDataBigStream.Length);
    var scclLayoutCandidates = AudioAssetReader.CorrelateEventToHeaderRows(
        ReadBounded(scclLayoutEventStream, 1 * 1024 * 1024),
        scclLayoutHeaderBigStream,
        scclLayoutHeaderTable,
        scclLayoutDataTable);
    var scclLayoutRows = AudioScclChannelLayoutAuditor.Audit(
        scclLayoutCandidates,
        scclLayoutDataBigStream);
    var scclLayoutSummary = AudioScclChannelLayoutAuditor.Summarize(scclLayoutRows);
    if (scclLayoutReportPath is not null)
    {
        AudioScclChannelLayoutAuditor.WriteReport(scclLayoutReportPath, scclLayoutRows);
    }

    Console.WriteLine($"{GameIdentity.Name} PC SCCl channel-layout candidate audit");
    Console.WriteLine("mode: ISO read-only, bounded SCDl hypothesis validation; no channel metadata rewrite or event/gameplay binding");
    Console.WriteLine($"candidate_rows: {scclLayoutCandidates.Select(candidate => candidate.HeaderRowIndex).Distinct().Count()}");
    Console.WriteLine($"zero_channel_rows: {scclLayoutSummary.ObservationCount}");
    Console.WriteLine($"mono_valid: {scclLayoutSummary.MonoValidCount}");
    Console.WriteLine($"stereo_valid: {scclLayoutSummary.StereoValidCount}");
    Console.WriteLine($"unique_mono: {scclLayoutSummary.UniqueMonoCount}");
    Console.WriteLine($"unique_stereo: {scclLayoutSummary.UniqueStereoCount}");
    Console.WriteLine($"both_valid: {scclLayoutSummary.BothValidCount}");
    Console.WriteLine($"neither_valid: {scclLayoutSummary.NeitherValidCount}");
    Console.WriteLine($"distinct_sccl_payload_prefixes: {scclLayoutSummary.DistinctScclPayloadPrefixes}");
    if (scclLayoutReportPath is not null)
    {
        Console.WriteLine($"report: {scclLayoutReportPath} | rows={scclLayoutRows.Count}");
    }

    Console.WriteLine("status: successful mono/stereo layouts are payload evidence only; no EVT name, ABKC ID, material, UV, skinning, rigging, animation or GPU binding assigned");
    return scclLayoutRows.Count > 0 ? 0 : 1;
}

if (string.Equals(args[0], "audio-sccl-inspect", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-sccl-inspect <caminho-do-iso> [amostras] [--rejected]");
        return 2;
    }

    var sampleLimit = args.Length >= 3 && int.TryParse(args[2], out var requestedSamples)
        ? Math.Clamp(requestedSamples, 1, 64)
        : 12;
    var rejectedOnly = args.Any(argument => string.Equals(argument, "--rejected", StringComparison.OrdinalIgnoreCase));
    using var scclFileSystem = new XdvdfsFileSystem(args[1]);
    const string scclEventPath = "data/audio/eventspa.evt";
    const string scclHeaderOffPath = "data/audio/pcpahdr.off";
    const string scclHeaderBigPath = "data/audio/pcpahdr.big";
    const string scclDataOffPath = "data/audio/pcpadat.off";
    const string scclDataBigPath = "data/audio/pcpadat.big";
    using var scclEventStream = scclFileSystem.OpenReadOnly(scclEventPath);
    using var scclHeaderOffStream = scclFileSystem.OpenReadOnly(scclHeaderOffPath);
    using var scclHeaderBigStream = scclFileSystem.OpenReadOnly(scclHeaderBigPath);
    using var scclDataOffStream = scclFileSystem.OpenReadOnly(scclDataOffPath);
    using var scclDataBigStream = scclFileSystem.OpenReadOnly(scclDataBigPath);
    var scclHeaderTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(scclHeaderOffStream, 1 * 1024 * 1024), scclHeaderBigStream.Length);
    var scclDataTable = AudioAssetReader.InspectOffsetTable(
        ReadBounded(scclDataOffStream, 1 * 1024 * 1024), scclDataBigStream.Length);
    var scclCandidates = AudioAssetReader.CorrelateEventToHeaderRows(
        ReadBounded(scclEventStream, 1 * 1024 * 1024),
        scclHeaderBigStream,
        scclHeaderTable,
        scclDataTable);
    var scclRows = scclCandidates
        .Where(candidate => candidate.HeaderRowIndex >= 0 &&
                           candidate.HeaderRowIndex < scclDataTable.Rows.Count)
        .GroupBy(candidate => candidate.HeaderRowIndex)
        .Select(group => group.First())
        .OrderBy(candidate => candidate.HeaderRowIndex)
        .ToArray();
    var printed = 0;
    var firstChunkCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    var channelCounts = new Dictionary<int, int>();
    Console.WriteLine($"{GameIdentity.Name} PC SCCl chunk evidence");
    Console.WriteLine($"mode: ISO read-only, bounded chunk inspection, no SCCl decode or channel inference | sample_filter={(rejectedOnly ? "channels=0" : "all")}");
    Console.WriteLine($"candidate_rows: {scclRows.Length}");
    foreach (var candidate in scclRows)
    {
        var dataRow = candidate.ParallelDataRow;
        if (dataRow is null)
        {
            continue;
        }

        using var dataRange = new AudioRangeStream(
            scclDataBigStream,
            dataRow.FirstColumn,
            dataRow.SecondColumn);
        IReadOnlyList<EaSchlChunkEvidence> chunks;
        IReadOnlyList<EaSchlHeaderFieldEvidence> fields;
        EaSchlAudioInfo streamInfo;
        try
        {
            streamInfo = EaSchlAudioDecoder.Inspect(dataRange);
            fields = EaSchlAudioDecoder.InspectHeaderFields(dataRange);
            chunks = EaSchlAudioDecoder.InspectChunkSequence(dataRange, maximumChunks: 3, payloadPrefixBytes: 16);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException)
        {
            continue;
        }
        if (chunks.Count == 0)
        {
            continue;
        }
        firstChunkCounts[chunks[0].Type] = firstChunkCounts.GetValueOrDefault(chunks[0].Type) + 1;
        channelCounts[streamInfo.Channels] = channelCounts.GetValueOrDefault(streamInfo.Channels) + 1;
        if (!string.Equals(chunks[0].Type, "SCCl", StringComparison.Ordinal))
        {
            continue;
        }
        if (rejectedOnly && streamInfo.Channels != 0)
        {
            continue;
        }
        if (printed >= sampleLimit)
        {
            continue;
        }

        var headerRow = candidate.HeaderRow;
        var headerPrefixLength = checked((int)Math.Min(32L, headerRow.SecondColumn));
        var headerPrefix = new byte[headerPrefixLength];
        scclHeaderBigStream.Position = headerRow.FirstColumn;
        scclHeaderBigStream.ReadExactly(headerPrefix);
        var chain = string.Join(
            " -> ",
            chunks.Select(chunk =>
                $"{chunk.Type}:{chunk.Size}[{Convert.ToHexString(chunk.PayloadPrefix)}]"));
        var metadata = string.Join(
            ",",
            fields.Select(field => $"0x{field.Tag:X2}/{field.Length}=0x{field.Value:X8}"));
        Console.WriteLine(
            $"sccl_sample: row={candidate.HeaderRowIndex} | " +
            $"header_offset={headerRow.FirstColumn} header_length={headerRow.SecondColumn} " +
            $"data_offset={dataRow.FirstColumn} data_length={dataRow.SecondColumn} | " +
            $"stream_header={streamInfo.HeaderSize} channels={streamInfo.Channels} " +
            $"sample_rate={streamInfo.SampleRate} declared_samples={streamInfo.DeclaredSamples} | " +
            $"metadata={metadata} | header_prefix={Convert.ToHexString(headerPrefix)} | chain={chain}");
        printed++;
    }

    Console.WriteLine($"first_chunk_types: {string.Join(",", firstChunkCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"stream_channels: {string.Join(",", channelCounts.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}={pair.Value}"))}");
    Console.WriteLine($"printed_samples: {printed}");
    Console.WriteLine("status: SCCl envelope is observed; payload semantics, channel count, event-name binding and playback remain unverified");
    return 0;
}

if (string.Equals(args[0], "audio-abk-inspect", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-abk-inspect <caminho-do-iso> [arquivo.abk]");
        return 2;
    }

    using var abkFileSystem = new XdvdfsFileSystem(args[1]);
    var requestedAbkPath = args.Skip(2).FirstOrDefault(argument =>
        !argument.StartsWith("--", StringComparison.Ordinal));
    var abkPathsForInspection = abkFileSystem.Enumerate("data/audio")
        .Where(path => path.EndsWith(".abk", StringComparison.OrdinalIgnoreCase))
        .Where(path => requestedAbkPath is null || string.Equals(path, requestedAbkPath, StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (abkPathsForInspection.Length == 0)
    {
        Console.Error.WriteLine("Nenhum banco ABKC selecionado foi encontrado.");
        return 2;
    }

    var allAudioPathsForInspection = abkFileSystem.Enumerate("data/audio")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    Console.WriteLine($"{GameIdentity.Name} PC ABKC module/sample-table inspection");
    Console.WriteLine("mode: ISO read-only, independent bounded structure reader; no event semantics");
    foreach (var abkPath in abkPathsForInspection)
    {
        using var abkStream = abkFileSystem.OpenReadOnly(abkPath);
        var abkData = ReadBounded(abkStream, 16 * 1024 * 1024);
        AudioAbkInspection inspection;
        try
        {
            inspection = AudioAbkReader.Inspect(abkData);
        }
        catch (InvalidDataException exception)
        {
            Console.WriteLine($"abk_invalid: {abkPath} | {exception.Message}");
            continue;
        }

        AudioAbkBnkInspection? bnkEvidence = null;
        IReadOnlyList<AudioAbkBnkHeaderEvidence> bnkHeaders = Array.Empty<AudioAbkBnkHeaderEvidence>();
        string? bnkIssue = null;
        try
        {
            bnkEvidence = AudioAbkReader.InspectEmbeddedBnk(inspection, abkData);
            if (bnkEvidence is not null)
            {
                bnkHeaders = AudioAbkReader.InspectEmbeddedBnkHeaders(bnkEvidence, abkData);
            }
        }
        catch (InvalidDataException exception)
        {
            bnkIssue = exception.Message;
        }

        var astPath = abkPath[..^4] + ".ast";
        AudioAbkAstReferenceEvidence? astEvidence = null;
        var astValidated = 0;
        var astRejected = 0;
        long astFrames = 0;
        var astBlocks = 0;
        if (allAudioPathsForInspection.Contains(astPath))
        {
            using var astStream = abkFileSystem.OpenReadOnly(astPath);
            astEvidence = AudioAbkReader.InspectAstReferences(inspection, astStream);
            if (args.Any(argument => string.Equals(argument, "--validate-streams", StringComparison.OrdinalIgnoreCase)))
            {
                var ranges = AudioAbkReader.BuildAstRanges(inspection, astStream.Length)
                    .Where(range => range.Length >= 8)
                    .GroupBy(range => (range.Offset, range.Length))
                    .Select(group => group.First())
                    .ToArray();
                foreach (var range in ranges)
                {
                    using var astRange = new AudioRangeStream(astStream, range.Offset, range.Length);
                    try
                    {
                        var validation = EaSchlAudioDecoder.Validate(astRange);
                        astValidated++;
                        astFrames += validation.SampleFrames;
                        astBlocks += validation.Blocks;
                    }
                    catch (Exception exception) when (
                        exception is InvalidDataException or NotSupportedException or EndOfStreamException or OverflowException)
                    {
                        astRejected++;
                        Console.WriteLine($"abk_ast_invalid: sound={range.SoundIndex} | loop={range.LoopSegment} | offset=0x{range.Offset:X} | length={range.Length} | {exception.Message}");
                        var channelLayoutCandidates = new List<int>();
                        foreach (var candidateChannels in new[] { 1, 2 })
                        {
                            using var candidateRange = new AudioRangeStream(astStream, range.Offset, range.Length);
                            if (EaSchlAudioDecoder.TryValidateChannelLayoutCandidate(
                                    candidateRange,
                                    candidateChannels,
                                    out _,
                                    out _))
                            {
                                channelLayoutCandidates.Add(candidateChannels);
                            }
                        }

                        var channelLayoutStatus = channelLayoutCandidates.Count switch
                        {
                            0 => "none",
                            1 => "unique-candidate",
                            _ => "ambiguous-candidate",
                        };
                        Console.WriteLine(
                            $"abk_ast_channel_layout_candidate: sound={range.SoundIndex} | loop={range.LoopSegment} | " +
                            $"channels={string.Join(",", channelLayoutCandidates)} | status={channelLayoutStatus} | " +
                            "evidence-only; no automatic bank/event/gameplay binding");
                    }
                }
            }
        }

        Console.WriteLine(
            $"abk: {abkPath} | bytes={inspection.AssetLength} | endian={(inspection.LittleEndian ? "little" : "big")} | " +
            $"modules={inspection.ModuleCount} | module_table=0x{inspection.ModuleTableOffset:X} | bnk=0x{inspection.BnkOffset:X} | " +
            $"sample_tables={inspection.SampleTables.Count} | sounds={inspection.Sounds.Count} | real_sounds={inspection.NonDummySoundCount} | " +
            $"dummy_sounds={inspection.DummySoundCount} | ram_sounds={inspection.RamSoundCount} | stream_sounds={inspection.StreamSoundCount} | " +
            $"looped_stream_sounds={inspection.LoopedStreamSoundCount} | " +
            $"bnk={(bnkEvidence is null ? (bnkIssue ?? "unavailable") : $"v{bnkEvidence.Version},sounds={bnkEvidence.SoundCount},real={bnkEvidence.RealSoundCount},valid_headers={bnkEvidence.ValidHeaderCount},parsed_variable_headers={bnkHeaders.Count(header => header.Parsed)},table=0x{bnkEvidence.TableOffset:X}")} | " +
            $"ast={(astEvidence is null ? "unavailable" : $"data_schl={astEvidence.DataOffsetSchlCount}/{astEvidence.StreamSoundCount},loop_schl={astEvidence.LoopOffsetSchlCount}/{astEvidence.LoopOffsetWithinAstCount},issues={astEvidence.Issues.Count},validated={astValidated},rejected={astRejected},frames={astFrames},blocks={astBlocks}")}");
        if (bnkEvidence is not null)
        {
            var bnkValidations = AudioAbkReader.ValidateEmbeddedBnkStreams(bnkEvidence, abkData);
            Console.WriteLine($"abk_bnk_xbox_ima_data: validated={bnkValidations.Count(validation => validation.Valid)} | rejected={bnkValidations.Count(validation => !validation.Valid)}");
            foreach (var entry in bnkEvidence.Entries.Take(8))
            {
                Console.WriteLine($"abk_bnk_entry: index={entry.EntryIndex} | relative=0x{entry.RelativeOffset:X} | header=0x{entry.HeaderOffset:X} | dummy={entry.IsDummy} | header_within_asset={entry.HeaderWithinAsset}");
            }
            var bnkHeaderSampleLimit = args.Any(argument => string.Equals(argument, "--all-bnk", StringComparison.OrdinalIgnoreCase)) ? 4096 : 8;
            foreach (var header in bnkHeaders.Take(bnkHeaderSampleLimit))
            {
                var tags = string.Join(",", header.Tags.Select(tag => $"0x{tag.Tag:X2}:{tag.Payload.Length}:{(tag.BigEndianValue is null ? "-" : $"0x{tag.BigEndianValue:X}")}"));
                Console.WriteLine($"abk_bnk_header: index={header.EntryIndex} | offset=0x{header.HeaderOffset:X} | prefix={header.Prefix} | platform_word={(header.PlatformWord is null ? "none" : $"0x{header.PlatformWord:X4}")} | tags={tags} | end=0x{header.EndOffset:X} | terminated={header.HeaderTerminated} | issue={header.Issue ?? "none"}");
                var dataTag = header.FindTag(0x88);
                if (dataTag?.BigEndianValue is { } relativeDataOffset)
                {
                    var dataOffset = checked((uint)(bnkEvidence.Offset + relativeDataOffset));
                    var prefixLength = dataOffset <= (uint)abkData.Length
                        ? Math.Min(16, abkData.Length - checked((int)dataOffset))
                        : 0;
                    var prefix = prefixLength > 0
                        ? Convert.ToHexString(abkData.AsSpan(checked((int)dataOffset), prefixLength))
                        : "outside";
                    Console.WriteLine($"abk_bnk_data_prefix: index={header.EntryIndex} | relative=0x{relativeDataOffset:X} | absolute=0x{dataOffset:X} | bytes={prefix}");
                }
            }
        }
        foreach (var sound in inspection.Sounds.Take(12))
        {
            Console.WriteLine(
                $"abk_sound: index={sound.SoundIndex} | module={sound.ModuleIndex} | player={sound.PlayerIndex} | table={sound.SampleTableIndex} | entry={sound.EntryIndex} | type={sound.SoundType} | priority={sound.Priority} | data=0x{sound.DataOffset:X} | loop=0x{sound.LoopOffset:X} | dummy={sound.IsDummy}");
        }
        if (astEvidence is not null)
        {
            foreach (var issue in astEvidence.Issues.Take(8))
            {
                Console.WriteLine($"abk_ast_issue: {issue}");
            }
        }
    }

    return 0;
}

if (string.Equals(args[0], "audio-abk-decode", StringComparison.OrdinalIgnoreCase) ||
    string.Equals(args[0], "audio-abk-play", StringComparison.OrdinalIgnoreCase))
{
    var playRamSound = string.Equals(args[0], "audio-abk-play", StringComparison.OrdinalIgnoreCase);
    var minimumArguments = playRamSound ? 4 : 5;
    if (args.Length < minimumArguments || !int.TryParse(args[3], out var requestedSoundIndex) || requestedSoundIndex < 1)
    {
        Console.Error.WriteLine(playRamSound
            ? "Uso: FifaStreetPc audio-abk-play <caminho-do-iso> <arquivo.abk> <índice-do-som>"
            : "Uso: FifaStreetPc audio-abk-decode <caminho-do-iso> <arquivo.abk> <índice-do-som> <saida.wav>");
        return 2;
    }

    using var ramDecodeFileSystem = new XdvdfsFileSystem(args[1]);
    var ramDecodeCatalog = GameAssetCatalog.Scan(ramDecodeFileSystem);
    var ramDecodeBank = ramDecodeCatalog.AudioBanks.FirstOrDefault(bank =>
        string.Equals(bank.Path, args[2], StringComparison.OrdinalIgnoreCase));
    if (ramDecodeBank is null)
    {
        Console.Error.WriteLine($"Banco ABKC não encontrado: {args[2]}");
        return 2;
    }

    var ramDecodeMixer = new CatalogAudioMixer(
        ramDecodeCatalog,
        path => ramDecodeFileSystem.OpenReadOnly(path));
    ramDecodeMixer.Initialize();
    if (!ramDecodeMixer.TryPrepareBankRamSound(ramDecodeBank, requestedSoundIndex, out var ramWave))
    {
        Console.Error.WriteLine($"Som RAM {requestedSoundIndex} não pôde ser decodificado no banco {args[2]}.");
        return 1;
    }

    if (playRamSound)
    {
        Console.WriteLine($"{GameIdentity.Name} PC explicit ABKC RAM sound playback");
        Console.WriteLine($"bank: {ramDecodeBank.Path}");
        Console.WriteLine($"sound_index: {requestedSoundIndex}");
        Console.WriteLine($"decoded_pcm_bytes: {ramWave.Length - 44}");
        Console.WriteLine("mode: ISO read-only, explicit sound selection, Xbox IMA managed decode; no EVT binding");
        Win32WavePlayer.PlaySynchronously(ramWave);
        Console.WriteLine("playback: complete");
        return 0;
    }

    var outputPath = Path.GetFullPath(args[4]);
    var outputDirectory = Path.GetDirectoryName(outputPath);
    if (string.IsNullOrEmpty(outputDirectory))
    {
        Console.Error.WriteLine("A saída WAV precisa de um caminho válido.");
        return 2;
    }
    Directory.CreateDirectory(outputDirectory);
    try
    {
        using var output = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        output.Write(ramWave);
        output.Flush(true);
    }
    catch (IOException exception)
    {
        Console.Error.WriteLine($"Não foi possível criar a saída WAV sem sobrescrever um arquivo existente: {exception.Message}");
        return 2;
    }

    Console.WriteLine($"{GameIdentity.Name} PC explicit ABKC RAM sound decode");
    Console.WriteLine($"bank: {ramDecodeBank.Path}");
    Console.WriteLine($"sound_index: {requestedSoundIndex}");
    Console.WriteLine($"wav: {outputPath} | bytes={ramWave.Length}");
    Console.WriteLine($"prepared_bank_pcm_bytes: {ramDecodeMixer.PreparedBankPcmBytes}");
    Console.WriteLine("mode: ISO read-only, explicit sound selection, Xbox IMA managed decode; no EVT binding");
    return 0;
}

if (string.Equals(args[0], "audio-structure-smoke", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-structure-smoke <caminho-do-iso>");
        return 2;
    }

    using var audioFileSystem = new XdvdfsFileSystem(args[1]);
    var audioPaths = audioFileSystem.Enumerate("data/audio")
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var abkPaths = audioPaths
        .Where(path => path.EndsWith(".abk", StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    var offPaths = audioPaths
        .Where(path => path.EndsWith(".off", StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();
    var evtPaths = audioPaths
        .Where(path => path.EndsWith(".evt", StringComparison.OrdinalIgnoreCase))
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    Console.WriteLine($"{GameIdentity.Name} PC audio structure smoke");
    Console.WriteLine("mode: ISO read-only, bounded structural inspection");
    Console.WriteLine($"audio_files: {audioPaths.Count}");
    Console.WriteLine($"abk_files: {abkPaths.Length}");
    foreach (var path in abkPaths)
    {
        using var stream = audioFileSystem.OpenReadOnly(path);
        var evidence = AudioAssetReader.InspectBankEvidence(ReadBounded(stream, 16 * 1024 * 1024));
        var astPath = path[..^4] + ".ast";
        Console.WriteLine($"abk: {path} | bytes={evidence.AssetLength} | word_0x14_matches_length={evidence.WordAt20MatchesAssetLength} | word_0x1C=0x{evidence.WordAt1C:X8} | word_0x1C_within_asset={evidence.WordAt1CWithinAsset} | same_stem_ast={audioPaths.Contains(astPath)} | embedded_schl={evidence.SchlSignatureCount} | embedded_scdl={evidence.ScdlSignatureCount}");
    }

    Console.WriteLine($"off_files: {offPaths.Length}");
    foreach (var path in offPaths)
    {
        using var stream = audioFileSystem.OpenReadOnly(path);
        var bigPath = path[..^4] + ".big";
        long? companionLength = null;
        if (audioPaths.Contains(bigPath))
        {
            using var companion = audioFileSystem.OpenReadOnly(bigPath);
            companionLength = companion.Length;
            var tableBytes = ReadBounded(stream, 1 * 1024 * 1024);
            var companionTable = AudioAssetReader.InspectOffsetTable(tableBytes, companionLength);
            var companionEvidence = AudioAssetReader.InspectOffsetCompanion(companion, companionTable);
            Console.WriteLine($"off: {path} | rows={companionTable.Rows.Count} | csv={companionTable.IsAsciiCsv} | rows_valid={companionTable.RowsWellFormed} | first_column_monotonic={companionTable.FirstColumnMonotonic} | companion_bytes={companionTable.CompanionLength?.ToString() ?? "unknown"} | ranges_within_companion={companionTable.RangesWithinCompanion} | ranges_readable={companionEvidence.AllRangesReadable} | schl_prefixes={companionEvidence.SchlPrefixCount}/{companionEvidence.RangeCount} | all_ranges_start_schl={companionEvidence.AllRangesStartWithSchl} | first_row_zero={companionTable.FirstRowStartsAtZero}");
            continue;
        }

        var table = AudioAssetReader.InspectOffsetTable(ReadBounded(stream, 1 * 1024 * 1024), companionLength);
        Console.WriteLine($"off: {path} | rows={table.Rows.Count} | csv={table.IsAsciiCsv} | rows_valid={table.RowsWellFormed} | first_column_monotonic={table.FirstColumnMonotonic} | companion_bytes={table.CompanionLength?.ToString() ?? "unknown"} | ranges_within_companion={table.RangesWithinCompanion} | first_row_zero={table.FirstRowStartsAtZero}");
    }

    foreach (var (headerStem, dataStem) in new[]
    {
        (Header: "pcpahdr", Data: "pcpadat"),
        (Header: "djhdr_bo", Data: "djdat_bo"),
        (Header: "djhdr_en", Data: "djdat_en"),
        (Header: "djhdr_fr", Data: "djdat_fr"),
        (Header: "djhdr_ge", Data: "djdat_ge"),
        (Header: "djhdr_it", Data: "djdat_it"),
        (Header: "djhdr_rr", Data: "djdat_rr"),
        (Header: "djhdr_sp", Data: "djdat_sp"),
    })
    {
        var headerOffPath = $"data/audio/{headerStem}.off";
        var headerBigPath = $"data/audio/{headerStem}.big";
        var dataOffPath = $"data/audio/{dataStem}.off";
        var dataBigPath = $"data/audio/{dataStem}.big";
        if (!audioPaths.Contains(headerOffPath) || !audioPaths.Contains(headerBigPath) ||
            !audioPaths.Contains(dataOffPath) || !audioPaths.Contains(dataBigPath))
        {
            Console.WriteLine($"parallel: {headerStem}/{dataStem} | available=false");
            continue;
        }

        using var headerOffStream = audioFileSystem.OpenReadOnly(headerOffPath);
        using var headerCompanionForLength = audioFileSystem.OpenReadOnly(headerBigPath);
        var headerTable = AudioAssetReader.InspectOffsetTable(
            ReadBounded(headerOffStream, 1 * 1024 * 1024),
            headerCompanionForLength.Length);
        using var dataOffStream = audioFileSystem.OpenReadOnly(dataOffPath);
        using var dataCompanionForLength = audioFileSystem.OpenReadOnly(dataBigPath);
        var dataTable = AudioAssetReader.InspectOffsetTable(
            ReadBounded(dataOffStream, 1 * 1024 * 1024),
            dataCompanionForLength.Length);
        using var headerCompanion = audioFileSystem.OpenReadOnly(headerBigPath);
        using var dataCompanion = audioFileSystem.OpenReadOnly(dataBigPath);
        var parallel = AudioAssetReader.InspectParallelCompanions(
            headerCompanion, headerTable, dataCompanion, dataTable);
        Console.WriteLine($"parallel: {headerStem}/{dataStem} | header_ranges={parallel.HeaderRangeCount} | data_ranges={parallel.DataRangeCount} | same_range_count={parallel.SameRangeCount} | header_ranges_readable={parallel.HeaderRangesReadable} | data_ranges_readable={parallel.DataRangesReadable} | data_schl_prefixes={parallel.DataSchlPrefixCount}/{parallel.DataRangeCount} | all_data_ranges_start_schl={parallel.AllDataRangesStartWithSchl}");
        using var comparisonHeader = audioFileSystem.OpenReadOnly(headerBigPath);
        using var comparisonData = audioFileSystem.OpenReadOnly(dataBigPath);
        var comparison = AudioAssetReader.CompareIndexedCompanions(
            comparisonHeader, headerTable, comparisonData, dataTable);
        Console.WriteLine($"indexed_compare: {headerStem}/{dataStem} | pairs={comparison.PairCount} | same_range_count={comparison.SameRangeCount} | header_ranges_readable={comparison.HeaderRangesReadable} | data_ranges_readable={comparison.DataRangesReadable} | data_schl_prefixes={comparison.DataSchlPrefixCount}/{comparison.PairCount} | header_data_length_equal={comparison.HeaderDataLengthEqualCount}/{comparison.PairCount} | header_data_third_column_equal={comparison.HeaderDataThirdColumnEqualCount}/{comparison.PairCount} | header_prefix_equal={comparison.HeaderPrefixEqualCount}/{comparison.PairCount} | pairs_with_shared_nonzero_aligned_words_in_data_prefix={comparison.PairsWithSharedNonZeroAlignedWords}/{comparison.PairCount}");
        foreach (var sample in comparison.Samples)
        {
            Console.WriteLine($"indexed_sample: {headerStem}/{dataStem} | row={sample.RowIndex} | header_offset={sample.HeaderOffset} | header_length={sample.HeaderLength} | data_offset={sample.DataOffset} | data_length={sample.DataLength} | header_prefix={sample.HeaderPrefixHex} | data_prefix={sample.DataPrefixHex} | shared_nonzero_aligned_words_in_data_prefix={sample.SharedNonZeroAlignedWords}");
        }
        using var schlHeader = audioFileSystem.OpenReadOnly(headerBigPath);
        using var schlData = audioFileSystem.OpenReadOnly(dataBigPath);
        var schlComparison = AudioAssetReader.CompareIndexedSchlHeaders(
            schlHeader, headerTable, schlData, dataTable);
        var failureSummary = string.Join(";", schlComparison.SchlParseFailureReasons.Select(pair => $"{pair.Key}x{pair.Value}"));
        var platformOffsets = string.Join(",", schlComparison.PlatformWordOffsets.OrderBy(pair => pair.Key).Select(pair => $"0x{pair.Key:X}x{pair.Value}"));
        var versionOffsets = string.Join(",", schlComparison.VersionWordOffsets.OrderBy(pair => pair.Key).Select(pair => $"0x{pair.Key:X}x{pair.Value}"));
        Console.WriteLine($"indexed_schl: {headerStem}/{dataStem} | pairs={schlComparison.PairCount} | envelope_valid={schlComparison.SchlEnvelopeValidCount}/{schlComparison.PairCount} | pt_headers={schlComparison.SchlPtHeaderCount}/{schlComparison.PairCount} | platform_7={schlComparison.SchlPlatform7Count}/{schlComparison.PairCount} | metadata_header_available={schlComparison.SchlMetadataHeaderAvailableCount}/{schlComparison.PairCount} | metadata_terminated={schlComparison.SchlMetadataTerminatedCount}/{schlComparison.PairCount} | metadata_boundary_violations={schlComparison.SchlMetadataBoundaryViolationCount}/{schlComparison.PairCount} | metadata_parsed={schlComparison.SchlParsedCount}/{schlComparison.PairCount} | parse_failures={schlComparison.SchlParseFailureCount} | failure_reasons={(failureSummary.Length == 0 ? "none" : failureSummary)} | header_contains_schl_header_size={schlComparison.HeaderContainsSchlHeaderSizeCount}/{schlComparison.SchlParsedCount} | header_contains_platform={schlComparison.HeaderContainsPlatformCount}/{schlComparison.SchlParsedCount} | header_contains_version={schlComparison.HeaderContainsVersionCount}/{schlComparison.SchlParsedCount} | header_contains_codec={schlComparison.HeaderContainsCodecCount}/{schlComparison.SchlParsedCount} | header_contains_channels={schlComparison.HeaderContainsChannelsCount}/{schlComparison.SchlParsedCount} | header_contains_sample_rate={schlComparison.HeaderContainsSampleRateCount}/{schlComparison.SchlParsedCount} | header_contains_declared_samples={schlComparison.HeaderContainsDeclaredSamplesCount}/{schlComparison.SchlParsedCount} | platform_word_offsets={platformOffsets} | version_word_offsets={versionOffsets}");
        foreach (var sample in schlComparison.Samples)
        {
            Console.WriteLine($"indexed_schl_sample: {headerStem}/{dataStem} | row={sample.RowIndex} | header_size={sample.HeaderSize} | platform={sample.Platform} | version={sample.Version} | codec={sample.Codec} | channels={sample.Channels} | sample_rate={sample.SampleRate} | declared_samples={sample.DeclaredSamples} | metadata_terminated={sample.MetadataReachedTerminator} | metadata_boundary_violation={sample.MetadataBoundaryViolation} | metadata_tokens={sample.MetadataTokenCount} | metadata_violation_offset={sample.MetadataBoundaryViolationOffset} | header_matches=[size:{sample.HeaderMatchesSchlHeaderSize},platform:{sample.HeaderMatchesPlatform},version:{sample.HeaderMatchesVersion},codec:{sample.HeaderMatchesCodec},channels:{sample.HeaderMatchesChannels},rate:{sample.HeaderMatchesSampleRate},samples:{sample.HeaderMatchesDeclaredSamples}]");
        }
    }

    Console.WriteLine($"evt_files: {evtPaths.Length}");
    foreach (var path in evtPaths)
    {
        using var stream = audioFileSystem.OpenReadOnly(path);
        var eventData = ReadBounded(stream, 1 * 1024 * 1024);
        var evidence = AudioAssetReader.InspectEventFile(eventData);
        var textSummary = string.Join(",", evidence.TextRuns.Select(run => $"0x{run.Offset:X}:{run.Value}"));
        var rawMatchSummary = new List<string>();
        var leadingWordSummary = new List<string>();
        var schlFieldSummary = "unavailable";
        var eventHeaderOccurrenceSummary = "unavailable";
        var eventHeaderWindowSummary = "unavailable";
        foreach (var offPath in offPaths)
        {
            using var offStream = audioFileSystem.OpenReadOnly(offPath);
            var offData = ReadBounded(offStream, 1 * 1024 * 1024);
            var offTable = AudioAssetReader.InspectOffsetTable(offData);
            var rawMatches = AudioAssetReader.FindAlignedRawWordMatches(eventData, offTable.Rows);
            if (rawMatches.Count > 0)
            {
                rawMatchSummary.Add($"{Path.GetFileName(offPath)}=[{string.Join(",", rawMatches.Select(match => $"{match.Value}x{match.Count}@0x{match.FirstWordOffset:X}"))}]");
            }

            var bigPath = offPath[..^4] + ".big";
            if (audioPaths.Contains(bigPath))
            {
                using var companion = audioFileSystem.OpenReadOnly(bigPath);
                var leadingWords = AudioAssetReader.ReadCompanionLeadingWords(companion, offTable);
                var leadingMatches = AudioAssetReader.FindAlignedRawWordMatches(eventData, leadingWords);
                if (leadingMatches.Count > 0)
                {
                    var matchedCandidateCount = leadingMatches.Count(match => leadingWords.Contains(match.Value));
                    var rawOccurrenceCount = leadingMatches.Sum(match => match.Count);
                    leadingWordSummary.Add($"{Path.GetFileName(offPath)}=[candidates={leadingWords.Distinct().Count()},matched={matchedCandidateCount},occurrences={rawOccurrenceCount}]");
                }
            }
        }

        var pcpadatOffPath = "data/audio/pcpadat.off";
        var pcpadatBigPath = "data/audio/pcpadat.big";
        if (audioPaths.Contains(pcpadatOffPath) && audioPaths.Contains(pcpadatBigPath))
        {
            using var pcpadatOffStream = audioFileSystem.OpenReadOnly(pcpadatOffPath);
            using var pcpadatLengthStream = audioFileSystem.OpenReadOnly(pcpadatBigPath);
            var pcpadatTable = AudioAssetReader.InspectOffsetTable(
                ReadBounded(pcpadatOffStream, 1 * 1024 * 1024),
                pcpadatLengthStream.Length);
            using var pcpadatCompanion = audioFileSystem.OpenReadOnly(pcpadatBigPath);
            var schlFieldComparison = AudioAssetReader.CompareEventToIndexedSchlFields(
                eventData, pcpadatCompanion, pcpadatTable);
            schlFieldSummary = string.Join(";", schlFieldComparison.Fields.Select(field =>
                $"{field.FieldName}=[candidates:{field.CandidateCount},matched:{field.DistinctMatchedCount},occurrences:{field.OccurrenceCount}]"));
        }

        if (string.Equals(Path.GetFileName(path), "eventspa.evt", StringComparison.OrdinalIgnoreCase))
        {
            const string pcpahdrOffPath = "data/audio/pcpahdr.off";
            const string pcpahdrBigPath = "data/audio/pcpahdr.big";
            if (audioPaths.Contains(pcpahdrOffPath) && audioPaths.Contains(pcpahdrBigPath))
            {
                using var pcpahdrOffStream = audioFileSystem.OpenReadOnly(pcpahdrOffPath);
                using var pcpahdrLengthStream = audioFileSystem.OpenReadOnly(pcpahdrBigPath);
                var pcpahdrTable = AudioAssetReader.InspectOffsetTable(
                    ReadBounded(pcpahdrOffStream, 1 * 1024 * 1024),
                    pcpahdrLengthStream.Length);
                using var pcpahdrCompanion = audioFileSystem.OpenReadOnly(pcpahdrBigPath);
                var pcpahdrLeadingWords = AudioAssetReader.ReadCompanionLeadingWords(
                    pcpahdrCompanion, pcpahdrTable);
                var occurrences = AudioAssetReader.FindAlignedRawWordOccurrences(
                    eventData, pcpahdrLeadingWords.Select(value => (uint)value));
                var sortedOffsets = occurrences.Select(occurrence => occurrence.WordOffset).OrderBy(offset => offset).ToArray();
                var gaps = sortedOffsets.Zip(sortedOffsets.Skip(1), (previous, current) => current - previous).ToArray();
                var sample = string.Join(",", occurrences.Take(12).Select(occurrence => $"{occurrence.Value}@0x{occurrence.WordOffset:X}"));
                var relativeStats = AudioAssetReader.InspectRelativeWordStats(eventData, occurrences);
                var relativeSummary = string.Join(",", relativeStats.Select(stat =>
                    $"{stat.RelativeByteOffset}:[samples:{stat.SampleCount},distinct:{stat.DistinctWordCount},nonzero:{stat.DistinctNonZeroWordCount},common:0x{stat.MostCommonValue:X8}x{stat.MostCommonCount}]"));
                var duplicateEvidence = AudioAssetReader.InspectDuplicateOccurrenceContexts(
                    eventData, occurrences);
                eventHeaderOccurrenceSummary = $"count={occurrences.Count} | distinct_values={occurrences.Select(occurrence => occurrence.Value).Distinct().Count()} | first_offset=0x{sortedOffsets.FirstOrDefault():X} | last_offset=0x{sortedOffsets.LastOrDefault():X} | before_word_0x04={occurrences.Count(occurrence => occurrence.WordOffset < evidence.WordAt4)} | min_gap={gaps.DefaultIfEmpty(0).Min()} | max_gap={gaps.DefaultIfEmpty(0).Max()} | duplicate_values={duplicateEvidence.DuplicateValueCount} | duplicate_occurrences={duplicateEvidence.DuplicateOccurrenceCount} | duplicate_values_single_context={duplicateEvidence.DuplicateValuesWithSingleContext} | duplicate_values_multiple_contexts={duplicateEvidence.DuplicateValuesWithMultipleContexts} | valid_context_occurrences={duplicateEvidence.ValidContextOccurrenceCount} | distinct_contexts={duplicateEvidence.DistinctContextCount} | repeated_context_families={duplicateEvidence.ContextFamiliesWithMultipleOccurrences} | most_common_context={duplicateEvidence.MostCommonContextCount} | sample={sample} | relative_words={relativeSummary}";
            }
        }

        var headerStems = string.Equals(Path.GetFileName(path), "eventspa.evt", StringComparison.OrdinalIgnoreCase)
            ? new[] { "pcpahdr" }
            : new[] { "djhdr_bo", "djhdr_en", "djhdr_fr", "djhdr_ge", "djhdr_it", "djhdr_rr", "djhdr_sp" };
        var windowSummaries = new List<string>();
        foreach (var headerStem in headerStems)
        {
            var headerOffPath = $"data/audio/{headerStem}.off";
            var headerBigPath = $"data/audio/{headerStem}.big";
            if (!audioPaths.Contains(headerOffPath) || !audioPaths.Contains(headerBigPath))
            {
                continue;
            }

            using var headerOffStream = audioFileSystem.OpenReadOnly(headerOffPath);
            using var headerBigStream = audioFileSystem.OpenReadOnly(headerBigPath);
            var headerTable = AudioAssetReader.InspectOffsetTable(
                ReadBounded(headerOffStream, 1 * 1024 * 1024), headerBigStream.Length);
            var headerWords = AudioAssetReader.ReadCompanionLeadingWords(headerBigStream, headerTable);
            var occurrences = AudioAssetReader.FindAlignedRawWordOccurrences(
                eventData, headerWords.Select(value => (uint)value));
            var spacing = AudioAssetReader.InspectOccurrenceSpacing(occurrences);
            if (spacing.OccurrenceCount > 0)
            {
                windowSummaries.Add($"{headerStem}=[candidates:{headerWords.Distinct().Count()},occurrences:{spacing.OccurrenceCount},distinct_values:{spacing.DistinctValueCount},first:0x{spacing.FirstOffset:X},last:0x{spacing.LastOffset:X},min_gap:{spacing.MinGap},max_gap:{spacing.MaxGap},distinct_gaps:{spacing.DistinctGapCount},common_gap:{spacing.MostCommonGap}x{spacing.MostCommonGapCount}]");
            }
        }
        eventHeaderWindowSummary = string.Join(";", windowSummaries.DefaultIfEmpty("none"));

        Console.WriteLine($"evt: {path} | bytes={evidence.AssetLength} | prefix={evidence.PrefixHex} | observed_prefix={evidence.HasObservedPrefix} | word_0x04=0x{evidence.WordAt4:X8} | word_0x04_matches_length={evidence.WordAt4MatchesAssetLength} | word_0x04_within_asset={evidence.WordAt4WithinAsset} | text_runs={evidence.TextRuns.Count} | text={textSummary} | raw_word_matches_to_off_starts={string.Join(";", rawMatchSummary.DefaultIfEmpty("none"))} | raw_word_matches_to_companion_leading_words={string.Join(";", leadingWordSummary.DefaultIfEmpty("none"))} | raw_word_matches_to_pcpadat_schl_fields={schlFieldSummary} | eventspa_pcpahdr_occurrences={eventHeaderOccurrenceSummary} | event_header_windows={eventHeaderWindowSummary}");
    }

    Console.WriteLine("relationships: OFF ranges and SCHl prefixes prove same-stem .big data segments; EVT text/IDs still have no direct ABKC reference in this limited reader.");
    return 0;
}

if (string.Equals(args[0], "texture-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc texture-preview <caminho-do-iso> <arquivo.xbx> <saida.ppm>");
        return 2;
    }

    using var textureStore = new GameAssetStore(args[1]);
    using var textureStream = textureStore.OpenRaw(args[2]);
    if (textureStream.Length > 64 * 1024 * 1024)
    {
        Console.Error.WriteLine("Textura grande demais para o preview limitado.");
        return 2;
    }
    using var textureMemory = new MemoryStream();
    textureStream.CopyTo(textureMemory);
    var textureAsset = TextureAssetLoader.Load(textureMemory.ToArray());
    TexturePreviewWriter.SavePpm(textureAsset, args[3]);
    Console.WriteLine($"{GameIdentity.Name} PC XPR texture preview");
    Console.WriteLine($"path: {args[2]}");
    Console.WriteLine($"dimensions: {textureAsset.Width}x{textureAsset.Height}");
    Console.WriteLine($"pixel_format: {textureAsset.PixelFormatName}");
    Console.WriteLine($"output: {Path.GetFullPath(args[3])}");
    Console.WriteLine("mode: ISO read-only, XPR0 base-level format decode, PPM output");
    return 0;
}

if (string.Equals(args[0], "audio-export", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-export <caminho-do-iso> <arquivo.ast> <saida.wav>");
        return 2;
    }

    using var audioStore = new GameAssetStore(args[1]);
    using var audioStream = audioStore.OpenRaw(args[2]);
    var exportedAudio = EaSchlAudioDecoder.ExportWave(audioStream, args[3]);
    Console.WriteLine($"{GameIdentity.Name} PC EA-XA audio conversion");
    Console.WriteLine($"source: {args[2]}");
    Console.WriteLine($"platform/version/codec: {exportedAudio.Stream.Platform}/{exportedAudio.Stream.Version}/0x{exportedAudio.Stream.Codec:X2}");
    Console.WriteLine($"format: PCM16 | {exportedAudio.Stream.Channels} channel(s) | {exportedAudio.Stream.SampleRate} Hz");
    Console.WriteLine($"sample_frames: {exportedAudio.SampleFrames} | blocks: {exportedAudio.Blocks} | data_bytes: {exportedAudio.DataBytes}");
    Console.WriteLine($"output: {Path.GetFullPath(args[3])}");
    Console.WriteLine("mode: SCHl/SCDl parse, EA-XA v3 decode, RIFF/WAVE output; SCCl remains unsupported");
    return 0;
}

if (string.Equals(args[0], "audio-play", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc audio-play <caminho-do-iso> <arquivo.ast>");
        return 2;
    }

    using var audioStore = new GameAssetStore(args[1]);
    using var audioStream = audioStore.OpenRaw(args[2]);
    var audioInfo = EaSchlAudioDecoder.Inspect(audioStream);
    var wave = EaSchlAudioDecoder.DecodeWave(audioStream);
    Console.WriteLine($"{GameIdentity.Name} PC EA-XA audio playback");
    Console.WriteLine($"source: {args[2]}");
    Console.WriteLine($"format: PCM16 | {audioInfo.Channels} channel(s) | {audioInfo.SampleRate} Hz");
    Console.WriteLine($"decoded_bytes: {wave.Length - 44}");
    Console.WriteLine("output: Windows audio device (synchronous playback)");
    Win32WavePlayer.PlaySynchronously(wave);
    Console.WriteLine("playback: complete");
    return 0;
}

if (string.Equals(args[0], "shpx-image-entry-audit", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc shpx-image-entry-audit <caminho-do-iso> <arquivo.xsh> [--report <saida.tsv>]");
        return 2;
    }

    var imageAuditIsoPath = Path.GetFullPath(args[1]);
    var imageAuditReportPath = Path.Combine(
        Directory.GetParent(imageAuditIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "shpx-image-entry-audit.tsv");
    for (var argumentIndex = 3; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            imageAuditReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            break;
        }
    }

    if (string.Equals(imageAuditIsoPath, imageAuditReportPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("A auditoria SHPX não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var imageAuditStore = new GameAssetStore(imageAuditIsoPath);
    using var imageAuditStream = imageAuditStore.OpenRaw(args[2]);
    if (imageAuditStream.Length > 64 * 1024 * 1024)
    {
        Console.Error.WriteLine("Pacote SHPX grande demais para a auditoria limitada a 64 MiB.");
        return 2;
    }
    using var imageAuditMemory = new MemoryStream();
    imageAuditStream.CopyTo(imageAuditMemory);
    var imageAuditRows = ShpxImageEntryAudit.Audit(args[2], imageAuditMemory.ToArray());
    ShpxImageEntryAudit.WriteReport(imageAuditReportPath, imageAuditRows);
    var imageAuditFirst = imageAuditRows[0];
    Console.WriteLine($"shpx_image_entry_audit: {imageAuditReportPath} | asset={args[2]} | entries={imageAuditRows.Count}");
    Console.WriteLine($"source_sha256: {imageAuditFirst.SourceSha256} | source_bytes={imageAuditFirst.SourceBytes} | declared_size_matches={imageAuditFirst.DeclaredSizeMatches}");
    Console.WriteLine($"decoded_entries: {imageAuditRows.Count(row => string.Equals(row.DecodeStatus, "decoded", StringComparison.Ordinal))} | unsupported_entries: {imageAuditRows.Count(row => string.Equals(row.DecodeStatus, "unsupported", StringComparison.Ordinal))}");
    Console.WriteLine($"ordered_entry_table_sha256: {imageAuditFirst.OrderedEntryTableSha256}");
    Console.WriteLine($"ordered_decoded_entry_sha256: {imageAuditFirst.OrderedDecodedEntrySha256}");
    Console.WriteLine($"status: {imageAuditFirst.AuditStatus}; image-entry decode/order evidence only; timing, pose, rig, weight, frame sequencing, animation, material, UV, skinning, morph, and GPU semantics not assigned");
    Console.WriteLine("mode: ISO read-only, bounded SHPX decode, per-entry decoded-pixel hashes");
    return imageAuditRows.All(row => string.Equals(row.AuditStatus, "verified-image-entries", StringComparison.Ordinal)) ? 0 : 1;
}

if (string.Equals(args[0], "shpx-export-crosscheck", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc shpx-export-crosscheck <caminho-do-iso> <arquivo.xsh> <diretório-export> [--report <saida.tsv>] [--summary <saida.tsv>]");
        return 2;
    }

    var exportCrosscheckIsoPath = Path.GetFullPath(args[1]);
    var exportCrosscheckReportPath = Path.Combine(
        Directory.GetParent(exportCrosscheckIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "shpx-export-crosscheck.tsv");
    var exportCrosscheckSummaryPath = Path.Combine(
        Directory.GetParent(exportCrosscheckIsoPath)?.FullName ?? Directory.GetCurrentDirectory(),
        "fifa_pc",
        "shpx-export-crosscheck-summary.tsv");
    for (var argumentIndex = 4; argumentIndex + 1 < args.Length; argumentIndex++)
    {
        if (string.Equals(args[argumentIndex], "--report", StringComparison.OrdinalIgnoreCase))
        {
            exportCrosscheckReportPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
        else if (string.Equals(args[argumentIndex], "--summary", StringComparison.OrdinalIgnoreCase))
        {
            exportCrosscheckSummaryPath = Path.GetFullPath(args[argumentIndex + 1]);
            argumentIndex++;
        }
    }

    if (string.Equals(exportCrosscheckIsoPath, exportCrosscheckReportPath, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(exportCrosscheckIsoPath, exportCrosscheckSummaryPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("O cross-check SHPX/TGA não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var exportCrosscheckStore = new GameAssetStore(exportCrosscheckIsoPath);
    using var exportCrosscheckStream = exportCrosscheckStore.OpenRaw(args[2]);
    if (exportCrosscheckStream.Length > 64 * 1024 * 1024)
    {
        Console.Error.WriteLine("Pacote SHPX grande demais para o cross-check limitado a 64 MiB.");
        return 2;
    }
    using var exportCrosscheckMemory = new MemoryStream();
    exportCrosscheckStream.CopyTo(exportCrosscheckMemory);
    var exportCrosscheckRows = ShpxExportCrosscheck.Audit(
        args[2],
        exportCrosscheckMemory.ToArray(),
        args[3]);
    var exportCrosscheckSummary = ShpxExportCrosscheck.Summarize(
        args[2],
        args[3],
        exportCrosscheckRows);
    ShpxExportCrosscheck.WriteReport(exportCrosscheckReportPath, exportCrosscheckRows);
    ShpxExportCrosscheck.WriteSummary(exportCrosscheckSummaryPath, exportCrosscheckSummary);
    Console.WriteLine($"shpx_export_crosscheck: {exportCrosscheckReportPath} | summary={exportCrosscheckSummaryPath} | asset={args[2]}");
    Console.WriteLine($"source_sha256: {exportCrosscheckSummary.SourceSha256} | entries={exportCrosscheckSummary.EntryCount}");
    Console.WriteLine($"exports_present: {exportCrosscheckSummary.ExportFilesPresent} | tga_header_valid: {exportCrosscheckSummary.HeaderValid} | pixel_matches: {exportCrosscheckSummary.PixelMatches}");
    Console.WriteLine($"status: {exportCrosscheckSummary.Status}; persisted image pixels only; export provenance={exportCrosscheckSummary.ExportProvenance}; original semantics={exportCrosscheckSummary.OriginalSemantics}");
    Console.WriteLine("mode: ISO read-only, bounded SHPX decode, workspace TGA byte/pixel cross-check; timing, pose, rig, animation, material, UV, skinning, morph, and GPU semantics not assigned");
    return string.Equals(exportCrosscheckSummary.Status, "verified-export-pixel-match", StringComparison.Ordinal) ? 0 : 1;
}

if (string.Equals(args[0], "shpx-export", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 4)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc shpx-export <caminho-do-iso> <arquivo.xsh> <diretório>");
        return 2;
    }

    using var shapeStore = new GameAssetStore(args[1]);
    using var shapeStream = shapeStore.OpenRaw(args[2]);
    if (shapeStream.Length > 64 * 1024 * 1024)
    {
        Console.Error.WriteLine("Pacote SHPX grande demais para a conversão limitada a 64 MiB.");
        return 2;
    }
    using var shapeMemory = new MemoryStream();
    shapeStream.CopyTo(shapeMemory);
    var exportedShapes = ShapeImageTextureWriter.ExportAll(shapeMemory.ToArray(), args[3]);
    Console.WriteLine($"{GameIdentity.Name} PC SHPX image conversion");
    Console.WriteLine($"source: {args[2]}");
    Console.WriteLine($"written_images: {exportedShapes.WrittenFiles.Count}");
    foreach (var path in exportedShapes.WrittenFiles)
    {
        Console.WriteLine($"image: {path}");
    }
    foreach (var item in exportedShapes.Unsupported)
    {
        Console.WriteLine($"skipped: {item.Entry.Name} | {item.UnsupportedReason}");
    }
    Console.WriteLine("mode: SHPX entry parse, DXT1/DXT3/PAL8 decode, transparent TGA output");
    return 0;
}

if (string.Equals(args[0], "shpx-frame-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 5 || !int.TryParse(args[3], out var frameIndex) || frameIndex < 0)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc shpx-frame-preview <caminho-do-iso> <arquivo.xsh> <índice-do-quadro> <saida.ppm>");
        return 2;
    }

    using var previewStore = new GameAssetStore(args[1]);
    using var previewStream = previewStore.OpenRaw(args[2]);
    if (previewStream.Length > 64 * 1024 * 1024)
    {
        Console.Error.WriteLine("Pacote SHPX grande demais para a prévia limitada a 64 MiB.");
        return 2;
    }
    using var previewMemory = new MemoryStream();
    previewStream.CopyTo(previewMemory);
    var frames = ShapeImageTextureLoader.DecodeSupported(previewMemory.ToArray());
    if (frameIndex >= frames.Count)
    {
        Console.Error.WriteLine($"Índice de quadro fora do pacote: {frameIndex} (quadros: {frames.Count}).");
        return 2;
    }
    var selectedFrame = frames[frameIndex];
    if (selectedFrame.Texture is null)
    {
        Console.Error.WriteLine($"Quadro não suportado: {selectedFrame.Entry.Name} | {selectedFrame.UnsupportedReason}");
        return 2;
    }
    TexturePreviewWriter.SavePpm(selectedFrame.Texture, args[4]);
    Console.WriteLine($"{GameIdentity.Name} PC SHPX frame preview");
    Console.WriteLine($"source: {args[2]} | frame: {frameIndex} | name: {selectedFrame.Entry.Name}");
    Console.WriteLine($"format: {selectedFrame.Texture.PixelFormat} | dimensions: {selectedFrame.Texture.Width}x{selectedFrame.Texture.Height}");
    Console.WriteLine($"output: {Path.GetFullPath(args[4])} (alpha composited over checkerboard)");
    return 0;
}

if (string.Equals(args[0], "vivf-nested-shpx-export", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 6 || !int.TryParse(args[3], out var shapeBlockIndex))
    {
        Console.Error.WriteLine("Uso: FifaStreetPc vivf-nested-shpx-export <caminho-do-iso> <arquivo.bh> <índice> <entrada.xsh> <diretório>");
        return 2;
    }

    using var nestedShapeStore = new GameAssetStore(args[1]);
    using var shapeBlock = nestedShapeStore.OpenVivFBlock(args[2], shapeBlockIndex);
    var expandedShapeBlock = RefPackDecoder.Decode(shapeBlock.Data, checked((int)shapeBlock.Record.ExpandedSize));
    var nestedShape = BigArchiveReader.ReadEntry(expandedShapeBlock.Data, args[4]);
    var nestedExport = ShapeImageTextureWriter.ExportAll(nestedShape, args[5]);
    Console.WriteLine($"{GameIdentity.Name} PC nested SHPX image conversion");
    Console.WriteLine($"package: {args[2]} | block: {shapeBlockIndex} | entry: {args[4]}");
    Console.WriteLine($"written_images: {nestedExport.WrittenFiles.Count}");
    foreach (var path in nestedExport.WrittenFiles)
    {
        Console.WriteLine($"image: {path}");
    }
    foreach (var item in nestedExport.Unsupported)
    {
        Console.WriteLine($"skipped: {item.Entry.Name} | {item.UnsupportedReason}");
    }
    Console.WriteLine("mode: RefPack decode, nested BIG read, SHPX parse, DXT1/DXT3/PAL8 decode");
    return 0;
}

if (string.Equals(args[0], "inventory", StringComparison.OrdinalIgnoreCase))
{
    using var inventoryFileSystem = new XdvdfsFileSystem(args[1]);
    var inventoryCatalog = GameAssetCatalog.Scan(inventoryFileSystem);
    Console.WriteLine($"{GameIdentity.Name} PC asset inventory");
    Console.WriteLine($"data_files: {inventoryCatalog.DataFiles.Count}");
    Console.WriteLine($"validated_big_archives: {inventoryCatalog.Archives.Count}");
    Console.WriteLine($"validated_vivf_packages: {inventoryCatalog.VivfPackages.Count}");
    foreach (var group in inventoryCatalog.Assets
                 .GroupBy(asset => asset.Kind)
                 .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal))
    {
        Console.WriteLine($"kind_{group.Key}: {group.Count()}");
    }

    foreach (var asset in inventoryCatalog.Assets)
    {
        Console.WriteLine($"asset: {asset.Kind} | {asset.Signature,-8} | {asset.Size,12} | {asset.Path}");
    }

    foreach (var package in inventoryCatalog.VivfPackages)
    {
        Console.WriteLine(
            $"vivf: entries={package.EntryCount,4} | index_valid={package.IndexValidated} " +
            $"| ranges_valid={package.CompanionRangesValid} | sentinel_valid={package.SentinelMatchesCompanion} " +
            $"| companion={package.CompanionPath} | {package.Path}");
    }
    return 0;
}

if (string.Equals(args[0], "render-preview", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 3)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc render-preview <caminho-do-iso> <saida.ppm> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--authored-hud] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
        return 2;
    }

    var previewIsoPath = Path.GetFullPath(args[1]);
    var previewOutputPath = Path.GetFullPath(args[2]);
    if (string.Equals(previewIsoPath, previewOutputPath, StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("render-preview não pode sobrescrever a ISO de origem.");
        return 2;
    }

    using var previewFileSystem = new XdvdfsFileSystem(args[1]);
    var previewCatalog = GameAssetCatalog.Scan(previewFileSystem);
    using var previewStore = new GameAssetStore(args[1]);
    using var previewTextureStream = previewStore.OpenRaw("data/titleimage.xbx");
    using var previewTextureMemory = new MemoryStream();
    previewTextureStream.CopyTo(previewTextureMemory);
    var previewTexture = TextureAssetLoader.Load(previewTextureMemory.ToArray());
    var previewMatch = new StreetMatch();
    var previewFrame = new SoftwareFrameBuffer(960, 540);
    var previewCleanRoomCharacters = args.Any(argument =>
        string.Equals(argument, "--clean-room-characters", StringComparison.OrdinalIgnoreCase));
    var previewCandidateHomeCharacter = args.Any(argument =>
        string.Equals(argument, "--candidate-home-character", StringComparison.OrdinalIgnoreCase));
    var previewCandidateAwayCharacter = args.Any(argument =>
        string.Equals(argument, "--candidate-away-character", StringComparison.OrdinalIgnoreCase));
    var previewAuthoredHud = args.Any(argument =>
        string.Equals(argument, "--authored-hud", StringComparison.OrdinalIgnoreCase));
    var previewCandidateMaterialOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--candidate-material", StringComparison.OrdinalIgnoreCase));
    var previewCandidateMaterialRequested = previewCandidateMaterialOption >= 0;
    var previewCandidateMaterialArchive = "tops0030.big";
    var previewCandidateMaterialShape = "tops";
    string? previewCandidateMaterialObject = null;
    if (previewCandidateMaterialRequested)
    {
        var optionValueIndex = previewCandidateMaterialOption + 1;
        if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
        {
            previewCandidateMaterialArchive = args[optionValueIndex++];
            if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
            {
                previewCandidateMaterialShape = args[optionValueIndex++];
                if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
                {
                    previewCandidateMaterialObject = args[optionValueIndex];
                }
            }
        }
    }
    EaglCandidateMesh? previewCandidateMesh = null;
    PcShapeImageTexture? previewCandidateTexture = null;
    var previewCandidateMaterialStatus = previewCandidateMaterialRequested ? "unavailable" : "disabled";
    if (previewCandidateMaterialRequested)
    {
        try
        {
            var materialAsset = previewCandidateMaterialObject is null
                ? EaglShapeMaterialLoader.LoadFirstForShape(
                    previewStore,
                    "data/models.big",
                    previewCandidateMaterialArchive,
                    previewCandidateMaterialShape)
                : EaglShapeMaterialLoader.LoadForObject(
                    previewStore,
                    "data/models.big",
                    previewCandidateMaterialArchive,
                    previewCandidateMaterialObject,
                    previewCandidateMaterialShape);
            var candidateAsset = EaglCandidateAssetLoader.Load(
                previewStore,
                "data/models.big",
                materialAsset.ModelArchiveName,
                materialAsset.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidateAsset.Primitives,
                    materialAsset.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{materialAsset.ShapeName}'.");
            }

            var renderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
                candidateAsset.Mesh,
                materialAsset.ShapeName);
            previewCandidateMesh = candidateAsset.Mesh;
            previewCandidateTexture = materialAsset.Texture;
            previewCandidateMaterialStatus = "validated-candidate";
            Console.WriteLine(
                $"candidate_material: {materialAsset.ModelArchiveName} | object={materialAsset.ModelObjectName} | " +
                $"shape={materialAsset.ShapeName} | object_state=exact-shapename | " +
                $"render_group_state={(renderGroupState ? "exact-shapename" : "unproven")} | " +
                $"texture={materialAsset.Texture.Width}x{materialAsset.Texture.Height} {materialAsset.Texture.PixelFormat}");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException or NotSupportedException)
        {
            Console.WriteLine($"candidate_material: unavailable | {exception.Message}");
        }
    }
    PreviewRenderer.Render(
        previewMatch,
        previewFrame,
        previewTexture,
        candidateMesh: previewCandidateMesh,
        candidateTexture: previewCandidateTexture,
        cleanRoomCharacters: previewCleanRoomCharacters,
        candidateHomeCharacter: previewCandidateHomeCharacter,
        candidateAwayCharacter: previewCandidateAwayCharacter,
        showAuthoredHud: previewAuthoredHud);
    previewFrame.SavePpm(previewOutputPath);
    Console.WriteLine($"{GameIdentity.Name} PC clean-room software preview");
    Console.WriteLine($"output: {previewOutputPath}");
    Console.WriteLine($"catalog_data_files: {previewCatalog.DataFiles.Count}");
    Console.WriteLine($"catalog_texture_images: {previewCatalog.TextureImages.Count(texture => texture.HeaderValidated)} validated");
    Console.WriteLine($"title_texture: {previewTexture.Width}x{previewTexture.Height} {previewTexture.PixelFormatName}");
    Console.WriteLine($"frame: {previewMatch.Frame}");
    Console.WriteLine($"hud: {previewMatch.HudText()}");
    Console.WriteLine("renderer: managed .NET software framebuffer, PPM output");
    Console.WriteLine($"characters: {(previewCleanRoomCharacters ? "clean-room-procedural" : "legacy-shapes")}");
    Console.WriteLine($"candidate_home_character: {(previewCandidateHomeCharacter && previewCandidateMesh is not null ? "enabled-rigid-candidate" : "disabled")}");
    Console.WriteLine($"candidate_away_character: {(previewCandidateAwayCharacter && previewCandidateMesh is not null ? "enabled-rigid-candidate" : "disabled")}");
    Console.WriteLine($"authored_hud: {(previewAuthoredHud ? "enabled" : "disabled")}");
    Console.WriteLine($"candidate_material_status: {previewCandidateMaterialStatus}");
    if (previewCandidateMesh is not null)
    {
        Console.WriteLine($"candidate_mesh: groups={previewCandidateMesh.Groups.Count} | vertices={previewCandidateMesh.VertexCount} | triangles={previewCandidateMesh.TriangleCount}");
    }
    Console.WriteLine($"frame_checksum: {previewFrame.ComputeChecksum():X16}");
    Console.WriteLine("status: candidate material path is opt-in diagnostic; UV, skinning, deformation and original GPU draw semantics remain unverified");
    Console.WriteLine("source: original ISO, read-only");
    return 0;
}

if (string.Equals(args[0], "run-windowed", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.Error.WriteLine("Uso: FifaStreetPc run-windowed <caminho-do-iso> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--round-reset] [--match-seconds <segundos>] [--run-seconds <segundos>] [--capture-frame <saida.ppm>] [--match-rule timed|first-to-goals|goals-or-panna] [--target-goals <gols>] [--target-pannas <pannas>] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
        return 2;
    }

    using var windowedFileSystem = new XdvdfsFileSystem(args[1]);
    var windowedCatalog = GameAssetCatalog.Scan(windowedFileSystem);
    using var windowedStore = new GameAssetStore(args[1]);
    using var titleStream = windowedStore.OpenRaw("data/titleimage.xbx");
    using var titleMemory = new MemoryStream();
    titleStream.CopyTo(titleMemory);
    var titleTexture = TextureAssetLoader.Load(titleMemory.ToArray());
    EaglCandidateMesh? candidateMesh = null;
    PcShapeImageTexture? candidateTexture = null;
    var windowedCleanRoomCharacters = args.Any(argument =>
        string.Equals(argument, "--clean-room-characters", StringComparison.OrdinalIgnoreCase));
    var windowedCandidateHomeCharacter = args.Any(argument =>
        string.Equals(argument, "--candidate-home-character", StringComparison.OrdinalIgnoreCase));
    var windowedCandidateAwayCharacter = args.Any(argument =>
        string.Equals(argument, "--candidate-away-character", StringComparison.OrdinalIgnoreCase));
    var windowedCleanRoomRoundReset = args.Any(argument =>
        string.Equals(argument, "--round-reset", StringComparison.OrdinalIgnoreCase));
    var windowedMatchDurationSeconds = 300.0;
    var windowedMatchSecondsOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--match-seconds", StringComparison.OrdinalIgnoreCase));
    if (windowedMatchSecondsOption >= 0)
    {
        if (windowedMatchSecondsOption + 1 >= args.Length ||
            !double.TryParse(
                args[windowedMatchSecondsOption + 1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out windowedMatchDurationSeconds) ||
            double.IsNaN(windowedMatchDurationSeconds) ||
            double.IsInfinity(windowedMatchDurationSeconds) ||
            windowedMatchDurationSeconds <= 0)
        {
            Console.Error.WriteLine("--match-seconds requer um número positivo.");
            return 2;
        }
    }
    var windowedRunSeconds = 0.0;
    var windowedRunSecondsOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--run-seconds", StringComparison.OrdinalIgnoreCase));
    if (windowedRunSecondsOption >= 0)
    {
        if (windowedRunSecondsOption + 1 >= args.Length ||
            !double.TryParse(
                args[windowedRunSecondsOption + 1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out windowedRunSeconds) ||
            double.IsNaN(windowedRunSeconds) ||
            double.IsInfinity(windowedRunSeconds) ||
            windowedRunSeconds <= 0)
        {
            Console.Error.WriteLine("--run-seconds requer um número positivo.");
            return 2;
        }
    }
    var windowedCaptureFramePath = (string?)null;
    var windowedCaptureFrameOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--capture-frame", StringComparison.OrdinalIgnoreCase));
    if (windowedCaptureFrameOption >= 0)
    {
        if (windowedCaptureFrameOption + 1 >= args.Length ||
            args[windowedCaptureFrameOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("--capture-frame requer um caminho .ppm.");
            return 2;
        }

        windowedCaptureFramePath = args[windowedCaptureFrameOption + 1];
    }
    var windowedMatchEndCondition = CleanRoomMatchEndCondition.Timed;
    var windowedMatchRuleOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--match-rule", StringComparison.OrdinalIgnoreCase));
    if (windowedMatchRuleOption >= 0)
    {
        if (windowedMatchRuleOption + 1 >= args.Length ||
            args[windowedMatchRuleOption + 1].StartsWith("--", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("--match-rule requer timed, first-to-goals ou goals-or-panna.");
            return 2;
        }

        var windowedMatchRuleValue = args[windowedMatchRuleOption + 1].ToLowerInvariant();
        if (windowedMatchRuleValue == "timed")
        {
            windowedMatchEndCondition = CleanRoomMatchEndCondition.Timed;
        }
        else if (windowedMatchRuleValue == "first-to-goals")
        {
            windowedMatchEndCondition = CleanRoomMatchEndCondition.FirstToGoals;
        }
        else if (windowedMatchRuleValue == "goals-or-panna")
        {
            windowedMatchEndCondition = CleanRoomMatchEndCondition.GoalsOrPanna;
        }
        else
        {
            Console.Error.WriteLine("--match-rule deve ser timed, first-to-goals ou goals-or-panna.");
            return 2;
        }
    }
    var windowedTargetGoalsOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--target-goals", StringComparison.OrdinalIgnoreCase));
    var windowedTargetGoals = 0;
    if (windowedTargetGoalsOption >= 0)
    {
        if (windowedTargetGoalsOption + 1 >= args.Length ||
            !int.TryParse(args[windowedTargetGoalsOption + 1], NumberStyles.None, CultureInfo.InvariantCulture, out windowedTargetGoals) ||
            windowedTargetGoals <= 0)
        {
            Console.Error.WriteLine("--target-goals requer um inteiro positivo.");
            return 2;
        }
    }
    var windowedTargetPannasOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--target-pannas", StringComparison.OrdinalIgnoreCase));
    var windowedTargetPannas = 0;
    if (windowedTargetPannasOption >= 0)
    {
        if (windowedTargetPannasOption + 1 >= args.Length ||
            !int.TryParse(args[windowedTargetPannasOption + 1], NumberStyles.None, CultureInfo.InvariantCulture, out windowedTargetPannas) ||
            windowedTargetPannas <= 0)
        {
            Console.Error.WriteLine("--target-pannas requer um inteiro positivo.");
            return 2;
        }
    }
    if (windowedMatchEndCondition == CleanRoomMatchEndCondition.FirstToGoals &&
        windowedTargetGoalsOption < 0)
    {
        Console.Error.WriteLine("--match-rule first-to-goals requer --target-goals.");
        return 2;
    }
    if (windowedMatchEndCondition == CleanRoomMatchEndCondition.GoalsOrPanna &&
        (windowedTargetGoalsOption < 0 || windowedTargetPannasOption < 0))
    {
        Console.Error.WriteLine("--match-rule goals-or-panna requer --target-goals e --target-pannas.");
        return 2;
    }
    if (windowedMatchEndCondition != CleanRoomMatchEndCondition.GoalsOrPanna &&
        windowedTargetPannasOption >= 0)
    {
        Console.Error.WriteLine("--target-pannas só pode ser usado com --match-rule goals-or-panna.");
        return 2;
    }
    if (windowedMatchEndCondition == CleanRoomMatchEndCondition.Timed &&
        windowedTargetGoalsOption >= 0)
    {
        Console.Error.WriteLine("--target-goals só pode ser usado com --match-rule first-to-goals.");
        return 2;
    }
    var windowedCandidateMaterialOption = Array.FindIndex(args, argument =>
        string.Equals(argument, "--candidate-material", StringComparison.OrdinalIgnoreCase));
    var windowedCandidateMaterialRequested = windowedCandidateMaterialOption >= 0;
    var windowedCandidateMaterialArchive = "tops0030.big";
    var windowedCandidateMaterialShape = "tops";
    string? windowedCandidateMaterialObject = null;
    if (windowedCandidateMaterialRequested)
    {
        var optionValueIndex = windowedCandidateMaterialOption + 1;
        if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
        {
            windowedCandidateMaterialArchive = args[optionValueIndex++];
            if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
            {
                windowedCandidateMaterialShape = args[optionValueIndex++];
                if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
                {
                    windowedCandidateMaterialObject = args[optionValueIndex];
                }
            }
        }
    }
    var windowedCandidateMaterialStatus = windowedCandidateMaterialRequested ? "unavailable" : "disabled";
    if (windowedCandidateMaterialRequested)
    {
        try
        {
            var materialAsset = windowedCandidateMaterialObject is null
                ? EaglShapeMaterialLoader.LoadFirstForShape(
                    windowedStore,
                    "data/models.big",
                    windowedCandidateMaterialArchive,
                    windowedCandidateMaterialShape)
                : EaglShapeMaterialLoader.LoadForObject(
                    windowedStore,
                    "data/models.big",
                    windowedCandidateMaterialArchive,
                    windowedCandidateMaterialObject,
                    windowedCandidateMaterialShape);
            var candidateAsset = EaglCandidateAssetLoader.Load(
                windowedStore,
                "data/models.big",
                materialAsset.ModelArchiveName,
                materialAsset.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidateAsset.Primitives,
                    materialAsset.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{materialAsset.ShapeName}'.");
            }

            var renderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
                candidateAsset.Mesh,
                materialAsset.ShapeName);
            candidateMesh = candidateAsset.Mesh;
            candidateTexture = materialAsset.Texture;
            windowedCandidateMaterialStatus = "validated-candidate";
            Console.WriteLine(
                $"candidate_material: {materialAsset.ModelArchiveName} | object={materialAsset.ModelObjectName} | " +
                $"shape={materialAsset.ShapeName} | object_state=exact-shapename | " +
                $"render_group_state={(renderGroupState ? "exact-shapename" : "unproven")} | " +
                $"texture={materialAsset.Texture.Width}x{materialAsset.Texture.Height} {materialAsset.Texture.PixelFormat}");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException or NotSupportedException)
        {
            Console.WriteLine($"candidate_material: unavailable | {exception.Message}");
        }
    }
    if (candidateMesh is null)
    {
        try
        {
            var candidateAsset = EaglCandidateAssetLoader.Load(
                windowedStore,
                "data/models.big",
                "star0001.big",
                "star0001-head.ord");
            candidateMesh = candidateAsset.Mesh;
            Console.WriteLine($"candidate_mesh: {candidateAsset.ObjectName} | groups={candidateMesh.Groups.Count} | vertices={candidateMesh.VertexCount} | triangles={candidateMesh.TriangleCount}");
        }
        catch (Exception exception) when (
            exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException)
        {
            Console.WriteLine($"candidate_mesh: unavailable | {exception.Message}");
        }
    }
    Console.WriteLine($"{GameIdentity.Name} PC windowed runtime | data_files={windowedCatalog.DataFiles.Count}");
    Console.WriteLine($"audio_streams={windowedCatalog.AudioStreams.Count} | audio_headers_validated={windowedCatalog.AudioStreams.Count(stream => stream.HeaderValidated)}");
    Console.WriteLine($"texture_images={windowedCatalog.TextureImages.Count} | texture_headers_validated={windowedCatalog.TextureImages.Count(texture => texture.HeaderValidated)}");
    Console.WriteLine($"texture_decodes_validated={windowedCatalog.TextureImages.Count(texture => texture.DecodeValidated)}");
    Console.WriteLine($"candidate_material_status={windowedCandidateMaterialStatus}");
    Console.WriteLine($"render_characters={(windowedCleanRoomCharacters ? "clean-room-procedural" : "legacy-shapes")}");
    Console.WriteLine($"candidate_home_character={(windowedCandidateHomeCharacter ? "enabled-rigid-candidate" : "disabled")}");
    Console.WriteLine($"candidate_away_character={(windowedCandidateAwayCharacter ? "enabled-rigid-candidate" : "disabled")}");
    Console.WriteLine($"clean_room_round_reset={(windowedCleanRoomRoundReset ? "enabled" : "disabled")}");
    Console.WriteLine($"match_duration_seconds={windowedMatchDurationSeconds:0.000}");
    Console.WriteLine($"match_end_condition={windowedMatchEndCondition}");
    Console.WriteLine($"match_target_goals={windowedTargetGoals}");
    Console.WriteLine($"match_target_pannas={windowedTargetPannas}");
    Console.WriteLine($"run_seconds={(windowedRunSeconds > 0 ? windowedRunSeconds.ToString("0.000", CultureInfo.InvariantCulture) : "until-window-close")}");
    Console.WriteLine($"capture_frame={(windowedCaptureFramePath ?? "disabled")}");
    Console.WriteLine("Teclas: WASD mover | P passe | Espaço chute | T drible | N panna authored | J juggle authored | G gamebreaker authored | K tackle authored | fechar a janela para sair");
    Console.WriteLine($"asset: titleimage.xbx | {titleTexture.Width}x{titleTexture.Height} {titleTexture.PixelFormatName}");
    return Win32GameHost.Run(
        windowedCatalog,
        titleTexture,
        candidateMesh,
        candidateTexture,
        windowedFileSystem.OpenReadOnly,
        windowedCleanRoomCharacters,
        windowedCandidateHomeCharacter,
        windowedCandidateAwayCharacter,
        windowedMatchDurationSeconds,
        windowedCleanRoomRoundReset,
        windowedMatchEndCondition,
        windowedTargetGoals,
        windowedTargetPannas,
        windowedCaptureFramePath,
        windowedRunSeconds);
}

if (!string.Equals(args[0], "run-headless", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Comando desconhecido.");
    Console.Error.WriteLine("Uso: FifaStreetPc run-headless <caminho-do-iso> [frames] [--demo|--aimed-demo|--round-demo|--gamebreaker-demo|--juggle-demo|--steal-demo] [--input-script <entrada.tsv>] [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--frame-output <saida.ppm>] [--match-trace-output <saida.tsv>] [--match-trace-expected <entrada.tsv>] [--input-trace-output <saida.tsv>] [--input-trace-expected <entrada.tsv>] [--animation-trace-output <saida.tsv>] [--animation-trace-expected <entrada.tsv>] [--audio-trace-output <saida.tsv>] [--audio-trace-expected <entrada.tsv>] [--integration-report <saida.tsv>] [--match-seconds <segundos>] [--match-rule timed|first-to-goals|goals-or-panna] [--target-goals <gols>] [--target-pannas <pannas>] [--opponent-ai] [--play-audio] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc asset-smoke <caminho-do-iso> <arquivo.big> <entrada>");
    Console.Error.WriteLine("   ou: FifaStreetPc inventory <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-runtime-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-name-index <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-compare <caminho-do-iso> [--profile-report <saida.tsv>] [--pair-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-elf-structure <caminho-do-iso> [--profile-report <saida.tsv>] [--section-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-data-relocation-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-tail-compare <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-catalog-tail-range-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-tail-pair-relocation-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>] [--detail-report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc animation-bank-sidecar-boundary-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-runtime-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-pose-bank-index <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-block-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-decode-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-validate-smoke <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-big-smoke <caminho-do-iso> <arquivo.bh> <índice>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-big-validate <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-model-inventory <caminho-do-iso> <arquivo.bh>");
    Console.Error.WriteLine("   ou: FifaStreetPc big-model-inventory <caminho-do-iso> <arquivo.big> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-runtime-batch-smoke <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-asset-provenance-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-shape-state-primitive-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-geometry-stream-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-render-influence-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-geometry-audit <caminho-do-iso> [--report <saida.tsv>] [--pixel-size <pixels>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-attribute-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-render-chain-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-vertex-record-association-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-topology-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-opaque-discrimination-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-command-payload-identity-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-original-primitive-stream-command-descriptor-adjacency-audit <caminho-do-iso> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-projection-preview <caminho-do-iso> <nested.big> <SHAPENAME> <diretório-de-saída> [objeto.ord]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-candidate-state-correspondence <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-adjacency-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-profile-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-state-geometry-order-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-material-batch-preview <caminho-do-iso> <diretório-de-saída>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-smoke <caminho-do-iso> <nested.big> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-sibling-inventory <caminho-do-iso> <arquivo.big> [--catalog <saida.tsv>] [--ambiguity-report <saida.tsv>] [--byte-report <saida.tsv>] [--numeric-gap-report <saida.tsv>] [--numeric-gap-correlation-report <saida.tsv>] [--geometry-report <saida.tsv>] [--geometry-summary <saida.tsv>] [--geometry-correlation-report <saida.tsv>] [--geometry-correlation-summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-summary <ambiguity-report.tsv> <summary.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-byte-summary <ambiguity-report.tsv> <byte-report.tsv> <summary.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-group-metadata <ambiguity-report.tsv> <byte-report.tsv> <numeric-gap-report.tsv> <metadata.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc morph-ambiguity-independent-audit <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-shpx-structure-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-shpx-geometry-correlation-audit <structural-catalog.tsv> <shpx-audit.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-elf-header-audit <caminho-do-iso> <arquivo.big> <structural-catalog.tsv> <ambiguity-report.tsv> <audit.tsv> [--summary <summary.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-name-state-audit <morph-structural-catalog.tsv> <saida.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc eagl-morph-animation-name-negative-control <morph-structural-catalog.tsv> <animation-bank-name-index.tsv> <saida.tsv>");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-asset-smoke <caminho-do-iso> <arquivo.bh> <índice> <entrada>");
    Console.Error.WriteLine("   ou: FifaStreetPc asset-format-smoke <caminho-do-iso> <arquivo> [--verbose]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-structure-smoke <caminho-do-iso>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-inspect <caminho-do-iso> [arquivo.abk]");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-decode <caminho-do-iso> <arquivo.abk> <índice-do-som> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-abk-play <caminho-do-iso> <arquivo.abk> <índice-do-som>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-event-candidate-decode <caminho-do-iso> <row> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-sccl-channel-layout-audit <caminho-do-iso> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc texture-preview <caminho-do-iso> <arquivo.xbx> <saida.ppm>");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-export <caminho-do-iso> <arquivo.xsh> <diretório>");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-image-entry-audit <caminho-do-iso> <arquivo.xsh> [--report <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-export-crosscheck <caminho-do-iso> <arquivo.xsh> <diretório-export> [--report <saida.tsv>] [--summary <saida.tsv>]");
    Console.Error.WriteLine("   ou: FifaStreetPc vivf-nested-shpx-export <caminho-do-iso> <arquivo.bh> <índice> <entrada.xsh> <diretório>");
    Console.Error.WriteLine("   ou: FifaStreetPc shpx-frame-preview <caminho-do-iso> <arquivo.xsh> <índice-do-quadro> <saida.ppm>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-export <caminho-do-iso> <arquivo.ast> <saida.wav>");
    Console.Error.WriteLine("   ou: FifaStreetPc audio-play <caminho-do-iso> <arquivo.ast>");
    Console.Error.WriteLine("   ou: FifaStreetPc render-preview <caminho-do-iso> <saida.ppm> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc run-windowed <caminho-do-iso> [--clean-room-characters] [--candidate-home-character] [--candidate-away-character] [--round-reset] [--match-seconds <segundos>] [--run-seconds <segundos>] [--capture-frame <saida.ppm>] [--candidate-material [nested.big] [SHAPENAME] [objeto.ord]]");
    Console.Error.WriteLine("   ou: FifaStreetPc self-test");
    return 2;
}

var aimedDemo = args.Any(argument => string.Equals(argument, "--aimed-demo", StringComparison.OrdinalIgnoreCase));
var roundDemo = args.Any(argument => string.Equals(argument, "--round-demo", StringComparison.OrdinalIgnoreCase));
var gamebreakerDemo = args.Any(argument => string.Equals(argument, "--gamebreaker-demo", StringComparison.OrdinalIgnoreCase));
var juggleDemo = args.Any(argument => string.Equals(argument, "--juggle-demo", StringComparison.OrdinalIgnoreCase));
var stealDemo = args.Any(argument => string.Equals(argument, "--steal-demo", StringComparison.OrdinalIgnoreCase));
var tackleDemo = args.Any(argument => string.Equals(argument, "--tackle-demo", StringComparison.OrdinalIgnoreCase));
var cleanRoomCharacters = args.Any(argument => string.Equals(argument, "--clean-room-characters", StringComparison.OrdinalIgnoreCase));
var candidateHomeCharacter = args.Any(argument => string.Equals(argument, "--candidate-home-character", StringComparison.OrdinalIgnoreCase));
var candidateAwayCharacter = args.Any(argument => string.Equals(argument, "--candidate-away-character", StringComparison.OrdinalIgnoreCase));
var demoInput = aimedDemo || roundDemo || gamebreakerDemo || juggleDemo || stealDemo || tackleDemo || args.Any(argument => string.Equals(argument, "--demo", StringComparison.OrdinalIgnoreCase));
var inputScriptOption = Array.FindIndex(args, argument =>
    string.Equals(argument, "--input-script", StringComparison.OrdinalIgnoreCase));
MatchInputScript? inputScript = null;
string? inputScriptPath = null;
if (inputScriptOption >= 0)
{
    var inputScriptValueIndex = inputScriptOption + 1;
    if (inputScriptValueIndex >= args.Length || args[inputScriptValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--input-script requer o caminho de um arquivo TSV.");
        return 2;
    }

    inputScriptPath = Path.GetFullPath(args[inputScriptValueIndex]);
    if (string.Equals(inputScriptPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--input-script não pode apontar para a ISO de origem.");
        return 2;
    }

    try
    {
        inputScript = MatchInputScriptReader.Read(inputScriptPath);
    }
    catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
    {
        Console.Error.WriteLine($"--input-script inválido: {exception.Message}");
        return 2;
    }
}
if (inputScript is not null && (aimedDemo || gamebreakerDemo || juggleDemo || stealDemo || tackleDemo || args.Any(argument =>
        string.Equals(argument, "--demo", StringComparison.OrdinalIgnoreCase))))
{
    Console.Error.WriteLine("--input-script não pode ser combinado com --demo, --aimed-demo, --gamebreaker-demo, --juggle-demo, --steal-demo ou --tackle-demo; --round-demo pode ser usado apenas para habilitar o reset authored.");
    return 2;
}
var opponentAi = args.Any(argument => string.Equals(argument, "--opponent-ai", StringComparison.OrdinalIgnoreCase));
var playAudio = args.Any(argument => string.Equals(argument, "--play-audio", StringComparison.OrdinalIgnoreCase));
var matchDurationSeconds = 300.0;
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (!string.Equals(args[argumentIndex], "--match-seconds", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    if (!double.TryParse(
            args[argumentIndex + 1],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out matchDurationSeconds) ||
        double.IsNaN(matchDurationSeconds) ||
        double.IsInfinity(matchDurationSeconds) ||
        matchDurationSeconds <= 0)
    {
        Console.Error.WriteLine("--match-seconds requer um número positivo.");
        return 2;
    }
    break;
}
var matchEndCondition = CleanRoomMatchEndCondition.Timed;
var matchRuleOption = Array.FindIndex(args, argument =>
    string.Equals(argument, "--match-rule", StringComparison.OrdinalIgnoreCase));
if (matchRuleOption >= 0)
{
    if (matchRuleOption + 1 >= args.Length || args[matchRuleOption + 1].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--match-rule requer timed, first-to-goals ou goals-or-panna.");
        return 2;
    }

    var matchRuleValue = args[matchRuleOption + 1].ToLowerInvariant();
    if (matchRuleValue == "timed")
    {
        matchEndCondition = CleanRoomMatchEndCondition.Timed;
    }
    else if (matchRuleValue == "first-to-goals")
    {
        matchEndCondition = CleanRoomMatchEndCondition.FirstToGoals;
    }
    else if (matchRuleValue == "goals-or-panna")
    {
        matchEndCondition = CleanRoomMatchEndCondition.GoalsOrPanna;
    }
    else
    {
        Console.Error.WriteLine("--match-rule deve ser timed, first-to-goals ou goals-or-panna.");
        return 2;
    }
}
var targetGoalsOption = Array.FindIndex(args, argument =>
    string.Equals(argument, "--target-goals", StringComparison.OrdinalIgnoreCase));
var targetGoals = 0;
if (targetGoalsOption >= 0)
{
    if (targetGoalsOption + 1 >= args.Length ||
        !int.TryParse(args[targetGoalsOption + 1], NumberStyles.None, CultureInfo.InvariantCulture, out targetGoals) ||
        targetGoals <= 0)
    {
        Console.Error.WriteLine("--target-goals requer um inteiro positivo.");
        return 2;
    }
}
var targetPannasOption = Array.FindIndex(args, argument =>
    string.Equals(argument, "--target-pannas", StringComparison.OrdinalIgnoreCase));
var targetPannas = 0;
if (targetPannasOption >= 0)
{
    if (targetPannasOption + 1 >= args.Length ||
        !int.TryParse(args[targetPannasOption + 1], NumberStyles.None, CultureInfo.InvariantCulture, out targetPannas) ||
        targetPannas <= 0)
    {
        Console.Error.WriteLine("--target-pannas requer um inteiro positivo.");
        return 2;
    }
}
if (matchEndCondition == CleanRoomMatchEndCondition.FirstToGoals && targetGoalsOption < 0)
{
    Console.Error.WriteLine("--match-rule first-to-goals requer --target-goals.");
    return 2;
}
if (matchEndCondition == CleanRoomMatchEndCondition.GoalsOrPanna &&
    (targetGoalsOption < 0 || targetPannasOption < 0))
{
    Console.Error.WriteLine("--match-rule goals-or-panna requer --target-goals e --target-pannas.");
    return 2;
}
if (matchEndCondition != CleanRoomMatchEndCondition.GoalsOrPanna && targetPannasOption >= 0)
{
    Console.Error.WriteLine("--target-pannas só pode ser usado com --match-rule goals-or-panna.");
    return 2;
}
if (matchEndCondition == CleanRoomMatchEndCondition.Timed && targetGoalsOption >= 0)
{
    Console.Error.WriteLine("--target-goals só pode ser usado com --match-rule first-to-goals.");
    return 2;
}
string? frameOutputPath = null;
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--frame-output", StringComparison.OrdinalIgnoreCase))
    {
        frameOutputPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
string? matchTraceOutputPath = null;
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--match-trace-output", StringComparison.OrdinalIgnoreCase))
    {
        matchTraceOutputPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
string? audioTraceOutputPath = null;
string? inputTraceOutputPath = null;
string? animationTraceOutputPath = null;
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--input-trace-output", StringComparison.OrdinalIgnoreCase))
    {
        inputTraceOutputPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--audio-trace-output", StringComparison.OrdinalIgnoreCase))
    {
        audioTraceOutputPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--animation-trace-output", StringComparison.OrdinalIgnoreCase))
    {
        animationTraceOutputPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
string? integrationReportPath = null;
for (var argumentIndex = 2; argumentIndex + 1 < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--integration-report", StringComparison.OrdinalIgnoreCase))
    {
        integrationReportPath = Path.GetFullPath(args[argumentIndex + 1]);
        break;
    }
}
string? matchTraceExpectedPath = null;
IReadOnlyList<string>? expectedMatchTraceLines = null;
for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
{
    if (!string.Equals(args[argumentIndex], "--match-trace-expected", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    var expectedValueIndex = argumentIndex + 1;
    if (expectedValueIndex >= args.Length || args[expectedValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--match-trace-expected requer o caminho de um trace TSV.");
        return 2;
    }

    matchTraceExpectedPath = Path.GetFullPath(args[expectedValueIndex]);
    if (string.Equals(matchTraceExpectedPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--match-trace-expected não pode apontar para a ISO de origem.");
        return 2;
    }

    try
    {
        expectedMatchTraceLines = File.ReadAllLines(matchTraceExpectedPath);
        if (expectedMatchTraceLines.Count == 0 ||
            !string.Equals(expectedMatchTraceLines[0], MatchTraceSerializer.Header, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"O trace esperado deve começar com o cabeçalho '{MatchTraceSerializer.Header}'.");
        }
    }
    catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
    {
        Console.Error.WriteLine($"--match-trace-expected inválido: {exception.Message}");
        return 2;
    }

    break;
}
string? inputTraceExpectedPath = null;
IReadOnlyList<string>? expectedInputTraceLines = null;
for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
{
    if (!string.Equals(args[argumentIndex], "--input-trace-expected", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    var expectedValueIndex = argumentIndex + 1;
    if (expectedValueIndex >= args.Length || args[expectedValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--input-trace-expected requer o caminho de um trace TSV.");
        return 2;
    }

    inputTraceExpectedPath = Path.GetFullPath(args[expectedValueIndex]);
    if (string.Equals(inputTraceExpectedPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--input-trace-expected não pode apontar para a ISO de origem.");
        return 2;
    }

    try
    {
        expectedInputTraceLines = File.ReadAllLines(inputTraceExpectedPath);
        if (expectedInputTraceLines.Count == 0 ||
            !string.Equals(expectedInputTraceLines[0], CleanRoomInputTraceSerializer.Header, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"O trace de input esperado deve começar com o cabeçalho '{CleanRoomInputTraceSerializer.Header}'.");
        }
    }
    catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
    {
        Console.Error.WriteLine($"--input-trace-expected inválido: {exception.Message}");
        return 2;
    }

    break;
}
string? animationTraceExpectedPath = null;
IReadOnlyList<string>? expectedAnimationTraceLines = null;
for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
{
    if (!string.Equals(args[argumentIndex], "--animation-trace-expected", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    var expectedValueIndex = argumentIndex + 1;
    if (expectedValueIndex >= args.Length || args[expectedValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--animation-trace-expected requer o caminho de um trace TSV.");
        return 2;
    }

    animationTraceExpectedPath = Path.GetFullPath(args[expectedValueIndex]);
    if (string.Equals(animationTraceExpectedPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--animation-trace-expected não pode apontar para a ISO de origem.");
        return 2;
    }

    try
    {
        expectedAnimationTraceLines = File.ReadAllLines(animationTraceExpectedPath);
        if (expectedAnimationTraceLines.Count == 0 ||
            !string.Equals(expectedAnimationTraceLines[0], CleanRoomAuthoredAnimationTraceSerializer.Header, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"O trace de animação authored esperado deve começar com o cabeçalho '{CleanRoomAuthoredAnimationTraceSerializer.Header}'.");
        }
    }
    catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
    {
        Console.Error.WriteLine($"--animation-trace-expected inválido: {exception.Message}");
        return 2;
    }

    break;
}
string? audioTraceExpectedPath = null;
IReadOnlyList<string>? expectedAudioTraceLines = null;
for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
{
    if (!string.Equals(args[argumentIndex], "--audio-trace-expected", StringComparison.OrdinalIgnoreCase))
    {
        continue;
    }

    var expectedValueIndex = argumentIndex + 1;
    if (expectedValueIndex >= args.Length || args[expectedValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        Console.Error.WriteLine("--audio-trace-expected requer o caminho de um trace TSV.");
        return 2;
    }

    audioTraceExpectedPath = Path.GetFullPath(args[expectedValueIndex]);
    if (string.Equals(audioTraceExpectedPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--audio-trace-expected não pode apontar para a ISO de origem.");
        return 2;
    }

    try
    {
        expectedAudioTraceLines = File.ReadAllLines(audioTraceExpectedPath);
        if (expectedAudioTraceLines.Count == 0 ||
            !string.Equals(expectedAudioTraceLines[0], GameplayAudioTraceSerializer.Header, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"O trace de áudio esperado deve começar com o cabeçalho '{GameplayAudioTraceSerializer.Header}'.");
        }
    }
    catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or InvalidDataException or FormatException)
    {
        Console.Error.WriteLine($"--audio-trace-expected inválido: {exception.Message}");
        return 2;
    }

    break;
}
if (matchTraceExpectedPath is not null &&
    matchTraceOutputPath is not null &&
    string.Equals(matchTraceExpectedPath, matchTraceOutputPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("--match-trace-expected e --match-trace-output devem apontar para arquivos diferentes.");
    return 2;
}
if (inputTraceExpectedPath is not null &&
    inputTraceOutputPath is not null &&
    string.Equals(inputTraceExpectedPath, inputTraceOutputPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("--input-trace-expected e --input-trace-output devem apontar para arquivos diferentes.");
    return 2;
}
if (animationTraceExpectedPath is not null &&
    animationTraceOutputPath is not null &&
    string.Equals(animationTraceExpectedPath, animationTraceOutputPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("--animation-trace-expected e --animation-trace-output devem apontar para arquivos diferentes.");
    return 2;
}
if (audioTraceExpectedPath is not null &&
    audioTraceOutputPath is not null &&
    string.Equals(audioTraceExpectedPath, audioTraceOutputPath, StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("--audio-trace-expected e --audio-trace-output devem apontar para arquivos diferentes.");
    return 2;
}
var candidateMaterialOption = Array.FindIndex(args, argument =>
    string.Equals(argument, "--candidate-material", StringComparison.OrdinalIgnoreCase));
var candidateMaterialRequested = candidateMaterialOption >= 0;
var candidateMaterialArchive = "tops0030.big";
var candidateMaterialShape = "tops";
string? candidateMaterialObject = null;
if (candidateMaterialRequested)
{
    var optionValueIndex = candidateMaterialOption + 1;
    if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
    {
        candidateMaterialArchive = args[optionValueIndex++];
        if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
        {
            candidateMaterialShape = args[optionValueIndex++];
            if (optionValueIndex < args.Length && !args[optionValueIndex].StartsWith("--", StringComparison.Ordinal))
            {
                candidateMaterialObject = args[optionValueIndex];
            }
        }
    }
}
if (playAudio && !OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("A opção --play-audio requer Windows/WinMM.");
    return 2;
}
var frameCount = inputScript?.FrameCount ?? (demoInput ? 80 : 3);
for (var argumentIndex = 2; argumentIndex < args.Length; argumentIndex++)
{
    if (string.Equals(args[argumentIndex], "--match-seconds", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--match-rule", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--target-goals", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--target-pannas", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--frame-output", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--match-trace-output", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--match-trace-expected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--input-trace-output", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--input-trace-expected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--animation-trace-output", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--animation-trace-expected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--audio-trace-output", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--audio-trace-expected", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--integration-report", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(args[argumentIndex], "--input-script", StringComparison.OrdinalIgnoreCase))
    {
        argumentIndex++;
        continue;
    }

    var argument = args[argumentIndex];
    if (int.TryParse(argument, out var parsedFrames))
    {
        frameCount = Math.Max(1, parsedFrames);
        break;
    }
}

using var fileSystem = new XdvdfsFileSystem(args[1]);
var catalog = GameAssetCatalog.Scan(fileSystem);
using var runtimeStore = new GameAssetStore(args[1]);
PcTextureResource? runtimeTitleTexture = null;
try
{
    using var runtimeTitleStream = runtimeStore.OpenRaw("data/titleimage.xbx");
    using var runtimeTitleMemory = new MemoryStream();
    runtimeTitleStream.CopyTo(runtimeTitleMemory);
    runtimeTitleTexture = TextureAssetLoader.Load(runtimeTitleMemory.ToArray());
}
catch (Exception exception) when (
    exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException)
{
    Console.WriteLine($"render_title_texture: unavailable | {exception.Message}");
}

EaglCandidateMesh? runtimeCandidateMesh = null;
PcShapeImageTexture? runtimeCandidateTexture = null;
var candidateMaterialStatus = candidateMaterialRequested ? "unavailable" : "disabled";
if (candidateMaterialRequested)
{
    try
    {
        var materialAsset = candidateMaterialObject is null
            ? EaglShapeMaterialLoader.LoadFirstForShape(
                runtimeStore,
                "data/models.big",
                candidateMaterialArchive,
                candidateMaterialShape)
            : EaglShapeMaterialLoader.LoadForObject(
                runtimeStore,
                "data/models.big",
                candidateMaterialArchive,
                candidateMaterialObject,
                candidateMaterialShape);
        var candidateAsset = EaglCandidateAssetLoader.Load(
            runtimeStore,
            "data/models.big",
            materialAsset.ModelArchiveName,
            materialAsset.ModelObjectName);
            if (!EaglShapeMaterialLoader.HasExactShapeStateInPrimitives(
                    candidateAsset.Primitives,
                    materialAsset.ShapeName))
            {
                throw new InvalidDataException(
                    $"Candidate object has no exact SHAPENAME state for '{materialAsset.ShapeName}'.");
            }

        var renderGroupState = EaglShapeMaterialLoader.HasExactShapeState(
            candidateAsset.Mesh,
            materialAsset.ShapeName);
        runtimeCandidateMesh = candidateAsset.Mesh;
        runtimeCandidateTexture = materialAsset.Texture;
        candidateMaterialStatus = "validated-candidate";
        Console.WriteLine(
            $"render_candidate_material: {materialAsset.ModelArchiveName} | object={materialAsset.ModelObjectName} | " +
            $"shape={materialAsset.ShapeName} | object_state=exact-shapename | " +
            $"render_group_state={(renderGroupState ? "exact-shapename" : "unproven")} | " +
            $"texture={materialAsset.Texture.Width}x{materialAsset.Texture.Height} {materialAsset.Texture.PixelFormat}");
    }
    catch (Exception exception) when (
        exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException or NotSupportedException)
    {
        Console.WriteLine($"render_candidate_material: unavailable | {exception.Message}");
    }
}

if (runtimeCandidateMesh is null)
{
    try
    {
        runtimeCandidateMesh = EaglCandidateAssetLoader.Load(
            runtimeStore,
            "data/models.big",
            "star0001.big",
            "star0001-head.ord").Mesh;
    }
    catch (Exception exception) when (
        exception is InvalidDataException or EndOfStreamException or OverflowException or FileNotFoundException)
    {
        Console.WriteLine($"render_candidate_mesh: unavailable | {exception.Message}");
    }
}

var runtimeRenderer = new SoftwareGameplayRenderer(
    titleTexture: runtimeTitleTexture,
    candidateMesh: runtimeCandidateMesh,
    candidateTexture: runtimeCandidateTexture,
    cleanRoomCharacters: cleanRoomCharacters,
    candidateHomeCharacter: candidateHomeCharacter,
    candidateAwayCharacter: candidateAwayCharacter);
using var runtimeAudio = new CatalogAudioMixer(
    catalog,
    fileSystem.OpenReadOnly,
    enablePlayback: playAudio,
    prepareObservedBankCandidates: true);
var runtime = new GameRuntime(
    catalog,
    runtimeRenderer,
    runtimeAudio,
    inputScript is not null
        ? new MatchInputScriptSource(inputScript)
        : demoInput
        ? tackleDemo
        ? new TackleDemoInputSource()
        : stealDemo
        ? new StealDemoInputSource()
        : new GameplayDemoInputSource(aimedDemo, gamebreakerDemo, juggleDemo)
            : new HeadlessInputSource(),
    opponentAi,
    enableCleanRoomRoundReset: roundDemo,
    matchDurationSeconds: matchDurationSeconds,
    endCondition: matchEndCondition,
    targetGoals: targetGoals,
    targetPannas: targetPannas);

runtime.Start();
for (var frame = 0; frame < frameCount; frame++)
{
    runtime.Tick(1.0 / 60.0);
}

IReadOnlyList<string>? actualMatchTraceLines = null;
if (matchTraceExpectedPath is not null || matchTraceOutputPath is not null)
{
    actualMatchTraceLines = MatchTraceSerializer.BuildLines(runtime.Match.EventHistory);
}
if (matchTraceExpectedPath is not null && expectedMatchTraceLines is not null && actualMatchTraceLines is not null)
{
    var firstDifference = MatchTraceSerializer.FindFirstDifference(
        expectedMatchTraceLines,
        actualMatchTraceLines);
    if (firstDifference >= 0)
    {
        var expectedLine = firstDifference < expectedMatchTraceLines.Count
            ? expectedMatchTraceLines[firstDifference]
            : "<fim do trace esperado>";
        var actualLine = firstDifference < actualMatchTraceLines.Count
            ? actualMatchTraceLines[firstDifference]
            : "<fim do trace atual>";
        Console.Error.WriteLine(
            $"--match-trace-expected não corresponde na linha {firstDifference + 1}.");
        Console.Error.WriteLine($"expected: {expectedLine}");
        Console.Error.WriteLine($"actual:   {actualLine}");
        return 1;
    }

    Console.WriteLine(
        $"match_trace_expected: {matchTraceExpectedPath} | matched=True | events={runtime.Match.EventHistory.Count}");
}

if (frameOutputPath is not null)
{
    if (string.Equals(frameOutputPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--frame-output não pode sobrescrever a ISO de origem.");
        return 2;
    }

    runtimeRenderer.Frame.SavePpm(frameOutputPath);
    Console.WriteLine($"frame_output: {frameOutputPath}");
}

if (matchTraceOutputPath is not null)
{
    if (string.Equals(matchTraceOutputPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--match-trace-output não pode sobrescrever a ISO de origem.");
        return 2;
    }

    WriteMatchTrace(matchTraceOutputPath, runtime.Match.EventHistory);
    Console.WriteLine($"match_trace_output: {matchTraceOutputPath} | events={runtime.Match.EventHistory.Count}");
}

IReadOnlyList<string>? actualInputTraceLines = null;
if (inputTraceExpectedPath is not null || inputTraceOutputPath is not null)
{
    actualInputTraceLines = CleanRoomInputTraceSerializer.BuildLines(runtime.InputHistory);
}
if (inputTraceExpectedPath is not null &&
    expectedInputTraceLines is not null &&
    actualInputTraceLines is not null)
{
    var firstDifference = CleanRoomInputTraceSerializer.FindFirstDifference(
        expectedInputTraceLines,
        actualInputTraceLines);
    if (firstDifference >= 0)
    {
        var expectedLine = firstDifference < expectedInputTraceLines.Count
            ? expectedInputTraceLines[firstDifference]
            : "<fim do trace de input esperado>";
        var actualLine = firstDifference < actualInputTraceLines.Count
            ? actualInputTraceLines[firstDifference]
            : "<fim do trace de input atual>";
        Console.Error.WriteLine(
            $"--input-trace-expected não corresponde na linha {firstDifference + 1}.");
        Console.Error.WriteLine($"expected: {expectedLine}");
        Console.Error.WriteLine($"actual:   {actualLine}");
        return 1;
    }

    Console.WriteLine(
        $"input_trace_expected: {inputTraceExpectedPath} | matched=True | samples={runtime.InputHistory.Count}");
}

if (inputTraceOutputPath is not null)
{
    if (string.Equals(inputTraceOutputPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--input-trace-output não pode sobrescrever a ISO de origem.");
        return 2;
    }

    CleanRoomInputTraceSerializer.Write(inputTraceOutputPath, runtime.InputHistory);
    Console.WriteLine($"input_trace_output: {inputTraceOutputPath} | samples={runtime.InputHistory.Count}");
}

IReadOnlyList<string>? actualAnimationTraceLines = null;
if (animationTraceExpectedPath is not null || animationTraceOutputPath is not null)
{
    actualAnimationTraceLines = CleanRoomAuthoredAnimationTraceSerializer.BuildLines(runtime.AuthoredAnimationHistory);
}
if (animationTraceExpectedPath is not null &&
    expectedAnimationTraceLines is not null &&
    actualAnimationTraceLines is not null)
{
    var firstDifference = CleanRoomAuthoredAnimationTraceSerializer.FindFirstDifference(
        expectedAnimationTraceLines,
        actualAnimationTraceLines);
    if (firstDifference >= 0)
    {
        var expectedLine = firstDifference < expectedAnimationTraceLines.Count
            ? expectedAnimationTraceLines[firstDifference]
            : "<fim do trace de animação authored esperado>";
        var actualLine = firstDifference < actualAnimationTraceLines.Count
            ? actualAnimationTraceLines[firstDifference]
            : "<fim do trace de animação authored atual>";
        Console.Error.WriteLine(
            $"--animation-trace-expected não corresponde na linha {firstDifference + 1}.");
        Console.Error.WriteLine($"expected: {expectedLine}");
        Console.Error.WriteLine($"actual:   {actualLine}");
        return 1;
    }

    Console.WriteLine(
        $"animation_trace_expected: {animationTraceExpectedPath} | matched=True | samples={runtime.AuthoredAnimationHistory.Count}");
}

if (animationTraceOutputPath is not null)
{
    if (string.Equals(animationTraceOutputPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--animation-trace-output não pode sobrescrever a ISO de origem.");
        return 2;
    }

    CleanRoomAuthoredAnimationTraceSerializer.Write(
        animationTraceOutputPath,
        runtime.AuthoredAnimationHistory);
    Console.WriteLine(
        $"animation_trace_output: {animationTraceOutputPath} | samples={runtime.AuthoredAnimationHistory.Count}");
}

IReadOnlyList<string>? actualAudioTraceLines = null;
if (audioTraceExpectedPath is not null || audioTraceOutputPath is not null)
{
    actualAudioTraceLines = GameplayAudioTraceSerializer.BuildLines(runtimeAudio.GameplayAudioRoutes);
}
if (audioTraceExpectedPath is not null &&
    expectedAudioTraceLines is not null &&
    actualAudioTraceLines is not null)
{
    var firstDifference = GameplayAudioTraceSerializer.FindFirstDifference(
        expectedAudioTraceLines,
        actualAudioTraceLines);
    if (firstDifference >= 0)
    {
        var expectedLine = firstDifference < expectedAudioTraceLines.Count
            ? expectedAudioTraceLines[firstDifference]
            : "<fim do trace de áudio esperado>";
        var actualLine = firstDifference < actualAudioTraceLines.Count
            ? actualAudioTraceLines[firstDifference]
            : "<fim do trace de áudio atual>";
        Console.Error.WriteLine(
            $"--audio-trace-expected não corresponde na linha {firstDifference + 1}.");
        Console.Error.WriteLine($"expected: {expectedLine}");
        Console.Error.WriteLine($"actual:   {actualLine}");
        return 1;
    }

    Console.WriteLine(
        $"audio_trace_expected: {audioTraceExpectedPath} | matched=True | events={runtimeAudio.GameplayAudioRoutes.Count}");
}

if (audioTraceOutputPath is not null)
{
    if (string.Equals(audioTraceOutputPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--audio-trace-output não pode sobrescrever a ISO de origem.");
        return 2;
    }

    GameplayAudioTraceSerializer.Write(audioTraceOutputPath, runtimeAudio.GameplayAudioRoutes);
    Console.WriteLine($"audio_trace_output: {audioTraceOutputPath} | events={runtimeAudio.GameplayAudioRoutes.Count}");
}

if (integrationReportPath is not null)
{
    if (string.Equals(integrationReportPath, Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase))
    {
        Console.Error.WriteLine("--integration-report não pode sobrescrever a ISO de origem.");
        return 2;
    }

    var candidateHomeEnabled = candidateHomeCharacter && runtimeCandidateMesh is not null;
    var candidateAwayEnabled = candidateAwayCharacter && runtimeCandidateMesh is not null;
    CleanRoomIntegrationReport.Write(
        integrationReportPath,
        new CleanRoomIntegrationReportData(
            CandidateMaterialValidated: string.Equals(candidateMaterialStatus, "validated-candidate", StringComparison.Ordinal),
            CandidateHomeEnabled: candidateHomeEnabled,
            CandidateAwayEnabled: candidateAwayEnabled,
            RenderedFrames: runtimeRenderer.RenderedFrameCount,
            RenderChecksum: runtimeRenderer.LastFrameChecksum.ToString("X16"),
            DistinctRenderChecksums: runtimeRenderer.DistinctFrameChecksumCount,
            AuthoredCharacterAssetFrames: runtimeRenderer.AuthoredCharacterAssetFrames,
            AuthoredCharacterAssetManifest: CleanRoomCharacterAssetManifest.Default.ManifestId,
            AuthoredSceneAssetFrames: runtimeRenderer.AuthoredSceneAssetFrames,
            AuthoredSceneAssetManifest: CleanRoomSceneAssetManifest.Default.ManifestId,
            AuthoredAnimationAssetManifest: CleanRoomAnimationAssetManifest.Default.ManifestId,
            InputSamples: runtime.InputHistory.Count,
            AnimationSamples: runtime.AuthoredAnimationHistory.Count,
            MatchEvents: runtime.Match.EventHistory.Count,
            AudioEvents: runtimeAudio.GameplayAudioRoutes.Count,
            AudioPrepared: runtimeAudio.PreparedGameplayAudioCount,
            AudioPlayed: runtimeAudio.PlayedGameplayAudioCount,
            AudioUnmapped: runtimeAudio.GameplayAudioUnmappedEventCount,
            NamedEventCandidateWavesPrepared: runtimeAudio.PreparedNamedEventCandidateCount,
            NamedEventCandidatePcmBytesPrepared: runtimeAudio.PreparedNamedEventCandidatePcmBytes,
            NamedEventCandidateDecodeRejected: runtimeAudio.NamedEventCandidateDecodeRejectedCount,
            ObservedBankCandidateCount: runtimeAudio.ObservedBankCandidateResults.Count,
            ObservedBankCandidateDecoded: runtimeAudio.ObservedBankCandidateResults.Count(row => row.DecodeValidated),
            ObservedBankCandidateRejected: runtimeAudio.ObservedBankCandidateResults.Count(row => !row.DecodeValidated),
            GameplayAudioPcmBytes: runtimeAudio.GameplayAudioPcmBytes,
            GameplayAudioPcmSha256: runtimeAudio.GameplayAudioPcmSha256,
            AuthoredAudioAssetManifest: runtimeAudio.AuthoredAudioAssetManifest,
            MatchTraceWritten: matchTraceOutputPath is not null,
            InputTraceWritten: inputTraceOutputPath is not null,
            AnimationTraceWritten: animationTraceOutputPath is not null,
            AudioTraceWritten: audioTraceOutputPath is not null,
            OpponentAiEnabled: runtime.OpponentAiEnabled));
    Console.WriteLine($"integration_report: {integrationReportPath}");
}

Console.WriteLine(runtime.Report());
var inputMode = inputScript is not null
    ? "file-script"
    : roundDemo
        ? (aimedDemo ? "deterministic-aimed-round-demo" : "deterministic-round-demo")
        : juggleDemo
            ? "deterministic-juggle-demo"
        : stealDemo
            ? "deterministic-steal-demo"
        : gamebreakerDemo
            ? "deterministic-gamebreaker-demo"
        : aimedDemo
            ? "deterministic-aimed-demo"
            : demoInput
                ? "deterministic-demo"
                : "idle-headless";
Console.WriteLine($"input_mode: {inputMode}");
if (inputScript is not null)
{
    Console.WriteLine($"input_script: {inputScriptPath} | frames={inputScript.FrameCount}");
}
Console.WriteLine($"audio_output: {(playAudio ? "winmm-async" : "disabled-headless")}");
Console.WriteLine($"gameplay_audio_trace_events: {runtimeAudio.GameplayAudioRoutes.Count}");
Console.WriteLine($"gameplay_audio_route_sequence: {string.Join(",", runtimeAudio.GameplayAudioRoutes.Select(route => $"{route.Action}->{(route.CandidateName.Length == 0 ? "-" : route.CandidateName)}:{route.Route}"))}");
Console.WriteLine($"render_assets: title_texture={(runtimeTitleTexture is null ? "unavailable" : "validated")} | candidate_mesh={(runtimeCandidateMesh is null ? "unavailable" : "candidate")} | candidate_material={candidateMaterialStatus}");
Console.WriteLine($"render_characters: {(cleanRoomCharacters ? "clean-room-procedural" : "legacy-shapes")}");
Console.WriteLine($"candidate_home_character: {(candidateHomeCharacter && runtimeCandidateMesh is not null ? "enabled-rigid-candidate" : "disabled")}");
Console.WriteLine($"candidate_away_character: {(candidateAwayCharacter && runtimeCandidateMesh is not null ? "enabled-rigid-candidate" : "disabled")}");
return 0;

static void WriteMatchTrace(string path, IReadOnlyList<CleanRoomMatchEvent> events)
{
    MatchTraceSerializer.Write(path, events);
}

static uint ReadObjectSectionWord(byte[] objectData, ElfSection section, uint sectionRelativeOffset)
{
    if (section.Type == 8 || (ulong)sectionRelativeOffset + 4 > section.Size)
    {
        throw new InvalidDataException("EAGL pointer or field is outside its data section.");
    }
    var fileOffset = (ulong)section.Offset + sectionRelativeOffset;
    if (fileOffset + 4 > (ulong)objectData.Length)
    {
        throw new InvalidDataException("EAGL field is outside the object payload.");
    }
    return BinaryPrimitives.ReadUInt32LittleEndian(objectData.AsSpan(checked((int)fileOffset), 4));
}

static string GetNumericValueCategoryPayload(string? signature)
{
    if (string.IsNullOrEmpty(signature))
    {
        return string.Empty;
    }

    const string marker = "value_categories=";
    const string terminator = ";sha256=";
    var start = signature.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0)
    {
        return string.Empty;
    }

    start += marker.Length;
    var end = signature.IndexOf(terminator, start, StringComparison.Ordinal);
    return end < 0 ? signature[start..] : signature[start..end];
}

static IReadOnlyList<string> GetNumericValueCategoryTokens(string payload)
{
    return payload.Length == 0
        ? Array.Empty<string>()
        : payload.Split('|', StringSplitOptions.None);
}

static IReadOnlyList<string> GetNormalizedPayloadWindowHashes(string? signature)
{
    if (string.IsNullOrEmpty(signature))
    {
        return Array.Empty<string>();
    }

    const string marker = "hashes=";
    var start = signature.IndexOf(marker, StringComparison.Ordinal);
    if (start < 0)
    {
        return Array.Empty<string>();
    }

    start += marker.Length;
    var end = signature.IndexOf(';', start);
    var payload = end < 0 ? signature[start..] : signature[start..end];
    return payload.Length == 0
        ? Array.Empty<string>()
        : payload.Split(',', StringSplitOptions.RemoveEmptyEntries);
}

static string CatalogField(string? value) =>
    (value ?? string.Empty)
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

static string GetElfSymbolMetadataKey(ElfSymbol symbol) =>
    string.Join(
        "|",
        symbol.TableSectionIndex,
        symbol.Index,
        symbol.SectionIndex,
        symbol.Value,
        symbol.Size,
        symbol.Info);

static string SafeFilePart(string value)
{
    ArgumentException.ThrowIfNullOrWhiteSpace(value);
    var invalid = Path.GetInvalidFileNameChars();
    var sanitized = new string(value
        .Select(character => invalid.Contains(character) ? '_' : character)
        .ToArray());
    return string.IsNullOrWhiteSpace(sanitized) ? "unnamed" : sanitized;
}

static byte[] ReadBounded(Stream stream, long maximumBytes)
{
    if (stream.Length > maximumBytes)
    {
        throw new InvalidDataException($"Asset exceeds bounded read limit: {stream.Length} bytes.");
    }

    using var memory = new MemoryStream(checked((int)stream.Length));
    stream.CopyTo(memory);
    return memory.ToArray();
}
