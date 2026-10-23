[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [Parameter(Mandatory = $true)][string]$XbePath,
    [Parameter(Mandatory = $true)][string]$GameDataDirectory,
    [string]$OutputRoot = 'D:\projeto zeta\artifacts\bringup',
    [ValidateRange(1, 300)][int]$TimeoutSeconds = 20,
    [ValidateRange(1, 400000)][int]$TraceBudget = 20000,
    [switch]$GraphicsSnapshot,
    [switch]$HostOutputVblank,
    [switch]$NativeDisplayOrigin,
    [switch]$GraphicsTrace,
    [switch]$HighResolutionGpuWait,
    [switch]$Aa2Approximation,
    [switch]$UntexturedAa2Approximation,
    [switch]$ProjectiveAa2Approximation,
    [switch]$Dxt1NoiseApproximation,
    [switch]$P8Aa2Approximation,
    [switch]$P8_128x32Aa2Approximation,
    [switch]$Bc2Aa2Approximation,
    [switch]$Bc2C9df00Aa2Approximation,
    [switch]$Bc2Aa2FamilyApproximation,
    [switch]$P8Aa2FamilyApproximation,
    [switch]$Yuy2Bt601Approximation,
    [switch]$Yuy2Aa2Approximation,
    [switch]$Yuy2SecondBt601Approximation,
    [switch]$Yuy2SecondAa2Approximation,
    [switch]$Yuy2FamilyBt601Approximation,
    [switch]$Yuy2FamilyAa2Approximation,
    [switch]$CaptureLcImage,
    [switch]$CapturePresentedFrames,
    [ValidateRange(1000, 10000)][int]$FrameCaptureIntervalMs = 1000,
    [switch]$Lines16segmentAa2Approximation,
    [switch]$Lines4segmentAa2Approximation,
    [switch]$QuincunxApproximation,
    [switch]$Ac97ResetApproximation,
    [switch]$AudioEffectsBypass,
    [ValidateRange(1, 16384)][int]$AudioEffectsBypassMaxCompletions = 4096,
    [switch]$NativePcmOutput,
    [switch]$FrontendLoadingTrace,
    [switch]$NativeGamepadInput,
    [switch]$NativeKeyboardInput,
    [switch]$NativeInputVirtualOnly,
    [ValidateLength(0,512)][string]$NativeInputScript='',
    [switch]$StreamCallbackTrace,
    [ValidateRange(1, 10000)][int]$StreamCallbackTraceLimit = 128,
    [switch]$TraceStreamWait,
    [switch]$TraceStreamCompletion,
    [switch]$TraceStreamSpace,
    [switch]$TraceStreamResume,
    [switch]$TraceStreamRead,
    [switch]$TraceStreamEvent,
    [switch]$TraceStreamBufferCallback,
    [switch]$TraceStreamBufferCallbackDetail,
    [switch]$StreamSchedulerTrace,
    [switch]$TraceStreamPump,
    [switch]$TraceTargetStreamRelease,
    [switch]$TraceTargetEventTable,
    [switch]$TraceEventWait,
    [switch]$TraceStreamWaitClock,
    [switch]$ExperimentalTargetStreamPumpAfterStatus2
)

$ErrorActionPreference = 'Stop'
if(($NativeKeyboardInput -or $NativeInputVirtualOnly -or $NativeInputScript) -and -not $NativeGamepadInput){throw 'Native keyboard/script/provider requires -NativeGamepadInput.'}
if (($UntexturedAa2Approximation -or $ProjectiveAa2Approximation) -and -not $Aa2Approximation) {
    throw 'The requested AA2 experiment requires -Aa2Approximation.'
}
if ($Lines4segmentAa2Approximation -and -not $Aa2Approximation) {
    throw 'Four-segment LINES requires -Aa2Approximation.'
}
if (($Dxt1NoiseApproximation -or $P8Aa2Approximation -or $P8_128x32Aa2Approximation -or $Bc2Aa2Approximation -or $Bc2C9df00Aa2Approximation -or $Bc2Aa2FamilyApproximation -or $P8Aa2FamilyApproximation -or $Yuy2Bt601Approximation -or $Yuy2SecondBt601Approximation -or $Yuy2FamilyBt601Approximation -or $Yuy2FamilyAa2Approximation) -and -not $Aa2Approximation) {
    throw 'Restricted texture experiments require -Aa2Approximation.'
}
if ($Yuy2Aa2Approximation -and (-not $Aa2Approximation -or -not $Yuy2Bt601Approximation)) {
    throw 'YUY2 AA2 requires -Aa2Approximation and -Yuy2Bt601Approximation.'
}
if ($Yuy2SecondAa2Approximation -and (-not $Aa2Approximation -or -not $Yuy2SecondBt601Approximation)) {
    throw 'Second YUY2 AA2 requires -Aa2Approximation and -Yuy2SecondBt601Approximation.'
}
if ($Yuy2FamilyAa2Approximation -and (-not $Aa2Approximation -or -not $Yuy2FamilyBt601Approximation)) {
    throw 'YUY2 family AA2 requires -Aa2Approximation and -Yuy2FamilyBt601Approximation.'
}
if ($TraceStreamBufferCallbackDetail -and -not $TraceStreamBufferCallback) {
    throw '-TraceStreamBufferCallbackDetail requires -TraceStreamBufferCallback.'
}
if ($ExperimentalTargetStreamPumpAfterStatus2 -and
    (-not $AudioEffectsBypass -or -not $StreamCallbackTrace -or
     -not $TraceStreamSpace -or -not $TraceStreamPump)) {
    throw 'The target status2 pump experiment requires -AudioEffectsBypass, -StreamCallbackTrace, -TraceStreamSpace, and -TraceStreamPump.'
}
if ($TraceTargetStreamRelease -and
    (-not $ExperimentalTargetStreamPumpAfterStatus2 -or -not $TraceStreamPump)) {
    throw '-TraceTargetStreamRelease requires -ExperimentalTargetStreamPumpAfterStatus2 and -TraceStreamPump.'
}
if ($TraceTargetEventTable -and
    (-not $ExperimentalTargetStreamPumpAfterStatus2 -or -not $TraceTargetStreamRelease)) {
    throw '-TraceTargetEventTable requires -ExperimentalTargetStreamPumpAfterStatus2 and -TraceTargetStreamRelease.'
}
if ($TraceEventWait -and
    (-not $ExperimentalTargetStreamPumpAfterStatus2 -or -not $TraceTargetEventTable)) {
    throw '-TraceEventWait requires -ExperimentalTargetStreamPumpAfterStatus2 and -TraceTargetEventTable.'
}
if ($TraceStreamWaitClock -and
    (-not $ExperimentalTargetStreamPumpAfterStatus2 -or -not $TraceTargetStreamRelease)) {
    throw '-TraceStreamWaitClock requires -ExperimentalTargetStreamPumpAfterStatus2 and -TraceTargetStreamRelease.'
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
$tempDirectory = 'D:\projeto zeta\artifacts\tmp'
New-Item -ItemType Directory -Path $saveDirectory, $tempDirectory -Force | Out-Null
$stdout = Join-Path $runDirectory 'stdout.log'
$stderr = Join-Path $runDirectory 'stderr.log'
$variables = @{
    TEMP = $tempDirectory; TMP = $tempDirectory; TMPDIR = $tempDirectory
    RECOMP_DIAGNOSTIC_TRAP = '1'; RECOMP_UNIMPL_TRAP = '1'
    RECOMP_TRACE_BUDGET = [string]$TraceBudget
    RECOMP_AA2_APPROXIMATION = '0'
    RECOMP_UNTEXTURED_AA2_APPROXIMATION = '0'
    RECOMP_PROJECTIVE_AA2_APPROXIMATION = '0'
    RECOMP_DXT1_NOISE_APPROXIMATION = '0'
    RECOMP_P8_AA2_APPROXIMATION = '0'
    RECOMP_P8_128X32_AA2_APPROXIMATION = '0'
    RECOMP_BC2_AA2_APPROXIMATION = '0'
    RECOMP_BC2_C9DF00_AA2_APPROXIMATION = '0'
    RECOMP_BC2_AA2_FAMILY_APPROXIMATION = '0'
    RECOMP_P8_AA2_FAMILY_APPROXIMATION = '0'
    RECOMP_YUY2_023F1580_BT601_APPROXIMATION = '0'
    RECOMP_YUY2_023F1580_AA2_APPROXIMATION = '0'
    RECOMP_YUY2_02487680_BT601_APPROXIMATION = '0'
    RECOMP_YUY2_02487680_AA2_APPROXIMATION = '0'
    RECOMP_YUY2_FAMILY_BT601_APPROXIMATION = '0'
    RECOMP_YUY2_FAMILY_AA2_APPROXIMATION = '0'
    RECOMP_CAPTURE_LC_IMAGE = '0'
    RECOMP_CAPTURE_PRESENTED_FRAMES = '0'
    RECOMP_FRAME_CAPTURE_INTERVAL_MS = [string]$FrameCaptureIntervalMs
    RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION = '0'
    RECOMP_LINES_4SEGMENT_AA2_APPROXIMATION = '0'
    RECOMP_QUINCUNX_APPROXIMATION = '0'
    RECOMP_NATIVE_DISPLAY_ORIGIN = '0'
    RECOMP_AUDIO_EFFECTS_BYPASS = '0'
    RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = [string]$AudioEffectsBypassMaxCompletions
    RECOMP_NATIVE_PCM_OUTPUT = '0'
    RECOMP_NATIVE_INPUT = '0'
    RECOMP_NATIVE_KEYBOARD_INPUT = '0'
    RECOMP_NATIVE_INPUT_PHYSICAL = '1'
    RECOMP_NATIVE_INPUT_SCRIPT = ''
    RECOMP_FRONTEND_LOADING_TRACE = '0'
    RECOMP_GPU_HIGH_RES_WAIT = '0'
    RECOMP_STREAM_CALLBACK_TRACE = '0'
    RECOMP_STREAM_CALLBACK_TRACE_LIMIT = [string]$StreamCallbackTraceLimit
    RECOMP_TRACE_TARGET_STREAM_EVENTS = $null
    RECOMP_TRACE_TARGET_STREAM_ROUTES = $null
    RECOMP_TRACE_STREAM_UPDATER = $null
    RECOMP_TRACE_TARGET_STREAM_MANAGER = $null
    RECOMP_TRACE_TARGET_STREAM_REGISTRATION = $null
    RECOMP_TRACE_STREAM_WAIT = $null
    RECOMP_TRACE_STREAM_COMPLETION = $null
    RECOMP_TRACE_STREAM_SPACE = $null
    RECOMP_TRACE_STREAM_RESUME = $null
    RECOMP_TRACE_STREAM_READ = $null
    RECOMP_TRACE_STREAM_EVENT = $null
    RECOMP_TRACE_STREAM_CALLBACK = $null
    RECOMP_TRACE_STREAM_CALLBACK_DETAIL = $null
    RECOMP_TRACE_STREAM_SCHEDULER = $null
    RECOMP_TRACE_STREAM_PUMP = $null
    RECOMP_TRACE_TARGET_STREAM_PUMP_PROGRESS = $null
    RECOMP_TRACE_TARGET_STREAM_RELEASE = $null
    RECOMP_TRACE_TARGET_EVENT_TABLE = $null
    RECOMP_TRACE_EVENT_WAIT = $null
    RECOMP_TRACE_STREAM_WAIT_CLOCK = $null
    RECOMP_EXPERIMENT_TARGET_STREAM_PUMP = $null
    RECOMP_EXPERIMENT_TARGET_STREAM_PUMP_AFTER_STATUS2 = $null
}
if ($AudioEffectsBypass) { $variables.RECOMP_AUDIO_EFFECTS_BYPASS = '1' }
if ($UntexturedAa2Approximation) { $variables.RECOMP_UNTEXTURED_AA2_APPROXIMATION = '1' }
if ($ProjectiveAa2Approximation) { $variables.RECOMP_PROJECTIVE_AA2_APPROXIMATION = '1' }
if ($FrontendLoadingTrace) { $variables.RECOMP_FRONTEND_LOADING_TRACE = '1' }
if ($NativeGamepadInput) { $variables.RECOMP_NATIVE_INPUT = '1' }
if ($NativeKeyboardInput) { $variables.RECOMP_NATIVE_KEYBOARD_INPUT = '1' }
if ($NativeInputVirtualOnly) { $variables.RECOMP_NATIVE_INPUT_PHYSICAL = '0' }
if ($NativeInputScript) { $variables.RECOMP_NATIVE_INPUT_SCRIPT = $NativeInputScript }
if ($NativePcmOutput) { $variables.RECOMP_NATIVE_PCM_OUTPUT = '1' }
if ($HighResolutionGpuWait) { $variables.RECOMP_GPU_HIGH_RES_WAIT = '1' }
if ($CapturePresentedFrames) { $variables.RECOMP_CAPTURE_PRESENTED_FRAMES = '1' }
if ($StreamCallbackTrace) {
    $variables.RECOMP_STREAM_CALLBACK_TRACE = '1'
    $variables.RECOMP_TRACE_TARGET_STREAM_EVENTS = '1'
    $variables.RECOMP_TRACE_TARGET_STREAM_ROUTES = '1'
    $variables.RECOMP_TRACE_STREAM_UPDATER = '1'
    $variables.RECOMP_TRACE_TARGET_STREAM_MANAGER = '1'
    $variables.RECOMP_TRACE_TARGET_STREAM_REGISTRATION = '1'
}
if ($TraceStreamWait) { $variables.RECOMP_TRACE_STREAM_WAIT = '1' }
if ($TraceStreamCompletion) { $variables.RECOMP_TRACE_STREAM_COMPLETION = '1' }
if ($TraceStreamSpace) { $variables.RECOMP_TRACE_STREAM_SPACE = '1' }
if ($TraceStreamResume) { $variables.RECOMP_TRACE_STREAM_RESUME = '1' }
if ($TraceStreamRead) { $variables.RECOMP_TRACE_STREAM_READ = '1' }
if ($TraceStreamEvent) { $variables.RECOMP_TRACE_STREAM_EVENT = '1' }
if ($TraceStreamBufferCallback) { $variables.RECOMP_TRACE_STREAM_CALLBACK = '1' }
if ($TraceStreamBufferCallbackDetail) { $variables.RECOMP_TRACE_STREAM_CALLBACK_DETAIL = '1' }
if ($StreamSchedulerTrace) { $variables.RECOMP_TRACE_STREAM_SCHEDULER = '1' }
if ($TraceStreamPump) {
    $variables.RECOMP_TRACE_STREAM_PUMP = '1'
    $variables.RECOMP_TRACE_TARGET_STREAM_PUMP_PROGRESS = '1'
}
if ($TraceTargetStreamRelease) { $variables.RECOMP_TRACE_TARGET_STREAM_RELEASE = '1' }
if ($TraceTargetEventTable) { $variables.RECOMP_TRACE_TARGET_EVENT_TABLE = '1' }
if ($TraceEventWait) { $variables.RECOMP_TRACE_EVENT_WAIT = '1' }
if ($TraceStreamWaitClock) { $variables.RECOMP_TRACE_STREAM_WAIT_CLOCK = '1' }
if ($ExperimentalTargetStreamPumpAfterStatus2) {
    $variables.RECOMP_EXPERIMENT_TARGET_STREAM_PUMP_AFTER_STATUS2 = '1'
}
if ($GraphicsSnapshot) { $variables.RECOMP_GRAPHICS_SNAPSHOT = '1' }
if ($HostOutputVblank) { $variables.RECOMP_HOST_OUTPUT_VBLANK = '1' }
if ($NativeDisplayOrigin) { $variables.RECOMP_NATIVE_DISPLAY_ORIGIN = '1' }
if ($GraphicsTrace) { $variables.RECOMP_GRAPHICS_TRACE = '1' }
if ($Aa2Approximation) { $variables.RECOMP_AA2_APPROXIMATION = '1' }
if ($Dxt1NoiseApproximation) { $variables.RECOMP_DXT1_NOISE_APPROXIMATION = '1' }
if ($P8Aa2Approximation) { $variables.RECOMP_P8_AA2_APPROXIMATION = '1' }
if ($P8_128x32Aa2Approximation) { $variables.RECOMP_P8_128X32_AA2_APPROXIMATION = '1' }
if ($Bc2Aa2Approximation) { $variables.RECOMP_BC2_AA2_APPROXIMATION = '1' }
if ($Bc2C9df00Aa2Approximation) { $variables.RECOMP_BC2_C9DF00_AA2_APPROXIMATION = '1' }
if ($Bc2Aa2FamilyApproximation) { $variables.RECOMP_BC2_AA2_FAMILY_APPROXIMATION = '1' }
if ($P8Aa2FamilyApproximation) { $variables.RECOMP_P8_AA2_FAMILY_APPROXIMATION = '1' }
if ($Yuy2Bt601Approximation) { $variables.RECOMP_YUY2_023F1580_BT601_APPROXIMATION = '1' }
if ($Yuy2Aa2Approximation) { $variables.RECOMP_YUY2_023F1580_AA2_APPROXIMATION = '1' }
if ($Yuy2SecondBt601Approximation) { $variables.RECOMP_YUY2_02487680_BT601_APPROXIMATION = '1' }
if ($Yuy2SecondAa2Approximation) { $variables.RECOMP_YUY2_02487680_AA2_APPROXIMATION = '1' }
if ($Yuy2FamilyBt601Approximation) { $variables.RECOMP_YUY2_FAMILY_BT601_APPROXIMATION = '1' }
if ($Yuy2FamilyAa2Approximation) { $variables.RECOMP_YUY2_FAMILY_AA2_APPROXIMATION = '1' }
if ($CaptureLcImage) { $variables.RECOMP_CAPTURE_LC_IMAGE = '1' }
if ($Lines16segmentAa2Approximation) { $variables.RECOMP_LINES_16SEGMENT_AA2_APPROXIMATION = '1' }
if ($Lines4segmentAa2Approximation) { $variables.RECOMP_LINES_4SEGMENT_AA2_APPROXIMATION = '1' }
if ($QuincunxApproximation) { $variables.RECOMP_QUINCUNX_APPROXIMATION = '1' }
if ($Ac97ResetApproximation) { $variables.RECOMP_AC97_RESET_ONLY = '1' }
$process = $null
$stdoutFile = $null
$stderrFile = $null
$timedOut = $false
try {
    $arguments = '--launch "{0}" "{1}" "{2}"' -f $XbePath, $GameDataDirectory, $saveDirectory
    # Drain both streams on .NET tasks, independently of the PowerShell runspace.
    # A verbose native trace must not stall behind a full redirected pipe.
    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = $ExecutablePath
    $startInfo.Arguments = $arguments
    $startInfo.WorkingDirectory = $runDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $childEnvironment = $startInfo.EnvironmentVariables
    foreach ($name in $variables.Keys) {
        if ($null -eq $variables[$name]) {
            [void]$childEnvironment.Remove($name)
        } else {
            $childEnvironment[$name] = [string]$variables[$name]
        }
    }
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
        host_output_vblank = [bool]$HostOutputVblank
        native_display_origin = [bool]$NativeDisplayOrigin
        graphics_trace = [bool]$GraphicsTrace
        aa2_approximation = [bool]$Aa2Approximation
        untextured_aa2_approximation = [bool]$UntexturedAa2Approximation
        projective_aa2_approximation = [bool]$ProjectiveAa2Approximation
        dxt1_noise_approximation = [bool]$Dxt1NoiseApproximation
        p8_aa2_approximation = [bool]$P8Aa2Approximation
        p8_128x32_aa2_approximation = [bool]$P8_128x32Aa2Approximation
        bc2_aa2_approximation = [bool]$Bc2Aa2Approximation
        bc2_c9df00_aa2_approximation = [bool]$Bc2C9df00Aa2Approximation
        bc2_aa2_family_approximation = [bool]$Bc2Aa2FamilyApproximation
        p8_aa2_family_approximation = [bool]$P8Aa2FamilyApproximation
        yuy2_bt601_approximation = [bool]$Yuy2Bt601Approximation
        yuy2_aa2_approximation = [bool]$Yuy2Aa2Approximation
        yuy2_second_bt601_approximation = [bool]$Yuy2SecondBt601Approximation
        yuy2_second_aa2_approximation = [bool]$Yuy2SecondAa2Approximation
        yuy2_family_bt601_approximation = [bool]$Yuy2FamilyBt601Approximation
        yuy2_family_aa2_approximation = [bool]$Yuy2FamilyAa2Approximation
        capture_lc_image = [bool]$CaptureLcImage
        lines_16segment_aa2_approximation = [bool]$Lines16segmentAa2Approximation
        lines_4segment_aa2_approximation = [bool]$Lines4segmentAa2Approximation
        quincunx_approximation = [bool]$QuincunxApproximation
        ac97_reset_approximation = [bool]$Ac97ResetApproximation
        audio_effects_bypass = [bool]$AudioEffectsBypass
        native_pcm_output = [bool]$NativePcmOutput
        frontend_loading_trace = [bool]$FrontendLoadingTrace
        native_gamepad_input = [bool]$NativeGamepadInput
        native_keyboard_input = [bool]$NativeKeyboardInput
        native_input_virtual_only = [bool]$NativeInputVirtualOnly
        native_input_script = $NativeInputScript
        readback_resource_cache_enabled = ($env:RECOMP_READBACK_RESOURCE_CACHE -ne '0')
        high_resolution_gpu_wait = [bool]$HighResolutionGpuWait
        vertex_shader_cache_enabled = ($childEnvironment['RECOMP_VERTEX_SHADER_CACHE'] -ne '0')
        fragment_shader_cache_enabled = ($childEnvironment['RECOMP_VERTEX_SHADER_CACHE'] -ne '0' -and $childEnvironment['RECOMP_FRAGMENT_SHADER_CACHE'] -ne '0')
        fragment_compiler_profile = ($childEnvironment['RECOMP_FRAGMENT_COMPILER_PROFILE'] -eq '1')
        graphics_stage_timing = ($childEnvironment['RECOMP_GRAPHICS_STAGE_TIMING'] -eq '1')
        capture_presented_frames = [bool]$CapturePresentedFrames
        frame_capture_interval_ms = $FrameCaptureIntervalMs
        audio_effects_bypass_max_completions = $(if ($AudioEffectsBypass) { $AudioEffectsBypassMaxCompletions } else { 0 })
        stream_callback_trace = [bool]$StreamCallbackTrace
        stream_callback_trace_local_limit_per_event = $(if ($StreamCallbackTrace) { $StreamCallbackTraceLimit } else { 0 })
        stream_wait_trace = [bool]$TraceStreamWait
        stream_completion_trace = [bool]$TraceStreamCompletion
        stream_space_trace = [bool]$TraceStreamSpace
        stream_resume_trace = [bool]$TraceStreamResume
        stream_read_trace = [bool]$TraceStreamRead
        stream_event_trace = [bool]$TraceStreamEvent
        stream_buffer_callback_trace = [bool]$TraceStreamBufferCallback
        stream_buffer_callback_detail_trace = [bool]$TraceStreamBufferCallbackDetail
        stream_scheduler_trace = [bool]$StreamSchedulerTrace
        stream_pump_trace = [bool]$TraceStreamPump
        target_stream_pump_progress_trace = [bool]$TraceStreamPump
        target_stream_release_trace = [bool]$TraceTargetStreamRelease
        target_event_table_trace = [bool]$TraceTargetEventTable
        event_wait_trace = [bool]$TraceEventWait
        stream_wait_clock_trace = [bool]$TraceStreamWaitClock
        experimental_target_stream_pump_after_status2 = [bool]$ExperimentalTargetStreamPumpAfterStatus2
        target_stream_diagnostics = [bool]$StreamCallbackTrace
        target_stream_events = [bool]$StreamCallbackTrace
        target_stream_routes = [bool]$StreamCallbackTrace
        stream_updater_diagnostics = [bool]$StreamCallbackTrace
        target_stream_manager_diagnostics = [bool]$StreamCallbackTrace
        target_stream_registration_diagnostics = [bool]$StreamCallbackTrace
        temporary_directory = $tempDirectory
        functional_status = 'NOT_READY'
        stdout = $stdout; stderr = $stderr
    }
    $summary | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $runDirectory 'run.json') -Encoding UTF8
    $summary | ConvertTo-Json | Write-Output
    Get-Content -LiteralPath $stderr -Tail 60
} finally {
    if ($null -ne $process -and -not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
    if ($null -ne $process) { $process.Dispose() }
    if ($null -ne $stdoutFile) { $stdoutFile.Dispose() }
    if ($null -ne $stderrFile) { $stderrFile.Dispose() }
}
