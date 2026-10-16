param(
    [Parameter(Mandatory=$true)][string]$ExecutablePath,
    [Parameter(Mandatory=$true)][string]$GoldenPath,
    [string]$OutputRoot='D:\projeto zeta\artifacts\aa2-validation',
    [switch]$UniformGrid
)
$ErrorActionPreference='Stop'
# Public Xbox1.0 hardware output, not a locally performed hardware measurement:
# https://github.com/abaire/nxdk_pgraph_tests_golden_results/blob/48dd6b93b10ebdf561cd396cc2ba1667e22748d6/results/Antialiasing_tests/FBSurfaceWithCenterCorner2.png
# Geometry: FBSurfaceWithCenterCorner2 in nxdk_pgraph_tests/antialiasing_tests.cpp.
# This experiment checks the clean upper diamond band only. It does not test
# fragment shading, sample masks, resolve, depth/stencil or dithering.
$ExecutablePath=(Resolve-Path -LiteralPath $ExecutablePath).Path
$GoldenPath=(Resolve-Path -LiteralPath $GoldenPath).Path
$OutputRoot=[IO.Path]::GetFullPath($OutputRoot)
foreach($path in @($ExecutablePath,$GoldenPath,$OutputRoot)) {
    if($path -notmatch '^[Dd]:\\' -or $path.Contains('"')) {throw 'All experiment paths must be on D:'}
}
$expectedHash='B0DA4F42F22D990AA17E0E4D693994DEC3AF00CE0732F74324F6F95F1E9B5119'
if((Get-FileHash -LiteralPath $GoldenPath).Hash -ne $expectedHash) {throw 'Unrecognized hardware golden image'}
$run=Join-Path $OutputRoot ((Get-Date -Format 'yyyyMMdd-HHmmss')+'-'+[guid]::NewGuid().ToString('N').Substring(0,8))
$temp=Join-Path $run 'tmp'
New-Item -ItemType Directory -Path $temp,(Join-Path $run 'saves') -Force | Out-Null
$maskPath=Join-Path $run 'hardware-mask.bin'
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new($GoldenPath)
$writer=$null
try {
    if($bitmap.Width -ne 640 -or $bitmap.Height -ne 480) {throw 'Unexpected golden dimensions'}
    $writer=[IO.BinaryWriter]::new([IO.File]::Open($maskPath,'CreateNew'))
    foreach($word in @(0x32414146,1280,90,121)) {$writer.Write([uint32]$word)}
    # Hardware image is the un-resolved packed framebuffer read with 640-pixel
    # screenshot rows: two screenshot rows contain one 1280-column AA row.
    for($y=90;$y -lt 211;$y++) {
        for($x=0;$x -lt 1280;$x++) {
            $color=$bitmap.GetPixel(($x%640),(2*$y+[int][Math]::Floor($x/640))).ToArgb() -band 0xFFFFFF
            if($color -eq 0x40F2BF) {$writer.Write([byte]1)}
            elseif($color -eq 0x050505) {$writer.Write([byte]0)}
            else {throw ('Unexpected RGB at packed {0},{1}: {2:X6}' -f $x,$y,$color)}
        }
    }
} finally {if($writer){$writer.Dispose()};$bitmap.Dispose()}
$info=[Diagnostics.ProcessStartInfo]::new()
$info.FileName=$ExecutablePath;$info.Arguments='"'+$maskPath+'"'
if($UniformGrid){$info.Arguments+=' --uniform-grid'}
$info.WorkingDirectory=$run;$info.UseShellExecute=$false;$info.CreateNoWindow=$true
$info.RedirectStandardOutput=$true;$info.RedirectStandardError=$true
foreach($name in @('TEMP','TMP','TMPDIR')) {$info.EnvironmentVariables[$name]=$temp}
$process=[Diagnostics.Process]::new();$process.StartInfo=$info
try {
    if(-not $process.Start()){throw 'Cannot start AA coverage experiment'}
    $stdout=$process.StandardOutput.ReadToEndAsync();$stderr=$process.StandardError.ReadToEndAsync()
    $timedOut=-not $process.WaitForExit(15000)
    if($timedOut){$process.Kill();$process.WaitForExit()}
    $out=$stdout.GetAwaiter().GetResult();$err=$stderr.GetAwaiter().GetResult()
    $out | Set-Content -LiteralPath (Join-Path $run 'stdout.log')
    $err | Set-Content -LiteralPath (Join-Path $run 'stderr.log')
    $process.Refresh()
    [ordered]@{executable=$ExecutablePath;executable_sha256=(Get-FileHash $ExecutablePath).Hash;
        golden=$GoldenPath;golden_sha256=$expectedHash;mask_sha256=(Get-FileHash $maskPath).Hash;
        timeout_seconds=15;timed_out=$timedOut;exit_code=$process.ExitCode;
        coverage_only=$true;uniform_grid_negative_control=[bool]$UniformGrid} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'run.json')
    Write-Output $run;Write-Output $out;Write-Output $err
    $wantedExit=if($UniformGrid){22}else{0}
    if($timedOut -or $process.ExitCode -ne $wantedExit){throw 'AA hardware coverage comparison failed'}
} finally {if(-not $process.HasExited){$process.Kill();$process.WaitForExit()};$process.Dispose()}
