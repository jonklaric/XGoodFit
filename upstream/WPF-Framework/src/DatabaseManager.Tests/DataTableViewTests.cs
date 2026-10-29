using System;
using System.Data;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Unit tests for DataTableView functionality.
    /// Tests cover basic properties, column operations, cell operations, undo/redo, and complex edit scenarios.
    /// </summary>
    public class DataTableViewTests
    {
        #region Basic Properties Tests

        /// <summary>
        /// Verifies that the TableName property returns the correct table name.
        /// </summary>
        [Fact]
        public void TableName_ReturnsCorrectName()
        {
            var dataTable = new DataTable("MyTestTable");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("MyTestTable");

            Assert.Equal("MyTestTable", tableManager.TableName);
        }

        /// <summary>
        /// Verifies that the NumberOfRows property returns the correct row count.
        /// </summary>
        [Fact]
        public void NumberOfRows_ReturnsCorrectCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(3, tableManager.NumberOfRows);
        }

        /// <summary>
        /// Verifies that the ColumnNames property returns the correct number of columns.
        /// </summary>
        [Fact]
        public void NumberOfColumns_ReturnsCorrectCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(3, tableManager.ColumnNames.Length);
        }

        #endregion

        #region Column Name Tests

        /// <summary>
        /// Verifies that column names are returned correctly by index.
        /// </summary>
        [Fact]
        public void GetColumnName_ReturnsCorrectName()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("FirstColumn", typeof(int));
            dataTable.Columns.Add("SecondColumn", typeof(string));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("FirstColumn", tableManager.ColumnNames[0]);
            Assert.Equal("SecondColumn", tableManager.ColumnNames[1]);
        }

        /// <summary>
        /// Verifies that column indices can be found by name.
        /// </summary>
        [Fact]
        public void GetColumnIndex_ReturnsCorrectIndex()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("FirstColumn", typeof(int));
            dataTable.Columns.Add("SecondColumn", typeof(string));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(0, Array.IndexOf(tableManager.ColumnNames, "FirstColumn"));
            Assert.Equal(1, Array.IndexOf(tableManager.ColumnNames, "SecondColumn"));
        }

        /// <summary>
        /// Verifies that searching for a non-existent column name returns -1.
        /// </summary>
        [Fact]
        public void GetColumnIndex_WithInvalidName_ReturnsNegativeOne()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(-1, Array.IndexOf(tableManager.ColumnNames, "NonExistentColumn"));
        }

        #endregion

        #region Column Type Tests

        /// <summary>
        /// Verifies that column types are returned correctly.
        /// </summary>
        [Fact]
        public void GetColumnType_ReturnsCorrectType()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("IntCol", typeof(int));
            dataTable.Columns.Add("StringCol", typeof(string));
            dataTable.Columns.Add("DoubleCol", typeof(double));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(typeof(int), tableManager.ColumnTypes[0]);
            Assert.Equal(typeof(string), tableManager.ColumnTypes[1]);
            Assert.Equal(typeof(double), tableManager.ColumnTypes[2]);
        }

        #endregion

        #region GetCell/EditCell Tests

        /// <summary>
        /// Verifies that GetCell returns the correct value at the specified position.
        /// </summary>
        [Fact]
        public void GetCell_ReturnsCorrectValue()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(42, "Hello");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(42, tableManager.GetCell(0, 0));
            Assert.Equal("Hello", tableManager.GetCell(1, 0));
        }

        /// <summary>
        /// Verifies that EditCell updates the cell value correctly.
        /// </summary>
        [Fact]
        public void EditCell_UpdatesValue()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // EditCell takes (rowIndex, columnIndex, value)
            tableManager.EditCell(0, 0, 999);

            // Before ApplyEdits, the edit should be staged and visible through GetCell
            Assert.Equal(999, tableManager.GetCell(0, 0));

            tableManager.ApplyEdits();

            Assert.Equal(999, tableManager.GetCell(0, 0));
        }

        #endregion

        #region CanUndo/CanRedo Tests

        /// <summary>
        /// Verifies that CanUndo returns false when no edits have been made.
        /// </summary>
        [Fact]
        public void CanUndo_ReturnsFalseInitially()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.False(tableManager.CanUndo());
        }

        /// <summary>
        /// Verifies that CanUndo returns true after an edit has been made.
        /// </summary>
        [Fact]
        public void CanUndo_ReturnsTrueAfterEdit()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, 100);

            Assert.True(tableManager.CanUndo());
        }

        /// <summary>
        /// Verifies that CanRedo returns true after an undo operation.
        /// </summary>
        [Fact]
        public void CanRedo_ReturnsTrueAfterUndo()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, 100);
            // Don't call ApplyEdits() - ApplyEdits clears the edit history
            // We want to undo the staged edit to test CanRedo
            tableManager.UndoEdit();

            Assert.True(tableManager.CanRedo());
        }

        #endregion

        #region Complex Edit Scenarios

        /// <summary>
        /// Verifies that multiple columns can be added and applied correctly.
        /// </summary>
        [Fact]
        public void MultipleAddColumns_AppliedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("A");
            dataTable.Rows.Add("B");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add multiple columns with data
            tableManager.AddColumn("IntCol", new int[] { 1, 2 });
            tableManager.AddColumn("DoubleCol", new double[] { 1.5, 2.5 });
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.ColumnNames.Length);
            Assert.Equal("IntCol", tableManager.ColumnNames[1]);
            Assert.Equal("DoubleCol", tableManager.ColumnNames[2]);
            Assert.Equal(1, tableManager.GetCell(1, 0));
            Assert.Equal(2.5, tableManager.GetCell(2, 1));
        }

        /// <summary>
        /// Verifies that adding and then deleting a column works correctly.
        /// </summary>
        [Fact]
        public void AddColumnThenDeleteColumn_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("A");
            dataTable.Rows.Add("B");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add a column
            tableManager.AddColumn("NewCol", new int[] { 1, 2 });
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);

            // Delete the original column
            tableManager.DeleteColumn(0);
            tableManager.ApplyEdits();

            Assert.Single(tableManager.ColumnNames);
            Assert.Equal("NewCol", tableManager.ColumnNames[0]);
        }

        /// <summary>
        /// Verifies that adding and then deleting a row works correctly.
        /// </summary>
        [Fact]
        public void AddRowThenDeleteRow_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add a row
            tableManager.AddRow(new object[] { 3 });
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.NumberOfRows);

            // Delete the middle row
            tableManager.DeleteRow(1);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal(3, tableManager.GetCell(0, 1));
        }

        /// <summary>
        /// Verifies that editing a cell followed by undo and redo maintains correct state.
        /// </summary>
        [Fact]
        public void EditCellThenUndoRedo_MaintainsCorrectState()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(100);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Edit (don't call ApplyEdits - it clears the edit history)
            tableManager.EditCell(0, 0, 200);
            Assert.Equal(200, tableManager.GetCell(0, 0));

            // Undo
            tableManager.UndoEdit();
            Assert.Equal(100, tableManager.GetCell(0, 0));

            // Redo
            tableManager.RedoEdit();
            Assert.Equal(200, tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that CancelEdits removes all pending changes.
        /// </summary>
        [Fact]
        public void CancelEdits_RemovesPendingChanges()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, 999);
            Assert.True(tableManager.CanUndo());

            tableManager.CancelEdits();

            Assert.False(tableManager.CanUndo());
            Assert.Equal(1, tableManager.GetCell(0, 0));
        }

        #endregion

        #region GetCell By Column Name Tests

        /// <summary>
        /// Verifies that GetCell with column name returns the correct value.
        /// </summary>
        [Fact]
        public void GetCell_ByColumnName_ReturnsCorrectValue()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(1, "Alice");
            dataTable.Rows.Add(2, "Bob");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("Alice", tableManager.GetCell("Name", 0));
            Assert.Equal("Bob", tableManager.GetCell("Name", 1));
            Assert.Equal(1, tableManager.GetCell("Id", 0));
        }

        #endregion

        #region GetRow Tests

        /// <summary>
        /// Verifies that GetRow returns all values in the specified row.
        /// </summary>
        [Fact]
        public void GetRow_ReturnsAllValues()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));
            dataTable.Rows.Add(1, "Test", 3.14);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var row = tableManager.GetRow(0);

            Assert.Equal(3, row.Length);
            Assert.Equal(1, row[0]);
            Assert.Equal("Test", row[1]);
            Assert.Equal(3.14, row[2]);
        }

        /// <summary>
        /// Verifies that GetRow with column indices returns selected columns.
        /// </summary>
        [Fact]
        public void GetRow_WithColumnIndices_ReturnsSelectedColumns()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));
            dataTable.Rows.Add(1, "Test", 3.14);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var row = tableManager.GetRow(0, new int[] { 0, 2 });

            Assert.Equal(2, row.Length);
            Assert.Equal(1, row[0]);
            Assert.Equal(3.14, row[1]);
        }

        /// <summary>
        /// Verifies that GetRow with column names returns selected columns.
        /// </summary>
        [Fact]
        public void GetRow_WithColumnNames_ReturnsSelectedColumns()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Columns.Add("Value", typeof(double));
            dataTable.Rows.Add(1, "Test", 3.14);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var row = tableManager.GetRow(0, new string[] { "Id", "Value" });

            Assert.Equal(2, row.Length);
            Assert.Equal(1, row[0]);
            Assert.Equal(3.14, row[1]);
        }

        /// <summary>
        /// Verifies that GetRows returns a range of rows.
        /// </summary>
        [Fact]
        public void GetRows_ReturnsRangeOfRows()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(10);
            dataTable.Rows.Add(20);
            dataTable.Rows.Add(30);
            dataTable.Rows.Add(40);
            dataTable.Rows.Add(50);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var rows = tableManager.GetRows(1, 3);

            Assert.Equal(3, rows.Count);
            Assert.Equal(20, rows[0][0]);
            Assert.Equal(30, rows[1][0]);
            Assert.Equal(40, rows[2][0]);
        }

        #endregion

        #region GetColumn Tests

        /// <summary>
        /// Verifies that GetColumn by index returns all values in the column.
        /// </summary>
        [Fact]
        public void GetColumn_ByIndex_ReturnsAllValues()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            dataTable.Rows.Add(3, "C");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var column = tableManager.GetColumn(0);

            Assert.Equal(3, column.Length);
            Assert.Equal(1, column[0]);
            Assert.Equal(2, column[1]);
            Assert.Equal(3, column[2]);
        }

        /// <summary>
        /// Verifies that GetColumn by name returns all values in the column.
        /// </summary>
        [Fact]
        public void GetColumn_ByName_ReturnsAllValues()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(1, "Alice");
            dataTable.Rows.Add(2, "Bob");
            dataTable.Rows.Add(3, "Charlie");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var column = tableManager.GetColumn("Name");

            Assert.Equal(3, column.Length);
            Assert.Equal("Alice", column[0]);
            Assert.Equal("Bob", column[1]);
            Assert.Equal("Charlie", column[2]);
        }

        #endregion

        #region Bulk Row Operations Tests

        /// <summary>
        /// Verifies that AddRows adds multiple rows correctly.
        /// </summary>
        [Fact]
        public void AddRows_AddsMultipleRows()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var newRows = new System.Collections.Generic.List<object[]>
            {
                new object[] { 2 },
                new object[] { 3 },
                new object[] { 4 }
            };
            tableManager.AddRows(newRows);
            tableManager.ApplyEdits();

            Assert.Equal(4, tableManager.NumberOfRows);
            Assert.Equal(4, tableManager.GetCell(0, 3));
        }

        /// <summary>
        /// Verifies that DeleteRows removes multiple rows correctly.
        /// </summary>
        [Fact]
        public void DeleteRows_RemovesMultipleRows()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(10);
            dataTable.Rows.Add(20);
            dataTable.Rows.Add(30);
            dataTable.Rows.Add(40);
            dataTable.Rows.Add(50);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRows(new int[] { 1, 3 });
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.NumberOfRows);
            Assert.Equal(10, tableManager.GetCell(0, 0));
            Assert.Equal(30, tableManager.GetCell(0, 1));
            Assert.Equal(50, tableManager.GetCell(0, 2));
        }

        /// <summary>
        /// Verifies that DeleteRows with range removes rows correctly.
        /// </summary>
        [Fact]
        public void DeleteRows_WithRange_RemovesCorrectRows()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            for (int i = 0; i < 10; i++)
            {
                dataTable.Rows.Add(i * 10);
            }

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRows(2, 5);
            tableManager.ApplyEdits();

            Assert.Equal(6, tableManager.NumberOfRows);
            Assert.Equal(0, tableManager.GetCell(0, 0));
            Assert.Equal(10, tableManager.GetCell(0, 1));
            Assert.Equal(60, tableManager.GetCell(0, 2));
        }

        #endregion

        #region Bulk Column Operations Tests

        /// <summary>
        /// Verifies that DeleteColumns removes multiple columns correctly.
        /// </summary>
        [Fact]
        public void DeleteColumns_RemovesMultipleColumns()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));
            dataTable.Columns.Add("Col4", typeof(bool));
            dataTable.Rows.Add(1, "A", 1.1, true);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteColumns(new int[] { 1, 3 });
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal("Col1", tableManager.ColumnNames[0]);
            Assert.Equal("Col3", tableManager.ColumnNames[1]);
        }

        #endregion

        #region EditCell By Column Name Tests

        /// <summary>
        /// Verifies that EditCell with column name updates the correct cell.
        /// </summary>
        [Fact]
        public void EditCell_ByColumnName_UpdatesCorrectCell()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("Name", typeof(string));
            dataTable.Rows.Add(1, "Original");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, "Name", "Updated");

            Assert.Equal("Updated", tableManager.GetCell("Name", 0));
        }

        #endregion

        #region GetNumericColumns Tests

        /// <summary>
        /// Verifies that GetNumericColumns returns only numeric column names.
        /// </summary>
        [Fact]
        public void GetNumericColumns_ReturnsOnlyNumericColumns()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("IntCol", typeof(int));
            dataTable.Columns.Add("StringCol", typeof(string));
            dataTable.Columns.Add("DoubleCol", typeof(double));
            dataTable.Columns.Add("BoolCol", typeof(bool));
            dataTable.Columns.Add("FloatCol", typeof(float));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var numericColumns = tableManager.GetNumericColumns();

            Assert.Equal(3, numericColumns.Count);
            Assert.Contains("IntCol", numericColumns);
            Assert.Contains("DoubleCol", numericColumns);
            Assert.Contains("FloatCol", numericColumns);
            Assert.DoesNotContain("StringCol", numericColumns);
            Assert.DoesNotContain("BoolCol", numericColumns);
        }

        #endregion

        #region ExportToDataTable Tests

        /// <summary>
        /// Verifies that ExportToDataTable returns correct data.
        /// </summary>
        [Fact]
        public void ExportToDataTable_ReturnsCorrectData()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            dataTable.Rows.Add(3, "C");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var exported = tableManager.ExportToDataTable();

            Assert.Equal(3, exported.Rows.Count);
            Assert.Equal(2, exported.Columns.Count);
            Assert.Equal(1, exported.Rows[0][0]);
            Assert.Equal("C", exported.Rows[2][1]);
        }

        /// <summary>
        /// Verifies that ExportToDataTable with row/column indices exports selected data.
        /// </summary>
        [Fact]
        public void ExportToDataTable_WithIndices_ExportsSelectedData()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));
            dataTable.Rows.Add(1, "A", 1.1);
            dataTable.Rows.Add(2, "B", 2.2);
            dataTable.Rows.Add(3, "C", 3.3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var exported = tableManager.ExportToDataTable(
                new int[] { 0, 2 },
                new int[] { 0, 2 });

            Assert.Equal(2, exported.Rows.Count);
            Assert.Equal(2, exported.Columns.Count);
            Assert.Equal(1, exported.Rows[0][0]);
            Assert.Equal(3.3, exported.Rows[1][1]);
        }

        #endregion

        #region AddRow Without Data Tests

        /// <summary>
        /// Verifies that AddRow without parameters adds an empty row.
        /// </summary>
        [Fact]
        public void AddRow_WithoutData_AddsEmptyRow()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddRow();
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
        }

        #endregion

        #region AddColumn With Type Tests

        /// <summary>
        /// Verifies that AddColumn with type creates correct column type.
        /// </summary>
        [Fact]
        public void AddColumn_WithType_CreatesCorrectType()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddColumn("NewCol", typeof(double));
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.ColumnNames.Length);
            Assert.Equal(typeof(double), tableManager.ColumnTypes[1]);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifies that an empty table (with columns but no rows) is handled correctly.
        /// </summary>
        [Fact]
        public void EmptyTable_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(0, tableManager.NumberOfRows);
            Assert.Single(tableManager.ColumnNames);
        }

        /// <summary>
        /// Verifies that a large number of edits are applied correctly.
        /// </summary>
        [Fact]
        public void LargeNumberOfEdits_AppliedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            for (int i = 0; i < 100; i++)
            {
                dataTable.Rows.Add(i);
            }

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Make 100 edits
            for (int i = 0; i < 100; i++)
            {
                tableManager.EditCell(i, 0, i * 10);
            }
            tableManager.ApplyEdits();

            // Verify all edits
            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(i * 10, tableManager.GetCell(0, i));
            }
        }

        #endregion
    }
}
