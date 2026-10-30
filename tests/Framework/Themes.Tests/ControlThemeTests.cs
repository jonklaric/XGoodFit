using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
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

    // Every control type the ported themes give an implicit (type-keyed) theme.
    private static readonly Type[] ImplicitlyThemed =
    [
        typeof(Button), typeof(ToggleButton), typeof(CheckBox), typeof(RadioButton), typeof(TextBox),
        typeof(ComboBox), typeof(ComboBoxItem), typeof(ListBox), typeof(ListBoxItem), typeof(Menu),
        typeof(MenuItem), typeof(ContextMenu), typeof(ScrollBar), typeof(ScrollViewer), typeof(Separator),
        typeof(TabControl), typeof(TabItem), typeof(TreeView), typeof(TreeViewItem), typeof(ToolTip),
        typeof(Label), typeof(GroupBox), typeof(Slider), typeof(ProgressBar), typeof(Calendar),
        typeof(CalendarItem), typeof(CalendarDayButton), typeof(CalendarButton),
        typeof(DataGrid), typeof(DataGridRow), typeof(DataGridCell),
    ];

    // The template a theme sets, following BasedOn: the implicit themes are mostly based on keyed ones.
    private static object? TemplateOf(ControlTheme theme)
    {
        for (ControlTheme? current = theme; current != null; current = current.BasedOn)
        {
            if (current.Setters.OfType<Setter>().FirstOrDefault(s => s.Property == TemplatedControl.TemplateProperty) is { } setter)
            {
                return setter.Value;
            }
        }
        return null;
    }

    [TestMethod]
    public void ImplicitThemes_AreThePortedOnes_AndTheirTemplatesApply()
    {
        HeadlessSession.Run(() =>
        {
            var panel = new StackPanel();
            var window = Show(panel);
            // The control templates dictionary (Merged.axaml) is the one that defines RepeatButtonTransparent.
            IResourceProvider ported = Application.Current!.Resources.MergedDictionaries
                .OfType<ResourceDictionary>()
                .Single(d => d.ContainsKey("RepeatButtonTransparent"));
            foreach (Type type in ImplicitlyThemed)
            {
                Assert.IsTrue(ported.TryGetResource(type, null, out object? found) && found is ControlTheme,
                    $"{type.Name} has no ported implicit theme");
                var theme = (ControlTheme)found!;
                if (type == typeof(CalendarItem) || type == typeof(CalendarDayButton) || type == typeof(CalendarButton)
                    || type == typeof(ContextMenu) || type == typeof(ToolTip) || type == typeof(DataGridRow) || type == typeof(DataGridCell))
                {
                    continue; // created and hosted by their owners
                }
                var control = (TemplatedControl)Activator.CreateInstance(type)!;
                panel.Children.Add(control);
                Dispatcher.UIThread.RunJobs();
                Assert.AreSame(TemplateOf(theme), control.Template, $"{type.Name} doesn't use the ported template");
                Assert.IsTrue(control.GetTemplateDescendants().Any(), $"{type.Name}'s template wasn't applied");
            }
            window.Close();
        });
    }

    // A grid's row.
    public sealed record GridRow(string Name, double Value);

    [TestMethod]
    public void DataGrid_ShowsScrollBars_WhenItsRowsDontFit_AsWpfDoes()
    {
        HeadlessSession.Run(() =>
        {
            var grid = new DataGrid { ItemsSource = Enumerable.Range(1, 50).Select(i => new GridRow("Item " + i, i)).ToList() };
            var window = Show(grid);

            Assert.AreEqual(ScrollBarVisibility.Auto, grid.VerticalScrollBarVisibility, "WPF's default");
            Assert.AreEqual(ScrollBarVisibility.Auto, grid.HorizontalScrollBarVisibility, "WPF's default");
            Assert.IsTrue(TemplatePart<ScrollBar>(grid, "PART_VerticalScrollbar").IsVisible, "fifty rows don't fit in 300 pixels");
            Assert.IsFalse(TemplatePart<ScrollBar>(grid, "PART_HorizontalScrollbar").IsVisible, "the two columns fit");
            window.Close();
        });
    }

    [TestMethod]
    public void TextBox_WrapsItsText_WhenTextWrappingIsWrap_AsWpfDoes()
    {
        HeadlessSession.Run(() =>
        {
            const string text = "Daily mean discharge for Moose River at Victory, VT. Record begins 1947.";
            var wrapping = new TextBox { Text = text, TextWrapping = Avalonia.Media.TextWrapping.Wrap, Width = 120 };
            var single = new TextBox { Text = text, Width = 120 };
            var window = Show(new StackPanel { Children = { wrapping, single } });

            // Avalonia's text box disables horizontal scrolling while it wraps, which its template must follow:
            // a scroll viewer that scrolls sideways measures the text unbounded, on one line.
            Assert.AreEqual(ScrollBarVisibility.Disabled, TemplatePart<ScrollViewer>(wrapping, "PART_ScrollViewer").HorizontalScrollBarVisibility);
            Assert.IsTrue(wrapping.Bounds.Height > 2 * single.Bounds.Height, $"the text wraps onto several lines ({wrapping.Bounds.Height} vs {single.Bounds.Height})");
            Assert.AreEqual(ScrollBarVisibility.Hidden, TemplatePart<ScrollViewer>(single, "PART_ScrollViewer").HorizontalScrollBarVisibility, "WPF's default");
            window.Close();
        });
    }

    [TestMethod]
    public void DataGrid_HidesItsRowOfHeaders_WithTheColumnHeaders()
    {
        HeadlessSession.Run(() =>
        {
            var grid = new DataGrid { ItemsSource = new List<GridRow> { new("Item", 1) } };
            var window = Show(grid);
            var filler = TemplatePart<DataGridColumnHeader>(grid, "FillerColumnHeader");
            Assert.IsTrue(filler.IsVisible, "the header behind the column headers");

            grid.HeadersVisibility = DataGridHeadersVisibility.None;
            Dispatcher.UIThread.RunJobs();
            Assert.IsFalse(filler.IsVisible, "WPF's is in the column headers' presenter, which is hidden");
            Assert.AreEqual(0, TemplatePart<DataGridRowsPresenter>(grid, "PART_RowsPresenter").Bounds.Top, "the rows start at the top");

            grid.HeadersVisibility = DataGridHeadersVisibility.Column;
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(filler.IsVisible);
            window.Close();
        });
    }

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
