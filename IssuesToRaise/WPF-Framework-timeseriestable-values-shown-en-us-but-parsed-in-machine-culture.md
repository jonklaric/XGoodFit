# TimeSeriesTable shows values in en-US but reads edits in the machine's culture, so an edited "1234.6" becomes 12346 on comma-decimal machines

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/NumericControls/Data/Time Series Editor/TimeSeriesTable.xaml` and `DoubleToNAConverter` in `src/GenericControls/Utilities/Converters.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Affects** [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) 2.0.1, whose time-series data editor is this table ([`TimeSeriesControl.xaml` lines 43-47](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.xaml#L43-L47)) and which doesn't set `FrameworkElement.Language`.

**Type:** wrong result · **Severity:** high

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package. The test used a `DataGrid` whose `DataGridTextColumn` is bound exactly like the table's value column (`Mode=TwoWay, UpdateSourceTrigger=LostFocus, Converter=DoubleToNAConverter`), edited through `BeginEdit` and `CommitEdit`, with no `Language` override.

## Summary

The value column formats numbers through its binding, which uses the element's `Language` (en-US unless the host overrides it). `DoubleToNAConverter.ConvertBack` parses the edited text with `CultureInfo.CurrentCulture`. On a machine whose decimal separator is a comma and group separator a dot (de-DE, it-IT, es-ES, nl-NL, pt-BR, …), the cell shows `1234.5`. A user who changes it to `1234.6` stores **12346**, and `0.5` edited to `0.6` stores 6. The date column next to it formats in the machine's culture, so the same row mixes two cultures.

## Details and how to reproduce

```csharp
// WPF app, RMC.Wpf.Framework.Controls 1.0.5, no LanguageProperty override, CurrentCulture = de-DE.
var rows = new ObservableCollection<Holder> { new Holder { Value = 1234.5 } };    // Holder: INotifyPropertyChanged
var column = new DataGridTextColumn { Binding = new Binding("Value")
    { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
      Converter = new GenericControls.DoubleToNAConverter() } };                   // as TimeSeriesTable's ValueColumn
var grid = new DataGrid { AutoGenerateColumns = false, ItemsSource = rows };
grid.Columns.Add(column);
// lay out, then: grid.CurrentCell = new DataGridCellInfo(rows[0], column); grid.BeginEdit();
// the editing TextBox shows "1234.5"; the user changes it to "1234.6"; grid.CommitEdit(DataGridEditingUnit.Row, true);
```

| Culture | Cell shows | User edits to | Stored |
|---|---|---|---|
| en-US | `1234.5` | `1234.6` | 1234.6 |
| de-DE | `1234.5` | `1234.6` | **12346** |
| de-DE | `1234.5` | `1234,6` (German form) | 1234.6 |

A cell committed without changes isn't written back (WPF skips unchanged values), so values only go wrong when edited. But the natural edit is to change the digits that are shown, and then the stored value is multiplied by a power of ten with no error shown.

The table's copy command writes `value.ToString()` in the machine culture (`1234,5`), so what's copied isn't what's shown either.

## Why this is an issue and not intended behaviour

- WPF-Framework's `XamlBindingCultureTests` records the intended design: numeric bindings in shared controls carry `ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}` "so they parse user input under the user's locale even in host apps that don't call `FrameworkElement.LanguageProperty.OverrideMetadata`" ([lines 6-21](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/GenericControls.Tests/XamlBindingCultureTests.cs#L6-L21)). `TimeSeriesTable.xaml` isn't in its list ([lines 29-40](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/GenericControls.Tests/XamlBindingCultureTests.cs#L29-L40)), and its value binding has no `ConverterCulture`.
- `DoubleToNAConverter` ignores the `culture` argument WPF passes. It returns the raw `double` from `Convert`, leaving formatting to the binding's culture, but parses in `ConvertBack` with `CultureInfo.CurrentCulture`. The two directions can't agree unless the host app happens to set `Language` to the current culture, as every WPF-Framework demo does and RMC-BestFit doesn't.
- The date column of the same table, through `DateToStringConverter`, deliberately uses the current culture both ways, so the table's own columns disagree with each other.

## Where in the source

- The value column, with no `ConverterCulture`: [`src/NumericControls/Data/Time Series Editor/TimeSeriesTable.xaml` lines 262-270](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml#L262-L270)
  ```xml
  <DataGridTextColumn x:Name="ValueColumn" ...
      Binding="{Binding Value, UpdateSourceTrigger=LostFocus, Mode=TwoWay, FallbackValue=N/A, TargetNullValue=N/A, Converter={StaticResource NAConverter}}"
  ```
- The converter: [`src/GenericControls/Utilities/Converters.cs` lines 1540-1604](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Utilities/Converters.cs#L1540-L1604)
  ```csharp
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  { ... return val; }                                            // formatted later by WPF in the binding's culture
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
  { ... if (NumberFormatHelper.TryParseDouble(val, out dblVal) == false) ... }   // parsed in CultureInfo.CurrentCulture
  ```
- The date column and its converter (current culture both ways): [`TimeSeriesTable.xaml` lines 254-260](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml#L254-L260), [`TimeSeriesTable.xaml.cs` lines 843-870](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml.cs#L843-L870).

## Suggested fix

1. Add `ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}` to `ValueColumn`'s binding, and add `TimeSeriesTable.xaml` to `XamlBindingCultureTests`. Display and parsing then both use the machine's culture, like the date column.
2. Make `DoubleToNAConverter` honour the `culture` argument in both directions: format with `val.ToString(culture)` in `Convert`, and parse with `NumberFormatHelper.TryParseDouble(val, culture, out …)` in `ConvertBack`. Then the converter is consistent whatever binding culture the host uses.

Either fixes the corruption. Doing both makes the converter safe in RMC-BestFit's other grids, where it's used for read-only result columns today. No file format is involved: the values are stored as doubles.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps both cultures as upstream: numbers in WPF's binding culture (en-US, `WpfCompat.FormattingCulture`) and dates in the machine's culture. See the header comment of [`TimeSeriesTable.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.axaml) and [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L578), where the change is left to the OS-locale decision (D8).
