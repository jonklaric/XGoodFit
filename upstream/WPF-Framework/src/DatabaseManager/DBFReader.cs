using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Provides functionality to read and manipulate DBF (dBASE) files.
    /// </summary>
    public class DbfReader : DatabaseManager
    {
        /// <summary>
        /// Stream used to access the DBF file.
        /// </summary>
        private FileStream _dbfStream;

        /// <summary>
        /// Binary reader to read from the DBF file stream.
        /// </summary>
        private BinaryReader _dbfReader;

        /// <summary>
        /// Exposes the binary reader used to read DBF content.
        /// </summary>
        public BinaryReader DbReader
        {
            get
            {
                return _dbfReader;
            }
        }

        /// <summary>
        /// Structure to define metadata for a DBF field.
        /// </summary>
        private struct DbfField
        {
            // Public Name As String
            public string TypeId;
            // Public Type As Type
            public int Length;
            public int NumDecimal;
            public bool WriteField;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DbfReader"/> class and verifies file extension.
        /// </summary>
        /// <param name="filePath">The path to the DBF file.</param>
        /// <exception cref="Exception">Thrown if the file is not a .dbf file.</exception>
        public DbfReader(string filePath)
        {
            if (Path.GetExtension(filePath).ToLower() != ".dbf")
                throw new Exception("This is not a .dbf file");
            _dataBasePath = filePath;
            _tableNames = GetTableNames();
        }

        /// <summary>
        /// Opens the DBF file and initializes the binary reader.
        /// </summary>
        public override void Open()
        {
            _dbfStream = new FileStream(_dataBasePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            _dbfReader = new BinaryReader(_dbfStream);
            _dataBaseOpen = true;
        }

        /// <summary>
        /// Closes the DBF file and disposes of associated resources.
        /// </summary>
        public override void Close()
        {
            _dataBaseOpen = false;
            // Disposing BinaryReader automatically disposes the underlying stream
            _dbfReader?.Dispose();
            _dbfReader = null;
            _dbfStream = null;
        }

        /// <summary>
        /// Gets a table manager for a given table name.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>An instance of <see cref="DataTableView"/> for reading DBF data.</returns>
        public override DataTableView GetTableManager(string tableName)
        {
            return new DbfTableReader(this, tableName);
        }

        /// <summary>
        /// Retrieves the name of the table from the DBF file.
        /// </summary>
        /// <returns>An array containing the single table name.</returns>
        public override string[] GetTableNames()
        {
            return new string[] { Path.GetFileNameWithoutExtension(_dataBasePath) };
        }

        /// <summary>
        /// Reads the number of stored rows from the DBF file header.
        /// </summary>
        /// <param name="tableName">The table name (not used directly in DBF format).</param>
        /// <returns>The number of rows stored in the DBF file.</returns>
        public override long GetStoredNumberOfRows(string tableName)
        {
            bool wasOpen = DataBaseOpen;
            if (DataBaseOpen == false)
            {
                Open(); 
            }
            _dbfReader.BaseStream.Position = 0L;
            // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
            byte[] bytes = _dbfReader.ReadBytes(32);
            int numberOfRows = BitConverter.ToInt32(bytes, 4); // number of rows
                                                               // 
            if (wasOpen == false)
            {
                Close(); 
            }
            return numberOfRows;
        }

        /// <summary>
        /// Reads the number of stored columns from the DBF file header.
        /// </summary>
        /// <param name="tableName">The table name (not used directly in DBF format).</param>
        /// <returns>The number of columns in the DBF file.</returns>
        public override int GetStoredNumberOfColumns(string tableName)
        {
            bool wasOpen = DataBaseOpen;
            if (DataBaseOpen == false)
            {
                Open(); 
            }
            _dbfReader.BaseStream.Position = 0L;
            // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
            byte[] bytes = _dbfReader.ReadBytes(32);
            short firstDataRecordIndex = BitConverter.ToInt16(bytes, 8); // always 32 + 32*number of fields + 1
            int totalColumns = (firstDataRecordIndex - 32 - 1) / 32;
            // 
            if (wasOpen == false)
            {
                Close(); 
            }
            return totalColumns;
        }

        /// <summary>
        /// Provides access to reading and writing rows and columns in a DBF file.
        /// </summary>
        private class DbfTableReader : DataTableView
        {
            private short _firstDataRecordIndex;
            private List<long> _recordStartPositions = new List<long>();
            private readonly DbfReader _parentDbfReader;
            private short _recordLength;
            private byte[] _lengths;
            private short[] _positions;

            /// <summary>
            /// Initializes a new instance of the <see cref="DbfTableReader"/> class.
            /// </summary>
            /// <param name="parentManager">Reference to the parent DBF reader.</param>
            /// <param name="dataTableName">Name of the data table being managed.</param>
            public DbfTableReader(DbfReader parentManager, string dataTableName)
            {
                _parentDatabase = parentManager;
                _parentDbfReader = parentManager;
                _tableName = dataTableName;
                LoadAttributeInfo();
                InitializeView();
            }

            /// <summary>
            /// Gets the stored column names in the DBF table.
            /// </summary>
            /// <returns>An array of column names.</returns>
            protected override string[] GetStoredColumnNames()
            {
                return _storedColumnNames;
            }

            /// <summary>
            /// Gets the stored column types in the DBF table.
            /// </summary>
            /// <returns>An array of column data types.</returns>
            protected override Type[] GetStoredColumnTypes()
            {
                return _storedColumnTypes;
            }

            /// <summary>
            /// Gets the number of rows stored in the DBF table.
            /// </summary>
            /// <returns>Unsigned long indicating the number of rows.</returns>
            protected override ulong GetStoredRowCount()
            {
                return (ulong)_recordStartPositions.Count;
            }

            #region Database Row Stuff

            /// <summary>
            /// Adds a new row with specified data to the DBF file.
            /// </summary>
            /// <param name="row">Array of values representing a single row.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddRowToDatabase(object[] row)
            {
                // Error Checking
                if (row.Count() != _storedColumnNames.Count())
                {
                    throw new Exception("Number of columns that you are trying to add do not match the number of columns in the database."); 
                }
                for (int i = 0; i < row.Count(); i++)
                {
                    if (ConvertToColumnType(_storedColumnTypes[i], ref row[i]) == false)
                    {
                        throw new Exception("Column data for " + _storedColumnNames[i] + " was of an incorrect data type."); 
                    }
                }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Insert Row Data
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Update the number of rows
                    dbfReaderFs.Position = 4L;
                    dbfReaderFs.Write(BitConverter.GetBytes(_recordStartPositions.Count + 1), 0, 4);
                    dbfReaderFs.Position = dbfReaderFs.Length - 1L;
                    // Write the Row
                    byte nDecimals;
                    string value;
                    byte[] asciiBytes;
                    string formatString;
                    dbfReaderFs.WriteByte(32);
                    for (int i = 0; i < row.Count(); i++)
                    {
                        switch (_storedColumnTypes[i])
                        {
                            case var @case when @case == typeof(float):
                            case var case1 when case1 == typeof(double):
                                {
                                    dbfReaderFs.Position = 32 + i * 32 + 17;
                                    nDecimals = (byte)dbfReaderFs.ReadByte();
                                    if (_lengths[i] - nDecimals < 7)
                                    {
                                        formatString = "G";
                                    }
                                    else
                                    {
                                        formatString = "0.";
                                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                                        formatString = formatString + "e+000";
                                    }

                                    // edit the appropriate part of the dbf file
                                    dbfReaderFs.Position = dbfReaderFs.Length;
                                    if (row[i].Equals(DBNull.Value))
                                    {
                                        value = "".PadRight(_lengths[i], ' ');
                                    }
                                    else
                                    {
                                        value = Convert.ToDouble(row[i]).ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[i], ' ');
                                    }
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case2 when case2 == typeof(int):
                            case var case3 when case3 == typeof(short):
                            case var case4 when case4 == typeof(byte):
                                {
                                    if (row[i].Equals(DBNull.Value))
                                    {
                                        value = "".PadLeft(_lengths[i], ' ');
                                    }
                                    else
                                    {
                                        value = Convert.ToInt32(row[i]).ToString().PadLeft(_lengths[i], ' ');
                                    }
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case5 when case5 == typeof(bool):
                                {
                                    if (row[i].Equals(DBNull.Value))
                                    {
                                        value = "0";
                                    }
                                    else if (Convert.ToBoolean(row[i]) == true)
                                    {
                                        value = "1"; // can be 1,T,t,Y,and y
                                    }
                                    else
                                    {
                                        value = "0";
                                    } // can be 0,F,f,N, and n
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case6 when case6 == typeof(string):
                                {
                                    if (row[i].Equals(DBNull.Value))
                                    {
                                        value = "".PadRight(_lengths[i], ' ');
                                    }
                                    else if (Convert.ToString(row[i]).Length > _lengths[i])
                                    {
                                        value = Convert.ToString(row[i]).Substring(0, _lengths[i]);
                                    }
                                    else
                                    {
                                        value = Convert.ToString(row[i]).PadRight(_lengths[i], ' ');
                                    }
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                        }
                    }

                    // eof dbf file marker
                    dbfReaderFs.WriteByte(26);
                    // 
                }
                // 
                LoadAttributeInfo();

                if (wasOpen == true)
                    _parentDatabase.Open();
            }


            /// <summary>
            /// Adds a new blank row to the DBF file.
            /// </summary>
            protected override void AddRowToDatabase()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                    _parentDatabase.Close();
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Insert Row Data
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Dim row(_storedColumnNames.Count - 1) As Object
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Update the number of rows
                    dbfReaderFs.Position = 4L;
                    dbfReaderFs.Write(BitConverter.GetBytes(_recordStartPositions.Count + 1), 0, 4);
                    dbfReaderFs.Position = dbfReaderFs.Length - 1L;
                    // Write the Row
                    string value;
                    byte[] asciiBytes; // , formatString As String
                    dbfReaderFs.WriteByte(32);
                    for (int i = 0; i < _storedColumnNames.Count(); i++)
                    {
                        switch (_storedColumnTypes[i])
                        {
                            case var @case when @case == typeof(float):
                            case var case1 when case1 == typeof(double):
                                {
                                    // edit the appropriate part of the dbf file
                                    dbfReaderFs.Position = dbfReaderFs.Length;
                                    value = "".PadRight(_lengths[i], ' ');
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case2 when case2 == typeof(int):
                            case var case3 when case3 == typeof(short):
                            case var case4 when case4 == typeof(byte):
                                {
                                    value = "".PadLeft(_lengths[i], ' ');
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case5 when case5 == typeof(bool):
                                {
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes("0");
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                            case var case6 when case6 == typeof(string):
                                {
                                    value = "".PadRight(_lengths[i], ' ');
                                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                    break;
                                }
                        }
                    }

                    // eof dbf file marker
                    dbfReaderFs.WriteByte(26);
                    // 
                }
                // 
                LoadAttributeInfo();
                // 
                if (wasOpen == true)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Adds multiple new rows to the DBF file.
            /// </summary>
            /// <param name="newRowData"></param>
            /// <exception cref="Exception"></exception>
            protected override void AddRowsToDatabase(List<object[]> newRowData)
            {
                // Error Checking
                foreach (object[] row in newRowData)
                {
                    if (row.Count() != _storedColumnNames.Count())
                    {
                        throw new Exception("Number of columns that you are trying to add do not match the number of columns in the database."); 
                    }
                }
                foreach (object[] row in newRowData)
                {
                    for (int i = 0; i < row.Count(); i++)
                    {
                        if (ConvertToColumnType(_storedColumnTypes[i], ref row[i]) == false)
                        {
                            throw new Exception("Column data for " + _storedColumnNames[i] + " was of an incorrect data type."); 
                        }
                    }
                }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Insert Row Data
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // Update the number of rows
                    dbfReaderFs.Position = 4L;
                    dbfReaderFs.Write(BitConverter.GetBytes(_recordStartPositions.Count + newRowData.Count), 0, 4);
                    dbfReaderFs.Position = dbfReaderFs.Length - 1L;
                    // Write the Row
                    byte nDecimals;
                    string value;
                    byte[] asciiBytes;
                    string formatString;
                    foreach (object[] row in newRowData)
                    {
                        dbfReaderFs.WriteByte(32);
                        for (int i = 0; i < row.Count(); i++)
                        {
                            switch (_storedColumnTypes[i])
                            {
                                case var @case when @case == typeof(float):
                                case var case1 when case1 == typeof(double):
                                    {
                                        dbfReaderFs.Position = 32 + i * 32 + 17;
                                        nDecimals = (byte)dbfReaderFs.ReadByte();
                                        if (_lengths[i] - nDecimals < 7)
                                        {
                                            formatString = "G";
                                        }
                                        else
                                        {
                                            formatString = "0.";
                                            formatString = formatString.PadRight(nDecimals + formatString.Length, '0');
                                            formatString = formatString + "e+000";
                                        }

                                        // edit the appropriate part of the dbf file
                                        dbfReaderFs.Position = dbfReaderFs.Length;
                                        if (row[i].Equals(DBNull.Value))
                                        {
                                            value = "".PadRight(_lengths[i], ' ');
                                        }
                                        else
                                        {
                                            value = Convert.ToDouble(row[i]).ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[i], ' ');
                                        }
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        break;
                                    }
                                case var case2 when case2 == typeof(int):
                                case var case3 when case3 == typeof(short):
                                case var case4 when case4 == typeof(byte):
                                    {
                                        if (row[i].Equals(DBNull.Value))
                                        {
                                            value = "".PadLeft(_lengths[i], ' ');
                                        }
                                        else
                                        {
                                            value = Convert.ToInt32(row[i]).ToString().PadLeft(_lengths[i], ' ');
                                        }
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        break;
                                    }
                                case var case5 when case5 == typeof(bool):
                                    {
                                        if (row[i].Equals(DBNull.Value))
                                        {
                                            value = "0";
                                        }
                                        else if (Convert.ToBoolean(row[i]) == true)
                                        {
                                            value = "1"; // can be 1,T,t,Y,and y
                                        }
                                        else
                                        {
                                            value = "0";
                                        } // can be 0,F,f,N, and n
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        break;
                                    }
                                case var case6 when case6 == typeof(string):
                                    {
                                        if (row[i].Equals(DBNull.Value))
                                        {
                                            value = "".PadRight(_lengths[i], ' ');
                                        }
                                        else if (Convert.ToString(row[i]).Length > _lengths[i])
                                        {
                                            value = Convert.ToString(row[i]).Substring(0, _lengths[i]);
                                        }
                                        else
                                        {
                                            value = Convert.ToString(row[i]).PadRight(_lengths[i], ' ');
                                        }
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        break;
                                    }
                            }
                        }
                    }
                    // eof dbf file marker
                    dbfReaderFs.WriteByte(26);
                    //
                }
                //
                LoadAttributeInfo();
                //
                if (wasOpen == true)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Deletes a specific row from the DBF file.
            /// </summary>
            /// <param name="rowIndex">Index of the row to delete.</param>
            protected override void DeleteRowFromDatabase(int rowIndex)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadByte());
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        writeBwDbf.Write(_recordStartPositions.Count - 1);
                        _parentDbfReader.DbReader.BaseStream.Position += 4L;
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(24));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        for (int i = 0; i < _storedColumnNames.Count(); i++)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        for (int i = 0; i < _recordStartPositions.Count; i++)
                        {
                            if (i == rowIndex)
                            {
                                _parentDbfReader.DbReader.BaseStream.Position += _recordLength;
                            }
                            else
                            {
                                writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(_recordLength));
                            }
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                File.Delete(_parentDatabase.DataBasePath);
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                // 
                File.Delete(tmpdbf);
                LoadAttributeInfo();
                if (wasOpen == true)
                {
                    _parentDatabase.Open();
                }
                else
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// Deletes multiple rows from the DBF file.
            /// </summary>
            /// <param name="rowIndices">Array of indices of rows to delete.</param>
            protected override void DeleteRowsFromDatabase(int[] rowIndices)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                Array.Sort(rowIndices);
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadByte());
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        writeBwDbf.Write(_recordStartPositions.Count - rowIndices.Count());
                        _parentDbfReader.DbReader.BaseStream.Position += 4L;
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(24));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        for (int i = 0; i < _storedColumnNames.Count(); i++)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        int rowToDeleteIndex = 0;
                        for (int i = 0; i < _recordStartPositions.Count; i++)
                        {
                            if (i == rowIndices[rowToDeleteIndex])
                            {
                                _parentDbfReader.DbReader.BaseStream.Position += _recordLength;
                                if (rowToDeleteIndex < rowIndices.Count() - 1)
                                {
                                    rowToDeleteIndex += 1; 
                                }
                            }
                            else
                            {
                                writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(_recordLength));
                            }
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                File.Delete(_parentDatabase.DataBasePath);
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                // 
                File.Delete(tmpdbf);
                LoadAttributeInfo();
                if (wasOpen == true)
                {
                    _parentDatabase.Open();
                }
                else
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// Parses a row from DBF file byte arrays into typed values.
            /// </summary>
            /// <param name="rowBytes">The raw bytes representing a single row in the DBF file.</param>
            /// <returns>An array of typed values parsed from the row bytes.</returns>
            private object[] ParseRowBytes(byte[] rowBytes)
            {
                var row = new object[(_storedColumnNames.Count())];
                string cellValue;
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    cellValue = System.Text.Encoding.UTF8.GetString(rowBytes, _positions[i], _lengths[i]).Trim('\0');
                    row[i] = ConvertCellValueToProperType(cellValue, _storedColumnTypes[i]);
                }
                return row;
            }

            /// <summary>
            /// Retrieves a single row from the DBF file at the specified index.
            /// </summary>
            /// <param name="storedRowIndex">The zero-based index of the stored row.</param>
            /// <returns>An object array representation the row data.</returns>
            protected override object[] GetStoredRow(int storedRowIndex)
            {
                _parentDbfReader.DbReader.BaseStream.Position = _recordStartPositions[storedRowIndex];
                return ParseRowBytes(_parentDbfReader.DbReader.ReadBytes(_recordLength));
            }

            /// <summary>
            /// Retrieves a single row by index, limited to specified stored column names.
            /// </summary>
            /// <param name="storedRowIndex">Index of the row to retrieve.</param>
            /// <param name="storedColumnNames">Subset of stored column names to include.</param>
            /// <returns>Object array of cell values in requested columns.</returns>
            protected override object[] GetStoredRow(int storedRowIndex, string[] storedColumnNames)
            {
                var columnIndices = new int[(storedColumnNames.Count())];
                for (int i = 0; i < storedColumnNames.Count(); i++)
                {
                    columnIndices[i] = Array.IndexOf(_storedColumnNames, storedColumnNames[i]); 
                }
                return GetStoredRow(storedRowIndex, columnIndices);
            }

            /// <summary>
            /// Retrieves a single row by index, limited to specified stored column indices.
            /// </summary>
            /// <param name="storedRowIndex">Index of the row to retrieve.</param>
            /// <param name="storedColumnIndices">Array of column indices to include.</param>
            /// <returns>Object array of cell values.</returns>
            protected override object[] GetStoredRow(int storedRowIndex, int[] storedColumnIndices)
            {
                _parentDbfReader.DbReader.BaseStream.Position = _recordStartPositions[storedRowIndex];
                byte[] rowBytes = _parentDbfReader.DbReader.ReadBytes(_recordLength);
                string cellValue;
                var returnValues = new object[(storedColumnIndices.Count())];
                for (int i = 0; i < storedColumnIndices.Count(); i++)
                {
                    cellValue = System.Text.Encoding.UTF8.GetString(rowBytes, _positions[storedColumnIndices[i]], _lengths[storedColumnIndices[i]]).Trim('\0');
                    returnValues[i] = ConvertCellValueToProperType(cellValue, _storedColumnTypes[storedColumnIndices[i]]);
                }
                return returnValues;
            }

            /// <summary>
            /// Retrieves multiple rows from the DBF file within a specified range.
            /// </summary>
            /// <param name="startStoredRowIndex">The starting row index.</param>
            /// <param name="endStoredRowIndex">The ending row index (inclusive).</param>
            /// <returns>A list of object arrays representing rows.</returns>
            protected override List<object[]> GetStoredRows(int startStoredRowIndex, int endStoredRowIndex)
            {
                // Need to compare this function with a function that reads in a block of bytes.  In other words, is it faster to use the readers
                // .readbytes() function for each cell or is it faster to just read in all of the requested bytes up front and parse the cells from
                // the byte array?
                if (endStoredRowIndex > _recordStartPositions.Count - 1)
                    endStoredRowIndex = _recordStartPositions.Count - 1;
                var row = new object[(_storedColumnNames.Count())];
                var rows = new List<object[]>(endStoredRowIndex - startStoredRowIndex + 1);
                byte[] rowBytes;
                string cellValue;
                for (int i = startStoredRowIndex; i <= endStoredRowIndex; i++)
                {
                    _parentDbfReader.DbReader.BaseStream.Seek(_recordStartPositions[i], SeekOrigin.Begin);
                    rowBytes = _parentDbfReader.DbReader.ReadBytes(_recordLength);
                    for (int j = 0; j < _storedColumnNames.Count(); j++)
                    {
                        cellValue = System.Text.Encoding.UTF8.GetString(rowBytes, _positions[j], _lengths[j]).Trim('\0');
                        row[j] = ConvertCellValueToProperType(cellValue, _storedColumnTypes[j]);
                    }
                    rows.Add((object[])row.Clone());
                }
                return rows;
            }
            #endregion

            #region Database Column Stuff

            /// <summary>
            /// Retrieves all values in a column by name from the DBF file.
            /// </summary>
            /// <param name="storedColumnName">Name of the column to retrieve.</param>
            /// <returns>An array of values from the specified column.</returns>
            /// <exception cref="Exception">Thrown when the specified column does not exist.</exception>
            protected override object[] GetStoredColumn(string storedColumnName)
            {
                if (_storedColumnNames.Contains(storedColumnName) == false)
                {
                    throw new Exception("Field '" + storedColumnName + "' does not exist"); 
                }
                int colindex = Array.IndexOf(_storedColumnNames, storedColumnName);
                return GetStoredColumn(colindex);
            }

            /// <summary>
            /// Retrieves all values in a column by index from the DBF file.
            /// </summary>
            /// <param name="storedColumnIndex">Index of the column to retrieve.</param>
            /// <returns>An array of values from the specified column.</returns>
            /// <exception cref="Exception">Thrown when the specified column index is out of range.</exception>
            protected override object[] GetStoredColumn(int storedColumnIndex)
            {
                if (_storedColumnNames.Count() <= storedColumnIndex)
                {
                    throw new Exception("Requested index does not exist"); 
                }
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                byte[] dbfbytes;
                var result = new List<object>(_recordStartPositions.Count);                     
                _parentDbfReader.DbReader.BaseStream.Position = _firstDataRecordIndex;
                string cellValue;
                for (int i = 0; i < _recordStartPositions.Count; i++)
                {
                    dbfbytes = _parentDbfReader.DbReader.ReadBytes(_recordLength);
                    cellValue = System.Text.Encoding.UTF8.GetString(dbfbytes, _positions[storedColumnIndex] + 1, _lengths[storedColumnIndex]).Trim('\0');
                    result.Add(ConvertCellValueToProperType(cellValue, _storedColumnTypes[storedColumnIndex]));
                }
                if (wasOpen == false)
                    _parentDatabase.Close();
                return result.ToArray();
            }

            /// <summary>
            /// Edits an entire numeric column in the DBF file using double values.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of new values to apply to the column.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, double[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp = columnData; object argvalue = tmp[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue); tmp[0] = Convert.ToDouble(argvalue); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the number of decimals from the Main File Header
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                _parentDbfReader.DbReader.BaseStream.Position = 32 + columnIndex * 32 + 17;
                byte nDecimals;
                nDecimals = _parentDbfReader.DbReader.ReadByte();
                _parentDatabase.Close();
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    string formatString;
                    if (_lengths[columnIndex] - nDecimals < 7)
                    {
                        formatString = "G";
                    }
                    else
                    {
                        formatString = "0.";
                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                        formatString = formatString + "e+000";
                    }

                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[columnIndex], ' ');
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits an entire numeric column in the DBF file using float values.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of byte arrays.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, float[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp1 = columnData; object argvalue1 = tmp1[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue1); tmp1[0] = Convert.ToSingle(argvalue1); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the number of decimals from the Main File Header
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }

                _parentDbfReader.DbReader.BaseStream.Position = 32 + columnIndex * 32 + 17;
                byte nDecimals;
                nDecimals = _parentDbfReader.DbReader.ReadByte();
                _parentDatabase.Close();
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    string formatString;
                    if (_lengths[columnIndex] - nDecimals < 7)
                    {
                        formatString = "G";
                    }
                    else
                    {
                        formatString = "0.";
                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                        formatString = formatString + "e+000";
                    }

                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[columnIndex], ' ');
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Not supported. DBF files do not allow binary array column types.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of byte arrays.</param>
            /// <exception cref="NotImplementedException">Thrown always because DBF format does not support binary arrays.</exception>
            protected override void EditDatabaseColumn(string columnName, byte[][] columnData)
            {
                throw new NotImplementedException("dbf file format does not support storing binary array data.");
            }

            /// <summary>
            /// Edits an entire text column in the DBF file using string values.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of string values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, string[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp2 = columnData; object argvalue2 = tmp2[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue2); tmp2[0] = Convert.ToString(argvalue2); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        if (columnData[i].Length > _lengths[columnIndex])
                        {
                            value = columnData[i].Substring(0, _lengths[columnIndex]);
                        }
                        else
                        {
                            value = columnData[i].PadRight(_lengths[columnIndex], ' ');
                        }
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a boolean column in the DBF file.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of boolean values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, bool[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp3 = columnData; object argvalue3 = tmp3[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue3); tmp3[0] = Convert.ToBoolean(argvalue3); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        if (columnData[i])
                        {
                            value = "1"; // can be 1,T,t,Y,and y
                        }
                        else
                        {
                            value = "0";
                        } // can be 0,F,f,N, and n
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a long (Int64) column in the DBF file.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of integer values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, long[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp4 = columnData; object argvalue4 = tmp4[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue4); tmp4[0] = Convert.ToInt64(argvalue4); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData(i).ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits an integer column in the DBF file.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of integer values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, int[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp5 = columnData; object argvalue5 = tmp5[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue5); tmp5[0] = Convert.ToInt32(argvalue5); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData(i).ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a short (Int16) column in the DBF file.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of short values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, short[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp6 = columnData; object argvalue6 = tmp6[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue6); tmp6[0] = Convert.ToInt16(argvalue6); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData(i).ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a byte column in the DBF file.
            /// </summary>
            /// <param name="columnName">Name of the column to edit.</param>
            /// <param name="columnData">Array of short values.</param>
            /// <exception cref="Exception"></exception>
            protected override void EditDatabaseColumn(string columnName, byte[] columnData)
            {
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired column named: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp7 = columnData; object argvalue7 = tmp7[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue7); tmp7[0] = Convert.ToByte(argvalue7); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + columnName + " is of type " + _storedColumnTypes[columnIndex].ToString() + ", not of type Double."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("The dbf file does not have: " + columnData.Count() + " records"); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Verify the DBF is closed
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Get the position of the value in the row
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                int lengthBegin = 0;
                for (int i = 0; i < columnIndex; i++)
                {
                    lengthBegin += _lengths[i]; 
                }
                lengthBegin += 1;
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                // Data as rows
                // ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    int startPosition;
                    for (int i = 0; i < _recordStartPositions.Count; i++)
                    {
                        startPosition = (int)(_recordStartPositions[i] - 1L); 
                        dbfReaderFs.Position = startPosition + lengthBegin;
                        value = columnData[i].ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData(i).ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                    }
                }
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Deletes a column from the DBF file by name. This rewrites the DBF file with the specified column removed.
            /// </summary>
            /// <param name="columnName">The name of the column to delete.</param>
            protected override void DeleteColumnFromDatabase(string columnName)
            {

                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex < 0) { return; }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex - 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen - _lengths[columnIndex]));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        for (int i = 0; i < _storedColumnNames.Count(); i++)
                        {
                            if (i == columnIndex)
                            {
                                _parentDbfReader.DbReader.BaseStream.Position += 32L;
                                continue;
                            }
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32));
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        int preFieldrecordLength = 0;
                        for (int j = 0; j < columnIndex; j++)
                        {
                            preFieldrecordLength += _lengths[j]; 
                        }
                        int postFieldrecordLength = 0;
                        for (int j = columnIndex + 1; j < _lengths.Count(); j++)
                        {
                            postFieldrecordLength += _lengths[j]; 
                        }
                        for (int i = 0; i < nRecords; i++)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(preFieldrecordLength + 1)); // The plus 1 is for the preceding byte that indicates deletion or not deleted.
                            _parentDbfReader.DbReader.BaseStream.Position += _lengths[columnIndex];
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(postFieldrecordLength));
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                File.Delete(_parentDatabase.DataBasePath);
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Deletes multiple columns from the DBF file rewriting the file with specified columns removed.
            /// </summary>
            /// <param name="columnsToDelete">An array of column names to remove form the DBF file.</param>
            protected override void DeleteColumnsFromDatabase(string[] columnsToDelete)
            {
                var saveColumn = new bool[(_storedColumnNames.Count())];
                int numberToDelete = 0;
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    if (columnsToDelete.Contains(_storedColumnNames[i]))
                    {
                        saveColumn[i] = false;
                        numberToDelete += 1;
                    }
                    else
                    {
                        saveColumn[i] = true;
                    }
                }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex - 32 * numberToDelete));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        short lengthToRemove = 0;
                        for (int i = 0; i < saveColumn.Count(); i++)
                        {
                            if (saveColumn[i] == false)
                                lengthToRemove += _lengths[i];
                        }
                        writeBwDbf.Write((short)(recordLen - lengthToRemove));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        for (int i = 0; i < _storedColumnNames.Count(); i++)
                        {
                            if (saveColumn[i] == true)
                            {
                                writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32));
                            }
                            else
                            {
                                _parentDbfReader.DbReader.BaseStream.Position += 32L;
                            }
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        for (int i = 0; i < nRecords; i++)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadByte()); // preceding byte that indicates deletion or not deleted.
                            for (int j = 0; j < saveColumn.Count(); j++)
                            {
                                if (saveColumn[j] == true)
                                {
                                    writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(_lengths[j]));
                                }
                                else
                                {
                                    _parentDbfReader.DbReader.BaseStream.Position += _lengths[j];
                                }
                            }
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                File.Delete(_parentDatabase.DataBasePath);
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                { 
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Adds a new column type byte[][] to the DBF file. Not implemented in this solution.
            /// </summary>
            /// <param name="columnName"></param>
            /// <param name="columnData"></param>
            /// <exception cref="NotImplementedException"></exception>
            protected override void AddColumnToDatabase(string columnName, byte[][] columnData)
            {
                throw new NotImplementedException("DBF does not support storage of byte arrays.");
            }

            /// <summary>
            /// Adds a new column type double[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, double[] columnData)
            {
                byte fieldLength = 19;
                if (columnName.Length > 10)
                { 
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                { 
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                { 
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        { 
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("F"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)11); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString("0.00000000000e+000", CultureInfo.InvariantCulture).PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                { 
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Adds a new column type float[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, float[] columnData)
            {
                byte fieldLength = 19;
                if (columnName.Length > 10)
                { 
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters.");
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name.");
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("F"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)11); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString("0.00000000000e+000", CultureInfo.InvariantCulture).PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Adds a new column type long[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, long[] columnData)
            {
                byte fieldLength = (byte)long.MinValue.ToString().Length;
                if (columnName.Length > 10)
                {
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name.");
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("N"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString().PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Adds a new column type int[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, int[] columnData)
            {
                byte fieldLength = (byte)int.MinValue.ToString().Length;
                if (columnName.Length > 10)
                {
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters.");
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }

                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("N"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString().PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Adds a new column type short[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, short[] columnData)
            {
                byte fieldLength = (byte)short.MinValue.ToString().Length;
                if (columnName.Length > 10)
                { 
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("N"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString().PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Adds a new column type byte[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, byte[] columnData)
            {
                byte fieldLength = 3;
                if (columnName.Length > 10)
                {
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("N"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString().PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Adds a new column type bool[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, bool[] columnData)
            {
                byte fieldLength = 1;
                if (columnName.Length > 10)
                {
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("L"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            if (columnData[i] == true)
                            {
                                asciiBytes = System.Text.Encoding.ASCII.GetBytes("1".PadRight(fieldLength, ' '));
                            }
                            else
                            {
                                asciiBytes = System.Text.Encoding.ASCII.GetBytes("0".PadRight(fieldLength, ' '));
                            }
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                    _parentDatabase.Open();
            }

            /// <summary>
            /// Adds a new column type string[] to the DBF file by creating a new version 
            /// with updated structure and content.
            /// </summary>
            /// <param name="columnName">The name of the column to add. Must be 10 characters or fewer.</param>
            /// <param name="columnData">An array of typed values representing the new column data.</param>
            /// <exception cref="Exception"></exception>
            protected override void AddColumnToDatabase(string columnName, string[] columnData)
            {
                if (columnName.Length > 10)
                {
                    throw new Exception("DBF name is too long, column name cannot be longer than 10 characters."); 
                }
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Field name " + columnName + " already exists in the dbf file, choose a different name."); 
                }
                if (_recordStartPositions.Count != columnData.Count())
                {
                    throw new Exception("Number of records in dbf file do not match the number of records in Values() array."); 
                }
                // 
                byte fieldLength = 0;
                int valueBytesLength;
                for (int i = 0; i < columnData.Count(); i++)
                {
                    valueBytesLength = System.Text.Encoding.UTF8.GetByteCount(columnData[i]);
                    if (valueBytesLength > 256)
                    {
                        throw new Exception("String value at index: " + i.ToString() + " contains too many characters for writing to the .dbf file format."); 
                    }

                    if (valueBytesLength > fieldLength)
                    {
                        fieldLength = (byte)valueBytesLength; 
                    }
                }
                // open stream to read the dbf
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // temporary file to write new dbf
                string tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf";
                if (File.Exists(tmpdbf))
                {
                    tmpdbf = Path.GetTempPath() + Guid.NewGuid().ToString() + ".dbf"; 
                }
                using (var writeFsDbf = new FileStream(tmpdbf, FileMode.Create))
                {
                    using (var writeBwDbf = new BinaryWriter(writeFsDbf))
                    {
                        _parentDbfReader.DbReader.BaseStream.Position = 0L;
                        // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        writeBwDbf.Write((byte)(DateTime.Now.Year - 1900)); // YY    
                        writeBwDbf.Write((byte)DateTime.Now.Month);       // MM 
                        writeBwDbf.Write((byte)DateTime.Now.Day);         // DD 
                        _parentDbfReader.DbReader.BaseStream.Position += 3L;
                        // 
                        int nRecords = _parentDbfReader.DbReader.ReadInt32(); // number of rows
                        writeBwDbf.Write(nRecords);
                        short firstDataRecordIndex = _parentDbfReader.DbReader.ReadInt16(); // always 32 + 32*number of fields + 1
                        writeBwDbf.Write((short)(firstDataRecordIndex + 32));
                        short recordLen = _parentDbfReader.DbReader.ReadInt16();
                        writeBwDbf.Write((short)(recordLen + fieldLength));
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(20));
                        // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                        while (_parentDbfReader.DbReader.PeekChar() != 13)
                        {
                            writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(32)); 
                        }
                        columnName = columnName.PadRight(10, '\0');
                        byte[] asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnName);
                        writeBwDbf.Write(asciiBytes); // FieldName
                        writeBwDbf.Write("C"); // FieldType
                        writeBwDbf.Write(0); // FieldDataAddress
                        writeBwDbf.Write(fieldLength); // FieldLength
                        writeBwDbf.Write((byte)0); // NDecimalPlaces
                        writeBwDbf.Write((byte)0); // FieldFlag
                        writeBwDbf.Write(0); // AutoIncrementValue
                        writeBwDbf.Write((byte)0); // AutoIncrementStep
                        for (int i = 1; i <= 8; i++)
                        {
                            writeBwDbf.Write((byte)0); // reserved
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // ''''''''''''''''''''''''''''''''''''''Data as rows'''''''''''''''''''''''''''
                        _parentDbfReader.DbReader.BaseStream.Seek(firstDataRecordIndex, SeekOrigin.Begin);
                        byte[] recordBytes;
                        for (int i = 0; i < nRecords; i++)
                        {
                            recordBytes = _parentDbfReader.DbReader.ReadBytes(recordLen);
                            asciiBytes = System.Text.Encoding.ASCII.GetBytes(columnData[i].ToString().PadRight(fieldLength, ' '));
                            writeBwDbf.Write(recordBytes);
                            writeBwDbf.Write(asciiBytes);
                        }
                        writeBwDbf.Write(_parentDbfReader.DbReader.ReadBytes(1));
                        // 
                        _parentDatabase.Close();
                    }
                }
                // 
                // update original
                try
                {
                    File.Delete(_parentDatabase.DataBasePath);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to save changes: " + ex.Message, ex);
                }
                // 
                File.Copy(tmpdbf, _parentDatabase.DataBasePath);
                // 
                LoadAttributeInfo();
                File.Delete(tmpdbf);
                // 
                if (wasOpen)
                {
                    _parentDatabase.Open(); 
                }
            }

            #endregion

            #region Database Cell Stuff

            /// <summary>
            /// Converts a raw string cell value to its appropriate .NET type based on the provided type.
            /// </summary>
            /// <param name="cellValue">The raw string value read from the DBF file.</param>
            /// <param name="columnType">The expected .NET type for the column (e.g., int, double, bool).</param>
            /// <returns>The value converted to the appropriate type, or <see cref="DBNull"/> if conversion fails.</returns>
            /// <exception cref="Exception">Thrown when the column type is not supported.</exception>
            private object ConvertCellValueToProperType(string cellValue, Type columnType)
            {
                switch (columnType)
                {
                    case var @case when @case == typeof(int):
                        {
                            int i;
                            if (int.TryParse(cellValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out i) == false) { return DBNull.Value; }
                            return i;
                        }
                    case var case1 when case1 == typeof(double):
                        {
                            double i;
                            if (double.TryParse(cellValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out i) == false) { return DBNull.Value; }
                            return i;
                        }
                    case var case2 when case2 == typeof(string):
                        {
                            return cellValue.Trim(' ');
                        }
                    case var case3 when case3 == typeof(bool):
                        {
                            switch (cellValue ?? "")
                            {
                                case "0":
                                case "F":
                                case "f":
                                case "N":
                                case "n":
                                    {
                                        return false;
                                    }
                                case "1":
                                case "T":
                                case "t":
                                case "Y":
                                case "y":
                                    {
                                        return true;
                                    }

                                default:
                                    {
                                        return false;
                                    }
                            }
                        }

                    default:
                        {
                            throw new Exception("Column Type not supported");
                        }
                }
            }

            /// <summary>
            /// Gets a typed value from the stored DBF database by column name and row index.
            /// </summary>
            /// <param name="storedColumnName">The name of the column.</param>
            /// <param name="storedRowIndex">The row index of the value.</param>
            /// <returns>The typed value converted from the raw DBF storage format.</returns>
            /// <exception cref="Exception">Thrown if the column name does not exist.</exception>
            protected override object GetStoredCell(string storedColumnName, int storedRowIndex)
            {
                if (storedRowIndex < 0 || storedRowIndex >= _recordStartPositions.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_recordStartPositions.Count - 1}.");
                }
                int columnIndex = Array.IndexOf(_storedColumnNames, storedColumnName);
                if (columnIndex == -1)
                {
                    throw new Exception("Column Name " + storedColumnName + " does not exist.");
                }
                return ConvertCellValueToProperType(ReadRawCellSafe(columnIndex, storedRowIndex), _storedColumnTypes[columnIndex]);
            }

            /// <summary>
            /// Gets a typed value from the stored DBF database by column and row indices.
            /// </summary>
            /// <param name="storedColumnIndex">The column index.</param>
            /// <param name="storedRowIndex">The row index.</param>
            /// <returns>The cell value at the specified position.</returns>
            protected override object GetStoredCell(int storedColumnIndex, int storedRowIndex)
            {
                if (storedColumnIndex < 0 || storedColumnIndex >= _storedColumnNames.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedColumnIndex), storedColumnIndex,
                        $"Column index must be between 0 and {_storedColumnNames.Length - 1}.");
                }
                if (storedRowIndex < 0 || storedRowIndex >= _recordStartPositions.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_recordStartPositions.Count - 1}.");
                }
                return ConvertCellValueToProperType(ReadRawCellSafe(storedColumnIndex, storedRowIndex), _storedColumnTypes[storedColumnIndex]);
            }

            /// <summary>
            /// Gets multiple typed values from the stored DBF database on arrays of column and row indices.
            /// </summary>
            /// <param name="storedColumnIndices">Array of column indices to retrieve from.</param>
            /// <param name="storedRowIndices">Array of row indices corresponding to each column.</param>
            /// <returns>An array of cell values at the specified positions.</returns>
            protected override object[] GetStoredCells(int[] storedColumnIndices, int[] storedRowIndices)
            {
                // Validate all indices before proceeding
                for (int i = 0; i < storedColumnIndices.Length; i++)
                {
                    if (storedColumnIndices[i] < 0 || storedColumnIndices[i] >= _storedColumnNames.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(storedColumnIndices),
                            $"Column index {storedColumnIndices[i]} at position {i} is out of range. Must be between 0 and {_storedColumnNames.Length - 1}.");
                    }
                }
                for (int i = 0; i < storedRowIndices.Length; i++)
                {
                    if (storedRowIndices[i] < 0 || storedRowIndices[i] >= _recordStartPositions.Count)
                    {
                        throw new ArgumentOutOfRangeException(nameof(storedRowIndices),
                            $"Row index {storedRowIndices[i]} at position {i} is out of range. Must be between 0 and {_recordStartPositions.Count - 1}.");
                    }
                }

                var cells = new object[(storedColumnIndices.Count())];
                for (int i = 0; i < storedColumnIndices.Count(); i++)
                {
                    cells[i] = ConvertCellValueToProperType(ReadRawCellSafe(storedColumnIndices[i], storedRowIndices[i]), _storedColumnTypes[storedColumnIndices[i]]);
                }
                return cells;
            }

            /// <summary>
            /// Reads a raw string value from the DBF file using the specified column and row index, reopening the stream if necessary.
            /// </summary>
            /// <param name="col">The column index. </param>
            /// <param name="row">The row index.</param>
            /// <returns>A raw string representation of the cell value.</returns>
            private string ReadRawCellSafe(int col, int row)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // much slower for some reason _dbfreader.BaseStream.Position = _firstDataRecordIndex + 1 + (row * _recordLength) + _Positions(col)
                _parentDbfReader.DbReader.BaseStream.Position = _recordStartPositions[row] + _positions[col];
                string result = System.Text.Encoding.UTF8.GetString(_parentDbfReader.DbReader.ReadBytes(_lengths[col]), 0, _lengths[col]).Trim('\0');
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
                return result;
            }

            /// <summary>
            /// Reads a raw string value directly from the DBF file stream without verifying or reopening file access state.
            /// </summary>
            /// <param name="columnIndex">The column index of the cell.</param>
            /// <param name="rowIndex">The row index of the cell.</param>
            /// <returns>A raw string representation of the cell value.</returns>
            public string ReadRawCellUnsafe(int columnIndex, int rowIndex)
            {
                // much slower for some reason _dbfreader.BaseStream.Position = _firstDataRecordIndex + 1 + (Row * _recordLength) + _Positions(Column)
                _parentDbfReader.DbReader.BaseStream.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                return System.Text.Encoding.UTF8.GetString(_parentDbfReader.DbReader.ReadBytes(_lengths[columnIndex]), 0, _lengths[columnIndex]).Trim('\0');
            }

            /// <summary>
            /// Edits multiple DBF database cells by column name, row index, and new value.
            /// </summary>
            /// <param name="columnEditNames">The column names to edit.</param>
            /// <param name="rowIndices">The row indices to apply edits to.</param>
            /// <param name="cellValues">The values to assign.</param>
            /// <exception cref="Exception">Thrown if a column name is not found.</exception>
            protected override void EditDatabaseCells(string[] columnEditNames, int[] rowIndices, object[] cellValues)
            {
                var columnIndices = new int[(columnEditNames.Count())];
                for (int i = 0; i < columnEditNames.Count(); i++)
                {
                    columnIndices[i] = Array.IndexOf(_storedColumnNames, columnEditNames[i]);
                    if (columnIndices[i] == -1)
                    {
                        throw new Exception("The column: " + columnEditNames[i] + " does not exist in the database."); 
                    }
                }
                EditDatabaseCells(columnIndices, rowIndices, cellValues);
            }

            /// <summary>
            /// Edits multiple DBF database cells using column and row indices and new values.
            /// </summary>
            /// <param name="columnIndices">The indices of the columns to edit.</param>
            /// <param name="rowIndices">The indices of the rows to edit.</param>
            /// <param name="cellValues">The new values to be written.</param>
            /// <exception cref="Exception">Thrown if a type mismatch occurs or conversion fails.</exception>
            protected override void EditDatabaseCells(int[] columnIndices, int[] rowIndices, object[] cellValues)
            {
                // 
                for (int i = 0; i < columnIndices.Count(); i++)
                {
                    if (ConvertToColumnType(_storedColumnTypes[columnIndices[i]], ref cellValues[i]) == false)
                    {
                        throw new Exception("The desired Column: " + _storedColumnNames[i] + " is of type " + _storedColumnTypes[columnIndices[i]].ToString() + " not of type " + cellValues[i].GetType().ToString() + "."); 
                    }
                }
                // 
                var sorted = columnIndices.ToList().Select((x, i) => new KeyValuePair<int, int>(x, i)).OrderBy(x => x.Key).ToList();
                var idx = sorted.Select(x => x.Value).ToList();
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    // update yymmdd to indicate date of edit 
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // 
                    string value;
                    byte[] asciiBytes;
                    string formatString;
                    byte nDecimals;
                    for (int i = 0; i < idx.Count; i++)
                    {
                        switch (_storedColumnTypes[columnIndices[idx[i]]])
                        {
                            case var @case when @case == typeof(byte):
                            case var case1 when case1 == typeof(short):
                            case var case2 when case2 == typeof(ushort):
                            case var case3 when case3 == typeof(int):
                            case var case4 when case4 == typeof(uint):
                            case var case5 when case5 == typeof(long):
                            case var case6 when case6 == typeof(ulong):
                                {
                                    do
                                    {
                                        dbfReaderFs.Position = _recordStartPositions[rowIndices[idx[i]]] + _positions[columnIndices[idx[i]]];
                                        value = cellValues[idx[i]].ToString().PadLeft(_lengths[columnIndices[idx[i]]], ' '); // probably needs space padding.value = DoubleData.ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        i += 1;
                                        if (i > idx.Count - 1) { break; }
                                    }
                                    while (columnIndices[idx[i]] == columnIndices[idx[i - 1]]);
                                    i -= 1;
                                    break;
                                }
                            case var case7 when case7 == typeof(float):
                            case var case8 when case8 == typeof(double):
                                {
                                    dbfReaderFs.Position = 32 + columnIndices[idx[i]] * 32 + 17;
                                    nDecimals = (byte)dbfReaderFs.ReadByte();
                                    if (_lengths[columnIndices[idx[i]]] - nDecimals < 7)
                                    {
                                        formatString = "G";
                                    }
                                    else
                                    {
                                        formatString = "0.";
                                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                                        formatString = formatString + "e+000";
                                    }
                                    do
                                    {
                                        dbfReaderFs.Position = _recordStartPositions[rowIndices[idx[i]]] + _positions[columnIndices[idx[i]]];
                                        value = Convert.ToDouble(cellValues[idx[i]]).ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[columnIndices[idx[i]]], ' ');
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        i += 1;
                                        if (i > idx.Count - 1) { break; }
                                    }
                                    while (columnIndices[idx[i]] == columnIndices[idx[i - 1]]);
                                    i -= 1;
                                    break;
                                }
                            case var case9 when case9 == typeof(string):
                                {
                                    do
                                    {
                                        dbfReaderFs.Position = _recordStartPositions[rowIndices[idx[i]]] + _positions[columnIndices[idx[i]]];
                                        string strValue = (string)cellValues[idx[i]];
                                        if (strValue.Length > _lengths[columnIndices[idx[i]]])
                                        {
                                            value = Convert.ToString(strValue.Substring(0, _lengths[columnIndices[idx[i]]]));
                                        }
                                        else
                                        {
                                            value = Convert.ToString(strValue.PadRight(_lengths[columnIndices[idx[i]]], ' '));
                                        }
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        i += 1;
                                        if (i > idx.Count - 1) { break; }
                                    }
                                    while (columnIndices[idx[i]] == columnIndices[idx[i - 1]]);
                                    i -= 1;
                                    break;
                                }
                            case var case10 when case10 == typeof(bool):
                                {
                                    do
                                    {
                                        dbfReaderFs.Position = _recordStartPositions[rowIndices[idx[i]]] + _positions[columnIndices[idx[i]]];
                                        if (Convert.ToBoolean(cellValues[idx[i]]))
                                        {
                                            value = "1"; // can be 1,T,t,Y,and y
                                        }
                                        else
                                        {
                                            value = "0";
                                        } // can be 0,F,f,N, and n
                                        asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                                        dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                                        i += 1;
                                        if (i > idx.Count - 1) { break; }
                                    }
                                    while (columnIndices[idx[i]] == columnIndices[idx[i - 1]]);
                                    i -= 1;
                                    break;
                                }
                                // Case Else

                        }
                    }

                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new double value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, double cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue8 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue8); cellValue = Convert.ToDouble(argvalue8); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                _parentDbfReader.DbReader.BaseStream.Position = 32 + columnIndex * 32 + 17;
                byte nDecimals;
                nDecimals = _parentDbfReader.DbReader.ReadByte();
                _parentDatabase.Close();
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit 
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // find format of field
                    string value;
                    byte[] asciiBytes;
                    string formatString;
                    if (_lengths[columnIndex] - nDecimals < 7)
                    {
                        formatString = "G";
                    }
                    else
                    {
                        formatString = "0.";
                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                        formatString = formatString + "e+000";
                    }

                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[columnIndex], ' ');
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new float value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, float cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue9 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue9); cellValue = Convert.ToSingle(argvalue9); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                _parentDbfReader.DbReader.BaseStream.Position = 32 + columnIndex * 32 + 17;
                byte nDecimals;
                nDecimals = _parentDbfReader.DbReader.ReadByte();
                _parentDatabase.Close();
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit 
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);
                    // find format of field
                    string value;
                    byte[] asciiBytes;
                    string formatString;
                    if (_lengths[columnIndex] - nDecimals < 7)
                    {
                        formatString = "G";
                    }
                    else
                    {
                        formatString = "0.";
                        formatString = formatString.PadRight(formatString.Length + nDecimals, '0');
                        formatString = formatString + "e+000";
                    }

                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString(formatString, CultureInfo.InvariantCulture).PadRight(_lengths[columnIndex], ' ');
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new string value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, string cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue10 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue10); cellValue = Convert.ToString(argvalue10); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;

                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    if (cellValue.Length > _lengths[columnIndex])
                    {
                        value = cellValue.Substring(0, _lengths[columnIndex]);
                    }
                    else
                    {
                        value = cellValue.PadRight(_lengths[columnIndex], ' ');
                    }
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new int value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, int cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue11 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue11); cellValue = Convert.ToInt32(argvalue11); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }

                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;
                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData.ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new long value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, long cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue12 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue12); cellValue = Convert.ToInt64(argvalue12); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;
                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData.ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new byte value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue13 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue13); cellValue = Convert.ToByte(argvalue13); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;
                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData.ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new short value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, short cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue14 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue14); cellValue = Convert.ToInt16(argvalue14); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;

                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    value = cellValue.ToString().PadLeft(_lengths[columnIndex], ' '); // probably needs space padding.value = DoubleData.ToString(FormatString).PadLeft(_Lengths(fieldindex), Chr(32))
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Edits a single DBF cell with a new boolean value, updating the binary stream and header date.
            /// </summary>
            /// <param name="columnIndex">The index of the column to edit.</param>
            /// <param name="rowIndex">The index of the row to edit.</param>
            /// <param name="cellValue">The new value to write to the cell.</param>
            /// <exception cref="Exception">Thrown if the column index is out of bounds or if the value does not match the expected column type.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, bool cellValue)
            {
                if (columnIndex >= _storedColumnNames.Count())
                {
                    throw new Exception("The column index does not exist in the database."); 
                }
                bool localConvertToColumnType() { object argvalue15 = cellValue; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue15); cellValue = Convert.ToBoolean(argvalue15); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Column: " + _storedColumnNames[columnIndex] + " is of type " + _storedColumnTypes[columnIndex].ToString() + " not of type Double."); 
                }
                // ''''''''''''''''''''''''''''''''''''''''Get the number of decimals from the Main File Header''''''''''''''''''''''''''''''''''''''''''''''''''
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == true)
                {
                    _parentDatabase.Close(); 
                }
                // ''''''''''''''''''''''''''''''''''''''Get the position of the value in the row'''''''''''''''''''''''''''
                // update yymmdd to indicate date of edit 
                using (var dbfReaderFs = new FileStream(_parentDatabase.DataBasePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var yymmdd = new byte[3];
                    yymmdd[0] = (byte)(DateTime.Now.Year - 1900); // YY 
                    yymmdd[1] = (byte)DateTime.Now.Month;       // MM 
                    yymmdd[2] = (byte)DateTime.Now.Day;         // DD 
                    dbfReaderFs.Position = 1L;
                    dbfReaderFs.Write(yymmdd, 0, 3);

                    string value;
                    byte[] asciiBytes;

                    // edit the appropriate part of the dbf file
                    dbfReaderFs.Position = _recordStartPositions[rowIndex] + _positions[columnIndex];
                    if (cellValue)
                    {
                        value = "1"; // can be 1,T,t,Y,and y
                    }
                    else
                    {
                        value = "0";
                    } // can be 0,F,f,N, and n
                    asciiBytes = System.Text.Encoding.ASCII.GetBytes(value);
                    dbfReaderFs.Write(asciiBytes, 0, asciiBytes.Count());
                }
                if (wasOpen == true)
                {
                    _parentDatabase.Open(); 
                }
            }

            /// <summary>
            /// Throws a not supported exception when attempting to store byte arrays in DBF format.
            /// </summary>
            /// <param name="columnIndex">The column index.</param>
            /// <param name="rowIndex">The row index.</param>
            /// <param name="cellValue">The byte array value.</param>
            /// <exception cref="NotImplementedException">Always thrown. DBF does not support byte array values.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte[] cellValue)
            {
                throw new NotImplementedException("DBF does not support storage of byte arrays.");
            }

            /// <summary>
            /// Throws a not supported exception when attempting to store DateTime directly in DBF format.
            /// </summary>
            /// <param name="columnIndex">The column index.</param>
            /// <param name="rowIndex">The row index.</param>
            /// <param name="cellValue">The DateTime value.</param>
            /// <exception cref="NotImplementedException">Always thrown. DBF DateTime editing is not implemented.</exception>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, DateTime cellValue)
            {
                throw new NotImplementedException("DBF DateTime editing is not implemented.");
            }
            #endregion

            /// <summary>
            /// Loads metadata from the DBF file, including number of rows, field definitions, and record start positions.
            /// </summary>
            private void LoadAttributeInfo()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                _parentDbfReader.DbReader.BaseStream.Position = 0L;
                // ''''''''''''''''''''''''''''''''''''''Table Header (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                byte[] bytes = _parentDbfReader.DbReader.ReadBytes(32);
                _storedNumberOfRows = BitConverter.ToInt32(bytes, 4); // number of rows
                _firstDataRecordIndex = BitConverter.ToInt16(bytes, 8); // always 32 + 32*number of fields + 1
                _recordLength = BitConverter.ToInt16(bytes, 10);
                int totalColumns = (_firstDataRecordIndex - 32 - 1) / 32;
                // 
                _recordStartPositions = new List<long>(_storedNumberOfRows);
                for (long i = 0L; i < _storedNumberOfRows; i++)
                {
                    _parentDbfReader.DbReader.BaseStream.Position = _firstDataRecordIndex + i * _recordLength;
                    _recordStartPositions.Add(_firstDataRecordIndex + 1 + i * _recordLength);
                }
                // ''''''''''''''''''''''''''''''''''''''Field Sub-records (always 32 bytes from 0 to 31)'''''''''''''''''''''''''''
                _parentDbfReader.DbReader.BaseStream.Position = 32L;
                // create the table columns
                _lengths = new byte[totalColumns];
                _positions = new short[totalColumns];
                _storedColumnNames = new string[totalColumns];
                _storedColumnTypes = new Type[totalColumns];

                for (int col = 1; col <= totalColumns; col++)
                {
                    bytes = _parentDbfReader.DbReader.ReadBytes(32);
                    _storedColumnNames[col - 1] = System.Text.Encoding.ASCII.GetString(bytes, 0, 10).Trim('\0');
                    // field type
                    switch (System.Text.Encoding.UTF8.GetString(bytes, 11, 1) ?? "")
                    {
                        case "I":
                            {
                                _storedColumnTypes[col - 1] = typeof(int);
                                break;
                            }
                        case "N":
                            {
                                if (bytes[17] > 0) // number of decimals
                                {
                                    _storedColumnTypes[col - 1] = typeof(double);
                                }
                                else
                                {
                                    _storedColumnTypes[col - 1] = typeof(int);
                                }

                                break;
                            }
                        case "F":
                        case "B":
                            {
                                _storedColumnTypes[col - 1] = typeof(double);
                                break;
                            }
                        case "L":
                            {
                                _storedColumnTypes[col - 1] = typeof(bool);
                                break;
                            }
                        case "C":
                            {
                                _storedColumnTypes[col - 1] = typeof(string);
                                break;
                            }

                        default:
                            {
                                _storedColumnTypes[col - 1] = typeof(string);
                                break;
                            }
                    }

                    // field length
                    _lengths[col - 1] = bytes[16];
                    if (col == 1)
                    {
                        _positions[col - 1] = 0;
                    }
                    else
                    {
                        _positions[col - 1] = (short)(_positions[col - 2] + _lengths[col - 2]);
                    }

                }

                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

        }

        /// <summary>
        /// Creates a new DBF file at the specified path using the given column headers and record data arrays.
        /// </summary>
        /// <param name="outputPath">The output path to save the DBF file. Must end with ".dbf".</param>
        /// <param name="columnHeaders">List of column names. Each name must be 10 characters or fewer.</param>
        /// <param name="recordData">
        /// A list of arrays, one for each column. Each array must be a supported type: <c>int[]</c>, <c>double[]</c>, <c>string[]</c>, etc.
        /// The number of elements in each array must match across all columns.
        /// </param>
        /// <param name="overwrite">If <c>true</c>, an existing file at the output path will be deleted and overwritten.</param>
        /// <exception cref="Exception">
        /// Thrown if the file extension is invalid, a DBF file already exists and overwrite is false, or an error occurs while writing.
        /// </exception>
        public static void CreateDbf(string outputPath, List<string> columnHeaders, List<Array> recordData, bool overwrite = true)
        {
            if (Path.GetExtension(outputPath).ToLower() != ".dbf")
            { 
                throw new Exception("DBF path does not contain the proper extension (.dbf).");
            }
            if (File.Exists(outputPath) == true)
            {
                if (overwrite == true)
                {
                    File.Delete(outputPath);
                }
                else
                {
                    throw new Exception("DBF file: " + Path.GetFileName(outputPath) + " already exists in the chosen location.");
                }
            }
            // 
            try
            {
                var fs = new FileStream(outputPath, FileMode.Create);
                // dbf header (write now to fill space, then re-write again later)
                var header = new byte[32];
                // dbf file type
                header[0] = 3;
                // last updated      
                header[1] = (byte)(DateTime.Now.Year - 1900); // YY 
                header[2] = (byte)DateTime.Now.Month;       // MM 
                header[3] = (byte)DateTime.Now.Day;         // DD 
                                                            // code page mark(NFI)
                header[29] = 87;
                header[29] = 57;
                header[29] = 0;
                fs.Write(header, 0, 32);
                // fields          
                var dbfFields = new DbfField[recordData.Count];
                short recordLength = 1;
                for (int column = 0; column < recordData.Count; column++)
                {
                    var fieldbytes = new byte[32];
                    // column name
                    string columnName = columnHeaders[column];
                    if (columnName.Length > 10)
                    { 
                        columnName = columnName.Substring(0, 10); 
                    }
                    Array.Copy(System.Text.Encoding.UTF8.GetBytes(columnName), 0, fieldbytes, 0, System.Text.Encoding.UTF8.GetBytes(columnName).Length);
                    // column type        
                    switch (recordData[column].GetType()) 
                    {
                        case var @case when @case == typeof(int[]):
                        case var case1 when case1 == typeof(uint[]):
                            {
                                dbfFields[column].TypeId = "N";
                                dbfFields[column].Length = int.MinValue.ToString().Length;
                                dbfFields[column].NumDecimal = 0;
                                dbfFields[column].WriteField = true;
                                break;
                            }
                        case var case2 when case2 == typeof(short[]):
                        case var case3 when case3 == typeof(ushort[]):
                            {
                                dbfFields[column].TypeId = "N";
                                dbfFields[column].Length = short.MinValue.ToString().Length;
                                dbfFields[column].NumDecimal = 0;
                                dbfFields[column].WriteField = true;
                                break;
                            }
                        case var case4 when case4 == typeof(byte):
                            {
                                dbfFields[column].TypeId = "N";
                                dbfFields[column].Length = 3;
                                dbfFields[column].NumDecimal = 0;
                                dbfFields[column].WriteField = true;
                                break;
                            }
                        case var case5 when case5 == typeof(double[]):
                        case var case6 when case6 == typeof(float[]):
                            {
                                dbfFields[column].TypeId = "F";
                                dbfFields[column].Length = 19;
                                dbfFields[column].NumDecimal = 11;
                                dbfFields[column].WriteField = true;
                                break;
                            }
                        case var case7 when case7 == typeof(bool):
                            {
                                dbfFields[column].TypeId = "L";
                                dbfFields[column].Length = 1;
                                dbfFields[column].NumDecimal = 0;
                                dbfFields[column].WriteField = true;
                                break;
                            }
                        case var case8 when case8 == typeof(string[]):
                            {
                                string[] col = (string[])recordData[column];
                                dbfFields[column].TypeId = "C";
                                // compute the maximum character length used in the table
                                dbfFields[column].Length = 0;
                                for (int i = 0; i < col.Length; i++)
                                {
                                    if (string.IsNullOrEmpty(col[i])) 
                                    {
                                    }
                                    // null data
                                    else
                                    {
                                        // character data
                                        string cellText = col[i];
                                        int cellBytesLength = System.Text.Encoding.UTF8.GetByteCount(cellText);
                                        if (cellBytesLength > dbfFields[column].Length)
                                            dbfFields[column].Length = cellBytesLength;
                                    }
                                }
                                dbfFields[column].NumDecimal = 0;
                                // only write fields with length less or equal to 256 characters
                                dbfFields[column].WriteField = dbfFields[column].Length <= 256;
                                break;
                            }

                        default:
                            {
                                // only write primitive fields (skip the feature column)
                                dbfFields[column].WriteField = false;
                                break;
                            }
                    }
                    // 
                    if (dbfFields[column].WriteField)
                    {
                        fieldbytes[11] = System.Text.Encoding.UTF8.GetBytes(dbfFields[column].TypeId)[0];
                        fieldbytes[16] = (byte)dbfFields[column].Length;
                        fieldbytes[17] = (byte)dbfFields[column].NumDecimal;
                        recordLength = (short)(recordLength + dbfFields[column].Length);
                        // 
                        // write the field information
                        fs.Write(fieldbytes, 0, 32);
                    }
                }
                // blank space at the end of the header (spec say use 0, but xl says use 13)     
                fs.WriteByte(13);
                // note the data starting byte position 
                short firstRecordPosition = (short)fs.Position;
                // write out the feature datatable
                int recordCount = 0;
                for (int i = 0; i < recordData[0].Length; i++)
                {
                    // deleted record indicator  (32 = space (' ') for not deleted, 42 = asterisk ('*') for deleted)     
                    // http://www.dbase.com/Knowledgebase/INT/db7_file_fmt.htm   
                    fs.WriteByte(32);
                    for (int column = 0; column < recordData.Count; column++)
                    {
                        if (dbfFields[column].WriteField)
                        {
                            string cellText;
                            switch (recordData[column].GetType())
                            {
                                case var case9 when case9 == typeof(int[]):
                                case var case10 when case10 == typeof(uint[]):
                                    {
                                        cellText = ((int[])recordData[column])[i].ToString();
                                        break;
                                    }
                                case var case11 when case11 == typeof(short[]):
                                case var case12 when case12 == typeof(ushort[]):
                                    {
                                        cellText = ((short[])recordData[column])[i].ToString();
                                        break;
                                    }
                                case var case13 when case13 == typeof(byte[]):
                                    {
                                        cellText = ((byte[])recordData[column])[i].ToString();
                                        break;
                                    }
                                case var case14 when case14 == typeof(float[]):
                                    {
                                        cellText = ((float[])recordData[column])[i].ToString("0.00000000000e+000", CultureInfo.InvariantCulture);
                                        break;
                                    }
                                case var case15 when case15 == typeof(double[]):
                                    {
                                        cellText = ((double[])recordData[column])[i].ToString("0.00000000000e+000", CultureInfo.InvariantCulture);
                                        break;
                                    }
                                case var case16 when case16 == typeof(bool[]):
                                    {
                                        if (((bool[])recordData[column])[i] == true)
                                        {
                                            cellText = "1";
                                        }
                                        else
                                        {
                                            cellText = "0";
                                        }

                                        break;
                                    }

                                default:
                                    {
                                        cellText = ((string[])recordData[column])[i];
                                        break;
                                    }
                            }
                            // get the bytes for the cell characters - use the UTF8 instead of ascii to handle extended characters (above 127)
                            byte[] cellbytes = System.Text.Encoding.UTF8.GetBytes(cellText);
                            // write them to the filestream
                            fs.Write(cellbytes, 0, cellbytes.Count());
                            // write blanks at the end to make up to the column length
                            for (int n = cellbytes.Length + 1; n <= dbfFields[column].Length; n++)
                            {
                                fs.WriteByte(32); 
                            }
                        }
                    }
                    recordCount += 1;
                }
                // eof dbf file marker
                fs.WriteByte(26);

                fs.Position = 4L;
                fs.Write(BitConverter.GetBytes(recordCount), 0, 4);
                fs.Write(BitConverter.GetBytes(firstRecordPosition), 0, 2);
                fs.Write(BitConverter.GetBytes(recordLength), 0, 2);
                // all done
                fs.Close();
            }
            catch
            {
                // an error occurred
                throw new Exception("An error writing shapefile attributes file: " + outputPath);
            }
        }

        /// <summary>
        /// Creates a new DBF file at the specified path from a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="outputPath">The file path to save the DBF file. Must end with ".dbf".</param>
        /// <param name="dt">The <see cref="DataTable"/> containing the data to export.</param>
        /// <param name="overwrite">If <c>true</c>, will delete and overwrite the DBF file if it already exists.</param>
        /// <exception cref="Exception">
        /// Thrown if the file extension is not ".dbf", if the file exists and overwrite is <c>false</c>, or if an error occurs during export.
        /// </exception>
        public static void CreateDbf(string outputPath, DataTable dt, bool overwrite = true)
        {
            if (Path.GetExtension(outputPath).ToLower() != ".dbf")
            { 
                throw new Exception("DBF path does not contain the proper extension (.dbf).");
            }
            if (File.Exists(outputPath) == true)
            {
                if (overwrite == true)
                {
                    File.Delete(outputPath);
                }
                else
                {
                    throw new Exception("DBF file: " + Path.GetFileName(outputPath) + " already exists in the chosen location.");
                }
            }
            // 
            try
            {
                var fs = new FileStream(outputPath, FileMode.Create);
                // dbf header (write now to fill space, then re-write again later)
                var header = new byte[32];
                // dbf file type
                header[0] = 3;
                // last updated      
                header[1] = (byte)(DateTime.Now.Year - 1900); // YY 
                header[2] = (byte)DateTime.Now.Month;       // MM 
                header[3] = (byte)DateTime.Now.Day;         // DD 
                                                            // code page mark(NFI)
                header[29] = 87;
                header[29] = 57;
                header[29] = 0;
                fs.Write(header, 0, 32);
                // fields          
                var dbfFields = new DbfField[dt.Columns.Count];
                short recordLength = 1;
                for (int column = 0; column < dt.Columns.Count; column++)
                {
                    var col = dt.Columns[column];
                    var fieldbytes = new byte[32];
                    // column name
                    string colname = col.ColumnName;
                    if (colname.Length > 10)
                        colname = colname.Substring(0, 10);
                    Array.Copy(System.Text.Encoding.UTF8.GetBytes(colname), 0, fieldbytes, 0, System.Text.Encoding.UTF8.GetBytes(colname).Length);
                    // column type        
                    if (ReferenceEquals(col.DataType, typeof(int)) || ReferenceEquals(col.DataType, typeof(uint)))
                    {
                        dbfFields[column].TypeId = "N";
                        dbfFields[column].Length = int.MinValue.ToString().Length;
                        dbfFields[column].NumDecimal = 0;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(long)) || ReferenceEquals(col.DataType, typeof(ulong)))
                    {
                        dbfFields[column].TypeId = "N";
                        dbfFields[column].Length = long.MinValue.ToString().Length;
                        dbfFields[column].NumDecimal = 0;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(short)) || ReferenceEquals(col.DataType, typeof(ushort)))
                    {
                        dbfFields[column].TypeId = "N";
                        dbfFields[column].Length = short.MinValue.ToString().Length;
                        dbfFields[column].NumDecimal = 0;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(byte)))
                    {
                        dbfFields[column].TypeId = "N";
                        dbfFields[column].Length = 3;
                        dbfFields[column].NumDecimal = 0;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(bool)))
                    {
                        dbfFields[column].TypeId = "L";
                        dbfFields[column].Length = 1;
                        dbfFields[column].NumDecimal = 0;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(double)) || ReferenceEquals(col.DataType, typeof(float)))
                    {
                        dbfFields[column].TypeId = "F";
                        dbfFields[column].Length = 19;
                        dbfFields[column].NumDecimal = 11;
                        dbfFields[column].WriteField = true;
                    }
                    else if (ReferenceEquals(col.DataType, typeof(string)))
                    {
                        dbfFields[column].TypeId = "C";
                        // compute the maximum character length used in the table
                        dbfFields[column].Length = 0;
                        foreach (DataRow row in dt.Rows)
                        {
                            if (DBNull.Value.Equals(row[column]))
                            {
                            }
                            // null data
                            else
                            {
                                // character data
                                string cellText = Convert.ToString(row[column]);
                                int cellBytesLength = System.Text.Encoding.UTF8.GetByteCount(cellText);
                                if (cellBytesLength > dbfFields[column].Length)
                                    dbfFields[column].Length = cellBytesLength;
                            }
                        }
                        dbfFields[column].NumDecimal = 0;
                        // only write fields with length less or equal to 256 characters
                        dbfFields[column].WriteField = dbfFields[column].Length <= 256;
                    }
                    else
                    {
                        // only write primitive fields (skip the feature column)
                        dbfFields[column].WriteField = false;
                    }
                    // 
                    if (dbfFields[column].WriteField)
                    {
                        fieldbytes[11] = System.Text.Encoding.UTF8.GetBytes(dbfFields[column].TypeId)[0];
                        fieldbytes[16] = (byte)dbfFields[column].Length;
                        fieldbytes[17] = (byte)dbfFields[column].NumDecimal;
                        recordLength = (short)(recordLength + dbfFields[column].Length);
                        // 
                        // write the field information
                        fs.Write(fieldbytes, 0, 32);
                    }
                }
                // blank space at the end of the header (spec say use 0, but xl says use 13)     
                fs.WriteByte(13);
                // note the data starting byte position 
                short firstRecordPosition = (short)fs.Position;
                // write out the feature datatable
                int recordCount = 0;
                foreach (DataRow row in dt.Rows)
                {
                    // deleted record indicator  (32 = space (' ') for not deleted, 42 = asterisk ('*') for deleted)     
                    // http://www.dbase.com/Knowledgebase/INT/db7_file_fmt.htm     
                    fs.WriteByte(32);
                    for (int c = 0; c < dt.Columns.Count; c++)
                    {
                        if (dbfFields[c].WriteField)
                        {
                            string cellText;
                            if (row[c] is DBNull)
                            {
                                // cell is blank
                                cellText = "";
                            }
                            else if (row[c] is double || row[c] is float)
                            {
                                // col is floating point (single or double)
                                cellText = Convert.ToDouble(row[c]).ToString("0.00000000000e+000", CultureInfo.InvariantCulture);
                            }
                            else if (row[c] is int)
                            {
                                // col is integer
                                cellText = row[c].ToString();
                            }
                            else if (row[c] is bool)
                            {
                                if (Convert.ToBoolean(row[c]) == true)
                                {
                                    cellText = "1";
                                }
                                else
                                {
                                    cellText = "0";
                                }
                            }
                            else
                            {
                                // col is string
                                cellText = Convert.ToString(row[c]);
                            }
                            // get the bytes for the cell characters - use the UTF8 instead of ascii to handle extended characters (above 127)
                            byte[] cellbytes = System.Text.Encoding.UTF8.GetBytes(cellText);
                            // write them to the filestream
                            fs.Write(cellbytes, 0, cellbytes.Count());
                            // write blanks at the end to make up to the column length
                            for (int n = cellbytes.Length + 1; n <= dbfFields[c].Length; n++)
                                fs.WriteByte(32);
                        }
                    }
                    recordCount += 1;
                }
                // eof dbf file marker
                fs.WriteByte(26);
                // go back and update the header
                fs.Position = 4L;
                fs.Write(BitConverter.GetBytes(recordCount), 0, 4);
                fs.Write(BitConverter.GetBytes(firstRecordPosition), 0, 2);
                fs.Write(BitConverter.GetBytes(recordLength), 0, 2);
                // all done
                fs.Close();
            }
            catch (Exception ex)
            {
                // an error occurred
                throw new Exception("An error occurred when writing DBF file: " + outputPath + Environment.NewLine + ex.Message);
            }
        }
    }
}