using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace Themes
{
    /// <summary>
    /// Whether a grid shows its horizontal and vertical grid lines, for the DataGrid templates.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition. WPF draws the grid lines when it renders, with the grid's current brushes, and
    /// its cells leave their right and bottom pixels for them when the grid shows those lines, so a row's
    /// height includes its line. The ported templates draw the lines as elements bound to the grid's
    /// brushes and <c>GridLinesVisibility</c>, through these converters.
    /// </remarks>
    public static class DataGridGridLines
    {
        /// <summary>Converts a grid's <c>GridLinesVisibility</c> to whether it shows horizontal lines.</summary>
        public static IValueConverter ShowsHorizontal { get; } =
            new FuncValueConverter<DataGridGridLinesVisibility, bool>(
                lines => lines == DataGridGridLinesVisibility.Horizontal || lines == DataGridGridLinesVisibility.All);

        /// <summary>Converts a grid's <c>GridLinesVisibility</c> to whether it shows vertical lines.</summary>
        public static IValueConverter ShowsVertical { get; } =
            new FuncValueConverter<DataGridGridLinesVisibility, bool>(
                lines => lines == DataGridGridLinesVisibility.Vertical || lines == DataGridGridLinesVisibility.All);
    }
}
