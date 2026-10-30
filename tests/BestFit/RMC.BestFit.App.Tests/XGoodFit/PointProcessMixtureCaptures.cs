using System.Linq;
using Avalonia.Controls;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.PointProcessMixtureTesting;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the Point Process and Mixture analysis views and their properties panels in the
    /// Light, Dark and Blue themes, on the examples' fits, in <c>artifacts/screenshots/{point-process,mixture}-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native exports of the same fits, beside the examples
    /// (<c>sample-data/RMC-BestFit/examples/4-univariate-distribution-analysis/{3-point-process-analysis,4-mixture-analysis}/screenshots/*.png</c>:
    /// the plot alone, on white, from factory-reset plots, so their styling isn't the saved plots'), and the
    /// walkthroughs' figures (<c>images/*.png</c>, drawn from the desktop's coordinates). PointProcessMixtureShellTests
    /// captures the main window with the analyses open. Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class PointProcessMixtureCaptures
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
        public void CaptureThePointProcessView_InEveryTheme()
        {
            var project = Open(PointProcessExample);
            try
            {
                foreach (ThemeColor theme in Themes)
                {
                    UseAppResources(theme);
                    OverlayPopups();
                    string suffix = "-" + theme.ToString().ToLowerInvariant();

                    // Big Bear Lake: native exports of its frequency plot (POT and AMS data), kernel density, trace and
                    // autocorrelation of the location.
                    var element = Element<PointProcessAnalysis>(project, BigBear);
                    var view = new PointProcessAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "point-process-frequency" + suffix);
                    Select(PointProcessAnalysisViewTests.TabularResults(view));
                    Capture(window, "point-process-tables" + suffix);
                    Select(view.KernelDensityTabItem);
                    Capture(window, "point-process-kernel-density" + suffix);
                    Select(view.MarkovChainTabItem);
                    Capture(window, "point-process-trace" + suffix);
                    Select(view.AutocorrelationTabItem);
                    Capture(window, "point-process-autocorrelation" + suffix);
                    Select(view.MCMCReportTabItem);
                    Capture(window, "point-process-mcmc-report" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;

                    var properties = new PointProcessAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "point-process-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "point-process-properties-options" + suffix);
                    Select(properties.Output_TabItem);
                    Capture(window, "point-process-properties-output" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;

                    // The two-season fit: its seasons' curves (the walkthrough's figure), and its block year.
                    var seasonal = Element<PointProcessAnalysis>(project, Seasonal);
                    view = new PointProcessAnalysisControl { Element = seasonal };
                    window = Show(view, 1185, 760);
                    Capture(window, "point-process-seasonal-frequency" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                    properties = new PointProcessAnalysisPropertiesControl { Element = seasonal };
                    window = Show(properties, 420, 1000);
                    Capture(window, "point-process-properties-seasonal" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                }
            }
            finally
            {
                Close(project);
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheMixtureView_InEveryTheme()
        {
            var project = Open(MixtureExample);
            try
            {
                foreach (ThemeColor theme in Themes)
                {
                    UseAppResources(theme);
                    OverlayPopups();
                    string suffix = "-" + theme.ToString().ToLowerInvariant();

                    // The two-Normal fit: native exports of its frequency plot, and the first weight's kernel density,
                    // trace and autocorrelation.
                    var element = Element<MixtureAnalysis>(project, TwoNormals);
                    var view = new MixtureAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "mixture-frequency" + suffix);
                    Select(MixtureAnalysisViewTests.TabularResults(view));
                    Capture(window, "mixture-tables" + suffix);
                    Select(view.KernelDensityTabItem);
                    Capture(window, "mixture-kernel-density" + suffix);
                    Select(view.MarkovChainTabItem);
                    Capture(window, "mixture-trace" + suffix);
                    Select(view.ParameterSetsTabItem);
                    Capture(window, "mixture-parameter-sets" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;

                    // The panel with its component grid, on each tab.
                    var properties = new MixtureAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "mixture-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "mixture-properties-options" + suffix);
                    Select(properties.Output_TabItem);
                    Capture(window, "mixture-properties-output" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;

                    // The zero-inflated fit (its zeros on the saved linear axis, and its zero weight), and the
                    // three-Normal fit with the two-Normal one as an alternative.
                    var zero = Element<MixtureAnalysis>(project, ZeroInflated);
                    view = new MixtureAnalysisControl { Element = zero };
                    window = Show(view, 1185, 760);
                    Capture(window, "mixture-zero-inflated-frequency" + suffix);
                    Select(MixtureAnalysisViewTests.TabularResults(view));
                    Capture(window, "mixture-zero-inflated-tables" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                    var three = Element<MixtureAnalysis>(project, ThreeNormals);
                    view = new MixtureAnalysisControl { Element = three };
                    window = Show(view, 1185, 760);
                    view.ShowFilterToggleButton.IsChecked = true;
                    view.AlternativeSelector.AnalysisList.Single(i => i.Alternative.Name == TwoNormals).IsChecked = true;
                    Settle();
                    Capture(window, "mixture-three-normals-alternative" + suffix);
                    foreach (var item in view.AlternativeSelector.AnalysisList) item.IsChecked = false;
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                }
            }
            finally
            {
                Close(project);
            }
        }
    }
}
