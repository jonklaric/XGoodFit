// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IntervalBarSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.IntervalBarSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.IntervalBarSeries"/>.
    /// </summary>
    /// <remarks>
    /// An interval bar series displays horizontal bars defined by start and end values,
    /// useful for Gantt charts, range displays, or showing intervals on a timeline.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class IntervalBarSeries : BarSeriesBase<IntervalBarItem>
    {
        /// <summary>
        /// Identifies the <see cref="StartField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> StartFieldProperty = AvaloniaProperty.Register<IntervalBarSeries, string>(
            nameof(StartField),
            null);

        /// <summary>
        /// Identifies the <see cref="EndField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> EndFieldProperty = AvaloniaProperty.Register<IntervalBarSeries, string>(
            nameof(EndField),
            null);

        /// <summary>
        /// Initializes static members of the <see cref="IntervalBarSeries"/> class.
        /// </summary>
        static IntervalBarSeries()
        {
            StartFieldProperty.Changed.AddClassHandler<IntervalBarSeries>(DataChanged);
            EndFieldProperty.Changed.AddClassHandler<IntervalBarSeries>(DataChanged);
            TrackerFormatStringProperty.OverrideDefaultValue<IntervalBarSeries>(OxyPlot.Series.IntervalBarSeries.DefaultTrackerFormatString);
            TrackerFormatStringProperty.Changed.AddClassHandler<IntervalBarSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalBarSeries"/> class.
        /// </summary>
        public IntervalBarSeries()
        {
            this.InternalSeries = new OxyPlot.Series.IntervalBarSeries();
        }

        /// <summary>
        /// Gets or sets the name of the property containing the start value.
        /// </summary>
        /// <value>The field name. The default is <c>null</c>.</value>
        public string StartField
        {
            get => (string)this.GetValue(StartFieldProperty);
            set => this.SetValue(StartFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the end value.
        /// </summary>
        /// <value>The field name. The default is <c>null</c>.</value>
        public string EndField
        {
            get => (string)this.GetValue(EndFieldProperty);
            set => this.SetValue(EndFieldProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>An <see cref="OxyPlot.Series.IntervalBarSeries"/> instance.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.IntervalBarSeries s)
            {
                s.StartField = this.StartField;
                s.EndField = this.EndField;
            }
        }
    }
}
