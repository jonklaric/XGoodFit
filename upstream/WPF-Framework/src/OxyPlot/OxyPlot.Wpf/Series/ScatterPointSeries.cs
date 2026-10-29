// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ScatterPointSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an alias for ScatterSeries for backward compatibility.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Represents an alias for <see cref="ScatterSeries"/> for backward compatibility.
    /// </summary>
    /// <remarks>
    /// This class is provided for backward compatibility with code that references ScatterPointSeries.
    /// New code should use <see cref="ScatterSeries"/> directly.
    /// </remarks>
    public class ScatterPointSeries : ScatterSeries
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScatterPointSeries"/> class.
        /// </summary>
        public ScatterPointSeries()
        {
        }
    }
}
