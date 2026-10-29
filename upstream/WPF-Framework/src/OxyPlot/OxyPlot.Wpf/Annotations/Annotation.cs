// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Annotation.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for WPF annotation wrappers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf
{
    using System.ComponentModel;
    using System.Windows;

    using OxyPlot.Annotations;

    /// <summary>
    /// Provides an abstract base class for WPF annotation wrappers that synchronize
    /// WPF dependency properties with OxyPlot core annotation objects.
    /// </summary>
    public abstract class Annotation : FrameworkElement, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when an appearance-related dependency property value changes.
        /// </summary>
        /// <remarks>
        /// This event is raised by the <see cref="AppearanceChanged"/> callback for visual
        /// properties (color, stroke, text, position, etc.). It is not raised for
        /// data-level changes. This enables integration with <c>UndoableStateBridge</c>
        /// for non-destructive undo/redo of annotation visual settings.
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets whether <see cref="PropertyChanged"/> events are suppressed.
        /// When true, no PropertyChanged events fire from this annotation instance.
        /// </summary>
        /// <remarks>
        /// Use this to suppress events during bulk operations such as theme application
        /// where the undo system should not record individual changes.
        /// </remarks>
        public bool SuppressPropertyChanged { get; set; }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event if not suppressed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (SuppressPropertyChanged) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for the specified property names, bypassing
        /// the <see cref="SuppressPropertyChanged"/> flag.
        /// </summary>
        /// <param name="propertyNames">The property names to raise change notifications for.</param>
        /// <remarks>
        /// Use this after a drag operation where <see cref="SuppressPropertyChanged"/> was true
        /// during the drag to batch all changes into a single set of notifications at mouse-up.
        /// This allows the undo system to capture the entire drag as one undoable operation.
        /// </remarks>
        public void RaisePropertyChanged(params string[] propertyNames)
        {
            foreach (var name in propertyNames)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Identifies the <see cref="Layer"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LayerProperty = DependencyProperty.Register(
            nameof(Layer),
            typeof(AnnotationLayer),
            typeof(Annotation),
            new PropertyMetadata(AnnotationLayer.AboveSeries, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="XAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XAxisKeyProperty = DependencyProperty.Register(
            nameof(XAxisKey),
            typeof(string),
            typeof(Annotation),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="YAxisKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YAxisKeyProperty = DependencyProperty.Register(
            nameof(YAxisKey),
            typeof(string),
            typeof(Annotation),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Gets or sets the rendering layer for this annotation. The default is <see cref="AnnotationLayer.AboveSeries"/>.
        /// </summary>
        /// <value>The layer that determines when the annotation is rendered relative to series.</value>
        public AnnotationLayer Layer
        {
            get => (AnnotationLayer)this.GetValue(LayerProperty);
            set => this.SetValue(LayerProperty, value);
        }

        /// <summary>
        /// Gets or sets the X axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>The key of the X axis to use for this annotation.</value>
        public string XAxisKey
        {
            get => (string)this.GetValue(XAxisKeyProperty);
            set => this.SetValue(XAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the Y axis key. The default is <c>null</c>.
        /// </summary>
        /// <value>The key of the Y axis to use for this annotation.</value>
        public string YAxisKey
        {
            get => (string)this.GetValue(YAxisKeyProperty);
            set => this.SetValue(YAxisKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets the internal OxyPlot annotation model.
        /// </summary>
        public OxyPlot.Annotations.Annotation InternalAnnotation { get; protected set; }

        /// <summary>
        /// Creates the internal OxyPlot annotation model.
        /// </summary>
        /// <returns>The OxyPlot annotation model.</returns>
        public abstract OxyPlot.Annotations.Annotation CreateModel();

        /// <summary>
        /// Synchronizes the WPF properties to the internal OxyPlot annotation.
        /// </summary>
        public virtual void SynchronizeProperties()
        {
            var a = this.InternalAnnotation;
            if (a == null)
            {
                return;
            }

            a.Layer = this.Layer;
            a.XAxisKey = this.XAxisKey;
            a.YAxisKey = this.YAxisKey;
            a.ToolTip = this.ToolTip as string;
        }

        /// <summary>
        /// Handles changes to appearance-related properties.
        /// </summary>
        /// <remarks>
        /// Always triggers a visual update via <see cref="IPlotView.InvalidatePlot"/> so that
        /// the annotation renders correctly even when <see cref="SuppressPropertyChanged"/> is
        /// true (e.g., during interactive annotation creation). The <see cref="PropertyChanged"/>
        /// event is only raised when suppression is off, keeping the undo system clean.
        /// </remarks>
        protected static void AppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = (Annotation)d;
            var pc = annotation.Parent as IPlotView;
            if (pc is Plot plot) plot._needsSynchronization = true;
            pc?.InvalidatePlot(false);
            if (annotation.SuppressPropertyChanged) return;
            annotation.OnPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Handles changes to data-related properties.
        /// </summary>
        protected static void DataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var annotation = (Annotation)d;
            var pc = annotation.Parent as IPlotView;
            if (pc is Plot plot) plot._needsSynchronization = true;
            pc?.InvalidatePlot(false);
        }
    }
}
