using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Themes.Tests;

/// <summary>
/// Behaviour of the ported control themes that a screenshot alone wouldn't show.
/// </summary>
[TestClass]
public sealed class ControlThemeTests
{
    private static Window Show(Control content)
    {
        ThemeService.Instance.Initialize(Theme.Light);
        ThemeService.Instance.SetTheme(Theme.Light);
        var window = new Window { Width = 400, Height = 300, Content = content };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static T TemplatePart<T>(TemplatedControl control, string name) where T : Control =>
        control.GetTemplateDescendants().OfType<T>().Single(c => c.Name == name);

    [TestMethod]
    public void TreeViewItem_CollapsedWithChildren_ShowsItsExpander()
    {
        HeadlessSession.Run(() =>
        {
            var collapsed = new TreeViewItem { Header = "Collapsed", IsExpanded = false, Items = { new TreeViewItem { Header = "Child" } } };
            var leaf = new TreeViewItem { Header = "Leaf" };
            var window = Show(new TreeView { Items = { collapsed, leaf } });

            var expander = TemplatePart<ToggleButton>(collapsed, "Expander");
            Assert.IsFalse(collapsed.Classes.Contains(":empty"), "an item with children isn't :empty");
            Assert.AreEqual(1.0, expander.Opacity, "expander of an item with children");
            Assert.IsFalse(expander.IsChecked ?? true);
            Avalonia.Controls.Shapes.Path arrow = expander.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
            Assert.IsTrue(arrow.Bounds.Width > 0 && arrow.Bounds.Height > 0, $"arrow bounds {arrow.Bounds}");
            Assert.AreEqual(1.0, arrow.StrokeThickness, "the unexpanded arrow is an outline in WPF's default stroke thickness");

            Assert.AreEqual(0.0, TemplatePart<ToggleButton>(leaf, "Expander").Opacity, "a leaf's expander is hidden but keeps its space");
            window.Close();
        });
    }
}
