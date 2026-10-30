// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.DateTimeAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Globalization;
    using Avalonia;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.DateTimeAxis"/>.
    /// </summary>
    /// <remarks>
    /// A date/time axis displays DateTime values. The axis values are internally stored as
    /// days since 1899-12-31 (OLE Automation date format).
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class DateTimeAxis : Axis
    {
        /// <summary>
        /// Identifies the <see cref="CalendarWeekRule"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<CalendarWeekRule> CalendarWeekRuleProperty = AvaloniaProperty.Register<DateTimeAxis, CalendarWeekRule>(
            nameof(CalendarWeekRule),
            CalendarWeekRule.FirstFourDayWeek);

        /// <summary>
        /// Identifies the <see cref="FirstDateTime"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTime> FirstDateTimeProperty = AvaloniaProperty.Register<DateTimeAxis, DateTime>(
            nameof(FirstDateTime),
            DateTime.MinValue);

        /// <summary>
        /// Identifies the <see cref="FirstDayOfWeek"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DayOfWeek> FirstDayOfWeekProperty = AvaloniaProperty.Register<DateTimeAxis, DayOfWeek>(
            nameof(FirstDayOfWeek),
            DayOfWeek.Monday);

        /// <summary>
        /// Identifies the <see cref="IntervalType"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTimeIntervalType> IntervalTypeProperty = AvaloniaProperty.Register<DateTimeAxis, DateTimeIntervalType>(
            nameof(IntervalType),
            DateTimeIntervalType.Auto);

        /// <summary>
        /// Identifies the <see cref="LastDateTime"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTime> LastDateTimeProperty = AvaloniaProperty.Register<DateTimeAxis, DateTime>(
            nameof(LastDateTime),
            DateTime.MaxValue);

        /// <summary>
        /// Identifies the <see cref="MinorIntervalType"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DateTimeIntervalType> MinorIntervalTypeProperty = AvaloniaProperty.Register<DateTimeAxis, DateTimeIntervalType>(
            nameof(MinorIntervalType),
            DateTimeIntervalType.Auto);

        /// <summary>
        /// Initializes static members of the <see cref="DateTimeAxis"/> class.
        /// </summary>
        static DateTimeAxis()
        {
            CalendarWeekRuleProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            FirstDateTimeProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            FirstDayOfWeekProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            IntervalTypeProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            LastDateTimeProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            MinorIntervalTypeProperty.Changed.AddClassHandler<DateTimeAxis>(DataChanged);
            PositionProperty.OverrideDefaultValue<DateTimeAxis>(AxisPosition.Bottom);
            PositionProperty.Changed.AddClassHandler<DateTimeAxis>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateTimeAxis"/> class.
        /// </summary>
        public DateTimeAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.DateTimeAxis();
        }

        /// <summary>
        /// Gets or sets the calendar week rule for week number calculations. The default is <see cref="System.Globalization.CalendarWeekRule.FirstFourDayWeek"/>.
        /// </summary>
        public CalendarWeekRule CalendarWeekRule
        {
            get => (CalendarWeekRule)this.GetValue(CalendarWeekRuleProperty);
            set => this.SetValue(CalendarWeekRuleProperty, value);
        }

        /// <summary>
        /// Gets or sets the first valid date/time on the axis. The default is <see cref="DateTime.MinValue"/>.
        /// </summary>
        public DateTime FirstDateTime
        {
            get => (DateTime)this.GetValue(FirstDateTimeProperty);
            set => this.SetValue(FirstDateTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the first day of the week for week number calculations. The default is <see cref="DayOfWeek.Monday"/>.
        /// </summary>
        public DayOfWeek FirstDayOfWeek
        {
            get => (DayOfWeek)this.GetValue(FirstDayOfWeekProperty);
            set => this.SetValue(FirstDayOfWeekProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval type for major tick marks. The default is <see cref="DateTimeIntervalType.Auto"/>.
        /// </summary>
        public DateTimeIntervalType IntervalType
        {
            get => (DateTimeIntervalType)this.GetValue(IntervalTypeProperty);
            set => this.SetValue(IntervalTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the last valid date/time on the axis. The default is <see cref="DateTime.MaxValue"/>.
        /// </summary>
        public DateTime LastDateTime
        {
            get => (DateTime)this.GetValue(LastDateTimeProperty);
            set => this.SetValue(LastDateTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval type for minor tick marks. The default is <see cref="DateTimeIntervalType.Auto"/>.
        /// </summary>
        public DateTimeIntervalType MinorIntervalType
        {
            get => (DateTimeIntervalType)this.GetValue(MinorIntervalTypeProperty);
            set => this.SetValue(MinorIntervalTypeProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.DateTimeAxis"/> model.</returns>
        public override OxyPlot.Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAxis is OxyPlot.Axes.DateTimeAxis a)
            {
                a.IntervalType = this.IntervalType;
                a.MinorIntervalType = this.MinorIntervalType;
                a.FirstDayOfWeek = this.FirstDayOfWeek;
                a.CalendarWeekRule = this.CalendarWeekRule;

                if (this.FirstDateTime > DateTime.MinValue)
                {
                    a.Minimum = OxyPlot.Axes.DateTimeAxis.ToDouble(this.FirstDateTime);
                }

                if (this.LastDateTime < DateTime.MaxValue)
                {
                    a.Maximum = OxyPlot.Axes.DateTimeAxis.ToDouble(this.LastDateTime);
                }
            }
        }
    }
}
