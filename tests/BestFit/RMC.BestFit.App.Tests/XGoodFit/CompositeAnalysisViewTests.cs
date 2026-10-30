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
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.Estimation;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using ModelAnalyses = RMC.BestFit.Analyses;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Composite Analysis view and its properties panel (GUI/UnivariateAnalysis/Composite) on
    /// the composite example project's two composites, a competing-risks maximum and a fixed-weight mixture.
    /// </summary>
    /// <remarks>
    /// The frequency plot is compared with RMC-BestFit's native export of the competing-risks composite (the
    /// screenshot's capture data) and with the walkthrough's figures of both composites, which record what the
    /// desktop drew. The tables show upstream's own results, in US formats (plan D8). Editing the properties writes
    /// to the element as upstream's bindings do, and the element's undo restores it; showing, closing and reopening
    /// the views changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CompositeAnalysisViewTests
    {
        internal const string Example = Folder + "5-composite-analysis/mixed-population-examples.bestfit";
        internal const string Competing = "Competing Flood Types";
        internal const string Mixture = "Mixture of Flood Types";

        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the windows and projects a test opened, and removes the application's resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) CloseDocument(window);
            foreach (var project in _projects) Close(project);
            ReleaseAppResources();
            Mouse.OverrideCursor = null;
            // The closed projects' views and plots go before the next test opens another.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Both composites' frequency plots: the competing-risks maximum as RMC-BestFit exported it, and both as the
        /// walkthrough drew them, point for point.
        /// </summary>
        [STATestMethod]
        public void FrequencyPlot_EqualsRmcBestFitsExport_AndTheWalkthroughsFigures()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Example);
            var exports = NativeExports(Example).Where(e => e.Capture.GetProperty("plotId").GetString() == "composite.frequency").ToList();
            var figures = Figures(Example).Where(f => f.Figure.GetProperty("plotId").GetString() == "composite.frequency").ToList();
            Assert.AreEqual(1, exports.Count);
            Assert.AreEqual(2, figures.Count);
            int compared = 0;
            foreach (string name in new[] { Competing, Mixture })
            {
                var element = Element<CompositeAnalysis>(project, name);
                var view = Host(new CompositeAnalysisControl { Element = element });
                Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotHost.Content);
                Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotToolbar.Plot);
                Assert.IsTrue(view.SubDistCheckbox.IsChecked == true, "the sub-distributions show");
                foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                {
                    AssertFigure(element.FrequencyPlot, figure, file);
                    compared++;
                }
                foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                {
                    AssertNative(element.FrequencyPlot, capture, file);
                    compared++;
                }
                // Upstream's tracker formats: the curves' values in the user's format, the data's probabilities to nine
                // places.
                foreach (var series in element.FrequencyPlot.Series)
                    AssertTrackerFormat(series.Name == "ExactData" ? DataTracker : CurveTracker, series);
            }
            Assert.AreEqual(3, compared);
        }

        [STATestMethod]
        public void ResultsTables_ShowTheFrequencyCurveAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<CompositeAnalysis>(OpenProject(Example), Competing);
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes
                // decimal commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new CompositeAnalysisControl { Element = element });
                BivariateAnalysisViewTests.Select(Tab(view, 1));

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
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                Assert.AreEqual(BayesianAnalysis.PointEstimateType.PosteriorMean, element.BayesianAnalysis.PointEstimator);
                Assert.AreEqual("Posterior Mean", table.Columns[4].Header);

                // The walkthrough's saved 1% AEP flow and its 90% bounds: 3,429.371 (2,553.787 to 4,840.352).
                int one = Enumerable.Range(0, element.ProbabilityOrdinates.Count).Single(i => element.ProbabilityOrdinates[i] == 0.01);
                Assert.AreEqual("3,429.37", CellText(table, one, 4));
                Assert.AreEqual(3429.371, Math.Round(results.ModeCurve[one], 3));
                Assert.AreEqual(2553.787, Math.Round(results.ConfidenceIntervals[one, 0], 3));
                Assert.AreEqual(4840.352, Math.Round(results.ConfidenceIntervals[one, 1], 3));

                // The summary statistics: the point estimate distribution's range and moments (the composite's own
                // distribution has none), in upstream's F4.
                var distribution = element.GetPointEstimateDistribution();
                double[] moments = distribution.CentralMoments(1E-6);
                var rows = new (string Name, double Value)[]
                {
                    ("Minimum", distribution.Minimum < -1E12 ? double.NegativeInfinity : distribution.Minimum),
                    ("Maximum", distribution.Maximum > 1E12 ? double.PositiveInfinity : distribution.Maximum),
                    ("Mean", moments[0]), ("Std Dev", moments[1]), ("Skewness", moments[2]), ("Kurtosis", moments[3]),
                };
                var statistics = view.SummaryStatisticsTable;
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                Assert.AreEqual(rows.Length, statistics.Items.Count);
                for (int i = 0; i < rows.Length; i++)
                {
                    Assert.AreEqual(rows[i].Name, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Value), CellText(statistics, i, 1), rows[i].Name);
                }
                Assert.AreEqual("+∞", CellText(statistics, 1, 1), "the competing maximum's unbounded upper end");
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                UserSettings.ValueStringFormat = valueFormat;
            }
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Example);
            var element = Element<CompositeAnalysis>(project, Competing);
            var view = Host(new CompositeAnalysisControl { Element = element });
            var properties = Host(new CompositeAnalysisPropertiesControl { Element = element }, 420, 1100);

            // General: the element's name and dates, and its optional input data: <None>, then the project's input
            // data, sorted by name.
            Assert.AreEqual(Competing, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            Assert.AreEqual(element.LastModified.ToString(Us), properties.LastModified.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            var choices = inputData.Items.Cast<InputDataSelectionItem>().ToList();
            Assert.IsNull(choices[0].Value);
            Assert.AreEqual("<None>", choices[0].DisplayName);
            CollectionAssert.AreEquivalent(Elements(project).OfType<InputData>().ToList(), choices.Skip(1).Select(c => c.Value).ToList());
            CollectionAssert.AreEqual(choices.Skip(1).Select(c => c.DisplayName).OrderBy(n => n, StringComparer.InvariantCulture).ToList(),
                choices.Skip(1).Select(c => c.DisplayName).ToList());
            Assert.AreEqual("Full POR - Annual Max Series", element.InputData.Name);
            Assert.AreSame(element.InputData, ((InputDataSelectionItem)inputData.SelectedItem).Value);
            Assert.AreEqual(new Thickness(0), ((Border)properties.InputDataComboBox.InnerContent).BorderThickness);
            Assert.IsNull(ToolTip.GetTip(properties.InputDataComboBox));

            // Composite options: a competing-risks maximum of independent components, without weights.
            var type = (ComboBox)properties.CompositeType.InnerContent;
            var dependency = (ComboBox)properties.Dependency.InnerContent;
            var average = (ComboBox)properties.AverageMethod.InnerContent;
            Assert.AreEqual(ModelAnalyses.CompositeType.CompetingRisks, type.SelectedValue);
            Assert.AreEqual(Numerics.Data.Statistics.Probability.DependencyType.Independent, dependency.SelectedValue);
            Assert.IsTrue(properties.IsMaximum.IsSelected);
            Assert.IsFalse(properties.AverageMethod.IsVisible);
            Assert.IsTrue(properties.Dependency.IsVisible);
            Assert.IsTrue(properties.IsMaximum.IsVisible);
            var grid = properties.CompositeFunctionDataGrid;
            Assert.IsFalse(grid.Columns[1].IsVisible, "no weights for competing risks");

            // The components: a row each, whose combo box lists the project's univariate analyses but composites,
            // sorted by name.
            var eligible = Elements(project).OfType<IUnivariate>().Where(u => u is not CompositeAnalysis).ToList();
            Assert.AreEqual(element.Analyses.Count, grid.Items.Count);
            for (int i = 0; i < element.Analyses.Count; i++)
            {
                var component = Component(grid, i);
                Assert.AreSame(element.Analyses[i].UnivariateAnalysis, component.SelectedItem, "row " + i);
                var listed = component.Items.Cast<IUnivariate>().ToList();
                CollectionAssert.AreEquivalent(eligible, listed, "row " + i);
                CollectionAssert.AreEqual(listed.Select(u => u.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(u => u.Name).ToList());
            }

            // Each edit writes to the element, and the element's undo restores it. A mixture has weights, which the
            // grid shows and edits; a model average computes them, so they show greyed, and it has a method.
            int undos = element.UndoManager.UndoStack.Count;
            type.SelectedItem = properties.CompositeTypeList.Single(i => i.Value == ModelAnalyses.CompositeType.Mixture);
            Settle();
            Assert.AreEqual(ModelAnalyses.CompositeType.Mixture, element.CompositeDistributionType);
            Assert.IsFalse(properties.Dependency.IsVisible);
            Assert.IsFalse(properties.IsMaximum.IsVisible);
            Assert.IsFalse(properties.AverageMethod.IsVisible);
            Assert.IsTrue(grid.Columns[1].IsVisible);
            Assert.IsFalse(grid.Columns[1].IsReadOnly);
            Assert.AreEqual(element.Analyses[0].Weight.ToString(Us), CellText(grid, 0, 1));
            // A weight edits in its cell, in WPF's binding culture. The edit changes the analyses, which gives the weight
            // cells upstream's theme: Right_CellStyle, with a trigger on the rows' IsWeightValid, which they don't have,
            // so it never flags a weight (an upstream bug).
            CoincidentFrequencyViewTests.Edit(grid, 0, 1, "0.6");
            Assert.AreEqual(0.6, element.Analyses[0].Weight);
            Assert.AreEqual("0.6", CellText(grid, 0, 1));
            Assert.AreSame(Application.Current.FindResource("Right_CellStyle"), grid.Columns[1].CellTheme.BasedOn);
            Assert.IsTrue(grid.Columns[1].CellTheme.Setters.OfType<Avalonia.Styling.Setter>().Any(s => s.Property == NumericControls.DataTrigger.BindingProperty));
            Assert.IsNull(NumericControls.DataTrigger.GetBinding(grid.GetCell(0, 1)), "the trigger's binding finds no IsWeightValid");
            type.SelectedItem = properties.CompositeTypeList.Single(i => i.Value == ModelAnalyses.CompositeType.ModelAverage);
            Settle();
            Assert.AreEqual(ModelAnalyses.CompositeType.ModelAverage, element.CompositeDistributionType);
            Assert.IsTrue(properties.AverageMethod.IsVisible);
            Assert.IsTrue(grid.Columns[1].IsVisible);
            Assert.IsTrue(grid.Columns[1].IsReadOnly);
            Assert.AreEqual(Avalonia.Media.Brushes.Gray, ((DataGridTextColumn)grid.Columns[1]).Foreground);
            Assert.AreEqual(Avalonia.Media.FontStyle.Italic, ((DataGridTextColumn)grid.Columns[1]).FontStyle);
            var method = element.ModelAverageMethod;
            var other = properties.AverageMethodList.First(i => i.Value != method);
            average.SelectedItem = other;
            Assert.AreEqual(other.Value, element.ModelAverageMethod);
            while (element.UndoManager.UndoStack.Count > undos) element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(ModelAnalyses.CompositeType.CompetingRisks, element.CompositeDistributionType);
            Assert.AreEqual(method, element.ModelAverageMethod);
            Assert.AreEqual(ModelAnalyses.CompositeType.CompetingRisks, type.SelectedValue, "the restored type shows");

            dependency.SelectedItem = properties.DependencyList.Single(i => i.Value == Numerics.Data.Statistics.Probability.DependencyType.PerfectlyPositive);
            Assert.AreEqual(Numerics.Data.Statistics.Probability.DependencyType.PerfectlyPositive, element.Dependency);
            element.UndoManager.Undo();
            Assert.AreEqual(Numerics.Data.Statistics.Probability.DependencyType.Independent, element.Dependency);
            Assert.AreEqual(Numerics.Data.Statistics.Probability.DependencyType.Independent, dependency.SelectedValue);

            properties.IsMaximum.IsSelected = false;
            Assert.IsFalse(element.IsMaximum);
            element.UndoManager.Undo();
            Assert.IsTrue(element.IsMaximum);
            Assert.IsTrue(properties.IsMaximum.IsSelected);

            properties.Description.Text = "Snow and rain floods";
            Assert.AreEqual("Snow and rain floods", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Snow and rain floods", element.Description);

            // Options: the credible interval and point estimator.
            BivariateAnalysisViewTests.Select(properties.Options_TabItem);
            var interval = (ComboBox)properties.CredibleInterval.InnerContent;
            Assert.AreEqual(0.9, interval.SelectedValue);
            interval.SelectedItem = properties.CredibleIntervalItems.Single(i => i.Value == 0.99);
            Assert.AreEqual(0.99, element.BayesianAnalysis.CredibleIntervalWidth);
            UnivariateAnalysisViewTests.AssertIntervalHeaders(view.FrequencyCurveTable, 0.99);
            element.UndoManager.Undo();
            Assert.AreEqual(0.9, element.BayesianAnalysis.CredibleIntervalWidth);
            Assert.AreEqual(0.9, interval.SelectedValue);
            Assert.AreSame(element.ProbabilityOrdinates, properties.ProbabilityOrdinatesControl.ProbabilityOrdinates);
            BivariateAnalysisViewTests.Select(properties.General_TabItem);

            // The input data last: none is a valid choice (the plot's observed data go).
            InputData data = element.InputData;
            inputData.SelectedItem = choices[0];
            Assert.IsNull(element.InputData);
            Assert.AreEqual(new Thickness(0), ((Border)properties.InputDataComboBox.InnerContent).BorderThickness);
            Assert.IsFalse(element.FrequencyPlot.Series.Any(s => s.Name == "ExactData"));
            element.UndoManager.Undo();
            Settle();
            Assert.AreSame(data, element.InputData);
            Assert.AreSame(data, ((InputDataSelectionItem)inputData.SelectedItem).Value);
            Assert.IsTrue(element.FrequencyPlot.Series.Any(s => s.Name == "ExactData"));

            // A component chosen in its row's combo box clears the composite's results. As upstream's element, the
            // choice isn't recorded for undo (only the rows and a mixture's weights are): choose the first again.
            IUnivariate first = element.Analyses[0].UnivariateAnalysis;
            IUnivariate replacement = eligible.First(u => u != first && element.Analyses.All(a => a.UnivariateAnalysis != u));
            undos = element.UndoManager.UndoStack.Count;
            Component(grid, 0).SelectedItem = replacement;
            Assert.AreSame(replacement, element.Analyses[0].UnivariateAnalysis);
            Assert.IsFalse(element.IsEstimated);
            Assert.AreEqual(undos, element.UndoManager.UndoStack.Count, "upstream records no undo for a component");
            Assert.IsTrue(element.IsDirty, "the composite has a change to save");
            Component(grid, 0).SelectedItem = first;
            Assert.AreSame(first, element.Analyses[0].UnivariateAnalysis);
            Assert.AreSame(first, Component(grid, 0).SelectedItem);
        }

        [STATestMethod]
        public void FrequencyPlot_FollowsTheInputDataLabel_AndKeepsAUsersTitle()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<CompositeAnalysis>(OpenProject(Example), Competing);
            Host(new CompositeAnalysisControl { Element = element });
            var axis = element.FrequencyPlot.Axes.Single(a => a.Key == "Yaxis");
            Assert.AreEqual(element.InputData.UnitLabel, axis.Title);

            // As upstream's binding: a title that is the input data's label follows it.
            element.InputData.UnitLabel = "Peak flow (m3/s)";
            Assert.AreEqual("Peak flow (m3/s)", axis.Title);

            // A title the user typed stays, whatever the label (PlotAxisTitleDefaults).
            axis.Title = "Annual peak discharge";
            element.InputData.UnitLabel = "Discharge (m3/s)";
            Assert.AreEqual("Annual peak discharge", axis.Title);
        }

        [STATestMethod]
        public void SubDistributionsAndAlternatives_AddTheirCurvesToTheFrequencyPlot()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Example);
            var element = Element<CompositeAnalysis>(project, Competing);
            var mixture = Element<CompositeAnalysis>(project, Mixture);
            var view = Host(new CompositeAnalysisControl { Element = element });
            var plot = element.FrequencyPlot;

            // The components' point curves first, in upstream's short list of colours from the second on (alpha 200).
            for (int i = 0; i < element.Analyses.Count; i++)
            {
                var component = (Wpf.LineSeries)plot.Series[i];
                Assert.AreEqual("series_" + i, component.Name);
                Assert.AreEqual("Posterior Mean - " + element.Analyses[i].UnivariateAnalysis.Name, component.Title);
                var color = Avalonia.Media.Color.Parse(GenericControls.GeneralMethods.RandomColorsShortList[i + 1]);
                Assert.AreEqual(Avalonia.Media.Color.FromArgb(200, color.R, color.G, color.B), component.Color);
            }
            // Hidden, the plot has the composite's curves and the data.
            view.SubDistCheckbox.IsChecked = false;
            Settle();
            CollectionAssert.AreEqual(new[] { "CredibleIntervals", "PosteriorPredictive", "PosteriorMode", "ExactData" }, plot.Series.Select(s => s.Name).ToList());
            view.SubDistCheckbox.IsChecked = true;
            Settle();
            CollectionAssert.AreEqual(new[] { "series_0", "series_1", "CredibleIntervals", "PosteriorPredictive", "PosteriorMode", "ExactData" }, plot.Series.Select(s => s.Name).ToList());

            // Alternatives: the project's other univariate analyses, the other composite too.
            view.ShowFilterToggleButton.IsChecked = true;
            Settle();
            var others = Elements(project).OfType<IAnalysisElement>().Where(e => e != element && e.ParentCollection == element.ParentCollection).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == mixture);
            int index = plot.Series.Count;
            item.IsChecked = true;
            Settle();

            // Its interval, predictive and point curves, named by the series count, in the first unused colour of
            // upstream's list from the fifth on (the interval's fill at alpha 100, without a line).
            var color4 = Avalonia.Media.Color.Parse(GenericControls.GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)plot.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var predictive = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorPredictive_" + index);
            var point = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual(Mixture + " - 90% Credible Intervals", interval.Title);
            Assert.AreEqual(Mixture + " - Posterior Predictive", predictive.Title);
            Assert.AreEqual(Mixture + " - Posterior Mean", point.Title);
            Assert.AreEqual(Avalonia.Media.Color.FromArgb(100, color4.R, color4.G, color4.B), interval.Fill);
            Assert.AreEqual(Avalonia.Media.Colors.Transparent, interval.Color);
            Assert.AreEqual(color4, point.Color);
            var points = ((IEnumerable<OxyPlot.DataPoint>)point.ItemsSource).ToList();
            Assert.AreEqual(mixture.ProbabilityOrdinates.Count, points.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(new OxyPlot.DataPoint(mixture.ProbabilityOrdinates[i], mixture.AnalysisResults.ModeCurve[i]), points[i]);

            // The checked alternative stays as the plot updates, after the composite's own curves and data, renamed
            // by the series count again.
            view.SubDistCheckbox.IsChecked = false;
            Settle();
            CollectionAssert.AreEqual(new[] { "CredibleIntervals", "PosteriorPredictive", "PosteriorMode", "ExactData", "CredibleIntervals_4", "PosteriorPredictive_4", "PosteriorMode_4" },
                plot.Series.Select(s => s.Name).ToList());
            Assert.AreSame(point, plot.Series.Last());
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(plot.Series.Contains(interval) || plot.Series.Contains(predictive) || plot.Series.Contains(point));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");
            Assert.IsFalse(element.IsDirty);
        }

        [STATestMethod]
        public void PreviewClicks_TellTheShellWhetherThePlotOrToolbarWasClicked()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<CompositeAnalysis>(OpenProject(Example), Competing);
            var view = Host(new CompositeAnalysisControl { Element = element });
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

            // The toolbar's Properties button asks for the plot's properties through the view.
            Click(window, HitPoint(window, view.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.FrequencyPlot, called.Last());
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Example);
            // The two walkthrough composites, and the example's third, which isn't estimated.
            var composites = Elements(project).OfType<CompositeAnalysis>().ToList();
            Assert.AreEqual(3, composites.Count);
            foreach (var element in composites)
            {
                string name = element.Name;
                bool estimated = element.IsEstimated;
                var changes = new List<string>();
                Watch(element, changes);
                Watch(element.BayesianAnalysis, changes);
                Watch(element.InputData, changes);
                foreach (var component in element.Analyses) Watch(component, changes);
                element.Analyses.CollectionChanged += (sender, e) => changes.Add("Analyses." + e.Action);

                // Two documents of the element, the first still holding it when the second opens, as upstream's
                // MainProjectNode leaves a closed one: the second takes the plot.
                var views = new List<CompositeAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new CompositeAnalysisControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.ShowFilterToggleButton.IsChecked = true;
                    view.SubDistCheckbox.IsChecked = false;
                    Settle();
                    view.SubDistCheckbox.IsChecked = true;
                    BivariateAnalysisViewTests.Select(Tab(view, 1));
                    BivariateAnalysisViewTests.Select(Tab(view, 0));
                    if (document == 0) ChangeThemes();
                    CloseDocument(window);
                }
                Assert.AreSame(element.FrequencyPlot, views[1].FrequencyPlotHost.Content, name);

                // The properties panel, on each of its tabs, and in each theme.
                var properties = new CompositeAnalysisPropertiesControl { Element = element };
                Window panel = Show(properties, 420, 1100);
                BivariateAnalysisViewTests.Select(properties.Options_TabItem);
                BivariateAnalysisViewTests.Select(properties.General_TabItem);
                ChangeThemes();
                CloseDocument(panel);
                properties.Element = null;

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                Assert.AreEqual(estimated, element.IsEstimated, name);
                foreach (var view in views) view.Element = null;
            }
        }

        // Shows the views in the Dark, Blue and Light themes in turn, as the theme menu does: a theme restyles the
        // views, which mustn't write to the element (upstream's AnalysisSourceSelectionLifecycleTests).
        internal static void ChangeThemes()
        {
            foreach (ThemeColor theme in new[] { ThemeColor.Dark, ThemeColor.Blue, ThemeColor.Light })
            {
                ThemeManager.SetTheme(theme);
                Settle();
            }
        }

        // Upstream's tracker formats, built with Environment.NewLine.
        internal static string CurveTracker => "{0}" + Environment.NewLine + "{1}: {2}" + Environment.NewLine + "{3}: {4:" + UserSettings.ValueStringFormat + "}";
        internal static string DataTracker => "{0}" + Environment.NewLine + "{1}: {2:0.000000000}" + Environment.NewLine + "{3}: {4:" + UserSettings.ValueStringFormat + "}";

        // A series keeps a saved format that differs only in its line breaks (OxyPlot.Wpf's Series), so off Windows a
        // series read from a project keeps its CRLFs: compare line by line.
        internal static void AssertTrackerFormat(string expected, Wpf.Series series) =>
            Assert.AreEqual(expected.Replace("\r\n", "\n"), series.TrackerFormatString?.Replace("\r\n", "\n"), series.Name);

        // The view's tabs: Frequency Plot (0) and Tabular Results (1).
        internal static TabItem Tab(CompositeAnalysisControl view, int index) =>
            ((TabControl)((Panel)view.Content).Children[0]).Items.Cast<TabItem>().ElementAt(index);

        // A components row's combo box: the cell template's grid holds it.
        private static ComboBox Component(GenericControls.CopyPasteDataGrid grid, int row) =>
            (ComboBox)((Grid)grid.GetCell(row, 0).Content).Children[0];

        private static void Watch(object source, List<string> changes)
        {
            if (source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }

        internal static string CellText(GenericControls.CopyPasteDataGrid grid, int row, int column) =>
            (grid.GetCell(row, column)?.Content as TextBlock)?.Text;

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

        // Takes the content out of its window, as the shell does when a document closes, then closes the window:
        // the test application's own window theme would otherwise give the plots on hidden tabs a new font as the
        // window closes.
        internal static void CloseDocument(Window window)
        {
            if (window.Content is Border border) border.Child = null;
            Settle();
            window.Close();
        }
    }
}
