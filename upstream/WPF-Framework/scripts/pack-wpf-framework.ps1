param(
    [string]$Configuration = "Release",
    [string]$OutputDirectory = "artifacts/packages",
    [string]$Version,
    [switch]$SkipRestore,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$outputPath = Join-Path $repoRoot $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputPath | Out-Null

$versionArgs = @()
if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $versionArgs = @("/p:Version=$Version")
}

if (-not $SkipRestore) {
    & dotnet restore (Join-Path $repoRoot "WPF-Framework.sln")
    if ($LASTEXITCODE -ne 0) {
        throw "Restore failed before packing."
    }
}

if (-not $SkipBuild) {
    & dotnet build (Join-Path $repoRoot "WPF-Framework.sln") -c $Configuration --no-restore @versionArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed before packing."
    }
}

$packageProjects = @(
    "src/Packaging/RMC.Wpf.Framework.Core/RMC.Wpf.Framework.Core.csproj",
    "src/Packaging/RMC.Wpf.Framework.Models/RMC.Wpf.Framework.Models.csproj",
    "src/Packaging/RMC.Wpf.Framework.Support/RMC.Wpf.Framework.Support.csproj",
    "src/Packaging/RMC.Wpf.Framework.Controls/RMC.Wpf.Framework.Controls.csproj"
)

foreach ($project in $packageProjects) {
    & dotnet pack (Join-Path $repoRoot $project) -c $Configuration --no-build --no-restore -o $outputPath @versionArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Package creation failed for $project."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

$expectedEntries = @{
    "RMC.Wpf.Framework.Core" = @(
        "lib/net10.0-windows7.0/FrameworkInterfaces.dll",
        "lib/net10.0-windows7.0/Themes.dll")
    "RMC.Wpf.Framework.Models" = @(
        "lib/net10.0-windows7.0/DAG.dll",
        "lib/net10.0-windows7.0/DatabaseManager.dll",
        "lib/net10.0-windows7.0/ExpressionParser.dll",
        "lib/net10.0-windows7.0/OxyPlot.dll",
        "lib/net10.0-windows7.0/OxyPlot.Wpf.dll",
        "lib/net10.0-windows7.0/OxyPlot.Wpf.Shared.dll")
    "RMC.Wpf.Framework.Support" = @(
        "lib/net10.0-windows7.0/SoftwareUpdate.dll",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.exe",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.dll",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.deps.json",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.runtimeconfig.json")
    "RMC.Wpf.Framework.Controls" = @(
        "lib/net10.0-windows7.0/GenericControls.dll",
        "lib/net10.0-windows7.0/FrameworkUI.dll",
        "lib/net10.0-windows7.0/DAGControls.dll",
        "lib/net10.0-windows7.0/DatabaseControls.dll",
        "lib/net10.0-windows7.0/ExpressionParserControls.dll",
        "lib/net10.0-windows7.0/NumericControls.dll",
        "lib/net10.0-windows7.0/OxyPlotControls.dll",
        "lib/net10.0-windows7.0/Xceed.Wpf.AvalonDock.dll",
        "lib/net10.0-windows7.0/Xceed.Wpf.AvalonDock.Themes.VS2013.dll")
}

$expectedVersionedEntries = @{
    "RMC.Wpf.Framework.Core" = @(
        "lib/net10.0-windows7.0/FrameworkInterfaces.dll",
        "lib/net10.0-windows7.0/Themes.dll")
    "RMC.Wpf.Framework.Models" = @(
        "lib/net10.0-windows7.0/DAG.dll",
        "lib/net10.0-windows7.0/DatabaseManager.dll",
        "lib/net10.0-windows7.0/ExpressionParser.dll")
    "RMC.Wpf.Framework.Support" = @(
        "lib/net10.0-windows7.0/SoftwareUpdate.dll",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.exe",
        "contentFiles/any/net10.0-windows7.0/SoftwareUpdate.Updater.dll")
    "RMC.Wpf.Framework.Controls" = @(
        "lib/net10.0-windows7.0/GenericControls.dll",
        "lib/net10.0-windows7.0/FrameworkUI.dll",
        "lib/net10.0-windows7.0/DAGControls.dll",
        "lib/net10.0-windows7.0/DatabaseControls.dll",
        "lib/net10.0-windows7.0/ExpressionParserControls.dll",
        "lib/net10.0-windows7.0/NumericControls.dll",
        "lib/net10.0-windows7.0/OxyPlotControls.dll")
}

$expectedReleaseNotes = @{
    "RMC.Wpf.Framework.Core" = "Version 1.0.5 keeps element display names synchronized when names change during undo or redo."
    "RMC.Wpf.Framework.Models" = "Version 1.0.5 releases SQLite connections when initialization or database operations fail."
    "RMC.Wpf.Framework.Support" = "Coordinated WPF Framework 1.0.5 release; no package-specific functional changes."
    "RMC.Wpf.Framework.Controls" = "Version 1.0.5 keeps compaction active until database work completes, reports maintenance errors, and updates FrameworkUI demo workflows."
}

$expectedPackageDependencies = @{
    "RMC.Wpf.Framework.Controls" = @{
        "RMC.Numerics" = "[2.1.4,3.0.0)"
    }
}

foreach ($packageId in $expectedEntries.Keys) {
    $packageFilter = if ([string]::IsNullOrWhiteSpace($Version)) {
        "$packageId.*.nupkg"
    }
    else {
        "$packageId.$Version.nupkg"
    }

    $package = Get-ChildItem -Path $outputPath -Filter $packageFilter |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($package -eq $null) {
        throw "Missing package for $packageId."
    }

    $zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
    try {
        $entries = @($zip.Entries | ForEach-Object { $_.FullName })
        foreach ($expectedEntry in $expectedEntries[$packageId]) {
            if ($entries -notcontains $expectedEntry) {
                throw "$($package.Name) is missing $expectedEntry."
            }
        }

        $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -like "*.nuspec" } | Select-Object -First 1
        if ($nuspecEntry -eq $null) {
            throw "$($package.Name) is missing its nuspec metadata."
        }

        $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
        try {
            [xml]$nuspec = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }

        $namespaceManager = [System.Xml.XmlNamespaceManager]::new($nuspec.NameTable)
        $namespaceManager.AddNamespace("n", $nuspec.DocumentElement.NamespaceURI)
        $packageVersion = $nuspec.SelectSingleNode("/n:package/n:metadata/n:version", $namespaceManager).InnerText
        $releaseNotes = $nuspec.SelectSingleNode("/n:package/n:metadata/n:releaseNotes", $namespaceManager).InnerText

        if (-not [string]::IsNullOrWhiteSpace($Version) -and $packageVersion -ne $Version) {
            throw "$($package.Name) declares version $packageVersion instead of $Version."
        }

        if ($releaseNotes -ne $expectedReleaseNotes[$packageId]) {
            throw "$($package.Name) has unexpected NuGet release notes."
        }

        if ($expectedPackageDependencies.ContainsKey($packageId)) {
            foreach ($dependencyId in $expectedPackageDependencies[$packageId].Keys) {
                $dependency = $nuspec.SelectSingleNode("/n:package/n:metadata/n:dependencies/n:group/n:dependency[@id='$dependencyId']", $namespaceManager)
                if ($dependency -eq $null) {
                    throw "$($package.Name) is missing NuGet dependency $dependencyId."
                }

                $dependencyVersion = $dependency.Attributes["version"].Value
                $expectedDependencyVersion = $expectedPackageDependencies[$packageId][$dependencyId]
                $normalizedDependencyVersion = $dependencyVersion.Replace(" ", "")
                $normalizedExpectedDependencyVersion = $expectedDependencyVersion.Replace(" ", "")
                if ($normalizedDependencyVersion -ne $normalizedExpectedDependencyVersion) {
                    throw "$($package.Name) declares $dependencyId dependency $dependencyVersion instead of $expectedDependencyVersion."
                }
            }
        }

        $numericVersion = ($packageVersion -split '-')[0]
        $expectedBinaryVersion = "$numericVersion.0"
        $inspectionDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "wpf-framework-package-$([Guid]::NewGuid().ToString('N'))"
        New-Item -ItemType Directory -Path $inspectionDirectory | Out-Null

        try {
            foreach ($payloadEntryName in $expectedVersionedEntries[$packageId]) {
                $payloadEntry = $zip.GetEntry($payloadEntryName)
                $payloadPath = Join-Path $inspectionDirectory ([System.IO.Path]::GetFileName($payloadEntryName))
                [System.IO.Compression.ZipFileExtensions]::ExtractToFile($payloadEntry, $payloadPath, $false)
                $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($payloadPath)

                if ($versionInfo.FileVersion -ne $expectedBinaryVersion) {
                    throw "$($package.Name) payload $payloadEntryName has file version $($versionInfo.FileVersion) instead of $expectedBinaryVersion."
                }

                if ([string]::IsNullOrWhiteSpace($versionInfo.ProductVersion) -or
                    -not $versionInfo.ProductVersion.StartsWith($packageVersion, [System.StringComparison]::OrdinalIgnoreCase)) {
                    throw "$($package.Name) payload $payloadEntryName has product version $($versionInfo.ProductVersion) instead of $packageVersion."
                }

                if ([System.IO.Path]::GetExtension($payloadPath).Equals(".dll", [System.StringComparison]::OrdinalIgnoreCase)) {
                    $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($payloadPath).Version.ToString()
                    if ($assemblyVersion -ne $expectedBinaryVersion) {
                        throw "$($package.Name) payload $payloadEntryName has assembly version $assemblyVersion instead of $expectedBinaryVersion."
                    }
                }
            }
        }
        finally {
            Remove-Item -LiteralPath $inspectionDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    finally {
        $zip.Dispose()
    }
}

Write-Host "Packed and validated $($expectedEntries.Count) RMC WPF Framework packages in $outputPath."
