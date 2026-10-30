// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectangleAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.RectangleAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.RectangleAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A rectangle annotation draws a filled rectangle on the plot,
    /// defined by minimum and maximum X and Y coordinates.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class RectangleAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumXProperty = AvaloniaProperty.Register<RectangleAnnotation, double>(
            nameof(MaximumX),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MaximumYProperty = AvaloniaProperty.Register<RectangleAnnotation, double>(
            nameof(MaximumY),
            double.MaxValue);

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumXProperty = AvaloniaProperty.Register<RectangleAnnotation, double>(
            nameof(MinimumX),
            double.MinValue);

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumYProperty = AvaloniaProperty.Register<RectangleAnnotation, double>(
            nameof(MinimumY),
            double.MinValue);

        /// <summary>
        /// Initializes static members of the <see cref="RectangleAnnotation" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static RectangleAnnotation()
        {
            MaximumXProperty.Changed.AddClassHandler<RectangleAnnotation>(DataChanged);
            MaximumYProperty.Changed.AddClassHandler<RectangleAnnotation>(DataChanged);
            MinimumXProperty.Changed.AddClassHandler<RectangleAnnotation>(DataChanged);
            MinimumYProperty.Changed.AddClassHandler<RectangleAnnotation>(DataChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RectangleAnnotation"/> class.
        /// </summary>
        public RectangleAnnotation()
        {
            this.InternalAnnotation = new Annotations.RectangleAnnotation();
        }

        /// <summary>
        /// Gets or sets the maximum X coordinate of the rectangle.
        /// </summary>
        /// <value>The maximum X. The default is <see cref="double.MaxValue"/>.</value>
        public double MaximumX
        {
            get => (double)this.GetValue(MaximumXProperty);
            set => this.SetValue(MaximumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum Y coordinate of the rectangle.
        /// </summary>
        /// <value>The maximum Y. The default is <see cref="double.MaxValue"/>.</value>
        public double MaximumY
        {
            get => (double)this.GetValue(MaximumYProperty);
            set => this.SetValue(MaximumYProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum X coordinate of the rectangle.
        /// </summary>
        /// <value>The minimum X. The default is <see cref="double.MinValue"/>.</value>
        public double MinimumX
        {
            get => (double)this.GetValue(MinimumXProperty);
            set => this.SetValue(MinimumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum Y coordinate of the rectangle.
        /// </summary>
        /// <value>The minimum Y. The default is <see cref="double.MinValue"/>.</value>
        public double MinimumY
        {
            get => (double)this.GetValue(MinimumYProperty);
            set => this.SetValue(MinimumYProperty, value);
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

            if (this.InternalAnnotation is Annotations.RectangleAnnotation a)
            {
                a.MinimumX = this.MinimumX;
                a.MaximumX = this.MaximumX;
                a.MinimumY = this.MinimumY;
                a.MaximumY = this.MaximumY;
            }
        }
    }
}
