param([string]$Workspace = (Get-Location).Path)

$ErrorActionPreference = 'Stop'
$Workspace = [IO.Path]::GetFullPath($Workspace)
$productReports = Join-Path $Workspace 'fifa_pc'
$productProject = Join-Path $productReports 'FifaStreetPc.csproj'
$productAssembly = Join-Path $productReports 'bin/Release/net8.0/FifaStreetPc.dll'
$productExecutable = Join-Path $productReports 'bin/Release/net8.0/FifaStreetPc.exe'
$productIso = Join-Path $Workspace 'FIFA Street 2 (USA) (En,Es).iso'
$expectedIsoHash = 'F47069436C3A5605C21FB9C796C5ED7155C50D761A5EDCBBD73C8DCEBFA005B2'
$productIsoBefore = Get-Item -LiteralPath $productIso
$productHashBefore = (Get-FileHash -Algorithm SHA256 -LiteralPath $productIso).Hash
if ($productHashBefore -cne $expectedIsoHash) { throw 'ISO identity differs before validation.' }

function Invoke-ProductDotnet {
    param([string[]]$ProductArguments)
    $productOutput = @(& dotnet @ProductArguments 2>&1)
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $($productOutput -join [Environment]::NewLine)" }
    $productOutput
}

function Assert-ProductBanner {
    param([object[]]$ProductOutput, [string]$ExpectedBanner)
    if (-not ($ProductOutput | Where-Object { $_.ToString().StartsWith($ExpectedBanner, [StringComparison]::Ordinal) })) {
        throw "Product banner missing: $ExpectedBanner"
    }
    if ($ProductOutput | Where-Object { $_.ToString().StartsWith('FIFA Street 2 PC', [StringComparison]::Ordinal) }) {
        throw 'Legacy public product banner remains.'
    }
}

Invoke-ProductDotnet @('build', $productProject, '-c', 'Release', '--no-restore')
$productTests = @(Invoke-ProductDotnet @($productAssembly, 'self-test'))
$productTests | Select-Object -Last 1
if (-not ($productTests -contains 'self_test: product-identity ok')) { throw 'Identity self-test missing.' }
$productVersion = (Get-Item -LiteralPath $productExecutable).VersionInfo
if ($productVersion.ProductName -cne 'Fut Street 2' -or $productVersion.FileDescription -cne 'Fut Street 2') {
    throw 'Executable product metadata differs.'
}
Write-Output "product_metadata: $($productVersion.ProductName) | $($productVersion.FileDescription)"

$productTraceOptions = @('--demo', '--clean-room-characters')
foreach ($trace in @('match', 'input', 'animation', 'audio')) {
    $productTraceOptions += @("--$trace-trace-output", (Join-Path $productReports "fut-street-identity-$trace.tsv"))
}
$productHeadless = @(Invoke-ProductDotnet (@($productAssembly, 'run-headless', $productIso, '80') + $productTraceOptions))
Assert-ProductBanner $productHeadless 'Fut Street 2 PC clean-room runtime'
Write-Output 'product_headless: renamed banner verified | frames=80'

$productReplayOptions = @('--demo', '--clean-room-characters')
foreach ($trace in @('match', 'input', 'animation', 'audio')) {
    $productReplayOptions += @("--$trace-trace-expected", (Join-Path $productReports "fut-street-identity-$trace.tsv"))
    $productReplayOptions += @("--$trace-trace-output", (Join-Path $productReports "fut-street-identity-$trace-replayed.tsv"))
}
$productReplay = @(Invoke-ProductDotnet (@($productAssembly, 'run-headless', $productIso, '80') + $productReplayOptions))
Assert-ProductBanner $productReplay 'Fut Street 2 PC clean-room runtime'
foreach ($trace in @('match', 'input', 'animation', 'audio')) {
    $productExpected = Join-Path $productReports "fut-street-identity-$trace.tsv"
    $productReplayed = Join-Path $productReports "fut-street-identity-$trace-replayed.tsv"
    if ((Get-FileHash -LiteralPath $productExpected).Hash -cne (Get-FileHash -LiteralPath $productReplayed).Hash) {
        throw "Replay differs: $trace"
    }
}
Write-Output 'product_replay: match/input/animation/audio byte-identical'

if (-not [OperatingSystem]::IsWindows()) { throw 'Windowed identity validation requires Windows.' }
$productCapture = Join-Path $productReports 'fut-street-identity-windowed.ppm'
$productWindowed = @(Invoke-ProductDotnet @($productAssembly, 'run-windowed', $productIso,
    '--clean-room-characters', '--run-seconds', '1', '--capture-frame', $productCapture))
Assert-ProductBanner $productWindowed 'Fut Street 2 PC windowed runtime'
$productPixels = [IO.File]::ReadAllBytes($productCapture)
$productHeader = [Text.Encoding]::ASCII.GetBytes("P6`n960 540`n255`n")
if ($productPixels.Length -ne $productHeader.Length + 960 * 540 * 3) { throw 'Windowed capture length differs.' }
for ($index = 0; $index -lt $productHeader.Length; $index++) {
    if ($productPixels[$index] -ne $productHeader[$index]) { throw 'Windowed capture header differs.' }
}
Write-Output 'product_windowed: renamed banner verified | capture=960x540'

$productIsoAfter = Get-Item -LiteralPath $productIso
$productHashAfter = (Get-FileHash -Algorithm SHA256 -LiteralPath $productIso).Hash
if ($productHashAfter -cne $expectedIsoHash -or $productIsoBefore.Length -ne $productIsoAfter.Length -or
    $productIsoBefore.LastWriteTimeUtc -ne $productIsoAfter.LastWriteTimeUtc -or
    $productIsoBefore.Attributes -ne $productIsoAfter.Attributes) { throw 'ISO changed during validation.' }
Write-Output "iso_sha256: $productHashAfter"
Write-Output 'product_identity_status: verified'
