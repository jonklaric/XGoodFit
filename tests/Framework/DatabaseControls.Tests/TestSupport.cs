using System.Data;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DatabaseManager;

namespace DatabaseControls.Tests;

/// <summary>Tables, viewers, input and dialogs for the tests.</summary>
internal static class TestSupport
{
    /// <summary>
    /// A table of <paramref name="rows"/> rows: Name (text, "Item 0", "Item 1", ...), Value (double, i * 1.5)
    /// and Count (int, i % 4).
    /// </summary>
    public static DataTable SampleTable(int rows, string name = "Sample")
    {
        var table = new DataTable(name);
        table.Columns.Add(new DataColumn("Name", typeof(string)));
        table.Columns.Add(new DataColumn("Value", typeof(double)));
        table.Columns.Add(new DataColumn("Count", typeof(int)));
        for (int i = 0; i < rows; i++)
        {
            table.Rows.Add($"Item {i}", i * 1.5, i % 4);
        }
        return table;
    }

    /// <summary>The table's view through DatabaseManager's in-memory reader, as RMC-BestFit's parameter sets.</summary>
    public static DataTableView InMemory(DataTable table) => new InMemoryReader(table).GetTableManager(table.TableName);

    /// <summary>Shows a table viewer in a window of the given size, lets it lay out, and returns both.</summary>
    public static (TableViewer Viewer, Window Window) ShowViewer(DataTableView? view, double width = 600, double height = 400, Action<TableViewer>? configure = null)
    {
        var viewer = new TableViewer();
        configure?.Invoke(viewer);
        var window = new Window { Width = width, Height = height, Content = viewer };
        window.Show();
        Settle();
        if (view != null)
        {
            viewer.DataView = view;
            Settle();
        }
        return (viewer, window);
    }

    /// <summary>Runs the dispatcher's queue, so posted work and layout finish.</summary>
    public static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>The number of rows the viewer shows.</summary>
    public static int VisibleRowCount(this TableViewer viewer) => viewer.GridPanel.RowDefinitions.Count;

    /// <summary>The text of a cell the viewer shows, by its place in the view.</summary>
    public static string CellText(this TableViewer viewer, int viewRow, int column) =>
        CellBlock(viewer, viewRow, column).Text ?? "";

    /// <summary>The text block of a cell the viewer shows.</summary>
    public static TextBlock CellBlock(this TableViewer viewer, int viewRow, int column) =>
        (TextBlock)((Border)viewer.GridPanel.Children[viewRow * viewer.DataView.ColumnNames.Length + column]).Child!;

    /// <summary>The background of a cell the viewer shows.</summary>
    public static Avalonia.Media.IBrush? CellBackground(this TableViewer viewer, int viewRow, int column) =>
        ((Border)viewer.GridPanel.Children[viewRow * viewer.DataView.ColumnNames.Length + column]).Background;

    /// <summary>The column headers' texts.</summary>
    public static string[] HeaderTexts(this TableViewer viewer) =>
        viewer.ColumnHeadersGrid.Children.OfType<Border>()
            .Select(b => b.GetVisualDescendants().OfType<TextBlock>().Single().Text ?? "").ToArray();

    /// <summary>The row headers' texts.</summary>
    public static string[] RowHeaderTexts(this TableViewer viewer) =>
        viewer.RowHeadersGrid.Children.OfType<Border>().Select(b => ((TextBlock)b.Child!).Text ?? "").ToArray();

    /// <summary>The centre of a cell, in the window's coordinates.</summary>
    public static Point CellCentre(this TableViewer viewer, int viewRow, int column)
    {
        var cell = (Visual)viewer.GridPanel.Children[viewRow * viewer.DataView.ColumnNames.Length + column];
        return cell.TranslatePoint(new Point(cell.Bounds.Width / 2, cell.Bounds.Height / 2), (Visual)TopLevel.GetTopLevel(viewer)!)!.Value;
    }

    /// <summary>The centre of a visual, in its window's coordinates.</summary>
    public static Point Centre(this Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), (Visual)TopLevel.GetTopLevel(visual)!)!.Value;

    /// <summary>Clicks the left button at a point of the window.</summary>
    public static void Click(this Window window, Point at, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.MouseDown(at, MouseButton.Left, modifiers);
        window.MouseUp(at, MouseButton.Left, modifiers);
        Settle();
    }

    /// <summary>Double-clicks the left button at a point of the window.</summary>
    public static void DoubleClick(this Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Settle();
    }

    /// <summary>Clicks the right button at a point of the window.</summary>
    public static void RightClick(this Window window, Point at)
    {
        window.MouseDown(at, MouseButton.Right);
        window.MouseUp(at, MouseButton.Right);
        Settle();
    }

    /// <summary>Presses and releases a key.</summary>
    public static void Press(this Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Settle();
    }

    /// <summary>Types text as a keyboard does: each character's key goes down, and its text comes.</summary>
    public static void Type(this Window window, string text)
    {
        foreach (char c in text)
        {
            Key key = char.IsDigit(c) ? Key.D0 + (c - '0')
                : char.IsLetter(c) ? Key.A + (char.ToUpperInvariant(c) - 'A')
                : c == '.' ? Key.OemPeriod
                : c == '-' ? Key.OemMinus
                : c == ' ' ? Key.Space
                : Key.OemQuestion;
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, c.ToString());
            window.KeyTextInput(c.ToString());
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, c.ToString());
        }
        Settle();
    }

    /// <summary>Runs <paramref name="answer"/> on the next window of type T that opens, once it's ready.</summary>
    public static IDisposable WhenOpened<T>(Action<T> answer) where T : Window
    {
        IDisposable? handler = null;
        handler = Window.WindowOpenedEvent.AddClassHandler<T>((window, _) =>
        {
            handler?.Dispose();
            Dispatcher.UIThread.Post(() => answer(window), DispatcherPriority.Background);
        });
        return handler;
    }

    /// <summary>Clicks a button as the user does, raising its Click event.</summary>
    public static void Click(this Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

    /// <summary>The button with the given content in a window.</summary>
    public static Button ButtonWithContent(this Window window, string content) =>
        window.GetVisualDescendants().OfType<Button>().Single(b => Equals(b.Content, content));
}

/// <summary>Sets the thread's culture for a test, and restores it.</summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
    private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

    public CultureScope(string name)
    {
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _culture;
        CultureInfo.CurrentUICulture = _uiCulture;
    }
}
