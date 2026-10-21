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
foreach($name in @('recomp_0026.c','recomp_0027.c')) {
    $text=[IO.File]::ReadAllText((Join-Path $GeneratedDirectory $name)).Replace("`r`n","`n")
    $declarations="`nextern int recomp_frontend_loading_trace_enabled(void);`nextern void recomp_frontend_loading_trace(uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t,uint32_t);`n"
    $text=Replace-Unique $text '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+$declarations)
    $sites=if($name -eq 'recomp_0026.c'){@('0015F7B0','001606E0')}else{@('001630B0','00163570')}
    foreach($site in $sites) {
        $anchor="void sub_$site(void)`n{`n"
        $call="    if (recomp_frontend_loading_trace_enabled())`n        recomp_frontend_loading_trace(0x${site}u, ecx, esp, MEM32(esp), MEM32(esp+4u), MEM32(esp+8u), MEM32(esp+12u));`n"
        $text=Replace-Unique $text $anchor ($anchor+$call)
    }
    $results[$name]=$text
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
foreach($name in $results.Keys){[IO.File]::WriteAllText((Join-Path $OutputDirectory $name),$results[$name],(New-Object Text.UTF8Encoding($false)))}
