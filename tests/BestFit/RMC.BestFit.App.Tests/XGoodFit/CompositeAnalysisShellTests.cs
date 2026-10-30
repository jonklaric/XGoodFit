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
    /// XGoodFit addition: the Composite Analysis view in the main window, as MainProjectNode opens it from the
    /// explorer, with its properties beside it, its plot's properties, its document closed and opened again, and the
    /// Estimate button.
    /// </summary>
    /// <remarks>
    /// The buttons are clicked by raising their Click event: in the main window, moving the headless pointer over a
    /// plot toolbar lays the document out again, so a press can land on another button. CompositeAnalysisViewTests
    /// clicks the view's toolbar with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class CompositeAnalysisShellTests
    {
        private const string Example = "mixed-population-examples";
        private static readonly ThemeColor[] Themes = { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue };

        // The capture runs first (MSTest runs a class's tests in the order they're declared): three themes restyle
        // whatever earlier shell tests left alive, which costs memory.
        /// <summary>
        /// Captures the main window with the competing-risks composite open in the Light, Dark and Blue themes, in
        /// artifacts/screenshots/composite-main-{theme}.png.
        /// </summary>
        [TestMethod]
        [TestCategory("Capture")]
        public void CaptureTheMainWindow_WithTheCompositeOpen_InEveryTheme()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    IElement composite = Analysis(project, CompositeAnalysisViewTests.Competing);
                    window.DoubleClick(ElementNode.FindElementNode(composite, node).NodeHeader);
                    foreach (ThemeColor theme in Themes)
                    {
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        ElementNode.FindElementNode(composite, node).IsSelected = true;
                        BestFitShell.Capture(window, "composite-main-" + theme.ToString().ToLowerInvariant());
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
        public void TheDocuments_ShowTheComposites_AndTheirProperties_AndOpenAgainAfterClosing()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(Example));
                    foreach (string name in new[] { CompositeAnalysisViewTests.Competing, CompositeAnalysisViewTests.Mixture })
                    {
                        var composite = Analysis(project, name);
                        for (int opened = 0; opened < 2; opened++)
                        {
                            // The explorer opens the document, with the element's properties beside it; a closed one
                            // opens again (upstream's MainProjectNode leaves the closed view holding its element).
                            // Clicks on the same spot within the double-click time of the last continue its count (a third click
                            // opens nothing): wait it out, as a user's pause does.
                            System.Threading.Thread.Sleep(600);
                            window.DoubleClick(ElementNode.FindElementNode(composite, node).NodeHeader);
                            var view = Shown<CompositeAnalysisControl>(window);
                            Assert.AreSame(composite, view.Element);
                            Assert.AreSame(composite, Shown<CompositeAnalysisPropertiesControl>(window).Element);
                            Assert.AreSame(composite.FrequencyPlot, view.FrequencyPlotHost.Content);
                            Assert.AreEqual(6, composite.FrequencyPlot.Series.Count, name + ": the components' curves, the composite's and the observations");

                            // The plot toolbar's Properties button shows the plot's properties in the panel, and closes
                            // them again, which gives the analysis its properties back.
                            var plotProperties = view.FrequencyPlotToolbar.FindControl<Button>("PropertiesButton");
                            Click(plotProperties);
                            Assert.AreSame(composite.FrequencyPlot, Shown<OxyPlotPropertiesControl>(window).Plot);
                            Assert.IsFalse(composite.IsDirty, "showing the plot's properties changes nothing");
                            Click(plotProperties);
                            Assert.IsFalse(window.GetVisualDescendants().OfType<OxyPlotPropertiesControl>().Any(c => c.IsEffectivelyVisible), "the plot's properties closed");
                            Assert.AreSame(composite, Shown<CompositeAnalysisPropertiesControl>(window).Element);

                            // Both tabs, then the document closes: the analysis is as it was.
                            BivariateAnalysisViewTests.Select(CompositeAnalysisViewTests.Tab(view, 1));
                            BivariateAnalysisViewTests.Select(CompositeAnalysisViewTests.Tab(view, 0));
                            // An unsaved analysis would be asked about: close the question, and fail below.
                            using (BestFitShell.WhenOpened<Window>(dialog => dialog.Close()))
                                Documents(window).Single(d => d.Content == view).Close();
                            BestFitShell.Frame();
                            Assert.IsFalse(window.GetVisualDescendants().Any(v => v == view), "the document closed");
                            Assert.IsFalse(composite.IsDirty, name + " is unchanged");
                            Assert.IsFalse(composite.UndoManager.CanUndo, name + " has nothing to undo");
                        }
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
        public void Estimate_RunsTheComposite_AndGivesTheSavedResults()
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
                    var composite = Analysis(project, CompositeAnalysisViewTests.Competing);
                    window.DoubleClick(ElementNode.FindElementNode(composite, node).NodeHeader);
                    var view = Shown<CompositeAnalysisControl>(window);
                    var properties = Shown<CompositeAnalysisPropertiesControl>(window);
                    var saved = composite.AnalysisResults;
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
                        Assert.IsFalse(properties.CompositeOptionsExpander.IsEnabled);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");
                    Console.WriteLine("Estimate took " + watch.Elapsed);

                    // The panel and shell are back, and the view shows the new results: the components' saved results
                    // give the saved curves again.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.CancelButton.Visibility);
                    Assert.AreEqual(Visibility.Hidden, properties.ProgressBar.Visibility);
                    Assert.IsTrue(properties.PropertiesExpander.IsEnabled);
                    Assert.IsTrue(properties.CompositeOptionsExpander.IsEnabled);
                    Assert.IsTrue(composite.IsEstimated);
                    var results = composite.AnalysisResults;
                    Assert.AreNotSame(saved, results, "a new run");
                    for (int i = 0; i < mode.Length; i++)
                    {
                        UnivariateAnalysisTesting.AssertClose(mode[i], results.ModeCurve[i], "the point curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(mean[i], results.MeanCurve[i], "the predictive curve at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 0], results.ConfidenceIntervals[i, 0], "the lower limit at " + i);
                        UnivariateAnalysisTesting.AssertClose(intervals[i, 1], results.ConfidenceIntervals[i, 1], "the upper limit at " + i);
                    }
                    BestFitShell.Frame();
                    var points = ((IEnumerable<OxyPlot.DataPoint>)composite.FrequencyPlot.Series.Single(s => s.Name == "PosteriorMode").ItemsSource).ToList();
                    Assert.AreEqual(results.ModeCurve[0], points[0].Y, "the view shows the new run");
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

        private static CompositeAnalysis Analysis(BestFitProject project, string name) =>
            project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single().OfType<CompositeAnalysis>().Single(e => e.Name == name);

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
