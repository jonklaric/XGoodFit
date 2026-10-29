using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit operation that deletes a single row from a table.
    /// This class is intended to record structural changes rather than specific cell modifications.
    /// </summary>
    public class DeleteRowEdit : TableEdit
    {
        private readonly int _rowIndex;

        /// <summary>
        /// Gets the index of the row that is marked for deletion.
        /// </summary>
        public int RowIndex
        {
            get
            {
                return _rowIndex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteRowEdit"/> class.
        /// </summary>
        /// <param name="viewRowIndex">The index of the row to be deleted.</param>
        public DeleteRowEdit(int viewRowIndex)
        {
            _rowIndex = viewRowIndex;
        }

        /// <summary>
        /// Handles the addition of a column.
        /// This operation is not applicable for a row delete edit.
        /// </summary>
        /// <typeparam name="T">The type of data in the added column.</typeparam>
        /// <param name="indexOfColumn">The index of the added column.</param>
        /// <param name="columnData">Optional column data.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the deletion of a column.
        /// This operation is not applicable for a row delete edit.
        /// </summary>
        /// <param name="indexOfColumn">The index of the deleted column.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the addition of a row.
        /// This operation is not applicable for a row delete edit.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional data for the added row.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Handles the deletion of a row.
        /// This operation is not applicable for a row delete edit.
        /// </summary>
        /// <param name="indexOfRow">The index of the deleted row.</param>
        public override void RowDeleted(int indexOfRow)
        {
            // No index adjustment needed for this edit type.
        }

        /// <summary>
        /// Indicates whether this edit affects a specific cell.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">The cell value, if found (unused).</param>
        /// <returns>Always returns <c>false</c> since row deletion does not track specific cell values.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            return false;
        }

        /// <summary>
        /// Indicates whether this edit affects a specific column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>Always returns <c>false</c> since row deletion does not affect columns.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            return false;
        }

        /// <summary>
        /// Indicates whether this edit affects a specific row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>Always returns <c>false</c> since this edit represents a structural deletion, not specific row data.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return false;
        }

        /// <summary>
        /// Returns the list of cell edits in the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>An empty list since row deletion does not modify specific cells.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            return new List<CellEdit>();
        }

        /// <summary>
        /// Returns the list of cell edits in the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>An empty list since row deletion does not modify specific cells.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            return new List<CellEdit>();
        }
    }
}