[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GeneratedDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$GeneratedDirectory = [IO.Path]::GetFullPath($GeneratedDirectory)
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if ($GeneratedDirectory -notmatch '^[Dd]:\\' -or $OutputDirectory -notmatch '^[Dd]:\\') {
    throw 'Generated and overlay paths must be absolute paths on D:.'
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

function Replace-Unique([string]$Text, [string]$Old, [string]$New, [string]$Label) {
    $matches = 0
    $offset = 0
    while (($offset = $Text.IndexOf($Old, $offset, [StringComparison]::Ordinal)) -ge 0) {
        ++$matches
        $offset += $Old.Length
    }
    if ($matches -ne 1) {
        throw "Expected one anchor for '$Label'; found $matches. Refusing a partial overlay."
    }
    return $Text.Replace($Old, $New)
}

$sourcePath = Join-Path $GeneratedDirectory 'recomp_0048.c'
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Required generated source missing: $sourcePath"
}
$source = [IO.File]::ReadAllText($sourcePath).Replace("`r`n", "`n")
$source = Replace-Unique $source `
    "#include <math.h>`n" `
    "#include <math.h>`n#include <stdio.h>`n#include <stdlib.h>`n#include <stdatomic.h>`n" `
    'clock trace includes'

$source = Replace-Unique $source @'
void sub_0024EBA8(void)
{
'@ @'
void sub_0024EBA8(void)
{
    const uint32_t stream_wait_clock_caller = MEM32(esp);
    const int stream_wait_clock_target_context =
        stream_wait_clock_caller == 0x0022389Bu && g_ebp != 0 &&
        g_ebp <= 0x0FFFFFF7u && MEM32(g_ebp + 4u) == 0x0021E769u &&
        MEM32(g_ebp + 8u) == 0x00007480u;
    const int stream_wait_clock_trace =
        getenv("RECOMP_TRACE_STREAM_WAIT_CLOCK") != NULL &&
        (stream_wait_clock_caller == 0x0022380Au ||
         stream_wait_clock_target_context);
    uint32_t stream_wait_clock_thread = 0;
    static atomic_uint stream_wait_clock_samples;
'@ 'clock trace state'

$source = Replace-Unique $source @'
loc_0024EBA8: ;
    eax = MEM32(XBOX_FS_BASE + 0x28);
    eax = MEM32(eax + 0x12C);
    esp += 4; return; /* ret */
'@ @'
loc_0024EBA8: ;
    eax = MEM32(XBOX_FS_BASE + 0x28);
    stream_wait_clock_thread = eax;
    eax = MEM32(eax + 0x12C);
    if (stream_wait_clock_trace) {
        const unsigned int sample = atomic_fetch_add_explicit(
            &stream_wait_clock_samples, 1u, memory_order_relaxed) + 1u;
        if (sample <= 32u) {
            const uint32_t baseline = MEM32(0x34D430u);
            fprintf(stderr,
                    "[STREAM-WAIT-CLOCK] sample=%u caller=%08X target_context=%u fs=%08X thread=%08X value=%08X baseline=%08X equal=%u\n",
                    sample, stream_wait_clock_caller,
                    (unsigned)stream_wait_clock_target_context, XBOX_FS_BASE,
                    stream_wait_clock_thread, eax, baseline,
                    (unsigned)(eax == baseline));
            fflush(stderr);
        }
    }
    esp += 4; return; /* ret */
'@ 'thread clock read'

$encoding = [System.Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'recomp_0048.c'), $source, $encoding)
Write-Output $OutputDirectory
