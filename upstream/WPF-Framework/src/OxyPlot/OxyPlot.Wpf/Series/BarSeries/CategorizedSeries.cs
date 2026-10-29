// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategorizedSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents an abstract base class for series that use categorical axes.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Represents an abstract base class for series that use categorical axes.
    /// </summary>
    /// <remarks>
    /// This class serves as a base for series types like bar and column series
    /// that display data in discrete categories rather than continuous values.
    /// </remarks>
    public abstract class CategorizedSeries : XYAxisSeries
    {
    }
}
