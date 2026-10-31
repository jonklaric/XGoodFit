# Walkthrough coverage: univariate distribution analysis

RMC-BestFit's walkthroughs under `sample-data/RMC-BestFit/examples/4-univariate-distribution-analysis/` say what to open,
change and run, and what the user then sees. This file maps each checkable claim of the fourteen walkthroughs in that
folder to the XGoodFit test that checks it.

## How the walkthrough tests work

- One test class per walkthrough, `tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/Walkthrough<Name>Tests.cs`, with the
  shared helpers in `UnivariateWalkthroughTesting.cs` (`WalkthroughExample`, `UnivariateWalkthroughTesting`).
- Each class opens a copy of its example once, in the main window (`BestFitShell`), and each test double-clicks the
  elements it reads in the project explorer, which opens their documents with their properties beside them, then reads
  what those show: grid cells, report text, property boxes, check boxes, plot series.
- The window uses three decimal digits for values (Tools > Options > Defaults), the walkthroughs' rounding, so the
  frequency tables show the walkthroughs' numbers as they write them: those are compared as text. Values a view shows
  with other precision are compared within both roundings (`AssertAgrees`): R-hat with the MCMC Report's four decimals,
  parameter summaries with its six significant digits, DIC with the summary statistics' four decimals. Tests run in
  en-US (the examples' culture); dates shown with ICU's narrow no-break space before AM/PM are normalised.
- Run a class with `bash scripts/run-app-tests.sh Release <Class>`; the walkthrough classes form the opt-in
  `Walkthroughs` group. No test downloads anything.
- The area tests already compare many of the walkthroughs' figures point for point with RMC-BestFit's native exports
  and the figures' plot data, on views hosted outside the main window. Those figures are mapped to them ("Area:"),
  and the walkthrough classes compare the rest through the main window.
- Values the app doesn't show (a trend model's start index, a GMM's optimizer settings, a mixture's fixed zero mass,
  a stationary point process's block start) are read from the element's model, and the table says so.

Abbreviations: **UAV** `UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures`;
**B17V** `B17CAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures`; **SAC**
`SharedAnalysisControlsTests.DiagnosticPlots_DrawTheStoredResults_AsRmcBestFitDid`; **PPV**
`PointProcessAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures`; **MXV**
`MixtureAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures`; **CMV**
`CompositeAnalysisViewTests.FrequencyPlot_EqualsRmcBestFitsExport_AndTheWalkthroughsFigures`.

Claims about method, interpretation and study provenance ("a credible band is not a band containing 90% of future
floods", "check your understanding") aren't claims about what the app shows and aren't listed.

## Blakely Mountain Dam: Bayesian study reference

`1-univariate-analysis/1-information-expansion/blakely-mountain-dam-bayesian.md`. The page says its project isn't in the
examples and gives no results, settings or figures: nothing to check. Its one-day companion is covered below.

## Kamp at Zwettl: historical evidence and quantile priors

`1-univariate-analysis/1-information-expansion/viglione-et-al-2013.md`, `WalkthroughViglioneTests`.

| Claim | Test |
| --- | --- |
| Input table: each input's exact rows and index span (51, 1951–2001; 55, 1951–2005), uncertain, interval, window and low-flag counts | `Inputs_HoldTheWalkthroughsRecords` |
| Units: m³/s, and the label Value the 1951–2005 inputs retain | `Inputs_HoldTheWalkthroughsRecords` |
| Step 1: the 2002 flood in the 1951–2005 record | `Inputs_HoldTheWalkthroughsRecords` |
| Intervals 180–300 (1655), 240–400 (1803), 202.5–337.5 (1829) | `Inputs_HoldTheWalkthroughsRecords` |
| Window 1600–1950 at 300 m³/s, 348 below, none above | `Inputs_HoldTheWalkthroughsRecords` (the grid shows start, end, threshold and number above; the years below are the window's years less the entered events and those above) |
| The models are GEV (not LP3) | `Analyses_ShowTheSavedResultsAndSettings` |
| Step 3: Posterior Mode for 1951–2005, Posterior Mean for 1951–2001 | `Analyses_ShowTheSavedResultsAndSettings` |
| Step 5: the three-prior fit's 95% interval and 1,200/2,400 warmup/iterations; 90% and 1,750/3,500 for the other eight | `Analyses_ShowTheSavedResultsAndSettings` |
| DE-MCzs, six chains, thinning 30, seed 12345, output length 10,000, Jeffreys' rule | `Analyses_ShowTheSavedResultsAndSettings` (sampler and seed in the MCMC Report, the rest in Properties) |
| Results table: 1% AEP point and limits, width, largest R-hat, smallest ESS (nine rows) | `Analyses_ShowTheSavedResultsAndSettings` |
| "Nine stationary GEV analyses" | `Analyses_ShowTheSavedResultsAndSettings`: the project holds a tenth, Univariate Analysis_10 (documentation issue) |
| Parameter summaries of MCMC - Systematic (1951-2001) | `FirstAnalysis_ReportsTheParameterSummaries` (MCMC Report and the Kernel Density summary) |
| Select Parameter: Location (ξ) in Kernel Density, Markov Chain Traces, Autocorrelation | `FirstAnalysis_ReportsTheParameterSummaries` |
| Causal alternatives: Normal quantile prior at AEP 0.002, mean 480, SD 80; the three-prior alternative's three; none in the others (step 4) | `QuantilePriors_AreTheWalkthroughsInformation` |
| Step 6: the four Fit - elements fit the inputs they name | `FitElements_FitTheNamedInputs` |
| Figure: historical chronology | `Figures_ShowTheChronologyAndTheTrace` |
| Figure: trace companion (Location, MCMC - Systematic (1951-2001)) | `Figures_ShowTheChronologyAndTheTrace` |
| Figures: frequency of 1951–2001 (native and companion), 1951–2005, Temporal + Causal, three priors | Area: UAV |
| Native exports: kernel density, trace, autocorrelation (Location) | Area: UAV, SAC |

## ARR and FLIKE

`1-univariate-analysis/2-arr-flike/arr-flike-examples.md`, `WalkthroughArrFlikeTests`.

| Claim | Test |
| --- | --- |
| Input table: Examples #3, #4 (31, 1938–1968), #6a, #6b (56, 1960–2015), windows, 27 low flags in #6b; m³/s | `Inputs_HoldTheWalkthroughsRecords` |
| Step 2: Example #4's window 1820–1937 at 12,525 m³/s, 117 below, one above | `Inputs_HoldTheWalkthroughsRecords` |
| Five Bayesian alternatives, no B17C alternatives | `Analyses_ShowTheSavedResultsAndSettings` |
| Examples 3–5 LP3, 6a/6b GEV; Example #5 uses input Example #3 | `Analyses_ShowTheSavedResultsAndSettings` |
| Results table (five rows) and the saved settings (DE-MCzs, 6/30, seed, 10,000, 1,750/3,500, Jeffreys) | `Analyses_ShowTheSavedResultsAndSettings` |
| Example #3's parameter summaries; Select Parameter: Mean (of log) (µ) | `FirstAnalysis_ReportsTheParameterSummaries` |
| Step 3: Example #5's skew prior Normal(0, 0.30), default flat priors off; the others default | `Example5_HasTheRegionalSkewPrior` |
| Figure: trace companion | `Figures_ShowTheTrace` |
| Figures: Examples 3, 4, 5, 6a, 6b frequency; native frequency, kernel density, trace, autocorrelation | Area: UAV |

## Bulletin 17C datasets: Bayesian fits and a GMM comparison

`1-univariate-analysis/3-bulletin17C-examples/bulletin-17c-bayesian-examples.md`, `WalkthroughBulletin17CBayesianTests`.

| Claim | Test |
| --- | --- |
| Input table: the seven datasets' rows, spans, intervals, windows and low flags; cfs | `Inputs_HoldTheWalkthroughsRecords` |
| Step 2: Orestimba's 30 flags and retained low-outlier threshold | `Inputs_HoldTheWalkthroughsRecords` |
| American River's five intervals (650, 1437, 1574, 1711, 1862) | `Inputs_HoldTheWalkthroughsRecords` |
| Results table, Bayes Examples #2, #4–#7, and the saved settings | `Analyses_ShowTheSavedResultsAndSettings` |
| Results table, Bayes Example #1 (5,231.877; 4,248.95–6,902.67; R-hat 0.99988; ESS 9,300) | Not the saved fit: the test checks what the app shows (4,806.606; 4,139.069–5,777.893; 1.0009; 9,515), as RMC-BestFit 2.0.1 shows and exported. Documentation issue |
| Bayes Example #1's parameter summaries | `FirstAnalysis_ReportsTheParameterSummaries`, with the saved fit's values (the walkthrough's are the earlier fit's) |
| Select Parameter: Mean (of log) (µ) | `FirstAnalysis_ReportsTheParameterSummaries` |
| Step 5: Bayes Example #2's custom uniform priors (about 0–4, 0–3, −6–6) with Jeffreys' rule | `BayesExample2_HasCustomUniformPriors` |
| "The other five keep default-flat-prior mode" | `BayesExample2_HasCustomUniformPriors`: #4–#7 do; #1 has a Normal(0, 0.3) skew prior (documentation issue) |
| The saved Bayes Example #1 is what its settings give | `BayesExample1_EstimateGivesTheSavedFit` (Estimate, about 10 s) |
| Step 4: B17C Example #3 is selectable, the app doesn't load its legacy fit; the figure's observations | `BackCreekGmm_ShowsTheObservationsWithoutItsLegacyFit` |
| B17C Example #3's legacy values (22,479.428; 17,349.951–33,555.869; Nelder–Mead; 10,000 MVN draws; seed) | Not covered: neither RMC-BestFit 2.0.1 nor XGoodFit loads them (`IssuesToRaise/RMC-BestFit-b17c-example-3-loses-results-on-save.md`); file compatibility keeps the cells |
| Figure: American River chronology | `Figures_ShowTheChronology` |
| Figure: trace companion | Not compared: it shows the earlier Bayes Example #1 (first draw 3.32834; the saved chains and the native export, which the area tests compare, have 3.35128). Documentation issue |
| Figure: Moose River frequency | Not compared, for the same reason (Area: UAV excludes it, `InconsistentFigures`) |
| Figures: Orestimba, American River frequency; native frequency, kernel density, trace, autocorrelation | Area: UAV |

## Sinnemahoning Creek: Bayesian fits with an uncertain MOVE.3 extension

`1-univariate-analysis/4-measurement-errors/sinnemahoning-move3-bayesian.md`, `WalkthroughSinnemahoningBayesianTests`.

| Claim | Test |
| --- | --- |
| Input table: 104 (1914–2017), 79 + 25 uncertain (1939–2017), 79 (1939–2017); cfs | `Inputs_HoldTheWalkthroughsRecords` |
| Steps 1–3: the 25 extension years 1914–1938 in No Errors' exact data and With Errors' uncertain data, each LogNormal with log10 sigma 0.074 | `Inputs_HoldTheWalkthroughsRecords` |
| Step 4 and the results table: posterior mean, 90%, 1% AEP and limits, R-hat, ESS; the saved settings | `Analyses_ShowTheSavedResultsAndSettings` |
| LPIII - No Extension's parameter summaries; Select Parameter: Mean (of log) (µ) | `FirstAnalysis_ReportsTheParameterSummaries` |
| Figures: uncertain chronology; trace of LPIII - With Errors | `Figures_ShowTheChronologyAndTheTrace` |
| Figures: no-extension, exact-extension, uncertain-extension frequency; native exports | Area: UAV |

## Brays Bayou: conditional flood frequency

`1-univariate-analysis/5-nonstationary-ffa/nsffa-brays-bayou-texas.md`, `WalkthroughBraysBayouTests`.

| Claim | Test |
| --- | --- |
| Input table: four inputs of 90 peaks during 1929–2024, one window each; cfs and m³/s | `Inputs_HoldTheWalkthroughsRecords` |
| The 1930–1935 gap as six years below 35,100 cfs (994 m³/s) | `Inputs_HoldTheWalkthroughsRecords`: the second copy's threshold is 200,000 cfs (documentation issue) |
| All seven fits use the metric input; LP3 | `Analyses_HaveTheWalkthroughsTrendModels` |
| Step 2: the trends are on the mean, SD and skew of log flow, in that order | `Analyses_HaveTheWalkthroughsTrendModels` |
| Trend table: each alternative's trend types (StepFunction is shown as Step Function), evaluation index 2024 | `Analyses_HaveTheWalkthroughsTrendModels` |
| Trend start index 1929 | `Analyses_HaveTheWalkthroughsTrendModels` (not shown: read from the model) |
| No quantile priors; default flat priors; Jeffreys' rule; Constant uses the nonstationary wrapper; Alpha 0.5 | `Analyses_HaveTheWalkthroughsTrendModels` |
| Results table: chains/thinning/draws, DIC, 1% AEP and limits at 2024, R-hat, ESS; DE-MCzs, seed, posterior mean, 90% | `Analyses_ShowTheSavedResults` |
| Select Parameter: Mean (of log) (µ) (α) | `Analyses_ShowTheSavedResults` |
| Step 5: the model average's three components and DIC weights | `ModelAverage_HasTheThreeComponentsAndTheirWeights` |
| Figure: model average frequency | `ModelAverage_HasTheThreeComponentsAndTheirWeights` |
| Figure: metric input chronology | `Figures_ShowTheInputChronology` |
| Figures: linear and step–logistic chronology, step–logistic frequency; native exports | Area: UAV |

## OC Fisher Dam: trend assumptions and quantile priors

`1-univariate-analysis/5-nonstationary-ffa/nsffa-oc-fisher-dam.md`, `WalkthroughOcFisherTests`.

| Claim | Test |
| --- | --- |
| Input table and step 1: 106 values (1916–2021) in m³/s; window 1853–1915 at 1,150, 62 below, one above | `Input_HoldsTheWalkthroughsRecords` |
| Steps 2–4 and the trend table: trend types, evaluation index 2021 (0 for the stationary SFFA), Alpha 0.5 | `Analyses_HaveTheWalkthroughsTrendsAndPriors` |
| Trend start index 1853 | `Analyses_HaveTheWalkthroughsTrendsAndPriors` (not shown: read from the model) |
| Quantile priors: LnNormal (shown as LN) at 0.1, 0.01, 0.001; SFFA means 275/1014/2330, NSFFA 261/963/2214, SDs 164/550/1252 | `Analyses_HaveTheWalkthroughsTrendsAndPriors` |
| Results table (five rows) and DE-MCzs, seed, posterior mean, 90% | `Analyses_ShowTheSavedResults` |
| Step 5: the sinusoid's frequency about 0.0064835 cycles/year, a period of about 154 years | `Analyses_ShowTheSavedResults` |
| Select Parameter: Mean (of log) (µ) for SFFA | `Analyses_ShowTheSavedResults` |
| Step 6: the model average's components (Linear, Sinusoidal, Step), weights, and empty result | `ModelAverage_HasItsComponentsButNoResult` |
| Figures: historical chronology; sinusoidal trace | `Figures_ShowTheChronologyAndTheSinusoidalTrace` |
| Figures: sinusoidal chronology and frequency, stationary priors; native exports | Area: UAV |

## Synthetic data: recognizing nonstationary trend shapes

`1-univariate-analysis/5-nonstationary-ffa/nsffa-synthetic-data.md`, `WalkthroughSyntheticTrendsTests`.

| Claim | Test |
| --- | --- |
| Nine inputs of 100 exact values at indexes 1–100, Value units, nothing else | `Inputs_HoldTheWalkthroughsRecords` |
| Trend table: Normal fits on their own data, the mean's trend, constant SD, start index 1 (from the model), index 51, Alpha 0.5; no quantile priors; no model average | `Analyses_HaveTheWalkthroughsTrendModels` |
| Results table (nine rows) and DE-MCzs, seed, posterior mean, 90% | `Analyses_ShowTheSavedResults` |
| Cubic Trend's largest R-hat 1.00004 and smallest ESS 9,028 | Not the saved run's: the test checks what the MCMC Report shows (1.0005, 9,523). Documentation issue |
| The cubic fit's saved diagnostics are what its settings give | `CubicTrend_EstimateGivesTheSavedDiagnostics` (Estimate, about 20 s) |
| Step 3: the sinusoid's frequency about 0.0140238 per index | `Analyses_ShowTheSavedResults` |
| Select Parameter: Mean (µ) (α) | `Analyses_ShowTheSavedResults` |
| Figures: nine chronologies, index-51 frequency; native exports | Area: UAV |

## Bulletin 17C datasets: GMM fits and uncertainty methods

`2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md`, `WalkthroughBulletin17CTests`.

| Claim | Test |
| --- | --- |
| Input table: the seven datasets (as in the Bayesian walkthrough); manual low-outlier thresholds of Examples 3 and 5 | `Inputs_HoldTheWalkthroughsRecords` |
| Results table, thirteen saved fits: uncertainty method, draws, status, 1% AEP computed, limits; seed, 90% | `Analyses_ShowTheSavedResults` |
| Iterative GMM, Success, converged within tolerance | `Analyses_ShowTheSavedResults` (GMM Report; convergence from the model) |
| BFGS, at most 100 GMM iterations, tolerances 1e-8 | `Analyses_ShowTheSavedResults` (not shown: read from the model) |
| Step 3: the BCB copies' computed curve is their parents' | `Analyses_ShowTheSavedResults` |
| Step 2: Example #1's skew penalty, mean 0.44, MSE 0.078; no other example's | `Example1_HasTheRegionalSkewPenalty` |
| Select Parameter: Mean (of log) (µ) in Kernel Density | `Example1_HasTheRegionalSkewPenalty` |
| Step 5 and the BCB table: 1,000 of 1,000 replicates, no substitutions or retries, fallbacks, evaluation-limit statuses, clips (six saved BCB fits) | `BootstrapReports_ShowTheWalkthroughsDiagnostics` |
| Example #2 - BCB's row and counters | Not in the project (IsEstimated False, no results or report), as RMC-BestFit shows. `Example2Bcb_HasNoSavedResult_AndItsEstimateIsntTheWalkthroughs`: Estimate gives the computed 13,823.815 but other limits and counters, while Example #1 - BCB re-estimates to its saved fit (documentation issue) |
| Figures: American River chronology; parameter-ensemble histogram (titled Posterior Histogram in the app) | `Figures_ShowTheChronologyAndTheEnsembleHistogram` |
| Figures: Moose and American River MVN, linked-MVN and bootstrap frequency; native frequency and kernel density | Area: B17V |

## Blakely Mountain Dam: one-day inflows

`2-bulletin-17C-analysis/2-information-expansion/blakely-mountain-dam-b17c.md` (imperial project and
`Case Study/blakely-mountain-dam-b17c-metric.bestfit`), `WalkthroughBlakelyB17CTests`.

| Claim | Test |
| --- | --- |
| Three inputs of 91 peaks (1923–2018), the 1931–1935 gap at 110,000 cfs | `ImperialInputs_HoldTheWalkthroughsRecords` |
| The 1882 interval (115,000–150,000, shown 132,500) and 1870–1922 window; the 1020 event (181,000–194,000, shown 187,000) | `ImperialInputs_HoldTheWalkthroughsRecords` |
| Paleo windows: thresholds, years below (3,999, 850, 52, 5), none above | `ImperialInputs_HoldTheWalkthroughsRecords` |
| Metric inputs, the threshold stored as 3,114.87 m³/s | `MetricInputs_HoldTheWalkthroughsRecords` |
| Imperial results (seven), one MaximumFunctionEvaluationsReached; linked MVN, seed, 90% | `ImperialAnalyses_ShowTheSavedResultsAndPenalties` |
| Step 4: Reg Skew penalty −0.17, MSE 0.12; step 5: QPen at 0.0001, log10 mean 5.4689, MSE 0.016 | `ImperialAnalyses_ShowTheSavedResultsAndPenalties` |
| Select Parameter: B17C Mean (of log) (µ) | `ImperialAnalyses_ShowTheSavedResultsAndPenalties` |
| Step 6 and the metric results (twelve), skew penalty, quantile penalty log10 mean 3.920947241 | `MetricAnalyses_ShowTheSavedResultsAndPenalties` |
| Figures: paleo chronology; metric final alternative | `Figures_ShowThePaleoChronologyAndTheMetricFit` |
| Figures: imperial systematic, paleo and penalty frequency; native exports | Area: B17V |

## Sinnemahoning Creek: GMM with an uncertain MOVE.3 extension

`2-bulletin-17C-analysis/3-measurement-errors/sinnemahoning-move3-b17c.md`, `WalkthroughSinnemahoningB17CTests`.

| Claim | Test |
| --- | --- |
| Input table and steps 1–2 (as in the Bayesian walkthrough) | `Inputs_HoldTheWalkthroughsRecords` |
| Results table: linked MVN, 10,000 draws, Success, converged, 1% AEP and limits, seed, 90% | `Analyses_ShowTheSavedResults` |
| The GMM uses the uncertain series (With Errors' fit isn't No Extension's on the same exact peaks) | `Analyses_ShowTheSavedResults` |
| Select Parameter: B17C Mean (of log) (µ) | `Analyses_ShowTheSavedResults` |
| Figure: uncertain chronology | `Figures_ShowTheUncertainChronology` |
| Figures: no-extension, no-errors, with-errors frequency; native exports | Area: B17V |

## Precipitation frequency: annual maxima and point processes

`3-point-process-analysis/point-process-examples.md`, `WalkthroughPointProcessTests`.

| Claim | Test |
| --- | --- |
| Daily records: date ranges, positions, missing positions; inches | `DailyRecords_HaveTheWalkthroughsPositions` |
| Input table: POT 253/439 events, AMS 67/120 values, spans; inches | `Inputs_HoldTheWalkthroughsRecords` |
| POT thresholds 1 and 2.5 in, separation one step, no smoothing; rates 3.776 and 3.658 a year | `Inputs_HoldTheWalkthroughsRecords` |
| AMS water years from October (Big Bear) and September (De Sabla) | `Inputs_HoldTheWalkthroughsRecords` |
| Point processes' inputs, thresholds, exposures 67 and 120 years | `PointProcesses_HaveTheWalkthroughsSettings` |
| Stationary models start in October | `PointProcesses_HaveTheWalkthroughsSettings` (the panel hides the setting for stationary models: read from its hidden combo box) |
| Seasonal model: August start, change points 99.53338 and 246.45066, eight parameters | `PointProcesses_HaveTheWalkthroughsSettings` |
| Results table (five rows): chains/thinning/draws, DIC, 1% AEP and limits, R-hat, ESS | `Analyses_ShowTheSavedResults` |
| Select Parameter: Location (ξ) | `Analyses_ShowTheSavedResults` |
| Figures: Big Bear POT chronology and seasonality | `Figures_ShowThePotChronologyAndSeasonality` |
| Figures: AMS GEV and point-process frequency (both stations), seasonal; native exports | Area: PPV |

## Mixture distributions

`4-mixture-analysis/mixture-distribution-examples.md`, `WalkthroughMixtureTests`.

| Claim | Test |
| --- | --- |
| Input table: 100 values (1–100), positive, ten exact zeros in the zero-inflated data | `Inputs_HoldTheWalkthroughsRecords` |
| Component table: means, SDs and masses in saved order | `Analyses_HaveTheWalkthroughsComponents` |
| 5, 8 and 5 free parameters | `Analyses_HaveTheWalkthroughsComponents` |
| Zero mass fixed at 0.10, continuous masses summing to 0.90 | `Analyses_HaveTheWalkthroughsComponents` (zero mass from the model; the data's ten zeros shown) |
| Default flat priors, Jeffreys' rule, no quantile priors | `Analyses_HaveTheWalkthroughsComponents` |
| Select Parameter: Weight (w₁) | `Analyses_HaveTheWalkthroughsComponents` |
| Settings and results tables: chains/thinning, posterior mean, DIC, R-hat, ESS, 1% AEP and bounds; DE-MCzs, seed, 1,750/3,500, 10,000, 90% | `Analyses_ShowTheSavedResults` |
| Figure: zero-inflated input histogram | `Figures_ShowTheZeroInflatedHistogram` |
| Figures: two-, three-Normal and zero-inflated frequency, three-Normal trace; native exports | Area: MXV, SAC |

## Mixed flood populations

`5-composite-analysis/mixed-population-examples.md`, `WalkthroughMixedPopulationTests`.

| Claim | Test |
| --- | --- |
| Input table: six inputs' counts and spans, cfs and Value | `Inputs_HoldTheWalkthroughsRecords` |
| The full annual maxima are the index-wise maximum of the full components at all 100 rows | `Inputs_HoldTheWalkthroughsRecords` |
| The pooled sample is the union of the 74/26 sub-samples and differs from the full annual maxima at every index | `Inputs_HoldTheWalkthroughsRecords` |
| Step 2 and the marginal table: LogNormal fits, mean and SD parameters | `ComponentFits_ShowTheSavedResults` |
| Settings and results: chains/thinning, posterior mean, DIC, R-hat, ESS, 1% AEP and bounds | `ComponentFits_ShowTheSavedResults` |
| Step 3: Competing Flood Types, independent maximum | `Composites_CombineTheirComponents` |
| Step 4: Mixture of Flood Types' 0.75/0.25 weights | `Composites_CombineTheirComponents` |
| The model-average method is inactive for both; saved curves, no MCMC run; 1% AEP and bounds | `Composites_CombineTheirComponents` |
| Figures: full annual maxima chronology; snow and rain component frequency | `Figures_ShowTheChronologyAndTheComponentFits` |
| Figures: competing maximum (native and companion), fixed mixture | Area: CMV |
