using System.Windows;
using Themes;

namespace GenericControls.Demo
{
    /// <summary>
    /// Represents the WPF application entry point for the GenericControls demo.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    /// can be handled in this file.
    /// </para>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class App
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        public App()
        {
            // Set WPF to use the current culture for all bindings (international number support)
            // This ensures StringFormat in XAML bindings uses the user's locale settings
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(
                    System.Windows.Markup.XmlLanguage.GetLanguage(
                        System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));
        }

        /// <summary>
        /// Handles the application Startup event. Initializes the theme system
        /// and creates the main window.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Using the Startup event instead of StartupUri ensures that theme resources
        /// (control templates and colors) are loaded before any XAML windows are parsed.
        /// This prevents "Cannot find resource" errors for theme-dependent styles.
        /// </para>
        /// </remarks>
        /// <param name="sender">The application instance.</param>
        /// <param name="e">Startup event arguments.</param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize the theme system with the Light theme as default
            // This loads control templates and color resources BEFORE MainWindow is created
            ThemeService.Instance.Initialize(Theme.Light);

            // Create and show the main window after theme resources are loaded
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}