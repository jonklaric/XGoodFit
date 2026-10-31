# Bulletin 17C's GMM fits with a regional skew or quantile penalty depend on the machine: the skew is good to about 1e-4, the optimizer's status varies, and on one machine a fit lands on a different solution

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `RMC.BestFit` (`Estimation/GeneralizedMethodOfMoments.cs`, `Analyses/Univariate/Bulletin17CAnalysis.cs`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** wrong result (machine-dependent) · **Severity:** medium (raised from low on 2026-10-31, when a macOS x64 run found a different solution reported as Success)

**Verification:** reproduced with upstream's model library built from this commit, on the Blakely Mountain Dam examples, on Windows x64 (RMC-BestFit 2.0.1's own WPF element layer) and Linux x64 (.NET 10), then on GitHub's runners: Windows x64, Linux x64, macOS arm64 and macOS x64 (XGoodFit's reproducibility workflow, [run 41183231534](https://github.com/jonklaric/XGoodFit/actions/runs/41183231534), 2026-10-31).

## Summary

A Bulletin 17C analysis fits its log-Pearson III distribution by iterative GMM, each step a BFGS minimization that stops when the objective agrees to 1e-8 or after 2,000 evaluations. Without penalties, the fits of the Blakely Mountain Dam examples agree between Windows and Linux to 1e-10 or better. With a regional skew penalty or quantile penalties, the objective is flatter, and the same fits differ between the two by 1.6e-6 to 8e-5 in the skew, and in the fifth or sixth significant digit of the computed quantiles. For one analysis the optimizer reports "Success" on Windows and "MaximumFunctionEvaluationsReached" on Linux, which the GMM report shows as its Optimizer Status.

On macOS x64 (Intel) one fit goes further: "Systematic + Historical + QPen" in the metric project converges to a different solution, with a skew of 0.0018 instead of −0.1797. Its 1% AEP flow is 5.5% higher and its 1-in-a-million flow 35% higher. The status is still Success, so nothing tells the user. Its AIC is 1.43 higher (1,455.915 against 1,454.488), so it is the worse fit by the likelihood. Windows, Linux and macOS arm64 all give the saved solution (see "A different solution on macOS x64", below).

## Details and how to reproduce

`4-univariate-distribution-analysis/2-bulletin-17C-analysis/2-information-expansion/blakely-mountain-dam-b17c.bestfit` and its metric copy under `Case Study/`: estimate each analysis again on Windows and on Linux (the same build, seed and settings):

| Analysis (metric project) | Skew on Windows | Skew on Linux | Relative | Optimizer status (saved / Windows / Linux) |
|---|---|---|---|---|
| Systematic | −0.24615307 | −0.24615307 | 8.5e-11 | Success / Success / Success |
| Systematic + Historical + Paleo | −0.28172460 | −0.28172459 | 5.6e-9 | Success / Success / Success |
| Systematic + Reg Skew | −0.21846557 | −0.21846625 | 3.1e-6 | Success / Success / Success |
| Systematic + Historical + Paleo + Reg Skew | −0.25780196 | −0.25779395 | 3.1e-5 | Success / Success / **MaximumFunctionEvaluationsReached** |
| Systematic + Historical + Paleo + QPen | −0.23261162 | −0.23262602 | 6.2e-5 | Success / Success / Success |

In the non-metric project the "Systematic + Historical + Paleo + Reg Skew" fit was saved with MaximumFunctionEvaluationsReached, reports Success on Windows today, and MaximumFunctionEvaluationsReached on Linux; its "+ QPen" variant's skew differs by 8.1e-5 between the two. The 1% AEP flows move by up to 1e-5 (133,224.99 against 133,223.65 cfs). .NET's `Math.Log`, `Math.Exp` and `Math.Pow` come from each OS's C maths library and can differ in the last bit; the unpenalized fits show that is harmless when the optimum is well defined.

### A different solution on macOS x64

"Systematic + Historical + QPen" (`Case Study/blakely-mountain-dam-b17c-metric.bestfit`), estimated again with its saved settings and seed 12345:

| Value | Saved, and Windows x64, Linux x64, macOS arm64 | macOS x64 |
|---|---|---|
| Mean (of log) µ | 3.09617 | 3.09332 |
| Standard deviation σ | 0.224042 | 0.222249 |
| Skew γ | −0.179742 | 0.001802 |
| 1% AEP | 3,868.95 | 4,079.86 (+5.5%) |
| 0.1% AEP | 5,390.05 | 6,035.24 (+12%) |
| 1e-6 AEP | 10,477.95 | 14,164.74 (+35%) |
| AIC | 1,454.488 | 1,455.915 |
| RMSE | 92.19 | 133.56 |
| GMM status | Success (1) | Success (1) |

(The other three agree with the saved values to about 1e-6 or better.) Two fixed points of the iterative GMM, one near zero skew, both pass the objective tolerance. Which one the iteration reaches depends on last-bit differences in the maths library, here Apple's on Intel.

## Why this is an issue and not intended behaviour

An estimate whose sixth digit, and whether it "converged", depend on the machine is hard to verify and to reproduce, for example when a study is checked on another computer. An estimate that lands on a different solution, 35% apart in the extreme quantiles, while it reports Success, is a wrong result for whoever runs it on that machine. The status matters beyond the report: the jackknife in `Bulletin17CAnalysis` keeps only replicates whose GMM status is Success (line 2874). The likely cause is the penalized objective's flatness against a tolerance on the objective (1e-8) rather than on the parameters, and a 2,000-evaluation budget the penalized fits come close to.

## Where in the source

- [`GeneralizedMethodOfMoments.cs` lines 204-210](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Estimation/GeneralizedMethodOfMoments.cs#L204-L210): BFGS, iterative GMM, tolerances of 1e-8 and at most 2,000 function evaluations.
  ```csharp
  private OptimizationMethod _optimizerMethod = OptimizationMethod.BFGS;
  private GMMEstimationStrategy _estimationStrategy = GMMEstimationStrategy.Iterative;
  private int _maxGMMIterations = 100;
  ...
  private double _absoluteTolerance = 1E-8;
  private double _relativeTolerance = 1E-8;
  private int _maxFunctionEvaluations = 2000;
  ```
- [`Bulletin17CAnalysis.cs` line 3688](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Analyses/Univariate/Bulletin17CAnalysis.cs#L3688): the GMM report's Optimizer Status.

## Suggested fix

Give the penalized fits a convergence test on the parameters (or the gradient) as well as the objective, and a larger evaluation budget, or polish the GMM solution with a few Newton steps on the final weighting. Then check that the Blakely fits agree across machines to about 1e-10, as the unpenalized ones do. To catch a different solution, compare the objective at the solution with a few restarts from other starting points (for example the unpenalized fit and zero skew), keep the best, and report when they disagree.

## How XGoodFit handles it

XGoodFit compiles RMC.BestFit unchanged, so on Windows its fits are RMC-BestFit 2.0.1's, bit for bit. Its reproducibility check ([`docs/reproducibility.md`](https://github.com/jonklaric/XGoodFit/blob/main/docs/reproducibility.md)) classes a Bulletin 17C fit's values as located by an optimizer, which may differ between machines up to 1e-3, and lists the largest such differences in each report.
