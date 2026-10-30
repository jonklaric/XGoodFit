using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using GenericControls;
using Numerics.Data;
using WpfCompat;
using static NumericControls.Tests.TestSupport;

namespace NumericControls.Tests;

/// <summary>
/// ProbabilityOrdinatesControl, as upstream implements it: one validated column of probabilities that
/// edits the bound <see cref="ProbabilityOrdinates"/>, and follows its changes.
/// </summary>
[TestClass]
public sealed class ProbabilityOrdinatesControlTests
{
    [TestMethod]
    public void Ordinates_ShowInOneColumn_WithUpstreamsHeaderAndCellStyles()
    {
        HeadlessSession.Run(() =>
        {
            var (window, control, grid, _) = ShowOrdinates(0.5, 0.1, 0.01);

            DataGridColumn column = grid.Columns.Single();
            Assert.AreEqual("Probability Ordinates", column.Header, "the row item's display name");
            Assert.IsTrue(column.Width.IsStar);
            Assert.AreEqual(20, column.MinWidth);
            // As upstream: the grid replaces the column's Right_CellStyle with a style based on its own cell style
            // that takes Right_CellStyle's setters (not its triggers), and adds its error trigger.
            Assert.AreSame(grid.CellTheme, column.CellTheme!.BasedOn);
            Assert.IsTrue(column.CellTheme.Setters.OfType<Setter>().Any(s => s.Property == TextBlock.TextAlignmentProperty && Equals(s.Value, Avalonia.Media.TextAlignment.Right)));
            Assert.AreSame(control.FindResource("Center_ColumnHeaderStyle"), column.HeaderTheme!.BasedOn);
            Assert.IsInstanceOfType<FuncTemplate<TextBlock>>(column.HeaderTheme.Setters.OfType<Setter>().Single(s => s.Property == ToolTip.TipProperty).Value,
                "the tool tip's text block is built for the header");
            var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => h.OwningColumn == column);
            Assert.AreEqual("Enter the desired probabilities.", ((TextBlock)ToolTip.GetTip(header)!).Text);

            CollectionAssert.AreEqual(new[] { "0.5", "0.1", "0.01" }, Enumerable.Range(0, 3).Select(r => CellText(grid, r, 0)).ToArray());
            Assert.AreEqual(Avalonia.Media.TextAlignment.Right, Cell(grid, 0, 0).GetValue(TextBlock.TextAlignmentProperty));
            window.Close();
        });
    }

    [TestMethod]
    public void Numbers_ShowInEnglish_WhateverTheCulture()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("de-DE");
            var (window, _, grid, _) = ShowOrdinates(0.5, 0.25);
            Assert.AreEqual("0.5", CellText(grid, 0, 0), "a generated column converts with WPF's binding culture, en-US");
            Assert.AreEqual("0.25", CellText(grid, 1, 0));
            window.Close();
        });
    }

    [TestMethod]
    public void Validation_FlagsProbabilitiesOutOfOrderOrRange()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, grid, _) = ShowOrdinates(0.1, 0.05, 1.5);

            DataGridCell outOfOrder = Cell(grid, 1, 0);
            Assert.AreEqual(grid.ErrorCellBackgroundBrush, outOfOrder.Background);
            Assert.AreEqual("The probability values must be in ascending order.", ToolTip.GetTip(outOfOrder));
            // As upstream's PropertyRule: a failed rule after the first is joined with a new line, even when the
            // first passed, so this message starts with one.
            DataGridCell outOfRange = Cell(grid, 2, 0);
            Assert.AreEqual(grid.ErrorCellBackgroundBrush, outOfRange.Background);
            Assert.AreEqual(Environment.NewLine + "The probability value must be between 0 and 1.", ToolTip.GetTip(outOfRange));
            Assert.AreNotEqual(grid.ErrorCellBackgroundBrush, Cell(grid, 0, 0).Background);
            Assert.IsNull(ToolTip.GetTip(Cell(grid, 0, 0)));
            window.Close();
        });
    }

    [TestMethod]
    public void EditingACell_WritesTheOrdinate()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, grid, ordinates) = ShowOrdinates(0.1, 0.2, 0.3);
            var changes = Record(ordinates);

            grid.CurrentCell = new DataGridCellInfo(grid.Items[1], grid.Columns[0], 1, 0);
            Settle();
            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            Settle();
            var editor = (TextBox)Cell(grid, 1, 0).Content!;
            editor.SelectAll();
            window.Type("0.25");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle();

            CollectionAssert.AreEqual(new[] { 0.1, 0.25, 0.3 }, ordinates.ToArray());
            CollectionAssert.AreEqual(new[] { NotifyCollectionChangedAction.Replace }, changes);
            Assert.AreEqual("0.25", CellText(grid, 1, 0));
            window.Close();
        });
    }

    [TestMethod]
    public void Paste_WritesTheOrdinates_AndAddsRows_ThenRaisesOneReset()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var (window, _, grid, ordinates) = ShowOrdinates(0.1, 0.2);
            var changes = Record(ordinates);

            Select(grid, (1, 0));
            Clipboard.SetText("0.5\r\n0.9\r\n0.99\r\n");
            grid.PasteClipboard();

            CollectionAssert.AreEqual(new[] { 0.1, 0.5, 0.9, 0.99 }, ordinates.ToArray());
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, changes.Last(), "the paste ends with one reset");
            Assert.IsFalse(ordinates.SuppressCollectionChanged);
            Assert.IsNull(WpfCompat.Input.Mouse.OverrideCursor, "the wait cursor is cleared");
            Assert.AreEqual(4, grid.Items.Count);
            Assert.AreEqual("0.99", CellText(grid, 3, 0));
            window.Close();
        });
    }

    [TestMethod]
    public void AddingAndDeletingRows_InsertsAndRemovesOrdinates()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, grid, ordinates) = ShowOrdinates(0.1, 0.2, 0.3);
            var changes = Record(ordinates);

            grid.AddRows(2);
            CollectionAssert.AreEqual(new[] { 0.1, 0.2, 0.3, 0.0, 0.0 }, ordinates.ToArray(), "new rows' probability is 0");
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, changes.Single(), "one reset for the added rows");

            changes.Clear();
            Select(grid, (0, 0), (2, 0));
            grid.DeleteRows();
            CollectionAssert.AreEqual(new[] { 0.2, 0.0, 0.0 }, ordinates.ToArray());
            Assert.AreEqual(NotifyCollectionChangedAction.Reset, changes.Single());
            Assert.AreEqual(3, grid.Items.Count);

            changes.Clear();
            Select(grid, (1, 0));
            grid.InsertRows();
            CollectionAssert.AreEqual(new[] { 0.2, 0.0, 0.0, 0.0 }, ordinates.ToArray());
            window.Close();
        });
    }

    [TestMethod]
    public void ChangesToTheOrdinates_ShowInTheGrid()
    {
        HeadlessSession.Run(() =>
        {
            var (window, control, grid, ordinates) = ShowOrdinates(0.1, 0.2);

            ordinates.Add(0.3);
            Settle();
            Assert.AreEqual("0.3", CellText(grid, 2, 0));

            ordinates[0] = 0.05;
            Settle();
            Assert.AreEqual("0.05", CellText(grid, 0, 0));

            ordinates.RemoveAt(1);
            Settle();
            Assert.AreEqual(2, grid.Items.Count);
            Assert.AreEqual("0.3", CellText(grid, 1, 0));

            var other = new ProbabilityOrdinates(new[] { 0.9 });
            control.ProbabilityOrdinates = other;
            Settle();
            Assert.AreEqual(1, grid.Items.Count);
            ordinates.Add(0.4);
            Settle();
            Assert.AreEqual(1, grid.Items.Count, "the old ordinates aren't followed any more");
            window.Close();
        });
    }

    // The control showing these ordinates.
    private static (Window Window, ProbabilityOrdinatesControl Control, ValidationDataGrid Grid, ProbabilityOrdinates Ordinates) ShowOrdinates(params double[] values)
    {
        UseAppResources();
        var ordinates = new ProbabilityOrdinates(values);
        var control = new ProbabilityOrdinatesControl { ProbabilityOrdinates = ordinates };
        var window = Show(control);
        var grid = control.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
        grid.Focus();
        Settle();
        return (window, control, grid, ordinates);
    }

    // The collection changes the ordinates raise.
    private static List<NotifyCollectionChangedAction> Record(ProbabilityOrdinates ordinates)
    {
        var changes = new List<NotifyCollectionChangedAction>();
        ordinates.CollectionChanged += (_, e) => changes.Add(e.Action);
        return changes;
    }
}
