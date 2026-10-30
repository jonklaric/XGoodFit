# After undoing a bivariate model change, edits in the Output tab's XY editor are lost

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs` (`RestoreModelFromSnapshot`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** data loss · **Severity:** medium

**Verification:** verified by reading the source; not run.

## Summary

Undoing or redoing a change to a bivariate analysis's model (its copula, estimation method, parameters or priors) rebuilds the inner analysis from XML, which gives it a new `XYOrdinates` list. `RestoreModelFromSnapshot` raises property changes for the distribution, the Bayesian analysis, `IsEstimated` and the results, but not for `XYOrdinates`. The properties panel's Output tab stays bound to the old list, so edits made there afterwards change a list the analysis no longer uses: they don't affect the results and aren't saved.

## Details and how to reproduce

1. Open a bivariate analysis and its properties panel.
2. Change the copula type, then undo.
3. On the Output tab, edit the X-Y ordinates (add a row or change a value), then save.
4. Expected: the edit is in the analysis and the saved project. Actual, by reading: the editor shows it, but the inner analysis's ordinates are unchanged, so it's lost on save. Reopening the panel shows the model's list without the edit.

The old list also keeps the element's `XYOrdinates_CollectionChanged` subscription, which `RestoreModelFromSnapshot` doesn't remove. So each edit still runs the element's handler, but the handler validates and reprocesses the inner analysis's new, unedited list, so nothing signals that the edit went nowhere.

## Why this is an issue and not intended behaviour

`XYOrdinates` is a pass-through to `_innerAnalysis.XYOrdinates`, so replacing `_innerAnalysis` changes its value, and bindings need to be told. The method already notifies the other pass-through properties it replaces, so `XYOrdinates` looks like an oversight.

## Where in the source

- `RestoreModelFromSnapshot`, [`src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs` lines 1579-1600](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L1579-L1600):
  ```csharp
  if (_innerAnalysis != null)
      _innerAnalysis.PropertyChanged -= InnerAnalysis_PropertyChanged;
  ...
  var analysisXml = _innerAnalysis?.ToXElement();
  var newModel = new BivariateDistribution(margX, margY, snapshot);
  _innerAnalysis = new ModelAnalyses.BivariateAnalysis(newModel, analysisXml);
  ...
  SetupBridges();
  RaisePropertyChange(nameof(BivariateDistribution));
  RaisePropertyChange(nameof(BayesianAnalysis));
  RaisePropertyChange(nameof(IsEstimated));
  RaisePropertyChange(nameof(AnalysisResults));
  ```
- The rebuilt analysis reads a new list from the XML: [`src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs` lines 55-58](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Bivariate/BivariateAnalysis.cs#L55-L58).
- The pass-through property: [UI `BivariateAnalysis.cs` lines 504-506](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L504-L506). `SetupBridges` subscribes to the new list ([lines 1708-1709](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L1708-L1709)).
- What records these undo steps: [lines 369-373](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L369-L373) (`"Parameters", "CopulaType", "CopulaEstimationMethod", "UseJeffreysRuleForScale", "EnableQuantilePriors", "QuantilePriors", "UseDefaultFlatPriors"`) and `RecordModelUndo` ([lines 1553-1570](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L1553-L1570)).
- The Output tab's editor: [`src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml` line 214](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml#L214): `UncertainOrderedData="{Binding Path=Element.XYOrdinates}"`.

## Suggested fix

In `RestoreModelFromSnapshot`, unsubscribe the old list before replacing the inner analysis, and raise the change afterwards:

```csharp
if (_innerAnalysis?.XYOrdinates != null)
    _innerAnalysis.XYOrdinates.CollectionChanged -= XYOrdinates_CollectionChanged;
...
RaisePropertyChange(nameof(XYOrdinates));
```

Better still, keep the same `XYOrdinates` object across the rebuild (assign the old list to the new inner analysis), since the snapshot only describes the model, not the ordinates. A regression test: record a copula change, undo, add an ordinate through `Element.XYOrdinates` as bound before the undo, and check that the inner analysis has it. No file-format risk.

## How XGoodFit handles it

XGoodFit's port compiles the same element code, so it has the same gap for now ([`src/BestFit/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs` line 1584](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.UI/Elements/BivariateAnalysis/BivariateAnalysis.cs#L1584)).
