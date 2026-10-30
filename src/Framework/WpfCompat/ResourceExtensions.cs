using Avalonia;
using Avalonia.Controls;

namespace WpfCompat;

/// <summary>WPF's one-argument <c>TryFindResource</c> and <c>SetResourceReference</c>, which Avalonia lacks.</summary>
/// <remarks>
/// Avalonia's own <c>FindResource(key)</c> already searches as WPF's does (up the tree, then the
/// application), but returns <c>AvaloniaProperty.UnsetValue</c> where WPF throws.
/// </remarks>
public static class ResourceExtensions
{
    /// <summary>The resource with <paramref name="key"/>, searching up from <paramref name="host"/>, or null.</summary>
    public static object? TryFindResource(this IResourceHost host, object key) =>
        host.TryFindResource(key, out object? value) ? value : null;

    /// <summary>
    /// Sets <paramref name="property"/> to the resource with <paramref name="key"/>, following it as the
    /// resource changes (a theme switch, say), as WPF's <c>SetResourceReference</c> and XAML's
    /// <c>{DynamicResource}</c> do.
    /// </summary>
    public static IDisposable SetResourceReference(this StyledElement element, AvaloniaProperty property, object key) =>
        element.Bind(property, element.GetResourceObservable(key));
}
