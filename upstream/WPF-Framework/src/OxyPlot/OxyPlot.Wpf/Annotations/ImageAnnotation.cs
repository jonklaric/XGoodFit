// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.ImageAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.ImageAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// An image annotation displays an image at a specified position on the plot.
    /// The position, size, and alignment can all be customized.
    /// </remarks>
    public class ImageAnnotation : Annotation
    {
        /// <summary>
        /// Identifies the <see cref="ImageSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                nameof(ImageSource),
                typeof(OxyImage),
                typeof(ImageAnnotation),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register(
                nameof(X),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register(
                nameof(Y),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OffsetX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetXProperty =
            DependencyProperty.Register(
                nameof(OffsetX),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(0, PlotLengthUnit.ScreenUnits), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="OffsetY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OffsetYProperty =
            DependencyProperty.Register(
                nameof(OffsetY),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(0, PlotLengthUnit.ScreenUnits), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register(
                nameof(Width),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Height"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register(
                nameof(Height),
                typeof(PlotLength),
                typeof(ImageAnnotation),
                new PropertyMetadata(new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits), AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Opacity"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty OpacityProperty =
            DependencyProperty.Register(
                nameof(Opacity),
                typeof(double),
                typeof(ImageAnnotation),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Interpolate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InterpolateProperty =
            DependencyProperty.Register(
                nameof(Interpolate),
                typeof(bool),
                typeof(ImageAnnotation),
                new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="HorizontalAlignment"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty HorizontalAlignmentProperty =
            DependencyProperty.Register(
                nameof(HorizontalAlignment),
                typeof(OxyPlot.HorizontalAlignment),
                typeof(ImageAnnotation),
                new PropertyMetadata(OxyPlot.HorizontalAlignment.Center, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="VerticalAlignment"/> dependency property.
        /// </summary>
        public new static readonly DependencyProperty VerticalAlignmentProperty =
            DependencyProperty.Register(
                nameof(VerticalAlignment),
                typeof(OxyPlot.VerticalAlignment),
                typeof(ImageAnnotation),
                new PropertyMetadata(OxyPlot.VerticalAlignment.Middle, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageAnnotation"/> class.
        /// </summary>
        public ImageAnnotation()
        {
            this.InternalAnnotation = new Annotations.ImageAnnotation();
        }

        /// <summary>
        /// Gets or sets the image source.
        /// </summary>
        /// <value>The OxyImage to display. The default is <c>null</c>.</value>
        public OxyImage ImageSource
        {
            get => (OxyImage)this.GetValue(ImageSourceProperty);
            set => this.SetValue(ImageSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the X position of the image.
        /// </summary>
        /// <value>The X position. The default is 0.5 (center of plot area).</value>
        public PlotLength X
        {
            get => (PlotLength)this.GetValue(XProperty);
            set => this.SetValue(XProperty, value);
        }

        /// <summary>
        /// Gets or sets the Y position of the image.
        /// </summary>
        /// <value>The Y position. The default is 0.5 (center of plot area).</value>
        public PlotLength Y
        {
            get => (PlotLength)this.GetValue(YProperty);
            set => this.SetValue(YProperty, value);
        }

        /// <summary>
        /// Gets or sets the X offset from the position.
        /// </summary>
        /// <value>The X offset. The default is 0 screen units.</value>
        public PlotLength OffsetX
        {
            get => (PlotLength)this.GetValue(OffsetXProperty);
            set => this.SetValue(OffsetXProperty, value);
        }

        /// <summary>
        /// Gets or sets the Y offset from the position.
        /// </summary>
        /// <value>The Y offset. The default is 0 screen units.</value>
        public PlotLength OffsetY
        {
            get => (PlotLength)this.GetValue(OffsetYProperty);
            set => this.SetValue(OffsetYProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the image.
        /// </summary>
        /// <value>The width. The default is NaN (use image width).</value>
        public new PlotLength Width
        {
            get => (PlotLength)this.GetValue(WidthProperty);
            set => this.SetValue(WidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the image.
        /// </summary>
        /// <value>The height. The default is NaN (use image height).</value>
        public new PlotLength Height
        {
            get => (PlotLength)this.GetValue(HeightProperty);
            set => this.SetValue(HeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the opacity of the image (0-1).
        /// </summary>
        /// <value>The opacity. The default is <c>1.0</c>.</value>
        public new double Opacity
        {
            get => (double)this.GetValue(OpacityProperty);
            set => this.SetValue(OpacityProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to apply smooth interpolation.
        /// </summary>
        /// <value><c>true</c> for smooth interpolation; <c>false</c> for nearest neighbor. The default is <c>true</c>.</value>
        public bool Interpolate
        {
            get => (bool)this.GetValue(InterpolateProperty);
            set => this.SetValue(InterpolateProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the image relative to the position.
        /// </summary>
        /// <value>The horizontal alignment. The default is <see cref="OxyPlot.HorizontalAlignment.Center"/>.</value>
        public new OxyPlot.HorizontalAlignment HorizontalAlignment
        {
            get => (OxyPlot.HorizontalAlignment)this.GetValue(HorizontalAlignmentProperty);
            set => this.SetValue(HorizontalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the image relative to the position.
        /// </summary>
        /// <value>The vertical alignment. The default is <see cref="OxyPlot.VerticalAlignment.Middle"/>.</value>
        public new OxyPlot.VerticalAlignment VerticalAlignment
        {
            get => (OxyPlot.VerticalAlignment)this.GetValue(VerticalAlignmentProperty);
            set => this.SetValue(VerticalAlignmentProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.ImageAnnotation"/> instance.</returns>
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

            if (this.InternalAnnotation is Annotations.ImageAnnotation a)
            {
                a.ImageSource = this.ImageSource;
                a.X = this.X;
                a.Y = this.Y;
                a.OffsetX = this.OffsetX;
                a.OffsetY = this.OffsetY;
                a.Width = this.Width;
                a.Height = this.Height;
                a.Opacity = this.Opacity;
                a.Interpolate = this.Interpolate;
                a.HorizontalAlignment = this.HorizontalAlignment;
                a.VerticalAlignment = this.VerticalAlignment;
            }
        }
    }
}
