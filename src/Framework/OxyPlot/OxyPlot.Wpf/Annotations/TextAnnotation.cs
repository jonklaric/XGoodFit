// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.TextAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Layout;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.TextAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A text annotation displays text at a specific position on the plot,
    /// optionally with a background box and border.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public class TextAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Background"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> BackgroundProperty = AvaloniaProperty.Register<TextAnnotation, Color>(
            nameof(Background),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="Offset"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Vector> OffsetProperty = AvaloniaProperty.Register<TextAnnotation, Vector>(
            nameof(Offset),
            default(Vector));

        /// <summary>
        /// Identifies the <see cref="Padding"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PaddingProperty = AvaloniaProperty.Register<TextAnnotation, Thickness>(
            nameof(Padding),
            new Thickness(4));

        /// <summary>
        /// Identifies the <see cref="Stroke"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> StrokeProperty = AvaloniaProperty.Register<TextAnnotation, Color>(
            nameof(Stroke),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<TextAnnotation, double>(
            nameof(StrokeThickness),
            1.0);

        /// <summary>
        /// Initializes static members of the <see cref="TextAnnotation"/> class.
        /// </summary>
        static TextAnnotation()
        {
            BackgroundProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            OffsetProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            PaddingProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            StrokeProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            TextHorizontalAlignmentProperty.OverrideDefaultValue<TextAnnotation>(HorizontalAlignment.Center);
            TextHorizontalAlignmentProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
            TextVerticalAlignmentProperty.OverrideDefaultValue<TextAnnotation>(VerticalAlignment.Bottom);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<TextAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextAnnotation"/> class.
        /// </summary>
        public TextAnnotation()
        {
            this.InternalAnnotation = new Annotations.TextAnnotation();
        }

        /// <summary>
        /// Gets or sets the fill color of the background rectangle.
        /// </summary>
        /// <value>The background color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color Background
        {
            get => (Color)this.GetValue(BackgroundProperty);
            set => this.SetValue(BackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the position offset in screen coordinates.
        /// </summary>
        /// <value>The offset vector. The default is <c>(0, 0)</c>.</value>
        public Vector Offset
        {
            get => (Vector)this.GetValue(OffsetProperty);
            set => this.SetValue(OffsetProperty, value);
        }

        /// <summary>
        /// Gets or sets the padding of the background rectangle.
        /// </summary>
        /// <value>The padding. The default is <c>4</c> on all sides.</value>
        public Thickness Padding
        {
            get => (Thickness)this.GetValue(PaddingProperty);
            set => this.SetValue(PaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke color of the background rectangle border.
        /// </summary>
        /// <value>The stroke color. The default is <see cref="Colors.Black"/>.</value>
        public Color Stroke
        {
            get => (Color)this.GetValue(StrokeProperty);
            set => this.SetValue(StrokeProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness of the background rectangle border.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
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

            if (this.InternalAnnotation is Annotations.TextAnnotation a)
            {
                a.TextHorizontalAlignment = this.TextHorizontalAlignment.ToHorizontalAlignment();
                a.Background = this.Background.ToOxyColor();
                a.Offset = this.Offset.ToScreenVector();
                a.TextVerticalAlignment = this.TextVerticalAlignment.ToVerticalAlignment();
                a.Padding = this.Padding.ToOxyThickness();
                a.Stroke = this.Stroke.ToOxyColor();
                a.StrokeThickness = this.StrokeThickness;
            }
        }
    }
}
