<#
.SYNOPSIS
Defines fail-closed identity, receipt, package and publication comparison contracts.
#>
# Shared fail-closed release artifact contracts. No build, restore or package creation is permitted here.
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:ReleasePackageIds = @('RMC.Wpf.Framework.Core','RMC.Wpf.Framework.Models','RMC.Wpf.Framework.Support','RMC.Wpf.Framework.Controls')

<#
.SYNOPSIS
Rejects any case-sensitive identity or contract mismatch.
#>
function Assert-ReleaseEqual {
    param($Actual, $Expected, [string]$Label)
    if ([string]$Actual -cne [string]$Expected) { throw "$Label mismatch: expected '$Expected', received '$Actual'." }
}

<#
.SYNOPSIS
Returns a lower-case SHA256 digest for an immutable evidence file.
#>
function Get-ReleaseSha256 {
    param([string]$Path)
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

<#
.SYNOPSIS
Requires a stable three-part version and its corresponding tag.
#>
function Assert-StableReleaseVersion {
    param([string]$Tag, [string]$Version)
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Only stable three-part versions are publishable.' }
    Assert-ReleaseEqual $Tag "v$Version" 'Tag/version'
}

<#
.SYNOPSIS
Reads a JSON evidence document without executing its content.
#>
function Read-ReleaseJson {
    param([string]$Path)
    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -Depth 100
}

<#
.SYNOPSIS
Hashes extracted ZIP entries and rejects ambiguous or unsafe entries.
#>
function Get-PackageEntryHashes {
    param([string]$Path, [switch]$RequireSignature)
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $entries = @{}
        $seen = @{}
        $signed = $false
        foreach ($entry in $archive.Entries) {
            if ($entry.FullName.EndsWith('/')) { continue }
            if ($seen.ContainsKey($entry.FullName)) { throw "Duplicate ZIP entry: $($entry.FullName)" }
            $seen[$entry.FullName] = $true
            if ($entry.FullName -match '(^/|\\|(^|/)\.\.(/|$))') { throw 'Unsafe ZIP entry.' }
            if ($entry.FullName -ceq '.signature.p7s') { $signed = $true; continue }
            $stream = $entry.Open()
            try {
                $hash = [Security.Cryptography.SHA256]::Create()
                try { $entries[$entry.FullName] = [Convert]::ToHexString($hash.ComputeHash($stream)).ToLowerInvariant() }
                finally { $hash.Dispose() }
            }
            finally { $stream.Dispose() }
        }
        if ($RequireSignature -and !$signed) { throw 'Published package has no NuGet signature.' }
        return $entries
    }
    finally { $archive.Dispose() }
}

<#
.SYNOPSIS
Reads a unique package nuspec and rejects duplicate dependencies.
#>
function Get-PackageMetadata {
    param([string]$Path)
    # Also rejects duplicate/path-traversal entries before reading metadata.
    $null = Get-PackageEntryHashes $Path
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $nuspecs = @($archive.Entries | Where-Object { $_.FullName -match '^[^/]+\.nuspec$' })
        if ($nuspecs.Count -ne 1) { throw 'Exactly one root nuspec is required.' }
        $reader = [IO.StreamReader]::new($nuspecs[0].Open())
        try { [xml]$xml = $reader.ReadToEnd() } finally { $reader.Dispose() }
        $metadata = $xml.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
        if (!$metadata) { throw 'Missing package metadata.' }
        $groups = Get-NuspecDependencyGroups $metadata
        return @{ id = $metadata.SelectSingleNode('*[local-name()="id"]').InnerText; version = $metadata.SelectSingleNode('*[local-name()="version"]').InnerText; dependencyGroups = $groups }
    }
    finally { $archive.Dispose() }
}

<#
.SYNOPSIS
Preserves exact dependency framework groups, including empty groups, and rejects ambiguous membership.
#>
function Get-NuspecDependencyGroups {
    param([System.Xml.XmlElement]$Metadata, [hashtable]$Values = @{})
    $containers = @($Metadata.SelectNodes('*[local-name()="dependencies"]'))
    if ($containers.Count -ne 1) { throw 'Exactly one dependencies container is required.' }
    $groups = @{}
    foreach ($group in $containers[0].ChildNodes) {
        if ($group.NodeType -ne [Xml.XmlNodeType]::Element) { continue }
        if ($group.LocalName -cne 'group' -or !$group.HasAttribute('targetFramework') -or $group.Attributes.Count -ne 1) { throw 'Only explicit dependency framework groups are allowed.' }
        $framework = $group.GetAttribute('targetFramework')
        if ([string]::IsNullOrWhiteSpace($framework) -or $groups.ContainsKey($framework)) { throw 'Missing or duplicate dependency framework group.' }
        $dependencies = @{}
        foreach ($dependency in $group.ChildNodes) {
            if ($dependency.NodeType -ne [Xml.XmlNodeType]::Element) { continue }
            if ($dependency.LocalName -cne 'dependency' -or !$dependency.HasAttribute('id') -or !$dependency.HasAttribute('version') -or $dependency.Attributes.Count -ne 2) { throw 'Unexpected dependency declaration.' }
            $id = $dependency.GetAttribute('id')
            if ([string]::IsNullOrWhiteSpace($id) -or $dependencies.ContainsKey($id)) { throw "Missing or duplicate group dependency: $id" }
            $value = $dependency.GetAttribute('version')
            foreach ($token in [regex]::Matches($value, '\$(?<name>[^$]+)\$')) {
                $key = $token.Groups['name'].Value
                if (!$Values.ContainsKey($key)) { throw "Unknown dependency variable: $key" }
                $value = $value.Replace($token.Value, [string]$Values[$key])
            }
            if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains('$')) { throw "Unresolved or empty dependency version: $value" }
            $dependencies[$id] = $value.Replace(' ','')
        }
        $groups[$framework] = $dependencies
    }
    return $groups
}

<#
.SYNOPSIS
Resolves dependency contracts from tag-source nuspecs and central properties.
#>
function Get-ExpectedReleaseDependencies {
    param([string]$RepositoryRoot, [string]$Id, [string]$Version)
    [xml]$props = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Directory.Packages.props') -Raw
    $values = @{ version = $Version }
    foreach ($property in $props.Project.PropertyGroup.ChildNodes) {
        if ($property.NodeType -eq [Xml.XmlNodeType]::Element) { $values[$property.Name] = $property.InnerText }
    }
    $templatePath = Join-Path $RepositoryRoot "src/Packaging/$Id/$Id.nuspec"
    [xml]$template = Get-Content -LiteralPath $templatePath -Raw
    $metadata = $template.SelectSingleNode('/*[local-name()="package"]/*[local-name()="metadata"]')
    return Get-NuspecDependencyGroups $metadata $values
}

<#
.SYNOPSIS
Normalizes numeric NuGet version components without changing range boundaries or constraint operators.
#>
function Normalize-ReleaseDependencyVersion {
    param([string]$Version)
    # NuGet packing pads numeric versions to three parts and omits a zero fourth revision.
    # Keep brackets, parentheses and commas intact; prerelease/floating text is not rewritten.
    return [regex]::Replace($Version.Replace(' ',''), '(^|[\[(,])([0-9]+(?:\.[0-9]+){0,3})(?=$|[,\])])', {
        param($match)
        $parts = @($match.Groups[2].Value.Split('.') | ForEach-Object { $part = $_.TrimStart('0'); if ($part -eq '') { '0' } else { $part } })
        while ($parts.Count -lt 3) { $parts += '0' }
        if ($parts.Count -eq 4 -and $parts[3] -eq '0') { $parts = @($parts[0..2]) }
        return $match.Groups[1].Value + ($parts -join '.')
    })
}

<#
.SYNOPSIS
Checks a package ID, version and complete expected dependency set.
#>
function Assert-PackageContract {
    param([string]$Path, [string]$Id, [string]$Version, [string]$RepositoryRoot)
    $metadata = Get-PackageMetadata $Path
    Assert-ReleaseEqual $metadata.id $Id 'Package ID'
    Assert-ReleaseEqual $metadata.version $Version 'Package version'
    $expected = Get-ExpectedReleaseDependencies $RepositoryRoot $Id $Version
    Assert-ReleaseEqual $metadata.dependencyGroups.Count $expected.Count 'Dependency framework group count'
    foreach ($framework in $expected.Keys) {
        $actualNames = @($metadata.dependencyGroups.Keys | Where-Object { $_ -ceq $framework })
        if ($actualNames.Count -ne 1) { throw "Missing exact dependency framework group: $framework" }
        $actualGroup = $metadata.dependencyGroups[$framework]
        $expectedGroup = $expected[$framework]
        Assert-ReleaseEqual $actualGroup.Count $expectedGroup.Count "Dependency count in $framework"
        foreach ($id in $expectedGroup.Keys) {
            if (@($actualGroup.Keys | Where-Object { $_ -ceq $id }).Count -ne 1) { throw "Missing exact dependency $id in $framework" }
            Assert-ReleaseEqual (Normalize-ReleaseDependencyVersion $actualGroup[$id]) (Normalize-ReleaseDependencyVersion $expectedGroup[$id]) "Dependency $id in $framework"
        }
    }
}

<#
.SYNOPSIS
Requires exact release identity and all four successful downstream gates.
#>
function Assert-ReleaseReceipt {
    param($Receipt, [string]$Repository, [string]$Tag, [string]$CommitSHA)
    Assert-ReleaseEqual $Receipt.schemaVersion 1 'Receipt schema'
    Assert-ReleaseEqual $Receipt.repository $Repository 'Receipt repository'
    Assert-StableReleaseVersion $Tag $Receipt.version
    Assert-ReleaseEqual $Receipt.frameworkTag $Tag 'Receipt tag'
    Assert-ReleaseEqual $Receipt.frameworkTagSHA $CommitSHA 'Receipt tag SHA'
    foreach ($key in @('runID','runAttempt','artifactID')) {
        if ([string]$Receipt.$key -notmatch '^[1-9]\d*$') { throw "Invalid receipt $key." }
    }
    if ([string]$Receipt.manifestSHA256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid manifest hash.' }
    if ([string]$Receipt.bestFitCommit -notmatch '^[a-f0-9]{40}$') { throw 'Missing BestFit commit identity.' }
    foreach ($approval in @($Receipt.compatibility, $Receipt.uiAcceptance)) {
        Assert-ReleaseEqual $approval.status 'passed' 'Downstream acceptance'
        if ([string]::IsNullOrWhiteSpace([string]$approval.evidence)) { throw 'Missing acceptance evidence.' }
    }
    $requiredSuites = @('RMC.BestFit.Tests','RMC.BestFit.UI.Tests','RMC.BestFit.App.Tests','RMC.BestFit.Api.Tests')
    if (@($Receipt.testResults).Count -ne $requiredSuites.Count) { throw 'All four mandatory downstream suites are required.' }
    $seenSuites = @{}
    foreach ($test in $Receipt.testResults) {
        if ($requiredSuites -cnotcontains $test.suite -or $seenSuites.ContainsKey($test.suite)) { throw 'Unexpected or duplicate downstream suite.' }
        $seenSuites[$test.suite] = $true
        foreach ($count in @('passed','failed','skipped')) {
            if ([string]$test.$count -notmatch '^(0|[1-9]\d*)$') { throw 'Downstream test counts must be nonnegative integers.' }
            $parsedCount = 0L
            if (![long]::TryParse([string]$test.$count, [ref]$parsedCount)) { throw 'Downstream test count exceeds supported range.' }
        }
        if ([long]$test.passed -le 0 -or [long]$test.failed -ne 0 -or [long]$test.skipped -ne 0 -or [string]::IsNullOrWhiteSpace([string]$test.evidence)) { throw 'Every mandatory suite must pass without failures or skips and identify evidence.' }
    }
    Assert-PackageHashRecords $Receipt.packages $Receipt.version
}

<#
.SYNOPSIS
Requires four uniquely identified packages with safe filenames and hashes.
#>
function Assert-PackageHashRecords {
    param($Packages, [string]$Version)
    if (@($Packages).Count -ne 4) { throw 'Exactly four package hash records are required.' }
    $seen = @{}
    foreach ($package in $Packages) {
        if ($script:ReleasePackageIds -cnotcontains $package.id -or $seen.ContainsKey($package.id)) { throw 'Unexpected or duplicate package record.' }
        $seen[$package.id] = $true
        Assert-ReleaseEqual $package.filename "$($package.id).$Version.nupkg" 'Package filename'
        if ([string]$package.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid package hash.' }
    }
}

<#
.SYNOPSIS
Binds an approved receipt to an exact successful workflow attempt and artifact.
#>
function Assert-ReleaseRun {
    param($Receipt, $Run, $Artifact, [string]$Repository, [string]$Tag, [string]$CommitSHA, $Release)
    Assert-ReleaseReceipt $Receipt $Repository $Tag $CommitSHA
    if ($Release.draft -or $Release.prerelease) { throw 'Publishing requires a published stable release.' }
    Assert-ReleaseEqual $Release.tag_name $Tag 'Release tag'
    Assert-ReleaseEqual $Run.id $Receipt.runID 'Run ID'
    Assert-ReleaseEqual $Run.run_attempt $Receipt.runAttempt 'Run attempt'
    Assert-ReleaseEqual $Run.path '.github/workflows/Release.yml' 'Release workflow'
    Assert-ReleaseEqual $Run.event 'push' 'Release run event'
    Assert-ReleaseEqual $Run.head_branch $Tag 'Release run tag'
    Assert-ReleaseEqual $Run.head_sha $CommitSHA 'Release run SHA'
    Assert-ReleaseEqual $Run.status 'completed' 'Release run status'
    Assert-ReleaseEqual $Run.conclusion 'success' 'Release run conclusion'
    Assert-ReleaseEqual $Run.repository.full_name $Repository 'Run repository'
    Assert-ReleaseEqual $Artifact.id $Receipt.artifactID 'Artifact ID'
    Assert-ReleaseEqual $Artifact.name "wpf-framework-packages-$($Receipt.runID)-$($Receipt.runAttempt)" 'Artifact name'
    if ($Artifact.expired -or [DateTimeOffset]::Parse($Artifact.expires_at) -le [DateTimeOffset]::UtcNow) { throw 'Release artifact expired.' }
    Assert-ReleaseEqual $Artifact.workflow_run.id $Receipt.runID 'Artifact run ID'
    Assert-ReleaseEqual $Artifact.workflow_run.head_sha $CommitSHA 'Artifact SHA'
    Assert-ReleaseEqual $Artifact.workflow_run.head_branch $Tag 'Artifact tag'
}

<#
.SYNOPSIS
Checks artifact contents against immutable manifest and receipt hashes.
#>
function Assert-ReleaseArtifact {
    param([string]$Directory, $Receipt, [string]$RepositoryRoot)
    $files = @(Get-ChildItem -LiteralPath $Directory -Recurse -File)
    if ($files.Count -ne 5 -or @(Get-ChildItem -LiteralPath $Directory -Directory).Count -ne 0) { throw 'Artifact must contain exactly four packages and one manifest at its root.' }
    $manifestPath = Join-Path $Directory 'release-manifest.json'
    Assert-ReleaseEqual (Get-ReleaseSha256 $manifestPath) $Receipt.manifestSHA256 'Manifest hash'
    $manifest = Read-ReleaseJson $manifestPath
    Assert-ReleaseEqual $manifest.schemaVersion 1 'Manifest schema'
    foreach ($key in @('repository','version','runID','runAttempt')) { Assert-ReleaseEqual $manifest.$key $Receipt.$key "Manifest $key" }
    Assert-ReleaseEqual $manifest.tag $Receipt.frameworkTag 'Manifest tag'
    Assert-ReleaseEqual $manifest.commitSHA $Receipt.frameworkTagSHA 'Manifest SHA'
    Assert-PackageHashRecords $manifest.packages $Receipt.version
    foreach ($package in $manifest.packages) {
        $receiptPackage = @($Receipt.packages | Where-Object { $_.id -ceq $package.id })
        if ($receiptPackage.Count -ne 1) { throw 'Missing receipt package.' }
        Assert-ReleaseEqual $package.sha256 $receiptPackage[0].sha256 'Receipt package hash'
        $path = Join-Path $Directory $package.filename
        Assert-ReleaseEqual (Get-ReleaseSha256 $path) $package.sha256 'Package content hash'
        Assert-PackageContract $path $package.id $Receipt.version $RepositoryRoot
    }
    return $manifest
}

<#
.SYNOPSIS
Requires verified signatures and byte-identical payload and nuspec entries.
#>
function Assert-PublishedPackageMatches {
    param([string]$Candidate, [string]$Published, [scriptblock]$SignatureVerifier)
    # NuGet.org repository signing changes ZIP bytes; compare all extracted entry hashes including nuspec.
    if (!$SignatureVerifier) { throw 'A signature verifier is required.' }
    & $SignatureVerifier $Published
    $publishedEntries = Get-PackageEntryHashes $Published -RequireSignature
    $candidateEntries = Get-PackageEntryHashes $Candidate
    Assert-ReleaseEqual $publishedEntries.Count $candidateEntries.Count 'Published payload count'
    foreach ($key in $candidateEntries.Keys) {
        Assert-ReleaseEqual $publishedEntries[$key] $candidateEntries[$key] "Published payload $key"
    }
}
