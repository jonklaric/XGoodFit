using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public static DependencyProperty HeaderTextProperty = DependencyProperty.Register(nameof(HeaderText), typeof(string), typeof(NodeHeader), new FrameworkPropertyMetadata("Header"));

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
        public static DependencyProperty HeaderDescriptionProperty = DependencyProperty.Register(nameof(HeaderDescription), typeof(string), typeof(NodeHeader), new FrameworkPropertyMetadata("Description"));

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
        public static DependencyProperty HeaderFontWeightProperty = DependencyProperty.Register(nameof(HeaderFontWeight), typeof(FontWeight), typeof(NodeHeader), new FrameworkPropertyMetadata(FontWeights.Normal));

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
        public static DependencyProperty HeaderFontSizeProperty = DependencyProperty.Register(nameof(HeaderFontSize), typeof(double), typeof(NodeHeader), new FrameworkPropertyMetadata(12d));

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
        public static DependencyProperty ExpandedImageProperty = DependencyProperty.Register(nameof(ExpandedImage), typeof(ImageSource), typeof(NodeHeader), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the image for the TreeViewItem header when expanded.
        /// </summary>
        public ImageSource ExpandedImage
        {
            get { return (ImageSource)GetValue(ExpandedImageProperty); }
            set { SetValue(ExpandedImageProperty, value); }
        }

        /// <summary>
        /// Static image dependency property.
        /// </summary>
        public static DependencyProperty StaticImageProperty = DependencyProperty.Register(nameof(StaticImage), typeof(ImageSource), typeof(NodeHeader), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the static image for the TreeViewItem header.
        /// </summary>
        public ImageSource StaticImage
        {
            get { return (ImageSource)GetValue(StaticImageProperty); }
            set { SetValue(StaticImageProperty, value); }
        }

        /// <summary>
        /// IsCheckBox dependency property.
        /// </summary>
        public static DependencyProperty IsCheckBoxProperty = DependencyProperty.Register(nameof(IsCheckBox), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(false));

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
        public static DependencyProperty IsCheckedProperty = DependencyProperty.Register(nameof(IsChecked), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(false));

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
        public static DependencyProperty IsExpandedProperty = DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(true));

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
        public static DependencyProperty ImageVisibilityProperty = DependencyProperty.Register(nameof(ImageVisibility), typeof(Visibility), typeof(NodeHeader), new FrameworkPropertyMetadata(Visibility.Visible));

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
        public static DependencyProperty ShowAsteriskProperty = DependencyProperty.Register(nameof(ShowAsterisk), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(false));

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
        public static DependencyProperty ShowToolTipProperty = DependencyProperty.Register(nameof(ShowToolTip), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(true));

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
        public static DependencyProperty RenameTextProperty = DependencyProperty.Register(nameof(RenameText), typeof(string), typeof(NodeHeader), new FrameworkPropertyMetadata(""));

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
        public static DependencyProperty ShowRenameTextBoxProperty = DependencyProperty.Register(nameof(ShowRenameTextBox), typeof(bool), typeof(NodeHeader), new UIPropertyMetadata(false));

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
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var formattedText = new FormattedText(candidate, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface(HeaderTextBlock.FontFamily, HeaderTextBlock.FontStyle, HeaderTextBlock.FontWeight, HeaderTextBlock.FontStretch), HeaderTextBlock.FontSize, Brushes.Black, pixelsPerDip);
            return new Size(formattedText.Width, formattedText.Height);
        }

    }
}
