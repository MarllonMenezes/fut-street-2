param(
    [Parameter(Mandatory = $true)]
    [string] $ExecutablePath
)

$missingXbe = Join-Path $env:TEMP 'fut-street-2-preflight-missing-default.xbe'
$output = @(& $ExecutablePath --preflight $missingXbe 2>&1)
$exitCode = $LASTEXITCODE
$message = $output -join [Environment]::NewLine

if ($exitCode -ne 1 -or $message -notmatch 'Cannot open XBE') {
    throw "The static preflight did not reject a missing XBE (exit=$exitCode). Output: $message"
}

Write-Output 'native_static_preflight: missing XBE rejected without guest execution'
