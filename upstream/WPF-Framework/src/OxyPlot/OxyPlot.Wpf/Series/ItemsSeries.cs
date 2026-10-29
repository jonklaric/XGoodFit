// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItemsSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for series that can contain items from an ItemsSource.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    /// <summary>
    /// Provides an abstract base class for series that can contain items from an ItemsSource.
    /// </summary>
    /// <remarks>
    /// This class serves as a bridge between the WPF ItemsControl pattern and OxyPlot's
    /// ItemsSeries. It inherits directly from <see cref="Series"/> which already extends
    /// <see cref="System.Windows.Controls.ItemsControl"/>.
    /// </remarks>
    public abstract class ItemsSeries : Series
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemsSeries"/> class.
        /// </summary>
        protected ItemsSeries()
        {
        }

        /// <summary>
        /// Synchronizes the wrapper properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="s">The OxyPlot series to synchronize properties to.</param>
        /// <remarks>
        /// This method syncs the ItemsSource from the WPF control to the OxyPlot series.
        /// </remarks>
        protected override void SynchronizeProperties(OxyPlot.Series.Series s)
        {
            base.SynchronizeProperties(s);

            if (s is OxyPlot.Series.ItemsSeries itemsSeries)
            {
                itemsSeries.ItemsSource = this.ItemsSource;
            }
        }
    }
}
