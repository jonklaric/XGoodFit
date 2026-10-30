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
    using Avalonia;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.ImageAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// An image annotation displays an image at a specified position on the plot.
    /// The position, size, and alignment can all be customized.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class ImageAnnotation : Annotation
    {
        /// <summary>
        /// Identifies the <see cref="ImageSource"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyImage> ImageSourceProperty = AvaloniaProperty.Register<ImageAnnotation, OxyImage>(
            nameof(ImageSource),
            null);

        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<PlotLength> XProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(X),
            new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<PlotLength> YProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(Y),
            new PlotLength(0.5, PlotLengthUnit.RelativeToPlotArea));

        /// <summary>
        /// Identifies the <see cref="OffsetX"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<PlotLength> OffsetXProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(OffsetX),
            new PlotLength(0, PlotLengthUnit.ScreenUnits));

        /// <summary>
        /// Identifies the <see cref="OffsetY"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<PlotLength> OffsetYProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(OffsetY),
            new PlotLength(0, PlotLengthUnit.ScreenUnits));

        /// <summary>
        /// Identifies the <see cref="Width"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<PlotLength> WidthProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(Width),
            new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits));

        /// <summary>
        /// Identifies the <see cref="Height"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<PlotLength> HeightProperty = AvaloniaProperty.Register<ImageAnnotation, PlotLength>(
            nameof(Height),
            new PlotLength(double.NaN, PlotLengthUnit.ScreenUnits));

        /// <summary>
        /// Identifies the <see cref="Opacity"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<double> OpacityProperty = AvaloniaProperty.Register<ImageAnnotation, double>(
            nameof(Opacity),
            1.0);

        /// <summary>
        /// Identifies the <see cref="Interpolate"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> InterpolateProperty = AvaloniaProperty.Register<ImageAnnotation, bool>(
            nameof(Interpolate),
            true);

        /// <summary>
        /// Identifies the <see cref="HorizontalAlignment"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<OxyPlot.HorizontalAlignment> HorizontalAlignmentProperty = AvaloniaProperty.Register<ImageAnnotation, OxyPlot.HorizontalAlignment>(
            nameof(HorizontalAlignment),
            OxyPlot.HorizontalAlignment.Center);

        /// <summary>
        /// Identifies the <see cref="VerticalAlignment"/> dependency property.
        /// </summary>
        public new static readonly StyledProperty<OxyPlot.VerticalAlignment> VerticalAlignmentProperty = AvaloniaProperty.Register<ImageAnnotation, OxyPlot.VerticalAlignment>(
            nameof(VerticalAlignment),
            OxyPlot.VerticalAlignment.Middle);

        /// <summary>
        /// Initializes static members of the <see cref="ImageAnnotation" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static ImageAnnotation()
        {
            WidthProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            HeightProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            OpacityProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            HorizontalAlignmentProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            VerticalAlignmentProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            ImageSourceProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            XProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            YProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            OffsetXProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            OffsetYProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
            InterpolateProperty.Changed.AddClassHandler<ImageAnnotation>(AppearanceChanged);
        }

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
