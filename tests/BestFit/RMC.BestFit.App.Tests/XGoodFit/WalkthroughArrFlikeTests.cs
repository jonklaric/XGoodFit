using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "ARR and FLIKE: systematic records, historical counts and regional
    /// skew" (examples/4-univariate-distribution-analysis/1-univariate-analysis/2-arr-flike/arr-flike-examples.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures and the native exports are compared, point for point, by
    /// <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughArrFlikeTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/2-arr-flike/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("arr-flike-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 1, 2 and 4: each input's records in m³/s, Example #4's 1820-1937 window at 12,525 m³/s
        /// (117 years below, one unentered event above), and Example #6b's 27 low outlier flags.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                foreach (var (name, exact, first, last, windows, flags) in new[]
                {
                    ("Example #3", 31, 1938, 1968, 0, 0), ("Example #4", 31, 1938, 1968, 1, 0),
                    ("Example #6a", 56, 1960, 2015, 0, 0), ("Example #6b", 56, 1960, 2015, 0, 27),
                })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), exact, first, last, 0, 0, windows, flags, name);
                    Assert.AreEqual("Flow (cms)", Shown(properties.UnitLabel), name + ": the unit label");
                    if (windows > 0)
                    {
                        SelectTab(view, "Threshold Data");
                        string[] window = Row(view.ThresholdDataGrid, 0);
                        Assert.AreEqual("1820|1937|12,525|1", string.Join("|", window), name + ": the historical window");
                        Assert.AreEqual(117, 1937 - 1820 + 1 - int.Parse(window[3]), name + ": the years below the threshold");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved settings and results" and steps 1, 3 and 4: the five Bayesian alternatives (and no Bulletin 17C one), their
        /// models and inputs, 1% AEP and limits, interval, R-hat and ESS, and the sampler settings.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResultsAndSettings()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Result.Name).ToArray(), Example.Elements<UnivariateAnalysis>().Select(e => e.Name).ToArray(), "the Bayesian alternatives");
                Assert.AreEqual(0, Example.Elements<B17CAnalysis>().Count, "no Bulletin 17C alternatives");
                foreach (var (expected, model, input) in Results)
                {
                    var (view, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                    Assert.AreEqual(model, Shown(properties.Distribution), expected.Name + ": the model");
                    Assert.AreEqual(input, Shown(properties.InputDataComboBox), expected.Name + ": its input");
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, chains: "6", thinning: "30", warmup: "1750", iterations: "3500", draws: "10000");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>Example #3's parameter summary in the MCMC Report, and Select Parameter choosing Mean (of log) (µ).</summary>
        [TestMethod]
        public void FirstAnalysis_ReportsTheParameterSummaries()
        {
            Example.Run(() =>
            {
                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>("Example #3");
                SelectTab(view, "MCMC Report");
                string report = ReportText(view.MCMCReportControl);
                AssertParameterSummary(report, "Mean (of log) (µ)", "2.79", "2.79", "2.605", "2.974", "Example #3");
                AssertParameterSummary(report, "Std Dev (of log) (σ)", "0.626", "0.614", "0.497", "0.795", "Example #3");
                AssertParameterSummary(report, "Skew (of log) (γ)", "0.116", "0.099", "-0.666", "0.932", "Example #3");
                SelectParameter(Parts(view), "Mean (of log) (µ)", "2.79", "2.79", "2.605", "2.974");
            });
        }

        /// <summary>
        /// Step 3 and the source notes: Example #5's skew prior is Normal(0, 0.30), with default flat priors off; the others
        /// keep the default flat priors. No alternative has quantile priors.
        /// </summary>
        [TestMethod]
        public void Example5_HasTheRegionalSkewPrior()
        {
            Example.Run(() =>
            {
                foreach (var (result, _, _) in Results)
                {
                    var (_, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(result.Name);
                    bool regional = result.Name == "Example #5";
                    Assert.AreEqual(!regional, Ticked(properties.ParameterPriorsControl.UsePriorDistributionDefaults), result.Name + ": default flat priors");
                    if (regional)
                        CollectionAssert.AreEqual(new[] { "Skew (of log) (γ)", "N (0, 0.3)" }, Priors(properties.ParameterPriorsControl).Single(r => r[0].StartsWith("Skew", System.StringComparison.Ordinal)), "Example #5's skew prior");
                    Assert.IsFalse(QuantilePriorsEnabled(properties.QuantilePriorsControl), result.Name + ": no quantile priors");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>The trace figure's companion (Example #3, Mean (of log) (µ)), which no area test compares.</summary>
        [TestMethod]
        public void Figures_ShowTheTrace()
        {
            Example.Run(() =>
            {
                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>("Example #3");
                SelectTab(view, "Markov Chain Traces");
                UnivariateAnalysisTesting.AssertFigure(view.MarkovChainTraceControl.Plot, Figure(Folder + "images/arr-flike-examples-trace.plotspec.json.gz"), "the trace");
            });
        }

        // The walkthrough's table of saved results, with each alternative's model and input.
        private static readonly (BayesianResult Result, string Model, string Input)[] Results =
        {
            (new("Example #3", "Posterior Mean", "19,933.992", "7,239.394", "102,334.579", "90%", "1.00043", "9,157"), "Log-Pearson Type III", "Example #3"),
            (new("Example #4", "Posterior Mean", "13,787.128", "7,754.179", "27,214.116", "90%", "1.00031", "8,745"), "Log-Pearson Type III", "Example #4"),
            (new("Example #5", "Posterior Mean", "15,931.58", "7,180.378", "45,324.538", "90%", "1.00022", "9,715"), "Log-Pearson Type III", "Example #3"),
            (new("Example #6a", "Posterior Mean", "2,590.881", "741.838", "12,838.827", "90%", "1.00009", "9,079"), "Generalized Extreme Value", "Example #6a"),
            (new("Example #6b", "Posterior Mean", "546.057", "354.35", "1,141.581", "90%", "1.00062", "8,439"), "Generalized Extreme Value", "Example #6b"),
        };
    }
}
