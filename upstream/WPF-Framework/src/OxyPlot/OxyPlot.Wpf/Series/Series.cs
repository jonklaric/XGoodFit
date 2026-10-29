// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Series.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides an abstract base class for WPF series wrappers.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    /// <summary>
    /// Provides an abstract base class for WPF series wrappers that synchronize
    /// WPF dependency properties with OxyPlot core series objects.
    /// </summary>
    /// <remarks>
    /// This class extends <see cref="ItemsControl"/> to support ItemsSource binding
    /// and provides the synchronization pattern for WPF to OxyPlot property mapping.
    /// </remarks>
    public abstract class Series : ItemsControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when an appearance-related dependency property value changes.
        /// </summary>
        /// <remarks>
        /// This event is raised by the <see cref="AppearanceChanged"/> callback for visual
        /// properties (color, title, line style, markers, etc.). It is not raised for
        /// data-level changes such as ItemsSource updates or collection resets.
        /// This enables integration with <c>UndoableStateBridge</c> for non-destructive
        /// undo/redo of series visual settings.
        /// </remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets whether <see cref="PropertyChanged"/> events are suppressed.
        /// When true, no PropertyChanged events fire from this series instance.
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
        /// Identifies the <see cref="Color"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            nameof(Color),
            typeof(Color),
            typeof(Series),
            new PropertyMetadata(MoreColors.Automatic, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(Series),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="RenderInLegend"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RenderInLegendProperty = DependencyProperty.Register(
            nameof(RenderInLegend),
            typeof(bool),
            typeof(Series),
            new PropertyMetadata(true, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TrackerFormatString"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackerFormatStringProperty = DependencyProperty.Register(
            nameof(TrackerFormatString),
            typeof(string),
            typeof(Series),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="TrackerKey"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TrackerKeyProperty = DependencyProperty.Register(
            nameof(TrackerKey),
            typeof(string),
            typeof(Series),
            new PropertyMetadata(null, AppearanceChanged));

        /// <summary>
        /// Identifies the <see cref="IsHitTestEnabled"/> dependency property.
        /// </summary>
        /// <remarks>
        /// Uses <see cref="HitTestRoutingChanged"/> instead of <see cref="AppearanceChanged"/>
        /// or <see cref="DataChanged"/>. Toggling hit-test routing is neither a visual change
        /// nor a data change, so it must not trigger <c>InvalidatePlot</c> or fire
        /// <c>PropertyChanged</c>. Setting the DP directly updates the internal series so
        /// the change is picked up on the next tracker event without forcing a render.
        /// </remarks>
        public static readonly DependencyProperty IsHitTestEnabledProperty = DependencyProperty.Register(
            nameof(IsHitTestEnabled),
            typeof(bool),
            typeof(Series),
            new PropertyMetadata(true, HitTestRoutingChanged));

        /// <summary>
        /// Identifies the <see cref="EdgeRenderingMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EdgeRenderingModeProperty = DependencyProperty.Register(
            nameof(EdgeRenderingMode),
            typeof(EdgeRenderingMode),
            typeof(Series),
            new PropertyMetadata(EdgeRenderingMode.Automatic, AppearanceChanged));

        /// <summary>
        /// The event listener used to subscribe to ItemSource.CollectionChanged events.
        /// </summary>
        private readonly EventListener eventListener;

        /// <summary>
        /// Initializes static members of the <see cref="Series" /> class.
        /// </summary>
        static Series()
        {
            VisibilityProperty.OverrideMetadata(typeof(Series), new PropertyMetadata(Visibility.Visible, AppearanceChanged));
            BackgroundProperty.OverrideMetadata(typeof(Series), new FrameworkPropertyMetadata(null, AppearanceChanged));
            FontFamilyProperty.OverrideMetadata(typeof(Series), new FrameworkPropertyMetadata(new FontFamily("Segoe UI"), AppearanceChanged));
            FontSizeProperty.OverrideMetadata(typeof(Series), new FrameworkPropertyMetadata(AppearanceChanged));
            FontWeightProperty.OverrideMetadata(typeof(Series), new FrameworkPropertyMetadata(AppearanceChanged));
            ForegroundProperty.OverrideMetadata(typeof(Series), new FrameworkPropertyMetadata(AppearanceChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Series" /> class.
        /// </summary>
        protected Series()
        {
            this.eventListener = new EventListener(this.OnCollectionChanged);
        }

        /// <summary>
        /// Gets or sets the color of the series. The default is <see cref="MoreColors.Automatic"/>.
        /// </summary>
        /// <value>The color that will be used for rendering the series.</value>
        /// <remarks>
        /// When set to <see cref="MoreColors.Automatic"/>, the plot model will assign
        /// a color from its default color palette.
        /// </remarks>
        public Color Color
        {
            get => (Color)this.GetValue(ColorProperty);
            set => this.SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the internal OxyPlot series model.
        /// </summary>
        /// <value>The internal series model that this wrapper synchronizes with.</value>
        /// <remarks>
        /// This property holds the reference to the underlying OxyPlot core series object.
        /// Derived classes should set this in their constructor.
        /// </remarks>
        public OxyPlot.Series.Series InternalSeries { get; protected set; }

        /// <summary>
        /// Gets or sets the title of the series. The default is <c>null</c>.
        /// </summary>
        /// <value>The title that is shown in the legend of the plot.</value>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the series should be rendered in the legend.
        /// The default is <c>true</c>.
        /// </summary>
        /// <value><c>true</c> if the series should appear in the legend; otherwise, <c>false</c>.</value>
        public bool RenderInLegend
        {
            get => (bool)this.GetValue(RenderInLegendProperty);
            set => this.SetValue(RenderInLegendProperty, value);
        }

        /// <summary>
        /// Gets or sets the tracker format string. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The format string used when displaying tracker information.
        /// The available arguments depend on the series type.
        /// </value>
        public string TrackerFormatString
        {
            get => (string)this.GetValue(TrackerFormatStringProperty);
            set => this.SetValue(TrackerFormatStringProperty, value);
        }

        /// <summary>
        /// Gets or sets the tracker key. The default is <c>null</c>.
        /// </summary>
        /// <value>
        /// The key that can be used by the plot view to show a custom tracker for this series.
        /// </value>
        public string TrackerKey
        {
            get => (string)this.GetValue(TrackerKeyProperty);
            set => this.SetValue(TrackerKeyProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this series participates in tracker hit-tests.
        /// The default is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Set to <c>false</c> for dense or decorative series (e.g. MCMC chain traces, overlay
        /// reference lines) where per-point hit-testing provides no diagnostic value. When
        /// disabled, the series is skipped by the tracker on every mouse move, avoiding the
        /// O(n) nearest-point scan that would otherwise saturate the UI thread on large datasets.
        /// This is a tracker-routing flag only: it does not affect rendering, does not trigger
        /// <c>InvalidatePlot</c>, and does not fire <see cref="PropertyChanged"/>.
        /// </para>
        /// <para>
        /// This property round-trips through
        /// <c>OxyPlot.Wpf.Serialization.SeriesSerializer</c> — saved plot configurations
        /// preserve the hit-test flag so reopening a plot keeps tracker-routing exactly
        /// as the user left it.
        /// </para>
        /// </remarks>
        public bool IsHitTestEnabled
        {
            get => (bool)this.GetValue(IsHitTestEnabledProperty);
            set => this.SetValue(IsHitTestEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the edge rendering mode for the series. The default is <see cref="EdgeRenderingMode.Automatic"/>.
        /// </summary>
        /// <value>The edge rendering mode that controls anti-aliasing behavior.</value>
        /// <remarks>
        /// This property round-trips through
        /// <c>OxyPlot.Wpf.Serialization.SeriesSerializer</c> — saved plot configurations
        /// preserve the edge-rendering selection (e.g., <c>Adaptive</c>, <c>PreferSpeed</c>)
        /// rather than reverting to <see cref="EdgeRenderingMode.Automatic"/> on reload.
        /// </remarks>
        public EdgeRenderingMode EdgeRenderingMode
        {
            get => (EdgeRenderingMode)this.GetValue(EdgeRenderingModeProperty);
            set => this.SetValue(EdgeRenderingModeProperty, value);
        }

        /// <summary>
        /// Creates the internal OxyPlot series model.
        /// </summary>
        /// <returns>The OxyPlot series model that this wrapper represents.</returns>
        /// <remarks>
        /// This method is called by the Plot control when synchronizing its model.
        /// Derived classes must implement this to create and return their specific series type.
        /// </remarks>
        public abstract OxyPlot.Series.Series CreateModel();

        /// <summary>
        /// Handles changes to appearance-related dependency properties.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event arguments containing property change information.</param>
        /// <remarks>
        /// This callback triggers a visual update without reloading data.
        /// </remarks>
        protected static void AppearanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (Series)d;
            if (series.SuppressPropertyChanged) return;
            series.OnVisualChanged();
            series.OnPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Handles changes to data-related dependency properties.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event arguments containing property change information.</param>
        /// <remarks>
        /// This callback triggers a full data update and re-render.
        /// </remarks>
        protected static void DataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (Series)d;
            if (series.SuppressPropertyChanged) return;
            series.OnDataChanged();
        }

        /// <summary>
        /// Handles changes to the <see cref="IsHitTestEnabled"/> dependency property.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event arguments containing property change information.</param>
        /// <remarks>
        /// Hit-test routing is neither a visual nor a data change, so this callback must NOT
        /// trigger <c>InvalidatePlot</c> or fire <see cref="PropertyChanged"/>. Instead it
        /// directly pushes the new value to <see cref="InternalSeries"/> so the tracker
        /// (which reads from the internal model on every mouse move) picks up the change
        /// without a render. The next call to <see cref="SynchronizeProperties"/> will reassert
        /// the same value via the normal pathway; there is no race.
        /// </remarks>
        protected static void HitTestRoutingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var series = (Series)d;
            if (series.InternalSeries != null)
            {
                series.InternalSeries.IsHitTestEnabled = (bool)e.NewValue;
            }
        }

        /// <summary>
        /// Called when data has changed and a full update is required.
        /// </summary>
        /// <remarks>
        /// This method invalidates the plot with data update enabled.
        /// </remarks>
        protected void OnDataChanged()
        {
            var pc = this.Parent as IPlotView;
            if (pc != null)
            {
                pc.InvalidatePlot();
            }
        }

        /// <summary>
        /// Called when the items source has changed.
        /// </summary>
        /// <param name="oldValue">The old items source.</param>
        /// <param name="newValue">The new items source.</param>
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            this.SubscribeToCollectionChanged(oldValue, newValue);
            this.OnDataChanged();
        }

        /// <summary>
        /// Called when visual appearance has changed.
        /// </summary>
        /// <remarks>
        /// This method invalidates the plot without reloading data.
        /// </remarks>
        protected void OnVisualChanged()
        {
            // Set _needsSynchronization before InvalidatePlot so the sync is not skipped.
            // OnVisualChanged is only called from WPF DP change callbacks (not zoom/pan),
            // so this does not affect zoom/pan performance.
            if (this.Parent is Plot plot)
            {
                plot._needsSynchronization = true;
                plot.InvalidatePlot(false);
            }
        }

        /// <summary>
        /// Synchronizes the wrapper properties to the internal OxyPlot series.
        /// </summary>
        /// <param name="s">The OxyPlot series to synchronize properties to.</param>
        /// <remarks>
        /// Derived classes should override this method and call the base implementation
        /// to ensure all properties are properly synchronized.
        /// </remarks>
        protected virtual void SynchronizeProperties(OxyPlot.Series.Series s)
        {
            s.Background = this.Background.ToOxyColor();
            s.Title = this.Title;
            s.RenderInLegend = this.RenderInLegend;
            s.TrackerFormatString = this.TrackerFormatString;
            s.TrackerKey = this.TrackerKey;
            s.IsVisible = this.Visibility == Visibility.Visible;
            s.IsHitTestEnabled = this.IsHitTestEnabled;
            s.Font = this.FontFamily?.ToString();
            s.FontSize = this.FontSize;
            s.FontWeight = this.FontWeight.ToOpenTypeWeight();
            s.TextColor = this.Foreground.ToOxyColor();
            s.EdgeRenderingMode = this.EdgeRenderingMode;
        }

        /// <summary>
        /// Subscribes to or unsubscribes from collection changed events.
        /// </summary>
        /// <param name="oldValue">The old items source.</param>
        /// <param name="newValue">The new items source.</param>
        private void SubscribeToCollectionChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            if (oldValue is INotifyCollectionChanged oldCollection)
            {
                CollectionChangedEventManager.RemoveListener(oldCollection, this.eventListener);
            }

            if (newValue is INotifyCollectionChanged newCollection)
            {
                CollectionChangedEventManager.AddListener(newCollection, this.eventListener);
            }
        }

        /// <summary>
        /// Handles collection changed events from the items source.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.OnDataChanged();
        }

        /// <summary>
        /// Provides a weak event listener for collection changed events.
        /// </summary>
        private class EventListener : IWeakEventListener
        {
            /// <summary>
            /// The handler delegate for collection changed events.
            /// </summary>
            private readonly EventHandler<NotifyCollectionChangedEventArgs> handler;

            /// <summary>
            /// Initializes a new instance of the <see cref="EventListener"/> class.
            /// </summary>
            /// <param name="handler">The handler to invoke when a collection changes.</param>
            public EventListener(EventHandler<NotifyCollectionChangedEventArgs> handler)
            {
                this.handler = handler;
            }

            /// <summary>
            /// Receives a weak event notification.
            /// </summary>
            /// <param name="managerType">The type of the event manager.</param>
            /// <param name="sender">The source of the event.</param>
            /// <param name="e">The event arguments.</param>
            /// <returns><c>true</c> if the event was handled; otherwise, <c>false</c>.</returns>
            public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
            {
                if (managerType == typeof(CollectionChangedEventManager))
                {
                    this.handler(sender, (NotifyCollectionChangedEventArgs)e);
                    return true;
                }

                return false;
            }
        }
    }
}
