using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace WpfCompat;

/// <summary>
/// WPF's modal dialog model for Avalonia windows: a synchronous <c>ShowDialog()</c> that returns the
/// window's <c>DialogResult</c>, and setting <c>DialogResult</c> closes the dialog.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Avalonia's <see cref="Window.ShowDialog(Window)"/> needs an owner and returns a task. Here the
/// owner is the one requested with <see cref="SetRequestedOwner"/> (WPF's <c>Owner</c>; Avalonia's
/// <c>Owner</c> can only be set by showing the window), or else the active window.</item>
/// <item>As in WPF, a dialog closed without a result returns false.</item>
/// </list>
/// </remarks>
public static class DialogWindows
{
    /// <summary>The window's dialog result, as WPF's <c>Window.DialogResult</c>.</summary>
    public static readonly AttachedProperty<bool?> DialogResultProperty =
        AvaloniaProperty.RegisterAttached<Window, bool?>("DialogResult", typeof(DialogWindows));

    /// <summary>The owner a dialog is shown for, as WPF's <c>Window.Owner</c> before it's shown.</summary>
    public static readonly AttachedProperty<Window?> RequestedOwnerProperty =
        AvaloniaProperty.RegisterAttached<Window, Window?>("RequestedOwner", typeof(DialogWindows));

    private static readonly AttachedProperty<bool> IsShownAsDialogProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("IsShownAsDialog", typeof(DialogWindows));

    static DialogWindows()
    {
        // Setting the result of an open dialog closes it, as in WPF.
        DialogResultProperty.Changed.AddClassHandler<Window>((window, e) =>
        {
            if (window.GetValue(IsShownAsDialogProperty) && e.GetNewValue<bool?>() is not null)
            {
                window.Close();
            }
        });
    }

    /// <summary>Gets the window's dialog result.</summary>
    public static bool? GetDialogResult(Window window) => window.GetValue(DialogResultProperty);

    /// <summary>Sets the window's dialog result, closing it if it's an open dialog.</summary>
    public static void SetDialogResult(Window window, bool? value) => window.SetValue(DialogResultProperty, value);

    /// <summary>Gets the owner the window will be shown for.</summary>
    public static Window? GetRequestedOwner(Window window) => window.GetValue(RequestedOwnerProperty);

    /// <summary>Sets the owner the window will be shown for.</summary>
    public static void SetRequestedOwner(Window window, Window? value) => window.SetValue(RequestedOwnerProperty, value);

    /// <summary>
    /// Shows <paramref name="window"/> as a modal dialog and waits until it closes, as WPF's
    /// <c>ShowDialog()</c> does.
    /// </summary>
    /// <returns>The dialog's result: false if it closed without one.</returns>
    public static bool? ShowDialog(this Window window)
    {
        Window? owner = GetRequestedOwner(window) ?? Modal.ActiveWindow();
        if (owner == window)
        {
            owner = null;
        }

        window.SetValue(DialogResultProperty, null);
        window.SetValue(IsShownAsDialogProperty, true);
        var frame = new DispatcherFrame();
        void OnClosed(object? sender, EventArgs e)
        {
            window.SetValue(IsShownAsDialogProperty, false);
            frame.Continue = false;
        }
        window.Closed += OnClosed;
        try
        {
            if (owner != null)
            {
                _ = window.ShowDialog(owner);
            }
            else
            {
                window.Show();
            }
            Dispatcher.UIThread.PushFrame(frame);
        }
        finally
        {
            window.Closed -= OnClosed;
        }
        return GetDialogResult(window) ?? false;
    }

    extension(Window window)
    {
        /// <summary>The window's dialog result, as WPF's <c>Window.DialogResult</c>. Setting it closes an open dialog.</summary>
        public bool? DialogResult
        {
            get => GetDialogResult(window);
            set => SetDialogResult(window, value);
        }
    }
}
