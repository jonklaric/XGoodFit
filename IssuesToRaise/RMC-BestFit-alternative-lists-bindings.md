# The alternatives lists enable unestimated analyses, don't sort by name, and don't follow a time series' data

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.xaml(.cs)` and `src/RMC.BestFit.App/GUI/Support/ComboBoxItems/TimeSeriesAlternativeItem.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

Three small binding problems in the lists that let users overlay other analyses or time series on a plot:

1. **Every analysis alternative is enabled.** The check box binds `IsEnabled` to `Alternative.Analysis.IsEstimated`, but no analysis element has an `Analysis` property. The binding fails and the check box keeps its default, enabled, whether the analysis is estimated or not.
2. **The analysis list isn't sorted by name.** The sort description uses `nameof(AnalysisAlternativeItem.Alternative.Name)`, which is just `"Name"`. `AnalysisAlternativeItem` has no `Name` property, so every item sorts as equal.
3. **Time series check boxes don't follow the data.** `TimeSeriesAlternativeItem.HasData` is computed from the element's `TimeSeries`, and the check box's `IsEnabled` is bound to it, but the item raises `PropertyChanged` for `TimeSeries` and `Name` only, never for `HasData`.

## Details and how to reproduce

1. In an analysis view with alternatives (for example Mixture, Point Process or Rating Curve), add an analysis that hasn't been estimated. Expected: its check box is disabled until it's estimated. Actual, by reading: it's enabled, and a binding error for `Analysis` is logged.
2. Create alternatives named "C", "A" and "B". Expected: the list shows A, B, C. Actual, by reading: it isn't sorted by name.
3. In the Time Series view's alternative list, import data into a time series that was empty. Expected: its check box becomes enabled. Actual, by reading: it stays disabled until the list is rebuilt. Clearing a series' data leaves its check box enabled in the same way.

## Why this is an issue and not intended behaviour

Each binding names what it means to do, and the analogous code elsewhere works: `IAnalysisElement` has `IsEstimated` itself; `AlternativeTimeSeriesControl` sorts by the path `"Alternative.Name"`; and `HasData` is a computed property, so the item has to raise a change for it when its input changes.

## Where in the source

1. [`src/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.xaml` line 17](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.xaml#L17):
   ```xml
   <CheckBox IsChecked="{Binding IsChecked}" IsEnabled="{Binding Alternative.Analysis.IsEstimated}">
   ```
   `Alternative` is an `IAnalysisElement` ([`AnalysisAlternativeItem.cs` line 42](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/ComboBoxItems/AnalysisAlternativeItem.cs#L42)), which declares `IsEstimated` ([`src/RMC.BestFit.UI/Elements/Support/IAnalysisElement.cs` line 59](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/IAnalysisElement.cs#L59)). No element class in `src/RMC.BestFit.UI/Elements` has a public `Analysis` property.
2. [`AlternativeControl.xaml.cs` line 321](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.xaml.cs#L321):
   ```csharp
   csv.SortDescriptions.Add(new SortDescription(nameof(AnalysisAlternativeItem.Alternative.Name), ListSortDirection.Ascending));
   ```
   The working version: [`src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/AlternativeTimeSeriesControl.xaml.cs` line 256](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/AlternativeTimeSeriesControl.xaml.cs#L256).
3. [`src/RMC.BestFit.App/GUI/Support/ComboBoxItems/TimeSeriesAlternativeItem.cs` lines 51-54](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/ComboBoxItems/TimeSeriesAlternativeItem.cs#L51-L54) (`HasData`) and [lines 110-116](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/ComboBoxItems/TimeSeriesAlternativeItem.cs#L110-L116):
   ```csharp
   if (e.PropertyName == nameof(Alternative.TimeSeries) || e.PropertyName == nameof(Alternative.Name))
   {
       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
   }
   ```
   The binding: [`AlternativeTimeSeriesControl.xaml` line 17](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/AlternativeTimeSeriesControl.xaml#L17).

## Suggested fix

1. `IsEnabled="{Binding Alternative.IsEstimated}"`.
2. `new SortDescription("Alternative.Name", ListSortDirection.Ascending)`, as `AlternativeTimeSeriesControl` does.
3. When `TimeSeries` changes, also raise `HasData`:
   ```csharp
   if (e.PropertyName == nameof(Alternative.TimeSeries))
       PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasData)));
   ```

No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps all three for now: [`src/BestFit/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.axaml` line 22](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.axaml#L22), [`AlternativeControl.axaml.cs` line 334](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/AlternativeControl.axaml.cs#L334), and `TimeSeriesAlternativeItem.cs` compiled unchanged ([`RMC.BestFit.App.csproj` line 107](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj#L107)).
