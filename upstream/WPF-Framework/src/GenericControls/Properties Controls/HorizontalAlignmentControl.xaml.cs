using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GenericControls
{
    /// <summary>
    /// A user control that allows the user to select a <see cref="HorizontalAlignment"/> value,
    /// with configurable layout and property metadata.
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
    public partial class HorizontalAlignmentControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HorizontalAlignmentControl"/> class.
        /// </summary>
        public HorizontalAlignmentControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets a list of all possible <see cref="HorizontalAlignment"/> values.
        /// </summary>
        public static List<HorizontalAlignment> AlignmentOptions { get; private set; } = new List<HorizontalAlignment>((HorizontalAlignment[])Enum.GetValues(typeof(HorizontalAlignment)));

        /// <summary>
        /// A precomputed list of <see cref="HorizontalAlignment"/> values excluding <see cref="HorizontalAlignment.Stretch"/>.
        /// </summary>
        private static readonly List<HorizontalAlignment> _optionsWithoutStretch = AlignmentOptions.Where(a => a != HorizontalAlignment.Stretch).ToList();

        /// <summary>
        /// Identifies the <see cref="Alignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(nameof(Alignment), typeof(HorizontalAlignment), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(HorizontalAlignment.Stretch));

        /// <summary>
        /// Gets or sets the currently selected <see cref="HorizontalAlignment"/>.
        /// </summary>
        public HorizontalAlignment Alignment
        {
            get
            {
                return (HorizontalAlignment)this.GetValue(AlignmentProperty);
            }
            set
            {
                this.SetValue(AlignmentProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(HorizontalAlignmentControl), new UIPropertyMetadata("Title"));
        /// <summary>
        /// Gets or sets the display title of the control.
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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
        /// <summary>
        /// Gets or sets the maximum width of the property label section.
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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
        /// <summary>
        /// Gets or sets the minimum width of the property label section.
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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
        /// <summary>
        /// Gets or sets the width of the property label section.
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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(true));
        /// <summary>
        /// Gets or sets a value indicating whether a leader line should be displayed next to the property label.
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
        /// Identifies the <see cref="ShowStretch"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStretchProperty = DependencyProperty.Register(nameof(ShowStretch), typeof(bool), typeof(HorizontalAlignmentControl), new UIPropertyMetadata(true, OnShowStretchChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="HorizontalAlignment.Stretch"/> option
        /// is included in the alignment dropdown. Default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// Set to <c>false</c> for contexts where Stretch has no meaning, such as OxyPlot annotation
        /// and legend alignment controls.
        /// </remarks>
        public bool ShowStretch
        {
            get
            {
                return (bool)this.GetValue(ShowStretchProperty);
            }
            set
            {
                this.SetValue(ShowStretchProperty, value);
            }
        }

        /// <summary>
        /// Handles changes to the <see cref="ShowStretch"/> property by raising <see cref="PropertyChanged"/>
        /// for <see cref="FilteredAlignmentOptions"/> so the ComboBox re-binds.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnShowStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HorizontalAlignmentControl control)
                control.PropertyChanged?.Invoke(control, new PropertyChangedEventArgs(nameof(FilteredAlignmentOptions)));
        }

        /// <summary>
        /// Gets the list of alignment options filtered by the <see cref="ShowStretch"/> setting.
        /// Returns the full list when <see cref="ShowStretch"/> is <c>true</c>, or a list excluding
        /// <see cref="HorizontalAlignment.Stretch"/> when <c>false</c>.
        /// </summary>
        public List<HorizontalAlignment> FilteredAlignmentOptions
        {
            get { return ShowStretch ? AlignmentOptions : _optionsWithoutStretch; }
        }

        private double _actualWidth = 0d;
        /// <summary>
        /// Gets the actual rendered width of the control.
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
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles changes in the control's size and updates <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The element whose size changed.</param>
        /// <param name="e">The size change event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }

    }
}