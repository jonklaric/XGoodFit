param(
    [string]$Configuration = "Debug",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$failures = New-Object System.Collections.Generic.List[string]

function Get-RelativePath([string]$Path) {
    $root = (Resolve-Path $repoRoot).Path.TrimEnd('\', '/')
    $resolved = (Resolve-Path $Path).Path
    if ($resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $resolved.Substring($root.Length).TrimStart('\', '/').Replace("\", "/")
    }

    return $resolved.Replace("\", "/")
}

function Add-Failure([string]$Message) {
    [void]$failures.Add($Message)
}

$codeRoots = @(
    "src/DAG",
    "src/DAG.Demo",
    "src/DAGControls",
    "src/DatabaseControls",
    "src/DatabaseControls.Demo",
    "src/DatabaseManager",
    "src/ExpressionParser",
    "src/ExpressionParserControls",
    "src/ExpressionParserControls.Demo",
    "src/FrameworkInterfaces",
    "src/FrameworkUI",
    "src/FrameworkUI.Demo",
    "src/GenericControls",
    "src/GenericControls.Demo",
    "src/NumericControls",
    "src/NumericControls.Demo",
    "src/OxyPlotControls",
    "src/OxyPlotControls.Demo",
    "src/SoftwareUpdate",
    "src/SoftwareUpdate.Updater",
    "src/Themes"
)

$sourceFiles = @(foreach ($root in $codeRoots) {
    $path = Join-Path $repoRoot $root
    if (Test-Path $path) {
        Get-ChildItem -Path $path -Recurse -Filter *.cs -File |
            Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
    }
})

function Test-HasXmlDocumentation($lines, [int]$index) {
    $j = $index - 1
    while ($j -ge 0 -and [string]::IsNullOrWhiteSpace($lines[$j])) {
        $j--
    }

    if ($j -ge 0 -and $lines[$j].Trim().EndsWith("]")) {
        $depth = 0
        while ($j -ge 0) {
            $trimmed = $lines[$j].Trim()
            if ($trimmed.EndsWith("]")) {
                $depth++
            }

            if ($trimmed.StartsWith("[")) {
                $depth--
            }

            $j--
            while ($j -ge 0 -and
                ([string]::IsNullOrWhiteSpace($lines[$j]) -or
                ($lines[$j].TrimStart().StartsWith("//") -and -not $lines[$j].TrimStart().StartsWith("///")))) {
                $j--
            }

            if ($depth -le 0 -and ($j -lt 0 -or -not $lines[$j].Trim().EndsWith("]"))) {
                break
            }
        }
    }

    return ($j -ge 0 -and $lines[$j].TrimStart().StartsWith("///"))
}

$documentedMemberFiles = @($sourceFiles | Where-Object {
    $_.Name -notmatch '\.(Designer|g|g\.i)\.cs$'
})

$accessModifier = '(?:public|internal|private|protected|file)(?:\s+(?:internal|protected))?'
$typeDeclaration = [regex]("^\s*$accessModifier\s+(?:(?:abstract|sealed|static|partial|readonly|unsafe|new|ref)\s+)*(?:class|struct|interface|enum|record(?:\s+(?:class|struct))?)\s+[A-Za-z_][A-Za-z0-9_]*")
$methodDeclaration = [regex]("^\s*$accessModifier\s+(?:(?:static|virtual|override|abstract|async|extern|unsafe|sealed|new|partial)\s+)*(?!class\b|struct\b|interface\b|enum\b|record\b|delegate\b|event\b)(?:[A-Za-z_][A-Za-z0-9_<>\[\],.?]*(?:\s+|[\*&]+\s*))+[A-Za-z_][A-Za-z0-9_]*(?:<[^>]+>)?\s*\(")
$constructorDeclaration = [regex]("^\s*$accessModifier\s+(?:(?:static|extern|unsafe)\s+)*[A-Za-z_][A-Za-z0-9_]*\s*\(")
$operatorDeclaration = [regex]("^\s*$accessModifier\s+(?:(?:static|extern|unsafe)\s+)*(?:[A-Za-z_][A-Za-z0-9_<>\[\],.?]*\s+)?(?:implicit|explicit\s+)?operator\s+")

foreach ($file in $documentedMemberFiles) {
    $lines = [System.IO.File]::ReadAllLines($file.FullName)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $isDocumentedMember =
            $typeDeclaration.IsMatch($line) -or
            $methodDeclaration.IsMatch($line) -or
            $constructorDeclaration.IsMatch($line) -or
            $operatorDeclaration.IsMatch($line)

        if ($isDocumentedMember -and -not (Test-HasXmlDocumentation $lines $i)) {
            $relative = Get-RelativePath $file.FullName
            Add-Failure "${relative}:$($i + 1) is missing XML documentation for a class or method declaration."
        }
    }
}

if (-not $SkipBuild) {
    $projects = @(
        "src/DAG/DAG.csproj",
        "src/DAG.Demo/DAG.Demo.csproj",
        "src/DAGControls/DAGControls.csproj",
        "src/DatabaseControls/DatabaseControls.csproj",
        "src/DatabaseControls.Demo/DatabaseControls.Demo.csproj",
        "src/DatabaseManager/DatabaseManager.csproj",
        "src/ExpressionParser/ExpressionParser.csproj",
        "src/ExpressionParserControls/ExpressionParserControls.csproj",
        "src/ExpressionParserControls.Demo/ExpressionParserControls.Demo.csproj",
        "src/FrameworkInterfaces/FrameworkInterfaces.csproj",
        "src/FrameworkUI/FrameworkUI.csproj",
        "src/FrameworkUI.Demo/FrameworkUI.Demo.csproj",
        "src/GenericControls/GenericControls.csproj",
        "src/GenericControls.Demo/GenericControls.Demo.csproj",
        "src/NumericControls/NumericControls.csproj",
        "src/NumericControls.Demo/NumericControls.Demo.csproj",
        "src/OxyPlotControls/OxyPlotControls.csproj",
        "src/OxyPlotControls.Demo/OxyPlotControls.Demo.csproj",
        "src/SoftwareUpdate/SoftwareUpdate.csproj",
        "src/SoftwareUpdate.Updater/SoftwareUpdate.Updater.csproj",
        "src/Themes/Themes.csproj"
    )

    foreach ($project in $projects) {
        $projectPath = Join-Path $repoRoot $project
        if (-not (Test-Path $projectPath)) {
            Add-Failure "Missing project: $project"
            continue
        }

        $args = @(
            "build",
            $projectPath,
            "-c",
            $Configuration,
            "--no-restore",
            "--no-dependencies",
            "-p:EnforceXmlDocumentation=true",
            "-p:UseSharedCompilation=false",
            "-v:minimal"
        )

        & dotnet @args
        if ($LASTEXITCODE -ne 0) {
            Add-Failure "XML documentation build failed for $project."
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { [Console]::Error.WriteLine($_) }
    exit 1
}

Write-Host "Code XML documentation validation passed."
