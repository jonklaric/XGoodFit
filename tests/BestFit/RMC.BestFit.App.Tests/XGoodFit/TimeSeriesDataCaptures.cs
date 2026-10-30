using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.TimeSeriesDataTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Time Series Data views on the chapter's examples, as PNGs in artifacts/screenshots, to
    /// compare with RMC-BestFit's native screenshots of the same examples (screenshots/*-native.png and
    /// *-current.png beside each example):
    /// <list type="bullet">
    /// <item><c>time-series-data-&lt;screenshot&gt;-export.png</c>: the plot a native plot screenshot shows, exported as
    /// RMC-BestFit's screenshots were (PngExporter, 1000 × 650, on white), after the view has drawn it.</item>
    /// <item><c>time-series-data-&lt;screenshot&gt;-{light,blue,dark}.png</c>: the document view showing that plot.</item>
    /// <item><c>time-series-data-&lt;example&gt;-properties-panel-{theme}.png</c>: the properties panel, at the native
    /// screenshot's size (650 × 576).</item>
    /// <item><c>time-series-data-hec-dss-path-selector-{theme}.png</c>: the DSS pathname selector on the example's DSS
    /// file, with the inflow's path selected, at the native screenshot's size (1050 × 620).</item>
    /// </list>
    /// The views of records over 200,000 observations take gigabytes, so they're captured only with
    /// XGOODFIT_LARGE_TESTS=1 (see TimeSeriesDataTests). Run the captures alone with
    /// <c>RMC.BestFit.App.Tests --filter "FullyQualifiedName~TimeSeriesDataCaptures"</c>.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class TimeSeriesDataCaptures
    {
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Blue, ThemeColor.Dark };

        private readonly List<BestFitProject> _projects = new List<BestFitProject>();
        private readonly List<Window> _windows = new List<Window>();

        private CultureScope _culture;

        // The examples were saved, and their screenshots taken, in en-US (see TimeSeriesDataTesting.CultureScope).
        [TestInitialize]
        public void Initialize() => _culture = new CultureScope("en-US");

        [TestCleanup]
        public void Cleanup()
        {
            foreach (var window in _windows) window.Close();
            _windows.Clear();
            foreach (var project in _projects) CloseProject(project);
            _projects.Clear();
            UseAppResources(ThemeColor.Light);
            ReleaseAppResources();
            _culture.Dispose();
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CapturePlots_AsTheNativeScreenshotsShowThem()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                BestFitProject project = null;
                foreach (var shot in NativePlots)
                {
                    // One example at a time: they hold millions of observations.
                    if (project == null || Path.GetFileName(project.FullFileName) != Path.GetFileName(shot.Example))
                    {
                        if (project != null)
                        {
                            CloseProject(project);
                            _projects.Remove(project);
                            GC.Collect();
                        }
                        project = Open(shot.Example);
                        _projects.Add(project);
                    }
                    var element = Element<TimeSeriesElement>(project, shot.Element);
                    if (element.TimeSeries.Count > 200000 && Environment.GetEnvironmentVariable("XGOODFIT_LARGE_TESTS") != "1") continue;
                    var (view, window) = ShowView(element, shot.Tab);
                    _windows.Add(window);
                    var selector = view.FindControl<AlternativeTimeSeriesControl>("AlternativeTimeSeriesSelector");
                    if (shot.Alternative != null)
                    {
                        view.FindControl<Avalonia.Controls.Primitives.ToggleButton>("ShowAlternativesToggleButton").IsChecked = true;
                        selector.TimeSeriesList.Single(item => item.Alternative.Name == shot.Alternative).IsChecked = true;
                        Settle();
                    }
                    if (theme == ThemeColor.Light)
                    {
                        string export = Path.Combine(RepositoryRoot, "artifacts", "screenshots", $"time-series-data-{shot.Name}-export.png");
                        Directory.CreateDirectory(Path.GetDirectoryName(export));
                        view.GetCurrentPlot().SaveBitmap(export, 1000, 650, OxyColors.White);
                    }
                    Capture(window, $"time-series-data-{shot.Name}-{theme.ToString().ToLowerInvariant()}");
                    if (shot.Alternative != null)
                        selector.TimeSeriesList.Single(item => item.Alternative.Name == shot.Alternative).IsChecked = false;
                    window.Close();
                    _windows.Remove(window);
                    view.Element = null;

                    // A view of a million observations holds a gigabyte: let each go before the next.
                    Settle();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureDssPathSelectorAndUsgsText_InEveryTheme()
        {
            string folder = Path.Combine(Path.GetTempPath(), "XGoodFitTimeSeriesData", Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(folder);
            string dss = Path.Combine(folder, "dss-example-data.dss");
            File.Copy(Example("1-time-series-data/5-hec-dss-import/dss-example-data.dss"), dss);
            var project = Open(Usgs);
            _projects.Add(project);
            var peaks = Element<TimeSeriesElement>(project, "USGS - 01614000 - Peak Discharge");
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                string name = theme.ToString().ToLowerInvariant();

                // As the native screenshot: no parts chosen, the inflow's row selected (the window's own title bar
                // is part of the window here; the native screenshot left the system's out).
                var selector = new DSSPathSelectorWindow { FullFileName = dss, Width = 1050, Height = 650 };
                selector.Show();
                _windows.Add(selector);
                Settle();
                selector.MyDataGrid.SelectedItem = selector.MyDataGrid.Items.Cast<DssPathSelectorRow>().Single(r => r.Bpart == "GRAPEVINE INFLOW" && r.Cpart == "FLOW");
                selector.MyDataGrid.ScrollIntoView(selector.MyDataGrid.SelectedItem, null);
                Settle();
                Capture(selector, $"time-series-data-hec-dss-path-selector-{name}");
                selector.Close();

                var (view, window) = ShowView(peaks, "USGS Text File");
                _windows.Add(window);
                Capture(window, $"time-series-data-usgs-peak-discharge-text-{name}");
                window.Close();
                view.Element = null;
            }
            DeleteFolder(folder);
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CapturePropertiesPanels_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                foreach (var shot in NativeProperties)
                {
                    var project = Open(shot.Example);
                    var properties = new TimeSeriesPropertiesControl { Element = Element<TimeSeriesElement>(project, shot.Element) };
                    Window window = Show(properties, 650, 576);
                    try
                    {
                        Capture(window, $"time-series-data-{shot.Name}-{theme.ToString().ToLowerInvariant()}");
                    }
                    finally
                    {
                        window.Close();
                        properties.Element = null;
                        CloseProject(project);
                    }
                }
            }
        }
    }
}
