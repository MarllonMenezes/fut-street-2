param([Parameter(Mandatory = $true)][string]$ExecutablePath)
$ErrorActionPreference = 'Stop'
$previousTrap = $env:RECOMP_DIAGNOSTIC_TRAP
try {
    $env:RECOMP_DIAGNOSTIC_TRAP = '1'
    $cases = @{
        'unresolved' = '[ICALL] unresolved guest target 0x00001234'
        'not-code' = '[ICALL] target 0xFFFFFFFF is not code'
        'read' = '[IO] unsupported Xbox byte read from port 0x1234'
        'write-port' = '[IO] unsupported Xbox byte write port=0x1234 value=0x01'
        'write-value' = '[IO] unsupported Xbox byte write port=0x80C0 value=0x02'
        'unimpl' = '[UNIMPL] reached untranslated instruction at 0x00001234'
    }
    foreach ($case in $cases.Keys) {
        # Windows PowerShell turns redirected stderr into ErrorRecords.
        $ErrorActionPreference = 'Continue'
        $output = @(& $ExecutablePath $case 2>&1)
        $exitCode = $LASTEXITCODE
        $ErrorActionPreference = 'Stop'
        if ($exitCode -ne 3 -or -not ($output -join "`n").Contains($cases[$case])) {
            throw "Diagnostic $case failed: exit=$exitCode output=$output"
        }
    }
    Write-Output 'Six runtime gaps stopped with diagnostic exit 3 and their expected messages.'
} finally {
    $env:RECOMP_DIAGNOSTIC_TRAP = $previousTrap
}
