using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GenericControls;
using Themes;

namespace NumericControls.Tests;

/// <summary>Showing the controls in a window with the app's resources, and driving them as a user does.</summary>
internal static class TestSupport
{
    private static readonly Uri[] Themes =
    [
        new("avares://GenericControls/Themes/GenericControlsTheme.axaml"),
        new("avares://NumericControls/Themes/NumericControlsTheme.axaml"),
    ];

    /// <summary>
    /// The app's resources, merged as upstream's App merges them: GenericControls' theme, then
    /// NumericControls', then Themes' (ThemeService adds those after them).
    /// </summary>
    public static void UseAppResources(Theme theme = Theme.Light)
    {
        // Avalonia finds a dictionary's compiled XAML in the loaded assemblies: load NumericControls.
        _ = typeof(ProbabilityOrdinatesControl).Assembly;
        var merged = Application.Current!.Resources.MergedDictionaries;
        for (int i = 0; i < Themes.Length; i++)
        {
            if (!merged.OfType<ResourceInclude>().Any(r => r.Source == Themes[i]))
            {
                merged.Insert(i, new ResourceInclude(new Uri("avares://NumericControls.Tests")) { Source = Themes[i] });
            }
        }
        ThemeService.Instance.Initialize(theme);
        ThemeService.Instance.SetTheme(theme);
    }

    /// <summary>Shows <paramref name="content"/> in a focused window and lays it out.</summary>
    public static Window Show(Control content, double width = 500, double height = 400)
    {
        UseAppResources();
        // The clipboard belongs to the window with the focus, which WpfCompat's command manager tracks.
        WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        window.Activate();
        Settle();
        return window;
    }

    /// <summary>Runs the jobs the dispatcher has, so layout and posted work are done.</summary>
    public static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>Selects the cells (row, column display index) of <paramref name="grid"/>.</summary>
    public static void Select(DataGrid grid, params (int Row, int Column)[] cells)
    {
        var items = grid.CollectionView.Cast<object>().ToList();
        grid.SelectedCells = cells
            .Select(c =>
            {
                DataGridColumn column = grid.Columns.Single(col => col.DisplayIndex == c.Column);
                return new DataGridCellInfo(items[c.Row], column, c.Row, grid.Columns.IndexOf(column));
            })
            .ToList();
        Settle();
    }

    /// <summary>The realized cell of <paramref name="grid"/> at (row, column display index).</summary>
    public static DataGridCell Cell(DataGrid grid, int row, int column) =>
        grid.GetVisualDescendants().OfType<DataGridCell>()
            .Single(c => c.OwningRow is { } r && r.IsVisible && r.Index == row && c.OwningColumn?.DisplayIndex == column && grid.Columns.Contains(c.OwningColumn));

    /// <summary>The text a cell shows.</summary>
    public static string CellText(DataGrid grid, int row, int column) =>
        Cell(grid, row, column).GetVisualDescendants().OfType<TextBlock>().FirstOrDefault()?.Text ?? "";

    /// <summary>Clicks <paramref name="button"/>, as a user would.</summary>
    public static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Settle();
    }

    /// <summary>Types text one character at a time, as a keyboard does.</summary>
    public static void Type(this Window window, string text)
    {
        foreach (char c in text)
        {
            window.KeyTextInput(c.ToString());
        }
    }

    /// <summary>Presses and releases a key.</summary>
    public static void Press(this Window window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Settle();
    }

    /// <summary>A point of a visual, in its window's coordinates.</summary>
    public static Point PointOf(this Window window, Visual visual, Point point) => visual.TranslatePoint(point, window)!.Value;

    /// <summary>Clicks the left button at a point of a visual (its centre by default).</summary>
    public static void ClickAt(this Window window, Visual visual, Point? point = null, MouseButton button = MouseButton.Left)
    {
        Point at = window.PointOf(visual, point ?? new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2));
        window.MouseDown(at, button);
        window.MouseUp(at, button);
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

    /// <summary>Answers the next message box with its first button, and gives back its text.</summary>
    /// <remarks>The window's named elements are internal to GenericControls: they're found by name.</remarks>
    public static IDisposable AnswerMessageBox(Action<string> message) =>
        WhenOpened<MessageBoxWindow>(window =>
        {
            message(window.FindControl<TextBlock>("MessageText")?.Text ?? "");
            window.FindControl<StackPanel>("ButtonPanel")!.Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
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
