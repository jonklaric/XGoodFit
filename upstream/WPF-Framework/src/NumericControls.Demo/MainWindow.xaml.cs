using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using GenericControls;
using Numerics.Data;
using Numerics.Distributions;
using Numerics.Sampling;
using Themes;

namespace NumericControls.Demo
{
    /// <summary>
    /// Main window for the NumericControls demonstration application.
    /// Demonstrates the usage of various NumericControls with theme support.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This demo showcases:
    /// <list type="bullet">
    /// <item><description>Distribution Selector controls for univariate probability distributions</description></item>
    /// <item><description>Uncertain Curve Editor for ordered paired data with uncertainty</description></item>
    /// <item><description>Curve Editor for standard ordered paired data</description></item>
    /// <item><description>Time Series Table for temporal data management</description></item>
    /// <item><description>Bin Definition control for stratification options</description></item>
    /// <item><description>Bivariate CDF control for two-dimensional empirical distributions</description></item>
    /// <item><description>Runtime theme switching (Light, Blue, Dark)</description></item>
    /// </list>
    /// </para>
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
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        private readonly List<UnivariateDistributionBase> _distOptions = new List<UnivariateDistributionBase>(new[] {
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Deterministic),
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Normal),
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.LnNormal),
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.TruncatedNormal),
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Triangular),
            UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Pert)
        });

        /// <summary>
        /// Gets a list of available univariate distribution options for the distribution selector control.
        /// </summary>
        /// <value>
        /// A list containing instances of Deterministic, Normal, LnNormal, TruncatedNormal, Triangular, and Pert distributions.
        /// </value>
        public List<UnivariateDistributionBase> DistOptions => _distOptions;

        private readonly List<UnivariateDistributionType> _tableDistOptions = new List<UnivariateDistributionType>(new[] {
            UnivariateDistributionType.Deterministic,
            UnivariateDistributionType.Normal,
            UnivariateDistributionType.Pert,
            UnivariateDistributionType.Triangular,
            UnivariateDistributionType.LnNormal,
            UnivariateDistributionType.TruncatedNormal
        });

        /// <summary>
        /// Gets a list of univariate distribution types for use in table-based distribution selection.
        /// </summary>
        /// <value>
        /// A list of distribution type enumerations including Deterministic, Normal, Pert, Triangular, LnNormal, and TruncatedNormal.
        /// </value>
        public List<UnivariateDistributionType> TableDistOptions => _tableDistOptions;

        private UnivariateDistributionBase _selectedDistribution;

        /// <summary>
        /// Gets or sets the currently selected univariate distribution.
        /// </summary>
        /// <value>
        /// The selected distribution instance. Initialized to a Normal distribution with mean 100 and standard deviation 15.
        /// </value>
        public UnivariateDistributionBase SelectedDistribution
        {
            get
            {
                return _selectedDistribution;
            }
            set
            {
                if (_selectedDistribution != value)
                {
                    _selectedDistribution = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDistribution)));

                    DistSelectorWithFitting.SampleData = _selectedDistribution.GenerateRandomValues(1000);
                }
            }
        }

        private UnivariateDistributionBase _popUpDistribution;

        /// <summary>
        /// Gets or sets the currently selected univariate distribution for the pop up controls.
        /// </summary>
        /// <value>
        /// The selected distribution instance. Initialized to a Normal distribution with mean 100 and standard deviation 15.
        /// </value>
        public UnivariateDistributionBase PopUpDistribution
        {
            get
            {
                return _popUpDistribution;
            }
            set
            {
                _popUpDistribution = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PopUpDistribution)));              
            }
        }

        private UncertainOrderedPairedData _uncertainCurve;

        /// <summary>
        /// Gets or sets the uncertain curve data for the uncertain curve editor control.
        /// </summary>
        /// <value>
        /// An ordered paired data set with uncertainty quantification for each ordinate.
        /// </value>
        public UncertainOrderedPairedData SelectedUncertainCurve
        {
            get
            {
                return _uncertainCurve;
            }
            set
            {
                if (_uncertainCurve != value)
                {
                    _uncertainCurve = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedUncertainCurve)));
                }
            }
        }

        private OrderedPairedData _curve;

        /// <summary>
        /// Gets or sets the standard curve data for the curve editor control.
        /// </summary>
        /// <value>
        /// An ordered paired data set containing X-Y coordinate pairs.
        /// </value>
        public OrderedPairedData SelectedCurve
        {
            get
            {
                return _curve;
            }
            set
            {
                if (_curve != value)
                {
                    _curve = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedCurve)));
                }
            }
        }

        private ProbabilityOrdinates _probabilityOrdinates = new ProbabilityOrdinates();

        /// <summary>
        /// Gets or sets the probability ordinates.
        /// </summary>
        /// <value>
        /// An ordered data set containing probability ordinates.
        /// </value>
        public ProbabilityOrdinates ProbabilityOrdinates
        {
            get
            {
                return _probabilityOrdinates;
            }
            set
            {
                if (_probabilityOrdinates != value)
                {
                    _probabilityOrdinates = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProbabilityOrdinates)));
                }
            }
        }

        private List<StratificationOptions> _stratificationOptionsCollection = new List<StratificationOptions>();

        /// <summary>
        /// Gets or sets the collection of stratification options for bin definition control.
        /// </summary>
        /// <value>
        /// A list of stratification options defining bin boundaries and counts for data stratification.
        /// </value>
        public List<StratificationOptions> StratificationOptionsCollection
        {
            get
            {
                return _stratificationOptionsCollection;
            }
            set
            {
                _stratificationOptionsCollection = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StratificationOptionsCollection)));
            }
        }

        private BivariateEmpirical _bivariateCDF = new BivariateEmpirical();

        /// <summary>
        /// Gets or sets the bivariate empirical cumulative distribution function data.
        /// </summary>
        /// <value>
        /// A two-dimensional empirical distribution with X values, Y values, and associated cumulative probabilities.
        /// </value>
        public BivariateEmpirical BivariateCDF
        {
            get
            {
                return _bivariateCDF;
            }
            set
            {
                _bivariateCDF = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BivariateCDF)));
            }
        }

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// Sets up all demonstration data including distributions, curves, time series, and stratification options.
        /// </summary>
        public MainWindow()
        {

            // This call is required by the designer.
            this.InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            DistSelectorWithFitting.Distributions = DistOptions;
            SelectedDistribution = new Normal(100d, 15d);
            PopUpDistribution = new Normal(100d, 15d);

            //myOtherControl.SampleData = SelectedDistribution.GenerateRandomValues(1000); //Enumerable.Repeat(0d, 1000).ToArray(); //
            //myOtherControl.SampleData = (new double[] { 108, 108, 114, 125, 123, 123, 96, 130, 134, 114, 129, 121, 129, 123, 125, 110, 119, 99, 113, 112, 108, 138, 131, 126, 115, 116, 109, 128, 107, 137, 129, 109, 113, 106, 133, 113, 117, 112, 126, 123, 123, 136, 107, 103, 123, 107, 119, 147, 125, 130, 124, 119, 110, 113, 103, 97, 119, 92, 103, 125, 114, 110, 110, 110, 116, 103, 123, 111, 116, 101, 124, 108, 133, 120, 108, 122, 113, 112, 103, 105, 94, 108, 123, 116, 139, 121, 109, 106, 131, 130, 125, 134, 111, 107, 103, 100, 116, 128, 94, 113, 112, 133, 109, 120, 112, 122, 114, 125, 133, 143, 127, 93, 123, 126, 111, 114, 114, 105, 121, 122, 116, 107, 111, 124, 120, 116, 95, 124, 109, 106, 108, 148, 131, 108, 114, 120, 123, 112, 103, 117, 143, 103, 120, 119, 117, 112, 104, 94, 125, 104, 125, 122, 101, 108, 116, 111, 114, 118, 109, 104, 121, 114, 126, 103, 128, 111, 114, 103, 114, 143, 127, 148, 103, 121, 123, 118, 117, 111, 95, 120, 108, 112, 137, 136, 86, 125, 128, 131, 85, 113, 102, 106, 134, 118, 127, 107, 94, 121, 123, 119, 112, 148, 113, 90, 104, 131, 128, 131, 95, 117, 117, 111, 115, 124, 94, 124, 129, 102, 149, 123, 138, 120, 117, 104, 113, 116, 147, 109, 121, 108, 137, 140, 107, 114, 112, 143, 126, 125, 135, 107, 103, 117, 116, 121, 114, 105, 95, 101, 105, 110, 112, 112, 112, 124, 130, 114, 111, 122, 108, 138, 125, 106, 122, 109, 106, 109, 129, 131, 115, 142, 123, 91, 85, 114, 122, 100, 128, 128, 134, 98, 104, 105, 99, 114, 121, 112, 101, 108, 124, 114, 133, 107, 123, 131, 116, 127, 135, 115, 135, 109, 110, 128, 111, 105, 122, 123, 117, 104, 95, 135, 109, 119, 134, 128, 143, 104, 124, 134, 103, 109, 115, 148, 105, 105, 122, 105, 134, 147, 93, 107, 109, 133, 132, 132, 96, 120, 136, 100, 100, 126, 117, 119, 116, 121, 120, 135, 116, 121, 125, 108, 136, 118, 116, 82, 110, 118, 122, 121, 127, 137, 112, 109, 106, 124, 98, 119, 123, 114, 112, 125, 101, 146, 78, 121, 123, 103, 118, 120, 122, 96, 105, 114, 113, 122, 133, 100, 127, 120, 104, 141, 127, 114, 122, 122, 111, 119, 109, 119, 138, 110, 107, 111, 104, 122, 106, 114, 104, 101, 138, 118, 119, 96, 102, 106, 131, 95, 112, 121, 123, 103, 107, 112, 125, 121, 102, 106, 138, 115, 96, 115, 132, 103, 100, 139, 124, 112, 119, 132, 90, 111, 123, 99, 106, 104, 95, 144, 137, 112, 128, 120, 125, 117, 109, 98, 100, 98, 85, 135, 118, 114, 92, 133, 102, 111, 121, 120, 109, 115, 94, 127, 139, 110, 105, 139, 136, 107, 111, 102, 105, 143, 127, 118, 101, 122, 119, 109, 116, 134, 135, 114, 111, 112, 147, 130, 128, 127, 131, 102, 117, 125, 107, 133, 95, 120, 102, 124, 82, 108, 108, 142, 125, 108, 145, 134, 122, 114, 133, 97, 129, 141, 102, 120, 134, 126, 111, 122, 113, 111, 131, 125, 105, 115, 105, 128, 113, 107, 123, 134, 106, 110, 132, 109, 114, 110, 121, 119, 128, 121, 117, 131, 102, 124, 119, 129, 115, 128, 107, 108, 141, 113, 120, 110, 109, 113, 121, 123, 124, 101, 125, 135, 114, 118, 102, 112, 132, 115, 111, 106, 106, 121, 142, 103, 120, 126, 108, 133, 112, 113, 129, 133, 118, 100, 115, 121, 113, 106, 117, 124, 94, 94, 119, 117, 111, 112, 109, 130, 118, 114, 127, 127, 119, 122, 113, 102, 85, 117, 125, 115, 127, 136, 121, 125, 126, 142, 100, 90, 105, 111, 115, 106, 102, 128, 122, 132, 99, 136, 124, 126, 137, 107, 132, 124, 118, 121, 112, 131, 107, 120, 111, 107, 119, 106, 126, 112, 121, 132, 127, 102, 91, 120, 133, 110, 107, 119, 103, 115, 116, 105, 120, 114, 108, 123, 109, 122, 121, 99, 88, 114, 126, 114, 106, 111, 127, 121, 132, 120, 128, 95, 93, 136, 121, 124, 124, 98, 129, 109, 126, 103, 105, 130, 135, 90, 119, 126, 106, 108, 119, 132, 129, 104, 106, 108, 135, 111, 117, 123, 100, 119, 130, 123, 120, 126, 140, 108, 107, 114, 106, 114, 118, 126, 109, 111, 117, 98, 120, 100, 130, 134, 129, 95, 131, 118, 121, 107, 111, 119, 117, 119, 113, 119, 131, 126, 110, 108, 109, 98, 106, 116, 108, 101, 118, 109, 123, 122, 97, 130, 112, 113, 111, 95, 111, 112, 108, 102, 93, 116, 116, 111, 111, 133, 93, 104, 132, 138, 107, 120, 108, 108, 103, 108, 114, 113, 106, 106, 104, 130, 112, 132, 102, 126, 111, 118, 104, 102, 100, 95, 112, 113, 114, 114, 112, 128, 107, 100, 136, 115, 112, 110, 121, 118, 121, 112, 115, 118, 92, 113, 92, 133, 94, 125, 123, 138, 116, 108, 144, 136, 134, 122, 115, 97, 119, 119, 127, 128, 123, 132, 91, 121, 112, 111, 112, 107, 139, 102, 112, 129, 113, 130, 104, 143, 102, 107, 136, 120, 126, 111, 137, 120, 124, 118, 108, 111, 123, 118, 127, 105, 102, 115, 94, 125, 125, 99, 111, 137, 139, 113, 109, 118, 127, 133, 97, 122, 121, 116, 87, 131, 120, 101, 111, 100, 99, 108, 108, 138, 117, 117, 114, 105, 113, 115, 110, 106, 111, 139, 118, 119, 129, 119, 98, 125, 113, 125, 156, 120, 135, 96, 127, 135, 101, 108, 96, 137, 107, 129, 123, 123, 99, 133, 122, 111, 124, 127, 101, 121, 100, 116, 101, 89, 107, 111, 111, 112, 113, 106, 102, 125, 98, 138, 119, 132, 100, 112, 103, 116, 138, 101, 132, 97, 123, 121, 142, 135, 99, 114, 114, 143, 135, 124, 126, 114, 138, 122, 104, 129, 123, 122, 116, 104, 110, 131, 131, 120, 134, 116, 109, 104, 131, 97, 125, 144 });

            // 
            var uncertainOrdinates = new UncertainOrdinate[81];
            var ordinates = new Ordinate[81];
            uncertainOrdinates[0] = new UncertainOrdinate(0.25d, new Deterministic(Math.Sqrt(1112.412d / 0.25d)));
            ordinates[0] = new Ordinate(0.25d, Math.Sqrt(1112.412d / 0.25d));
            for (int i = 1, loopTo = uncertainOrdinates.Length - 1; i <= loopTo; i++)
            {
                uncertainOrdinates[i] = new UncertainOrdinate(i * 0.5d, new Deterministic(Math.Max(Math.Sqrt(1112.412d / (i * 0.5d)), 5.4d)));
                ordinates[i] = new Ordinate(i * 0.5d, Math.Max(Math.Sqrt(1112.412d / (i * 0.5d)), 5.4d));
            }
            // 
            SelectedUncertainCurve = new UncertainOrderedPairedData(uncertainOrdinates, false, SortOrder.Ascending, false, SortOrder.Descending, UnivariateDistributionType.Deterministic);
            SelectedCurve = new OrderedPairedData(ordinates, false, SortOrder.Ascending, false, SortOrder.Descending);
            StratificationOptionsCollection = new List<StratificationOptions>(new[] { new StratificationOptions(0d, 10d, 5), new StratificationOptions(10d, 15d, 3), new StratificationOptions(15d, 23d, 5), new StratificationOptions(23d, 63d, 100) });
            // 
            var xVals = new double[] { 0d, 1d, 2d, 3d, 4d, 5d, 6d };
            var yVals = new double[] { .0d, 0.1d, 0.2d, 0.3d, 0.4d, 0.5d, 0.6d };
            var pVals = new double[(xVals.Count()), (yVals.Count())];
            double cumProb = 0d;
            for (int i = 0, loopTo1 = xVals.Count() - 1; i <= loopTo1; i++)
            {
                for (int j = 0, loopTo2 = yVals.Count() - 1; j <= loopTo2; j++)
                {
                    pVals[i, j] = cumProb;
                    cumProb += 1d / (xVals.Count() * yVals.Count());
                }
            }

            BivariateCDF = new BivariateEmpirical(xVals, yVals, pVals);



            // Time Series - defer the async-download-triggering selection until the window's
            // visual tree is fully loaded. Previously the assignment here fired
            // USGSItem_Selected (async void) synchronously during the constructor, which
            // started a network download against a TimeSeriesTableControl that hadn't yet
            // completed attachment to the visual tree.
            Loaded += (s, e) => { USGSItem.IsSelected = true; };


            //if( File.Exists(mapSettingsXMLFile) Then File.Delete(mapSettingsXMLFile)
            //var xmlSettings = new XmlWriterSettings();
            //xmlSettings.Indent = true;
            //using (XmlWriter w = XmlWriter.Create(mapSettingsXMLFile, xmlSettings))
            //{
            //    TimeSeriesTableControl.Series.ToXElement().WriteTo(w);
            //}



        }

        /// <summary>
        /// Handles the Click event of the TestButton control.
        /// Demonstrates dynamic modification of uncertain curve data.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedUncertainCurve[0] = new UncertainOrdinate(SelectedUncertainCurve[0].X + 1d, SelectedUncertainCurve[0].Y.Clone());
            SelectedUncertainCurve.Add(new UncertainOrdinate(0d, SelectedUncertainCurve[0].Y.Clone()));
        }

        /// <summary>
        /// Handles the Selected event for the USGS time series menu item.
        /// Downloads USGS daily discharge time series data. 
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void USGSItem_Selected(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await TimeSeriesDownload.FromUSGS("01134500", TimeSeriesDownload.TimeSeriesType.DailyDischarge);
                TimeSeriesTableControl.Series = result.TimeSeries;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"USGS download failed: {ex.Message}");
                System.Windows.MessageBox.Show($"Failed to download USGS data: {ex.Message}", "Download Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles the Selected event for the irregular time series menu item.
        /// Creates and displays a sample irregular time series with two data points.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void IrregularItem_Selected(object sender, RoutedEventArgs e)
        {
            var s = new TimeSeries(TimeInterval.Irregular);
            s.Add(new SeriesOrdinate<DateTime, double>(new DateTime(1980, 7, 30), 0));
            s.Add(new SeriesOrdinate<DateTime, double>(DateTime.Now, DateTime.Now.Year - 1980));
            TimeSeriesTableControl.Series = s;
        }

        /// <summary>
        /// Handles the Selected event for the daily time series menu item.
        /// Creates and displays a daily time series spanning one month.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DailyItem_Selected(object sender, RoutedEventArgs e)
        {
            TimeSeriesTableControl.Series = new TimeSeries(TimeInterval.OneDay, new DateTime(1980, 7, 30), new DateTime(1980, 8, 30), 10);
        }

        /// <summary>
        /// Handles the Selected event for the hourly time series menu item.
        /// Creates and displays an hourly time series spanning two days.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void HourlyItem_Selected(object sender, RoutedEventArgs e)
        {
            TimeSeriesTableControl.Series = new TimeSeries(TimeInterval.OneHour, new DateTime(1980, 7, 30), new DateTime(1980, 8, 1), 15);
        }

        /// <summary>
        /// Handles the Selected event for the minutes time series menu item.
        /// Creates and displays a per-minute time series spanning one day.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MinutesItem_Selected(object sender, RoutedEventArgs e)
        {
            TimeSeriesTableControl.Series = new TimeSeries(TimeInterval.OneMinute, new DateTime(1980, 7, 30), new DateTime(1980, 7, 30, 23, 59, 0), 10);
        }

        /// <summary>
        /// Handles theme radio button selection changes.
        /// </summary>
        /// <param name="sender">The radio button that was checked.</param>
        /// <param name="e">Event arguments.</param>
        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                Theme theme = Theme.Light;

                if (radioButton == LightThemeRadio)
                    theme = Theme.Light;
                else if (radioButton == BlueThemeRadio)
                    theme = Theme.Blue;
                else if (radioButton == DarkThemeRadio)
                    theme = Theme.Dark;

                ThemeService.Instance.SetTheme(theme);
            }
        }
    }
}
