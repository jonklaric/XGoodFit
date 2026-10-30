// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategoryColorAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.CategoryColorAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections.Generic;
    using System.Linq;
    using Avalonia;
    using Avalonia.Metadata;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.CategoryColorAxis"/>.
    /// </summary>
    /// <remarks>
    /// A category color axis maps category indices to colors using a color palette.
    /// Each category is assigned a unique color from the palette.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor; the XAML content
    /// property is marked on the property (Avalonia's <c>[Content]</c>) rather than on the class.
    /// </remarks>
    public class CategoryColorAxis : CategoryAxis
    {
        /// <summary>
        /// Identifies the <see cref="Palette"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IList<Color>> PaletteProperty = AvaloniaProperty.Register<CategoryColorAxis, IList<Color>>(
            nameof(Palette),
            null);

        /// <summary>
        /// Identifies the <see cref="InvalidCategoryColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> InvalidCategoryColorProperty = AvaloniaProperty.Register<CategoryColorAxis, Color>(
            nameof(InvalidCategoryColor),
            MoreColors.Undefined);

        /// <summary>
        /// Initializes static members of the <see cref="CategoryColorAxis" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static CategoryColorAxis()
        {
            PaletteProperty.Changed.AddClassHandler<CategoryColorAxis>(AppearanceChanged);
            InvalidCategoryColorProperty.Changed.AddClassHandler<CategoryColorAxis>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryColorAxis"/> class.
        /// </summary>
        public CategoryColorAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.CategoryColorAxis();
        }

        /// <summary>
        /// Gets or sets the color palette for categories.
        /// </summary>
        /// <value>A list of colors, one for each category. The default is <c>null</c>.</value>
        [Content]
        public IList<Color> Palette
        {
            get => (IList<Color>)this.GetValue(PaletteProperty);
            set => this.SetValue(PaletteProperty, value);
        }

        /// <summary>
        /// Gets or sets the color used for invalid category indices.
        /// </summary>
        /// <value>The invalid category color. The default is <see cref="MoreColors.Undefined"/>.</value>
        public Color InvalidCategoryColor
        {
            get => (Color)this.GetValue(InvalidCategoryColorProperty);
            set => this.SetValue(InvalidCategoryColorProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.CategoryColorAxis"/> model.</returns>
        public override OxyPlot.Axes.Axis CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAxis;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot axis.
        /// </summary>
        protected override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAxis is OxyPlot.Axes.CategoryColorAxis a)
            {
                a.InvalidCategoryColor = this.InvalidCategoryColor.ToOxyColor();

                if (this.Palette != null && this.Palette.Count > 0)
                {
                    a.Palette = new OxyPalette(this.Palette.Select(c => c.ToOxyColor()));
                }
            }
        }
    }
}
