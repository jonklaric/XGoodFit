# Analysis views report their main plot as current while a diagnostic tab is showing

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI` (`GetCurrentPlot` and `GetCurrentPlotToolbar` in the Bivariate, Univariate, Bulletin 17C, Mixture, Point Process, Rating Curve and Time Series Analysis views, and `MainProjectNode.GetPropertiesControl`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source. For the Rating Curve and Time Series Analysis views it was also seen in XGoodFit's port, which keeps upstream's order: its view tests select a residual tab, then a diagnostic tab, and `GetCurrentPlot()` still returns the residual plot.

## Summary

These views have an outer tab strip (Distribution Results, Kernel Density, Histogram, Mean Likelihood, …) and, inside Distribution Results, an inner tab control whose first tab holds the main plot. `GetCurrentPlot()` checks the inner plot tab's `IsSelected` before the outer diagnostic tabs. An inner tab stays selected within its own tab control when the user switches to another outer tab, so the method keeps returning the main plot, and the diagnostic plots never become current. `GetCurrentPlotToolbar()` has the same order.

## Details and how to reproduce

The shell uses `GetCurrentPlot()` when a document is activated, to decide whether an open plot-properties pane still belongs to the plot on screen. If the pane's plot equals the current plot it stays; otherwise it's replaced by the element's properties. With the wrong current plot:

1. Open a bivariate analysis, select **Kernel Density**, and open that plot's properties from its toolbar. Activate the document again (for example, click another window and come back). Expected: the Kernel Density plot's properties stay. Actual, by reading: they're replaced by the analysis's properties, because the current plot is reported as the X-Y plot.
2. Open the X-Y plot's properties, then select **Kernel Density** and activate the document. Expected: the X-Y plot's properties are replaced. Actual, by reading: they stay, describing a plot that isn't on screen.

The Rating Curve and Time Series Analysis views have a second inner tab control, **Residual Diagnostics** (Residuals, Histogram and Normal Q-Q plots, and ACF and PACF in Time Series Analysis), which `GetCurrentPlot()` checks after the main plot and before the diagnostic tabs. One residual tab is always selected within it, so in these two views the diagnostic tabs' plots can never become current: once **Tabular Results** is showing, a diagnostic tab reports the residual tab's plot instead.

## Why this is an issue and not intended behaviour

The method's documentation says it "Gets the currently selected plot based on which tab is active." Its later branches exist precisely for the diagnostic tabs, but they can't be reached while the inner plot tab is selected, which is its default state.

## Where in the source

- The Bivariate view: [`src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs` lines 394-425](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs#L394-L425) and [lines 431-436](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml.cs#L431-L436):
  ```csharp
  public Plot GetCurrentPlot()
  {
      if (CopulaResultsTabItem.IsSelected == true)
      {
          return CopulaPlot;
      }
      else if (KernelDensityTabItem.IsSelected == true)
      ...
  ```
  `CopulaResultsTabItem` is in the inner tab control: [`BivariateAnalysisControl.xaml` lines 30-33](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.xaml#L30-L33); the diagnostic tabs are outer tabs at lines 153-183.
- The same order, inner plot tab first: [`UnivariateAnalysisControl.xaml.cs` line 379](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs#L379) (chronology and frequency tabs), and `GetCurrentPlot` in [`B17CAnalysisControl.xaml.cs` line 331](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L331), [`MixtureAnalysisControl.xaml.cs` line 332](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Mixture/MixtureAnalysisControl.xaml.cs#L332), [`PointProcessAnalysisControl.xaml.cs` line 348](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs#L348), [`RatingCurveAnalysisControl.xaml.cs` line 400](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/RatingCurveAnalysis/RatingCurveAnalysisControl.xaml.cs#L400) and [`TimeSeriesAnalysisControl.xaml.cs` line 429](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesAnalysis/TimeSeriesAnalysisControl.xaml.cs#L429).
- The residual tabs, an inner tab control in the Rating Curve and Time Series Analysis views: [`RatingCurveAnalysisControl.xaml` lines 159-195](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/RatingCurveAnalysis/RatingCurveAnalysisControl.xaml#L159-L195), checked at [`RatingCurveAnalysisControl.xaml.cs` lines 406-417](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/RatingCurveAnalysis/RatingCurveAnalysisControl.xaml.cs#L406-L417), before the diagnostic tabs from line 418; and [`TimeSeriesAnalysisControl.xaml` lines 168-224](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesAnalysis/TimeSeriesAnalysisControl.xaml#L168-L224), checked at [`TimeSeriesAnalysisControl.xaml.cs` lines 435-454](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesAnalysis/TimeSeriesAnalysisControl.xaml.cs#L435-L454).
- The shell's use, for example the Bivariate branch of `GetPropertiesControl`: [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` lines 1417-1430](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1417-L1430).

## Suggested fix

Check the outer tab first, then the inner one:

```csharp
if (DistributionResultsTabItem.IsSelected && CopulaResultsTabItem.IsSelected) return CopulaPlot;
if (KernelDensityTabItem.IsSelected) return KernelDensityControl.Plot;
...
```

Or switch on the outer tab control's `SelectedItem`. Do the same in `GetCurrentPlotToolbar()`. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's order for now, for example [`src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.axaml.cs` lines 452-454](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisControl.axaml.cs#L452-L454). Its view tests assert upstream's order in the Rating Curve and Time Series Analysis views: [`tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/RatingCurveAnalysisViewTests.cs` lines 267-278](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/RatingCurveAnalysisViewTests.cs#L267-L278) and [`TimeSeriesAnalysisViewTests.cs` lines 281-299](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/TimeSeriesAnalysisViewTests.cs#L281-L299).
