using Avalonia;
using Avalonia.Input;

namespace WpfCompat.Input;

/// <summary>
/// The standard application commands the ported code uses, with WPF's names, texts and key gestures:
/// WPF's <c>System.Windows.Input.ApplicationCommands</c>.
/// </summary>
/// <remarks>
/// Gestures use the platform's command key: Ctrl on Windows and Linux, Cmd on macOS. The commands are
/// created on first use, after the application has started, so the platform is known.
/// </remarks>
public static class ApplicationCommands
{
    private static readonly Dictionary<string, Lazy<RoutedUICommand>> Commands = new(StringComparer.Ordinal)
    {
        ["Cut"] = Command("Cut", "Cut", (Key.X, true), (Key.Delete, KeyModifiers.Shift)),
        ["Copy"] = Command("Copy", "Copy", (Key.C, true), (Key.Insert, true)),
        ["Paste"] = Command("Paste", "Paste", (Key.V, true), (Key.Insert, KeyModifiers.Shift)),
        ["Delete"] = Command("Delete", "Delete", (Key.Delete, KeyModifiers.None)),
        ["Undo"] = Command("Undo", "Undo", (Key.Z, true)),
        ["Redo"] = Command("Redo", "Redo", (Key.Y, true)),
        ["Find"] = Command("Find", "Find", (Key.F, true)),
        ["Replace"] = Command("Replace", "Replace", (Key.H, true)),
        ["SelectAll"] = Command("SelectAll", "Select All", (Key.A, true)),
        ["New"] = Command("New", "New", (Key.N, true)),
        ["Open"] = Command("Open", "Open", (Key.O, true)),
        ["Save"] = Command("Save", "Save", (Key.S, true)),
        ["SaveAs"] = Command("SaveAs", "Save As"),
        ["Close"] = Command("Close", "Close"),
        ["Print"] = Command("Print", "Print", (Key.P, true)),
        ["Help"] = Command("Help", "Help", (Key.F1, KeyModifiers.None)),
        ["Properties"] = Command("Properties", "Properties", (Key.F4, KeyModifiers.None)),
    };

    /// <summary>Cut: Ctrl+X or Shift+Delete.</summary>
    public static RoutedUICommand Cut => Commands["Cut"].Value;

    /// <summary>Copy: Ctrl+C or Ctrl+Insert.</summary>
    public static RoutedUICommand Copy => Commands["Copy"].Value;

    /// <summary>Paste: Ctrl+V or Shift+Insert.</summary>
    public static RoutedUICommand Paste => Commands["Paste"].Value;

    /// <summary>Delete: Delete.</summary>
    public static RoutedUICommand Delete => Commands["Delete"].Value;

    /// <summary>Undo: Ctrl+Z.</summary>
    public static RoutedUICommand Undo => Commands["Undo"].Value;

    /// <summary>Redo: Ctrl+Y.</summary>
    public static RoutedUICommand Redo => Commands["Redo"].Value;

    /// <summary>Find: Ctrl+F.</summary>
    public static RoutedUICommand Find => Commands["Find"].Value;

    /// <summary>Replace: Ctrl+H.</summary>
    public static RoutedUICommand Replace => Commands["Replace"].Value;

    /// <summary>Select All: Ctrl+A.</summary>
    public static RoutedUICommand SelectAll => Commands["SelectAll"].Value;

    /// <summary>New: Ctrl+N.</summary>
    public static RoutedUICommand New => Commands["New"].Value;

    /// <summary>Open: Ctrl+O.</summary>
    public static RoutedUICommand Open => Commands["Open"].Value;

    /// <summary>Save: Ctrl+S.</summary>
    public static RoutedUICommand Save => Commands["Save"].Value;

    /// <summary>Save As: no gesture.</summary>
    public static RoutedUICommand SaveAs => Commands["SaveAs"].Value;

    /// <summary>Close: no gesture.</summary>
    public static RoutedUICommand Close => Commands["Close"].Value;

    /// <summary>Print: Ctrl+P.</summary>
    public static RoutedUICommand Print => Commands["Print"].Value;

    /// <summary>Help: F1.</summary>
    public static RoutedUICommand Help => Commands["Help"].Value;

    /// <summary>Properties: F4.</summary>
    public static RoutedUICommand Properties => Commands["Properties"].Value;

    /// <summary>Finds a command by name, as WPF's command converter does for <c>Command="Save"</c>.</summary>
    public static RoutedUICommand? FromName(string name)
    {
        // WPF also accepts the qualified form.
        const string prefix = "ApplicationCommands.";
        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            name = name[prefix.Length..];
        }
        return Commands.TryGetValue(name, out Lazy<RoutedUICommand>? command) ? command.Value : null;
    }

    // A gesture is a key with either explicit modifiers or, if the flag is set, the platform's command key.
    private static Lazy<RoutedUICommand> Command(string name, string text, params (Key Key, object Modifiers)[] gestures) =>
        new(() => new RoutedUICommand(text, name, typeof(ApplicationCommands),
            gestures.Select(g => new KeyGesture(g.Key, g.Modifiers is KeyModifiers modifiers ? modifiers : CommandKey()))));

    private static KeyModifiers CommandKey() =>
        Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
}
