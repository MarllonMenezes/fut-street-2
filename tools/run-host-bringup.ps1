[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [Parameter(Mandatory = $true)][string]$XbePath,
    [Parameter(Mandatory = $true)][string]$GameDataDirectory,
    [string]$OutputRoot = 'D:\projeto zeta\artifacts\bringup',
    [ValidateRange(1, 60)][int]$TimeoutSeconds = 20,
    [ValidateRange(1, 400000)][int]$TraceBudget = 20000,
    [switch]$GraphicsSnapshot,
    [switch]$HostOutputVblank,
    [switch]$NativeDisplayOrigin,
    [switch]$GraphicsTrace,
    [switch]$Aa2Approximation,
    [switch]$Dxt1NoiseApproximation,
    [switch]$P8Aa2Approximation,
    [switch]$P8_128x32Aa2Approximation,
    [switch]$Bc2Aa2Approximation,
    [switch]$Yuy2Bt601Approximation,
    [switch]$Yuy2Aa2Approximation,
    [switch]$Lines16segmentAa2Approximation,
    [switch]$QuincunxApproximation,
    [switch]$Ac97ResetApproximation,
    [switch]$AudioEffectsBypass
)

$ErrorActionPreference = 'Stop'
if (($Dxt1NoiseApproximation -or $P8Aa2Approximation -or $P8_128x32Aa2Approximation -or $Bc2Aa2Approximation -or $Yuy2Bt601Approximation) -and -not $Aa2Approximation) {
    throw 'Restricted texture experiments require -Aa2Approximation.'
}
if ($Yuy2Aa2Approximation -and (-not $Aa2Approximation -or -not $Yuy2Bt601Approximation)) {
    throw 'YUY2 AA2 requires -Aa2Approximation and -Yuy2Bt601Approximation.'
}
foreach ($path in @($ExecutablePath, $XbePath, $GameDataDirectory)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Input missing: $path" }
}
$ExecutablePath = (Resolve-Path -LiteralPath $ExecutablePath).Path
$XbePath = (Resolve-Path -LiteralPath $XbePath).Path
$GameDataDirectory = (Resolve-Path -LiteralPath $GameDataDirectory).Path
$OutputRoot = [IO.Path]::GetFullPath($OutputRoot)
foreach ($path in @($ExecutablePath, $XbePath, $GameDataDirectory, $OutputRoot)) {
    if ($path -notmatch '^[Dd]:\\' -or $path.Contains('"')) {
        throw "Bring-up paths must be absolute paths on D: without quote characters: $path"
    }
}
foreach ($name in @('RECOMP_AC97_READY', 'RECOMP_AC97_RESET_ONLY', 'RECOMP_PB_EXEC', 'RECOMP_VBLANK')) {
    if ($null -ne [Environment]::GetEnvironmentVariable($name)) {
        throw "Unsupported hardware opt-in is set: $name"
    }
}
$runDirectory = Join-Path $OutputRoot ((Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + [guid]::NewGuid().ToString('N').Substring(0, 8))
$saveDirectory = Join-Path $runDirectory 'saves'
$tempDirectory = Join-Path $runDirectory 'tmp'
New-Item -ItemType Directory -Path $saveDirectory, $tempDirectory -Force | Out-Null
$stdout = Join-Path $runDirectory 'stdout.log'
$stderr = Join-Path $runDirectory 'stderr.log'
$variables = @{
    TEMP = $tempDirectory; TMP = $tempDirectory; TMPDIR = $tempDirectory
    RECOMP_DIAGNOSTIC_TRAP = '1'; RECOMP_UNIMPL_TRAP = '1'
    RECOMP_TRACE_BUDGET = [string]$TraceBudget
    RECOMP_AA2_APPROXIMATION = '0'
    RECOMP_DXT1_NOISE_APPROXIMATION = '0'
    RECOMP_P8_AA2_APPROXIMATION = '0'
    RECOMP_P8_128X32_AA2_APPROXIMATION = '0'
    RECOMP_BC2_AA2_APPROXIMATION = '0'
    RECOMP_YUY2_023F1580_BT601_APPROXIMATION = '0'
    RECOMP_YUY2_023F1580_AA2_APPROXIMATION = '0'
    RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION = '0'
    RECOMP_QUINCUNX_APPROXIMATION = '0'
    RECOMP_NATIVE_DISPLAY_ORIGIN = '0'
    RECOMP_AUDIO_EFFECTS_BYPASS = '0'
}
if ($AudioEffectsBypass) { $variables.RECOMP_AUDIO_EFFECTS_BYPASS = '1' }
if ($GraphicsSnapshot) { $variables.RECOMP_GRAPHICS_SNAPSHOT = '1' }
if ($HostOutputVblank) { $variables.RECOMP_HOST_OUTPUT_VBLANK = '1' }
if ($NativeDisplayOrigin) { $variables.RECOMP_NATIVE_DISPLAY_ORIGIN = '1' }
if ($GraphicsTrace) { $variables.RECOMP_GRAPHICS_TRACE = '1' }
if ($Aa2Approximation) { $variables.RECOMP_AA2_APPROXIMATION = '1' }
if ($Dxt1NoiseApproximation) { $variables.RECOMP_DXT1_NOISE_APPROXIMATION = '1' }
if ($P8Aa2Approximation) { $variables.RECOMP_P8_AA2_APPROXIMATION = '1' }
if ($P8_128x32Aa2Approximation) { $variables.RECOMP_P8_128X32_AA2_APPROXIMATION = '1' }
if ($Bc2Aa2Approximation) { $variables.RECOMP_BC2_AA2_APPROXIMATION = '1' }
if ($Yuy2Bt601Approximation) { $variables.RECOMP_YUY2_023F1580_BT601_APPROXIMATION = '1' }
if ($Yuy2Aa2Approximation) { $variables.RECOMP_YUY2_023F1580_AA2_APPROXIMATION = '1' }
if ($Lines16segmentAa2Approximation) { $variables.RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION = '1' }
if ($QuincunxApproximation) { $variables.RECOMP_QUINCUNX_APPROXIMATION = '1' }
if ($Ac97ResetApproximation) { $variables.RECOMP_AC97_RESET_ONLY = '1' }
$previous = @{}
$process = $null
$stdoutFile = $null
$stderrFile = $null
$timedOut = $false
try {
    foreach ($name in $variables.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        [Environment]::SetEnvironmentVariable($name, $variables[$name], 'Process')
    }
    $arguments = '--launch "{0}" "{1}" "{2}"' -f $XbePath, $GameDataDirectory, $saveDirectory
    # Drain both streams on .NET tasks, independently of the PowerShell runspace.
    # A verbose native trace must not stall behind a full redirected pipe.
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $ExecutablePath
    $startInfo.Arguments = $arguments
    $startInfo.WorkingDirectory = $runDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $stdoutFile = [IO.File]::Open($stdout, 'CreateNew', 'Write', 'Read')
    $stderrFile = [IO.File]::Open($stderr, 'CreateNew', 'Write', 'Read')
    if (-not $process.Start()) { throw 'Unable to start recompiled host' }
    $stdoutTask = $process.StandardOutput.BaseStream.CopyToAsync($stdoutFile)
    $stderrTask = $process.StandardError.BaseStream.CopyToAsync($stderrFile)
    $timedOut = -not $process.WaitForExit($TimeoutSeconds * 1000)
    if ($timedOut) { $process.Kill(); $process.WaitForExit() }
    $null = $stdoutTask.GetAwaiter().GetResult()
    $null = $stderrTask.GetAwaiter().GetResult()
    $stdoutFile.Dispose(); $stdoutFile = $null
    $stderrFile.Dispose(); $stderrFile = $null
    $process.Refresh()
    $summary = [ordered]@{
        executable = $ExecutablePath; executable_sha256 = (Get-FileHash -LiteralPath $ExecutablePath).Hash
        xbe = $XbePath; xbe_sha256 = (Get-FileHash -LiteralPath $XbePath).Hash
        game_data = $GameDataDirectory; save_directory = $saveDirectory
        timeout_seconds = $TimeoutSeconds; timed_out = $timedOut
        trace_budget = $TraceBudget
        exit_code = $process.ExitCode; diagnostic_trap = $true
        graphics_snapshot = [bool]$GraphicsSnapshot
        native_display_origin = [bool]$NativeDisplayOrigin
        graphics_trace = [bool]$GraphicsTrace
        aa2_approximation = [bool]$Aa2Approximation
        dxt1_noise_approximation = [bool]$Dxt1NoiseApproximation
        p8_aa2_approximation = [bool]$P8Aa2Approximation
        p8_128x32_aa2_approximation = [bool]$P8_128x32Aa2Approximation
        bc2_aa2_approximation = [bool]$Bc2Aa2Approximation
        yuy2_bt601_approximation = [bool]$Yuy2Bt601Approximation
        yuy2_aa2_approximation = [bool]$Yuy2Aa2Approximation
        lines_16segment_aa2_approximation = [bool]$Lines16segmentAa2Approximation
        quincunx_approximation = [bool]$QuincunxApproximation
        ac97_reset_approximation = [bool]$Ac97ResetApproximation
        audio_effects_bypass = [bool]$AudioEffectsBypass
        audio_effects_bypass_max_completions = $(if ($AudioEffectsBypass) { 4096 } else { 0 })
        functional_status = 'NOT_READY'
        stdout = $stdout; stderr = $stderr
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runDirectory 'run.json') -Encoding UTF8
    $summary | ConvertTo-Json | Write-Output
    Get-Content -LiteralPath $stderr -Tail 60
} finally {
    if ($null -ne $process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    foreach ($name in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
    if ($null -ne $process) { $process.Dispose() }
    if ($null -ne $stdoutFile) { $stdoutFile.Dispose() }
    if ($null -ne $stderrFile) { $stderrFile.Dispose() }
}
