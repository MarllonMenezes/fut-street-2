param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
foreach($case in @(
    @{Name='off'; Exit=0; Text=$null},
    @{Name='invalid-option'; Exit=3; Text='invalid opt-in'},
    @{Name='invalid-pointer'; Exit=3; Text='configuration scope'},
    @{Name='invalid-format'; Exit=3; Text='unsupported original WAVEFORMATEX'},
    @{Name='valid-format'; Exit=0; Text='captured original PCM'}
)) {
    $ErrorActionPreference='Continue'
    $output = & $Executable $case.Name 2>&1
    $exitCode=$LASTEXITCODE
    $ErrorActionPreference='Stop'
    if ($exitCode -ne $case.Exit) {throw "Unexpected exit for $($case.Name): $exitCode $output"}
    if ($case.Text -and ($output -join "`n") -notmatch [regex]::Escape($case.Text)) {
        throw "Missing diagnostic for $($case.Name): $output"
    }
}
Write-Output 'Native PCM opt-in and exact-format boundary: 5 cases passed; no device opened.'
