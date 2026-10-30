# GetRawCatalog passes a filter without a terminating null to hec_dss_catalog

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), `dotnet/Hec.Dss/DssReader.cs`. Found in 2.0.3 at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

With a non-empty filter, `GetRawCatalog` converts it with `Encoding.ASCII.GetBytes(filter)` and passes the array to `hec_dss_catalog`, whose `pathFilter` is a C string (`const char*`). The array has no terminating zero byte, so the native code reads past its end until it happens to find one. That's an out-of-bounds read, and the filter can silently include whatever bytes follow it in memory. The empty-filter case passes `{ 0 }` and is fine.

## Details and how to reproduce

The only caller with a filter is the wrapper's own test, `CoreTesting` (`"/*/*/*Flow*/*/*/*/"`), so the shipped API doesn't use this path yet. It works only when the byte after the array in managed memory happens to be zero; the result depends on the heap, so it's undefined behaviour.

## Why this is an issue and not intended behaviour

`hecdss.h` documents `pathFilter` as "Either null (for ignore) or a String", meaning a null-terminated C string. The empty case adds the terminator by hand (`new byte[] { 0 }`), which shows the convention is known.

## Where in the source

[`dotnet/Hec.Dss/DssReader.cs` lines 122-145](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssReader.cs#L122-L145)

```csharp
byte[] byteFilter = new byte[] { 0 };
if( filter!= "")
{
  byteFilter = Encoding.ASCII.GetBytes(filter);        // no trailing '\0'
}
var numRecords = DssNative.hec_dss_catalog(dss, rawCatalog, recordTypes, byteFilter, count, pathBufferSize);
```

The native side passes `pathFilter` to `zcatalog` as a string: [hec-dss `heclib/hecdss/hecdss.c` lines 196-200](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L196-L200).

## Suggested fix

```csharp
byteFilter = Encoding.ASCII.GetBytes(filter + "\0");
```

Or declare the parameter as `string` with `CharSet.Ansi` and let the marshaller add the terminator.

## How XGoodFit handles it

XGoodFit (which compiles this wrapper for its RMC-BestFit port) keeps the code unchanged. RMC-BestFit doesn't use filtered catalogues. Recorded in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L468).
