# Axis Minimum and Maximum raise no PropertyChanged, so plot undo never records axis-range edits

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs` (`MinMaxChanged`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Affects** RMC-BestFit 2.0.1's plot undo (`PlotUndoManager`), and the same class in `FrameworkUI.Demo`.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

`Axis` implements `INotifyPropertyChanged`, and most of its properties raise `PropertyChanged` through `AppearanceChanged` or `DataChanged`. `Minimum` and `Maximum` use `MinMaxChanged`, which calls `OnVisualChanged()` but not `OnPropertyChanged`. `AxisSerializer.AxisVisualProperties` lists both, and its documentation says "Consumers can use this list to monitor these properties for change tracking". RMC-BestFit's `PlotUndoManager` does exactly that, so changing an axis's minimum or maximum in the plot properties is never added to the undo history.

## Details and how to reproduce

In RMC-BestFit, open a plot's properties, change the Y axis title, then its maximum, then press Undo twice. **Expected:** the maximum, then the title, are undone. **Actual:** the first Undo reverts the title; the maximum change was never recorded.

## Why this is an issue and not intended behaviour

The serializer's list includes `Maximum` and `Minimum` for change tracking, and every other entry in it raises `PropertyChanged`. `MinMaxChanged` differs from `AppearanceChanged` only by the zoom reset, so the missing call looks accidental.

## Where in the source

- [`src/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs` lines 261-262 and 279-280](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs#L261-L280): both registered with `MinMaxChanged`.
- [`Axis.cs` lines 1013-1048](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs#L1013-L1048)
  ```csharp
  protected static void AppearanceChanged(...) { ... axis.OnVisualChanged(); axis.OnPropertyChanged(e.Property.Name); }
  private static void MinMaxChanged(...) { ... axis.OnVisualChanged(); }   // no OnPropertyChanged
  ```
- [`src/OxyPlot/OxyPlot.Wpf/Serialization/AxisSerializer.cs` lines 34-43](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlot/OxyPlot.Wpf/Serialization/AxisSerializer.cs#L34-L43): `"Maximum", "Minimum", …` "for change tracking".
- The consumer: [RMC-BestFit `src/RMC.BestFit.UI/Elements/Support/PlotUndoManager.cs` lines 65-70 and 318-330](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/PlotUndoManager.cs#L65-L70).

## Suggested fix

Add `axis.OnPropertyChanged(e.Property.Name);` at the end of `MinMaxChanged`. Zooming and panning change the internal view range, not these properties, so this records only explicit edits. No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the behaviour in its port of `OxyPlot.Wpf` ([`src/Framework/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/OxyPlot/OxyPlot.Wpf/Axes/Axis.cs)).
