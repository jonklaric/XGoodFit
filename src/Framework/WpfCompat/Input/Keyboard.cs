using Avalonia.Input;
using Avalonia.Interactivity;

namespace WpfCompat.Input;

/// <summary>WPF's modifier keys (<c>System.Windows.Input.ModifierKeys</c>), with WPF's values.</summary>
[Flags]
public enum ModifierKeys
{
    /// <summary>No modifier key.</summary>
    None = 0,

    /// <summary>The Alt (Option) key.</summary>
    Alt = 1,

    /// <summary>The Control key, and on macOS also the Command key.</summary>
    Control = 2,

    /// <summary>The Shift key.</summary>
    Shift = 4,

    /// <summary>The Windows key (Command on macOS).</summary>
    Windows = 8,
}

/// <summary>
/// WPF's <c>Keyboard.Modifiers</c>: the modifier keys held down now, as of the latest keyboard or
/// pointer input.
/// </summary>
/// <remarks>
/// Avalonia has no global keyboard state, so this follows the modifiers of every key and pointer event
/// in the application, and clears them when focus leaves it. On macOS the Command key also reports as
/// <see cref="ModifierKeys.Control"/>, so that upstream's Ctrl+click and Ctrl+key handling works with
/// the Mac's usual Command+click and Command+key.
/// </remarks>
public static class Keyboard
{
    private static KeyModifiers _modifiers;

    static Keyboard()
    {
        InputElement.KeyDownEvent.AddClassHandler<InputElement>((_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.KeyUpEvent.AddClassHandler<InputElement>((_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerPressedEvent.AddClassHandler<InputElement>((_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<InputElement>((_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<InputElement>((_, e) => _modifiers = e.KeyModifiers, RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.LostFocusEvent.AddClassHandler<InputElement>((_, _) => _modifiers = KeyModifiers.None, handledEventsToo: true);
    }

    /// <summary>Starts following the modifier keys. Called by the first use of <see cref="Modifiers"/>.</summary>
    public static void Initialize()
    {
    }

    /// <summary>The modifier keys held down.</summary>
    public static ModifierKeys Modifiers
    {
        get
        {
            var keys = (ModifierKeys)(_modifiers & (KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Meta));
            if (OperatingSystem.IsMacOS() && keys.HasFlag(ModifierKeys.Windows))
            {
                keys = (keys & ~ModifierKeys.Windows) | ModifierKeys.Control;
            }
            return keys;
        }
    }

    /// <summary>
    /// Whether a key is held down, as WPF's <c>Keyboard.IsKeyDown</c>, for the modifier keys only: the
    /// Shift, Control and Alt keys and the Windows (Command) keys.
    /// </summary>
    public static bool IsKeyDown(Key key) => key switch
    {
        Key.LeftShift or Key.RightShift => Modifiers.HasFlag(ModifierKeys.Shift),
        Key.LeftCtrl or Key.RightCtrl => Modifiers.HasFlag(ModifierKeys.Control),
        Key.LeftAlt or Key.RightAlt => Modifiers.HasFlag(ModifierKeys.Alt),
        Key.LWin or Key.RWin => _modifiers.HasFlag(KeyModifiers.Meta),
        _ => throw new NotSupportedException($"WpfCompat's Keyboard.IsKeyDown only knows the modifier keys, not {key}."),
    };
}
