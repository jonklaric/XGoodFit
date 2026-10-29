using System.Collections.ObjectModel;
using GenericControls;

namespace NumericControls
{
    /// <summary>
    /// Represents a validating row item for univariate distribution data entry in a data grid,
    /// providing validation for X values and probabilities.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UnivariateDistributionValidatingRow : DataGridRowItem
    {

        private double _xMax;
        private double _xMin;
        private double _x;
        private double _p;

        /// <summary>
        /// Gets or sets the X value (random variable value).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
                NotifyPropertyChanged(nameof(X));
            }
        }

        /// <summary>
        /// Gets or sets the probability value associated with the X value.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public double P
        {
            get { return _p; }

            set
            {
                _p = value;
                NotifyPropertyChanged(nameof(P));
            }
        }

        /// <summary>
        /// Gets or sets the minimum allowed X value for validation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public double MinX
        {
            get { return _xMin; }
            set
            {
                if (_xMin != value)
                {
                    _xMin = value;
                    NotifyPropertyChanged(nameof(MinX));
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed X value for validation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public double MaxX
        {
            get { return _xMax; }
            set
            {
                if (_xMax != value)
                {
                    _xMax = value;
                    NotifyPropertyChanged(nameof(MaxX));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the UnivariateDistributionValidatingRow class with default values.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public UnivariateDistributionValidatingRow() : base(null)
        {
            // 
            _x = 0d;
            _p = 0d;
            _xMin = double.MinValue;
            _xMax = double.MaxValue;
        }

        /// <summary>
        /// Initializes a new instance of the UnivariateDistributionValidatingRow class with specified values and validation constraints.
        /// </summary>
        /// <param name="xValue">The X value (random variable value).</param>
        /// <param name="pValue">The probability value.</param>
        /// <param name="xMin">The minimum allowed X value.</param>
        /// <param name="xMax">The maximum allowed X value.</param>
        /// <param name="theList">The observable collection this row belongs to.</param>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        public UnivariateDistributionValidatingRow(double xValue, double pValue, double xMin, double xMax, ObservableCollection<object> theList) : base(theList)
        {
            // 
            _x = xValue;
            _p = pValue;
            _xMin = xMin;
            _xMax = xMax;
        }

        /// <inheritdoc/>
        public override void AddValidationRules()
        {
            AddRule(nameof(P), () => P > 1d, "Probability must be less than or equal to 1");
            AddRule(nameof(P), () => P < 0d, "Probability must be greater than or equal to 0");
            AddRule(nameof(X), () => X > MaxX, "X must be less than or equal to " + MaxX);
            AddRule(nameof(X), () => X < MinX, "X must be greater than or equal to " + MinX);
            AddRule(nameof(P), () => OrderRule<double, UnivariateDistributionValidatingRow>(o => o.P, nameof(P)), "Probability must be in ascending order");
            AddRule(nameof(X), () => OrderRule<double, UnivariateDistributionValidatingRow>(o => o.X, nameof(X)), "X values must be in ascending order");
        }

        /// <inheritdoc/>
        public override string PropertyDisplayName(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(X): return "X";
                case nameof(P): return "Probability";
                default: return propertyName;
            }
        }

        /// <inheritdoc/>
        public override bool IsGridDisplayable(string propertyName)
        {
            if ((propertyName == nameof(X)) || (propertyName == nameof(P))) return true;
            return false;
        }

    }
}
