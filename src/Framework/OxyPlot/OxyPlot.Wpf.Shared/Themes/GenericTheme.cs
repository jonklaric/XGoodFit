// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GenericTheme.cs" company="XGoodFit">
//   Copyright (c) 2026 XGoodFit contributors
// </copyright>
// <summary>
//   Loads the assembly's control themes, as WPF's ThemeInfo did.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Markup.Xaml;

    /// <summary>
    /// Loads this assembly's control themes (Themes/Generic.axaml) into the application's resources.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF applies an assembly's generic.xaml by itself (ThemeInfo), so the plot view
    /// and tracker had their templates without any setup. Avalonia finds control themes only in
    /// resources, so <see cref="PlotViewBase"/> and <see cref="TrackerControl"/> call
    /// <see cref="EnsureLoaded"/>, which merges the dictionary into the application's resources once.
    /// It goes first among the merged dictionaries, so it is searched last: as with WPF's theme styles,
    /// a theme the application defines for the same control wins.
    /// </remarks>
    public static class GenericTheme
    {
        /// <summary>
        /// The location of the themes.
        /// </summary>
        public static readonly Uri Source = new Uri("avares://OxyPlot.Wpf.Shared/Themes/Generic.axaml");

        /// <summary>
        /// Whether the themes are in the application's resources.
        /// </summary>
        private static bool loaded;

        /// <summary>
        /// Merges the themes into the current application's resources, if they aren't there yet. Does
        /// nothing without an application.
        /// </summary>
        public static void EnsureLoaded()
        {
            if (loaded || Application.Current is not { } application)
            {
                return;
            }

            loaded = true;
            var themes = (IResourceProvider)AvaloniaXamlLoader.Load(Source);
            application.Resources.MergedDictionaries.Insert(0, themes);
        }
    }
}
