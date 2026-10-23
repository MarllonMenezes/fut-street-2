param([Parameter(Mandatory=$true)][string]$OverlayScript)
$ErrorActionPreference='Stop'
$root=Join-Path 'D:\projeto zeta\artifacts\tmp' ('frontend-overlay-test-'+[guid]::NewGuid().ToString('N'))
$inputDir=Join-Path $root 'input'; $outputDir=Join-Path $root 'output'
New-Item -ItemType Directory -Force $inputDir | Out-Null
$chunks=@{'recomp_0026.c'=@('0015F7B0','001606E0');'recomp_0027.c'=@('001630B0','00163570')}
foreach($name in $chunks.Keys){
 $text="#include `"recomp_funcs.h`"`n"
 foreach($site in $chunks[$name]){$text+="void sub_$site(void)`n{`n    esp += 4; return;`n}`n"}
 [IO.File]::WriteAllText((Join-Path $inputDir $name),$text)
}
$extra="void sub_00160DF0(void)`n{`nloc_00160E2E: ;`n}`nvoid sub_00160ED0(void)`n{`n}`n"
[IO.File]::AppendAllText((Join-Path $inputDir 'recomp_0026.c'),$extra)
[IO.File]::AppendAllText((Join-Path $inputDir 'recomp_0026.c'), "void sub_00160330(void)`n{`nloc_00160429: ;`nloc_00160433: ;`n}`n")
[IO.File]::AppendAllText((Join-Path $inputDir 'recomp_0026.c'), "void sub_00160840(void)`n{`nloc_00160859: ;`n}`n")
$chunks['recomp_0036.c']=@()
[IO.File]::WriteAllText((Join-Path $inputDir 'recomp_0036.c'), "#include `"recomp_funcs.h`"`nloc_001D3F13: ;`nloc_001D3F2B: ;`n")
$chunks['recomp_0037.c']=@()
[IO.File]::WriteAllText((Join-Path $inputDir 'recomp_0037.c'), "#include `"recomp_funcs.h`"`nloc_001DE510: ;`nloc_001E6CAD: ;`nloc_001E6CC9: ;`n")
$before=@{};foreach($name in $chunks.Keys){$before[$name]=[IO.File]::ReadAllText((Join-Path $inputDir $name))}
& $OverlayScript -GeneratedDirectory $inputDir -OutputDirectory $outputDir
foreach($name in $chunks.Keys){
 if([IO.File]::ReadAllText((Join-Path $inputDir $name)) -ne $before[$name]){throw 'Original modified.'}
 $text=[IO.File]::ReadAllText((Join-Path $outputDir $name))
 foreach($site in $chunks[$name]){
  if(([regex]::Matches($text,"recomp_frontend_loading_trace\(0x${site}u")).Count -ne 1){throw 'Missing or duplicate hook.'}
 }
 $guards=if($name -eq 'recomp_0026.c'){10}elseif($name -eq 'recomp_0037.c'){0}else{2}
 if(([regex]::Matches($text,'if \(recomp_frontend_loading_trace_enabled\(\)')).Count -ne $guards){throw 'Default-off guard missing.'}
 if($name -eq 'recomp_0026.c') {
  foreach($site in @('00160330','00160429','00160433')) {
   if(([regex]::Matches($text,"recomp_frontend_command_trace\(0x${site}u")).Count -ne 1){throw 'Input formatter hook missing or duplicated.'}
  }
 }
 if($name -eq 'recomp_0036.c' -and ([regex]::Matches($text,'MEM32\(ebp\+4u\)==0x00160429u')).Count -ne 2){throw 'Input VM resolution caller scope missing.'}
 if($name -eq 'recomp_0037.c') {
  foreach($site in @('001E6CAD','001E6CC9')) {
   if(([regex]::Matches($text,"if \(recomp_frontend_preload_trace_active\(\)\) recomp_frontend_member_trace\(0x${site}u, ebx, MEM32\(ebp-4u\), MEM32\(ebp-8u\), eax\);")).Count -ne 1){throw 'Member setter scope/operands missing.'}
  }
 }
}
foreach($case in @('missing','duplicate')){
 $path=Join-Path $inputDir 'recomp_0027.c'
 $original=[IO.File]::ReadAllText($path)
 $broken=if($case -eq 'missing'){$original.Replace('sub_00163570','sub_00000000')}else{$original+"void sub_00163570(void)`n{`n}"}
 [IO.File]::WriteAllText($path,$broken)
 $failed=$false
 try{& $OverlayScript -GeneratedDirectory $inputDir -OutputDirectory $outputDir}catch{$failed=$true}
 [IO.File]::WriteAllText($path,$original)
 if(-not $failed){throw "Accepted $case anchor."}
}
# Keep bounded test inputs on D: for inspection; no proprietary source is used.
