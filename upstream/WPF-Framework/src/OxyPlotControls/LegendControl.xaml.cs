using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using OxyPlot.Legends;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// A user control that provides UI for editing legend properties of an OxyPlot chart,
    /// including title, items, area styling, and position settings.
    /// </summary>
    public partial class LegendControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Plot"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlotProperty = DependencyProperty.Register(
            nameof(Plot), typeof(Wpf.Plot), typeof(LegendControl),
            new PropertyMetadata(null, OnPlotPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ExpanderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle), typeof(Style), typeof(LegendControl));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the OxyPlot Plot control that this control edits.
        /// </summary>
        public Wpf.Plot Plot
        {
            get { return (Wpf.Plot)GetValue(PlotProperty); }
            set { SetValue(PlotProperty, value); }
        }

        /// <summary>
        /// Gets or sets the style applied to expander controls within this control.
        /// </summary>
        public Style ExpanderStyle
        {
            get { return (Style)GetValue(ExpanderStyleProperty); }
            set { SetValue(ExpanderStyleProperty, value); }
        }

        /// <summary>
        /// Gets the available legend orientation options.
        /// </summary>
        public static List<LegendOrientation> OrientationOptions { get; } = new List<LegendOrientation>((LegendOrientation[])Enum.GetValues(typeof(LegendOrientation)));

        /// <summary>
        /// Gets the available legend item order options.
        /// </summary>
        public static List<LegendItemOrder> ItemOrderOptions { get; } = new List<LegendItemOrder>((LegendItemOrder[])Enum.GetValues(typeof(LegendItemOrder)));

        /// <summary>
        /// Gets the available legend placement options.
        /// </summary>
        public static List<LegendPlacement> PlacementOptions { get; } = new List<LegendPlacement>((LegendPlacement[])Enum.GetValues(typeof(LegendPlacement)));

        /// <summary>
        /// Gets the available legend position options.
        /// </summary>
        public static List<LegendPosition> PositionOptions { get; } = new List<LegendPosition>((LegendPosition[])Enum.GetValues(typeof(LegendPosition)));

        /// <summary>
        /// Gets the available legend symbol placement options.
        /// </summary>
        public static List<LegendSymbolPlacement> SymbolPlacementOptions { get; } = new List<LegendSymbolPlacement>((LegendSymbolPlacement[])Enum.GetValues(typeof(LegendSymbolPlacement)));

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="LegendControl"/> class.
        /// </summary>
        public LegendControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the Plot dependency property changes.
        /// Forces a layout refresh when a new Plot is assigned.
        /// </summary>
        /// <param name="d">The dependency object (LegendControl instance).</param>
        /// <param name="e">The event arguments containing old and new values.</param>
        private static void OnPlotPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is LegendControl control && e.NewValue != null)
            {
                // Force layout update to sync bindings
                control.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
                {
                    control.UpdateLayout();
                    // Update orientation ComboBox enabled state based on current position
                    control.UpdateOrientationEnabled();
                }));
            }
        }

        /// <summary>
        /// Handles legend property ComboBox selection changes and invalidates the plot to refresh the display.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void LegendPropertyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Legend property changes (orientation, position, font, padding, etc.) are display-only;
            // no series data changes, so updateData=false avoids walking every series via
            // PlotModel.Update(true) for what is purely a layout/style refresh.
            if (Plot != null)
            {
                Plot.InvalidatePlot(false);
            }

            UpdateOrientationEnabled();
        }

        /// <summary>
        /// Updates the enabled state of the Legend Orientation ComboBox.
        /// Horizontal orientation is not supported for Left/Right legend positions
        /// because the OxyPlot core measurement pass uses the full plot width,
        /// making horizontal layout visually identical to vertical for side-positioned legends.
        /// </summary>
        private void UpdateOrientationEnabled()
        {
            if (Plot == null || LegendOrientationControl == null) return;

            var position = Plot.LegendPosition;
            bool isLeftOrRight = position == LegendPosition.LeftTop
                              || position == LegendPosition.LeftMiddle
                              || position == LegendPosition.LeftBottom
                              || position == LegendPosition.RightTop
                              || position == LegendPosition.RightMiddle
                              || position == LegendPosition.RightBottom;

            if (isLeftOrRight)
            {
                // Force Vertical and disable the control.
                // Suppress PropertyChanged so this programmatic write doesn't
                // create a spurious undo entry.
                Plot.SuppressPropertyChanged = true;
                try
                {
                    Plot.LegendOrientation = LegendOrientation.Vertical;
                }
                finally
                {
                    Plot.SuppressPropertyChanged = false;
                }
                LegendOrientationControl.IsEnabled = false;
            }
            else
            {
                LegendOrientationControl.IsEnabled = true;
            }
        }

        #endregion
    }
}
