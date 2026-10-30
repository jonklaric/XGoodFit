// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PathAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an abstract base class for path-based annotations.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Layout;
    using Avalonia.Media;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents an abstract base class for path-based annotations.
    /// </summary>
    /// <remarks>
    /// This class provides common properties for annotations that are rendered
    /// as paths or lines, including color, stroke, line style, and text positioning.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public abstract class PathAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<PathAnnotation, Color>(
            nameof(Color),
            Colors.Blue);

        /// <summary>
        /// Identifies the <see cref="ClipByXAxis"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ClipByXAxisProperty = AvaloniaProperty.Register<PathAnnotation, bool>(
            nameof(ClipByXAxis),
            true);

        /// <summary>
        /// Identifies the <see cref="ClipByYAxis"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> ClipByYAxisProperty = AvaloniaProperty.Register<PathAnnotation, bool>(
            nameof(ClipByYAxis),
            true);

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineJoin> LineJoinProperty = AvaloniaProperty.Register<PathAnnotation, LineJoin>(
            nameof(LineJoin),
            LineJoin.Miter);

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyleProperty = AvaloniaProperty.Register<PathAnnotation, LineStyle>(
            nameof(LineStyle),
            LineStyle.Dash);

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<PathAnnotation, double>(
            nameof(StrokeThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TextMarginProperty = AvaloniaProperty.Register<PathAnnotation, double>(
            nameof(TextMargin),
            12.0);

        /// <summary>
        /// Identifies the <see cref="TextOrientation"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<AnnotationTextOrientation> TextOrientationProperty = AvaloniaProperty.Register<PathAnnotation, AnnotationTextOrientation>(
            nameof(TextOrientation),
            AnnotationTextOrientation.AlongLine);

        /// <summary>
        /// Identifies the <see cref="TextLinePosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TextLinePositionProperty = AvaloniaProperty.Register<PathAnnotation, double>(
            nameof(TextLinePosition),
            1.0);

        /// <summary>
        /// Identifies the <see cref="ClipText"/> dependency property.
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility. It may not have any effect in the current version.
        /// </remarks>
        public static readonly StyledProperty<bool> ClipTextProperty = AvaloniaProperty.Register<PathAnnotation, bool>(
            nameof(ClipText),
            true);

        /// <summary>
        /// Initializes static members of the <see cref="PathAnnotation"/> class.
        /// </summary>
        static PathAnnotation()
        {
            ColorProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            ClipByXAxisProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            ClipByYAxisProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            LineJoinProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            LineStyleProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            StrokeThicknessProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            TextMarginProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            TextOrientationProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            TextLinePositionProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            ClipTextProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
            // Override TextVerticalAlignment default to Top so text appears below the line
            TextVerticalAlignmentProperty.OverrideDefaultValue<PathAnnotation>(VerticalAlignment.Top);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<PathAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip by the X axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by X axis; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool ClipByXAxis
        {
            get => (bool)this.GetValue(ClipByXAxisProperty);
            set => this.SetValue(ClipByXAxisProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip by the Y axis range.
        /// </summary>
        /// <value><c>true</c> if clipping by Y axis; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        public bool ClipByYAxis
        {
            get => (bool)this.GetValue(ClipByYAxisProperty);
            set => this.SetValue(ClipByYAxisProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of the path.
        /// </summary>
        /// <value>The color. The default is <see cref="Colors.Blue"/>.</value>
        public Color Color
        {
            get => (Color)this.GetValue(ColorProperty);
            set => this.SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the line join style.
        /// </summary>
        /// <value>The line join. The default is <see cref="LineJoin.Miter"/>.</value>
        public LineJoin LineJoin
        {
            get => (LineJoin)this.GetValue(LineJoinProperty);
            set => this.SetValue(LineJoinProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style.
        /// </summary>
        /// <value>The line style. The default is <see cref="LineStyle.Dash"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke thickness.
        /// </summary>
        /// <value>The stroke thickness. The default is <c>1.0</c>.</value>
        public double StrokeThickness
        {
            get => (double)this.GetValue(StrokeThicknessProperty);
            set => this.SetValue(StrokeThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the text margin along the line.
        /// </summary>
        /// <value>The text margin in pixels. The default is <c>12.0</c>.</value>
        public double TextMargin
        {
            get => (double)this.GetValue(TextMarginProperty);
            set => this.SetValue(TextMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the text orientation relative to the line.
        /// </summary>
        /// <value>The text orientation. The default is <see cref="AnnotationTextOrientation.AlongLine"/>.</value>
        public AnnotationTextOrientation TextOrientation
        {
            get => (AnnotationTextOrientation)this.GetValue(TextOrientationProperty);
            set => this.SetValue(TextOrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the relative position of text along the line.
        /// </summary>
        /// <value>The text line position (0-1). The default is <c>1.0</c>.</value>
        /// <remarks>
        /// Positions less than 0.25 are left-aligned at the start.
        /// Positions greater than 0.75 are right-aligned at the end.
        /// Other positions are center-aligned at the specified position.
        /// </remarks>
        public double TextLinePosition
        {
            get => (double)this.GetValue(TextLinePositionProperty);
            set => this.SetValue(TextLinePositionProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the annotation text.
        /// </summary>
        /// <value><c>true</c> if text should be clipped; otherwise, <c>false</c>. The default is <c>true</c>.</value>
        /// <remarks>
        /// This property is provided for backward compatibility. It may not have any effect in the current version.
        /// </remarks>
        public bool ClipText
        {
            get => (bool)this.GetValue(ClipTextProperty);
            set => this.SetValue(ClipTextProperty, value);
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

            if (this.InternalAnnotation is Annotations.PathAnnotation a)
            {
                a.Color = this.Color.ToOxyColor();
                a.ClipByXAxis = this.ClipByXAxis;
                a.ClipByYAxis = this.ClipByYAxis;
                a.StrokeThickness = this.StrokeThickness;
                a.LineStyle = this.LineStyle;
                a.LineJoin = this.LineJoin;
                a.TextLinePosition = this.TextLinePosition;
                a.TextOrientation = this.TextOrientation;
                a.TextMargin = this.TextMargin;
            }
        }
    }
}
