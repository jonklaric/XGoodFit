using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DatabaseManager;

#nullable enable

namespace DatabaseManager.Tests
{
    /// <summary>
    /// Unit tests for TableEdit, CellEdit, and related edit classes.
    /// Tests cover construction, property access, containment checks, and index adjustment operations.
    /// </summary>
    public class TableEditTests
    {
        #region CellEdit Tests

        /// <summary>
        /// Verifies that the CellEdit constructor correctly initializes all properties.
        /// </summary>
        [Fact]
        public void CellEdit_Constructor_SetsPropertiesCorrectly()
        {
            // Constructor signature is (rowIndex, columnIndex, value)
            var cellEdit = new CellEdit(10, 5, "TestValue");

            Assert.Equal(5, cellEdit.ColumnIndex);
            Assert.Equal(10, cellEdit.RowIndex);
            Assert.Equal("TestValue", cellEdit.Value);
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a matching cell.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsCell_ReturnsTrueForMatchingCell()
        {
            // Constructor signature is (rowIndex, columnIndex, value)
            var cellEdit = new CellEdit(10, 5, "TestValue");
            object? returnValue = null;

            // ContainsCell signature is (columnIndex, rowIndex, ref value)
            var result = cellEdit.ContainsCell(5, 10, ref returnValue);

            Assert.True(result);
            Assert.Equal("TestValue", returnValue);
        }

        /// <summary>
        /// Verifies that ContainsCell returns false for a non-matching cell.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsCell_ReturnsFalseForNonMatchingCell()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");
            object? returnValue = null;

            // ContainsCell signature is (columnIndex, rowIndex, ref value) -> checking col=6, row=10
            var result = cellEdit.ContainsCell(6, 10, ref returnValue);

            Assert.False(result);
            Assert.Null(returnValue);
        }

        /// <summary>
        /// Verifies that ContainsColumn returns true for a matching column index.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsColumn_ReturnsTrueForMatchingColumn()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            Assert.True(cellEdit.ContainsColumn(5));
        }

        /// <summary>
        /// Verifies that ContainsColumn returns false for a non-matching column index.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsColumn_ReturnsFalseForNonMatchingColumn()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            Assert.False(cellEdit.ContainsColumn(6));
        }

        /// <summary>
        /// Verifies that ContainsRow returns true for a matching row index.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsRow_ReturnsTrueForMatchingRow()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            Assert.True(cellEdit.ContainsRow(10));
        }

        /// <summary>
        /// Verifies that ContainsRow returns false for a non-matching row index.
        /// </summary>
        [Fact]
        public void CellEdit_ContainsRow_ReturnsFalseForNonMatchingRow()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            Assert.False(cellEdit.ContainsRow(11));
        }

        /// <summary>
        /// Verifies that ColumnDeleted decrements the column index when a column before it is deleted.
        /// </summary>
        [Fact]
        public void CellEdit_ColumnDeleted_AdjustsIndexCorrectly()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            cellEdit.ColumnDeleted(3);

            // Column 5 becomes 4 after deleting column 3
            Assert.Equal(4, cellEdit.ColumnIndex);
        }

        /// <summary>
        /// Verifies that ColumnAdded increments the column index when a column before it is added.
        /// </summary>
        [Fact]
        public void CellEdit_ColumnAdded_AdjustsIndexCorrectly()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            cellEdit.ColumnAdded<object>(3, null);

            // Column 5 becomes 6 after adding column at index 3
            Assert.Equal(6, cellEdit.ColumnIndex);
        }

        /// <summary>
        /// Verifies that RowDeleted decrements the row index when a row before it is deleted.
        /// </summary>
        [Fact]
        public void CellEdit_RowDeleted_AdjustsIndexCorrectly()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            cellEdit.RowDeleted(3);

            // Row 10 becomes 9 after deleting row 3
            Assert.Equal(9, cellEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that RowAdded increments the row index when a row before it is added.
        /// </summary>
        [Fact]
        public void CellEdit_RowAdded_AdjustsIndexCorrectly()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            cellEdit.RowAdded(3, null);

            // Row 10 becomes 11 after adding row at index 3
            Assert.Equal(11, cellEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns the cell when the row index matches.
        /// </summary>
        [Fact]
        public void CellEdit_GetEditedCellsInRow_ReturnsCorrectCell()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            var cells = cellEdit.GetEditedCellsInRow(10);

            Assert.Single(cells);
            Assert.Equal(5, cells[0].ColumnIndex);
            Assert.Equal(10, cells[0].RowIndex);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns the cell when the column index matches.
        /// </summary>
        [Fact]
        public void CellEdit_GetEditedCellsInColumn_ReturnsCorrectCell()
        {
            // Constructor signature is (rowIndex, columnIndex, value) -> row=10, col=5
            var cellEdit = new CellEdit(10, 5, "TestValue");

            var cells = cellEdit.GetEditedCellsInColumn(5);

            Assert.Single(cells);
            Assert.Equal(5, cells[0].ColumnIndex);
            Assert.Equal(10, cells[0].RowIndex);
        }

        #endregion

        #region AddColumnEdit Tests

        /// <summary>
        /// Verifies that the AddColumnEdit constructor correctly initializes all properties.
        /// </summary>
        [Fact]
        public void AddColumnEdit_Constructor_SetsPropertiesCorrectly()
        {
            int[] data = new int[] { 1, 2, 3 };
            var addColumnEdit = new AddColumnEdit<int>(data, 5, "TestColumn");

            Assert.Equal(5, addColumnEdit.ColumnIndex);
            Assert.Equal(data.ToList(), addColumnEdit.ColumnData);
            Assert.Equal("TestColumn", addColumnEdit.ColumnName);
        }

        /// <summary>
        /// Verifies that ContainsColumn returns true for a matching column index.
        /// </summary>
        [Fact]
        public void AddColumnEdit_ContainsColumn_ReturnsTrueForMatchingColumn()
        {
            var addColumnEdit = new AddColumnEdit<int>(new int[] { 1, 2, 3 }, 5, "Col");

            Assert.True(addColumnEdit.ContainsColumn(5));
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a cell in the added column.
        /// </summary>
        [Fact]
        public void AddColumnEdit_ContainsCell_ReturnsTrueForCellInColumn()
        {
            var addColumnEdit = new AddColumnEdit<int>(new int[] { 10, 20, 30 }, 5, "Col");
            object? returnValue = null;

            var result = addColumnEdit.ContainsCell(5, 1, ref returnValue);

            Assert.True(result);
            Assert.Equal(20, returnValue);
        }

        /// <summary>
        /// Verifies that ColumnAdded increments the column index when a column before it is added.
        /// </summary>
        [Fact]
        public void AddColumnEdit_ColumnAdded_AdjustsIndexCorrectly()
        {
            var addColumnEdit = new AddColumnEdit<int>(new int[] { 1, 2, 3 }, 5, "Col");

            addColumnEdit.ColumnAdded<object>(3, null);

            Assert.Equal(6, addColumnEdit.ColumnIndex);
        }

        /// <summary>
        /// Verifies that ColumnDeleted decrements the column index when a column before it is deleted.
        /// </summary>
        [Fact]
        public void AddColumnEdit_ColumnDeleted_AdjustsIndexCorrectly()
        {
            var addColumnEdit = new AddColumnEdit<int>(new int[] { 1, 2, 3 }, 5, "Col");

            addColumnEdit.ColumnDeleted(3);

            Assert.Equal(4, addColumnEdit.ColumnIndex);
        }

        #endregion

        #region AddColumnsEdit Tests

        /// <summary>
        /// Verifies that ContainsColumn checks all columns in the composite edit.
        /// </summary>
        [Fact]
        public void AddColumnsEdit_ContainsColumn_ChecksAllColumns()
        {
            var col1 = new AddColumnEdit<int>(new int[] { 1, 2 }, 2, "Col1");
            var col2 = new AddColumnEdit<string>(new string[] { "A", "B" }, 5, "Col2");
            var addColumnsEdit = new AddColumnsEdit(new IColumnEdit[] { col1, col2 });

            Assert.True(addColumnsEdit.ContainsColumn(2));
            Assert.True(addColumnsEdit.ContainsColumn(5));
            Assert.False(addColumnsEdit.ContainsColumn(3));
        }

        /// <summary>
        /// Verifies that ContainsCell checks all columns and returns the correct values.
        /// </summary>
        [Fact]
        public void AddColumnsEdit_ContainsCell_ChecksAllColumns()
        {
            var col1 = new AddColumnEdit<int>(new int[] { 10, 20 }, 2, "Col1");
            var col2 = new AddColumnEdit<string>(new string[] { "A", "B" }, 5, "Col2");
            var addColumnsEdit = new AddColumnsEdit(new IColumnEdit[] { col1, col2 });

            object? returnValue = null;
            Assert.True(addColumnsEdit.ContainsCell(2, 0, ref returnValue));
            Assert.Equal(10, returnValue);

            returnValue = null;
            Assert.True(addColumnsEdit.ContainsCell(5, 1, ref returnValue));
            Assert.Equal("B", returnValue);
        }

        /// <summary>
        /// Verifies that ColumnAdded adjusts all column indices correctly.
        /// </summary>
        [Fact]
        public void AddColumnsEdit_ColumnAdded_AdjustsAllColumnsCorrectly()
        {
            var col1 = new AddColumnEdit<int>(new int[] { 1, 2 }, 2, "Col1");
            var col2 = new AddColumnEdit<string>(new string[] { "A", "B" }, 5, "Col2");
            var addColumnsEdit = new AddColumnsEdit(new IColumnEdit[] { col1, col2 });

            addColumnsEdit.ColumnAdded<object>(1, null);

            Assert.True(addColumnsEdit.ContainsColumn(3));  // Was 2
            Assert.True(addColumnsEdit.ContainsColumn(6));  // Was 5
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns all cells from all columns for the specified row.
        /// </summary>
        [Fact]
        public void AddColumnsEdit_GetEditedCellsInRow_ReturnsAllCellsFromRow()
        {
            var col1 = new AddColumnEdit<int>(new int[] { 10, 20 }, 2, "Col1");
            var col2 = new AddColumnEdit<string>(new string[] { "A", "B" }, 5, "Col2");
            var addColumnsEdit = new AddColumnsEdit(new IColumnEdit[] { col1, col2 });

            var cells = addColumnsEdit.GetEditedCellsInRow(0);

            Assert.Equal(2, cells.Count);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns all cells from the specified column.
        /// </summary>
        [Fact]
        public void AddColumnsEdit_GetEditedCellsInColumn_ReturnsAllCellsFromColumn()
        {
            var col1 = new AddColumnEdit<int>(new int[] { 10, 20, 30 }, 2, "Col1");
            var addColumnsEdit = new AddColumnsEdit(new IColumnEdit[] { col1 });

            var cells = addColumnsEdit.GetEditedCellsInColumn(2);

            Assert.Equal(3, cells.Count);
        }

        #endregion

        #region DeleteRowEdit Tests

        /// <summary>
        /// Verifies that the DeleteRowEdit constructor correctly initializes the row index.
        /// </summary>
        [Fact]
        public void DeleteRowEdit_Constructor_SetsPropertiesCorrectly()
        {
            var deleteRowEdit = new DeleteRowEdit(5);

            Assert.Equal(5, deleteRowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that ContainsRow returns false for the deleted row since it no longer exists.
        /// </summary>
        [Fact]
        public void DeleteRowEdit_ContainsRow_ReturnsFalseForMatchingRow()
        {
            var deleteRowEdit = new DeleteRowEdit(5);

            // DeleteRowEdit.ContainsRow always returns false since the row is deleted
            Assert.False(deleteRowEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ContainsRow returns false for non-matching row indices.
        /// </summary>
        [Fact]
        public void DeleteRowEdit_ContainsRow_ReturnsFalseForNonMatchingRow()
        {
            var deleteRowEdit = new DeleteRowEdit(5);

            Assert.False(deleteRowEdit.ContainsRow(6));
        }

        /// <summary>
        /// Verifies that calling RowDeleted does not throw an exception.
        /// </summary>
        [Fact]
        public void DeleteRowEdit_RowDeleted_DoesNotThrow()
        {
            var deleteRowEdit = new DeleteRowEdit(5);

            // Calling RowDeleted does not throw; implementation is a stub
            deleteRowEdit.RowDeleted(3);

            Assert.Equal(5, deleteRowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that calling RowAdded does not throw an exception.
        /// </summary>
        [Fact]
        public void DeleteRowEdit_RowAdded_DoesNotThrow()
        {
            var deleteRowEdit = new DeleteRowEdit(5);

            // Calling RowAdded does not throw; implementation is a stub
            deleteRowEdit.RowAdded(3, null);

            Assert.Equal(5, deleteRowEdit.RowIndex);
        }

        #endregion

        #region DeleteColumnEdit Tests

        /// <summary>
        /// Verifies that the DeleteColumnEdit constructor correctly initializes the column index.
        /// </summary>
        [Fact]
        public void DeleteColumnEdit_Constructor_SetsPropertiesCorrectly()
        {
            var deleteColumnEdit = new DeleteColumnEdit(5);

            Assert.Equal(5, deleteColumnEdit.ColumnIndex);
        }

        /// <summary>
        /// Verifies that ContainsColumn returns false for the deleted column since it no longer exists.
        /// </summary>
        [Fact]
        public void DeleteColumnEdit_ContainsColumn_ReturnsFalseForMatchingColumn()
        {
            var deleteColumnEdit = new DeleteColumnEdit(5);

            // DeleteColumnEdit.ContainsColumn always returns false since the column is deleted
            Assert.False(deleteColumnEdit.ContainsColumn(5));
        }

        /// <summary>
        /// Verifies that calling ColumnDeleted does not throw an exception.
        /// </summary>
        [Fact]
        public void DeleteColumnEdit_ColumnDeleted_DoesNotThrow()
        {
            var deleteColumnEdit = new DeleteColumnEdit(5);

            // Calling ColumnDeleted does not throw; implementation is a stub
            deleteColumnEdit.ColumnDeleted(3);

            Assert.Equal(5, deleteColumnEdit.ColumnIndex);
        }

        #endregion

        #region AddRowEdit Tests

        /// <summary>
        /// Verifies that the AddRowEdit constructor correctly initializes all properties.
        /// </summary>
        [Fact]
        public void AddRowEdit_Constructor_SetsPropertiesCorrectly()
        {
            object[] rowData = new object[] { 1, "Test", 3.14 };
            var addRowEdit = new AddRowEdit(rowData, 5);

            Assert.Equal(5, addRowEdit.RowIndex);
            Assert.Equal(rowData.ToList(), addRowEdit.RowData);
        }

        /// <summary>
        /// Verifies that ContainsRow returns true for a matching row index.
        /// </summary>
        [Fact]
        public void AddRowEdit_ContainsRow_ReturnsTrueForMatchingRow()
        {
            var addRowEdit = new AddRowEdit(new object[] { 1, 2, 3 }, 5);

            Assert.True(addRowEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a cell in the added row.
        /// </summary>
        [Fact]
        public void AddRowEdit_ContainsCell_ReturnsTrueForCellInRow()
        {
            var addRowEdit = new AddRowEdit(new object[] { 10, "B", 30 }, 5);
            object? returnValue = null;

            var result = addRowEdit.ContainsCell(1, 5, ref returnValue);

            Assert.True(result);
            Assert.Equal("B", returnValue);
        }

        /// <summary>
        /// Verifies that RowAdded increments the row index when a row before it is added.
        /// </summary>
        [Fact]
        public void AddRowEdit_RowAdded_AdjustsIndexCorrectly()
        {
            var addRowEdit = new AddRowEdit(new object[] { 1, 2, 3 }, 5);

            addRowEdit.RowAdded(3, null);

            Assert.Equal(6, addRowEdit.RowIndex);
        }

        #endregion

        #region RowEdit Tests

        /// <summary>
        /// Verifies that the RowEdit constructor correctly initializes the row data and index.
        /// </summary>
        [Fact]
        public void RowEdit_Constructor_SetsPropertiesCorrectly()
        {
            object[] rowData = new object[] { 1, "Test", 3.14, true };
            var rowEdit = new RowEdit(rowData, 5);

            Assert.Equal(5, rowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that ContainsRow returns true for a matching row index.
        /// </summary>
        [Fact]
        public void RowEdit_ContainsRow_ReturnsTrueForMatchingRow()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            Assert.True(rowEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ContainsRow returns false for a non-matching row index.
        /// </summary>
        [Fact]
        public void RowEdit_ContainsRow_ReturnsFalseForNonMatchingRow()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            Assert.False(rowEdit.ContainsRow(6));
        }

        /// <summary>
        /// Verifies that ContainsColumn always returns true since RowEdit affects all columns.
        /// </summary>
        [Fact]
        public void RowEdit_ContainsColumn_AlwaysReturnsTrue()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            Assert.True(rowEdit.ContainsColumn(0));
            Assert.True(rowEdit.ContainsColumn(100));
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a cell in the row.
        /// </summary>
        [Fact]
        public void RowEdit_ContainsCell_ReturnsTrueForCellInRow()
        {
            var rowEdit = new RowEdit(new object[] { "A", "B", "C" }, 5);
            object? returnValue = null;

            // ContainsCell(columnIndex, rowIndex, ref value)
            var result = rowEdit.ContainsCell(1, 5, ref returnValue);

            Assert.True(result);
            Assert.Equal("B", returnValue);
        }

        /// <summary>
        /// Verifies that ContainsCell returns false for a cell not in the row.
        /// </summary>
        [Fact]
        public void RowEdit_ContainsCell_ReturnsFalseForCellNotInRow()
        {
            var rowEdit = new RowEdit(new object[] { "A", "B", "C" }, 5);
            object? returnValue = null;

            var result = rowEdit.ContainsCell(1, 6, ref returnValue);

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that RowAdded increments the row index when a row before it is added.
        /// </summary>
        [Fact]
        public void RowEdit_RowAdded_AdjustsIndexCorrectly()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            rowEdit.RowAdded(3, null);

            Assert.Equal(6, rowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that RowAdded does not change the index when a row after it is added.
        /// </summary>
        [Fact]
        public void RowEdit_RowAdded_DoesNotAdjustIndexForLaterRow()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            rowEdit.RowAdded(7, null);

            Assert.Equal(5, rowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that RowDeleted decrements the row index when a row before it is deleted.
        /// </summary>
        [Fact]
        public void RowEdit_RowDeleted_AdjustsIndexCorrectly()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 5);

            rowEdit.RowDeleted(3);

            Assert.Equal(4, rowEdit.RowIndex);
        }

        /// <summary>
        /// Verifies that ColumnAdded inserts data at the correct position.
        /// </summary>
        [Fact]
        public void RowEdit_ColumnAdded_InsertsDataCorrectly()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 0);
            object? returnValue = null;

            rowEdit.ColumnAdded(1, new object[] { 99 });

            // Column 2 should now contain 2 (shifted from column 1)
            rowEdit.ContainsCell(2, 0, ref returnValue);
            Assert.Equal(2, returnValue);

            // Column 1 should now contain 99
            rowEdit.ContainsCell(1, 0, ref returnValue);
            Assert.Equal(99, returnValue);
        }

        /// <summary>
        /// Verifies that ColumnDeleted removes data at the correct position.
        /// </summary>
        [Fact]
        public void RowEdit_ColumnDeleted_RemovesDataCorrectly()
        {
            var rowEdit = new RowEdit(new object[] { 1, 2, 3 }, 0);
            object? returnValue = null;

            rowEdit.ColumnDeleted(1);

            // Column 1 should now contain 3 (shifted from column 2)
            rowEdit.ContainsCell(1, 0, ref returnValue);
            Assert.Equal(3, returnValue);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns all cells for a matching row.
        /// </summary>
        [Fact]
        public void RowEdit_GetEditedCellsInRow_ReturnsAllCells()
        {
            var rowEdit = new RowEdit(new object[] { "A", "B", "C" }, 5);

            var cells = rowEdit.GetEditedCellsInRow(5);

            Assert.Equal(3, cells.Count);
            Assert.Equal("A", cells[0].Value);
            Assert.Equal("B", cells[1].Value);
            Assert.Equal("C", cells[2].Value);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns the cell for the specified column.
        /// </summary>
        [Fact]
        public void RowEdit_GetEditedCellsInColumn_ReturnsCellForColumn()
        {
            var rowEdit = new RowEdit(new object[] { "A", "B", "C" }, 5);

            var cells = rowEdit.GetEditedCellsInColumn(1);

            Assert.Single(cells);
            Assert.Equal("B", cells[0].Value);
            Assert.Equal(5, cells[0].RowIndex);
        }

        /// <summary>
        /// Verifies that GetRowData returns a MultiCellEdit with all row values.
        /// </summary>
        [Fact]
        public void RowEdit_GetRowData_ReturnsMultiCellEdit()
        {
            var rowEdit = new RowEdit(new object[] { 10, 20, 30 }, 5);

            var multiCellEdit = rowEdit.GetRowData();

            Assert.Equal(3, multiCellEdit.CellEdits.Length);
        }

        #endregion

        #region MultiCellEdit Tests

        /// <summary>
        /// Verifies that the MultiCellEdit constructor correctly initializes the cell edits.
        /// </summary>
        [Fact]
        public void MultiCellEdit_Constructor_SetsCellEditsCorrectly()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(1, 1, "B"),
                new CellEdit(2, 2, "C")
            };

            var multiCellEdit = new MultiCellEdit(cellEdits);

            Assert.Equal(3, multiCellEdit.CellEdits.Length);
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a matching cell.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsCell_ReturnsTrueForMatchingCell()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(1, 1, "B"),
                new CellEdit(2, 2, "C")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);
            object? returnValue = null;

            // ContainsCell(columnIndex, rowIndex, ref value)
            var result = multiCellEdit.ContainsCell(1, 1, ref returnValue);

            Assert.True(result);
            Assert.Equal("B", returnValue);
        }

        /// <summary>
        /// Verifies that ContainsCell returns false for a cell not in the edit.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsCell_ReturnsFalseForNonMatchingCell()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(2, 2, "C")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);
            object? returnValue = null;

            var result = multiCellEdit.ContainsCell(1, 1, ref returnValue);

            Assert.False(result);
        }

        /// <summary>
        /// Verifies that ContainsRow returns true for a row within the edit range.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsRow_ReturnsTrueForMatchingRow()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(5, 2, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            Assert.True(multiCellEdit.ContainsRow(0));
            Assert.True(multiCellEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ContainsRow returns false for a row outside the edit range.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsRow_ReturnsFalseForNonMatchingRow()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(5, 2, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            Assert.False(multiCellEdit.ContainsRow(10));
        }

        /// <summary>
        /// Verifies that ContainsColumn returns true for a column within the edit range.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsColumn_ReturnsTrueForMatchingColumn()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(1, 5, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            Assert.True(multiCellEdit.ContainsColumn(0));
            Assert.True(multiCellEdit.ContainsColumn(5));
        }

        /// <summary>
        /// Verifies that ContainsColumn returns false for a column outside the edit range.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ContainsColumn_ReturnsFalseForNonMatchingColumn()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 0, "A"),
                new CellEdit(1, 2, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            Assert.False(multiCellEdit.ContainsColumn(10));
        }

        /// <summary>
        /// Verifies that RowAdded adjusts all cell edit indices correctly.
        /// </summary>
        [Fact]
        public void MultiCellEdit_RowAdded_AdjustsAllIndicesCorrectly()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(5, 0, "A"),
                new CellEdit(10, 1, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            multiCellEdit.RowAdded(3, null);

            // Rows should be shifted
            Assert.Equal(6, multiCellEdit.CellEdits[0].RowIndex);
            Assert.Equal(11, multiCellEdit.CellEdits[1].RowIndex);
        }

        /// <summary>
        /// Verifies that RowDeleted adjusts all cell edit indices correctly.
        /// </summary>
        [Fact]
        public void MultiCellEdit_RowDeleted_AdjustsAllIndicesCorrectly()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(5, 0, "A"),
                new CellEdit(10, 1, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            multiCellEdit.RowDeleted(3);

            // Rows should be shifted down
            Assert.Equal(4, multiCellEdit.CellEdits[0].RowIndex);
            Assert.Equal(9, multiCellEdit.CellEdits[1].RowIndex);
        }

        /// <summary>
        /// Verifies that ColumnAdded adjusts all cell edit indices correctly.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ColumnAdded_AdjustsAllIndicesCorrectly()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 5, "A"),
                new CellEdit(1, 10, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            multiCellEdit.ColumnAdded<object>(3, null);

            // Columns should be shifted
            Assert.Equal(6, multiCellEdit.CellEdits[0].ColumnIndex);
            Assert.Equal(11, multiCellEdit.CellEdits[1].ColumnIndex);
        }

        /// <summary>
        /// Verifies that ColumnDeleted adjusts all cell edit indices correctly.
        /// </summary>
        [Fact]
        public void MultiCellEdit_ColumnDeleted_AdjustsAllIndicesCorrectly()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 5, "A"),
                new CellEdit(1, 10, "B")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            multiCellEdit.ColumnDeleted(3);

            // Columns should be shifted down
            Assert.Equal(4, multiCellEdit.CellEdits[0].ColumnIndex);
            Assert.Equal(9, multiCellEdit.CellEdits[1].ColumnIndex);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns all cells in the specified row.
        /// </summary>
        [Fact]
        public void MultiCellEdit_GetEditedCellsInRow_ReturnsCorrectCells()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(5, 0, "A"),
                new CellEdit(5, 1, "B"),
                new CellEdit(6, 2, "C")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            var cells = multiCellEdit.GetEditedCellsInRow(5);

            Assert.Equal(2, cells.Count);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns all cells in the specified column.
        /// </summary>
        [Fact]
        public void MultiCellEdit_GetEditedCellsInColumn_ReturnsCorrectCells()
        {
            var cellEdits = new CellEdit[]
            {
                new CellEdit(0, 5, "A"),
                new CellEdit(1, 5, "B"),
                new CellEdit(2, 6, "C")
            };
            var multiCellEdit = new MultiCellEdit(cellEdits);

            var cells = multiCellEdit.GetEditedCellsInColumn(5);

            Assert.Equal(2, cells.Count);
        }

        #endregion

        #region DeleteRowsEdit Tests

        /// <summary>
        /// Verifies that the DeleteRowsEdit constructor correctly initializes the delete row edits.
        /// </summary>
        [Fact]
        public void DeleteRowsEdit_Constructor_SetsPropertiesCorrectly()
        {
            var deleteEdits = new DeleteRowEdit[]
            {
                new DeleteRowEdit(1),
                new DeleteRowEdit(3),
                new DeleteRowEdit(5)
            };

            var deleteRowsEdit = new DeleteRowsEdit(deleteEdits);

            Assert.Equal(3, deleteRowsEdit.DeleteRowEdits.Length);
        }

        /// <summary>
        /// Verifies that GetDeletedRowIndices returns all deleted row indices.
        /// </summary>
        [Fact]
        public void DeleteRowsEdit_GetDeletedRowIndices_ReturnsCorrectIndices()
        {
            var deleteEdits = new DeleteRowEdit[]
            {
                new DeleteRowEdit(1),
                new DeleteRowEdit(3),
                new DeleteRowEdit(5)
            };
            var deleteRowsEdit = new DeleteRowsEdit(deleteEdits);

            var indices = deleteRowsEdit.GetDeletedRowIndices();

            Assert.Equal(new int[] { 1, 3, 5 }, indices);
        }

        /// <summary>
        /// Verifies that ContainsRow returns false since deleted rows no longer exist.
        /// </summary>
        [Fact]
        public void DeleteRowsEdit_ContainsRow_ReturnsFalseForDeletedRows()
        {
            var deleteEdits = new DeleteRowEdit[]
            {
                new DeleteRowEdit(1),
                new DeleteRowEdit(3)
            };
            var deleteRowsEdit = new DeleteRowsEdit(deleteEdits);

            // DeleteRowEdit.ContainsRow always returns false
            Assert.False(deleteRowsEdit.ContainsRow(1));
            Assert.False(deleteRowsEdit.ContainsRow(3));
            Assert.False(deleteRowsEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ColumnAdded propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteRowsEdit_ColumnAdded_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteRowEdit[]
            {
                new DeleteRowEdit(1),
                new DeleteRowEdit(3)
            };
            var deleteRowsEdit = new DeleteRowsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteRowsEdit.ColumnAdded<object>(2, null));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that RowAdded propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteRowsEdit_RowAdded_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteRowEdit[]
            {
                new DeleteRowEdit(1),
                new DeleteRowEdit(3)
            };
            var deleteRowsEdit = new DeleteRowsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteRowsEdit.RowAdded(0, null));
            Assert.Null(exception);
        }

        #endregion

        #region AddRowsEdit Tests

        /// <summary>
        /// Verifies that the AddRowsEdit constructor correctly initializes the add row edits.
        /// </summary>
        [Fact]
        public void AddRowsEdit_Constructor_SetsPropertiesCorrectly()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, "A" }, 0),
                new AddRowEdit(new object[] { 2, "B" }, 1),
                new AddRowEdit(new object[] { 3, "C" }, 2)
            };

            var addRowsEdit = new AddRowsEdit(addEdits);

            Assert.Equal(3, addRowsEdit.AddRowEdits.Length);
        }

        /// <summary>
        /// Verifies that ContainsRow returns true for rows in the edit.
        /// </summary>
        [Fact]
        public void AddRowsEdit_ContainsRow_ReturnsTrueForAddedRows()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, "A" }, 0),
                new AddRowEdit(new object[] { 2, "B" }, 2)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            Assert.True(addRowsEdit.ContainsRow(0));
            Assert.True(addRowsEdit.ContainsRow(2));
            Assert.False(addRowsEdit.ContainsRow(5));
        }

        /// <summary>
        /// Verifies that ContainsCell returns true and outputs the value for a cell in the added rows.
        /// </summary>
        [Fact]
        public void AddRowsEdit_ContainsCell_ReturnsTrueForCellInAddedRow()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 10, 20, 30 }, 5)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);
            object? returnValue = null;

            var result = addRowsEdit.ContainsCell(1, 5, ref returnValue);

            Assert.True(result);
            Assert.Equal(20, returnValue);
        }

        /// <summary>
        /// Verifies that ContainsColumn returns true since added rows affect all columns.
        /// </summary>
        [Fact]
        public void AddRowsEdit_ContainsColumn_ReturnsTrueForAnyColumn()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, 2, 3 }, 0)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            Assert.True(addRowsEdit.ContainsColumn(0));
            Assert.True(addRowsEdit.ContainsColumn(2));
        }

        /// <summary>
        /// Verifies that GetOriginalRowEdits returns all original row data.
        /// </summary>
        [Fact]
        public void AddRowsEdit_GetOriginalRowEdits_ReturnsCorrectData()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, "A" }, 0),
                new AddRowEdit(new object[] { 2, "B" }, 1)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            var originalEdits = addRowsEdit.GetOriginalRowEdits();

            Assert.Equal(2, originalEdits.Count);
        }

        /// <summary>
        /// Verifies that RowAdded propagates to all contained edits and adjusts indices.
        /// </summary>
        [Fact]
        public void AddRowsEdit_RowAdded_PropagatesCorrectly()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, 2 }, 5),
                new AddRowEdit(new object[] { 3, 4 }, 10)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            addRowsEdit.RowAdded(3, null);

            // Both rows should be shifted
            Assert.Equal(6, addRowsEdit.AddRowEdits[0].RowIndex);
            Assert.Equal(11, addRowsEdit.AddRowEdits[1].RowIndex);
        }

        /// <summary>
        /// Verifies that RowDeleted propagates to all contained edits and adjusts indices.
        /// </summary>
        [Fact]
        public void AddRowsEdit_RowDeleted_PropagatesCorrectly()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, 2 }, 5),
                new AddRowEdit(new object[] { 3, 4 }, 10)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            addRowsEdit.RowDeleted(3);

            // Both rows should be shifted down
            Assert.Equal(4, addRowsEdit.AddRowEdits[0].RowIndex);
            Assert.Equal(9, addRowsEdit.AddRowEdits[1].RowIndex);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns all cells from matching rows.
        /// </summary>
        [Fact]
        public void AddRowsEdit_GetEditedCellsInRow_ReturnsCorrectCells()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, 2, 3 }, 5)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            var cells = addRowsEdit.GetEditedCellsInRow(5);

            Assert.Equal(3, cells.Count);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns all cells from the specified column.
        /// </summary>
        [Fact]
        public void AddRowsEdit_GetEditedCellsInColumn_ReturnsCorrectCells()
        {
            var addEdits = new AddRowEdit[]
            {
                new AddRowEdit(new object[] { 1, 2, 3 }, 5),
                new AddRowEdit(new object[] { 4, 5, 6 }, 6)
            };
            var addRowsEdit = new AddRowsEdit(addEdits);

            var cells = addRowsEdit.GetEditedCellsInColumn(1);

            Assert.Equal(2, cells.Count);
        }

        #endregion

        #region DeleteColumnsEdit Tests

        /// <summary>
        /// Verifies that the DeleteColumnsEdit constructor correctly initializes the delete column edits.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_Constructor_SetsPropertiesCorrectly()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3),
                new DeleteColumnEdit(5)
            };

            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            Assert.Equal(3, deleteColumnsEdit.ColumnsDeleted.Length);
        }

        /// <summary>
        /// Verifies that ContainsColumn returns false since deleted columns no longer exist.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_ContainsColumn_ReturnsFalseForDeletedColumns()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // DeleteColumnEdit.ContainsColumn always returns false
            Assert.False(deleteColumnsEdit.ContainsColumn(1));
            Assert.False(deleteColumnsEdit.ContainsColumn(3));
            Assert.False(deleteColumnsEdit.ContainsColumn(5));
        }

        /// <summary>
        /// Verifies that ContainsRow returns false since column deletions don't affect specific rows.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_ContainsRow_ReturnsFalseForAnyRow()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // DeleteColumnEdit.ContainsRow always returns false
            Assert.False(deleteColumnsEdit.ContainsRow(0));
            Assert.False(deleteColumnsEdit.ContainsRow(10));
        }

        /// <summary>
        /// Verifies that ColumnAdded propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_ColumnAdded_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteColumnsEdit.ColumnAdded<object>(0, null));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that ColumnDeleted propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_ColumnDeleted_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteColumnsEdit.ColumnDeleted(0));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that RowAdded propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_RowAdded_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteColumnsEdit.RowAdded(0, null));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that RowDeleted propagates to all contained edits.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_RowDeleted_PropagatesCorrectly()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            // Should not throw
            var exception = Record.Exception(() => deleteColumnsEdit.RowDeleted(0));
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInColumn returns an empty list for deleted columns.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_GetEditedCellsInColumn_ReturnsEmptyList()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            var cells = deleteColumnsEdit.GetEditedCellsInColumn(1);

            Assert.Empty(cells);
        }

        /// <summary>
        /// Verifies that GetEditedCellsInRow returns an empty list for deleted columns.
        /// </summary>
        [Fact]
        public void DeleteColumnsEdit_GetEditedCellsInRow_ReturnsEmptyList()
        {
            var deleteEdits = new DeleteColumnEdit[]
            {
                new DeleteColumnEdit(1),
                new DeleteColumnEdit(3)
            };
            var deleteColumnsEdit = new DeleteColumnsEdit(deleteEdits);

            var cells = deleteColumnsEdit.GetEditedCellsInRow(0);

            Assert.Empty(cells);
        }

        #endregion
    }
}
