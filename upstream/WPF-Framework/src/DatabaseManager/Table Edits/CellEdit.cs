using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit to a single cell in a table, storing row index, column index, and the edited value.
    /// </summary>
    public class CellEdit : TableEdit
    {
        private int _rowIndex;
        private int _columnIndex;
        private readonly object _editValue;

        /// <summary>
        /// Gets the row index of the edited cell. 
        /// </summary>
        public int RowIndex
        {
            get
            {
                return _rowIndex;
            }
        }

        /// <summary>
        /// Gets the column index of the edited cell.
        /// </summary>
        public int ColumnIndex
        {
            get
            {
                return _columnIndex;
            }
        }

        /// <summary>
        /// Gets the new value assigned to the cell.
        /// </summary>
        public object Value
        {
            get
            {
                return _editValue;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CellEdit"/> class with specified row, column, and value.
        /// </summary>
        /// <param name="indexOfRow">The row index of the edited cell.</param>
        /// <param name="indexOfColumn">The column index of the edited cell.</param>
        /// <param name="editValue">The new value for the cell. </param>
        public CellEdit(int indexOfRow, int indexOfColumn, object editValue)
        {
            _rowIndex = indexOfRow;
            _columnIndex = indexOfColumn;
            _editValue = editValue;
        }

        /// <summary>
        /// Returns the edited cell if its column matches the specified index.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns>A list containing this cell edit if the column matches; otherwise, an empty list.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            if (indexOfColumn == _columnIndex)
                return new List<CellEdit>(new[] { this });
            return new List<CellEdit>();
        }

        /// <summary>
        /// Returns the edited cell if its row matches the specified index.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns>A list containing this cell edit if the row matches; otherwise, an empty list.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            if (indexOfRow == _rowIndex)
                return new List<CellEdit>(new[] { this });
            return new List<CellEdit>();
        }

        /// <summary>
        /// Determines whether the edited cell affects the specified row.
        /// </summary>
        /// <param name="indexOfRow">The index of the row to check.</param>
        /// <returns><c>true</c> if this edit affects the specified row; otherwise, <c>false</c>.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            return _rowIndex == indexOfRow;
        }

        /// <summary>
        /// Adjusts the column index of the edit when a new column is added.
        /// </summary>
        /// <typeparam name="T">The type of the column data (optional)</typeparam>
        /// <param name="indexOfColumn">The index where the column is added. </param>
        /// <param name="columnData">The data of the new column (optional)</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            if (_columnIndex >= indexOfColumn)
                _columnIndex += 1;
            // 
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
        /// Adjusts the column index of the edit when a column is deleted.
        /// </summary>
        /// <param name="indexOfColumn">The index of the deleted column.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            if (_columnIndex < 0)
            {
                if (_columnIndex * -1 >= indexOfColumn)
                    _columnIndex -= 1;
            }
            // 
            if (_columnIndex == indexOfColumn)
                _columnIndex *= -1;
            if (_columnIndex > indexOfColumn)
                _columnIndex -= 1;
        }

        /// <summary>
        /// Adjusts the row index of the edit when a new row is added.
        /// </summary>
        /// <param name="indexOfRow">The index where the row is added.</param>
        /// <param name="rowData">The data of the new row.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
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
        /// Adjusts the row index of the edit when a row is deleted.
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
        /// Determines whether the edit contains the specified cell and return its value.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <param name="returnValue">The value of the cell if found.</param>
        /// <returns><c>true</c> if the cell matches the edit; otherwise, <c>false</c>.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            if (indexOfColumn != _columnIndex)
                return false;
            if (indexOfRow != _rowIndex)
                return false;
            returnValue = _editValue;
            return true;
        }

        /// <summary>
        /// Determines whether the edit affects the specified column. 
        /// </summary>
        /// <param name="indexOfColumn">The index of the column to check.</param>
        /// <returns><c>true</c> if the column is affected; otherwise, <c>false</c>.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            return indexOfColumn == _columnIndex;
        }
    }
}