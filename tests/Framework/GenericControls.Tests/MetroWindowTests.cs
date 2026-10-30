using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Themes;
using WpfCompat;

namespace GenericControls.Tests;

/// <summary>
/// The Metro window chrome (Themes' MetroWindowStyle and MetroDialogStyle) with the ported
/// <see cref="MetroWindow"/> and <see cref="MetroDialogWindow"/>: the caption buttons run the windows'
/// command handlers, and ResizeMode decides which buttons show, as in WPF.
/// </summary>
[TestClass]
public sealed class MetroWindowTests
{
    private static T Show<T>(T window, string theme) where T : Window
    {
        ThemeService.Instance.Initialize(Theme.Light);
        window.Theme = (ControlTheme)Application.Current!.FindResource(theme)!;
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Button CaptionButton(Window window, string name) =>
        window.GetTemplateDescendants().OfType<Button>().Single(b => b.Name == name);

    [TestMethod]
    public void CaptionButtons_MaximizeRestoreMinimizeAndClose()
    {
        HeadlessSession.Run(() =>
        {
            var window = Show(new MetroWindow { Title = "Metro" }, "MetroWindowStyle");
            Assert.AreEqual(ResizeMode.CanResizeWithGrip, window.ResizeMode, "MetroWindowStyle's ResizeMode");

            Button maximize = CaptionButton(window, "Maximize");
            Assert.IsTrue(maximize.IsEffectivelyEnabled);
            maximize.Command!.Execute(null);
            Assert.AreEqual(WindowState.Maximized, window.WindowState);
            Dispatcher.UIThread.RunJobs();
            Assert.IsFalse(maximize.IsVisible, "maximized: the restore button replaces maximize");
            Assert.IsTrue(CaptionButton(window, "Restore").IsVisible);

            CaptionButton(window, "Restore").Command!.Execute(null);
            Assert.AreEqual(WindowState.Normal, window.WindowState);

            CaptionButton(window, "Minimize").Command!.Execute(null);
            Assert.AreEqual(WindowState.Minimized, window.WindowState);

            bool closed = false;
            window.Closed += (_, _) => closed = true;
            CaptionButton(window, "Close").Command!.Execute(null);
            Assert.IsTrue(closed);
        });
    }

    [TestMethod]
    public void ResizeMode_DecidesTheButtons_AsInWpf()
    {
        HeadlessSession.Run(() =>
        {
            var window = Show(new MetroWindow { ResizeMode = ResizeMode.NoResize }, "MetroWindowStyle");
            Assert.IsFalse(window.CanResize || window.CanMinimize || window.CanMaximize);
            Assert.IsFalse(CaptionButton(window, "Minimize").IsVisible, "NoResize hides minimize");
            Assert.IsFalse(CaptionButton(window, "Maximize").IsVisible, "and maximize");
            Assert.IsFalse(CaptionButton(window, "Maximize").Command!.CanExecute(null));

            // CanMinimize keeps every button, but maximize can't run, so it shows disabled.
            window.ResizeMode = ResizeMode.CanMinimize;
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(CaptionButton(window, "Maximize").IsVisible);
            Assert.IsFalse(CaptionButton(window, "Maximize").Command!.CanExecute(null));
            Assert.IsTrue(CaptionButton(window, "Minimize").Command!.CanExecute(null));
            window.Close();
        });
    }

    [TestMethod]
    public void TitleBar_ShowsTitle_AndHidesWhereTheNativeTitleBarStays()
    {
        HeadlessSession.Run(() =>
        {
            var window = Show(new MetroWindow { Title = "Metro title" }, "MetroWindowStyle");
            Control titleBar = window.GetTemplateDescendants().OfType<Grid>().Single(g => g.Name == "TitleBar");
            Assert.IsTrue(titleBar.GetVisualDescendants().OfType<Label>().Any(l => Equals(l.Content, "Metro title")));
            // The headless platform keeps its own decorations, as Linux does, so the drawn title bar hides.
            Assert.IsFalse(window.IsExtendedIntoWindowDecorations);
            Assert.IsFalse(titleBar.IsVisible);
            window.Close();
        });
    }

    [TestMethod]
    public void MetroDialogWindow_IsAFixedSizeDialogWithACloseButton()
    {
        HeadlessSession.Run(() =>
        {
            var dialog = Show(new MetroDialogWindow(), "MetroDialogStyle");
            Assert.AreEqual(ResizeMode.NoResize, dialog.ResizeMode);
            Assert.IsFalse(dialog.ShowInTaskbar);
            Assert.AreEqual(WindowStartupLocation.CenterOwner, dialog.WindowStartupLocation);
            Assert.IsFalse(dialog.GetTemplateDescendants().OfType<Button>().Any(b => b.Name is "Minimize" or "Maximize"));

            bool closed = false;
            dialog.Closed += (_, _) => closed = true;
            CaptionButton(dialog, "Close").Command!.Execute(null);
            Assert.IsTrue(closed);
        });
    }
}
