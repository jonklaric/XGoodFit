using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Blakely Mountain Dam: one-day inflows and information expansion"
    /// (examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis/2-information-expansion/
    /// blakely-mountain-dam-b17c.md), followed through the main window in both of its projects, the imperial one and the
    /// metric Case Study one.
    /// </summary>
    /// <remarks>
    /// The imperial project's frequency figures and native exports are compared, point for point, by
    /// <see cref="B17CAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughBlakelyB17CTests
    {
        private const string Folder = "4-univariate-distribution-analysis/2-bulletin-17C-analysis/2-information-expansion/";
        private const string Paleo = "Systematic + Historical + Paleo - 1Day";
        private static readonly WalkthroughExample Imperial = new WalkthroughExample("blakely-mountain-dam-b17c");
        private static readonly WalkthroughExample Metric = new WalkthroughExample("blakely-mountain-dam-b17c-metric");

        /// <summary>Closes the examples.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExamples()
        {
            Imperial.Close();
            Metric.Close();
        }

        /// <summary>
        /// "Source evidence" and steps 1-3: three inputs of 91 exact peaks during 1923-2018 in cfs, each with the 1931-1935
        /// gap below 110,000 cfs; the historical input's 1882 interval (115,000-150,000, shown at 132,500) and 1870-1922
        /// window; and the paleo input's 1020 event (181,000-194,000, shown at 187,000) and its four windows with the years
        /// below each threshold (3,999, 850, 52 and 5) and no unentered events above.
        /// </summary>
        [TestMethod]
        public void ImperialInputs_HoldTheWalkthroughsRecords()
        {
            Imperial.Run(() => AssertInputs(Imperial, "110,000", "Flow (cfs)", "115,000", "132,500", "150,000", "181,000", "187,000", "194,000", "376,500", "175,900"));
        }

        /// <summary>
        /// The metric project's inputs: the same records in m³/s, with the 110,000 cfs threshold stored as 3,114.87 m³/s and
        /// "the project's rounded metric inputs".
        /// </summary>
        [TestMethod]
        public void MetricInputs_HoldTheWalkthroughsRecords()
        {
            Metric.Run(() =>
            {
                var (view, properties) = Metric.Open<InputData, InputDataControl, InputDataPropertiesControl>(Paleo);
                AssertRecords(Records(view), 91, 1923, 2018, 0, 2, 4, 0, Paleo);
                Assert.AreEqual("Flow (m 3 /s)", Shown(properties.UnitLabel), "the metric unit label");
                SelectTab(view, "Threshold Data");
                CollectionAssert.AreEqual(new[] { "1870|1922|3,114.87", "1931|1935|3,114.87" },
                    Rows(view.ThresholdDataGrid).Where(r => r[0] is "1870" or "1931").Select(r => string.Join("|", r.Take(3))).ToArray(), "110,000 cfs as 3,114.87 m³/s");
                Metric.CloseDocuments();
                foreach (string name in new[] { "Systematic - 1Day", "Systematic + Historical - 1Day" })
                {
                    var (input, _) = Metric.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(input), 91, 1923, 2018, 0, name.Contains("Historical", StringComparison.Ordinal) ? 1 : 0,
                        name.Contains("Historical", StringComparison.Ordinal) ? 2 : 1, 0, name);
                    Metric.CloseDocuments();
                }
            });
        }

        // Checks the three inputs of a Blakely project, with its values as the grids show them.
        private static void AssertInputs(WalkthroughExample example, string gap, string unit, string historicalLower, string historicalValue, string historicalUpper,
            string paleoLower, string paleoValue, string paleoUpper, string oldest, string middle)
        {
            foreach (var (name, intervals, windows) in new[] { ("Systematic - 1Day", 0, 1), ("Systematic + Historical - 1Day", 1, 2), (Paleo, 2, 4) })
            {
                var (view, properties) = example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                AssertRecords(Records(view), 91, 1923, 2018, 0, intervals, windows, 0, name);
                Assert.AreEqual(unit, Shown(properties.UnitLabel), name + ": the unit label");
                SelectTab(view, "Interval Data");
                var intervalRows = Rows(view.IntervalDataGrid);
                if (intervals > 0)
                    Assert.AreEqual($"1882|{historicalLower}|{historicalValue}|{historicalUpper}", string.Join("|", intervalRows.Single(r => r[0] == "1882").Take(4)), name + ": the 1882 interval");
                if (intervals > 1)
                    Assert.AreEqual($"1020|{paleoLower}|{paleoValue}|{paleoUpper}", string.Join("|", intervalRows.Single(r => r[0] == "1020").Take(4)), name + ": the 1020 event");
                SelectTab(view, "Threshold Data");
                var windowRows = Rows(view.ThresholdDataGrid);
                var expected = new[] { ("-2980", "1018", oldest, 3999), ("1019", "1869", middle, 850), ("1870", "1922", gap, 52), ("1931", "1935", gap, 5) }
                    .Skip(4 - windows).ToArray();
                Assert.AreEqual(windows, windowRows.Count, name + ": the windows");
                int[] entered = intervalRows.Select(r => int.Parse(r[0], System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                for (int i = 0; i < windows; i++)
                {
                    var (start, end, threshold, below) = expected[i];
                    string[] row = windowRows[i];
                    Assert.AreEqual($"{start}|{end}|{threshold}|0", string.Join("|", row), name + ": a window");
                    // The years below a threshold: the window's years less the events entered in it and those above.
                    int first = int.Parse(start, System.Globalization.CultureInfo.InvariantCulture), last = int.Parse(end, System.Globalization.CultureInfo.InvariantCulture);
                    Assert.AreEqual(below, last - first + 1 - entered.Count(year => year >= first && year <= last) - int.Parse(row[3], System.Globalization.CultureInfo.InvariantCulture),
                        $"{name}: the years below the {start}-{end} window's threshold");
                }
                example.CloseDocuments();
            }
        }

        /// <summary>
        /// "Saved imperial results" and steps 4-6: the seven alternatives' linked-MVN uncertainty, draws, optimizer status (one
        /// MaximumFunctionEvaluationsReached), 1% AEP and limits, seed 12345 and 90% limits; the Reg Skew alternatives' skew
        /// penalty (mean -0.17, MSE 0.12) and the QPen alternative's quantile penalty (AEP 0.0001, log10 mean 5.4689, MSE
        /// 0.016); and Select Parameter choosing B17C Mean (of log) (µ) for Systematic.
        /// </summary>
        [TestMethod]
        public void ImperialAnalyses_ShowTheSavedResultsAndPenalties()
        {
            Imperial.Run(() => AssertAnalyses(Imperial, ImperialResults, "5.4689"));
        }

        /// <summary>
        /// "Saved metric results": the twelve alternatives' linked-MVN uncertainty with 10,000 draws, Success, 1% AEP and
        /// limits; the same skew penalty, and the quantile penalty's log10 mean 3.920947241 (MSE 0.016 at AEP 0.0001).
        /// </summary>
        [TestMethod]
        public void MetricAnalyses_ShowTheSavedResultsAndPenalties()
        {
            Metric.Run(() => AssertAnalyses(Metric, MetricResults, "3.920947241"));
        }

        private static void AssertAnalyses(WalkthroughExample example, GmmResult[] results, string quantileMean)
        {
            CollectionAssert.AreEqual(results.Select(r => r.Name).ToArray(), example.Elements<B17CAnalysis>().Select(e => e.Name).ToArray(), "the alternatives");
            foreach (GmmResult expected in results)
            {
                var element = example.Element<B17CAnalysis>(expected.Name);
                var (view, properties) = example.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(element);
                AssertGmmResult(view, properties, expected);
                var skew = EnabledPenalties(properties.ParametersTable);
                var quantile = EnabledPenalties(properties.QuantileTable);
                if (expected.Name.Contains("Reg Skew", StringComparison.Ordinal))
                {
                    Assert.AreEqual(1, skew.Count, expected.Name + ": the skew penalty");
                    StringAssert.EndsWith(skew[0][1], "Skew (of log) (γ)", expected.Name + ": the penalty's parameter");
                    AssertAgrees("-0.17", skew[0][2], expected.Name + ": the skew penalty's mean");
                    AssertAgrees("0.12", skew[0][3], expected.Name + ": the skew penalty's MSE");
                }
                else
                    Assert.AreEqual(0, skew.Count, expected.Name + ": no parameter penalty");
                if (expected.Name.Contains("QPen", StringComparison.Ordinal))
                {
                    // Enabled, Probability, Log10, Mean, MSE.
                    Assert.AreEqual(1, quantile.Count, expected.Name + ": the quantile penalty");
                    Assert.AreEqual("0.0001|True", quantile[0][1] + "|" + quantile[0][2], expected.Name + ": the quantile penalty's AEP, in log10 space");
                    AssertAgrees(quantileMean, quantile[0][3], expected.Name + ": the quantile penalty's log10 mean");
                    AssertAgrees("0.016", quantile[0][4], expected.Name + ": the quantile penalty's MSE");
                }
                else
                    Assert.AreEqual(0, quantile.Count, expected.Name + ": no quantile penalty");
                if (expected.Name == "Systematic" && example == Imperial)
                {
                    SelectTab(view, "Kernel Density");
                    var combo = view.KernelDensityControl.ParameterComboBox;
                    combo.SelectedItem = combo.Items.Cast<object>().Single(i => i.ToString() == "B17C Mean (of log) (µ)");
                    SharedControlsTesting.Settle();
                    Assert.AreEqual("B17C Mean (of log) (µ)", combo.SelectedItem.ToString(), "Kernel Density's parameter");
                }
                example.CloseDocuments();
            }
        }

        /// <summary>
        /// The figures no area test compares: the imperial paleo input's chronology, and the metric project's final
        /// alternative (its quantile information labelled Quantile Penalty in the figure, Quantile Prior in the app).
        /// </summary>
        [TestMethod]
        public void Figures_ShowThePaleoChronologyAndTheMetricFit()
        {
            Imperial.Run(() =>
            {
                var input = Imperial.Element<InputData>(Paleo);
                Imperial.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/blakely-mountain-dam-b17c-paleo-chronology.plotspec.json.gz"), "the chronology");
            });
            Metric.Run(() =>
            {
                var final = Metric.Element<B17CAnalysis>("Systematic + Historical + Paleo + Reg Skew + QPen");
                Metric.Open<B17CAnalysisControl, B17CAnalysisPropertiesControl>(final);
                UnivariateAnalysisTesting.AssertFigure(final.FrequencyPlot, Figure(Folder + "Case Study/images/blakely-mountain-dam-b17c-metric-penalty-frequency.plotspec.json.gz"), "the metric fit");
            });
        }

        // The walkthrough's table of saved imperial results (cfs).
        private static readonly GmmResult[] ImperialResults =
        {
            new("Systematic", "LinkedMultivariateNormal", "10000", "Success", "135,318.409", "110,938.936", "167,810.735"),
            new("Systematic + Historical", "LinkedMultivariateNormal", "1000", "Success", "131,339.869", "113,768.505", "153,199.084"),
            new("Systematic + Historical + Paleo", "LinkedMultivariateNormal", "1000", "Success", "131,472.797", "117,589.716", "143,797.842"),
            new("Systematic + Reg Skew", "LinkedMultivariateNormal", "1000", "Success", "136,488.768", "115,687.472", "165,044.246"),
            new("Systematic + Historical + Reg Skew", "LinkedMultivariateNormal", "1000", "Success", "132,397.222", "116,086.68", "154,175.891"),
            new("Systematic + Historical + Paleo + Reg Skew", "LinkedMultivariateNormal", "1000", "MaximumFunctionEvaluationsReached", "131,630.822", "118,816.452", "144,338.939"),
            new("Systematic + Historical + Paleo + Reg Skew + QPen", "LinkedMultivariateNormal", "2000", "Success", "133,223.803", "120,534.936", "145,010.734"),
        };

        // The walkthrough's table of saved metric results (m³/s).
        private static readonly GmmResult[] MetricResults =
        {
            new("Systematic", "LinkedMultivariateNormal", "10000", "Success", "3,831.811", "3,141.458", "4,751.897"),
            new("Systematic + Historical", "LinkedMultivariateNormal", "10000", "Success", "3,719.151", "3,198.396", "4,365.361"),
            new("Systematic + Historical + Paleo", "LinkedMultivariateNormal", "10000", "Success", "3,722.915", "3,329.594", "4,088.78"),
            new("Systematic + Reg Skew", "LinkedMultivariateNormal", "10000", "Success", "3,864.96", "3,235.376", "4,734.961"),
            new("Systematic + Historical + Reg Skew", "LinkedMultivariateNormal", "10000", "Success", "3,749.09", "3,262.677", "4,367.598"),
            new("Systematic + Historical + Paleo + Reg Skew", "LinkedMultivariateNormal", "10000", "Success", "3,727.391", "3,349.138", "4,092.369"),
            new("Systematic + Historical + Paleo + Reg Skew + QPen", "LinkedMultivariateNormal", "10000", "Success", "3,772.535", "3,421.37", "4,114.96"),
            new("Systematic + QPen", "LinkedMultivariateNormal", "10000", "Success", "4,012.95", "3,444.511", "4,789.651"),
            new("Systematic + Historical + QPen", "LinkedMultivariateNormal", "10000", "Success", "3,868.952", "3,402.122", "4,449.323"),
            new("Systematic + Historical + Paleo + QPen", "LinkedMultivariateNormal", "10000", "Success", "3,772.47", "3,415.448", "4,111.961"),
            new("Systematic + Reg Skew + QPen", "LinkedMultivariateNormal", "10000", "Success", "4,009.638", "3,461.112", "4,763.693"),
            new("Systematic + Historical + Reg Skew + QPen", "LinkedMultivariateNormal", "10000", "Success", "3,870.582", "3,415.333", "4,442.277"),
        };
    }
}
