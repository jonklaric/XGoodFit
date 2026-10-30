# TimeSeriesTable's math button binds to an element named PresetsPopup that doesn't exist

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/NumericControls/Data/Time Series Editor/TimeSeriesTable.xaml`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source. The effect described is the one this binding pattern exists to prevent; it wasn't run.

## Summary

The "Math Functions" toggle button sets `IsHitTestVisible="{Binding ElementName=PresetsPopup, Path=IsOpen, Converter={StaticResource ReverseBoolConverter}}"`, but the popup it opens is named `MathPopup`. No element is named `PresetsPopup`, so the binding fails (WPF logs it in the output window) and the button is always hit-testable.

## Details and how to reproduce

Open the time-series table's math popup with the button, then click the button again to close it. **Expected:** it closes, as the date-time picker button in the same file does. **Probable actual:** the click first closes the `StaysOpen="False"` popup, then reaches the toggle button and opens it again. The `IsHitTestVisible = !Popup.IsOpen` binding is there to stop exactly that.

## Why this is an issue and not intended behaviour

The same file uses the same pattern correctly for its date-time button, `IsHitTestVisible="{Binding ElementName=DateTimePopup, Path=IsOpen, …}"` ([line 211](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml#L211)), with a popup that does exist. `PresetsPopup` looks like a name left over from a rename.

## Where in the source

[`TimeSeriesTable.xaml` lines 130-166](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml#L130-L166)

```xml
<ToggleButton x:Name="PresetToggleButton" ...
    IsHitTestVisible="{Binding ElementName=PresetsPopup, Path=IsOpen, Converter={StaticResource ReverseBoolConverter}}">
...
<Popup x:Name="MathPopup" ... IsOpen="{Binding ElementName=PresetToggleButton, Path=IsChecked, Mode=TwoWay}" StaysOpen="False" ...>
```

## Suggested fix

Change `ElementName=PresetsPopup` to `ElementName=MathPopup`. It's a one-word change with no other impact.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the broken binding, see the header comment of [`TimeSeriesTable.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.axaml) (lines 23-24).
