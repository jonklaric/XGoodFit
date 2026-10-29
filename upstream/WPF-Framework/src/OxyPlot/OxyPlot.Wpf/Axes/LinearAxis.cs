// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LinearAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.LinearAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Windows;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.LinearAxis"/>.
    /// </summary>
    /// <remarks>
    /// A linear axis displays continuous numeric values with uniform spacing.
    /// This is the most common axis type for general-purpose charts.
    /// </remarks>
    public class LinearAxis : Axis
    {
        /// <summary>
        /// Identifies the <see cref="FormatAsFractions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FormatAsFractionsProperty = DependencyProperty.Register(
            nameof(FormatAsFractions),
            typeof(bool),
            typeof(LinearAxis),
            new PropertyMetadata(false, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FractionUnit"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FractionUnitProperty = DependencyProperty.Register(
            nameof(FractionUnit),
            typeof(double),
            typeof(LinearAxis),
            new PropertyMetadata(1.0, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="FractionUnitSymbol"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FractionUnitSymbolProperty = DependencyProperty.Register(
            nameof(FractionUnitSymbol),
            typeof(string),
            typeof(LinearAxis),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="LinearAxis"/> class.
        /// </summary>
        public LinearAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.LinearAxis();
        }

        /// <summary>
        /// Gets or sets a value indicating whether to format values as fractions. The default is <c>false</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> to format axis labels as fractions; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When enabled, values are displayed as fractions of <see cref="FractionUnit"/>.
        /// </remarks>
        public bool FormatAsFractions
        {
            get => (bool)this.GetValue(FormatAsFractionsProperty);
            set => this.SetValue(FormatAsFractionsProperty, value);
        }

        /// <summary>
        /// Gets or sets the fraction unit. The default is <c>1</c>.
        /// </summary>
        /// <value>The unit to use when formatting values as fractions.</value>
        public double FractionUnit
        {
            get => (double)this.GetValue(FractionUnitProperty);
            set => this.SetValue(FractionUnitProperty, value);
        }

        /// <summary>
        /// Gets or sets the fraction unit symbol. The default is <c>null</c>.
        /// </summary>
        /// <value>The symbol to display with fractional values (e.g., "π" for pi).</value>
        public string FractionUnitSymbol
        {
            get => (string)this.GetValue(FractionUnitSymbolProperty);
            set => this.SetValue(FractionUnitSymbolProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.LinearAxis"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.LinearAxis a)
            {
                a.FormatAsFractions = this.FormatAsFractions;
                a.FractionUnit = this.FractionUnit;
                a.FractionUnitSymbol = this.FractionUnitSymbol;
            }
        }
    }
}
