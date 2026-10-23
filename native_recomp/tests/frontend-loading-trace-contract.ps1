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
 foreach($case in @('frontend-input','frontend-song')) {
  foreach($mode in @($null,'0','2','1')) {
   $env:RECOMP_FRONTEND_LOADING_TRACE=$mode
   $ErrorActionPreference='Continue'
   $lines=@(& $Executable $case 2>&1 | ForEach-Object { "$_" })
   $code=$LASTEXITCODE
   $ErrorActionPreference='Stop'
   if($code -ne 0){throw 'Input/song trace changed registers, input or LastError.'}
   $trace=@($lines | Where-Object {$_ -match '^\[FRONTEND-COMMAND-TRACE\]'})
   $expected=if($mode -ne '1'){0}elseif($case -eq 'frontend-input'){192}else{64}
   if($trace.Count -ne $expected){throw 'Input/song opt-in or budget failed.'}
   if($mode -eq '1' -and $case -eq 'frontend-input') {
    foreach($site in @('00160330','00160429','00160433')) {
     if(@($trace | Where-Object {$_ -match "site=$site "}).Count -ne 64){throw 'Input per-site budget failed.'}
    }
   }
   if($mode -eq '1' -and $case -eq 'frontend-song') {
    if(@($trace | Where-Object {$_ -match 'text="eatrax\?sub=currentsong"'}).Count -ne 2 -or
       @($trace | Where-Object {$_ -match 'text="_level0.feInputEvent"'}).Count -ne 62){throw 'Recurring query exhausted later-command budget.'}
   }
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
 foreach($mode in @($null,'0','2','1')) {
  $env:RECOMP_FRONTEND_LOADING_TRACE=$mode
  $ErrorActionPreference='Continue'
  $lines=@(& $Executable frontend-vm 2>&1 | ForEach-Object { "$_" })
  $code=$LASTEXITCODE
  $ErrorActionPreference='Stop'
  if($code -ne 0){throw 'Preload scope/register/input preservation failed.'}
  $trace=@($lines | Where-Object {$_ -match '^\[PRELOAD-VM-TRACE\]'})
  $expected=if($mode -eq '1'){512}else{0}
  if($trace.Count -ne $expected){throw 'Preload VM opt-in, scope, or bound failed.'}
  if($mode -eq '1' -and @($trace | Where-Object {$_ -notlike '*readable=1 top=00010010/02000000/11223344/55667788*'}).Count){throw 'Protected VM stack read failed.'}
 }
 foreach($mode in @($null,'0','2','1')) {
  $env:RECOMP_FRONTEND_LOADING_TRACE=$mode
  $ErrorActionPreference='Continue'
  $lines=@(& $Executable frontend-member 2>&1 | ForEach-Object { "$_" })
  $code=$LASTEXITCODE
  $ErrorActionPreference='Stop'
  if($code -ne 0){throw 'Member trace changed registers, memory, scope or LastError.'}
  $trace=@($lines | Where-Object {$_ -match '^\[PRELOAD-MEMBER-TRACE\]'})
  $expected=if($mode -eq '1'){32}else{0}
  if($trace.Count -ne $expected){throw 'Member trace opt-in/scope/budget failed.'}
  if($mode -eq '1') {
   foreach($site in @('001E6CAD','001E6CC9')) {
    if(@($trace | Where-Object {$_ -match "site=$site "}).Count -ne 16){throw 'Member per-site budget failed.'}
   }
   if(@($trace | Where-Object {$_ -like '*text="_visible" text_status=terminated*object_readable=1 value_readable=1 cell=CAFEBABE/0A000077/00000001/00000000 words=12345678/1A040191/*read-only=1'}).Count -ne 30){throw 'Member safe snapshot fields failed.'}
   if(@($trace | Where-Object {$_ -like '*text_status=unreadable*object_readable=0 value_readable=0*'}).Count -ne 2){throw 'Member invalid/guard-page handling failed.'}
  }
 }
} finally {$env:RECOMP_FRONTEND_LOADING_TRACE=$old}
