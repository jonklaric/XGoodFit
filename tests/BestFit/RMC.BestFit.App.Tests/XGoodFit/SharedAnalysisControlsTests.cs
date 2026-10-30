using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NumericControls.Distributions.Univariate;
using Numerics.Distributions;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using RMC.BestFit.Estimation;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using WpfCompat.Documents;
using WpfCompat.Media.Media3D;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the shared analysis controls (GUI/Support/Controls) on the example projects' stored
    /// results, hosted as the analysis views host them: each diagnostic control is given its element's plot
    /// (<c>SetPlot</c>, with the element's plot bridges suspended) and then the element's analysis.
    /// </summary>
    /// <remarks>
    /// The plots are compared with RMC-BestFit's native exports of the same examples (the screenshots'
    /// capture data: title, axes and their ranges, series), and the tables with upstream's own computations,
    /// in US formats (plan D8). Editing writes to the element as upstream's bindings do, and the element's
    /// undo restores it; showing a control changes nothing in the element.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class SharedAnalysisControlsTests
    {
        internal const string Viglione = "4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.bestfit";
        internal const string ViglioneElement = "MCMC - Systematic (1951-2001)";
        internal const string ViglioneQuantilePriors = "MCMC - Systematic (1951-2001) + 3 Quantile Priors";
        internal const string Mixture = "4-univariate-distribution-analysis/4-mixture-analysis/mixture-distribution-examples.bestfit";
        internal const string MixtureElement = "Mixture Distribution - 2 Normals";
        internal const string B17C = "4-univariate-distribution-analysis/2-bulletin-17C-analysis/1-bulletin17C-examples/bulletin-17c-examples.bestfit";
        internal const string Waimea = "5-bivariate-distribution-analysis/2-coincident-frequency/waimea-river-stage-frequency.bestfit";
        private const string ViglioneShots = "4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/screenshots/viglione-et-al-2013-";
        private const string MixtureShots = "4-univariate-distribution-analysis/4-mixture-analysis/screenshots/mixture-";

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
        public void DiagnosticPlots_DrawTheStoredResults_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            BayesianAnalysis analysis = element.BayesianAnalysis;
            BayesianController plots = element.BayesianPlots;
            string location = analysis.ParameterNames[0];

            // Kernel density: RMC-BestFit's own export of this plot (title, axes and ranges, the density).
            var density = Host(element, new KernelDensityControl(), c => c.SetPlot(plots.KernelDensityPlot), c => c.Analysis = analysis);
            JsonElement native = NativePlot(ViglioneShots + "kernel-density-location.json.gz");
            AssertPlot(native, plots.KernelDensityPlot);
            var points = ((IEnumerable<Point3D>)plots.KernelDensityPlot.Series.OfType<Wpf.AreaSeries>().Single().ItemsSource).ToList();
            double[,] kde = analysis.Results.ParameterResults[0].KernelDensity;
            JsonElement nativeDensity = native.GetProperty("series")[0].GetProperty("points2");
            Assert.AreEqual(kde.GetLength(0), points.Count);
            Assert.AreEqual(nativeDensity.GetArrayLength(), points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                Assert.AreEqual(new Point3D(kde[i, 0], 0, kde[i, 1]), points[i]);
                Assert.AreEqual(nativeDensity[i].GetProperty("X").GetDouble(), points[i].X);
                Assert.AreEqual(nativeDensity[i].GetProperty("Y").GetDouble(), points[i].Z);
            }

            // The prior's density overlays the posterior's (Show Prior), and another parameter is another plot.
            density.ShowPriorDistribution.IsChecked = true;
            var prior = plots.KernelDensityPlot.Series.OfType<Wpf.AreaSeries>().Single(s => s.Name == "PriorDensity");
            double[,] pdf = analysis.Model.Parameters[0].PriorDistribution.CreatePDFGraph();
            Assert.AreEqual("Prior Density", prior.Title);
            Assert.AreEqual(pdf[10, 1], ((IList<Point3D>)prior.ItemsSource)[10].Z);
            density.ParameterComboBox.SelectedIndex = 1;
            Assert.AreEqual($"Marginal Posterior Density of {analysis.ParameterNames[1]}", plots.KernelDensityPlot.Title);
            Assert.AreEqual(analysis.ParameterNames[1], plots.KernelDensityPlot.Axes.Single(a => a.Position == AxisPosition.Bottom).Title);

            // Markov chain traces: every chain, warm-up excluded, in upstream's seeded colours.
            Host(element, new MarkovChainTraceControl(), c => c.SetPlot(plots.MarkovChainTracePlot), c => c.Analysis = analysis);
            native = NativePlot(ViglioneShots + "trace-location.json.gz");
            AssertPlot(native, plots.MarkovChainTracePlot);
            var chains = plots.MarkovChainTracePlot.Series.OfType<Wpf.LineSeries>().ToList();
            JsonElement nativeChains = native.GetProperty("series");
            Assert.AreEqual(nativeChains.GetArrayLength(), chains.Count);
            for (int c = 0; c < chains.Count; c++)
            {
                Assert.AreEqual(nativeChains[c].GetProperty("name").GetString(), chains[c].Name);
                Assert.AreEqual(nativeChains[c].GetProperty("Title").GetString(), chains[c].Title);
                Assert.AreEqual(nativeChains[c].GetProperty("style").GetProperty("Color").GetString(), Hex(chains[c].Color));
                var data = (DataPoint[])chains[c].ItemsSource;
                JsonElement nativePoints = nativeChains[c].GetProperty("points");
                Assert.AreEqual(nativePoints.GetArrayLength(), data.Length);
                for (int i = 0; i < data.Length; i++)
                    Assert.AreEqual(new DataPoint(nativePoints[i].GetProperty("X").GetDouble(), nativePoints[i].GetProperty("Y").GetDouble()), data[i]);
            }

            // Autocorrelation: the lags' bars and the credible interval's lines.
            Host(element, new AutocorrelationControl(), c => c.SetPlot(plots.AutocorrelationPlot), c => c.Analysis = analysis);
            native = NativePlot(ViglioneShots + "autocorrelation-location.json.gz");
            AssertPlot(native, plots.AutocorrelationPlot);
            var bars = (List<HistogramItem>)plots.AutocorrelationPlot.Series.OfType<Wpf.HistogramSeries>().Single().ItemsSource;
            JsonElement nativeBars = native.GetProperty("series")[0].GetProperty("actualItems");
            Assert.AreEqual(nativeBars.GetArrayLength(), bars.Count);
            for (int i = 0; i < bars.Count; i++)
            {
                Assert.AreEqual(nativeBars[i].GetProperty("RangeStart").GetDouble(), bars[i].RangeStart);
                Assert.AreEqual(nativeBars[i].GetProperty("RangeEnd").GetDouble(), bars[i].RangeEnd);
                Assert.AreEqual(nativeBars[i].GetProperty("Value").GetDouble(), bars[i].Value, 1E-15);
            }
            foreach (JsonElement annotation in native.GetProperty("annotations").EnumerateArray())
            {
                var line = plots.AutocorrelationPlot.Annotations.OfType<Wpf.LineAnnotation>().Single(a => a.Text == annotation.GetProperty("text").GetString());
                Assert.AreEqual(annotation.GetProperty("geometry").GetProperty("Y").GetDouble(), line.Y, 1E-15);
            }

            // Histogram: upstream's bins, normalised to a density.
            Host(element, new HistogramControl(), c => c.SetPlot(plots.HistogramPlot), c => c.Analysis = analysis);
            Assert.AreEqual($"Marginal Posterior Histogram of {location}", plots.HistogramPlot.Title);
            Assert.AreEqual(location, plots.HistogramPlot.Axes.Single(a => a.Position == AxisPosition.Bottom).Title);
            var histogram = analysis.Results.ParameterResults[0].Histogram;
            var binItems = (List<HistogramItem>)plots.HistogramPlot.Series.OfType<Wpf.HistogramSeries>().Single().ItemsSource;
            double sum = 0;
            for (int i = 0; i < histogram.NumberOfBins; i++) sum += histogram[i].Frequency * histogram.BinWidth;
            Assert.AreEqual(histogram.NumberOfBins, binItems.Count);
            for (int i = 0; i < binItems.Count; i++)
            {
                Assert.AreEqual(histogram[i].LowerBound, binItems[i].RangeStart);
                Assert.AreEqual(histogram[i].UpperBound, binItems[i].RangeEnd);
                Assert.AreEqual(histogram[i].Frequency / sum * histogram.BinWidth, binItems[i].Area);
            }

            // Mean log-likelihood: one point per iteration.
            Host(element, new MeanLikelihoodControl(), c => c.SetPlot(plots.MeanLikelihoodPlot), c => c.Analysis = analysis);
            Assert.AreEqual("Mean Log-Likelihood", plots.MeanLikelihoodPlot.Title);
            CollectionAssert.AreEqual(analysis.Results.MeanLogLikelihood.Select((value, i) => new DataPoint(i + 1, value)).ToList(),
                (List<DataPoint>)plots.MeanLikelihoodPlot.Series.OfType<Wpf.LineSeries>().Single().ItemsSource);

            // Bivariate heat map: the first two parameters' joint density over their histograms' range.
            Host(element, new BivariateHeatMapControl(), c => c.SetPlot(plots.BivariateHeatMapPlot), c => c.Analysis = analysis);
            Assert.AreEqual($"Joint Posterior Density of {location} and {analysis.ParameterNames[1]}", plots.BivariateHeatMapPlot.Title);
            var heat = plots.BivariateHeatMapPlot.Series.OfType<Wpf.HeatMapSeries>().Single();
            Assert.AreEqual(1.0, heat.Data.Cast<double>().Sum(), 1E-9, "each retained draw adds 1/n");
            Assert.AreEqual(histogram[0].LowerBound, heat.X0);
            Assert.AreEqual(histogram[histogram.NumberOfBins - 1].UpperBound, heat.X1);
            Assert.AreEqual(6, plots.BivariateHeatMapPlot.Series.OfType<Wpf.ContourSeries>().Single().ContourLevels.Length);

            // Influence: the leverage tornado of the most influential observations and priors.
            var influence = Host(element, new InfluenceDiagnosticsControl(), c => c.SetPlot(plots.InfluenceDiagnosticsPlot), c => c.Analysis = analysis);
            var leverage = analysis.ComputeLeverageDiagnostics();
            Assert.AreEqual("Combined Leverage (% of Total Influence)", plots.InfluenceDiagnosticsPlot.Title);
            StringAssert.StartsWith(influence.SummaryText.Text, $"p = {leverage.NumberOfParameters}. Data: ");
            var labels = plots.InfluenceDiagnosticsPlot.Axes.OfType<Wpf.CategoryAxis>().Single().Labels;
            Assert.AreEqual(Math.Min(20, leverage.Observations.Count() + (leverage.PriorComponents?.Length ?? 0)), labels.Count);
            influence.ViewComboBox.SelectedIndex = 3;
            Assert.AreEqual("Leave-One-Out Predictive Surprise", plots.InfluenceDiagnosticsPlot.Title);
            Assert.AreEqual(Visibility.Collapsed, influence.IncludePriorsCheckBox.Visibility);
        }

        [STATestMethod]
        public void MixtureDiagnostics_PlotTheStoredCoordinates_AsRmcBestFitDid()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<MixtureAnalysis>(OpenProject(Mixture), MixtureElement);
            BayesianAnalysis analysis = element.BayesianAnalysis;
            BayesianController plots = element.BayesianPlots;

            var density = Host(element, new KernelDensityControl(), c => c.SetPlot(plots.KernelDensityPlot), c => c.Analysis = analysis);
            AssertPlot(NativePlot(MixtureShots + "kernel-density-w1.json.gz"), plots.KernelDensityPlot);
            // New results store K-1 weights: the selector leaves out the derived last weight.
            CollectionAssert.DoesNotContain(density.ParameterComboBox.Items.Cast<string>().ToList(), analysis.ParameterNames[1]);
            Assert.AreEqual(analysis.Results.ParameterResults.Length, density.ParameterComboBox.ItemCount);

            Host(element, new MarkovChainTraceControl(), c => c.SetPlot(plots.MarkovChainTracePlot), c => c.Analysis = analysis);
            JsonElement native = NativePlot(MixtureShots + "trace-w1.json.gz");
            AssertPlot(native, plots.MarkovChainTracePlot);
            Assert.AreEqual(native.GetProperty("series").GetArrayLength(), plots.MarkovChainTracePlot.Series.Count);

            Host(element, new AutocorrelationControl(), c => c.SetPlot(plots.AutocorrelationPlot), c => c.Analysis = analysis);
            AssertPlot(NativePlot(MixtureShots + "autocorrelation-w1.json.gz"), plots.AutocorrelationPlot);
        }

        [STATestMethod]
        public void StatisticsTable_ShowsTheSummaryStatistics_InUsFormats()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            BayesianAnalysis analysis = element.BayesianAnalysis;
            var density = Host(element, new KernelDensityControl(), c => c.SetPlot(element.BayesianPlots.KernelDensityPlot), c => c.Analysis = analysis);
            var statistics = analysis.Results.ParameterResults[0].SummaryStatistics;
            string[] expected =
            {
                statistics.Rhat.ToString("F3", Us), statistics.ESS.ToString("N0", Us), statistics.Mean.ToString("N4", Us),
                statistics.StandardDeviation.ToString("N4", Us), statistics.LowerCI.ToString("N4", Us),
                statistics.Median.ToString("N4", Us), statistics.UpperCI.ToString("N4", Us),
            };
            for (int column = 0; column < expected.Length; column++)
                Assert.AreEqual(expected[column], CellText(density.KDEDataGrid, 0, column), $"column {column}");

            // The percentile headers follow the credible interval (90% here), in the code-behind's culture, as upstream's.
            double alpha = (1 - analysis.CredibleIntervalWidth) / 2d;
            Assert.AreEqual((alpha * 100).ToString("F1") + "%", density.KDEDataGrid.Columns[4].Header);
            Assert.AreEqual(((1 - alpha) * 100).ToString("F1") + "%", density.KDEDataGrid.Columns[6].Header);

            // B17C's simple view hides the MCMC diagnostics' columns and the prior's check box.
            var b17c = Element<B17CAnalysis>(OpenProject(B17C), "Example #1");
            var simple = Host(b17c, new HistogramControl { SimpleView = true }, c => c.SetPlot(b17c.BayesianPlots.HistogramPlot), c => c.Analysis = b17c.BayesianAnalysis);
            Assert.IsFalse(simple.KDEDataGrid.Columns[0].IsVisible);
            Assert.IsFalse(simple.KDEDataGrid.Columns[1].IsVisible);
            Assert.AreEqual(Visibility.Collapsed, simple.ShowPriorDistribution.Visibility);
            StringAssert.StartsWith(b17c.BayesianPlots.HistogramPlot.Title, "Marginal Histogram of ");
        }

        [STATestMethod]
        public void Reports_ShowTheAnalysesReports_OnAWidePage()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            var mcmc = new MCMCReportControl { Analysis = element.BayesianAnalysis };
            Host(mcmc, 900, 600);
            Assert.AreEqual(element.BayesianAnalysis.GenerateReport(), ReportText(mcmc.ReportTextBox));
            Assert.AreEqual(1200, mcmc.ReportTextBox.Document.PageWidth);
            Assert.AreEqual("Consolas", mcmc.ReportTextBox.Document.FontFamily.Name);
            StringAssert.Contains(ReportText(mcmc.ReportTextBox), element.BayesianAnalysis.ParameterNames[0]);

            // B17C's GMM report is the one the project stored (a loaded analysis has no optimizer).
            var b17c = Element<B17CAnalysis>(OpenProject(B17C), "Example #1");
            var gmm = new GMMReportControl { Element = b17c };
            Host(gmm, 900, 600);
            Assert.IsFalse(string.IsNullOrEmpty(b17c.GMMReport));
            Assert.AreEqual(b17c.GMMReport, ReportText(gmm.ReportTextBox));
        }

        [STATestMethod]
        public void ParameterSets_ListEveryRetainedDraw()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            BayesianAnalysis analysis = element.BayesianAnalysis;
            var sets = new ParameterSetsControl { Analysis = analysis };
            Host(sets, 800, 500);
            var view = sets.ParameterSetTableViewer.DataView;
            CollectionAssert.AreEqual(analysis.ParameterNames.Append("Log-Likelihood").ToArray(), view.ColumnNames);
            Assert.AreEqual(analysis.Results.Output.Count, view.NumberOfRows);
            var rows = view.GetRows(0, 2);
            for (int row = 0; row < 2; row++)
                CollectionAssert.AreEqual(analysis.Results.Output[row].Values.Cast<object>().Append(analysis.Results.Output[row].Fitness).ToArray(), rows[row]);

            // A mixture's stored draws leave out the last weight, which the table derives (upstream's K-1 rule).
            var mixture = Element<MixtureAnalysis>(OpenProject(Mixture), MixtureElement);
            var mixtureSets = new ParameterSetsControl { Analysis = mixture.BayesianAnalysis };
            Host(mixtureSets, 800, 500);
            var mixtureView = mixtureSets.ParameterSetTableViewer.DataView;
            Assert.AreEqual(mixture.BayesianAnalysis.NumberOfEstimatedParameters + 1, mixtureView.ColumnNames.Length);
            double[] first = ParameterSetsControl.GetPhysicalDisplayValues(mixture.BayesianAnalysis, mixture.BayesianAnalysis.Results.Output[0].Values);
            CollectionAssert.AreEqual(first.Cast<object>().ToArray(), mixtureView.GetRows(0, 0)[0].Take(first.Length).ToArray());
            Assert.AreEqual(Visibility.Collapsed, mixtureSets.InvalidMixtureDrawWarning.Visibility);

            // B17C's parameter sets have no likelihood column.
            var b17c = Element<B17CAnalysis>(OpenProject(B17C), "Example #1");
            var b17cSets = new ParameterSetsControl { ShowLikelihoodColumn = false, Analysis = b17c.BayesianAnalysis };
            Host(b17cSets, 800, 500);
            CollectionAssert.DoesNotContain(b17cSets.ParameterSetTableViewer.DataView.ColumnNames, "Log-Likelihood");
        }

        [STATestMethod]
        public void BayesianOptionsAndOutput_ShowAndEditTheAnalysis_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            BayesianAnalysis analysis = element.BayesianAnalysis;

            var options = new BayesianOptionsControl { Analysis = analysis };
            Host(options, 320, 480);
            Assert.AreEqual(analysis.NumberOfChains, options.NumberOfChains.Number);
            Assert.AreEqual(analysis.ThinningInterval, options.ThinningInterval.Number);
            Assert.AreEqual(analysis.InitialIterations, options.InitialIterations.Number);
            Assert.AreEqual(analysis.WarmupIterations, options.WarmupIterations.Number);
            Assert.AreEqual(analysis.Iterations, options.Iterations.Number);
            Assert.AreEqual(analysis.PRNGSeed, options.PRNGSeed.Number);
            Assert.AreEqual(analysis.UseSimulationDefaults, options.UseSimulationDefaults.IsSelected);
            Assert.AreEqual(!analysis.UseSimulationDefaults, options.NumberOfChains.IsTextBoxEnabled);

            // The advanced options shown are the sampler's (DE-MCzs here); another sampler shows its own.
            Assert.AreEqual(BayesianAnalysis.SamplerType.DEMCzs, analysis.Type);
            Assert.AreEqual(Visibility.Visible, options.SnookerThreshold.Visibility);
            Assert.AreEqual(Visibility.Collapsed, options.ScaleParameter.Visibility);
            analysis.Type = BayesianAnalysis.SamplerType.ARWMH;
            Assert.AreEqual(Visibility.Visible, options.ScaleParameter.Visibility);
            Assert.AreEqual(Visibility.Collapsed, options.JumpParameter.Visibility);
            element.UndoManager.Undo();
            Assert.AreEqual(BayesianAnalysis.SamplerType.DEMCzs, analysis.Type);
            Assert.AreEqual(Visibility.Visible, options.JumpParameter.Visibility);

            // Editing a simulation option writes it to the analysis, and the element's undo restores it.
            options.UseSimulationDefaults.IsSelected = false;
            int chains = analysis.NumberOfChains;
            options.NumberOfChains.Number = chains + 2;
            Assert.AreEqual(chains + 2, analysis.NumberOfChains);
            element.UndoManager.Undo();
            Assert.AreEqual(chains, analysis.NumberOfChains);
            Assert.AreEqual(chains, options.NumberOfChains.Number);

            // The output options: the credible interval and point estimator, as the combo boxes show them.
            var output = new BayesianOutputControl { Analysis = analysis };
            Host(output, 320, 160);
            var interval = (ComboBox)output.CredibleInterval.InnerContent;
            var estimator = (ComboBox)output.PointEstimator.InnerContent;
            Assert.AreEqual(analysis.CredibleIntervalWidth, ((CredibleIntervalItem)interval.SelectedItem).Value);
            Assert.AreEqual(analysis.PointEstimator, estimator.SelectedValue);
            Assert.AreEqual(analysis.OutputLength, output.OutputLength.Number);
            double width = analysis.CredibleIntervalWidth;
            interval.SelectedItem = output.CredibleIntervalItems.Single(item => item.Value == 0.99);
            Assert.AreEqual(0.99, analysis.CredibleIntervalWidth);
            element.UndoManager.Undo();
            Assert.AreEqual(width, analysis.CredibleIntervalWidth);
            Assert.AreEqual(width, ((CredibleIntervalItem)interval.SelectedItem).Value, "the restored interval shows");
        }

        [STATestMethod]
        public void PriorControls_ShowAndEditTheModelsPriors_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var project = OpenProject(Viglione);
            var element = Element<UnivariateAnalysis>(project, ViglioneElement);

            // Parameter priors: one row per parameter, its prior's label on the button that edits it.
            var priors = new ParameterPriorsControl { Model = element.UnivariateDistribution };
            Host(priors, 320, 220);
            var parameters = element.UnivariateDistribution.Parameters;
            Assert.AreEqual(parameters.Count, priors.PriorDistributionsDataGrid.Items.Count);
            for (int row = 0; row < parameters.Count; row++)
            {
                Assert.AreEqual(parameters[row].DisplayName, CellText(priors.PriorDistributionsDataGrid, row, 0));
                Assert.AreEqual(parameters[row].PriorDistribution.DisplayLabel, ToggleText(priors.PriorDistributionsDataGrid, row));
            }
            AssertHeaderToolTip(priors.PriorDistributionsDataGrid.Columns[0], "Left_ColumnHeaderStyle", "The model parameter names.");
            AssertHeaderToolTip(priors.PriorDistributionsDataGrid.Columns[1], "Center_ColumnHeaderStyle", "Set the prior distributions of the model parameters.");

            // Editing a prior in its popup writes a copy to the parameter when the popup closes; undo restores it.
            string before = parameters[0].PriorDistribution.DisplayLabel;
            var cell = priors.PriorDistributionsDataGrid.GetCell(0, 1);
            var toggle = cell.GetVisualDescendants().OfType<ToggleButton>().Single();
            var popup = cell.GetVisualDescendants().OfType<Popup>().Single();
            Assert.AreEqual(!element.UnivariateDistribution.UseDefaultFlatPriors, toggle.IsEffectivelyEnabled);
            toggle.IsChecked = true;
            Settle();
            Assert.IsTrue(popup.IsOpen);
            var selector = (DistributionSelectorControl)((Border)popup.Child).Child;
            Assert.AreEqual(11, selector.Distributions.Count, "upstream's eleven prior distributions");
            Assert.AreEqual(before, selector.SelectedDistribution.DisplayLabel);
            // Upstream titles the editor with the parameter's Name, which a stationary distribution's parameters
            // leave empty (their label is OwnerName): "Select Distribution for :", as in RMC-BestFit.
            Assert.AreEqual(parameters[0].Name, selector.DistributionTitle);
            var normal = new Normal(40, 5);
            selector.SelectedDistribution = normal;
            toggle.IsChecked = false;
            Settle();
            Assert.IsFalse(popup.IsOpen);
            var edited = element.UnivariateDistribution.Parameters[0].PriorDistribution;
            Assert.AreEqual(normal.DisplayLabel, edited.DisplayLabel);
            Assert.AreNotSame(normal, edited, "the parameter takes a copy, as upstream's");
            Assert.AreEqual(normal.DisplayLabel, ToggleText(priors.PriorDistributionsDataGrid, 0));
            Assert.IsTrue(element.UndoManager.CanUndo);
            element.UndoManager.Undo();
            // As the properties view does, the control is given the element's restored model.
            priors.Model = element.UnivariateDistribution;
            Settle();
            Assert.AreEqual(before, element.UnivariateDistribution.Parameters[0].PriorDistribution.DisplayLabel);
            Assert.AreEqual(before, ToggleText(priors.PriorDistributionsDataGrid, 0));

            // Quantile priors: the probabilities in US formats, and an edited probability writes to its prior.
            var three = Element<UnivariateAnalysis>(project, ViglioneQuantilePriors);
            var quantiles = new QuantilePriorsControl { Model = three.UnivariateDistribution };
            Host(quantiles, 320, 240);
            var quantilePriors = three.UnivariateDistribution.QuantilePriors;
            Assert.AreEqual(quantilePriors.Count, quantiles.PriorDistributionsDataGrid.Items.Count);
            Assert.AreEqual(three.UnivariateDistribution.EnableQuantilePriors, quantiles.EnablePriorQuantileDistributions.IsSelected);
            var combo = quantiles.PriorDistributionsDataGrid.GetCell(0, 0).GetVisualDescendants().OfType<ComboBox>().Single();
            Assert.AreEqual(quantilePriors[0].Alpha, (double)combo.SelectedItem);
            Assert.AreEqual(quantilePriors[0].Alpha.ToString(Us), combo.GetVisualDescendants().OfType<TextBlock>().First(t => !string.IsNullOrEmpty(t.Text)).Text);
            Assert.AreEqual(quantilePriors[0].Distribution.DisplayLabel, ToggleText(quantiles.PriorDistributionsDataGrid, 0));
            double alpha = quantilePriors[0].Alpha;
            double other = quantiles.QuantileOptions.First(option => option != alpha);
            combo.SelectedItem = other;
            Assert.AreEqual(other, three.UnivariateDistribution.QuantilePriors[0].Alpha);
            Assert.IsTrue(three.UndoManager.CanUndo);
            three.UndoManager.Undo();
            Assert.AreEqual(alpha, three.UnivariateDistribution.QuantilePriors[0].Alpha);

            // Mixture analyses hide the single quantile check box.
            var mixture = Element<MixtureAnalysis>(OpenProject(Mixture), MixtureElement);
            var mixtureQuantiles = new QuantilePriorsControl { ShowSingleQuantileCheckBox = false, Model = mixture.MixtureDistribution };
            Host(mixtureQuantiles, 320, 240);
            Assert.AreEqual(Visibility.Collapsed, mixtureQuantiles.UseSingleQuantile.Visibility);
        }

        [STATestMethod]
        public void Ordinates_ShowAndEditTheElementsValues_WithUndo()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<CoincidentFrequencyAnalysis>(OpenProject(Waimea), "CFA - Normal - Conditional");
            var ordinates = new OrdinatesControl { RowItemType = typeof(XOrdinateRowItem), Ordinates = element.XValues };
            Host(ordinates, 300, 400);
            var rows = (IList)ordinates.DataGrid.ItemsSource;
            CollectionAssert.AreEqual(element.XValues.ToList(), rows.Cast<OrdinateRowItem>().Select(row => row.Value).ToList());
            Assert.IsInstanceOfType(rows[0], typeof(XOrdinateRowItem));
            Assert.AreEqual(element.XValues[0], double.Parse(CellText(ordinates.DataGrid, 0, 0), Us));

            // A committed cell edit sets the row's value, which writes the element's ordinate; undo restores both.
            double value = element.XValues[0];
            ((OrdinateRowItem)rows[0]).Value = value + 1;
            Assert.AreEqual(value + 1, element.XValues[0]);
            Assert.IsTrue(element.UndoManager.CanUndo);
            element.UndoManager.Undo();
            Assert.AreEqual(value, element.XValues[0]);
            Assert.AreEqual(value, ((OrdinateRowItem)rows[0]).Value, "the grid shows the restored ordinate");
        }

        [STATestMethod]
        public void TraceControl_DrawsOnlyWhileItIsVisibleInAWindow()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            BayesianController plots = element.BayesianPlots;
            var trace = new MarkovChainTraceControl();
            using (element.SuspendPlotBridges()) trace.SetPlot(plots.MarkovChainTracePlot);
            trace.Analysis = element.BayesianAnalysis;
            var tabs = new TabControl
            {
                ItemsSource = new[] { new TabItem { Header = "Summary", Content = new TextBlock() }, new TabItem { Header = "Markov Chain Traces", Content = trace } },
                SelectedIndex = 0,
            };
            _windows.Add(Show(tabs, 1000, 650));
            // The project's saved plot settings hold the chains' series, without their data.
            Assert.IsTrue(plots.MarkovChainTracePlot.Series.All(s => s.ItemsSource == null), "nothing is drawn while the tab is hidden");

            tabs.SelectedIndex = 1;
            Settle();
            Assert.AreEqual(element.BayesianAnalysis.NumberOfChains, plots.MarkovChainTracePlot.Series.Count);
            Assert.IsTrue(plots.MarkovChainTracePlot.Series.All(s => s.ItemsSource is DataPoint[]));

            // A selection made while an ancestor is hidden is drawn when it shows again (WPF's IsVisibleChanged).
            tabs.IsVisible = false;
            trace.ChainComboBox.SelectedIndex = 2;
            Assert.AreEqual(element.BayesianAnalysis.NumberOfChains, plots.MarkovChainTracePlot.Series.Count);
            tabs.IsVisible = true;
            Settle();
            Assert.AreEqual("Chain 2", plots.MarkovChainTracePlot.Series.Single().Title);

            // Including the warm-up starts the traces at the first iteration.
            trace.IncludeWarmUpCheckBox.IsChecked = true;
            Assert.AreEqual(1, ((DataPoint[])plots.MarkovChainTracePlot.Series.OfType<Wpf.LineSeries>().Single().ItemsSource)[0].X);
        }

        [STATestMethod]
        public void PreviewClicks_TellTheViewWhetherThePlotOrToolbarWasClicked()
        {
            UseAppResources(ThemeColor.Light);
            var element = Element<UnivariateAnalysis>(OpenProject(Viglione), ViglioneElement);
            var histogram = Host(element, new HistogramControl(), c => c.SetPlot(element.BayesianPlots.HistogramPlot), c => c.Analysis = element.BayesianAnalysis);
            Window window = _windows.Last();
            var clicks = new List<(bool Plot, bool Toolbar, Wpf.Plot Clicked)>();
            histogram.PreviewControlClicked += (plotClicked, toolbarClicked, plot) => clicks.Add((plotClicked, toolbarClicked, plot));

            Click(window, Centre(window, element.BayesianPlots.HistogramPlot));
            Assert.AreEqual((true, false, element.BayesianPlots.HistogramPlot), clicks.Single());
            Assert.IsTrue(histogram.PlotClicked);

            // Pressed on the toolbar, released away from it (so no tool runs).
            Point toolbar = HitPoint(window, histogram.PlotToolbar);
            window.MouseDown(toolbar, MouseButton.Left);
            window.MouseUp(new Point(1, 1), MouseButton.Left);
            Settle();
            Assert.AreEqual((false, true, element.BayesianPlots.HistogramPlot), clicks.Last());
            Assert.IsFalse(histogram.PlotClicked);
        }

        [STATestMethod]
        public void ShowingTheControls_ChangesNothingInTheElement()
        {
            UseAppResources(ThemeColor.Light);
            OverlayPopups();
            var cases = new (string Project, string Element)[]
            {
                (Viglione, ViglioneElement),
                (Viglione, ViglioneQuantilePriors),
                (Mixture, MixtureElement),
                (B17C, "Example #1"),
                ("4-univariate-distribution-analysis/3-point-process-analysis/point-process-examples.bestfit", "USC00040741 - Point Process"),
                ("5-bivariate-distribution-analysis/1-bivariate-distributions/bivariate-distribution-examples.bestfit", "AMH Copula"),
                ("6-rating-curve-analysis/usgs-07024175-mississippi-rating-curve.bestfit", "USGS 07024175 Rating Curve"),
                ("7-time-series-analysis/2-classic-time-series-examples/classic-time-series-examples.bestfit", "Airline Passengers - TSA"),
            };
            foreach (var (file, name) in cases)
            {
                var element = (ElementBase)Elements(OpenProject(file)).Single(e => e.Name == name);
                var analysis = (BayesianAnalysis)element.GetType().GetProperty("BayesianAnalysis").GetValue(element);
                var plots = (BayesianController)element.GetType().GetProperty("BayesianPlots")?.GetValue(element);
                Assert.IsNotNull(analysis, name);
                var changes = new List<string>();
                Watch(element, changes);
                Watch(analysis, changes);
                Watch(analysis.Model, changes);
                foreach (var parameter in analysis.Model?.Parameters ?? Enumerable.Empty<ModelParameter>()) Watch(parameter, changes);
                if (analysis.Model is IQuantilePriors withQuantiles && withQuantiles.QuantilePriors != null)
                    foreach (var quantile in withQuantiles.QuantilePriors) Watch(quantile, changes);

                // The controls each analysis view hosts: B17C's shows no MCMC diagnostics, and the bivariate
                // view no heat map.
                bool isB17C = element is B17CAnalysis;
                bool hasHeatMap = !(element is BivariateAnalysis);
                var controls = new List<Control> { new ParameterSetsControl { ShowLikelihoodColumn = !isB17C, Analysis = analysis } };
                if (!isB17C)
                {
                    controls.Add(new BayesianOptionsControl { Analysis = analysis });
                    controls.Add(new BayesianOutputControl { Analysis = analysis });
                    controls.Add(new MCMCReportControl { Analysis = analysis });
                }
                // The views that have a model give the prior controls its priors (B17C's has its own).
                if (!isB17C && analysis.Model != null)
                    controls.Add(new ParameterPriorsControl { Model = analysis.Model });
                if (!isB17C && analysis.Model is IQuantilePriors quantilePriors)
                    controls.Add(new QuantilePriorsControl { Model = quantilePriors });
                if (element is B17CAnalysis b17c)
                    controls.Add(new GMMReportControl { Element = b17c });
                if (plots != null)
                {
                    using (Suspend(element))
                    {
                        var density = new KernelDensityControl { SimpleView = isB17C };
                        density.SetPlot(plots.KernelDensityPlot);
                        var histogram = new HistogramControl { SimpleView = isB17C };
                        histogram.SetPlot(plots.HistogramPlot);
                        var influence = new InfluenceDiagnosticsControl();
                        influence.SetPlot(plots.InfluenceDiagnosticsPlot);
                        controls.AddRange(new Control[] { density, histogram, influence });
                        if (hasHeatMap)
                        {
                            var heatMap = new BivariateHeatMapControl { SimpleView = isB17C };
                            heatMap.SetPlot(plots.BivariateHeatMapPlot);
                            controls.Add(heatMap);
                        }
                        if (!isB17C)
                        {
                            var trace = new MarkovChainTraceControl();
                            trace.SetPlot(plots.MarkovChainTracePlot);
                            var acf = new AutocorrelationControl();
                            acf.SetPlot(plots.AutocorrelationPlot);
                            var likelihood = new MeanLikelihoodControl();
                            likelihood.SetPlot(plots.MeanLikelihoodPlot);
                            controls.AddRange(new Control[] { trace, acf, likelihood });
                        }
                    }
                    foreach (var control in controls)
                    {
                        switch (control)
                        {
                            case KernelDensityControl c: c.Analysis = analysis; break;
                            case HistogramControl c: c.Analysis = analysis; break;
                            case MarkovChainTraceControl c: c.Analysis = analysis; break;
                            case AutocorrelationControl c: c.Analysis = analysis; break;
                            case MeanLikelihoodControl c: c.Analysis = analysis; break;
                            case BivariateHeatMapControl c: c.Analysis = analysis; break;
                            case InfluenceDiagnosticsControl c when element is B17CAnalysis b:
                                c.Mode = InfluenceControlMode.GMM;
                                c.GMMAnalysis = b.GMM;
                                break;
                            case InfluenceDiagnosticsControl c: c.Analysis = analysis; break;
                        }
                    }
                }

                // Each control is shown in turn, as the views' tabs show them.
                var tabs = new TabControl { ItemsSource = controls.Select(control => new TabItem { Header = control.GetType().Name, Content = control }).ToList() };
                Window window = Show(tabs, 1100, 700);
                for (int tab = 0; tab < controls.Count; tab++)
                {
                    tabs.SelectedIndex = tab;
                    Settle();
                }
                window.Close();

                Assert.AreEqual(0, changes.Count, $"{name}: showing the controls changed " + string.Join(", ", changes));
                Assert.IsFalse(element.UndoManager.CanUndo, $"{name}: showing the controls recorded an undo");
                Assert.IsFalse(element.IsDirty, $"{name}: showing the controls made the element dirty");
            }
        }

        private BestFitProject OpenProject(string relative)
        {
            var project = Open(relative);
            _projects.Add(project);
            return project;
        }

        private Window Host(Control control, double width, double height)
        {
            var window = Show(control, width, height);
            _windows.Add(window);
            return window;
        }

        // The analysis views' order: the element's plot is set with its bridges suspended, then the analysis.
        private T Host<T>(IElement element, T control, Action<T> setPlot, Action<T> setAnalysis) where T : Control
        {
            using (Suspend(element)) setPlot(control);
            setAnalysis(control);
            Host(control, 1000, 650);
            return control;
        }

        private static IDisposable Suspend(object element) =>
            (IDisposable)element.GetType().GetMethod("SuspendPlotBridges").Invoke(element, null);

        private static void Watch(object source, List<string> changes)
        {
            if (source is INotifyPropertyChanged notifier)
                notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }

        // The plot's title, and its axes' titles and ranges, as RMC-BestFit's export recorded them.
        private static void AssertPlot(JsonElement native, Wpf.Plot plot)
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
                Assert.AreEqual(minimum, actual.ActualMinimum, tolerance, position);
                Assert.AreEqual(maximum, actual.ActualMaximum, tolerance, position);
            }
        }

        private static string Hex(Color color) => $"#{color.A:x2}{color.R:x2}{color.G:x2}{color.B:x2}";

        private static string CellText(GenericControls.CopyPasteDataGrid grid, int row, int column) =>
            (grid.GetCell(row, column)?.Content as TextBlock)?.Text;

        private static string ToggleText(GenericControls.CopyPasteDataGrid grid, int row) =>
            grid.GetCell(row, 1).GetVisualDescendants().OfType<ToggleButton>().Single().GetVisualDescendants().OfType<TextBlock>().Single().Text;

        private static string ReportText(WpfCompat.Controls.RichTextBox box)
        {
            var paragraph = box.Document.Blocks.OfType<Paragraph>().Single();
            string text = ((Run)paragraph.Inlines.Single()).Text;
            var shown = box.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
            Assert.AreEqual(text, shown.Inlines.OfType<Avalonia.Controls.Documents.Run>().Single().Text, "the box shows the document");
            return text;
        }

        // The header theme upstream's code gives the column: its XAML's theme, with the tool tip.
        private static void AssertHeaderToolTip(DataGridColumn column, string baseKey, string toolTip)
        {
            var theme = column.HeaderTheme;
            Assert.AreSame(Application.Current.FindResource(baseKey), theme.BasedOn);
            var setter = theme.Setters.OfType<Avalonia.Styling.Setter>().Single(s => s.Property == ToolTip.TipProperty);
            var text = ((FuncTemplate<TextBlock>)setter.Value).Build();
            Assert.AreEqual(toolTip, text.Text);
        }
    }
}
