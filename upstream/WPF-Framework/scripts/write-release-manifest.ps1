<#
.SYNOPSIS
Validates four built packages and records the exact Release workflow build identity.
#>
param(
    [Parameter(Mandatory)][string]$Directory,
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Version,
    [Parameter(Mandatory)][string]$CommitSHA,
    [Parameter(Mandatory)][string]$RunID,
    [Parameter(Mandatory)][string]$RunAttempt
)
. (Join-Path $PSScriptRoot 'release-artifact-contract.ps1')
Assert-StableReleaseVersion $Tag $Version
if ($CommitSHA -notmatch '^[a-f0-9]{40}$' -or $RunID -notmatch '^[1-9]\d*$' -or $RunAttempt -notmatch '^[1-9]\d*$') { throw 'Invalid build identity.' }
$packages = @(Get-ChildItem -LiteralPath $Directory -File)
if ($packages.Count -ne 4 -or @(Get-ChildItem -LiteralPath $Directory -Directory).Count) { throw 'Manifest creation requires exactly four package files.' }
$records = foreach ($id in $script:ReleasePackageIds) {
    $filename = "$id.$Version.nupkg"
    $path = Join-Path $Directory $filename
    Assert-PackageContract $path $id $Version (Split-Path $PSScriptRoot)
    @{ id = $id; filename = $filename; sha256 = Get-ReleaseSha256 $path }
}
@{ schemaVersion = 1; repository = $Repository; tag = $Tag; version = $Version; commitSHA = $CommitSHA; runID = $RunID; runAttempt = $RunAttempt; packages = @($records) } |
    ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $Directory 'release-manifest.json') -Encoding utf8NoBOM
