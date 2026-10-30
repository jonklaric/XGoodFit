# The bivariate X-Y plot's contour levels come from a racy parallel maximum

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug (race condition) · **Severity:** low

**Verification:** reproduced with the same loop shape. A small program ran the loop below (`n = 100`, the same captured `minf`/`maxf` updates) 2,000 times over a fixed 100 × 100 grid on a 16-thread machine: `maxf` was wrong in 1,090 runs. Its loop body is cheaper than the real one (which evaluates three log-densities per cell), so the real loop races less often, but it can still lose updates. XGoodFit's porting agents reported two runs of the same analysis giving different maxima (−7.3069 and −7.3127); I didn't reproduce that.

## Summary

To draw the joint density's contours, the view evaluates the log-density on a 100 × 100 grid inside `Parallel.For` and tracks the extremes with `minf = Math.Min(minf, …)` and `maxf = Math.Max(maxf, …)` on variables shared by all the threads. These read-modify-write updates race, so `maxf` can miss the true maximum. The contour levels are spread between a minimum derived from `maxf` and `maxf` itself, so the plot can differ from one redraw to the next for the same fitted model.

## Details and how to reproduce

```csharp
double minf = double.MaxValue, maxf = double.MinValue;
Parallel.For(0, n, (int i) =>
{
    for (int j = 0; j < n; j++)
    {
        pdf[i, j] = values[i, j];
        minf = Math.Min(minf, pdf[i, j]);
        maxf = Math.Max(maxf, pdf[i, j]);
    }
});
// maxf != the true maximum in 1,090 of 2,000 runs
```

Expected: the same contour levels on every redraw of the same model. Actual: the top level, and in rare cases the lowest one (its decade is derived from `maxf`), can vary.

## Why this is an issue and not intended behaviour

The extremes are meant to be those of the grid that was just computed; a lost update makes them depend on thread timing. The loop's `minf` result isn't even used: it's overwritten right after the loop.

## Where in the source

[`src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs` lines 613-631](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs#L613-L631):

```csharp
Parallel.For(0, n, (int i) =>
{
    for (int j = 0; j < n; j++)
    {
        pdf[i, j] = copula.LogPDF(Fx[i], Fy[j]) + xDist.LogPDF(xF[i]) + yDist.LogPDF(yF[j]);
        minf = Math.Min(minf, pdf[i, j]);
        maxf = Math.Max(maxf, pdf[i, j]);
    }
});

minf = Math.Log(1d / Math.Pow(10, Math.Ceiling(Math.Log10(1d / Math.Exp(maxf)) + 2.5)));
var zVals = Stratify.XValues(new StratificationOptions(minf, maxf, 9), false);
```

## Suggested fix

Fill `pdf` in parallel, then take the maximum afterwards on one thread (10,000 values):

```csharp
Parallel.For(0, n, i => { for (int j = 0; j < n; j++) pdf[i, j] = ...; });
double maxf = double.MinValue;
foreach (double v in pdf) maxf = Math.Max(maxf, v);
```

Or keep a per-row maximum (`double[] rowMax`) and combine it after the loop. Drop the unused `minf` updates. No compatibility risk; nothing is saved.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps the loop for now ([`src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.axaml.cs` line 681](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.axaml.cs#L681)).
