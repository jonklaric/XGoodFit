# The Input Data summary statistics separators are hard-coded black, so they vanish in the Dark theme

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

The Input Data view's summary statistics grid draws separator lines above "Minimum" and below "Kurtosis (of log)" with a static black brush. On the Dark theme's dark background they're all but invisible. The same grid in the Distribution Fitting view uses the theme's `EnvironmentWindowText` brush, which follows the theme.

## Details and how to reproduce

Switch to the Dark theme and open an Input Data element with data. Expected: the summary statistics sections are separated by visible lines, as in the Fitting Analysis view. Actual, by reading: the lines are black on a dark background.

## Why this is an issue and not intended behaviour

The lines exist to separate sections, and the Fitting Analysis view's equivalent code uses the theme brush for the same purpose.

## Where in the source

- [`src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs` line 270](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L270): `private static readonly SolidColorBrush _borderBrush = new SolidColorBrush(Colors.Black);`, used at [lines 3566-3579](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L3566-L3579):
  ```csharp
  if (((SummaryStatistic)e.Row.DataContext).Name == "Minimum")
  {
      e.Row.BorderThickness = _topBorderThickness;
      e.Row.BorderBrush = _borderBrush;
  }
  ```
- The Fitting Analysis view: [`src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs` lines 1620-1625](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs#L1620-L1625):
  ```csharp
  e.Row.SetResourceReference(Border.BorderBrushProperty, "EnvironmentWindowText");
  ```

## Suggested fix

Use the same resource reference as the Fitting Analysis view in both places, and remove `_borderBrush`:

```csharp
e.Row.SetResourceReference(Border.BorderBrushProperty, "EnvironmentWindowText");
```

No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps the black brush for now ([`src/BestFit/RMC.BestFit.App/GUI/InputData/InputDataControl.axaml.cs` line 309](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/InputData/InputDataControl.axaml.cs#L309), used at lines 3620 and 3625).
