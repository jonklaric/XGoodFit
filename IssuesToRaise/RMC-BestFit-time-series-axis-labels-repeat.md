# Time series plots repeat axis labels for small values and short records

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.xaml.cs` and `src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source, and by upstream's own plot exporter, which corrects both cases. The label strings below are .NET's output for these formats; the tick positions OxyPlot picks weren't checked.

## Summary

The time series and seasonality plots fix the Y axis format at `"N0"`, so values below about 10 get integer labels that repeat. Their date axis switches to `"MMM-yyyy"` for any record from 90 days to 4 years long, so when ticks are less than a month apart the month repeats. RMC-BestFit's `tools/PlotReferenceExporter` detects both and corrects them before exporting the examples' figures, so the figures don't show what users see.

## Details and how to reproduce

- **Y axis.** For daily precipitation in inches, or any series whose values stay below about 10, OxyPlot's ticks are fractional (0, 0.2, 0.4, …). Formatted with `"N0"` they read:
  ```
  0, 0, 0, 1, 1, 1, 1, 1, 2
  ```
- **Date axis.** For a record of 90 days to about a year, ticks are often days or weeks apart. Weekly ticks formatted with `"MMM-yyyy"` read:
  ```
  Mar-2007, Mar-2007, Apr-2007, Apr-2007, Apr-2007, Apr-2007, Apr-2007, May-2007, May-2007
  ```
  The HEC-DSS example (16 March to 10 July 2007, 116 days) falls in this range.

Expected: labels that tell the ticks apart. Actual: repeated labels, which also hide where the ticks fall.

## Why this is an issue and not intended behaviour

Upstream's own exporter says so. In [`tools/PlotReferenceExporter/Program.cs` lines 227-243](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L227-L243):

```csharp
if (axis.StringFormat == "N0" && span > 0 && span < 10)
{
    axis.StringFormat = "G4";
    displayCorrections.Add($"{axis.Key}: fractional tick precision avoids repeated integer labels; coordinates unchanged.");
}
if (options["plot-id"] == "time_series_data.series" && axis is OxyPlot.Axes.DateTimeAxis dates && span > 0 && span < 731)
{
    dates.IntervalType = span > 120 ? OxyPlot.Axes.DateTimeIntervalType.Months : OxyPlot.Axes.DateTimeIntervalType.Days;
    ...
    dates.StringFormat = span > 120 ? "MMM yyyy" : "dd MMM yyyy";
    displayCorrections.Add($"{axis.Key}: short-record calendar ticks made explicit; source dates unchanged.");
}
```

## Where in the source

- The default plots' Y axes: [`src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs` line 2499](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L2499) (`CreateDefaultTimeSeriesPlot`) and [line 2547](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L2547) (`CreateDefaultSeasonalityPlot`): `StringFormat = "N0",`
- The seasonality plot's Y axis, set again when it's populated: [`src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.xaml.cs` line 1063](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.xaml.cs#L1063).
- The date format chosen by span, in `TimeSeriesPlotModelUpdated`: [lines 792-803](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.xaml.cs#L792-L803) (condensed):
  ```csharp
  if (dateSpan.TotalDays > 365 * 4)      dateTimeAxis.StringFormat = "yyyy";
  else if (dateSpan.TotalDays > 90)      dateTimeAxis.StringFormat = "MMM-yyyy";
  else if (dateSpan.TotalDays > 2)       dateTimeAxis.StringFormat = "dd-MMM";
  ```

## Suggested fix

- **Y axis:** leave `StringFormat` unset (OxyPlot's default format shows fractional ticks), or choose the format from the axis range when the plot updates, as the exporter does (`"G4"` below a span of 10).
- **Dates:** set `IntervalType` with the format, as the exporter does: months with `"MMM yyyy"` above about 120 days, days with `"dd MMM yyyy"` below. Or pick the format from the actual tick interval rather than the record length.

Then the exporter's corrections can be removed, and the examples' figures will match the application. I haven't checked whether projects save an axis's `StringFormat` with their plot settings; if they do, plots saved with `"N0"` would keep it until reset, unless a saved `"N0"` is treated like the default.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's formats for now: [`src/BestFit/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs` lines 2503 and 2551](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L2503), and [`TimeSeriesControl.axaml.cs` lines 823 and 1088](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesControl.axaml.cs#L823).
