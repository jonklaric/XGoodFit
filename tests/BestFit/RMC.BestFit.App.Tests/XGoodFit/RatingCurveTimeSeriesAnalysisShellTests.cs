using System;
using System.Collections.Generic;
using System.Linq;
using AvalonDock;
using AvalonDock.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using Numerics.Distributions;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using static RMC.BestFit.App.Tests.XGoodFit.RatingCurveTimeSeriesTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Rating Curve and Time Series Analysis views in the main window, as MainProjectNode opens them
    /// from the explorer, with their properties beside them, their plots' properties, closing and opening them again,
    /// and the Estimate button.
    /// </summary>
    /// <remarks>
    /// Each example opens once (a session of the main window), and the document opens three times in it. The buttons
    /// are clicked by raising their Click event: in the main window, moving the headless pointer over a plot toolbar lays
    /// the document out again, so a press can land on another button (porting guide, §7). The view tests click the views'
    /// toolbars with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class RatingCurveTimeSeriesAnalysisShellTests
    {
        private const string MississippiProject = "usgs-07024175-mississippi-rating-curve";
        private const string ClassicProject = "classic-time-series-examples";
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        /// <summary>
        /// The explorer opens the analysis's document with its properties, twice (a closed document opens again), and
        /// closing it changes nothing; then the Estimate button reruns the analysis, and its saved seed gives the saved
        /// fit again (the walkthroughs' DIC, curves and limits).
        /// </summary>
        [TestMethod]
        [DataRow(MississippiProject, NewMadrid)]
        [DataRow(ClassicProject, Nile)]
        public void TheDocument_ShowsTheAnalysis_AndOpensAgain_AndEstimateGivesTheSavedFit(string example, string name)
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(example));
                    IElement element = Analysis(project, name);
                    for (int opened = 0; opened < 2; opened++)
                        ShowAndClose(window, node, element);
                    Assert.IsFalse(project.IsDirty, "the project is unchanged");
                    Estimate(window, node, element);
                }
                finally
                {
                    Application.Current.MainWindow = null;
                    BestFitShell.Close(window);
                    ReleaseExample();
                }
            });
        }

        // Opens the element's document from the explorer, shows its plots' properties and every tab, and closes it.
        private static void ShowAndClose(MainWindow window, MainProjectNode node, IElement element)
        {
            // The explorer opens the document, with the element's properties beside it; a closed one opens again
            // (upstream's MainProjectNode leaves the closed view holding its element).
            window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
            Control view;
            Wpf.Plot main, density;
            Button plotProperties, densityProperties;
            List<TabItem> tabs;
            if (element is RatingCurveAnalysis rating)
            {
                var shown = Shown<RatingCurveAnalysisControl>(window);
                Assert.AreSame(rating, shown.Element);
                Assert.AreSame(rating, Shown<RatingCurveAnalysisPropertiesControl>(window).Element);
                (view, main) = (shown, rating.RatingCurvePlot);
                Assert.AreSame(main, shown.RatingCurvePlotHost.Content);
                Assert.AreSame(main, shown.GetCurrentPlot());
                plotProperties = shown.PlotToolbar.FindControl<Button>("PropertiesButton");
                UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), rating.BayesianPlots.KernelDensityPlot);
                tabs = ((TabControl)shown.ResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                UnivariateAnalysisTesting.Select(shown.ResultsTabItem);
                Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,StageDischargeData", string.Join(",", main.Series.Select(s => s.Name)));
            }
            else
            {
                var series = (TimeSeriesAnalysis)element;
                var shown = Shown<TimeSeriesAnalysisControl>(window);
                Assert.AreSame(series, shown.Element);
                Assert.AreSame(series, Shown<TimeSeriesAnalysisPropertiesControl>(window).Element);
                (view, main) = (shown, series.TimeSeriesPlot);
                Assert.AreSame(main, shown.TimeSeriesPlotHost.Content);
                Assert.AreSame(main, shown.GetCurrentPlot());
                plotProperties = shown.PlotToolbar.FindControl<Button>("PropertiesButton");
                UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), series.BayesianPlots.KernelDensityPlot);
                tabs = ((TabControl)shown.ResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                UnivariateAnalysisTesting.Select(shown.ResultsTabItem);
                Assert.AreEqual("TrainingCredibleIntervals,PredictionCredibleIntervals,PosteriorPredictive,PosteriorMode,TimeSeries", string.Join(",", main.Series.Select(s => s.Name)));
            }

            // The plot toolbar's Properties button shows the plot's properties in the panel, and closes them again,
            // which gives the analysis its properties back.
            Click(plotProperties);
            Assert.AreSame(main, Shown<OxyPlotPropertiesControl>(window).Plot);
            Assert.IsFalse(element.IsDirty, "showing the plot's properties changes nothing");
            Click(plotProperties);
            Assert.IsFalse(window.GetVisualDescendants().OfType<OxyPlotPropertiesControl>().Any(c => c.IsEffectivelyVisible), "the plot's properties closed");

            // A diagnostic plot's toolbar shows its plot's properties.
            UnivariateAnalysisTesting.Select(tabs.Single(t => (string)t.Header == "Kernel Density"));
            Click(densityProperties);
            Assert.AreSame(density, Shown<OxyPlotPropertiesControl>(window).Plot);
            Click(densityProperties);

            // Every tab, then the document closes: the analysis is as it was.
            foreach (TabItem tab in tabs) UnivariateAnalysisTesting.Select(tab);
            using (BestFitShell.WhenOpened<Window>(dialog => dialog.Close()))
                Documents(window).Single(d => d.Content == view).Close();
            BestFitShell.Frame();
            Assert.IsFalse(window.GetVisualDescendants().Any(v => v == view), "the document closed");
            Assert.IsFalse(element.IsDirty, element.Name + " is unchanged");
            Assert.IsFalse(((ElementBase)element).UndoManager.CanUndo, element.Name + " has nothing to undo");
        }

        // Opens the element's document again and runs its analysis with the Estimate button: the saved sampler settings
        // and seed give the saved fit, which the view shows.
        private static void Estimate(MainWindow window, MainProjectNode node, IElement element)
        {
            // Upstream's Estimate button disables the main window's menus and panes while the analysis runs.
            Application.Current.MainWindow = window;
            window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
            Button estimate, cancel;
            Control busy;
            Func<UncertaintyAnalysisResults> results;
            Func<IEnumerable<SummaryStatistic>> statistics;
            Wpf.Plot main;
            string series;
            if (element is RatingCurveAnalysis rating)
            {
                var view = Shown<RatingCurveAnalysisControl>(window);
                var properties = Shown<RatingCurveAnalysisPropertiesControl>(window);
                (estimate, cancel, busy) = (properties.EstimateButton, properties.CancelButton, properties.PropertiesExpander);
                (results, main) = (() => rating.AnalysisResults, rating.RatingCurvePlot);
                statistics = () => view.SummaryStatisticsTable.Items.Cast<SummaryStatistic>();
                series = "CredibleIntervals,PosteriorPredictive,PosteriorMode,StageDischargeData";
            }
            else
            {
                var analysis = (TimeSeriesAnalysis)element;
                var view = Shown<TimeSeriesAnalysisControl>(window);
                var properties = Shown<TimeSeriesAnalysisPropertiesControl>(window);
                (estimate, cancel, busy) = (properties.EstimateButton, properties.CancelButton, properties.PropertiesExpander);
                (results, main) = (() => analysis.AnalysisResults, analysis.TimeSeriesPlot);
                statistics = () => view.SummaryStatisticsTable.Items.Cast<SummaryStatistic>();
                series = "TrainingCredibleIntervals,PredictionCredibleIntervals,PosteriorPredictive,PosteriorMode,TimeSeries";
            }
            var saved = results();
            double dic = saved.DIC;
            double[] mode = (double[])saved.ModeCurve.Clone();
            double[,] intervals = (double[,])saved.ConfidenceIntervals.Clone();
            var dialogs = new List<string>();
            using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
            {
                Click(estimate);
                Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                Assert.AreEqual(Visibility.Visible, cancel.Visibility, "the Cancel button replaces Estimate while it runs");
                Assert.IsFalse(busy.IsEnabled, "the analysis's settings can't change while it runs");
                BestFitShell.WaitUntil(() => estimate.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
            }
            CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");

            Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
            Assert.AreEqual(Visibility.Hidden, cancel.Visibility);
            Assert.IsTrue(busy.IsEnabled);
            Assert.IsTrue(((IAnalysisElement)element).IsEstimated);
            var run = results();
            Assert.AreNotSame(saved, run, "a new run");
            UnivariateAnalysisTesting.AssertClose(dic, run.DIC, "the DIC");
            Assert.AreEqual(mode.Length, run.ModeCurve.Length);
            for (int i = 0; i < mode.Length; i++)
                UnivariateAnalysisTesting.AssertClose(mode[i], run.ModeCurve[i], "the point estimate's curve at " + i);
            for (int i = 0; i < intervals.GetLength(0); i++)
            {
                UnivariateAnalysisTesting.AssertClose(intervals[i, 1], run.ConfidenceIntervals[i, 1], "the lower limit at " + i);
                UnivariateAnalysisTesting.AssertClose(intervals[i, 2], run.ConfidenceIntervals[i, 2], "the upper limit at " + i);
            }
            BestFitShell.Frame();
            Assert.AreEqual(series, string.Join(",", main.Series.Select(s => s.Name)));
            var points = ((IEnumerable<OxyPlot.DataPoint>)main.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
            Assert.AreEqual(run.ModeCurve[0], element is RatingCurveAnalysis ? points[0].X : points[0].Y, "the view shows the new run");
            Assert.AreEqual(run.DIC, statistics().Single(s => s.Name == "DIC").Value, "the summary statistics show the new run");
            Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor, "the wait cursor is gone");
        }

        /// <summary>
        /// Captures the main window with the Rating Curve and Time Series analyses open in the Light, Dark and Blue themes,
        /// in artifacts/screenshots/{rating-curve,time-series-analysis}-main-{theme}.png.
        /// </summary>
        [TestMethod]
        [TestCategory("Capture")]
        [DataRow(MississippiProject, NewMadrid, "rating-curve")]
        [DataRow(ClassicProject, Airline, "time-series-analysis")]
        public void CaptureTheMainWindow_WithTheAnalysisOpen_InEveryTheme(string example, string name, string prefix)
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(example));
                    IElement element = Analysis(project, name);
                    window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
                    foreach (ThemeColor theme in Themes)
                    {
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        ElementNode.FindElementNode(element, node).IsSelected = true;
                        BestFitShell.Capture(window, prefix + "-main-" + theme.ToString().ToLowerInvariant());
                    }
                    ThemeManager.SetTheme(ThemeColor.Light);
                }
                finally
                {
                    BestFitShell.Close(window);
                    ReleaseExample();
                }
            });
        }

        // Lets the example go, which the shell's project otherwise keeps until the next test opens another
        // (BestFitShell.Close leaves it open), and collects it.
        private static void ReleaseExample()
        {
            BestFitProject.GetInstance().Close();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // Raises the button's Click, as a click on it does, and lets the shell respond.
        private static void Click(Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            BestFitShell.Frame();
        }

        private static IElement Analysis(BestFitProject project, string name) =>
            project.ElementCollections.Where(c => c is RatingCurveAnalysisCollection or TimeSeriesAnalysisCollection)
                .SelectMany(c => c).Single(e => e.Name == name);

        private static List<LayoutDocument> Documents(MainWindow window) =>
            window.FindControl<DockingManager>("MainDock").Layout.Descendents().OfType<LayoutDocument>().ToList();

        /// <summary>The one control of type T the window shows.</summary>
        private static T Shown<T>(Window window) where T : Control
        {
            BestFitShell.Frame();
            List<T> shown = window.GetVisualDescendants().OfType<T>().Where(c => c.IsEffectivelyVisible).ToList();
            Assert.AreEqual(1, shown.Count, typeof(T).Name + " shown");
            return shown[0];
        }
    }
}
