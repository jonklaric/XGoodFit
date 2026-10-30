# Two model tests depend on wall-clock timing and fail intermittently on CI runners

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.Tests`: `BatchAnalysis.BatchAnalysisRunnerTests.RunAsync_MultipleAnalyses_Parallel` and `Bivariate.CoincidentFrequencyAnalysisTests.Cancel_DuringParallelLoop_StopsAndReportsCanceled`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-30.

**Type:** test reliability · **Severity:** low

**Verification:** verified by reading the source. Both failed intermittently on GitHub-hosted CI runners while XGoodFit ran the suite on Windows and Linux. They didn't fail on a 12-core Windows laptop: 0 of 60 isolated runs each, and 4 full parallel suite runs.

## Summary

- `RunAsync_MultipleAnalyses_Parallel` runs three 100 ms mock analyses in parallel and asserts that the whole batch takes under 250 ms. The test assembly runs tests in parallel at method level (`[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`), so CPU-heavy MCMC tests compete with it for the thread pool, and on a loaded runner the limit is exceeded.
- `Cancel_DuringParallelLoop_StopsAndReportsCanceled` cancels a coincident-frequency run 50 ms after starting it, with work its own comment says is sized "for Debug". In a Release build on a fast machine the run can finish first, and the test sees `Succeeded=True` instead of `Cancelled`.

## Details and how to reproduce

Run the whole `RMC.BestFit.Tests` assembly in Release on a 2-4 core CI runner, repeatedly. Seen in XGoodFit's CI: `Expected parallel execution < 250ms, got …ms` on Windows and Linux, and `AnalysisRunCompletedEventArgs.Cancelled must report true … Succeeded=True` on Windows (30 October 2026).

## Why this is an issue and not intended behaviour

Both tests are about behaviour (analyses run concurrently; cancellation is honoured), but they assert on time, which depends on the machine and on what else the parallel test run is doing. The cancel test's comment says the work is enough "in Debug for the cancel token to be observed mid-loop", which doesn't hold in Release.

## Where in the source

- [`src/RMC.BestFit.Tests/BatchAnalysis/BatchAnalysisRunnerTests.cs` lines 260-285](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.Tests/BatchAnalysis/BatchAnalysisRunnerTests.cs#L260-L285)
  ```csharp
  Assert.IsTrue(sw.ElapsedMilliseconds < 250, $"Expected parallel execution < 250ms, got {sw.ElapsedMilliseconds}ms");
  ```
- [`src/RMC.BestFit.Tests/Bivariate/CoincidentFrequencyAnalysisTests.cs` lines 846-883](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.Tests/Bivariate/CoincidentFrequencyAnalysisTests.cs#L846-L883)
  ```csharp
  // 5000 copula realisations × 50 bins × 5×5 grid is enough wall-clock work in
  // Debug for the cancel token to be observed mid-loop. ...
  await Task.Delay(50);
  cfa.CancelAnalysis();
  ```
- [`src/TestCommon/MSTestSettings.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/TestCommon/MSTestSettings.cs): method-level parallelism.

## Suggested fix

- **Parallel test:** assert concurrency, not speed. Have the mock analyses record how many are running at once (an `Interlocked` counter and a max) and assert that the maximum is greater than 1. Or make the mocks wait on a barrier that only releases when all three have started. Mark it `[DoNotParallelize]` if a timing check stays.
- **Cancel test:** make cancellation deterministic. Cancel from inside the loop, for example from a progress callback or a hook raised on the first iteration, or have a test delegate block until the token is cancelled, instead of relying on a 50 ms head start.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) runs the model tests unchanged except for these, which it leaves out with the reasons above: [`scripts/run-upstream-tests.sh` lines 57-70](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/scripts/run-upstream-tests.sh#L57-L70).
