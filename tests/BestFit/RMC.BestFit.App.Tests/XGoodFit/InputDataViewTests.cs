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
    }
}
