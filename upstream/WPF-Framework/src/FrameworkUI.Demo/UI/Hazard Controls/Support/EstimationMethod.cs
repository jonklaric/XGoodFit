using Numerics.Distributions;

namespace FrameworkUI.Demo
{
    /// <summary>
    /// Represents a parameter estimation method option for display in a combobox.
    /// </summary>
    public class EstimationMethod
    {
        /// <summary>
        /// Gets or sets the display name shown in the combobox.
        /// </summary>
        /// <value>
        /// The text representation of the estimation method (e.g., "Product Moments", "Linear Moments").
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the parameter estimation method.
        /// </summary>
        /// <value>
        /// The <see cref="ParameterEstimationMethod"/> enumeration value.
        /// </value>
        public ParameterEstimationMethod Method { get; set; }

        /// <summary>
        /// Gets the tooltip text describing the estimation method.
        /// </summary>
        /// <value>
        /// A description of the estimation method, or an empty string if not specified.
        /// </value>
        public string ToolTip { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EstimationMethod"/> class.
        /// </summary>
        /// <param name="displayName">Text to be shown in the combobox.</param>
        /// <param name="method">The parameter estimation method.</param>
        /// <param name="tooltip">Optional tooltip text describing the method.</param>
        public EstimationMethod(string displayName, ParameterEstimationMethod method, string tooltip = "")
        {
            DisplayName = displayName;
            Method = method;
            ToolTip = tooltip;
        }
    }
}
