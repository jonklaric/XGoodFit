using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Distributions;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.PointProcessMixtureTesting;
using static RMC.BestFit.App.Tests.XGoodFit.RatingCurveTimeSeriesTesting;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Time Series Analysis view and its properties panel (GUI/TimeSeriesAnalysis) on the time
    /// series examples (7-time-series-analysis), as their walkthroughs read them.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports (the series plots of the log-transformed synthetic model,
    /// the airline passengers' and the simple regression's, the first's residual ACF, and the others' intercept chain
    /// ACF) and with the walkthroughs' figures. The tables show upstream's own results in US formats (plan D8), dates
    /// included. Editing the properties writes to the element as upstream's bindings do, and the element's undo restores
    /// it; showing the views changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class TimeSeriesAnalysisViewTests
    {
        private static readonly SharedExample Classic = new SharedExample(ClassicSeries);
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the shared example.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExample() => Classic.Close();

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
        /// The models RMC-BestFit exported or drew for the walkthroughs: the views show what the desktop did, point for
        /// point. Each example is opened in turn and closed after.
        /// </summary>
        [STATestMethod]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures()
        {
            UseAppResources(ThemeColor.Light);
            int compared = 0, expected = 0;
            var disagreements = new List<string>();
            foreach (string example in new[] { SyntheticSeries, ClassicSeries, Regression })
            {
                var exports = NativeExports(example).ToList();
                var figures = Figures(example).ToList();
                expected += exports.Count + figures.Count;
                var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                    .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
                var project = example == ClassicSeries ? Classic.Project : Open(example);
                try
                {
                    foreach (string name in names)
                    {
                        var element = Element<TimeSeriesAnalysis>(project, name);
                        var view = new TimeSeriesAnalysisControl { Element = element };
                        Window window = Show(view, 1300, 850);
                        try
                        {
                            Assert.AreSame(element.TimeSeriesPlot, view.TimeSeriesPlotHost.Content);
                            Assert.AreSame(element.TimeSeriesPlot, view.GetCurrentPlot(), "the series plot's tab is the one selected");
                            foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                            {
                                AssertNativeExport(Plot(view, capture.GetProperty("plotId").GetString(), file), capture, file);
                                compared++;
                            }
                            foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                            {
                                try
                                {
                                    AssertWalkthroughFigure(Plot(view, figure.GetProperty("plotId").GetString(), file), figure, file);
                                }
                                catch (AssertFailedException e)
                                {
                                    disagreements.Add(file);
                                    Console.WriteLine(e.Message);
                                }
                                compared++;
                            }
                            Select(view.ResultsTabItem);
                        }
                        finally
                        {
                            UnivariateAnalysisViewTests.CloseDocument(window);
                            view.Element = null;
                        }
                    }
                }
                finally
                {
                    if (example != ClassicSeries) Close(project);
                    GC.Collect();
                }
            }
            // RMC-BestFit's six native exports and the walkthroughs' 25 figures.
            Assert.AreEqual(31, expected);
            Assert.AreEqual(expected, compared);
            // The classic walkthrough's airline figures were drawn from an earlier version of the project file (their
            // runId, sha256 cc227c72...), whose airline fit has since been re-run; the shipped file's (32fd922f...) is
            // what RMC-BestFit's native exports show, and the view. Its other models are unchanged, and so are their figures.
            CollectionAssert.AreEquivalent(StaleFigures, disagreements, "the figures that don't show the saved results");
        }

        [STATestMethod]
        public void ResultsTables_ShowTheSeriesAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<TimeSeriesAnalysis>(Classic.Project, Nile);
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes decimal
                // commas and day-first dates: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new TimeSeriesAnalysisControl { Element = element });
                Select(TabularResults(view));

                // One row per time step: the date, then the values in the user's value format.
                var results = element.AnalysisResults;
                var table = view.TimeSeriesTable;
                string value = "{0:" + UserSettings.ValueStringFormat + "}";
                var dates = TimeSeriesResultTimeline.CreateDates(element.ARIMAX.TimeSeries, results.MeanCurve.Length);
                Assert.AreEqual(100, results.MeanCurve.Length, "the 100 years, with no forecast");
                Assert.AreEqual(results.MeanCurve.Length, table.Items.Count);
                for (int i = 0; i < results.MeanCurve.Length; i++)
                {
                    string[] expected =
                    {
                        dates[i].ToString(Us),
                        string.Format(Us, value, results.ConfidenceIntervals[i, 2]), string.Format(Us, value, results.ConfidenceIntervals[i, 1]),
                        string.Format(Us, value, results.MeanCurve[i]), string.Format(Us, value, i < results.ModeCurve.Length ? results.ModeCurve[i] : double.NaN),
                    };
                    for (int column = 0; column < expected.Length; column++)
                        Assert.AreEqual(expected[column], CellText(table, i, column), $"row {i}, column {column}");
                }
                // ICU's en-US time format on Linux and macOS puts a narrow no-break space (U+202F) before AM.
                Assert.AreEqual("1/1/1897 12:00:00 AM", CellText(table, 0, 0).Replace((char)0x202F, ' '), "a US date");
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual("Date Time", table.Columns[0].Header);
                Assert.AreEqual("Posterior Mean", table.Columns[4].Header);

                // The summary statistics: the model's parameters with their significance codes (the posterior's mass
                // beyond zero, but the error scale's), and the fit's criteria, with a rule above AIC; the codes' legend.
                var statistics = view.SummaryStatisticsTable;
                var model = element.ARIMAX;
                var output = element.BayesianAnalysis.Results.Output;
                var rows = model.Parameters.Select((p, i) => (p.DisplayName, p.Value, Code: i < model.Parameters.Count - 1 ? Code(output.Select(set => set.Values[i]).ToArray()) : ""))
                    .Concat(new[]
                    {
                        ("AIC", results.AIC, ""), ("BIC", results.BIC, ""), ("DIC", results.DIC, ""),
                        ("WAIC", element.BayesianAnalysis.WAIC, ""), ("LOO-CV", element.BayesianAnalysis.LOOIC, ""), ("RMSE", results.RMSE, ""),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                Assert.AreEqual("Signif.", statistics.Columns[2].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    Assert.AreEqual(rows[i].Item3, CellText(statistics, i, 2) ?? "", rows[i].Item1 + "'s code");
                    DataGridRow row = statistics.GetRow(i);
                    Assert.AreEqual(rows[i].Item1 == "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), row.BorderThickness, rows[i].Item1);
                }
                Assert.IsTrue(view.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "Significance codes:  0 '***' 0.001 '**' 0.01 '*' 0.05 '.' 0.1 ' ' 1"));
                // The walkthrough's DIC and RMSE, and its MCMC report's largest R-hat and smallest effective sample size.
                Assert.AreEqual(1016.319, results.DIC, 5E-4);
                Assert.AreEqual(156.318, results.RMSE, 5E-4);
                var sampled = element.BayesianAnalysis.Results.ParameterResults;
                Assert.AreEqual(1.00041, sampled.Max(r => r.SummaryStatistics.Rhat), 5E-6);
                Assert.AreEqual(6166, sampled.Min(r => r.SummaryStatistics.ESS), 1);
                view.Element = null;

                // The airline passengers' saved fit isn't the one the walkthrough describes (DIC 1,115.279, RMSE 26.65, a
                // 120-month training window), which an earlier version of the project file had: the shipped file's fit was
                // re-run on the default rule's 115 months, and RMC-BestFit's native export shows it, as the view does.
                var airline = Element<TimeSeriesAnalysis>(Classic.Project, Airline);
                Assert.AreEqual(115, airline.TrainingTimeSteps);
                Assert.IsTrue(airline.UseDefaultTrainingSteps);
                Assert.AreEqual(1048.945, airline.AnalysisResults.DIC, 5E-4);
                Assert.AreEqual(24.206, airline.AnalysisResults.RMSE, 5E-4);
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                UserSettings.ValueStringFormat = valueFormat;
            }
        }

        [STATestMethod]
        public void PreviewClicks_TellTheShellWhetherThePlotOrToolbarWasClicked_AndAlternativesAddTheirCurves()
        {
            UseAppResources(ThemeColor.Light);
            var project = Classic.Project;
            var element = Element<TimeSeriesAnalysis>(project, Nile);
            var view = Host(new TimeSeriesAnalysisControl { Element = element });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => called.Add(plot);

            Click(window, Centre(window, element.TimeSeriesPlot));
            Assert.AreEqual((true, false, element.TimeSeriesPlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);
            Point toolbar = HitPoint(window, view.PlotToolbar);
            window.MouseMove(toolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.TimeSeriesPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            // The label beside the toggle button shows and hides the alternatives, as the button does.
            Assert.IsFalse(view.AlternativeSelector.IsVisible);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsTrue(view.ShowFilterToggleButton.IsChecked == true);
            Assert.IsTrue(view.AlternativeSelector.IsVisible);

            // The project's other models. One adds its curves, named by the series count, in the first unused colour of
            // upstream's list from the fifth on; its points are at its own series' dates.
            var others = Elements(project).OfType<TimeSeriesAnalysis>().Where(e => e != element).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var airline = Element<TimeSeriesAnalysis>(project, Airline);
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == airline);
            var series = element.TimeSeriesPlot;
            int index = series.Series.Count;
            item.IsChecked = true;
            Settle();
            var color = Avalonia.Media.Color.Parse(GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)series.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var predictive = (Wpf.LineSeries)series.Series.Single(s => s.Name == "PosteriorPredictive_" + index);
            var point = (Wpf.LineSeries)series.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual("Airline Passengers - TSA - 90% Credible Intervals", interval.Title);
            Assert.AreEqual("Airline Passengers - TSA - Posterior Predictive", predictive.Title);
            Assert.AreEqual("Airline Passengers - TSA - Posterior Mean", point.Title);
            Assert.AreEqual(color, point.Color);
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            var dates = TimeSeriesResultTimeline.CreateDates(airline.ARIMAX.TimeSeries, airline.AnalysisResults.ModeCurve.Length);
            Assert.AreEqual(airline.AnalysisResults.ModeCurve.Length, points.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(dates[i].ToOADate(), airline.AnalysisResults.ModeCurve[i]), points[i]);
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(series.Series.Any(s => s.Name.Contains('_')));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");

            // A residual tab's plot is the current plot once the results show their table (upstream's order, as the
            // Rating Curve view's: IssuesToRaise/RMC-BestFit-current-plot-ignores-diagnostic-tabs.md).
            Assert.AreSame(element.TimeSeriesPlot, view.GetCurrentPlot());
            Select(TabularResults(view));
            Select(view.ResidualsTabItem);
            foreach (var (tab, plot, plotToolbar) in new[]
            {
                (view.ResidualHistogramTab, element.ResidualHistogramPlot, view.HistogramPlotToolbar),
                (view.ResidualQQTab, element.ResidualQQPlot, view.QQPlotToolbar),
                (view.ResidualACFTab, element.ResidualACFPlot, view.ACFPlotToolbar),
                (view.ResidualPACFTab, element.ResidualPACFPlot, view.PACFPlotToolbar),
            })
            {
                Select(tab);
                Assert.AreSame(plot, view.GetCurrentPlot(), (string)tab.Header);
                Assert.AreSame(plotToolbar, view.GetCurrentPlotToolbar(), (string)tab.Header);
            }
            Select(view.KernelDensityTabItem);
            Assert.AreSame(element.ResidualPACFPlot, view.GetCurrentPlot(), "upstream's order: the residual tab first");
            // A diagnostic plot's toolbar asks for its plot's properties through the view all the same.
            Click(window, HitPoint(window, view.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.BayesianPlots.KernelDensityPlot, called.Last());
            Select(view.ResidualsTabItem);
            Select(view.ResidualsPlotTab);
            Select(view.ResultsTabItem);
            Select(view.PlotTabItem);
            view.Element = null;
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Regression);
            var element = Element<TimeSeriesAnalysis>(project, SimpleRegression);
            var view = Host(new TimeSeriesAnalysisControl { Element = element });
            var properties = Host(new TimeSeriesAnalysisPropertiesControl { Element = element }, 420, 1100);
            // Upstream's undo restores the model from a snapshot, a new ARIMAX: the panel and the tests read the element's.

            // General: the element's name and dates, its series and transform, and its priors.
            Assert.AreEqual(SimpleRegression, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var data = (ComboBox)((Border)properties.TimeSeriesDataComboBox.InnerContent).Child;
            Assert.AreSame(element.TimeSeriesData, data.SelectedItem);
            Assert.AreEqual("Consumption", element.TimeSeriesData.Name);
            var listed = data.Items.Cast<TimeSeriesElement>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<TimeSeriesElement>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            Assert.AreEqual(new Thickness(0), ((Border)properties.TimeSeriesDataComboBox.InnerContent).BorderThickness, "a valid series");
            Assert.IsNull(properties.TimeSeriesDataComboBox.ToolTip);
            var transform = (ComboBox)properties.TransformType.InnerContent;
            Assert.AreEqual(element.ARIMAX.TransformType, transform.SelectedValue);
            Assert.AreEqual("None", ((TransformTypeItem)transform.SelectedItem).DisplayName);
            Assert.AreSame(element.ARIMAX, properties.ParameterPriorsControl.Model);
            Assert.AreEqual(element.ARIMAX.UseJeffreysRuleForScale, properties.UseJeffreysRule.IsSelected);

            properties.Description.Text = "Consumption on income";
            Assert.AreEqual("Consumption on income", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Consumption on income", element.Description);

            // Output: the training window (the default rule's), the validation steps it leaves, the forecast, and how
            // the covariate is extended past the data.
            Select(properties.Output_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOutputControl.Analysis);
            int count = element.TimeSeriesData.TimeSeries.Count;
            var training = (GenericControls.NumericUpDown)properties.TrainingSteps.InnerContent;
            var validation = (TextBlock)properties.ValidationSteps.InnerContent;
            var forecast = (GenericControls.NumericUpDown)properties.ForecastingSteps.InnerContent;
            var extension = (ComboBox)properties.CovariateExtension.InnerContent;
            int minimum = Math.Min(Math.Max(10, element.ARIMAX.Parameters.Count), count);
            Assert.IsTrue(element.UseDefaultTrainingSteps);
            Assert.AreEqual(149, element.TrainingTimeSteps);
            Assert.AreEqual(element.TrainingTimeSteps, training.Value);
            Assert.AreEqual(minimum, training.Minimum);
            Assert.AreEqual(Math.Max(count, element.TrainingTimeSteps), training.Maximum);
            Assert.AreEqual($"Must be between {minimum} and {count} based on the number of parameters and data points.", training.ToolTip);
            Assert.AreEqual((count - element.TrainingTimeSteps).ToString(CultureInfo.InvariantCulture), validation.Text);
            Assert.AreEqual(30, element.ForecastSteps);
            Assert.AreEqual(30, forecast.Value);
            Assert.AreEqual(element.CovariateExtension, extension.SelectedValue);
            Assert.AreEqual("Block Bootstrap", ((CovariateExtensionItem)extension.SelectedItem).DisplayName);

            // A new credible interval: the table's headers at once, and the curves once the element has reprocessed
            // them (on a worker thread, as upstream's model layer does), with the wait cursor until then.
            Select(TabularResults(view));
            var table = view.TimeSeriesTable;
            double saved = Upper(element).Value;
            element.BayesianAnalysis.CredibleIntervalWidth = 0.99;
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.99);
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) is double upper && upper != saved && Mouse.OverrideCursor == null
                && Interval(element) == "99% Credible Intervals — Training" && table.Items.Cast<FrequencyCurvePoint>().Last().Upper == upper, "the reprocessed curves");
            element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) == saved && Mouse.OverrideCursor == null
                && Interval(element) == "90% Credible Intervals — Training" && table.Items.Cast<FrequencyCurvePoint>().Last().Upper == saved, "the curves back");
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.9);

            // A shorter forecast: the element reprocesses the curves, which end sooner; undo restores the forecast.
            int rows = table.Items.Count;
            Assert.AreEqual(count + 30, rows, "the series and its 30 forecast steps");
            forecast.Value = 10;
            Assert.AreEqual(10, element.ForecastSteps);
            UnivariateAnalysisViewTests.PumpUntil(() => element.AnalysisResults?.MeanCurve?.Length == count + 10 && Mouse.OverrideCursor == null
                && table.Items.Count == count + 10, "the curves with 10 forecast steps");
            element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => element.AnalysisResults?.MeanCurve?.Length == count + 30 && Mouse.OverrideCursor == null
                && table.Items.Count == count + 30, "the curves with 30 forecast steps again");
            Assert.AreEqual(30, forecast.Value);

            // A manual training window: the validation steps follow it, and it clears the fit (as upstream's model);
            // Reset gives the default rule back and no forecast. Undo restores the saved window and forecast.
            int undos = element.UndoManager.UndoStack.Count;
            training.Value = 140;
            Settle();
            Assert.AreEqual(140, element.TrainingTimeSteps);
            Assert.IsFalse(element.UseDefaultTrainingSteps);
            Assert.AreEqual((count - 140).ToString(CultureInfo.InvariantCulture), validation.Text);
            Assert.AreEqual(count, training.Maximum, "a manual window is at most the series");
            Assert.IsFalse(element.IsEstimated, "a new training window clears the fit");
            properties.ResetButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Settle();
            Assert.IsTrue(element.UseDefaultTrainingSteps);
            Assert.AreEqual(149, element.TrainingTimeSteps);
            Assert.AreEqual(0, element.ForecastSteps);
            Assert.AreEqual(0, forecast.Value);
            Assert.AreEqual((count - 149).ToString(CultureInfo.InvariantCulture), validation.Text);
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(149, element.TrainingTimeSteps);
            Assert.IsTrue(element.UseDefaultTrainingSteps);
            Assert.AreEqual(30, element.ForecastSteps);
            Assert.AreEqual(30, forecast.Value);
            Select(properties.General_TabItem);

            // Options: the covariate grid lists the regression's covariate, whose selector offers the project's series;
            // the model's terms and orders write to the model, and undo restores them.
            Select(properties.Options_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOptionsControl.Analysis);
            Assert.AreSame(element.Covariates, properties.CovariateDataGrid.ItemsSource);
            Assert.AreEqual(1, element.Covariates.Count);
            Assert.AreEqual("Income", element.Covariates[0].TimeSeriesElement.Name);
            var covariate = properties.CovariateDataGrid.GetVisualDescendants().OfType<ComboBox>().Single();
            Assert.AreSame(element.Covariates[0].TimeSeriesElement, covariate.SelectedItem);
            CollectionAssert.AreEqual(listed, covariate.Items.Cast<TimeSeriesElement>().ToList());
            Assert.AreEqual(element.ARIMAX.IncludeIntercept, properties.InlcudeIntercept.IsSelected);
            Assert.AreEqual(element.ARIMAX.IncludeSeasonality, properties.InlcudeSeasonality.IsSelected);
            var trend = (ComboBox)properties.TrendType.InnerContent;
            Assert.AreEqual(element.ARIMAX.TrendType, trend.SelectedValue);
            var orders = new[] { properties.XOrderB, properties.AROrderP, properties.DiffOrderD, properties.MAOrderQ }
                .Select(c => (GenericControls.NumericUpDown)c.InnerContent).ToArray();
            CollectionAssert.AreEqual(new double[] { element.ARIMAX.XOrderB, element.ARIMAX.AROrderP, element.ARIMAX.DiffOrderD, element.ARIMAX.MAOrderQ },
                orders.Select(o => o.Value).ToArray());

            undos = element.UndoManager.UndoStack.Count;
            Tick(properties.InlcudeSeasonality, true);
            Assert.IsTrue(element.ARIMAX.IncludeSeasonality);
            trend.SelectedItem = properties.TrendTypeItems.Single(t => t.Value == ARIMAX.Trend.Linear);
            Assert.AreEqual(ARIMAX.Trend.Linear, element.ARIMAX.TrendType);
            orders[1].Value = 2;
            Settle();
            Assert.AreEqual(2, element.ARIMAX.AROrderP);
            Assert.IsFalse(element.IsEstimated, "a new model clears the fit");
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.IsFalse(element.ARIMAX.IncludeSeasonality);
            Assert.AreEqual(ARIMAX.Trend.None, element.ARIMAX.TrendType);
            Assert.AreEqual(0, element.ARIMAX.AROrderP);
            Assert.AreEqual(0, orders[1].Value, "the restored order shows");
            Assert.AreEqual(ARIMAX.Trend.None, trend.SelectedValue);
            Assert.AreSame(element.ARIMAX, properties.ParameterPriorsControl.Model, "the priors edit the restored model");
            Select(properties.General_TabItem);

            // The series last: another clears the fit's results, as upstream's element does; undo gives the saved series
            // back.
            TimeSeriesElement series = element.TimeSeriesData;
            var another = listed.First(i => i != series);
            data.SelectedItem = another;
            Settle();
            Assert.AreSame(another, element.TimeSeriesData);
            element.UndoManager.Undo();
            Settle();
            Assert.AreSame(series, element.TimeSeriesData);
            Assert.AreSame(series, data.SelectedItem);

            // The panel lists a series added to the project, and still does once it has unloaded (a theme change
            // re-templates the docked panel, for one) and loaded again: upstream's panel subscribes again when it loads.
            var added = AddSeries(project, "Added series");
            Assert.IsTrue(properties.TimeSeriesElements.Contains(added));
            CollectionAssert.Contains(data.Items.Cast<object>().ToList(), added);
            Reload(_windows.Last());
            var later = AddSeries(project, "Later series");
            Assert.IsTrue(properties.TimeSeriesElements.Contains(later), "the panel follows the project's series after a reload");
            CollectionAssert.Contains(data.Items.Cast<object>().ToList(), later);
            properties.Element = null;
            view.Element = null;
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var synthetic = OpenProject(SyntheticSeries);
            var regression = OpenProject(Regression);
            // The classic and regression models, and three synthetic ones: a manual training window with a cubic trend,
            // an ARMA model and the log-transformed one. The example tour shows every model once.
            var elements = Elements(Classic.Project).Concat(Elements(regression)).OfType<TimeSeriesAnalysis>()
                .Concat(new[] { "Intercept + Cubic Trend", "ARMA(2,2)", LogTransform }.Select(n => Element<TimeSeriesAnalysis>(synthetic, n))).ToList();
            Assert.AreEqual(8, elements.Count);
            foreach (var element in elements)
            {
                string name = element.Name;
                var changes = new List<string>();
                Watch(changes, element, element.ARIMAX, element.BayesianAnalysis, element.TimeSeriesData);
                Watch(changes, element.ARIMAX.Parameters.Cast<object>().ToArray());
                Watch(changes, element.Covariates.Cast<object>().ToArray());

                // Two documents of the element, the first still holding it when the second opens, as upstream's
                // MainProjectNode leaves a closed one: the second takes the plots.
                var views = new List<TimeSeriesAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new TimeSeriesAnalysisControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.ShowFilterToggleButton.IsChecked = true;
                    Select(TabularResults(view));
                    if (document == 0)
                    {
                        foreach (TabItem tab in ((TabControl)view.ResultsTabItem.Parent).Items.Cast<TabItem>())
                            Select(tab);
                        foreach (TabItem tab in ((TabControl)view.ResidualsPlotTab.Parent).Items.Cast<TabItem>())
                        {
                            Select(view.ResidualsTabItem);
                            Select(tab);
                        }
                        Select(view.ResultsTabItem);
                    }
                    Select(view.PlotTabItem);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                }
                var second = views[1];
                Assert.AreSame(element.TimeSeriesPlot, second.TimeSeriesPlotHost.Content, name);
                Assert.AreSame(element.ResidualPlot, second.ResidualPlotHost.Content, name);
                Assert.AreSame(element.ResidualHistogramPlot, second.ResidualHistogramPlotHost.Content, name);
                Assert.AreSame(element.ResidualQQPlot, second.ResidualQQPlotHost.Content, name);
                Assert.AreSame(element.ResidualACFPlot, second.ResidualACFPlotHost.Content, name);
                Assert.AreSame(element.ResidualPACFPlot, second.ResidualPACFPlotHost.Content, name);
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
                    var properties = new TimeSeriesAnalysisPropertiesControl { Element = element };
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

        /// <summary>The walkthroughs' figures of an earlier fit than the example's (see the figures' test).</summary>
        private static readonly string[] StaleFigures =
        {
            "classic-time-series-examples-airline-residual-acf.plotspec.json.gz",
            "classic-time-series-examples-airline-trace.plotspec.json.gz",
            "classic-time-series-examples-airline-validation.plotspec.json.gz",
        };

        /// <summary>The plot of a native export or figure, its tab selected so the view lays it out.</summary>
        private static Wpf.Plot Plot(TimeSeriesAnalysisControl view, string plotId, string file)
        {
            TimeSeriesAnalysis element = view.Element;
            switch (plotId)
            {
                case "time_series_analysis.series":
                    Select(view.ResultsTabItem);
                    Select(view.PlotTabItem);
                    return element.TimeSeriesPlot;
                case "time_series_analysis.residuals":
                    return Residual(view, view.ResidualsPlotTab, element.ResidualPlot);
                case "time_series_analysis.residual_histogram":
                    return Residual(view, view.ResidualHistogramTab, element.ResidualHistogramPlot);
                case "time_series_analysis.residual_qq":
                    return Residual(view, view.ResidualQQTab, element.ResidualQQPlot);
                case "time_series_analysis.residual_acf":
                    return Residual(view, view.ResidualACFTab, element.ResidualACFPlot);
                case "time_series_analysis.residual_pacf":
                    return Residual(view, view.ResidualPACFTab, element.ResidualPACFPlot);
                case "shared_diagnostics.acf":
                    Select(view.AutocorrelationTabItem);
                    return view.AutocorrelationControl.Plot;
                case "shared_diagnostics.trace":
                    Select(view.MarkovChainTabItem);
                    return view.MarkovChainTraceControl.Plot;
                default:
                    throw new AssertFailedException(file + ": a plot this test doesn't know, " + plotId);
            }
        }

        private static Wpf.Plot Residual(TimeSeriesAnalysisControl view, TabItem tab, Wpf.Plot plot)
        {
            Select(view.ResidualsTabItem);
            Select(tab);
            return plot;
        }

        // A parameter's significance code, as upstream's view computes it: the posterior's mass above zero.
        private static string Code(double[] draws)
        {
            double p = new KernelDensity(draws).CCDF(0);
            return p > 0.999 || p < 0.001 ? "***" : p > 0.99 || p < 0.01 ? "**" : p > 0.95 || p < 0.05 ? "*" : p > 0.9 || p < 0.1 ? "." : " ";
        }

        // The series plot's training interval title.
        private static string Interval(TimeSeriesAnalysis element) => element.TimeSeriesPlot.Series.SingleOrDefault(s => s.Name == "TrainingCredibleIntervals")?.Title;

        // The last upper interval value, or null while the element is reprocessing its results.
        private static double? Upper(TimeSeriesAnalysis element) =>
            element.AnalysisResults?.ConfidenceIntervals is double[,] intervals && intervals.GetLength(0) > 0 ? intervals[intervals.GetLength(0) - 1, 2] : null;

        /// <summary>The Time Series Results tab's Tabular Results tab.</summary>
        internal static TabItem TabularResults(TimeSeriesAnalysisControl view) =>
            ((TabControl)view.PlotTabItem.Parent).Items.Cast<TabItem>().Single(t => (string)t.Header == "Tabular Results");

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
