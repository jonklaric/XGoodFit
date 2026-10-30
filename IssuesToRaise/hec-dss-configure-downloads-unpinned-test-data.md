# CMake configure always downloads the test data from the unpinned master branch

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `deps/CMakeLists.txt`. Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); unchanged in this respect on `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07).

**Type:** build · **Severity:** low

**Verification:** verified by reading the source.

## Summary

Configuring hec-dss runs `FetchContent_MakeAvailable(dss_test_data)` from `GIT_TAG master`, whether or not tests are wanted. So:

- a plain library build needs network access to GitHub, which fails offline, in sandboxed package builds and behind restrictive proxies, unless the builder knows to set `FETCHCONTENT_FULLY_DISCONNECTED` or `FETCHCONTENT_SOURCE_DIR_DSS_TEST_DATA`;
- test results aren't reproducible, because the same hec-dss commit tests against whatever `master` of `dss-test-data` holds that day.

zlib is fetched the same way but pinned (`GIT_TAG v1.3.1`), which is fine.

## Details and how to reproduce

`cmake -S . -B build` without network access fails while cloning `dss-test-data`, although the library itself needs only zlib.

## Why this is an issue and not intended behaviour

`RUN_TESTS` already makes the tests optional ("requires dss-test-data git repo"), so the data clearly isn't needed for a library build. The zlib dependency is pinned to a tag, so pinning is evidently the intent for dependencies.

## Where in the source

[`deps/CMakeLists.txt` lines 1-20](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/deps/CMakeLists.txt#L1-L20)

```cmake
FetchContent_Declare(
    dss_test_data
    GIT_REPOSITORY https://github.com/HydrologicEngineeringCenter/dss-test-data.git
    GIT_TAG master
)
FetchContent_MakeAvailable(dss_test_data)
```

## Suggested fix

- Pin `GIT_TAG` to a commit or tag of `dss-test-data`, and bump it deliberately.
- Fetch it only when testing: wrap the declaration in `if(RUN_TESTS)` (with `option(RUN_TESTS … OFF)` declared before `add_subdirectory(deps)`), or accept a `DSS_TEST_DATA_DIR` supplied by the builder.

## How XGoodFit handles it

XGoodFit builds hec-dss with its own top-level CMake file ([`native/hecdss/CMakeLists.txt`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/native/hecdss/CMakeLists.txt)), using a vendored zlib and test data pinned in [`sample-data.lock`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/sample-data.lock), so the build needs no network ([`docs/migration-plan.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L469)).
