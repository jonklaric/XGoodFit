# Seven example walkthroughs describe earlier states of their shipped projects

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `examples/`: the walkthroughs (`*.md`) and figures of the classic time series, Orestimba peaks-over-threshold, Viglione et al. (2013), Bulletin 17C Bayesian, Brays Bayou and synthetic nonstationary, and Bulletin 17C GMM examples. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** documentation · **Severity:** low

**Verification:** verified by reading the shipped project files (SQLite, read-only) and the figures' data, and seen in XGoodFit's port: its walkthrough tests open each example in the application and check what the walkthrough says. Where the tests re-ran an estimate, that's said below; the stored values were read from the files themselves.

Related: [The examples' "native" figures don't show what the application shows](RMC-BestFit-example-figures-differ-from-app.md), which is about the exporter's styling; this issue is about the values.

## Summary

In seven walkthroughs, some values describe a version of the example project from before it was last edited and saved. RMC-BestFit 2.0.1 and XGoodFit agree with each other and with the shipped projects; the walkthroughs don't. Where a figure's data records its source, its `source.runId` is the SHA-256 of an earlier project file, not of the shipped one. A user following these walkthroughs sees different numbers from the ones the text tells them to look for.

## Details and how to reproduce

Open each project and compare with its walkthrough:

| Example | The walkthrough says | The shipped project holds, and both applications show |
|---|---|---|
| Classic time series, "Airline Passengers - TSA" (`7-time-series-analysis/2-classic-time-series-examples`) | training / validation / future 120 / 24 / 0 (line 27); DIC 1,115.279, RMSE 26.65 (line 38); its parameter table (lines 44-49). The airline figures (`airline-residual-acf`, `-trace`, `-validation`) come from file `sha256:cc227c72…` | The fit was re-run on the default training rule's 115 months (`TrainingTimeSteps="115" UseDefaultTrainingSteps="True"`, last modified 10/8/2026): DIC 1,048.945, RMSE 24.206, which RMC-BestFit's native export shows too. The shipped file is `32fd922f…`. The element's own description still says "Training 120" and "maximum R-hat 1.262". The project's other two fits are unchanged, so their figures still match |
| Orestimba peaks-over-threshold (`2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.md`) | "Saved Lambda 0.8764044944 per year (78 / 89)"; "Explicit source-exposure field: Absent in this older saved input" (lines 34-35). The figures come from `sha256:dd174446…` | `Lambda="0.82105263157894737"` and `PointProcessObservationYears="95"` (78 / 95, 1932-2026; saved 9/29/2026), and the summary statistics show λ = 0.8211. The shipped file is `1e2a98e7…` |
| Viglione et al. (2013) (`4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion`) | "nine stationary Generalized Extreme Value (GEV) analyses" (line 3) | Ten: the tenth, "Univariate Analysis_10", is an estimated GEV fit to "Systematic (1951-2005) + Temporal Expansion" with no description, created 9/30/2026 8:22 PM. It may have been left in by accident |
| Bulletin 17C Bayesian, "Bayes Example #1" (`1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.md`) | 1% AEP 5,231.877 cfs, limits 4,248.95-6,902.67, R-hat 0.99988, ESS 9,300 (line 59); its parameter summaries (lines 68-74); "the other five Bayesian alternatives retain default-flat-prior mode" (line 47). The frequency and trace figures come from `sha256:0c38589f…`; the frequency figure's curve gives the same 5,231.9 cfs | Last modified 9/30/2026 8:33 PM, with `UseDefaultFlatPriors="False"` and a Normal(0, 0.3) prior on the skew. Its saved results: 1% AEP 4,806.606 cfs, limits 4,139.069-5,777.893, R-hat 1.0009, ESS 9,515, which RMC-BestFit 2.0.1 shows and its native export has. The shipped file is `ae028ece…`. XGoodFit's test re-ran Estimate with the saved seed and got the saved fit back |
| Brays Bayou (`1-univariate-analysis/5-nonstationary-ffa/nsffa-brays-bayou-texas.md`) | "USGS 08075000 Brays Bayou_copy_copy": "The 1930–1935 gap is represented by six nonexceedances below 35,100 cfs" (line 20) | That copy's threshold is 200,000 cfs (`<ThresholdData StartIndex="1930" EndIndex="1935" Value="200000" NumberBelow="6" …/>`); the other two cfs copies have 35,100 |
| Synthetic nonstationary data (`nsffa-synthetic-data.md`) | NSFFA - Cubic Trend: largest R-hat 1.00004, smallest ESS 9,028 (line 49) | The stored run's MCMC report gives 1.0005 and 9,523; its quantiles and DIC match the walkthrough. Seen in XGoodFit's test; a re-run reproduces the stored values |
| Bulletin 17C GMM, "Example #2 - BCB" (`2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md`) | A saved fit: 13,823.815 cfs, limits 8,735.409-17,720.516 (line 53); optimizer fallbacks, evaluation-limit statuses and z-limit clips 33 / 3 / 5 (line 67) | Not estimated: `IsEstimated="False"`, no results, last modified 9/30/2026 9:02 PM (Example #2 a minute later; the other twelve fits were last saved on 9/29). Estimate gives the walkthrough's 13,823.815 but limits 8,582.358-17,706.822 and counters 39 / 3 / 10 (XGoodFit's test). Examples #1 and #3 - BCB re-estimate exactly to their saved fits |

The hashes are SHA-256 of the `.bestfit` files at `bfce2dd`; their `-wal` side files are empty.

## Why this is an issue and not intended behaviour

The walkthroughs present these values as what the saved project holds ("Saved Lambda", "the saved parameter summaries are", the airline results "preserved as a diagnostic example … not tuned or rerun"), and the figures' `source.kind` is `saved`. The project files were edited and saved after the text and figures were written (the last-modified dates above are in late September and early October 2026), and the documentation wasn't regenerated.

## Where in the source

- [`examples/7-time-series-analysis/2-classic-time-series-examples/classic-time-series-examples.md` lines 27, 38 and 44-49](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/7-time-series-analysis/2-classic-time-series-examples/classic-time-series-examples.md#L27-L49), and its `images/classic-time-series-examples-airline-*.plotspec.json.gz`.
- [`examples/2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.md` lines 33-35](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.md#L33-L35).
- [`examples/4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.md` line 3](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.md#L3).
- [`examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.md` lines 47, 59 and 68-74](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.md#L47-L74), and its `images/bulletin-17c-bayesian-examples-moose.plotspec.json.gz` and `-trace.plotspec.json.gz`.
- [`examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-brays-bayou-texas.md` line 20](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-brays-bayou-texas.md#L20) and [`nsffa-synthetic-data.md` line 49](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-synthetic-data.md#L49).
- [`examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md` lines 53 and 67](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md#L53-L67).

## Suggested fix

For each example, decide which is meant, the project or the text, and make the other agree:

1. Regenerate the values and figures from the shipped projects: the airline rows, figures and element description; Orestimba's Lambda row; Bayes Example #1's table, summaries, prior description and figures; the Cubic Trend's diagnostics.
2. Or restore the projects the text describes: remove "Univariate Analysis_10" from the Viglione project, set Brays Bayou_copy_copy's threshold back to 35,100 cfs, and re-estimate and save "Example #2 - BCB".

A check in the documentation build that each figure's `source.runId` equals its project file's hash would catch this the next time a project is saved without its documentation.

## How XGoodFit handles it

XGoodFit doesn't change the examples. Its walkthrough tests check what the application shows, the shipped projects' values, and record each discrepancy: [`docs/walkthrough-coverage/univariate.md` lines 58, 94-103, 127, 166 and 186](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/docs/walkthrough-coverage/univariate.md#L58) and [`docs/walkthrough-coverage/time-series-input-bivariate.md` line 235](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/docs/walkthrough-coverage/time-series-input-bivariate.md#L235); the airline check is in [`tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/TimeSeriesAnalysisViewTests.cs` lines 207-214](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/TimeSeriesAnalysisViewTests.cs#L207-L214).
