using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        public static List<VerticalAlignment> AlignmentOptions { get; private set; } = new List<VerticalAlignment>((VerticalAlignment[])Enum.GetValues(typeof(VerticalAlignment)));

        /// <summary>
        /// A precomputed list of <see cref="VerticalAlignment"/> values excluding <see cref="VerticalAlignment.Stretch"/>.
        /// </summary>
        private static readonly List<VerticalAlignment> _optionsWithoutStretch = AlignmentOptions.Where(a => a != VerticalAlignment.Stretch).ToList();

        /// <summary>
        /// Identifies the <see cref="Alignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlignmentProperty = DependencyProperty.Register(nameof(Alignment), typeof(VerticalAlignment), typeof(VerticalAlignmentControl), new UIPropertyMetadata(VerticalAlignment.Stretch));

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
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(VerticalAlignmentControl), new UIPropertyMetadata("Title"));
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
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(VerticalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
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
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(VerticalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
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
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(VerticalAlignmentControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
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
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(VerticalAlignmentControl), new UIPropertyMetadata(true));
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
        public static readonly DependencyProperty ShowStretchProperty = DependencyProperty.Register(nameof(ShowStretch), typeof(bool), typeof(VerticalAlignmentControl), new UIPropertyMetadata(true, OnShowStretchChanged));

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

        /// <summary>
        /// Handles changes to the <see cref="ShowStretch"/> property by raising <see cref="PropertyChanged"/>
        /// for <see cref="FilteredAlignmentOptions"/> so the ComboBox re-binds.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnShowStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Updates the <see cref="ActualPropertyWidth"/> when the control's size changes.
        /// </summary>
        /// <param name="sender">The source element that triggered the event.</param>
        /// <param name="e">The size changed event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }

    }
}