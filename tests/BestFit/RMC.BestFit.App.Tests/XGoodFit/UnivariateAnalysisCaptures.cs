using System.Linq;
using Avalonia.Controls;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the Univariate and Bulletin 17C analysis views and their properties panels in
    /// the Light, Dark and Blue themes, on the univariate examples' fits, in
    /// <c>artifacts/screenshots/{univariate,b17c}-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native exports of the same fits, beside the examples
    /// (<c>sample-data/RMC-BestFit/examples/4-univariate-distribution-analysis/**/screenshots/*.png</c>: the plot alone,
    /// on white, from factory-reset plots, so their styling isn't the saved plots'), and the walkthroughs' figures
    /// (<c>images/*.png</c>, drawn from the desktop's coordinates). UnivariateAnalysisShellTests captures the main
    /// window with the analyses open. Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class UnivariateAnalysisCaptures
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
        public void CaptureTheUnivariateView_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                OverlayPopups();
                string suffix = "-" + theme.ToString().ToLowerInvariant();
                var project = Open(UnivariateAnalysisViewTests.ArrFlike);
                try
                {
                    // Example #3: native exports of its frequency plot, kernel density, trace and autocorrelation.
                    var element = Element<UnivariateAnalysis>(project, "Example #3");
                    var view = new UnivariateAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "univariate-frequency" + suffix);
                    Select(UnivariateAnalysisViewTests.TabularResults(view));
                    Capture(window, "univariate-tables" + suffix);
                    Select(view.KernelDensityTabItem);
                    Capture(window, "univariate-kernel-density" + suffix);
                    Select(view.MarkovChainTabItem);
                    Capture(window, "univariate-trace" + suffix);
                    Select(view.AutocorrelationTabItem);
                    Capture(window, "univariate-autocorrelation" + suffix);
                    Select(view.ParameterSetsTabItem);
                    Capture(window, "univariate-parameter-sets" + suffix);
                    Select(view.MCMCReportTabItem);
                    Capture(window, "univariate-mcmc-report" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);

                    // The properties panel, on each tab, as the shell's properties pane shows it.
                    var properties = new UnivariateAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "univariate-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "univariate-properties-options" + suffix);
                    Select(properties.Output_TabItem);
                    Capture(window, "univariate-properties-output" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                    view.Element = null;
                }
                finally
                {
                    Close(project);
                }

                // A nonstationary fit: its chronology (the walkthrough's linear trend figure) and its trend models.
                project = Open(UnivariateAnalysisViewTests.BraysBayou);
                try
                {
                    var element = Element<UnivariateAnalysis>(project, "NSFFA - Linear");
                    var view = new UnivariateAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Select(view.ChronologyResultsTabItem);
                    Capture(window, "univariate-chronology" + suffix);
                    Select(view.FrequencyResultsTabItem);
                    view.ShowFilterToggleButton.IsChecked = true;
                    view.AlternativeSelector.AnalysisList.Single(i => i.Alternative.Name == "NSFFA - Constant").IsChecked = true;
                    Settle();
                    Capture(window, "univariate-frequency-alternative" + suffix);
                    foreach (var item in view.AlternativeSelector.AnalysisList) item.IsChecked = false;
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    var properties = new UnivariateAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Select(properties.Options_TabItem);
                    Capture(window, "univariate-properties-options-nonstationary" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                    view.Element = null;
                }
                finally
                {
                    Close(project);
                }
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheB17CView_InEveryTheme()
        {
            foreach (ThemeColor theme in Themes)
            {
                UseAppResources(theme);
                OverlayPopups();
                string suffix = "-" + theme.ToString().ToLowerInvariant();
                var project = Open(B17CAnalysisViewTests.Blakely);
                try
                {
                    // The paleoflood fit with a quantile penalty (the walkthrough's penalty figure).
                    var element = Element<B17CAnalysis>(project, "Systematic + Historical + Paleo + Reg Skew + QPen");
                    var view = new B17CAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "b17c-frequency" + suffix);
                    Select(((TabControl)view.FrequencyResultsTabItem.Parent).Items.Cast<TabItem>().Last());
                    Capture(window, "b17c-tables" + suffix);
                    Select(view.KernelDensityTabItem);
                    Capture(window, "b17c-kernel-density" + suffix);
                    Select(view.InfluenceTabItem);
                    Capture(window, "b17c-influence" + suffix);
                    Select(view.GMMReportTabItem);
                    Capture(window, "b17c-gmm-report" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);

                    var properties = new B17CAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "b17c-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "b17c-properties-options" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                    view.Element = null;
                }
                finally
                {
                    Close(project);
                }
            }
        }
    }
}
