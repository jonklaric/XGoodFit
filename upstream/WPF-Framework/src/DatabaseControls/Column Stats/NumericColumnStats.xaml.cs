using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DatabaseManager;
using Numerics.Data.Statistics;
using OxyPlot;
using OxyPlot.Wpf;

namespace DatabaseControls
{
    /// <summary>
    /// A WPF UserControl that displays statistical analysis and histogram visualization for numeric column data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This control provides comprehensive statistical analysis including count, minimum, maximum, sum, mean,
    /// standard deviation, and various percentiles (5th, 25th, 50th, 75th, 95th).
    /// </para>
    /// <para>
    /// Multiple classification methods are supported for histogram binning:
    /// <list type="bullet">
    /// <item><description>Rice Rule - automatic bin count based on data size</description></item>
    /// <item><description>Jenks Natural Breaks - minimizes within-class variance</description></item>
    /// <item><description>Quantiles - equal number of observations per class</description></item>
    /// <item><description>Head/Tail Breaks - for heavy-tailed distributions</description></item>
    /// <item><description>Standard Deviations - breaks at multiples of standard deviation</description></item>
    /// <item><description>Equal Interval - equal-width bins</description></item>
    /// <item><description>Custom Interval - user-defined interval size</description></item>
    /// <item><description>Manual Interval - user-editable break values</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class NumericColumnStats : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(double[]),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(new double[] { }, DataProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="DataName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataNameProperty = DependencyProperty.Register(
            nameof(DataName),
            typeof(string),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(""));

        /// <summary>
        /// Identifies the <see cref="ClassCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ClassCountProperty = DependencyProperty.Register(
            nameof(ClassCount),
            typeof(int),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(4, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="Threshold"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
            nameof(Threshold),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(0.4, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="Deviations"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeviationsProperty = DependencyProperty.Register(
            nameof(Deviations),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(2.0, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="IntervalSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IntervalSizeProperty = DependencyProperty.Register(
            nameof(IntervalSize),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(10.0, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="DefaultIntervalSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultIntervalSizeProperty = DependencyProperty.Register(
            nameof(DefaultIntervalSize),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(10.0));

        /// <summary>
        /// Identifies the <see cref="DefaultR"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultRProperty = DependencyProperty.Register(
            nameof(DefaultR),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(1.08));

        /// <summary>
        /// Identifies the <see cref="GeometricR"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeometricRProperty = DependencyProperty.Register(
            nameof(GeometricR),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(1.08, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="GeometricMin"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeometricMinProperty = DependencyProperty.Register(
            nameof(GeometricMin),
            typeof(double),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(1.0, ClassCountProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="GeometricMirror"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GeometricMirrorProperty = DependencyProperty.Register(
            nameof(GeometricMirror),
            typeof(bool),
            typeof(NumericColumnStats),
            new UIPropertyMetadata(false, ClassCountProperty_Callback));

        #endregion

        #region Private Fields

        private double[] _sortedData = new double[] { };
        private string _stringFormat = "{0:0}";

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NumericColumnStats"/> class.
        /// </summary>
        /// <remarks>
        /// Sets up the statistics table with standard statistical measures and initializes
        /// the breaks table for histogram classification. Also configures the histogram series colors.
        /// </remarks>
        public NumericColumnStats()
        {
            InitializeComponent();

            // Set the summary statistics table
            DataTable statsDataTable = new DataTable("StatsDataTable");
            statsDataTable.Columns.Add(new DataColumn("Statistic", typeof(string)));
            statsDataTable.Columns.Add(new DataColumn("Value", typeof(string)));
            statsDataTable.Rows.Add("Count", "");
            statsDataTable.Rows.Add("Minimum", "");
            statsDataTable.Rows.Add("Maximum", "");
            statsDataTable.Rows.Add("Sum", "");
            statsDataTable.Rows.Add("Mean", "");
            statsDataTable.Rows.Add("Std. Deviation", "");
            statsDataTable.Rows.Add("Skewness", "");
            statsDataTable.Rows.Add("Kurtosis", "");
            statsDataTable.Rows.Add("5th %-ile", "");
            statsDataTable.Rows.Add("25th %-ile", "");
            statsDataTable.Rows.Add("50th %-ile", "");
            statsDataTable.Rows.Add("75th %-ile", "");
            statsDataTable.Rows.Add("95th %-ile", "");

            InMemoryReader statsDataView = new InMemoryReader(statsDataTable);
            StatsTable.DataView = statsDataView.GetTableManager(statsDataTable.TableName);
            StatsTable.TableToolbarTray.Visibility = Visibility.Collapsed;

            // Set the breaks table
            DataTable breaksDataTable = new DataTable("BreaksTable");
            breaksDataTable.Columns.Add(new DataColumn("Less Than", typeof(double)));
            breaksDataTable.Columns.Add(new DataColumn("Count", typeof(int)));
            InMemoryReader breaksDataView = new InMemoryReader(breaksDataTable);
            var viewer = breaksDataView.GetTableManager(breaksDataTable.TableName);
            viewer.EditAdded += BreaksEditAdded;
            BreaksTable.DataView = viewer;
            BreaksTable.SetColumnsAsReadOnly(new string[] { "Count" });

            BreaksTable.TableToolbarTray.Visibility = Visibility.Collapsed;

            HistogramSeries.FillColor = Color.FromArgb(75, 220, 20, 60);
            HistogramSeries.StrokeColor = Color.FromArgb(255, 255, 0, 0);
            BackGroundHistogramSeries.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the numeric data array to analyze and display.
        /// </summary>
        /// <value>An array of double values representing the column data.</value>
        /// <remarks>
        /// When set, the data is automatically sorted and invalid values (NaN, Infinity) are filtered out.
        /// Statistics and histogram are recalculated automatically.
        /// </remarks>
        public double[] Data
        {
            get => (double[])GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name for the data column.
        /// </summary>
        /// <value>A string representing the column name, used as the X-axis title in the histogram.</value>
        public string DataName
        {
            get => (string)GetValue(DataNameProperty);
            set => SetValue(DataNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of classes (bins) for histogram classification.
        /// </summary>
        /// <value>An integer specifying the number of classes. Default is 4.</value>
        /// <remarks>
        /// Used by Jenks Breaks, Quantiles, Equal Interval, and Manual Interval classification methods.
        /// </remarks>
        public int ClassCount
        {
            get => (int)GetValue(ClassCountProperty);
            set => SetValue(ClassCountProperty, value);
        }

        /// <summary>
        /// Gets or sets the threshold value for Head/Tail Breaks classification.
        /// </summary>
        /// <value>A double value between 0 and 1 representing the proportion threshold. Default is 0.4.</value>
        /// <remarks>
        /// The threshold determines when to stop the iterative head/tail classification process.
        /// </remarks>
        public double Threshold
        {
            get => (double)GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of standard deviations for Standard Deviation interval classification.
        /// </summary>
        /// <value>A double value representing the deviation multiplier. Default is 2.0.</value>
        public double Deviations
        {
            get => (double)GetValue(DeviationsProperty);
            set => SetValue(DeviationsProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval size for Custom Interval classification.
        /// </summary>
        /// <value>A double value specifying the width of each interval. Default is 10.0.</value>
        public double IntervalSize
        {
            get => (double)GetValue(IntervalSizeProperty);
            set => SetValue(IntervalSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the default interval size calculated from the data range.
        /// </summary>
        /// <value>A double value representing the default interval size.</value>
        public double DefaultIntervalSize
        {
            get => (double)GetValue(DefaultIntervalSizeProperty);
            set => SetValue(DefaultIntervalSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the default R value for geometric progression.
        /// </summary>
        /// <value>A double value representing the default geometric ratio. Default is 1.08.</value>
        public double DefaultR
        {
            get => (double)GetValue(DefaultRProperty);
            set => SetValue(DefaultRProperty, value);
        }

        /// <summary>
        /// Gets or sets the R value for geometric progression classification.
        /// </summary>
        /// <value>A double value representing the geometric ratio. Default is 1.08.</value>
        public double GeometricR
        {
            get => (double)GetValue(GeometricRProperty);
            set => SetValue(GeometricRProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum value for geometric progression.
        /// </summary>
        /// <value>A double value representing the geometric minimum. Default is 1.0.</value>
        public double GeometricMin
        {
            get => (double)GetValue(GeometricMinProperty);
            set => SetValue(GeometricMinProperty, value);
        }

        /// <summary>
        /// Gets or sets whether to mirror the geometric progression from the mean.
        /// </summary>
        /// <value>True to mirror the progression; otherwise, false. Default is false.</value>
        public bool GeometricMirror
        {
            get => (bool)GetValue(GeometricMirrorProperty);
            set => SetValue(GeometricMirrorProperty, value);
        }

        /// <summary>
        /// Gets the collection of histogram items for data binding to the histogram series.
        /// </summary>
        /// <value>An observable collection of <see cref="OxyPlot.Series.HistogramItem"/> objects.</value>
        public ObservableCollection<OxyPlot.Series.HistogramItem> HistogramData { get; } = new ObservableCollection<OxyPlot.Series.HistogramItem>();

        /// <summary>
        /// Gets the collection of histogram items for the background histogram (50 equal-interval bins).
        /// </summary>
        public ObservableCollection<OxyPlot.Series.HistogramItem> BackgroundHistogramData { get; } = new ObservableCollection<OxyPlot.Series.HistogramItem>();

        #endregion

        #region Dependency Property Callbacks

        /// <summary>
        /// Callback method invoked when the <see cref="Data"/> property changes.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void DataProperty_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            NumericColumnStats thisControl = (NumericColumnStats)d;

            double[]? newValue = e.NewValue as double[];
            if (newValue == null)
            {
                thisControl._sortedData = new double[] { };
            }
            else
            {
                thisControl._sortedData = newValue.Where(o => !double.IsNaN(o) && !double.IsInfinity(o)).ToArray();
                Array.Sort(thisControl._sortedData);
            }

            // Guard against empty array access
            if (thisControl._sortedData.Length == 0)
            {
                thisControl.DefaultR = 1.08;
                thisControl.DefaultIntervalSize = 10.0;
                thisControl.UpdateDataView();
                return;
            }

            double maxVal = thisControl._sortedData[thisControl._sortedData.Length - 1];
            double avg = thisControl._sortedData.Average();
            thisControl.DefaultR = (avg <= 0 || maxVal <= 0) ? 1.08 : Math.Pow(maxVal / avg, 1.0 / (3 - 1));
            double range = thisControl._sortedData[thisControl._sortedData.Length - 1] - thisControl._sortedData[0];
            double defaultInterval = range == 0 ? 1.0 : range / 5; // 5 equally sized bins
            if (thisControl.DefaultIntervalSize == thisControl.IntervalSize)
            {
                thisControl.IntervalSize = defaultInterval;
            }
            thisControl.DefaultIntervalSize = defaultInterval;

            thisControl.UpdateDataView();
        }

        /// <summary>
        /// Callback method invoked when classification-related properties change.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void ClassCountProperty_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((NumericColumnStats)d).Plot();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles edits made to the breaks table, ensuring break values remain in ascending order.
        /// </summary>
        /// <param name="edit">The table edit that was added.</param>
        private void BreaksEditAdded(TableEdit edit)
        {
            if (edit.GetType() == typeof(CellEdit))
            {
                double[] breaks = Array.ConvertAll(BreaksTable.DataView.GetColumn(0),
                    o => o is DBNull || (o is string s && string.IsNullOrWhiteSpace(s)) ? 0 : Convert.ToDouble(o));

                bool update = false;
                for (int i = ((CellEdit)edit).RowIndex; i >= 1; i--)
                {
                    if (breaks[i] < breaks[i - 1])
                    {
                        breaks[i - 1] = breaks[i];
                        update = true;
                    }
                }
                for (int i = ((CellEdit)edit).RowIndex; i <= breaks.Length - 2; i++)
                {
                    if (breaks[i + 1] < breaks[i])
                    {
                        breaks[i + 1] = breaks[i];
                        update = true;
                    }
                }

                if (update)
                {
                    object[] formattedBreaks = breaks.Select(b => (object)b.ToString("F15", CultureInfo.InvariantCulture)).ToArray();
                    BreaksTable.DataView.EditColumn(0, formattedBreaks);
                }

                if (ManualIntervalItem.IsSelected)
                {
                    Plot();
                }
                else
                {
                    if (breaks.Length != ClassCount) ClassCount = breaks.Length;
                    ManualIntervalItem.IsSelected = true;
                }
            }
        }

        /// <summary>
        /// Handles the properties panel toggle from the plot toolbar.
        /// </summary>
        /// <param name="targetPlot">The target plot control.</param>
        /// <param name="openProperties">Whether to open the properties panel.</param>
        /// <param name="propertyExpander">The property expander to display.</param>
        /// <param name="selectedObject">The selected object in the plot.</param>
        private void PlotToolbar_PropertiesCalled(Plot targetPlot, bool openProperties, OxyPlotControls.OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject)
        {
            if (!openProperties)
            {
                if (PropertiesControl.Visibility == Visibility.Visible && propertyExpander.HasValue)
                {
                    PropertiesControl.ExpandProperty(propertyExpander.Value, selectedObject);
                }
                return;
            }

            if (selectedObject == null)
            {
                if (PropertiesControl.Visibility == Visibility.Collapsed)
                {
                    PropertiesControl.Visibility = Visibility.Visible;
                    if (propertyExpander.HasValue)
                        PropertiesControl.ExpandProperty(propertyExpander.Value);
                }
                else
                {
                    PropertiesControl.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                PropertiesControl.Visibility = Visibility.Visible;
                if (propertyExpander.HasValue)
                    PropertiesControl.ExpandProperty(propertyExpander.Value, selectedObject);
            }
        }

        /// <summary>
        /// Handles the close request from the properties control.
        /// </summary>
        /// <param name="propertiesControl">The properties control requesting to close.</param>
        private void PropertiesControl_ClosePropertiesCalled(OxyPlotControls.OxyPlotPropertiesControl propertiesControl)
        {
            propertiesControl.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Toggles the visibility of the background histogram series.
        /// </summary>
        private void ShowBackgroundCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            BackGroundHistogramSeries.Visibility = ShowBackgroundCheckbox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            StatsPlot.InvalidatePlot(true);
        }

        /// <summary>
        /// Handles the selection change in the classification method combo box.
        /// </summary>
        /// <param name="sender">The combo box that raised the event.</param>
        /// <param name="e">Event arguments containing the selection change information.</param>
        private void GroupingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClassCountControl.Visibility = Visibility.Collapsed;
            HeadTailThresholdControl.Visibility = Visibility.Collapsed;
            DeviationsControl.Visibility = Visibility.Collapsed;
            IntervalSizeControl.Visibility = Visibility.Collapsed;
            GeometricRControl.Visibility = Visibility.Collapsed;
            GeometricMirrorControl.Visibility = Visibility.Collapsed;

            if (JenksBreaksItem.IsSelected || QuantilesItem.IsSelected || EqualIntervalItem.IsSelected || ManualIntervalItem.IsSelected)
            {
                ClassCountControl.Visibility = Visibility.Visible;
                ClassCountControl.MaxValue = ManualIntervalItem.IsSelected ? 50 : 12;
            }
            else if (HeadTailsItem.IsSelected)
            {
                HeadTailThresholdControl.Visibility = Visibility.Visible;
            }
            else if (StandardDeviationItem.IsSelected)
            {
                DeviationsControl.Visibility = Visibility.Visible;
            }
            else if (CustomIntervalItem.IsSelected)
            {
                IntervalSizeControl.Visibility = Visibility.Visible;
            }

            Plot();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Updates the statistics data view with calculated statistics from the sorted data.
        /// </summary>
        private void UpdateDataView()
        {
            if (StatsTable == null || StatsTable.DataView == null) return;

            string[] columnStrings = new string[StatsTable.DataView.NumberOfRows];
            if (_sortedData == null || _sortedData.Length == 0)
            {
                Array.Fill(columnStrings, "");
            }
            else
            {
                double[] prodMoments = Statistics.ProductMoments(_sortedData);
                double[] columnData = new double[]
                {
                    _sortedData.Length,
                    _sortedData[0],
                    _sortedData.Last(),
                    _sortedData.Sum(),
                    prodMoments[0],
                    prodMoments[1],
                    prodMoments[2],
                    prodMoments[3],
                    Statistics.Percentile(_sortedData, 0.05, true),
                    Statistics.Percentile(_sortedData, 0.25, true),
                    Statistics.Percentile(_sortedData, 0.5, true),
                    Statistics.Percentile(_sortedData, 0.75, true),
                    Statistics.Percentile(_sortedData, 0.95, true)
                };

                // Format Count (index 0) as integer, all others as F15
                columnStrings[0] = ((int)columnData[0]).ToString(CultureInfo.InvariantCulture);
                for (int i = 1; i < columnData.Length; i++)
                    columnStrings[i] = double.IsNaN(columnData[i]) ? "" : columnData[i].ToString("F15", CultureInfo.InvariantCulture);

                if (_sortedData.Length != Data.Length)
                {
                    InvalidDataDetectedWarningTextBlock.Visibility = Visibility.Visible;
                }
                else
                {
                    InvalidDataDetectedWarningTextBlock.Visibility = Visibility.Collapsed;
                }
            }

            StatsTable.DataView.EditColumn(1, columnStrings);
            StatsTable.UpdateVisibleRows();

            // Update histogram numeric output format (only if data exists)
            if (_sortedData != null && _sortedData.Length > 0)
            {
                _stringFormat = "{0:0}";
                double range = _sortedData.Last() - _sortedData[0];
                if (range < 0.1)
                {
                    _stringFormat = "{0:0.####}";
                }
                else if (range < 1)
                {
                    _stringFormat = "{0:0.###}";
                }
                else if (range < 10)
                {
                    _stringFormat = "{0:0.##}";
                }
                else if (range < 100)
                {
                    _stringFormat = "{0:0.#}";
                }

                HistogramSeries.LabelFormatString = _stringFormat;
            }

            // Generate background histogram (50 equal-interval bins)
            BackgroundHistogramData.Clear();
            if (_sortedData != null && _sortedData.Length > 1)
            {
                double[] bgBreaks = Classification.EqualInterval(_sortedData, 50, true);
                if (bgBreaks.Length > 0)
                {
                    int[] bgCounts = new int[bgBreaks.Length];
                    int binIdx = 0;
                    for (int i = 0; i < _sortedData.Length; i++)
                    {
                        while (binIdx < bgBreaks.Length && _sortedData[i] > bgBreaks[binIdx])
                        {
                            binIdx++;
                        }
                        if (binIdx >= bgBreaks.Length) break;
                        bgCounts[binIdx]++;
                    }

                    double prev = _sortedData[0];
                    for (int i = 0; i < bgBreaks.Length; i++)
                    {
                        double lo = prev, hi = bgBreaks[i];
                        if (hi != lo)
                            BackgroundHistogramData.Add(new OxyPlot.Series.HistogramItem(lo, hi, bgCounts[i] * (hi - lo)));
                        else
                            BackgroundHistogramData.Add(new OxyPlot.Series.HistogramItem(lo - 0.000001, hi + 0.000001, bgCounts[i] * 0.000002));
                        prev = hi;
                    }
                }
            }

            Plot();
        }

        /// <summary>
        /// Generates the histogram plot using the selected classification method.
        /// </summary>
        private void Plot()
        {
            if (GroupingComboBox.SelectedIndex == -1) GroupingComboBox.SelectedIndex = 0;
            if (_sortedData == null || _sortedData.Length == 0) return;
            Mouse.OverrideCursor = Cursors.Wait;

            double[] breaks = Array.Empty<double>();
            int[]? rangeCounts = null;

            if (JenksBreaksItem.IsSelected)
            {
                breaks = Classification.JenksNaturalBreaks(_sortedData, ClassCount, true, ref rangeCounts);
            }
            else if (RiceRuleItem.IsSelected)
            {
                if (_sortedData[0] == _sortedData[_sortedData.Length - 1])
                {
                    breaks = new double[1];
                    breaks[0] = _sortedData[0];
                }
                else
                {
                    Histogram hist = new Histogram(_sortedData);
                    breaks = new double[hist.NumberOfBins];
                    for (int i = 0; i < hist.NumberOfBins; i++)
                    {
                        breaks[i] = hist[i].UpperBound;
                    }
                }
            }
            else if (QuantilesItem.IsSelected)
            {
                breaks = Classification.Quantiles(_sortedData, ClassCount, true);
            }
            else if (HeadTailsItem.IsSelected)
            {
                breaks = Classification.HeadTailInterval(_sortedData, true, Threshold);
            }
            else if (StandardDeviationItem.IsSelected)
            {
                breaks = Classification.StandardDeviationInterval(_sortedData, Deviations, true);
            }
            else if (EqualIntervalItem.IsSelected)
            {
                breaks = Classification.EqualInterval(_sortedData, ClassCount, true);
            }
            else if (CustomIntervalItem.IsSelected)
            {
                breaks = Classification.DefinedInterval(_sortedData, IntervalSize, true);
            }
            else if (ManualIntervalItem.IsSelected)
            {
                if (BreaksTable.DataView != null && BreaksTable.DataView.NumberOfRows == ClassCount)
                {
                    // Table row count matches ClassCount — read existing/edited breaks
                    breaks = Array.ConvertAll(BreaksTable.DataView.GetColumn(0),
                        o => o is DBNull || (o is string s && string.IsNullOrWhiteSpace(s)) ? 0 : Convert.ToDouble(o));
                }
                else
                {
                    // Count mismatch — regenerate equal-interval breaks at new ClassCount
                    breaks = Classification.EqualInterval(_sortedData, ClassCount, true);
                }
            }

            // If there are too many breaks, don't show the label.
            if (breaks.Length >= 30)
            {
                HistogramSeries.LabelFormatString = null;
            }
            else if (HistogramSeries.LabelFormatString == null)
            {
                HistogramSeries.LabelFormatString = _stringFormat;
            }

            if (breaks.Length == 0)
            {
                HistogramData.Clear();
                return;
            }

            // Use Jenks-computed rangeCounts if available; otherwise count manually
            if (rangeCounts == null || rangeCounts.Length != breaks.Length)
            {
                rangeCounts = new int[breaks.Length];
                int binIdx = 0;
                for (int i = 0; i < _sortedData.Length; i++)
                {
                    // Advance binIdx until this sorted value fits within breaks[binIdx].
                    // Both i and binIdx move forward only — no retry needed.
                    while (binIdx < breaks.Length && _sortedData[i] > breaks[binIdx])
                    {
                        binIdx++;
                    }
                    if (binIdx >= breaks.Length) break;
                    rangeCounts[binIdx] += 1;
                }
            }

            List<Tuple<double, double>> ranges = new List<Tuple<double, double>>();
            ranges.Add(new Tuple<double, double>(_sortedData[0], breaks[0]));
            for (int i = 1; i < breaks.Length; i++)
            {
                ranges.Add(new Tuple<double, double>(breaks[i - 1], breaks[i]));
            }

            HistogramData.Clear();

            DataTable breaksDataTable = new DataTable("BreaksTable");
            breaksDataTable.Columns.Add(new DataColumn("Less Than", typeof(string)));
            breaksDataTable.Columns.Add(new DataColumn("Count", typeof(int)));

            for (int i = 0; i < ranges.Count; i++)
            {
                // Set breaks table
                breaksDataTable.Rows.Add(ranges[i].Item2.ToString("F15", CultureInfo.InvariantCulture), rangeCounts[i]);

                if (ranges[i].Item2 == ranges[i].Item1)
                {
                    double offset = 0.000001;
                    OxyPlot.Series.HistogramItem hItem = new OxyPlot.Series.HistogramItem(ranges[i].Item1 - offset, ranges[i].Item2 + offset, rangeCounts[i] * (2 * offset));
                    HistogramData.Add(hItem);
                }
                else if (rangeCounts[i] == 0)
                {
                    OxyPlot.Series.HistogramItem hItem = new OxyPlot.Series.HistogramItem(ranges[i].Item1, ranges[i].Item2, 0);
                    HistogramData.Add(hItem);
                }
                else
                {
                    OxyPlot.Series.HistogramItem hItem = new OxyPlot.Series.HistogramItem(ranges[i].Item1, ranges[i].Item2, rangeCounts[i] * (ranges[i].Item2 - ranges[i].Item1));
                    HistogramData.Add(hItem);
                }
            }

            // Set up the breaks table
            var breaksDataView = new InMemoryReader(breaksDataTable);
            if (BreaksTable.DataView != null)
                BreaksTable.DataView.EditAdded -= BreaksEditAdded;
            BreaksTable.DataView = breaksDataView.GetTableManager(breaksDataTable.TableName)!;
            BreaksTable.DataView.EditAdded += BreaksEditAdded;
            BreaksTable.SetColumnsAsReadOnly(new string[] { "Count" });

            StatsPlot.InvalidatePlot(true);
            Mouse.OverrideCursor = null;
        }

        #endregion
    }
}
