[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GeneratedDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$GeneratedDirectory = (Resolve-Path -LiteralPath $GeneratedDirectory).Path
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ($OutputDirectory -notmatch '^[Dd]:\\' -or $GeneratedDirectory -notmatch '^[Dd]:\\' -or
    $OutputDirectory.TrimEnd('\') -eq $GeneratedDirectory.TrimEnd('\')) {
    throw 'Use distinct input/output directories on D:; generated input is preserved.'
}
$candidates = @(Get-ChildItem -LiteralPath $GeneratedDirectory -Filter 'recomp_*.c' |
    Where-Object { [IO.File]::ReadAllText($_.FullName).Contains('loc_002872FF: ;') })
if ($candidates.Count -ne 1) { throw 'Expected exactly one audio-wait translation unit.' }
$source = $candidates[0]
$text = [IO.File]::ReadAllText($source.FullName)
$wait = 'loc_002872FF: ;'
$after = 'loc_00287304: ;'
foreach ($anchor in @($wait, $after)) {
    if ([regex]::Matches($text, [regex]::Escape($anchor)).Count -ne 1) {
        throw "Missing or ambiguous audio-wait anchor: $anchor"
    }
}
if ($text.Contains('fut_audio_effects_bypass') -or
    $text -notmatch 'MEM32\(ebx\) = eax;[\s\S]{0,500}loc_002872FF: ;' -or
    $text -notmatch 'loc_002872FF: ;\s+_fa = \(uint32_t\)\(MEM32\(ebx\)\)' -or
    $text -notmatch 'if \(CMP_NE\(_fa, _fb\)\) goto loc_002872FF;[^\r\n]*\s+loc_00287304: ;') {
    throw 'Unrecognized or previously instrumented wait shape; refusing overlay.'
}
$hook = @'
    /* Explicit diagnostic overlay; no changes to original generated input. */
    { extern int fut_audio_effects_bypass(uint32_t, uint32_t, volatile uint32_t *);
      fut_audio_effects_bypass(0x002872FFu, ebx, &MEM32(ebx)); }

loc_002872FF: ;
'@
$return = @'
loc_00287304: ;
    if (getenv("RECOMP_AUDIO_EFFECTS_BYPASS") &&
        strcmp(getenv("RECOMP_AUDIO_EFFECTS_BYPASS"), "1") == 0)
        fprintf(stderr, "[EXPERIMENTAL-AUDIO-BYPASS] original-wait-exited site=00287304 NOT_READY\n");
'@
$output = "#include <stdlib.h>`n#include <stdio.h>`n#include <string.h>`n" +
    $text.Replace($wait, $hook).Replace($after, $return)
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$destination = Join-Path $OutputDirectory $source.Name
$utf8 = New-Object System.Text.UTF8Encoding($false)
if (-not (Test-Path -LiteralPath $destination) -or
    [IO.File]::ReadAllText($destination) -cne $output) {
    [IO.File]::WriteAllText($destination, $output, $utf8)
}
Write-Output $destination
