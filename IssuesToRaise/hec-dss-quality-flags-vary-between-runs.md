# Some records' quality flags change on every read: they look like uninitialised memory

**Target:** [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), time-series retrieval (`heclib/hecdss/hecdss.c` `hec_dss_tsRetrieve`, and below it `heclib_c` `ztsRetrieve`/`ztsRegReadBlock`/`ztsDisaggregate`). Found in the 7-IU-6 binary (the one Hec.Dss 2.0.3 downloads) and in 7-JA-7 ([`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7)). Not checked against `main`, where #365 rewrote the final quality copy but not the read path.

**Type:** bug (uninitialised or out-of-bounds read) · **Severity:** medium

**Verification:** reproduced on Windows x64. Every time series in HEC's sample files was read twice, in separate processes, through `Hec.Dss.DssReader.GetTimeSeries`: with RMC-BestFit 2.0.0's `Hec.Dss.dll` and its 7-IU-6 native, and with XGoodFit's build of the wrapper (corrected declarations) on 7-JA-7 built from source. The number of values and of quality entries was the same in both runs, but the quality flags of the same three records differed every time, in both libraries. XGoodFit's own comparison test also found the times and values identical. The wrapper zero-fills its buffers, so the values come from the native library.

## Summary

For some records, the quality array returned by `hec_dss_tsRetrieve` holds a single arbitrary 32-bit value repeated for every point, and the value is different on each run. Anyone using quality flags from these records reads garbage. If the bytes come from beyond the end of a buffer, that's also an out-of-bounds read.

## Details and how to reproduce

Files from [HydrologicEngineeringCenter/dss-test-data](https://github.com/HydrologicEngineeringCenter/dss-test-data) (commit `5df48ce`):

| File | Record | Values | First quality values, run 1 / run 2 |
|---|---|---|---|
| `DSS-178.dss` | `//JAND/FLOW-INC/01Jun2021-01Jul2021/1Hour/BEST-NWDM/` | 359 | 7-IU-6: `205073531 ×4` / `187766577 ×4`; 7-JA-7: `-1652818353 ×4` / `-1317435254 ×4` |
| `DSS-178.dss` | `//PIND-PINGREE-PIPESTEM/STAGE/01Jan2017/1Hour/COMBINED-REV/` | 240 | `0, 201722375, 201722375, …` / different |
| `version7AlbersGridsTimeSeries.dss` | `//WINNEBAGO_IN/FLOW/01May2016-01Jun2016/1Hour/FCST/` | 235 | first four 0, the rest differ between runs |

Every other series in these files (and all 32 in `sample7.dss`) gives identical flags on every run.

```csharp
using var reader = new Hec.Dss.DssReader(copyOf("DSS-178.dss"));
foreach (Hec.Dss.DssPath path in reader.GetCatalog())
{
    var ts = reader.GetTimeSeries(path);   // times and values stable; Qualities differs between runs for the records above
}
```

## Why this is an issue and not intended behaviour

Reading a file shouldn't give different answers from one run to the next. One value repeated for every point, and different each time, is the signature of an uninitialised variable or a read past the end of a buffer.

## Where in the source

Not pinned down. Some leads:

- The pattern matches the "all repeats" quality compression branch (`qualityCompressionFlag == 2`), which copies one stored value to every point: [`heclib/heclib_c/src/Internal/ztsDisaggregate.c` lines 576-585](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/heclib_c/src/Internal/ztsDisaggregate.c#L576-L585). If `dataInPosition` points past what was read into `dataIn` for these records, that value would be whatever the buffer held.
- Two of the three records span two monthly blocks (`01Jun2021-01Jul2021`, `01May2016-01Jun2016`), so block boundaries or blocks without quality may be involved.
- 7-JA-7's final copy in `hec_dss_tsRetrieve` reads `tss->quality[i]` for every value read, with no NULL or size check ([`hecdss.c` lines 357-365](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/heclib/hecdss/hecdss.c#L357-L365)). That was fixed on `main` by #365.

Running the reads under Valgrind or AddressSanitizer on Linux should show the exact location.

## Suggested fix

Find the read of uninitialised or out-of-range memory with Valgrind or ASan on the records above, and add them to the C test suite as a regression test. Reading each record twice and comparing the flags is enough.

## How XGoodFit handles it

XGoodFit's DSS comparison tests (for its RMC-BestFit port, which doesn't read quality flags) compare only the number of quality flags, not their values, for these records: [`tests/Hec.Dss.Tests/XGoodFit/ReferenceComparisonTests.cs` lines 21-29](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/Hec.Dss.Tests/XGoodFit/ReferenceComparisonTests.cs#L21-L29).
