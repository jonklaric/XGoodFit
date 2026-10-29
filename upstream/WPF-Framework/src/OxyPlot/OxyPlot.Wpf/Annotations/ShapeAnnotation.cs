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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents an abstract base class for shape annotations.
    /// </summary>
    /// <remarks>
    /// This class provides common properties for shape-based annotations,
    /// including fill color, stroke color, and stroke thickness.
    /// </remarks>
    public abstract class ShapeAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Fill"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(
                nameof(Fill),
                typeof(Color),
                typeof(ShapeAnnotation),
                new PropertyMetadata(Colors.LightBlue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(
                nameof(Stroke),
                typeof(Color),
                typeof(ShapeAnnotation),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(ShapeAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

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
