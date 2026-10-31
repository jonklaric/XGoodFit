using System;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateWalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: RMC-BestFit's walkthrough "Precipitation frequency: annual maxima and point-process models"
    /// (examples/4-univariate-distribution-analysis/3-point-process-analysis/point-process-examples.md), followed through
    /// the main window.
    /// </summary>
    /// <remarks>
    /// The frequency figures and the native exports are compared, point for point, by
    /// <see cref="PointProcessAnalysisViewTests.Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures"/>.
    /// docs/walkthrough-coverage/univariate.md maps each claim to its test.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class WalkthroughPointProcessTests
    {
        private const string Folder = "4-univariate-distribution-analysis/3-point-process-analysis/";
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");
        private static readonly WalkthroughExample Example = new WalkthroughExample("point-process-examples");

        /// <summary>Closes the example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Example.Close();

        /// <summary>
        /// The daily records' table and step 1: Big Bear Lake's 24,106 positions from 1960-07-01 to 2026-06-30 with 482
        /// missing, and De Sabla's 43,496 from 1906-03-01 to 2025-03-31 with 1,684 missing, in inches.
        /// </summary>
        [TestMethod]
        public void DailyRecords_HaveTheWalkthroughsPositions()
        {
            Example.Run(() =>
            {
                foreach (var (name, first, last, positions, missing) in new[]
                {
                    ("USC00040741", new DateTime(1960, 7, 1), new DateTime(2026, 6, 30), "24,106", "482"),
                    ("USC00042402", new DateTime(1906, 3, 1), new DateTime(2025, 3, 31), "43,496", "1,684"),
                })
                {
                    var (view, properties) = Example.Open<TimeSeriesElement, TimeSeriesControl, TimeSeriesPropertiesControl>(name);
                    var grid = view.TimeSeriesTable.GetVisualDescendants().OfType<DataGrid>().Single(g => g.Name == "TimeSeriesDataGrid");
                    // Date Time is the grid's second column; the dates are shown in en-US (on Linux and macOS with a
                    // narrow no-break space before AM and PM).
                    Assert.AreEqual(first, Date(Text(grid, 0, 1)), name + ": the first position");
                    Assert.AreEqual(last, Date(Text(grid, Count(grid) - 1, 1)), name + ": the last position");
                    Assert.AreEqual(positions, Count(grid).ToString("N0", Us), name + ": the positions");
                    SelectTab(view, "Summary Statistics");
                    Assert.AreEqual(positions, Statistic(view.SummaryStatisticsDataGrid, "Record Length"), name + ": the record length");
                    Assert.AreEqual(missing, Statistic(view.SummaryStatisticsDataGrid, "Missing Values"), name + ": the missing positions");
                    Assert.AreEqual("Inches", Shown(properties.DepthUnitSelector), name + ": the depth unit");
                    Example.CloseDocuments();
                }
            });
        }

        // A date and time as the grids show them.
        private static DateTime Date(string shown) => DateTime.Parse(shown.Replace((char)0x202F, ' '), Us);

        /// <summary>
        /// The input table and steps 2 and the block-year notes: the POT samples' 253 and 439 events (above 1 and 2.5 in,
        /// one step apart, no smoothing; 3.776 and 3.658 events a year), and the annual maxima's 67 and 120 values (water
        /// years starting in October and September).
        /// </summary>
        [TestMethod]
        public void Inputs_HoldTheWalkthroughsRecords()
        {
            Example.Run(() =>
            {
                foreach (var (name, exact, first, last, threshold, rate, month) in new (string, int, int, int, string, string, string)[]
                {
                    ("USC00040741 - POT", 253, 1960, 2026, "1", "3.776", null), ("USC00040741 - AMS", 67, 1960, 2026, null, null, "October"),
                    ("USC00042402 - POT", 439, 1906, 2025, "2.5", "3.658", null), ("USC00042402 - AMS", 120, 1906, 2025, null, null, "September"),
                })
                {
                    var (view, properties) = Example.Open<InputData, InputDataControl, InputDataPropertiesControl>(name);
                    AssertRecords(Records(view), exact, first, last, 0, 0, 0, 0, name);
                    Assert.AreEqual("Rainfall (in)", Shown(properties.UnitLabel), name + ": the unit label");
                    Assert.AreEqual(name.Substring(0, 11), Shown(properties.TimeSeriesElement), name + ": its daily record");
                    Assert.AreEqual("None", Shown(properties.SmoothingFunction), name + ": no smoothing");
                    if (threshold != null)
                    {
                        Assert.AreEqual("Peaks-Over-Threshold Series", Shown(properties.ExactDateMethod), name + ": the extraction");
                        Assert.AreEqual(threshold, Shown(properties.POTThresholdValue), name + ": the threshold");
                        Assert.AreEqual("1", Shown(properties.MinStepsBetweenPeaks), name + ": the separation");
                        SelectTab(view, "Summary Statistics");
                        AssertAgrees(rate, Statistic(view.SummaryStatisticsDataGrid, "Events Per Index (λ)"), name + ": the events a year");
                    }
                    else
                    {
                        Assert.AreEqual("Block Series", Shown(properties.ExactDateMethod), name + ": the extraction");
                        Assert.AreEqual("Maximum", Shown(properties.BlockFunction), name + ": the block function");
                        Assert.AreEqual("Water Year", Shown(properties.TimeBlock), name + ": the block");
                        Assert.AreEqual(month, Shown(properties.StartMonth), name + ": the water year's start");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// Step 3 and the block-year notes: each point-process analysis's input, threshold and inferred exposure (67 and 120
        /// years); the stationary models' October start (a setting the panel shows only for seasonal models) and the seasonal
        /// model's August start; and the seasonal point estimate's change points 99.53338 and 246.45066 followed by two GEV
        /// parameter triples (eight parameters).
        /// </summary>
        [TestMethod]
        public void PointProcesses_HaveTheWalkthroughsSettings()
        {
            Example.Run(() =>
            {
                foreach (var (name, input, threshold, years, seasonal, month) in new[]
                {
                    ("USC00040741 - Point Process", "USC00040741 - POT", "1", "67", false, "October"),
                    ("USC00042402 - Point Process", "USC00042402 - POT", "2.5", "120", false, "October"),
                    ("USC00040741 - Seasonal Point Process", "USC00040741 - POT", "1", "67", true, "August"),
                })
                {
                    var (view, properties) = Example.Open<PointProcessAnalysis, PointProcessAnalysisControl, PointProcessAnalysisPropertiesControl>(name);
                    Assert.AreEqual(input, Shown(properties.InputDataComboBox), name + ": its input");
                    Assert.AreEqual(threshold, Shown(properties.Threshold), name + ": the threshold");
                    Assert.AreEqual(years, Shown(properties.TotalYears), name + ": the exposure");
                    Assert.AreEqual(seasonal, Ticked(properties.IsSeasonal), name + ": seasonal");
                    Assert.AreEqual(seasonal, properties.StartMonth.IsEffectivelyVisible, name + ": the start month shows");
                    Assert.AreEqual(month, SelectedName((ComboBox)properties.StartMonth.InnerContent), name + ": the block year's start");
                    if (seasonal)
                    {
                        Assert.AreEqual("Water Year", Shown(properties.TimeBlock), name + ": the block");
                        SelectTab(view, "Tabular Results");
                        var statistics = Rows(view.SummaryStatisticsTable);
                        int parameters = statistics.FindIndex(r => r[0] == "Minimum");
                        Assert.AreEqual(8, parameters, name + ": eight parameters, " + string.Join(", ", statistics.Take(parameters).Select(r => r[0])));
                        AssertAgrees("99.53338", statistics[0][1], name + ": the first change point (" + statistics[0][0] + ")");
                        AssertAgrees("246.45066", statistics[1][1], name + ": the second change point (" + statistics[1][0] + ")");
                    }
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// "Saved frequency results": each fit's chains, thinning and draws, DIC, 1% AEP and limits, R-hat and ESS, with
        /// DE-MCzs, seed 12345, posterior mean parameters and 90% intervals; and Select Parameter choosing Location (ξ) for
        /// USC00040741 - Point Process.
        /// </summary>
        [TestMethod]
        public void Analyses_ShowTheSavedResults()
        {
            Example.Run(() =>
            {
                foreach (BayesianResult expected in Results)
                {
                    BayesianView view;
                    BayesianPanel panel;
                    if (expected.Name.EndsWith("GEV", StringComparison.Ordinal))
                    {
                        var (univariate, properties) = Example.Open<UnivariateAnalysis, UnivariateAnalysisControl, UnivariateAnalysisPropertiesControl>(expected.Name);
                        Assert.AreEqual("Generalized Extreme Value", Shown(properties.Distribution), expected.Name + ": the model");
                        Assert.AreEqual(expected.Name.Substring(0, 11) + " - AMS", Shown(properties.InputDataComboBox), expected.Name + ": its input");
                        (view, panel) = (Parts(univariate), Parts(properties));
                    }
                    else
                    {
                        var (pointProcess, properties) = Example.Open<PointProcessAnalysis, PointProcessAnalysisControl, PointProcessAnalysisPropertiesControl>(expected.Name);
                        (view, panel) = (Parts(pointProcess), Parts(properties));
                    }
                    AssertResult(view, panel, expected);
                    AssertSampler(view, panel, expected.Name, jeffreys: null);
                    if (expected.Name == "USC00040741 - Point Process")
                        SelectParameter(view, "Location (ξ)");
                    Example.CloseDocuments();
                }
            });
        }

        /// <summary>
        /// The figures no area test compares: Big Bear Lake's POT events in chronology, and their seasonality (the share of
        /// events in each month).
        /// </summary>
        [TestMethod]
        public void Figures_ShowThePotChronologyAndSeasonality()
        {
            Example.Run(() =>
            {
                var input = Example.Element<InputData>("USC00040741 - POT");
                var (view, _) = Example.Open<InputDataControl, InputDataPropertiesControl>(input);
                UnivariateAnalysisTesting.AssertFigure(input.ChronologyPlot, Figure(Folder + "images/point-process-examples-big-bear-pot-chronology.plotspec.json.gz"), "the chronology");
                SelectTab(view, "Seasonality Plot");
                AssertSeasonality(input.SeasonalityPlot, Figure(Folder + "images/point-process-examples-big-bear-seasonality.plotspec.json.gz"));
            });
        }

        // The seasonality figure's bars: each month's share of the events, centred mid-month in a leap year (2020).
        private static void AssertSeasonality(OxyPlot.Wpf.Plot plot, System.Text.Json.JsonElement figure)
        {
            ((OxyPlot.IPlotModel)plot.ActualModel).Update(true);
            var expected = figure.GetProperty("series")[0];
            var series = plot.Series.OfType<OxyPlot.Wpf.HistogramSeries>().Single(s => s.Title == expected.GetProperty("name").GetString());
            var bars = ((System.Collections.IEnumerable)series.ItemsSource).Cast<OxyPlot.Series.HistogramItem>().ToList();
            Assert.AreEqual(expected.GetProperty("x").GetArrayLength(), bars.Count, "the months");
            for (int i = 0; i < bars.Count; i++)
            {
                DateTime centre = DateTime.Parse(expected.GetProperty("x")[i].GetString(), CultureInfo.InvariantCulture);
                UnivariateAnalysisTesting.AssertClose(OxyPlot.Axes.DateTimeAxis.ToDouble(centre), (bars[i].RangeStart + bars[i].RangeEnd) / 2, $"month {i + 1}'s centre");
                UnivariateAnalysisTesting.AssertClose(expected.GetProperty("width")[i].GetDouble(), bars[i].RangeEnd - bars[i].RangeStart, $"month {i + 1}'s days");
                UnivariateAnalysisTesting.AssertClose(expected.GetProperty("y")[i].GetDouble(), bars[i].Value, $"month {i + 1}'s share");
            }
        }

        // The name a combo box of the panel shows for its selected item (its display name), even when the panel hides it.
        private static string SelectedName(ComboBox combo) =>
            combo.SelectedItem?.GetType().GetProperty("DisplayName")?.GetValue(combo.SelectedItem) as string ?? combo.SelectedItem?.ToString();

        // The walkthrough's table of saved frequency results (inches of daily precipitation).
        private static readonly BayesianResult[] Results =
        {
            new("USC00040741 - GEV", "Posterior Mean", "9.466", "7.472", "13.079", "90%", "1.00100", "9,026", "247.967", "6", "30", "10000"),
            new("USC00042402 - GEV", "Posterior Mean", "10.086", "8.879", "12.121", "90%", "1.00014", "9,255", "441.208", "6", "30", "10000"),
            new("USC00040741 - Point Process", "Posterior Mean", "9.471", "7.505", "12.88", "90%", "1.00056", "9,583", "372.8", "6", "30", "10000"),
            new("USC00042402 - Point Process", "Posterior Mean", "9.687", "8.703", "11.153", "90%", "1.00017", "9,429", "809.085", "6", "30", "10000"),
            new("USC00040741 - Seasonal Point Process", "Posterior Mean", "9.327", "7.538", "12.934", "90%", "1.00056", "6,007", "179.504", "16", "80", "10000"),
        };
    }
}
