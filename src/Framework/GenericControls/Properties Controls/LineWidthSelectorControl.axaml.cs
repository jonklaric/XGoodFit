using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using WpfCompat;

namespace GenericControls
{
    /// <summary>
    /// A WPF user control that allows the user to select a line width from a predefined set or via direct input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
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
    /// XGoodFit port: the properties are Avalonia styled properties. The text-input handler, which
    /// upstream's XAML doesn't use, finds the template part with WpfCompat's <c>FindName</c>.
    /// </para>
    /// </remarks>
    public partial class LineWidthSelectorControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LineWidthSelectorControl"/> class.
        /// </summary>
        public LineWidthSelectorControl()
        {
            InitializeComponent();
            if (WidthOptions == null)
            {
                WidthOptions = new List<double> { 0d, 1d, 2d, 3d, 4d, 5d };
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> SelectedWidthProperty = AvaloniaProperty.Register<LineWidthSelectorControl, double>(nameof(SelectedWidth), 0d);
        /// <summary>
        /// Gets or sets the selected line width.
        /// </summary>
        public double SelectedWidth
        {
            get
            {
                return (double)this.GetValue(SelectedWidthProperty);
            }
            set
            {
                this.SetValue(SelectedWidthProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="WidthOptions"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        // Per-instance default initialized in the constructor.
        public static readonly StyledProperty<IList<double>> WidthOptionsProperty = AvaloniaProperty.Register<LineWidthSelectorControl, IList<double>>(nameof(WidthOptions), null);
        /// <summary>
        /// Gets or sets the list of selectable line width options.
        /// </summary>
        public IList<double> WidthOptions
        {
            get
            {
                return (IList<double>)this.GetValue(WidthOptionsProperty);
            }
            set
            {
                this.SetValue(WidthOptionsProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<LineWidthSelectorControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the label text shown next to the width selector.
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
        /// Identifies the <see cref="MaxPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<LineWidthSelectorControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);
        /// <summary>
        /// Gets or sets the maximum width of the label area.
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
        /// Identifies the <see cref="MinPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<LineWidthSelectorControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);
        /// <summary>
        /// Gets or sets the minimum width of the label area.
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
        /// Identifies the <see cref="PropertyWidth"/> dependency property. 
        /// </summary>
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<LineWidthSelectorControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);
        /// <summary>
        /// Gets or sets the column width allocated to the property selector.
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
        /// Identifies the <see cref="ShowLeaderLine"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<LineWidthSelectorControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets a value indicating whether a line should visually connect the title to control.
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

        private double _actualWidth = 0d;
        /// <summary>
        /// Gets the current rendered width of the control.
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
        /// Event raised when a property value changes. 
        /// </summary>
        /// <remarks>XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event; this class's
        /// <see cref="INotifyPropertyChanged"/> implementation raises this one.</remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control is resized.
        /// </summary>
        /// <param name="sender">The sender (framework element).</param>
        /// <param name="e">Size changed event data.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }

        /// <summary>
        /// Restricts ComboBox text input to digits, decimal points, and prevents invalid characters like space.
        /// </summary>
        /// <param name="sender">The ComboBox receiving text input.</param>
        /// <param name="e">Text composition event data.</param>
        private void ComboBox_PreviewTextInput(object sender, TextInputEventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            // Get the TextBox from the ComboBox template to check cursor position
            TextBox editableTextBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
            // XGoodFit port: WPF's SelectionStart is where the selection starts; Avalonia's is where it was
            // started, which can be its end.
            int selectionStart = editableTextBox is null ? 0 : Math.Min(editableTextBox.SelectionStart, editableTextBox.SelectionEnd);
            string selectedText = editableTextBox?.SelectedText;

            // Line width is positive only, no negative values
            e.Handled = !NumberFormatHelper.IsValidNumericInput(
                e.Text,
                comboBox.Text,
                selectionStart,
                selectedText,
                allowNegative: false,
                allowDecimal: true,
                allowScientific: false);
        }

        /// <summary>
        /// Prevents spacebar key input in the ComboBox.
        /// </summary>
        /// <param name="sender">The ComboBox receiving key input.</param>
        /// <param name="e">Key event data.</param>
        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
                e.Handled = true;
        }
    }
}