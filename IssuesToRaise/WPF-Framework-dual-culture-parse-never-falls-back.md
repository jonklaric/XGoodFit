# The "dual-culture" number parsing never falls back for US-formatted numbers on de-DE machines: pasting "1.5" gives 15

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/DataGrid/CopyPasteDataGrid.cs` (paste) and `src/DatabaseManager/DataTableView.cs` (`TryParseDoubleDualCulture`, used by `ConvertToColumnType`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** wrong result · **Severity:** medium

**Verification:** reproduced. The exact parsing expression was run under de-DE, fr-FR and en-US. It wasn't run through the clipboard, to avoid overwriting the user's clipboard.

## Summary

Pasted and imported numbers are parsed with the current culture first and the invariant culture only if that fails. Both attempts use `NumberStyles.Any`, which allows group separators anywhere, so on machines where `.` is the group separator (de-DE, it-IT, es-ES, nl-NL, pt-BR, id-ID, …) the first attempt never fails for US-formatted text. `"1.5"` becomes 15, `"0.25"` becomes 25 and `"1234.5"` becomes 12345, and the invariant fallback never runs.

## Details and how to reproduce

```csharp
static bool DualCulture(string text, out double result) =>             // as CopyPasteDataGrid and DataTableView parse
    double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
    || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
```

| Culture | `"1.5"` | `"1234.5"` | `"0.25"` | `"1,5"` |
|---|---|---|---|---|
| de-DE | **15** | **12345** | **25** | 1.5 |
| fr-FR | 1.5 | 1234.5 | 0.25 | 1.5 |
| en-US | 1.5 | 1234.5 | 0.25 | **15** |

The same happens when a German user pastes a column copied from a US-formatted CSV, a web page or an e-mail into any `CopyPasteDataGrid` (RMC-BestFit's data tables, the time-series editor, …), or edits through `DataTableView`.

## Why this is an issue and not intended behaviour

- The code says what it means to do. `CopyPasteDataGrid`'s comment: "parse as double with dual-culture fallback first … Convert.ChangeType alone uses Thread.CurrentCulture and silently misparses cross-culture clipboard data ("1,234" US vs DE)". `DataTableView`'s helper is documented as parsing "using the current culture, then invariant culture".
- WPF-Framework's [`tests/CULTURE_SMOKE_TESTS.md`, Test 6](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/CULTURE_SMOKE_TESTS.md#L79-L87) asks the tester to copy a row containing `1.5` from a US source under German settings, paste it, and "Verify: parses correctly via the dual-culture fallback in `CopyPasteDataGrid` and `DataTableView.ConvertToColumnType`". With this code it gives 15.
- `.NET`'s parser accepts misplaced group separators (`"1.5"` isn't a valid German grouped number), so "the current culture succeeded" doesn't mean the text was in the current culture.

## Where in the source

- [`src/GenericControls/DataGrid/CopyPasteDataGrid.cs` lines 840-845](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/DataGrid/CopyPasteDataGrid.cs#L840-L845) (doubles), [lines 852-860](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/DataGrid/CopyPasteDataGrid.cs#L852-L860) (other numeric types), and the same pair again from [line 959](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/DataGrid/CopyPasteDataGrid.cs#L959)
  ```csharp
  else if (double.TryParse(clipboardData[i][j], NumberStyles.Any, CultureInfo.CurrentCulture, out double parsed) ||
           double.TryParse(clipboardData[i][j], NumberStyles.Any, CultureInfo.InvariantCulture, out parsed))
  ```
- [`src/DatabaseManager/DataTableView.cs` lines 2765-2769](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseManager/DataTableView.cs#L2765-L2769), used by `ConvertToColumnType` ([line 2789](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseManager/DataTableView.cs#L2789)).

## Suggested fix

Try the strict forms first, and allow group separators only as a last resort:

```csharp
static bool TryParseDoubleDualCulture(string text, out double result)
{
    const NumberStyles Strict = NumberStyles.Float;                    // no group separators
    return double.TryParse(text, Strict, CultureInfo.CurrentCulture, out result)
        || double.TryParse(text, Strict, CultureInfo.InvariantCulture, out result)
        || double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
        || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
}
```

With this, on de-DE `"1,5"` gives 1.5, `"1.5"` gives 1.5 (invariant, strict) and `"1.234,56"` gives 1234.56 (current culture, grouped). On en-US `"1,5"` still gives 15, which is genuinely ambiguous there. Optionally reject results where a group separator isn't followed by exactly three digits. Put the helper in one place (for example `NumberFormatHelper`) and use it from both classes and their tests.

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps upstream's parsing in its port of [`CopyPasteDataGrid.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/DataGrid/CopyPasteDataGrid.cs) and compiles `DatabaseManager` unchanged, so it behaves the same.
