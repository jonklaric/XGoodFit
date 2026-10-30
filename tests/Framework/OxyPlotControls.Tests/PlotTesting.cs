using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using Themes;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

/// <summary>Plots like the demo's and BestFit's, shown in a window with the app's resources.</summary>
internal static class PlotTesting
{
    private static readonly Uri GenericControlsTheme = new("avares://GenericControls/Themes/GenericControlsTheme.axaml");
    private static readonly Uri OxyPlotControlsTheme = new("avares://OxyPlotControls/Themes/OxyPlotControlsTheme.axaml");

    /// <summary>
    /// The app's resources, merged as upstream's App merges them: GenericControls' theme, then
    /// OxyPlotControls', then Themes' (ThemeService adds those after them).
    /// </summary>
    public static void UseAppResources(Theme theme = Theme.Light)
    {
        var merged = Application.Current!.Resources.MergedDictionaries;
        if (!merged.OfType<ResourceInclude>().Any(r => r.Source == GenericControlsTheme))
        {
            merged.Insert(0, new ResourceInclude(new Uri("avares://OxyPlotControls.Tests")) { Source = GenericControlsTheme });
            merged.Insert(1, new ResourceInclude(new Uri("avares://OxyPlotControls.Tests")) { Source = OxyPlotControlsTheme });
        }
        ThemeService.Instance.Initialize(theme);
        ThemeService.Instance.SetTheme(theme);

        // The toolbar asks before a theme change recolours the plot; a test has nobody to answer.
        OxyPlotThemeManager.SuppressThemeChangeWarning = true;
    }

    /// <summary>Shows <paramref name="content"/> in a window and lays it out.</summary>
    public static Window Show(Control content, double width = 1100, double height = 600)
    {
        UseAppResources(ThemeService.Instance.IsInitialized ? ThemeService.Instance.CurrentTheme : Theme.Light);

        // File dialogs belong to the window with the focus, which WpfCompat's command manager tracks
        // (the headless app has no desktop lifetime to ask).
        WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
        var window = new Window { Width = width, Height = height, Content = content };
        window.Show();
        window.Activate();
        Settle();
        return window;
    }

    /// <summary>
    /// Runs the jobs the dispatcher has, so layout and posted work are done, then renders: input finds
    /// its target in the rendered scene, which only a render brings up to date.
    /// </summary>
    public static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }

    /// <summary>
    /// Clicks <paramref name="control"/> with the left button, at the point nearest its centre that hits
    /// it: the toolbar's buttons, as upstream's, have no background until the pointer is over their icon.
    /// </summary>
    public static void Click(Window window, Control control)
    {
        Settle();
        Point point = HitPoint(window, control);
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);
        Settle();
    }

    private static Point HitPoint(Window window, Control control)
    {
        Point centre = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        var candidates = new List<Point>();
        for (double dx = -control.Bounds.Width / 2 + 1; dx < control.Bounds.Width / 2; dx++)
        {
            for (double dy = -control.Bounds.Height / 2 + 1; dy < control.Bounds.Height / 2; dy++)
            {
                candidates.Add(centre + new Vector(dx, dy));
            }
        }
        foreach (Point candidate in candidates.OrderBy(p => Point.Distance(p, centre)))
        {
            if (window.InputHitTest(candidate) is Visual hit && (hit == control || hit.GetVisualAncestors().Contains(control)))
            {
                return candidate;
            }
        }
        return centre;
    }

    /// <summary>Drags with <paramref name="button"/> from <paramref name="from"/> to <paramref name="to"/>, in window coordinates.</summary>
    public static void Drag(Window window, Point from, Point to, MouseButton button = MouseButton.Left)
    {
        RawInputModifiers held = button switch
        {
            MouseButton.Right => RawInputModifiers.RightMouseButton,
            MouseButton.Middle => RawInputModifiers.MiddleMouseButton,
            _ => RawInputModifiers.LeftMouseButton,
        };
        window.MouseMove(from);
        window.MouseDown(from, button);
        for (int step = 1; step <= 5; step++)
        {
            window.MouseMove(from + (to - from) * step / 5, held);
            Dispatcher.UIThread.RunJobs();
        }
        window.MouseUp(to, button);
        Settle();
    }

    /// <summary>A point of the plot area (fractions of its width and height from its top left), in window coordinates.</summary>
    public static Point PlotAreaPoint(Window window, Wpf.Plot plot, double x, double y)
    {
        OxyRect area = plot.ActualModel.PlotArea;
        return plot.TranslatePoint(new Point(area.Left + area.Width * x, area.Top + area.Height * y), window)!.Value;
    }

    /// <summary>The data point under a window point, on the plot's first two axes (bottom, left).</summary>
    public static DataPoint DataPointAt(Window window, Wpf.Plot plot, Point point)
    {
        Point local = window.TranslatePoint(point, plot)!.Value;
        var x = plot.ActualModel.Axes.Single(a => a.Position == AxisPosition.Bottom);
        var y = plot.ActualModel.Axes.Single(a => a.Position == AxisPosition.Left);
        return new DataPoint(x.InverseTransform(local.X), y.InverseTransform(local.Y));
    }

    /// <summary>Runs <paramref name="answer"/> on the next window of type T that opens, once it's ready (see GenericControls.Tests).</summary>
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

    /// <summary>
    /// Closes any message box that opens, keeping its text: a failed save shows one, which would otherwise
    /// wait for a click forever.
    /// </summary>
    public static IDisposable CloseMessageBoxes(List<string> messages) =>
        Window.WindowOpenedEvent.AddClassHandler<GenericControls.MessageBoxWindow>((window, _) =>
            Dispatcher.UIThread.Post(() =>
            {
                messages.Add(window.FindControl<TextBlock>("MessageText")?.Text ?? string.Empty);
                window.Close();
            }, DispatcherPriority.Background));

    /// <summary>Raises a button's or menu item's Click, as a click would.</summary>
    public static void RaiseClick(Control control) =>
        control.RaiseEvent(new RoutedEventArgs(control is MenuItem ? MenuItem.ClickEvent : Avalonia.Controls.Button.ClickEvent));

    /// <summary>
    /// The demo's line series plot (upstream's OxyPlotControls.Demo, "Line Series"): two normal densities
    /// on linear axes keyed "x" and "y".
    /// </summary>
    public static Wpf.Plot LinePlot()
    {
        var plot = new Wpf.Plot
        {
            Title = "Line Series",
            IsLegendVisible = true,
            LegendPosition = LegendPosition.RightBottom,
            Margin = new Thickness(5),
        };
        plot.Axes.Add(new Wpf.LinearAxis
        {
            AxisTitleDistance = 20,
            Position = AxisPosition.Bottom,
            TitleFontSize = 20,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dash,
            Title = "X",
            Key = "x",
        });
        plot.Axes.Add(new Wpf.LinearAxis
        {
            AxisTitleDistance = 20,
            TitleFontSize = 20,
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
            MinorGridlineStyle = LineStyle.Dash,
            Title = "Y",
            Key = "y",
        });
        plot.Series.Add(new Wpf.LineSeries { Title = "Test Line Series 1", ItemsSource = NormalDensity(-5, 5, 0, 1) });
        plot.Series.Add(new Wpf.LineSeries { Title = "Test Line Series 2", ItemsSource = NormalDensity(-2, 2, 0, 0.25) });
        return plot;
    }

    /// <summary>The normal density from <paramref name="from"/> to <paramref name="to"/>, as the demo draws it.</summary>
    public static List<DataPoint> NormalDensity(double from, double to, double mean, double variance)
    {
        var points = new List<DataPoint>();
        for (int i = 0; i <= 100; i++)
        {
            double x = from + (to - from) * i / 100;
            points.Add(new DataPoint(x, Math.Exp(-(x - mean) * (x - mean) / (2 * variance)) / Math.Sqrt(2 * Math.PI * variance)));
        }
        return points;
    }

    /// <summary>
    /// A window as the demo's: the plot with its toolbar on the right, and the properties control in a
    /// 280-pixel column beside them.
    /// </summary>
    public static (Window Window, Wpf.Plot Plot, OxyPlotToolbar Toolbar, OxyPlotPropertiesControl Properties) ShowDemo(
        Wpf.Plot? plot = null, double width = 1100, double height = 600)
    {
        plot ??= LinePlot();
        var toolbar = new OxyPlotToolbar
        {
            Plot = plot,
            ToolBarOrientation = Avalonia.Layout.Orientation.Vertical,
            Margin = new Thickness(4),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };
        Grid.SetColumn(toolbar, 1);
        var properties = new OxyPlotPropertiesControl { Plot = plot, ShowCloseButton = true, Margin = new Thickness(5, 0) };
        Grid.SetColumn(properties, 1);
        var plotHost = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { plot, toolbar } };
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("*,280"), Children = { plotHost, properties } };
        Window window = Show(root, width, height);
        return (window, plot, toolbar, properties);
    }
}
