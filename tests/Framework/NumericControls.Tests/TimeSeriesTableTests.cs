using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GenericControls;
using Numerics.Data;
using WpfCompat;
using static NumericControls.Tests.TestSupport;

namespace NumericControls.Tests;

/// <summary>
/// TimeSeriesTable, as upstream implements it: a validated date and value grid over a TimeSeries, with rows
/// added and deleted in the series' interval, Delete clearing values, and math functions from the context
/// menu and the toolbar's popup. The series come from upstream's NumericControls.Demo.
/// </summary>
[TestClass]
public sealed class TimeSeriesTableTests
{
    [TestMethod]
    public void RegularSeries_ShowsReadOnlyDates_InTheCurrentCulture_AndValuesInEnglish()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            // The demo's daily series.
            var (window, table, grid) = ShowTable(new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), new DateTime(1980, 8, 30), 10));

            Assert.AreEqual(32, grid.Items.Count);
            Assert.IsFalse(grid.Columns[0].IsVisible, "no date selectors for a regular series");
            Assert.IsTrue(grid.Columns[1].IsReadOnly, "a regular series' dates follow its interval");
            Assert.AreSame(table.Resources["DateTime_CellStyleDisabled"], grid.Columns[1].CellTheme);
            // The culture's own short date and time: ICU (macOS, Linux) puts a narrow no-break space
            // before AM, Windows a plain space.
            Assert.AreEqual(ShortDateTime(new DateTime(1980, 7, 30)), CellText(grid, 0, 1));
            Assert.AreEqual(ShortDateTime(new DateTime(1980, 7, 31)), CellText(grid, 1, 1));
            StringAssert.StartsWith(CellText(grid, 0, 1), "7/30/1980 12:00");
            Assert.AreEqual("10", CellText(grid, 0, 2));
            CollectionAssert.AreEqual(new object[] { "X Data", "Y Data" }, new[] { grid.Columns[1].Header, grid.Columns[2].Header });
            table.XColumnHeader = "Date";
            table.YColumnHeader = "Data";
            CollectionAssert.AreEqual(new object[] { "Date", "Data" }, new[] { grid.Columns[1].Header, grid.Columns[2].Header });
            window.Close();
        });
        HeadlessSession.Run(() =>
        {
            // As upstream: DateToStringConverter formats with the current culture, while the values convert with
            // WPF's binding culture (en-US).
            using var culture = new CultureScope("de-DE");
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1234.5, double.NaN, double.PositiveInfinity]);
            var (window, _, grid) = ShowTable(series);
            Assert.AreEqual("30.07.1980 00:00", CellText(grid, 0, 1));
            Assert.AreEqual("1234.5", CellText(grid, 0, 2));
            Assert.AreEqual("N/A", CellText(grid, 1, 2), "DoubleToNAConverter");
            Assert.AreEqual("+∞", CellText(grid, 2, 2));
            window.Close();
        });
    }

    [TestMethod]
    public void IrregularSeries_EditsDates_AndFlagsThemOutOfOrder()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            // The demo's irregular series.
            var series = new TimeSeries(TimeInterval.Irregular);
            series.Add(new SeriesOrdinate<DateTime, double>(new DateTime(1980, 7, 30), 0));
            series.Add(new SeriesOrdinate<DateTime, double>(new DateTime(2020, 1, 1), 40));
            var (window, table, grid) = ShowTable(series);

            Assert.IsTrue(grid.Columns[0].IsVisible, "the date selectors show for an irregular series");
            Assert.IsFalse(grid.Columns[1].IsReadOnly);
            Assert.AreSame(table.Resources["DateTime_CellStyle"], grid.Columns[1].CellTheme);
            Assert.IsInstanceOfType<RadioButton>(Cell(grid, 0, 0).GetVisualDescendants().OfType<RadioButton>().Single());

            EditCell(window, grid, 1, 1, "1/1/1970 12:00 AM");
            Assert.AreEqual(new DateTime(1970, 1, 1), series[1].Index);
            DataGridCell date = Cell(grid, 1, 1);
            Assert.AreEqual(grid.ErrorCellBackgroundBrush, date.Background, "the date is before the one above it");
            Assert.AreEqual("Date/time values must be in ascending data order. Grid sorting does not reorder the time series used by the plot.", ToolTip.GetTip(date));
            Assert.AreNotEqual(grid.ErrorCellBackgroundBrush, Cell(grid, 0, 1).Background);
            window.Close();
        });
    }

    [TestMethod]
    public void DatesShownWithoutSeconds_KeepThem()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            // The date column shows minutes; rebuilding the rows mustn't write that back into the series (see
            // GenericControls' DataGridDisplayBinding).
            var series = new TimeSeries(TimeInterval.Irregular);
            series.Add(new SeriesOrdinate<DateTime, double>(new DateTime(1980, 7, 30, 6, 15, 42), 1));
            series.Add(new SeriesOrdinate<DateTime, double>(new DateTime(1980, 7, 31, 8, 20, 17), 2));
            var (window, _, grid) = ShowTable(series);
            var changes = Record(series);
            Assert.AreEqual(ShortDateTime(new DateTime(1980, 7, 30, 6, 15, 0)), CellText(grid, 0, 1));
            StringAssert.StartsWith(CellText(grid, 0, 1), "7/30/1980 6:15");
            series.RaiseCollectionChangedReset();
            Settle();
            Assert.AreEqual(new DateTime(1980, 7, 30, 6, 15, 42), series[0].Index);
            Assert.AreEqual(new DateTime(1980, 7, 31, 8, 20, 17), series[1].Index);
            CollectionAssert.AreEqual(new[] { NotifyCollectionChangedAction.Reset }, changes, "no replacements");
            window.Close();
        });
    }

    [TestMethod]
    public void EditingAValue_ReplacesTheOrdinate()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, _, grid) = ShowTable(series);
            SeriesOrdinate<DateTime, double> before = series[1];
            var changes = Record(series);

            EditCell(window, grid, 1, 2, "25.5");

            Assert.AreEqual(25.5, series[1].Value);
            Assert.AreNotSame(before, series[1], "the row clones the ordinate and replaces it, for undo");
            Assert.AreEqual(2, before.Value);
            CollectionAssert.AreEqual(new[] { NotifyCollectionChangedAction.Replace }, changes);
            Assert.AreEqual("25.5", CellText(grid, 1, 2));
            window.Close();
        });
    }

    [TestMethod]
    public void DeleteKey_ClearsTheSelectedValues()
    {
        HeadlessSession.Run(() =>
        {
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, _, grid) = ShowTable(series);

            Select(grid, (0, 2), (1, 2), (1, 1));
            window.Press(Key.Delete);

            Assert.IsTrue(double.IsNaN(series[0].Value));
            Assert.IsTrue(double.IsNaN(series[1].Value));
            Assert.AreEqual(3, series[2].Value);
            Assert.AreEqual(new DateTime(1980, 7, 31), series[1].Index, "a read-only date column is left alone");
            Assert.AreEqual("N/A", CellText(grid, 0, 2));
            window.Close();
        });
    }

    [TestMethod]
    public void Paste_WritesTheValues_BetweenTheConsumersEvents()
    {
        HeadlessSession.Run(() =>
        {
            using var culture = new CultureScope("en-US");
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, table, grid) = ShowTable(series);
            var events = new List<string>();
            table.PreviewPasteData += (string[][] data, ref bool cancel) => events.Add("preview " + data.Length);
            table.DataPasted += () => events.Add("pasted");

            Select(grid, (1, 2));
            Clipboard.SetText("5\r\n6\r\n");
            grid.PasteClipboard();

            CollectionAssert.AreEqual(new[] { 1.0, 5, 6 }, series.Select(o => o.Value).ToArray());
            CollectionAssert.AreEqual(new[] { "preview 2", "pasted" }, events);
            Assert.AreEqual("6", CellText(grid, 2, 2));
            window.Close();
        });
    }

    [TestMethod]
    public void AddingAndDeletingRows_EditsTheSeries_InItsInterval()
    {
        HeadlessSession.Run(() =>
        {
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, table, grid) = ShowTable(series);
            var events = new List<string>();
            table.PreviewAddRows += (int start, int count, ref bool cancel) => events.Add($"preview add {start} {count}");
            table.RowsAdded += (start, count) => events.Add($"added {start} {count}");
            table.PreviewDeleteRows += (List<int> rows, ref bool cancel) => events.Add("preview delete " + string.Join(",", rows));
            table.RowsDeleted += rows => events.Add("deleted " + string.Join(",", rows));

            grid.AddRows(2);
            Assert.AreEqual(5, series.Count);
            Assert.AreEqual(new DateTime(1980, 8, 3), series[4].Index, "the dates follow the interval");
            Assert.IsTrue(double.IsNaN(series[3].Value) && double.IsNaN(series[4].Value), "new values are missing");
            Assert.AreEqual(5, grid.Items.Count);

            // Rows inserted at the top move the start back.
            Select(grid, (0, 2));
            grid.InsertRows();
            Assert.AreEqual(6, series.Count);
            Assert.AreEqual(new DateTime(1980, 7, 29), series[0].Index);
            Assert.IsTrue(double.IsNaN(series[0].Value));
            Assert.AreEqual(1, series[1].Value);

            Select(grid, (1, 2), (2, 2));
            grid.DeleteRows();
            CollectionAssert.AreEqual(new[] { double.NaN, 3, double.NaN, double.NaN }, series.Select(o => o.Value).ToArray());
            Assert.AreEqual(new DateTime(1980, 7, 29), series[0].Index);
            Assert.AreEqual(new DateTime(1980, 7, 30), series[1].Index, "the dates close up from the start");
            CollectionAssert.AreEqual(new[] { "preview add 3 2", "added 3 2", "preview add 0 1", "added 0 1", "preview delete 1,2", "deleted 1,2" }, events);
            window.Close();
        });
    }

    [TestMethod]
    public void MathFunctions_FromTheContextMenu_ApplyToTheSelectedValues()
    {
        HeadlessSession.Run(() =>
        {
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 4]);
            var (window, table, grid) = ShowTable(series);
            var events = new List<string>();
            table.PreviewPasteData += (string[][] data, ref bool cancel) => events.Add("suppress");
            table.DataPasted += () => events.Add("resume");

            MenuItem functions = grid.CustomMenuItems.Single();
            Assert.AreEqual("Math Functions", functions.Header);
            CollectionAssert.AreEqual(
                new[] { "Add", "Subtract", "Multiply", "Divide", "Logarithmic Transform", "Exponentiate", "Replace", "Inverse", "Interpolate" },
                functions.Items.OfType<MenuItem>().Select(m => (string)m.Header!).ToArray(), "the functions with an operand first");

            // A function with an operand asks for it.
            Select(grid, (0, 2), (1, 2));
            string? title = null, label = null;
            using (WhenOpened<NumericEntryDialog>(dialog =>
            {
                title = dialog.Title;
                label = dialog.FindControl<TextBlock>("NameLabel")!.Text;
                dialog.FindControl<NumericTextBox2>("ValueTextBox")!.Value = 3;
                dialog.FindControl<Button>("OKButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }))
            {
                Click(functions, "Multiply");
            }
            Assert.AreEqual("Multiply", title);
            Assert.AreEqual("x * ", label);
            CollectionAssert.AreEqual(new[] { 3.0, 6, 4 }, series.Select(o => o.Value).ToArray());
            CollectionAssert.AreEqual(new[] { "suppress", "resume" }, events);
            Assert.AreEqual(2, grid.SelectedCells.Count, "the cells are selected again");
            Assert.AreEqual("6", CellText(grid, 1, 2));

            // One without an operand applies at once.
            Select(grid, (2, 2));
            Click(functions, "Inverse");
            Assert.AreEqual(0.25, series[2].Value);

            // Dividing by zero is refused.
            Select(grid, (0, 2));
            string? message = null;
            using (WhenOpened<NumericEntryDialog>(dialog =>
            {
                dialog.FindControl<NumericTextBox2>("ValueTextBox")!.Value = 0;
                dialog.FindControl<Button>("OKButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }))
            using (AnswerMessageBox(text => message = text))
            {
                Click(functions, "Divide");
            }
            Assert.AreEqual("Cannot divide by zero.", message);
            Assert.AreEqual(3, series[0].Value);
            window.Close();
        });
    }

    [TestMethod]
    public void ClickingAColumnHeader_SelectsTheColumn()
    {
        HeadlessSession.Run(() =>
        {
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, _, grid) = ShowTable(series);

            DataGridColumnHeader header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => h.OwningColumn == grid.Columns[2]);
            window.ClickAt(header);

            Assert.AreEqual(3, grid.SelectedCells.Count);
            Assert.IsTrue(grid.SelectedCells.All(c => c.Column == grid.Columns[2]));
            Assert.IsTrue(Cell(grid, 2, 2).IsSelected);
            window.Close();
        });
    }

    [TestMethod]
    public void MathPopup_EditsTheSeries()
    {
        HeadlessSession.Run(() =>
        {
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, table, grid) = ShowTable(series);
            var toggle = table.FindControl<ToggleButton>("PresetToggleButton")!;
            var popup = table.FindControl<Popup>("MathPopup")!;

            toggle.IsChecked = true;
            Settle();
            Assert.IsTrue(popup.IsOpen);
            var editor = (MathEditorControl)((Border)popup.Child!).Child!;
            Assert.AreSame(series, editor.Series);
            Assert.AreSame(grid, editor.Source);
            Assert.AreEqual("Applies to all.", editor.FindControl<TextBlock>("NotificationText")!.Text);

            editor.FindControl<NumericTextBox2>("ValueTextBox")!.Value = 10;
            ClickFunction(editor, MathFunctionType.Add);
            CollectionAssert.AreEqual(new[] { 11.0, 12, 13 }, series.Select(o => o.Value).ToArray());
            Assert.AreEqual("13", CellText(grid, 2, 2), "the series' reset rebuilds the rows");
            window.Close();
        });
    }

    [TestMethod]
    public void MathPopup_WithValueCellsSelected_ChangesNothing_AsUpstream()
    {
        HeadlessSession.Run(() =>
        {
            // Upstream's MathEditorControl finds a selected row with TimeSeries.IndexOf on the cell's item, a
            // TimeSeriesRowItem, which gives -1; the series' functions skip that index.
            var series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), [1, 2, 3]);
            var (window, table, grid) = ShowTable(series);
            Select(grid, (1, 2), (2, 2));

            table.FindControl<ToggleButton>("PresetToggleButton")!.IsChecked = true;
            Settle();
            var editor = (MathEditorControl)((Border)table.FindControl<Popup>("MathPopup")!.Child!).Child!;
            Assert.AreEqual("Applies to rows 0, 0.", editor.FindControl<TextBlock>("NotificationText")!.Text);

            editor.FindControl<NumericTextBox2>("ValueTextBox")!.Value = 10;
            ClickFunction(editor, MathFunctionType.Add);
            CollectionAssert.AreEqual(new[] { 1.0, 2, 3 }, series.Select(o => o.Value).ToArray());
            Assert.AreEqual(2, grid.SelectedCells.Count, "the cells are selected again");
            window.Close();
        });
    }

    [TestMethod]
    public void IsReadOnly_IsTheGrids()
    {
        HeadlessSession.Run(() =>
        {
            var (window, table, grid) = ShowTable(new TimeSeries(TimeInterval.OneHour, new DateTime(1980, 7, 30), new DateTime(1980, 8, 1), 15));
            Assert.AreEqual(49, grid.Items.Count, "the demo's hourly series");
            Assert.IsFalse(grid.IsReadOnly);
            table.IsReadOnly = true;
            Assert.IsTrue(grid.IsReadOnly);
            window.Close();
        });
    }

    // The table showing this series, with its grid focused.
    private static (Window Window, TimeSeriesTable Table, ValidationDataGrid Grid) ShowTable(TimeSeries series)
    {
        UseAppResources();
        var table = new TimeSeriesTable { Series = series };
        var window = Show(table, 600, 500);
        var grid = table.GetVisualDescendants().OfType<ValidationDataGrid>().Single();
        grid.Focus();
        Settle();
        return (window, table, grid);
    }

    // Edits a cell as a user does: F2, the text, Enter.
    private static void EditCell(Window window, CopyPasteDataGrid grid, int row, int column, string text)
    {
        grid.Focus();
        grid.CurrentCell = new DataGridCellInfo(grid.Items[row], grid.Columns[column], row, column);
        Settle();
        window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
        Settle();
        var editor = (TextBox)Cell(grid, row, column).Content!;
        editor.SelectAll();
        window.Type(text);
        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Settle();
    }

    // Clicks the math function menu item with this header.
    private static void Click(MenuItem menu, string header)
    {
        menu.Items.OfType<MenuItem>().Single(m => (string)m.Header! == header).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        Settle();
    }

    // Clicks the popup's button for this function.
    private static void ClickFunction(MathEditorControl editor, MathFunctionType function)
    {
        editor.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.DataContext, function)).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    // A date as the table shows it: the current culture's short date and short time.
    private static string ShortDateTime(DateTime date)
    {
        var format = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat;
        return date.ToString($"{format.ShortDatePattern} {format.ShortTimePattern}", System.Globalization.CultureInfo.CurrentCulture);
    }

    // The collection changes the series raises.
    private static List<NotifyCollectionChangedAction> Record(TimeSeries series)
    {
        var changes = new List<NotifyCollectionChangedAction>();
        series.CollectionChanged += (_, e) => changes.Add(e.Action);
        return changes;
    }
}
