# TableViewer's Escape handler is unreachable: the "non-character key" filter returns first

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/DatabaseControls/TableViewer.xaml.cs` (`Grid_KeyDown`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

`Grid_KeyDown` has an `Escape` branch that clears the clipboard (cancelling a copy, as Excel does), but it comes after a filter that marks every key outside the letter, digit, numpad, OEM and space ranges as handled and returns. `Key.Escape` is outside all of them, so the branch never runs and Escape does nothing.

## Details and how to reproduce

In a `TableViewer`, select cells, press Ctrl+C, then Escape. **Expected (per the code):** the copy is cancelled; the clipboard is cleared and Paste becomes unavailable. **Actual:** nothing happens, and the copied cells can still be pasted.

## Why this is an issue and not intended behaviour

The branch exists and has no other way to run. Its position after a filter that excludes Escape looks like an ordering mistake.

## Where in the source

[`TableViewer.xaml.cs` lines 2327-2340](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/TableViewer.xaml.cs#L2327-L2340)

```csharp
// Filter out non-character keys
if ((e.Key < Key.NumPad0 || e.Key > Key.Divide) && (e.Key < Key.D0 || e.Key > Key.D9) &&
    (e.Key < Key.A || e.Key > Key.Z) && (e.Key < Key.Oem1 || e.Key > Key.Oem3) &&
    (e.Key < Key.OemOpenBrackets || e.Key > Key.OemQuotes) && e.Key != Key.Space)
{
    e.Handled = true;
    return;
}

if (e.Key == Key.Escape)        // never reached
{
    Clipboard.Clear();
    return;
}
```

## Suggested fix

Move the `Escape` check above the filter, as was done for Tab and Enter. Or delete the branch, if clearing the user's system-wide clipboard on Escape isn't wanted; that is arguably surprising, since it also clears what other applications copied. Either way the dead branch should go. No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the order in its port of [`TableViewer.axaml.cs`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/DatabaseControls/TableViewer.axaml.cs), listed among the upstream quirks kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L580).
