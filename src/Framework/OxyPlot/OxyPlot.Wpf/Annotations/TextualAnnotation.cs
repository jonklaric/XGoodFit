// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextualAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an abstract base class for annotations that contain text.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Layout;
    using WpfCompat.Media;
    using Avalonia.Media;

    /// <summary>
    /// Represents an abstract base class for annotations that contain text.
    /// </summary>
    /// <remarks>
    /// This class provides common text properties such as content, color,
    /// positioning, rotation, and font settings for text-based annotations.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; WPF's text
    /// alignments are Avalonia's <c>HorizontalAlignment</c> and <c>VerticalAlignment</c>, with the same
    /// names; the font family passed to the OxyPlot annotation is the family's WPF text form (WpfCompat's
    /// <c>FontFamilyConverter</c>), as Avalonia's <c>ToString()</c> gives a resource key for a family with
    /// fallbacks.
    /// </remarks>
    public abstract class TextualAnnotation : Annotation
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<TextualAnnotation, string>(
            nameof(Text),
            null);

        /// <summary>
        /// Identifies the <see cref="TextPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<DataPoint> TextPositionProperty = AvaloniaProperty.Register<TextualAnnotation, DataPoint>(
            nameof(TextPosition),
            DataPoint.Undefined);

        /// <summary>
        /// Identifies the <see cref="TextRotation"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TextRotationProperty = AvaloniaProperty.Register<TextualAnnotation, double>(
            nameof(TextRotation),
            0.0);

        /// <summary>
        /// Identifies the <see cref="TextColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TextColorProperty = AvaloniaProperty.Register<TextualAnnotation, Color>(
            nameof(TextColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="TextHorizontalAlignment"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> TextHorizontalAlignmentProperty = AvaloniaProperty.Register<TextualAnnotation, HorizontalAlignment>(
            nameof(TextHorizontalAlignment),
            HorizontalAlignment.Center);

        /// <summary>
        /// Identifies the <see cref="TextVerticalAlignment"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> TextVerticalAlignmentProperty = AvaloniaProperty.Register<TextualAnnotation, VerticalAlignment>(
            nameof(TextVerticalAlignment),
            VerticalAlignment.Center);

        /// <summary>
        /// Identifies the <see cref="FontFamily"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontFamily> FontFamilyProperty = AvaloniaProperty.Register<TextualAnnotation, FontFamily>(
            nameof(FontFamily),
            null);

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> FontWeightProperty = AvaloniaProperty.Register<TextualAnnotation, FontWeight>(
            nameof(FontWeight),
            FontWeight.Normal);

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> FontSizeProperty = AvaloniaProperty.Register<TextualAnnotation, double>(
            nameof(FontSize),
            double.NaN);

        /// <summary>
        /// Initializes static members of the <see cref="TextualAnnotation" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static TextualAnnotation()
        {
            TextProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            TextPositionProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            TextRotationProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            TextColorProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            TextHorizontalAlignmentProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            FontFamilyProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            FontWeightProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
            FontSizeProperty.Changed.AddClassHandler<TextualAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Gets or sets the text content of the annotation.
        /// </summary>
        /// <value>The text. The default is <c>null</c>.</value>
        public string Text
        {
            get => (string)this.GetValue(TextProperty);
            set => this.SetValue(TextProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <value>The text color. The default is <see cref="Colors.Black"/>.</value>
        public Color TextColor
        {
            get => (Color)this.GetValue(TextColorProperty);
            set => this.SetValue(TextColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the position of the text in data coordinates.
        /// </summary>
        /// <value>The text position. The default is <see cref="DataPoint.Undefined"/>.</value>
        /// <remarks>If undefined, the position is determined by the annotation type.</remarks>
        public DataPoint TextPosition
        {
            get => (DataPoint)this.GetValue(TextPositionProperty);
            set => this.SetValue(TextPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the text.
        /// </summary>
        /// <value>The text horizontal alignment. The default is <see cref="HorizontalAlignment.Center"/>.</value>
        public HorizontalAlignment TextHorizontalAlignment
        {
            get => (HorizontalAlignment)this.GetValue(TextHorizontalAlignmentProperty);
            set => this.SetValue(TextHorizontalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the text.
        /// </summary>
        /// <value>The text vertical alignment. The default is <see cref="VerticalAlignment.Center"/>.</value>
        public VerticalAlignment TextVerticalAlignment
        {
            get => (VerticalAlignment)this.GetValue(TextVerticalAlignmentProperty);
            set => this.SetValue(TextVerticalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the rotation angle of the text in degrees.
        /// </summary>
        /// <value>The rotation angle. The default is <c>0.0</c>.</value>
        public double TextRotation
        {
            get => (double)this.GetValue(TextRotationProperty);
            set => this.SetValue(TextRotationProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family for the text.
        /// </summary>
        /// <value>The font family. The default is <c>null</c> (uses plot default).</value>
        public FontFamily FontFamily
        {
            get => (FontFamily)this.GetValue(FontFamilyProperty);
            set => this.SetValue(FontFamilyProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight for the text.
        /// </summary>
        /// <value>The font weight. The default is <see cref="FontWeights.Normal"/>.</value>
        public FontWeight FontWeight
        {
            get => (FontWeight)this.GetValue(FontWeightProperty);
            set => this.SetValue(FontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the font size for the text.
        /// </summary>
        /// <value>The font size. The default is <see cref="double.NaN"/> (uses plot default).</value>
        public double FontSize
        {
            get => (double)this.GetValue(FontSizeProperty);
            set => this.SetValue(FontSizeProperty, value);
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot annotation.
        /// </summary>
        public override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAnnotation is Annotations.TextualAnnotation a)
            {
                a.TextColor = this.TextColor.ToOxyColor();
                a.Text = this.Text;
                a.TextPosition = this.TextPosition;
                a.TextRotation = this.TextRotation;
                a.TextHorizontalAlignment = this.TextHorizontalAlignment.ToHorizontalAlignment();
                a.TextVerticalAlignment = this.TextVerticalAlignment.ToVerticalAlignment();
                a.Font = this.FontFamily == null ? null : FontFamilyConverter.ToString(this.FontFamily);
                a.FontSize = this.FontSize;
                a.FontWeight = this.FontWeight.ToOpenTypeWeight();
            }
        }
    }
}
