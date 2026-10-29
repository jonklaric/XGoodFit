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
    using System.Windows;
    using System.Windows.Media;

    /// <summary>
    /// Represents an abstract base class for annotations that contain text.
    /// </summary>
    /// <remarks>
    /// This class provides common text properties such as content, color,
    /// positioning, rotation, and font settings for text-based annotations.
    /// </remarks>
    public abstract class TextualAnnotation : Annotation
    {
        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(TextualAnnotation),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextPositionProperty =
            DependencyProperty.Register(
                nameof(TextPosition),
                typeof(DataPoint),
                typeof(TextualAnnotation),
                new PropertyMetadata(DataPoint.Undefined, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextRotation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextRotationProperty =
            DependencyProperty.Register(
                nameof(TextRotation),
                typeof(double),
                typeof(TextualAnnotation),
                new PropertyMetadata(0.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextColorProperty =
            DependencyProperty.Register(
                nameof(TextColor),
                typeof(Color),
                typeof(TextualAnnotation),
                new PropertyMetadata(Colors.Black, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextHorizontalAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextHorizontalAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextHorizontalAlignment),
                typeof(HorizontalAlignment),
                typeof(TextualAnnotation),
                new UIPropertyMetadata(HorizontalAlignment.Center, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextVerticalAlignment"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextVerticalAlignmentProperty =
            DependencyProperty.Register(
                nameof(TextVerticalAlignment),
                typeof(VerticalAlignment),
                typeof(TextualAnnotation),
                new UIPropertyMetadata(VerticalAlignment.Center, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontFamilyProperty =
            DependencyProperty.Register(
                nameof(FontFamily),
                typeof(FontFamily),
                typeof(TextualAnnotation),
                new UIPropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontWeightProperty =
            DependencyProperty.Register(
                nameof(FontWeight),
                typeof(FontWeight),
                typeof(TextualAnnotation),
                new UIPropertyMetadata(FontWeights.Normal, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FontSizeProperty =
            DependencyProperty.Register(
                nameof(FontSize),
                typeof(double),
                typeof(TextualAnnotation),
                new UIPropertyMetadata(double.NaN, AppearanceChanged));

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
                a.Font = this.FontFamily?.Source;
                a.FontSize = this.FontSize;
                a.FontWeight = this.FontWeight.ToOpenTypeWeight();
            }
        }
    }
}
