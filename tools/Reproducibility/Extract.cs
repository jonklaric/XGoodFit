using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FrameworkInterfaces;
using Numerics.Distributions;
using Numerics.Sampling.MCMC;
using RMC.BestFit.Estimation;
using RMC.BestFit.UI;
using ModelAnalyses = RMC.BestFit.Analyses;

namespace Reproducibility;

/// <summary>An analysis's results as numbers by name, and how it estimates (<see cref="AnalysisRecord.Method"/>).</summary>
public sealed record Extracted(string Method, List<Value> Values);

/// <summary>
/// Reads an analysis element's results as numbers by name: what the project saved, right after opening, or what a rerun
/// computed. The same members are read both times.
/// </summary>
/// <remarks>
/// <para>What is read, where the analysis has it:</para>
/// <list type="bullet">
/// <item>the point estimate: a univariate analysis's distribution (<see cref="IUnivariate.GetPointEstimateDistribution"/>),
/// its parameters and its quantiles at <see cref="StandardAeps"/>; a fitting analysis's every fitted distribution;</item>
/// <item>the results curves (<see cref="UncertaintyAnalysisResults"/>): the mode (computed) and mean (expected) curves
/// and the confidence or credible limits at each of the analysis's ordinates, and its AIC, BIC, DIC, RMSE and ERL;</item>
/// <item>the MCMC sample's summary (<see cref="MCMCResults"/>): each parameter's mean, median, standard deviation,
/// credible limits, R-hat and effective sample size, the posterior mode (MAP) and mean, and the acceptance rates; and
/// the sampler's DIC, WAIC and LOOIC;</item>
/// <item>a Bulletin 17C bootstrap's counters (<see cref="ModelAnalyses.BootstrapDiagnostics"/>).</item>
/// </list>
/// <para>
/// Roles (<see cref="ValueRole"/>): everything an MCMC sampler or a bootstrap produces is Monte Carlo, including the
/// point estimate when it is the posterior's mode or mean. Located by an optimizer: Bulletin 17C's computed (GMM) fit,
/// its curve and statistics. Deterministic: a fitting analysis's AIC and BIC. Bulletin 17C's limits, expected curve and counters come from its uncertainty
/// method's random sample (bootstrap or multivariate-normal realizations), so they are Monte Carlo, as are a fitting
/// analysis's parameters, quantiles and RMSE, which a seeded random search finds (see <see cref="Fitting"/>).
/// </para>
/// </remarks>
public static class Extract
{
    /// <summary>The annual exceedance probabilities point-estimate quantiles are read at: the 2- to 1,000,000-year events.</summary>
    public static readonly double[] StandardAeps = { 0.5, 0.2, 0.1, 0.05, 0.02, 0.01, 0.005, 0.002, 0.001, 1e-4, 1e-5, 1e-6 };

    private const ValueRole E = ValueRole.Estimate, O = ValueRole.Optimizer, MC = ValueRole.MonteCarlo;

    /// <summary>Reads an analysis element's results.</summary>
    public static Extracted From(IElement element)
    {
        var values = new List<Value>();
        string method;
        switch (element)
        {
            case FittingAnalysis fitting:
                method = "maximum likelihood by differential evolution, seed 12345";
                Fitting(fitting, values);
                break;
            case B17CAnalysis b17c:
                method = $"GMM; limits by {b17c.UncertaintyMethod}, seed {b17c.PRNGSeed}, {b17c.BayesianAnalysis?.OutputLength} realizations";
                B17C(b17c, values);
                break;
            case IAnalysisElement analysis:
                bool sampled = Sampled(analysis.BayesianAnalysis);
                method = Bayesian(analysis.BayesianAnalysis);
                ValueRole role = sampled ? MC : E;
                if (analysis is IUnivariate univariate)
                    PointEstimate(univariate, role, values);
                Curves(analysis, analysis.AnalysisResults, role, MC, values);
                Mcmc(analysis.BayesianAnalysis, values);
                break;
            default:
                throw new ArgumentException($"{element.GetType().Name} isn't an analysis.");
        }
        return new Extracted(method, values);
    }

    // Whether the analysis's results come from its MCMC sampler: every analysis kind but B17C and fitting estimates by MCMC
    // (RMC.BestFit's model analyses call BayesianAnalysis.Estimate), so this is true unless it holds no sample.
    private static bool Sampled(BayesianAnalysis? bayesian) => bayesian != null;

    private static string Bayesian(BayesianAnalysis? bayesian) => bayesian == null
        ? "no sampler"
        : $"{bayesian.Type}, seed {bayesian.PRNGSeed}, {bayesian.NumberOfChains} chains, {bayesian.WarmupIterations}+{bayesian.Iterations} iterations, " +
          $"thinning {bayesian.ThinningInterval}, {bayesian.OutputLength} output, {bayesian.PointEstimator}";

    // A fitting analysis maximizes each likelihood by differential evolution (RMC.BestFit's FittingAnalysis), a random
    // search seeded with 12345 that stops once its population's log-likelihoods agree to 1e-8 (Numerics' default
    // tolerances), with no local polish. The log-likelihood is pinned to that tolerance, so AIC and BIC are deterministic
    // values; the parameters only to about its square root, wherever the search stopped on the flat top of the
    // likelihood, and a rounding difference that flips one of its accept-or-keep comparisons ends the search elsewhere.
    // So the parameters, the quantiles and the RMSE (which follows from them) are classed as a seeded random method's.
    private static void Fitting(FittingAnalysis fitting, List<Value> values)
    {
        foreach (var fit in fitting.FittedDistributions ?? new())
        {
            UnivariateDistributionBase? distribution = fit.Distribution;
            string name = "fit " + (distribution?.DisplayName ?? "?");
            values.Add(new($"{name}/succeeded", E, fit.FitSucceeded ? 1 : 0));
            values.Add(new($"{name}/AIC", E, fit.AIC));
            values.Add(new($"{name}/BIC", E, fit.BIC));
            values.Add(new($"{name}/RMSE", MC, fit.RMSE));
            if (distribution is null || !fit.FitSucceeded) continue;
            Distribution(name, distribution, MC, values);
        }
    }

    private static void B17C(B17CAnalysis analysis, List<Value> values)
    {
        // The computed (GMM) fit, its curve and its statistics are where the GMM's optimizer stopped; the limits, the
        // expected curve and the counters come from the uncertainty method's random sample. The optimizer's status is
        // compared as a code: measured against a scale of 1e4, a change of status is an optimizer difference, as the
        // point it describes is.
        PointEstimate(analysis, O, values);
        Curves(analysis, analysis.AnalysisResults, O, MC, values);
        if (analysis.GMM is { } gmm)
            values.Add(new("gmm/status", O, (double)gmm.Status, 1e4));
        if (analysis.BootstrapResults is { } bootstrap)
        {
            // Counts of replicates are measured against the number of replicates: 1 fallback against 0 is a difference
            // of one replicate in a thousand, not of 100%.
            double replicates = bootstrap.TotalReplicates;
            values.Add(new("bootstrap/total replicates", MC, bootstrap.TotalReplicates));
            values.Add(new("bootstrap/retained replicates", MC, bootstrap.RetainedReplicates, replicates));
            values.Add(new("bootstrap/failed replicates", MC, bootstrap.FailedReplicates, replicates));
            values.Add(new("bootstrap/optimizer fallbacks", MC, bootstrap.OptimizerFallbacks, replicates));
            values.Add(new("bootstrap/total retries", MC, bootstrap.TotalRetries, replicates));
            values.Add(new("bootstrap/status success", MC, bootstrap.StatusSuccessCount, replicates));
            values.Add(new("bootstrap/status maximum iterations", MC, bootstrap.StatusMaximumIterationsCount, replicates));
            values.Add(new("bootstrap/status maximum evaluations", MC, bootstrap.StatusMaximumFunctionEvaluationsCount, replicates));
            values.Add(new("bootstrap/status failure", MC, bootstrap.StatusFailureCount, replicates));
            values.Add(new("bootstrap/pivot rejections", MC, bootstrap.PivotRejections, replicates));
            values.Add(new("bootstrap/mahalanobis rejections", MC, bootstrap.MahalanobisRejections, replicates));
            values.Add(new("bootstrap/bound repairs", MC, bootstrap.BoundRepairs, replicates));
            values.Add(new("bootstrap/transform failures", MC, bootstrap.TransformFailures, replicates));
            values.Add(new("bootstrap/function evaluations", MC, bootstrap.TotalFunctionEvaluations));
        }
    }

    private static void PointEstimate(IUnivariate analysis, ValueRole role, List<Value> values)
    {
        UnivariateDistributionBase? distribution;
        try
        {
            distribution = analysis.GetPointEstimateDistribution();
        }
        catch (Exception)
        {
            distribution = null;
        }
        if (distribution is not null) Distribution("point", distribution, role, values);
    }

    private static void Distribution(string name, UnivariateDistributionBase distribution, ValueRole role, List<Value> values)
    {
        double[] parameters = distribution.GetParameters;
        string[] names = distribution.ParameterNamesShortForm;
        for (int i = 0; i < parameters.Length; i++)
            values.Add(new($"{name}/{(i < names.Length ? names[i] : "p" + i)}", role, parameters[i]));
        foreach (double aep in StandardAeps)
            values.Add(new($"{name}/quantile AEP {Aep(aep)}", role, Safe(() => distribution.InverseCDF(1 - aep))));
    }

    // The results curves at the analysis's ordinates: AEPs for a univariate analysis, otherwise by index.
    private static void Curves(IElement analysis, UncertaintyAnalysisResults? results, ValueRole pointRole, ValueRole sampleRole,
        List<Value> values)
    {
        if (results == null) return;
        IList<double>? ordinates = (analysis as ModelAnalyses.IProbabilityOrdinates)?.ProbabilityOrdinates;
        string At(int i) => ordinates != null && i < ordinates.Count ? "AEP " + Aep(ordinates[i]) : "#" + i.ToString(CultureInfo.InvariantCulture);

        if (results.ModeCurve is { } mode)
            for (int i = 0; i < mode.Length; i++) values.Add(new($"curve/mode {At(i)}", pointRole, mode[i]));
        if (results.MeanCurve is { } mean)
            for (int i = 0; i < mean.Length; i++) values.Add(new($"curve/mean {At(i)}", sampleRole, mean[i]));
        // Limits are [point, lower and upper], or for a rating curve or a time series [point, x and lower and upper],
        // where x is the stage or time step, which comes from the settings.
        if (results.ConfidenceIntervals is { } limits)
        {
            int columns = limits.GetLength(1), first = columns == 3 ? 1 : 0;
            for (int i = 0; i < limits.GetLength(0); i++)
            {
                if (first == 1) values.Add(new($"curve/x {At(i)}", E, limits[i, 0]));
                for (int j = first; j < columns; j++)
                    values.Add(new($"curve/{(j == first ? "lower" : j == columns - 1 ? "upper" : "limit " + j)} {At(i)}", sampleRole, limits[i, j]));
            }
        }
        values.Add(new("fit/AIC", pointRole, results.AIC));
        values.Add(new("fit/BIC", pointRole, results.BIC));
        values.Add(new("fit/DIC", sampleRole, results.DIC));
        values.Add(new("fit/RMSE", pointRole, results.RMSE));
        values.Add(new("fit/ERL", pointRole, results.ERL));
    }

    private static void Mcmc(BayesianAnalysis? bayesian, List<Value> values)
    {
        if (bayesian?.Results is not { } results) return;
        IReadOnlyList<string>? names = bayesian.ParameterNames;
        string Name(int i) => names != null && i < names.Count ? names[i] : "p" + i.ToString(CultureInfo.InvariantCulture);

        // A parameter's location in its posterior (its mean, median, limits, MAP) is measured against its posterior
        // standard deviation, so a skew near zero (0.0126 against -0.00006) is a shift of a fraction of the posterior's
        // width, not a relative difference of 100%.
        double? Spread(int i) => results.ParameterResults != null && i < results.ParameterResults.Length
            ? results.ParameterResults[i]?.SummaryStatistics?.StandardDeviation : null;
        // An effective sample size is measured against the sample's size: two chains of the same posterior can mix
        // differently, and 580 against 1,126 effective draws of 10,000 is a difference of 5% of the sample.
        double? draws = results.Output?.Count;
        if (results.ParameterResults != null)
            for (int i = 0; i < results.ParameterResults.Length; i++)
            {
                var s = results.ParameterResults[i]?.SummaryStatistics;
                if (s == null) continue;
                string p = "mcmc/" + Name(i);
                values.Add(new($"{p}/mean", MC, s.Mean, s.StandardDeviation));
                values.Add(new($"{p}/median", MC, s.Median, s.StandardDeviation));
                values.Add(new($"{p}/sd", MC, s.StandardDeviation));
                values.Add(new($"{p}/lower", MC, s.LowerCI, s.StandardDeviation));
                values.Add(new($"{p}/upper", MC, s.UpperCI, s.StandardDeviation));
                values.Add(new($"{p}/R-hat", MC, s.Rhat));
                values.Add(new($"{p}/ESS", MC, s.ESS, draws));
            }
        if (results.MAP.Values is { } map)
        {
            for (int i = 0; i < map.Length; i++) values.Add(new($"mcmc/MAP/{Name(i)}", MC, map[i], Spread(i)));
            values.Add(new("mcmc/MAP/log-likelihood", MC, results.MAP.Fitness));
        }
        if (results.PosteriorMean.Values is { } posteriorMean)
            for (int i = 0; i < posteriorMean.Length; i++) values.Add(new($"mcmc/posterior mean/{Name(i)}", MC, posteriorMean[i], Spread(i)));
        if (results.AcceptanceRates != null)
            for (int i = 0; i < results.AcceptanceRates.Length; i++)
                values.Add(new($"mcmc/acceptance rate/chain {i + 1}", MC, results.AcceptanceRates[i]));
        values.Add(new("mcmc/output length", MC, results.Output?.Count));
        values.Add(new("mcmc/DIC", MC, bayesian.DIC));
        values.Add(new("mcmc/WAIC", MC, bayesian.WAIC));
        values.Add(new("mcmc/LOOIC", MC, bayesian.LOOIC));
    }

    private static double? Safe(Func<double> value)
    {
        try
        {
            return value();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string Aep(double aep) => aep.ToString("R", CultureInfo.InvariantCulture);
}
