using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the shared analysis controls in the Light, Dark and Blue themes, on the
    /// example projects, in <c>artifacts/screenshots/shared-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare the plots with RMC-BestFit's native exports of the same examples, for instance
    /// <c>sample-data/RMC-BestFit/examples/4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/screenshots/viglione-et-al-2013-{kernel-density,trace,autocorrelation}-location.png</c>
    /// (the native exports are the plot alone, 1000 x 650 with a white background; the captures show the
    /// controls: selectors, plot and toolbar). Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class SharedAnalysisControlsCaptures
    {
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        /// <summary>Removes the application's resources and sets the Light theme back.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            ReleaseAppResources();
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheDiagnosticPlots_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                var project = Open(SharedAnalysisControlsTests.Viglione);
                try
                {
                    var element = Element<UnivariateAnalysis>(project, SharedAnalysisControlsTests.ViglioneElement);
                    var plots = element.BayesianPlots;
                    var analysis = element.BayesianAnalysis;
                    CapturePlot(element, theme, "kernel-density", new KernelDensityControl(), c => c.SetPlot(plots.KernelDensityPlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "histogram", new HistogramControl(), c => c.SetPlot(plots.HistogramPlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "trace", new MarkovChainTraceControl(), c => c.SetPlot(plots.MarkovChainTracePlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "autocorrelation", new AutocorrelationControl(), c => c.SetPlot(plots.AutocorrelationPlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "mean-likelihood", new MeanLikelihoodControl(), c => c.SetPlot(plots.MeanLikelihoodPlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "bivariate-heat-map", new BivariateHeatMapControl(), c => c.SetPlot(plots.BivariateHeatMapPlot), c => c.Analysis = analysis);
                    CapturePlot(element, theme, "influence", new InfluenceDiagnosticsControl(), c => c.SetPlot(plots.InfluenceDiagnosticsPlot), c => c.Analysis = analysis);
                }
                finally
                {
                    Close(project);
                }
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheTablesAndReports_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                var viglione = Open(SharedAnalysisControlsTests.Viglione);
                var b17c = Open(SharedAnalysisControlsTests.B17C);
                var waimea = Open(SharedAnalysisControlsTests.Waimea);
                try
                {
                    var element = Element<UnivariateAnalysis>(viglione, SharedAnalysisControlsTests.ViglioneElement);
                    CaptureControl(theme, "parameter-sets", new ParameterSetsControl { Analysis = element.BayesianAnalysis }, 800, 500);
                    CaptureControl(theme, "mcmc-report", new MCMCReportControl { Analysis = element.BayesianAnalysis }, 900, 600);
                    CaptureControl(theme, "gmm-report", new GMMReportControl { Element = Element<B17CAnalysis>(b17c, "Example #1") }, 900, 600);
                    var cfa = Element<CoincidentFrequencyAnalysis>(waimea, "CFA - Normal - Conditional");
                    CaptureControl(theme, "ordinates", PropertiesPanel(new OrdinatesControl { RowItemType = typeof(XOrdinateRowItem), Ordinates = cfa.XValues }), 320, 420);
                }
                finally
                {
                    Close(viglione);
                    Close(b17c);
                    Close(waimea);
                }
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureThePropertiesControls_InEveryTheme()
        {
            OverlayPopups();
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                var project = Open(SharedAnalysisControlsTests.Viglione);
                try
                {
                    var element = Element<UnivariateAnalysis>(project, SharedAnalysisControlsTests.ViglioneElement);
                    var three = Element<UnivariateAnalysis>(project, SharedAnalysisControlsTests.ViglioneQuantilePriors);
                    CaptureControl(theme, "bayesian-options", PropertiesPanel(Expanded(new BayesianOptionsControl { Analysis = element.BayesianAnalysis })), 320, 560);
                    CaptureControl(theme, "bayesian-output", PropertiesPanel(new BayesianOutputControl { Analysis = element.BayesianAnalysis }), 320, 170);
                    CaptureControl(theme, "parameter-priors", PropertiesPanel(new ParameterPriorsControl { Model = three.UnivariateDistribution }), 320, 170);
                    CaptureControl(theme, "quantile-priors", PropertiesPanel(new QuantilePriorsControl { Model = three.UnivariateDistribution }), 320, 190);

                    // The prior's editor, open beside its parameter as a user's click opens it, once the default
                    // flat priors are off (they disable the editors).
                    var priors = new ParameterPriorsControl { Model = three.UnivariateDistribution };
                    var panel = new Grid { ColumnDefinitions = new ColumnDefinitions("320,*"), Children = { PropertiesPanel(priors) } };
                    Window window = Show(panel, 720, 560);
                    try
                    {
                        priors.UsePriorDistributionDefaults.IsSelected = false;
                        Settle();
                        var toggle = priors.PriorDistributionsDataGrid.GetCell(0, 1).GetVisualDescendants().OfType<ToggleButton>().Single();
                        Assert.IsTrue(toggle.IsEffectivelyEnabled);
                        toggle.IsChecked = true;
                        Settle();
                        Capture(window, $"shared-parameter-priors-popup-{Name(theme)}");
                        toggle.IsChecked = false;
                        Settle();
                    }
                    finally
                    {
                        window.Close();
                    }
                }
                finally
                {
                    Close(project);
                }
            }
        }

        private static void CapturePlot<T>(IElement element, ThemeColor theme, string name, T control, Action<T> setPlot, Action<T> setAnalysis) where T : Control
        {
            using (Suspend(element)) setPlot(control);
            setAnalysis(control);
            CaptureControl(theme, name, control, 1000, 650);
        }

        private static void CaptureControl(ThemeColor theme, string name, Control control, double width, double height)
        {
            Window window = Show(control, width, height);
            try
            {
                Capture(window, $"shared-{name}-{Name(theme)}");
            }
            finally
            {
                window.Close();
            }
        }

        // A properties panel's background and margins, as the properties views host these controls.
        private static Control PropertiesPanel(Control control)
        {
            var panel = new Border { Padding = new Thickness(5, 5, 0, 5), Child = control, VerticalAlignment = VerticalAlignment.Stretch };
            panel.Bind(Border.BackgroundProperty, panel.GetResourceObservable("PropertyGrid.Content.Background"));
            return panel;
        }

        // The options' advanced expander open, so the capture shows the sampler's options.
        private static BayesianOptionsControl Expanded(BayesianOptionsControl options)
        {
            options.AttachedToVisualTree += (_, _) =>
            {
                foreach (var expander in options.GetLogicalDescendants().OfType<Expander>())
                    expander.IsExpanded = true;
            };
            return options;
        }

        private static IDisposable Suspend(object element) =>
            (IDisposable)element.GetType().GetMethod("SuspendPlotBridges").Invoke(element, null);

        private static string Name(ThemeColor theme) => theme.ToString().ToLowerInvariant();
    }

}
