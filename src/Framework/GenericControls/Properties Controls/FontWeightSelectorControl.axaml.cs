using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using WpfCompat.Media;

namespace GenericControls
{
    /// <summary>
    /// A user control that allows selection of a <see cref="FontWeight"/> value with adjustable layout properties.
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
    /// XGoodFit port: the properties are Avalonia styled properties, and the weight is Avalonia's
    /// <see cref="FontWeight"/>; <see cref="FontWeights"/> is WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class FontWeightSelectorControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FontWeightSelectorControl"/> class.
        /// </summary>
        public FontWeightSelectorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<FontWeightSelectorControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the title displayed for this control.
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
        /// Identifies the <see cref="SelectedFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> SelectedFontWeightProperty = AvaloniaProperty.Register<FontWeightSelectorControl, FontWeight>(nameof(SelectedFontWeight), FontWeights.Normal);

        /// <summary>
        /// Gets or sets the selected <see cref="FontWeight"/>.
        /// </summary>
        public FontWeight SelectedFontWeight
        {
            get
            {
                return (FontWeight)this.GetValue(SelectedFontWeightProperty);
            }
            set
            {
                this.SetValue(SelectedFontWeightProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="MaxPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<FontWeightSelectorControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);
        /// <summary>
        /// Gets or sets the maximum width allowed for the property display.
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
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<FontWeightSelectorControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);
        /// <summary>
        /// Gets or sets the minimum width allowed for the property display.
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
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<FontWeightSelectorControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);
        /// <summary>
        /// Gets or sets the desired layout width for the property column.
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
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<FontWeightSelectorControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets whether to show a visual leader line next to the property.
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
        /// Gets the actual rendered width of the property area.
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
        /// Handles control resizing and updates <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The control being resized.</param>
        /// <param name="e">Event data for the size change.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }
    }
}