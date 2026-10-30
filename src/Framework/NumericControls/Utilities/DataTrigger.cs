using Avalonia;
using Avalonia.Controls;

namespace NumericControls
{
    /// <summary>
    /// The value of a WPF <c>DataTrigger</c>'s binding, for the cell themes ported from upstream's cell
    /// styles.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. A WPF style's DataTrigger applies its setters while its binding's value equals its
    /// <c>Value</c>. Avalonia's styles select on properties, not on bindings, so a ported theme binds this
    /// attached property to the trigger's binding and has a nested style for the trigger's value, as
    /// GenericControls' ValidationDataGrid does for its error trigger:
    /// <c>&lt;Setter Property="local:DataTrigger.Binding" Value="{Binding RuleMap[Value].HasError}"/&gt;</c>
    /// with <c>&lt;Style Selector="^[(local|DataTrigger.Binding)=True]"&gt;</c>. The value is null until the
    /// binding gives one, so a binding that fails matches no trigger, as in WPF.
    /// </remarks>
    public static class DataTrigger
    {
        /// <summary>The value of the trigger's binding, or null while it has none.</summary>
        public static readonly AttachedProperty<bool?> BindingProperty =
            AvaloniaProperty.RegisterAttached<Control, bool?>("Binding", typeof(DataTrigger));

        /// <summary>Gets the value of <paramref name="element"/>'s trigger binding.</summary>
        /// <param name="element">The element.</param>
        public static bool? GetBinding(Control element) => element.GetValue(BindingProperty);

        /// <summary>Sets the value of <paramref name="element"/>'s trigger binding.</summary>
        /// <param name="element">The element.</param>
        /// <param name="value">The value.</param>
        public static void SetBinding(Control element, bool? value) => element.SetValue(BindingProperty, value);
    }
}
