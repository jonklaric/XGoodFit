# Distribution Fitting's maximum-likelihood fits stop short of the maximum, so their parameters are only good to about 1e-4

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `RMC.BestFit` (`Analyses/DistributionFitting/FittingAnalysis.cs`), with [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics)' `DifferentialEvolution`. Found in RMC-BestFit 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) and Numerics 2.2.0 at [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0), both still `main` on 2026-10-31.

**Type:** wrong result (numerical precision) · **Severity:** low

**Verification:** reproduced with upstream's own code: RMC-BestFit 2.0.1's WPF element layer, built from this commit, estimating the shipped `viglione-et-al-2013` example again on Windows, against the closed-form maximum-likelihood estimate and the fits the example was saved with.

## Summary

A Distribution Fitting Analysis maximizes each candidate's likelihood by differential evolution alone, and stops as soon as its population's log-likelihoods agree to 1e-8. That pins the log-likelihood, but the parameters only to about 1e-4 relative: the search stops anywhere on the flat top of the likelihood, and nothing refines its result. So the reported fits aren't the maximum-likelihood estimates, only close to them, and their last digits move with any change to the search: the example's saved fits and 2.0.1's own fits of the same data differ by up to 0.3% in a skew.

## Details and how to reproduce

The example `4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.bestfit`, analysis "Fit - Systematic (1951-2001)", 51 exact annual peaks. For the Log-Normal (base 10) distribution the maximum-likelihood estimate has a closed form: µ is the mean of log₁₀ x, and σ their standard deviation with divisor n.

| Log-Normal (base 10) | µ | σ | 1% AEP flow | 1e-6 AEP flow | AIC |
|---|---|---|---|---|---|
| Closed form (the MLE) | 1.7006341231 | 0.2159114220 | 159.5582 | 533.2768 | 476.8657495 |
| RMC-BestFit 2.0.1, estimated again | 1.7006208271 | 0.2159278613 | 159.5674 | 533.3565 | 476.8657502 |
| Saved in the example (an earlier build) | 1.7006396514 | 0.2159012116 | 159.5515 | 533.2240 | 476.8657497 |

All three agree on the log-likelihood (AIC) to 1e-9, but 2.0.1's σ is 7.6e-5 from the closed form, and its quantiles 6e-5 to 1.5e-4. The Log-Pearson Type III fit of the same data moves more between the saved fit and 2.0.1's: skew −0.10578 against −0.10549 (0.28%), and the 1e-6 AEP flow 442.85 against 443.09, at the same AIC to 1e-9. Of the example's four fitting analyses, three differ between the saved fits and 2.0.1's in this way (the Log-Normal and Log-Pearson III candidates); the fourth, and all the Exponential fits, are identical. 2.0.1's fits are the same on Windows and Linux to the last bit, so it's the search's stopping point that varies, not the machine.

## Why this is an issue and not intended behaviour

The analysis is presented as maximum likelihood, and its AIC and BIC are computed from it. Differential evolution is a global search, good for finding the right basin, but it isn't meant to locate the maximum precisely. The stopping rule (`statistics.StandardDeviation < AbsoluteTolerance + RelativeTolerance * Math.Abs(statistics.Mean)`) measures how much the population's log-likelihoods still differ, which near a maximum is quadratic in the distance from it, so 1e-8 there means about 1e-4 in the parameters. The differences are small next to sampling uncertainty, but they show in the digits RMC-BestFit displays (159.567 against the MLE's 159.558 cfs), and they make a fit change whenever the search changes, which is how the shipped example's saved fits came to differ from what 2.0.1 computes.

## Where in the source

- [`FittingAnalysis.cs` lines 355-363](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/DistributionFitting/FittingAnalysis.cs#L355-L363): each candidate is fitted by differential evolution only.
  ```csharp
  var mle = new MaximumLikelihood(dist, OptimizationMethod.DifferentialEvolution)
  {
      ReportFailure = true,
      ComputeHessian = false,
      Optimizer = { MaxIterations = 10000, MaxFunctionEvaluations = 100000 }
  };
  mle.Estimate();
  ```
- Numerics' [`DifferentialEvolution.cs` lines 182-195](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Mathematics/Optimization/Global/DifferentialEvolution.cs#L182-L195): a trial replaces its target when `fitness <= Xp[i].Fitness`, and the search stops when the population's fitness spread is below the tolerance, which defaults to 1e-8 absolute and relative ([`Optimizer.cs` lines 48-53](https://github.com/USACE-RMC/Numerics/blob/7e8e8d1c5f26e045a35ec9fc09367de95ed05b02/Numerics/Mathematics/Optimization/Support/Optimizer.cs#L48-L53)).

## Suggested fix

Polish the global search's best point with a local optimizer, as Bulletin 17C's GMM already does with BFGS: for example run `NelderMead` or `BFGS` from `mle.BestParameterSet.Values` (within the same bounds) and keep the better of the two. The fits then reach the maximum to rounding. Alternatively use `OptimizationMethod.MultilevelSingleLinkage`, whose local searches converge, or tighten the differential evolution's tolerances, which costs evaluations and still leaves the parameters to the stopping rule.

## How XGoodFit handles it

XGoodFit compiles RMC.BestFit unchanged, so its fits are RMC-BestFit 2.0.1's, bit for bit. Its reproducibility check ([`docs/reproducibility.md`](https://github.com/jonklaric/XGoodFit/blob/main/docs/reproducibility.md), `tools/Reproducibility`) classes a fitting analysis's parameters, quantiles and RMSE as the output of a random search, and requires its AIC and BIC to agree within 1e-6.
