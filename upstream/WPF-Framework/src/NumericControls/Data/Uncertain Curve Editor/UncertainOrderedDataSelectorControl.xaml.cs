using Numerics.Data;
using Numerics.Distributions;
using OxyPlot;
using OxyPlot.Legends;
using OxyPlot.Wpf;
using OxyPlotControls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace NumericControls
{
    /// <summary>
    /// A user control that combines a data table editor with a plot for visualizing uncertain ordered paired data.
    /// Allows users to select distribution types and edit distribution parameters for each data point.
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
    public partial class UncertainOrderedDataSelectorControl : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="XAxisLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisLabelProperty = DependencyProperty.Register(nameof(XAxisLabel), typeof(string), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata("X Axis"));

        /// <summary>
        /// Gets or sets the label text for the X axis on the plot.
        /// </summary>
        public string XAxisLabel
        {
            get { return (string)GetValue(XAxisLabelProperty); }
            set { SetValue(XAxisLabelProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YAxisLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisLabelProperty = DependencyProperty.Register(nameof(YAxisLabel), typeof(string), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata("Y Axis"));

        /// <summary>
        /// Gets or sets the label text for the Y axis on the plot.
        /// </summary>
        public string YAxisLabel
        {
            get { return (string)GetValue(YAxisLabelProperty); }
            set { SetValue(YAxisLabelProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlotTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotTitleProperty = DependencyProperty.Register(nameof(PlotTitle), typeof(string), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the title displayed on the plot.
        /// </summary>
        public string PlotTitle
        {
            get { return (string)GetValue(PlotTitleProperty); }
            set { SetValue(PlotTitleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="PlotLegendPosition"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotLegendPositionProperty = DependencyProperty.Register(nameof(PlotLegendPosition), typeof(global::OxyPlot.Legends.LegendPosition), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(LegendPosition.BottomRight));

        /// <summary>
        /// Gets or sets the position of the legend on the plot.
        /// </summary>
        public LegendPosition PlotLegendPosition
        {
            get { return (LegendPosition)GetValue(PlotLegendPositionProperty); }
            set {  SetValue(PlotLegendPositionProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YAxisMinimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisMinimumProperty = DependencyProperty.Register(nameof(YAxisMinimum), typeof(double), typeof(UncertainOrderedDataSelectorControl), new UIPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum value for the Y axis on the plot.
        /// </summary>
        public double YAxisMinimum
        {
            get { return (double)GetValue(YAxisMinimumProperty); }
            set { SetValue(YAxisMinimumProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YAxisMaximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisMaximumProperty = DependencyProperty.Register(nameof(YAxisMaximum), typeof(double), typeof(UncertainOrderedDataSelectorControl), new UIPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum value for the Y axis on the plot.
        /// </summary>
        public double YAxisMaximum
        {
            get { return (double)GetValue(YAxisMaximumProperty); }
            set { SetValue(YAxisMaximumProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XAxisMinimum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisMinimumProperty = DependencyProperty.Register(nameof(XAxisMinimum), typeof(double), typeof(UncertainOrderedDataSelectorControl), new UIPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum value for the X axis on the plot.
        /// </summary>
        public double XAxisMinimum
        {
            get { return (double)GetValue(XAxisMinimumProperty); }
            set { SetValue(XAxisMinimumProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XAxisMaximum"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisMaximumProperty = DependencyProperty.Register(nameof(XAxisMaximum), typeof(double), typeof(UncertainOrderedDataSelectorControl), new UIPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum value for the X axis on the plot.
        /// </summary>
        public double XAxisMaximum
        {
            get { return (double)GetValue(XAxisMaximumProperty); }
            set { SetValue(XAxisMaximumProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XColumnHeaderProperty = DependencyProperty.Register(nameof(XColumnHeader), typeof(string), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata("X Data"));

        /// <summary>
        /// Gets or sets the header text for the X data column in the grid.
        /// </summary>
        public string XColumnHeader
        {
            get
            { return (string)GetValue(XColumnHeaderProperty); }
            set { SetValue(XColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YColumnHeaderProperty = DependencyProperty.Register(nameof(YColumnHeader), typeof(string), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata("Y Data"));

        /// <summary>
        /// Gets or sets the header text for the Y data column in the grid.
        /// </summary>
        public string YColumnHeader
        {
            get { return (string)GetValue(YColumnHeaderProperty); }
            set { SetValue(YColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStrictXProperty = DependencyProperty.Register(nameof(IsStrictX), typeof(bool), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether X values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictX
        {
            get { return (bool)GetValue(IsStrictXProperty); }
            set { SetValue(IsStrictXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStrictYProperty = DependencyProperty.Register(nameof(IsStrictY), typeof(bool), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether Y values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictY
        {
            get { return (bool)GetValue(IsStrictYProperty); }
            set { SetValue(IsStrictYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrderXProperty = DependencyProperty.Register(nameof(OrderX), typeof(SortOrder), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(SortOrder.Ascending));

        /// <summary>
        /// Gets or sets the sort order for X values.
        /// </summary>
        public SortOrder OrderX
        {
            get { return (SortOrder)GetValue(OrderXProperty); }
            set { SetValue(OrderXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrderYProperty = DependencyProperty.Register(nameof(OrderY), typeof(SortOrder), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(SortOrder.Ascending));

        /// <summary>
        /// Gets or sets the sort order for Y values.
        /// </summary>
        public SortOrder OrderY
        {
            get { return (SortOrder)GetValue(OrderYProperty); }
            set { SetValue(OrderYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(nameof(MaximumX), typeof(double), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum allowed X value for validation.
        /// </summary>
        public double MaximumX
        {
            get { return (double)GetValue(MaximumXProperty); }
            set { SetValue(MaximumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(nameof(MinimumX), typeof(double), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed X value for validation.
        /// </summary>
        public double MinimumX
        {
            get { return (double)GetValue(MinimumXProperty); }
            set { SetValue(MinimumXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(nameof(MaximumY), typeof(double), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum allowed Y value for validation.
        /// </summary>
        public double MaximumY
        {
            get { return (double)GetValue(MaximumYProperty); }
            set { SetValue(MaximumYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="MinimumY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(nameof(MinimumY), typeof(double), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(double.MinValue));

        /// <summary>
        /// Gets or sets the minimum allowed Y value for validation.
        /// </summary>
        public double MinimumY
        {
            get { return (double)GetValue(MinimumYProperty); }
            set { SetValue(MinimumYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets whether the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TableWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TableWidthProperty = DependencyProperty.Register(nameof(TableWidth), typeof(GridLength), typeof(UncertainOrderedDataSelectorControl), new FrameworkPropertyMetadata(new GridLength(1d, GridUnitType.Star)));

        /// <summary>
        /// Gets or sets the width of the data table portion of the control.
        /// </summary>
        public GridLength TableWidth
        {
            get { return (GridLength)GetValue(TableWidthProperty); }
            set { SetValue(TableWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedUncertainOrderedData"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedUncertainOrderedDataProperty = DependencyProperty.Register(nameof(SelectedUncertainOrderedData), typeof(UncertainOrderedPairedData), typeof(UncertainOrderedDataSelectorControl), new PropertyMetadata(null, SetData));

        /// <summary>
        /// Callback invoked when the SelectedUncertainOrderedData property changes.
        /// Updates the distribution combobox selection and configures line visibility based on distribution type.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void SetData(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(UncertainOrderedDataSelectorControl)) return;
            UncertainOrderedDataSelectorControl thisControl = (UncertainOrderedDataSelectorControl)d;
            if (e.NewValue == null)
            {
                thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
                return;
            }
            // 
            // GenerateCurveSet(Curve)
            UncertainOrderedPairedData newCurve = e.NewValue as UncertainOrderedPairedData;
            if (newCurve == null)
            {
                thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
                return;
            }
            // 
            int index = -1;
            for (int i = 0; i < thisControl.Distributions.Count; i++)
            {
                if (thisControl.Distributions[i].Data.Distribution == newCurve.Distribution)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
                return;
            }
            // 
            thisControl.CurveUncertaintyComboBox.SelectionChanged -= thisControl.CurveUncertaintyComboBox_SelectionChanged;
            thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
            thisControl.CurveUncertaintyComboBox.SelectionChanged += thisControl.CurveUncertaintyComboBox_SelectionChanged;
            // 
            var newItem = new DistributionDataItem(newCurve, newCurve.Distribution, thisControl.MinimumX, thisControl.MaximumX, thisControl.MinimumY, thisControl.MaximumY, thisControl.IsStrictX, thisControl.IsStrictY, thisControl.OrderX, thisControl.OrderY);
            newItem.DataChanged += (int dataIndex) =>
            {
                if (thisControl._pastingData == false)
                    thisControl.GetBindingExpression(SelectedUncertainOrderedDataProperty).UpdateSource();
                thisControl.UpdatePlot(dataIndex);
            };
            thisControl.Distributions[index] = newItem;
            // 
            thisControl.CurveUncertaintyComboBox.SelectedIndex = index;
            // Update lines based on distribution type
            thisControl.MeanLine.Visibility = Visibility.Visible;
            thisControl.MedianLine.Visibility = Visibility.Visible;
            thisControl.ModeLine.Visibility = Visibility.Visible;
            thisControl.MaximumLine.Visibility = Visibility.Visible;
            thisControl.MinimumLine.Visibility = Visibility.Visible;
            thisControl.UncertaintyBoundsArea.Visibility = Visibility.Visible;
            switch (newCurve.Distribution)
            {
                case UnivariateDistributionType.Uniform:
                    {
                        thisControl.MeanLine.Visibility = Visibility.Collapsed;
                        thisControl.MedianLine.Visibility = Visibility.Collapsed;
                        thisControl.ModeLine.Visibility = Visibility.Collapsed;
                        break;
                    }

                case UnivariateDistributionType.Deterministic:
                    {
                        thisControl.MaximumLine.Visibility = Visibility.Collapsed;
                        thisControl.MinimumLine.Visibility = Visibility.Collapsed;
                        thisControl.MedianLine.Visibility = Visibility.Collapsed;
                        thisControl.ModeLine.Visibility = Visibility.Collapsed;
                        thisControl.UncertaintyBoundsArea.Visibility = Visibility.Collapsed;
                        break;
                    }
            }
        }

        /// <summary>
        /// Get and set the selected probability distribution.
        /// </summary>
        public UncertainOrderedPairedData SelectedUncertainOrderedData
        {
            get { return (UncertainOrderedPairedData)GetValue(SelectedUncertainOrderedDataProperty); }
            set { SetValue(SelectedUncertainOrderedDataProperty, value); }
        }

        private ObservableCollection<DistributionDataItem> Distributions { get; set; } = new ObservableCollection<DistributionDataItem>();
       
        /// <summary>
        /// Dependency property for the control distribution options.
        /// </summary>
        public static readonly DependencyProperty DistributionOptionsProperty = DependencyProperty.Register(nameof(DistributionOptions), typeof(List<UnivariateDistributionType>), typeof(UncertainOrderedDataSelectorControl), new PropertyMetadata(DefaultDistributionOptions, DistributionOptionsCallback));

        /// <summary>
        /// Gets and sets the distribution options.
        /// </summary>
        public List<UnivariateDistributionType> DistributionOptions
        {
            get { return (List<UnivariateDistributionType>)GetValue(DistributionOptionsProperty); }
            set { SetValue(DistributionOptionsProperty, value); }
        }

        /// <summary>
        /// When the dependency property changes, this sets the distribution options.
        /// </summary>
        private static void DistributionOptionsCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UncertainOrderedDataSelectorControl thisControl = (UncertainOrderedDataSelectorControl)d;
            thisControl.Distributions.Clear();
            if ((e.NewValue == null) || (e.NewValue.GetType() != typeof(List<UnivariateDistributionType>)))
            {
                thisControl.DistributionOptions = DefaultDistributionOptions;
            }
            else
            {
                foreach (var dist in (List<UnivariateDistributionType>)e.NewValue)
                {
                    if (!UnivariateDistributionFactory.TryCreateDistribution(dist, out var distribution) ||
                        distribution is null) continue;
                    // 
                    var ordinates = new List<UncertainOrdinate>();
                    for (int i = 0; i <= 5; i++)
                        ordinates.Add(new UncertainOrdinate(i * 2, distribution));
                    var distCurve = new UncertainOrderedPairedData(ordinates, thisControl.IsStrictX, thisControl.OrderX, thisControl.IsStrictY, thisControl.OrderY, dist);
                    // 
                    var distItem = new DistributionDataItem(distCurve, dist, thisControl.MinimumX, thisControl.MaximumX, thisControl.MinimumY, thisControl.MaximumY, thisControl.IsStrictX, thisControl.IsStrictY, thisControl.OrderX, thisControl.OrderY);
                    thisControl.Distributions.Add(distItem);
                }
            }
            // 
        }

        /// <summary>
        /// Returns the default distribution types supported by the uncertain curve selector.
        /// </summary>
        /// <returns>A list of supported distribution types.</returns>
        public static List<UnivariateDistributionType> DefaultDistributionOptions
        {
            get
            {
                return ((UnivariateDistributionType[])Enum.GetValues(typeof(UnivariateDistributionType))).Where(o => (o != UnivariateDistributionType.Bernoulli) &&
                                                                                                                     (o != UnivariateDistributionType.Beta) &&
                                                                                                                     (o != UnivariateDistributionType.Binomial) &&
                                                                                                                     (o != UnivariateDistributionType.Cauchy) &&
                                                                                                                     (o != UnivariateDistributionType.ChiSquared) &&
                                                                                                                     (o != UnivariateDistributionType.CompetingRisks) &&
                                                                                                                     (o != UnivariateDistributionType.Empirical) &&
                                                                                                                     (o != UnivariateDistributionType.Geometric) &&
                                                                                                                     (o != UnivariateDistributionType.InverseChiSquared) &&
                                                                                                                     (o != UnivariateDistributionType.InverseGamma) &&
                                                                                                                     (o != UnivariateDistributionType.KappaFour) &&
                                                                                                                     (o != UnivariateDistributionType.KernelDensity) &&
                                                                                                                     (o != UnivariateDistributionType.Mixture) &&
                                                                                                                     (o != UnivariateDistributionType.NoncentralT) &&
                                                                                                                     (o != UnivariateDistributionType.UserDefined) &&
                                                                                                                     (o != UnivariateDistributionType.UniformDiscrete) &&
                                                                                                                     (o != UnivariateDistributionType.Poisson)).ToList();
            }
        }

        /// <summary>
        /// Gets the collection of data points for the minimum value line on the plot.
        /// </summary>
        public ObservableCollection<DataPoint> MinimumLinePoints { get; private set; } = new ObservableCollection<DataPoint>();

        /// <summary>
        /// Gets the collection of data points for the maximum value line on the plot.
        /// </summary>
        public ObservableCollection<DataPoint> MaximumLinePoints { get; private set; } = new ObservableCollection<DataPoint>();

        /// <summary>
        /// Gets the collection of data points for the mean value line on the plot.
        /// </summary>
        public ObservableCollection<DataPoint> MeanLinePoints { get; private set; } = new ObservableCollection<DataPoint>();

        /// <summary>
        /// Gets the collection of data points for the median value line on the plot.
        /// </summary>
        public ObservableCollection<DataPoint> MedianLinePoints { get; private set; } = new ObservableCollection<DataPoint>();

        /// <summary>
        /// Gets the collection of data points for the mode value line on the plot.
        /// </summary>
        public ObservableCollection<DataPoint> ModeLinePoints { get; private set; } = new ObservableCollection<DataPoint>();

        /// <summary>
        /// Gets the collection of area points defining the uncertainty bounds on the plot.
        /// </summary>
        public ObservableCollection<AreaPoint> AreaPoints { get; private set; } = new ObservableCollection<AreaPoint>();

        /// <summary>
        /// Occurs when the user requests to view or modify plot properties.
        /// </summary>
        public event PlotPropertiesRequestedEventHandler PlotPropertiesRequested;

        /// <summary>
        /// Represents a method that handles plot properties requests.
        /// </summary>
        /// <param name="targetPlot">The plot whose properties are being requested.</param>
        /// <param name="openProperties">Whether to open the properties dialog.</param>
        /// <param name="propertyExpander">The property expander to display.</param>
        /// <param name="selectedObject">The selected object in the plot.</param>
        public delegate void PlotPropertiesRequestedEventHandler(Plot targetPlot, bool openProperties, OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject);

        private bool _pastingData = false;
        private bool _isLoaded = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="UncertainOrderedDataSelectorControl"/> class.
        /// </summary>
        public UncertainOrderedDataSelectorControl()
        {

            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.

            DistributionOptions = DefaultDistributionOptions;
            // Bind the combobox
            CurveUncertaintyComboBox.ItemsSource = Distributions;
            CurveUncertaintyComboBox.DisplayMemberPath = "DistributionName";
        }

        /// <summary>
        /// Handles the selection changed event for the curve uncertainty combobox.
        /// Updates the validation grid item source and refreshes the plot.
        /// </summary>
        /// <param name="sender">The combobox that raised the event.</param>
        /// <param name="e">The selection changed event arguments.</param>
        private void CurveUncertaintyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurveUncertaintyComboBox.SelectedIndex != -1)
            {
                ValidationGrid.ItemsSource = ((DistributionDataItem)CurveUncertaintyComboBox.SelectedItem).DistributionRows;
                SelectedUncertainOrderedData = ((DistributionDataItem)CurveUncertaintyComboBox.SelectedItem).Data;
            }
            else
            {
                ValidationGrid.ItemsSource = null;
            }
            // 
            UpdatePlot();
            UpdateGrid();
        }

        /// <summary>
        /// Forces validation on all rows in the data grid.
        /// </summary>
        public void UpdateGrid()
        {
            if (CurveUncertaintyComboBox.SelectedIndex == -1) return;
            foreach (var r in ((DistributionDataItem)CurveUncertaintyComboBox.SelectedItem).DistributionRows)
                ((DistributionRowItem)r).ForceValidation();
        }

        /// <summary>
        /// Updates the plot with current data from the distribution rows.
        /// </summary>
        /// <param name="dataIndex">The index of a specific data point to update, or -1 to update all points.</param>
        public void UpdatePlot(int dataIndex = -1)
        {
            if ((CurveUncertaintyComboBox.SelectedIndex == -1) || (CurveUncertaintyComboBox.SelectedItem.GetType() != typeof(DistributionDataItem)))
            {
                MinimumLinePoints.Clear();
                MaximumLinePoints.Clear();
                MeanLinePoints.Clear();
                MedianLinePoints.Clear();
                ModeLinePoints.Clear();
                AreaPoints.Clear();
                return;
            }

            var rows = ((DistributionDataItem)CurveUncertaintyComboBox.SelectedItem).DistributionRows;
            // 
            DistributionRowItem rowItem;
            if ((dataIndex == -1) || (dataIndex >= rows.Count) || (dataIndex >= MinimumLinePoints.Count))
            {
                MinimumLinePoints.Clear();
                MaximumLinePoints.Clear();
                MeanLinePoints.Clear();
                MedianLinePoints.Clear();
                ModeLinePoints.Clear();
                AreaPoints.Clear();
                for (int i = 0; i < rows.Count; i++)
                {
                    if (rows[i].GetType() != typeof(DistributionRowItem)) continue;
                    rowItem = (DistributionRowItem)rows[i];
                    // 
                    MinimumLinePoints.Add(new DataPoint(rowItem.X, rowItem.Minimum));
                    MaximumLinePoints.Add(new DataPoint(rowItem.X, rowItem.Maximum));
                    MeanLinePoints.Add(new DataPoint(rowItem.X, rowItem.Mean));
                    MedianLinePoints.Add(new DataPoint(rowItem.X, rowItem.Distribution.Median));
                    ModeLinePoints.Add(new DataPoint(rowItem.X, rowItem.Distribution.Mode));
                    AreaPoints.Add(new AreaPoint(MinimumLinePoints[i], MaximumLinePoints[i]));
                }
            }
            else
            {
                rowItem = (DistributionRowItem)rows[dataIndex];
                MinimumLinePoints[dataIndex] = new DataPoint(rowItem.X, rowItem.Minimum);
                MaximumLinePoints[dataIndex] = new DataPoint(rowItem.X, rowItem.Maximum);
                MeanLinePoints[dataIndex] = new DataPoint(rowItem.X, rowItem.Mean);
                MedianLinePoints[dataIndex] = new DataPoint(rowItem.X, rowItem.Distribution.Median);
                ModeLinePoints[dataIndex] = new DataPoint(rowItem.X, rowItem.Distribution.Mode);
                AreaPoints[dataIndex] = new AreaPoint(MinimumLinePoints[dataIndex], MaximumLinePoints[dataIndex]);
                Plot.InvalidatePlot(true);
            }
        }

        /// <summary>
        /// Handles the column header click event to select all cells in that column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void DataGridColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader columnHeader = sender as DataGridColumnHeader;
            if (columnHeader == null) return;
            // 
            ValidationGrid.SelectedCells.Clear();
            foreach (var item in ValidationGrid.Items)
                ValidationGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
        }

        /// <summary>
        /// Handles the properties called event from the OxyPlot toolbar.
        /// Raises the PlotPropertiesRequested event to notify subscribers.
        /// </summary>
        /// <param name="targetPlot">The plot requesting properties.</param>
        /// <param name="openProperties">Whether to open the properties dialog.</param>
        /// <param name="propertyExpander">The property expander to display.</param>
        /// <param name="selectedObject">The currently selected object.</param>
        private void OxyplotToolbar_PropertiesCalled(Plot targetPlot, bool openProperties, OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject)
        {
            PlotPropertiesRequested?.Invoke(targetPlot, openProperties, propertyExpander, selectedObject);
        }

        /// <summary>
        /// Handles the auto generated columns event from the validation grid.
        /// Creates merged column header definitions based on the generated columns.
        /// </summary>
        /// <param name="sender">The data grid that raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ValidationGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            MergedColumnHeaderGrid.ColumnDefinitions.Clear();
            var cd = new ColumnDefinition();
            cd.SetBinding(ColumnDefinition.WidthProperty, new Binding("RowHeaderActualWidth") { ElementName = "ValidationGrid" });
            MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
            foreach (var c in ValidationGrid.Columns)
            {
                cd = new ColumnDefinition();
                cd.SetBinding(ColumnDefinition.WidthProperty, new Binding("ActualWidth") { Source = c });
                MergedColumnHeaderGrid.ColumnDefinitions.Add(cd);
            }
        }

        /// <summary>
        /// Handles the preview add rows event from the validation grid.
        /// Creates new uncertain ordinates and refreshes the display.
        /// </summary>
        /// <param name="startRowIndex">The starting index for the new rows.</param>
        /// <param name="nRows">The number of rows to add.</param>
        /// <param name="cancelAddRows">Reference parameter set to true to handle row addition manually.</param>
        private void ValidationGrid_PreviewAddRows(int startRowIndex, int nRows, ref bool cancelAddRows)
        {
            cancelAddRows = true;
            // 
            if (CurveUncertaintyComboBox.SelectedIndex == -1) return;
            // 
            DistributionDataItem selectedItem = (DistributionDataItem)CurveUncertaintyComboBox.SelectedItem;
            var type = selectedItem.Data.Distribution;
            UnivariateDistributionBase distribution;
            if (selectedItem.Data.Count > 0)
                distribution = selectedItem.Data[0].Y.Clone();
            else if (!UnivariateDistributionFactory.TryCreateDistribution(type, out distribution) || distribution is null)
                return;
            for (int i = startRowIndex; i < startRowIndex + nRows; i++)
                selectedItem.Data.Insert(i, new UncertainOrdinate(0d, distribution.Clone()));
            // Refresh the view
            selectedItem.Refresh(); // MinimumX, MaximumX, MinimumY, MaximumY, IsStrictX, IsStrictY, OrderX, OrderY)
            ValidationGrid.Items.Refresh();
            UpdateGrid();
            UpdatePlot();
            if (_pastingData == false)
                GetBindingExpression(SelectedUncertainOrderedDataProperty).UpdateSource();
        }

        /// <summary>
        /// Handles the rows added event from the validation grid.
        /// Updates the grid validation and plot display.
        /// </summary>
        /// <param name="startrow">The starting index of the added rows.</param>
        /// <param name="numrows">The number of rows that were added.</param>
        private void ValidationGrid_RowsAdded(int startrow, int numrows)
        {
            UpdateGrid();
            UpdatePlot();
            if (_pastingData == false)
                GetBindingExpression(SelectedUncertainOrderedDataProperty).UpdateSource();
        }

        /// <summary>
        /// Handles the rows deleted event from the validation grid.
        /// Removes the deleted ordinates and updates the display.
        /// </summary>
        /// <param name="rowindices">The list of row indices that were deleted.</param>
        private void ValidationGrid_RowsDeleted(List<int> rowindices)
        {
            if (CurveUncertaintyComboBox.SelectedIndex == -1) return;
            DistributionDataItem selectedItem = (DistributionDataItem)CurveUncertaintyComboBox.SelectedItem;
            // 
            // Refresh the view
            rowindices.Sort();
            for (int i = rowindices.Count - 1; i >= 0; i -= 1)
                selectedItem.Data.RemoveAt(rowindices[i]);
            // 
            UpdateGrid();
            UpdatePlot();
            GetBindingExpression(SelectedUncertainOrderedDataProperty).UpdateSource();
        }

        /// <summary>
        /// Handles the data pasted event from the validation grid.
        /// Resets the pasting flag, updates display, and restores cursor.
        /// </summary>
        private void ValidationGrid_DataPasted()
        {
            _pastingData = false;
            UpdateGrid();
            UpdatePlot();
            GetBindingExpression(SelectedUncertainOrderedDataProperty).UpdateSource();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Handles the preview paste data event from the validation grid.
        /// Sets the pasting flag and displays a wait cursor.
        /// </summary>
        /// <param name="clipboardData">The clipboard data being pasted.</param>
        /// <param name="cancelPaste">Reference parameter to cancel the paste operation if needed.</param>
        private void ValidationGrid_PreviewPasteData(string[][] clipboardData, ref bool cancelPaste)
        {
            _pastingData = true;
            Mouse.OverrideCursor = Cursors.Wait;
        }

        /// <summary>
        /// Handles the control loaded event.
        /// Initializes the validation grid with dummy data to trigger column generation.
        /// </summary>
        /// <param name="sender">The control that raised the event.</param>
        /// <param name="e">The routed event arguments.</param>
        private void UncertainOrderedDataSelectorControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded == false)
            {
                var dummyCollection = new ObservableCollection<object>();
                dummyCollection.Add(new DistributionRowItem(0d, new Deterministic(0d), dummyCollection, 0d, 10d, 0d, 10d, false, false, OrderX, OrderY));
                ValidationGrid.ItemsSource = dummyCollection;
                dummyCollection.Clear();
            }

            _isLoaded = true;
        }
    }

    /// <summary>
    /// Represents a point defining an area on a plot with X and Y coordinate bounds.
    /// Used to visualize uncertainty bounds between minimum and maximum distribution values.
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
    public class AreaPoint
    {
        /// <summary>
        /// Gets or sets the X coordinate of the first point.
        /// </summary>
        public double X1 { get; set; }

        /// <summary>
        /// Gets or sets the X coordinate of the second point.
        /// </summary>
        public double X2 { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the first point (typically the minimum).
        /// </summary>
        public double Y1 { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate of the second point (typically the maximum).
        /// </summary>
        public double Y2 { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AreaPoint"/> class from two data points.
        /// </summary>
        /// <param name="p1">The first data point defining the area bounds.</param>
        /// <param name="p2">The second data point defining the area bounds.</param>
        public AreaPoint(DataPoint p1, DataPoint p2)
        {
            X1 = p1.X;
            X2 = p2.X;
            Y1 = p1.Y;
            Y2 = p2.Y;
        }
    }
}

