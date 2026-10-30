using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using RMC.BestFit.Estimation;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat.Input;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Coincident Frequency Analysis view and its properties panel
    /// (GUI/BivariateAnalysis/CoincidentFrequency) on the coincident-frequency examples: the sum of two Normal
    /// variables at three correlations, and the Waimea River's conditional stage frequency.
    /// </summary>
    /// <remarks>
    /// The frequency plot is compared with RMC-BestFit's native exports (the screenshots' capture data) and with the
    /// walkthroughs' figures, which record what the desktop drew. The response grid and the tables show upstream's
    /// own data and results, in US formats (plan D8). Editing writes to the element as upstream's bindings and
    /// handlers do, and the element's undo restores it; showing, closing and reopening the views changes nothing in
    /// the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CoincidentFrequencyViewTests
    {
        internal const string Folder = "5-bivariate-distribution-analysis/2-coincident-frequency/";
        internal const string SumOfNormals = Folder + "sum-two-normals.bestfit";
        internal const string Waimea = Folder + "waimea-river-stage-frequency.bestfit";
        internal const string NearZero = "CFA - Rho = 0.0";
        internal const string Negative = "CFA - Rho = -0.5";
        internal const string Positive = "CFA - Rho = +0.5";
        internal const string Conditional = "CFA - Normal - Conditional";

        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the windows and projects a test opened, and removes the application's resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) CompositeAnalysisViewTests.CloseDocument(window);
            foreach (var project in _projects) Close(project);
            ReleaseAppResources();
            Mouse.OverrideCursor = null;
            // The closed projects' views and plots go before the next test opens another.
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// Each analysis's frequency plot that RMC-BestFit exported or drew for its walkthrough: the probabilities at
        /// each response, their interval and the observed sums, point for point.
        /// </summary>
        [STATestMethod]
        [DataRow(SumOfNormals)]
        [DataRow(Waimea)]
        public void FrequencyPlot_EqualsRmcBestFitsExports_AndTheWalkthroughsFigures(string example)
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(example);
            var exports = NativeExports(example).Where(e => e.Capture.GetProperty("plotId").GetString() == "coincident.frequency").ToList();
            var figures = Figures(example).Where(f => f.Figure.GetProperty("plotId").GetString() == "coincident.frequency").ToList();
            Assert.AreEqual(1, exports.Count, example);
            Assert.AreEqual(example == SumOfNormals ? 3 : 1, figures.Count, example);
            int compared = 0;
            foreach (var element in project.ElementCollections.OfType<BivariateAnalysisCollection>().Single().OfType<CoincidentFrequencyAnalysis>())
            {
                var view = Host(new CoincidentFrequencyControl { Element = element });
                Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotHost.Content);
                Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotToolbar.Plot);
                Assert.IsNull(view.GetCurrentPlot(), "the response grid's tab is the one selected");
                Select(view, 1);
                Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot());
                foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == element.Name))
                {
                    AssertFigure(element.FrequencyPlot, figure, file);
                    compared++;
                }
                foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == element.Name))
                {
                    AssertNativeSeries(element.FrequencyPlot, capture, file);
                    compared++;
                }
                foreach (var series in element.FrequencyPlot.Series)
                    CompositeAnalysisViewTests.AssertTrackerFormat(series.Name == "ExactData" ? CompositeAnalysisViewTests.DataTracker : CompositeAnalysisViewTests.CurveTracker, series);
                // The export was made from a factory-reset plot: the saved plot keeps the annotation its user drew,
                // Waimea's levee line, as RMC-BestFit shows it.
                if (element.Name == Conditional)
                    Assert.AreEqual("Top of Levee = 22.85 ft", ((Wpf.LineAnnotation)element.FrequencyPlot.Annotations.Single()).Text);
                else
                    Assert.AreEqual(0, element.FrequencyPlot.Annotations.Count, element.Name);
            }
            Assert.AreEqual(exports.Count + figures.Count, compared);
        }

        [STATestMethod]
        public void ResponseGrid_ShowsTheSurface_AndEditsWriteToTheElement_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<CoincidentFrequencyAnalysis>(OpenProject(SumOfNormals), NearZero);
            var view = Host(new CoincidentFrequencyControl { Element = element });
            var grid = view.BivariateDataGrid;

            // The secondary (Y) ordinates head the columns and the primary (X) ordinates the rows, as upstream writes
            // them (the invariant culture); every saved response is X + Y, shown in WPF's binding culture.
            var x = element.XValues.ToList();
            var y = element.YValues.ToList();
            CollectionAssert.AreEqual(new[] { 65d, 75, 90, 100, 110, 125, 135 }, x);
            CollectionAssert.AreEqual(new[] { 22d, 39, 63, 80, 97, 121, 138 }, y);
            CollectionAssert.AreEqual(y.Select(v => v.ToString(CultureInfo.InvariantCulture)).ToList(), grid.Columns.Select(c => (string)c.Header).ToList());
            var rowHeader = Application.Current.FindResource("Right_RowHeaderStyle");
            for (int i = 0; i < x.Count; i++)
            {
                DataGridRow row = grid.GetRow(i);
                Assert.AreEqual(x[i].ToString(CultureInfo.InvariantCulture), row.Header, "row " + i);
                Assert.AreSame(rowHeader, row.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.DataGridRowHeader>().Single().Theme, "the grid's row header style");
                for (int j = 0; j < y.Count; j++)
                {
                    Assert.AreEqual(x[i] + y[j], element.BivariateResponse[i, j]);
                    Assert.AreEqual((x[i] + y[j]).ToString("G", Us), CompositeAnalysisViewTests.CellText(grid, i, j), $"cell {i}, {j}");
                    Assert.IsFalse(IsFlagged(grid, i, j), $"cell {i}, {j} is valid");
                }
            }

            // An edit writes the cell to the element's response, which records it for undo; undo rebuilds the grid.
            Edit(grid, 2, 3, "171");
            Assert.AreEqual(171, element.BivariateResponse[2, 3]);
            Assert.AreEqual("171", CompositeAnalysisViewTests.CellText(grid, 2, 3));
            Assert.IsTrue(element.UndoManager.CanUndo);
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(170, element.BivariateResponse[2, 3]);
            Assert.AreEqual("170", CompositeAnalysisViewTests.CellText(grid, 2, 3));

            // A value that breaks the surface's strict increase flags the cell and the neighbours it disagrees with,
            // with upstream's tool tips; a valid value takes the flags off again.
            Edit(grid, 2, 3, "300");
            Assert.AreEqual(300, element.BivariateResponse[2, 3]);
            Assert.IsTrue(IsFlagged(grid, 2, 3));
            Assert.AreEqual("The response surface must be strictly increasing; this value must be less than the next X-row value, less than the next Y-column value.",
                ToolTip.GetTip(grid.GetCell(2, 3)));
            Assert.AreEqual(new Thickness(1), grid.GetCell(2, 3).BorderThickness);
            Assert.IsTrue(IsFlagged(grid, 3, 3), "the next X row's");
            Assert.IsTrue(IsFlagged(grid, 2, 4), "the next Y column's");
            Assert.IsFalse(IsFlagged(grid, 1, 3), "the previous X row's is still less");
            Edit(grid, 2, 3, "170");
            Assert.AreEqual(170, element.BivariateResponse[2, 3]);
            foreach (var (i, j) in new[] { (2, 3), (3, 3), (2, 4) })
            {
                Assert.IsFalse(IsFlagged(grid, i, j), $"cell {i}, {j}");
                Assert.IsNull(ToolTip.GetTip(grid.GetCell(i, j)), $"cell {i}, {j}");
            }

            // A paste writes the block to the response at once.
            int undos = element.UndoManager.UndoStack.Count;
            grid.SelectedCells = new[] { new DataGridCellInfo(grid.Items[0], grid.Columns[0], 0, 0, true), new DataGridCellInfo(grid.Items[0], grid.Columns[1], 0, 1, true) };
            grid.SelectedCells = null;
            WpfCompat.Clipboard.SetText("88\t105");
            grid.PasteClipboard();
            Settle();
            Assert.AreEqual(88, element.BivariateResponse[0, 0]);
            Assert.AreEqual(105, element.BivariateResponse[0, 1]);
            Assert.AreEqual("88", CompositeAnalysisViewTests.CellText(grid, 0, 0));
            Assert.AreEqual("105", CompositeAnalysisViewTests.CellText(grid, 0, 1));
            Assert.AreEqual(undos + 1, element.UndoManager.UndoStack.Count, "one undo for the paste");
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(87, element.BivariateResponse[0, 0]);
            Assert.AreEqual("104", CompositeAnalysisViewTests.CellText(grid, 0, 1));
        }

        [STATestMethod]
        public void ResultsTables_ShowTheProbabilitiesAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(SumOfNormals);
            CultureInfo culture = CultureInfo.CurrentCulture;
            try
            {
                // On a machine whose culture writes decimal commas, the tables keep WPF's binding culture, en-US.
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                // The walkthrough's rows: each fixes a response and reports its exceedance probability and bounds.
                var walkthrough = new Dictionary<string, (double Z, double Aep, double Lower, double Upper)[]>
                {
                    [NearZero] = new[] { (135.947, 0.931373, 0.890485, 0.96115), (184.895, 0.449537, 0.375522, 0.524639), (233.842, 0.0426435, 0.0214771, 0.073852) },
                    [Negative] = new[] { (132.551, 0.9621, 0.932147, 0.98229), (181.898, 0.418483, 0.336907, 0.500623), (227.449, 0.0200105, 0.00805294, 0.0399749) },
                    [Positive] = new[] { (132.551, 0.939866, 0.912128, 0.961259), (181.898, 0.555414, 0.499386, 0.609683), (227.449, 0.122184, 0.0891043, 0.160763) },
                };
                foreach (string name in new[] { NearZero, Negative, Positive })
                {
                    var element = Element<CoincidentFrequencyAnalysis>(project, name);
                    var view = Host(new CoincidentFrequencyControl { Element = element });
                    Select(view, 2);

                    // One row per response bin: the response, then the probabilities in scientific notation.
                    var results = element.AnalysisResults;
                    var z = element.ZOutputValues;
                    var table = view.FrequencyCurveTable;
                    Assert.AreEqual(name == NearZero ? 20 : 50, element.NumberOfBins);
                    Assert.AreEqual(z.Length, table.Items.Count, name);
                    for (int k = 0; k < z.Length; k++)
                    {
                        string[] expected =
                        {
                            z[k].ToString(Us), results.ConfidenceIntervals[k, 1].ToString("E4", Us), results.ConfidenceIntervals[k, 0].ToString("E4", Us),
                            results.MeanCurve[k].ToString("E4", Us), results.ModeCurve[k].ToString("E4", Us),
                        };
                        for (int column = 0; column < expected.Length; column++)
                            Assert.AreEqual(expected[column], CompositeAnalysisViewTests.CellText(table, k, column), $"{name}: row {k}, column {column}");
                    }
                    UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                    Assert.AreEqual(name == NearZero ? 0.95 : 0.9, element.BayesianAnalysis.CredibleIntervalWidth);
                    Assert.AreEqual("Posterior Mean", table.Columns[4].Header);
                    foreach (var row in walkthrough[name])
                    {
                        int k = Enumerable.Range(0, z.Length).Single(i => Math.Round(z[i], 3) == row.Z);
                        Assert.AreEqual(row.Aep, results.ModeCurve[k], 5E-7, $"{name} at {row.Z}");
                        Assert.AreEqual(row.Lower, results.ConfidenceIntervals[k, 0], 5E-7, $"{name} at {row.Z}");
                        Assert.AreEqual(row.Upper, results.ConfidenceIntervals[k, 1], 5E-7, $"{name} at {row.Z}");
                    }

                    // The summary statistics: the bins' range, and the point estimate distribution's moments, in F4.
                    double[] moments = element.GetPointEstimateDistribution().CentralMoments(1000);
                    var rows = new (string Name, double Value)[]
                    {
                        ("Minimum", z[0]), ("Maximum", z[^1]), ("Mean", moments[0]), ("Std Dev", moments[1]), ("Skewness", moments[2]), ("Kurtosis", moments[3]),
                    };
                    var statistics = view.SummaryStatisticsTable;
                    Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                    Assert.AreEqual(rows.Length, statistics.Items.Count);
                    for (int i = 0; i < rows.Length; i++)
                    {
                        Assert.AreEqual(rows[i].Name, CompositeAnalysisViewTests.CellText(statistics, i, 0));
                        Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Value), CompositeAnalysisViewTests.CellText(statistics, i, 1), $"{name}: {rows[i].Name}");
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
            }
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(SumOfNormals);
            var element = Element<CoincidentFrequencyAnalysis>(project, NearZero);
            var view = Host(new CoincidentFrequencyControl { Element = element });
            var properties = Host(new CoincidentFrequencyPropertiesControl { Element = element }, 420, 1100);

            // General: the element's name and dates, its optional input data (<None>, then the project's input data
            // in the project's order) and its bivariate analysis (the project's copulas).
            Assert.AreEqual(NearZero, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            Assert.AreEqual(element.LastModified.ToString(Us), properties.LastModified.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            var choices = inputData.Items.Cast<InputDataSelectionItem>().ToList();
            Assert.IsNull(choices[0].Value);
            CollectionAssert.AreEqual(project.ElementCollections.OfType<InputDataCollection>().Single().OfType<InputData>().ToList(), choices.Skip(1).Select(c => c.Value).ToList());
            Assert.AreEqual("X+Y Data - Rho = 0.0", element.InputData.Name);
            Assert.AreSame(element.InputData, ((InputDataSelectionItem)inputData.SelectedItem).Value);
            Assert.AreEqual(new Thickness(0), ((Border)properties.InputDataComboBox.InnerContent).BorderThickness);
            Assert.IsNull(ToolTip.GetTip(properties.InputDataComboBox));
            var analysis = (ComboBox)((Border)properties.AnalysisComboBox.InnerContent).Child;
            CollectionAssert.AreEqual(project.ElementCollections.OfType<BivariateAnalysisCollection>().Single().OfType<BivariateAnalysis>().ToList(), analysis.Items.Cast<BivariateAnalysis>().ToList());
            Assert.AreEqual("Normal Copula - Rho = 0.0", element.BivariateAnalysis.Name);
            Assert.AreSame(element.BivariateAnalysis, analysis.SelectedItem);
            Assert.AreEqual(new Thickness(0), ((Border)properties.AnalysisComboBox.InnerContent).BorderThickness);
            Assert.IsNull(ToolTip.GetTip(properties.AnalysisComboBox));
            Assert.AreSame(element.XValues, properties.XOrdinateDataGrid.Ordinates);
            Assert.AreSame(element.YValues, properties.YOrdinateDataGrid.Ordinates);

            // Each edit writes to the element, and the element's undo restores it.
            properties.Description.Text = "Z = X + Y";
            Assert.AreEqual("Z = X + Y", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Z = X + Y", element.Description);

            // An ordinate: the response grid follows it.
            var xGrid = properties.XOrdinateDataGrid.FindControl<GenericControls.ValidationDataGrid>("DataGrid");
            Edit(xGrid, 0, 0, "60");
            Assert.AreEqual(60, element.XValues[0]);
            Assert.AreEqual("60", view.BivariateDataGrid.GetRow(0).Header);
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(65, element.XValues[0]);
            Assert.AreEqual("65", view.BivariateDataGrid.GetRow(0).Header);

            // Output: the credible interval, point estimator and response bins.
            BivariateAnalysisViewTests.Select(properties.Output_TabItem);
            var interval = (ComboBox)properties.CredibleInterval.InnerContent;
            Assert.AreEqual(0.95, interval.SelectedValue);
            interval.SelectedItem = properties.CredibleIntervalItems.Single(i => i.Value == 0.99);
            Assert.AreEqual(0.99, element.BayesianAnalysis.CredibleIntervalWidth);
            UnivariateAnalysisViewTests.AssertIntervalHeaders(view.FrequencyCurveTable, 0.99);
            element.UndoManager.Undo();
            Assert.AreEqual(0.95, element.BayesianAnalysis.CredibleIntervalWidth);
            Assert.AreEqual(0.95, interval.SelectedValue);
            var bins = (GenericControls.NumericUpDown)properties.NumberOfBins.InnerContent;
            Assert.AreEqual(20, bins.Value);
            bins.Value = 30;
            Assert.AreEqual(30, element.NumberOfBins);
            element.UndoManager.Undo();
            Assert.AreEqual(20, element.NumberOfBins);
            Assert.AreEqual(20, bins.Value);
            BivariateAnalysisViewTests.Select(properties.General_TabItem);

            // The input data: none is a valid choice, and the plot's observed data go.
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

            // The bivariate analysis last: another clears the results.
            BivariateAnalysis copula = element.BivariateAnalysis;
            var other = analysis.Items.Cast<BivariateAnalysis>().First(a => a != copula);
            analysis.SelectedItem = other;
            Assert.AreSame(other, element.BivariateAnalysis);
            Assert.IsFalse(element.IsEstimated);
            element.UndoManager.Undo();
            Settle();
            Assert.AreSame(copula, element.BivariateAnalysis);
            Assert.AreSame(copula, analysis.SelectedItem);
        }

        [STATestMethod]
        public void PreviewClicksAndAlternatives_OnTheFrequencyPlot()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(SumOfNormals);
            var element = Element<CoincidentFrequencyAnalysis>(project, NearZero);
            var view = Host(new CoincidentFrequencyControl { Element = element });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) => called.Add(plot);

            // The response grid's tab has no plot.
            Click(window, Centre(window, view.BivariateDataGrid));
            Assert.AreEqual((false, false, (Wpf.Plot)null), clicks.Last());

            Select(view, 1);
            Click(window, Centre(window, element.FrequencyPlot));
            Assert.AreEqual((true, false, element.FrequencyPlot), clicks.Last());
            Assert.IsTrue(view.PlotClicked);
            Point toolbar = HitPoint(window, view.FrequencyPlotToolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.FrequencyPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);
            Click(window, HitPoint(window, view.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton")));
            Assert.AreSame(element.FrequencyPlot, called.Last());

            // The label shows the alternatives: the project's other coincident frequency analyses only.
            Click(window, Centre(window, view.AlternativesTextBlock));
            Assert.IsTrue(view.AlternativeSelector.IsVisible);
            var others = project.ElementCollections.OfType<BivariateAnalysisCollection>().Single().OfType<CoincidentFrequencyAnalysis>().Where(e => e != element).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());

            // An alternative's horizontal interval, predictive and point curves, named by the series count, in the
            // first unused colour of upstream's list from the fifth on.
            var positive = Element<CoincidentFrequencyAnalysis>(project, Positive);
            var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative == positive);
            var plot = element.FrequencyPlot;
            int index = plot.Series.Count;
            item.IsChecked = true;
            Settle();
            var color = Avalonia.Media.Color.Parse(GenericControls.GeneralMethods.RandomColorsLongList[4]);
            var interval = (Wpf.AreaSeries)plot.Series.Single(s => s.Name == "CredibleIntervals_" + index);
            var point = (Wpf.LineSeries)plot.Series.Single(s => s.Name == "PosteriorMode_" + index);
            Assert.AreEqual(Positive + " - 90% Credible Intervals", interval.Title);
            Assert.AreEqual(Positive + " - Posterior Predictive", plot.Series.Single(s => s.Name == "PosteriorPredictive_" + index).Title);
            Assert.AreEqual(Positive + " - Posterior Mean", point.Title);
            Assert.AreEqual(Avalonia.Media.Color.FromArgb(100, color.R, color.G, color.B), interval.Fill);
            Assert.AreEqual(color, point.Color);
            Assert.AreEqual(("Y", "X", "Z", "X"), (interval.DataFieldX, interval.DataFieldY, interval.DataFieldX2, interval.DataFieldY2));
            var points = ((IEnumerable<DataPoint>)point.ItemsSource).ToList();
            Assert.AreEqual(positive.ZOutputValues.Length, points.Count);
            for (int k = 0; k < points.Count; k++)
                Assert.AreEqual(new DataPoint(positive.AnalysisResults.ModeCurve[k], positive.ZOutputValues[k]), points[k]);
            item.IsChecked = false;
            Settle();
            Assert.IsFalse(plot.Series.Contains(interval) || plot.Series.Contains(point));
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");
            Assert.IsFalse(element.IsDirty);
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(SumOfNormals);
            foreach (string name in new[] { NearZero, Negative, Positive })
            {
                var element = Element<CoincidentFrequencyAnalysis>(project, name);
                var changes = new List<string>();
                Watch(element, changes);
                Watch(element.BayesianAnalysis, changes);
                Watch(element.InputData, changes);
                Watch(element.BivariateAnalysis, changes);
                element.XValues.CollectionChanged += (sender, e) => changes.Add("XValues." + e.Action);
                element.YValues.CollectionChanged += (sender, e) => changes.Add("YValues." + e.Action);

                // Two documents of the element, the first still holding it when the second opens, as upstream's
                // MainProjectNode leaves a closed one: the second takes the plot.
                var views = new List<CoincidentFrequencyControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new CoincidentFrequencyControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.BivariateDataGrid.Items.Refresh();
                    Settle();
                    Select(view, 1);
                    view.ShowFilterToggleButton.IsChecked = true;
                    Settle();
                    Select(view, 2);
                    Select(view, 0);
                    if (document == 0) CompositeAnalysisViewTests.ChangeThemes();
                    CompositeAnalysisViewTests.CloseDocument(window);
                }
                Assert.AreSame(element.FrequencyPlot, views[1].FrequencyPlotHost.Content, name);

                // The properties panel, on each of its tabs, and in each theme.
                var properties = new CoincidentFrequencyPropertiesControl { Element = element };
                Window panel = Show(properties, 420, 1100);
                BivariateAnalysisViewTests.Select(properties.Output_TabItem);
                BivariateAnalysisViewTests.Select(properties.General_TabItem);
                CompositeAnalysisViewTests.ChangeThemes();
                CompositeAnalysisViewTests.CloseDocument(panel);
                properties.Element = null;

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                Assert.IsTrue(element.IsEstimated, name);
                foreach (var view in views) view.Element = null;
            }
        }

        // Edits a grid's cell as a user does: selects it, types the text into its editor and commits it.
        internal static void Edit(GenericControls.CopyPasteDataGrid grid, int row, int column, string text)
        {
            grid.CurrentColumn = grid.Columns[column];
            grid.SelectedItem = grid.Items[row];
            Assert.IsTrue(grid.BeginEdit(), "the cell edits");
            Settle();
            ((TextBox)grid.GetCell(row, column).Content).Text = text;
            Assert.IsTrue(grid.CommitEdit(), "the edit commits");
            Settle();
        }

        // Whether a response cell has upstream's error look (its background is the theme's error background).
        private static bool IsFlagged(GenericControls.CopyPasteDataGrid grid, int row, int column) =>
            Equals(grid.GetCell(row, column).Background, Application.Current.FindResource("DataGrid.Cell.Error.Background"));

        private static void Watch(object source, List<string> changes)
        {
            if (source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }

        // RMC-BestFit's native export of a plot: its series by name, with their titles and points. The exports were
        // made from factory-reset plots: their styling, and the annotations a user added, aren't compared.
        private static void AssertNativeSeries(Wpf.Plot plot, JsonElement capture, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var expectedSeries = capture.GetProperty("series").EnumerateArray().ToList();
            Assert.AreEqual(string.Join(",", expectedSeries.Select(s => s.GetProperty("name").GetString())),
                string.Join(",", plot.Series.Select(s => s.Name)), label + ": the series");
            foreach (JsonElement expected in expectedSeries)
            {
                string name = expected.GetProperty("name").GetString();
                var series = plot.Series.Single(s => s.Name == name);
                Assert.AreEqual(expected.GetProperty("Title").GetString(), series.Title, label + ": " + name + "'s title");
                AssertNativePoints(expected.GetProperty("points"), Points(series.InternalSeries, "ActualPoints"), label + ": " + name);
                if (expected.GetProperty("points2").GetArrayLength() > 0)
                    AssertNativePoints(expected.GetProperty("points2"), Points(series.InternalSeries, "ActualPoints2"), label + ": " + name + "'s second points");
            }
        }

        private static void AssertNativePoints(JsonElement expected, List<(double X, double Y)> actual, string label)
        {
            Assert.AreEqual(expected.GetArrayLength(), actual.Count, label + ": the number of points");
            for (int i = 0; i < actual.Count; i++)
            {
                AssertClose(Number(expected[i].GetProperty("X")), actual[i].X, $"{label} {i}.X");
                AssertClose(Number(expected[i].GetProperty("Y")), actual[i].Y, $"{label} {i}.Y");
            }
        }

        // A JSON number, or NaN for null (the exports write NaN as null).
        private static double Number(JsonElement value) => value.ValueKind == JsonValueKind.Number ? value.GetDouble() : double.NaN;

        /// <summary>Selects one of the view's tabs: Bivariate Response (0), Frequency Plot (1), Tabular Results (2).</summary>
        internal static void Select(CoincidentFrequencyControl view, int index) =>
            BivariateAnalysisViewTests.Select(((TabControl)((Panel)view.Content).Children[0]).Items.Cast<TabItem>().ElementAt(index));

        /// <summary>
        /// Checks a plot against a walkthrough's figure of it: each series by title, with its points. The interval is
        /// a horizontal band, at each response from the lower probability (its first points) to the upper (its
        /// second points).
        /// </summary>
        private static void AssertFigure(Wpf.Plot plot, JsonElement figure, string label)
        {
            ((IPlotModel)plot.ActualModel).Update(true);
            var figureSeries = figure.GetProperty("series").EnumerateArray().ToList();
            CollectionAssert.AreEquivalent(figureSeries.Select(s => s.GetProperty("name").GetString()).ToList(),
                plot.Series.Select(s => s.Title).ToList(), label + ": the series");
            foreach (JsonElement expected in figureSeries)
            {
                string title = expected.GetProperty("name").GetString();
                var series = plot.Series.Single(s => s.Title == title);
                bool band = expected.GetProperty("kind").GetString() == "band";
                AssertPoints(band ? expected.GetProperty("xLower") : expected.GetProperty("x"), expected.GetProperty("y"),
                    Points(series.InternalSeries, "ActualPoints"), label + ": " + title);
                if (band)
                    AssertPoints(expected.GetProperty("xUpper"), expected.GetProperty("y"), Points(series.InternalSeries, "ActualPoints2"), label + ": " + title + "'s upper points");
            }
        }

        private static void AssertPoints(JsonElement x, JsonElement y, List<(double X, double Y)> points, string label)
        {
            Assert.AreEqual(x.GetArrayLength(), points.Count, label + ": the number of points");
            for (int i = 0; i < points.Count; i++)
            {
                AssertClose(x[i].GetDouble(), points[i].X, $"{label} {i}.X");
                AssertClose(y[i].GetDouble(), points[i].Y, $"{label} {i}.Y");
            }
        }

        // The (X, Y) of a core series' points: data points or scatter points, from a property that may be protected.
        private static List<(double X, double Y)> Points(object series, string property)
        {
            PropertyInfo info = null;
            for (Type type = series.GetType(); type != null && info == null; type = type.BaseType)
                info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.IsNotNull(info, series.GetType().Name + " has no " + property);
            return ((IEnumerable)info.GetValue(series)).Cast<object>().Select(point => point switch
            {
                DataPoint p => (p.X, p.Y),
                OxyPlot.Series.ScatterPoint p => (p.X, p.Y),
                _ => throw new AssertFailedException("Unexpected point type " + point.GetType().Name),
            }).ToList();
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
    }
}
