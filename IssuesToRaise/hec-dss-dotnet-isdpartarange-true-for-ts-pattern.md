# DssPath.IsDPartARange() is true for "TS-PATTERN" (any D part with one dash)

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), `dotnet/Hec.Dss/DssPath.cs`. Found in 2.0.3 at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

`IsDPartARange()` returns `_Dpart.Split('-').Length == 2`, so the D part of a time-series pattern record, `TS-PATTERN`, counts as a date range. `GetDPartAsDateTime()` then tries to parse `"TS"` as a HEC date and throws `FormatException("invalid Hec Date format 'TS'")`. Callers that normalise ranges treat the pattern as a range too. RMC-BestFit, for example, then looks up a path whose D part is `TS` and finds no data.

## Details and how to reproduce

```csharp
var p = new DssPath("/A/B/FLOW/TS-PATTERN/1HOUR/F/");
bool isRange = p.IsDPartARange();          // true
DateTime d = p.GetDPartAsDateTime();       // FormatException: invalid Hec Date format 'TS'
```

## Why this is an issue and not intended behaviour

hec-dss itself treats `TS-Pattern` as a special D part: `isTsPattern()` compares it case-insensitively and the C API branches on it ([`heclib/heclib_c/src/Internal/isTsPattern.c` lines 9-19](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Internal/isTsPattern.c#L9-L19)). A range is two dates, and the method's documentation says "true if DPart is a range (contains a -)", which only holds if no other D part contains a dash.

## Where in the source

- [`dotnet/Hec.Dss/DssPath.cs` lines 330-360](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssPath.cs#L330-L360)
  ```csharp
  public bool IsDPartARange()
  {
    return (_Dpart.Split('-').Length == 2);
  }
  ```
- Also used in `DssPathCollection.GetCondensedPath` ([line 366](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/DssPathCollection.cs#L366)), and in RMC-BestFit 2.0.1's DSS import ([`TimeSeriesElement.cs` lines 2124-2129](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L2124-L2129), [`DssPathSelectorRow.cs` lines 167-177](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/DssPathSelectorRow.cs#L167-L177)).

## Suggested fix

```csharp
public bool IsDPartARange()
{
  var parts = _Dpart.Split('-');
  return parts.Length == 2
      && Time.TryConvertFromHecDateTime(parts[0].Trim(), out _)
      && Time.TryConvertFromHecDateTime(parts[1].Trim(), out _);
}
```

Optionally add `IsTsPattern` (case-insensitive `TS-PATTERN`), mirroring the C library.

## How XGoodFit handles it

XGoodFit (which compiles this wrapper for its RMC-BestFit port) keeps the code unchanged. Its DSS comparison tests skip block reads for `TS-PATTERN` records. Recorded in [`docs/migration-plan.md`, Phase 0](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L468).
