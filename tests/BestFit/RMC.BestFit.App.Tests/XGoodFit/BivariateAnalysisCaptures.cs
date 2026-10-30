using System.Linq;
using Avalonia.Controls;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the Bivariate Distribution Analysis view and its properties panel in the
    /// Light, Dark and Blue themes, on the bivariate example's fits, in <c>artifacts/screenshots/bivariate-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native exports of the AMH fit,
    /// <c>sample-data/RMC-BestFit/examples/5-bivariate-distribution-analysis/1-bivariate-distributions/screenshots/amh-{copula-scatter,dependence-kde,dependence-trace}.png</c>
    /// (the plot alone, 1000 x 650 on white; the captures show the view, with its tabs, selectors and
    /// toolbar, and the plot at about that size), and the walkthrough's figures of the Normal fit's contours
    /// (<c>images/bivariate-distribution-examples-normal-{density,joint-exceedance}.png</c>, drawn from the
    /// desktop's coordinates). BivariateAnalysisShellTests captures the main window with the analysis open. Run
    /// them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class BivariateAnalysisCaptures
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
        public void CaptureTheView_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                OverlayPopups();
                string suffix = "-" + theme.ToString().ToLowerInvariant();
                var project = Open(BivariateAnalysisViewTests.Examples);
                try
                {
                    var amh = Element<BivariateAnalysis>(project, "AMH Copula");
                    var view = new BivariateAnalysisControl { Element = amh };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "bivariate-xy-scatter" + suffix);
                    view.AxisTypeComboBox.SelectedIndex = 1;
                    Settle();
                    Capture(window, "bivariate-xy-scatter-probability" + suffix);
                    view.AxisTypeComboBox.SelectedIndex = 0;
                    BivariateAnalysisViewTests.Select(BivariateAnalysisViewTests.InnerTab(view, 1));
                    Capture(window, "bivariate-tables" + suffix);
                    BivariateAnalysisViewTests.Select(view.KernelDensityTabItem);
                    Capture(window, "bivariate-kernel-density" + suffix);
                    BivariateAnalysisViewTests.Select(view.MarkovChainTabItem);
                    Capture(window, "bivariate-trace" + suffix);
                    BivariateAnalysisViewTests.Select(view.ParameterSetsTabItem);
                    Capture(window, "bivariate-parameter-sets" + suffix);
                    BivariateAnalysisViewTests.Select(view.MCMCReportTabItem);
                    Capture(window, "bivariate-mcmc-report" + suffix);
                    window.Close();

                    var normal = Element<BivariateAnalysis>(project, "Normal Copula");
                    var normalView = new BivariateAnalysisControl { Element = normal };
                    window = Show(normalView, 1185, 760);
                    normalView.PlotTypeComboBox.SelectedIndex = 1;
                    Settle();
                    Capture(window, "bivariate-xy-density" + suffix);
                    normalView.PlotTypeComboBox.SelectedIndex = 2;
                    Settle();
                    Capture(window, "bivariate-xy-joint-exceedance" + suffix);
                    window.Close();

                    // The properties panel, on each tab, as the shell's properties pane shows it.
                    var properties = new BivariateAnalysisPropertiesControl { Element = amh };
                    window = Show(properties, 420, 1000);
                    Capture(window, "bivariate-properties-general" + suffix);
                    BivariateAnalysisViewTests.Select(properties.Options_TabItem);
                    Capture(window, "bivariate-properties-options" + suffix);
                    BivariateAnalysisViewTests.Select(properties.Output_TabItem);
                    Capture(window, "bivariate-properties-output" + suffix);
                    window.Close();
                    properties.Element = null;
                }
                finally
                {
                    Close(project);
                }
            }
        }
    }
}
