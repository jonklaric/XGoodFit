using Avalonia;
using Avalonia.Controls;

namespace WpfCompat;

/// <summary>
/// WPF's <c>Selector.IsSelectionActive</c>: whether a selection control has the keyboard focus, so its
/// selected items show the active selection colours rather than the inactive ones.
/// </summary>
/// <remarks>
/// XGoodFit addition. In WPF the selection control sets this attached property and its items inherit
/// it, and the ported themes' triggers read it (<c>Selector.IsSelectionActive</c> False, the inactive
/// selection). Here a control's theme sets it from the control's <c>IsKeyboardFocusWithin</c>, and the
/// value is inherited the same way:
/// <c>&lt;Setter Property="wpf:WpfSelector.IsSelectionActive" Value="{Binding IsKeyboardFocusWithin, RelativeSource={RelativeSource Self}}"/&gt;</c>,
/// read with <c>^:selected[(wpf|WpfSelector.IsSelectionActive)=False]</c>.
/// </remarks>
public static class WpfSelector
{
    /// <summary>Whether the selection control the element belongs to has the keyboard focus. Inherited.</summary>
    public static readonly AttachedProperty<bool> IsSelectionActiveProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSelectionActive", typeof(WpfSelector), inherits: true);

    /// <summary>Gets whether <paramref name="element"/>'s selection is active.</summary>
    public static bool GetIsSelectionActive(Control element) => element.GetValue(IsSelectionActiveProperty);

    /// <summary>Sets whether <paramref name="element"/>'s selection is active.</summary>
    public static void SetIsSelectionActive(Control element, bool value) => element.SetValue(IsSelectionActiveProperty, value);
}
