param([Parameter(Mandatory=$true)][string]$OverlayScript)
$ErrorActionPreference='Stop'
$root=Join-Path 'D:\projeto zeta\artifacts\tmp' ('native-input-contract-'+[guid]::NewGuid().ToString('N'))
$inputDir=Join-Path $root 'input';$outputDir=Join-Path $root 'output'
New-Item -ItemType Directory -Force $inputDir|Out-Null
$sites=@('002A2112','002A2168','002A2174','002A234C','002A23BF','002A2419','002A243B')
$source="#include `"recomp_funcs.h`"`n"
foreach($site in $sites){$source+="void sub_$site(void)`n{`n    esp += 4; return;`n}`n"}
$path=Join-Path $inputDir 'recomp_0060.c';[IO.File]::WriteAllText($path,$source)
& $OverlayScript -GeneratedDirectory $inputDir -OutputDirectory $outputDir
if([IO.File]::ReadAllText($path)-ne $source){throw 'Original source changed.'}
$actual=[IO.File]::ReadAllText((Join-Path $outputDir 'recomp_0060.c'))
foreach($site in $sites){if(([regex]::Matches($actual,"if \(fut_native_input_dispatch\(0x${site}u\)\) return;")).Count-ne 1){throw 'Missing/duplicate input dispatch.'}}
foreach($site in $sites){foreach($kind in @('missing','duplicate')){
 $broken=if($kind-eq'missing'){$source.Replace("void sub_$site",'void sub_00000000')}else{$source+"void sub_$site(void)`n{`n}"}
 [IO.File]::WriteAllText($path,$broken);$refused=$false
 try{& $OverlayScript -GeneratedDirectory $inputDir -OutputDirectory $outputDir}catch{$refused=$true}
 if(-not $refused){throw "Accepted $kind SDK anchor $site"}
}}
