using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;

namespace GenericControls
{
    /// <summary>
    /// A WPF user control that allows editing of a <see cref="DateTime"/> value with configurable title, layout, and read-only support.
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
    /// XGoodFit port: the properties are Avalonia styled properties.
    /// </para>
    /// </remarks>
    public partial class DateTimePropertyControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimePropertyControl"/> class.
        /// </summary>
        public DateTimePropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Identifies the <see cref="SelectedDateTime"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTime> SelectedDateTimeProperty = AvaloniaProperty.Register<DateTimePropertyControl, DateTime>(nameof(SelectedDateTime), new DateTime(2000, 1, 1, 0, 0, 0));
        /// <summary>
        /// Gets or sets the selected <see cref="DateTime"/> value.
        /// </summary>
        public DateTime SelectedDateTime
        {
            get
            {
                return (DateTime)this.GetValue(SelectedDateTimeProperty);
            }
            set
            {
                this.SetValue(SelectedDateTimeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property. 
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<DateTimePropertyControl, string>(nameof(Title), "Title");
        /// <summary>
        /// Gets or sets the title text shown next to the DateTime input.
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
        /// Identifies the <see cref="MinTitleWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinTitleWidthProperty = AvaloniaProperty.Register<DateTimePropertyControl, double>(nameof(MinTitleWidth), 100d);
        /// <summary>
        /// Gets or sets the minimum width for the title label.
        /// </summary>
        public double MinTitleWidth
        {
            get
            {
                return (double)this.GetValue(MinTitleWidthProperty);
            }
            set
            {
                this.SetValue(MinTitleWidthProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsReadOnlyProperty = AvaloniaProperty.Register<DateTimePropertyControl, bool>(nameof(IsReadOnly), false);
        /// <summary>
        /// Gets or sets whether the DateTime input is read-only.
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
        /// Identifies the <see cref="MaxPropertyWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaxPropertyWidthProperty = AvaloniaProperty.Register<DateTimePropertyControl, double>(nameof(MaxPropertyWidth), PropertyDefaults.DefaultMaxPropertyWidth);
        /// <summary>
        /// Gets or sets the maximum width allowed for the DateTime input area.
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
        public static readonly StyledProperty<double> MinPropertyWidthProperty = AvaloniaProperty.Register<DateTimePropertyControl, double>(nameof(MinPropertyWidth), PropertyDefaults.DefaultMinPropertyWidth);
        /// <summary>
        /// Gets or sets the minimum width allowed for the DateTime input area.
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
        public static readonly StyledProperty<GridLength> PropertyWidthProperty = AvaloniaProperty.Register<DateTimePropertyControl, GridLength>(nameof(PropertyWidth), PropertyDefaults.DefaultPropertyWidth);
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
        /// Identifies the <see cref="ShowLeaderLine"/> dependency property. 
        /// </summary>
        public static readonly StyledProperty<bool> ShowLeaderLineProperty = AvaloniaProperty.Register<DateTimePropertyControl, bool>(nameof(ShowLeaderLine), true);
        /// <summary>
        /// Gets or sets whether to display a visual leader line alongside the property control.
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
        /// Gets the actual rendered width of the DateTime input area.
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
        /// Handles size changes of the control to update the <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The control raising the event.</param>
        /// <param name="e">Size change event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // XGoodFit port: WPF's FrameworkElement and ActualWidth are Avalonia's Control and Bounds.Width.
            Control el = sender as Control;
            ActualPropertyWidth = el.Bounds.Width;
        }
    }
}