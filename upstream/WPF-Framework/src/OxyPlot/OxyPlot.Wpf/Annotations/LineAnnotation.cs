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
    using System.Windows;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.LineAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A line annotation draws a line on the plot. It can be horizontal, vertical,
    /// or defined by a linear equation (y = mx + b).
    /// </remarks>
    public class LineAnnotation : PathAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Type"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(LineAnnotationType),
                typeof(LineAnnotation),
                new PropertyMetadata(LineAnnotationType.LinearEquation, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Intercept"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InterceptProperty =
            DependencyProperty.Register(
                nameof(Intercept),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty =
            DependencyProperty.Register(
                nameof(MaximumX),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(double.MaxValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumYProperty =
            DependencyProperty.Register(
                nameof(MaximumY),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(double.MaxValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty =
            DependencyProperty.Register(
                nameof(MinimumX),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(double.MinValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumYProperty =
            DependencyProperty.Register(
                nameof(MinimumY),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(double.MinValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="Slope"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SlopeProperty =
            DependencyProperty.Register(
                nameof(Slope),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(
                nameof(X),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(
                nameof(Y),
                typeof(double),
                typeof(LineAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="LineAnnotation"/> class.
        /// </summary>
        static LineAnnotation()
        {
            TextHorizontalAlignmentProperty.OverrideMetadata(typeof(LineAnnotation), new FrameworkPropertyMetadata(HorizontalAlignment.Right, AppearanceChanged));
            TextVerticalAlignmentProperty.OverrideMetadata(typeof(LineAnnotation), new FrameworkPropertyMetadata(VerticalAlignment.Top, AppearanceChanged));
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
