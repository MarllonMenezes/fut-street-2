[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'clean-room-release-invocation.ps1')
$cleanRoomTestProject = 'D:\projeto zeta\fifa_pc\FifaStreetPc.csproj'
$cleanRoomTestAssembly = 'D:\projeto zeta\fifa_pc\bin\Release\net8.0\FifaStreetPc.dll'
$cleanRoomTestPrefix = @('run', '--project', $cleanRoomTestProject, '-c', 'Release', '--no-build', '--')
$cleanRoomTestCount = 0

foreach ($cleanRoomPayload in @(
    ,@('self-test'),
    ,@('run-headless', 'D:\projeto zeta\FIFA Street 2 (USA) (En,Es).iso', '--report', 'D:\projeto zeta\áudio com espaços.tsv', '--', 'a"b')
)) {
    $cleanRoomActual = @(Get-CleanRoomReleaseInvocation -ProjectPath $cleanRoomTestProject -RuntimeAssembly $cleanRoomTestAssembly -Arguments ($cleanRoomTestPrefix + $cleanRoomPayload))
    $cleanRoomExpected = @($cleanRoomTestAssembly) + $cleanRoomPayload
    if ($cleanRoomActual.Count -ne $cleanRoomExpected.Count) { throw 'Argument count changed.' }
    for ($cleanRoomTestIndex = 0; $cleanRoomTestIndex -lt $cleanRoomExpected.Count; $cleanRoomTestIndex++) {
        if ($cleanRoomActual[$cleanRoomTestIndex] -cne $cleanRoomExpected[$cleanRoomTestIndex]) { throw 'Argument value/order changed.' }
    }
    $cleanRoomTestCount++
}

$cleanRoomInvalidCases = @()
$cleanRoomInvalidCases += ,$cleanRoomTestPrefix
foreach ($cleanRoomMutation in @(
    @{ Index = 0; Value = 'build' },
    @{ Index = 1; Value = '--file' },
    @{ Index = 2; Value = 'D:\outro\Other.csproj' },
    @{ Index = 3; Value = '--framework' },
    @{ Index = 4; Value = 'Debug' },
    @{ Index = 5; Value = '--no-restore' },
    @{ Index = 6; Value = '--launch-profile' }
)) {
    $cleanRoomChanged = $cleanRoomTestPrefix.Clone() + @('self-test')
    $cleanRoomChanged[$cleanRoomMutation.Index] = $cleanRoomMutation.Value
    $cleanRoomInvalidCases += ,$cleanRoomChanged
}
foreach ($cleanRoomInvalid in $cleanRoomInvalidCases) {
    $cleanRoomRejected = $false
    try { Get-CleanRoomReleaseInvocation -ProjectPath $cleanRoomTestProject -RuntimeAssembly $cleanRoomTestAssembly -Arguments $cleanRoomInvalid | Out-Null }
    catch { $cleanRoomRejected = $true }
    if (-not $cleanRoomRejected) { throw 'Invalid run prefix was accepted.' }
    $cleanRoomTestCount++
}
Write-Host "runner_self_test: $cleanRoomTestCount passed"
