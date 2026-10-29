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
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.RectangleAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A rectangle annotation draws a filled rectangle on the plot,
    /// defined by minimum and maximum X and Y coordinates.
    /// </remarks>
    public class RectangleAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty =
            DependencyProperty.Register(
                nameof(MaximumX),
                typeof(double),
                typeof(RectangleAnnotation),
                new PropertyMetadata(double.MaxValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumYProperty =
            DependencyProperty.Register(
                nameof(MaximumY),
                typeof(double),
                typeof(RectangleAnnotation),
                new PropertyMetadata(double.MaxValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty =
            DependencyProperty.Register(
                nameof(MinimumX),
                typeof(double),
                typeof(RectangleAnnotation),
                new PropertyMetadata(double.MinValue, DataChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumYProperty =
            DependencyProperty.Register(
                nameof(MinimumY),
                typeof(double),
                typeof(RectangleAnnotation),
                new PropertyMetadata(double.MinValue, DataChanged));

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
