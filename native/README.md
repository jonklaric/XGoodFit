# Native libraries

XGoodFit builds HEC-DSS's native `hecdss` library from the vendored source, for every platform it
supports. RMC-BestFit imports time series from DSS files through HEC's .NET wrapper, `Hec.Dss`,
which calls this library (docs/migration-plan.md §7, Phase 0, and §9.2).

| What | Version | Source |
|---|---|---|
| HEC-DSS (`heclib` and `hecdss`, C only) | 7-JA-7 | `upstream/hec-dss` |
| zlib, linked in statically | 1.3.1 (the tag HEC-DSS 7-JA-7 builds with) | `upstream/zlib` |

## Building

You need CMake 3.16 or later (3.27 or later for the tests on Windows), a C compiler, and bash:

- **Windows:** Visual Studio 2022 with the "Desktop development with C++" workload, which includes
  CMake; run the script from Git Bash. Without `cmake` on `PATH`, the script finds Visual Studio's.
- **Linux:** GCC or Clang, and CMake.
- **macOS:** the Xcode command-line tools, and CMake (or set `CMAKE` to a `cmake` of your own).

```bash
# This machine's runtime identifier (win-x64, linux-x64, osx-arm64 or osx-x64):
native/build-hecdss.sh

# A Mac builds both architectures:
native/build-hecdss.sh osx-arm64 osx-x64

# Also build HEC's C tests and run them (needs scripts/fetch-sample-data.sh dss-test-data):
native/build-hecdss.sh --test
```

Each library lands in `artifacts/native/runtimes/<rid>/native/`: `hecdss.dll`, `libhecdss.so` or
`libhecdss.dylib`. `src/Hec.Dss` ships every one it finds there as a `runtimes/<rid>/native` asset,
so `dotnet build` afterwards copies them to every project that references it. The build trees are in
`artifacts/native-build/hecdss/<rid>/`; `--clean` starts again. Nothing is downloaded.

## How the build differs from HEC's

`hecdss/CMakeLists.txt` replaces HEC's top-level `CMakeLists.txt` and uses HEC's own `heclib`,
`hecdss` and `test` CMake files unchanged:

- **zlib comes from `upstream/zlib`**, not from a download. HEC's `deps/CMakeLists.txt` fetches zlib
  and HEC's test data from GitHub while configuring; this build doesn't use it.
- **zlib is linked statically everywhere.** On Linux and macOS, HEC's `deps/zlib/CMakeLists.txt`
  makes `ZLIB::ZLIBSTATIC` an alias of zlib's *shared* library, so HEC's own builds there load a
  separate `libz` (on macOS through an rpath HEC doesn't ship). Here it's zlib's static library, as
  HEC's MSVC branch already uses. zlib is built from a copy in the build tree without `zconf.h`,
  because zlib's `CMakeLists.txt` renames that file in its source folder otherwise.
- **The MSVC C runtime is linked statically** (`/MT`), so `hecdss.dll` doesn't need the Visual C++
  redistributable: it loads only `KERNEL32.dll`.
- **On Linux, only `hecdss.c`'s API is exported**, as on Windows: the symbols of the static `heclib`
  and zlib stay inside the library (`--exclude-libs`).
- **No Java library**: `javaHeclib` isn't built.

The macOS libraries target macOS 11.0, the first with Apple silicon. The script prints the libraries
each build loads, and fails if one of them is a separate zlib or the Visual C++ runtime.

`--test` also builds HEC's C tests (`test/C` and `test/Dss-C`, as HEC's own CMake registers them)
and HEC's test of the shared library's API (`test/C/hecdss_test.c`), and runs them with CTest. On a
Mac with Apple silicon, the `osx-x64` tests run under Rosetta 2 if it's installed. `CTEST_ARGS`
passes more options to CTest, e.g. `CTEST_ARGS="-E Dss-C"` to leave out the long Dss-C suite. On
Linux and macOS that suite squeezes files and sorts catalogues in `/tmp`, a path fixed in HEC's
library (the wrapper's unsorted catalogue never does).

## Updating HEC-DSS

The wrapper's native declarations (`src/Hec.Dss/DssNative.cs`) must match the library exactly, and the
library is rejected at load time unless `hec_dss_api_version()` returns the version they were written
for (`DssNative.ExpectedApiVersion`, "0.3.0" for 7-JA-7). So bump the `hec-dss` pin and
`DssNative.cs` together (migration plan §10): compare every declaration with the new `hecdss.h`
(`tests/Hec.Dss.Tests` fails if one differs), then rebuild the natives and run the DSS tests on every OS.
