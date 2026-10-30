# Hec.Dss 2.0.3's native declarations don't match any hec-dss release since March 2025: calling a current native corrupts the stack

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), `dotnet/Hec.Dss/DssNative.cs`. Found in 2.0.3 at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd), still `main` on 2026-10-30. Compared with [hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss) 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)) and `main` ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c)).

**Type:** bug (memory corruption) · **Severity:** high

**Verification:** verified by reading the source. Each P/Invoke declaration was compared with `heclib/hecdss/hecdss.h` at 7-JA-7. Parameter counts were also compared with the headers at `v7-IT-2` and `ee6dff5` through GitHub. XGoodFit has a test that checks its corrected declarations against 7-JA-7's header.

## Summary

The wrapper's P/Invoke signatures match the C API as it was until February 2025, which is also the 7-IU-6 binary that `download-hecdss.ps1` fetches. Every hec-dss release since then has changed six of them: 7-IU-9, 7-IU-15, 7-IV-1 and 7-JA-7. Pairing the wrapper with any of these natives, for example one built from source for macOS, which the package doesn't ship, passes too few arguments. The callee reads leftover stack and register values as pointers and flags. `hec_dss_tsRetrieve` then writes the record's time zone through a garbage "buffer" whenever a record has one.

## Details and how to reproduce

| Function | Wrapper 2.0.3 | 7-JA-7 | Change (HEC PR) |
|---|---|---|---|
| `hec_dss_tsRetrieve` | 18 parameters | 20 | `char* timeZoneName, int timeZoneNameLength` appended (#271) |
| `hec_dss_pdRetrieve` | 18 | 20 | the same (#271) |
| `hec_dss_tsStoreRegular` | 11 | 13 | `const char* timeZoneName, int storageFlag` appended (#271, #280) |
| `hec_dss_tsStoreIregular` | 12 | 14 | the same (#271, #280) |
| `hec_dss_pdStore` | 14 | 15 | `const char* timeZoneName` appended (#271) |
| `hec_dss_gridStore` | 29 | 29 | `int compressionSize` inserted after `isTimeStamped` (#341), and the wrapper's trailing `dataLength` isn't a native parameter, so every argument after `isTimeStamped` is shifted |

Also:

- `hec_dss_convertToVersion7` no longer exists (DSS 6 support was removed), so `DssWriter.ConvertFromDSS6ToDSS7` throws `EntryPointNotFoundException`. `hec_dss_deprecated_ifltab` and `hec_dss_deprecated_ifltab_print` aren't exported either; nothing calls them.
- The wrapper passes the **total** quality-array length (`numberValues * qualityElementSize`) as `hec_dss_tsRetrieve`'s `qualityWidth`. 7-IU-6 and 7-JA-7 only test it for `> 0`. On hec-dss `main` (#365) quality is copied only when `qualityWidth == tss->qualityElementSize`, so with the next release the wrapper would receive no quality flags at all.

To see the corruption, build hec-dss 7-JA-7, put the native beside an app using Hec.Dss 2.0.3, and read a time series whose record has a time zone. `numberValuesRead` and the values are fine, but the time-zone string is written to whatever address happens to be in the missing argument's slot.

## Why this is an issue and not intended behaviour

The C header is the contract, and it changed on purpose (#271 "Hecdss/add time zone name", #280 "add storage flag to regular and irregular save methods", #341 "Allow Saving Pre-compressed Grids"). The wrapper wasn't updated to match. Nothing checks compatibility at load time either, although 7-JA-7 exports `hec_dss_api_version()` for that.

## Where in the source

- [`dotnet/Hec.Dss/DssNative.cs` lines 56-80, 141-161 and 186-205](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssNative.cs#L56-L80): the five store and retrieve functions, then `pdRetrieve`/`pdStore` and `gridStore`. Lines 17-18 and 46-50 hold the removed exports.
  ```csharp
  public static extern int hec_dss_tsRetrieve(IntPtr dss, string pathname, ...,
      ref int julianBaseDate, ref int timeGranularitySeconds,
      byte[] units, int unitsLength, byte[] type, int typeLength);   // 7-JA-7 adds timeZoneName, timeZoneNameLength
  ```
- [hec-dss 7-JA-7 `heclib/hecdss/hecdss.h`](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.h#L250-L258): `hec_dss_tsRetrieve` (lines 250-258), `tsStoreRegular` (289-294), `tsStoreIregular` (319-325), `pdRetrieve` (430-439), `pdStore` (459-468), `gridStore` (564-583), `hec_dss_api_version` (67).
- The quality copy on hec-dss `main`: [`heclib/hecdss/hecdss.c`](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c/heclib/hecdss/hecdss.c#L408-L425) (`qualityWidth == tss->qualityElementSize`).

## Suggested fix

- Update `DssNative.cs` to the 7-JA-7 signatures. Keep the existing C# methods as overloads that pass what 7-IU-6 did: no time zone as `""` (not `null`, which crashes 7-JA-7), storage flag 0 (`REPLACE_ALL`), and `compressionSize` 0. Then `DssReader`/`DssWriter` don't change.
- Pass the quality width (`qualityElementSize`), not the total length, and size the buffer from it.
- Call `hec_dss_api_version()` once when the library loads, and fail clearly if it isn't the version the declarations were written for.
- Remove `ConvertFromDSS6ToDSS7` and the two `deprecated` declarations, or mark them obsolete.
- Build the natives from a tagged hec-dss release for every platform the package targets, including macOS (see [hec-dss-dotnet-build-requires-windows.md](hec-dss-dotnet-build-requires-windows.md)).

No DSS file format is involved: these are calling conventions.

## How XGoodFit handles it

XGoodFit (a port of USACE RMC-BestFit, which uses Hec.Dss for DSS import) builds hec-dss 7-JA-7 from source and compiles the wrapper with a corrected [`src/Hec.Dss/DssNative.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Hec.Dss/DssNative.cs). That file has the 7-JA-7 declarations, upstream's signatures as overloads, and an `hec_dss_api_version()` check. [`tests/Hec.Dss.Tests/XGoodFit/DeclarationTests.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/Hec.Dss.Tests/XGoodFit/DeclarationTests.cs) checks every declaration against `hecdss.h`. The analysis is in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L455-L466).
