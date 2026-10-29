using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DatabaseManager;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Wpf;

namespace DatabaseControls
{
    /// <summary>
    /// A WPF UserControl that displays statistical analysis and pie chart visualization for text/alphabetic column data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This control analyzes text column data by counting unique values and displaying their distribution.
    /// It provides both a tabular view showing all unique values with their counts, and a pie chart
    /// visualization showing the proportion of each value.
    /// </para>
    /// <para>
    /// For columns with more than 4 unique values, the pie chart shows the top 4 values and groups
    /// all remaining values into an "All Other" category to maintain readability.
    /// </para>
    /// </remarks>
    public partial class AlphabeticColumnStats : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            nameof(Data),
            typeof(object[]),
            typeof(AlphabeticColumnStats),
            new UIPropertyMetadata(new object[] { }, DataProperty_Callback));

        /// <summary>
        /// Identifies the <see cref="DataName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataNameProperty = DependencyProperty.Register(
            nameof(DataName),
            typeof(string),
            typeof(AlphabeticColumnStats),
            new UIPropertyMetadata(""));

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AlphabeticColumnStats"/> class.
        /// </summary>
        public AlphabeticColumnStats()
        {
            InitializeComponent();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the data array to analyze and display.
        /// </summary>
        /// <value>An array of objects representing the column data values.</value>
        /// <remarks>
        /// When set, unique values are counted and both the statistics table and pie chart are updated.
        /// Null, DBNull, and empty string values are counted separately as empty entries.
        /// </remarks>
        public object[] Data
        {
            get => (object[])GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        /// <summary>
        /// Gets or sets the display name for the data column.
        /// </summary>
        /// <value>A string representing the column name, used as the plot title.</value>
        public string DataName
        {
            get => (string)GetValue(DataNameProperty);
            set => SetValue(DataNameProperty, value);
        }

        #endregion

        #region Dependency Property Callbacks

        /// <summary>
        /// Callback method invoked when the <see cref="Data"/> property changes.
        /// </summary>
        /// <param name="d">The dependency object on which the property changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void DataProperty_Callback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AlphabeticColumnStats thisControl = (AlphabeticColumnStats)d;
            thisControl.Plot();
        }

        #endregion

        #region Event Handlers

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

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates a default empty data table for the statistics display.
        /// </summary>
        /// <returns>A <see cref="DataTable"/> with columns for unique values and counts.</returns>
        private DataTable DefaultDataTable()
        {
            DataTable statsDataTable = new DataTable("StatsDataTable");
            statsDataTable.Columns.Add(new DataColumn("Unique Value", typeof(string)));
            statsDataTable.Columns.Add(new DataColumn("Count", typeof(int)));
            statsDataTable.Rows.Add("< Total Count >", 0);
            return statsDataTable;
        }

        /// <summary>
        /// Generates the statistics table and pie chart visualization from the data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method counts occurrences of each unique value in the data, sorts them by frequency
        /// (descending), and updates both the statistics table and pie chart.
        /// </para>
        /// <para>
        /// Empty values (null, DBNull, or empty strings) are tracked separately and displayed
        /// as a blank entry in the results.
        /// </para>
        /// <para>
        /// For pie chart readability, if there are more than 4 unique values, only the top 4
        /// are shown individually, with remaining values aggregated into "All Other".
        /// </para>
        /// </remarks>
        private void Plot()
        {
            if (Data == null)
            {
                return;
            }

            Dictionary<string, int> dict = new Dictionary<string, int>();
            int emptyCount = 0;

            for (int i = 0; i < Data.Length; i++)
            {
                if (Data[i] is DBNull || Data[i] == null || string.IsNullOrEmpty(Data[i]?.ToString()))
                {
                    emptyCount += 1;
                }
                else
                {
                    string? tempString = Convert.ToString(Data[i]);
                    if (tempString != null && dict.ContainsKey(tempString))
                    {
                        dict[tempString] += 1;
                    }
                    else if (tempString != null)
                    {
                        dict.Add(tempString, 1);
                    }
                }
            }

            // Sort the dictionary by values (descending)
            List<KeyValuePair<string, int>> sortedList = dict.ToList();
            if (emptyCount > 0)
            {
                sortedList.Add(new KeyValuePair<string, int>("", emptyCount));
            }
            sortedList.Sort((pair1, pair2) => pair2.Value.CompareTo(pair1.Value));

            // Create pie chart
            OxyPlot.Series.PieSeries internalPieSeries = (OxyPlot.Series.PieSeries)PieSeries.InternalSeries;
            internalPieSeries.Slices.Clear();

            if (sortedList.Count <= 4)
            {
                foreach (KeyValuePair<string, int> uniqueValue in sortedList)
                {
                    internalPieSeries.Slices.Add(new PieSlice(uniqueValue.Key, (double)uniqueValue.Value / Data.Length));
                }
            }
            else
            {
                double sumSoFar = 0;
                for (int i = 0; i < 4; i++)
                {
                    internalPieSeries.Slices.Add(new PieSlice(sortedList[i].Key, (double)sortedList[i].Value / Data.Length));
                    sumSoFar += (double)sortedList[i].Value / Data.Length;
                }
                internalPieSeries.Slices.Add(new PieSlice("All Other", 1 - sumSoFar));
            }

            // Create statistics table
            DataTable statsDataTable = new DataTable("StatsDataTable");
            statsDataTable.Columns.Add(new DataColumn("Unique Value", typeof(string)));
            statsDataTable.Columns.Add(new DataColumn("Count", typeof(int)));
            statsDataTable.Rows.Add("< Total Count >", Data.Length);

            foreach (KeyValuePair<string, int> uniqueValue in sortedList)
            {
                statsDataTable.Rows.Add(uniqueValue.Key, uniqueValue.Value);
            }

            InMemoryReader statsDataView = new InMemoryReader(statsDataTable);
            StatsTable.DataView = statsDataView.GetTableManager(statsDataTable.TableName);
            StatsTable.ResizeColumnWidth(0);
            StatsTable.UpdateVisibleRows();

            StatsPlot.InvalidatePlot(true);
        }

        #endregion
    }
}
