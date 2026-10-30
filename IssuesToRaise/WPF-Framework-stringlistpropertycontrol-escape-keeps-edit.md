# StringListPropertyControl keeps an edit that the user cancelled with Escape

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/Properties Controls/StringListPropertyControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package. With `StringList = ["Jan", "Feb"]`, a cell edit to "Changed" followed by `CancelEdit` (what Escape does) left `StringList` as `["Changed", "Feb"]`.

## Summary

`CellEditEnding` rebuilds `StringList` from the cells' current content whether the edit was committed or cancelled. During a cancelled edit the cell still holds the editing `TextBox` with the typed text, so the cancelled text becomes the new value.

## Details and how to reproduce

In any `StringListPropertyControl` (for example OxyPlotControls' axis editor, for category labels), start editing a row, type something, and press Escape. **Expected:** the row returns to its previous value. **Actual:** the typed text is kept in the grid and written back to the bound `StringList`.

## Why this is an issue and not intended behaviour

Escape means cancel in WPF's `DataGrid`, and `DataGridCellEditEndingEventArgs.EditAction` tells a handler which one happened. The handler ignores it.

## Where in the source

[`StringListPropertyControl.xaml.cs` lines 185-188](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Properties%20Controls/StringListPropertyControl.xaml.cs#L185-L188)

```csharp
private void StringListDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
{
    StringList = GetStringList();   // reads the editing TextBox's text even on Cancel
}
```

`GetStringList` reads each cell's content, including the editing `TextBox` ([lines 154-180](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Properties%20Controls/StringListPropertyControl.xaml.cs#L154-L180)).

## Suggested fix

```csharp
private void StringListDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
{
    if (e.EditAction == DataGridEditAction.Cancel) return;
    StringList = GetStringList();
}
```

Building the list from the `StringContainer` items after the commit, instead of from the cells' visuals, would be more robust still.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the behaviour, see [`StringListPropertyControl.axaml.cs` lines 211-212](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/Properties%20Controls/StringListPropertyControl.axaml.cs#L211-L212), and a test pins it ([`StringListPropertyControlTests.cs` line 49](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/Framework/GenericControls.Tests/StringListPropertyControlTests.cs#L49)).
