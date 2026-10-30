using System.Linq;
using Avalonia.Controls;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.RatingCurveTimeSeriesTesting;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;
using static RMC.BestFit.App.Tests.XGoodFit.UnivariateAnalysisTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the Rating Curve and Time Series Analysis views and their properties panels in the
    /// Light, Dark and Blue themes, on the examples' fits, in
    /// <c>artifacts/screenshots/{rating-curve,time-series-analysis}-*-{theme}.png</c>.
    /// </summary>
    /// <remarks>
    /// Compare with RMC-BestFit's native exports of the same fits, beside the examples
    /// (<c>sample-data/RMC-BestFit/examples/{6-rating-curve-analysis,7-time-series-analysis/*}/screenshots/*.png</c>: the
    /// plot alone, on white, from factory-reset plots, so their styling isn't the saved plots'), and the walkthroughs'
    /// figures (<c>images/*.png</c>, drawn from the desktop's coordinates). RatingCurveTimeSeriesAnalysisShellTests
    /// captures the main window with the analyses open. Run them alone with the filter TestCategory=Capture.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class RatingCurveTimeSeriesAnalysisCaptures
    {
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        private string _valueFormat;

        /// <summary>Shows values in the application's default format, two decimal places, as the main window does.</summary>
        [TestInitialize]
        public void UseDefaultValueFormat()
        {
            _valueFormat = UserSettings.ValueStringFormat;
            UserSettings.DefaultValueDigits = 2;
        }

        /// <summary>Removes the application's resources, and sets the Light theme and the value format back.</summary>
        [TestCleanup]
        public void Cleanup()
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            ReleaseAppResources();
            UserSettings.ValueStringFormat = _valueFormat;
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheRatingCurveView_InEveryTheme()
        {
            var mississippi = Open(Mississippi);
            var synthetic = Open(SyntheticRatings);
            try
            {
                foreach (ThemeColor theme in Themes)
                {
                    UseAppResources(theme);
                    OverlayPopups();
                    string suffix = "-" + theme.ToString().ToLowerInvariant();

                    // New Madrid: native exports of its rating curve, residuals and zero-flow stage trace.
                    var element = Element<RatingCurveAnalysis>(mississippi, NewMadrid);
                    var view = new RatingCurveAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "rating-curve-plot" + suffix);
                    Select(RatingCurveAnalysisViewTests.TabularResults(view));
                    Capture(window, "rating-curve-tables" + suffix);
                    Select(view.ResidualsTabItem);
                    Capture(window, "rating-curve-residuals" + suffix);
                    Select(view.ResidualHistogramTab);
                    Capture(window, "rating-curve-residual-histogram" + suffix);
                    Select(view.ResidualQQTab);
                    Capture(window, "rating-curve-residual-qq" + suffix);
                    Select(view.ResidualsPlotTab);
                    Select(view.MarkovChainTabItem);
                    Capture(window, "rating-curve-trace" + suffix);
                    Select(view.MCMCReportTabItem);
                    Capture(window, "rating-curve-mcmc-report" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;

                    var properties = new RatingCurveAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "rating-curve-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "rating-curve-properties-options" + suffix);
                    Select(properties.Output_TabItem);
                    Capture(window, "rating-curve-properties-output" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;

                    // The three-control synthetic rating, with the one- and two-control ratings as alternatives.
                    var three = Element<RatingCurveAnalysis>(synthetic, ThreeSegments);
                    view = new RatingCurveAnalysisControl { Element = three };
                    window = Show(view, 1185, 760);
                    view.ShowFilterToggleButton.IsChecked = true;
                    foreach (var item in view.AlternativeSelector.AnalysisList) item.IsChecked = true;
                    Settle();
                    Capture(window, "rating-curve-three-controls-alternatives" + suffix);
                    foreach (var item in view.AlternativeSelector.AnalysisList) item.IsChecked = false;
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                }
            }
            finally
            {
                Close(mississippi);
                Close(synthetic);
            }
        }

        [STATestMethod]
        [TestCategory("Capture")]
        public void CaptureTheTimeSeriesAnalysisView_InEveryTheme()
        {
            var classic = Open(ClassicSeries);
            var regression = Open(Regression);
            try
            {
                foreach (ThemeColor theme in Themes)
                {
                    UseAppResources(theme);
                    OverlayPopups();
                    string suffix = "-" + theme.ToString().ToLowerInvariant();

                    // The airline passengers: native exports of its series plot and intercept chain ACF.
                    var element = Element<TimeSeriesAnalysis>(classic, Airline);
                    var view = new TimeSeriesAnalysisControl { Element = element };
                    Window window = Show(view, 1185, 760);
                    Capture(window, "time-series-analysis-plot" + suffix);
                    Select(TimeSeriesAnalysisViewTests.TabularResults(view));
                    Capture(window, "time-series-analysis-tables" + suffix);
                    Select(view.ResidualsTabItem);
                    Capture(window, "time-series-analysis-residuals" + suffix);
                    Select(view.ResidualACFTab);
                    Capture(window, "time-series-analysis-residual-acf" + suffix);
                    Select(view.ResidualPACFTab);
                    Capture(window, "time-series-analysis-residual-pacf" + suffix);
                    Select(view.ResidualsPlotTab);
                    Select(view.AutocorrelationTabItem);
                    Capture(window, "time-series-analysis-autocorrelation" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;

                    var properties = new TimeSeriesAnalysisPropertiesControl { Element = element };
                    window = Show(properties, 420, 1000);
                    Capture(window, "time-series-analysis-properties-general" + suffix);
                    Select(properties.Options_TabItem);
                    Capture(window, "time-series-analysis-properties-options" + suffix);
                    Select(properties.Output_TabItem);
                    Capture(window, "time-series-analysis-properties-output" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;

                    // Mauna Loa's seasonal fit with its forecast-free validation window, and the multiple regression's
                    // prediction and covariates.
                    var co2 = Element<TimeSeriesAnalysis>(classic, MaunaLoa);
                    view = new TimeSeriesAnalysisControl { Element = co2 };
                    window = Show(view, 1185, 760);
                    Capture(window, "time-series-analysis-mauna-loa" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                    var multiple = Element<TimeSeriesAnalysis>(regression, MultipleRegression);
                    view = new TimeSeriesAnalysisControl { Element = multiple };
                    window = Show(view, 1185, 760);
                    Capture(window, "time-series-analysis-regression" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    view.Element = null;
                    properties = new TimeSeriesAnalysisPropertiesControl { Element = multiple };
                    window = Show(properties, 420, 1000);
                    Select(properties.Options_TabItem);
                    Capture(window, "time-series-analysis-regression-covariates" + suffix);
                    UnivariateAnalysisViewTests.CloseDocument(window);
                    properties.Element = null;
                }
            }
            finally
            {
                Close(classic);
                Close(regression);
            }
        }
    }
}
