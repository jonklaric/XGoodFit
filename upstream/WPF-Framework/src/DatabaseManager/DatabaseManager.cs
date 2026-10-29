using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace DatabaseManager
{
    /// <summary>
    /// Abstract base class for managing database files, providing shared functionality such as event hooks and static helpers.
    /// </summary>
    public abstract class DatabaseManager
    {
        /// <summary>
        /// Indicates whether the database is currently open. 
        /// </summary>
        protected bool _dataBaseOpen = false;

        /// <summary>
        /// Path to the database file.
        /// </summary>
        protected string _dataBasePath;

        /// <summary>
        /// Array of table names in the database.
        /// </summary>
        protected string[] _tableNames;

        /// <summary>
        /// Gets the file path of the database.
        /// </summary>
        public string DataBasePath
        {
            get
            {
                return _dataBasePath;
            }
        }

        /// <summary>
        /// Gets whether the database is currently open.
        /// </summary>
        public bool DataBaseOpen
        {
            get
            {
                return _dataBaseOpen;
            }
        }

        /// <summary>
        /// Gets the list of table names contained in the database.
        /// </summary>
        public string[] TableNames
        {
            get
            {
                return _tableNames;
            }
        }

        #region Events

        /// <summary>
        /// Raised when edits are saved to a table.
        /// </summary>
        public event EditsSavedEventHandler EditsSaved;

        /// <summary>
        /// Delegate signature for the <see cref="EditsSaved"/> event.
        /// </summary>
        /// <param name="tableName">The name of the table whose edits were saved.</param>
        /// <param name="editsSaved">The list of edits that were saved to the database.</param>
        public delegate void EditsSavedEventHandler(string tableName, List<TableEdit> editsSaved);

        /// <summary>
        /// Raises the <see cref="EditsSaved"/> event after edits have been applied to the database.
        /// </summary>
        /// <param name="tableName">The name of the table whose edits were saved.</param>
        /// <param name="editsSaved">The list of edits that were saved to the database.</param>
        public void OnEditsSaved(string tableName, List<TableEdit> editsSaved)
        {
            EditsSaved?.Invoke(tableName, editsSaved);
        }

        /// <summary>
        /// Raised before edits are saved, allowing cancellation of the save operation.
        /// </summary>
        public event PreviewEditsSavedEventHandler PreviewEditsSaved;

        /// <summary>
        /// Delegate signature for the <see cref="PreviewEditsSaved"/> event.
        /// </summary>
        /// <param name="tableName">The name of the table being saved.</param>
        /// <param name="cancel">Reference parameter that can be set to true to cancel the save operation.</param>
        public delegate void PreviewEditsSavedEventHandler(string tableName, ref bool cancel);

        /// <summary>
        /// Raises the <see cref="PreviewEditsSaved"/> event before edits are applied to the database.
        /// </summary>
        /// <param name="tableName">The name of the table being saved.</param>
        /// <param name="cancel">Reference parameter that subscribers can set to true to cancel the save operation.</param>
        public void OnPreviewEditsSaved(string tableName, ref bool cancel)
        {
            PreviewEditsSaved?.Invoke(tableName, ref cancel);
        }

        #endregion

        /// <summary>
        /// Opens the database.
        /// </summary>
        public abstract void Open();

        /// <summary>
        /// Closes the database.
        /// </summary>
        public abstract void Close();

        /// <summary>
        /// Retrieves a table view manager for the specified table name.
        /// </summary>
        /// <param name="tableName">The name of the table to retrieve a manager for.</param>
        /// <returns>A <see cref="DataTableView"/> instance for managing the specified table.</returns>
        public abstract DataTableView GetTableManager(string tableName);

        /// <summary>
        /// Gets the list of table names in the database.
        /// </summary>
        /// <returns>An array of table names available in the database.</returns>
        public abstract string[] GetTableNames();

        /// <summary>
        /// Gets the stored number of rows in a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of rows stored in the table.</returns>
        public abstract long GetStoredNumberOfRows(string tableName);

        /// <summary>
        /// Gets the stored number of columns in a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of columns stored in the table.</returns>
        public abstract int GetStoredNumberOfColumns(string tableName);

        /// <summary>
        /// Determines if a type is numeric. Nullable numeric types are considered numeric.
        /// </summary>
        /// <param name="typeToTest">The <see cref="Type"/> to evaluate.</param>
        /// <returns><c>true</c> if the type is a numeric type; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Boolean is not considered numeric.
        /// http://stackoverflow.com/questions/124411/using-net-how-can-i-determine-if-a-type-is-a-numeric-valuetype
        /// </remarks>
        public static bool IsNumericType(Type typeToTest)
        {
            if (typeToTest is null)
            {
                return false;
            }

            switch (Type.GetTypeCode(typeToTest))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    {
                        return true;
                    }
                case TypeCode.Object:
                    {
                        if (typeToTest.IsGenericType && typeToTest.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            return IsNumericType(Nullable.GetUnderlyingType(typeToTest));
                        }
                        return false;
                    }
            }
            return false;
        }

        /// <summary>
        /// Converts a CSV file to an SQLite table.
        /// </summary>
        /// <param name="inputFile">The path to the input CSV file.</param>
        /// <param name="outputFile">The path to the output SQLite database file.</param>
        /// <param name="outputtableName">The name of the table to create in the SQLite database.</param>
        /// <param name="hasHeaders">Whether the CSV includes header rows.</param>
        /// <param name="dataLineStartIndex">The index at which data rows begin.</param>
        /// <param name="fieldsEnclosedInQuotes">Whether fields in the CSV are enclosed in quotes.</param>
        /// <exception cref="Exception">Thrown when file format is invalid or table already exists.</exception>
        public static void ConvertCsvToSqLite(string inputFile, string outputFile, string outputtableName, bool hasHeaders, int dataLineStartIndex, bool fieldsEnclosedInQuotes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(inputFile);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputFile);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputtableName);

            if (Path.GetExtension(inputFile).ToLower() != ".csv")
            {
                throw new Exception("Provided input file, " + Path.GetFileName(inputFile) + " is not a comma separated (.csv) file.");
            }
            if (File.Exists(inputFile) == false)
            {
                throw new Exception("Input file, " + Path.GetFileName(inputFile) + " does not exist.");
            }

            var sqLiteEdit = new SQLiteManager(outputFile);
            try
            {
                sqLiteEdit.Open();
                string[] tableNames = sqLiteEdit.GetTableNames();
                for (int i = 0; i < tableNames.Length; i++)
                {
                    if ((outputtableName ?? "") == (tableNames[i] ?? ""))
                    {
                        throw new Exception("Output table name already exists in the SQLite database file.");
                    }
                }

                var tempDataTable = new DataTable();
                var columnHeaders = new List<string>();
                var columnTypes = new List<Type>();
                using (var csvParser = new Microsoft.VisualBasic.FileIO.TextFieldParser(inputFile) { TextFieldType = Microsoft.VisualBasic.FileIO.FieldType.Delimited })
                {
                    csvParser.HasFieldsEnclosedInQuotes = fieldsEnclosedInQuotes;
                    csvParser.SetDelimiters(new string[] { "," });

                    string[] lineArray = csvParser.ReadFields();
                    if (hasHeaders == true)
                    {
                        int headerCounter;
                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            string header = lineArray[i];
                            headerCounter = 1;
                            while (tempDataTable.Columns.Contains(header) != false)
                            {
                                header = header + headerCounter;
                                headerCounter += 1;
                            }
                            tempDataTable.Columns.Add(header, typeof(string));
                            columnHeaders.Add(header);
                            columnTypes.Add(typeof(string));
                        }
                    }
                    else
                    {
                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            tempDataTable.Columns.Add("Column_" + (i + 1), typeof(string));
                            columnHeaders.Add("Column_" + (i + 1));
                            columnTypes.Add(typeof(string));
                        }
                    }

                    sqLiteEdit.CreateTable(outputtableName, columnHeaders.ToArray(), columnTypes.ToArray());
                    var sqLiteTable = sqLiteEdit.GetTableManager(outputtableName);

                    for (int i = 0; i < dataLineStartIndex; i++)
                    {
                        lineArray = csvParser.ReadFields();
                    }
                    int lineCounter = 0;
                    while (!csvParser.EndOfData)
                    {
                        // Advance to the next row at the top of the loop. Previously `continue`
                        // on a malformed-row skip returned to the while without calling
                        // ReadFields(), spinning on the same lineArray forever.
                        if (lineArray.Length == columnHeaders.Count)
                        {
                            tempDataTable.Rows.Add(lineArray);

                            lineCounter += 1;
                            if (lineCounter > 500000)
                            {
                                sqLiteTable.AddRows(tempDataTable);
                                tempDataTable.Rows.Clear();
                                lineCounter = 0;
                            }
                        }

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
                    sqLiteTable.AddRows(tempDataTable);
                    sqLiteTable.ApplyEdits();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while writing sqlite table: " + Environment.NewLine + ex.Message, ex);
            }
            finally
            {
                sqLiteEdit.Close();
            }
        }
    }
}