# Grid column header styles are rebuilt on top of themselves, so the BasedOn chain grows with every update

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App` (`InputDataControl`, `FittingAnalysisControl`, `ParameterPriorsControl`, `QuantilePriorsControl`, `UnivariateAnalysisPropertiesControl`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug (resource growth) · **Severity:** low

**Verification:** verified by reading the source; not measured.

## Summary

Five grid-update methods give a column a tool tip by creating a new header style based on the column's current header style, then assigning it back to the column. The methods run every time their data changes, so each run adds one more level to the style's `BasedOn` chain, with its own tool-tip `TextBlock`. Over a session the chain grows without limit, and every level is kept in memory and walked when the style is applied.

## Details and how to reproduce

In the Input Data view, every change that refreshes the summary statistics runs `UpdateSummaryStats`, which does:

```csharp
var headerStyle = new Style(typeof(DataGridColumnHeader), ExactDataColumn.HeaderStyle);
headerStyle.Setters.Add(new Setter(ToolTipProperty, new TextBlock() { Text = "The summary statistics using only the exact data sample.", ... }));
ExactDataColumn.HeaderStyle = headerStyle;
```

After *n* refreshes, `ExactDataColumn.HeaderStyle` is *n* styles deep, each with an identical setter. Walking `HeaderStyle.BasedOn` in a debugger after a few edits shows it.

## Why this is an issue and not intended behaviour

The intent is one style: the column's original header style plus a tool tip. Basing the new style on the *current* style is only right the first time; after that it stacks copies. Nothing ever trims the chain, and each level also keeps its `TextBlock` alive.

## Where in the source

The same expression, `new Style(typeof(DataGridColumnHeader), <column>.HeaderStyle)` assigned back to the column, occurs 11 times, in five methods that are each called from two or three places:

- `InputDataControl.UpdateSummaryStats`: [`src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs` lines 3548 and 3552](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L3546-L3554)
- `FittingAnalysisControl.UpdateAICBICDataGrid`: [`src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs` lines 1501, 1506 and 1511](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/FittingAnalysis/FittingAnalysisControl.xaml.cs#L1499-L1513)
- `ParameterPriorsControl.UpdateDataGrid`: [`src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml.cs` lines 195 and 200](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml.cs#L195-L202)
- `QuantilePriorsControl.UpdateDataGrid`: [`src/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.xaml.cs` lines 199 and 204](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.xaml.cs#L199-L206)
- `UnivariateAnalysisPropertiesControl.BindTrendModelDataGrid`: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisPropertiesControl.xaml.cs` lines 694 and 699](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Univariate/UnivariateAnalysisPropertiesControl.xaml.cs#L694-L701)

## Suggested fix

Any of these:

1. Set the tool tips once, in XAML or in the constructor, instead of in the update methods. They are constant strings.
2. Base the new style on the original style, kept in a field (or looked up by its resource key), not on `column.HeaderStyle`.
3. Put the tool tips in the column definitions with a small header template, so no code-behind style is needed.

No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's pattern for now, with Avalonia's `ControlTheme` in place of `Style`: for example [`src/BestFit/RMC.BestFit.App/GUI/InputData/InputDataControl.axaml.cs` lines 3597 and 3601](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/InputData/InputDataControl.axaml.cs#L3597-L3601), and `HeaderThemeOf`, which returns the column's current theme when it has one ([`ParameterPriorsControl.axaml.cs` lines 250-255](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.axaml.cs#L250-L255)).
