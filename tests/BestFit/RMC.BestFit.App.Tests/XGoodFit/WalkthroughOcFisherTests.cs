using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "OC Fisher Dam: trend assumptions and quantile priors"
    /// (examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-oc-fisher-dam.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The analyses' frequency and chronology figures and the native exports are compared, point for point, by
    /// <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughOcFisherTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/";
        private const string Input = "OC Fisher Inflows";
        private static readonly WalkthroughExample Example = new WalkthroughExample("nsffa-oc-fisher-dam");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and step 1: 106 annual values during 1916-2021 in m³/s, and the 1853-1915 window at 1,150 m³/s
        /// with 62 years below and one undated exceedance.
        /// </summary>
        [TestMethod]
        public void Input_HoldsTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(Input);
                AssertRecords(Records(view), 106, 1916, 2021, 0, 0, 1, 0, Input);
                Assert.AreEqual("Flow (cms)", Shown(properties.UnitLabel), "the unit label");
                SelectTab(view, "Threshold Data");
                string[] window = Row(view.ThresholdDataGrid, 0);
                Assert.AreEqual("1853|1915|1,150|1", string.Join("|", window), "the historical window");
                Assert.AreEqual(62, 1915 - 1853 + 1 - int.Parse(window[3]), "the years below the threshold");
            });
        }

        /// <summary>
        /// Steps 2-4, the trend table and the quantile priors: each alternative's trend models (only the log mean's
        /// changes), start index 1853 and evaluation index (2021, or 0 for the stationary SFFA), alpha 0.5, and the
        /// LnNormal priors (LN in the app) at AEP 0.1, 0.01 and 0.001 that all five enable (SFFA's means 275, 1014 and 2330, the NSFFA
        /// alternatives' 261, 963 and 2214, with standard deviations 164, 550 and 1252).
        /// </summary>
        [TestMethod]
        public void Analyses_HaveTheWalkthroughsTrendsAndPriors()
        {
            Example.Run(() =>
            {
                foreach (var (name, types) in Trends)
                {
                    var element = Example.Element<UnivariateAnalysis>(name);
                    var (_, properties) = Example.Open<UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(element);
                    bool stationary = name == "SFFA";
                    Assert.AreEqual(Input, Shown(properties.InputDataComboBox), name + ": its input");
                    Assert.AreEqual("Log-Pearson Type III", Shown(properties.Distribution), name + ": the model");
                    Assert.IsTrue(QuantilePriorsEnabled(properties.QuantilePriorsControl), name + ": quantile priors");
                    string[] means = stationary ? new[] { "275", "1014", "2330" } : new[] { "261", "963", "2214" };
                    CollectionAssert.AreEqual(new[] { "0.1", "0.01", "0.001" }.Zip(means, new[] { "164", "550", "1252" })
                        .Select(p => $"{p.First}|LN ({p.Second}, {p.Third})").ToArray(),
                        Priors(properties.QuantilePriorsControl).Select(r => string.Join("|", r)).ToArray(), name + ": the quantile priors");
                    AssertTrends(element, properties, !stationary, types, 1853, stationary ? "0" : "2021");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved individual results" and step 5: each fit's chains, thinning and draws, DIC, 1% AEP and limits, R-hat and
        /// ESS, with DE-MCzs, seed 12345, posterior mean parameters and 90% intervals; the sinusoidal fit's frequency
        /// coefficient of about 0.0064835 cycles a year (a period of about 154 years); and Select Parameter choosing
        /// Mean (of log) (µ) for SFFA.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Name).ToArray(), Example.Elements<UnivariateAnalysis>().Select(e => e.Name).ToArray(), "the alternatives");
                foreach (BayesianResult expected in Results)
                {
                    var (view, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, jeffreys: null);
                    if (expected.Name == "SFFA")
                        SelectParameter(Parts(view), "Mean (of log) (µ)");
                    if (expected.Name == "NSFFA - Sinusoidal Trend")
                    {
                        // The point estimate is the posterior mean, which the MCMC Report shows to six significant digits.
                        SelectTab(view, "MCMC Report");
                        string frequency = ReportRow(ReportText(view.MCMCReportControl), "PARAMETER SUMMARY STATISTICS", "Mean (of log) (µ) (γ)")[0];
                        AssertAgrees("0.0064835", frequency, "the sinusoid's frequency");
                        Assert.AreEqual(154, System.Math.Round(1 / Parse(frequency)), "the sinusoid's period");
                        SelectTab(view, "Distribution Results");
                        SelectTab(view, "Tabular Results");
                        AssertAgrees("0.0064835", Statistic(view.SummaryStatisticsTable, "Mean (of log) (µ) (γ)"), "the sinusoid's frequency in the summary");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 6: the model average's components (Linear, Sinusoidal and Step, with DIC weights 0.003153451, 0.961453939
        /// and 0.035392610) and its empty result: it isn't estimated, and its plot and table show no curve.
        /// </summary>
        [TestMethod]
        public void ModelAverage_HasItsComponentsButNoResult()
        {
            Example.Run(() =>
            {
                var average = Example.Element<CompositeAnalysis>("Bayesian Model Average");
                var (view, properties) = Example.Open<CompositeAnalysisControl, CompositeAnalysisPropertiesControl>(average);
                var rows = Rows(properties.CompositeFunctionDataGrid);
                CollectionAssert.AreEqual(new[] { "NSFFA - Linear Trend", "NSFFA - Sinusoidal Trend", "NSFFA - Step Function" }, rows.Select(r => r[0]).ToArray(), "the components");
                foreach (var (row, weight) in rows.Zip(new[] { "0.003153451", "0.961453939", "0.035392610" }))
                    AssertAgrees(weight, row[1], row[0] + "'s DIC weight");
                Assert.IsFalse(average.IsEstimated, "the model average has no saved result");
                SelectTab(view, "Tabular Results");
                Assert.AreEqual(0, Count(view.FrequencyCurveTable), "no frequency curve");
            });
        }

        /// <summary>The figures no area test compares: the input's chronology, and the sinusoidal fit's trace.</summary>
        [TestMethod]
        public void Figures_ShowTheChronologyAndTheSinusoidalTrace()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>(Input);
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/nsffa-oc-fisher-dam-historical-chronology.plotspec.json.gz"), "the chronology");
                Example.CloseDocuments();

                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>("NSFFA - Sinusoidal Trend");
                SelectTab(view, "Markov Chain Traces");
                UnivariateAnalysisTesting.AssertFigure(view.MarkovChainTraceControl.Plot, Figure(Folder + "images/nsffa-oc-fisher-dam-sinusoidal-trace.plotspec.json.gz"), "the trace");
            });
        }

        // The walkthrough's trend table: each alternative's trend models in the model's order (its StepFunction, the saved
        // name, is the app's Step Function).
        private static readonly (string Name, string[] Types)[] Trends =
        {
            ("SFFA", new[] { "Constant", "Constant", "Constant" }),
            ("NSFFA - Constant", new[] { "Constant", "Constant", "Constant" }),
            ("NSFFA - Linear Trend", new[] { "Linear", "Constant", "Constant" }),
            ("NSFFA - Step Function", new[] { "Step Function", "Constant", "Constant" }),
            ("NSFFA - Sinusoidal Trend", new[] { "Sinusoidal", "Constant", "Constant" }),
        };

        // The walkthrough's table of saved individual results (m³/s; the nonstationary ones at 2021).
        private static readonly BayesianResult[] Results =
        {
            new("SFFA", "Posterior Mean", "944.552", "624.787", "1,408.868", "90%", "1.00020", "9,545", "1,051.796", "6", "30", "10000"),
            new("NSFFA - Constant", "Posterior Mean", "933.205", "619.449", "1,396.259", "90%", "1.00050", "9,373", "1,051.838", "6", "30", "10000"),
            new("NSFFA - Linear Trend", "Posterior Mean", "401.6", "209.852", "747.362", "90%", "1.00032", "9,796", "1,034.034", "8", "40", "10000"),
            new("NSFFA - Step Function", "Posterior Mean", "426.637", "235.543", "767.055", "90%", "1.00026", "5,935", "1,029.198", "10", "50", "10000"),
            new("NSFFA - Sinusoidal Trend", "Posterior Mean", "286.469", "240.879", "879.679", "90%", "1.00426", "553", "1,022.595", "12", "60", "10000"),
        };
    }
}
