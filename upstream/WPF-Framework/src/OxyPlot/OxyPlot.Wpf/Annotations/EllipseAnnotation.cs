// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EllipseAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.EllipseAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.EllipseAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// An ellipse annotation displays an ellipse at a specified location with a given width and height.
    /// The ellipse can be filled and stroked, and can include text.
    /// </remarks>
    public class EllipseAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(
                nameof(X),
                typeof(double),
                typeof(EllipseAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(
                nameof(Y),
                typeof(double),
                typeof(EllipseAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(double),
                typeof(EllipseAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Height"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(
                nameof(Height),
                typeof(double),
                typeof(EllipseAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="EllipseAnnotation"/> class.
        /// </summary>
        public EllipseAnnotation()
        {
            this.InternalAnnotation = new Annotations.EllipseAnnotation();
        }

        /// <summary>
        /// Gets or sets the x-coordinate of the ellipse center.
        /// </summary>
        /// <value>The x-coordinate. The default is <c>0.0</c>.</value>
        public double X
        {
            get => (double)this.GetValue(XProperty);
            set => this.SetValue(XProperty, value);
        }

        /// <summary>
        /// Gets or sets the y-coordinate of the ellipse center.
        /// </summary>
        /// <value>The y-coordinate. The default is <c>0.0</c>.</value>
        public double Y
        {
            get => (double)this.GetValue(YProperty);
            set => this.SetValue(YProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the ellipse.
        /// </summary>
        /// <value>The width. The default is <see cref="double.NaN"/>.</value>
        public new double Width
        {
            get => (double)this.GetValue(WidthProperty);
            set => this.SetValue(WidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the ellipse.
        /// </summary>
        /// <value>The height. The default is <see cref="double.NaN"/>.</value>
        public new double Height
        {
            get => (double)this.GetValue(HeightProperty);
            set => this.SetValue(HeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum x-coordinate (left edge) of the ellipse.
        /// </summary>
        /// <remarks>This is a computed property based on X and Width for backward compatibility.</remarks>
        public double MinimumX
        {
            get => this.X - (this.Width / 2);
            set
            {
                var maxX = this.MaximumX;
                this.Width = maxX - value;
                this.X = (value + maxX) / 2;
            }
        }

        /// <summary>
        /// Gets or sets the maximum x-coordinate (right edge) of the ellipse.
        /// </summary>
        /// <remarks>This is a computed property based on X and Width for backward compatibility.</remarks>
        public double MaximumX
        {
            get => this.X + (this.Width / 2);
            set
            {
                var minX = this.MinimumX;
                this.Width = value - minX;
                this.X = (minX + value) / 2;
            }
        }

        /// <summary>
        /// Gets or sets the minimum y-coordinate (bottom edge) of the ellipse.
        /// </summary>
        /// <remarks>This is a computed property based on Y and Height for backward compatibility.</remarks>
        public double MinimumY
        {
            get => this.Y - (this.Height / 2);
            set
            {
                var maxY = this.MaximumY;
                this.Height = maxY - value;
                this.Y = (value + maxY) / 2;
            }
        }

        /// <summary>
        /// Gets or sets the maximum y-coordinate (top edge) of the ellipse.
        /// </summary>
        /// <remarks>This is a computed property based on Y and Height for backward compatibility.</remarks>
        public double MaximumY
        {
            get => this.Y + (this.Height / 2);
            set
            {
                var minY = this.MinimumY;
                this.Height = value - minY;
                this.Y = (minY + value) / 2;
            }
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.EllipseAnnotation"/> instance.</returns>
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

            if (this.InternalAnnotation is Annotations.EllipseAnnotation a)
            {
                a.X = this.X;
                a.Y = this.Y;
                a.Width = this.Width;
                a.Height = this.Height;
            }
        }
    }
}
