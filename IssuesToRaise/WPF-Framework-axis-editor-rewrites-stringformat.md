# The axis editor rewrites an axis's StringFormat just by showing it (null becomes "")

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/OxyPlotControls/Axes/AxisControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Affects** RMC-BestFit 2.0.1's plot settings, which are saved in `.bestfit` files as plot XML.

**Type:** bug · **Severity:** medium

**Verification:** verified by reading the source. The first case below was seen by XGoodFit's port of the editor, where the saved plot XML changed. The second case follows the same code path; it was read, not run.

## Summary

When the editor is given an axis, `InitializePlot` calls `SyncLabelTypeFromStringFormat()` without the `_isSyncingFromStringFormat` guard that `OnAxisPropertyChanged` uses. The sync sets `DecimalPlaces.Number`, whose `PropertyChanged` handler then writes `Axis.StringFormat = _stringFormatCategory + _stringFormatDecimals`. That runs before the sync has updated `_stringFormatCategory`, so it uses a stale category.

1. **First axis shown, `StringFormat` null:** `Number` goes from its default 0 to `NaN`, the handler runs with `_stringFormatCategory == ""`, and the axis's `StringFormat` becomes `""`. Merely opening the plot properties changes the plot's saved XML.
2. **A later axis with a different category (by reading):** if the editor showed an axis with no format (category left at "G") and then shows one with `"N2"`, `Number` changes from `NaN` to 2 and the handler writes `"G" + "2"`. `N2` (number with group separators) silently becomes `G2` (two significant digits).

## Details and how to reproduce

In RMC-BestFit, open a plot whose Y axis has no `StringFormat` and open Plot Properties > Axes, without changing anything. Save the project. **Expected:** the plot XML is unchanged. **Actual:** the axis gains `StringFormat=""`.

## Why this is an issue and not intended behaviour

The guard exists precisely so that syncing the editor from the axis doesn't write back to it. Its comment says it keeps the controls in sync "when `StringFormat` changes externally". The initial sync skips it, and the handler (`DecimalPlaces_PropertyChanged`) checks only that guard.

## Where in the source

- The unguarded sync: [`AxisControl.xaml.cs` lines 272-275](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L272-L275), inside `InitializePlot` (from [line 89](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L89)).
- The fields: [lines 594-596](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L594-L596) (`_stringFormatCategory = ""`).
- The write-back: [lines 641-653](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L641-L653)
  ```csharp
  if (_isSyncingFromStringFormat) return;
  ...
  Axis.StringFormat = _stringFormatCategory + _stringFormatDecimals;
  ```
- The guarded sync: [lines 676-690](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L676-L690). The sync itself sets `DecimalPlaces.Number` before `_stringFormatCategory`: [lines 739-750](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Axes/AxisControl.xaml.cs#L739-L750).
- `NumericAutoPropertyControl.Number` defaults to 0 and raises `PropertyChanged` on every change: [`GenericControls/Properties Controls/NumericAutoPropertyControl.xaml.cs` lines 40 and 105](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Properties%20Controls/NumericAutoPropertyControl.xaml.cs#L40).

## Suggested fix

```csharp
// InitializePlot
thisControl._isSyncingFromStringFormat = true;
try { thisControl.SyncLabelTypeFromStringFormat(); }
finally { thisControl._isSyncingFromStringFormat = false; }
```

Also, in `SyncLabelTypeFromStringFormat`, assign `_stringFormatCategory` and `_stringFormatDecimals` before setting `DecimalPlaces.Number`. **Compatibility:** files already saved with `StringFormat=""` behave like null (OxyPlot treats an empty format as the default), so no migration is needed.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the unguarded call in its OxyPlotControls port (branch `t3/port-oxyplot-controls`, not yet on `main`), so it saves what WPF saves.
