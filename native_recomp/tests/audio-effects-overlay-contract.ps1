param([Parameter(Mandatory = $true)][string]$PrepareScript)
$ErrorActionPreference = 'Stop'
if ($env:TEMP -notmatch '^[Dd]:\\') { throw 'Overlay tests require TEMP on D:' }
$root = Join-Path $env:TEMP ('audio-overlay-test-' + [guid]::NewGuid().ToString('N'))
$inputDirectory = Join-Path $root 'input'
$outputDirectory = Join-Path $root 'output'
New-Item -ItemType Directory -Path $inputDirectory -Force | Out-Null
$path = Join-Path $inputDirectory 'recomp_test.c'
$source = @'
void test(void) {
    MEM32(ebx) = eax;
loc_002872FF: ;
    _fa = (uint32_t)(MEM32(ebx));
    if (CMP_NE(_fa, _fb)) goto loc_002872FF;
loc_00287304: ;
    return;
}
'@
[IO.File]::WriteAllText($path, $source)
function Get-BytesHash([string]$file) {
    $sha = [Security.Cryptography.SHA256]::Create()
    try { [BitConverter]::ToString($sha.ComputeHash([IO.File]::ReadAllBytes($file))) }
    finally { $sha.Dispose() }
}
$before = Get-BytesHash $path
function Invoke-Overlay([int]$expected) {
    $ErrorActionPreference = 'Continue'
    $result = @(& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $PrepareScript `
        -GeneratedDirectory $inputDirectory -OutputDirectory $outputDirectory 2>&1)
    $code = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if (($expected -eq 0) -ne ($code -eq 0)) { throw "Overlay exit=$code output=$result" }
}
Invoke-Overlay 0
$overlay = Join-Path $outputDirectory 'recomp_test.c'
$text = [IO.File]::ReadAllText($overlay)
if ((Get-BytesHash $path) -ne $before -or
    [regex]::Matches($text, 'fut_audio_effects_bypass\(0x002872FFu').Count -ne 1 -or
    -not $text.Contains('original-wait-exited') -or
    -not $text.Contains('if (CMP_NE(_fa, _fb)) goto loc_002872FF;')) {
    throw 'Overlay changed its input, missed hook/exit marker or removed original wait.'
}
$hash = Get-BytesHash $overlay
$stamp = (Get-Item -LiteralPath $overlay).LastWriteTimeUtc
Invoke-Overlay 0
if ((Get-BytesHash $overlay) -ne $hash -or
    (Get-Item -LiteralPath $overlay).LastWriteTimeUtc -ne $stamp) {
    throw 'Repeated overlay preparation must be stable and avoid needless rebuilds.'
}
[IO.File]::WriteAllText($path, $source.Replace('CMP_NE', 'CMP_EQ'))
Invoke-Overlay 1
if ((Get-BytesHash $overlay) -ne $hash) {
    throw 'Rejected wait shape changed previous overlay.'
}
[IO.File]::WriteAllText($path, $source)
[IO.File]::WriteAllText((Join-Path $inputDirectory 'recomp_duplicate.c'), $source)
Invoke-Overlay 1
Write-Output 'Overlay input preservation, original wait retention, deterministic output and two refusal cases passed.'
