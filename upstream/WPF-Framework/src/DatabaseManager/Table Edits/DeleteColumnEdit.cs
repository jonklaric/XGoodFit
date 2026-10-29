using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit operation that deletes a column from a table.
    /// This class is typically used to record structural changes without tracking individual cell edits.
    /// </summary>
    public class DeleteColumnEdit : TableEdit
    {
        private readonly int _columnIndex;

        /// <summary>
        /// Gets the index of the column to be deleted.
        /// </summary>
        public int ColumnIndex
        {
            get
            {
                return _columnIndex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteColumnEdit"/> class.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to delete.</param>
        public DeleteColumnEdit(int indexOfColumn)
        {
            _columnIndex = indexOfColumn;
        }

        /// <summary>
        /// Handles the addition of a column.
        /// This operation is not applicable for a column delete edit.
        /// </summary>
        /// <typeparam name="T">The data type of the added column (if any).</typeparam>
        /// <param name="indexOfColumn">The index at which the column was added.</param>
        /// <param name="columnData">The column data.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the deletion of a column.
        /// This operation is not applicable for a column delete edit.
        /// </summary>
        /// <param name="indexOfColumn">The index of the deleted column.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the addition of a row.
        /// This operation is not applicable for a column delete edit.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional data for the added row.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the deletion of a row.
        /// This operation is not applicable for a column delete edit.
        /// </summary>
        /// <param name="indexOfRow">The index of the deleted row.</param>
        public override void RowDeleted(int indexOfRow)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Determines whether this edit affects a specific cell.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">The value of the cell (unused).</param>
        /// <returns>Always returns <c>false</c> since column deletion does not track specific cell values.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            return false;
        }

        /// <summary>
        /// Determines whether this edit affects the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>Always returns <c>false</c> because this edit removes the column.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            return false;
        }

        /// <summary>
        /// Determines whether this edit affects the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>Always returns <c>false</c> since column deletion does not affect rows.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return false;
        }

        /// <summary>
        /// Returns the list of cell edits in the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>An empty list since column deletion does not modify specific cells.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            return new List<CellEdit>();
        }

        /// <summary>
        /// Returns the list of cell edits in the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>An empty list since column deletion does not modify specific cells.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            return new List<CellEdit>();
        }
    }
}