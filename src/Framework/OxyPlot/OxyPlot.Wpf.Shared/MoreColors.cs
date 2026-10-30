// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MoreColors.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Defines additional <see cref="Colors" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia.Media;

    /// <summary>
    /// Defines additional <see cref="Colors" />.
    /// </summary>
    /// <remarks>XGoodFit port: the colours are Avalonia's <see cref="Color"/>.</remarks>
    public static class MoreColors
    {
        /// <summary>
        /// The undefined color.
        /// </summary>
        public static readonly Color Undefined = Color.FromArgb(0, 0, 0, 0);

        /// <summary>
        /// The automatic color.
        /// </summary>
        public static readonly Color Automatic = Color.FromArgb(0, 0, 0, 1);
    }
}