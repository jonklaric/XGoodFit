using Numerics.Distributions;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NumericControls
{
    /// <summary>
    /// A user control for editing univariate empirical distribution X and probability values.
    /// Provides a data grid for entering paired X values and probabilities with validation.
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
    public partial class UnivariateXPControl : UserControl
    {
        private ObservableCollection<object> _univariateRowData = new ObservableCollection<object>();

        /// <summary>
        /// Dependency property for the selected distribution.
        /// </summary>
        public static readonly DependencyProperty DistributionProperty = DependencyProperty.Register(nameof(Distribution), typeof(EmpiricalDistribution), typeof(UnivariateXPControl), new PropertyMetadata(null, SetDistribution));

        /// <summary>
        /// Callback invoked when the Distribution property changes.
        /// Updates the data grid with X values and probability values from the new distribution.
        /// </summary>
        /// <param name="d">The dependency object whose property changed.</param>
        /// <param name="e">Event arguments containing the old and new property values.</param>
        private static void SetDistribution(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(UnivariateXPControl)) return;
            UnivariateXPControl thisControl = (UnivariateXPControl)d;
            thisControl._univariateRowData.Clear();
            // 
            if (e.NewValue == null) return;
            EmpiricalDistribution newDistribution = e.NewValue as EmpiricalDistribution;
            if (newDistribution == null) return;
            // 
            var xVals = newDistribution.XValues;
            var yVals = newDistribution.ProbabilityValues;
            // 
            thisControl._univariateRowData.Clear();
            for (int i = 0; i < xVals.Count; i++)
                thisControl._univariateRowData.Add(new UnivariateDistributionValidatingRow(xVals[i], yVals[i], newDistribution.Minimum, newDistribution.Maximum, thisControl._univariateRowData));
        }

        /// <summary>
        /// Get and set the selected probability distribution.
        /// </summary>
        public EmpiricalDistribution Distribution
        {
            get { return (EmpiricalDistribution)GetValue(DistributionProperty); }
            set { SetValue(DistributionProperty, value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnivariateXPControl"/> class.
        /// Sets up the data grid with the univariate row data binding.
        /// </summary>
        public UnivariateXPControl()
        {

            // This call is required by the designer.
            InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            UnivariateGrid.ItemsSource = _univariateRowData;
        }

        /// <summary>
        /// Handles the event when a column is being auto-generated for the univariate data grid.
        /// Applies custom header styling to the column.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Column auto-generation event arguments containing the column being generated.</param>
        private void UnivariateGrid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            e.Column.HeaderStyle = (Style)Resources["WrappedColumnHeaderStyle"];
        }

        /// <summary>
        /// Handles the click event on a data grid column header.
        /// Selects all cells in the clicked column.
        /// </summary>
        /// <param name="sender">The column header that was clicked.</param>
        /// <param name="e">The routed event arguments.</param>
        private void ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            DataGridColumnHeader columnHeader = sender as DataGridColumnHeader;
            if (columnHeader == null) return;
            // 
            UnivariateGrid.SelectedCells.Clear();
            foreach (var item in UnivariateGrid.Items)
                UnivariateGrid.SelectedCells.Add(new DataGridCellInfo(item, columnHeader.Column));
        }
    }
}
