using Avalonia.Controls;
using Avalonia.Input;

namespace WpfCompat.Input;

/// <summary>Where to move keyboard focus: WPF's <c>System.Windows.Input.FocusNavigationDirection</c>.</summary>
public enum FocusNavigationDirection
{
    /// <summary>The next element in tab order.</summary>
    Next = 0,

    /// <summary>The previous element in tab order.</summary>
    Previous = 1,

    /// <summary>The first element in tab order.</summary>
    First = 2,

    /// <summary>The last element in tab order.</summary>
    Last = 3,

    /// <summary>The element to the left.</summary>
    Left = 4,

    /// <summary>The element to the right.</summary>
    Right = 5,

    /// <summary>The element above.</summary>
    Up = 6,

    /// <summary>The element below.</summary>
    Down = 7,
}

/// <summary>A request to move keyboard focus: WPF's <c>System.Windows.Input.TraversalRequest</c>.</summary>
public class TraversalRequest
{
    /// <summary>Creates a request to move focus in <paramref name="focusNavigationDirection"/>.</summary>
    public TraversalRequest(FocusNavigationDirection focusNavigationDirection)
    {
        FocusNavigationDirection = focusNavigationDirection;
    }

    /// <summary>Where to move focus.</summary>
    public FocusNavigationDirection FocusNavigationDirection { get; }

    /// <summary>Kept for source compatibility: whether focus wraps around.</summary>
    public bool Wrapped { get; set; }
}

/// <summary>WPF's <c>UIElement.MoveFocus</c>, over Avalonia's keyboard navigation.</summary>
public static class FocusNavigation
{
    /// <summary>
    /// Moves keyboard focus from <paramref name="element"/> to the next element in the request's
    /// direction, as Tab and the arrow keys do.
    /// </summary>
    /// <returns>Whether focus moved.</returns>
    public static bool MoveFocus(this InputElement element, TraversalRequest request)
    {
        if (TopLevel.GetTopLevel(element)?.FocusManager is not { } focusManager)
        {
            return false;
        }
        IInputElement? edge = request.FocusNavigationDirection switch
        {
            FocusNavigationDirection.First => focusManager.FindFirstFocusableElement(),
            FocusNavigationDirection.Last => focusManager.FindLastFocusableElement(),
            _ => null,
        };
        if (request.FocusNavigationDirection is FocusNavigationDirection.First or FocusNavigationDirection.Last)
        {
            return edge?.Focus(NavigationMethod.Tab) ?? false;
        }
        NavigationDirection direction = request.FocusNavigationDirection switch
        {
            FocusNavigationDirection.Next => NavigationDirection.Next,
            FocusNavigationDirection.Previous => NavigationDirection.Previous,
            FocusNavigationDirection.Left => NavigationDirection.Left,
            FocusNavigationDirection.Right => NavigationDirection.Right,
            FocusNavigationDirection.Up => NavigationDirection.Up,
            _ => NavigationDirection.Down,
        };
        return focusManager.TryMoveFocus(direction, new FindNextElementOptions { FocusedElement = element });
    }
}
