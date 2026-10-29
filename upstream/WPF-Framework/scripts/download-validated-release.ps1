<#
.SYNOPSIS
Downloads only the receipt-approved successful Release artifact and verifies its content.
#>
param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory)][string]$Directory
)
. (Join-Path $PSScriptRoot 'release-artifact-contract.ps1')
# The current checkout is the release tag; resolve annotated tags to their commit.
$sha = & git rev-parse "$Tag^{commit}"
if ($LASTEXITCODE -ne 0) { throw 'Cannot peel release tag.' }
$head = & git rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve checkout.' }
Assert-ReleaseEqual $head $sha 'Checkout/tag SHA'
if (Test-Path -LiteralPath $Directory) { throw 'Use a new empty artifact destination.' }
New-Item -ItemType Directory -Path $Directory | Out-Null
$headers = @{ Authorization = "Bearer $env:GITHUB_TOKEN"; Accept = 'application/vnd.github+json'; 'X-GitHub-Api-Version' = '2022-11-28' }
$base = "https://api.github.com/repos/$Repository"
$release = Invoke-RestMethod "$base/releases/tags/$([Uri]::EscapeDataString($Tag))" -Headers $headers
if ($release.draft -or $release.prerelease) { throw 'Only a published stable release is allowed.' }
$assets = @($release.assets | Where-Object { $_.name -ceq 'downstream-validation.json' })
if ($assets.Count -ne 1) { throw 'Exactly one downstream-validation.json release asset is required.' }
$receiptPath = Join-Path $Directory '..' 'downstream-validation.json'
$downloadHeaders = $headers.Clone(); $downloadHeaders.Accept = 'application/octet-stream'
Invoke-WebRequest "$base/releases/assets/$($assets[0].id)" -Headers $downloadHeaders -OutFile $receiptPath | Out-Null
$receipt = Read-ReleaseJson $receiptPath
Assert-ReleaseReceipt $receipt $Repository $Tag $sha
# Fetch exactly the recorded attempt, never the most recent run or artifact with a similar name.
$run = Invoke-RestMethod "$base/actions/runs/$($receipt.runID)/attempts/$($receipt.runAttempt)" -Headers $headers
$artifact = Invoke-RestMethod "$base/actions/artifacts/$($receipt.artifactID)" -Headers $headers
Assert-ReleaseRun $receipt $run $artifact $Repository $Tag $sha $release
$archivePath = Join-Path $Directory '..' 'release-artifact.zip'
Invoke-WebRequest "$base/actions/artifacts/$($receipt.artifactID)/zip" -Headers $headers -OutFile $archivePath | Out-Null
$archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    $seen = @{}
    $allowed = @('release-manifest.json') + @($receipt.packages | ForEach-Object { $_.filename })
    if ($archive.Entries.Count -ne 5) { throw 'Unexpected artifact ZIP entry count.' }
    foreach ($entry in $archive.Entries) {
        if ($allowed -cnotcontains $entry.FullName -or $seen.ContainsKey($entry.FullName)) { throw 'Unexpected/duplicate artifact ZIP entry.' }
        $seen[$entry.FullName] = $true
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $Directory $entry.FullName), $false)
    }
}
finally { $archive.Dispose() }
$null = Assert-ReleaseArtifact $Directory $receipt (Split-Path $PSScriptRoot)
Write-Host "Validated exact artifact $($receipt.artifactID), run $($receipt.runID) attempt $($receipt.runAttempt), tag $Tag, SHA $sha."
