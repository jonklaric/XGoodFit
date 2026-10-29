using GenericControls;
using Numerics.Data;
using System.Collections.ObjectModel;

namespace NumericControls
{
    /// <summary>
    /// Represents a data grid row item for time series ordinates, providing a view model wrapper
    /// that uses the clone-and-replace pattern for undo/redo support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When a cell edit occurs, the setter clones the underlying SeriesOrdinate,
    /// modifies the clone, and replaces the original in the <see cref="TimeSeries"/> collection via the indexer.
    /// This fires a <see cref="System.Collections.Specialized.NotifyCollectionChangedAction.Replace"/> event
    /// which the undo bridge records.
    /// </para>
    /// <para>
    /// During undo replay, <see cref="SetOrdinate"/> updates the internal reference without triggering
    /// clone-and-replace, preventing feedback loops.
    /// </para>
    /// </remarks>
    public class TimeSeriesRowItem : DataGridRowItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesRowItem"/> class with default values.
        /// </summary>
        public TimeSeriesRowItem() : base(null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSeriesRowItem"/> class with the specified collection and ordinate.
        /// </summary>
        /// <param name="observableCollection">The observable collection that contains this row item.</param>
        /// <param name="ordinate">The time series ordinate to be wrapped by this row item.</param>
        /// <param name="series">The time series collection that contains the ordinate, used for clone-and-replace on edits.</param>
        /// <param name="index">The positional index of this ordinate within the series, used for O(1) replacement.</param>
        /// <param name="parentDataGrid">The parent validation grid used to honor bulk validation suppression.</param>
        public TimeSeriesRowItem(ObservableCollection<object> observableCollection, SeriesOrdinate<DateTime, double> ordinate, TimeSeries series, int index, ValidationDataGrid parentDataGrid = null) : base(observableCollection, parentDataGrid)
        {
            _ordinate = ordinate;
            _series = series;
            _index = index;
        }

        /// <summary>
        /// The underlying time series ordinate model object.
        /// </summary>
        private SeriesOrdinate<DateTime, double> _ordinate;

        /// <summary>
        /// The time series collection containing the ordinate, used for the clone-and-replace pattern.
        /// </summary>
        private TimeSeries _series;

        /// <summary>
        /// The positional index of this ordinate within the series.
        /// Used for O(1) replacement instead of IndexOf which uses value equality
        /// and can return the wrong index when duplicate entries exist.
        /// Stable because RowItems are rebuilt from scratch on every Reset event.
        /// </summary>
        private int _index;

        /// <summary>
        /// When true, suppresses <see cref="DataGridRowItem.NotifyPropertyChanged"/> calls in property setters.
        /// Set during bulk paste operations where per-cell visual updates are wasteful because
        /// <see cref="TimeSeriesTable.RebuildRowItems"/> will refresh everything at the end.
        /// </summary>
        internal bool SuppressNotify { get; set; }

        /// <summary>
        /// Gets or sets the date and time of this time series observation.
        /// When set, creates a clone of the underlying ordinate, modifies the clone, and replaces
        /// it in the time series to fire a CollectionChanged Replace event for undo support.
        /// </summary>
        public DateTime DateTime
        {
            get => _ordinate.Index;
            set
            {
                if (_ordinate.Index != value)
                {
                    var clone = _ordinate.Clone();
                    clone.Index = value;
                    ReplaceOrdinate(clone);
                    if (!SuppressNotify) NotifyPropertyChanged(nameof(DateTime));
                }
            }
        }

        /// <summary>
        /// Gets or sets the measured value of this time series observation.
        /// When set, creates a clone of the underlying ordinate, modifies the clone, and replaces
        /// it in the time series to fire a CollectionChanged Replace event for undo support.
        /// </summary>
        public double Value
        {
            get => _ordinate.Value;
            set
            {
                if (_ordinate.Value != value)
                {
                    var clone = _ordinate.Clone();
                    clone.Value = value;
                    ReplaceOrdinate(clone);
                    if (!SuppressNotify) NotifyPropertyChanged(nameof(Value));
                }
            }
        }

        /// <summary>
        /// Updates the underlying ordinate reference and notifies the UI of all property changes.
        /// Called by the CollectionChanged(Replace) handler during undo/redo to sync the RowItem
        /// with the restored model data without triggering a clone-and-replace back to the series.
        /// </summary>
        /// <param name="newOrdinate">The restored ordinate from the model.</param>
        public void SetOrdinate(SeriesOrdinate<DateTime, double> newOrdinate)
        {
            _ordinate = newOrdinate;
            NotifyPropertyChanged(nameof(DateTime));
            NotifyPropertyChanged(nameof(Value));
        }

        /// <summary>
        /// Replaces the current ordinate in the time series with a new clone using the stored positional index.
        /// This fires a CollectionChanged Replace event which the UndoableCollectionBridge records.
        /// Uses <see cref="_index"/> instead of IndexOf to avoid O(n) lookup
        /// and value-equality ambiguity when duplicate entries exist.
        /// </summary>
        /// <param name="newOrdinate">The cloned and modified ordinate to replace the current one.</param>
        private void ReplaceOrdinate(SeriesOrdinate<DateTime, double> newOrdinate)
        {
            if (_series == null) return;
            if (_index >= 0 && _index < _series.Count)
            {
                _series[_index] = newOrdinate;
                _ordinate = newOrdinate;
            }
        }

        /// <summary>
        /// Determines whether this ordinate violates strict ascending date-time order.
        /// </summary>
        /// <returns><see langword="true"/> when the current date-time is less than or equal to its predecessor; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        /// The stored positional index makes this check O(1). Searching the parent row collection
        /// for every row makes full-table validation O(n²) for large irregular series.
        /// </remarks>
        private bool IsDateTimeOutOfOrder()
        {
            if (_series == null || _series.TimeInterval != TimeInterval.Irregular)
                return false;
            if (_index <= 0 || _index >= _series.Count)
                return false;

            return _ordinate.Index <= _series[_index - 1].Index;
        }

        /// <summary>
        /// Adds validation rules for the time series row item properties.
        /// Validates that irregular date-time entries are in ascending data order
        /// and that the value is a valid number.
        /// </summary>
        public override void AddValidationRules()
        {
            AddRule(nameof(DateTime),
                IsDateTimeOutOfOrder,
                "Date/time values must be in ascending data order. Grid sorting does not reorder the time series used by the plot.");
            AddRule(nameof(Value), () => double.IsInfinity(Value), "The value must be a finite number.");
        }

        /// <summary>
        /// Determines whether the specified property should be displayed in the data grid.
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>True if the property should be displayed; otherwise, false.</returns>
        public override bool IsGridDisplayable(string propertyName)
        {
            if (propertyName == nameof(DateTime) || propertyName == nameof(Value))
                return true;
            return false;
        }

        /// <summary>
        /// Gets the display name for the specified property to be shown in the data grid header.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The display name for the property, or null if not recognized.</returns>
        public override string PropertyDisplayName(string propertyName)
        {
            if (propertyName == nameof(DateTime))
                return "Date Time";
            else if (propertyName == nameof(Value))
                return "Value";
            else
                return null;
        }
    }
}
