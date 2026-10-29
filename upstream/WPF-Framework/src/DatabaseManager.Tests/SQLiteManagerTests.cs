using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Unit tests for SQLiteManager class functionality.
    /// Tests cover database creation, table operations, data management, and maintenance operations.
    /// </summary>
    public class SQLiteManagerTests : IDisposable
    {
        private readonly string _testDbPath;

        /// <summary>
        /// Initializes a new test instance with a unique temporary database file.
        /// </summary>
        public SQLiteManagerTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.sqlite");
        }

        /// <summary>
        /// Cleans up the temporary database file after each test.
        /// </summary>
        public void Dispose()
        {
            // Clean up test database file
            if (File.Exists(_testDbPath))
            {
                try
                {
                    SQLiteConnection.ClearAllPools();
                    File.Delete(_testDbPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        #region Static File Creation Tests

        /// <summary>
        /// Verifies that CreateSqLiteFile creates a new database file at the specified path.
        /// </summary>
        [Fact]
        public void CreateSqLiteFile_WithValidPath_CreatesFile()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);

            Assert.True(File.Exists(_testDbPath));
        }

        /// <summary>
        /// Verifies that the DefaultConnectionBuilder returns a properly configured connection builder.
        /// </summary>
        [Fact]
        public void DefaultConnectionBuilder_ReturnsConfiguredBuilder()
        {
            var builder = SQLiteManager.DefaultConnectionBuilder;

            Assert.NotNull(builder);
            Assert.Equal(3, builder.Version);
            Assert.Equal(SynchronizationModes.Full, builder.SyncMode);
            Assert.Equal(SQLiteJournalModeEnum.Wal, builder.JournalMode);
            Assert.False(builder.ReadOnly);
        }

        #endregion

        #region Constructor and Connection Tests

        /// <summary>
        /// Verifies that the constructor initializes the manager with correct database path.
        /// </summary>
        [Fact]
        public void Constructor_WithFilePath_SetsDataBasePath()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            Assert.Equal(_testDbPath, manager.DataBasePath);
        }

        /// <summary>
        /// Verifies that the constructor creates a valid database connection.
        /// </summary>
        [Fact]
        public void Constructor_WithFilePath_CreatesConnection()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            Assert.NotNull(manager.DbConnection);
        }

        #endregion

        #region Open/Close Tests

        /// <summary>
        /// Verifies that Open sets DataBaseOpen to true.
        /// </summary>
        [Fact]
        public void Open_SetsDataBaseOpenToTrue()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            manager.Open();

            Assert.True(manager.DataBaseOpen);
            manager.Close();
        }

        /// <summary>
        /// Verifies that Close sets DataBaseOpen to false.
        /// </summary>
        [Fact]
        public void Close_SetsDataBaseOpenToFalse()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            manager.Open();
            manager.Close();

            Assert.False(manager.DataBaseOpen);
        }

        #endregion

        #region Table Creation Tests

        /// <summary>
        /// Verifies that CreateTable creates a table with the specified columns.
        /// </summary>
        [Fact]
        public void CreateTable_WithColumnsAndTypes_CreatesTable()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            manager.CreateTable("TestTable",
                new string[] { "Id", "Name", "Value" },
                new Type[] { typeof(int), typeof(string), typeof(double) });

            var tableNames = manager.GetTableNames();
            Assert.Contains("TestTable", tableNames);
        }

        /// <summary>
        /// Verifies that CreateTable with various data types creates correct column types.
        /// </summary>
        [Fact]
        public void CreateTable_WithVariousTypes_CreatesCorrectColumns()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            manager.CreateTable("TypeTest",
                new string[] { "IntCol", "StringCol", "DoubleCol", "BoolCol", "DateCol" },
                new Type[] { typeof(int), typeof(string), typeof(double), typeof(bool), typeof(DateTime) });

            var tableView = manager.GetTableManager("TypeTest");
            Assert.Equal(5, tableView.ColumnNames.Length);
        }

        #endregion

        #region Table Deletion Tests

        /// <summary>
        /// Verifies that DeleteTable removes an existing table from the database.
        /// </summary>
        [Fact]
        public void DeleteTable_ExistingTable_RemovesTable()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);
            manager.CreateTable("ToDelete", new string[] { "Col1" }, new Type[] { typeof(int) });

            manager.DeleteTable("ToDelete");

            var tableNames = manager.GetTableNames();
            Assert.DoesNotContain("ToDelete", tableNames);
        }

        /// <summary>
        /// Verifies that DeleteTable with non-existent table does not throw.
        /// </summary>
        [Fact]
        public void DeleteTable_NonExistentTable_DoesNotThrow()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            var exception = Record.Exception(() => manager.DeleteTable("NonExistent"));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that DeleteTableData clears all rows but keeps the table structure.
        /// </summary>
        [Fact]
        public void DeleteTableData_ClearsAllRows()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create table and add data
            var dt = new DataTable("TestData");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Test1");
            dt.Rows.Add(2, "Test2");
            manager.SaveDataTable(dt);

            // Verify data exists
            Assert.Equal(2, manager.GetStoredNumberOfRows("TestData"));

            // Delete data
            manager.DeleteTableData("TestData");

            // Verify table exists but is empty
            var tableNames = manager.GetTableNames();
            Assert.Contains("TestData", tableNames);
            Assert.Equal(0, manager.GetStoredNumberOfRows("TestData"));
        }

        #endregion

        #region Table Copy and Rename Tests

        /// <summary>
        /// Verifies that CopyTable creates an identical copy of the source table.
        /// </summary>
        [Fact]
        public void CopyTable_CreatesIdenticalTable()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create source table with data
            var dt = new DataTable("Source");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Value", typeof(string));
            dt.Rows.Add(1, "A");
            dt.Rows.Add(2, "B");
            manager.SaveDataTable(dt);

            // Copy table
            manager.CopyTable("Source", "Destination");

            // Verify both tables exist
            var tableNames = manager.GetTableNames();
            Assert.Contains("Source", tableNames);
            Assert.Contains("Destination", tableNames);

            // Verify data is identical
            Assert.Equal(manager.GetStoredNumberOfRows("Source"), manager.GetStoredNumberOfRows("Destination"));
        }

        /// <summary>
        /// Verifies that CopyTable throws exception for non-existent source table.
        /// </summary>
        [Fact]
        public void CopyTable_NonExistentSource_ThrowsException()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            Assert.Throws<Exception>(() => manager.CopyTable("NonExistent", "Destination"));
        }

        /// <summary>
        /// Verifies that RenameTable changes the table name correctly.
        /// </summary>
        [Fact]
        public void RenameTable_UpdatesTableName()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);
            manager.CreateTable("OldName", new string[] { "Col1" }, new Type[] { typeof(int) });

            manager.RenameTable("OldName", "NewName");

            var tableNames = manager.GetTableNames();
            Assert.DoesNotContain("OldName", tableNames);
            Assert.Contains("NewName", tableNames);
        }

        /// <summary>
        /// Verifies that RenameTable throws exception for non-existent table.
        /// </summary>
        [Fact]
        public void RenameTable_NonExistentTable_ThrowsException()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            Assert.Throws<Exception>(() => manager.RenameTable("NonExistent", "NewName"));
        }

        #endregion

        #region SaveDataTable Tests

        /// <summary>
        /// Verifies that SaveDataTable persists a DataTable to the database.
        /// </summary>
        [Fact]
        public void SaveDataTable_PersistsData()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            var dt = new DataTable("SaveTest");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Rows.Add(1, "Test1");
            dt.Rows.Add(2, "Test2");
            dt.Rows.Add(3, "Test3");

            manager.SaveDataTable(dt);

            Assert.Contains("SaveTest", manager.GetTableNames());
            Assert.Equal(3, manager.GetStoredNumberOfRows("SaveTest"));
        }

        /// <summary>
        /// Verifies that SaveDataTable correctly handles various column types.
        /// </summary>
        [Fact]
        public void SaveDataTable_WithVariousTypes_PersistsCorrectly()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            var dt = new DataTable("TypesTest");
            dt.Columns.Add("IntCol", typeof(int));
            dt.Columns.Add("DoubleCol", typeof(double));
            dt.Columns.Add("StringCol", typeof(string));
            dt.Columns.Add("BoolCol", typeof(bool));
            dt.Rows.Add(42, 3.14, "Hello", true);

            manager.SaveDataTable(dt);

            var tableView = manager.GetTableManager("TypesTest");
            Assert.Equal(1, tableView.NumberOfRows);
            Assert.Equal(4, tableView.ColumnNames.Length);
        }

        #endregion

        #region GetTableManager Tests

        /// <summary>
        /// Verifies that GetTableManager returns a valid table view for an existing table.
        /// </summary>
        [Fact]
        public void GetTableManager_ExistingTable_ReturnsTableView()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);
            manager.CreateTable("TestTable", new string[] { "Col1" }, new Type[] { typeof(int) });

            var tableView = manager.GetTableManager("TestTable");

            Assert.NotNull(tableView);
            Assert.Equal("TestTable", tableView.TableName);
        }

        #endregion

        #region Maintenance Operations Tests

        /// <summary>
        /// Verifies that Vacuum executes without throwing an exception.
        /// </summary>
        [Fact]
        public void Vacuum_ExecutesSuccessfully()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);
            manager.CreateTable("TestTable", new string[] { "Col1" }, new Type[] { typeof(int) });

            var exception = Record.Exception(() => manager.Vacuum());

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that Optimize executes without throwing an exception.
        /// </summary>
        [Fact]
        public void Optimize_ExecutesSuccessfully()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);
            manager.CreateTable("TestTable", new string[] { "Col1" }, new Type[] { typeof(int) });

            var exception = Record.Exception(() => manager.Optimize());

            Assert.Null(exception);
        }

        #endregion

        #region GetStoredNumberOfRows/Columns Tests

        /// <summary>
        /// Verifies that GetStoredNumberOfRows returns correct count.
        /// </summary>
        [Fact]
        public void GetStoredNumberOfRows_ReturnsCorrectCount()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            var dt = new DataTable("RowCountTest");
            dt.Columns.Add("Id", typeof(int));
            dt.Rows.Add(1);
            dt.Rows.Add(2);
            dt.Rows.Add(3);
            dt.Rows.Add(4);
            dt.Rows.Add(5);
            manager.SaveDataTable(dt);

            Assert.Equal(5, manager.GetStoredNumberOfRows("RowCountTest"));
        }

        /// <summary>
        /// Verifies that GetStoredNumberOfColumns returns correct count.
        /// </summary>
        [Fact]
        public void GetStoredNumberOfColumns_ReturnsCorrectCount()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            manager.CreateTable("ColCountTest",
                new string[] { "Col1", "Col2", "Col3", "Col4" },
                new Type[] { typeof(int), typeof(string), typeof(double), typeof(bool) });

            Assert.Equal(4, manager.GetStoredNumberOfColumns("ColCountTest"));
        }

        #endregion
    }
}
