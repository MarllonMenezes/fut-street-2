param(
    [Parameter(Mandatory = $true)]
    [string] $ExecutablePath
)

$previousVblank = $env:RECOMP_VBLANK
$env:RECOMP_VBLANK = '1'

try {
    $output = @(& $ExecutablePath --launch 'unused.xbe' 'unused-data' 'unused-save' 2>&1)
    $exitCode = $LASTEXITCODE
    $message = $output -join [Environment]::NewLine

    if ($exitCode -ne 2 -or $message -notmatch 'Refusing emulator or synthetic-hardware opt-in') {
        throw "The no-emulation host did not reject RECOMP_VBLANK before launch (exit=$exitCode). Output: $message"
    }

    Write-Output 'native_no_emulation_vblank_opt_in: rejected before XBE/data access'
}
finally {
    if ($null -eq $previousVblank) {
        Remove-Item Env:RECOMP_VBLANK -ErrorAction SilentlyContinue
    }
    else {
        $env:RECOMP_VBLANK = $previousVblank
    }
}
