using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
    /// </remarks>
    public partial class DateAndTimePickerControl:UserControl
    {
        /// <summary>
        /// Dependency property for the selected DateTime.
        /// </summary>
        public static readonly DependencyProperty DateAndTimeProperty = DependencyProperty.Register(nameof(DateAndTime), typeof(DateTime), typeof(DateAndTimePickerControl), new UIPropertyMetadata(new DateTime(1980, 7, 30, 12, 0, 0), TimePropertyCallback));

        /// <summary>
        /// Initializes a new instance of the <see cref="DateAndTimePickerControl"/> class.
        /// </summary>
        public DateAndTimePickerControl()
        {
            InitializeComponent();
            this.Loaded += DateAndTimePickerControl_Loaded;
        }

        /// <summary>
        /// Callback when the <see cref="DateAndTime"/> property changes.
        /// </summary>
        /// <param name="d">Dependency object initiating event.</param>
        /// <param name="e">Event arguments.</param>
        private static void TimePropertyCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
        public static readonly DependencyProperty Is24HourProperty = DependencyProperty.Register(nameof(Is24Hour), typeof(bool), typeof(DateAndTimePickerControl), new FrameworkPropertyMetadata(false));
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
        public static readonly DependencyProperty HasSecondsProperty = DependencyProperty.Register(nameof(HasSeconds), typeof(bool), typeof(DateAndTimePickerControl), new UIPropertyMetadata(false));
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
        public static readonly DependencyProperty SetToHoursOnLoadProperty = DependencyProperty.Register(nameof(SetToHoursOnLoad), typeof(bool), typeof(DateAndTimePickerControl), new UIPropertyMetadata(false));
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
        /// Releases the mouse capture from the calendar to allow other controls to get focus.
        /// Solution was found here: https://stackoverflow.com/questions/25352961/have-to-click-away-twice-from-calendar-in-wpf/50536606#50536606
        /// </summary>
        /// <param name="sender">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private void CalendarSelector_GotMouseCapture(object sender, MouseEventArgs e)
        {
            UIElement originalElement = (UIElement)e.OriginalSource;
            if (originalElement.GetType() == typeof(System.Windows.Controls.Primitives.CalendarDayButton) || originalElement.GetType() == typeof(System.Windows.Controls.Primitives.CalendarItem))
                originalElement.ReleaseMouseCapture();
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