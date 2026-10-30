using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI.ProjectExplorer;

namespace FrameworkUI.Tests;

/// <summary>A project node for the explorer's tests: upstream's ProjectNode with no menu items of its own.</summary>
public sealed class TestProjectNode(TestProject project) : ProjectNode(project)
{
    /// <inheritdoc/>
    public override bool CanMultiSelect => false;

    /// <inheritdoc/>
    protected override void DefineProjectExplorerMenuItems()
    {
    }
}

/// <summary>
/// The Project Explorer: nodes built from a project, renaming, context menus, drag-and-drop reordering
/// with the insertion adorner, and double-click opening.
/// </summary>
[TestClass]
public sealed class ExplorerTests
{
    internal static (Window Window, ProjectExplorerTreeView Tree, TestProjectNode Root) Show(TestProject project)
    {
        var root = new TestProjectNode(project) { Theme = (Avalonia.Styling.ControlTheme)Application.Current!.FindResource("TreeViewItemStyle")! };
        root.Load();
        var tree = new ProjectExplorerTreeView { Theme = (Avalonia.Styling.ControlTheme)Application.Current!.FindResource("TreeViewStyle")! };
        var window = new Window { Width = 400, Height = 500, Content = tree };
        window.Show();
        tree.ProjectNode = root;
        Dispatcher.UIThread.RunJobs();
        return (window, tree, root);
    }

    internal static TestProject SampleProject() => new(
        Path.Combine(Path.GetTempPath(), "xgf-explorer.test"),
        ("Time Series Data", ["Daily Discharge", "Daily Stage", "Peak Discharge"]),
        ("Input Data", []),
        ("Fitting Analysis", ["Fit A"]));

    [TestMethod]
    public void Nodes_AreBuiltFromTheProject()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var (window, tree, root) = Show(SampleProject());
            try
            {
                Assert.AreEqual(3, root.ChildNodes.Count);
                var series = (ElementNodeCollection)root.ChildNodes[0];
                Assert.AreEqual("Time Series Data", series.NodeHeader.HeaderText);
                CollectionAssert.AreEqual(new[] { "Daily Discharge", "Daily Stage", "Peak Discharge" },
                    series.ChildNodes.Cast<ElementNode>().Select(n => n.NodeHeader.HeaderText).ToArray());

                // Every node is laid out and drawn with its theme's template.
                foreach (Node node in new Node[] { root, series, series.ChildNodes[0] })
                {
                    Assert.IsTrue(node.Bounds.Height > 0, $"{node.NodeHeader.HeaderText} is laid out");
                    Assert.IsTrue(node.GetVisualDescendants().OfType<NodeHeader>().Any(), $"{node.NodeHeader.HeaderText} shows its header");
                }
            }
            finally
            {
                window.Close();
            }
        });
    }
}
