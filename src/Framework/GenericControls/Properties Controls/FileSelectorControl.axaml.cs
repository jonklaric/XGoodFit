using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace GenericControls
{
    /// <summary>
    /// A user control for selecting a file path with a browse button and file type filtering.
    /// </summary>
    /// <remarks>
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
    /// <para>
    /// XGoodFit port: the properties are Avalonia styled properties. The text box's TextChanging raises
    /// <see cref="TextChanged"/>: Avalonia raises its TextChanged after rendering, WPF as the text changes.
    /// The file dialog is WpfCompat's, through <see cref="GeneralMethods.FileOpenDialog"/>; it waits for
    /// the user, as WPF's does.
    /// </para>
    /// </remarks>
    public partial class FileSelectorControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSelectorControl"/> class.
        /// </summary>
        public FileSelectorControl()
        {
            InitializeComponent();

            // XGoodFit port: Avalonia raises TextChanged after the change is rendered; WPF raises it as the
            // text changes, which TextChanging matches.
            this.FilePathTextBox.TextChanging += (sender, e) => FilePathTextBox_TextChanged(sender, new TextChangedEventArgs(TextBox.TextChangedEvent, this.FilePathTextBox));
        }

        static FileSelectorControl()
        {
            // XGoodFit port: upstream's FrameworkPropertyMetadataOptions.AffectsRender.
            AffectsRender<FileSelectorControl>(ShowLeaderLineProperty, ShowTitleProperty);
        }

        #region Members

        /// <summary>
        /// Dependency property for the file filters property.
        /// </summary>
        public static readonly StyledProperty<string> FileFiltersProperty = AvaloniaProperty.Register<FileSelectorControl, string>(nameof(FileFilters), "All files (*.*) |*.*");

        /// <summary>
        /// Gets or sets the file filter string for the file dialog.
        /// </summary>
        public string FileFilters
        {
            get
            {
                return (string)this.GetValue(FileFiltersProperty);
            }
            set
            {
                this.SetValue(FileFiltersProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the text property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<FileSelectorControl, string>(nameof(Text), "");

        /// <summary>
        /// Gets or sets the file path text.
        /// </summary>
        public string Text
        {
            get
            {
                return (string)this.GetValue(TextProperty);
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<FileSelectorControl, bool>(nameof(IsReadOnly), false);

        /// <summary>
        /// Gets or sets a value indicating whether the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                return (bool)this.GetValue(IsReadOnlyProperty);
            }
            set
            {
                this.SetValue(IsReadOnlyProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the title property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<FileSelectorControl, string>(nameof(Title), "Title");

        /// <summary>
        /// Gets or sets the title text displayed for the control.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)this.GetValue(TitleProperty);
            }
            set
            {
                this.SetValue(TitleProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the property width.
        /// </summary>
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<FileSelectorControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);

        /// <summary>
        /// Gets or sets the width of the property column in the layout.
        /// </summary>
        public GridLength PropertyWidth
        {
            get
            {
                return (GridLength)this.GetValue(PropertyWidthProperty);
            }
            set
            {
                this.SetValue(PropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the maximum property width.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<FileSelectorControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);

        /// <summary>
        /// Gets or sets the maximum width of the property area.
        /// </summary>
        public double MaxPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MaxPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MaxPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the minimum property width.
        /// </summary>
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<FileSelectorControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);

        /// <summary>
        /// Gets or sets the minimum width of the property area.
        /// </summary>
        public double MinPropertyWidth
        {
            get
            {
                return (double)this.GetValue(MinPropertyWidthProperty);
            }
            set
            {
                this.SetValue(MinPropertyWidthProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the show leader line property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<FileSelectorControl, bool>(nameof(ShowLeaderLine), false);

        /// <summary>
        /// Gets or sets a value indicating whether the leader line should be visible.
        /// </summary>
        public bool ShowLeaderLine
        {
            get
            {
                return (bool)this.GetValue(ShowLeaderLineProperty);
            }
            set
            {
                this.SetValue(ShowLeaderLineProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the show title property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowTitleProperty = AvaloniaProperty.Register<FileSelectorControl, bool>(nameof(ShowTitle), true);

        /// <summary>
        /// Gets or sets a value indicating whether the title should be visible.
        /// </summary>
        public bool ShowTitle
        {
            get
            {
                return (bool)this.GetValue(ShowTitleProperty);
            }
            set
            {
                this.SetValue(ShowTitleProperty, value);
            }
        }

        #endregion

        private double _actualWidth = 0d;

        /// <summary>
        /// Gets the actual rendered width of the control.
        /// Updates when the control's size changes.
        /// </summary>
        public double ActualPropertyWidth
        {
            get
            {
                return _actualWidth;
            }
            private set
            {
                if (_actualWidth != value)
                {
                    _actualWidth = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ActualPropertyWidth)));
                }
            }
        }

        /// <summary>
        /// Occurs when a property value changes, primarily for <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <remarks>XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event; this class's
        /// <see cref="INotifyPropertyChanged"/> implementation raises this one.</remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Occurs when the text is changed in the associated text box.
        /// </summary>
        public event TextChangedEventHandler TextChanged;

        /// <summary>
        /// Delegate for the <see cref="TextChanged"/> event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event arguments containing change details.</param>
        public delegate void TextChangedEventHandler(object sender, TextChangedEventArgs e);

        /// <summary>
        /// Handles the control's <see cref="Control.SizeChanged"/> event to update the <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The control that triggered the event.</param>
        /// <param name="e">Size changed event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }

        /// <summary>
        /// Opens a file dialog and updates the <see cref="Text"/> property with the selected path.
        /// </summary>
        /// <param name="sender">The button triggering the event.</param>
        /// <param name="e">Routed event arguments.</param>
        private void FilePathButton_Click(object sender, RoutedEventArgs e)
        {
            string fileName = GeneralMethods.FileOpenDialog(FileFilters);
            if (fileName != null && !string.IsNullOrEmpty(fileName))
                Text = fileName;
        }

        /// <summary>
        /// Raises the <see cref="TextChanged"/> event when the file text box changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Text changed event arguments.</param>
        private void FilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextChanged?.Invoke(sender, e);
        }
    }
}