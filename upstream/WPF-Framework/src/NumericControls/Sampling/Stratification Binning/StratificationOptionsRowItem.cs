using System.Collections.ObjectModel;
using GenericControls;
using Numerics.Sampling;

namespace NumericControls
{
    /// <summary>
    /// Represents a row item for defining stratification options in a data grid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class StratificationOptionsRowItem : DataGridRowItem
    {
        private double _start;
        private double _end;
        private int _numberOfBins;
        private int _maxBins;
        private bool _isProbability = true;

        /// <summary>
        /// The lower bound of the starting bin.
        /// </summary>
        public double Start
        {
            get { return _start; }
            set
            {
                if (_start != value)
                {
                    _start = value;
                    NotifyPropertyChanged(nameof(Start));
                }
            }
        }

        /// <summary>
        /// The upper bound of the end bin.
        /// </summary>
        public double End
        {
            get { return _end; }
            set
            {
                if (_end != value)
                {
                    _end = value;
                    NotifyPropertyChanged(nameof(End));
                }
            }
        }

        /// <summary>
        /// The number of bins. Must be greater than 1.
        /// </summary>
        public int NumberOfBins
        {
            get { return _numberOfBins; }
            set
            {
                if (_numberOfBins != value)
                {
                    _numberOfBins = value;
                    NotifyPropertyChanged(nameof(NumberOfBins));
                }
            }
        }

        /// <summary>
        /// The maximum number of bins.
        /// </summary>
        public int MaxBins
        {
            get { return _maxBins; }
            set
            {
                if (_maxBins != value)
                {
                    _maxBins = value;
                    NotifyPropertyChanged(nameof(MaxBins));
                }
            }
        }

        /// <summary>
        /// Determines whether or not the values are probabilities.
        /// </summary>
        public bool IsProbability
        {
            get { return _isProbability; }
            set
            {
                if (_isProbability != value)
                {
                    _isProbability = value;
                    NotifyPropertyChanged(nameof(IsProbability));
                }
            }
        }

        /// <summary>
        /// Create empty row item.
        /// </summary>
        public StratificationOptionsRowItem() : base(null)
        {
            _start = 0d;
            _end = 0d;
            _numberOfBins = 0;
            _maxBins = 1000;
            _isProbability = false;
        }

        /// <summary>
        /// Create new row item with specific options.
        /// </summary>
        /// <param name="stratificationOptions">Stratification options.</param>
        /// <param name="maximumBins">Max number of bins.</param>
        /// <param name="list">List of stratification options.</param>
        public StratificationOptionsRowItem(StratificationOptions stratificationOptions, int maximumBins, ObservableCollection<object> list) : base(list)
        {
            _start = stratificationOptions.LowerBound;
            _end = stratificationOptions.UpperBound;
            _numberOfBins = stratificationOptions.NumberOfBins;
            _isProbability = stratificationOptions.IsProbability;
            _maxBins = maximumBins;
            // Must add this rule here so that the maximum number of bins is appropriately defined. The AddValidationRules() Method is called inside the MyBase.New() method.
            AddRule(nameof(NumberOfBins), () => NumberOfBins > MaxBins, $"Number of bins must be less than or equal to {MaxBins}.");
        }

        /// <summary>
        /// Returns stratification options given the row item inputs.
        /// </summary>
        public StratificationOptions GetStratificationOptions()
        {
            return new StratificationOptions(_start, _end, _numberOfBins, _isProbability);
        }

        /// <inheritdoc/>
        public override void AddValidationRules()
        {
            // Order rules
            AddRule(nameof(Start), () => OrderRule<double, StratificationOptionsRowItem>(o => o.Start, nameof(Start)), "Start values must be in ascending order.");
            AddRule(nameof(End), () => OrderRule<double, StratificationOptionsRowItem>(o => o.End, nameof(End)), "End values must be in ascending order.");
            // Greater than/less than rules
            AddRule(nameof(Start), () => Start >= End, "Start value must be less than the end value.", new[] { nameof(End) });
            AddRule(nameof(End), () => Start >= End, "End value must be greater than the start value.", new[] { nameof(Start) });
            // Probability rules
            AddRule(nameof(Start), () =>
            {
                if (_isProbability == true)
                {
                    if (Start < 0.0d) return true;
                }
                return false;
            }, "Start value must be greater than or equal to 0.");
            AddRule(nameof(End), () =>
            {
                if (_isProbability == true)
                {
                    if (End > 1.0d) return true;
                }
                return false;
            }, "End value must be less than or equal to 1.");
            // 
            AddRule(nameof(NumberOfBins), () => NumberOfBins < 2, "Number of bins must be greater than 1.");
        }

        /// <inheritdoc/>
        public override string PropertyDisplayName(string propertyName)
        {
            if (propertyName == nameof(NumberOfBins)) return "# Bins";
            return propertyName;
        }

        /// <inheritdoc/>
        public override bool IsGridDisplayable(string propertyName)
        {
            switch (propertyName)
            {
                case nameof(Start): return true;
                case nameof(End): return true;
                case nameof(NumberOfBins): return true;
                default: return false;
            }
        }

    }
}
