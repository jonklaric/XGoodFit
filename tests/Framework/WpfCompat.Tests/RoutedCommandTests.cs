using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using WpfCompat.Input;

namespace WpfCompat.Tests;

/// <summary>
/// The routed-command shim behaves as WPF's routed commands do for the ported code: commands route from
/// their target up to the first binding that handles them.
/// </summary>
[TestClass]
public sealed class RoutedCommandTests
{
    private static (Window Window, TextBox Focused) ShowWindow()
    {
        // The command manager tracks focus from its first use; the real app uses it from the start.
        CommandManager.InvalidateRequerySuggested();
        var text = new TextBox();
        var window = new Window { Content = new StackPanel { Children = { text } } };
        window.Show();
        text.Focus();
        Dispatcher.UIThread.RunJobs();
        return (window, text);
    }

    [TestMethod]
    public void Execute_WithoutTarget_RoutesFromTheFocusedElementToTheWindowsBinding()
    {
        HeadlessSession.Run(() =>
        {
            var (window, text) = ShowWindow();
            object? sender = null;
            ExecutedRoutedEventArgs? args = null;
            window.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (s, e) => (sender, args) = (s, e)));

            ICommand save = ApplicationCommands.Save;
            Assert.IsTrue(save.CanExecute("parameter"));
            save.Execute("parameter");

            Assert.AreSame(window, sender, "the binding's element is the sender");
            Assert.AreSame(text, args!.Source, "the focused element is the source");
            Assert.AreEqual("parameter", args.Parameter);
            Assert.AreSame(ApplicationCommands.Save, args.Command);
            window.Close();
        });
    }

    [TestMethod]
    public void CanExecute_IsFalseWithoutABinding_AndFollowsTheBindingsHandler()
    {
        HeadlessSession.Run(() =>
        {
            var (window, text) = ShowWindow();
            Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, text), "no binding");

            bool allowed = false;
            window.CommandBindings.Add(new CommandBinding(ApplicationCommands.Undo, (_, _) => { }, (_, e) => e.CanExecute = allowed));
            Assert.IsFalse(ApplicationCommands.Undo.CanExecute(null, text));
            allowed = true;
            Assert.IsTrue(ApplicationCommands.Undo.CanExecute(null, text));
            window.Close();
        });
    }

    [TestMethod]
    public void TheNearestBindingThatAllowsTheCommandHandlesIt()
    {
        HeadlessSession.Run(() =>
        {
            var (window, text) = ShowWindow();
            var panel = (StackPanel)window.Content!;
            var handled = new List<string>();
            window.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, _) => handled.Add("window")));
            panel.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, (_, _) => handled.Add("panel")));

            ApplicationCommands.Copy.Execute(null, text);
            CollectionAssert.AreEqual(new[] { "panel" }, handled, "the panel's binding handles it and stops the routing");

            // A binding that doesn't allow the command lets it route on, as in WPF.
            panel.CommandBindings[0].CanExecute += (_, e) => e.CanExecute = false;
            ApplicationCommands.Copy.Execute(null, text);
            CollectionAssert.AreEqual(new[] { "panel", "window" }, handled);
            window.Close();
        });
    }

    [TestMethod]
    public void KeyGesture_RunsTheCommandOfABindingOnTheRoute()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _) = ShowWindow();
            int saved = 0;
            window.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, (_, _) => saved++));

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            Dispatcher.UIThread.RunJobs();

            Assert.AreEqual(1, saved, "Ctrl+S runs Save");
            window.Close();
        });
    }

    [TestMethod]
    public void CommandSource_RoutesFromItsElement_NotTheFocusedOne()
    {
        HeadlessSession.Run(() =>
        {
            // WPF routes a button's command from the button, so caption buttons work in an inactive window.
            var (first, _) = ShowWindow();
            var button = new Button();
            var second = new Window { Content = button };
            second.Show();
            int closed = 0;
            second.CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, (_, _) => closed++));

            ICommand command = new RoutedCommandSource(SystemCommands.CloseWindowCommand, button);
            Assert.IsTrue(command.CanExecute(null));
            command.Execute(null);
            Assert.AreEqual(1, closed);
            Assert.IsFalse(SystemCommands.CloseWindowCommand.CanExecute(null, first), "nothing binds it in the first window");
            first.Close();
            second.Close();
        });
    }

    [TestMethod]
    public void ApplicationCommands_FromName_AsWpfsConverterDoes()
    {
        Assert.AreSame(ApplicationCommands.Save, ApplicationCommands.FromName("Save"));
        Assert.AreSame(ApplicationCommands.SelectAll, ApplicationCommands.FromName("ApplicationCommands.SelectAll"));
        Assert.IsNull(ApplicationCommands.FromName("NoSuchCommand"));
        Assert.AreEqual("Select All", ApplicationCommands.SelectAll.Text);
    }

    [TestMethod]
    public void RequerySuggested_IsRaisedOnceAfterInvalidation()
    {
        HeadlessSession.Run(() =>
        {
            int raised = 0;
            EventHandler handler = (_, _) => raised++;
            CommandManager.RequerySuggested += handler;
            CommandManager.InvalidateRequerySuggested();
            CommandManager.InvalidateRequerySuggested();
            Dispatcher.UIThread.RunJobs();
            CommandManager.RequerySuggested -= handler;
            Assert.AreEqual(1, raised);
        });
    }

    [TestMethod]
    public void ResizeMode_MapsToAvaloniasWindowFlags()
    {
        Assert.IsFalse(ResizeMode.NoResize.CanResize() || ResizeMode.NoResize.CanMinimize() || ResizeMode.NoResize.CanMaximize());
        Assert.IsTrue(ResizeMode.CanMinimize.CanMinimize() && !ResizeMode.CanMinimize.CanMaximize() && !ResizeMode.CanMinimize.CanResize());
        Assert.IsTrue(ResizeMode.CanResizeWithGrip.CanResize() && ResizeMode.CanResizeWithGrip.CanMaximize());
    }
}
