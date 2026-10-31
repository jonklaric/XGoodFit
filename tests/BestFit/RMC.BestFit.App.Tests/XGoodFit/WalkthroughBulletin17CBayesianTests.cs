using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Bulletin 17C datasets: Bayesian fits and a GMM comparison"
    /// (examples/4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/
    /// bulletin-17c-bayesian-examples.md), followed through the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures (but the Moose River one, which doesn't show the saved results) and the native exports are
    /// compared, point for point, by <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBulletin17CBayesianTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/3-bulletin17C-examples/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("bulletin-17c-bayesian-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 2-3: each dataset's records in cfs, Orestimba Creek's 30 low outlier flags and retained
        /// threshold, and American River's five intervals (650, 1437, 1574, 1711 and 1862).
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() => AssertBulletin17CInputs(Example, ""));
        }

        /// <summary>Checks the seven Bulletin 17C datasets, which the Bayesian and GMM walkthroughs describe alike.</summary>
        /// <param name="example">The example.</param>
        /// <param name="suffix">The inputs' names after "Example #n" (the GMM project's are "Example #n - Data").</param>
        internal static void AssertBulletin17CInputs(WalkthroughExample example, string suffix)
        {
            foreach (var (number, exact, first, last, intervals, windows, flags) in new[]
            {
                (1, 68, 1947, 2014, 0, 0, 0), (2, 82, 1932, 2013, 0, 0, 30), (3, 56, 1929, 2012, 0, 3, 2), (4, 81, 1895, 1976, 4, 4, 0),
                (5, 50, 1965, 2014, 0, 9, 9), (6, 65, 1949, 2013, 0, 1, 10), (7, 77, 1905, 1997, 5, 10, 0),
            })
            {
                string name = "Example #" + number + suffix;
                var (view, properties) = example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                AssertRecords(Records(view), exact, first, last, 0, intervals, windows, flags, name);
                Assert.AreEqual("Peak Flow (cfs)", Shown(properties.UnitLabel), name + ": the unit label");
                if (number == 2)
                    Assert.IsTrue(Parse(Shown(properties.LOThresholdValue)) > 0, name + ": the retained low outlier threshold");
                if (number == 7)
                {
                    SelectTab(view, "Interval Data");
                    CollectionAssert.AreEqual(new[] { "650", "1437", "1574", "1711", "1862" }, Rows(view.IntervalDataGrid).Select(r => r[0]).ToArray(), name + ": the intervals' years");
                }
                example.CloseDocuments();
            }
        }

        /// <summary>
        /// "Saved settings and results": the six Bayesian LP3 fits' 1% AEP and limits, interval, R-hat and ESS, and the
        /// sampler settings. Bayes Example #1's row is the saved fit RMC-BestFit 2.0.1 shows (see <see cref="Results"/>).
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResultsAndSettings()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Name).ToArray(), Example.Elements<UnivariateAnalysis>().Select(e => e.Name).ToArray(), "the Bayesian alternatives");
                foreach (BayesianResult expected in Results)
                {
                    var (view, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                    Assert.AreEqual("Log-Pearson Type III", Shown(properties.Distribution), expected.Name + ": the model");
                    Assert.AreEqual("Example #" + expected.Name.Last(), Shown(properties.InputDataComboBox), expected.Name + ": its input");
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, chains: "6", thinning: "30", warmup: "1750", iterations: "3500", draws: "10000");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Bayes Example #1's parameter summary in the MCMC Report, and Select Parameter choosing Mean (of log) (µ): the
        /// saved fit's, which the walkthrough's table doesn't show (it has 3.329, 3.329, 3.3 and 3.359 for the log mean;
        /// 0.145, 0.144, 0.124 and 0.171 for the log standard deviation; 0.495, 0.508, -0.042 and 0.982 for the skew: the
        /// earlier fit with default flat priors, see <see cref="Results"/>).
        /// </summary>
        [TestMethod]
        public void FirstAnalysis_ReportsTheParameterSummaries()
        {
            Example.Run(() =>
            {
                const string name = "Bayes Example #1";
                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(name);
                SelectTab(view, "MCMC Report");
                string report = ReportText(view.MCMCReportControl);
                AssertParameterSummary(report, "Mean (of log) (µ)", "3.329", "3.329", "3.302", "3.357", name);
                AssertParameterSummary(report, "Std Dev (of log) (σ)", "0.142", "0.141", "0.122", "0.164", name);
                AssertParameterSummary(report, "Skew (of log) (γ)", "0.228", "0.227", "-0.144", "0.603", name);
                SelectParameter(Parts(view), "Mean (of log) (µ)", "3.329", "3.329", "3.302", "3.357");
            });
        }

        /// <summary>
        /// Step 5 and the limits section: Bayes Example #2's custom uniform priors (log-mean about 0-4, log-standard deviation
        /// about 0-3, skew -6-6) with Jeffreys' rule, and the default flat priors of Bayes Examples #4-#7. The walkthrough says
        /// all five others keep default flat priors, but the saved Bayes Example #1 has a Normal(0, 0.3) skew prior with
        /// default flat priors off, as RMC-BestFit 2.0.1 shows (see <see cref="Results"/>).
        /// </summary>
        [TestMethod]
        public void BayesExample2_HasCustomUniformPriors()
        {
            Example.Run(() =>
            {
                foreach (BayesianResult result in Results)
                {
                    var (_, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(result.Name);
                    bool custom = result.Name == "Bayes Example #2", regional = result.Name == "Bayes Example #1";
                    Assert.AreEqual(!custom && !regional, Ticked(properties.ParameterPriorsControl.UsePriorDistributionDefaults), result.Name + ": default flat priors");
                    if (regional)
                        CollectionAssert.AreEqual(new[] { "Mean (of log) (µ)|U (0, 5)", "Std Dev (of log) (σ)|U (0, 2)", "Skew (of log) (γ)|N (0, 0.3)" },
                            Priors(properties.ParameterPriorsControl).Select(r => string.Join("|", r)).ToArray(), result.Name + ": its priors");
                    if (custom)
                    {
                        Assert.IsTrue(Ticked(properties.UseJeffreysRule), result.Name + ": Jeffreys' rule");
                        var priors = Priors(properties.ParameterPriorsControl);
                        foreach (var (parameter, lower, upper) in new[] { ("Mean (of log) (µ)", 0, 4), ("Std Dev (of log) (σ)", 0, 3), ("Skew (of log) (γ)", -6, 6) })
                        {
                            string prior = priors.Single(r => r[0] == parameter)[1];
                            var match = System.Text.RegularExpressions.Regex.Match(prior, @"^U \((\S+), (\S+)\)$");
                            Assert.IsTrue(match.Success, parameter + ": a uniform prior, not " + prior);
                            Assert.AreEqual(lower, System.Math.Round(Parse(match.Groups[1].Value)), parameter + "'s lower bound, " + prior);
                            Assert.AreEqual(upper, System.Math.Round(Parse(match.Groups[2].Value)), parameter + "'s upper bound, " + prior);
                        }
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 4 and the Back Creek notes: B17C Example #3 is selectable, but neither RMC-BestFit 2.0.1 nor XGoodFit loads
        /// its legacy result row (IssuesToRaise/RMC-BestFit-b17c-example-3-loses-results-on-save.md): it isn't estimated and
        /// its frequency plot draws no curve. The walkthrough's Back Creek figure overlays the saved arrays on the desktop's
        /// observations, which the plot draws as the figure has them.
        /// </summary>
        [TestMethod]
        public void BackCreekGmm_ShowsTheObservationsWithoutItsLegacyFit()
        {
            Example.Run(() =>
            {
                var b17c = Example.Element<B17CAnalysis>("B17C Example #3");
                var (view, properties) = Example.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(b17c);
                Assert.IsFalse(b17c.IsEstimated, "the legacy fit isn't loaded");
                var figure = Figure(Folder + "images/bulletin-17c-bayesian-examples-back-creek-gmm.plotspec.json.gz");
                foreach (string title in new[] { "Exact Data", "Low Outlier Data" })
                    AssertSeries(b17c.FrequencyPlot, figure, title, "B17C Example #3");
                foreach (string curve in new[] { "90% Confidence Intervals", "Expected Probability", "Computed" })
                    Assert.AreEqual(0, PointCount(b17c.FrequencyPlot, curve), "B17C Example #3 draws no " + curve);
                SelectTab(view, "GMM Report");
                Assert.AreEqual("", ReportText(view.GMMReportControl), "no GMM report");
            });
        }

        /// <summary>
        /// The figure no area test compares: American River's chronology. The trace's companion
        /// (images/bulletin-17c-bayesian-examples-trace) isn't compared: like the Moose River figure it shows the earlier
        /// Bayes Example #1 (its first draw is 3.32834 where the saved chains and the native export,
        /// screenshots/bulletin-17c-bayesian-trace-mean, which the area tests compare, have 3.35128).
        /// </summary>
        [TestMethod]
        public void Figures_ShowTheChronology()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("Example #7");
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/bulletin-17c-bayesian-examples-american-chronology.plotspec.json.gz"), "the chronology");
            });
        }

        // The walkthrough's table of saved results. Its Bayes Example #1 row (5,231.877 cfs, 4,248.95-6,902.67, R-hat 0.99988,
        // ESS 9,300), like its parameter summary, Moose River figure and trace figure, isn't the saved fit: the project's
        // Bayes Example #1 has a Normal(0, 0.3) skew prior (last modified 30 September 2026), whose results RMC-BestFit
        // 2.0.1 shows and exported (screenshots/bulletin-17c-bayesian-*), and XGoodFit shows. The row here is what they show.
        private static readonly BayesianResult[] Results =
        {
            new("Bayes Example #1", "Posterior Mean", "4,806.606", "4,139.069", "5,777.893", "90%", "1.0009", "9,515"),
            new("Bayes Example #2", "Posterior Mean", "12,999.086", "9,898.143", "18,829.796", "90%", "1.00013", "7,452"),
            new("Bayes Example #4", "Posterior Mean", "37,433.876", "28,905.245", "50,444.26", "90%", "1.00057", "9,138"),
            new("Bayes Example #5", "Posterior Mean", "4,372.461", "3,930.256", "5,283.586", "90%", "1.00014", "7,941"),
            new("Bayes Example #6", "Posterior Mean", "12,300.194", "8,330.79", "20,737.466", "90%", "1.00003", "8,573"),
            new("Bayes Example #7", "Posterior Mean", "320,554.217", "274,138.344", "369,195.561", "90%", "1.00005", "9,373"),
        };
    }
}
