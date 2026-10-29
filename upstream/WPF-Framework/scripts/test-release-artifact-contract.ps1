<#
.SYNOPSIS
Runs controlled fail-closed release contract fixtures without network access or publishing.
#>
# Executable controlled fixtures; no Pester, NuGet push, network or production builds.
. (Join-Path $PSScriptRoot 'release-artifact-contract.ps1')
$script:Checks = 0
$root = Split-Path $PSScriptRoot
$temp = Join-Path ([IO.Path]::GetTempPath()) "release-contract-$([Guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $temp | Out-Null
<#
.SYNOPSIS
Requires a controlled invalid fixture to be rejected.
#>
function Expect-Rejection {
    param([string]$Name, [scriptblock]$Check)
    $rejected = $false
    try { & $Check | Out-Null } catch { $rejected = $true }
    if (!$rejected) { throw "FAIL: $Name accepted invalid evidence." }
    $script:Checks++
    Write-Host "PASS rejection: $Name"
}
<#
.SYNOPSIS
Deep-copies fixture evidence so mutations remain isolated.
#>
function Copy-Fixture {
    param($Object)
    $Object | ConvertTo-Json -Depth 30 | ConvertFrom-Json -Depth 30
}
<#
.SYNOPSIS
Creates a deterministic ZIP and nuspec fixture without real binaries or signatures.
#>
function New-FixturePackage {
    param([string]$Path, [string]$Id, [string]$Version, [switch]$Signature, [switch]$AlterPayload, [switch]$DuplicateEntry, [switch]$BadDependency, [string]$GroupMutation = '')
    if (Test-Path -LiteralPath $Path) { Remove-Item -LiteralPath $Path }
    $groups = Get-ExpectedReleaseDependencies $root $Id $Version
    $groupXml = ''
    foreach ($framework in ($groups.Keys | Sort-Object)) {
        $dependencies = $groups[$framework]
        if ($BadDependency) { $dependencies['Unexpected.Package'] = '9.0.0' }
        if ($GroupMutation -eq 'missing-member' -and $dependencies.Count) { $dependencies.Remove(@($dependencies.Keys)[0]) }
        $dependencyXml = ($dependencies.Keys | Sort-Object | ForEach-Object { '<dependency id="' + $_ + '" version="' + [Security.SecurityElement]::Escape($dependencies[$_]) + '" />' }) -join ''
        if ($GroupMutation -eq 'duplicate-member' -and $dependencies.Count) {
            $member = @($dependencies.Keys)[0]
            $dependencyXml += '<dependency id="' + $member + '" version="' + [Security.SecurityElement]::Escape($dependencies[$member]) + '" />'
        }
        $groupXml += '<group targetFramework="' + $framework + '">' + $dependencyXml + '</group>'
    }
    switch ($GroupMutation) {
        'wrong' { $groupXml = $groupXml.Replace('net10.0-windows7.0','net99.0') }
        'missing' { $groupXml = '' }
        'duplicate' { $groupXml += $groupXml }
        'extra' { $groupXml += '<group targetFramework="net99.0" />' }
        'ungrouped' { $groupXml = $dependencyXml }
        'split-membership' {
            $groupXml = '<group targetFramework="net10.0-windows7.0" /><group targetFramework="net99.0">' + $dependencyXml + '</group>'
        }
    }
    $nuspec = '<package><metadata><id>' + $Id + '</id><version>' + $Version + '</version><dependencies>' + $groupXml + '</dependencies></metadata></package>'
    $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $items = @(@{ name = "$Id.nuspec"; content = $nuspec }, @{ name = 'lib/net10.0-windows7.0/payload.dll'; content = $(if ($AlterPayload) { 'altered bytes' } else { 'fixture bytes' }) })
        if ($Signature) { $items += @{ name = '.signature.p7s'; content = 'controlled fixture signature' } }
        if ($DuplicateEntry) { $items += @{ name = 'lib/net10.0-windows7.0/payload.dll'; content = 'duplicate' } }
        foreach ($item in $items) {
            $entry = $archive.CreateEntry($item.name)
            $writer = [IO.StreamWriter]::new($entry.Open())
            try { $writer.Write($item.content) } finally { $writer.Dispose() }
        }
    }
    finally { $archive.Dispose() }
}
try {
    $directory = Join-Path $temp 'packages'
    New-Item -ItemType Directory -Path $directory | Out-Null
    foreach ($id in $script:ReleasePackageIds) { New-FixturePackage (Join-Path $directory "$id.1.0.5.nupkg") $id '1.0.5' }
    $sha = 'a' * 40
    & (Join-Path $PSScriptRoot 'write-release-manifest.ps1') -Directory $directory -Repository 'USACE-RMC/wpf-framework' -Tag 'v1.0.5' -Version '1.0.5' -CommitSHA $sha -RunID '123' -RunAttempt '2'
    $manifest = Read-ReleaseJson (Join-Path $directory 'release-manifest.json')
    $receipt = [pscustomobject]@{
        schemaVersion = 1; repository = 'USACE-RMC/wpf-framework'; frameworkTag = 'v1.0.5'; version = '1.0.5'; frameworkTagSHA = $sha
        runID = '123'; runAttempt = '2'; artifactID = '456'; manifestSHA256 = Get-ReleaseSha256 (Join-Path $directory 'release-manifest.json')
        packages = $manifest.packages; bestFitCommit = 'b' * 40
        testResults = @(@('RMC.BestFit.Tests','RMC.BestFit.UI.Tests','RMC.BestFit.App.Tests','RMC.BestFit.Api.Tests') | ForEach-Object { @{ suite = $_; passed = 1; failed = 0; skipped = 0; evidence = "fixture://tests/$_" } })
        compatibility = @{ status = 'passed'; evidence = 'fixture://compatibility' }; uiAcceptance = @{ status = 'passed'; evidence = 'fixture://ui' }
    }
    $run = [pscustomobject]@{ id = 123; run_attempt = 2; path = '.github/workflows/Release.yml'; event = 'push'; head_branch = 'v1.0.5'; head_sha = $sha; status = 'completed'; conclusion = 'success'; repository = @{ full_name = $receipt.repository } }
    $artifact = [pscustomobject]@{ id = 456; name = 'wpf-framework-packages-123-2'; expired = $false; expires_at = [DateTimeOffset]::UtcNow.AddDays(1).ToString('o'); workflow_run = @{ id = 123; head_sha = $sha; head_branch = 'v1.0.5' } }
    $release = [pscustomobject]@{ draft = $false; prerelease = $false; tag_name = 'v1.0.5' }
    Assert-ReleaseRun $receipt $run $artifact $receipt.repository 'v1.0.5' $sha $release
    $null = Assert-ReleaseArtifact $directory $receipt $root
    $script:Checks += 2
    Write-Host 'PASS exact run and artifact happy paths'
    foreach ($case in @(@('frameworkTag','v1.0.6'),@('frameworkTagSHA',('c'*40)),@('version','1.0.6'),@('runID','999'),@('runAttempt','9'),@('artifactID','999'),@('repository','another/repository'),@('schemaVersion',2))) {
        $copy = Copy-Fixture $receipt; $copy.($case[0]) = $case[1]
        Expect-Rejection "receipt $($case[0])" { Assert-ReleaseRun $copy $run $artifact $receipt.repository 'v1.0.5' $sha $release }
    }
    foreach ($case in @(@('id',999),@('run_attempt',9),@('head_sha',('c'*40)),@('head_branch','v1.0.6'),@('path','.github/workflows/Other.yml'),@('event','workflow_dispatch'),@('status','in_progress'),@('conclusion','failure'))) {
        $copy = Copy-Fixture $run; $copy.($case[0]) = $case[1]
        Expect-Rejection "run $($case[0])" { Assert-ReleaseRun $receipt $copy $artifact $receipt.repository 'v1.0.5' $sha $release }
    }
    foreach ($case in @(@('id',999),@('name','wpf-framework-packages-123-1'),@('expired',$true),@('expires_at',[DateTimeOffset]::UtcNow.AddDays(-1).ToString('o')))) {
        $copy = Copy-Fixture $artifact; $copy.($case[0]) = $case[1]
        Expect-Rejection "artifact $($case[0])" { Assert-ReleaseRun $receipt $run $copy $receipt.repository 'v1.0.5' $sha $release }
    }
    foreach ($flag in @('draft','prerelease')) {
        $copy = Copy-Fixture $release; $copy.$flag = $true
        Expect-Rejection "release $flag" { Assert-ReleaseRun $receipt $run $artifact $receipt.repository 'v1.0.5' $sha $copy }
    }
    $copy = Copy-Fixture $artifact; $copy.workflow_run.id = 999
    Expect-Rejection 'artifact belongs to wrong run' { Assert-ReleaseRun $receipt $run $copy $receipt.repository 'v1.0.5' $sha $release }
    $copy = Copy-Fixture $artifact; $copy.workflow_run.head_sha = 'c'*40
    Expect-Rejection 'artifact wrong commit' { Assert-ReleaseRun $receipt $run $copy $receipt.repository 'v1.0.5' $sha $release }
    $copy = Copy-Fixture $artifact; $copy.workflow_run.head_branch = 'v1.0.6'
    Expect-Rejection 'artifact wrong tag' { Assert-ReleaseRun $receipt $run $copy $receipt.repository 'v1.0.5' $sha $release }
    $copy = Copy-Fixture $receipt; $copy.PSObject.Properties.Remove('artifactID')
    Expect-Rejection 'missing receipt artifact identity' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    Expect-Rejection 'prerelease version' { Assert-StableReleaseVersion 'v1.0.5-rc.1' '1.0.5-rc.1' }
    $copy = Copy-Fixture $receipt; $copy.compatibility.status = 'failed'
    Expect-Rejection 'downstream compatibility failed' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.uiAcceptance.evidence = ''
    Expect-Rejection 'missing UI evidence' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.testResults[0].failed = 1
    Expect-Rejection 'downstream test failure' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.testResults = @($copy.testResults[0..2])
    Expect-Rejection 'missing mandatory downstream suite' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.testResults[1].suite = $copy.testResults[0].suite
    Expect-Rejection 'duplicate downstream suite' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.testResults[0].suite = 'Unknown.Tests'
    Expect-Rejection 'unknown downstream suite' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.testResults[0].skipped = 1
    Expect-Rejection 'skipped downstream test' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    foreach ($count in @('passed','failed','skipped')) {
        $copy = Copy-Fixture $receipt; $copy.testResults[0].$count = 0.1
        Expect-Rejection "nonintegral downstream $count count" { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    }
    $copy = Copy-Fixture $receipt; $copy.packages[0].sha256 = 'c'*64
    Expect-Rejection 'receipt package hash mismatch' { Assert-ReleaseArtifact $directory $copy $root }
    $copy = Copy-Fixture $receipt; $copy.manifestSHA256 = 'c'*64
    Expect-Rejection 'manifest hash mismatch' { Assert-ReleaseArtifact $directory $copy $root }
    $copy = Copy-Fixture $receipt; $copy.packages[1] = $copy.packages[0]
    Expect-Rejection 'duplicate package records' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    $copy = Copy-Fixture $receipt; $copy.packages = @($copy.packages[0..2])
    Expect-Rejection 'missing package record' { Assert-ReleaseReceipt $copy $receipt.repository 'v1.0.5' $sha }
    Set-Content -LiteralPath (Join-Path $directory 'extra.nupkg') -Value 'extra'
    Expect-Rejection 'extraneous package file' { Assert-ReleaseArtifact $directory $receipt $root }
    Remove-Item -LiteralPath (Join-Path $directory 'extra.nupkg')
    $candidate = Join-Path $directory 'RMC.Wpf.Framework.Core.1.0.5.nupkg'
    $backup = [IO.File]::ReadAllBytes($candidate)
    Remove-Item -LiteralPath $candidate
    Expect-Rejection 'missing package file' { Assert-ReleaseArtifact $directory $receipt $root }
    [IO.File]::WriteAllBytes($candidate, $backup)
    New-FixturePackage $candidate 'RMC.Wpf.Framework.Core' '1.0.5' -AlterPayload
    Expect-Rejection 'altered package bytes' { Assert-ReleaseArtifact $directory $receipt $root }
    [IO.File]::WriteAllBytes($candidate, $backup)
    $published = Join-Path $temp 'published.nupkg'
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.5' -Signature
    $script:VerifierCalled = $false
    Assert-PublishedPackageMatches $candidate $published { param($path); $script:VerifierCalled = $true }
    if (!$script:VerifierCalled) { throw 'Signature verification was skipped.' }
    $script:Checks++
    Write-Host 'PASS signature-verified repacked matching payload is idempotent'
    Expect-Rejection 'signature verification failed' { Assert-PublishedPackageMatches $candidate $published { param($path); throw 'invalid signature' } }
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.5'
    Expect-Rejection 'unsigned existing package' { Assert-PublishedPackageMatches $candidate $published { param($path) } }
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.5' -Signature -AlterPayload
    Expect-Rejection 'existing version conflicting payload' { Assert-PublishedPackageMatches $candidate $published { param($path) } }
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.6' -Signature
    Expect-Rejection 'existing version conflicting nuspec' { Assert-PublishedPackageMatches $candidate $published { param($path) } }
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.5' -DuplicateEntry
    Expect-Rejection 'duplicate ZIP entries' { Get-PackageMetadata $published }
    New-FixturePackage $published 'RMC.Wpf.Framework.Models' '1.0.5' -BadDependency
    Expect-Rejection 'unexpected dependency' { Assert-PackageContract $published 'RMC.Wpf.Framework.Models' '1.0.5' $root }
    New-FixturePackage $published 'RMC.Wpf.Framework.Core' '1.0.6'
    Expect-Rejection 'package nuspec wrong version' { Assert-PackageContract $published 'RMC.Wpf.Framework.Core' '1.0.5' $root }
    foreach ($pair in @(@('1.1','1.1.0'),@('1','1.0.0'),@('1.1.0.0','1.1.0'),@('[1.1,2.0)','[1.1.0,2.0.0)'),@('[1.1]','[1.1.0]'))) {
        Assert-ReleaseEqual (Normalize-ReleaseDependencyVersion $pair[0]) (Normalize-ReleaseDependencyVersion $pair[1]) 'Canonical numeric dependency version'
        $script:Checks++
    }
    foreach ($pair in @(@('[1.1,2.0)','[1.1,2.0]'),@('[1.1]','1.1'),@('[1.1,2.0)','(1.1,2.0)'),@('1.1','1.2'),@('1.1.0.1','1.1.0'))) {
        Expect-Rejection "Changed version constraint $($pair[0]) vs $($pair[1])" { Assert-ReleaseEqual (Normalize-ReleaseDependencyVersion $pair[0]) (Normalize-ReleaseDependencyVersion $pair[1]) 'Dependency constraint' }
    }
    New-FixturePackage $published 'RMC.Wpf.Framework.Models' '1.0.5'
    $zip = [IO.Compression.ZipFile]::Open($published, [IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = $zip.GetEntry('RMC.Wpf.Framework.Models.nuspec')
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $canonicalNuspec = $reader.ReadToEnd().Replace('id="ExcelNumberFormat" version="1.1"','id="ExcelNumberFormat" version="1.1.0"') } finally { $reader.Dispose() }
        $entry.Delete()
        $writer = [IO.StreamWriter]::new($zip.CreateEntry('RMC.Wpf.Framework.Models.nuspec').Open())
        try { $writer.Write($canonicalNuspec) } finally { $writer.Dispose() }
    }
    finally { $zip.Dispose() }
    Assert-PackageContract $published 'RMC.Wpf.Framework.Models' '1.0.5' $root
    $script:Checks++
    Write-Host 'PASS packed canonical ExcelNumberFormat1.1.0 dependency'
    foreach ($id in $script:ReleasePackageIds) {
        foreach ($mutation in @('wrong','missing','duplicate','extra')) {
            New-FixturePackage $published $id '1.0.5' -GroupMutation $mutation
            Expect-Rejection "$id $mutation framework group" { Assert-PackageContract $published $id '1.0.5' $root }
        }
    }
    foreach ($id in @('RMC.Wpf.Framework.Models','RMC.Wpf.Framework.Controls')) {
        foreach ($mutation in @('ungrouped','split-membership','missing-member','duplicate-member')) {
            New-FixturePackage $published $id '1.0.5' -GroupMutation $mutation
            Expect-Rejection "$id $mutation dependency membership" { Assert-PackageContract $published $id '1.0.5' $root }
        }
    }
    Write-Host "PASS: $script:Checks release artifact contract checks; no network or NuGet publishing."
}
finally {
    $resolved = [IO.Path]::GetFullPath($temp)
    $tempRoot = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ((Split-Path $resolved -Parent) -ne $tempRoot -or (Split-Path $resolved -Leaf) -notmatch '^release-contract-[a-f0-9]{32}$') { throw 'Unsafe temporary cleanup target.' }
    Remove-Item -LiteralPath $resolved -Recurse -Force
}
