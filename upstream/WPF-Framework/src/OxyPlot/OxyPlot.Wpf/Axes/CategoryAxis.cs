// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategoryAxis.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF wrapper for OxyPlot.Axes.CategoryAxis.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Windows;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a WPF wrapper for <see cref="OxyPlot.Axes.CategoryAxis"/>.
    /// </summary>
    /// <remarks>
    /// A category axis displays discrete category labels. This is useful for bar charts,
    /// grouped data, or any chart where the x-axis represents distinct categories.
    /// </remarks>
    public class CategoryAxis : LinearAxis
    {
        /// <summary>
        /// Identifies the <see cref="GapWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GapWidthProperty = DependencyProperty.Register(
            nameof(GapWidth),
            typeof(double),
            typeof(CategoryAxis),
            new PropertyMetadata(1.0, DataChanged));

        /// <summary>
        /// Identifies the <see cref="IsTickCentered"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTickCenteredProperty = DependencyProperty.Register(
            nameof(IsTickCentered),
            typeof(bool),
            typeof(CategoryAxis),
            new PropertyMetadata(false, DataChanged));

        /// <summary>
        /// Identifies the <see cref="ItemsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(CategoryAxis),
            new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="LabelField"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelFieldProperty = DependencyProperty.Register(
            nameof(LabelField),
            typeof(string),
            typeof(CategoryAxis),
            new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Identifies the <see cref="Labels"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
            nameof(Labels),
            typeof(IList<string>),
            typeof(CategoryAxis),
            new PropertyMetadata(null, DataChanged));

        /// <summary>
        /// Initializes static members of the <see cref="CategoryAxis"/> class.
        /// </summary>
        static CategoryAxis()
        {
            PositionProperty.OverrideMetadata(typeof(CategoryAxis), new PropertyMetadata(AxisPosition.Bottom, DataChanged));
            MinimumPaddingProperty.OverrideMetadata(typeof(CategoryAxis), new PropertyMetadata(0.0, DataChanged));
            MaximumPaddingProperty.OverrideMetadata(typeof(CategoryAxis), new PropertyMetadata(0.0, DataChanged));
            MajorStepProperty.OverrideMetadata(typeof(CategoryAxis), new PropertyMetadata(1.0, DataChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CategoryAxis"/> class.
        /// </summary>
        public CategoryAxis()
        {
            this.InternalAxis = new OxyPlot.Axes.CategoryAxis();
        }

        /// <summary>
        /// Gets or sets the gap width between categories. The default is <c>1</c>.
        /// </summary>
        /// <value>
        /// The relative width of the gap between categories. A value of 0 means no gap,
        /// while 1 means the gap is as wide as the category.
        /// </value>
        public double GapWidth
        {
            get => (double)this.GetValue(GapWidthProperty);
            set => this.SetValue(GapWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether tick marks are centered on categories. The default is <c>false</c>.
        /// </summary>
        /// <value>
        /// <c>true</c> if tick marks should be centered on categories; otherwise, <c>false</c>.
        /// </value>
        public bool IsTickCentered
        {
            get => (bool)this.GetValue(IsTickCenteredProperty);
            set => this.SetValue(IsTickCenteredProperty, value);
        }

        /// <summary>
        /// Gets or sets the items source for category labels. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A collection of items from which category labels are extracted using <see cref="LabelField"/>.
        /// </value>
        public IEnumerable ItemsSource
        {
            get => (IEnumerable)this.GetValue(ItemsSourceProperty);
            set => this.SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the name of the property to use for labels. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The name of the property on items in <see cref="ItemsSource"/> that provides label text.
        /// </value>
        public string LabelField
        {
            get => (string)this.GetValue(LabelFieldProperty);
            set => this.SetValue(LabelFieldProperty, value);
        }

        /// <summary>
        /// Gets or sets the category labels. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// A list of string labels for each category. Used when <see cref="ItemsSource"/> is not set.
        /// </value>
        public IList<string> Labels
        {
            get => (IList<string>)this.GetValue(LabelsProperty);
            set => this.SetValue(LabelsProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot axis model.
        /// </summary>
        /// <returns>The <see cref="OxyPlot.Axes.CategoryAxis"/> model.</returns>
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

            if (this.InternalAxis is OxyPlot.Axes.CategoryAxis a)
            {
                a.GapWidth = this.GapWidth;
                a.IsTickCentered = this.IsTickCentered;
                a.ItemsSource = this.ItemsSource;
                a.LabelField = this.LabelField;

                if (this.Labels != null && this.ItemsSource == null)
                {
                    a.Labels.Clear();
                    a.Labels.AddRange(this.Labels);
                }
            }
        }
    }
}
