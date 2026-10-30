using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Themes;
using WpfCompat;

namespace GenericControls.Tests;

/// <summary>The rows the grid tests show: plain objects, as most of upstream's grids show.</summary>
public sealed class GridRow
{
    /// <summary>A text value.</summary>
    public string Name { get; set; } = "";

    /// <summary>A double value.</summary>
    public double Value { get; set; }

    /// <summary>An integer value.</summary>
    public int Count { get; set; }
}

/// <summary>A row with validation rules: unique names, positive values that increase down the table.</summary>
public sealed class RuleRow : DataGridRowItem
{
    private string _name = "";
    private double _value;

    /// <summary>Creates a row of <paramref name="list"/>, shown by <paramref name="grid"/>.</summary>
    public RuleRow(ObservableCollection<object> list, ValidationDataGrid grid) : base(list, grid)
    {
    }

    /// <summary>Creates a row the grid adds (it gives the row its list).</summary>
    public RuleRow() : base(null)
    {
    }

    /// <summary>The row's name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>The row's value.</summary>
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public override void AddValidationRules()
    {
        AddRule(nameof(Name), () => UniqueRule(nameof(Name), "Names must be unique."), "Names must be unique.");
        AddRule(nameof(Value), () => Value <= 0, "Values must be positive.");
        AddRule(nameof(Value), () => OrderRule<double, RuleRow>(row => row.Value, nameof(Value)), "Values must increase.");
    }

    /// <inheritdoc/>
    public override string PropertyDisplayName(string propertyName) => propertyName == nameof(Value) ? "Value (cfs)" : propertyName;

    /// <inheritdoc/>
    public override bool IsGridDisplayable(string propertyName) => propertyName != nameof(RuleMap);
}

/// <summary>Showing grids in a window with the app's resources, and driving them.</summary>
internal static class GridTesting
{
    private static readonly Uri GenericControlsTheme = new("avares://GenericControls/Themes/GenericControlsTheme.axaml");

    /// <summary>
    /// The app's resources, merged as upstream's App merges them: GenericControls' theme, then Themes'
    /// (ThemeService adds those after it).
    /// </summary>
    public static void UseAppResources()
    {
        var merged = Application.Current!.Resources.MergedDictionaries;
        if (!merged.OfType<ResourceInclude>().Any(r => r.Source == GenericControlsTheme))
        {
            merged.Insert(0, new ResourceInclude(new Uri("avares://GenericControls.Tests")) { Source = GenericControlsTheme });
        }
        ThemeService.Instance.Initialize(Theme.Light);
        ThemeService.Instance.SetTheme(Theme.Light);
    }

    /// <summary>Shows <paramref name="content"/> in a window and lays it out.</summary>
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

    /// <summary>The plain rows the grid tests start with.</summary>
    public static ObservableCollection<GridRow> Rows(int count = 5) =>
        new(Enumerable.Range(1, count).Select(i => new GridRow { Name = "Item " + i, Value = i * 10.5, Count = i }));

    /// <summary>A copy-paste grid of <paramref name="rows"/>, shown and focused.</summary>
    public static (Window Window, CopyPasteDataGrid Grid) ShowGrid(ObservableCollection<GridRow> rows, Action<CopyPasteDataGrid>? configure = null)
    {
        var grid = new CopyPasteDataGrid { RowType = typeof(GridRow) };
        configure?.Invoke(grid);
        grid.ItemsSource = rows;
        var window = Show(grid);
        grid.Focus();
        Settle();
        return (window, grid);
    }

    /// <summary>Selects the cells (row, column) of <paramref name="grid"/>, by display index.</summary>
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

    /// <summary>Selects the block of cells from (row, column) to (row, column), inclusive.</summary>
    public static void SelectBlock(DataGrid grid, int fromRow, int fromColumn, int toRow, int toColumn) =>
        Select(grid, (from r in Enumerable.Range(fromRow, toRow - fromRow + 1)
                      from c in Enumerable.Range(fromColumn, toColumn - fromColumn + 1)
                      select (r, c)).ToArray());

    /// <summary>The realized cell of <paramref name="grid"/> at (row, column display index).</summary>
    public static DataGridCell Cell(DataGrid grid, int row, int column) =>
        grid.GetVisualDescendants().OfType<DataGridCell>()
            .Single(c => c.OwningRow is { } r && r.IsVisible && r.Index == row && c.OwningColumn?.DisplayIndex == column && c.OwningColumn.IsVisible && grid.Columns.Contains(c.OwningColumn));

    /// <summary>Clicks <paramref name="button"/>, as a user would.</summary>
    public static void Click(Button button)
    {
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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

    /// <summary>Answers the next message box with OK, and gives back its text.</summary>
    public static IDisposable AnswerMessageBox(Action<string> message) =>
        WhenOpened<MessageBoxWindow>(window =>
        {
            message(window.MessageText.Text ?? "");
            window.ButtonPanel.Children.OfType<Button>().First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        });
}
