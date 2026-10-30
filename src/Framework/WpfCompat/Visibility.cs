namespace WpfCompat;

/// <summary>
/// Whether an element shows: WPF's <c>System.Windows.Visibility</c>, with the same names and values.
/// </summary>
/// <remarks>
/// XGoodFit addition. Avalonia elements have a boolean <c>IsVisible</c> instead. Ported classes that
/// keep WPF's <c>Visibility</c> property (for example the plot elements in <c>OxyPlot.Wpf</c>, whose
/// value is saved in project files as text) use this enum, so code and saved text stay as in WPF.
/// </remarks>
public enum Visibility : byte
{
    /// <summary>The element shows.</summary>
    Visible = 0,

    /// <summary>The element doesn't show, but keeps its space in layout.</summary>
    Hidden = 1,

    /// <summary>The element doesn't show and takes no space.</summary>
    Collapsed = 2,
}
