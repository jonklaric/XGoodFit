using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Themes;

/// <summary>
/// XGoodFit addition: the fonts the VS2013 theme draws with, configured on the application's
/// <see cref="AppBuilder"/> (docs/migration-plan.md §6, typography).
/// </summary>
/// <remarks>
/// <para>
/// WPF draws in Segoe UI, and upstream's XAML often names it directly. Segoe UI ships with Windows only.
/// Elsewhere Avalonia would draw those names, and text that names no font, in the platform's own
/// default font, which differs per OS and is missing on minimal systems (such as CI runners), where text
/// then fails to lay out.
/// </para>
/// <para>
/// So the theme's embedded Selawik, Microsoft's metric-compatible substitute for Segoe UI, is
/// registered, and outside Windows it is the default font family and stands in for "Segoe UI"
/// wherever that is named. On Windows the platform's fonts are used, as in WPF.
/// </para>
/// </remarks>
public static class ThemeFonts
{
    /// <summary>The key of the embedded Selawik font collection.</summary>
    public static readonly Uri SelawikCollection = new("fonts:Selawik", UriKind.Absolute);

    /// <summary>Selawik, as a font family name the font manager resolves.</summary>
    public const string Selawik = "fonts:Selawik#Selawik";

    /// <summary>
    /// Registers the theme's fonts and, outside Windows, makes Selawik the default font family and the
    /// stand-in for Segoe UI. Call it on every application's builder, as the theme's other defaults
    /// (<see cref="ThemeService"/>) assume it.
    /// </summary>
    /// <param name="builder">The application's builder.</param>
    /// <returns>The builder.</returns>
    public static AppBuilder UseThemeFonts(this AppBuilder builder)
    {
        builder.ConfigureFonts(manager =>
            manager.AddFontCollection(new EmbeddedFontCollection(SelawikCollection, new Uri("avares://Themes/Assets/Fonts", UriKind.Absolute))));
        if (OperatingSystem.IsWindows())
        {
            return builder;
        }

        var selawik = new FontFamily(Selawik);
        return builder.With(new FontManagerOptions
        {
            DefaultFamilyName = Selawik,
            FontFamilyMappings = new Dictionary<string, FontFamily> { ["Segoe UI"] = selawik },
        });
    }
}
