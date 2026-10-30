using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using WpfCompat;
using static GenericControls.Tests.GridTesting;

namespace GenericControls.Tests;

/// <summary>
/// The grids as GenericControls' theme styles them: what a screenshot alone wouldn't show.
/// </summary>
[TestClass]
public sealed class DataGridThemeTests
{
    [TestMethod]
    public void ColumnHeaders_AreBoldAndCentred()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid) = ShowGrid(Rows(2));

            DataGridColumnHeader header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Name"));
            TextBlock text = header.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Text == "Name");
            Assert.AreEqual(FontWeight.Bold, text.FontWeight);
            Assert.AreEqual(TextAlignment.Center, text.TextAlignment);
            Assert.AreSame(grid.FindResource("DataGrid.Header.Foreground"), header.Foreground);
            window.Close();
        });
    }

    [TestMethod]
    public void Rows_Alternate_AndSelectedCellsDim_WhenTheGridLosesTheFocus()
    {
        HeadlessSession.Run(() =>
        {
            var other = new TextBox();
            var grid = new CopyPasteDataGrid { RowType = typeof(GridRow), ItemsSource = Rows(3) };
            DockPanel.SetDock(other, Dock.Top);
            var window = Show(new DockPanel { Children = { other, grid } });
            grid.Focus();
            Settle();

            DataGridRow Row(int index) => grid.GetVisualDescendants().OfType<DataGridRow>().Single(r => r.IsVisible && r.Index == index);
            Assert.AreSame(grid.FindResource("DataGrid.Row.Background"), Row(0).Background);
            Assert.AreSame(grid.FindResource("DataGrid.Row.Alternating.Background"), Row(1).Background);

            Select(grid, (1, 1));
            DataGridCell cell = Cell(grid, 1, 1);
            Assert.IsTrue(WpfSelector.GetIsSelectionActive(cell));
            Assert.AreSame(grid.FindResource("DataGrid.Row.Selection.Background"), cell.Background);

            other.Focus();
            Settle();
            Assert.IsFalse(WpfSelector.GetIsSelectionActive(cell), "the selection is inactive without the focus, as in WPF");
            Assert.AreSame(grid.FindResource("DataGrid.Row.Selection.Inactive.Background"), cell.Background);
            window.Close();
        });
    }

    [TestMethod]
    public void ColumnCellStyles_AlignTheText_AsTheyChange()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid) = ShowGrid(Rows(2));
            TextBlock Text(int row, int column) => (TextBlock)Cell(grid, row, column).Content!;
            Assert.AreEqual(TextAlignment.Start, Text(0, 1).TextAlignment, "WPF's Left, for left-to-right text");

            grid.Columns[1].CellTheme = (ControlTheme)grid.FindResource("Right_CellStyle")!;
            Settle();
            Assert.AreEqual(TextAlignment.Right, Text(0, 1).TextAlignment);
            Assert.AreEqual(TextAlignment.Right, Text(1, 1).TextAlignment);

            grid.Columns[1].CellTheme = (ControlTheme)grid.FindResource("Center_CellStyle")!;
            Settle();
            Assert.AreEqual(TextAlignment.Center, Text(0, 1).TextAlignment);
            Assert.AreEqual(TextAlignment.Start, Text(0, 0).TextAlignment, "only that column's cells");
            window.Close();
        });
    }

    [TestMethod]
    public void ColumnHeaders_AreAsHighAsTheirTextAtTheColumnsWidths()
    {
        HeadlessSession.Run(() =>
        {
            // As the distribution selector's statistics: star columns, in a grid first laid out narrow.
            var (window, grid) = ShowGrid(Rows(2), g =>
            {
                g.ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star);
                g.Width = 30;
            });
            var headers = grid.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.DataGridColumnHeadersPresenter>().Single();
            Assert.IsTrue(headers.Bounds.Height > 50, $"the narrow headers wrap their text: {headers.Bounds.Height}");

            grid.Width = 400;
            Settle();
            DataGridColumnHeader header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => Equals(h.Content, "Name"));
            Assert.AreEqual(24, header.Bounds.Height, "one line of text (the headers' MinHeight)");
            Assert.AreEqual(24, headers.Bounds.Height, "the row of headers is as high as its headers");
            window.Close();
        });
    }

    [TestMethod]
    public void ValidationGrid_TakesItsErrorBrushesFromTheTheme()
    {
        HeadlessSession.Run(() =>
        {
            var rows = new ObservableCollection<object>();
            var grid = new ValidationDataGrid { RowType = typeof(RuleRow), ItemsSource = rows };
            rows.Add(new RuleRow(rows, grid) { Name = "A", Value = 1 });
            var window = Show(grid);

            Assert.AreSame(grid.FindResource("DataGrid.Cell.Error.Background"), grid.ErrorCellBackgroundBrush);
            Assert.AreSame(grid.FindResource("DataGrid.Cell.Error.Border"), grid.ErrorCellBorderBrush);
            Assert.AreSame(grid.FindResource("ValidationDataGridCellStyle"), grid.CellTheme);
            window.Close();
        });
    }
}
