[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GeneratedDir,
    [Parameter(Mandatory = $true)][string]$XboxRecompSourceDir,
    [Parameter(Mandatory = $true)][string]$BuildDir,
    [Parameter(Mandatory = $true)][string[]]$HostLibrary,
    [string]$CMakePath = 'cmake',
    [string]$CCompilerPath = '',
    [string]$NinjaPath = ''
)

$ErrorActionPreference = 'Stop'

function Assert-ExistingPath {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Description
    )
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description not found: $Path"
    }
}

Assert-ExistingPath -Path $GeneratedDir -Description 'Generated directory'
Assert-ExistingPath -Path $XboxRecompSourceDir -Description 'XboxRecomp source directory'
foreach ($library in $HostLibrary) {
    if ($library -match '^[A-Za-z]:[\\/]' -or $library.StartsWith('\\')) {
        Assert-ExistingPath -Path $library -Description 'Host library'
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$nativeSourceDir = Join-Path $repoRoot 'native_recomp'
Assert-ExistingPath -Path $nativeSourceDir -Description 'Native source directory'

$configureArgs = @(
    '-S', $nativeSourceDir,
    '-B', $BuildDir,
    '-G', 'Ninja',
    '-DCMAKE_BUILD_TYPE=Release',
    "-DRECOMP_GENERATED_DIR=$GeneratedDir",
    "-DRECOMP_XBOXRECOMP_SOURCE_DIR=$XboxRecompSourceDir",
    '-DFUT_STREET2_BUILD_HOST_APP=ON',
    "-DRECOMP_HOST_LIBRARIES=$($HostLibrary -join ';')"
)
if ($CCompilerPath -ne '') {
    $configureArgs += "-DCMAKE_C_COMPILER=$CCompilerPath"
}
if ($NinjaPath -ne '') {
    $configureArgs += "-DCMAKE_MAKE_PROGRAM=$NinjaPath"
}

Write-Host 'Configuring Release host link gate...'
& $CMakePath @configureArgs
if ($LASTEXITCODE -ne 0) {
    throw "CMake configure failed with exit code $LASTEXITCODE."
}

Write-Host 'Building host executable and safe test targets...'
& $CMakePath '--build' $BuildDir '--target' 'all' '--parallel'
if ($LASTEXITCODE -ne 0) {
    throw "CMake build failed with exit code $LASTEXITCODE."
}

$ctestPath = 'ctest'
if ([IO.Path]::IsPathRooted($CMakePath)) {
    $candidateCtest = Join-Path (Split-Path -Parent $CMakePath) 'ctest.exe'
    if (Test-Path -LiteralPath $candidateCtest) {
        $ctestPath = $candidateCtest
    }
}

Write-Host 'Running safe host CTest gates; no XBE launch is performed.'
& $ctestPath '--test-dir' $BuildDir '--output-on-failure'
if ($LASTEXITCODE -ne 0) {
    throw "CTest failed with exit code $LASTEXITCODE."
}

Write-Host 'host_link_gate: configure=passed build=passed ctest=passed'
