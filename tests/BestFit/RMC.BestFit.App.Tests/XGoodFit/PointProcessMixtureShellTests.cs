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
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using static RMC.BestFit.App.Tests.XGoodFit.PointProcessMixtureTesting;
using Wpf = OxyPlot.Wpf;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Point Process and Mixture analysis views in the main window, as MainProjectNode opens them
    /// from the explorer, with their properties beside them, their plots' properties, closing and opening them again,
    /// and the Estimate button.
    /// </summary>
    /// <remarks>
    /// The buttons are clicked by raising their Click event: in the main window, moving the headless pointer over a plot
    /// toolbar lays the document out again, so a press can land on another button (porting guide, §7). The view tests
    /// click the views' toolbars with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class PointProcessMixtureShellTests
    {
        private const string PointProcessProject = "point-process-examples";
        private const string MixtureProject = "mixture-distribution-examples";
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        [TestMethod]
        [DataRow(PointProcessProject, BigBear)]
        [DataRow(MixtureProject, TwoNormals)]
        public void TheDocument_ShowsTheAnalysis_AndItsProperties_AndOpensAgainAfterClosing(string example, string name)
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(example));
                    IElement element = Analysis<IElement>(project, name);
                    for (int opened = 0; opened < 2; opened++)
                    {
                        // The explorer opens the document, with the element's properties beside it; a closed one opens
                        // again (upstream's MainProjectNode leaves the closed view holding its element).
                        window.DoubleClick(ElementNode.FindElementNode(element, node).NodeHeader);
                        Control view;
                        Wpf.Plot frequency, density;
                        Button plotProperties, densityProperties;
                        List<TabItem> tabs;
                        if (element is PointProcessAnalysis pointProcess)
                        {
                            var shown = Shown<PointProcessAnalysisControl>(window);
                            Assert.AreSame(pointProcess, shown.Element);
                            Assert.AreSame(pointProcess, Shown<PointProcessAnalysisPropertiesControl>(window).Element);
                            (view, frequency) = (shown, pointProcess.FrequencyPlot);
                            Assert.AreSame(frequency, shown.FrequencyPlotHost.Content);
                            Assert.AreSame(frequency, shown.GetCurrentPlot());
                            plotProperties = shown.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton");
                            UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                            (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), pointProcess.BayesianPlots.KernelDensityPlot);
                            tabs = ((TabControl)shown.DistributionResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                            UnivariateAnalysisTesting.Select(shown.DistributionResultsTabItem);
                            Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData,AMSExactData", string.Join(",", frequency.Series.Select(s => s.Name)));
                        }
                        else
                        {
                            var mixture = (MixtureAnalysis)element;
                            var shown = Shown<MixtureAnalysisControl>(window);
                            Assert.AreSame(mixture, shown.Element);
                            Assert.AreSame(mixture, Shown<MixtureAnalysisPropertiesControl>(window).Element);
                            (view, frequency) = (shown, mixture.FrequencyPlot);
                            Assert.AreSame(frequency, shown.FrequencyPlotHost.Content);
                            Assert.AreSame(frequency, shown.GetCurrentPlot());
                            plotProperties = shown.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton");
                            UnivariateAnalysisTesting.Select(shown.KernelDensityTabItem);
                            (densityProperties, density) = (shown.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"), mixture.BayesianPlots.KernelDensityPlot);
                            tabs = ((TabControl)shown.DistributionResultsTabItem.Parent).Items.Cast<TabItem>().ToList();
                            UnivariateAnalysisTesting.Select(shown.DistributionResultsTabItem);
                            Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData", string.Join(",", frequency.Series.Select(s => s.Name)));
                        }

                        // The plot toolbar's Properties button shows the plot's properties in the panel, and closes them
                        // again, which gives the analysis its properties back.
                        Click(plotProperties);
                        Assert.AreSame(frequency, Shown<OxyPlotPropertiesControl>(window).Plot);
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
                        Assert.IsFalse(element.IsDirty, name + " is unchanged");
                        Assert.IsFalse(((ElementBase)element).UndoManager.CanUndo, name + " has nothing to undo");
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
        public void Estimate_RunsTheMixtureAnalysis_AndItsSeedGivesTheSavedFit()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                var dialogs = new List<string>();
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(MixtureProject));
                    // Upstream's Estimate button disables the main window's menus and panes while the analysis runs.
                    Application.Current.MainWindow = window;
                    var mixture = Analysis<MixtureAnalysis>(project, TwoNormals);
                    window.DoubleClick(ElementNode.FindElementNode(mixture, node).NodeHeader);
                    var view = Shown<MixtureAnalysisControl>(window);
                    var properties = Shown<MixtureAnalysisPropertiesControl>(window);
                    var saved = mixture.AnalysisResults;
                    double dic = saved.DIC;
                    double[] mode = (double[])saved.ModeCurve.Clone();
                    double[,] intervals = (double[,])saved.ConfidenceIntervals.Clone();
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        Assert.AreEqual(Visibility.Visible, properties.CancelButton.Visibility, "the Cancel button replaces Estimate while it runs");
                        Assert.IsFalse(properties.MixtureOptionsExpander.IsEnabled, "the components can't change while it runs");
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
                    Console.WriteLine("Estimate took " + watch.Elapsed);

                    // The saved sampler settings and seed give the saved fit again, which the view shows.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.CancelButton.Visibility);
                    Assert.IsTrue(properties.MixtureOptionsExpander.IsEnabled);
                    Assert.IsTrue(mixture.IsEstimated);
                    var results = mixture.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    UnivariateAnalysisTesting.AssertClose(dic, results.DIC, "the DIC");
                    for (int i = 0; i < mode.Length; i++)
                    {
                        UnivariateAnalysisTesting.AssertClose(mode[i], results.ModeCurve[i], "the point estimate's curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 0], results.ConfidenceIntervals[i, 0], "the lower limit at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 1], results.ConfidenceIntervals[i, 1], "the upper limit at " + i);
                    }
                    BestFitShell.Frame();
                    var points = ((IEnumerable<OxyPlot.DataPoint>)mixture.FrequencyPlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
                    Assert.AreEqual(results.ModeCurve[0], points[0].Y, "the view shows the new run");
                    UnivariateAnalysisTesting.Select(MixtureAnalysisViewTests.TabularResults(view));
                    Assert.AreEqual(results.DIC, view.SummaryStatisticsTable.Items.Cast<SummaryStatistic>().Single(s => s.Name == "DIC").Value);
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

        [TestMethod]
        public void Estimate_RunsThePointProcessAnalysis_AndItsSeedGivesTheSavedFit()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                var dialogs = new List<string>();
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(PointProcessProject));
                    Application.Current.MainWindow = window;
                    var pointProcess = Analysis<PointProcessAnalysis>(project, BigBear);
                    window.DoubleClick(ElementNode.FindElementNode(pointProcess, node).NodeHeader);
                    var view = Shown<PointProcessAnalysisControl>(window);
                    var properties = Shown<PointProcessAnalysisPropertiesControl>(window);
                    var saved = pointProcess.AnalysisResults;
                    double dic = saved.DIC;
                    double[] mode = (double[])saved.ModeCurve.Clone();
                    double[,] intervals = (double[,])saved.ConfidenceIntervals.Clone();
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        Assert.IsFalse(properties.PropertiesExpander.IsEnabled, "the threshold and exposure can't change while it runs");
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
                    Console.WriteLine("Estimate took " + watch.Elapsed);

                    // The saved sampler settings and seed give the saved fit again (the walkthrough's DIC of 372.8),
                    // which the view shows with the POT and AMS data.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.ProgressBar.Visibility);
                    Assert.IsTrue(properties.PropertiesExpander.IsEnabled);
                    Assert.IsTrue(pointProcess.IsEstimated);
                    var results = pointProcess.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    UnivariateAnalysisTesting.AssertClose(dic, results.DIC, "the DIC");
                    for (int i = 0; i < mode.Length; i++)
                    {
                        UnivariateAnalysisTesting.AssertClose(mode[i], results.ModeCurve[i], "the point estimate's curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 0], results.ConfidenceIntervals[i, 0], "the lower limit at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 1], results.ConfidenceIntervals[i, 1], "the upper limit at " + i);
                    }
                    BestFitShell.Frame();
                    Assert.AreEqual("CredibleIntervals,PosteriorPredictive,PosteriorMode,ExactData,AMSExactData", string.Join(",", pointProcess.FrequencyPlot.Series.Select(s => s.Name)));
                    var points = ((IEnumerable<OxyPlot.DataPoint>)pointProcess.FrequencyPlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
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

        /// <summary>
        /// Captures the main window with the Point Process and Mixture fits open in the Light, Dark and Blue themes, in
        /// artifacts/screenshots/{point-process,mixture}-main-{theme}.png.
        /// </summary>
        [TestMethod]
        [TestCategory("Capture")]
        [DataRow(PointProcessProject, Seasonal, "point-process")]
        [DataRow(MixtureProject, TwoNormals, "mixture")]
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
                    IElement element = Analysis<IElement>(project, name);
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
