using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace WpfCompat.Media;

/// <summary>
/// Converts a <see cref="Color"/> to and from text exactly as WPF's
/// <c>System.Windows.Media.ColorConverter</c> does.
/// </summary>
/// <remarks>
/// XGoodFit addition, so ported code that saves colours as text writes what WPF wrote, and reads what
/// WPF wrote. Avalonia's own <c>Color.ToString()</c> differs: it names known colours and writes
/// lowercase hexadecimal.
/// <list type="bullet">
/// <item>To text: <c>#AARRGGBB</c> in uppercase hexadecimal (WPF's <c>Color.ToString()</c>); see
/// <see cref="ColorExtensions.ToWpfString"/>.</item>
/// <item>From text, ignoring surrounding space: <c>#RGB</c>, <c>#ARGB</c>, <c>#RRGGBB</c>,
/// <c>#AARRGGBB</c>, or a colour name in any case. WPF's rarely used <c>sc#</c> (scRGB) and
/// <c>ContextColor</c> forms aren't supported.</item>
/// </list>
/// </remarks>
public class ColorConverter : TypeConverter
{
    /// <summary>
    /// Parses a colour as WPF does. Returns null for null text, like WPF; the result is a boxed
    /// <see cref="Color"/>, so callers cast it: <c>(Color)ColorConverter.ConvertFromString(text)</c>.
    /// </summary>
    /// <param name="value">The text.</param>
    /// <exception cref="FormatException"><paramref name="value"/> isn't a colour.</exception>
    public static new object? ConvertFromString(string? value) => value is null ? null : Parse(value);

    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string s ? Parse(s) : throw GetConvertFromException(value);

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        if (value is Color color && destinationType == typeof(string))
        {
            return color.ToWpfString();
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>Parses a colour as WPF's <c>Parsers.ParseColor</c> does.</summary>
    internal static Color Parse(string value)
    {
        string s = value.Trim();
        if (s.StartsWith('#'))
        {
            return ParseHex(s) ?? throw new FormatException($"'{value}' is not a valid colour.");
        }

        // A colour name. Avalonia knows the same named colours as WPF, in any case; its parser also
        // takes CSS functions such as rgb(...), which WPF rejects.
        if (s.Length > 0 && char.IsLetter(s[0]) && !s.Contains('(') && Color.TryParse(s, out var named))
        {
            return named;
        }
        throw new FormatException($"'{value}' is not a valid colour.");
    }

    private static Color? ParseHex(string s)
    {
        ReadOnlySpan<char> hex = s.AsSpan(1);
        foreach (char c in hex)
        {
            if (!char.IsAsciiHexDigit(c))
            {
                return null;
            }
        }

        static byte Nibble(char c) => (byte)(Convert.ToByte(c.ToString(), 16) * 17);
        static byte Pair(ReadOnlySpan<char> pair) => byte.Parse(pair, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return hex.Length switch
        {
            3 => Color.FromArgb(0xFF, Nibble(hex[0]), Nibble(hex[1]), Nibble(hex[2])),
            4 => Color.FromArgb(Nibble(hex[0]), Nibble(hex[1]), Nibble(hex[2]), Nibble(hex[3])),
            6 => Color.FromArgb(0xFF, Pair(hex[0..2]), Pair(hex[2..4]), Pair(hex[4..6])),
            8 => Color.FromArgb(Pair(hex[0..2]), Pair(hex[2..4]), Pair(hex[4..6]), Pair(hex[6..8])),
            _ => null,
        };
    }
}

/// <summary>WPF's text forms of Avalonia colours.</summary>
/// <remarks>XGoodFit addition; see <see cref="ColorConverter"/>.</remarks>
public static class ColorExtensions
{
    /// <summary>
    /// Formats the colour as WPF's <c>Color.ToString()</c> does: <c>#AARRGGBB</c>, uppercase.
    /// Use it where ported code called <c>ToString()</c> on a WPF colour.
    /// </summary>
    /// <param name="color">The colour.</param>
    public static string ToWpfString(this Color color) =>
        string.Create(CultureInfo.InvariantCulture, $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
}
