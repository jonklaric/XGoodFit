# Small fixes in RMC.BestFit.App: latent null dereferences, an unthemed progress bar and stale code

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App` (`MarkovChainTraceControl`, `QuantilePriorsControl`, `MainProjectNode`, `BatchRunWindow`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug (cleanup) · **Severity:** low

**Verification:** verified by reading the source; none of these was run. Items 1 and 3 are latent: no current path I found triggers them.

## Summary

A checklist of small problems found while porting the application's views. None changes results today; each is a one- or two-line fix. A related item, the unused `BestFitStyles.xaml`, is filed separately in [`RMC-BestFit-unused-bestfitstyles-dictionary.md`](RMC-BestFit-unused-bestfitstyles-dictionary.md).

## Details and how to reproduce

1. **The trace plot reads `Results.MarkovChains` without a null check.** `MarkovChainTraceControl`'s update path guards `Analysis`, `IsEstimated` and `Results`, then calls `Analysis.Results.MarkovChains.Count()`. `MCMCResults.MarkovChains` is declared nullable and is left null by the constructor that takes a MAP and a list of parameter sets, which `Bulletin17CAnalysis` uses. The cache builder in the same control does check for null. The Bulletin 17C view doesn't host the trace control, so this can't happen today, but any future analysis that builds its results that way and shows a trace would throw.
2. **`QuantilePriorsControl` binds a check box's `Visibility` two-way.** The source is the control's own `ShowSingleQuantileCheckBox` property, and nothing else sets the check box's visibility, so `Mode=TwoWay` serves no purpose and invites a write-back if anything ever does.
3. **`GetPropertiesControl` calls `_plotPropertiesControl.Plot.Equals(plot)` without checking `Plot` for null** in 11 of its 12 branches. The twelfth, and `PlotPropertiesCalled`, check it, the latter with the comment "if `_plotPropertiesOpen` was set true but `_plotPropertiesControl.Plot` was reset to null by a separate code path, dereferencing `.Plot.Equals` would NRE". The same reasoning applies to the 11 unguarded branches.
4. **The batch run window's progress bars lose the theme's style.** Each progress bar has a local `Style` (for its visibility triggers) without `BasedOn`. A local style replaces the implicit `ProgressBar` style that the Themes library merges into the application, so these bars use WPF's default template rather than the theme's. The window's local `TextBlock` and `Image` styles don't lose anything, since the Themes library defines no implicit style for those types. Its `DataGrid.RowStyle` does replace the theme's `DataGridRowStyle`, which the theme's `DataGrid` style sets ([`DataGrid.xaml` line 354](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/DataGrid.xaml#L354)); it sets its own theme colours, so that may be deliberate.
5. **The batch run window declares a `BooleanToVisibilityConverter` (`BoolToVisConverter`) that nothing uses.**
6. **`DocumentClosed`'s documentation says it saves plot settings.** The summary says it "Saves plot settings to the element", and the remarks list "Serializes and saves all plot configurations back to the element". The method only unsubscribes events (and clears one view's element); a comment inside it says plots are owned by the element, so no serialisation is needed on close. The documentation should match.

## Why this is an issue and not intended behaviour

Items 1 and 3 are guards the code already applies elsewhere for the same values; item 3's own comment says the value can be null. Item 4 is the usual WPF trap of a local style without `BasedOn`, and the result differs from every other progress bar in the application. Items 2, 5 and 6 are leftovers that mislead readers.

## Where in the source

1. [`src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs` line 778](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs#L778) (`if (Analysis.NumberOfChains != Analysis.Results.MarkovChains.Count())`), after the guard at [line 749](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs#L749); the null check in the cache builder is at [line 596](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.xaml.cs#L596). Numerics: [`Numerics/Sampling/MCMC/Support/MCMCResults.cs` line 57](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Sampling/MCMC/Support/MCMCResults.cs#L57) (the constructor) and [line 69](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Sampling/MCMC/Support/MCMCResults.cs#L69) (`public List<ParameterSet>[]? MarkovChains`). Its use: [`src/RMC.BestFit/Analyses/Univariate/Bulletin17CAnalysis.cs` line 794](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Univariate/Bulletin17CAnalysis.cs#L794).
2. [`src/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.xaml` line 91](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.xaml#L91): `Visibility="{Binding ShowSingleQuantileCheckBox, UpdateSourceTrigger=PropertyChanged, Mode=TwoWay, Converter={StaticResource BoolVisibilityConverter}}"`.
3. [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` line 1294](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1294), and lines 1310, 1326, 1342, 1358, 1374, 1390, 1422, 1438, 1454 and 1470. Guarded: [line 1406](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1406) and [lines 1503-1505](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1503-L1505).
4. [`src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml` lines 67-87](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml#L67-L87). The theme's implicit style: [WPF-Framework `src/Themes/Resources/Controls/ProgressBar.xaml` line 17](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/ProgressBar.xaml#L17), merged by [`Merged.xaml` line 81](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/Merged.xaml#L81).
5. [`BatchRunWindow.xaml` line 23](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml#L23).
6. [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` lines 1070-1084](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1070-L1084) (the documentation) and [line 1127](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L1127) (the comment).

## Suggested fix

1. `if (Analysis.Results.MarkovChains == null || Analysis.NumberOfChains != Analysis.Results.MarkovChains.Length)`, treating null like the cache builder does.
2. Drop `Mode=TwoWay` (and `UpdateSourceTrigger`).
3. Add `_plotPropertiesControl.Plot != null &&` to the 11 conditions, or compare with `Equals(_plotPropertiesControl.Plot, plot)`, or factor the check into one helper.
4. Add `BasedOn="{StaticResource {x:Type ProgressBar}}"` to the progress bar's style.
5. Delete the converter.
6. Reword the summary to say the method unsubscribes the views' events, and drop the serialisation item.

No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps items 1, 2, 3 and 6 as upstream for now (for example [`MarkovChainTraceControl.axaml.cs` line 835](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/MarkovChainTraceControl.axaml.cs#L835) and [`QuantilePriorsControl.axaml` line 111](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/QuantilePriorsControl.axaml#L111)). Items 4 and 5 don't arise there: Avalonia styles add to a control's theme rather than replacing it, and the port's batch window doesn't declare the converter.
