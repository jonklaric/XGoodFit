#!/usr/bin/env bash
# Builds HEC-DSS's native hecdss library for XGoodFit from the vendored sources: hec-dss 7-JA-7
# (upstream/hec-dss) with the vendored zlib (upstream/zlib) linked statically. Uses CMake and a C
# compiler only, and no network access. native/README.md explains the build.
#
# Usage: native/build-hecdss.sh [--test] [--clean] [rid...]
#   rid      win-x64, linux-x64, osx-arm64 or osx-x64 (default: this machine's). macOS builds either
#            architecture on either kind of Mac.
#   --test   also build HEC's C tests and run them with CTest (needs sample-data/dss-test-data,
#            from scripts/fetch-sample-data.sh dss-test-data). An osx-x64 build on Apple silicon
#            runs them under Rosetta 2, if it is installed. On Linux and macOS, HEC's Dss-C test
#            squeezes and sorts files in /tmp (a fixed path in HEC's library).
#   --clean  delete the build folder first.
# Output:      artifacts/native/runtimes/<rid>/native/ (hecdss.dll, libhecdss.so or libhecdss.dylib),
#              where src/Hec.Dss picks it up
# Build trees: artifacts/native-build/hecdss/<rid>/
# Environment: CMAKE (cmake to use; otherwise cmake on PATH, then Visual Studio's on Windows),
#              JOBS (parallel compile jobs), CTEST_ARGS (more ctest arguments, e.g. -E Dss-C)
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
run_tests=0
clean=0
rids=""
for arg in "$@"; do
  case "$arg" in
    -h|--help) sed -n '2,18p' "${BASH_SOURCE[0]}"; exit 0 ;;
    --test) run_tests=1 ;;
    --clean) clean=1 ;;
    -*) echo "Unknown option: $arg" >&2; exit 2 ;;
    *) rids="$rids $arg" ;;
  esac
done

case "$(uname -s)" in
  Linux) host_os=linux ;;
  Darwin) host_os=osx ;;
  MINGW*|MSYS*|CYGWIN*) host_os=win ;;
  *) echo "Unsupported build OS: $(uname -s)" >&2; exit 1 ;;
esac
case "$(uname -m)" in
  x86_64|amd64) host_arch=x64 ;;
  arm64|aarch64) host_arch=arm64 ;;
  *) echo "Unsupported build architecture: $(uname -m)" >&2; exit 1 ;;
esac
[ -n "$rids" ] || rids="$host_os-$host_arch"

# Paths handed to Windows programs (cmake.exe) must be Windows paths.
native_path() {
  if [ "$host_os" = win ]; then cygpath -m "$1"; else printf '%s\n' "$1"; fi
}

find_cmake() {
  if [ -n "${CMAKE:-}" ]; then printf '%s\n' "$CMAKE"; return 0; fi
  if command -v cmake >/dev/null 2>&1; then command -v cmake; return 0; fi
  if [ "$host_os" = win ]; then
    # Visual Studio's own CMake, found with vswhere (installed with every Visual Studio 2017+).
    local pf86 vswhere found
    pf86="$(printenv 'ProgramFiles(x86)' || true)"
    vswhere="$(cygpath -u "${pf86:-C:\\Program Files (x86)}")/Microsoft Visual Studio/Installer/vswhere.exe"
    if [ -x "$vswhere" ]; then
      found="$("$vswhere" -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 \
        -find 'Common7/IDE/CommonExtensions/Microsoft/CMake/CMake/bin/cmake.exe' | tr -d '\r' | head -n 1)"
      if [ -n "$found" ]; then cygpath -u "$found"; return 0; fi
    fi
  fi
  echo "CMake not found: install CMake 3.16 or later, or set CMAKE to its path." >&2
  return 1
}

if [ -n "${JOBS:-}" ]; then
  jobs="$JOBS"
elif [ "$host_os" = win ]; then
  jobs="${NUMBER_OF_PROCESSORS:-4}"
else
  jobs="$(getconf _NPROCESSORS_ONLN 2>/dev/null || echo 4)"
fi

cmake="$(find_cmake)"
ctest="$(dirname "$cmake")/ctest"
[ -x "$ctest" ] || [ -x "$ctest.exe" ] || ctest=ctest
echo "Using $("$cmake" --version | head -n 1) ($cmake)"

test_data="$root/sample-data/dss-test-data"
if [ "$run_tests" = 1 ] && [ ! -f "$test_data/sample7.dss" ]; then
  echo "--test needs HEC's test data: run scripts/fetch-sample-data.sh dss-test-data first." >&2
  exit 1
fi

# Lists the libraries a built library loads, one per line (best effort: needs objdump or otool).
library_dependencies() {
  case "$1" in
    *.dll) if command -v objdump >/dev/null 2>&1; then objdump -p "$1" | awk '/DLL Name:/ { print $3 }'; fi ;;
    *.so) if command -v objdump >/dev/null 2>&1; then objdump -p "$1" | awk '$1 == "NEEDED" { print $2 }'; fi ;;
    # otool -L also lists the library's own install name (otool -D), which isn't a dependency.
    *.dylib) otool -L "$1" | tail -n +2 | awk '{ print $1 }' | grep -vxF "$(otool -D "$1" | tail -n +2)" ;;
  esac
}

for rid in $rids; do
  generator_args=""
  case "$rid" in
    win-x64)
      [ "$host_os" = win ] || { echo "$rid can only be built on Windows." >&2; exit 1; }
      lib_name=hecdss.dll
      # Visual Studio generators take the target platform; others (e.g. Ninja in a developer
      # prompt) use the compiler the environment provides.
      case "${CMAKE_GENERATOR:-Visual Studio}" in "Visual Studio"*) generator_args="-A x64" ;; esac
      ;;
    linux-x64)
      [ "$host_os" = linux ] && [ "$host_arch" = x64 ] || { echo "$rid can only be built on x64 Linux." >&2; exit 1; }
      lib_name=libhecdss.so
      ;;
    osx-arm64|osx-x64)
      [ "$host_os" = osx ] || { echo "$rid can only be built on macOS." >&2; exit 1; }
      lib_name=libhecdss.dylib
      if [ "$rid" = osx-arm64 ]; then osx_arch=arm64; else osx_arch=x86_64; fi
      # 11.0 is the first macOS with Apple silicon; .NET 10 itself needs a newer one.
      generator_args="-DCMAKE_OSX_ARCHITECTURES=$osx_arch -DCMAKE_OSX_DEPLOYMENT_TARGET=11.0"
      ;;
    *) echo "Unsupported runtime identifier: $rid (win-x64, linux-x64, osx-arm64 or osx-x64)" >&2; exit 2 ;;
  esac

  build="$root/artifacts/native-build/hecdss/$rid"
  out="$root/artifacts/native/runtimes/$rid/native"
  echo "==> hecdss for $rid"
  [ "$clean" = 0 ] || rm -rf "$build"
  mkdir -p "$build" "$out"

  test_args="-DXGF_HECDSS_TESTS=OFF"
  [ "$run_tests" = 0 ] || test_args="-DXGF_HECDSS_TESTS=ON -DDSS_TEST_DATA_DIR=$(native_path "$test_data")"

  # $generator_args and $test_args hold no spaces besides the separators, so word splitting is wanted.
  "$cmake" -S "$(native_path "$root/native/hecdss")" -B "$(native_path "$build")" \
    -DCMAKE_BUILD_TYPE=Release $generator_args $test_args
  "$cmake" --build "$(native_path "$build")" --config Release --parallel "$jobs"

  built="$(tr -d '\r' < "$build/hecdss-library-Release.txt")"
  [ "$host_os" != win ] || built="$(cygpath -u "$built")"
  cp "$built" "$out/$lib_name"
  echo "Built $out/$lib_name"

  # The library must not depend on a separate zlib (or, on Windows, on the Visual C++ runtime).
  deps="$(library_dependencies "$out/$lib_name")"
  if [ -n "$deps" ]; then
    echo "It loads:" $deps
    if printf '%s\n' "$deps" | grep -Eiq '(^|/)(lib)?z(lib)?[.0-9]*\.(so|dylib|dll)|vcruntime|msvcp'; then
      echo "Unexpected dependency of $lib_name: zlib and the C runtime should be linked statically." >&2
      exit 1
    fi
  fi

  if [ "$run_tests" = 1 ]; then
    if [ "$rid" = osx-x64 ] && [ "$host_arch" = arm64 ] && [ ! -e /Library/Apple/usr/libexec/oah/libRosettaRuntime ]; then
      echo "Not running the osx-x64 tests: Rosetta 2 isn't installed." >&2
    else
      # shellcheck disable=SC2086 # CTEST_ARGS holds separate arguments.
      "$ctest" --test-dir "$(native_path "$build")" -C Release --output-on-failure ${CTEST_ARGS:-}
    fi
  fi
done
