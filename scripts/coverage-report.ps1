<#
.SYNOPSIS
    Runs the test suite with code coverage collection and generates an HTML report.
.DESCRIPTION
    Standard coverage workflow: clears prior results, runs `dotnet test` with the
    XPlat Code Coverage collector, then renders an HTML + text summary report via
    ReportGenerator. Runs entirely from the command line, so it never leaves
    Visual Studio's code coverage line-coloring in the editor.
.PARAMETER TestProject
    Path to the test .csproj. If omitted, auto-discovers a single *.Tests.csproj
    under the repo (excluding bin/obj).
.PARAMETER Filter
    Optional dotnet test --filter expression (e.g. "FullyQualifiedName~UploadFileService").
.EXAMPLE
    ./scripts/coverage-report.ps1
.EXAMPLE
    ./scripts/coverage-report.ps1 -Filter "FullyQualifiedName~UploadFileService"
#>
[CmdletBinding()]
param(
    [string]$TestProject,
    [string]$Filter
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot

if (-not $TestProject) {
    $candidates = Get-ChildItem -Path $repoRoot -Recurse -Filter "*.Tests.csproj" -File |
        Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

    if ($candidates.Count -eq 0) {
        throw "No *.Tests.csproj found under $repoRoot. Pass -TestProject explicitly."
    }
    if ($candidates.Count -gt 1) {
        $list = ($candidates | ForEach-Object { $_.FullName }) -join "`n  "
        throw "Multiple test projects found; pass -TestProject explicitly:`n  $list"
    }
    $TestProject = $candidates[0].FullName
}
else {
    $TestProject = (Resolve-Path $TestProject).Path
}

if (-not (Get-Command reportgenerator -ErrorAction SilentlyContinue)) {
    throw "reportgenerator not found on PATH. Install it with: dotnet tool install -g dotnet-reportgenerator-globaltool"
}

$testResultsDir = Join-Path (Split-Path -Parent $TestProject) "TestResults"
$coverageReportDir = Join-Path $repoRoot "CoverageReport"

if (Test-Path $testResultsDir) { Remove-Item -Recurse -Force $testResultsDir }
if (Test-Path $coverageReportDir) { Remove-Item -Recurse -Force $coverageReportDir }

$testArgs = @($TestProject, '--collect:XPlat Code Coverage')
if ($Filter) { $testArgs += @('--filter', $Filter) }

& dotnet test @testArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet test failed with exit code $LASTEXITCODE"
}

$coverageFile = Get-ChildItem -Path $testResultsDir -Recurse -Filter "coverage.cobertura.xml" -File |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $coverageFile) {
    throw "No coverage.cobertura.xml produced under $testResultsDir"
}

& reportgenerator "-reports:$($coverageFile.FullName)" "-targetdir:$coverageReportDir" "-reporttypes:Html;TextSummary"

Write-Host ""
Write-Host "Coverage report: $coverageReportDir\index.html"
Write-Host ""
Get-Content (Join-Path $coverageReportDir "Summary.txt")
