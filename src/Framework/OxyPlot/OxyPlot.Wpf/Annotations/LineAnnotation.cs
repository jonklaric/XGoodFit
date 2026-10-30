// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LineAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.LineAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Layout;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.LineAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A line annotation draws a line on the plot. It can be horizontal, vertical,
    /// or defined by a linear equation (y = mx + b).
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public class LineAnnotation : PathAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Type"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineAnnotationType> TypeProperty = AvaloniaProperty.Register<LineAnnotation, LineAnnotationType>(
            nameof(Type),
            LineAnnotationType.LinearEquation);

        /// <summary>
        /// Identifies the <see cref="Intercept"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> InterceptProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(Intercept),
            0.0);

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumXProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(MaximumX),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumYProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(MaximumY),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumXProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(MinimumX),
            double.MinValue);

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumYProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(MinimumY),
            double.MinValue);

        /// <summary>
        /// Identifies the <see cref="Slope"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> SlopeProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(Slope),
            0.0);

        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(X),
            0.0);

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<LineAnnotation, double>(
            nameof(Y),
            0.0);

        /// <summary>
        /// Initializes static members of the <see cref="LineAnnotation"/> class.
        /// </summary>
        static LineAnnotation()
        {
            TypeProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            InterceptProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            MaximumXProperty.Changed.AddClassHandler<LineAnnotation>(DataChanged);
            MaximumYProperty.Changed.AddClassHandler<LineAnnotation>(DataChanged);
            MinimumXProperty.Changed.AddClassHandler<LineAnnotation>(DataChanged);
            MinimumYProperty.Changed.AddClassHandler<LineAnnotation>(DataChanged);
            SlopeProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            XProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            YProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            TextHorizontalAlignmentProperty.OverrideDefaultValue<LineAnnotation>(HorizontalAlignment.Right);
            TextHorizontalAlignmentProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
            TextVerticalAlignmentProperty.OverrideDefaultValue<LineAnnotation>(VerticalAlignment.Top);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<LineAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineAnnotation"/> class.
        /// </summary>
        public LineAnnotation()
        {
            this.InternalAnnotation = new Annotations.LineAnnotation();
        }

        /// <summary>
        /// Gets or sets the Y-intercept of the line (for linear equations).
        /// </summary>
        /// <value>The intercept. The default is <c>0.0</c>.</value>
        public double Intercept
        {
            get => (double)this.GetValue(InterceptProperty);
            set => this.SetValue(InterceptProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum X coordinate of the line.
        /// </summary>
        /// <value>The maximum X. The default is <see cref="double.MaxValue"/>.</value>
        public double MaximumX
        {
            get => (double)this.GetValue(MaximumXProperty);
            set => this.SetValue(MaximumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum Y coordinate of the line.
        /// </summary>
        /// <value>The maximum Y. The default is <see cref="double.MaxValue"/>.</value>
        public double MaximumY
        {
            get => (double)this.GetValue(MaximumYProperty);
            set => this.SetValue(MaximumYProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum X coordinate of the line.
        /// </summary>
        /// <value>The minimum X. The default is <see cref="double.MinValue"/>.</value>
        public double MinimumX
        {
            get => (double)this.GetValue(MinimumXProperty);
            set => this.SetValue(MinimumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum Y coordinate of the line.
        /// </summary>
        /// <value>The minimum Y. The default is <see cref="double.MinValue"/>.</value>
        public double MinimumY
        {
            get => (double)this.GetValue(MinimumYProperty);
            set => this.SetValue(MinimumYProperty, value);
        }

        /// <summary>
        /// Gets or sets the slope of the line (for linear equations).
        /// </summary>
        /// <value>The slope. The default is <c>0.0</c>.</value>
        public double Slope
        {
            get => (double)this.GetValue(SlopeProperty);
            set => this.SetValue(SlopeProperty, value);
        }

        /// <summary>
        /// Gets or sets the type of line annotation.
        /// </summary>
        /// <value>The line type. The default is <see cref="LineAnnotationType.LinearEquation"/>.</value>
        public LineAnnotationType Type
        {
            get => (LineAnnotationType)this.GetValue(TypeProperty);
            set => this.SetValue(TypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the X coordinate (for vertical lines).
        /// </summary>
        /// <value>The X coordinate. The default is <c>0.0</c>.</value>
        public double X
        {
            get => (double)this.GetValue(XProperty);
            set => this.SetValue(XProperty, value);
        }

        /// <summary>
        /// Gets or sets the Y coordinate (for horizontal lines).
        /// </summary>
        /// <value>The Y coordinate. The default is <c>0.0</c>.</value>
        public double Y
        {
            get => (double)this.GetValue(YProperty);
            set => this.SetValue(YProperty, value);
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

            if (this.InternalAnnotation is Annotations.LineAnnotation a)
            {
                a.Type = this.Type;
                a.Slope = this.Slope;
                a.Intercept = this.Intercept;
                a.X = this.X;
                a.Y = this.Y;
                a.MinimumX = this.MinimumX;
                a.MaximumX = this.MaximumX;
                a.MinimumY = this.MinimumY;
                a.MaximumY = this.MaximumY;
            }
        }
    }
}
