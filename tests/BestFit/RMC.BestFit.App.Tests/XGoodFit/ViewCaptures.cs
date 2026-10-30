using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: renders the Input Data and Distribution Fitting Analysis views of RMC-BestFit's examples in
    /// the Light, Blue and Dark themes to artifacts/screenshots, to compare with the examples' native screenshots
    /// (screenshots/*-native.png for plots, *-current.png for properties) and with
    /// upstream/RMC-BestFit/docs/images/rmc-bestfit-gui.png. Run them alone with the filter TestCategory=Capture.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ViewCaptures
    {
        private const string BlockMaximum = "2-input-data/1-block-maximum/usgs-block-max-example.bestfit";
        private const string PeakDischarge = "2-input-data/2-usgs-peak-discharge/usgs-peak-download-example.bestfit";
        private const string UsgsPot = "2-input-data/3-peaks-over-threshold/usgs-peaks-over-threshold-example.bestfit";

        /// <summary>The themes, by their colour dictionaries' names.</summary>
        internal static readonly string[] Themes = { "Light", "Blue", "Dark" };

        private IDisposable _themeDefaults;

        private IDisposable _culture;

        /// <summary>Sets up the shared resources, the theme defaults and popups in the window (so captures show them).</summary>
        [TestInitialize]
        public void Initialize()
        {
            OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            _ = typeof(InputDataControl).Assembly;
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

        /// <summary>Each tab and plot of the block maxima's input data, in each theme.</summary>
        [STATestMethod]
        [TestCategory("Capture")]
        public void InputData_EveryTabAndPlot()
        {
            using var opened = ViewTesting.Open(BlockMaximum);
            var element = opened.Element<InputData>("USGS - 01134500 - Block Max - Calendar Year");
            ForEachTheme(theme =>
            {
                var control = new InputDataControl { Element = element };
                Window window = ViewTesting.Show(control, 1200, 700);
                try
                {
                    Save(window, $"input-data-chronology-{theme}");
                    control.FindControl<RadioButton>("FrequencyPlotRadioButton").IsChecked = true;
                    Save(window, $"input-data-frequency-{theme}");
                    control.FindControl<RadioButton>("ChronologyPlotRadioButton").IsChecked = true;
                    foreach (string tab in new[] { "SummaryStatisticsTab", "HypothesisTestsTab", "SeasonalityTab", "DensityTab", "HistogramTab", "QQTab", "ACFTab", "PACFTab" })
                    {
                        control.FindControl<TabItem>(tab).IsSelected = true;
                        Save(window, $"input-data-{tab.Replace("Tab", "").ToLowerInvariant()}-{theme}");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>The peaks-over-threshold diagnostics and the USGS text file tab, in each theme.</summary>
        [STATestMethod]
        [TestCategory("Capture")]
        public void InputData_ThresholdDiagnosticsAndUsgsText()
        {
            using var pot = ViewTesting.Open(UsgsPot);
            using var peak = ViewTesting.Open(PeakDischarge);
            var potElement = pot.Element<InputData>("USGS - 11274500 - Peaks-Over-Threshold");
            var peakElement = peak.Element<InputData>("USGS - 01134500 - Peak Discharge");
            ForEachTheme(theme =>
            {
                var control = new InputDataControl { Element = potElement };
                Window window = ViewTesting.Show(control, 1200, 700);
                try
                {
                    control.FindControl<TabItem>("ThresholdDiagnosticsTab").IsSelected = true;
                    Save(window, $"input-data-pot-mrl-{theme}");
                    control.FindControl<RadioButton>("ModifiedScalePlotRadioButton").IsChecked = true;
                    Save(window, $"input-data-pot-modified-scale-{theme}");
                    control.FindControl<RadioButton>("ShapePlotRadioButton").IsChecked = true;
                    Save(window, $"input-data-pot-shape-{theme}");
                    control.FindControl<RadioButton>("MRLPlotRadioButton").IsChecked = true;

                    var usgs = new InputDataControl { Element = peakElement };
                    window.Content = usgs;
                    ViewTesting.Settle();
                    usgs.FindControl<TabItem>("USGSTextFileTab").IsSelected = true;
                    Save(window, $"input-data-usgs-text-{theme}");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>The input data properties of the three native properties screenshots, in each theme.</summary>
        [STATestMethod]
        [TestCategory("Capture")]
        public void InputData_Properties()
        {
            var examples = new (string Example, string Element, string Name)[]
            {
                (BlockMaximum, "USGS - 01134500 - Block Max - Calendar Year", "block-max"),
                (PeakDischarge, "USGS - 01134500 - Peak Discharge", "peak-download"),
                (UsgsPot, "USGS - 11274500 - Peaks-Over-Threshold", "usgs-pot"),
            };
            foreach (var (example, name, label) in examples)
            {
                using var opened = ViewTesting.Open(example);
                var element = opened.Element<InputData>(name);
                ForEachTheme(theme =>
                {
                    Window window = ViewTesting.Show(new InputDataPropertiesControl { Element = element }, 650, 800);
                    try
                    {
                        Save(window, $"input-data-properties-{label}-{theme}");
                    }
                    finally
                    {
                        window.Close();
                    }
                });
            }
        }

        /// <summary>Each plot and table of a distribution fitting analysis, and its properties, in each theme.</summary>
        [STATestMethod]
        [TestCategory("Capture")]
        public void FittingAnalysis_EveryPlotTableAndProperties()
        {
            using var opened = ViewTesting.Open(FittingAnalysisViewTests.Viglione);
            var element = opened.Elements<FittingAnalysis>().First();
            ForEachTheme(theme =>
            {
                var control = new FittingAnalysisControl { Element = element };
                Window window = ViewTesting.Show(control, 1200, 700);
                try
                {
                    Save(window, $"fitting-analysis-frequency-{theme}");
                    foreach (string plot in new[] { "PDF", "CDF", "PP", "QQ" })
                    {
                        control.FindControl<RadioButton>(plot + "PlotRadioButton").IsChecked = true;
                        Save(window, $"fitting-analysis-{plot.ToLowerInvariant()}-{theme}");
                    }
                    control.FindControl<TabItem>("TabularResultsTab").IsSelected = true;
                    Save(window, $"fitting-analysis-tabular-{theme}");

                    var properties = new FittingAnalysisPropertiesControl { Element = element };
                    window.Width = 400;
                    window.Height = 700;
                    window.Content = properties;
                    Save(window, $"fitting-analysis-properties-general-{theme}");
                    properties.FindControl<TabItem>("Options_TabItem").IsSelected = true;
                    Save(window, $"fitting-analysis-properties-options-{theme}");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // Runs an action in each theme, with the theme's colour dictionaries over the light ones WpfTestHost merges
        // (as ThemeService swaps them) and Avalonia's dark variant for the dark theme.
        internal static void ForEachTheme(Action<string> action)
        {
            foreach (string theme in Themes)
            {
                var added = new List<ResourceInclude>();
                if (theme != "Light")
                {
                    foreach (var (assembly, path) in new[] { ("Themes", $"Resources/Colors/{theme}Colors.axaml"), ("FrameworkUI", $"Themes/VS2013/{theme}Theme.axaml") })
                    {
                        var source = new Uri($"avares://{assembly}/{path}");
                        var dictionary = new ResourceInclude(source) { Source = source };
                        Application.Current.Resources.MergedDictionaries.Add(dictionary);
                        added.Add(dictionary);
                    }
                }
                Application.Current.RequestedThemeVariant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
                try
                {
                    action(theme);
                }
                finally
                {
                    foreach (var dictionary in added)
                        Application.Current.Resources.MergedDictionaries.Remove(dictionary);
                    Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                }
            }
        }

        // Renders the window to artifacts/screenshots/<name>.png, once layout, bindings and the plots have settled.
        internal static void Save(Window window, string name)
        {
            ViewTesting.Settle();
            Thread.Sleep(150);
            ViewTesting.Settle();
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(Path.Combine(ScreenshotFolder(), name + ".png"), new PngBitmapEncoderOptions());
        }

        // The repository's artifacts/screenshots folder.
        internal static string ScreenshotFolder()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XGoodFit.slnx")))
                directory = directory.Parent;
            string folder = Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "artifacts", "screenshots");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }
}
