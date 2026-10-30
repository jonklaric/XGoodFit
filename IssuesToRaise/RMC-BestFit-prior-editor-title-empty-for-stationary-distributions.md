# The prior distribution editor's title is "Select Distribution for :" for stationary distributions

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml` (with `UnivariateDistribution` in `src/RMC.BestFit`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

In the prior distributions grid, each parameter's distribution editor is titled "Select Distribution for *X*:", where *X* is bound to the parameter's `Name`. For a stationary distribution, the model sets every parameter's `Name` to an empty string and the name is carried by `OwnerName` instead. So the editor reads "Select Distribution for :", while the grid's own Parameter column, bound to `DisplayName`, shows the name correctly.

## Details and how to reproduce

1. Open a univariate analysis with a stationary distribution (for example, the normal) and Bayesian estimation.
2. In the prior distributions grid, open the distribution editor for a parameter.
3. Expected: the parameter's name after "for", as in the Parameter column. Actual: "Select Distribution for :".

## Why this is an issue and not intended behaviour

The editor is opened from a row whose Parameter column shows the name, and the title's layout ("for … :") expects a name. Non-stationary distributions keep their parameter names, so the title only goes empty in the stationary case, which is the common one.

## Where in the source

- The title binding: [`src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml` line 62](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml#L62): `DistributionTitle="{Binding Path=Name}"`. The Parameter column in the same file uses `DisplayName` ([line 70](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml#L70)).
- Names cleared for stationary distributions: [`src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs` lines 800-805](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs#L800-L805)
  ```csharp
  // If not nonstationary, remove name string
  if (!IsNonstationary)
  {
      for (int i = 0; i < Distribution.NumberOfParameters; i++)
          _parameters[i].Name = "";
  }
  ```
- `DisplayName` falls back to `OwnerName`: [`src/RMC.BestFit/Models/Support/ModelParameter.cs` line 134](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/Support/ModelParameter.cs#L134).
- The title text in WPF-Framework: [`NumericControls/Distributions/Univariate/Distribution Selector/DistributionSelectorControl.xaml` lines 107-110](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml#L107-L110).

## Suggested fix

Bind the title to `DisplayName`, as the Parameter column does:

```xml
DistributionTitle="{Binding Path=DisplayName}"
```

No compatibility risk; nothing is saved.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's binding for now ([`src/BestFit/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.axaml` line 73](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.axaml#L73)).
