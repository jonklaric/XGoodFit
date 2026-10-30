using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;

namespace FrameworkUI.Tests;

/// <summary>
/// Keyboard and mouse input for the headless tests, as a user gives it: after each, the jobs it queued run
/// and a frame is rendered, as the docking controls act on their layout.
/// </summary>
internal static class TestInput
{
    // Runs the queued jobs and renders a frame.
    public static void Frame()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // A point of a visual, in its window's coordinates.
    public static Point PointOf(this TopLevel window, Visual visual, Point point) => visual.TranslatePoint(point, window)!.Value;

    public static Point CenterOf(this TopLevel window, Visual visual) =>
        window.PointOf(visual, new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2));

    // Clicks a button at the centre of a visual.
    public static void Click(this TopLevel window, Visual visual, MouseButton button = MouseButton.Left, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        Point at = window.CenterOf(visual);
        window.MouseDown(at, button, modifiers);
        window.MouseUp(at, button, modifiers);
        Frame();
    }

    // Two left clicks in quick succession at the centre of a visual: the second press has a click count of two.
    public static void DoubleClick(this TopLevel window, Visual visual)
    {
        Point at = window.CenterOf(visual);
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Frame();
    }

    // Types text one character at a time, as a keyboard does.
    public static void Type(this TopLevel window, string text)
    {
        foreach (char c in text)
        {
            window.KeyTextInput(c.ToString());
        }
        Frame();
    }

    // Presses and releases a key.
    public static void Press(this TopLevel window, Key key, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        window.KeyPress(key, modifiers, PhysicalKey.None, null);
        window.KeyRelease(key, modifiers, PhysicalKey.None, null);
        Frame();
    }
}
