param(
 [Parameter(Mandatory=$true)][string]$OverlayScript,
 [Parameter(Mandatory=$true)][string]$GeneratedDirectory
)
$ErrorActionPreference='Stop'
$root=Join-Path 'D:\projeto zeta\artifacts\tmp' ('pcm-refill-overlay-'+[guid]::NewGuid().ToString('N'))
$before=@{}
foreach($name in @('recomp_0046.c','recomp_0047.c','recomp_callback_00239C1A.c')){
 $before[$name]=[IO.File]::ReadAllText((Join-Path $GeneratedDirectory $name))
}
& $OverlayScript -GeneratedDirectory $GeneratedDirectory -OutputDirectory $root | Out-Null
$text=[IO.File]::ReadAllText((Join-Path $root 'recomp_0046.c')).Replace("`r`n","`n")
$start=$text.IndexOf("loc_0023C548: ;`n",[StringComparison]::Ordinal)
if($start -lt 0){throw 'Missing original refill tail.'}
$end=$text.IndexOf("`n}",$start,[StringComparison]::Ordinal)
if($end -lt 0){throw 'Missing original refill return.'}
$tail=$text.Substring($start,$end-$start)
$expected="loc_0023C548: ;`n    POP32(esp, edi);`n    MEM32(0x351870) = esi;`n    fut_native_pcm_submit(MEM32(0x351850u), MEM32(0x351870u));`n    POP32(esp, esi);"
if(-not $tail.StartsWith($expected,[StringComparison]::Ordinal)){throw 'PCM submit must follow committed original refill cursor and precede register restore.'}
if(([regex]::Matches($text,'(?m)^    fut_native_pcm_submit\(')).Count -ne 1){throw 'Duplicate/missing PCM submit call.'}
foreach($name in $before.Keys){
 if([IO.File]::ReadAllText((Join-Path $GeneratedDirectory $name)) -cne $before[$name]){throw 'Original generated input modified.'}
}
Write-Output 'PCM overlay submits after the original filled cursor commit; input sources unchanged.'
