# Walkthrough coverage: Time Series Data, Input Data and Bivariate Distribution Analysis

Which test checks each checkable claim of the thirteen walkthroughs under
`sample-data/RMC-BestFit/examples/1-time-series-data/`, `2-input-data/` and `5-bivariate-distribution-analysis/`
(the `*.md` beside each example project).

**How the walkthrough tests work.** Each walkthrough has a class, `tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/Walkthrough<Name>Tests.cs`,
that follows it through XGoodFit's main window as a user does: it opens a copy of the example (once per class,
`WalkthroughSession`), double-clicks elements in the project explorer to open their documents, reads their properties
panels, switches their tabs, plot types and axis types, presses their buttons (New Time Series..., New Input Data...,
Delete..., the DSS file and pathname `...` buttons, Import, Process, Download), and compares what the views show with the
walkthrough at the walkthrough's own rounding. The helpers are `WalkthroughTesting.cs` (the session, the explorer and panel
captures, grids, rounding, figure data), `WalkthroughTimeSeriesTesting.cs`, `WalkthroughInputDataTesting.cs`,
`WalkthroughBayesianTesting.cs` (MCMC reports, summary statistics, the "Saved diagnostics" tables) and
`WalkthroughFilePicker.cs` (answers the open-file picker). The classes run in the `Walkthroughs` group of
`scripts/run-app-tests.sh` (`bash scripts/run-app-tests.sh Release <Class>` runs one).

**Categories.** `External` tests download from the live services (USGS, NOAA, Water Survey of Canada, Bureau of
Meteorology) and are left out of every run by default. `Large` tests open the instantaneous records over 200,000
observations, whose views take over a gigabyte; they run only with `XGOODFIT_LARGE_TESTS=1` (as TimeSeriesDataTests'
Large test). No walkthrough test reruns an MCMC fit, so none is `Long`.

**Area tests referred to below** (all in `tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/`; they host the views directly,
not through the main window): `TimeSeriesDataTests` (TSD), `InputDataViewTests` (IDV), `BivariateAnalysisViewTests` (BAV),
`CoincidentFrequencyViewTests` (CFV), and the example tour (`ExampleTour*Tests`: every element opened, nothing changes).

**Not checked anywhere, by design:** interpretation and advice (what a user should conclude, study decisions, "do not
…"), links to external sites, and the walkthroughs' own Python SVG and PNG styling (the tests compare the figures' data,
`*.plotspec.json.gz`, not their drawing). Created On and Last Modified aren't compared: the examples store them in en-US,
which other cultures don't read (an upstream issue already in IssuesToRaise).

## 1-time-series-data/1-usgs-download/usgs-download-example.md

Class `WalkthroughUsgsDownloadTests`.

| Claim | Test |
|---|---|
| Project Explorer capture: Time Series Data with the eight records, every collection expanded (`usgs-project-explorer-current`) | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Properties capture of USGS - 01134500 - Daily Discharge (`usgs-properties-panel-current`): Data Entry Method = USGS, Data Type = Daily Discharge, Site Number = 01134500, Download | same; TSD `PropertiesPanels_ShowEachSourcesSettings_AsRmcBestFitDid` (hosted panel) |
| The Time Series tab holds the Time Series Data grid and plot, Summary Statistics beside the grid; Seasonality, ACF and PACF tabs on the left | `FindTheControls_…` (`AssertTabs`) |
| "New Time Series..." on Time Series Data, a name, then Properties (Data Entry Method = USGS, site number, series type) and Download | `NewSource_IsSetUpForADownload`; the download itself `NewSource_Downloads` (External) |
| Inventory: units, first and last dates, ordinates and missing values of the six records up to 200,000 ordinates (daily discharge, St. Louis stage with 197 missing, measured discharge 412 and stage 939, peak discharge and stage 69 each) | `SavedRecords_AreTheInventorys` |
| Inventory of the two instantaneous records (1,272,826 and 723,856 ordinates); step 5, their within-day timestamps | `LargestRecords_AreTheInventorys` (Large) |
| Step 1: daily means in cfs, date and value columns, chronological plot | `WorkThroughTheExample_TheRecordsAreWhatTheStepsSay` |
| Step 2: Seasonality Plot groups the observations by month, with month names | same (`AssertSeasonality`) |
| Step 3: Peak Discharge, 69 observations 1929-2025, years missing; its USGS Text File tab | same; TSD `UsgsTextTab_ShowsTheDownloadedText_ForPeakSeriesOnly` |
| Step 4: Measured Discharge and Stage have different counts (412, 939) | same |
| Limits: St. Louis stage's generic "Value" label and 197 missing values; its height unit is feet | same; the height unit is checked on the element: RMC-BestFit's properties have no Height Unit row |
| Figures 1-4 and the instantaneous plot: native exports (`*-native.json.gz`) | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid`; the instantaneous one TSD `LargestRecords_DrawAsRmcBestFitDid_AndShowingThemChangesNothing` (Large) |
| Figures 1-4: the walkthrough's plot data (`figures/*.plotspec.json.gz`) | `Figures_AreWhatTheViewsShow` |
| Steps 5-6 advice, "Check your understanding" | not checkable (interpretation) |

## 1-time-series-data/2-ghcn-download/ghcn-download-example.md

Class `WalkthroughGhcnDownloadTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = GHCN, Data Type = Daily Precipitation, Site Number = USC00040741, Depth Unit = Inches; the tabs | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Inventory: Precipitation (in) 1960-07-01 to 2026-06-30, 24,106 ordinates, 482 missing; Snow (in) 1957-05-01 to 2022-05-31, 23,772, 2,197 missing | `SavedRecords_AreTheInventorys` |
| Step 2: a zero is a stored zero, a missing value is unavailable (the table shows 0 and N/A); seasonality by month | `WorkThroughTheExample_ZerosMissingValuesAndTheSnowfallsUnits` |
| Source-unit finding: 38 nonzero snowfall values, NOAA's SNOW divided by 254; 445 mm on 1975-01-29 saved as 1.752 in, the source being 17.52 in | same (the upstream conversion the walkthrough documents; XGoodFit shows the same saved values) |
| Step 4: a new GHCN source with an 11-character station, the variable and depth unit, then Download | `NewSource_IsSetUpForADownload`; `NewSource_Downloads` (External) |
| Figures 1-3, native exports | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid` |
| Figures 1-3, plot data | `Figures_AreWhatTheViewsShow` |
| Step 5 and the advice | not checkable (a link; interpretation) |

## 1-time-series-data/3-chmn-download/chmn-download-example.md

Class `WalkthroughChmnDownloadTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = CHMN, Data Type = Daily Discharge, Site Number = 08MG005; the tabs | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Inventory of the six records (units, dates, ordinates, missing: 2,656 and 159 for the daily records; 157,213 each instantaneous; 64 and 13 peaks) | `SavedRecords_AreTheInventorys` |
| Step 3: the instantaneous records' saved interval is five minutes, timestamps five minutes apart, with gaps | `WorkThroughTheExample_IntervalsPeaksAndSeasonality` (the interval is checked on the series: CHMN's properties have no Time Interval row) |
| Step 4: 64 discharge peaks, 13 stage peaks | same |
| Step 5: daily discharge seasonality by month | same |
| Step 6: a new CHMN source, then Download | `NewSource_IsSetUpForADownload`; `NewSource_Downloads` (External) |
| Figures 1-3 and the instantaneous plot, native exports | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid` |
| Figures 1-3, plot data | `Figures_AreWhatTheViewsShow` |
| Steps 1-2 interpretation (m³/s, datum), "Check your understanding" | the units are the inventory's; the rest not checkable |

## 1-time-series-data/4-abom-download/abom-download-example.md

Class `WalkthroughAbomDownloadTests`.

| Claim | Test |
|---|---|
| Explorer capture (six records; the three-value manual test element is gone) and properties capture; Data Entry Method = ABOM, Data Type = Daily Precipitation, Site Number = 410730, Depth Unit = Millimeters; the tabs | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Inventory of the four daily records | `SavedRecords_AreTheInventorys` |
| Inventory of the instantaneous records (222,246 and 669,279); step 5, irregular sampling | `LargestRecords_AreTheInventorys` (Large) |
| Steps 1-4: millimetres; Cotter's Flow (cms), no missing values, seasonality; Murrumbidgee 98 and Murray stage 39 missing; Stage (m) | `WorkThroughTheExample_UnitsSeasonalityAndMissingValues` |
| Step 6: a new ABOM source, then Download | `NewSource_IsSetUpForADownload`; `NewSource_Downloads` (External) |
| Figures 1-2 and the instantaneous stage, native exports | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid` (the instantaneous stage: TSD's Large test) |
| Figures 1-4, plot data (3 and 4 have no native export) | `Figures_AreWhatTheViewsShow` |

## 1-time-series-data/5-hec-dss-import/hec-dss-import-example.md

Class `WalkthroughHecDssImportTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = HEC-DSS, DSS Filename and DSS Pathname with their `...` buttons, Import; the tabs | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Inventory: both CFS, 2007-03-16 to 2007-07-10, 2,785 ordinates, none missing | `SavedRecords_AreTheInventorys_AndShareTheirHourlyClock` |
| Steps 1-2: hourly values; the outflow's 2,785 timestamps are the inflow's | same |
| "Check your understanding": both pathnames are FLOW at 1Hour | same |
| Limits: maximum inflow about 34,118 cfs, release 1,382 cfs | same (Summary Statistics' Maximum) |
| Steps 3-5: browse to the supplied DSS file, the HEC-DSS Pathname Selector (capture `hec-dss-path-selector-current`: its title, 199 records, the inflow record's dated pathname), Set Path with each element's pathname, Import; the dates, hourly spacing, units and ordinates are the inventory's | `Import_FromTheSuppliedFile_GivesTheSavedRecords`; the selector's filters also TSD `DssPathSelector_ListsTheFilesPaths_AndGivesTheDatelessPath` |
| Step 5: Alternative Time Series' plus control, the outflow overlaid and removed | `AlternativeTimeSeries_OverlayTheOutflow_AndClearingItRemovesIt`; TSD `AlternativeTimeSeries_AreAddedRemovedAndRefreshed` |
| Figure 1 and the comparison, native exports | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid` |
| Figures 1-2, plot data | `Figures_AreWhatTheViewsShow` |
| What a water balance would need, and the other interpretation | not checkable (interpretation) |

## 1-time-series-data/6-manual-entry/manual-entry-example.md

Class `WalkthroughManualEntryTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = Manual Entry, Time Interval = 1-Month, Start Date; the tabs | `FindTheControls_TheExplorerPropertiesAndTabsAreTheWalkthroughs` |
| Inventory of the three records | `SavedRecords_AreTheInventorys` |
| Steps 1, 3, 4: 144 monthly airline values (1949-01 to 1960-12) and 790 CO2 values are the CSV files' on their dates; the Nile's 100 values are the CSV's, dated 1897-1996 where the CSV has 1871-1970 (26 years); the stored DailyDischarge series type | `SavedRecords_AreTheSuppliedFilesValues_AndTheNileIsShiftedBy26Years` (the series type on the element: manual entry shows no Data Type row) |
| Step 5: Time Series, Seasonality Plot and ACF Plot views | `Views_ShowTheTimeSeriesSeasonalityAndAcf` |
| Step 2 and "Check your understanding": a new element, interval and first date set, five pasted values get monthly dates | `NewManualEntry_FiveMonthlyValues_GetTheirDates` |
| Figures 1-3 and the ACF plot, native exports | TSD `Plots_DrawTheStoredSeries_AsRmcBestFitDid` |
| Figures 1-3, plot data | `Figures_AreWhatTheViewsShow` |

## 2-input-data/1-block-maximum/usgs-block-max-example.md

Class `WalkthroughBlockMaximumTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = Block Series, the Moose River source, Block Function = Maximum, Time Block = Calendar Year; Data Frame on the left, Exact Data above the grid, Chronology Plot and Frequency Plot below the plot | `FindTheControls_TheExplorerPropertiesAndViewAreTheWalkthroughs` |
| The source: 29,050 daily means 1947-01-01 to 2026-07-14, cfs, none missing; its native export (`block-max-daily-discharge-native`) | `Source_IsTheDailyRecord` |
| Saved samples: 80 observations each, indexed 1947-2026, no low outliers; the Multiple Grubbs-Beck Test on, threshold 0; Time Block Water Year; Start/End Month 10 and 9 | `SavedSamples_AreTheWalkthroughs` (the months on the element: the calendar year shows neither row, the water year its Start Month) |
| Step 4: "an October event belongs to the following water year" | **differs**: see Discrepancies; `SavedSamples_AreTheWalkthroughs` checks what RMC-BestFit and XGoodFit show |
| Step 5: 16 of the 80 paired magnitudes differ | `TheTwoSamples_DifferIn16Years` (paired in order and by water year) |
| Step 6 and the figures, native exports | IDV `Plot_HasTheNativeScreenshotsData` |
| Figures, plot data | `Figures_AreWhatTheViewsShow` |
| Step 7: a new input with the source, maximum and water year; Process | `RepeatTheExtraction_OnANewInput_GivesTheSavedSample` |

## 2-input-data/2-usgs-peak-discharge/usgs-peak-download-example.md

Class `WalkthroughUsgsPeakDischargeTests`.

| Claim | Test |
|---|---|
| Explorer capture (two inputs, no time series) and properties capture; Data Entry Method = USGS Peak Discharge, Site Number = 01134500, the Multiple Grubbs-Beck Test on; the generic Value label on both plots | `FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs` |
| Saved samples: Moose 79 rows 1947-2025, 0 flags, 0 cfs; Orestimba 94 rows 1932-2025, 47 flags, 1,270 cfs; the flags stay in the exact series (no threshold series); zero magnitudes can't show on the log axis | `SavedSamples_AreTheWalkthroughs` |
| Figures, native exports | IDV `Plot_HasTheNativeScreenshotsData` |
| Figures, plot data | `Figures_AreWhatTheViewsShow` |
| Step 7: a new input, the station number, Download | `NewDownload_IsSetUp`; `NewDownload_DownloadsThePeaks` (External) |

## 2-input-data/3-peaks-over-threshold/ghcn-peaks-over-threshold-example.md

Class `WalkthroughGhcnPeaksOverThresholdTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Data Entry Method = Peaks-Over-Threshold Series, Time Series = GHCN-USC00040741-Precipitation, Threshold 1.0, Minimum Steps 5, Smoothing None (Period = 2 inactive); the GHCN spelling | `FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs` |
| The source: 24,106 daily values 1960-07-01 to 2026-06-30, 482 missing, inches; its native export | `Source_IsTheDailyPrecipitation` |
| 233 events indexed 1960-2026, several a year; λ = 3.4776119403 = 233 / 67; exposure 67 years; the POT Diagnostics' three plots | `SavedSample_AndItsDiagnostics_AreTheWalkthroughs` (the exposure on the data frame: the input's views don't show it) |
| Chronology, frequency and mean residual life, native exports | IDV `Plot_HasTheNativeScreenshotsData` |
| Figures (chronology, frequency, mean residual life, modified scale), plot data | `Figures_AreWhatTheViewsShow` |
| Step 7 (alternative thresholds) | not checked: an open-ended exercise |

## 2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.md

Class `WalkthroughUsgsPeaksOverThresholdTests`.

| Claim | Test |
|---|---|
| Explorer and properties captures; Threshold 650, Minimum Steps 5, Smoothing None (Period = 1 retained) | `FindTheControls_TheExplorerAndPropertiesAreTheWalkthroughs` |
| The source: 34,438 daily means 1932-04-01 to 2026-07-14, none missing, zero-flow days; its native export | `Source_IsTheDailyDischarge` |
| 78 events indexed 1937-2025 (89 years), several sharing a year; the POT Diagnostics' three plots | `SavedSample_AndItsDiagnostics_AreTheWalkthroughs` |
| "Saved Lambda 0.8764044944 (78 / 89)", "explicit source-exposure field absent" | **differs**: see Discrepancies; the test checks the shipped file's 78 / 95 |
| Chronology, frequency, mean residual life, modified scale and shape, native exports | IDV `Plot_HasTheNativeScreenshotsData` |
| Figures (chronology, frequency, mean residual life, shape), plot data | `Figures_AreWhatTheViewsShow` |

## 5-bivariate-distribution-analysis/1-bivariate-distributions/bivariate-distribution-examples.md

Class `WalkthroughBivariateDistributionsTests`.

| Claim | Test |
|---|---|
| Twelve inputs of 100 exact values indexed 1-100, Value units, nothing else | `Inputs_Are100ExactValuesEach` |
| Marginal fits table: twelve Normal fits, their inputs, saved mean and SD | `MarginalFits_AreNormal_WithTheSavedParameters` |
| Copula table: family, saved dependence parameter, marginals, InferenceFromMargins; one saved XY query near probability one | `Copulas_HaveTheirFamiliesMarginalsAndParameters` |
| Saved diagnostics: DE-MCzs, seed 12345, 1,750 warmup, 3,500 iterations, 10,000 output draws, 90%; each fit's chains/thinning, point parameters, DIC, max R-hat, min ESS | `SavedDiagnostics_AreTheWalkthroughs` |
| Steps 2-3: X-Y Plot, Scatter Plot/Value, Probability (marginal-CDF space), Density Contour (negative log-density labels), Probability Contour | `XYPlot_PlotAndAxisTypes_AreTheWalkthroughs` |
| AMH parameter views: Kernel Density of Dependency (θ), Markov Chain Traces, MCMC Report | `AmhParameterViews_ShowTheDependency`; the exports BAV `DiagnosticTabs_ShowTheAnalysisPosterior_AsRmcBestFitDid` |
| AMH scatter export | BAV `CopulaPlot_DrawsTheSavedFits_AsRmcBestFitDid` |
| Figures: the six families' CDF scatters and the Normal trace | `Figures_AreWhatTheViewsShow` (AMH's also BAV) |
| Figures: Normal density and joint-exceedance contours | BAV `CopulaPlot_DrawsTheSavedFits_AsRmcBestFitDid` |
| Step 5, "Interpretation" | not checkable (interpretation) |

## 5-bivariate-distribution-analysis/2-coincident-frequency/sum-two-normals.md

Class `WalkthroughSumTwoNormalsTests`.

| Claim | Test |
|---|---|
| Nine inputs (X, Y, X+Y per scenario) of 100 exact values indexed 1-100 | `Inputs_Are100ExactValuesEach` |
| Scenario table: X and Y means and SDs, fitted rho, the marginals' and copulas' inputs | `MarginalsAndCopulas_HaveTheSavedParameters` |
| Scenario table's bins (20, 50, 50); 7x7 grids of X+Y on the walkthrough's ordinates, 87-273; steps 2-5 (tabs, General and Output properties, 95% and 90% intervals); the CFA rows; no MCMC run of its own; the wrappers' zero DIC | `CoincidentFrequencyAnalyses_AreTheWalkthroughs`; the rows' data also CFV `ResultsTables_ShowTheProbabilitiesAndStatistics_InUsFormats`, the near-zero grid CFV `ResponseGrid_ShowsTheSurface_AndEditsWriteToTheElement_WithUndo` |
| Saved diagnostics (nine fits) | `SavedDiagnostics_AreTheWalkthroughs` |
| Normal Copula - Rho = 0.0's scatter export; the negative and positive scatter figures | `CopulaScatters_AreRmcBestFitsAndTheFigures` |
| The three response figures and the near-zero export | CFV `FrequencyPlot_EqualsRmcBestFitsExports_AndTheWalkthroughsFigures` |

## 5-bivariate-distribution-analysis/2-coincident-frequency/waimea-river-stage-frequency.md

Class `WalkthroughWaimeaRiverStageFrequencyTests`.

| Claim | Test |
|---|---|
| The four inputs: counts, index spans, units, Waimea's three low flags and two perception windows (start, end, 37,100 cfs and none above as the Threshold Data grid shows them; the numbers below, 25 and 20, from the saved threshold series, which no column shows, upstream's grid either) | `Inputs_AreTheWalkthroughs` |
| Seven LP3 alternatives; the RR alternatives' three LogNormal quantile priors (the properties' Quantile Priors table, Enable Priors on Quantiles); the RSkew alternatives' Use Default Flat Priors off, N (-0.157, 0.46) on the log skew, U (0, 6) and U (0, 2) on the mean and scale (the MCMC Report's prior configuration), and Jeffreys' rule | `MarginalAlternatives_AndTheirPriors_AreTheWalkthroughs` |
| Copula table; 52 and 51 eligible pairs; AMH near its bound; the query at (0, 0) | `Copulas_HaveTheirFamiliesMarginalsParametersAndPairs` |
| The conditional CFA: its copula, empty input binding, 10x5 grid spanning 7.14-28.15, 50 outputs, 90% from 10,000 draws, the saved rows | `ConditionalCfa_IsTheWalkthroughs` |
| Saved diagnostics (fourteen fits) | `SavedDiagnostics_AreTheWalkthroughs` |
| 1% AEP quantiles and 90% bounds (seven fits) | `OnePercentQuantiles_AreTheWalkthroughs` |
| Ten legacy Bayesian Estimation Analysis records; the fitting analysis's 15 visible fits | `LegacyMaterial_IsRetained` |
| Figures: Waimea chronology, the priors and conditional marginals' curves, the conditional CDF scatter; the conditional scatter export | `Figures_AreWhatTheViewsShow` |
| Figure: the conditional response, and its export | CFV `FrequencyPlot_EqualsRmcBestFitsExports_AndTheWalkthroughsFigures` |
| Figure: the conditional joint-exceedance contours (CDF axes) | not compared: the contours are drawn on a racy 100x100 grid (IssuesToRaise: bivariate contour levels race); BAV checks contours on the Normal example |
| Study context (units, provenance, the response's meaning) | not checkable (the walkthrough says it's unresolved) |

## Discrepancies

| Walkthrough | Claim | What RMC-BestFit and XGoodFit show | Kind |
|---|---|---|---|
| Block maximum, "Saved configuration" and step 4 | Water-year blocks "named by ending year"; "an October event belongs to the following water year" | The saved water-year sample (and XGoodFit's re-extraction) indexes each maximum by its event's calendar year: the seven October-December maxima (1959-11-29, 1973-12-22, 1990-12-24, 1996-12-03, 2003-10-28, 2005-10-17, 2021-11-01) keep their water year's starting year, so seven indexes repeat (1959, 1973, 1990, 1996, 2003, 2005, 2021) and seven are missing (1960, 1974, 1991, 1997, 2004, 2006, 2022). Cause: `DataFrame.CreateBlockSeries` builds `new ExactData(date, value)`, whose constructor sets the index to `date.Year` (`RMC.BestFit/Models/DataFrame/DataTypes/ExactData.cs` line 32). | upstream: the block's index or the walkthrough |
| Orestimba POT, "Saved configuration" | Saved Lambda 0.8764044944 (78 / 89); no explicit source-exposure field | The shipped file (saved 2026-09-29) holds Lambda 0.82105263157894737 and PointProcessObservationYears 95 (1932-2026), and the summary statistics show λ = 0.8211; the walkthrough's Python figures were drawn from an earlier file (their `source.runId` isn't the shipped file's hash) | walkthrough (stale) |
| Manual entry, Figure 2 | "Monthly Mauna Loa CO2 concentrations, in ppm" (the figure's value axis: CO2 (ppm)) | The saved plot's value axis is titled "CO2"; the figures and native exports come from factory-reset plots | known (IssuesToRaise: example figures differ from the app) |

## The classes' runs

On the Windows laptop at `c04bebb`, each class in its own process (`bash scripts/run-app-tests.sh Release <Class>`), with
its peak private memory. Skipped: the opt-in Large tests.

| Class | Tests | Duration | Peak memory |
|---|---:|---:|---:|
| WalkthroughUsgsDownloadTests | 6 (1 Large) + 1 External | 41 s | 1,165 MB |
| WalkthroughGhcnDownloadTests | 5 + 1 External | 20 s | 541 MB |
| WalkthroughChmnDownloadTests | 5 + 1 External | 39 s | 1,336 MB |
| WalkthroughAbomDownloadTests | 6 (1 Large) + 1 External | 32 s | 828 MB |
| WalkthroughHecDssImportTests | 5 | 31 s | 558 MB |
| WalkthroughManualEntryTests | 6 | 24 s | 504 MB |
| WalkthroughBlockMaximumTests | 6 | 25 s | 415 MB |
| WalkthroughUsgsPeakDischargeTests | 4 + 1 External | 13 s | 218 MB |
| WalkthroughGhcnPeaksOverThresholdTests | 4 | 15 s | 354 MB |
| WalkthroughUsgsPeaksOverThresholdTests | 4 | 17 s | 448 MB |
| WalkthroughBivariateDistributionsTests | 7 | 66 s | 1,256 MB |
| WalkthroughSumTwoNormalsTests | 5 | 41 s | 786 MB |
| WalkthroughWaimeaRiverStageFrequencyTests | 8 | 54 s | 937 MB |

The two Large tests passed together in 64 s (about 4 GB in one process). The External tests passed for USGS (both),
NOAA and the Water Survey of Canada; the Bureau of Meteorology's API answered HTTP 500 ("Error connecting to WDP") on
2026-10-31, which the ABOM test reports as the application's error message.

A window that keeps one example open for a whole class lets the documents its tests close go only if they're released:
`WalkthroughSession.CloseDocuments` does so for two upstream leaks and a docking one (see its comments), and without it the
CHMN class peaked at 2.3 GB.
