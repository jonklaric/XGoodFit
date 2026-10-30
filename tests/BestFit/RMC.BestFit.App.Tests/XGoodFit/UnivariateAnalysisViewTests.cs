using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.Estimation;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using RMC.BestFit.Models.TrendFunctions.Support;
using Numerics.Distributions;
using WpfCompat;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Univariate Distribution Analysis view and its properties panel
    /// (GUI/UnivariateAnalysis/Univariate) on the univariate examples' Bayesian fits.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports of the examples (the frequency plots and the
    /// diagnostics' kernel densities, traces and autocorrelations) and with the walkthroughs' figures, which record
    /// what the desktop drew (frequency plots, and the nonstationary fits' chronology plots). The tables show
    /// upstream's own results, in US formats (plan D8). Editing the properties writes to the element as upstream's
    /// bindings do, and the element's undo restores it; showing the views changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class UnivariateAnalysisViewTests
    {
        private const string Examples = UnivariateAnalysisTesting.Folder + "1-univariate-analysis/";
        internal const string Viglione = Examples + "1-information-expansion/viglione-et-al-2013.bestfit";
        internal const string ArrFlike = Examples + "2-arr-flike/arr-flike-examples.bestfit";
        internal const string BayesianB17C = Examples + "3-bulletin17C-examples/bulletin-17c-bayesian-examples.bestfit";
        internal const string Sinnemahoning = Examples + "4-measurement-errors/sinnemahoning-move3-bayesian.bestfit";
        internal const string BraysBayou = Examples + "5-nonstationary-ffa/nsffa-brays-bayou-texas.bestfit";
        internal const string OcFisher = Examples + "5-nonstationary-ffa/nsffa-oc-fisher-dam.bestfit";
        internal const string Synthetic = Examples + "5-nonstationary-ffa/nsffa-synthetic-data.bestfit";

        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        // Walkthrough figures that don't show the saved results RMC-BestFit shows: the Moose River figure (and the
        // walkthrough's table) has a 1% AEP of 5,231.9 cfs, where the saved AnalysisResults that RMC-BestFit 2.0.1
        // shows, and exports (screenshots/bulletin-17c-bayesian-frequency), have 4,806.6.
        private static readonly HashSet<string> InconsistentFigures = new HashSet<string> { "bulletin-17c-bayesian-examples-moose.plotspec.json.gz" };

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the windows and projects a test opened, and removes the application's resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Close();
            _windows.Clear();
            foreach (var project in _projects) Close(project);
            _projects.Clear();
            // The closed projects' views and plots go before the next test opens another (ReleaseAppResources collects
            // them), and the lists let go of them: MSTest keeps the test class's instance.
            ReleaseAppResources();
        }

        /// <summary>
        /// Each of an example's Bayesian fits that RMC-BestFit exported or drew for its walkthrough: the view shows
        /// what the desktop did, point for point.
        /// </summary>
        [STATestMethod]
        [DataRow(Viglione)]
        [DataRow(ArrFlike)]
        [DataRow(BayesianB17C)]
        [DataRow(Sinnemahoning)]
        [DataRow(BraysBayou)]
        [DataRow(OcFisher)]
        [DataRow(Synthetic)]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures(string example)
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(example);
            var exports = NativeExports(example).ToList();
            // The walkthroughs' own plots of the view: the frequency plot, and a nonstationary fit's chronology.
            var figures = Figures(example).Where(f => f.Figure.GetProperty("plotId").GetString() is "univariate.frequency" or "univariate.chronology"
                && !InconsistentFigures.Contains(f.Name)).ToList();
            Assert.IsTrue(exports.Count > 0, example);
            var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
            int compared = 0;
            foreach (string name in names)
            {
                var element = Element<UnivariateAnalysis>(project, name);
                var view = new UnivariateAnalysisControl { Element = element };
                Window window = Show(view, 1300, 850);
                try
                {
                    Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotHost.Content);
                    Assert.AreSame(element.ChronologyPlot, view.ChronologyPlotHost.Content);
                    Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot(), "the frequency plot's tab is the one selected");
                    Assert.AreEqual(element.UnivariateDistribution.IsNonstationary, view.ChronologyResultsTabItem.IsVisible, name + ": the chronology tab");
                    foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                    {
                        bool chronology = figure.GetProperty("plotId").GetString() == "univariate.chronology";
                        if (chronology) Select(view.ChronologyResultsTabItem);
                        AssertFigure(chronology ? element.ChronologyPlot : element.FrequencyPlot, figure, file);
                        compared++;
                    }
                    foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                    {
                        Wpf.Plot plot;
                        switch (capture.GetProperty("plotId").GetString())
                        {
                            case "univariate.frequency":
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
                    CloseDocument(window);
                }
            }
            Assert.AreEqual(exports.Count + figures.Count, compared);
        }

        [STATestMethod]
        public void ResultsTables_ShowTheFrequencyCurveAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(ArrFlike), "Example #3");
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes
                // decimal commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new UnivariateAnalysisControl { Element = element });
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
                // The headers are upstream's: the interval's percentages in the code's (machine) culture, and the
                // point estimator.
                AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual(BayesianAnalysis.PointEstimateType.PosteriorMean, element.BayesianAnalysis.PointEstimator);
                Assert.AreEqual("Posterior Mean", table.Columns[4].Header);

                // The summary statistics: the parameters, the distribution's moments and the fit's criteria, with
                // rules above Minimum and AIC.
                var statistics = view.SummaryStatisticsTable;
                var distribution = element.UnivariateDistribution;
                var rows = distribution.Parameters.Select(p => (p.DisplayName, p.Value))
                    .Concat(new[]
                    {
                        ("Minimum", distribution.Distribution.Minimum < -1E12 ? double.NegativeInfinity : distribution.Distribution.Minimum),
                        ("Maximum", distribution.Distribution.Maximum > 1E12 ? double.PositiveInfinity : distribution.Distribution.Maximum),
                        ("Mean", distribution.Distribution.Mean), ("Std Dev", distribution.Distribution.StandardDeviation),
                        ("Skewness", distribution.Distribution.Skewness), ("Kurtosis", distribution.Distribution.Kurtosis),
                        ("AIC", results.AIC), ("BIC", results.BIC), ("DIC", results.DIC),
                        ("WAIC", element.BayesianAnalysis.WAIC), ("LOO-CV", element.BayesianAnalysis.LOOIC), ("RMSE", results.RMSE),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    DataGridRow row = statistics.GetRow(i);
                    Assert.AreEqual(rows[i].Item1 is "Minimum" or "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), row.BorderThickness, rows[i].Item1);
                }
                Assert.AreEqual(Application.Current.FindResource("EnvironmentWindowText"), statistics.GetRow(distribution.Parameters.Count).BorderBrush);

                // A new credible interval: the headers at once, and the curves once the element has reprocessed them
                // (on a worker thread, as upstream's model layer does), with the wait cursor until then.
                var saved = element.AnalysisResults.ConfidenceIntervals[0, 1];
                element.BayesianAnalysis.CredibleIntervalWidth = 0.99;
                AssertIntervalHeaders(table, 0.99);
                PumpUntil(() => Upper(element) is double upper && upper != saved && Mouse.OverrideCursor == null, "the reprocessed curves");
                Assert.AreEqual(string.Format(Us, value, element.AnalysisResults.ConfidenceIntervals[0, 1]), CellText(table, 0, 1));
                Assert.AreEqual("99% Credible Intervals", element.FrequencyPlot.Series.Single(s => s.Name == "CredibleIntervals").Title);
                element.UndoManager.Undo();
                AssertIntervalHeaders(table, 0.9);
                PumpUntil(() => Upper(element) == saved && Mouse.OverrideCursor == null, "the curves back");
                Assert.AreEqual(string.Format(Us, value, saved), CellText(table, 0, 1));
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                UserSettings.ValueStringFormat = valueFormat;
                Mouse.OverrideCursor = null;
            }
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(ArrFlike);
            var element = Element<UnivariateAnalysis>(project, "Example #3");
            var view = Host(new UnivariateAnalysisControl { Element = element });
            var properties = Host(new UnivariateAnalysisPropertiesControl { Element = element }, 420, 1100);

            // General: the element's name and dates, its input data, distribution and priors.
            Assert.AreEqual("Example #3", properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            Assert.AreSame(element.InputData, inputData.SelectedItem);
            // The project's input data, sorted by name; a valid one loses the red border and the tool tip asking for one.
            var listed = inputData.Items.Cast<InputData>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<InputData>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            Assert.AreEqual(new Thickness(0), ((Border)properties.InputDataComboBox.InnerContent).BorderThickness);
            Assert.IsNull(ToolTip.GetTip(properties.InputDataComboBox));
            var distribution = (ComboBox)properties.Distribution.InnerContent;
            var original = element.UnivariateDistribution.DistributionType;
            Assert.AreEqual(original, distribution.SelectedValue);
            Assert.AreEqual(properties.DistributionList.Single(i => i.Value == original).DisplayName, ((UnivariateDistributionItem)distribution.SelectedItem).DisplayName);
            Assert.AreSame(element.UnivariateDistribution, properties.ParameterPriorsControl.Model);
            Assert.AreSame(element.UnivariateDistribution, properties.QuantilePriorsControl.Model);
            Assert.AreEqual(element.UnivariateDistribution.UseJeffreysRuleForScale, properties.UseJeffreysRule.IsSelected);

            // Each edit writes to the element, and the element's undo restores it. A new distribution replaces the
            // model, which the panel gives its priors controls and combo box (upstream's Element_PropertyChanged).
            var other = original == UnivariateDistributionType.GeneralizedExtremeValue ? UnivariateDistributionType.Normal : UnivariateDistributionType.GeneralizedExtremeValue;
            distribution.SelectedItem = properties.DistributionList.Single(i => i.Value == other);
            Assert.AreEqual(other, element.UnivariateDistribution.DistributionType);
            Assert.IsFalse(element.IsEstimated);
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(original, element.UnivariateDistribution.DistributionType);
            Assert.AreEqual(original, distribution.SelectedValue, "the restored distribution shows");
            Assert.AreSame(element.UnivariateDistribution, properties.ParameterPriorsControl.Model, "the priors edit the restored model");
            Assert.AreSame(element.UnivariateDistribution, properties.QuantilePriorsControl.Model);

            properties.Description.Text = "Flood frequency at the gauge";
            Assert.AreEqual("Flood frequency at the gauge", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Flood frequency at the gauge", element.Description);

            bool jeffreys = element.UnivariateDistribution.UseJeffreysRuleForScale;
            properties.UseJeffreysRule.IsSelected = !jeffreys;
            Assert.AreEqual(!jeffreys, element.UnivariateDistribution.UseJeffreysRuleForScale);
            element.UndoManager.Undo();
            Assert.AreEqual(jeffreys, element.UnivariateDistribution.UseJeffreysRuleForScale);
            Assert.AreEqual(jeffreys, properties.UseJeffreysRule.IsSelected);

            // Options: a nonstationary model, its trend models and time index. The view shows the chronology tab
            // only for a nonstationary model.
            Select(properties.Options_TabItem);
            Assert.IsFalse(properties.IsNonstationary.IsSelected);
            Assert.IsFalse(view.ChronologyResultsTabItem.IsVisible);
            Assert.IsFalse(properties.ParameterTimeStep.IsTextBoxEnabled);
            var rows = properties.TrendModelDataGrid.ItemsSource.Cast<TrendModelRowItem>().ToList();
            CollectionAssert.AreEqual(element.UnivariateDistribution.TrendModels.Select(t => t.OwnerName).ToList(), rows.Select(r => r.Name).ToList());
            Assert.IsTrue(rows.All(r => r.Value == TrendModelType.Constant));
            foreach (var (column, key) in new[] { (0, "Left_ColumnHeaderStyle"), (1, "Center_ColumnHeaderStyle") })
            {
                // Upstream's header tool tips, on the columns' header styles, which as upstream's are based on the
                // previous ones each time the grid is bound (IssuesToRaise: header styles based on themselves).
                var theme = properties.TrendModelDataGrid.Columns[column].HeaderTheme;
                Assert.IsTrue(theme.Setters.OfType<Avalonia.Styling.Setter>().Any(s => s.Property == ToolTip.TipProperty), "column " + column);
                while (theme.BasedOn != null && theme.Setters.OfType<Avalonia.Styling.Setter>().Any(s => s.Property == ToolTip.TipProperty)) theme = theme.BasedOn;
                Assert.AreSame(Application.Current.FindResource(key), theme, "column " + column);
            }
            // As upstream's model, a nonstationary model also takes the data's middle time index: two undos.
            int undos = element.UndoManager.UndoStack.Count;
            properties.IsNonstationary.IsSelected = true;
            Settle();
            CollectionAssert.AreEqual(new[] { "Change ParameterTimeIndex", "Change IsNonstationary" },
                element.UndoManager.UndoStack.Take(element.UndoManager.UndoStack.Count - undos).Select(a => a.Description).ToList(), "newest first");
            Assert.IsTrue(element.UnivariateDistribution.IsNonstationary);
            Assert.IsTrue(view.ChronologyResultsTabItem.IsVisible);
            Assert.IsTrue(properties.ParameterTimeStep.IsTextBoxEnabled);
            var series = element.InputData.DataFrame.FullTimeSeries;
            Assert.AreEqual(series.First().Index, properties.ParameterTimeStep.MinValue);
            Assert.AreEqual(series.Last().Index + 100, properties.ParameterTimeStep.MaxValue);
            properties.ParameterTimeStep.Number = series.Last().Index;
            Assert.AreEqual(series.Last().Index, element.UnivariateDistribution.ParameterTimeIndex);
            element.UndoManager.Undo();
            Assert.AreNotEqual(series.Last().Index, element.UnivariateDistribution.ParameterTimeIndex);

            var trend = (ComboBox)properties.TrendModelDataGrid.GetCell(0, 1).Content;
            Assert.IsTrue(trend.IsEffectivelyEnabled, "the trend models are editable for a nonstationary model");
            trend.SelectedItem = properties.TrendFunctionItems.Single(i => i.Value == TrendModelType.Linear);
            Assert.AreEqual(TrendModelType.Linear, element.UnivariateDistribution.TrendModels[0].Type);
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(TrendModelType.Constant, element.UnivariateDistribution.TrendModels[0].Type);
            Assert.AreEqual(TrendModelType.Constant, properties.TrendModelDataGrid.ItemsSource.Cast<TrendModelRowItem>().First().Value, "the restored trend model shows");
            element.UndoManager.Undo();
            element.UndoManager.Undo();
            Settle();
            Assert.IsFalse(element.UnivariateDistribution.IsNonstationary);
            Assert.IsFalse(properties.IsNonstationary.IsSelected);
            Assert.IsFalse(view.ChronologyResultsTabItem.IsVisible);

            // The sampler's settings, as the Bayesian options control edits them.
            Assert.AreSame(element.BayesianAnalysis, properties.BayesianOptionsControl.Analysis);
            properties.BayesianOptionsControl.UseSimulationDefaults.IsSelected = false;
            int chains = element.BayesianAnalysis.NumberOfChains;
            properties.BayesianOptionsControl.NumberOfChains.Number = chains + 2;
            Assert.AreEqual(chains + 2, element.BayesianAnalysis.NumberOfChains);
            element.UndoManager.Undo();
            Assert.AreEqual(chains, element.BayesianAnalysis.NumberOfChains);
            element.UndoManager.Undo();

            // Output: the Bayesian output settings and the probability ordinates.
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
        }

        [STATestMethod]
        public void FrequencyPlot_FollowsTheInputDataLabel_AndKeepsAUsersTitle()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(BraysBayou), "NSFFA - Linear");
            Host(new UnivariateAnalysisControl { Element = element });
            var frequency = element.FrequencyPlot.Axes.Single(a => a.Key == "Yaxis");
            var chronologyY = element.ChronologyPlot.Axes.Single(a => a.Key == "Yaxis");
            var chronologyX = element.ChronologyPlot.Axes.Single(a => a.Key == "Xaxis");
            Assert.AreEqual(element.InputData.UnitLabel, frequency.Title);

            // As upstream's binding: the titles that are the input data's labels follow them.
            element.InputData.UnitLabel = "Peak flow (m3/s)";
            Assert.AreEqual("Peak flow (m3/s)", frequency.Title);
            if (chronologyY.Title == "Peak flow (m3/s)" || chronologyX.Title == element.InputData.IndexLabel)
            {
                element.InputData.IndexLabel = "Water year";
                Assert.AreEqual("Water year", chronologyX.Title);
            }

            // A title the user typed stays, whatever the label (PlotAxisTitleDefaults).
            frequency.Title = "Annual peak discharge";
            element.InputData.UnitLabel = "Discharge (m3/s)";
            Assert.AreEqual("Annual peak discharge", frequency.Title);
        }

        [STATestMethod]
        public void PreviewClicks_TellTheShellWhetherThePlotOrToolbarWasClicked()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(ArrFlike), "Example #3");
            var view = Host(new UnivariateAnalysisControl { Element = element });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => called.Add(plot);

            Click(window, Centre(window, element.FrequencyPlot));
            Assert.AreEqual((true, false, element.FrequencyPlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);

            Point toolbar = HitPoint(window, view.FrequencyPlotToolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.FrequencyPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            // The label beside the toggle button shows and hides the alternatives, as the button does.
            Assert.IsFalse(view.AlternativeSelector.IsVisible);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.AreEqual((false, false, element.FrequencyPlot), clicks.Last());
            Assert.IsTrue(view.ShowFilterToggleButton.IsChecked == true);
            Assert.IsTrue(view.AlternativeSelector.IsVisible);
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsFalse(view.AlternativeSelector.IsVisible);

            // A diagnostic plot's toolbar asks for its plot's properties through the view. Upstream asks the inner
            // Frequency Plot tab first, which stays selected under the other outer tabs.
            Select(view.HistogramTabItem);
            Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot(), "upstream's order of tests");
            Select(TabularResults(view));
            Select(view.HistogramTabItem);
            Assert.AreSame(view.HistogramControl.Plot, view.GetCurrentPlot());
            Click(window, HitPoint(window, view.HistogramControl.PlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.BayesianPlots.HistogramPlot, called.Last());
        }

        [STATestMethod]
        public void Alternatives_AddTheirCurvesToTheFrequencyPlot()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(BayesianB17C);
            var element = Element<UnivariateAnalysis>(project, "Bayes Example #1");
            var alternative = Element<UnivariateAnalysis>(project, "Bayes Example #2");
            var view = Host(new UnivariateAnalysisControl { Element = element });
            view.ShowFilterToggleButton.IsChecked = true;
            Settle();

            // The project's other univariate analyses, B17C ones too.
            var others = Elements(project).OfType<IAnalysisElement>().Where(e => e != element && e.ParentCollection == element.ParentCollection).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == alternative);
            var plot = element.FrequencyPlot;
            int index = plot.Series.Count;
            item.IsChecked = true;
            Settle();

            // Its interval, predictive and point curves, named by the series count, in the first unused colour of
            // upstream's list from the fifth on (the interval's fill at alpha 100, without a line).
            var color = Avalonia.Media.Color.Parse(GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)plot.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var predictive = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorPredictive_" + index);
            var point = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual("Bayes Example #2 - 90% Credible Intervals", interval.Title);
            Assert.AreEqual("Bayes Example #2 - Posterior Predictive", predictive.Title);
            Assert.AreEqual("Bayes Example #2 - Posterior Mean", point.Title);
            Assert.AreEqual(Avalonia.Media.Color.FromArgb(100, color.R, color.G, color.B), interval.Fill);
            Assert.AreEqual(Avalonia.Media.Colors.Transparent, interval.Color);
            Assert.AreEqual(color, predictive.Color);
            Assert.AreEqual(color, point.Color);
            var results = alternative.AnalysisResults;
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            Assert.AreEqual(alternative.ProbabilityOrdinates.Count, points.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(alternative.ProbabilityOrdinates[i], results.ModeCurve[i]), points[i]);
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");

            item.IsChecked = false;
            Settle();
            Assert.IsFalse(plot.Series.Any(s => s.Name.EndsWith("_" + index, StringComparison.Ordinal)));
        }

        [STATestMethod]
        [DataRow(ArrFlike)]
        [DataRow(BraysBayou)]
        [DataRow(OcFisher)]
        public void ShowingTheViews_ChangesNothingInTheElement(string example)
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(example);
            foreach (var element in Elements(project).OfType<UnivariateAnalysis>().ToList())
            {
                string name = element.Name;
                var changes = new List<string>();
                Watch(element, changes);
                Watch(element.UnivariateDistribution, changes);
                foreach (var parameter in element.UnivariateDistribution.Parameters) Watch(parameter, changes);
                Watch(element.BayesianAnalysis, changes);
                Watch(element.InputData, changes);

                // Two documents of the element, the first still holding it when the second opens, as upstream's
                // MainProjectNode leaves a closed one: the second takes the plots.
                var views = new List<UnivariateAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new UnivariateAnalysisControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.ShowFilterToggleButton.IsChecked = true;
                    foreach (TabItem tab in ((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Where(t => t.IsVisible))
                        Select(tab);
                    // Every tab of the first; the second's diagnostics are the first's (upstream's ParameterSetsControl
                    // keeps every view of an element alive until its project closes: IssuesToRaise).
                    if (document == 0)
                    {
                        foreach (TabItem tab in ((TabControl)view.DistributionResultsTabItem.Parent).Items.Cast<TabItem>())
                            Select(tab);
                        Select(view.DistributionResultsTabItem);
                    }
                    Select(view.FrequencyResultsTabItem);
                    CloseDocument(window);
                }
                var second = views[1];
                Assert.AreSame(element.FrequencyPlot, second.FrequencyPlotHost.Content, name);
                Assert.AreSame(element.ChronologyPlot, second.ChronologyPlotHost.Content, name);
                var plots = element.BayesianPlots;
                Assert.AreSame(plots.KernelDensityPlot, second.KernelDensityControl.PlotHost.Content, name);
                Assert.AreSame(plots.HistogramPlot, second.HistogramControl.PlotHost.Content, name);
                Assert.AreSame(plots.BivariateHeatMapPlot, second.BivariateHeatMapControl.PlotHost.Content, name);
                Assert.AreSame(plots.MeanLikelihoodPlot, second.MeanLikelihoodControl.PlotHost.Content, name);
                Assert.AreSame(plots.AutocorrelationPlot, second.AutocorrelationControl.PlotHost.Content, name);
                Assert.AreSame(plots.MarkovChainTracePlot, second.MarkovChainTraceControl.PlotHost.Content, name);
                Assert.AreSame(plots.InfluenceDiagnosticsPlot, second.InfluenceDiagnosticsControl.PlotHost.Content, name);

                // The properties panel, on each of its tabs.
                var properties = new UnivariateAnalysisPropertiesControl { Element = element };
                Window panel = Show(properties, 420, 1100);
                Select(properties.Options_TabItem);
                Select(properties.Output_TabItem);
                Select(properties.General_TabItem);
                CloseDocument(panel);
                properties.Element = null;

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                foreach (var view in views) view.Element = null;
            }
        }

        // The first upper interval value, or null while the element is reprocessing its results.
        private static double? Upper(UnivariateAnalysis element) =>
            element.AnalysisResults?.ConfidenceIntervals is double[,] intervals && intervals.GetLength(0) > 0 ? intervals[0, 1] : null;

        /// <summary>The Distribution Results tab's Tabular Results tab.</summary>
        internal static TabItem TabularResults(UnivariateAnalysisControl view) =>
            ((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Single(t => (string)t.Header == "Tabular Results");

        // The interval columns' headers, as upstream writes them: the percentages in the current culture.
        internal static void AssertIntervalHeaders(CopyPasteDataGrid table, double width)
        {
            double alpha = (1 - width) / 2;
            Assert.AreEqual(((1 - alpha) * 100).ToString("F1") + "% CI", table.Columns[1].Header);
            Assert.AreEqual((alpha * 100).ToString("F1") + "% CI", table.Columns[2].Header);
        }

        // A summary statistic as the table shows it: upstream's F4, through its N/A converter.
        internal static string Statistic(double value) =>
            double.IsNaN(value) ? "N/A" : double.IsPositiveInfinity(value) ? "+∞" : double.IsNegativeInfinity(value) ? "-∞" : value.ToString("F4", Us);

        /// <summary>Runs the dispatcher until a condition holds, as background reprocessing finishes.</summary>
        internal static void PumpUntil(Func<bool> condition, string what)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(60))
            {
                Avalonia.Threading.Dispatcher.UIThread.RunJobs();
                System.Threading.Thread.Sleep(10);
            }
            Assert.IsTrue(condition(), what);
            Settle();
        }

        // Takes the content out of its window, as the shell does when a document closes, then closes the window:
        // the test application's own window theme would otherwise give the plots on hidden tabs a new font as the
        // window closes.
        internal static void CloseDocument(Window window)
        {
            ((Border)window.Content).Child = null;
            Settle();
            window.Close();
        }

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

        private static void Watch(object source, List<string> changes)
        {
            if (source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }

        private static string CellText(CopyPasteDataGrid grid, int row, int column) =>
            (grid.GetCell(row, column)?.Content as TextBlock)?.Text;
    }
}
