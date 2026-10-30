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

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Coincident Frequency Analysis view in the main window, as MainProjectNode opens it from
    /// the explorer, with its properties beside it, its plot's properties, its document closed and opened again, and
    /// the Estimate button.
    /// </summary>
    /// <remarks>
    /// The buttons are clicked by raising their Click event: in the main window, moving the headless pointer over a
    /// plot toolbar lays the document out again, so a press can land on another button.
    /// CoincidentFrequencyViewTests clicks the view's toolbar with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CoincidentFrequencyShellTests
    {
        private const string Example = "sum-two-normals";
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        // The capture runs first (MSTest runs a class's tests in the order they're declared): three themes restyle
        // whatever earlier shell tests left alive, which costs memory.
        /// <summary>
        /// Captures the main window with the near-zero correlation analysis open in the Light, Dark and Blue themes,
        /// on its response grid and its frequency plot, in artifacts/screenshots/coincident-main-{grid,plot}-{theme}.png.
        /// </summary>
        [TestMethod]
        [TestCategory("Capture")]
        public void CaptureTheMainWindow_WithTheAnalysisOpen_InEveryTheme()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    IElement analysis = Analysis(project, CoincidentFrequencyViewTests.NearZero);
                    window.DoubleClick(ElementNode.FindElementNode(analysis, node).NodeHeader);
                    var view = Shown<CoincidentFrequencyControl>(window);
                    foreach (ThemeColor theme in Themes)
                    {
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        ElementNode.FindElementNode(analysis, node).IsSelected = true;
                        CoincidentFrequencyViewTests.Select(view, 0);
                        BestFitShell.Capture(window, "coincident-main-grid-" + theme.ToString().ToLowerInvariant());
                        CoincidentFrequencyViewTests.Select(view, 1);
                        BestFitShell.Capture(window, "coincident-main-plot-" + theme.ToString().ToLowerInvariant());
                    }
                    ThemeManager.SetTheme(ThemeColor.Light);
                }
                finally
                {
                    BestFitShell.Close(window);
                    Release();
                }
            });
        }

        [TestMethod]
        public void TheDocument_ShowsTheAnalysis_AndItsProperties_AndOpensAgainAfterClosing()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    var analysis = Analysis(project, CoincidentFrequencyViewTests.NearZero);
                    for (int opened = 0; opened < 2; opened++)
                    {
                        // The explorer opens the document, with the element's properties beside it; a closed one opens
                        // again (upstream's MainProjectNode leaves the closed view holding its element).
                        // Clicks on the same spot within the double-click time of the last continue its count (a third click
                        // opens nothing): wait it out, as a user's pause does.
                        System.Threading.Thread.Sleep(600);
                        window.DoubleClick(ElementNode.FindElementNode(analysis, node).NodeHeader);
                        var view = Shown<CoincidentFrequencyControl>(window);
                        Assert.AreSame(analysis, view.Element);
                        Assert.AreSame(analysis, Shown<CoincidentFrequencyPropertiesControl>(window).Element);
                        Assert.AreSame(analysis.FrequencyPlot, view.FrequencyPlotHost.Content);
                        Assert.AreEqual(analysis.XValues.Count, view.BivariateDataGrid.Items.Count, "the response grid's rows");
                        Assert.AreEqual(4, analysis.FrequencyPlot.Series.Count, "the interval, curves and observed sums");

                        // On the plot's tab, the plot toolbar's Properties button shows the plot's properties in the
                        // panel, and closes them again, which gives the analysis its properties back.
                        CoincidentFrequencyViewTests.Select(view, 1);
                        Assert.AreSame(analysis.FrequencyPlot, view.GetCurrentPlot());
                        var plotProperties = view.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton");
                        Click(plotProperties);
                        Assert.AreSame(analysis.FrequencyPlot, Shown<OxyPlotPropertiesControl>(window).Plot);
                        Assert.IsFalse(analysis.IsDirty, "showing the plot's properties changes nothing");
                        Click(plotProperties);
                        Assert.IsFalse(window.GetVisualDescendants().OfType<OxyPlotPropertiesControl>().Any(c => c.IsEffectivelyVisible), "the plot's properties closed");
                        Assert.AreSame(analysis, Shown<CoincidentFrequencyPropertiesControl>(window).Element);

                        // Every tab, then the document closes: the analysis is as it was.
                        CoincidentFrequencyViewTests.Select(view, 2);
                        CoincidentFrequencyViewTests.Select(view, 0);
                        // An unsaved analysis would be asked about: close the question, and fail below.
                        using (BestFitShell.WhenOpened<Window>(dialog => dialog.Close()))
                            Documents(window).Single(d => d.Content == view).Close();
                        BestFitShell.Frame();
                        Assert.IsFalse(window.GetVisualDescendants().Any(v => v == view), "the document closed");
                        Assert.IsFalse(analysis.IsDirty, "the analysis is unchanged");
                        Assert.IsFalse(analysis.UndoManager.CanUndo, "and has nothing to undo");
                    }
                    Assert.IsFalse(project.IsDirty, "the project is unchanged");
                }
                finally
                {
                    BestFitShell.Close(window);
                    Release();
                }
            });
        }

        [TestMethod]
        public void Estimate_RunsTheAnalysis_AndGivesTheSavedResults()
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
                    var analysis = Analysis(project, CoincidentFrequencyViewTests.NearZero);
                    window.DoubleClick(ElementNode.FindElementNode(analysis, node).NodeHeader);
                    var view = Shown<CoincidentFrequencyControl>(window);
                    var properties = Shown<CoincidentFrequencyPropertiesControl>(window);
                    var saved = analysis.AnalysisResults;
                    double[] z = (double[])analysis.ZOutputValues.Clone();
                    double[] mode = (double[])saved.ModeCurve.Clone(), mean = (double[])saved.MeanCurve.Clone();
                    double[,] intervals = (double[,])saved.ConfidenceIntervals.Clone();
                    var watch = System.Diagnostics.Stopwatch.StartNew();

                    // An error message would wait for an answer: close it and fail.
                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.AreEqual(Visibility.Visible, properties.CancelButton.Visibility, "the Cancel button replaces Estimate while it runs");
                        Assert.AreEqual(Visibility.Hidden, properties.EstimateButton.Visibility);
                        Assert.IsFalse(properties.PropertiesExpander.IsEnabled);
                        Assert.IsFalse(properties.XOrdinatesExpander.IsEnabled);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
                    Console.WriteLine("Estimate took " + watch.Elapsed);

                    // The panel and shell are back, and the view shows the new results: the upstream fits' saved draws
                    // give the saved probabilities again.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.CancelButton.Visibility);
                    Assert.AreEqual(Visibility.Hidden, properties.ProgressBar.Visibility);
                    Assert.IsTrue(properties.PropertiesExpander.IsEnabled);
                    Assert.IsTrue(properties.Output_TabItem.IsEnabled);
                    Assert.IsTrue(analysis.IsEstimated);
                    var results = analysis.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    CollectionAssert.AreEqual(z, analysis.ZOutputValues, "the response bins");
                    for (int k = 0; k < mode.Length; k++)
                    {
                        UnivariateAnalysisTesting.AssertClose(mode[k], results.ModeCurve[k], "the point curve at " + k);
                        UnivariateAnalysisTesting.AssertClose(mean[k], results.MeanCurve[k], "the predictive curve at " + k);
                        UnivariateAnalysisTesting.AssertClose(intervals[k, 0], results.ConfidenceIntervals[k, 0], "the lower limit at " + k);
                        UnivariateAnalysisTesting.AssertClose(intervals[k, 1], results.ConfidenceIntervals[k, 1], "the upper limit at " + k);
                    }
                    BestFitShell.Frame();
                    var points = ((IEnumerable<OxyPlot.DataPoint>)analysis.FrequencyPlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
                    Assert.AreEqual(results.ModeCurve[0], points[0].X, "the view shows the new run");
                    Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor, "the wait cursor is gone");
                }
                finally
                {
                    Application.Current.MainWindow = null;
                    BestFitShell.Close(window);
                    Release();
                }
            });
        }

        // The closed project's messages and views go before the next test opens another: FrameworkInterfaces'
        // Messenger keeps every element that had a message until it's cleared, as the application does on Open.
        private static void Release()
        {
            FrameworkInterfaces.Messaging.Messenger.GetInstance().Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        // Raises the button's Click, as a click on it does, and lets the shell respond.
        private static void Click(Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            BestFitShell.Frame();
        }

        private static CoincidentFrequencyAnalysis Analysis(BestFitProject project, string name) =>
            project.ElementCollections.OfType<BivariateAnalysisCollection>().Single().OfType<CoincidentFrequencyAnalysis>().Single(e => e.Name == name);

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
