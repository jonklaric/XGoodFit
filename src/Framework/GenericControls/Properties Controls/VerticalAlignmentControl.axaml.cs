using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace GenericControls
{
    /// <summary>
    /// A WPF UserControl that allows selection of a <see cref="VerticalAlignment"/> value with a labeled title and customizable layout of options.
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
    /// XGoodFit port: the properties are Avalonia styled properties, the alignment is Avalonia's, and
    /// the Stretch option's change callback is a class handler.
    /// </para>
    /// </remarks>
    public partial class VerticalAlignmentControl :UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VerticalAlignmentControl"/> class.
        /// </summary>
        public VerticalAlignmentControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the list of available <see cref="VerticalAlignment"/> options.
        /// </summary>
        public static List<VerticalAlignment> AlignmentOptions { get; private set; } = new List<VerticalAlignment> { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.Stretch };

        /// <summary>
        /// A precomputed list of <see cref="VerticalAlignment"/> values excluding <see cref="VerticalAlignment.Stretch"/>.
        /// </summary>
        private static readonly List<VerticalAlignment> _optionsWithoutStretch = AlignmentOptions.Where(a => a != VerticalAlignment.Stretch).ToList();

        /// <summary>
        /// Identifies the <see cref="Alignment"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> AlignmentProperty = AvaloniaProperty.Register<VerticalAlignmentControl, VerticalAlignment>(nameof(Alignment), VerticalAlignment.Stretch);

        /// <summary>
        /// Gets or sets the selected vertical alignment value.
        /// </summary>
        public VerticalAlignment Alignment
        {
            get
            {
                return (VerticalAlignment)this.GetValue(AlignmentProperty);
            }
            set
            {
                this.SetValue(AlignmentProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<VerticalAlignmentControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the label title for the control.
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
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<VerticalAlignmentControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);
        /// <summary>
        /// Gets or sets the maximum width allowed for the control layout.
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
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<VerticalAlignmentControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);
        /// <summary>
        /// Gets or sets the minimum width allowed for the control layout.
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
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<VerticalAlignmentControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);
        /// <summary>
        /// Gets or sets the width of the control using GridLength (e.g., auto, star, fixed).
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
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<VerticalAlignmentControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets whether the control displays a visual leader line.
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
        public static readonly StyledProperty<bool> ShowStretchProperty = AvaloniaProperty.Register<VerticalAlignmentControl, bool>(nameof(ShowStretch), true);

        /// <summary>
        /// Gets or sets a value indicating whether the <see cref="VerticalAlignment.Stretch"/> option
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

        static VerticalAlignmentControl()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ShowStretchProperty.Changed.AddClassHandler<VerticalAlignmentControl>(OnShowStretchChanged);
        }

        /// <summary>
        /// Handles changes to the <see cref="ShowStretch"/> property by raising <see cref="PropertyChanged"/>
        /// for <see cref="FilteredAlignmentOptions"/> so the ComboBox re-binds.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnShowStretchChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d is VerticalAlignmentControl control)
                control.PropertyChanged?.Invoke(control, new PropertyChangedEventArgs(nameof(FilteredAlignmentOptions)));
        }

        /// <summary>
        /// Gets the list of alignment options filtered by the <see cref="ShowStretch"/> setting.
        /// Returns the full list when <see cref="ShowStretch"/> is <c>true</c>, or a list excluding
        /// <see cref="VerticalAlignment.Stretch"/> when <c>false</c>.
        /// </summary>
        public List<VerticalAlignment> FilteredAlignmentOptions
        {
            get { return ShowStretch ? AlignmentOptions : _optionsWithoutStretch; }
        }

        private double _actualWidth = 0d;
        /// <summary>
        /// Gets the current rendered width of the property control.
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
        /// <remarks>XGoodFit port: <c>new</c> hides Avalonia's own PropertyChanged event; this class's
        /// <see cref="INotifyPropertyChanged"/> implementation raises this one.</remarks>
        public new event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control's size changes.
        /// </summary>
        /// <param name="sender">The source element that triggered the event.</param>
        /// <param name="e">The size changed event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }

    }
}