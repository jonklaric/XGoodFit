using Avalonia.Controls;
using GenericControls;

namespace NumericControls
{
    /// <summary>
    /// Selects a grid's cells as clearing WPF's <c>DataGrid.SelectedCells</c> and adding cells to it does.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF selects a cell as it's added to SelectedCells. ProDataGrid selects the cells
    /// its SelectedCells is set to, and keeps them selected when it's set back to null; adding to or clearing
    /// the collection it gives doesn't change the selection. WPF keeps the selected cells as row and column
    /// indexes, so a cell whose row item has been replaced (a table rebuilt after a math function) selects
    /// the row now at its index: a cell is found by its item, or else by its row index.
    /// </remarks>
    internal static class CellSelection
    {
        /// <summary>Selects <paramref name="cells"/> of <paramref name="grid"/>, and only them.</summary>
        /// <param name="grid">The grid.</param>
        /// <param name="cells">The cells to select.</param>
        public static void Select(CopyPasteDataGrid grid, IEnumerable<DataGridCellInfo> cells)
        {
            var selection = new List<DataGridCellInfo>();
            foreach (DataGridCellInfo cell in cells)
            {
                int row = grid.Items.IndexOf(cell.Item);
                if (row < 0)
                {
                    row = cell.RowIndex;
                }
                if (row < 0 || row >= grid.Items.Count || cell.Column == null || !grid.Columns.Contains(cell.Column))
                {
                    continue;
                }
                selection.Add(new DataGridCellInfo(grid.Items[row], cell.Column, row, grid.Columns.IndexOf(cell.Column)));
            }
            grid.SelectedCells = selection;
            grid.SelectedCells = null;
        }

        /// <summary>A cell of <paramref name="grid"/>: WPF's <c>new DataGridCellInfo(item, column)</c>.</summary>
        /// <param name="grid">The grid.</param>
        /// <param name="item">The cell's row item.</param>
        /// <param name="column">The cell's column.</param>
        /// <returns>The cell, with the row and column indexes ProDataGrid's cell infos also take.</returns>
        public static DataGridCellInfo Cell(CopyPasteDataGrid grid, object item, DataGridColumn column) =>
            new DataGridCellInfo(item, column, grid.Items.IndexOf(item), grid.Columns.IndexOf(column));
    }
}
