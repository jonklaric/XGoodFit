using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
#if NET9_0_OR_GREATER
using System.Text.Json;
#else
using System.Text;
#endif

namespace DatabaseManager
{
    /// <summary>
    /// Abstract base class for managing database files, providing shared functionality such as event hooks and static helpers.
    /// </summary>
    public abstract class DataTableView : IDisposable
    {

        #region Variables

        /// <summary>
        /// Reference to the parent <see cref="DatabaseManager"/>
        /// </summary>
        protected DatabaseManager _parentDatabase;

        /// <summary>
        /// The name of the table represented by this view.
        /// </summary>
        protected string _tableName;
        
        /// <summary>
        /// Sorted column names from the original database.
        /// </summary>
        protected string[] _storedColumnNames;

        /// <summary>
        /// Stored column data types from the original database.
        /// </summary>
        protected Type[] _storedColumnTypes;

        /// <summary>
        /// Number of rows stored in the database for this table.
        /// </summary>
        protected int _storedNumberOfRows;

        /// <summary>
        /// Number of rows currently in the view.
        /// </summary>
        private int _nRows;
        
        /// <summary>
        /// Names of the columns in the current view.
        /// </summary>
        private string[] _columnNames;

        /// <summary>
        /// Types of columns in the current view.
        /// </summary>
        private Type[] _columnTypes;

        /// <summary>
        /// Local list of edits pending commit to the database.
        /// </summary>
        private readonly List<TableEdit> _edits = new List<TableEdit>();

        /// <summary>
        /// Index of the last edit in the list of edits.
        /// </summary>
        private int _editIndex = -1;
        private int[] _viewToStoredRowIndex; // array index is the view row index, array value is the stored row index
        private int[] _viewToStoredColumnIndex; // array index is the view column index, array value is the stored column index

        /// <summary>
        /// Set of column names currently hidden in the view.
        /// </summary>
        private HashSet<string> _hiddenColumns = new HashSet<string>();
        #endregion

        #region Properties

        /// <summary>
        /// Gets the parent database manager.
        /// </summary>
        public DatabaseManager ParentDatabase
        {
            get
            {
                return _parentDatabase;
            }
        }

        /// <summary>
        /// Gets the name of the table associated in the view.
        /// </summary>
        public string TableName
        {
            get
            {
                return _tableName;
            }
        }

        /// <summary>
        /// Gets the current column names.
        /// </summary>
        public string[] ColumnNames
        {
            get
            {
                return _columnNames;
            }
        }

        /// <summary>
        /// Gets the current column data types.
        /// </summary>
        public Type[] ColumnTypes
        {
            get
            {
                return _columnTypes;
            }
        }

        /// <summary>
        /// Gets the current number of rows.
        /// </summary>
        public int NumberOfRows
        {
            get
            {
                return _nRows;
            }
        }
        #endregion

        #region Events

        /// <summary>
        /// Raised when rows are deleted from the table view.
        /// </summary>
        public event RowsDeletedEventHandler RowsDeleted;

        /// <summary>
        /// Delegate for handling the <see cref="RowsDeleted"/> event.
        /// </summary>
        /// <param name="rowIndices">The indices of the rows that were deleted.</param>
        public delegate void RowsDeletedEventHandler(int[] rowIndices);

        /// <summary>
        /// Raised when rows are added to the table view.
        /// </summary>
        public event RowsAddedEventHandler RowsAdded;

        /// <summary>
        /// Delegate for handling the <see cref="RowsAdded"/> event.
        /// </summary>
        /// <param name="rowIndices">The indices of the rows that were added.</param>
        public delegate void RowsAddedEventHandler(int[] rowIndices);

        /// <summary>
        /// Raised when columns are deleted from the table view.
        /// </summary>
        public event ColumnsDeletedEventHandler ColumnsDeleted;

        /// <summary>
        /// Delegate for handling the <see cref="ColumnsDeleted"/> event.
        /// </summary>
        /// <param name="columnIndices">The indices of the columns that were deleted.</param>
        public delegate void ColumnsDeletedEventHandler(int[] columnIndices);

        /// <summary>
        /// Raised when columns are added to the table view.
        /// </summary>
        public event ColumnsAddedEventHandler ColumnsAdded;

        /// <summary>
        /// Delegate for handling the <see cref="ColumnsAdded"/> event.
        /// </summary>
        /// <param name="columnIndices">The indices of the columns that were added.</param>
        public delegate void ColumnsAddedEventHandler(int[] columnIndices);

        /// <summary>
        /// Raised when an edit is added to the list of pending edits.
        /// </summary>
        public event EditAddedEventHandler EditAdded;

        /// <summary>
        /// Delegate for handling the <see cref="EditAdded"/> event.
        /// </summary>
        /// <param name="edit">The edit that was added to the pending edits list.</param>
        public delegate void EditAddedEventHandler(TableEdit edit);

        #endregion

        /// <summary>
        /// Initializes the view by loading metadata and mappings.
        /// </summary>
        protected void InitializeView()
        {
            _nRows = (int)GetStoredRowCount(); 
            _columnNames = GetStoredColumnNames();
            _columnTypes = GetStoredColumnTypes();
            _viewToStoredRowIndex = new int[_nRows];
            for (int i = 0; i < _viewToStoredRowIndex.Length; i++)
            { 
                _viewToStoredRowIndex[i] = i; 
            }
            _viewToStoredColumnIndex = new int[(_columnNames.Length)];
            for (int i = 0; i < _viewToStoredColumnIndex.Length; i++)
            { 
                _viewToStoredColumnIndex[i] = i; 
            }
        }

        #region Editing stuff

        /// <summary>
        /// Applies all accumulated edits (rows, columns, and cells) to the underlying database.
        /// Commits the changes in order: structural edits first, then data edits.
        /// Triggers <see cref="DatabaseManager.OnPreviewEditsSaved(string, ref bool)"/> and <see cref="DatabaseManager.OnEditsSaved(string, List{TableEdit})"/>
        /// </summary>
        public void ApplyEdits()
        {
            bool cancelSave = false;
            _parentDatabase.OnPreviewEditsSaved(_tableName, ref cancelSave);
            if (cancelSave == true) { return; }
            // 
            if (_editIndex < 0)
            {
                CancelEdits();
                return;
            }
            // first apply all of the row and column edits since the cell edits indices have been updated to the current view.
            bool wasOpen = _parentDatabase.DataBaseOpen;
            if (wasOpen == false)
            {
                _parentDatabase.Open(); 
            }
            // 
            for (int i = 0; i <= _editIndex; i++)
            {
                switch (_edits[i].GetType())
                {
                    case var @case when @case == typeof(AddRowEdit):
                        {
                            AddRowToDatabase(((AddRowEdit)_edits[i]).OriginalRowEdit);
                            break;
                        }
                    case var case1 when case1 == typeof(AddRowsEdit):
                        {
                            AddRowsToDatabase(((AddRowsEdit)_edits[i]).GetOriginalRowEdits());
                            break;
                        }
                    case var case2 when case2 == typeof(AddColumnEdit<byte[]>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<byte[]>)_edits[i]).ColumnName, ((AddColumnEdit<byte[]>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case3 when case3 == typeof(AddColumnEdit<double>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<double>)_edits[i]).ColumnName, ((AddColumnEdit<double>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case4 when case4 == typeof(AddColumnEdit<float>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<float>)_edits[i]).ColumnName, ((AddColumnEdit<float>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case5 when case5 == typeof(AddColumnEdit<long>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<long>)_edits[i]).ColumnName, ((AddColumnEdit<long>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case6 when case6 == typeof(AddColumnEdit<int>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<int>)_edits[i]).ColumnName, ((AddColumnEdit<int>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case7 when case7 == typeof(AddColumnEdit<short>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<short>)_edits[i]).ColumnName, ((AddColumnEdit<short>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case8 when case8 == typeof(AddColumnEdit<byte>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<byte>)_edits[i]).ColumnName, ((AddColumnEdit<byte>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case9 when case9 == typeof(AddColumnEdit<bool>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<bool>)_edits[i]).ColumnName, ((AddColumnEdit<bool>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case10 when case10 == typeof(AddColumnEdit<string>):
                        {
                            AddColumnToDatabase(((AddColumnEdit<string>)_edits[i]).ColumnName, ((AddColumnEdit<string>)_edits[i]).OriginalColumnEdit);
                            break;
                        }
                    case var case11 when case11 == typeof(AddColumnsEdit):
                        {
                            foreach (IColumnEdit columnToAdd in ((AddColumnsEdit)_edits[i]).AddColumnEdits)
                            {
                                switch (columnToAdd.GetType())
                                {
                                    case var case12 when case12 == typeof(AddColumnEdit<byte[]>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<byte[]>)columnToAdd).ColumnName, ((AddColumnEdit<byte[]>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case13 when case13 == typeof(AddColumnEdit<double>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<double>)columnToAdd).ColumnName, ((AddColumnEdit<double>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case14 when case14 == typeof(AddColumnEdit<float>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<float>)columnToAdd).ColumnName, ((AddColumnEdit<float>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case15 when case15 == typeof(AddColumnEdit<long>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<long>)columnToAdd).ColumnName, ((AddColumnEdit<long>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case16 when case16 == typeof(AddColumnEdit<int>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<int>)columnToAdd).ColumnName, ((AddColumnEdit<int>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case17 when case17 == typeof(AddColumnEdit<short>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<short>)columnToAdd).ColumnName, ((AddColumnEdit<short>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case18 when case18 == typeof(AddColumnEdit<byte>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<byte>)columnToAdd).ColumnName, ((AddColumnEdit<byte>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case19 when case19 == typeof(AddColumnEdit<bool>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<bool>)columnToAdd).ColumnName, ((AddColumnEdit<bool>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                    case var case20 when case20 == typeof(AddColumnEdit<string>):
                                        {
                                            AddColumnToDatabase(((AddColumnEdit<string>)columnToAdd).ColumnName, ((AddColumnEdit<string>)columnToAdd).OriginalColumnEdit);
                                            break;
                                        }
                                }
                            }

                            break;
                        }
                    case var case21 when case21 == typeof(DeleteRowEdit):
                        {
                            DeleteRowFromDatabase(((DeleteRowEdit)_edits[i]).RowIndex);
                            break;
                        }
                    case var case22 when case22 == typeof(DeleteRowsEdit):
                        {
                            DeleteRowsFromDatabase(((DeleteRowsEdit)_edits[i]).GetDeletedRowIndices());
                            break;
                        }
                    case var case23 when case23 == typeof(DeleteColumnEdit):
                        {
                            DeleteColumnFromDatabase(_storedColumnNames[((DeleteColumnEdit)_edits[i]).ColumnIndex]);
                            break;
                        }
                    case var case24 when case24 == typeof(DeleteColumnsEdit):
                        {
                            {
                                var withBlock = (DeleteColumnsEdit)_edits[i];
                                var columnsToDelete = new string[(withBlock.ColumnsDeleted.Length)];
                                for (int j = 0; j < withBlock.ColumnsDeleted.Length; j++)
                                    columnsToDelete[j] = _storedColumnNames[withBlock.ColumnsDeleted[j].ColumnIndex];
                                DeleteColumnsFromDatabase(columnsToDelete);
                            }

                            break;
                        }
                }
            }

            // next apply all of the database edits (the indices should match up with the database since all the row/column deletes and additions have been made.
            for (int i = 0; i <= _editIndex; i++)
            {
                switch (_edits[i].GetType())
                {
                    case var case25 when case25 == typeof(CellEdit):
                        {
                            ApplyCellEdit((CellEdit)_edits[i]);
                            break;
                        }
                    case var case26 when case26 == typeof(ColumnEdit):
                        {
                            ApplyEditColumnEdit((ColumnEdit)_edits[i]);
                            break;
                        }
                    case var case27 when case27 == typeof(MultiCellEdit):
                        {
                            {
                                var withBlock1 = (MultiCellEdit)_edits[i];
                                var columnIndices = new int[(withBlock1.CellEdits.Length)];
                                var rowIndices = new int[(withBlock1.CellEdits.Length)];
                                var cellvalues = new object[(withBlock1.CellEdits.Length)];
                                for (int j = 0; j < columnIndices.Length; j++)
                                {
                                    columnIndices[j] = withBlock1.CellEdits[j].ColumnIndex;
                                    rowIndices[j] = withBlock1.CellEdits[j].RowIndex;
                                    cellvalues[j] = withBlock1.CellEdits[j].Value;
                                }
                                EditDatabaseCells(columnIndices, rowIndices, cellvalues);
                            }

                            break;
                        }
                    case var case28 when case28 == typeof(RowEdit):
                        {
                            // for now I am just applying the edits as a multi-cell edit. This works alright since the database calls put it in a transaction. 
                            {
                                var withBlock2 = ((RowEdit)_edits[i]).GetRowData();
                                var columnIndices = new int[(withBlock2.CellEdits.Length)];
                                var rowIndices = new int[(withBlock2.CellEdits.Length)];
                                var cellvalues = new object[(withBlock2.CellEdits.Length)];
                                for (int j = 0; j < columnIndices.Length; j++)
                                {
                                    columnIndices[j] = withBlock2.CellEdits[j].ColumnIndex;
                                    rowIndices[j] = withBlock2.CellEdits[j].RowIndex;
                                    cellvalues[j] = withBlock2.CellEdits[j].Value;
                                }
                                EditDatabaseCells(columnIndices, rowIndices, cellvalues);
                            }

                            break;
                        }
                        // row edits have not been implemented. No real purpose for them yet.
                        // Throw New NotImplementedException()
                }
            }
            // 
            if (wasOpen == false)
            {
                _parentDatabase.Close(); 
            }
            // Raise edits saved event
            var editsSaved = new List<TableEdit>();
            for (int i = 0; i <= _editIndex; i++)
            { 
                editsSaved.Add(_edits[i]); 
            }
            // Clear edits
            CancelEdits();

            _parentDatabase.OnEditsSaved(_tableName, editsSaved);


        }

        /// <summary>
        /// Applies a single <see cref="CellEdit"/> to the database based on its value type.
        /// </summary>
        /// <param name="cellToEdit">The cell edit object containing row, column, and value.</param>
        private void ApplyCellEdit(CellEdit cellToEdit)
        {
            // Guard against null/DBNull cell values — cellToEdit.Value.GetType()
            // would NRE. ConvertToColumnType normally maps null to DBNull before
            // the edit reaches this stack, so a null here is unexpected; emit a
            // diagnostic and skip the apply rather than crashing the dispatcher.
            // The cell retains its prior stored value; a cleared cell should be
            // routed through ConvertToColumnType (which produces a typed default).
            if (cellToEdit.Value is null || cellToEdit.Value is DBNull)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[DataTableView] Skipping null/DBNull cell edit at column {cellToEdit.ColumnIndex}, row {cellToEdit.RowIndex}.");
                return;
            }

            switch (cellToEdit.Value.GetType())
            {
                case var @case when @case == typeof(double):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (double)cellToEdit.Value);
                        break;
                    }
                case var case1 when case1 == typeof(float):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (float)cellToEdit.Value);
                        break;
                    }
                case var case2 when case2 == typeof(long):
                case var case3 when case3 == typeof(ulong):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (long)cellToEdit.Value);
                        break;
                    }
                case var case4 when case4 == typeof(int):
                case var case5 when case5 == typeof(uint):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (int)cellToEdit.Value);
                        break;
                    }
                case var case6 when case6 == typeof(short):
                case var case7 when case7 == typeof(ushort):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (short)cellToEdit.Value);
                        break;
                    }
                case var case8 when case8 == typeof(byte):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (byte)cellToEdit.Value);
                        break;
                    }
                case var case9 when case9 == typeof(bool):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (bool)cellToEdit.Value);
                        break;
                    }
                case var case10 when case10 == typeof(string):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (string)cellToEdit.Value);
                        break;
                    }
                case var case11 when case11 == typeof(byte[]):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (byte[])cellToEdit.Value);
                        break;
                    }
                case var case12 when case12 == typeof(DateTime):
                    {
                        EditDatabaseCell(cellToEdit.ColumnIndex, cellToEdit.RowIndex, (DateTime)cellToEdit.Value);
                        break;
                    }
            }
        }

        /// <summary>
        /// Applies a <see cref="ColumnEdit"/> by converting its object array to the appropriate type and writing to the database.
        /// </summary>
        /// <param name="columnEditToAdd">The cell edit object containing row, column, and value.</param>
        private void ApplyEditColumnEdit(ColumnEdit columnEditToAdd)
        {
            switch (_storedColumnTypes[columnEditToAdd.ColumnIndex])
            {
                case var @case when @case == typeof(double):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (double)o));
                        break;
                    }
                case var case1 when case1 == typeof(float):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (float)o));
                        break;
                    }
                case var case2 when case2 == typeof(long):
                case var case3 when case3 == typeof(ulong):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (long)o));
                        break;
                    }
                case var case4 when case4 == typeof(int):
                case var case5 when case5 == typeof(uint):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (int)o));
                        break;
                    }
                case var case6 when case6 == typeof(short):
                case var case7 when case7 == typeof(ushort):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (short)o));
                        break;
                    }
                case var case8 when case8 == typeof(byte):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (byte)o));
                        break;
                    }
                case var case9 when case9 == typeof(bool):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (bool)o));
                        break;
                    }
                case var case10 when case10 == typeof(byte[]):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (byte[])o));
                        break;
                    }
                case var case11 when case11 == typeof(string):
                    {
                        EditDatabaseColumn(_storedColumnNames[columnEditToAdd.ColumnIndex], Array.ConvertAll(columnEditToAdd.ColumnData.ToArray(), o => (string)o));
                        break;
                    }
            }
        }

        /// <summary>
        /// Determines whether there are edits available to undo.
        /// </summary>
        /// <returns><c>true</c> if there are edits that can be undone; otherwise, <c>false</c>.</returns>
        public bool CanUndo()
        {
            return _editIndex >= 0;
        }

        /// <summary>
        /// Determines whether there are undone edits that can be redone.
        /// </summary>
        /// <returns><c>true</c> if there are undone edits that can be redone; otherwise, <c>false</c>.</returns>
        public bool CanRedo()
        {
            return _editIndex < _edits.Count - 1;
        }

        /// <summary>
        /// Edits a single cell at the specified row and column index with the given value.
        /// </summary>
        /// <param name="rowIndex">Row index of the cell.</param>
        /// <param name="columnIndex">The column index of the cell.</param>
        /// <param name="cellEdit">The new value to set in the cell.</param>
        /// <exception cref="Exception">Thrown when the type of the value does not match the column type.</exception>
        public void EditCell(int rowIndex, int columnIndex, object cellEdit)
        {
            if (ConvertToColumnType(_columnTypes[columnIndex], ref cellEdit) == false)
            { 
                throw new Exception("Cell edit of type '" + cellEdit.GetType().ToString() + "' is not a valid type for column '" + _columnNames[columnIndex] + "' which is of type '" + _columnTypes[columnIndex].ToString() + "'.");
            }
            AddEdit(new CellEdit(rowIndex, columnIndex, cellEdit));
        }

        /// <summary>
        /// Edits a single cell at the specified row index and column name with given value.
        /// </summary>
        /// <param name="rowIndex">Row index of the cell.</param>
        /// <param name="columnName">The column name of the cell.</param>
        /// <param name="cellEdit">The new value to set in the cell.</param>
        public void EditCell(int rowIndex, string columnName, object cellEdit)
        {
            int columnIndex = Array.IndexOf(_columnNames, columnName);
            if (columnIndex < 0) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            EditCell(rowIndex, columnIndex, cellEdit);
        }

        /// <summary>
        /// Edits multiple cells at once using corresponding row, column, and value arrays.
        /// </summary>
        /// <param name="rowIndices">Array of row indices.</param>
        /// <param name="columnIndices">Array of column indices.</param>
        /// <param name="cellEdits">Array of values to apply to the specified cells.</param>
        /// <exception cref="Exception">Thrown if a cell edit contains an invalid type.</exception>
        public void EditCells(int[] rowIndices, int[] columnIndices, object[] cellEdits)
        {
            if (rowIndices.Length != columnIndices.Length) { return; }
            if (columnIndices.Length != cellEdits.Length) { return; }
            var cellEditSet = new CellEdit[(rowIndices.Length)];
            for (int i = 0; i < rowIndices.Length; i++)
            {
                if (ConvertToColumnType(_columnTypes[columnIndices[i]], ref cellEdits[i]) == false)
                { 
                    throw new Exception("Cell edit of type '" + cellEdits[i].GetType().ToString() + "' is not a valid type for column '" + _columnNames[columnIndices[i]] + "' which is of type '" + _columnTypes[columnIndices[i]].ToString() + "'. Error occurred at row index " + rowIndices[i] + "."); 
                }
                cellEditSet[i] = new CellEdit(rowIndices[i], columnIndices[i], cellEdits[i]);
            }
            // 
            AddEdit(new MultiCellEdit(cellEditSet));
        }
        
        /// <summary>
        /// Edits an entire row's data.
        /// </summary>
        /// <param name="rowIndex">The row index to update.</param>
        /// <param name="rowData">Array of new values for the row.</param>
        /// <exception cref="Exception">Thrown if the values don't match column count or types.</exception>
        public void EditRow(int rowIndex, object[] rowData)
        {
            if (rowData.Length != _columnNames.Length)
            { 
                throw new Exception("Number of columns in row edit do not match the number of columns in the current view."); 
            }
            for (int i = 0; i < rowData.Length; i++)
            {
                if (ConvertToColumnType(_columnTypes[i], ref rowData[i]) == false)
                { 
                    throw new Exception("Row edit of type '" + rowData[i].GetType().ToString() + "' is not a valid type for column '" + _columnNames[i] + "' which is of type '" + _columnTypes[i].ToString() + "'.");
                }
            }
            // 
            AddEdit(new RowEdit(rowData, rowIndex));
        }

        /// <summary>
        /// Edits an entire column's data.
        /// </summary>
        /// <param name="columnIndex">Index of the column to edit.</param>
        /// <param name="columnData">Array of values to apply to the column.</param>
        /// <exception cref="Exception">Thrown if the value count or types are invalid.</exception>
        public void EditColumn(int columnIndex, object[] columnData)
        {
            if (columnData.Length == 0) { return; }
            if (columnData.Length != _nRows)
            { 
                throw new Exception("Number of records in the column to edit do not match the number of records in the current view."); 
            }
            for (int i = 0; i < columnData.Length; i++)
            {
                // this is a hack to make it work with byte arrays. not the best solution by any means.
                var b = columnData[i];
                if (ConvertToColumnType(_columnTypes[columnIndex], ref b) == false)
                { 
                    throw new Exception("Attempting to edit column '" + _columnNames[columnIndex] + "' which is of type '" + _columnTypes[columnIndex].Name + "' with invalid data of type '" + columnData[i].GetType().Name + "'. Error occurred at row index " + i + ".");
                }
                columnData[i] = b;
            }
            // 
            AddEdit(new ColumnEdit(columnIndex, columnData));
        }

        /// <summary>
        /// Edits an entire column's data using a strongly typed array.
        /// </summary>
        /// <typeparam name="T">Type of data in the column.</typeparam>
        /// <param name="columnIndex">Index of the column to edit.</param>
        /// <param name="columnData">Array of values to apply to the column.</param>
        /// <exception cref="Exception">Thrown if the value count or types are invalid.</exception>
        public void EditColumn<T>(int columnIndex, T[] columnData)
        {
            if (columnData.Length == 0) { return; }
            if (columnData.Length != _nRows)
            {
                throw new Exception("Number of records in the column to edit do not match the number of records in the current view."); 
            }
            var editedColumn = new object[(columnData.Length)];
            for (int i = 0; i < columnData.Length; i++)
            {
                // this is a hack to make it work with byte arrays. not the best solution by any means.
                object b = columnData[i];
                if (ConvertToColumnType(_columnTypes[columnIndex], ref b) == false)
                {
                    throw new Exception("Attempting to edit column '" + _columnNames[columnIndex] + "' which is of type '" + _columnTypes[columnIndex].Name + "' with invalid data of type '" + columnData[i].GetType().Name + "'. Error occurred at row index " + i + "."); 
                }
                editedColumn[i] = b;
            }
            // 
            AddEdit(new ColumnEdit(columnIndex, editedColumn));
        }

        /// <summary>
        /// Deletes a row at the specified index.
        /// </summary>
        /// <param name="rowIndex">Index of the row to delete.</param>
        /// <exception cref="Exception">Index of the row to delete.</exception>
        public void DeleteRow(int rowIndex)
        {

            if (rowIndex < 0)
            {
                throw new Exception("Attempting to delete a row that is not in the current view."); 
            }
            if (rowIndex >= _nRows)
            {
                throw new Exception("Attempting to delete a row that is not in the current view."); 
            }

            AddEdit(new DeleteRowEdit(rowIndex));
            RowDeleteMade(rowIndex);
        }

        /// <summary>
        /// Deletes a range of rows.
        /// </summary>
        /// <param name="startIndex">Starting index of rows to delete.</param>
        /// <param name="endIndex">Ending index of rows to delete.</param>
        /// <exception cref="Exception">Thrown if indices are out of bounds.</exception>
        public void DeleteRows(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < 0)
            {
                throw new Exception("Attempting to delete a row that is not in the current view."); 
            }
            if (startIndex >= _nRows || endIndex >= _nRows)
            {
                throw new Exception("Attempting to delete a row that is not in the current view."); 
            }
            if (endIndex < startIndex) { return; }// Throw New Exception("Attempting to delete a row that is not in the current view.")
                        // 
            var rowsToDelete = new DeleteRowEdit[endIndex - startIndex + 1];
            var rowIndices = new int[endIndex - startIndex + 1];
            int counter = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                rowIndices[counter] = i;
                rowsToDelete[counter] = new DeleteRowEdit(i);
                counter += 1;
            }
            AddEdit(new DeleteRowsEdit(rowsToDelete));
            RowDeletesMade(rowIndices);
        }

        /// <summary>
        /// Deletes a set of rows specified by their indices.
        /// </summary>
        /// <param name="rowIndices">Array of row indices to delete.</param>
        /// <exception cref="Exception">Thrown if any index is out of bounds.</exception>
        public void DeleteRows(int[] rowIndices)
        {
            if (rowIndices == null)
                return;
            if (rowIndices.Length == 0)
            {
                return;
            }
            else if (rowIndices.Length == 1)
            {
                DeleteRow(rowIndices[0]);
            }
            else
            {
                Array.Sort(rowIndices);
                if (rowIndices[0] < 0)
                {
                    throw new Exception("Attempting to delete a row that is not in the current view."); 
                }
                if (rowIndices[rowIndices.Length - 1] >= _nRows)
                {
                    throw new Exception("Attempting to delete a row that is not in the current view."); 
                }
                var rowsToDelete = new DeleteRowEdit[(rowIndices.Length)];
                for (int i = 0; i < rowIndices.Length; i++)
                {
                    rowsToDelete[i] = new DeleteRowEdit(rowIndices[i]); 
                }

                AddEdit(new DeleteRowsEdit(rowsToDelete));
                RowDeletesMade(rowIndices);
            }
        }

        /// <summary>
        /// Adds a blank row with default values to the table.
        /// </summary>
        public void AddRow()
        {
            var dummyRow = new object[(_columnNames.Length)];
            for (int i = 0; i < _columnNames.Length; i++)
            { 
                dummyRow[i] = DBNull.Value;
            }
            AddRow(dummyRow);
        }

        /// <summary>
        /// Adds a row to the table with specified values.
        /// </summary>
        /// <param name="rowData">Array of values to populate the new row.</param>
        /// <exception cref="Exception">Thrown if values don't match expected count/types.</exception>
        public void AddRow(object[] rowData)
        {
            if (rowData == null) { return; }
            if (rowData.Length == 0) { return; }
            if (rowData.Length != _columnNames.Length)
            {
                throw new Exception("Number of columns in a row to be added do not match the number of columns in the current view."); 
            }
            for (int i = 0; i < rowData.Length; i++)
            {
                if (ConvertToColumnType(_columnTypes[i], ref rowData[i]) == false)
                {
                    throw new Exception("Row edit of type '" + rowData[i].GetType().ToString() + "' is not a valid type for column '" + _columnNames[i] + "' which is of type '" + _columnTypes[i].ToString() + "'."); 
                }
            }
            // Add it to the edit stack
            AddEdit(new AddRowEdit(rowData, _nRows));
            RowAddMade(_nRows, true, rowData);
        }

        /// <summary>
        /// Adds multiple rows to the table.
        /// </summary>
        /// <param name="rowData">List of object arrays representing each row to add.</param>
        /// <exception cref="Exception">Thrown if any row has mismatched values or types.</exception>
        public void AddRows(List<object[]> rowData)
        {
            if (rowData == null) { return; }
            if (rowData.Count == 0) { return; }
            for (int i = 0; i < rowData.Count; i++)
            {
                if (rowData[i].Length != _columnNames.Length)
                {
                    throw new Exception("Number of columns in a row to be added do not match the number of columns in the current view."); 
                }
                for (int j = 0; j < rowData[i].Length; j++)
                {
                    var tmp = rowData[i];
                    var argvalue = tmp[j];
                    if (ConvertToColumnType(_columnTypes[j], ref argvalue) == false)
                    {
                        throw new Exception("Row edit of type '" + rowData[i][j].GetType().ToString() + "' is not a valid type for column '" + _columnNames[j] + "' which is of type '" + _columnTypes[j].ToString() + "'."); 
                    }
                }
            }
            // Add it to the edit stack
            var rowsToAdd = new AddRowEdit[rowData.Count];
            var rowIndices = new int[rowData.Count];
            for (int i = 0; i < rowData.Count; i++)
            {
                rowIndices[i] = _nRows + i;
                rowsToAdd[i] = new AddRowEdit(rowData[i], rowIndices[i]);
            }

            AddEdit(new AddRowsEdit(rowsToAdd));
            RowAddsMade(rowIndices, rowData);
        }

        /// <summary>
        /// Adds rows to the table from a DataTable.
        /// </summary>
        /// <param name="newRowData">DataTable containing new rows.</param>
        /// <exception cref="Exception">Thrown if the data does not match expected schema.</exception>
        public void AddRows(DataTable newRowData)
        {
            if (newRowData == null) { return; }
            if (newRowData.Rows.Count == 0) { return; }
            if (newRowData.Columns.Count != _columnNames.Length)
            {
                throw new Exception("Number of columns in a row to be added do not match the number of columns in the current view."); 
            }
            for (int j = 0; j < newRowData.Columns.Count; j++)
            {
                bool localConvertToColumnType() { var tmp = newRowData.Rows[0]; var argvalue = tmp[j]; var ret = ConvertToColumnType(_columnTypes[j], ref argvalue); tmp[j] = argvalue; return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("Row edit of type '" + newRowData.Columns[j].GetType().ToString() + "' is not a valid type for column '" + _columnNames[j] + "' which is of type '" + _columnTypes[j].ToString() + "'."); 
                }
            }
            // Next
            // Add it to the edit stack
            var rowsToAdd = new AddRowEdit[newRowData.Rows.Count];
            var rowIndices = new int[newRowData.Rows.Count];
            var rowData = new List<object[]>();
            for (int i = 0; i < newRowData.Rows.Count; i++)
            {
                rowIndices[i] = _nRows + i;
                rowData.Add(newRowData.Rows[i].ItemArray);
                rowsToAdd[i] = new AddRowEdit(newRowData.Rows[i].ItemArray, rowIndices[i]);
            }

            AddEdit(new AddRowsEdit(rowsToAdd));
            RowAddsMade(rowIndices, rowData);
        }
        
        /// <summary>
        /// Deletes a column by its index. 
        /// </summary>
        /// <param name="columnIndex">The index of the column to delete.</param>
        /// <exception cref="Exception">Thrown if the index is invalid.</exception>
        public void DeleteColumn(int columnIndex)
        {
            if (columnIndex < 0) { return; }
            if (columnIndex >= _columnNames.Length)
            {
                throw new Exception("Attempting to delete a column that is not in the current view."); 
            }
            // 
            AddEdit(new DeleteColumnEdit(columnIndex));
            ColumnDeleteMade(columnIndex);
        }

        /// <summary>
        /// This method is not fully tested. Use at own risk.
        /// </summary>
        /// <param name="columnIndex">Index of the column to hide.</param>
        [Obsolete("This method has not been fully validated. Use with caution.")]
        public void HideColumn(int columnIndex)
        {
            if (columnIndex < 0) { return; }
            if (columnIndex >= _columnNames.Length)
            {
                throw new Exception("Attempting to hide a column that is not in the current view."); 
            }
            // 
            _hiddenColumns.Add(_columnNames[columnIndex]);
            UpdateColumnInfo();
        }

        /// <summary>
        /// This method is not fully tested. Use at own risk.
        /// </summary>
        /// <param name="columnName">name of the column to hide.</param>
        [Obsolete("This method has not been fully validated. Use with caution.")]
        public void HideColumn(string columnName)
        {
            if (_columnNames.Contains(columnName) == false)
            {
                throw new Exception("Attempting to hide a column that is not in the current view."); 
            }
            // 
            _hiddenColumns.Add(columnName);
            UpdateColumnInfo();
        }

        /// <summary>
        /// Deletes multiple columns by their indices.
        /// </summary>
        /// <param name="columnIndices">Array of column indices to delete.</param>
        /// <exception cref="Exception">Thrown if any index is invalid.</exception>
        public void DeleteColumns(int[] columnIndices)
        {
            if (columnIndices == null) { return; }
            if (columnIndices.Length == 0) { return; }
            Array.Sort(columnIndices);
            if (columnIndices[0] < 0)
            {
                throw new Exception("Attempting to delete a column that is not in the current view."); 
            }
            if (columnIndices[columnIndices.Length - 1] >= _columnNames.Length)
            {
                throw new Exception("Attempting to delete a column that is not in the current view."); 
            }
            // 
            var columnsToDelete = new DeleteColumnEdit[(columnIndices.Length)];
            for (int i = 0; i < columnIndices.Length; i++)
            {
                columnsToDelete[i] = new DeleteColumnEdit(columnIndices[i]); 
            }
            AddEdit(new DeleteColumnsEdit(columnsToDelete));
            ColumnDeletesMade(columnIndices);
        }

        /// <summary>
    /// Adds a blank column to the table.
    /// </summary>
    /// <param name="columnName">Name of the new column to add.</param>
    /// <param name="columnType">Type of the new column to add.</param>
        public void AddColumn(string columnName, Type columnType)
        {
            var defaultValue = GetDefaultFromType(columnType);
            object[] columnData = Enumerable.Repeat(defaultValue, _nRows).ToArray();
            AddColumn(columnName, columnData, columnType);
        }

        /// <summary>
        /// Adds a column using typed values.
        /// </summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="columnName">Name of the new column.</param>
        /// <param name="columnData">Array of values for the column.</param>
        /// <exception cref="Exception">Thrown if value count mismatches row count.</exception>
        public void AddColumn<T>(string columnName, T[] columnData)
        {
            if (columnData.Length != _nRows)
            {
                throw new Exception("Number of records in the column to edit do not match the number of records in the current view."); 
            }
            AddEdit(new AddColumnEdit<T>(columnData, _columnNames.Length, columnName));
            // update column information and existing edits
            ColumnAddMade(_columnNames.Length, true, columnData);
        }

        /// <summary>
        /// Adds a column with specified data and type.
        /// </summary>
        /// <param name="columnName">Name of the new column.</param>
        /// <param name="columnData">Array of values.</param>
        /// <param name="columnDataType">Type of the data values.</param>
        /// <exception cref="Exception">Thrown if values mismatch expected types.</exception>
        public void AddColumn(string columnName, object[] columnData, Type columnDataType)
        {
            if (columnData.Length != _nRows)
            { 
                throw new Exception("Number of records in the column to edit do not match the number of records in the current view."); 
            }
            if (columnData.Length > 0)
            {
                for (int i = 0; i < columnData.Length; i++)
                {
                    // this is a hack to make it work with byte arrays. not the best solution by any means.
                    var b = columnData[i];
                    if (ConvertToColumnType(columnDataType, ref b) == false)
                    {
                        throw new Exception("column data that is added must all be of the same type (e.g. integer). Column at row '" + (i + 1) + "' is of type '" + columnData[i].GetType().ToString() + "' and does not match the expected data type of type '" + columnDataType.ToString() + "'.");
                    }
                    columnData[i] = b;
                }
            }
            // 
            switch (columnDataType)
            {
                case var @case when @case == typeof(double):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? 0d : Convert.ToDouble(o)));
                        break;
                    }
                case var case1 when case1 == typeof(float):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? 0f : Convert.ToSingle(o)));
                        break;
                    }
                case var case2 when case2 == typeof(long):
                case var case3 when case3 == typeof(ulong):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? 0L : Convert.ToInt64(o)));
                        break;
                    }
                case var case4 when case4 == typeof(int):
                case var case5 when case5 == typeof(uint):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? 0 : Convert.ToInt32(o)));
                        break;
                    }
                case var case6 when case6 == typeof(short):
                case var case7 when case7 == typeof(ushort):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? (short)0 : Convert.ToInt16(o)));
                        break;
                    }
                case var case8 when case8 == typeof(byte):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? (byte)0 : Convert.ToByte(o)));
                        break;
                    }
                case var case9 when case9 == typeof(bool):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? false : Convert.ToBoolean(o)));
                        break;
                    }
                case var case10 when case10 == typeof(byte[]):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? (new byte[] { }) : (byte[])o));
                        break;
                    }
                case var case11 when case11 == typeof(string):
                    {
                        AddColumn(columnName, Array.ConvertAll(columnData.ToArray(), o => o is DBNull ? "" : Convert.ToString(o)));
                        break;
                    }
            }
        }

        /// <summary>
        /// Adds multiple columns at once.
        /// </summary>
        /// <param name="namesOfColumns">Names for new columns.</param>
        /// <param name="columnData">List of column value arrays.</param>
        /// <param name="columnDataTypes">Expected types for each column.</param>
        /// <exception cref="Exception">Thrown if any column data mismatch occurs.</exception>
        public void AddColumns(string[] namesOfColumns, List<object[]> columnData, Type[] columnDataTypes)
        {
            if (columnData == null) { return; }
            if (_columnNames == null) { return; }
            if (ColumnTypes == null) { return; }
            if (columnData.Count == 0) { return; }
            if (namesOfColumns.Length != columnData.Count)
            {
                throw new Exception("Number of column names and column datasets do not match for the columns to add.");
            }
            if (namesOfColumns.Length != columnDataTypes.Length)
            { 
                throw new Exception("Number of column names and column types do not match for the columns to add.");
            }
            for (int i = 0; i < columnData.Count; i++)
            {
                if (columnData[i].Length != _nRows)
                { 
                    throw new Exception("Number of records in the column to edit do not match the number of records in the current view."); 
                }
                if (columnData[i].Length > 0)
                {
                    for (int j = 0; j < columnData[i].Length; j++)
                    {
                        if (columnData[i][j].GetType() != columnDataTypes[i])
                        { 
                            throw new Exception("column data that is added must all be of the same type (e.g. integer). Column at row '" + (j + 1) + "' is of type '" + columnData[i][j].GetType().ToString() + "' and does not match the expected data type of type '" + columnDataTypes[i].ToString() + "'."); 
                        }
                    }
                }
            }
            // 
            var columnsToAdd = new IColumnEdit[(namesOfColumns.Length)];
            var addedIndices = new int[(namesOfColumns.Length)];
            for (int i = 0; i < namesOfColumns.Length; i++)
            {
                addedIndices[i] = _columnNames.Length + i;
                switch (columnDataTypes[i])
                {
                    case var @case when @case == typeof(double):
                        {
                            columnsToAdd[i] = new AddColumnEdit<double>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToDouble(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case1 when case1 == typeof(float):
                        {
                            columnsToAdd[i] = new AddColumnEdit<float>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToSingle(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case2 when case2 == typeof(long):
                    case var case3 when case3 == typeof(ulong):
                        {
                            columnsToAdd[i] = new AddColumnEdit<long>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToInt64(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case4 when case4 == typeof(int):
                    case var case5 when case5 == typeof(uint):
                        {
                            columnsToAdd[i] = new AddColumnEdit<int>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToInt32(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case6 when case6 == typeof(short):
                    case var case7 when case7 == typeof(ushort):
                        {
                            columnsToAdd[i] = new AddColumnEdit<short>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToInt16(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case8 when case8 == typeof(byte):
                        {
                            columnsToAdd[i] = new AddColumnEdit<byte>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToByte(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case9 when case9 == typeof(bool):
                        {
                            columnsToAdd[i] = new AddColumnEdit<bool>(Array.ConvertAll(columnData[i].ToArray(), o => Convert.ToBoolean(o)), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case10 when case10 == typeof(byte[]):
                        {
                            columnsToAdd[i] = new AddColumnEdit<byte[]>(Array.ConvertAll(columnData[i].ToArray(), o => (byte[])o), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                    case var case11 when case11 == typeof(string):
                        {
                            columnsToAdd[i] = new AddColumnEdit<string>(Array.ConvertAll(columnData[i].ToArray(), o => o.ToString()), addedIndices[i], namesOfColumns[i]);
                            break;
                        }
                }
            }
            // 
            AddEdit(new AddColumnsEdit(columnsToAdd));
            UpdateColumnInfo();
            // 
            for (int i = 0; i < addedIndices.Length; i++)
            {
                for (int j = 0; j < _editIndex; j++)
                {
                    _edits[j].ColumnAdded(addedIndices[i], columnData[i]);
                }
            }
            // 
            ColumnsAdded?.Invoke(addedIndices);
        }

        /// <summary>
        /// Clears all pending edits and reinitializes the table view.
        /// </summary>
        public void CancelEdits()
        {
            _edits.Clear();
            _editIndex = -1;
            InitializeView();
        }

        /// <summary>
        /// Undoes the last edit made to the table.
        /// </summary>
        public void UndoEdit()
        {
            if (_editIndex < 0)
                return;
            var editToUndo = _edits[_editIndex];
            _editIndex -= 1;
            switch (editToUndo.GetType())
            {
                case var @case when @case == typeof(DeleteRowEdit):
                    {
                        RowAddMade(((DeleteRowEdit)editToUndo).RowIndex, false);
                        break;
                    }
                case var case1 when case1 == typeof(DeleteRowsEdit):
                    {
                        int[] rowIndices = ((DeleteRowsEdit)editToUndo).GetDeletedRowIndices();
                        var rowData = new List<object[]>();
                        for (int i = 0; i < rowIndices.Length; i++)
                            rowData.Add(null);
                        RowAddsMade(rowIndices.ToArray(), rowData);
                        break;
                    }
                case var case2 when case2 == typeof(AddRowEdit):
                    {
                        RowDeleteMade(((AddRowEdit)editToUndo).RowIndex);
                        break;
                    }
                case var case3 when case3 == typeof(AddRowsEdit):
                    {
                        var rowIndices = new List<int>();
                        foreach (AddRowEdit rowAdd in ((AddRowsEdit)editToUndo).AddRowEdits)
                            rowIndices.Add(rowAdd.RowIndex);
                        RowDeletesMade(rowIndices.ToArray());
                        break;
                    }
                case var case4 when case4 == typeof(DeleteColumnEdit):
                    {
                        ColumnAddMade<bool>(((DeleteColumnEdit)editToUndo).ColumnIndex, true);
                        break;
                    }
                case var case5 when case5 == typeof(DeleteColumnsEdit):
                    {
                        var columnIndices = new List<int>();
                        var rowData = new List<object[]>();
                        foreach (DeleteColumnEdit columnDelete in ((DeleteColumnsEdit)editToUndo).ColumnsDeleted)
                        {
                            columnIndices.Add(columnDelete.ColumnIndex);
                            rowData.Add(null);
                        }
                        ColumnAddsMade(columnIndices.ToArray(), rowData);
                        break;
                    }
                case var case6 when case6 == typeof(AddColumnsEdit):
                    {
                        var columnIndices = new List<int>();
                        foreach (IColumnEdit columnAdd in ((AddColumnsEdit)editToUndo).AddColumnEdits)
                            columnIndices.Add(columnAdd.ColumnIndex);
                        ColumnDeletesMade(columnIndices.ToArray());
                        break;
                    }
            }

            if (editToUndo is IColumnEdit)
            {
                if (((IColumnEdit)editToUndo).IsColumnAdd == true)
                { 
                    ColumnDeleteMade(((IColumnEdit)editToUndo).ColumnIndex);
                }
            }


        }

        /// <summary>
        /// Redoes the previously undone edit.
        /// </summary>
        public void RedoEdit()
        {
            if (!CanRedo()) return;
            if (_editIndex < _edits.Count - 1)
            { 
                _editIndex += 1;
            }
            var editToRedo = _edits[_editIndex];
            switch (editToRedo.GetType())
            {
                case var @case when @case == typeof(DeleteRowEdit):
                    {
                        RowDeleteMade(((DeleteRowEdit)editToRedo).RowIndex);
                        break;
                    }
                case var case1 when case1 == typeof(DeleteRowsEdit):
                    {
                        int[] rowIndices = ((DeleteRowsEdit)editToRedo).GetDeletedRowIndices();
                        RowDeletesMade(rowIndices.ToArray());
                        break;
                    }
                case var case2 when case2 == typeof(AddRowEdit):
                    {
                        RowAddMade(((AddRowEdit)editToRedo).RowIndex, true);
                        break;
                    }
                case var case3 when case3 == typeof(AddRowsEdit):
                    {
                        var rowData = new List<object[]>();
                        var rowIndices = new List<int>();
                        foreach (AddRowEdit rowAdd in ((AddRowsEdit)editToRedo).AddRowEdits)
                        {
                            rowIndices.Add(rowAdd.RowIndex);
                            rowData.Add(null);
                        }
                        RowAddsMade(rowIndices.ToArray(), rowData);
                        break;
                    }
                case var case4 when case4 == typeof(DeleteColumnEdit):
                    {
                        ColumnDeleteMade(((DeleteColumnEdit)editToRedo).ColumnIndex);
                        break;
                    }
                case var case5 when case5 == typeof(DeleteColumnsEdit):
                    {
                        var columnIndices = new List<int>();
                        foreach (DeleteColumnEdit columnDelete in ((DeleteColumnsEdit)editToRedo).ColumnsDeleted)
                        { 
                            columnIndices.Add(columnDelete.ColumnIndex); 
                        }
                        ColumnDeletesMade(columnIndices.ToArray());
                        break;
                    }
                case var case6 when case6 == typeof(AddColumnsEdit):
                    {
                        var rowData = new List<object[]>();
                        var columnIndices = new List<int>();
                        foreach (IColumnEdit columnAdd in ((AddColumnsEdit)editToRedo).AddColumnEdits)
                        {
                            columnIndices.Add(columnAdd.ColumnIndex);
                            rowData.Add(null);
                        }
                        ColumnAddsMade(columnIndices.ToArray(), rowData);
                        break;
                    }
            }

            if (editToRedo is IColumnEdit)
            {
                if (((IColumnEdit)editToRedo).IsColumnAdd == true)
                { 
                    ColumnAddMade<bool>(((IColumnEdit)editToRedo).ColumnIndex, true);
                }
            }
        }

        /// <summary>
        /// Adds an edit operation to the internal undo/redo stack.
        /// </summary>
        /// <param name="newEdit">The edit to track and manage.</param>
        private void AddEdit(TableEdit newEdit)
        {
            _editIndex += 1;
            if (_editIndex < _edits.Count)
            {
                _edits.RemoveRange(_editIndex, _edits.Count - _editIndex);
                _edits.Add(newEdit);
            }
            else
            {
                _edits.Add(newEdit);
            }
            // 
            EditAdded?.Invoke(newEdit);
        }

        /// <summary>
        /// Updates the internal state after a row has been deleted.
        /// </summary>
        /// <param name="rowIndex">The index of the deleted row.</param>
        private void RowDeleteMade(int rowIndex)
        {
            _nRows -= 1;
            // 
            UpdateRowInfo();
            // 
            for (int i = 0; i <= _editIndex; i++)
            { 
                _edits[i].RowDeleted(rowIndex);
            }
            // 
            RowsDeleted?.Invoke(new[] { rowIndex });
        }

        /// <summary>
        /// Updates the internal state after a row has been deleted.
        /// </summary>
        /// <param name="rowIndices">Indices of rows deleted.</param>
        private void RowDeletesMade(int[] rowIndices)
        {
            _nRows -= rowIndices.Length;
            // 
            UpdateRowInfo();
            // 
            for (int i = 0; i <= _editIndex; i++)
            {
                // For some reason I am not updating the row indices for the delete/add row edits. I need to test if not updating row indices is ok.
                // This can really get slow when there are a lot of rows getting deleted (opportunity to improve).
                if (_edits[i].GetType() == typeof(DeleteRowEdit) || _edits[i].GetType() == typeof(DeleteRowsEdit))
                {
                    continue;
                }
                for (int j = rowIndices.Length - 1; j >= 0; j -= 1)
                {
                    _edits[i].RowDeleted(rowIndices[j]);
                }
            }
            // 
            RowsDeleted?.Invoke(rowIndices);
        }

        /// <summary>
        /// Updates the internal state after a row has been added.
        /// </summary>
        /// <param name="rowIndex">The index of the added row.</param>
        /// <param name="ignoreLastEdit">Indicates if the most recent edit should be ignored.</param>
        /// <param name="rowData">The data added to the row.</param>
        private void RowAddMade(int rowIndex, bool ignoreLastEdit, object[] rowData = null)
        {
            _nRows += 1;
            // 
            UpdateRowInfo();
            // 
            if (ignoreLastEdit)
            {
                for (int i = 0; i < _editIndex; i++)
                { 
                    _edits[i].RowAdded(rowIndex, rowData); 
                }
            }
            else
            {
                for (int i = 0; i <= _editIndex; i++)
                { 
                    _edits[i].RowAdded(rowIndex, rowData);
                }
            }
            // 
            RowsAdded?.Invoke(new[] { rowIndex });
        }

        /// <summary>
        /// Updates the internal state after multiple rows have been added.
        /// </summary>
        /// <param name="rowIndices">Indices of added rows.</param>
        /// <param name="rowData">Row data for each index.</param>
        private void RowAddsMade(int[] rowIndices, List<object[]> rowData)
        {
            _nRows += rowIndices.Length;
            // 
            UpdateRowInfo();
            // 
            for (int i = 0; i <= _editIndex; i++)
            {
                // For some reason I am not updating the row indices for the delete/add row edits. I need to test if not updating row indices is ok. 
                // This can really get slow when there are a lot of rows getting added (opportunity to improve).
                if (_edits[i].GetType() == typeof(DeleteRowEdit) || _edits[i].GetType() == typeof(DeleteRowsEdit))
                {
                    continue;
                }
                for (int j = 0; j < rowIndices.Length; j++)
                {
                    _edits[i].RowAdded(rowIndices[j], rowData[j]); 
                }
            }
            // 
            RowsAdded?.Invoke(rowIndices);
        }

        /// <summary>
        /// Updates the internal state after a column has been deleted.
        /// </summary>
        /// <param name="columnIndex">The index of the deleted column.</param>
        private void ColumnDeleteMade(int columnIndex)
        {
            UpdateColumnInfo();
            // 
            for (int i = 0; i <= _editIndex; i++)
            {
                _edits[i].ColumnDeleted(columnIndex);
            }
            // 
            ColumnsDeleted?.Invoke(new[] { columnIndex });
        }

        /// <summary>
        /// Updates the internal state after multiple columns have been deleted.
        /// </summary>
        /// <param name="columnIndices">Indices of deleted columns.</param>
        private void ColumnDeletesMade(int[] columnIndices)
        {
            UpdateColumnInfo();
            // 
            for (int i = 0; i < columnIndices.Length; i++)
            {
                for (int j = 0; j <= _editIndex; j++)
                { 
                    _edits[j].ColumnDeleted(columnIndices[i]); 
                }
            }
            // 
            ColumnsDeleted?.Invoke(columnIndices);
        }

        /// <summary>
        /// Updates the internal state after a column has been added.
        /// </summary>
        /// <typeparam name="T">The type of data added to the column.</typeparam>
        /// <param name="columnIndex">The index where the column was added.</param>
        /// <param name="ignoreLastEdit">True if the last edit should be skipped when updating.</param>
        /// <param name="columnData">Optional data added in the column.</param>
        private void ColumnAddMade<T>(int columnIndex, bool ignoreLastEdit, T[] columnData = null)
        {
            UpdateColumnInfo();
            // 
            if (ignoreLastEdit == true)
            {
                for (int i = 0; i < _editIndex; i++)
                { 
                    _edits[i].ColumnAdded(columnIndex, columnData); 
                }
            }
            else
            {
                for (int i = 0; i <= _editIndex; i++)
                {
                    _edits[i].ColumnAdded(columnIndex, columnData); 
                }
            }
            // 
            ColumnsAdded?.Invoke(new[] { columnIndex });
        }

        /// <summary>
        /// Updates the internal state after multiple columns have been added.
        /// </summary>
        /// <param name="columnIndices">Indices of columns added.</param>
        /// <param name="columnData">Data for each added column.</param>
        private void ColumnAddsMade(int[] columnIndices, List<object[]> columnData)
        {
            UpdateColumnInfo();
            // 
            for (int i = 0; i < columnIndices.Length; i++)
            {
                for (int j = 0; j <= _editIndex; j++)
                { 
                    _edits[j].ColumnAdded(columnIndices[i], columnData[i]);
                }
            }
            // 
            ColumnsAdded?.Invoke(columnIndices);
        }

        /// <summary>
        /// Recomputes the internal row view-to-storage mapping after edits.
        /// </summary>
        private void UpdateRowInfo()
        {
            TableEdit edit;
            var newStored = new List<int>();
            for (int i = 0; i < _storedNumberOfRows; i++)
            { 
                newStored.Add(i); 
            }
            for (int i = 0; i <= _editIndex; i++)
            {
                edit = _edits[i];
                switch (edit.GetType())
                {
                    case var @case when @case == typeof(AddRowEdit):
                        {
                            newStored.Add(-1);
                            break;
                        }
                    case var case1 when case1 == typeof(AddRowsEdit):
                        {
                            for (int j = 0; j < ((AddRowsEdit)edit).AddRowEdits.Length; j++)
                                newStored.Add(-1);
                            break;
                        }
                    case var case2 when case2 == typeof(DeleteRowEdit):
                        {
                            newStored.RemoveAt(((DeleteRowEdit)edit).RowIndex);
                            break;
                        }
                    case var case3 when case3 == typeof(DeleteRowsEdit):
                        {
                            {
                                var withBlock = (DeleteRowsEdit)edit;
                                var rowsdeleted = new int[(withBlock.DeleteRowEdits.Length)];
                                for (int j = 0; j < withBlock.DeleteRowEdits.Length; j++)
                                { 
                                    rowsdeleted[j] = withBlock.DeleteRowEdits[j].RowIndex; 
                                }
                                Array.Sort(rowsdeleted);
                                for (int j = rowsdeleted.Length - 1; j >= 0; j -= 1)
                                { 
                                    newStored.RemoveAt(rowsdeleted[j]); 
                                }
                            }

                            break;
                        }
                }
            }
            // 
            _viewToStoredRowIndex = newStored.ToArray();
        }

        /// <summary>
        /// Recomputes the internal compute view-to-storage mapping after edits.
        /// </summary>
        private void UpdateColumnInfo()
        {
            var newColumnNames = GetStoredColumnNames().ToList();
            var newColumnTypes = GetStoredColumnTypes().ToList();
            TableEdit edit;
            var newStored = new List<int>();
            for (int i = 0; i < newColumnNames.Count; i++)
            { 
                newStored.Add(i); 
            }
            // hidden columns
            for (int i = newColumnNames.Count - 1; i >= 0; i -= 1)
            {
                if (_hiddenColumns.Contains(newColumnNames[i]))
                {
                    newColumnNames.RemoveAt(i);
                    newColumnTypes.RemoveAt(i);
                    newStored.RemoveAt(i);
                }
            }
            // edits
            for (int i = 0; i <= _editIndex; i++)
            {
                edit = _edits[i];
                if (edit is IColumnEdit)
                {
                    if (((IColumnEdit)edit).IsColumnAdd == true)
                    {
                        newColumnNames.Add(((IColumnEdit)edit).ColumnName);
                        newColumnTypes.Add(((IColumnEdit)edit).ColumnDataType);
                        newStored.Add(-1);
                    }
                }
                // 
                switch (edit.GetType())
                {
                    case var @case when @case == typeof(AddColumnsEdit):
                        {
                            foreach (IColumnEdit editColumn in ((AddColumnsEdit)edit).AddColumnEdits)
                            {
                                newColumnNames.Add(editColumn.ColumnName);
                                newColumnTypes.Add(editColumn.ColumnDataType);
                                newStored.Add(-1);
                            }

                            break;
                        }
                    case var case1 when case1 == typeof(DeleteColumnEdit):
                        {
                            newColumnNames.RemoveAt(((DeleteColumnEdit)edit).ColumnIndex);
                            newColumnTypes.RemoveAt(((DeleteColumnEdit)edit).ColumnIndex);
                            newStored.RemoveAt(((DeleteColumnEdit)edit).ColumnIndex);
                            break;
                        }
                    case var case2 when case2 == typeof(DeleteColumnsEdit):
                        {
                            {
                                var withBlock = (DeleteColumnsEdit)edit;
                                for (int j = withBlock.ColumnsDeleted.Length - 1; j >= 0; j -= 1)
                                {
                                    newColumnNames.RemoveAt(withBlock.ColumnsDeleted[j].ColumnIndex);
                                    newColumnTypes.RemoveAt(withBlock.ColumnsDeleted[j].ColumnIndex);
                                    newStored.RemoveAt(withBlock.ColumnsDeleted[j].ColumnIndex);
                                }
                            }

                            break;
                        }
                }
            }
            // 
            // 
            _viewToStoredColumnIndex = newStored.ToArray();
            _columnNames = newColumnNames.ToArray();
            _columnTypes = newColumnTypes.ToArray();
        }
        #endregion

        #region Get view data

        /// <summary>
        /// Retrieves the value of a specific cell, applying any edits if present.
        /// </summary>
        /// <param name="columnIndex">Zero-based column index in the view.</param>
        /// <param name="rowIndex">Zero-based row index in the view.</param>
        /// <returns>The current value of the specified cell.</returns>
        public object GetCell(int columnIndex, int rowIndex)
        {
            if (_editIndex < 0)
            {
                return GetStoredCell(columnIndex, rowIndex);
            }
            object result = null;
            for (int i = _editIndex; i >= 0; i -= 1)
            {
                if (_edits[i].ContainsCell(columnIndex, rowIndex, ref result))
                {
                    return result;
                }
            }
            //
            // Check if column and row exist in stored data (newly added columns/rows have index -1)
            if (_viewToStoredColumnIndex[columnIndex] >= 0 && _viewToStoredColumnIndex[columnIndex] < _storedColumnNames.Length &&
                _viewToStoredRowIndex[rowIndex] >= 0 && _viewToStoredRowIndex[rowIndex] < _storedNumberOfRows)
            {
                return GetStoredCell(_viewToStoredColumnIndex[columnIndex], _viewToStoredRowIndex[rowIndex]);
            }
            // Column or row doesn't exist in stored data (newly added with no edits)
            return null;
        }

        /// <summary>
        /// Retrieves the value of a specific cell using the column name.
        /// </summary>
        /// <param name="columnName">Name of the column.</param>
        /// <param name="rowIndex">Row index in the view.</param>
        /// <returns>The current value of the specified cell.</returns>
        public object GetCell(string columnName, int rowIndex)
        {
            if (_editIndex < 0)
            {
                return GetStoredCell(columnName, rowIndex);
            }
            int columnIndex = Array.IndexOf(_columnNames, columnName);
            if (columnIndex < 0) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            return GetCell(columnIndex, rowIndex);
        }

        /// <summary>
        /// Retrieves the values of multiple cells based on column and row indices.
        /// </summary>
        /// <param name="columnIndices">Array of column indices.</param>
        /// <param name="rowIndices">Array of row indices.</param>
        /// <returns>An array of cell values corresponding to the specified column and row index pairs.</returns>
        public object[] GetCells(int[] columnIndices, int[] rowIndices)
        {
            if (_editIndex < 0)
            { 
                return GetStoredCells(columnIndices, rowIndices); 
            }
            var result = new object[(columnIndices.Length)];
            for (int i = 0; i < columnIndices.Length; i++)
            { 
                result[i] = GetCell(columnIndices[i], rowIndices[i]);
            }
            return result;
        }

        /// <summary>
        /// Retrieves the values of a full row.
        /// </summary>
        /// <param name="rowIndex">Index of the row in the view.</param>
        /// <returns>Array of cell values in the row.</returns>
        public object[] GetRow(int rowIndex)
        {
            if (_editIndex < 0)
            {
                return GetStoredRow(rowIndex); 
            }
            // Get the stored data
            var result = new object[(_columnNames.Length)];
            if (_viewToStoredRowIndex[rowIndex] < _storedNumberOfRows && _viewToStoredRowIndex[rowIndex] >= 0)
            {
                object[] storedrow = GetStoredRow(_viewToStoredRowIndex[rowIndex]);
                for (int i = 0; i < _viewToStoredColumnIndex.Length; i++)
                {
                    if (_viewToStoredColumnIndex[i] < _storedColumnNames.Length && _viewToStoredColumnIndex[i] >= 0)
                    {
                        result[i] = storedrow[_viewToStoredColumnIndex[i]];
                    }
                }
            }
            // update with edits made
            for (int i = 0; i <= _editIndex; i++)
            {
                foreach (CellEdit edit in _edits[i].GetEditedCellsInRow(rowIndex))
                { 
                    result[edit.ColumnIndex] = edit.Value;
                }
            }
            // 
            return result;
        }

        /// <summary>
        /// Retrieves selected columns from a specific row.
        /// </summary>
        /// <param name="rowIndex">Row index in the view.</param>
        /// <param name="columnIndices">Indices of columns to retrieve.</param>
        /// <returns>Array of values from specified columns in the row.</returns>
        public object[] GetRow(int rowIndex, int[] columnIndices)
        {
            if (_editIndex < 0)
            { 
                return GetStoredRow(rowIndex, columnIndices);
            }
            // 
            object[] allColumnsResult = GetRow(rowIndex);
            var result = new object[(columnIndices.Length)];
            for (int i = 0; i < columnIndices.Length; i++)
            { 
                result[i] = allColumnsResult[columnIndices[i]]; 
            }
            return result;
        }

        /// <summary>
        /// Retrieves a row with specified column names.
        /// </summary>
        /// <param name="rowIndex">Index of the row in the view.</param>
        /// <param name="columns">Array of column names to retrieve values for.</param>
        /// <returns>Array of values from specified columns.</returns>
        public object[] GetRow(int rowIndex, string[] columns)
        {
            if (_editIndex < 0)
            {
                return GetStoredRow(rowIndex, columns); 
            }
            // 
            var columnIndices = new int[(columns.Length)];
            for (int i = 0; i < columns.Length; i++)
            { 
                columnIndices[i] = Array.IndexOf(_columnNames, columns[i]); 
            }
            return GetRow(rowIndex, columnIndices);
        }

        /// <summary>
        /// Retrieves multiple rows from the view.
        /// </summary>
        /// <param name="startRowIndex">Starting row index.</param>
        /// <param name="endRowIndex">Ending row index.</param>
        /// <returns>List of object arrays representing each row.</returns>
        public List<object[]> GetRows(int startRowIndex, int endRowIndex)
        {
            if (_editIndex < 0)
            { 
                return GetStoredRows(startRowIndex, endRowIndex);
            }
            // 
            var result = new List<object[]>();
            for (int i = startRowIndex; i <= endRowIndex; i++)
            { 
                result.Add(GetRow(i)); 
            }
            return result;
        }

        /// <summary>
        ///     returns a specified field of data from the data table as an array of the column type.
        /// </summary>
        /// <param name="columnIndex">Column index of the desired data.</param>
        /// <returns>A one-dimensional array of the column type.</returns>
        /// <remarks></remarks>
        public object[] GetColumn(int columnIndex)
        {
            if (_editIndex < 0)
            { 
                return GetStoredColumn(columnIndex);
            }
            var result = new object[_nRows];
            if (_viewToStoredColumnIndex[columnIndex] >= 0 && _viewToStoredColumnIndex[columnIndex] < _storedColumnNames.Length)
            {
                object[] storedcolumn = GetStoredColumn(_viewToStoredColumnIndex[columnIndex]);
                for (int i = 0; i < _viewToStoredRowIndex.Length; i++)
                {
                    if (_viewToStoredRowIndex[i] >= 0 && _viewToStoredRowIndex[i] < _storedNumberOfRows)
                    {
                        result[i] = storedcolumn[_viewToStoredRowIndex[i]];
                    }
                }
            }
            // 
            for (int i = 0; i <= _editIndex; i++)
            {
                foreach (CellEdit edit in _edits[i].GetEditedCellsInColumn(columnIndex))
                { 
                    result[edit.RowIndex] = edit.Value; 
                }
            }
            // 
            return result;
        }

        /// <summary>
        ///     returns a specified field of data from the data table as an array of the column type.
        /// </summary>
        /// <param name="columnName">Name of the desired column of data.</param>
        /// <returns>A one-dimensional array of the column type.</returns>
        /// <remarks></remarks>
        public object[] GetColumn(string columnName)
        {
            if (_editIndex < 0)
            {
                return GetStoredColumn(columnName);
            }
            int columnIndex = Array.IndexOf(_columnNames, columnName);
            if (columnIndex < 0) throw new ArgumentException($"Column '{columnName}' not found.", nameof(columnName));
            return GetColumn(columnIndex);
        }

        #endregion

        #region Get data from the stored database

        /// <summary>
        /// Gets the number of rows in the underlying stored database.
        /// </summary>
        /// <returns>Total number of stored rows.</returns>
        protected abstract ulong GetStoredRowCount();

        /// <summary>
        /// Gets the names of columns in the underlying stored database.
        /// </summary>
        /// <returns>The total of stored rows.</returns>
        protected abstract string[] GetStoredColumnNames();

        /// <summary>
        /// Gets the data types of columns in the stored database.
        /// </summary>
        /// <returns>An array of column types.</returns>
        protected abstract Type[] GetStoredColumnTypes();

        /// <summary>
        /// Retrieves a cell value from the stored database using column and row indices.
        /// </summary>
        /// <param name="storedColumnIndex">Index of the column in the stored database.</param>
        /// <param name="storedRowIndex">Index of the row in the stored database.</param>
        /// <returns>The stored cell value.</returns>
        protected abstract object GetStoredCell(int storedColumnIndex, int storedRowIndex);

        /// <summary>
        /// Retrieves a cell value form the stored database using column name and row index.
        /// </summary>
        /// <param name="storedColumnName">Name of the column in the stored database.</param>
        /// <param name="storedRowIndex">Index of the row in the stored database.</param>
        /// <returns>The stored cell value.</returns>
        protected abstract object GetStoredCell(string storedColumnName, int storedRowIndex);

        /// <summary>
        /// Retrieves multiple cell values from the stored database.
        /// </summary>
        /// <param name="storedColumnIndices">Array of column indices.</param>
        /// <param name="storedRowIndices">Array of row indices.</param>
        /// <returns>Array of stored cell values.</returns>
        protected abstract object[] GetStoredCells(int[] storedColumnIndices, int[] storedRowIndices);

        /// <summary>
        /// Retrieves an entire row from the stored database.
        /// </summary>
        /// <param name="storedRowIndex">Index of the row to retrieve.</param>
        /// <returns>Array of value from specified columns in the row.</returns>
        protected abstract object[] GetStoredRow(int storedRowIndex);

        /// <summary>
        /// Retrieves selected columns from a specific row in the stored database.
        /// </summary>
        /// <param name="storedRowIndex">Index of the row to retrieve.</param>
        /// <param name="storedColumnIndices">Indices of columns to retrieve.</param>
        /// <returns>Array of values from specified columns in the row.</returns>
        protected abstract object[] GetStoredRow(int storedRowIndex, int[] storedColumnIndices);

        /// <summary>
        /// Retrieves a row from the stored database using column names.
        /// </summary>
        /// <param name="storedRowIndex">Index of the row to retrieve.</param>
        /// <param name="storedColumns">Array of column names to retrieve.</param>
        /// <returns>Array of values from the specified columns in the row.</returns>
        protected abstract object[] GetStoredRow(int storedRowIndex, string[] storedColumns);

        /// <summary>
        /// Retrieves multiple rows from the stored database.
        /// </summary>
        /// <param name="startStoredRowIndex">Starting row index (inclusive).</param>
        /// <param name="endStoredRowIndex">Ending row index (inclusive).</param>
        /// <returns>A list of object arrays, where each array represents a row of data.</returns>
        protected abstract List<object[]> GetStoredRows(int startStoredRowIndex, int endStoredRowIndex);

        /// <summary>
        ///     returns a specified field of data from the data table as an array of the column type.
        /// </summary>
        /// <param name="storedColumnIndex">Column index of the desired data.</param>
        /// <returns>A one-dimensional array of the column type.</returns>
        /// <remarks>Written 9/8/2012 by Woodrow Lee Fields.</remarks>
        protected abstract object[] GetStoredColumn(int storedColumnIndex);

        /// <summary>
        ///     returns a specified field of data from the data table as an array of the column type.
        /// </summary>
        /// <param name="storedColumnName">Name of the desired column of data.</param>
        /// <returns>A one-dimensional array of the column type.</returns>
        /// <remarks>Written 9/8/2012 by Woodrow Lee Fields.</remarks>
        protected abstract object[] GetStoredColumn(string storedColumnName);
        #endregion

        #region Add data to stored database

        /// <summary>
        /// Adds a new column with byte[][] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The byte[][] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, byte[][] columnData);
        
        /// <summary>
        /// Adds a new column with byte[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The byte[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, byte[] columnData);

        /// <summary>
        /// Adds a new column with short[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The short[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, short[] columnData);

        /// <summary>
        /// Adds a new column with int[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The int[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, int[] columnData);

        /// <summary>
        /// Adds a new column with long[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The long[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, long[] columnData);

        /// <summary>
        /// Adds a new column with float[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The float[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, float[] columnData);

        /// <summary>
        /// Adds a new column with double[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The double[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, double[] columnData);

        /// <summary>
        /// Adds a new column with string[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The string[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, string[] columnData);

        /// <summary>
        /// Adds a new column with bool[] data to the database.
        /// </summary>
        /// <param name="columnName">The name of the new column.</param>
        /// <param name="columnData">The bool[] data for the column.</param>
        protected abstract void AddColumnToDatabase(string columnName, bool[] columnData);

        /// <summary>
        /// Adds a new row to the database using provided values.
        /// </summary>
        /// <param name="row">The values for the new row.</param>
        protected abstract void AddRowToDatabase(object[] row);

        /// <summary>
        /// Adds a new empty row to the database.
        /// </summary>
        protected abstract void AddRowToDatabase();

        /// <summary>
        /// Adds multiple rows to the database.
        /// </summary>
        /// <param name="newRowData">List of object arrays representing new rows.</param>
        protected abstract void AddRowsToDatabase(List<object[]> newRowData);

        /// <summary>
        /// Adds multiple rows from a DataTable to the database.
        /// </summary>
        /// <param name="newRowData">A DataTable whose rows will be added to the database.</param>
        protected void AddRowsToDatabase(DataTable newRowData)
        {
            var rowsAdded = new List<object[]>(newRowData.Rows.Count);
            for (int i = 0; i < newRowData.Rows.Count; i++)
            { 
                rowsAdded.Add(newRowData.Rows[i].ItemArray); 
            }
            AddRowsToDatabase(rowsAdded);
        }
        #endregion

        #region Delete data from stored database

        /// <summary>
        /// Deletes a single column from the stored database.
        /// </summary>
        /// <param name="columnName">The name of the column to delete.</param>
        protected abstract void DeleteColumnFromDatabase(string columnName);

        /// <summary>
        /// Deletes multiple columns from the stored database.
        /// </summary>
        /// <param name="columnsToDelete">Array of columns to delete.</param>
        protected abstract void DeleteColumnsFromDatabase(string[] columnsToDelete);
        
        /// <summary>
        /// Deletes a single row from the stored database.
        /// </summary>
        /// <param name="rowIndex">The index of the row to delete.</param>
        protected abstract void DeleteRowFromDatabase(int rowIndex);

        /// <summary>
        /// Deletes multiple rows from the stored database.
        /// </summary>
        /// <param name="rowIndices">Array of row indices to delete.</param>
        protected abstract void DeleteRowsFromDatabase(int[] rowIndices);
        #endregion

        #region Edit data in stored database

        /// <summary>
        /// Edits an entire column in the database with byte[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New byte[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, byte[] columnData);

        /// <summary>
        /// Edits an entire column in the database with short[] values.
        /// </summary>
        /// <param name="columnName">The column name to update</param>
        /// <param name="columnData">New short[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, short[] columnData);

        /// <summary>
        /// Edits an entire column in the database with int[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New int[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, int[] columnData);

        /// <summary>
        /// Edits an entire column in the database with long[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New long[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, long[] columnData);

        /// <summary>
        /// Edits an entire column in the database with float[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New float[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, float[] columnData);

        /// <summary>
        /// Edits an entire column in the database with double[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New double[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, double[] columnData);

        /// <summary>
        /// Edits an entire column in the database with string[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New string[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, string[] columnData);

        /// <summary>
        /// Edits an entire column in the database with bool[] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New bool[] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, bool[] columnData);

        /// <summary>
        /// Edits an entire column in the database with byte[][] values.
        /// </summary>
        /// <param name="columnName">The column name to update.</param>
        /// <param name="columnData">New byte[][] column data.</param>
        protected abstract void EditDatabaseColumn(string columnName, byte[][] columnData);

        /// <summary>
        /// Edits a specific cell in the database with a byte value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, byte cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a short value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, short cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a int value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, int cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a long value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, long cellValue);
        
        /// <summary>
        /// Edits a specific cell in the database with a float value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, float cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a double value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, double cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a string value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, string cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a bool value.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, bool cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a byte array.
        /// </summary>
        /// <param name="columnIndex"></param>
        /// <param name="rowIndex"></param>
        /// <param name="cellValue"></param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, byte[] cellValue);

        /// <summary>
        /// Edits a specific cell in the database with a DateTime value.
        /// </summary>
        /// <param name="columnIndex">The index of the column to edit.</param>
        /// <param name="rowIndex">The index of the row to edit.</param>
        /// <param name="cellValue">The DateTime value to set.</param>
        protected abstract void EditDatabaseCell(int columnIndex, int rowIndex, DateTime cellValue);

        /// <summary>
        /// Edits multiple cells in the database using column and row indices. 
        /// </summary>
        /// <param name="columnIndices">Array of column indices to edit.</param>
        /// <param name="rowIndices">Array of row indices corresponding to each column index.</param>
        /// <param name="cellValues">Array of new cell values.</param>
        protected abstract void EditDatabaseCells(int[] columnIndices, int[] rowIndices, object[] cellValues);

        /// <summary>
        /// Edits multiple cells in the database using column names and row indices 
        /// </summary>
        /// <param name="columnNamesToEdit">Array of column names to edit.</param>
        /// <param name="rowIndices">Array of row indices.</param>
        /// <param name="cellValues">Array of new values to assign to the cells.</param>
        protected abstract void EditDatabaseCells(string[] columnNamesToEdit, int[] rowIndices, object[] cellValues);
       
        #endregion

        /// <summary>
        /// Searches for a string value in a column by name within a specified row range.
        /// </summary>
        /// <param name="startIndex">The starting index of the search.</param>
        /// <param name="endIndex">The ending index of the search.</param>
        /// <param name="columnName">The name of the column to search in.</param>
        /// <param name="searchValue">The string value to search in.</param>
        /// <param name="matchCase">Whether the search should be case-sensitive.</param>
        /// <param name="wholeWord">Whether to match the entire word exactly.</param>
        /// <returns>The row index where the match is found, or -1 if not found.</returns>
        public int SearchColumn(int startIndex, int endIndex, string columnName, string searchValue, bool matchCase, bool wholeWord)
        {
            if (_columnNames.Contains(columnName) == false)
            {
                return -1;
            }
            int loopStep = 1;
            string cellValue;
            // Avoid ToLower() — culture-dependent and allocates per-call. Use StringComparison
            // overloads of Contains/Equals so we never copy the string just to compare it.
            var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            if (startIndex > endIndex)
            {
                loopStep = -1;
            }
            object[] columnArray = GetColumn(columnName);
            if (columnArray.Length == 0)
            {
                return -1;
            }
            for (int i = startIndex; loopStep >= 0 ? i <= endIndex : i >= endIndex; i += loopStep)
            {
                cellValue = columnArray[i].ToString();
                if (wholeWord == false)
                {
                    if (cellValue != null && searchValue != null && cellValue.Contains(searchValue, comparison))
                    {
                        return i;
                    }
                }
                else if (string.Equals(searchValue ?? "", cellValue ?? "", comparison))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Searches for a string value in a column by index within a specified row range.
        /// </summary>
        /// <param name="startIndex">The starting index of the search.</param>
        /// <param name="endIndex">The ending index of the search.</param>
        /// <param name="columnIndex">The index of the column to search in.</param>
        /// <param name="searchValue">The string value to search for.</param>
        /// <param name="matchCase">Whether the search should be case-sensitive.</param>
        /// <param name="wholeWord">Whether to match the entire word exactly.</param>
        /// <returns>The row index where the value was found, or -1 if not found.</returns>
        public int SearchColumn(int startIndex, int endIndex, int columnIndex, string searchValue, bool matchCase, bool wholeWord)
        {
            if (columnIndex < 0 || columnIndex >= _columnNames.Length)
            { 
                return -1; 
            }
            return SearchColumn(startIndex, endIndex, _columnNames[columnIndex], searchValue, matchCase, wholeWord);
        }

        /// <summary>
        /// Gets a list of all numeric column names in the table.
        /// </summary>
        /// <returns>A list of column names with numeric data types.</returns>
        public List<string> GetNumericColumns()
        {
            var numericColumns = new List<string>();
            for (int i = 0; i < _columnNames.Length; i++)
            {
                if (DatabaseManager.IsNumericType(_columnTypes[i]))
                {
                    numericColumns.Add(_columnNames[i]);
                }
            }
            return numericColumns;
        }

        /// <summary>
        /// Exports the table data to a new or existing SQLite database file.
        /// </summary>
        /// <param name="filePath">The path to the SQLite database file. If the file does not exist, it will be created.</param>
        /// <param name="tableName">The name of the table to create in the SQLite database.</param>
        /// <param name="rowIndicesToExport">Optional array of row indices to export. If null, all rows are exported.</param>
        /// <param name="columnIndicesToExport">Optional array of column indices to export. If null, all columns are exported.</param>
        public void ExportToSqlite(string filePath, string tableName, int[] rowIndicesToExport = null, int[] columnIndicesToExport = null)
        {
            if (File.Exists(filePath) == false)
            {
                SQLiteManager.CreateSqLiteFile(filePath);
            }
            var sqliteDatabase = new SQLiteManager(filePath);
            if (sqliteDatabase.DataBaseOpen == false)
            { 
                sqliteDatabase.Open();
            }
            var dt = ExportToDataTable(rowIndicesToExport, columnIndicesToExport);
            dt.TableName = tableName;
            sqliteDatabase.SaveDataTable(dt);
            sqliteDatabase.Close();
        }

        /// <summary>
        ///  Exports a data table to a comma delimited text file.
        /// </summary>
        /// <param name="filePath">Output file path</param>
        /// <param name="rowIndicesToExport">Which rows to export as row indices</param>
        /// <param name="columnIndicesToExport">Which columns to export as column indices</param>
        /// <remarks></remarks>
        public void ExportToCsv(string filePath, int[] rowIndicesToExport = null, int[] columnIndicesToExport = null)
        {
            if (Path.GetExtension(filePath) != ".csv")
            { 
                throw new Exception("Supplied file path is not a comma delimited file (.csv)."); 
            }
            // 
            if (columnIndicesToExport == null)
            {
                columnIndicesToExport = new int[(_columnNames.Length)];
                for (int i = 0; i < _columnNames.Length; i++)
                { 
                    columnIndicesToExport[i] = i; 
                }
            }
            if (rowIndicesToExport == null)
            {
                rowIndicesToExport = new int[_nRows];
                for (int i = 0; i < _nRows; i++)
                { 
                    rowIndicesToExport[i] = i;
                }
            }
            using (var csvWriter = new StreamWriter(filePath))
            {
                csvWriter.Write(_columnNames[columnIndicesToExport[0]]);
                for (int i = 1; i < columnIndicesToExport.Length; i++)
                {
                    csvWriter.Write("," + _columnNames[columnIndicesToExport[i]]);
                }
                csvWriter.WriteLine();
                object[] row;
                for (int i = 0; i < rowIndicesToExport.Length; i++)
                {
                    row = GetRow(rowIndicesToExport[i], columnIndicesToExport);
                    csvWriter.Write(row[0]?.ToString() ?? string.Empty);
                    for (int j = 1; j < row.Length; j++)
                    {
                        csvWriter.Write("," + (row[j]?.ToString() ?? string.Empty));
                    }
                    csvWriter.WriteLine();
                }
            }
            // 
        }

        /// <summary>
        /// Exports the contents of a DataTable to an excel spreadsheet.
        /// FilePath must have an .xlsx or .xls extension.
        /// The name of the excel worksheet will be the name of the supplied DataTable
        /// </summary>
        public void ExportToXlsx(string filePath, int[] rowIndicesToExport = null, int[] columnIndicesToExport = null)
        {
            // This sub  will create a new excel worksheet and write the contents of the datatable to it.
            // If the FilePath given does not point to an already existing file, a new excel file will be created.
            // The name of the worksheet will be the "Name" attribute of the provided datatable.

            if (Path.GetExtension(filePath) != ".xlsx")
            {
                throw new Exception("Supplied file path is not an excel spreadsheet file (.xlsx)."); 
            }

            // if the file exists, check to see if the spreadsheet is open
            if (File.Exists(filePath))
            {
                using (var wb = new ClosedXML.Excel.XLWorkbook(filePath))
                {
                    //strings are immutable, so this is ok
                    string nameOfTable = _tableName;
                    int counter = 1;
                    do
                    {
                        if (wb.Worksheets.Any(o => (o.Name ?? "") == (nameOfTable ?? "")))
                        {
                            nameOfTable = nameOfTable + "_" + counter;
                            counter += 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    while (true);
                    // 
                    var ws = wb.Worksheets.Add(nameOfTable);
                    var headerRange = ws.Cell(1, 1).InsertData(_columnNames, true);
                    var dataRange = ws.Cell(2, 1).InsertData(ExportToDataTable(rowIndicesToExport, columnIndicesToExport));
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.Black;
                    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(225, 240, 250);
                    headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                    headerRange.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
                    headerRange.Style.Border.BottomBorderColor = ClosedXML.Excel.XLColor.FromArgb(125, 140, 150);
                    headerRange.SetAutoFilter(true);
                    ws.Columns(1, _columnNames.Length).AdjustToContents();
                    // 
                    wb.SaveAs(filePath);
                }
            }
            else
            {
                using (var wb = new ClosedXML.Excel.XLWorkbook())
                {
                    var ws = wb.Worksheets.Add(_tableName);
                    var headerRange = ws.Cell(1, 1).InsertData(_columnNames, true);
                    var dataRange = ws.Cell(2, 1).InsertData(ExportToDataTable(rowIndicesToExport, columnIndicesToExport));
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.Black;
                    headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromArgb(225, 240, 250);
                    headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Left;
                    headerRange.Style.Border.BottomBorder = ClosedXML.Excel.XLBorderStyleValues.Medium;
                    headerRange.Style.Border.BottomBorderColor = ClosedXML.Excel.XLColor.FromArgb(125, 140, 150);
                    headerRange.SetAutoFilter(true);
                    ws.Columns(1, _columnNames.Length).AdjustToContents();
                    // 
                    wb.SaveAs(filePath);
                }
            }
        }

        /// <summary>
        /// Exports the specified subset of data to a System.Data.DataTable.
        /// </summary>
        /// <param name="rowIndicesToExport">Optional row indices to include.</param>
        /// <param name="columnIndicesToExport">Optional column indices to include.</param>
        /// <returns>A populated DataTable containing the selected data.</returns>
        public DataTable ExportToDataTable(int[] rowIndicesToExport = null, int[] columnIndicesToExport = null)
        {
            if (columnIndicesToExport == null)
            {
                columnIndicesToExport = new int[(_columnNames.Length)];
                for (int i = 0; i < _columnNames.Length; i++)
                { 
                    columnIndicesToExport[i] = i; 
                }
            }
            if (rowIndicesToExport == null)
            {
                rowIndicesToExport = new int[_nRows];
                for (int i = 0; i < _nRows; i++)
                { 
                    rowIndicesToExport[i] = i;
                }
            }
            // 
            var dt = new DataTable();
            for (int i = 0; i < columnIndicesToExport.Length; i++)
            {
                if (dt.Columns.Contains(_columnNames[columnIndicesToExport[i]]))
                {
                    int sameNameCounter = 1;
                    string newColumnName = _columnNames[columnIndicesToExport[i]] + sameNameCounter;
                    while (dt.Columns.Contains(newColumnName) != false)
                    {
                        sameNameCounter += 1;
                        newColumnName = _columnNames[columnIndicesToExport[i]] + sameNameCounter;
                    }
                    dt.Columns.Add(newColumnName, _columnTypes[columnIndicesToExport[i]]);
                }
                else
                {
                    dt.Columns.Add(_columnNames[columnIndicesToExport[i]], _columnTypes[columnIndicesToExport[i]]);
                }
            }

            bool wasOpen = _parentDatabase.DataBaseOpen;
            if (_parentDatabase.DataBaseOpen == false)
            {
                _parentDatabase.Open(); 
            }
            for (int i = 0; i < rowIndicesToExport.Length; i++)
            { 
                dt.Rows.Add(GetRow(rowIndicesToExport[i], columnIndicesToExport));
            }
            if (wasOpen == false)
            {
                _parentDatabase.Close();
            }
            // 
            return dt;
        }

        /// <summary>
        /// Exports the data table to a DBF (dBASE IV) file format.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="rowIndicesToExport"></param>
        /// <param name="columnIndicesToExport"></param>
        /// <exception cref="Exception"></exception>
        public void ExportToDbf(string filePath, int[] rowIndicesToExport = null, int[] columnIndicesToExport = null)
        {
            if (Path.GetExtension(filePath) != ".dbf")
            { 
                throw new Exception("Supplied file path is not a database file (.dbf).");
            }
            if (columnIndicesToExport == null)
            {
                columnIndicesToExport = new int[(_columnNames.Length)];
                for (int i = 0; i < _columnNames.Length; i++)
                { 
                    columnIndicesToExport[i] = i; 
                }
            }
            if (rowIndicesToExport == null)
            {
                rowIndicesToExport = new int[_nRows];
                for (int i = 0; i < _nRows; i++)
                { 
                    rowIndicesToExport[i] = i;
                }
            }
            // only include DBF fields that are supported
            var filteredColumnsToExport = new List<int>();
            for (int i = 0; i < columnIndicesToExport.Length; i++)
            {
                switch (_columnTypes[i])
                {
                    case var @case when @case == typeof(double):
                    case var case1 when case1 == typeof(float):
                    case var case2 when case2 == typeof(int):
                    case var case3 when case3 == typeof(uint):
                    case var case4 when case4 == typeof(short):
                    case var case5 when case5 == typeof(ushort):
                    case var case6 when case6 == typeof(byte):
                    case var case7 when case7 == typeof(bool):
                    case var case8 when case8 == typeof(string):
                        {
                            filteredColumnsToExport.Add(columnIndicesToExport[i]);
                            break;
                        }
                }
            }
            // Gotta Start the DBF
            var dt = new DataTable();
            for (int i = 0; i < filteredColumnsToExport.Count; i++)
            {
                if (dt.Columns.Contains(_columnNames[filteredColumnsToExport[i]]))
                {
                    int sameNameCounter = 1;
                    string newColumnName = _columnNames[filteredColumnsToExport[i]] + sameNameCounter;
                    while (dt.Columns.Contains(newColumnName) != false)
                    {
                        sameNameCounter += 1;
                        newColumnName = _columnNames[filteredColumnsToExport[i]] + sameNameCounter;
                    }
                    dt.Columns.Add(newColumnName, _columnTypes[filteredColumnsToExport[i]]);
                }
                else
                {
                    dt.Columns.Add(_columnNames[filteredColumnsToExport[i]], _columnTypes[filteredColumnsToExport[i]]);
                }
            }
            dt.Rows.Add(GetRow(rowIndicesToExport[0], filteredColumnsToExport.ToArray()));
            if (File.Exists(filePath))
            { 
                File.Delete(filePath); 
            }
            try
            {
                DbfReader.CreateDbf(filePath, dt);
                var outputDbf = new DbfReader(filePath);
                var outputDbft = outputDbf.GetTableManager(Path.GetFileNameWithoutExtension(filePath));
                for (int i = 1; i < rowIndicesToExport.Length; i++)
                {
                    outputDbft.AddRow(GetRow(rowIndicesToExport[i], filteredColumnsToExport.ToArray())); 
                }
                outputDbft.ApplyEdits();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred during export." + Environment.NewLine + ex.Message);
            }
        }

        /// <summary>
        /// Parses a double using the current culture, then invariant culture.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="result">The parsed double value.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Current culture matches user-entered values; invariant culture keeps
        /// cross-machine data such as US-format CSV values readable.
        /// </remarks>
        private static bool TryParseDoubleDualCulture(string text, out double result)
        {
            return double.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                || double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Parses a single-precision value using the current culture, then invariant culture.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <param name="result">The parsed single-precision value.</param>
        /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
        private static bool TryParseSingleDualCulture(string text, out float result)
        {
            return float.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out result)
                || float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        /// <summary>
        /// Converts the input value to the specified type.
        /// </summary>
        /// <param name="columnType">The target <see cref="Type"/> to convert the value to.</param>
        /// <param name="value">The value to convert. Modified in place with the converted value.</param>
        /// <returns><c>true</c> if the conversion was successful; otherwise, <c>false</c>.</returns>
        public static bool ConvertToColumnType(Type columnType, ref object value)
        {
            if (value is DBNull)
            { 
                return true; 
            }
            if (value is null)
            {
                value = DBNull.Value;
                return true;
            }
            switch (columnType)
            {
                case var @case when @case == typeof(double):
                    {
                        if (value.GetType() != typeof(double))
                        {
                            double test;
                            if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                            { 
                                return false; 
                            }
                            value = test;
                        }

                        break;
                    }
                case var case1 when case1 == typeof(float):
                    {
                        if (value.GetType() != typeof(float))
                        {
                            float test;
                            if (TryParseSingleDualCulture(value.ToString(), out test) == false)
                            { 
                                return false; 
                            }
                            value = test;
                        }

                        break;
                    }
                case var case2 when case2 == typeof(long):
                    {
                        double test;
                        long test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false; 
                        }
                        try
                        {
                            test2 = Convert.ToInt64(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case3 when case3 == typeof(ulong):
                    {
                        double test;
                        ulong test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false;
                        }
                        try
                        {
                            test2 = Convert.ToUInt64(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case4 when case4 == typeof(int):
                    {
                        double test;
                        int test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false;
                        }
                        try
                        {
                            test2 = Convert.ToInt32(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case5 when case5 == typeof(uint):
                    {
                        double test;
                        uint test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false; 
                        }
                        try
                        {
                            test2 = Convert.ToUInt32(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case6 when case6 == typeof(short):
                    {
                        double test;
                        short test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        {
                            return false; 
                        }
                        try
                        {
                            test2 = Convert.ToInt16(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case7 when case7 == typeof(ushort):
                    {
                        double test;
                        ushort test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false; 
                        }
                        try
                        {
                            test2 = Convert.ToUInt16(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case8 when case8 == typeof(byte):
                    {
                        double test;
                        byte test2;
                        if (TryParseDoubleDualCulture(value.ToString(), out test) == false)
                        { 
                            return false;
                        }
                        try
                        {
                            test2 = Convert.ToByte(test);
                        }
                        catch
                        {
                            return false;
                        }
                        value = test2;
                        break;
                    }
                case var case9 when case9 == typeof(bool):
                    {
                        bool test;
                        if (bool.TryParse(value.ToString(), out test) == false)
                        {
                            test = value.ToString() == "1";
                        }
                        value = test;
                        break;
                    }
                case var case10 when case10 == typeof(byte[]):
                    {
                        if (value.GetType() != typeof(byte[]))
                        {
                            // Use System.Text.Json for modern, secure serialization (backwards compatible with VB BinaryFormatter behavior)
                            value = JsonSerializer.SerializeToUtf8Bytes(value, value.GetType());
                        }
                        return true;
                    }
                case var case11 when case11 == typeof(string):
                    {
                        if (value == null)
                        {
                            value = "";
                        }
                        else
                        {
                            value = value.ToString();
                        }

                        break;
                    }
                    // If String.IsNullOrEmpty(value) = True Then Return True
                    // If value.GetType <> GetType(String) Then Return False
                    // value = value.ToString
                    // .tostring is sufficient for the majority of cases.

            }
            return true;
        }

        /// <summary>
        /// Creates a default value for the specified type. Returns an empty string for string types and zero for numeric types.
        /// </summary>
        /// <param name="columnType">The <see cref="Type"/> to get the default value for.</param>
        /// <returns>The default value for the specified type.</returns>
        public static object GetDefaultFromType(Type columnType)
        {
            switch (columnType)
            {
                case var @case when @case == typeof(double):
                    {
                        return 0d;
                    }
                case var case1 when case1 == typeof(float):
                    {
                        return 0f;
                    }
                case var case2 when case2 == typeof(long):
                case var case3 when case3 == typeof(ulong):
                    {
                        return 0L;
                    }
                case var case4 when case4 == typeof(int):
                case var case5 when case5 == typeof(uint):
                    {
                        return 0;
                    }
                case var case6 when case6 == typeof(short):
                case var case7 when case7 == typeof(ushort):
                    {
                        return (short)0;
                    }
                case var case8 when case8 == typeof(byte):
                    {
                        return (byte)0;
                    }
                case var case9 when case9 == typeof(bool):
                    {
                        return false;
                    }
                case var case10 when case10 == typeof(byte[]):
                    {
                        return new byte[] { };
                    }
                case var case11 when case11 == typeof(string):
                    {
                        return "";
                    }

                default:
                    {
                        // Throw rather than RETURN an exception object — previously the
                        // exception was used as a cell value, which crashed downstream
                        // type-validation with a stack trace far from the root cause.
                        throw new NotImplementedException(
                            $"GetDefaultFromType: no default value defined for column type '{columnType}'.");
                    }
            }
        }

        #region IDisposable

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources if needed.
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
