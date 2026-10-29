using GenericControls;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        private void AboutWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Measure the software name and widen the name column if needed
            var formattedText = new FormattedText(
                SoftwareName ?? string.Empty,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(FontFamily, FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                32,
                Brushes.Black,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            double nameWidth = formattedText.Width + 10; // small buffer for padding
            if (nameWidth > 250 && FindName("NameColumn") is System.Windows.Controls.ColumnDefinition nameColumn)
                nameColumn.Width = new GridLength(nameWidth);

            // Auto-populate window icon from owner or main window
            if (Icon == null)
            {
                if (Owner != null)
                    Icon = Owner.Icon;
                else if (Application.Current?.MainWindow != null)
                    Icon = Application.Current.MainWindow.Icon;
            }

            // Auto-populate software image from window icon if not set.
            // For a crisp high-res icon, callers should set SoftwareImage explicitly
            // from their app's .ico ImageSource resource.
            if (SoftwareImage == null)
            {
                if (Owner != null && Owner.Icon != null)
                    SoftwareImage = Owner.Icon;
                else if (Application.Current?.MainWindow?.Icon != null)
                    SoftwareImage = Application.Current.MainWindow.Icon;
            }
        }

        #region Dependency Properties

        /// <summary>
        /// The software name displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty SoftwareNameProperty =
            DependencyProperty.Register(nameof(SoftwareName), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata(ApplicationAttributes.Title));

        /// <summary>
        /// Gets or sets the software name.
        /// </summary>
        public string SoftwareName
        {
            get { return (string)GetValue(SoftwareNameProperty); }
            set { SetValue(SoftwareNameProperty, value); }
        }

        /// <summary>
        /// The software version displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty SoftwareVersionProperty =
            DependencyProperty.Register(nameof(SoftwareVersion), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata(ShellPublicVariables.SoftwareVersion));

        /// <summary>
        /// Gets or sets the software version.
        /// </summary>
        public string SoftwareVersion
        {
            get { return (string)GetValue(SoftwareVersionProperty); }
            set { SetValue(SoftwareVersionProperty, value); }
        }

        /// <summary>
        /// The software version date displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty SoftwareVersionDateProperty =
            DependencyProperty.Register(nameof(SoftwareVersionDate), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata(ShellPublicVariables.SoftwareVersionDate));

        /// <summary>
        /// Gets or sets the software version date.
        /// </summary>
        public string SoftwareVersionDate
        {
            get { return (string)GetValue(SoftwareVersionDateProperty); }
            set { SetValue(SoftwareVersionDateProperty, value); }
        }

        /// <summary>
        /// The software description displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty SoftwareDescriptionProperty =
            DependencyProperty.Register(nameof(SoftwareDescription), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata(ApplicationAttributes.Description));

        /// <summary>
        /// Gets or sets the software description.
        /// </summary>
        public string SoftwareDescription
        {
            get { return (string)GetValue(SoftwareDescriptionProperty); }
            set { SetValue(SoftwareDescriptionProperty, value); }
        }

        /// <summary>
        /// The software copyright displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty SoftwareCopyrightProperty =
            DependencyProperty.Register(nameof(SoftwareCopyright), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata(ApplicationAttributes.Copyright));

        /// <summary>
        /// Gets or sets the software copyright.
        /// </summary>
        public string SoftwareCopyright
        {
            get { return (string)GetValue(SoftwareCopyrightProperty); }
            set { SetValue(SoftwareCopyrightProperty, value); }
        }

        /// <summary>
        /// The software image/icon displayed in the About window.
        /// If not set, auto-populated from the application's main window icon.
        /// </summary>
        public static readonly DependencyProperty SoftwareImageProperty =
            DependencyProperty.Register(nameof(SoftwareImage), typeof(ImageSource), typeof(AboutWindow),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the software image.
        /// </summary>
        public ImageSource SoftwareImage
        {
            get { return (ImageSource)GetValue(SoftwareImageProperty); }
            set { SetValue(SoftwareImageProperty, value); }
        }

        /// <summary>
        /// The organization name displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty OrganizationNameProperty =
            DependencyProperty.Register(nameof(OrganizationName), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata("Risk Management Center"));

        /// <summary>
        /// Gets or sets the organization name.
        /// </summary>
        public string OrganizationName
        {
            get { return (string)GetValue(OrganizationNameProperty); }
            set { SetValue(OrganizationNameProperty, value); }
        }

        /// <summary>
        /// The organization details (address, etc.) displayed in the About window.
        /// </summary>
        public static readonly DependencyProperty OrganizationDetailsProperty =
            DependencyProperty.Register(nameof(OrganizationDetails), typeof(string), typeof(AboutWindow),
            new FrameworkPropertyMetadata("Institute for Water Resources\nU.S. Army Corps of Engineers\n12596 W. Bayaud Ave. Suite 400\nLakewood, CO 80228"));

        /// <summary>
        /// Gets or sets the organization details.
        /// </summary>
        public string OrganizationDetails
        {
            get { return (string)GetValue(OrganizationDetailsProperty); }
            set { SetValue(OrganizationDetailsProperty, value); }
        }

        /// <summary>
        /// The organization image/logo displayed in the About window.
        /// Defaults to the USACE logo.
        /// </summary>
        public static readonly DependencyProperty OrganizationImageProperty =
            DependencyProperty.Register(nameof(OrganizationImage), typeof(ImageSource), typeof(AboutWindow),
            new FrameworkPropertyMetadata(GetDefaultOrganizationImage()));

        /// <summary>
        /// Gets or sets the organization image.
        /// </summary>
        public ImageSource OrganizationImage
        {
            get { return (ImageSource)GetValue(OrganizationImageProperty); }
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
                var uri = new Uri("pack://application:,,,/FrameworkUI;component/Resources/USACE.png", UriKind.Absolute);
                return new BitmapImage(uri);
            }
            catch
            {
                return null;
            }
        }
    }
}
