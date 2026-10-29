using System.Windows;
using Themes;

namespace NumericControls.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// Demonstrates the NumericControls library with theme support.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class App : Application
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
        /// Handles the application startup event.
        /// Initializes the theme system before creating any UI elements.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments containing startup information.</param>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Initialize the theme system with Light theme
            // This must be called before creating any windows
            ThemeService.Instance.Initialize(Theme.Light);

            // Create and show the main window
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
