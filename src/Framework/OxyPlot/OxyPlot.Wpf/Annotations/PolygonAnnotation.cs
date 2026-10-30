// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PolygonAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.PolygonAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using Avalonia;
    using Avalonia.Layout;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.PolygonAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A polygon annotation displays a closed polygon shape defined by a list of points.
    /// The polygon can be filled and stroked, and can include text at the centroid.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the defaults WPF
    /// overrode for this class are overridden in the same way, and the base class's change callbacks still
    /// run too, as in WPF; WPF's text alignments are Avalonia's <c>HorizontalAlignment</c> and
    /// <c>VerticalAlignment</c>, with the same names.
    /// </remarks>
    public class PolygonAnnotation : ShapeAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="LineJoin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineJoin> LineJoinProperty = AvaloniaProperty.Register<PolygonAnnotation, LineJoin>(
            nameof(LineJoin),
            LineJoin.Miter);

        /// <summary>
        /// Identifies the <see cref="LineStyle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<LineStyle> LineStyleProperty = AvaloniaProperty.Register<PolygonAnnotation, LineStyle>(
            nameof(LineStyle),
            LineStyle.Solid);

        /// <summary>
        /// Identifies the <see cref="MinimumSegmentLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> MinimumSegmentLengthProperty = AvaloniaProperty.Register<PolygonAnnotation, double>(
            nameof(MinimumSegmentLength),
            2.0);

        /// <summary>
        /// Initializes static members of the <see cref="PolygonAnnotation"/> class.
        /// </summary>
        static PolygonAnnotation()
        {
            LineJoinProperty.Changed.AddClassHandler<PolygonAnnotation>(AppearanceChanged);
            LineStyleProperty.Changed.AddClassHandler<PolygonAnnotation>(AppearanceChanged);
            MinimumSegmentLengthProperty.Changed.AddClassHandler<PolygonAnnotation>(AppearanceChanged);
            // Override TextVerticalAlignment default to Top so text appears below the polygon
            TextVerticalAlignmentProperty.OverrideDefaultValue<PolygonAnnotation>(VerticalAlignment.Top);
            TextVerticalAlignmentProperty.Changed.AddClassHandler<PolygonAnnotation>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonAnnotation"/> class.
        /// </summary>
        public PolygonAnnotation()
        {
            this.InternalAnnotation = new Annotations.PolygonAnnotation();
        }

        /// <summary>
        /// Gets or sets the points that define the polygon vertices.
        /// </summary>
        /// <value>The list of data points.</value>
        public IList<DataPoint> Points
        {
            get => ((Annotations.PolygonAnnotation)this.InternalAnnotation).Points;
            set
            {
                var annotation = (Annotations.PolygonAnnotation)this.InternalAnnotation;
                annotation.Points.Clear();
                if (value != null)
                {
                    annotation.Points.AddRange(value);
                }
            }
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
        /// <value>The line style. The default is <see cref="LineStyle.Solid"/>.</value>
        public LineStyle LineStyle
        {
            get => (LineStyle)this.GetValue(LineStyleProperty);
            set => this.SetValue(LineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum segment length.
        /// </summary>
        /// <value>The minimum segment length. The default is <c>2.0</c>.</value>
        /// <remarks>
        /// Increasing this value will increase performance but make the polygon less accurate.
        /// </remarks>
        public double MinimumSegmentLength
        {
            get => (double)this.GetValue(MinimumSegmentLengthProperty);
            set => this.SetValue(MinimumSegmentLengthProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.PolygonAnnotation"/> instance.</returns>
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

            if (this.InternalAnnotation is Annotations.PolygonAnnotation a)
            {
                a.LineJoin = this.LineJoin;
                a.LineStyle = this.LineStyle;
                a.MinimumSegmentLength = this.MinimumSegmentLength;
            }
        }
    }
}
