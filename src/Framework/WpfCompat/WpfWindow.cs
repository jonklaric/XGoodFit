using Avalonia;
using Avalonia.Controls;

namespace WpfCompat;

/// <summary>
/// WPF <c>Window</c> properties that Avalonia's <see cref="Window"/> lacks, as attached properties so
/// that themes can set them: <c>wpf:WpfWindow.ResizeMode="NoResize"</c>. A window class can also take
/// them on as its own properties with <c>AddOwner</c>.
/// </summary>
public static class WpfWindow
{
    /// <summary>
    /// How the window can be resized, as WPF's <c>Window.ResizeMode</c>. Setting it sets the window's
    /// <see cref="Window.CanResize"/>, <see cref="Window.CanMinimize"/> and <see cref="Window.CanMaximize"/>.
    /// WPF's default is <see cref="ResizeMode.CanResize"/>, which matches Avalonia's defaults.
    /// </summary>
    public static readonly AttachedProperty<ResizeMode> ResizeModeProperty =
        AvaloniaProperty.RegisterAttached<Window, ResizeMode>("ResizeMode", typeof(WpfWindow), ResizeMode.CanResize);

    static WpfWindow()
    {
        ResizeModeProperty.Changed.AddClassHandler<Window>((window, e) => Apply(window, e.GetNewValue<ResizeMode>()));
    }

    /// <summary>Gets how <paramref name="window"/> can be resized.</summary>
    public static ResizeMode GetResizeMode(Window window) => window.GetValue(ResizeModeProperty);

    /// <summary>Sets how <paramref name="window"/> can be resized.</summary>
    public static void SetResizeMode(Window window, ResizeMode value) => window.SetValue(ResizeModeProperty, value);

    private static void Apply(Window window, ResizeMode mode)
    {
        window.CanResize = mode.CanResize();
        window.CanMinimize = mode.CanMinimize();
        window.CanMaximize = mode.CanMaximize();
    }
}
