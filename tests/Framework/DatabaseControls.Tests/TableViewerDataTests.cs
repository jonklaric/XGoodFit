using System.Data;
using System.Data.SQLite;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DatabaseManager;
using GenericControls;
using WpfCompat;
using WpfCompat.Input;

namespace DatabaseControls.Tests;

/// <summary>
/// What the TableViewer writes: edits saved to DatabaseManager's SQLite tables, the clipboard's copy and
/// paste, and the row and column context menus.
/// </summary>
[TestClass]
public sealed class TableViewerDataTests
{
    private static string NewFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "xgf-dbc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static void CreateSampleDatabase(string path, int rows)
    {
        SQLiteManager.CreateSqLiteFile(path);
        var db = new SQLiteManager(path);
        db.SaveDataTable(TestSupport.SampleTable(rows));
        db.Close();
    }

    // Every cell of a table as SQLite stores it: its storage class and its value.
    private static List<string> ReadStored(string path, string table)
    {
        var cells = new List<string>();
        using var connection = new SQLiteConnection($"Data Source={path}");
        connection.Open();
        using var command = new SQLiteCommand($"SELECT typeof([Name]), [Name], typeof([Value]), [Value], typeof([Count]), [Count] FROM [{table}] ORDER BY rowid", connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cells.Add(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => Convert.ToString(reader.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
        }
        return cells;
    }

    private static void SetClipboard(Window window, string text)
    {
        CommandManager.InvalidateRequerySuggested();
        window.Activate();
        Clipboard.SetText(text);
    }

    [TestMethod]
    public void Edits_AreSavedToTheSQLiteTable_AsDatabaseManagerWritesThem()
    {
        HeadlessSession.Run(() =>
        {
            string folder = NewFolder();
            string viewerDb = Path.Combine(folder, "viewer.sqlite");
            string referenceDb = Path.Combine(folder, "reference.sqlite");
            CreateSampleDatabase(viewerDb, 20);
            CreateSampleDatabase(referenceDb, 20);

            // Through the viewer: typed edits, a pasted block, and Save.
            var manager = new SQLiteManager(viewerDb);
            var (viewer, window) = TestSupport.ShowViewer(manager.GetTableManager("Sample"), configure: v => v.Editable = true);
            window.Click(viewer.CellCentre(2, 1));
            window.Type("7.25");
            window.Press(Key.Enter);
            window.Click(viewer.CellCentre(4, 0));
            window.Type("Renamed");
            window.Press(Key.Enter);
            window.Click(viewer.CellCentre(5, 2));
            window.Type("3");
            window.Press(Key.Enter);
            SetClipboard(window, "8.5\t2\r\n9.75\t1\r\n");
            window.Click(viewer.CellCentre(6, 1));
            window.Press(Key.V, RawInputModifiers.Control);
            Assert.AreEqual("9.75", viewer.CellText(7, 1), "the block pastes from the selected cell on");

            string[] texts = [];
            using (TestSupport.WhenOpened<MessageBoxWindow>(box =>
            {
                texts = box.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
                box.ButtonWithContent("OK").RaiseClick();
            }))
            {
                viewer.SaveButton.RaiseClick();
            }
            CollectionAssert.Contains(texts, "Are you sure you want to save edits?");
            Assert.IsFalse(viewer.SaveButton.IsEnabled, "nothing is left to save");
            window.Close();
            manager.Close();

            // The same edits straight through DatabaseManager, as upstream's TableViewer hands them to it.
            var reference = new SQLiteManager(referenceDb);
            var view = reference.GetTableManager("Sample");
            view.EditCell(2, 1, "7.25");
            view.EditCell(4, 0, "Renamed");
            view.EditCell(5, 2, "3");
            view.EditCells(new[] { 6, 6, 7, 7 }, new[] { 1, 2, 1, 2 }, new object[] { "8.5", "2", "9.75", "1" });
            view.ApplyEdits();
            reference.Close();

            List<string> stored = ReadStored(viewerDb, "Sample");
            CollectionAssert.AreEqual(ReadStored(referenceDb, "Sample"), stored);
            Assert.AreEqual("text | Item 2 | real | 7.25 | integer | 2", stored[2]);
            Assert.AreEqual("text | Renamed | real | 6 | integer | 0", stored[4]);
            Assert.AreEqual("text | Item 6 | real | 8.5 | integer | 2", stored[6]);
        });
    }

    [TestMethod]
    public void Copy_PutsTheSelectedCellsOnTheClipboard_WithOrWithoutHeaders()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(20)));
            window.Click(viewer.CellCentre(1, 0));
            window.Click(viewer.CellCentre(2, 1), RawInputModifiers.Shift);
            CommandManager.InvalidateRequerySuggested();
            window.Activate();

            viewer.CopyButton.RaiseClick();
            Assert.AreEqual("Item 1\t1.5\nItem 2\t3", Clipboard.GetText());
            viewer.CopyWithHeadersButton.RaiseClick();
            Assert.AreEqual("Name\tValue\nItem 1\t1.5\nItem 2\t3", Clipboard.GetText());

            // Whole rows, from the row headers.
            var headers = viewer.RowHeadersGrid.Children.OfType<Border>().ToArray();
            window.Click(headers[0].Centre());
            window.Click(headers[1].Centre(), RawInputModifiers.Shift);
            window.Activate();
            window.Press(Key.C, RawInputModifiers.Control);
            Assert.AreEqual("Item 0\t0\t0\nItem 1\t1.5\t1", Clipboard.GetText());
            window.Close();
        });
    }

    [TestMethod]
    public void RowHeaderMenu_DeletesTheSelectedRows()
    {
        HeadlessSession.Run(() =>
        {
            Captures.UsePopupOverlay();
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            ContextMenu? menu = null;
            int menuRow = -1;
            viewer.RowRightButtonUp += (m, row) => (menu, menuRow) = (m, row);

            window.RightClick(viewer.RowHeadersGrid.Children.OfType<Border>().ElementAt(3).Centre());
            Assert.IsNotNull(menu);
            Assert.AreEqual(3, menuRow);
            Assert.IsTrue(menu.IsOpen);
            var delete = (MenuItem)menu.Items[0]!;
            Assert.AreEqual("Delete Row", delete.Header);

            delete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            TestSupport.Settle();
            Assert.AreEqual(19, view.NumberOfRows);
            Assert.AreEqual("Item 4", viewer.CellText(3, 0), "the viewer follows the deleted row");
            Assert.IsTrue(viewer.Undo.IsEnabled);
            menu.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void ColumnHeaderMenu_OffersUpstreamsCommands()
    {
        HeadlessSession.Run(() =>
        {
            Captures.UsePopupOverlay();
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            viewer.SetColumnsAsReadOnly(new[] { 2 });

            window.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(1).Centre());
            var menu = window.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen);
            var items = menu.Items.Cast<MenuItem>().ToArray();
            CollectionAssert.AreEqual(
                new[] { "Sort Ascending", "Sort Descending", "Clear Sort", "Summary Statistics...", "Find...", "Field Calculator...", "Delete Column" },
                items.Select(i => (string)i.Header!).ToArray());
            Assert.IsFalse(items[2].IsEnabled, "nothing to clear before a sort");
            Assert.IsTrue(items.All(i => i.Icon is Control), "each item has its icon");

            items[1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            TestSupport.Settle();
            Assert.AreEqual("Item 19", viewer.CellText(0, 0));

            // A read-only column has no field calculator or delete.
            window.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(2).Centre());
            menu = window.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen);
            Assert.AreEqual(5, menu.ItemCount);
            menu.Close();

            // Delete Column deletes the selected column.
            window.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(0).Centre());
            menu = window.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen);
            menu.Items.Cast<MenuItem>().Last().RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            TestSupport.Settle();
            CollectionAssert.AreEqual(new[] { "Value", "Count" }, view.ColumnNames);
            CollectionAssert.AreEqual(new[] { "Value", "Count" }, viewer.HeaderTexts());
            window.Close();
        });
    }
}
