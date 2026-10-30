using Avalonia.Controls;

namespace WpfCompat;

/// <summary>WPF's one-argument <c>TryFindResource</c>, which Avalonia lacks.</summary>
/// <remarks>
/// Avalonia's own <c>FindResource(key)</c> already searches as WPF's does (up the tree, then the
/// application), but returns <c>AvaloniaProperty.UnsetValue</c> where WPF throws.
/// </remarks>
public static class ResourceExtensions
{
    /// <summary>The resource with <paramref name="key"/>, searching up from <paramref name="host"/>, or null.</summary>
    public static object? TryFindResource(this IResourceHost host, object key) =>
        host.TryFindResource(key, out object? value) ? value : null;
}
