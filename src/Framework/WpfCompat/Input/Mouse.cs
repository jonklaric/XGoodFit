using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

namespace WpfCompat.Input;

/// <summary>WPF's <c>Mouse.OverrideCursor</c>: one cursor for the whole application, such as Wait.</summary>
/// <remarks>
/// Avalonia has no application-wide cursor. This gives every open window the cursor, above any value
/// the window has, and every element in it inherits it. Elements with their own cursor, such as text
/// boxes and splitters, keep theirs. The cursor changes at once, so it shows during long work on the
/// user-interface thread, as in WPF.
/// </remarks>
public static partial class Mouse
{
    private static readonly List<IDisposable> Overrides = [];
    private static Cursor? _overrideCursor;

    /// <summary>The cursor every window shows, or null for each element's own.</summary>
    public static Cursor? OverrideCursor
    {
        get => _overrideCursor;
        set
        {
            _overrideCursor = value;
            foreach (IDisposable cursor in Overrides)
            {
                cursor.Dispose();
            }
            Overrides.Clear();
            if (value is null)
            {
                return;
            }
            foreach (TopLevel topLevel in TopLevels())
            {
                if (topLevel.SetValue(InputElement.CursorProperty, value, BindingPriority.Animation) is { } cursor)
                {
                    Overrides.Add(cursor);
                }
            }
        }
    }

    private static IEnumerable<TopLevel> TopLevels()
    {
        if (Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.Windows;
        }
        return Modal.ActiveWindow() is { } window ? [window] : [];
    }
}
