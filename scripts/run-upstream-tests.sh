#!/usr/bin/env bash
# Runs the vendored upstream test suites that cover the portable (OS-independent) layers on the
# current machine, with the exclusions documented below. Used by CI and for local checks on any OS.
# Logs go to artifacts/logs/test-<suite>.log. RMC-BestFit builds against the vendored RMC.Numerics
# source (Directory.Build.rsp).
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"
logs="$root/artifacts/logs"
mkdir -p "$logs"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 VSTEST_CONNECTION_TIMEOUT=600
failed=0

is_windows() { [[ "${OS:-}" == "Windows_NT" ]]; }

run() {
  local name="$1"; shift
  local log="$logs/test-$name.log" status=0
  echo "==> $name"
  "$@" > "$log" 2>&1 || status=$?
  grep -E "Passed!|Failed!|^\s+(total|failed|succeeded|skipped):" "$log" | tail -4
  # A suite that discovers no tests exits 0; treat that as a failure, not a pass.
  if grep -q "No test is available" "$log"; then
    echo "    ERROR: no tests ran"
    status=1
  fi
  if [[ $status -ne 0 ]]; then
    echo "    FAILED, see ${log#"$root/"}"
    failed=1
  fi
}

# RMC.Numerics, the numerical library under the model (vendored at tag v2.2.0). Excluded:
#  - LiveServiceIntegration: downloads from live web services (USGS, BOM, ...), which fail whenever a
#    service is down (seen: USGS HTTP 503). Upstream's own CI excludes them the same way.
#  - On arm64 only, three tests that assert bit-for-bit equality with reference values recorded on
#    x64. Their results differ in the last digits (about 1e-13 relative), as floating-point maths
#    does across architectures; the model's tolerance-based tests pass on arm64.
numerics_filter="TestCategory!=LiveServiceIntegration"
case "$(uname -m)" in
  arm64|aarch64)
    numerics_filter+="&Name!=Test_NUTS_GradientReuse_AdaptedMetric_ReproducesReferenceDrawsBitwise"
    numerics_filter+="&Name!=Test_NUTS_GradientReuse_FixedMetric_ReproducesReferenceDrawsBitwise"
    numerics_filter+="&Name!=Weibull_ordinary_PreservesBaselineConstraints"
    ;;
esac
run Test_Numerics dotnet test upstream/Numerics/Test_Numerics/Test_Numerics.csproj -c Release -f net10.0 --filter "$numerics_filter"

# RMC.BestFit model library, run directly so --filter reaches the test runner. Excluded:
#  - Documentation_LocalLinksResolve: checks links into upstream folders that are deliberately
#    not vendored (examples, validation evidence, skill packages).
#  - RunAsync_MultipleAnalyses_Parallel: asserts a 250 ms wall-clock limit, so it fails
#    intermittently on busy machines (seen on Windows and Linux); it measures speed, not results.
#  - XYOrdinatesChange_EstimatedAnalysis_PreservesMcmcResults: races with itself. Setting
#    XYOrdinates starts a fire-and-forget reprocess (AnalysisBase.ReprocessIfEstimated) while the
#    test calls CreateFrequencyAnalysisResultsAsync directly, so it fails intermittently (seen once
#    on a Windows CI runner).
bestfit="upstream/RMC-BestFit/src/RMC.BestFit.Tests"
bestfit_filter="FullyQualifiedName!=RMC.BestFit.Tests.Documentation.TechnicalReferenceDocumentationTests.Documentation_LocalLinksResolve"
bestfit_filter+="&FullyQualifiedName!=RMC.BestFit.Tests.BatchAnalysis.BatchAnalysisRunnerTests.RunAsync_MultipleAnalyses_Parallel"
bestfit_filter+="&FullyQualifiedName!=RMC.BestFit.Tests.Bivariate.BivariateAnalysisXYOrdinatesReprocessTests.XYOrdinatesChange_EstimatedAnalysis_PreservesMcmcResults"
if dotnet build "$bestfit/RMC.BestFit.Tests.csproj" -c Release > "$logs/build-RMC.BestFit.Tests.log" 2>&1; then
  run RMC.BestFit.Tests dotnet "$bestfit/bin/Release/net10.0/RMC.BestFit.Tests.dll" --filter "$bestfit_filter"
else
  echo "==> RMC.BestFit.Tests: build FAILED, see artifacts/logs/build-RMC.BestFit.Tests.log"
  failed=1
fi

run DatabaseManager.Tests dotnet test upstream/WPF-Framework/src/DatabaseManager.Tests/DatabaseManager.Tests.csproj -c Release
run ExpressionParser.Tests dotnet test upstream/WPF-Framework/src/ExpressionParser.Tests/ExpressionParser.Tests.csproj -c Release

# OxyPlot core. The test assembly requires an STA thread ([assembly: Apartment(ApartmentState.STA)]),
# which only Windows provides; elsewhere NUnit silently runs nothing. Three SVG baselines expect
# en-US dates (OxyPlot formats axis labels in the OS culture), so they are skipped on other cultures.
if is_windows; then
  oxyplot=(dotnet test upstream/WPF-Framework/tests/OxyPlot.Tests/OxyPlot.Tests.csproj -c Release)
  culture="$(powershell -NoProfile -Command "(Get-Culture).Name" 2>/dev/null | tr -d '\r')"
  if [[ "$culture" != "en-US" ]]; then
    echo "    (culture $culture: skipping 3 DateTimeAxis SVG baselines that assume en-US dates)"
    oxyplot+=(--filter "Name!=C01_DateTimeAxis&Name!=C02_DateTimeAxis_WithSomeUndefinedPoints&Name!=C03_DateTimeAxis_WithAllUndefinedPoints")
  fi
  run OxyPlot.Tests "${oxyplot[@]}"
else
  echo "==> OxyPlot.Tests skipped: the test assembly requires an STA thread (Windows only)"
fi

exit $failed
