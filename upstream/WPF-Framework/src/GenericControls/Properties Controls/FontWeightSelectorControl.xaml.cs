using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(FontWeightSelectorControl), new UIPropertyMetadata("Title"));
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
        public static readonly DependencyProperty SelectedFontWeightProperty = DependencyProperty.Register(nameof(SelectedFontWeight), typeof(FontWeight), typeof(FontWeightSelectorControl), new UIPropertyMetadata(FontWeights.Normal));

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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(FontWeightSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(FontWeightSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(FontWeightSelectorControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(FontWeightSelectorControl), new UIPropertyMetadata(true));
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
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles control resizing and updates <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The control being resized.</param>
        /// <param name="e">Event data for the size change.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }
    }
}