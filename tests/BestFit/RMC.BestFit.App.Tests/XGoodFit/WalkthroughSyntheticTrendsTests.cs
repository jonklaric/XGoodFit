using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Synthetic data: recognizing nonstationary trend shapes"
    /// (examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-synthetic-data.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The nine chronology figures, the index-51 frequency figure and the native exports are compared, point for point,
    /// by <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughSyntheticTrendsTests
    {
        private static readonly WalkthroughExample Example = new WalkthroughExample("nsffa-synthetic-data");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// "Data and model scope": nine inputs, each of 100 exact values at indexes 1-100 in generic Value units, with no
        /// uncertain, interval or threshold data and no low outlier flags.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                var inputs = Example.Elements<InputData>();
                CollectionAssert.AreEqual(Trends.Select(t => t.Input).ToArray(), inputs.Select(i => i.Name).ToArray(), "the inputs");
                foreach (InputData input in inputs)
                {
                    var (view, properties) = Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                    AssertRecords(Records(view), 100, 1, 100, 0, 0, 0, 0, input.Name);
                    Assert.AreEqual("Value", Shown(properties.UnitLabel), input.Name + ": the unit label");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Steps 1-2 and 5 and the trend table: each Normal fit uses its own dataset, varies the mean with its named trend and
        /// keeps the standard deviation constant, from start index 1, evaluated at index 51 with alpha 0.5; no quantile
        /// priors; and no model-average element.
        /// </summary>
        [TestMethod]
        public void Analyses_HaveTheWalkthroughsTrendModels()
        {
            Example.Run(() =>
            {
                Assert.AreEqual(0, Example.Elements<CompositeAnalysis>().Count, "no model average");
                foreach (var (name, input, trend) in Trends)
                {
                    var element = Example.Element<UnivariateAnalysis>(name);
                    var (_, properties) = Example.Open<UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(element);
                    Assert.AreEqual(input, Shown(properties.InputDataComboBox), name + ": its input");
                    Assert.AreEqual("Normal", Shown(properties.Distribution), name + ": the model");
                    Assert.IsFalse(QuantilePriorsEnabled(properties.QuantilePriorsControl), name + ": no quantile priors");
                    AssertTrends(element, properties, true, new[] { trend, "Constant" }, 1, "51", new[] { "Mean (µ)", "Std Dev (σ)" });
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved conditional results" and step 3: each fit's chains, thinning and draws, DIC, 1% AEP at index 51 and limits,
        /// R-hat and ESS, with DE-MCzs, seed 12345, posterior mean parameters and 90% intervals; the sinusoidal fit's
        /// frequency of about 0.0140238 cycles per index; and Select Parameter choosing Mean (µ) (α) for the constant trend.
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
                    if (expected.Name == "NSFFA - Constant Trend")
                        SelectParameter(Parts(view), "Mean (µ) (α)");
                    if (expected.Name == "NSFFA - Sinusoidal Trend")
                    {
                        SelectTab(view, "MCMC Report");
                        AssertAgrees("0.0140238", ReportRow(ReportText(view.MCMCReportControl), "PARAMETER SUMMARY STATISTICS", "Mean (µ) (γ)")[0], "the sinusoid's frequency");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        // The walkthrough's trend table, with each fit's input (its StepFunction, the saved name, is the app's Step Function).
        private static readonly (string Name, string Input, string Trend)[] Trends =
        {
            ("NSFFA - Constant Trend", "Constant Trend Data", "Constant"), ("NSFFA - Cubic Trend", "Cubic Trend Data", "Cubic"),
            ("NSFFA - Exponential Trend", "Exponential Trend Data", "Exponential"), ("NSFFA - Linear Trend", "Linear Trend Data", "Linear"),
            ("NSFFA - Logistic Trend", "Logistic Trend Data", "Logistic"), ("NSFFA - Power Trend", "Power Trend Data", "Power"),
            ("NSFFA - Quadratic Trend", "Quadratic Trend Data", "Quadratic"), ("NSFFA - Sinusoidal Trend", "Sinusoidal Trend Data", "Sinusoidal"),
            ("NSFFA - Step Function", "Step Function Data", "Step Function"),
        };

        // The walkthrough's table of saved conditional results (generic units at index 51).
        private static readonly BayesianResult[] Results =
        {
            new("NSFFA - Constant Trend", "Posterior Mean", "136.38", "131.593", "141.608", "90%", "0.99979", "9,148", "837.739", "4", "20", "10000"),
            // The walkthrough gives the cubic fit a largest R-hat of 1.00004 and a smallest ESS of 9,028, which no parameter of
            // the saved run has: its stored diagnostics (MCMCResults, written at sampling time and loaded as saved), which
            // RMC-BestFit's MCMC Report shows, are 1.0005 (the standard deviation's) and 9,523. Its quantiles and DIC are the
            // walkthrough's. The element was saved again on 30 September 2026, after the others (16 July).
            new("NSFFA - Cubic Trend", "Posterior Mean", "147.297", "142.032", "153.179", "90%", "1.0005", "9,523", "831.867", "10", "50", "10000"),
            new("NSFFA - Exponential Trend", "Posterior Mean", "176.446", "171.427", "181.93", "90%", "1.00019", "9,296", "836.514", "6", "30", "10000"),
            new("NSFFA - Linear Trend", "Posterior Mean", "141.964", "137.165", "147.315", "90%", "1.00040", "9,625", "839.377", "6", "30", "10000"),
            new("NSFFA - Logistic Trend", "Posterior Mean", "129.566", "124.427", "134.66", "90%", "1.00001", "9,167", "838.575", "6", "30", "10000"),
            new("NSFFA - Power Trend", "Posterior Mean", "265.622", "259.279", "272.123", "90%", "1.00029", "9,629", "883.869", "6", "30", "10000"),
            new("NSFFA - Quadratic Trend", "Posterior Mean", "150.104", "144.569", "156.122", "90%", "1.00004", "8,762", "836.951", "8", "40", "10000"),
            new("NSFFA - Sinusoidal Trend", "Posterior Mean", "162.661", "156.151", "169.633", "90%", "1.00020", "9,230", "835.068", "10", "50", "10000"),
            new("NSFFA - Step Function", "Posterior Mean", "136.883", "131.574", "142.89", "90%", "1.00008", "8,721", "839.985", "8", "40", "10000"),
        };
    }
}
