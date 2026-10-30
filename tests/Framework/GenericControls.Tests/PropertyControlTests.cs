using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Themes;

namespace GenericControls.Tests;

/// <summary>
/// The property editors: their rows lay out as upstream's (title, dot leader, editor), and their
/// properties reach the controls inside them.
/// </summary>
[TestClass]
public sealed class PropertyControlTests
{
    private static bool _resourcesAdded;

    // The app's resources, as upstream's App.xaml merges them: Themes, then GenericControls' theme.
    internal static void UseAppResources()
    {
        ThemeService.Instance.Initialize(Theme.Light);
        if (!_resourcesAdded)
        {
            Application.Current!.Resources.MergedDictionaries.Add(
                new ResourceInclude(new Uri("avares://GenericControls.Tests"))
                {
                    Source = new Uri("avares://GenericControls/Themes/GenericControlsTheme.axaml"),
                });
            _resourcesAdded = true;
        }
    }

    // Shows control in a window of the given width and lays it out.
    internal static Window Show(Control control, double width = 400)
    {
        UseAppResources();
        var window = new Window { Width = width, Height = 300, Content = new StackPanel { Children = { control } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    [TestMethod]
    public void TextPropertyControl_LaysOutTitleLeaderAndEditor()
    {
        HeadlessSession.Run(() =>
        {
            var control = new TextPropertyControl { Title = "Name", Text = "Sample", PropertyWidth = new GridLength(200) };
            Window window = Show(control);

            var grid = (Grid)control.Content!;
            Assert.AreEqual(22d, control.Bounds.Height);
            Assert.AreEqual(200d, grid.ColumnDefinitions[2].ActualWidth, "PropertyWidth sizes the editor column");
            TextBox box = control.GetVisualDescendants().OfType<TextBox>().Single();
            Assert.AreEqual("Sample", box.Text);
            Assert.AreEqual(200d, box.Bounds.Width);

            var leader = control.GetVisualDescendants().OfType<WpfCompat.Shapes.Line>().Single();
            Assert.IsTrue(leader.IsVisible);
            Assert.AreEqual(1d, leader.Bounds.Height, "WPF's stretched line is as tall as its stroke");
            Assert.AreEqual(grid.ColumnDefinitions[1].ActualWidth - 3, leader.Bounds.Width, "and fills its column, less the margin");
            Assert.AreEqual(21d, leader.Bounds.Y, "at the bottom of the row");

            control.ShowLeaderLine = false;
            Assert.IsFalse(leader.IsVisible);
            window.Close();
        });
    }
}
