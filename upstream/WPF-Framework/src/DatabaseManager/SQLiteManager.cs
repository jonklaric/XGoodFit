using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace DatabaseManager
{

    /// <summary>
/// A class for a managing an SQLite database. 
/// </summary>
/// <remarks>
/// <para>
///     Authors:
///     Woodrow Fields, USACE Risk Management Center, Woodrow.L.Fields@usace.army.mil
///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil 
/// </para>
/// <para>
/// Versions:
///     <list type="bullet">
///         <item><description>
///         First created by Woody Fields
///         </description></item>
///         <item><description>
///         Modified by Haden Smith in July 2019. Documented the class, and added a few features.
///         Modified by Tiki Gonzalez in July 2025. Documented the class.
///         </description></item>         
///     </list>
/// </para>
/// </remarks>
    public class SQLiteManager : DatabaseManager, IDisposable
    {

        #region Construction

        /// <summary>
        /// Constructs a new SQLite manager.
        /// </summary>
        /// <param name="dataBaseFile">The SQLite database filename.</param>
        /// <param name="connectionBuilder">Optional connection string builder for custom connection settings. If null, <see cref="DefaultConnectionBuilder"/> is used.</param>
        public SQLiteManager(string dataBaseFile, SQLiteConnectionStringBuilder connectionBuilder = null)
        {
            _dataBasePath = dataBaseFile;
            try
            {
                SetDatabaseConnection(dataBaseFile, connectionBuilder);
                _tableNames = GetTableNames();
            }
            catch
            {
                DisposeFailedInitialization();
                throw;
            }
        }

        /// <summary>
        /// Constructs a new SQLite manager with a password for encrypted databases.
        /// </summary>
        /// <param name="dataBaseFile">The SQLite database filename.</param>
        /// <param name="databasePassword">The database encryption password.</param>
        /// <param name="connectionBuilder">Optional connection string builder for custom connection settings. If null, <see cref="DefaultConnectionBuilder"/> is used.</param>
        public SQLiteManager(string dataBaseFile, string databasePassword, SQLiteConnectionStringBuilder connectionBuilder = null)
        {
            _dataBasePath = dataBaseFile;
            try
            {
                SetDatabaseConnection(dataBaseFile, databasePassword, connectionBuilder);
                _tableNames = GetTableNames();
            }
            catch
            {
                DisposeFailedInitialization();
                throw;
            }
        }

        /// <summary>Releases the owned provider after failed initialization without dispatching virtual disposal.</summary>
        /// <remarks>A cleanup failure is diagnostic only so the original initialization exception retains its stack.</remarks>
        private void DisposeFailedInitialization()
        {
            try
            {
                _dbConnection?.Dispose();
            }
            catch (Exception cleanupError)
            {
                Debug.WriteLine(cleanupError);
            }
        }

        #endregion

        #region Members

        private SQLiteConnection _dbConnection;
        private bool _disposed;

        /// <summary>
        /// Get the SQLite database connection.
        /// </summary>
        public SQLiteConnection DbConnection
        {
            get
            {
                return _dbConnection;
            }

        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases all resources used by the <see cref="SQLiteManager"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="SQLiteManager"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _dbConnection?.Dispose();
                }
                _disposed = true;
            }
        }

        #endregion

        #region Methods

        #region Database Connection

        /// <summary>
        /// Default connection builder. Designed to optimize insertion time.
        /// </summary>
        public static SQLiteConnectionStringBuilder DefaultConnectionBuilder
        {
            get
            {
                var connectionBuilder = new SQLiteConnectionStringBuilder();
                connectionBuilder.Version = 3;
                // Set the fast connection parameters
                // https://stackoverflow.com/questions/22281187/change-sqliteconnectionstringbuilder-parameters-make-write-to-database-faster-in
                connectionBuilder.DefaultTimeout = 5000;
                connectionBuilder.BusyTimeout = 5000;
                connectionBuilder.PageSize = 65536;
                connectionBuilder.CacheSize = 16777216;
                connectionBuilder.SyncMode = SynchronizationModes.Full;
                connectionBuilder.JournalMode = SQLiteJournalModeEnum.Wal;
                connectionBuilder.FailIfMissing = false;
                connectionBuilder.ReadOnly = false;
                // Use InvariantCulture for DateTime parsing to handle various date formats
                connectionBuilder.DateTimeFormat = SQLiteDateFormats.InvariantCulture;
                connectionBuilder.DateTimeKind = DateTimeKind.Local;
                return connectionBuilder;
            }
        }

        /// <summary>
        /// Sets the database connection using the specified filename.
        /// </summary>
        /// <param name="fileName">The SQLite database filename.</param>
        /// <param name="connectionBuilder">Optional connection string builder for custom connection settings. If null, <see cref="DefaultConnectionBuilder"/> is used.</param>
        public void SetDatabaseConnection(string fileName, SQLiteConnectionStringBuilder connectionBuilder = null)
        {
            if (connectionBuilder is null)
            {
                connectionBuilder = DefaultConnectionBuilder;
            }
            connectionBuilder.DataSource = fileName;
            SetDatabaseConnection(connectionBuilder);
        }

        /// <summary>
        /// Sets the database connection using the specified filename and password.
        /// </summary>
        /// <param name="fileName">The SQLite database filename.</param>
        /// <param name="password">The database encryption password.</param>
        /// <param name="connectionBuilder">Optional connection string builder for custom connection settings. If null, <see cref="DefaultConnectionBuilder"/> is used.</param>
        public void SetDatabaseConnection(string fileName, string password, SQLiteConnectionStringBuilder connectionBuilder = null)
        {
            if (connectionBuilder is null)
            {
                connectionBuilder = DefaultConnectionBuilder;
            }
            connectionBuilder.DataSource = fileName;
            connectionBuilder.Password = password;
            SetDatabaseConnection(connectionBuilder);
        }

        /// <summary>
        /// Sets the database connection using the specified connection string builder.
        /// </summary>
        /// <param name="connectionBuilder">The SQLite connection string builder containing connection settings.</param>
        public void SetDatabaseConnection(SQLiteConnectionStringBuilder connectionBuilder)
        {
            _dbConnection = new SQLiteConnection(connectionBuilder.ToString());
        }

        #endregion

        #region Database Management

        /// <summary>
        /// Open database connection.
        /// </summary>
        public override void Open()
        {
            _dbConnection.Open();
            _dataBaseOpen = true;
        }

        /// <summary>
        /// Close the database connection.
        /// </summary>
        public override void Close()
        {
            _dbConnection.Close();
            SQLiteConnection.ClearAllPools();
            _dataBaseOpen = false;
        }

        /// <summary>
        /// Creates a new SQLite database file.
        /// </summary>
        /// <param name="databaseFile">The SQLite database filename to create.</param>
        public static void CreateSqLiteFile(string databaseFile)
        {
            SQLiteConnection.CreateFile(databaseFile);
        }

        /// <summary>
        /// Creates a new encrypted SQLite database file with a password.
        /// </summary>
        /// <param name="databaseFile">The SQLite database filename to create.</param>
        /// <param name="databasePassword">The database encryption password.</param>
        public static void CreateSqLiteFile(string databaseFile, string databasePassword)
        {
            SQLiteConnection.CreateFile(databaseFile);
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = databaseFile,
                Version = 3,
                Password = databasePassword
            };
            using (var sqlConn = new SQLiteConnection(builder.ConnectionString))
            {
                sqlConn.Open();
                sqlConn.Close();
            }
        }

        /// <summary>
        /// Rebuilds the database file, repacking it into a minimal amount of disk space.
        /// </summary>
        public void Vacuum()
        {
            WithConnectionOwnership(() =>
            {
                using (var command = new SQLiteCommand("vacuum", _dbConnection))
                {
                    command.ExecuteNonQuery();
                }
                return true;
            });
        }

        /// <summary>
        /// Optimizes the current SQLite database by executing the PRAGMA optimize command.
        /// This command analyzes and improves the performance of internal data structures 
        /// such as indices, statistics, and temporary storage usage.
        /// </summary>
        public void Optimize()
        {
            WithConnectionOwnership(() =>
            {
                using (var command = new SQLiteCommand("PRAGMA optimize", _dbConnection))
                {
                    command.ExecuteNonQuery();
                }
                return true;
            });
        }


        #endregion

        /// <summary>Runs an operation while restoring ownership of an initially closed connection.</summary>
        /// <typeparam name="TResult">The operation result type.</typeparam>
        /// <param name="operation">The operation to run after opening the connection if needed.</param>
        /// <returns>The operation result.</returns>
        /// <remarks>Acquisition is protected because a failed open can acquire provider resources. Borrowed connections remain open. A secondary close failure is logged while the original exception propagates; a cleanup-only failure propagates.</remarks>
        private TResult WithConnectionOwnership<TResult>(Func<TResult> operation)
        {
            bool wasOpen = DataBaseOpen;
            Exception operationError = null;
            try
            {
                if (!wasOpen)
                    Open();
                return operation();
            }
            catch (Exception error)
            {
                operationError = error;
                throw;
            }
            finally
            {
                if (!wasOpen)
                {
                    try
                    {
                        Close();
                    }
                    catch (Exception cleanupError) when (operationError != null)
                    {
                        Debug.WriteLine(cleanupError);
                    }
                }
            }
        }

        #region Database Table Management

        /// <summary>
        /// Validates a table or column identifier for safe inclusion in SQL via
        /// <c>[bracket]</c> quoting. Rejects names that contain a closing bracket
        /// (which would break the quote) or null bytes. Public entry points that
        /// build SQL from caller-supplied identifiers should call this once at the
        /// boundary so internal SQL construction can trust the input.
        /// </summary>
        /// <param name="name">The identifier to validate.</param>
        /// <param name="paramName">Name of the parameter being validated, for the exception message.</param>
        /// <exception cref="ArgumentException">The identifier is null, empty, or contains <c>]</c> or a null byte.</exception>
        private static void ValidateIdentifier(string name, string paramName)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Identifier must not be null or empty.", paramName);
            }
            if (name.IndexOf(']') >= 0 || name.IndexOf('\0') >= 0)
            {
                throw new ArgumentException(
                    $"Identifier '{name}' contains characters that are not safe for SQL bracket-quoting (']' or null byte).",
                    paramName);
            }
        }

        /// <summary>
        /// Copy table.
        /// </summary>
        /// <param name="existingTableName">The name of the table to copy.</param>
        /// <param name="newTableName">The name of the new table.</param>
        public void CopyTable(string existingTableName, string newTableName)
        {
            ValidateIdentifier(existingTableName, nameof(existingTableName));
            ValidateIdentifier(newTableName, nameof(newTableName));

            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            {
                Open();
            }
            if (_tableNames.Contains(existingTableName) == false)
            { 
                throw new Exception("Table '" + existingTableName + "' does not exist in the database."); 
            }
            // Create the table copy create statement from existing table.
            string existingCreateStatement = "";
            using (var cmd = new SQLiteCommand("SELECT sql FROM sqlite_master WHERE type='table' AND name=@tableName", _dbConnection))
            {
                cmd.Parameters.AddWithValue("@tableName", existingTableName);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                            existingCreateStatement = Convert.ToString(reader[0]);
                    }
                }
            }
            if (string.IsNullOrEmpty(existingCreateStatement))
            { 
                throw new Exception("Table '" + existingTableName + "' does not have a create statement and currently cannot be copied."); 
            }
            // rename existing table to new table and create fresh copy of existing table.
            RenameTable(existingTableName, newTableName);
            using (var command = new SQLiteCommand(existingCreateStatement, _dbConnection))
            {
                command.ExecuteNonQuery();
            }
            // Copy from new table into existing table since existing table was simply renamed to new table to simplify create statement.
            using (var command = new SQLiteCommand("INSERT INTO [" + existingTableName + "] SELECT * FROM [" + newTableName + "]", _dbConnection))
            {
                command.ExecuteNonQuery();
            }
            // 
            if (wasOpen == false)
            {
                Close(); 
            }
            _tableNames = GetTableNames();
        }

        /// <summary>
        /// Rename table.
        /// </summary>
        /// <param name="oldTableName">The name of the table to rename.</param>
        /// <param name="newTableName">The new name of the table.</param>
        public void RenameTable(string oldTableName, string newTableName)
        {
            ValidateIdentifier(oldTableName, nameof(oldTableName));
            ValidateIdentifier(newTableName, nameof(newTableName));

            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            {
                Open();
            }
            if (_tableNames.Contains(oldTableName) == false)
            { 
                throw new Exception("Table '" + oldTableName + "' does not exist in the database."); 
            }
            // 
            using (var command = new SQLiteCommand("ALTER TABLE [" + oldTableName + "] RENAME TO [" + newTableName + "]", _dbConnection))
            {
                command.ExecuteNonQuery();
            }
            // 
            if (wasOpen == false)
            { 
                Close(); 
            }
            _tableNames = GetTableNames();
        }

        /// <summary>
        /// Get table names.
        /// </summary>
        public override string[] GetTableNames()
        {
            var result = WithConnectionOwnership(() =>
            {
                var names = new List<string>();
                using (var command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table'", _dbConnection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                                names.Add(Convert.ToString(reader[0]));
                        }
                    }
                }
                return names;
            });
            _tableNames = result.ToArray();
            return result.ToArray();
        }

        /// <summary>
        /// Save table based on datatable. This will create a table if one does not exist.
        /// </summary>
        /// <param name="dt">Data table.</param>
        public void SaveDataTable(DataTable dt)
        {
            try
            {
                bool wasOpen = _dataBaseOpen;
                if (_dataBaseOpen == false)
                    Open();
                // 
                var parameters = new List<SQLiteParameter>();
                var sb = new System.Text.StringBuilder(500);
                sb.Append("Create Table [").Append(dt.TableName).Append("] (");
                foreach (DataColumn c in dt.Columns)
                {
                    var tc = Type.GetTypeCode(c.DataType);
                    sb.Append("[").Append(c.ColumnName).Append("] ");
                    switch (tc)
                    {
                        case TypeCode.String:
                            {
                                sb.Append("TEXT,");
                                break;
                            }
                        case TypeCode.DateTime:
                            {
                                sb.Append("DATETIME,");
                                break;
                            }
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                            {
                                sb.Append("INT1,");
                                break;
                            }
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                            {
                                sb.Append("INT2,");
                                break;
                            }
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                            {
                                sb.Append("INT4,");
                                break;
                            }
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            {
                                sb.Append("INT8,");
                                break;
                            }
                        case TypeCode.Single:
                            {
                                sb.Append("FLOAT,");
                                break;
                            }
                        case TypeCode.Double:
                            {
                                sb.Append("DOUBLE,");
                                break;
                            }
                        case TypeCode.Decimal:
                            {
                                sb.Append("NUMBER,");
                                break;
                            }
                        case TypeCode.Char:
                            {
                                sb.Append("CHAR,");
                                break;
                            }
                        case TypeCode.Boolean:
                            {
                                sb.Append("BOOLEAN,");
                                break;
                            }
                        case TypeCode.Object:
                            {
                                sb.Append("BLOB,");
                                break;
                            }

                        default:
                            {
                                if (ReferenceEquals(c.DataType, typeof(Guid)))
                                {
                                    sb.Append("Text,");
                                }
                                else
                                {
                                    throw new Exception(tc.ToString() + " Not implemented, Column: " + c.ColumnName);
                                }

                                break;
                            }
                    }
                    parameters.Add(new SQLiteParameter(c.ColumnName));
                }
                if (dt.Columns.Count > 0)
                {
                    sb[sb.Length - 1] = ')';
                }
                else
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                // Create the table
                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        // Create Table
                        cmd.CommandText = sb.ToString();
                        cmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
                // add the rows
                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        // Create Insert string
                        string insertText = "INSERT INTO [" + dt.TableName + "] VALUES (";
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            insertText = insertText + "?" + ","; 
                            cmd.Parameters.Add(parameters[i]);
                        }
                        insertText = insertText.Substring(0, insertText.Length - 1) + ")";
                        cmd.CommandText = insertText;
                        // Insert Rows
                        foreach (DataRow r in dt.Rows)
                        {
                            for (int i = 0; i < parameters.Count; i++)
                            { 
                                parameters[i].Value = r[i]; 
                            }
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
                // 
                _tableNames = GetTableNames();
                if (wasOpen == false)
                    Close();
            }

            catch (Exception ex)
            {
                throw new Exception("Error writing SQLite DataTable: " + ex.Message);
            }
        }

        /// <summary>
        /// Create new table.
        /// </summary>
        /// <param name="newTableName">Table name.</param>
        /// <param name="tableColumnNames">Array of table column names.</param>
        /// <param name="newColumnTypes">Array of table column types.</param>
        public void CreateTable(string newTableName, string[] tableColumnNames, Type[] newColumnTypes)
        {
            ValidateIdentifier(newTableName, nameof(newTableName));
            if (tableColumnNames == null)
            {
                throw new ArgumentNullException(nameof(tableColumnNames));
            }
            for (int i = 0; i < tableColumnNames.Length; i++)
            {
                ValidateIdentifier(tableColumnNames[i], $"{nameof(tableColumnNames)}[{i}]");
            }

            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            {
                Open();
            }
            var sb = new System.Text.StringBuilder(500);
            sb.Append("Create Table [").Append(newTableName).Append("] (");
            for (int i = 0; i < tableColumnNames.Count(); i++)
            {
                sb.Append("[").Append(tableColumnNames[i]).Append("] ");
                switch (newColumnTypes[i])
                {
                    case var @case when @case == typeof(string):
                        {
                            sb.Append("TEXT,");
                            break;
                        }
                    case var case1 when case1 == typeof(DateTime):
                        {
                            sb.Append("DATETIME,");
                            break;
                        }
                    case var case2 when case2 == typeof(byte):
                    case var case3 when case3 == typeof(sbyte):
                        {
                            sb.Append("INT1,");
                            break;
                        }
                    case var case4 when case4 == typeof(short):
                    case var case5 when case5 == typeof(ushort):
                        {
                            sb.Append("INT2,");
                            break;
                        }
                    case var case6 when case6 == typeof(int):
                    case var case7 when case7 == typeof(uint):
                        {
                            sb.Append("INT4,");
                            break;
                        }
                    case var case8 when case8 == typeof(long):
                    case var case9 when case9 == typeof(ulong):
                        {
                            sb.Append("INT8,");
                            break;
                        }
                    case var case10 when case10 == typeof(float):
                        {
                            sb.Append("FLOAT,");
                            break;
                        }
                    case var case11 when case11 == typeof(double):
                        {
                            sb.Append("DOUBLE,");
                            break;
                        }
                    case var case12 when case12 == typeof(decimal):
                        {
                            sb.Append("NUMBER,");
                            break;
                        }
                    case var case13 when case13 == typeof(char):
                        {
                            sb.Append("CHAR,");
                            break;
                        }
                    case var case14 when case14 == typeof(bool):
                        {
                            sb.Append("BOOLEAN,");
                            break;
                        }
                    case var case15 when case15 == typeof(object):
                    case var case16 when case16 == typeof(byte[]):
                        {
                            sb.Append("BLOB,");
                            break;
                        }

                    default:
                        {
                            throw new Exception(newColumnTypes[i].ToString() + " Not implemented, Column: " + tableColumnNames[i]);
                        }
                }
            }
            if (tableColumnNames.Count() > 0)
            {
                sb[sb.Length - 1] = ')';
            }
            else
            {
                sb.Remove(sb.Length - 1, 1);
            }
            using (var cmd = _dbConnection.CreateCommand())
            {
                cmd.CommandText = sb.ToString();
                cmd.ExecuteNonQuery();
            }
            _tableNames = GetTableNames();
            if (wasOpen == false)
            { 
                Close();
            }
        }

        /// <summary>
        /// Append rows to existing table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        /// <param name="newRowData">New rows as DataTable.</param>
        /// <param name="maxAttempts">Optional. Maximum number of attempts to write to table from parallel loops. Default = 8.</param>
        public void AppendRows(string tableName, DataTable newRowData, int maxAttempts = 8)
        {
            ValidateIdentifier(tableName, nameof(tableName));

            for (int ma = maxAttempts; ma >= 0; ma -= 1)
            {

                try
                {

                    bool wasOpen = _dataBaseOpen;
                    if (_dataBaseOpen == false)
                    { 
                        Open();
                    }

                    object[] _ColumnNames;
                    object[] _ColumnTypes;

                    using (var Command = new SQLiteCommand("PRAGMA table_info([" + tableName + "])", DbConnection))
                    {
                        using var Adap = new SQLiteDataAdapter(Command);
                        var tab = new DataTable();
                        Adap.Fill(tab);
                        _ColumnNames = new object[tab.Rows.Count];
                        _ColumnTypes = new object[tab.Rows.Count];

                        string TypeString;
                        for (int i = 0; i < tab.Rows.Count; i++)
                        {
                            _ColumnNames[i] = Convert.ToString(tab.Rows[i][1]);
                            TypeString = Convert.ToString(tab.Rows[i][2]);
                            if (TypeString.Contains("INT"))
                            {
                                switch (TypeString ?? "")
                                {
                                    case "INT1":
                                        {
                                            _ColumnTypes[i] = typeof(byte);
                                            break;
                                        }
                                    case "INT2":
                                        {
                                            _ColumnTypes[i] = typeof(short);
                                            break;
                                        }
                                    case "INT4":
                                    case "INTEGER":
                                        {
                                            _ColumnTypes[i] = typeof(int);
                                            break;
                                        }
                                    case "INT8":
                                        {
                                            _ColumnTypes[i] = typeof(long);
                                            break;
                                        }
                                }
                            }
                            else if (TypeString.Contains("CHAR") || TypeString.Contains("CLOB") || TypeString.Contains("TEXT"))
                            {
                                _ColumnTypes[i] = typeof(string);
                            }
                            else if (TypeString.Contains("FLOA"))
                            {
                                _ColumnTypes[i] = typeof(float);
                            }
                            else if (TypeString.Contains("REAL") || TypeString.Contains("DOUB"))
                            {
                                _ColumnTypes[i] = typeof(double);
                            }
                            else if (TypeString.Contains("BOOL"))
                            {
                                _ColumnTypes[i] = typeof(bool);
                            }
                            else if (TypeString.Contains("BLOB"))
                            {
                                _ColumnTypes[i] = typeof(byte[]);
                            }
                            else
                            {
                                _ColumnTypes[i] = typeof(byte[]);
                            }
                        }
                    }

                    int _NColumns = _ColumnNames.Count();

                    var Parameters = new List<SQLiteParameter>();
                    if (newRowData.Columns.Count != _NColumns)
                    {
                        throw new Exception("Number of columns does not match."); 
                    }
                    for (int i = 0; i < newRowData.Columns.Count; i++)
                    {
                        if (!newRowData.Columns[i].DataType.Equals(_ColumnTypes[i]))
                        { 
                            throw new Exception("Column types do not match.");
                        }
                        Parameters.Add(new SQLiteParameter(newRowData.Columns[i].ColumnName));
                    }

                    using (var tr = DbConnection.BeginTransaction())
                    {
                        using (var cmd = DbConnection.CreateCommand())
                        {
                            cmd.Transaction = tr;
                            // Create Insert string
                            string InsertText = "INSERT INTO [" + tableName + "] VALUES (";
                            for (int i = 0; i < Parameters.Count; i++)
                            {
                                InsertText = InsertText + "?" + ","; 
                                cmd.Parameters.Add(Parameters[i]);
                            }
                            InsertText = InsertText.Substring(0, InsertText.Length - 1) + ")";
                            cmd.CommandText = InsertText;
                            // Insert Rows
                            foreach (DataRow r in newRowData.Rows)
                            {
                                for (int i = 0; i < Parameters.Count; i++)
                                { 
                                    Parameters[i].Value = r[i];
                                }
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tr.Commit();
                    }

                    if (wasOpen == false)
                    {
                        Close(); 
                    }

                    break;
                }

                catch (SQLiteException)
                {
                    // If there was an exception, put thread to sleep and try again
                    Thread.Sleep(2000);
                    // If it tried to write to table the maximum number of times and failed then throw exception
                    if (ma == 0)
                    {
                        throw;
                    }
                    //else, just keep going 
                }

            }

        }

        /// <summary>
        /// Delete table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        public void DeleteTable(string tableName)
        {
            ValidateIdentifier(tableName, nameof(tableName));
            if (_tableNames.Contains(tableName) == false) { return; }
            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            {
                Open();
            }
            using (var cmd = _dbConnection.CreateCommand())
            {
                cmd.CommandText = "DROP TABLE [" + tableName + "]";
                cmd.ExecuteNonQuery();
            }
            _tableNames = GetTableNames();
            if (wasOpen == false)
            {
                Close();
            }
        }

        /// <summary>
        /// Delete all data from a table.
        /// </summary>
        /// <param name="tableName">Table name.</param>
        public void DeleteTableData(string tableName)
        {
            ValidateIdentifier(tableName, nameof(tableName));
            if (_tableNames.Contains(tableName) == false) { return; }
            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            { 
                Open();
            }
            using (var tr = _dbConnection.BeginTransaction())
            {
                using (var cmd = _dbConnection.CreateCommand())
                {
                    cmd.Transaction = tr;
                    cmd.CommandText = "DELETE FROM " + "[" + tableName + "]";
                    cmd.ExecuteNonQuery();
                }
                tr.Commit();
            }
            if (wasOpen == false)
            { 
                Close(); 
            }
        }

        /// <summary>
        /// Gets a table manager for the specified table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>A <see cref="DataTableView"/> instance for managing the specified table.</returns>
        public override DataTableView GetTableManager(string tableName)
        {
            ValidateIdentifier(tableName, nameof(tableName));
            return new SqLiteTableManager(this, tableName, _dbConnection);
        }

        /// <summary>
        /// Gets the number of rows stored in a table.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        /// <returns>The number of rows in the table.</returns>
        public override long GetStoredNumberOfRows(string tableName)
        {
            ValidateIdentifier(tableName, nameof(tableName));
            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            { 
                Open();
            }
            long rowCount;
            using (var command = new SQLiteCommand("SELECT Count(*) FROM [" + tableName + "]", _dbConnection))
            {
                rowCount = Convert.ToInt64(command.ExecuteScalar());
            }
            if (wasOpen == false)
            { 
                Close(); 
            }
            return rowCount;
        }

        /// <inheritdoc/>
        public override int GetStoredNumberOfColumns(string tableName)
        {
            ValidateIdentifier(tableName, nameof(tableName));
            bool wasOpen = _dataBaseOpen;
            if (_dataBaseOpen == false)
            {
                Open();
            }
            //
            int columnCount;
            using (var command = new SQLiteCommand("PRAGMA table_info([" + tableName + "])", _dbConnection))
            {
                var adap = new SQLiteDataAdapter(command);
                var tab = new DataTable();
                adap.Fill(tab);
                columnCount = tab.Rows.Count;
            }
            // 
            if (wasOpen == false)
            {
                Close(); 
            }
            return columnCount;
        }


        #endregion

        #region SQLiteTableManager

        /// <summary>
        /// A specialized DataTableView that provides low-level read/write access to a SQLite database table.
        /// Handles mapping of row indices via SQLirw row id and supports typed editing of individual cells,
        /// rows, and columns using schema introspection.
        /// </summary>
        private class SqLiteTableManager : DataTableView
        {
            private long[] _rowIdArray;
            private bool[] _columnIsPrimaryKey;
            private readonly SQLiteConnection _dbConnection;

            /// <summary>
            /// Initializes a new instance of the SqLiteTableManager class. 
            /// Loads column schema, row count, and maps the table's row id values.
            /// </summary>
            /// <param name="reader">The SQLiteManager class.</param>
            /// <param name="dataTableName">The name of the SQLite table being managed.</param>
            /// <param name="dbConnection">The active SQLite database connection.</param>
            public SqLiteTableManager(SQLiteManager reader, string dataTableName, SQLiteConnection dbConnection)
            {
                _tableName = dataTableName;
                _parentDatabase = reader;
                _dbConnection = dbConnection;
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                _storedNumberOfRows = (int)GetStoredRowCount();
                InitializeView();
                _rowIdArray = GetRowIdArray();
            }

            /// <summary>
            /// Retrieves the number of rows in the underlying SQLite table.
            /// </summary>
            /// <returns>The total number of rows stored in the table.</returns>
            protected override ulong GetStoredRowCount()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                ulong rowCount;
                using (var command = new SQLiteCommand("SELECT Count(*) FROM [" + _tableName + "]", _dbConnection))
                {
                    rowCount = (ulong)Convert.ToInt64(command.ExecuteScalar());
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
                return rowCount;
            }

            /// <summary>
            /// Returns the row id values of all rows in the table.
            /// These are used for uniquely identifying rows during update/delete operations.
            /// </summary>
            /// <returns>An array of 64-bit integers corresponding to each row's row id.</returns>
            private long[] GetRowIdArray()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }

                var rowIDs = new List<long>();
                using (var cmd = new SQLiteCommand("SELECT rowid FROM [" + _tableName + "]", _dbConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                                rowIDs.Add(Convert.ToInt64(reader[0]));
                        }
                    }
                }

                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
                return rowIDs.ToArray();
            }

            /// <summary>
            /// Retrieves the list of column names from the SQLite table using PRAGMA table_info.
            /// </summary>
            /// <returns>An array of column names in the table.</returns>
            protected override string[] GetStoredColumnNames()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                // 
                string[] result;
                using (var command = new SQLiteCommand("PRAGMA table_info([" + _tableName + "])", _dbConnection))
                {
                    var adap = new SQLiteDataAdapter(command);
                    var tab = new DataTable();
                    adap.Fill(tab);
                    result = new string[tab.Rows.Count];
                    _columnIsPrimaryKey = new bool[tab.Rows.Count];

                    for (int i = 0; i < tab.Rows.Count; i++)
                    {
                        result[i] = Convert.ToString(tab.Rows[i][1]);
                        if (Convert.ToInt32(tab.Rows[i][5]) > 0)
                        {
                            _columnIsPrimaryKey [i] = true; 
                        }
                    }
                }
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
                return result;
            }

            /// <summary>
            /// Retrieves the CLR type for each column in the SQLite table based on SQLite column type declarations.
            /// </summary>
            /// <returns>An array of System.Type corresponding to each column's inferred data type.</returns>
            protected override Type[] GetStoredColumnTypes()
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                Type[] existingColumnTypes;
                using (var command = new SQLiteCommand("PRAGMA table_info([" + _tableName + "])", _dbConnection))
                {
                    var adap = new SQLiteDataAdapter(command);
                    var tab = new DataTable();
                    adap.Fill(tab);
                    existingColumnTypes = new Type[tab.Rows.Count];

                    string typeString;
                    for (int i = 0; i < tab.Rows.Count; i++)
                    {
                        typeString = Convert.ToString(tab.Rows[i][2]);
                        if (typeString.Contains("INT"))
                        {
                            switch (typeString ?? "")
                            {
                                case "INT1":
                                case "TINYINT":
                                    {
                                        existingColumnTypes[i] = typeof(byte);
                                        break;
                                    }
                                case "INT2":
                                case "SMALLINT":
                                    {
                                        existingColumnTypes[i] = typeof(short);
                                        break;
                                    }
                                case "INT4":
                                case "INTEGER":
                                case "MEDIUMINT":
                                    {
                                        existingColumnTypes[i] = typeof(int);
                                        break;
                                    }
                                case "INT8":
                                    {
                                        existingColumnTypes[i] = typeof(long);
                                        break;
                                    }
                                case "POINT":
                                case "MULTIPOINT": // This is to handle GeoPackage and their geometry type codes...which fail the sqlite typing rules.
                                    {
                                        existingColumnTypes[i] = typeof(byte[]); // Unknown, this will probably break
                                        break;
                                    }

                                default:
                                    {
                                        existingColumnTypes[i] = typeof(byte[]);
                                        break;
                                    }
                            }
                        }
                        else if (typeString.Contains("CHAR") || typeString.Contains("CLOB") || typeString.Contains("TEXT"))
                        {
                            existingColumnTypes[i] = typeof(string);
                        }
                        else if (typeString.Contains("FLOA")) // This doesn't guarantee single precision per the sqlite spec. It is still stored internally as 8 byte decimal.
                        {
                            existingColumnTypes[i] = typeof(float);
                        }
                        else if (typeString.Contains("REAL") || typeString.Contains("DOUB"))
                        {
                            existingColumnTypes[i] = typeof(double);
                        }
                        else if (typeString.Contains("BOOL"))
                        {
                            existingColumnTypes[i] = typeof(bool);
                        }
                        else if (typeString.Contains("BLOB"))
                        {
                            existingColumnTypes[i] = typeof(byte[]);
                        }
                        else
                        {
                            existingColumnTypes[i] = typeof(byte[]);
                        }
                    }
                }
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
                return existingColumnTypes;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            /// <param name="row"></param>
            /// <exception cref="Exception"></exception>
            protected override void AddRowToDatabase(object[] row)
            {
                var parameters = new List<SQLiteParameter>();
                if (row.Count() != _storedColumnNames.Count())
                {
                    throw new Exception("Number of columns that you are trying to add do not match the number of columns in the database."); 
                }
                for (int i = 0; i < row.Count(); i++)
                {
                    if (ConvertToColumnType(_storedColumnTypes[i], ref row[i]) == false)
                    {
                        throw new Exception("Column type of '" + row[i].GetType().ToString() + "' is invalid for column '" + _storedColumnNames[i] + "'."); 
                    }
                    parameters.Add(new SQLiteParameter(_storedColumnNames[i]));
                }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                using (var cmd = _dbConnection.CreateCommand())
                {
                    // Create Insert string
                    string insertText = "INSERT INTO [" + _tableName + "] VALUES (";
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        insertText = insertText + "?" + ",";
                        cmd.Parameters.Add(parameters[i]);
                    }
                    insertText = insertText.Substring(0, insertText.Length - 1) + ")";
                    cmd.CommandText = insertText;
                    // Insert Rows
                    for (int i = 0; i < row.Count(); i++)
                    {
                        if (_columnIsPrimaryKey[i] == true)
                        {
                            parameters[i].Value = DBNull.Value; // should let SQLite do its thing with primary key columns.
                        }
                        else
                        {
                            parameters[i].Value = row[i];
                        }
                    }
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedNumberOfRows = (int)GetStoredRowCount();
                _rowIdArray = GetRowIdArray();
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }
            
            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddRowToDatabase()
            {
                var row = new object[(_storedColumnNames.Count())];
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    row[i] = DBNull.Value; 
                }
                AddRowToDatabase(row);
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddRowsToDatabase(List<object[]> newRowData)
            {
                for (int i = 0; i < newRowData.Count; i++)
                {
                    if (newRowData[i].Count() != _storedColumnNames.Count())
                    {
                        throw new Exception("Number of columns does not match for row " + i.ToString() + "."); 
                    }
                    for (int j = 0; j < newRowData[i].Count(); j++)
                    {
                        var tmp = newRowData[i];
                        var argvalue = tmp[j];
                        if (ConvertToColumnType(_storedColumnTypes[j], ref argvalue) == false)
                        {
                            throw new Exception("Column type '" + newRowData[i][j].GetType().ToString() + "' does not match for column '" + _storedColumnNames[j] + "' in row " + i.ToString() + "."); 
                        }
                    }
                }
                var parameters = new List<SQLiteParameter>();
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    parameters.Add(new SQLiteParameter(_storedColumnNames[i])); 
                }
                // 
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        // Create Insert string
                        string insertText = "INSERT INTO [" + _tableName + "] VALUES (";
                        for (int i = 0; i < parameters.Count; i++)
                        {
                            insertText = insertText + "?" + ","; 
                            cmd.Parameters.Add(parameters[i]);
                        }
                        insertText = insertText.Substring(0, insertText.Length - 1) + ")";
                        cmd.CommandText = insertText;
                        // Insert Rows
                        for (int i = 0; i < newRowData.Count; i++)
                        {
                            for (int j = 0; j < newRowData[i].Count(); j++)
                            {
                                if (_columnIsPrimaryKey[j] == true)
                                {
                                    parameters[j].Value = DBNull.Value; // should let SQLite do its thing with primary key columns.
                                }
                                else
                                {
                                    parameters[j].Value = newRowData[i][j];
                                }
                            }
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
                // 
                _storedNumberOfRows = (int)GetStoredRowCount();
                _rowIdArray = GetRowIdArray();
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void DeleteRowFromDatabase(int rowIndex)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                using (var cmd = new SQLiteCommand("DELETE FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedNumberOfRows = (int)GetStoredRowCount();
                _rowIdArray = GetRowIdArray();
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void DeleteRowsFromDatabase(int[] rowIndices)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        for (int i = 0; i < rowIndices.Count(); i++)
                        {
                            cmd.CommandText = "DELETE FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[rowIndices[i]];
                            cmd.ExecuteNonQuery();
                        }

                    }
                    trans.Commit();
                }
                // 
                _storedNumberOfRows = (int)GetStoredRowCount();
                _rowIdArray = GetRowIdArray();
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
                
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, byte[][] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] BLOB", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, byte[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] INT1", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, double[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] DOUBLE", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, int[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                    // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] INT4", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 

                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, long[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] INT8", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, short[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column.");
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] INT2", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, float[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] FLOAT", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, bool[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                { 
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] BOOLEAN", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void AddColumnToDatabase(string columnName, string[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (_storedColumnNames.Contains(columnName) == true)
                {
                    throw new Exception("Column Name " + columnName + " already exists, choose a different name."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("Number of records do not match the number of records for the new column."); 
                }
                // 
                using (var cmd = new SQLiteCommand("ALTER TABLE [" + _tableName + "] ADD COLUMN [" + columnName + "] TEXT", _dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                EditDatabaseColumn(columnName, columnData);
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// Maps a CLR <see cref="Type"/> to its corresponding SQLite type name string.
            /// </summary>
            /// <param name="t">The CLR type to map.</param>
            /// <returns>The SQLite type name (e.g., <c>"TEXT"</c>, <c>"INT4"</c>, <c>"BLOB"</c>).</returns>
            /// <exception cref="Exception">Thrown when the type has no known SQLite mapping.</exception>
            private static string GetSQLiteTypeName(Type t)
            {
                if (t == typeof(string)) return "TEXT";
                if (t == typeof(DateTime)) return "DATETIME";
                if (t == typeof(byte) || t == typeof(sbyte)) return "INT1";
                if (t == typeof(short) || t == typeof(ushort)) return "INT2";
                if (t == typeof(int) || t == typeof(uint)) return "INT4";
                if (t == typeof(long) || t == typeof(ulong)) return "INT8";
                if (t == typeof(float)) return "FLOAT";
                if (t == typeof(double)) return "DOUBLE";
                if (t == typeof(decimal)) return "NUMBER";
                if (t == typeof(char)) return "CHAR";
                if (t == typeof(bool)) return "BOOLEAN";
                if (t == typeof(object) || t == typeof(byte[])) return "BLOB";
                throw new Exception(t.ToString() + " Not implemented.");
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void DeleteColumnsFromDatabase(string[] columnsToDelete)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                //
                var newColumnNames = new List<string>();
                var newColumnTypes = new List<Type>();
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    if (columnsToDelete.Contains(_storedColumnNames[i])) { continue; }
                    newColumnNames.Add(_storedColumnNames[i]);
                    newColumnTypes.Add(_storedColumnTypes[i]);
                    sb.Append("[").Append(_storedColumnNames[i]).Append("] ");
                    sb.Append(GetSQLiteTypeName(_storedColumnTypes[i])).Append(",");
                }
                sb.Remove(sb.Length - 1, 1);

                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        cmd.CommandText = "CREATE TEMPORARY TABLE [" + _tableName + "_Old] (" + sb.ToString() + ")";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "INSERT INTO [" + _tableName + "_Old] SELECT " + sb.ToString() + " FROM [" + _tableName + "]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "DROP TABLE [" + _tableName + "]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "CREATE TABLE [" + _tableName + "] (" + sb.ToString() + ")";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "INSERT INTO [" + _tableName + "] SELECT " + sb.ToString() + " FROM [" + _tableName + "_Old]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "DROP TABLE [" + _tableName + "_Old]";
                        cmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void DeleteColumnFromDatabase(string columnName)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                //
                var newColumnNames = new List<string>();
                var newColumnTypes = new List<Type>();
                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _storedColumnNames.Count(); i++)
                {
                    if ((_storedColumnNames[i] ?? "") == (columnName ?? "")) { continue; }
                    newColumnNames.Add(_storedColumnNames[i]);
                    newColumnTypes.Add(_storedColumnTypes[i]);
                    sb.Append("[").Append(_storedColumnNames[i]).Append("] ");
                    sb.Append(GetSQLiteTypeName(_storedColumnTypes[i])).Append(",");
                }
                sb.Remove(sb.Length - 1, 1);

                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        cmd.CommandText = "CREATE TEMPORARY TABLE [" + _tableName + "_Old] (" + sb.ToString() + ")";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "INSERT INTO [" + _tableName + "_Old] SELECT " + sb.ToString() + " FROM [" + _tableName + "]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "DROP TABLE [" + _tableName + "]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "CREATE TABLE [" + _tableName + "] (" + sb.ToString() + ")";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "INSERT INTO [" + _tableName + "] SELECT " + sb.ToString() + " FROM [" + _tableName + "_Old]";
                        cmd.ExecuteNonQuery();
                        // 
                        cmd.CommandText = "DROP TABLE [" + _tableName + "_Old]";
                        cmd.ExecuteNonQuery();
                    }
                    tr.Commit();
                }
                // 
                _storedColumnNames = GetStoredColumnNames();
                _storedColumnTypes = GetStoredColumnTypes();
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCells(int[] columnIndices, int[] rowIndices, object[] cellValues)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        for (int i = 0; i < columnIndices.Count(); i++)
                        {
                            if (cmd.Parameters.Contains(_storedColumnNames[columnIndices[i]]) == false)
                                cmd.Parameters.Add(new SQLiteParameter(_storedColumnNames[columnIndices[i]]));
                            cmd.Parameters[_storedColumnNames[columnIndices[i]]].Value = cellValues[i];
                            cmd.CommandText = "UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndices[i]] + "]= @" + _storedColumnNames[columnIndices[i]] + " WHERE rowid=" + _rowIdArray[rowIndices[i]];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCells(string[] columnNamesToEdit, int[] rowIndices, object[] cellValues)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                using (var tr = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = tr;
                        for (int i = 0; i < columnNamesToEdit.Count(); i++)
                        {
                            if (cmd.Parameters.Contains(columnNamesToEdit[i]) == false)
                            { 
                                cmd.Parameters.Add(new SQLiteParameter(columnNamesToEdit[i])); 
                            }
                            cmd.Parameters[columnNamesToEdit[i]].Value = cellValues[i];
                            cmd.CommandText = "UPDATE [" + _tableName + "] SET [" + columnNamesToEdit[i] + "]= @" + columnNamesToEdit[i] + " WHERE rowid=" + _rowIdArray[rowIndices[i]]; // "UPDATE [" & _tableName & "] SET [" & columnNamesToEdit(i) & "]='" & cellValues(i).ToString & "' WHERE rowid=" & _rowIdArray(rowIndices(i))
                            cmd.ExecuteNonQuery();
                        }
                    }
                    tr.Commit();
                }
                if (wasOpen == false)
                { 
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, bool cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, short cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, int cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, long cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, float cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, double cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=@cellValue WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.AddWithValue("@cellValue", cellValue);
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, string cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]= @" + _storedColumnNames[columnIndex] + " WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.Add(new SQLiteParameter(_storedColumnNames[columnIndex], cellValue));
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, byte[] cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=? WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.Add(new SQLiteParameter(_storedColumnNames[columnIndex], DbType.Binary));
                    cmd.Parameters[0].Value = cellValue;
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            /// <param name="columnIndex"></param>
            /// <param name="rowIndex"></param>
            /// <param name="cellValue"></param>
            protected override void EditDatabaseCell(int columnIndex, int rowIndex, DateTime cellValue)
            {
                using (var cmd = new SQLiteCommand("UPDATE [" + _tableName + "] SET [" + _storedColumnNames[columnIndex] + "]=? WHERE rowid=" + _rowIdArray[rowIndex], _dbConnection))
                {
                    cmd.Parameters.Add(new SQLiteParameter(_storedColumnNames[columnIndex], DbType.DateTime));
                    cmd.Parameters[0].Value = cellValue;
                    cmd.ExecuteNonQuery();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, bool[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist.");
                }
                bool localConvertToColumnType() { var tmp = columnData; object argvalue = tmp[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue); tmp[0] = Convert.ToBoolean(argvalue); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records"); 
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", false);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, byte[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                //
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired Field Name: " + columnName + " does not exist.");
                }
                bool localConvertToColumnType() { var tmp1 = columnData; object argvalue1 = tmp1[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue1); tmp1[0] = Convert.ToByte(argvalue1); return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", (byte)0);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }

            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, byte[][] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open();
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp2 = columnData; object argvalue2 = tmp2[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue2); tmp2[0] = (byte[])argvalue2; return ret; }

                if (localConvertToColumnType() == false)
                {
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records"); 
                }
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=? WHERE rowid=";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.Parameters.Add("columnName", DbType.Binary);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.CommandText = commandText + _rowIdArray[i];
                            cmd.Parameters[0].Value = columnData[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, double[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp3 = columnData; object argvalue3 = tmp3[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue3); tmp3[0] = Convert.ToDouble(argvalue3); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", 0.0);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, int[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp4 = columnData; object argvalue4 = tmp4[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue4); tmp4[0] = Convert.ToInt32(argvalue4); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", 0);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, short[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp5 = columnData; object argvalue5 = tmp5[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue5); tmp5[0] = Convert.ToInt16(argvalue5); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", (short)0);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, long[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                {
                    _parentDatabase.Open(); 
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp6 = columnData; object argvalue6 = tmp6[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue6); tmp6[0] = Convert.ToInt64(argvalue6); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                { 
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", 0L);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, float[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                {
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp7 = columnData; object argvalue7 = tmp7[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue7); tmp7[0] = Convert.ToSingle(argvalue7); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'."); 
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                //
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@colValue WHERE rowid=@rowId";
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        cmd.CommandText = commandText;
                        cmd.Parameters.AddWithValue("@colValue", 0f);
                        cmd.Parameters.AddWithValue("@rowId", 0L);
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters["@colValue"].Value = columnData[i];
                            cmd.Parameters["@rowId"].Value = _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override void EditDatabaseColumn(string columnName, string[] columnData)
            {
                ValidateIdentifier(columnName, nameof(columnName));
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open();
                }
                // 
                if (columnData.Count() == 0) { return; }
                int columnIndex = Array.IndexOf(_storedColumnNames, columnName);
                if (columnIndex == -1)
                { 
                    throw new Exception("The desired Field Name: " + columnName + " does not exist."); 
                }
                bool localConvertToColumnType() { var tmp8 = columnData; object argvalue8 = tmp8[0]; var ret = ConvertToColumnType(_storedColumnTypes[columnIndex], ref argvalue8); tmp8[0] = Convert.ToString(argvalue8); return ret; }

                if (localConvertToColumnType() == false)
                { 
                    throw new Exception("The desired Field: '" + columnName + "' is of type '" + _storedColumnTypes[columnIndex].ToString() + "' not of type '" + columnData[0].GetType().ToString() + "'.");
                }
                if (_rowIdArray.Count() != columnData.Count())
                {
                    throw new Exception("The table '" + _tableName + "' does not have: " + columnData.Count() + " records");
                }
                // 
                string commandText = "UPDATE [" + _tableName + "] SET [" + columnName + "]=@" + columnName;
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Parameters.Add(new SQLiteParameter(columnName));
                        cmd.Transaction = trans;
                        for (int i = 0; i < columnData.Count(); i++)
                        {
                            cmd.Parameters[0].Value = columnData[i];
                            cmd.CommandText = commandText + " WHERE rowid=" + _rowIdArray[i];
                            cmd.ExecuteNonQuery();
                        }
                    }
                    trans.Commit();
                }
                if (wasOpen == false)
                { 
                    _parentDatabase.Close(); 
                }
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object GetStoredCell(int storedColumnIndex, int storedRowIndex)
            {
                if (storedColumnIndex < 0 || storedColumnIndex >= _storedColumnNames.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedColumnIndex), storedColumnIndex,
                        $"Column index must be between 0 and {_storedColumnNames.Length - 1}.");
                }
                if (storedRowIndex < 0 || storedRowIndex >= _rowIdArray.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_rowIdArray.Length - 1}.");
                }
                return GetStoredCell(_storedColumnNames[storedColumnIndex], storedRowIndex);
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object GetStoredCell(string storedColumnName, int storedRowIndex)
            {
                if (storedRowIndex < 0 || storedRowIndex >= _rowIdArray.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedRowIndex), storedRowIndex,
                        $"Row index must be between 0 and {_rowIdArray.Length - 1}.");
                }
                using (var command = new SQLiteCommand("SELECT [" + storedColumnName + "] FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndex], _dbConnection))
                {
                    try
                    {
                        return command.ExecuteScalar();
                    }
                    catch (FormatException)
                    {
                        // If DateTime parsing fails, try to get the raw string value and parse it with flexible formats
                        using (var rawCommand = new SQLiteCommand("SELECT CAST([" + storedColumnName + "] AS TEXT) FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndex], _dbConnection))
                        {
                            var rawValue = rawCommand.ExecuteScalar();
                            if (rawValue is string dateString)
                            {
                                return ParseDateTimeFlexible(dateString);
                            }
                            return rawValue;
                        }
                    }
                }
            }

            /// <summary>
            /// Parses a date/time string using multiple common formats.
            /// </summary>
            /// <param name="dateString">The date/time string to parse.</param>
            /// <returns>A DateTime if parsing succeeds, otherwise the original string.</returns>
            private static object ParseDateTimeFlexible(string dateString)
            {
                if (string.IsNullOrWhiteSpace(dateString))
                {
                    return dateString;
                }

                // Try parsing with various common formats
                string[] formats = new[]
                {
                    "M/d/yyyy h:mm:ss tt",      // US format with 12-hour time: 11/5/2017 4:16:02 PM
                    "M/d/yyyy H:mm:ss",         // US format with 24-hour time
                    "M/d/yyyy",                 // US date only
                    "d/M/yyyy h:mm:ss tt",      // UK format with 12-hour time
                    "d/M/yyyy H:mm:ss",         // UK format with 24-hour time
                    "d/M/yyyy",                 // UK date only
                    "yyyy-MM-dd HH:mm:ss",      // ISO format
                    "yyyy-MM-dd",               // ISO date only
                    "yyyy-MM-ddTHH:mm:ss",      // ISO with T separator
                    "yyyy-MM-ddTHH:mm:ss.fff",  // ISO with milliseconds
                    "MM/dd/yyyy HH:mm:ss",      // US with leading zeros
                    "dd/MM/yyyy HH:mm:ss",      // UK with leading zeros
                };

                if (DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime result))
                {
                    return result;
                }

                // Fallback: try general parsing with US culture (most common in databases)
                if (DateTime.TryParse(dateString, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out result))
                {
                    return result;
                }

                // Final fallback: try with current culture
                if (DateTime.TryParse(dateString, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out result))
                {
                    return result;
                }

                // If all parsing fails, return the original string
                return dateString;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
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
                    if (storedRowIndices[i] < 0 || storedRowIndices[i] >= _rowIdArray.Length)
                    {
                        throw new ArgumentOutOfRangeException(nameof(storedRowIndices),
                            $"Row index {storedRowIndices[i]} at position {i} is out of range. Must be between 0 and {_rowIdArray.Length - 1}.");
                    }
                }

                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (wasOpen == false)
                {
                    _parentDatabase.Open();
                }
                //
                var result = new object[(storedColumnIndices.Count())];
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand())
                    {
                        cmd.Transaction = trans;
                        //
                        for (int i = 0; i < storedColumnIndices.Count(); i++)
                        {
                            cmd.CommandText = "SELECT [" + _storedColumnNames[storedColumnIndices[i]] + "] FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndices[i]];
                            try
                            {
                                result[i] = cmd.ExecuteScalar();
                            }
                            catch (FormatException)
                            {
                                // If DateTime parsing fails, try to get the raw string value and parse it with flexible formats
                                cmd.CommandText = "SELECT CAST([" + _storedColumnNames[storedColumnIndices[i]] + "] AS TEXT) FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndices[i]];
                                var rawValue = cmd.ExecuteScalar();
                                if (rawValue is string dateString)
                                {
                                    result[i] = ParseDateTimeFlexible(dateString);
                                }
                                else
                                {
                                    result[i] = rawValue;
                                }
                            }
                        }
                    }
                    trans.Commit();
                }
                //
                if (wasOpen == false)
                {
                    _parentDatabase.Close();
                }
                return result;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object[] GetStoredColumn(int storedColumnIndex)
            {
                if (storedColumnIndex < 0 || storedColumnIndex >= _storedColumnNames.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(storedColumnIndex), storedColumnIndex,
                        $"Column index must be between 0 and {_storedColumnNames.Length - 1}.");
                }
                return GetStoredColumn(_storedColumnNames[storedColumnIndex]);
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object[] GetStoredColumn(string storedColumnName)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (wasOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                var column = new object[(_rowIdArray.Count())];
                using (var cmd = new SQLiteCommand("SELECT [" + storedColumnName + "] FROM [" + _tableName + "]", _dbConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            int counter = 0;
                            while (reader.Read())
                            {
                                column[counter] = reader[storedColumnName];
                                counter += 1;
                            }
                        }
                    }
                }
                // 
                if (wasOpen == false)
                { 
                    _parentDatabase.Close();
                }
                return column;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object[] GetStoredRow(int storedRowIndex)
            {
                var row = new object[1];
                using (var cmd = new SQLiteCommand("SELECT * FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndex], _dbConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                row = new object[reader.FieldCount];

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        row[i] = null;
                                    }
                                    else if (ReferenceEquals(_storedColumnTypes[i], typeof(byte[])))
                                    {
                                        if (reader[i] is byte[])
                                        {
                                            row[i] = reader[i];
                                        }
                                        else if (reader[i] is string)
                                        {
                                            // Optional: convert string to bytes or raise error
                                            row[i] = System.Text.Encoding.UTF8.GetBytes(reader[i].ToString());
                                        }
                                        else
                                        {
                                            throw new InvalidCastException($"Expected Byte() but got {reader[i].GetType().Name} in column {i}.");
                                        }
                                    }
                                    else
                                    {
                                        row[i] = Convert.ChangeType(reader[i], _storedColumnTypes[i]);
                                    }
                                }
                            }
                        }
                    }
                }
                // 
                return row;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object[] GetStoredRow(int storedRowIndex, string[] storedColumnNames)
            {
                string commandString = "SELECT [" + storedColumnNames[0] + "]";
                for (int i = 1; i < storedColumnNames.Count(); i++)
                { 
                    commandString = commandString + ",[" + storedColumnNames[i] + "]";
                }
                commandString = commandString + " FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndex];
                // 
                var row = new object[(_storedColumnNames.Count())];
                using (var cmd = new SQLiteCommand(commandString, _dbConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                reader.GetValues(row); // not changing type here, decided that the time hit to find the column index for each stored column name isn't worth it.
                            }
                        }
                    }
                }
                // 
                return row;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override object[] GetStoredRow(int storedRowIndex, int[] storedColumnIndices)
            {
                string commandString = "SELECT [" + _storedColumnNames[storedColumnIndices[0]] + "]";
                for (int i = 1; i < storedColumnIndices.Count(); i++)
                { 
                    commandString = commandString + ",[" + _storedColumnNames[storedColumnIndices[i]] + "]";
                }
                commandString = commandString + " FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[storedRowIndex];
                // 
                var row = new object[(_storedColumnNames.Count())];
                using (var cmd = new SQLiteCommand(commandString, _dbConnection))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    if (reader.IsDBNull(i))
                                    {
                                        row[i] = reader[i];
                                    }
                                    else
                                    {
                                        row[i] = Convert.ChangeType(reader[i], _storedColumnTypes[storedColumnIndices[i]]);
                                    }
                                }
                            }
                        }
                    }
                }
                // 
                return row;
            }

            /// <summary>
            /// <inheritdoc/>
            /// </summary>
            protected override List<object[]> GetStoredRows(int startStoredRowIndex, int endStoredRowIndex)
            {
                bool wasOpen = _parentDatabase.DataBaseOpen;
                if (_parentDatabase.DataBaseOpen == false)
                { 
                    _parentDatabase.Open(); 
                }
                // 
                var rows = new List<object[]>();
                var row = new object[(_storedColumnNames.Count())];
                using (var trans = _dbConnection.BeginTransaction())
                {
                    using (var cmd = _dbConnection.CreateCommand()) 
                    {
                        cmd.Transaction = trans;
                        for (int i = startStoredRowIndex; i <= endStoredRowIndex; i++)
                        {
                            cmd.CommandText = "SELECT * FROM [" + _tableName + "] WHERE rowid=" + _rowIdArray[i];
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        row = new object[reader.FieldCount];
                                        // reader.GetValues(row)
                                        for (int j = 0; j < reader.FieldCount; j++)
                                        {
                                            if (reader.IsDBNull(j))
                                            {
                                                row[j] = reader[j];
                                            }
                                            else
                                            {
                                                row[j] = Convert.ChangeType(reader[j], _storedColumnTypes[j]);
                                            }
                                        }
                                        rows.Add(row);
                                    }
                                }
                            }
                        }
                    }
                    trans.Commit();
                }
                // 
                if (wasOpen == false)
                {
                    _parentDatabase.Close(); 
                }
                return rows;
            }

        }

        #endregion

        #endregion

    }
}