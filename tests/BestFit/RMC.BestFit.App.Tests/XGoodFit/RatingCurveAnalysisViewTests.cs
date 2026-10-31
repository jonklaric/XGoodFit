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
    /// XGoodFit addition: the Rating Curve Analysis view and its properties panel (GUI/RatingCurveAnalysis) on the
    /// rating curve examples (6-rating-curve-analysis), as their walkthroughs read them.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports (the one-control synthetic rating's and New Madrid's
    /// rating curves, residuals and zero-flow stage traces) and with the walkthroughs' figures (the three synthetic
    /// ratings, the three-control rating's residuals and Q-Q plot, and New Madrid's rating and residual diagnostics). The
    /// tables show upstream's own results in US formats (plan D8). Editing the properties writes to the element as
    /// upstream's bindings do, and the element's undo restores it; showing the views changes nothing in the element.
    /// The tests that only show the examples share one copy of each.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class RatingCurveAnalysisViewTests
    {
        private static readonly SharedExample Synthetic = new SharedExample(SyntheticRatings);
        private static readonly SharedExample NewMadridExample = new SharedExample(Mississippi);
        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the shared examples.</summary>
        [ClassCleanup(ClassCleanupBehavior.EndOfClass)]
        public static void CloseExamples()
        {
            Synthetic.Close();
            NewMadridExample.Close();
        }

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
        /// The ratings RMC-BestFit exported or drew for the walkthroughs: the views show what the desktop did, point for
        /// point.
        /// </summary>
        [STATestMethod]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures()
        {
            UseAppResources(ThemeColor.Light);
            int compared = 0, expected = 0;
            foreach (var (example, shared) in new[] { (SyntheticRatings, Synthetic), (Mississippi, NewMadridExample) })
            {
                var exports = NativeExports(example).ToList();
                var figures = Figures(example).ToList();
                expected += exports.Count + figures.Count;
                var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                    .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
                foreach (string name in names)
                {
                    var element = Element<RatingCurveAnalysis>(shared.Project, name);
                    var view = new RatingCurveAnalysisControl { Element = element };
                    Window window = Show(view, 1300, 850);
                    try
                    {
                        Assert.AreSame(element.RatingCurvePlot, view.RatingCurvePlotHost.Content);
                        Assert.AreSame(element.RatingCurvePlot, view.GetCurrentPlot(), "the rating curve's tab is the one selected");
                        foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                        {
                            AssertNativeExport(Plot(view, capture.GetProperty("plotId").GetString(), file), capture, file);
                            compared++;
                        }
                        foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                        {
                            AssertWalkthroughFigure(Plot(view, figure.GetProperty("plotId").GetString(), file), figure, file);
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
            // RMC-BestFit's six native exports and the walkthroughs' nine figures.
            Assert.AreEqual(15, expected);
            Assert.AreEqual(expected, compared);
        }

        [STATestMethod]
        public void ResultsTables_ShowTheRatingCurveAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<RatingCurveAnalysis>(NewMadridExample.Project, NewMadrid);
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes decimal
                // commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new RatingCurveAnalysisControl { Element = element });
                Select(TabularResults(view));

                // One row per stage bin: the stage, then the discharges in the user's value format.
                var results = element.AnalysisResults;
                var table = view.RatingCurveTable;
                string value = "{0:" + UserSettings.ValueStringFormat + "}";
                Assert.AreEqual(element.StageBins, table.Items.Count);
                for (int i = 0; i < element.StageBins; i++)
                {
                    string[] expected =
                    {
                        string.Format(Us, value, results.ConfidenceIntervals[i, 0]),
                        string.Format(Us, value, results.ConfidenceIntervals[i, 2]), string.Format(Us, value, results.ConfidenceIntervals[i, 1]),
                        string.Format(Us, value, results.MeanCurve[i]), string.Format(Us, value, results.ModeCurve[i]),
                    };
                    for (int column = 0; column < expected.Length; column++)
                        Assert.AreEqual(expected[column], CellText(table, i, column), $"row {i}, column {column}");
                }
                // The walkthrough's saved grid, from -10.32 to 45.96 ft, and its best fits and 90% limits (cfs).
                Assert.AreEqual(-10.32, results.ConfidenceIntervals[0, 0], 1E-9);
                Assert.AreEqual(45.96, results.ConfidenceIntervals[element.StageBins - 1, 0], 1E-9);
                foreach (var (stage, best, lower, upper) in new[]
                {
                    (-10.32, 98812.152, 89163.094, 107222.979),
                    (18.104, 549275.844, 503670.856, 600438.657),
                    (45.96, 1662011.799, 1513008.617, 1812674.396),
                })
                {
                    int i = Enumerable.Range(0, element.StageBins).Single(r => Math.Abs(results.ConfidenceIntervals[r, 0] - stage) < 5E-4);
                    Assert.AreEqual(best, results.ModeCurve[i], 5E-4, $"the best fit at {stage} ft");
                    Assert.AreEqual(lower, results.ConfidenceIntervals[i, 1], 5E-4, $"the lower limit at {stage} ft");
                    Assert.AreEqual(upper, results.ConfidenceIntervals[i, 2], 5E-4, $"the upper limit at {stage} ft");
                }
                // The headers are upstream's: the interval's percentages in the code's (machine) culture, and the
                // point estimator.
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual("Stage", table.Columns[0].Header);
                Assert.AreEqual("Posterior Mode", table.Columns[4].Header);

                // The summary statistics: the rating's parameters and the fit's criteria, with a rule above AIC.
                var statistics = view.SummaryStatisticsTable;
                var model = element.RatingCurve;
                var rows = model.Parameters.Select(p => (p.DisplayName, p.Value))
                    .Concat(new[]
                    {
                        ("AIC", results.AIC), ("BIC", results.BIC), ("DIC", results.DIC),
                        ("WAIC", element.BayesianAnalysis.WAIC), ("LOO-CV", element.BayesianAnalysis.LOOIC), ("RMSE", results.RMSE),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mode", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    DataGridRow row = statistics.GetRow(i);
                    Assert.AreEqual(rows[i].Item1 == "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), row.BorderThickness, rows[i].Item1);
                }
                // The walkthrough's parameters (the log10 coefficient is stored in log space), DIC and RMSE, and its MCMC
                // report's largest R-hat and smallest effective sample size.
                CollectionAssert.AreEqual(new[] { "Zero-Flow Stage (h₁)", "Coefficient (α₁)", "Exponent (β₁)", "Scale (σ)" },
                    model.Parameters.Select(p => p.DisplayName).ToArray(), "the one-control rating's parameters");
                Assert.AreEqual(-52.471, model.Parameters[0].Value, 5E-4);
                Assert.AreEqual(-0.413, model.Parameters[1].Value, 5E-4);
                Assert.AreEqual(3.328, model.Parameters[2].Value, 5E-4);
                Assert.AreEqual(0.023, model.Parameters[3].Value, 5E-4);
                Assert.AreEqual(2233.321, results.DIC, 5E-4);
                Assert.AreEqual(30328.582, results.RMSE, 5E-4);
                var sampled = element.BayesianAnalysis.Results.ParameterResults;
                Assert.AreEqual(1.00019, sampled.Max(r => r.SummaryStatistics.Rhat), 5E-6);
                Assert.AreEqual(8266, sampled.Min(r => r.SummaryStatistics.ESS), 1);
                view.Element = null;
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
            var project = Synthetic.Project;
            var element = Element<RatingCurveAnalysis>(project, ThreeSegments);
            var view = Host(new RatingCurveAnalysisControl { Element = element });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => called.Add(plot);

            Click(window, Centre(window, element.RatingCurvePlot));
            Assert.AreEqual((true, false, element.RatingCurvePlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);
            Point toolbar = HitPoint(window, view.PlotToolbar);
            window.MouseMove(toolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.RatingCurvePlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            // The label beside the toggle button shows and hides the alternatives, as the button does.
            Assert.IsFalse(view.AlternativeSelector.IsVisible);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsTrue(view.ShowFilterToggleButton.IsChecked == true);
            Assert.IsTrue(view.AlternativeSelector.IsVisible);

            // The project's other ratings. One adds its curves, named by the series count, in the first unused colour of
            // upstream's list from the fifth on; its points are discharges at its stages.
            var others = Elements(project).OfType<RatingCurveAnalysis>().Where(e => e != element).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var one = Element<RatingCurveAnalysis>(project, OneSegment);
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == one);
            var rating = element.RatingCurvePlot;
            int index = rating.Series.Count;
            item.IsChecked = true;
            Settle();
            var color = Avalonia.Media.Color.Parse(GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)rating.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var predictive = (Wpf.LineSeries)rating.Series.Single(s => s.Name == "PosteriorPredictive_" + index);
            var point = (Wpf.LineSeries)rating.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual("1 Segment Rating Curve - 90% Credible Intervals", interval.Title);
            Assert.AreEqual("1 Segment Rating Curve - Posterior Predictive", predictive.Title);
            Assert.AreEqual("1 Segment Rating Curve - Posterior Mean", point.Title);
            Assert.AreEqual(color, point.Color);
            Assert.AreEqual(color, predictive.Color);
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            Assert.AreEqual(one.StageBins, points.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(one.AnalysisResults.ModeCurve[i], one.AnalysisResults.ConfidenceIntervals[i, 0]), points[i]);
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(rating.Series.Any(s => s.Name.Contains('_')));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");

            // A residual tab's plot is the current plot once the results show their table. As upstream, the current
            // plot is the first selected tab of the inner tab controls, then the outer ones: a residual tab is always
            // selected in its own, so the diagnostic tabs' plots never become current
            // (IssuesToRaise/RMC-BestFit-current-plot-ignores-diagnostic-tabs.md).
            Assert.AreSame(element.RatingCurvePlot, view.GetCurrentPlot());
            Select(TabularResults(view));
            Select(view.ResidualsTabItem);
            Select(view.ResidualQQTab);
            Assert.AreSame(element.ResidualQQPlot, view.GetCurrentPlot());
            Assert.AreSame(view.QQPlotToolbar, view.GetCurrentPlotToolbar());
            Select(view.HistogramTabItem);
            Assert.AreSame(element.ResidualQQPlot, view.GetCurrentPlot(), "upstream's order: the residual tab first");
            // A diagnostic plot's toolbar asks for its plot's properties through the view all the same.
            Click(window, HitPoint(window, view.HistogramControl.PlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.BayesianPlots.HistogramPlot, called.Last());
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
            var project = OpenProject(SyntheticRatings);
            var element = Element<RatingCurveAnalysis>(project, TwoSegments);
            var view = Host(new RatingCurveAnalysisControl { Element = element });
            var properties = Host(new RatingCurveAnalysisPropertiesControl { Element = element }, 420, 1100);
            // Upstream's undo restores the model from a snapshot, a new RatingCurve: the panel and the tests read the element's.

            // General: the element's name and dates, its stage and discharge series, its controls and its priors.
            Assert.AreEqual(TwoSegments, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var stage = (ComboBox)((Border)properties.StageDataComboBox.InnerContent).Child;
            var discharge = (ComboBox)((Border)properties.DischargeDataComboBox.InnerContent).Child;
            Assert.AreSame(element.StageData, stage.SelectedItem);
            Assert.AreSame(element.DischargeData, discharge.SelectedItem);
            var listed = stage.Items.Cast<TimeSeriesElement>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<TimeSeriesElement>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            CollectionAssert.AreEqual(listed, discharge.Items.Cast<TimeSeriesElement>().ToList());
            Assert.AreEqual(new Thickness(0), ((Border)properties.StageDataComboBox.InnerContent).BorderThickness, "a valid stage series");
            Assert.IsNull(properties.StageDataComboBox.ToolTip);
            var segments = (GenericControls.NumericUpDown)properties.NumberOfSegments.InnerContent;
            Assert.AreEqual(2, segments.Value);
            Assert.AreEqual(element.RatingCurve.NumberOfSegments, (int)segments.Value);
            Assert.AreSame(element.RatingCurve, properties.ParameterPriorsControl.Model);
            Assert.AreEqual(element.RatingCurve.UseJeffreysRuleForScale, properties.UseJeffreysRule.IsSelected);

            properties.Description.Text = "Two hydraulic controls";
            Assert.AreEqual("Two hydraulic controls", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Two hydraulic controls", element.Description);

            // A new credible interval: the table's headers at once, and the curves once the element has reprocessed
            // them (on a worker thread, as upstream's model layer does), with the wait cursor until then.
            Select(TabularResults(view));
            var table = view.RatingCurveTable;
            double saved = Upper(element).Value;
            Select(properties.Output_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOutputControl.Analysis);
            element.BayesianAnalysis.CredibleIntervalWidth = 0.99;
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.99);
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) is double upper && upper != saved && Mouse.OverrideCursor == null
                && Interval(element) == "99% Credible Intervals" && table.Items.Cast<FrequencyCurvePoint>().Last().Upper == upper, "the reprocessed curves");
            element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) == saved && Mouse.OverrideCursor == null
                && Interval(element) == "90% Credible Intervals" && table.Items.Cast<FrequencyCurvePoint>().Last().Upper == saved, "the curves back");
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.9);

            // The stage bins are editable once the defaults are off; fewer bins give a shorter table and curves once the
            // element has reprocessed them, and undo gives the saved ones back.
            Assert.IsTrue(element.UseDefaultStageBins);
            Assert.IsFalse(properties.StageBins.IsTextBoxEnabled || properties.MinStage.IsTextBoxEnabled || properties.MaxStage.IsTextBoxEnabled);
            Assert.AreEqual(element.MinStage, properties.MinStage.Number);
            Assert.AreEqual(element.MaxStage, properties.MaxStage.Number);
            Assert.AreEqual(element.StageBins, properties.StageBins.Number);
            int undos = element.UndoManager.UndoStack.Count;
            Tick(properties.UseDefaults, false);
            Assert.IsFalse(element.UseDefaultStageBins);
            Assert.IsTrue(properties.StageBins.IsTextBoxEnabled && properties.MinStage.IsTextBoxEnabled && properties.MaxStage.IsTextBoxEnabled);
            properties.StageBins.Number = 50;
            Assert.AreEqual(50, element.StageBins);
            UnivariateAnalysisViewTests.PumpUntil(() => element.AnalysisResults?.ModeCurve?.Length == 50 && Mouse.OverrideCursor == null
                && table.Items.Count == 50, "the curves on 50 bins");
            Assert.AreEqual(50, ((IEnumerable<OxyPlot.DataPoint>)element.RatingCurvePlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).Count());
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => element.AnalysisResults?.ModeCurve?.Length == 100 && Mouse.OverrideCursor == null
                && table.Items.Count == 100, "the curves back on 100 bins");
            Assert.IsTrue(element.UseDefaultStageBins);
            Assert.IsTrue(properties.UseDefaults.IsSelected);
            Assert.AreEqual(100, properties.StageBins.Number);
            Select(properties.General_TabItem);

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

            // A prior's setting changes the model, which clears the fit; undo restores the model (not its fit, as
            // upstream's).
            bool jeffreys = element.RatingCurve.UseJeffreysRuleForScale;
            Tick(properties.UseJeffreysRule, !jeffreys);
            Assert.AreEqual(!jeffreys, element.RatingCurve.UseJeffreysRuleForScale);
            Assert.IsFalse(element.IsEstimated, "a new prior clears the fit");
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(jeffreys, element.RatingCurve.UseJeffreysRuleForScale);
            Assert.AreEqual(jeffreys, properties.UseJeffreysRule.IsSelected);
            Assert.AreSame(element.RatingCurve, properties.ParameterPriorsControl.Model, "the priors edit the restored model");

            // Another number of controls is a new model: it clears the fit, and undo restores the model (a new object,
            // which the priors then edit).
            segments.Value = 3;
            Settle();
            Assert.AreEqual(3, element.RatingCurve.NumberOfSegments);
            Assert.IsFalse(element.IsEstimated);
            Assert.AreEqual(0, table.Items.Count, "the cleared fit has no curves");
            while (element.UndoManager.CanUndo && element.RatingCurve.NumberOfSegments != 2) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(2, element.RatingCurve.NumberOfSegments);
            Assert.AreEqual(2, segments.Value, "the restored number of controls shows");
            Assert.AreSame(element.RatingCurve, properties.ParameterPriorsControl.Model, "the priors edit the restored model");

            // The stage series last: another clears the fit's results, as upstream's element does, and the rating
            // curve's stage axis follows its unit label; undo gives the saved series back.
            TimeSeriesElement data = element.StageData;
            var another = listed.First(i => i != data && i != element.DischargeData);
            stage.SelectedItem = another;
            Settle();
            Assert.AreSame(another, element.StageData);
            Assert.IsFalse(element.IsEstimated);
            element.UndoManager.Undo();
            Settle();
            Assert.AreSame(data, element.StageData);
            Assert.AreSame(data, stage.SelectedItem);

            // The panel lists a series added to the project; but once it has unloaded (in WPF a theme change re-templates
            // the docked panel; in XGoodFit taking it out of its pane and back in does) and loaded again, it no longer
            // follows the project's series, as upstream's: only a new element subscribes it again, where the Time Series
            // panel subscribes again when it loads.
            var added = AddSeries(project, "Added stage");
            Assert.IsTrue(properties.TimeSeriesElements.Contains(added));
            CollectionAssert.Contains(stage.Items.Cast<object>().ToList(), added);
            Reload(_windows.Last());
            var later = AddSeries(project, "Later stage");
            Assert.IsFalse(properties.TimeSeriesElements.Contains(later), "upstream's panel stops following the project's series once unloaded");
            properties.Element = null;
            view.Element = null;
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            foreach (var shared in new[] { Synthetic, NewMadridExample })
            {
                foreach (var element in Elements(shared.Project).OfType<RatingCurveAnalysis>().ToList())
                {
                    string name = element.Name;
                    var changes = new List<string>();
                    Watch(changes, element, element.RatingCurve, element.BayesianAnalysis, element.StageData, element.DischargeData);
                    Watch(changes, element.RatingCurve.Parameters.Cast<object>().ToArray());

                    // Two documents of the element, the first still holding it when the second opens, as upstream's
                    // MainProjectNode leaves a closed one: the second takes the plots.
                    var views = new List<RatingCurveAnalysisControl>();
                    for (int document = 0; document < 2; document++)
                    {
                        var view = new RatingCurveAnalysisControl { Element = element };
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
                    Assert.AreSame(element.RatingCurvePlot, second.RatingCurvePlotHost.Content, name);
                    Assert.AreSame(element.ResidualPlot, second.ResidualPlotHost.Content, name);
                    Assert.AreSame(element.ResidualHistogramPlot, second.ResidualHistogramPlotHost.Content, name);
                    Assert.AreSame(element.ResidualQQPlot, second.ResidualQQPlotHost.Content, name);
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
                        var properties = new RatingCurveAnalysisPropertiesControl { Element = element };
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
        }

        /// <summary>The plot of a native export or figure, its tab selected so the view lays it out.</summary>
        private static Wpf.Plot Plot(RatingCurveAnalysisControl view, string plotId, string file)
        {
            switch (plotId)
            {
                case "rating.curve":
                    Select(view.ResultsTabItem);
                    Select(view.PlotTabItem);
                    return view.Element.RatingCurvePlot;
                case "rating.residuals":
                    Select(view.ResidualsTabItem);
                    Select(view.ResidualsPlotTab);
                    return view.Element.ResidualPlot;
                case "rating.residual_histogram":
                    Select(view.ResidualsTabItem);
                    Select(view.ResidualHistogramTab);
                    return view.Element.ResidualHistogramPlot;
                case "rating.residual_qq":
                    Select(view.ResidualsTabItem);
                    Select(view.ResidualQQTab);
                    return view.Element.ResidualQQPlot;
                case "shared_diagnostics.trace":
                    Select(view.MarkovChainTabItem);
                    return view.MarkovChainTraceControl.Plot;
                default:
                    throw new AssertFailedException(file + ": a plot this test doesn't know, " + plotId);
            }
        }

        // The rating curve plot's interval title.
        private static string Interval(RatingCurveAnalysis element) => element.RatingCurvePlot.Series.SingleOrDefault(s => s.Name == "CredibleIntervals")?.Title;

        // The upper interval value at the highest stage (the lowest stages are below the zero-flow stage, where both
        // limits are zero), or null while the element is reprocessing its results.
        private static double? Upper(RatingCurveAnalysis element) =>
            element.AnalysisResults?.ConfidenceIntervals is double[,] intervals && intervals.GetLength(0) > 0 ? intervals[intervals.GetLength(0) - 1, 2] : null;

        /// <summary>The Rating Curve Results tab's Tabular Results tab.</summary>
        internal static TabItem TabularResults(RatingCurveAnalysisControl view) =>
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
