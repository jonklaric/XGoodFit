# NumericTextBox2 never shows its red error border, because it doesn't implement INotifyPropertyChanged

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/General Controls/NumericTextBox2.xaml(.cs)`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package. With `DataGrid.Cell.Error.Border` defined as red in the application resources, `NumericTextBox2 { MinValue = 0, MaxValue = 10 }` given `Value = 20` reported `ValueIsValid == false` and set its tooltip, but the border's brush stayed `null`, and `box is INotifyPropertyChanged` was `false`.

## Summary

The control's error border is a `DataTrigger` bound to its `ValueIsValid` property. `ValueIsValid` is a plain CLR property that raises a `PropertyChanged` event, but the class never declares `INotifyPropertyChanged`, so WPF's binding never subscribes to it. The trigger reads `true` once at load and never updates. Invalid input gets a tooltip but no red border.

## Details and how to reproduce

Use the control anywhere, for example NumericControls' math editor or "enter a value" dialog on a time series, or the colour picker's RGB boxes. Enter a value outside `MinValue`/`MaxValue`. **Expected:** the red `DataGrid.Cell.Error.Border` border, as `NumericTextBox` shows. **Actual:** no border change; only the tooltip.

## Why this is an issue and not intended behaviour

The XAML defines the border trigger explicitly ([`NumericTextBox2.xaml` lines 23-36](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericTextBox2.xaml#L23-L36)), and the setter raises `PropertyChanged` for `ValueIsValid`. Both show the border is meant to update. The sibling `NumericTextBox` makes the same property a dependency property, and its border works.

## Where in the source

- [`NumericTextBox2.xaml.cs` line 25](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericTextBox2.xaml.cs#L25): `public partial class NumericTextBox2 : UserControl` (no `INotifyPropertyChanged`).
- [Lines 140-159](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericTextBox2.xaml.cs#L140-L159) (the `ValueIsValid` setter raising `PropertyChanged`) and [line 246](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericTextBox2.xaml.cs#L246) (the event).
- The trigger: [`NumericTextBox2.xaml` lines 27-32](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericTextBox2.xaml#L27-L32).

## Suggested fix

```csharp
public partial class NumericTextBox2 : UserControl, INotifyPropertyChanged
```

Or make `ValueIsValid` a read-only dependency property, as in `NumericTextBox`. Either is one line, with no compatibility risk.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps upstream's behaviour in its port of [`NumericTextBox2.axaml.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/General%20Controls/NumericTextBox2.axaml.cs). Its comment on the `PropertyChanged` event notes that, as upstream's, the class doesn't implement the interface.
