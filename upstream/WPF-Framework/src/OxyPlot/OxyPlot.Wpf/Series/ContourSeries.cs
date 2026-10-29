// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContourSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.ContourSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.ContourSeries"/>.
    /// </summary>
    /// <remarks>
    /// A contour series displays 2D data as contour lines connecting points
    /// of equal value, similar to topographic maps showing elevation.
    /// </remarks>
    public class ContourSeries : XYAxisSeries
    {
        /// <summary>
        /// Identifies the <see cref="ColumnCoordinates"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnCoordinatesProperty =
            DependencyProperty.Register(
                nameof(ColumnCoordinates),
                typeof(double[]),
                typeof(ContourSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="ContourLevelStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContourLevelStepProperty =
            DependencyProperty.Register(
                nameof(ContourLevelStep),
                typeof(double),
                typeof(ContourSeries),
                new PropertyMetadata(double.NaN, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ContourLevels"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContourLevelsProperty =
            DependencyProperty.Register(
                nameof(ContourLevels),
                typeof(double[]),
                typeof(ContourSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ContourColors"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ContourColorsProperty =
            DependencyProperty.Register(
                nameof(ContourColors),
                typeof(Color[]),
                typeof(ContourSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(double[,]),
                typeof(ContourSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="LabelBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelBackgroundProperty =
            DependencyProperty.Register(
                nameof(LabelBackground),
                typeof(Color),
                typeof(ContourSeries),
                new PropertyMetadata(Color.FromArgb(220, 255, 255, 255), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelStep"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelStepProperty =
            DependencyProperty.Register(
                nameof(LabelStep),
                typeof(int),
                typeof(ContourSeries),
                new PropertyMetadata(1, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(ContourSeries),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="RowCoordinates"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowCoordinatesProperty =
            DependencyProperty.Register(
                nameof(RowCoordinates),
                typeof(double[]),
                typeof(ContourSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ContourSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="ContourSeries"/> class.
        /// </summary>
        static ContourSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(ContourSeries),
                new PropertyMetadata(OxyPlot.Series.ContourSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContourSeries"/> class.
        /// </summary>
        public ContourSeries()
        {
            this.InternalSeries = new OxyPlot.Series.ContourSeries();
        }

        /// <summary>
        /// Gets or sets the column (X) coordinates for the data grid.
        /// </summary>
        /// <value>The column coordinates. The default is <c>null</c>.</value>
        public double[] ColumnCoordinates
        {
            get => (double[])this.GetValue(ColumnCoordinatesProperty);
            set => this.SetValue(ColumnCoordinatesProperty, value);
        }

        /// <summary>
        /// Gets or sets the step size between contour levels.
        /// </summary>
        /// <value>The contour level step. The default is <see cref="double.NaN"/>.</value>
        /// <remarks>This property is not used if <see cref="ContourLevels"/> is set.</remarks>
        public double ContourLevelStep
        {
            get => (double)this.GetValue(ContourLevelStepProperty);
            set => this.SetValue(ContourLevelStepProperty, value);
        }

        /// <summary>
        /// Gets or sets the explicit contour levels.
        /// </summary>
        /// <value>The contour levels. The default is <c>null</c>.</value>
        public double[] ContourLevels
        {
            get => (double[])this.GetValue(ContourLevelsProperty);
            set => this.SetValue(ContourLevelsProperty, value);
        }

        /// <summary>
        /// Gets or sets the colors for contour lines.
        /// </summary>
        /// <value>The contour colors. The default is <c>null</c>.</value>
        /// <remarks>
        /// These colors override the series color. If there are fewer colors
        /// than contour levels, the colors will cycle.
        /// </remarks>
        public Color[] ContourColors
        {
            get => (Color[])this.GetValue(ContourColorsProperty);
            set => this.SetValue(ContourColorsProperty, value);
        }

        /// <summary>
        /// Gets or sets the 2D data array.
        /// </summary>
        /// <value>The data array. The default is <c>null</c>.</value>
        public double[,] Data
        {
            get => (double[,])this.GetValue(DataProperty);
            set => this.SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for contour labels.
        /// </summary>
        /// <value>The label background color. The default is semi-transparent white.</value>
        public Color LabelBackground
        {
            get => (Color)this.GetValue(LabelBackgroundProperty);
            set => this.SetValue(LabelBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of contours per label.
        /// </summary>
        /// <value>The label step. The default is <c>1</c> (label every contour).</value>
        public int LabelStep
        {
            get => (int)this.GetValue(LabelStepProperty);
            set => this.SetValue(LabelStepProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style for contour lines.
        /// </summary>
        /// <value>The line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for contour value labels.
        /// </summary>
        /// <value>The label format string. The default is <c>null</c> (uses default number formatting).</value>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the row (Y) coordinates for the data grid.
        /// </summary>
        /// <value>The row coordinates. The default is <c>null</c>.</value>
        public double[] RowCoordinates
        {
            get => (double[])this.GetValue(RowCoordinatesProperty);
            set => this.SetValue(RowCoordinatesProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness for contour lines.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.ContourSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.ContourSeries s)
            {
                s.Color = this.Color.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.LineStyle = this.LineStyle;
                s.ColumnCoordinates = this.ColumnCoordinates;
                s.ContourLevelStep = this.ContourLevelStep;
                s.ContourLevels = this.ContourLevels;

                if (this.ContourColors != null)
                {
                    var oxyColors = new OxyColor[this.ContourColors.Length];
                    for (int i = 0; i < this.ContourColors.Length; i++)
                    {
                        oxyColors[i] = this.ContourColors[i].ToOxyColor();
                    }
                    s.ContourColors = oxyColors;
                }

                s.Data = this.Data;
                s.LabelBackground = this.LabelBackground.ToOxyColor();
                s.LabelFormatString = this.LabelFormatString;
                s.LabelStep = this.LabelStep;
                s.RowCoordinates = this.RowCoordinates;
            }
        }
    }
}
