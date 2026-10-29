using System;
using System.Data;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Unit tests for edge cases including null handling, empty arrays, boundary conditions,
    /// type conversions, sequential operations, large datasets, and special character handling.
    /// </summary>
    public class EdgeCaseTests
    {
        #region DBNull Value Handling

        /// <summary>
        /// Verifies that GetCell returns DBNull.Value for cells containing DBNull.
        /// </summary>
        [Fact]
        public void GetCell_WithDBNullValue_ReturnsDBNull()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            var row = dataTable.NewRow();
            row["Col1"] = DBNull.Value;
            row["Col2"] = DBNull.Value;
            dataTable.Rows.Add(row);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(DBNull.Value, tableManager.GetCell(0, 0));
            Assert.Equal(DBNull.Value, tableManager.GetCell(1, 0));
        }

        /// <summary>
        /// Verifies that EditCell can set a cell value to DBNull.Value.
        /// </summary>
        [Fact]
        public void EditCell_WithDBNull_SetsValueCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(42);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, DBNull.Value);
            // Staged edit should be visible through GetCell
            Assert.Equal(DBNull.Value, tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that AddRow correctly handles rows containing only DBNull values.
        /// </summary>
        [Fact]
        public void AddRow_WithDBNullValues_AddsCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("IntCol", typeof(int));
            dataTable.Columns.Add("StringCol", typeof(string));
            dataTable.Columns.Add("DoubleCol", typeof(double));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddRow(new object[] { DBNull.Value, DBNull.Value, DBNull.Value });
            tableManager.ApplyEdits();

            Assert.Equal(1, tableManager.NumberOfRows);
            Assert.Equal(DBNull.Value, tableManager.GetCell(0, 0));
            Assert.Equal(DBNull.Value, tableManager.GetCell(1, 0));
            Assert.Equal(DBNull.Value, tableManager.GetCell(2, 0));
        }

        /// <summary>
        /// Verifies that mixed DBNull and valid values are handled correctly.
        /// </summary>
        [Fact]
        public void MixedDBNullAndValidValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));

            var row1 = dataTable.NewRow();
            row1["Col1"] = 1;
            row1["Col2"] = "Valid";
            dataTable.Rows.Add(row1);

            var row2 = dataTable.NewRow();
            row2["Col1"] = DBNull.Value;
            row2["Col2"] = DBNull.Value;
            dataTable.Rows.Add(row2);

            var row3 = dataTable.NewRow();
            row3["Col1"] = 3;
            row3["Col2"] = DBNull.Value;
            dataTable.Rows.Add(row3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal("Valid", tableManager.GetCell(1, 0));
            Assert.Equal(DBNull.Value, tableManager.GetCell(0, 1));
            Assert.Equal(DBNull.Value, tableManager.GetCell(1, 1));
            Assert.Equal(3, tableManager.GetCell(0, 2));
            Assert.Equal(DBNull.Value, tableManager.GetCell(1, 2));
        }

        #endregion

        #region Empty Array Handling

        /// <summary>
        /// Verifies that creating a reader with no rows produces a valid table manager.
        /// </summary>
        [Fact]
        public void Constructor_WithNoRows_CreatesValidReader()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            // No rows added

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(0, tableManager.NumberOfRows);
            Assert.Equal(2, tableManager.ColumnNames.Length);
        }

        /// <summary>
        /// Verifies that deleting all rows leaves the table empty.
        /// </summary>
        [Fact]
        public void DeleteAllRows_LeavesEmptyTable()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteRow(0);
            tableManager.ApplyEdits();
            tableManager.DeleteRow(0);
            tableManager.ApplyEdits();

            Assert.Equal(0, tableManager.NumberOfRows);
            Assert.Single(tableManager.ColumnNames);
        }

        /// <summary>
        /// Verifies that deleting all columns leaves the table with no columns.
        /// </summary>
        [Fact]
        public void DeleteAllColumns_LeavesTableWithNoColumns()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.DeleteColumn(0);
            tableManager.ApplyEdits();

            Assert.Empty(tableManager.ColumnNames);
        }

        /// <summary>
        /// Verifies that adding a row to an empty table works correctly.
        /// </summary>
        [Fact]
        public void AddRowToEmptyTable_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            // No rows

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddRow(new object[] { 42, "Test" });
            tableManager.ApplyEdits();

            Assert.Equal(1, tableManager.NumberOfRows);
            Assert.Equal(42, tableManager.GetCell(0, 0));
            Assert.Equal("Test", tableManager.GetCell(1, 0));
        }

        #endregion

        #region Empty String Handling

        /// <summary>
        /// Verifies that empty strings are stored and retrieved correctly.
        /// </summary>
        [Fact]
        public void EmptyString_StoredAndRetrievedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("");
            dataTable.Rows.Add("Not Empty");
            dataTable.Rows.Add("");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("", tableManager.GetCell(0, 0));
            Assert.Equal("Not Empty", tableManager.GetCell(0, 1));
            Assert.Equal("", tableManager.GetCell(0, 2));
        }

        /// <summary>
        /// Verifies that editing a cell to an empty string works correctly.
        /// </summary>
        [Fact]
        public void EditCell_ToEmptyString_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Original");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.EditCell(0, 0, "");
            tableManager.ApplyEdits();

            Assert.Equal("", tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that adding a column with empty strings works correctly.
        /// </summary>
        [Fact]
        public void AddColumn_WithEmptyStrings_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            tableManager.AddColumn("StringCol", new string[] { "", "" });
            tableManager.ApplyEdits();

            Assert.Equal("", tableManager.GetCell(1, 0));
            Assert.Equal("", tableManager.GetCell(1, 1));
        }

        #endregion

        #region Boundary Conditions

        /// <summary>
        /// Verifies that GetCell works correctly at boundary indices (corners of the table).
        /// </summary>
        [Fact]
        public void GetCell_AtBoundaryIndices_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(int));
            dataTable.Columns.Add("Col3", typeof(int));
            dataTable.Rows.Add(1, 2, 3);
            dataTable.Rows.Add(4, 5, 6);
            dataTable.Rows.Add(7, 8, 9);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // First row, first column
            Assert.Equal(1, tableManager.GetCell(0, 0));
            // Last row, last column
            Assert.Equal(9, tableManager.GetCell(2, 2));
            // First row, last column
            Assert.Equal(3, tableManager.GetCell(2, 0));
            // Last row, first column
            Assert.Equal(7, tableManager.GetCell(0, 2));
        }

        /// <summary>
        /// Verifies that deleting rows at the first and last positions works correctly.
        /// </summary>
        [Fact]
        public void DeleteRow_AtBoundaries_WorksCorrectly()
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

            // Delete last row
            tableManager.DeleteRow(4);
            tableManager.ApplyEdits();
            Assert.Equal(4, tableManager.NumberOfRows);

            // Delete first row
            tableManager.DeleteRow(0);
            tableManager.ApplyEdits();
            Assert.Equal(3, tableManager.NumberOfRows);
            Assert.Equal(2, tableManager.GetCell(0, 0)); // Was originally row 2
        }

        /// <summary>
        /// Verifies that all operations work correctly on a single row, single column table.
        /// </summary>
        [Fact]
        public void SingleRowSingleColumn_AllOperationsWork()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(42);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(1, tableManager.NumberOfRows);
            Assert.Single(tableManager.ColumnNames);
            Assert.Equal(42, tableManager.GetCell(0, 0));

            // Edit (don't call ApplyEdits - it clears edit history)
            tableManager.EditCell(0, 0, 100);
            Assert.Equal(100, tableManager.GetCell(0, 0));

            // Undo
            tableManager.UndoEdit();
            Assert.Equal(42, tableManager.GetCell(0, 0));

            // Redo
            tableManager.RedoEdit();
            Assert.Equal(100, tableManager.GetCell(0, 0));
        }

        #endregion

        #region Type Conversion Edge Cases

        /// <summary>
        /// Verifies that zero values for various numeric types are handled correctly.
        /// </summary>
        [Fact]
        public void NumericTypes_ZeroValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("IntCol", typeof(int));
            dataTable.Columns.Add("DoubleCol", typeof(double));
            dataTable.Columns.Add("FloatCol", typeof(float));
            dataTable.Columns.Add("LongCol", typeof(long));
            dataTable.Rows.Add(0, 0.0, 0.0f, 0L);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(0, tableManager.GetCell(0, 0));
            Assert.Equal(0.0, tableManager.GetCell(1, 0));
            Assert.Equal(0.0f, tableManager.GetCell(2, 0));
            Assert.Equal(0L, tableManager.GetCell(3, 0));
        }

        /// <summary>
        /// Verifies that maximum and minimum values for numeric types are handled correctly.
        /// </summary>
        [Fact]
        public void NumericTypes_MaxMinValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("IntMax", typeof(int));
            dataTable.Columns.Add("IntMin", typeof(int));
            dataTable.Columns.Add("DoubleMax", typeof(double));
            dataTable.Rows.Add(int.MaxValue, int.MinValue, double.MaxValue);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(int.MaxValue, tableManager.GetCell(0, 0));
            Assert.Equal(int.MinValue, tableManager.GetCell(1, 0));
            Assert.Equal(double.MaxValue, tableManager.GetCell(2, 0));
        }

        /// <summary>
        /// Verifies that boolean true and false values are handled correctly.
        /// </summary>
        [Fact]
        public void BooleanType_TrueAndFalse_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("BoolCol", typeof(bool));
            dataTable.Rows.Add(true);
            dataTable.Rows.Add(false);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(true, tableManager.GetCell(0, 0));
            Assert.Equal(false, tableManager.GetCell(0, 1));
        }

        #endregion

        #region Multiple Sequential Operations

        /// <summary>
        /// Verifies that multiple undo and redo operations maintain correct state.
        /// </summary>
        [Fact]
        public void MultipleUndoRedo_MaintainsCorrectState()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(0);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Make multiple edits (don't call ApplyEdits - it clears edit history)
            tableManager.EditCell(0, 0, 1);
            tableManager.EditCell(0, 0, 2);
            tableManager.EditCell(0, 0, 3);

            Assert.Equal(3, tableManager.GetCell(0, 0));

            // Undo all
            tableManager.UndoEdit();
            Assert.Equal(2, tableManager.GetCell(0, 0));
            tableManager.UndoEdit();
            Assert.Equal(1, tableManager.GetCell(0, 0));
            tableManager.UndoEdit();
            Assert.Equal(0, tableManager.GetCell(0, 0));

            // Redo all
            tableManager.RedoEdit();
            Assert.Equal(1, tableManager.GetCell(0, 0));
            tableManager.RedoEdit();
            Assert.Equal(2, tableManager.GetCell(0, 0));
            tableManager.RedoEdit();
            Assert.Equal(3, tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that add-delete-add row operations maintain correct row count.
        /// </summary>
        [Fact]
        public void AddDeleteAddRow_MaintainsCorrectRowCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add rows (staged edits are visible immediately)
            tableManager.AddRow(new object[] { 1 });
            tableManager.AddRow(new object[] { 2 });
            Assert.Equal(2, tableManager.NumberOfRows);

            // Delete a row
            tableManager.DeleteRow(0);
            Assert.Equal(1, tableManager.NumberOfRows);

            // Add more rows
            tableManager.AddRow(new object[] { 3 });
            tableManager.AddRow(new object[] { 4 });
            Assert.Equal(3, tableManager.NumberOfRows);
        }

        /// <summary>
        /// Verifies that add-delete-add column operations maintain correct column count.
        /// </summary>
        [Fact]
        public void AddDeleteAddColumn_MaintainsCorrectColumnCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Original", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add a column
            tableManager.AddColumn("Col2", new int[] { 2 });
            tableManager.ApplyEdits();
            Assert.Equal(2, tableManager.ColumnNames.Length);

            // Delete the original column
            tableManager.DeleteColumn(0);
            tableManager.ApplyEdits();
            Assert.Single(tableManager.ColumnNames);
            Assert.Equal("Col2", tableManager.ColumnNames[0]);

            // Add another column
            tableManager.AddColumn("Col3", new int[] { 3 });
            tableManager.ApplyEdits();
            Assert.Equal(2, tableManager.ColumnNames.Length);
        }

        #endregion

        #region Large Dataset Tests

        /// <summary>
        /// Verifies that a large dataset with 1000 rows is handled correctly.
        /// </summary>
        [Fact]
        public void LargeDataset_1000Rows_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Columns.Add("Col3", typeof(double));

            for (int i = 0; i < 1000; i++)
            {
                dataTable.Rows.Add(i, $"Row{i}", i * 1.5);
            }

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(1000, tableManager.NumberOfRows);
            Assert.Equal(3, tableManager.ColumnNames.Length);

            // Verify first, middle, and last rows
            Assert.Equal(0, tableManager.GetCell(0, 0));
            Assert.Equal("Row500", tableManager.GetCell(1, 500));
            Assert.Equal(999 * 1.5, tableManager.GetCell(2, 999));
        }

        /// <summary>
        /// Verifies that a table with many columns (100) is handled correctly.
        /// </summary>
        [Fact]
        public void LargeDataset_ManyColumns_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");

            for (int i = 0; i < 100; i++)
            {
                dataTable.Columns.Add($"Col{i}", typeof(int));
            }

            var row = dataTable.NewRow();
            for (int i = 0; i < 100; i++)
            {
                row[i] = i;
            }
            dataTable.Rows.Add(row);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(100, tableManager.ColumnNames.Length);
            Assert.Equal(0, tableManager.GetCell(0, 0));
            Assert.Equal(50, tableManager.GetCell(50, 0));
            Assert.Equal(99, tableManager.GetCell(99, 0));
        }

        #endregion

        #region Special Character Handling

        /// <summary>
        /// Verifies that strings with special characters (tabs, newlines, quotes, unicode) are handled correctly.
        /// </summary>
        [Fact]
        public void StringWithSpecialCharacters_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("Hello\tWorld");
            dataTable.Rows.Add("Line1\nLine2");
            dataTable.Rows.Add("Quote\"Test");
            dataTable.Rows.Add("Comma,Test");
            dataTable.Rows.Add("Unicode: \u00E9\u00F1\u00FC");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("Hello\tWorld", tableManager.GetCell(0, 0));
            Assert.Equal("Line1\nLine2", tableManager.GetCell(0, 1));
            Assert.Equal("Quote\"Test", tableManager.GetCell(0, 2));
            Assert.Equal("Comma,Test", tableManager.GetCell(0, 3));
            Assert.Equal("Unicode: \u00E9\u00F1\u00FC", tableManager.GetCell(0, 4));
        }

        /// <summary>
        /// Verifies that column names with special characters (spaces, dashes, underscores) are handled correctly.
        /// </summary>
        [Fact]
        public void ColumnNameWithSpecialCharacters_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Column With Spaces", typeof(int));
            dataTable.Columns.Add("Column-With-Dashes", typeof(int));
            dataTable.Columns.Add("Column_With_Underscores", typeof(int));
            dataTable.Rows.Add(1, 2, 3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("Column With Spaces", tableManager.ColumnNames[0]);
            Assert.Equal("Column-With-Dashes", tableManager.ColumnNames[1]);
            Assert.Equal("Column_With_Underscores", tableManager.ColumnNames[2]);

            Assert.Equal(0, Array.IndexOf(tableManager.ColumnNames, "Column With Spaces"));
            Assert.Equal(1, Array.IndexOf(tableManager.ColumnNames, "Column-With-Dashes"));
            Assert.Equal(2, Array.IndexOf(tableManager.ColumnNames, "Column_With_Underscores"));
        }

        #endregion

        #region Concurrent Edit Operations

        /// <summary>
        /// Verifies that interleaved add and edit operations produce correct results.
        /// </summary>
        [Fact]
        public void InterleavedAddAndEdit_ProducesCorrectResults()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add row, edit original, add another row, edit new row
            // EditCell signature: (rowIndex, columnIndex, value)
            tableManager.AddRow(new object[] { 2 });
            tableManager.EditCell(0, 0, 10);   // Edit row 0, col 0
            tableManager.AddRow(new object[] { 3 });
            tableManager.EditCell(2, 0, 30);   // Edit row 2, col 0
            tableManager.ApplyEdits();

            Assert.Equal(3, tableManager.NumberOfRows);
            Assert.Equal(10, Convert.ToInt32(tableManager.GetCell(0, 0)));
            Assert.Equal(2, Convert.ToInt32(tableManager.GetCell(0, 1)));
            Assert.Equal(30, Convert.ToInt32(tableManager.GetCell(0, 2)));
        }

        /// <summary>
        /// Verifies that deleting a row works correctly and remaining rows are accessible.
        /// </summary>
        [Fact]
        public void DeleteRow_RemainingRowsAccessible()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            dataTable.Rows.Add(3);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Delete middle row
            tableManager.DeleteRow(1);
            tableManager.ApplyEdits();

            Assert.Equal(2, tableManager.NumberOfRows);
            Assert.Equal(1, Convert.ToInt32(tableManager.GetCell(0, 0)));
            Assert.Equal(3, Convert.ToInt32(tableManager.GetCell(0, 1)));
        }

        /// <summary>
        /// Verifies that multiple column additions with data are tracked correctly.
        /// </summary>
        [Fact]
        public void MultipleColumnAdditions_TrackedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Original", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Add multiple columns
            tableManager.AddColumn("Col2", new int[] { 10, 20 });
            tableManager.AddColumn("Col3", new int[] { 100, 200 });
            tableManager.AddColumn("Col4", new int[] { 1000, 2000 });
            tableManager.ApplyEdits();

            Assert.Equal(4, tableManager.ColumnNames.Length);
            Assert.Equal("Original", tableManager.ColumnNames[0]);
            Assert.Equal("Col2", tableManager.ColumnNames[1]);
            Assert.Equal("Col3", tableManager.ColumnNames[2]);
            Assert.Equal("Col4", tableManager.ColumnNames[3]);
            Assert.Equal(1000, tableManager.GetCell(3, 0));
            Assert.Equal(2000, tableManager.GetCell(3, 1));
        }

        #endregion

        #region Negative Value Handling

        /// <summary>
        /// Verifies that negative integer values are handled correctly.
        /// </summary>
        [Fact]
        public void NegativeIntegerValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(-1);
            dataTable.Rows.Add(-100);
            dataTable.Rows.Add(int.MinValue);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(-1, tableManager.GetCell(0, 0));
            Assert.Equal(-100, tableManager.GetCell(0, 1));
            Assert.Equal(int.MinValue, tableManager.GetCell(0, 2));
        }

        /// <summary>
        /// Verifies that negative double values are handled correctly.
        /// </summary>
        [Fact]
        public void NegativeDoubleValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(double));
            dataTable.Rows.Add(-1.5);
            dataTable.Rows.Add(-0.001);
            dataTable.Rows.Add(double.MinValue);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(-1.5, tableManager.GetCell(0, 0));
            Assert.Equal(-0.001, tableManager.GetCell(0, 1));
            Assert.Equal(double.MinValue, tableManager.GetCell(0, 2));
        }

        #endregion

        #region DateTime Edge Cases

        /// <summary>
        /// Verifies that DateTime values including min, max, and current are handled correctly.
        /// </summary>
        [Fact]
        public void DateTimeValues_HandledCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("DateCol", typeof(DateTime));
            var specificDate = new DateTime(2024, 6, 15, 14, 30, 45);
            dataTable.Rows.Add(DateTime.MinValue);
            dataTable.Rows.Add(DateTime.MaxValue);
            dataTable.Rows.Add(specificDate);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(DateTime.MinValue, tableManager.GetCell(0, 0));
            Assert.Equal(DateTime.MaxValue, tableManager.GetCell(0, 1));
            Assert.Equal(specificDate, tableManager.GetCell(0, 2));
        }

        /// <summary>
        /// Verifies that editing DateTime values works correctly with ApplyEdits.
        /// </summary>
        [Fact]
        public void EditDateTimeValue_WorksCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("DateCol", typeof(DateTime));
            var originalDate = new DateTime(2020, 1, 1);
            dataTable.Rows.Add(originalDate);

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Verify original value
            Assert.Equal(originalDate, tableManager.GetCell(0, 0));

            // Edit and apply
            var newDate = new DateTime(2025, 1, 1);
            tableManager.EditCell(0, 0, newDate);
            tableManager.ApplyEdits();

            // Verify edit was applied
            Assert.Equal(newDate, tableManager.GetCell(0, 0));
        }

        #endregion

        #region Whitespace String Handling

        /// <summary>
        /// Verifies that strings containing only whitespace are preserved correctly.
        /// </summary>
        [Fact]
        public void WhitespaceOnlyStrings_PreservedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add(" ");
            dataTable.Rows.Add("  ");
            dataTable.Rows.Add("\t");
            dataTable.Rows.Add("   \t   ");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal(" ", tableManager.GetCell(0, 0));
            Assert.Equal("  ", tableManager.GetCell(0, 1));
            Assert.Equal("\t", tableManager.GetCell(0, 2));
            Assert.Equal("   \t   ", tableManager.GetCell(0, 3));
        }

        /// <summary>
        /// Verifies that strings with leading and trailing whitespace are preserved.
        /// </summary>
        [Fact]
        public void LeadingTrailingWhitespace_PreservedCorrectly()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(string));
            dataTable.Rows.Add("  leading");
            dataTable.Rows.Add("trailing  ");
            dataTable.Rows.Add("  both  ");

            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Equal("  leading", tableManager.GetCell(0, 0));
            Assert.Equal("trailing  ", tableManager.GetCell(0, 1));
            Assert.Equal("  both  ", tableManager.GetCell(0, 2));
        }

        #endregion
    }
}

