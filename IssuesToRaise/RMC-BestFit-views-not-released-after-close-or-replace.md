# Closed document views stay subscribed to their elements, so they're never released

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/MainProjectNode.cs` (`DocumentClosed`) and `src/RMC.BestFit.App/GUI/Support/Controls/ParameterSetsControl.xaml.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug (memory leak) · **Severity:** low

**Verification:** verified by reading the source; not measured.

## Summary

Each document view subscribes to its element's `PropertyChanged` when its `Element` is set, and unsubscribes only when `Element` changes again. When a document is closed, `DocumentClosed` sets `Element = null` for only one of the twelve views it handles, so the other eleven stay subscribed to an element that lives as long as the project. Opening a document creates a new view each time, so every open and close leaves another view, with its grids and plots, in memory and handling the element's events. `ParameterSetsControl` never unsubscribes at all, so even the one view that is cleared leaves its parameter-sets grid behind.

## Details and how to reproduce

1. Open an Input Data element, close it, and repeat a few times.
2. Change the element (for example, rename it or edit it from the properties pane).
3. Expected: only the open view, if any, handles the change; closed views are collectable. Actual, by reading: every view ever opened for the element is still referenced from `InputData.PropertyChanged` and runs `ElementPropertyChanged`.

A memory profiler, or a `WeakReference` to a closed view followed by a forced collection, would show this.

The same holds for the Time Series, Fitting Analysis, Univariate, Bulletin 17C, Point Process, Mixture, Composite, Bivariate, Coincident Frequency and Rating Curve views. `ParameterSetsControl` (hosted by the Univariate, Bulletin 17C, Point Process, Mixture, Bivariate, Rating Curve and Time Series Analysis views) subscribes to the analysis's `PropertyChanged` with no matching unsubscribe, and returns early when the new value is null, so setting its `Element` to null doesn't release it either. Each instance holds a `DataTable` of all the posterior parameter sets.

## Why this is an issue and not intended behaviour

- `DocumentClosed`'s own documentation says it unregisters handlers "to prevent memory leaks", and it does so for plot events. The properties views closed in `PropertiesClosed` all get `Element = null` ([lines 1201-1233](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1201-L1233)), and so does `TimeSeriesAnalysisControl` in `DocumentClosed`. The other document views look like an omission.
- The leaked views keep doing work: each one rebinds its grids when the element changes, or, for `ParameterSetsControl`, rebuilds the parameter-sets table when the analysis is re-estimated.

## Where in the source

- `DocumentClosed`, [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` lines 1085-1185](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1085-L1185): only the Time Series Analysis case ends with `cntrl.Element = null;` ([line 1181](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1181)).
- A new view per open, for example [line 864](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L864): `var cntrl = new InputDataControl() { Element = (InputData)element };`
- The views subscribe in their `Element` callbacks and unsubscribe only from the old value, for example [`InputData/InputDataControl.xaml.cs` lines 96 and 128](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L96-L128) and [`FittingAnalysis/FittingAnalysisControl.xaml.cs` lines 91 and 115](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs#L91-L115). `InputDataControl`'s `Unloaded` handler drops only the series' `CollectionChanged` subscriptions ([lines 410-414](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L410-L414)).
- `ParameterSetsControl.ElementCallback`, [`src/RMC.BestFit.App/GUI/Support/Controls/ParameterSetsControl.xaml.cs` lines 59-69](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/ParameterSetsControl.xaml.cs#L59-L69)
  ```csharp
  if (e.NewValue == null) return;
  var newElement = e.NewValue as BayesianAnalysis;
  if (newElement == null) return;

  newElement.PropertyChanged += thisControl.Analysis_PropertyChanged;
  ```

## Suggested fix

1. In `DocumentClosed`, set `Element = null` for every document view, as for `TimeSeriesAnalysisControl` and the properties views.
2. In `ParameterSetsControl.ElementCallback`, unsubscribe from the old value first:
   ```csharp
   if (e.OldValue is BayesianAnalysis oldElement)
       oldElement.PropertyChanged -= thisControl.Analysis_PropertyChanged;
   ```
3. Alternatively, have the views subscribe in `Loaded` and unsubscribe in `Unloaded`, or use `WeakEventManager`, so closing doesn't depend on the shell.

No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's behaviour for now: `DocumentClosed` still clears only the Time Series Analysis view ([`src/BestFit/RMC.BestFit.App/GUI/MainProjectNode.cs` line 1252](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/MainProjectNode.cs#L1252)), and `ParameterSetsControl` subscribes without unsubscribing ([`ParameterSetsControl.axaml.cs` line 73](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/ParameterSetsControl.axaml.cs#L73)).
