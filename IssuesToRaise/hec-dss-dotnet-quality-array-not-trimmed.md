# TimeSeries.Qualities isn't trimmed to the values read, so it can be longer than Values

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), `dotnet/Hec.Dss/DssReader.cs` (`GetTimeSeries`). Found in 2.0.3 at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** reproduced with RMC-BestFit 2.0.0's `Hec.Dss.dll` (2.0.3) and its 7-IU-6 native, reading every time series in three of HEC's sample files from `dss-test-data`. 9 series came back with more quality entries than values: 6 of 740 in `DSS-178.dss`, 1 of 586 in `version7AlbersGridsTimeSeries.dss` (235 values, 1,464 quality entries), and 2 of 32 in `sample7.dss`.

## Summary

`GetTimeSeries` sizes `times`, `values` and `quality` from `hec_dss_tsGetSizes`, then resizes `times` and `values` to `numberValuesRead` but not `quality`. When fewer values are read than estimated, `ts.Qualities` keeps the full estimated length, and its tail entries don't belong to any value.

## Details and how to reproduce

```csharp
using var reader = new DssReader("DSS-178.dss");
foreach (DssPath path in reader.GetCatalog())
{
    if (path.RecordType != RecordType.RegularTimeSeries && path.RecordType != RecordType.IrregularTimeSeries) continue;
    var ts = reader.GetTimeSeries(path);
    if (ts.Qualities.Length > ts.Count) Console.WriteLine($"{path.FullPath}: n={ts.Count} quality={ts.Qualities.Length}");
}
// e.g. //PIND-PINGREE-PIPESTEM/STAGE/01Jan2017/1Hour/COMBINED-REV/: n=240 quality=744
```

`TimeSeries.Trim()` (regular series only) copies `Qualities` only when it trims something, so an untrimmed regular series keeps the long array too.

## Why this is an issue and not intended behaviour

`TimeSeries.SetQuality` rejects an array whose length differs from `Values.Length` ("Size of quality array doesn't match value array."), so the class's own invariant is one quality per value, and the reader breaks it. Callers that iterate `Qualities`, or pair it with values by index after an append, get misaligned data.

## Where in the source

- [`DssReader.cs` lines 268-305](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssReader.cs#L268-L305)
  ```csharp
  int qualityLength = numberValues * qualityElementSize;
  var quality = new int[qualityLength];
  ...
  Array.Resize(ref times,numberValuesRead);
  ...
  Array.Resize(ref values, numberValuesRead);
  if (qualityLength > 0)
    ts.Qualities = quality;                        // not resized
  ```
- The invariant: [`TimeSeries.cs` lines 153-158](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/TimeSeries.cs#L153-L158). `Trim`: [lines 327-356](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/TimeSeries.cs#L327-L356).

## Suggested fix

```csharp
if (qualityLength > 0)
{
  Array.Resize(ref quality, numberValuesRead * qualityElementSize);
  ts.Qualities = quality;
}
```

If `qualityElementSize` can be greater than 1, `Qualities` would also need a width, since today it's indexed one entry per value. See also [hec-dss-quality-flags-vary-between-runs.md](hec-dss-quality-flags-vary-between-runs.md): some of the returned flags are themselves unreliable.

## How XGoodFit handles it

XGoodFit (which compiles this wrapper for its RMC-BestFit port) keeps the code unchanged; RMC-BestFit doesn't read quality flags. Recorded in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L468).
