using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Mixture distributions: latent components and exact zeros"
    /// (examples/4-univariate-distribution-analysis/4-mixture-analysis/mixture-distribution-examples.md), followed through
    /// the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures, the three-Normal fit's trace and the native exports are compared, point for point, by
    /// <see cref="MixtureAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughMixtureTests
    {
        private const string Folder = "4-univariate-distribution-analysis/4-mixture-analysis/";
        private const string ZeroInflatedData = "Mixture of 2 Normals and Zero Inflated - Data";
        private static readonly WalkthroughExample Example = new WalkthroughExample("mixture-distribution-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and step 3: three datasets of 100 exact generic values indexed 1-100, all positive but the
        /// zero-inflated one's ten exact zeros, with no other records.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                foreach (var (name, zeros) in new[] { ("Mixture of 2 Normals - Data", 0), ("Mixture of 3 Normals - Data", 0), (ZeroInflatedData, 10) })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), 100, 1, 100, 0, 0, 0, 0, name);
                    Assert.AreEqual("Value", Shown(properties.UnitLabel), name + ": the unit label");
                    var values = Enumerable.Range(0, 100).Select(row => Parse(Text(view.ExactDataGrid, row, 1))).ToList();
                    Assert.AreEqual(zeros, values.Count(v => v == 0), name + ": the exact zeros");
                    Assert.AreEqual(100 - zeros, values.Count(v => v > 0), name + ": the positive values");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Steps 1, 2 and 4, the component table and the priors: each fit's components in their saved order (means and
        /// standard deviations) and masses (the last one the rest of the continuous mass); 5, 8 and 5 free parameters (the
        /// weights constrained); the zero-inflated fit's zero mass fixed at the data's 0.10, the continuous masses summing to
        /// 0.90; default flat priors, Jeffreys' rule and no quantile priors; and Select Parameter choosing Weight (w₁) for the
        /// two-Normal fit.
        /// </summary>
        [TestMethod]
        public void Analyses_HaveTheWalkthroughsComponents()
        {
            Example.Run(() =>
            {
                foreach (var (name, input, components, masses, zeroInflated) in Components)
                {
                    var element = Example.Element<MixtureAnalysis>(name);
                    var (view, properties) = Example.Open<MixtureAnalysisControl, MixtureAnalysisPropertiesControl>(element);
                    Assert.AreEqual(input, Shown(properties.InputDataComboBox), name + ": its input");
                    Assert.AreEqual(zeroInflated, Ticked(properties.IsZeroInflated), name + ": zero-inflated");
                    Assert.IsTrue(Ticked(properties.ParameterPriorsControl.UsePriorDistributionDefaults), name + ": default flat priors");
                    Assert.IsTrue(Ticked(properties.UseJeffreysRule), name + ": Jeffreys' rule");
                    Assert.IsFalse(QuantilePriorsEnabled(properties.QuantilePriorsControl), name + ": no quantile priors");

                    SelectTab(view, "MCMC Report");
                    string report = ReportText(view.MCMCReportControl);
                    // The point estimates are the posterior means, which the report shows to six significant digits.
                    var parameters = ReportParameters(report);
                    Assert.AreEqual(components.Length * 2 + components.Length - 1, parameters.Count, name + ": the free parameters, " + string.Join(", ", parameters.Select(p => p.Name)));
                    for (int i = 0; i < components.Length; i++)
                    {
                        AssertAgrees(components[i].Mean, parameters.Single(p => p.Name == $"D{i + 1} Mean (µ)").Mean, $"{name}: component {i + 1}'s mean");
                        AssertAgrees(components[i].Sd, parameters.Single(p => p.Name == $"D{i + 1} Std Dev (σ)").Mean, $"{name}: component {i + 1}'s standard deviation");
                    }
                    double zero = zeroInflated ? 0.1 : 0;
                    double rest = 1 - zero, roundings = 0.5E-7;
                    for (int i = 0; i < components.Length - 1; i++)
                    {
                        string weight = parameters.Single(p => p.Name == $"Weight (w{(char)('₀' + i + 1)})").Mean;
                        AssertAgrees(masses[i], weight, $"{name}: component {i + 1}'s mass");
                        rest -= Parse(weight);
                        roundings += 0.5 * Math.Pow(10, -(weight.Length - weight.IndexOf('.') - 1));
                    }
                    // The last component's mass, which the model derives from the others: within their roundings.
                    Assert.AreEqual(Parse(masses[^1]), rest, roundings, $"{name}: the last component's mass");
                    // The zero mass isn't shown: it's the model's, from the data's share of exact zeros.
                    Assert.AreEqual(zero, element.MixtureDistribution.Mixture.ZeroWeight, 1E-12, name + ": the zero mass");
                    SelectTab(view, "Distribution Results");
                    if (name == "Mixture Distribution - 2 Normals")
                        SelectParameter(Parts(view), "Weight (w₁)");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved component values"' settings and results tables: each fit's chains and thinning, posterior mean parameters,
        /// DIC, R-hat and ESS, 1% AEP and 90% bounds, with DE-MCzs, seed 12345, 1,750 warmup and 3,500 iterations and 10,000
        /// draws.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Name).ToArray(), Example.Elements<MixtureAnalysis>().Select(e => e.Name).ToArray(), "the fits");
                foreach (BayesianResult expected in Results)
                {
                    var (view, properties) = Example.Open<MixtureAnalysis, MixtureAnalysisControl, MixtureAnalysisPropertiesControl>(expected.Name);
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, warmup: "1750", iterations: "3500", draws: "10000");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>The zero-inflated input's histogram figure (its ten zeros included), which no area test compares.</summary>
        [TestMethod]
        public void Figures_ShowTheZeroInflatedHistogram()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>(ZeroInflatedData);
                var (view, _) = Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                SelectTab(view, "Histogram Plot");
                AssertBars(input.HistogramPlot, Figure(Folder + "images/mixture-distribution-examples-zero-input-histogram.plotspec.json.gz"), "the histogram");
            });
        }

        // The MCMC Report's parameter summary: each parameter's name and posterior mean, in the model's order.
        private static System.Collections.Generic.List<(string Name, string Mean)> ReportParameters(string report)
        {
            string[] lines = report.Split('\n');
            int header = Array.FindIndex(lines, l => l.Trim() == "PARAMETER SUMMARY STATISTICS");
            var rows = new System.Collections.Generic.List<(string, string)>();
            for (int i = header + 4; i < lines.Length && lines[i].Trim().Length > 0; i++)
            {
                // The name, padded, then Mean, Std Dev, the limits, Median, R-hat and ESS.
                string[] words = lines[i].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                rows.Add((string.Join(" ", words.Take(words.Length - 7)), words[^7]));
            }
            return rows;
        }

        // The walkthrough's component table: means and standard deviations in saved order, and the components' masses.
        private static readonly (string Name, string Input, (string Mean, string Sd)[] Components, string[] Masses, bool ZeroInflated)[] Components =
        {
            ("Mixture Distribution - 2 Normals", "Mixture of 2 Normals - Data", new[] { ("52.19778", "9.54935"), ("98.65123", "6.91164") }, new[] { "0.4331345", "0.5668655" }, false),
            ("Mixture Distribution - 3 Normals", "Mixture of 3 Normals - Data", new[] { ("150.90702", "9.26919"), ("51.36111", "14.24873"), ("101.42935", "9.13917") },
                new[] { "0.3120533", "0.2030666", "0.4848801" }, false),
            ("Mixture Distribution - 2 Normals - Zero-Inflated", ZeroInflatedData, new[] { ("48.87159", "11.59018"), ("99.04838", "9.59798") }, new[] { "0.3460540", "0.5539460" }, true),
        };

        // The walkthrough's settings and results tables (generic Value units).
        private static readonly BayesianResult[] Results =
        {
            new("Mixture Distribution - 2 Normals", "Posterior Mean", "113.201", "110.589", "116.292", "90%", "1.00030", "9,285", "838.221", "12", "60"),
            new("Mixture Distribution - 3 Normals", "Posterior Mean", "168.069", "163.59", "173.682", "90%", "1.00065", "5,702", "950.565", "18", "90"),
            new("Mixture Distribution - 2 Normals - Zero-Inflated", "Posterior Mean", "119.163", "115.526", "123.434", "90%", "1.00053", "9,283", "861.705", "12", "60"),
        };
    }
}
