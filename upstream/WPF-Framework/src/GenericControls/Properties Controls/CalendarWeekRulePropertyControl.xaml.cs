using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GenericControls
{
    /// <summary>
    /// A user control for selecting <see cref="CalendarWeekRule"/> value with configurable layout properties.
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
    public partial class CalendarWeekRulePropertyControl:UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarWeekRulePropertyControl"/> class.
        /// </summary>
        public CalendarWeekRulePropertyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// A list of available <see cref="CalendarWeekRule"/> options for display or binding.
        /// </summary>
        public static List<CalendarWeekRule> CalendarRuleOptions { get; private set; } = new List<CalendarWeekRule>((CalendarWeekRule[])Enum.GetValues(typeof(CalendarWeekRule)));

        /// <summary>
        /// Dependency property for <see cref="SelectedCalendarWeekRule"/>
        /// </summary>
        public static readonly DependencyProperty SelectedCalendarWeekRuleProperty = DependencyProperty.Register(nameof(SelectedCalendarWeekRule), typeof(CalendarWeekRule), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata(CalendarWeekRule.FirstDay));

        /// <summary>
        /// Gets/sets the selected <see cref="CalendarWeekRule"/>
        /// </summary>
        public CalendarWeekRule SelectedCalendarWeekRule
        {
            get
            {
                return (CalendarWeekRule)this.GetValue(SelectedCalendarWeekRuleProperty);
            }
            set
            {
                this.SetValue(SelectedCalendarWeekRuleProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for <see cref="Title"/>
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata("Title"));
        /// <summary>
        /// Gets/sets the title displayed for the property control.
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
        /// Dependency property for <see cref="MaxPropertyWidth"/>
        /// </summary>
        public static readonly DependencyProperty MaxPropertyWidthProperty = DependencyProperty.Register(nameof(MaxPropertyWidth), typeof(double), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMaxPropertyWidth));
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
        /// Dependency property for <see cref="MinPropertyWidth"/>
        /// </summary>
        public static readonly DependencyProperty MinPropertyWidthProperty = DependencyProperty.Register(nameof(MinPropertyWidth), typeof(double), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultMinPropertyWidth));
        /// <summary>
        /// Gets/sets the minimum width of the property area.
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
        /// Dependency property for <see cref="PropertyWidth"/>
        /// </summary>
        public static readonly DependencyProperty PropertyWidthProperty = DependencyProperty.Register(nameof(PropertyWidth), typeof(GridLength), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata(PropertyDefaults.DefaultPropertyWidth));
        /// <summary>
        /// Gets/sets the width of the property column in the layout.
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
        /// Dependency property for <see cref="ShowLeaderLine"/>
        /// </summary>
        public static readonly DependencyProperty ShowLeaderLineProperty = DependencyProperty.Register(nameof(ShowLeaderLine), typeof(bool), typeof(CalendarWeekRulePropertyControl), new UIPropertyMetadata(true));
        /// <summary>
        /// Gets/sets a value indicating whether the leader line should be shown.
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
        /// Gets the actual rendered width of the property control area. Raises <see cref="PropertyChanged"/> when updated.
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
        /// Handles the size change event to update <see cref="ActualPropertyWidth"/>.
        /// </summary>
        /// <param name="sender">The framework element whose size changed.</param>
        /// <param name="e">Size changed event arguments.</param>
        private void ControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            FrameworkElement el = sender as FrameworkElement;
            ActualPropertyWidth = el.ActualWidth;
        }
    }
}