using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// A control that provides a combobox for selecting and managing series in an OxyPlot chart.
    /// Allows users to select, reorder, and delete series from the plot.
    /// </summary>
    public partial class SeriesSelectorControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Plot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotProperty = DependencyProperty.Register(
            nameof(Plot),
            typeof(Plot),
            typeof(SeriesSelectorControl),
            new PropertyMetadata(null, InitializePlot));

        /// <summary>
        /// Gets or sets the OxyPlot Plot control that this selector is bound to.
        /// </summary>
        public Plot Plot
        {
            get => (Plot)GetValue(PlotProperty);
            set => SetValue(PlotProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedSeries"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedSeriesProperty = DependencyProperty.Register(
            nameof(SelectedSeries),
            typeof(OxyPlot.Wpf.Series),
            typeof(SeriesSelectorControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the currently selected series.
        /// </summary>
        public OxyPlot.Wpf.Series SelectedSeries
        {
            get => (OxyPlot.Wpf.Series)GetValue(SelectedSeriesProperty);
            set => SetValue(SelectedSeriesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ComboBoxStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ComboBoxStyleProperty = DependencyProperty.Register(
            nameof(ComboBoxStyle),
            typeof(Style),
            typeof(SeriesSelectorControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style to apply to the series selection combobox.
        /// </summary>
        public Style ComboBoxStyle
        {
            get => (Style)GetValue(ComboBoxStyleProperty);
            set => SetValue(ComboBoxStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle),
            typeof(Style),
            typeof(SeriesSelectorControl));

        /// <summary>
        /// Gets or sets the style to apply to expanders in the series properties control.
        /// </summary>
        public Style ExpanderStyle
        {
            get => (Style)GetValue(ExpanderStyleProperty);
            set => SetValue(ExpanderStyleProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesSelectorControl"/> class.
        /// </summary>
        public SeriesSelectorControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the default combobox style from resources.
        /// </summary>
        private void SetDefaultComboboxStyle()
        {
            ComboBoxStyle = (Style)FindResource("CleanComboBoxStyle");
        }

        /// <summary>
        /// Handles changes to the Plot property.
        /// </summary>
        private static void InitializePlot(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(SeriesSelectorControl)) return;
            var thisControl = (SeriesSelectorControl)d;

            // Unsubscribe from old collection's events
            if (e.OldValue is Plot oldPlot)
            {
                oldPlot.Series.CollectionChanged -= thisControl.Series_CollectionChanged;
            }

            thisControl.SeriesPropertyControlComboBox.ItemsSource = null;
            if (e.NewValue == null) return;
            if (e.NewValue.GetType() != typeof(Plot)) return;
            var newPlot = (Plot)e.NewValue;

            if (thisControl.ComboBoxStyle == null) thisControl.SetDefaultComboboxStyle();

            thisControl.SeriesPropertyControlComboBox.ItemsSource = newPlot.Series;

            // Subscribe to collection changes to handle demo switching
            newPlot.Series.CollectionChanged += thisControl.Series_CollectionChanged;

            if (newPlot.Series.Count > 0) thisControl.SeriesPropertyControlComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles changes to the series collection.
        /// </summary>
        private void Series_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle Reset action (when Clear() is called)
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // Selection will be cleared, wait for new items to be added
                return;
            }

            // New item was added
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                // If no item is selected and items exist, select the first one
                if (SeriesPropertyControlComboBox.SelectedItem == null && Plot?.Series.Count > 0)
                {
                    SeriesPropertyControlComboBox.SelectedIndex = 0;
                }
                else
                {
                    // Select the newly added item
                    foreach (var newItem in e.NewItems)
                    {
                        var series = newItem as OxyPlot.Wpf.Series;
                        if (series != null)
                        {
                            SeriesPropertyControlComboBox.SelectedItem = newItem;
                            SeriesPropertiesControl.Series = series;
                            break;
                        }
                    }
                }
            }

            // Item was removed
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (SeriesPropertyControlComboBox.SelectedItem == null && Plot?.Series.Count > 0)
                {
                    SeriesPropertyControlComboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Handles selection changes in the series combobox.
        /// </summary>
        private void SeriesPropertyControlComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Early exit if nothing is selected
            if (SeriesPropertyControlComboBox.SelectedItem == null) return;

            var seriesToSelect = SeriesPropertyControlComboBox.SelectedItem as OxyPlot.Wpf.Series;
            if (seriesToSelect == null) return;
            SeriesPropertiesControl.Series = seriesToSelect;
        }

        /// <summary>
        /// Handles the click event for moving a series up in the order.
        /// </summary>
        private void MoveSeriesUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;
            if (sender == null) return;
            if (sender.GetType() != typeof(Button)) return;
            var btn = (Button)sender;
            if (btn.DataContext == null) return;
            var seriesToMoveUp = btn.DataContext as OxyPlot.Wpf.Series;
            if (seriesToMoveUp == null) return;

            int index = Plot.Series.IndexOf(seriesToMoveUp);
            if (index == 0 || index == -1) return;

            // You can't simply swap the series, no that would be too easy.
            // Instead you have to make a copy of the series, swap on the copy, then add each series back.
            var oldSeries = new System.Collections.Generic.List<OxyPlot.Wpf.Series>(Plot.Series);
            Plot.Series.Clear();
            oldSeries[index] = oldSeries[index - 1];
            oldSeries[index - 1] = seriesToMoveUp;
            for (int i = 0; i < oldSeries.Count; i++)
            {
                Plot.Series.Add(oldSeries[i]);
            }

            SeriesPropertyControlComboBox.SelectedIndex = index - 1;
            // Reordering series Z-order is structural, not data — same series objects in a
            // different sequence. updateData=false skips the per-series UpdateData walk.
            Plot.InvalidatePlot(false);
        }

        /// <summary>
        /// Handles the click event for moving a series down in the order.
        /// </summary>
        private void MoveSeriesDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;
            if (sender == null) return;
            if (sender.GetType() != typeof(Button)) return;
            var btn = (Button)sender;
            if (btn.DataContext == null) return;

            var seriesToMoveDown = btn.DataContext as OxyPlot.Wpf.Series;
            if (seriesToMoveDown == null) return;

            int index = Plot.Series.IndexOf(seriesToMoveDown);
            if (index == Plot.Series.Count - 1 || index == -1) return;

            var oldSeries = new System.Collections.Generic.List<OxyPlot.Wpf.Series>(Plot.Series);
            Plot.Series.Clear();
            oldSeries[index] = oldSeries[index + 1];
            oldSeries[index + 1] = seriesToMoveDown;
            for (int i = 0; i < oldSeries.Count; i++)
            {
                Plot.Series.Add(oldSeries[i]);
            }

            SeriesPropertyControlComboBox.SelectedIndex = index + 1;
            // Reordering series Z-order is structural, not data — same series objects in a
            // different sequence. updateData=false skips the per-series UpdateData walk.
            Plot.InvalidatePlot(false);
        }

        /// <summary>
        /// Handles the click event for deleting a series.
        /// </summary>
        private void DeleteSeriesButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;
            if (sender == null) return;
            if (sender.GetType() != typeof(Button)) return;
            var btn = (Button)sender;
            if (btn.DataContext == null) return;

            var seriesToDelete = btn.DataContext as OxyPlot.Wpf.Series;
            if (seriesToDelete == null) return;

            int index = Plot.Series.IndexOf(seriesToDelete);
            if (index == SeriesPropertyControlComboBox.SelectedIndex)
            {
                if (index > 0) index -= 1;
                if (Plot.Series.Count == 1) index = -1;
            }
            Plot.Series.Remove(seriesToDelete);
            SeriesPropertyControlComboBox.SelectedIndex = index;
            Plot.InvalidatePlot(false);

            SeriesPropertiesControl.CloseExpanders();
        }
    }
}
