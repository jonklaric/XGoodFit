using System.Collections.Generic;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Represents a batch edit operation that adds multiple rows to a table.
    /// Each row is managed by an individual <see cref="AddRowEdit"/> instance.
    /// </summary>
    public class AddRowsEdit : TableEdit
    {

        private readonly AddRowEdit[] _addRowEdits;
        /// <summary>
        /// Gets the array of row edits representing the rows that were added.
        /// </summary>
        public AddRowEdit[] AddRowEdits
        {
            get
            {
                return _addRowEdits;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="AddRowsEdit"/> class with a collection of row edits.
        /// </summary>
        /// <param name="rowEditsToAdd">An array of <see cref="AddRowsEdit"/> objects representing rows to add.</param>
        public AddRowsEdit(AddRowEdit[] rowEditsToAdd)
        {
            _addRowEdits = rowEditsToAdd;
        }
        /// <summary>
        /// Gets the original row data for each added row.
        /// </summary>
        /// <returns></returns>
        public List<object[]> GetOriginalRowEdits()
        {
            var edits = new List<object[]>(_addRowEdits.Length);
            foreach (AddRowEdit rowEdit in _addRowEdits)
                edits.Add(rowEdit.OriginalRowEdit);
            // 
            return edits;
        }

        /// <summary>
        /// Propagates a column addition to all added rows.
        /// </summary>
        /// <typeparam name="T">The type of the added column.</typeparam>
        /// <param name="indexOfColumn">The index where the column was added.</param>
        /// <param name="columnData">Optional data for the new column.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                rowEditToAdd.ColumnAdded(indexOfColumn, columnData);
        }

        /// <summary>
        /// Propagates a column deletion to all added rows.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to delete.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                rowEditToAdd.ColumnDeleted(indexOfColumn);
        }

        /// <summary>
        /// Determines whether any added row contains the specified cell.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">The value of the cell if found.</param>
        /// <returns></returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
            {
                if (rowEditToAdd.ContainsCell(indexOfColumn, indexOfRow, ref returnValue))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether any added row contains the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns></returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
            {
                if (rowEditToAdd.ContainsColumn(indexOfColumn))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether any added row is located at the specified index.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns></returns>
        public override bool ContainsRow(int indexOfRow)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
            {
                if (rowEditToAdd.ContainsRow(indexOfRow))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Gets a list of all cell edits in the specified column across all added rows.
        /// </summary>
        /// <param name="indexOfColumn">The column index to retrieve edits from.</param>
        /// <returns></returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            var result = new List<CellEdit>();
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                result.AddRange(rowEditToAdd.GetEditedCellsInColumn(indexOfColumn));
            return result;
        }

        /// <summary>
        /// Gets a list of all cell edits in the specified row across all added rows.
        /// </summary>
        /// <param name="indexOfRow">The row index to retrieve edits from.</param>
        /// <returns></returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            var result = new List<CellEdit>();
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                result.AddRange(rowEditToAdd.GetEditedCellsInRow(indexOfRow));
            return result;
        }

        /// <summary>
        /// Updates the internal row index of all added rows when a new row is inserted in the table.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional data for the new row.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                rowEditToAdd.RowAdded(indexOfRow, rowData);
        }

        /// <summary>
        /// Updates the internal row index of all added rows when a row is deleted from the table.
        /// </summary>
        /// <param name="indexOfRow">The index of the deleted row.</param>
        public override void RowDeleted(int indexOfRow)
        {
            foreach (AddRowEdit rowEditToAdd in _addRowEdits)
                rowEditToAdd.RowDeleted(indexOfRow);
        }
    }
}