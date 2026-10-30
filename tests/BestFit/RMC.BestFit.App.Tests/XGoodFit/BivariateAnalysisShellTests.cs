using System;
using System.Collections.Generic;
using System.Linq;
using AvalonDock;
using AvalonDock.Layout;
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

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Bivariate Distribution Analysis view in the main window, as MainProjectNode opens it
    /// from the explorer, with its properties beside it, its plots' properties, and the Estimate button.
    /// </summary>
    /// <remarks>
    /// The buttons are clicked by raising their Click event: in the main window, moving the headless pointer over
    /// a plot toolbar lays the document out again, so a press can land on another button.
    /// BivariateAnalysisViewTests clicks the view's toolbars with the pointer.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class BivariateAnalysisShellTests
    {
        [TestMethod]
        public void TheDocument_ShowsTheAnalysis_ItsPropertiesAndItsPlotsProperties_AndClosingItChangesNothing()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("bivariate-distribution-examples"));
                    var amh = Analysis(project, "AMH Copula");
                    window.DoubleClick(ElementNode.FindElementNode(amh, node).NodeHeader);
                    var view = Shown<BivariateAnalysisControl>(window);
                    Assert.AreSame(amh, view.Element);
                    Assert.AreSame(amh, Shown<BivariateAnalysisPropertiesControl>(window).Element);
                    Assert.AreSame(amh.CopulaPlot, view.GetCurrentPlot());
                    Assert.AreEqual(2, amh.CopulaPlot.Series.Count, "the simulated and observed points");

                    // The plot toolbar's Properties button shows the plot's properties in the panel, and closes them
                    // again, which gives the analysis its properties back.
                    var plotProperties = view.CopulaPlotToolbar.FindControl<Button>("PropertiesButton");
                    Click(plotProperties);
                    Assert.AreSame(amh.CopulaPlot, Shown<OxyPlotPropertiesControl>(window).Plot);
                    Assert.IsFalse(amh.IsDirty, "showing the plot's properties changes nothing: " + (amh.UndoManager.CanUndo ? amh.UndoManager.UndoDescription : ""));
                    Click(plotProperties);
                    Assert.IsFalse(window.GetVisualDescendants().OfType<OxyPlotPropertiesControl>().Any(c => c.IsEffectivelyVisible), "the plot's properties closed");
                    Assert.AreSame(amh, Shown<BivariateAnalysisPropertiesControl>(window).Element);

                    // A diagnostic plot's toolbar shows its plot's properties.
                    BivariateAnalysisViewTests.Select(view.KernelDensityTabItem);
                    Click(view.KernelDensityControl.PlotToolbar.FindControl<Button>("PropertiesButton"));
                    Assert.AreSame(amh.BayesianPlots.KernelDensityPlot, Shown<OxyPlotPropertiesControl>(window).Plot);

                    // Every tab, then the document closes: the analysis is as it was.
                    BivariateAnalysisViewTests.Select(BivariateAnalysisViewTests.InnerTab(view, 1));
                    foreach (TabItem tab in ((TabControl)view.DistributionResultsTabItem.Parent).Items.Cast<TabItem>())
                        BivariateAnalysisViewTests.Select(tab);
                    // An unsaved analysis would be asked about: close the question, and fail below.
                    using (BestFitShell.WhenOpened<Window>(dialog => dialog.Close()))
                        Documents(window).Single(d => d.Content == view).Close();
                    BestFitShell.Frame();
                    Assert.IsFalse(window.GetVisualDescendants().OfType<BivariateAnalysisControl>().Any(), "the document closed");
                    Assert.IsFalse(amh.IsDirty, "the analysis is unchanged");
                    Assert.IsFalse(amh.UndoManager.CanUndo, "and has nothing to undo");
                    Assert.IsFalse(project.IsDirty, "the project is unchanged");
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void Estimate_RunsTheAnalysis_WithProgress_AndTheViewShowsTheResults()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                var dialogs = new List<string>();
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("bivariate-distribution-examples"));
                    // Upstream's Estimate button disables the main window's menus and panes while the analysis runs.
                    Application.Current.MainWindow = window;
                    var amh = Analysis(project, "AMH Copula");
                    window.DoubleClick(ElementNode.FindElementNode(amh, node).NodeHeader);
                    var view = Shown<BivariateAnalysisControl>(window);
                    var properties = Shown<BivariateAnalysisPropertiesControl>(window);
                    double savedDic = amh.AnalysisResults.DIC;
                    var savedResults = amh.AnalysisResults;

                    // An error message would wait for an answer: close it and fail.
                    using (BestFitShell.WhenOpened<Window>(dialog => { dialogs.Add(dialog.Title); dialog.Close(); }))
                    {
                        Click(properties.EstimateButton);
                        Assert.AreEqual(Visibility.Visible, properties.CancelButton.Visibility, "the Cancel button replaces Estimate while it runs");
                        Assert.AreEqual(Visibility.Hidden, properties.EstimateButton.Visibility);
                        Assert.IsFalse(properties.PropertiesExpander.IsEnabled);
                        Assert.IsTrue(ShellPublicVariables.SimulationInProgress);
                        BestFitShell.WaitUntil(() => properties.EstimateButton.Visibility == Visibility.Visible, TimeSpan.FromMinutes(3), "the analysis to finish");
                    }
                    CollectionAssert.AreEqual(Array.Empty<string>(), dialogs, "no message");

                    // The panel and shell are back, and the view shows the new results: the saved settings and seed
                    // give the saved fit again.
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.AreEqual(Visibility.Hidden, properties.CancelButton.Visibility);
                    Assert.AreEqual(Visibility.Hidden, properties.ProgressBar.Visibility);
                    Assert.IsTrue(properties.PropertiesExpander.IsEnabled);
                    Assert.IsTrue(amh.IsEstimated);
                    Assert.AreNotSame(savedResults, amh.AnalysisResults, "a new run");
                    Assert.AreEqual(savedDic, amh.AnalysisResults.DIC, Math.Abs(savedDic) * 1E-9, "the saved seed's fit");
                    BestFitShell.Frame();
                    BivariateAnalysisViewTests.Select(BivariateAnalysisViewTests.InnerTab(view, 1));
                    var dic = view.SummaryStatisticsTable.Items.Cast<SummaryStatistic>().Single(s => s.Name == "DIC");
                    Assert.AreEqual(amh.AnalysisResults.DIC, dic.Value);
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

        // Raises the button's Click, as a click on it does, and lets the shell respond.
        private static void Click(Button button)
        {
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            BestFitShell.Frame();
        }

        private static BivariateAnalysis Analysis(BestFitProject project, string name) =>
            project.ElementCollections.OfType<BivariateAnalysisCollection>().Single().OfType<BivariateAnalysis>().Single(e => e.Name == name);

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
