using System.Collections.ObjectModel;
using GenericControls;
using Numerics;
using Numerics.Data;
using Numerics.Distributions;

namespace NumericControls
{
    /// <summary>
    /// Represents a row item for editing distribution parameters in uncertain ordered paired data,
    /// providing validation and constraints for distribution values in a data grid.
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
    public class DistributionRowItem : DataGridRowItem
    {

        private UnivariateDistributionBase _distribution;
        private double _maxXValue;
        private double _minXValue;
        private double _maxYValue;
        private double _minYValue;
        private bool _isStrictX;
        private bool _isStrictY;
        private SortOrder _xOrder;
        private SortOrder _yOrder;
        private string[] _propertyNames;
        private string[] _propertyDisplayNames;


        /// <summary>
        /// Gets the probability distribution associated with this row.
        /// </summary>
        public UnivariateDistributionBase Distribution
        {
            get { return _distribution; }
        }

        /// <summary>
        /// Gets or sets the maximum allowed X value for validation.
        /// </summary>
        public double MaxXValue
        {
            get { return _maxXValue; }
            set
            {
                if (_maxXValue != value)
                {
                    _maxXValue = value;
                    NotifyPropertyChanged(nameof(MaxXValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum allowed X value for validation.
        /// </summary>
        public double MinXValue
        {
            get { return _minXValue; }
            set
            {
                if (_minXValue != value)
                {
                    _minXValue = value;
                    NotifyPropertyChanged(nameof(MinXValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum allowed Y value for validation.
        /// </summary>
        public double MaxYValue
        {
            get { return _maxYValue; }
            set
            {
                if (_maxYValue != value)
                {
                    _maxYValue = value;
                    NotifyPropertyChanged(nameof(MaxYValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets the minimum allowed Y value for validation.
        /// </summary>
        public double MinYValue
        {
            get { return _minYValue; }
            set
            {
                if (_minYValue != value)
                {
                    _minYValue = value;
                    NotifyPropertyChanged(nameof(MinYValue));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether X values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictX
        {
            get { return _isStrictX; }
            set
            {
                if (_isStrictX != value)
                {
                    _isStrictX = value;
                    NotifyPropertyChanged(nameof(IsStrictX));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether Y values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictY
        {
            get { return _isStrictY; }
            set
            {
                if (_isStrictY != value)
                {
                    _isStrictY = value;
                    NotifyPropertyChanged(nameof(IsStrictY));
                }
            }
        }

        /// <summary>
        /// Gets or sets the sort order for X values.
        /// </summary>
        public SortOrder XOrder
        {
            get { return _xOrder; }
            set
            {
                if (_xOrder != value)
                {
                    _xOrder = value;
                    NotifyPropertyChanged(nameof(XOrder));
                }
            }
        }

        /// <summary>
        /// Gets or sets the sort order for Y values.
        /// </summary>
        public SortOrder YOrder
        {
            get { return _yOrder; }
            set
            {
                if (_yOrder != value)
                {
                    _yOrder = value;
                    NotifyPropertyChanged(nameof(YOrder));
                }
            }
        }

        private double _x;
        private double _p1;
        private double _p2;
        private double _p3;
        private double _p4;

        /// <summary>
        /// Gets or sets the X value (independent variable) for this row.
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                if (!_x.AlmostEquals(value))
                {
                    _x = value;
                    NotifyPropertyChanged(nameof(X));
                }
            }
        }

        /// <summary>
        /// Gets or sets the first distribution parameter value.
        /// </summary>
        /// <value>The first parameter value of the distribution.</value>
        public double P1
        {
            get { return _p1; }
            set
            {
                if (!_p1.AlmostEquals(value))
                {
                    _p1 = value;
                    NotifyPropertyChanged(nameof(P1));
                }
            }
        }

        /// <summary>
        /// Gets or sets the second distribution parameter value.
        /// </summary>
        /// <value>The second parameter value, or unused if the distribution has fewer than 2 parameters.</value>
        public double P2
        {
            get { return _p2; }
            set
            {
                if (!_p2.AlmostEquals(value))
                {
                    _p2 = value;
                    NotifyPropertyChanged(nameof(P2));
                }
            }
        }

        /// <summary>
        /// Gets or sets the third distribution parameter value.
        /// </summary>
        /// <value>The third parameter value, or unused if the distribution has fewer than 3 parameters.</value>
        public double P3
        {
            get { return _p3; }
            set
            {
                if (!_p3.AlmostEquals(value))
                {
                    _p3 = value;
                    NotifyPropertyChanged(nameof(P3));
                }
            }
        }

        /// <summary>
        /// Gets or sets the fourth distribution parameter value.
        /// </summary>
        /// <value>The fourth parameter value, or unused if the distribution has fewer than 4 parameters.</value>
        public double P4
        {
            get { return _p4; }
            set
            {
                if (!_p4.AlmostEquals(value))
                {
                    _p4 = value;
                    NotifyPropertyChanged(nameof(P4));
                }
            }
        }

        /// <summary>
        /// Gets the effective minimum value of the distribution at the minimum probability threshold.
        /// </summary>
        /// <value>The inverse CDF at the minimum probability, or <see cref="double.NaN"/> if parameters are invalid.</value>
        public double Minimum => _distribution.ParametersValid ? _distribution.InverseCDF(_minProbability) : double.NaN;

        /// <summary>
        /// Gets the effective maximum value of the distribution at the maximum probability threshold.
        /// </summary>
        /// <value>The inverse CDF at the maximum probability, or <see cref="double.NaN"/> if parameters are invalid.</value>
        public double Maximum => _distribution.ParametersValid ? _distribution.InverseCDF(_maxProbability) : double.NaN;

        /// <summary>
        /// Gets the mean of the distribution.
        /// </summary>
        /// <value>The distribution mean, or <see cref="double.NaN"/> if parameters are invalid.</value>
        public double Mean => _distribution.ParametersValid ? _distribution.Mean : double.NaN;

        private readonly double _minProbability = 1E-5d;
        private readonly double _maxProbability = 1d - 1E-5d;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributionRowItem"/> class.
        /// </summary>
        /// <param name="xVal">The initial X value.</param>
        /// <param name="dist">The probability distribution for this row.</param>
        /// <param name="list">The observable collection this item belongs to.</param>
        /// <param name="minX">The minimum allowed X value.</param>
        /// <param name="maxX">The maximum allowed X value.</param>
        /// <param name="minY">The minimum allowed Y value.</param>
        /// <param name="maxY">The maximum allowed Y value.</param>
        /// <param name="strictX">Whether X values must be strictly ordered.</param>
        /// <param name="strictY">Whether Y values must be strictly ordered.</param>
        /// <param name="orderX">The sort order for X values.</param>
        /// <param name="orderY">The sort order for Y values.</param>
        /// <exception cref="Exception">Thrown when the distribution has more than 4 parameters.</exception>
        public DistributionRowItem(double xVal, UnivariateDistributionBase dist, ObservableCollection<object> list, double minX, double maxX, double minY, double maxY, bool strictX, bool strictY, Numerics.Data.SortOrder orderX, Numerics.Data.SortOrder orderY) : base(list)
        {
            MinXValue = minX;
            MinYValue = minY;
            MaxXValue = maxX;
            MaxYValue = maxY;
            _x = xVal;
            _distribution = dist.Clone();

            if (_distribution.Type == UnivariateDistributionType.PertPercentile ||
                _distribution.Type == UnivariateDistributionType.PertPercentileZ)
            {
                _minProbability = 0.05;
                _maxProbability = 0.95;
            }

            double[] parameters = _distribution.GetParameters;
            if (parameters.Count() > 4)
            {
                throw new Exception("Uncertain Ordered Paired Data editor can only work with distributions with 4 parameters or less.");
            }
            _p1 = parameters[0];
            if (parameters.Count() >= 2) { _p2 = parameters[1]; }
            if (parameters.Count() >= 3) { _p3 = parameters[2]; }
            if (parameters.Count() >= 4) { _p4 = parameters[3]; }
            _propertyNames = _distribution.GetParameterPropertyNames;
            _propertyDisplayNames = new string[(_propertyNames.Count())];
            string[,] paramString = _distribution.ParametersToString;
            for (int i = 0; i < paramString.GetLength(0); i++)
            {
                _propertyDisplayNames[i] = paramString[i, 0];
            }
            IsStrictX = strictX;
            IsStrictY = strictY;
            XOrder = orderX;
            YOrder = orderY;
            AddDataRules();
        }

        /// <summary>
        /// Raises the PropertyChanged event for all properties, forcing a UI refresh.
        /// </summary>
        public void RaisePropertyChanged()
        {
            NotifyPropertyChanged();
        }

        /// <summary>
        /// Updates the distribution with the current parameter values and validates them.
        /// </summary>
        /// <returns><c>true</c> if the parameters are invalid; otherwise, <c>false</c>.</returns>
        private bool SetDistribution()
        {
            var newParams = new double[_distribution.NumberOfParameters];
            if (_distribution.NumberOfParameters >= 1) { newParams[0] = P1; }
            if (_distribution.NumberOfParameters >= 2) { newParams[1] = P2; }
            if (_distribution.NumberOfParameters >= 3) { newParams[2] = P3; }
            if (_distribution.NumberOfParameters >= 4) { newParams[3] = P4; }
            _distribution.SetParameters(newParams);

            if (_distribution.ParametersValid == false)
            {
                var argError = _distribution.ValidateParameters(newParams, false);
                int pKeyCount = Math.Min(_propertyNames.Count(), RuleMap.Keys.Count(k => k.StartsWith("P")));
                for (int i = 0; i < pKeyCount; i++)
                    RuleMap["P" + (i + 1)].ErrorMessage = argError.Message;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void AddValidationRules()
        {
        }

        /// <summary>
        /// Adds the validation rules for this row item, including X/Y range checks and ordering constraints.
        /// </summary>
        private void AddDataRules()
        {
            if (XOrder != SortOrder.None)
            {
                AddRule(nameof(X), () => OrderRule<double, DistributionRowItem>(o => o.X, nameof(X), XOrder == SortOrder.Ascending, !IsStrictX), "X values must be in " + XOrder.ToString() + " order.");
            }
            AddRule(nameof(X), () => double.IsNaN(X) || X < MinXValue, "X values must be greater than or equal to " + MinXValue + ".");
            AddRule(nameof(X), () => double.IsNaN(X) || X > MaxXValue, "X values must be less than or equal to " + MaxXValue + ".");

            // These need to be before the min, max, and mean validation rules. I think so that the distribution parameters can be set so that the mean, min, and max can be updated.
            AddRule(nameof(P1), SetDistribution, null);
            if (_propertyNames.Count() >= 2) { AddRule(nameof(P2), SetDistribution, null); }
            if (_propertyNames.Count() >= 3) { AddRule(nameof(P3), SetDistribution, null); }
            if (_propertyNames.Count() >= 4) { AddRule(nameof(P4), SetDistribution, null); }

            // Order Rules
            if (YOrder != SortOrder.None)
            {
                // Means
                AddRule(nameof(P1), () => OrderRule<double, DistributionRowItem>(o => o.Mean, nameof(P1), YOrder == SortOrder.Ascending, !IsStrictY), "Mean values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 2)
                    AddRule(nameof(P2), () => OrderRule<double, DistributionRowItem>(o => o.Mean, nameof(P2), YOrder == SortOrder.Ascending, !IsStrictY), "Mean values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 3)
                    AddRule(nameof(P3), () => OrderRule<double, DistributionRowItem>(o => o.Mean, nameof(P3), YOrder == SortOrder.Ascending, !IsStrictY), "Mean values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 4)
                    AddRule(nameof(P4), () => OrderRule<double, DistributionRowItem>(o => o.Mean, nameof(P4), YOrder == SortOrder.Ascending, !IsStrictY), "Mean values must be in " + YOrder.ToString() + " order.");

                // Minimums
                AddRule(nameof(P1), () => OrderRule<double, DistributionRowItem>(o => o.Minimum, nameof(Minimum), YOrder == SortOrder.Ascending, !IsStrictY), "Minimum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 2)
                    AddRule(nameof(P2), () => OrderRule<double, DistributionRowItem>(o => o.Minimum, nameof(Minimum), YOrder == SortOrder.Ascending, !IsStrictY), "Minimum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 3)
                    AddRule(nameof(P3), () => OrderRule<double, DistributionRowItem>(o => o.Minimum, nameof(Minimum), YOrder == SortOrder.Ascending, !IsStrictY), "Minimum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 4)
                    AddRule(nameof(P4), () => OrderRule<double, DistributionRowItem>(o => o.Minimum, nameof(Minimum), YOrder == SortOrder.Ascending, !IsStrictY), "Minimum values must be in " + YOrder.ToString() + " order.");

                // Maximums
                AddRule(nameof(P1), () => OrderRule<double, DistributionRowItem>(o => o.Maximum, nameof(Maximum), YOrder == SortOrder.Ascending, !IsStrictY), "Maximum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 2)
                    AddRule(nameof(P2), () => OrderRule<double, DistributionRowItem>(o => o.Maximum, nameof(Maximum), YOrder == SortOrder.Ascending, !IsStrictY), "Maximum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 3)
                    AddRule(nameof(P3), () => OrderRule<double, DistributionRowItem>(o => o.Maximum, nameof(Maximum), YOrder == SortOrder.Ascending, !IsStrictY), "Maximum values must be in " + YOrder.ToString() + " order.");
                if (_propertyNames.Count() >= 4)
                    AddRule(nameof(P4), () => OrderRule<double, DistributionRowItem>(o => o.Maximum, nameof(Maximum), YOrder == SortOrder.Ascending, !IsStrictY), "Maximum values must be in " + YOrder.ToString() + " order.");
            }
            // Minimum and Maximum values
            AddRule(nameof(P1), () => Minimum < MinYValue || double.IsNaN(Minimum), "The min values must be greater than " + MinYValue + ".", new[] { "P2", "P3", "P4" });
            AddRule(nameof(P1), () => Maximum > MaxYValue || double.IsNaN(Maximum), "The max values must be less than " + MaxYValue + ".");
            if (_propertyNames.Count() >= 2)
            {
                AddRule(nameof(P2), () => Minimum < MinYValue || double.IsNaN(Minimum), "The min values must be greater than " + MinYValue + ".", new[] { "P1", "P3", "P4" });
                AddRule(nameof(P2), () => Maximum > MaxYValue || double.IsNaN(Maximum), "The max values must be less than " + MaxYValue + ".");
            }

            if (_propertyNames.Count() >= 3)
            {
                AddRule(nameof(P3), () => Minimum < MinYValue || double.IsNaN(Minimum), "The min values must be greater than " + MinYValue + ".", new[] { "P1", "P2", "P4" });
                AddRule(nameof(P3), () => Maximum > MaxYValue || double.IsNaN(Maximum), "The max values must be less than " + MaxYValue + ".");
            }

            if (_propertyNames.Count() >= 4)
            {
                AddRule(nameof(P4), () => Minimum < MinYValue || double.IsNaN(Minimum), "The min values must be greater than " + MinYValue + ".", new[] { "P1", "P2", "P3" });
                AddRule(nameof(P4), () => Maximum > MaxYValue || double.IsNaN(Maximum), "The max values must be less than " + MaxYValue + ".");
            }
            if (Distribution as Pert != null || Distribution as PertPercentile != null)
            {
                AddRule(nameof(P1), () => P1 < MinYValue, "The parameter values must be greater than " + MinYValue + ".", new[] { "P2", "P3", "P4" });
                AddRule(nameof(P1), () => P1 > MaxYValue, "The parameter values must be less than " + MaxYValue + ".");
                if (_propertyNames.Count() >= 2)
                {
                    AddRule(nameof(P2), () => P2 < MinYValue, "The parameter values must be greater than " + MinYValue + ".", new[] { "P1", "P3", "P4" });
                    AddRule(nameof(P2), () => P2 > MaxYValue, "The parameter values must be less than " + MaxYValue + ".");
                }

                if (_propertyNames.Count() >= 3)
                {
                    AddRule(nameof(P3), () => P3 < MinYValue, "The parameter values must be greater than " + MinYValue + ".", new[] { "P1", "P2", "P4" });
                    AddRule(nameof(P3), () => P3 > MaxYValue, "The parameter values must be less than " + MaxYValue + ".");
                }
            }
        }

        /// <inheritdoc/>
        public override string PropertyDisplayName(string propertyName)
        {
            if (propertyName == nameof(P1)) { return _propertyDisplayNames[0]; }
            if (propertyName == nameof(P2) && _propertyDisplayNames.Length > 1) { return _propertyDisplayNames[1]; }
            if (propertyName == nameof(P3) && _propertyDisplayNames.Length > 2) { return _propertyDisplayNames[2]; }
            if (propertyName == nameof(P4) && _propertyDisplayNames.Length > 3) { return _propertyDisplayNames[3]; }
            return propertyName;
        }

        /// <inheritdoc/>
        public override bool IsGridDisplayable(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(X): return true;
                case nameof(P1): return true;
                case nameof(P2): return _propertyDisplayNames.Length > 1;
                case nameof(P3): return _propertyDisplayNames.Length > 2;
                case nameof(P4): return _propertyDisplayNames.Length > 3;
                default: return false;
            }
        }
    }
}
