# Numerical reproducibility across operating systems

RMC-BestFit is life-safety software: its flood frequency curves and their limits feed dam and levee decisions. XGoodFit
must give RMC-BestFit's results on every OS it runs on. This page says what is measured, how to run the checks, how
differences are classed, and what has been found (Phase 9 of the [migration plan](migration-plan.md)).

XGoodFit computes nothing of its own. The model library (`RMC.BestFit`) and the numerical library under it
(`RMC.Numerics`) are upstream's, compiled unchanged from [`upstream/`](../upstream). The port's element layer
(`src/BestFit/RMC.BestFit.UI`) holds the analyses, reads and writes them, and calls the model to estimate them, as
upstream's does. So a difference between XGoodFit's results and RMC-BestFit's can come from three places:

- **the machine:** .NET's maths functions (`Math.Exp`, `Math.Log`, `Math.Pow`, …) call each OS's C maths library, whose
  last bits can differ between Windows, glibc (Linux) and Apple's libm, and between x64 and arm64;
- **upstream's code since the examples were saved:** the examples' results were saved by RMC-BestFit builds from
  2.0 Beta-4 to 2.0.1, many of them older than the 2.0.1 source vendored here;
- **the port:** a mistake in the element layer, which these checks are there to catch.

## What is measured

1. **Upstream's own test suites.** On every push, on Windows, Linux and macOS (`scripts/run-upstream-tests.sh`, CI's
   portable job): Numerics' suite, `RMC.BestFit.Tests` (the model, 3,546 tolerance-based tests) and
   `RMC.BestFit.Api.Tests` (the REST API and MCP server, 605 tests in seconds). Upstream's verification suite,
   `RMC.BestFit.Verification` (329 tests: the model against published results, R and Python packages' answers in
   `upstream/RMC-BestFit/verification/data`, and Bulletin 17C's reference data), takes 20 to 25 minutes on a
   12-to-16-thread machine, so the weekly workflow runs it, on the same OSes and macOS x64
   (`scripts/run-upstream-tests.sh --verification`).
2. **The examples, estimated again.** `tools/Reproducibility run` opens each of RMC-BestFit's 32 example projects with
   XGoodFit's element layer and, for every analysis the project holds as estimated (170; three more are saved
   unestimated and are listed), reads the saved results, estimates it again with its saved settings and seed as the
   Estimate button does, and reads the new results. Analyses built on others (a composite's components, a bivariate
   analysis's marginals, a coincident-frequency analysis's bivariate one) are estimated after them. It writes both as
   JSON, with each analysis's run time and the machine. Estimating never reads a DSS file, so no HEC-DSS native is
   needed.
3. **RMC-BestFit itself, on Windows.** `tools/BestFitFileCompat.Wpf rerun` does exactly the same with upstream's own WPF
   element layer, RMC-BestFit 2.0.1 built from `upstream/` unedited (the same runner and extractor files are compiled
   into it). This is the reference: on Windows XGoodFit must compute what RMC-BestFit computes.
4. **The comparison.** `tools/Reproducibility compare` reads every run's JSON and reports each analysis's class (below)
   against the saved results, against XGoodFit's Windows run (the other OSes, and RMC-BestFit's run), and the notable
   cases.

### Which results

For each analysis, where it has them:

- **the point estimate:** a univariate analysis's distribution (Bulletin 17C's computed fit, a Bayesian analysis's
  posterior mode or mean), its parameters, and its quantiles at the 2- to 1,000,000-year events (AEP 0.5 to 1e-6); for a
  Distribution Fitting Analysis, each fitted distribution's parameters, quantiles, AIC, BIC and RMSE;
- **the curves at the analysis's ordinates:** the mode (computed) and mean (expected) curves, and the lower and upper
  confidence or credible limits: for a univariate analysis at each of its AEPs, for the others (rating curves, time
  series, bivariate and coincident-frequency analyses) at each of their points;
- **goodness of fit:** AIC, BIC, DIC, RMSE and ERL;
- **the MCMC sample:** each parameter's mean, median, standard deviation, credible limits, R-hat and effective sample
  size (ESS), the posterior mode (MAP) and its log-likelihood, the posterior mean, each chain's acceptance rate, and the
  sampler's DIC, WAIC and LOOIC;
- **Bulletin 17C's GMM status, and a bootstrap's counters:** replicates retained and failed, optimizer fallbacks,
  retries, the optimizer's status counts, pivot and Mahalanobis rejections, bound repairs, and function evaluations.

Each value has one of three roles, which decide how far it may move:

- **Deterministic:** computed by a deterministic method from the data and settings. A Distribution Fitting Analysis's
  AIC and BIC (below), and what the other roles leave.
- **Located by an optimizer:** Bulletin 17C's GMM fit, its computed curve, its AIC, BIC, RMSE and ERL, and the GMM's
  status. Each GMM step is a BFGS minimization that stops when the objective agrees to 1e-8, within 2,000 evaluations.
  On a flat objective that pins the parameters only to about the tolerance's square root, 1e-4, so a rounding
  difference can move them that far (see the findings).
- **Monte Carlo:** from a random method with the saved seed. Everything from an MCMC sample, including a Bayesian
  point estimate; Bulletin 17C's limits, expected curve and counters, which come from its bootstrap or
  multivariate-normal realizations; a composite's or coincident-frequency analysis's resampling; and a Distribution
  Fitting Analysis's parameters, quantiles and RMSE. A Distribution Fitting Analysis maximizes each likelihood by
  differential evolution, a random search (seed 12345) that stops once its population's log-likelihoods agree to 1e-8,
  with no local refinement: the log-likelihood (AIC, BIC) is pinned to that, the parameters only to about 1e-4,
  anywhere on the flat top of the likelihood.

## The classes

An analysis's class is that of its farthest value. The relative difference of two values is
`|a − b| / max(|a|, |b|, scale)`. The scale is 1e-13, so that values that are zero up to rounding compare as equal,
except for values that can be near zero:

- an MCMC parameter's mean, median, limits, MAP and posterior mean are measured against its posterior standard
  deviation;
- its ESS against the sample's size;
- a bootstrap's counts against its number of replicates;
- the GMM status (a code) against 1e4.

| Class | Relative difference | What it means |
|---|---|---|
| identical | 0 | Bit for bit the same. |
| last-bit | ≤ 1e-12 | The last bits of a double: one maths library rounds differently. |
| small | ≤ 1e-6 | Rounding carried through an optimizer's iterations or a long sum. Far below any digit RMC-BestFit shows. |
| optimizer | ≤ 1e-3, in a value an optimizer located | The optimizer stopped elsewhere within its tolerance. |
| Monte Carlo | ≤ 50 %, in a Monte Carlo value | A rounding difference changed which MCMC proposals were accepted, which bootstrap replicates' optimizers took another path, or where a differential-evolution search stopped: a different draw from the same distribution. |
| since saved | anything, against the saved results | RMC-BestFit 2.0.1's own rerun differs from the saved results too: what RMC-BestFit computes has changed since the example was saved. Needs RMC-BestFit's run; without it, such differences are unexplained. |
| known issue | anything, in one analysis on one platform | An unexplained difference already explained as an upstream issue, listed in `tools/Reproducibility/KnownIssues.cs` with its issue. Reported, with the issue, but doesn't fail the check; the same difference anywhere else still does. Remove the entry when the issue is fixed. |
| unexplained | anything else | A deterministic value more than 1e-6 apart, an optimizer's more than 1e-3, a Monte Carlo value more than 50 %, a value one side has and the other hasn't, a rerun that fails, or saved results that two runs read differently (beyond rounding, in values computed from them). **Fails the check.** |

The 50 % bound isn't a statement about Monte Carlo error, which is a few percent at most in the analyses' limits: it
only stops a value that is no longer the same quantity from passing as Monte Carlo. The report lists the largest
optimizer and Monte Carlo differences, and the ones since saved, so they can be judged.

## Running it

Locally, on any OS (the examples are about 455 MB):

```bash
bash scripts/fetch-sample-data.sh RMC-BestFit
dotnet build tools/Reproducibility -c Release
# All 170 estimated analyses (10 to 15 minutes on a 12-to-16-thread machine), or a few with --only.
dotnet run -c Release --no-build --project tools/Reproducibility -- run sample-data/RMC-BestFit/examples --out artifacts/reproducibility/my-machine.json
dotnet run -c Release --no-build --project tools/Reproducibility -- run sample-data/RMC-BestFit/examples --out trial.json --only "bulletin-17c-examples" --only "Bayes Example #1"
# Lists the analyses and whether each is estimated in its project.
dotnet run -c Release --no-build --project tools/Reproducibility -- list sample-data/RMC-BestFit/examples
# Windows only: the same with RMC-BestFit 2.0.1's own WPF element layer.
dotnet build tools/BestFitFileCompat.Wpf -c Release
dotnet run -c Release --no-build --project tools/BestFitFileCompat.Wpf -- rerun sample-data/RMC-BestFit/examples artifacts/reproducibility/rmc-bestfit.json
# A report from several runs' files (exit code 1 if anything is unexplained).
dotnet run -c Release --no-build --project tools/Reproducibility -- compare artifacts/reproducibility --report report.md
```

`--only <text>` keeps the analyses whose `<example path> | <analysis name>` contains the text, ignoring case; give it
several times to keep several. Runs split this way add up in the report. `--limit <minutes>` (default 10) cancels any
analysis's rerun that takes longer and lists it as skipped; none does. `compare --baseline <rid>` picks the run the
others are compared with (default XGoodFit's `win-x64`).

The upstream suites: `bash scripts/run-upstream-tests.sh` (every push's) and `bash scripts/run-upstream-tests.sh
--verification [FILTER]` (the verification suite, or the part a test filter selects).

In CI, the [Reproducibility workflow](../.github/workflows/reproducibility.yml) runs every Monday and by hand (Actions,
Reproducibility, Run workflow; its `only` input passes `--only`). It reruns the examples with XGoodFit on windows-latest,
ubuntu-latest, macos-latest (arm64) and macos-15-intel (x64, GitHub's last Intel image, until the macOS 15 images
retire in 2027), and with RMC-BestFit 2.0.1 on windows-latest, each in two parts of about equal length: the Bayesian
univariate examples, and the rest. It runs the verification suite on the same four OSes in two parts too (its
Univariate tests, and the rest). Its report job writes the report's summary and notable cases to the run's summary, and
all of it, with every run's JSON, to the `reproducibility-report` artifact. It fails if anything is unexplained.

## Findings

From the first full runs (2026-10-31): XGoodFit on a Windows 11 laptop (16 threads, 12.5 minutes), XGoodFit on Ubuntu
26.04 (12 cores, 10 minutes), and RMC-BestFit 2.0.1's WPF element layer on the same laptop (13 minutes), all on .NET
10.0.14. Then CI's first run of the workflow ([run 41183231534](https://github.com/jonklaric/XGoodFit/actions/runs/41183231534),
the same day) on GitHub's Windows x64, Ubuntu x64, macOS arm64 and macOS x64 (Intel) runners, with RMC-BestFit 2.0.1 on
Windows. It found one difference beyond the classes, on macOS x64, now a known issue (below).

| Comparison | identical | last-bit | small | optimizer | Monte Carlo | since saved | unexplained |
|---|---:|---:|---:|---:|---:|---:|---:|
| RMC-BestFit 2.0.1 vs XGoodFit, Windows | 170 | | | | | | |
| XGoodFit Linux vs XGoodFit Windows | 20 | 110 | 16 | | 24 | | |
| XGoodFit Windows vs saved | 123 | | 2 | | 37 | 8 | |
| XGoodFit Linux vs saved | 14 | 78 | 14 | | 56 | 8 | |
| XGoodFit macOS arm64 vs XGoodFit Windows (CI) | 17 | 112 | 18 | | 23 | | |
| XGoodFit macOS x64 vs XGoodFit Windows (CI) | 20 | 112 | 16 | | 21 | | 1 (now a known issue) |

Three are listed, not compared: "B17C Example #3" (Bulletin 17C Bayesian examples, which opens invalid, see
`IssuesToRaise/RMC-BestFit-b17c-example-3-loses-results-on-save.md`), "Example #2 - BCB" and NSFFA OC Fisher Dam's
"Bayesian Model Average", which their projects hold unestimated.

- **XGoodFit computes exactly what RMC-BestFit does.** On Windows, all 170 analyses, every value, bit for bit. Each run
  also repeats itself exactly on the same machine.
- **Linux agrees with Windows to rounding, except for Bulletin 17C's random samples.**
  - Every analysis but Bulletin 17C's (135: the Bayesian univariate, mixture, point-process, composite, bivariate,
    coincident-frequency, rating-curve and time-series analyses, and the Distribution Fitting ones) is identical,
    last-bit or small, at most 3.2e-10 apart: no MCMC chain and no differential-evolution search took another path.
    The 1% AEP quantile and its credible limits agree to 2e-16 in every univariate analysis.
  - The 24 Monte Carlo differences are all Bulletin 17C. In its bias-corrected bootstrap, a replicate's optimizer
    that takes another path changes that replicate: the 1% AEP limits move by up to 0.92% (Example #7 - BCB's lower
    limit, 265,211 against 262,759 cfs), more far out in the tail (9% at AEP 1e-6), and the counters differ (Example #1
    - BCB: 77 optimizer fallbacks on Windows, 82 on Linux, as CI's walkthrough tests found). Its multivariate-normal
    methods move by at most 1.3e-5 at the 1% AEP: their sample follows the fit's covariance smoothly.
  - Bulletin 17C's GMM fits agree to 2e-8 or better without a penalty (Example #7's to 7e-7). With Blakely Mountain
    Dam's regional skew or quantile penalties they differ by 1.6e-6 to 8e-5 in the skew (optimizer class), and for one
    analysis the optimizer
    reports Success on Windows but its evaluation limit on Linux
    (`IssuesToRaise/RMC-BestFit-b17c-penalized-gmm-stops-at-tolerance-or-evaluation-limit.md`). Example #7's ERL,
    computed from the fit's covariance, moves 4e-5 for a 7e-7 move in the fit.
- **macOS agrees with Windows as Linux does, except for one analysis on Intel.** On CI's macOS arm64 runner every
  analysis falls in the same classes as on Linux. On the macOS x64 runner, Blakely Mountain Dam's "Systematic +
  Historical + QPen" (the metric project) converges to another solution of its penalized GMM. Its skew is 0.0018
  instead of −0.1797, its 1% AEP flow 5.5% higher, its 1-in-a-million flow 35% higher, and its AIC 1.43 worse. The
  optimizer still reports Success. It's RMC-BestFit's own Bulletin 17C code meeting Apple's maths library on Intel,
  not the port, so it's listed as a known issue (`tools/Reproducibility/KnownIssues.cs`). The issue was raised to
  medium (`IssuesToRaise/RMC-BestFit-b17c-penalized-gmm-stops-at-tolerance-or-evaluation-limit.md`). A user on an
  Intel Mac would get the other solution for that analysis.
- **Against the saved results, RMC-BestFit 2.0.1 itself differs wherever an analysis was saved by an earlier build.**
  The projects were last saved by builds from 2.0 Beta-4 (May 2026) to 2.0.1 (October 2026). The six projects saved by
  2.0.1 in October reproduce exactly but for two DICs (4e-6, `sum-two-normals`); the others' analyses differ where they
  were estimated by an earlier build, within the Monte Carlo class (37: MCMC chains and differential-evolution searches
  that took another path) or beyond it (8, "since saved"):
  - **Waimea's Exponential fit** (Distribution Fitting, `waimea-river-stage-frequency`): saved with ξ = 4,266, now
    ξ = 1,470, and its 1% AEP flow 15% higher. The saved fit is the right one: 2.0.1 caps the location at the smallest
    flow, low outliers included, below its own likelihood's maximum
    (`IssuesToRaise/RMC-BestFit-fitting-location-bound-includes-low-outliers.md`).
  - **Blakely Mountain Dam's seven Bulletin 17C analyses** (the project saved by 2.0 Beta-4): the GMM fits agree to
    5e-4 (optimizer class), but RMSE, AIC and BIC changed by up to 7% (AIC by 7), as the newer code computes them at
    nearly the same fit. Its metric
    copy, saved by 2.0.1, reproduces exactly.
  - **The Viglione et al. fits** move within differential evolution's tolerance (0.28% in a Log-Pearson III skew, at the
    same AIC to 1e-9), and neither the saved fits nor 2.0.1's are the maximum-likelihood estimates: 2.0.1's Log-Normal σ
    is 7.6e-5 from the closed form (`IssuesToRaise/RMC-BestFit-fitting-mle-stops-short-of-the-maximum.md`).
- **Upstream's suites:** the verification suite passes 329/329 on Windows (25 minutes) and Linux (23 minutes), and on
  all four of CI's runners; the API suite passes 605/605 everywhere, with no exclusions.

So on the evidence so far, the port adds nothing: XGoodFit's results are RMC-BestFit's on Windows. On Linux and macOS
they differ only where RMC-BestFit's own methods are sensitive to rounding (Bulletin 17C's bootstrap and its penalized
GMM fits), by amounts below the precision those methods claim. The exception is one penalized GMM fit on macOS x64,
which reaches another solution: an upstream weakness that the other platforms happen not to hit.
