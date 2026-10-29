using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Represents an in-memory database reader that wraps a <see cref="DataTable"/> and provides
    /// a unified interface for table management through the <see cref="DatabaseManager"/> abstraction.
    /// </summary>
    public class InMemoryReader : DatabaseManager
    {
        private DataTable _table;

        /// <summary>
        /// Gets or sets the underlying <see cref="DataTable"/>. Setting it will clone the data into internal memory.
        /// </summary>
        public DataTable Table
        {
            get
            {
                return _table;
            }
            set
            {
                _table = value.Copy();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryReader"/> class using a given <see cref="DataTable"/>
        /// </summary>
        /// <param name="table">The source <see cref="DataTable"/> to wrap.</param>
        public InMemoryReader(DataTable table)
        {
            _table = table.Copy();
            _tableNames = GetTableNames();
            _dataBaseOpen = true;
        }
        
        /// <inheritdoc/>
        public override void Close()
        {
            _dataBaseOpen = false;
        }

        /// <inheritdoc/>
        public override void Open()
        {
            _dataBaseOpen = true;
        }

        /// <inheritdoc/>
        public override string[] GetTableNames()
        {
            return new string[] { _table.TableName };
        }

        /// <inheritdoc/>
        public override DataTableView GetTableManager(string tableName)
        {
            return new InMemoryTableReader(this, tableName);
        }

        /// <inheritdoc/>
        public override long GetStoredNumberOfRows(string tableName)
        {
            return _table.Rows.Count;
        }

        /// <inheritdoc/>
        public override int GetStoredNumberOfColumns(string tableName)
        {
            return _table.Columns.Count;
        }

        /// <summary>
        /// Provides a table-level data access editing capabilities for the in-memory DataTable.
        /// </summary>
        private class InMemoryTableReader : DataTableView
        {
            private readonly DataTable _table;

            /// <summary>
            /// Initializes a new instance of the InMemoryTableReader class with parent context and table name.
            /// </summary>
            /// <param name="inMemManager">Reference to the InMemoryReader parent.</param>
            /// <param name="dataTableName">Name of the table being managed.</param>
            public InMemoryTableReader(InMemoryReader inMemManager, string dataTableName)
            {
                _parentDatabase = inMemManager;
                _table = inMemManager.Table;
                _tableName = dataTableName;
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                _storedNumberOfRows = checked((int)GetStoredRowCount());
                InitializeView();
            }

            /// <inheritdoc/>
            protected override string[] GetStoredColumnNames()
            {
                var names = new string[_table.Columns.Count];
                for (int i = 0; i < names.Count(); i++)
                { 
                    names[i] = _table.Columns[i].ColumnName;
                }
                return names;
            }

            /// <inheritdoc/>
            protected override Type[] GetStoredColumnTypes()
            {
                var types = new Type[_table.Columns.Count];
                for (int i = 0; i < types.Count(); i++)
                { 
                    types[i] = _table.Columns[i].DataType;
                }
                return types;
            }

            /// <inheritdoc/>
            protected override ulong GetStoredRowCount()
            {
                return (ulong)_table.Rows.Count;
            }


            #region Column Stuff

            /// <inheritdoc/>
            protected override object[] GetStoredColumn(string storedColumnName)
            {
                var result = new object[_table.Rows.Count];
                for (int i = 0; i < _table.Rows.Count; i++)
                { 
                    result[i] = _table.Rows[i][storedColumnName];          
                }
                return result;
            }

            /// <inheritdoc/>
            protected override object[] GetStoredColumn(int storedColumnIndex)
            {
                var result = new object[_table.Rows.Count];
                for (int i = 0; i < _table.Rows.Count; i++)
                {
                    result[i] = _table.Rows[i][storedColumnIndex]; 
                }
                return result;
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, byte[][] columnData)
            {
                throw new NotImplementedException("table does not support storage of jagged byte arrays.");
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, byte[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                {
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }

                _table.Columns.Add(columnName, typeof(byte));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, double[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(double));
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i]; 
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, int[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(int));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, long[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(long));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, short[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(short));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, float[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(float));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, bool[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                {
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(bool));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, string[] columnData)
            {
                if (_table.Columns.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }

                _table.Columns.Add(columnName, typeof(string));
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i];
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void DeleteColumnFromDatabase(string columnName)
            {
                _table.Columns.Remove(columnName);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnsToDelete">The names of the columns to delete from the database.</param>
            protected override void DeleteColumnsFromDatabase(string[] columnsToDelete)
            {
                for (int i = 0; i < columnsToDelete.Count(); i++)
                {
                    _table.Columns.Remove(columnsToDelete[i]); 
                }
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, bool[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i]; 
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, byte[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name.");   
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][columnName] = columnData[i]; 
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, byte[][] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                {
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i]; 
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, short[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                for (int i = 0; i < columnData.Count(); i++)
                {
                    _table.Rows[i][columnName] = columnData[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, int[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, long[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                {
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i]; 
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, float[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, double[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name."); 
                }
                if (_table.Rows.Count != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, string[] columnData)
            {
                if (_table.Columns.Contains(columnName) == false)
                { 
                    throw new Exception("Column Name " + columnName + " does not exist, choose a different name.");
                }
                if (_table.Rows.Count != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][columnName] = columnData[i]; 
                }
            }
            #endregion

            #region Row Stuff

            /// <inheritdoc/>
            protected override void AddRowToDatabase(object[] row)
            {
                _table.Rows.Add(row);
            }

            /// <inheritdoc/>
            protected override void AddRowToDatabase()
            {
                _table.Rows.Add(_table.NewRow());
            }

            /// <inheritdoc/>
            /// <param name="newRowData"></param>
            protected override void AddRowsToDatabase(List<object[]> newRowData)
            {
                for (int i = 0; i < newRowData.Count; i++)
                    AddRowToDatabase(newRowData[i]);
            }

            /// <inheritdoc/>
            /// <param name="rowIndex">The index of the row to delete.</param>
            protected override void DeleteRowFromDatabase(int rowIndex)
            {
                _table.Rows[rowIndex].Delete();
                _table.AcceptChanges();
            }

            /// <inheritdoc/>
            /// <param name="rowIndices">The indices of the rows to delete.</param>
            protected override void DeleteRowsFromDatabase(int[] rowIndices)
            {
                Array.Sort(rowIndices);
                for (int i = rowIndices.Count() - 1; i >= 0; i -= 1)
                    _table.Rows[rowIndices[i]].Delete();
                _table.AcceptChanges();
            }

            /// <inheritdoc/>
            protected override object[] GetStoredRow(int storedRowIndex)
            {
                return _table.Rows[storedRowIndex].ItemArray;
            }

            /// <inheritdoc/>
            protected override object[] GetStoredRow(int storedRowIndex, string[] storedColumnNames)
            {
                var result = new object[(storedColumnNames.Count())];
                for (int i = 0; i < storedColumnNames.Count(); i++)
                { 
                    result[i] = _table.Rows[storedRowIndex][storedColumnNames[i]];
                }
                return result;
            }

            /// <inheritdoc/>
            protected override object[] GetStoredRow(int storedRowIndex, int[] storedColumnIndices)
            {
                var result = new object[(storedColumnIndices.Count())];
                for (int i = 0; i < storedColumnIndices.Count(); i++)
                { 
                    result[i] = _table.Rows[storedRowIndex][storedColumnIndices[i]];
                }
                return result;
            }

            /// <inheritdoc/>
            protected override List<object[]> GetStoredRows(int startStoredRowIndex, int endStoredRowIndex)
            {
                var result = new List<object[]>();
                for (int i = startStoredRowIndex; i <= endStoredRowIndex; i++)
                { 
                    result.Add(_table.Rows[i].ItemArray); 
                }
                return result;
            }
            #endregion

            #region Cell Stuff

            /// <inheritdoc/>
            /// <param name="columnsToEdit"></param>
            /// <param name="rowIndices"></param>
            /// <param name="cellValues"></param>
            protected override void EditDatabaseCells(string[] columnsToEdit, int[] rowIndices, object[] cellValues)
            {
                for (int i = 0; i < columnsToEdit.Count(); i++)
                { 
                    _table.Rows[rowIndices[i]][columnsToEdit[i]] = cellValues[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnIndices"></param>
            /// <param name="rowIndices"></param>
            /// <param name="cellValues"></param>
            protected override void EditDatabaseCells(int[] columnIndices, int[] rowIndices, object[] cellValues)
            {
                for (int i = 0; i < columnIndices.Count(); i++)
                { 
                    _table.Rows[rowIndices[i]][columnIndices[i]] = cellValues[i];
                }
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, bool cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, double cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, float cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, short cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, int cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, long cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, string cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte[] cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, DateTime cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <inheritdoc/>
            protected override object GetStoredCell(int storedColumnIndex, int storedRowIndex)
            {
                if (storedColumnIndex < 0 || storedColumnIndex >= _table.Columns.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedColumnIndex), storedColumnIndex,
                        $"Column index must be between 0 and {_table.Columns.Count - 1}.");
                }
                if (storedRowIndex < 0 || storedRowIndex >= _table.Rows.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_table.Rows.Count - 1}.");
                }
                return _table.Rows[storedRowIndex][storedColumnIndex];
            }

            /// <inheritdoc/>
            protected override object GetStoredCell(string storedColumnName, int storedRowIndex)
            {
                if (storedRowIndex < 0 || storedRowIndex >= _table.Rows.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_table.Rows.Count - 1}.");
                }
                return _table.Rows[storedRowIndex][storedColumnName];
            }

            /// <inheritdoc/>
            protected override object[] GetStoredCells(int[] storedColumnIndices, int[] storedRowIndices)
            {
                // Validate all indices before proceeding
                for (int i = 0; i < storedColumnIndices.Length; i++)
                {
                    if (storedColumnIndices[i] < 0 || storedColumnIndices[i] >= _table.Columns.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(storedColumnIndices),
                            $"Column index {storedColumnIndices[i]} at position {i} is out of range. Must be between 0 and {_table.Columns.Count - 1}.");
                    }
                }
                for (int i = 0; i < storedRowIndices.Length; i++)
                {
                    if (storedRowIndices[i] < 0 || storedRowIndices[i] >= _table.Rows.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(storedRowIndices),
                            $"Row index {storedRowIndices[i]} at position {i} is out of range. Must be between 0 and {_table.Rows.Count - 1}.");
                    }
                }

                var result = new object[(storedColumnIndices.Count())];
                for (int i = 0; i < storedColumnIndices.Count(); i++)
                {
                    result[i] = _table.Rows[storedRowIndices[i]][storedColumnIndices[i]];
                }
                return result.ToArray();
            }
            #endregion

        }


    }
}