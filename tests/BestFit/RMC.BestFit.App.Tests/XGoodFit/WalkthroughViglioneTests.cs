using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Kamp at Zwettl: adding historical evidence and quantile priors"
    /// (examples/4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The walkthrough's figures of the analyses' plots are compared, point for point, by
    /// <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/> and
    /// <see cref="SharedAnalysisControlsTests.DiagnosticPlots_DrawTheStoredResults_AsRmcBestFitDid"/>; these tests read
    /// the walkthrough's values where it tells the user to, and compare the figures no other test does (the chronology
    /// and the trace's companion). docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughViglioneTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("viglione-et-al-2013");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 1-2: each input's records, the 2002 flood in the longer record, the Temporal Expansion
        /// inputs' three intervals and their 1600-1950 window (348 years below its threshold, none above), and the label
        /// the 1951-2005 inputs retain.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                foreach (var (name, exact, first, last, intervals, windows, unit) in new[]
                {
                    ("Systematic (1951-2001)", 51, 1951, 2001, 0, 0, "Peak Discharge (cms)"),
                    ("Systematic (1951-2005)", 55, 1951, 2005, 0, 0, "Value"),
                    ("Systematic (1951-2001) + Temporal Expansion", 51, 1951, 2001, 3, 1, "Peak Discharge (cms)"),
                    ("Systematic (1951-2005) + Temporal Expansion", 55, 1951, 2005, 3, 1, "Value"),
                })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), exact, first, last, 0, intervals, windows, 0, name);
                    // "The retained axis label is Value" (the 1951-2005 inputs); the others are labelled in m³/s.
                    Assert.AreEqual(unit, Shown(properties.UnitLabel), name + ": the unit label");
                    if (last == 2005)
                        Assert.AreEqual("2002", Row(view.ExactDataGrid, "2002")[0], name + ": the 2002 flood");
                    if (intervals > 0)
                    {
                        // "180–300 m³/s in 1655, 240–400 in 1803 and 202.5–337.5 in 1829."
                        SelectTab(view, "Interval Data");
                        var rows = Rows(view.IntervalDataGrid);
                        CollectionAssert.AreEqual(new[] { "1655", "1803", "1829" }, rows.Select(r => r[0]).ToArray(), name + ": the intervals' years");
                        CollectionAssert.AreEqual(new[] { "180", "240", "202.5" }, rows.Select(r => r[1]).ToArray(), name + ": the intervals' lower bounds");
                        CollectionAssert.AreEqual(new[] { "300", "400", "337.5" }, rows.Select(r => r[3]).ToArray(), name + ": the intervals' upper bounds");
                        // "The saved perception window spans 1600–1950 at 300 m³/s, with 348 events below and zero
                        // additional unentered events above": the 351 years less the three entered intervals.
                        SelectTab(view, "Threshold Data");
                        string[] window = Row(view.ThresholdDataGrid, 0);
                        Assert.AreEqual("1600|1950|300|0", string.Join("|", window), name + ": the window");
                        Assert.AreEqual(348, 1950 - 1600 + 1 - intervals - int.Parse(window[3]), name + ": the years below the threshold");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved settings and results" and step 5: each alternative's GEV model, point estimator, 1% AEP and limits,
        /// interval, largest R-hat and smallest ESS, and the sampler settings (DE-MCzs, six chains, thinning 30, seed
        /// 12345, 10,000 draws, Jeffreys' rule; 1,750/3,500 warmup/iterations but 1,200/2,400 for the three-prior fit).
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResultsAndSettings()
        {
            Example.Run(() =>
            {
                // The walkthrough compares nine GEV analyses; the project also holds a tenth, "Univariate Analysis_10" (on
                // the 1951-2005 Temporal Expansion input), which the explorer lists as RMC-BestFit's does.
                CollectionAssert.AreEqual(Results.Select(r => r.Name).Append("Univariate Analysis_10").ToArray(),
                    Example.Elements<UnivariateAnalysis>().Select(e => e.Name).ToArray(), "the project's univariate analyses");
                foreach (BayesianResult expected in Results)
                {
                    var (view, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                    Assert.AreEqual("Generalized Extreme Value", Shown(properties.Distribution), expected.Name + ": the model");
                    AssertResult(Parts(view), Parts(properties), expected);
                    bool three = expected.Name.EndsWith("3 Quantile Priors", System.StringComparison.Ordinal);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, chains: "6", thinning: "30",
                        warmup: three ? "1200" : "1750", iterations: three ? "2400" : "3500", draws: "10000");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Find these results" and the parameter summary table: the MCMC Report's posterior means, medians and limits of
        /// MCMC - Systematic (1951-2001), and Select Parameter choosing Location (ξ) in the Kernel Density, Markov Chain
        /// Traces and Autocorrelation views (whose plots are compared with the native exports elsewhere): the density's
        /// summary shows the same values.
        /// </summary>
        [TestMethod]
        public void FirstAnalysis_ReportsTheParameterSummaries()
        {
            Example.Run(() =>
            {
                const string name = "MCMC - Systematic (1951-2001)";
                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(name);
                SelectTab(view, "MCMC Report");
                string report = ReportText(view.MCMCReportControl);
                AssertParameterSummary(report, "Location (ξ)", "42.871", "42.789", "37.496", "48.405", name);
                AssertParameterSummary(report, "Scale (α)", "21.185", "20.995", "17.102", "25.894", name);
                AssertParameterSummary(report, "Shape (κ)", "-0.119", "-0.108", "-0.348", "0.075", name);

                SelectParameter(Parts(view), "Location (ξ)", "42.871", "42.789", "37.496", "48.405");
            });
        }

        /// <summary>
        /// The quantile priors and step 4: the four Causal alternatives' Normal prior at AEP 0.002 (mean 480, SD 80), the
        /// three-prior alternative's three, and no quantile prior in the other four.
        /// </summary>
        [TestMethod]
        public void QuantilePriors_AreTheWalkthroughsInformation()
        {
            Example.Run(() =>
            {
                foreach (BayesianResult result in Results)
                {
                    var (_, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(result.Name);
                    string[] expected = result.Name.Contains("Causal", System.StringComparison.Ordinal) ? new[] { "0.002|N (480, 80)" }
                        : result.Name.EndsWith("3 Quantile Priors", System.StringComparison.Ordinal) ? new[] { "0.1|N (100, 20)", "0.01|N (250, 40)", "0.001|N (500, 60)" }
                        : null;
                    Assert.AreEqual(expected != null, QuantilePriorsEnabled(properties.QuantilePriorsControl), result.Name + ": quantile priors enabled");
                    if (expected != null)
                        CollectionAssert.AreEqual(expected, Priors(properties.QuantilePriorsControl).Select(r => string.Join("|", r)).ToArray(), result.Name + ": the quantile priors");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>Step 6: the four Fit - elements are distribution-fitting comparisons on the inputs they name.</summary>
        [TestMethod]
        public void FitElements_FitTheNamedInputs()
        {
            Example.Run(() =>
            {
                var fits = Example.Elements<FittingAnalysis>();
                Assert.AreEqual(4, fits.Count, "the Fit - elements");
                foreach (FittingAnalysis fit in fits)
                {
                    var (_, properties) = Example.Open<FittingAnalysisControl, FittingAnalysisPropertiesControl>(fit);
                    Assert.AreEqual(fit.Name.Substring("Fit - ".Length), Shown(properties.InputDataComboBox), fit.Name + ": its input data");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// The walkthrough's figures that no area test compares: the Temporal Expansion input's chronology, and the
        /// companion of the native trace (MCMC - Systematic (1951-2001), Location (ξ)).
        /// </summary>
        [TestMethod]
        public void Figures_ShowTheChronologyAndTheTrace()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("Systematic (1951-2001) + Temporal Expansion");
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/viglione-et-al-2013-historical-chronology.plotspec.json.gz"), "the chronology");
                Example.CloseDocuments();

                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>("MCMC - Systematic (1951-2001)");
                SelectTab(view, "Markov Chain Traces");
                UnivariateAnalysisTesting.AssertFigure(view.MarkovChainTraceControl.Plot, Figure(Folder + "images/viglione-et-al-2013-trace.plotspec.json.gz"), "the trace");
            });
        }

        // The walkthrough's table of saved results: 1% AEP point, limits, interval, largest R-hat and smallest ESS.
        private static readonly BayesianResult[] Results =
        {
            new("MCMC - Systematic (1951-2001)", "Posterior Mean", "172.726", "127.463", "285.787", "90%", "1.00015", "9,429"),
            new("MCMC - Systematic (1951-2005)", "Posterior Mode", "251.85", "180.951", "519.734", "90%", "1.00036", "9,057"),
            new("MCMC - Systematic (1951-2001) + Temporal", "Posterior Mean", "221.1", "174.631", "289.824", "90%", "1.00037", "9,230"),
            new("MCMC - Systematic (1951-2005) + Temporal", "Posterior Mean", "249.898", "195.755", "331.057", "90%", "1.00036", "9,642"),
            new("MCMC - Systematic (1951-2001) + Causal", "Posterior Mean", "246.934", "192.085", "304.608", "90%", "1.00105", "9,097"),
            new("MCMC - Systematic (1951-2005) + Causal", "Posterior Mean", "263.651", "213.74", "313.861", "90%", "1.00026", "9,538"),
            new("MCMC - Systematic (1951-2001) + Temporal + Causal", "Posterior Mean", "250.039", "204.608", "298.046", "90%", "1.00027", "9,339"),
            new("MCMC - Systematic (1951-2005) + Temporal + Causal", "Posterior Mean", "260.861", "217.955", "306.113", "90%", "1.00043", "9,591"),
            new("MCMC - Systematic (1951-2001) + 3 Quantile Priors", "Posterior Mean", "230.439", "185.761", "283.794", "95%", "1.00052", "8,952"),
        };
    }
}
