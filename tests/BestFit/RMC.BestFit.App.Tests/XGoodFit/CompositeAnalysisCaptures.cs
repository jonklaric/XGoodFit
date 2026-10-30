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
    /// XGoodFit addition: captures of the Composite Analysis view and its properties panel in the Light, Dark and
    /// Blue themes, on the composite example's competing-risks maximum and fixed-weight mixture, in
    /// <c>artifacts/screenshots/composite-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native export of the competing-risks composite,
    /// <c>sample-data/RMC-BestFit/examples/4-univariate-distribution-analysis/5-composite-analysis/screenshots/mixed-population-frequency.png</c>
    /// (the plot alone, 1000 x 650 on white; the captures show the view, with its tabs, check box and toolbar, and
    /// the plot at about that size), and the walkthrough's figures of both composites
    /// (<c>images/mixed-population-examples-{competing-maximum,fixed-mixture}.png</c>, drawn from the desktop's
    /// coordinates). Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CompositeAnalysisCaptures
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
            var project = Open(CompositeAnalysisViewTests.Example);
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
                    var competing = Element<CompositeAnalysis>(project, CompositeAnalysisViewTests.Competing);
                    var view = new CompositeAnalysisControl { Element = competing };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "composite-frequency" + suffix);
                    view.ShowFilterToggleButton.IsChecked = true;
                    Settle();
                    Capture(window, "composite-frequency-alternatives" + suffix);
                    view.ShowFilterToggleButton.IsChecked = false;
                    BivariateAnalysisViewTests.Select(CompositeAnalysisViewTests.Tab(view, 1));
                    Capture(window, "composite-tables" + suffix);
                    BivariateAnalysisViewTests.Select(CompositeAnalysisViewTests.Tab(view, 0));
                    CompositeAnalysisViewTests.CloseDocument(window);

                    var mixture = Element<CompositeAnalysis>(project, CompositeAnalysisViewTests.Mixture);
                    window = Show(new CompositeAnalysisControl { Element = mixture }, 1185, 760);
                    Capture(window, "composite-mixture-frequency" + suffix);
                    CompositeAnalysisViewTests.CloseDocument(window);

                    // The properties panel, on each tab, as the shell's properties pane shows it: the competing-risks
                    // composite's options, and the mixture's weights.
                    var properties = new CompositeAnalysisPropertiesControl { Element = competing };
                    window = Show(properties, 420, 1000);
                    Capture(window, "composite-properties-general" + suffix);
                    BivariateAnalysisViewTests.Select(properties.Options_TabItem);
                    Capture(window, "composite-properties-options" + suffix);
                    CompositeAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                    properties = new CompositeAnalysisPropertiesControl { Element = mixture };
                    window = Show(properties, 420, 1000);
                    Capture(window, "composite-properties-mixture" + suffix);
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
