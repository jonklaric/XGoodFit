using OxyPlot;

namespace OxyPlotControls
{
    /// <summary>
    /// Defines the color values for an OxyPlot chart theme.
    /// Themes control chart infrastructure colors (background, axes, text, gridlines, legend)
    /// while preserving user-controlled series and annotation colors.
    /// </summary>
    public class OxyPlotTheme
    {
        // General

        /// <summary>
        /// Gets or sets the overall plot background color.
        /// </summary>
        public OxyColor Background { get; set; }

        /// <summary>
        /// Gets or sets the plot area background color.
        /// </summary>
        public OxyColor PlotAreaBackground { get; set; }

        /// <summary>
        /// Gets or sets the plot area border color.
        /// </summary>
        public OxyColor PlotAreaBorderColor { get; set; }

        /// <summary>
        /// Gets or sets the default text color for the plot. Used as the fallback when
        /// TitleColor, SubtitleColor, or other text colors are set to <see cref="OxyColors.Automatic"/>.
        /// </summary>
        public OxyColor TextColor { get; set; }

        // Title & Subtitle

        /// <summary>
        /// Gets or sets the title text color.
        /// </summary>
        public OxyColor TitleColor { get; set; }

        /// <summary>
        /// Gets or sets the subtitle text color.
        /// </summary>
        public OxyColor SubtitleColor { get; set; }

        // Axes

        /// <summary>
        /// Gets or sets the axis title text color.
        /// </summary>
        public OxyColor AxisTitleColor { get; set; }

        /// <summary>
        /// Gets or sets the axis line color.
        /// </summary>
        public OxyColor AxisLineColor { get; set; }

        /// <summary>
        /// Gets or sets the axis label text color.
        /// </summary>
        public OxyColor AxisTextColor { get; set; }

        /// <summary>
        /// Gets or sets the axis tick mark color.
        /// </summary>
        public OxyColor AxisTickColor { get; set; }

        /// <summary>
        /// Gets or sets the major gridline color.
        /// </summary>
        public OxyColor MajorGridlineColor { get; set; }

        /// <summary>
        /// Gets or sets the minor gridline color.
        /// </summary>
        public OxyColor MinorGridlineColor { get; set; }

        // Legend

        /// <summary>
        /// Gets or sets the legend title text color.
        /// </summary>
        public OxyColor LegendTitleColor { get; set; }

        /// <summary>
        /// Gets or sets the legend item text color.
        /// </summary>
        public OxyColor LegendTextColor { get; set; }

        /// <summary>
        /// Gets or sets the legend background color.
        /// </summary>
        public OxyColor LegendBackground { get; set; }

        /// <summary>
        /// Gets or sets the legend border color.
        /// </summary>
        public OxyColor LegendBorderColor { get; set; }

        // Series

        /// <summary>
        /// Gets or sets the background color used behind contour-series labels.
        /// Should be opaque or near-opaque and visually consistent with the plot-area
        /// background so labels hide the contour line behind them without clashing with the plot.
        /// </summary>
        public OxyColor ContourLabelBackground { get; set; }

    }
}
