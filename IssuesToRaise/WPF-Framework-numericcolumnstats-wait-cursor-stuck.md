# Numeric column statistics leave the application's wait cursor on when a classification gives no breaks

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/DatabaseControls/Column Stats/NumericColumnStats.xaml.cs` (`Plot`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source.

## Summary

`Plot()` sets `Mouse.OverrideCursor = Cursors.Wait`, which applies to the whole application, and resets it only at the end of the method. If the classification returns no breaks, it returns early and the wait cursor stays on everywhere until another plot completes. An exception from a classification method leaves it on too, as there is no `try`/`finally`.

## Details and how to reproduce

Open a table, choose Statistics on a numeric column, pick "Custom Interval" and enter an interval size of 0. `Classification.DefinedInterval` returns an empty array for `intervalSize <= 0`. **Expected:** an empty histogram and a normal cursor. **Actual:** the histogram is cleared and the busy cursor stays over every window of the application.

## Why this is an issue and not intended behaviour

The method's last line resets the cursor, which shows the intent. The early return just skips it.

## Where in the source

[`NumericColumnStats.xaml.cs` lines 656-801](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/Column%20Stats/NumericColumnStats.xaml.cs#L656-L801)

```csharp
Mouse.OverrideCursor = Cursors.Wait;          // line 660
...
if (breaks.Length == 0)                       // line 731
{
    HistogramData.Clear();
    return;                                   // cursor never reset
}
...
Mouse.OverrideCursor = null;                  // line 800
```

The empty result: [`Classification.cs` line 135](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/DatabaseControls/Column%20Stats/Classification.cs#L135).

## Suggested fix

```csharp
Mouse.OverrideCursor = Cursors.Wait;
try
{
    ... // existing body, including the early return
}
finally
{
    Mouse.OverrideCursor = null;
}
```

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the early return in its port of `NumericColumnStats.axaml.cs`. Listed among the upstream quirks kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L580).
