using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Sinnemahoning Creek: GMM with an uncertain MOVE.3 extension"
    /// (examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/3-measurement-errors/sinnemahoning-move3-b17c.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures and the native exports are compared, point for point, by
    /// <see cref="B17CAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughSinnemahoningB17CTests
    {
        private const string Folder = "4-univariate-distribution-analysis/2-bulletin-17C-analysis/3-measurement-errors/";
        private static readonly WalkthroughExample Example = new WalkthroughExample("sinnemahoning-move3-b17c");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and steps 1-2: the records in cfs, the 25 reconstructed years 1914-1938 in the No Errors input's
        /// exact data and in the With Errors input's uncertain data, each a LogNormal distribution with log10 sigma 0.074.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() => WalkthroughSinnemahoningBayesianTests.AssertSinnemahomingInputs(Example));
        }

        /// <summary>
        /// "Saved results" and steps 3-4: the three fits' linked-MVN uncertainty, 10,000 draws, Success (converged within
        /// tolerance), 1% AEP and 90% limits, seed 12345; and Select Parameter choosing B17C Mean (of log) (µ) for No
        /// Extension. The With Errors fit, on No Extension's 79 exact peaks and the 25 uncertain years, isn't No Extension's:
        /// the GMM uses the uncertain series.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                CollectionAssert.AreEqual(Results.Select(r => r.Name).ToArray(), Example.Elements<B17CAnalysis>().Select(e => e.Name).ToArray(), "the fits");
                Assert.AreNotEqual(Results[0].Point, Results[2].Point, "the uncertain years change the fit");
                foreach (GmmResult expected in Results)
                {
                    var element = Example.Element<B17CAnalysis>(expected.Name);
                    var (view, properties) = Example.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(element);
                    AssertGmmResult(view, properties, expected);
                    Assert.IsTrue(element.GMM.ConvergedWithinTolerance, expected.Name + ": converged within tolerance");
                    if (expected.Name == "B17C - LPIII - No Extension")
                    {
                        SelectTab(view, "Kernel Density");
                        var combo = view.KernelDensityControl.ParameterComboBox;
                        combo.SelectedItem = combo.Items.Cast<object>().Single(i => i.ToString() == "B17C Mean (of log) (µ)");
                        SharedControlsTesting.Settle();
                        Assert.AreEqual("B17C Mean (of log) (µ)", combo.SelectedItem.ToString(), "Kernel Density's parameter");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>The With Errors input's chronology figure, which no area test compares.</summary>
        [TestMethod]
        public void Figures_ShowTheUncertainChronology()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("Sinnemahoning - MOVE.3 - With Errors");
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/sinnemahoning-move3-b17c-uncertain-chronology.plotspec.json.gz"), "the chronology");
            });
        }

        // The walkthrough's table of saved results (cfs).
        private static readonly GmmResult[] Results =
        {
            new("B17C - LPIII - No Extension", "LinkedMultivariateNormal", "10000", "Success", "41,820.971", "33,375.658", "60,873.539"),
            new("B17C - LPIII - No Errors", "LinkedMultivariateNormal", "10000", "Success", "40,221.255", "33,284.916", "54,271.765"),
            new("B17C - LPIII - With Errors", "LinkedMultivariateNormal", "10000", "Success", "40,862.386", "33,336.625", "56,521.083"),
        };
    }
}
