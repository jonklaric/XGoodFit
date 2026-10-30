using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the ported Input Data views (InputDataControl, InputDataPropertiesControl) on
    /// RMC-BestFit's example projects.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class InputDataViewTests
    {
        /// <summary>The examples with input data elements that the tests open.</summary>
        internal static readonly string[] InputDataExamples =
        {
            "2-input-data/1-block-maximum/usgs-block-max-example.bestfit",
            "2-input-data/2-usgs-peak-discharge/usgs-peak-download-example.bestfit",
            "2-input-data/3-peaks-over-threshold/ghcn-peaks-over-threshold-example.bestfit",
            "2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.bestfit",
            "4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.bestfit",
        };

        /// <summary>The main tab control's tabs, in order.</summary>
        internal static readonly string[] Tabs =
        {
            "DataFrameTab", "SummaryStatisticsTab", "HypothesisTestsTab", "SeasonalityTab", "DensityTab", "HistogramTab",
            "QQTab", "ACFTab", "PACFTab", "ThresholdDiagnosticsTab", "USGSTextFileTab",
        };

        /// <summary>The radio buttons that choose plots, each checked in turn.</summary>
        internal static readonly string[] PlotRadioButtons =
        {
            "FrequencyPlotRadioButton", "ChronologyPlotRadioButton", "RealSpaceRadioButton", "LogSpaceRadioButton",
            "ModifiedScalePlotRadioButton", "ShapePlotRadioButton", "MRLPlotRadioButton",
        };

        /// <summary>Sets up the shared resources and the plot toolbar's theme switch.</summary>
        [TestInitialize]
        public void Initialize()
        {
            OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            // avares://XGoodFit resolves to the first loaded assembly whose name starts with XGoodFit: load the application's.
            _ = typeof(InputDataControl).Assembly;
            WpfTestHost.EnsureResources();
            _themeDefaults = ViewTesting.ApplyThemeDefaults();
        }

        /// <summary>Releases the shared resources.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            _themeDefaults?.Dispose();
            WpfTestHost.ReleaseResources();
        }

        private IDisposable _themeDefaults;

        /// <summary>
        /// Showing an input data element's document and properties, every tab and plot of them, changes nothing:
        /// the element raises no property change and records no undo action. Nor does a second document of the
        /// element (one opened again), while the first still holds the element, as upstream's MainProjectNode
        /// leaves it.
        /// </summary>
        [STATestMethod]
        public void ShowingEveryTabAndPlot_OfEveryExampleInput_ChangesNothing()
        {
            foreach (string example in InputDataExamples)
            {
                using var opened = ViewTesting.Open(example);
                foreach (InputData element in opened.Elements<InputData>())
                {
                    string label = example + ": " + element.Name;
                    int undoCount = element.UndoManager.UndoStack.Count;
                    List<string> changes = ViewTesting.RecordChanges(element);

                    for (int document = 0; document < 2; document++)
                    {
                        var control = new InputDataControl { Element = element };
                        Window window = ViewTesting.Show(control);
                        try
                        {
                            foreach (string tab in Tabs)
                            {
                                var item = control.FindControl<TabItem>(tab);
                                if (!item.IsVisible) continue;
                                item.IsSelected = true;
                                ViewTesting.Settle();
                                Assert.AreSame(item, control.FindControl<TabControl>("MainTabControl").SelectedItem, label + " " + tab);
                            }
                            foreach (string radio in PlotRadioButtons)
                            {
                                control.FindControl<RadioButton>(radio).IsChecked = true;
                                ViewTesting.Settle();
                            }

                            window.Content = new InputDataPropertiesControl { Element = element };
                            ViewTesting.Settle();
                        }
                        finally
                        {
                            window.Close();
                        }
                    }

                    Assert.AreEqual("", string.Join(", ", changes), label + ": showing the views raised property changes");
                    Assert.AreEqual(undoCount, element.UndoManager.UndoStack.Count, label + ": showing the views recorded undo actions");
                }
            }
        }

        private const string BlockMaximum = "2-input-data/1-block-maximum/usgs-block-max-example.bestfit";
        private const string PeakDischarge = "2-input-data/2-usgs-peak-discharge/usgs-peak-download-example.bestfit";
        private const string GhcnPot = "2-input-data/3-peaks-over-threshold/ghcn-peaks-over-threshold-example.bestfit";
        private const string UsgsPot = "2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.bestfit";

        /// <summary>
        /// Each plot RMC-BestFit's native screenshots show of the examples' input data has the same series, points
        /// and annotations in the port, which the views compute from the saved data (capture data beside the
        /// screenshots: screenshots/*-native.json.gz). The threshold diagnostics fit distributions, so they compare
        /// with a looser tolerance.
        /// </summary>
        /// <param name="example">The example.</param>
        /// <param name="capture">The native screenshot's name.</param>
        [STATestMethod]
        [DataRow(BlockMaximum, "block-max-calendar-chronology-native")]
        [DataRow(BlockMaximum, "block-max-calendar-frequency-native")]
        [DataRow(BlockMaximum, "block-max-wateryear-chronology-native")]
        [DataRow(PeakDischarge, "peak-download-moose-river-chronology-native")]
        [DataRow(PeakDischarge, "peak-download-moose-river-frequency-native")]
        [DataRow(PeakDischarge, "peak-download-orestimba-chronology-native")]
        [DataRow(PeakDischarge, "peak-download-orestimba-frequency-native")]
        [DataRow(GhcnPot, "ghcn-pot-chronology-native")]
        [DataRow(GhcnPot, "ghcn-pot-frequency-native")]
        [DataRow(GhcnPot, "ghcn-pot-mrl-native")]
        [DataRow(UsgsPot, "usgs-pot-chronology-native")]
        [DataRow(UsgsPot, "usgs-pot-frequency-native")]
        [DataRow(UsgsPot, "usgs-pot-mrl-native")]
        [DataRow(UsgsPot, "usgs-pot-modified-scale-native")]
        [DataRow(UsgsPot, "usgs-pot-shape-native")]
        public void Plot_HasTheNativeScreenshotsData(string example, string capture)
        {
            var native = NativeCaptures.Read(example, capture);
            using var opened = ViewTesting.Open(example);
            var element = opened.Element<InputData>(native.GetProperty("element").GetString());
            var control = new InputDataControl { Element = element };
            Window window = ViewTesting.Show(control);
            try
            {
                string plotId = native.GetProperty("plotId").GetString();
                (string tab, string radio, OxyPlot.Wpf.Plot plot, double tolerance) = plotId switch
                {
                    "input_data.chronology" => ("DataFrameTab", "ChronologyPlotRadioButton", element.ChronologyPlot, 1E-12),
                    "input_data.frequency" => ("DataFrameTab", "FrequencyPlotRadioButton", element.FrequencyPlot, 1E-12),
                    "input_data.mean_residual_life" => ("ThresholdDiagnosticsTab", "MRLPlotRadioButton", element.MRLPlot, 1E-6),
                    "input_data.modified_scale" => ("ThresholdDiagnosticsTab", "ModifiedScalePlotRadioButton", element.ModifiedScalePlot, 1E-6),
                    "input_data.shape" => ("ThresholdDiagnosticsTab", "ShapePlotRadioButton", element.ShapePlot, 1E-6),
                    _ => throw new AssertFailedException("Unknown plot " + plotId),
                };
                control.FindControl<TabItem>(tab).IsSelected = true;
                ViewTesting.Settle();
                control.FindControl<RadioButton>(radio).IsChecked = true;
                ViewTesting.Settle();
                Assert.AreSame(plot, control.GetCurrentPlot(), plotId);

                NativeCaptures.AssertPlotMatches(plot, native, tolerance);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// The data frame's grid shows the processed block maxima, read-only, in the generated columns upstream
        /// configures (without the dummy row that made them); the summary statistics and hypothesis tests tables
        /// show what the data frame computes, formatted as upstream's bindings and code format them.
        /// </summary>
        [STATestMethod]
        public void Tables_ShowTheDataFramesValuesAndStatistics()
        {
            using var opened = ViewTesting.Open(BlockMaximum);
            var element = opened.Element<InputData>("USGS - 01134500 - Block Max - Calendar Year");
            var control = new InputDataControl { Element = element };
            Window window = ViewTesting.Show(control);
            try
            {
                var culture = WpfCompat.FormattingCulture.Current;
                var grid = control.FindControl<GenericControls.ValidationDataGrid>("ExactDataGrid");
                CollectionAssert.AreEqual(new[] { "Date Time", "Value", "Plotting Position", "Low Outlier" }, grid.Columns.Select(c => c.Header?.ToString()).ToArray());
                Assert.AreEqual(element.DataFrame.ExactSeries.Count, control.ExactDataOrdinates.Count, "the rows, without the dummy row that generated the columns");
                Assert.IsTrue(grid.IsReadOnly, "a processed series is read-only");
                Assert.IsFalse(grid.CanUserAddInsertDeleteRows);
                for (int row = 0; row < 3; row++)
                {
                    var data = (RMC.BestFit.Models.ExactData)element.DataFrame.ExactSeries[row];
                    Assert.AreEqual(data.DateTime.ToString("g", culture), CellText(grid, row, 0));
                    Assert.AreEqual(string.Format(culture, "{0:" + FrameworkUI.UserSettings.ValueStringFormat + "}", data.Value), CellText(grid, row, 1));
                    Assert.AreEqual(data.PlottingPosition.ToString("F6", culture), CellText(grid, row, 2));
                }

                control.FindControl<TabItem>("SummaryStatisticsTab").IsSelected = true;
                ViewTesting.Settle();
                var statistics = control.FindControl<GenericControls.CopyPasteDataGrid>("SummaryStatisticsDataGrid");
                var exactOnly = element.DataFrame.SummaryStatisticsExactDataOnly();
                var allData = element.DataFrame.SummaryStatisticsAllData();
                Assert.AreEqual(exactOnly.Count, statistics.Items.Count);
                for (int row = 0; row < 5; row++)
                {
                    Assert.AreEqual(exactOnly.ElementAt(row).Key, CellText(statistics, row, 0));
                    Assert.AreEqual(Formatted(exactOnly.ElementAt(row).Value, culture), CellText(statistics, row, 1));
                    Assert.AreEqual(Formatted(allData.ElementAt(row).Value, culture), CellText(statistics, row, 2));
                }

                control.FindControl<TabItem>("HypothesisTestsTab").IsSelected = true;
                ViewTesting.Settle();
                var tests = control.FindControl<GenericControls.CopyPasteDataGrid>("HypothesisDataGrid");
                var slider = control.FindControl<GenericControls.NumericSlider>("IndexSlider");
                var results = element.DataFrame.SummaryHypothesisTest((int)slider.Value, true);
                Assert.AreEqual(results.Count, tests.Items.Count);
                Assert.AreEqual(results.ElementAt(0).Key, CellText(tests, 0, 0));
                double p = results.ElementAt(0).Value;
                string expectedP = p > 1E-4 ? GenericControls.NumberFormatHelper.FormatDouble(p, "N4")
                    : p < 1E-15 ? "< " + GenericControls.NumberFormatHelper.FormatDouble(1E-15, "E0")
                    : GenericControls.NumberFormatHelper.FormatDouble(p, "E2");
                Assert.AreEqual(expectedP, CellText(tests, 0, 1));
                Assert.AreEqual(p < 0.1 ? "The data is not Normally distributed." : "The data is Normally distributed.", CellText(tests, 0, 3));
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>
        /// The properties panel shows the rows upstream's data entry method shows (as the native screenshot of the
        /// block maxima's properties), and its options change the element as upstream's do, each undoable: the
        /// plotting position (which the document's grid shows at once), the time block, the smoothing and the
        /// low outlier test.
        /// </summary>
        [STATestMethod]
        public void Properties_ShowTheMethodsRows_AndEditTheElementUndoably()
        {
            using var opened = ViewTesting.Open(BlockMaximum);
            var element = opened.Element<InputData>("USGS - 01134500 - Block Max - Calendar Year");
            var document = new InputDataControl { Element = element };
            var properties = new InputDataPropertiesControl { Element = element };
            Grid.SetColumn(properties, 1);
            Window window = ViewTesting.Show(new Grid { ColumnDefinitions = new ColumnDefinitions("*,350"), Children = { document, properties } });
            try
            {
                string[] shown = { "ElementName", "Description", "CreationDate", "LastModified", "UnitLabel", "IndexLabel", "ExactDateMethod",
                    "TimeSeriesElement", "BlockFunction", "TimeBlock", "SmoothingFunction", "ProcessDataButton", "PlottingParameter", "MGBTButton", "LOThresholdValue", "RunTestButton" };
                string[] hidden = { "USGSSiteNumber", "StartMonth", "EndMonth", "POTThresholdValue", "MinStepsBetweenPeaks", "Period" };
                foreach (string name in shown)
                    Assert.IsTrue(properties.FindControl<Control>(name).IsVisible, name + " is shown");
                foreach (string name in hidden)
                    Assert.IsFalse(properties.FindControl<Control>(name).IsVisible, name + " is hidden");
                Assert.AreEqual("Process", properties.FindControl<TextBlock>("ProcessDataButtonText").Text);
                Assert.AreSame(element.TimeSeriesElement, Combo(properties, "TimeSeriesElement").SelectedItem, "the time series, selected without writing to the element");
                Assert.IsFalse(properties.FindControl<Control>("LOThresholdValue").IsEnabled, "the threshold is off while the Grubbs-Beck test is on");

                var grid = document.FindControl<GenericControls.ValidationDataGrid>("ExactDataGrid");
                int undo = element.UndoManager.UndoStack.Count;
                double alpha = element.DataFrame.PlottingParameter;
                Select<PlottingPositionItem>(Combo(properties, "PlottingParameter"), item => item.Value == 0.44);
                Assert.AreEqual(0.44, element.DataFrame.PlottingParameter);
                Assert.AreEqual(undo + 1, element.UndoManager.UndoStack.Count, "one undoable change");
                var first = (RMC.BestFit.Models.ExactData)element.DataFrame.ExactSeries[0];
                Assert.AreEqual(first.PlottingPosition.ToString("F6", WpfCompat.FormattingCulture.Current), CellText(grid, 0, 2), "the grid shows the new plotting positions");
                element.UndoManager.Undo();
                ViewTesting.Settle();
                Assert.AreEqual(alpha, element.DataFrame.PlottingParameter);
                Assert.AreEqual(alpha, ((PlottingPositionItem)Combo(properties, "PlottingParameter").SelectedItem).Value, "the combo box follows the undo");

                Select<TimeBlockItem>(Combo(properties, "TimeBlock"), item => item.Value == Numerics.Data.TimeBlockWindow.CustomYear);
                Assert.AreEqual(Numerics.Data.TimeBlockWindow.CustomYear, element.TimeBlock);
                Assert.IsTrue(properties.FindControl<Control>("StartMonth").IsVisible);
                Assert.IsTrue(properties.FindControl<Control>("EndMonth").IsVisible);
                element.UndoManager.Undo();
                ViewTesting.Settle();
                Assert.AreEqual(Numerics.Data.TimeBlockWindow.CalendarYear, element.TimeBlock);
                Assert.IsFalse(properties.FindControl<Control>("StartMonth").IsVisible, "the rows follow the undo");

                Select<SmoothingFunctionItem>(Combo(properties, "SmoothingFunction"), item => item.Value == Numerics.Data.SmoothingFunctionType.MovingAverage);
                Assert.AreEqual(Numerics.Data.SmoothingFunctionType.MovingAverage, element.SmoothingFunction);
                Assert.IsTrue(properties.FindControl<Control>("Period").IsVisible);
                element.UndoManager.Undo();
                ViewTesting.Settle();
                Assert.AreEqual(Numerics.Data.SmoothingFunctionType.None, element.SmoothingFunction);

                properties.FindControl<GenericControls.BooleanPropertyControl>("MGBTButton").IsSelected = false;
                ViewTesting.Settle();
                Assert.IsFalse(element.UseMultipleGrubbsBeckTest);
                Assert.IsTrue(properties.FindControl<Control>("LOThresholdValue").IsEnabled);
                element.UndoManager.Undo();
                ViewTesting.Settle();
                Assert.IsTrue(element.UseMultipleGrubbsBeckTest);
            }
            finally
            {
                window.Close();
            }
        }

        /// <summary>The text a grid cell shows.</summary>
        internal static string CellText(GenericControls.CopyPasteDataGrid grid, int row, int column) =>
            ((TextBlock)grid.GetCell(row, column).Content).Text;

        // A number as upstream's summary statistics columns show it (DoubleToNAConverter, then the format).
        private static string Formatted(double value, IFormatProvider culture) =>
            double.IsNaN(value) ? "N/A" : double.IsPositiveInfinity(value) ? "+∞" : double.IsNegativeInfinity(value) ? "-∞" : value.ToString("#,##0.####", culture);

        // A property row's combo box: its inner content, or the combo box in its border.
        private static ComboBox Combo(Control properties, string row)
        {
            object content = properties.FindControl<GenericControls.ContentPropertyControl>(row).InnerContent;
            return content as ComboBox ?? (ComboBox)((Border)content).Child;
        }

        // Selects a combo box's item, as a user does.
        private static void Select<T>(ComboBox combo, Func<T, bool> match)
        {
            combo.SelectedItem = combo.Items.Cast<T>().First(match);
            ViewTesting.Settle();
        }
    }
}
