# Switching a bivariate analysis's point estimator can mark the fit as not estimated

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit` (`BivariateDistribution.GetEligibleExactData`, `BivariateAnalysis.UpdatePointEstimateResultsAsync`) and `src/RMC.BestFit.UI` (the `BivariateAnalysis` element's validation). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug (race condition; also test reliability) · **Severity:** medium

**Verification:** reproduced. A throwaway MSTest test against upstream's `RMC.BestFit` library, compiled unchanged, built an estimated bivariate analysis (two Normal marginals with 80 paired values, a Normal copula, injected MCMC draws, as upstream's `BivariateAnalysisXYOrdinatesReprocessTests.CreateInjectedAnalysis` does) and switched its point estimator. Straight after, on the same thread, it called `GetSampleDataAlignmentCounts()`, which is what the element's validation does for the same notification, after a delay swept from 0 to 1 ms. That cleared `IsEstimated` in 3 of 400 runs on an idle 16-core laptop. Without the validation call it never did (0 of 400). Each failure was an `InvalidOperationException`, "Collection was modified; enumeration operation may not execute.", thrown in `UpdatePointEstimateResultsAsync`'s worker. It was first seen through upstream's `PointEstimatorControlContractTests` (its Bivariate row): run after other test classes on a two-core machine it failed in about a third of runs, and on a Windows CI runner it failed in its own process too.

Related: [Editing XY ordinates during a background reprocess clears `IsEstimated`](RMC-BestFit-reprocess-race-clears-isestimated.md), the same reprocess pattern losing the same state through another input.

## Summary

Changing the point estimator of an estimated bivariate analysis starts `UpdatePointEstimateResultsAsync` on a thread-pool thread, which enumerates both marginals' exact series to compute the RMSE. The same `PointEstimator` notification also reaches the element's `SetIsValid` on the UI thread, through `BayesianController`'s handler. `SetIsValid` validates the marginals with `GetSampleDataAlignmentCounts`, which sorts both exact series in place. `List.Sort` invalidates any enumeration of the list, even when the order doesn't change. If the sort lands while the worker is enumerating, the worker throws, and `ReprocessIfEstimated`'s `catch` sets `IsEstimated = false`. A valid MCMC fit is then reported as not estimated, and the user has to run it again.

## Details and how to reproduce

The order of events for one change of point estimator, for example from the Bivariate properties panel's **Point Estimator** selector:

1. `BayesianAnalysis.PointEstimator` raises `PropertyChanged`. Its first handler is the model analysis's (it subscribed when the analysis was built), which calls `ReprocessIfEstimated(UpdatePointEstimateResultsAsync)`. That queues the reprocess on the thread pool and returns.
2. The next handler is `BayesianController.BayesianAnalysis_UseDefaultsChanged` (subscribed later, in the element's `SetupBridges`), which calls its `_onValidate` callback, the element's `SetIsValid`, for any property.
3. `SetIsValid` → `ValidateMarginals` → `BivariateDistribution.GetSampleDataAlignmentCounts()` → `GetEligibleExactData()`, which calls `ExactSeries.SortByIndex()` on both marginals.
4. Meanwhile the worker runs `marginalX.DataFrame.ExactSeries.Where(...).ToList()`. `DataSeries` enumerates its inner `List<Data>` directly, so a `Sort` during that loop throws.

The test's core (`CreateInjected` builds the estimated analysis):

```csharp
var analysis = CreateInjected(80);
await analysis.CreateFrequencyAnalysisResultsAsync();
var ba = analysis.BayesianAnalysis;
ba.PointEstimator = ba.PointEstimator == PointEstimateType.PosteriorMode ? PointEstimateType.PosteriorMean : PointEstimateType.PosteriorMode;
SpinFor((r % 50) * 0.02 /* ms */);
analysis.BivariateDistribution.GetSampleDataAlignmentCounts();   // what SetIsValid does
// wait for the reprocess gate
if (!analysis.IsEstimated) lost++;
```

Output:

```
validation call after the switch: False; IsEstimated cleared in 0 of 400 runs (Environment.ProcessorCount 16)
validation call after the switch: True; IsEstimated cleared in 3 of 400 runs (Environment.ProcessorCount 16)
   first-chance: InvalidOperationException: Collection was modified; enumeration operation may not execute. @ RMC.BestFit.Analyses.BivariateAnalysis.<UpdatePointEstimateResultsAsync>b__26_0()
```

On a busy or slower machine the window is wider, as the test failures show.

## Why this is an issue and not intended behaviour

- Switching the point estimator is documented to keep the fit: "PointEstimator changes update the point-estimate output without re-running the chain" ([`BivariateAnalysis.cs` lines 229-233](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L229-L233)). Losing `IsEstimated` here is the failure path, not the design.
- A validation read shouldn't mutate shared data. `GetEligibleExactData`'s remarks say it sorts the underlying series "matching the historical behavior of `SetSampleData`", but nothing about validation needs the stored order to change; it needs sorted copies.
- The cost is the same as in the related issue: re-running a bivariate MCMC takes minutes, and the reason isn't shown (the exception only goes to `Debug.WriteLine`).

## Where in the source

- The in-place sort: [`src/RMC.BestFit/Models/BivariateDistribution/BivariateDistribution.cs` lines 491-511](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/BivariateDistribution/BivariateDistribution.cs#L491-L511):
  ```csharp
  MarginalX.DataFrame.ExactSeries.SortByIndex();
  MarginalY.DataFrame.ExactSeries.SortByIndex();
  ```
  called by `GetSampleDataAlignmentCounts` ([lines 473-477](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/BivariateDistribution/BivariateDistribution.cs#L473-L477)). `SortByIndex` is `List.Sort` on the series' inner list ([`ExactSeries.cs` lines 189-199](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataCollections/ExactSeries.cs#L189-L199)), and the series enumerates that list directly ([`DataSeries.cs` lines 156-159](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataCollections/DataSeries.cs#L156-L159)).
- The worker's enumeration: [`src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs` lines 438-439](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L438-L439), inside `UpdatePointEstimateResultsAsync` (from [line 403](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L403)), started by the `PointEstimator` handler at [lines 243-247](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L243-L247), which subscribed at [line 138](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L138).
- The failure handling: [`src/RMC.BestFit/Analyses/Support/AnalysisBase.cs` lines 191-227](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Support/AnalysisBase.cs#L191-L227) (`IsEstimated = false` in the `catch`).
- The UI-thread path: [`src/RMC.BestFit.UI/Elements/Support/BayesianController.cs` lines 1008-1014](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/BayesianController.cs#L1008-L1014) (`_onValidate?.Invoke()` on any property), subscribed at [line 979](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/BayesianController.cs#L979); the element passes `SetIsValid` at [`src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs` lines 1725-1726](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L1725-L1726); `SetIsValid` ([line 867](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L867)) calls `ValidateMarginals` ([lines 611-680](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L611-L680)), which calls `GetSampleDataAlignmentCounts` at line 674.

## Suggested fix

1. **Don't sort shared data to read it.** In `GetEligibleExactData`, sort copies: `MarginalX.DataFrame.ExactSeries.Cast<ExactData>().Where(d => !d.IsLowOutlier).OrderBy(d => d.Index).ToList()`. If `SetSampleData` relies on the series being sorted afterwards, sort there, under the reprocess gate, not on every validation.
2. **Snapshot in the reprocess.** Copy both marginals' eligible data at the start of `UpdatePointEstimateResultsAsync` (and `CreateFrequencyAnalysisResultsAsync`), on the caller's thread if possible, rather than enumerating the live series on the worker.
3. **Don't clear `IsEstimated` for an exception caused by inputs changing mid-run**, as the related issue suggests; retry or discard instead.

Fix 1 alone removes this race. None of this changes what's saved.

## How XGoodFit handles it

XGoodFit fixes this in its port: the project owner decided to fix upstream bugs that lose users' work and to record each change in [`docs/differences-from-rmc-bestfit.md`](../docs/differences-from-rmc-bestfit.md). Until that fix was in, its test script ran `PointEstimatorControlContractTests` in a process of its own with retries, and said why: [`scripts/run-app-tests.sh` lines 92-105](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/scripts/run-app-tests.sh#L92-L105).
