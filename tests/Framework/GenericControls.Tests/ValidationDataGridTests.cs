using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using WpfCompat;
using static GenericControls.Tests.GridTesting;

namespace GenericControls.Tests;

/// <summary>
/// ValidationDataGrid's rules, as upstream implements them: the cells that break a row's rules are marked
/// with the grid's error brushes and the rules' messages, and the table is validated again after the
/// operations that suspend validation.
/// </summary>
[TestClass]
public sealed class ValidationDataGridTests
{
    [TestMethod]
    public void Rules_MarkTheCellsThatBreakThem_WithTheirMessagesAsTheTip()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, _) = ShowRuleGrid(("A", 1), ("B", -2), ("A", 3));

            CollectionAssert.AreEqual(new[] { "Name", "Value (cfs)" }, grid.Columns.Select(c => (string)c.Header).ToArray(),
                "the rows' display names, without RuleMap or the write-only ParentList");
            Assert.IsTrue(grid.Columns.All(c => c.Width.IsStar), "upstream gives the text columns star widths");

            DataGridCell negative = Cell(grid, 1, 1);
            Assert.IsTrue(negative.GetValue(ValidationDataGrid.CellHasErrorProperty));
            Assert.AreEqual(grid.ErrorCellBackgroundBrush, negative.Background);
            Assert.AreEqual(grid.ErrorCellBorderBrush, negative.BorderBrush);
            Assert.AreEqual(new Thickness(1), negative.BorderThickness);
            Assert.AreEqual("Values must be positive." + Environment.NewLine + "Values must increase.", ToolTip.GetTip(negative));

            // The table's validation ends with the first row's, which runs the unique rule for every row.
            DataGridCell duplicate = Cell(grid, 2, 0);
            Assert.AreEqual(grid.ErrorCellBackgroundBrush, duplicate.Background);
            Assert.AreEqual("Names must be unique.", ToolTip.GetTip(duplicate));

            DataGridCell valid = Cell(grid, 2, 1);
            Assert.IsFalse(valid.GetValue(ValidationDataGrid.CellHasErrorProperty));
            Assert.AreNotEqual(grid.ErrorCellBackgroundBrush, valid.Background);
            Assert.IsNull(ToolTip.GetTip(valid));
            window.Close();
        });
    }

    [TestMethod]
    public void Editing_FiltersWhatIsTyped_AndValidatesTheValue()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, rows) = ShowRuleGrid(("A", 1), ("B", -2), ("C", 3));
            grid.CurrentCell = new DataGridCellInfo(rows[1], grid.Columns[1], 1, 1);
            Settle();

            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            Settle();
            var editor = (TextBox)Cell(grid, 1, 1).Content!;
            editor.SelectAll();
            window.KeyTextInput("x");
            Assert.AreEqual("-2", editor.Text, "a number column's editor takes only numbers");
            window.KeyTextInput("2");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle();

            Assert.AreEqual(2.0, ((RuleRow)rows[1]).Value);
            DataGridCell cell = Cell(grid, 1, 1);
            Assert.IsFalse(cell.GetValue(ValidationDataGrid.CellHasErrorProperty), "1 < 2 < 3, and positive");
            Assert.IsNull(ToolTip.GetTip(cell));
            window.Close();
        });
    }

    [TestMethod]
    public void PastingAndDeleting_SuspendValidation_ThenValidateTheTable()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, rows) = ShowRuleGrid(("A", 1), ("B", 5), ("C", 3));
            var last = (RuleRow)rows[2];
            Assert.IsTrue(last.RuleMap["Value"].HasError, "3 follows 5");

            bool suppressedWhileDeleting = false;
            grid.PreviewDeleteRows += (List<int> indices, ref bool cancel) => suppressedWhileDeleting = grid.SuppressValidation;
            Select(grid, (1, 0));
            grid.DeleteRows();
            Assert.IsTrue(suppressedWhileDeleting);
            Assert.IsFalse(grid.SuppressValidation);
            Assert.IsFalse(last.RuleMap["Value"].HasError, "validated again: 3 now follows 1");

            bool suppressedWhilePasting = false;
            grid.PreviewPasteData += (string[][] data, ref bool cancel) => suppressedWhilePasting = grid.SuppressValidation;
            Select(grid, (0, 1));
            Clipboard.SetText("-5");
            grid.PasteClipboard();
            Assert.IsTrue(suppressedWhilePasting);
            Assert.IsFalse(grid.SuppressValidation);
            Assert.AreEqual(-5.0, ((RuleRow)rows[0]).Value);
            Assert.IsTrue(((RuleRow)rows[0]).RuleMap["Value"].HasError, "validated after the paste");
            window.Close();
        });
    }

    [TestMethod]
    public void AddedRows_AreGivenTheirList_AndValidated()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, rows) = ShowRuleGrid(("A", 1), ("B", 2));

            grid.AddRows(1);
            Assert.AreEqual(3, rows.Count);
            var added = (RuleRow)rows[2];
            Assert.IsTrue(added.RuleMap["Value"].HasError, "a new row's 0 isn't positive");

            added.Name = "A";
            Assert.AreEqual("Names must be unique.", added.RuleMap["Name"].ErrorMessage, "the unique rule sees the row's list");
            window.Close();
        });
    }

    [TestMethod]
    public void SuspendValidation_DefersIt_UntilTheOutermostScopeEnds()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, rows) = ShowRuleGrid(("A", 1), ("B", 2));
            var first = (RuleRow)rows[0];

            using (grid.SuspendValidation())
            {
                using (grid.SuspendValidation())
                    first.Value = -1;
                Assert.IsTrue(grid.SuppressValidation);
                Assert.IsFalse(first.RuleMap["Value"].HasError, "not validated while suspended");
            }
            Assert.IsFalse(grid.SuppressValidation);
            Assert.IsTrue(first.RuleMap["Value"].HasError, "validated as the last scope ends");

            using (grid.SuspendValidation(validateOnResume: false))
                first.Value = 1;
            Assert.IsTrue(first.RuleMap["Value"].HasError, "a scope can end without validating");
            grid.ValidateTable();
            Assert.IsFalse(first.RuleMap["Value"].HasError);
            window.Close();
        });
    }

    // A validation grid of rule rows with these names and values, shown and focused.
    private static (Window Window, ValidationDataGrid Grid, ObservableCollection<object> Rows) ShowRuleGrid(params (string Name, double Value)[] values)
    {
        var rows = new ObservableCollection<object>();
        var grid = new ValidationDataGrid { RowType = typeof(RuleRow) };
        foreach (var (name, value) in values)
            rows.Add(new RuleRow(rows, grid) { Name = name, Value = value });
        grid.ItemsSource = rows;
        var window = Show(grid);
        grid.Focus();
        Settle();
        return (window, grid, rows);
    }
}
