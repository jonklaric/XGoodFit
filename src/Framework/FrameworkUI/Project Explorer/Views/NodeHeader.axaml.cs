using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WpfCompat;
using WpfCompat.Media;

namespace FrameworkUI.ProjectExplorer
{
    /// <summary>
    /// Interaction logic for NodeHeader.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authors:</b>
    /// </para>
    /// <list type="bullet">
    ///     <item>Woody Fields</item>
    ///     <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    /// </list>
    /// <para>
    /// XGoodFit port: the dependency properties are styled properties with the same defaults, and
    /// <see cref="ImageVisibility"/> is WpfCompat's <see cref="Visibility"/>. The text is measured with
    /// Avalonia's <see cref="FormattedText"/>, which needs no pixels-per-dip.
    /// </para>
    /// </remarks>
    public partial class NodeHeader : UserControl
    {

        /// <summary>
        /// Construct Project Explorer TreeViewItem header.
        /// </summary>
        public NodeHeader()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
        }

        /// <summary>
        /// Header text dependency property.
        /// </summary>
        public static readonly StyledProperty<string> HeaderTextProperty = AvaloniaProperty.Register<NodeHeader, string>(nameof(HeaderText), "Header");

        /// <summary>
        /// Gets or sets the header text.
        /// </summary>
        public string HeaderText
        {
            get { return GetValue(HeaderTextProperty)?.ToString() ?? string.Empty; }
            set { SetValue(HeaderTextProperty, value); }
        }

        /// <summary>
        /// Header description dependency property.
        /// </summary>
        public static readonly StyledProperty<string> HeaderDescriptionProperty = AvaloniaProperty.Register<NodeHeader, string>(nameof(HeaderDescription), "Description");

        /// <summary>
        /// Gets or sets the header description.
        /// </summary>
        public string HeaderDescription
        {
            get { return GetValue(HeaderDescriptionProperty)?.ToString() ?? string.Empty; }
            set { SetValue(HeaderDescriptionProperty, value); }
        }

        /// <summary>
        /// Header font weight dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> HeaderFontWeightProperty = AvaloniaProperty.Register<NodeHeader, FontWeight>(nameof(HeaderFontWeight), FontWeights.Normal);

        /// <summary>
        /// Gets or sets the header font weight.
        /// </summary>
        public FontWeight HeaderFontWeight
        {
            get { return (FontWeight)GetValue(HeaderFontWeightProperty); }
            set { SetValue(HeaderFontWeightProperty, value); }
        }

        /// <summary>
        /// Header font size dependency property.
        /// </summary>
        public static readonly StyledProperty<double> HeaderFontSizeProperty = AvaloniaProperty.Register<NodeHeader, double>(nameof(HeaderFontSize), 12d);

        /// <summary>
        /// Gets or sets the header font size.
        /// </summary>
        public double HeaderFontSize
        {
            get { return (double)GetValue(HeaderFontSizeProperty); }
            set { SetValue(HeaderFontSizeProperty, value); }
        }

        /// <summary>
        /// Expanded image dependency property.
        /// </summary>
        public static readonly StyledProperty<ImageSource?> ExpandedImageProperty = AvaloniaProperty.Register<NodeHeader, ImageSource?>(nameof(ExpandedImage), null);

        /// <summary>
        /// Gets or sets the image for the TreeViewItem header when expanded.
        /// </summary>
        public ImageSource ExpandedImage
        {
            get { return (ImageSource)GetValue(ExpandedImageProperty)!; }
            set { SetValue(ExpandedImageProperty, value); }
        }

        /// <summary>
        /// Static image dependency property.
        /// </summary>
        public static readonly StyledProperty<ImageSource?> StaticImageProperty = AvaloniaProperty.Register<NodeHeader, ImageSource?>(nameof(StaticImage), null);

        /// <summary>
        /// Gets or sets the static image for the TreeViewItem header.
        /// </summary>
        public ImageSource StaticImage
        {
            get { return (ImageSource)GetValue(StaticImageProperty)!; }
            set { SetValue(StaticImageProperty, value); }
        }

        /// <summary>
        /// IsCheckBox dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckBoxProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(IsCheckBox), false);

        /// <summary>
        /// Determines whether the TreeViewItem header is a check box header.
        /// </summary>
        public bool IsCheckBox
        {
            get { return (bool)GetValue(IsCheckBoxProperty); }
            set { SetValue(IsCheckBoxProperty, value); }
        }

        /// <summary>
        /// IsCheckBox dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(IsChecked), false);

        /// <summary>
        /// Determines whether the header is checked.
        /// </summary>
        public bool IsChecked
        {
            get { return (bool)GetValue(IsCheckedProperty); }
            set { SetValue(IsCheckedProperty, value); }
        }

        /// <summary>
        /// IsExpanded dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(IsExpanded), true);

        /// <summary>
        /// Determines whether the TreeViewItem header is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return (bool)GetValue(IsExpandedProperty); }
            set { SetValue(IsExpandedProperty, value); }
        }

        /// <summary>
        /// Image visibility dependency property.
        /// </summary>
        public static readonly StyledProperty<Visibility> ImageVisibilityProperty = AvaloniaProperty.Register<NodeHeader, Visibility>(nameof(ImageVisibility), Visibility.Visible);

        /// <summary>
        /// Gets or sets the visibility of the TreeViewItem header image.
        /// </summary>
        public Visibility ImageVisibility
        {
            get { return (Visibility)GetValue(ImageVisibilityProperty); }
            set { SetValue(ImageVisibilityProperty, value); }
        }

        /// <summary>
        /// Show asterisk dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowAsteriskProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(ShowAsterisk), false);

        /// <summary>
        /// Gets or sets whether to show an asterisk next to the header text.
        /// </summary>
        public bool ShowAsterisk
        {
            get { return (bool)GetValue(ShowAsteriskProperty); }
            set { SetValue(ShowAsteriskProperty, value); }
        }

        /// <summary>
        /// Show tooltip dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowToolTipProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(ShowToolTip), true);

        /// <summary>
        /// Gets or sets whether to show the header tooltip.
        /// </summary>
        public bool ShowToolTip
        {
            get { return (bool)GetValue(ShowToolTipProperty); }
            set { SetValue(ShowToolTipProperty, value); }
        }

        /// <summary>
        /// Rename text box dependency property.
        /// </summary>
        public static readonly StyledProperty<string> RenameTextProperty = AvaloniaProperty.Register<NodeHeader, string>(nameof(RenameText), "");

        /// <summary>
        /// Gets or sets the rename text box text.
        /// </summary>
        public string RenameText
        {
            get { return GetValue(RenameTextProperty)?.ToString() ?? string.Empty; }
            set { SetValue(RenameTextProperty, value); }
        }

        /// <summary>
        /// Show rename dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowRenameTextBoxProperty = AvaloniaProperty.Register<NodeHeader, bool>(nameof(ShowRenameTextBox), false);

        /// <summary>
        /// Gets or sets whether to show the header tooltip.
        /// </summary>
        public bool ShowRenameTextBox
        {
            get { return (bool)GetValue(ShowRenameTextBoxProperty); }
            set
            {
                SetValue(ShowRenameTextBoxProperty, value);
                // This is a hack to make sure the rename text box and header resize with the text.
                // This should be changed in the future if possible.
                var textSize = MeasureString(RenameTextBox.Text);
                if (value == true)
                {
                    RenameTextBox.Width = textSize.Width + 8d;
                    HeaderTextBlock.Width = RenameTextBox.Width;
                }
                else
                {
                    textSize = MeasureString(HeaderText);
                    RenameTextBox.Width = textSize.Width;
                    HeaderTextBlock.Width = double.NaN;
                }
            }
        }

        /// <summary>
        /// Rename text box changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RenameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This is a hack to make sure the rename text box and header resize with the text.
            // This should be changed in the future if possible.
            var textSize = MeasureString(RenameTextBox.Text);
            RenameTextBox.Width = textSize.Width + 8d;
            HeaderTextBlock.Width = RenameTextBox.Width;
        }

        /// <summary>
        /// Support method used to size text box.
        /// </summary>
        /// <param name="candidate">The string to measure.</param>
        /// <returns>The size of the string when rendered.</returns>
        private Size MeasureString(string candidate)
        {
            // XGoodFit port: Avalonia's FormattedText takes no pixels-per-dip, and its text may not be null.
            var formattedText = new FormattedText(candidate ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(HeaderTextBlock.FontFamily, HeaderTextBlock.FontStyle, HeaderTextBlock.FontWeight, HeaderTextBlock.FontStretch), HeaderTextBlock.FontSize, Brushes.Black);
            return new Size(formattedText.Width, formattedText.Height);
        }

    }
}
