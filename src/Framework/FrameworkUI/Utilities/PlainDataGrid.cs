using Avalonia.Input;
using Avalonia.Interactivity;
using GenericControls;

namespace FrameworkUI
{
    /// <summary>
    /// Makes a <see cref="CopyPasteDataGrid"/> stand in for a plain WPF DataGrid.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. The porting guide maps every WPF DataGrid to GenericControls'
    /// CopyPasteDataGrid, which adds WPF's grid members to ProDataGrid's grid, but also upstream
    /// CopyPasteDataGrid's context menus (add, insert and delete rows; select all, copy and paste; sort).
    /// The shell's message window and dialogs use plain WPF DataGrids, which have no context menu, so
    /// their grids suppress those menus. Their XAML sets back the WPF defaults CopyPasteDataGrid changes.
    /// </remarks>
    internal static class PlainDataGrid
    {
        /// <summary>Keeps <paramref name="grid"/>'s context menus from opening.</summary>
        /// <param name="grid">The grid.</param>
        internal static void SuppressContextMenus(CopyPasteDataGrid grid) =>
            grid.AddHandler(InputElement.ContextRequestedEvent, (sender, e) => e.Handled = true, RoutingStrategies.Tunnel);
    }
}
