param([Parameter(Mandatory = $true)][string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$previous = $env:RECOMP_AUDIO_EFFECTS_BYPASS
$previousLimit = $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS
try {
    $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = $null
    foreach ($option in @($null, '0', '1')) {
        $env:RECOMP_AUDIO_EFFECTS_BYPASS = $option
        $case = if ($option -eq '1') { 'audio-on' } else { 'audio-off' }
        $ErrorActionPreference = 'Continue'
        $output = @(& $ExecutablePath $case 2>&1) -join "`n"
        $code = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($code -ne 0) { throw "Bypass $case failed: $code $output" }
        if (($option -eq '1') -ne $output.Contains('simulated-completion=1')) {
            throw "Unexpected bypass activation: $option $output"
        }
    }
    $env:RECOMP_AUDIO_EFFECTS_BYPASS = '1'
    $ErrorActionPreference = 'Continue'
    $output = @(& $ExecutablePath audio-repeat 2>&1) -join "`n"
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($code -ne 0 -or -not $output.Contains('application=2/4096')) {
        throw "Repeated known submission failed: $code $output"
    }
    foreach ($case in @('audio-site', 'audio-address', 'audio-value', 'audio-pointer', 'audio-budget', 'audio-invalid')) {
        $env:RECOMP_AUDIO_EFFECTS_BYPASS = if ($case -eq 'audio-invalid') { 'yes' } else { '1' }
        $ErrorActionPreference = 'Continue'
        $output = @(& $ExecutablePath $case 2>&1) -join "`n"
        $code = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($code -ne 3 -or -not $output.Contains('refused scope/configuration')) {
            throw "Bypass refusal $case failed: $code $output"
        }
        if ($case -eq 'audio-budget' -and
            ([regex]::Matches($output, 'simulated-completion=1').Count -ne 4096 -or
             -not $output.Contains('applied=4096;'))) {
            throw 'Bypass budget must allow exactly 4096, then refuse the 4097th.'
        }
    }
    $env:RECOMP_AUDIO_EFFECTS_BYPASS = '1'
    $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = '2'
    $ErrorActionPreference = 'Continue'
    $output = @(& $ExecutablePath audio-budget 2>&1) -join "`n"
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($code -ne 3 -or ([regex]::Matches($output, 'simulated-completion=1').Count -ne 2) -or
        -not $output.Contains('applied=2;')) {throw "Custom budget not enforced: $code $output"}
    $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = '16384'
    & $ExecutablePath audio-limit-upper
    if ($LASTEXITCODE -ne 0) {throw 'Upper configuration bound rejected'}
    foreach ($limit in @('0', '16385', '-1', '1x', '999999999999999999999999')) {
        $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = $limit
        $ErrorActionPreference = 'Continue'
        $output = @(& $ExecutablePath audio-on 2>&1) -join "`n"
        $code = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($code -ne 3 -or -not $output.Contains('refused scope/configuration') -or
            $output.Contains('simulated-completion=1')) {throw "Invalid limit accepted: $limit $code $output"}
    }
    Write-Output 'Audio bypass: default4096/custom2 exact refusal, bounded16384 configuration, invalid limits and original scope guards passed.'
} finally {
    $env:RECOMP_AUDIO_EFFECTS_BYPASS = $previous
    $env:RECOMP_AUDIO_EFFECTS_BYPASS_MAX_COMPLETIONS = $previousLimit
}
