# DistributionSelectorControl doesn't flag invalid parameters when the distribution is set from code

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/NumericControls/Distributions/Univariate/Distribution Selector/DistributionSelectorControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; also seen while porting the control to Avalonia (the port keeps this logic).

## Summary

Parameter cells are marked invalid only in `SetDistributionParameters`. When `SelectedDistribution` is set from code, for example by a binding when a project opens, the callback selects the combo-box item with `_propertyChanging = true`, and the selection handler skips `SetDistributionParameters` while that flag is set. A distribution with invalid parameters is shown with no cells flagged, until the user edits a parameter.

## Details and how to reproduce

Bind `SelectedDistribution` to a `Normal` with σ = -1 (or open an RMC-BestFit project whose prior has invalid parameters). **Expected:** the σ cell shows the validation error, as it does when the user types -1. **Actual:** no cell is flagged. The plot and statistics are updated, but the parameter grid looks valid.

## Why this is an issue and not intended behaviour

The same distribution shows its error when the user enters the value. Whether the error is shown shouldn't depend on where the value came from, and `_propertyChanging` is there to stop the control writing `SelectedDistribution` back, not to skip validation.

## Where in the source

- The callback, setting `_propertyChanging` around the selection: [`DistributionSelectorControl.xaml.cs` lines 222-257](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml.cs#L222-L257).
- The selection handler: [lines 600-659](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml.cs#L600-L659)
  ```csharp
  // Update selected distribution
  if (_propertyChanging == false) { SetDistributionParameters(false); }   // line 649
  ```
- The only place that flags cells, `SetInvalidParameter` in `SetDistributionParameters`: [lines 694-740](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml.cs#L694-L740).

## Suggested fix

Split validation from write-back. After the parameter list is rebuilt, always run the validation part (`ClearValidation()`, then `ValidateParameters` and `SetInvalidParameter`), and keep only the `SelectedDistribution = currentDistribution.Clone()` write-back behind `_propertyChanging`. Display only; nothing saved changes.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the behaviour, listed among the upstream bugs kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L576).
