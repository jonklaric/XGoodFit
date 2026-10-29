using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Numerics.Data;
using Numerics.Distributions;

namespace NumericControls
{
    /// <summary>
    /// Represents a data item that manages distribution data for uncertain ordered paired data.
    /// Handles the collection of distribution row items and synchronizes changes between the data model and UI.
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
    public class DistributionDataItem
    {

        private UncertainOrderedPairedData _data;
        private bool _updatingData = false;
        private double _maxXValue;
        private double _minXValue;
        private double _maxYValue;
        private double _minYValue;
        private bool _isStrictX;
        private bool _isStrictY;
        private SortOrder _xOrder;
        private SortOrder _yOrder;

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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxXValue)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinXValue)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MaxYValue)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MinYValue)));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether X values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictX
        {
            get { return _isStrictX; }
            set
            {
                if (_isStrictX != value)
                {
                    _isStrictX = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStrictX)));
                }
            }
        }

        /// <summary>
        /// Gets or sets whether Y values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictY
        {
            get { return _isStrictY; }
            set
            {
                if (_isStrictY != value)
                {
                    _isStrictY = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStrictY)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(XOrder)));
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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(YOrder)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the underlying uncertain ordered paired data.
        /// </summary>
        public UncertainOrderedPairedData Data
        {
            get { return _data; }
            set
            {
                if (_data == null && value == null) return;
                if (_data == value) return;
                if (_data != null) _data.CollectionChanged -= DataCollectionChanged;
                _data = value;
                Refresh();
                if (_data != null) _data.CollectionChanged += DataCollectionChanged;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Data)));
            }
        }

        /// <summary>
        /// Gets the distribution type for this data item.
        /// </summary>
        public UnivariateDistributionType Distribution { get; private set; }

        /// <summary>
        /// Gets the display name of the distribution type.
        /// </summary>
        public string DistributionName { get; private set; }

        /// <summary>
        /// Gets the collection of distribution row items for display in the data grid.
        /// </summary>
        public ObservableCollection<object> DistributionRows { get; private set; } = new ObservableCollection<object>();

        /// <summary>
        /// Occurs when data in a row changes.
        /// </summary>
        public event DataChangedEventHandler DataChanged;

        /// <summary>
        /// Represents a method that handles data changed events.
        /// </summary>
        /// <param name="dataChangedIndex">The index of the data item that changed.</param>
        public delegate void DataChangedEventHandler(int dataChangedIndex);

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="DistributionDataItem"/> class.
        /// </summary>
        /// <param name="d">The uncertain ordered paired data.</param>
        /// <param name="dType">The distribution type.</param>
        /// <param name="minX">The minimum allowed X value.</param>
        /// <param name="maxX">The maximum allowed X value.</param>
        /// <param name="minY">The minimum allowed Y value.</param>
        /// <param name="maxY">The maximum allowed Y value.</param>
        /// <param name="strictX">Whether X values must be strictly ordered.</param>
        /// <param name="strictY">Whether Y values must be strictly ordered.</param>
        /// <param name="orderX">The sort order for X values.</param>
        /// <param name="orderY">The sort order for Y values.</param>
        public DistributionDataItem(UncertainOrderedPairedData d, UnivariateDistributionType dType, double minX, double maxX, double minY, double maxY, bool strictX, bool strictY, SortOrder orderX, SortOrder orderY)
        {
            _data = d;
            if (_data != null) _data.CollectionChanged += DataCollectionChanged;
            Distribution = dType;
            _minXValue = minX;
            _maxXValue = maxX;
            _minYValue = minY;
            _maxYValue = maxY;
            _isStrictX = strictX;
            _isStrictY = strictY;
            _xOrder = orderX;
            _yOrder = orderY;
            if (_data is not null && _data.Count > 0)
                DistributionName = _data[0].Y.DisplayName;
            else if (UnivariateDistributionFactory.TryCreateDistribution(Distribution, out var distribution) &&
                     distribution is not null)
                DistributionName = distribution.DisplayName;
            else
                DistributionName = Distribution.ToString();
            Refresh();
        }

        /// <summary>
        /// Handles collection changed events from the underlying data source and synchronizes with the UI.
        /// </summary>
        /// <param name="sender">The data collection that changed.</param>
        /// <param name="e">The collection changed event arguments.</param>
        private void DataCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_updatingData == true) return;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        Refresh();
                        break;
                    }
                case NotifyCollectionChangedAction.Remove:
                    {
                        int startIndex = e.OldStartingIndex;
                        if (startIndex == -1 || e.OldItems == null)
                        {
                            Refresh();
                        }
                        else
                        {
                            for (int i = 1; i <= e.OldItems.Count; i++)
                                DistributionRows.RemoveAt(startIndex);
                        }
                        break;
                    }
                case NotifyCollectionChangedAction.Replace:
                    {
                        int index = _data.IndexOf((UncertainOrdinate)e.NewItems[0]);
                        Refresh(index);
                        break;
                    }
                default:
                    {
                        Refresh();
                        break;
                    }
            }
        }

        /// <summary>
        /// Refreshes all distribution rows from the underlying data.
        /// </summary>
        public void Refresh()
        {
            foreach (DistributionRowItem row in DistributionRows)
                row.PropertyChanged -= RowItem_PropertyChanged;
            DistributionRows.Clear();
            if (Data == null) return;
            DistributionRowItem rowItem;
            foreach (var o in Data)
            {
                rowItem = new DistributionRowItem(o.X, o.Y, DistributionRows, MinXValue, MaxXValue, MinYValue, MaxYValue, IsStrictX, IsStrictY, XOrder, YOrder);
                rowItem.PropertyChanged += RowItem_PropertyChanged;
                DistributionRows.Add(rowItem);
            }
        }

        /// <summary>
        /// Refreshes a specific distribution row from the underlying data.
        /// </summary>
        /// <param name="rowIndex">The index of the row to refresh.</param>
        public void Refresh(int rowIndex)
        {
            if (_data == null) return;
            if ((rowIndex >= _data.Count) || (rowIndex < 0)) return;
            ((DistributionRowItem)DistributionRows[rowIndex]).PropertyChanged -= RowItem_PropertyChanged;
            var rowItem = new DistributionRowItem(Data[rowIndex].X, Data[rowIndex].Y, DistributionRows, MinXValue, MaxXValue, MinYValue, MaxYValue, IsStrictX, IsStrictY, XOrder, YOrder);
            rowItem.PropertyChanged += RowItem_PropertyChanged;
            DistributionRows[rowIndex] = rowItem;
        }

        /// <summary>
        /// Handles property changed events for row items and updates the data source.
        /// </summary>
        /// <param name="sender">The row item that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void RowItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            _updatingData = true;
            DistributionRowItem rItem = (DistributionRowItem)sender;
            int dataIndex = DistributionRows.IndexOf(rItem);
            if (dataIndex < 0) { _updatingData = false; return; }
            _data[dataIndex] = new UncertainOrdinate(rItem.X, rItem.Distribution.Clone());
            _updatingData = false;
            DataChanged?.Invoke(dataIndex);
        }

    }
}
