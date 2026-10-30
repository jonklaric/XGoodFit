using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using FrameworkInterfaces;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: captures of the bootstrap's windows in the Light, Blue and Dark themes, in
    /// artifacts/screenshots, to compare with RMC-BestFit's (upstream/RMC-BestFit/docs/images/rmc-bestfit-gui.png
    /// is viglione-et-al-2013 with an MCMC analysis open, in the Dark theme).
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    [TestCategory("Capture")]
    public sealed class BootstrapCaptures
    {
        private static readonly ThemeColor[] Themes = [ThemeColor.Light, ThemeColor.Blue, ThemeColor.Dark];

        [TestMethod]
        public void MainWindow_WithAnExample_ItsMenusAndContextMenus()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("viglione-et-al-2013"));
                    // As upstream's screenshot: MCMC - Systematic (1951-2005) open and selected.
                    IElement analysis = project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single().First(e => e.Name == "MCMC - Systematic (1951-2005)");
                    window.DoubleClick(ElementNode.FindElementNode(analysis, node).NodeHeader);
                    var mainMenu = window.FindControl<Menu>("MainMenu")!;
                    foreach (ThemeColor theme in Themes)
                    {
                        string suffix = theme.ToString().ToLowerInvariant();
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        // The analysis selected again (the menus below select other nodes), as a click would
                        // without the slow second click that renames.
                        ElementNode.FindElementNode(analysis, node).IsSelected = true;
                        BestFitShell.Capture(window, "bootstrap-main-viglione-" + suffix);

                        // The Project menu, with its Univariate Analyses submenu.
                        var projectMenu = window.FindControl<MenuItem>("ProjectMenu")!;
                        projectMenu.Open();
                        BestFitShell.Settle();
                        node.ProjectMenuItems[3].Open();
                        BestFitShell.Capture(window, "bootstrap-project-menu-" + suffix);
                        mainMenu.Close();

                        window.FindControl<MenuItem>("HelpMenu")!.Open();
                        BestFitShell.Capture(window, "bootstrap-help-menu-" + suffix);
                        mainMenu.Close();

                        // The explorer's menus: a collection's (its New items and Batch Run) and an analysis's.
                        ElementNodeCollection analyses = node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is UnivariateAnalysisCollection);
                        window.RightClick(analyses.NodeHeader);
                        BestFitShell.Capture(window, "bootstrap-explorer-collection-menu-" + suffix);
                        analyses.ContextMenu!.Close();

                        ElementNode analysisNode = ElementNode.FindElementNode(analysis, node);
                        window.RightClick(analysisNode.NodeHeader);
                        BestFitShell.Capture(window, "bootstrap-explorer-element-menu-" + suffix);
                        analysisNode.ContextMenu!.Close();

                        ElementNodeCollection bivariate = node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is BivariateAnalysisCollection);
                        window.RightClick(bivariate.NodeHeader);
                        BestFitShell.Capture(window, "bootstrap-explorer-bivariate-menu-" + suffix);
                        bivariate.ContextMenu!.Close();
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void BatchRunWindow_BeforeAndAfterARun()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                BatchRunWindow batch = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("bulletin-17c-examples"));
                    ElementNodeCollection analyses = node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is UnivariateAnalysisCollection);
                    using (BestFitShell.WhenOpened<BatchRunWindow>(opened => batch = opened))
                    {
                        BestFitShell.Click(analyses.CustomContextItems.Single(m => (string)m.Header == "Batch Run..."));
                        BestFitShell.Frame();
                    }
                    var grid = batch.FindControl<CopyPasteDataGrid>("MyDataGrid")!;
                    var summary = batch.FindControl<TextBlock>("SummaryTextBlock")!;
                    bool ran = false;
                    foreach (ThemeColor theme in Themes)
                    {
                        string suffix = theme.ToString().ToLowerInvariant();
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        BestFitShell.Capture(batch, "bootstrap-batch-run-" + suffix);
                        if (!ran)
                        {
                            // One run, of the first two analyses, then the window in each theme.
                            grid.SelectedItems.Clear();
                            foreach (object row in ((System.Collections.IEnumerable)grid.ItemsSource).Cast<object>().Take(2))
                            {
                                grid.SelectedItems.Add(row);
                            }
                            BestFitShell.Frame();
                            batch.FindControl<Button>("SimulateButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                            BestFitShell.WaitUntil(() => summary.Text.StartsWith("Batch run finished", StringComparison.Ordinal), TimeSpan.FromMinutes(3), "the batch run");
                            ran = true;
                        }
                        BestFitShell.Capture(batch, "bootstrap-batch-run-finished-" + suffix);
                    }
                }
                finally
                {
                    batch?.Close();
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void AboutAndTermsWindows()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                BestFitShell.UseCaptureStyles();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, _) = BestFitShell.Open(null);
                    foreach (ThemeColor theme in Themes)
                    {
                        string suffix = theme.ToString().ToLowerInvariant();
                        UserSettings.ColorTheme = theme.ToString();
                        ThemeManager.SetTheme(theme);
                        // The Help menu's items, made again for the theme (MainWindow.ThemeChanged).
                        MenuItem about = node.HelpMenuItems.Last();
                        MenuItem terms = node.HelpMenuItems[4];
                        using (BestFitShell.WhenOpened<AboutWindow>(opened =>
                        {
                            BestFitShell.Capture(opened, "bootstrap-about-" + suffix);
                            opened.Close();
                        }))
                        {
                            BestFitShell.Click(about);
                        }
                        using (BestFitShell.WhenOpened<TermsAndConditionsWindow>(opened =>
                        {
                            BestFitShell.Capture(opened, "bootstrap-terms-" + suffix);
                            opened.Close();
                        }))
                        {
                            BestFitShell.Click(terms);
                        }
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }
    }
}
