using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Time Series Data walkthroughs' common steps (examples/1-time-series-data/*/*.md), through the
    /// main window: the saved records' inventory, the walkthroughs' figures, and a new source set up for a download.
    /// </summary>
    internal static class WalkthroughTimeSeriesTesting
    {
        /// <summary>
        /// Records over this many observations open in category Large only: a view costs about 1 KB per observation
        /// (NumericControls' table makes a row for each, as upstream's), as TimeSeriesDataTests has it.
        /// </summary>
        internal const int LargeRecord = 200000;

        /// <summary>Whether the tests of the largest records may run (they take over a gigabyte).</summary>
        internal static void RequireLarge()
        {
            if (Environment.GetEnvironmentVariable("XGOODFIT_LARGE_TESTS") != "1")
                Assert.Inconclusive("Views of the examples' largest records take over a gigabyte: set XGOODFIT_LARGE_TESTS=1 to run this test.");
        }

        /// <summary>A row of a walkthrough's inventory table ("Source and saved records").</summary>
        /// <param name="Element">The saved element.</param>
        /// <param name="Units">Its units (its Unit Label).</param>
        /// <param name="First">The first saved date, yyyy-MM-dd.</param>
        /// <param name="Last">The last saved date.</param>
        /// <param name="Ordinates">The number of ordinates.</param>
        /// <param name="Missing">The number of missing (nonfinite) values.</param>
        internal sealed record Record(string Element, string Units, string First, string Last, int Ordinates, int Missing);

        /// <summary>The Time Series view's left-hand tabs.</summary>
        internal static TabControl MainTabs(TimeSeriesControl view) => view.FindControl<TabControl>("MainTabControl");

        /// <summary>The Time Series tab's tabs: the table's and the statistics'.</summary>
        internal static TabControl DataTabs(TimeSeriesControl view) => view.FindControl<TabControl>("DataTabControl");

        /// <summary>The Time Series Data table's grid.</summary>
        internal static ValidationDataGrid Table(TimeSeriesControl view) =>
            view.TimeSeriesTable.GetVisualDescendants().OfType<ValidationDataGrid>().Single();

        /// <summary>Shows a tab of the view by its header, as a click on it does.</summary>
        internal static void Show(TimeSeriesControl view, string header)
        {
            Select(Tab(MainTabs(view), header));
            if (header == "Time Series") Select(Tab(DataTabs(view), "Time Series Data"));
        }

        /// <summary>The Summary Statistics tab's measures, as it shows them, by name.</summary>
        internal static Dictionary<string, string> SummaryStatistics(TimeSeriesControl view)
        {
            Select(Tab(MainTabs(view), "Time Series"));
            Select(Tab(DataTabs(view), "Summary Statistics"));
            Dictionary<string, string> shown = Statistics(view.SummaryStatisticsDataGrid);
            Select(Tab(DataTabs(view), "Time Series Data"));
            return shown;
        }

        /// <summary>
        /// A date the Time Series Data table shows: in the machine's culture, as upstream's (NumericControls'
        /// DateToStringConverter formats with the current culture's short date and time patterns, not WPF's binding culture).
        /// </summary>
        internal static DateTime TableDate(string text) => DateTime.Parse(Plain(text), CultureInfo.CurrentCulture);

        /// <summary>The date the Time Series Data table shows in a row.</summary>
        internal static DateTime TableDate(ValidationDataGrid grid, int row) => TableDate(CellText(grid, row, 1));

        /// <summary>The dates the Time Series Data table shows in its first and last rows.</summary>
        internal static (DateTime First, DateTime Last) Dates(TimeSeriesControl view)
        {
            Show(view, "Time Series");
            ValidationDataGrid grid = Table(view);
            Assert.AreEqual("Date Time", grid.Columns[1].Header as string);
            Assert.AreEqual("Value", grid.Columns[2].Header as string);
            return (TableDate(grid, 0), TableDate(grid, grid.Items.Count - 1));
        }

        /// <summary>
        /// Opens a record's document and checks its row of the inventory as a user reads it: the Unit Label in its
        /// properties, the first and last dates of its table, and the Record Length and Missing Values of its summary
        /// statistics. Then closes the document.
        /// </summary>
        internal static void AssertRecord(WalkthroughSession session, Record record)
        {
            var view = session.OpenDocument<TimeSeriesControl>(record.Element);
            try
            {
                Assert.AreEqual(record.Units, Property(session.Properties, "Unit Label"), record.Element + ": the units");
                (DateTime first, DateTime last) = Dates(view);
                Assert.AreEqual(Day(record.First), first.Date, record.Element + ": the first saved date");
                Assert.AreEqual(Day(record.Last), last.Date, record.Element + ": the last saved date");
                Dictionary<string, string> statistics = SummaryStatistics(view);
                Assert.AreEqual(record.Ordinates.ToString("N0", Us), statistics["Record Length"], record.Element + ": the ordinates");
                Assert.AreEqual(record.Missing.ToString("N0", Us), statistics["Missing Values"], record.Element + ": the missing values");
            }
            finally
            {
                session.CloseDocuments();
            }
        }

        /// <summary>A walkthrough's date (yyyy-MM-dd).</summary>
        internal static DateTime Day(string date) => DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>
        /// The view's tabs are the walkthroughs': Time Series (the Time Series Data table and Summary Statistics beside it,
        /// and the plot), then Seasonality Plot, ACF Plot and PACF Plot on the left, and the USGS Text File of a USGS peak
        /// record.
        /// </summary>
        internal static void AssertTabs(TimeSeriesControl view, bool usgsText = false)
        {
            var expected = new List<string> { "Time Series", "Seasonality Plot", "ACF Plot", "PACF Plot" };
            if (usgsText) expected.Add("USGS Text File");
            CollectionAssert.AreEqual(expected, Headers(MainTabs(view)));
            Assert.AreEqual(Avalonia.Controls.Dock.Left, MainTabs(view).TabStripPlacement, "the plots' tabs are on the left");
            CollectionAssert.AreEqual(new[] { "Time Series Data", "Summary Statistics" }, Headers(DataTabs(view)));
            Show(view, "Time Series");
            Assert.AreSame(view.TimeSeriesPlot, view.GetCurrentPlot(), "the Time Series tab's plot");
        }

        /// <summary>
        /// Each figure of a walkthrough (figures/*.plotspec.json.gz beside the example) is what the view of its element shows
        /// on the figure's tab, series by series and point by point.
        /// </summary>
        /// <param name="session">The example's session.</param>
        /// <param name="folder">The example's folder under the examples folder, with a trailing slash.</param>
        /// <returns>The figures compared, by file name.</returns>
        internal static List<string> AssertFigures(WalkthroughSession session, string folder)
        {
            var compared = new List<string>();
            string figures = Path.Combine(SharedControlsTesting.ExamplesFolder, folder, "figures");
            foreach (string file in Directory.GetFiles(figures, "*.plotspec.json.gz").OrderBy(f => f, StringComparer.Ordinal))
            {
                JsonElement figure = Data(folder + "figures/" + Path.GetFileName(file));
                string element = figure.GetProperty("source").GetProperty("id").GetString();
                string tab = figure.GetProperty("plotId").GetString() switch
                {
                    "time_series_data.series" => "Time Series",
                    "time_series_data.seasonality" => "Seasonality Plot",
                    string other => throw new AssertFailedException(file + ": a figure of " + other),
                };
                var view = session.OpenDocument<TimeSeriesControl>(element);
                try
                {
                    Show(view, tab);
                    OxyPlot.Wpf.Plot plot = view.GetCurrentPlot();
                    Assert.AreSame(tab == "Time Series" ? view.TimeSeriesPlot : view.SeasonalityPlot, plot, file);
                    // The figures' titles are a factory-reset plot's; the application shows the plots the project saved.
                    AssertFigureData(plot, figure, Path.GetFileName(file));
                    compared.Add(Path.GetFileName(file));
                }
                finally
                {
                    session.CloseDocuments();
                }
            }
            return compared;
        }

        /// <summary>
        /// The Seasonality Plot groups the observations by month: its horizontal axis is Month, labelled with month names,
        /// and each line has a point for each month.
        /// </summary>
        internal static void AssertSeasonality(TimeSeriesControl view)
        {
            Show(view, "Seasonality Plot");
            OxyPlot.Wpf.Plot plot = view.SeasonalityPlot;
            Assert.AreSame(plot, view.GetCurrentPlot());
            var months = (OxyPlot.Wpf.DateTimeAxis)plot.Axes.Single(a => a.Key == "Xaxis");
            Assert.AreEqual("Month", months.Title);
            Assert.AreEqual("MMM", months.StringFormat, "month names");
            var median = plot.Series.Single(s => s.Title == "Median");
            List<(double X, double Y)> points = Points(median.InternalSeries, "ActualPoints");
            CollectionAssert.AreEqual(Enumerable.Range(1, 12).ToList(),
                points.Select(p => DateTime.FromOADate(p.X).Month).ToList(), "a point for each month, in order");
        }

        /// <summary>
        /// Adds a time series as the walkthroughs say: right-click Time Series Data in the explorer, choose New Time
        /// Series..., and enter a name. Its document opens, with its properties beside it.
        /// </summary>
        /// <returns>The new element.</returns>
        internal static TimeSeriesElement NewTimeSeries(WalkthroughSession session, string name)
        {
            ElementNodeCollection collection = session.Node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is TimeSeriesCollection);
            int count = collection.ElementCollection.Count;
            session.Window.RightClick(collection.NodeHeader);
            Assert.IsTrue(collection.ContextMenu!.IsOpen, "a right click opens the collection's menu");
            MenuItem item = collection.ContextMenu.Items.OfType<MenuItem>().Single(m => m.Header as string == "New Time Series...");
            NameDialog dialog = null;
            using (session.Expect<NameDialog>())
            using (BestFitShell.WhenOpened<NameDialog>(opened =>
            {
                dialog = opened;
                opened.Text = name;
                BestFitShell.ClickButton(opened, "OK");
            }))
            {
                BestFitShell.Click(item);
            }
            collection.ContextMenu.Close();
            BestFitShell.Frame();
            Assert.IsNotNull(dialog, "New Time Series... asks for a name");
            Assert.AreEqual(count + 1, collection.ElementCollection.Count, "a time series is added");
            var added = (TimeSeriesElement)collection.ElementCollection[count];
            Assert.AreEqual(name, added.Name);
            session.OpenDocument<TimeSeriesControl>(added);
            Assert.AreSame(added, ((TimeSeriesPropertiesControl)session.Properties).Element, "its properties show");
            return added;
        }

        /// <summary>Picks the item of a properties row's combo box that shows <paramref name="text"/>, as a user does.</summary>
        internal static void Choose(ContentPropertyControl row, string text)
        {
            var box = (ComboBox)row.InnerContent;
            object item = box.Items.Cast<object>().SingleOrDefault(i => (i.GetType().GetProperty("DisplayName")?.GetValue(i) as string ?? i.ToString()) == text);
            Assert.IsNotNull(item, row.Title + " offers " + text);
            box.SelectedItem = item;
            BestFitShell.Frame();
        }

        /// <summary>Types <paramref name="text"/> into a properties row's text box.</summary>
        internal static void Type(Control row, string text)
        {
            TextBox box = row.GetVisualDescendants().OfType<TextBox>().First(b => b.IsEffectivelyVisible);
            box.Focus();
            box.Text = text;
            BestFitShell.Frame();
        }

        /// <summary>
        /// Sets up a new download source as a walkthrough says: a new time series with the source's Data Entry Method, its
        /// site number and data type (and depth unit). The Download button is then ready.
        /// </summary>
        /// <returns>The new element and its properties.</returns>
        internal static (TimeSeriesElement Element, TimeSeriesPropertiesControl Properties) NewDownload(WalkthroughSession session, string name,
            string method, string site, string dataType, string depthUnit = null)
        {
            TimeSeriesElement element = NewTimeSeries(session, name);
            var properties = (TimeSeriesPropertiesControl)session.Properties;
            Assert.AreEqual("Manual Entry", Property(properties, "Data Entry Method"), "a new time series is entered by hand");
            Choose(properties.EntryMethod, method);
            Control siteRow = method switch
            {
                "USGS" => properties.USGSSiteNumber,
                "GHCN" => properties.GHCNSiteNumber,
                "CHMN" => properties.CHMNSiteNumber,
                "ABOM" => properties.ABOMSiteNumber,
                _ => throw new ArgumentException(method),
            };
            Type(siteRow, site);
            Choose(properties.DataType, dataType);
            if (depthUnit != null) Choose(properties.DepthUnitSelector, depthUnit);
            Assert.AreEqual(method, Property(properties, "Data Entry Method"));
            Assert.AreEqual(site, Property(properties, "Site Number"));
            Assert.AreEqual(dataType, Property(properties, "Data Type"));
            if (depthUnit != null) Assert.AreEqual(depthUnit, Property(properties, "Depth Unit"));
            Assert.AreEqual("Download", properties.ProcessDataButtonText.Text);
            Assert.IsTrue(properties.ProcessDataButton.IsEffectivelyEnabled, "Download is ready");
            Assert.IsFalse(element.TimeSeries.Any(o => double.IsFinite(o.Value)), "nothing is downloaded yet: the new series' placeholder");
            return (element, properties);
        }

        /// <summary>
        /// Presses Download (category External: the provider's live service) and waits for the record; any message the
        /// shell shows fails the test.
        /// </summary>
        internal static void Download(WalkthroughSession session, TimeSeriesPropertiesControl properties)
        {
            Click(properties.ProcessDataButton);
            BestFitShell.WaitUntil(() => properties.ProcessDataButton.IsEnabled, TimeSpan.FromMinutes(5), "the download");
            BestFitShell.Frame();
            CollectionAssert.AreEqual(Array.Empty<string>(), session.Dialogs, "no message: " + string.Join(" | ", session.Dialogs));
        }
    }
}
