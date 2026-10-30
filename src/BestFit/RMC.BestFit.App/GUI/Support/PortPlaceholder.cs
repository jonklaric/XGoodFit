using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: what the placeholder views show (the files marked "XGoodFit port: placeholder"),
    /// while their GUI areas are ported. Delete it with the last placeholder.
    /// </summary>
    internal static class PortPlaceholder
    {
        /// <summary>The placeholder's content: a note that the area isn't ported yet.</summary>
        /// <param name="area">The GUI area, as the user knows it.</param>
        /// <returns>The content.</returns>
        public static Control Create(string area) => new TextBlock
        {
            Text = $"The {area} view isn't ported to XGoodFit yet.",
            Margin = new Thickness(10),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
    }
}
