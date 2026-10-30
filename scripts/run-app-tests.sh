#!/usr/bin/env bash
# Runs RMC.BestFit.App.Tests (the application's views, headlessly) in several processes: each of
# XGoodFit's own test classes (tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/*Tests.cs and *Captures.cs)
# on its own, then every other test together. In one process, the views and example projects that
# the classes open add up to more memory than CI's runners have: the Linux runner was shut down, and
# the Windows and macOS jobs ran for over 90 minutes. Tests in category External (live web services)
# are left out, as dotnet test leaves them out. Used by CI; dotnet test skips the project when
# -p:XGoodFitAppTestsInScript=true is passed. Build first. Logs go to artifacts/logs/app-tests/.
# Usage: scripts/run-app-tests.sh [configuration [group...]], where a group is one of those classes
# or Others; all groups by default.
set -uo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"
config="${1:-Release}"
selected=" ${*:2} "
project="tests/BestFit/RMC.BestFit.App.Tests"
bin="$project/bin/$config/net10.0"
logs="$root/artifacts/logs/app-tests"
mkdir -p "$logs"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

exe="$bin/RMC.BestFit.App.Tests"
[[ -f "$exe.exe" ]] && exe="$exe.exe"
if [[ ! -f "$exe" ]]; then
  echo "ERROR: $exe not found; build the tests first (dotnet build XGoodFit.slnx -c $config)."
  exit 1
fi

failed=0
ran=0

run() {
  local name="$1" filter="$2"
  local log="$logs/$name.log" status=0 start=$SECONDS
  [[ "$selected" == "  " || "$selected" == *" $name "* ]] || return 0
  "$exe" --filter "$filter" > "$log" 2>&1 || status=$?
  # Exit code 8: no test matched (a class whose tests are all External).
  [[ $status -eq 8 ]] && status=0
  local summary
  summary="$(grep -E "^\s+(total|failed|succeeded|skipped):" "$log" | tr -s ' ' | tr '\n' ' ')"
  echo "==> $name ($((SECONDS - start)) s):$summary"
  if [[ $status -ne 0 ]]; then
    grep -E "^failed " "$log" | head -20
    echo "    FAILED (exit $status), see ${log#"$root/"}"
    failed=1
  fi
  ran=$((ran + 1))
}

# XGoodFit's test classes, one process each. A file is a class of the same name.
rest="TestCategory!=External"
for file in "$project"/XGoodFit/*Tests.cs "$project"/XGoodFit/*Captures.cs; do
  [[ -f "$file" ]] || continue
  class="$(basename "$file" .cs)"
  grep -q "\[TestClass\]" "$file" && grep -Eq "class $class([^A-Za-z0-9_]|$)" "$file" || continue
  run "$class" "FullyQualifiedName~.XGoodFit.$class.&TestCategory!=External"
  rest+="&FullyQualifiedName!~.XGoodFit.$class."
done

# Everything else: upstream's tests (support classes and ported copies), which are light.
run "Others" "$rest"

if [[ $failed -ne 0 ]]; then
  echo "App tests FAILED"
  exit 1
fi
echo "App tests passed ($ran processes)."
