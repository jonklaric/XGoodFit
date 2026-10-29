using Numerics.Data;
using Numerics.Distributions;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing uncertain ordered paired data in a table format with distribution selection.
    /// Combines a distribution selector combobox with a table editor for managing uncertainty in paired data.
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
    public partial class UncertainOrderedDataTableEditor : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="AddRemoveRows"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AddRemoveRowsProperty = DependencyProperty.Register(nameof(AddRemoveRows), typeof(bool), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether users can add or remove rows.
        /// </summary>
        public bool AddRemoveRows
        {
            get { return (bool)GetValue(AddRemoveRowsProperty); }
            set { SetValue(AddRemoveRowsProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="XColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XColumnHeaderProperty = DependencyProperty.Register(nameof(XColumnHeader), typeof(string), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata("X Data"));

        /// <summary>
        /// Gets or sets the column header text for the X (independent variable) column.
        /// </summary>
        public string XColumnHeader
        {
            get { return (string)GetValue(XColumnHeaderProperty); }
            set { SetValue(XColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="YColumnHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YColumnHeaderProperty = DependencyProperty.Register(nameof(YColumnHeader), typeof(string), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata("Y Data"));

        /// <summary>
        /// Gets or sets the column header text for the Y (distribution parameters) columns.
        /// </summary>
        public string YColumnHeader
        {
            get { return (string)GetValue(YColumnHeaderProperty); }
            set { SetValue(YColumnHeaderProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStrictXProperty = DependencyProperty.Register(nameof(IsStrictX), typeof(bool), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(false, PropertyChanged_Callback));

        /// <summary>
        /// Gets or sets a value indicating whether X values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictX
        {
            get { return (bool)GetValue(IsStrictXProperty); }
            set { SetValue(IsStrictXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="IsStrictY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsStrictYProperty = DependencyProperty.Register(nameof(IsStrictY), typeof(bool), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(false, PropertyChanged_Callback));

        /// <summary>
        /// Gets or sets a value indicating whether Y values must be strictly ordered (no duplicates allowed).
        /// </summary>
        public bool IsStrictY
        {
            get { return (bool)GetValue(IsStrictYProperty); }
            set { SetValue(IsStrictYProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrderXProperty = DependencyProperty.Register(nameof(OrderX), typeof(SortOrder), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(SortOrder.Ascending, PropertyChanged_Callback));

        /// <summary>
        /// Gets or sets the required sort order for X values.
        /// </summary>
        public SortOrder OrderX
        {
            get { return (SortOrder)GetValue(OrderXProperty); }
            set { SetValue(OrderXProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="OrderY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty OrderYProperty = DependencyProperty.Register(nameof(OrderY), typeof(SortOrder), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(SortOrder.Ascending, PropertyChanged_Callback));

        /// <summary>
        /// Gets or sets the required sort order for Y values.
        /// </summary>
        public SortOrder OrderY
        {
            get { return (SortOrder)GetValue(OrderYProperty); }
            set { SetValue(OrderYProperty, value); }
        }

        /// <summary>
        /// Handles property changed callbacks for ordering and strictness properties.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private static void PropertyChanged_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UncertainOrderedDataTableEditor thisControl = (UncertainOrderedDataTableEditor)d;
            foreach (var dist in thisControl._distributions)
            {
                dist.OrderX = thisControl.OrderX;
                dist.OrderY = thisControl.OrderY;
                dist.StrictX = thisControl.IsStrictX;
                dist.StrictY = thisControl.IsStrictY;
            }
        }

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty = DependencyProperty.Register(nameof(MaximumX), typeof(double), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(double.MaxValue));

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
        public static readonly DependencyProperty MinimumXProperty = DependencyProperty.Register(nameof(MinimumX), typeof(double), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(double.MinValue));

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
        public static readonly DependencyProperty MaximumYProperty = DependencyProperty.Register(nameof(MaximumY), typeof(double), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(double.MaxValue));

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
        public static readonly DependencyProperty MinimumYProperty = DependencyProperty.Register(nameof(MinimumY), typeof(double), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(double.MinValue));

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
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(nameof(IsReadOnly), typeof(bool), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the control is read-only.
        /// </summary>
        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ShowToolBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowToolBarProperty = DependencyProperty.Register(nameof(ShowToolBar), typeof(bool), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the toolbar is visible.
        /// </summary>
        public bool ShowToolBar
        {
            get { return (bool)GetValue(ShowToolBarProperty); }
            set { SetValue(ShowToolBarProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderStyleProperty = DependencyProperty.Register(nameof(ColumnHeaderStyle), typeof(Style), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for column headers in the data grid.
        /// </summary>
        public Style ColumnHeaderStyle
        {
            get { return (Style)GetValue(ColumnHeaderStyleProperty); }
            set { SetValue(ColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CellStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(nameof(CellStyle), typeof(Style), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for cells in the data grid.
        /// </summary>
        public Style CellStyle
        {
            get { return (Style)GetValue(CellStyleProperty); }
            set { SetValue(CellStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DistributionSelectorMaxWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DistributionSelectorMaxWidthProperty = DependencyProperty.Register(nameof(DistributionSelectorMaxWidth), typeof(double), typeof(UncertainOrderedDataTableEditor), new FrameworkPropertyMetadata(double.MaxValue));

        /// <summary>
        /// Gets or sets the maximum width of the distribution selector control.
        /// </summary>
        public double DistributionSelectorMaxWidth
        {
            get { return (double)GetValue(DistributionSelectorMaxWidthProperty); }
            set { SetValue(DistributionSelectorMaxWidthProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedUncertainOrderedData"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedUncertainOrderedDataProperty = DependencyProperty.Register(nameof(SelectedUncertainOrderedData), typeof(UncertainOrderedPairedData), typeof(UncertainOrderedDataTableEditor), new PropertyMetadata(null, SelectedUncertainData_Callback));
        private bool _settingSelected = false;

        /// <summary>
        /// Handles changes to the SelectedUncertainOrderedData property and synchronizes the UI.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private static void SelectedUncertainData_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(UncertainOrderedDataTableEditor)) return;
            UncertainOrderedDataTableEditor thisControl = (UncertainOrderedDataTableEditor)d;
            if (thisControl._updatingSelected == true) return;

            UncertainOrderedPairedData newData = e.NewValue as UncertainOrderedPairedData;
            if (newData == null) return;

            // Get distribution index
            int index = -1;
            for (int i = 0; i < thisControl._distributions.Count; i++)
            {
                if (thisControl._distributions[i].Distribution == newData.Distribution)
                {
                    index = i;
                    break;
                }
            }
            // Check if current distribution is supported
            if (index == -1)
            {
                thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
                return;
            }
            thisControl._settingSelected = true;
            thisControl.CurveUncertaintyComboBox.SelectedIndex = -1;
            thisControl._distributions[index] = newData;
            thisControl.CurveUncertaintyComboBox.SelectedIndex = index;
            for (int i = 0; i < thisControl._distributions.Count; i++)
            {
                if (i == index)
                    continue;
                thisControl._distributions[i] = thisControl._distributions[i].Clone();
            }

            thisControl._settingSelected = false;
        }

        private bool _updatingSelected = false;

        /// <summary>
        /// Handles selection changes in the curve uncertainty combobox.
        /// </summary>
        /// <param name="sender">The combobox that changed.</param>
        /// <param name="e">The selection changed event arguments.</param>
        private void CurveUncertaintyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_settingSelected == true) return;
            _updatingSelected = true;
            if (CurveUncertaintyComboBox.SelectedIndex >= 0)
            {
                SelectedUncertainOrderedData = _distributions[CurveUncertaintyComboBox.SelectedIndex];
            }
            else
            {
                SelectedUncertainOrderedData = null;
            }

            _updatingSelected = false;
        }

        /// <summary>
        /// Get and set the selected probability distribution.
        /// </summary>
        public UncertainOrderedPairedData SelectedUncertainOrderedData
        {
            get { return (UncertainOrderedPairedData)GetValue(SelectedUncertainOrderedDataProperty); }
            set { SetValue(SelectedUncertainOrderedDataProperty, value); }
        }

        private ObservableCollection<UncertainOrderedPairedData> _distributions = new ObservableCollection<UncertainOrderedPairedData>();

        /// <summary>
        /// Dependency property for the control distribution options.
        /// </summary>
        public static readonly DependencyProperty DistributionOptionsProperty = DependencyProperty.Register(nameof(DistributionOptions), typeof(IList<UnivariateDistributionType>), typeof(UncertainOrderedDataTableEditor), new PropertyMetadata(null, DistributionOptionsCallback));

        /// <summary>
        /// Gets and sets the distribution options.
        /// </summary>
        public IList<UnivariateDistributionType> DistributionOptions
        {
            get { return (IList<UnivariateDistributionType>)GetValue(DistributionOptionsProperty); }
            set { SetValue(DistributionOptionsProperty, value); }
        }

        /// <summary>
        /// Handles changes to the DistributionOptions property and updates the available distributions.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The property changed event arguments.</param>
        private static void DistributionOptionsCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UncertainOrderedDataTableEditor thisControl = (UncertainOrderedDataTableEditor)d;
            IList<UnivariateDistributionType> distOptions = e.NewValue as IList<UnivariateDistributionType>;
            if (e.NewValue == null || distOptions == null)
            {
                thisControl.DistributionOptions = DefaultDistributionOptions;
            }
            else
            {
                // clear any distributions that should go
                var toDelete = new List<UncertainOrderedPairedData>();
                foreach (var dist in thisControl._distributions)
                {
                    if (distOptions.Contains(dist.Distribution) == false)
                        toDelete.Add(dist);
                }

                foreach (var item in toDelete)
                    thisControl._distributions.Remove(item);
                // add any new ones.
                foreach (var dist in distOptions)
                {
                    if (thisControl._distributions.Any(o => o.Distribution == dist)) continue;
                    if (!UnivariateDistributionFactory.TryCreateDistribution(dist, out var distribution) ||
                        distribution is null) continue;
                    var ordinates = new List<UncertainOrdinate>();
                    for (int i = 0; i <= 1; i++)
                        ordinates.Add(new UncertainOrdinate(i, distribution.Clone()));
                    var uncertainData = new UncertainOrderedPairedData(ordinates, thisControl.IsStrictX, thisControl.OrderX, thisControl.IsStrictY, thisControl.OrderY, dist);
                    thisControl._distributions.Add(uncertainData);
                }
            }
        }

        /// <summary>
        /// Returns the default distribution types supported by the uncertain curve table editor.
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
        /// Occurs when the data grid columns have been auto-generated.
        /// </summary>
        public event ColumnsAutoGeneratedEventHandler ColumnsAutoGenerated;

        /// <summary>
        /// Represents a method that handles the columns auto-generated event.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The collection of auto-generated columns.</param>
        public delegate void ColumnsAutoGeneratedEventHandler(object sender, ObservableCollection<DataGridColumn> e);

        /// <summary>
        /// Initializes a new instance of the <see cref="UncertainOrderedDataTableEditor"/> class.
        /// </summary>
        public UncertainOrderedDataTableEditor()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
            ColumnHeaderStyle = (Style)FindResource("WrappedColumnHeaderStyle");
            DistributionOptions = DefaultDistributionOptions;
            CurveUncertaintyComboBox.ItemsSource = _distributions;
            UncertainTableEditor.ValidationGrid.AutoGeneratedColumns += (object sender, EventArgs e) => ColumnsAutoGenerated?.Invoke(sender, UncertainTableEditor.ValidationGrid.Columns);
        }

        /// <summary>
        /// Forces validation on all rows in the data grid.
        /// </summary>
        public void ForceGridValidation()
        {
            UncertainTableEditor.ForceGridValidation();
        }

        /// <summary>
        /// Refreshes the data grid with current data.
        /// </summary>
        public void Refresh()
        {
            UncertainTableEditor.Refresh();
        }

    }

    /// <summary>
    /// Converts UncertainOrderedPairedData objects to their distribution display names for UI binding.
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
    public class DistributionNameConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            UncertainOrderedPairedData val = value as UncertainOrderedPairedData;
            if (val == null) return "";
            if (val.Count > 0) return val[0].Y.DisplayName;
            if (UnivariateDistributionFactory.TryCreateDistribution(val.Distribution, out var distribution) &&
                distribution is not null) return distribution.DisplayName;
            return val.Distribution.ToString();
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

    }

}
