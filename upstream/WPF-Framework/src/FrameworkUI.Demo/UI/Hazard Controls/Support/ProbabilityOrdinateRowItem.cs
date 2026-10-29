using System.Collections.ObjectModel;
using GenericControls;

namespace FrameworkUI.Demo.UI
{
    /// <summary>
    /// Probability ordinate row item.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class ProbabilityOrdinateRowItem : DataGridRowItem
    {
        /// <summary>
        /// Construct an empty row item.
        /// </summary>
        public ProbabilityOrdinateRowItem()
            : base(null)
        {
        }

        /// <summary>
        /// Construct a row item.
        /// </summary>
        /// <param name="observableCollection">The observable collection of all row items.</param>
        /// <param name="ordinate">The probability ordinate value.</param>
        public ProbabilityOrdinateRowItem(ObservableCollection<object> observableCollection, double ordinate)
            : base(observableCollection)
        {
            _ordinate = ordinate;
        }

        private double _ordinate;

        /// <summary>
        /// Gets or sets the probability ordinate value.
        /// </summary>
        /// <value>
        /// The exceedance probability value, which must be between 0 and 1.
        /// </value>
        public double Ordinate
        {
            get => _ordinate;
            set
            {
                if (_ordinate != value)
                {
                    _ordinate = value;
                    NotifyPropertyChanged(nameof(Ordinate));
                }
            }
        }

        /// <summary>
        /// Sets the validation rules for this row item.
        /// </summary>
        /// <remarks>
        /// Adds rules to ensure:
        /// <list type="bullet">
        ///     <item><description>Probability values are in ascending order</description></item>
        ///     <item><description>Probability values are between 0 and 1</description></item>
        /// </list>
        /// </remarks>
        public override void AddValidationRules()
        {
            AddRule(nameof(Ordinate), () => OrderRule<double, ProbabilityOrdinateRowItem>(x => x.Ordinate, nameof(ProbabilityOrdinateRowItem.Ordinate), true, false), "The exceedance probability values must be in ascending order.");
            AddRule(nameof(Ordinate), () => Ordinate < 0, "The exceedance probability value must be between 0 and 1.");
            AddRule(nameof(Ordinate), () => Ordinate > 1.0, "The exceedance probability value must be between 0 and 1.");
        }

        /// <summary>
        /// Determines the property display name in the data grid.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The display name for the property, or <c>null</c> if not specified.</returns>
        public override string PropertyDisplayName(string propertyName)
        {
            if (string.Equals(propertyName, nameof(Ordinate)))
            {
                return "Probability Ordinates";
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determines if the property is displayable in the data grid.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns><c>true</c> if the property should be displayed; otherwise, <c>false</c>.</returns>
        public override bool IsGridDisplayable(string propertyName)
        {
            return true;
        }
    }
}
