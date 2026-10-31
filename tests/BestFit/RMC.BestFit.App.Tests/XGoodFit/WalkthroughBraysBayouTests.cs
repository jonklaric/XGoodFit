using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Brays Bayou: conditional flood frequency with changing parameters"
    /// (examples/4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/nsffa-brays-bayou-texas.md),
    /// followed through the main window.
    /// </summary>
    /// <remarks>
    /// The analyses' frequency and chronology figures and the native exports are compared, point for point, by
    /// <see cref="UnivariateAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBraysBayouTests
    {
        private const string Folder = "4-univariate-distribution-analysis/1-univariate-analysis/5-nonstationary-ffa/";
        private const string Metric = "USGS 08075000 Brays Bayou - Metric";
        private static readonly WalkthroughExample Example = new WalkthroughExample("nsffa-brays-bayou-texas");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The input table and step 1: four inputs of 90 peaks during 1929-2024 (three in cfs, the metric one in m³/s), each
        /// with the 1930-1935 gap as six years below a threshold: 35,100 cfs (994 m³/s), but 200,000 cfs in the second copy,
        /// which the walkthrough's table also gives as 35,100.
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                foreach (var (name, threshold, unit) in new[]
                {
                    ("USGS 08075000 Brays Bayou", "35,100", "Peak Discharge (cfs)"), (Metric, "994", "Peak Discharge (cms)"),
                    ("USGS 08075000 Brays Bayou_copy", "35,100", "Peak Discharge (cfs)"),
                    // The walkthrough says 35,100 cfs; the project saves 200,000 (DataFrame's ThresholdData), as RMC-BestFit shows.
                    ("USGS 08075000 Brays Bayou_copy_copy", "200,000", "Peak Discharge (cfs)"),
                })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), 90, 1929, 2024, 0, 0, 1, 0, name);
                    Assert.AreEqual(unit, Shown(properties.UnitLabel), name + ": the unit label");
                    SelectTab(view, "Threshold Data");
                    string[] window = Row(view.ThresholdDataGrid, 0);
                    Assert.AreEqual("1930|1935|" + threshold + "|0", string.Join("|", window), name + ": the gap's window");
                    Assert.AreEqual(6, 1935 - 1930 + 1 - int.Parse(window[3]), name + ": the years below the threshold");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Steps 2-4 and the trend table: every fit uses the metric input; the mean, standard deviation and skew of log flow
        /// in that order, with each alternative's trend models; the trend start index 1929; the evaluation index 2024; alpha
        /// 0.5; no quantile priors, default flat priors and Jeffreys' rule.
        /// </summary>
        [TestMethod]
        public void Analyses_HaveTheWalkthroughsTrendModels()
        {
            Example.Run(() =>
            {
                string[] parameters = { "Mean (of log) (µ)", "Std Dev (of log) (σ)", "Skew (of log) (γ)" };
                foreach (var (name, types) in Trends)
                {
                    var element = Example.Element<UnivariateAnalysis>(name);
                    var (_, properties) = Example.Open<UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(element);
                    Assert.AreEqual(Metric, Shown(properties.InputDataComboBox), name + ": its input");
                    Assert.AreEqual("Log-Pearson Type III", Shown(properties.Distribution), name + ": the model");
                    Assert.IsFalse(QuantilePriorsEnabled(properties.QuantilePriorsControl), name + ": no quantile priors");
                    Assert.IsTrue(Ticked(properties.ParameterPriorsControl.UsePriorDistributionDefaults), name + ": default flat priors");
                    Assert.IsTrue(Ticked(properties.UseJeffreysRule), name + ": Jeffreys' rule");
                    AssertTrends(element, properties, true, types, 1929, "2024", parameters);
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Read the saved results": each fit's chains, thinning and draws, DIC, 1% AEP at 2024 and limits, R-hat and ESS,
        /// with DE-MCzs, seed 12345, posterior mean parameters and 90% intervals; and Select Parameter choosing
        /// Mean (of log) (µ) (α) for NSFFA - Constant.
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
                    AssertSampler(Parts(view), Parts(properties), expected.Name);
                    if (expected.Name == "NSFFA - Constant")
                        SelectParameter(Parts(view), "Mean (of log) (µ) (α)");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 5: the Bayesian Model Average's three components and their DIC weights, and its frequency figure, which no
        /// area test compares.
        /// </summary>
        [TestMethod]
        public void ModelAverage_HasTheThreeComponentsAndTheirWeights()
        {
            Example.Run(() =>
            {
                var average = Example.Element<CompositeAnalysis>("Bayesian Model Average");
                var (_, properties) = Example.Open<CompositeAnalysisControl, CompositeAnalysisPropertiesControl>(average);
                var rows = Rows(properties.CompositeFunctionDataGrid);
                CollectionAssert.AreEqual(new[] { "NSFFA - Linear - Logistic", "NSFFA - Logistic - Logistic", "NSFFA - Step - Logistic" }, rows.Select(r => r[0]).ToArray(), "the components");
                foreach (var (row, weight) in rows.Zip(new[] { "0.000156752", "0.029356226", "0.970487022" }))
                    AssertAgrees(weight, row[1], row[0] + "'s DIC weight");
                UnivariateAnalysisTesting.AssertFigure(average.FrequencyPlot, Figure(Folder + "images/nsffa-brays-bayou-texas-model-average.plotspec.json.gz"), "the model average");
            });
        }

        /// <summary>The metric input's chronology figure, which no area test compares.</summary>
        [TestMethod]
        public void Figures_ShowTheInputChronology()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>(Metric);
                Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/nsffa-brays-bayou-texas-input-chronology.plotspec.json.gz"), "the chronology");
            });
        }

        // The walkthrough's trend table: each alternative's trend models in the model's order (its StepFunction, the saved
        // name, is the app's Step Function).
        private static readonly (string Name, string[] Types)[] Trends =
        {
            ("NSFFA - Constant", new[] { "Constant", "Constant", "Constant" }),
            ("NSFFA - Linear", new[] { "Linear", "Constant", "Constant" }),
            ("NSFFA - Logistic", new[] { "Logistic", "Constant", "Constant" }),
            ("NSFFA - Step", new[] { "Step Function", "Constant", "Constant" }),
            ("NSFFA - Linear - Logistic", new[] { "Linear", "Logistic", "Constant" }),
            ("NSFFA - Logistic - Logistic", new[] { "Logistic", "Logistic", "Constant" }),
            ("NSFFA - Step - Logistic", new[] { "Step Function", "Logistic", "Constant" }),
        };

        // The walkthrough's table of saved results (m³/s at 2024).
        private static readonly BayesianResult[] Results =
        {
            new("NSFFA - Constant", "Posterior Mean", "992.122", "912.23", "1,122.722", "90%", "1.00006", "8,762", "1,228.347", "6", "30", "10000"),
            new("NSFFA - Linear", "Posterior Mean", "2,290.874", "1,705.76", "3,236.131", "90%", "1.00050", "9,367", "1,180.365", "8", "40", "10000"),
            new("NSFFA - Logistic", "Posterior Mean", "2,565.012", "1,924.624", "3,433.781", "90%", "1.00075", "7,392", "1,178.943", "8", "40", "10000"),
            new("NSFFA - Step", "Posterior Mean", "1,111.165", "960.12", "1,348.675", "90%", "1.00025", "8,497", "1,158.12", "10", "50", "10000"),
            new("NSFFA - Linear - Logistic", "Posterior Mean", "1,445.906", "1,141.636", "1,934.591", "90%", "1.00009", "9,309", "1,172.296", "10", "50", "10000"),
            new("NSFFA - Logistic - Logistic", "Posterior Mean", "1,175.09", "888.553", "1,406.932", "90%", "1.00482", "918", "1,161.831", "10", "50", "20000"),
            new("NSFFA - Step - Logistic", "Posterior Mean", "972.381", "843.204", "1,141.713", "90%", "1.00042", "8,619", "1,154.834", "12", "60", "10000"),
        };
    }
}
