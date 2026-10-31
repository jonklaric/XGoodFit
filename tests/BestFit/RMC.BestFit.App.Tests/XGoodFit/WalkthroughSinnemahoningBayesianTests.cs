using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Sinnemahoning Creek: Bayesian fits with an uncertain MOVE.3
    /// extension" (examples/4-univariate-distribution-analysis/1-univariate-analysis/4-measurement-errors/
    /// sinnemahoning-move3-bayesian.md), followed through the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures and the native exports are compared, point for point, by
    /// <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughSinnemahoningBayesianTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/4-measurement-errors/";
        private const string WithErrors = "Sinnemahoning - MOVE.3 - With Errors";
        private static readonly WalkthroughExample Example = new WalkthroughExample("sinnemahoning-move3-bayesian");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 1-3: the records in cfs, the 25 extension years 1914-1938 among the No Errors input's
        /// exact rows, and the same years in the With Errors input's uncertain data, each a LogNormal distribution with
        /// log10 standard deviation 0.074.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() => AssertSinnemahomingInputs(Example));
        }

        /// <summary>Checks the Sinnemahoning inputs, which the Bayesian and Bulletin 17C walkthroughs describe alike.</summary>
        internal static void AssertSinnemahomingInputs(WalkthroughExample example)
        {
            string[] extension = Enumerable.Range(1914, 25).Select(year => year.ToString(System.Globalization.CultureInfo.InvariantCulture)).ToArray();
            foreach (var (name, exact, first, uncertain) in new[]
            {
                ("Sinnemahoning - MOVE.3 - No Errors", 104, 1914, 0), (WithErrors, 79, 1939, 25), ("Sinnemahoning - No Extension", 79, 1939, 0),
            })
            {
                var (view, properties) = example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                AssertRecords(Records(view), exact, first, 2017, uncertain, 0, 0, 0, name);
                Assert.AreEqual("Flow (cfs)", Shown(properties.UnitLabel), name + ": the unit label");
                if (exact == 104)
                    CollectionAssert.AreEqual(extension, Enumerable.Range(0, 25).Select(row => Text(view.ExactDataGrid, row, 0)).ToArray(), name + ": the extension years");
                if (uncertain > 0)
                {
                    SelectTab(view, "Uncertain Data");
                    var rows = Rows(view.UncertainDataGrid);
                    CollectionAssert.AreEqual(extension, rows.Select(r => r[0]).ToArray(), name + ": the uncertain years");
                    foreach (string[] row in rows)
                        StringAssert.Matches(row[1], new System.Text.RegularExpressions.Regex(@"^LogN \([0-9.]+, 0\.074\)$"), name + ": " + row[0] + "'s distribution");
                }
                example.CloseDocuments();
            }
        }

        /// <summary>
        /// "Saved settings and results" and step 4: the three LP3 fits' 1% AEP and limits, posterior mean parameters and 90%
        /// interval, R-hat and ESS, and the sampler settings.
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
                    AssertResult(Parts(view), Parts(properties), expected);
                    AssertSampler(Parts(view), Parts(properties), expected.Name, chains: "6", thinning: "30", warmup: "1750", iterations: "3500", draws: "10000");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>LPIII - No Extension's parameter summary in the MCMC Report, and Select Parameter choosing Mean (of log) (µ).</summary>
        [TestMethod]
        public void FirstAnalysis_ReportsTheParameterSummaries()
        {
            Example.Run(() =>
            {
                const string name = "LPIII - No Extension";
                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(name);
                SelectTab(view, "MCMC Report");
                string report = ReportText(view.MCMCReportControl);
                AssertParameterSummary(report, "Mean (of log) (µ)", "4.129", "4.129", "4.093", "4.166", name);
                AssertParameterSummary(report, "Std Dev (of log) (σ)", "0.199", "0.197", "0.173", "0.23", name);
                AssertParameterSummary(report, "Skew (of log) (γ)", "0.417", "0.42", "-0.094", "0.92", name);
                SelectParameter(Parts(view), "Mean (of log) (µ)", "4.129", "4.129", "4.093", "4.166");
            });
        }

        /// <summary>
        /// The figures no area test compares: the With Errors input's chronology, and LPIII - With Errors' trace (its first
        /// parameter).
        /// </summary>
        [TestMethod]
        public void Figures_ShowTheChronologyAndTheTrace()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>(WithErrors);
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/sinnemahoning-move3-bayesian-uncertain-chronology.plotspec.json.gz"), "the chronology");
                Example.CloseDocuments();

                var (view, _) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>("LPIII - With Errors");
                SelectTab(view, "Markov Chain Traces");
                UnivariateAnalysisTesting.AssertFigure(view.MarkovChainTraceControl.Plot, Figure(Folder + "images/sinnemahoning-move3-bayesian-trace.plotspec.json.gz"), "the trace");
            });
        }

        // The walkthrough's table of saved results.
        private static readonly BayesianResult[] Results =
        {
            new("LPIII - No Extension", "Posterior Mean", "44,801.943", "34,688.792", "63,941.632", "90%", "1.00031", "9,262"),
            new("LPIII - No Errors", "Posterior Mean", "42,156.645", "34,184.869", "55,394.5", "90%", "1.00029", "9,436"),
            new("LPIII - With Errors", "Posterior Mean", "41,908.746", "33,738.768", "55,712.337", "90%", "0.99994", "9,072"),
        };
    }
}
