# With low outliers, Distribution Fitting can't reach the Exponential's maximum: its location is capped at the smallest flow, outliers included

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `RMC.BestFit` (`Models/UnivariateDistribution/UnivariateDistribution.cs`), with [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics)' `Exponential.GetParameterConstraints`. Found in RMC-BestFit 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) and Numerics 2.2.0 at [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0), both still `main` on 2026-10-31.

**Type:** wrong result · **Severity:** medium

**Verification:** reproduced with upstream's own code: RMC-BestFit 2.0.1's WPF element layer, built from this commit, estimating the shipped example again on Windows (and the model library on Linux, with the same result), and a program evaluating RMC.BestFit's own log-likelihood.

## Summary

A Distribution Fitting Analysis bounds each candidate's parameters with the distribution's `GetParameterConstraints`, computed from every exact value of the Input Data, low outliers included. For the two-parameter Exponential that caps the location ξ at the smallest flow. But the likelihood censors the low outliers (they count only as "below the low-outlier threshold"), so its maximum can lie above that flow, out of the search's reach. The fit then stops at the cap, and the reported Exponential fit, quantiles and AIC aren't the maximum-likelihood ones.

## Details and how to reproduce

The example `5-bivariate-distribution-analysis/2-coincident-frequency/waimea-river-stage-frequency.bestfit`, Distribution Fitting Analysis "16031000_WaimeaPk", on Input Data "16031000_Waimea_Peaks": 64 annual peaks, of which three (1,470, 2,780 and 3,080 cfs) are low outliers below the threshold of 4,740 cfs, plus two perception thresholds. Estimate it again:

| Exponential fit | ξ | α | log-likelihood (2.0.1's) | AIC | 1% AEP flow | 1e-6 AEP flow |
|---|---|---|---|---|---|---|
| Saved in the example (an earlier build) | 4,265.92 | 9,631.00 | −639.329 | 1,282.66 | 48,618 cfs | 137,323 cfs |
| RMC-BestFit 2.0.1, estimated again | 1,470.00 | 11,844.86 | −649.345 | 1,302.69 | 56,018 cfs | 165,113 cfs |

The log-likelihoods are RMC.BestFit 2.0.1's own `UnivariateDistribution.DataLogLikelihood` for these data, evaluated at both parameter sets. With the scale at its best for each location, the profile log-likelihood rises steadily from ξ = 0 (−655.3) to ξ ≈ 4,266 (−639.33), and is −∞ above the smallest retained flow (4,740). Nelder–Mead started from the saved fit stays there. So the saved fit is the maximum, and 2.0.1's is on the bound: 2.0.1's differential evolution, and its Nelder–Mead and MLSL from the default start, all stop at ξ = 1,470.00, 10 log-likelihood units lower. The 1% AEP flow is 15% too high, and the Exponential drops from 8th to 15th of the 15 candidates by AIC. The best candidate (Kappa-4) and every other distribution's fit are unchanged, to 1e-9 in AIC.

## Why this is an issue and not intended behaviour

The analysis reports maximum-likelihood fits, and ranks them by AIC. The likelihood deliberately treats low outliers as censored, so the parameter space it defines allows a location above them; the bound contradicts the likelihood it constrains. The example's own saved fit shows that an earlier build reached the maximum.

## Where in the source

- [`UnivariateDistribution.cs` lines 631-633](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs#L631-L633): `SetDefaultParameters` passes every exact value, low outliers included, to the constraints.
  ```csharp
  TryGetDefaultParameterConstraints(Distribution, DataFrame.ExactSeries.Select(x => x.Value).ToList(),
      out var tuple, out _defaultParameterInitializationError))
  ```
  The bounds become each parameter's `LowerBound`/`UpperBound` (and the uniform prior's support, [lines 666-671](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs#L666-L671)), which `MaximumLikelihood` uses as the optimizer's bounds ([`MaximumLikelihood.cs` lines 198-199](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Estimation/MaximumLikelihood.cs#L198-L199)).
- Numerics' [`Exponential.cs` lines 349-356](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Distributions/Univariate/Exponential.cs#L349-L356) (and the robust fallback at lines 388-394): the location's upper bound is the sample's minimum.
  ```csharp
  double minData = Statistics.Minimum(sample);
  ...
  upperVals[0] = minData + Tools.DoubleMachineEpsilon;
  ```
- [`FittingAnalysis.cs` lines 351-363](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/DistributionFitting/FittingAnalysis.cs#L351-L363): each candidate's `SetDefaultParameters`, then its fit.

## Suggested fix

Compute the default constraints from the values the likelihood treats as exact: leave out the low outliers (`IsLowOutlier`, or values below `LowOutlierThreshold`), or, for a location bounded by the data, cap it at the smallest value that contributes a density. Check the other distributions whose constraints bound a parameter by the sample's minimum or maximum the same way (only the Exponential changed in the shipped examples). A Bayesian analysis takes its default uniform priors from the same bounds, so its posterior would be truncated the same way; no shipped example shows it.

## How XGoodFit handles it

XGoodFit compiles RMC.BestFit and Numerics unchanged, so its fit is RMC-BestFit 2.0.1's, bit for bit. Its reproducibility check ([`docs/reproducibility.md`](https://github.com/jonklaric/XGoodFit/blob/main/docs/reproducibility.md)) reports this fit as differing from the saved one "since saved": RMC-BestFit 2.0.1's own rerun differs from the example the same way.
