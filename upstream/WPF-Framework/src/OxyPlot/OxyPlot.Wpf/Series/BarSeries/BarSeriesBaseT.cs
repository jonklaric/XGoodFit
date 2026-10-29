// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BarSeriesBaseT.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a generic base class for bar series types.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a generic base class for bar series types.
    /// </summary>
    /// <typeparam name="T">The type of the bar items.</typeparam>
    /// <remarks>
    /// This generic class handles synchronization of manually added bar items
    /// when no ItemsSource is specified.
    /// </remarks>
    public class BarSeriesBase<T> : BarSeriesBase
        where T : OxyPlot.Series.BarItemBase, new()
    {
        /// <summary>
        /// The items collection for manually added items.
        /// </summary>
        private readonly List<T> items = new List<T>();

        /// <summary>
        /// Gets the items collection.
        /// </summary>
        /// <value>A list of items that can be manually added when not using ItemsSource.</value>
        /// <remarks>
        /// This property shadows <see cref="System.Windows.Controls.ItemsControl.Items"/> to provide
        /// a strongly-typed collection for bar series items.
        /// </remarks>
        public new List<T> Items => this.items;

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal series to synchronize.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (this.ItemsSource == null && series is OxyPlot.Series.BarSeriesBase<T> s)
            {
                s.Items.Clear();
                foreach (var item in this.items)
                {
                    s.Items.Add(item);
                }
            }
        }
    }
}
