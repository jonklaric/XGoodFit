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
    using System.Windows;
    using System.Windows.Media;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents an abstract base class for path-based annotations.
    /// </summary>
    /// <remarks>
    /// This class provides common properties for annotations that are rendered
    /// as paths or lines, including color, stroke, line style, and text positioning.
    /// </remarks>
    public abstract class PathAnnotation : TextualAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register(
                nameof(Color),
                typeof(Color),
                typeof(PathAnnotation),
                new PropertyMetadata(Colors.Blue, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipByXAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClipByXAxisProperty =
            DependencyProperty.Register(
                nameof(ClipByXAxis),
                typeof(bool),
                typeof(PathAnnotation),
                new UIPropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipByYAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClipByYAxisProperty =
            DependencyProperty.Register(
                nameof(ClipByYAxis),
                typeof(bool),
                typeof(PathAnnotation),
                new UIPropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineJoinProperty =
            DependencyProperty.Register(
                nameof(LineJoin),
                typeof(LineJoin),
                typeof(PathAnnotation),
                new UIPropertyMetadata(LineJoin.Miter, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LineStyleProperty =
            DependencyProperty.Register(
                nameof(LineStyle),
                typeof(LineStyle),
                typeof(PathAnnotation),
                new PropertyMetadata(LineStyle.Dash, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="StrokeThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(PathAnnotation),
                new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextMargin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMarginProperty =
            DependencyProperty.Register(
                nameof(TextMargin),
                typeof(double),
                typeof(PathAnnotation),
                new UIPropertyMetadata(12.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextOrientation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextOrientationProperty =
            DependencyProperty.Register(
                nameof(TextOrientation),
                typeof(AnnotationTextOrientation),
                typeof(PathAnnotation),
                new UIPropertyMetadata(AnnotationTextOrientation.AlongLine, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TextLinePosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextLinePositionProperty =
            DependencyProperty.Register(
                nameof(TextLinePosition),
                typeof(double),
                typeof(PathAnnotation),
                new UIPropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="ClipText"/> dependency property.
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility. It may not have any effect in the current version.
        /// </remarks>
        public static readonly DependencyProperty ClipTextProperty =
            DependencyProperty.Register(
                nameof(ClipText),
                typeof(bool),
                typeof(PathAnnotation),
                new UIPropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Initializes static members of the <see cref="PathAnnotation"/> class.
        /// </summary>
        static PathAnnotation()
        {
            // Override TextVerticalAlignment default to Top so text appears below the line
            TextVerticalAlignmentProperty.OverrideMetadata(typeof(PathAnnotation), new FrameworkPropertyMetadata(VerticalAlignment.Top, AppearanceChanged));
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
