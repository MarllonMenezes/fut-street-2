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

function Read-GeneratedText([string]$Name) {
    $path = Join-Path $GeneratedDirectory $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required generated source missing: $path"
    }
    return [IO.File]::ReadAllText($path).Replace("`r`n", "`n")
}

$declaration = "`n#include <stdio.h>`n#include <string.h>`nextern void recomp_stream_callback_trace(uint32_t event, uint32_t a, uint32_t b, uint32_t c, uint32_t d, uint32_t e, uint32_t f);`n"
$dispatch = Read-GeneratedText 'recomp_0046.c'
$dispatch = Replace-Unique $dispatch '#include "recomp_funcs.h"' `
    ('#include "recomp_funcs.h"' + $declaration + "`nextern void fut_native_pcm_configure(uint32_t format);`nextern int fut_native_pcm_position(uint32_t ring, uint32_t frames, uint32_t rate, uint32_t *position);`nextern void fut_native_pcm_submit(uint32_t ring, uint32_t filled);`n") 'dispatcher trace declaration'

$dispatch = Replace-Unique $dispatch @'
void sub_00240BD1(void)
{
    uint32_t ebp = 0;
'@ @'
void sub_00240BD1(void)
{
    const int stream_factory_trace_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_factory_entry_return = MEM32(esp);
    const uint32_t stream_factory_entry_object_index = MEM32(esp + 4u);
    const uint32_t stream_factory_entry_type = MEM32(esp + 8u);
    const uint32_t stream_factory_entry_arg2 = MEM32(esp + 0xCu);
    const uint32_t stream_factory_entry_arg3 = MEM32(esp + 0x10u);
    const uint32_t stream_factory_entry_arg4 = MEM32(esp + 0x14u);
    const uint32_t stream_factory_entry_arg5 = MEM32(esp + 0x18u);
    const uint32_t stream_factory_entry_arg6 = MEM32(esp + 0x1Cu);
    const uint32_t stream_factory_entry_arg7 = MEM32(esp + 0x20u);
    const uint32_t stream_factory_entry_arg12 = MEM32(esp + 0x34u);
    const uint32_t stream_factory_entry_arg13 = MEM32(esp + 0x38u);
    uint32_t ebp = 0;
'@ 'stream factory entry locals'

$dispatch = Replace-Unique $dispatch @'
loc_00240BD1: ;
    PUSH32(esp, ebp);
'@ @'
loc_00240BD1: ;
    if (stream_factory_trace_enabled) {
        static uint32_t stream_factory_entry_returns[32];
        static uint32_t stream_factory_entry_objects[32];
        static uint32_t stream_factory_entry_types[32];
        static uint32_t stream_factory_entry_arg2s[32];
        static unsigned int stream_factory_entry_samples;
        int stream_factory_entry_seen = 0;
        for (unsigned int i = 0; i < stream_factory_entry_samples; ++i) {
            if (stream_factory_entry_returns[i] == stream_factory_entry_return &&
                stream_factory_entry_objects[i] == stream_factory_entry_object_index &&
                stream_factory_entry_types[i] == stream_factory_entry_type &&
                stream_factory_entry_arg2s[i] == stream_factory_entry_arg2) {
                stream_factory_entry_seen = 1;
                break;
            }
        }
        if (!stream_factory_entry_seen && stream_factory_entry_samples < 32u) {
            const unsigned int sample = stream_factory_entry_samples++;
            stream_factory_entry_returns[sample] = stream_factory_entry_return;
            stream_factory_entry_objects[sample] = stream_factory_entry_object_index;
            stream_factory_entry_types[sample] = stream_factory_entry_type;
            stream_factory_entry_arg2s[sample] = stream_factory_entry_arg2;
            fprintf(stderr,
                    "[STRM-FACTORY-ENTRY] sample=%u caller=%08X objectIndex=%08X type=%08X arg2=%08X arg3=%08X arg4=%08X arg5=%08X arg6=%08X arg7=%08X arg12=%08X arg13=%08X\n",
                    sample + 1u, stream_factory_entry_return, stream_factory_entry_object_index,
                    stream_factory_entry_type, stream_factory_entry_arg2, stream_factory_entry_arg3,
                    stream_factory_entry_arg4, stream_factory_entry_arg5, stream_factory_entry_arg6,
                    stream_factory_entry_arg7, stream_factory_entry_arg12, stream_factory_entry_arg13);
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@ 'stream factory entry trace'

$dispatch = Replace-Unique $dispatch @'
loc_0023AF7A: ;
    PUSH32(esp, edi);
'@ @'
loc_0023AF7A: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_update_caller = MEM32(esp);
        const uint32_t audio_update_arg1 = MEM32(esp + 4u);
        const uint32_t audio_update_arg2 = MEM32(esp + 8u);
        const uint32_t audio_update_arg3 = MEM32(esp + 0xCu);
        static struct { uint32_t caller, a1, a2, a3; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_update_caller && seen[i].a1 == audio_update_arg1 &&
                seen[i].a2 == audio_update_arg2 && seen[i].a3 == audio_update_arg3) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_update_caller;
            seen[sample].a1 = audio_update_arg1;
            seen[sample].a2 = audio_update_arg2;
            seen[sample].a3 = audio_update_arg3;
            fprintf(stderr, "[STRM-AUDIO-UPDATE] function=0023AF7A sample=%u caller=%08X args=%08X/%08X/%08X\n",
                    sample + 1u, audio_update_caller, audio_update_arg1, audio_update_arg2, audio_update_arg3);
            fflush(stderr);
        }
    }
    PUSH32(esp, edi);
'@ 'audio update entry 23AF7A'

$dispatch = Replace-Unique $dispatch @'
loc_0023FA33: ;
    PUSH32(esp, ebp);
'@ @'
loc_0023FA33: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_update_caller = MEM32(esp);
        const uint32_t audio_update_arg1 = MEM32(esp + 4u);
        const uint32_t audio_update_arg2 = MEM32(esp + 8u);
        const uint32_t audio_update_arg3 = MEM32(esp + 0xCu);
        static struct { uint32_t caller, a1, a2, a3; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_update_caller && seen[i].a1 == audio_update_arg1 &&
                seen[i].a2 == audio_update_arg2 && seen[i].a3 == audio_update_arg3) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_update_caller;
            seen[sample].a1 = audio_update_arg1;
            seen[sample].a2 = audio_update_arg2;
            seen[sample].a3 = audio_update_arg3;
            fprintf(stderr, "[STRM-AUDIO-UPDATE] function=0023FA33 sample=%u caller=%08X args=%08X/%08X/%08X\n",
                    sample + 1u, audio_update_caller, audio_update_arg1, audio_update_arg2, audio_update_arg3);
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@ 'audio update entry 23FA33'

$dispatch = Replace-Unique $dispatch @'
loc_0023F5EC: ;
    PUSH32(esp, ebp);
'@ @'
loc_0023F5EC: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_update_caller = MEM32(esp);
        const uint32_t audio_update_arg1 = MEM32(esp + 4u);
        const uint32_t audio_update_arg2 = MEM32(esp + 8u);
        const uint32_t audio_update_arg3 = MEM32(esp + 0xCu);
        static struct { uint32_t caller, a1, a2, a3; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_update_caller && seen[i].a1 == audio_update_arg1 &&
                seen[i].a2 == audio_update_arg2 && seen[i].a3 == audio_update_arg3) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_update_caller;
            seen[sample].a1 = audio_update_arg1;
            seen[sample].a2 = audio_update_arg2;
            seen[sample].a3 = audio_update_arg3;
            fprintf(stderr, "[STRM-AUDIO-UPDATE] function=0023F5EC sample=%u caller=%08X args=%08X/%08X/%08X\n",
                    sample + 1u, audio_update_caller, audio_update_arg1, audio_update_arg2, audio_update_arg3);
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@ 'audio update entry 23F5EC'

$dispatch = Replace-Unique $dispatch @'
loc_0024227B: ;
    PUSH32(esp, ebp);
'@ @'
loc_0024227B: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_call_caller = MEM32(esp);
        if (audio_call_caller == 0x0023F9F3u) {
            const uint32_t audio_call_arg1 = MEM32(esp + 4u);
            const uint32_t audio_call_arg2 = MEM32(esp + 8u);
            const uint32_t audio_call_arg3 = MEM32(esp + 0xCu);
            const uint32_t audio_call_arg4 = MEM32(esp + 0x10u);
            const uint32_t audio_call_arg5 = MEM32(esp + 0x14u);
            const uint32_t audio_call_arg6 = MEM32(esp + 0x18u);
            static struct { uint32_t a1, a2, a3, a4, a5, a6; } seen[32];
            static unsigned int samples;
            int duplicate = 0;
            for (unsigned int i = 0; i < samples; ++i) {
                if (seen[i].a1 == audio_call_arg1 && seen[i].a2 == audio_call_arg2 &&
                    seen[i].a3 == audio_call_arg3 && seen[i].a4 == audio_call_arg4 &&
                    seen[i].a5 == audio_call_arg5 && seen[i].a6 == audio_call_arg6) {
                    duplicate = 1;
                    break;
                }
            }
            if (!duplicate && samples < 32u) {
                const unsigned int sample = samples++;
                seen[sample].a1 = audio_call_arg1;
                seen[sample].a2 = audio_call_arg2;
                seen[sample].a3 = audio_call_arg3;
                seen[sample].a4 = audio_call_arg4;
                seen[sample].a5 = audio_call_arg5;
                seen[sample].a6 = audio_call_arg6;
                fprintf(stderr, "[STRM-AUDIO-CALL] sample=%u caller=%08X args=%08X/%08X/%08X/%08X/%08X/%08X\n",
                        sample + 1u, audio_call_caller, audio_call_arg1, audio_call_arg2,
                        audio_call_arg3, audio_call_arg4, audio_call_arg5, audio_call_arg6);
                fflush(stderr);
            }
        }
    }
    PUSH32(esp, ebp);
'@ 'audio object dispatch entry 24227B'

$dispatch = Replace-Unique $dispatch `
    "    MEM8(0x3516FB) = MEM8(0x3516FB) + 1;`n" `
    "    MEM8(0x3516FB) = MEM8(0x3516FB) + 1;`n    recomp_stream_callback_trace(1u, ecx, eax, (uint32_t)(MEM8(0x3516FB) - 1u), MEM8(0x3516FB), MEM8(0x3516F8), MEM32(eax * 4 + 0x35171C));`n" `
    'callback registration'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EDEC: ;`n    ecx = (uint32_t)(int32_t)SMEM8(0x3516FB);" `
    "loc_0023EDEC: ;`n    recomp_stream_callback_trace(2u, MEM32(esp + 4), MEM8(0x3516FB), MEM8(0x3516F8), 0, 0, 0);`n    ecx = (uint32_t)(int32_t)SMEM8(0x3516FB);" `
    'callback removal entry'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE0B: ;`n    esp += 4; return; /* ret */" `
    "loc_0023EE0B: ;`n    recomp_stream_callback_trace(4u, MEM32(esp + 4), 0xFFFFFFFFu, eax, MEM8(0x3516FB), MEM8(0x3516F8), 0);`n    esp += 4; return; /* ret */" `
    'callback removal miss'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE0C: ;`n    MEM8(0x3516FB) = MEM8(0x3516FB) - 1;" `
    "loc_0023EE0C: ;`n    MEM8(0x3516FB) = MEM8(0x3516FB) - 1;`n    recomp_stream_callback_trace(3u, MEM32(esp + 4), eax, MEM8(0x3516FB), MEM8(0x3516F8), edx, 0);" `
    'callback removal found'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE2E: ;`n    esp += 4; return; /* ret */" `
    "loc_0023EE2E: ;`n    recomp_stream_callback_trace(4u, MEM32(esp + 4), 0xFFFFFFFEu, eax, MEM8(0x3516FB), MEM8(0x3516F8), 0);`n    esp += 4; return; /* ret */" `
    'callback removal empty return'
$dispatch = Replace-Unique $dispatch `
    "loc_0023A4EC: ;`n    PUSH32(esp, 0x0023A4F1u); RECOMP_ABI_CALL(0x0023EE2Fu, sub_0023EE2F); /* call 0x0023EE2F */" `
    "loc_0023A4EC: ;`n    recomp_stream_callback_trace(5u, MEM8(0x3516FB), MEM8(0x3516F8), 0, 0, 0, 0);`n    PUSH32(esp, 0x0023A4F1u); RECOMP_ABI_CALL(0x0023EE2Fu, sub_0023EE2F); /* call 0x0023EE2F */" `
    'dispatcher caller entry'
$dispatch = Replace-Unique $dispatch `
    "loc_0023A4F1: ;`n    eax = 0; /* xor self */" `
    "loc_0023A4F1: ;`n    recomp_stream_callback_trace(6u, MEM8(0x3516FB), MEM8(0x3516F8), 0, 0, 0, 0);`n    eax = 0; /* xor self */" `
    'dispatcher caller return'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE2F: ;`n    _fa = (uint32_t)(MEM8(0x3516F8)) & 0xFFu;" `
    "loc_0023EE2F: ;`n    recomp_stream_callback_trace(7u, MEM8(0x3516F8), MEM8(0x3516FB), MEM32(0x35171C), MEM32(0x351720), 0, 0);`n    _fa = (uint32_t)(MEM8(0x3516F8)) & 0xFFu;" `
    'dispatcher entry'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE44: ;`n    { uint32_t _icall_esp = g_esp;" `
    "loc_0023EE44: ;`n    recomp_stream_callback_trace(8u, esi, MEM32(esi * 4 + 0x35171C), MEM8(0x3516FB), MEM8(0x3516F8), 0, 0);`n    { uint32_t _icall_esp = g_esp;" `
    'dispatcher callback delivery'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE4B: ;`n    eax = (uint32_t)(int32_t)SMEM8(0x3516FB);" `
    "loc_0023EE4B: ;`n    recomp_stream_callback_trace(9u, esi, MEM32(esi * 4 + 0x35171C), MEM8(0x3516FB), MEM8(0x3516F8), 0, 0);`n    eax = (uint32_t)(int32_t)SMEM8(0x3516FB);" `
    'dispatcher callback return'
$dispatch = Replace-Unique $dispatch `
    "loc_0023EE58: ;`n    esp += 4; return; /* ret */" `
    "loc_0023EE58: ;`n    recomp_stream_callback_trace(10u, MEM8(0x3516F8), MEM8(0x3516FB), 0, 0, 0, 0);`n    esp += 4; return; /* ret */" `
    'dispatcher exit'
$dispatch = Replace-Unique $dispatch @'
    { uint32_t _icall_target = MEM32(edi + 0x351C90); PUSH32(esp, 0x00240CD7u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00240CD1u); } /* indirect call */
'@ @'
    { uint32_t _icall_target = MEM32(edi + 0x351C90);
      if (stream_factory_trace_enabled) {
          const uint32_t stream_factory_dispatch_index = edi >> 2;
          const uint32_t stream_factory_dispatch_object = MEM32(esi + 0x48u);
          const uint32_t stream_factory_dispatch_arg2 = MEM32(ebp + 0x10u);
          const uint32_t stream_factory_dispatch_arg3 = MEM32(ebp + 0x14u);
          const uint32_t stream_factory_dispatch_arg7 = MEM32(ebp + 0x2Cu);
          const uint32_t stream_factory_dispatch_arg12 = MEM32(ebp + 0x38u);
          const uint32_t stream_factory_dispatch_arg13 = MEM32(ebp + 0x3Cu);
          static uint32_t stream_factory_dispatch_returns[32];
          static uint32_t stream_factory_dispatch_types[32];
          static uint32_t stream_factory_dispatch_indices[32];
          static uint32_t stream_factory_dispatch_targets[32];
          static uint32_t stream_factory_dispatch_objects[32];
          static unsigned int stream_factory_dispatch_samples;
          int stream_factory_dispatch_seen = 0;
          for (unsigned int i = 0; i < stream_factory_dispatch_samples; ++i) {
              if (stream_factory_dispatch_returns[i] == stream_factory_entry_return &&
                  stream_factory_dispatch_types[i] == stream_factory_entry_type &&
                  stream_factory_dispatch_indices[i] == stream_factory_dispatch_index &&
                  stream_factory_dispatch_targets[i] == _icall_target &&
                  stream_factory_dispatch_objects[i] == stream_factory_dispatch_object) {
                  stream_factory_dispatch_seen = 1;
                  break;
              }
          }
          if (!stream_factory_dispatch_seen && stream_factory_dispatch_samples < 32u) {
              const unsigned int sample = stream_factory_dispatch_samples++;
              stream_factory_dispatch_returns[sample] = stream_factory_entry_return;
              stream_factory_dispatch_types[sample] = stream_factory_entry_type;
              stream_factory_dispatch_indices[sample] = stream_factory_dispatch_index;
              stream_factory_dispatch_targets[sample] = _icall_target;
              stream_factory_dispatch_objects[sample] = stream_factory_dispatch_object;
              fprintf(stderr,
                      "[STRM-FACTORY-DISPATCH] sample=%u caller=%08X objectIndex=%08X type=%08X tableIndex=%08X tableByteOffset=%08X constructor=%08X destination=%08X arg2=%08X arg3=%08X arg7=%08X arg12=%08X arg13=%08X\n",
                      sample + 1u, stream_factory_entry_return, stream_factory_entry_object_index,
                      stream_factory_entry_type, stream_factory_dispatch_index, edi,
                      _icall_target, stream_factory_dispatch_object, stream_factory_dispatch_arg2,
                      stream_factory_dispatch_arg3, stream_factory_dispatch_arg7,
                      stream_factory_dispatch_arg12, stream_factory_dispatch_arg13);
              fflush(stderr);
          }
      }
      PUSH32(esp, 0x00240CD7u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x00240CD1u); } /* indirect call */
'@ 'stream factory constructor dispatch trace'
$dispatch = Replace-Unique $dispatch @'
void sub_0023C3B2(void)
{
    uint32_t ebp = 0;
'@ @'
void sub_0023C3B2(void)
{
    const int stream_async_service_trace = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_async_service_caller = MEM32(esp);
    const uint32_t stream_async_service_arg1 = MEM32(esp + 4u);
    const uint32_t stream_async_service_arg2 = MEM32(esp + 8u);
    const uint32_t stream_async_service_queue_before = MEM32(0x3518E0u);
    uint32_t ebp = 0;
    if (stream_async_service_trace) {
        static unsigned int stream_async_service_entry_samples;
        static uint32_t stream_async_service_entry_caller = 0xFFFFFFFFu;
        static uint32_t stream_async_service_entry_arg1 = 0xFFFFFFFFu;
        static uint32_t stream_async_service_entry_arg2 = 0xFFFFFFFFu;
        static uint32_t stream_async_service_entry_queue = 0xFFFFFFFFu;
        if (stream_async_service_entry_samples < 16u &&
            (stream_async_service_caller != stream_async_service_entry_caller ||
             stream_async_service_arg1 != stream_async_service_entry_arg1 ||
             stream_async_service_arg2 != stream_async_service_entry_arg2 ||
             stream_async_service_queue_before != stream_async_service_entry_queue)) {
            ++stream_async_service_entry_samples;
            fprintf(stderr,
                    "[STRM-ASYNC-SERVICE] phase=entry sample=%u caller=%08X arg1=%08X arg2=%08X queue=%08X tick=%08X pending=%08X\n",
                    stream_async_service_entry_samples, stream_async_service_caller,
                    stream_async_service_arg1, stream_async_service_arg2,
                    stream_async_service_queue_before, MEM32(0x351878u), MEM32(0x351874u));
            fflush(stderr);
            stream_async_service_entry_caller = stream_async_service_caller;
            stream_async_service_entry_arg1 = stream_async_service_arg1;
            stream_async_service_entry_arg2 = stream_async_service_arg2;
            stream_async_service_entry_queue = stream_async_service_queue_before;
        }
    }
'@ 'async service entry trace'
$dispatch = Replace-Unique $dispatch @'
loc_0023C4B6: ;
    POP32(esp, edi);
'@ @'
loc_0023C4B6: ;
    if (stream_async_service_trace) {
        static unsigned int stream_async_service_exit_samples;
        static uint32_t stream_async_service_exit_caller = 0xFFFFFFFFu;
        static uint32_t stream_async_service_exit_arg1 = 0xFFFFFFFFu;
        static uint32_t stream_async_service_exit_queue_before = 0xFFFFFFFFu;
        static uint32_t stream_async_service_exit_queue_after = 0xFFFFFFFFu;
        const uint32_t stream_async_service_queue_after = MEM32(0x3518E0u);
        if (stream_async_service_exit_samples < 16u &&
            (stream_async_service_caller != stream_async_service_exit_caller ||
             stream_async_service_arg1 != stream_async_service_exit_arg1 ||
             stream_async_service_queue_before != stream_async_service_exit_queue_before ||
             stream_async_service_queue_after != stream_async_service_exit_queue_after)) {
            ++stream_async_service_exit_samples;
            fprintf(stderr,
                    "[STRM-ASYNC-SERVICE] phase=return sample=%u caller=%08X arg1=%08X queue_before=%08X queue_after=%08X tick=%08X pending=%08X\n",
                    stream_async_service_exit_samples, stream_async_service_caller,
                    stream_async_service_arg1, stream_async_service_queue_before,
                    stream_async_service_queue_after, MEM32(0x351878u), MEM32(0x351874u));
            fflush(stderr);
            stream_async_service_exit_caller = stream_async_service_caller;
            stream_async_service_exit_arg1 = stream_async_service_arg1;
            stream_async_service_exit_queue_before = stream_async_service_queue_before;
            stream_async_service_exit_queue_after = stream_async_service_queue_after;
        }
    }
    POP32(esp, edi);
'@ 'async service return trace'
$dispatch = Replace-Unique $dispatch @'
void sub_0023E782(void)
{
    int _flags = 0; /* fallback flag var */
'@ @'
void sub_0023E782(void)
{
    const int stream_async_dispatch_trace = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_async_dispatch_caller = MEM32(esp);
    const uint32_t stream_async_dispatch_queue_before = MEM32(0x3518E0u);
    const uint32_t stream_async_dispatch_first_slot = stream_async_dispatch_queue_before != 0u
        ? ZX16(MEM16(0x3518E4u)) : 0xFFFFFFFFu;
    const uint32_t stream_async_dispatch_first_id = stream_async_dispatch_queue_before != 0u
        ? ZX16(MEM16(0x3518E6u)) : 0xFFFFFFFFu;
    uint32_t stream_async_dispatch_slot = 0u;
    uint32_t stream_async_dispatch_id = 0u;
    uint32_t stream_async_dispatch_record = 0u;
    uint32_t stream_async_dispatch_payload = 0u;
    uint32_t stream_async_dispatch_target = 0u;
    unsigned int stream_async_dispatch_sample = 0u;
    if (stream_async_dispatch_trace) {
        static unsigned int stream_async_dispatch_entry_samples;
        static uint32_t stream_async_dispatch_entry_caller = 0xFFFFFFFFu;
        static uint32_t stream_async_dispatch_entry_queue = 0xFFFFFFFFu;
        static uint32_t stream_async_dispatch_entry_slot = 0xFFFFFFFFu;
        static uint32_t stream_async_dispatch_entry_id = 0xFFFFFFFFu;
        if (stream_async_dispatch_entry_samples < 16u &&
            (stream_async_dispatch_caller != stream_async_dispatch_entry_caller ||
             stream_async_dispatch_queue_before != stream_async_dispatch_entry_queue ||
             stream_async_dispatch_first_slot != stream_async_dispatch_entry_slot ||
             stream_async_dispatch_first_id != stream_async_dispatch_entry_id)) {
            ++stream_async_dispatch_entry_samples;
            fprintf(stderr,
                    "[STRM-ASYNC-DISPATCHER] phase=entry sample=%u caller=%08X queue=%08X first_slot=%08X first_id=%08X\n",
                    stream_async_dispatch_entry_samples, stream_async_dispatch_caller,
                    stream_async_dispatch_queue_before, stream_async_dispatch_first_slot,
                    stream_async_dispatch_first_id);
            fflush(stderr);
            stream_async_dispatch_entry_caller = stream_async_dispatch_caller;
            stream_async_dispatch_entry_queue = stream_async_dispatch_queue_before;
            stream_async_dispatch_entry_slot = stream_async_dispatch_first_slot;
            stream_async_dispatch_entry_id = stream_async_dispatch_first_id;
        }
    }
    int _flags = 0; /* fallback flag var */
'@ 'async dispatcher entry trace'
$dispatch = Replace-Unique $dispatch @'
    eax = MEM32(ecx * 4 + 0x351BE4);
    PUSH32(esp, MEM32(eax + 0x5C));
'@ @'
    eax = MEM32(ecx * 4 + 0x351BE4);
    stream_async_dispatch_slot = ZX16(MEM16(esi - 4u));
    stream_async_dispatch_id = ecx;
    stream_async_dispatch_record = eax;
    stream_async_dispatch_payload = MEM32(esi);
    stream_async_dispatch_sample = 0u;
    PUSH32(esp, MEM32(eax + 0x5C));
'@ 'async dispatch event snapshot'
$dispatch = Replace-Unique $dispatch @'
loc_0023E7AA: ;
    { uint32_t _icall_esp = g_esp;
    PUSH32(esp, ecx);
    { uint32_t _icall_target = MEM32(eax + 0x58); PUSH32(esp, 0x0023E7AEu); RECOMP_ICALL_SAFE_AT_CC(_icall_target, _icall_esp, 0x0023E7ABu); } /* indirect call */
    }
'@ @'
loc_0023E7AA: ;
    { uint32_t _icall_esp = g_esp;
      stream_async_dispatch_target = MEM32(eax + 0x58u);
      if (stream_async_dispatch_trace && stream_async_dispatch_id == 6u &&
          stream_async_dispatch_record >= 0x80000000u && stream_async_dispatch_record <= 0x9FFFFF9Du) {
          const uint32_t record = stream_async_dispatch_record;
          const uint32_t count = MEM8(record + 0x62u);
          const uint32_t high = (uint32_t)(int32_t)SMEM16(record + 0x40u);
          const int32_t cursor0 = count > 0u ? (int32_t)SMEM16(record + 0x34u) : -1;
          const int32_t cursor1 = count > 1u ? (int32_t)SMEM16(record + 0x36u) : -1;
          const int32_t cursor2 = count > 2u ? (int32_t)SMEM16(record + 0x38u) : -1;
          const int32_t cursor3 = count > 3u ? (int32_t)SMEM16(record + 0x3Au) : -1;
          const int32_t cursor4 = count > 4u ? (int32_t)SMEM16(record + 0x3Cu) : -1;
          const int32_t cursor5 = count > 5u ? (int32_t)SMEM16(record + 0x3Eu) : -1;
          static unsigned int samples;
          static uint32_t last_slot = 0xFFFFFFFFu, last_id = 0xFFFFFFFFu;
          static uint32_t last_payload = 0xFFFFFFFFu, last_target = 0xFFFFFFFFu;
          static uint32_t last_count = 0xFFFFFFFFu, last_high = 0xFFFFFFFFu;
          static uint32_t last_cursors[6] = {0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu,
                                             0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu};
          if (samples < 32u &&
              (stream_async_dispatch_slot != last_slot || stream_async_dispatch_id != last_id ||
               stream_async_dispatch_payload != last_payload || stream_async_dispatch_target != last_target ||
               count != last_count || high != last_high || (uint32_t)cursor0 != last_cursors[0] ||
               (uint32_t)cursor1 != last_cursors[1] || (uint32_t)cursor2 != last_cursors[2] ||
               (uint32_t)cursor3 != last_cursors[3] || (uint32_t)cursor4 != last_cursors[4] ||
               (uint32_t)cursor5 != last_cursors[5])) {
              stream_async_dispatch_sample = ++samples;
              fprintf(stderr,
                      "[STRM-ASYNC-CALL] phase=entry sample=%u caller=0023E7AE queue_count=%08X slot=%08X id=%08X record=%08X payload=%08X target=%08X context=%08X count=%02X high=%08X cursors=%08X/%08X/%08X/%08X/%08X/%08X\n",
                      stream_async_dispatch_sample, stream_async_dispatch_queue_before,
                      stream_async_dispatch_slot, stream_async_dispatch_id, record,
                      stream_async_dispatch_payload, stream_async_dispatch_target,
                      MEM32(record + 0x5Cu), count, high, (uint32_t)cursor0,
                      (uint32_t)cursor1, (uint32_t)cursor2, (uint32_t)cursor3,
                      (uint32_t)cursor4, (uint32_t)cursor5);
              fflush(stderr);
              last_slot = stream_async_dispatch_slot;
              last_id = stream_async_dispatch_id;
              last_payload = stream_async_dispatch_payload;
              last_target = stream_async_dispatch_target;
              last_count = count;
              last_high = high;
              last_cursors[0] = (uint32_t)cursor0;
              last_cursors[1] = (uint32_t)cursor1;
              last_cursors[2] = (uint32_t)cursor2;
              last_cursors[3] = (uint32_t)cursor3;
              last_cursors[4] = (uint32_t)cursor4;
              last_cursors[5] = (uint32_t)cursor5;
          }
      }
      PUSH32(esp, ecx);
      PUSH32(esp, 0x0023E7AEu);
      RECOMP_ICALL_SAFE_AT_CC(stream_async_dispatch_target, _icall_esp, 0x0023E7ABu);
      if (stream_async_dispatch_sample != 0u) {
          const uint32_t record = stream_async_dispatch_record;
          const uint32_t count = MEM8(record + 0x62u);
          fprintf(stderr,
                  "[STRM-ASYNC-CALL] phase=return sample=%u slot=%08X id=%08X result=%08X queue_count=%08X count=%02X high=%08X cursors=%08X/%08X/%08X/%08X/%08X/%08X\n",
                  stream_async_dispatch_sample, stream_async_dispatch_slot,
                  stream_async_dispatch_id, eax, MEM32(0x3518E0u), count,
                  (uint32_t)(int32_t)SMEM16(record + 0x40u),
                  count > 0u ? (uint32_t)(int32_t)SMEM16(record + 0x34u) : 0xFFFFFFFFu,
                  count > 1u ? (uint32_t)(int32_t)SMEM16(record + 0x36u) : 0xFFFFFFFFu,
                  count > 2u ? (uint32_t)(int32_t)SMEM16(record + 0x38u) : 0xFFFFFFFFu,
                  count > 3u ? (uint32_t)(int32_t)SMEM16(record + 0x3Au) : 0xFFFFFFFFu,
                  count > 4u ? (uint32_t)(int32_t)SMEM16(record + 0x3Cu) : 0xFFFFFFFFu,
                  count > 5u ? (uint32_t)(int32_t)SMEM16(record + 0x3Eu) : 0xFFFFFFFFu);
          fflush(stderr);
      }
    }
'@ 'async callback dispatch site 58'
$dispatch = Replace-Unique $dispatch @'
loc_0023E7B3: ;
    { uint32_t _icall_esp = g_esp;
    { uint32_t _icall_target = MEM32(eax + 0x54); PUSH32(esp, 0x0023E7B6u); RECOMP_ICALL_SAFE_AT(_icall_target, _icall_esp, 0x0023E7B3u); } /* indirect call */
    }
'@ @'
loc_0023E7B3: ;
    { uint32_t _icall_esp = g_esp;
      stream_async_dispatch_target = MEM32(eax + 0x54u);
      if (stream_async_dispatch_trace && stream_async_dispatch_id == 6u &&
          stream_async_dispatch_record >= 0x80000000u && stream_async_dispatch_record <= 0x9FFFFF9Du) {
          const uint32_t record = stream_async_dispatch_record;
          const uint32_t count = MEM8(record + 0x62u);
          const uint32_t high = (uint32_t)(int32_t)SMEM16(record + 0x40u);
          static unsigned int samples;
          static uint32_t last_slot = 0xFFFFFFFFu, last_id = 0xFFFFFFFFu;
          static uint32_t last_payload = 0xFFFFFFFFu, last_target = 0xFFFFFFFFu;
          static uint32_t last_count = 0xFFFFFFFFu, last_high = 0xFFFFFFFFu;
          if (samples < 32u &&
              (stream_async_dispatch_slot != last_slot || stream_async_dispatch_id != last_id ||
               stream_async_dispatch_payload != last_payload || stream_async_dispatch_target != last_target ||
               count != last_count || high != last_high)) {
              stream_async_dispatch_sample = ++samples;
              fprintf(stderr,
                      "[STRM-ASYNC-CALL] phase=entry sample=%u caller=0023E7B3 queue_count=%08X slot=%08X id=%08X record=%08X payload=%08X target=%08X context=%08X count=%02X high=%08X\n",
                      stream_async_dispatch_sample, stream_async_dispatch_queue_before,
                      stream_async_dispatch_slot, stream_async_dispatch_id, record,
                      stream_async_dispatch_payload, stream_async_dispatch_target,
                      MEM32(record + 0x5Cu), count, high);
              fflush(stderr);
              last_slot = stream_async_dispatch_slot;
              last_id = stream_async_dispatch_id;
              last_payload = stream_async_dispatch_payload;
              last_target = stream_async_dispatch_target;
              last_count = count;
              last_high = high;
          }
      }
      PUSH32(esp, 0x0023E7B6u);
      RECOMP_ICALL_SAFE_AT(stream_async_dispatch_target, _icall_esp, 0x0023E7B3u);
      if (stream_async_dispatch_sample != 0u) {
          const uint32_t record = stream_async_dispatch_record;
          const uint32_t count = MEM8(record + 0x62u);
          fprintf(stderr,
                  "[STRM-ASYNC-CALL] phase=return sample=%u slot=%08X id=%08X result=%08X queue_count=%08X count=%02X high=%08X\n",
                  stream_async_dispatch_sample, stream_async_dispatch_slot,
                  stream_async_dispatch_id, eax, MEM32(0x3518E0u), count,
                  (uint32_t)(int32_t)SMEM16(record + 0x40u));
          fflush(stderr);
      }
    }
'@ 'async callback dispatch site 54'
$dispatch = Replace-Unique $dispatch `
    "loc_0023E7C5: ;`n    MEM32(0x3518E0) = MEM32(0x3518E0) & 0;`n    _fa = (uint32_t)(MEM32(0x3518E0)) & 0xFFFFFFFFu; _fas = (int32_t)(int32_t)(_fa); /* and result */`n    POP32(esp, edi);" `
    @"
loc_0023E7C5: ;
    MEM32(0x3518E0) = MEM32(0x3518E0) & 0;
    _fa = (uint32_t)(MEM32(0x3518E0)) & 0xFFFFFFFFu; _fas = (int32_t)(int32_t)(_fa); /* and result */
    if (stream_async_dispatch_trace) {
        static unsigned int stream_async_dispatch_exit_samples;
        static uint32_t stream_async_dispatch_exit_caller = 0xFFFFFFFFu;
        static uint32_t stream_async_dispatch_exit_queue = 0xFFFFFFFFu;
        if (stream_async_dispatch_exit_samples < 16u &&
            (stream_async_dispatch_caller != stream_async_dispatch_exit_caller ||
             stream_async_dispatch_queue_before != stream_async_dispatch_exit_queue)) {
            ++stream_async_dispatch_exit_samples;
            fprintf(stderr,
                    "[STRM-ASYNC-DISPATCHER] phase=return sample=%u caller=%08X queue_before=%08X queue_after=%08X\n",
                    stream_async_dispatch_exit_samples, stream_async_dispatch_caller,
                    stream_async_dispatch_queue_before, MEM32(0x3518E0u));
            fflush(stderr);
            stream_async_dispatch_exit_caller = stream_async_dispatch_caller;
            stream_async_dispatch_exit_queue = stream_async_dispatch_queue_before;
        }
    }
    POP32(esp, edi);
"@ `
    'async dispatcher return trace'
$dispatch = Replace-Unique $dispatch `
    "loc_0023E5AD: ;`n    eax = (uint32_t)(int32_t)SMEM16(eax + 0x40);" `
    @"
loc_0023E5AD: ;
    if (MEM32(esp + 4u) == 0x00239CA5u && MEM32(esp + 8u) == 6u && getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t credit_record = eax;
        const int credit_record_valid = credit_record >= 0x80000000u && credit_record <= 0x9FFFFF9Du;
        if (credit_record_valid) {
            const uint32_t credit_count = MEM8(credit_record + 0x62u);
            const int32_t credit_minimum = (int32_t)esi;
            const int32_t credit_high = (int32_t)SMEM16(credit_record + 0x40u);
            const int32_t credit_result = credit_high - credit_minimum - 1;
            const int32_t credit_slot0 = credit_count > 0u ? (int32_t)SMEM16(credit_record + 0x34u) : 0;
            const int32_t credit_slot1 = credit_count > 1u ? (int32_t)SMEM16(credit_record + 0x36u) : 0;
            const int32_t credit_slot2 = credit_count > 2u ? (int32_t)SMEM16(credit_record + 0x38u) : 0;
            const int32_t credit_slot3 = credit_count > 3u ? (int32_t)SMEM16(credit_record + 0x3Au) : 0;
            const uint32_t credit_field48 = MEM32(credit_record + 0x48u);
            const uint32_t credit_field4c = MEM32(credit_record + 0x4Cu);
            const uint32_t credit_field50 = MEM32(credit_record + 0x50u);
            const uint32_t credit_field5c = MEM32(credit_record + 0x5Cu);
            static unsigned int credit_samples;
            static uint32_t credit_last_count = 0xFFFFFFFFu, credit_last_min = 0xFFFFFFFFu;
            static uint32_t credit_last_high = 0xFFFFFFFFu, credit_last_field48 = 0xFFFFFFFFu;
            static uint32_t credit_last_field4c = 0xFFFFFFFFu, credit_last_field50 = 0xFFFFFFFFu;
            static uint32_t credit_last_field5c = 0xFFFFFFFFu;
            if (credit_result == 0 && credit_samples < 32u &&
                (credit_count != credit_last_count || (uint32_t)credit_minimum != credit_last_min ||
                 (uint32_t)credit_high != credit_last_high || credit_field48 != credit_last_field48 ||
                 credit_field4c != credit_last_field4c || credit_field50 != credit_last_field50 ||
                 credit_field5c != credit_last_field5c)) {
                ++credit_samples;
                fprintf(stderr,
                        "[STRM-CB-CREDIT] sample=%u caller=%08X index=%08X record=%08X count62=%08X minimum=%08X high40=%08X result=%08X slot0=%08X slot1=%08X slot2=%08X slot3=%08X field48=%08X field4c=%08X field50=%08X field5c=%08X\n",
                        credit_samples, MEM32(esp + 4u), MEM32(esp + 8u), credit_record,
                        credit_count, (uint32_t)credit_minimum, (uint32_t)credit_high,
                        (uint32_t)credit_result, (uint32_t)credit_slot0, (uint32_t)credit_slot1,
                        (uint32_t)credit_slot2, (uint32_t)credit_slot3, credit_field48,
                        credit_field4c, credit_field50, credit_field5c);
                fflush(stderr);
                credit_last_count = credit_count;
                credit_last_min = (uint32_t)credit_minimum;
                credit_last_high = (uint32_t)credit_high;
                credit_last_field48 = credit_field48;
                credit_last_field4c = credit_field4c;
                credit_last_field50 = credit_field50;
                credit_last_field5c = credit_field5c;
            }
        }
    }
    eax = (uint32_t)(int32_t)SMEM16(eax + 0x40);
"@ `
    'target stream credit operands'

$dispatch = Replace-Unique $dispatch `
    "void sub_0023D17F(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_0023D17F(void)
{
    const int audio_scheduler_trace_enabled =
        getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t audio_scheduler_trace_caller = audio_scheduler_trace_enabled ? MEM32(esp) : 0u;
    uint32_t ebp = 0;
"@ `
    'audio scheduler entry state'

$dispatch = Replace-Unique $dispatch @'
loc_0023D17F: ;
    PUSH32(esp, ebp);
'@ @'
loc_0023D17F: ;
    if (audio_scheduler_trace_enabled) {
        const uint32_t audio_scheduler_list0 = MEM32(0x3517F8u);
        const uint32_t audio_scheduler_list1 = MEM32(0x351804u);
        static struct { uint32_t caller, list0, list1; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_scheduler_trace_caller &&
                seen[i].list0 == audio_scheduler_list0 && seen[i].list1 == audio_scheduler_list1) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_scheduler_trace_caller;
            seen[sample].list0 = audio_scheduler_list0;
            seen[sample].list1 = audio_scheduler_list1;
            fprintf(stderr,
                    "[STRM-AUDIO-SCHEDULER] phase=entry sample=%u caller=%08X list3517F8=%08X list351804=%08X\n",
                    sample + 1u, audio_scheduler_trace_caller,
                    audio_scheduler_list0, audio_scheduler_list1);
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@ 'audio scheduler entry trace'

$dispatch = Replace-Unique $dispatch @'
loc_0023D1B2: ;
    _fa = (uint32_t)(MEM32(esi + 0x20)) & 0xFFFFFFFFu;
'@ @'
loc_0023D1B2: ;
    if (audio_scheduler_trace_enabled) {
        const uint32_t audio_scheduler_list = MEM32(ebp - 4u);
        const uint32_t audio_scheduler_index = ZX16(MEM16(esi + 0xCu));
        const uint32_t audio_scheduler_state20 = MEM32(esi + 0x20u);
        const uint32_t audio_scheduler_state8 = MEM32(esi + 8u);
        const uint32_t audio_scheduler_guard = ((int32_t)audio_scheduler_state20 >= 0) ? 1u : 0u;
        static struct { uint32_t caller, list, object, index, state20; } seen[32];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_scheduler_trace_caller &&
                seen[i].list == audio_scheduler_list && seen[i].object == esi &&
                seen[i].index == audio_scheduler_index && seen[i].state20 == audio_scheduler_state20) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 32u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_scheduler_trace_caller;
            seen[sample].list = audio_scheduler_list;
            seen[sample].object = esi;
            seen[sample].index = audio_scheduler_index;
            seen[sample].state20 = audio_scheduler_state20;
            fprintf(stderr,
                    "[STRM-AUDIO-SCHEDULER] phase=guard sample=%u caller=%08X list=%08X object=%08X index=%08X field8=%08X field20=%08X passes=%u\n",
                    sample + 1u, audio_scheduler_trace_caller, audio_scheduler_list,
                    esi, audio_scheduler_index, audio_scheduler_state8,
                    audio_scheduler_state20, audio_scheduler_guard);
            fflush(stderr);
        }
    }
    _fa = (uint32_t)(MEM32(esi + 0x20)) & 0xFFFFFFFFu;
'@ 'audio scheduler signed guard'

$dispatch = Replace-Unique $dispatch @'
loc_0023D1EC: ;
    PUSH32(esp, esi);
'@ @'
loc_0023D1EC: ;
    if (audio_scheduler_trace_enabled) {
        const uint32_t audio_scheduler_list = MEM32(ebp - 4u);
        const uint32_t audio_scheduler_index = ZX16(MEM16(esi + 0xCu));
        const uint32_t audio_scheduler_state20 = MEM32(esi + 0x20u);
        static struct { uint32_t caller, list, object, index, state20; } seen[32];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_scheduler_trace_caller &&
                seen[i].list == audio_scheduler_list && seen[i].object == esi &&
                seen[i].index == audio_scheduler_index && seen[i].state20 == audio_scheduler_state20) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 32u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_scheduler_trace_caller;
            seen[sample].list = audio_scheduler_list;
            seen[sample].object = esi;
            seen[sample].index = audio_scheduler_index;
            seen[sample].state20 = audio_scheduler_state20;
            fprintf(stderr,
                    "[STRM-AUDIO-SCHEDULER] phase=dispatch sample=%u caller=%08X list=%08X object=%08X index=%08X field20=%08X\n",
                    sample + 1u, audio_scheduler_trace_caller, audio_scheduler_list,
                    esi, audio_scheduler_index, audio_scheduler_state20);
            fflush(stderr);
        }
    }
    PUSH32(esp, esi);
'@ 'audio scheduler consumer dispatch'

$dispatch = Replace-Unique $dispatch `
    "void sub_0023E5EC(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_0023E5EC(void)
{
    const int stream_consumer_probe_env = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_consumer_probe_return = MEM32(esp);
    const uint32_t stream_consumer_probe_index = MEM32(esp + 4u);
    const uint32_t stream_consumer_probe_slot = MEM32(esp + 8u);
    const uint32_t stream_consumer_probe_aux = MEM32(esp + 0xCu);
    const int stream_consumer_probe_caller =
        stream_consumer_probe_return == 0x0023FCADu || stream_consumer_probe_return == 0x002416DDu ||
        stream_consumer_probe_return == 0x002491F7u || stream_consumer_probe_return == 0x00249532u ||
        stream_consumer_probe_return == 0x002499DCu;
    const int stream_consumer_probe_enabled = stream_consumer_probe_env && stream_consumer_probe_caller;
    const int stream_consumer_probe_index_valid = stream_consumer_probe_enabled &&
        stream_consumer_probe_index < 256u;
    const uint32_t stream_consumer_probe_record = stream_consumer_probe_index_valid
        ? MEM32(stream_consumer_probe_index * 4u + 0x351BE4u) : 0u;
    const int stream_consumer_probe_record_valid = stream_consumer_probe_index_valid &&
        stream_consumer_probe_record >= 0x80000000u &&
        stream_consumer_probe_record <= 0x9FFFFF7Fu;
    const uint32_t stream_consumer_probe_count = stream_consumer_probe_record_valid
        ? MEM8(stream_consumer_probe_record + 0x62u) : 0u;
    const uint32_t stream_consumer_probe_cursor = stream_consumer_probe_record_valid &&
        stream_consumer_probe_slot < stream_consumer_probe_count && stream_consumer_probe_slot < 24u
        ? (uint32_t)(int32_t)SMEM16(stream_consumer_probe_record + 0x34u + stream_consumer_probe_slot * 2u)
        : 0xFFFFFFFFu;
    const uint32_t stream_consumer_probe_high = stream_consumer_probe_record_valid
        ? (uint32_t)(int32_t)SMEM16(stream_consumer_probe_record + 0x40u) : 0xFFFFFFFFu;
    const uint32_t stream_consumer_probe_q8 = stream_consumer_probe_record_valid
        ? MEM32(stream_consumer_probe_record + 8u) : 0u;
    const uint32_t stream_consumer_probe_qc = stream_consumer_probe_record_valid
        ? MEM32(stream_consumer_probe_record + 0xCu) : 0u;
    if (stream_consumer_probe_enabled) {
        static uint32_t stream_consumer_probe_returns[32];
        static uint32_t stream_consumer_probe_indices[32];
        static unsigned int stream_consumer_probe_samples;
        int stream_consumer_probe_seen = 0;
        for (unsigned int i = 0; i < stream_consumer_probe_samples; ++i) {
            if (stream_consumer_probe_returns[i] == stream_consumer_probe_return &&
                stream_consumer_probe_indices[i] == stream_consumer_probe_index) {
                stream_consumer_probe_seen = 1;
                break;
            }
        }
        if (!stream_consumer_probe_seen && stream_consumer_probe_samples < 32u) {
            const unsigned int sample = stream_consumer_probe_samples++;
            stream_consumer_probe_returns[sample] = stream_consumer_probe_return;
            stream_consumer_probe_indices[sample] = stream_consumer_probe_index;
            fprintf(stderr,
                    "[STRM-RING-CONSUMER-ENTRY] sample=%u caller=%08X index=%08X slot=%08X aux=%08X record=%08X count62=%08X cursor=%08X high40=%08X q8=%08X qC=%08X\n",
                    sample + 1u, stream_consumer_probe_return, stream_consumer_probe_index,
                    stream_consumer_probe_slot, stream_consumer_probe_aux, stream_consumer_probe_record,
                    stream_consumer_probe_count, stream_consumer_probe_cursor, stream_consumer_probe_high,
                    stream_consumer_probe_q8, stream_consumer_probe_qc);
            fflush(stderr);
        }
    }
    const int stream_consumer_enabled =
        getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL && MEM32(esp + 4u) == 6u;
    const uint32_t stream_consumer_return = MEM32(esp);
    const uint32_t stream_consumer_index = MEM32(esp + 4u);
    const uint32_t stream_consumer_slot = MEM32(esp + 8u);
    const uint32_t stream_consumer_aux = MEM32(esp + 0xCu);
    const uint32_t stream_consumer_record = stream_consumer_enabled
        ? MEM32(stream_consumer_index * 4u + 0x351BE4u) : 0u;
    const int stream_consumer_record_valid = stream_consumer_record >= 0x80000000u &&
        stream_consumer_record <= 0x9FFFFF7Fu;
    const uint32_t stream_consumer_count_before = stream_consumer_record_valid
        ? MEM8(stream_consumer_record + 0x62u) : 0u;
    const uint32_t stream_consumer_slot_before = stream_consumer_record_valid &&
        stream_consumer_slot < stream_consumer_count_before && stream_consumer_slot < 24u
        ? (uint32_t)(int32_t)SMEM16(stream_consumer_record + 0x34u + stream_consumer_slot * 2u)
        : 0xFFFFFFFFu;
    const uint32_t stream_consumer_q8_before = stream_consumer_record_valid
        ? MEM32(stream_consumer_record + 8u) : 0u;
    const uint32_t stream_consumer_qc_before = stream_consumer_record_valid
        ? MEM32(stream_consumer_record + 0xCu) : 0u;
    const uint32_t stream_consumer_read_before = stream_consumer_record_valid
        ? MEM16(stream_consumer_record + 0x44u) : 0u;
    const uint32_t stream_consumer_write_before = stream_consumer_record_valid
        ? MEM16(stream_consumer_record + 0x46u) : 0u;
    const uint32_t stream_consumer_high_before = stream_consumer_record_valid
        ? MEM16(stream_consumer_record + 0x40u) : 0u;
    uint32_t ebp = 0;
"@ `
    'target stream ring consumer entry'
$dispatch = Replace-Unique $dispatch `
    "loc_0023E73D: ;`n    POP32(esp, edi);" `
    @"
loc_0023E73D: ;
    if (stream_consumer_enabled && stream_consumer_record_valid) {
        const uint32_t stream_consumer_result = eax;
        const uint32_t stream_consumer_count_after = MEM8(stream_consumer_record + 0x62u);
        const uint32_t stream_consumer_slot_after = stream_consumer_slot < stream_consumer_count_after &&
            stream_consumer_slot < 24u
            ? (uint32_t)(int32_t)SMEM16(stream_consumer_record + 0x34u + stream_consumer_slot * 2u)
            : 0xFFFFFFFFu;
        const uint32_t stream_consumer_q8_after = MEM32(stream_consumer_record + 8u);
        const uint32_t stream_consumer_qc_after = MEM32(stream_consumer_record + 0xCu);
        const uint32_t stream_consumer_read_after = MEM16(stream_consumer_record + 0x44u);
        const uint32_t stream_consumer_write_after = MEM16(stream_consumer_record + 0x46u);
        const uint32_t stream_consumer_high_after = MEM16(stream_consumer_record + 0x40u);
        static unsigned int stream_consumer_samples;
        static uint32_t stream_consumer_last_caller = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_slot = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_result = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_cursor_before = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_cursor_after = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_q8 = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_qc = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_read = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_write = 0xFFFFFFFFu;
        static uint32_t stream_consumer_last_high = 0xFFFFFFFFu;
        if (stream_consumer_samples < 32u &&
            (stream_consumer_return != stream_consumer_last_caller ||
             stream_consumer_slot != stream_consumer_last_slot ||
             stream_consumer_result != stream_consumer_last_result ||
             stream_consumer_slot_before != stream_consumer_last_cursor_before ||
             stream_consumer_slot_after != stream_consumer_last_cursor_after ||
             stream_consumer_q8_after != stream_consumer_last_q8 ||
             stream_consumer_qc_after != stream_consumer_last_qc ||
             stream_consumer_read_after != stream_consumer_last_read ||
             stream_consumer_write_after != stream_consumer_last_write ||
             stream_consumer_high_after != stream_consumer_last_high)) {
            ++stream_consumer_samples;
            fprintf(stderr,
                    "[STRM-RING-CONSUMER] sample=%u caller=%08X index=%08X slot=%08X aux=%08X record=%08X result=%08X cursor=%08X->%08X count62=%08X high40=%08X q8=%08X->%08X qC=%08X->%08X read44=%08X->%08X write46=%08X->%08X\n",
                    stream_consumer_samples, stream_consumer_return, stream_consumer_index,
                    stream_consumer_slot, stream_consumer_aux, stream_consumer_record,
                    stream_consumer_result, stream_consumer_slot_before, stream_consumer_slot_after,
                    stream_consumer_count_after, stream_consumer_high_after,
                    stream_consumer_q8_before, stream_consumer_q8_after,
                    stream_consumer_qc_before, stream_consumer_qc_after,
                    stream_consumer_read_before, stream_consumer_read_after,
                    stream_consumer_write_before, stream_consumer_write_after);
            fflush(stderr);
            stream_consumer_last_caller = stream_consumer_return;
            stream_consumer_last_slot = stream_consumer_slot;
            stream_consumer_last_result = stream_consumer_result;
            stream_consumer_last_cursor_before = stream_consumer_slot_before;
            stream_consumer_last_cursor_after = stream_consumer_slot_after;
            stream_consumer_last_q8 = stream_consumer_q8_after;
            stream_consumer_last_qc = stream_consumer_qc_after;
            stream_consumer_last_read = stream_consumer_read_after;
            stream_consumer_last_write = stream_consumer_write_after;
            stream_consumer_last_high = stream_consumer_high_after;
        }
    }
    POP32(esp, edi);
"@ `
    'target stream ring consumer outcome'

$dispatch = Replace-Unique $dispatch `
    "void sub_0023FC59(void)`n{`n    uint32_t ebp = 0;`n    ebp = g_ebp;" `
    @"
void sub_0023FC59(void)
{
    const int stream_read_trace_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_read_object = stream_read_trace_enabled ? MEM32(esp + 4u) : 0u;
    const int stream_read_object_valid = stream_read_object >= 0x80000000u &&
        stream_read_object <= 0x9FFFFF2Bu;
    const uint32_t stream_read_index = stream_read_object_valid
        ? MEM32(stream_read_object + 0x2Cu) : 0xFFFFFFFFu;
    const int stream_read_target = stream_read_trace_enabled &&
        stream_read_object_valid && stream_read_index == 6u;
    const uint32_t stream_read_slot = stream_read_target
        ? ZX8(MEM8(stream_read_object + 0x35u)) : 0xFFFFFFFFu;
    const uint32_t stream_read_field1c = stream_read_target ? MEM32(stream_read_object + 0x1Cu) : 0u;
    const uint32_t stream_read_cursor20 = stream_read_target ? MEM32(stream_read_object + 0x20u) : 0u;
    const uint32_t stream_read_end24 = stream_read_target ? MEM32(stream_read_object + 0x24u) : 0u;
    const uint32_t stream_read_pending28 = stream_read_target ? MEM32(stream_read_object + 0x28u) : 0u;
    const uint32_t stream_read_field30 = stream_read_target ? MEM32(stream_read_object + 0x30u) : 0u;
    const uint32_t stream_read_arg8 = stream_read_target ? MEM32(esp + 8u) : 0u;
    const uint32_t stream_read_argc = stream_read_object_valid ? MEM32(esp + 0xCu) : 0u;
    const uint32_t stream_read_arg10 = stream_read_target ? MEM32(esp + 0x10u) : 0u;
    const uint32_t stream_read_arg14 = stream_read_target ? MEM32(esp + 0x14u) : 0u;
    uint32_t ebp = 0;
    if (stream_read_trace_enabled && stream_read_object_valid && stream_read_index < 256u) {
        static uint32_t stream_read_seen_indices[8];
        static unsigned int stream_read_distinct_samples;
        const uint32_t stream_read_word = stream_read_index >> 5;
        const uint32_t stream_read_bit = 1u << (stream_read_index & 31u);
        if (stream_read_distinct_samples < 16u &&
            (stream_read_seen_indices[stream_read_word] & stream_read_bit) == 0u) {
            stream_read_seen_indices[stream_read_word] |= stream_read_bit;
            ++stream_read_distinct_samples;
            fprintf(stderr,
                    "[STRM-READ-METHOD] sample=%u object=%08X index=%08X slot35=%08X field1c=%08X cursor20=%08X end24=%08X pending28=%08X last30=%08X argc=%08X\\n",
                    stream_read_distinct_samples, stream_read_object, stream_read_index,
                    (uint32_t)ZX8(MEM8(stream_read_object + 0x35u)),
                    MEM32(stream_read_object + 0x1Cu), MEM32(stream_read_object + 0x20u),
                    MEM32(stream_read_object + 0x24u), MEM32(stream_read_object + 0x28u),
                    MEM32(stream_read_object + 0x30u), stream_read_argc);
            fflush(stderr);
        }
    }
    if (stream_read_target) {
        static unsigned int stream_read_entry_samples;
        static uint32_t stream_read_last_object = 0xFFFFFFFFu;
        static uint32_t stream_read_last_slot = 0xFFFFFFFFu;
        static uint32_t stream_read_last_field1c = 0xFFFFFFFFu;
        static uint32_t stream_read_last_cursor20 = 0xFFFFFFFFu;
        static uint32_t stream_read_last_end24 = 0xFFFFFFFFu;
        static uint32_t stream_read_last_pending28 = 0xFFFFFFFFu;
        static uint32_t stream_read_prev30 = 0xFFFFFFFFu;
        static uint32_t stream_read_last_argc = 0xFFFFFFFFu;
        if (stream_read_entry_samples < 32u &&
            (stream_read_object != stream_read_last_object || stream_read_slot != stream_read_last_slot ||
             stream_read_field1c != stream_read_last_field1c || stream_read_cursor20 != stream_read_last_cursor20 ||
             stream_read_end24 != stream_read_last_end24 || stream_read_pending28 != stream_read_last_pending28 ||
             stream_read_field30 != stream_read_prev30 || stream_read_argc != stream_read_last_argc)) {
            ++stream_read_entry_samples;
            fprintf(stderr,
                    "[STRM-READ-SLOT6] phase=entry sample=%u object=%08X index=%08X slot35=%08X field1c=%08X cursor20=%08X end24=%08X pending28=%08X last30=%08X args=%08X/%08X/%08X/%08X\n",
                    stream_read_entry_samples, stream_read_object, stream_read_index, stream_read_slot,
                    stream_read_field1c, stream_read_cursor20, stream_read_end24, stream_read_pending28,
                    stream_read_field30, stream_read_arg8, stream_read_argc, stream_read_arg10, stream_read_arg14);
            fflush(stderr);
            stream_read_last_object = stream_read_object;
            stream_read_last_slot = stream_read_slot;
            stream_read_last_field1c = stream_read_field1c;
            stream_read_last_cursor20 = stream_read_cursor20;
            stream_read_last_end24 = stream_read_end24;
            stream_read_last_pending28 = stream_read_pending28;
            stream_read_prev30 = stream_read_field30;
            stream_read_last_argc = stream_read_argc;
        }
    }
    ebp = g_ebp;
"@ `
    'target stream read-method entry'
$dispatch = Replace-Unique $dispatch `
    "loc_0023FC9B: ;`n    eax = ebp + 8;" `
    @"
loc_0023FC9B: ;
    if (stream_read_target) {
        static unsigned int stream_read_call_samples;
        static uint32_t stream_read_call_last_cursor = 0xFFFFFFFFu;
        static uint32_t stream_read_call_last_end = 0xFFFFFFFFu;
        static uint32_t stream_read_call_last_amount = 0xFFFFFFFFu;
        static uint32_t stream_read_call_last_pending = 0xFFFFFFFFu;
        const uint32_t stream_read_call_cursor = MEM32(stream_read_object + 0x20u);
        const uint32_t stream_read_call_end = MEM32(stream_read_object + 0x24u);
        const uint32_t stream_read_call_amount = MEM32(ebp + 0xCu);
        const uint32_t stream_read_call_pending = MEM32(stream_read_object + 0x28u);
        if (stream_read_call_samples < 32u &&
            (stream_read_call_cursor != stream_read_call_last_cursor ||
             stream_read_call_end != stream_read_call_last_end ||
             stream_read_call_amount != stream_read_call_last_amount ||
             stream_read_call_pending != stream_read_call_last_pending)) {
            ++stream_read_call_samples;
            fprintf(stderr,
                    "[STRM-READ-SLOT6] phase=consumer-call sample=%u object=%08X index=%08X slot35=%08X amount=%08X cursor20=%08X end24=%08X pending28=%08X local_cursor=%08X\n",
                    stream_read_call_samples, stream_read_object, stream_read_index, stream_read_slot,
                    stream_read_call_amount, stream_read_call_cursor, stream_read_call_end,
                    stream_read_call_pending, MEM32(edi));
            fflush(stderr);
            stream_read_call_last_cursor = stream_read_call_cursor;
            stream_read_call_last_end = stream_read_call_end;
            stream_read_call_last_amount = stream_read_call_amount;
            stream_read_call_last_pending = stream_read_call_pending;
        }
    }
    eax = ebp + 8;
"@ `
    'target stream read-method consumer call'

$dispatch = Replace-Unique $dispatch `
    "void sub_00241656(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_00241656(void)
{
    const int stream_audio_trace_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_audio_object = stream_audio_trace_enabled ? MEM32(esp + 4u) : 0u;
    const int stream_audio_object_valid = stream_audio_object >= 0x80000000u &&
        stream_audio_object <= 0x9FFFFF2Bu;
    const uint32_t stream_audio_id = stream_audio_object_valid
        ? MEM32(stream_audio_object + 0x20u) : 0xFFFFFFFFu;
    const uint32_t stream_audio_count = stream_audio_object_valid
        ? MEM8(stream_audio_object + 0x23u) : 0u;
    const uint32_t stream_audio_field8 = stream_audio_object_valid
        ? MEM32(stream_audio_object + 8u) : 0u;
    const uint32_t stream_audio_field14 = stream_audio_object_valid
        ? MEM32(stream_audio_object + 0x14u) : 0u;
    const uint32_t stream_audio_field18 = stream_audio_object_valid
        ? MEM32(stream_audio_object + 0x18u) : 0u;
    const uint32_t stream_audio_field1c = stream_audio_object_valid
        ? MEM32(stream_audio_object + 0x1Cu) : 0u;
    uint32_t ebp = 0;
    if (stream_audio_trace_enabled && stream_audio_object_valid) {
        static unsigned int stream_audio_entry_samples;
        static uint32_t stream_audio_last_object = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_id = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_count = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_field8 = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_field14 = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_field18 = 0xFFFFFFFFu;
        static uint32_t stream_audio_last_field1c = 0xFFFFFFFFu;
        if (stream_audio_entry_samples < 32u &&
            (stream_audio_object != stream_audio_last_object || stream_audio_id != stream_audio_last_id ||
             stream_audio_count != stream_audio_last_count || stream_audio_field8 != stream_audio_last_field8 ||
             stream_audio_field14 != stream_audio_last_field14 || stream_audio_field18 != stream_audio_last_field18 ||
             stream_audio_field1c != stream_audio_last_field1c)) {
            ++stream_audio_entry_samples;
            fprintf(stderr,
                    "[STRM-AUDIO-CONSUMER] phase=entry sample=%u object=%08X id20=%08X count23=%08X field8=%08X field14=%08X field18=%08X field1c=%08X\n",
                    stream_audio_entry_samples, stream_audio_object, stream_audio_id, stream_audio_count,
                    stream_audio_field8, stream_audio_field14, stream_audio_field18, stream_audio_field1c);
            fflush(stderr);
            stream_audio_last_object = stream_audio_object;
            stream_audio_last_id = stream_audio_id;
            stream_audio_last_count = stream_audio_count;
            stream_audio_last_field8 = stream_audio_field8;
            stream_audio_last_field14 = stream_audio_field14;
            stream_audio_last_field18 = stream_audio_field18;
            stream_audio_last_field1c = stream_audio_field1c;
        }
    }
"@ `
    'audio stream consumer entry'

$dispatch = Replace-Unique $dispatch `
    "loc_002416CA: ;`n    eax = ebp + -32;" `
    @"
loc_002416CA: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL &&
        esi >= 0x80000000u && esi <= 0x9FFFFF2Bu) {
        const uint32_t audio_consumer_id = MEM32(esi + 0x20u);
        const uint32_t audio_consumer_loop_slot = MEM32(ebp + 8u);
        const int32_t audio_consumer_entry = (int32_t)SMEM16(edi);
        const uint32_t audio_consumer_record = audio_consumer_id < 256u
            ? MEM32(audio_consumer_id * 4u + 0x351BE4u) : 0u;
        const int audio_consumer_record_valid = audio_consumer_record >= 0x80000000u &&
            audio_consumer_record <= 0x9FFFFF7Fu;
        const uint32_t audio_consumer_count = audio_consumer_record_valid
            ? MEM8(audio_consumer_record + 0x62u) : 0u;
        const int32_t audio_consumer_high = audio_consumer_record_valid
            ? (int32_t)SMEM16(audio_consumer_record + 0x40u) : 0;
        const int32_t audio_consumer_cursor0 = audio_consumer_record_valid && audio_consumer_count > 0u
            ? (int32_t)SMEM16(audio_consumer_record + 0x34u) : -1;
        const int32_t audio_consumer_cursor1 = audio_consumer_record_valid && audio_consumer_count > 1u
            ? (int32_t)SMEM16(audio_consumer_record + 0x36u) : -1;
        const int32_t audio_consumer_cursor2 = audio_consumer_record_valid && audio_consumer_count > 2u
            ? (int32_t)SMEM16(audio_consumer_record + 0x38u) : -1;
        const int32_t audio_consumer_cursor3 = audio_consumer_record_valid && audio_consumer_count > 3u
            ? (int32_t)SMEM16(audio_consumer_record + 0x3Au) : -1;
        const int32_t audio_consumer_cursor4 = audio_consumer_record_valid && audio_consumer_count > 4u
            ? (int32_t)SMEM16(audio_consumer_record + 0x3Cu) : -1;
        const int32_t audio_consumer_cursor5 = audio_consumer_record_valid && audio_consumer_count > 5u
            ? (int32_t)SMEM16(audio_consumer_record + 0x3Eu) : -1;
        static uint32_t audio_consumer_seen_ids[8];
        static unsigned int audio_consumer_other_samples;
        static unsigned int audio_consumer_target_samples;
        static uint32_t audio_consumer_last_target_object = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_slot = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_entry = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_count = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_high = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor0 = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor1 = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor2 = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor3 = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor4 = 0xFFFFFFFFu;
        static uint32_t audio_consumer_last_target_cursor5 = 0xFFFFFFFFu;
        const int audio_consumer_target = audio_consumer_id == 6u;
        int audio_consumer_emit = 0;
        if (audio_consumer_target && audio_consumer_target_samples < 16u &&
            (esi != audio_consumer_last_target_object || audio_consumer_loop_slot != audio_consumer_last_target_slot ||
             (uint32_t)audio_consumer_entry != audio_consumer_last_target_entry ||
             audio_consumer_count != audio_consumer_last_target_count ||
             (uint32_t)audio_consumer_high != audio_consumer_last_target_high ||
             (uint32_t)audio_consumer_cursor0 != audio_consumer_last_target_cursor0 ||
             (uint32_t)audio_consumer_cursor1 != audio_consumer_last_target_cursor1 ||
             (uint32_t)audio_consumer_cursor2 != audio_consumer_last_target_cursor2 ||
             (uint32_t)audio_consumer_cursor3 != audio_consumer_last_target_cursor3 ||
             (uint32_t)audio_consumer_cursor4 != audio_consumer_last_target_cursor4 ||
             (uint32_t)audio_consumer_cursor5 != audio_consumer_last_target_cursor5)) {
            ++audio_consumer_target_samples;
            audio_consumer_emit = 1;
            audio_consumer_last_target_object = esi;
            audio_consumer_last_target_slot = audio_consumer_loop_slot;
            audio_consumer_last_target_entry = (uint32_t)audio_consumer_entry;
            audio_consumer_last_target_count = audio_consumer_count;
            audio_consumer_last_target_high = (uint32_t)audio_consumer_high;
            audio_consumer_last_target_cursor0 = (uint32_t)audio_consumer_cursor0;
            audio_consumer_last_target_cursor1 = (uint32_t)audio_consumer_cursor1;
            audio_consumer_last_target_cursor2 = (uint32_t)audio_consumer_cursor2;
            audio_consumer_last_target_cursor3 = (uint32_t)audio_consumer_cursor3;
            audio_consumer_last_target_cursor4 = (uint32_t)audio_consumer_cursor4;
            audio_consumer_last_target_cursor5 = (uint32_t)audio_consumer_cursor5;
        } else if (audio_consumer_id < 256u && !audio_consumer_target &&
                   audio_consumer_other_samples < 16u) {
            const uint32_t audio_consumer_word = audio_consumer_id >> 5;
            const uint32_t audio_consumer_bit = 1u << (audio_consumer_id & 31u);
            if ((audio_consumer_seen_ids[audio_consumer_word] & audio_consumer_bit) == 0u) {
                audio_consumer_seen_ids[audio_consumer_word] |= audio_consumer_bit;
                ++audio_consumer_other_samples;
                audio_consumer_emit = 1;
            }
        }
        if (audio_consumer_emit) {
            fprintf(stderr,
                    "[STRM-AUDIO-CONSUMER] phase=ring-call object=%08X id=%08X loop_slot=%08X entry=%08X record=%08X count=%08X high=%08X cursors=%08X/%08X/%08X/%08X/%08X/%08X\n",
                    esi, audio_consumer_id, audio_consumer_loop_slot, (uint32_t)audio_consumer_entry,
                    audio_consumer_record, audio_consumer_count, (uint32_t)audio_consumer_high,
                    (uint32_t)audio_consumer_cursor0, (uint32_t)audio_consumer_cursor1,
                    (uint32_t)audio_consumer_cursor2, (uint32_t)audio_consumer_cursor3,
                    (uint32_t)audio_consumer_cursor4, (uint32_t)audio_consumer_cursor5);
            fflush(stderr);
        }
    }
    eax = ebp + -32;
"@ `
    'audio stream consumer callsite'

$stream_methods = Read-GeneratedText 'recomp_0047.c'
$stream_methods = Replace-Unique $stream_methods `
    "void sub_00249179(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_00249179(void)
{
    const int stream_read_method_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_read_method_return = stream_read_method_enabled ? MEM32(esp) : 0u;
    const uint32_t stream_read_method_object = stream_read_method_enabled ? MEM32(esp + 4u) : 0u;
    const uint32_t stream_read_method_arg1 = stream_read_method_enabled ? MEM32(esp + 8u) : 0u;
    const uint32_t stream_read_method_arg2 = stream_read_method_enabled ? MEM32(esp + 0xCu) : 0u;
    const uint32_t stream_read_method_arg3 = stream_read_method_enabled ? MEM32(esp + 0x10u) : 0u;
    const int stream_read_method_object_valid = stream_read_method_enabled &&
        stream_read_method_object >= 0x80000000u && stream_read_method_object <= 0x9FFFFF27u;
    const uint32_t stream_read_method_index = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x24u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_slot = stream_read_method_object_valid ? MEM8(stream_read_method_object + 0x38u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_state = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x20u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field28 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x28u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field2c = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x2Cu) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field34 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x34u) : 0xFFFFFFFFu;
    if (stream_read_method_enabled) {
        static struct { uint32_t ret, object, index, slot, state, field28, field2c, field34; } seen[32];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].ret == stream_read_method_return && seen[i].object == stream_read_method_object &&
                seen[i].index == stream_read_method_index && seen[i].slot == stream_read_method_slot &&
                seen[i].state == stream_read_method_state && seen[i].field28 == stream_read_method_field28 &&
                seen[i].field2c == stream_read_method_field2c && seen[i].field34 == stream_read_method_field34) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 32u) {
            const unsigned int sample = samples++;
            seen[sample].ret = stream_read_method_return;
            seen[sample].object = stream_read_method_object;
            seen[sample].index = stream_read_method_index;
            seen[sample].slot = stream_read_method_slot;
            seen[sample].state = stream_read_method_state;
            seen[sample].field28 = stream_read_method_field28;
            seen[sample].field2c = stream_read_method_field2c;
            seen[sample].field34 = stream_read_method_field34;
            fprintf(stderr, "[STRM-READ-METHOD] function=00249179 sample=%u caller=%08X object=%08X args=%08X/%08X/%08X index24=%08X slot38=%08X state20=%08X field28=%08X field2c=%08X field34=%08X\n",
                    sample + 1u, stream_read_method_return, stream_read_method_object,
                    stream_read_method_arg1, stream_read_method_arg2, stream_read_method_arg3,
                    stream_read_method_index, stream_read_method_slot, stream_read_method_state,
                    stream_read_method_field28, stream_read_method_field2c, stream_read_method_field34);
            fflush(stderr);
        }
    }
    uint32_t ebp = 0;
"@ `
    'indirect stream read method 49179 entry'

$stream_methods = Replace-Unique $stream_methods `
    "void sub_002494B4(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_002494B4(void)
{
    const int stream_read_method_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_read_method_return = stream_read_method_enabled ? MEM32(esp) : 0u;
    const uint32_t stream_read_method_object = stream_read_method_enabled ? MEM32(esp + 4u) : 0u;
    const uint32_t stream_read_method_arg1 = stream_read_method_enabled ? MEM32(esp + 8u) : 0u;
    const uint32_t stream_read_method_arg2 = stream_read_method_enabled ? MEM32(esp + 0xCu) : 0u;
    const uint32_t stream_read_method_arg3 = stream_read_method_enabled ? MEM32(esp + 0x10u) : 0u;
    const int stream_read_method_object_valid = stream_read_method_enabled &&
        stream_read_method_object >= 0x80000000u && stream_read_method_object <= 0x9FFFFF2Bu;
    const uint32_t stream_read_method_index = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x30u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_slot = stream_read_method_object_valid ? MEM8(stream_read_method_object + 0x38u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_state = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x24u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field20 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x20u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field28 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x28u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field34 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x34u) : 0xFFFFFFFFu;
    if (stream_read_method_enabled) {
        static struct { uint32_t ret, object, index, slot, state, field20, field28, field34; } seen[32];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].ret == stream_read_method_return && seen[i].object == stream_read_method_object &&
                seen[i].index == stream_read_method_index && seen[i].slot == stream_read_method_slot &&
                seen[i].state == stream_read_method_state && seen[i].field20 == stream_read_method_field20 &&
                seen[i].field28 == stream_read_method_field28 && seen[i].field34 == stream_read_method_field34) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 32u) {
            const unsigned int sample = samples++;
            seen[sample].ret = stream_read_method_return;
            seen[sample].object = stream_read_method_object;
            seen[sample].index = stream_read_method_index;
            seen[sample].slot = stream_read_method_slot;
            seen[sample].state = stream_read_method_state;
            seen[sample].field20 = stream_read_method_field20;
            seen[sample].field28 = stream_read_method_field28;
            seen[sample].field34 = stream_read_method_field34;
            fprintf(stderr, "[STRM-READ-METHOD] function=002494B4 sample=%u caller=%08X object=%08X args=%08X/%08X/%08X index30=%08X slot38=%08X state24=%08X field20=%08X field28=%08X field34=%08X\n",
                    sample + 1u, stream_read_method_return, stream_read_method_object,
                    stream_read_method_arg1, stream_read_method_arg2, stream_read_method_arg3,
                    stream_read_method_index, stream_read_method_slot, stream_read_method_state,
                    stream_read_method_field20, stream_read_method_field28, stream_read_method_field34);
            fflush(stderr);
        }
    }
    uint32_t ebp = 0;
"@ `
    'indirect stream read method 494B4 entry'

$stream_methods = Replace-Unique $stream_methods `
    "void sub_00249995(void)`n{`n    uint32_t ebp = 0;" `
    @"
void sub_00249995(void)
{
    const int stream_read_method_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_read_method_return = stream_read_method_enabled ? MEM32(esp) : 0u;
    const uint32_t stream_read_method_object = stream_read_method_enabled ? MEM32(esp + 4u) : 0u;
    const uint32_t stream_read_method_arg1 = stream_read_method_enabled ? MEM32(esp + 8u) : 0u;
    const uint32_t stream_read_method_arg2 = stream_read_method_enabled ? MEM32(esp + 0xCu) : 0u;
    const uint32_t stream_read_method_arg3 = stream_read_method_enabled ? MEM32(esp + 0x10u) : 0u;
    const int stream_read_method_object_valid = stream_read_method_enabled &&
        stream_read_method_object >= 0x80000000u && stream_read_method_object <= 0x9FFFFF2Fu;
    const uint32_t stream_read_method_index = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x28u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_slot = stream_read_method_object_valid ? MEM8(stream_read_method_object + 0x31u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_state = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x20u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field24 = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x24u) : 0xFFFFFFFFu;
    const uint32_t stream_read_method_field2c = stream_read_method_object_valid ? MEM32(stream_read_method_object + 0x2Cu) : 0xFFFFFFFFu;
    if (stream_read_method_enabled) {
        static struct { uint32_t ret, object, index, slot, state, field24, field2c; } seen[32];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].ret == stream_read_method_return && seen[i].object == stream_read_method_object &&
                seen[i].index == stream_read_method_index && seen[i].slot == stream_read_method_slot &&
                seen[i].state == stream_read_method_state && seen[i].field24 == stream_read_method_field24 &&
                seen[i].field2c == stream_read_method_field2c) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 32u) {
            const unsigned int sample = samples++;
            seen[sample].ret = stream_read_method_return;
            seen[sample].object = stream_read_method_object;
            seen[sample].index = stream_read_method_index;
            seen[sample].slot = stream_read_method_slot;
            seen[sample].state = stream_read_method_state;
            seen[sample].field24 = stream_read_method_field24;
            seen[sample].field2c = stream_read_method_field2c;
            fprintf(stderr, "[STRM-READ-METHOD] function=00249995 sample=%u caller=%08X object=%08X args=%08X/%08X/%08X index28=%08X slot31=%08X state20=%08X field24=%08X field2c=%08X\n",
                    sample + 1u, stream_read_method_return, stream_read_method_object,
                    stream_read_method_arg1, stream_read_method_arg2, stream_read_method_arg3,
                    stream_read_method_index, stream_read_method_slot, stream_read_method_state,
                    stream_read_method_field24, stream_read_method_field2c);
            fflush(stderr);
        }
    }
    uint32_t ebp = 0;
"@ `
    'indirect stream read method 49995 entry'

$dispatch = Replace-Unique $dispatch @'
loc_00240D9C: ;
    MEM8(eax) = 2;
'@ @'
loc_00240D9C: ;
    MEM8(eax) = 2;
    {
        const char *option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int samples;
        if (option && strcmp(option, "1") == 0 && samples < 16u) {
            fprintf(stderr, "[STRM-CHANNEL-ACTIVATED] sample=%u caller=%08X channel=%08X state=%u head40=%08X count=%u\n",
                    ++samples, MEM32(esp + 4u), eax, (unsigned)MEM8(eax),
                    MEM32(eax + 0x40u), (unsigned)MEM8(0x351C80u));
            fflush(stderr);
        }
    }
'@ 'reader channel activation completed'

$dispatch = Replace-Unique $dispatch @'
loc_0024108F: ;
    PUSH32(esp, ebp);
'@ @'
loc_0024108F: ;
    {
        const char *option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int samples;
        if (option && strcmp(option, "1") == 0 && samples < 8u) {
            fprintf(stderr, "[STRM-MIXER-ENTRY] sample=%u caller=%08X arg1=%08X requested=%08X count=%u table=%08X\n",
                    ++samples, MEM32(esp), MEM32(esp + 4u), MEM32(esp + 8u),
                    (unsigned)MEM8(0x351C80u), MEM32(0x351E58u));
            fflush(stderr);
        }
    }
    PUSH32(esp, ebp);
'@ 'reader mixer entry'

$dispatch = Replace-Unique $dispatch @'
loc_0024117A: ;
    ecx = MEM32(esi + 0x40);
'@ @'
loc_0024117A: ;
    ecx = MEM32(esi + 0x40);
    {
        const char *option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int samples;
        if (option && strcmp(option, "1") == 0 && samples < 16u) {
            fprintf(stderr, "[STRM-MIXER-CALL] sample=%u caller=%08X channel=%08X state=%u remaining=%08X head=%08X target=%08X\n",
                    ++samples, MEM32(ebp + 4u), esi, (unsigned)MEM8(esi),
                    eax, ecx, MEM32(ecx));
            fflush(stderr);
        }
    }
'@ 'reader mixer chain call'

$dispatch = Replace-Unique $dispatch @'
loc_0023C4D3: ;
    eax = MEM32(ebp + -4);
'@ @'
loc_0023C4D3: ;
    {
        uint32_t native_write_position = 0u;
        if (fut_native_pcm_position(MEM32(0x351850u), MEM32(0x351868u),
                                    MEM16(0x3515B6u), &native_write_position))
            MEM32(ebp - 4u) = native_write_position;
        const char *option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int calls;
        if (option && strcmp(option, "1") == 0 && calls < 1048576u) {
            ++calls;
            if (calls <= 8u || (calls & (calls - 1u)) == 0u) {
                fprintf(stderr, "[STRM-AUDIO-POSITION] call=%u caller=%08X object=%08X result=%08X position_bytes=%08X ring_samples=%08X lead_samples=%08X filled_samples=%08X\n",
                        calls, MEM32(ebp + 4u), MEM32(0x351838u), eax,
                        MEM32(ebp - 4u), MEM32(0x351868u), MEM32(0x35186Cu),
                        MEM32(0x351870u));
                const uint32_t position_object = MEM32(0x351838u);
                const uint32_t voice = position_object >= 0x10000u && position_object <= 0x9FFFFFF7u
                    ? MEM32(position_object + 4u) : 0u;
                if (voice >= 0x10000u && voice <= 0x9FFFFEABu) {
                    const uint32_t dma = MEM32(0x28D5E8u);
                    const unsigned int selector = MEM8(voice + 0x64u);
                    const unsigned int voice_id = MEM16(voice + 0xCu);
                    const unsigned int selected_id = selector < 4u ? MEM16(voice + 0xAu + selector * 2u) : 0xFFFFu;
                    const int dma_valid = dma >= 0x10000u && dma <= 0x9FFF7FFFu;
                    const uint32_t format = MEM32(voice + 0x70u);
                    const uint32_t buffer = MEM32(voice + 0x80u);
                    const int format_valid = format >= 0x10000u && format <= 0x9FFFFFE7u;
                    const int buffer_valid = buffer >= 0x10000u && buffer <= 0x9FFFFF2Bu;
                    fprintf(stderr, "[STRM-AUDIO-PRODUCER] call=%u voice=%08X flags12=%04X saved148=%08X selector=%u id=%u selected=%u dma=%08X status54=%08X raw58=%08X format=%08X tag0c=%04X stride14=%08X buffer=%08X minimum14=%08X bytesc0=%08X loopcc=%08X/%08X\n",
                            calls, voice, (unsigned)MEM16(voice + 0x12u), MEM32(voice + 0x148u),
                            selector, voice_id, selected_id, dma,
                            dma_valid && selected_id < 256u ? MEM32(dma + selected_id * 128u + 0x54u) : 0xFFFFFFFFu,
                            dma_valid && voice_id < 256u ? MEM32(dma + voice_id * 128u + 0x58u) : 0xFFFFFFFFu,
                            format, format_valid ? (unsigned)MEM16(format + 0xCu) : 0xFFFFu,
                            format_valid ? MEM32(format + 0x14u) : 0xFFFFFFFFu,
                            buffer, buffer_valid ? MEM32(buffer + 0x14u) : 0xFFFFFFFFu,
                            buffer_valid ? MEM32(buffer + 0xC0u) : 0xFFFFFFFFu,
                            buffer_valid ? MEM32(buffer + 0xCCu) : 0xFFFFFFFFu,
                            buffer_valid ? MEM32(buffer + 0xD0u) : 0xFFFFFFFFu);
                }
                fflush(stderr);
            }
        }
    }
    eax = MEM32(ebp + -4);
'@ 'audio position query result'

$dispatch = Replace-Unique $dispatch @'
loc_0023C2F2: ;
'@ @'
loc_0023C2F2: ;
    fut_native_pcm_configure(ebp - 40u);
'@ 'native PCM original format'

$dispatch = Replace-Unique $dispatch @'
loc_0023C548: ;
    POP32(esp, edi);
    MEM32(0x351870) = esi;
'@ @'
loc_0023C548: ;
    POP32(esp, edi);
    MEM32(0x351870) = esi;
    fut_native_pcm_submit(MEM32(0x351850u), MEM32(0x351870u));
'@ 'native PCM submit original refill'

$dispatch = Replace-Unique $dispatch @'
loc_0023C513: ;
    MEM32(ebp + -16) = ecx;
'@ @'
loc_0023C513: ;
    MEM32(ebp + -16) = ecx;
    {
        const char *option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int calls;
        if (option && strcmp(option, "1") == 0 && calls < 1048576u) {
            ++calls;
            if (calls <= 8u || (calls & (calls - 1u)) == 0u) {
                fprintf(stderr, "[STRM-AUDIO-DEMAND] call=%u position_samples=%08X target=%08X filled=%08X range0=%08X range1=%08X\n",
                        calls, MEM32(ebp - 4u), esi, MEM32(0x351870u),
                        ecx, MEM32(ebp - 12u));
                fflush(stderr);
            }
        }
    }
'@ 'audio refill demand ranges'

$stream_methods = Replace-Unique $stream_methods @'
loc_00249638: ;
    ecx = MEM32(ebp + -12);
    MEM32(esi + 0x1C) = eax;
'@ @'
loc_00249638: ;
    ecx = MEM32(ebp + -12);
    MEM32(esi + 0x1C) = eax;
    {
        const char *reader_ctor_option = getenv("RECOMP_STREAM_CALLBACK_TRACE");
        static unsigned int reader_ctor_samples;
        if (reader_ctor_option && strcmp(reader_ctor_option, "1") == 0 &&
            reader_ctor_samples < 16u) {
            const uint32_t reader_index = MEM32(esi + 0x30u);
            const uint32_t reader_record = reader_index == 6u
                ? MEM32(0x351BE4u + 6u * 4u) : 0u;
            fprintf(stderr,
                    "[STRM-READER-CONSTRUCTED] sample=%u caller=%08X object=%08X descriptor=%08X reader=%08X index=%08X slot=%u state24=%08X allocation1c=%08X record_for_index6=%08X\n",
                    ++reader_ctor_samples, MEM32(ebp + 4u), esi, edi,
                    MEM32(esi), reader_index, (unsigned)MEM8(esi + 0x38u),
                    MEM32(esi + 0x24u), MEM32(esi + 0x1Cu), reader_record);
            fflush(stderr);
        }
    }
'@ 'reader constructor completed association'

$callback = Read-GeneratedText 'recomp_callback_00239C1A.c'
$callback = Replace-Unique $callback '#include "recomp_funcs.h"' `
    ('#include "recomp_funcs.h"' + $declaration) 'callback trace declaration'
$callback = Replace-Unique $callback `
    "loc_00239C1A: ;`n    PUSH32(esp, ebp);" `
    "loc_00239C1A: ;`n    recomp_stream_callback_trace(11u, MEM8(0x3516FB), MEM8(0x3516F8), MEM8(0x3515CE), MEM32(esp), 0, 0);`n    PUSH32(esp, ebp);" `
    'callback entry'
$callback = Replace-Unique $callback `
    "    ebx = MEM32(ebp * 4 + 0x3514B8);`n    _fa = (uint32_t)(ebx) & 0xFFFFFFFFu;" `
    "    ebx = MEM32(ebp * 4 + 0x3514B8);`n    recomp_stream_callback_trace(12u, ebp, ebx, (ebx != 0) ? MEM32(ebx + 0x128) : 0u, (ebx != 0) ? MEM8(ebx + 0x10) : 0u, MEM8(0x3516F8), MEM8(0x3515CE));`n    _fa = (uint32_t)(ebx) & 0xFFFFFFFFu;" `
    'callback node guards'
$callback = Replace-Unique $callback `
    "loc_00239C92: ;`n    _fa = (uint32_t)(eax) & 0xFFFFFFFFu;" `
    "loc_00239C92: ;`n    recomp_stream_callback_trace(13u, ebp, ebx, eax, MEM32(ebx + 0x128), MEM8(ebx + 0x10), MEM8(0x3516F8));`n    _fa = (uint32_t)(eax) & 0xFFFFFFFFu;" `
    'sub_0023988D return'
$callback = Replace-Unique $callback `
    "loc_00239CA5: ;`n    esi = eax;`n    _fa = (uint32_t)(esi) & 0xFFFFFFFFu;" `
    @"
loc_00239CA5: ;
    esi = eax;
    if (ebx == 0x81D72E00u && (int32_t)esi <= 0 && getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t cbready_argument = MEM32(ebx + 8u);
        const uint32_t cbready_wrapper = MEM32(ebx);
        const int cbready_wrapper_valid = cbready_wrapper >= 0x80000000u && cbready_wrapper <= 0x9FFFFFECu;
        const uint32_t cbready_queue8 = cbready_wrapper_valid ? MEM32(cbready_wrapper + 8u) : 0u;
        const uint32_t cbready_queueC = cbready_wrapper_valid ? MEM32(cbready_wrapper + 0xCu) : 0u;
        const uint32_t cbready_status48 = MEM32(0x81D735C8u);
        const uint32_t cbready_head60 = MEM32(0x81D735E0u);
        const uint32_t cbready_tail64 = MEM32(0x81D735E4u);
        const uint32_t cbready_avail68 = MEM32(0x81D735E8u);
        const uint32_t cbready_pos180 = MEM32(0x81D73700u);
        const uint32_t cbready_pending184 = MEM32(0x81D73704u);
        const uint32_t cbready_parser = MEM32(ebx + 0x138u);
        const int cbready_parser_valid = cbready_parser >= 0x80000000u && cbready_parser <= 0x9FFFFFDAu;
        const uint32_t cbready_parser_cursor = cbready_parser_valid ? MEM32(cbready_parser + 0x1Cu) : 0u;
        static unsigned int cbready_samples;
        static uint32_t cbready_last_argument = 0xFFFFFFFFu, cbready_last_result = 0xFFFFFFFFu;
        static uint32_t cbready_last_queue8 = 0xFFFFFFFFu, cbready_last_queueC = 0xFFFFFFFFu;
        static uint32_t cbready_last_status48 = 0xFFFFFFFFu, cbready_last_head60 = 0xFFFFFFFFu;
        static uint32_t cbready_last_tail64 = 0xFFFFFFFFu, cbready_last_avail68 = 0xFFFFFFFFu;
        static uint32_t cbready_last_pos180 = 0xFFFFFFFFu, cbready_last_pending184 = 0xFFFFFFFFu;
        static uint32_t cbready_last_parser = 0xFFFFFFFFu;
        if (cbready_samples < 32u &&
            (cbready_argument != cbready_last_argument || (uint32_t)esi != cbready_last_result ||
             cbready_queue8 != cbready_last_queue8 || cbready_queueC != cbready_last_queueC ||
             cbready_status48 != cbready_last_status48 || cbready_head60 != cbready_last_head60 ||
             cbready_tail64 != cbready_last_tail64 || cbready_avail68 != cbready_last_avail68 ||
             cbready_pos180 != cbready_last_pos180 || cbready_pending184 != cbready_last_pending184 ||
             cbready_parser_cursor != cbready_last_parser)) {
            ++cbready_samples;
            fprintf(stderr,
                    "[STRM-CB-READY-GATE] sample=%u index=%08X node=%08X argument=%08X result=%08X wrapper=%08X queue8=%08X queueC=%08X status48=%08X head60=%08X tail64=%08X avail68=%08X pos180=%08X pending184=%08X parser=%08X parser_cursor=%08X\n",
                    cbready_samples, ebp, ebx, cbready_argument, (uint32_t)esi, cbready_wrapper,
                    cbready_queue8, cbready_queueC, cbready_status48, cbready_head60,
                    cbready_tail64, cbready_avail68, cbready_pos180, cbready_pending184,
                    cbready_parser, cbready_parser_cursor);
            fflush(stderr);
            cbready_last_argument = cbready_argument;
            cbready_last_result = (uint32_t)esi;
            cbready_last_queue8 = cbready_queue8;
            cbready_last_queueC = cbready_queueC;
            cbready_last_status48 = cbready_status48;
            cbready_last_head60 = cbready_head60;
            cbready_last_tail64 = cbready_tail64;
            cbready_last_avail68 = cbready_avail68;
            cbready_last_pos180 = cbready_pos180;
            cbready_last_pending184 = cbready_pending184;
            cbready_last_parser = cbready_parser_cursor;
        }
    }
    _fa = (uint32_t)(esi) & 0xFFFFFFFFu;
"@ `
    'target callback readiness result'
$callback = Replace-Unique $callback `
    "loc_00239CBB: ;`n    _fa = (uint32_t)(eax) & 0xFFFFFFFFu;" `
    @"
loc_00239CBB: ;
    if (eax == 0 && ebx == 0x81D72E00u && getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t cbmiss_wrapper = MEM32(ebx);
        const int cbmiss_wrapper_valid = cbmiss_wrapper >= 0x80000000u && cbmiss_wrapper <= 0x9FFFFFECu;
        const uint32_t cbmiss_queue8 = cbmiss_wrapper_valid ? MEM32(cbmiss_wrapper + 8u) : 0u;
        const uint32_t cbmiss_queueC = cbmiss_wrapper_valid ? MEM32(cbmiss_wrapper + 0xCu) : 0u;
        const uint32_t cbmiss_status48 = MEM32(0x81D735C8u);
        const uint32_t cbmiss_head60 = MEM32(0x81D735E0u);
        const uint32_t cbmiss_tail64 = MEM32(0x81D735E4u);
        const uint32_t cbmiss_avail68 = MEM32(0x81D735E8u);
        const uint32_t cbmiss_pos180 = MEM32(0x81D73700u);
        const uint32_t cbmiss_pending184 = MEM32(0x81D73704u);
        const uint32_t cbmiss_parser = MEM32(ebx + 0x138u);
        const int cbmiss_parser_valid = cbmiss_parser >= 0x80000000u && cbmiss_parser <= 0x9FFFFFDAu;
        const uint32_t cbmiss_parser_cursor = cbmiss_parser_valid ? MEM32(cbmiss_parser + 0x1Cu) : 0u;
        static unsigned int cbmiss_samples;
        static uint32_t cbmiss_last_loop = 0xFFFFFFFFu, cbmiss_last_queue8 = 0xFFFFFFFFu;
        static uint32_t cbmiss_last_queueC = 0xFFFFFFFFu, cbmiss_last_status48 = 0xFFFFFFFFu;
        static uint32_t cbmiss_last_head60 = 0xFFFFFFFFu, cbmiss_last_tail64 = 0xFFFFFFFFu;
        static uint32_t cbmiss_last_avail68 = 0xFFFFFFFFu, cbmiss_last_pos180 = 0xFFFFFFFFu;
        static uint32_t cbmiss_last_pending184 = 0xFFFFFFFFu, cbmiss_last_parser = 0xFFFFFFFFu;
        if (cbmiss_samples < 32u &&
            (esi != cbmiss_last_loop || cbmiss_queue8 != cbmiss_last_queue8 ||
             cbmiss_queueC != cbmiss_last_queueC || cbmiss_status48 != cbmiss_last_status48 ||
             cbmiss_head60 != cbmiss_last_head60 || cbmiss_tail64 != cbmiss_last_tail64 ||
             cbmiss_avail68 != cbmiss_last_avail68 || cbmiss_pos180 != cbmiss_last_pos180 ||
             cbmiss_pending184 != cbmiss_last_pending184 || cbmiss_parser_cursor != cbmiss_last_parser)) {
            ++cbmiss_samples;
            fprintf(stderr,
                    "[STRM-CB-ITEM-MISS] sample=%u index=%08X node=%08X wrapper=%08X result=%08X loop_remaining=%08X queue8=%08X queueC=%08X status48=%08X head60=%08X tail64=%08X avail68=%08X pos180=%08X pending184=%08X parser=%08X parser_cursor=%08X\n",
                    cbmiss_samples, ebp, ebx, cbmiss_wrapper, eax, esi, cbmiss_queue8,
                    cbmiss_queueC, cbmiss_status48, cbmiss_head60, cbmiss_tail64,
                    cbmiss_avail68, cbmiss_pos180, cbmiss_pending184, cbmiss_parser,
                    cbmiss_parser_cursor);
            fflush(stderr);
            cbmiss_last_loop = esi;
            cbmiss_last_queue8 = cbmiss_queue8;
            cbmiss_last_queueC = cbmiss_queueC;
            cbmiss_last_status48 = cbmiss_status48;
            cbmiss_last_head60 = cbmiss_head60;
            cbmiss_last_tail64 = cbmiss_tail64;
            cbmiss_last_avail68 = cbmiss_avail68;
            cbmiss_last_pos180 = cbmiss_pos180;
            cbmiss_last_pending184 = cbmiss_pending184;
            cbmiss_last_parser = cbmiss_parser_cursor;
        }
    }
    _fa = (uint32_t)(eax) & 0xFFFFFFFFu;
"@ `
    'target callback item lookup miss'
$callback = Replace-Unique $callback `
    "loc_00239CC0: ;`n    PUSH32(esp, eax);" `
    "loc_00239CC0: ;`n    recomp_stream_callback_trace(14u, ebp, ebx, eax, (eax >= 0x80000000u && eax <= 0x9FFFFFF4u) ? MEM32(eax) : 0u, (eax >= 0x80000000u && eax <= 0x9FFFFFF4u) ? MEM32(eax + 4u) : 0u, (eax >= 0x80000000u && eax <= 0x9FFFFFF4u) ? MEM32(eax + 8u) : 0u);`n    PUSH32(esp, eax);" `
    'sub_00239BD7 item dwords'
$callback = Replace-Unique $callback `
    "    RECOMP_TRACE_ESP(`"sub_00239C1A`", `"after call 0x00239BD7`");`n`nloc_00239CC7: ;" `
    "    RECOMP_TRACE_ESP(`"sub_00239C1A`", `"after call 0x00239BD7`");`n    {`n        const uint32_t trace_parser = (ebx >= 0x80000000u && ebx <= 0x9FFFFEC7u) ? MEM32(ebx + 0x138u) : 0u;`n        const int trace_parser_valid = trace_parser >= 0x80000000u && trace_parser <= 0x9FFFFFDAu;`n        const uint32_t trace_parser_flags = trace_parser_valid ? ((uint32_t)ZX8(MEM8(trace_parser + 0x24u)) | ((uint32_t)ZX8(MEM8(trace_parser + 0x25u)) << 8)) : 0u;`n        recomp_stream_callback_trace(15u, ebp, ebx, eax, trace_parser, trace_parser_valid ? MEM32(trace_parser + 0x1Cu) : 0u, trace_parser_flags);`n    }`n`nloc_00239CC7: ;" `
    'sub_00239BD7 parser state after item'
$callback = Replace-Unique $callback `
    "loc_00239CE6: ;`n    RECOMP_TRACE_ESP(`"sub_00239C1A`", `"pop ebp`");" `
    "loc_00239CE6: ;`n    recomp_stream_callback_trace(16u, MEM8(0x3516F8), MEM8(0x3516FB), MEM8(0x3515CE), 0, 0, 0);`n    RECOMP_TRACE_ESP(`"sub_00239C1A`", `"pop ebp`");" `
    'callback exit'

$dispatch = Replace-Unique $dispatch `
    "void sub_0023E827(void)`n{`n    int _flags = 0;" `
    @"
void sub_0023E827(void)
{
    const int audio_list_trace_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t audio_list_trace_return = audio_list_trace_enabled ? MEM32(esp) : 0u;
    const uint32_t audio_list_trace_header = audio_list_trace_enabled ? MEM32(esp + 4u) : 0u;
    const uint32_t audio_list_trace_node = audio_list_trace_enabled ? MEM32(esp + 8u) : 0u;
    const int audio_list_trace_match = audio_list_trace_enabled;
    const uint32_t audio_list_trace_head_before = audio_list_trace_match
        ? MEM32(audio_list_trace_header) : 0u;
    const uint32_t audio_list_trace_count_before = audio_list_trace_match
        ? MEM32(audio_list_trace_header + 8u) : 0u;
    int _flags = 0;
"@ `
    'audio list insertion trace state'

$dispatch = Replace-Unique $dispatch @'
loc_0023E845: ;
    MEM32(eax + 8) = MEM32(eax + 8) + 1;
    _fa = (uint32_t)(MEM32(eax + 8)) & 0xFFFFFFFFu;
    _fas = (int32_t)(int32_t)(_fa); _fb = (_fa == 0x80000000u); /* inc result/SF/OF; CF unchanged */
    MEM32(eax) = ecx;
    esp += 4; return; /* ret */
'@ @'
loc_0023E845: ;
    MEM32(eax + 8) = MEM32(eax + 8) + 1;
    _fa = (uint32_t)(MEM32(eax + 8)) & 0xFFFFFFFFu;
    _fas = (int32_t)(int32_t)(_fa); _fb = (_fa == 0x80000000u); /* inc result/SF/OF; CF unchanged */
    MEM32(eax) = ecx;
    if (audio_list_trace_match) {
        static unsigned int audio_list_trace_samples;
        if (audio_list_trace_samples < 32u) {
            const unsigned int sample = ++audio_list_trace_samples;
            fprintf(stderr,
                    "[STRM-AUDIO-LIST-APPEND] sample=%u caller=%08X header=%08X node=%08X head_before=%08X count_before=%08X head_after=%08X tail_after=%08X count_after=%08X node_next=%08X node_prev=%08X\n",
                    sample, audio_list_trace_return, audio_list_trace_header,
                    audio_list_trace_node, audio_list_trace_head_before,
                    audio_list_trace_count_before, MEM32(audio_list_trace_header),
                    MEM32(audio_list_trace_header + 4u), MEM32(audio_list_trace_header + 8u),
                    MEM32(audio_list_trace_node), MEM32(audio_list_trace_node + 4u));
            fflush(stderr);
        }
    }
    esp += 4; return; /* ret */
'@ `
    'audio scheduler list append result'

$dispatch = Replace-Unique $dispatch @'
loc_00241A61: ;
    _fa = (uint32_t)(MEM32(esi + 0x351818)) & 0xFFFFFFFFu; _fb = (uint32_t)(ebx) & 0xFFFFFFFFu;
'@ @'
loc_00241A61: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_factory_category_offset = MEM32(ebp - 16u);
        const uint32_t audio_factory_category = MEM32(ebp - 12u);
        const uint32_t audio_factory_loop_index = ebx;
        const uint32_t audio_factory_list_limit = MEM32(esi + 0x351818u);
        const uint32_t audio_factory_target_header = audio_factory_category_offset + 0x3517F8u;
        const uint32_t audio_factory_guard_passes =
            (int32_t)audio_factory_list_limit > (int32_t)audio_factory_loop_index ? 1u : 0u;
        static struct { uint32_t offset, category, loop, limit; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].offset == audio_factory_category_offset &&
                seen[i].category == audio_factory_category &&
                seen[i].loop == audio_factory_loop_index &&
                seen[i].limit == audio_factory_list_limit) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].offset = audio_factory_category_offset;
            seen[sample].category = audio_factory_category;
            seen[sample].loop = audio_factory_loop_index;
            seen[sample].limit = audio_factory_list_limit;
            fprintf(stderr,
                    "[STRM-AUDIO-FACTORY-GUARD] sample=%u offset=%08X category=%08X loop=%08X limit=%08X target_header=%08X passes=%u\n",
                    sample + 1u, audio_factory_category_offset, audio_factory_category,
                    audio_factory_loop_index, audio_factory_list_limit,
                    audio_factory_target_header, audio_factory_guard_passes);
            fflush(stderr);
        }
    }
    _fa = (uint32_t)(MEM32(esi + 0x351818)) & 0xFFFFFFFFu; _fb = (uint32_t)(ebx) & 0xFFFFFFFFu;
'@ `
    'audio factory list selection guard'

$dispatch = Replace-Unique $dispatch @'
loc_00241A7E: ;
    esi = eax;
'@ @'
loc_00241A7E: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_factory_insert_offset = MEM32(ebp - 16u);
        const uint32_t audio_factory_insert_header = audio_factory_insert_offset + 0x3517F8u;
        const uint32_t audio_factory_insert_node = eax;
        const uint32_t audio_factory_insert_head = MEM32(audio_factory_insert_header);
        const uint32_t audio_factory_insert_count = MEM32(audio_factory_insert_header + 8u);
        static struct { uint32_t header, node; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].header == audio_factory_insert_header &&
                seen[i].node == audio_factory_insert_node) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].header = audio_factory_insert_header;
            seen[sample].node = audio_factory_insert_node;
            fprintf(stderr,
                    "[STRM-AUDIO-FACTORY-APPEND] sample=%u offset=%08X header=%08X node=%08X head_before=%08X count_before=%08X\n",
                    sample + 1u, audio_factory_insert_offset, audio_factory_insert_header,
                    audio_factory_insert_node, audio_factory_insert_head, audio_factory_insert_count);
            fflush(stderr);
        }
    }
    esi = eax;
'@ `
    'audio factory append callsite'

$dispatch = Replace-Unique $dispatch @'
    _fa = (uint32_t)(MEM8(edi + 0x24)) & 0xFFu; _fb = (uint32_t)(4) & 0xFFu;
'@ @'
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_factory_branch_return = MEM32(ebp + 4u);
        const uint32_t audio_factory_branch_index = MEM32(ebp + 0xCu);
        const uint32_t audio_factory_flags = ZX8(MEM8(edi + 0x24u));
        const uint32_t audio_factory_count = ZX8(MEM8(edi + 0x23u));
        const uint32_t audio_factory_loop = ebx;
        static struct { uint32_t caller, index, flags, count; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == audio_factory_branch_return &&
                seen[i].index == audio_factory_branch_index &&
                seen[i].flags == audio_factory_flags && seen[i].count == audio_factory_count) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = audio_factory_branch_return;
            seen[sample].index = audio_factory_branch_index;
            seen[sample].flags = audio_factory_flags;
            seen[sample].count = audio_factory_count;
            fprintf(stderr,
                    "[STRM-AUDIO-FACTORY-BRANCH] sample=%u caller=%08X index=%08X edi=%08X flags24=%08X bit04=%u count23=%08X loop=%08X reaches_list_branch=%u\n",
                    sample + 1u, audio_factory_branch_return, audio_factory_branch_index,
                    edi, audio_factory_flags, (audio_factory_flags & 4u) != 0u,
                    audio_factory_count, audio_factory_loop,
                    (audio_factory_flags & 4u) == 0u);
            fflush(stderr);
        }
    }
    _fa = (uint32_t)(MEM8(edi + 0x24)) & 0xFFu; _fb = (uint32_t)(4) & 0xFFu;
'@ `
    'audio factory initial branch inputs'

$dispatch = Replace-Unique $dispatch @'
loc_002419C5: ;
    _fb = (uint32_t)(0x1C) & 0xFFFFFFFFu; _fbs = (int32_t)(int32_t)(_fb); /* add source, before the write */
'@ @'
loc_002419C5: ;
    if (getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        const uint32_t audio_factory_count = ZX8(MEM8(edi + 0x23u));
        const uint32_t audio_factory_loop = ebx & 0xFFu;
        const uint32_t audio_factory_continue = audio_factory_count > audio_factory_loop ? 1u : 0u;
        static struct { uint32_t caller, count, loop; } seen[16];
        static unsigned int samples;
        int duplicate = 0;
        for (unsigned int i = 0; i < samples; ++i) {
            if (seen[i].caller == MEM32(ebp + 4u) &&
                seen[i].count == audio_factory_count && seen[i].loop == audio_factory_loop) {
                duplicate = 1;
                break;
            }
        }
        if (!duplicate && samples < 16u) {
            const unsigned int sample = samples++;
            seen[sample].caller = MEM32(ebp + 4u);
            seen[sample].count = audio_factory_count;
            seen[sample].loop = audio_factory_loop;
            fprintf(stderr,
                    "[STRM-AUDIO-FACTORY-LOOP] sample=%u caller=%08X count23=%08X index=%08X continue=%u\n",
                    sample + 1u, MEM32(ebp + 4u), audio_factory_count,
                    audio_factory_loop, audio_factory_continue);
            fflush(stderr);
        }
    }
    _fb = (uint32_t)(0x1C) & 0xFFFFFFFFu; _fbs = (int32_t)(int32_t)(_fb); /* add source, before the write */
'@ `
    'audio factory post-construction loop decision'

$dispatch = Replace-Unique $dispatch @'
loc_0023E51F: ;
    PUSH32(esp, 0xFFFFFFF3u);
'@ @'
loc_0023E51F: ;
    if (ecx == 0x81D73040u && getenv("RECOMP_STREAM_CALLBACK_TRACE")) {
        static unsigned int stream_ring_full_return_samples;
        if (stream_ring_full_return_samples < 16u) {
            const unsigned int sample = ++stream_ring_full_return_samples;
            const uint32_t index = MEM32(ebp + 8u);
            const uint32_t count = ZX8(MEM8(ecx + 0x62u));
            const int32_t high = SMEM16(ecx + 0x40u);
            const int32_t cursor = SMEM16(ecx + 0x46u);
            int32_t blocked_slot = -1;
            int32_t blocked_value = 0;
            for (unsigned int i = 0; i < count && i < 6u; ++i) {
                const int32_t value = SMEM16(ecx + 0x34u + i * 2u);
                if (blocked_slot < 0 && value >= high - 1) {
                    blocked_slot = (int32_t)i;
                    blocked_value = value;
                }
            }
            fprintf(stderr,
                    "[STRM-RING-FULL-RETURN] sample=%u index=%08X record=%08X count=%u high=%d cursor=%d blocked_slot=%d blocked_value=%d result=FFFFFFF3\n",
                    sample, index, ecx, count, high, cursor, blocked_slot, blocked_value);
            fflush(stderr);
        }
    }
    PUSH32(esp, 0xFFFFFFF3u);
'@ `
    'ring full rejection return'

$dispatch = Replace-Unique $dispatch @'
void sub_0023E49E(void)
{
    uint32_t ebp = 0;
'@ @'
void sub_0023E49E(void)
{
    const int stream_ring_update_trace_enabled = getenv("RECOMP_STREAM_CALLBACK_TRACE") != NULL;
    const uint32_t stream_ring_update_caller = stream_ring_update_trace_enabled ? MEM32(esp) : 0u;
    const uint32_t stream_ring_update_index = stream_ring_update_trace_enabled ? MEM32(esp + 4u) : 0u;
    const uint32_t stream_ring_update_payload = stream_ring_update_trace_enabled ? MEM32(esp + 8u) : 0u;
    const uint32_t stream_ring_update_record = stream_ring_update_trace_enabled
        ? MEM32(stream_ring_update_index * 4u + 0x351BE4u) : 0u;
    uint32_t stream_ring_update_count_before = 0u;
    uint32_t stream_ring_update_queue_before = 0u;
    uint32_t stream_ring_update_bytes_before = 0u;
    int32_t stream_ring_update_high_before = 0;
    int32_t stream_ring_update_cursor_before = 0;
    int32_t stream_ring_update_blocked_slot = -1;
    int32_t stream_ring_update_blocked_value = 0;
    int16_t stream_ring_update_slots_before[6] = { 0, 0, 0, 0, 0, 0 };
    unsigned int stream_ring_update_sample = 0u;
    unsigned int stream_ring_update_target_call = 0u;
    if (stream_ring_update_trace_enabled && stream_ring_update_record != 0u) {
        stream_ring_update_count_before = ZX8(MEM8(stream_ring_update_record + 0x62u));
        stream_ring_update_queue_before = MEM32(stream_ring_update_record + 4u);
        stream_ring_update_bytes_before = MEM32(stream_ring_update_record + 0x48u);
        stream_ring_update_high_before = SMEM16(stream_ring_update_record + 0x40u);
        stream_ring_update_cursor_before = SMEM16(stream_ring_update_record + 0x46u);
        for (unsigned int i = 0; i < stream_ring_update_count_before && i < 6u; ++i) {
            stream_ring_update_slots_before[i] = SMEM16(stream_ring_update_record + 0x34u + i * 2u);
            if (stream_ring_update_blocked_slot < 0 &&
                stream_ring_update_slots_before[i] >= stream_ring_update_high_before - 1) {
                stream_ring_update_blocked_slot = (int32_t)i;
                stream_ring_update_blocked_value = stream_ring_update_slots_before[i];
            }
        }
        static struct {
            uint32_t index, record, count;
            int32_t last_cursor, last_high;
            int last_blocked;
        } stream_ring_update_pairs[64];
        static unsigned int stream_ring_update_pair_count;
        static unsigned int stream_ring_update_samples;
        static unsigned int stream_ring_update_target_samples;
        static unsigned int stream_ring_update_target_tail_samples;
        static unsigned int stream_ring_update_other_samples;
        static unsigned int stream_ring_update_target_calls;
        const int stream_ring_update_is_target = stream_ring_update_record == 0x81D73040u;
        if (stream_ring_update_is_target) {
            stream_ring_update_target_call = ++stream_ring_update_target_calls;
        }
        int pair_slot = -1;
        int emit_sample = 0;
        for (unsigned int i = 0; i < stream_ring_update_pair_count; ++i) {
            if (stream_ring_update_pairs[i].index == stream_ring_update_index &&
                stream_ring_update_pairs[i].record == stream_ring_update_record) {
                pair_slot = (int)i;
                const int cursor_milestone = stream_ring_update_cursor_before == 0 ||
                    (((uint32_t)stream_ring_update_cursor_before & 7u) == 0u) ||
                    stream_ring_update_cursor_before == stream_ring_update_high_before - 1;
                emit_sample =
                    (cursor_milestone && stream_ring_update_pairs[i].last_cursor != stream_ring_update_cursor_before) ||
                    stream_ring_update_pairs[i].last_high != stream_ring_update_high_before ||
                    stream_ring_update_pairs[i].count != stream_ring_update_count_before ||
                    (stream_ring_update_blocked_slot >= 0 && !stream_ring_update_pairs[i].last_blocked);
                if (stream_ring_update_blocked_slot < 0) {
                    stream_ring_update_pairs[i].last_blocked = 0;
                }
                break;
            }
        }
        if (pair_slot < 0 && stream_ring_update_pair_count < 64u) {
            pair_slot = (int)stream_ring_update_pair_count++;
            stream_ring_update_pairs[pair_slot].index = stream_ring_update_index;
            stream_ring_update_pairs[pair_slot].record = stream_ring_update_record;
            stream_ring_update_pairs[pair_slot].last_blocked = 0;
            emit_sample = 1;
        }
        if (stream_ring_update_is_target && stream_ring_update_cursor_before >= 24 &&
            stream_ring_update_target_tail_samples < 32u) {
            emit_sample = 1;
        }
        if (pair_slot >= 0 && emit_sample && stream_ring_update_samples < 72u &&
            (stream_ring_update_is_target
                ? stream_ring_update_target_samples < 40u
                : stream_ring_update_other_samples < 32u)) {
            stream_ring_update_sample = ++stream_ring_update_samples;
            if (stream_ring_update_is_target) {
                ++stream_ring_update_target_samples;
                if (stream_ring_update_cursor_before >= 24 &&
                    stream_ring_update_target_tail_samples < 32u) {
                    ++stream_ring_update_target_tail_samples;
                }
            } else {
                ++stream_ring_update_other_samples;
            }
            stream_ring_update_pairs[pair_slot].count = stream_ring_update_count_before;
            stream_ring_update_pairs[pair_slot].last_cursor = stream_ring_update_cursor_before;
            stream_ring_update_pairs[pair_slot].last_high = stream_ring_update_high_before;
            stream_ring_update_pairs[pair_slot].last_blocked = stream_ring_update_blocked_slot >= 0;
            fprintf(stderr,
                    "[STRM-RING-UPDATE] phase=entry sample=%u target_call=%u caller=%08X index=%08X payload=%08X record=%08X count=%u high=%d cursor=%d bytes=%08X queued=%08X slots=%d/%d/%d/%d/%d/%d blocked=%u blocked_slot=%d blocked_value=%d\n",
                    stream_ring_update_sample, stream_ring_update_target_call,
                    stream_ring_update_caller, stream_ring_update_index,
                    stream_ring_update_payload, stream_ring_update_record, stream_ring_update_count_before,
                    stream_ring_update_high_before, stream_ring_update_cursor_before,
                    stream_ring_update_bytes_before, stream_ring_update_queue_before,
                    stream_ring_update_slots_before[0], stream_ring_update_slots_before[1],
                    stream_ring_update_slots_before[2], stream_ring_update_slots_before[3],
                    stream_ring_update_slots_before[4], stream_ring_update_slots_before[5],
                    stream_ring_update_blocked_slot >= 0, stream_ring_update_blocked_slot,
                    stream_ring_update_blocked_value);
            fflush(stderr);
        }
    }
    uint32_t ebp = 0;
'@ `
    'ring update entry snapshot'

$dispatch = Replace-Unique $dispatch @'
loc_0023E57F: ;
    POP32(esp, edi);
'@ @'
loc_0023E57F: ;
    if (stream_ring_update_sample != 0u) {
        const uint32_t count_after = ZX8(MEM8(stream_ring_update_record + 0x62u));
        const uint32_t queue_after = MEM32(stream_ring_update_record + 4u);
        const uint32_t bytes_after = MEM32(stream_ring_update_record + 0x48u);
        const int32_t high_after = SMEM16(stream_ring_update_record + 0x40u);
        const int32_t cursor_after = SMEM16(stream_ring_update_record + 0x46u);
        int16_t slots_after[6] = { 0, 0, 0, 0, 0, 0 };
        int32_t blocked_slot_after = -1;
        int32_t blocked_value_after = 0;
        for (unsigned int i = 0; i < count_after && i < 6u; ++i) {
            slots_after[i] = SMEM16(stream_ring_update_record + 0x34u + i * 2u);
            if (blocked_slot_after < 0 && slots_after[i] >= high_after - 1) {
                blocked_slot_after = (int32_t)i;
                blocked_value_after = slots_after[i];
            }
        }
        fprintf(stderr,
                "[STRM-RING-UPDATE] phase=exit sample=%u caller=%08X index=%08X record=%08X count=%u high=%d cursor=%d bytes=%08X queued=%08X slots=%d/%d/%d/%d/%d/%d blocked=%u blocked_slot=%d blocked_value=%d\n",
                stream_ring_update_sample, stream_ring_update_caller, stream_ring_update_index,
                stream_ring_update_record, count_after, high_after, cursor_after, bytes_after,
                queue_after, slots_after[0], slots_after[1], slots_after[2],
                slots_after[3], slots_after[4], slots_after[5],
                blocked_slot_after >= 0, blocked_slot_after, blocked_value_after);
        fflush(stderr);
    }
    POP32(esp, edi);
'@ `
    'ring update exit snapshot'


$encoding = [System.Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'recomp_0046.c'), $dispatch, $encoding)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'recomp_0047.c'), $stream_methods, $encoding)
[IO.File]::WriteAllText((Join-Path $OutputDirectory 'recomp_callback_00239C1A.c'), $callback, $encoding)
Write-Output $OutputDirectory
