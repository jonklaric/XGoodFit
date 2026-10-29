// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FunctionAnnotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Annotations.FunctionAnnotation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Windows;

    using OxyPlot.Annotations;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Annotations.FunctionAnnotation"/>.
    /// </summary>
    /// <remarks>
    /// A function annotation renders a mathematical function as a path on the plot.
    /// The function can be either y=f(x) or x=f(y) depending on the type.
    /// </remarks>
    public class FunctionAnnotation : PathAnnotation
    {
        /// <summary>
        /// Identifies the <see cref="Type"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(
                nameof(Type),
                typeof(FunctionAnnotationType),
                typeof(FunctionAnnotation),
                new PropertyMetadata(FunctionAnnotationType.EquationX, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Equation"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EquationProperty =
            DependencyProperty.Register(
                nameof(Equation),
                typeof(Func<double, double>),
                typeof(FunctionAnnotation),
                new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Resolution"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ResolutionProperty =
            DependencyProperty.Register(
                nameof(Resolution),
                typeof(int),
                typeof(FunctionAnnotation),
                new PropertyMetadata(400, AppearanceChanged));

        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionAnnotation"/> class.
        /// </summary>
        public FunctionAnnotation()
        {
            this.InternalAnnotation = new Annotations.FunctionAnnotation();
        }

        /// <summary>
        /// Gets or sets the type of function.
        /// </summary>
        /// <value>The function type. The default is <see cref="FunctionAnnotationType.EquationX"/>.</value>
        /// <remarks>
        /// Use <see cref="FunctionAnnotationType.EquationX"/> for y=f(x) functions.
        /// Use <see cref="FunctionAnnotationType.EquationY"/> for x=f(y) functions.
        /// </remarks>
        public FunctionAnnotationType Type
        {
            get => (FunctionAnnotationType)this.GetValue(TypeProperty);
            set => this.SetValue(TypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the equation function.
        /// </summary>
        /// <value>The equation function. The default is <c>null</c>.</value>
        public Func<double, double> Equation
        {
            get => (Func<double, double>)this.GetValue(EquationProperty);
            set => this.SetValue(EquationProperty, value);
        }

        /// <summary>
        /// Gets or sets the resolution (number of points) for rendering the function.
        /// </summary>
        /// <value>The resolution. The default is <c>400</c>.</value>
        public int Resolution
        {
            get => (int)this.GetValue(ResolutionProperty);
            set => this.SetValue(ResolutionProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>An <see cref="Annotations.FunctionAnnotation"/> instance.</returns>
        public override Annotations.Annotation CreateModel()
        {
            this.SynchronizeProperties();
            return this.InternalAnnotation;
        }

        /// <summary>
        /// Synchronizes the WPF properties with the internal OxyPlot annotation.
        /// </summary>
        public override void SynchronizeProperties()
        {
            base.SynchronizeProperties();

            if (this.InternalAnnotation is Annotations.FunctionAnnotation a)
            {
                a.Type = this.Type;
                a.Equation = this.Equation;
                a.Resolution = this.Resolution;
            }
        }
    }
}
