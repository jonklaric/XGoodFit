using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace WpfCompat.Tests;

/// <summary>
/// WPF's visibility, modal dialogs, file-dialog filters and clipboard, as WpfCompat provides them.
/// </summary>
[TestClass]
public sealed class WindowingTests
{
    [TestMethod]
    public void Visibility_HiddenKeepsTheSpace_AndVisibleRestoresTheOpacity()
    {
        HeadlessSession.Run(() =>
        {
            var border = new Border { Width = 40, Height = 20, Opacity = 0.5 };
            var window = new Window { Content = new StackPanel { Children = { border } } };
            window.Show();

            border.Visibility = Visibility.Hidden;
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(border.IsVisible, "hidden elements still take part in layout");
            Assert.AreEqual(0d, border.Opacity);
            Assert.IsFalse(border.IsHitTestVisible);
            Assert.AreEqual(20d, border.Bounds.Height, "and keep their space");

            border.Visibility = Visibility.Visible;
            Assert.AreEqual(0.5, border.Opacity, "its own opacity comes back");
            Assert.IsTrue(border.IsHitTestVisible);

            border.Visibility = Visibility.Collapsed;
            Assert.IsFalse(border.IsVisible);
            Assert.AreEqual(Visibility.Collapsed, border.Visibility);
            window.Close();
        });
    }

    [TestMethod]
    public void Visibility_AgreesWithIsVisibleSetInXaml()
    {
        // Ported XAML writes WPF's Visibility="Collapsed" as IsVisible="False".
        var border = new Border { IsVisible = false };
        Assert.AreEqual(Visibility.Collapsed, border.Visibility);

        border.Visibility = Visibility.Visible;
        Assert.IsTrue(border.IsVisible, "code can show it again");
        Assert.AreEqual(Visibility.Visible, border.Visibility);
    }

    [TestMethod]
    public void ShowDialog_WaitsAndReturnsTheDialogResult()
    {
        HeadlessSession.Run(() =>
        {
            var owner = new Window();
            owner.Show();

            var accepted = new Window();
            DialogWindows.SetRequestedOwner(accepted, owner);
            object? ownerWhileOpen = null;
            Dispatcher.UIThread.Post(() =>
            {
                ownerWhileOpen = accepted.Owner;
                accepted.DialogResult = true;
            }, DispatcherPriority.Background);
            Assert.IsTrue(accepted.ShowDialog(), "setting DialogResult closes the dialog with that result");
            Assert.AreSame(owner, ownerWhileOpen, "shown for its owner");

            var closed = new Window();
            DialogWindows.SetRequestedOwner(closed, owner);
            Dispatcher.UIThread.Post(closed.Close, DispatcherPriority.Background);
            Assert.IsFalse(closed.ShowDialog(), "closed without a result: false, as in WPF");
            owner.Close();
        });
    }

    private sealed class FilterProbe : FileDialog
    {
        public IReadOnlyList<FilePickerFileType> Types => FileTypes();

        public string? Extension => DefaultExtension();

        protected override Task<string[]> PickAsync(IStorageProvider storage) => Task.FromResult(Array.Empty<string>());
    }

    [TestMethod]
    public void FileDialogFilter_ParsesWpfsFormat()
    {
        var dialog = new FilterProbe
        {
            Filter = "BestFit projects (*.bestfit)|*.bestfit|Text and CSV files|*.txt;*.csv|All files (*.*)|*.*",
            DefaultExt = ".bestfit",
        };
        Assert.AreEqual(3, dialog.Types.Count);
        Assert.AreEqual("BestFit projects (*.bestfit)", dialog.Types[0].Name);
        CollectionAssert.AreEqual(new[] { "*.txt", "*.csv" }, dialog.Types[1].Patterns!.ToArray());
        Assert.AreEqual("bestfit", dialog.Extension);
    }

    [TestMethod]
    public void Clipboard_RoundTripsText()
    {
        HeadlessSession.Run(() =>
        {
            // The clipboard belongs to the window with focus.
            Input.CommandManager.InvalidateRequerySuggested();
            var text = new TextBox();
            var window = new Window { Content = text };
            window.Show();
            text.Focus();
            Clipboard.SetText("1\t2\n3\t4");
            Assert.IsTrue(Clipboard.ContainsText());
            Assert.AreEqual("1\t2\n3\t4", Clipboard.GetText());
            window.Close();
        });
    }
}
