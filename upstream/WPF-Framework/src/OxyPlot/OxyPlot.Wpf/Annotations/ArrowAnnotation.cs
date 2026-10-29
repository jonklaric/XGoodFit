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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.ArrowAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// An arrow annotation draws an arrow between two points on the plot.
    /// The arrow can have customizable head dimensions and line style.
    /// </remarks>
    public class ArrowAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="ArrowDirection"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ArrowDirectionProperty =
            DependencyProperty.Register(
                nameof(ArrowDirection),
                typeof(ScreenVector),
                typeof(ArrowAnnotation),
                new PropertyMetadata(DataChanged));

        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(ArrowAnnotation),
                new PropertyMetadata(Colors.Blue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="EndPoint"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EndPointProperty =
            DependencyProperty.Register(
                nameof(EndPoint),
                typeof(DataPoint),
                typeof(ArrowAnnotation),
                new PropertyMetadata(AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HeadLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeadLengthProperty =
            DependencyProperty.Register(
                nameof(HeadLength),
                typeof(double),
                typeof(ArrowAnnotation),
                new PropertyMetadata(10.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HeadWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeadWidthProperty =
            DependencyProperty.Register(
                nameof(HeadWidth),
                typeof(double),
                typeof(ArrowAnnotation),
                new PropertyMetadata(3.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineJoinProperty =
            DependencyProperty.Register(
                nameof(LineJoin),
                typeof(LineJoin),
                typeof(ArrowAnnotation),
                new UIPropertyMetadata(LineJoin.Miter, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(ArrowAnnotation),
                new PropertyMetadata(LineStyle.Solid, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StartPoint"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartPointProperty =
            DependencyProperty.Register(
                nameof(StartPoint),
                typeof(DataPoint),
                typeof(ArrowAnnotation),
                new PropertyMetadata(AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ArrowAnnotation),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Veeness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VeenessProperty =
            DependencyProperty.Register(
                nameof(Veeness),
                typeof(double),
                typeof(ArrowAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="ArrowAnnotation"/> class.
        /// </summary>
        static ArrowAnnotation()
        {
            TextHorizontalAlignmentProperty.OverrideMetadata(typeof(ArrowAnnotation), new FrameworkPropertyMetadata(HorizontalAlignment.Left, AppearanceChanged));
            TextVerticalAlignmentProperty.OverrideMetadata(typeof(ArrowAnnotation), new FrameworkPropertyMetadata(VerticalAlignment.Bottom, AppearanceChanged));
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
