using OxyPlot;

namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// Represents an area defined by two data points, used for plotting area series.
    /// </summary>
    public class AreaPoint
    {
        /// <summary>
        /// Gets or sets the X coordinate of the first point.
        /// </summary>
        public double X1 { get; set; }

        /// <summary>
        /// Gets or sets the X coordinate of the second point.
        /// </summary>
        public double X2 { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the first point.
        /// </summary>
        public double Y1 { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the second point.
        /// </summary>
        public double Y2 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AreaPoint"/> class.
        /// </summary>
        /// <param name="p1">The first data point defining the area boundary.</param>
        /// <param name="p2">The second data point defining the area boundary.</param>
        public AreaPoint(DataPoint p1, DataPoint p2)
        {
            X1 = p1.X;
            X2 = p2.X;
            Y1 = p1.Y;
            Y2 = p2.Y;
        }
    }
}
