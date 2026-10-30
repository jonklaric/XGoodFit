using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// The TableViewer's mouse and keyboard behaviour, as upstream's: the active cell, cell, row and column
/// selection, the arrow, page and edit keys, the mouse wheel, sorting from the headers, and editing a cell
/// with undo and redo.
/// </summary>
[TestClass]
public sealed class TableViewerInteractionTests
{
    [TestMethod]
    public void ClickingACell_MakesItTheActiveCell_AndArrowsMoveIt()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));
            int moves = 0;
            viewer.ActiveCellLocationChanged += () => moves++;

            window.Click(viewer.CellCentre(2, 1));
            Assert.AreEqual(2, viewer.ActiveCellRowIndex);
            Assert.AreEqual(1, viewer.ActiveCellColumnIndex);
            Assert.IsTrue(moves > 0);
            Assert.IsTrue(viewer.GridPanel.IsFocused, "a click focuses the table, as upstream's GridPanel.Focus()");
            Assert.AreEqual(viewer.ActiveCellBackground, viewer.CellBackground(2, 1), "the active cell shows while the table has focus");

            window.Press(Key.Down);
            Assert.AreEqual(3, viewer.ActiveCellRowIndex);
            window.Press(Key.Right);
            Assert.AreEqual(2, viewer.ActiveCellColumnIndex);
            window.Press(Key.Right);
            Assert.AreEqual(2, viewer.ActiveCellColumnIndex, "the last column stops it");
            window.Press(Key.Left);
            window.Press(Key.Up);
            Assert.AreEqual((2, 1), (viewer.ActiveCellRowIndex, viewer.ActiveCellColumnIndex));
            // Enter moves down, Tab right (when no cell is edited).
            window.Press(Key.Enter);
            Assert.AreEqual(3, viewer.ActiveCellRowIndex);
            window.Press(Key.Tab);
            Assert.AreEqual(2, viewer.ActiveCellColumnIndex);
            Assert.IsTrue(viewer.GridPanel.IsFocused, "Tab stays in the table");
            window.Close();
        });
    }

    [TestMethod]
    public void ArrowsPastTheLastVisibleRow_ScrollTheTable()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));
            int rows = viewer.VisibleRowCount();
            window.Click(viewer.CellCentre(rows - 2, 0));
            int first = (int)viewer.VerticalScrollbar.Value;

            window.Press(Key.Down);
            window.Press(Key.Down);
            Assert.AreEqual(rows, viewer.ActiveCellRowIndex);
            Assert.IsTrue(viewer.VerticalScrollbar.Value > first, "the table scrolls to the active cell");
            Assert.AreEqual($"Item {(int)viewer.VerticalScrollbar.Value}", viewer.CellText(0, 0));

            window.Press(Key.PageDown);
            Assert.IsTrue(viewer.ActiveCellRowIndex > rows, "PageDown moves a page down");
            window.Close();
        });
    }

    [TestMethod]
    public void MouseWheel_ScrollsTwelveRowsANotch_AndRefillsTheCells()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));

            window.MouseWheel(viewer.CellCentre(1, 1), new Vector(0, -1));
            TestSupport.Settle();
            // As upstream's: WPF's 120-per-notch delta divided by 10.
            Assert.AreEqual(12, viewer.VerticalScrollbar.Value);
            Assert.AreEqual("Item 12", viewer.CellText(0, 0));
            Assert.AreEqual("12", viewer.RowHeaderTexts()[0]);
            Assert.AreEqual("Item 13", viewer.CellText(1, 0));

            window.MouseWheel(viewer.CellCentre(1, 1), new Vector(0, 1));
            TestSupport.Settle();
            Assert.AreEqual(0, viewer.VerticalScrollbar.Value);
            Assert.AreEqual("Item 0", viewer.CellText(0, 0));
            window.Close();
        });
    }

    [TestMethod]
    public void RowHeaders_SelectRows_AndRaiseSelectedRowIndicesChanged()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)), configure: v => v.Editable = true);
            List<int>? reported = null;
            viewer.SelectedRowIndicesChanged += rows => reported = rows.ToList();
            var headers = viewer.RowHeadersGrid.Children.OfType<Border>().ToArray();

            window.Click(headers[3].Centre());
            window.Click(headers[5].Centre(), RawInputModifiers.Shift);

            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, viewer.GetSelectedRows);
            CollectionAssert.AreEqual(new[] { 3, 4, 5 }, reported);
            Assert.AreEqual(viewer.SelectedColor, viewer.CellBackground(4, 2), "the rows' cells show as selected");
            Assert.AreEqual(viewer.DeSelectedColor, viewer.CellBackground(6, 2));
            Assert.IsTrue(viewer.ShowSelected.IsEnabled && viewer.DeSelectAll.IsEnabled);

            // Show selected rows only, then all again.
            viewer.ShowSelected.RaiseClick();
            TestSupport.Settle();
            Assert.IsTrue(viewer.ShowingSelectedRowsOnly);
            Assert.AreEqual(3, viewer.VisibleRowCount());
            CollectionAssert.AreEqual(new[] { "3", "4", "5" }, viewer.RowHeaderTexts());
            Assert.AreEqual("Item 5", viewer.CellText(2, 0));
            viewer.ShowAll.RaiseClick();
            TestSupport.Settle();
            Assert.IsFalse(viewer.ShowingSelectedRowsOnly);
            Assert.IsTrue(viewer.VisibleRowCount() > 3);

            viewer.DeSelectAll.RaiseClick();
            TestSupport.Settle();
            Assert.AreEqual(0, viewer.GetSelectedRows.Count);
            window.Close();
        });
    }

    [TestMethod]
    public void ColumnHeaderClick_SelectsTheColumn_AndSelectAllSelectsEverything()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));
            var header = viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(1);

            window.Click(header.Centre());
            Assert.AreEqual(viewer.SelectedColor, viewer.CellBackground(5, 1));
            Assert.AreEqual(viewer.DeSelectedColor, viewer.CellBackground(5, 0));

            window.Press(Key.A, RawInputModifiers.Control);
            Assert.IsTrue(viewer.AllCellsSelected);
            Assert.AreEqual(viewer.SelectedColor, viewer.CellBackground(5, 0));
            Assert.AreEqual("Deselect all cells", ToolTip.GetTip(viewer.SelectAllButton), "the select-all button turns into deselect-all");

            viewer.SelectAllButton.RaiseClick();
            TestSupport.Settle();
            Assert.IsFalse(viewer.AllCellsSelected);
            Assert.AreEqual("Select all cells", ToolTip.GetTip(viewer.SelectAllButton));
            window.Close();
        });
    }

    [TestMethod]
    public void DoubleClickingAColumnHeader_SortsDescending_ThenAscending()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));
            var header = viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(1);

            window.DoubleClick(header.Centre());
            Assert.AreEqual("Item 99", viewer.CellText(0, 0), "the first double click sorts descending");
            Assert.AreEqual("99", viewer.RowHeaderTexts()[0], "row headers keep the data rows' numbers");
            Assert.IsTrue(header.GetVisualDescendants().OfType<Viewbox>().Count(v => v.IsVisible) == 1, "the sort arrow shows");

            Thread.Sleep(600); // past the double-click time: otherwise these clicks count 3 and 4, as in WPF
            window.DoubleClick(header.Centre());
            Assert.AreEqual("Item 0", viewer.CellText(0, 0), "the second sorts ascending");
            window.Close();
        });
    }

    [TestMethod]
    public void TypingInACell_EditsIt_EnterCommits_AndUndoRedoFollow()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            window.Click(viewer.CellCentre(1, 1));

            window.Type("42");
            var editor = viewer.GridPanel.Children.OfType<TextBox>().Single();
            Assert.AreEqual("42", editor.Text, "the first key starts the edit and replaces the text");
            Assert.IsTrue(editor.IsFocused);

            window.Press(Key.Enter);
            Assert.IsFalse(viewer.GridPanel.Children.OfType<TextBox>().Any(), "Enter ends the edit");
            Assert.AreEqual(42d, view.GetCell(1, 1), "the view stores the value in the column's type");
            Assert.AreEqual("42", viewer.CellText(1, 1));
            Assert.AreEqual(2, viewer.ActiveCellRowIndex, "and moves down, as upstream's");
            Assert.IsTrue(viewer.Undo.IsEnabled && viewer.SaveButton.IsEnabled);
            Assert.IsFalse(viewer.Redo.IsEnabled);

            window.Press(Key.Z, RawInputModifiers.Control);
            Assert.AreEqual(1.5d, view.GetCell(1, 1));
            Assert.AreEqual("1.5", viewer.CellText(1, 1));
            Assert.IsTrue(viewer.Redo.IsEnabled);
            window.Press(Key.Y, RawInputModifiers.Control);
            Assert.AreEqual("42", viewer.CellText(1, 1));
            window.Close();
        });
    }

    [TestMethod]
    public void DoubleClickingACell_EditsItsText_AndEscapeKeepsTheValue()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);

            window.DoubleClick(viewer.CellCentre(3, 0));
            var editor = viewer.GridPanel.Children.OfType<TextBox>().Single();
            Assert.AreEqual("Item 3", editor.Text);
            Assert.AreEqual("Item 3", editor.SelectedText, "the text is selected, so typing replaces it");
            Assert.IsTrue(editor.IsFocused, "the release focuses the editor");

            window.Type("x");
            window.Press(Key.Escape);
            Assert.IsFalse(viewer.GridPanel.Children.OfType<TextBox>().Any());
            Assert.AreEqual("Item 3", view.GetCell(0, 3));
            Assert.IsFalse(view.CanUndo(), "Escape makes no edit");
            window.Close();
        });
    }

    [TestMethod]
    public void ReadOnlyColumns_AndViewersThatArentEditable_DontEdit()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            viewer.SetColumnsAsReadOnly(new[] { "Count" });

            window.Click(viewer.CellCentre(1, 2));
            window.Type("7");
            Assert.IsFalse(viewer.GridPanel.Children.OfType<TextBox>().Any());

            viewer.Editable = false;
            window.Click(viewer.CellCentre(1, 0));
            window.Type("7");
            Assert.IsFalse(viewer.GridPanel.Children.OfType<TextBox>().Any());
            Assert.IsFalse(view.CanUndo());
            Assert.IsFalse(viewer.EditorToolbar.IsVisible, "the editor tools hide when the table isn't editable");
            window.Close();
        });
    }
}
