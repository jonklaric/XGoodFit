using System;
using System.Data;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Tests for InMemoryReader class functionality.
    /// </summary>
    public class InMemoryReaderTests
    {
        #region Basic Operations

        /// <summary>
        /// Verifies that constructing an InMemoryReader with a DataTable with no rows creates a valid reader.
        /// </summary>
        [Fact]
        public void Constructor_WithEmptyDataTable_CreatesValidReader()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int)); // Add a column for a valid table structure
            var reader = new InMemoryReader(dataTable);

            Assert.NotNull(reader);
            Assert.True(reader.DataBaseOpen);
            Assert.Equal("TestTable", reader.TableNames[0]);
        }

        /// <summary>
        /// Verifies that constructing an InMemoryReader with a DataTable copies all data correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithDataTable_CopiesData()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "Test");
            dataTable.Rows.Add(2, "Test2");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(2, tableManager.ColumnNames.Length);
        }

        /// <summary>
        /// Verifies that Close sets DataBaseOpen to false.
        /// </summary>
        [Fact]
        public void Close_SetsDataBaseOpenToFalse()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            var reader = new InMemoryReader(dataTable);

            reader.Close();

            Assert.False(reader.DataBaseOpen);
        }

        /// <summary>
        /// Verifies that Open after Close reopens the database.
        /// </summary>
        [Fact]
        public void Open_AfterClose_ReopensDatabase()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            var reader = new InMemoryReader(dataTable);

            reader.Close();
            reader.Open();

            Assert.True(reader.DataBaseOpen);
        }

        #endregion

        #region AddColumn Tests

        /// <summary>
        /// Verifies that adding a column with integer data adds the column correctly with all values.
        /// </summary>
        [Fact]
        public void AddColumn_WithIntData_AddsColumnCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Row1");
            dataTable.Rows.Add("Row2");
            dataTable.Rows.Add("Row3");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            int[] columnData = new int[] { 10, 20, 30 };
            tableManager.AddColumn("IntColumn", columnData);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal(10, tableManager.GetCell(1, 0));
            Assert.Equal(20, tableManager.GetCell(1, 1));
            Assert.Equal(30, tableManager.GetCell(1, 2));
        }

        /// <summary>
        /// Verifies that adding a column with double data adds the column correctly with all values.
        /// </summary>
        [Fact]
        public void AddColumn_WithDoubleData_AddsColumnCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Row1");
            dataTable.Rows.Add("Row2");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            double[] columnData = new double[] { 1.5, 2.5 };
            tableManager.AddColumn("DoubleColumn", columnData);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal(1.5, tableManager.GetCell(1, 0));
            Assert.Equal(2.5, tableManager.GetCell(1, 1));
        }

        /// <summary>
        /// Verifies that adding a column with string data adds the column correctly with all values.
        /// </summary>
        [Fact]
        public void AddColumn_WithStringData_AddsColumnCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            string[] columnData = new string[] { "A", "B" };
            tableManager.AddColumn("StringColumn", columnData);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal("A", tableManager.GetCell(1, 0));
            Assert.Equal("B", tableManager.GetCell(1, 1));
        }

        /// <summary>
        /// Verifies that adding a column with boolean data adds the column correctly with all values.
        /// </summary>
        [Fact]
        public void AddColumn_WithBoolData_AddsColumnCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Row1");
            dataTable.Rows.Add("Row2");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            bool[] columnData = new bool[] { true, false };
            tableManager.AddColumn("BoolColumn", columnData);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal(true, tableManager.GetCell(1, 0));
            Assert.Equal(false, tableManager.GetCell(1, 1));
        }

        /// <summary>
        /// Verifies that adding multiple columns in sequence adds all columns correctly with proper indexing.
        /// </summary>
        [Fact]
        public void AddMultipleColumns_InSequence_AddsAllColumnsCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Row1");
            dataTable.Rows.Add("Row2");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            int[] intData = new int[] { 1, 2 };
            double[] doubleData = new double[] { 1.1, 2.2 };
            string[] stringData = new string[] { "A", "B" };

            tableManager.AddColumn("IntCol", intData);
            tableManager.AddColumn("DoubleCol", doubleData);
            tableManager.AddColumn("StringCol", stringData);
            tableManager.ApplyEdits();

            Assert.Equal(4, tableManager.ColumnNames.Length);
            Assert.Equal(1, tableManager.GetCell(1, 0));
            Assert.Equal(1.1, tableManager.GetCell(2, 0));
            Assert.Equal("A", tableManager.GetCell(3, 0));
        }

        #endregion

        #region DeleteRow Tests

        /// <summary>
        /// Verifies that deleting a row removes it correctly and adjusts remaining row indices.
        /// </summary>
        [Fact]
        public void DeleteRow_RemovesRowCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRow(1);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal(3, tableManager.GetCell(0, 1));
        }

        /// <summary>
        /// Verifies that deleting the first row removes it correctly.
        /// </summary>
        [Fact]
        public void DeleteRow_FirstRow_RemovesCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRow(0);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(2, tableManager.GetCell(0, 0));
            Assert.Equal(3, tableManager.GetCell(0, 1));
        }

        /// <summary>
        /// Verifies that deleting the last row removes it correctly.
        /// </summary>
        [Fact]
        public void DeleteRow_LastRow_RemovesCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRow(2);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal(2, tableManager.GetCell(0, 1));
        }

        /// <summary>
        /// Verifies that deleting multiple rows removes them all correctly.
        /// </summary>
        [Fact]
        public void DeleteMultipleRows_RemovesAllCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);
            dataTable.Rows.Add(4);
            dataTable.Rows.Add(5);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRows(new int[] { 1, 3 });
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.NumberOfRows);
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal(3, tableManager.GetCell(0, 1));
            Assert.Equal(5, tableManager.GetCell(0, 2));
        }

        #endregion

        #region AddRow Tests

        /// <summary>
        /// Verifies that adding a row with data adds the row correctly with all column values.
        /// </summary>
        [Fact]
        public void AddRow_AddsRowCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddRow(new object[] { 2, "B" });
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(2, tableManager.GetCell(0, 1));
            Assert.Equal("B", tableManager.GetCell(1, 1));
        }

        /// <summary>
        /// Verifies that adding multiple rows from a DataTable adds all rows correctly.
        /// </summary>
        [Fact]
        public void AddMultipleRows_AddsAllRowsCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var rowsDataTable = new DataTable();
            rowsDataTable.Columns.Add("Col1", typeof(int));
            rowsDataTable.Rows.Add(1);
            rowsDataTable.Rows.Add(2);
            rowsDataTable.Rows.Add(3);

            tableManager.AddRows(rowsDataTable);
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.NumberOfRows);
        }

        #endregion

        #region DeleteColumn Tests

        /// <summary>
        /// Verifies that deleting a column removes it correctly and adjusts remaining column indices.
        /// </summary>
        [Fact]
        public void DeleteColumn_RemovesColumnCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));
            dataTable.Rows.Add(1, "A", 1.1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteColumn(1);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal("Col1", tableManager.ColumnNames[0]);
            Assert.Equal("Col3", tableManager.ColumnNames[1]);
        }

        #endregion

        #region Cell Edit Tests

        /// <summary>
        /// Verifies that editing a cell updates its value correctly.
        /// </summary>
        [Fact]
        public void EditCell_UpdatesCellCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, 100);
            tableManager.ApplyEdits();

            Assert.Equal(100, tableManager.GetCell(0, 0));
            Assert.Equal(2, tableManager.GetCell(0, 1));
        }

        /// <summary>
        /// Verifies that editing multiple cells applies all edits correctly.
        /// </summary>
        [Fact]
        public void EditCell_WithMultipleEdits_AppliesAllCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, 100);
            tableManager.EditCell(0, 1, "Modified");
            tableManager.EditCell(1, 0, 200);
            tableManager.ApplyEdits();

            Assert.Equal(100, tableManager.GetCell(0, 0));
            Assert.Equal("Modified", tableManager.GetCell(1, 0));
            Assert.Equal(200, tableManager.GetCell(0, 1));
        }

        #endregion

        #region Undo/Redo Tests

        /// <summary>
        /// Verifies that UndoEdit reverts the last staged edit.
        /// </summary>
        [Fact]
        public void UndoEdit_RevertsLastEdit()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Don't call ApplyEdits - it clears the edit history
            tableManager.EditCell(0, 0, 100);
            Assert.Equal(100, tableManager.GetCell(0, 0));

            tableManager.UndoEdit();

            Assert.Equal(1, tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that RedoEdit reapplies an undone edit.
        /// </summary>
        [Fact]
        public void RedoEdit_ReappliesUndoneEdit()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Don't call ApplyEdits - it clears the edit history
            tableManager.EditCell(0, 0, 100);
            tableManager.UndoEdit();
            tableManager.RedoEdit();

            Assert.Equal(100, tableManager.GetCell(0, 0));
        }

        #endregion

        #region GetStoredNumberOfRows/Columns Tests

        /// <summary>
        /// Verifies that GetStoredNumberOfRows returns the correct row count.
        /// </summary>
        [Fact]
        public void GetStoredNumberOfRows_ReturnsCorrectCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);

            Assert.Equal(3, reader.GetStoredNumberOfRows("TestTable"));
        }

        /// <summary>
        /// Verifies that GetStoredNumberOfColumns returns the correct column count.
        /// </summary>
        [Fact]
        public void GetStoredNumberOfColumns_ReturnsCorrectCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));

            var reader = new InMemoryReader(dataTable);

            Assert.Equal(3, reader.GetStoredNumberOfColumns("TestTable"));
        }

        #endregion
    }
}
