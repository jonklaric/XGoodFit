using System.Collections.Generic;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit operation that adds a new row to a table.
    /// Tracks the row's original data, current data, and position in the table.
    /// </summary>
    public class AddRowEdit : TableEdit
    {
        private readonly object[] _originalRowEdit;
        private readonly List<object> _rowData;
        private int _rowIndex;

        /// <summary>
        /// Gets the index of the row in the table.
        /// </summary>
        public int RowIndex
        {
            get
            {
                return _rowIndex;
            }
        }

        /// <summary>
        /// Gets the current data of the row after edits.
        /// </summary>
        public List<object> RowData
        {
            get
            {
                return _rowData;
            }
        }

        /// <summary>
        /// Gets the original data of the row before any edits.
        /// </summary>
        public object[] OriginalRowEdit
        {
            get
            {
                return _originalRowEdit;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddRowEdit"/> class.
        /// </summary>
        /// <param name="newRowData">The values in the new row.</param>
        /// <param name="indexOfRow">The index at which the row is inserted.</param>
        public AddRowEdit(object[] newRowData, int indexOfRow)
        {
            _originalRowEdit = newRowData;
            _rowData = newRowData.ToList();
            _rowIndex = indexOfRow;
        }

        /// <summary>
        /// Inserts a default value into the row when a new column is added to the table.
        /// </summary>
        /// <typeparam name="T">The type of the added column.</typeparam>
        /// <param name="indexOfColumn">The index of the added column.</param>
        /// <param name="columnData">Optional data for the new column.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            _rowData.Insert(indexOfColumn, null);
        }

        /// <summary>
        /// Removes a value form the row when a column is deleted from the table.
        /// </summary>
        /// <param name="indexOfColumn">The index of the deleted column.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            _rowData.RemoveAt(indexOfColumn);
        }

        /// <summary>
        /// Updates the row index if another row is added before this one.
        /// </summary>
        /// <param name="indexOfRow">The index where a new row was added.</param>
        /// <param name="dataOfRow">Optional data for the new row.</param>
        public override void RowAdded(int indexOfRow, object[] dataOfRow = null)
        {
            if (_rowIndex >= indexOfRow)
                _rowIndex += 1;
            if (_rowIndex < 0)
            {
                if (_rowIndex * -1 > indexOfRow)
                {
                    _rowIndex += 1;
                }
                else if (_rowIndex * -1 == indexOfRow)
                {
                    _rowIndex *= -1;
                }
            }
        }

        /// <summary>
        /// Updates the row index if another row is deleted or if this row is the one deleted.
        /// </summary>
        /// <param name="indexOfRow">The index of the deleted row.</param>
        public override void RowDeleted(int indexOfRow)
        {
            if (_rowIndex < 0)
            {
                if (_rowIndex * -1 >= indexOfRow)
                    _rowIndex -= 1;
            }
            if (_rowIndex == indexOfRow)
                _rowIndex *= -1;
            if (_rowIndex > indexOfRow)
                _rowIndex -= 1;
        }

        /// <summary>
        /// Determines whether the specified cell exists in the added row.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">The value of the cell if found.</param>
        /// <returns></returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            if (_rowIndex != indexOfRow)
                return false;
            if (indexOfColumn < 0)
                return false;
            if (indexOfColumn >= _rowData.Count)
                return false;
            returnValue = _rowData[indexOfColumn];
            return true;
        }

        /// <summary>
        /// Determines whether the specified column exists in the row.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns></returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            if (indexOfColumn < 0)
                return false;
            if (indexOfColumn >= _rowData.Count)
                return false;
            return true;
        }

        /// <summary>
        /// Determines whether the row exists at the specified index.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to check.</param>
        /// <returns></returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return _rowIndex == indexOfRow;
        }

        /// <summary>
        /// Gets a list of edited cells in the specified column if it belongs to this row.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns></returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            if (_rowIndex < 0)
                return new List<CellEdit>();
            var result = new List<CellEdit>();
            if (indexOfColumn < 0)
                return result;
            if (indexOfColumn >= _rowData.Count)
                return result;
            result.Add(new CellEdit(_rowIndex, indexOfColumn, _rowData[indexOfColumn]));
            return result;
        }

        /// <summary>
        /// Gets a list of all edited cells in the row if it matches the specified index.
        /// </summary>
        /// <param name="indexOfRow">The row of the index to check.</param>
        /// <returns></returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            var result = new List<CellEdit>();
            if (_rowIndex != indexOfRow)
                return result;
            for (int i = 0; i < _rowData.Count; i++)
                result.Add(new CellEdit(_rowIndex, i, _rowData[i]));
            return result;
        }
    }
}