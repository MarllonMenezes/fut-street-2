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
 foreach($mode in @($null,'0','2','1')) {
  $env:RECOMP_FRONTEND_LOADING_TRACE=$mode
  $ErrorActionPreference='Continue'
  $lines=@(& $Executable frontend-command 2>&1 | ForEach-Object { "$_" })
  $code=$LASTEXITCODE
  $ErrorActionPreference='Stop'
  if($code -ne 0){throw 'Command trace changed registers or input.'}
  $trace=@($lines | Where-Object {$_ -match '^\[FRONTEND-COMMAND-TRACE\]'})
  $expected=if($mode -eq '1'){192}else{0}
  if($trace.Count -ne $expected){throw 'Command trace activation/bound failed.'}
  if($mode -eq '1') {
   $valid=@($trace | Where-Object {$_ -like '*text="boot\x0A\x22\x5Ccommand" text_status=terminated detail=00000004 result=00000005*'})
   $invalid=@($trace | Where-Object {$_ -like '*text="" text_status=unreadable*'})
   if($valid.Count -ne 64 -or $invalid.Count -ne 128){throw 'Safe text capture or escaping failed.'}
  }
 }
 $env:RECOMP_FRONTEND_LOADING_TRACE='1'
 $ErrorActionPreference='Continue'
 $output=(@(& $Executable frontend-text-boundaries 2>&1) -join "`n")
 $code=$LASTEXITCODE
 $ErrorActionPreference='Stop'
 if($code -ne 0 -or $output -notmatch 'text="" text_status=unreadable' -or
    -not $output.Contains(('\x01'*96)+'" text_status=truncated') -or
    -not $output.Contains(('X'*95)+'" text_status=terminated')) {
  throw 'Protected page, text length boundary, or Win32 error preservation failed.'
 }
} finally {$env:RECOMP_FRONTEND_LOADING_TRACE=$old}
