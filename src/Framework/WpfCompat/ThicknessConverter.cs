using System.ComponentModel;
using System.Globalization;
using Avalonia;

namespace WpfCompat;

/// <summary>
/// Converts a <see cref="Thickness"/> to and from text exactly as WPF's
/// <c>System.Windows.ThicknessConverter</c> does.
/// </summary>
/// <remarks>
/// XGoodFit addition, so ported code that saves thicknesses as text writes what WPF wrote, and reads
/// what WPF wrote. Avalonia's own <c>Thickness.ToString()</c> differs (it separates with ", ").
/// <list type="bullet">
/// <item>To text: left, top, right and bottom, each a double in the culture's default format, separated
/// by the culture's numeric list separator (<c>,</c>, or <c>;</c> where the decimal separator is
/// <c>,</c>). NaN is written as <c>Auto</c>. For example <c>8,8,8,8</c>.</item>
/// <item>From text: one value (every side), two (left and right, then top and bottom) or four,
/// separated by the list separator or white space. Each may be <c>Auto</c> or carry a unit:
/// <c>px</c>, <c>in</c>, <c>cm</c> or <c>pt</c>. From a number: the same thickness on every side.</item>
/// </list>
/// </remarks>
public class ThicknessConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        Type.GetTypeCode(sourceType) switch
        {
            TypeCode.String or TypeCode.Decimal or TypeCode.Single or TypeCode.Double or TypeCode.Int16
                or TypeCode.Int32 or TypeCode.Int64 or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 => true,
            _ => false,
        };

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        culture ??= CultureInfo.CurrentCulture;
        return value switch
        {
            string s => FromString(s, culture),
            double d => new Thickness(d),
            _ => new Thickness(Convert.ToDouble(value, culture)),
        };
    }

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        if (value is Thickness thickness && destinationType == typeof(string))
        {
            return ToString(thickness, culture ?? CultureInfo.CurrentCulture);
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>Formats <paramref name="thickness"/> as WPF does.</summary>
    internal static string ToString(Thickness thickness, CultureInfo culture)
    {
        char separator = GetNumericListSeparator(culture);
        return string.Concat(
            FormatLength(thickness.Left, culture), separator.ToString(),
            FormatLength(thickness.Top, culture), separator.ToString(),
            FormatLength(thickness.Right, culture), separator.ToString(),
            FormatLength(thickness.Bottom, culture));
    }

    /// <summary>Parses a thickness as WPF does.</summary>
    internal static Thickness FromString(string s, CultureInfo culture)
    {
        var lengths = new List<double>(4);
        foreach (string token in Tokenize(s, GetNumericListSeparator(culture)))
        {
            if (lengths.Count == 4)
            {
                throw new FormatException($"'{s}' is not a valid Thickness.");
            }
            lengths.Add(ParseLength(token, culture));
        }

        return lengths.Count switch
        {
            1 => new Thickness(lengths[0]),
            2 => new Thickness(lengths[0], lengths[1], lengths[0], lengths[1]),
            4 => new Thickness(lengths[0], lengths[1], lengths[2], lengths[3]),
            _ => throw new FormatException($"'{s}' is not a valid Thickness."),
        };
    }

    /// <summary>WPF's numeric list separator: a comma, or a semicolon where the decimal separator is a comma.</summary>
    internal static char GetNumericListSeparator(IFormatProvider? provider)
    {
        var format = NumberFormatInfo.GetInstance(provider);
        return format.NumberDecimalSeparator.Length > 0 && format.NumberDecimalSeparator[0] == ',' ? ';' : ',';
    }

    // WPF's LengthConverter: NaN is "Auto"; other values use the culture's default double format.
    private static string FormatLength(double value, CultureInfo culture) =>
        double.IsNaN(value) ? "Auto" : value.ToString(culture);

    // WPF's LengthConverter.FromString: "auto" (any case), or a number with an optional unit suffix.
    private static double ParseLength(string token, CultureInfo culture)
    {
        ReadOnlySpan<char> span = token.AsSpan().Trim();
        if (span.Equals("auto", StringComparison.OrdinalIgnoreCase))
        {
            return double.NaN;
        }

        double factor = 1.0;
        foreach (var (unit, unitFactor) in Units)
        {
            if (span.EndsWith(unit, StringComparison.OrdinalIgnoreCase))
            {
                span = span[..^unit.Length];
                factor = unitFactor;
                break;
            }
        }

        if (span.IsEmpty)
        {
            return 0;
        }

        try
        {
            return double.Parse(span, culture) * factor;
        }
        catch (FormatException)
        {
            throw new FormatException($"'{span.ToString()}' is not a valid length.");
        }
    }

    private static readonly (string Unit, double Factor)[] Units =
        [("px", 1.0), ("in", 96.0), ("cm", 96.0 / 2.54), ("pt", 96.0 / 72.0)];

    // WPF's TokenizerHelper for number lists: tokens are separated by white space and/or one list
    // separator. An empty token (two separators, or one at either end) is an error.
    private static IEnumerable<string> Tokenize(string s, char separator)
    {
        int i = 0;
        bool expectToken = false;
        while (true)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }
            if (i == s.Length)
            {
                if (expectToken)
                {
                    throw new FormatException($"'{s}' ends with a separator.");
                }
                yield break;
            }
            if (s[i] == separator)
            {
                throw new FormatException($"'{s}' has an empty value.");
            }

            int start = i;
            while (i < s.Length && !char.IsWhiteSpace(s[i]) && s[i] != separator)
            {
                i++;
            }
            yield return s[start..i];

            while (i < s.Length && char.IsWhiteSpace(s[i]))
            {
                i++;
            }
            expectToken = i < s.Length && s[i] == separator;
            if (expectToken)
            {
                i++;
            }
        }
    }
}
