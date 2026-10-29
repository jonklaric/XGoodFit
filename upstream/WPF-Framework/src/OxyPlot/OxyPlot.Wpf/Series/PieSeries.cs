// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PieSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.PieSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.PieSeries"/>.
    /// </summary>
    /// <remarks>
    /// A pie series displays data as slices of a circular pie chart. Each slice
    /// represents a proportion of the whole, making it useful for showing
    /// percentage or proportional data.
    /// </remarks>
    public class PieSeries : ItemsSeries
    {
        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke),
                typeof(Color),
                typeof(PieSeries),
                new PropertyMetadata(Colors.White, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Diameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DiameterProperty =
            DependencyProperty.Register(
                nameof(Diameter),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="InnerDiameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InnerDiameterProperty =
            DependencyProperty.Register(
                nameof(InnerDiameter),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StartAngle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartAngleProperty =
            DependencyProperty.Register(
                nameof(StartAngle),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AngleSpan"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AngleSpanProperty =
            DependencyProperty.Register(
                nameof(AngleSpan),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(360.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AngleIncrement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AngleIncrementProperty =
            DependencyProperty.Register(
                nameof(AngleIncrement),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LegendFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LegendFormatProperty =
            DependencyProperty.Register(
                nameof(LegendFormat),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OutsideLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OutsideLabelFormatProperty =
            DependencyProperty.Register(
                nameof(OutsideLabelFormat),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata("{2:0} %", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="InsideLabelColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InsideLabelColorProperty =
            DependencyProperty.Register(
                nameof(InsideLabelColor),
                typeof(Color),
                typeof(PieSeries),
                new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="InsideLabelFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InsideLabelFormatProperty =
            DependencyProperty.Register(
                nameof(InsideLabelFormat),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata("{1}", AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="InsideLabelPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InsideLabelPositionProperty =
            DependencyProperty.Register(
                nameof(InsideLabelPosition),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(0.5, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="AreInsideLabelsAngled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AreInsideLabelsAngledProperty =
            DependencyProperty.Register(
                nameof(AreInsideLabelsAngled),
                typeof(bool),
                typeof(PieSeries),
                new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickDistanceProperty =
            DependencyProperty.Register(
                nameof(TickDistance),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickRadialLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickRadialLengthProperty =
            DependencyProperty.Register(
                nameof(TickRadialLength),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(6.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickHorizontalLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickHorizontalLengthProperty =
            DependencyProperty.Register(
                nameof(TickHorizontalLength),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(8.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TickLabelDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickLabelDistanceProperty =
            DependencyProperty.Register(
                nameof(TickLabelDistance),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ExplodedDistance"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExplodedDistanceProperty =
            DependencyProperty.Register(
                nameof(ExplodedDistance),
                typeof(double),
                typeof(PieSeries),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LabelField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFieldProperty =
            DependencyProperty.Register(
                nameof(LabelField),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="ValueField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ValueFieldProperty =
            DependencyProperty.Register(
                nameof(ValueField),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="ColorField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorFieldProperty =
            DependencyProperty.Register(
                nameof(ColorField),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="IsExplodedField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsExplodedFieldProperty =
            DependencyProperty.Register(
                nameof(IsExplodedField),
                typeof(string),
                typeof(PieSeries),
                new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="PieSeries"/> class.
        /// </summary>
        static PieSeries()
        {
            TrackerFormatStringProperty.OverrideMetadata(
                typeof(PieSeries),
                new PropertyMetadata(OxyPlot.Series.PieSeries.DefaultTrackerFormatString, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PieSeries"/> class.
        /// </summary>
        public PieSeries()
        {
            this.InternalSeries = new OxyPlot.Series.PieSeries();
        }

        /// <summary>
        /// Gets or sets the stroke color of the pie slices.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.White"/>.</value>
        public Color Stroke
        {
            get => (Color)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the pie slices.
        /// </summary>
        /// <value>The stroke thickness in pixels. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the diameter of the pie as a fraction of the available space.
        /// </summary>
        /// <value>The diameter (0-1). The default is <c>1.0</c>.</value>
        public double Diameter
        {
            get => (double)this.GetValue(DiameterProperty);
            set => this.SetValue(DiameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the inner diameter of the pie (for creating donut charts).
        /// </summary>
        /// <value>The inner diameter (0-1). The default is <c>0.0</c>.</value>
        /// <remarks>Set this to a value greater than 0 to create a donut chart.</remarks>
        public double InnerDiameter
        {
            get => (double)this.GetValue(InnerDiameterProperty);
            set => this.SetValue(InnerDiameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the start angle of the pie in degrees.
        /// </summary>
        /// <value>The start angle in degrees. The default is <c>0.0</c> (3 o'clock position).</value>
        public double StartAngle
        {
            get => (double)this.GetValue(StartAngleProperty);
            set => this.SetValue(StartAngleProperty, value);
        }

        /// <summary>
        /// Gets or sets the angular span of the pie in degrees.
        /// </summary>
        /// <value>The angle span in degrees. The default is <c>360.0</c>.</value>
        public double AngleSpan
        {
            get => (double)this.GetValue(AngleSpanProperty);
            set => this.SetValue(AngleSpanProperty, value);
        }

        /// <summary>
        /// Gets or sets the angle increment for rendering slices.
        /// </summary>
        /// <value>The angle increment in degrees. The default is <c>1.0</c>.</value>
        public double AngleIncrement
        {
            get => (double)this.GetValue(AngleIncrementProperty);
            set => this.SetValue(AngleIncrementProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for the legend.
        /// </summary>
        /// <value>The legend format string. The default is <c>null</c>.</value>
        public string LegendFormat
        {
            get => (string)this.GetValue(LegendFormatProperty);
            set => this.SetValue(LegendFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for labels outside the slices.
        /// </summary>
        /// <value>The outside label format string. The default is <c>"{2:0} %"</c>.</value>
        /// <remarks>
        /// The format arguments are: {0} = label, {1} = value, {2} = percentage.
        /// </remarks>
        public string OutsideLabelFormat
        {
            get => (string)this.GetValue(OutsideLabelFormatProperty);
            set => this.SetValue(OutsideLabelFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the inside labels.
        /// </summary>
        /// <value>The inside label color. The default is <see cref="MoreColors.Automatic"/>.</value>
        public Color InsideLabelColor
        {
            get => (Color)this.GetValue(InsideLabelColorProperty);
            set => this.SetValue(InsideLabelColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the format string for labels inside the slices.
        /// </summary>
        /// <value>The inside label format string. The default is <c>"{1}"</c>.</value>
        /// <remarks>
        /// The format arguments are: {0} = label, {1} = value, {2} = percentage.
        /// </remarks>
        public string InsideLabelFormat
        {
            get => (string)this.GetValue(InsideLabelFormatProperty);
            set => this.SetValue(InsideLabelFormatProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of inside labels as a fraction from center to edge.
        /// </summary>
        /// <value>The inside label position (0-1). The default is <c>0.5</c>.</value>
        public double InsideLabelPosition
        {
            get => (double)this.GetValue(InsideLabelPositionProperty);
            set => this.SetValue(InsideLabelPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether inside labels are angled along the slice.
        /// </summary>
        /// <value><c>true</c> if inside labels are angled; otherwise, <c>false</c>. The default is <c>false</c>.</value>
        public bool AreInsideLabelsAngled
        {
            get => (bool)this.GetValue(AreInsideLabelsAngledProperty);
            set => this.SetValue(AreInsideLabelsAngledProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from the edge of the pie slice to the tick line.
        /// </summary>
        /// <value>The tick distance in pixels. The default is <c>0.0</c>.</value>
        public double TickDistance
        {
            get => (double)this.GetValue(TickDistanceProperty);
            set => this.SetValue(TickDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the radial part of the tick line.
        /// </summary>
        /// <value>The tick radial length in pixels. The default is <c>6.0</c>.</value>
        public double TickRadialLength
        {
            get => (double)this.GetValue(TickRadialLengthProperty);
            set => this.SetValue(TickRadialLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the horizontal part of the tick line.
        /// </summary>
        /// <value>The tick horizontal length in pixels. The default is <c>8.0</c>.</value>
        public double TickHorizontalLength
        {
            get => (double)this.GetValue(TickHorizontalLengthProperty);
            set => this.SetValue(TickHorizontalLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance from the tick line to the outside label.
        /// </summary>
        /// <value>The tick label distance in pixels. The default is <c>4.0</c>.</value>
        public double TickLabelDistance
        {
            get => (double)this.GetValue(TickLabelDistanceProperty);
            set => this.SetValue(TickLabelDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance to explode slices marked as exploded.
        /// </summary>
        /// <value>The exploded distance as a fraction of the radius. The default is <c>0.0</c>.</value>
        public double ExplodedDistance
        {
            get => (double)this.GetValue(ExplodedDistanceProperty);
            set => this.SetValue(ExplodedDistanceProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the label.
        /// </summary>
        /// <value>The label field name. The default is <c>null</c>.</value>
        public string LabelField
        {
            get => (string)this.GetValue(LabelFieldProperty);
            set => this.SetValue(LabelFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the value.
        /// </summary>
        /// <value>The value field name. The default is <c>null</c>.</value>
        public string ValueField
        {
            get => (string)this.GetValue(ValueFieldProperty);
            set => this.SetValue(ValueFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property containing the color.
        /// </summary>
        /// <value>The color field name. The default is <c>null</c>.</value>
        public string ColorField
        {
            get => (string)this.GetValue(ColorFieldProperty);
            set => this.SetValue(ColorFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property indicating whether the item is exploded.
        /// </summary>
        /// <value>The is exploded field name. The default is <c>null</c>.</value>
        public string IsExplodedField
        {
            get => (string)this.GetValue(IsExplodedFieldProperty);
            set => this.SetValue(IsExplodedFieldProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.PieSeries"/> instance.</returns>
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

            if (series is OxyPlot.Series.PieSeries s)
            {
                s.Stroke = this.Stroke.ToOxyColor();
                s.StrokeThickness = this.StrokeThickness;
                s.Diameter = this.Diameter;
                s.InnerDiameter = this.InnerDiameter;
                s.StartAngle = this.StartAngle;
                s.AngleSpan = this.AngleSpan;
                s.AngleIncrement = this.AngleIncrement;

                s.LegendFormat = this.LegendFormat;

                s.OutsideLabelFormat = this.OutsideLabelFormat;
                s.InsideLabelColor = this.InsideLabelColor.ToOxyColor();
                s.InsideLabelFormat = this.InsideLabelFormat;
                s.InsideLabelPosition = this.InsideLabelPosition;
                s.AreInsideLabelsAngled = this.AreInsideLabelsAngled;

                s.TickDistance = this.TickDistance;
                s.TickRadialLength = this.TickRadialLength;
                s.TickHorizontalLength = this.TickHorizontalLength;
                s.TickLabelDistance = this.TickLabelDistance;

                s.ExplodedDistance = this.ExplodedDistance;

                s.LabelField = this.LabelField;
                s.ValueField = this.ValueField;
                s.ColorField = this.ColorField;
                s.IsExplodedField = this.IsExplodedField;
            }
        }
    }
}
