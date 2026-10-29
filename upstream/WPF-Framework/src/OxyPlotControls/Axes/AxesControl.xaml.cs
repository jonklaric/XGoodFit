using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// A user control that provides a selector and editor for OxyPlot axes.
    /// Allows users to select an axis from a dropdown and edit its properties.
    /// </summary>
    public partial class AxesControl : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="Plot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotProperty = DependencyProperty.Register(
            nameof(Plot), typeof(Wpf.Plot), typeof(AxesControl),
            new PropertyMetadata(null, InitializePlot));

        /// <summary>
        /// Gets or sets the OxyPlot Plot control that contains the axes.
        /// </summary>
        public Wpf.Plot Plot
        {
            get { return (Wpf.Plot)GetValue(PlotProperty); }
            set { SetValue(PlotProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedAxis"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedAxisProperty = DependencyProperty.Register(
            nameof(SelectedAxis), typeof(Wpf.Axis), typeof(AxesControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the currently selected axis.
        /// </summary>
        public Wpf.Axis SelectedAxis
        {
            get { return (Wpf.Axis)GetValue(SelectedAxisProperty); }
            set { SetValue(SelectedAxisProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TitleMinWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleMinWidthProp = DependencyProperty.Register(
            nameof(TitleMinWidth), typeof(int), typeof(AxesControl),
            new UIPropertyMetadata(110));

        /// <summary>
        /// Gets or sets the minimum width for title elements.
        /// </summary>
        public int TitleMinWidth
        {
            get { return (int)GetValue(TitleMinWidthProp); }
            set { SetValue(TitleMinWidthProp, value); }
        }

        /// <summary>
        /// Identifies the <see cref="LeaderLinesVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LeaderLinesVisibilityProp = DependencyProperty.Register(
            nameof(LeaderLinesVisibility), typeof(Visibility), typeof(AxesControl),
            new UIPropertyMetadata(Visibility.Visible));

        /// <summary>
        /// Gets or sets the visibility of leader lines.
        /// </summary>
        public Visibility LeaderLinesVisibility
        {
            get { return (Visibility)GetValue(LeaderLinesVisibilityProp); }
            set { SetValue(LeaderLinesVisibilityProp, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TabItemStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TabItemStyleProperty = DependencyProperty.Register(
            nameof(TabItemStyle), typeof(Style), typeof(AxesControl));

        /// <summary>
        /// Gets or sets the style for tab items.
        /// </summary>
        public Style TabItemStyle
        {
            get { return (Style)GetValue(TabItemStyleProperty); }
            set { SetValue(TabItemStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle), typeof(Style), typeof(AxesControl));

        /// <summary>
        /// Gets or sets the style for expander controls.
        /// </summary>
        public Style ExpanderStyle
        {
            get { return (Style)GetValue(ExpanderStyleProperty); }
            set { SetValue(ExpanderStyleProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ComboBoxStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ComboBoxStyleProperty = DependencyProperty.Register(
            nameof(ComboBoxStyle), typeof(Style), typeof(AxesControl),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the style for combo box controls.
        /// </summary>
        public Style ComboBoxStyle
        {
            get { return (Style)GetValue(ComboBoxStyleProperty); }
            set { SetValue(ComboBoxStyleProperty, value); }
        }

        /// <summary>
        /// Sets the default combobox style from resources.
        /// </summary>
        private void SetDefaultComboboxStyle()
        {
            ComboBoxStyle = (Style)FindResource("CleanComboBoxStyle");
        }

        /// <summary>
        /// Handles changes to the Plot property and initializes the axes selector.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">Event args containing the old and new values.</param>
        private static void InitializePlot(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d.GetType() != typeof(AxesControl)) return;
            var thisControl = (AxesControl)d;

            // Unsubscribe from old collection's events
            if (e.OldValue is Wpf.Plot oldPlot)
            {
                oldPlot.Axes.CollectionChanged -= thisControl.Axes_CollectionChanged;
            }

            thisControl.AxesPropertyControlComboBox.ItemsSource = null;
            if (e.NewValue == null) return;
            if (e.NewValue.GetType() != typeof(Wpf.Plot)) return;
            var newPlot = (Wpf.Plot)e.NewValue;

            if (thisControl.ComboBoxStyle == null) thisControl.SetDefaultComboboxStyle();
            thisControl.AxesPropertyControlComboBox.ItemsSource = newPlot.Axes;

            // Subscribe to collection changes to handle demo switching
            newPlot.Axes.CollectionChanged += thisControl.Axes_CollectionChanged;

            thisControl.AxesPropertyControlComboBox.ApplyTemplate();

            if (newPlot.Axes.Count > 0) thisControl.AxesPropertyControlComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles changes to the axes collection and updates the selected axis.
        /// </summary>
        /// <param name="sender">The collection that changed.</param>
        /// <param name="e">Event args describing the change.</param>
        private void Axes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // When axes are reset (cleared and repopulated), select the first axis
            if (e.Action == NotifyCollectionChangedAction.Reset ||
                e.Action == NotifyCollectionChangedAction.Add)
            {
                if (Plot?.Axes.Count > 0 && AxesPropertyControlComboBox.SelectedItem == null)
                {
                    AxesPropertyControlComboBox.SelectedIndex = 0;
                }
            }
            // When an axis is removed and it was selected, select another one
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if (AxesPropertyControlComboBox.SelectedItem == null && Plot?.Axes.Count > 0)
                {
                    AxesPropertyControlComboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AxesControl"/> class.
        /// </summary>
        public AxesControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles selection changes in the axes combobox.
        /// </summary>
        /// <param name="sender">The combobox control.</param>
        /// <param name="e">Event args containing selection details.</param>
        private void AxesPropertyControlComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Early exit if nothing is selected.
            if (AxesPropertyControlComboBox.SelectedItem == null) return;

            var axisToSelect = AxesPropertyControlComboBox.SelectedItem as Wpf.Axis;
            if (axisToSelect == null) return;
            AxisPropertiesControl.Axis = axisToSelect;
        }

        /// <summary>
        /// Handles the click event for deleting an axis.
        /// </summary>
        /// <param name="sender">The delete button.</param>
        /// <param name="e">Event args.</param>
        private void DeleteAxisButton_Click(object sender, RoutedEventArgs e)
        {
            if (AxesPropertyControlComboBox.SelectedItem == null) return;
            if (Plot == null) return;
            if (sender == null) return;
            if (sender.GetType() != typeof(Button)) return;
            var btn = (Button)sender;
            if (btn.DataContext == null) return;

            var axisToDelete = btn.DataContext as Wpf.Axis;
            if (axisToDelete == null) return;

            int index = Plot.Axes.IndexOf(axisToDelete);
            if (index == AxesPropertyControlComboBox.SelectedIndex)
            {
                if (index > 0) index -= 1;
                if (Plot.Axes.Count == 1) index = -1;
            }
            Plot.Axes.Remove(axisToDelete);
            AxesPropertyControlComboBox.SelectedIndex = index;

            AxisPropertiesControl.CloseExpanders();
        }

        /// <summary>
        /// Handles when the axis type is changed, updating the selector to reflect the new axis.
        /// </summary>
        /// <param name="oldAxis">The previous axis before the type change.</param>
        /// <param name="newAxis">The new axis after the type change.</param>
        private void AxisPropertiesControl_AxisTypeChanged(Wpf.Axis oldAxis, Wpf.Axis newAxis)
        {
            AxesPropertyControlComboBox.ItemsSource = Plot.Axes;
            SelectedAxis = newAxis;
            AxesPropertyControlComboBox.SelectedItem = newAxis;
        }
    }
}
