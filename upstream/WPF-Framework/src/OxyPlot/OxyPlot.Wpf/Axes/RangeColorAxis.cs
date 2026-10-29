// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RangeColorAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.RangeColorAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.RangeColorAxis"/>.
    /// </summary>
    /// <remarks>
    /// A range color axis maps values to colors based on specified ranges.
    /// Each range has a lower bound, upper bound, and associated color.
    /// </remarks>
    public class RangeColorAxis : LinearAxis
    {
        /// <summary>
        /// Identifies the <see cref="InvalidNumberColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InvalidNumberColorProperty =
            DependencyProperty.Register(
                nameof(InvalidNumberColor),
                typeof(Color),
                typeof(RangeColorAxis),
                new PropertyMetadata(Colors.Gray, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HighColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HighColorProperty =
            DependencyProperty.Register(
                nameof(HighColor),
                typeof(Color),
                typeof(RangeColorAxis),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LowColorProperty =
            DependencyProperty.Register(
                nameof(LowColor),
                typeof(Color),
                typeof(RangeColorAxis),
                new PropertyMetadata(MoreColors.Undefined, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="RangeColorAxis"/> class.
        /// </summary>
        static RangeColorAxis()
        {
            PositionProperty.OverrideMetadata(typeof(RangeColorAxis), new PropertyMetadata(AxisPosition.None, AppearanceChanged));
            IsPanEnabledProperty.OverrideMetadata(typeof(RangeColorAxis), new PropertyMetadata(false));
            IsZoomEnabledProperty.OverrideMetadata(typeof(RangeColorAxis), new PropertyMetadata(false));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeColorAxis"/> class.
        /// </summary>
        public RangeColorAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.RangeColorAxis();
        }

        /// <summary>
        /// Gets or sets the color used for NaN values.
        /// </summary>
        /// <value>The invalid number color. The default is <see cref="Colors.Gray"/>.</value>
        public Color InvalidNumberColor
        {
            get => (Color)this.GetValue(InvalidNumberColorProperty);
            set => this.SetValue(InvalidNumberColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values above the highest range.
        /// </summary>
        /// <value>The high color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color HighColor
        {
            get => (Color)this.GetValue(HighColorProperty);
            set => this.SetValue(HighColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color for values below the lowest range.
        /// </summary>
        /// <value>The low color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color LowColor
        {
            get => (Color)this.GetValue(LowColorProperty);
            set => this.SetValue(LowColorProperty, value);
        }

        /// <summary>
        /// Adds a color range to the axis.
        /// </summary>
        /// <param name="lowerBound">The lower bound of the range.</param>
        /// <param name="upperBound">The upper bound of the range.</param>
        /// <param name="color">The color for values in this range.</param>
        public void AddRange(double lowerBound, double upperBound, Color color)
        {
            if (this.InternalAxis is OxyPlot.Axes.RangeColorAxis a)
            {
                a.AddRange(lowerBound, upperBound, color.ToOxyColor());
            }
        }

        /// <summary>
        /// Clears all ranges from the axis.
        /// </summary>
        public void ClearRanges()
        {
            if (this.InternalAxis is OxyPlot.Axes.RangeColorAxis a)
            {
                a.ClearRanges();
            }
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.RangeColorAxis"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.RangeColorAxis a)
            {
                a.InvalidNumberColor = this.InvalidNumberColor.ToOxyColor();
                a.HighColor = this.HighColor.ToOxyColor();
                a.LowColor = this.LowColor.ToOxyColor();
            }
        }
    }
}
