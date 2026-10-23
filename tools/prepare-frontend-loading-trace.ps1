[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$GeneratedDirectory,
    [Parameter(Mandatory=$true)][string]$OutputDirectory
)
$ErrorActionPreference='Stop'
$GeneratedDirectory=[IO.Path]::GetFullPath($GeneratedDirectory)
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if($GeneratedDirectory -notmatch '^[Dd]:\\' -or $OutputDirectory -notmatch '^[Dd]:\\' -or $GeneratedDirectory.TrimEnd('\') -eq $OutputDirectory.TrimEnd('\')) { throw 'Use distinct absolute D: paths.' }
function Replace-Unique([string]$Text,[string]$Old,[string]$New) {
    if(([regex]::Matches($Text,[regex]::Escape($Old))).Count -ne 1) { throw 'Missing or duplicate overlay anchor.' }
    return $Text.Replace($Old,$New)
}
$results=@{}
foreach($name in @('recomp_0026.c','recomp_0027.c','recomp_0036.c','recomp_0037.c')) {
    $text=[IO.File]::ReadAllText((Join-Path $GeneratedDirectory $name)).Replace("`r`n","`n")
    $declarations="`nextern int recomp_frontend_loading_trace_enabled(void);`nextern void recomp_frontend_loading_trace(uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t);`n"
    $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+$declarations)
    $sites=if($name -eq 'recomp_0026.c'){@('0015F7B0','001606E0')}elseif($name -eq 'recomp_0027.c'){@('001630B0','00163570')}else{@()}
    foreach($site in $sites) {
        $anchor="void sub_$site(void)`n{`n"
        $call="    if (recomp_frontend_loading_trace_enabled())`n        recomp_frontend_loading_trace(0x${site}u, ecx, esp, MEM32(esp), MEM32(esp+4u), MEM32(esp+8u), MEM32(esp+12u));`n"
        $text=Replace-Unique $text $anchor ($anchor+$call)
    }
    if($name -eq 'recomp_0026.c') {
        $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+"`nextern void recomp_frontend_command_trace(uint32_t,uint32_t,uint32_t,uint32_t);`n")
        $anchor="void sub_00160DF0(void)`n{`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160DF0u, MEM32(esp+4u), MEM32(esp+8u), 0u);`n")
        $anchor="loc_00160E2E: ;`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160E2Eu, esi, edi, eax);`n")
        $anchor="void sub_00160ED0(void)`n{`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160ED0u, MEM32(esp+4u), 0u, 0u);`n")
        $anchor="void sub_00160330(void)`n{`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160330u, MEM32(0x310B20u), MEM32(esp+8u), MEM32(esp+4u));`n")
        foreach($site in @('00160429','00160433')) {
            $anchor="loc_${site}: ;`n"
            $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x${site}u, MEM32(0x310B20u), ebp, eax);`n")
        }
    }
    if($name -eq 'recomp_0026.c') {
        $anchor="void sub_00160840(void)`n{`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160840u, MEM32(0x310B2Cu), ecx, 0u);`n")
        $anchor="loc_00160859: ;`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled()) recomp_frontend_command_trace(0x00160859u, MEM32(0x310B2Cu), esi, eax);`n")
    }
    if($name -eq 'recomp_0036.c') {
        $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+"`nextern void recomp_frontend_command_trace(uint32_t,uint32_t,uint32_t,uint32_t);`n")
        foreach($site in @('001D3F13','001D3F2B')) {
            $anchor="loc_${site}: ;`n"
            $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_loading_trace_enabled() && (MEM32(ebp+4u)==0x00160859u || MEM32(ebp+4u)==0x00160429u)) recomp_frontend_command_trace(0x${site}u, MEM32(ebp+8u), MEM32(ebp-4u), eax);`n")
        }
    }
    if($name -eq 'recomp_0037.c') {
        $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+"`nextern int recomp_frontend_preload_trace_active(void);`nextern void recomp_frontend_preload_vm_trace(uint32_t,uint32_t,uint32_t);`n")
        $anchor="loc_001DE510: ;`n"
        $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_preload_trace_active()) recomp_frontend_preload_vm_trace(MEM32(ebp-28u)-1u, eax, esi);`n")
        $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+"`nextern void recomp_frontend_member_trace(uint32_t,uint32_t,uint32_t,uint32_t,uint32_t);`n")
        foreach($site in @('001E6CAD','001E6CC9')) {
            $anchor="loc_${site}: ;`n"
            $text=Replace-Unique $text $anchor ($anchor+"    if (recomp_frontend_preload_trace_active()) recomp_frontend_member_trace(0x${site}u, ebx, MEM32(ebp-4u), MEM32(ebp-8u), eax);`n")
        }
    }
    $results[$name]=$text
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($name in $results.Keys){[IO.File]::WriteAllText((Join-Path $OutputDirectory $name),$results[$name],(New-Object Text.UTF8Encoding($false)))}
