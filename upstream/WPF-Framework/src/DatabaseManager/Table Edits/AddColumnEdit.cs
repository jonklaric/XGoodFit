using System;
using System.Collections.Generic;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit operation that adds a new column to a table.
    /// </summary>
    /// <typeparam name="T">The type of data stored in the column.</typeparam>
    public class AddColumnEdit<T> : TableEdit, IColumnEdit
    {
        private readonly T[] _originalColumnEdit;
        private readonly List<T> _columnData;
        private readonly List<T> _removedValues = new List<T>();
        private int _columnIndex;
        private readonly string _columnName;

        /// <summary>
        /// Gets the index of the column in the table.
        /// </summary>
        public int ColumnIndex
        {
            get
            {
                return _columnIndex;
            }
        }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        public string ColumnName
        {
            get
            {
                return _columnName;
            }
        }

        /// <summary>
        /// Gets the data type of the column.
        /// </summary>
        public Type ColumnDataType
        {
            get
            {
                return typeof(T);
            }
        }

        /// <summary>
        /// Gets the current column data after edits.
        /// </summary>
        public List<T> ColumnData
        {
            get
            {
                return _columnData;
            }
        }

        /// <summary>
        /// Gets the original data of the column before edits.
        /// </summary>
        public T[] OriginalColumnEdit
        {
            get
            {
                return _originalColumnEdit;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this edit adds a column (always true).
        /// </summary>
        public bool IsColumnAdd
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddColumnEdit{T}"/> class.
        /// </summary>
        /// <param name="data">The initial values of the column.</param>
        /// <param name="indexOfColumn">The index at which the column is added.</param>
        /// <param name="nameOfColumn">The name of the column.</param>
        public AddColumnEdit(T[] data, int indexOfColumn, string nameOfColumn)
        {
            _columnIndex = indexOfColumn;
            _originalColumnEdit = data;
            _columnData = data.ToList();
            _columnName = nameOfColumn;
        }

        /// <summary>
        /// Updates the internal column index when a new column is added elsewhere in the table.
        /// </summary>
        /// <typeparam name="U">The type of the column being added.</typeparam>
        /// <param name="indexOfColumn">The index at which the column is added.</param>
        /// <param name="dataOfColumn">Optional data for the column being added.</param>
        public override void ColumnAdded<U>(int indexOfColumn, U[] dataOfColumn = null)
        {
            if (_columnIndex >= indexOfColumn)
            {
                _columnIndex += 1;
            }
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
        /// Updates the internal column index when another column is deleted from the table.
        /// </summary>
        /// <param name="indexOfColumn">Index of the column that was deleted.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            if (_columnIndex < 0)
            {
                if (_columnIndex * -1 >= indexOfColumn)
                    _columnIndex -= 1;
            }
            if (_columnIndex == indexOfColumn)
            {
                _columnIndex *= -1;
            }
            if (_columnIndex > indexOfColumn)
            {
                _columnIndex -= 1;
            }
        }

        /// <summary>
        /// Inserts a new row into the column's data.
        /// </summary>
        /// <param name="indexOfRow">The index at which the row is inserted.</param>
        /// <param name="rowData">Optional row data; if null, uses the last removed value.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            if (rowData == null)
            {
                _columnData.Insert(indexOfRow, _removedValues.Last());
                _removedValues.RemoveAt(_removedValues.Count - 1);
            }
            else
            {
                _columnData.Insert(indexOfRow, (T)Convert.ChangeType(rowData[_columnIndex], typeof(T)));
            }
        }

        /// <summary>
        /// Removes a row from the column and stores it for possible re-insertion.
        /// </summary>
        /// <param name="indexOfRow"></param>
        public override void RowDeleted(int indexOfRow)
        {
            _removedValues.Add(_columnData[indexOfRow]);
            _columnData.RemoveAt(indexOfRow);
        }

        /// <summary>
        /// Indicates whether a particular row index exists in the column (always true for added columns).
        /// </summary>
        /// <param name="indexOfRow">The index of the row.</param>
        /// <returns><c>true</c> since added columns contain all rows.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return true;
        }

        /// <summary>
        /// Gets a list of edited cell values in the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to retrieve edits for.</param>
        /// <returns>A list of cell edits if the column matches; otherwise, an empty list.</returns>
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
        /// Gets a list of edited cells in the specified row for this column.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to retrieve.</param>
        /// <returns>A list containing the cell edit for the specified row, or empty if column is deleted.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            if (_columnIndex < 0)
                return new List<CellEdit>();
            return new List<CellEdit>(new[] { new CellEdit(indexOfRow, _columnIndex, _columnData[indexOfRow]) });
        }

        /// <summary>
        /// Checks whether a specific cell exists in the edited column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <param name="returnValue">Outputs the cell value if the cell exists.</param>
        /// <returns><c>true</c> if the cell is in this added column; otherwise, <c>false</c>.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            if (_columnIndex != indexOfColumn)
                return false;
            returnValue = _columnData[indexOfRow];
            return true;
        }

        /// <summary>
        /// Checks whether the specified column matches this added column.
        /// </summary>
        /// <param name="indexOfColumn">The index to compare.</param>
        /// <returns><c>true</c> if the index matches this added column; otherwise, <c>false</c>.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            return _columnIndex == indexOfColumn;
        }
    }
}