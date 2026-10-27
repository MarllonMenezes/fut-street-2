param(
    [Parameter(Mandatory = $true)]
    [string] $GeneratedDir
)

$ErrorActionPreference = 'Stop'
$dispatchPath = Join-Path $GeneratedDir 'recomp_dispatch.c'
if (-not (Test-Path -LiteralPath $dispatchPath -PathType Leaf)) {
    throw "Generated dispatch is missing: $dispatchPath"
}

$text = Get-Content -LiteralPath $dispatchPath -Raw
$entryPattern = '(?m)^\s*\{\s*0x[0-9A-Fa-f]+u,\s*\(recomp_func_t\)[A-Za-z_][A-Za-z0-9_]*\s*\},'
$entries = [regex]::Matches($text, $entryPattern)

if ($entries.Count -ne 15615) {
    throw "Expected 15615 generated dispatch entries, found $($entries.Count)"
}

# The current generation includes the fifteen recovered indirect-call seeds.
# A count alone must not hide either missing start behind an unrelated entry.
foreach ($seed in @('002A135C', '002A1D63', '00239C1A', '00227853', '0021421A', '001D864E', '001D8079', '001BF6D2', '00240AFB', '001D1EAD', '001D17BC', '000DDBE0', '000F4520', '00100160', '00050060')) {
    if ($text -notmatch "(?m)^\s*\{\s*0x${seed}u,\s*\(recomp_func_t\)sub_${seed}\s*\},") {
        throw "Recovered indirect-call seed 0x$seed is missing"
    }
}

if ($text -notmatch '(?m)^\s*\{\s*0x00011000u,\s*\(recomp_func_t\)sub_00011000\s*\},') {
    throw 'Known generated entry 0x00011000 -> sub_00011000 is missing'
}

if ($text -match '(?m)^\s*\{\s*0x00011001u,') {
    throw 'Unexpected non-start VA 0x00011001 is present in the dispatch table'
}
if ($text -match '(?m)^\s*\{\s*0x(?:000DDBF[EF]|000F459[DEF]|0010028[1-9A-F]|0005029[A-F])u,') {
    throw 'Unexpected INT3 padding VA is present in dispatch'
}

if ($text -match '(?m)^\s*\{\s*0xFFFFFFFFu,') {
    throw 'Unexpected wild VA 0xFFFFFFFF is present in the dispatch table'
}

$sizeMatch = [regex]::Match($text, 'static const size_t g_recomp_table_size = (\d+);')
if (-not $sizeMatch.Success -or [int]$sizeMatch.Groups[1].Value -ne $entries.Count) {
    throw 'Dispatch size constant does not match the generated entry count'
}

Write-Output "Generated dispatch table OK: $($entries.Count) exact VA entries; known and rejected VA cases verified."
