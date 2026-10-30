// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ArrowAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.ArrowAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Layout;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.ArrowAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// An arrow annotation draws an arrow between two points on the plot.
    /// The arrow can have customizable head dimensions and line style.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public class ArrowAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="ArrowDirection"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ScreenVector> ArrowDirectionProperty = AvaloniaProperty.Register<ArrowAnnotation, ScreenVector>(
            nameof(ArrowDirection));

        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<ArrowAnnotation, Color>(
            nameof(Color),
            Colors.Blue);

        /// <summary>
        /// Identifies the <see cref="EndPoint"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DataPoint> EndPointProperty = AvaloniaProperty.Register<ArrowAnnotation, DataPoint>(
            nameof(EndPoint));

        /// <summary>
        /// Identifies the <see cref="HeadLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> HeadLengthProperty = AvaloniaProperty.Register<ArrowAnnotation, double>(
            nameof(HeadLength),
            10.0);

        /// <summary>
        /// Identifies the <see cref="HeadWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> HeadWidthProperty = AvaloniaProperty.Register<ArrowAnnotation, double>(
            nameof(HeadWidth),
            3.0);

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineJoin> LineJoinProperty = AvaloniaProperty.Register<ArrowAnnotation, LineJoin>(
            nameof(LineJoin),
            LineJoin.Miter);

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyleProperty = AvaloniaProperty.Register<ArrowAnnotation, LineStyle>(
            nameof(LineStyle),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="StartPoint"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DataPoint> StartPointProperty = AvaloniaProperty.Register<ArrowAnnotation, DataPoint>(
            nameof(StartPoint));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<ArrowAnnotation, double>(
            nameof(StrokeThickness),
            2.0);

        /// <summary>
        /// Identifies the <see cref="Veeness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> VeenessProperty = AvaloniaProperty.Register<ArrowAnnotation, double>(
            nameof(Veeness),
            0.0);

        /// <summary>
        /// Initializes static members of the <see cref="ArrowAnnotation"/> class.
        /// </summary>
        static ArrowAnnotation()
        {
            ArrowDirectionProperty.Changed.AddClassHandler<ArrowAnnotation>(DataChanged);
            ColorProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            EndPointProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            HeadLengthProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            HeadWidthProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            LineJoinProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            LineStyleProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            StartPointProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            VeenessProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            TextHorizontalAlignmentProperty.OverrideDefaultValue<ArrowAnnotation>(HorizontalAlignment.Left);
            TextHorizontalAlignmentProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
            TextVerticalAlignmentProperty.OverrideDefaultValue<ArrowAnnotation>(VerticalAlignment.Bottom);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<ArrowAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArrowAnnotation"/> class.
        /// </summary>
        public ArrowAnnotation()
        {
            this.InternalAnnotation = new Annotations.ArrowAnnotation();
        }

        /// <summary>
        /// Gets or sets the arrow direction in screen coordinates.
        /// </summary>
        /// <value>The arrow direction vector.</value>
        /// <remarks>This overrides <see cref="StartPoint"/> if set.</remarks>
        public ScreenVector ArrowDirection
        {
            get => (ScreenVector)this.GetValue(ArrowDirectionProperty);
            set => this.SetValue(ArrowDirectionProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the arrow.
        /// </summary>
        /// <value>The color. The default is <see cref="Colors.Blue"/>.</value>
        public Color Color
        {
            get => (Color)this.GetValue(ColorProperty);
            set => this.SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the end point (arrow head position) in data coordinates.
        /// </summary>
        /// <value>The end point.</value>
        public DataPoint EndPoint
        {
            get => (DataPoint)this.GetValue(EndPointProperty);
            set => this.SetValue(EndPointProperty, value);
        }

        /// <summary>
        /// Gets or sets the length of the arrow head relative to stroke thickness.
        /// </summary>
        /// <value>The head length. The default is <c>10.0</c>.</value>
        public double HeadLength
        {
            get => (double)this.GetValue(HeadLengthProperty);
            set => this.SetValue(HeadLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the arrow head relative to stroke thickness.
        /// </summary>
        /// <value>The head width. The default is <c>3.0</c>.</value>
        public double HeadWidth
        {
            get => (double)this.GetValue(HeadWidthProperty);
            set => this.SetValue(HeadWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the line join style.
        /// </summary>
        /// <value>The line join. The default is <see cref="LineJoin.Miter"/>.</value>
        public LineJoin LineJoin
        {
            get => (LineJoin)this.GetValue(LineJoinProperty);
            set => this.SetValue(LineJoinProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the start point (arrow tail position) in data coordinates.
        /// </summary>
        /// <value>The start point.</value>
        /// <remarks>This is overridden by <see cref="ArrowDirection"/> if set.</remarks>
        public DataPoint StartPoint
        {
            get => (DataPoint)this.GetValue(StartPointProperty);
            set => this.SetValue(StartPointProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>2.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the veeness (inward curve) of the arrow head.
        /// </summary>
        /// <value>The veeness relative to thickness. The default is <c>0.0</c>.</value>
        public double Veeness
        {
            get => (double)this.GetValue(VeenessProperty);
            set => this.SetValue(VeenessProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.Annotation"/> instance.</returns>
        public override Annotations.Annotation CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAnnotation;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot annotation.
        /// </summary>
        public override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAnnotation is Annotations.ArrowAnnotation a)
            {
                a.StartPoint = this.StartPoint;
                a.EndPoint = this.EndPoint;
                a.ArrowDirection = this.ArrowDirection;
                a.HeadLength = this.HeadLength;
                a.HeadWidth = this.HeadWidth;
                a.Veeness = this.Veeness;
                a.Color = this.Color.ToOxyColor();
                a.StrokeThickness = this.StrokeThickness;
                a.LineStyle = this.LineStyle;
                a.LineJoin = this.LineJoin;
            }
        }
    }
}
