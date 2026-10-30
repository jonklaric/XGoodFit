using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WpfCompat.Media;

/// <summary>
/// WPF's <c>System.Windows.FontWeights</c>: the named weights, as Avalonia's <see cref="FontWeight"/>
/// values, and the names WPF shows for them.
/// </summary>
/// <remarks>
/// Avalonia's <see cref="FontWeight"/> is an enum with several names for some weights, and .NET names 600
/// "DemiBold" where WPF's <c>FontWeight.ToString()</c> says "SemiBold". <see cref="ToString(FontWeight)"/>
/// and <see cref="NameConverter"/> give WPF's names.
/// </remarks>
public static class FontWeights
{
    /// <summary>Weight 100.</summary>
    public static FontWeight Thin => FontWeight.Thin;

    /// <summary>Weight 200.</summary>
    public static FontWeight ExtraLight => FontWeight.ExtraLight;

    /// <summary>Weight 200.</summary>
    public static FontWeight UltraLight => FontWeight.UltraLight;

    /// <summary>Weight 300.</summary>
    public static FontWeight Light => FontWeight.Light;

    /// <summary>Weight 400.</summary>
    public static FontWeight Normal => FontWeight.Normal;

    /// <summary>Weight 400.</summary>
    public static FontWeight Regular => FontWeight.Regular;

    /// <summary>Weight 500.</summary>
    public static FontWeight Medium => FontWeight.Medium;

    /// <summary>Weight 600.</summary>
    public static FontWeight DemiBold => FontWeight.DemiBold;

    /// <summary>Weight 600.</summary>
    public static FontWeight SemiBold => FontWeight.SemiBold;

    /// <summary>Weight 700.</summary>
    public static FontWeight Bold => FontWeight.Bold;

    /// <summary>Weight 800.</summary>
    public static FontWeight ExtraBold => FontWeight.ExtraBold;

    /// <summary>Weight 800.</summary>
    public static FontWeight UltraBold => FontWeight.UltraBold;

    /// <summary>Weight 900.</summary>
    public static FontWeight Black => FontWeight.Black;

    /// <summary>Weight 900.</summary>
    public static FontWeight Heavy => FontWeight.Heavy;

    /// <summary>Weight 950.</summary>
    public static FontWeight ExtraBlack => FontWeight.ExtraBlack;

    /// <summary>Weight 950.</summary>
    public static FontWeight UltraBlack => FontWeight.UltraBlack;

    /// <summary>A converter from a <see cref="FontWeight"/> to WPF's name for it.</summary>
    public static IValueConverter NameConverter { get; } = new FuncValueConverter<FontWeight, string>(ToString);

    /// <summary>
    /// WPF's name for <paramref name="weight"/>, as WPF's <c>FontWeight.ToString()</c>: the name of a
    /// standard weight, or else the number.
    /// </summary>
    public static string ToString(FontWeight weight) => (int)weight switch
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
        int other => other.ToString(CultureInfo.CurrentCulture),
    };
}
