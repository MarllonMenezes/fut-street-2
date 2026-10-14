[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ExecutablePath,
    [Parameter(Mandatory = $true)][string]$XbePath,
    [ValidateSet('Static', 'Runtime')][string]$Mode = 'Static',
    [string]$GameDataDirectory = '',
    [string]$SaveDirectory = ''
)

$ErrorActionPreference = 'Stop'

function Assert-ExistingPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][ValidateSet('Leaf', 'Container')][string]$PathType,
        [Parameter(Mandatory = $true)][string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path -PathType $PathType)) {
        throw "$Description not found: $Path"
    }
}

Assert-ExistingPath -Path $ExecutablePath -PathType Leaf -Description 'Host executable'
Assert-ExistingPath -Path $XbePath -PathType Leaf -Description 'XBE input'

$forbiddenVariables = @(
    @('RECOMP_AC97_READY', 'RECOMP_PB_EXEC', 'RECOMP_VBLANK') |
        Where-Object { $null -ne [Environment]::GetEnvironmentVariable($_) }
)
if ($forbiddenVariables.Count -gt 0) {
    throw "Refusing preflight with emulator or synthetic-hardware variables set: $($forbiddenVariables -join ', ')"
}

$arguments = @('--preflight', $XbePath)
if ($Mode -eq 'Runtime') {
    if ($GameDataDirectory -eq '' -or $SaveDirectory -eq '') {
        throw 'Runtime mode requires -GameDataDirectory and -SaveDirectory.'
    }

    New-Item -ItemType Directory -Force -Path $GameDataDirectory, $SaveDirectory | Out-Null
    $arguments += @($GameDataDirectory, $SaveDirectory)
}

Write-Host "Running $Mode host preflight; --launch is never used and guest code is not executed."
$output = @(& $ExecutablePath @arguments 2>&1)
$exitCode = $LASTEXITCODE
$output | Write-Output

if ($exitCode -ne 0) {
    throw "Host preflight failed with exit code $exitCode."
}

$successMarker = if ($Mode -eq 'Runtime') {
    'Runtime preflight OK: XBE memory, kernel bridge, dispatch and translated entry point resolved; no guest code executed.'
}
else {
    'Static preflight OK: XBE memory layout initialized and translated entry point resolved; no guest code executed.'
}

if (($output -join [Environment]::NewLine) -notmatch [regex]::Escape($successMarker)) {
    throw "Host preflight did not emit the expected success marker for $Mode mode."
}

Write-Host "host_preflight_gate: mode=$Mode result=passed guest_execution=none"
