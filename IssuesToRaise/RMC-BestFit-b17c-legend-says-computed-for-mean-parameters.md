# Bulletin 17C's frequency plot legend says "Computed" whatever the point estimator

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs` (`UpdateFrequencyPlot`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

A Bulletin 17C analysis offers two point estimators, **Computed** (the GMM estimate) and **Mean Parameters** (the mean of the uncertainty ensemble). The view relabels its tables when the estimator changes, and the alternatives' curves are labelled to match, but the analysis's own curve in the frequency plot keeps the title it was created with, "Computed". With **Mean Parameters** selected, the legend and the tracker call the mean-parameter curve "Computed", while the tables beside it say "Mean Parameters".

## Details and how to reproduce

1. Open "Example #1" in `examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.bestfit`.
2. In **Properties > Options**, set **Point Estimator** to **Mean Parameters**.
3. Expected: the frequency plot's legend names the solid curve "Mean Parameters", as the frequency curve table's column and the summary statistics column do. Actual, by reading: the legend still says "Computed".

The title is set only when the series is first created. The series is then found by name and reused, so a title saved with the plot also persists.

## Why this is an issue and not intended behaviour

The same view gets this right everywhere else: the frequency curve table's header and the summary column follow the estimator, and so does an alternative's curve title (" - Mean Parameters" or " - Computed"). The Univariate view retitles its own point-estimate series in the same place on every update ("Posterior Mean" or "Posterior Mode"). B17C's update is a copy of that code without the title line.

## Where in the source

- The series is created titled "Computed": [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs` lines 446-454](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L446-L454).
- Each update fills it but doesn't retitle it: [lines 557-562](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L557-L562):
  ```csharp
  // Point Estimator
  posteriorMode.ItemsSource = mdPoints;
  ...
  plot.Series.Add(posteriorMode);
  ```
- The parts that follow the estimator: the alternatives' title, [line 709](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L709); the frequency curve table, [line 789](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L789); the summary statistics, [line 844](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L844):
  ```csharp
  ModeColumn.Header = Element.BayesianAnalysis.PointEstimator == BayesianAnalysis.PointEstimateType.PosteriorMean ? "Mean Parameters" : "Computed";
  ```
- The estimator's names: [`B17CAnalysisPropertiesControl.xaml.cs` lines 179-183](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml.cs#L179-L183).
- The Univariate view's version: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs` line 641](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs#L641).

## Suggested fix

Set the title before adding the series, as the Univariate view does:

```csharp
posteriorMode.Title = Element.BayesianAnalysis.PointEstimator == BayesianAnalysis.PointEstimateType.PosteriorMean ? "Mean Parameters" : "Computed";
```

The view already updates the plot when the estimator changes (the reprocess raises `AnalysisResults`). No compatibility risk: a title saved with the plot is overwritten on the next update, as in the Univariate view.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's behaviour for now: [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.axaml.cs` line 503](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.axaml.cs#L503) creates the series titled "Computed", and [line 611](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.axaml.cs#L611) fills it without retitling it.
