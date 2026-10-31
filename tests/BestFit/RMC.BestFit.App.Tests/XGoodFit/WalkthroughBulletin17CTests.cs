using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Bulletin 17C datasets: GMM fits and uncertainty methods"
    /// (examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures and the native exports are compared, point for point, by
    /// <see cref="B17CAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBulletin17CTests
    {
        private const string Folder = "4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("bulletin-17c-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 1 and 4: the seven datasets' records in cfs (American River's five intervals among
        /// them), and the manual low outlier thresholds Examples 3 and 5 retain.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() => WalkthroughBulletin17CBayesianTests.AssertBulletin17CInputs(Example, " - Data", 3, 5));
        }

        /// <summary>
        /// "Saved configuration and results" and steps 1, 3 and 4: each parent fit's uncertainty method, draws, optimizer
        /// status, 1% AEP and confidence limits (the BCB copies' computed curves are their parents'), seed 12345 and 90%
        /// interval, with iterative GMM and, as the model holds them (the app doesn't show them), BFGS, at most 100 GMM
        /// iterations and tolerances of 1e-8. Example #2 - BCB, which the walkthrough lists too, has no saved result: see
        /// <see cref="Example2Bcb_HasNoSavedResult_AndItsEstimateIsntTheWalkthroughs"/>.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Name).ToArray(), Example.Elements<B17CAnalysis>().Select(e => e.Name).ToArray(), "the fits");
                foreach (GmmResult expected in Results.Where(r => r.Name != "Example #2 - BCB"))
                {
                    var element = Example.Element<B17CAnalysis>(expected.Name);
                    var (view, properties) = Example.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(element);
                    string data = expected.Name.Substring(0, "Example #n".Length) + " - Data";
                    Assert.AreEqual(data, Shown(properties.InputDataComboBox), expected.Name + ": its input");
                    Assert.AreEqual("Log-Pearson Type III", Shown(properties.Distribution), expected.Name + ": the model");
                    AssertGmmResult(view, properties, expected);
                    AssertGmmSettings(element);
                    Assert.IsTrue(element.GMM.ConvergedWithinTolerance, expected.Name + ": converged within tolerance");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 2: Example #1's regional skew penalty (mean 0.44, MSE 0.078), which its BCB copy shares; no other example
        /// enables a penalty. And "Find these results": Select Parameter choosing Mean (of log) (µ) in Kernel Density.
        /// </summary>
        [TestMethod]
        public void Example1_HasTheRegionalSkewPenalty()
        {
            Example.Run(() =>
            {
                foreach (GmmResult result in Results)
                {
                    var (view, properties) = Example.Open<B17CAnalysis, B17CAnalysisControl, B17CAnalysisPropertiesControl>(result.Name);
                    var parameters = EnabledPenalties(properties.ParametersTable);
                    if (result.Name.StartsWith("Example #1", StringComparison.Ordinal))
                    {
                        Assert.AreEqual(1, parameters.Count, result.Name + ": one parameter penalty");
                        StringAssert.EndsWith(parameters[0][1], "Skew (of log) (γ)", result.Name + ": the penalty's parameter");
                        Assert.AreEqual("0.44|0.078", parameters[0][2] + "|" + parameters[0][3], result.Name + ": the skew penalty's mean and MSE");
                    }
                    else
                        Assert.AreEqual(0, parameters.Count, result.Name + ": no parameter penalty");
                    Assert.AreEqual(0, EnabledPenalties(properties.QuantileTable).Count, result.Name + ": no quantile penalty");
                    if (result.Name == "Example #1")
                    {
                        SelectTab(view, "Kernel Density");
                        var combo = view.KernelDensityControl.ParameterComboBox;
                        combo.SelectedItem = combo.Items.Cast<object>().Single(i => i.ToString() == "Mean (of log) (µ)");
                        SharedControlsTesting.Settle();
                        Assert.AreEqual("Mean (of log) (µ)", combo.SelectedItem.ToString(), "Kernel Density's parameter");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 5 and the BCB table: each estimated BCB report's 1,000 of 1,000 replicates, no parent-fit substitutions or
        /// retries, and its optimizer fallbacks, candidate evaluation-limit statuses (the report's GMM status count Maximum
        /// Evaluations) and pivot z-limit clips (not reported for Examples 1 and 4).
        /// </summary>
        [TestMethod]
        public void BootstrapReports_ShowTheWalkthroughsDiagnostics()
        {
            Example.Run(() =>
            {
                foreach (var (number, fallbacks, limits, clips) in Bootstraps.Where(b => b.Number != 2))
                {
                    string name = "Example #" + number + " - BCB";
                    var (view, _) = Example.Open<B17CAnalysis, B17CAnalysisControl, B17CAnalysisPropertiesControl>(name);
                    SelectTab(view, "GMM Report");
                    AssertBootstrapReport(ReportText(view.GMMReportControl), fallbacks, limits, clips, name);
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Example #2 - BCB, which the walkthrough lists with a saved fit (1,000 draws, Success, 13,823.815 cfs and
        /// 8,735.409-17,720.516; 33 fallbacks, 3 evaluation-limit statuses and 5 clips), has none in the project: its row
        /// says IsEstimated False, with empty results and no GMM report, so RMC-BestFit 2.0.1 shows it unestimated, as
        /// XGoodFit does. Its Estimate button (the saved settings: seed 12345, 1,000 replicates) gives the parent's computed
        /// 13,823.815 cfs but limits of 8,582.358-17,706.822, with 39 fallbacks, 3 evaluation-limit statuses and 10 clips,
        /// while Example #1 - BCB's gives its saved fit exactly: the bootstrap reproduces a saved run, so the walkthrough's
        /// values come from a run the project doesn't hold. While the analysis runs its document stays shown, disabled, and
        /// it shows the new fit when the run ends (FrameworkUI's OpenWindows.DisableAllWindows keeps the selection, as WPF's
        /// AvalonDock does). About 15 seconds.
        /// </summary>
        [TestMethod]
        public void Example2Bcb_HasNoSavedResult_AndItsEstimateIsntTheWalkthroughs()
        {
            Example.Run(() =>
            {
                try
                {
                    Rerun(Results.Single(r => r.Name == "Example #1 - BCB"), Bootstraps.Single(b => b.Number == 1), saved: true);
                    // The walkthrough's row and counters (see above) aren't what the saved settings give.
                    Rerun(new GmmResult("Example #2 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "13,823.815", "8,582.358", "17,706.822"), (2, "39", "3", "10"), saved: false);
                }
                finally
                {
                    Example.Reopen();
                }
            });
        }

        // Runs a BCB analysis's Estimate and checks the fit and bootstrap diagnostics the document shows afterwards.
        private static void Rerun(GmmResult expected, (int Number, string Fallbacks, string Limits, string Clips) bootstrap, bool saved)
        {
            var element = Example.Element<B17CAnalysis>(expected.Name);
            var (view, properties) = Example.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(element);
            Assert.AreEqual(saved, element.IsEstimated, expected.Name + ": a saved result");
            // "All fourteen parent fits retain iterative GMM, BFGS, ...": settings, which an unestimated fit keeps too.
            AssertGmmSettings(element);
            SelectTab(view, "GMM Report");
            Assert.AreEqual(saved, ReportText(view.GMMReportControl).Length > 0, expected.Name + ": a saved GMM report");
            SelectTab(view, "Distribution Results");
            Example.Estimate(view, view.FrequencyCurveTable, properties.EstimateButton, TimeSpan.FromMinutes(3));
            Assert.IsTrue(element.IsEstimated, expected.Name + ": estimated");
            Assert.AreSame(view, Example.Shown<B17CAnalysisControl, B17CAnalysisPropertiesControl>(element).View, expected.Name + ": the document's view");
            AssertGmmResult(view, properties, expected);
            SelectTab(view, "GMM Report");
            AssertBootstrapReport(ReportText(view.GMMReportControl), bootstrap.Fallbacks, bootstrap.Limits, bootstrap.Clips, expected.Name);
            Example.CloseDocuments();
        }

        /// <summary>
        /// The figures no area test compares: American River's chronology, and Example #1's parameter-ensemble histogram,
        /// whose series the figure titles Uncertainty Histogram (without a display correction) where RMC-BestFit's view, as
        /// XGoodFit's, titles it Posterior Histogram (the "legacy plot selector" the caption mentions).
        /// </summary>
        [TestMethod]
        public void Figures_ShowTheChronologyAndTheEnsembleHistogram()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("Example #7 - Data");
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/bulletin-17c-examples-american-chronology.plotspec.json.gz"), "the chronology");
                Example.CloseDocuments();

                var (view, _) = Example.Open<B17CAnalysis, B17CAnalysisControl, B17CAnalysisPropertiesControl>("Example #1");
                SelectTab(view, "Histogram");
                AssertBars(view.HistogramControl.Plot, Figure(Folder + "images/bulletin-17c-examples-parameter-ensemble.plotspec.json.gz"), "the ensemble histogram", "Posterior Histogram");
            });
        }

        private static void AssertBootstrapReport(string report, string fallbacks, string limits, string clips, string label)
        {
            Assert.AreEqual("1,000", ReportWord(report, "Replicates Requested:"), label + ": replicates requested");
            Assert.AreEqual("1,000", ReportWord(report, "Replicates Used:"), label + ": replicates used");
            Assert.AreEqual("0", ReportWord(report, "Substituted (parent fit):"), label + ": parent-fit substitutions");
            Assert.AreEqual("0", ReportWord(report, "Total Retries:"), label + ": retries");
            Assert.AreEqual(fallbacks, ReportWord(report, "Optimizer Fallbacks:"), label + ": optimizer fallbacks");
            Assert.AreEqual(limits, ReportWord(report, "Maximum Evaluations:"), label + ": candidate evaluation-limit statuses");
            if (clips == null)
                Assert.IsFalse(report.Contains("Pivot z-limit Clips:", StringComparison.Ordinal), label + ": no pivot clips reported");
            else
                Assert.AreEqual(clips, ReportWord(report, "Pivot z-limit Clips:"), label + ": pivot z-limit clips");
        }

        // The walkthrough's BCB table: optimizer fallbacks, candidate evaluation-limit statuses and pivot z-limit clips. Example
        // #2's row isn't in the project (see Example2Bcb_HasNoSavedResult_AndItsEstimateIsntTheWalkthroughs).
        private static readonly (int Number, string Fallbacks, string Limits, string Clips)[] Bootstraps =
        {
            (1, "77", "0", null), (2, "33", "3", "5"), (3, "122", "17", "12"), (4, "1", "0", null), (5, "15", "0", "1"), (6, "86", "16", "19"), (7, "267", "42", "26"),
        };

        // The walkthrough's table of saved results (cfs).
        private static readonly GmmResult[] Results =
        {
            new("Example #1", "MultivariateNormal", "10000", "Success", "4,994.651", "4,162.258", "6,048.374"),
            new("Example #2", "MultivariateNormal", "10000", "Success", "13,823.815", "9,276.134", "17,912.057"),
            new("Example #3", "LinkedMultivariateNormal", "10000", "Success", "22,479.428", "17,626.626", "32,717.991"),
            new("Example #4", "LinkedMultivariateNormal", "10000", "Success", "39,780.299", "29,087.21", "68,407.592"),
            new("Example #5", "LinkedMultivariateNormal", "10000", "Success", "4,585.886", "3,819.165", "5,315.172"),
            new("Example #6", "LinkedMultivariateNormal", "10000", "Success", "10,973.628", "7,139.819", "17,193.733"),
            new("Example #7", "LinkedMultivariateNormal", "10000", "Success", "317,458.051", "272,749.028", "366,762.306"),
            new("Example #1 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "4,994.651", "4,189.959", "6,331.611"),
            // Not in the project (see Example2Bcb_HasNoSavedResult_AndItsEstimateIsntTheWalkthroughs).
            new("Example #2 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "13,823.815", "8,735.409", "17,720.516"),
            new("Example #3 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "22,479.428", "17,224.273", "35,416.426"),
            new("Example #4 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "39,780.299", "28,901.371", "68,146.434"),
            new("Example #5 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "4,585.886", "3,638.675", "5,328.071"),
            new("Example #6 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "10,973.628", "6,360.233", "18,929.878"),
            new("Example #7 - BCB", "BiasCorrectedBootstrap", "1000", "Success", "317,458.051", "265,210.604", "368,346.588"),
        };
    }
}
