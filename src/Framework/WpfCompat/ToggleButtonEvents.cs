using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Checked</c>, <c>Unchecked</c> and <c>Indeterminate</c> events on toggle buttons (check
/// boxes, radio buttons): <c>toggle.AddCheckedHandler(H)</c> for WPF's <c>Checked += H</c> or XAML's
/// <c>Checked="H"</c>.
/// </summary>
/// <remarks>
/// Avalonia 12's toggle buttons raise only <c>IsCheckedChanged</c>. Each helper adds a handler for it
/// that runs only when the button becomes checked, unchecked or indeterminate, as WPF's events did.
/// </remarks>
public static class ToggleButtonEvents
{
    extension(ToggleButton toggle)
    {
        /// <summary>WPF's <c>Checked</c>: the button became checked.</summary>
        public void AddCheckedHandler(EventHandler<RoutedEventArgs> handler) => Add(toggle, handler, true);

        /// <summary>WPF's <c>Unchecked</c>: the button became unchecked.</summary>
        public void AddUncheckedHandler(EventHandler<RoutedEventArgs> handler) => Add(toggle, handler, false);

        /// <summary>WPF's <c>Indeterminate</c>: the button became neither checked nor unchecked.</summary>
        public void AddIndeterminateHandler(EventHandler<RoutedEventArgs> handler) => Add(toggle, handler, null);
    }

    private static void Add(ToggleButton toggle, EventHandler<RoutedEventArgs> handler, bool? state) =>
        toggle.IsCheckedChanged += (sender, e) =>
        {
            if (toggle.IsChecked == state)
            {
                handler(sender, e);
            }
        };
}
