# Editing a bivariate analysis's XY ordinates while a background reprocess runs can mark the fit as not estimated

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit` (`AnalysisBase`, `BivariateAnalysis`) and `src/RMC.BestFit.UI` (the `BivariateAnalysis` element), plus the test `RMC.BestFit.Tests.Bivariate.BivariateAnalysisXYOrdinatesReprocessTests.XYOrdinatesChange_EstimatedAnalysis_PreservesMcmcResults`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-30.

**Type:** bug (race condition; also test reliability) · **Severity:** medium

**Verification:** reproduced. A small program against upstream's own `RMC.BestFit.dll` (built from this commit) followed the element's code path 20 times per case. Deleting two XY rows during a reprocess cleared `IsEstimated` in 13 of 20 runs; clearing the grid during a reprocess did so in 12 of 20. The same edit with nothing else touching the ordinates lost nothing (0 of 20). The upstream test failed once on a GitHub Windows runner in XGoodFit's CI. On a 12-core laptop it passed 150 isolated runs and 4 full parallel suite runs, so the test itself needs a loaded machine to lose the race; the program above doesn't.

Related: [Switching a bivariate analysis's point estimator can mark the fit as not estimated](RMC-BestFit-bivariate-point-estimator-race-clears-isestimated.md), the same reprocess losing the same state when the element's validation sorts the marginals' series under it.

## Summary

When an estimated bivariate analysis's XY ordinates change, `ReprocessIfEstimated` rebuilds the joint-frequency results on a thread-pool thread. That background run reads the live `XYOrdinates` collection and the shared `AnalysisResults` object while the UI thread can still change both. If a row is removed, or the grid cleared, during the run, it throws. Its `catch` then sets `IsEstimated = false`, so a valid MCMC fit is reported as not estimated and the user has to run the analysis again.

## Details and how to reproduce

The element forwards every row edit to the model ([`RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs` lines 800-806](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L800-L806)):

```csharp
private void XYOrdinates_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
{
    RecordXYOrdinatesUndo();
    ValidateOrdinates();
    _innerAnalysis.ReprocessOrClearXYOrdinates();
    ...
```

Reproduction against the model, as the element drives it (`CreateInjectedAnalysis` is the existing test helper, called by reflection):

```csharp
var a = CreateInjectedAnalysis();          // estimated, with injected MCMC results
a.XYOrdinates = Grid(40);                  // starts a background reprocess over 40 ordinates
Thread.Sleep(5);
a.XYOrdinates.RemoveAt(a.XYOrdinates.Count - 1);
a.XYOrdinates.RemoveAt(a.XYOrdinates.Count - 1);
a.ReprocessOrClearXYOrdinates();           // what XYOrdinates_CollectionChanged does
// wait until the reprocess gate is free
// Expected: a.IsEstimated == true (the MCMC fit is unchanged).
// Actual:   a.IsEstimated == false in 13 of 20 runs.
```

Removing every row instead (so `ReprocessOrClearXYOrdinates` calls `ClearFrequencyAnalysisResults`, documented to keep `IsEstimated`) gives `false` in 12 of 20 runs.

There are two ways the background run fails:

1. **The ordinates change under it.** `CreateFrequencyAnalysisResultsAsync` reads `int n = XYOrdinates.Count;` and then indexes `XYOrdinates[i]` in its loop. That is the same collection the UI thread is editing, so a deleted row gives an index out of range. The UI thread also mutates a `List` the pool thread is reading.
2. **The results object is cleared under it.** `ClearFrequencyAnalysisResults()` sets `AnalysisResults = null` without taking `_reprocessGate`. The background loop then dereferences `AnalysisResults.MeanCurve[i]` and gets a `NullReferenceException`.

Either exception lands in `ReprocessIfEstimated`'s `catch`, which sets `IsEstimated = false`. The reprocess queued by the edit itself then returns at once, because `ReprocessIfEstimated` starts with `if (!IsEstimated) return;`. So the state is lost for good.

The upstream test races in a third way: setting `XYOrdinates` starts the background reprocess, and the test then calls `CreateFrequencyAnalysisResultsAsync()` itself, outside the gate. Both runs set `AnalysisResults = null` and fill a new object on the shared property, so one can null it while the other is writing, and the test's final `Assert.IsTrue(analysis.IsEstimated)` fails.

## Why this is an issue and not intended behaviour

- `ClearFrequencyAnalysisResults` is documented to leave "the Bayesian MCMC output … and `IsEstimated` intact … the fit survives" ([lines 270-283](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L270-L283)), and `XYOrdinates_CollectionChanged` says it preserves the underlying MCMC fit "either way". The race breaks both promises.
- `_reprocessGate` exists precisely to stop reprocesses and `RunAsync` from trampling each other ([`AnalysisBase.cs` lines 52-64](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Support/AnalysisBase.cs#L52-L64)). The clear methods and the inputs the reprocess reads aren't covered by it.
- The user's cost is real: re-running the MCMC for a bivariate analysis takes minutes, and the reason isn't shown (the exception only goes to `Debug.WriteLine`).
- With no concurrent edit the same steps never lose the state (0 of 20), so the loss is timing, not data.

## Where in the source

- The background reprocess and its failure handling: [`src/RMC.BestFit/Analyses/Support/AnalysisBase.cs` lines 191-227](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Support/AnalysisBase.cs#L191-L227)
  ```csharp
  catch (Exception ex)
  {
      Debug.WriteLine($"Reprocess from {callerName} failed: {ex.InnerException?.Message ?? ex.Message}");
      IsEstimated = false;
  ```
- The setter and the row-edit entry point: [`src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs` lines 153-162](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L153-L162) and [lines 304-317](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L304-L317).
- The ungated clear: [lines 279-283](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L279-L283).
- The reprocessor reading shared state: [lines 501-558](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L501-L558)
  ```csharp
  AnalysisResults = null;
  ...
  int n = XYOrdinates.Count;
  AnalysisResults = new UncertaintyAnalysisResults();
  ...
  var u = marginalXDist.CDF(XYOrdinates[i].X);
  ...
  AnalysisResults.MeanCurve[i] = Statistics.ParallelMean(p);
  ```
- The test: [`src/RMC.BestFit.Tests/Bivariate/BivariateAnalysisXYOrdinatesReprocessTests.cs` lines 160-184](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.Tests/Bivariate/BivariateAnalysisXYOrdinatesReprocessTests.cs#L160-L184).
- Other analyses use the same `ReprocessIfEstimated` pattern (rating curve, the AR/MA/ARIMA/ARIMAX time-series analyses, spatial GEV). I haven't checked each for the same exposure.

## Suggested fix

1. **Snapshot inputs and publish results once.** At the start of the reprocess, copy what it reads (`XYOrdinates.ToArray()`, the marginals, the copula, `OutputLength`, `CredibleIntervalWidth`) and build the results into a local object, then assign `AnalysisResults` once at the end. Nothing the UI thread does can then make the loop throw.
2. **Drop stale results rather than failing.** Give each reprocess a sequence number (or cancel the previous one when a new edit arrives) and discard results from runs that are no longer the latest, instead of treating them as failures.
3. **Gate the clears.** Take `_reprocessGate`, or post through the same queue, in `ClearFrequencyAnalysisResults` and `ClearResults`. Or make them set a flag the running reprocess checks before publishing.
4. **Only clear `IsEstimated` for real failures**, not for exceptions caused by inputs changing mid-run.
5. **In the test,** wait for the background reprocess (through the gate, or a `Task` exposed for testing) instead of calling `CreateFrequencyAnalysisResultsAsync()` alongside it.

None of this changes what is saved, so there's no file-format risk.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) compiles the model library unchanged, so the race is there as in upstream. Its upstream-test script leaves this test out and says why: [`scripts/run-upstream-tests.sh` lines 59-62](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/scripts/run-upstream-tests.sh#L59-L62), recorded in [`docs/migration-plan.md`, Phase 0 findings](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L444).
