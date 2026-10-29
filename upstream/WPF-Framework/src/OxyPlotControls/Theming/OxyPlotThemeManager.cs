using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Legends;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// Manages OxyPlot chart theme templates and provides methods to apply themes to plots.
    /// Themes control chart infrastructure colors (background, axes, text, gridlines, legend)
    /// while preserving user-controlled series and annotation colors.
    /// </summary>
    public static class OxyPlotThemeManager
    {
        private static readonly Dictionary<string, bool> _suppressionStore = new();
        private const string ThemeChangeWarningKey = "ThemeChangeWarning";

        /// <summary>
        /// Gets or sets whether the theme change warning dialog is suppressed.
        /// </summary>
        /// <remarks>
        /// When true, theme changes are applied without showing the confirmation dialog.
        /// Set this from persisted user settings at application startup to remember the
        /// user's "Don't show this message again" preference across sessions.
        /// </remarks>
        public static bool SuppressThemeChangeWarning
        {
            get => _suppressionStore.TryGetValue(ThemeChangeWarningKey, out bool val) && val;
            set => _suppressionStore[ThemeChangeWarningKey] = value;
        }

        /// <summary>
        /// Light theme: white plot area, dark text. Used for Light and Blue app themes.
        /// </summary>
        public static OxyPlotTheme LightTheme { get; } = new OxyPlotTheme
        {
            Background = OxyColors.Undefined, // Resolved at runtime from EnvironmentWindowBackground
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            TextColor = OxyColors.Black,
            TitleColor = OxyColors.Black,
            SubtitleColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            AxisTitleColor = OxyColors.Black,
            AxisLineColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            AxisTextColor = OxyColors.Black,
            AxisTickColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            MajorGridlineColor = OxyColor.FromRgb(0xE0, 0xE0, 0xE0),
            MinorGridlineColor = OxyColor.FromRgb(0xF0, 0xF0, 0xF0),
            LegendTitleColor = OxyColors.Black,
            LegendTextColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            LegendBackground = OxyColors.Transparent,
            LegendBorderColor = OxyColors.Transparent,
            ContourLabelBackground = OxyColor.FromArgb(220, 255, 255, 255),
        };

        /// <summary>
        /// Dark theme: dark plot area, light text.
        /// </summary>
        public static OxyPlotTheme DarkTheme { get; } = new OxyPlotTheme
        {
            Background = OxyColor.FromRgb(0x2D, 0x2D, 0x30),
            PlotAreaBackground = OxyColor.FromRgb(0x80, 0x80, 0x8E),
            PlotAreaBorderColor = OxyColor.FromRgb(0x88, 0x88, 0x88),
            TextColor = OxyColor.FromRgb(0xF1, 0xF1, 0xF1),
            TitleColor = OxyColor.FromRgb(0xF1, 0xF1, 0xF1),
            SubtitleColor = OxyColor.FromRgb(0xCC, 0xCC, 0xCC),
            AxisTitleColor = OxyColor.FromRgb(0xF1, 0xF1, 0xF1),
            AxisLineColor = OxyColor.FromRgb(0x88, 0x88, 0x88),
            AxisTextColor = OxyColor.FromRgb(0xCC, 0xCC, 0xCC),
            AxisTickColor = OxyColor.FromRgb(0x88, 0x88, 0x88),
            MajorGridlineColor = OxyColor.FromRgb(0x6A, 0x6A, 0x78),
            MinorGridlineColor = OxyColor.FromRgb(0x7A, 0x7A, 0x88),
            LegendTitleColor = OxyColors.Black,
            LegendTextColor = OxyColors.Black,
            LegendBackground = OxyColors.Transparent,
            LegendBorderColor = OxyColors.Transparent,
            ContourLabelBackground = OxyColor.FromArgb(220, 0x80, 0x80, 0x8E),
        };

        /// <summary>
        /// Report theme: white background, black text. Optimized for printing and reports.
        /// </summary>
        public static OxyPlotTheme ReportTheme { get; } = new OxyPlotTheme
        {
            Background = OxyColors.White,
            PlotAreaBackground = OxyColors.White,
            PlotAreaBorderColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            TextColor = OxyColors.Black,
            TitleColor = OxyColors.Black,
            SubtitleColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            AxisTitleColor = OxyColors.Black,
            AxisLineColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            AxisTextColor = OxyColors.Black,
            AxisTickColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            MajorGridlineColor = OxyColor.FromRgb(0xE0, 0xE0, 0xE0),
            MinorGridlineColor = OxyColor.FromRgb(0xF0, 0xF0, 0xF0),
            LegendTitleColor = OxyColors.Black,
            LegendTextColor = OxyColor.FromRgb(0x33, 0x33, 0x33),
            LegendBackground = OxyColors.Transparent,
            LegendBorderColor = OxyColors.Transparent,
            ContourLabelBackground = OxyColor.FromArgb(220, 255, 255, 255),
        };

        /// <summary>
        /// Returns the OxyPlot theme corresponding to the given application theme.
        /// Light and Blue app themes both map to the Light OxyPlot theme.
        /// </summary>
        public static OxyPlotTheme GetThemeFor(Themes.Theme appTheme)
        {
            return appTheme == Themes.Theme.Dark ? DarkTheme : LightTheme;
        }

        /// <summary>
        /// Applies the specified theme to a Plot control, setting all theme-controlled properties
        /// on the plot, axes, legend, and annotations. Series colors are not modified.
        /// </summary>
        public static void ApplyTheme(Wpf.Plot plot, OxyPlotTheme theme)
        {
            if (plot?.ActualModel == null) return;

            // Suppress PropertyChanged on all child elements during theme application.
            // Plot.SuppressPropertyChanged is already set by the caller (OxyPlotToolbar_Loaded),
            // but axes, series, and annotations need their own suppression to prevent
            // UndoableStateBridge from recording theme-induced property changes.
            foreach (Wpf.Axis axis in plot.Axes) axis.SuppressPropertyChanged = true;
            foreach (Wpf.Series series in plot.Series) series.SuppressPropertyChanged = true;
            try
            {

            // Background: resolve from EnvironmentWindowBackground if theme uses Undefined
            if (theme.Background == OxyColors.Undefined)
            {
                var bgBrush = ResolveEnvironmentWindowBackground();
                if (bgBrush != null)
                {
                    plot.Background = bgBrush;
                    // Also set model-level background for export (PngExporter uses model.Background)
                    plot.ActualModel.Background = OxyColor.FromArgb(bgBrush.Color.A, bgBrush.Color.R, bgBrush.Color.G, bgBrush.Color.B);
                }
            }
            else
            {
                plot.Background = new SolidColorBrush(ToMediaColor(theme.Background));
                plot.ActualModel.Background = theme.Background;
            }

            // Plot area
            plot.PlotAreaBackground = new SolidColorBrush(ToMediaColor(theme.PlotAreaBackground));
            plot.PlotAreaBorderColor = ToMediaColor(theme.PlotAreaBorderColor);
            plot.ActualModel.PlotAreaBackground = theme.PlotAreaBackground;
            plot.ActualModel.PlotAreaBorderColor = theme.PlotAreaBorderColor;

            // Text color (fallback for Automatic title/subtitle/axis colors)
            plot.TextColor = ToMediaColor(theme.TextColor);
            plot.ActualModel.TextColor = theme.TextColor;

            // Title & Subtitle
            plot.TitleColor = ToMediaColor(theme.TitleColor);
            plot.SubtitleColor = ToMediaColor(theme.SubtitleColor);
            plot.ActualModel.TitleColor = theme.TitleColor;
            plot.ActualModel.SubtitleColor = theme.SubtitleColor;

            // Axes (WPF wrapper)
            foreach (Wpf.Axis axis in plot.Axes)
            {
                axis.TitleColor = ToMediaColor(theme.AxisTitleColor);
                axis.TextColor = ToMediaColor(theme.AxisTextColor);
                axis.AxislineColor = ToMediaColor(theme.AxisLineColor);
                axis.TicklineColor = ToMediaColor(theme.AxisTickColor);
                axis.MajorGridlineColor = ToMediaColor(theme.MajorGridlineColor);
                axis.MinorGridlineColor = ToMediaColor(theme.MinorGridlineColor);
            }

            // Axes (model-level for export)
            foreach (var modelAxis in plot.ActualModel.Axes)
            {
                modelAxis.TitleColor = theme.AxisTitleColor;
                modelAxis.TextColor = theme.AxisTextColor;
                modelAxis.AxislineColor = theme.AxisLineColor;
                modelAxis.TicklineColor = theme.AxisTickColor;
                modelAxis.MajorGridlineColor = theme.MajorGridlineColor;
                modelAxis.MinorGridlineColor = theme.MinorGridlineColor;
            }

            // Legend (WPF wrapper)
            plot.LegendTitleColor = ToMediaColor(theme.LegendTitleColor);
            plot.LegendTextColor = ToMediaColor(theme.LegendTextColor);
            plot.LegendBackground = ToMediaColor(theme.LegendBackground);
            plot.LegendBorder = ToMediaColor(theme.LegendBorderColor);

            // Legend (model-level for export)
            foreach (var legend in plot.ActualModel.Legends)
            {
                legend.LegendTitleColor = theme.LegendTitleColor;
                legend.LegendTextColor = theme.LegendTextColor;
                legend.LegendBackground = theme.LegendBackground;
                legend.LegendBorder = theme.LegendBorderColor;
            }

            // Contour series (WPF wrapper)
            foreach (var s in plot.Series)
            {
                if (s is Wpf.ContourSeries contour)
                {
                    contour.LabelBackground = ToMediaColor(theme.ContourLabelBackground);
                }
            }

            // Contour series (model-level for export)
            foreach (var s in plot.ActualModel.Series)
            {
                if (s is OxyPlot.Series.ContourSeries contourModel)
                {
                    contourModel.LabelBackground = theme.ContourLabelBackground;
                }
            }

            }
            finally
            {
                foreach (Wpf.Axis axis in plot.Axes) axis.SuppressPropertyChanged = false;
                foreach (Wpf.Series series in plot.Series) series.SuppressPropertyChanged = false;
            }

            plot.InvalidatePlot(false);
        }

        /// <summary>
        /// Shows a confirmation dialog warning the user that theme changes will overwrite plot settings.
        /// Includes a "Don't show this message again" checkbox.
        /// Returns true if the user confirms (or if the warning was previously suppressed).
        /// </summary>
        public static bool ConfirmThemeChange(Window owner)
        {
            var result = GenericControls.MessageBox.Show(
                owner,
                "Changing the plot theme will overwrite current plot colors and styles. Do you want to continue?",
                "Plot Theme Change",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning,
                ThemeChangeWarningKey,
                _suppressionStore,
                MessageBoxResult.OK);

            return result == MessageBoxResult.OK;
        }

        /// <summary>
        /// Temporarily applies a theme to the given PlotModel, executes an action (e.g., render/export),
        /// then restores the original colors. This avoids the complexity of deep-cloning a PlotModel.
        /// </summary>
        public static void WithThemedModel(PlotModel model, OxyPlotTheme theme, System.Action<PlotModel> action)
        {
            if (model == null) return;

            // Snapshot original model-level colors
            var origBackground = model.Background;
            var origPlotAreaBackground = model.PlotAreaBackground;
            var origPlotAreaBorderColor = model.PlotAreaBorderColor;
            var origTitleColor = model.TitleColor;
            var origSubtitleColor = model.SubtitleColor;

            // Snapshot legend colors
            var legendSnapshots = new List<(LegendBase legend, OxyColor titleColor, OxyColor textColor, OxyColor background, OxyColor border)>();
            foreach (var legend in model.Legends)
            {
                legendSnapshots.Add((legend, legend.LegendTitleColor, legend.LegendTextColor, legend.LegendBackground, legend.LegendBorder));
            }

            // Snapshot original axis colors
            var axisSnapshots = new List<(OxyPlot.Axes.Axis axis, OxyColor titleColor, OxyColor textColor, OxyColor axisLineColor,
                OxyColor tickColor, OxyColor majorGridColor, OxyColor minorGridColor)>();
            foreach (var axis in model.Axes)
            {
                axisSnapshots.Add((axis, axis.TitleColor, axis.TextColor, axis.AxislineColor, axis.TicklineColor,
                    axis.MajorGridlineColor, axis.MinorGridlineColor));
            }

            // Snapshot original contour-series label backgrounds
            var contourSnapshots = new List<(OxyPlot.Series.ContourSeries series, OxyColor labelBackground)>();
            foreach (var s in model.Series)
            {
                if (s is OxyPlot.Series.ContourSeries contourModel)
                {
                    contourSnapshots.Add((contourModel, contourModel.LabelBackground));
                }
            }

            try
            {
                // Apply theme
                model.Background = theme.Background == OxyColors.Undefined ? OxyColors.White : theme.Background;
                model.PlotAreaBackground = theme.PlotAreaBackground;
                model.PlotAreaBorderColor = theme.PlotAreaBorderColor;
                model.TitleColor = theme.TitleColor;
                model.SubtitleColor = theme.SubtitleColor;
                foreach (var legend in model.Legends)
                {
                    legend.LegendTitleColor = theme.LegendTitleColor;
                    legend.LegendTextColor = theme.LegendTextColor;
                    legend.LegendBackground = theme.LegendBackground;
                    legend.LegendBorder = theme.LegendBorderColor;
                }

                foreach (var axis in model.Axes)
                {
                    axis.TitleColor = theme.AxisTitleColor;
                    axis.TextColor = theme.AxisTextColor;
                    axis.AxislineColor = theme.AxisLineColor;
                    axis.TicklineColor = theme.AxisTickColor;
                    axis.MajorGridlineColor = theme.MajorGridlineColor;
                    axis.MinorGridlineColor = theme.MinorGridlineColor;
                }

                foreach (var (contourModel, _) in contourSnapshots)
                {
                    contourModel.LabelBackground = theme.ContourLabelBackground;
                }

                // Execute the action with the themed model
                action(model);
            }
            finally
            {
                // Restore original colors
                model.Background = origBackground;
                model.PlotAreaBackground = origPlotAreaBackground;
                model.PlotAreaBorderColor = origPlotAreaBorderColor;
                model.TitleColor = origTitleColor;
                model.SubtitleColor = origSubtitleColor;

                foreach (var (legend, titleColor, textColor, background, border) in legendSnapshots)
                {
                    legend.LegendTitleColor = titleColor;
                    legend.LegendTextColor = textColor;
                    legend.LegendBackground = background;
                    legend.LegendBorder = border;
                }

                foreach (var (axis, titleColor, textColor, axisLineColor, tickColor, majorGridColor, minorGridColor) in axisSnapshots)
                {
                    axis.TitleColor = titleColor;
                    axis.TextColor = textColor;
                    axis.AxislineColor = axisLineColor;
                    axis.TicklineColor = tickColor;
                    axis.MajorGridlineColor = majorGridColor;
                    axis.MinorGridlineColor = minorGridColor;
                }

                foreach (var (contourModel, labelBackground) in contourSnapshots)
                {
                    contourModel.LabelBackground = labelBackground;
                }
            }
        }

        /// <summary>
        /// Resolves the EnvironmentWindowBackground brush from the application's theme resources.
        /// Returns null if the resource cannot be found.
        /// </summary>
        private static SolidColorBrush? ResolveEnvironmentWindowBackground()
        {
            try
            {
                var resource = Application.Current?.FindResource("EnvironmentWindowBackground");
                return resource as SolidColorBrush;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Converts an OxyColor to a WPF Color. The OxyPlot.Wpf wrapper properties
        /// (TitleColor, AxislineColor, etc.) use System.Windows.Media.Color.
        /// </summary>
        private static Color ToMediaColor(OxyColor c)
        {
            return Color.FromArgb(c.A, c.R, c.G, c.B);
        }
    }
}
