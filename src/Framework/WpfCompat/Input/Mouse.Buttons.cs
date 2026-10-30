using Avalonia.Input;
using Avalonia.Interactivity;

namespace WpfCompat.Input;

/// <summary>
/// WPF's <c>Mouse.LeftButton</c>, <c>MiddleButton</c> and <c>RightButton</c>: whether each mouse button
/// is down now, as of the latest pointer input.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has no global mouse state, so this follows the buttons of every pointer event in the
/// application, as <see cref="Keyboard"/> follows the modifier keys. A handler of a pointer event, or of
/// anything that event leads to (such as a plot model's mouse events), sees the state including that
/// event: down during a press of the button, up during its release. Call <see cref="Initialize"/>
/// before the first press the state matters for.
/// </para>
/// <para>XGoodFit addition: the shim behind ported <c>Mouse.LeftButton == MouseButtonState.Pressed</c> tests.</para>
/// </remarks>
public static partial class Mouse
{
    private static PointerPointProperties _buttons;

    static Mouse()
    {
        InputElement.PointerPressedEvent.AddClassHandler<InputElement>((_, e) => Follow(e), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerReleasedEvent.AddClassHandler<InputElement>((_, e) => Follow(e), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerMovedEvent.AddClassHandler<InputElement>((_, e) => Follow(e), RoutingStrategies.Tunnel, handledEventsToo: true);
        InputElement.PointerEnteredEvent.AddClassHandler<InputElement>((_, e) => Follow(e), handledEventsToo: true);
    }

    /// <summary>Starts following the buttons. The first use of a button's state also starts it, too late for the event in hand.</summary>
    public static void Initialize()
    {
    }

    /// <summary>Whether the left button is down, as WPF's <c>Mouse.LeftButton</c>.</summary>
    public static MouseButtonState LeftButton => _buttons.IsLeftButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;

    /// <summary>Whether the middle button is down, as WPF's <c>Mouse.MiddleButton</c>.</summary>
    public static MouseButtonState MiddleButton => _buttons.IsMiddleButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;

    /// <summary>Whether the right button is down, as WPF's <c>Mouse.RightButton</c>.</summary>
    public static MouseButtonState RightButton => _buttons.IsRightButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;

    private static void Follow(PointerEventArgs e) => _buttons = e.GetCurrentPoint(null).Properties;
}
