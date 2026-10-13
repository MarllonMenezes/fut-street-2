# Pure argument adapter; never runs an application or changes the filesystem.
function Get-CleanRoomReleaseInvocation {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)][string]$ProjectPath,
        [Parameter(Mandatory = $true)][string]$RuntimeAssembly,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )

    $cleanRoomPrefix = @('run', '--project', $ProjectPath, '-c', 'Release', '--no-build', '--')
    if ($Arguments.Count -le $cleanRoomPrefix.Count) {
        throw 'Release invocation must contain the exact run prefix and application arguments.'
    }
    for ($cleanRoomIndex = 0; $cleanRoomIndex -lt $cleanRoomPrefix.Count; $cleanRoomIndex++) {
        if ($Arguments[$cleanRoomIndex] -cne $cleanRoomPrefix[$cleanRoomIndex]) {
            throw "Unsupported Release invocation prefix at index $cleanRoomIndex."
        }
    }
    # Keep arguments as individual array items. Do not interpolate a shell
    # command or reinterpret spaces, quotes, Unicode or a literal '--'.
    @($RuntimeAssembly) + $Arguments[$cleanRoomPrefix.Count..($Arguments.Count - 1)]
}
