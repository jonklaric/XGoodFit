using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Distributions;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.PointProcessMixtureTesting;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Mixture Analysis view and its properties panel (GUI/UnivariateAnalysis/Mixture) on the
    /// mixture example's fits (4-mixture-analysis), as its walkthrough reads them.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports of the two-Normal fit (its frequency plot, and the
    /// diagnostics' kernel density, trace and autocorrelation of the first weight) and with the walkthrough's figures
    /// (the three fits' frequency curves, the three-Normal fit's trace). The tables show upstream's own results in US
    /// formats (plan D8). Editing the properties (the component grid among them) writes to the element as upstream's
    /// panel does, and the element's undo restores it; showing the views changes nothing in the element. The tests that
    /// only show the example share one copy of it.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class MixtureAnalysisViewTests
    {
        private static readonly SharedExample Shared = new SharedExample(MixtureExample);
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
        /// The fits RMC-BestFit exported or drew for the walkthrough: the view shows what the desktop did, point for point.
        /// </summary>
        [STATestMethod]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures()
        {
            UseAppResources(ThemeColor.Light);
            var project = Shared.Project;
            var exports = NativeExports(MixtureExample).ToList();
            // The walkthrough's figures of the view: the frequency curves and the three-Normal fit's trace.
            var figures = Figures(MixtureExample).Where(f => f.Figure.GetProperty("plotId").GetString() is "mixture.frequency" or "shared_diagnostics.trace").ToList();
            Assert.AreEqual(4, exports.Count);
            Assert.AreEqual(4, figures.Count);
            var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
            int compared = 0;
            foreach (string name in names)
            {
                var element = Element<MixtureAnalysis>(project, name);
                var view = new MixtureAnalysisControl { Element = element };
                Window window = Show(view, 1300, 850);
                try
                {
                    Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotHost.Content);
                    Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot(), "the frequency plot's tab is the one selected");
                    foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                    {
                        Wpf.Plot plot = element.FrequencyPlot;
                        if (figure.GetProperty("plotId").GetString() == "shared_diagnostics.trace")
                        {
                            Select(view.MarkovChainTabItem);
                            plot = view.MarkovChainTraceControl.Plot;
                            AssertFigure(plot, figure, file);
                        }
                        else if (!plot.Axes.Any(a => a.Key == "Yaxis" && a is Wpf.LogarithmicAxis))
                        {
                            // The saved plots' value axes are linear, so the zero-inflated fit's view also shows the
                            // data's ten zeros, which the walkthrough's factory-default (logarithmic) figure can't.
                            AssertFigureOnLinearValueAxis(plot, figure, file);
                        }
                        else
                            AssertFigure(plot, figure, file);
                        Select(view.DistributionResultsTabItem);
                        compared++;
                    }
                    foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                    {
                        Wpf.Plot plot;
                        switch (capture.GetProperty("plotId").GetString())
                        {
                            case "mixture.frequency":
                                Select(view.FrequencyResultsTabItem);
                                plot = element.FrequencyPlot;
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
        [DataRow(TwoNormals, 113.201, 110.589, 116.292, 838.221, 1.00030, 9285)]
        [DataRow(ThreeNormals, 168.069, 163.59, 173.682, 950.565, 1.00065, 5702)]
        [DataRow(ZeroInflated, 119.163, 115.526, 123.434, 861.705, 1.00053, 9283)]
        public void ResultsTables_ShowTheFrequencyCurveAndStatistics_InUsFormats(string name, double point, double lower, double upper, double dic, double rhat, double ess)
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<MixtureAnalysis>(Shared.Project, name);
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes decimal
                // commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new MixtureAnalysisControl { Element = element });
                Select(TabularResults(view));

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
                // The walkthrough's 1% AEP point estimate and 90% bounds, its DIC, and its MCMC report's largest R-hat and
                // smallest effective sample size.
                int one = Enumerable.Range(0, element.ProbabilityOrdinates.Count).Single(i => Math.Abs(element.ProbabilityOrdinates[i] - 0.01) < 1E-12);
                Assert.AreEqual(point, results.ModeCurve[one], 5E-4);
                Assert.AreEqual(lower, results.ConfidenceIntervals[one, 0], 5E-3);
                Assert.AreEqual(upper, results.ConfidenceIntervals[one, 1], 5E-3);
                Assert.AreEqual(dic, results.DIC, 5E-4);
                var sampled = element.BayesianAnalysis.Results.ParameterResults;
                Assert.AreEqual(rhat, sampled.Max(r => r.SummaryStatistics.Rhat), 5E-6);
                Assert.AreEqual(ess, sampled.Min(r => r.SummaryStatistics.ESS), 1);
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual("Posterior Mean", table.Columns[4].Header);

                // The summary statistics: a zero-inflated fit's zero weight first, the parameters, the mixture's
                // moments and the fit's criteria, with rules above Minimum and AIC.
                var statistics = view.SummaryStatisticsTable;
                var model = element.MixtureDistribution;
                var mixture = model.Mixture;
                double[] moments = mixture.CentralMoments(1E-6);
                var rows = new List<(string, double)>();
                if (model.IsZeroInflated) rows.Add(("Weight 0", mixture.ZeroWeight));
                rows.AddRange(model.Parameters.Select(p => (p.DisplayName, p.Value)));
                rows.AddRange(new[]
                {
                    ("Minimum", mixture.Minimum < -1E12 ? double.NegativeInfinity : mixture.Minimum),
                    ("Maximum", mixture.Maximum > 1E12 ? double.PositiveInfinity : mixture.Maximum),
                    ("Mean", moments[0]), ("Std Dev", moments[1]), ("Skewness", moments[2]), ("Kurtosis", moments[3]),
                    ("AIC", results.AIC), ("BIC", results.BIC), ("DIC", results.DIC),
                    ("WAIC", element.BayesianAnalysis.WAIC), ("LOO-CV", element.BayesianAnalysis.LOOIC), ("RMSE", results.RMSE),
                });
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    Assert.AreEqual(rows[i].Item1 is "Minimum" or "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), statistics.GetRow(i).BorderThickness, rows[i].Item1);
                }
                // The walkthrough's zero mass, fixed at the observed proportion.
                if (model.IsZeroInflated)
                    Assert.AreEqual("0.1000", CellText(statistics, 0, 1));
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
            var project = Shared.Project;
            var element = Element<MixtureAnalysis>(project, TwoNormals);
            var view = Host(new MixtureAnalysisControl { Element = element });
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

            // The label shows the alternatives: the example's other mixtures. One adds its curves.
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsTrue(view.AlternativeSelector.IsVisible);
            var others = Elements(project).OfType<IAnalysisElement>().Where(e => e != element && e.ParentCollection == element.ParentCollection).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var three = Element<MixtureAnalysis>(project, ThreeNormals);
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == three);
            var frequency = element.FrequencyPlot;
            int index = frequency.Series.Count;
            item.IsChecked = true;
            Settle();
            var point = (Wpf.LineSeries)frequency.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual(ThreeNormals + " - Posterior Mean", point.Title);
            Assert.AreEqual(Avalonia.Media.Color.Parse(GeneralMethods.RandomColorsLongList[4]), point.Color);
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(three.ProbabilityOrdinates[i], three.AnalysisResults.ModeCurve[i]), points[i]);
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(frequency.Series.Any(s => s.Name.Contains('_')));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");

            // A diagnostic plot's toolbar asks for its plot's properties through the view.
            Select(TabularResults(view));
            Select(view.KernelDensityTabItem);
            Assert.AreSame(view.KernelDensityControl.Plot, view.GetCurrentPlot());
            Click(window, HitPoint(window, view.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.BayesianPlots.KernelDensityPlot, called.Last());
            Select(view.DistributionResultsTabItem);
            Select(view.FrequencyResultsTabItem);
            view.Element = null;
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(MixtureExample);
            var element = Element<MixtureAnalysis>(project, TwoNormals);
            var view = Host(new MixtureAnalysisControl { Element = element });
            var properties = Host(new MixtureAnalysisPropertiesControl { Element = element }, 420, 1100);

            // General: the element's name and dates, its input data, its components and its priors.
            Assert.AreEqual(TwoNormals, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            Assert.AreSame(element.InputData, inputData.SelectedItem);
            var listed = inputData.Items.Cast<InputData>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<InputData>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            var grid = properties.DistributionDataGrid;
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Normal, UnivariateDistributionType.Normal }, element.Distributions.ToArray());
            Assert.AreEqual(2, grid.Items.Count);
            for (int row = 0; row < 2; row++)
            {
                var cell = Combo(grid, row);
                Assert.AreEqual(UnivariateDistributionType.Normal, cell.SelectedValue, "row " + row);
                Assert.AreEqual("Normal", ((UnivariateDistributionItem)cell.SelectedItem).DisplayName);
                CollectionAssert.AreEqual(properties.DistributionList.ToList(), cell.Items.Cast<object>().ToList());
            }
            Assert.IsFalse(grid.ShowSortContextMenu);
            Assert.AreEqual(element.MixtureDistribution.IsZeroInflated, properties.IsZeroInflated.IsSelected);
            Assert.AreSame(element.MixtureDistribution, properties.ParameterPriorsControl.Model);
            Assert.AreSame(element.MixtureDistribution, properties.QuantilePriorsControl.Model);
            Assert.AreEqual(element.MixtureDistribution.UseJeffreysRuleForScale, properties.UseJeffreysRule.IsSelected);

            properties.Description.Text = "Two latent populations";
            Assert.AreEqual("Two latent populations", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Two latent populations", element.Description);

            // A new credible interval: the table's headers at once, and the curves once the element has reprocessed
            // them, with the wait cursor until then.
            Select(TabularResults(view));
            var table = view.FrequencyCurveTable;
            double saved = element.AnalysisResults.ConfidenceIntervals[0, 1];
            element.BayesianAnalysis.CredibleIntervalWidth = 0.99;
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.99);
            // The view redraws on the reprocessed results' notification, which it posts to the UI thread; the shared
            // diagnostic controls also clear the wait cursor, so the test waits for the view's own update too.
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) is double upper && upper != saved && Mouse.OverrideCursor == null
                && Interval(element) == "99% Credible Intervals" && table.Items.Cast<FrequencyCurvePoint>().First().Upper == upper, "the reprocessed curves");
            element.UndoManager.Undo();
            UnivariateAnalysisViewTests.PumpUntil(() => Upper(element) == saved && Mouse.OverrideCursor == null
                && Interval(element) == "90% Credible Intervals" && table.Items.Cast<FrequencyCurvePoint>().First().Upper == saved, "the curves back");
            UnivariateAnalysisViewTests.AssertIntervalHeaders(table, 0.9);

            // The components: a cell's combo box changes its row's distribution, the toolbar adds a row (up to three)
            // and deletes the selected ones (keeping one), and each undoes.
            int undos = element.UndoManager.UndoStack.Count;
            var second = Combo(grid, 1);
            second.SelectedItem = properties.DistributionList.Single(i => i.Value == UnivariateDistributionType.Gumbel);
            Settle();
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Normal, UnivariateDistributionType.Gumbel }, element.Distributions.ToArray());
            Assert.AreEqual(2, element.MixtureDistribution.Mixture.Distributions.Length);
            Assert.IsInstanceOfType(element.MixtureDistribution.Mixture.Distributions[1], typeof(Gumbel));
            Assert.AreSame(element.MixtureDistribution, properties.ParameterPriorsControl.Model, "the priors edit the new model");
            grid.AddRows(1);
            Settle();
            Assert.AreEqual(3, grid.Items.Count);
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Normal, UnivariateDistributionType.Gumbel, UnivariateDistributionType.Normal }, element.Distributions.ToArray());
            grid.AddRows(1);
            Settle();
            Assert.AreEqual(3, element.Distributions.Count, "three components at most");
            SelectRow(grid, 0);
            grid.DeleteRows();
            Settle();
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Gumbel, UnivariateDistributionType.Normal }, element.Distributions.ToArray());
            Assert.AreEqual("Gumbel (EVI)", ((UnivariateDistributionItem)Combo(grid, 0).SelectedItem).DisplayName);
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Normal, UnivariateDistributionType.Normal }, element.Distributions.ToArray());
            Assert.AreEqual(2, grid.Items.Count);
            Assert.AreEqual(UnivariateDistributionType.Normal, Combo(grid, 1).SelectedValue, "the restored component shows");
            Assert.AreSame(element.MixtureDistribution, properties.ParameterPriorsControl.Model, "the priors edit the restored model");
            Assert.AreSame(element.MixtureDistribution, properties.QuantilePriorsControl.Model);

            // Zero inflation and Jeffreys' rule.
            bool zero = element.MixtureDistribution.IsZeroInflated;
            Tick(properties.IsZeroInflated, !zero);
            Assert.AreEqual(!zero, element.MixtureDistribution.IsZeroInflated);
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(zero, element.MixtureDistribution.IsZeroInflated);
            Assert.AreEqual(zero, properties.IsZeroInflated.IsSelected);
            bool jeffreys = element.MixtureDistribution.UseJeffreysRuleForScale;
            Tick(properties.UseJeffreysRule, !jeffreys);
            Assert.AreEqual(!jeffreys, element.MixtureDistribution.UseJeffreysRuleForScale);
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(jeffreys, properties.UseJeffreysRule.IsSelected);

            // The sampler and output settings.
            Select(properties.Options_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOptionsControl.Analysis);
            Select(properties.Output_TabItem);
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOutputControl.Analysis);
            Assert.AreSame(element.ProbabilityOrdinates, properties.ProbabilityOrdinatesControl.ProbabilityOrdinates);
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
            foreach (var element in Elements(project).OfType<MixtureAnalysis>().ToList())
            {
                string name = element.Name;
                var changes = new List<string>();
                Watch(changes, element, element.MixtureDistribution, element.BayesianAnalysis, element.InputData);
                Watch(changes, element.MixtureDistribution.Parameters.Cast<object>().ToArray());
                int collectionChanges = 0;
                element.Distributions.CollectionChanged += (_, _) => collectionChanges++;

                // Two documents of the element, the first still holding it when the second opens.
                var views = new List<MixtureAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new MixtureAnalysisControl { Element = element };
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
                    var properties = new MixtureAnalysisPropertiesControl { Element = element };
                    Window window = Show(properties, 420, 1100);
                    Select(properties.Options_TabItem);
                    Select(properties.Output_TabItem);
                    Select(properties.General_TabItem);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                }

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.AreEqual(0, collectionChanges, $"{name}: showing the views changed the components");
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                foreach (var view in views) view.Element = null;
            }
        }

        // The frequency plot's interval title.
        private static string Interval(MixtureAnalysis element) => element.FrequencyPlot.Series.SingleOrDefault(s => s.Name == "CredibleIntervals")?.Title;

        // The first upper interval value, or null while the element is reprocessing its results.
        private static double? Upper(MixtureAnalysis element) =>
            element.AnalysisResults?.ConfidenceIntervals is double[,] intervals && intervals.GetLength(0) > 0 ? intervals[0, 1] : null;

        /// <summary>The Distribution Results tab's Tabular Results tab.</summary>
        internal static TabItem TabularResults(MixtureAnalysisControl view) =>
            ((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Single(t => (string)t.Header == "Tabular Results");

        // A component row's distribution combo box.
        private static ComboBox Combo(CopyPasteDataGrid grid, int row) =>
            grid.GetCell(row, 0).GetVisualDescendants().OfType<ComboBox>().Single();

        // Selects a component row's cell, as a click on it does.
        private static void SelectRow(CopyPasteDataGrid grid, int row) =>
            grid.SelectedCells = new List<DataGridCellInfo> { new DataGridCellInfo(grid.Items[row], grid.Columns[0], row, 0) };

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
