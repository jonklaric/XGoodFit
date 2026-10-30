# NumericUpDown and NumericSlider lose their DecimalPlaces and ThousandsSeparator formatting straight away

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/General Controls/NumericUpDown.xaml(.cs)` and `NumericSlider.xaml(.cs)`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package under en-US (same harness as [WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md](WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md)).

## Summary

`DecimalPlaces` and `ThousandsSeparator` are applied by `SetText`, which writes the formatted text into the inner text box. That text box is also two-way bound to `Value`, and the binding writes its own default text over the formatted one. So the controls never show their configured format: `23` instead of `23.0000`, and `1500` instead of `1,500`.

## Details and how to reproduce

Under en-US, controls created in code and laid out without a window:

| Control and settings | Value set | Expected text | Shown |
|---|---|---|---|
| NumericSlider, `DecimalPlaces = 4` | 23 | `23.0000` | `23` |
| NumericUpDown, `DecimalPlaces = 2` | 1 | `1.00` | `1` |
| NumericUpDown, `DecimalPlaces = 0`, `ThousandsSeparator = true` | 1500 | `1,500` | `1500` |

`SetText` runs in `Value`'s property-changed callback and sets `NumericTextBox.Text` to the formatted string. The text box's `Text` is also bound two-way to `Value` (`{Binding ElementName=…, Path=Value, Mode=TwoWay}`). When `Value` changes, the binding transfers the new value into `Text` with WPF's default double-to-string conversion, replacing what `SetText` wrote.

In RMC-BestFit 2.0.1 this affects, for example, "Training time steps" (`ThousandsSeparator="True"`) in the time-series analysis properties, and the split-index slider (`DecimalPlaces="4"`) in the input-data view.

## Why this is an issue and not intended behaviour

The properties exist only to format the text. Their documentation says "Gets and sets the number of decimal places to display" and "Gets and sets whether the thousands separator will be displayed" ([`NumericUpDown.xaml.cs` lines 171-203](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml.cs#L171-L203)), and `SetText` builds the matching format string. Nothing else reads them, so as things stand they have no visible effect.

## Where in the source

- The formatting: [`NumericUpDown.xaml.cs` lines 83-104](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml.cs#L83-L104), [`NumericSlider.xaml.cs` lines 76-98](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericSlider.xaml.cs#L76-L98)
  ```csharp
  thisControl.FormatString = thisControl.ThousandsSeparator ? "N" : "F";
  thisControl.FormatString += thisControl.DecimalPlaces.ToString();
  ...
  thisControl.NumericTextBox.Text = NumberFormatHelper.FormatDouble(valueToFormat, thisControl.FormatString);
  ```
- The binding that overwrites it: [`NumericUpDown.xaml` lines 60-62](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml#L60-L62), [`NumericSlider.xaml` line 17](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericSlider.xaml#L17).

## Suggested fix

Let the control own its text instead of round-tripping it through a binding:

- remove the two-way `Text` binding;
- in `SetText`, set the formatted text, as now;
- when the user commits the text (Enter or losing focus, through the inner text box's `TextChanged`/`LostFocus`), parse it with `NumberFormatHelper.TryParseDouble` (current culture, as the control's validation already does), clamp it, and set `Value`.

This fixes the formatting and the culture bug in the linked issue together, with one culture used for both directions. If the binding stays, the format could move into a converter used as the binding's `Converter`, with the format passed in; `DecimalPlaces` and `ThousandsSeparator` would then need to be dependency properties that refresh it.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps this behaviour in [`NumericUpDown.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/General%20Controls/NumericUpDown.axaml) and `NumericSlider.axaml`. Their header comments note that the formatting is replaced by the binding's text, as in WPF.
