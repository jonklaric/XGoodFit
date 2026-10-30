using AvalonDock.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI.MessageWindow;
using FrameworkUI.ProjectExplorer;
using GenericControls;

namespace FrameworkUI.Tests;

/// <summary>
/// The main window: the shell's panes in their dock layout, documents and the Window menu, the layout
/// saved to and loaded from a project, closing with the unsaved-element prompts, options and themes.
/// </summary>
[TestClass]
public sealed class MainWindowTests
{
    private static List<LayoutAnchorable> Anchorables(MainWindow window) =>
        window.MainDock.Layout.Descendents().OfType<LayoutAnchorable>().ToList();

    private static LayoutAnchorable Anchorable(MainWindow window, string contentId) =>
        Anchorables(window).Single(a => a.ContentId == contentId);

    private static List<LayoutDocument> Documents(MainWindow window) =>
        window.MainDock.Layout.Descendents().OfType<LayoutDocument>().ToList();

    // The Window menu's entries for the open documents, "1 name", "2 name"..., which it lists as it opens.
    private static string[] WindowMenuEntries(MainWindow window)
    {
        window.WindowMenuItem.IsSubMenuOpen = true;
        TestInput.Frame();
        string[] entries = window.WindowMenuItem.Items.OfType<MenuItem>().Select(m => m.Header as string)
            .Where(h => h is { Length: > 0 } && char.IsDigit(h[0])).Select(h => h!).ToArray();
        window.WindowMenuItem.IsSubMenuOpen = false;
        TestInput.Frame();
        return entries;
    }

    [TestMethod]
    public void MainWindow_ShowsTheProjectInItsPanes()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var (window, controller) = Shell.Open(project);
            try
            {
                var anchorables = Anchorables(window);
                CollectionAssert.AreEqual(
                    new[] { ShellPublicVariables.ProjectExplorerContentID, ShellPublicVariables.MessageWindowContentID, ShellPublicVariables.PropertiesWindowContentID },
                    anchorables.Select(a => a.ContentId).ToArray());
                Assert.IsInstanceOfType(anchorables[0].Content, typeof(ProjectExplorerTreeView));
                Assert.IsInstanceOfType(anchorables[1].Content, typeof(MessageWindowControl));
                Assert.IsInstanceOfType(anchorables[2].Content, typeof(ProjectPropertiesControl));

                var tree = (ProjectExplorerTreeView)anchorables[0].Content!;
                Assert.AreSame(controller, tree.ProjectNode);
                Assert.IsTrue(tree.GetVisualDescendants().OfType<NodeHeader>().Count() >= 6, "the explorer shows the project's nodes");
                Assert.AreEqual($"{ShellPublicVariables.SoftwareName} 1.0.0  -  {project.FullFileName}", window.Title, "the title names the project's file");
                CollectionAssert.Contains(window.ProjectMenu.Items.OfType<MenuItem>().Select(m => m.Header).ToArray(), "New Test Element...",
                    "the Project menu has the application's items");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void DoubleClick_OpensTheElementsDocument_ListedInTheWindowMenu()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var (window, controller) = Shell.Open(project);
            try
            {
                var series = project.Collection("Time Series Data");
                window.DoubleClick(ElementNode.FindElementNode(series[0], controller)!.NodeHeader);
                window.DoubleClick(ElementNode.FindElementNode(series[1], controller)!.NodeHeader);

                var documents = Documents(window);
                CollectionAssert.AreEqual(new[] { "Daily Discharge", "Daily Stage" }, documents.Select(d => d.Title).ToArray());
                Assert.AreSame(series[1], ((TestDocument)documents[1].Content!).Element, "each document is its element's");
                Assert.IsTrue(documents[1].IsSelected, "the last opened is selected");
                var properties = Anchorable(window, ShellPublicVariables.PropertiesWindowContentID).Content as TestProperties;
                Assert.AreSame(series[1], properties?.Element, "and the properties window shows its properties");
                CollectionAssert.AreEqual(new[] { "1 Daily Discharge", "2 Daily Stage" }, WindowMenuEntries(window), "the Window menu lists them");

                // Opening an element whose document is open selects that document rather than opening another.
                window.DoubleClick(ElementNode.FindElementNode(series[0], controller)!.NodeHeader);
                Assert.AreEqual(2, Documents(window).Count);
                Assert.IsTrue(documents[0].IsSelected, "the open document is selected");

                // Closing a document gives it back to the application and takes it off the Window menu.
                var closing = (TestDocument)documents[1].Content!;
                documents[1].Close();
                Dispatcher.UIThread.RunJobs();
                CollectionAssert.Contains(controller.ClosedDocuments, closing);
                CollectionAssert.AreEqual(new[] { "1 Daily Discharge" }, WindowMenuEntries(window));
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void Closing_AsksAboutUnsavedElements_CancelKeepsTheWindowAndSaveSavesThem()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var (window, _) = Shell.Open(project);
            try
            {
                var element = (TestElement)project.Collection("Time Series Data")[1];
                element.IsDirty = true;

                string[]? listed = null;
                using (Dialogs.WhenOpened<SaveElementsDialog>(dialog =>
                {
                    listed = dialog.ElementItems.Select(i => i.Element.Name).ToArray();
                    Dialogs.Click(dialog, "Cancel");
                }))
                {
                    window.Close();
                }
                CollectionAssert.AreEqual(new[] { "Daily Stage" }, listed, "the dialog lists the unsaved elements");
                Assert.IsTrue(window.IsVisible, "Cancel keeps the window open");
                Assert.AreEqual(0, element.Saves);

                using (Dialogs.WhenOpened<SaveElementsDialog>(dialog => Dialogs.Click(dialog, "Save")))
                {
                    window.Close();
                }
                Assert.IsFalse(window.IsVisible, "Save closes the window");
                Assert.AreEqual(1, element.Saves, "having saved the element");
                CollectionAssert.Contains(project.Calls, "Save", "and the project");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void Closing_WithoutSaving_RevertsTheElements()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var (window, _) = Shell.Open(project);
            try
            {
                var element = (TestElement)project.Collection("Fitting Analysis")[0];
                element.IsDirty = true;
                using (Dialogs.WhenOpened<SaveElementsDialog>(dialog => Dialogs.Click(dialog, "Don't Save")))
                {
                    window.Close();
                }
                Assert.IsFalse(window.IsVisible);
                Assert.AreEqual(0, element.Saves, "Don't Save doesn't save");
                Assert.AreEqual(1, element.Opens, "but reloads the element's saved state");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void Closing_AsksAboutAnUnsavedProject()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var (window, _) = Shell.Open(project);
            try
            {
                project.IsDirty = true;
                string? message = null;
                using (Dialogs.WhenOpened<MessageBoxWindow>(box =>
                {
                    message = Dialogs.Text(box);
                    Dialogs.Click(box, "Yes");
                }))
                {
                    window.Close();
                }
                StringAssert.Contains(message, "The project has unsaved changes. Would you like to save before closing?");
                Assert.IsFalse(window.IsVisible);
                CollectionAssert.Contains(project.Calls, "Save", "Yes saves the project");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void Layout_IsSavedToTheProject_AndRestoredWithItsDocuments()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var project = ExplorerTests.SampleProject();
            var series = project.Collection("Time Series Data");
            var (window, controller) = Shell.Open(project);
            window.DoubleClick(ElementNode.FindElementNode(series[1], controller)!.NodeHeader);
            window.Close();
            Dispatcher.UIThread.RunJobs();
            StringAssert.Contains(project.AvalonDockLayout, "<LayoutDocument Title=\"Daily Stage\"", "closing saves the layout to the project");
            Assert.IsTrue(File.Exists(ShellPublicVariables.DefaultAvalonDockLayoutFilePath), "and as the default layout");

            // Opening the project again restores its document.
            (window, controller) = Shell.Open(project);
            try
            {
                var document = Documents(window).Single();
                Assert.AreEqual("Daily Stage", document.Title);
                Assert.AreSame(series[1], ((TestDocument)document.Content!).Element);
                CollectionAssert.AreEqual(new[] { "1 Daily Stage" }, WindowMenuEntries(window));
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void StoredLayout_OfABestFitProject_IsRestored()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            // As a session of upstream's application left it: the layout in the project, and a default layout.
            var project = new TestProject(Path.Combine(Path.GetTempPath(), "usgs-download-example.test"),
                ("Time Series Data", ["USGS - 01134500 - Daily Discharge", "USGS - 07010000 - Daily Stage"]))
            {
                AvalonDockLayout = ShellTests.StoredLayout,
            };
            Directory.CreateDirectory(ShellPublicVariables.AvalonDockLayoutFolderPath);
            File.WriteAllText(ShellPublicVariables.DefaultAvalonDockLayoutFilePath, ShellTests.StoredLayout);

            var (window, _) = Shell.Open(project);
            try
            {
                var documents = Documents(window);
                CollectionAssert.AreEqual(new[] { "USGS - 01134500 - Daily Discharge", "USGS - 07010000 - Daily Stage" },
                    documents.Select(d => d.Title).ToArray(), "the documents the layout lists are opened");
                CollectionAssert.AreEqual(project.Collection("Time Series Data").ToArray(),
                    documents.Select(d => ((TestDocument)d.Content!).Element).ToArray());
                Assert.IsTrue(documents[0].IsSelected, "with the one it selected selected");

                var explorerGroup = window.MainDock.Layout.Descendents().OfType<LayoutAnchorablePaneGroup>().First();
                Assert.AreEqual(340d, explorerGroup.DockWidth.Value, "and the panes at its sizes");
                Assert.IsInstanceOfType(Anchorable(window, ShellPublicVariables.ProjectExplorerContentID).Content, typeof(ProjectExplorerTreeView),
                    "the shell's panes keep their content");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void ThemeSwitch_ReachesTheShellAndItsFloatingWindows()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var (window, _) = Shell.Open(ExplorerTests.SampleProject());
            try
            {
                Anchorable(window, ShellPublicVariables.PropertiesWindowContentID).Float();
                Dispatcher.UIThread.RunJobs();
                Window floating = window.MainDock.FloatingWindows.OfType<Window>().Single();
                var tree = (ProjectExplorerTreeView)Anchorable(window, ShellPublicVariables.ProjectExplorerContentID).Content!;

                foreach (ThemeColor theme in new[] { ThemeColor.Dark, ThemeColor.Blue, ThemeColor.Light })
                {
                    ThemeManager.SetTheme(theme);
                    Dispatcher.UIThread.RunJobs();
                    Assert.AreEqual(DockThemeTests.Background(theme), DockThemeTests.BrushColour(window.MainDock, "Vs2013Dock.Background"), $"{theme}: the dock");
                    Assert.AreEqual(DockThemeTests.Background(theme), DockThemeTests.BrushColour(floating, "Vs2013Dock.Background"), $"{theme}: its floating window");
                    var treeBackground = (ISolidColorBrush)Application.Current!.FindResource("TreeView.Static.Background")!;
                    Assert.AreEqual(treeBackground.Color, ((ISolidColorBrush)tree.Background!).Color, $"{theme}: the explorer");
                }
            }
            finally
            {
                ThemeManager.SetTheme(ThemeColor.Light);
                Shell.Close(window);
            }
        });
    }

    [TestMethod]
    public void Options_ApplySavesTheUserSettings_AndSwitchesTheTheme()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var (window, _) = Shell.Open(ExplorerTests.SampleProject());
            try
            {
                using (Dialogs.WhenOpened<OptionsDialog>(dialog =>
                {
                    Assert.AreEqual("Light", dialog.GeneralOptions.ColorThemeComboBox.SelectedItem, "the dialog shows the settings");
                    dialog.GeneralOptions.ColorThemeComboBox.SelectedItem = "Dark";
                    dialog.GeneralOptions.LayoutCheckBox.IsChecked = false;
                    Dialogs.Click(dialog, "Apply");
                    Dialogs.Click(dialog, "Cancel");
                }))
                {
                    window.OptionsMenuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                }
                Assert.AreEqual(ThemeColor.Dark, ThemeManager.CurrentThemeColor, "Apply switches the theme");

                // The settings are in the user settings file.
                UserSettings.RestoreDefaults();
                UserSettings.Load(ShellPublicVariables.UserSettingsFilePath);
                Assert.AreEqual("Dark", UserSettings.ColorTheme);
                Assert.IsFalse(UserSettings.SaveWindowLayout);
            }
            finally
            {
                ThemeManager.SetTheme(ThemeColor.Light);
                Shell.Close(window);
            }
        });
    }
}
