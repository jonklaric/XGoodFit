using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.VisualTree;
using Themes;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// The TableViewer shows a DatabaseManager table as upstream's does: its own grid of cells, with column
/// and row headers, as many rows as fit, and a scroll bar for the rest; its options hide its parts, and its
/// colours follow the theme.
/// </summary>
[TestClass]
public sealed class TableViewerDisplayTests
{
    // Upstream measures header and cell text in bold Segoe UI 12 (Selawik outside Windows: ThemeFonts).
    private static double TextWidth(string text) =>
        new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
            12, Brushes.Black).Width;

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
            Assert.AreEqual(22, viewer.GridPanel.RowDefinitions[0].ActualHeight, "the theme's row height");
            // The row header column fits the row count in bold, plus 10, as upstream sizes it.
            Assert.AreEqual((int)TextWidth("100") + 10, viewer.TableControlGrid.ColumnDefinitions[0].Width.Value);
            window.Close();
        });
    }

    [TestMethod]
    public void ATableThatFits_HasNoScrollBar()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(null);
            Assert.IsFalse(viewer.VerticalScrollbar.IsVisible, "no table: WPF's disabled scroll bar is collapsed");
            viewer.DataView = TestSupport.InMemory(TestSupport.SampleTable(5));
            TestSupport.Settle();
            Assert.AreEqual(5, viewer.VisibleRowCount());
            Assert.IsFalse(viewer.VerticalScrollbar.IsVisible);
            Assert.AreEqual(0, viewer.VerticalScrollbar.Maximum);

            viewer.DataView = TestSupport.InMemory(TestSupport.SampleTable(50));
            TestSupport.Settle();
            Assert.IsTrue(viewer.VerticalScrollbar.IsVisible);
            window.Close();
        });
    }

    [TestMethod]
    public void Options_HideTheirParts()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(10)), configure: v =>
            {
                v.ShowRowHeaders = false;
                v.Editable = true;
                v.HasFieldCalculator = false;
                v.HasSaveButton = false;
            });
            Assert.AreEqual(Visibility.Collapsed, viewer.RowHeadersGrid.Visibility);
            Assert.AreEqual(Visibility.Collapsed, viewer.SelectAllBorder.Visibility);
            Assert.IsTrue(viewer.EditorToolbar.IsVisible);
            Assert.IsFalse(viewer.OpenFC.IsVisible);
            Assert.IsFalse(viewer.SaveButton.IsVisible);
            Assert.IsTrue(viewer.Undo.IsVisible && viewer.Redo.IsVisible);
            Assert.IsTrue(viewer.PasteButton.IsVisible);

            viewer.HasUndoRedoButtons = false;
            viewer.RowSelectable = false;
            viewer.Editable = false;
            TestSupport.Settle();
            Assert.IsFalse(viewer.Undo.IsVisible);
            Assert.IsFalse(viewer.SelectionToolbar.IsVisible);
            Assert.IsFalse(viewer.EditorToolbar.IsVisible);
            Assert.IsFalse(viewer.PasteButton.IsEnabled, "nothing pastes into a table that isn't editable");
            window.Close();
        });
    }

    [TestMethod]
    public void ColumnWidths_FitTheHeaders_OrShareTheWidthWithAutoFit()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(10)), width: 700);
            double[] widths = viewer.ColumnHeadersGrid.ColumnDefinitions.Select(c => c.Width.Value).ToArray();
            CollectionAssert.AreEqual(new[] { "Name", "Value", "Count" }.Select(h => (double)((int)TextWidth(h) + 12)).ToArray(), widths,
                "each column fits its header in bold, plus 12, as upstream sizes it");
            CollectionAssert.AreEqual(widths, viewer.GridPanel.ColumnDefinitions.Select(c => c.Width.Value).ToArray(), "the cells line up with the headers");
            window.Close();

            (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(10)), width: 700, configure: v => v.AutoFitColumns = true);
            widths = viewer.ColumnHeadersGrid.ColumnDefinitions.Select(c => c.Width.Value).ToArray();
            Assert.AreEqual(viewer.HorizontalScrollViewer.Bounds.Width - 1, widths.Sum(), 2, "auto-fit columns share the width");
            Assert.AreEqual(widths[0], widths[2], 1);

            // Double-clicking a column's right edge fits it to its longest value.
            var splitter = viewer.ColumnHeadersGrid.Children.OfType<GridSplitter>().First();
            window.DoubleClick(splitter.Centre());
            Assert.AreEqual(Math.Max((int)TextWidth("Name") + 12, (int)TextWidth("Item 9") + 6), viewer.ColumnHeadersGrid.ColumnDefinitions[0].Width.Value);
            window.Close();
        });
    }

    [TestMethod]
    public void Themes_RecolourTheTable()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(10)));
            foreach (Theme theme in new[] { Theme.Dark, Theme.Blue, Theme.Light })
            {
                ThemeService.Instance.SetTheme(theme);
                TestSupport.Settle();
                var rows = viewer.RowColorGrid.Children.OfType<Rectangle>().ToArray();
                Assert.AreEqual(viewer.FindResource("DataGrid.Row.Background"), rows[0].Fill, $"{theme}: rows");
                Assert.AreEqual(viewer.FindResource("DataGrid.Row.Alternating.Background"), rows[1].Fill, $"{theme}: alternate rows");
                Assert.AreEqual(viewer.FindResource("DataGrid.Static.Foreground"), viewer.CellBlock(0, 0).Foreground, $"{theme}: text");
                var headerText = viewer.ColumnHeadersGrid.Children.OfType<Border>().First().GetVisualDescendants().OfType<TextBlock>().Single();
                Assert.AreEqual(viewer.FindResource("DataGrid.Header.Foreground"), headerText.Foreground, $"{theme}: header text");
                Assert.AreEqual(FontWeight.Bold, headerText.FontWeight);
                var columnLine = (WpfCompat.Shapes.Line)viewer.GridLinesCanvas.Children[1];
                Assert.AreEqual(viewer.FindResource("DataGrid.GridLines"), columnLine.Stroke, $"{theme}: grid lines");
            }
            window.Close();
        });
    }
}
