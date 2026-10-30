using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace GenericControls
{
    /// <summary>
    /// Binds the element a bound column shows in a cell one way, as WPF's in effect is: it never writes to the
    /// row's data. The column's editor keeps the column's two-way binding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XGoodFit addition. ProDataGrid gives a cell's display element (a text block) the column's own binding,
    /// which it makes two-way for the editor. Avalonia's two-way bindings write their value back when the
    /// element's data context changes, as a recycled or refreshed row's does, if it doesn't convert back to the
    /// source's value: a StringFormat or converter that rounds or drops seconds. So scrolling, sorting or
    /// refreshing a grid would round its data to what it shows (a distribution parameter shown with
    /// <c>{0:0.####}</c> lost its other digits). WPF's text block never changes its text itself, so its binding
    /// writes nothing.
    /// </para>
    /// <para>
    /// The display element takes a one-way copy of the column's binding as the cell shows it. A binding that
    /// names an element (ElementName) is left as it is: its name scope can't be copied.
    /// </para>
    /// <para>
    /// ProDataGrid gives a column's binding a converter of its own, which makes text of the value, unless the
    /// binding has a converter or a StringFormat when the column gets it. A StringFormat set later, as upstream's
    /// code sets the formats it reads from the user's settings, would then format that text and show it as it
    /// is. WPF's binding has no converter and shows the value formatted, so the copy leaves that converter out.
    /// </para>
    /// </remarks>
    internal static class DataGridDisplayBinding
    {
        static DataGridDisplayBinding()
        {
            // A cell's content is the element its column created for it, or the column's editor while the cell is
            // edited (ProDataGrid marks the cell :edited first).
            ContentControl.ContentProperty.Changed.AddClassHandler<DataGridCell>((cell, _) => Apply(cell));
        }

        /// <summary>Makes sure the grid's cells are handled (runs the static constructor).</summary>
        internal static void Register()
        {
        }

        // Gives the cell's display element a one-way copy of its column's two-way binding.
        private static void Apply(DataGridCell cell)
        {
            if (cell.Classes.Contains(":edited")
                || cell.Content is not TextBlock text
                || cell.OwningColumn is not DataGridBoundColumn { Binding: ReflectionBinding binding }
                || binding.Mode is not BindingMode.TwoWay
                || binding.ElementName != null)
            {
                return;
            }
            text.Bind(TextBlock.TextProperty, new ReflectionBinding(binding.Path)
            {
                Mode = BindingMode.OneWay,
                Converter = FormatsTheValue(binding) ? null : binding.Converter,
                ConverterCulture = binding.ConverterCulture,
                ConverterParameter = binding.ConverterParameter,
                StringFormat = binding.StringFormat,
                FallbackValue = binding.FallbackValue,
                TargetNullValue = binding.TargetNullValue,
                Source = binding.Source,
                RelativeSource = binding.RelativeSource,
                TypeResolver = binding.TypeResolver,
                Priority = binding.Priority,
                Delay = binding.Delay,
            });
        }

        // Whether the binding has a StringFormat and the converter ProDataGrid gave it, which it had no format for.
        private static bool FormatsTheValue(ReflectionBinding binding) =>
            !string.IsNullOrEmpty(binding.StringFormat)
            && binding.Converter?.GetType().FullName == "Avalonia.Controls.DataGridValueConverter";
    }
}
