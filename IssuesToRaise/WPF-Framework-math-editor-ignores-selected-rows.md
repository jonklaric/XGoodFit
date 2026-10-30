# The time-series math functions do nothing when value cells are selected ("Applies to rows 0, 0.")

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/NumericControls/Data/Time Series Editor/MathEditorControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Affects** RMC-BestFit 2.0.1's time-series data editor.

**Type:** bug · **Severity:** medium

**Verification:** verified by reading the source; also seen while porting the control to Avalonia (the port keeps this logic).

## Summary

The table's rows are now `TimeSeriesRowItem` wrappers, but `MathEditorControl.Source` still finds each selected row with `_series.IndexOf(cellInfo.Item)`, looking a wrapper up in the `TimeSeries`. Every selected row maps to -1. The popup says "Applies to rows 0, 0." (or "0 - 0"), and the chosen function changes nothing, because `TimeSeries.Add(constant, indexes)` and its siblings skip negative indexes.

## Details and how to reproduce

In RMC-BestFit, open a time series, select some cells in the value column (not all rows), click the math button, choose "Add", enter 5 and apply. **Expected:** the popup lists the selected rows and 5 is added to those values. **Actual:** "Applies to rows 0, 0." and no value changes. With no cells, or every row, selected, the function applies to the whole series as intended.

## Why this is an issue and not intended behaviour

- The popup's own text ("Applies to rows …") and the consecutive/non-consecutive logic show it is meant to work on a selection.
- Row 0 doesn't exist in the one-based numbering the message uses. -1 + 1 = 0 is the signature of a failed `IndexOf`.
- The table builds `TimeSeriesRowItem` wrappers ([`TimeSeriesTable.xaml.cs` line 95](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml.cs#L95)) and passes its grid to the editor ([lines 612-629](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/TimeSeriesTable.xaml.cs#L612-L629)), so `cellInfo.Item` is never a `TimeSeries` ordinate.

## Where in the source

- [`MathEditorControl.xaml.cs` lines 70-120](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Data/Time%20Series%20Editor/MathEditorControl.xaml.cs#L70-L120)
  ```csharp
  foreach (DataGridCellInfo cellInfo in _selectedCells)
  {
      if (cellInfo.Column.DisplayIndex <= 1) { continue; }
      _selectedValueRowIndices.Add(_series.IndexOf(cellInfo.Item));   // Item is a TimeSeriesRowItem: always -1
  }
  ```
- Numerics skips the -1 indexes: [`Numerics/Data/Time Series/TimeSeries.cs` lines 224-233](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Data/Time%20Series/TimeSeries.cs#L224-L233)
  ```csharp
  if (indexes[i] >= 0 && indexes[i] < Count && (!double.IsNaN(this[indexes[i]].Value))) { this[indexes[i]].Value += constant; }
  ```

## Suggested fix

Map the selected cell's item to its row. The table builds one `TimeSeriesRowItem` per ordinate in series order, so the grid's item index is the series index:

```csharp
int row = _source.Items.IndexOf(cellInfo.Item);
if (row >= 0) _selectedValueRowIndices.Add(row);
```

Alternatively, expose the index `TimeSeriesRowItem` is constructed with (it's in the private `_index` field) as an internal property. Either way, drop -1 results rather than passing them on. Nothing is saved differently, so no file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the lookup, see the `Source` comment in [`MathEditorControl.axaml.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/NumericControls/Data/Time%20Series%20Editor/MathEditorControl.axaml.cs#L33), and lists it among the upstream bugs kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L576).
