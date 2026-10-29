// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PointAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.PointAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.PointAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A point annotation displays a marker at a specified location.
    /// The marker shape, size, and colors can be customized.
    /// </remarks>
    public class PointAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(
                nameof(X),
                typeof(double),
                typeof(PointAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(
                nameof(Y),
                typeof(double),
                typeof(PointAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register(
                nameof(Size),
                typeof(double),
                typeof(PointAnnotation),
                new PropertyMetadata(4.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(
                nameof(TextMargin),
                typeof(double),
                typeof(PointAnnotation),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Shape"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShapeProperty =
            DependencyProperty.Register(
                nameof(Shape),
                typeof(MarkerType),
                typeof(PointAnnotation),
                new PropertyMetadata(MarkerType.Circle, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="CustomOutline"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CustomOutlineProperty =
            DependencyProperty.Register(
                nameof(CustomOutline),
                typeof(ScreenPoint[]),
                typeof(PointAnnotation),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="PointAnnotation"/> class.
        /// </summary>
        static PointAnnotation()
        {
            // Override TextVerticalAlignment default to Top so text appears below the point
            TextVerticalAlignmentProperty.OverrideMetadata(typeof(PointAnnotation), new FrameworkPropertyMetadata(VerticalAlignment.Top, AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointAnnotation"/> class.
        /// </summary>
        public PointAnnotation()
        {
            this.InternalAnnotation = new Annotations.PointAnnotation();
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the point.
        /// </summary>
        /// <value>The x-coordinate. The default is <c>0.0</c>.</value>
        public double X
        {
            get => (double)this.GetValue(XProperty);
            set => this.SetValue(XProperty, value);
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the point.
        /// </summary>
        /// <value>The y-coordinate. The default is <c>0.0</c>.</value>
        public double Y
        {
            get => (double)this.GetValue(YProperty);
            set => this.SetValue(YProperty, value);
        }

        /// <summary>
        /// Gets or sets the size of the point marker.
        /// </summary>
        /// <value>The size. The default is <c>4.0</c>.</value>
        public double Size
        {
            get => (double)this.GetValue(SizeProperty);
            set => this.SetValue(SizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the distance between the point and the text.
        /// </summary>
        /// <value>The text margin. The default is <c>2.0</c>.</value>
        public double TextMargin
        {
            get => (double)this.GetValue(TextMarginProperty);
            set => this.SetValue(TextMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the shape of the point marker.
        /// </summary>
        /// <value>The marker shape. The default is <see cref="MarkerType.Circle"/>.</value>
        public MarkerType Shape
        {
            get => (MarkerType)this.GetValue(ShapeProperty);
            set => this.SetValue(ShapeProperty, value);
        }

        /// <summary>
        /// Gets or sets a custom polygon outline for the point marker.
        /// </summary>
        /// <value>A polygon outline. The default is <c>null</c>.</value>
        /// <remarks>Set <see cref="Shape"/> to <see cref="MarkerType.Custom"/> to use this property.</remarks>
        public ScreenPoint[] CustomOutline
        {
            get => (ScreenPoint[])this.GetValue(CustomOutlineProperty);
            set => this.SetValue(CustomOutlineProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.PointAnnotation"/> instance.</returns>
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

            if (this.InternalAnnotation is Annotations.PointAnnotation a)
            {
                a.X = this.X;
                a.Y = this.Y;
                a.Size = this.Size;
                a.TextMargin = this.TextMargin;
                a.Shape = this.Shape;
                a.CustomOutline = this.CustomOutline;
            }
        }
    }
}
