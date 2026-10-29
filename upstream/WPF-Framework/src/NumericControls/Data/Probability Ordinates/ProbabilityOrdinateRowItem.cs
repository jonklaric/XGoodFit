using GenericControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NumericControls
{
    /// <summary>
    /// Represents a data grid row item for probability ordinate values, used for specifying
    /// probability levels at which to evaluate probability distributions.
    /// These ordinates are commonly used in frequency analysis and risk assessment applications.
    /// </summary>
    public class ProbabilityOrdinateRowItem : DataGridRowItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilityOrdinateRowItem"/> class with default values.
        /// </summary>
        public ProbabilityOrdinateRowItem() : base(null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilityOrdinateRowItem"/> class with the specified probability value.
        /// </summary>
        /// <param name="observableCollection">The observable collection that contains this row item.</param>
        /// <param name="probability">The exceedance probability value, ranging from 0 to 1.</param>
        public ProbabilityOrdinateRowItem(ObservableCollection<Object> observableCollection, double probability) : base(observableCollection)
        {
            _probability = probability;
        }

        /// <summary>
        /// The backing field for the Probability property.
        /// </summary>
        private double _probability;

        /// <summary>
        /// Gets or sets the probability value.
        /// Must be between 0 and 1.
        /// </summary>
        public double Probability
        {
            get { return _probability; }
            set
            {
                if (_probability != value)
                {
                    _probability = value;
                    NotifyPropertyChanged(nameof(Probability));
                }
            }
        }

        /// <summary>
        /// Adds validation rules for the probability ordinate row item properties.
        /// Validates that probability values are in ascending order and within the valid range of 0 to 1.
        /// </summary>
        public override void AddValidationRules()
        {
            AddRule(nameof(Probability), () => OrderRule<double, ProbabilityOrdinateRowItem>((x) => x.Probability, nameof(Probability), true, false), "The probability values must be in ascending order.");
            AddRule(nameof(Probability), () => Probability < 0d, "The probability value must be between 0 and 1.");
            AddRule(nameof(Probability), () => Probability > 1d, "The probability value must be between 0 and 1.");
        }

        /// <summary>
        /// Determines whether the specified property should be displayed in the data grid.
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>Always returns true, indicating all properties are displayable.</returns>
        public override bool IsGridDisplayable(string propertyName)
        {
            return true;
        }

        /// <summary>
        /// Gets the display name for the specified property to be shown in the data grid header.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The display name for the property, or null if not recognized.</returns>
        public override string PropertyDisplayName(string propertyName)
        {
            if (propertyName == nameof(Probability))
            {
                return "Probability Ordinates";
            }
            else
            {
                return null;
            }
        }
    }
}
