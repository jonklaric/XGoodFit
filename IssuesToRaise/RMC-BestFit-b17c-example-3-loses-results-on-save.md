# "B17C Example #3" opens empty, and saving it discards its stored MCMC and analysis results

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.UI` (Bulletin 17C analysis element) and the example project `examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), which was still `main` on 2026-10-30.

**Type:** data loss · **Severity:** medium

**Verification:** verified by reading the source, and reproduced with upstream's own code: XGoodFit's file-compatibility reference tool builds `RMC.BestFit.UI` 2.0.1 from this commit (against the `RMC.Wpf.Framework.*` 1.0.5 packages), opens the example and calls the element's `Save()`. The before/after values below come from that run and from the example file itself.

## Summary

One Bulletin 17C analysis in the shipped Bayesian examples, "B17C Example #3", was last written by a 2.0 pre-release. Its row has no `Bulletin17CDistribution` or `AnalysisXml`, so 2.0.1 opens it as an empty, invalid analysis (sample size 0) and never loads the results the row still holds. The first time that analysis is saved, 2.0.1 overwrites its `AnalysisResults` (2,207 characters) and `MCMCResults` (352,882 bytes) with empty values.

## Details and how to reproduce

1. Look at the row as shipped (any SQLite client):
   ```sql
   SELECT Name, length(AnalysisResults), length(MCMCResults),
          length(Bulletin17CDistribution), length(AnalysisXml), LastModified
   FROM "<Bulletin 17C>";
   -- B17C Example #3 | 2207 | 352882 | 0 | 0 | 9/13/2025 9:49:13 PM
   ```
   The project row says `SoftwareVersion` 2.0.1, but this element's row was last written on 13 September 2025. Its settings are in the older `UnivariateDistribution` and `GMM` columns.
2. Open the project in RMC-BestFit 2.0.1 and select "B17C Example #3". **Expected:** the fitted analysis with its 84 observations and results. **Actual:** an empty distribution, sample size 0, the analysis invalid, no results, although its input data "Example #3" exists and is valid.
3. Change anything in that analysis (its description, say) and use File > Save, which saves edited elements. Reopen.
   **Actual:** the analysis is now valid with 84 observations (the default `Bulletin17CDistribution` that was saved is combined with the input data), but its results are gone:
   ```sql
   -- B17C Example #3 | 0 | 0 | 1598 | 1205 | ...
   ```
   The estimation has to be run again, with default settings rather than the ones in the old `UnivariateDistribution` column.

What the reference run recorded for that row after saving (table `<Bulletin 17C>`, columns in table order): `AnalysisResults` `''`, `MCMCResults` a 0-byte blob, `Bulletin17CDistribution` 1,598 bytes, `AnalysisXml` 1,205 bytes. The legacy columns (`UnivariateDistribution`, `GMM`, `UncertaintyAnalysisResults`) are left as they were. Reopening the saved file differs from opening the original in exactly three properties of this analysis: `Bulletin17CDistribution/DataFrame`, `Bulletin17CDistribution/SampleSize` and `IsValid`.

## Why this is an issue and not intended behaviour

- The results are on disk and readable, but the loader never reads them for this row. It reconstructs the analysis only when `Bulletin17CDistribution` is present; otherwise the constructor's empty `Bulletin17CDistribution()` stays in place and the stored `MCMCResults` and `AnalysisResults` are skipped.
- The code already has a "legacy fallback" for rows without `AnalysisXml` (lines 953-966), which shows that older rows are meant to load. It just sits inside the branch that requires `Bulletin17CDistribution`, so this older layout never reaches it. A comment in the constructor also says there's "no legacy migration path" for B17C (line 116).
- Saving then writes the in-memory state, so results that were never loaded are replaced with nothing. Nothing tells the user that results are being dropped.
- It happens in an example that ships with 2.0.1, which is where new users start.

## Where in the source

- Loading: only a non-empty `Bulletin17CDistribution` rebuilds the analysis and loads the stored results. [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs` lines 939-972](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L939-L972)
  ```csharp
  var modelXmlStr = dtView.ColumnNames.Contains("Bulletin17CDistribution") ? dtView.GetCell("Bulletin17CDistribution", rowIndex)?.ToString() : null;
  if (!string.IsNullOrEmpty(modelXmlStr) && InputData != null && InputData.DataFrame != null)
  {
      ...
      MCMCResults mcmcResults = AnalysisPersistenceHelper.TryLoadMCMCResults(dtView, rowIndex, Name);
      UncertaintyAnalysisResults analysisResults = AnalysisPersistenceHelper.TryLoadAnalysisResults(dtView, nameof(AnalysisResults), rowIndex, Name);
  ```
- The empty default it falls back to: [lines 83-85](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L83-L85), and the "no legacy migration path" comment, [line 116](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L116).
- Saving writes whatever is in memory: [lines 1090-1100](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/B17CAnalysis.cs#L1090-L1100)
  ```csharp
  dtView.EditCell(rowIndex, nameof(MCMCResults),
      AnalysisPersistenceHelper.SerializeMCMCResults(_innerAnalysis.BayesianAnalysis.Results));
  dtView.EditCell(rowIndex, nameof(AnalysisResults),
      AnalysisPersistenceHelper.SerializeAnalysisResults(_innerAnalysis.AnalysisResults));
  ```
  and `null` results serialise to an empty array and an empty string: [`src/RMC.BestFit.UI/Elements/Support/AnalysisPersistenceHelper.cs` lines 144-159](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/Support/AnalysisPersistenceHelper.cs#L144-L159).
- An element is saved only when it is dirty: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysisCollection.cs` lines 126-134](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysisCollection.cs#L126-L134). So opening and saving without touching the analysis keeps the row; any edit to it loses the results.
- The example: [`examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit).

## Suggested fix

Any of these, most robust first:

1. **Don't overwrite what wasn't loaded.** Remember in `Open()` whether the stored results were loaded. If they weren't, and the user hasn't re-run the analysis, leave the `MCMCResults` and `AnalysisResults` cells as they are in `Save()`, as is already done for the legacy columns.
2. **Migrate the 2.0-beta layout.** When `Bulletin17CDistribution` is empty but `UnivariateDistribution` (and `GMM`) are present, build the `Bulletin17CDistribution` from them and load `MCMCResults`/`AnalysisResults` the way the other branch does. Other elements' loaders show the pattern. This also restores the example's own settings instead of defaults.
3. **At least re-save the example** with 2.0.1 after re-running it, so the shipped file is in the current layout. This fixes the example but not users' own projects from 2.0 betas.

Compatibility: options 1 and 2 only change how old rows are read, and files written by 2.0.1 load as before. Option 2 needs care to read the beta's `UnivariateDistribution` XML exactly as the beta wrote it.

## How XGoodFit handles it

XGoodFit is a community port of RMC-BestFit to Avalonia that must read and write `.bestfit` files exactly as 2.0.1 does. Its port of `B17CAnalysis.cs` keeps this behaviour, and its file-compatibility suite pins it: [`tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden/README.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden/README.md) ("One example loses data") and the golden description of the example, [`…/3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit.txt`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/tests/BestFit/RMC.BestFit.FileCompat.Tests/Golden/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit.txt) (row line 36, reopen differences lines 142-144). A fix upstream would be ported, and the golden data regenerated.
