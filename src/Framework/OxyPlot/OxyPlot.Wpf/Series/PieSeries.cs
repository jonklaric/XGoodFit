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
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.PieSeries"/>.
    /// </summary>
    /// <remarks>
    /// A pie series displays data as slices of a circular pie chart. Each slice
    /// represents a proportion of the whole, making it useful for showing
    /// percentage or proportional data.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF.
    /// </remarks>
    public class PieSeries : ItemsSeries
    {
        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> StrokeProperty = AvaloniaProperty.Register<PieSeries, Color>(
            nameof(Stroke),
            Colors.White);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(StrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="Diameter"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> DiameterProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(Diameter),
            1.0);

        /// <summary>
        /// Identifies the <see cref="InnerDiameter"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> InnerDiameterProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(InnerDiameter),
            0.0);

        /// <summary>
        /// Identifies the <see cref="StartAngle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StartAngleProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(StartAngle),
            0.0);

        /// <summary>
        /// Identifies the <see cref="AngleSpan"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AngleSpanProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(AngleSpan),
            360.0);

        /// <summary>
        /// Identifies the <see cref="AngleIncrement"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> AngleIncrementProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(AngleIncrement),
            1.0);

        /// <summary>
        /// Identifies the <see cref="LegendFormat"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LegendFormatProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(LegendFormat),
            null);

        /// <summary>
        /// Identifies the <see cref="OutsideLabelFormat"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> OutsideLabelFormatProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(OutsideLabelFormat),
            "{2:0} %");

        /// <summary>
        /// Identifies the <see cref="InsideLabelColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> InsideLabelColorProperty = AvaloniaProperty.Register<PieSeries, Color>(
            nameof(InsideLabelColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="InsideLabelFormat"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> InsideLabelFormatProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(InsideLabelFormat),
            "{1}");

        /// <summary>
        /// Identifies the <see cref="InsideLabelPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> InsideLabelPositionProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(InsideLabelPosition),
            0.5);

        /// <summary>
        /// Identifies the <see cref="AreInsideLabelsAngled"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> AreInsideLabelsAngledProperty = AvaloniaProperty.Register<PieSeries, bool>(
            nameof(AreInsideLabelsAngled),
            false);

        /// <summary>
        /// Identifies the <see cref="TickDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TickDistanceProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(TickDistance),
            0.0);

        /// <summary>
        /// Identifies the <see cref="TickRadialLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TickRadialLengthProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(TickRadialLength),
            6.0);

        /// <summary>
        /// Identifies the <see cref="TickHorizontalLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TickHorizontalLengthProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(TickHorizontalLength),
            8.0);

        /// <summary>
        /// Identifies the <see cref="TickLabelDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TickLabelDistanceProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(TickLabelDistance),
            4.0);

        /// <summary>
        /// Identifies the <see cref="ExplodedDistance"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ExplodedDistanceProperty = AvaloniaProperty.Register<PieSeries, double>(
            nameof(ExplodedDistance),
            0.0);

        /// <summary>
        /// Identifies the <see cref="LabelField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LabelFieldProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(LabelField),
            null);

        /// <summary>
        /// Identifies the <see cref="ValueField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> ValueFieldProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(ValueField),
            null);

        /// <summary>
        /// Identifies the <see cref="ColorField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> ColorFieldProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(ColorField),
            null);

        /// <summary>
        /// Identifies the <see cref="IsExplodedField"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> IsExplodedFieldProperty = AvaloniaProperty.Register<PieSeries, string>(
            nameof(IsExplodedField),
            null);

        /// <summary>
        /// Initializes static members of the <see cref="PieSeries"/> class.
        /// </summary>
        static PieSeries()
        {
            StrokeProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            DiameterProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            InnerDiameterProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            StartAngleProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            AngleSpanProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            AngleIncrementProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            LegendFormatProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            OutsideLabelFormatProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            InsideLabelColorProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            InsideLabelFormatProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            InsideLabelPositionProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            AreInsideLabelsAngledProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            TickDistanceProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            TickRadialLengthProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            TickHorizontalLengthProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            TickLabelDistanceProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            ExplodedDistanceProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
            LabelFieldProperty.Changed.AddClassHandler<PieSeries>(DataChanged);
            ValueFieldProperty.Changed.AddClassHandler<PieSeries>(DataChanged);
            ColorFieldProperty.Changed.AddClassHandler<PieSeries>(DataChanged);
            IsExplodedFieldProperty.Changed.AddClassHandler<PieSeries>(DataChanged);
            TrackerFormatStringProperty.OverrideDefaultValue<PieSeries>(OxyPlot.Series.PieSeries.DefaultTrackerFormatString);
            TrackerFormatStringProperty.Changed.AddClassHandler<PieSeries>(AppearanceChanged);
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
