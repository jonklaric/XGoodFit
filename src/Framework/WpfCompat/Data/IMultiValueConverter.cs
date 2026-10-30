using System.Globalization;

namespace WpfCompat.Data;

#nullable disable

/// <summary>
/// WPF's <c>System.Windows.Data.IMultiValueConverter</c>: converts the values of a multi-binding's
/// bindings to the target's value and, for two-way bindings, back.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia's <see cref="Avalonia.Data.Converters.IMultiValueConverter"/> converts one way only. This
/// keeps WPF's two methods and signatures, so ported converters change only their <c>using</c>, and
/// passes Avalonia's method on to WPF's, so such a converter also works in Avalonia's own
/// <c>MultiBinding</c>. WpfCompat's <see cref="MultiBinding"/> calls <see cref="ConvertBack"/>.
/// </para>
/// <para>
/// The signatures are nullable-oblivious, as WPF's are, so ported converters keep their annotations.
/// XGoodFit addition.
/// </para>
/// </remarks>
public interface IMultiValueConverter : Avalonia.Data.Converters.IMultiValueConverter
{
    /// <summary>Converts the bindings' values to the target's value.</summary>
    object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);

    /// <summary>
    /// Converts the target's value back to values for the bindings, in their order. A binding gets
    /// <see cref="Avalonia.Data.BindingOperations.DoNothing"/> to stay as it is.
    /// </summary>
    object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);

    /// <summary>Avalonia's conversion: WPF's <see cref="Convert(object[], Type, object, CultureInfo)"/>.</summary>
    object Avalonia.Data.Converters.IMultiValueConverter.Convert(IList<object> values, Type targetType, object parameter, CultureInfo culture) =>
        Convert(values.ToArray(), targetType, parameter, culture);
}
