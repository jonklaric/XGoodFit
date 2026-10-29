// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PolylineAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.PolylineAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.PolylineAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A polyline annotation displays an open path defined by a list of points.
    /// The polyline can be styled with various line properties and can include text.
    /// </remarks>
    public class PolylineAnnotation : PathAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="InterpolationAlgorithm"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InterpolationAlgorithmProperty =
            DependencyProperty.Register(
                nameof(InterpolationAlgorithm),
                typeof(IInterpolationAlgorithm),
                typeof(PolylineAnnotation),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumSegmentLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumSegmentLengthProperty =
            DependencyProperty.Register(
                nameof(MinimumSegmentLength),
                typeof(double),
                typeof(PolylineAnnotation),
                new PropertyMetadata(2.0, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="PolylineAnnotation"/> class.
        /// </summary>
        public PolylineAnnotation()
        {
            this.InternalAnnotation = new Annotations.PolylineAnnotation();
        }

        /// <summary>
        /// Gets or sets the points that define the polyline path.
        /// </summary>
        /// <value>The list of data points.</value>
        public IList<DataPoint> Points
        {
            get => ((Annotations.PolylineAnnotation)this.InternalAnnotation).Points;
            set
            {
                var annotation = (Annotations.PolylineAnnotation)this.InternalAnnotation;
                annotation.Points.Clear();
                if (value != null)
                {
                    annotation.Points.AddRange(value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the interpolation algorithm used to create smooth curves.
        /// </summary>
        /// <value>The interpolation algorithm. The default is <c>null</c> (no interpolation).</value>
        public IInterpolationAlgorithm InterpolationAlgorithm
        {
            get => (IInterpolationAlgorithm)this.GetValue(InterpolationAlgorithmProperty);
            set => this.SetValue(InterpolationAlgorithmProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum segment length for resampling points.
        /// </summary>
        /// <value>The minimum segment length. The default is <c>2.0</c>.</value>
        public double MinimumSegmentLength
        {
            get => (double)this.GetValue(MinimumSegmentLengthProperty);
            set => this.SetValue(MinimumSegmentLengthProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.PolylineAnnotation"/> instance.</returns>
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

            if (this.InternalAnnotation is Annotations.PolylineAnnotation a)
            {
                a.InterpolationAlgorithm = this.InterpolationAlgorithm;
            }
        }
    }
}
