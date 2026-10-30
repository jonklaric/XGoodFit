using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace GenericControls.Tests;

/// <summary>Keyboard and mouse input for the headless tests, as a user gives it.</summary>
internal static class TestInput
{
    // Shows control in a window with the app's resources, above a text box that focus can move to.
    public static Window Show(Control control, out TextBox elsewhere, double width = 400, double height = 300)
    {
        PropertyControlTests.UseAppResources();
        elsewhere = new TextBox();
        var window = new Window { Width = width, Height = height, Content = new StackPanel { Children = { control, elsewhere } } };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    // Types text one character at a time, as a keyboard does.
    public static void Type(this Window window, string text)
    {
        foreach (char c in text)
        {
            window.KeyTextInput(c.ToString());
        }
    }

    // Presses and releases a key.
    public static void Press(this Window window, Key key)
    {
        window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
    }

    // A point of a visual, in its window's coordinates.
    public static Point PointOf(this Window window, Visual visual, Point point) => visual.TranslatePoint(point, window)!.Value;

    public static Point CenterOf(this Window window, Visual visual) =>
        window.PointOf(visual, new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2));

    // Clicks the left button at a point of a visual (its centre by default).
    public static void Click(this Window window, Visual visual, Point? point = null)
    {
        Point at = point is { } p ? window.PointOf(visual, p) : window.CenterOf(visual);
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
    }
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
