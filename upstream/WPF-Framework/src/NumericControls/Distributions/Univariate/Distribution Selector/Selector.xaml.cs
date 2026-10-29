#nullable enable
using GenericControls;
using Numerics.Data.Statistics;
using Numerics.Distributions;
using Numerics.Sampling;
using OxyPlot.Wpf;
using OxyPlotControls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Themes;

namespace NumericControls.Distributions.Univariate
{
    /// <summary>
    /// A comprehensive user control for selecting, configuring, and visualizing univariate probability distributions.
    /// Provides distribution selection, parameter editing, PDF plotting, and statistical comparison with sample data.
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
    public partial class Selector : UserControl, INotifyPropertyChanged
    {
        private Theme? _lastAppliedTheme;

        private HistogramSeries _histogramSeries = new HistogramSeries()
        {
            Name = "Histogram",
            Title = "Input Data Histogram",
            FillColor = Color.FromArgb(225, 184, 243, 205),
            StrokeColor = Color.FromArgb(255, 53, 59, 122),
            StrokeThickness = 1,
            RenderInLegend = false
        };
        private UnivariateDistributionBase? _selectedDistribution = null;
        private ObservableCollection<Parameter> ParameterList { get; set; } = new ObservableCollection<Parameter>();
        private List<SummaryStatistic> SummaryStatisticsList { get; set; } = new List<SummaryStatistic>();

        private bool _settingDistributionOptions = false;
        private bool _propertySetting = false;
        private List<UnivariateDistributionBase> _distributions = DefaultDistributions;

        /// <summary>
        /// Construct a distribution selector control using the default, full list of continuous distribution types.
        /// </summary>
        public Selector()
        {

            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.

            // Create summary statistics list
            SummaryStatisticsList.Add(new SummaryStatistic("Minimum", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Maximum", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Mean", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Mode", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Std Dev", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Skewness", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("Kurtosis", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("5%", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("25%", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("50%", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("75%", "", ""));
            SummaryStatisticsList.Add(new SummaryStatistic("95%", "", ""));

            // Bind the summary and parameter data tables
            ParametersTable.ItemsSource = ParameterList;
            SummaryTable.ItemsSource = SummaryStatisticsList;
            DistributionCombobox.ItemsSource = _distributions;

            // OxyPlot
            Plot.ActualController.UnbindAll();

            Loaded += Selector_Loaded;
            Unloaded += Selector_Unloaded;
        }

        /// <summary>
        /// Subscribes to theme changes and applies the current plot theme.
        /// </summary>
        /// <param name="sender">The selector control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Selector_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeService.Instance.ThemeChanged -= OnAppThemeChanged;
            ThemeService.Instance.ThemeChanged += OnAppThemeChanged;

            var currentTheme = ThemeService.Instance.CurrentTheme;
            if (Plot != null && _lastAppliedTheme != currentTheme)
            {
                _lastAppliedTheme = currentTheme;
                var theme = OxyPlotThemeManager.GetThemeFor(currentTheme);
                OxyPlotThemeManager.ApplyTheme(Plot, theme);
                ApplyAxisVisibilityOverrides();
            }
        }

        /// <summary>
        /// Removes the theme-change subscription when the selector unloads.
        /// </summary>
        /// <param name="sender">The selector control that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Selector_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeService.Instance.ThemeChanged -= OnAppThemeChanged;
        }

        /// <summary>
        /// Applies the new theme to the selector plot.
        /// </summary>
        /// <param name="sender">The theme service that raised the event.</param>
        /// <param name="e">The theme-change details.</param>
        private void OnAppThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (Plot == null) return;
            _lastAppliedTheme = e.NewTheme;
            var theme = OxyPlotThemeManager.GetThemeFor(e.NewTheme);
            OxyPlotThemeManager.ApplyTheme(Plot, theme);
            ApplyAxisVisibilityOverrides();
        }

        /// <summary>
        /// Applies axis title, label, and tick visibility settings after theming.
        /// </summary>
        private void ApplyAxisVisibilityOverrides()
        {
            var transparentColor = Color.FromArgb(0, 0, 0, 0);

            if (!ShowAxisTitle)
            {
                Yaxis.TitleColor = transparentColor;
                Xaxis.TitleColor = transparentColor;
            }
            if (!ShowAxisLabel)
            {
                Yaxis.TextColor = transparentColor;
                Xaxis.TextColor = transparentColor;
            }
            Plot.InvalidatePlot(false);
        }

        /// <summary>
        /// Gets or sets the currently selected univariate distribution.
        /// </summary>
        public UnivariateDistributionBase? SelectedDistribution
        {
            get => _selectedDistribution;
            set
            {
                if (_selectedDistribution is null && value is null) { return; }

                if (_selectedDistribution is null || _selectedDistribution.Equals(value) == false)
                {
                    _selectedDistribution = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedDistribution)));
                }

                if (_distributionChanging == false) // If false then it is changing from an external source.
                {
                    _propertySetting = true;
                    bool isContained = false;
                    if (_selectedDistribution is not null)
                    {
                        for (int i = 0; i < _distributions.Count; i++)
                        {
                            if (_distributions[i].Type == _selectedDistribution.Type)
                            {
                                isContained = true;
                                if (_distributions[i].Equals(_selectedDistribution) == false)
                                {
                                    _distributions[i] = _selectedDistribution.Clone();
                                    DistributionCombobox.Items.Refresh();
                                }
                                //
                                DistributionCombobox.SelectedIndex = i;
                                UpdatePDFPlot();
                                UpdateDistributionStats();
                                break;
                            }
                        }
                    }
                    //
                    if (isContained == false) { DistributionCombobox.SelectedIndex = -1; }
                    _propertySetting = false;
                }
            }
        }

        /// <summary>
        /// Dependency property for the control background color.
        /// </summary>
        public static readonly DependencyProperty BackgroundColorProperty = DependencyProperty.Register(nameof(BackgroundColor), typeof(Brush), typeof(Selector), new FrameworkPropertyMetadata(new SolidColorBrush(Colors.White)));

        /// <summary>
        /// Get and set the distribution selector background color.
        /// </summary>
        public Brush BackgroundColor
        {
            get => (Brush)GetValue(BackgroundColorProperty);
            set => SetValue(BackgroundColorProperty, value);
        }

        /// <summary>
        /// Dependency property to show the plot axis title.
        /// </summary>
        public static readonly DependencyProperty ShowAxisProperty = DependencyProperty.Register(nameof(ShowAxis), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Get and set the distribution selector plot axis title visibility.
        /// </summary>
        public bool ShowAxis
        {
            get => (bool)GetValue(ShowAxisProperty);
            set => SetValue(ShowAxisProperty, value);
        }

        /// <summary>
        /// Dependency property to show the plot axis title.
        /// </summary>
        public static readonly DependencyProperty ShowAxisTitleProperty = DependencyProperty.Register(nameof(ShowAxisTitle), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(true, OnShowAxisPropertyChanged));

        /// <summary>
        /// Get and set the distribution selector plot axis title visibility.
        /// </summary>
        public bool ShowAxisTitle
        {
            get => (bool)GetValue(ShowAxisTitleProperty);
            set => SetValue(ShowAxisTitleProperty, value);
        }

        /// <summary>
        /// Dependency property to show the plot label title.
        /// </summary>
        public static readonly DependencyProperty ShowAxisLabelProperty = DependencyProperty.Register(nameof(ShowAxisLabel), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(false, OnShowAxisPropertyChanged));

        /// <summary>
        /// Get and set the distribution selector plot axis label visibility.
        /// </summary>
        public bool ShowAxisLabel
        {
            get => (bool)GetValue(ShowAxisLabelProperty);
            set => SetValue(ShowAxisLabelProperty, value);
        }

        /// <summary>
        /// Reapplies axis visibility when an axis visibility dependency property changes.
        /// </summary>
        /// <param name="d">The selector control whose property changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void OnShowAxisPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Selector control && control._lastAppliedTheme != null)
                control.ApplyAxisVisibilityOverrides();
        }

        private bool _showTickLines = true;

        /// <summary>
        /// Gets or sets a value indicating whether tick lines are shown on the plot axes.
        /// </summary>
        public bool ShowTickLines
        {
            get => _showTickLines;
            set
            {
                _showTickLines = value;
                if (value == true)
                {
                    Xaxis.TickStyle = OxyPlot.Axes.TickStyle.Outside;
                    Yaxis.TickStyle = OxyPlot.Axes.TickStyle.Outside;
                }
                else
                {
                    Xaxis.TickStyle = OxyPlot.Axes.TickStyle.None;
                    Yaxis.TickStyle = OxyPlot.Axes.TickStyle.None;
                }
            }
        }

        /// <summary>
        /// Dependency property for showing the distribution pdf plot.
        /// </summary>
        public static readonly DependencyProperty ShowPlotProperty = DependencyProperty.Register(nameof(ShowPlot), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets and sets the plot visibility.
        /// </summary>
        public bool ShowPlot
        {
            get => (bool)GetValue(ShowPlotProperty);
            set => SetValue(ShowPlotProperty, value);
        }

        /// <summary>
        /// Dependency property for expanding the distribution pdf plot.
        /// </summary>
        public static readonly DependencyProperty ExpandPlotProperty = DependencyProperty.Register(nameof(ExpandPlot), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets and sets the distribution options.
        /// </summary>
        public bool ExpandPlot
        {
            get => (bool)GetValue(ExpandPlotProperty);
            set => SetValue(ExpandPlotProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(nameof(ExpanderStyle), typeof(Style), typeof(Selector), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style applied to the expander control.
        /// </summary>
        public Style ExpanderStyle
        {
            get { return (Style)GetValue(ExpanderStyleProperty); }
            set { SetValue(ExpanderStyleProperty, value); }
        }

        /// <summary>
        /// Dependency property for showing the distribution summary statistics.
        /// </summary>
        public static readonly DependencyProperty ShowStatisticsProperty = DependencyProperty.Register(nameof(ShowStatistics), typeof(bool), typeof(Selector), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets and sets the distribution options.
        /// </summary>
        public bool ShowStatistics
        {
            get => (bool)GetValue(ShowStatisticsProperty);
            set => SetValue(ShowStatisticsProperty, value);
        }

        /// <summary>
        /// Dependency property for showing the distribution title.
        /// </summary>
        public static readonly DependencyProperty DistributionTitleProperty = DependencyProperty.Register(nameof(DistributionTitle), typeof(string), typeof(Selector), new FrameworkPropertyMetadata(""));

        /// <summary>
        /// Gets and sets the distribution title.
        /// </summary>
        public string DistributionTitle
        {
            get => (string)GetValue(DistributionTitleProperty);
            set => SetValue(DistributionTitleProperty, value);
        }

        /// <summary>
        /// Dependency property for showing the distribution title.
        /// </summary>
        public static readonly DependencyProperty SampleDataProperty = DependencyProperty.Register(nameof(SampleData), typeof(double[]), typeof(Selector), new FrameworkPropertyMetadata(null, SampleDataProperty_Callback));

        /// <summary>
        /// Property changed callback for the SampleData dependency property.
        /// Updates the histogram and statistics when sample data changes.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void SampleDataProperty_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) { return; }
            if (d.GetType() != typeof(Selector)) { return; }
            Selector thisControl = (Selector)d;
            // 
            thisControl.UpdateHistogram();
            thisControl.UpdateDistributionStats();
        }

        /// <summary>
        /// Gets and sets the distribution title.
        /// </summary>
        public double[] SampleData
        {
            get => (double[])GetValue(SampleDataProperty);
            set => SetValue(SampleDataProperty, value);
        }

        /// <summary>
        /// Returns directly constructible distributions supported by the selector, excluding empirical,
        /// kernel-density, user-defined, and component-only distribution types.
        /// </summary>
        /// <returns></returns>
        public static List<UnivariateDistributionBase> DefaultDistributions
        {
            get
            {
                List<UnivariateDistributionBase> distributions = new List<UnivariateDistributionBase>();
                foreach (UnivariateDistributionType distributionType in (UnivariateDistributionType[])Enum.GetValues(typeof(UnivariateDistributionType)))
                {
                    if (distributionType == UnivariateDistributionType.Empirical) { continue; }
                    if (distributionType == UnivariateDistributionType.KernelDensity) { continue; }
                    if (distributionType == UnivariateDistributionType.UserDefined) { continue; }
                    if (UnivariateDistributionFactory.TryCreateDistribution(distributionType, out var distributionToAdd) &&
                        distributionToAdd is not null)
                        distributions.Add(distributionToAdd);
                }
                // 
                return distributions;
            }
        }

        /// <summary>
        /// Updates the histogram display with sample data.
        /// Creates bins, normalizes frequencies, and updates statistics columns.
        /// </summary>
        private void UpdateHistogram()
        {
            //Update Histogram and show fit options (filtered by those that implement IEstimate()
            if (SampleData == null || SampleData.Length == 0)
            {
                Plot.Series.Remove(_histogramSeries);
                _histogramSeries.ItemsSource = null;
                EstimateParametersButton.Visibility = Visibility.Collapsed;
                DataStatsColumn.Visibility = Visibility.Collapsed;
            }
            else
            {
                var histogram = new Histogram(SampleData);
                var histItems = new List<OxyPlot.Series.HistogramItem>();
                double sum = 0;
                for (int i = 0; i < histogram.NumberOfBins; i++) { sum += histogram[i].Frequency * histogram.BinWidth; }
                for (int i = 0; i < histogram.NumberOfBins; i++)
                {
                    histItems.Add(new OxyPlot.Series.HistogramItem(histogram[i].LowerBound, histogram[i].UpperBound, histogram[i].Frequency * histogram.BinWidth / sum));
                }

                _histogramSeries.ItemsSource = histItems;

                if (Plot.Series.Contains(_histogramSeries) == false) { Plot.Series.Insert(0, _histogramSeries); }
                //show fit options (filtered by those that implement IEstimate()
                if (DistributionCanEstimate() == true)
                {
                    EstimateParametersButton.Visibility = Visibility.Visible;
                }
                else
                {
                    EstimateParametersButton.Visibility = Visibility.Collapsed;
                }
                // Data Stats
                DataStatsColumn.Visibility = Visibility.Visible;
                var summaryPercentiles = Numerics.Data.Statistics.Statistics.SevenNumberSummary(SampleData);
                var stats = Numerics.Data.Statistics.Statistics.ProductMoments(SampleData);
                var mode = histogram.Mode;

                SummaryStatisticsList[0].DataStat = summaryPercentiles[0].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[1].DataStat = summaryPercentiles[6].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[2].DataStat = stats[0].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[3].DataStat = mode.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[4].DataStat = stats[1].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[5].DataStat = stats[2].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[6].DataStat = stats[3].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[7].DataStat = summaryPercentiles[1].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[8].DataStat = summaryPercentiles[2].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[9].DataStat = summaryPercentiles[3].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[10].DataStat = summaryPercentiles[4].ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[11].DataStat = summaryPercentiles[5].ToString("N4", CultureInfo.CurrentCulture);

                SummaryTable.Items.Refresh();

            }

            Plot.InvalidatePlot(true);
            UpdatePDFPlot();
        }

        /// <summary>
        /// The distribution combobox selection has changed. Perform action.
        /// </summary>
        private void DistributionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // clear the parameter list
            for (int i = ParameterList.Count - 1; i >= 0; i--)
            {
                ParameterList[i].PropertyChanged -= ParameterPropertyChanged;
                ParameterList.RemoveAt(i);
            }

            // return if nothing selected
            if (DistributionCombobox.SelectedIndex == -1)
            {
                UpdatePDFPlot();
                UpdateDistributionStats();
                return;
            }
            //
            UnivariateDistributionBase currentDistribution = (UnivariateDistributionBase)DistributionCombobox.SelectedItem;
            if (currentDistribution.Type != UnivariateDistributionType.Empirical && currentDistribution.Type != UnivariateDistributionType.KernelDensity)
            {
                string[,] paramString = currentDistribution.ParametersToString;
                string[] paramNames = currentDistribution.GetParameterPropertyNames;
                // Update parameter grid
                for (int i = 0; i < paramString.GetLength(0); i++)
                {
                    if (NumberFormatHelper.TryParseDouble(paramString[i, 1], out double paramValue) == false) { paramValue = double.NaN; }
                    Parameter param = new Parameter(paramNames[i], paramString[i, 0], paramValue);
                    param.PropertyChanged += ParameterPropertyChanged;
                    ParameterList.Add(param);
                }
            }

            // Update selected distribution
            if (_propertySetting == false && _settingDistributionOptions==false) { SetDistributionParameters(false); }

            //show fit options (filtered by those that implement IEstimate()
            if (SampleData != null && SampleData.Length > 0 && DistributionCanEstimate() == true)
            {
                EstimateParametersButton.Visibility = Visibility.Visible;
            }
            else
            {
                EstimateParametersButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Determines whether the currently selected distribution supports parameter estimation.
        /// </summary>
        /// <returns><c>true</c> if the distribution implements parameter estimation; otherwise, <c>false</c>.</returns>
        private bool DistributionCanEstimate()
        {
            if (SelectedDistribution is not null)
            {
                if (SelectedDistribution.Type == UnivariateDistributionType.Deterministic) { return true; }
                if (SelectedDistribution.Type == UnivariateDistributionType.Normal) { return true; }
                if (SelectedDistribution.Type == UnivariateDistributionType.Triangular) { return true; }
                if (SelectedDistribution.Type == UnivariateDistributionType.Pert) { return true; }
                if (SelectedDistribution.Type == UnivariateDistributionType.LnNormal) { return true; }
                if (SelectedDistribution.Type == UnivariateDistributionType.TruncatedNormal) { return true; }
            }
            return false;
        }

        /// <summary>
        /// Handles property changes on parameter objects.
        /// Triggers distribution parameter updates when a parameter value changes.
        /// </summary>
        /// <param name="sender">The parameter that raised the event.</param>
        /// <param name="e">Event arguments containing the property name.</param>
        private void ParameterPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Parameter.Value)) { SetDistributionParameters(true); }
        }

        /// <summary>
        /// Set the probability distribution parameters.
        /// </summary>
        private void SetDistributionParameters(bool setParameters)
        {
            if (DistributionCombobox.SelectedIndex == -1) { return; }
            ClearValidation();

            UnivariateDistributionBase? currentDistribution = null;
            try
            {
                // Get the selected distribution and set the parameters from the parameter list
                currentDistribution = ((UnivariateDistributionBase)DistributionCombobox.SelectedItem).Clone();

                if (currentDistribution.Type == UnivariateDistributionType.Empirical || currentDistribution.Type == UnivariateDistributionType.KernelDensity)
                {
                    throw new NotImplementedException();
                }
                else
                {
                    if (setParameters) { currentDistribution.SetParameters(ParameterList.Select(o => o.Value).ToArray()); }
                    if (currentDistribution.ParametersValid == false)
                    {
                        ArgumentOutOfRangeException? ex = currentDistribution.ValidateParameters(ParameterList.Select(o => o.Value).ToArray(), false);
                        if (ex?.ParamName != null) { SetInvalidParameter(ex.ParamName, ex.Message); }
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is ArgumentOutOfRangeException argEx && argEx.ParamName != null)
                {
                    SetInvalidParameter(argEx.ParamName, argEx.Message);
                }
            }
            //
            if (_propertySetting == false && currentDistribution is not null)
            {
                _distributionChanging = true;
                SelectedDistribution = currentDistribution.Clone();
                _distributionChanging = false;
            }
            // Update the plot and summary statistics
            UpdatePDFPlot();
            UpdateDistributionStats();
        }
        private bool _distributionChanging = false;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Clears validation errors from all parameters in the parameter list.
        /// </summary>
        private void ClearValidation()
        {
            foreach (Parameter p in ParameterList)
            {
                p.IsValid = true;
                p.ErrorMessage = null!;
            }
        }

        /// <summary>
        /// Marks a specific parameter as invalid and sets its error message.
        /// </summary>
        /// <param name="parameterName">The name of the parameter to mark as invalid.</param>
        /// <param name="errorMessage">The validation error message to display.</param>
        private void SetInvalidParameter(string parameterName, string errorMessage)
        {
            Parameter? param = ParameterList.FirstOrDefault(o => string.Equals(o.Name, parameterName, StringComparison.OrdinalIgnoreCase));
            if (param is null) { return; }
            param.IsValid = false;
            param.ErrorMessage = errorMessage;
        }

        /// <summary>
        /// Sets the available distribution options for the selector.
        /// </summary>
        /// <param name="univariateTypes">The collection of distribution types to display in the selector.</param>
        public void SetDistributionOptions(IEnumerable<UnivariateDistributionType> univariateTypes)
        {
            _settingDistributionOptions = true;
            DistributionCombobox.ItemsSource = null;
            _distributions.Clear();
            foreach (var d in univariateTypes)
            {
                if (_selectedDistribution is not null && d == _selectedDistribution.Type)
                {
                    _distributions.Add(_selectedDistribution);
                }
                else if (UnivariateDistributionFactory.TryCreateDistribution(d, out var distribution) &&
                         distribution is not null)
                {
                    _distributions.Add(distribution);
                }
            }
            DistributionCombobox.ItemsSource = _distributions;
            DistributionCombobox.SelectedItem = _selectedDistribution;
            _settingDistributionOptions = false;
        }

        /// <summary>
        /// Update the PDF plot.
        /// </summary>
        private void UpdatePDFPlot()
        {
            OxyPlot.Series.AreaSeries areaSeries = (OxyPlot.Series.AreaSeries)PDF.InternalSeries;

            if (SampleData == null || SampleData.Length == 0)
            { PDF.Fill = (Color)ColorConverter.ConvertFromString("#FF3F8BD6"); }
            else
            { PDF.Fill = Color.FromArgb(100, 63, 139, 214); }

            areaSeries.Points.Clear();
            areaSeries.Points2.Clear();
            if ((SelectedDistribution is not null) && SelectedDistribution.ParametersValid)
            {
                InvalidDistributionTextBlock.Visibility = Visibility.Collapsed;
                Plot.Visibility = Visibility.Visible;
                double[,]? PDFgraph;

                if (SelectedDistribution.Type == UnivariateDistributionType.Empirical)
                {
                    List<StratificationBin> range = Stratify.XValues(new StratificationOptions(SelectedDistribution.InverseCDF(0.001d), SelectedDistribution.InverseCDF(0.999d), 500));
                    PDFgraph = SelectedDistribution.CreatePDFGraph(range);
                }
                else if (SelectedDistribution.Type == UnivariateDistributionType.KernelDensity)
                {
                    PDFgraph = null;
                }
                else if (SelectedDistribution.Type == UnivariateDistributionType.Deterministic)
                {
                    PDFgraph = new double[5, 2];
                    PDFgraph[0, 0] = 0d;
                    PDFgraph[0, 1] = 0d;
                    PDFgraph[1, 0] = SelectedDistribution.Mean;
                    PDFgraph[1, 1] = 0d;
                    PDFgraph[2, 0] = SelectedDistribution.Mean;
                    PDFgraph[2, 1] = 1d;
                    PDFgraph[3, 0] = SelectedDistribution.Mean;
                    PDFgraph[3, 1] = 0d;
                    PDFgraph[4, 0] = SelectedDistribution.Mean * 2d;
                    PDFgraph[4, 1] = 0d;
                }
                else
                {
                    List<StratificationBin> range = Stratify.XValues(new StratificationOptions(SelectedDistribution.InverseCDF(0.001d), SelectedDistribution.InverseCDF(0.999d), 500));
                    PDFgraph = SelectedDistribution.CreatePDFGraph(range);
                }
                //
                // Create PDF Plot
                OxyPlot.Series.AreaSeries arSeries = (OxyPlot.Series.AreaSeries)PDF.InternalSeries;
                arSeries.Points.Clear();
                arSeries.Points2.Clear();
                if (PDFgraph is not null)
                {
                    for (int i = 0; i < PDFgraph.GetLength(0); i++)
                    {
                        arSeries.Points.Add(new OxyPlot.DataPoint(PDFgraph[i, 0], PDFgraph[i, 1]));
                        arSeries.Points2.Add(new OxyPlot.DataPoint(PDFgraph[i, 0], 0d));
                    }
                }
            }
            else
            {
                InvalidDistributionTextBlock.Visibility = Visibility.Visible;
                Plot.Visibility = Visibility.Hidden;
                InvalidDistributionTextBlock.Text = SelectedDistribution is null ? "No distribution has been selected." : "Selected distribution has invalid parameters.";
            }
            // 
            Plot.InvalidatePlot();
        }

        /// <summary>
        /// Update the summary statistics.
        /// </summary>
        private void UpdateDistributionStats()
        {
            // Distribution
            if ((SelectedDistribution is null) || (SelectedDistribution.ParametersValid == false))
            {
                SummaryStatisticsList[0].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[1].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[2].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[3].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[4].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[5].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[6].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[7].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[8].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[9].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[10].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[11].DistStat = double.NaN.ToString("N4", CultureInfo.CurrentCulture);
                SummaryTable.Items.Refresh();
            }
            else
            {
                SummaryStatisticsList[0].DistStat = SelectedDistribution.Minimum.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[1].DistStat = SelectedDistribution.Maximum.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[2].DistStat = SelectedDistribution.Mean.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[3].DistStat = SelectedDistribution.Mode.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[4].DistStat = SelectedDistribution.StandardDeviation.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[5].DistStat = SelectedDistribution.Skewness.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[6].DistStat = SelectedDistribution.Kurtosis.ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[7].DistStat = SelectedDistribution.InverseCDF(0.05d).ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[8].DistStat = SelectedDistribution.InverseCDF(0.25d).ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[9].DistStat = SelectedDistribution.InverseCDF(0.5d).ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[10].DistStat = SelectedDistribution.InverseCDF(0.75d).ToString("N4", CultureInfo.CurrentCulture);
                SummaryStatisticsList[11].DistStat = SelectedDistribution.InverseCDF(0.95d).ToString("N4", CultureInfo.CurrentCulture);

                // Add Goodness of fit stats. Only populate indices 12-14 when the list was
                // built with all 15 rows. Selector initializes 12 rows; the three GoF rows
                // are added only by DistributionSelectorControl's histogram update path.
                // Without the bounds check this threw ArgumentOutOfRangeException whenever
                // the fitting branch was reached under Selector.
                if (SampleData != null && DistributionCanEstimate() == true && SummaryStatisticsList.Count >= 15)
                {
                    var data = SampleData.ToArray();
                    Array.Sort(data);

                    var rmse = GoodnessOfFit.RMSE(SampleData, SelectedDistribution);
                    var chi = GoodnessOfFit.ChiSquared(data, SelectedDistribution);
                    var ks = GoodnessOfFit.KolmogorovSmirnov(data, SelectedDistribution);

                    SummaryStatisticsList[12].DistStat = rmse.ToString("N4", CultureInfo.CurrentCulture);
                    SummaryStatisticsList[13].DistStat = chi.ToString("N4", CultureInfo.CurrentCulture);
                    SummaryStatisticsList[14].DistStat = ks.ToString("N4", CultureInfo.CurrentCulture);
                }


                SummaryTable.Items.Refresh();
            }
        }

        /// <summary>
        /// Handles the estimate parameters button click event.
        /// Estimates distribution parameters from sample data using appropriate estimation methods.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void EstimateParametersButton_Click(object sender, RoutedEventArgs e)
        {
            if (DistributionCanEstimate() == true && SelectedDistribution is not null)
            {
                var estimatedDistribution = SelectedDistribution.Clone();
                if (estimatedDistribution.Type == UnivariateDistributionType.Pert)
                {
                    ((Pert)estimatedDistribution).Estimate(SampleData, ParameterEstimationMethod.MethodOfPercentiles);
                }
                else
                {
                    ((IEstimation)estimatedDistribution).Estimate(SampleData, ParameterEstimationMethod.MethodOfMoments);
                }

                SelectedDistribution = estimatedDistribution;
            }
            //
            UpdatePDFPlot();
        }
    }
}
