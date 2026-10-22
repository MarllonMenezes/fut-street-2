[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$GeneratedDirectory,[Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$GeneratedDirectory=[IO.Path]::GetFullPath($GeneratedDirectory)
$OutputDirectory=[IO.Path]::GetFullPath($OutputDirectory)
if($GeneratedDirectory -notmatch '^[Dd]:\\' -or $OutputDirectory -notmatch '^[Dd]:\\' -or $GeneratedDirectory.TrimEnd('\') -eq $OutputDirectory.TrimEnd('\')){throw 'Use distinct absolute D: paths.'}
$name='recomp_0060.c'
$text=[IO.File]::ReadAllText((Join-Path $GeneratedDirectory $name)).Replace("`r`n","`n")
function Replace-Unique([string]$Old,[string]$New){if(([regex]::Matches($script:text,[regex]::Escape($Old))).Count-ne 1){throw 'Missing or duplicate native input anchor.'};$script:text=$script:text.Replace($Old,$New)}
Replace-Unique '#include "recomp_funcs.h"' ('#include "recomp_funcs.h"'+"`nextern int fut_native_input_dispatch(uint32_t);`n")
foreach($site in @('002A2112','002A2168','002A2174','002A234C','002A23BF','002A2419','002A243B')){
 $anchor="void sub_$site(void)`n{`n"
 Replace-Unique $anchor ($anchor+"    if (fut_native_input_dispatch(0x${site}u)) return;`n")
}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
[IO.File]::WriteAllText((Join-Path $OutputDirectory $name),$text,(New-Object Text.UTF8Encoding($false)))
