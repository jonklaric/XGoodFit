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
/// WPF draws in Segoe UI, and upstream's XAML names it and a few other Windows fonts directly:
/// Consolas for monospaced reports and text, Cambria for the italic f(x) in function icons, Calibri for
/// one expander header. Those ship with Windows only. Elsewhere Avalonia would draw them, and text
/// that names no font, in the platform's own default font: proportional where Consolas aligned
/// columns, different on every OS, and missing on minimal systems (such as CI runners), where text then
/// fails to lay out.
/// </para>
/// <para>
/// So the theme embeds open-source substitutes, and outside Windows they stand in wherever the Windows
/// font is named: Selawik (Segoe UI's metric-compatible substitute) for Segoe UI, which is also the
/// default font family; Cascadia Mono for Consolas; Caladea for Cambria and Carlito for Calibri, both
/// metric-compatible. On Windows the platform's fonts are used, as in WPF.
/// </para>
/// </remarks>
public static class ThemeFonts
{
    /// <summary>The key of the theme's embedded font collection.</summary>
    public static readonly Uri Collection = new("fonts:Themes", UriKind.Absolute);

    /// <summary>Selawik, Segoe UI's substitute, as a font family name the font manager resolves.</summary>
    public const string Selawik = "fonts:Themes#Selawik";

    /// <summary>Cascadia Mono, Consolas's substitute.</summary>
    public const string CascadiaMono = "fonts:Themes#Cascadia Mono";

    /// <summary>Caladea, Cambria's substitute.</summary>
    public const string Caladea = "fonts:Themes#Caladea";

    /// <summary>Carlito, Calibri's substitute.</summary>
    public const string Carlito = "fonts:Themes#Carlito";

    /// <summary>The Windows fonts upstream names, and the substitute each gets outside Windows.</summary>
    public static IReadOnlyDictionary<string, string> Substitutes { get; } = new Dictionary<string, string>
    {
        ["Segoe UI"] = Selawik,
        ["Consolas"] = CascadiaMono,
        ["Cambria"] = Caladea,
        ["Calibri"] = Carlito,
    };

    /// <summary>
    /// Registers the theme's fonts and, outside Windows, makes Selawik the default font family and each
    /// substitute the stand-in for its Windows font. Call it on every application's builder, as the
    /// theme's other defaults (<see cref="ThemeService"/>) assume it.
    /// </summary>
    /// <param name="builder">The application's builder.</param>
    /// <returns>The builder.</returns>
    public static AppBuilder UseThemeFonts(this AppBuilder builder)
    {
        builder.ConfigureFonts(manager =>
            manager.AddFontCollection(new EmbeddedFontCollection(Collection, new Uri("avares://Themes/Assets/Fonts", UriKind.Absolute))));
        if (OperatingSystem.IsWindows())
        {
            return builder;
        }

        return builder.With(new FontManagerOptions
        {
            DefaultFamilyName = Selawik,
            FontFamilyMappings = Substitutes.ToDictionary(pair => pair.Key, pair => new FontFamily(pair.Value)),
        });
    }
}
