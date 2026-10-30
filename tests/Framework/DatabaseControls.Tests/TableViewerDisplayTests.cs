using Avalonia.Controls;
using Avalonia.Media;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// The TableViewer shows a DatabaseManager table as upstream's does: its own grid of cells, with column
/// and row headers, as many rows as fit, and a scroll bar for the rest.
/// </summary>
[TestClass]
public sealed class TableViewerDisplayTests
{
    [TestMethod]
    public void ShowsHeaders_RowNumbers_AndTheRowsThatFit()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));

            CollectionAssert.AreEqual(new[] { "Name", "Value", "Count" }, viewer.HeaderTexts());
            int rows = viewer.VisibleRowCount();
            Assert.IsTrue(rows > 5 && rows < 100, $"only the rows that fit are made ({rows})");
            Assert.AreEqual("Item 0", viewer.CellText(0, 0));
            Assert.AreEqual("1.5", viewer.CellText(1, 1));
            Assert.AreEqual("3", viewer.CellText(3, 2));
            // Row headers count from 0, as upstream's (for its VB version's sake).
            Assert.AreEqual("0", viewer.RowHeaderTexts()[0]);
            Assert.AreEqual("4", viewer.RowHeaderTexts()[4]);
            Assert.IsTrue(viewer.VerticalScrollbar.IsVisible, "the rest of the rows scroll");
            Assert.AreEqual(100 - rows, viewer.VerticalScrollbar.Maximum, 1);
            window.Close();
        });
    }
}
