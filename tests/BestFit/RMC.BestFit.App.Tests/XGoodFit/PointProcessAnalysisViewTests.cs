using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Data;
using RMC.BestFit.Estimation;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.PointProcessMixtureTesting;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Point Process Analysis view and its properties panel (GUI/UnivariateAnalysis/PointProcess)
    /// on the point process example's fits (3-point-process-analysis), as its walkthrough reads them.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports of Big Bear Lake's fit (its frequency plot with the POT
    /// and AMS data, and the diagnostics' kernel density, trace and autocorrelation of the location) and with the
    /// walkthrough's figures of the stations' frequency curves (the seasonal fit's with its seasons, and the GEV fits',
    /// which the Univariate view shows). The tables show upstream's own results in US formats (plan D8). Editing the
    /// properties writes to the element as upstream's bindings do, and the element's undo restores it; showing the
    /// views changes nothing in the element. The tests that only show the example share one copy of it.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class PointProcessAnalysisViewTests
    {
        private static readonly SharedExample Shared = new SharedExample(PointProcessExample);
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the shared example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Shared.Close();

        /// <summary>Closes the windows and the projects a test opened, and removes the application's resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Close();
            foreach (var project in _projects) Close(project);
            ReleaseAppResources();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// The fits RMC-BestFit exported or drew for the walkthrough: the views show what the desktop did, point for point.
        /// </summary>
        [STATestMethod]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures()
        {
            UseAppResources(ThemeColor.Light);
            var project = Shared.Project;
            var exports = NativeExports(PointProcessExample).ToList();
            // The walkthrough's frequency curves: the point-process fits', and the GEV fits' (the Univariate view's).
            var figures = Figures(PointProcessExample).Where(f => f.Figure.GetProperty("plotId").GetString() is "point_process.frequency" or "univariate.frequency").ToList();
            Assert.AreEqual(4, exports.Count);
            Assert.AreEqual(5, figures.Count);
            var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
            int compared = 0;
            foreach (string name in names)
            {
                IElement element = Elements(project).Single(e => e.Name == name);
                if (element is UnivariateAnalysis gev)
                {
                    var univariate = new UnivariateAnalysisControl { Element = gev };
                    Window shown = Show(univariate, 1300, 850);
                    try
                    {
                        foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                        {
                            Assert.AreEqual("univariate.frequency", figure.GetProperty("plotId").GetString(), file);
                            AssertFigure(gev.FrequencyPlot, figure, file);
                            compared++;
                        }
                    }
                    finally
                    {
                        UnivariateAnalysisViewTests.CloseDocument(shown);
                        univariate.Element = null;
                    }
                    continue;
                }
                var analysis = (PointProcessAnalysis)element;
                var view = new PointProcessAnalysisControl { Element = analysis };
                Window window = Show(view, 1300, 850);
                try
                {
                    Assert.AreSame(analysis.FrequencyPlot, view.FrequencyPlotHost.Content);
                    Assert.AreSame(analysis.FrequencyPlot, view.GetCurrentPlot(), "the frequency plot's tab is the one selected");
                    // The seasons' check box shows for a seasonal model only; both check boxes start checked.
                    Assert.AreEqual(analysis.PointProcess.IsSeasonal ? Visibility.Visible : Visibility.Collapsed, view.SeasonalCheckbox.Visibility, name);
                    Assert.IsTrue(view.POTCheckbox.IsChecked == true && view.SeasonalCheckbox.IsChecked == true, name);
                    foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                    {
                        Assert.AreEqual("point_process.frequency", figure.GetProperty("plotId").GetString(), file);
                        AssertFigure(analysis.FrequencyPlot, figure, file);
                        compared++;
                    }
                    foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                    {
                        Wpf.Plot plot;
                        switch (capture.GetProperty("plotId").GetString())
                        {
                            case "point_process.frequency":
                                Select(view.FrequencyResultsTabItem);
                                plot = analysis.FrequencyPlot;
                                break;
                            case "shared_diagnostics.kde":
                                Select(view.KernelDensityTabItem);
                                plot = view.KernelDensityControl.Plot;
                                break;
                            case "shared_diagnostics.trace":
                                Select(view.MarkovChainTabItem);
                                plot = view.MarkovChainTraceControl.Plot;
                                break;
                            case "shared_diagnostics.acf":
                                Select(view.AutocorrelationTabItem);
                                plot = view.AutocorrelationControl.Plot;
                                break;
                            default:
                                throw new AssertFailedException(file + ": an export this test doesn't know");
                        }
                        AssertNative(plot, capture, file);
                        compared++;
                    }
                    Select(view.DistributionResultsTabItem);
                }
                finally
                {
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                }
            }
            Assert.AreEqual(exports.Count + figures.Count, compared);
        }

        [STATestMethod]
        public void ResultsTables_ShowTheFrequencyCurveAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<PointProcessAnalysis>(Shared.Project, BigBear);
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes decimal
                // commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new PointProcessAnalysisControl { Element = element });
                Select(TabularResults(view));

                // One row per probability ordinate: the probability, then the curves in the user's value format.
                var results = element.AnalysisResults;
                var table = view.FrequencyCurveTable;
                string value = "{0:" + UserSettings.ValueStringFormat + "}";
                Assert.AreEqual(element.ProbabilityOrdinates.Count, table.Items.Count);
                for (int i = 0; i < element.ProbabilityOrdinates.Count; i++)
                {
                    string[] expected =
                    {
                        element.ProbabilityOrdinates[i].ToString(Us),
                        string.Format(Us, value, results.ConfidenceIntervals[i, 1]), string.Format(Us, value, results.ConfidenceIntervals[i, 0]),
                        string.Format(Us, value, results.MeanCurve[i]), string.Format(Us, value, results.ModeCurve[i]),
                    };
                    for (int column = 0; column < expected.Length; column++)
                        Assert.AreEqual(expected[column], CellText(table, i, column), $"row {i}, column {column}");
                }
                // The walkthrough's 1% AEP point estimate and limits (inches): 9.471, 7.505 to 12.88.
                int one = Enumerable.Range(0, element.ProbabilityOrdinates.Count).Single(i => Math.Abs(element.ProbabilityOrdinates[i] - 0.01) < 1E-12);
                Assert.AreEqual(9.471, results.ModeCurve[one], 5E-4);
                Assert.AreEqual(7.505, results.ConfidenceIntervals[one, 0], 5E-4);
                Assert.AreEqual(12.88, results.ConfidenceIntervals[one, 1], 5E-3);
                // The headers are upstream's: the interval's percentages in the code's (machine) culture, and the
                // point estimator.
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual("Posterior Mean", table.Columns[4].Header);

                // The summary statistics: the parameters, the annualized distribution's moments and the fit's criteria,
                // with rules above Minimum and AIC.
                var statistics = view.SummaryStatisticsTable;
                var model = element.PointProcess;
                double[] moments = model.Distribution.CentralMoments(1E-6);
                var rows = model.Parameters.Select(p => (p.DisplayName, p.Value))
                    .Concat(new[]
                    {
                        ("Minimum", model.Distribution.Minimum < -1E12 ? double.NegativeInfinity : model.Distribution.Minimum),
                        ("Maximum", model.Distribution.Maximum > 1E12 ? double.PositiveInfinity : model.Distribution.Maximum),
                        ("Mean", moments[0]), ("Std Dev", moments[1]), ("Skewness", moments[2]), ("Kurtosis", moments[3]),
                        ("AIC", results.AIC), ("BIC", results.BIC), ("DIC", results.DIC),
                        ("WAIC", element.BayesianAnalysis.WAIC), ("LOO-CV", element.BayesianAnalysis.LOOIC), ("RMSE", results.RMSE),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    DataGridRow row = statistics.GetRow(i);
                    Assert.AreEqual(rows[i].Item1 is "Minimum" or "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), row.BorderThickness, rows[i].Item1);
                }
                // The walkthrough's DIC.
                Assert.AreEqual(372.8, results.DIC, 0.05);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                UserSettings.ValueStringFormat = valueFormat;
            }
        }

        [STATestMethod]
        public void TheCheckBoxes_ShowAndHideThePotDataAndTheSeasons()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<PointProcessAnalysis>(Shared.Project, Seasonal);
            var view = Host(new PointProcessAnalysisControl { Element = element });
            var plot = element.FrequencyPlot;
            string Names() => string.Join(",", plot.Series.Select(s => s.Name));
            const string all = "PosteriorModeS1,PosteriorModeS2,CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData,AMSExactData";
            Assert.AreEqual(all, Names());
            Assert.AreEqual(Visibility.Visible, view.SeasonalCheckbox.Visibility);

            // The seasons' curves go with their check box, the POT data with its own; the AMS data stays.
            view.SeasonalCheckbox.IsChecked = false;
            Settle();
            Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData,AMSExactData", Names());
            view.POTCheckbox.IsChecked = false;
            Settle();
            Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,AMSExactData", Names());
            view.POTCheckbox.IsChecked = true;
            view.SeasonalCheckbox.IsChecked = true;
            Settle();
            Assert.AreEqual(all, Names());
            // The seasons' curves are each season's GEV, by the point estimator.
            var seasons = (Numerics.Distributions.CompetingRisks)element.AnalysisResults.ParentDistribution;
            var first = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorModeS1");
            Assert.AreEqual("Posterior Mean - Season 1", first.Title);
            var points = ((IEnumerable<OxyPlot.DataPoint>)first.ItemsSource).ToList();
            Assert.AreEqual(element.ProbabilityOrdinates.Count, points.Count);
            Assert.AreEqual(seasons.Distributions[0].InverseCDF(1 - points[3].X), points[3].Y);
            Assert.IsFalse(element.UndoManager.CanUndo, "the check boxes record nothing");
            Assert.IsFalse(element.IsDirty, "the check boxes change nothing in the element");

            // A stationary fit has no seasons' check box.
            view.Element = Element<PointProcessAnalysis>(Shared.Project, BigBear);
            Settle();
            Assert.AreEqual(Visibility.Collapsed, view.SeasonalCheckbox.Visibility);
            Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData,AMSExactData",
                string.Join(",", view.Element.FrequencyPlot.Series.Select(s => s.Name)));
            view.Element = null;
        }

        [STATestMethod]
        public void PreviewClicks_TellTheShellWhetherThePlotOrToolbarWasClicked_AndAlternativesAddTheirCurves()
        {
            UseAppResources(ThemeColor.Light);
            var project = Shared.Project;
            var element = Element<PointProcessAnalysis>(project, BigBear);
            var view = Host(new PointProcessAnalysisControl { Element = element });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => called.Add(plot);

            Click(window, Centre(window, element.FrequencyPlot));
            Assert.AreEqual((true, false, element.FrequencyPlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);
            Point toolbar = HitPoint(window, view.FrequencyPlotToolbar);
            window.MouseMove(toolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.FrequencyPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            // The label beside the toggle button shows and hides the alternatives, as the button does.
            Assert.IsFalse(view.AlternativeSelector.IsVisible);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsTrue(view.ShowFilterToggleButton.IsChecked == true);
            Assert.IsTrue(view.AlternativeSelector.IsVisible);

            // The project's other univariate analyses: the GEV fits too. One adds its curves, named by the series
            // count, in the first unused colour of upstream's list from the fifth on.
            var others = Elements(project).OfType<IAnalysisElement>().Where(e => e != element && e.ParentCollection == element.ParentCollection).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var gev = Element<UnivariateAnalysis>(project, "USC00040741 - GEV");
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == gev);
            var frequency = element.FrequencyPlot;
            int index = frequency.Series.Count;
            item.IsChecked = true;
            Settle();
            var color = Avalonia.Media.Color.Parse(GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)frequency.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var point = (Wpf.LineSeries)frequency.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual("USC00040741 - GEV - 90% Credible Intervals", interval.Title);
            Assert.AreEqual("USC00040741 - GEV - Posterior Mean", point.Title);
            Assert.AreEqual(color, point.Color);
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(gev.ProbabilityOrdinates[i], gev.AnalysisResults.ModeCurve[i]), points[i]);
            // Hiding the POT data redraws the plot with the alternative still in it.
            view.POTCheckbox.IsChecked = false;
            Settle();
            Assert.IsTrue(frequency.Series.Any(s => s.Name.StartsWith("PosteriorMode_", StringComparison.Ordinal)));
            view.POTCheckbox.IsChecked = true;
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(frequency.Series.Any(s => s.Name.Contains('_')));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");

            // A diagnostic plot's toolbar asks for its plot's properties through the view.
            Select(TabularResults(view));
            Select(view.HistogramTabItem);
            Assert.AreSame(view.HistogramControl.Plot, view.GetCurrentPlot());
            Click(window, HitPoint(window, view.HistogramControl.PlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.BayesianPlots.HistogramPlot, called.Last());
            Select(view.DistributionResultsTabItem);
            Select(view.FrequencyResultsTabItem);
            view.Element = null;
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(PointProcessExample);
            var element = Element<PointProcessAnalysis>(project, DeSabla);
            var view = Host(new PointProcessAnalysisControl { Element = element });
            var properties = Host(new PointProcessAnalysisPropertiesControl { Element = element }, 420, 1100);
            // Upstream's undo restores the model from a snapshot, a new PointProcessModel: the panel and the tests read the element's.

            // General: the element's name and dates, its input data, threshold and exposure, and its priors.
            Assert.AreEqual(DeSabla, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            Assert.AreSame(element.InputData, inputData.SelectedItem);
            var listed = inputData.Items.Cast<InputData>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<InputData>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            Assert.AreEqual(new Thickness(0), ((Border)properties.InputDataComboBox.InnerContent).BorderThickness);
            Assert.AreEqual(element.PointProcess.Threshold, properties.Threshold.Number);
            Assert.AreEqual(element.PointProcess.TotalYears, properties.TotalYears.Number);
            Assert.AreEqual(element.PointProcess.UseDefaults, properties.UseDefaults.IsSelected);
            Assert.AreEqual(!element.PointProcess.UseDefaults, properties.Threshold.IsTextBoxEnabled);
            Assert.AreEqual(!element.PointProcess.UseDefaults, properties.TotalYears.IsTextBoxEnabled);
            Assert.IsFalse(element.PointProcess.IsSeasonal);
            Assert.AreEqual(Visibility.Collapsed, properties.TimeBlock.Visibility, "a stationary model has no block year");
            Assert.AreEqual(Visibility.Collapsed, properties.StartMonth.Visibility);
            Assert.AreSame(element.PointProcess, properties.ParameterPriorsControl.Model);
            Assert.AreSame(element.PointProcess, properties.QuantilePriorsControl.Model);
            Assert.AreEqual(element.PointProcess.UseJeffreysRuleForScale, properties.UseJeffreysRule.IsSelected);

            properties.Description.Text = "Daily precipitation above 2.5 in";
            Assert.AreEqual("Daily precipitation above 2.5 in", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Daily precipitation above 2.5 in", element.Description);

            // A new credible interval: the table's headers at once, and the curves once the element has reprocessed
            // them (on a worker thread, as upstream's model layer does), with the wait cursor until then.
            Select(TabularResults(view));
            var table = view.FrequencyCurveTable;
            double saved = element.AnalysisResults.ConfidenceIntervals[0, 1];
            Select(properties.Output_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOutputControl.Analysis);
            Assert.AreSame(element.ProbabilityOrdinates, properties.ProbabilityOrdinatesControl.ProbabilityOrdinates);
            element.BayesianAnalysis.CredibleIntervalWidth = 0.99;
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.99);
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) is double upper && upper != saved && Mouse.OverrideCursor == null, "the reprocessed curves");
            Assert.AreEqual("99% Credible Intervals", element.FrequencyPlot.Series.Single(s => s.Name == "CredibleIntervals").Title);
            element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) == saved && Mouse.OverrideCursor == null, "the curves back");
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.9);
            Select(properties.General_TabItem);

            // The threshold and exposure are editable once the defaults are off; a new exposure moves the annual
            // maxima's plotting positions (their Langbein conversion), and its undo restores the exposure.
            int undos = element.UndoManager.UndoStack.Count;
            if (element.PointProcess.UseDefaults)
            {
                Tick(properties.UseDefaults, false);
                Settle();
                Assert.IsFalse(element.PointProcess.UseDefaults);
            }
            Assert.IsTrue(properties.Threshold.IsTextBoxEnabled && properties.TotalYears.IsTextBoxEnabled);
            double threshold = element.PointProcess.Threshold, years = element.PointProcess.TotalYears;
            properties.Threshold.Number = threshold + 0.5;
            Assert.AreEqual(threshold + 0.5, element.PointProcess.Threshold);
            var ams = (Wpf.ScatterPointSeries)element.FrequencyPlot.Series.Single(s => s.Name == "AMSExactData");
            double FirstAmsPosition() => ((IEnumerable<object>)ams.ItemsSource).Cast<ExactData>().First().PlottingPosition;
            double position = FirstAmsPosition();
            properties.TotalYears.Number = years * 2;
            Settle();
            Assert.AreEqual(years * 2, element.PointProcess.TotalYears);
            double changed = FirstAmsPosition();
            Assert.AreNotEqual(position, changed, "the AMS data follow the exposure");
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(threshold, element.PointProcess.Threshold);
            Assert.AreEqual(years, element.PointProcess.TotalYears);
            // As upstream's view: undo restores the model from a snapshot, and the element announces a new model
            // (PointProcess), not a new exposure, so the view keeps the undone exposure's AMS data until it's shown
            // again (an upstream bug). A new view of the element shows the restored exposure's.
            Assert.AreEqual(changed, FirstAmsPosition(), "upstream's view keeps the undone exposure's AMS data");
            var again = Host(new PointProcessAnalysisControl { Element = element });
            Assert.AreEqual(position, FirstAmsPosition(), "a new view shows the restored exposure's AMS data");
            again.Element = null;
            Assert.AreEqual(element.PointProcess.UseDefaults, properties.UseDefaults.IsSelected);
            Assert.AreEqual(element.PointProcess.Threshold, properties.Threshold.Number, "the restored threshold shows");
            Assert.AreSame(element.PointProcess, properties.ParameterPriorsControl.Model, "the priors edit the restored model");
            Assert.AreSame(element.PointProcess, properties.QuantilePriorsControl.Model);

            // A seasonal model shows its block year and start month, a calendar year no start month; undo goes back
            // to the stationary model (upstream's setter also resets the parameters: several undo steps).
            undos = element.UndoManager.UndoStack.Count;
            Tick(properties.IsSeasonal, true);
            Settle();
            Assert.IsTrue(element.PointProcess.IsSeasonal);
            Assert.AreEqual(Visibility.Visible, properties.TimeBlock.Visibility);
            Assert.AreEqual(element.PointProcess.TimeBlock == TimeBlockWindow.CalendarYear ? Visibility.Collapsed : Visibility.Visible, properties.StartMonth.Visibility);
            Assert.AreEqual(Visibility.Visible, view.SeasonalCheckbox.Visibility, "the view's seasons' check box shows");
            var timeBlock = (ComboBox)properties.TimeBlock.InnerContent;
            var startMonth = (ComboBox)properties.StartMonth.InnerContent;
            Assert.AreEqual(element.PointProcess.TimeBlock, timeBlock.SelectedValue);
            Assert.AreEqual(element.PointProcess.StartMonth, startMonth.SelectedValue);
            Assert.AreEqual("October", ((MonthItem)startMonth.SelectedItem).DisplayName, "the walkthrough's October block start");
            startMonth.SelectedItem = properties.MonthOptions.Single(m => m.Value == 8);
            Assert.AreEqual(8, element.PointProcess.StartMonth);
            timeBlock.SelectedItem = properties.TimeBlockOptions.Single(t => t.Value == TimeBlockWindow.CalendarYear);
            Settle();
            Assert.AreEqual(TimeBlockWindow.CalendarYear, element.PointProcess.TimeBlock);
            Assert.AreEqual(Visibility.Collapsed, properties.StartMonth.Visibility, "a calendar year starts in January");
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.IsFalse(element.PointProcess.IsSeasonal);
            Assert.AreEqual(TimeBlockWindow.WaterYear, element.PointProcess.TimeBlock);
            Assert.AreEqual(10, element.PointProcess.StartMonth);
            Assert.AreEqual(10, startMonth.SelectedValue, "the restored start month shows");
            Assert.AreEqual(Visibility.Collapsed, properties.TimeBlock.Visibility);
            Assert.AreEqual(Visibility.Collapsed, view.SeasonalCheckbox.Visibility);

            bool jeffreys = element.PointProcess.UseJeffreysRuleForScale;
            Tick(properties.UseJeffreysRule, !jeffreys);
            Assert.AreEqual(!jeffreys, element.PointProcess.UseJeffreysRuleForScale);
            element.UndoManager.Undo();
            Assert.AreEqual(jeffreys, properties.UseJeffreysRule.IsSelected);

            // The sampler's settings, as the Bayesian options control edits them.
            Select(properties.Options_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOptionsControl.Analysis);
            properties.BayesianOptionsControl.UseSimulationDefaults.IsSelected = false;
            int chains = element.BayesianAnalysis.NumberOfChains;
            properties.BayesianOptionsControl.NumberOfChains.Number = chains + 2;
            Assert.AreEqual(chains + 2, element.BayesianAnalysis.NumberOfChains);
            element.UndoManager.Undo();
            Assert.AreEqual(chains, element.BayesianAnalysis.NumberOfChains);
            element.UndoManager.Undo();
            Select(properties.General_TabItem);

            // The input data last: another clears the fit's results, as upstream's element does.
            InputData data = element.InputData;
            var another = listed.First(i => i != data);
            inputData.SelectedItem = another;
            Assert.AreSame(another, element.InputData);
            Assert.IsFalse(element.IsEstimated);
            element.UndoManager.Undo();
            Settle();
            Assert.AreSame(data, element.InputData);
            Assert.AreSame(data, inputData.SelectedItem);
            properties.Element = null;
            view.Element = null;
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = Shared.Project;
            foreach (var element in Elements(project).OfType<PointProcessAnalysis>().ToList())
            {
                string name = element.Name;
                var changes = new List<string>();
                Watch(changes, element, element.PointProcess, element.BayesianAnalysis, element.InputData);
                Watch(changes, element.PointProcess.Parameters.Cast<object>().ToArray());

                // Two documents of the element, the first still holding it when the second opens, as upstream's
                // MainProjectNode leaves a closed one: the second takes the plots.
                var views = new List<PointProcessAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new PointProcessAnalysisControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.ShowFilterToggleButton.IsChecked = true;
                    Select(TabularResults(view));
                    if (document == 0)
                    {
                        foreach (TabItem tab in ((TabControl)view.DistributionResultsTabItem.Parent).Items.Cast<TabItem>())
                            Select(tab);
                        Select(view.DistributionResultsTabItem);
                    }
                    Select(view.FrequencyResultsTabItem);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                }
                var second = views[1];
                Assert.AreSame(element.FrequencyPlot, second.FrequencyPlotHost.Content, name);
                var plots = element.BayesianPlots;
                Assert.AreSame(plots.KernelDensityPlot, second.KernelDensityControl.PlotHost.Content, name);
                Assert.AreSame(plots.HistogramPlot, second.HistogramControl.PlotHost.Content, name);
                Assert.AreSame(plots.BivariateHeatMapPlot, second.BivariateHeatMapControl.PlotHost.Content, name);
                Assert.AreSame(plots.MeanLikelihoodPlot, second.MeanLikelihoodControl.PlotHost.Content, name);
                Assert.AreSame(plots.AutocorrelationPlot, second.AutocorrelationControl.PlotHost.Content, name);
                Assert.AreSame(plots.MarkovChainTracePlot, second.MarkovChainTraceControl.PlotHost.Content, name);
                Assert.AreSame(plots.InfluenceDiagnosticsPlot, second.InfluenceDiagnosticsControl.PlotHost.Content, name);

                // The properties panel, on each of its tabs, shown twice.
                for (int panel = 0; panel < 2; panel++)
                {
                    var properties = new PointProcessAnalysisPropertiesControl { Element = element };
                    Window window = Show(properties, 420, 1100);
                    Select(properties.Options_TabItem);
                    Select(properties.Output_TabItem);
                    Select(properties.General_TabItem);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                }

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                foreach (var view in views) view.Element = null;
            }
        }

        // The first upper interval value, or null while the element is reprocessing its results.
        private static double? Upper(PointProcessAnalysis element) =>
            element.AnalysisResults?.ConfidenceIntervals is double[,] intervals && intervals.GetLength(0) > 0 ? intervals[0, 1] : null;

        /// <summary>The Distribution Results tab's Tabular Results tab.</summary>
        internal static TabItem TabularResults(PointProcessAnalysisControl view) =>
            ((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Single(t => (string)t.Header == "Tabular Results");

        private BestFitProject OpenProject(string relative)
        {
            var project = Open(relative);
            _projects.Add(project);
            return project;
        }

        private T Host<T>(T control, double width = 1300, double height = 850) where T : Control
        {
            _windows.Add(Show(control, width, height));
            return control;
        }

        private static string CellText(CopyPasteDataGrid grid, int row, int column) =>
            (grid.GetCell(row, column)?.Content as TextBlock)?.Text;
    }
}
