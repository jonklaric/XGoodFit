// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AreaSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.AreaSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.AreaSeries"/>.
    /// </summary>
    /// <remarks>
    /// An area series fills the polygon defined by two sets of points or one set of points and a constant baseline.
    /// This class provides WPF dependency properties for all AreaSeries properties.
    /// XGoodFit port: its dependency properties are Avalonia styled properties with the same names, types
    /// and defaults, and their change callbacks are registered in the static constructor.
    /// </remarks>
    public class AreaSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="Color2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> Color2Property = AvaloniaProperty.Register<AreaSeries, Color>(
            nameof(Color2),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="ConstantY2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> ConstantY2Property = AvaloniaProperty.Register<AreaSeries, double>(
            nameof(ConstantY2),
            0.0);

        /// <summary>
        /// Identifies the <see cref="DataFieldX2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldX2Property = AvaloniaProperty.Register<AreaSeries, string>(
            nameof(DataFieldX2),
            null);

        /// <summary>
        /// Identifies the <see cref="DataFieldY2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> DataFieldY2Property = AvaloniaProperty.Register<AreaSeries, string>(
            nameof(DataFieldY2),
            null);

        /// <summary>
        /// Identifies the <see cref="Fill"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> FillProperty = AvaloniaProperty.Register<AreaSeries, Color>(
            nameof(Fill),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="Reverse2"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> Reverse2Property = AvaloniaProperty.Register<AreaSeries, bool>(
            nameof(Reverse2),
            true);

        /// <summary>
        /// Initializes static members of the <see cref="AreaSeries" /> class.
        /// </summary>
        /// <remarks>XGoodFit port: registers the property-changed callbacks of the styled properties.</remarks>
        static AreaSeries()
        {
            Color2Property.Changed.AddClassHandler<AreaSeries>(AppearanceChanged);
            ConstantY2Property.Changed.AddClassHandler<AreaSeries>(DataChanged);
            DataFieldX2Property.Changed.AddClassHandler<AreaSeries>(DataChanged);
            DataFieldY2Property.Changed.AddClassHandler<AreaSeries>(DataChanged);
            FillProperty.Changed.AddClassHandler<AreaSeries>(AppearanceChanged);
            Reverse2Property.Changed.AddClassHandler<AreaSeries>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AreaSeries" /> class.
        /// </summary>
        public AreaSeries()
        {
            this.InternalSeries = new OxyPlot.Series.AreaSeries();
        }

        /// <summary>
        /// Gets or sets the color of the second (baseline) line. The default is <see cref="MoreColors.Automatic"/>.
        /// </summary>
        /// <value>The color of the second line.</value>
        /// <remarks>
        /// When set to <see cref="MoreColors.Automatic"/>, the color will match the primary line color.
        /// </remarks>
        public Color Color2
        {
            get => (Color)this.GetValue(Color2Property);
            set => this.SetValue(Color2Property, value);
        }

        /// <summary>
        /// Gets or sets the constant Y value for the baseline. The default is <c>0</c>.
        /// </summary>
        /// <value>The Y coordinate of a horizontal baseline.</value>
        /// <remarks>
        /// This value is used when <see cref="DataFieldX2"/> and <see cref="DataFieldY2"/> are not set,
        /// creating a horizontal baseline at the specified Y coordinate.
        /// </remarks>
        public double ConstantY2
        {
            get => (double)this.GetValue(ConstantY2Property);
            set => this.SetValue(ConstantY2Property, value);
        }

        /// <summary>
        /// Gets or sets the data field for X coordinates of the second data set. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides X values for the baseline.
        /// </value>
        /// <remarks>
        /// Both <see cref="DataFieldX2"/> and <see cref="DataFieldY2"/> must be set to use a second data set.
        /// </remarks>
        public string DataFieldX2
        {
            get => (string)this.GetValue(DataFieldX2Property);
            set => this.SetValue(DataFieldX2Property, value);
        }

        /// <summary>
        /// Gets or sets the data field for Y coordinates of the second data set. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in the ItemsSource that provides Y values for the baseline.
        /// </value>
        /// <remarks>
        /// Both <see cref="DataFieldX2"/> and <see cref="DataFieldY2"/> must be set to use a second data set.
        /// </remarks>
        public string DataFieldY2
        {
            get => (string)this.GetValue(DataFieldY2Property);
            set => this.SetValue(DataFieldY2Property, value);
        }

        /// <summary>
        /// Gets or sets the fill color of the area. The default is <see cref="MoreColors.Automatic"/>.
        /// </summary>
        /// <value>The fill color for the area between the two lines.</value>
        /// <remarks>
        /// When set to <see cref="MoreColors.Automatic"/>, a semi-transparent version of the line color is used.
        /// </remarks>
        public Color Fill
        {
            get => (Color)this.GetValue(FillProperty);
            set => this.SetValue(FillProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the second data set should be reversed. The default is <c>true</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> if the second data set should be reversed when creating the polygon; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// The second data set is typically reversed to create a properly closed polygon for the filled area.
        /// </remarks>
        public bool Reverse2
        {
            get => (bool)this.GetValue(Reverse2Property);
            set => this.SetValue(Reverse2Property, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Series.AreaSeries"/> model.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="series">The internal OxyPlot series to update.</param>
        protected override void SynchronizeProperties(OxyPlot.Series.Series series)
        {
            base.SynchronizeProperties(series);

            if (series is OxyPlot.Series.AreaSeries s)
            {
                s.Color2 = this.Color2.ToOxyColor();
                s.ConstantY2 = this.ConstantY2;
                s.DataFieldX2 = this.DataFieldX2;
                s.DataFieldY2 = this.DataFieldY2;
                s.Fill = this.Fill.ToOxyColor();
                s.Reverse2 = this.Reverse2;
            }
        }
    }
}
