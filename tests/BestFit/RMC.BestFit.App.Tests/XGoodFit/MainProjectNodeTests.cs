using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: MainProjectNode in the main window, with the example projects: the explorer tree,
    /// the explorer's and Project menu's items and their commands, the batch run window, the Help menu, and
    /// the documents and properties panels of the GUI areas (placeholders until those are ported).
    /// </summary>
    /// <remarks>
    /// The tests run on the headless session's UI thread with Avalonia's synchronization context, as the
    /// application does, because the batch window's handlers continue there after awaiting.
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public sealed class MainProjectNodeTests
    {
        /// <summary>The explorer's New items and Batch Run for each collection, in upstream's order.</summary>
        private static readonly Dictionary<Type, string[]> ExplorerItems = new()
        {
            [typeof(TimeSeriesCollection)] = ["New Time Series..."],
            [typeof(InputDataCollection)] = ["New Input Data..."],
            [typeof(FittingAnalysisCollection)] = ["New Distribution Fitting Analysis..."],
            [typeof(UnivariateAnalysisCollection)] = ["New Univariate Distribution Analysis...", "New Bulletin 17C Analysis...", "New Point Process Analysis...", "New Mixture Distribution Analysis...", "New Composite Distribution Analysis...", "Batch Run..."],
            [typeof(BivariateAnalysisCollection)] = ["New Bivariate Distribution Analysis...", "New Coincident Frequency Analysis...", "Batch Run..."],
            [typeof(RatingCurveAnalysisCollection)] = ["New Rating Curve Analysis...", "Batch Run..."],
            [typeof(TimeSeriesAnalysisCollection)] = ["New Time Series Analysis...", "Batch Run..."],
        };

        /// <summary>What each New item makes: the element type, the dialog's title and the default name's stem.</summary>
        private static readonly Dictionary<string, (Type Type, string Title, string Stem)> NewItems = new()
        {
            ["New Time Series..."] = (typeof(TimeSeriesElement), "New Time Series", "Time Series_"),
            ["New Input Data..."] = (typeof(InputData), "New Input Data", "Input Data_"),
            ["New Distribution Fitting Analysis..."] = (typeof(FittingAnalysis), "New Distribution Fitting Analysis", "Fitting Analysis_"),
            ["New Univariate Distribution Analysis..."] = (typeof(UnivariateAnalysis), "New Univariate Distribution Analysis", "Univariate Analysis_"),
            ["New Bulletin 17C Analysis..."] = (typeof(B17CAnalysis), "New Bulletin 17C Analysis", "B17C Analysis_"),
            ["New Point Process Analysis..."] = (typeof(PointProcessAnalysis), "New Point Process Analysis", "Point Process Analysis_"),
            ["New Mixture Distribution Analysis..."] = (typeof(MixtureAnalysis), "New Mixture Distribution Analysis", "Mixture Analysis_"),
            ["New Composite Distribution Analysis..."] = (typeof(CompositeAnalysis), "New Composite Distribution Analysis", "Composite Analysis_"),
            ["New Bivariate Distribution Analysis..."] = (typeof(BivariateAnalysis), "New Bivariate Distribution Analysis", "Bivariate Analysis_"),
            ["New Coincident Frequency Analysis..."] = (typeof(CoincidentFrequencyAnalysis), "New Coincident Frequency Analysis", "Coincident Frequency_"),
            ["New Rating Curve Analysis..."] = (typeof(RatingCurveAnalysis), "New Rating Curve Analysis", "Rating Curve Analysis_"),
            ["New Time Series Analysis..."] = (typeof(TimeSeriesAnalysis), "New Time Series Analysis", "Time Series Analysis_"),
        };

        [TestMethod]
        public void OpenProject_ShowsEachExamplesCollectionsAndElements_WithTheirIcons()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(null);
                    IReadOnlyList<string> examples = BestFitShell.Examples();
                    Assert.IsTrue(examples.Count >= 30, "the example projects are fetched");
                    foreach (string example in examples)
                    {
                        string name = Path.GetFileName(example);
                        string copy = Path.Combine(BestFitShell.Folder, name);
                        File.Copy(example, copy, overwrite: true);
                        window.OpenProject(copy);
                        BestFitShell.Frame();

                        Assert.AreEqual(copy, project.FullFileName, name);
                        Assert.AreSame(project, node.Project, name);
                        Assert.IsFalse(project.IsDirty, name + ": opening the project doesn't change it");
                        List<ElementNodeCollection> collections = node.ChildNodes.OfType<ElementNodeCollection>().ToList();
                        CollectionAssert.AreEqual(project.ElementCollections.Select(c => c.Name).ToList(), collections.Select(c => c.NodeHeader.HeaderText).ToList(), name);
                        for (int i = 0; i < collections.Count; i++)
                        {
                            IElementCollection collection = project.ElementCollections[i];
                            Assert.AreSame(collection, collections[i].ElementCollection, name);
                            Assert.IsNotNull(collections[i].NodeHeader.StaticImage, $"{name}: {collection.Name} has its folder icon");
                            List<ElementNode> elementNodes = Descendants(collections[i]).OfType<ElementNode>().ToList();
                            Assert.AreEqual(collection.Count, elementNodes.Count, $"{name}: {collection.Name}'s elements");
                            foreach (IElement element in collection)
                            {
                                ElementNode elementNode = ElementNode.FindElementNode(element, collections[i]);
                                Assert.IsNotNull(elementNode, $"{name}: {element.Name} has a node");
                                Assert.AreEqual(element.Name, elementNode.NodeHeader.HeaderText, name);
                                // The element's icon: its resource's drawing (a new instance per lookup,
                                // x:Shared="False"). Time series, input data and fitting analyses declare their
                                // key on the class, which IElement's default member hides (an upstream quirk), so
                                // the node shows their ElementImage, the same drawing.
                                string key = element.ElementImageResourceKey ?? element.GetType().GetProperty(nameof(IElement.ElementImageResourceKey))?.GetValue(element) as string;
                                Assert.IsNotNull(key, $"{name}: {element.Name}'s icon key");
                                var icon = Application.Current!.FindResource(key) as IImage;
                                Assert.IsNotNull(icon, $"{name}: {key}");
                                Assert.AreEqual(Signature(icon), Signature(elementNode.NodeHeader.StaticImage), $"{name}: {element.Name}'s icon");
                            }
                        }
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void ExplorerMenus_OfferEachCollectionsNewItemsAndBatchRun()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("viglione-et-al-2013"));
                    List<ElementNodeCollection> collections = node.ChildNodes.OfType<ElementNodeCollection>().ToList();
                    CollectionAssert.AreEquivalent(ExplorerItems.Keys.ToList(), collections.Select(c => c.ElementCollection.GetType()).ToList());
                    foreach (ElementNodeCollection collection in collections)
                    {
                        string[] expected = ExplorerItems[collection.ElementCollection.GetType()];
                        CollectionAssert.AreEqual(expected, collection.CustomContextItems.Select(item => item.Header as string).ToList(), collection.NodeHeader.HeaderText);

                        // The context menu shows them first, with their icons, all enabled.
                        collection.ContextMenu!.Open(collection);
                        BestFitShell.Frame();
                        List<MenuItem> shown = collection.ContextMenu.Items.OfType<MenuItem>().ToList();
                        CollectionAssert.AreEqual(expected, shown.Take(expected.Length).Select(item => item.Header as string).ToList());
                        foreach (MenuItem item in shown.Take(expected.Length))
                        {
                            Assert.IsTrue(item.IsEffectivelyEnabled, (string)item.Header);
                            Assert.IsInstanceOfType(item.Icon, typeof(Image), (string)item.Header);
                            Assert.IsNotNull(((Image)item.Icon).Source, (string)item.Header + "'s icon resolves once shown");
                        }
                        collection.ContextMenu.Close();
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void ProjectMenu_OffersEveryNewItem_InUpstreamsOrder()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, _) = BestFitShell.Open(null);
                    CollectionAssert.AreEqual(new[]
                    {
                        "New Time Series...", "New Input Data...", "New Distribution Fitting Analysis...", "Univariate Analyses...",
                        "New Bivariate Distribution Analysis...", "New Coincident Frequency Analysis...", "New Rating Curve Analysis...", "New Time Series Analysis...",
                    }, node.ProjectMenuItems.Select(item => item.Header as string).ToArray());
                    MenuItem univariate = node.ProjectMenuItems[3];
                    CollectionAssert.AreEqual(ExplorerItems[typeof(UnivariateAnalysisCollection)].Take(5).ToArray(), univariate.Items.OfType<MenuItem>().Select(item => item.Header as string).ToArray());

                    // The main window's Project menu shows them above its own items.
                    var projectMenu = window.FindControl<MenuItem>("ProjectMenu")!;
                    List<object> menu = projectMenu.Items.Cast<object>().ToList();
                    int separator = menu.IndexOf(window.FindControl<Control>("ProjectMenuSeparator"));
                    CollectionAssert.AreEqual(node.ProjectMenuItems.ToList(), menu.Take(separator).ToList());
                    projectMenu.Open();
                    BestFitShell.Frame();
                    foreach (MenuItem item in node.ProjectMenuItems)
                    {
                        Assert.IsTrue(item.IsEffectivelyEnabled, (string)item.Header);
                        Assert.IsNotNull(((Image)item.Icon).Source, (string)item.Header);
                    }
                    window.FindControl<Menu>("MainMenu")!.Close();
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void NewItems_AddAnElementOfEachKind_WithTheNameDialogsName()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("viglione-et-al-2013"));
                    foreach (ElementNodeCollection collectionNode in node.ChildNodes.OfType<ElementNodeCollection>())
                    {
                        IElementCollection collection = collectionNode.ElementCollection;
                        foreach (MenuItem item in collectionNode.CustomContextItems.Where(item => (string)item.Header != "Batch Run..."))
                        {
                            (Type type, string title, string stem) = NewItems[(string)item.Header];
                            int count = collection.Count;
                            NameDialog dialog = null;
                            using (BestFitShell.WhenOpened<NameDialog>(opened =>
                            {
                                dialog = opened;
                                BestFitShell.ClickButton(opened, "OK");
                            }))
                            {
                                BestFitShell.Click(item);
                            }
                            Assert.IsNotNull(dialog, (string)item.Header + " asks for a name");
                            Assert.AreEqual(title, dialog.Title);
                            Assert.AreSame(window, dialog.Owner, "the dialog is the main window's");
                            Assert.AreEqual(count + 1, collection.Count, title);
                            IElement added = collection[collection.Count - 1];
                            Assert.IsInstanceOfType(added, type, title);
                            Assert.AreEqual(stem + (count + 1), added.Name, "the default name, as upstream's");
                            Assert.IsNotNull(ElementNode.FindElementNode(added, collectionNode), "the explorer shows it");
                        }
                    }

                    // Cancel adds nothing; the Project menu's items do the same as the explorer's.
                    var univariate = node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is UnivariateAnalysisCollection).ElementCollection;
                    int before = univariate.Count;
                    using (BestFitShell.WhenOpened<NameDialog>(opened => BestFitShell.ClickButton(opened, "Cancel")))
                    {
                        BestFitShell.Click(node.ProjectMenuItems[3].Items.OfType<MenuItem>().Single(i => (string)i.Header == "New Bulletin 17C Analysis..."));
                    }
                    Assert.AreEqual(before, univariate.Count, "Cancel adds nothing");
                    using (BestFitShell.WhenOpened<NameDialog>(opened =>
                    {
                        opened.Text = "Annual Peaks B17C";
                        BestFitShell.ClickButton(opened, "OK");
                    }))
                    {
                        BestFitShell.Click(node.ProjectMenuItems[3].Items.OfType<MenuItem>().Single(i => (string)i.Header == "New Bulletin 17C Analysis..."));
                    }
                    Assert.AreEqual(before + 1, univariate.Count);
                    Assert.IsInstanceOfType(univariate[before], typeof(B17CAnalysis));
                    Assert.AreEqual("Annual Peaks B17C", univariate[before].Name, "the name the user typed");
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void ElementCommands_CopyRenameAndDeleteAnalyses()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("viglione-et-al-2013"));
                    ElementNodeCollection analyses = node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is UnivariateAnalysisCollection);
                    IElementCollection collection = analyses.ElementCollection;
                    IElement original = collection[0];
                    ElementNode originalNode = ElementNode.FindElementNode(original, analyses);
                    window.Click(originalNode.NodeHeader);
                    Assert.IsTrue(originalNode.IsSelected);

                    // Copy... asks for the copy's name, then adds a copy of the same kind.
                    List<MenuItem> commands = originalNode.ContextMenu!.Items.OfType<MenuItem>().ToList();
                    foreach (string header in new[] { "Edit...", "Copy...", "Rename...", "Delete..." })
                    {
                        Assert.IsTrue(commands.Single(m => (string)m.Header == header).IsEnabled, header);
                    }
                    int count = collection.Count;
                    using (BestFitShell.WhenOpened<NameDialog>(opened => BestFitShell.ClickButton(opened, "OK")))
                    {
                        BestFitShell.Click(commands.Single(m => (string)m.Header == "Copy..."));
                    }
                    Assert.AreEqual(count + 1, collection.Count);
                    IElement copy = collection.Single(e => e.Name == original.Name + "_copy");
                    Assert.AreEqual(original.GetType(), copy.GetType());
                    ElementNode copyNode = ElementNode.FindElementNode(copy, analyses);
                    Assert.IsNotNull(copyNode, "the copy has a node");

                    // Rename... edits the name in the tree.
                    window.Click(copyNode.NodeHeader);
                    BestFitShell.Click(copyNode.ContextMenu!.Items.OfType<MenuItem>().Single(m => (string)m.Header == "Rename..."));
                    Assert.IsTrue(copyNode.IsInEditMode);
                    BestFitShell.Frame();
                    foreach (char c in "MCMC copy")
                    {
                        window.KeyTextInput(c.ToString());
                    }
                    window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
                    BestFitShell.Frame();
                    Assert.AreEqual("MCMC copy", copy.Name);
                    Assert.AreEqual("MCMC copy", copyNode.NodeHeader.HeaderText);

                    // Delete... (from the node's context menu, which a right click opens and selects the node
                    // for) asks first, then removes it. It deletes a node the project opened with: a right click
                    // on a node added since (the copy) doesn't select it in the port's explorer (FrameworkUI).
                    window.RightClick(originalNode.NodeHeader);
                    Assert.IsTrue(originalNode.ContextMenu!.IsOpen, "a right click opens the node's menu");
                    CollectionAssert.AreEqual(new Node[] { originalNode }, originalNode.ParentTreeView.SelectedNodes.ToArray(), "and selects the node");
                    MessageBoxWindow question = null;
                    using (BestFitShell.WhenOpened<MessageBoxWindow>(opened =>
                    {
                        question = opened;
                        BestFitShell.ClickButton(opened, "OK");
                    }))
                    {
                        BestFitShell.Click(originalNode.ContextMenu!.Items.OfType<MenuItem>().Single(m => (string)m.Header == "Delete..."));
                    }
                    originalNode.ContextMenu.Close();
                    Assert.IsNotNull(question, "Delete asks first");
                    Assert.AreEqual(count, collection.Count);
                    Assert.IsFalse(collection.Contains(original));
                    Assert.IsNull(ElementNode.FindElementNode(original, analyses));
                    Assert.IsTrue(collection.Contains(copy), "the copy stays");
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void BatchRun_ListsTheValidAnalyses_AndEstimatesTheSelectedOnes()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
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
                    Assert.IsNotNull(batch, "Batch Run... opens the batch window");
                    Assert.AreSame(window, batch.Owner, "for the main window, as WPF's Owner");
                    Assert.AreEqual("Batch Run " + analyses.ElementCollection.Name, batch.Title);
                    List<IElement> expected = analyses.ElementCollection.Where(a => a.IsValid || (a is CompositeAnalysis c && c.IsBatchEligible)).ToList();
                    CollectionAssert.AreEqual(expected, batch.Analyses.ToList(), "the valid (or batch-eligible) analyses");

                    var grid = batch.FindControl<CopyPasteDataGrid>("MyDataGrid")!;
                    var rows = ((IEnumerable<BatchRunItemViewModel>)grid.ItemsSource).ToList();
                    Assert.IsTrue(rows.Count > 0);
                    Assert.IsFalse(batch.FindControl<Button>("SimulateButton")!.IsEnabled, "nothing selected yet");

                    // Select the first B17C analysis and estimate it.
                    BatchRunItemViewModel row = rows.First(r => r.Element is B17CAnalysis);
                    grid.SelectedItem = row;
                    BestFitShell.Frame();
                    Assert.IsTrue(batch.FindControl<Button>("SimulateButton")!.IsEnabled);
                    Assert.IsTrue(batch.FindControl<Button>("DeselectButton")!.IsEnabled);
                    batch.FindControl<Button>("SimulateButton")!.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    var summary = batch.FindControl<TextBlock>("SummaryTextBlock")!;
                    BestFitShell.WaitUntil(() => summary.Text.StartsWith("Batch run finished", StringComparison.Ordinal), TimeSpan.FromMinutes(3), "the batch run");

                    Assert.AreEqual("Batch run finished: 1 Analysis (1 Succeeded)", summary.Text);
                    Assert.AreEqual(BatchRunStatus.Succeeded, row.Status);
                    Assert.AreEqual("Complete", row.StatusMessage);
                    Assert.AreNotEqual(string.Empty, row.DurationText);
                    Assert.IsTrue(((IAnalysisElement)row.Element).IsEstimated);
                    Assert.IsFalse(ShellPublicVariables.SimulationInProgress);
                    Assert.IsTrue(grid.IsEnabled && batch.FindControl<Button>("SelectButton")!.IsEnabled, "the window is usable again");
                    Assert.AreEqual(Visibility.Visible, batch.FindControl<Button>("SimulateButton")!.Visibility);
                    Assert.AreEqual(Visibility.Hidden, batch.FindControl<Button>("CancelButton")!.Visibility);

                    // Select All and Deselect All.
                    BestFitShell.ClickButton(batch, "Select All");
                    Assert.AreEqual(rows.Count, grid.SelectedItems.Count);
                    BestFitShell.ClickButton(batch, "Deselect All");
                    Assert.AreEqual(0, grid.SelectedItems.Count);
                    Assert.IsFalse(batch.FindControl<Button>("SimulateButton")!.IsEnabled);
                }
                finally
                {
                    batch?.Close();
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void HelpMenu_HasUpstreamsItems_WithTheirAddressesTermsAndAbout()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, _) = BestFitShell.Open(null);
                    List<MenuItem> items = node.HelpMenuItems.ToList();
                    Assert.AreEqual(6, items.Count);
                    CollectionAssert.AreEqual(new[] { "User Guide", "Technical Reference", "Example Projects", null, "Terms & Conditions for Use", "About " + ApplicationAttributes.Title },
                        items.Select(item => item.Header as string).ToArray());
                    List<object> helpMenu = window.FindControl<MenuItem>("HelpMenu")!.Items.Cast<object>().ToList();
                    CollectionAssert.AreEqual(items, helpMenu.Skip(helpMenu.IndexOf(items[0])).Take(6).ToList(), "the Help menu shows them");

                    // The online resources: upstream's addresses (RMC-BestFit's documentation), which the
                    // handlers open with OnlineHelpLauncher; the tests read them rather than open a browser.
                    string[] urls = items.Take(3).Select(item => BestFitShell.Captured(BestFitShell.ClickHandlers(item).Single(), "url") as string).ToArray();
                    CollectionAssert.AreEqual(new[] { OnlineHelpLauncher.UserGuideUrl, OnlineHelpLauncher.TechnicalReferenceUrl, OnlineHelpLauncher.ExampleProjectsUrl }, urls);

                    window.FindControl<MenuItem>("HelpMenu")!.Open();
                    BestFitShell.Frame();
                    Assert.AreEqual(Signature(Application.Current!.FindResource("HelpImage") as IImage), Signature(((Image)items[0].Icon).Source), "User Guide has the Help icon");
                    Assert.IsNull(items[1].Icon);
                    Assert.IsNull(items[2].Icon);
                    MenuItem separator = items[3];
                    Assert.IsFalse(separator.IsEnabled || separator.Focusable || separator.IsHitTestVisible, "the separator is inert");
                    Border line = separator.GetVisualDescendants().OfType<Border>().Single();
                    Assert.AreEqual(1d, line.Bounds.Height);
                    Assert.AreEqual(new Thickness(28, 4, 4, 4), line.Margin);
                    Assert.AreSame(Application.Current.FindResource("MenuPopupDefaultSeparator"), line.Background);
                    Assert.IsInstanceOfType(items[4].Icon, typeof(Viewbox), "Terms has its icon");
                    Assert.IsInstanceOfType(items[5].Icon, typeof(Viewbox), "About has its icon");
                    window.FindControl<Menu>("MainMenu")!.Close();

                    // Terms & Conditions for Use: the 0BSD license and citation, read-only, its links live.
                    TermsAndConditionsWindow terms = null;
                    bool linksEnabled = false;
                    string firstParagraph = null;
                    using (BestFitShell.WhenOpened<TermsAndConditionsWindow>(opened =>
                    {
                        terms = opened;
                        var box = opened.FindControl<global::WpfCompat.Controls.RichTextBox>("TCURichTextBox")!;
                        linksEnabled = box.IsDocumentEnabled && box.IsReadOnly;
                        firstParagraph = string.Concat(((global::WpfCompat.Documents.Paragraph)box.Document.Blocks[0]).Inlines.OfType<global::WpfCompat.Documents.Run>().Select(r => r.Text));
                        opened.Close();
                    }))
                    {
                        BestFitShell.Click(items[4]);
                    }
                    Assert.IsNotNull(terms);
                    Assert.IsFalse(terms.ShowButtons, "a read-only view, without the agreement buttons");
                    Assert.IsTrue(linksEnabled, "the citation link can be clicked; the document stays read-only");
                    Assert.AreEqual("RMC-BestFit License and Citation", firstParagraph);

                    // About: the application's own name and icon (BestFit, XGoodFitIcon).
                    AboutWindow about = null;
                    string title = null;
                    using (BestFitShell.WhenOpened<AboutWindow>(opened =>
                    {
                        about = opened;
                        title = opened.Title;
                        opened.Close();
                    }))
                    {
                        BestFitShell.Click(items[5]);
                    }
                    Assert.IsNotNull(about);
                    Assert.AreSame(Application.Current.FindResource("BestFit"), about.SoftwareImage);
                    Assert.AreEqual(ApplicationAttributes.Title, about.SoftwareName);
                    Assert.AreEqual("About " + ApplicationAttributes.Title, title);
                    Assert.AreEqual(RMC_BestFit.App.ProductVersion, about.SoftwareVersion);
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        [TestMethod]
        public void Documents_ShowTheAreasViews_AndThePropertiesPanelFollowsTheActiveDocument()
        {
            HeadlessSession.Run(() =>
            {
                BestFitShell.Start();
                MainWindow window = null;
                try
                {
                    (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example("viglione-et-al-2013"));
                    IElement inputData = project.ElementCollections.OfType<InputDataCollection>().Single()[0];
                    IElement analysis = project.ElementCollections.OfType<UnivariateAnalysisCollection>().Single()[0];

                    // Double-clicking a node opens its element's view, with its properties beside it.
                    window.DoubleClick(ElementNode.FindElementNode(inputData, node).NodeHeader);
                    InputDataControl inputView = Shown<InputDataControl>(window);
                    Assert.AreSame(inputData, inputView.Element);
                    Assert.AreSame(inputData, node.GetControlElement(inputView));
                    InputDataPropertiesControl inputProperties = Shown<InputDataPropertiesControl>(window);
                    Assert.AreSame(inputData, inputProperties.Element);
                    Assert.AreSame(inputData, node.GetControlElement(inputProperties));

                    window.DoubleClick(ElementNode.FindElementNode(analysis, node).NodeHeader);
                    UnivariateAnalysisControl analysisView = Shown<UnivariateAnalysisControl>(window);
                    Assert.AreSame(analysis, analysisView.Element);
                    Assert.AreSame(analysis, Shown<UnivariateAnalysisPropertiesControl>(window).Element);
                    Assert.IsNull(inputProperties.Element, "the input data's properties closed (PropertiesClosed)");

                    // Back to the first document: its properties again.
                    window.DoubleClick(ElementNode.FindElementNode(inputData, node).NodeHeader);
                    Assert.AreSame(inputView, Shown<InputDataControl>(window), "the open document is activated, not opened again");
                    Assert.AreSame(inputData, Shown<InputDataPropertiesControl>(window).Element);
                    Assert.IsFalse(project.IsDirty, "showing views doesn't change the project");

                    // Every kind of element has its view and properties.
                    foreach (IElement element in project.ElementCollections.Select(c => c.FirstOrDefault()).Where(e => e != null))
                    {
                        Control view = node.GetDocumentControl(element);
                        Assert.IsNotNull(view, element.Name);
                        Assert.AreSame(element, node.GetControlElement(view), element.Name);
                        Control properties = node.GetPropertiesControl(view);
                        Assert.IsNotNull(properties, element.Name);
                        Assert.AreSame(element, node.GetControlElement(properties), element.Name);
                        node.PropertiesClosed(properties);
                        Assert.IsNull(node.GetControlElement(properties), element.Name + "'s properties let go of it");
                        node.DocumentClosed(view);
                        Assert.IsNull(node.GetPropertiesControl(element), "upstream makes properties from documents only");
                    }
                }
                finally
                {
                    BestFitShell.Close(window);
                }
            });
        }

        /// <summary>The one control of type T the window shows.</summary>
        private static T Shown<T>(Window window) where T : Control
        {
            BestFitShell.Frame();
            List<T> shown = window.GetVisualDescendants().OfType<T>().ToList();
            Assert.AreEqual(1, shown.Count, typeof(T).Name + " shown");
            return shown[0];
        }

        /// <summary>What an icon draws: its drawings' geometries' bounds, which tell the icons apart.</summary>
        private static string Signature(IImage image) => image switch
        {
            DrawingImage drawingImage => Describe(drawingImage.Drawing),
            null => "none",
            _ => image.GetType().Name + image.Size,
        };

        private static string Describe(Drawing drawing) => drawing switch
        {
            DrawingGroup group => "[" + string.Join(",", group.Children.Select(Describe)) + "]",
            GeometryDrawing geometry => geometry.Geometry?.Bounds.ToString() ?? "-",
            null => "-",
            _ => drawing.GetType().Name,
        };

        /// <summary>A node's descendants in the explorer.</summary>
        private static IEnumerable<Node> Descendants(Node node)
        {
            foreach (Node child in node.ChildNodes)
            {
                yield return child;
                foreach (Node descendant in Descendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
}
