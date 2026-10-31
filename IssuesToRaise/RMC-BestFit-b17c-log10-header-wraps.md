# Bulletin 17C's "Log₁₀" column header wraps onto two lines

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml` (the quantile grid's `UseLog10Column`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** reproduced (WPF text layout). A throwaway WPF program on Windows 11, .NET 10.0.14, measured the header's text as the theme draws it: a bold Segoe UI 12 `TextBlock` with `TextWrapping="Wrap"`, in the width the header leaves. It wasn't checked in the running application.

## Summary

The **Quantile Information** grid's "Log₁₀" column is fixed at 44 px, and its header uses `Center_ColumnHeaderStyle`, whose text wraps. The header's template leaves 30 px for the text, and "Log₁₀" is 30.48 px wide (30.00 px with Display text formatting). WPF wraps it in both modes, so the header shows "Log₁" over "₀" and the header row is twice as tall as the other grids'.

## Details and how to reproduce

Open any Bulletin 17C analysis and look at **Properties > General > Quantile Information**.

The program's output:

```
Ideal: text width 30.48 px; in 30 px: wraps (height 31.92 vs 15.96); in 32 px: one line (height 15.96 vs 15.96);
Display: text width 30.00 px; in 30 px: wraps (height 32.00 vs 16.00); in 32 px: one line (height 16.00 vs 16.00);
```

The 30 px: the column's 44 px, less the header border's right edge (1 px), the header grid's left margin (1 px) and the content presenter's margins (6 px each side). The sort arrow's column is collapsed.

## Why this is an issue and not intended behaviour

A label split inside "Log₁₀", leaving its last subscript digit on a line of its own, isn't a deliberate layout. The grids' other headed columns are star-sized and grow with the panel; this fixed one needs only 2 px more. It's cosmetic only.

## Where in the source

- The column: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml` line 274](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml#L274):
  ```xml
  <DataGridTemplateColumn x:Name="UseLog10Column" IsReadOnly="False" Header="Log₁₀" Width="44" MinWidth="44" MaxWidth="44"
          HeaderStyle="{DynamicResource Center_ColumnHeaderStyle}">
  ```
- The header style, which wraps: [WPF-Framework `src/Themes/Resources/Controls/DataGrid.xaml` lines 185-195](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/DataGrid.xaml#L185-L195), based on `ColumnHeaderStyle`, whose template sets the border (line 105), the grid's margin (line 117) and the content presenter's margin (line 130): [lines 100-131](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/DataGrid.xaml#L100-L131).

## Suggested fix

Widen the column to 46 px (`Width`, `MinWidth` and `MaxWidth`), which leaves 32 px, where the text fits on one line in both modes. Or give this column a header style that doesn't wrap. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps the 44 px column and the wrapping header theme: [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.axaml` line 277](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.axaml#L277) and [`src/Framework/Themes/Resources/Controls/DataGrid.axaml` lines 272-280](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/Framework/Themes/Resources/Controls/DataGrid.axaml#L272-L280).
