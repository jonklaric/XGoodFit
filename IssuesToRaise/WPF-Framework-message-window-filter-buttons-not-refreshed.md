# Message window: toggling a filter doesn't update its button's text or tooltip

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Message Window/Views/MessageWindowControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; also seen in XGoodFit's port, whose test had to allow for it.

## Summary

`UpdateButtonText()` writes "N Errors" or "0 of N Errors", and the "Hide Errors"/"Show Errors" tooltip, according to whether each filter is on. The four filter buttons' click handlers change the filter but don't call it; it runs only when messages are added or removed. After clicking "Errors" to hide them, the button still says "3 Errors" and "Hide Errors" until the next message arrives.

## Details and how to reproduce

With errors in the message window, click the Errors button. **Expected:** the button reads "0 of 3 Errors" with the tooltip "Show Errors", as `UpdateButtonText` would set. **Actual:** the rows are hidden, but the text and tooltip are unchanged. The same applies to Warnings, Messages and Events, and to "clear all filters".

## Why this is an issue and not intended behaviour

The "0 of N" wording and the swapped tooltips exist only to reflect the filter state, and the handlers already change the buttons' styles for it. Leaving out the text update looks like an omission.

## Where in the source

- [`MessageWindowControl.xaml.cs` lines 299-341](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Message%20Window/Views/MessageWindowControl.xaml.cs#L299-L341) (`UpdateButtonText`). It's called only from lines 34, 67, 208 and 293 (construction, clearing, message removed, message added).
- The filter handlers, which don't call it: [`ErrorsButton_Click` lines 380-403](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Message%20Window/Views/MessageWindowControl.xaml.cs#L380-L403), and likewise `WarningsButton_Click`, `MessagesButton_Click`, `EventsButton_Click` and `ClearFiltersButton_Click`.

## Suggested fix

Call `UpdateButtonText()` at the end of each filter handler, next to `UpdateClearAllFilterButtonStyle()`. No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps this in its FrameworkUI shell port (branch `t3/port-frameworkui-shell`, not yet on `main`). Its message-window test was adjusted to expect the stale text.
