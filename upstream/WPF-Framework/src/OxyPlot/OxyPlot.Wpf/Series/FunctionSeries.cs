// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Series.FunctionSeries.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Windows;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Series.FunctionSeries"/>.
    /// </summary>
    /// <remarks>
    /// A function series generates its dataset from a mathematical function.
    /// Define f(x) to plot y=f(x), or define x(t) and y(t) for parametric curves.
    /// </remarks>
    public class FunctionSeries : LineSeries
    {
        /// <summary>
        /// Identifies the <see cref="Function"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FunctionProperty =
            DependencyProperty.Register(
                nameof(Function),
                typeof(Func<double, double>),
                typeof(FunctionSeries),
                new PropertyMetadata(null, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="FunctionX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FunctionXProperty =
            DependencyProperty.Register(
                nameof(FunctionX),
                typeof(Func<double, double>),
                typeof(FunctionSeries),
                new PropertyMetadata(null, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="FunctionY"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FunctionYProperty =
            DependencyProperty.Register(
                nameof(FunctionY),
                typeof(Func<double, double>),
                typeof(FunctionSeries),
                new PropertyMetadata(null, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumXProperty =
            DependencyProperty.Register(
                nameof(MinimumX),
                typeof(double),
                typeof(FunctionSeries),
                new PropertyMetadata(0.0, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumX"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumXProperty =
            DependencyProperty.Register(
                nameof(MaximumX),
                typeof(double),
                typeof(FunctionSeries),
                new PropertyMetadata(1.0, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="MinimumT"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumTProperty =
            DependencyProperty.Register(
                nameof(MinimumT),
                typeof(double),
                typeof(FunctionSeries),
                new PropertyMetadata(0.0, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="MaximumT"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumTProperty =
            DependencyProperty.Register(
                nameof(MaximumT),
                typeof(double),
                typeof(FunctionSeries),
                new PropertyMetadata(1.0, OnFunctionChanged));

        /// <summary>
        /// Identifies the <see cref="Resolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register(
                nameof(Resolution),
                typeof(int),
                typeof(FunctionSeries),
                new PropertyMetadata(100, OnFunctionChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionSeries"/> class.
        /// </summary>
        public FunctionSeries()
        {
            this.InternalSeries = new OxyPlot.Series.FunctionSeries();
        }

        /// <summary>
        /// Gets or sets the function y=f(x).
        /// </summary>
        /// <value>The function. The default is <c>null</c>.</value>
        /// <remarks>
        /// When set, points are generated using this function over the range [MinimumX, MaximumX].
        /// </remarks>
        public Func<double, double> Function
        {
            get => (Func<double, double>)this.GetValue(FunctionProperty);
            set => this.SetValue(FunctionProperty, value);
        }

        /// <summary>
        /// Gets or sets the parametric function x(t).
        /// </summary>
        /// <value>The x function. The default is <c>null</c>.</value>
        /// <remarks>
        /// When set along with <see cref="FunctionY"/>, points are generated parametrically.
        /// </remarks>
        public Func<double, double> FunctionX
        {
            get => (Func<double, double>)this.GetValue(FunctionXProperty);
            set => this.SetValue(FunctionXProperty, value);
        }

        /// <summary>
        /// Gets or sets the parametric function y(t).
        /// </summary>
        /// <value>The y function. The default is <c>null</c>.</value>
        /// <remarks>
        /// When set along with <see cref="FunctionX"/>, points are generated parametrically.
        /// </remarks>
        public Func<double, double> FunctionY
        {
            get => (Func<double, double>)this.GetValue(FunctionYProperty);
            set => this.SetValue(FunctionYProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum x value for f(x) functions.
        /// </summary>
        /// <value>The minimum x. The default is <c>0.0</c>.</value>
        public double MinimumX
        {
            get => (double)this.GetValue(MinimumXProperty);
            set => this.SetValue(MinimumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum x value for f(x) functions.
        /// </summary>
        /// <value>The maximum x. The default is <c>1.0</c>.</value>
        public double MaximumX
        {
            get => (double)this.GetValue(MaximumXProperty);
            set => this.SetValue(MaximumXProperty, value);
        }

        /// <summary>
        /// Gets or sets the minimum t parameter for parametric functions.
        /// </summary>
        /// <value>The minimum t. The default is <c>0.0</c>.</value>
        public double MinimumT
        {
            get => (double)this.GetValue(MinimumTProperty);
            set => this.SetValue(MinimumTProperty, value);
        }

        /// <summary>
        /// Gets or sets the maximum t parameter for parametric functions.
        /// </summary>
        /// <value>The maximum t. The default is <c>1.0</c>.</value>
        public double MaximumT
        {
            get => (double)this.GetValue(MaximumTProperty);
            set => this.SetValue(MaximumTProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of points to generate.
        /// </summary>
        /// <value>The resolution. The default is <c>100</c>.</value>
        public int Resolution
        {
            get => (int)this.GetValue(ResolutionProperty);
            set => this.SetValue(ResolutionProperty, value);
        }

        /// <summary>
        /// Called when function-related properties change.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event arguments.</param>
        private static void OnFunctionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FunctionSeries)d).GeneratePoints();
            DataChanged(d, e);
        }

        /// <summary>
        /// Generates points from the function(s).
        /// </summary>
        private void GeneratePoints()
        {
            if (!(this.InternalSeries is OxyPlot.Series.LineSeries s))
            {
                return;
            }

            s.Points.Clear();

            if (this.Function != null && this.Resolution > 1)
            {
                // f(x) mode
                double dx = (this.MaximumX - this.MinimumX) / (this.Resolution - 1);
                for (double x = this.MinimumX; x <= this.MaximumX + (dx * 0.5); x += dx)
                {
                    s.Points.Add(new DataPoint(x, this.Function(x)));
                }
            }
            else if (this.FunctionX != null && this.FunctionY != null && this.Resolution > 1)
            {
                // Parametric mode: x(t), y(t)
                double dt = (this.MaximumT - this.MinimumT) / (this.Resolution - 1);
                for (double t = this.MinimumT; t <= this.MaximumT + (dt * 0.5); t += dt)
                {
                    s.Points.Add(new DataPoint(this.FunctionX(t), this.FunctionY(t)));
                }
            }
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>A <see cref="OxyPlot.Series.FunctionSeries"/> instance.</returns>
        public override OxyPlot.Series.Series CreateModel()
        {
            this.GeneratePoints();
            this.SynchronizeProperties(this.InternalSeries);
            return this.InternalSeries;
        }
    }
}
