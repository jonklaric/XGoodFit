# A composite analysis's invalid-weight highlight can never show

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml.cs` (`SetDataGridStyle`) and `src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateSupport/WeightedUnivariateAnalysis.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

The Composite properties panel gives its **Weight** column a cell style with a trigger that turns a cell red, with the tool tip "The weight must be between 0 and 1", when `IsWeightValid` is false. The grid's rows are `WeightedUnivariateAnalysis` objects, which have no `IsWeightValid` property, so the binding fails and the highlight never shows. The style is also installed only after the element raises `Analyses`, not when the panel gets its element.

## Details and how to reproduce

1. Open "Mixture of Flood Types" in `examples/4-univariate-distribution-analysis/5-composite-analysis/mixed-population-examples.bestfit` (or any composite with **Composite Type = Mixture**, where the Weight column is shown and editable).
2. Type 1.5 in a row's **Weight**.
3. Expected: the cell turns red with the tool tip. Actual, by reading: it doesn't. A binding error for `IsWeightValid` is logged.

The model still rejects it: `CompositeAnalysis`'s validation adds "Error: Weights for each univariate analysis must be between 0 and 1.", which the Message Window shows.

## Why this is an issue and not intended behaviour

The trigger is written for exactly this message, and nothing else reads `IsWeightValid`. The row class has `Weight` and `UnivariateAnalysis` only, so the property the trigger expects was never added.

## Where in the source

- The style: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml.cs` lines 743-754](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml.cs#L743-L754):
  ```csharp
  DataTrigger dt1 = new DataTrigger();
  dt1.Binding = new Binding("IsWeightValid");
  dt1.Value = false;
  ...
  dt1.Setters.Add(new Setter(DataGridCell.ToolTipProperty, new ToolTip() { Content = "The weight must be between 0 and 1" }));
  ```
  called only from `Element_PropertyChanged` for `Analyses`, [line 405](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml.cs#L405). Until then the column has the XAML's plain `Right_CellStyle` ([`CompositeAnalysisPropertiesControl.xaml` lines 203-210](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.xaml#L203-L210)).
- The row class, without `IsWeightValid`: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateSupport/WeightedUnivariateAnalysis.cs` lines 44-81](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateSupport/WeightedUnivariateAnalysis.cs#L44-L81).
- The model's message: [`src/RMC.BestFit/Analyses/Univariate/CompositeAnalysis.cs` line 1164](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Univariate/CompositeAnalysis.cs#L1164).

## Suggested fix

1. Add the property to `WeightedUnivariateAnalysis`, and raise it with `Weight`:
   ```csharp
   public bool IsWeightValid => Weight >= 0 && Weight <= 1;
   // in the Weight setter, after RaisePropertyChange(nameof(Weight)):
   RaisePropertyChange(nameof(IsWeightValid));
   ```
2. Call `SetDataGridStyle()` when the element is set (in `ElementCallback`), or declare the trigger in XAML, so the style is there from the start.

The property is computed and not serialised (`ToXElement` writes `Weight` and the analysis's name), so files are unaffected.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's style, so the highlight doesn't show there either: [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.axaml.cs` lines 816-830](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.axaml.cs#L816-L830), installed at [line 476](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/Composite/CompositeAnalysisPropertiesControl.axaml.cs#L476). `WeightedUnivariateAnalysis` is upstream's file, compiled unchanged.
