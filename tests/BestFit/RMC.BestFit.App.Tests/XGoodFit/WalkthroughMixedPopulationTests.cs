using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Mixed flood populations: a maximum and a mixture"
    /// (examples/4-univariate-distribution-analysis/5-composite-analysis/mixed-population-examples.md), followed through the
    /// main window.
    /// </summary>
    /// <remarks>
    /// The composites' frequency figures and native export are compared, point for point, by
    /// <see cref="CompositeAnalysisViewTests.FrequencyPlot_EqualsRmcBestFitsExport_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughMixedPopulationTests
    {
        private const string Folder = "4-univariate-distribution-analysis/5-composite-analysis/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("mixed-population-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table, the note under it and step 1: the six inputs' records and units; the full annual-maximum series
        /// is the index-wise maximum of the full snow and rain series at all 100 indexes; the pooled mixture is exactly the
        /// union of the 74 snow and 26 rain sub-sample records, and differs from the full annual maxima at every index.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                var series = new Dictionary<string, Dictionary<int, double>>();
                foreach (var (name, exact, first, last, unit) in new[]
                {
                    ("AMS Sub-Sample - Snow Driven Floods", 74, 1, 100, "Flow (cfs)"), ("Full POR - Snow Driven Floods", 100, 1, 100, "Flow (cfs)"),
                    ("Full POR - Rainfall Driven Floods", 100, 1, 100, "Flow (cfs)"), ("Full POR - Annual Max Series", 100, 1, 100, "Flow (cfs)"),
                    ("AMS Sub-Sample - Rain Driven Floods", 26, 2, 93, "Flow (cfs)"), ("AMS Mixture of Flood Types", 100, 1, 100, "Value"),
                })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), exact, first, last, 0, 0, 0, 0, name);
                    Assert.AreEqual(unit, Shown(properties.UnitLabel), name + ": the unit label");
                    series[name] = Enumerable.Range(0, exact).ToDictionary(row => int.Parse(Text(view.ExactDataGrid, row, 0), System.Globalization.CultureInfo.InvariantCulture),
                        row => Parse(Text(view.ExactDataGrid, row, 1)));
                    Example.CloseDocuments();
                }
                var snow = series["Full POR - Snow Driven Floods"];
                var rain = series["Full POR - Rainfall Driven Floods"];
                var maxima = series["Full POR - Annual Max Series"];
                foreach (int index in Enumerable.Range(1, 100))
                    Assert.AreEqual(Math.Max(snow[index], rain[index]), maxima[index], "the annual maximum at " + index);
                var pooled = series["AMS Mixture of Flood Types"];
                var union = series["AMS Sub-Sample - Snow Driven Floods"].Concat(series["AMS Sub-Sample - Rain Driven Floods"]).ToDictionary(p => p.Key, p => p.Value);
                CollectionAssert.AreEquivalent(union.ToList(), pooled.ToList(), "the pooled sample is the sub-samples' union");
                Assert.IsTrue(Enumerable.Range(1, 100).All(index => pooled[index] != maxima[index]), "the pooled sample differs from the full annual maxima at every index");
            });
        }

        /// <summary>
        /// Steps 2 and 4 and the marginal table: the four component fits are LogNormal, with their mean and standard
        /// deviation parameters (log10 space) as saved; their chains and thinning, posterior mean parameters, DIC, R-hat, ESS
        /// and 1% AEP with 90% bounds, with DE-MCzs, seed 12345, 1,750 warmup and 3,500 iterations and 10,000 draws.
        /// </summary>
        [TestMethod]
        public void ComponentFits_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                foreach (var (expected, input, mean, sd) in Marginals)
                {
                    var (view, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                    Assert.AreEqual(input, Shown(properties.InputDataComboBox), expected.Name + ": its input");
                    Assert.AreEqual("Log-Normal", Shown(properties.Distribution), expected.Name + ": the model");
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, warmup: "1750", iterations: "3500", draws: "10000", jeffreys: null);
                    SelectTab(view, "MCMC Report");
                    string report = ReportText(view.MCMCReportControl);
                    var names = view.KernelDensityControl.ParameterComboBox.Items.Cast<object>().Select(i => i.ToString()).ToList();
                    Assert.AreEqual(2, names.Count, expected.Name + ": two parameters, " + string.Join(", ", names));
                    AssertAgrees(mean, ReportRow(report, "PARAMETER SUMMARY STATISTICS", names[0])[0], expected.Name + ": the mean parameter");
                    AssertAgrees(sd, ReportRow(report, "PARAMETER SUMMARY STATISTICS", names[1])[0], expected.Name + ": the standard deviation parameter");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Steps 3-5 and the composites' results: Competing Flood Types combines the full-record fits as an independent maximum;
        /// Mixture of Flood Types weights the sub-sample fits 0.75 and 0.25; the model-average method is inactive (not shown)
        /// for both; each has its saved curve, 1% AEP and 90% bounds, and no MCMC Report of its own.
        /// </summary>
        [TestMethod]
        public void Composites_CombineTheirComponents()
        {
            Example.Run(() =>
            {
                foreach (var (name, type, maximum, components, weights, point, lower, upper) in new[]
                {
                    ("Competing Flood Types", "Competing Risks", true, new[] { "Full POR Snow Driven", "Full POR Rainfall Driven" }, new[] { "", "" }, "3,429.371", "2,553.787", "4,840.352"),
                    ("Mixture of Flood Types", "Mixture Distribution", false, new[] { "Sub-Sample - Snow Driven", "Sub-Sample - Rain Driven" }, new[] { "0.75", "0.25" }, "2,503.944", "1,762.428", "4,774.472"),
                })
                {
                    var composite = Example.Element<CompositeAnalysis>(name);
                    var (view, properties) = Example.Open<CompositeAnalysisControl, CompositeAnalysisPropertiesControl>(composite);
                    Assert.AreEqual(type, Shown(properties.CompositeType), name + ": the combination");
                    Assert.IsFalse(properties.AverageMethod.IsEffectivelyVisible, name + ": no model-average method");
                    if (maximum)
                    {
                        Assert.AreEqual("Independent", Shown(properties.Dependency), name + ": the dependence");
                        Assert.IsTrue(Ticked(properties.IsMaximum), name + ": a maximum");
                    }
                    var rows = Rows(properties.CompositeFunctionDataGrid);
                    CollectionAssert.AreEqual(components, rows.Select(r => r[0]).ToArray(), name + ": the components");
                    CollectionAssert.AreEqual(weights, rows.Select(r => r[1]).ToArray(), name + ": the weights");
                    Assert.IsTrue(composite.IsEstimated, name + ": a saved curve");
                    SelectTab(view, "Tabular Results");
                    AssertIntervalWidth(view.FrequencyCurveTable, "90%", name);
                    AssertQuantile(view.FrequencyCurveTable, "0.01", point, lower, upper, name);
                    Assert.IsFalse(Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(view).OfType<MCMCReportControl>().Any(), name + ": no MCMC Report");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// The figures no area test compares: the full annual maxima's chronology, and the full-record snow and rain fits'
        /// frequency plots.
        /// </summary>
        [TestMethod]
        public void Figures_ShowTheChronologyAndTheComponentFits()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("Full POR - Annual Max Series");
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/mixed-population-examples-full-maximum-chronology.plotspec.json.gz"), "the chronology");
                Example.CloseDocuments();
                foreach (var (name, figure) in new[] { ("Full POR Snow Driven", "snow-component"), ("Full POR Rainfall Driven", "rain-component") })
                {
                    var fit = Example.Element<UnivariateAnalysis>(name);
                    Example.Open<UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(fit);
                    UnivariateAnalysisTesting.AssertFigure(fit.FrequencyPlot, Figure(Folder + $"images/mixed-population-examples-{figure}.plotspec.json.gz"), name);
                    Example.CloseDocuments();
                }
            });
        }

        // The walkthrough's marginal fits: their results (chains and thinning, DIC, R-hat, ESS, 1% AEP and bounds), inputs and
        // saved mean and standard deviation parameters.
        private static readonly (BayesianResult Result, string Input, string Mean, string Sd)[] Marginals =
        {
            (new("Full POR Snow Driven", "Posterior Mean", "1,527.94", "1,341.072", "1,770.208", "90%", "1.00016", "9,080", "1,382.872", "4", "20"), "Full POR - Snow Driven Floods", "2.749", "0.187"),
            (new("Full POR Rainfall Driven", "Posterior Mean", "3,427.486", "2,527.793", "4,839.752", "90%", "1.00067", "9,500", "1,443.738", "4", "20"), "Full POR - Rainfall Driven Floods", "2.509", "0.441"),
            (new("Sub-Sample - Snow Driven", "Posterior Mean", "1,585.612", "1,341.873", "1,918.701", "90%", "1.00035", "9,361", "1,028.466", "4", "20"), "AMS Sub-Sample - Snow Driven Floods", "2.718", "0.207"),
            (new("Sub-Sample - Rain Driven", "Posterior Mean", "4,609.005", "2,470.697", "10,329.904", "90%", "1.00076", "9,347", "385.965", "4", "20"), "AMS Sub-Sample - Rain Driven Floods", "2.558", "0.475"),
        };
    }
}
