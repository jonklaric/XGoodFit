using System;
using System.Collections.Generic;
using System.Data;
using Xunit;
using DatabaseManager;

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Regression tests for the DatabaseManager library covering invalid input handling,
    /// undo/redo edge cases, and structural edit operations.
    /// </summary>
    public class RegressionTests
    {
        #region Invalid Input Tests

        /// <summary>
        /// DM-016: EditCell with an invalid column name throws ArgumentException.
        /// </summary>
        [Fact]
        public void EditCell_WithInvalidColumnName_ThrowsArgumentException()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.Throws<ArgumentException>(() => tableManager.EditCell(0, "NonExistentColumn", "value"));
        }

        /// <summary>
        /// DM-017: GetColumn with an invalid column name throws ArgumentException.
        /// </summary>
        [Fact]
        public void GetColumn_WithInvalidColumnName_ThrowsArgumentException()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Force at least one edit so _editIndex >= 0 and the column name lookup path is taken
            tableManager.EditCell(0, 0, 99);

            Assert.Throws<ArgumentException>(() => tableManager.GetColumn("NonExistentColumn"));
        }

        /// <summary>
        /// DM-018: GetCell with an invalid column name throws ArgumentException.
        /// </summary>
        [Fact]
        public void GetCell_WithInvalidColumnName_ThrowsArgumentException()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Force at least one edit so _editIndex >= 0 and the column name lookup path is taken
            tableManager.EditCell(0, 0, 99);

            Assert.Throws<ArgumentException>(() => tableManager.GetCell("NonExistentColumn", 0));
        }

        /// <summary>
        /// DM-020: AddColumns with mismatched column name and data array lengths throws Exception.
        /// </summary>
        [Fact]
        public void AddColumns_WithMismatchedArrayLengths_ThrowsException()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(1);
            dataTable.Rows.Add(2);
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            var names = new string[] { "NewCol1", "NewCol2" };
            var data = new List<object[]>
            {
                new object[] { 10, 20 },
                new object[] { 30, 40 },
                new object[] { 50, 60 }
            };
            var types = new Type[] { typeof(int), typeof(int) };

            Assert.Throws<Exception>(() => tableManager.AddColumns(names, data, types));
        }

        /// <summary>
        /// DM-036: MultiCellEdit constructed with an empty CellEdit array throws ArgumentException.
        /// </summary>
        [Fact]
        public void MultiCellEdit_WithEmptyArray_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new MultiCellEdit(new CellEdit[0]));
        }

        #endregion

        #region Undo/Redo Edge Case Tests

        /// <summary>
        /// DM-019: RedoEdit when CanRedo is false does nothing and does not crash.
        /// </summary>
        [Fact]
        public void RedoEdit_WhenCanRedoIsFalse_DoesNothing()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.False(tableManager.CanRedo());

            // Should not throw
            tableManager.RedoEdit();

            // State should be unchanged
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal("A", tableManager.GetCell(1, 0));
            Assert.Equal(2, tableManager.NumberOfRows);
        }

        /// <summary>
        /// DM-019 related: UndoEdit when CanUndo is false does nothing and does not crash.
        /// </summary>
        [Fact]
        public void UndoEdit_WhenCanUndoIsFalse_DoesNothing()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            Assert.False(tableManager.CanUndo());

            // Should not throw
            tableManager.UndoEdit();

            // State should be unchanged
            Assert.Equal(1, tableManager.GetCell(0, 0));
            Assert.Equal("A", tableManager.GetCell(1, 0));
            Assert.Equal(2, tableManager.NumberOfRows);
        }

        /// <summary>
        /// Verifies that EditCell followed by UndoEdit restores the original cell value.
        /// </summary>
        [Fact]
        public void EditCell_ThenUndo_RestoresOriginalValue()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Edit cell
            tableManager.EditCell(0, 0, 999);
            Assert.Equal(999, tableManager.GetCell(0, 0));

            // Undo
            tableManager.UndoEdit();
            Assert.Equal(1, tableManager.GetCell(0, 0));
        }

        /// <summary>
        /// Verifies that making 3 edits, undoing 2, and redoing 1 produces the correct intermediate state.
        /// </summary>
        [Fact]
        public void MultipleEdits_UndoRedoCycle_MaintainsCorrectState()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Rows.Add(0);
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            // Make 3 edits (don't call ApplyEdits - it clears the edit history)
            tableManager.EditCell(0, 0, 10);
            tableManager.EditCell(0, 0, 20);
            tableManager.EditCell(0, 0, 30);
            Assert.Equal(30, tableManager.GetCell(0, 0));

            // Undo 2 edits: 30 -> 20 -> 10
            tableManager.UndoEdit();
            Assert.Equal(20, tableManager.GetCell(0, 0));
            tableManager.UndoEdit();
            Assert.Equal(10, tableManager.GetCell(0, 0));

            // Redo 1 edit: 10 -> 20
            tableManager.RedoEdit();
            Assert.Equal(20, tableManager.GetCell(0, 0));

            // Verify CanUndo and CanRedo
            Assert.True(tableManager.CanUndo());
            Assert.True(tableManager.CanRedo());
        }

        #endregion

        #region Structural Edit Tests

        /// <summary>
        /// Verifies that AddColumn increases the column count by one.
        /// </summary>
        [Fact]
        public void AddColumn_IncreasesColumnCount()
        {
            var dataTable = new DataTable("TestTable");
            dataTable.Columns.Add("Col1", typeof(int));
            dataTable.Columns.Add("Col2", typeof(string));
            dataTable.Rows.Add(1, "A");
            dataTable.Rows.Add(2, "B");
            var reader = new InMemoryReader(dataTable);
            var tableManager = reader.GetTableManager("TestTable");

            int originalColumnCount = tableManager.ColumnNames.Length;

            tableManager.AddColumn("NewCol", new int[] { 100, 200 });
            tableManager.ApplyEdits();

            Assert.Equal(originalColumnCount + 1, tableManager.ColumnNames.Length);
            Assert.Equal("NewCol", tableManager.ColumnNames[originalColumnCount]);
            Assert.Equal(100, tableManager.GetCell(originalColumnCount, 0));
            Assert.Equal(200, tableManager.GetCell(originalColumnCount, 1));
        }

        #endregion
    }
}
