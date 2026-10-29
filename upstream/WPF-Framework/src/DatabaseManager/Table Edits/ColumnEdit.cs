using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit operation applied to an entire column in a table,
    /// including data additions, deletions, and tracking.
    /// </summary>
    public class ColumnEdit : TableEdit
    {

        private int _columnIndex;
        private readonly List<object> _columnData;
        private readonly List<object> _removedValues = new List<object>();

        /// <summary>
        /// Gets the index of the column being edited.
        /// </summary>
        public int ColumnIndex
        {
            get
            {
                return _columnIndex;
            }
        }

        /// <summary>
        /// Gets the list of values currently in the edited column. 
        /// </summary>
        public List<object> ColumnData
        {
            get
            {
                return _columnData;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnEdit"/> class.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column being edited.</param>
        /// <param name="columnData">The initial values of the column.</param>
        public ColumnEdit(int indexOfColumn, object[] columnData)
        {
            _columnIndex = indexOfColumn;
            _columnData = columnData.ToList();
        }

        /// <summary>
        /// Gets a list of <see cref="CellEdit"/> instances for all cells in this column if 
        /// the column matches the given index.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>A list of edited cells in the column, or an empty list if it doesn't match.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            if (indexOfColumn != _columnIndex)
                return new List<CellEdit>();
            var result = new List<CellEdit>();
            for (int i = 0; i < _columnData.Count; i++)
                result.Add(new CellEdit(i, _columnIndex, _columnData[i]));
            return result;
        }

        /// <summary>
        /// Gets the <see cref="CellEdit"/> for a specific row if this column is valid.
        /// </summary>
        /// <param name="indexOfRow">The row index to retrieve the cell edit from.</param>
        /// <returns>A list containing one <see cref="CellEdit"/>, or empty if column is negative.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            if (indexOfRow < 0 || indexOfRow >= _columnData.Count) return new List<CellEdit>();
            if (_columnIndex >= 0)
            {
                return new List<CellEdit>(new[] { new CellEdit(indexOfRow, _columnIndex, _columnData[indexOfRow]) });
            }
            else
            {
                return new List<CellEdit>();
            }
        }

        /// <summary>
        /// Determines whether the column edit contains any data for the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>Always returns <c>true</c> since all rows are assumed to be affected.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return true;
        }

        /// <summary>
        /// Adjusts the internal column index when a new column is inserted.
        /// </summary>
        /// <typeparam name="T">The type of the column data.</typeparam>
        /// <param name="indexOfColumn">The index at which the column was added.</param>
        /// <param name="dataOfColumn">Optional column data.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] dataOfColumn = null) 
        {
            if (_columnIndex >= indexOfColumn)
                _columnIndex += 1;

            if (_columnIndex < 0)
            {
                if (_columnIndex * -1 > indexOfColumn)
                {
                    _columnIndex += 1;
                }
                else if (_columnIndex * -1 == indexOfColumn)
                {
                    _columnIndex *= -1;
                }
            }
        }

        /// <summary>
        /// Adjusts the internal column index and state when a column is deleted.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column that was deleted.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            if (_columnIndex < 0)
            {
                if (_columnIndex * -1 >= indexOfColumn)
                    _columnIndex -= 1;
            }

            if (_columnIndex == indexOfColumn)
                _columnIndex *= -1;
            if (_columnIndex > indexOfColumn)
                _columnIndex -= 1;
        }

        /// <summary>
        /// Handles a row insertion by adding the appropriate value into the column data.
        /// </summary>
        /// <param name="indexOfRow">The row index at which the new row was added.</param>
        /// <param name="rowData">Optional row data. If null, a previously removed value is reinserted</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            if (rowData == null)
            {
                if (_removedValues.Count == 0) throw new InvalidOperationException("No removed values available to restore.");
                _columnData.Insert(indexOfRow, _removedValues.Last());
                _removedValues.RemoveAt(_removedValues.Count - 1);
            }
            else
            {
                _columnData.Insert(indexOfRow, rowData[_columnIndex]);
            }
        }

        /// <summary>
        /// Handles a row deletion by removing the value at the specified index and storing it for potential restoration.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to remove.</param>
        public override void RowDeleted(int indexOfRow)
        {
            _removedValues.Add(_columnData[indexOfRow]);
            _columnData.RemoveAt(indexOfRow);
        }

        /// <summary>
        /// Determines whether this column edit includes the specified cell and provides its value.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell to check.</param>
        /// <param name="indexOfRow">The row index of the cell to check.</param>
        /// <param name="returnValue">The value in the specified cell if found.</param>
        /// <returns><c>true</c> if the cell exists in this column; otherwise, <c>false</c>.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            if (_columnIndex != indexOfColumn)
                return false;
            returnValue = _columnData[indexOfRow];
            return true;
        }

        /// <summary>
        /// Determines whether this edit affects the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns><c>true</c> if the column is the one being edited; otherwise, <c>false</c>.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            return _columnIndex == indexOfColumn;
        }
    }
}