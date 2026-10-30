using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using WpfCompat;

namespace GenericControls
{
    /// <summary>
    /// A WPF control for selecting time using a circular, analog-style clock face.
    /// Supports hour, minute, and second selection in 12-hour or 24-hour modes.
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
    /// XGoodFit port: the properties are Avalonia styled properties, and the time's change callback a
    /// class handler. Colour defaults are new <see cref="SolidColorBrush"/>es: Avalonia's
    /// <see cref="Brushes"/> are immutable brushes. The toggles' styles are ControlThemes, mouse events
    /// are pointer events, a hand's X2 and Y2 are its line's EndPoint, and Visibility is WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class ClockControl:UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Time"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTime> TimeProperty = AvaloniaProperty.Register<ClockControl, DateTime>(nameof(Time), new DateTime(1980, 7, 30, 12, 0, 0));

        static ClockControl()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            TimeProperty.Changed.AddClassHandler<ClockControl>(TimePropertyCallback);
        }

        /// <summary>
        /// Event handler for time property dependencies.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">Event data describing the property change.</param>
        private static void TimePropertyCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(ClockControl))
                return;
            ClockControl thisControl = (ClockControl)d;
            // 
            if (e.NewValue == null)
                return;
            if (e.NewValue.GetType() != typeof(DateTime))
                return;
            thisControl.ClearSelected();
            thisControl.SelectCurrentTime((DateTime)e.NewValue);
        }

        /// <summary>
        /// Gets or sets the time displayed and edited by the control.
        /// </summary>
        public DateTime Time
        {
            get
            {
                return (DateTime)this.GetValue(TimeProperty);
            }
            set
            {
                this.SetValue(TimeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="Is24Hour"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> Is24HourProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(Is24Hour), false);

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
        /// Identifies the <see cref="IsHours"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHoursProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(IsHours), true);

        /// <summary>
        /// Gets or sets whether the control is currently in hour-selection mode.
        /// </summary>
        public bool IsHours
        {
            get
            {
                return (bool)this.GetValue(IsHoursProperty);
            }
            set
            {
                this.SetValue(IsHoursProperty, value);
                ClearSelected();
                SelectCurrentTime(Time);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsMinutes"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsMinutesProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(IsMinutes), false);

        /// <summary>
        /// Gets or sets whether the control is currently in minute-selection mode.
        /// </summary>
        public bool IsMinutes
        {
            get
            {
                return (bool)this.GetValue(IsMinutesProperty);
            }
            set
            {
                this.SetValue(IsMinutesProperty, value);
                ClearSelected();
                SelectCurrentTime(Time);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsSeconds"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSecondsProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(IsSeconds), false);

        /// <summary>
        /// Gets or sets whether the control is currently in second-selection mode.
        /// </summary>
        public bool IsSeconds
        {
            get
            {
                return (bool)this.GetValue(IsSecondsProperty);
            }
            set
            {
                this.SetValue(IsSecondsProperty, value);
                ClearSelected();
                SelectCurrentTime(Time);
            }
        }

        /// <summary>
        /// Identifies the <see cref="HasMinutes"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> HasMinutesProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(HasMinutes), true);

        /// <summary>
        /// Gets or sets a value indicating whether minutes selection is available.
        /// </summary>
        public bool HasMinutes
        {
            get
            {
                return (bool)this.GetValue(HasMinutesProperty);
            }
            set
            {
                this.SetValue(HasMinutesProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="HasSeconds"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> HasSecondsProperty = AvaloniaProperty.Register<ClockControl, bool>(nameof(HasSeconds), false);

        /// <summary>
        /// Gets or sets a value indicating whether seconds selection is available.
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
        /// Identifies the <see cref="SelectedTimeColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> SelectedTimeColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(SelectedTimeColor), new SolidColorBrush(Colors.Black));
        /// <summary>
        /// Gets or sets the brush used to render the selected time color.
        /// </summary>
        public SolidColorBrush SelectedTimeColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(SelectedTimeColorProperty);
            }
            set
            {
                this.SetValue(SelectedTimeColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> TimeColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(TimeColor), new SolidColorBrush(Colors.DarkGray));
        /// <summary>
        /// Gets or sets the brush used for the time color.
        /// </summary>
        public SolidColorBrush TimeColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(TimeColorProperty);
            }
            set
            {
                this.SetValue(TimeColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedTimeFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> SelectedTimeFontSizeProperty = AvaloniaProperty.Register<ClockControl, double>(nameof(SelectedTimeFontSize), 24d);
        /// <summary>
        /// Gets or sets the font size for the selected time display.
        /// </summary>
        public double SelectedTimeFontSize
        {
            get
            {
                return (double)this.GetValue(SelectedTimeFontSizeProperty);
            }
            set
            {
                this.SetValue(SelectedTimeFontSizeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TimeFontSizeProperty = AvaloniaProperty.Register<ClockControl, double>(nameof(TimeFontSize), 24d);
        /// <summary>
        /// Gets or sets the font size for the time display.
        /// </summary>
        public double TimeFontSize
        {
            get
            {
                return (double)this.GetValue(TimeFontSizeProperty);
            }
            set
            {
                this.SetValue(TimeFontSizeProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        public static readonly StyledProperty<SolidColorBrush> SelectedColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(SelectedColor), null);
        /// <summary>
        /// Gets or sets the brush used to render selected tick marks.
        /// </summary>
        public SolidColorBrush SelectedColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(SelectedColorProperty);
            }
            set
            {
                this.SetValue(SelectedColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="HighlightColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> HighlightColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(HighlightColor), new SolidColorBrush(Colors.LightGray));
        /// <summary>
        /// Gets or sets the brush used for highlighting hover states.
        /// </summary>
        public SolidColorBrush HighlightColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(HighlightColorProperty);
            }
            set
            {
                this.SetValue(HighlightColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="FaceColor"/> dependency property.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        public static readonly StyledProperty<SolidColorBrush> FaceColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(FaceColor), null);
        /// <summary>
        /// Gets or sets the brush used for coloring the face of the clock.
        /// </summary>
        public SolidColorBrush FaceColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(FaceColorProperty);
            }
            set
            {
                this.SetValue(FaceColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="HandColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> HandColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(HandColor), new SolidColorBrush(Colors.DarkGray));
        /// <summary>
        /// Gets or sets the brush used for coloring the hands of the clock.
        /// </summary>
        public SolidColorBrush HandColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(HandColorProperty);
            }
            set
            {
                this.SetValue(HandColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="PreviewHandColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> PreviewHandColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(PreviewHandColor), new SolidColorBrush(Colors.LightGray));
        /// <summary>
        /// Gets or sets the brush used for the preview hand color.
        /// </summary>
        public SolidColorBrush PreviewHandColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(PreviewHandColorProperty);
            }
            set
            {
                this.SetValue(PreviewHandColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="FontColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> FontColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(FontColor), new SolidColorBrush(Colors.Black));
        /// <summary>
        /// Gets or sets the brush used for the font color.
        /// </summary>
        public SolidColorBrush FontColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(FontColorProperty);
            }
            set
            {
                this.SetValue(FontColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedFontColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> SelectedFontColorProperty = AvaloniaProperty.Register<ClockControl, SolidColorBrush>(nameof(SelectedFontColor), new SolidColorBrush(Colors.White));
        /// <summary>
        /// Gets or sets the brush used to render the selected font color.
        /// </summary>
        public SolidColorBrush SelectedFontColor
        {
            get
            {
                return (SolidColorBrush)this.GetValue(SelectedFontColorProperty);
            }
            set
            {
                this.SetValue(SelectedFontColorProperty, value);
            }
        }

        private ControlTheme _clockMajorNumberStyle;
        private ControlTheme _clockMinorNumberStyle;
        private ClockToggle[] _hourBorders = new ClockToggle[0];
        private ClockToggle[] _minuteBorders = new ClockToggle[60];
        private ClockToggle[] _secondBorders = new ClockToggle[60];

        /// <summary>
        /// Initializes a new instance of the <see cref="ClockControl"/> class.
        /// Sets up resources, initializes hour/minute/second ticks, and registers event handlers.
        /// </summary>
        public ClockControl()
        {

            // This call is required by the designer.
            this.InitializeComponent();
            // Per-instance defaults for the SelectedColor and FaceColor DPs. The DP
            // defaults are null because a shared SolidColorBrush across every
            // ClockControl instance would let one instance's color change leak
            // visually to others.
            if (SelectedColor == null)
            {
                SelectedColor = new SolidColorBrush(Color.FromArgb(255, 24, 24, 25));
            }
            if (FaceColor == null)
            {
                FaceColor = new SolidColorBrush(Color.FromArgb(255, 240, 240, 245));
            }
            // 
            // Add any initialization after the InitializeComponent() call.
            _clockMajorNumberStyle = (ControlTheme)this.FindResource("ClockMajorValueStyle");
            _clockMinorNumberStyle = (ControlTheme)this.FindResource("ClockMinorValueStyle");

            // XGoodFit port: Avalonia's styles can't set events, so the AM/PM style's EventSetters are these.
            foreach (Border amPmFace in new[] { this.AMFace, this.PMFace })
            {
                amPmFace.PointerEntered += AMPMBorder_MouseEnter;
                amPmFace.PointerExited += AMPMBorder_MouseLeave;
                amPmFace.PointerReleased += AMPMBorder_MouseUp;
            }

            // XGoodFit port: WPF doesn't capture the mouse in the clock, so its mouse-up and mouse-move
            // handlers see the element the pointer is over. Avalonia captures the pointer on a press, so
            // this releases it.
            this.AddHandler(PointerPressedEvent, (sender, e) => e.Pointer.Capture(null), RoutingStrategies.Bubble, handledEventsToo: true);
            // Set up Hours
            InitializeClock();
            // 
            // Minute and second borders only need to be set once on initialization
            double transformRadius = 42d; // transform radius
                                          // Get and set the minute and second tick circles
            for (int i = 1; i <= 59; i++)
            {
                if (i % 5 != 0)
                {
                    _minuteBorders[i] = GetBorder(i, 180 - i * 6, transformRadius, ClockToggle.State.MinuteMinor);
                    this.MinutesGrid.Children.Add(_minuteBorders[i]);
                    _secondBorders[i] = GetBorder(i, 180 - i * 6, transformRadius, ClockToggle.State.MinuteMinor);
                    this.SecondsGrid.Children.Add(_secondBorders[i]);
                }
            }

            // larger toggles (5, 10, 15, etc.) need to be rendered after to make sure they show up on top of the minor minute toggles.
            // Adding to the grid after the minors have been added set the render order.
            for (int i = 0; i <= 55; i += 5)
            {
                _minuteBorders[i] = GetBorder(i, 180 - i * 6, transformRadius, ClockToggle.State.MinuteMajor);
                this.MinutesGrid.Children.Add(_minuteBorders[i]);
                _secondBorders[i] = GetBorder(i, 180 - i * 6, transformRadius, ClockToggle.State.MinuteMajor);
                this.SecondsGrid.Children.Add(_secondBorders[i]);
            }

            this.Loaded += ClockControl_Loaded;
        }

        /// <summary>
        /// Handles the control's <see cref="Control.Loaded"/> event.
        /// Re-initializes clock and selects current time after layout is completed.
        /// </summary>
        /// <param name="sender">The object raising the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockControl_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeClock();
            SelectCurrentTime(Time);
        }

        /// <summary>
        /// Initializes the hour tick marks based on 12-hour or 24-hour configuration. 
        /// Clears existing elements and repositions tick controls.
        /// </summary>
        private void InitializeClock()
        {
            double transformRadius = 42d; // transform radius
                                          //
                                          // Get and set the hour tick circles
            _hourBorders = new ClockToggle[Is24Hour ? 24 : 12];
            for (int i = this.HoursGrid.Children.Count - 1; i >= 0; i -= 1)
            {
                if (this.HoursGrid.Children[i].GetType() == typeof(ClockToggle))
                    this.HoursGrid.Children.RemoveAt(i);
            }
            // Add hour tick circles
            double angle = 360d / _hourBorders.Count();
            if (Is24Hour)
            {
                for (int i = 1, loopTo = _hourBorders.Count() - 1; i <= loopTo; i += 2)
                {
                    _hourBorders[i] = GetBorder(i, 180d - i * angle, transformRadius, ClockToggle.State.HourMinor);
                    this.HoursGrid.Children.Add(_hourBorders[i]);
                }
                // In 24-hour mode, display 0 at the top (midnight)
                _hourBorders[0] = GetBorder(0, 180d, transformRadius, ClockToggle.State.HourMajor);
                this.HoursGrid.Children.Add(_hourBorders[0]);
                for (int i = 2, loopTo1 = _hourBorders.Count() - 1; i <= loopTo1; i += 2)
                {
                    _hourBorders[i] = GetBorder(i, 180d - i * angle, transformRadius, ClockToggle.State.HourMajor);
                    this.HoursGrid.Children.Add(_hourBorders[i]);
                }
            }
            else
            {
                _hourBorders[0] = GetBorder(_hourBorders.Count(), 180d, transformRadius, ClockToggle.State.HourMajor);
                this.HoursGrid.Children.Add(_hourBorders[0]);
                for (int i = 1, loopTo2 = _hourBorders.Count() - 1; i <= loopTo2; i++)
                {
                    _hourBorders[i] = GetBorder(i, 180d - i * angle, transformRadius, ClockToggle.State.HourMajor);
                    this.HoursGrid.Children.Add(_hourBorders[i]);
                }
            }
        }

        /// <summary>
        /// A circular toggle used with the <see cref="ClockControl"/> for selecting specific time units (hours, minutes, seconds).
        /// Displays a number and highlights or selects on interaction.
        /// </summary>
        /// <param name="t">The numeric time value (e.g. 5, 10).</param>
        /// <param name="angleDegree">Angle on the clock face to position the toggle.</param>
        /// <param name="transformRadius">distance from the clock center to the toggle.</param>
        /// <param name="borderState">The visual and logical state of the toggle (e.g., minute major).</param>
        /// <returns>A configured <see cref="ClockToggle"/> element.</returns>
        private ClockToggle GetBorder(int t, double angleDegree, double transformRadius, ClockToggle.State borderState)
        {
            var b = new ClockToggle() { TimeValue = t, TimeState = borderState };
            if (borderState == ClockToggle.State.MinuteMinor || borderState == ClockToggle.State.SecondMinor || borderState == ClockToggle.State.HourMinor)
            {
                b.Theme = _clockMinorNumberStyle;
            }
            else
            {
                b.Theme = _clockMajorNumberStyle;
            }
            // 
            b.RenderTransform = new TranslateTransform(transformRadius * Math.Sin(angleDegree * Math.PI / 180d), transformRadius * Math.Cos(angleDegree * Math.PI / 180d));
            return b;
        }

        /// <summary>
        /// Handles clicks on the hour section and sets mode to hour selection.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void HoursMouseUp(object sender, PointerReleasedEventArgs e)
        {
            IsMinutes = false;
            IsSeconds = false;
            IsHours = true;
        }

        /// <summary>
        /// Handles clicks on the minutes section and sets mode to minute selection.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void MinutesMouseUp(object sender, PointerReleasedEventArgs e)
        {
            if (HasMinutes == false)
                return;
            IsMinutes = true;
            IsSeconds = false;
            IsHours = false;
        }

        /// <summary>
        /// Handles clicks on the seconds section and sets mode to second selection.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void SecondsMouseUp(object sender, PointerReleasedEventArgs e)
        {
            IsMinutes = false;
            IsSeconds = true;
            IsHours = false;
        }

        /// <summary>
        /// Toggles AM/PM by adding or subtracting 12 hours from the selected time.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void AmPmMouseUp(object sender, PointerReleasedEventArgs e)
        {
            int hour = Time.Hour;
            if (hour >= 12)
                hour = hour - 12;
            else
                hour = hour + 12;
            Time = new DateTime(Time.Year, Time.Month, Time.Day, hour, Time.Minute, Time.Second);
        }

        /// <summary>
        /// Handles promotion of selection from hours to minutes to seconds on left click. 
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockFace_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            if (e.InitialPressMouseButton == MouseButton.Left)
            {
                if (IsHours)
                {
                    if (HasMinutes)
                    {
                        IsMinutes = true;
                        IsSeconds = false;
                        IsHours = false;
                    }
                }
                else if (IsMinutes)
                {
                    IsHours = false;
                    if (HasSeconds)
                    {
                        IsMinutes = false;
                        IsSeconds = true;
                    }
                }
                else if (IsSeconds)
                {
                    IsHours = false;
                    if (HasSeconds)
                    {
                        IsMinutes = false;
                        IsSeconds = true;
                    }
                }
            }
        }

        /// <summary>
        /// Selects or highlights the time value closest to the clicked position.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockFace_MouseDown(object sender, PointerPressedEventArgs e)
        {
            int timeValue = GetTimeValue(e.GetPosition(this.ClockFace));
            ClearHighlighted();
            if (e.GetCurrentPoint(this.ClockFace).Properties.IsLeftButtonPressed)
            {
                SetTimeValue(timeValue);
            }
            else
            {
                SetHighlighted(timeValue);
            }
        }

        /// <summary>
        /// Updates selection or highlight as the mouse moves over the clock face.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockFace_MouseMove(object sender, PointerEventArgs e)
        {
            int timeValue = GetTimeValue(e.GetPosition(this.ClockFace));
            ClearHighlighted();
            if (e.GetCurrentPoint(this.ClockFace).Properties.IsLeftButtonPressed)
            {
                SetTimeValue(timeValue);
            }
            else
            {
                SetHighlighted(timeValue);
            }
            // UpdateForTimeValue(timeValue, e.LeftButton)
        }

        /// <summary>
        /// Converts a mouse position into a corresponding clock value (hour/minute/second).
        /// </summary>
        /// <param name="p">The mouse position relative to the clock face.</param>
        /// <returns>The time value (hour, minute, or second) corresponding to the mouse position.</returns>
        private int GetTimeValue(Point p)
        {
            double quadrantSize;
            if (IsHours)
            {
                quadrantSize = 1d / _hourBorders.Count(); // If(Is24Hour = True, 24, 12)
            }
            else // Must be minutes or seconds
            {
                quadrantSize = 1d / 60d;
            }
            // 
            double angleRadians = Math.Atan2(-50 + p.X, 50d - p.Y);
            double arc0To1 = angleRadians > 0d ? angleRadians / Math.PI / 2d : (2d + angleRadians / Math.PI) / 2d;
            int timeValue = (int)Math.Round(arc0To1 / quadrantSize);
            if (IsHours)
            {
                if (timeValue == _hourBorders.Count())
                    timeValue = 0;
            }
            else if (timeValue == 60) // Must be minutes or seconds
                timeValue = 0;
            return timeValue;
        }

        /// <summary>
        /// Updates the internal Time value based on the selected hour/minute/second.
        /// </summary>
        /// <param name="timeValue">The selected time value (hour, minute, or second).</param>
        private void SetTimeValue(int timeValue)
        {
            if (IsHours)
            {
                // Convert from AM/PM to 24 hour value
                if (Is24Hour == false)
                {
                    int hour = Time.Hour;
                    if (timeValue >= 12)
                        timeValue = timeValue - 12;
                    if (hour >= 12)
                        timeValue = timeValue + 12;
                }
                Time = new DateTime(Time.Year, Time.Month, Time.Day, timeValue, Time.Minute, Time.Second);
            }
            else if (IsMinutes)
            {
                Time = new DateTime(Time.Year, Time.Month, Time.Day, Time.Hour, timeValue, Time.Second);
            }
            else if (IsSeconds)
            {
                Time = new DateTime(Time.Year, Time.Month, Time.Day, Time.Hour, Time.Minute, timeValue);
            }
        }

        /// <summary>
        /// Removes all highlight effects from tick marks on the clock face.
        /// </summary>
        private void ClearHighlighted()
        {
            if (IsHours)
            {
                for (int i = 0, loopTo = _hourBorders.Count() - 1; i <= loopTo; i++)
                    _hourBorders[i].IsHighlighted = false;
                this.PreviewHourHandLine.Visibility = Visibility.Hidden;
            }
            else if (IsMinutes)
            {
                for (int i = 0, loopTo1 = _minuteBorders.Count() - 1; i <= loopTo1; i++)
                    _minuteBorders[i].IsHighlighted = false;
                this.PreviewMinuteHandLine.Visibility = Visibility.Hidden;
            }
            else if (IsSeconds)
            {
                for (int i = 0, loopTo2 = _secondBorders.Count() - 1; i <= loopTo2; i++)
                    _secondBorders[i].IsHighlighted = false;
                this.PreviewSecondHandLine.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Visually highlights the tick mark for the current hover value.
        /// </summary>
        /// <param name="timeValue">The time value to highlight.</param>
        private void SetHighlighted(int timeValue)
        {
            if (IsHours)
            {
                if (_hourBorders[timeValue].IsSelected == false)
                    _hourBorders[timeValue].IsHighlighted = true;
                this.PreviewHourHandLine.Visibility = Visibility.Visible;
                this.PreviewHourHandLine.EndPoint = new Point(((TranslateTransform)_hourBorders[timeValue].RenderTransform).X, ((TranslateTransform)_hourBorders[timeValue].RenderTransform).Y);
            }
            else if (IsMinutes)
            {
                if (_minuteBorders[timeValue].IsSelected == false)
                    _minuteBorders[timeValue].IsHighlighted = true;
                this.PreviewMinuteHandLine.Visibility = Visibility.Visible;
                this.PreviewMinuteHandLine.EndPoint = new Point(((TranslateTransform)_minuteBorders[timeValue].RenderTransform).X, ((TranslateTransform)_minuteBorders[timeValue].RenderTransform).Y);
            }
            else if (IsSeconds)
            {
                if (_secondBorders[timeValue].IsSelected == false)
                    _secondBorders[timeValue].IsHighlighted = true;
                this.PreviewSecondHandLine.Visibility = Visibility.Visible;
                this.PreviewSecondHandLine.EndPoint = new Point(((TranslateTransform)_secondBorders[timeValue].RenderTransform).X, ((TranslateTransform)_secondBorders[timeValue].RenderTransform).Y);
            }
        }

        /// <summary>
        /// Deselects all tick marks of the active time unit (hour/minute/second)
        /// </summary>
        private void ClearSelected()
        {
            if (IsHours)
            {
                for (int i = 0, loopTo = _hourBorders.Count() - 1; i <= loopTo; i++)
                    _hourBorders[i].IsSelected = false;
            }
            else if (IsMinutes)
            {
                for (int i = 0, loopTo1 = _minuteBorders.Count() - 1; i <= loopTo1; i++)
                    _minuteBorders[i].IsSelected = false;
            }
            else if (IsSeconds)
            {
                for (int i = 0, loopTo2 = _secondBorders.Count() - 1; i <= loopTo2; i++)
                    _secondBorders[i].IsSelected = false;
            }
        }

        /// <summary>
        /// Sets the time and updates UI selection accordingly.
        /// </summary>
        /// <param name="timeValue">The DateTime value to select.</param>
        public void SelectTime(DateTime timeValue)
        {
            SelectCurrentTime(timeValue);
        }

        /// <summary>
        /// Updates the UI to reflect the selected DateTime (hour, minute, second).
        /// </summary>
        /// <param name="timeValue">The DateTime value to display as selected.</param>
        private void SelectCurrentTime(DateTime timeValue)
        {
            ClearHighlighted();
            if (IsHours)
            {
                int hour = timeValue.Hour;
                if (_hourBorders.Count() < 13)
                {
                    if (hour >= 12)
                        hour = hour - 12; // Else hour = hour + 12
                }
                _hourBorders[hour].IsSelected = true;
                this.HourHandLine.EndPoint = new Point(((TranslateTransform)_hourBorders[hour].RenderTransform).X, ((TranslateTransform)_hourBorders[hour].RenderTransform).Y);
            }
            else if (IsMinutes)
            {
                _minuteBorders[timeValue.Minute].IsSelected = true;
                this.MinuteHandLine.EndPoint = new Point(((TranslateTransform)_minuteBorders[timeValue.Minute].RenderTransform).X, ((TranslateTransform)_minuteBorders[timeValue.Minute].RenderTransform).Y);
            }
            else if (IsSeconds)
            {
                _secondBorders[timeValue.Second].IsSelected = true;
                this.SecondHandLine.EndPoint = new Point(((TranslateTransform)_secondBorders[timeValue.Second].RenderTransform).X, ((TranslateTransform)_secondBorders[timeValue.Second].RenderTransform).Y);
            }
            // 
            if (Is24Hour == false)
            {
                if (timeValue.Hour < 12)
                {
                    this.AMFace.Background = SelectedColor;
                    this.AMText.Foreground = SelectedFontColor;
                    this.PMFace.Background = FaceColor;
                    this.PMText.Foreground = FontColor;
                }
                else
                {
                    this.PMFace.Background = SelectedColor;
                    this.PMText.Foreground = SelectedFontColor;
                    this.AMFace.Background = FaceColor;
                    this.AMText.Foreground = FontColor;
                }
            }
        }

        /// <summary>
        /// Utility method to compute Euclidean distance between two points.
        /// </summary>
        /// <param name="x1">x coordinate of the first point.</param>
        /// <param name="y1">y coordinate of the first point.</param>
        /// <param name="x2">x coordinate of the second point.</param>
        /// <param name="y2">y coordinate of the second point.</param>
        /// <returns>The Euclidean distance between the two points.</returns>
        public static double LineMagnitude(double x1, double y1, double x2, double y2)
        {
            // There exists methods to approximate the square root that are much faster than math.sqrt(). 
            // May want to consider implementing. http: //blog.wouldbetheologian.com/2011/11/fast-approximate-sqrt-method-in-c.html
            return Math.Sqrt(Math.Pow(x2 - x1, 2d) + Math.Pow(y2 - y1, 2d));
        }

        /// <summary>
        /// Focuses the control when mouse enters the clock face.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockFace_MouseEnter(object sender, PointerEventArgs e)
        {
            this.ClockFace.Focus();
        }

        /// <summary>
        /// Clears highlights when the mouse exits the clock face.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void ClockFace_MouseLeave(object sender, PointerEventArgs e)
        {
            ClearHighlighted();
        }

        /// <summary>
        /// Highlights AM/PM toggle when hovered and it would switch period.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void AMPMBorder_MouseEnter(object sender, PointerEventArgs e)
        {
            Border selectedBorder = (Border)sender;
            // 
            int hour = Time.Hour;
            if (hour >= 12 & selectedBorder.Equals(this.AMFace))
            {
                this.AMFace.Background = HighlightColor;
            }
            else if (hour < 12 & selectedBorder.Equals(this.PMFace))
            {
                this.PMFace.Background = HighlightColor;
            }

        }

        /// <summary>
        /// Restores default background when AM/PM toggle is no longer hovered.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void AMPMBorder_MouseLeave(object sender, PointerEventArgs e)
        {
            Border selectedBorder = (Border)sender;
            // 
            int hour = Time.Hour;
            if (hour >= 12 & selectedBorder.Equals(this.AMFace))
            {
                this.AMFace.Background = FaceColor;
            }
            else if (hour < 12 & selectedBorder.Equals(this.PMFace))
            {
                this.PMFace.Background = FaceColor;
            }
        }

        /// <summary>
        /// Switches between AM and PM when the user clicks on the toggle.
        /// </summary>
        /// <param name="sender">Object raising event.</param>
        /// <param name="e">Event arguments.</param>
        private void AMPMBorder_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            // 
            Border selectedBorder = (Border)sender;

            int hour = Time.Hour;
            if (selectedBorder.Equals(this.AMFace) & hour >= 12)
            {
                hour = hour - 12;
                Time = new DateTime(Time.Year, Time.Month, Time.Day, hour, Time.Minute, Time.Second);
            }
            else if (selectedBorder.Equals(this.PMFace) & hour < 12)
            {
                hour = hour + 12;
                Time = new DateTime(Time.Year, Time.Month, Time.Day, hour, Time.Minute, Time.Second);
            }
        }

    }

    /// <summary>
    /// Represents the type of time element rendered on the clock face.
    /// </summary>
    public class ClockToggle : Border
    {

        /// <summary>
        /// Represents the type of time element rendered on the clock face.
        /// </summary>
        public enum State
        {
            /// <summary>Major hour tick mark (e.g., 12, 3, 6, 9).</summary>
            HourMajor,
            /// <summary>Major minute tick mark (e.g., 5, 10, 15).</summary>
            MinuteMajor,
            /// <summary>Major second tick mark (e.g., 5, 10, 15).</summary>
            SecondMajor,
            /// <summary>Minor hour tick mark.</summary>
            HourMinor,
            /// <summary>Minor minute tick mark.</summary>
            MinuteMinor,
            /// <summary>Minor second tick mark.</summary>
            SecondMinor
        }

        /// <summary>
        /// Identifies the <see cref="IsSelected"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ClockToggle, bool>(nameof(IsSelected), false);

        /// <summary>
        /// Gets or sets whether the clock toggle is selected.
        /// </summary>
        public bool IsSelected
        {
            get
            {
                return (bool)GetValue(IsSelectedProperty);
            }
            set
            {
                SetValue(IsSelectedProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="IsHighlighted"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsHighlightedProperty = AvaloniaProperty.Register<ClockToggle, bool>(nameof(IsHighlighted), false);
        /// <summary>
        /// Gets or sets whether the clock toggle is highlighted.
        /// </summary>
        public bool IsHighlighted
        {
            get
            {
                return (bool)GetValue(IsHighlightedProperty);
            }
            set
            {
                SetValue(IsHighlightedProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="FontColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> FontColorProperty = AvaloniaProperty.Register<ClockToggle, SolidColorBrush>(nameof(FontColor), new SolidColorBrush(Colors.Black));
        /// <summary>
        /// Gets or sets the font color of the toggle.
        /// </summary>
        public SolidColorBrush FontColor
        {
            get
            {
                return (SolidColorBrush)GetValue(FontColorProperty);
            }
            set
            {
                SetValue(FontColorProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeValue"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<int> TimeValueProperty = AvaloniaProperty.Register<ClockToggle, int>(nameof(TimeValue), 1);
        /// <summary>
        /// Gets or sets the numeric value of the time unit this toggle represents.
        /// </summary>
        public int TimeValue
        {
            get
            {
                return (int)GetValue(TimeValueProperty);
            }
            set
            {
                SetValue(TimeValueProperty, value);
            }
        }

        /// <summary>
        /// Identifies the <see cref="TimeState"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<State> TimeStateProp = AvaloniaProperty.Register<ClockToggle, State>(nameof(TimeState), State.HourMajor);

        static ClockToggle()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            TimeStateProp.Changed.AddClassHandler<ClockToggle>(StateChangedCallback);
        }

        /// <summary>
        /// Callback triggered when the TimeState property is changed. Removes content for minor ticks.
        /// </summary>
        /// <param name="d">The dependency object (should be a ClockToggle)</param>
        /// <param name="e">Change event arguments.</param>
        private static void StateChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(ClockToggle))
                return;
            ClockToggle thisControl = (ClockToggle)d;
            // 
            if (e.NewValue == null)
                return;
            if (e.NewValue.GetType() != typeof(State))
                return;
            State newState = (State)e.NewValue;
            if (newState == State.MinuteMinor || newState == State.SecondMinor || newState == State.HourMinor)
            {
                thisControl.Child = null;
            }


        }

        /// <summary>
        /// Gets or sets the visual state of the toggle (e.g., HourMajor, MinuteMinor).
        /// </summary>
        public State TimeState
        {
            get
            {
                return (State)GetValue(TimeStateProp);
            }
            set
            {
                SetValue(TimeStateProp, value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ClockToggle class, sets up bindings and default child visuals.
        /// </summary>
        public ClockToggle()
        {

            // Make it a circle
            IsHitTestVisible = false;
            // XGoodFit port: Avalonia's actual height is the bounds' height, which WPF converted to a corner
            // radius by itself.
            this.Bind(CornerRadiusProperty, new Binding() { Path = "Bounds.Height", Source = this, Converter = new DoubleToCornerRadiusConverter() });
            this.Bind(WidthProperty, new Binding() { Path = "Bounds.Height", Source = this });
            // Create inner text
            var tBox = new TextBlock() { FontSize = 7d, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Stretch, TextAlignment = TextAlignment.Center };
            tBox.Bind(TextBlock.TextProperty, new Binding() { Path = nameof(TimeValue), Source = this });
            tBox.Bind(TextBlock.ForegroundProperty, new Binding() { Path = "FontColor", Source = this });
            // add text to circle
            Child = tBox;
        }

    }
}