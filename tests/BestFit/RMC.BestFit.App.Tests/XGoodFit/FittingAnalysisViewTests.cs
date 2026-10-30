using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.Models;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the ported Distribution Fitting Analysis views (FittingAnalysisControl,
    /// FittingAnalysisPropertiesControl) on the fits in RMC-BestFit's Viglione et al. (2013) example.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class FittingAnalysisViewTests
    {
        /// <summary>The example with distribution fitting analyses.</summary>
        internal const string Viglione = "4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.bestfit";

        /// <summary>The radio buttons that choose plots.</summary>
        internal static readonly string[] PlotRadioButtons = { "PDFPlotRadioButton", "CDFPlotRadioButton", "PPPlotRadioButton", "QQPlotRadioButton", "FrequencyPlotRadioButton" };

        private IDisposable _themeDefaults;

        private IDisposable _culture;

        /// <summary>Sets up the shared resources and the plot toolbar's theme switch.</summary>
        [TestInitialize]
        public void Initialize()
        {
            OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            // avares://XGoodFit resolves to the first loaded assembly whose name starts with XGoodFit: load the application's.
            _ = typeof(FittingAnalysisControl).Assembly;
            WpfTestHost.EnsureResources();
            _themeDefaults = ViewTesting.ApplyThemeDefaults();
            _culture = ViewTesting.UseExamplesCulture();
        }

        /// <summary>Releases the shared resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            _culture?.Dispose();
            _themeDefaults?.Dispose();
            WpfTestHost.ReleaseResources();
        }

        /// <summary>
        /// Showing each fit's document (every plot, both tabs, every distribution type) and properties changes
        /// nothing: the analysis raises no property change and records no undo action. Nor does a second document.
        /// </summary>
        [STATestMethod]
        public void ShowingEveryPlotAndTable_OfEveryExampleFit_ChangesNothing()
        {
            using var opened = ViewTesting.Open(Viglione);
            var fits = opened.Elements<FittingAnalysis>().ToList();
            Assert.AreEqual(4, fits.Count);
            foreach (FittingAnalysis element in fits)
            {
                Assert.IsTrue(element.IsEstimated, element.Name + " is saved estimated");
                int undoCount = element.UndoManager.UndoStack.Count;
                List<string> changes = ViewTesting.RecordChanges(element);
                for (int document = 0; document < 2; document++)
                {
                    var control = new FittingAnalysisControl { Element = element };
                    Window window = ViewTesting.Show(control);
                    try
                    {
                        foreach (string radio in PlotRadioButtons)
                        {
                            control.FindControl<RadioButton>(radio).IsChecked = true;
                            ViewTesting.Settle();
                        }
                        var type = control.FindControl<ComboBox>("TypeComboBox");
                        for (int i = 3; i >= 0; i--)
                        {
                            type.SelectedIndex = i;
                            ViewTesting.Settle();
                        }
                        control.FindControl<TabItem>("TabularResultsTab").IsSelected = true;
                        ViewTesting.Settle();

                        window.Content = new FittingAnalysisPropertiesControl { Element = element };
                        ViewTesting.Settle();
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                Assert.AreEqual("", string.Join(", ", changes), element.Name + ": showing the views raised property changes");
                Assert.AreEqual(undoCount, element.UndoManager.UndoStack.Count, element.Name + ": showing the views recorded undo actions");
            }
        }

        /// <summary>
        /// The goodness-of-fit table lists each fitted distribution with its AIC, BIC and RMSE (two decimals, or
        /// N/A), and the tabular results show each distribution's parameters, moments and quantiles as upstream
        /// computes them from the fitted distributions.
        /// </summary>
        [STATestMethod]
        public void Tables_ShowTheFitsGoodnessOfFitAndResults()
        {
            using var opened = ViewTesting.Open(Viglione);
            var element = opened.Elements<FittingAnalysis>().First();
            var control = new FittingAnalysisControl { Element = element };
            Window window = ViewTesting.Show(control, 1400, 900);
            try
            {
                var culture = FormattingCulture.Current;
                var table = control.FindControl<GenericControls.CopyPasteDataGrid>("AICBICTable");
                CollectionAssert.AreEqual(new[] { "Distribution", "AIC", "BIC", "RMSE" }, table.Columns.Skip(1).Select(c => c.Header?.ToString()).ToArray());
                Assert.AreEqual(element.FittedDistributions.Count, table.Items.Count);
                for (int row = 0; row < 5; row++)
                {
                    var fit = (FittedDistribution)table.Items[row];
                    Assert.AreSame(element.FittedDistributions[row], fit);
                    Assert.AreEqual(fit.Distribution.DisplayName, InputDataViewTests.CellText(table, row, 1));
                    Assert.AreEqual(Formatted(fit.AIC, "F2", culture), InputDataViewTests.CellText(table, row, 2));
                    Assert.AreEqual(Formatted(fit.BIC, "F2", culture), InputDataViewTests.CellText(table, row, 3));
                    Assert.AreEqual(Formatted(fit.RMSE, "F2", culture), InputDataViewTests.CellText(table, row, 4));
                    Assert.AreEqual(fit.FitSucceeded, table.GetRow(row).IsEnabled, "a failed fit's row is disabled");
                }

                control.FindControl<TabItem>("TabularResultsTab").IsSelected = true;
                ViewTesting.Settle();
                var results = control.FindControl<GenericControls.CopyPasteDataGrid>("SummaryStatisticsDataGrid");
                Assert.AreEqual(10 + element.ProbabilityOrdinates.Count, results.Items.Count);
                for (int column = 0; column < 3; column++)
                {
                    var distribution = element.FittedDistributions[column].Distribution;
                    Assert.AreEqual(Formatted(distribution.GetParameters[0], "N4", culture), InputDataViewTests.CellText(results, 0, column + 1), "location");
                    Assert.AreEqual(Formatted(distribution.Mean, "N4", culture), InputDataViewTests.CellText(results, 6, column + 1), "mean");
                    double p = element.ProbabilityOrdinates[0];
                    Assert.AreEqual(p.ToString(), InputDataViewTests.CellText(results, 10, 0));
                    Assert.AreEqual(Formatted(distribution.InverseCDF(1 - p), "N4", culture), InputDataViewTests.CellText(results, 10, column + 1), "quantile");
                }
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// Each plot shows a curve for each fitted distribution, with upstream's points, and the input data. Hiding a
        /// distribution hides its curves and its results column.
        /// </summary>
        [STATestMethod]
        public void Plots_ShowEachFittedDistribution_AndFollowItsCheckBox()
        {
            using var opened = ViewTesting.Open(Viglione);
            var element = opened.Elements<FittingAnalysis>().First();
            var control = new FittingAnalysisControl { Element = element };
            Window window = ViewTesting.Show(control, 1400, 900);
            try
            {
                var fitted = element.FittedDistributions.Where(f => f.FitSucceeded).ToList();
                var frequency = element.FrequencyPlot;
                Assert.AreSame(frequency, control.GetCurrentPlot());
                var curves = frequency.Series.OfType<OxyPlot.Wpf.LineSeries>().ToList();
                CollectionAssert.AreEqual(fitted.Select(f => f.Distribution.Type.ToString()).ToArray(), curves.Select(s => s.Name).ToArray());
                Assert.IsTrue(frequency.Series.Any(s => s.Name == "ExactData"), "the input data");
                var points = ((IEnumerable<OxyPlot.DataPoint>)curves[0].ItemsSource).ToList();
                Assert.AreEqual(element.ProbabilityOrdinates.Count, points.Count);
                Assert.AreEqual(fitted[0].Distribution.InverseCDF(1 - element.ProbabilityOrdinates[3]), points[3].Y);

                control.FindControl<RadioButton>("PDFPlotRadioButton").IsChecked = true;
                ViewTesting.Settle();
                Assert.AreSame(element.PDFPlot, control.GetCurrentPlot());
                Assert.AreEqual("Histogram", element.PDFPlot.Series[0].Name);
                Assert.AreEqual(fitted.Count, element.PDFPlot.Series.OfType<OxyPlot.Wpf.LineSeries>().Count());
                Assert.AreEqual(Visibility.Visible, control.FindControl<ContentControl>("PDFPlotHost").Visibility);
                Assert.AreEqual(Visibility.Hidden, control.FindControl<ContentControl>("FrequencyPlotHost").Visibility);

                fitted[0].ShowResults = false;
                ViewTesting.Settle();
                Assert.AreEqual(Visibility.Hidden, element.PDFPlot.Series.OfType<OxyPlot.Wpf.LineSeries>().First().Visibility);
                Assert.IsFalse(control.FindControl<CheckBox>("SelectAllCheckBox").IsChecked == true, "Select All follows one distribution");
                var results = control.FindControl<GenericControls.CopyPasteDataGrid>("SummaryStatisticsDataGrid");
                Assert.IsFalse(results.Columns[1 + element.FittedDistributions.IndexOf(fitted[0])].IsVisible, "its results column is hidden");

                control.FindControl<CheckBox>("SelectAllCheckBox").IsChecked = true;
                ViewTesting.Settle();
                Assert.IsTrue(element.FittedDistributions.All(f => f.ShowResults));
                Assert.AreEqual(Visibility.Visible, element.PDFPlot.Series.OfType<OxyPlot.Wpf.LineSeries>().First().Visibility);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// The properties panel shows the analysis's input data (selected without writing to the analysis) and its
        /// probability ordinates.
        /// </summary>
        [STATestMethod]
        public void Properties_ShowTheInputDataAndOrdinates()
        {
            using var opened = ViewTesting.Open(Viglione);
            var element = opened.Elements<FittingAnalysis>().First();
            List<string> changes = ViewTesting.RecordChanges(element);
            var properties = new FittingAnalysisPropertiesControl { Element = element };
            Window window = ViewTesting.Show(properties, 400, 800);
            try
            {
                var inputData = properties.FindControl<GenericControls.ContentPropertyControl>("InputDataComboBox");
                var combo = (ComboBox)((Border)inputData.InnerContent).Child;
                Assert.AreSame(element.InputData, combo.SelectedItem);
                Assert.AreEqual(0, ((Border)inputData.InnerContent).BorderThickness.Left, "a valid input data has no red border");
                CollectionAssert.AreEqual(opened.Elements<InputData>().Select(i => i.Name).OrderBy(n => n, StringComparer.InvariantCulture).ToArray(),
                    combo.Items.Cast<InputData>().Select(i => i.Name).ToArray(), "the project's input data, sorted by name");
                Assert.AreSame(element.ProbabilityOrdinates, properties.FindControl<NumericControls.ProbabilityOrdinatesControl>("ProbabilityOrdinatesControl").ProbabilityOrdinates);
                Assert.AreEqual(Visibility.Hidden, properties.FindControl<ProgressBar>("ProgressBar").Visibility);
                Assert.AreEqual(Visibility.Hidden, properties.FindControl<Button>("CancelButton").Visibility);
                Assert.AreEqual("", string.Join(", ", changes));
            }
            finally
            {
                window.Close();
            }
        }

        // A number as upstream's grid columns show it (DoubleToNAConverter, then the format).
        private static string Formatted(double value, string format, IFormatProvider culture) =>
            double.IsNaN(value) ? "N/A" : double.IsPositiveInfinity(value) ? "+∞" : double.IsNegativeInfinity(value) ? "-∞" : value.ToString(format, culture);
    }
}
