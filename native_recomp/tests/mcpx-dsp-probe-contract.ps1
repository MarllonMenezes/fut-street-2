param([Parameter(Mandatory=$true)][string]$ExecutablePath)
$ErrorActionPreference='Stop'
$root=Join-Path (Get-Location).Path ('dsp-probe-test-'+[guid]::NewGuid().ToString('N'))
if($root -notmatch '^[Dd]:\\'){throw 'DSP probe test artifacts must be on D:'}
New-Item -ItemType Directory -Path $root,(Join-Path $root 'tmp'),(Join-Path $root 'saves') | Out-Null

function Test-Probe([string]$Name,[uint32[]]$Words,[int]$Expected,[string]$Pattern,[int]$Size=8192) {
    $bytes=[byte[]]::new($Size)
    for($i=0;$i -lt $Words.Length;$i++) {
        for($b=0;$b -lt 4;$b++) {$bytes[4*$i+$b]=[byte](($Words[$i] -shr (8*$b)) -band 255)}
    }
    $path=Join-Path $root ($Name+'.bin')
    [IO.File]::WriteAllBytes($path,$bytes)
    $info=[Diagnostics.ProcessStartInfo]::new()
    $info.FileName=$ExecutablePath;$info.Arguments='"'+$path+'"';$info.WorkingDirectory=$root
    $info.UseShellExecute=$false;$info.CreateNoWindow=$true
    $info.RedirectStandardError=$true;$info.RedirectStandardOutput=$true
    foreach($variable in @('TEMP','TMP','TMPDIR')) {$info.EnvironmentVariables[$variable]=Join-Path $root 'tmp'}
    $p=[Diagnostics.Process]::new();$p.StartInfo=$info;$started=$false
    try {
        $started=$p.Start();if(-not $started){throw 'Probe start failed'}
        $err=$p.StandardError.ReadToEndAsync();$out=$p.StandardOutput.ReadToEndAsync()
        if(-not $p.WaitForExit(3000)){$p.Kill();$p.WaitForExit();throw 'Probe test timeout'}
        $text=$err.GetAwaiter().GetResult()+$out.GetAwaiter().GetResult();$p.Refresh()
        $text | Set-Content -LiteralPath (Join-Path $root ($Name+'.log'))
        if($p.ExitCode -ne $Expected -or $text -notmatch $Pattern) {
            throw "$Name expected exit=$Expected pattern=$Pattern; got exit=$($p.ExitCode): $text"
        }
    } finally {if($started -and -not $p.HasExited){$p.Kill();$p.WaitForExit()};$p.Dispose()}
}

# Synthetic instructions, not bytes from a title. Encodings follow the pinned
# dsp56300 tests: JMP short, MOVEP immediate X:qq and MOVEP immediate X:pp.
Test-Probe 'cycle-limit' @(0x0C0000) 25 'cycle budget ended pc=000000'
Test-Probe 'unknown-write' @(0x07F420,0x123456) 22 'unsupported peripheral write'
Test-Probe 'first-start' @(0x08F496,1) 20 'first-DMA-start'
Test-Probe 'bad-node' @(0x08F494,0x1000,0x08F496,1) 24 'first-DMA-start next=001000'
Test-Probe 'short-input' @() 2 '' 1
Write-Output 'DSP probe: cycle bound, unknown access, first-start capture, invalid node and input size passed'
