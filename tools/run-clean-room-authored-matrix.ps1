[CmdletBinding()]
param(
    [string]$Workspace = (Get-Location).Path,
    [string]$IsoPath = ''
)

$Workspace = [IO.Path]::GetFullPath($Workspace)
$ProjectPath = Join-Path $Workspace 'fifa_pc/FifaStreetPc.csproj'
$ReportRoot = Join-Path $Workspace 'fifa_pc'
$RuntimeAssembly = Join-Path $ReportRoot 'bin/Release/net8.0/FifaStreetPc.dll'
. (Join-Path $PSScriptRoot 'clean-room-release-invocation.ps1')
if ([string]::IsNullOrWhiteSpace($IsoPath)) {
    $IsoPath = Join-Path $Workspace 'FIFA Street 2 (USA) (En,Es).iso'
}
$IsoPath = [IO.Path]::GetFullPath($IsoPath)

if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) {
    throw "Projeto não encontrado: $ProjectPath"
}
if (-not (Test-Path -LiteralPath $IsoPath -PathType Leaf)) {
    throw "ISO não encontrada: $IsoPath"
}

$isoBefore = Get-FileHash -Algorithm SHA256 -LiteralPath $IsoPath
$isoItemBefore = Get-Item -LiteralPath $IsoPath

function Invoke-Dotnet {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $executedArguments = $Arguments
    if ($Arguments[0] -ceq 'run') {
        $executedArguments = @(Get-CleanRoomReleaseInvocation -ProjectPath $ProjectPath -RuntimeAssembly $RuntimeAssembly -Arguments $Arguments)
        if (-not (Test-Path -LiteralPath $RuntimeAssembly -PathType Leaf)) {
            throw "Release assembly missing after build: $RuntimeAssembly"
        }
    }
    & dotnet @executedArguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet falhou com exit code ${LASTEXITCODE}: dotnet $($executedArguments -join ' ')"
    }
}

function Invoke-Python {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    & python @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "python falhou com exit code ${LASTEXITCODE}: python $($Arguments -join ' ')"
    }
}

function Invoke-Runtime {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $runtimeArguments = @(
        'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
        'run-headless', $IsoPath
    ) + $Arguments
    Invoke-Dotnet -Arguments $runtimeArguments
}

function Invoke-Scenario {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$GenerateArguments,
        [Parameter(Mandatory = $true)][string[]]$ReplayArguments
    )

    Write-Host "[matrix] $Name generate"
    Invoke-Runtime -Arguments $GenerateArguments
    Write-Host "[matrix] $Name replay"
    Invoke-Runtime -Arguments $ReplayArguments
}

function Invoke-Smoke {
    param(
        [Parameter(Mandatory = $true)][string]$Name,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    Write-Host "[smoke] $Name"
    $smokeTimer = [Diagnostics.Stopwatch]::StartNew()
    $smokeArguments = @(
        'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--'
    ) + $Arguments
    Invoke-Dotnet -Arguments $smokeArguments
    $smokeTimer.Stop()
    Write-Host "smoke_elapsed_ms: $Name | $($smokeTimer.ElapsedMilliseconds)"
}

function Assert-WindowedFrameCapture {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "Captura Win32 não foi criada: $Path"
    }

    $bytes = [IO.File]::ReadAllBytes($Path)
    $expectedHeader = "P6`n960 540`n255`n"
    $headerLength = [Text.Encoding]::ASCII.GetByteCount($expectedHeader)
    if ($bytes.Length -ne $headerLength + (960 * 540 * 3)) {
        throw "Captura Win32 tem tamanho inesperado: $Path ($($bytes.Length) bytes)"
    }

    $actualHeader = [Text.Encoding]::ASCII.GetString($bytes, 0, $headerLength)
    if ($actualHeader -cne $expectedHeader) {
        throw "Captura Win32 tem cabeçalho inesperado: $Path"
    }

    $firstPixelByte = $bytes[$headerLength]
    $varied = $false
    for ($index = $headerLength + 1; $index -lt [Math]::Min($bytes.Length, $headerLength + 200000); $index++) {
        if ($bytes[$index] -ne $firstPixelByte) {
            $varied = $true
            break
        }
    }
    if (-not $varied) {
        throw "Captura Win32 parece uniforme: $Path"
    }

    Write-Host "windowed_frame_capture: $Path | bytes=$($bytes.Length) | header=P6 960x540 | nonuniform=True"
}

function Assert-CandidateIntegrationReport {
    param(
        [Parameter(Mandatory = $true)][string]$Path
    )

    $content = Get-Content -LiteralPath $Path -Raw
    foreach ($requiredMarker in @(
        ('candidate_material_validated' + [char]9 + 'True'),
        ('candidate_home_enabled' + [char]9 + 'True'),
        ('candidate_away_enabled' + [char]9 + 'True'),
        ('original_semantics' + [char]9 + 'not-claimed')
    )) {
        if ($content.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
            throw "Candidate integration report missing '$requiredMarker': $Path"
        }
    }
}

Invoke-Dotnet -Arguments @(
    'build', $ProjectPath, '-c', 'Release', '--no-restore'
)
& (Join-Path $PSScriptRoot 'test-clean-room-release-invocation.ps1')
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'self-test'
)

Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'animation-morph-relocation-profile-audit',
    (Join-Path $ReportRoot 'morph-sibling-structural-catalog.tsv'),
    (Join-Path $ReportRoot 'animation-bank-data-relocation-audit.tsv'),
    (Join-Path $ReportRoot 'animation-morph-relocation-profile-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-morph-relocation-profile-audit-summary.tsv')
)
$animationMorphSummaryPath = Join-Path $ReportRoot 'animation-morph-relocation-profile-audit-summary.tsv'
$animationMorphSummary = Import-Csv -LiteralPath $animationMorphSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $animationMorphSummary -or
    $animationMorphSummary.morph_catalog_rows -cne '594' -or
    $animationMorphSummary.morph_layouts -cne '393' -or
    $animationMorphSummary.parse_valid_layouts -cne '393' -or
    $animationMorphSummary.invalid_layouts -cne '0' -or
    $animationMorphSummary.animation_banks -cne '19' -or
    $animationMorphSummary.containing_pairs -cne '159' -or
    $animationMorphSummary.exact_gap_set_pairs -cne '0' -or
    $animationMorphSummary.layouts_with_exact_gap_histogram_bank -cne '0' -or
    $animationMorphSummary.exact_gap_histogram_pairs -cne '0') {
    throw "Animation/morph relocation profile summary has unexpected content: $animationMorphSummaryPath"
}
$animationMorphHeader = Get-Content -LiteralPath (Join-Path $ReportRoot 'animation-morph-relocation-profile-audit.tsv') -TotalCount 1
foreach ($requiredColumn in @('morph_gap_histogram', 'exact_gap_histogram_bank_count', 'exact_gap_histogram_banks')) {
    if ($animationMorphHeader -notmatch [regex]::Escape($requiredColumn)) {
        throw "Animation/morph relocation profile report is missing '$requiredColumn'."
    }
}

Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/animation_morph_pose_bank_ambiguity_control.py'),
    (Join-Path $ReportRoot 'morph-pose-bank-index.tsv'),
    (Join-Path $ReportRoot 'animation-bank-elf-profiles.tsv'),
    (Join-Path $ReportRoot 'animation-morph-relocation-profile-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'animation-morph-pose-bank-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-morph-pose-bank-ambiguity-control-summary.tsv')
)
$animationPoseBankSummaryPath = Join-Path $ReportRoot 'animation-morph-pose-bank-ambiguity-control-summary.tsv'
$animationPoseBankSummary = Import-Csv -LiteralPath $animationPoseBankSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $animationPoseBankSummary -or
    $animationPoseBankSummary.status -cne 'verified-independent-animation-pose-bank-ambiguity-control' -or
    $animationPoseBankSummary.pose_rows -cne '391' -or
    $animationPoseBankSummary.distinct_pose_archives -cne '391' -or
    $animationPoseBankSummary.distinct_pose_symbols -cne '1' -or
    $animationPoseBankSummary.distinct_normalized_names -cne '1' -or
    $animationPoseBankSummary.animation_bank_rows -cne '19' -or
    $animationPoseBankSummary.animation_bank_valid_rows -cne '19' -or
    $animationPoseBankSummary.animation_bank_symbol_metadata_rows -cne '19' -or
    $animationPoseBankSummary.morph_layout_rows -cne '393' -or
    $animationPoseBankSummary.morph_parse_valid_rows -cne '393' -or
    $animationPoseBankSummary.morph_containing_pairs -cne '159' -or
    $animationPoseBankSummary.morph_exact_gap_set_layouts -cne '0' -or
    $animationPoseBankSummary.morph_exact_gap_set_pairs -cne '0' -or
    $animationPoseBankSummary.morph_exact_gap_histogram_layouts -cne '0' -or
    $animationPoseBankSummary.morph_exact_gap_histogram_pairs -cne '0' -or
    $animationPoseBankSummary.pose_name_match_rows -cne '0' -or
    $animationPoseBankSummary.pose_symbol_metadata_match_rows -cne '0' -or
    $animationPoseBankSummary.unique_pose_animation_binding_proven_rows -cne '0' -or
    $animationPoseBankSummary.valid_pose_rows -cne '391' -or
    $animationPoseBankSummary.semantic_binding -cne 'not-proven' -or
    $animationPoseBankSummary.original_semantics -cne 'not-claimed') {
    throw "Animation pose-bank ambiguity summary has unexpected content: $animationPoseBankSummaryPath"
}
$animationPoseBankHeader = Get-Content -LiteralPath (Join-Path $ReportRoot 'animation-morph-pose-bank-ambiguity-control.tsv') -TotalCount 1
foreach ($requiredColumn in @('matches_animation_bank_name', 'matches_animation_symbol_metadata', 'unique_pose_animation_binding_proven')) {
    if ($animationPoseBankHeader -notmatch [regex]::Escape($requiredColumn)) {
        throw "Animation pose-bank ambiguity report is missing '$requiredColumn'."
    }
}

Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/animation_catalog_pose_timing_global_assignment_control.py'),
    (Join-Path $ReportRoot 'animation-bank-catalog-timestamp-audit.tsv'),
    (Join-Path $ReportRoot 'animation-bank-catalog-payload-anchor-audit.tsv'),
    (Join-Path $ReportRoot 'morph-pose-bank-index.tsv'),
    (Join-Path $ReportRoot 'animation-morph-relocation-profile-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'animation-catalog-pose-timing-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-catalog-pose-timing-global-assignment-control-summary.tsv')
)
$animationCatalogPoseTimingSummaryPath = Join-Path $ReportRoot 'animation-catalog-pose-timing-global-assignment-control-summary.tsv'
$animationCatalogPoseTimingSummary = Import-Csv -LiteralPath $animationCatalogPoseTimingSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $animationCatalogPoseTimingSummary -or
    $animationCatalogPoseTimingSummary.status -cne 'verified-independent-animation-catalog-pose-timing-global-assignment-control' -or
    $animationCatalogPoseTimingSummary.catalog_bank_count -cne '19' -or
    $animationCatalogPoseTimingSummary.catalog_rows -cne '1584' -or
    $animationCatalogPoseTimingSummary.catalog_timestamp_valid_rows -cne '1584' -or
    $animationCatalogPoseTimingSummary.anchor_bank_rows -cne '19' -or
    $animationCatalogPoseTimingSummary.anchor_complete_bank_rows -cne '19' -or
    $animationCatalogPoseTimingSummary.anchor_count -cne '3168' -or
    $animationCatalogPoseTimingSummary.pose_rows -cne '391' -or
    $animationCatalogPoseTimingSummary.distinct_pose_archives -cne '391' -or
    $animationCatalogPoseTimingSummary.distinct_pose_symbols -cne '1' -or
    $animationCatalogPoseTimingSummary.distinct_normalized_names -cne '1' -or
    $animationCatalogPoseTimingSummary.morph_layout_rows -cne '393' -or
    $animationCatalogPoseTimingSummary.morph_parse_valid_rows -cne '393' -or
    $animationCatalogPoseTimingSummary.morph_containing_layouts -cne '53' -or
    $animationCatalogPoseTimingSummary.morph_containing_pairs -cne '159' -or
    $animationCatalogPoseTimingSummary.morph_unique_containment_layouts -cne '8' -or
    $animationCatalogPoseTimingSummary.morph_ambiguous_containment_layouts -cne '45' -or
    $animationCatalogPoseTimingSummary.morph_exact_gap_set_pairs -cne '0' -or
    $animationCatalogPoseTimingSummary.pose_rows_without_catalog_join_key -cne '391' -or
    $animationCatalogPoseTimingSummary.pose_rows_without_morph_join_key -cne '391' -or
    $animationCatalogPoseTimingSummary.pose_bank_domain_size_before -cne '19' -or
    $animationCatalogPoseTimingSummary.pose_bank_domain_size_after -cne '19' -or
    $animationCatalogPoseTimingSummary.global_assignment_models_before -cne '7429' -or
    $animationCatalogPoseTimingSummary.global_assignment_models_after -cne '7429' -or
    $animationCatalogPoseTimingSummary.pose_bank_domain_reduced_rows -cne '0' -or
    $animationCatalogPoseTimingSummary.unique_pose_animation_binding_proven_rows -cne '0' -or
    $animationCatalogPoseTimingSummary.valid_pose_rows -cne '391' -or
    $animationCatalogPoseTimingSummary.semantic_binding -cne 'not-proven' -or
    $animationCatalogPoseTimingSummary.original_semantics -cne 'not-claimed') {
    throw "Animation catalog/pose timing global-assignment summary has unexpected content: $animationCatalogPoseTimingSummaryPath"
}
$animationCatalogPoseTimingHeader = Get-Content -LiteralPath (Join-Path $ReportRoot 'animation-catalog-pose-timing-global-assignment-control.tsv') -TotalCount 1
foreach ($requiredColumn in @('pose_bank_domain_size_before', 'pose_bank_domain_size_after', 'unique_pose_animation_binding_proven')) {
    if ($animationCatalogPoseTimingHeader -notmatch [regex]::Escape($requiredColumn)) {
        throw "Animation catalog/pose timing control report is missing '$requiredColumn'."
    }
}

Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_static_cluster_audit.py'),
    (Join-Path $ReportRoot 'xbe-string-xref-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-string-xref-call-context.tsv'),
    '--report', (Join-Path $ReportRoot 'xbe-render-static-literal-audit.tsv'),
    '--cluster-report', (Join-Path $ReportRoot 'xbe-render-static-call-clusters.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-static-summary.tsv')
)
$renderStaticSummaryPath = Join-Path $ReportRoot 'xbe-render-static-summary.tsv'
$renderStaticSummary = Import-Csv -LiteralPath $renderStaticSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderStaticSummary -or
    $renderStaticSummary.status -cne 'verified-static-render-literal-context' -or
    $renderStaticSummary.render_xref_rows -cne '494' -or
    $renderStaticSummary.unique_render_literals -cne '88' -or
    $renderStaticSummary.render_context_with_executable_calls -cne '3917' -or
    $renderStaticSummary.original_semantics -cne 'not-claimed') {
    throw "Static render summary has unexpected content: $renderStaticSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_target_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-static-call-clusters.tsv'),
    '--max-targets', '64',
    '--max-bytes', '384',
    '--report', (Join-Path $ReportRoot 'xbe-render-target-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-target-summary.tsv')
)
$renderTargetSummaryPath = Join-Path $ReportRoot 'xbe-render-target-summary.tsv'
$renderTargetSummary = Import-Csv -LiteralPath $renderTargetSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderTargetSummary -or
    $renderTargetSummary.status -cne 'verified-static-render-target-windows' -or
    $renderTargetSummary.candidate_targets -cne '64' -or
    $renderTargetSummary.disassembled_targets -cne '64' -or
    $renderTargetSummary.ret_terminated_targets -cne '63' -or
    $renderTargetSummary.original_semantics -cne 'not-claimed') {
    throw "Static render target summary has unexpected content: $renderTargetSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_cfg_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-target-audit.tsv'),
    '--max-bytes', '2048',
    '--report', (Join-Path $ReportRoot 'xbe-render-cfg-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-cfg-summary.tsv')
)
$renderCfgSummaryPath = Join-Path $ReportRoot 'xbe-render-cfg-summary.tsv'
$renderCfgSummary = Import-Csv -LiteralPath $renderCfgSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderCfgSummary -or
    $renderCfgSummary.status -cne 'verified-static-render-cfg-slices' -or
    $renderCfgSummary.candidate_targets -cne '64' -or
    $renderCfgSummary.analyzed_targets -cne '64' -or
    $renderCfgSummary.targets_with_reachable_cfg -cne '64' -or
    $renderCfgSummary.original_semantics -cne 'not-claimed') {
    throw "Static render CFG summary has unexpected content: $renderCfgSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_call_target_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-cfg-audit.tsv'),
    '--source-max-bytes', '2048',
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-call-target-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-call-target-summary.tsv')
)
$renderCallTargetSummaryPath = Join-Path $ReportRoot 'xbe-render-call-target-summary.tsv'
$renderCallTargetSummary = Import-Csv -LiteralPath $renderCallTargetSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderCallTargetSummary -or
    $renderCallTargetSummary.status -cne 'verified-static-render-call-target-slices' -or
    $renderCallTargetSummary.source_render_target_windows -cne '64' -or
    $renderCallTargetSummary.unique_direct_call_targets -cne '87' -or
    $renderCallTargetSummary.disassembled_targets -cne '87' -or
    $renderCallTargetSummary.original_semantics -cne 'not-claimed') {
    throw "Static render call-target summary has unexpected content: $renderCallTargetSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_call_graph_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-call-target-audit.tsv'),
    '--max-depth', '2',
    '--max-bytes', '256',
    '--max-nodes', '512',
    '--node-report', (Join-Path $ReportRoot 'xbe-render-call-graph-nodes.tsv'),
    '--edge-report', (Join-Path $ReportRoot 'xbe-render-call-graph-edges.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-call-graph-summary.tsv')
)
$renderCallGraphSummaryPath = Join-Path $ReportRoot 'xbe-render-call-graph-summary.tsv'
$renderCallGraphSummary = Import-Csv -LiteralPath $renderCallGraphSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderCallGraphSummary -or
    $renderCallGraphSummary.status -cne 'verified-static-bounded-render-call-graph' -or
    $renderCallGraphSummary.seed_targets -cne '87' -or
    $renderCallGraphSummary.discovered_nodes -cne '235' -or
    $renderCallGraphSummary.mapped_executable_nodes -cne '235' -or
    $renderCallGraphSummary.unique_edges -cne '326' -or
    $renderCallGraphSummary.truncated_by_max_nodes -cne 'False' -or
    $renderCallGraphSummary.original_semantics -cne 'not-claimed') {
    throw "Static render call-graph summary has unexpected content: $renderCallGraphSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_transfer_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-cfg-audit.tsv'),
    '--max-bytes', '2048',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-transfer-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-transfer-summary.tsv')
)
$renderIndirectSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-transfer-summary.tsv'
$renderIndirectSummary = Import-Csv -LiteralPath $renderIndirectSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectSummary -or
    $renderIndirectSummary.status -cne 'verified-static-render-indirect-transfer-inventory' -or
    $renderIndirectSummary.candidate_targets -cne '64' -or
    $renderIndirectSummary.analyzed_targets -cne '64' -or
    $renderIndirectSummary.total_indirect_transfers -cne '22' -or
    $renderIndirectSummary.indirect_calls -cne '21' -or
    $renderIndirectSummary.indirect_jumps -cne '1' -or
    $renderIndirectSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-transfer summary has unexpected content: $renderIndirectSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_reference_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-transfer-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-reference-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-reference-summary.tsv')
)
$renderIndirectReferenceSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-reference-summary.tsv'
$renderIndirectReferenceSummary = Import-Csv -LiteralPath $renderIndirectReferenceSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectReferenceSummary -or
    $renderIndirectReferenceSummary.status -cne 'verified-static-render-indirect-reference-words' -or
    $renderIndirectReferenceSummary.indirect_rows -cne '22' -or
    $renderIndirectReferenceSummary.reference_rows -cne '8' -or
    $renderIndirectReferenceSummary.reference_words_valid -cne '8' -or
    $renderIndirectReferenceSummary.reference_words_in_executable_sections -cne '8' -or
    $renderIndirectReferenceSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-reference summary has unexpected content: $renderIndirectReferenceSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_table_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-transfer-audit.tsv'),
    '--max-entries', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-table-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-table-summary.tsv')
)
$renderIndirectIndexedTableSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-table-summary.tsv'
$renderIndirectIndexedTableSummary = Import-Csv -LiteralPath $renderIndirectIndexedTableSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedTableSummary -or
    $renderIndirectIndexedTableSummary.status -cne 'verified-bounded-static-render-indirect-indexed-table' -or
    $renderIndirectIndexedTableSummary.indexed_rows -cne '1' -or
    $renderIndirectIndexedTableSummary.distinct_table_bases -cne '1' -or
    $renderIndirectIndexedTableSummary.tables_scan_capped -cne '0' -or
    $renderIndirectIndexedTableSummary.tables_terminated_after_non_executable_words -cne '1' -or
    $renderIndirectIndexedTableSummary.readable_table_words -cne '89' -or
    $renderIndirectIndexedTableSummary.executable_table_words -cne '87' -or
    $renderIndirectIndexedTableSummary.non_executable_table_words -cne '2' -or
    $renderIndirectIndexedTableSummary.unreadable_table_words -cne '0' -or
    $renderIndirectIndexedTableSummary.distinct_executable_words -cne '81' -or
    $renderIndirectIndexedTableSummary.max_contiguous_executable_prefix -cne '87' -or
    $renderIndirectIndexedTableSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed-table summary has unexpected content: $renderIndirectIndexedTableSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_target_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-table-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-summary.tsv')
)
$renderIndirectIndexedTargetSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-summary.tsv'
$renderIndirectIndexedTargetSummary = Import-Csv -LiteralPath $renderIndirectIndexedTargetSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedTargetSummary -or
    $renderIndirectIndexedTargetSummary.status -cne 'verified-bounded-static-render-indirect-indexed-target-slices' -or
    $renderIndirectIndexedTargetSummary.indexed_table_rows -cne '1' -or
    $renderIndirectIndexedTargetSummary.candidate_entry_occurrences -cne '87' -or
    $renderIndirectIndexedTargetSummary.distinct_candidate_targets -cne '81' -or
    $renderIndirectIndexedTargetSummary.mapped_executable_targets -cne '81' -or
    $renderIndirectIndexedTargetSummary.disassembled_candidate_targets -cne '81' -or
    $renderIndirectIndexedTargetSummary.ret_terminated_candidate_targets -cne '81' -or
    $renderIndirectIndexedTargetSummary.prologue_like_candidate_targets -cne '0' -or
    $renderIndirectIndexedTargetSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed target summary has unexpected content: $renderIndirectIndexedTargetSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_accessor_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-table-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-accessor-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-accessor-summary.tsv')
)
$renderIndirectIndexedAccessorSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-accessor-summary.tsv'
$renderIndirectIndexedAccessorSummary = Import-Csv -LiteralPath $renderIndirectIndexedAccessorSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedAccessorSummary -or
    $renderIndirectIndexedAccessorSummary.status -cne 'verified-bounded-static-render-indirect-indexed-accessor-shapes' -or
    $renderIndirectIndexedAccessorSummary.indexed_table_rows -cne '1' -or
    $renderIndirectIndexedAccessorSummary.candidate_entry_occurrences -ne '87' -or
    $renderIndirectIndexedAccessorSummary.distinct_candidate_targets -ne '81' -or
    $renderIndirectIndexedAccessorSummary.mapped_executable_targets -ne '81' -or
    $renderIndirectIndexedAccessorSummary.disassembled_candidate_targets -ne '81' -or
    $renderIndirectIndexedAccessorSummary.ret_terminated_candidate_targets -ne '81' -or
    $renderIndirectIndexedAccessorSummary.fallback_pop_pop_ret_shapes -ne '1' -or
    $renderIndirectIndexedAccessorSummary.single_mov_memory_ret_shapes -ne '55' -or
    $renderIndirectIndexedAccessorSummary.lea_push_call_ret_shapes -ne '24' -or
    $renderIndirectIndexedAccessorSummary.other_shapes -ne '1' -or
    $renderIndirectIndexedAccessorSummary.distinct_direct_call_targets -ne '1' -or
    $renderIndirectIndexedAccessorSummary.original_semantics -ne 'not-claimed') {
    throw "Static render indirect-indexed accessor-shape summary has unexpected content: $renderIndirectIndexedAccessorSummaryPath"
}
Invoke-Smoke 'original-render-chain' @(
    'eagl-original-render-chain-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-render-chain-summary.tsv')
)
$originalRenderChainSummaryPath = Join-Path $ReportRoot 'eagl-original-render-chain-summary.tsv'
$originalRenderChainSummary = Import-Csv -LiteralPath $originalRenderChainSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalRenderChainSummary -or
    $originalRenderChainSummary.status -cne 'verified-bounded-original-render-chain-structure' -or
    $originalRenderChainSummary.model_package_candidates -le '0' -or
    $originalRenderChainSummary.model_packages_loaded -cne $originalRenderChainSummary.model_package_candidates -or
    $originalRenderChainSummary.object_candidates -le '0' -or
    $originalRenderChainSummary.objects_loaded -le '0' -or
    $originalRenderChainSummary.model_symbol_rows -le '0' -or
    $originalRenderChainSummary.vertex_buffer_symbol_rows -le '0' -or
    $originalRenderChainSummary.validated_vertex_stream_rows -le '0' -or
    $originalRenderChainSummary.objects_with_bounds -le '0' -or
    $originalRenderChainSummary.objects_with_validated_vertex_streams -le '0' -or
    $originalRenderChainSummary.primitive_rows -le '0' -or
    $originalRenderChainSummary.valid_chain_rows -cne $originalRenderChainSummary.primitive_rows -or
    $originalRenderChainSummary.complete_command_streams -cne $originalRenderChainSummary.primitive_rows -or
    $originalRenderChainSummary.fetch_slot_rows -le '0' -or
    $originalRenderChainSummary.complete_five_slot_fetch_rows -le '0' -or
    $originalRenderChainSummary.state_shapename_primitive_type_overlap_rows -ne '0' -or
    $originalRenderChainSummary.rejected_packages -cne '0' -or
    $originalRenderChainSummary.rejected_objects -cne '0' -or
    $originalRenderChainSummary.original_semantics -cne 'not-claimed' -or
    $originalRenderChainSummary.state_fetch_control_status -cne 'verified-bounded-original-state-population-negative-control' -or
    $originalRenderChainSummary.vertex_record_control_status -cne 'verified-bounded-original-vertex-record-structure') {
    throw "Original EAGL render-chain summary has unexpected content: $originalRenderChainSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_render_command_dispatch_control.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-render-command-dispatch-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-render-command-dispatch-control-summary.tsv')
)
$originalRenderCommandDispatchSummaryPath = Join-Path $ReportRoot 'eagl-original-render-command-dispatch-control-summary.tsv'
$originalRenderCommandDispatchSummary = Import-Csv -LiteralPath $originalRenderCommandDispatchSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalRenderCommandDispatchSummary -or
    $originalRenderCommandDispatchSummary.status -cne 'verified-static-eagl-command-id-dispatch-control' -or
    $originalRenderCommandDispatchSummary.xbe_sha256 -cne '5FABB5B763BE0CC8823E814EE343AC5BE6950305330F46C5170E08EAADEA910B' -or
    $originalRenderCommandDispatchSummary.eagl_rows -cne '2364' -or
    $originalRenderCommandDispatchSummary.complete_command_stream_rows -cne '2364' -or
    $originalRenderCommandDispatchSummary.command_occurrences -cne '36779' -or
    $originalRenderCommandDispatchSummary.unique_observed_command_ids -cne '12' -or
    $originalRenderCommandDispatchSummary.mapped_observed_command_ids -cne '12' -or
    $originalRenderCommandDispatchSummary.executable_dispatch_table_prefix_entries -cne '42' -or
    $originalRenderCommandDispatchSummary.distinct_dispatch_targets -cne '39' -or
    $originalRenderCommandDispatchSummary.dispatch_table_base -cne '0x0031A930' -or
    $originalRenderCommandDispatchSummary.dispatch_table_file_offset -cne '0x0030A350' -or
    $originalRenderCommandDispatchSummary.first_non_executable_index -cne '42' -or
    $originalRenderCommandDispatchSummary.first_non_executable_word -cne '0x00000001' -or
    $originalRenderCommandDispatchSummary.handler_table_scan_capped -cne 'False' -or
    $originalRenderCommandDispatchSummary.dispatcher_instruction_addresses_validated -cne '7' -or
    $originalRenderCommandDispatchSummary.dispatch_binding -cne 'verified-static-command-header-index-to-handler-table-slot' -or
    $originalRenderCommandDispatchSummary.semantic_binding -cne 'not-proven' -or
    $originalRenderCommandDispatchSummary.original_semantics -cne 'not-claimed') {
    throw "Original EAGL command-dispatch summary has unexpected content: $originalRenderCommandDispatchSummaryPath"
}
Invoke-Smoke 'original-render-parameters' @(
    'eagl-render-parameter-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-render-parameter-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-render-parameter-audit-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_render_parameter_static_control.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'eagl-render-parameter-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-render-parameter-static-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-render-parameter-static-control-summary.tsv')
)
Invoke-Smoke 'original-indexed-draw-source' @(
    'eagl-indexed-draw-source-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-indexed-draw-source-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-indexed-draw-source-audit-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_indexed_draw_source_control.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'eagl-indexed-draw-source-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-render-parameter-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-indexed-draw-source-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-indexed-draw-source-control-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_material_parent_probe.py'),
    $IsoPath,
    '--report', (Join-Path $ReportRoot 'eagl-material-parent-probe.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-material-parent-probe-summary.tsv')
)
Invoke-Smoke 'material-declaration-and-vs-envelope' @(
    'eagl-material-declaration-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-material-declaration-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-material-declaration-audit-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_material_declaration_control.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'eagl-material-declaration-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-material-declaration-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-material-declaration-control-summary.tsv')
)
Invoke-Smoke 'vertex-program-opcode-operands' @(
    'eagl-vertex-program-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-vertex-program-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-vertex-program-audit-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_vertex_program_control.py'),
    $IsoPath, (Join-Path $ReportRoot 'eagl-vertex-program-audit.tsv'),
    '--managed-summary', (Join-Path $ReportRoot 'eagl-vertex-program-audit-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-vertex-program-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-vertex-program-control-summary.tsv')
)
Invoke-Smoke 'model-material-program-references' @(
    'eagl-model-material-reference-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-model-material-reference-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-model-material-reference-audit-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_model_material_reference_control.py'),
    $IsoPath, (Join-Path $ReportRoot 'eagl-model-material-reference-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-model-material-reference-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-model-material-reference-control-summary.tsv')
)
Invoke-Smoke 'original-state-geometry-adjacency' @(
    'eagl-state-geometry-adjacency-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv')
)
Invoke-Smoke 'original-state-geometry-profile' @(
    'eagl-state-geometry-profile-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-state-geometry-profile-audit.tsv')
)
Invoke-Smoke 'original-state-geometry-order' @(
    'eagl-state-geometry-order-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-state-geometry-order-audit.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_partition_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-profile-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-order-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control-summary.tsv')
)
$originalStateGeometryPartitionControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control-summary.tsv'
$originalStateGeometryPartitionControlSummary = Import-Csv -LiteralPath $originalStateGeometryPartitionControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryPartitionControlSummary -or
    $originalStateGeometryPartitionControlSummary.status -cne 'verified-independent-original-state-geometry-partition-control' -or
    $originalStateGeometryPartitionControlSummary.adjacency_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.profile_rows -ne '269' -or
    $originalStateGeometryPartitionControlSummary.order_rows -ne '62' -or
    $originalStateGeometryPartitionControlSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.adjacency_input_status_valid_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.profile_input_status_valid_rows -ne '269' -or
    $originalStateGeometryPartitionControlSummary.order_input_status_valid_rows -ne '62' -or
    $originalStateGeometryPartitionControlSummary.object_state_exact_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.geometry_group_state_exact_rows -ne '0' -or
    $originalStateGeometryPartitionControlSummary.profile_geometry_pair_rows -ne '38' -or
    $originalStateGeometryPartitionControlSummary.profile_state_pair_rows -ne '180' -or
    $originalStateGeometryPartitionControlSummary.profile_rows_reconciled -ne '24' -or
    $originalStateGeometryPartitionControlSummary.order_partitioned_states -ne '180' -or
    $originalStateGeometryPartitionControlSummary.adjacency_states -ne '180' -or
    $originalStateGeometryPartitionControlSummary.order_states -ne '180' -or
    $originalStateGeometryPartitionControlSummary.state_index_set_match_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.interval_index_sequence_match_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.interval_boundary_shape_match_rows -ne '24' -or
    $originalStateGeometryPartitionControlSummary.complete_profile_stream_rows -ne '269' -or
    $originalStateGeometryPartitionControlSummary.exact_command_shape_rows -ne '0' -or
    $originalStateGeometryPartitionControlSummary.exact_command_shape_expected_zero -cne 'True' -or
    $originalStateGeometryPartitionControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry partition control has unexpected content: $originalStateGeometryPartitionControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_binding_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-profile-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-order-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-binding-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-binding-ambiguity-control-summary.tsv')
)
$originalStateGeometryBindingAmbiguitySummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-binding-ambiguity-control-summary.tsv'
$originalStateGeometryBindingAmbiguitySummary = Import-Csv -LiteralPath $originalStateGeometryBindingAmbiguitySummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryBindingAmbiguitySummary -or
    $originalStateGeometryBindingAmbiguitySummary.status -cne 'verified-independent-original-state-geometry-binding-ambiguity-control' -or
    $originalStateGeometryBindingAmbiguitySummary.adjacency_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.profile_rows -ne '269' -or
    $originalStateGeometryBindingAmbiguitySummary.order_rows -ne '62' -or
    $originalStateGeometryBindingAmbiguitySummary.partition_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.joined_object_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.missing_profile_objects -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.missing_order_objects -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.missing_partition_objects -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.profile_cardinality_reconciled_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.order_cardinality_reconciled_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.order_state_index_set_reconciled_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.state_count_greater_than_geometry_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.geometry_group_state_exact_rows -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.exact_command_shape_rows -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.unique_geometry_state_binding_proven_rows -ne '0' -or
    $originalStateGeometryBindingAmbiguitySummary.valid_object_rows -ne '24' -or
    $originalStateGeometryBindingAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryBindingAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry binding ambiguity control has unexpected content: $originalStateGeometryBindingAmbiguitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_global_assignment_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-order-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control-summary.tsv')
)
$originalStateGeometryGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control-summary.tsv'
$originalStateGeometryGlobalAssignmentSummary = Import-Csv -LiteralPath $originalStateGeometryGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryGlobalAssignmentSummary -or
    $originalStateGeometryGlobalAssignmentSummary.status -cne 'verified-independent-original-state-geometry-global-assignment-control' -or
    $originalStateGeometryGlobalAssignmentSummary.order_rows -ne '62' -or
    $originalStateGeometryGlobalAssignmentSummary.partition_rows -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.missing_partition_objects -ne '0' -or
    $originalStateGeometryGlobalAssignmentSummary.order_input_status_valid_rows -ne '62' -or
    $originalStateGeometryGlobalAssignmentSummary.partition_input_status_valid_rows -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.nonempty_interval_rows -ne '36' -or
    $originalStateGeometryGlobalAssignmentSummary.ambiguous_interval_rows -ne '12' -or
    $originalStateGeometryGlobalAssignmentSummary.objects_with_multiple_assignment_models -ne '12' -or
    $originalStateGeometryGlobalAssignmentSummary.objects_with_unique_assignment_model -ne '12' -or
    $originalStateGeometryGlobalAssignmentSummary.objects_with_one_to_one_unique_model -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.state_index_set_reconciled_rows -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.unique_state_geometry_assignment_proven_rows -ne '0' -or
    $originalStateGeometryGlobalAssignmentSummary.valid_object_rows -ne '24' -or
    $originalStateGeometryGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry global assignment control has unexpected content: $originalStateGeometryGlobalAssignmentSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_uv_material_global_assignment_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-slot1-candidate-uv-ambiguity-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-candidate-material-ambiguity-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-uv-material-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-uv-material-global-assignment-control-summary.tsv')
)
$originalStateGeometryUvMaterialGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-uv-material-global-assignment-control-summary.tsv'
$originalStateGeometryUvMaterialGlobalAssignmentSummary = Import-Csv -LiteralPath $originalStateGeometryUvMaterialGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryUvMaterialGlobalAssignmentSummary -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.status -cne 'verified-independent-original-state-geometry-uv-material-global-assignment-control' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.global_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.material_rows -ne '62' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.missing_uv_objects -ne '0' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.missing_material_objects -ne '0' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.global_input_status_valid_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_candidate_attribute_valid_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_candidate_raster_influence_valid_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_complete_group_rows -ne '216' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_incomplete_group_rows -ne '2' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_slot1_near_unit_group_rows -ne '216' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.uv_other_slot_near_unit_group_rows -ne '0' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.material_input_status_valid_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.material_candidate_asset_valid_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.material_state_candidate_source_complete_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_multiple_geometry_assignment_models -ne '12' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_single_geometry_assignment_model -ne '12' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_single_candidate_uv_role_domain -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_single_candidate_material_domain -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.structural_cross_product_model_sum -ne '36' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_multiple_structural_cross_product_models -ne '12' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.objects_with_single_structural_cross_product_model -ne '12' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.unique_state_geometry_uv_material_binding_proven_rows -ne '0' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.valid_object_rows -ne '24' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryUvMaterialGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry/UV/material global assignment control has unexpected content: $originalStateGeometryUvMaterialGlobalAssignmentSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_candidate_render_global_assignment_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control.tsv'),
    (Join-Path $ReportRoot 'eagl-material-runtime-batch.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-payload-join-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-candidate-render-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-candidate-render-global-assignment-control-summary.tsv')
)
$originalStateGeometryCandidateRenderGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-candidate-render-global-assignment-control-summary.tsv'
$originalStateGeometryCandidateRenderGlobalAssignmentSummary = Import-Csv -LiteralPath $originalStateGeometryCandidateRenderGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryCandidateRenderGlobalAssignmentSummary -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.status -cne 'verified-independent-original-state-geometry-candidate-render-global-assignment-control' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.global_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.runtime_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.candidate_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.payload_join_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.missing_runtime_objects -ne '0' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.missing_candidate_objects -ne '0' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.missing_payload_join_objects -ne '0' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.global_input_status_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.runtime_input_status_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.candidate_input_status_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.payload_join_input_status_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.geometry_candidate_count_sum -ne '38' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.candidate_geometry_group_sum -ne '38' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.geometry_candidate_cardinality_match_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.object_state_exact_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.render_group_state_match_rows -ne '0' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.candidate_source_chain_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.payload_object_join_valid_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.objects_with_multiple_geometry_assignment_models -ne '12' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.objects_with_single_geometry_assignment_models -ne '12' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.unconstrained_assignment_model_sum -ne '36' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.one_to_one_assignment_model_sum -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.unique_state_geometry_candidate_render_binding_proven_rows -ne '0' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.valid_object_rows -ne '24' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryCandidateRenderGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry candidate-render global assignment control has unexpected content: $originalStateGeometryCandidateRenderGlobalAssignmentSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_payload_identity_global_assignment_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-candidate-geometry-payload-identity-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-payload-identity-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-payload-identity-global-assignment-control-summary.tsv')
)
$originalStateGeometryPayloadIdentityGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-payload-identity-global-assignment-control-summary.tsv'
$originalStateGeometryPayloadIdentityGlobalAssignmentSummary = Import-Csv -LiteralPath $originalStateGeometryPayloadIdentityGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryPayloadIdentityGlobalAssignmentSummary -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.status -cne 'verified-independent-original-state-geometry-payload-identity-global-assignment-control' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.global_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_rows -ne '38' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.missing_payload_identity_objects -ne '0' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.global_input_status_valid_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_input_status_valid_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.geometry_candidate_count_sum -ne '38' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_geometry_row_sum -ne '38' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_complete_geometry_sum -ne '38' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_raw_hash_row_sum -ne '190' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_cardinality_match_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.payload_identity_reduced_state_domain_rows -ne '0' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.objects_with_multiple_assignment_models -ne '12' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.objects_with_single_assignment_models -ne '12' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.assignment_model_sum_before_payload_identity -ne '36' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.assignment_model_sum_after_payload_identity -ne '36' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.one_to_one_model_sum_before_payload_identity -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.one_to_one_model_sum_after_payload_identity -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.unique_state_geometry_payload_binding_proven_rows -ne '0' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.valid_object_rows -ne '24' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryPayloadIdentityGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry payload identity global assignment control has unexpected content: $originalStateGeometryPayloadIdentityGlobalAssignmentSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_draw_classification_global_assignment_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-global-assignment-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-geometry-draw-classification-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-candidate-geometry-payload-identity-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-draw-classification-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-draw-classification-global-assignment-control-summary.tsv')
)
$originalStateGeometryDrawClassificationGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-draw-classification-global-assignment-control-summary.tsv'
$originalStateGeometryDrawClassificationGlobalAssignmentSummary = Import-Csv -LiteralPath $originalStateGeometryDrawClassificationGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryDrawClassificationGlobalAssignmentSummary -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.status -cne 'verified-independent-original-state-geometry-draw-classification-global-assignment-control' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.global_rows -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.draw_classification_rows -ne '218' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.payload_identity_rows -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.missing_draw_classification_objects -ne '0' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.missing_payload_identity_objects -ne '0' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.global_input_status_valid_rows -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.draw_classification_input_status_valid_rows -ne '218' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.payload_identity_input_status_valid_rows -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.geometry_candidate_count_sum -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.draw_classification_geometry_marker_sum -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.payload_identity_geometry_row_sum -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.classification_match_rows -ne '218' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.geometry_draw_payload_identity_join_rows -ne '38' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.state_marker_rows -ne '180' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.state_rows_without_payload_identity -ne '180' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.objects_with_state_over_geometry_rows -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.assignment_model_sum_before_draw_classification -ne '36' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.assignment_model_sum_after_draw_classification -ne '36' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.one_to_one_model_sum_before_draw_classification -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.one_to_one_model_sum_after_draw_classification -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.draw_classification_reduced_state_domain_rows -ne '0' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.unique_state_geometry_draw_binding_proven_rows -ne '0' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.valid_object_rows -ne '24' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $originalStateGeometryDrawClassificationGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry draw classification global assignment control has unexpected content: $originalStateGeometryDrawClassificationGlobalAssignmentSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_profile_discrimination_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-profile-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-profile-discrimination-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-profile-discrimination-control-summary.tsv')
)
$originalStateProfileDiscriminationSummaryPath = Join-Path $ReportRoot 'eagl-original-state-profile-discrimination-control-summary.tsv'
$originalStateProfileDiscriminationSummary = Import-Csv -LiteralPath $originalStateProfileDiscriminationSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateProfileDiscriminationSummary -or
    $originalStateProfileDiscriminationSummary.status -cne 'verified-independent-original-state-profile-discrimination-control' -or
    $originalStateProfileDiscriminationSummary.profile_rows -ne '269' -or
    $originalStateProfileDiscriminationSummary.profile_objects -ne '24' -or
    $originalStateProfileDiscriminationSummary.geometry_candidate_rows -ne '38' -or
    $originalStateProfileDiscriminationSummary.state_primitive_rows -ne '180' -or
    $originalStateProfileDiscriminationSummary.distinct_object_state_profile_signatures -ne '27' -or
    $originalStateProfileDiscriminationSummary.profile_tie_count_match_rows -ne '269' -or
    $originalStateProfileDiscriminationSummary.state_primitives_with_multiple_geometry_candidates -ne '89' -or
    $originalStateProfileDiscriminationSummary.state_primitives_with_single_geometry_candidate -ne '91' -or
    $originalStateProfileDiscriminationSummary.state_primitives_with_zero_geometry_candidates -ne '0' -or
    $originalStateProfileDiscriminationSummary.state_signature_groups_with_multiple_geometry_candidates -ne '15' -or
    $originalStateProfileDiscriminationSummary.exact_command_shape_rows -ne '0' -or
    $originalStateProfileDiscriminationSummary.unique_state_to_geometry_binding_proven_rows -ne '0' -or
    $originalStateProfileDiscriminationSummary.valid_object_rows -ne '24' -or
    $originalStateProfileDiscriminationSummary.semantic_binding -cne 'not-proven' -or
    $originalStateProfileDiscriminationSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state-profile discrimination control has unexpected content: $originalStateProfileDiscriminationSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_render_state_geometry_bounds_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-geometry-partition-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-bounds-fetch-slot-join-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-render-state-geometry-bounds-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-render-state-geometry-bounds-control-summary.tsv')
)
$originalRenderStateGeometryBoundsControlSummaryPath = Join-Path $ReportRoot 'eagl-original-render-state-geometry-bounds-control-summary.tsv'
$originalRenderStateGeometryBoundsControlSummary = Import-Csv -LiteralPath $originalRenderStateGeometryBoundsControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalRenderStateGeometryBoundsControlSummary -or
    $originalRenderStateGeometryBoundsControlSummary.status -cne 'verified-independent-original-render-state-geometry-bounds-structural-control' -or
    $originalRenderStateGeometryBoundsControlSummary.partition_object_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.bounds_object_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.joined_object_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.partition_input_status_valid_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.bounds_input_status_valid_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.object_cardinality_match_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.bounds_group_rows -ne '218' -or
    $originalRenderStateGeometryBoundsControlSummary.complete_fetch_group_rows -ne '216' -or
    $originalRenderStateGeometryBoundsControlSummary.expected_incomplete_fetch_group_rows -ne '2' -or
    $originalRenderStateGeometryBoundsControlSummary.bounds_slot0_match_rows -ne '218' -or
    $originalRenderStateGeometryBoundsControlSummary.bounds_nonzero_match_rows -ne '0' -or
    $originalRenderStateGeometryBoundsControlSummary.exact_command_shape_rows -ne '0' -or
    $originalRenderStateGeometryBoundsControlSummary.complete_control_rows -ne '24' -or
    $originalRenderStateGeometryBoundsControlSummary.semantic_binding -cne 'not-proven' -or
    $originalRenderStateGeometryBoundsControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original render-state/geometry/bounds control has unexpected content: $originalRenderStateGeometryBoundsControlSummaryPath"
}
Invoke-Smoke 'original-vertex-record-association' @(
    'eagl-original-vertex-record-association-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-vertex-record-association.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-vertex-record-association-summary.tsv')
)
$originalVertexRecordAssociationSummaryPath = Join-Path $ReportRoot 'eagl-original-vertex-record-association-summary.tsv'
$originalVertexRecordAssociationSummary = Import-Csv -LiteralPath $originalVertexRecordAssociationSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalVertexRecordAssociationSummary -or
    $originalVertexRecordAssociationSummary.status -cne 'verified-bounded-original-opaque-vertex-record-association' -or
    $originalVertexRecordAssociationSummary.model_package_candidates -le '0' -or
    $originalVertexRecordAssociationSummary.model_packages_loaded -ne $originalVertexRecordAssociationSummary.model_package_candidates -or
    $originalVertexRecordAssociationSummary.object_candidates -le '0' -or
    $originalVertexRecordAssociationSummary.objects_loaded -le '0' -or
    $originalVertexRecordAssociationSummary.validated_vertex_stream_rows -le '0' -or
    $originalVertexRecordAssociationSummary.objects_with_validated_vertex_streams -le '0' -or
    $originalVertexRecordAssociationSummary.primitive_rows -le '0' -or
    $originalVertexRecordAssociationSummary.rows_with_opaque_ordinal_records -le '0' -or
    $originalVertexRecordAssociationSummary.rows_with_unique_opaque_sequences -le '0' -or
    $originalVertexRecordAssociationSummary.complete_five_slot_fetch_rows -le '0' -or
    $originalVertexRecordAssociationSummary.rows_with_unique_opaque_sequences -gt $originalVertexRecordAssociationSummary.complete_five_slot_fetch_rows -or
    $originalVertexRecordAssociationSummary.rejected_packages -ne '0' -or
    $originalVertexRecordAssociationSummary.rejected_objects -ne '0' -or
    $originalVertexRecordAssociationSummary.original_semantics -cne 'not-claimed') {
    throw "Original opaque __VB record-association summary has unexpected content: $originalVertexRecordAssociationSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_vertex_record_association_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-record-association.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-vertex-record-association-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-vertex-record-association-control-summary.tsv')
)
$originalVertexRecordAssociationControlSummaryPath = Join-Path $ReportRoot 'eagl-original-vertex-record-association-control-summary.tsv'
$originalVertexRecordAssociationControlSummary = Import-Csv -LiteralPath $originalVertexRecordAssociationControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalVertexRecordAssociationControlSummary -or
    $originalVertexRecordAssociationControlSummary.status -cne 'verified-independent-original-opaque-vertex-record-association-control' -or
    $originalVertexRecordAssociationControlSummary.input_rows -ne $originalVertexRecordAssociationSummary.primitive_rows -or
    $originalVertexRecordAssociationControlSummary.input_unique_rows -ne $originalVertexRecordAssociationSummary.rows_with_unique_opaque_sequences -or
    $originalVertexRecordAssociationControlSummary.recomputed_exact_rows -ne $originalVertexRecordAssociationControlSummary.input_unique_rows -or
    $originalVertexRecordAssociationControlSummary.reverse_stride_match_rows -ne '0' -or
    $originalVertexRecordAssociationControlSummary.shifted_ordinal_match_rows -ne '0' -or
    $originalVertexRecordAssociationControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original opaque __VB association control has unexpected content: $originalVertexRecordAssociationControlSummaryPath"
}
Invoke-Smoke 'original-primitive-stream-topology' @(
    'eagl-original-primitive-stream-topology-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology-summary.tsv')
)
$originalPrimitiveStreamTopologySummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-topology-summary.tsv'
$originalPrimitiveStreamTopologySummary = Import-Csv -LiteralPath $originalPrimitiveStreamTopologySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalPrimitiveStreamTopologySummary -or
    $originalPrimitiveStreamTopologySummary.status -cne 'verified-bounded-original-primitive-stream-topology' -or
    $originalPrimitiveStreamTopologySummary.model_package_candidates -le '0' -or
    $originalPrimitiveStreamTopologySummary.model_packages_loaded -ne $originalPrimitiveStreamTopologySummary.model_package_candidates -or
    $originalPrimitiveStreamTopologySummary.object_candidates -le '0' -or
    $originalPrimitiveStreamTopologySummary.objects_loaded -le '0' -or
    $originalPrimitiveStreamTopologySummary.objects_with_validated_streams -ne $originalPrimitiveStreamTopologySummary.objects_loaded -or
    $originalPrimitiveStreamTopologySummary.primitive_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveStreamTopologySummary.stream_rows -ne $originalVertexRecordAssociationSummary.validated_vertex_stream_rows -or
    $originalPrimitiveStreamTopologySummary.primitive_rows_with_ordinal_matched_streams -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalPrimitiveStreamTopologySummary.stream_rows_with_ordinal_match -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveStreamTopologySummary.stream_rows_with_stride_match -le '0' -or
    $originalPrimitiveStreamTopologySummary.stream_rows_with_valid_payload_range -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveStreamTopologySummary.complete_primitive_rows -le '0' -or
    $originalPrimitiveStreamTopologySummary.objects_with_non_overlapping_payload_ranges -ne $originalPrimitiveStreamTopologySummary.objects_loaded -or
    $originalPrimitiveStreamTopologySummary.reversed_slot_match_rows -ne '0' -or
    $originalPrimitiveStreamTopologySummary.shifted_ordinal_match_rows -le '0' -or
    $originalPrimitiveStreamTopologySummary.rejected_packages -ne '0' -or
    $originalPrimitiveStreamTopologySummary.rejected_objects -ne '0' -or
    $originalPrimitiveStreamTopologySummary.original_semantics -cne 'not-claimed') {
    throw "Original primitive/__VB stream topology summary has unexpected content: $originalPrimitiveStreamTopologySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_primitive_stream_topology_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology-control-summary.tsv')
)
$originalPrimitiveStreamTopologyControlSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-topology-control-summary.tsv'
$originalPrimitiveStreamTopologyControlSummary = Import-Csv -LiteralPath $originalPrimitiveStreamTopologyControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalPrimitiveStreamTopologyControlSummary -or
    $originalPrimitiveStreamTopologyControlSummary.status -cne 'verified-independent-original-primitive-stream-topology-control' -or
    $originalPrimitiveStreamTopologyControlSummary.input_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveStreamTopologyControlSummary.primitive_rows -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalPrimitiveStreamTopologyControlSummary.primitive_groups -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalPrimitiveStreamTopologyControlSummary.groups_with_five_rows -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalPrimitiveStreamTopologyControlSummary.groups_with_structural_match -ne $originalPrimitiveStreamTopologySummary.complete_primitive_rows -or
    $originalPrimitiveStreamTopologyControlSummary.ordinal_match_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows_with_ordinal_match -or
    $originalPrimitiveStreamTopologyControlSummary.stride_match_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows_with_stride_match -or
    $originalPrimitiveStreamTopologyControlSummary.valid_payload_range_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows_with_valid_payload_range -or
    $originalPrimitiveStreamTopologyControlSummary.reversed_slot_match_rows -ne $originalPrimitiveStreamTopologySummary.reversed_slot_match_rows -or
    $originalPrimitiveStreamTopologyControlSummary.shifted_ordinal_match_rows -ne $originalPrimitiveStreamTopologySummary.shifted_ordinal_match_rows -or
    $originalPrimitiveStreamTopologyControlSummary.input_status_valid_rows -ne $originalPrimitiveStreamTopologyControlSummary.input_rows -or
    $originalPrimitiveStreamTopologyControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original primitive/__VB topology control has unexpected content: $originalPrimitiveStreamTopologyControlSummaryPath"
}
Invoke-Smoke 'original-primitive-stream-opaque-discrimination' @(
    'eagl-original-primitive-stream-opaque-discrimination-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination-summary.tsv')
)
$originalOpaqueDiscriminationSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination-summary.tsv'
$originalOpaqueDiscriminationSummary = Import-Csv -LiteralPath $originalOpaqueDiscriminationSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalOpaqueDiscriminationSummary -or
    $originalOpaqueDiscriminationSummary.status -cne 'verified-bounded-original-primitive-stream-opaque-discrimination' -or
    $originalOpaqueDiscriminationSummary.model_package_candidates -ne $originalPrimitiveStreamTopologySummary.model_package_candidates -or
    $originalOpaqueDiscriminationSummary.model_packages_loaded -ne $originalPrimitiveStreamTopologySummary.model_packages_loaded -or
    $originalOpaqueDiscriminationSummary.object_candidates -ne $originalPrimitiveStreamTopologySummary.object_candidates -or
    $originalOpaqueDiscriminationSummary.objects_loaded -ne $originalPrimitiveStreamTopologySummary.objects_loaded -or
    $originalOpaqueDiscriminationSummary.objects_with_validated_streams -ne $originalPrimitiveStreamTopologySummary.objects_with_validated_streams -or
    $originalOpaqueDiscriminationSummary.primitive_rows -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalOpaqueDiscriminationSummary.complete_fetch_rows -ne $originalPrimitiveStreamTopologySummary.complete_primitive_rows -or
    $originalOpaqueDiscriminationSummary.candidate_ordinal_rows -le '0' -or
    $originalOpaqueDiscriminationSummary.stride_matching_candidate_rows -le '0' -or
    $originalOpaqueDiscriminationSummary.metadata_matching_candidate_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.payload_prefix_matching_candidate_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.current_ordinal_rows -ne $originalOpaqueDiscriminationSummary.primitive_rows -or
    $originalOpaqueDiscriminationSummary.current_ordinal_metadata_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.current_ordinal_payload_prefix_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.uniquely_discriminated_by_metadata_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.uniquely_discriminated_by_payload_prefix_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.shifted_stride_candidate_rows -ne $originalPrimitiveStreamTopologySummary.shifted_ordinal_match_rows -or
    $originalOpaqueDiscriminationSummary.shifted_metadata_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.shifted_payload_prefix_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationSummary.rejected_packages -ne '0' -or
    $originalOpaqueDiscriminationSummary.rejected_objects -ne '0' -or
    $originalOpaqueDiscriminationSummary.original_semantics -cne 'not-claimed') {
    throw "Original primitive/__VB opaque discrimination summary has unexpected content: $originalOpaqueDiscriminationSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_primitive_stream_opaque_discrimination_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination-control-summary.tsv')
)
$originalOpaqueDiscriminationControlSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-opaque-discrimination-control-summary.tsv'
$originalOpaqueDiscriminationControlSummary = Import-Csv -LiteralPath $originalOpaqueDiscriminationControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalOpaqueDiscriminationControlSummary -or
    $originalOpaqueDiscriminationControlSummary.status -cne 'verified-independent-original-primitive-stream-opaque-discrimination-control' -or
    $originalOpaqueDiscriminationControlSummary.input_rows -ne $originalOpaqueDiscriminationSummary.primitive_rows -or
    $originalOpaqueDiscriminationControlSummary.primitive_rows -ne $originalOpaqueDiscriminationSummary.primitive_rows -or
    $originalOpaqueDiscriminationControlSummary.complete_fetch_rows -ne $originalOpaqueDiscriminationSummary.complete_fetch_rows -or
    $originalOpaqueDiscriminationControlSummary.candidate_ordinal_rows -ne $originalOpaqueDiscriminationSummary.candidate_ordinal_rows -or
    $originalOpaqueDiscriminationControlSummary.stride_matching_candidate_rows -ne $originalOpaqueDiscriminationSummary.stride_matching_candidate_rows -or
    $originalOpaqueDiscriminationControlSummary.metadata_matching_candidate_rows -ne '0' -or
    $originalOpaqueDiscriminationControlSummary.payload_prefix_matching_candidate_rows -ne '0' -or
    $originalOpaqueDiscriminationControlSummary.shifted_stride_candidate_rows -ne $originalOpaqueDiscriminationSummary.shifted_stride_candidate_rows -or
    $originalOpaqueDiscriminationControlSummary.shifted_metadata_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationControlSummary.shifted_payload_prefix_matching_rows -ne '0' -or
    $originalOpaqueDiscriminationControlSummary.candidate_list_coherent_rows -ne $originalOpaqueDiscriminationControlSummary.input_rows -or
    $originalOpaqueDiscriminationControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original primitive/__VB opaque discrimination control has unexpected content: $originalOpaqueDiscriminationControlSummaryPath"
}
Invoke-Smoke 'original-primitive-stream-command-payload-identity' @(
    'eagl-original-primitive-stream-command-payload-identity-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity-summary.tsv')
)
$originalCommandPayloadIdentitySummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity-summary.tsv'
$originalCommandPayloadIdentitySummary = Import-Csv -LiteralPath $originalCommandPayloadIdentitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCommandPayloadIdentitySummary -or
    $originalCommandPayloadIdentitySummary.status -cne 'verified-bounded-original-primitive-stream-command-payload-identity-control' -or
    $originalCommandPayloadIdentitySummary.model_package_candidates -ne $originalPrimitiveStreamTopologySummary.model_package_candidates -or
    $originalCommandPayloadIdentitySummary.model_packages_loaded -ne $originalPrimitiveStreamTopologySummary.model_packages_loaded -or
    $originalCommandPayloadIdentitySummary.object_candidates -ne $originalPrimitiveStreamTopologySummary.object_candidates -or
    $originalCommandPayloadIdentitySummary.objects_loaded -ne $originalPrimitiveStreamTopologySummary.objects_loaded -or
    $originalCommandPayloadIdentitySummary.objects_with_validated_streams -ne $originalPrimitiveStreamTopologySummary.objects_with_validated_streams -or
    $originalCommandPayloadIdentitySummary.primitive_rows -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalCommandPayloadIdentitySummary.complete_fetch_rows -ne $originalOpaqueDiscriminationSummary.complete_fetch_rows -or
    $originalCommandPayloadIdentitySummary.candidate_ordinal_rows -ne $originalOpaqueDiscriminationSummary.candidate_ordinal_rows -or
    $originalCommandPayloadIdentitySummary.raw_identity_candidate_rows -ne $originalCommandPayloadIdentitySummary.candidate_ordinal_rows -or
    $originalCommandPayloadIdentitySummary.distinct_raw_identity_rows -ne $originalCommandPayloadIdentitySummary.raw_identity_candidate_rows -or
    $originalCommandPayloadIdentitySummary.duplicate_raw_identity_rows -ne '0' -or
    $originalCommandPayloadIdentitySummary.command_payload_exact_candidate_rows -ne '0' -or
    $originalCommandPayloadIdentitySummary.current_ordinal_rows -ne $originalCommandPayloadIdentitySummary.primitive_rows -or
    $originalCommandPayloadIdentitySummary.current_ordinal_raw_identity_unique_rows -ne $originalCommandPayloadIdentitySummary.primitive_rows -or
    $originalCommandPayloadIdentitySummary.current_ordinal_command_payload_exact_rows -ne '0' -or
    $originalCommandPayloadIdentitySummary.current_ordinal_uniquely_discriminated_by_command_payload_rows -ne '0' -or
    $originalCommandPayloadIdentitySummary.shifted_ordinal_command_payload_exact_rows -ne '0' -or
    $originalCommandPayloadIdentitySummary.rejected_packages -ne '0' -or
    $originalCommandPayloadIdentitySummary.rejected_objects -ne '0' -or
    $originalCommandPayloadIdentitySummary.original_semantics -cne 'not-claimed') {
    throw "Original primitive/__VB command-payload identity summary has unexpected content: $originalCommandPayloadIdentitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_primitive_stream_command_payload_identity_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity-control-summary.tsv')
)
$originalCommandPayloadIdentityControlSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-command-payload-identity-control-summary.tsv'
$originalCommandPayloadIdentityControlSummary = Import-Csv -LiteralPath $originalCommandPayloadIdentityControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCommandPayloadIdentityControlSummary -or
    $originalCommandPayloadIdentityControlSummary.status -cne 'verified-independent-original-primitive-stream-command-payload-identity-control' -or
    $originalCommandPayloadIdentityControlSummary.input_rows -ne $originalCommandPayloadIdentitySummary.primitive_rows -or
    $originalCommandPayloadIdentityControlSummary.primitive_rows -ne $originalCommandPayloadIdentitySummary.primitive_rows -or
    $originalCommandPayloadIdentityControlSummary.complete_fetch_rows -ne $originalCommandPayloadIdentitySummary.complete_fetch_rows -or
    $originalCommandPayloadIdentityControlSummary.candidate_ordinal_rows -ne $originalCommandPayloadIdentitySummary.candidate_ordinal_rows -or
    $originalCommandPayloadIdentityControlSummary.raw_identity_candidate_rows -ne $originalCommandPayloadIdentitySummary.raw_identity_candidate_rows -or
    $originalCommandPayloadIdentityControlSummary.distinct_raw_identity_rows -ne $originalCommandPayloadIdentitySummary.distinct_raw_identity_rows -or
    $originalCommandPayloadIdentityControlSummary.duplicate_raw_identity_rows -ne '0' -or
    $originalCommandPayloadIdentityControlSummary.command_payload_exact_candidate_rows -ne '0' -or
    $originalCommandPayloadIdentityControlSummary.current_ordinal_rows -ne $originalCommandPayloadIdentitySummary.current_ordinal_rows -or
    $originalCommandPayloadIdentityControlSummary.current_ordinal_raw_identity_unique_rows -ne $originalCommandPayloadIdentitySummary.current_ordinal_raw_identity_unique_rows -or
    $originalCommandPayloadIdentityControlSummary.current_ordinal_command_payload_exact_rows -ne '0' -or
    $originalCommandPayloadIdentityControlSummary.current_ordinal_uniquely_discriminated_by_command_payload_rows -ne '0' -or
    $originalCommandPayloadIdentityControlSummary.shifted_ordinal_command_payload_exact_rows -ne '0' -or
    $originalCommandPayloadIdentityControlSummary.input_status_valid_rows -ne $originalCommandPayloadIdentityControlSummary.input_rows -or
    $originalCommandPayloadIdentityControlSummary.candidate_list_coherent_rows -ne $originalCommandPayloadIdentityControlSummary.input_rows -or
    $originalCommandPayloadIdentityControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original primitive/__VB command-payload identity control has unexpected content: $originalCommandPayloadIdentityControlSummaryPath"
}
Invoke-Smoke 'original-primitive-stream-command-descriptor-adjacency' @(
    'eagl-original-primitive-stream-command-descriptor-adjacency-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-summary.tsv')
)
$originalCommandDescriptorAdjacencySummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-summary.tsv'
$originalCommandDescriptorAdjacencySummary = Import-Csv -LiteralPath $originalCommandDescriptorAdjacencySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCommandDescriptorAdjacencySummary -or
    $originalCommandDescriptorAdjacencySummary.status -cne 'verified-bounded-original-primitive-stream-command-descriptor-adjacency' -or
    $originalCommandDescriptorAdjacencySummary.command_descriptor_delta -ne '12' -or
    $originalCommandDescriptorAdjacencySummary.model_package_candidates -ne $originalPrimitiveStreamTopologySummary.model_package_candidates -or
    $originalCommandDescriptorAdjacencySummary.model_packages_loaded -ne $originalPrimitiveStreamTopologySummary.model_packages_loaded -or
    $originalCommandDescriptorAdjacencySummary.object_candidates -ne $originalPrimitiveStreamTopologySummary.object_candidates -or
    $originalCommandDescriptorAdjacencySummary.objects_loaded -ne $originalPrimitiveStreamTopologySummary.objects_loaded -or
    $originalCommandDescriptorAdjacencySummary.objects_with_validated_streams -ne $originalPrimitiveStreamTopologySummary.objects_with_validated_streams -or
    $originalCommandDescriptorAdjacencySummary.primitive_rows -ne $originalPrimitiveStreamTopologySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.complete_command_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.identity_candidate_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.current_ordinal_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.current_ordinal_identity_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.unique_current_ordinal_identity_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.shifted_ordinal_identity_rows -ne '0' -or
    $originalCommandDescriptorAdjacencySummary.same_section_identity_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencySummary.exact_adjacency_stream_rows -le '0' -or
    [string]::IsNullOrWhiteSpace($originalCommandDescriptorAdjacencySummary.exact_adjacency_category_histogram) -or
    [string]::IsNullOrWhiteSpace($originalCommandDescriptorAdjacencySummary.exact_adjacency_stride_histogram) -or
    $originalCommandDescriptorAdjacencySummary.rejected_packages -ne '0' -or
    $originalCommandDescriptorAdjacencySummary.rejected_objects -ne '0' -or
    $originalCommandDescriptorAdjacencySummary.original_semantics -cne 'not-claimed') {
    throw "Original command/__VB descriptor adjacency summary has unexpected content: $originalCommandDescriptorAdjacencySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_primitive_stream_command_descriptor_adjacency_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-control-summary.tsv')
)
$originalCommandDescriptorAdjacencyControlSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-control-summary.tsv'
$originalCommandDescriptorAdjacencyControlSummary = Import-Csv -LiteralPath $originalCommandDescriptorAdjacencyControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCommandDescriptorAdjacencyControlSummary -or
    $originalCommandDescriptorAdjacencyControlSummary.status -cne 'verified-independent-original-primitive-stream-command-descriptor-adjacency-control' -or
    $originalCommandDescriptorAdjacencyControlSummary.command_descriptor_delta -ne '12' -or
    $originalCommandDescriptorAdjacencyControlSummary.render_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.primitive_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.complete_command_rows -ne $originalCommandDescriptorAdjacencySummary.complete_command_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.candidate_ordinal_rows -ne $originalCommandDescriptorAdjacencySummary.candidate_ordinal_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.identity_candidate_rows -ne $originalCommandDescriptorAdjacencySummary.identity_candidate_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.current_ordinal_rows -ne $originalCommandDescriptorAdjacencySummary.current_ordinal_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.current_ordinal_identity_rows -ne $originalCommandDescriptorAdjacencySummary.current_ordinal_identity_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.unique_current_ordinal_identity_rows -ne $originalCommandDescriptorAdjacencySummary.unique_current_ordinal_identity_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.shifted_ordinal_identity_rows -ne '0' -or
    $originalCommandDescriptorAdjacencyControlSummary.same_section_identity_rows -ne $originalCommandDescriptorAdjacencySummary.same_section_identity_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.exact_adjacency_stream_rows -ne $originalCommandDescriptorAdjacencySummary.exact_adjacency_stream_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.exact_adjacency_category_histogram -cne $originalCommandDescriptorAdjacencySummary.exact_adjacency_category_histogram -or
    $originalCommandDescriptorAdjacencyControlSummary.exact_adjacency_stride_histogram -cne $originalCommandDescriptorAdjacencySummary.exact_adjacency_stride_histogram -or
    $originalCommandDescriptorAdjacencyControlSummary.render_input_status_valid_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalCommandDescriptorAdjacencyControlSummary.topology_input_status_valid_rows -le '0' -or
    $originalCommandDescriptorAdjacencyControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent command/__VB descriptor adjacency control has unexpected content: $originalCommandDescriptorAdjacencyControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_fetch_adjacency_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-fetch-adjacency-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-fetch-adjacency-control-summary.tsv')
)
$originalStateFetchAdjacencyControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-fetch-adjacency-control-summary.tsv'
$originalStateFetchAdjacencyControlSummary = Import-Csv -LiteralPath $originalStateFetchAdjacencyControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateFetchAdjacencyControlSummary -or
    $originalStateFetchAdjacencyControlSummary.status -cne 'verified-independent-original-state-fetch-adjacency-control' -or
    $originalStateFetchAdjacencyControlSummary.render_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.adjacency_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.joined_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.render_input_status_valid_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.adjacency_input_status_valid_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.joined_exact_adjacency_rows -ne $originalCommandDescriptorAdjacencySummary.primitive_rows -or
    $originalStateFetchAdjacencyControlSummary.reported_state_fetch_overlap_rows -ne $originalStateFetchAdjacencyControlSummary.recomputed_state_fetch_overlap_rows -or
    $originalStateFetchAdjacencyControlSummary.reported_state_shape_primitive_type_overlap_rows -ne $originalStateFetchAdjacencyControlSummary.recomputed_state_shape_primitive_type_overlap_rows -or
    [string]::IsNullOrWhiteSpace($originalStateFetchAdjacencyControlSummary.exact_adjacency_category_histogram) -or
    $originalStateFetchAdjacencyControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/fetch adjacency control has unexpected content: $originalStateFetchAdjacencyControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_primitive_fetch_topology_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-primitive-fetch-topology-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-primitive-fetch-topology-control-summary.tsv')
)
$originalPrimitiveFetchTopologyControlSummaryPath = Join-Path $ReportRoot 'eagl-original-primitive-fetch-topology-control-summary.tsv'
$originalPrimitiveFetchTopologyControlSummary = Import-Csv -LiteralPath $originalPrimitiveFetchTopologyControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalPrimitiveFetchTopologyControlSummary -or
    $originalPrimitiveFetchTopologyControlSummary.status -cne 'verified-independent-original-primitive-fetch-topology-control' -or
    $originalPrimitiveFetchTopologyControlSummary.render_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveFetchTopologyControlSummary.joined_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.render_input_status_valid_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_input_status_valid_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_groups_with_five_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_valid_slot_rows -ne '11818' -or
    $originalPrimitiveFetchTopologyControlSummary.topology_key_ordinal_match_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_valid_payload_range_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalPrimitiveFetchTopologyControlSummary.topology_structural_match_rows -ne '11810' -or
    $originalPrimitiveFetchTopologyControlSummary.render_fetch_slot_rows -ne '11818' -or
    $originalPrimitiveFetchTopologyControlSummary.render_complete_five_slot_rows -ne '2362' -or
    $originalPrimitiveFetchTopologyControlSummary.topology_complete_five_slot_rows -ne '2362' -or
    $originalPrimitiveFetchTopologyControlSummary.fetch_slot_count_match_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.fetch_slot_id_set_match_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.complete_five_slot_sequence_agreement_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalPrimitiveFetchTopologyControlSummary.incomplete_slot_two_rows -ne '2' -or
    $originalPrimitiveFetchTopologyControlSummary.missing_render_keys -ne '0' -or
    $originalPrimitiveFetchTopologyControlSummary.missing_topology_keys -ne '0' -or
    $originalPrimitiveFetchTopologyControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original primitive/fetch topology control has unexpected content: $originalPrimitiveFetchTopologyControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_command_payload_region_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-command-payload-region-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-command-payload-region-control-summary.tsv')
)
$originalCommandPayloadRegionControlSummaryPath = Join-Path $ReportRoot 'eagl-original-command-payload-region-control-summary.tsv'
$originalCommandPayloadRegionControlSummary = Import-Csv -LiteralPath $originalCommandPayloadRegionControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalCommandPayloadRegionControlSummary -or
    $originalCommandPayloadRegionControlSummary.status -cne 'verified-independent-original-command-payload-region-control' -or
    $originalCommandPayloadRegionControlSummary.render_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.topology_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalCommandPayloadRegionControlSummary.joined_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.render_input_status_valid_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.topology_input_status_valid_rows -ne $originalPrimitiveStreamTopologySummary.stream_rows -or
    $originalCommandPayloadRegionControlSummary.topology_groups_with_five_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.payload_range_valid_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.payload_interval_nonoverlap_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_section_match_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_interval_valid_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_payload_nonoverlap_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_after_payload_end_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_payload_gap_equals_0x10_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalCommandPayloadRegionControlSummary.command_payload_gap_histogram -cne '16:2364' -or
    $originalCommandPayloadRegionControlSummary.incomplete_slot_two_rows -ne '2' -or
    $originalCommandPayloadRegionControlSummary.missing_render_keys -ne '0' -or
    $originalCommandPayloadRegionControlSummary.missing_topology_keys -ne '0' -or
    $originalCommandPayloadRegionControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original command/payload region control has unexpected content: $originalCommandPayloadRegionControlSummaryPath"
}
Invoke-Smoke 'original-vertex-payload' @(
    'eagl-original-vertex-payload-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-vertex-payload.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-vertex-payload-summary.tsv'), '--stream-report',
    (Join-Path $ReportRoot 'eagl-original-vertex-stream-numeric.tsv')
)
$originalVertexPayloadSummaryPath = Join-Path $ReportRoot 'eagl-original-vertex-payload-summary.tsv'
$originalVertexPayloadSummary = Import-Csv -LiteralPath $originalVertexPayloadSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalVertexPayloadSummary -or
    $originalVertexPayloadSummary.status -cne 'verified-bounded-original-numeric-payload-structure' -or
    $originalVertexPayloadSummary.model_package_candidates -le '0' -or
    $originalVertexPayloadSummary.model_packages_loaded -ne $originalVertexPayloadSummary.model_package_candidates -or
    $originalVertexPayloadSummary.object_candidates -le '0' -or
    $originalVertexPayloadSummary.objects_loaded -le '0' -or
    $originalVertexPayloadSummary.validated_vertex_stream_rows -le '0' -or
    $originalVertexPayloadSummary.objects_with_validated_vertex_streams -ne $originalVertexPayloadSummary.objects_loaded -or
    $originalVertexPayloadSummary.objects_with_finite_packed_float_profiles -le '0' -or
    $originalVertexPayloadSummary.objects_with_finite_float2_profiles -le '0' -or
    $originalVertexPayloadSummary.objects_with_declared_bounds_containment -le '0' -or
    $originalVertexPayloadSummary.payload_rows -ne $originalVertexPayloadSummary.objects_loaded -or
    $originalVertexPayloadSummary.objects_with_distinct_payload_ranges -ne $originalVertexPayloadSummary.objects_loaded -or
    $originalVertexPayloadSummary.rejected_packages -ne '0' -or
    $originalVertexPayloadSummary.rejected_objects -ne '0' -or
    $originalVertexPayloadSummary.original_semantics -cne 'not-claimed') {
    throw "Original numeric __VB payload summary has unexpected content: $originalVertexPayloadSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_vertex_payload_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-payload.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-vertex-payload-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-vertex-payload-control-summary.tsv')
)
$originalVertexPayloadControlSummaryPath = Join-Path $ReportRoot 'eagl-original-vertex-payload-control-summary.tsv'
$originalVertexPayloadControlSummary = Import-Csv -LiteralPath $originalVertexPayloadControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalVertexPayloadControlSummary -or
    $originalVertexPayloadControlSummary.status -cne 'verified-independent-original-numeric-payload-control' -or
    $originalVertexPayloadControlSummary.input_rows -ne $originalVertexPayloadSummary.payload_rows -or
    $originalVertexPayloadControlSummary.object_rows -ne $originalVertexPayloadSummary.objects_loaded -or
    $originalVertexPayloadControlSummary.numeric_signature_match_rows -ne $originalVertexPayloadControlSummary.input_rows -or
    $originalVertexPayloadControlSummary.payload_range_metadata_valid_rows -ne $originalVertexPayloadControlSummary.input_rows -or
    $originalVertexPayloadControlSummary.input_status_valid_rows -ne $originalVertexPayloadControlSummary.input_rows -or
    $originalVertexPayloadControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original numeric __VB payload control has unexpected content: $originalVertexPayloadControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_numeric_stream_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-payload.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-numeric-stream-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-numeric-stream-ambiguity-control-summary.tsv')
)
$originalNumericStreamAmbiguitySummaryPath = Join-Path $ReportRoot 'eagl-original-numeric-stream-ambiguity-control-summary.tsv'
$originalNumericStreamAmbiguitySummary = Import-Csv -LiteralPath $originalNumericStreamAmbiguitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalNumericStreamAmbiguitySummary -or
    $originalNumericStreamAmbiguitySummary.status -cne 'verified-independent-original-numeric-stream-ambiguity-control' -or
    $originalNumericStreamAmbiguitySummary.input_rows -ne $originalVertexPayloadSummary.payload_rows -or
    $originalNumericStreamAmbiguitySummary.valid_input_rows -ne $originalNumericStreamAmbiguitySummary.input_rows -or
    $originalNumericStreamAmbiguitySummary.objects_with_no_bounds_match -ne '1' -or
    $originalNumericStreamAmbiguitySummary.objects_with_unique_bounds_match -ne '494' -or
    $originalNumericStreamAmbiguitySummary.objects_with_multiple_bounds_matches -ne '388' -or
    $originalNumericStreamAmbiguitySummary.objects_with_non_unique_or_missing_match -ne '389' -or
    $originalNumericStreamAmbiguitySummary.bounds_match_cardinality_histogram -cne '0=1,1=494,4=196,5=160,6=1,7=4,8=9,9=7,10=8,11=1,12=1,13=1' -or
    $originalNumericStreamAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $originalNumericStreamAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original numeric stream ambiguity control has unexpected content: $originalNumericStreamAmbiguitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_bounds_consistent_record_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-stream-numeric.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-bounds-consistent-record-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-bounds-consistent-record-control-summary.tsv')
)
$originalBoundsConsistentRecordSummaryPath = Join-Path $ReportRoot 'eagl-original-bounds-consistent-record-control-summary.tsv'
$originalBoundsConsistentRecordSummary = Import-Csv -LiteralPath $originalBoundsConsistentRecordSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalBoundsConsistentRecordSummary -or
    $originalBoundsConsistentRecordSummary.status -cne 'verified-independent-original-bounds-consistent-record-control' -or
    $originalBoundsConsistentRecordSummary.input_rows -ne $originalVertexPayloadSummary.validated_vertex_stream_rows -or
    $originalBoundsConsistentRecordSummary.object_ordinal_groups -ne $originalRenderChainSummary.primitive_rows -or
    $originalBoundsConsistentRecordSummary.complete_five_stream_groups -ne $originalBoundsConsistentRecordSummary.object_ordinal_groups -or
    $originalBoundsConsistentRecordSummary.category_sequence_valid_groups -ne $originalBoundsConsistentRecordSummary.object_ordinal_groups -or
    $originalBoundsConsistentRecordSummary.valid_stream_rows -ne $originalBoundsConsistentRecordSummary.input_rows -or
    $originalBoundsConsistentRecordSummary.first_raw_stride12_groups -ne $originalBoundsConsistentRecordSummary.object_ordinal_groups -or
    $originalBoundsConsistentRecordSummary.first_raw_bounds_match_groups -ne '2363' -or
    $originalBoundsConsistentRecordSummary.first_raw_no_bounds_match_groups -ne '1' -or
    $originalBoundsConsistentRecordSummary.later_raw_bounds_match_groups -ne '0' -or
    $originalBoundsConsistentRecordSummary.all_bounds_matches_are_first_raw -cne 'True' -or
    $originalBoundsConsistentRecordSummary.first_raw_category_histogram -cne '10=2010,11=354' -or
    $originalBoundsConsistentRecordSummary.semantic_binding -cne 'not-proven' -or
    $originalBoundsConsistentRecordSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original bounds-consistent record control has unexpected content: $originalBoundsConsistentRecordSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_bounds_fetch_slot_join_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-stream-numeric.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-bounds-fetch-slot-join-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-bounds-fetch-slot-join-control-summary.tsv')
)
$originalBoundsFetchSlotJoinSummaryPath = Join-Path $ReportRoot 'eagl-original-bounds-fetch-slot-join-control-summary.tsv'
$originalBoundsFetchSlotJoinSummary = Import-Csv -LiteralPath $originalBoundsFetchSlotJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalBoundsFetchSlotJoinSummary -or
    $originalBoundsFetchSlotJoinSummary.status -cne 'verified-independent-original-bounds-fetch-slot-join-control' -or
    $originalBoundsFetchSlotJoinSummary.numeric_rows -ne $originalVertexPayloadSummary.validated_vertex_stream_rows -or
    $originalBoundsFetchSlotJoinSummary.topology_rows -ne $originalVertexPayloadSummary.validated_vertex_stream_rows -or
    $originalBoundsFetchSlotJoinSummary.joined_rows -ne $originalBoundsFetchSlotJoinSummary.numeric_rows -or
    $originalBoundsFetchSlotJoinSummary.missing_numeric_keys -ne '0' -or
    $originalBoundsFetchSlotJoinSummary.missing_topology_keys -ne '0' -or
    $originalBoundsFetchSlotJoinSummary.object_ordinal_groups -ne $originalRenderChainSummary.primitive_rows -or
    $originalBoundsFetchSlotJoinSummary.complete_five_stream_groups -ne $originalBoundsFetchSlotJoinSummary.object_ordinal_groups -or
    $originalBoundsFetchSlotJoinSummary.identity_join_rows -ne $originalBoundsFetchSlotJoinSummary.joined_rows -or
    $originalBoundsFetchSlotJoinSummary.numeric_input_status_valid_rows -ne $originalBoundsFetchSlotJoinSummary.numeric_rows -or
    $originalBoundsFetchSlotJoinSummary.topology_input_status_valid_rows -ne $originalBoundsFetchSlotJoinSummary.topology_rows -or
    $originalBoundsFetchSlotJoinSummary.first_raw_fetch_slot_zero_groups -ne $originalBoundsFetchSlotJoinSummary.object_ordinal_groups -or
    $originalBoundsFetchSlotJoinSummary.first_raw_stride_match_groups -ne $originalBoundsFetchSlotJoinSummary.object_ordinal_groups -or
    $originalBoundsFetchSlotJoinSummary.first_raw_bounds_match_groups -ne '2363' -or
    $originalBoundsFetchSlotJoinSummary.first_raw_no_bounds_match_groups -ne '1' -or
    $originalBoundsFetchSlotJoinSummary.later_raw_bounds_match_groups -ne '0' -or
    $originalBoundsFetchSlotJoinSummary.incomplete_fetch_slot_groups -ne '2' -or
    $originalBoundsFetchSlotJoinSummary.semantic_binding -cne 'not-proven' -or
    $originalBoundsFetchSlotJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original bounds/fetch-slot join control has unexpected content: $originalBoundsFetchSlotJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_numeric_slot_role_discrimination_control.py'),
    (Join-Path $ReportRoot 'eagl-original-vertex-stream-numeric.tsv'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control-summary.tsv')
)
$originalNumericSlotRoleDiscriminationSummaryPath = Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control-summary.tsv'
$originalNumericSlotRoleDiscriminationSummary = Import-Csv -LiteralPath $originalNumericSlotRoleDiscriminationSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalNumericSlotRoleDiscriminationSummary -or
    $originalNumericSlotRoleDiscriminationSummary.status -cne 'verified-independent-original-numeric-slot-role-discrimination-control' -or
    $originalNumericSlotRoleDiscriminationSummary.numeric_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.topology_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.joined_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.per_primitive_slot_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.missing_numeric_keys -ne '0' -or
    $originalNumericSlotRoleDiscriminationSummary.missing_topology_keys -ne '0' -or
    $originalNumericSlotRoleDiscriminationSummary.duplicate_numeric_keys -ne '0' -or
    $originalNumericSlotRoleDiscriminationSummary.duplicate_topology_keys -ne '0' -or
    $originalNumericSlotRoleDiscriminationSummary.primitive_groups -ne '2364' -or
    $originalNumericSlotRoleDiscriminationSummary.groups_with_five_rows -ne '2364' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_five_slot_groups -ne '2362' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_unique_near_unit_float2 -ne '2362' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_near_unit_float2 -ne '2362' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_slot1_near_unit_float2 -ne '2362' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_other_slot_near_unit_float2 -ne '0' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_slot0_bounds_match -ne '2361' -or
    $originalNumericSlotRoleDiscriminationSummary.complete_groups_with_no_slot0_bounds_match -ne '1' -or
    $originalNumericSlotRoleDiscriminationSummary.incomplete_five_slot_groups -ne '2' -or
    $originalNumericSlotRoleDiscriminationSummary.numeric_input_status_valid_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.topology_input_status_valid_rows -ne '11820' -or
    $originalNumericSlotRoleDiscriminationSummary.slot_row_count_histogram -cne '5=2364' -or
    $originalNumericSlotRoleDiscriminationSummary.near_unit_float2_slot_histogram -cne '1=2364' -or
    $originalNumericSlotRoleDiscriminationSummary.semantic_binding -cne 'not-proven' -or
    $originalNumericSlotRoleDiscriminationSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original numeric slot-role discrimination control has unexpected content: $originalNumericSlotRoleDiscriminationSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_numeric_slot_cooccurrence_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-fetch-adjacency-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-numeric-slot-cooccurrence-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-numeric-slot-cooccurrence-control-summary.tsv')
)
$originalStateNumericSlotCooccurrenceSummaryPath = Join-Path $ReportRoot 'eagl-original-state-numeric-slot-cooccurrence-control-summary.tsv'
$originalStateNumericSlotCooccurrenceSummary = Import-Csv -LiteralPath $originalStateNumericSlotCooccurrenceSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateNumericSlotCooccurrenceSummary -or
    $originalStateNumericSlotCooccurrenceSummary.status -cne 'verified-independent-original-state-numeric-slot-cooccurrence-control' -or
    $originalStateNumericSlotCooccurrenceSummary.state_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.role_rows -ne '11820' -or
    $originalStateNumericSlotCooccurrenceSummary.joined_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.missing_state_keys -ne '0' -or
    $originalStateNumericSlotCooccurrenceSummary.missing_role_keys -ne '0' -or
    $originalStateNumericSlotCooccurrenceSummary.duplicate_state_keys -ne '0' -or
    $originalStateNumericSlotCooccurrenceSummary.duplicate_role_keys -ne '0' -or
    $originalStateNumericSlotCooccurrenceSummary.state_input_status_valid_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.role_input_status_valid_rows -ne '11820' -or
    $originalStateNumericSlotCooccurrenceSummary.primitive_groups -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.groups_with_five_role_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_five_slot_groups -ne '2362' -or
    $originalStateNumericSlotCooccurrenceSummary.state_relocation_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.shapename_state_rows -ne '752' -or
    $originalStateNumericSlotCooccurrenceSummary.primitive_type_state_rows -ne '1612' -or
    $originalStateNumericSlotCooccurrenceSummary.exact_adjacency_stream_rows -ne '2364' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_with_shapename_state -ne '750' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_without_shapename_state -ne '1612' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_with_primitive_type_state -ne '1612' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_without_primitive_type_state -ne '750' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_with_unique_near_unit_float2 -ne '2362' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_with_slot1_near_unit_float2 -ne '2362' -or
    $originalStateNumericSlotCooccurrenceSummary.complete_groups_with_other_slot_near_unit_float2 -ne '0' -or
    $originalStateNumericSlotCooccurrenceSummary.incomplete_five_slot_groups -ne '2' -or
    $originalStateNumericSlotCooccurrenceSummary.role_row_count_histogram -cne '5=2364' -or
    $originalStateNumericSlotCooccurrenceSummary.semantic_binding -cne 'not-proven' -or
    $originalStateNumericSlotCooccurrenceSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/numeric slot cooccurrence control has unexpected content: $originalStateNumericSlotCooccurrenceSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_command_slot_role_cooccurrence_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-command-descriptor-adjacency-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-command-slot-role-cooccurrence-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-command-slot-role-cooccurrence-control-summary.tsv')
)
$originalCommandSlotRoleCooccurrenceSummaryPath = Join-Path $ReportRoot 'eagl-original-command-slot-role-cooccurrence-control-summary.tsv'
$originalCommandSlotRoleCooccurrenceSummary = Import-Csv -LiteralPath $originalCommandSlotRoleCooccurrenceSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCommandSlotRoleCooccurrenceSummary -or
    $originalCommandSlotRoleCooccurrenceSummary.status -cne 'verified-independent-original-command-slot-role-cooccurrence-control' -or
    $originalCommandSlotRoleCooccurrenceSummary.descriptor_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.role_rows -ne '11820' -or
    $originalCommandSlotRoleCooccurrenceSummary.joined_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.missing_descriptor_keys -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.missing_role_keys -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.duplicate_descriptor_keys -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.duplicate_role_keys -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.descriptor_input_status_valid_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.role_input_status_valid_rows -ne '11820' -or
    $originalCommandSlotRoleCooccurrenceSummary.current_ordinal_present_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.current_ordinal_identity_match_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.current_ordinal_uniquely_identified_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.shifted_ordinal_identity_match_rows -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.primitive_groups -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.groups_with_five_role_rows -ne '2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.complete_five_slot_groups -ne '2362' -or
    $originalCommandSlotRoleCooccurrenceSummary.complete_groups_with_unique_near_unit_float2 -ne '2362' -or
    $originalCommandSlotRoleCooccurrenceSummary.complete_groups_with_slot1_near_unit_float2 -ne '2362' -or
    $originalCommandSlotRoleCooccurrenceSummary.complete_groups_with_other_slot_near_unit_float2 -ne '0' -or
    $originalCommandSlotRoleCooccurrenceSummary.incomplete_five_slot_groups -ne '2' -or
    $originalCommandSlotRoleCooccurrenceSummary.role_row_count_histogram -cne '5=2364' -or
    $originalCommandSlotRoleCooccurrenceSummary.semantic_binding -cne 'not-proven' -or
    $originalCommandSlotRoleCooccurrenceSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original command/slot-role cooccurrence control has unexpected content: $originalCommandSlotRoleCooccurrenceSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_population_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-population-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-population-control-summary.tsv')
)
$originalStatePopulationControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-population-control-summary.tsv'
$originalStatePopulationControlSummary = Import-Csv -LiteralPath $originalStatePopulationControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStatePopulationControlSummary -or
    $originalStatePopulationControlSummary.status -cne 'verified-bounded-original-state-population-control' -or
    $originalStatePopulationControlSummary.object_rows -le '0' -or
    $originalStatePopulationControlSummary.objects_with_rows -ne $originalStatePopulationControlSummary.object_rows -or
    $originalStatePopulationControlSummary.primitive_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalStatePopulationControlSummary.shapename_rows -ne $originalRenderChainSummary.shapename_state_rows -or
    $originalStatePopulationControlSummary.primitive_type_rows -ne $originalRenderChainSummary.primitive_type_state_rows -or
    $originalStatePopulationControlSummary.population_overlap_rows -ne '0' -or
    $originalStatePopulationControlSummary.original_semantics -cne 'not-claimed') {
    throw "Original EAGL state-population control summary has unexpected content: $originalStatePopulationControlSummaryPath"
}
Invoke-Smoke 'original-state-token' @(
    'eagl-original-state-token-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-original-state-token.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-state-token-summary.tsv')
)
$originalStateTokenSummaryPath = Join-Path $ReportRoot 'eagl-original-state-token-summary.tsv'
$originalStateTokenSummary = Import-Csv -LiteralPath $originalStateTokenSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateTokenSummary -or
    $originalStateTokenSummary.status -cne 'verified-bounded-original-state-token-negative-control' -or
    $originalStateTokenSummary.input_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalStateTokenSummary.state_rows -ne $originalStateTokenSummary.input_rows -or
    $originalStateTokenSummary.valid_input_rows -ne $originalStateTokenSummary.input_rows -or
    $originalStateTokenSummary.shapename_rows -ne $originalRenderChainSummary.shapename_state_rows -or
    $originalStateTokenSummary.clamp_u_rows -ne $originalStateTokenSummary.shapename_rows -or
    $originalStateTokenSummary.clamp_v_rows -ne $originalStateTokenSummary.shapename_rows -or
    $originalStateTokenSummary.primitive_type_rows -ne $originalRenderChainSummary.primitive_type_state_rows -or
    $originalStateTokenSummary.texture_token_rows -ne '0' -or
    $originalStateTokenSummary.material_token_rows -ne '0' -or
    $originalStateTokenSummary.uv_coordinate_token_rows -ne '0' -or
    $originalStateTokenSummary.vertex_attribute_token_rows -ne '0' -or
    $originalStateTokenSummary.deformation_token_rows -ne '0' -or
    $originalStateTokenSummary.original_semantics -cne 'not-claimed') {
    throw "Original EAGL state-token summary has unexpected content: $originalStateTokenSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_token_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-token.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-token-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-token-control-summary.tsv')
)
$originalStateTokenControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-token-control-summary.tsv'
$originalStateTokenControlSummary = Import-Csv -LiteralPath $originalStateTokenControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateTokenControlSummary -or
    $originalStateTokenControlSummary.status -cne 'verified-independent-original-state-token-control' -or
    $originalStateTokenControlSummary.input_rows -ne $originalStateTokenSummary.input_rows -or
    $originalStateTokenControlSummary.state_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.valid_input_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.assignment_name_reparse_match_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.classification_flags_match_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.negative_token_counts_zero_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.valid_rows -ne $originalStateTokenControlSummary.input_rows -or
    $originalStateTokenControlSummary.texture_token_rows -ne '0' -or
    $originalStateTokenControlSummary.material_token_rows -ne '0' -or
    $originalStateTokenControlSummary.uv_coordinate_token_rows -ne '0' -or
    $originalStateTokenControlSummary.vertex_attribute_token_rows -ne '0' -or
    $originalStateTokenControlSummary.deformation_token_rows -ne '0' -or
    $originalStateTokenControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original EAGL state-token control has unexpected content: $originalStateTokenControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_render_payload_object_join.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-original-vertex-payload.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-render-payload-object-join.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-render-payload-object-join-summary.tsv')
)
$originalRenderPayloadObjectJoinSummaryPath = Join-Path $ReportRoot 'eagl-original-render-payload-object-join-summary.tsv'
$originalRenderPayloadObjectJoinSummary = Import-Csv -LiteralPath $originalRenderPayloadObjectJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalRenderPayloadObjectJoinSummary -or
    $originalRenderPayloadObjectJoinSummary.status -cne 'verified-independent-original-render-payload-object-join' -or
    $originalRenderPayloadObjectJoinSummary.render_object_rows -ne $originalRenderChainSummary.objects_loaded -or
    $originalRenderPayloadObjectJoinSummary.payload_object_rows -ne $originalVertexPayloadSummary.objects_loaded -or
    $originalRenderPayloadObjectJoinSummary.joined_object_rows -ne $originalRenderPayloadObjectJoinSummary.render_object_rows -or
    $originalRenderPayloadObjectJoinSummary.render_only_objects -ne '0' -or
    $originalRenderPayloadObjectJoinSummary.payload_only_objects -ne '0' -or
    $originalRenderPayloadObjectJoinSummary.render_primitive_rows -ne $originalRenderChainSummary.primitive_rows -or
    $originalRenderPayloadObjectJoinSummary.payload_validated_stream_rows -ne $originalVertexPayloadSummary.validated_vertex_stream_rows -or
    $originalRenderPayloadObjectJoinSummary.joined_identity_match_rows -ne $originalRenderPayloadObjectJoinSummary.joined_object_rows -or
    $originalRenderPayloadObjectJoinSummary.render_status_valid_rows -ne $originalRenderPayloadObjectJoinSummary.joined_object_rows -or
    $originalRenderPayloadObjectJoinSummary.payload_status_valid_rows -ne $originalRenderPayloadObjectJoinSummary.joined_object_rows -or
    $originalRenderPayloadObjectJoinSummary.structural_counts_valid_rows -ne $originalRenderPayloadObjectJoinSummary.joined_object_rows -or
    $originalRenderPayloadObjectJoinSummary.reversed_key_match_rows -ne '0' -or
    $originalRenderPayloadObjectJoinSummary.shifted_key_match_rows -ne '0' -or
    $originalRenderPayloadObjectJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original render/payload object join has unexpected content: $originalRenderPayloadObjectJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_geometry_payload_join_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-original-render-payload-object-join.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-geometry-payload-join-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-geometry-payload-join-control-summary.tsv')
)
$originalStateGeometryPayloadJoinControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-geometry-payload-join-control-summary.tsv'
$originalStateGeometryPayloadJoinControlSummary = Import-Csv -LiteralPath $originalStateGeometryPayloadJoinControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateGeometryPayloadJoinControlSummary -or
    $originalStateGeometryPayloadJoinControlSummary.status -cne 'verified-independent-original-state-geometry-payload-join-control' -or
    $originalStateGeometryPayloadJoinControlSummary.adjacency_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.join_rows -ne '883' -or
    $originalStateGeometryPayloadJoinControlSummary.joined_object_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.missing_join_objects -ne '0' -or
    $originalStateGeometryPayloadJoinControlSummary.adjacency_input_status_valid_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.join_input_status_valid_rows -ne '883' -or
    $originalStateGeometryPayloadJoinControlSummary.identity_join_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.render_status_valid_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.payload_status_valid_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.structural_counts_valid_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.primitive_count_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.complete_command_count_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.fetch_count_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.five_slot_payload_count_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.distinct_payload_range_count_match_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.payload_overlap_zero_rows -ne '24' -or
    $originalStateGeometryPayloadJoinControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/geometry payload join control has unexpected content: $originalStateGeometryPayloadJoinControlSummaryPath"
}
Invoke-Smoke 'candidate-fetch-association' @(
    'eagl-candidate-fetch-association-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association-summary.tsv')
)
$candidateFetchAssociationSummaryPath = Join-Path $ReportRoot 'eagl-candidate-fetch-association-summary.tsv'
$candidateFetchAssociationSummary = Import-Csv -LiteralPath $candidateFetchAssociationSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $candidateFetchAssociationSummary -or
    $candidateFetchAssociationSummary.status -cne 'verified-bounded-candidate-fetch-record-association' -or
    $candidateFetchAssociationSummary.candidate_count -cne '24' -or
    $candidateFetchAssociationSummary.candidate_assets_loaded -cne '24' -or
    $candidateFetchAssociationSummary.candidate_assets_with_complete_five_slot_layouts -le '0' -or
    $candidateFetchAssociationSummary.primitive_rows -le '0' -or
    $candidateFetchAssociationSummary.complete_five_slot_primitive_rows -le '0' -or
    $candidateFetchAssociationSummary.slot_rows -le '0' -or
    $candidateFetchAssociationSummary.valid_slot_rows -cne $candidateFetchAssociationSummary.slot_rows -or
    $candidateFetchAssociationSummary.rejected_candidates -cne '0' -or
    $candidateFetchAssociationSummary.original_semantics -cne 'not-claimed') {
    throw "Candidate fetch association summary has unexpected content: $candidateFetchAssociationSummaryPath"
}
Invoke-Smoke 'original-candidate-slot-profile' @(
    'eagl-original-candidate-slot-profile-audit',
    (Join-Path $ReportRoot 'eagl-original-vertex-stream-numeric.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'), '--report',
    (Join-Path $ReportRoot 'eagl-original-candidate-slot-profile-control.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-candidate-slot-profile-control-summary.tsv')
)
$originalCandidateSlotProfileSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-slot-profile-control-summary.tsv'
$originalCandidateSlotProfileSummary = Import-Csv -LiteralPath $originalCandidateSlotProfileSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateSlotProfileSummary -or
    $originalCandidateSlotProfileSummary.status -cne 'verified-independent-original-candidate-slot1-category11-profile-control' -or
    $originalCandidateSlotProfileSummary.original_stream_rows -ne '11820' -or
    $originalCandidateSlotProfileSummary.candidate_primitive_rows -ne '218' -or
    $originalCandidateSlotProfileSummary.candidate_complete_five_slot_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.candidate_slot1_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.symbol_match_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.object_match_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.category11_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.finite_float2_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.near_unit_range_float2_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.stream_stride_match_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.byte_length_match_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.element_count_match_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.complete_control_rows -ne '216' -or
    $originalCandidateSlotProfileSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate slot profile control has unexpected content: $originalCandidateSlotProfileSummaryPath"
}
Invoke-Smoke 'original-candidate-raw-stream-identity' @(
    'eagl-original-candidate-raw-stream-identity-audit', $IsoPath,
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'), '--report',
    (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity-summary.tsv')
)
$originalCandidateRawStreamIdentitySummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity-summary.tsv'
$originalCandidateRawStreamIdentitySummary = Import-Csv -LiteralPath $originalCandidateRawStreamIdentitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateRawStreamIdentitySummary -or
    $originalCandidateRawStreamIdentitySummary.status -cne 'verified-independent-original-candidate-raw-stream-identity-control' -or
    $originalCandidateRawStreamIdentitySummary.candidate_object_count -ne '24' -or
    $originalCandidateRawStreamIdentitySummary.candidate_primitive_rows -ne '218' -or
    $originalCandidateRawStreamIdentitySummary.candidate_complete_five_slot_rows -ne '216' -or
    $originalCandidateRawStreamIdentitySummary.candidate_slot_rows -ne '1080' -or
    $originalCandidateRawStreamIdentitySummary.raw_objects_loaded -ne '24' -or
    $originalCandidateRawStreamIdentitySummary.raw_valid_slot_rows -ne '1080' -or
    $originalCandidateRawStreamIdentitySummary.raw_metadata_exact_rows -ne '1080' -or
    $originalCandidateRawStreamIdentitySummary.raw_payload_hash_rows -ne '1080' -or
    $originalCandidateRawStreamIdentitySummary.complete_identity_rows -ne '216' -or
    $originalCandidateRawStreamIdentitySummary.expected_unmaterialized_rows -ne '2' -or
    $originalCandidateRawStreamIdentitySummary.rejected_objects -ne '0' -or
    $originalCandidateRawStreamIdentitySummary.original_semantics -cne 'not-claimed') {
    throw "Original/candidate raw stream identity summary has unexpected content: $originalCandidateRawStreamIdentitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_raw_stream_identity_control.py'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity-control-summary.tsv')
)
$originalCandidateRawStreamIdentityControlSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity-control-summary.tsv'
$originalCandidateRawStreamIdentityControlSummary = Import-Csv -LiteralPath $originalCandidateRawStreamIdentityControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateRawStreamIdentityControlSummary -or
    $originalCandidateRawStreamIdentityControlSummary.status -cne 'verified-independent-original-candidate-raw-stream-identity-control' -or
    $originalCandidateRawStreamIdentityControlSummary.candidate_rows -ne '218' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_rows -ne '218' -or
    $originalCandidateRawStreamIdentityControlSummary.candidate_object_rows -ne '24' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_object_rows -ne '24' -or
    $originalCandidateRawStreamIdentityControlSummary.joined_rows -ne '218' -or
    $originalCandidateRawStreamIdentityControlSummary.candidate_complete_rows -ne '216' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_complete_rows -ne '216' -or
    $originalCandidateRawStreamIdentityControlSummary.expected_unmaterialized_rows -ne '2' -or
    $originalCandidateRawStreamIdentityControlSummary.candidate_slot_rows -ne '1080' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_valid_slot_rows -ne '1080' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_metadata_exact_rows -ne '1080' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_payload_hash_rows -ne '1080' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_hash_format_rows -ne '216' -or
    $originalCandidateRawStreamIdentityControlSummary.raw_hash_distinctness_rows -ne '216' -or
    $originalCandidateRawStreamIdentityControlSummary.missing_candidate_keys -ne '0' -or
    $originalCandidateRawStreamIdentityControlSummary.missing_raw_keys -ne '0' -or
    $originalCandidateRawStreamIdentityControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate raw stream identity control has unexpected content: $originalCandidateRawStreamIdentityControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_slot_category_control.py'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-slot-category-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-slot-category-control-summary.tsv')
)
$originalCandidateSlotCategoryControlSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-slot-category-control-summary.tsv'
$originalCandidateSlotCategoryControlSummary = Import-Csv -LiteralPath $originalCandidateSlotCategoryControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateSlotCategoryControlSummary -or
    $originalCandidateSlotCategoryControlSummary.status -cne 'verified-independent-original-candidate-slot-category-ordinal-control' -or
    $originalCandidateSlotCategoryControlSummary.candidate_rows -ne '218' -or
    $originalCandidateSlotCategoryControlSummary.raw_identity_rows -ne '218' -or
    $originalCandidateSlotCategoryControlSummary.candidate_object_rows -ne '24' -or
    $originalCandidateSlotCategoryControlSummary.raw_identity_object_rows -ne '24' -or
    $originalCandidateSlotCategoryControlSummary.joined_rows -ne '218' -or
    $originalCandidateSlotCategoryControlSummary.candidate_complete_rows -ne '216' -or
    $originalCandidateSlotCategoryControlSummary.raw_identity_complete_rows -ne '216' -or
    $originalCandidateSlotCategoryControlSummary.expected_unmaterialized_rows -ne '2' -or
    $originalCandidateSlotCategoryControlSummary.candidate_slot_rows -ne '1080' -or
    $originalCandidateSlotCategoryControlSummary.symbol_format_rows -ne '1080' -or
    $originalCandidateSlotCategoryControlSummary.category_exact_rows -ne '1080' -or
    $originalCandidateSlotCategoryControlSummary.ordinal_exact_rows -ne '1080' -or
    $originalCandidateSlotCategoryControlSummary.complete_control_rows -ne '218' -or
    $originalCandidateSlotCategoryControlSummary.missing_candidate_keys -ne '0' -or
    $originalCandidateSlotCategoryControlSummary.missing_raw_identity_keys -ne '0' -or
    $originalCandidateSlotCategoryControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent opaque candidate slot category/ordinal control has unexpected content: $originalCandidateSlotCategoryControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_primitive_fetch_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-primitive-fetch-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-primitive-fetch-control-summary.tsv')
)
$originalCandidatePrimitiveFetchControlSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-primitive-fetch-control-summary.tsv'
$originalCandidatePrimitiveFetchControlSummary = Import-Csv -LiteralPath $originalCandidatePrimitiveFetchControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidatePrimitiveFetchControlSummary -or
    $originalCandidatePrimitiveFetchControlSummary.status -cne 'verified-independent-original-candidate-primitive-fetch-control' -or
    $originalCandidatePrimitiveFetchControlSummary.original_rows -ne '2364' -or
    $originalCandidatePrimitiveFetchControlSummary.candidate_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.joined_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.missing_candidate_keys -ne '2146' -or
    $originalCandidatePrimitiveFetchControlSummary.missing_original_keys -ne '0' -or
    $originalCandidatePrimitiveFetchControlSummary.command_count_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.command_bytes_sha256_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.command_stream_complete_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.fetch_slot_count_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.fetch_slot_id_sequence_match_rows -ne '216' -or
    $originalCandidatePrimitiveFetchControlSummary.complete_five_slot_sequence_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.state_assignment_sequence_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.primitive_type_flag_match_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.candidate_slot_records_valid_rows -ne '216' -or
    $originalCandidatePrimitiveFetchControlSummary.candidate_slot_records_unmaterialized_expected_rows -ne '2' -or
    $originalCandidatePrimitiveFetchControlSummary.original_input_valid_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.candidate_input_valid_rows -ne '218' -or
    $originalCandidatePrimitiveFetchControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate primitive fetch control has unexpected content: $originalCandidatePrimitiveFetchControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_uid_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-uid-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-uid-ambiguity-control-summary.tsv')
)
$originalStateUidAmbiguitySummaryPath = Join-Path $ReportRoot 'eagl-original-state-uid-ambiguity-control-summary.tsv'
$originalStateUidAmbiguitySummary = Import-Csv -LiteralPath $originalStateUidAmbiguitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateUidAmbiguitySummary -or
    $originalStateUidAmbiguitySummary.status -cne 'verified-independent-original-state-runtime-uid-ambiguity-control' -or
    $originalStateUidAmbiguitySummary.candidate_object_rows -ne '24' -or
    $originalStateUidAmbiguitySummary.render_scoped_rows -ne '218' -or
    $originalStateUidAmbiguitySummary.joined_object_rows -ne '24' -or
    $originalStateUidAmbiguitySummary.missing_candidate_objects -ne '0' -or
    $originalStateUidAmbiguitySummary.missing_render_objects -ne '0' -or
    $originalStateUidAmbiguitySummary.geometry_rows -ne '38' -or
    $originalStateUidAmbiguitySummary.state_rows -ne '180' -or
    $originalStateUidAmbiguitySummary.geometry_rows_with_valid_uid -ne '38' -or
    $originalStateUidAmbiguitySummary.state_rows_with_valid_uid -ne '180' -or
    $originalStateUidAmbiguitySummary.shared_uid_count -ne '24' -or
    $originalStateUidAmbiguitySummary.geometry_rows_same_uid_state_candidates -ne '38' -or
    $originalStateUidAmbiguitySummary.geometry_rows_zero_same_uid_state_candidates -ne '0' -or
    $originalStateUidAmbiguitySummary.geometry_rows_one_same_uid_state_candidate -ne '0' -or
    $originalStateUidAmbiguitySummary.geometry_rows_multiple_same_uid_state_candidates -ne '38' -or
    $originalStateUidAmbiguitySummary.state_rows_in_shared_uid_domains -ne '180' -or
    $originalStateUidAmbiguitySummary.objects_with_uid_ambiguity -ne '24' -or
    $originalStateUidAmbiguitySummary.unique_geometry_state_binding_proven_rows -ne '0' -or
    $originalStateUidAmbiguitySummary.candidate_input_status_valid_rows -ne '24' -or
    $originalStateUidAmbiguitySummary.render_input_status_valid_rows -ne '24' -or
    $originalStateUidAmbiguitySummary.valid_object_rows -ne '24' -or
    $originalStateUidAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $originalStateUidAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original RUNTIME_ALLOC UID ambiguity control has unexpected content: $originalStateUidAmbiguitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_uid_command_hash_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-uid-command-hash-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-uid-command-hash-ambiguity-control-summary.tsv')
)
$originalStateUidCommandHashSummaryPath = Join-Path $ReportRoot 'eagl-original-state-uid-command-hash-ambiguity-control-summary.tsv'
$originalStateUidCommandHashSummary = Import-Csv -LiteralPath $originalStateUidCommandHashSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateUidCommandHashSummary -or
    $originalStateUidCommandHashSummary.status -cne 'verified-independent-original-state-uid-command-hash-ambiguity-control' -or
    $originalStateUidCommandHashSummary.candidate_object_rows -ne '24' -or
    $originalStateUidCommandHashSummary.render_scoped_rows -ne '218' -or
    $originalStateUidCommandHashSummary.joined_object_rows -ne '24' -or
    $originalStateUidCommandHashSummary.missing_candidate_objects -ne '0' -or
    $originalStateUidCommandHashSummary.missing_render_objects -ne '0' -or
    $originalStateUidCommandHashSummary.geometry_rows -ne '38' -or
    $originalStateUidCommandHashSummary.state_rows -ne '180' -or
    $originalStateUidCommandHashSummary.geometry_rows_with_valid_uid -ne '38' -or
    $originalStateUidCommandHashSummary.state_rows_with_valid_uid -ne '180' -or
    $originalStateUidCommandHashSummary.shared_uid_count -ne '24' -or
    $originalStateUidCommandHashSummary.geometry_rows_same_uid_state_candidates -ne '38' -or
    $originalStateUidCommandHashSummary.geometry_rows_zero_same_uid_state_candidates -ne '0' -or
    $originalStateUidCommandHashSummary.geometry_rows_one_same_uid_state_candidate -ne '0' -or
    $originalStateUidCommandHashSummary.geometry_rows_multiple_same_uid_state_candidates -ne '38' -or
    $originalStateUidCommandHashSummary.geometry_rows_with_uid_and_command_hash_candidates -ne '0' -or
    $originalStateUidCommandHashSummary.geometry_rows_with_zero_uid_and_command_hash_candidates -ne '38' -or
    $originalStateUidCommandHashSummary.geometry_rows_with_one_uid_and_command_hash_candidate -ne '0' -or
    $originalStateUidCommandHashSummary.geometry_rows_with_multiple_uid_and_command_hash_candidates -ne '0' -or
    $originalStateUidCommandHashSummary.state_rows_in_shared_uid_domains -ne '180' -or
    $originalStateUidCommandHashSummary.objects_with_uid_ambiguity -ne '24' -or
    $originalStateUidCommandHashSummary.unique_geometry_state_binding_proven_rows -ne '0' -or
    $originalStateUidCommandHashSummary.unique_geometry_state_binding_with_command_hash_proven_rows -ne '0' -or
    $originalStateUidCommandHashSummary.candidate_input_status_valid_rows -ne '218' -or
    $originalStateUidCommandHashSummary.render_input_status_valid_rows -ne '218' -or
    $originalStateUidCommandHashSummary.valid_object_rows -ne '24' -or
    $originalStateUidCommandHashSummary.semantic_binding -cne 'not-proven' -or
    $originalStateUidCommandHashSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original UID/command hash ambiguity control has unexpected content: $originalStateUidCommandHashSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_geometry_draw_classification_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-geometry-draw-classification-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-geometry-draw-classification-control-summary.tsv')
)
$originalGeometryDrawClassificationSummaryPath = Join-Path $ReportRoot 'eagl-original-geometry-draw-classification-control-summary.tsv'
$originalGeometryDrawClassificationSummary = Import-Csv -LiteralPath $originalGeometryDrawClassificationSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalGeometryDrawClassificationSummary -or
    $originalGeometryDrawClassificationSummary.status -cne 'verified-independent-original-geometry-draw-classification-control' -or
    $originalGeometryDrawClassificationSummary.candidate_object_rows -ne '24' -or
    $originalGeometryDrawClassificationSummary.original_scoped_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.candidate_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.joined_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.missing_original_keys -ne '0' -or
    $originalGeometryDrawClassificationSummary.missing_candidate_keys -ne '0' -or
    $originalGeometryDrawClassificationSummary.joined_object_rows -ne '24' -or
    $originalGeometryDrawClassificationSummary.objects_with_complete_classification -ne '24' -or
    $originalGeometryDrawClassificationSummary.original_input_status_valid_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.candidate_input_status_valid_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.original_geometry_marker_rows -ne '38' -or
    $originalGeometryDrawClassificationSummary.original_state_marker_rows -ne '180' -or
    $originalGeometryDrawClassificationSummary.candidate_geometry_marker_rows -ne '38' -or
    $originalGeometryDrawClassificationSummary.candidate_state_marker_rows -ne '180' -or
    $originalGeometryDrawClassificationSummary.classification_match_rows -ne '218' -or
    $originalGeometryDrawClassificationSummary.state_count_greater_than_geometry_rows -ne '24' -or
    $originalGeometryDrawClassificationSummary.state_to_geometry_unique_binding_proven_rows -ne '0' -or
    $originalGeometryDrawClassificationSummary.semantic_binding -cne 'not-proven' -or
    $originalGeometryDrawClassificationSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original geometry/draw classification control has unexpected content: $originalGeometryDrawClassificationSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_candidate_material_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-original-render-chain.tsv'),
    (Join-Path $ReportRoot 'eagl-state-geometry-order-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-shapename-candidate-asset-control.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-candidate-material-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-candidate-material-ambiguity-control-summary.tsv')
)
$originalStateCandidateMaterialAmbiguitySummaryPath = Join-Path $ReportRoot 'eagl-original-state-candidate-material-ambiguity-control-summary.tsv'
$originalStateCandidateMaterialAmbiguitySummary = Import-Csv -LiteralPath $originalStateCandidateMaterialAmbiguitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalStateCandidateMaterialAmbiguitySummary -or
    $originalStateCandidateMaterialAmbiguitySummary.status -cne 'verified-independent-original-state-candidate-material-ambiguity-control' -or
    $originalStateCandidateMaterialAmbiguitySummary.candidate_object_rows -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.render_scoped_rows -ne '218' -or
    $originalStateCandidateMaterialAmbiguitySummary.order_rows -ne '62' -or
    $originalStateCandidateMaterialAmbiguitySummary.order_state_rows -ne '180' -or
    $originalStateCandidateMaterialAmbiguitySummary.original_geometry_rows -ne '38' -or
    $originalStateCandidateMaterialAmbiguitySummary.original_state_rows -ne '180' -or
    $originalStateCandidateMaterialAmbiguitySummary.nonempty_state_interval_rows -ne '36' -or
    $originalStateCandidateMaterialAmbiguitySummary.intervals_with_two_adjacent_geometry_candidates -ne '12' -or
    $originalStateCandidateMaterialAmbiguitySummary.intervals_with_one_adjacent_geometry_candidate -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.intervals_with_zero_adjacent_geometry_candidates -ne '0' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_rows_joined -ne '180' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_rows_with_shape_token_match -ne '180' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_rows_with_candidate_asset_source -ne '180' -or
    $originalStateCandidateMaterialAmbiguitySummary.candidate_asset_valid_rows -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.object_asset_join_rows -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.objects_with_complete_ambiguity_control -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.original_input_status_valid_rows -ne '218' -or
    $originalStateCandidateMaterialAmbiguitySummary.order_input_status_valid_rows -ne '62' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_asset_input_status_valid_rows -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.candidate_asset_input_status_valid_rows -ne '24' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_to_geometry_unique_binding_proven_rows -ne '0' -or
    $originalStateCandidateMaterialAmbiguitySummary.state_to_material_unique_binding_proven_rows -ne '0' -or
    $originalStateCandidateMaterialAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $originalStateCandidateMaterialAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/candidate material ambiguity control has unexpected content: $originalStateCandidateMaterialAmbiguitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_stream_record_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-stream-record-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-stream-record-control-summary.tsv')
)
$originalCandidateStreamRecordControlSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-stream-record-control-summary.tsv'
$originalCandidateStreamRecordControlSummary = Import-Csv -LiteralPath $originalCandidateStreamRecordControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateStreamRecordControlSummary -or
    $originalCandidateStreamRecordControlSummary.status -cne 'verified-independent-original-candidate-stream-record-control' -or
    $originalCandidateStreamRecordControlSummary.original_rows -ne '11820' -or
    $originalCandidateStreamRecordControlSummary.original_primitive_rows -ne '2364' -or
    $originalCandidateStreamRecordControlSummary.candidate_rows -ne '218' -or
    $originalCandidateStreamRecordControlSummary.joined_primitive_rows -ne '218' -or
    $originalCandidateStreamRecordControlSummary.missing_candidate_keys -ne '2146' -or
    $originalCandidateStreamRecordControlSummary.missing_original_keys -ne '0' -or
    $originalCandidateStreamRecordControlSummary.candidate_complete_primitive_rows -ne '216' -or
    $originalCandidateStreamRecordControlSummary.candidate_incomplete_primitive_rows -ne '2' -or
    $originalCandidateStreamRecordControlSummary.candidate_slot_rows_materialized -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.joined_materialized_slot_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.symbol_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.auxiliary_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.fetch_stride_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.stream_stride_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.byte_length_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.element_count_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.record_exact_rows -ne '1080' -or
    $originalCandidateStreamRecordControlSummary.candidate_slot_records_unmaterialized_expected_rows -ne '2' -or
    $originalCandidateStreamRecordControlSummary.original_input_valid_rows -ne '218' -or
    $originalCandidateStreamRecordControlSummary.candidate_input_valid_rows -ne '218' -or
    $originalCandidateStreamRecordControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate stream-record control has unexpected content: $originalCandidateStreamRecordControlSummaryPath"
}
Invoke-Smoke 'candidate-geometry-stream' @(
    'eagl-candidate-geometry-stream-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-geometry-stream.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-candidate-geometry-stream-summary.tsv')
)
$candidateGeometryStreamSummaryPath = Join-Path $ReportRoot 'eagl-candidate-geometry-stream-summary.tsv'
$candidateGeometryStreamSummary = Import-Csv -LiteralPath $candidateGeometryStreamSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $candidateGeometryStreamSummary -or
    $candidateGeometryStreamSummary.status -cne 'verified-bounded-candidate-geometry-stream-audit' -or
    $candidateGeometryStreamSummary.candidate_count -ne '24' -or
    $candidateGeometryStreamSummary.candidate_assets_loaded -ne '24' -or
    $candidateGeometryStreamSummary.candidate_groups -ne '38' -or
    $candidateGeometryStreamSummary.candidate_groups_with_primitive -ne '38' -or
    $candidateGeometryStreamSummary.candidate_source_slot_zero_rows -ne '38' -or
    $candidateGeometryStreamSummary.candidate_complete_primitive_rows -ne '38' -or
    $candidateGeometryStreamSummary.candidate_finite_and_indexed_rows -ne '38' -or
    $candidateGeometryStreamSummary.rejected_candidates -ne '0' -or
    $candidateGeometryStreamSummary.original_semantics -cne 'not-claimed') {
    throw "Candidate geometry-stream summary has unexpected content: $candidateGeometryStreamSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_geometry_stream_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-geometry-stream.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-geometry-stream-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-geometry-stream-control-summary.tsv')
)
$originalCandidateGeometryStreamControlSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-geometry-stream-control-summary.tsv'
$originalCandidateGeometryStreamControlSummary = Import-Csv -LiteralPath $originalCandidateGeometryStreamControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateGeometryStreamControlSummary -or
    $originalCandidateGeometryStreamControlSummary.status -cne 'verified-independent-original-candidate-geometry-stream-control' -or
    $originalCandidateGeometryStreamControlSummary.original_slot_zero_rows -ne '2364' -or
    $originalCandidateGeometryStreamControlSummary.candidate_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.joined_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.missing_candidate_keys -ne '2326' -or
    $originalCandidateGeometryStreamControlSummary.missing_original_keys -ne '0' -or
    $originalCandidateGeometryStreamControlSummary.candidate_source_slot_zero_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.candidate_complete_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.candidate_finite_and_indexed_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.symbol_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.stream_stride_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.byte_length_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.element_count_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.vertex_count_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.triangle_count_exact_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.original_input_valid_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.candidate_input_valid_rows -ne '38' -or
    $originalCandidateGeometryStreamControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate geometry-stream control has unexpected content: $originalCandidateGeometryStreamControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_geometry_payload_identity_control.py'),
    (Join-Path $ReportRoot 'eagl-original-primitive-stream-topology.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-fetch-association.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-geometry-stream.tsv'),
    (Join-Path $ReportRoot 'eagl-original-candidate-raw-stream-identity.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-geometry-payload-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-geometry-payload-identity-control-summary.tsv')
)
$originalCandidateGeometryPayloadIdentitySummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-geometry-payload-identity-control-summary.tsv'
$originalCandidateGeometryPayloadIdentitySummary = Import-Csv -LiteralPath $originalCandidateGeometryPayloadIdentitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $originalCandidateGeometryPayloadIdentitySummary -or
    $originalCandidateGeometryPayloadIdentitySummary.status -cne 'verified-independent-original-candidate-geometry-payload-identity-control' -or
    $originalCandidateGeometryPayloadIdentitySummary.geometry_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.candidate_geometry_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.joined_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.missing_candidate_geometry_keys -ne '0' -or
    $originalCandidateGeometryPayloadIdentitySummary.missing_candidate_fetch_keys -ne '0' -or
    $originalCandidateGeometryPayloadIdentitySummary.missing_raw_identity_keys -ne '0' -or
    $originalCandidateGeometryPayloadIdentitySummary.original_slot_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.candidate_slot_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.raw_hash_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.symbol_exact_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.metadata_exact_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.payload_hash_exact_rows -ne '190' -or
    $originalCandidateGeometryPayloadIdentitySummary.complete_identity_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.original_input_valid_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.candidate_input_valid_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.geometry_input_valid_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.raw_input_valid_rows -ne '38' -or
    $originalCandidateGeometryPayloadIdentitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original/candidate geometry payload identity control has unexpected content: $originalCandidateGeometryPayloadIdentitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-summary.tsv')
)
$renderIndirectIndexedCfgSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-summary.tsv'
$renderIndirectIndexedCfgSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgSummary -or
    $renderIndirectIndexedCfgSummary.status -cne 'verified-bounded-static-render-indirect-indexed-cfg-slices' -or
    $renderIndirectIndexedCfgSummary.candidate_targets -cne '81' -or
    $renderIndirectIndexedCfgSummary.analyzed_targets -cne '81' -or
    $renderIndirectIndexedCfgSummary.targets_with_reachable_cfg -cne '81' -or
    $renderIndirectIndexedCfgSummary.targets_with_reachable_ret -cne '81' -or
    $renderIndirectIndexedCfgSummary.total_linear_instructions -cne '7295' -or
    $renderIndirectIndexedCfgSummary.total_reachable_instructions -cne '421' -or
    $renderIndirectIndexedCfgSummary.total_basic_blocks -cne '105' -or
    $renderIndirectIndexedCfgSummary.total_conditional_branches -cne '0' -or
    $renderIndirectIndexedCfgSummary.total_unconditional_branches -cne '0' -or
    $renderIndirectIndexedCfgSummary.total_internal_branch_targets -cne '0' -or
    $renderIndirectIndexedCfgSummary.total_out_of_window_branch_targets -cne '0' -or
    $renderIndirectIndexedCfgSummary.total_direct_calls -cne '24' -or
    $renderIndirectIndexedCfgSummary.total_unique_direct_call_targets -cne '24' -or
    $renderIndirectIndexedCfgSummary.distinct_direct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgSummary.total_indirect_control_transfers -cne '0' -or
    $renderIndirectIndexedCfgSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG summary has unexpected content: $renderIndirectIndexedCfgSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_call_target_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-summary.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-summary.tsv')
)
$renderIndirectIndexedCfgCallTargetSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-summary.tsv'
$renderIndirectIndexedCfgCallTargetSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgCallTargetSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgCallTargetSummary -or
    $renderIndirectIndexedCfgCallTargetSummary.status -cne 'verified-bounded-static-render-indirect-indexed-cfg-call-target-slices' -or
    $renderIndirectIndexedCfgCallTargetSummary.cfg_direct_call_occurrences -cne '24' -or
    $renderIndirectIndexedCfgCallTargetSummary.cfg_unique_direct_call_targets_sum -cne '24' -or
    $renderIndirectIndexedCfgCallTargetSummary.distinct_direct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetSummary.mapped_executable_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetSummary.disassembled_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetSummary.ret_terminated_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG call-target summary has unexpected content: $renderIndirectIndexedCfgCallTargetSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_call_target_cfg_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-cfg-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-cfg-summary.tsv')
)
$renderIndirectIndexedCfgCallTargetCfgSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-cfg-summary.tsv'
$renderIndirectIndexedCfgCallTargetCfgSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgCallTargetCfgSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgCallTargetCfgSummary -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.status -cne 'verified-bounded-static-render-indirect-indexed-cfg-call-target-cfg-slices' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.candidate_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.analyzed_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.targets_with_reachable_cfg -cne '1' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.targets_with_reachable_ret -cne '1' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.total_linear_instructions -cne '76' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.total_reachable_instructions -cne '23' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.total_basic_blocks -cne '7' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.total_conditional_branches -cne '2' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.total_direct_calls -cne '2' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.distinct_direct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgCallTargetCfgSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG call-target CFG summary has unexpected content: $renderIndirectIndexedCfgCallTargetCfgSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_nested_call_target_cfg_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-cfg-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-target-cfg-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-target-cfg-summary.tsv')
)
$renderIndirectIndexedCfgNestedCallTargetCfgSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-target-cfg-summary.tsv'
$renderIndirectIndexedCfgNestedCallTargetCfgSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgNestedCallTargetCfgSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgNestedCallTargetCfgSummary -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.status -cne 'verified-bounded-static-render-indirect-indexed-cfg-nested-call-target-cfg-slices' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.parent_target_rows -cne '1' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.parent_direct_call_occurrences -cne '2' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.distinct_nested_targets -cne '1' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.analyzed_targets -cne '1' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.targets_with_reachable_cfg -cne '1' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.targets_with_reachable_ret -cne '1' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_linear_instructions -cne '89' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_reachable_instructions -cne '37' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_basic_blocks -cne '7' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_conditional_branches -cne '3' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_unconditional_branches -cne '2' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_internal_branch_targets -cne '4' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.total_direct_calls -cne '0' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.distinct_nested_direct_call_targets -cne '0' -or
    $renderIndirectIndexedCfgNestedCallTargetCfgSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed nested CFG call-target summary has unexpected content: $renderIndirectIndexedCfgNestedCallTargetCfgSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_nested_call_site_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-target-cfg-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-site-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-site-summary.tsv')
)
$renderIndirectIndexedCfgNestedCallSiteSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-nested-call-site-summary.tsv'
$renderIndirectIndexedCfgNestedCallSiteSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgNestedCallSiteSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgNestedCallSiteSummary -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.status -cne 'verified-bounded-static-render-indirect-indexed-nested-call-sites' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.parent_cfg_target_rows -cne '1' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.nested_call_target_rows -cne '2' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.distinct_nested_call_targets -cne '1' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.nested_call_sites_with_push_edx_before_lea -cne '0' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.nested_call_sites_with_lea_ecx_base_before_call -cne '0' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.nested_call_sites_with_ret_imm_4_after_call -cne '0' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.distinct_nested_lea_displacements -cne '0' -or
    $renderIndirectIndexedCfgNestedCallSiteSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed nested call-site summary has unexpected content: $renderIndirectIndexedCfgNestedCallSiteSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_call_site_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-summary.tsv')
)
$renderIndirectIndexedCfgCallSiteSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-summary.tsv'
$renderIndirectIndexedCfgCallSiteSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgCallSiteSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgCallSiteSummary -or
    $renderIndirectIndexedCfgCallSiteSummary.status -cne 'verified-bounded-static-render-indirect-indexed-call-sites' -or
    $renderIndirectIndexedCfgCallSiteSummary.source_cfg_candidates -cne '81' -or
    $renderIndirectIndexedCfgCallSiteSummary.direct_call_occurrences -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.distinct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgCallSiteSummary.candidate_targets_with_call_sites -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.call_sites_with_push_edx_before_lea -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.call_sites_with_lea_ecx_base_before_call -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.call_sites_with_ret_imm_4_after_call -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.distinct_lea_displacements -cne '24' -or
    $renderIndirectIndexedCfgCallSiteSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG call-site summary has unexpected content: $renderIndirectIndexedCfgCallSiteSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_call_site_join_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-join-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-join-summary.tsv')
)
$renderIndirectIndexedCfgCallSiteJoinSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-call-site-join-summary.tsv'
$renderIndirectIndexedCfgCallSiteJoinSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgCallSiteJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgCallSiteJoinSummary -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.status -cne 'verified-bounded-static-render-indirect-indexed-call-site-join' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.indexed_table_target_rows -cne '81' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.distinct_table_bases -cne '1' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.call_site_rows -cne '24' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.joined_call_site_rows -cne '24' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.distinct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.distinct_table_entry_indices -cne '27' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.table_entry_occurrences_for_joined_call_sites -cne '27' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.duplicate_table_occurrences_over_call_sites -cne '3' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.call_sites_with_complete_local_shape -cne '24' -or
    $renderIndirectIndexedCfgCallSiteJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG call-site join summary has unexpected content: $renderIndirectIndexedCfgCallSiteJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_target_join_audit.py'),
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-call-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-target-audit.tsv'),
    '--image', $IsoPath,
    '--internal-path', 'default.xbe',
    '--xbe-sha256', $renderIndirectIndexedTargetSummary.xbe_sha256,
    '--xbe-size', $renderIndirectIndexedTargetSummary.xbe_size,
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-join-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-join-summary.tsv')
)
$renderIndirectIndexedTargetJoinSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-target-join-summary.tsv'
$renderIndirectIndexedTargetJoinSummary = Import-Csv -LiteralPath $renderIndirectIndexedTargetJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedTargetJoinSummary -or
    $renderIndirectIndexedTargetJoinSummary.status -cne 'verified-structural-negative' -or
    $renderIndirectIndexedTargetJoinSummary.candidate_targets -cne '81' -or
    $renderIndirectIndexedTargetJoinSummary.direct_call_targets -cne '87' -or
    $renderIndirectIndexedTargetJoinSummary.render_window_targets -cne '64' -or
    $renderIndirectIndexedTargetJoinSummary.candidate_targets_in_direct_call_targets -cne '0' -or
    $renderIndirectIndexedTargetJoinSummary.candidate_targets_in_render_window_targets -cne '0' -or
    $renderIndirectIndexedTargetJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed target join summary has unexpected content: $renderIndirectIndexedTargetJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_indexed_cfg_join_audit.py'),
    (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-call-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-call-graph-nodes.tsv'),
    '--image', $IsoPath,
    '--internal-path', 'default.xbe',
    '--xbe-sha256', $renderIndirectIndexedCfgSummary.xbe_sha256,
    '--xbe-size', $renderIndirectIndexedCfgSummary.xbe_size,
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-join-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-join-summary.tsv')
)
$renderIndirectIndexedCfgJoinSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-indexed-cfg-join-summary.tsv'
$renderIndirectIndexedCfgJoinSummary = Import-Csv -LiteralPath $renderIndirectIndexedCfgJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectIndexedCfgJoinSummary -or
    $renderIndirectIndexedCfgJoinSummary.status -cne 'verified-structural-negative' -or
    $renderIndirectIndexedCfgJoinSummary.cfg_direct_call_targets -cne '1' -or
    $renderIndirectIndexedCfgJoinSummary.render_direct_call_targets -cne '87' -or
    $renderIndirectIndexedCfgJoinSummary.render_window_targets -cne '64' -or
    $renderIndirectIndexedCfgJoinSummary.render_call_graph_nodes -cne '235' -or
    $renderIndirectIndexedCfgJoinSummary.cfg_targets_in_render_direct_call_targets -cne '0' -or
    $renderIndirectIndexedCfgJoinSummary.cfg_targets_in_render_window_targets -cne '0' -or
    $renderIndirectIndexedCfgJoinSummary.cfg_targets_in_render_call_graph_nodes -cne '0' -or
    $renderIndirectIndexedCfgJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-indexed CFG join summary has unexpected content: $renderIndirectIndexedCfgJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_reference_slice_audit.py'),
    $IsoPath,
    (Join-Path $ReportRoot 'xbe-render-indirect-reference-audit.tsv'),
    '--max-bytes', '256',
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-reference-slice-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-reference-slice-summary.tsv')
)
$renderIndirectReferenceSliceSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-reference-slice-summary.tsv'
$renderIndirectReferenceSliceSummary = Import-Csv -LiteralPath $renderIndirectReferenceSliceSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectReferenceSliceSummary -or
    $renderIndirectReferenceSliceSummary.status -cne 'verified-static-render-indirect-reference-word-slices' -or
    $renderIndirectReferenceSliceSummary.reference_rows -cne '8' -or
    $renderIndirectReferenceSliceSummary.distinct_raw_words -cne '3' -or
    $renderIndirectReferenceSliceSummary.executable_raw_words -cne '3' -or
    $renderIndirectReferenceSliceSummary.disassembled_candidate_words -cne '3' -or
    $renderIndirectReferenceSliceSummary.ret_terminated_candidate_words -cne '3' -or
    $renderIndirectReferenceSliceSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-reference word-slice summary has unexpected content: $renderIndirectReferenceSliceSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/xbe_render_indirect_join_audit.py'),
    (Join-Path $ReportRoot 'xbe-render-target-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-indirect-transfer-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-indirect-reference-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-render-indirect-reference-slice-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'xbe-render-indirect-window-join-audit.tsv'),
    '--summary', (Join-Path $ReportRoot 'xbe-render-indirect-window-join-summary.tsv')
)
$renderIndirectWindowJoinSummaryPath = Join-Path $ReportRoot 'xbe-render-indirect-window-join-summary.tsv'
$renderIndirectWindowJoinSummary = Import-Csv -LiteralPath $renderIndirectWindowJoinSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $renderIndirectWindowJoinSummary -or
    $renderIndirectWindowJoinSummary.status -cne 'verified-static-render-indirect-window-join' -or
    $renderIndirectWindowJoinSummary.target_windows -cne '64' -or
    $renderIndirectWindowJoinSummary.targets_with_indirect_transfers -cne '15' -or
    $renderIndirectWindowJoinSummary.total_indirect_transfers -cne '22' -or
    $renderIndirectWindowJoinSummary.indirect_calls -cne '21' -or
    $renderIndirectWindowJoinSummary.indirect_jumps -cne '1' -or
    $renderIndirectWindowJoinSummary.targets_with_references -cne '4' -or
    $renderIndirectWindowJoinSummary.reference_rows -cne '8' -or
    $renderIndirectWindowJoinSummary.targets_with_transfer_reference_instruction_overlap -cne '4' -or
    $renderIndirectWindowJoinSummary.exact_transfer_reference_instruction_rows -cne '8' -or
    $renderIndirectWindowJoinSummary.distinct_raw_words -cne '3' -or
    $renderIndirectWindowJoinSummary.executable_raw_words -cne '3' -or
    $renderIndirectWindowJoinSummary.disassembled_candidate_words -cne '3' -or
    $renderIndirectWindowJoinSummary.ret_terminated_candidate_words -cne '3' -or
    $renderIndirectWindowJoinSummary.prologue_like_candidate_words -cne '0' -or
    $renderIndirectWindowJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Static render indirect-window join summary has unexpected content: $renderIndirectWindowJoinSummaryPath"
}

$p = $ReportRoot

Invoke-Scenario 'full-stack' @(
    '140', '--demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-full-stack-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-full-stack-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-full-stack-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-full-stack-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-full-stack-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-full-stack-integration.tsv"
) @(
    '140', '--demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-full-stack-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-full-stack-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-full-stack-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-full-stack-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-full-stack-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-full-stack-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-full-stack-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-full-stack-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-full-stack-replayed-integration.tsv"
)

Invoke-Scenario 'candidate-full-stack' @(
    '140', '--demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--candidate-home-character', '--candidate-away-character', '--candidate-material',
    '--frame-output', "$p/gameplay-full-stack-frame.ppm",
    '--match-trace-output', "$p/gameplay-full-stack-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-full-stack-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-full-stack-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-full-stack-audio-trace.tsv",
    '--integration-report', "$p/gameplay-full-stack-integration.tsv"
) @(
    '140', '--demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--candidate-home-character', '--candidate-away-character', '--candidate-material',
    '--match-trace-expected', "$p/gameplay-full-stack-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-full-stack-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-full-stack-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-full-stack-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-full-stack-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-full-stack-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-full-stack-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-full-stack-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-full-stack-replayed-integration.tsv"
)
Assert-CandidateIntegrationReport "$p/gameplay-full-stack-integration.tsv"
Assert-CandidateIntegrationReport "$p/gameplay-full-stack-replayed-integration.tsv"
if ((Get-FileHash -Algorithm SHA256 -LiteralPath "$p/gameplay-full-stack-integration.tsv").Hash -ne
     (Get-FileHash -Algorithm SHA256 -LiteralPath "$p/gameplay-full-stack-replayed-integration.tsv").Hash) {
    throw "Candidate full-stack integration reports differ."
}
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'run-windowed', $IsoPath, '--clean-room-characters',
    '--candidate-home-character', '--candidate-away-character', '--candidate-material',
    '--run-seconds', '2', '--capture-frame', (Join-Path $ReportRoot 'windowed-runtime-capture.ppm')
)
Assert-WindowedFrameCapture (Join-Path $ReportRoot 'windowed-runtime-capture.ppm')
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'eagl-candidate-asset-provenance-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit-summary.tsv')
)
$candidateSourceChainSummary = Get-Content -LiteralPath (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit-summary.tsv') -Raw
foreach ($requiredMarker in @(
    ('candidate_count' + [char]9 + '24'),
    ('source_chain_verified' + [char]9 + '24'),
    ('decoded_pixel_matches' + [char]9 + '24'),
    ('candidate_structural_verified' + [char]9 + '24'),
    ('model_object_entries_read' + [char]9 + '24'),
    ('model_companion_entries_read' + [char]9 + '24'),
    ('candidate_bounds_valid' + [char]9 + '24'),
    ('candidate_geometry_finite_and_indexed' + [char]9 + '24'),
    ('candidate_primitive_shape_state_rows' + [char]9 + '24'),
    ('candidate_geometry_streams_complete' + [char]9 + '24'),
    ('candidate_shape_state_streams_complete' + [char]9 + '24'),
    ('status' + [char]9 + 'verified-candidate-source-chain'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($candidateSourceChainSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "Candidate source-chain summary missing '$requiredMarker'."
    }
}
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'candidate-integrated-runtime-audit', $Workspace, '--report',
    (Join-Path $ReportRoot 'candidate-integrated-runtime-audit.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'shpx-image-entry-audit', $IsoPath, 'data/aptanims/player1.xsh', '--report',
    (Join-Path $ReportRoot 'shpx-image-entry-audit.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'shpx-export-crosscheck', $IsoPath, 'data/aptanims/player1.xsh',
    (Join-Path $Workspace 'exports_fifa_layer3/player1'), '--report',
    (Join-Path $ReportRoot 'shpx-export-crosscheck.tsv'), '--summary',
    (Join-Path $ReportRoot 'shpx-export-crosscheck-summary.tsv')
)
$shpxExportCrosscheckSummary = Get-Content -LiteralPath (Join-Path $ReportRoot 'shpx-export-crosscheck-summary.tsv') -Raw
foreach ($requiredMarker in @(
    ('entry_count' + [char]9 + '46'),
    ('export_files_present' + [char]9 + '46'),
    ('tga_header_valid' + [char]9 + '46'),
    ('pixel_matches' + [char]9 + '46'),
    ('status' + [char]9 + 'verified-export-pixel-match'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($shpxExportCrosscheckSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "SHPX export cross-check summary missing '$requiredMarker'."
    }
}

Invoke-Scenario 'aimed' @(
    '80', '--aimed-demo', '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-aimed-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-aimed-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-aimed-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-aimed-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-aimed-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-aimed-integration.tsv"
) @(
    '80', '--aimed-demo', '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-aimed-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-aimed-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-aimed-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-aimed-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-aimed-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-aimed-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-aimed-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-aimed-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-aimed-replayed-integration.tsv"
)

Invoke-Scenario 'round' @(
    '140', '--round-demo', '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-round-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-round-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-round-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-round-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-round-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-round-integration.tsv"
) @(
    '140', '--round-demo', '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-round-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-round-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-round-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-round-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-round-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-round-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-round-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-round-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-round-replayed-integration.tsv"
)

Invoke-Scenario 'gamebreaker' @(
    '40', '--input-script', "$p/gameplay-clean-room-gamebreaker-input.tsv",
    '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-gamebreaker-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-gamebreaker-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-gamebreaker-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-gamebreaker-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-gamebreaker-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-gamebreaker-integration.tsv"
) @(
    '40', '--input-script', "$p/gameplay-clean-room-gamebreaker-input.tsv",
    '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-gamebreaker-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-gamebreaker-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-gamebreaker-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-gamebreaker-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-gamebreaker-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-gamebreaker-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-gamebreaker-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-gamebreaker-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-gamebreaker-replayed-integration.tsv"
)

Invoke-Scenario 'juggle' @(
    '56', '--input-script', "$p/gameplay-clean-room-juggle-input.tsv",
    '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-juggle-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-juggle-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-juggle-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-juggle-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-juggle-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-juggle-integration.tsv"
) @(
    '56', '--input-script', "$p/gameplay-clean-room-juggle-input.tsv",
    '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-juggle-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-juggle-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-juggle-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-juggle-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-juggle-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-juggle-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-juggle-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-juggle-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-juggle-replayed-integration.tsv"
)

Invoke-Scenario 'panna' @(
    '31', '--input-script', "$p/gameplay-clean-room-panna-input.tsv",
    '--play-audio', '--clean-room-characters', '--match-rule', 'goals-or-panna',
    '--target-goals', '9', '--target-pannas', '1',
    '--frame-output', "$p/gameplay-authored-panna-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-panna-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-panna-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-panna-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-panna-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-panna-integration.tsv"
) @(
    '31', '--input-script', "$p/gameplay-clean-room-panna-input.tsv",
    '--play-audio', '--clean-room-characters', '--match-rule', 'goals-or-panna',
    '--target-goals', '9', '--target-pannas', '1',
    '--match-trace-expected', "$p/gameplay-authored-panna-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-panna-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-panna-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-panna-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-panna-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-panna-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-panna-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-panna-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-panna-replayed-integration.tsv"
)

Invoke-Scenario 'full-time' @(
    '30', '--demo', '--match-seconds', '0.25', '--play-audio',
    '--clean-room-characters', '--frame-output', "$p/gameplay-authored-full-time-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-full-time-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-full-time-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-full-time-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-full-time-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-full-time-integration.tsv"
) @(
    '30', '--demo', '--match-seconds', '0.25', '--play-audio',
    '--clean-room-characters', '--match-trace-expected', "$p/gameplay-authored-full-time-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-full-time-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-full-time-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-full-time-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-full-time-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-full-time-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-full-time-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-full-time-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-full-time-replayed-integration.tsv"
)

Invoke-Scenario 'steal' @(
    '80', '--steal-demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-steal-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-steal-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-steal-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-steal-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-steal-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-steal-integration.tsv"
) @(
    '80', '--steal-demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-steal-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-steal-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-steal-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-steal-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-steal-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-steal-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-steal-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-steal-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-steal-replayed-integration.tsv"
)

Invoke-Scenario 'tackle' @(
    '100', '--tackle-demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--frame-output', "$p/gameplay-authored-tackle-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-tackle-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-tackle-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-tackle-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-tackle-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-tackle-integration.tsv"
) @(
    '100', '--tackle-demo', '--opponent-ai', '--play-audio', '--clean-room-characters',
    '--match-trace-expected', "$p/gameplay-authored-tackle-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-tackle-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-tackle-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-tackle-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-tackle-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-tackle-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-tackle-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-tackle-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-tackle-replayed-integration.tsv"
)

Invoke-Scenario 'first-to-goals' @(
    '80', '--input-script', "$p/gameplay-clean-room-demo-input.tsv", '--play-audio',
    '--clean-room-characters', '--match-rule', 'first-to-goals', '--target-goals', '1',
    '--frame-output', "$p/gameplay-authored-first-to-goals-frame.ppm",
    '--match-trace-output', "$p/gameplay-authored-first-to-goals-match-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-first-to-goals-input-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-first-to-goals-animation-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-first-to-goals-audio-trace.tsv",
    '--integration-report', "$p/gameplay-authored-first-to-goals-integration.tsv"
) @(
    '80', '--input-script', "$p/gameplay-clean-room-demo-input.tsv", '--play-audio',
    '--clean-room-characters', '--match-rule', 'first-to-goals', '--target-goals', '1',
    '--match-trace-expected', "$p/gameplay-authored-first-to-goals-match-trace.tsv",
    '--match-trace-output', "$p/gameplay-authored-first-to-goals-match-replayed.tsv",
    '--input-trace-expected', "$p/gameplay-authored-first-to-goals-input-trace.tsv",
    '--input-trace-output', "$p/gameplay-authored-first-to-goals-input-replayed.tsv",
    '--animation-trace-expected', "$p/gameplay-authored-first-to-goals-animation-trace.tsv",
    '--animation-trace-output', "$p/gameplay-authored-first-to-goals-animation-replayed.tsv",
    '--audio-trace-expected', "$p/gameplay-authored-first-to-goals-audio-trace.tsv",
    '--audio-trace-output', "$p/gameplay-authored-first-to-goals-audio-replayed.tsv",
    '--integration-report', "$p/gameplay-authored-first-to-goals-replayed-integration.tsv"
)

Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'clean-room-integrated-coverage-audit', $Workspace, '--report',
    (Join-Path $ReportRoot 'clean-room-integrated-coverage.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'gameplay-coverage-audit', $Workspace, '--report',
    (Join-Path $ReportRoot 'gameplay-coverage.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'gameplay-audio-coverage-audit', $Workspace, '--report',
    (Join-Path $ReportRoot 'gameplay-audio-coverage.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'clean-room-gameplay-invariant-audit', $Workspace, '--report',
    (Join-Path $ReportRoot 'clean-room-gameplay-invariants.tsv')
)
Invoke-Smoke 'animation-bank-catalog-tail-range' @(
    'animation-bank-catalog-tail-range-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-audit-summary.tsv')
)
$animationTailRangeSummaryPath = Join-Path $ReportRoot 'animation-bank-catalog-tail-range-audit-summary.tsv'
$animationTailRangeSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationTailRangeSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationTailRangeSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationTailRangeSummary.Count -eq 0 -or
    $animationTailRangeSummary['catalog_banks'] -ne '19' -or
    $animationTailRangeSummary['validated_banks'] -ne '19' -or
    $animationTailRangeSummary['catalog_rows'] -ne '1584' -or
    $animationTailRangeSummary['tail_header_shape_valid_rows'] -ne '19' -or
    $animationTailRangeSummary['tail_entry_count_match_rows'] -ne '19' -or
    $animationTailRangeSummary['tail_range_within_data_rows'] -ne '19' -or
    $animationTailRangeSummary['tail_range_aligned_rows'] -ne '19' -or
    $animationTailRangeSummary['tail_range_length_match_rows'] -ne '19' -or
    $animationTailRangeSummary['tail_range_relocation_count_match_rows'] -ne '19' -or
    $animationTailRangeSummary['verified_structural_rows'] -ne '19' -or
    $animationTailRangeSummary['status'] -ne 'verified-structural-animation-bank-tail-range' -or
    $animationTailRangeSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationTailRangeSummary['original_semantics'] -ne 'not-claimed') {
    throw "Animation bank catalog tail-range audit has unexpected content: $animationTailRangeSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/animation_bank_catalog_tail_range_control.py'),
    (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-audit.tsv'),
    (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-audit-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-bank-catalog-tail-range-control-summary.tsv')
)
$animationTailRangeControlSummaryPath = Join-Path $ReportRoot 'animation-bank-catalog-tail-range-control-summary.tsv'
$animationTailRangeControlSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationTailRangeControlSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationTailRangeControlSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationTailRangeControlSummary.Count -eq 0 -or
    $animationTailRangeControlSummary['status'] -ne 'verified-independent-animation-bank-catalog-tail-range-control' -or
    $animationTailRangeControlSummary['input_rows'] -ne '19' -or
    $animationTailRangeControlSummary['valid_input_rows'] -ne '19' -or
    $animationTailRangeControlSummary['catalog_rows'] -ne '1584' -or
    $animationTailRangeControlSummary['duplicate_catalog_entries'] -ne '0' -or
    $animationTailRangeControlSummary['duplicate_bank_names'] -ne '0' -or
    $animationTailRangeControlSummary['header_shape_valid_rows'] -ne '19' -or
    $animationTailRangeControlSummary['entry_count_match_rows'] -ne '19' -or
    $animationTailRangeControlSummary['range_within_data_rows'] -ne '19' -or
    $animationTailRangeControlSummary['range_aligned_rows'] -ne '19' -or
    $animationTailRangeControlSummary['range_length_match_rows'] -ne '19' -or
    $animationTailRangeControlSummary['range_relocation_count_match_rows'] -ne '19' -or
    $animationTailRangeControlSummary['no_relocation_rows'] -ne '0' -or
    $animationTailRangeControlSummary['invalid_geometry_rows'] -ne '0' -or
    $animationTailRangeControlSummary['verified_structural_rows'] -ne '19' -or
    $animationTailRangeControlSummary['source_summary_matches_recomputed'] -ne 'True' -or
    $animationTailRangeControlSummary['semantic_binding'] -ne 'not-proven' -or
    $animationTailRangeControlSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationTailRangeControlSummary['original_semantics'] -ne 'not-claimed') {
    throw "Independent animation bank catalog tail-range control has unexpected content: $animationTailRangeControlSummaryPath"
}
Invoke-Smoke 'animation-bank-tail-pair-relocation' @(
    'animation-bank-tail-pair-relocation-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-audit-summary.tsv'), '--detail-report',
    (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-detail.tsv')
)
$animationPairSummaryPath = Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-audit-summary.tsv'
$animationPairSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationPairSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationPairSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationPairSummary.Count -eq 0 -or
    $animationPairSummary['catalog_banks'] -ne '19' -or
    $animationPairSummary['validated_banks'] -ne '19' -or
    $animationPairSummary['catalog_rows'] -ne '1584' -or
    $animationPairSummary['pair_ordinal_rows'] -ne '1584' -or
    $animationPairSummary['pair_rows_with_two_relocations'] -ne '1584' -or
    $animationPairSummary['pair_rows_with_expected_offsets'] -ne '1584' -or
    $animationPairSummary['rows_with_unexpected_relocation_multiplicity'] -ne '0' -or
    $animationPairSummary['first_raw_words_within_data'] -ne '1584' -or
    $animationPairSummary['second_raw_words_within_data'] -ne '1584' -or
    $animationPairSummary['first_raw_words_at_known_symbols'] -ne '0' -or
    $animationPairSummary['second_raw_words_at_known_symbols'] -ne '0' -or
    $animationPairSummary['distinct_raw_word_pairs'] -ne '1584' -or
    $animationPairSummary['banks_with_complete_pair_layout'] -ne '19' -or
    $animationPairSummary['status'] -ne 'verified-structural-animation-bank-tail-pair-relocations' -or
    $animationPairSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationPairSummary['original_semantics'] -ne 'not-claimed') {
    throw "Animation bank tail pair-relocation audit has unexpected content: $animationPairSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/animation_bank_tail_pair_relocation_control.py'),
    (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-audit.tsv'),
    (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-audit-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-control-summary.tsv')
)
$animationPairControlSummaryPath = Join-Path $ReportRoot 'animation-bank-tail-pair-relocation-control-summary.tsv'
$animationPairControlSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationPairControlSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationPairControlSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationPairControlSummary.Count -eq 0 -or
    $animationPairControlSummary['status'] -ne 'verified-independent-animation-bank-tail-pair-relocation-control' -or
    $animationPairControlSummary['input_rows'] -ne '19' -or
    $animationPairControlSummary['valid_input_rows'] -ne '19' -or
    $animationPairControlSummary['catalog_rows'] -ne '1584' -or
    $animationPairControlSummary['pair_ordinal_rows'] -ne '1584' -or
    $animationPairControlSummary['pair_rows_with_two_relocations'] -ne '1584' -or
    $animationPairControlSummary['pair_rows_with_expected_offsets'] -ne '1584' -or
    $animationPairControlSummary['rows_with_unexpected_relocation_multiplicity'] -ne '0' -or
    $animationPairControlSummary['first_raw_words_within_data'] -ne '1584' -or
    $animationPairControlSummary['second_raw_words_within_data'] -ne '1584' -or
    $animationPairControlSummary['first_raw_words_at_known_symbols'] -ne '0' -or
    $animationPairControlSummary['second_raw_words_at_known_symbols'] -ne '0' -or
    $animationPairControlSummary['distinct_raw_word_pairs'] -ne '1584' -or
    $animationPairControlSummary['banks_with_complete_pair_layout'] -ne '19' -or
    $animationPairControlSummary['source_summary_matches_recomputed'] -ne 'True' -or
    $animationPairControlSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationPairControlSummary['original_semantics'] -ne 'not-claimed') {
    throw "Independent animation bank tail pair-relocation control has unexpected content: $animationPairControlSummaryPath"
}
Write-Host '[smoke] animation-bank-tail-pair-target-chain'
$animationPairTargetChainReportPath = Join-Path $ReportRoot 'animation-bank-tail-target-chain-audit.tsv'
$animationPairTargetChainSummaryPath = Join-Path $ReportRoot 'animation-bank-tail-target-chain-audit-summary.tsv'
$animationPairTargetChainDetailPath = Join-Path $ReportRoot 'animation-bank-tail-target-chain-detail.tsv'
if (-not (Test-Path -LiteralPath $animationPairTargetChainReportPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $animationPairTargetChainSummaryPath -PathType Leaf) -or
    -not (Test-Path -LiteralPath $animationPairTargetChainDetailPath -PathType Leaf)) {
    throw "Animation bank tail target-chain reports were not generated."
}
$animationPairTargetChainSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationPairTargetChainSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationPairTargetChainSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationPairTargetChainSummary.Count -eq 0 -or
    $animationPairTargetChainSummary['catalog_banks'] -ne '19' -or
    $animationPairTargetChainSummary['validated_banks'] -ne '19' -or
    $animationPairTargetChainSummary['catalog_rows'] -ne '1584' -or
    $animationPairTargetChainSummary['pair_rows'] -ne '1584' -or
    $animationPairTargetChainSummary['first_target_rows_with_one_relocation'] -ne '0' -or
    $animationPairTargetChainSummary['second_target_rows_with_one_relocation'] -ne '0' -or
    $animationPairTargetChainSummary['first_second_level_words_within_data'] -ne '0' -or
    $animationPairTargetChainSummary['second_second_level_words_within_data'] -ne '0' -or
    $animationPairTargetChainSummary['first_second_level_words_at_known_symbols'] -ne '0' -or
    $animationPairTargetChainSummary['second_second_level_words_at_known_symbols'] -ne '0' -or
    $animationPairTargetChainSummary['rows_with_complete_target_chain'] -ne '1584' -or
    $animationPairTargetChainSummary['banks_with_complete_target_chain'] -ne '19' -or
    $animationPairTargetChainSummary['status'] -ne 'verified-structural-animation-bank-tail-pair-target-chain' -or
    $animationPairTargetChainSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationPairTargetChainSummary['original_semantics'] -ne 'not-claimed') {
    throw "Animation bank tail target-chain audit has unexpected content: $animationPairTargetChainSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/animation_bank_tail_pair_target_chain_control.py'),
    $animationPairTargetChainReportPath,
    $animationPairTargetChainSummaryPath,
    '--report', (Join-Path $ReportRoot 'animation-bank-tail-target-chain-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'animation-bank-tail-target-chain-control-summary.tsv')
)
$animationPairTargetChainControlSummaryPath = Join-Path $ReportRoot 'animation-bank-tail-target-chain-control-summary.tsv'
$animationPairTargetChainControlSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $animationPairTargetChainControlSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $animationPairTargetChainControlSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($animationPairTargetChainControlSummary.Count -eq 0 -or
    $animationPairTargetChainControlSummary['status'] -ne 'verified-independent-animation-bank-tail-pair-target-chain-control' -or
    $animationPairTargetChainControlSummary['input_rows'] -ne '19' -or
    $animationPairTargetChainControlSummary['valid_input_rows'] -ne '19' -or
    $animationPairTargetChainControlSummary['catalog_rows'] -ne '1584' -or
    $animationPairTargetChainControlSummary['pair_rows'] -ne '1584' -or
    $animationPairTargetChainControlSummary['rows_with_complete_target_chain'] -ne '1584' -or
    $animationPairTargetChainControlSummary['source_summary_matches_recomputed'] -ne 'True' -or
    $animationPairTargetChainControlSummary['candidate_record_binding'] -ne 'not-proven' -or
    $animationPairTargetChainControlSummary['original_semantics'] -ne 'not-claimed') {
    throw "Independent animation bank tail target-chain control has unexpected content: $animationPairTargetChainControlSummaryPath"
}
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'animation-bank-catalog-timestamp-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'animation-bank-catalog-timestamp-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'animation-bank-catalog-timestamp-audit-summary.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'external-animation-timing-audit',
    (Join-Path $ReportRoot 'external-animation-timing-observations.tsv'), '--report',
    (Join-Path $ReportRoot 'external-animation-timing-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'external-animation-timing-audit-summary.tsv')
)
Invoke-Smoke 'animation-bank-runtime' @(
    'animation-bank-runtime-smoke', $IsoPath
)
Invoke-Smoke 'eagl-material-batch' @(
    'eagl-material-batch-smoke', $IsoPath
)
Invoke-Smoke 'eagl-material-runtime-batch' @(
    'eagl-material-runtime-batch-smoke', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-material-runtime-batch.tsv')
)
Invoke-Smoke 'candidate-render-influence' @(
    'eagl-candidate-render-influence-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'candidate-render-influence-audit.tsv')
)
Invoke-Smoke 'candidate-geometry' @(
    'eagl-candidate-geometry-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-geometry-audit.tsv')
)
Invoke-Smoke 'candidate-attribute' @(
    'eagl-candidate-attribute-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-attribute-audit.tsv')
)
Invoke-Smoke 'candidate-attribute-raster-influence' @(
    'eagl-candidate-attribute-raster-influence-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'eagl-candidate-attribute-raster-influence-audit.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_candidate_attribute_texture_compatibility_control.py'),
    '--attribute-report', (Join-Path $ReportRoot 'eagl-candidate-attribute-audit.tsv'),
    '--asset-report', (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-candidate-attribute-texture-compatibility-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-candidate-attribute-texture-compatibility-control-summary.tsv')
)
$candidateAttributeTextureSummaryPath = Join-Path $ReportRoot 'eagl-candidate-attribute-texture-compatibility-control-summary.tsv'
$candidateAttributeTextureSummary = Import-Csv -LiteralPath $candidateAttributeTextureSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $candidateAttributeTextureSummary -or
    $candidateAttributeTextureSummary.status -cne 'verified-independent-candidate-attribute-texture-compatibility-control' -or
    $candidateAttributeTextureSummary.attribute_object_rows -ne '24' -or
    $candidateAttributeTextureSummary.asset_object_rows -ne '24' -or
    $candidateAttributeTextureSummary.joined_object_rows -ne '24' -or
    $candidateAttributeTextureSummary.attribute_input_status_valid_rows -ne '24' -or
    $candidateAttributeTextureSummary.asset_input_status_valid_rows -ne '24' -or
    $candidateAttributeTextureSummary.texture_dimensions_valid_rows -ne '24' -or
    $candidateAttributeTextureSummary.texture_pal8_rows -ne '24' -or
    $candidateAttributeTextureSummary.decoded_pixel_match_rows -ne '24' -or
    $candidateAttributeTextureSummary.slot1_range_rows -ne '24' -or
    $candidateAttributeTextureSummary.slot1_finite_range_rows -ne '24' -or
    $candidateAttributeTextureSummary.slot1_normalized_range_rows -ne '24' -or
    $candidateAttributeTextureSummary.slot1_value_count_match_vertex_rows -ne '24' -or
    $candidateAttributeTextureSummary.attribute_texture_compatibility_rows -ne '24' -or
    $candidateAttributeTextureSummary.semantic_binding -cne 'not-proven' -or
    $candidateAttributeTextureSummary.original_semantics -cne 'not-claimed') {
    throw "Candidate attribute/texture compatibility control summary has unexpected content: $candidateAttributeTextureSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_slot1_candidate_uv_ambiguity_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-fetch-adjacency-control.tsv'),
    (Join-Path $ReportRoot 'eagl-original-numeric-slot-role-discrimination-control.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-attribute-texture-compatibility-control.tsv'),
    (Join-Path $ReportRoot 'candidate-render-influence-audit.tsv'),
    (Join-Path $ReportRoot 'eagl-original-state-profile-discrimination-control.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-slot1-candidate-uv-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-slot1-candidate-uv-ambiguity-control-summary.tsv')
)
$originalStateSlot1CandidateUvAmbiguitySummaryPath = Join-Path $ReportRoot 'eagl-original-state-slot1-candidate-uv-ambiguity-control-summary.tsv'
$originalStateSlot1CandidateUvAmbiguitySummary = Import-Csv -LiteralPath $originalStateSlot1CandidateUvAmbiguitySummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $originalStateSlot1CandidateUvAmbiguitySummary -or
    $originalStateSlot1CandidateUvAmbiguitySummary.status -cne 'verified-independent-original-state-slot1-candidate-uv-ambiguity-control' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.state_rows -ne '2364' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.role_rows -ne '11820' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.joined_primitive_groups -ne '2364' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.candidate_joined_primitive_groups -ne '218' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.missing_state_keys -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.missing_role_keys -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.duplicate_state_keys -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.duplicate_role_keys -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_five_slot_groups -ne '216' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.incomplete_five_slot_groups -ne '2' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.shapename_state_groups -ne '180' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_shapename_state_groups -ne '178' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_shapename_slot1_near_unit_groups -ne '178' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_shapename_other_slot_near_unit_groups -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_groups_slot1_near_unit_groups -ne '216' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.complete_groups_other_slot_near_unit_groups -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.candidate_object_rows -ne '24' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.candidate_attribute_texture_valid_rows -ne '24' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.candidate_raster_influence_valid_rows -ne '24' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.unique_state_to_geometry_binding_proven_rows -ne '0' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.valid_object_rows -ne '24' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $originalStateSlot1CandidateUvAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent original state/slot1 candidate UV ambiguity control has unexpected content: $originalStateSlot1CandidateUvAmbiguitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_candidate_attribute_control.py'),
    (Join-Path $ReportRoot 'eagl-original-state-token.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-attribute-audit.tsv'),
    (Join-Path $ReportRoot 'candidate-render-influence-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-candidate-attribute-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-candidate-attribute-control-summary.tsv')
)
$stateCandidateAttributeControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-candidate-attribute-control-summary.tsv'
$stateCandidateAttributeControlSummary = Import-Csv -LiteralPath $stateCandidateAttributeControlSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $stateCandidateAttributeControlSummary -or
    $stateCandidateAttributeControlSummary.status -cne 'verified-independent-original-state-candidate-attribute-control' -or
    $stateCandidateAttributeControlSummary.candidate_object_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.joined_object_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.missing_state_objects -ne '0' -or
    $stateCandidateAttributeControlSummary.missing_attribute_objects -ne '0' -or
    $stateCandidateAttributeControlSummary.missing_influence_objects -ne '0' -or
    $stateCandidateAttributeControlSummary.attribute_complete_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.attribute_finite_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.attribute_near_unit_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.influence_geometry_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.influence_status_rows -ne '24' -or
    $stateCandidateAttributeControlSummary.influence_mesh_shape_state_exact_rows -ne '0' -or
    $stateCandidateAttributeControlSummary.original_semantics -cne 'not-claimed') {
    throw "Original state/candidate attribute control summary has unexpected content: $stateCandidateAttributeControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_shapename_candidate_asset_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-shapename-candidate-asset-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-shapename-candidate-asset-control-summary.tsv')
)
$shapeNameCandidateAssetSummaryPath = Join-Path $ReportRoot 'eagl-original-shapename-candidate-asset-control-summary.tsv'
$shapeNameCandidateAssetSummary = Import-Csv -LiteralPath $shapeNameCandidateAssetSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $shapeNameCandidateAssetSummary -or
    $shapeNameCandidateAssetSummary.status -cne 'verified-independent-original-shapename-candidate-asset-control' -or
    $shapeNameCandidateAssetSummary.state_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.candidate_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.joined_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.missing_candidate_objects -ne '0' -or
    $shapeNameCandidateAssetSummary.missing_state_objects -ne '0' -or
    $shapeNameCandidateAssetSummary.state_input_valid_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.candidate_input_valid_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.object_state_exact_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.shapename_token_match_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.same_trunk_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.source_chain_valid_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.shape_entry_exact_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.decoded_pixel_match_rows -ne '24' -or
    $shapeNameCandidateAssetSummary.original_semantics -cne 'not-claimed') {
    throw "Original SHAPENAME/candidate asset control summary has unexpected content: $shapeNameCandidateAssetSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_state_candidate_primitive_shape_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-asset-provenance-audit.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-state-candidate-primitive-shape-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-state-candidate-primitive-shape-control-summary.tsv')
)
$primitiveShapeControlSummaryPath = Join-Path $ReportRoot 'eagl-original-state-candidate-primitive-shape-control-summary.tsv'
$primitiveShapeControlSummary = Import-Csv -LiteralPath $primitiveShapeControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $primitiveShapeControlSummary -or
    $primitiveShapeControlSummary.status -cne 'verified-independent-original-state-candidate-primitive-shape-control' -or
    $primitiveShapeControlSummary.state_rows -ne '24' -or
    $primitiveShapeControlSummary.candidate_rows -ne '24' -or
    $primitiveShapeControlSummary.joined_rows -ne '24' -or
    $primitiveShapeControlSummary.missing_candidate_objects -ne '0' -or
    $primitiveShapeControlSummary.missing_state_objects -ne '0' -or
    $primitiveShapeControlSummary.state_input_valid_rows -ne '24' -or
    $primitiveShapeControlSummary.candidate_input_valid_rows -ne '24' -or
    $primitiveShapeControlSummary.state_primitive_count_match_rows -ne '24' -or
    $primitiveShapeControlSummary.shapename_count_match_rows -ne '24' -or
    $primitiveShapeControlSummary.candidate_shape_state_streams_complete_rows -ne '24' -or
    $primitiveShapeControlSummary.same_trunk_rows -ne '24' -or
    $primitiveShapeControlSummary.source_chain_valid_rows -ne '24' -or
    $primitiveShapeControlSummary.shape_entry_exact_rows -ne '24' -or
    $primitiveShapeControlSummary.decoded_pixel_match_rows -ne '24' -or
    $primitiveShapeControlSummary.original_semantics -cne 'not-claimed') {
    throw "Original state/candidate primitive shape control summary has unexpected content: $primitiveShapeControlSummaryPath"
}
Invoke-Smoke 'candidate-shape-state-primitive' @(
    'eagl-candidate-shape-state-primitive-audit', $IsoPath,
    '--report', (Join-Path $ReportRoot 'eagl-candidate-shape-state-primitive.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-candidate-shape-state-primitive-summary.tsv')
)
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/eagl_original_candidate_shapename_primitive_index_control.py'),
    (Join-Path $ReportRoot 'eagl-state-geometry-adjacency.tsv'),
    (Join-Path $ReportRoot 'eagl-candidate-shape-state-primitive.tsv'),
    '--report', (Join-Path $ReportRoot 'eagl-original-candidate-shapename-primitive-index-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'eagl-original-candidate-shapename-primitive-index-control-summary.tsv')
)
$candidateShapeStatePrimitiveIndexSummaryPath = Join-Path $ReportRoot 'eagl-original-candidate-shapename-primitive-index-control-summary.tsv'
$candidateShapeStatePrimitiveIndexSummary = Import-Csv -LiteralPath $candidateShapeStatePrimitiveIndexSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $candidateShapeStatePrimitiveIndexSummary -or
    $candidateShapeStatePrimitiveIndexSummary.status -cne 'verified-independent-original-candidate-shapename-primitive-index-control' -or
    $candidateShapeStatePrimitiveIndexSummary.state_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.candidate_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.joined_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.missing_candidate_objects -ne '0' -or
    $candidateShapeStatePrimitiveIndexSummary.missing_state_objects -ne '0' -or
    $candidateShapeStatePrimitiveIndexSummary.sequence_match_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.set_match_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.count_match_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.complete_candidate_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.original_input_valid_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.candidate_input_valid_rows -ne '24' -or
    $candidateShapeStatePrimitiveIndexSummary.original_semantics -cne 'not-claimed') {
    throw "Original/candidate SHAPENAME primitive index control summary has unexpected content: $candidateShapeStatePrimitiveIndexSummaryPath"
}
Invoke-Smoke 'candidate-object' @(
    'eagl-candidate-object-smoke', $IsoPath, 'tops0030.big', 'tops0030.ord', '--commands'
)
Invoke-Smoke 'material' @(
    'eagl-material-smoke', $IsoPath, 'tops0030.big', 'tops'
)
Invoke-Smoke 'morph-sibling-runtime' @(
    'eagl-morph-sibling-runtime-smoke', $IsoPath
)
Invoke-Smoke 'morph-sibling' @(
    'eagl-morph-sibling-smoke', $IsoPath, 'star0001.big'
)
Invoke-Smoke 'audio-named' @(
    'audio-event-named-decode-smoke', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-named-event-decode.tsv')
)
Invoke-Smoke 'audio-named-runtime' @(
    'audio-named-event-runtime-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-named-event-runtime.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-named-event-runtime-summary.tsv')
)
$audioNamedRuntimeSummary = Get-Content -LiteralPath (Join-Path $ReportRoot 'audio-named-event-runtime-summary.tsv') -Raw
foreach ($requiredMarker in @(
    ('candidate_count' + [char]9 + '17'),
    ('candidate_metadata_validated' + [char]9 + '17'),
    ('runtime_prepared' + [char]9 + '17'),
    ('wave_headers_valid' + [char]9 + '17'),
    ('status' + [char]9 + 'verified-runtime-named-payload'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($audioNamedRuntimeSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "Named EVT runtime summary missing '$requiredMarker'."
    }
}
Invoke-Smoke 'audio-event-parallel-range' @(
    'audio-event-parallel-range-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-event-parallel-range.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-event-parallel-range-summary.tsv')
)
$audioParallelRangeSummaryPath = Join-Path $ReportRoot 'audio-event-parallel-range-summary.tsv'
$audioParallelRangeSummary = Get-Content -LiteralPath $audioParallelRangeSummaryPath -Raw
foreach ($requiredMarker in @(
    ('candidate_count' + [char]9 + '17'),
    ('event_string_anchor_valid' + [char]9 + '17'),
    ('header_range_valid' + [char]9 + '17'),
    ('data_range_valid' + [char]9 + '17'),
    ('parallel_row_index_valid' + [char]9 + '17'),
    ('data_schl_prefix' + [char]9 + '17'),
    ('unique_event_names' + [char]9 + '17'),
    ('unique_event_string_offsets' + [char]9 + '17'),
    ('unique_header_rows' + [char]9 + '17'),
    ('header_table_rows' + [char]9 + '283'),
    ('data_table_rows' + [char]9 + '283'),
    ('verified_rows' + [char]9 + '17'),
    ('status' + [char]9 + 'verified-structural-named-parallel-range'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($audioParallelRangeSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "Named EVT parallel-range summary missing '$requiredMarker'."
    }
}
Invoke-Smoke 'audio-raw-identity' @(
    'audio-event-abk-raw-identity-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-event-abk-raw-identity.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-event-abk-raw-identity-summary.tsv')
)
$audioRawIdentitySummaryPath = Join-Path $ReportRoot 'audio-event-abk-raw-identity-summary.tsv'
$audioRawIdentitySummary = Import-Csv -LiteralPath $audioRawIdentitySummaryPath -Delimiter ([char]9) | ForEach-Object { @{ field = $_.field; value = $_.value } }
$audioRawIdentity = @{}
foreach ($entry in $audioRawIdentitySummary) { $audioRawIdentity[$entry.field] = $entry.value }
if ($audioRawIdentity['event_candidates'] -ne '17' -or
    $audioRawIdentity['event_raw_rows'] -ne '17' -or
    $audioRawIdentity['event_distinct_raw_hashes'] -ne '17' -or
    $audioRawIdentity['bank_count'] -ne '16' -or
    $audioRawIdentity['bank_non_dummy_sound_entries'] -ne '270' -or
    [int]$audioRawIdentity['bank_ast_range_rows'] -le 0 -or
    $audioRawIdentity['bank_ast_schl_rows'] -ne $audioRawIdentity['bank_ast_range_rows'] -or
    $audioRawIdentity['full_raw_hash_pairs'] -ne '0' -or
    $audioRawIdentity['event_hashes_with_full_raw_match'] -ne '0' -or
    $audioRawIdentity['rejected_rows'] -ne '0' -or
    $audioRawIdentity['status'] -ne 'verified-bounded-audio-event-abk-raw-identity-negative-control' -or
    $audioRawIdentity['direct_selection_binding'] -ne 'not-proven' -or
    $audioRawIdentity['original_semantics'] -ne 'not-claimed') {
    throw "Raw EVT/ABKC/AST identity audit has unexpected content: $audioRawIdentitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_abk_raw_identity_control.py'),
    (Join-Path $ReportRoot 'audio-event-abk-raw-identity.tsv'),
    (Join-Path $ReportRoot 'audio-event-abk-raw-identity-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-abk-raw-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-abk-raw-identity-control-summary.tsv')
)
$audioRawIdentityControlPath = Join-Path $ReportRoot 'audio-event-abk-raw-identity-control-summary.tsv'
$audioRawIdentityControl = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $audioRawIdentityControlPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $audioRawIdentityControl[$summaryRow.field] = $summaryRow.value
    }
}
if ($audioRawIdentityControl.Count -eq 0 -or
    $audioRawIdentityControl['status'] -cne 'verified-independent-audio-event-abk-raw-identity-control' -or
    $audioRawIdentityControl['event_rows'] -ne '17' -or
    $audioRawIdentityControl['event_valid_rows'] -ne '17' -or
    $audioRawIdentityControl['full_raw_hash_pairs'] -ne '0' -or
    $audioRawIdentityControl['event_hashes_with_full_raw_match'] -ne '0' -or
    $audioRawIdentityControl['rejected_rows'] -ne '0' -or
    $audioRawIdentityControl['original_semantics'] -cne 'not-claimed') {
    throw "Independent raw EVT/ABKC/AST identity control has unexpected content: $audioRawIdentityControlPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_parallel_runtime_join_control.py'),
    (Join-Path $ReportRoot 'audio-event-parallel-range.tsv'),
    (Join-Path $ReportRoot 'audio-named-event-runtime.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control-summary.tsv')
)
$audioParallelRuntimeJoinSummaryPath = Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control-summary.tsv'
$audioParallelRuntimeJoinSummary = Import-Csv -LiteralPath $audioParallelRuntimeJoinSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioParallelRuntimeJoinSummary -or
    $audioParallelRuntimeJoinSummary.status -cne 'verified-independent-audio-event-parallel-runtime-join-control' -or
    $audioParallelRuntimeJoinSummary.parallel_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.runtime_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.joined_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.missing_runtime_names -ne '0' -or
    $audioParallelRuntimeJoinSummary.missing_parallel_names -ne '0' -or
    $audioParallelRuntimeJoinSummary.header_row_match_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.data_offset_match_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.data_length_match_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.candidate_metadata_validated_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.runtime_prepared_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.wave_header_valid_rows -ne '17' -or
    $audioParallelRuntimeJoinSummary.original_semantics -cne 'not-claimed') {
    throw "Independent audio parallel/runtime join control has unexpected content: $audioParallelRuntimeJoinSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_evt_pcp_range_discrimination_control.py'),
    (Join-Path $ReportRoot 'audio-event-parallel-range.tsv'),
    (Join-Path $ReportRoot 'audio-event-index-evidence.tsv'),
    (Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-evt-pcp-range-discrimination-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-evt-pcp-range-discrimination-control-summary.tsv')
)
$audioEventEvtPcpRangeSummaryPath = Join-Path $ReportRoot 'audio-event-evt-pcp-range-discrimination-control-summary.tsv'
$audioEventEvtPcpRangeSummary = Import-Csv -LiteralPath $audioEventEvtPcpRangeSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioEventEvtPcpRangeSummary -or
    $audioEventEvtPcpRangeSummary.status -cne 'verified-independent-audio-event-evt-pcp-range-discrimination-control' -or
    $audioEventEvtPcpRangeSummary.parallel_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.valid_named_range_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.event_string_anchor_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.header_range_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.data_range_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.parallel_row_index_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.data_schl_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.unique_event_name_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.unique_event_string_offset_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.unique_header_row_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.parallel_status_valid_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.index_candidate_names_contains_event_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.index_32bit_abk_zero -cne 'True' -or
    $audioEventEvtPcpRangeSummary.index_32bit_pcpahdr_zero -cne 'True' -or
    $audioEventEvtPcpRangeSummary.index_32bit_pcpadat_zero -cne 'True' -or
    $audioEventEvtPcpRangeSummary.runtime_joined_rows -ne '17' -or
    $audioEventEvtPcpRangeSummary.runtime_missing_names -ne '0' -or
    $audioEventEvtPcpRangeSummary.direct_selection_binding -cne 'not-proven' -or
    $audioEventEvtPcpRangeSummary.original_semantics -cne 'not-claimed') {
    throw "Independent EVT-to-PCP range discrimination control has unexpected content: $audioEventEvtPcpRangeSummaryPath"
}
Invoke-Smoke -Name 'audio-event-audio-asset-hash-universe' -Arguments @(
    'audio-event-audio-asset-hash-universe-audit', $IsoPath,
    '--report', (Join-Path $ReportRoot 'audio-event-audio-asset-hash-universe.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-audio-asset-hash-universe-summary.tsv')
)
$audioEventAudioAssetHashUniverseSummaryPath = Join-Path $ReportRoot 'audio-event-audio-asset-hash-universe-summary.tsv'
$audioEventAudioAssetHashUniverseSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $audioEventAudioAssetHashUniverseSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $audioEventAudioAssetHashUniverseSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($audioEventAudioAssetHashUniverseSummary.Count -eq 0 -or
    $audioEventAudioAssetHashUniverseSummary['event_path'] -cne 'data/audio/eventspa.evt' -or
    $audioEventAudioAssetHashUniverseSummary['status'] -cne 'verified-independent-audio-event-audio-asset-hash-universe-control' -or
    $audioEventAudioAssetHashUniverseSummary['candidate_count'] -ne '17' -or
    $audioEventAudioAssetHashUniverseSummary['scanned_asset_count'] -le 0 -or
    $audioEventAudioAssetHashUniverseSummary['valid_asset_count'] -ne $audioEventAudioAssetHashUniverseSummary['scanned_asset_count'] -or
    $audioEventAudioAssetHashUniverseSummary['scanned_bytes'] -le 0 -or
    $audioEventAudioAssetHashUniverseSummary['excluded_source_rows'] -ne '1' -or
    $audioEventAudioAssetHashUniverseSummary['assets_with_hits'] -ne '0' -or
    $audioEventAudioAssetHashUniverseSummary['asset_hash_hit_pairs'] -ne '0' -or
    $audioEventAudioAssetHashUniverseSummary['aligned_occurrences'] -ne '0' -or
    $audioEventAudioAssetHashUniverseSummary['direct_selection_binding'] -cne 'not-proven' -or
    $audioEventAudioAssetHashUniverseSummary['original_semantics'] -cne 'not-claimed') {
    throw "Complete audio-asset EVT hash-universe control has unexpected content: $audioEventAudioAssetHashUniverseSummaryPath"
}
Invoke-Smoke -Name 'audio-event-data-asset-hash-universe' -Arguments @(
    'audio-event-data-asset-hash-universe-audit', $IsoPath,
    '--report', (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-summary.tsv')
)
$audioEventDataAssetHashUniverseSummaryPath = Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-summary.tsv'
$audioEventDataAssetHashUniverseSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $audioEventDataAssetHashUniverseSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $audioEventDataAssetHashUniverseSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($audioEventDataAssetHashUniverseSummary.Count -eq 0 -or
    $audioEventDataAssetHashUniverseSummary['asset_root'] -cne 'data' -or
    $audioEventDataAssetHashUniverseSummary['event_path'] -cne 'data/audio/eventspa.evt' -or
    $audioEventDataAssetHashUniverseSummary['status'] -cne 'verified-independent-audio-event-data-asset-hash-universe-control' -or
    $audioEventDataAssetHashUniverseSummary['candidate_count'] -ne '17' -or
    [long]$audioEventDataAssetHashUniverseSummary['scanned_asset_count'] -le 60 -or
    $audioEventDataAssetHashUniverseSummary['valid_asset_count'] -ne $audioEventDataAssetHashUniverseSummary['scanned_asset_count'] -or
    [long]$audioEventDataAssetHashUniverseSummary['scanned_bytes'] -le 1737085606 -or
    $audioEventDataAssetHashUniverseSummary['excluded_source_rows'] -ne '1' -or
    $audioEventDataAssetHashUniverseSummary['direct_selection_binding'] -cne 'not-proven' -or
    $audioEventDataAssetHashUniverseSummary['original_semantics'] -cne 'not-claimed') {
    throw "Complete data-asset EVT hash-universe control has unexpected content: $audioEventDataAssetHashUniverseSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_data_asset_hash_universe_control.py'),
    (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe.tsv'),
    (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-control-summary.tsv')
)
$audioEventDataAssetHashUniverseControlSummaryPath = Join-Path $ReportRoot 'audio-event-data-asset-hash-universe-control-summary.tsv'
$audioEventDataAssetHashUniverseControlSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $audioEventDataAssetHashUniverseControlSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $audioEventDataAssetHashUniverseControlSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($audioEventDataAssetHashUniverseControlSummary.Count -eq 0 -or
    $audioEventDataAssetHashUniverseControlSummary['status'] -cne 'verified-independent-audio-event-data-asset-hash-universe-control' -or
    $audioEventDataAssetHashUniverseControlSummary['rows'] -ne '121' -or
    $audioEventDataAssetHashUniverseControlSummary['valid_rows'] -ne '121' -or
    $audioEventDataAssetHashUniverseControlSummary['unique_paths'] -ne '121' -or
    $audioEventDataAssetHashUniverseControlSummary['recomputed_bytes'] -ne '2008984464' -or
    $audioEventDataAssetHashUniverseControlSummary['hit_rows'] -ne '1' -or
    $audioEventDataAssetHashUniverseControlSummary['hit_asset_path'] -cne 'data/movies/advoffn.vp6' -or
    $audioEventDataAssetHashUniverseControlSummary['hit_hash_word'] -cne '0x34214F6B' -or
    $audioEventDataAssetHashUniverseControlSummary['hit_event_name'] -cne '3panna_goal' -or
    $audioEventDataAssetHashUniverseControlSummary['hit_offset'] -cne '0xA58D88' -or
    $audioEventDataAssetHashUniverseControlSummary['direct_selection_binding'] -cne 'not-proven' -or
    $audioEventDataAssetHashUniverseControlSummary['original_semantics'] -cne 'not-claimed') {
    throw "Independent complete data-asset EVT hash-universe reparse has unexpected content: $audioEventDataAssetHashUniverseControlSummaryPath"
}
Invoke-Smoke -Name 'audio-event-name-asset-text-search' -Arguments @(
    'audio-event-name-asset-text-search-audit', $IsoPath,
    '--report', (Join-Path $ReportRoot 'audio-event-name-asset-text-search.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-name-asset-text-search-summary.tsv')
)
$audioEventNameTextSummaryPath = Join-Path $ReportRoot 'audio-event-name-asset-text-search-summary.tsv'
$audioEventNameTextSummary = @{}
foreach ($summaryRow in (Import-Csv -LiteralPath $audioEventNameTextSummaryPath -Delimiter ([char]9))) {
    if ($summaryRow.field) {
        $audioEventNameTextSummary[$summaryRow.field] = $summaryRow.value
    }
}
if ($audioEventNameTextSummary.Count -eq 0 -or
    $audioEventNameTextSummary['event_path'] -cne 'data/audio/eventspa.evt' -or
    $audioEventNameTextSummary['status'] -cne 'verified-independent-audio-event-name-asset-text-search-control' -or
    $audioEventNameTextSummary['candidate_count'] -ne '17' -or
    $audioEventNameTextSummary['scanned_asset_count'] -ne '60' -or
    $audioEventNameTextSummary['valid_asset_count'] -ne '60' -or
    $audioEventNameTextSummary['scanned_bytes'] -ne '1737085606' -or
    $audioEventNameTextSummary['excluded_source_rows'] -ne '1' -or
    $audioEventNameTextSummary['asset_name_hit_pairs'] -ne '0' -or
    $audioEventNameTextSummary['assets_with_name_hits'] -ne '0' -or
    $audioEventNameTextSummary['raw_occurrences'] -ne '0' -or
    $audioEventNameTextSummary['direct_selection_binding'] -cne 'not-proven' -or
    $audioEventNameTextSummary['original_semantics'] -cne 'not-claimed') {
    throw "Complete audio-asset EVT name-text search has unexpected content: $audioEventNameTextSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_name_asset_text_search_control.py'),
    (Join-Path $ReportRoot 'audio-event-name-asset-text-search.tsv'),
    (Join-Path $ReportRoot 'audio-event-name-asset-text-search-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-name-asset-text-search-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-name-asset-text-search-control-summary.tsv')
)
$audioEventNameTextControlSummaryPath = Join-Path $ReportRoot 'audio-event-name-asset-text-search-control-summary.tsv'
$audioEventNameTextControlSummary = Import-Csv -LiteralPath $audioEventNameTextControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioEventNameTextControlSummary -or
    $audioEventNameTextControlSummary.status -cne 'verified-independent-audio-event-name-asset-text-search-control' -or
    $audioEventNameTextControlSummary.input_rows -ne '1020' -or
    $audioEventNameTextControlSummary.distinct_assets -ne '60' -or
    $audioEventNameTextControlSummary.distinct_names -ne '17' -or
    $audioEventNameTextControlSummary.valid_rows -ne '1020' -or
    $audioEventNameTextControlSummary.no_hit_rows -ne '1020' -or
    $audioEventNameTextControlSummary.raw_occurrence_rows -ne '0' -or
    $audioEventNameTextControlSummary.total_raw_occurrences -ne '0' -or
    $audioEventNameTextControlSummary.duplicate_asset_name_keys -ne '0' -or
    $audioEventNameTextControlSummary.source_summary_matches_recomputed -cne 'True' -or
    $audioEventNameTextControlSummary.expected_assets -ne '60' -or
    $audioEventNameTextControlSummary.expected_names -ne '17' -or
    $audioEventNameTextControlSummary.semantic_binding -cne 'not-proven' -or
    $audioEventNameTextControlSummary.direct_selection_binding -cne 'not-proven' -or
    $audioEventNameTextControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent EVT name-text universe reparse has unexpected content: $audioEventNameTextControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_authored_candidate_route_control.py'),
    (Join-Path $ReportRoot 'gameplay-audio-coverage.tsv'),
    (Join-Path $ReportRoot 'audio-named-event-runtime.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-authored-candidate-route-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-authored-candidate-route-control-summary.tsv')
)
$authoredAudioCandidateRouteSummaryPath = Join-Path $ReportRoot 'audio-authored-candidate-route-control-summary.tsv'
$authoredAudioCandidateRouteSummary = Import-Csv -LiteralPath $authoredAudioCandidateRouteSummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $authoredAudioCandidateRouteSummary -or
    $authoredAudioCandidateRouteSummary.status -cne 'verified-independent-authored-audio-candidate-route-control' -or
    $authoredAudioCandidateRouteSummary.scenario_rows -ne '10' -or
    $authoredAudioCandidateRouteSummary.verified_scenario_rows -ne '10' -or
    $authoredAudioCandidateRouteSummary.trace_rows -ne '54' -or
    $authoredAudioCandidateRouteSummary.trace_played_rows -ne '54' -or
    $authoredAudioCandidateRouteSummary.route_rows -ne '15' -or
    $authoredAudioCandidateRouteSummary.unique_action_rows -ne '15' -or
    $authoredAudioCandidateRouteSummary.unique_candidate_rows -ne '8' -or
    $authoredAudioCandidateRouteSummary.runtime_rows -ne '17' -or
    $authoredAudioCandidateRouteSummary.runtime_prepared_route_rows -ne '15' -or
    $authoredAudioCandidateRouteSummary.runtime_wave_header_valid_route_rows -ne '15' -or
    $authoredAudioCandidateRouteSummary.missing_runtime_candidates -ne '0' -or
    $authoredAudioCandidateRouteSummary.coverage_missing_policy_actions -ne '0' -or
    $authoredAudioCandidateRouteSummary.original_semantics -cne 'not-claimed') {
    throw "Authored audio candidate-route control summary has unexpected content: $authoredAudioCandidateRouteSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_authored_named_payload_identity_control.py'),
    (Join-Path $ReportRoot 'gameplay-audio-coverage.tsv'),
    (Join-Path $ReportRoot 'audio-named-event-runtime.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-authored-named-payload-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-authored-named-payload-identity-control-summary.tsv')
)
$authoredNamedPayloadIdentitySummaryPath = Join-Path $ReportRoot 'audio-authored-named-payload-identity-control-summary.tsv'
$authoredNamedPayloadIdentitySummary = Import-Csv -LiteralPath $authoredNamedPayloadIdentitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $authoredNamedPayloadIdentitySummary -or
    $authoredNamedPayloadIdentitySummary.status -cne 'verified-independent-authored-audio-named-payload-identity-control' -or
    $authoredNamedPayloadIdentitySummary.scenario_rows -ne '10' -or
    $authoredNamedPayloadIdentitySummary.verified_scenario_rows -ne '10' -or
    $authoredNamedPayloadIdentitySummary.trace_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.trace_played_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.identity_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.runtime_rows -ne '17' -or
    $authoredNamedPayloadIdentitySummary.runtime_prepared_route_rows -ne '8' -or
    $authoredNamedPayloadIdentitySummary.runtime_wave_header_valid_route_rows -ne '8' -or
    $authoredNamedPayloadIdentitySummary.wave_bytes_match_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.pcm_bytes_match_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.pcm_sha256_match_rows -ne '54' -or
    $authoredNamedPayloadIdentitySummary.invalid_trace_identity_rows -ne '0' -or
    $authoredNamedPayloadIdentitySummary.missing_runtime_candidates -ne '0' -or
    $authoredNamedPayloadIdentitySummary.direct_selection_binding -cne 'not-proven' -or
    $authoredNamedPayloadIdentitySummary.original_semantics -cne 'not-claimed') {
    throw "Authored named-payload identity control summary has unexpected content: $authoredNamedPayloadIdentitySummaryPath"
}
Invoke-Smoke 'audio-event-xbe-hash-search' @(
    'audio-event-xbe-hash-search-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-event-xbe-hash-search.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-event-xbe-hash-search-summary.tsv')
)
$audioXbeHashSummaryPath = Join-Path $ReportRoot 'audio-event-xbe-hash-search-summary.tsv'
$audioXbeHashSummary = Get-Content -LiteralPath $audioXbeHashSummaryPath -Raw
foreach ($requiredMarker in @(
    ('candidate_count' + [char]9 + '17'),
    ('unique_event_names' + [char]9 + '17'),
    ('unique_hash_words' + [char]9 + '17'),
    ('xbe_bytes' + [char]9 + '3293184'),
    ('aligned_occurrences' + [char]9 + '0'),
    ('unaligned_occurrences' + [char]9 + '0'),
    ('hash_words_with_aligned_hits' + [char]9 + '0'),
    ('hash_words_with_unaligned_hits' + [char]9 + '0'),
    ('status' + [char]9 + 'verified-structural-xbe-hash-search'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($audioXbeHashSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "EVT/XBE hash-search summary missing '$requiredMarker'."
    }
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_asset_path_index_control.py'),
    (Join-Path $ReportRoot 'xbe-asset-literal-audit.tsv'),
    (Join-Path $ReportRoot 'audio-event-index-evidence.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-asset-path-index-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-asset-path-index-control-summary.tsv')
)
$audioEventAssetPathSummaryPath = Join-Path $ReportRoot 'audio-event-asset-path-index-control-summary.tsv'
$audioEventAssetPathSummary = Import-Csv -LiteralPath $audioEventAssetPathSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioEventAssetPathSummary -or
    $audioEventAssetPathSummary.event_path -cne 'data/audio/eventspa.evt' -or
    $audioEventAssetPathSummary.candidate_name_count -cne '17' -or
    $audioEventAssetPathSummary.candidate_pairs -cne '1061' -or
    $audioEventAssetPathSummary.xbe_literal_rows -cne '4' -or
    $audioEventAssetPathSummary.xbe_literal_occurrences -cne '9' -or
    $audioEventAssetPathSummary.xbe_literal_xref_count -cne '0' -or
    $audioEventAssetPathSummary.matched_path_rows -cne '4' -or
    $audioEventAssetPathSummary.distinct_matched_paths -cne '1' -or
    $audioEventAssetPathSummary.status -cne 'verified-independent-audio-event-asset-path-index-control' -or
    $audioEventAssetPathSummary.direct_selection_binding -cne 'not-proven' -or
    $audioEventAssetPathSummary.original_semantics -cne 'not-claimed') {
    throw "Independent audio event asset-path/index control has unexpected content: $audioEventAssetPathSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_static_context_global_assignment_control.py'),
    (Join-Path $ReportRoot 'xbe-audio-static-literal-audit.tsv'),
    (Join-Path $ReportRoot 'xbe-audio-static-summary.tsv'),
    (Join-Path $ReportRoot 'audio-event-asset-path-index-control-summary.tsv'),
    (Join-Path $ReportRoot 'audio-event-sample-global-assignment-control-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-static-context-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-static-context-global-assignment-control-summary.tsv')
)
$audioEventStaticContextSummaryPath = Join-Path $ReportRoot 'audio-event-static-context-global-assignment-control-summary.tsv'
$audioEventStaticContextSummary = Import-Csv -LiteralPath $audioEventStaticContextSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioEventStaticContextSummary -or
    $audioEventStaticContextSummary.status -cne 'verified-independent-audio-event-static-context-global-assignment-control' -or
    $audioEventStaticContextSummary.static_audio_xref_rows -cne '58' -or
    $audioEventStaticContextSummary.unique_audio_literals -cne '23' -or
    $audioEventStaticContextSummary.audio_context_rows -cne '147' -or
    $audioEventStaticContextSummary.audio_context_with_executable_calls -cne '108' -or
    $audioEventStaticContextSummary.distinct_executable_call_targets -cne '56' -or
    $audioEventStaticContextSummary.audio_targets_with_multiple_literals -cne '14' -or
    $audioEventStaticContextSummary.nested_audio_literal_matches -cne '1' -or
    $audioEventStaticContextSummary.xdvdfs_audio_literal_matches -cne '10' -or
    $audioEventStaticContextSummary.audio_event_literal_xref_rows -cne '0' -or
    $audioEventStaticContextSummary.event_path_candidate_name_count -cne '17' -or
    $audioEventStaticContextSummary.event_path_candidate_pairs -cne '1061' -or
    $audioEventStaticContextSummary.event_path_literal_rows -cne '4' -or
    $audioEventStaticContextSummary.event_path_literal_occurrences -cne '9' -or
    $audioEventStaticContextSummary.event_path_literal_xref_count -cne '0' -or
    $audioEventStaticContextSummary.event_path_matched_rows -cne '4' -or
    $audioEventStaticContextSummary.event_path_distinct_matched_paths -cne '1' -or
    $audioEventStaticContextSummary.event_rows -cne '17' -or
    $audioEventStaticContextSummary.bank_candidate_pool_size -cne '241' -or
    $audioEventStaticContextSummary.event_exact_pcm_match_rows -cne '0' -or
    $audioEventStaticContextSummary.event_hashes_with_match -cne '0' -or
    $audioEventStaticContextSummary.unique_evt_abkc_sample_binding_proven_rows -cne '0' -or
    $audioEventStaticContextSummary.event_rows_without_static_event_xref -cne '17' -or
    $audioEventStaticContextSummary.static_event_context_target_pool_size -cne '56' -or
    $audioEventStaticContextSummary.event_static_candidate_model_sum_before -cne '952' -or
    $audioEventStaticContextSummary.event_static_candidate_model_sum_after -cne '952' -or
    $audioEventStaticContextSummary.event_static_context_reduced_rows -cne '0' -or
    $audioEventStaticContextSummary.unique_event_static_selector_proven_rows -cne '0' -or
    $audioEventStaticContextSummary.valid_event_rows -cne '17' -or
    $audioEventStaticContextSummary.semantic_binding -cne 'not-proven' -or
    $audioEventStaticContextSummary.direct_selection_binding -cne 'not-proven' -or
    $audioEventStaticContextSummary.original_semantics -cne 'not-claimed') {
    throw "Audio event static-context global-assignment summary has unexpected content: $audioEventStaticContextSummaryPath"
}
$audioEventStaticContextHeader = Get-Content -LiteralPath (Join-Path $ReportRoot 'audio-event-static-context-global-assignment-control.tsv') -TotalCount 1
foreach ($requiredColumn in @('static_audio_context_target_pool_size', 'event_static_context_reduced_rows', 'unique_event_static_selector_proven')) {
    if ($audioEventStaticContextHeader -notmatch [regex]::Escape($requiredColumn)) {
        throw "Audio event static-context control report is missing '$requiredColumn'."
    }
}
Invoke-Smoke 'fifast2-csi' @(
    'fifast2-csi-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'fifast2-csi-name-index.tsv'), '--summary',
    (Join-Path $ReportRoot 'fifast2-csi-name-index-summary.tsv')
)
$fifast2CsiSummaryPath = Join-Path $ReportRoot 'fifast2-csi-name-index-summary.tsv'
$fifast2CsiSummary = Get-Content -LiteralPath $fifast2CsiSummaryPath -Raw
foreach ($requiredMarker in @(
    ('asset_length' + [char]9 + '3024'),
    ('magic' + [char]9 + 'MOIR'),
    ('header_candidate_count' + [char]9 + '100'),
    ('candidate_record_count' + [char]9 + '100'),
    ('valid_record_count' + [char]9 + '100'),
    ('unique_name_count' + [char]9 + '100'),
    ('name_table_offset' + [char]9 + '0x5F4'),
    ('opaque_words_within_asset' + [char]9 + '12'),
    ('reserved_zero_count' + [char]9 + '100'),
    ('opaque_words_non_decreasing_pairs' + [char]9 + '98'),
    ('event_text_name_count' + [char]9 + '17'),
    ('event_exact_name_overlap_count' + [char]9 + '0'),
    ('event_substring_overlap_count' + [char]9 + '22'),
    ('status' + [char]9 + 'verified-structural-fifast2-csi-name-index-candidate'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($fifast2CsiSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "FIFAST2 CSI structural summary missing '$requiredMarker'."
    }
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/fifast2_csi_evt_name_ambiguity_control.py'),
    (Join-Path $ReportRoot 'fifast2-csi-name-index.tsv'),
    (Join-Path $ReportRoot 'fifast2-csi-name-index-summary.tsv'),
    (Join-Path $ReportRoot 'audio-event-parallel-range.tsv'),
    '--report', (Join-Path $ReportRoot 'fifast2-csi-evt-name-ambiguity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'fifast2-csi-evt-name-ambiguity-control-summary.tsv')
)
$fifast2CsiEvtNameAmbiguitySummaryPath = Join-Path $ReportRoot 'fifast2-csi-evt-name-ambiguity-control-summary.tsv'
$fifast2CsiEvtNameAmbiguitySummary = Import-Csv -LiteralPath $fifast2CsiEvtNameAmbiguitySummaryPath -Delimiter "`t" | Select-Object -First 1
if ($null -eq $fifast2CsiEvtNameAmbiguitySummary -or
    $fifast2CsiEvtNameAmbiguitySummary.status -cne 'verified-independent-fifast2-csi-evt-name-ambiguity-control' -or
    $fifast2CsiEvtNameAmbiguitySummary.csi_rows -ne '100' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_rows -ne '17' -or
    $fifast2CsiEvtNameAmbiguitySummary.csi_valid_rows -ne '100' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_valid_rows -ne '17' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_unique_names -ne '17' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_rows_with_zero_csi_name_matches -ne '10' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_rows_with_single_csi_name_match -ne '3' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_rows_with_ambiguous_csi_name_matches -ne '4' -or
    $fifast2CsiEvtNameAmbiguitySummary.evt_rows_with_any_csi_name_match -ne '7' -or
    $fifast2CsiEvtNameAmbiguitySummary.exact_event_name_overlap_rows -ne '0' -or
    $fifast2CsiEvtNameAmbiguitySummary.unique_name_binding_proven_rows -ne '0' -or
    $fifast2CsiEvtNameAmbiguitySummary.semantic_binding -cne 'not-proven' -or
    $fifast2CsiEvtNameAmbiguitySummary.direct_selection_binding -cne 'not-proven' -or
    $fifast2CsiEvtNameAmbiguitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent FIFAST2.csi/EVT name ambiguity control has unexpected content: $fifast2CsiEvtNameAmbiguitySummaryPath"
}
Invoke-Smoke 'fifast2-csi-tuna' @(
    'fifast2-csi-tuna-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'fifast2-csi-tuna-hash-crosscheck.tsv'), '--summary',
    (Join-Path $ReportRoot 'fifast2-csi-tuna-hash-crosscheck-summary.tsv')
)
$fifast2CsiTunaSummaryPath = Join-Path $ReportRoot 'fifast2-csi-tuna-hash-crosscheck-summary.tsv'
$fifast2CsiTunaSummary = Get-Content -LiteralPath $fifast2CsiTunaSummaryPath -Raw
foreach ($requiredMarker in @(
    ('tuna_archive_magic' + [char]9 + 'BIGF'),
    ('tuna_archive_size' + [char]9 + '6313'),
    ('tuna_archive_file_count' + [char]9 + '1'),
    ('tuna_archive_size_matches' + [char]9 + 'True'),
    ('tuna_entry_name' + [char]9 + 'tunavars.txt'),
    ('tuna_row_count' + [char]9 + '106'),
    ('tuna_hash_match_count' + [char]9 + '106'),
    ('csi_record_count' + [char]9 + '100'),
    ('csi_valid_record_count' + [char]9 + '100'),
    ('csi_name_overlap_count' + [char]9 + '89'),
    ('csi_rows_with_tuna_hash_match' + [char]9 + '89'),
    ('csi_opaque_hash_match_count' + [char]9 + '0'),
    ('csi_opaque_words_within_pcpahdr_asset' + [char]9 + '100'),
    ('hash_function_candidate' + [char]9 + 'djb2-like-seed-0x1505-multiply-0x21'),
    ('opaque_word_semantics' + [char]9 + 'not-decoded'),
    ('status' + [char]9 + 'verified-structural-tuna-csi-name-hash-crosscheck'),
    ('provenance' + [char]9 + 'tuna.big/tunavars.txt+FIFAST2.csi+pcpahdr.big;no-execution;no-binding'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($fifast2CsiTunaSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "FIFAST2 CSI/tuna hash cross-check summary missing '$requiredMarker'."
    }
}
Invoke-Smoke 'fifast2-csi-opaque-range' @(
    'fifast2-csi-opaque-range-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'fifast2-csi-opaque-range-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'fifast2-csi-opaque-range-audit-summary.tsv')
)
$fifast2CsiOpaqueRangeSummaryPath = Join-Path $ReportRoot 'fifast2-csi-opaque-range-audit-summary.tsv'
$fifast2CsiOpaqueRangeSummary = Get-Content -LiteralPath $fifast2CsiOpaqueRangeSummaryPath -Raw
foreach ($requiredMarker in @(
    ('csi_record_count' + [char]9 + '100'),
    ('csi_valid_record_count' + [char]9 + '100'),
    ('pcpahdr_row_count' + [char]9 + '283'),
    ('pcpahdr_companion_length' + [char]9 + '35572'),
    ('pcpahdr_table_structure_valid' + [char]9 + 'True'),
    ('pcpahdr_partition_coverage' + [char]9 + 'True'),
    ('pcpadat_row_count' + [char]9 + '283'),
    ('pcpadat_companion_length' + [char]9 + '601688832'),
    ('pcpadat_table_structure_valid' + [char]9 + 'True'),
    ('pcpadat_partition_coverage' + [char]9 + 'True'),
    ('csi_opaque_within_pcpahdr_count' + [char]9 + '100'),
    ('csi_opaque_pcpahdr_exact_start_count' + [char]9 + '0'),
    ('csi_opaque_pcpahdr_exact_end_count' + [char]9 + '0'),
    ('csi_opaque_within_pcpadat_count' + [char]9 + '100'),
    ('csi_opaque_pcpadat_exact_start_count' + [char]9 + '0'),
    ('csi_opaque_pcpadat_exact_end_count' + [char]9 + '0'),
    ('distinct_pcpahdr_containing_rows' + [char]9 + '80'),
    ('distinct_pcpadat_containing_rows' + [char]9 + '1'),
    ('opaque_word_semantics' + [char]9 + 'not-decoded'),
    ('status' + [char]9 + 'verified-structural-csi-opaque-range-boundary-control'),
    ('provenance' + [char]9 + 'FIFAST2.csi+pcpahdr.off/pcpahdr.big+pcpadat.off/pcpadat.big;no-execution;no-binding'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($fifast2CsiOpaqueRangeSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "FIFAST2 CSI opaque range/boundary summary missing '$requiredMarker'."
    }
}
Invoke-Smoke 'fifast2-csi-opaque-field' @(
    'fifast2-csi-opaque-field-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'fifast2-csi-opaque-field-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'fifast2-csi-opaque-field-audit-summary.tsv')
)
$fifast2CsiOpaqueFieldSummaryPath = Join-Path $ReportRoot 'fifast2-csi-opaque-field-audit-summary.tsv'
$fifast2CsiOpaqueFieldSummary = Get-Content -LiteralPath $fifast2CsiOpaqueFieldSummaryPath -Raw
foreach ($requiredMarker in @(
    ('csi_record_count' + [char]9 + '100'),
    ('csi_valid_record_count' + [char]9 + '100'),
    ('pcpahdr_row_count' + [char]9 + '283'),
    ('pcpahdr_table_structure_valid' + [char]9 + 'True'),
    ('pcpahdr_partition_coverage' + [char]9 + 'True'),
    ('pcpadat_row_count' + [char]9 + '283'),
    ('pcpadat_table_structure_valid' + [char]9 + 'True'),
    ('pcpadat_partition_coverage' + [char]9 + 'True'),
    ('pcpahdr_any_column_match_count' + [char]9 + '1'),
    ('pcpahdr_column1_match_count' + [char]9 + '0'),
    ('pcpahdr_column2_match_count' + [char]9 + '1'),
    ('pcpahdr_column3_match_count' + [char]9 + '0'),
    ('pcpahdr_row_ordinal_match_count' + [char]9 + '3'),
    ('pcpadat_any_column_match_count' + [char]9 + '0'),
    ('pcpadat_column1_match_count' + [char]9 + '0'),
    ('pcpadat_column2_match_count' + [char]9 + '0'),
    ('pcpadat_column3_match_count' + [char]9 + '0'),
    ('pcpadat_row_ordinal_match_count' + [char]9 + '3'),
    ('opaque_word_semantics' + [char]9 + 'not-decoded'),
    ('status' + [char]9 + 'verified-structural-csi-opaque-off-field-control'),
    ('provenance' + [char]9 + 'FIFAST2.csi+pcpahdr.off+pcpadat.off;numeric-column-equality-and-row-ordinal-control;no-binding'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($fifast2CsiOpaqueFieldSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "FIFAST2 CSI opaque off-field summary missing '$requiredMarker'."
    }
}
Invoke-Smoke 'audio-sccl' @(
    'audio-sccl-channel-layout-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-sccl-channel-layout-audit.tsv')
)
Invoke-Smoke 'audio-abk' @(
    'audio-abk-batch-decode-smoke', $IsoPath, '--report',
    (Join-Path $ReportRoot 'audio-abk-batch-decode.tsv'), '--max-attempts', '8'
)
Invoke-Smoke 'audio-payload-bounded-candidates' @(
    'audio-event-payload-wave-correlation-exhaustive-audit', $IsoPath,
    '--max-sound-candidates', '512', '--report',
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-exhaustive.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-exhaustive-summary.tsv')
)
Invoke-Smoke 'audio-payload-all-candidates' @(
    'audio-event-payload-wave-correlation-exhaustive-audit', $IsoPath,
    '--max-sound-candidates', '100000', '--report',
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all.tsv'), '--summary',
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all-summary.tsv')
)
$audioPayloadAllSummaryPath = Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all-summary.tsv'
$audioPayloadAllSummary = Get-Content -LiteralPath $audioPayloadAllSummaryPath -Raw
foreach ($requiredMarker in @(
    ('event_candidates' + [char]9 + '17'),
    ('event_decoded' + [char]9 + '17'),
    ('bank_count' + [char]9 + '16'),
    ('bank_sound_candidates' + [char]9 + '241'),
    ('bank_sound_decoded' + [char]9 + '241'),
    ('exact_pcm_hash_pairs' + [char]9 + '0'),
    ('event_hashes_with_match' + [char]9 + '0'),
    ('bounded_policy' + [char]9 + 'first_100000_sound_candidates_per_bank'),
    ('status' + [char]9 + 'verified-structural'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($audioPayloadAllSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "All-candidate audio payload summary missing '$requiredMarker'."
    }
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_named_payload_identity_control.py'),
    (Join-Path $ReportRoot 'audio-named-event-runtime.tsv'),
    (Join-Path $ReportRoot 'audio-event-named-decode.tsv'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-named-payload-identity-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-named-payload-identity-control-summary.tsv')
)
$audioNamedPayloadIdentitySummaryPath = Join-Path $ReportRoot 'audio-event-named-payload-identity-control-summary.tsv'
$audioNamedPayloadIdentitySummary = Import-Csv -LiteralPath $audioNamedPayloadIdentitySummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioNamedPayloadIdentitySummary -or
    $audioNamedPayloadIdentitySummary.status -cne 'verified-independent-audio-event-named-payload-identity-control' -or
    $audioNamedPayloadIdentitySummary.runtime_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.named_decode_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.correlation_event_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.correlation_bank_rows -ne '242' -or
    $audioNamedPayloadIdentitySummary.correlation_bank_decoded_rows -ne '241' -or
    $audioNamedPayloadIdentitySummary.joined_event_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.missing_runtime_events -ne '0' -or
    $audioNamedPayloadIdentitySummary.missing_named_decode_events -ne '0' -or
    $audioNamedPayloadIdentitySummary.missing_correlation_events -ne '0' -or
    $audioNamedPayloadIdentitySummary.runtime_metadata_validated_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.runtime_prepared_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.runtime_wave_header_valid_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.named_decode_validated_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.correlation_decode_validated_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.wave_bytes_match_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.pcm_bytes_match_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.pcm_sha256_match_rows -ne '17' -or
    $audioNamedPayloadIdentitySummary.event_hashes_with_bank_match -ne '0' -or
    $audioNamedPayloadIdentitySummary.exact_pcm_hash_pairs -ne '0' -or
    $audioNamedPayloadIdentitySummary.unique_event_names -ne '17' -or
    $audioNamedPayloadIdentitySummary.direct_selection_binding -cne 'not-proven' -or
    $audioNamedPayloadIdentitySummary.original_semantics -cne 'not-claimed') {
    throw "Independent named EVT payload identity control has unexpected content: $audioNamedPayloadIdentitySummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_payload_policy_control.py'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-exhaustive.tsv'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-payload-policy-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-payload-policy-control-summary.tsv')
)
$audioPayloadPolicyControlSummaryPath = Join-Path $ReportRoot 'audio-event-payload-policy-control-summary.tsv'
$audioPayloadPolicyControlSummary = Import-Csv -LiteralPath $audioPayloadPolicyControlSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioPayloadPolicyControlSummary -or
    $audioPayloadPolicyControlSummary.status -cne 'verified-independent-audio-event-payload-policy-control' -or
    $audioPayloadPolicyControlSummary.bounded_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.all_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.joined_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.bounded_event_rows -ne '17' -or
    $audioPayloadPolicyControlSummary.all_event_rows -ne '17' -or
    $audioPayloadPolicyControlSummary.bounded_bank_rows -ne '242' -or
    $audioPayloadPolicyControlSummary.all_bank_rows -ne '242' -or
    $audioPayloadPolicyControlSummary.identity_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.status_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.decode_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.wave_bytes_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.pcm_hash_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.exact_match_count_match_rows -ne '259' -or
    $audioPayloadPolicyControlSummary.exact_pcm_match_rows -ne '0' -or
    $audioPayloadPolicyControlSummary.original_semantics -cne 'not-claimed') {
    throw "Independent audio payload policy control has unexpected content: $audioPayloadPolicyControlSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_bank_decoder_selection_control.py'),
    (Join-Path $ReportRoot 'audio-abk-batch-decode.tsv'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-bank-decoder-selection-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-bank-decoder-selection-control-summary.tsv')
)
$audioBankDecoderSelectionSummaryPath = Join-Path $ReportRoot 'audio-bank-decoder-selection-control-summary.tsv'
$audioBankDecoderSelectionSummary = Import-Csv -LiteralPath $audioBankDecoderSelectionSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioBankDecoderSelectionSummary -or
    $audioBankDecoderSelectionSummary.status -cne 'verified-independent-audio-bank-decoder-selection-control' -or
    $audioBankDecoderSelectionSummary.batch_rows -ne '16' -or
    $audioBankDecoderSelectionSummary.exhaustive_bank_rows -ne '242' -or
    $audioBankDecoderSelectionSummary.unique_batch_banks -ne '16' -or
    $audioBankDecoderSelectionSummary.unique_exhaustive_banks -ne '16' -or
    $audioBankDecoderSelectionSummary.batch_decoded_rows -ne '15' -or
    $audioBankDecoderSelectionSummary.batch_rejected_rows -ne '1' -or
    $audioBankDecoderSelectionSummary.selected_identity_rows -ne '15' -or
    $audioBankDecoderSelectionSummary.selected_kind_match_rows -ne '15' -or
    $audioBankDecoderSelectionSummary.selected_wave_bytes_match_rows -ne '15' -or
    $audioBankDecoderSelectionSummary.rejected_policy_match_rows -ne '1' -or
    $audioBankDecoderSelectionSummary.valid_control_rows -ne '16' -or
    $audioBankDecoderSelectionSummary.direct_selection_binding -cne 'not-proven' -or
    $audioBankDecoderSelectionSummary.original_semantics -cne 'not-claimed') {
    throw "Independent audio bank decoder-selection control has unexpected content: $audioBankDecoderSelectionSummaryPath"
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_selection_negative_synthesis.py'),
    (Join-Path $ReportRoot 'audio-event-index-evidence.tsv'),
    (Join-Path $ReportRoot 'audio-event-xbe-hash-search-summary.tsv'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all-summary.tsv'),
    (Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control-summary.tsv'),
    (Join-Path $ReportRoot 'audio-authored-candidate-route-control-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-selection-negative-synthesis.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-selection-negative-synthesis-summary.tsv')
)
$audioSelectionNegativeSummaryPath = Join-Path $ReportRoot 'audio-event-selection-negative-synthesis-summary.tsv'
$audioSelectionNegativeSummary = Get-Content -LiteralPath $audioSelectionNegativeSummaryPath -Raw
foreach ($requiredMarker in @(
    ('source_count' + [char]9 + '5'),
    ('valid_source_count' + [char]9 + '5'),
    ('index_candidate_count' + [char]9 + '17'),
    ('index_abk_occurrences' + [char]9 + '0'),
    ('index_header_occurrences' + [char]9 + '0'),
    ('index_data_occurrences' + [char]9 + '0'),
    ('xbe_candidate_count' + [char]9 + '17'),
    ('xbe_aligned_occurrences' + [char]9 + '0'),
    ('xbe_unaligned_occurrences' + [char]9 + '0'),
    ('payload_event_candidates' + [char]9 + '17'),
    ('payload_event_decoded' + [char]9 + '17'),
    ('payload_bank_count' + [char]9 + '16'),
    ('payload_bank_sound_candidates' + [char]9 + '241'),
    ('payload_bank_sound_decoded' + [char]9 + '241'),
    ('payload_exact_pcm_hash_pairs' + [char]9 + '0'),
    ('payload_event_hashes_with_match' + [char]9 + '0'),
    ('parallel_joined_rows' + [char]9 + '17'),
    ('authored_route_rows' + [char]9 + '15'),
    ('status' + [char]9 + 'verified-independent-audio-selection-negative-synthesis'),
    ('direct_selection_binding' + [char]9 + 'not-proven'),
    ('original_semantics' + [char]9 + 'not-claimed')
)) {
    if ($audioSelectionNegativeSummary.IndexOf($requiredMarker, [StringComparison]::Ordinal) -lt 0) {
        throw "Independent audio selection negative synthesis missing '$requiredMarker'."
    }
}
Invoke-Python -Arguments @(
    (Join-Path $Workspace 'tools/analysis/audio_event_sample_global_assignment_control.py'),
    (Join-Path $ReportRoot 'audio-event-payload-wave-correlation-all.tsv'),
    (Join-Path $ReportRoot 'fifast2-csi-evt-name-ambiguity-control.tsv'),
    (Join-Path $ReportRoot 'audio-event-parallel-runtime-join-control.tsv'),
    (Join-Path $ReportRoot 'audio-event-selection-negative-synthesis-summary.tsv'),
    (Join-Path $ReportRoot 'audio-authored-candidate-route-control-summary.tsv'),
    '--report', (Join-Path $ReportRoot 'audio-event-sample-global-assignment-control.tsv'),
    '--summary', (Join-Path $ReportRoot 'audio-event-sample-global-assignment-control-summary.tsv')
)
$audioSampleGlobalAssignmentSummaryPath = Join-Path $ReportRoot 'audio-event-sample-global-assignment-control-summary.tsv'
$audioSampleGlobalAssignmentSummary = Import-Csv -LiteralPath $audioSampleGlobalAssignmentSummaryPath -Delimiter ([char]9) | Select-Object -First 1
if ($null -eq $audioSampleGlobalAssignmentSummary -or
    $audioSampleGlobalAssignmentSummary.status -cne 'verified-independent-original-audio-event-sample-global-assignment-control' -or
    $audioSampleGlobalAssignmentSummary.event_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.bank_rows -ne '242' -or
    $audioSampleGlobalAssignmentSummary.decoded_event_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.decoded_bank_rows -ne '241' -or
    $audioSampleGlobalAssignmentSummary.event_input_status_valid_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.csi_input_status_valid_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.parallel_input_status_valid_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.negative_synthesis_valid_source_count -ne '5' -or
    $audioSampleGlobalAssignmentSummary.authored_route_rows -ne '15' -or
    $audioSampleGlobalAssignmentSummary.bank_candidate_pool_size -ne '241' -or
    $audioSampleGlobalAssignmentSummary.event_exact_pcm_match_rows -ne '0' -or
    $audioSampleGlobalAssignmentSummary.event_hashes_with_match -ne '0' -or
    $audioSampleGlobalAssignmentSummary.evt_rows_with_zero_exact_sample_models -ne '17' -or
    $audioSampleGlobalAssignmentSummary.evt_rows_with_single_weak_name_models -ne '3' -or
    $audioSampleGlobalAssignmentSummary.evt_rows_with_multiple_weak_name_models -ne '4' -or
    $audioSampleGlobalAssignmentSummary.evt_rows_with_zero_weak_name_models -ne '10' -or
    $audioSampleGlobalAssignmentSummary.weak_name_domain_sum -ne '38' -or
    $audioSampleGlobalAssignmentSummary.exact_identity_model_sum -ne '0' -or
    $audioSampleGlobalAssignmentSummary.structural_cross_product_model_sum -ne '0' -or
    $audioSampleGlobalAssignmentSummary.unique_evt_abkc_sample_binding_proven_rows -ne '0' -or
    $audioSampleGlobalAssignmentSummary.valid_event_rows -ne '17' -or
    $audioSampleGlobalAssignmentSummary.semantic_binding -cne 'not-proven' -or
    $audioSampleGlobalAssignmentSummary.direct_selection_binding -cne 'not-proven' -or
    $audioSampleGlobalAssignmentSummary.original_semantics -cne 'not-claimed') {
    throw "Independent original audio EVT-to-sample global assignment control has unexpected content: $audioSampleGlobalAssignmentSummaryPath"
}
Invoke-Smoke 'apt-fe' @(
    'apt-structure-inventory', $IsoPath, 'data/aptanims/fe.bh', '--report',
    (Join-Path $ReportRoot 'apt-structure-fe.tsv'), '--profile-report',
    (Join-Path $ReportRoot 'apt-structure-fe-profiles.tsv')
)
Invoke-Smoke 'apt-ig' @(
    'apt-structure-inventory', $IsoPath, 'data/aptanims/ig.bh', '--report',
    (Join-Path $ReportRoot 'apt-structure-ig.tsv'), '--profile-report',
    (Join-Path $ReportRoot 'apt-structure-ig-profiles.tsv')
)
Invoke-Smoke 'apt-negative-control' @(
    'apt-animation-negative-control',
    (Join-Path $ReportRoot 'apt-structure-fe.tsv'),
    (Join-Path $ReportRoot 'apt-structure-ig.tsv'),
    (Join-Path $ReportRoot 'animation-bank-name-index.tsv'), '--report',
    (Join-Path $ReportRoot 'apt-animation-negative-control.tsv')
)
Invoke-Smoke 'apt-cross-package-profile' @(
    'apt-cross-package-profile-audit',
    (Join-Path $ReportRoot 'apt-structure-fe-profiles.tsv'),
    (Join-Path $ReportRoot 'apt-structure-ig-profiles.tsv'),
    (Join-Path $ReportRoot 'apt-cross-package-profile-audit.tsv'), '--summary',
    (Join-Path $ReportRoot 'apt-cross-package-profile-audit-summary.tsv')
)
Invoke-Dotnet -Arguments @(
    'run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--',
    'project-readiness-audit', $IsoPath, '--report',
    (Join-Path $ReportRoot 'project-readiness.tsv')
)

$isoAfter = Get-FileHash -Algorithm SHA256 -LiteralPath $IsoPath
$isoItemAfter = Get-Item -LiteralPath $IsoPath
if ($isoBefore.Hash -ne $isoAfter.Hash -or
    $isoItemBefore.Length -ne $isoItemAfter.Length -or
    $isoItemBefore.LastWriteTimeUtc -ne $isoItemAfter.LastWriteTimeUtc -or
    $isoItemBefore.Attributes -ne $isoItemAfter.Attributes) {
    throw "A ISO mudou durante a matriz authored. Antes=$($isoBefore.Hash), Depois=$($isoAfter.Hash)"
}

Write-Host "matrix_status: completed-authored-and-candidate-opt-in"
Write-Host "iso_length: $($isoItemAfter.Length)"
Write-Host "iso_attributes: $($isoItemAfter.Attributes)"
Write-Host "iso_sha256: $($isoAfter.Hash)"
