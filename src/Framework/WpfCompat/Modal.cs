using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace WpfCompat;

/// <summary>
/// Runs Avalonia's asynchronous operations the way WPF's synchronous ones behave: the caller waits,
/// while the user interface keeps running in a nested dispatcher frame.
/// </summary>
/// <remarks>
/// WPF's <c>ShowDialog</c>, <c>MessageBox.Show</c> and file dialogs return only when the user is done,
/// and the ported code relies on that order of operations. WPF does it with a nested message loop;
/// Avalonia 12 has the same mechanism (<see cref="Dispatcher.PushFrame"/>). See the migration plan,
/// §5.3.
/// </remarks>
public static class Modal
{
    /// <summary>Waits for <paramref name="task"/>, keeping the user interface responsive, and returns its result.</summary>
    public static T Wait<T>(Task<T> task)
    {
        WaitCore(task);
        return task.GetAwaiter().GetResult();
    }

    /// <summary>Waits for <paramref name="task"/>, keeping the user interface responsive.</summary>
    public static void Wait(Task task)
    {
        WaitCore(task);
        task.GetAwaiter().GetResult();
    }

    /// <summary>
    /// The window a dialog without an owner belongs to, as WPF picks one: the active window, or else the
    /// main window. Without a desktop lifetime (as in tests), it's the window with keyboard focus.
    /// </summary>
    public static Window? ActiveWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows.FirstOrDefault(w => w.IsActive) ?? desktop.MainWindow ?? desktop.Windows.LastOrDefault();
        }
        return Input.CommandManager.FocusedTarget() is Visual focused ? TopLevel.GetTopLevel(focused) as Window : null;
    }

    private static void WaitCore(Task task)
    {
        if (task.IsCompleted)
        {
            return;
        }
        var frame = new DispatcherFrame();
        task.ContinueWith(_ => Dispatcher.UIThread.Post(() => frame.Continue = false), TaskScheduler.Default);
        Dispatcher.UIThread.PushFrame(frame);
    }
}
