# Bar series settings save the label font but don't restore it

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/OxyPlotControls/Series/BarSeriesControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

The bar series settings write the label font from the WPF series' `FontFamily`, but read it back into the OxyPlot model's `InternalSeries.Font`. The next property synchronisation copies the WPF `FontFamily`, which was never restored, over the model's font, so a saved font doesn't survive loading.

## Details and how to reproduce

Set a bar series' label font to, say, Consolas in the series properties, save the settings, and load them into another plot or reopen. **Expected:** Consolas. **Actual:** the default font.

## Why this is an issue and not intended behaviour

Every other labelling attribute in the same block (`Title`, `Foreground`, `FontSize`, `FontWeight`, `Padding`) is read back into the same WPF property it was written from. `Font` alone targets the model.

## Where in the source

- Written from the WPF property: [`BarSeriesControl.xaml.cs` line 205](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Series/BarSeriesControl.xaml.cs#L205): `labelProps.SetAttributeValue("Font", barSeries.FontFamily);`
- Read into the model: [lines 153-154](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Series/BarSeriesControl.xaml.cs#L153-L154): `barSeries.InternalSeries.Font = labelingElement.Attribute("Font")!.Value;`
- Overwritten on sync: [`src/OxyPlot/OxyPlot.Wpf/Series/Series.cs` line 398](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlot/OxyPlot.Wpf/Series/Series.cs#L398): `s.Font = this.FontFamily?.ToString();`

## Suggested fix

```csharp
if (labelingElement.Attribute("Font") != null)
    barSeries.FontFamily = new FontFamily(labelingElement.Attribute("Font")!.Value);
```

The saved attribute is unchanged, so existing settings load correctly after the fix.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps this in its OxyPlotControls port (branch `t3/port-oxyplot-controls`, not yet on `main`).
