
namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// Confidence interval item to fill the combobox.
    /// </summary>
    public class ConfidenceIntervalItem
    {
        /// <summary>
        /// Gets or sets the display name shown in the combobox.
        /// </summary>
        /// <value>
        /// The text representation of the confidence interval (e.g., "90%", "95%").
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the confidence interval width value.
        /// </summary>
        /// <value>
        /// The numeric confidence interval width as a decimal (e.g., 0.9 for 90%).
        /// </value>
        public double Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfidenceIntervalItem"/> class.
        /// </summary>
        /// <param name="displayName">Text to be shown in the combobox.</param>
        /// <param name="value">Confidence interval width value.</param>
        public ConfidenceIntervalItem(string displayName, double value)
        {
            DisplayName = displayName;
            Value = value;
        }
    }
}
