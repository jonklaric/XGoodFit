namespace NumericControls.Distributions.Univariate
{
    /// <summary>
    /// Represents a summary statistic for comparing distribution and data values.
    /// Used in the Distribution Selector control to display statistical metrics.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class holds a single statistical measure (e.g., mean, standard deviation, percentile)
    /// with both the theoretical distribution value and the empirical data value for comparison.
    /// </para>
    /// <para>
    /// The Distribution Selector control uses a collection of <see cref="SummaryStatistic"/> objects
    /// to display a table comparing the fitted distribution's statistics against the input data's statistics.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a summary statistic for the mean
    /// var meanStat = new SummaryStatistic("Mean", "100.0000", "98.5432");
    ///
    /// // Create statistics for percentiles
    /// var p5Stat = new SummaryStatistic("5%", "75.3288", "76.1234");
    /// var p95Stat = new SummaryStatistic("95%", "124.6712", "122.8765");
    /// </code>
    /// </example>
    public class SummaryStatistic
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SummaryStatistic"/> class.
        /// </summary>
        /// <param name="statName">The name of the statistic (e.g., "Mean", "Std Dev", "5%").</param>
        /// <param name="distValue">The formatted string value from the distribution.</param>
        /// <param name="dataValue">The formatted string value from the sample data.</param>
        public SummaryStatistic(string statName, string distValue, string dataValue)
        {
            StatName = statName;
            DistStat = distValue;
            DataStat = dataValue;
        }

        /// <summary>
        /// Gets the name of the summary statistic.
        /// </summary>
        /// <value>
        /// The statistic's display name (e.g., "Minimum", "Maximum", "Mean", "Mode",
        /// "Std Dev", "Skewness", "Kurtosis", "5%", "25%", "50%", "75%", "95%",
        /// "RMSE", "Chi-Squared", "K-S").
        /// </value>
        public string StatName { get; private set; }

        /// <summary>
        /// Gets or sets the distribution's value for this statistic.
        /// </summary>
        /// <value>
        /// A culture-formatted string representing the theoretical value from the
        /// selected probability distribution.
        /// </value>
        /// <remarks>
        /// This value is calculated from the currently selected distribution's parameters.
        /// For example, for a Normal(100, 15) distribution, the "Mean" would be "100.0000".
        /// </remarks>
        public string DistStat { get; set; }

        /// <summary>
        /// Gets or sets the sample data's value for this statistic.
        /// </summary>
        /// <value>
        /// A culture-formatted string representing the empirical value calculated from
        /// the input sample data, or " - " if no sample data is available.
        /// </value>
        /// <remarks>
        /// This value is calculated from the sample data provided to the Distribution Selector.
        /// It allows users to compare how well the selected distribution fits the observed data.
        /// </remarks>
        public string DataStat { get; set; }
    }
}
