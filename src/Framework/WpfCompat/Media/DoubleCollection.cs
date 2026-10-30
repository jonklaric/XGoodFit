using Avalonia.Collections;

namespace WpfCompat.Media;

/// <summary>
/// WPF's <c>System.Windows.Media.DoubleCollection</c>, the type of WPF's dash arrays. It's an Avalonia list
/// of doubles, so it can be a shape's <c>StrokeDashArray</c> as it is.
/// </summary>
/// <remarks>
/// As in WPF, two collections are equal only if they're the same object: a combo box of dash arrays
/// finds the selected one among its items by reference.
/// </remarks>
public sealed class DoubleCollection : AvaloniaList<double>
{
    /// <summary>Creates an empty collection.</summary>
    public DoubleCollection()
    {
    }

    /// <summary>Creates an empty collection with room for <paramref name="capacity"/> values.</summary>
    public DoubleCollection(int capacity)
        : base(capacity)
    {
    }

    /// <summary>Creates a collection of <paramref name="collection"/>'s values.</summary>
    public DoubleCollection(IEnumerable<double> collection)
        : base(collection)
    {
    }

    /// <summary>A copy of the collection, as WPF's <c>Clone()</c>.</summary>
    public DoubleCollection Clone() => new(this);
}
