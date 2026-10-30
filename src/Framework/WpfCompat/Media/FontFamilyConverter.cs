using System.ComponentModel;
using System.Globalization;
using Avalonia.Media;

namespace WpfCompat.Media;

/// <summary>
/// Converts a <see cref="FontFamily"/> to and from text as WPF's
/// <c>System.Windows.Media.FontFamilyConverter</c> does.
/// </summary>
/// <remarks>
/// XGoodFit addition, so ported code that saves font families as text writes what WPF wrote.
/// <list type="bullet">
/// <item>To text: WPF writes the text the family was made from, such as <c>Segoe UI</c> or
/// <c>Arial, Verdana</c>. Avalonia families can also name font files, which XGoodFit uses for fallback
/// fonts (Themes' default is <c>Segoe UI, avares://Themes/Assets/Fonts#Selawik</c>), and Avalonia's
/// <c>ToString()</c> writes a resource key for those. This converter writes the family's installed-font
/// names, so that default is written <c>Segoe UI</c>, as in WPF. A family made only of font files is
/// written as its first family name.</item>
/// <item>From text: a new <see cref="FontFamily"/> of that name.</item>
/// </list>
/// </remarks>
public class FontFamilyConverter : TypeConverter
{
    /// <inheritdoc/>
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string);

    /// <inheritdoc/>
    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType) =>
        destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

    /// <inheritdoc/>
    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value switch
        {
            string { Length: > 0 } s => FontFamily.Parse(s),
            string => throw new ArgumentException("A font family needs a name.", nameof(value)),
            _ => throw GetConvertFromException(value),
        };

    /// <inheritdoc/>
    public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);
        if (destinationType == typeof(string))
        {
            // As in WPF, a null family can't be converted.
            ArgumentNullException.ThrowIfNull(value);
            if (value is FontFamily family)
            {
                return ToString(family);
            }
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }

    /// <summary>The WPF text form of <paramref name="family"/>; see <see cref="FontFamilyConverter"/>.</summary>
    /// <param name="family">The font family.</param>
    public static string ToString(FontFamily family)
    {
        ArgumentNullException.ThrowIfNull(family);

        // The text the family was made from. Avalonia keeps it for a family of several fonts as its
        // key ("compositefont:" + text); a single installed font has no key, and its name is the text.
        string? source = family.Key switch
        {
            null => family.FamilyNames.ToString(),
            { Source: { Scheme: FontManager.CompositeFontScheme } uri } => uri.OriginalString[(FontManager.CompositeFontScheme.Length + 1)..],
            _ => null,
        };

        var installed = source?.Split(',').Select(s => s.Trim()).Where(IsInstalledFontName).ToList();
        return installed is { Count: > 0 } ? string.Join(", ", installed) : family.Name;
    }

    // Avalonia's FontFamily parser reads a segment with exactly one '#' after a path as a font file
    // ("path#name"); anything else names an installed font.
    private static bool IsInstalledFontName(string segment)
    {
        int hash = segment.IndexOf('#');
        return segment.Length > 0 && (hash <= 0 || segment.IndexOf('#', hash + 1) != -1);
    }
}
