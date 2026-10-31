# The Point Process view keeps showing the AMS points of an undone Total Years

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs` (`Element_PropertyChanged`, `SetAMSData`) and `src/RMC.BestFit.UI/Elements/UnivariateAnalysis/PointProcessAnalysis.cs` (`RestoreModelFromSnapshot`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

The Point Process view plots the input data's annual-maximum equivalents: a copy of the data frame converted with Langbein's formula at the model's λ (events per year). It rebuilds that copy only when the element raises `InputData`, `DataFrame` or `TotalYears`. Undoing a model edit, such as **Total Years**, replaces the whole model and raises `PointProcess` instead. So after the undo the frequency plot is redrawn from the copy made for the undone exposure, and its AMS points stay wrong until the view is reopened.

## Details and how to reproduce

1. Open a point-process analysis with input data, for example in `examples/4-univariate-distribution-analysis/3-point-process-analysis/point-process-examples.bestfit`, and show its frequency plot.
2. Untick **Use Defaults** and change **Total Years**, for example to half its value. The AMS points move, because λ changes.
3. Press Undo. Expected: the points return to where they were. Actual, by reading: the model's Total Years is restored, but the plot keeps the AMS points computed for the undone value.

Reopening the view, or another edit that raises `TotalYears` or `InputData`, puts them right.

## Why this is an issue and not intended behaviour

The view's handler already treats `TotalYears` as a reason to rebuild the AMS copy, because the copy depends on λ. A restored model changes `TotalYears` too, just under another notification name, so the copy is stale. The element raises `PointProcess` deliberately on restore ("Replays a model XElement snapshot by reconstructing `_innerAnalysis.PointProcess`"), so the view needs to listen for it.

## Where in the source

- The view rebuilds the copy only for these three names: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs` lines 224-228](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs#L224-L228):
  ```csharp
  if (e.PropertyName == nameof(Element.InputData) || e.PropertyName == nameof(Element.InputData.DataFrame) || e.PropertyName == nameof(Element.PointProcess.TotalYears))
  {
      SetAMSData();
      UpdateFrequencyPlot();
  }
  ```
  and `SetAMSData`, [lines 271-276](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.xaml.cs#L271-L276):
  ```csharp
  amsDataFrame = Element.InputData.DataFrame.Clone();
  amsDataFrame.ApplyLangbeinConversion(Element.PointProcess.Lambda);
  ```
- The undo replaces the model and raises `PointProcess`: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/PointProcessAnalysis.cs` lines 1396-1416](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/PointProcessAnalysis.cs#L1396-L1416) (`RaisePropertyChange(nameof(PointProcess))` at line 1409), recorded by `RecordModelUndo` ([lines 1370-1388](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/PointProcessAnalysis.cs#L1370-L1388)) for the names in `ModelUndoProperties`, which include `TotalYears` ([lines 335-340](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/PointProcessAnalysis.cs#L335-L340)).

## Suggested fix

Treat a replaced model like a changed exposure in `Element_PropertyChanged`:

```csharp
if (e.PropertyName == nameof(Element.InputData) || e.PropertyName == nameof(Element.InputData.DataFrame)
    || e.PropertyName == nameof(Element.PointProcess.TotalYears) || e.PropertyName == nameof(Element.PointProcess))
```

The same restore affects every model property the view reads, so it's worth checking the view's other handlers for names a restore doesn't raise. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's handler for now: [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.axaml.cs` line 281](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/PointProcess/PointProcessAnalysisControl.axaml.cs#L281).
