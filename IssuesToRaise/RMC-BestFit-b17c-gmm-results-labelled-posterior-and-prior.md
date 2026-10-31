# Bulletin 17C labels its GMM results "Posterior" and its penalties "Quantile Prior"

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.xaml.cs`, `HistogramControl.xaml.cs` and `src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs`, and the figure `examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/images/bulletin-17c-examples-parameter-ensemble.*`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source and the examples' figure data; not run.

## Summary

A Bulletin 17C analysis is a frequentist fit: GMM, with a multivariate-normal or bootstrap ensemble for the uncertainty, and penalties rather than priors. Its view mostly says so. The shared diagnostic controls run in their `SimpleView` mode, which drops "Posterior" from the plot titles ("Marginal Density", "Marginal Histogram", "Joint Density") and hides the prior option. But two labels still use Bayesian names:

1. The **Kernel Density** and **Histogram** tabs' series are titled "Posterior Density" and "Posterior Histogram", in the legend and the tracker, because `SimpleView` changes only the plot titles.
2. The frequency plot titles the quantile penalties' series "Quantile Prior".

RMC-BestFit's own example figures correct both: the exporter relabels the B17C density as a sampling distribution, and the information-expansion figure names the series "Quantile Penalty". The parameter-ensemble figure, though, titles its series "Uncertainty Histogram" with no display correction recorded, where the application shows "Posterior Histogram".

## Details and how to reproduce

1. Open "Example #1" in `examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.bestfit` and select the **Kernel Density** tab. The plot is titled "Marginal Density of Mean (of log) (µ)"; its legend says "Posterior Density".
2. Select the **Histogram** tab: "Marginal Histogram of …", legend "Posterior Histogram".
3. Open an analysis with quantile penalties enabled, for example "Systematic + Historical + Paleo + Reg Skew + QPen" in `2-information-expansion/blakely-mountain-dam-b17c.bestfit`: the frequency plot's legend says "Quantile Prior".

The examples' figure data:

| Figure | Series as exported | Display corrections recorded |
|---|---|---|
| `1-bulletin17C-examples/screenshots/bulletin-17c-kernel-density-mean` (native export) | "Sampling Density" | "B17C MVN density labelled as a sampling distribution, not a posterior." |
| `2-information-expansion/images/blakely-mountain-dam-b17c-penalty-frequency` | "Quantile Penalty" | "GMM quantile information is labeled Quantile Penalty rather than Quantile Prior." |
| `1-bulletin17C-examples/images/bulletin-17c-examples-parameter-ensemble` | "Uncertainty Histogram", plot title "Marginal Uncertainty Histogram of Mean (of log) (µ)" | none |

The walkthrough's caption for the last says "The legacy plot selector is named posterior; the draws are not an MCMC posterior."

## Why this is an issue and not intended behaviour

- A GMM fit has no posterior and no prior. The view's authors meant to say so: `SimpleView` means "simplified (non-Bayesian) terminology … for B17C bootstrap/GMM contexts where the parameter samples are not posterior draws" ([`BivariateHeatMapControl.xaml.cs` lines 78-81](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BivariateHeatMapControl.xaml.cs#L78-L81)), the properties panel calls the settings "penalties", and the exporter's comment says B17C "uses a frequentist MVN sampling ensemble … Correct only the disposable figure labels, never its values or the application."
- The figures and the application now disagree, so users comparing them see different names for the same series. The parameter-ensemble figure's "Uncertainty" name also differs from what both the exporter's correction ("Sampling") and the application ("Posterior") would produce.

## Where in the source

- The density series: [`src/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.xaml.cs` lines 383-394](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.xaml.cs#L383-L394) (`Title = "Posterior Density"`, line 387), while the plot title follows `SimpleView` at [line 358](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.xaml.cs#L358):
  ```csharp
  string densityLabel = SimpleView ? "Marginal Density" : "Marginal Posterior Density";
  ```
- The histogram series: [`HistogramControl.xaml.cs` lines 382-386](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/HistogramControl.xaml.cs#L382-L386) (`Title = "Posterior Histogram"`); its plot title at [line 357](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/HistogramControl.xaml.cs#L357).
- B17C hosts both with `SimpleView="True"`: [`src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml` lines 174, 178 and 182](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml#L174-L182).
- The penalties' series: [`B17CAnalysisControl.xaml.cs` lines 498-508](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L498-L508) (`Name = "QuantilePrior"`, `Title = "Quantile Prior"`), filled from `Element.QuantilePenalties` at [lines 622-629](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.xaml.cs#L622-L629) under the comment "Quantile penalties".
- The exporter's correction: [`tools/PlotReferenceExporter/Program.cs` lines 209-217](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/tools/PlotReferenceExporter/Program.cs#L209-L217).
- The parameter-ensemble figure: [`examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/images/bulletin-17c-examples-parameter-ensemble.plotspec.json.gz`](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/images/bulletin-17c-examples-parameter-ensemble.plotspec.json.gz) (`series[0].name`, `displayCorrections: []`), shown at [`bulletin-17c-examples.md` line 98](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md#L98).

## Suggested fix

1. Let `SimpleView` title the series too, for example "Density" and "Histogram", or "Sampling Density" and "Sampling Histogram" to match the exporter.
2. Title B17C's series "Quantile Penalty". Its `Name` can stay `QuantilePrior`, so saved plot settings still find it; the update path would need to set the title each time, since a saved title is reused.
3. Then drop the exporter's relabelling, and regenerate the parameter-ensemble figure so its series has the application's name.

Set the titles on every update, not only when a series is created, so plots saved with the old titles change too. Values and files are unaffected.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's labels for now: [`src/BestFit/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.axaml.cs` line 432](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/KernelDensityControl.axaml.cs#L432), [`HistogramControl.axaml.cs` line 431](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/HistogramControl.axaml.cs#L431) and [`B17CAnalysisControl.axaml.cs` line 555](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.App/GUI/UnivariateAnalysis/B17C/B17CAnalysisControl.axaml.cs#L555).
