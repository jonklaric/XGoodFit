using GenericControls;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for DefaultsOptions.xaml providing default project settings configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port: the dependency properties are styled properties with the same defaults; WPF's Window.GetWindow
    /// is the control's top level.
    /// </para>
    /// </remarks>
    public partial class DefaultsOptions : UserControl
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultsOptions"/> class.
        /// </summary>
        public DefaultsOptions()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
        }

        /// <summary>
        /// Dependency property for the default location text.
        /// </summary>
        public static readonly StyledProperty<string> DefaultLocationProperty = AvaloniaProperty.Register<DefaultsOptions, string>(nameof(DefaultLocation), "");

        /// <summary>
        /// Gets or sets the folder location text.
        /// </summary>
        public string DefaultLocation
        {
            get { return GetValue(DefaultLocationProperty)?.ToString() ?? string.Empty; }
            set { SetValue(DefaultLocationProperty, value); }
        }

        /// <summary>
        /// Dependency property for the default value digits.
        /// </summary>
        public static readonly StyledProperty<int> DefaultValueDigitsProperty = AvaloniaProperty.Register<DefaultsOptions, int>(nameof(DefaultValueDigits), 2);

        /// <summary>
        /// Gets or sets the default output value decimal digits.
        /// </summary>
        public int DefaultValueDigits
        {
            get { return (int)GetValue(DefaultValueDigitsProperty); }
            set { SetValue(DefaultValueDigitsProperty, value); }
        }

        /// <summary>
        /// Dependency property for custom options.
        /// </summary>
        public static readonly StyledProperty<object?> CustomOptionsProperty = AvaloniaProperty.Register<DefaultsOptions, object?>(nameof(CustomOptions), null);

        /// <summary>
        /// Gets or sets the custom options control or content.
        /// </summary>
        public object CustomOptions
        {
            get { return GetValue(CustomOptionsProperty)!; }
            set { SetValue(CustomOptionsProperty, value); }
        }

        /// <summary>
        /// On click, open folder browser to select folder.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedLocation = GeneralMethods.FolderBrowserDialog(TopLevel.GetTopLevel(this) as Window, "Project Location", Directory.Exists(DefaultLocation) == true ? DefaultLocation : null);
            if (!string.IsNullOrEmpty(selectedLocation)) DefaultLocation = selectedLocation;
        }

    }
}
