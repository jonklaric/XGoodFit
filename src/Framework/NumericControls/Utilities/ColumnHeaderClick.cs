using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace NumericControls
{
    /// <summary>
    /// WPF's <c>EventSetter</c> for a column header's <c>Click</c> in a header style: raises a handler when a
    /// header with that style is clicked.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF's DataGridColumnHeader is a button, and upstream's WrappedColumnHeaderStyle
    /// handles its Click with an EventSetter, so that clicking a header with the style selects its column.
    /// A ControlTheme can't hold event setters, and ProDataGrid's header isn't a button. So the grid raises
    /// the handler as WPF's button raises Click: when the left button is pressed and then released over the
    /// same header, and the header's theme is the style or one based on it (WPF's BasedOn styles keep their
    /// base's event setters).
    /// </remarks>
    internal static class ColumnHeaderClick
    {
        /// <summary>
        /// Raises <paramref name="handler"/>, with the header as its sender, when a column header of
        /// <paramref name="grid"/> whose theme is <paramref name="style"/> (or is based on it) is clicked.
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <param name="style">The header style whose headers raise the handler.</param>
        /// <param name="handler">The Click handler.</param>
        public static void Add(DataGrid grid, ControlTheme style, EventHandler<RoutedEventArgs> handler)
        {
            DataGridColumnHeader pressed = null;
            // The header captures the pointer and handles its events: the grid sees them as handled.
            grid.AddHandler(InputElement.PointerPressedEvent, (sender, e) =>
            {
                pressed = null;
                if (e.GetCurrentPoint(grid).Properties.IsLeftButtonPressed
                    && (e.Source as Visual)?.FindAncestorOfType<DataGridColumnHeader>(includeSelf: true) is { } header
                    && UsesStyle(header, style))
                {
                    pressed = header;
                }
            }, RoutingStrategies.Bubble, handledEventsToo: true);
            grid.AddHandler(InputElement.PointerReleasedEvent, (sender, e) =>
            {
                DataGridColumnHeader header = pressed;
                pressed = null;
                if (header != null && e.InitialPressMouseButton == MouseButton.Left
                    && new Rect(header.Bounds.Size).Contains(e.GetPosition(header)))
                {
                    handler(header, new RoutedEventArgs());
                }
            }, RoutingStrategies.Bubble, handledEventsToo: true);
        }

        // Whether the header's theme is the style or is based on it.
        private static bool UsesStyle(DataGridColumnHeader header, ControlTheme style)
        {
            for (ControlTheme theme = header.Theme; theme != null; theme = theme.BasedOn)
            {
                if (ReferenceEquals(theme, style))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
