using System;
using System.Collections.Generic;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an edit that modifies multiple individual cells within a table.
    /// Provides optimized range-based access for row and column edits.
    /// </summary>
    public class MultiCellEdit : TableEdit
    {

        private readonly CellEdit[] _cellEdits;
        private int _minColumnIndex;
        private int _maxColumnIndex;
        private int _minRowIndex;
        private int _maxRowIndex;

        /// <summary>
        /// Gets the array of individual cell edits contained in this group edit.
        /// </summary>
        public CellEdit[] CellEdits
        {
            get
            {
                return _cellEdits;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiCellEdit"/> class.
        /// </summary>
        /// <param name="cellEdits">An array of <see cref="CellEdit"/> objects representing the edited cells.</param>
        public MultiCellEdit(CellEdit[] cellEdits)
        {
            if (cellEdits == null || cellEdits.Length == 0) throw new ArgumentException("cellEdits cannot be null or empty.", nameof(cellEdits));
            _cellEdits = cellEdits;
            _minColumnIndex = cellEdits[0].ColumnIndex;
            _maxColumnIndex = _minColumnIndex;
            _minRowIndex = cellEdits[0].RowIndex;
            _maxRowIndex = _minRowIndex;
            // 
            foreach (CellEdit edit in cellEdits)
            {
                if (_minColumnIndex > edit.ColumnIndex)
                    _minColumnIndex = edit.ColumnIndex;
                if (_maxColumnIndex < edit.ColumnIndex)
                    _maxColumnIndex = edit.ColumnIndex;
                if (_minRowIndex > edit.RowIndex)
                    _minRowIndex = edit.RowIndex;
                if (_maxRowIndex < edit.RowIndex)
                    _maxRowIndex = edit.RowIndex;
            }
        }

        /// <summary>
        /// Returns the list of cell edits that occur in the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to search.</param>
        /// <returns>A list of <see cref="CellEdit"/> objects in the specified column.</returns>
        public override List<CellEdit> GetEditedCellsInColumn(int indexOfColumn)
        {
            var result = new List<CellEdit>();
            if (indexOfColumn < _minColumnIndex)
                return result;
            if (indexOfColumn > _maxColumnIndex)
                return result;
            for (int i = 0; i < _cellEdits.Length; i++)
            {
                if (_cellEdits[i].ColumnIndex == indexOfColumn && _cellEdits[i].RowIndex >= 0)
                    result.Add(_cellEdits[i]);
            }
            return result;
        }

        /// <summary>
        /// Returns the list of cell edits that occur in the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to search.</param>
        /// <returns>A list of <see cref="CellEdit"/> objects in the specified row.</returns>
        public override List<CellEdit> GetEditedCellsInRow(int indexOfRow)
        {
            var result = new List<CellEdit>();
            if (indexOfRow < _minRowIndex)
                return result;
            if (indexOfRow > _maxRowIndex)
                return result;
            for (int i = 0; i < _cellEdits.Length; i++)
            {
                if (_cellEdits[i].RowIndex == indexOfRow && _cellEdits[i].ColumnIndex >= 0)
                    result.Add(_cellEdits[i]);
            }
            return result;
        }

        /// <summary>
        /// Updates all contained <see cref="CellEdit"/> instances in response to a column addition .
        /// </summary>
        /// <typeparam name="T">The type of the added column data.</typeparam>
        /// <param name="indexOfColumn">The index where the column was added.</param>
        /// <param name="columnData">Optional column data.</param>
        public override void ColumnAdded<T>(int indexOfColumn, T[] columnData = null)
        {
            if (indexOfColumn > _maxColumnIndex)
                return;
            for (int i = 0; i < _cellEdits.Length; i++)
                _cellEdits[i].ColumnAdded(indexOfColumn, columnData);
            if (indexOfColumn <= _minColumnIndex)
                _minColumnIndex += 1;
            if (indexOfColumn <= _maxColumnIndex)
                _maxColumnIndex += 1;
        }

        /// <summary>
        /// Updates all contained<see cref="CellEdit"/> instances in response to a column deletion
        /// </summary>
        /// <param name="indexOfColumn">The index of the column that was deleted.</param>
        public override void ColumnDeleted(int indexOfColumn)
        {
            if (indexOfColumn > _maxColumnIndex)
                return;
            for (int i = 0; i < _cellEdits.Length; i++)
                _cellEdits[i].ColumnDeleted(indexOfColumn);
            if (indexOfColumn < _minColumnIndex)
                _minColumnIndex -= 1;
            if (indexOfColumn < _maxColumnIndex)
                _maxColumnIndex -= 1;
        }

        /// <summary>
        /// Updates all contained <see cref="CellEdit"/> instances in response to a row addition.
        /// </summary>
        /// <param name="indexOfRow">The index where the row was added.</param>
        /// <param name="rowData">Optional row data.</param>
        public override void RowAdded(int indexOfRow, object[] rowData = null)
        {
            if (indexOfRow > _maxRowIndex)
                return;
            for (int i = 0; i < _cellEdits.Length; i++)
                _cellEdits[i].RowAdded(indexOfRow);
            if (indexOfRow <= _minRowIndex)
                _minRowIndex += 1;
            if (indexOfRow <= _maxRowIndex)
                _maxRowIndex += 1;
        }

        /// <summary>
        /// Updates all contained <see cref="CellEdit"/> instances in response to a row deletion.
        /// </summary>
        /// <param name="indexOfRow">The index of the row that was deleted.</param>
        public override void RowDeleted(int indexOfRow)
        {
            if (indexOfRow > _maxRowIndex)
                return;
            for (int i = 0; i < _cellEdits.Length; i++)
                _cellEdits[i].RowDeleted(indexOfRow);
            if (indexOfRow < _minRowIndex)
                _minRowIndex -= 1;
            if (indexOfRow < _maxRowIndex)
                _maxRowIndex -= 1;
        }

        /// <summary>
        /// Checks whether the specified cell is included in the edit and retrieves its value if found.
        /// </summary>
        /// <param name="indexOfColumn">The column index of the cell.</param>
        /// <param name="indexOfRow">The row index of the cell.</param>
        /// <param name="returnValue">The value of the cell if found.</param>
        /// <returns><c>true</c> if the specified cell is included in this edit; otherwise, <c>false</c>.</returns>
        public override bool ContainsCell(int indexOfColumn, int indexOfRow, ref object returnValue)
        {
            if (indexOfRow < _minRowIndex)
                return false;
            if (indexOfRow > _maxRowIndex)
                return false;
            if (indexOfColumn < _minColumnIndex)
                return false;
            if (indexOfColumn > _maxColumnIndex)
                return false;
            for (int i = 0; i < _cellEdits.Length; i++)
            {
                if (_cellEdits[i].ContainsCell(indexOfColumn, indexOfRow, ref returnValue))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether the edit contains any changes in the specified column.
        /// </summary>
        /// <param name="indexOfColumn">The column index to check.</param>
        /// <returns><c>true</c> if the edit contains changes in the specified column; otherwise, <c>false</c>.</returns>
        public override bool ContainsColumn(int indexOfColumn)
        {
            if (indexOfColumn < _minColumnIndex)
                return false;
            if (indexOfColumn > _maxColumnIndex)
                return false;
            for (int i = 0; i < _cellEdits.Length; i++)
            {
                if (_cellEdits[i].ColumnIndex == indexOfColumn)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether the edit contains any changes in the specified row.
        /// </summary>
        /// <param name="indexOfRow">The row index to check.</param>
        /// <returns><c>true</c> if the edit contains changes in the specified row; otherwise, <c>false</c>.</returns>
        public override bool ContainsRow(int indexOfRow)
        {
            if (indexOfRow < _minRowIndex)
                return false;
            if (indexOfRow > _maxRowIndex)
                return false;
            for (int i = 0; i < _cellEdits.Length; i++)
            {
                if (_cellEdits[i].RowIndex == indexOfRow)
                    return true;
            }
            return false;
        }
    }
}