using AvalonDock.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI.MessageWindow;
using FrameworkUI.ProjectExplorer;

namespace FrameworkUI.Tests;

/// <summary>
/// The main window: the shell's panes in their dock layout, menus, documents and the properties window,
/// the layout saved to and loaded from a project, closing with the unsaved-element prompts, and themes.
/// </summary>
[TestClass]
public sealed class MainWindowTests
{
    [TestMethod]
    public void MainWindow_ShowsTheProjectInItsPanes()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            Shell.UseTemporaryFolder();
            var (window, controller) = Shell.Open(ExplorerTests.SampleProject());
            try
            {
                var anchorables = window.MainDock.Layout.Descendents().OfType<LayoutAnchorable>().ToList();
                CollectionAssert.AreEqual(
                    new[] { ShellPublicVariables.ProjectExplorerContentID, ShellPublicVariables.MessageWindowContentID, ShellPublicVariables.PropertiesWindowContentID },
                    anchorables.Select(a => a.ContentId).ToArray());
                Assert.IsInstanceOfType(anchorables[0].Content, typeof(ProjectExplorerTreeView));
                Assert.IsInstanceOfType(anchorables[1].Content, typeof(MessageWindowControl));
                Assert.IsInstanceOfType(anchorables[2].Content, typeof(ProjectPropertiesControl));

                var tree = (ProjectExplorerTreeView)anchorables[0].Content!;
                Assert.AreSame(controller, tree.ProjectNode);
                Assert.IsTrue(tree.GetVisualDescendants().OfType<NodeHeader>().Count() >= 6, "the explorer shows the project's nodes");
                StringAssert.Contains(window.Title, "Test Project");
            }
            finally
            {
                Shell.Close(window);
            }
        });
    }
}
