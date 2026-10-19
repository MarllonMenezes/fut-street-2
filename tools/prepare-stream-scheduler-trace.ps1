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

$sourcePath = Join-Path $GeneratedDirectory 'recomp_0045.c'
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "Required generated source missing: $sourcePath"
}
$source = [IO.File]::ReadAllText($sourcePath)
$source = $source.Replace(([string][char]13 + [string][char]10), [string][char]10)

$old = @'
#include <stdio.h>
'@
$new = @'
#include <stdio.h>
#include <stdatomic.h>
'@
$source = Replace-Unique $source $old $new 'atomic diagnostic counters'

$old = @'
void sub_002398FD(void)
{
'@
$new = @'
void sub_002398FD(void)
{
    const int stream_scheduler_trace =
        getenv("RECOMP_TRACE_STREAM_SCHEDULER") != NULL;
    static atomic_uint stream_scheduler_slot_lines;
    static atomic_uint stream_scheduler_count_lines;
    const uint32_t stream_scheduler_caller = MEM32(esp);
'@
$source = Replace-Unique $source $old $new 'slot scan trace state'

$old = @'
loc_0023990C: ;
    _fa = (uint32_t)(MEM32(edx * 4 + 0x3514B8)) & 0xFFFFFFFFu;
'@
$new = @'
loc_0023990C: ;
    _fa = (uint32_t)(MEM32(edx * 4 + 0x3514B8)) & 0xFFFFFFFFu;
    if (stream_scheduler_trace && _fa != 0) {
        const uint32_t slot_state = _fa;
        const uint32_t slot_index = edx;
        const uint32_t slot_limit = ecx;
        uint32_t slot_wrapper = 0;
        uint32_t slot_object = 0;
        if (slot_state >= 0x80000000u && slot_state <= 0x9FFFFEC7u) {
            slot_wrapper = MEM32(slot_state);
            if (slot_wrapper >= 0x80000000u && slot_wrapper <= 0x9FFFFFFCu)
                slot_object = MEM32(slot_wrapper);
        }
        if ((slot_object == 0x81D73580u || slot_object == 0x82700E80u) &&
            MEM32(slot_object) == 0x4D525453u) {
            const unsigned int line = atomic_fetch_add_explicit(
                &stream_scheduler_slot_lines, 1u, memory_order_relaxed) + 1u;
            if (line <= 32u) {
                fprintf(stderr,
                        "[STREAM-SCHEDULER] phase=slot caller=%08X index=%u limit=%u state=%08X wrapper=%08X object=%08X flag11=%02X status48=%08X callback9c=%08X list120=%08X\n",
                        stream_scheduler_caller, slot_index, slot_limit, slot_state,
                        slot_wrapper, slot_object, (unsigned)ZX8(MEM8(slot_state + 0x11u)),
                        MEM32(slot_object + 0x48u), MEM32(slot_state + 0x9Cu),
                        MEM32(slot_state + 0x120u));
                fflush(stderr);
            }
        }
    }
'@
$source = Replace-Unique $source $old $new 'slot state observation'

$old = @'
loc_0023991C: ;
    esp += 4; return; /* ret */
'@
$new = @'
loc_0023991C: ;
    if (stream_scheduler_trace &&
        (stream_scheduler_caller == 0x00239D21u ||
         stream_scheduler_caller == 0x00239F5Du)) {
        const unsigned int line = atomic_fetch_add_explicit(
            &stream_scheduler_count_lines, 1u, memory_order_relaxed) + 1u;
        if (line <= 16u) {
            fprintf(stderr,
                    "[STREAM-SCHEDULER] phase=count-return caller=%08X slots=%u nonnull=%u last_index=%u\n",
                    stream_scheduler_caller, (unsigned)ZX8(MEM8(0x3515CE)),
                    eax, edx);
            fflush(stderr);
        }
    }
    esp += 4; return; /* ret */
'@
$source = Replace-Unique $source $old $new 'slot count return'

$old = @'
void sub_00239A04(void)
{
'@
$new = @'
void sub_00239A04(void)
{
    const int stream_scheduler_trace =
        getenv("RECOMP_TRACE_STREAM_SCHEDULER") != NULL;
    static atomic_uint stream_scheduler_samples;
    static atomic_uint stream_scheduler_return_lines;
    uint32_t stream_scheduler_sample = 0;
    uint32_t stream_scheduler_node = 0;
    uint32_t stream_scheduler_wrapper = 0;
    uint32_t stream_scheduler_object = 0;
    const uint32_t stream_scheduler_caller = MEM32(esp);
    const uint32_t stream_scheduler_selector = MEM32(esp + 4u);
'@
$source = Replace-Unique $source $old $new 'manager trace state'

$old = @'
loc_00239A14: ;
    esi = eax;
'@
$new = @'
loc_00239A14: ;
    esi = eax;
    if (stream_scheduler_trace && esi >= 0x80000000u &&
        esi <= 0x9FFFFEC7u) {
        const uint32_t candidate_wrapper = MEM32(esi);
        const uint32_t candidate_object =
            (candidate_wrapper >= 0x80000000u && candidate_wrapper <= 0x9FFFFFFCu)
                ? MEM32(candidate_wrapper) : 0u;
        if ((candidate_object == 0x81D73580u || candidate_object == 0x82700E80u) &&
            MEM32(candidate_object) == 0x4D525453u) {
            const unsigned int sample = atomic_fetch_add_explicit(
                &stream_scheduler_samples, 1u, memory_order_relaxed) + 1u;
            if (sample <= 16u) {
                stream_scheduler_sample = sample;
                stream_scheduler_node = esi;
                stream_scheduler_wrapper = candidate_wrapper;
                stream_scheduler_object = candidate_object;
                fprintf(stderr,
                        "[STREAM-SCHEDULER] phase=resolve sample=%u caller=%08X selector=%08X node=%08X wrapper=%08X object=%08X flag11=%02X status48=%08X callback9c=%08X list120=%08X\n",
                        sample, stream_scheduler_caller, stream_scheduler_selector,
                        stream_scheduler_node, stream_scheduler_wrapper,
                        stream_scheduler_object,
                        (unsigned)ZX8(MEM8(stream_scheduler_node + 0x11u)),
                        MEM32(stream_scheduler_object + 0x48u),
                        MEM32(stream_scheduler_node + 0x9Cu),
                        MEM32(stream_scheduler_node + 0x120u));
                fflush(stderr);
            }
        }
    }
'@
$source = Replace-Unique $source $old $new 'manager node resolution'

$old = @'
loc_00239AD6: ;
    eax = esi;
'@
$new = @'
loc_00239AD6: ;
    eax = esi;
    if (stream_scheduler_trace && stream_scheduler_sample != 0) {
        const unsigned int line = atomic_fetch_add_explicit(
            &stream_scheduler_return_lines, 1u, memory_order_relaxed) + 1u;
        if (line <= 32u) {
            fprintf(stderr,
                    "[STREAM-SCHEDULER] phase=manager-return sample=%u caller=%08X selector=%08X result=%08X node=%08X wrapper=%08X object=%08X flag11=%02X status48=%08X callback9c=%08X list120=%08X\n",
                    stream_scheduler_sample, stream_scheduler_caller,
                    stream_scheduler_selector, eax, stream_scheduler_node,
                    stream_scheduler_wrapper, stream_scheduler_object,
                    (unsigned)ZX8(MEM8(stream_scheduler_node + 0x11u)),
                    MEM32(stream_scheduler_object + 0x48u),
                    MEM32(stream_scheduler_node + 0x9Cu),
                    MEM32(stream_scheduler_node + 0x120u));
            fflush(stderr);
        }
    }
'@
$source = Replace-Unique $source $old $new 'manager return'

$old = @'
void sub_0023882D(void)
{
'@
$new = @'
void sub_0023882D(void)
{
    const int stream_virtual_trace =
        getenv("RECOMP_TRACE_STREAM_SCHEDULER") != NULL;
'@
$source = Replace-Unique $source $old $new 'virtual caller trace state'

$old = @'
loc_0023882D: ;
    PUSH32(esp, ebp);
'@
$new = @'
loc_0023882D: ;
    if (stream_virtual_trace) {
        const uint32_t caller = MEM32(esp);
        const uint32_t arg1 = MEM32(esp + 4u);
        const uint32_t arg2 = MEM32(esp + 8u);
        const uint32_t arg3 = MEM32(esp + 0xCu);
        static struct { uint32_t caller, a1, a2, a3; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == caller && seen[i].a1 == arg1 &&
                seen[i].a2 == arg2 && seen[i].a3 == arg3) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = caller;
            seen[sample].a1 = arg1;
            seen[sample].a2 = arg2;
            seen[sample].a3 = arg3;
            fprintf(stderr,
                    "[STREAM-VTABLE-CALLER] phase=entry function=0023882D sample=%u caller=%08X args=%08X/%08X/%08X\n",
                    sample + 1u, caller, arg1, arg2, arg3);
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@
$source = Replace-Unique $source $old $new 'virtual caller entry'

$old = @'
    PUSH32(esp, MEM32(eax + 0x40));
    g_ebp = ebp; /* frame stays current across calls */
    g_seh_ebp = ebp;
    PUSH32(esp, 0x00238889u); RECOMP_ABI_CALL(0x0023AF7Au, sub_0023AF7A); /* call 0x0023AF7A */
'@
$new = @'
    PUSH32(esp, MEM32(eax + 0x40));
    if (stream_virtual_trace) {
        const uint32_t call_arg1 = MEM32(esp);
        const uint32_t call_arg2 = MEM32(esp + 4u);
        const uint32_t call_arg3 = MEM32(esp + 8u);
        static struct { uint32_t a1, a2, a3, object; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].a1 == call_arg1 && seen[i].a2 == call_arg2 &&
                seen[i].a3 == call_arg3 && seen[i].object == edi) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].a1 = call_arg1;
            seen[sample].a2 = call_arg2;
            seen[sample].a3 = call_arg3;
            seen[sample].object = edi;
            fprintf(stderr,
                    "[STREAM-VTABLE-CALLER] phase=call function=0023882D sample=%u return=00238889 object=%08X args=%08X/%08X/%08X\n",
                    sample + 1u, edi, call_arg1, call_arg2, call_arg3);
            fflush(stderr);
        }
    }
    g_ebp = ebp; /* frame stays current across calls */
    g_seh_ebp = ebp;
    PUSH32(esp, 0x00238889u); RECOMP_ABI_CALL(0x0023AF7Au, sub_0023AF7A); /* call 0x0023AF7A */
'@
$source = Replace-Unique $source $old $new 'virtual caller helper call'

$encoding = [System.Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'recomp_0045.c'), $source, $encoding)
Write-Output $OutputDirectory
