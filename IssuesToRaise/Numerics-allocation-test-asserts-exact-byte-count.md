# OutsideSupport_AllocatesOnlyBoundsEnumeration asserts an exact allocation count, which varies with background JIT work

**Target:** [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics), `Test_Numerics/Distributions/Univariate/Test_CompetingRisksEvaluationAllocations.cs`. Found in 2.2.0 at commit [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0) (tag `v2.2.0`), still `main` on 2026-10-30.

**Type:** test reliability · **Severity:** low

**Verification:** verified by reading the source. It failed once on a GitHub-hosted Linux runner in XGoodFit's CI, with 6,408 against 6,400 bytes.

## Summary

The test measures `GC.GetAllocatedBytesForCurrentThread()` around two loops and asserts that the two counts are exactly equal. That counter includes anything the runtime allocates on the thread while the loop runs, such as tiered-compilation bookkeeping or a lazily created cache. An 8-byte difference fails the test although the code under test allocates nothing extra.

## Details and how to reproduce

Run `Test_Numerics` repeatedly on a busy machine or a CI runner; it fails intermittently with `Assert.AreEqual failed. Expected:<6400>. Actual:<6408>. Live validation and collection access should add no allocations to the bounds enumeration.`

## Why this is an issue and not intended behaviour

The test's purpose (its summary: out-of-support evaluations "allocate only the existing bounds enumeration") is about the code's allocations, not the runtime's. Exact equality makes it depend on JIT timing, so CI can fail for reasons unrelated to the change being tested.

## Where in the source

[`Test_CompetingRisksEvaluationAllocations.cs` lines 13-38](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Test_Numerics/Distributions/Univariate/Test_CompetingRisksEvaluationAllocations.cs#L13-L38)

```csharp
for (int i = 0; i < 100; i++) { _ = distribution.LogPDF(-1d); _ = distribution.CDF(-1d); }   // warm-up
long before = GC.GetAllocatedBytesForCurrentThread();
for (int i = 0; i < 200; i++) _ = distribution.Minimum;
long boundsAllocation = GC.GetAllocatedBytesForCurrentThread() - before;
...
Assert.AreEqual(boundsAllocation, allocated, "Live validation and collection access should add no allocations to the bounds enumeration.");
```

## Suggested fix

Any of these:

- allow a small tolerance (`Assert.IsTrue(allocated <= boundsAllocation + 64)`), since a real regression allocates per call, that is about 100× more;
- measure several times and compare the minimum;
- warm up longer, past tier-1 promotion (more than 30 calls each, then a short sleep), before measuring.

## How XGoodFit handles it

XGoodFit builds RMC.Numerics from this source for its RMC-BestFit port and leaves this test out of its runs, with the reason: [`scripts/run-upstream-tests.sh` lines 37-39](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/scripts/run-upstream-tests.sh#L37-L39).
