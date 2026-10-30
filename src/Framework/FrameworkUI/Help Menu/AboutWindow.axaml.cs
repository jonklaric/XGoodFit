using GenericControls;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using WpfCompat;

namespace FrameworkUI
{
    /// <summary>
    /// A standard About dialog window that displays application information.
    /// All text fields auto-populate from assembly attributes and can be overridden via dependency properties.
    /// </summary>
    public partial class AboutWindow : MetroWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AboutWindow"/> class.
        /// </summary>
        public AboutWindow()
        {
            InitializeComponent();
            Loaded += AboutWindow_Loaded;
        }

        /// <summary>
        /// Sizes the name column and applies a window icon when the dialog loads.
        /// </summary>
        /// <param name="sender">The about window that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void AboutWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Measure the software name and widen the name column if needed
            var formattedText = new FormattedText(
                SoftwareName ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyle.Normal, FontWeight.Bold, FontStretch.Normal),
                32,
                Brushes.Black);
            double nameWidth = formattedText.Width + 10; // small buffer for padding
            // XGoodFit port: the name column is the grid's second (see the XAML).
            if (nameWidth > 250 && LayoutGrid.ColumnDefinitions[1] is ColumnDefinition nameColumn)
                nameColumn.Width = new GridLength(nameWidth);

            // Auto-populate window icon from owner or main window
            if (Icon == null)
            {
                if (Owner != null)
                    Icon = Owner.Icon;
                else if (Application.Current?.MainWindow != null)
                    Icon = Application.Current!.MainWindow!.Icon;
            }

            // Auto-populate software image from window icon if not set.
            // For a crisp high-res icon, callers should set SoftwareImage explicitly
            // from their app's .ico ImageSource resource.
            if (SoftwareImage == null)
            {
                if (Owner != null && Owner.Icon != null)
                    SoftwareImage = IconImage(Owner.Icon);
                else if (Application.Current?.MainWindow?.Icon != null)
                    SoftwareImage = IconImage(Application.Current!.MainWindow!.Icon!);
            }
        }

        // XGoodFit port: Avalonia's window icons aren't images; Themes' converter draws one as an image.
        private static ImageSource? IconImage(WindowIcon icon) =>
            Themes.WindowIconImageConverter.Instance.Convert(icon, typeof(ImageSource), null, CultureInfo.InvariantCulture) as ImageSource;

        #region Dependency Properties

        /// <summary>
        /// The software name displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> SoftwareNameProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(SoftwareName), ApplicationAttributes.Title);

        /// <summary>
        /// Gets or sets the software name.
        /// </summary>
        public string SoftwareName
        {
            get { return GetValue(SoftwareNameProperty); }
            set { SetValue(SoftwareNameProperty, value); }
        }

        /// <summary>
        /// The software version displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> SoftwareVersionProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(SoftwareVersion), ShellPublicVariables.SoftwareVersion);

        /// <summary>
        /// Gets or sets the software version.
        /// </summary>
        public string SoftwareVersion
        {
            get { return GetValue(SoftwareVersionProperty); }
            set { SetValue(SoftwareVersionProperty, value); }
        }

        /// <summary>
        /// The software version date displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> SoftwareVersionDateProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(SoftwareVersionDate), ShellPublicVariables.SoftwareVersionDate);

        /// <summary>
        /// Gets or sets the software version date.
        /// </summary>
        public string SoftwareVersionDate
        {
            get { return GetValue(SoftwareVersionDateProperty); }
            set { SetValue(SoftwareVersionDateProperty, value); }
        }

        /// <summary>
        /// The software description displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> SoftwareDescriptionProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(SoftwareDescription), ApplicationAttributes.Description);

        /// <summary>
        /// Gets or sets the software description.
        /// </summary>
        public string SoftwareDescription
        {
            get { return GetValue(SoftwareDescriptionProperty); }
            set { SetValue(SoftwareDescriptionProperty, value); }
        }

        /// <summary>
        /// The software copyright displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> SoftwareCopyrightProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(SoftwareCopyright), ApplicationAttributes.Copyright);

        /// <summary>
        /// Gets or sets the software copyright.
        /// </summary>
        public string SoftwareCopyright
        {
            get { return GetValue(SoftwareCopyrightProperty); }
            set { SetValue(SoftwareCopyrightProperty, value); }
        }

        /// <summary>
        /// The software image/icon displayed in the About window.
        /// If not set, auto-populated from the application's main window icon.
        /// </summary>
        public static readonly StyledProperty<ImageSource?> SoftwareImageProperty =
            AvaloniaProperty.Register<AboutWindow, ImageSource?>(nameof(SoftwareImage), null);

        /// <summary>
        /// Gets or sets the software image.
        /// </summary>
        public ImageSource? SoftwareImage
        {
            get { return GetValue(SoftwareImageProperty); }
            set { SetValue(SoftwareImageProperty, value); }
        }

        /// <summary>
        /// The organization name displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> OrganizationNameProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(OrganizationName), "Risk Management Center");

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string OrganizationName
        {
            get { return GetValue(OrganizationNameProperty); }
            set { SetValue(OrganizationNameProperty, value); }
        }

        /// <summary>
        /// The organization details (address, etc.) displayed in the About window.
        /// </summary>
        public static readonly StyledProperty<string> OrganizationDetailsProperty =
            AvaloniaProperty.Register<AboutWindow, string>(nameof(OrganizationDetails), "Institute for Water Resources\nU.S. Army Corps of Engineers\n12596 W. Bayaud Ave. Suite 400\nLakewood, CO 80228");

        /// <summary>
        /// Gets or sets the organization details.
        /// </summary>
        public string OrganizationDetails
        {
            get { return GetValue(OrganizationDetailsProperty); }
            set { SetValue(OrganizationDetailsProperty, value); }
        }

        /// <summary>
        /// The organization image/logo displayed in the About window.
        /// Defaults to the USACE logo.
        /// </summary>
        public static readonly StyledProperty<ImageSource?> OrganizationImageProperty =
            AvaloniaProperty.Register<AboutWindow, ImageSource?>(nameof(OrganizationImage), GetDefaultOrganizationImage());

        /// <summary>
        /// Gets or sets the organization image.
        /// </summary>
        public ImageSource? OrganizationImage
        {
            get { return GetValue(OrganizationImageProperty); }
            set { SetValue(OrganizationImageProperty, value); }
        }

        #endregion

        /// <summary>
        /// Gets the window title, which defaults to "About {SoftwareName}".
        /// </summary>
        public string WindowTitle
        {
            get { return "About " + SoftwareName; }
        }

        /// <summary>
        /// Gets the visibility of the organization section.
        /// Visible when organization name is set or organization image is set.
        /// </summary>
        public Visibility OrganizationVisibility
        {
            get
            {
                if (!string.IsNullOrEmpty(OrganizationName) || OrganizationImage != null)
                    return Visibility.Visible;
                return Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Gets the default USACE organization logo from embedded resources.
        /// </summary>
        private static ImageSource? GetDefaultOrganizationImage()
        {
            try
            {
                var uri = new Uri("avares://FrameworkUI/Resources/USACE.png", UriKind.Absolute);
                return new Bitmap(AssetLoader.Open(uri));
            }
            catch
            {
                return null;
            }
        }
    }
}
