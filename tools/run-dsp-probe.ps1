[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$ExecutablePath,
    [Parameter(Mandatory=$true)][string]$BootstrapPath,
    [string]$OutputRoot='D:\projeto zeta\artifacts\dsp-probe',
    [ValidateRange(1,15)][int]$TimeoutSeconds=15
)
$ErrorActionPreference='Stop'
$ExecutablePath=(Resolve-Path -LiteralPath $ExecutablePath).Path
$BootstrapPath=(Resolve-Path -LiteralPath $BootstrapPath).Path
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
foreach($path in @($ExecutablePath,$BootstrapPath,$OutputRoot)) {
    if($path -notmatch '^[Dd]:\\' -or $path.Contains('"')) {throw 'Probe paths must be on D: without quotes'}
}
if((Get-Item -LiteralPath $BootstrapPath).Length -ne 8192) {throw 'Expected 8192-byte LE32 bootstrap fixture'}
$run=Join-Path $OutputRoot ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
$temp=Join-Path $run 'tmp'
New-Item -ItemType Directory -Path $temp,(Join-Path $run 'saves') -Force | Out-Null
$info=[Diagnostics.ProcessStartInfo]::new()
$info.FileName=$ExecutablePath; $info.Arguments='"'+$BootstrapPath+'"'
$info.WorkingDirectory=$run; $info.UseShellExecute=$false; $info.CreateNoWindow=$true
$info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
foreach($name in @('TEMP','TMP','TMPDIR')) {$info.EnvironmentVariables[$name]=$temp}
$process=[Diagnostics.Process]::new(); $process.StartInfo=$info
$started=$false
try {
    $started=$process.Start()
    if(-not $started){throw 'Cannot start isolated DSP probe'}
    $stdout=$process.StandardOutput.ReadToEndAsync()
    $stderr=$process.StandardError.ReadToEndAsync()
    $timedOut=-not $process.WaitForExit($TimeoutSeconds*1000)
    if($timedOut){$process.Kill();$process.WaitForExit()}
    $out=$stdout.GetAwaiter().GetResult();$err=$stderr.GetAwaiter().GetResult()
    $out | Set-Content -LiteralPath (Join-Path $run 'stdout.log')
    $err | Set-Content -LiteralPath (Join-Path $run 'stderr.log')
    $process.Refresh()
    [ordered]@{executable=$ExecutablePath;executable_sha256=(Get-FileHash $ExecutablePath).Hash;
        fixture=$BootstrapPath;fixture_sha256=(Get-FileHash $BootstrapPath).Hash;
        timeout_seconds=$TimeoutSeconds;timed_out=$timedOut;exit_code=$process.ExitCode;
        isolated=$true;dma_start_observed=(-not $timedOut -and $process.ExitCode -eq 20);
        game_boot=$false;hardware_fidelity_proven=$false} |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'run.json')
    Write-Output $run; Write-Output $err
    if($timedOut){throw 'Isolated DSP probe exceeded timeout'}
    if($process.ExitCode -lt 20 -or $process.ExitCode -gt 25){throw 'Probe failed before a diagnostic stop'}
} finally {
    if($started -and -not $process.HasExited){$process.Kill();$process.WaitForExit()}
    $process.Dispose()
}
