// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotViewBase.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using OxyPlot;
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Metadata;
    using Avalonia.Controls.Primitives;
    using Avalonia.Controls.Templates;
    using Avalonia.Data;
    using Avalonia.Input;
    using Avalonia.Input.Platform;
    using Avalonia.Interactivity;
    using Avalonia.LogicalTree;
    using Avalonia.Media;
    using Avalonia.Threading;
    using Avalonia.VisualTree;
    using WpfCompat.Input;
    using CursorType = OxyPlot.CursorType;

    /// <summary>
    /// Base class for WPF PlotView implementations.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: an Avalonia <see cref="TemplatedControl"/>.
    /// <list type="bullet">
    /// <item>Its theme, from the ported Themes/Generic.axaml, is shared by every derived view
    /// (<see cref="StyleKeyOverride"/>, WPF's DefaultStyleKey). <see cref="GenericTheme"/> loads it, as
    /// WPF applied generic.xaml by itself.</item>
    /// <item>Its dependency properties are styled properties, with the same defaults. The templates are
    /// Avalonia's <see cref="IControlTemplate"/> and the cursors Avalonia's.</item>
    /// <item>WPF's <c>Control</c> takes focus by default; this control does too, so the keyboard
    /// reaches the plot controller.</item>
    /// <item>Rendering is scheduled with Avalonia's dispatcher at the same (Render) priority.</item>
    /// <item>The DPI scale is the top level's render scaling, re-read when it changes
    /// (WPF's <c>OnDpiChanged</c>).</item>
    /// <item>Mouse events are Avalonia's pointer events. WPF's touch manipulation events have no
    /// Avalonia equivalent; Avalonia reports touch as pointer events, which drive the controller as
    /// mouse input. <c>IsManipulationEnabled</c> is gone with them.</item>
    /// <item>Avalonia opens a context menu by itself on any right-button release; this control keeps
    /// WPF's rule of opening it only on a right click without a drag (a right drag pans).</item>
    /// <item>The clipboard is Avalonia's, which is asynchronous.</item>
    /// </list>
    /// </remarks>
    [TemplatePart(PartGrid, typeof(Grid))]
    public abstract partial class PlotViewBase : TemplatedControl, IPlotView
    {
        /// <summary>
        /// The Grid PART constant.
        /// </summary>
        protected const string PartGrid = "PART_Grid";

        /// <summary>
        /// The grid.
        /// </summary>
        protected Grid grid;

        /// <summary>
        /// The plot presenter.
        /// </summary>
        protected Control plotPresenter;

        /// <summary>
        /// The render context
        /// </summary>
        protected IRenderContext renderContext;

        /// <summary>
        /// The model lock.
        /// </summary>
        private readonly object modelLock = new object();

        /// <summary>
        /// The current tracker.
        /// </summary>
        private Control currentTracker;

        /// <summary>
        /// The current tracker template.
        /// </summary>
        private IControlTemplate currentTrackerTemplate;

        /// <summary>
        /// The default plot controller.
        /// </summary>
        private IPlotController defaultController;

        /// <summary>
        /// Indicates whether the <see cref="PlotViewBase"/> was in the visual tree the last time <see cref="Render"/> was called.
        /// </summary>
        private bool isInVisualTree;

        /// <summary>
        /// The mouse down point.
        /// </summary>
        private ScreenPoint mouseDownPoint;

        /// <summary>
        /// The overlays.
        /// </summary>
        private Canvas overlays;

        /// <summary>
        /// The zoom-rectangle drag overlay. Renders the zoom-rectangle affordance via a single
        /// <c>UIElement.InvalidateVisual</c> per mouse-move with no layout-pass overhead.
        /// </summary>
        /// <remarks>
        /// Replaces the historical <see cref="ContentControl"/> + <c>ControlTemplate</c>
        /// approach (see <see cref="ZoomRectangleAdorner"/> for rationale). Sized once to the
        /// full plot area and positioned at <c>(0, 0)</c>; only its internal rectangle bounds
        /// change during a drag.
        /// </remarks>
        private ZoomRectangleAdorner zoomAdorner;

        /// <summary>
        /// True when a render has been scheduled via Dispatcher.BeginInvoke but has not yet executed.
        /// Prevents redundant render dispatches during rapid zoom/pan mouse events.
        /// </summary>
        private bool renderPending;

        /// <summary>
        /// Cached DPI scale. Filled lazily on first <see cref="UpdateDpi"/> call and invalidated
        /// in <see cref="OnDpiChanged"/> + <see cref="OnApplyTemplate"/>. Avoids the
        /// <c>PresentationSource.FromVisual</c> COM-boundary walk on every render —
        /// which fires once per pan/zoom step at 60Hz.
        /// </summary>
        private double? cachedDpiScale;

        /// <summary>
        /// Backing field for <see cref="DisableShapeAntiAliasing"/>.
        /// </summary>
        private bool disableShapeAntiAliasing;

        /// <summary>
        /// Gets or sets a value indicating whether shape anti-aliasing is disabled inside the
        /// hosted plot presenter. Default is <c>false</c> (anti-aliasing on).
        /// </summary>
        /// <remarks>
        /// <para>
        /// When set to <c>true</c>, applies <see cref="EdgeMode.Aliased"/> to the presenter via
        /// <c>RenderOptions.SetEdgeMode</c>. WPF's stroke-tessellation pass for anti-aliased
        /// lines is the dominant per-series cost on dense multi-series plots (e.g. 20-chain
        /// MCMC traces); disabling it produces a multi-x speedup for line rendering. Markers,
        /// fills, and text are unaffected.
        /// </para>
        /// <para>
        /// Recommended for trace, density, and scientific plots where many line series are
        /// rendered together and pixel-exact line edges are not visually critical. Leave
        /// <c>false</c> for plots that rely on anti-aliased thick strokes or curved geometry
        /// where the visual quality difference is noticeable.
        /// </para>
        /// <para>
        /// <b>Recommended pairings with <see cref="UseBitmapCache"/></b>:
        /// </para>
        /// <list type="bullet">
        /// <item><description><b>Static plot</b> (report view, finished chart): set both
        /// <see cref="DisableShapeAntiAliasing"/>=<c>true</c> and <see cref="UseBitmapCache"/>=<c>true</c>.
        /// Aliasing is invisible at typical thin strokes and the bitmap cache eliminates
        /// re-rasterization on overlay drags / dock splits.</description></item>
        /// <item><description><b>Real-time / streaming plot</b> (oscilloscope, live data): leave both
        /// at the default <c>false</c>. Every frame regenerates the geometry, so neither lever
        /// helps and <see cref="UseBitmapCache"/>=<c>true</c> would add bitmap-encode overhead.</description></item>
        /// <item><description><b>Interactive zoom/pan plot on large static data</b> (multi-LineSeries
        /// trace plot): set <see cref="DisableShapeAntiAliasing"/>=<c>true</c> and
        /// <see cref="UseBitmapCache"/>=<c>false</c>. Aliasing accelerates the per-zoom render;
        /// the bitmap cache is invalidated on every zoom/pan and offers no benefit.</description></item>
        /// </list>
        /// </remarks>
        public bool DisableShapeAntiAliasing
        {
            get => this.disableShapeAntiAliasing;
            set
            {
                if (this.disableShapeAntiAliasing == value) return;
                this.disableShapeAntiAliasing = value;
                this.ApplyAntiAliasingPreference();
            }
        }

        /// <summary>
        /// Pushes the current <see cref="DisableShapeAntiAliasing"/> setting onto the hosted
        /// plot presenter via <c>RenderOptions.SetEdgeMode</c>. Called from the property setter
        /// and from <see cref="OnApplyTemplate"/>. Walks the presenter's <c>Visual</c> property
        /// (if any) to also apply the mode to the underlying <c>DrawingVisual</c>,
        /// without requiring a type reference to the OxyPlot.Wpf-specific <c>DrawingVisualHost</c>
        /// from this Shared assembly.
        /// </summary>
        private void ApplyAntiAliasingPreference()
        {
            if (this.plotPresenter == null) return;
            var mode = this.disableShapeAntiAliasing ? EdgeMode.Aliased : EdgeMode.Unspecified;
            RenderOptions.SetEdgeMode(this.plotPresenter, mode);

            // DrawingVisualHost exposes its inner DrawingVisual via a public Visual property.
            // Reflect to avoid a circular reference back to OxyPlot.Wpf.
            // XGoodFit port: the ported host's drawing is not a visual of its own; it is drawn by the
            // host, whose edge mode is set above, so this finds nothing to set.
            var visualProp = this.plotPresenter.GetType().GetProperty("Visual",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (visualProp?.GetValue(this.plotPresenter) is Avalonia.Visual innerVisual)
            {
                RenderOptions.SetEdgeMode(innerVisual, mode);
            }
        }

        /// <summary>
        /// Backing field for <see cref="UseBitmapCache"/>.
        /// </summary>
        private bool useBitmapCache;

        /// <summary>
        /// Gets or sets a value indicating whether the rasterized output of the plot's hosted
        /// presenter is cached as a bitmap in video memory via WPF's <see cref="BitmapCache"/>
        /// mechanism. Default <c>false</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the underlying <c>DrawingVisualHost.UseBitmapCache</c> property is
        /// enabled, which assigns a <see cref="BitmapCache"/> to <c>UIElement.CacheMode</c>.
        /// WPF rasterizes the plot's visual subtree once into a GPU bitmap and reuses the bitmap
        /// on subsequent compositor frames; the cache regenerates only when the subtree's
        /// content actually changes (i.e., on each <c>Plot.InvalidatePlot</c>). Markers, fills,
        /// and other plot elements are unaffected.
        /// </para>
        /// <para>
        /// <b>When this helps.</b> Scenarios where the plot is static but the surface is
        /// repeatedly composited — dragging a transient overlay (e.g., the magnifier-glass zoom
        /// rectangle) over the plot, parent panel resize animations, dock-splitter drags, plot
        /// scrolling into and out of viewport. Without the cache, WPF re-rasterizes the plot's
        /// vector geometry under the dirty region on every compositor frame.
        /// </para>
        /// <para>
        /// <b>When this doesn't help.</b> Real-time streaming plots that re-render every frame
        /// see no benefit and pay a small per-render overhead. Default is off; opt in per plot.
        /// </para>
        /// <para>
        /// See <see cref="DisableShapeAntiAliasing"/> for the three-scenario pairing table
        /// (static / real-time / interactive) that pairs both flags for best results.
        /// </para>
        /// </remarks>
        public bool UseBitmapCache
        {
            get => this.useBitmapCache;
            set
            {
                if (this.useBitmapCache == value) return;
                this.useBitmapCache = value;
                this.ApplyBitmapCachePreference();
            }
        }

        /// <summary>
        /// Pushes the current <see cref="UseBitmapCache"/> setting onto the hosted plot
        /// presenter. Called from the property setter and from <see cref="OnApplyTemplate"/>.
        /// Reflects on a public <c>UseBitmapCache</c> property exposed by the presenter (the
        /// OxyPlot.Wpf <c>DrawingVisualHost</c>) without requiring a circular reference.
        /// </summary>
        private void ApplyBitmapCachePreference()
        {
            if (this.plotPresenter == null) return;
            var prop = this.plotPresenter.GetType().GetProperty("UseBitmapCache",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(this.plotPresenter, this.useBitmapCache);
            }
        }

        /// <summary>
        /// Initializes static members of the <see cref="PlotViewBase" /> class.
        /// </summary>
        static PlotViewBase()
        {
            PaddingProperty.OverrideDefaultValue<PlotViewBase>(new Thickness(8));
            FocusableProperty.OverrideDefaultValue<PlotViewBase>(true);

            // XGoodFit port: WPF applies generic.xaml's style to a plot view as soon as it is made, even
            // outside a window; Avalonia themes reach only controls in a window. So the theme's background
            // is also the default, giving the same value in both.
            BackgroundProperty.OverrideDefaultValue<PlotViewBase>(Brushes.White);
            ModelProperty.Changed.AddClassHandler<PlotViewBase>(ModelChanged);
            ContextRequestedEvent.AddClassHandler<PlotViewBase>(OnContextRequestedByPointer);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotViewBase" /> class.
        /// </summary>
        protected PlotViewBase()
        {
            GenericTheme.EnsureLoaded();
            this.TrackerDefinitions = new ObservableCollection<TrackerDefinition>();
            this.CommandBindings.Add(new CommandBinding(PlotCommands.ResetAxes, (s, e) => this.ResetAllAxes()));
            this.Loaded += this.OnPlotViewLoaded;
            this.Unloaded += this.OnPlotViewUnloaded;
        }

        /// <summary>
        /// Gets the key of the theme this control and every derived view use.
        /// </summary>
        /// <remarks>XGoodFit port: WPF's DefaultStyleKey override.</remarks>
        protected override Type StyleKeyOverride => typeof(PlotViewBase);

        /// <summary>
        /// Handles the Loaded event to subscribe to LayoutUpdated.
        /// </summary>
        private void OnPlotViewLoaded(object sender, RoutedEventArgs e)
        {
            this.LayoutUpdated += this.OnLayoutUpdated;
            // Once Loaded fires we know we're in a tree (Window, Popup, ElementHost, etc.).
            // OnLayoutUpdated still walks the tree for the rare popup-hosting edge case where
            // Loaded can fire before the popup is fully composed.
            this.isInVisualTree = true;
            // DPI may have changed since the last Loaded (cross-monitor / theme reload);
            // force re-fetch on next render.
            this.cachedDpiScale = null;
        }

        /// <summary>
        /// Handles the Unloaded event to unsubscribe from LayoutUpdated to prevent memory leaks.
        /// </summary>
        private void OnPlotViewUnloaded(object sender, RoutedEventArgs e)
        {
            this.LayoutUpdated -= this.OnLayoutUpdated;
            this.isInVisualTree = false;
        }

        /// <summary>
        /// The top level whose scaling is followed (see <see cref="OnDpiChanged"/>).
        /// </summary>
        private TopLevel scalingSource;

        /// <inheritdoc/>
        /// <remarks>XGoodFit addition: follows the top level's scaling, as WPF's <c>OnDpiChanged</c> did.</remarks>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            this.scalingSource = TopLevel.GetTopLevel(this);
            if (this.scalingSource != null)
            {
                this.scalingSource.ScalingChanged += this.OnDpiChanged;
            }

            this.cachedDpiScale = null;
        }

        /// <inheritdoc/>
        /// <remarks>XGoodFit addition: see <see cref="OnAttachedToVisualTree"/>.</remarks>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (this.scalingSource != null)
            {
                this.scalingSource.ScalingChanged -= this.OnDpiChanged;
                this.scalingSource = null;
            }
        }

        /// <summary>
        /// Gets the actual PlotView controller.
        /// </summary>
        /// <value>The actual PlotView controller.</value>
        public IPlotController ActualController => this.Controller ?? (this.defaultController ??= new PlotController());

        /// <inheritdoc/>
        IController IView.ActualController => this.ActualController;

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        /// <value>The actual model.</value>
        public virtual PlotModel ActualModel { get; private set; }

        /// <inheritdoc/>
        Model IView.ActualModel => this.ActualModel;

        /// <summary>
        /// Gets the coordinates of the client area of the view.
        /// </summary>
        public OxyRect ClientArea => new OxyRect(0, 0, this.Bounds.Width, this.Bounds.Height);

        /// <summary>
        /// Gets the tracker definitions.
        /// </summary>
        /// <value>The tracker definitions.</value>
        public ObservableCollection<TrackerDefinition> TrackerDefinitions { get; }

        /// <summary>
        /// Hides the tracker.
        /// </summary>
        public void HideTracker()
        {
            if (this.currentTracker != null && this.overlays != null)
            {
                this.overlays.Children.Remove(this.currentTracker);
                this.currentTracker = null;
                this.currentTrackerTemplate = null;
            }
        }

        /// <summary>
        /// Hides the zoom rectangle.
        /// </summary>
        public void HideZoomRectangle()
        {
            this.zoomAdorner?.ClearBounds();
        }

        /// <summary>
        /// Invalidate the PlotView (not blocking the UI thread)
        /// </summary>
        /// <param name="updateData">The update Data.</param>
        public virtual void InvalidatePlot(bool updateData = true)
        {
            if (this.ActualModel == null)
            {
                return;
            }

#if DEBUG
            DiagnoseInvalidatePlotCall(updateData);
            using (OxyPlot.PlotDiagnostics.Trace("PlotViewBase.InvalidatePlot", $"updateData={updateData} renderPending={this.renderPending}"))
            {
#endif

                lock (this.ActualModel.SyncRoot)
                {
                    ((IPlotModel)this.ActualModel).Update(updateData);
                }

                if (!this.renderPending)
                {
                    this.renderPending = true;
#if DEBUG
                    int wheelSeqAtDispatch = OxyPlot.PlotDiagnostics.CurrentWheelSeq;
                    long dispatchTicks = System.Diagnostics.Stopwatch.GetTimestamp();
#endif
                    // DispatcherPriority.Render runs at the same priority as WPF's internal
                    // composition tick — the deferred render therefore executes at the start
                    // of the next frame budget rather than after pending Layout/DataBind
                    // work drains (which is what DispatcherPriority.Loaded would do). Cuts
                    // ~one full dispatcher cycle (~16ms at 60Hz) of latency on rapid wheel
                    // zoom + pan. The renderPending flag still guards against double-queuing.
                    Dispatcher.UIThread.Post(() =>
                    {
                        this.renderPending = false;
#if DEBUG
                        if (OxyPlot.PlotDiagnostics.WheelTraceEnabled && wheelSeqAtDispatch != 0)
                        {
                            double queueMs = (System.Diagnostics.Stopwatch.GetTimestamp() - dispatchTicks) * 1000.0
                                             / System.Diagnostics.Stopwatch.Frequency;
                            System.Diagnostics.Debug.WriteLine(
                                $"[W#{wheelSeqAtDispatch,4}] PlotViewBase.Render DEQUEUED (Render) queueMs={queueMs,8:F2}");
                        }
                        var renderSw = System.Diagnostics.Stopwatch.StartNew();
#endif
                        this.Render();
#if DEBUG
                        renderSw.Stop();
                        if (OxyPlot.PlotDiagnostics.WheelTraceEnabled && wheelSeqAtDispatch != 0)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[W#{wheelSeqAtDispatch,4}] PlotViewBase.Render EXIT renderMs={renderSw.Elapsed.TotalMilliseconds,8:F2}");
                        }
#endif
                    }, DispatcherPriority.Render);
                }
#if DEBUG
            }
#endif
        }

#if DEBUG
        /// <summary>
        /// Set to <c>true</c> to enable per-call <see cref="InvalidatePlot"/> diagnostics
        /// (caller chain + timing). Consumers flip this on just before reproducing a suspected
        /// invalidation loop and off immediately after, to keep the log focused.
        /// </summary>
        /// <remarks>
        /// Debug-only. Has no effect in Release builds.
        /// </remarks>
        public static bool InvalidatePlotDiagnosticsEnabled { get; set; }

        /// <summary>
        /// Restrict diagnostics output to plots whose <see cref="PlotModel.Title"/> contains this
        /// substring (case-insensitive). Null or empty = log all plots. Use this to silence
        /// unrelated plots when reproducing a loop against a single plot.
        /// </summary>
        public static string InvalidatePlotDiagnosticsTitleFilter { get; set; }

        private static readonly System.Threading.ThreadLocal<long> _lastInvalidateTicks =
            new System.Threading.ThreadLocal<long>(() => 0);

        private static long _invalidateCallCounter;

        private void DiagnoseInvalidatePlotCall(bool updateData)
        {
            if (!InvalidatePlotDiagnosticsEnabled) return;

            var model = this.ActualModel;
            if (model == null) return;

            // Title filter so the log is focused on the plot under investigation.
            var filter = InvalidatePlotDiagnosticsTitleFilter;
            if (!string.IsNullOrEmpty(filter))
            {
                var title = model.Title ?? string.Empty;
                if (title.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) return;
            }

            long callId = System.Threading.Interlocked.Increment(ref _invalidateCallCounter);

            // Per-thread inter-call timing. InvalidatePlot is UI-thread-only in practice, but we
            // use ThreadLocal to be safe if any background-thread caller slips through.
            long nowTicks = DateTime.UtcNow.Ticks;
            long prevTicks = _lastInvalidateTicks.Value;
            _lastInvalidateTicks.Value = nowTicks;
            double dtMs = prevTicks == 0 ? -1 : (nowTicks - prevTicks) / 10_000.0;

            // Capture up to 8 frames of the call stack, skipping this method and InvalidatePlot.
            var stackTrace = new System.Diagnostics.StackTrace(skipFrames: 2, fNeedFileInfo: false);
            var sb = new System.Text.StringBuilder(256);
            int frameCount = System.Math.Min(stackTrace.FrameCount, 8);
            for (int i = 0; i < frameCount; i++)
            {
                var frame = stackTrace.GetFrame(i);
                var method = frame?.GetMethod();
                if (method == null) continue;
                var type = method.DeclaringType;
                string typeName = type != null ? (type.Name ?? string.Empty) : "?";
                if (i > 0) sb.Append(" <- ");
                sb.Append(typeName).Append('.').Append(method.Name);
            }

            string titleForLog = model.Title ?? "(no title)";
            System.Diagnostics.Debug.WriteLine(
                $"[InvalidatePlot #{callId,6} title=\"{titleForLog}\" updateData={(updateData ? "T" : "F"),-1} dt={dtMs,7:F2}ms] {sb}");
        }
#endif

        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            // Force DPI re-fetch on next render — template re-application may move the
            // visual to a different PresentationSource (AvalonDock detach to floating window
            // on a different monitor, theme switch that re-templates, etc.).
            this.cachedDpiScale = null;
            this.grid = e.NameScope.Find(PartGrid) as Grid;
            if (this.grid == null)
            {
                return;
            }

            this.plotPresenter = this.CreatePlotPresenter();
            this.grid.Children.Add(this.plotPresenter);
            this.plotPresenter.UpdateLayout();
            this.renderContext = this.CreateRenderContext();

            // Propagate any DisableShapeAntiAliasing / UseBitmapCache preference set before the
            // template was applied (the presenter is created here for the first time).
            this.ApplyAntiAliasingPreference();
            this.ApplyBitmapCachePreference();

            this.overlays = new Canvas();
            this.grid.Children.Add(this.overlays);

            // Clear bindings on the previous adorner instance (if any) before discarding the
            // reference. Otherwise the WPF binding objects retain a Source reference back to
            // the old overlay Canvas, keeping the prior visual subtree alive across
            // re-template events (AvalonDock dock/undock, theme switches that re-template
            // the control).
            if (this.zoomAdorner != null)
            {
                this.zoomAdorner.ClearValue(WidthProperty);
                this.zoomAdorner.ClearValue(HeightProperty);
            }

            this.zoomAdorner = new ZoomRectangleAdorner();
            // The adorner renders the zoom-rectangle drag affordance with no layout-pass overhead
            // per mouse-move (see ZoomRectangleAdorner remarks for the full rationale). It sits
            // at (0, 0) of the overlay canvas; the rectangle to draw is set via SetBounds.
            // Bind Width/Height so the adorner fills the overlay canvas (Canvas does not
            // auto-size its children); without this its render area would be 0x0 and OnRender
            // would draw nothing.
            this.zoomAdorner.Bind(WidthProperty, new ReflectionBinding("Bounds.Width") { Source = this.overlays });
            this.zoomAdorner.Bind(HeightProperty, new ReflectionBinding("Bounds.Height") { Source = this.overlays });
            this.overlays.Children.Add(this.zoomAdorner);

            // add additional grid on top of everthing else to fix issue of mouse events getting lost
            // it must be added last so it covers all other controls
            var mouseGrid = new Grid
            {
                Background = Brushes.Transparent // background must be set for hit test to work
            };
            this.grid.Children.Add(mouseGrid);
        }

        /// <summary>
        /// Pans all axes.
        /// </summary>
        /// <param name="delta">The delta.</param>
        public void PanAllAxes(Vector delta)
        {
            if (this.ActualModel != null)
            {
                this.ActualModel.PanAllAxes(delta.X, delta.Y);
            }

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Resets all axes.
        /// </summary>
        public void ResetAllAxes()
        {
            if (this.ActualModel != null)
            {
                this.ActualModel.ResetAllAxes();
            }

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Stores text on the clipboard.
        /// </summary>
        /// <param name="text">The text.</param>
        public void SetClipboardText(string text)
        {
            _ = TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(text);
        }

        /// <summary>
        /// Sets the cursor type.
        /// </summary>
        /// <param name="cursorType">The cursor type.</param>
        public void SetCursorType(CursorType cursorType)
        {
            this.Cursor = cursorType switch
            {
                CursorType.Pan => this.PanCursor,
                CursorType.ZoomRectangle => this.ZoomRectangleCursor,
                CursorType.ZoomHorizontal => this.ZoomHorizontalCursor,
                CursorType.ZoomVertical => this.ZoomVerticalCursor,
                _ => new Cursor(StandardCursorType.Arrow),
            };
        }

        /// <summary>
        /// Shows the tracker.
        /// </summary>
        /// <param name="trackerHitResult">The tracker data.</param>
        public void ShowTracker(TrackerHitResult trackerHitResult)
        {
            if (trackerHitResult == null)
            {
                this.HideTracker();
                return;
            }

            var trackerTemplate = this.DefaultTrackerTemplate;
            if (trackerHitResult.Series != null && !string.IsNullOrEmpty(trackerHitResult.Series.TrackerKey))
            {
                var match = this.TrackerDefinitions.FirstOrDefault(t => t.TrackerKey == trackerHitResult.Series.TrackerKey);
                if (match != null)
                {
                    trackerTemplate = match.TrackerTemplate;
                }
            }

            if (trackerTemplate == null)
            {
                this.HideTracker();
                return;
            }

            if (!ReferenceEquals(trackerTemplate, this.currentTrackerTemplate))
            {
                this.HideTracker();

                var tracker = new ContentControl { Template = trackerTemplate };
                this.overlays.Children.Add(tracker);
                this.currentTracker = tracker;
                this.currentTrackerTemplate = trackerTemplate;
            }

            if (this.currentTracker != null)
            {
                this.currentTracker.DataContext = trackerHitResult;
            }
        }

        /// <summary>
        /// Shows the zoom rectangle.
        /// </summary>
        /// <param name="r">The rectangle.</param>
        /// <remarks>
        /// Called on every mouse-move during a magnifier-glass zoom-rectangle drag (60–125 Hz),
        /// so the body must be cheap. Delegates to <see cref="ZoomRectangleAdorner.SetBounds"/>,
        /// which only updates an internal field and calls <c>UIElement.InvalidateVisual</c>
        /// — no WPF layout pass per mouse-move.
        /// </remarks>
        public void ShowZoomRectangle(OxyRect r)
        {
            this.zoomAdorner?.SetBounds(new Rect(r.Left, r.Top, r.Width, r.Height));
        }

        /// <summary>
        /// Zooms all axes.
        /// </summary>
        /// <param name="factor">The zoom factor.</param>
        public void ZoomAllAxes(double factor)
        {
            if (this.ActualModel != null)
            {
                this.ActualModel.ZoomAllAxes(factor);
            }

            this.InvalidatePlot(false);
        }

        /// <summary>
        /// Clears the background of the plot presenter.
        /// </summary>
        protected abstract void ClearBackground();

        /// <summary>
        /// Creates the plot presenter.
        /// </summary>
        /// <returns>The plot presenter.</returns>
        protected abstract Control CreatePlotPresenter();

        /// <summary>
        /// Creates the render context.
        /// </summary>
        /// <returns>The render context.</returns>
        protected abstract IRenderContext CreateRenderContext();

        /// <summary>
        /// Called when the model is changed.
        /// </summary>
        protected void OnModelChanged()
        {
            lock (this.modelLock)
            {
                if (this.ActualModel != null)
                {
                    ((IPlotModel)this.ActualModel).AttachPlotView(null);
                    this.ActualModel = null;
                }

                if (this.Model != null)
                {
                    ((IPlotModel)this.Model).AttachPlotView(this);
                    this.ActualModel = this.Model;
                }
            }

            this.InvalidatePlot();
        }

        /// <summary>
        /// Renders the plot model to the plot presenter.
        /// </summary>
        protected void Render()
        {
            if (this.plotPresenter == null || this.renderContext == null)
            {
                return;
            }

            // Skip the full IsInVisualTree() tree walk on every render. The Loaded/Unloaded
            // events maintain the cached value; OnLayoutUpdated handles edge cases (popup
            // hosting, ElementHost) where Loaded doesn't reliably reflect tree connectivity.
            // Only re-walk when the cached value says "not in tree" — to discover transitions
            // we may have missed.
            if (!this.isInVisualTree)
            {
                this.isInVisualTree = this.IsInVisualTree();
            }

            this.RenderOverride();
        }

        /// <summary>
        /// Renders the plot model to the plot presenter.
        /// </summary>
        protected virtual void RenderOverride()
        {
#if DEBUG
            using (OxyPlot.PlotDiagnostics.Trace("PlotViewBase.RenderOverride"))
#endif
            {
                var dpiScale = this.UpdateDpi();
                this.ClearBackground();

                if (this.ActualModel != null)
                {
                    // round width and height to full device pixels
                    var width = ((int)(this.plotPresenter.Bounds.Width * dpiScale)) / dpiScale;
                    var height = ((int)(this.plotPresenter.Bounds.Height * dpiScale)) / dpiScale;

                    lock (this.ActualModel.SyncRoot)
                    {
#if DEBUG
                        using (OxyPlot.PlotDiagnostics.Trace("PlotModel.Render", $"size={width:F0}x{height:F0}"))
#endif
                        {
                            ((IPlotModel)this.ActualModel).Render(this.renderContext, new OxyRect(0, 0, width, height));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the DPI scale of the render context.
        /// </summary>
        /// <returns>The DPI scale.</returns>
        /// <remarks>
        /// Caches the DPI scale across renders. <c>PresentationSource.FromVisual</c>
        /// is a non-trivial COM-boundary lookup; calling it on every render adds avoidable
        /// overhead when the DPI changes only on monitor switch. The cache is invalidated in
        /// <see cref="OnDpiChanged"/> (per-monitor DPI change) and reset in
        /// <see cref="OnApplyTemplate"/> (presenter recreation).
        /// </remarks>
        protected virtual double UpdateDpi()
        {
            if (this.cachedDpiScale.HasValue)
            {
                return this.cachedDpiScale.Value;
            }

            var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
            this.cachedDpiScale = scale;
            return scale;
        }

        /// <summary>
        /// Invalidates the cached DPI scale when the host moves between monitors with
        /// different DPI. Forces the next <see cref="UpdateDpi"/> to re-fetch from
        /// <c>PresentationSource</c>.
        /// </summary>
        private void OnDpiChanged(object sender, EventArgs e)
        {
            this.cachedDpiScale = null;
        }

        /// <summary>
        /// Called when the model is changed.
        /// </summary>
        /// <param name="d">The sender.</param>
        /// <param name="e">The <see cref="AvaloniaPropertyChangedEventArgs" /> instance containing the event data.</param>
        private static void ModelChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            ((PlotViewBase)d).OnModelChanged();
        }

        /// <summary>
        /// Stops Avalonia opening the context menu by itself on a right-button release: the menu opens
        /// only on a right click without a drag (see <c>OnPointerReleased</c>), as in WPF.
        /// </summary>
        /// <param name="view">The plot view.</param>
        /// <param name="e">The request.</param>
        /// <remarks>XGoodFit addition. Requests from the keyboard still open the menu.</remarks>
        private static void OnContextRequestedByPointer(PlotViewBase view, ContextRequestedEventArgs e)
        {
            if (e.TryGetPosition(view, out _))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Invokes the specified action on the dispatcher, if necessary.
        /// </summary>
        /// <param name="action">The action.</param>
        private void BeginInvoke(Action action)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                // Render priority matches the deferred-render dispatch in InvalidatePlot,
                // so cross-thread invocations don't sit behind pending Layout/DataBind
                // work in the dispatcher queue.
                Dispatcher.UIThread.Post(action, DispatcherPriority.Render);
            }
            else
            {
                action();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="PlotViewBase"/> is connected to the visual tree.
        /// </summary>
        /// <returns><c>true</c> if the PlotViewBase is connected to the visual tree; <c>false</c> otherwise.</returns>
        /// <remarks>XGoodFit port: Avalonia's root is a <see cref="TopLevel"/> (a window, a popup root, or
        /// an embedded view) where WPF's was a window, and its adorner host is a
        /// <see cref="VisualLayerManager"/>, where WPF's was an AdornerDecorator.</remarks>
        private bool IsInVisualTree()
        {
            Visual dpObject = this;
            while ((dpObject = dpObject.GetVisualParent()) != null)
            {
                if (dpObject is TopLevel)
                {
                    return true;
                }

                //Check if the parent is an AdornerDecorator like in an ElementHost
                if (dpObject is VisualLayerManager)
                {
                    return true;
                }

                //Check if the logical parent is a popup. If so, we found the popuproot
                var logicalRoot = (dpObject as ILogical)?.GetLogicalParent();
                if (logicalRoot is Popup)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This event fires every time Layout updates the layout of the trees associated with current Dispatcher.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event args.</param>
        private void OnLayoutUpdated(object sender, EventArgs e)
        {
            // if we were not in the visual tree the last time we tried to render but are now, we have to render
            if (!this.isInVisualTree && this.IsInVisualTree())
            {
                this.Render();
            }
        }
    }
}
