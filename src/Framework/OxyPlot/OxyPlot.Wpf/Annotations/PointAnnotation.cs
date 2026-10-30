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
    using Avalonia;
    using Avalonia.Layout;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.PointAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A point annotation displays a marker at a specified location.
    /// The marker shape, size, and colors can be customized.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public class PointAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> XProperty = AvaloniaProperty.Register<PointAnnotation, double>(
            nameof(X),
            0.0);

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> YProperty = AvaloniaProperty.Register<PointAnnotation, double>(
            nameof(Y),
            0.0);

        /// <summary>
        /// Identifies the <see cref="Size"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<PointAnnotation, double>(
            nameof(Size),
            4.0);

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TextMarginProperty = AvaloniaProperty.Register<PointAnnotation, double>(
            nameof(TextMargin),
            2.0);

        /// <summary>
        /// Identifies the <see cref="Shape"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<MarkerType> ShapeProperty = AvaloniaProperty.Register<PointAnnotation, MarkerType>(
            nameof(Shape),
            MarkerType.Circle);

        /// <summary>
        /// Identifies the <see cref="CustomOutline"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<ScreenPoint[]> CustomOutlineProperty = AvaloniaProperty.Register<PointAnnotation, ScreenPoint[]>(
            nameof(CustomOutline),
            null);

        /// <summary>
        /// Initializes static members of the <see cref="PointAnnotation"/> class.
        /// </summary>
        static PointAnnotation()
        {
            XProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            YProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            SizeProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            TextMarginProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            ShapeProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            CustomOutlineProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
            // Override TextVerticalAlignment default to Top so text appears below the point
            TextVerticalAlignmentProperty.OverrideDefaultValue<PointAnnotation>(VerticalAlignment.Top);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<PointAnnotation>(AppearanceChanged);
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
