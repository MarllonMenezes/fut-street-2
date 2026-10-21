param([Parameter(Mandatory=$true)][string]$Executable)
$ErrorActionPreference='Stop'
$old=$env:RECOMP_FRONTEND_LOADING_TRACE
try {
 foreach($mode in @($null,'0','2','1')) {
  $env:RECOMP_FRONTEND_LOADING_TRACE=$mode
  $ErrorActionPreference='Continue'
  $lines=@(& $Executable frontend-trace 2>&1 | ForEach-Object { "$_" })
  $ErrorActionPreference='Stop'
  if($LASTEXITCODE -ne 0){throw 'Register preservation failed.'}
  $trace=@($lines | Where-Object {$_ -match '^\[FRONTEND-LOADING-TRACE\]'})
  $expected=if($mode -eq '1'){256}else{0}
  if($trace.Count -ne $expected){throw "Unexpected line count $($trace.Count), mode '$mode'"}
  foreach($site in @('0015F7B0','001606E0','001630B0','00163570')) {
   if($mode -eq '1' -and @($trace | Where-Object {$_ -match "site=$site "}).Count -ne 64){throw 'Per-site bound failed.'}
  }
  if($mode -eq '1' -and @($trace | Where-Object {$_ -notmatch 'object=00000001 esp=00000002 return=00000003 arg0=00000004 arg1=00000005 arg2=00000006 read-only=1$'}).Count){throw 'Trace fields failed.'}
 }
} finally {$env:RECOMP_FRONTEND_LOADING_TRACE=$old}
