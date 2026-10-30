namespace WpfCompat;

/// <summary>
/// How a window can be resized: WPF's <c>System.Windows.ResizeMode</c>. Avalonia windows express the
/// same with <c>CanResize</c>, <c>CanMinimize</c> and <c>CanMaximize</c>; see <see cref="ResizeModeExtensions"/>.
/// </summary>
public enum ResizeMode
{
    /// <summary>The window can't be resized, minimized or maximized.</summary>
    NoResize = 0,

    /// <summary>The window can only be minimized and restored.</summary>
    CanMinimize = 1,

    /// <summary>The window can be resized, minimized and maximized.</summary>
    CanResize = 2,

    /// <summary>As <see cref="CanResize"/>, with a resize grip in the bottom-right corner.</summary>
    CanResizeWithGrip = 3,
}

/// <summary>What a <see cref="ResizeMode"/> allows, in Avalonia's terms.</summary>
public static class ResizeModeExtensions
{
    /// <summary>Whether the user can resize the window by dragging its edges.</summary>
    public static bool CanResize(this ResizeMode mode) => mode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip;

    /// <summary>Whether the window can be minimized.</summary>
    public static bool CanMinimize(this ResizeMode mode) => mode != ResizeMode.NoResize;

    /// <summary>Whether the window can be maximized.</summary>
    public static bool CanMaximize(this ResizeMode mode) => mode.CanResize();
}
