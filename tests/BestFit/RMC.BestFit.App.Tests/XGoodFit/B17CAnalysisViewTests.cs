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
using Numerics.Distributions;
using RMC.BestFit.Estimation;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;
using ModelAnalyses = RMC.BestFit.Analyses;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Bulletin 17C Analysis view and its properties panel (GUI/UnivariateAnalysis/B17C) on
    /// the Bulletin 17C examples' GMM fits.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports of the examples (the frequency plots and the
    /// parameters' kernel densities) and with the walkthroughs' frequency figures, which record what the desktop
    /// drew. The tables show upstream's own results, in US formats (plan D8). Editing the properties writes to the
    /// element, and the element's undo restores it; showing the views changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class B17CAnalysisViewTests
    {
        private const string Examples = UnivariateAnalysisTesting.Folder + "2-bulletin-17C-analysis/";
        internal const string Bulletin17C = Examples + "1-bulletin17C-examples/bulletin-17c-examples.bestfit";
        internal const string Blakely = Examples + "2-information-expansion/blakely-mountain-dam-b17c.bestfit";
        internal const string Sinnemahoning = Examples + "3-measurement-errors/sinnemahoning-move3-b17c.bestfit";

        private static readonly CultureInfo Us = CultureInfo.GetCultureInfo("en-US");

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
            // ReleaseAppResources collects the closed views; the lists let go of them (MSTest keeps the instance).
            ReleaseAppResources();
        }

        /// <summary>
        /// Each of an example's fits that RMC-BestFit exported or drew for its walkthrough: the view shows what the
        /// desktop did, point for point.
        /// </summary>
        [STATestMethod]
        [DataRow(Bulletin17C)]
        [DataRow(Blakely)]
        [DataRow(Sinnemahoning)]
        public void Plots_EqualRmcBestFitsExports_AndTheWalkthroughsFigures(string example)
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(example);
            var exports = NativeExports(example).ToList();
            var figures = Figures(example).Where(f => f.Figure.GetProperty("plotId").GetString() == "b17c.frequency").ToList();
            var names = exports.Select(e => e.Capture.GetProperty("element").GetString())
                .Concat(figures.Select(f => f.Figure.GetProperty("source").GetProperty("id").GetString())).Distinct().ToList();
            int compared = 0;
            foreach (string name in names)
            {
                var element = Element<B17CAnalysis>(project, name);
                var view = new B17CAnalysisControl { Element = element };
                Window window = Show(view, 1300, 850);
                try
                {
                    Assert.AreSame(element.FrequencyPlot, view.FrequencyPlotHost.Content);
                    Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot());
                    foreach (var (file, figure) in figures.Where(f => f.Figure.GetProperty("source").GetProperty("id").GetString() == name))
                    {
                        AssertFigure(element.FrequencyPlot, figure, file);
                        compared++;
                    }
                    foreach (var (file, capture) in exports.Where(e => e.Capture.GetProperty("element").GetString() == name))
                    {
                        Wpf.Plot plot;
                        switch (capture.GetProperty("plotId").GetString())
                        {
                            case "b17c.frequency":
                                plot = element.FrequencyPlot;
                                break;
                            case "shared_diagnostics.kde":
                                Select(view.KernelDensityTabItem);
                                plot = view.KernelDensityControl.Plot;
                                // The density of the GMM's parameter ensemble, not a posterior (SimpleView). The export
                                // relabels its series "Sampling Density"; RMC-BestFit 2.0.1 shows upstream's "Posterior Density".
                                Assert.AreEqual(capture.GetProperty("title").GetString(), plot.Title, file);
                                Assert.AreEqual("Posterior Density", plot.Series.Single(s => s.Name == "PosteriorDensity").Title, file);
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
                }
            }
            Assert.AreEqual(exports.Count + figures.Count, compared);
        }

        [STATestMethod]
        public void ResultsTables_ShowTheFrequencyCurveAndStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<B17CAnalysis>(OpenProject(Bulletin17C), "Example #1");
            CultureInfo culture = CultureInfo.CurrentCulture;
            string valueFormat = UserSettings.ValueStringFormat;
            try
            {
                UserSettings.DefaultValueDigits = 2;
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
                var view = Host(new B17CAnalysisControl { Element = element });
                Select(((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Single(t => (string)t.Header == "Tabular Results"));

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
                UnivariateAnalysisViewTests.AssertIntervalHeaders(table, element.BayesianAnalysis.CredibleIntervalWidth);
                // Upstream's B17C names: the expected probability curve and the computed (or mean) parameters' curve.
                Assert.AreEqual("Expected Probability", table.Columns[3].Header);
                Assert.AreEqual(BayesianAnalysis.PointEstimateType.PosteriorMode, element.BayesianAnalysis.PointEstimator);
                Assert.AreEqual("Computed", table.Columns[4].Header);

                // The parameters, the distribution's moments and the fit's pseudo criteria, with rules above
                // Minimum and Pseudo AIC.
                var statistics = view.SummaryStatisticsTable;
                var distribution = element.Bulletin17CDistribution;
                var rows = distribution.Parameters.Select(p => (p.DisplayName, p.Value))
                    .Concat(new[]
                    {
                        ("Minimum", distribution.Distribution.Minimum < -1E12 ? double.NegativeInfinity : distribution.Distribution.Minimum),
                        ("Maximum", distribution.Distribution.Maximum > 1E12 ? double.PositiveInfinity : distribution.Distribution.Maximum),
                        ("Mean", distribution.Distribution.Mean), ("Std Dev", distribution.Distribution.StandardDeviation),
                        ("Skewness", distribution.Distribution.Skewness), ("Kurtosis", distribution.Distribution.Kurtosis),
                        ("Pseudo AIC", results.AIC), ("Pseudo BIC", results.BIC), ("RMSE", results.RMSE),
                    }).ToList();
                Assert.AreEqual(rows.Count, statistics.Items.Count);
                Assert.AreEqual("Computed", statistics.Columns[1].Header);
                for (int i = 0; i < rows.Count; i++)
                {
                    Assert.AreEqual(rows[i].Item1, CellText(statistics, i, 0));
                    Assert.AreEqual(UnivariateAnalysisViewTests.Statistic(rows[i].Item2), CellText(statistics, i, 1), rows[i].Item1);
                    Assert.AreEqual(rows[i].Item1 is "Minimum" or "Pseudo AIC" ? new Thickness(0, 2, 0, 0) : new Thickness(0), statistics.GetRow(i).BorderThickness, rows[i].Item1);
                }
            }
            finally
            {
                CultureInfo.CurrentCulture = culture;
                UserSettings.ValueStringFormat = valueFormat;
            }
        }

        [STATestMethod]
        public void DiagnosticTabs_ShowTheParameterEnsemble_AndTheGmm()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<B17CAnalysis>(OpenProject(Bulletin17C), "Example #1");
            var view = Host(new B17CAnalysisControl { Element = element });
            BayesianController plots = element.BayesianPlots;

            // B17C's diagnostics: the GMM's parameter ensemble in the simple views, the GMM's influence and report.
            Assert.AreSame(element.BayesianAnalysis, view.ParameterSetsControl.Analysis);
            Assert.IsFalse(view.ParameterSetsControl.ShowLikelihoodColumn);
            Assert.IsTrue(view.KernelDensityControl.SimpleView && view.HistogramControl.SimpleView && view.BivariateHeatMapControl.SimpleView);
            Assert.AreEqual(InfluenceControlMode.GMM, view.InfluenceDiagnosticsControl.Mode);
            Assert.AreSame(element.GMM, view.InfluenceDiagnosticsControl.GMMAnalysis);
            Assert.AreSame(element, view.GMMReportControl.Element);
            var tabs = new (TabItem Tab, Wpf.Plot Plot, Func<Wpf.Plot> Shown)[]
            {
                (view.KernelDensityTabItem, plots.KernelDensityPlot, () => view.KernelDensityControl.Plot),
                (view.HistogramTabItem, plots.HistogramPlot, () => view.HistogramControl.Plot),
                (view.BivariateTabItem, plots.BivariateHeatMapPlot, () => view.BivariateHeatMapControl.Plot),
                (view.InfluenceTabItem, plots.InfluenceDiagnosticsPlot, () => view.InfluenceDiagnosticsControl.Plot),
            };
            // Upstream asks the inner Frequency Plot tab first, which stays selected under the other outer tabs.
            Select(view.KernelDensityTabItem);
            Assert.AreSame(element.FrequencyPlot, view.GetCurrentPlot(), "upstream's order of tests");
            Select(((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Last());
            foreach (var (tab, expected, shown) in tabs)
            {
                Select(tab);
                Assert.AreSame(expected, shown(), (string)tab.Header);
                Assert.AreSame(expected, view.GetCurrentPlot(), (string)tab.Header);
            }
            Assert.IsTrue(plots.HistogramPlot.Title.StartsWith("Marginal Histogram", StringComparison.Ordinal), plots.HistogramPlot.Title);
            Select(view.GMMReportTabItem);
            Assert.IsNull(view.GetCurrentPlot(), "the report has no plot");
        }

        [STATestMethod]
        public void Properties_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Blakely);
            var element = Element<B17CAnalysis>(project, "Systematic + Historical + Paleo + Reg Skew + QPen");
            var properties = Host(new B17CAnalysisPropertiesControl { Element = element }, 420, 1100);

            // General: the element's name and dates, its input data and distribution.
            Assert.AreEqual(element.Name, properties.ElementName.Text);
            Assert.AreEqual(element.CreationDate.ToString(Us), properties.CreationDate.Text);
            var inputData = (ComboBox)((Border)properties.InputDataComboBox.InnerContent).Child;
            Assert.AreSame(element.InputData, inputData.SelectedItem);
            var listed = inputData.Items.Cast<InputData>().ToList();
            CollectionAssert.AreEquivalent(Elements(project).OfType<InputData>().ToList(), listed);
            CollectionAssert.AreEqual(listed.Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToList(), listed.Select(i => i.Name).ToList());
            var distribution = (ComboBox)properties.Distribution.InnerContent;
            Assert.AreEqual(UnivariateDistributionType.LogPearsonTypeIII, distribution.SelectedValue);
            Assert.AreEqual("Log-Pearson Type III", ((UnivariateDistributionItem)distribution.SelectedItem).DisplayName);

            // The parameter and quantile information: the penalties, a disabled one's cells disabled.
            Assert.AreSame(element.ParameterPenalties, properties.ParametersTable.ItemsSource);
            Assert.AreSame(element.QuantilePenalties, properties.QuantileTable.ItemsSource);
            var parameters = properties.ParametersTable;
            for (int i = 0; i < element.ParameterPenalties.Count; i++)
            {
                var penalty = element.ParameterPenalties[i];
                Assert.AreEqual(penalty.Name, CellText(parameters, i, 1));
                Assert.AreEqual(Numeric(penalty.Mean), CellText(parameters, i, 2), penalty.Name);
                Assert.AreEqual(Numeric(penalty.MSE), CellText(parameters, i, 3), penalty.Name);
                for (int column = 1; column < 4; column++)
                    Assert.AreEqual(penalty.Enabled, parameters.GetCell(i, column).IsEnabled, $"{penalty.Name}, column {column}");
            }
            var quantile = element.QuantilePenalties.First(q => q.Enabled);
            int row = element.QuantilePenalties.IndexOf(quantile);
            var aep = (ComboBox)properties.QuantileTable.GetCell(row, 1).Content;
            Assert.AreEqual(quantile.AEP, aep.SelectedItem);
            // The probabilities, in WPF's binding culture (en-US), as WPF's content presenter shows them.
            var template = aep.ItemTemplate.Build(1E-05);
            template.DataContext = 1E-05;
            Assert.AreEqual("1E-05", ((TextBlock)template).Text);
            Assert.AreEqual(quantile.UseLog10, ((CheckBox)properties.QuantileTable.GetCell(row, 2).Content).IsChecked);

            // A penalty's check box enables its row's cells.
            var first = element.ParameterPenalties[0];
            bool enabled = first.Enabled;
            ((CheckBox)parameters.GetCell(0, 0).Content).IsChecked = !enabled;
            Settle();
            Assert.AreEqual(!enabled, first.Enabled);
            Assert.AreEqual(!enabled, parameters.GetCell(0, 2).IsEnabled);
            ((CheckBox)parameters.GetCell(0, 0).Content).IsChecked = enabled;
            Settle();
            Assert.AreEqual(enabled, first.Enabled);

            // Each edit writes to the element, and the element's undo restores it.
            distribution.SelectedItem = properties.DistributionList.Single(i => i.Value == UnivariateDistributionType.LogNormal);
            Assert.AreEqual(UnivariateDistributionType.LogNormal, element.Bulletin17CDistribution.DistributionType);
            Assert.IsFalse(element.IsEstimated);
            element.UndoManager.Undo();
            Settle();
            Assert.AreEqual(UnivariateDistributionType.LogPearsonTypeIII, element.Bulletin17CDistribution.DistributionType);
            Assert.AreEqual(UnivariateDistributionType.LogPearsonTypeIII, distribution.SelectedValue, "the restored distribution shows");
            Assert.AreSame(element.ParameterPenalties, properties.ParametersTable.ItemsSource, "the restored model's penalties show");
            Assert.AreSame(element.QuantilePenalties, properties.QuantileTable.ItemsSource);

            properties.Description.Text = "Paleoflood information";
            Assert.AreEqual("Paleoflood information", element.Description);
            element.UndoManager.Undo();
            Assert.AreNotEqual("Paleoflood information", element.Description);

            // Options: the uncertainty method, with its tool tips, and the seed.
            Select(properties.Options_TabItem);
            var method = (ComboBox)properties.UncertaintyMethod.InnerContent;
            Assert.AreEqual(element.UncertaintyMethod, method.SelectedValue);
            var original = element.UncertaintyMethod;
            var other = properties.UncertaintyMethodList.First(m => m.Value != original);
            method.SelectedItem = other;
            Assert.AreEqual(other.Value, element.UncertaintyMethod);
            element.UndoManager.Undo();
            Assert.AreEqual(original, element.UncertaintyMethod);
            Assert.AreEqual(original, method.SelectedValue);

            int seed = element.PRNGSeed;
            properties.PRNGSeed.Number = seed + 1;
            Assert.AreEqual(seed + 1, element.PRNGSeed);
            element.UndoManager.Undo();
            Assert.AreEqual(seed, element.PRNGSeed);

            int length = element.BayesianAnalysis.OutputLength;
            properties.OutputLength.Number = length * 2;
            Assert.AreEqual(length * 2, element.BayesianAnalysis.OutputLength);
            element.UndoManager.Undo();
            Assert.AreEqual(length, element.BayesianAnalysis.OutputLength);
            Assert.AreEqual(element.BayesianAnalysis.CredibleIntervalWidth, ((ComboBox)properties.ConfidenceInterval.InnerContent).SelectedValue);
            Assert.AreEqual(element.BayesianAnalysis.PointEstimator, ((ComboBox)properties.PointEstimator.InnerContent).SelectedValue);
            Assert.AreSame(element.ProbabilityOrdinates, properties.ProbabilityOrdinatesControl.ProbabilityOrdinates);
        }

        [STATestMethod]
        public void Alternatives_AddTheirCurves_WithTheirAnalysesLabels()
        {
            UseAppResources(ThemeColor.Light);
            var project = OpenProject(Sinnemahoning);
            var element = Element<B17CAnalysis>(project, "B17C - LPIII - No Extension");
            var view = Host(new B17CAnalysisControl { Element = element });
            view.ShowFilterToggleButton.IsChecked = true;
            Settle();
            var others = Elements(project).OfType<IAnalysisElement>().Where(e => e != element && e.ParentCollection == element.ParentCollection).ToList();
            CollectionAssert.AreEquivalent(others, view.AlternativeSelector.AnalysisList.Select(i => i.Alternative).ToList());
            var plot = element.FrequencyPlot;

            // A Bayesian alternative has upstream's Bayesian labels; a Bulletin 17C one, B17C's.
            foreach (var (name, interval, predictive, point) in new[]
            {
                ("LPIII - No Extension", "90% Credible Intervals", "Posterior Predictive", "Posterior Mean"),
                ("B17C - LPIII - No Errors", "90% Confidence Intervals", "Expected Probability", "Computed"),
            })
            {
                var item = view.AlternativeSelector.AnalysisList.Single(i => i.Alternative.Name == name);
                int index = plot.Series.Count;
                item.IsChecked = true;
                Settle();
                Assert.AreEqual(name + " - " + interval, plot.Series.Single(s => s.Name == "CredibleIntervals_" + index).Title);
                Assert.AreEqual(name + " - " + predictive, plot.Series.Single(s => s.Name == "PosteriorPredictive_" + index).Title);
                Assert.AreEqual(name + " - " + point, plot.Series.Single(s => s.Name == "PosteriorMode_" + index).Title);
            }
            Assert.IsFalse(element.UndoManager.CanUndo, "comparing records nothing");
            foreach (var item in view.AlternativeSelector.AnalysisList) item.IsChecked = false;
            Settle();
            Assert.AreEqual(4, plot.Series.Count, string.Join(", ", plot.Series.Select(s => s.Name)));
        }

        [STATestMethod]
        [DataRow(Blakely)]
        [DataRow(Sinnemahoning)]
        public void ShowingTheViews_ChangesNothingInTheElement(string example)
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(example);
            foreach (var element in Elements(project).OfType<B17CAnalysis>().ToList())
            {
                string name = element.Name;
                var changes = new List<string>();
                Watch(element, changes);
                Watch(element.Bulletin17CDistribution, changes);
                foreach (var parameter in element.Bulletin17CDistribution.Parameters) Watch(parameter, changes);
                foreach (var penalty in element.ParameterPenalties) Watch(penalty, changes);
                foreach (var penalty in element.QuantilePenalties) Watch(penalty, changes);
                Watch(element.BayesianAnalysis, changes);
                Watch(element.InputData, changes);

                // Two documents, the first still holding the element when the second opens: the second takes the plots.
                var views = new List<B17CAnalysisControl>();
                for (int document = 0; document < 2; document++)
                {
                    var view = new B17CAnalysisControl { Element = element };
                    views.Add(view);
                    Window window = Show(view, 1300, 850);
                    view.ShowFilterToggleButton.IsChecked = true;
                    foreach (TabItem tab in ((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>())
                        Select(tab);
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
                Assert.AreSame(element.BayesianPlots.KernelDensityPlot, second.KernelDensityControl.PlotHost.Content, name);
                Assert.AreSame(element.BayesianPlots.InfluenceDiagnosticsPlot, second.InfluenceDiagnosticsControl.PlotHost.Content, name);

                var properties = new B17CAnalysisPropertiesControl { Element = element };
                Window panel = Show(properties, 420, 1100);
                Select(properties.Options_TabItem);
                Select(properties.General_TabItem);
                UnivariateAnalysisViewTests.CloseDocument(panel);
                properties.Element = null;

                Assert.AreEqual(0, changes.Count, $"{name}: showing the views changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the views recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the views made the element dirty");
                foreach (var view in views) view.Element = null;
            }
        }

        // A penalty's number as its cell shows it: through upstream's N/A converter, in en-US.
        private static string Numeric(double value) =>
            double.IsNaN(value) ? "N/A" : double.IsPositiveInfinity(value) ? "+∞" : double.IsNegativeInfinity(value) ? "-∞" : value.ToString(Us);

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
