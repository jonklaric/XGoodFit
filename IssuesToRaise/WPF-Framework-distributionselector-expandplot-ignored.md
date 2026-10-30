# DistributionSelectorControl ignores ExpandPlot: the plot expander is always open

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/NumericControls/Distributions/Univariate/Distribution Selector/DistributionSelectorControl.xaml(.cs)`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source.

## Summary

`DistributionSelectorControl` declares an `ExpandPlot` dependency property, documented as "whether the plot expander is expanded by default", but nothing reads it. The plot expander's `IsExpanded` is hard-coded to `True`, so `ExpandPlot="False"` has no effect. RMC-BestFit sets it in three places, and NumericControls' own `DistributionSelectorPopup` in one.

## Details and how to reproduce

Use `<DistributionSelectorControl ExpandPlot="False"/>`. **Expected:** the "Probability Density Plot" expander starts collapsed. **Actual:** it starts expanded.

In RMC-BestFit 2.0.1 the parameter-prior and quantile-prior editors and the input-data distribution selector all pass `ExpandPlot="False"` ([`InputDataControl.xaml` line 128](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml#L128), [`ParameterPriorsControl.xaml` line 59](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/ParameterPriorsControl.xaml#L59), [`QuantilePriorsControl.xaml` line 60](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.xaml#L60)), and get an open plot.

## Why this is an issue and not intended behaviour

The property and its documentation exist only for this purpose, and both RMC-BestFit and NumericControls' own popup set it. The older `Selector` control in the same folder does use its `ExpandPlot`.

## Where in the source

- The property: [`DistributionSelectorControl.xaml.cs` lines 376-389](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml.cs#L376-L389), with no other reference in the file or its XAML.
- The expander: [`DistributionSelectorControl.xaml` line 176](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.xaml#L176)
  ```xml
  <Expander x:Name="PlotExpander" Header="Probability Density Plot" Grid.Row="3" IsExpanded="True" ...
  ```

## Suggested fix

```xml
<Expander x:Name="PlotExpander" ... IsExpanded="{Binding ExpandPlot, RelativeSource={RelativeSource AncestorType=local:DistributionSelectorControl}, Mode=OneTime}" ...
```

Or set `PlotExpander.IsExpanded = ExpandPlot` in a property-changed callback, if the user's later toggling should survive. This changes what RMC-BestFit's three editors show at first (the plot starts collapsed, as they ask). Nothing is saved, so no file is affected.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the expander open, as upstream does. See the header comment of [`DistributionSelectorControl.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/NumericControls/Distributions/Univariate/Distribution%20Selector/DistributionSelectorControl.axaml) ("nothing reads ExpandPlot").
