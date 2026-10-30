using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI.ProjectExplorer;
using GenericControls;

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

    [TestMethod]
    public void Rename_F2EditsTheNameAndEnterAppliesIt()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var project = SampleProject();
            var (window, _, root) = Show(project);
            try
            {
                var series = project.Collection("Time Series Data");
                ElementNode node = ElementNode.FindElementNode(series[0], root)!;
                window.Click(node.NodeHeader);
                window.Press(Key.F2);
                Assert.IsTrue(node.IsInEditMode, "F2 starts renaming the selected node");
                Assert.IsTrue(node.NodeHeader.ShowRenameTextBox, "in its rename text box");
                Assert.IsInstanceOfType(window.FocusManager!.GetFocusedElement(), typeof(TextBox), "which has the focus");

                window.Type("Annual Peaks");
                window.Press(Key.Enter);
                Assert.IsFalse(node.IsInEditMode);
                Assert.IsFalse(node.NodeHeader.ShowRenameTextBox);
                Assert.AreEqual("Annual Peaks", series[0].Name, "Enter renames the element, the typing replacing the selected name");
                Assert.AreEqual("Annual Peaks", node.NodeHeader.HeaderText);
                Assert.IsTrue(root.ChildNodes[0].IsExpanded, "Enter leaves the collection expanded, as WPF's tree view items have no Enter key");

                // A sibling's name is refused, and Escape cancels.
                window.Click(node.NodeHeader);
                Assert.IsTrue(node.IsSelected, "a click selects it again");
                window.Press(Key.F2);
                window.Type("Daily Stage");
                Assert.IsFalse(node.NodeHeader.RenameTextBox.IsValid, "a sibling's name isn't valid");
                window.Press(Key.Enter);
                Assert.AreEqual("Annual Peaks", series[0].Name, "so Enter keeps the old name");

                window.Click(node.NodeHeader);
                window.Press(Key.F2);
                window.Type("Other");
                window.Press(Key.Escape);
                Assert.IsFalse(node.IsInEditMode);
                Assert.AreEqual("Annual Peaks", series[0].Name, "Escape cancels");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void ContextMenu_RightClickSelectsTheNodeAndShowsItsCommands()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var project = SampleProject();
            var (window, tree, root) = Show(project);
            try
            {
                var series = project.Collection("Time Series Data");
                ElementNode node = ElementNode.FindElementNode(series[1], root)!;
                window.Click(node.NodeHeader, MouseButton.Right);
                CollectionAssert.AreEqual(new Node[] { node }, tree.SelectedNodes.ToArray(), "right-clicking selects the node");
                Assert.IsTrue(node.ContextMenu!.IsOpen, "and opens its context menu");

                string?[] shown = node.ContextMenu.Items.OfType<MenuItem>().Where(m => m.IsVisible).Select(m => m.Header as string).ToArray();
                CollectionAssert.IsSubsetOf(new[] { "Edit...", "Copy...", "Rename...", "Delete...", "Move Up", "Move Down" }, shown);
                var edit = node.ContextMenu.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Edit...");
                Assert.AreEqual(new KeyGesture(Key.E, Application.Current!.PlatformSettings!.HotkeyConfiguration.CommandModifiers), edit.InputGesture,
                    "with the platform's command key in its shortcuts");

                // Its commands act on the node: Rename starts renaming it.
                var rename = node.ContextMenu.Items.OfType<MenuItem>().First(m => (string?)m.Header == "Rename...");
                rename.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                node.ContextMenu.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.IsTrue(node.IsInEditMode, "Rename... starts renaming");

                // A collection's menu has its own commands.
                var collectionNode = (ElementNodeCollection)root.ChildNodes[0];
                window.Click(collectionNode.NodeHeader, MouseButton.Right);
                Assert.IsTrue(collectionNode.ContextMenu!.IsOpen);
                collectionNode.ContextMenu.Close();
            }
            finally
            {
                window.Close();
            }
        });
    }

    [TestMethod]
    public void DragDrop_MovesANodeWhereTheInsertionAdornerShows()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var project = SampleProject();
            var (window, _, root) = Show(project);
            try
            {
                var series = project.Collection("Time Series Data");
                var seriesNode = (ElementNodeCollection)root.ChildNodes[0];
                ElementNode dragged = ElementNode.FindElementNode(series[0], root)!;
                ElementNode target = ElementNode.FindElementNode(series[2], root)!;

                // Pressing on a node and moving the mouse starts dragging it...
                Point start = window.CenterOf(dragged.NodeHeader);
                window.MouseDown(start, MouseButton.Left);
                window.MouseMove(start + new Point(0, 12), RawInputModifiers.LeftMouseButton);

                // ...and the platform's drag over the bottom half of another node shows the insertion adorner.
                var data = new DataTransfer();
                var item = new DataTransferItem();
                item.Set(ExplorerTreeView.NodeDataFormat, dragged);
                item.SetText(dragged.GetDragText());
                data.Add(item);
                Point over = window.PointOf(target.NodeHeader, new Point(target.NodeHeader.Bounds.Width / 2, target.NodeHeader.Bounds.Height * 0.75));
                window.DragDrop(over, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.LeftMouseButton);
                window.DragDrop(over, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.LeftMouseButton);
                AdornerLayer layer = AdornerLayer.GetAdornerLayer(target.NodeHeader)!;
                var adorner = layer.Children.OfType<InsertionAdorner>().Single();
                Assert.AreSame(target.NodeHeader, adorner.AdornedElement, "the adorner marks the target node");
                Assert.IsFalse(adorner.IsInFirstHalf, "below it");

                // Dropping moves the node there and removes the adorner.
                window.DragDrop(over, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.LeftMouseButton);
                window.MouseUp(over, MouseButton.Left);
                Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(layer.Children.OfType<InsertionAdorner>().Any(), "the drop removes the adorner");
                CollectionAssert.AreEqual(new[] { "Daily Stage", "Peak Discharge", "Daily Discharge" },
                    seriesNode.ChildNodes.Select(n => n.NodeHeader.HeaderText).ToArray(), "and moves the node below the target");
            }
            finally
            {
                window.Close();
            }
        });
    }
}
