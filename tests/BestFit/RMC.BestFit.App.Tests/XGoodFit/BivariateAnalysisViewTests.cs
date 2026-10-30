using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using FrameworkInterfaces;
using FrameworkUI;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Distributions.Copulas;
using OxyPlot;
using RMC.BestFit.Estimation;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat.Media.Media3D;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Bivariate Distribution Analysis view and its properties panel
    /// (GUI/BivariateAnalysis/Bivariate) on the bivariate example project's six copula fits.
    /// </summary>
    /// <remarks>
    /// The copula plot is compared with RMC-BestFit's native export of the AMH fit (the screenshot's capture
    /// data: the 10,000 points simulated from the saved seed and the paired observations), and with the
    /// walkthrough's figures, which record what the desktop computed (the probability-space scatter, the
    /// log-density and joint-exceedance contour grids). The tables show upstream's own results, in US formats
    /// (plan D8). Editing the properties writes to the element as upstream's bindings do, and the element's
    /// undo restores it; showing the views changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class BivariateAnalysisViewTests
    {
        internal const string Examples = "5-bivariate-distribution-analysis/1-bivariate-distributions/bivariate-distribution-examples.bestfit";
        internal static readonly string[] Copulas = { "AMH Copula", "Clayton Copula", "Frank Copula", "Gumbel Copula", "Joe Copula", "Normal Copula" };
        private const string Shots = "5-bivariate-distribution-analysis/1-bivariate-distributions/screenshots/amh-";
        private const string Figures = "5-bivariate-distribution-analysis/1-bivariate-distributions/images/bivariate-distribution-examples-";

        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

        private readonly List<Window> _windows = new List<Window>();
        private readonly List<BestFitProject> _projects = new List<BestFitProject>();

        /// <summary>Closes the windows and projects a test opened, and removes the application's resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Close();
            foreach (var project in _projects) Close(project);
            ReleaseAppResources();
        }

        [STATestMethod]
        public void CopulaPlot_DrawsTheSavedFits_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Examples);
            var amh = Element<BivariateAnalysis>(project, "AMH Copula");
            var view = Host(new BivariateAnalysisControl { Element = amh });
            Wpf.Plot plot = amh.CopulaPlot;
            Assert.AreSame(plot, view.CopulaPlotHost.Content);
            Assert.AreSame(plot, view.CopulaPlotToolbar.Plot);
            Assert.AreSame(plot, view.GetCurrentPlot());

            // Scatter Plot, Value axes: RMC-BestFit's own export of this view, point for point.
            JsonElement native = NativePlot(Shots + "copula-scatter.json.gz");
            AssertPlot(native, plot);
            foreach (JsonElement series in native.GetProperty("series").EnumerateArray())
            {
                var scatter = plot.Series.OfType<Wpf.ScatterPointSeries>().Single(s => s.Name == series.GetProperty("name").GetString());
                Assert.AreEqual(series.GetProperty("Title").GetString(), scatter.Title);
                JsonElement style = series.GetProperty("style");
                Assert.AreEqual(style.GetProperty("MarkerFill").GetString(), Hex(scatter.MarkerFill));
                Assert.AreEqual(style.GetProperty("MarkerStroke").GetString(), Hex(scatter.MarkerStroke));
                Assert.AreEqual(style.GetProperty("MarkerType").GetString(), scatter.MarkerType.ToString());
                Assert.AreEqual(style.GetProperty("MarkerSize").GetDouble(), scatter.MarkerSize);
                AssertPoints(series.GetProperty("points"), Points(scatter), scatter.Name);
            }
            AssertTrackerFormat(BivariateAnalysisControl.CreateObservedCopulaTrackerFormat(false), Scatter(plot, "ExactData"));
            AssertTrackerFormat(BivariateAnalysisControl.CreateCopulaScatterTrackerFormat(false), Scatter(plot, "CopulaData"));

            // Probability axes: the simulated points' marginal probabilities and the observations' plotting
            // positions, as the walkthrough's figure of the desktop's coordinates.
            view.AxisTypeComboBox.SelectedIndex = 1;
            Settle();
            JsonElement figure = NativePlot(Figures + "amh-cdf.plotspec.json.gz");
            AssertFigureAxes(figure, plot, "N2");
            AssertPoints(figure.GetProperty("series")[0], Points(Scatter(plot, "CopulaData")), "simulated");
            AssertPoints(figure.GetProperty("series")[1], Points(Scatter(plot, "ExactData")), "observed");
            AssertTrackerFormat(BivariateAnalysisControl.CreateObservedCopulaTrackerFormat(true), Scatter(plot, "ExactData"));

            // Density and probability contours of the Normal copula fit, over the desktop's 100 x 100 grid.
            var normal = Element<BivariateAnalysis>(project, "Normal Copula");
            var normalView = Host(new BivariateAnalysisControl { Element = normal });
            normalView.PlotTypeComboBox.SelectedIndex = 1;
            Settle();
            AssertContours(NativePlot(Figures + "normal-density.plotspec.json.gz"), normal.CopulaPlot, racyMaximum: true);
            normalView.PlotTypeComboBox.SelectedIndex = 2;
            Settle();
            AssertContours(NativePlot(Figures + "normal-joint-exceedance.plotspec.json.gz"), normal.CopulaPlot, racyMaximum: false);
            var contours = normal.CopulaPlot.Series.OfType<Wpf.ContourSeries>().Single();
            Assert.AreEqual("Contours", contours.Title);
            Assert.AreEqual("G3", contours.LabelFormatString);
            Assert.AreEqual(Color.FromArgb(200, 255, 255, 255), contours.LabelBackground, "the plot area's white, as the label background");
            AssertTrackerFormat(BivariateAnalysisControl.CreateContourTrackerFormat(false), contours);
        }

        [STATestMethod]
        public void CopulaPlot_FollowsTheMarginalsDataLabels_AndKeepsAUsersTitle()
        {
            UseAppResources(ThemeColor.Light);
            var amh = Element<BivariateAnalysis>(OpenProject(Examples), "AMH Copula");
            var view = Host(new BivariateAnalysisControl { Element = amh });
            var x = amh.CopulaPlot.Axes.Single(a => a.Key == "Xaxis");
            var y = amh.CopulaPlot.Axes.Single(a => a.Key == "Yaxis");
            Assert.AreEqual("Marginal X - Value", x.Title);
            Assert.AreEqual("Marginal Y - Value", y.Title);

            // As upstream's binding: the titles follow the marginals' input data labels.
            amh.MarginalY.InputData.UnitLabel = "Stage (ft)";
            Assert.AreEqual("Marginal Y - Stage (ft)", y.Title);

            // The probability axes have their own titles, and the value axes' come back with the labels'.
            view.AxisTypeComboBox.SelectedIndex = 1;
            Settle();
            Assert.AreEqual("Marginal X - Probability", x.Title);
            Assert.AreEqual("Marginal Y - Probability", y.Title);
            view.AxisTypeComboBox.SelectedIndex = 0;
            Settle();
            Assert.AreEqual("Marginal Y - Stage (ft)", y.Title);

            // A title the user typed stays, whatever the label (PlotAxisTitleDefaults).
            x.Title = "Flow at the gauge";
            amh.MarginalX.InputData.UnitLabel = "Flow (cfs)";
            Assert.AreEqual("Flow at the gauge", x.Title);
        }

        [STATestMethod]
        public void DiagnosticTabs_ShowTheAnalysisPosterior_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            var amh = Element<BivariateAnalysis>(OpenProject(Examples), "AMH Copula");
            BayesianAnalysis analysis = amh.BayesianAnalysis;
            BayesianController plots = amh.BayesianPlots;
            var view = Host(new BivariateAnalysisControl { Element = amh });

            // Each diagnostic control has the element's plot and analysis, and is the current plot on its tab.
            var tabs = new (TabItem Tab, Wpf.Plot Plot, Func<Wpf.Plot> Shown)[]
            {
                (view.KernelDensityTabItem, plots.KernelDensityPlot, () => view.KernelDensityControl.Plot),
                (view.HistogramTabItem, plots.HistogramPlot, () => view.HistogramControl.Plot),
                (view.MeanLikelihoodTabItem, plots.MeanLikelihoodPlot, () => view.MeanLikelihoodControl.Plot),
                (view.AutocorrelationTabItem, plots.AutocorrelationPlot, () => view.AutocorrelationControl.Plot),
                (view.MarkovChainTabItem, plots.MarkovChainTracePlot, () => view.MarkovChainTraceControl.Plot),
                (view.InfluenceDiagnosticsTabItem, plots.InfluenceDiagnosticsPlot, () => view.InfluenceDiagnosticsControl.Plot),
            };
            // Upstream asks the inner X-Y Plot tab first, which stays selected under the other outer tabs.
            Select(view.KernelDensityTabItem);
            Assert.AreSame(amh.CopulaPlot, view.GetCurrentPlot(), "upstream's order of tests");
            Select(InnerTab(view, 1));
            foreach (var (tab, expected, shown) in tabs)
            {
                Select(tab);
                Assert.AreSame(expected, shown(), (string)tab.Header);
                Assert.AreSame(expected, view.GetCurrentPlot(), (string)tab.Header);
            }
            Assert.AreSame(analysis, view.ParameterSetsControl.Analysis);
            Assert.AreSame(analysis, view.MCMCReportControl.Analysis);
            Assert.AreSame(analysis, view.KernelDensityControl.Analysis);

            // Kernel density of the dependency parameter: RMC-BestFit's export of this view.
            JsonElement native = NativePlot(Shots + "dependence-kde.json.gz");
            AssertPlot(native, plots.KernelDensityPlot);
            var points = ((IEnumerable<Point3D>)plots.KernelDensityPlot.Series.OfType<Wpf.AreaSeries>().Single().ItemsSource).ToList();
            JsonElement density = native.GetProperty("series")[0].GetProperty("points2");
            Assert.AreEqual(density.GetArrayLength(), points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Assert.AreEqual(density[i].GetProperty("X").GetDouble(), points[i].X, Tolerance(points[i].X));
                Assert.AreEqual(density[i].GetProperty("Y").GetDouble(), points[i].Z, Tolerance(points[i].Z));
            }

            // Its Markov chains, in upstream's seeded colours.
            native = NativePlot(Shots + "dependence-trace.json.gz");
            AssertPlot(native, plots.MarkovChainTracePlot);
            var chains = plots.MarkovChainTracePlot.Series.OfType<Wpf.LineSeries>().ToList();
            Assert.AreEqual(native.GetProperty("series").GetArrayLength(), chains.Count);
            for (int c = 0; c < chains.Count; c++)
            {
                JsonElement chain = native.GetProperty("series")[c];
                Assert.AreEqual(chain.GetProperty("Title").GetString(), chains[c].Title);
                Assert.AreEqual(chain.GetProperty("style").GetProperty("Color").GetString(), Hex(chains[c].Color));
                var data = (DataPoint[])chains[c].ItemsSource;
                JsonElement nativePoints = chain.GetProperty("points");
                Assert.AreEqual(nativePoints.GetArrayLength(), data.Length);
                for (int i = 0; i < data.Length; i++)
                    Assert.AreEqual(nativePoints[i].GetProperty("Y").GetDouble(), data[i].Y, Tolerance(data[i].Y), $"chain {c}, point {i}");
            }
        }

        [STATestMethod]
        public void ResultsTables_ShowTheJointResultsAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var amh = Element<BivariateAnalysis>(OpenProject(Examples), "AMH Copula");
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                // The application's default settings (two decimal places), on a machine whose culture writes
                // decimal commas: the tables keep WPF's binding culture, en-US.
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new BivariateAnalysisControl { Element = amh });
                Select(InnerTab(view, 1));

                // One row per X-Y ordinate: the ordinate, then the joint results in scientific notation.
                var results = amh.AnalysisResults;
                var table = view.FrequencyCurveTable;
                string value = "{0:" + UserSettings.ValueStringFormat + "}";
                Assert.AreEqual(amh.XYOrdinates.Count, table.Items.Count);
                for (int i = 0; i < amh.XYOrdinates.Count; i++)
                {
                    string[] expected =
                    {
                        string.Format(Us, value, amh.XYOrdinates[i].X), string.Format(Us, value, amh.XYOrdinates[i].Y.Mean),
                        results.ConfidenceIntervals[i, 1].ToString("E4", Us), results.ConfidenceIntervals[i, 0].ToString("E4", Us),
                        results.MeanCurve[i].ToString("E4", Us), results.ModeCurve[i].ToString("E4", Us),
                    };
                    for (int column = 0; column < expected.Length; column++)
                        Assert.AreEqual(expected[column], CellText(table, i, column), $"row {i}, column {column}");
                }
                // The headers are upstream's: the interval's percentages in the code's (machine) culture, and the
                // point estimator.
                double alpha = (1 - amh.BayesianAnalysis.CredibleIntervalWidth) / 2;
                Assert.AreEqual(((1 - alpha) * 100).ToString("F1") + "% CI", table.Columns[2].Header);
                Assert.AreEqual((alpha * 100).ToString("F1") + "% CI", table.Columns[3].Header);
                Assert.AreEqual(BayesianAnalysis.PointEstimateType.PosteriorMean, amh.BayesianAnalysis.PointEstimator);
                Assert.AreEqual("Posterior Mean", table.Columns[5].Header);

                // The summary statistics: the copula's parameter, then the fit's criteria, with a rule above AIC.
                var statistics = view.SummaryStatisticsTable;
                var parameters = amh.BivariateDistribution.Parameters;
                var rows = parameters.Select(p => (p.DisplayName, p.Value))
                    .Concat(new[]
                    {
                        ("AIC", results.AIC), ("BIC", results.BIC), ("DIC", results.DIC),
                        ("WAIC", amh.BayesianAnalysis.WAIC), ("LOO-CV", amh.BayesianAnalysis.LOOIC), ("RMSE", results.RMSE),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Posterior Mean", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(double.IsNaN(rows[i].Item2) ? "N/A" : rows[i].Item2.ToString("F4", Us), CellText(statistics, i, 1), rows[i].Item1);
                    DataGridRow row = statistics.GetRow(i);
                    Assert.AreEqual(rows[i].Item1 == "AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), row.BorderThickness, rows[i].Item1);
                }
                Assert.AreEqual(Application.Current.FindResource("EnvironmentWindowText"), statistics.GetRow(parameters.Count).BorderBrush);
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
            var project = OpenProject(Examples);
            var amh = Element<BivariateAnalysis>(project, "AMH Copula");
            var properties = new BivariateAnalysisPropertiesControl { Element = amh };
            Host(properties, 420, 1100);

            // General: the element's name and dates, its marginals, copula and method.
            Assert.AreEqual("AMH Copula", properties.ElementName.Text);
            Assert.AreEqual(amh.CreationDate.ToString(Us), properties.CreationDate.Text);
            ComboBox marginalX = MarginalBox(properties.MarginalXComboBox);
            ComboBox marginalY = MarginalBox(properties.MarginalYComboBox);
            Assert.AreSame(amh.MarginalX, marginalX.SelectedItem);
            Assert.AreSame(amh.MarginalY, marginalY.SelectedItem);
            Assert.AreEqual("AMH - Marginal X", ((IElement)marginalX.SelectedItem).Name);
            // The project's univariate analyses that have a marginal model, sorted by name.
            var univariates = Elements(project).OfType<IUnivariate>().Where(u => u.GetMarginalModel() != null).ToList();
            var listed = marginalX.Items.Cast<IUnivariate>().ToList();
            CollectionAssert.AreEquivalent(univariates, listed);
            CollectionAssert.AreEqual(listed.Select(u => u.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(u => u.Name).ToList());
            // A valid marginal loses the red border and the tool tip that asks for one.
            Assert.AreEqual(new Thickness(0), ((Border)properties.MarginalXComboBox.InnerContent).BorderThickness);
            Assert.IsNull(ToolTip.GetTip(properties.MarginalXComboBox));
            var copula = (ComboBox)properties.Copula.InnerContent;
            var method = (ComboBox)properties.EstimationMethod.InnerContent;
            Assert.AreEqual("Ali-Mikhail-Haq", ((CopulaItem)copula.SelectedItem).DisplayName);
            Assert.AreEqual(CopulaEstimationMethod.InferenceFromMargins, method.SelectedValue);
            Assert.AreSame(amh.BivariateDistribution, properties.ParameterPriorsControl.Model);

            // Output: the X-Y ordinates, whose value column is upstream's "Y", in the user's value format.
            Select(properties.Output_TabItem);
            var ordinates = properties.OrderedDataGridControl;
            Assert.AreSame(amh.XYOrdinates, ordinates.UncertainOrderedData);
            var columns = ordinates.FindControl<DataGrid>("ValidationGrid").Columns;
            Assert.AreEqual("X", columns[0].Header);
            Assert.AreEqual("Y", columns.OfType<DataGridTextColumn>().Single(c => ((Avalonia.Data.ReflectionBinding)c.Binding).Path == "P1").Header);
            Assert.AreEqual("{0:" + UserSettings.ValueStringFormat + "}", ((Avalonia.Data.ReflectionBinding)((DataGridTextColumn)columns[0]).Binding).StringFormat);
            Select(properties.General_TabItem);

            // Each edit writes to the element, and the element's undo restores it.
            copula.SelectedItem = properties.CopulaList.Single(item => item.Value == CopulaType.Frank);
            Assert.AreEqual(CopulaType.Frank, amh.BivariateDistribution.CopulaType);
            amh.UndoManager.Undo();
            Assert.AreEqual(CopulaType.AliMikhailHaq, amh.BivariateDistribution.CopulaType);
            Assert.AreEqual(CopulaType.AliMikhailHaq, copula.SelectedValue, "the restored copula shows");
            // As upstream: the undo rebuilds the analysis, its ordinates too, without saying so, and the ordinates
            // editor keeps the old ones.
            Assert.AreNotSame(amh.XYOrdinates, ordinates.UncertainOrderedData);

            method.SelectedItem = properties.MethodList.Single(item => item.Value == CopulaEstimationMethod.PseudoLikelihood);
            Assert.AreEqual(CopulaEstimationMethod.PseudoLikelihood, amh.BivariateDistribution.CopulaEstimationMethod);
            amh.UndoManager.Undo();
            Assert.AreEqual(CopulaEstimationMethod.InferenceFromMargins, amh.BivariateDistribution.CopulaEstimationMethod);
            Assert.AreEqual(CopulaEstimationMethod.InferenceFromMargins, method.SelectedValue);

            properties.Description.Text = "Paired synthetic data";
            Assert.AreEqual("Paired synthetic data", amh.Description);
            amh.UndoManager.Undo();
            Assert.AreNotEqual("Paired synthetic data", amh.Description);

            // Options: the sampler's settings, as the Bayesian options control edits them.
            Select(properties.Options_TabItem);
            Assert.AreSame(amh.BayesianAnalysis, properties.BayesianOptionsControl.Analysis);
            properties.BayesianOptionsControl.UseSimulationDefaults.IsSelected = false;
            int chains = amh.BayesianAnalysis.NumberOfChains;
            properties.BayesianOptionsControl.NumberOfChains.Number = chains + 2;
            Assert.AreEqual(chains + 2, amh.BayesianAnalysis.NumberOfChains);
            amh.UndoManager.Undo();
            Assert.AreEqual(chains, amh.BayesianAnalysis.NumberOfChains);

            // Output: the credible interval.
            Select(properties.Output_TabItem);
            Assert.AreSame(amh.BayesianAnalysis, properties.BayesianOutputControl.Analysis);
            var interval = (ComboBox)properties.BayesianOutputControl.CredibleInterval.InnerContent;
            interval.SelectedItem = properties.BayesianOutputControl.CredibleIntervalItems.Single(item => item.Value == 0.99);
            Assert.AreEqual(0.99, amh.BayesianAnalysis.CredibleIntervalWidth);
            amh.UndoManager.Undo();
            Assert.AreEqual(0.9, amh.BayesianAnalysis.CredibleIntervalWidth);

            // The marginal last: changing it clears the fit's results, as upstream's element does.
            Select(properties.General_TabItem);
            IUnivariate original = amh.MarginalX;
            IUnivariate other = listed.Single(u => u.Name == "Clayton - Marginal X");
            marginalX.SelectedItem = other;
            Assert.AreSame(other, amh.MarginalX);
            Assert.IsFalse(amh.IsEstimated);
            amh.UndoManager.Undo();
            Assert.AreSame(original, amh.MarginalX);
            Assert.AreSame(original, marginalX.SelectedItem);

            // Renamed, the selected marginal sorts to the end of the list and stays selected, as in WPF.
            ((IElement)original).Name = "ZZ - Marginal X";
            Settle();
            Assert.AreSame(original, marginalX.Items.Cast<object>().Last());
            Assert.AreSame(original, marginalX.SelectedItem);
            Assert.AreSame(original, amh.MarginalX);
        }

        [STATestMethod]
        public void PreviewClicks_TellTheShellWhetherThePlotOrToolbarWasClicked()
        {
            UseAppResources(ThemeColor.Light);
            var amh = Element<BivariateAnalysis>(OpenProject(Examples), "AMH Copula");
            var view = Host(new BivariateAnalysisControl { Element = amh });
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            view.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));
            var called = new List<Wpf.Plot>();
            var calls = new List<string>();
            view.PlotPropertiesCalled += (plot, open, expander, selected) =>
            {
                called.Add(plot);
                calls.Add($"{plot.Title} {open} {expander}");
            };

            Click(window, Centre(window, amh.CopulaPlot));
            Assert.AreEqual((true, false, amh.CopulaPlot), clicks.Single());
            Assert.IsTrue(view.PlotClicked);
            // The click selected a series, whose properties the toolbar offers the shell (open only if they are).
            Assert.AreSame(amh.CopulaPlot, called.Single());
            StringAssert.Contains(calls.Single(), "Copula False Series_General");
            called.Clear();
            calls.Clear();

            Point toolbar = HitPoint(window, view.CopulaPlotToolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, amh.CopulaPlot), clicks.Last());
            Assert.IsFalse(view.PlotClicked);

            Click(window, Centre(window, view.AxisTypeComboBox));
            Assert.AreEqual((false, false, amh.CopulaPlot), clicks.Last());
            view.AxisTypeComboBox.IsDropDownOpen = false;

            // A diagnostic plot's toolbar asks for its plot's properties through the view.
            Select(InnerTab(view, 1));
            Select(view.HistogramTabItem);
            var properties = view.HistogramControl.PlotToolbar.FindControl<Button>("PropertiesButton");
            Click(window, HitPoint(window, properties));
            Assert.AreSame(amh.BayesianPlots.HistogramPlot, called.Single());
            StringAssert.StartsWith(calls.Single(), amh.BayesianPlots.HistogramPlot.Title + " True");
        }

        [STATestMethod]
        public void ShowingTheViews_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Examples);
            foreach (string name in Copulas)
            {
                var element = Element<BivariateAnalysis>(project, name);
                var changes = new List<string>();
                Watch(element, changes);
                Watch(element.BivariateDistribution, changes);
                foreach (var parameter in element.BivariateDistribution.Parameters) Watch(parameter, changes);
                Watch(element.BayesianAnalysis, changes);
                Watch(element.MarginalX, changes);
                Watch(element.MarginalY, changes);
                element.XYOrdinates.CollectionChanged += (sender, e) => changes.Add("XYOrdinates." + e.Action);

                // The view, on each of its tabs and plot settings.
                var view = new BivariateAnalysisControl { Element = element };
                Window window = Show(view, 1300, 850);
                foreach (int axis in new[] { 1, 0 })
                {
                    view.AxisTypeComboBox.SelectedIndex = axis;
                    foreach (int type in new[] { 1, 2, 0 })
                    {
                        view.PlotTypeComboBox.SelectedIndex = type;
                        Settle();
                    }
                }
                Select(InnerTab(view, 1));
                foreach (TabItem tab in ((TabControl)view.DistributionResultsTabItem.Parent).Items.Cast<TabItem>())
                    Select(tab);
                Select(view.DistributionResultsTabItem);
                Select(view.CopulaResultsTabItem);
                CloseDocument(window);

                // The document opened again while the closed view still holds the plot, as upstream's
                // MainProjectNode leaves it: the new view takes the plot.
                var again = new BivariateAnalysisControl { Element = element };
                window = Show(again, 1300, 850);
                Assert.AreSame(element.CopulaPlot, again.CopulaPlotHost.Content, name);
                CloseDocument(window);

                // The properties panel, on each of its tabs.
                var properties = new BivariateAnalysisPropertiesControl { Element = element };
                window = Show(properties, 420, 1100);
                Select(properties.Options_TabItem);
                Select(properties.Output_TabItem);
                Select(properties.General_TabItem);
                CloseDocument(window);
                properties.Element = null;

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                Assert.IsTrue(element.IsEstimated, name);
            }
        }

        // Takes the content out of its window, as the shell does when a document closes, then closes the window:
        // the test application's own window theme would otherwise give the plots on hidden tabs a new font as the
        // window closes.
        private static void CloseDocument(Window window)
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

        /// <summary>Selects a tab, as a click on its header does, and lets the view update.</summary>
        internal static void Select(TabItem tab)
        {
            ((TabControl)tab.Parent).SelectedItem = tab;
            Settle();
        }

        /// <summary>The Distribution Results tab's inner tabs: X-Y Plot (0) and Tabular Results (1).</summary>
        internal static TabItem InnerTab(BivariateAnalysisControl view, int index) =>
            ((TabControl)view.CopulaResultsTabItem.Parent).Items.Cast<TabItem>().ElementAt(index);

        private static ComboBox MarginalBox(ContentPropertyControl property) => (ComboBox)((Border)property.InnerContent).Child;

        // Upstream builds the formats with Environment.NewLine, and a series keeps a saved format that differs only
        // in its line breaks (OxyPlot.Wpf's Series), so off Windows a series read from a project keeps its CRLFs.
        private static void AssertTrackerFormat(string expected, Wpf.Series series) =>
            Assert.AreEqual(expected.Replace("\r\n", "\n"), series.TrackerFormatString?.Replace("\r\n", "\n"), series.Name);

        private static Wpf.ScatterPointSeries Scatter(Wpf.Plot plot, string name) =>
            plot.Series.OfType<Wpf.ScatterPointSeries>().Single(s => s.Name == name);

        // The points a scatter series shows: its items through its mapping.
        private static List<DataPoint> Points(Wpf.ScatterPointSeries series) =>
            series.ItemsSource.Cast<object>().Select(item => series.Mapping(item)).Select(p => new DataPoint(p.X, p.Y)).ToList();

        // The native points, as the capture data's {X, Y} objects or the figure's x and y lists.
        private static void AssertPoints(JsonElement native, List<DataPoint> points, string what)
        {
            if (native.ValueKind == JsonValueKind.Array)
            {
                Assert.AreEqual(native.GetArrayLength(), points.Count, what);
                for (int i = 0; i < points.Count; i++)
                {
                    Assert.AreEqual(native[i].GetProperty("X").GetDouble(), points[i].X, Tolerance(points[i].X), $"{what} {i}");
                    Assert.AreEqual(native[i].GetProperty("Y").GetDouble(), points[i].Y, Tolerance(points[i].Y), $"{what} {i}");
                }
                return;
            }
            JsonElement x = native.GetProperty("x"), y = native.GetProperty("y");
            Assert.AreEqual(x.GetArrayLength(), points.Count, what);
            for (int i = 0; i < points.Count; i++)
            {
                Assert.AreEqual(x[i].GetDouble(), points[i].X, Tolerance(points[i].X), $"{what} {i}");
                Assert.AreEqual(y[i].GetDouble(), points[i].Y, Tolerance(points[i].Y), $"{what} {i}");
            }
        }

        // The contour series' grid, values and levels, and the observations over them, as the figure's.
        // racyMaximum: the levels run from a round lower level to the grid's largest value, which upstream's
        // parallel loop finds with an unsynchronised Math.Max, so a run can keep a smaller one: the figure's run
        // (and most of the port's) found the largest.
        private static void AssertContours(JsonElement figure, Wpf.Plot plot, bool racyMaximum)
        {
            AssertFigureAxes(figure, plot, "N0");
            var contours = plot.Series.OfType<Wpf.ContourSeries>().Single();
            JsonElement series = figure.GetProperty("series")[0];
            double[] x = series.GetProperty("x").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            double[] y = series.GetProperty("y").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            Assert.AreEqual(x.Length, contours.ColumnCoordinates.Length);
            Assert.AreEqual(y.Length, contours.RowCoordinates.Length);
            for (int i = 0; i < x.Length; i++) Assert.AreEqual(x[i], contours.ColumnCoordinates[i], Tolerance(x[i]));
            for (int j = 0; j < y.Length; j++) Assert.AreEqual(y[j], contours.RowCoordinates[j], Tolerance(y[j]));
            // The figure's z is by row (y), then column (x): matplotlib's order.
            JsonElement z = series.GetProperty("z");
            for (int j = 0; j < y.Length; j++)
                for (int i = 0; i < x.Length; i++)
                    Assert.AreEqual(z[j][i].GetDouble(), contours.Data[i, j], Tolerance(contours.Data[i, j]), $"z at {i}, {j}");
            double[] levels = series.GetProperty("style").GetProperty("levels").EnumerateArray().Select(v => v.GetDouble()).ToArray();
            Assert.AreEqual(levels.Length, contours.ContourLevels.Length);
            double[] actual = contours.ContourLevels;
            double largest = contours.Data.Cast<double>().Max();
            if (racyMaximum && Math.Abs(actual[^1] - largest) > Tolerance(largest))
            {
                Assert.AreEqual(levels[0], actual[0], Tolerance(levels[0]), "the lowest level");
                Assert.IsTrue(actual[^1] < largest && contours.Data.Cast<double>().Any(v => Math.Abs(v - actual[^1]) <= Tolerance(v)), "the race kept a smaller value of the grid");
                for (int k = 1; k < actual.Length; k++)
                    Assert.AreEqual(actual[0] + k * (actual[^1] - actual[0]) / (actual.Length - 1), actual[k], Tolerance(actual[k]), $"level {k}");
            }
            else
            {
                for (int k = 0; k < levels.Length; k++) Assert.AreEqual(levels[k], actual[k], Tolerance(levels[k]), $"level {k}");
            }
            AssertPoints(figure.GetProperty("series")[1], Points(Scatter(plot, "ExactData")), "observed");
        }

        // The figure's axis labels and ranges (the plot's actual ones), and upstream's tick format.
        private static void AssertFigureAxes(JsonElement figure, Wpf.Plot plot, string format)
        {
            foreach (var (name, key) in new[] { ("x", "Xaxis"), ("y", "Yaxis") })
            {
                JsonElement axis = figure.GetProperty("axes").GetProperty(name);
                var actual = plot.ActualModel.Axes.Single(a => a.Key == key);
                double minimum = axis.GetProperty("minimum").GetDouble(), maximum = axis.GetProperty("maximum").GetDouble();
                Assert.AreEqual(axis.GetProperty("label").GetString(), actual.Title, key);
                Assert.AreEqual(minimum, actual.ActualMinimum, (maximum - minimum) * 1E-9, key);
                Assert.AreEqual(maximum, actual.ActualMaximum, (maximum - minimum) * 1E-9, key);
                Assert.AreEqual(format, plot.Axes.Single(a => a.Key == key).StringFormat, key);
            }
        }

        // The plot's title, and its axes' titles, formats and ranges, as RMC-BestFit's export recorded them.
        internal static void AssertPlot(JsonElement native, Wpf.Plot plot)
        {
            Assert.AreEqual(native.GetProperty("title").GetString(), plot.Title);
            foreach (JsonElement axis in native.GetProperty("axes").EnumerateArray())
            {
                string position = axis.GetProperty("position").GetString();
                var actual = plot.ActualModel.Axes.Single(a => a.Position.ToString() == position);
                double minimum = axis.GetProperty("ActualMinimum").GetDouble();
                double maximum = axis.GetProperty("ActualMaximum").GetDouble();
                double tolerance = (maximum - minimum) * 1E-9;
                Assert.AreEqual(axis.GetProperty("Title").GetString(), actual.Title, position);
                if (axis.TryGetProperty("StringFormat", out JsonElement format) && format.ValueKind == JsonValueKind.String)
                    Assert.AreEqual(format.GetString(), actual.StringFormat, position);
                Assert.AreEqual(minimum, actual.ActualMinimum, tolerance, position);
                Assert.AreEqual(maximum, actual.ActualMaximum, tolerance, position);
            }
        }

        // Doubles the desktop wrote with the same .NET code: equal but for the last bits of another OS's maths.
        private static double Tolerance(double value) => Math.Max(1E-12, Math.Abs(value) * 1E-9);

        private static void Watch(object source, List<string> changes)
        {
            if (source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }

        private static string Hex(Color color) => $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";

        private static string CellText(GenericControls.CopyPasteDataGrid grid, int row, int column) =>
            (grid.GetCell(row, column)?.Content as TextBlock)?.Text;
    }
}
