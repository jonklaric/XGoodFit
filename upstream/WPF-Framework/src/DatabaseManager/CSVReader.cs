using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Provides functionality to read and manipulate CSV data as a table, inheriting from DatabaseManager.
    /// </summary>
    public class CsvReader : DatabaseManager
    {
        private readonly InMemoryReader _table;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvReader"/> class from a .csv file.
        /// </summary>
        /// <param name="filePath">Path to CSV file.</param>
        /// <param name="hasHeaders">Indicates whether the first line contains column headers.</param>
        /// <param name="dataLineStartIndex">Line index to start reading data (0-based).</param>
        /// <param name="fieldsEnclosedInQuotes">Whether fields are enclosed in quotes.</param>
        /// <exception cref="Exception">Thrown when the file is not a .csv.</exception>
        public CsvReader(string filePath, bool hasHeaders, int dataLineStartIndex, bool fieldsEnclosedInQuotes)
        {
            if (Path.GetExtension(filePath).ToLower() != ".csv") { throw new Exception("This is not a .csv file"); }

            _dataBasePath = filePath;
            var table = new DataTable(Path.GetFileNameWithoutExtension(filePath));
            using (var csvParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(filePath) { TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited })
            {
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = fieldsEnclosedInQuotes;
                string[] lineArray;
                lineArray = csvParser.ReadFields();
                if (hasHeaders == true)
                {
                    foreach (string header in lineArray)
                    {
                        if (table.Columns.Contains(header))
                        {
                            int counter = 1;
                            while (table.Columns.Contains(header + "_" + counter) != false)
                            {
                                counter += 1;
                            }
                            table.Columns.Add(header + "_" + counter, typeof(string));
                        }
                        else
                        {
                            table.Columns.Add(header, typeof(string));
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < lineArray.Count(); i++)
                    { 
                        table.Columns.Add("Column_" + (i + 1), typeof(string));
                    }
                }
                // 
                for (int i = 0; i < dataLineStartIndex; i++)
                { 
                    lineArray = csvParser.ReadFields(); 
                }

                while (!csvParser.EndOfData)
                {
                    if (lineArray.Count() != table.Columns.Count)
                    {
                        try
                        {
                            lineArray = csvParser.ReadFields();
                        }
                        catch (Microsoft.VisualBasic.FileIO.MalformedLineException)
                        {
                            // If the fields are enclosed in quotes then likely failed due to quotes within the quoted text
                            if (fieldsEnclosedInQuotes && !string.IsNullOrEmpty(csvParser.ErrorLine) && csvParser.ErrorLine.Length >= 2)
                            {
                                lineArray = csvParser.ErrorLine.Substring(1, csvParser.ErrorLine.Length - 2).Split(new[] { '"' + "," + '"' }, StringSplitOptions.None);
                            }
                        }
                        continue;
                    }
                    table.Rows.Add(lineArray);
                    try
                    {
                        lineArray = csvParser.ReadFields();
                    }
                    catch (Microsoft.VisualBasic.FileIO.MalformedLineException)
                    {
                        // If the fields are enclosed in quotes then likely failed due to quotes within the quoted text
                        if (fieldsEnclosedInQuotes && !string.IsNullOrEmpty(csvParser.ErrorLine) && csvParser.ErrorLine.Length >= 2)
                        {
                            lineArray = csvParser.ErrorLine.Substring(1, csvParser.ErrorLine.Length - 2).Split(new[] { '"' + "," + '"' }, StringSplitOptions.None);
                        }
                    }
                }
                // Get the last row if it is valid.
                if (lineArray.Count() == table.Columns.Count)
                { 
                    table.Rows.Add(lineArray); 
                }
            }
            _table = new InMemoryReader(table);
            _tableNames = GetTableNames();
        }

        /// <summary>
        /// Opens the connection or resource. Not applicable to CSV.
        /// </summary>
        public override void Open()
        {
            // 
        }

        /// <summary>
        /// Closes the connection or resource. Not applicable to CSV.
        /// </summary>
        public override void Close()
        {
            // 
        }

        /// <summary>
        /// Gets all available table names. CSV only supports one table.
        /// </summary>
        /// <returns>An array containing the table name.</returns>
        public override string[] GetTableNames()
        {
            return _table.GetTableNames();
        }

        /// <summary>
        /// Returns a <see cref="DataTableView"/> to manage the given table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>A <see cref="DataTableView"/> instance for the specified table.</returns>
        public override DataTableView GetTableManager(string tableName)
        {
            return new CsvTableReader(this, _table, tableName);
        }

        /// <summary>
        /// Gets the number of stored rows for the given table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of rows in the table.</returns>
        public override long GetStoredNumberOfRows(string tableName)
        {
            return _table.GetStoredNumberOfRows(tableName);
        }

        /// <summary>
        /// Gets the number of stored columns for the given table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of columns in the table.</returns>
        public override int GetStoredNumberOfColumns(string tableName)
        {
            return _table.GetStoredNumberOfColumns(tableName);
        }

        /// <summary>
        /// Represents the editable view of the CSV data.
        /// </summary>
        private class CsvTableReader : DataTableView
        {
            private readonly DataTable _table;

            /// <summary>
            /// Initializes a new instance of the <see cref="CsvTableReader"/> class.
            /// </summary>
            /// <param name="inMemManager">The CSV reader parent.</param>
            /// <param name="inMemReader">The in-memory reader wrapping the table.</param>
            /// <param name="dataTableName">The table name to manage.</param>
            public CsvTableReader(CsvReader inMemManager, InMemoryReader inMemReader, string dataTableName)
            {
                _parentDatabase = inMemManager;
                _table = inMemReader.Table;
                _tableName = dataTableName;
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                _storedNumberOfRows = (int)GetStoredRowCount();
                // view stuff
                InitializeView();

            }

            /// <summary>
            /// Gets the names of all stored columns in the current CSV table.
            /// </summary>
            /// <returns>An array of column names.</returns>
            protected override string[] GetStoredColumnNames()
            {
                var names = new string[_table.Columns.Count];
                for (int i = 0; i < names.Count(); i++)
                { 
                    names[i] = _table.Columns[i].ColumnName; 
                }
                return names;
            }

            /// <summary>
            /// Gets the data types of all stored columns in the current CSV table.
            /// </summary>
            /// <returns>An array of column data types.</returns>
            protected override Type[] GetStoredColumnTypes()
            {
                var types = new Type[_table.Columns.Count];
                for (int i = 0; i < types.Count(); i++)
                { 
                    types[i] = _table.Columns[i].DataType; 
                }
                return types;
            }

            /// <summary>
            /// Gets the number of rows stored in the CSV table.
            /// </summary>
            /// <returns>The number of rows in the table.</returns>
            protected override ulong GetStoredRowCount()
            {
                return (ulong)_table.Rows.Count;
            }


            #region Column Stuff

            /// <summary>
            /// Retrieves all values in a given column by name.
            /// </summary>
            /// <param name="storedColumnName">The name of the column to retrieve.</param>
            /// <returns>An array of values from the specified column.</returns>
            protected override object[] GetStoredColumn(string storedColumnName)
            {
                var result = new object[_table.Rows.Count];
                for (int i = 0; i < _table.Rows.Count; i++)
                { 
                    result[i] = _table.Rows[i][storedColumnName]; 
                }
                return result;
            }

            /// <summary>
            /// Retrieves all values in a given column by index.
            /// </summary>
            /// <param name="storedColumnIndex">The index of the column to retrieve.</param>
            /// <returns>An array of values from the specified column.</returns>
            protected override object[] GetStoredColumn(int storedColumnIndex)
            {
                var result = new object[_table.Rows.Count];
                for (int i = 0; i < _table.Rows.Count; i++)
                { 
                    result[i] = _table.Rows[i][storedColumnIndex]; 
                }
                return result;
            }
            
            /// <summary>
            /// Throws because CSV does not support storage of byte arrays.
            /// </summary>
            /// <param name="columnName">Name of the new column.</param>
            /// <param name="columnData">Array of byte arrays.</param>
            /// <exception cref="NotImplementedException"></exception>
            protected override void AddColumnToDatabase(string columnName, byte[][] columnData)
            {
                throw new NotImplementedException("CSV does not support storage of byte arrays.");
            }

            /// <summary>
            /// Adds a column of byte values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                // 
                _table.Columns.Add(columnName, typeof(byte));
                for (int i = 0; i < columnData.Count(); i++)
                { 
                    _table.Rows[i][_table.Columns.Count - 1] = columnData[i]; 
                }
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of double values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of int values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of long values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of short values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of float values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of boolean values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Adds a column of string values to the database.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
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
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Deletes a column from the CSV table.
            /// </summary>
            /// <param name="columnName">The column to delete.</param>
            protected override void DeleteColumnFromDatabase(string columnName)
            {
                _table.Columns.Remove(columnName);
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Deletes multiple columns from the CSV table.
            /// </summary>
            /// <param name="columnsToDelete">The columns to delete.</param>
            protected override void DeleteColumnsFromDatabase(string[] columnsToDelete)
            {
                for (int i = 0; i < columnsToDelete.Count(); i++)
                { 
                    _table.Columns.Remove(columnsToDelete[i]);
                }
                ExportToCsv(_parentDatabase.DataBasePath);
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
            }

            /// <summary>
            /// Replaces all values in the specified column with a new boolean array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new boolean values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or the length doesn't match row count.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Replaces all values in the specified column with a new byte array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new byte values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or the length doesn't match row count.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Not supported in CSV format; throws an exception.
            /// </summary>
            /// <param name="columnName">The column name.</param>
            /// <param name="columnData">Byte arrays (not supported).</param>
            /// <exception cref="NotImplementedException">Always thrown.</exception>
            protected override void EditDatabaseColumn(string columnName, byte[][] columnData)
            {
                throw new NotImplementedException("csv file format does not support storing binary array data.");
            }

            /// <summary>
            /// Replaces all values in the specified column with a new short array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new short values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Replaces all values in the specified column with a new integer array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new int values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Replaces all values in the specified column with a new long integer array. 
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new float values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Replaces all values in the specified column with a new float array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new float values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Replaces all values in the specified column with a new double array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new double values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch.</exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }
            
            /// <summary>
            /// Replaces all values in the specified column with a new string array.
            /// </summary>
            /// <param name="columnName">The column name to modify.</param>
            /// <param name="columnData">The new string values to apply.</param>
            /// <exception cref="Exception">Thrown if the column doesn't exist or length mismatch. </exception>
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
                ExportToCsv(_parentDatabase.DataBasePath);
            }
            #endregion

            #region Row Stuff

            /// <summary>
            /// Adds a single row to the internal DataTable and exports to CSV.
            /// </summary>
            /// <param name="row"></param>
            protected override void AddRowToDatabase(object[] row)
            {
                _table.Rows.Add(row);
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Adds an empty row using the DataTable schema and exports to CSV.
            /// </summary>
            protected override void AddRowToDatabase()
            {
                _table.Rows.Add(_table.NewRow());
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Adds multiple rows to the table and exports to CSV.
            /// </summary>
            /// <param name="newRowData">List of object arrays representing new rows.</param>
            protected override void AddRowsToDatabase(List<object[]> newRowData)
            {
                for (int i = 0; i < newRowData.Count; i++)
                { 
                    AddRowToDatabase(newRowData[i]); 
                }
            }

            /// <summary>
            /// Deletes a row by index from the internal table and exports to CSV.
            /// </summary>
            /// <param name="rowIndex">Index of the row to delete.</param>
            protected override void DeleteRowFromDatabase(int rowIndex)
            {
                _table.Rows[rowIndex].Delete();
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Deletes multiple rows by index and exports to CSV.
            /// </summary>
            /// <param name="rowIndices">Array of row indices to delete.</param>
            protected override void DeleteRowsFromDatabase(int[] rowIndices)
            {
                Array.Sort(rowIndices);
                for (int i = rowIndices.Count() - 1; i >= 0; i -= 1)
                {
                    _table.Rows[rowIndices[i]].Delete(); 
                }
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Retrieves an entire row by index.
            /// </summary>
            /// <param name="storedRowIndex">Row index to retrieve.</param>
            /// <returns>An array of values representing the row.</returns>
            protected override object[] GetStoredRow(int storedRowIndex)
            {
                return _table.Rows[storedRowIndex].ItemArray;
            }

            /// <summary>
            /// Retrieves values from specific columns in a given row.
            /// </summary>
            /// <param name="storedRowIndex">Row index to retrieve.</param>
            /// <param name="storedColumnNames">Subset of column names.</param>
            /// <returns>An array of values from the specified columns in the row.</returns>
            protected override object[] GetStoredRow(int storedRowIndex, string[] storedColumnNames)
            {
                var result = new object[(storedColumnNames.Count())];
                for (int i = 0; i < storedColumnNames.Count(); i++)
                { 
                    result[i] = _table.Rows[storedRowIndex][storedColumnNames[i]]; 
                }
                return result;
            }

            /// <summary>
            /// Retrieves values from specific column indices in a given row.
            /// </summary>
            /// <param name="storedRowIndex">Row index.</param>
            /// <param name="storedColumnIndices">Array of column indices.</param>
            /// <returns>An array of values from the specified columns in the row.</returns>
            protected override object[] GetStoredRow(int storedRowIndex, int[] storedColumnIndices)
            {
                var result = new object[(storedColumnIndices.Count())];
                for (int i = 0; i < storedColumnIndices.Count(); i++)
                { 
                    result[i] = _table.Rows[storedRowIndex][storedColumnIndices[i]]; 
                }
                return result;
            }

            /// <summary>
            /// Retrieves a list of rows between two indices (inclusive).
            /// </summary>
            /// <param name="startStoredRowIndex">Starting index (inclusive).</param>
            /// <param name="endStoredRowIndex">Ending index (inclusive).</param>
            /// <returns>A list of object arrays, where each array represents a row.</returns>
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

            /// <summary>
            /// Edits multiple cells given column names and row indices.
            /// </summary>
            /// <param name="columnsToEdit"></param>
            /// <param name="rowIndices"></param>
            /// <param name="cellValues"></param>
            protected override void EditDatabaseCells(string[] columnsToEdit, int[] rowIndices, object[] cellValues)
            {
                if (columnsToEdit is null) throw new ArgumentNullException(nameof(columnsToEdit));
                if (rowIndices is null) throw new ArgumentNullException(nameof(rowIndices));
                if (cellValues is null) throw new ArgumentNullException(nameof(cellValues));

                int n = columnsToEdit.Length;
                if (rowIndices.Length != n || cellValues.Length != n)
                    throw new ArgumentException("columnsToEdit, rowIndices, and cellValues must have the same length.");

                // Validate each edit target
                for (int i = 0; i < n; i++)
                {
                    string col = columnsToEdit[i];
                    if (string.IsNullOrWhiteSpace(col))
                        throw new ArgumentException($"Column name at index {i} is null/empty.", nameof(columnsToEdit));

                    if (!_table.Columns.Contains(col))
                        throw new ArgumentException($"Column '{col}' does not exist.", nameof(columnsToEdit));

                    int r = rowIndices[i];
                    if ((uint)r >= (uint)_table.Rows.Count) // fast bounds check (covers negative too)
                        throw new ArgumentOutOfRangeException(nameof(rowIndices),
                            $"Row index {r} at edit {i} is out of range. Valid range: 0..{_table.Rows.Count - 1}.");
                }

                for (int i = 0; i < columnsToEdit.Count(); i++)
                { 
                    _table.Rows[rowIndices[i]][columnsToEdit[i]] = cellValues[i]; 
                }
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits multiple cells given column indices and row indices.
            /// </summary>
            /// <param name="columnIndices"></param>
            /// <param name="rowIndices"></param>
            /// <param name="cellValues"></param>
            protected override void EditDatabaseCells(int[] columnIndices, int[] rowIndices, object[] cellValues)
            {
                if (columnIndices is null) throw new ArgumentNullException(nameof(columnIndices));
                if (rowIndices is null) throw new ArgumentNullException(nameof(rowIndices));
                if (cellValues is null) throw new ArgumentNullException(nameof(cellValues));

                int n = columnIndices.Length;
                if (rowIndices.Length != n || cellValues.Length != n)
                    throw new ArgumentException("columnIndices, rowIndices, and cellValues must have the same length.");

                int rowCount = _table.Rows.Count;
                int colCount = _table.Columns.Count;

                for (int i = 0; i < n; i++)
                {
                    int c = columnIndices[i];
                    if ((uint)c >= (uint)colCount)
                        throw new ArgumentOutOfRangeException(nameof(columnIndices),
                            $"Column index {c} at edit {i} is out of range. Valid range: 0..{colCount - 1}.");

                    int r = rowIndices[i];
                    if ((uint)r >= (uint)rowCount)
                        throw new ArgumentOutOfRangeException(nameof(rowIndices),
                            $"Row index {r} at edit {i} is out of range. Valid range: 0..{rowCount - 1}.");
                }
                for (int i = 0; i < columnIndices.Count(); i++)
                {
                    _table.Rows[rowIndices[i]][columnIndices[i]] = cellValues[i]; 
                }
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a boolean value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, bool cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a byte value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a double value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, double cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a float value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, float cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a short value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, short cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with an int value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, int cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a long value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, long cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Edits a single cell with a string value.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, string cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
                ExportToCsv(_parentDatabase.DataBasePath);
            }

            /// <summary>
            /// Throws as CSV format does not support byte[] cells.
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            /// <exception cref="NotImplementedException"></exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte[] cellValue)
            {
                throw new NotImplementedException("CSV does not support storage of byte arrays.");
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, DateTime cellValue)
            {
                _table.Rows[rowIndex][columnIndex] = cellValue;
            }

            /// <summary>
            /// Retrieves a cell by column and row index.
            /// </summary>
            /// <param name="storedColumnIndex">The column index.</param>
            /// <param name="storedRowIndex">The row index.</param>
            /// <returns>The cell value at the specified position.</returns>
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

            /// <summary>
            /// Retrieves a cell by column name and row index.
            /// </summary>
            /// <param name="storedColumnName">The column name.</param>
            /// <param name="storedRowIndex">The row index.</param>
            /// <returns>The cell value at the specified position.</returns>
            protected override object GetStoredCell(string storedColumnName, int storedRowIndex)
            {
                if (storedRowIndex < 0 || storedRowIndex >= _table.Rows.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_table.Rows.Count - 1}.");
                }
                return _table.Rows[storedRowIndex][storedColumnName];
            }

            /// <summary>
            /// Retrieves a set of cells by column/row indices.
            /// </summary>
            /// <param name="storedColumnIndices">Array of column indices.</param>
            /// <param name="storedRowIndices">Array of row indices.</param>
            /// <returns>An array of cell values at the specified positions.</returns>
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