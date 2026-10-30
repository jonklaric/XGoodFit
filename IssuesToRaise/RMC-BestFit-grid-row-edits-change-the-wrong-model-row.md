# Editing a data row can change an identical earlier row instead, and lose the value typed

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App` (the Input Data grids' row items `ExactDataRowItem`, `IntervalDataRowItem`, `ThresholdDataRowItem` and `UncertainDataRowItem`, and `InputDataControl`'s series handlers). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** data loss · **Severity:** high

**Verification:** reproduced. A small program against upstream's own `RMC.BestFit.dll` (built from this commit) used the real `ExactSeries`, `IntervalSeries` and data classes, and repeated what a row item's setter and `InputDataControl`'s Replace handler do. Not checked in the running WPF application.

## Summary

When a cell in the Input Data grids is edited, the row item finds its ordinate in the model with `_series.IndexOf(_ordinate)`. The ordinates inherit value equality from Numerics' `SeriesOrdinate` (index and value), so `IndexOf` returns the first row that's equal, not the row being edited. If an earlier row has the same index and value, as rows just added with Add Row(s) do, the edit goes to that earlier row. The value typed can then be dropped from the model while the grid still shows it, so the analysis and the saved project use different data from what's on screen.

## Details and how to reproduce

In the Exact Data grid, add three rows and type 5 into the last one, then 7 into the second and 9 into the first. That is, fill the new rows from the bottom up, or in any order other than top to bottom.

- Expected: the grid and the model both hold 9, 7, 5.
- Actual (from the program below): after the first edit the grid shows 5 in the first row as well as the last; at the end the grid shows 9, 7, 5 but the model holds 9, 7, 0. The 5 isn't in the data that's analysed or saved, and reopening the project shows an empty row instead.

```
Exact data, three new rows; user types 5 in row 2, 7 in row 1, 9 in row 0
   edit in grid row 2: IndexOf found model row 0
   after the first edit, grid shows 5, 0, 5
   edit in grid row 1: IndexOf found model row 1
   edit in grid row 0: IndexOf found model row 0
   grid shows  9, 7, 5
   model holds 9, 7, 0
```

Interval data compares only the year and the most likely value, not the bounds. With two rows for the same year and most likely value, changing the second row's lower bound overwrites the first row with a copy of the second, in the grid and in the model. The first row's bounds are lost, and the model's second row keeps its old bound although the grid shows the new one:

```
Interval data (2000; 5, 10, 15) and (2000; 8, 10, 12); user changes row 1's lower bound to 9
   edit in grid row 1: IndexOf found model row 0
   grid shows  2000; 9, 10, 12 | 2000; 9, 10, 12
   model holds 2000; 9, 10, 12 | 2000; 8, 10, 12
```

The program's core (`grid[k]` stands for row item *k*'s `_ordinate`):

```csharp
// ExactDataRowItem setter and ReplaceOrdinate
Data clone = ((dynamic)grid[row]).Clone();
change(clone);
int idx = series.IndexOf(grid[row]);
series[idx] = clone;            // raises Replace at idx
grid[row] = clone;

// InputDataControl.ExactSeries_CollectionChanged on Replace
grid[e.NewStartingIndex + i] = (Data)e.NewItems[i];
```

Editing new rows from the top down doesn't trigger it, because the first equal row is then always the one being edited. Any other order does, as does editing the second of two rows with the same index and value.

## Why this is an issue and not intended behaviour

- An edit should change the row it's made in. Here it changes another row, and the grid then shows data the model doesn't hold, with no warning. The difference is saved with the project.
- The comment on the setters says they replace the ordinate "in the data series to fire a CollectionChanged Replace event for undo support", so the aim is to find this row's slot. `IndexOf` with value equality finds a slot with equal values instead.
- Value-equal rows are normal: every row added with Add Row(s) is a `new ExactData()` (index 0, value 0), and the data can contain repeated year and value pairs.

## Where in the source

- The row item's lookup, the same in all four row items: [`src/RMC.BestFit.App/GUI/Support/DataGrids/ExactDataRowItem.cs` lines 138-148](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/DataGrids/ExactDataRowItem.cs#L138-L148)
  ```csharp
  private void ReplaceOrdinate(ExactData newOrdinate)
  {
      if (_series == null) return;
      int idx = _series.IndexOf(_ordinate);
      if (idx >= 0)
      {
          _series[idx] = newOrdinate;
          _ordinate = newOrdinate;
      }
  }
  ```
  and [`IntervalDataRowItem.cs` line 173](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/DataGrids/IntervalDataRowItem.cs#L173), [`ThresholdDataRowItem.cs` line 152](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/DataGrids/ThresholdDataRowItem.cs#L152), [`UncertainDataRowItem.cs` line 131](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/DataGrids/UncertainDataRowItem.cs#L131). The setters that call it clone the ordinate first, for example [`ExactDataRowItem.cs` lines 60-72](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/DataGrids/ExactDataRowItem.cs#L60-L72).
- `DataSeries.IndexOf` is `List.IndexOf`: [`src/RMC.BestFit/Models/DataFrame/DataCollections/DataSeries.cs` lines 147-150](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataCollections/DataSeries.cs#L147-L150). Its indexer raises Replace at the slot it wrote: [lines 30-38](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataCollections/DataSeries.cs#L30-L38).
- `Data` derives from `SeriesOrdinate<int, double>` ([`Data.cs` line 15](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/DataFrame/DataTypes/Data.cs#L15)), whose equality compares only the index and value: [Numerics `Numerics/Data/Time Series/Support/SeriesOrdinate.cs` lines 82-88](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Data/Time%20Series/Support/SeriesOrdinate.cs#L82-L88).
- The view moves the change onto the row item at the slot written, which is how the grid comes to show the edit in the earlier row: [`src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs` line 547](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L547) (and lines 593, 636 and 679 for the other three series).
- Add Row(s) inserts equal ordinates: [lines 2427-2434](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L2427-L2434).

## Suggested fix

Find the row by reference, not by value. Either:

1. Search by reference in the row item:
   ```csharp
   int idx = -1;
   for (int i = 0; i < _series.Count; i++)
       if (ReferenceEquals(_series[i], _ordinate)) { idx = i; break; }
   ```
   or give `DataSeries` an `IndexOfReference` method that does this; or
2. Have the grid pass the row's position (the row item's index in `ExactDataOrdinates`), since the grid and the series are kept in the same order.

Changing `SeriesOrdinate.Equals` to reference equality would also fix it, but other Numerics code may rely on value equality, so the local fix is safer. A regression test: two equal rows, edit the second, check that the model's first row is unchanged.

## How XGoodFit handles it

XGoodFit's port of the Input Data view (to Avalonia) compiles upstream's four row items unchanged ([`src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj` lines 115-120](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj#L115-L120)), so it has the same bug for now.
