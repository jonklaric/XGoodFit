namespace WpfCompat;

/// <summary>
/// The WPF <c>System.Windows.SystemParameters</c> values the ported code reads, fixed at the values
/// WPF has on Windows at 100% scaling, so every platform lays out the same.
/// </summary>
/// <remarks>
/// XAML uses the same values as resources (Themes' <c>Merged.axaml</c>, for example
/// <c>SystemParameters.VerticalScrollBarWidth</c>).
/// </remarks>
public static class SystemParameters
{
    /// <summary>The width of a vertical scroll bar: 17, as in Themes' scroll bars.</summary>
    public static double VerticalScrollBarWidth => 17d;

    /// <summary>The height of a horizontal scroll bar: 17, as in Themes' scroll bars.</summary>
    public static double HorizontalScrollBarHeight => 17d;

    /// <summary>How far the pointer moves horizontally before a drag starts: Windows' default of 4.</summary>
    public static double MinimumHorizontalDragDistance => 4d;

    /// <summary>How far the pointer moves vertically before a drag starts: Windows' default of 4.</summary>
    public static double MinimumVerticalDragDistance => 4d;
}
