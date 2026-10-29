using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents a composite table edit that deletes multiple columns.
    /// Internally delegates all behavior to its constituent <see cref="DeleteColumnEdit"/> instances.
    /// </summary>
    public class DeleteColumnsEdit : TableEdit
    {
       
        private readonly DeleteColumnEdit[] _columnsDeleted;

        /// <summary>
        /// Gets the collection of column delete edits contained in this composite edit.
        /// </summary>
        public DeleteColumnEdit[] ColumnsDeleted
        {
            get
            {
                return _columnsDeleted;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteColumnsEdit"/> class.
        /// </summary>
        /// <param name="deleteColumnEdits">An array of column deletions to apply as a group.</param>
        public DeleteColumnsEdit(DeleteColumnEdit[] deleteColumnEdits)
        {
            _columnsDeleted = deleteColumnEdits;
        }

        /// <summary>
        /// Propagates column addition to all contained <see cref="DeleteColumnEdit"/> instances.
        /// </summary>
        /// <typeparam name="T">The type of the data in the added column.</typeparam>
        /// <param name="indexOfColumn">The index where the column was added.</param>
        /// <param name="columnData">Optional column data.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                columnDeleteEdit.ColumnAdded(indexOfColumn, columnData);
        }

        /// <summary>
        /// Propagates column deletion to all contained <see cref="DeleteColumnEdit"/> instances.
        /// </summary>
        /// <param name="indexOfColumn">The index of the deleted column.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                columnDeleteEdit.ColumnDeleted(indexOfColumn);
        }

        /// <summary>
        /// Checks whether any contained edit affects the specified cell.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell. </param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">Output parameter for the cell's value if found.</param>
        /// <returns><c>true</c> if any contained edit affects the specified cell; otherwise, <c>false</c>.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
            {
                if (columnDeleteEdit.ContainsCell(indexOfColumn, indexOfRow, ref returnValue))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether any contained edit affects the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns><c>true</c> if any contained edit affects the specified column; otherwise, <c>false</c>.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
            {
                if (columnDeleteEdit.ContainsColumn(indexOfColumn))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether any contained edit affects the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns><c>true</c> if any contained edit affects the specified row; otherwise, <c>false</c>.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
            {
                if (columnDeleteEdit.ContainsRow(indexOfRow))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the list of cell edits in the specified column across all contained edits.
        /// </summary>
        /// <param name="indexOfColumn">The column index to retrieve edits from.</param>
        /// <returns>A list of cell edits from all contained edits that affect the specified column.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            var result = new List<CellEdit>();
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                result.AddRange(columnDeleteEdit.GetEditedCellsInColumn(indexOfColumn));
            return result;
        }

        /// <summary>
        /// Gets the list of cell edits in the specified row across all contained edits.
        /// </summary>
        /// <param name="indexOfRow">The row index to retrieve edits from.</param>
        /// <returns>A list of cell edits from all contained edits that affect the specified row.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            var result = new List<CellEdit>();
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                result.AddRange(columnDeleteEdit.GetEditedCellsInRow(indexOfRow));
            return result;
        }

        /// <summary>
        /// Propagates a row addition event to all contained <see cref="DeleteColumnEdit"/> instances.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional row data.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                columnDeleteEdit.RowAdded(indexOfRow, rowData);
        }

        /// <summary>
        /// Propagates a row deletion event to all contained <see cref="DeleteColumnEdit"/> instances.
        /// </summary>
        /// <param name="indexOfRow">The index of the deleted row.</param>
        public override void RowDeleted(int indexOfRow)
        {
            foreach (DeleteColumnEdit columnDeleteEdit in _columnsDeleted)
                columnDeleteEdit.RowDeleted(indexOfRow);
        }
    }
}