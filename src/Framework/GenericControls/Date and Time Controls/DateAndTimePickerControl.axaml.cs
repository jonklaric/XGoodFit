using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace GenericControls
{
    /// <summary>
    /// A user control for selecting both date and time using a calendar and a clock interface.
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
    /// XGoodFit port: the properties are Avalonia styled properties, and the date's change callback a
    /// class handler. The calendar's events are Avalonia's.
    /// </para>
    /// </remarks>
    public partial class DateAndTimePickerControl:UserControl
    {
        /// <summary>
        /// Dependency property for the selected DateTime.
        /// </summary>
        public static readonly StyledProperty<DateTime> DateAndTimeProperty = AvaloniaProperty.Register<DateAndTimePickerControl, DateTime>(nameof(DateAndTime), new DateTime(1980, 7, 30, 12, 0, 0));

        /// <summary>
        /// Initializes a new instance of the <see cref="DateAndTimePickerControl"/> class.
        /// </summary>
        public DateAndTimePickerControl()
        {
            InitializeComponent();
            this.Loaded += DateAndTimePickerControl_Loaded;
        }

        static DateAndTimePickerControl()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            DateAndTimeProperty.Changed.AddClassHandler<DateAndTimePickerControl>(TimePropertyCallback);
        }

        /// <summary>
        /// Callback when the <see cref="DateAndTime"/> property changes.
        /// </summary>
        /// <param name="d">Dependency object initiating event.</param>
        /// <param name="e">Event arguments.</param>
        private static void TimePropertyCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(DateAndTimePickerControl))
                return;
            DateAndTimePickerControl thisControl = (DateAndTimePickerControl)d;
            // 
            if (e.NewValue == null)
                return;
            if (e.NewValue.GetType() != typeof(DateTime))
                return;
            if (thisControl.CalendarSelector.DisplayDate != (DateTime)e.NewValue)
                thisControl.CalendarSelector.DisplayDate = (DateTime)e.NewValue;
        }

        /// <summary>
        /// Identifies the <see cref="Is24Hour"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> Is24HourProperty = AvaloniaProperty.Register<DateAndTimePickerControl, bool>(nameof(Is24Hour), false);
        /// <summary>
        /// Gets or sets a value indicating whether the clock uses 24-hour format.
        /// </summary>
        public bool Is24Hour
        {
            get
            {
                return (bool)this.GetValue(Is24HourProperty);
            }
            set
            {
                this.SetValue(Is24HourProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the current date and time selected in the control.
        /// </summary>
        public DateTime DateAndTime
        {
            get
            {
                return (DateTime)this.GetValue(DateAndTimeProperty);
            }
            set
            {
                this.SetValue(DateAndTimeProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating whether seconds selection is enabled.
        /// </summary>
        public static readonly StyledProperty<bool> HasSecondsProperty = AvaloniaProperty.Register<DateAndTimePickerControl, bool>(nameof(HasSeconds), false);
        /// <summary>
        /// Gets or sets a value indicating whether seconds are enabled in the time selection.
        /// </summary>
        public bool HasSeconds
        {
            get
            {
                return (bool)this.GetValue(HasSecondsProperty);
            }
            set
            {
                this.SetValue(HasSecondsProperty, value);
            }
        }

        /// <summary>
        /// Dependency property indicating whether the control should start in hour selection mode.
        /// </summary>
        public static readonly StyledProperty<bool> SetToHoursOnLoadProperty = AvaloniaProperty.Register<DateAndTimePickerControl, bool>(nameof(SetToHoursOnLoad), false);
        /// <summary>
        /// Gets or sets whether the clock should initially display hour selection on load.
        /// </summary>
        public bool SetToHoursOnLoad
        {
            get
            {
                return (bool)this.GetValue(SetToHoursOnLoadProperty);
            }
            set
            {
                this.SetValue(SetToHoursOnLoadProperty, value);
            }
        }

        /// <summary>
        /// Handles calendar display date change to sync the selected date.
        /// </summary>
        /// <param name="sender">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private void CalendarSelector_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            int year = e.AddedDate.Value.Year;
            int month = e.AddedDate.Value.Month;
            int day = Math.Min(DateAndTime.Day, DateTime.DaysInMonth(year, month));
            DateAndTime = new DateTime(year, month, day, DateAndTime.Hour, DateAndTime.Minute, DateAndTime.Second);
        }

        /// <summary>
        /// Updates the selected day portion of the <see cref="DateAndTime"/> when the user picks a new date.
        /// </summary>
        /// <param name="sender">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private void CalendarSelector_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;
            if (e.AddedItems[0].GetType() != typeof(DateTime))
                return;
            DateTime newDate = (DateTime)e.AddedItems[0];
            DateAndTime = new DateTime(DateAndTime.Year, DateAndTime.Month, newDate.Day, DateAndTime.Hour, DateAndTime.Minute, DateAndTime.Second);
        }

        /// <summary>
        /// Sets the clock display to hours mode when the control is loaded, if configured to do so.
        /// </summary>
        /// <param name="sender">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private void DateAndTimePickerControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (SetToHoursOnLoad == true)
            {
                this.Clock.IsHours = true;
                this.Clock.IsMinutes = false;
                this.Clock.IsSeconds = false;
            }
        }
    }
}