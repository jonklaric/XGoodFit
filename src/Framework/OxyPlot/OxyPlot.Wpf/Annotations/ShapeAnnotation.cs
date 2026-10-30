// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShapeAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an abstract base class for shape annotations.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Represents an abstract base class for shape annotations.
    /// </summary>
    /// <remarks>
    /// This class provides common properties for shape-based annotations,
    /// including fill color, stroke color, and stroke thickness.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public abstract class ShapeAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Fill"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> FillProperty = AvaloniaProperty.Register<ShapeAnnotation, Color>(
            nameof(Fill),
            Colors.LightBlue);

        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> StrokeProperty = AvaloniaProperty.Register<ShapeAnnotation, Color>(
            nameof(Stroke),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<ShapeAnnotation, double>(
            nameof(StrokeThickness),
            0.0);

        /// <summary>
        /// Initializes static members of the <see cref="ShapeAnnotation" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static ShapeAnnotation()
        {
            FillProperty.Changed.AddClassHandler<ShapeAnnotation>(AppearanceChanged);
            StrokeProperty.Changed.AddClassHandler<ShapeAnnotation>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<ShapeAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Gets or sets the fill color of the shape.
        /// </summary>
        /// <value>The fill color. The default is <see cref="Colors.LightBlue"/>.</value>
        public Color Fill
        {
            get => (Color)this.GetValue(FillProperty);
            set => this.SetValue(FillProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color of the shape border.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color Stroke
        {
            get => (Color)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the shape border.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>0.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot annotation.
        /// </summary>
        public override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAnnotation is Annotations.ShapeAnnotation a)
            {
                a.Fill = this.Fill.ToOxyColor();
                a.Stroke = this.Stroke.ToOxyColor();
                a.StrokeThickness = this.StrokeThickness;
            }
        }
    }
}
