using System;
using System.Data;
using System.IO;
using System.Data.SQLite;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Integration tests for verifying that multiple components work together correctly.
    /// Tests cover end-to-end workflows, round-trip data operations, and complex editing scenarios.
    /// </summary>
    public class IntegrationTests : IDisposable
    {
        private readonly string _testDbPath;

        /// <summary>
        /// Initializes a new test instance with a unique temporary database file.
        /// </summary>
        public IntegrationTests()
        {
            _testDbPath = Path.Combine(Path.GetTempPath(), $"integration_test_{Guid.NewGuid()}.sqlite");
        }

        /// <summary>
        /// Cleans up the temporary database file after each test.
        /// </summary>
        public void Dispose()
        {
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

        #region SQLiteManager Integration Tests

        /// <summary>
        /// Verifies that creating a table, adding data, and reading it back produces consistent results.
        /// </summary>
        [Fact]
        public void SQLiteManager_CreateTableAddData_RoundTrip()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create table and add data
            var dt = new DataTable("RoundTrip");
            dt.Columns.Add("Id", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Value", typeof(double));
            dt.Rows.Add(1, "First", 10.5);
            dt.Rows.Add(2, "Second", 20.5);
            dt.Rows.Add(3, "Third", 30.5);
            manager.SaveDataTable(dt);

            // Open database before reading
            manager.Open();

            // Read back and verify
            var tableView = manager.GetTableManager("RoundTrip");
            Assert.Equal(3, tableView.NumberOfRows);
            Assert.Equal(3, tableView.ColumnNames.Length);
            // SQLite returns Int64 for integers, so use Convert
            Assert.Equal(1L, Convert.ToInt64(tableView.GetCell(0, 0)));
            Assert.Equal("First", tableView.GetCell(1, 0));
            Assert.Equal(10.5, Convert.ToDouble(tableView.GetCell(2, 0)));

            manager.Close();
        }

        /// <summary>
        /// Verifies that table copy preserves data integrity including all rows and columns.
        /// </summary>
        [Fact]
        public void SQLiteManager_CopyTable_PreservesDataIntegrity()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create source table with diverse data
            var dt = new DataTable("Source");
            dt.Columns.Add("IntCol", typeof(int));
            dt.Columns.Add("StringCol", typeof(string));
            dt.Columns.Add("DoubleCol", typeof(double));
            dt.Rows.Add(1, "A", 1.1);
            dt.Rows.Add(2, "B", 2.2);
            manager.SaveDataTable(dt);

            // Copy table
            manager.CopyTable("Source", "Copy");

            // Open database before reading
            manager.Open();

            // Verify copied data - both tables should have same row count
            var sourceView = manager.GetTableManager("Source");
            var copyView = manager.GetTableManager("Copy");

            Assert.Equal(sourceView.NumberOfRows, copyView.NumberOfRows);
            Assert.Equal(sourceView.ColumnNames.Length, copyView.ColumnNames.Length);

            // Verify values using string conversion to avoid type mismatch
            for (int row = 0; row < sourceView.NumberOfRows; row++)
            {
                for (int col = 0; col < sourceView.ColumnNames.Length; col++)
                {
                    Assert.Equal(
                        Convert.ToString(sourceView.GetCell(col, row)),
                        Convert.ToString(copyView.GetCell(col, row)));
                }
            }

            manager.Close();
        }

        /// <summary>
        /// Verifies that modifying data through DataTableView is persisted correctly when applied.
        /// </summary>
        [Fact]
        public void SQLiteManager_ModifyDataTableView_PersistsCorrectly()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create initial table
            var dt = new DataTable("Modify");
            dt.Columns.Add("Col1", typeof(int));
            dt.Rows.Add(100);
            dt.Rows.Add(200);
            manager.SaveDataTable(dt);

            // Open database before getting view
            manager.Open();

            // Get view and modify
            var tableView = manager.GetTableManager("Modify");
            tableView.EditCell(0, 0, 999);
            tableView.AddRow(new object[] { 300 });
            tableView.ApplyEdits();

            // Verify modifications - use Convert for SQLite type handling
            Assert.Equal(3, tableView.NumberOfRows);
            Assert.Equal(999L, Convert.ToInt64(tableView.GetCell(0, 0)));
            Assert.Equal(300L, Convert.ToInt64(tableView.GetCell(0, 2)));

            manager.Close();
        }

        #endregion

        #region InMemoryReader Integration Tests

        /// <summary>
        /// Verifies that complex editing workflow with undo/redo produces consistent state.
        /// </summary>
        [Fact]
        public void InMemoryReader_ComplexEditWorkflow_ConsistentState()
        {
            var dataTable = new DataTable("Test");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Test");

            // Complex edit sequence
            // EditCell signature: (rowIndex, columnIndex, value)
            tableView.EditCell(0, 0, 10);      // Edit1: row 0, col 0 (int)
            tableView.EditCell(0, 1, "AA");    // Edit2: row 0, col 1 (string)
            tableView.AddRow(new object[] { 3, "C" });  // Edit3

            Assert.Equal(10, Convert.ToInt32(tableView.GetCell(0, 0)));
            Assert.Equal("AA", Convert.ToString(tableView.GetCell(1, 0)));
            Assert.Equal(3, tableView.NumberOfRows);

            // Undo and verify
            tableView.UndoEdit();  // Undo Edit3
            Assert.Equal(2, tableView.NumberOfRows);

            tableView.UndoEdit();  // Undo Edit2
            Assert.Equal("A", Convert.ToString(tableView.GetCell(1, 0)));

            // Redo partial
            tableView.RedoEdit();  // Redo Edit2
            Assert.Equal("AA", Convert.ToString(tableView.GetCell(1, 0)));
        }

        /// <summary>
        /// Verifies that bulk operations (add multiple rows/columns) work correctly together.
        /// </summary>
        [Fact]
        public void InMemoryReader_BulkOperations_WorkTogether()
        {
            var dataTable = new DataTable("Bulk");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Bulk");

            // Add multiple rows
            var newRows = new System.Collections.Generic.List<object[]>
            {
                new object[] { 2 },
                new object[] { 3 },
                new object[] { 4 }
            };
            tableView.AddRows(newRows);
            tableView.ApplyEdits();

            Assert.Equal(4, tableView.NumberOfRows);

            // Add multiple columns
            tableView.AddColumn("Col2", new int[] { 10, 20, 30, 40 });
            tableView.ApplyEdits();

            Assert.Equal(2, tableView.ColumnNames.Length);

            // Verify data integrity
            Assert.Equal(1, tableView.GetCell(0, 0));
            Assert.Equal(10, tableView.GetCell(1, 0));
            Assert.Equal(4, tableView.GetCell(0, 3));
            Assert.Equal(40, tableView.GetCell(1, 3));
        }

        /// <summary>
        /// Verifies that ExportToDataTable produces a correct DataTable with all edits applied.
        /// </summary>
        [Fact]
        public void InMemoryReader_ExportToDataTable_IncludesAllEdits()
        {
            var dataTable = new DataTable("Export");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Export");

            // Make edits and apply
            tableView.EditCell(0, 0, 100);
            tableView.AddRow(new object[] { 3 });
            tableView.ApplyEdits();

            // Export
            var exported = tableView.ExportToDataTable();

            Assert.Equal(3, exported.Rows.Count);
            Assert.Equal(100, exported.Rows[0][0]);
            Assert.Equal(2, exported.Rows[1][0]);
            Assert.Equal(3, exported.Rows[2][0]);
        }

        #endregion

        #region DataTableView Edit Class Integration Tests

        /// <summary>
        /// Verifies that cell edits preserve values correctly through edit operations.
        /// </summary>
        [Fact]
        public void DataTableView_CellEdits_PreserveValueTypes()
        {
            var dataTable = new DataTable("Types");
            dataTable.Columns.Add("IntCol", typeof(int));
            dataTable.Columns.Add("StringCol", typeof(string));
            dataTable.Columns.Add("DoubleCol", typeof(double));
            dataTable.Columns.Add("BoolCol", typeof(bool));
            dataTable.Rows.Add(0, "", 0.0, false);

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Types");

            // Edit each column type
            // EditCell signature: (rowIndex, columnIndex, value)
            tableView.EditCell(0, 0, int.MaxValue);       // row 0, col 0 (IntCol)
            tableView.EditCell(0, 1, "Modified String");  // row 0, col 1 (StringCol)
            tableView.EditCell(0, 2, double.MaxValue);    // row 0, col 2 (DoubleCol)
            tableView.EditCell(0, 3, true);               // row 0, col 3 (BoolCol)
            tableView.ApplyEdits();

            // Verify values are preserved correctly using Convert for safe comparison
            Assert.Equal(int.MaxValue, Convert.ToInt32(tableView.GetCell(0, 0)));
            Assert.Equal("Modified String", Convert.ToString(tableView.GetCell(1, 0)));
            Assert.Equal(double.MaxValue, Convert.ToDouble(tableView.GetCell(2, 0)));
            Assert.True(Convert.ToBoolean(tableView.GetCell(3, 0)));
        }

        /// <summary>
        /// Verifies that row and column index adjustments work correctly when combining operations.
        /// </summary>
        [Fact]
        public void DataTableView_IndexAdjustments_WorkCorrectly()
        {
            var dataTable = new DataTable("Index");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(int));
            dataTable.Columns.Add("Col3", typeof(int));
            dataTable.Rows.Add(1, 2, 3);
            dataTable.Rows.Add(4, 5, 6);
            dataTable.Rows.Add(7, 8, 9);

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Index");

            // Add a new row
            tableView.AddRow(new object[] { 10, 11, 12 });
            tableView.ApplyEdits();

            Assert.Equal(4, tableView.NumberOfRows);
            Assert.Equal(10, tableView.GetCell(0, 3));

            // Add a new column
            tableView.AddColumn("NewCol", new int[] { 100, 200, 300, 400 });
            tableView.ApplyEdits();

            Assert.Equal(4, tableView.ColumnNames.Length);
            Assert.Equal(100, tableView.GetCell(3, 0));
        }

        /// <summary>
        /// Verifies that deleting rows and columns correctly updates indices of remaining edits.
        /// </summary>
        [Fact]
        public void DataTableView_DeleteOperations_UpdateIndicesCorrectly()
        {
            var dataTable = new DataTable("Delete");
            dataTable.Columns.Add("A", typeof(int));
            dataTable.Columns.Add("B", typeof(int));
            dataTable.Columns.Add("C", typeof(int));
            dataTable.Rows.Add(1, 2, 3);
            dataTable.Rows.Add(4, 5, 6);
            dataTable.Rows.Add(7, 8, 9);

            var reader = new InMemoryReader(dataTable);
            var tableView = reader.GetTableManager("Delete");

            // Delete middle row
            tableView.DeleteRow(1);
            tableView.ApplyEdits();

            Assert.Equal(2, tableView.NumberOfRows);
            Assert.Equal(7, tableView.GetCell(0, 1)); // Was row 2, now row 1

            // Delete middle column
            tableView.DeleteColumn(1);
            tableView.ApplyEdits();

            Assert.Equal(2, tableView.ColumnNames.Length);
            Assert.Equal("A", tableView.ColumnNames[0]);
            Assert.Equal("C", tableView.ColumnNames[1]);
            Assert.Equal(9, tableView.GetCell(1, 1)); // Column C, row 1
        }

        #endregion

        #region Multi-Table Integration Tests

        /// <summary>
        /// Verifies that multiple tables can be managed independently within the same database.
        /// </summary>
        [Fact]
        public void SQLiteManager_MultipleTables_IndependentManagement()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create multiple tables
            var dt1 = new DataTable("Table1");
            dt1.Columns.Add("Col1", typeof(int));
            dt1.Rows.Add(1);
            dt1.Rows.Add(2);
            manager.SaveDataTable(dt1);

            var dt2 = new DataTable("Table2");
            dt2.Columns.Add("ColA", typeof(string));
            dt2.Rows.Add("A");
            dt2.Rows.Add("B");
            dt2.Rows.Add("C");
            manager.SaveDataTable(dt2);

            // Verify tables are independent
            var view1 = manager.GetTableManager("Table1");
            var view2 = manager.GetTableManager("Table2");

            Assert.Equal(2, view1.NumberOfRows);
            Assert.Equal(3, view2.NumberOfRows);
            Assert.Equal("Col1", view1.ColumnNames[0]);
            Assert.Equal("ColA", view2.ColumnNames[0]);
        }

        /// <summary>
        /// Verifies that table rename and delete operations work correctly with multiple tables.
        /// </summary>
        [Fact]
        public void SQLiteManager_RenameAndDeleteTables_WorksCorrectly()
        {
            SQLiteManager.CreateSqLiteFile(_testDbPath);
            var manager = new SQLiteManager(_testDbPath);

            // Create tables
            manager.CreateTable("Original", new[] { "Col1" }, new[] { typeof(int) });
            manager.CreateTable("ToDelete", new[] { "Col1" }, new[] { typeof(int) });

            var tableNames = manager.GetTableNames();
            Assert.Contains("Original", tableNames);
            Assert.Contains("ToDelete", tableNames);

            // Rename
            manager.RenameTable("Original", "Renamed");
            tableNames = manager.GetTableNames();
            Assert.DoesNotContain("Original", tableNames);
            Assert.Contains("Renamed", tableNames);

            // Delete
            manager.DeleteTable("ToDelete");
            tableNames = manager.GetTableNames();
            Assert.DoesNotContain("ToDelete", tableNames);
            Assert.Contains("Renamed", tableNames);
        }

        #endregion
    }
}
