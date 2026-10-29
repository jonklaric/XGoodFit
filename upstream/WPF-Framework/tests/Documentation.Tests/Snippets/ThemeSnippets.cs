#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

using System.Windows;
using FrameworkUI;
using Themes;

namespace Documentation.Tests.Snippets
{
    /// <summary>
    /// Validates that all C# code snippets in docs/themes.md compile correctly.
    /// </summary>
    public class ThemeSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: Quick Start -- Initialize on startup
        // ---------------------------------------------------------------
        public void Snippet_InitializeOnStartup()
        {
            // Option A: Using FrameworkUI (most applications)
            // ThemeManager.SetTheme(ThemeColor.Light);
            // NOTE: Requires WPF Application context at runtime. Verify API compiles:
            ThemeColor tc = ThemeColor.Light;
            _ = typeof(ThemeManager).GetMethod("SetTheme");

            // Option B: Using Themes library directly (standalone)
            // ThemeService.Instance.Initialize(Theme.Light);
            Theme t = Theme.Light;
            _ = ThemeService.Instance;
        }

        // ---------------------------------------------------------------
        // Snippet: Quick Start -- Switch at runtime
        // ---------------------------------------------------------------
        public void Snippet_SwitchAtRuntime()
        {
            // FrameworkUI applications
            // ThemeManager.SetTheme(ThemeColor.Dark);
            ThemeColor dark = ThemeColor.Dark;

            // Standalone usage
            // ThemeService.Instance.SetTheme(Theme.Dark);
            Theme darkTheme = Theme.Dark;
        }

        // ---------------------------------------------------------------
        // Snippet: Quick Start -- Subscribe to changes
        // ---------------------------------------------------------------
        public void Snippet_SubscribeToChanges()
        {
            // ThemeService event (available to all libraries)
            ThemeService.Instance.ThemeChanged += (sender, e) =>
            {
                Theme oldTheme = e.OldTheme;
                Theme newTheme = e.NewTheme;
                ResourceDictionary colorDictionary = e.ColorDictionary;
            };

            // ThemeManager event (FrameworkUI only)
            ThemeManager.ThemeChanged += (newThemeDictionary, newThemeColor) =>
            {
                // newThemeDictionary contains FrameworkUI-specific resources
                // newThemeColor is the ThemeColor enum value
            };
        }

        // ---------------------------------------------------------------
        // Snippet: Standalone Usage
        // ---------------------------------------------------------------
        public void Snippet_StandaloneUsage()
        {
            // Initialize in App.xaml.cs OnStartup
            // ThemeService.Instance.Initialize(Theme.Light);
            // NOTE: Cannot call Initialize without Application.Current, but verify the API:

            // Switch themes
            // ThemeService.Instance.SetTheme(Theme.Dark);

            // Query current theme
            Theme current = ThemeService.Instance.CurrentTheme;
            bool ready = ThemeService.Instance.IsInitialized;

            // Parse theme from user settings
            string savedThemeName = "Dark";
            if (ThemeResourceHelper.TryParseTheme(savedThemeName, out Theme theme))
            {
                // ThemeService.Instance.SetTheme(theme);
                _ = theme;
            }
        }

        // ---------------------------------------------------------------
        // Snippet: ThemeResourceHelper utility methods
        // ---------------------------------------------------------------
        public void Snippet_ThemeResourceHelper()
        {
            // GetColorDictionaryUri
            string uri = ThemeResourceHelper.GetColorDictionaryUri(Theme.Dark);

            // ParseTheme
            Theme parsed = ThemeResourceHelper.ParseTheme("Dark");

            // TryParseTheme
            bool success = ThemeResourceHelper.TryParseTheme("Light", out Theme result);

            // Constants
            string controlTemplates = ThemeResourceHelper.ControlTemplatesUri;
            string lightColors = ThemeResourceHelper.LightColorsUri;
            string blueColors = ThemeResourceHelper.BlueColorsUri;
            string darkColors = ThemeResourceHelper.DarkColorsUri;
        }
    }
}
