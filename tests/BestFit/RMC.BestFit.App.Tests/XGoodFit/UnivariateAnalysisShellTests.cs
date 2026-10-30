using System;
using System.Collections.Generic;
using System.Linq;
using AvalonDock;
using AvalonDock.Layout;
using FrameworkInterfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Univariate and Bulletin 17C analysis views in the main window, as MainProjectNode opens
    /// them from the explorer, with their properties beside them, their plots' properties, closing and opening them
    /// again, and the Estimate button.
    /// </summary>
    /// <remarks>
    /// The buttons are clicked by raising their Click event: in the main window, moving the headless pointer over
    /// a plot toolbar lays the document out again, so a press can land on another button. The view tests click the
    /// views' toolbars with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class UnivariateAnalysisShellTests
    {
        private const string Example = "sinnemahoning-move3-b17c";
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        [TestMethod]
        public void TheDocuments_ShowTheAnalyses_AndTheirProperties_AndOpenAgainAfterClosing()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    var univariate = Analysis<UnivariateAnalysis>(project, "LPIII - No Extension");
                    var b17c = Analysis<B17CAnalysis>(project, "B17C - LPIII - No Extension");
                    foreach (IElement element in new IElement[] { univariate, b17c })
                    {
                        for (int opened = 0; opened < 2; opened++)
                        {
                            // The explorer opens the document, with the element's properties beside it; a closed one
                            // opens again (upstream's MainProjectNode leaves the closed view holding its element).
                            window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
                            Control view;
                            Wpf.Plot frequency;
                            Button plotProperties, densityProperties;
                            Wpf.Plot density;
                            List<TabItem> tabs;
                            if (element is UnivariateAnalysis analysis)
                            {
                                var shown = Shown<UnivariateAnalysisControl>(window);
                                Assert.AreSame(analysis, shown.Element);
                                Assert.AreSame(analysis, Shown<UnivariateAnalysisPropertiesControl>(window).Element);
                                (view, frequency, plotProperties) = (shown, analysis.FrequencyPlot, shown.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton"));
                                Assert.AreSame(frequency, shown.FrequencyPlotHost.Content);
                                Assert.AreSame(frequency, shown.GetCurrentPlot());
                                UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                                (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), analysis.BayesianPlots.KernelDensityPlot);
                                tabs = ((TabControl)shown.DistributionResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                                UnivariateAnalysisTesting.Select(shown.DistributionResultsTabItem);
                            }
                            else
                            {
                                var shown = Shown<B17CAnalysisControl>(window);
                                Assert.AreSame(b17c, shown.Element);
                                Assert.AreSame(b17c, Shown<B17CAnalysisPropertiesControl>(window).Element);
                                (view, frequency, plotProperties) = (shown, b17c.FrequencyPlot, shown.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton"));
                                Assert.AreSame(frequency, shown.FrequencyPlotHost.Content);
                                Assert.AreSame(frequency, shown.GetCurrentPlot());
                                UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                                (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), b17c.BayesianPlots.KernelDensityPlot);
                                tabs = ((TabControl)shown.DistributionResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                                UnivariateAnalysisTesting.Select(shown.DistributionResultsTabItem);
                            }
                            Assert.AreEqual(4, frequency.Series.Count, element.Name + ": the curves and the observations");

                            // The plot toolbar's Properties button shows the plot's properties in the panel, and closes
                            // them again, which gives the analysis its properties back.
                            Click(plotProperties);
                            Assert.AreSame(frequency, Shown<OxyPlotPropertiesControl>(window).Plot);
                            Assert.IsFalse(((IElement)element).IsDirty, "showing the plot's properties changes nothing");
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
                    }
                    Assert.IsFalse(project.IsDirty, "the project is unchanged");
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void Estimate_RunsTheB17CAnalysis_AndItsSeedGivesTheSavedFit()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                var dialogs = new List<string>();
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    // Upstream's Estimate button disables the main window's menus and panes while the analysis runs.
                    Application.Current.MainWindow = window;
                    var b17c = Analysis<B17CAnalysis>(project, "B17C - LPIII - No Extension");
                    window.DoubleClick(ElementNode.FindElementNode(b17c, node).NodeHeader);
                    var view = Shown<B17CAnalysisControl>(window);
                    var properties = Shown<B17CAnalysisPropertiesControl>(window);
                    var saved = b17c.AnalysisResults;
                    double[] mode = (double[])saved.ModeCurve.Clone(), mean = (double[])saved.MeanCurve.Clone();
                    double[,] intervals = (double[,])saved.ConfidenceIntervals.Clone();

                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        Assert.IsFalse(properties.PropertiesExpander.IsEnabled);
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");

                    // The panel and shell are back, and the view shows the new results: the saved settings and seed
                    // give the saved fit again.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.CancelButton.Visibility);
                    Assert.AreEqual(Visibility.Hidden, properties.ProgressBar.Visibility);
                    Assert.IsTrue(properties.PropertiesExpander.IsEnabled);
                    Assert.IsTrue(b17c.IsEstimated);
                    var results = b17c.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    for (int i = 0; i < mode.Length; i++)
                    {
                        UnivariateAnalysisTesting.AssertClose(mode[i], results.ModeCurve[i], "the computed curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(mean[i], results.MeanCurve[i], "the expected probability curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 0], results.ConfidenceIntervals[i, 0], "the lower limit at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 1], results.ConfidenceIntervals[i, 1], "the upper limit at " + i);
                    }
                    BestFitShell.Frame();
                    var points = ((IEnumerable<Avalonia.Point>)b17c.FrequencyPlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
                    Assert.AreEqual(results.ModeCurve[0], points[0].Y, "the view shows the new run");
                    Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor, "the wait cursor is gone");
                }
                finally
                {
                    Application.Current.MainWindow = null;
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void Estimate_RunsTheBayesianAnalysis_AndItsSeedGivesTheSavedFit()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                var dialogs = new List<string>();
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    Application.Current.MainWindow = window;
                    var univariate = Analysis<UnivariateAnalysis>(project, "LPIII - No Extension");
                    window.DoubleClick(ElementNode.FindElementNode(univariate, node).NodeHeader);
                    var view = Shown<UnivariateAnalysisControl>(window);
                    var properties = Shown<UnivariateAnalysisPropertiesControl>(window);
                    var saved = univariate.AnalysisResults;
                    double dic = saved.DIC;
                    double[] mode = (double[])saved.ModeCurve.Clone();
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.AreEqual(Visibility.Visible, properties.CancelButton.Visibility, "the Cancel button replaces Estimate while it runs");
                        Assert.IsFalse(properties.QuantilePriorsExpander.IsEnabled);
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
                    Console.WriteLine("Estimate took " + watch.Elapsed);

                    // The saved sampler settings and seed give the saved fit again, which the view shows.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.IsTrue(properties.QuantilePriorsExpander.IsEnabled);
                    Assert.IsTrue(univariate.IsEstimated);
                    var results = univariate.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    UnivariateAnalysisTesting.AssertClose(dic, results.DIC, "the DIC");
                    for (int i = 0; i < mode.Length; i++)
                        UnivariateAnalysisTesting.AssertClose(mode[i], results.ModeCurve[i], "the point estimate's curve at " + i);
                    BestFitShell.Frame();
                    UnivariateAnalysisTesting.Select(UnivariateAnalysisViewTests.TabularResults(view));
                    var statistic = view.SummaryStatisticsTable.Items.Cast<SummaryStatistic>().Single(s => s.Name == "DIC");
                    Assert.AreEqual(results.DIC, statistic.Value);
                    BestFitShell.Frame();
                    Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor, "the wait cursor is gone");
                }
                finally
                {
                    Application.Current.MainWindow = null;
                    BestFitShell.Close(window);
                }
            });
        }

        /// <summary>
        /// Captures the main window with the Bayesian and the Bulletin 17C fits open in the Light, Dark and Blue
        /// themes, in artifacts/screenshots/univariate-main-{bayesian,b17c}-{theme}.png.
        /// </summary>
        [TestMethod]
        [TestCategory("Capture")]
        public void CaptureTheMainWindow_WithTheAnalysesOpen_InEveryTheme()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    foreach (var (element, name) in new (IElement, string)[]
                    {
                        (Analysis<UnivariateAnalysis>(project, "LPIII - No Extension"), "bayesian"),
                        (Analysis<B17CAnalysis>(project, "B17C - LPIII - No Extension"), "b17c"),
                    })
                    {
                        window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
                        foreach (ThemeColor theme in Themes)
                        {
                            UserSettings.ColorTheme = theme.ToString();
                            ThemeManager.SetTheme(theme);
                            ElementNode.FindElementNode(element, node).IsSelected = true;
                            BestFitShell.Capture(window, "univariate-main-" + name + "-" + theme.ToString().ToLowerInvariant());
                        }
                        ThemeManager.SetTheme(ThemeColor.Light);
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        // Raises the button's Click, as a click on it does, and lets the shell respond.
        private static void Click(Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            BestFitShell.Frame();
        }

        private static T Analysis<T>(BestFitProject project, string name) where T : IElement =>
            project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single().OfType<T>().Single(e => e.Name == name);

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
