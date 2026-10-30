using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace WpfCompat.Input;

/// <summary>WPF's <c>MouseButtonState</c>.</summary>
public enum MouseButtonState
{
    /// <summary>The button is up.</summary>
    Released = 0,

    /// <summary>The button is down.</summary>
    Pressed = 1,
}

/// <summary>
/// WPF's mouse events on Avalonia elements, by their WPF names: <c>element.AddPreviewMouseLeftButtonDownHandler(H)</c>
/// for WPF's <c>PreviewMouseLeftButtonDown += H</c>.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia has pointer events for every button, and no Preview events. Each helper adds a handler for
/// the matching pointer event that runs only for WPF's button, tunnelling for the Preview events and
/// bubbling for the others. Handlers take Avalonia's pointer event arguments; the extension properties
/// below give them WPF's <c>ChangedButton</c>, <c>LeftButton</c>, <c>RightButton</c> and
/// <c>OriginalSource</c> (Avalonia's <c>Source</c>, the element the pointer is over).
/// </para>
/// <para>
/// WPF's <c>MouseDoubleClick</c> is the second press of a double click (<c>ClickCount</c> 2) with the
/// left or right button. WPF's <c>Control</c> raises it from class handlers that see handled presses
/// too, so it fires even when an element inside handled the press; so does this one. Each Remove
/// method removes a handler its Add method added.
/// </para>
/// </remarks>
public static class MouseEvents
{
    // The wrappers added for each handler, so the Remove methods can find them.
    private static readonly ConditionalWeakTable<Interactive, Dictionary<(Delegate, RoutedEvent, RoutingStrategies), Delegate>> Wrappers = new();

    private static void Add<TArgs>(Interactive element, RoutedEvent<TArgs> routedEvent, RoutingStrategies routing, EventHandler<TArgs> handler, Func<TArgs, bool> filter, bool handledEventsToo = false)
        where TArgs : RoutedEventArgs
    {
        EventHandler<TArgs> wrapper = (sender, e) =>
        {
            if (filter(e))
            {
                handler(sender, e);
            }
        };
        Wrappers.GetOrCreateValue(element)[(handler, routedEvent, routing)] = wrapper;
        element.AddHandler(routedEvent, wrapper, routing, handledEventsToo);
    }

    private static void Remove<TArgs>(Interactive element, RoutedEvent<TArgs> routedEvent, RoutingStrategies routing, EventHandler<TArgs> handler)
        where TArgs : RoutedEventArgs
    {
        if (Wrappers.TryGetValue(element, out var wrappers) && wrappers.Remove((handler, routedEvent, routing), out Delegate? wrapper))
        {
            element.RemoveHandler(routedEvent, (EventHandler<TArgs>)wrapper);
        }
    }

    private static bool Pressed(PointerPressedEventArgs e, MouseButton button) => e.GetCurrentPoint(null).Properties.PointerUpdateKind.GetMouseButton() == button;

    private static bool Released(PointerReleasedEventArgs e, MouseButton button) => e.InitialPressMouseButton == button;

    private static bool DoubleClicked(PointerPressedEventArgs e) => e.ClickCount == 2 && (Pressed(e, MouseButton.Left) || Pressed(e, MouseButton.Right));

    private const RoutingStrategies Preview = RoutingStrategies.Tunnel;
    private const RoutingStrategies Bubble = RoutingStrategies.Bubble;

    extension(Interactive element)
    {
        /// <summary>WPF's <c>MouseLeftButtonDown</c>.</summary>
        public void AddMouseLeftButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Bubble, handler, e => Pressed(e, MouseButton.Left));

        /// <summary>Removes a <c>MouseLeftButtonDown</c> handler.</summary>
        public void RemoveMouseLeftButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Remove(element, InputElement.PointerPressedEvent, Bubble, handler);

        /// <summary>WPF's <c>PreviewMouseLeftButtonDown</c>.</summary>
        public void AddPreviewMouseLeftButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Preview, handler, e => Pressed(e, MouseButton.Left));

        /// <summary>Removes a <c>PreviewMouseLeftButtonDown</c> handler.</summary>
        public void RemovePreviewMouseLeftButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Remove(element, InputElement.PointerPressedEvent, Preview, handler);

        /// <summary>WPF's <c>MouseRightButtonDown</c>.</summary>
        public void AddMouseRightButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Bubble, handler, e => Pressed(e, MouseButton.Right));

        /// <summary>Removes a <c>MouseRightButtonDown</c> handler.</summary>
        public void RemoveMouseRightButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Remove(element, InputElement.PointerPressedEvent, Bubble, handler);

        /// <summary>WPF's <c>PreviewMouseRightButtonDown</c>.</summary>
        public void AddPreviewMouseRightButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Preview, handler, e => Pressed(e, MouseButton.Right));

        /// <summary>Removes a <c>PreviewMouseRightButtonDown</c> handler.</summary>
        public void RemovePreviewMouseRightButtonDownHandler(EventHandler<PointerPressedEventArgs> handler) => Remove(element, InputElement.PointerPressedEvent, Preview, handler);

        /// <summary>WPF's <c>MouseDown</c> (any button).</summary>
        public void AddMouseDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Bubble, handler, _ => true);

        /// <summary>WPF's <c>PreviewMouseDown</c> (any button).</summary>
        public void AddPreviewMouseDownHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Preview, handler, _ => true);

        /// <summary>WPF's <c>MouseLeftButtonUp</c>.</summary>
        public void AddMouseLeftButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Bubble, handler, e => Released(e, MouseButton.Left));

        /// <summary>Removes a <c>MouseLeftButtonUp</c> handler.</summary>
        public void RemoveMouseLeftButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Remove(element, InputElement.PointerReleasedEvent, Bubble, handler);

        /// <summary>WPF's <c>PreviewMouseLeftButtonUp</c>.</summary>
        public void AddPreviewMouseLeftButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Preview, handler, e => Released(e, MouseButton.Left));

        /// <summary>Removes a <c>PreviewMouseLeftButtonUp</c> handler.</summary>
        public void RemovePreviewMouseLeftButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Remove(element, InputElement.PointerReleasedEvent, Preview, handler);

        /// <summary>WPF's <c>MouseRightButtonUp</c>.</summary>
        public void AddMouseRightButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Bubble, handler, e => Released(e, MouseButton.Right));

        /// <summary>Removes a <c>MouseRightButtonUp</c> handler.</summary>
        public void RemoveMouseRightButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Remove(element, InputElement.PointerReleasedEvent, Bubble, handler);

        /// <summary>WPF's <c>PreviewMouseRightButtonUp</c>.</summary>
        public void AddPreviewMouseRightButtonUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Preview, handler, e => Released(e, MouseButton.Right));

        /// <summary>WPF's <c>MouseUp</c> (any button).</summary>
        public void AddMouseUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Bubble, handler, _ => true);

        /// <summary>WPF's <c>PreviewMouseUp</c> (any button).</summary>
        public void AddPreviewMouseUpHandler(EventHandler<PointerReleasedEventArgs> handler) => Add(element, InputElement.PointerReleasedEvent, Preview, handler, _ => true);

        /// <summary>WPF's <c>MouseDoubleClick</c>: the second press of a double click.</summary>
        public void AddMouseDoubleClickHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Bubble, handler, DoubleClicked, handledEventsToo: true);

        /// <summary>WPF's <c>PreviewMouseDoubleClick</c>: the second press of a double click.</summary>
        public void AddPreviewMouseDoubleClickHandler(EventHandler<PointerPressedEventArgs> handler) => Add(element, InputElement.PointerPressedEvent, Preview, handler, DoubleClicked, handledEventsToo: true);

        /// <summary>WPF's <c>MouseMove</c>.</summary>
        public void AddMouseMoveHandler(EventHandler<PointerEventArgs> handler) => Add(element, InputElement.PointerMovedEvent, Bubble, handler, _ => true);

        /// <summary>Removes a <c>MouseMove</c> handler.</summary>
        public void RemoveMouseMoveHandler(EventHandler<PointerEventArgs> handler) => Remove(element, InputElement.PointerMovedEvent, Bubble, handler);

        /// <summary>WPF's <c>PreviewMouseMove</c>.</summary>
        public void AddPreviewMouseMoveHandler(EventHandler<PointerEventArgs> handler) => Add(element, InputElement.PointerMovedEvent, Preview, handler, _ => true);

        /// <summary>Removes a <c>PreviewMouseMove</c> handler.</summary>
        public void RemovePreviewMouseMoveHandler(EventHandler<PointerEventArgs> handler) => Remove(element, InputElement.PointerMovedEvent, Preview, handler);

        /// <summary>WPF's <c>MouseWheel</c>. Avalonia's delta is in notches; WPF's is 120 per notch.</summary>
        public void AddMouseWheelHandler(EventHandler<PointerWheelEventArgs> handler) => Add(element, InputElement.PointerWheelChangedEvent, Bubble, handler, _ => true);

        /// <summary>WPF's <c>PreviewMouseWheel</c>. Avalonia's delta is in notches; WPF's is 120 per notch.</summary>
        public void AddPreviewMouseWheelHandler(EventHandler<PointerWheelEventArgs> handler) => Add(element, InputElement.PointerWheelChangedEvent, Preview, handler, _ => true);
    }

    extension(PointerEventArgs e)
    {
        /// <summary>Whether the left button is down, as WPF's <c>MouseEventArgs.LeftButton</c>.</summary>
        public MouseButtonState LeftButton => e.GetCurrentPoint(null).Properties.IsLeftButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;

        /// <summary>Whether the right button is down, as WPF's <c>MouseEventArgs.RightButton</c>.</summary>
        public MouseButtonState RightButton => e.GetCurrentPoint(null).Properties.IsRightButtonPressed ? MouseButtonState.Pressed : MouseButtonState.Released;
    }

    extension(PointerPressedEventArgs e)
    {
        /// <summary>The button pressed, as WPF's <c>MouseButtonEventArgs.ChangedButton</c>.</summary>
        public MouseButton ChangedButton => e.GetCurrentPoint(null).Properties.PointerUpdateKind.GetMouseButton();
    }

    extension(PointerReleasedEventArgs e)
    {
        /// <summary>The button released, as WPF's <c>MouseButtonEventArgs.ChangedButton</c>.</summary>
        public MouseButton ChangedButton => e.InitialPressMouseButton;
    }

    extension(RoutedEventArgs e)
    {
        /// <summary>
        /// The element that raised the event, as WPF's <c>OriginalSource</c>: Avalonia's <c>Source</c>,
        /// which routing doesn't change.
        /// </summary>
        public object? OriginalSource => e.Source;
    }
}
