# The examples' "native" figures don't show what the application shows

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `tools/PlotReferenceExporter` (`Program.cs`, `PanelExporter.cs`) and the figures it produced under `examples/`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** documentation · **Severity:** low

**Verification:** verified by reading the exporter; the images weren't compared pixel by pixel.

## Summary

The examples' documentation shows "native" plot and panel captures made by `tools/PlotReferenceExporter`. The exporter doesn't reproduce the application's state or styling: it resets each plot to its factory default rather than using the plot saved in the example project, adjusts axes the application doesn't adjust, merges the theme before the component dictionaries (the application merges it last), and replaces three of the application's styles with empty ones. Users comparing their screen with an example's figures see differences that aren't in their data.

## Details and how to reproduce

Compare, for example, the HEC-DSS example's `screenshots/hec-dss-inflow-ts-plot-native.png` with the same plot in the application. The exporter's own metadata records `factoryReset = true` and lists its `displayCorrections` in each capture's `.json.gz`.

## Why this is an issue and not intended behaviour

The figures are labelled "native", and their captions ask readers to compare details (for example "Compare the common hourly clock and CFS…" in the HEC-DSS example). Some differences are deliberate (the white background is listed as a display correction), but the HEC-DSS example's text doesn't mention the factory reset or the styling.

## Where in the source

- Factory reset, [`tools/PlotReferenceExporter/Program.cs` line 108](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L108) (`Plot plot = ResetFactoryPlot(plotOwner, factory, propertyName);`), recorded at [line 342](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L342).
- Axis corrections: [lines 227-243](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L227-L243) (see [the axis labels issue](RMC-BestFit-time-series-axis-labels-repeat.md)).
- Resources: [`Program.cs` lines 395-405](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L395-L405) and [`PanelExporter.cs` lines 183-201](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/PanelExporter.cs#L183-L201):
  ```csharp
  FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Light);   // theme first
  ... MergedDictionaries.Add(...)                                      // then the component dictionaries
  app.Resources["Center_CellStyle"] = new Style(typeof(DataGridCell));
  app.Resources["Left_CellStyle"] = new Style(typeof(DataGridCell));
  ```
  A key set directly in `Application.Resources` takes precedence over the same key in its merged dictionaries, where the theme defines these styles ([WPF-Framework `src/Themes/Resources/Controls/DataGrid.xaml`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/DataGrid.xaml)).
- The application's order: the component dictionaries in [`src/RMC.BestFit.App/App.xaml` lines 12-30](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/App.xaml#L12-L30), then the theme in [`App.xaml.cs` line 60](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/App.xaml.cs#L60). `ThemeManager.SetTheme` appends the theme dictionary ([WPF-Framework `src/FrameworkUI/Themes/ThemeManager.cs` line 148](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Themes/ThemeManager.cs#L148)).

## Suggested fix

1. Initialise resources as the application does: merge the component dictionaries first and call `SetTheme` last, or load the application's own `App.xaml`.
2. Don't overwrite `Center_CellStyle`, `Left_CellStyle` and `DataGridEditingTextBoxStyle`; if they were stubbed because a dictionary was missing, merge that dictionary instead.
3. Either export the plot as saved in the example project, or say in each example that the figures show the default plot settings.
4. Once the axis formats are fixed in the application, drop the axis corrections so the figures show the application's output.

No compatibility risk: only the documentation figures change.

## How XGoodFit handles it

XGoodFit doesn't build the exporter. Its porting guide tells its agents to compare their views with the figures' data, not their styling: [`docs/porting-guide.md` lines 386-387](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/docs/porting-guide.md#L386-L387).
