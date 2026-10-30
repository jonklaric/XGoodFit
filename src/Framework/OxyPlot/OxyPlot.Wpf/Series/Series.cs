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
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Media;
    using Avalonia.Utilities;
    using WpfCompat;
    using WpfCompat.Media;

    /// <summary>
    /// Provides an abstract base class for WPF series wrappers that synchronize
    /// WPF dependency properties with OxyPlot core series objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class extends <see cref="ItemsControl"/> to support ItemsSource binding
    /// and provides the synchronization pattern for WPF to OxyPlot property mapping.
    /// </para>
    /// <para>
    /// XGoodFit port:
    /// <list type="bullet">
    /// <item>The dependency properties are styled properties with the same defaults and callbacks; the
    /// inherited ones WPF overrode (background, font, foreground) are Avalonia's, with the same callbacks.</item>
    /// <item><see cref="Visibility"/> keeps WPF's three states (WpfCompat's <see cref="WpfCompat.Visibility"/>),
    /// which serialization saves; Avalonia's <c>IsVisible</c> follows it, and setting <c>IsVisible</c>
    /// makes the series Visible or Collapsed.</item>
    /// <item>The font family passed to the OxyPlot series is the family's WPF text form
    /// (<see cref="FontFamilyConverter"/>), as Avalonia's <c>ToString()</c> gives a resource key for a
    /// family with fallbacks, such as XGoodFit's default font.</item>
    /// <item>Avalonia has no <c>OnItemsSourceChanged</c> to override; <see cref="OnPropertyChanged(AvaloniaPropertyChangedEventArgs)"/>
    /// calls this class's own when ItemsSource changes. The weak collection subscription is Avalonia's.</item>
    /// <item><see cref="PropertyChanged"/> hides Avalonia's own property-changed event of the same name,
    /// and is this class's INotifyPropertyChanged event, as in WPF.</item>
    /// </list>
    /// </para>
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
        public new event PropertyChangedEventHandler? PropertyChanged;

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
        public static readonly StyledProperty<Color> ColorProperty = AvaloniaProperty.Register<Series, Color>(
            nameof(Color),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Series, string>(
            nameof(Title),
            null);

        /// <summary>
        /// Identifies the <see cref="RenderInLegend"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> RenderInLegendProperty = AvaloniaProperty.Register<Series, bool>(
            nameof(RenderInLegend),
            true);

        /// <summary>
        /// Identifies the <see cref="TrackerFormatString"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TrackerFormatStringProperty = AvaloniaProperty.Register<Series, string>(
            nameof(TrackerFormatString),
            null);

        /// <summary>
        /// Identifies the <see cref="TrackerKey"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TrackerKeyProperty = AvaloniaProperty.Register<Series, string>(
            nameof(TrackerKey),
            null);

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
        public static readonly StyledProperty<bool> IsHitTestEnabledProperty = AvaloniaProperty.Register<Series, bool>(
            nameof(IsHitTestEnabled),
            true);

        /// <summary>
        /// Identifies the <see cref="EdgeRenderingMode"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<EdgeRenderingMode> EdgeRenderingModeProperty = AvaloniaProperty.Register<Series, EdgeRenderingMode>(
            nameof(EdgeRenderingMode),
            EdgeRenderingMode.Automatic);

        /// <summary>
        /// Identifies the <see cref="Visibility"/> dependency property.
        /// </summary>
        /// <remarks>XGoodFit addition: WPF's <c>UIElement.Visibility</c>, which Avalonia lacks.</remarks>
        public static readonly StyledProperty<Visibility> VisibilityProperty = AvaloniaProperty.Register<Series, Visibility>(
            nameof(Visibility),
            Visibility.Visible);

        /// <summary>
        /// The event listener used to subscribe to ItemSource.CollectionChanged events.
        /// </summary>
        private readonly EventListener eventListener;

        /// <summary>
        /// Initializes static members of the <see cref="Series" /> class.
        /// </summary>
        static Series()
        {
            ColorProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            TitleProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            RenderInLegendProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            TrackerFormatStringProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            TrackerKeyProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            IsHitTestEnabledProperty.Changed.AddClassHandler<Series>(HitTestRoutingChanged);
            EdgeRenderingModeProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            // XGoodFit port: WPF overrode the inherited Visibility's default here; this class registers
            // its own Visibility, with that default (Visible).
            VisibilityProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            VisibilityProperty.Changed.AddClassHandler<Series>((s, e) => s.IsVisible = s.Visibility == Visibility.Visible);
            IsVisibleProperty.Changed.AddClassHandler<Series>(IsVisibleChanged);
            BackgroundProperty.OverrideDefaultValue<Series>(null);
            BackgroundProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            FontFamilyProperty.OverrideDefaultValue<Series>(new FontFamily("Segoe UI"));
            FontFamilyProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            FontSizeProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            FontWeightProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
            ForegroundProperty.Changed.AddClassHandler<Series>(AppearanceChanged);
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
        /// Gets or sets the visibility of the series. The default is <see cref="Visibility.Visible"/>.
        /// </summary>
        /// <remarks>XGoodFit addition: WPF's <c>UIElement.Visibility</c>; only a visible series is drawn.</remarks>
        public Visibility Visibility
        {
            get => this.GetValue(VisibilityProperty);
            set => this.SetValue(VisibilityProperty, value);
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
        protected static void AppearanceChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
        protected static void DataChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
        protected static void HitTestRoutingChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
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
        protected virtual void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            this.SubscribeToCollectionChanged(oldValue, newValue);
            this.OnDataChanged();
        }

        /// <inheritdoc/>
        /// <remarks>XGoodFit addition: calls <see cref="OnItemsSourceChanged"/>, which Avalonia doesn't.</remarks>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == ItemsSourceProperty)
            {
                this.OnItemsSourceChanged((IEnumerable)change.OldValue, (IEnumerable)change.NewValue);
            }
        }

        /// <summary>
        /// Makes the series Visible or Collapsed when Avalonia's <c>IsVisible</c> is set.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>XGoodFit addition: keeps <see cref="Visibility"/> in step with <c>IsVisible</c>.</remarks>
        private static void IsVisibleChanged(Series series, AvaloniaPropertyChangedEventArgs e)
        {
            if (series.IsVisible && series.Visibility != Visibility.Visible)
            {
                series.Visibility = Visibility.Visible;
            }
            else if (!series.IsVisible && series.Visibility == Visibility.Visible)
            {
                series.Visibility = Visibility.Collapsed;
            }
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
            s.Font = this.FontFamily == null ? null : FontFamilyConverter.ToString(this.FontFamily);
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
                WeakEvents.CollectionChanged.Unsubscribe(oldCollection, this.eventListener);
            }

            if (newValue is INotifyCollectionChanged newCollection)
            {
                WeakEvents.CollectionChanged.Subscribe(newCollection, this.eventListener);
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
        /// <remarks>XGoodFit port: an Avalonia weak event subscriber.</remarks>
        private class EventListener : IWeakEventSubscriber<NotifyCollectionChangedEventArgs>
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
            /// <param name="sender">The source of the event.</param>
            /// <param name="ev">The weak event.</param>
            /// <param name="e">The event arguments.</param>
            public void OnEvent(object sender, WeakEvent ev, NotifyCollectionChangedEventArgs e)
            {
                this.handler(sender, e);
            }
        }
    }
}
