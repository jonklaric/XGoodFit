using Avalonia.Controls;
using Avalonia.Headless;
using WpfCompat;
using static GenericControls.Tests.GridTesting;

namespace GenericControls.Tests;

/// <summary>
/// DataGridToolbar's buttons, as upstream wires them: enabled by the grid's selection, and running the
/// grid's row, selection and clipboard commands.
/// </summary>
[TestClass]
public sealed class DataGridToolbarTests
{
    [TestMethod]
    public void Buttons_AreEnabledByTheSelection()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, toolbar) = ShowToolbar();

            Assert.IsTrue(toolbar.AddRowsButton.IsEnabled);
            Assert.IsTrue(toolbar.SelectAllButton.IsEnabled);
            Assert.IsFalse(toolbar.InsertRowsButton.IsEnabled, "nothing is selected");
            Assert.IsFalse(toolbar.DeleteRowsButton.IsEnabled);
            Assert.IsFalse(toolbar.CopyButton.IsEnabled);
            Assert.IsFalse(toolbar.CopyWithHeadersButton.IsEnabled);
            Assert.IsFalse(toolbar.PasteButton.IsEnabled);

            Clipboard.SetText("x");
            Select(grid, (0, 0));
            Assert.IsTrue(toolbar.InsertRowsButton.IsEnabled);
            Assert.IsTrue(toolbar.DeleteRowsButton.IsEnabled);
            Assert.IsTrue(toolbar.CopyButton.IsEnabled);
            Assert.IsTrue(toolbar.CopyWithHeadersButton.IsEnabled);
            Assert.IsTrue(toolbar.PasteButton.IsEnabled, "there's text to paste");

            grid.UnselectAllCells();
            Settle();
            Assert.IsFalse(toolbar.CopyButton.IsEnabled);
            Assert.IsFalse(toolbar.PasteButton.IsEnabled);
            window.Close();
        });
    }

    [TestMethod]
    public void RowButtons_AddInsertAndDeleteRows()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, toolbar) = ShowToolbar();
            var rows = (System.Collections.ObjectModel.ObservableCollection<GridRow>)grid.ItemsSource!;

            Click(toolbar.AddRowsButton);
            Assert.AreEqual(4, rows.Count, "one row with nothing selected");

            SelectBlock(grid, 0, 0, 1, 0);
            Click(toolbar.AddRowsButton);
            Assert.AreEqual(6, rows.Count, "as many rows as the selection spans");

            SelectBlock(grid, 1, 0, 2, 1);
            Click(toolbar.InsertRowsButton);
            Assert.AreEqual(8, rows.Count);
            CollectionAssert.AreEqual(new[] { "Item 1", "", "", "Item 2" }, rows.Take(4).Select(r => r.Name).ToArray());

            Select(grid, (1, 0), (2, 0));
            Click(toolbar.DeleteRowsButton);
            CollectionAssert.AreEqual(new[] { "Item 1", "Item 2", "Item 3" }, rows.Take(3).Select(r => r.Name).ToArray());

            grid.CanUserAddInsertDeleteRows = false;
            Assert.IsFalse(toolbar.AddRowsButton.IsVisible);
            Assert.IsFalse(toolbar.InsertRowsButton.IsVisible);
            Assert.IsFalse(toolbar.DeleteRowsButton.IsVisible);
            grid.CanUserAddInsertDeleteRows = true;
            Assert.IsTrue(toolbar.AddRowsButton.IsVisible);
            window.Close();
        });
    }

    [TestMethod]
    public void SelectAll_Toggles_AndTheClipboardButtonsCopyAndPaste()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid, toolbar) = ShowToolbar();
            var rows = (System.Collections.ObjectModel.ObservableCollection<GridRow>)grid.ItemsSource!;

            Click(toolbar.SelectAllButton);
            Assert.AreEqual(9, grid.SelectedCells.Count);
            Assert.AreEqual("Deselect all cells", ToolTip.GetTip(toolbar.SelectAllButton));
            Click(toolbar.SelectAllButton);
            Assert.AreEqual(0, grid.SelectedCells.Count);
            Assert.AreEqual("Select all cells", ToolTip.GetTip(toolbar.SelectAllButton));

            SelectBlock(grid, 0, 0, 1, 1);
            Click(toolbar.CopyButton);
            Assert.AreEqual($"Item 1\t{10.5}\nItem 2\t{21.0}", Clipboard.GetText());
            Click(toolbar.CopyWithHeadersButton);
            Assert.AreEqual($"Name\tValue\nItem 1\t{10.5}\nItem 2\t{21.0}", Clipboard.GetText());

            Clipboard.SetText("Pasted\t7");
            Select(grid, (2, 0));
            Click(toolbar.PasteButton);
            Assert.AreEqual("Pasted", rows[2].Name);
            Assert.AreEqual(7.0, rows[2].Value);
            window.Close();
        });
    }

    [TestMethod]
    public void CustomButtons_GoAfterTheRowButtons_WithTheirSeparator()
    {
        HeadlessSession.Run(() =>
        {
            var (window, _, toolbar) = ShowToolbar();
            var first = new Button { Content = "1" };
            var second = new Button { Content = "2" };

            toolbar.CustomButtons.Add(first);
            toolbar.CustomButtons.Add(second);
            var children = toolbar.ToolbarStackPanel.Children;
            Assert.AreEqual(children.IndexOf(toolbar.CustomOptionsSeparator) + 1, children.IndexOf(first));
            Assert.AreEqual(children.IndexOf(first) + 1, children.IndexOf(second));
            Assert.IsTrue(toolbar.CustomOptionsSeparator.IsVisible);
            Assert.IsNotNull(first.Theme, "the toolbar's button theme");

            // Removed one by one: upstream leaves the buttons in the toolbar when the collection is cleared.
            toolbar.CustomButtons.Remove(first);
            toolbar.CustomButtons.Remove(second);
            Assert.IsFalse(children.Contains(first));
            Assert.IsFalse(children.Contains(second));
            Assert.IsFalse(toolbar.CustomOptionsSeparator.IsVisible);
            window.Close();
        });
    }

    // A toolbar over a copy-paste grid of three rows, shown.
    private static (Window Window, CopyPasteDataGrid Grid, DataGridToolbar Toolbar) ShowToolbar()
    {
        var grid = new CopyPasteDataGrid { RowType = typeof(GridRow), ItemsSource = Rows(3) };
        var toolbar = new DataGridToolbar { DataGrid = grid };
        DockPanel.SetDock(toolbar, Dock.Top);
        var window = Show(new DockPanel { Children = { toolbar, grid } });
        grid.Focus();
        Settle();
        return (window, grid, toolbar);
    }
}
