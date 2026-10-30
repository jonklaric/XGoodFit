using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FrameworkUI.Tests;

/// <summary>Answers the shell's modal dialogs, which wait for the user as WPF's do, as a user would.</summary>
internal static class Dialogs
{
    /// <summary>Runs <paramref name="answer"/> on the next window of type T that opens, once it's shown.</summary>
    public static IDisposable WhenOpened<T>(Action<T> answer) where T : Window
    {
        IDisposable? handler = null;
        handler = Window.WindowOpenedEvent.AddClassHandler<T>((window, _) =>
        {
            handler?.Dispose();
            Dispatcher.UIThread.Post(() => answer(window), DispatcherPriority.Background);
        });
        return handler;
    }

    /// <summary>The button of <paramref name="window"/> that shows <paramref name="content"/>.</summary>
    public static Button Button(Window window, string content) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, content));

    /// <summary>Clicks the button of <paramref name="window"/> that shows <paramref name="content"/>.</summary>
    public static void Click(Window window, string content)
    {
        Button(window, content).RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The texts <paramref name="window"/> shows.</summary>
    public static string Text(Window window) =>
        string.Join("\n", window.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text));
}
