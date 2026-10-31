# Bulletin 17C's penalty grids never highlight an invalid mean or MSE

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml` and `src/RMC.BestFit/Models/Support/ParameterPenalty.cs`, `QuantilePenalty.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run. XGoodFit's port of the panel found it: its bindings fail the same way.

## Summary

The Bulletin 17C properties panel has two penalty grids, **Parameter Information** and **Quantile Information**. Their Mean and MSE cells have a trigger that turns the cell red, with the error message as its tool tip, when the row is enabled and the value is invalid. The triggers bind `IsMeanValid`, `IsMSEValid`, `MeanErrorMessage` and `MSEErrorMessage`, but the grids' rows are `ParameterPenalty` and `QuantilePenalty` objects, which have none of these properties. The bindings fail silently, so an invalid mean or MSE is never highlighted.

## Details and how to reproduce

1. Open a Bulletin 17C analysis, for example "Example #1" in `examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.bestfit`.
2. In **Parameter Information**, enable a row and type 0 (or -1) in its **MSE** cell.
3. Expected: the cell turns red, with "MSE must be greater than zero" as its tool tip. Actual, by reading: the cell looks normal. A binding error for `IsMSEValid` is logged.

The value is still caught elsewhere: `Bulletin17CDistribution.Validate()` adds each penalty's `Validate()` message to the element's messages. So the Message Window reports it, but the grid doesn't show which cell is wrong.

## Why this is an issue and not intended behaviour

The four triggers exist only to show these errors, with the same colours and border the application uses for invalid cells elsewhere. The penalties already have the validation they need: `IsValid` and `Validate()`, which returns a message for each kind of error. Only the per-cell properties the XAML expects were never added.

## Where in the source

- The triggers: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml` lines 174-183](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml#L174-L183) (the parameter Mean), [lines 201-210](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml#L201-L210) (the parameter MSE), [lines 306-315](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml#L306-L315) and [lines 333-342](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml#L333-L342) (the quantile Mean and MSE):
  ```xml
  <MultiDataTrigger>
      <MultiDataTrigger.Conditions>
          <Condition Binding="{Binding Enabled}" Value="True"/>
          <Condition Binding="{Binding IsMSEValid}" Value="False"/>
      </MultiDataTrigger.Conditions>
      ...
      <Setter Property="ToolTip"         Value="{Binding MSEErrorMessage}"/>
  ```
- The grids' rows: [`B17CAnalysisPropertiesControl.xaml.cs` lines 98-99](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.xaml.cs#L98-L99) (`ParametersTable.ItemsSource = newElement.ParameterPenalties`, and the same for `QuantilePenalties`).
- `ParameterPenalty`'s validation, with no per-field properties: [`src/RMC.BestFit/Models/Support/ParameterPenalty.cs` lines 235-238](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/Support/ParameterPenalty.cs#L235-L238) (`IsValid`) and [lines 307-324](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/Support/ParameterPenalty.cs#L307-L324) (`Validate()`); `QuantilePenalty`'s: [`QuantilePenalty.cs` lines 267-270 and 319-336](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/Support/QuantilePenalty.cs#L267-L336). No file in the repository defines `IsMeanValid`, `IsMSEValid`, `MeanErrorMessage` or `MSEErrorMessage`.
- Where the messages do go: [`src/RMC.BestFit/Models/UnivariateDistribution/Bulletin17CDistribution.cs` lines 2982-3001](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/Bulletin17CDistribution.cs#L2982-L3001).

## Suggested fix

Add the four properties to `ParameterPenalty` and `QuantilePenalty`, and raise them when `Mean`, `MSE`, `UseLog` (or, for quantiles, `AEP`) change. For example, in `ParameterPenalty`:

```csharp
public bool IsMeanValid => Tools.IsFinite(Mean) && (!UseLog || Mean > 0);
public bool IsMSEValid => Tools.IsFinite(MSE) && MSE > 0;
public string MeanErrorMessage => IsMeanValid ? string.Empty : (UseLog ? "The mean must be positive when Log is used." : "The mean must be a number.");
public string MSEErrorMessage => IsMSEValid ? string.Empty : "The MSE must be greater than zero.";
```

Or bind the triggers to a value converter over `Validate()`. The properties are computed and not serialised, so files are unaffected.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps the triggers as upstream, so they never apply either; its header comment says so: [`src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.axaml` lines 11-14 and 189-195](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisPropertiesControl.axaml#L11-L14). The penalties are upstream's model classes, compiled unchanged.
