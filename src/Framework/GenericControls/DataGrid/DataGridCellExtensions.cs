using Avalonia.Controls;

namespace GenericControls
{
    /// <summary>
    /// WPF's <c>DataGridCell.IsSelected</c> for ProDataGrid's cells.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. Upstream reads whether a cell is selected; ProDataGrid shows it with the
    /// cell's <c>:selected</c> pseudo-class (the cell's own selection, or its row's when whole rows
    /// are selected, as WPF's IsSelected).
    /// </remarks>
    public static class DataGridCellExtensions
    {
        extension(DataGridCell cell)
        {
            /// <summary>Whether the cell is selected: WPF's <c>DataGridCell.IsSelected</c>.</summary>
            public bool IsSelected => cell.Classes.Contains(":selected");
        }
    }
}
