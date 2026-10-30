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
    /// XGoodFit addition: captures of the Coincident Frequency Analysis view and its properties panel in the Light,
    /// Dark and Blue themes, on the sum-of-two-Normals example's near-zero correlation analysis, in
    /// <c>artifacts/screenshots/coincident-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native export of that analysis,
    /// <c>sample-data/RMC-BestFit/examples/5-bivariate-distribution-analysis/2-coincident-frequency/screenshots/sum-near-zero-response.png</c>
    /// (the plot alone, 1000 x 650 on white; the captures show the view, with its tabs and toolbar, and the plot at
    /// about that size), and the walkthrough's figure of it (<c>images/sum-two-normals-near-zero-response.png</c>).
    /// Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CoincidentFrequencyCaptures
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
            var project = Open(CoincidentFrequencyViewTests.SumOfNormals);
            // The application's default settings: values with two decimal places.
            string valueFormat = UserSettings.ValueStringFormat;
            UserSettings.DefaultValueDigits = 2;
            try
            {
                foreach (ThemeColor theme in Themes)
                {
                    UseAppResources(theme);
                    OverlayPopups();
                    string suffix = "-" + theme.ToString().ToLowerInvariant();
                    var element = Element<CoincidentFrequencyAnalysis>(project, CoincidentFrequencyViewTests.NearZero);
                    var view = new CoincidentFrequencyControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "coincident-response-grid" + suffix);
                    CoincidentFrequencyViewTests.Select(view, 1);
                    Capture(window, "coincident-frequency" + suffix);
                    CoincidentFrequencyViewTests.Select(view, 2);
                    Capture(window, "coincident-tables" + suffix);
                    CompositeAnalysisViewTests.CloseDocument(window);

                    // The properties panel, on each tab, as the shell's properties pane shows it.
                    var properties = new CoincidentFrequencyPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "coincident-properties-general" + suffix);
                    BivariateAnalysisViewTests.Select(properties.Output_TabItem);
                    Capture(window, "coincident-properties-output" + suffix);
                    CompositeAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                }
            }
            finally
            {
                UserSettings.ValueStringFormat = valueFormat;
                Close(project);
            }
        }
    }
}
