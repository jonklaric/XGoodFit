<#
.SYNOPSIS
Preflights immutable package versions and optionally pushes only validated absent packages.
#>
param(
    [Parameter(Mandatory)][string]$Directory,
    [Parameter(Mandatory)][string]$ReceiptPath,
    [switch]$Push
)
. (Join-Path $PSScriptRoot 'release-artifact-contract.ps1')
$receipt = Read-ReleaseJson $ReceiptPath
Assert-ReleaseReceipt $receipt $receipt.repository $receipt.frameworkTag $receipt.frameworkTagSHA
$manifest = Assert-ReleaseArtifact $Directory $receipt (Split-Path $PSScriptRoot)
$pending = @()
$temp = Join-Path ([IO.Path]::GetTempPath()) "nuget-existing-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $temp | Out-Null
try {
    # Preflight every version before any push. All network errors other than 404 are fatal.
    foreach ($package in $manifest.packages) {
        $id = $package.id.ToLowerInvariant()
        $version = $receipt.version.ToLowerInvariant()
        $url = "https://api.nuget.org/v3-flatcontainer/$id/$version/$id.$version.nupkg"
        $published = Join-Path $temp $package.filename
        $exists = $true
        try { Invoke-WebRequest $url -OutFile $published | Out-Null }
        catch {
            if ($null -ne $_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) { $exists = $false }
            else { throw }
        }
        $candidate = Join-Path $Directory $package.filename
        if ($exists) {
            Assert-PublishedPackageMatches $candidate $published {
                param($path)
                & dotnet nuget verify $path --all
                if ($LASTEXITCODE -ne 0) { throw 'Published NuGet signature verification failed.' }
            }
            Write-Host "Already published with matching signed payload: $($package.filename)"
        }
        else { $pending += $candidate }
    }
    if ($Push) {
        if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) { throw 'Missing trusted NuGet API key.' }
        foreach ($path in $pending) {
            # No skip-duplicate: a concurrent conflicting publish must fail and be checked on a rerun.
            & dotnet nuget push $path --api-key $env:NUGET_API_KEY --source 'https://api.nuget.org/v3/index.json'
            if ($LASTEXITCODE -ne 0) { throw "NuGet publish failed: $path" }
        }
    }
    else { Write-Host "Publication preflight passed; $($pending.Count) packages require publishing." }
}
finally {
    $resolved = [IO.Path]::GetFullPath($temp)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ((Split-Path $resolved -Parent) -ne $tempRoot -or (Split-Path $resolved -Leaf) -notmatch '^nuget-existing-[a-f0-9]{32}$') { throw 'Unsafe temporary cleanup target.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
