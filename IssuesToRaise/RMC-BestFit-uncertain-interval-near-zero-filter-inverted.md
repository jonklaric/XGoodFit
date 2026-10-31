# Frequency plots drop zero-valued uncertain and interval data on linear axes, and keep them on log axes

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI`: the frequency plots of the Input Data, Fitting Analysis, Univariate, Bulletin 17C, Mixture, Point Process, Composite and Coincident Frequency views. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

Each frequency plot leaves values of 1E-16 or less out of its data series when its Y axis is logarithmic, where they can't be drawn. For exact data and low outliers the condition is right. For uncertain and interval data it's reversed:

```csharp
// exact data: filter on log axes
logAxis == true ? ExactSeries.Where(x => ... && x.Value > 1E-16) : ExactSeries.Where(x => ...)
// uncertain and interval data: filter on linear axes
logAxis == true ? UncertainSeries : UncertainSeries.Where(x => x.Value > 1E-16)
```

So an uncertain or interval observation whose value is zero goes to a log axis, which can't show it, and is left out of a linear axis, which could. The same two lines are in eight views.

## Details and how to reproduce

1. In an Input Data element, add an interval observation with a most-likely value of 0 (for example a no-flow year: lower 0, most likely 0, upper 50).
2. Open the frequency plot and change its Y axis to linear.
3. Expected: the interval is drawn at zero. Actual, by reading: it's missing.
4. With the default logarithmic Y axis the point is given to the series. OxyPlot skips the marker, but `ScatterErrorSeries.Render` transforms the error bar's ends without checking them, so a zero bound goes to an infinite screen position.

The analyses aren't affected; only the plotted data is.

## Why this is an issue and not intended behaviour

The filter exists because zero and negative values can't be placed on a log axis, as the exact-data lines in the same methods show. Applying it only on linear axes has no purpose: a linear axis shows zero. Every view repeats the same pair of lines, so this looks like one copied slip.

## Where in the source

The uncertain-data and interval-data lines, with the exact-data line that does it right:

| View | Exact data (right) | Uncertain data | Interval data |
|---|---|---|---|
| [`InputData/InputDataControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L1319-L1340) | 1319 | 1331 | 1340 |
| [`FittingAnalysis/FittingAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs#L958-L979) | 958 | 970 | 979 |
| [`UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs#L659-L680) | 659 | 671 | 680 |
| [`UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L591-L614) | 591 | 605 | 614 |
| [`UnivariateAnalysis/Mixture/MixtureAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Mixture/MixtureAnalysisControl.xaml.cs#L565-L586) | 565 | 577 | 586 |
| [`UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs#L717-L763) | 717 (and 745 for the AMS) | 727 and 755 | 735 and 763 |
| [`UnivariateAnalysis/Composite/CompositeAnalysisControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisControl.xaml.cs#L525-L546) | 525 | 537 | 546 |
| [`BivariateAnalysis/CoincidentFrequency/CoincidentFrequencyControl.xaml.cs`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/CoincidentFrequency/CoincidentFrequencyControl.xaml.cs#L550-L579) | 550-552 | 566-568 | 577-579 |

For example [`UnivariateAnalysisControl.xaml.cs` line 671](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.xaml.cs#L671):

```csharp
frequencyUncertainData.ItemsSource = logAxis == true ? Element.InputData.DataFrame.UncertainSeries : Element.InputData.DataFrame.UncertainSeries.Where(x => x.Value > 1E-16);
```

The error bars: [WPF-Framework `src/OxyPlot/OxyPlot/Series/ScatterErrorSeries.cs` lines 187-201](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlot/OxyPlot/Series/ScatterErrorSeries.cs#L187-L201).

## Suggested fix

Swap the branches in the 18 lines:

```csharp
frequencyUncertainData.ItemsSource = logAxis == true ? UncertainSeries.Where(x => x.Value > 1E-16) : UncertainSeries;
```

On log axes, consider also leaving out points whose lower bound is ≤ 0, or clamping the bar, since the bar's lower end can't be drawn either. A shared helper (`PlotData.ForAxis(series, logAxis)`) would keep the eight views from drifting apart again. No compatibility risk: only the plotted points change.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's lines in all eight views for now, for example [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.axaml.cs` lines 724 and 733](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisControl.axaml.cs#L724-L733) and [`CoincidentFrequencyControl.axaml.cs` lines 620-633](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/CoincidentFrequency/CoincidentFrequencyControl.axaml.cs#L620-L633).
