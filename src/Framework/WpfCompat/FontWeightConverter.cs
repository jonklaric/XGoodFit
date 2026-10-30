using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace WpfCompat;

/// <summary>
/// Converts a <see cref="FontWeight"/> to and from text exactly as WPF's
/// <c>System.Windows.FontWeightConverter</c> does.
/// </summary>
/// <remarks>
/// XGoodFit addition, so ported code that saves font weights as text writes what WPF wrote. Avalonia's
/// <see cref="FontWeight"/> is an enum whose synonyms share values, so its <c>ToString()</c> may name a
/// weight differently (for example <c>Regular</c> for <c>Normal</c>).
/// <list type="bullet">
/// <item>To text: WPF's name for 100 to 900 in steps of 100 and for 950 (<c>Thin</c>, <c>ExtraLight</c>,
/// <c>Light</c>, <c>Normal</c>, <c>Medium</c>, <c>SemiBold</c>, <c>Bold</c>, <c>ExtraBold</c>,
/// <c>Black</c>, <c>ExtraBlack</c>); any other weight as its number.</item>
/// <item>From text: any of WPF's names and synonyms, ignoring case (but not surrounding space, as in
/// WPF), or a number from 1 to 999.</item>
/// </list>
/// </remarks>
public class FontWeightConverter : TypeConverter
{
    private static readonly Dictionary<string, FontWeight> KnownWeights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Thin"] = FontWeight.Thin,
        ["ExtraLight"] = FontWeight.ExtraLight,
        ["UltraLight"] = FontWeight.UltraLight,
        ["Light"] = FontWeight.Light,
        ["Normal"] = FontWeight.Normal,
        ["Regular"] = FontWeight.Regular,
        ["Medium"] = FontWeight.Medium,
        ["DemiBold"] = FontWeight.DemiBold,
        ["SemiBold"] = FontWeight.SemiBold,
        ["Bold"] = FontWeight.Bold,
        ["ExtraBold"] = FontWeight.ExtraBold,
        ["UltraBold"] = FontWeight.UltraBold,
        ["Black"] = FontWeight.Black,
        ["Heavy"] = FontWeight.Heavy,
        ["ExtraBlack"] = FontWeight.ExtraBlack,
        ["UltraBlack"] = FontWeight.UltraBlack,
    };

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string s)
        {
            throw value is null ? GetConvertFromException(value) : new ArgumentException("Expected a string.", nameof(value));
        }
        if (KnownWeights.TryGetValue(s, out var weight))
        {
            return weight;
        }
        if (int.TryParse(s, NumberStyles.Integer, culture, out int number))
        {
            if (number is < 1 or > 999)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "A font weight must be from 1 to 999.");
            }
            return (FontWeight)number;
        }
        throw new FormatException($"'{s}' is not a valid font weight.");
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        if (value is FontWeight weight && destinationType == typeof(string))
        {
            return ToString(weight, culture);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>Names <paramref name="weight"/> as WPF's <c>FontWeight.ToString</c> does.</summary>
    internal static string ToString(FontWeight weight, IFormatProvider? provider) => (int)weight switch
    {
        100 => "Thin",
        200 => "ExtraLight",
        300 => "Light",
        400 => "Normal",
        500 => "Medium",
        600 => "SemiBold",
        700 => "Bold",
        800 => "ExtraBold",
        900 => "Black",
        950 => "ExtraBlack",
        var number => number.ToString(provider),
    };
}
