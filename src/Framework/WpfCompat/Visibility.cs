using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;

namespace WpfCompat;

/// <summary>
/// Whether an element shows: WPF's <c>System.Windows.Visibility</c>, with the same names and values.
/// </summary>
/// <remarks>
/// Avalonia elements have a boolean <c>IsVisible</c> instead. Ported classes that keep WPF's
/// <c>Visibility</c> property (for example the plot elements in <c>OxyPlot.Wpf</c>, whose value is saved
/// in project files as text) use this enum, so code and saved text stay as in WPF.
/// </remarks>
public enum Visibility : byte
{
    /// <summary>The element shows.</summary>
    Visible = 0,

    /// <summary>The element doesn't show, but still takes up its space.</summary>
    Hidden = 1,

    /// <summary>The element doesn't show and takes up no space.</summary>
    Collapsed = 2,
}

/// <summary>
/// WPF's <c>UIElement.Visibility</c> for Avalonia elements, as an attached property, so a ported binding
/// or converter that produces a <see cref="Visibility"/> keeps working:
/// <c>wpf:WpfElement.Visibility="{Binding …, Converter={StaticResource BooleanToVisibility}}"</c>.
/// Code can use the <c>Visibility</c> extension property (<see cref="VisibilityExtensions"/>).
/// </summary>
/// <remarks>
/// Avalonia has <c>IsVisible</c> (WPF's Visible and Collapsed) but no Hidden, which keeps the
/// element's space. Hidden makes the element transparent and invisible to the pointer instead, over
/// any opacity it has, and Visible restores that opacity.
/// </remarks>
public static class WpfElement
{
    /// <summary>The element's visibility, as in WPF.</summary>
    public static readonly AttachedProperty<Visibility> VisibilityProperty =
        AvaloniaProperty.RegisterAttached<Visual, Visibility>("Visibility", typeof(WpfElement));

    // The values Hidden imposes, removed again when the element is shown.
    private static readonly ConditionalWeakTable<Visual, IDisposable[]> HiddenValues = new();

    static WpfElement()
    {
        VisibilityProperty.Changed.AddClassHandler<Visual>((visual, e) => Apply(visual, e.GetNewValue<Visibility>()));
    }

    /// <summary>
    /// Gets <paramref name="visual"/>'s visibility. An element that ported XAML hides with
    /// <c>IsVisible="False"</c> (WPF's <c>Visibility="Collapsed"</c>) is Collapsed.
    /// </summary>
    public static Visibility GetVisibility(Visual visual)
    {
        if (!visual.IsVisible)
        {
            return Visibility.Collapsed;
        }
        return visual.GetValue(VisibilityProperty) == Visibility.Hidden ? Visibility.Hidden : Visibility.Visible;
    }

    /// <summary>Sets <paramref name="visual"/>'s visibility.</summary>
    public static void SetVisibility(Visual visual, Visibility value)
    {
        if (visual.GetValue(VisibilityProperty) == value)
        {
            // IsVisible may have changed on its own (in XAML, say): apply the value again.
            Apply(visual, value);
            return;
        }
        visual.SetValue(VisibilityProperty, value);
    }

    private static void Apply(Visual visual, Visibility visibility)
    {
        if (HiddenValues.TryGetValue(visual, out IDisposable[]? hidden))
        {
            foreach (IDisposable value in hidden)
            {
                value.Dispose();
            }
            HiddenValues.Remove(visual);
        }

        visual.IsVisible = visibility != Visibility.Collapsed;
        if (visibility == Visibility.Hidden)
        {
            // Animation priority sits above every other value, as Hidden does in WPF.
            var values = new List<IDisposable>();
            if (visual.SetValue(Visual.OpacityProperty, 0d, BindingPriority.Animation) is { } opacity)
            {
                values.Add(opacity);
            }
            if (visual is Avalonia.Input.InputElement element
                && element.SetValue(Avalonia.Input.InputElement.IsHitTestVisibleProperty, false, BindingPriority.Animation) is { } hitTest)
            {
                values.Add(hitTest);
            }
            HiddenValues.Add(visual, [.. values]);
        }
    }
}

/// <summary>Gives every element WPF's <c>Visibility</c> property.</summary>
public static class VisibilityExtensions
{
    extension(Visual visual)
    {
        /// <summary>The element's visibility, as in WPF (<see cref="WpfElement.VisibilityProperty"/>).</summary>
        public Visibility Visibility
        {
            get => WpfElement.GetVisibility(visual);
            set => WpfElement.SetVisibility(visual, value);
        }
    }
}
