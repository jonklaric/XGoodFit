
namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// Represents a row of data for a parametric distribution frequency curve table.
    /// </summary>
    public class ParametricDistributionCurveRow
    {
        /// <summary>
        /// Gets or sets the Annual Exceedance Probability (AEP).
        /// </summary>
        public double AEP { get; set; }

        /// <summary>
        /// Gets or sets the upper confidence interval bound.
        /// </summary>
        public double Upper { get; set; }

        /// <summary>
        /// Gets or sets the lower confidence interval bound.
        /// </summary>
        public double Lower { get; set; }

        /// <summary>
        /// Gets or sets the predictive (mean) value.
        /// </summary>
        public double Predictive { get; set; }

        /// <summary>
        /// Gets or sets the mode (user-specified) value.
        /// </summary>
        public double Mode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParametricDistributionCurveRow"/> class.
        /// </summary>
        /// <param name="aep">The Annual Exceedance Probability.</param>
        /// <param name="upper">The upper confidence interval bound.</param>
        /// <param name="lower">The lower confidence interval bound.</param>
        /// <param name="predictive">The predictive (mean) value.</param>
        /// <param name="mode">The mode (user-specified) value.</param>
        public ParametricDistributionCurveRow(double aep, double upper, double lower, double predictive, double mode)
        {
            AEP = aep;
            Upper = upper;
            Lower = lower;
            Predictive = predictive;
            Mode = mode;
        }
    }
}
