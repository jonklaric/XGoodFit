# hec_dss_open no longer returns -700 for DSS 6 files: it fails inside zopen with an internal error code

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), `heclib/hecdss/hecdss.c` (`hec_dss_open`) and `heclib/heclib_c/src/Internal/zopenInternal.c`. Found in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)); `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07) takes the same path.

**Type:** bug · **Severity:** low

**Verification:** reproduced on Windows x64 by calling `hec_dss_open` directly on copies of HEC's `dss-test-data` files `Windowsv6.dss` and `sample6.dss`. 7-IU-6 (the binary Hec.Dss 2.0.3 downloads) returned **-700**; 7-JA-7, built from source with CMake, returned **-500010206**. Both return 0 for `Windowsv7.dss`, and `hec_dss_getFileVersion` returns 6 for the DSS 6 files with both.

## Summary

`hec_dss_open` has an explicit check that returns -700 when the file isn't version 7. In 7-JA-7, which dropped DSS 6 support, the open itself fails first: `zopenInternal` rejects the version with `INCOMPATIBLE_VERSION`. So `hec_dss_open` returns that composite code and never reaches its -700 branch. Callers that map -700 to "only DSS version 7 files are supported" now show a generic error. The .NET wrapper `Hec.Dss` reports "Error opening DSS file."

## Details and how to reproduce

```c
dss_file* dss = NULL;
int status = hec_dss_open("Windowsv6.dss", &dss);   // a DSS 6 file
// 7-IU-6: -700        7-JA-7: -500010206
```

The library's log line for the failure is empty, so nothing tells the user why the file didn't open.

## Why this is an issue and not intended behaviour

The -700 branch in `hec_dss_open` exists to give DSS 6 files a specific, documented result ("version is not supported. Only version 7 DSS files are supported"). It's now unreachable for exactly those files. Downstream code relies on it: [hec-dss-dotnet `DssReader.cs` line 85](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssReader.cs#L85) maps -700 to "This library only supports DSS version 7 files".

## Where in the source

- [`heclib/hecdss/hecdss.c` lines 135-153](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L135-L153)
  ```c
  int status = hec_dss_zopen(f->ifltab,filename);
  if (status != 0) { free(f); return status; }          // a DSS 6 file returns here
  int version = zgetVersion(f->ifltab);
  if (version != 7) { ...; return -700; }               // unreachable for DSS 6 files
  ```
- [`heclib/heclib_c/src/Internal/zopenInternal.c` lines 316-322](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Internal/zopenInternal.c#L316-L322): `//  Wrong version (e.g., version 6)` then `zerrorProcessing(…, zdssErrorCodes.INCOMPATIBLE_VERSION, …)`.
- On `main`, `hec_dss_open` calls `hec_dss_open_ex`, which calls `zopenExtended` and then `zopenInternal`, with the same check order: [`hecdss.c` lines 168-208](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c/heclib/hecdss/hecdss.c#L168-L208).

## Suggested fix

Check the version before opening:

```c
HECDSS_API int hec_dss_open(const char* filename, dss_file** dss)
{
    int fileVersion = zgetFileVersion(filename);
    if (fileVersion > 0 && fileVersion != 7) {
        hec_dss_log_message("version is not supported.\nOnly version 7 DSS files are supported");
        return -700;
    }
    ...
```

Or map `INCOMPATIBLE_VERSION` from `zopen` to -700. Document the return codes in `hecdss.h`, which today only says "zero if successful".

## How XGoodFit handles it

XGoodFit builds 7-JA-7 for its RMC-BestFit port and maps a DSS 6 file's open failure back to -700 in its `DssNative.cs` ([lines 33-35](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Hec.Dss/DssNative.cs#L33-L35)), so RMC-BestFit's message is unchanged. It's tested in [`tests/Hec.Dss.Tests/XGoodFit/Dss6FileTests.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/Hec.Dss.Tests/XGoodFit/Dss6FileTests.cs).
