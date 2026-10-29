using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents the base class for all table editing operations, including modifications 
    /// at the cell, row, or column level. Subclasses must implement logic for tracking changes
    /// and reacting to structural table events.
    /// </summary>
    public abstract class TableEdit
    {
        /// <summary>
        /// Gets the list of edited cells in a specific row.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to query.</param>
        /// <returns>A list of <see cref="CellEdit"/> objects representing edits in the specified row.</returns>
        public abstract List<CellEdit> GetEditedCellsInRow(int indexOfRow);

        /// <summary>
        /// Gets the list of edited cells in a specific column.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to query.</param>
        /// <returns>A list of <see cref="CellEdit"/> objects representing edits in the specified column.</returns>
        public abstract List<CellEdit> GetEditedCellsInColumn(int indexOfColumn);

        /// <summary>
        /// Determines whether a specific cell is included in the edit and retrieves its value if applicable.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">Outputs the value of the cell if it is part of the edit; otherwise unchanged.</param>
        /// <returns><c>true</c> if the cell is part of this edit; otherwise, <c>false</c>.</returns>
        public abstract bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue);

        /// <summary>
        /// Determines whether the specified column is affected by this edit.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to check.</param>
        /// <returns><c>true</c> if this edit affects the specified column; otherwise, <c>false</c>.</returns>
        public abstract bool ContainsColumn(int indexOfColumn);

        /// <summary>
        /// Determines whether the specified row is affected by this edit.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to check.</param>
        /// <returns><c>true</c> if this edit affects the specified row; otherwise, <c>false</c>.</returns>
        public abstract bool ContainsRow(int indexOfRow);

        /// <summary>
        /// Updates the edit state to reflect the removal of a column at the specified index.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column that was deleted.</param>
        public abstract void ColumnDeleted(int indexOfColumn);

        /// <summary>
        /// Updates the edit state to reflect the insertion of a column at the specified index.
        /// </summary>
        /// <typeparam name="T">The type of the column data, if provided.</typeparam>
        /// <param name="indexOfColumn">The index where the column was added.</param>
        /// <param name="columnData">Optional data for the added column.</param>
        public abstract void ColumnAdded<T>(int indexOfColumn, T[] columnData = null);

        /// <summary>
        /// Updates the edit state to reflect the deletion of a row at the specified index.
        /// </summary>
        /// <param name="indexOfRow">The index of the row that was deleted.</param>
        public abstract void RowDeleted(int indexOfRow);

        /// <summary>
        /// Updates the edit state to reflect the addition of a row at the specified index.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional data for the newly added row.</param>
        public abstract void RowAdded(int indexOfRow, object[] rowData = null);
    }
}