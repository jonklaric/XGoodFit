using System.Globalization;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// The column statistics window (numeric and text columns, and the selected rows only), upstream's
/// classification methods, and the calculator help pages in HtmlRenderer's panel.
/// </summary>
[TestClass]
public sealed class StatisticsAndHelpTests
{
    private static string[] StatValues(TableViewer stats) =>
        stats.DataView.GetColumn(1).Select(o => Convert.ToString(o, CultureInfo.InvariantCulture) ?? "").ToArray();

    [TestMethod]
    public void NumericColumn_ShowsUpstreamsStatistics_AndAHistogram()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(20)));
            var stats = new ColumnStatsWindow(viewer, 1);
            stats.Show();
            TestSupport.Settle();

            Assert.AreEqual("Value Summary Statistics", stats.Title);
            Assert.IsTrue(stats.NumericColumnViewer.IsVisible);
            Assert.IsFalse(stats.AlphabeticColumnViewer.IsVisible);
            Assert.IsFalse(stats.SelectedOnlyCheckbox.IsEnabled, "no rows are selected");
            string[] values = StatValues(stats.NumericColumnViewer.StatsTable);
            // Values 0, 1.5, ..., 28.5, as upstream formats them (F15, invariant).
            Assert.AreEqual("20", values[0]);
            Assert.AreEqual("0.000000000000000", values[1]);
            Assert.AreEqual("28.500000000000000", values[2]);
            Assert.AreEqual("285.000000000000000", values[3]);
            Assert.AreEqual("14.250000000000000", values[4]);
            Assert.AreEqual("14.250000000000000", values[10], "the median");
            // Upstream's 311-pixel table shows all 13 statistics: the column widths and the visible rows are
            // worked out after the layout pass, as in WPF (WpfCompat's SizeChangedEvents).
            Assert.AreEqual(13, stats.NumericColumnViewer.StatsTable.VisibleRowCount());
            Assert.IsFalse(stats.NumericColumnViewer.StatsTable.VerticalScrollbar.IsVisible);
            Assert.IsTrue(stats.NumericColumnViewer.HistogramData.Count > 0, "Rice's rule bins the data");
            Assert.AreEqual(stats.NumericColumnViewer.HistogramData.Count, stats.NumericColumnViewer.BreaksTable.DataView.NumberOfRows);
            Assert.AreEqual(20, stats.NumericColumnViewer.BreaksTable.DataView.GetColumn(1).Sum(o => Convert.ToInt32(o)), "the bins hold every value");

            // Equal intervals with four classes.
            stats.NumericColumnViewer.GroupingComboBox.SelectedItem = stats.NumericColumnViewer.EqualIntervalItem;
            TestSupport.Settle();
            Assert.IsTrue(stats.NumericColumnViewer.ClassCountControl.IsVisible);
            CollectionAssert.AreEqual(Classification.EqualInterval(Enumerable.Range(0, 20).Select(i => i * 1.5).ToArray(), 4, true),
                stats.NumericColumnViewer.HistogramData.Select(h => h.RangeEnd).ToArray());
            stats.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void SelectedRowsOnly_LimitsTheStatistics_AndFollowsTheViewersSelection()
    {
        HeadlessSession.Run(() =>
        {
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(20)));
            viewer.UpdateSelectedRowIndices(new[] { 0, 1, 2, 3, 4 });
            var stats = new ColumnStatsWindow(viewer, 1);
            stats.Show();
            TestSupport.Settle();

            Assert.AreEqual(true, stats.SelectedOnlyCheckbox.IsChecked, "with rows selected, it starts with them");
            Assert.AreEqual("5", StatValues(stats.NumericColumnViewer.StatsTable)[0]);
            stats.SelectedOnlyCheckbox.IsChecked = false;
            TestSupport.Settle();
            Assert.AreEqual("20", StatValues(stats.NumericColumnViewer.StatsTable)[0]);
            stats.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void TextColumn_CountsItsValues_AndPiesTheTopFour()
    {
        HeadlessSession.Run(() =>
        {
            var table = TestSupport.SampleTable(10);
            table.Rows[3]["Name"] = "Item 1";
            table.Rows[4]["Name"] = "Item 1";
            table.Rows[5]["Name"] = DBNull.Value;
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(table));
            var stats = new ColumnStatsWindow(viewer, 0);
            stats.Show();
            TestSupport.Settle();

            Assert.IsTrue(stats.AlphabeticColumnViewer.IsVisible);
            var counts = stats.AlphabeticColumnViewer.StatsTable.DataView;
            CollectionAssert.AreEqual(new object[] { "< Total Count >", "Item 1" }, counts.GetColumn(0).Take(2).ToArray());
            CollectionAssert.AreEqual(new object[] { 10, 3 }, counts.GetColumn(1).Take(2).ToArray());
            Assert.IsTrue(counts.GetColumn(0).Contains(""), "empty values count together");
            var pie = (OxyPlot.Series.PieSeries)stats.AlphabeticColumnViewer.PieSeries.InternalSeries;
            Assert.AreEqual(5, pie.Slices.Count, "the top four values and All Other");
            Assert.AreEqual("All Other", pie.Slices[4].Label);
            stats.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void SummaryStatistics_FromTheColumnMenu_OpensOwnedByTheViewersWindow()
    {
        HeadlessSession.Run(() =>
        {
            Captures.UsePopupOverlay();
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(20)));
            window.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(2).Centre());
            var menu = window.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen);
            menu.Items.Cast<MenuItem>().Single(i => (string)i.Header! == "Summary Statistics...").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            menu.Close();
            TestSupport.Settle();

            var stats = window.OwnedWindows.OfType<ColumnStatsWindow>().Single();
            Assert.AreEqual("Count Summary Statistics", stats.Title);
            stats.Close();
            window.Close();
        });
    }

    [TestMethod]
    public void CalculatorHelp_ShowsTheEmbeddedPage_WithItsPictures()
    {
        HeadlessSession.Run(() =>
        {
            foreach ((string type, string title, string heading, string picture) in new[]
            {
                ("FieldCalculator", "Field Calculator Help", "<h1>Field Calculator</h1>", "FieldCalculator.png"),
                ("SelectByAttribute", "Select By Attribute Help", "<h1>Select By Attribute</h1>", "SelectByAttributeHelp.png"),
            })
            {
                var help = new CalculatorHelpWindow(type);
                var requested = new List<string>();
                help.HelpBrowser.ImageLoad += (_, e) => requested.Add(e.Event.Src);
                help.Show();
                TestSupport.Settle();

                Assert.AreEqual(title, help.Title);
                StringAssert.Contains(help.HelpBrowser.Text, heading);
                CollectionAssert.Contains(requested, picture);
                Assert.IsTrue(File.Exists(Path.Combine(Path.GetTempPath(), "DatabaseControls_Help", picture)), "upstream extracts the page's pictures next to it");
                help.Close();
            }
        });
    }
}
