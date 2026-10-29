// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterErrorSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ScatterErrorSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Series;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ScatterErrorSeries"/>.
    /// </summary>
    /// <remarks>
    /// A scatter error series displays scatter points with error bars showing
    /// the uncertainty or variability of the data in both X and Y directions.
    /// </remarks>
    public class ScatterErrorSeries : ScatterSeries<ScatterErrorPoint>
    {
        /// <summary>
        /// Identifies the <see cref="DataFieldErrorX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldErrorXProperty =
            DependencyProperty.Register(
                nameof(DataFieldErrorX),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldErrorY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldErrorYProperty =
            DependencyProperty.Register(
                nameof(DataFieldErrorY),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldLowerErrorX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldLowerErrorXProperty =
            DependencyProperty.Register(
                nameof(DataFieldLowerErrorX),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldUpperErrorX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldUpperErrorXProperty =
            DependencyProperty.Register(
                nameof(DataFieldUpperErrorX),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldLowerErrorY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldLowerErrorYProperty =
            DependencyProperty.Register(
                nameof(DataFieldLowerErrorY),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="DataFieldUpperErrorY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataFieldUpperErrorYProperty =
            DependencyProperty.Register(
                nameof(DataFieldUpperErrorY),
                typeof(string),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorBarColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorBarColorProperty =
            DependencyProperty.Register(
                nameof(ErrorBarColor),
                typeof(Color),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorBarStopWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorBarStopWidthProperty =
            DependencyProperty.Register(
                nameof(ErrorBarStopWidth),
                typeof(double),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ErrorBarStrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorBarStrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(ErrorBarStrokeThickness),
                typeof(double),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumErrorSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumErrorSizeProperty =
            DependencyProperty.Register(
                nameof(MinimumErrorSize),
                typeof(double),
                typeof(ScatterErrorSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterErrorSeries"/> class.
        /// </summary>
        public ScatterErrorSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ScatterErrorSeries();
        }

        /// <summary>
        /// Gets or sets the data field for symmetric X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldErrorX
        {
            get => (string)this.GetValue(DataFieldErrorXProperty);
            set => this.SetValue(DataFieldErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for symmetric Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldErrorY
        {
            get => (string)this.GetValue(DataFieldErrorYProperty);
            set => this.SetValue(DataFieldErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for lower X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldLowerErrorX
        {
            get => (string)this.GetValue(DataFieldLowerErrorXProperty);
            set => this.SetValue(DataFieldLowerErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for upper X error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldUpperErrorX
        {
            get => (string)this.GetValue(DataFieldUpperErrorXProperty);
            set => this.SetValue(DataFieldUpperErrorXProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for lower Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldLowerErrorY
        {
            get => (string)this.GetValue(DataFieldLowerErrorYProperty);
            set => this.SetValue(DataFieldLowerErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the data field for upper Y error.
        /// </summary>
        /// <value>The data field name. The default is <c>null</c>.</value>
        public string DataFieldUpperErrorY
        {
            get => (string)this.GetValue(DataFieldUpperErrorYProperty);
            set => this.SetValue(DataFieldUpperErrorYProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the error bars.
        /// </summary>
        /// <value>The error bar color. The default is <see cref="Colors.Black"/>.</value>
        public Color ErrorBarColor
        {
            get => (Color)this.GetValue(ErrorBarColorProperty);
            set => this.SetValue(ErrorBarColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the error bar end caps.
        /// </summary>
        /// <value>The stop width in pixels. The default is <c>4.0</c>.</value>
        public double ErrorBarStopWidth
        {
            get => (double)this.GetValue(ErrorBarStopWidthProperty);
            set => this.SetValue(ErrorBarStopWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the error bars.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double ErrorBarStrokeThickness
        {
            get => (double)this.GetValue(ErrorBarStrokeThicknessProperty);
            set => this.SetValue(ErrorBarStrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum error size relative to marker size.
        /// </summary>
        /// <value>The minimum error size. The default is <c>0.0</c>.</value>
        /// <remarks>Error bars smaller than this size (relative to marker size) will not be shown.</remarks>
        public double MinimumErrorSize
        {
            get => (double)this.GetValue(MinimumErrorSizeProperty);
            set => this.SetValue(MinimumErrorSizeProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.ScatterErrorSeries s)
            {
                s.DataFieldErrorX = this.DataFieldErrorX;
                s.DataFieldErrorY = this.DataFieldErrorY;
                s.DataFieldLowerErrorX = this.DataFieldLowerErrorX;
                s.DataFieldUpperErrorX = this.DataFieldUpperErrorX;
                s.DataFieldLowerErrorY = this.DataFieldLowerErrorY;
                s.DataFieldUpperErrorY = this.DataFieldUpperErrorY;
                s.ErrorBarColor = this.ErrorBarColor.ToOxyColor();
                s.ErrorBarStopWidth = this.ErrorBarStopWidth;
                s.ErrorBarStrokeThickness = this.ErrorBarStrokeThickness;
                s.MinimumErrorSize = this.MinimumErrorSize;
            }
        }
    }
}
