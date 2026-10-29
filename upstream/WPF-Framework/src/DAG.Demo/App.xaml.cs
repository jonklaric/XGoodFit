using System.Windows;

namespace DAG.Demo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes culture-aware WPF binding metadata for the demo application.
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
    }
}
