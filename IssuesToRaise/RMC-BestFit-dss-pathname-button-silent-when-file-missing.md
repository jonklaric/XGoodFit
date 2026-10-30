# The DSS Pathname "…" button does nothing when the DSS file is missing, and DSS paths are stored absolute

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.xaml.cs`, `src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs` and the example `examples/1-time-series-data/5-hec-dss-import`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source and the example project (opened read-only with SQLite); not run in the application.

## Summary

The "…" button beside **DSS Pathname** opens the pathname selector only if the stored DSS file exists; otherwise it silently does nothing. DSS file paths are stored and loaded as absolute paths, so a project moved to another folder or machine has a missing file. The shipped HEC-DSS example stores its author's paths (`C:\GIT\rmc-bestfit\…`), so on other machines the button does nothing and **Import** fails with "The selected DSS file does not exist.", although the file ships beside the project. The example's instructions work around this by asking users to browse to the file again.

## Details and how to reproduce

1. Open `examples/1-time-series-data/5-hec-dss-import/hec-dss-import-example.bestfit` on any machine other than its author's.
2. Select **Grapevine Dam - Inflow** and click "…" beside **DSS Pathname**. Expected: the pathname selector opens, or a message says the DSS file can't be found. Actual, by reading: nothing happens.
3. Click **Import**. Actual, by reading: "The selected DSS file does not exist."

The project's two time series store:

```
HECDSSFullFilename  C:\GIT\rmc-bestfit\examples\1-time-series-data\5-hec-dss-import\dss-example-data.dss
HECDSSFullFilename  C:\GIT\RMC-BestFit\examples\1-time-series-data\5-hec-dss-import\dss-example-data.dss
```

## Why this is an issue and not intended behaviour

- A button that does nothing gives users no clue what to fix. The import path in the same feature reports the missing file, so the dialog should too.
- The example ships its DSS file next to the project, but the stored absolute path makes it unusable as shipped. Its instructions say "Replace the old machine-specific file path with this local path", which shows the problem is known.

## Where in the source

- The button, [`src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.xaml.cs` lines 641-653](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.xaml.cs#L641-L653):
  ```csharp
  if (Element == null) return;
  if (File.Exists(Element.HECDSSFullFilename))
  {
      var dssControl = new DSSPathSelectorWindow() { FullFileName = Element.HECDSSFullFilename };
      ...
  }
  ```
- The path loaded as stored: [`src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs` line 1238](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L1238); the import's check: [lines 2083-2086](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/TimeSeriesData/TimeSeriesElement.cs#L2083-L2086).
- The example's instructions: [`examples/1-time-series-data/5-hec-dss-import/hec-dss-import-example.md` line 38](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/1-time-series-data/5-hec-dss-import/hec-dss-import-example.md#L38).

## Suggested fix

1. In `PathNameButton_Click`, tell the user when the file is missing ("The DSS file … can't be found. Choose it again with the DSS Filename button."), as the import does.
2. When a stored DSS path doesn't exist, try the same file name in the project's folder before giving up; or store the path relative to the project file when the DSS file is in or below its folder.
3. Re-save the example with a relative path (if 2 is adopted), or with just the file name.

Option 2 changes what's saved only for new saves, and old projects keep working, so the compatibility risk is low.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's behaviour for now ([`src/BestFit/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.axaml.cs` line 675](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/TimeSeriesData/TimeSeries/TimeSeriesPropertiesControl.axaml.cs#L675)).
