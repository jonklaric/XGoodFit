using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace WpfCompat.Media;

/// <summary>
/// Converts a brush to and from text exactly as WPF's <c>System.Windows.Media.BrushConverter</c> does
/// for solid colours.
/// </summary>
/// <remarks>
/// XGoodFit addition, so ported code that saves brushes as text writes what WPF wrote.
/// <list type="bullet">
/// <item>To text: a solid colour brush as its colour, <c>#AARRGGBB</c> (WPF ignores the brush's
/// opacity too); no brush as empty text, as WPF's converter writes for null. WPF writes other brushes
/// as their type name; this converter writes Avalonia's <c>ToString()</c>, as neither can be read
/// back.</item>
/// <item>From text: the colour forms <see cref="ColorConverter"/> reads, as a new
/// <see cref="SolidColorBrush"/>.</item>
/// </list>
/// </remarks>
public class BrushConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string s ? new SolidColorBrush(ColorConverter.Parse(s)) : throw GetConvertFromException(value);

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        if (value is ISolidColorBrush brush && destinationType == typeof(string))
        {
            return brush.Color.ToWpfString();
        }

        // Null becomes empty text and other brushes their ToString(), as in WPF.
        return base.ConvertTo(context, culture, value, destinationType);
    }
}
