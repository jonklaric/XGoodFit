using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using WpfCompat;
using static GenericControls.Tests.GridTesting;

namespace GenericControls.Tests;

/// <summary>
/// StringListPropertyControl, a titled one-column CopyPasteDataGrid of strings (the axis editor's
/// category labels): it shows the list, writes edits and pastes back to it, and selects every string
/// when its title is clicked.
/// </summary>
[TestClass]
public sealed class StringListPropertyControlTests
{
    [TestMethod]
    public void TheList_IsShown_AndEditsAndPastesAreWrittenBack()
    {
        HeadlessSession.Run(() =>
        {
            var control = new StringListPropertyControl { Title = "Labels", StringList = ["Jan", "Feb", "Mar"] };
            // As in the properties window, the control sits in a scroll viewer, whose width the grid takes.
            var window = Show(new ScrollViewer { Content = control }, 300, 300);
            var grid = control.StringListDataGrid;
            Assert.AreEqual(300 - SystemParameters.VerticalScrollBarWidth, grid.Width, "the scroll viewer's width less a scroll bar");
            CollectionAssert.AreEqual(new[] { "Jan", "Feb", "Mar" }, Enumerable.Range(0, 3).Select(i => ((TextBlock)Cell(grid, i, 0).Content!).Text).ToArray());

            grid.Focus();
            grid.CurrentCell = new DataGridCellInfo(grid.Items[1], grid.Columns[0], 1, 0);
            Settle();
            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            Settle();
            ((TextBox)Cell(grid, 1, 0).Content!).SelectAll();
            window.KeyTextInput("February");
            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Settle();
            CollectionAssert.AreEqual(new[] { "Jan", "February", "Mar" }, control.StringList.ToArray(), "the edit is written back as the cell commits");

            Select(grid, (2, 0));
            Clipboard.SetText("March\r\nApril\r\n");
            grid.PasteClipboard();
            Settle();
            CollectionAssert.AreEqual(new[] { "Jan", "February", "March", "April" }, control.StringList.ToArray(), "the paste adds a row and is written back");

            // Upstream reads the list from the editor as the edit ends, so an edit cancelled with
            // Escape is kept too (an upstream quirk, kept).
            grid.Focus();
            grid.CurrentCell = new DataGridCellInfo(grid.Items[0], grid.Columns[0], 0, 0);
            Settle();
            window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
            Settle();
            ((TextBox)Cell(grid, 0, 0).Content!).SelectAll();
            window.KeyTextInput("January");
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            Settle();
            Assert.AreEqual("January", control.StringList[0]);
            window.Close();
        });
    }

    [TestMethod]
    public void ClickingTheTitle_SelectsEveryString()
    {
        HeadlessSession.Run(() =>
        {
            var control = new StringListPropertyControl { Title = "Labels", StringList = ["A", "B", "C"] };
            var window = Show(new ScrollViewer { Content = control }, 300, 300);
            var title = control.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "Labels");

            // Anywhere on the title's row, as in WPF, not only on its text.
            var point = title.TranslatePoint(new Point(5, title.Bounds.Height / 2), window)!.Value;
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Settle();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, control.StringListDataGrid.SelectedCells.Select(c => c.RowIndex).OrderBy(r => r).ToArray());
            window.Close();
        });
    }
}
