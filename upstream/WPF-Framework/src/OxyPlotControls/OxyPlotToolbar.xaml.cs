using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OxyPlot;
using Themes;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// OxyPlot toolbar control providing pan, zoom, annotation, and export functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Lifetime:</b> the toolbar uses five custom <see cref="System.Windows.Input.Cursor"/>
    /// instances loaded from embedded resources (move-points, add-point, pan-hand, pan-hand-closed,
    /// zoom). They are held in a private static cache (<c>CursorCache</c>) so they load exactly once
    /// per process and are shared across every toolbar instance. Because cursors are immutable
    /// process-lifetime resources, the toolbar deliberately does <i>not</i> implement
    /// <see cref="IDisposable"/> and consumers do not need to dispose it. Repeatedly creating and
    /// tearing down the toolbar (e.g., per-document tabs) no longer leaks HCURSOR handles.
    /// </para>
    /// </remarks>
    public partial class OxyPlotToolbar : UserControl
    {
        /// <summary>
        /// Tracks which theme was last applied to the plot. Used to detect when the theme changed
        /// while the toolbar was unloaded (inactive tab) so it can be reapplied on next load.
        /// </summary>
        private Themes.Theme? _lastAppliedTheme;

        /// <summary>
        /// Static cache of toolbar cursors loaded once from embedded resources and shared across
        /// all toolbar instances. Cursors are immutable, process-lifetime resources, so each
        /// toolbar instance does not need to own or dispose them. This removes the
        /// IDisposable contract previously imposed on consumers and prevents the cursor handle
        /// leak that occurred when consumers forgot to call <c>Dispose()</c>.
        /// </summary>
        private static class CursorCache
        {
            internal static readonly Cursor MovePoints = LoadCursor(Properties.Resources.SelectPointCursor);
            internal static readonly Cursor AddPoint = LoadCursor(Properties.Resources.AddPointCursor);
            internal static readonly Cursor PanHand = LoadCursor(Properties.Resources.Pan_Hand);
            internal static readonly Cursor PanHandClosed = LoadCursor(Properties.Resources.Pan_Hand_Closed);
            internal static readonly Cursor Zoom = LoadCursor(Properties.Resources.ZoomIn);

            /// <summary>
            /// Creates a WPF cursor from embedded cursor bytes.
            /// </summary>
            /// <param name="cursorBytes">The embedded cursor resource bytes.</param>
            /// <returns>The loaded cursor.</returns>
            private static Cursor LoadCursor(byte[] cursorBytes)
            {
                using (var ms = new MemoryStream(cursorBytes))
                {
                    return new Cursor(ms);
                }
            }
        }

        #region Construction

        /// <summary>
        /// Creates a new OxyPlot properties toolbar.
        /// </summary>
        public OxyPlotToolbar()
        {
            InitializeComponent();

            // Custom cursors are loaded once into CursorCache; no per-instance setup required.

            // Create leader line canvas and leader line
            _leaderLine.StrokeThickness = 2;
            _leaderLine.Visibility = Visibility.Collapsed;
            _leaderLine.Stroke = new SolidColorBrush(Colors.SkyBlue);
            _leaderLine.StrokeDashArray = new DoubleCollection(LineStyle.DashDashDot.GetDashArray());

            _leaderLineCanvas.Children.Add(_leaderLine);

            Loaded += OxyPlotToolbar_Loaded;
            Unloaded += OxyPlotToolbar_Unloaded;
        }

        /// <summary>
        /// Handles the Loaded event. Subscribes to theme changes.
        /// </summary>
        private void OxyPlotToolbar_Loaded(object sender, RoutedEventArgs e)
        {
            // Prevent stacking duplicate subscriptions (defensive against re-entrant Loaded)
            ThemeService.Instance.ThemeChanged -= OnAppThemeChanged;
            ThemeService.Instance.ThemeChanged += OnAppThemeChanged;

            // Apply theme if it changed while the toolbar was unloaded (inactive tab)
            var currentTheme = ThemeService.Instance.CurrentTheme;
            if (Plot != null && _lastAppliedTheme != currentTheme)
            {
                _lastAppliedTheme = currentTheme;
                Plot.SuppressPropertyChanged = true;
                try
                {
                    var theme = OxyPlotThemeManager.GetThemeFor(currentTheme);
                    OxyPlotThemeManager.ApplyTheme(Plot, theme);
                }
                finally
                {
                    Plot.SuppressPropertyChanged = false;
                    // ApplyTheme's internal InvalidatePlot was gated out by the Plot-level
                    // suppression flag above; flush now that the gate is closed so the theme
                    // takes effect immediately rather than at the next user interaction.
                    Plot.InvalidatePlot(false);
                }
            }
        }

        /// <summary>
        /// Handles the Unloaded event. Unsubscribes from theme changes.
        /// Plot-side event subscriptions are managed by <see cref="InitializePlot"/> (the Plot
        /// dependency-property changed callback), so they are detached automatically when the
        /// Plot binding is cleared. Cursors live in <see cref="CursorCache"/> and are not
        /// per-instance, so there is nothing to dispose here.
        /// </summary>
        private void OxyPlotToolbar_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeService.Instance.ThemeChanged -= OnAppThemeChanged;
        }

        /// <summary>
        /// Handles the ThemeService.ThemeChanged event. Applies the new theme to the connected plot
        /// after confirming with the user (unless the warning has been suppressed).
        /// </summary>
        private void OnAppThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            if (Plot == null) return;

            if (!OxyPlotThemeManager.ConfirmThemeChange(Window.GetWindow(this)))
                return;

            _lastAppliedTheme = e.NewTheme;
            Plot.SuppressPropertyChanged = true;
            try
            {
                var theme = OxyPlotThemeManager.GetThemeFor(e.NewTheme);
                OxyPlotThemeManager.ApplyTheme(Plot, theme);
            }
            finally
            {
                Plot.SuppressPropertyChanged = false;
                // ApplyTheme's internal InvalidatePlot was gated out by the Plot-level
                // suppression flag above; flush now that the gate is closed.
                Plot.InvalidatePlot(false);
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// Dependency property for the Plot property.
        /// </summary>
        public static readonly DependencyProperty PlotProperty = DependencyProperty.Register(
            nameof(Plot), typeof(Wpf.Plot), typeof(OxyPlotToolbar), new PropertyMetadata(null, InitializePlot));

        /// <summary>
        /// Gets and sets the OxyPlot Plot associated with this toolbar.
        /// </summary>
        public Wpf.Plot Plot
        {
            get => (Wpf.Plot)GetValue(PlotProperty);
            set => SetValue(PlotProperty, value);
        }

        /// <summary>
        /// Property changed callback for the Plot property.
        /// </summary>
        private static void InitializePlot(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d is not OxyPlotToolbar) return;

            var oxyToolBar = (OxyPlotToolbar)d;

            // Remove handlers from old plot
            if (e.OldValue is Wpf.Plot oldPlot)
            {
                if (oldPlot.ActualModel != null)
                {
                    oldPlot.ActualModel.MouseDown -= oxyToolBar.PlotModelMouseDown;
                    oldPlot.ActualModel.MouseMove -= oxyToolBar.PlotModelMouseMove;
                    oldPlot.ActualModel.MouseUp -= oxyToolBar.PlotModelMouseUp;
                }
                oldPlot.Annotations.CollectionChanged -= oxyToolBar.PlotModelAnnotationCollectionChanged;
                oldPlot.LayoutUpdated -= oxyToolBar.ToolBarLayoutUpdated;

                if (oldPlot.grid != null)
                {
                    oldPlot.grid.Children.Remove(oxyToolBar._leaderLineCanvas);
                }
            }

            // Add handlers to new plot
            if (e.NewValue is Wpf.Plot newPlot)
            {
                // Set up the mouse events
                if (newPlot.ActualModel != null)
                {
                    newPlot.ActualModel.MouseDown += oxyToolBar.PlotModelMouseDown;
                    newPlot.ActualModel.MouseMove += oxyToolBar.PlotModelMouseMove;
                    newPlot.ActualModel.MouseUp += oxyToolBar.PlotModelMouseUp;
                }
                newPlot.Annotations.CollectionChanged += oxyToolBar.PlotModelAnnotationCollectionChanged;
                foreach (var annotation in newPlot.Annotations)
                {
                    oxyToolBar.AttachAnnotationHandlers(annotation);
                }

                newPlot.ApplyTemplate(); // Needed to set the canvas

                // Define the zooming cursor (cached static cursors).
                newPlot.ZoomHorizontalCursor = _zoomCursor;
                newPlot.ZoomRectangleCursor = _zoomCursor;
                newPlot.ZoomVerticalCursor = _zoomCursor;

                // Define the pan cursor.
                newPlot.PanCursor = _panHandCursor;

                // Set up the mouse bindings
                if (oxyToolBar.PointerButton.IsChecked == true) oxyToolBar.PointerButton_Click(oxyToolBar, new RoutedEventArgs());
                if (oxyToolBar.ZoomButton.IsChecked == true) oxyToolBar.ZoomButton_Click(oxyToolBar, new RoutedEventArgs());
                if (oxyToolBar.PanButton.IsChecked == true) oxyToolBar.PanButton_Click(oxyToolBar, new RoutedEventArgs());

                newPlot.LayoutUpdated += oxyToolBar.ToolBarLayoutUpdated;

                // Set up leader line for adding polyline and polygon annotations
                newPlot.grid.Children.Add(oxyToolBar._leaderLineCanvas);

                // Apply current theme to the newly connected plot (only if toolbar is already loaded;
                // during initial construction, the Loaded handler will apply the theme instead)
                if (oxyToolBar.IsLoaded)
                {
                    newPlot.SuppressPropertyChanged = true;
                    try
                    {
                        var theme = OxyPlotThemeManager.GetThemeFor(ThemeService.Instance.CurrentTheme);
                        OxyPlotThemeManager.ApplyTheme(newPlot, theme);
                    }
                    finally
                    {
                        newPlot.SuppressPropertyChanged = false;
                        // ApplyTheme's internal InvalidatePlot was gated out by the Plot-level
                        // suppression flag above; flush now that the gate is closed.
                        newPlot.InvalidatePlot(false);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the toolbar margin based on plot layout.
        /// </summary>
        private void ToolBarLayoutUpdated(object? sender, EventArgs eventArgs)
        {
            if (Plot?.ActualModel == null) return;
            var model = Plot.ActualModel;
            if (!string.IsNullOrEmpty(Plot.Title))
            {
                OxyToolBar.Margin = new Thickness(OxyToolBar.Margin.Left, model.ActualPlotMargins.Top + model.TitleArea.Bottom - model.TitlePadding, OxyToolBar.Margin.Right, OxyToolBar.Margin.Bottom);
            }
            else
            {
                OxyToolBar.Margin = new Thickness(OxyToolBar.Margin.Left, model.ActualPlotMargins.Top + model.Padding.Top, OxyToolBar.Margin.Right, OxyToolBar.Margin.Bottom);
            }
        }

        /// <summary>
        /// Dependency property for the icon size.
        /// </summary>
        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize), typeof(double), typeof(OxyPlotToolbar), new PropertyMetadata(20.0));

        /// <summary>
        /// Gets and sets the toolbar icon size.
        /// </summary>
        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        /// <summary>
        /// Dependency property for the toolbar orientation.
        /// </summary>
        public static readonly DependencyProperty ToolBarOrientationProperty = DependencyProperty.Register(
            nameof(ToolBarOrientation), typeof(Orientation), typeof(OxyPlotToolbar), new UIPropertyMetadata(Orientation.Vertical));

        /// <summary>
        /// Gets and sets the toolbar orientation.
        /// </summary>
        public Orientation ToolBarOrientation
        {
            get => (Orientation)GetValue(ToolBarOrientationProperty);
            set => SetValue(ToolBarOrientationProperty, value);
        }

        private TextBox _textBox = null!;
        private ContextMenu _contextMenu = null!;

        /// <summary>
        /// Enumeration for adding annotation tool mode.
        /// </summary>
        public enum AddToolMode
        {
            /// <summary>
            /// No annotation tool is active.
            /// </summary>
            None,
            /// <summary>
            /// Arrow annotation tool is active.
            /// </summary>
            AddArrowAnnotation,
            /// <summary>
            /// Text annotation tool is active.
            /// </summary>
            AddTextAnnotation,
            /// <summary>
            /// Rectangle annotation tool is active.
            /// </summary>
            AddRectangleAnnotation,
            /// <summary>
            /// Ellipse annotation tool is active.
            /// </summary>
            AddEllipseAnnotation,
            /// <summary>
            /// Point annotation tool is active.
            /// </summary>
            AddPointAnnotation,
            /// <summary>
            /// Polygon annotation tool is active.
            /// </summary>
            AddPolygonAnnotation,
            /// <summary>
            /// Polyline annotation tool is active.
            /// </summary>
            AddPolylineAnnotation,
            /// <summary>
            /// Vertical line annotation tool is active.
            /// </summary>
            AddVerticalLineAnnotation,
            /// <summary>
            /// Horizontal line annotation tool is active.
            /// </summary>
            AddHorizontalLineAnnotation
        }

        // Custom cursors live in the static CursorCache nested type above; access them
        // through the helper properties below to keep call sites readable.
        private static Cursor _movePointsCursor => CursorCache.MovePoints;
        private static Cursor _addPointCursor => CursorCache.AddPoint;
        private static Cursor _panHandCursor => CursorCache.PanHand;
        private static Cursor _panHandClosedCursor => CursorCache.PanHandClosed;
        private static Cursor _zoomCursor => CursorCache.Zoom;

        /// <summary>
        /// The distance in screen pixels used for annotation hit-testing, edge detection, and point proximity checks.
        /// </summary>
        private const double HitTestTolerance = 10;

        /// <summary>
        /// The minimum size in screen pixels for newly created rectangle and ellipse annotations.
        /// </summary>
        private const double MinAnnotationSize = 10;

        // Edit Annotation variables
        private bool _doubleClicked = false;
        private bool _showPoints = false;
        private Polyline _leaderLine = new Polyline();
        private Canvas _leaderLineCanvas = new Canvas();
        private ScreenPoint _lastScreenPoint = ScreenPoint.Undefined;
        private bool _moveStartPoint = false;
        private bool _moveEndPoint = false;
        private int _movePointIndex = -1;
        private bool _scaleMaxX = false;
        private bool _scaleMaxY = false;
        private bool _scaleMinX = false;
        private bool _scaleMinY = false;
        // Adding Annotations
        private AddToolMode _addAnnotationToolMode = AddToolMode.None;
        private Wpf.Annotation _targetAddAnnotation = null!;

        // Annotation marker overlays (WPF elements drawn on the overlay canvas for hit-point markers)
        private readonly List<UIElement> _markerOverlays = new();

        // Tracks toolbar mouse-handler wiring without keeping removed annotations alive.
        private readonly ConditionalWeakTable<Wpf.Annotation, object> _annotationsWithMouseHandlers = new();

        private static readonly object AnnotationHandlersAttached = new();

        /// <summary>
        /// Delegate for the PropertiesCalled event.
        /// </summary>
        /// <param name="targetPlot">The plot whose properties need to be opened.</param>
        /// <param name="openProperties">Boolean value indicating if plot properties should be opened.</param>
        /// <param name="propertyExpander">The property expander that needs to be expanded.</param>
        /// <param name="selectedObject">The selected plot object to edit.</param>
        public delegate void PropertiesCalledEventHandler(Wpf.Plot targetPlot, bool openProperties, OxyPlotPropertiesControl.PropertyEXP? propertyExpander, object selectedObject);

        /// <summary>
        /// Event indicating the plot properties need to be opened.
        /// </summary>
        public event PropertiesCalledEventHandler? PropertiesCalled;

        // Non-swappable series types
        private static readonly HashSet<Type> _nonSwapSeriesTypes = new HashSet<Type>
        {
            typeof(Wpf.HistogramSeries),
            typeof(Wpf.BarSeries),
            typeof(Wpf.ColumnSeries),
            typeof(Wpf.HeatMapSeries)
        };

        #endregion

        #region Pan & Zoom

        /// <summary>
        /// User clicked the pointer button.
        /// </summary>
        private void PointerButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;

            var controller = Plot.ActualController;
            controller.UnbindAll();
            controller.BindMouseDown(OxyMouseButton.Middle, PlotCommands.PanAt);
            controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.SnapTrack);
            controller.BindMouseWheel(PlotCommands.ZoomWheel);
            controller.BindKeyDown(OxyKey.Escape, PlotCommands.Reset);

            SetCursor();
        }

        /// <summary>
        /// User clicked the pan button.
        /// </summary>
        private void PanButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;

            var controller = Plot.ActualController;
            controller.UnbindAll();
            controller.BindMouseDown(OxyMouseButton.Middle, PlotCommands.PanAt);
            controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.PanAt);
            controller.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);
            controller.BindMouseWheel(PlotCommands.ZoomWheel);
            controller.BindKeyDown(OxyKey.Escape, PlotCommands.Reset);

            SetCursor();
        }

        /// <summary>
        /// User clicked zoom button.
        /// </summary>
        private void ZoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;

            var controller = Plot.ActualController;
            controller.UnbindAll();
            controller.BindMouseDown(OxyMouseButton.Middle, PlotCommands.PanAt);
            controller.BindMouseDown(OxyMouseButton.Left, PlotCommands.ZoomRectangle);
            controller.BindMouseDown(OxyMouseButton.Right, PlotCommands.SnapTrack);
            controller.BindMouseWheel(PlotCommands.ZoomWheel);
            controller.BindKeyDown(OxyKey.Escape, PlotCommands.Reset);

            SetCursor();
        }

        /// <summary>
        /// User clicked zoom to extents.
        /// </summary>
        private void ZoomAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;

            // ResetAllAxes already calls InvalidatePlot(false). A second InvalidatePlot
            // here was running PlotModel.Update twice for every Zoom-To-Extents click.
            Plot.ResetAllAxes();
            Plot.Focus();
        }

        /// <summary>
        /// Sets the mouse cursor based on the current tool mode.
        /// </summary>
        private void SetCursor()
        {
            if (_addAnnotationToolMode != AddToolMode.None)
            {
                Plot.DefaultPlotCursor = _addPointCursor;
                Plot.Cursor = _addPointCursor;
            }
            else if (PanButton.IsChecked == true)
            {
                Plot.PanCursor = _panHandCursor;
                Plot.DefaultPlotCursor = _panHandCursor;
                Plot.Cursor = _panHandCursor;
            }
            else if (PointerButton.IsChecked == true)
            {
                Plot.DefaultPlotCursor = Cursors.Arrow;
                Plot.Cursor = Cursors.Arrow;
            }
            else if (ZoomButton.IsChecked == true)
            {
                Plot.DefaultPlotCursor = _zoomCursor;
                Plot.Cursor = _zoomCursor;
            }
            else
            {
                Plot.DefaultPlotCursor = Cursors.Arrow;
                Plot.Cursor = Cursors.Arrow;
            }
        }

        #endregion

        #region Annotations

        /// <summary>
        /// When Add button is clicked, show context menu.
        /// Creates the context menu dynamically to ensure it always uses the current theme.
        /// </summary>
        private void AddAnnotationToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Create context menu dynamically to ensure it picks up current theme
            var annotationMenu = new ContextMenu();

            // Helper to create menu item with icon
            MenuItem CreateAnnotationMenuItem(string header, string iconKey, RoutedEventHandler clickHandler)
            {
                var menuItem = new MenuItem { Header = header };
                var icon = TryFindResource(iconKey);
                if (icon != null)
                {
                    menuItem.Icon = new ContentControl { Content = icon };
                }
                menuItem.Click += clickHandler;
                return menuItem;
            }

            // Add annotation menu items
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Arrow Annotation", "ArrowAnnotationIcon", AddArrowAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Text Annotation", "TextAnnotationIcon", AddTextAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Vertical Line Annotation", "VerticalLineAnnotationIcon", AddVerticalLineAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Horizontal Line Annotation", "HorizontalLineAnnotationIcon", AddHorizontalLineAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Rectangle Annotation", "RectangleAnnotationIcon", AddRectangleAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Ellipse Annotation", "EllipseAnnotationIcon", AddEllipseAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Point Annotation", "PointAnnotationIcon", AddPointAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Polygon Annotation", "PolygonAnnotationIcon", AddPolygonAnnotationItem_Click));
            annotationMenu.Items.Add(CreateAnnotationMenuItem("Polyline Annotation", "PolylineAnnotationIcon", AddPolylineAnnotationItem_Click));

            // Position and show the menu
            annotationMenu.PlacementTarget = AddAnnotationToggleButton;
            annotationMenu.Placement = PlacementMode.Bottom;
            annotationMenu.IsOpen = true;
        }

        /// <summary>
        /// Add arrow annotation.
        /// </summary>
        private void AddArrowAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddArrowAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add text annotation.
        /// </summary>
        private void AddTextAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddTextAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add vertical line annotation.
        /// </summary>
        private void AddVerticalLineAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddVerticalLineAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add horizontal line annotation.
        /// </summary>
        private void AddHorizontalLineAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddHorizontalLineAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add rectangle annotation.
        /// </summary>
        private void AddRectangleAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddRectangleAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add ellipse annotation.
        /// </summary>
        private void AddEllipseAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddEllipseAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add point annotation.
        /// </summary>
        private void AddPointAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            Plot.ActualController.UnbindAll();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddPointAnnotation;
            SetCursor();
        }

        /// <summary>
        /// Add polygon annotation.
        /// </summary>
        private void AddPolygonAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddPolygonAnnotation;
            _leaderLine.Visibility = Visibility.Visible;
            _leaderLine.Points.Clear();
            SetCursor();
        }

        /// <summary>
        /// Add polyline annotation.
        /// </summary>
        private void AddPolylineAnnotationItem_Click(object sender, RoutedEventArgs e)
        {
            StopAddAnnotation();
            BindEscapeToCancelAnnotation();
            _addAnnotationToolMode = AddToolMode.AddPolylineAnnotation;
            _leaderLine.Visibility = Visibility.Visible;
            _leaderLine.Points.Clear();
            SetCursor();
        }

        /// <summary>
        /// Removes all WPF marker overlay elements from the plot canvas and clears the tracking list.
        /// </summary>
        private void ClearMarkerOverlays()
        {
            if (_markerOverlays.Count > 0)
            {
                foreach (var overlay in _markerOverlays)
                {
                    Plot.canvas.Children.Remove(overlay);
                }
                _markerOverlays.Clear();
            }
        }

        /// <summary>
        /// Stop adding the annotation.
        /// </summary>
        private void StopAddAnnotation()
        {
            if (_addAnnotationToolMode != AddToolMode.None)
            {
                bool annotationWasCreated = _targetAddAnnotation != null;

                ClearMarkerOverlays();

                // Unsuppress PropertyChanged and notify the plot so undo bridges
                // can rebuild their shadow state to match the final property values.
                // try/finally guarantees suppression is cleared even if NotifyAnnotationsModified
                // or InvalidatePlot throws — otherwise the annotation would remain permanently
                // silenced and break undo/redo for that annotation.
                if (_targetAddAnnotation != null)
                {
                    try
                    {
                        _targetAddAnnotation.SuppressPropertyChanged = false;
                        Plot.NotifyAnnotationsModified();
                        Plot.InvalidatePlot(false);
                    }
                    finally
                    {
                        _targetAddAnnotation.SuppressPropertyChanged = false;
                    }
                }

                if (_addAnnotationToolMode == AddToolMode.AddPolygonAnnotation || _addAnnotationToolMode == AddToolMode.AddPolylineAnnotation)
                {
                    _leaderLine.Visibility = Visibility.Collapsed;
                    _leaderLine.Points.Clear();
                    Plot.InvalidatePlot(false);
                }

                _doubleClicked = false;
                _addAnnotationToolMode = AddToolMode.None;
                _targetAddAnnotation = null!;

                if (PanButton.IsChecked == true)
                {
                    PanButton_Click(null!, null!);
                }
                else if (PointerButton.IsChecked == true)
                {
                    PointerButton_Click(null!, null!);
                }
                else if (ZoomButton.IsChecked == true)
                {
                    ZoomButton_Click(null!, null!);
                }
                SetCursor();
            }
        }

        /// <summary>
        /// Cancels the in-progress annotation placement, removing the partially-created annotation
        /// from the plot. Called when the user presses Escape during annotation placement.
        /// </summary>
        private void CancelAddAnnotation()
        {
            if (_addAnnotationToolMode == AddToolMode.None) return;

            // Remove the in-progress annotation from the plot before stopping.
            // Always clear suppression on the tracked annotation — even if it never made it
            // into Plot.Annotations (e.g. an exception fired between SuppressPropertyChanged=true
            // and the Plot.Annotations.Add call). Otherwise the orphaned annotation would
            // remain permanently silenced and a future reuse via the same reference would
            // suppress all change notifications.
            if (_targetAddAnnotation != null)
            {
                try
                {
                    _targetAddAnnotation.SuppressPropertyChanged = false;
                    if (Plot.Annotations.Contains(_targetAddAnnotation))
                    {
                        Plot.Annotations.Remove(_targetAddAnnotation);
                    }
                }
                finally
                {
                    _targetAddAnnotation.SuppressPropertyChanged = false;
                }
            }

            // For polygon/polyline that may not be added yet, just clear the leader line
            if (_addAnnotationToolMode == AddToolMode.AddPolygonAnnotation || _addAnnotationToolMode == AddToolMode.AddPolylineAnnotation)
            {
                _leaderLine.Visibility = Visibility.Collapsed;
                _leaderLine.Points.Clear();
            }

            _doubleClicked = false;
            _addAnnotationToolMode = AddToolMode.None;
            _targetAddAnnotation = null!;

            // Restore the previous tool mode
            if (PanButton.IsChecked == true)
            {
                PanButton_Click(null!, null!);
            }
            else if (PointerButton.IsChecked == true)
            {
                PointerButton_Click(null!, null!);
            }
            else if (ZoomButton.IsChecked == true)
            {
                ZoomButton_Click(null!, null!);
            }

            Plot.InvalidatePlot(false);
            SetCursor();
        }

        /// <summary>
        /// Binds the Escape key to cancel the current annotation placement via the OxyPlot controller.
        /// Called after UnbindAll() when entering an annotation add mode.
        /// </summary>
        private void BindEscapeToCancelAnnotation()
        {
            Plot.ActualController.BindKeyDown(OxyKey.Escape,
                new DelegatePlotCommand<OxyKeyEventArgs>((view, controller, args) =>
                {
                    CancelAddAnnotation();
                    args.Handled = true;
                }));
        }

        /// <summary>
        /// Attaches annotation mouse handlers for annotations that already existed before
        /// the toolbar subscribed to collection changes.
        /// </summary>
        /// <param name="annotation">The annotation to wire for toolbar interaction.</param>
        private void AttachAnnotationHandlers(Wpf.Annotation annotation)
        {
            this.PlotModelAnnotationCollectionChanged(
                this.Plot?.Annotations,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, annotation));
        }

        /// <summary>
        /// Gets the current screen position for a text annotation.
        /// </summary>
        /// <param name="annotation">The text annotation to inspect.</param>
        /// <returns>The annotation screen point, or the plot center when no position is defined.</returns>
        private ScreenPoint GetCurrentTextAnnotationScreenPoint(Wpf.TextAnnotation annotation)
        {
            return annotation.TextPosition.IsDefined()
                ? annotation.InternalAnnotation.Transform(annotation.TextPosition)
                : Plot.ActualModel.PlotArea.Center;
        }

        /// <summary>
        /// A new annotation has been added to the plot. Adds the appropriate handlers.
        /// </summary>
        private void PlotModelAnnotationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null) return;

            foreach (var item in e.NewItems)
            {
                if (item is not Wpf.Annotation annotation) continue;
                if (this._annotationsWithMouseHandlers.TryGetValue(annotation, out _)) continue;
                this._annotationsWithMouseHandlers.Add(annotation, AnnotationHandlersAttached);

                if (item is Wpf.ArrowAnnotation)
                {
                    var newArrow = (Wpf.ArrowAnnotation)item;

                    newArrow.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newArrow.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _moveStartPoint = ae.HitTestResult.Index != 2;
                        _moveEndPoint = ae.HitTestResult.Index != 1;
                        newArrow.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newArrow.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newArrow.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        TryOffsetDataPoint(newArrow, newArrow.StartPoint, dx, dy, out var startDataPoint);
                        TryOffsetDataPoint(newArrow, newArrow.EndPoint, dx, dy, out var endDataPoint);

                        if (_moveStartPoint && startDataPoint.IsDefined()) newArrow.StartPoint = startDataPoint;
                        if (_moveEndPoint && endDataPoint.IsDefined()) newArrow.EndPoint = endDataPoint;

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newArrow.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newArrow.IsEnabled) return;
                        newArrow.SuppressPropertyChanged = false;
                        newArrow.RaisePropertyChanged(nameof(newArrow.StartPoint), nameof(newArrow.EndPoint));
                    };
                }
                else if (item is Wpf.TextAnnotation)
                {
                    var newText = (Wpf.TextAnnotation)item;

                    newText.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newText.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _moveStartPoint = ae.HitTestResult.Index == 0;
                        newText.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newText.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newText.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        var theDataPoint = DataPoint.Undefined;
                        if (newText.TextPosition.IsDefined())
                        {
                            TryOffsetDataPoint(newText, newText.TextPosition, dx, dy, out theDataPoint);
                        }
                        else
                        {
                            var theScreenPoint = GetCurrentTextAnnotationScreenPoint(newText);
                            TryScreenPointToDataPoint(newText, new ScreenPoint(theScreenPoint.X + dx, theScreenPoint.Y + dy), out theDataPoint);
                        }

                        if (_moveStartPoint && theDataPoint.IsDefined()) newText.TextPosition = theDataPoint;

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newText.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newText.IsEnabled) return;
                        newText.SuppressPropertyChanged = false;
                        newText.RaisePropertyChanged(nameof(newText.TextPosition));
                    };
                }
                else if (item is Wpf.RectangleAnnotation)
                {
                    var newRect = (Wpf.RectangleAnnotation)item;

                    newRect.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newRect.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        var upperRight = newRect.InternalAnnotation.Transform(newRect.MaximumX, newRect.MaximumY);
                        var lowerLeft = newRect.InternalAnnotation.Transform(newRect.MinimumX, newRect.MinimumY);
                        var topRight = new ScreenPoint(Math.Abs(upperRight.X - ae.Position.X), Math.Abs(upperRight.Y - ae.Position.Y));
                        var bottomLeft = new ScreenPoint(Math.Abs(lowerLeft.X - ae.Position.X), Math.Abs(lowerLeft.Y - ae.Position.Y));

                        _scaleMaxX = topRight.X < HitTestTolerance;
                        _scaleMaxY = topRight.Y < HitTestTolerance;
                        _scaleMinX = bottomLeft.X < HitTestTolerance;
                        _scaleMinY = bottomLeft.Y < HitTestTolerance;

                        if (ae.HitTestResult.Index == 0)
                        {
                            _moveStartPoint = !_scaleMaxX && !_scaleMaxY && !_scaleMinX && !_scaleMinY;
                        }

                        newRect.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newRect.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newRect.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        TryOffsetDataPoint(newRect, new DataPoint(newRect.MaximumX, newRect.MaximumY), dx, dy, out var upperRightDataPoint);
                        TryOffsetDataPoint(newRect, new DataPoint(newRect.MinimumX, newRect.MinimumY), dx, dy, out var lowerLeftDataPoint);

                        if (_scaleMaxX && upperRightDataPoint.IsDefined()) newRect.MaximumX = upperRightDataPoint.X;
                        if (_scaleMaxY && upperRightDataPoint.IsDefined()) newRect.MaximumY = upperRightDataPoint.Y;
                        if (_scaleMinX && lowerLeftDataPoint.IsDefined()) newRect.MinimumX = lowerLeftDataPoint.X;
                        if (_scaleMinY && lowerLeftDataPoint.IsDefined()) newRect.MinimumY = lowerLeftDataPoint.Y;

                        if (_moveStartPoint && upperRightDataPoint.IsDefined() && lowerLeftDataPoint.IsDefined())
                        {
                            newRect.MaximumX = upperRightDataPoint.X;
                            newRect.MaximumY = upperRightDataPoint.Y;
                            newRect.MinimumX = lowerLeftDataPoint.X;
                            newRect.MinimumY = lowerLeftDataPoint.Y;
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newRect.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newRect.IsEnabled) return;
                        newRect.SuppressPropertyChanged = false;
                        newRect.RaisePropertyChanged(nameof(newRect.MinimumX), nameof(newRect.MaximumX), nameof(newRect.MinimumY), nameof(newRect.MaximumY));
                    };
                }
                else if (item is Wpf.EllipseAnnotation)
                {
                    var newEllipse = (Wpf.EllipseAnnotation)item;

                    newEllipse.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newEllipse.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        var upperRight = newEllipse.InternalAnnotation.Transform(newEllipse.MaximumX, newEllipse.MaximumY);
                        var lowerLeft = newEllipse.InternalAnnotation.Transform(newEllipse.MinimumX, newEllipse.MinimumY);
                        var topRight = new ScreenPoint(Math.Abs(upperRight.X - ae.Position.X), Math.Abs(upperRight.Y - ae.Position.Y));
                        var bottomLeft = new ScreenPoint(Math.Abs(lowerLeft.X - ae.Position.X), Math.Abs(lowerLeft.Y - ae.Position.Y));

                        _scaleMaxX = topRight.X < HitTestTolerance;
                        _scaleMaxY = topRight.Y < HitTestTolerance;
                        _scaleMinX = bottomLeft.X < HitTestTolerance;
                        _scaleMinY = bottomLeft.Y < HitTestTolerance;

                        if (ae.HitTestResult.Index == 0)
                        {
                            _moveStartPoint = !_scaleMaxX && !_scaleMaxY && !_scaleMinX && !_scaleMinY;
                        }

                        newEllipse.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newEllipse.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newEllipse.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        TryOffsetDataPoint(newEllipse, new DataPoint(newEllipse.MaximumX, newEllipse.MaximumY), dx, dy, out var upperRightDataPoint);
                        TryOffsetDataPoint(newEllipse, new DataPoint(newEllipse.MinimumX, newEllipse.MinimumY), dx, dy, out var lowerLeftDataPoint);

                        if (_scaleMaxX && upperRightDataPoint.IsDefined()) newEllipse.MaximumX = upperRightDataPoint.X;
                        if (_scaleMaxY && upperRightDataPoint.IsDefined()) newEllipse.MaximumY = upperRightDataPoint.Y;
                        if (_scaleMinX && lowerLeftDataPoint.IsDefined()) newEllipse.MinimumX = lowerLeftDataPoint.X;
                        if (_scaleMinY && lowerLeftDataPoint.IsDefined()) newEllipse.MinimumY = lowerLeftDataPoint.Y;

                        if (_moveStartPoint && upperRightDataPoint.IsDefined() && lowerLeftDataPoint.IsDefined())
                        {
                            newEllipse.MaximumX = upperRightDataPoint.X;
                            newEllipse.MaximumY = upperRightDataPoint.Y;
                            newEllipse.MinimumX = lowerLeftDataPoint.X;
                            newEllipse.MinimumY = lowerLeftDataPoint.Y;
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newEllipse.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newEllipse.IsEnabled) return;
                        newEllipse.SuppressPropertyChanged = false;
                        newEllipse.RaisePropertyChanged(nameof(newEllipse.X), nameof(newEllipse.Y), nameof(newEllipse.Width), nameof(newEllipse.Height));
                    };
                }
                else if (item is Wpf.PointAnnotation)
                {
                    var newPoint = (Wpf.PointAnnotation)item;

                    newPoint.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newPoint.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _moveStartPoint = ae.HitTestResult.Index == 0;
                        newPoint.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPoint.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newPoint.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        TryOffsetDataPoint(newPoint, new DataPoint(newPoint.X, newPoint.Y), dx, dy, out var theDataPoint);

                        if (_moveStartPoint && theDataPoint.IsDefined())
                        {
                            newPoint.X = theDataPoint.X;
                            newPoint.Y = theDataPoint.Y;
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPoint.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newPoint.IsEnabled) return;
                        newPoint.SuppressPropertyChanged = false;
                        newPoint.RaisePropertyChanged(nameof(newPoint.X), nameof(newPoint.Y));
                    };
                }
                else if (item is Wpf.PolygonAnnotation)
                {
                    var newPolygon = (Wpf.PolygonAnnotation)item;

                    newPolygon.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newPolygon.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _movePointIndex = -1;
                        for (int i = 0; i < newPolygon.Points.Count; i++)
                        {
                            if (IsMouseNearDataPoint(newPolygon, newPolygon.Points[i], ae.Position, HitTestTolerance))
                            {
                                _movePointIndex = i;
                                break;
                            }
                        }

                        if (_movePointIndex == -1)
                        {
                            bool onLine = false;
                            for (int i = 0; i < newPolygon.Points.Count - 1; i++)
                            {
                                var p1 = newPolygon.InternalAnnotation.Transform(newPolygon.Points[i]);
                                var p2 = newPolygon.InternalAnnotation.Transform(newPolygon.Points[i + 1]);
                                var linePoint = ScreenPointHelper.FindPointOnLine(ae.Position, p1, p2);
                                if ((linePoint - ae.Position).Length < HitTestTolerance
                                    && TryScreenPointToDataPoint(newPolygon, linePoint, out var insertedPoint))
                                {
                                    newPolygon.Points.Insert(i + 1, insertedPoint);
                                    onLine = true;
                                    _movePointIndex = i + 1;
                                    break;
                                }
                            }

                            if (!onLine)
                            {
                                var p1 = newPolygon.InternalAnnotation.Transform(newPolygon.Points[0]);
                                var p2 = newPolygon.InternalAnnotation.Transform(newPolygon.Points[newPolygon.Points.Count - 1]);
                                var linePoint = ScreenPointHelper.FindPointOnLine(ae.Position, p1, p2);
                                if ((linePoint - ae.Position).Length < HitTestTolerance
                                    && TryScreenPointToDataPoint(newPolygon, linePoint, out var insertedPoint))
                                {
                                    newPolygon.Points.Add(insertedPoint);
                                    _movePointIndex = newPolygon.Points.Count - 1;
                                }
                                else
                                {
                                    if (ae.HitTestResult.Index == 0) _moveStartPoint = true;
                                }
                            }
                        }

                        newPolygon.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPolygon.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newPolygon.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;

                        if (_movePointIndex > -1)
                        {
                            if (TryOffsetDataPoint(newPolygon, newPolygon.Points[_movePointIndex], dx, dy, out var movedPoint))
                            {
                                newPolygon.Points[_movePointIndex] = movedPoint;
                            }
                        }
                        else if (_moveStartPoint)
                        {
                            if (TryOffsetDataPoints(newPolygon, newPolygon.Points, dx, dy, out var movedPoints))
                            {
                                newPolygon.Points = movedPoints;
                            }
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPolygon.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newPolygon.IsEnabled) return;
                        newPolygon.SuppressPropertyChanged = false;
                        newPolygon.RaisePropertyChanged(nameof(newPolygon.Points));
                    };
                }
                else if (item is Wpf.PolylineAnnotation)
                {
                    var newPolyline = (Wpf.PolylineAnnotation)item;

                    newPolyline.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newPolyline.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _movePointIndex = -1;
                        for (int i = 0; i < newPolyline.Points.Count; i++)
                        {
                            if (IsMouseNearDataPoint(newPolyline, newPolyline.Points[i], ae.Position, HitTestTolerance))
                            {
                                _movePointIndex = i;
                                break;
                            }
                        }

                        bool onLine = false;
                        if (_movePointIndex == -1 && ae.IsControlDown)
                        {
                            for (int i = 0; i < newPolyline.Points.Count - 1; i++)
                            {
                                var p1 = newPolyline.InternalAnnotation.Transform(newPolyline.Points[i]);
                                var p2 = newPolyline.InternalAnnotation.Transform(newPolyline.Points[i + 1]);
                                var linePoint = ScreenPointHelper.FindPointOnLine(ae.Position, p1, p2);
                                if ((linePoint - ae.Position).Length < HitTestTolerance
                                    && TryScreenPointToDataPoint(newPolyline, linePoint, out var insertedPoint))
                                {
                                    newPolyline.Points.Insert(i + 1, insertedPoint);
                                    onLine = true;
                                    _movePointIndex = i + 1;
                                    break;
                                }
                            }
                        }

                        if (!onLine) _moveStartPoint = true;

                        newPolyline.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPolyline.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newPolyline.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;

                        if (_movePointIndex > -1)
                        {
                            if (TryOffsetDataPoint(newPolyline, newPolyline.Points[_movePointIndex], dx, dy, out var movedPoint))
                            {
                                newPolyline.Points[_movePointIndex] = movedPoint;
                            }
                        }
                        else if (_moveStartPoint)
                        {
                            if (TryOffsetDataPoints(newPolyline, newPolyline.Points, dx, dy, out var movedPoints))
                            {
                                newPolyline.Points = movedPoints;
                            }
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newPolyline.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newPolyline.IsEnabled) return;
                        newPolyline.SuppressPropertyChanged = false;
                        newPolyline.RaisePropertyChanged(nameof(newPolyline.Points));
                    };
                }
                else if (item is Wpf.LineAnnotation)
                {
                    var newLine = (Wpf.LineAnnotation)item;

                    newLine.InternalAnnotation.MouseDown += (s, ae) =>
                    {
                        if (!newLine.IsEnabled) return;
                        if (_addAnnotationToolMode != AddToolMode.None) return;
                        if (ae.ChangedButton != OxyMouseButton.Left) return;
                        ClearMarkerOverlays();

                        _lastScreenPoint = new ScreenPoint(ae.Position.X, ae.Position.Y);
                        _moveStartPoint = ae.HitTestResult.Index == 0;

                        newLine.SuppressPropertyChanged = true;

                        GetSelectedObjects(s!, ae);

                        if ((Mouse.LeftButton == MouseButtonState.Pressed && PanButton.IsChecked == true) || Mouse.MiddleButton == MouseButtonState.Pressed)
                        {
                            Plot.PanCursor = _panHandClosedCursor;
                            Plot.DefaultPlotCursor = _panHandClosedCursor;
                            Plot.Cursor = _panHandClosedCursor;
                        }

                        OpenLineAnnotationTooltip(newLine);
                        UpdateLineAnnotationTooltip(newLine);
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newLine.InternalAnnotation.MouseMove += (s, ae) =>
                    {
                        if (!newLine.IsEnabled) return;

                        double dx = ae.Position.X - _lastScreenPoint.X;
                        double dy = ae.Position.Y - _lastScreenPoint.Y;
                        TryOffsetDataPoint(newLine, new DataPoint(newLine.X, newLine.Y), dx, dy, out var dataPoint);

                        if (_moveStartPoint)
                        {
                            if (newLine.Type == OxyPlot.Annotations.LineAnnotationType.LinearEquation)
                            {
                                if (dataPoint.IsDefined())
                                {
                                    dx = dataPoint.X - newLine.X;
                                    dy = dataPoint.Y - newLine.Y;
                                    newLine.Intercept += (dy - newLine.Slope * dx);
                                }
                            }
                            else
                            {
                                if (TryScreenPointToDataPoint(newLine, ae.Position, out var mouseDataPoint))
                                {
                                    newLine.X = mouseDataPoint.X;
                                    newLine.Y = mouseDataPoint.Y;
                                }
                            }
                            UpdateLineAnnotationTooltip(newLine);
                        }

                        _lastScreenPoint = ae.Position;
                        Plot.ActualModel.InvalidatePlot(false);
                        ae.Handled = true;
                    };

                    newLine.InternalAnnotation.MouseUp += (s, ae) =>
                    {
                        if (!newLine.IsEnabled) return;
                        newLine.SuppressPropertyChanged = false;
                        newLine.RaisePropertyChanged(nameof(newLine.X), nameof(newLine.Y), nameof(newLine.Intercept));
                        CloseLineAnnotationTooltip(newLine);
                    };
                }
            }
        }

        /// <summary>
        /// Open the line annotation tooltip.
        /// </summary>
        private void OpenLineAnnotationTooltip(Wpf.LineAnnotation lineAnnotation)
        {
            if (lineAnnotation.ToolTip != null)
            {
                ((ToolTip)lineAnnotation.ToolTip).IsOpen = false;
            }

            var toolTip = new ToolTip
            {
                FontFamily = Plot.FontFamily,
                FontSize = Plot.FontSize,
                FontWeight = Plot.FontWeight,
                Placement = PlacementMode.Relative,
                PlacementTarget = Plot.canvas,
                Padding = new Thickness(1),
                Margin = new Thickness(0),
                IsOpen = true
            };
            // Use theme-aware colors for the tooltip
            toolTip.SetResourceReference(Control.BackgroundProperty, "EnvironmentToolTipBackground");
            toolTip.SetResourceReference(Control.BorderBrushProperty, "EnvironmentToolTipBorder");
            toolTip.SetResourceReference(Control.ForegroundProperty, "EnvironmentToolTipText");
            lineAnnotation.ToolTip = toolTip;
        }

        /// <summary>
        /// Update the line annotation tooltip.
        /// </summary>
        private void UpdateLineAnnotationTooltip(Wpf.LineAnnotation lineAnnotation)
        {
            switch (lineAnnotation.Type)
            {
                case OxyPlot.Annotations.LineAnnotationType.Horizontal:
                    if (lineAnnotation.ToolTip != null)
                    {
                        DataPoint dataPoint;
                        if (!lineAnnotation.InternalAnnotation.XAxis!.IsReversed)
                        {
                            dataPoint = new DataPoint(lineAnnotation.InternalAnnotation.XAxis.ClipMinimum, lineAnnotation.Y);
                        }
                        else
                        {
                            dataPoint = new DataPoint(lineAnnotation.InternalAnnotation.XAxis.ClipMaximum, lineAnnotation.Y);
                        }
                        var toolTip = (ToolTip)lineAnnotation.ToolTip;
                        toolTip.Content = lineAnnotation.InternalAnnotation.YAxis!.FormatValue(lineAnnotation.Y);
                        toolTip.UpdateLayout();
                        toolTip.VerticalOffset = lineAnnotation.InternalAnnotation.Transform(dataPoint).Y - toolTip.ActualHeight / 2;
                        toolTip.HorizontalOffset = lineAnnotation.InternalAnnotation.Transform(dataPoint).X - toolTip.ActualWidth;
                    }
                    break;

                case OxyPlot.Annotations.LineAnnotationType.Vertical:
                    if (lineAnnotation.ToolTip != null)
                    {
                        DataPoint dataPoint;
                        if (!lineAnnotation.InternalAnnotation.YAxis!.IsReversed)
                        {
                            dataPoint = new DataPoint(lineAnnotation.X, lineAnnotation.InternalAnnotation.YAxis.ClipMinimum);
                        }
                        else
                        {
                            dataPoint = new DataPoint(lineAnnotation.X, lineAnnotation.InternalAnnotation.YAxis.ClipMaximum);
                        }
                        var toolTip = (ToolTip)lineAnnotation.ToolTip;
                        toolTip.Content = lineAnnotation.InternalAnnotation.XAxis!.FormatValue(lineAnnotation.X);
                        toolTip.UpdateLayout();
                        toolTip.VerticalOffset = lineAnnotation.InternalAnnotation.Transform(dataPoint).Y;
                        toolTip.HorizontalOffset = lineAnnotation.InternalAnnotation.Transform(dataPoint).X - toolTip.ActualWidth / 2;
                    }
                    break;
            }
        }

        /// <summary>
        /// Close the line annotation tooltip.
        /// </summary>
        private void CloseLineAnnotationTooltip(Wpf.LineAnnotation lineAnnotation)
        {
            if (lineAnnotation.ToolTip != null)
            {
                ((ToolTip)lineAnnotation.ToolTip).IsOpen = false;
            }
        }

        #endregion

        #region Mouse Events

        /// <summary>
        /// Plot model mouse down.
        /// </summary>
        private void PlotModelMouseDown(object? sender, OxyMouseDownEventArgs e)
        {
            if (_contextMenu != null)
            {
                _contextMenu.IsOpen = false;
            }
            if (_textBox != null)
            {
                System.Windows.Input.Keyboard.ClearFocus();
            }

            _moveStartPoint = false;
            _moveEndPoint = false;
            _movePointIndex = -1;
            _scaleMaxX = false;
            _scaleMaxY = false;
            _scaleMinX = false;
            _scaleMinY = false;

            if (e.ClickCount == 2)
            {
                _doubleClicked = true;
            }

            if ((Mouse.LeftButton == MouseButtonState.Pressed && PanButton.IsChecked == true) || Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                Plot.PanCursor = _panHandClosedCursor;
                Plot.DefaultPlotCursor = _panHandClosedCursor;
                Plot.Cursor = _panHandClosedCursor;
            }

            if (_addAnnotationToolMode != AddToolMode.None)
            {
                switch (_addAnnotationToolMode)
                {
                    case AddToolMode.AddArrowAnnotation:
                        var newArrow = new Wpf.ArrowAnnotation();
                        newArrow.SuppressPropertyChanged = true;
                        newArrow.Text = "Arrow Annotation";
                        newArrow.HeadLength = 6;
                        newArrow.HeadWidth = 2;
                        newArrow.Veeness = 1;

                        Plot.Annotations.Add(newArrow);
                        if (!TryScreenPointToDataPoint(newArrow, e.Position, out var arrowDataPoint))
                        {
                            Plot.Annotations.Remove(newArrow);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newArrow);
                        newArrow.StartPoint = arrowDataPoint;
                        newArrow.EndPoint = newArrow.StartPoint;
                        _targetAddAnnotation = newArrow;
                        break;

                    case AddToolMode.AddTextAnnotation:
                        var newText = new Wpf.TextAnnotation();
                        newText.SuppressPropertyChanged = true;
                        newText.Text = "Text Annotation";

                        Plot.Annotations.Add(newText);
                        if (!TryScreenPointToDataPoint(newText, e.Position, out var textDataPoint))
                        {
                            Plot.Annotations.Remove(newText);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newText);
                        newText.TextPosition = textDataPoint;
                        _targetAddAnnotation = newText;
                        break;

                    case AddToolMode.AddVerticalLineAnnotation:
                        var newVLine = new Wpf.LineAnnotation();
                        newVLine.SuppressPropertyChanged = true;
                        newVLine.Text = "Vertical Line Annotation";

                        Plot.Annotations.Add(newVLine);
                        if (!TryGetAnnotationAxes(newVLine, out _, out var verticalYAxis)
                            || !TryScreenPointToDataPoint(newVLine, e.Position, out var verticalLineDataPoint))
                        {
                            Plot.Annotations.Remove(newVLine);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newVLine);

                        if (verticalYAxis.IsReversed == false)
                        {
                            newVLine.TextLinePosition = 0;
                            newVLine.TextHorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        }
                        else
                        {
                            newVLine.TextLinePosition = 0;
                            newVLine.TextHorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        }

                        {
                            newVLine.X = verticalLineDataPoint.X;
                            newVLine.Y = verticalLineDataPoint.Y;
                            newVLine.Type = OxyPlot.Annotations.LineAnnotationType.Vertical;
                            _targetAddAnnotation = newVLine;

                            OpenLineAnnotationTooltip(newVLine);
                            UpdateLineAnnotationTooltip(newVLine);
                        }
                        break;

                    case AddToolMode.AddHorizontalLineAnnotation:
                        var newHLine = new Wpf.LineAnnotation();
                        newHLine.SuppressPropertyChanged = true;
                        newHLine.Text = "Horizontal Line Annotation";

                        Plot.Annotations.Add(newHLine);
                        if (!TryGetAnnotationAxes(newHLine, out var horizontalXAxis, out _)
                            || !TryScreenPointToDataPoint(newHLine, e.Position, out var horizontalLineDataPoint))
                        {
                            Plot.Annotations.Remove(newHLine);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newHLine);

                        if (horizontalXAxis.IsReversed == false)
                        {
                            newHLine.TextLinePosition = 0;
                            newHLine.TextHorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        }
                        else
                        {
                            newHLine.TextLinePosition = 0;
                            newHLine.TextHorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        }

                        {
                            newHLine.X = horizontalLineDataPoint.X;
                            newHLine.Y = horizontalLineDataPoint.Y;
                            newHLine.Type = OxyPlot.Annotations.LineAnnotationType.Horizontal;
                            _targetAddAnnotation = newHLine;

                            OpenLineAnnotationTooltip(newHLine);
                            UpdateLineAnnotationTooltip(newHLine);
                        }
                        break;

                    case AddToolMode.AddRectangleAnnotation:
                        var newRectangle = new Wpf.RectangleAnnotation();
                        newRectangle.SuppressPropertyChanged = true;
                        newRectangle.Text = "Rectangle Annotation";

                        Plot.Annotations.Add(newRectangle);
                        if (!TryScreenPointToDataPoint(newRectangle, e.Position, out var rectangleDataPoint))
                        {
                            Plot.Annotations.Remove(newRectangle);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newRectangle);
                        {
                            newRectangle.MinimumX = rectangleDataPoint.X;
                            newRectangle.MaximumX = rectangleDataPoint.X;
                            newRectangle.MinimumY = rectangleDataPoint.Y;
                            newRectangle.MaximumY = rectangleDataPoint.Y;
                            _targetAddAnnotation = newRectangle;
                        }
                        break;

                    case AddToolMode.AddEllipseAnnotation:
                        var newEllipse = new Wpf.EllipseAnnotation();
                        newEllipse.SuppressPropertyChanged = true;
                        newEllipse.Text = "Ellipse Annotation";

                        Plot.Annotations.Add(newEllipse);
                        if (!TryScreenPointToDataPoint(newEllipse, e.Position, out var ellipseDataPoint))
                        {
                            Plot.Annotations.Remove(newEllipse);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newEllipse);
                        {
                            newEllipse.MinimumX = ellipseDataPoint.X;
                            newEllipse.MaximumX = ellipseDataPoint.X;
                            newEllipse.MinimumY = ellipseDataPoint.Y;
                            newEllipse.MaximumY = ellipseDataPoint.Y;
                            _targetAddAnnotation = newEllipse;
                        }
                        break;

                    case AddToolMode.AddPointAnnotation:
                        var newPoint = new Wpf.PointAnnotation();
                        newPoint.SuppressPropertyChanged = true;
                        newPoint.Text = "Point Annotation";
                        newPoint.Size = 5;

                        Plot.Annotations.Add(newPoint);
                        if (!TryScreenPointToDataPoint(newPoint, e.Position, out var pointDataPoint))
                        {
                            Plot.Annotations.Remove(newPoint);
                            break;
                        }

                        PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newPoint);
                        {
                            newPoint.X = pointDataPoint.X;
                            newPoint.Y = pointDataPoint.Y;
                            _targetAddAnnotation = newPoint;
                        }
                        break;

                    case AddToolMode.AddPolygonAnnotation:
                        if (_targetAddAnnotation == null)
                        {
                            // Defend against plots without default axes (e.g. category-only or
                            // not-yet-laid-out plots). Without this, an Undefined point would be
                            // added to the polygon and render as garbage / NaN.
                            var dataPointClicked = ConvertScreenPointToDataPoint(e.Position);
                            if (!dataPointClicked.IsDefined())
                            {
                                break;
                            }

                            var newPolygon = new Wpf.PolygonAnnotation();
                            newPolygon.SuppressPropertyChanged = true;
                            newPolygon.Text = "Polygon Annotation";

                            newPolygon.Points = new System.Collections.Generic.List<DataPoint>();
                            newPolygon.Points.Add(dataPointClicked);
                            _leaderLine.Points.Add(new Point(e.Position.X, e.Position.Y));
                            _leaderLine.Points.Add(new Point(e.Position.X, e.Position.Y));
                            _targetAddAnnotation = newPolygon;
                        }
                        else
                        {
                            _doubleClicked = e.ClickCount > 1;
                            var polyAnnotation = (Wpf.PolygonAnnotation)_targetAddAnnotation;
                            if (polyAnnotation.Points.Count == 3)
                            {
                                Plot.Annotations.Add(polyAnnotation);
                                PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, polyAnnotation);
                            }
                            if (e.ClickCount < 2)
                            {
                                var nextDataPoint = ConvertScreenPointToDataPoint(e.Position);
                                if (nextDataPoint.IsDefined())
                                {
                                    _leaderLine.Points.Add(new Point(e.Position.X, e.Position.Y));
                                    polyAnnotation.Points.Add(nextDataPoint);
                                }
                            }
                        }
                        break;

                    case AddToolMode.AddPolylineAnnotation:
                        if (_targetAddAnnotation == null)
                        {
                            // Defend against plots without default axes — see polygon comment above.
                            var dataPointClicked = ConvertScreenPointToDataPoint(e.Position);
                            if (!dataPointClicked.IsDefined())
                            {
                                break;
                            }

                            var newPolyline = new Wpf.PolylineAnnotation();
                            newPolyline.SuppressPropertyChanged = true;
                            newPolyline.Text = "Polyline Annotation";

                            newPolyline.Points = new System.Collections.Generic.List<DataPoint>();
                            Plot.Annotations.Add(newPolyline);
                            PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, newPolyline);
                            newPolyline.Points.Add(dataPointClicked);
                            _leaderLine.Points.Add(new Point(e.Position.X, e.Position.Y));
                            _leaderLine.Points.Add(new Point(e.Position.X, e.Position.Y));
                            _targetAddAnnotation = newPolyline;
                        }
                        else
                        {
                            _doubleClicked = e.ClickCount > 1;
                            if (e.ClickCount < 2)
                            {
                                // Mirror the polygon path's IsDefined guard: if axes are not yet
                                // laid out (AvalonDock dock/undock window) ConvertScreenPointToDataPoint
                                // returns DataPoint.Undefined; skip rather than appending NaN to the
                                // polyline points.
                                var nextDataPoint = ConvertScreenPointToDataPoint(e.Position);
                                if (nextDataPoint.IsDefined())
                                {
                                    ((Wpf.PolylineAnnotation)_targetAddAnnotation).Points.Add(nextDataPoint);
                                }
                            }
                        }
                        break;
                }

                return;
            }

            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                Plot.DefaultPlotCursor = Cursors.Arrow;
                Plot.Cursor = Cursors.Arrow;
                GetSelectedObjects(sender!, e);
                return;
            }
            else
            {
                GetSelectedObjects(sender!, e);
            }

            if (PanButton.IsChecked == true || Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                Plot.PanCursor = _panHandClosedCursor;
                Plot.DefaultPlotCursor = _panHandClosedCursor;
                Plot.Cursor = _panHandClosedCursor;
            }
        }

        /// <summary>
        /// Plot model mouse move.
        /// </summary>
        private void PlotModelMouseMove(object? sender, OxyMouseEventArgs e)
        {
            // Fast-path: while any mouse button is held (zoom-rect drag, pan, annotation drag,
            // etc.) skip the annotation-hit-test + cursor-update logic below. That logic exists
            // for hover-discovery of annotation handles; during an active drag the user is
            // committed to a manipulator and the per-mouse-move work is wasted (and on plots
            // with many annotations or shapes, expensive). The annotation-add early-out below
            // still runs for the in-progress add-annotation tools.
            if (_addAnnotationToolMode == AddToolMode.None
                && (Mouse.LeftButton == MouseButtonState.Pressed
                    || Mouse.MiddleButton == MouseButtonState.Pressed
                    || Mouse.RightButton == MouseButtonState.Pressed))
            {
                return;
            }

            // For adding annotations
            if (_addAnnotationToolMode != AddToolMode.None && _targetAddAnnotation != null)
            {
                switch (_addAnnotationToolMode)
                {
                    case AddToolMode.AddArrowAnnotation:
                        if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var arrowEndPoint))
                        {
                            ((Wpf.ArrowAnnotation)_targetAddAnnotation).EndPoint = arrowEndPoint;
                        }
                        break;

                    case AddToolMode.AddTextAnnotation:
                        if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var textPosition))
                        {
                            ((Wpf.TextAnnotation)_targetAddAnnotation).TextPosition = textPosition;
                        }
                        break;

                    case AddToolMode.AddVerticalLineAnnotation:
                        if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var verticalLinePoint))
                        {
                            ((Wpf.LineAnnotation)_targetAddAnnotation).X = verticalLinePoint.X;
                            ((Wpf.LineAnnotation)_targetAddAnnotation).Y = verticalLinePoint.Y;
                        }
                        UpdateLineAnnotationTooltip((Wpf.LineAnnotation)_targetAddAnnotation);
                        break;

                    case AddToolMode.AddHorizontalLineAnnotation:
                        if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var horizontalLinePoint))
                        {
                            ((Wpf.LineAnnotation)_targetAddAnnotation).X = horizontalLinePoint.X;
                            ((Wpf.LineAnnotation)_targetAddAnnotation).Y = horizontalLinePoint.Y;
                        }
                        UpdateLineAnnotationTooltip((Wpf.LineAnnotation)_targetAddAnnotation);
                        break;

                    case AddToolMode.AddRectangleAnnotation:
                        {
                            if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var mouseDataPoint))
                            {
                                var rect = (Wpf.RectangleAnnotation)_targetAddAnnotation;
                                rect.MaximumX = mouseDataPoint.X;
                                rect.MaximumY = mouseDataPoint.Y;
                            }
                        }
                        break;

                    case AddToolMode.AddEllipseAnnotation:
                        {
                            if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var mouseDataPoint))
                            {
                                var ellipse = (Wpf.EllipseAnnotation)_targetAddAnnotation;
                                ellipse.MaximumX = mouseDataPoint.X;
                                ellipse.MaximumY = mouseDataPoint.Y;
                            }
                        }
                        break;

                    case AddToolMode.AddPointAnnotation:
                        {
                            if (TryScreenPointToDataPoint(_targetAddAnnotation, e.Position, out var mouseDataPoint))
                            {
                                var point = (Wpf.PointAnnotation)_targetAddAnnotation;
                                point.X = mouseDataPoint.X;
                                point.Y = mouseDataPoint.Y;
                            }
                        }
                        break;

                    case AddToolMode.AddPolygonAnnotation:
                        _leaderLine.Points[_leaderLine.Points.Count - 1] = new Point(e.Position.X, e.Position.Y);
                        // Mouse-move during polygon drag updates the leader-line preview only;
                        // no series data changes, so updateData=false avoids a per-mousemove walk
                        // of all series. Matches the AddPolylineAnnotation case below.
                        Plot.InvalidatePlot(false);
                        break;

                    case AddToolMode.AddPolylineAnnotation:
                        {
                            var polyAnnotation = (Wpf.PolylineAnnotation)_targetAddAnnotation;
                            if (TryDataPointToScreenPoint(polyAnnotation, polyAnnotation.Points[polyAnnotation.Points.Count - 1], out var leaderStartPoint))
                            {
                                _leaderLine.Points[0] = new Point(leaderStartPoint.X, leaderStartPoint.Y);
                            }
                            _leaderLine.Points[_leaderLine.Points.Count - 1] = new Point(e.Position.X, e.Position.Y);
                            Plot.InvalidatePlot(false);
                        }
                        break;
                }
                return;
            }

            // Cursor update logic - show markers and update cursor when hovering over annotations
            bool requiresRedraw = _showPoints;
            _showPoints = false;
            var markerPoints = new List<ScreenPoint>();
            var markerSizes = new List<double>();
            Cursor? updatedCursor = null;

            foreach (var a in Plot.Annotations)
            {
                if (!a.IsEnabled) continue;
                var ht = a.InternalAnnotation.HitTest(new HitTestArguments(e.Position, HitTestTolerance));
                if (ht == null) continue;

                var aType = a.GetType();
                if (aType == typeof(Wpf.ArrowAnnotation))
                {
                    var arrowAnnotation = (Wpf.ArrowAnnotation)a;
                    markerPoints.Add(arrowAnnotation.InternalAnnotation.Transform(arrowAnnotation.StartPoint));
                    markerPoints.Add(arrowAnnotation.InternalAnnotation.Transform(arrowAnnotation.EndPoint));
                    markerSizes.AddRange(new[] { 2.2, 2.2 });

                    switch (ht.Index)
                    {
                        case 0:
                            updatedCursor = Cursors.SizeAll;
                            break;
                        case 1:
                        case 2:
                            updatedCursor = _movePointsCursor;
                            break;
                    }
                }
                else if (aType == typeof(Wpf.TextAnnotation))
                {
                    if (ht.Index == 0) updatedCursor = Cursors.SizeAll;
                }
                else if (aType == typeof(Wpf.RectangleAnnotation))
                {
                    var rAnnotation = (Wpf.RectangleAnnotation)a;
                    var ur = rAnnotation.InternalAnnotation.Transform(Math.Max(rAnnotation.MaximumX, rAnnotation.MinimumX), Math.Max(rAnnotation.MaximumY, rAnnotation.MinimumY));
                    var ll = rAnnotation.InternalAnnotation.Transform(Math.Min(rAnnotation.MinimumX, rAnnotation.MaximumX), Math.Min(rAnnotation.MinimumY, rAnnotation.MaximumY));

                    markerPoints.Add(ur);
                    markerPoints.Add(ll);
                    markerPoints.Add(new ScreenPoint(ll.X, ur.Y));
                    markerPoints.Add(new ScreenPoint(ur.X, ll.Y));
                    markerPoints.Add(new ScreenPoint(ll.X, ll.Y + (ur.Y - ll.Y) / 2));
                    markerPoints.Add(new ScreenPoint(ur.X, ll.Y + (ur.Y - ll.Y) / 2));
                    markerPoints.Add(new ScreenPoint(ll.X + (ur.X - ll.X) / 2, ll.Y));
                    markerPoints.Add(new ScreenPoint(ll.X + (ur.X - ll.X) / 2, ur.Y));
                    markerSizes.AddRange(new[] { 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0 });

                    var topRight = new ScreenPoint(Math.Abs(ur.X - e.Position.X), Math.Abs(ur.Y - e.Position.Y));
                    var bottomLeft = new ScreenPoint(Math.Abs(ll.X - e.Position.X), Math.Abs(ll.Y - e.Position.Y));

                    // Corners
                    if (topRight.X < HitTestTolerance && topRight.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNESW; continue; }
                    if (bottomLeft.X < HitTestTolerance && bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNESW; continue; }
                    if (bottomLeft.X < HitTestTolerance && topRight.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNWSE; continue; }
                    if (topRight.X < HitTestTolerance && bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNWSE; continue; }
                    // Edges
                    if (topRight.X < HitTestTolerance || bottomLeft.X < HitTestTolerance) { updatedCursor = Cursors.SizeWE; continue; }
                    if (topRight.Y < HitTestTolerance || bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNS; continue; }
                    // All
                    if (ht.Index == 0) updatedCursor = Cursors.SizeAll;
                }
                else if (aType == typeof(Wpf.EllipseAnnotation))
                {
                    var eAnnotation = (Wpf.EllipseAnnotation)a;
                    var ur = eAnnotation.InternalAnnotation.Transform(Math.Max(eAnnotation.MaximumX, eAnnotation.MinimumX), Math.Max(eAnnotation.MaximumY, eAnnotation.MinimumY));
                    var ll = eAnnotation.InternalAnnotation.Transform(Math.Min(eAnnotation.MinimumX, eAnnotation.MaximumX), Math.Min(eAnnotation.MinimumY, eAnnotation.MaximumY));

                    markerPoints.Add(ur);
                    markerPoints.Add(ll);
                    markerPoints.Add(new ScreenPoint(ll.X, ur.Y));
                    markerPoints.Add(new ScreenPoint(ur.X, ll.Y));
                    markerPoints.Add(new ScreenPoint(ll.X, ll.Y + (ur.Y - ll.Y) / 2));
                    markerPoints.Add(new ScreenPoint(ur.X, ll.Y + (ur.Y - ll.Y) / 2));
                    markerPoints.Add(new ScreenPoint(ll.X + (ur.X - ll.X) / 2, ll.Y));
                    markerPoints.Add(new ScreenPoint(ll.X + (ur.X - ll.X) / 2, ur.Y));
                    markerSizes.AddRange(new[] { 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0, 2.0 });

                    var topRight = new ScreenPoint(Math.Abs(ur.X - e.Position.X), Math.Abs(ur.Y - e.Position.Y));
                    var bottomLeft = new ScreenPoint(Math.Abs(ll.X - e.Position.X), Math.Abs(ll.Y - e.Position.Y));

                    // Corners
                    if (topRight.X < HitTestTolerance && topRight.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNESW; continue; }
                    if (bottomLeft.X < HitTestTolerance && bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNESW; continue; }
                    if (bottomLeft.X < HitTestTolerance && topRight.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNWSE; continue; }
                    if (topRight.X < HitTestTolerance && bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNWSE; continue; }
                    // Edges
                    if (topRight.X < HitTestTolerance || bottomLeft.X < HitTestTolerance) { updatedCursor = Cursors.SizeWE; continue; }
                    if (topRight.Y < HitTestTolerance || bottomLeft.Y < HitTestTolerance) { updatedCursor = Cursors.SizeNS; continue; }
                    // All
                    if (ht.Index == 0) updatedCursor = Cursors.SizeAll;
                }
                else if (aType == typeof(Wpf.PointAnnotation))
                {
                    if (ht.Index == 0) updatedCursor = Cursors.SizeAll;
                }
                else if (aType == typeof(Wpf.PolygonAnnotation))
                {
                    if (ht.Index == 0)
                    {
                        var polyAnnotation = (Wpf.PolygonAnnotation)a;

                        foreach (var p in polyAnnotation.Points)
                        {
                            markerPoints.Add(polyAnnotation.InternalAnnotation.Transform(p));
                            markerSizes.Add(2);
                        }

                        // Check if cursor is over any points
                        if (polyAnnotation.Points.Any(o => IsMouseNearDataPoint(polyAnnotation, o, e.Position, HitTestTolerance)))
                        {
                            updatedCursor = _movePointsCursor;
                        }
                        else
                        {
                            bool onLine = false;
                            for (int i = 0; i < polyAnnotation.Points.Count - 1; i++)
                            {
                                var p1 = polyAnnotation.InternalAnnotation.Transform(polyAnnotation.Points[i]);
                                var p2 = polyAnnotation.InternalAnnotation.Transform(polyAnnotation.Points[i + 1]);
                                var linePoint = ScreenPointHelper.FindPointOnLine(e.Position, p1, p2);
                                if ((linePoint - e.Position).Length < HitTestTolerance)
                                {
                                    onLine = true;
                                    updatedCursor = _addPointCursor;
                                    break;
                                }
                            }

                            if (!onLine)
                            {
                                var p1 = polyAnnotation.InternalAnnotation.Transform(polyAnnotation.Points[0]);
                                var p2 = polyAnnotation.InternalAnnotation.Transform(polyAnnotation.Points[polyAnnotation.Points.Count - 1]);
                                var linePoint = ScreenPointHelper.FindPointOnLine(e.Position, p1, p2);
                                if ((linePoint - e.Position).Length < HitTestTolerance)
                                {
                                    updatedCursor = _addPointCursor;
                                }
                                else
                                {
                                    updatedCursor = Cursors.SizeAll;
                                }
                            }
                        }
                    }
                }
                else if (aType == typeof(Wpf.PolylineAnnotation))
                {
                    if (ht.Index == 0)
                    {
                        var polylineAnnotation = (Wpf.PolylineAnnotation)a;

                        foreach (var p in polylineAnnotation.Points)
                        {
                            markerPoints.Add(polylineAnnotation.InternalAnnotation.Transform(p));
                            markerSizes.Add(2);
                        }

                        // Check if cursor is over any points
                        if (polylineAnnotation.Points.Any(o => IsMouseNearDataPoint(polylineAnnotation, o, e.Position, HitTestTolerance)))
                        {
                            updatedCursor = _movePointsCursor;
                        }
                        else
                        {
                            if (e.IsControlDown)
                            {
                                updatedCursor = _addPointCursor;
                            }
                            else
                            {
                                updatedCursor = Cursors.SizeAll;
                            }
                        }
                    }
                }
                else if (aType == typeof(Wpf.LineAnnotation))
                {
                    if (ht.Index == 0) updatedCursor = Cursors.SizeAll;
                }
            }

            ClearMarkerOverlays();
            if (markerPoints.Count > 0)
            {
                _showPoints = true;
                for (int i = 0; i < markerPoints.Count; i++)
                {
                    var mp = markerPoints[i];
                    var size = markerSizes[i] * 2;
                    var rect = new System.Windows.Shapes.Rectangle
                    {
                        Width = size,
                        Height = size,
                        Fill = System.Windows.Media.Brushes.White,
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 1,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(rect, mp.X - size / 2);
                    Canvas.SetTop(rect, mp.Y - size / 2);
                    Plot.canvas.Children.Add(rect);
                    _markerOverlays.Add(rect);
                }
            }
            else
            {
                if (requiresRedraw) Plot.InvalidatePlot(false);
            }

            if (updatedCursor == null)
            {
                Plot.Cursor = Plot.DefaultPlotCursor;
            }
            else
            {
                Plot.Cursor = updatedCursor;
                return;
            }

            // Set closed pan hand if needed
            if ((Mouse.LeftButton == MouseButtonState.Pressed && PanButton.IsChecked == true) || Mouse.MiddleButton == MouseButtonState.Pressed)
            {
                Plot.PanCursor = _panHandClosedCursor;
                Plot.DefaultPlotCursor = _panHandClosedCursor;
                Plot.Cursor = _panHandClosedCursor;
            }
        }

        /// <summary>
        /// Plot model mouse up.
        /// </summary>
        private void PlotModelMouseUp(object? sender, OxyMouseEventArgs e)
        {
            if (_addAnnotationToolMode == AddToolMode.AddPolygonAnnotation || _addAnnotationToolMode == AddToolMode.AddPolylineAnnotation)
            {
                if (_doubleClicked) StopAddAnnotation();
            }
            else if (_addAnnotationToolMode == AddToolMode.AddHorizontalLineAnnotation || _addAnnotationToolMode == AddToolMode.AddVerticalLineAnnotation)
            {
                CloseLineAnnotationTooltip((Wpf.LineAnnotation)_targetAddAnnotation);
                StopAddAnnotation();
            }
            else if (_addAnnotationToolMode == AddToolMode.AddRectangleAnnotation)
            {
                // Check to see if the size of rectangle is at least 10 pixels in height and width
                var rectangle = (Wpf.RectangleAnnotation)_targetAddAnnotation;
                ScreenPoint upperRight = rectangle.InternalAnnotation.Transform(rectangle.MaximumX, rectangle.MaximumY);
                ScreenPoint lowerLeft = rectangle.InternalAnnotation.Transform(rectangle.MinimumX, rectangle.MinimumY);
                double pixelWidth = Math.Abs(upperRight.X - lowerLeft.X);
                double pixelHeight = Math.Abs(upperRight.Y - lowerLeft.Y);
                // Correct the height and width if necessary
                if (pixelWidth < MinAnnotationSize || pixelHeight < MinAnnotationSize)
                {
                    if (TryCreateMinimumScreenBounds(rectangle, e.Position, out var minimumPoint, out var maximumPoint))
                    {
                        if (pixelWidth < MinAnnotationSize)
                        {
                            rectangle.MinimumX = minimumPoint.X;
                            rectangle.MaximumX = maximumPoint.X;
                        }
                        if (pixelHeight < MinAnnotationSize)
                        {
                            rectangle.MinimumY = minimumPoint.Y;
                            rectangle.MaximumY = maximumPoint.Y;
                        }
                    }
                }
                StopAddAnnotation();
            }
            else if (_addAnnotationToolMode == AddToolMode.AddEllipseAnnotation)
            {
                // Check to see if the size of the ellipse is at least 10 pixels in height and width
                var ellipse = (Wpf.EllipseAnnotation)_targetAddAnnotation;
                ScreenPoint upperRight = ellipse.InternalAnnotation.Transform(ellipse.MaximumX, ellipse.MaximumY);
                ScreenPoint lowerLeft = ellipse.InternalAnnotation.Transform(ellipse.MinimumX, ellipse.MinimumY);
                double pixelWidth = Math.Abs(upperRight.X - lowerLeft.X);
                double pixelHeight = Math.Abs(upperRight.Y - lowerLeft.Y);
                // Correct the height and width if necessary
                if (pixelWidth < MinAnnotationSize || pixelHeight < MinAnnotationSize)
                {
                    if (TryCreateMinimumScreenBounds(ellipse, e.Position, out var minimumPoint, out var maximumPoint))
                    {
                        if (pixelWidth < MinAnnotationSize)
                        {
                            ellipse.MinimumX = minimumPoint.X;
                            ellipse.MaximumX = maximumPoint.X;
                        }
                        if (pixelHeight < MinAnnotationSize)
                        {
                            ellipse.MinimumY = minimumPoint.Y;
                            ellipse.MaximumY = maximumPoint.Y;
                        }
                    }
                }
                StopAddAnnotation();
            }
            else
            {
                if (_addAnnotationToolMode == AddToolMode.AddArrowAnnotation)
                {
                    var arrow = (Wpf.ArrowAnnotation)_targetAddAnnotation;
                    if (Math.Abs(arrow.StartPoint.X - arrow.EndPoint.X) < 0.000000001 && Math.Abs(arrow.StartPoint.Y - arrow.EndPoint.Y) < 0.000000001)
                    {
                        // Offset the start point (tail) by 5% of plot width in screen space,
                        // then convert back to data space. This works correctly for all axis
                        // types including logarithmic.
                        OxyRect plotArea = Plot.ActualModel.PlotArea;
                        double shiftPixels = Math.Abs(plotArea.Right - plotArea.Left) * 0.05;
                        var arrowScreen = arrow.InternalAnnotation.Transform(arrow.EndPoint);
                        if (TryScreenPointToDataPoint(arrow, new ScreenPoint(arrowScreen.X + shiftPixels, arrowScreen.Y), out var shiftedData))
                        {
                            arrow.StartPoint = shiftedData;
                        }
                    }
                }
                StopAddAnnotation();
            }

            SetCursor();
        }

        /// <summary>
        /// Get the selected objects and build the right-click context menu.
        /// </summary>
        private void GetSelectedObjects(object sender, OxyMouseDownEventArgs e)
        {
            // Middle clicks initiate the Pan option
            if (e.ChangedButton == OxyMouseButton.Middle) return;

            // Left clicks try to edit/open the first thing clicked. Right clicks provide more context.
            bool leftClickBool = e.ChangedButton == OxyMouseButton.Left;

            _contextMenu = new ContextMenu();

            // SERIES hit test
            var seriesHTRS = Plot.ActualModel.HitTest(new HitTestArguments(e.Position, HitTestTolerance)).ToList();
            foreach (var htr in seriesHTRS)
            {
                var wpfSeries = Plot.Series.FirstOrDefault(d => d.InternalSeries.Equals(htr.Element));
                if (wpfSeries != null)
                {
                    if (leftClickBool)
                    {
                        PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Series_General, wpfSeries);
                        return;
                    }
                    else
                    {
                        var seriesItem = new MenuItem { Header = "Format Series: " + wpfSeries.Title, Icon = CreateMenuIcon("Format") };
                        seriesItem.Click += (s, args) => PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Series_General, wpfSeries);
                        _contextMenu.Items.Add(seriesItem);
                    }
                }
            }

            var plotAndAxisArea = Plot.ActualModel.PlotAndAxisArea;
            var plotArea = Plot.ActualModel.PlotArea;

            // Legend Area custom hit test
            var legendArea = Plot.ActualModel.LegendArea;
            if (legendArea.Contains(e.Position))
            {
                if (leftClickBool)
                {
                    PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Legend_Title, legendArea);
                    return;
                }
                else
                {
                    var legendItem = new MenuItem { Header = "Format Legend", Icon = CreateMenuIcon("Format") };
                    legendItem.Click += (s, args) => PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Legend_Title, legendArea);
                    _contextMenu.Items.Add(legendItem);
                }
            }

            // TEXT HIT TEST
            var textResult = Plot.HitTestRenderedText(new Point(e.Position.X, e.Position.Y));
            if (textResult != null)
            {
                string? clickedText = null;
                TextBlock? txtblock = null;
                if (textResult is TextBlock tb)
                {
                    clickedText = tb.Text;
                    txtblock = tb;
                }
                else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thr)
                {
                    clickedText = thr.Text;
                }

                if (clickedText != null)
                {

                    // CHART TITLE SELECTED
                    if (Plot.Title == clickedText && Plot.ActualModel.TitleArea.Contains(new ScreenPoint(e.Position.X, e.Position.Y)))
                    {
                        if (leftClickBool)
                        {
                            PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.General_PlotTitle, Plot.ActualModel.TitleArea);
                            if (txtblock != null) CreateEditTBX(txtblock, Plot, Wpf.Plot.TitleProperty, 0, Plot.canvas);
                            else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrTitle) CreateEditTBXFromBounds(thrTitle.Bounds, thrTitle.FontSize, Plot, Wpf.Plot.TitleProperty, 0, Plot.canvas);
                            return;
                        }
                        else
                        {
                            var editTitleItem = new MenuItem { Header = "Edit Plot Title", Icon = CreateMenuIcon("EditTextbox") };
                            editTitleItem.Click += (s, args) =>
                            {
                                PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.General_PlotTitle, Plot.ActualModel.TitleArea);
                                if (txtblock != null) CreateEditTBX(txtblock, Plot, Wpf.Plot.TitleProperty, 0, Plot.canvas);
                                else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrTitleCtx) CreateEditTBXFromBounds(thrTitleCtx.Bounds, thrTitleCtx.FontSize, Plot, Wpf.Plot.TitleProperty, 0, Plot.canvas);
                            };
                            var formatTitleItem = new MenuItem { Header = "Format Plot Title", Icon = CreateMenuIcon("Format") };
                            formatTitleItem.Click += (s, args) =>
                            {
                                PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.General_PlotTitle, Plot.ActualModel.TitleArea);
                                PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.General_PlotSubtitle, Plot.ActualModel.TitleArea);
                            };
                            _contextMenu.Items.Add(editTitleItem);
                            _contextMenu.Items.Add(formatTitleItem);
                        }
                    }

                    // CHART SUBTITLE SELECTED
                    if (Plot.Subtitle == clickedText && Plot.ActualModel.TitleArea.Contains(new ScreenPoint(e.Position.X, e.Position.Y)))
                    {
                        if (leftClickBool)
                        {
                            PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.General_PlotSubtitle, Plot.ActualModel.TitleArea);
                            if (txtblock != null) CreateEditTBX(txtblock, Plot, Wpf.Plot.SubtitleProperty, 0, Plot.canvas);
                            else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrSub) CreateEditTBXFromBounds(thrSub.Bounds, thrSub.FontSize, Plot, Wpf.Plot.SubtitleProperty, 0, Plot.canvas);
                            return;
                        }
                        else
                        {
                            var editSubtitleItem = new MenuItem { Header = "Edit Plot Subtitle", Icon = CreateMenuIcon("EditTextbox") };
                            editSubtitleItem.Click += (s, args) =>
                            {
                                PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.General_PlotSubtitle, Plot.ActualModel.TitleArea);
                                if (txtblock != null) CreateEditTBX(txtblock, Plot, Wpf.Plot.SubtitleProperty, 0, Plot.canvas);
                                else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrSubCtx) CreateEditTBXFromBounds(thrSubCtx.Bounds, thrSubCtx.FontSize, Plot, Wpf.Plot.SubtitleProperty, 0, Plot.canvas);
                            };
                            var formatSubtitleItem = new MenuItem { Header = "Format Plot Subtitle", Icon = CreateMenuIcon("Format") };
                            formatSubtitleItem.Click += (s, args) =>
                            {
                                PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.General_PlotSubtitle, Plot.ActualModel.TitleArea);
                            };
                            _contextMenu.Items.Add(editSubtitleItem);
                            _contextMenu.Items.Add(formatSubtitleItem);
                        }
                    }

                    // AXES TITLES SELECTED
                    if (Plot.ActualModel.PlotAndAxisArea.Contains(new ScreenPoint(e.Position.X, e.Position.Y)))
                    {
                        var axes = Plot.Axes.Where(x => x.Title != null && clickedText.Contains(x.Title)).ToList();

                        if (axes.Count > 1)
                        {
                            // Narrow it down to the selected axis area
                            OxyRect selectedAxisArea = default;

                            foreach (var ax in axes)
                            {
                                var dummyCanvas = new Canvas();
                                var crc = new Wpf.CanvasRenderContext(dummyCanvas);
                                var size = new Size(Plot.ActualWidth, Plot.ActualHeight);
                                dummyCanvas.Measure(size);
                                dummyCanvas.Arrange(new Rect(size));
                                dummyCanvas.UpdateLayout();

                                ax.InternalAxis.Render(crc, 1);
                                dummyCanvas.UpdateLayout();

                                foreach (var tbk in FindVisualChildren<TextBlock>(dummyCanvas))
                                {
                                    string title = ax.Title;
                                    if (ax.Unit != null)
                                    {
                                        title = string.Format(ax.TitleFormatString, ax.Title, ax.Unit);
                                    }

                                    if (title == tbk.Text)
                                    {
                                        double axLeft, axTop, axWidth, axHeight;
                                        if (ax.Position == OxyPlot.Axes.AxisPosition.Left || ax.Position == OxyPlot.Axes.AxisPosition.Right)
                                        {
                                            axLeft = GetPosition(tbk, dummyCanvas).X;
                                            axTop = GetPosition(tbk, dummyCanvas).Y - tbk.ActualWidth;
                                            axWidth = tbk.ActualHeight;
                                            axHeight = tbk.ActualWidth;
                                        }
                                        else
                                        {
                                            axLeft = GetPosition(tbk, dummyCanvas).X;
                                            axTop = GetPosition(tbk, dummyCanvas).Y;
                                            axWidth = tbk.ActualWidth;
                                            axHeight = tbk.ActualHeight;
                                        }

                                        selectedAxisArea = new OxyRect(axLeft, axTop, axWidth, axHeight);

                                        if (selectedAxisArea.Contains(e.Position))
                                        {
                                            axes = new List<Wpf.Axis> { ax };
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        if (axes.Count == 1)
                        {
                            var ax = axes.First();

                            if (leftClickBool)
                            {
                                PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Axes_Title, ax);
                                if (txtblock != null)
                                {
                                    if (ax.InternalAxis.IsVertical())
                                    {
                                        CreateEditTBX(txtblock, ax, Wpf.Axis.TitleProperty, -90, Plot.canvas);
                                    }
                                    else
                                    {
                                        CreateEditTBX(txtblock, ax, Wpf.Axis.TitleProperty, 0, Plot.canvas);
                                    }
                                }
                                else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrAxis)
                                {
                                    double axAngle = ax.InternalAxis.IsVertical() ? -90 : 0;
                                    CreateEditTBXFromBounds(thrAxis.Bounds, thrAxis.FontSize, ax, Wpf.Axis.TitleProperty, axAngle, Plot.canvas);
                                }
                                return;
                            }
                            else
                            {
                                var editAxisItem = new MenuItem { Header = "Edit Axis Title: " + ax.Title, Icon = CreateMenuIcon("EditTextbox") };
                                editAxisItem.Click += (s, args) =>
                                {
                                    PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Axes_Title, ax);
                                    if (txtblock != null)
                                    {
                                        if (ax.InternalAxis.IsVertical())
                                        {
                                            CreateEditTBX(txtblock, ax, Wpf.Axis.TitleProperty, -90, Plot.canvas);
                                        }
                                        else
                                        {
                                            CreateEditTBX(txtblock, ax, Wpf.Axis.TitleProperty, 0, Plot.canvas);
                                        }
                                    }
                                    else if (textResult is Wpf.DrawingVisualRenderContext.TextHitResult thrAxisCtx)
                                    {
                                        double axAngleCtx = ax.InternalAxis.IsVertical() ? -90 : 0;
                                        CreateEditTBXFromBounds(thrAxisCtx.Bounds, thrAxisCtx.FontSize, ax, Wpf.Axis.TitleProperty, axAngleCtx, Plot.canvas);
                                    }
                                };
                                _contextMenu.Items.Add(editAxisItem);
                            }
                        }
                    }
                }
            }

            // AXIS Areas hit test
            foreach (var ax in Plot.Axes)
            {
                double axLeft = 0, axTop = 0, axWidth = 0, axHeight = 0;

                switch (ax.Position)
                {
                    case OxyPlot.Axes.AxisPosition.Bottom:
                        axLeft = plotArea.Left;
                        axTop = plotArea.Bottom + ax.AxisDistance;
                        axWidth = plotArea.Width;
                        axHeight = ax.InternalAxis.DesiredSize.Height;
                        break;
                    case OxyPlot.Axes.AxisPosition.Top:
                        axLeft = plotArea.Left;
                        axTop = plotArea.Top - ax.AxisDistance - ax.InternalAxis.DesiredSize.Height;
                        axWidth = plotArea.Width;
                        axHeight = ax.InternalAxis.DesiredSize.Height;
                        break;
                    case OxyPlot.Axes.AxisPosition.Left:
                        axLeft = plotArea.Left - ax.AxisDistance - ax.InternalAxis.DesiredSize.Width;
                        axTop = plotArea.Top;
                        axWidth = ax.InternalAxis.DesiredSize.Width;
                        axHeight = plotArea.Height;
                        break;
                    case OxyPlot.Axes.AxisPosition.Right:
                        axLeft = plotArea.Right + ax.AxisDistance;
                        axTop = plotArea.Top;
                        axWidth = ax.InternalAxis.DesiredSize.Width;
                        axHeight = plotArea.Height;
                        break;
                }

                var axArea1 = new OxyRect(axLeft, axTop, axWidth, axHeight);
                if (axArea1.Contains(e.Position))
                {
                    if (leftClickBool)
                    {
                        PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Axes_Options, ax);
                        PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Axes_Display, ax);
                        return;
                    }
                    else
                    {
                        var formatAxisItem = new MenuItem { Header = "Format Axis: " + ax.Title, Icon = CreateMenuIcon("Format") };
                        formatAxisItem.Click += (s, args) =>
                        {
                            PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Axes_Options, ax);
                            PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Axes_Display, ax);
                        };
                        _contextMenu.Items.Add(formatAxisItem);
                    }
                }
            }

            // ANNOTATIONS hit test
            var annoHTRS = Plot.ActualModel.HitTest(new HitTestArguments(e.Position, HitTestTolerance)).ToList();
            foreach (var htr in annoHTRS)
            {
                var theAnno = htr.Element as OxyPlot.Annotations.Annotation;
                if (theAnno != null)
                {
                    var wpfAnno = Plot.Annotations.FirstOrDefault(d => d.InternalAnnotation == theAnno);
                    if (wpfAnno != null)
                    {
                        // LineAnnotation (vertical / horizontal lines) inherits PathAnnotation,
                        // not TextualAnnotation. A hard cast crashed on right-click of a
                        // line annotation; `as` + null-coalesce produces a blank label for
                        // non-textual annotations.
                        var annoText = (wpfAnno as Wpf.TextualAnnotation)?.Text ?? string.Empty;

                        if (leftClickBool)
                        {
                            PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, wpfAnno);
                            return;
                        }
                        else
                        {
                            var editAnnoItem = new MenuItem { Header = "Edit Annotation Text: " + annoText, Icon = CreateMenuIcon("EditTextbox") };
                            editAnnoItem.Click += (s, args) =>
                            {
                                PropertiesCalled?.Invoke(Plot, false, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, wpfAnno);

                                // Dummy canvas is used to render the item
                                var dummyCanvas = new Canvas();
                                var crc = new Wpf.CanvasRenderContext(dummyCanvas);
                                var size = new Size(Plot.ActualWidth, Plot.ActualHeight);
                                dummyCanvas.Measure(size);
                                dummyCanvas.Arrange(new Rect(size));
                                dummyCanvas.UpdateLayout();
                                wpfAnno.InternalAnnotation.Render(crc);
                                dummyCanvas.UpdateLayout();

                                foreach (var tbk in FindVisualChildren<TextBlock>(dummyCanvas))
                                {
                                    var annoType = wpfAnno.GetType();
                                    if (annoType == typeof(Wpf.ArrowAnnotation))
                                    {
                                        var anno = (Wpf.ArrowAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.ArrowAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.TextAnnotation))
                                    {
                                        var anno = (Wpf.TextAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.TextAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.RectangleAnnotation))
                                    {
                                        var anno = (Wpf.RectangleAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.RectangleAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.EllipseAnnotation))
                                    {
                                        var anno = (Wpf.EllipseAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.EllipseAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.PointAnnotation))
                                    {
                                        var anno = (Wpf.PointAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.PointAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.PolygonAnnotation))
                                    {
                                        var anno = (Wpf.PolygonAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.PolygonAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.PolylineAnnotation))
                                    {
                                        var anno = (Wpf.PolylineAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.PolylineAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                    else if (annoType == typeof(Wpf.LineAnnotation))
                                    {
                                        var anno = (Wpf.LineAnnotation)wpfAnno;
                                        if (tbk.Text == anno.Text)
                                        {
                                            CreateEditTBX(tbk, anno, Wpf.LineAnnotation.TextProperty, anno.TextRotation, dummyCanvas);
                                        }
                                    }
                                }
                            };

                            var formatAnnoItem = new MenuItem { Header = "Format Annotation: " + annoText, Icon = CreateMenuIcon("Format") };
                            formatAnnoItem.Click += (s, args) => PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.Annotations_Text, wpfAnno);

                            var deleteAnnoItem = new MenuItem { Header = "Delete Annotation: " + annoText, Icon = CreateMenuIcon("Delete") };
                            deleteAnnoItem.Click += (s, args) =>
                            {
                                Plot.Annotations.Remove(wpfAnno);
                                Plot.InvalidatePlot(false);
                            };

                            _contextMenu.Items.Add(editAnnoItem);
                            _contextMenu.Items.Add(formatAnnoItem);
                            _contextMenu.Items.Add(deleteAnnoItem);
                        }
                    }
                }

                // Only add one CM for annotations
                break;
            }

            // Add "Format Plot Area" only if no specific element was clicked and we're in the plot area
            if (_contextMenu.Items.Count == 0 && !leftClickBool && Plot.ActualModel.PlotArea.Contains(e.Position))
            {
                var formatPlotItem = new MenuItem { Header = "Format Plot Area", Icon = CreateMenuIcon("Format") };
                formatPlotItem.Click += (s, args) => PropertiesCalled?.Invoke(Plot, true, OxyPlotPropertiesControl.PropertyEXP.General_PlotArea, Plot.ActualModel.PlotArea);
                _contextMenu.Items.Add(formatPlotItem);
            }

            if (_contextMenu.Items.Count == 0)
            {
                return;
            }

            _contextMenu.Placement = PlacementMode.MousePoint;
            _contextMenu.HorizontalOffset = 0;
            _contextMenu.VerticalOffset = 0;
            _contextMenu.IsOpen = true;
        }

        #endregion

        #region CreateEditTBX

        /// <summary>
        /// Creates an in-place text box for editing text on the plot.
        /// Supports plot titles, axis titles, and all annotation types.
        /// </summary>
        /// <param name="existingTextblock">The existing TextBlock element being edited.</param>
        /// <param name="dependencyObj">The dependency object containing the text property.</param>
        /// <param name="dependencyProp">The dependency property to bind the text to.</param>
        /// <param name="angle">The rotation angle for the text box.</param>
        /// <param name="canvas">The canvas for positioning.</param>
        private void CreateEditTBX(TextBlock existingTextblock, DependencyObject dependencyObj, DependencyProperty dependencyProp, double angle, Canvas canvas)
        {
            IInputElement txtblckAsInputElem = existingTextblock as IInputElement;
            Color currentTextColor = Colors.Black; // This is for all annotations (hiding text while editing)
            Color currentStrokeColor = Colors.Black; // This is just for the text annotation, which is a box by default

            Point point;
            try
            {
                point = GetPosition((Visual)txtblckAsInputElem, canvas);
            }
            catch
            {
                return;
            }

            double left = point.X;
            double top = point.Y;
            double width = existingTextblock.ActualWidth;
            double height = existingTextblock.ActualHeight;
            double fontsize = existingTextblock.FontSize;
            FontFamily fontFamily = existingTextblock.FontFamily;
            FontWeight fontWeight = existingTextblock.FontWeight;
            Brush foreColor = existingTextblock.Foreground;

            // Create canvas for the textbox overlay
            var canvasOverlay = new Canvas { Name = "TextBoxCanvas" };
            canvasOverlay.Background = new SolidColorBrush(Colors.Transparent);
            var dockPanel = new DockPanel();
            var plotParent = (Grid)Plot.canvas.Parent;
            plotParent.Children.Add(canvasOverlay);

            // Set initial text box settings
            _textBox = new TextBox();
            _textBox.Background = Plot.Background;
            _textBox.TextAlignment = TextAlignment.Center;
            _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
            _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch;
            _textBox.Padding = new Thickness(-2);
            _textBox.FontSize = fontsize;
            _textBox.FontFamily = fontFamily;
            _textBox.FontWeight = fontWeight;
            _textBox.Foreground = foreColor;
            TextOptions.SetTextFormattingMode(_textBox, TextFormattingMode.Display);

            // Normalize all angles to 0-360
            if (angle < 0 || angle >= 360)
            {
                angle = angle % 360;
                if (angle < 0)
                {
                    angle += 360;
                }
            }

            // Determine what type of element was selected
            var depObjType = dependencyObj.GetType();

            if (depObjType == typeof(Wpf.Plot))
            {
                // Title or subtitle — hide the appropriate text and read its current color
                dockPanel.RenderTransform = new RotateTransform(angle, 0, 0);
                dockPanel.Width = Plot.ActualModel.PlotArea.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, Plot.ActualModel.PlotArea.Left);
                Canvas.SetTop(dockPanel, top);

                if (dependencyProp == Wpf.Plot.SubtitleProperty)
                {
                    currentTextColor = Plot.SubtitleColor;
                    Plot.SubtitleColor = Colors.Transparent;
                    _textBox.Text = Plot.Subtitle;
                }
                else
                {
                    currentTextColor = Plot.TitleColor;
                    Plot.TitleColor = Colors.Transparent;
                    _textBox.Text = Plot.Title;
                }
            }
            else if (depObjType == typeof(Wpf.LogarithmicAxis) || depObjType == typeof(Wpf.LinearAxis) ||
                     depObjType == typeof(Wpf.DateTimeAxis) || depObjType == typeof(Wpf.CategoryAxis) ||
                     depObjType == typeof(Wpf.GumbelProbabilityAxis) || depObjType == typeof(Wpf.LinearColorAxis) ||
                     depObjType == typeof(Wpf.AngleAxis) || depObjType == typeof(Wpf.NormalProbabilityAxis) ||
                     depObjType == typeof(Wpf.TimeSpanAxis) || depObjType == typeof(Wpf.MagnitudeAxis))
            {
                if (angle == 0)
                {
                    dockPanel.RenderTransform = new RotateTransform(angle, 0, 0);
                    dockPanel.Width = Plot.ActualModel.PlotArea.Width;
                    dockPanel.Height = height;
                    Canvas.SetLeft(dockPanel, Plot.ActualModel.PlotArea.Left);
                    Canvas.SetTop(dockPanel, top);
                }
                else if (angle == 270) // Vertical text - flowing up
                {
                    dockPanel.RenderTransform = new RotateTransform(angle, 0, 0);
                    dockPanel.Width = Plot.ActualModel.PlotArea.Height;
                    dockPanel.Height = height;
                    Canvas.SetTop(dockPanel, Plot.ActualModel.PlotArea.Bottom);
                    Canvas.SetLeft(dockPanel, left);
                }
                // else angle not handled
            }
            else if (depObjType == typeof(Wpf.ArrowAnnotation))
            {
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                var annotation = (Wpf.ArrowAnnotation)dependencyObj;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);

                // Hide rotated annotation
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;

                var startPoint = annotation.InternalAnnotation.Transform(annotation.StartPoint.X, annotation.StartPoint.Y);

                dockPanel.Width = existingTextblock.Width + 2;
                Canvas.SetTop(dockPanel, startPoint.Y - height);
                Canvas.SetLeft(dockPanel, startPoint.X);
            }
            else if (depObjType == typeof(Wpf.LineAnnotation))
            {
                var annotation = (Wpf.LineAnnotation)dependencyObj;
                _textBox.HorizontalAlignment = annotation.TextHorizontalAlignment;
                _textBox.HorizontalContentAlignment = annotation.TextHorizontalAlignment;
                _textBox.VerticalAlignment = annotation.TextVerticalAlignment;
                _textBox.VerticalContentAlignment = annotation.TextVerticalAlignment;
                _textBox.Padding = new Thickness(0);
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;

                switch (annotation.Type)
                {
                    case OxyPlot.Annotations.LineAnnotationType.Horizontal:
                        dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                        dockPanel.Width = existingTextblock.ActualWidth + 2;
                        _textBox.Width = dockPanel.Width;
                        Canvas.SetLeft(dockPanel, left);
                        Canvas.SetTop(dockPanel, top);
                        break;
                    case OxyPlot.Annotations.LineAnnotationType.Vertical:
                        dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                        dockPanel.Width = existingTextblock.ActualWidth + 2;
                        _textBox.Width = dockPanel.Width;
                        Canvas.SetLeft(dockPanel, left);
                        Canvas.SetTop(dockPanel, top);
                        break;
                    default: // Linear equation
                        dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                        dockPanel.Width = existingTextblock.ActualWidth + 2;
                        _textBox.Width = dockPanel.Width;
                        Canvas.SetLeft(dockPanel, left);
                        Canvas.SetTop(dockPanel, top);
                        break;
                }
            }
            else if (depObjType == typeof(Wpf.PolygonAnnotation))
            {
                var annotation = (Wpf.PolygonAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                // Just make a generic textbox near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }
            else if (depObjType == typeof(Wpf.PolylineAnnotation))
            {
                var annotation = (Wpf.PolylineAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                // Just make a generic textbox near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }
            else if (depObjType == typeof(Wpf.PointAnnotation))
            {
                var annotation = (Wpf.PointAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                // Just make a generic textbox near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }
            else if (depObjType == typeof(Wpf.RectangleAnnotation))
            {
                var annotation = (Wpf.RectangleAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                // Just make a generic text box near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }
            else if (depObjType == typeof(Wpf.TextAnnotation))
            {
                var annotation = (Wpf.TextAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                currentStrokeColor = annotation.Stroke;
                annotation.Stroke = Colors.Transparent;
                // Just make a generic text box near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }
            else if (depObjType == typeof(Wpf.EllipseAnnotation))
            {
                var annotation = (Wpf.EllipseAnnotation)dependencyObj;
                currentTextColor = annotation.TextColor;
                annotation.TextColor = Colors.Transparent;
                // Just make a generic text box near the object
                _textBox.Width = existingTextblock.Width;
                _textBox.TextAlignment = TextAlignment.Left;
                _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Left;
                _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
                _textBox.Padding = new Thickness(0);
                dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                dockPanel.Width = _textBox.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, left);
                Canvas.SetTop(dockPanel, top);
            }

            // Add the textbox to the dock panel and canvas
            dockPanel.Children.Add(_textBox);
            canvasOverlay.Children.Add(dockPanel);

            // Focus color from template is controlling here
            _textBox.BorderThickness = new Thickness(1);
            _textBox.Focus();

            // Set up binding
            var binding = new Binding { Mode = BindingMode.OneWay, Source = _textBox, Path = new PropertyPath("Text") };

            // Check if dependency object is an axis, in which case, need to get initial title
            if (depObjType == typeof(Wpf.LogarithmicAxis) || depObjType == typeof(Wpf.LinearAxis) ||
                depObjType == typeof(Wpf.DateTimeAxis) || depObjType == typeof(Wpf.CategoryAxis) ||
                depObjType == typeof(Wpf.GumbelProbabilityAxis) || depObjType == typeof(Wpf.LinearColorAxis) ||
                depObjType == typeof(Wpf.AngleAxis) || depObjType == typeof(Wpf.NormalProbabilityAxis) ||
                depObjType == typeof(Wpf.TimeSpanAxis) || depObjType == typeof(Wpf.MagnitudeAxis))
            {
                var axis = (Wpf.Axis)dependencyObj;
                string title = axis.Title;
                currentTextColor = axis.TitleColor;
                axis.TitleColor = Colors.Transparent;
                _textBox.Text = title; // Set up initial text
            }
            else
            {
                _textBox.Text = existingTextblock.Text; // Set up initial text
            }

            // Put the cursor at the end of the textbox
            if (_textBox.Text != null && _textBox.Text.Length > 0)
            {
                _textBox.SelectionStart = _textBox.Text.Length;
            }
            BindingOperations.SetBinding(dependencyObj, dependencyProp, binding);

            // If the plot size changes, remove the textbox overlay.
            // Store the handler so it can be unsubscribed in the cleanup to prevent leaking
            // references to the old canvasOverlay and TextBox.
            SizeChangedEventHandler sizeChangedHandler = null!;
            sizeChangedHandler = (s, args) =>
            {
                plotParent.Children.Remove(canvasOverlay);
                // This will also fire the lost focus event below
            };
            Plot.SizeChanged += sizeChangedHandler;

            // On key enter, remove the textbox overlay
            // Note: Do NOT clear the binding here - the binding must remain in place for the text to be saved
            _textBox.PreviewKeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    plotParent.Children.Remove(canvasOverlay);
                    // This will also fire the lost focus event below
                }
            };

            // On lost focus, remove the textbox overlay
            // Note: Do NOT clear the binding here - the binding must remain in place for the text to be saved
            _textBox.LostFocus += (s, args) =>
            {
                Plot.SizeChanged -= sizeChangedHandler;
                plotParent.Children.Remove(canvasOverlay);

                // Change the color of the text back from transparent for annotations
                if (depObjType == typeof(Wpf.RectangleAnnotation))
                {
                    var anno = (Wpf.RectangleAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.LineAnnotation))
                {
                    var anno = (Wpf.LineAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.PolygonAnnotation))
                {
                    var anno = (Wpf.PolygonAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.PolylineAnnotation))
                {
                    var anno = (Wpf.PolylineAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.EllipseAnnotation))
                {
                    var anno = (Wpf.EllipseAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.ArrowAnnotation))
                {
                    var anno = (Wpf.ArrowAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.PointAnnotation))
                {
                    var anno = (Wpf.PointAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.TextAnnotation))
                {
                    var anno = (Wpf.TextAnnotation)dependencyObj;
                    anno.TextColor = currentTextColor;
                    anno.Stroke = currentStrokeColor;
                }
                else if (depObjType == typeof(Wpf.LogarithmicAxis) || depObjType == typeof(Wpf.LinearAxis) ||
                         depObjType == typeof(Wpf.DateTimeAxis) || depObjType == typeof(Wpf.CategoryAxis) ||
                         depObjType == typeof(Wpf.GumbelProbabilityAxis) || depObjType == typeof(Wpf.LinearColorAxis) ||
                         depObjType == typeof(Wpf.AngleAxis) || depObjType == typeof(Wpf.NormalProbabilityAxis) ||
                         depObjType == typeof(Wpf.TimeSpanAxis) || depObjType == typeof(Wpf.MagnitudeAxis))
                {
                    var ax = (Wpf.Axis)dependencyObj;
                    ax.TitleColor = currentTextColor;
                }
                else if (depObjType == typeof(Wpf.Plot))
                {
                    if (dependencyProp == Wpf.Plot.SubtitleProperty)
                        Plot.SubtitleColor = currentTextColor;
                    else
                        Plot.TitleColor = currentTextColor;
                }

            };
        }

        /// <summary>
        /// Creates an in-place text box for editing titles and axis titles when using the DrawingVisual backend.
        /// Uses bounds from <see cref="Wpf.DrawingVisualRenderContext.TextHitResult"/> instead of a TextBlock.
        /// </summary>
        /// <param name="bounds">The bounding rectangle of the rendered text in screen coordinates.</param>
        /// <param name="fontSize">The font size of the rendered text.</param>
        /// <param name="dependencyObj">The dependency object containing the text property (Plot or Axis).</param>
        /// <param name="dependencyProp">The dependency property to bind the text to.</param>
        /// <param name="angle">The rotation angle for the text box.</param>
        /// <param name="canvas">The canvas for positioning.</param>
        private void CreateEditTBXFromBounds(OxyRect bounds, double fontSize, DependencyObject dependencyObj, DependencyProperty dependencyProp, double angle, Canvas canvas)
        {
            Color currentTextColor = Colors.Black;
            double left = bounds.Left;
            double top = bounds.Top;
            double width = bounds.Width;
            double height = bounds.Height;

            // Resolve font properties from the target dependency object
            string fontFamilyName = "Segoe UI";
            FontWeight fontWeight = System.Windows.FontWeights.Normal;
            Brush foreColor = System.Windows.Media.Brushes.Black;

            var depObjType = dependencyObj.GetType();
            if (depObjType == typeof(Wpf.Plot))
            {
                var plot = (Wpf.Plot)dependencyObj;
                if (dependencyProp == Wpf.Plot.TitleProperty)
                {
                    if (!string.IsNullOrEmpty(plot.TitleFont)) fontFamilyName = plot.TitleFont;
                    fontWeight = plot.TitleFontWeight;
                    currentTextColor = plot.TitleColor;
                    foreColor = new SolidColorBrush(currentTextColor == Wpf.MoreColors.Automatic ? Colors.Black : currentTextColor);
                }
                else // Subtitle
                {
                    if (!string.IsNullOrEmpty(plot.SubtitleFont)) fontFamilyName = plot.SubtitleFont;
                    fontWeight = plot.SubtitleFontWeight;
                    if (plot.SubtitleFontSize > 0) fontSize = plot.SubtitleFontSize;
                    currentTextColor = plot.SubtitleColor;
                    foreColor = new SolidColorBrush(currentTextColor == Wpf.MoreColors.Automatic ? Colors.Black : currentTextColor);
                }
            }
            else if (dependencyObj is Wpf.Axis ax)
            {
                if (!string.IsNullOrEmpty(ax.TitleFont)) fontFamilyName = ax.TitleFont;
                fontWeight = ax.TitleFontWeight;
                currentTextColor = ax.TitleColor;
                foreColor = new SolidColorBrush(currentTextColor == Wpf.MoreColors.Automatic ? Colors.Black : currentTextColor);
            }

            // Create canvas for the textbox overlay
            var canvasOverlay = new Canvas { Name = "TextBoxCanvas" };
            canvasOverlay.Background = new SolidColorBrush(Colors.Transparent);
            var dockPanel = new DockPanel();
            var plotParent = (Grid)Plot.canvas.Parent;
            plotParent.Children.Add(canvasOverlay);

            // Set initial text box settings
            _textBox = new TextBox();
            _textBox.Background = Plot.Background;
            _textBox.TextAlignment = TextAlignment.Center;
            _textBox.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            _textBox.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            _textBox.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;
            _textBox.VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch;
            _textBox.Padding = new Thickness(-2);
            _textBox.FontSize = fontSize;
            _textBox.FontFamily = new FontFamily(fontFamilyName);
            _textBox.FontWeight = fontWeight;
            _textBox.Foreground = foreColor;
            TextOptions.SetTextFormattingMode(_textBox, TextFormattingMode.Display);

            // Normalize angle
            if (angle < 0 || angle >= 360)
            {
                angle = angle % 360;
                if (angle < 0) angle += 360;
            }

            if (depObjType == typeof(Wpf.Plot))
            {
                // Title or subtitle — span the plot area width
                dockPanel.RenderTransform = new RotateTransform(angle, 0, 0);
                dockPanel.Width = Plot.ActualModel.PlotArea.Width;
                dockPanel.Height = height;
                Canvas.SetLeft(dockPanel, Plot.ActualModel.PlotArea.Left);
                Canvas.SetTop(dockPanel, top);

                string title = (string)dependencyObj.GetValue(dependencyProp);
                if (dependencyProp == Wpf.Plot.SubtitleProperty)
                    Plot.SubtitleColor = Colors.Transparent;
                else
                    Plot.TitleColor = Colors.Transparent;
                _textBox.Text = title;
            }
            else if (dependencyObj is Wpf.Axis axis)
            {
                if (angle == 0)
                {
                    dockPanel.RenderTransform = new RotateTransform(0, 0, 0);
                    dockPanel.Width = Plot.ActualModel.PlotArea.Width;
                    dockPanel.Height = height;
                    Canvas.SetLeft(dockPanel, Plot.ActualModel.PlotArea.Left);
                    Canvas.SetTop(dockPanel, top);
                }
                else if (angle == 270)
                {
                    // For -90° rotated text, the AABB bounds are swapped:
                    // bounds.Width = font line height, bounds.Height = string width.
                    // dockPanel.Height becomes visual width after 270° rotation,
                    // so use bounds.Width (font line height) to match CreateEditTBX behavior.
                    dockPanel.RenderTransform = new RotateTransform(270, 0, 0);
                    dockPanel.Width = Plot.ActualModel.PlotArea.Height;
                    dockPanel.Height = bounds.Width;
                    Canvas.SetTop(dockPanel, Plot.ActualModel.PlotArea.Bottom);
                    Canvas.SetLeft(dockPanel, left);
                }

                currentTextColor = axis.TitleColor;
                axis.TitleColor = Colors.Transparent;
                _textBox.Text = axis.Title;
            }

            // Add the textbox to the dock panel and canvas
            dockPanel.Children.Add(_textBox);
            canvasOverlay.Children.Add(dockPanel);
            _textBox.BorderThickness = new Thickness(1);
            _textBox.Focus();

            // Set up binding
            var binding = new Binding { Mode = BindingMode.OneWay, Source = _textBox, Path = new PropertyPath("Text") };

            // Put the cursor at the end of the textbox
            if (_textBox.Text != null && _textBox.Text.Length > 0)
            {
                _textBox.SelectionStart = _textBox.Text.Length;
            }
            BindingOperations.SetBinding(dependencyObj, dependencyProp, binding);

            // If the plot size changes, remove the textbox overlay.
            // Store the handler so it can be unsubscribed in the cleanup to prevent leaking
            // references to the old canvasOverlay and TextBox.
            SizeChangedEventHandler sizeChangedHandler2 = null!;
            sizeChangedHandler2 = (s, args) =>
            {
                plotParent.Children.Remove(canvasOverlay);
            };
            Plot.SizeChanged += sizeChangedHandler2;

            // On key enter, remove the textbox overlay
            _textBox.PreviewKeyDown += (s, args) =>
            {
                if (args.Key == Key.Enter)
                {
                    plotParent.Children.Remove(canvasOverlay);
                }
            };

            // On lost focus, remove the textbox overlay and restore text color
            _textBox.LostFocus += (s, args) =>
            {
                Plot.SizeChanged -= sizeChangedHandler2;
                plotParent.Children.Remove(canvasOverlay);

                if (depObjType == typeof(Wpf.Plot))
                {
                    if (dependencyProp == Wpf.Plot.SubtitleProperty)
                        Plot.SubtitleColor = currentTextColor;
                    else
                        Plot.TitleColor = currentTextColor;
                }
                else if (dependencyObj is Wpf.Axis axRestore)
                {
                    axRestore.TitleColor = currentTextColor;
                }
            };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Support method for finding visual children.
        /// </summary>
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        /// <summary>
        /// Converts a screen point to a data point using the plot's default axes.
        /// </summary>
        /// <param name="pt">The screen point to convert.</param>
        /// <returns>The corresponding data point, or <see cref="DataPoint.Undefined"/> if the plot has no default axes.</returns>
        private DataPoint ConvertScreenPointToDataPoint(ScreenPoint pt)
        {
            var xAxis = Plot?.ActualModel?.DefaultXAxis;
            var yAxis = Plot?.ActualModel?.DefaultYAxis;
            if (xAxis == null || yAxis == null)
            {
                return DataPoint.Undefined;
            }

            var dataPoint = xAxis.InverseTransform(pt.X, pt.Y, yAxis);
            return IsValidDataPoint(xAxis, yAxis, dataPoint)
                ? dataPoint
                : DataPoint.Undefined;
        }

        /// <summary>
        /// Resolves the x and y axes used by an annotation.
        /// </summary>
        /// <param name="annotation">The annotation whose axes are needed.</param>
        /// <param name="xAxis">The resolved x-axis.</param>
        /// <param name="yAxis">The resolved y-axis.</param>
        /// <returns><c>true</c> when both axes are available; otherwise, <c>false</c>.</returns>
        private bool TryGetAnnotationAxes(Wpf.Annotation annotation, out OxyPlot.Axes.Axis xAxis, out OxyPlot.Axes.Axis yAxis)
        {
            var resolvedXAxis = annotation.InternalAnnotation.XAxis ?? Plot?.ActualModel?.DefaultXAxis;
            var resolvedYAxis = annotation.InternalAnnotation.YAxis ?? Plot?.ActualModel?.DefaultYAxis;
            if (resolvedXAxis == null || resolvedYAxis == null)
            {
                xAxis = null!;
                yAxis = null!;
                return false;
            }

            xAxis = resolvedXAxis;
            yAxis = resolvedYAxis;
            return true;
        }

        /// <summary>
        /// Converts a screen point to an annotation data point.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="screenPoint">The screen point to convert.</param>
        /// <param name="dataPoint">The converted data point.</param>
        /// <returns><c>true</c> when conversion succeeds and the point is valid.</returns>
        private bool TryScreenPointToDataPoint(Wpf.Annotation annotation, ScreenPoint screenPoint, out DataPoint dataPoint)
        {
            dataPoint = DataPoint.Undefined;
            if (!IsFinite(screenPoint.X) || !IsFinite(screenPoint.Y))
            {
                return false;
            }

            if (!TryGetAnnotationAxes(annotation, out var xAxis, out var yAxis))
            {
                return false;
            }

            var convertedPoint = xAxis.InverseTransform(screenPoint.X, screenPoint.Y, yAxis);
            if (!IsValidDataPoint(xAxis, yAxis, convertedPoint))
            {
                return false;
            }

            dataPoint = convertedPoint;
            return true;
        }

        /// <summary>
        /// Converts an annotation data point to a screen point.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="dataPoint">The data point to convert.</param>
        /// <param name="screenPoint">The converted screen point.</param>
        /// <returns><c>true</c> when conversion succeeds and the point is finite.</returns>
        private bool TryDataPointToScreenPoint(Wpf.Annotation annotation, DataPoint dataPoint, out ScreenPoint screenPoint)
        {
            screenPoint = ScreenPoint.Undefined;
            if (!TryGetAnnotationAxes(annotation, out var xAxis, out var yAxis)
                || !IsValidDataPoint(xAxis, yAxis, dataPoint))
            {
                return false;
            }

            var convertedPoint = xAxis.Transform(dataPoint.X, dataPoint.Y, yAxis);
            if (!IsFinite(convertedPoint.X) || !IsFinite(convertedPoint.Y))
            {
                return false;
            }

            screenPoint = convertedPoint;
            return true;
        }

        /// <summary>
        /// Offsets an annotation data point by a screen-space delta.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="dataPoint">The source data point.</param>
        /// <param name="dx">The horizontal screen offset.</param>
        /// <param name="dy">The vertical screen offset.</param>
        /// <param name="offsetDataPoint">The offset data point.</param>
        /// <returns><c>true</c> when the offset point remains valid.</returns>
        private bool TryOffsetDataPoint(Wpf.Annotation annotation, DataPoint dataPoint, double dx, double dy, out DataPoint offsetDataPoint)
        {
            offsetDataPoint = DataPoint.Undefined;
            if (!TryDataPointToScreenPoint(annotation, dataPoint, out var screenPoint))
            {
                return false;
            }

            return TryScreenPointToDataPoint(
                annotation,
                new ScreenPoint(screenPoint.X + dx, screenPoint.Y + dy),
                out offsetDataPoint);
        }

        /// <summary>
        /// Offsets multiple annotation data points by a screen-space delta.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="dataPoints">The source data points.</param>
        /// <param name="dx">The horizontal screen offset.</param>
        /// <param name="dy">The vertical screen offset.</param>
        /// <param name="offsetDataPoints">The offset data points.</param>
        /// <returns><c>true</c> when every point can be offset.</returns>
        private bool TryOffsetDataPoints(Wpf.Annotation annotation, IList<DataPoint> dataPoints, double dx, double dy, out List<DataPoint> offsetDataPoints)
        {
            offsetDataPoints = new List<DataPoint>(dataPoints.Count);
            foreach (var dataPoint in dataPoints)
            {
                if (!TryOffsetDataPoint(annotation, dataPoint, dx, dy, out var offsetDataPoint))
                {
                    offsetDataPoints.Clear();
                    return false;
                }

                offsetDataPoints.Add(offsetDataPoint);
            }

            return true;
        }

        /// <summary>
        /// Creates minimum data bounds around a screen-space center point.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="center">The screen-space center for the bounds.</param>
        /// <param name="minimumPoint">The lower data bound.</param>
        /// <param name="maximumPoint">The upper data bound.</param>
        /// <returns><c>true</c> when both bounds are valid data points.</returns>
        private bool TryCreateMinimumScreenBounds(Wpf.Annotation annotation, ScreenPoint center, out DataPoint minimumPoint, out DataPoint maximumPoint)
        {
            minimumPoint = DataPoint.Undefined;
            maximumPoint = DataPoint.Undefined;

            var plotArea = Plot?.ActualModel?.PlotArea;
            if (plotArea == null)
            {
                return false;
            }

            double plotWidth = Math.Abs(plotArea.Value.Right - plotArea.Value.Left);
            double plotHeight = Math.Abs(plotArea.Value.Bottom - plotArea.Value.Top);
            if (plotWidth <= 0 || plotHeight <= 0)
            {
                return false;
            }

            double boxWidth = Math.Min(MinAnnotationSize, plotWidth);
            double boxHeight = Math.Min(MinAnnotationSize, plotHeight);
            double centerX = Clamp(center.X, plotArea.Value.Left, plotArea.Value.Right);
            double centerY = Clamp(center.Y, plotArea.Value.Top, plotArea.Value.Bottom);

            double left = Clamp(centerX - boxWidth / 2, plotArea.Value.Left, plotArea.Value.Right - boxWidth);
            double top = Clamp(centerY - boxHeight / 2, plotArea.Value.Top, plotArea.Value.Bottom - boxHeight);
            double right = left + boxWidth;
            double bottom = top + boxHeight;

            if (!TryScreenPointToDataPoint(annotation, new ScreenPoint(left, bottom), out var lowerLeftPoint)
                || !TryScreenPointToDataPoint(annotation, new ScreenPoint(right, top), out var upperRightPoint))
            {
                return false;
            }

            minimumPoint = new DataPoint(
                Math.Min(lowerLeftPoint.X, upperRightPoint.X),
                Math.Min(lowerLeftPoint.Y, upperRightPoint.Y));
            maximumPoint = new DataPoint(
                Math.Max(lowerLeftPoint.X, upperRightPoint.X),
                Math.Max(lowerLeftPoint.Y, upperRightPoint.Y));

            return IsValidDataPoint(annotation, minimumPoint) && IsValidDataPoint(annotation, maximumPoint);
        }

        /// <summary>
        /// Determines whether the mouse is within a screen-space tolerance of a data point.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="dataPoint">The data point to test.</param>
        /// <param name="mousePosition">The current mouse position.</param>
        /// <param name="tolerance">The screen-space tolerance.</param>
        /// <returns><c>true</c> when the mouse is near the point.</returns>
        private bool IsMouseNearDataPoint(Wpf.Annotation annotation, DataPoint dataPoint, ScreenPoint mousePosition, double tolerance)
        {
            return TryDataPointToScreenPoint(annotation, dataPoint, out var screenPoint)
                && (screenPoint - mousePosition).Length < tolerance;
        }

        /// <summary>
        /// Determines whether a data point is valid for an annotation's axes.
        /// </summary>
        /// <param name="annotation">The annotation that supplies axes.</param>
        /// <param name="dataPoint">The data point to test.</param>
        /// <returns><c>true</c> when the point is defined, finite, and valid for both axes.</returns>
        private bool IsValidDataPoint(Wpf.Annotation annotation, DataPoint dataPoint)
        {
            return TryGetAnnotationAxes(annotation, out var xAxis, out var yAxis)
                && IsValidDataPoint(xAxis, yAxis, dataPoint);
        }

        /// <summary>
        /// Determines whether a data point is valid for the specified axes.
        /// </summary>
        /// <param name="xAxis">The x-axis.</param>
        /// <param name="yAxis">The y-axis.</param>
        /// <param name="dataPoint">The data point to test.</param>
        /// <returns><c>true</c> when the point is defined, finite, and valid for both axes.</returns>
        private static bool IsValidDataPoint(OxyPlot.Axes.Axis xAxis, OxyPlot.Axes.Axis yAxis, DataPoint dataPoint)
        {
            return dataPoint.IsDefined()
                && IsFinite(dataPoint.X)
                && IsFinite(dataPoint.Y)
                && xAxis.IsValidValue(dataPoint.X)
                && yAxis.IsValidValue(dataPoint.Y);
        }

        /// <summary>
        /// Determines whether a double is neither NaN nor infinite.
        /// </summary>
        /// <param name="value">The value to test.</param>
        /// <returns><c>true</c> when the value is finite.</returns>
        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        /// <summary>
        /// Clamps a value to the supplied inclusive range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="minimum">The minimum allowed value.</param>
        /// <param name="maximum">The maximum allowed value.</param>
        /// <returns>The clamped value.</returns>
        private static double Clamp(double value, double minimum, double maximum)
        {
            if (maximum < minimum)
            {
                return minimum;
            }

            return Math.Max(minimum, Math.Min(maximum, value));
        }

        /// <summary>
        /// Gets the position of an element on the plot.
        /// </summary>
        /// <param name="element">The visual element.</param>
        /// <param name="canvas">The canvas to get position relative to.</param>
        /// <returns>The position of the element.</returns>
        private Point GetPosition(Visual element, Canvas canvas)
        {
            var positionTransform = element.TransformToAncestor(canvas);
            var areaPosition = positionTransform.Transform(new Point(0, 0));
            return areaPosition;
        }

        /// <summary>
        /// Creates a menu icon from vector resources.
        /// </summary>
        /// <param name="iconName">The name of the icon (e.g., "Format", "Delete", "EditTextbox").</param>
        /// <returns>A vector icon element, or null if not found.</returns>
        /// <remarks>
        /// Uses TryFindResource to search the control's local resources first (for Format, EditTextbox),
        /// then application resources (for Delete which is in GenericControls).
        /// </remarks>
        private object? CreateMenuIcon(string iconName)
        {
            return iconName switch
            {
                "Delete" => TryFindResource("DeleteImage") is ImageSource deleteImage
                    ? new Image { Source = deleteImage, Width = 16, Height = 16 }
                    : null,
                "Format" => TryFindResource("FormatIcon"),
                "EditTextbox" => TryFindResource("EditTextboxIcon"),
                _ => null
            };
        }

        #endregion

        #region Export Series Data

        /// <summary>
        /// Export series data to file.
        /// </summary>
        private static List<DataTable> BuildExportDataTables(Wpf.Plot plot)
        {
            var tableList = new List<DataTable>();
            int tableCount = 0;
            string[] badCharacters = { ":", "\\", "/", "?", "*", "[", "]" };

            foreach (Wpf.Series series in plot.Series)
            {
                series.CreateModel();

                var dataTable = new DataTable("Series");
                string seriesName = "";
                tableCount++;

                if (series is Wpf.LineSeries && series is not Wpf.AreaSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "LineSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_x", typeof(string));
                    dataTable.Columns.Add(seriesName + "_y", typeof(string));

                    var oxySeries = (OxyPlot.Series.LineSeries)series.InternalSeries;
                    var wpfSeries = (Wpf.DataPointSeries)series;

                    foreach (var seriesValue in GetDataPointExportValues(wpfSeries, oxySeries))
                    {
                        dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesValue.X, seriesValue.Y);
                    }
                }
                else if (series is Wpf.ScatterPointSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "ScatterSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_x", typeof(string));
                    dataTable.Columns.Add(seriesName + "_y", typeof(string));

                    var oxySeries = (OxyPlot.Series.ScatterSeries)series.InternalSeries;
                    var wpfSeries = (Wpf.ScatterPointSeries)series;

                    foreach (var seriesValue in GetScatterPointExportValues(wpfSeries, oxySeries))
                    {
                        dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesValue.X, seriesValue.Y);
                    }
                }
                else if (series is Wpf.AreaSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "AreaSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_x", typeof(string));
                    dataTable.Columns.Add(seriesName + "_y", typeof(string));
                    dataTable.Columns.Add(seriesName + "_x2", typeof(string));
                    dataTable.Columns.Add(seriesName + "_y2", typeof(string));

                    var oxySeries = (OxyPlot.Series.AreaSeries)series.InternalSeries;
                    var wpfSeries = (Wpf.AreaSeries)series;

                    foreach (var seriesValue in GetAreaExportValues(wpfSeries, oxySeries))
                    {
                        dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesValue.X, seriesValue.Y, seriesValue.X2, seriesValue.Y2);
                    }
                }
                else if (series is Wpf.BoxPlotSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "BoxPlotSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_position", typeof(string));
                    dataTable.Columns.Add(seriesName + "_lowerWhisker", typeof(string));
                    dataTable.Columns.Add(seriesName + "_boxMinimum", typeof(string));
                    dataTable.Columns.Add(seriesName + "_median", typeof(string));
                    dataTable.Columns.Add(seriesName + "_boxMaximum", typeof(string));
                    dataTable.Columns.Add(seriesName + "_upperWhisker", typeof(string));
                    dataTable.Columns.Add(seriesName + "_label", typeof(string));

                    var oxySeries = (OxyPlot.Series.BoxPlotSeries)series.InternalSeries;

                    if (oxySeries.ItemsSource != null)
                    {
                        var datalist = oxySeries.ItemsSource as IEnumerable<OxyPlot.Series.BoxPlotItem>;
                        if (datalist != null)
                        {
                            foreach (var bpi in datalist)
                            {
                                var r = dataTable.Rows.Add(dataTable.Rows.Count + 1, bpi.Position, bpi.LowerWhisker, bpi.BoxMinimum, bpi.Median, bpi.BoxMaximum, bpi.UpperWhisker);

                                // Check if a X Axis Label is specified
                                if (oxySeries.XAxis != null)
                                {
                                    if (oxySeries.XAxis is OxyPlot.Axes.CategoryAxis)
                                    {
                                        if (((OxyPlot.Axes.CategoryAxis)oxySeries.XAxis).LabelField != null)
                                        {
                                            r[seriesName + "_label"] = ((OxyPlot.Axes.CategoryAxis)oxySeries.XAxis).LabelField;
                                        }
                                    }
                                }

                                int j = 1;
                                foreach (var outlier in bpi.Outliers)
                                {
                                    if (!dataTable.Columns.Contains("outlier" + j))
                                    {
                                        dataTable.Columns.Add("outlier" + j, typeof(string));
                                    }
                                    r[dataTable.Columns.IndexOf("outlier" + j)] = outlier;
                                    j++;
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < oxySeries.Items.Count; i++)
                        {
                            var r = dataTable.Rows.Add(dataTable.Rows.Count + 1, oxySeries.Items[i].Position, oxySeries.Items[i].LowerWhisker, oxySeries.Items[i].BoxMinimum, oxySeries.Items[i].Median, oxySeries.Items[i].BoxMaximum, oxySeries.Items[i].UpperWhisker);

                            // Check if a X Axis Label is specified
                            if (oxySeries.XAxis != null)
                            {
                                if (oxySeries.XAxis is OxyPlot.Axes.CategoryAxis)
                                {
                                    if (((OxyPlot.Axes.CategoryAxis)oxySeries.XAxis).LabelField != null)
                                    {
                                        r[seriesName + "_label"] = ((OxyPlot.Axes.CategoryAxis)oxySeries.XAxis).LabelField;
                                    }
                                }
                            }

                            int j = 1;
                            foreach (var outlier in oxySeries.Items[i].Outliers)
                            {
                                if (!dataTable.Columns.Contains("outlier" + j))
                                {
                                    dataTable.Columns.Add("outlier" + j, typeof(string));
                                }
                                r[dataTable.Columns.IndexOf("outlier" + j)] = outlier;
                                j++;
                            }
                        }
                    }
                }
                else if (series is Wpf.BarSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "BarSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_categoryIndex", typeof(string));
                    dataTable.Columns.Add(seriesName + "_color", typeof(string));
                    dataTable.Columns.Add(seriesName + "_value", typeof(string));

                    var oxySeries = (OxyPlot.Series.BarSeries)series.InternalSeries;

                    if (oxySeries.ItemsSource != null)
                    {
                        var datalist = oxySeries.ItemsSource as IEnumerable<OxyPlot.Series.BarItem>;
                        if (datalist != null)
                        {
                            foreach (var seriesItem in datalist.ToList())
                            {
                                dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesItem.CategoryIndex, seriesItem.Color.GetColorName(), seriesItem.Value);
                            }
                        }
                        else
                        {
                            int c = 0;
                            foreach (var obj in oxySeries.ItemsSource.Cast<object>())
                            {
                                string colorVal = GetPropertyValueOrEmpty(obj, oxySeries.ColorField);
                                string valueVal = GetPropertyValueOrEmpty(obj, oxySeries.ValueField);

                                dataTable.Rows.Add(dataTable.Rows.Count + 1, c, colorVal, valueVal);
                                c++;
                            }
                        }
                    }
                    else
                    {
                        foreach (var seriesItem in oxySeries.Items)
                        {
                            dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesItem.CategoryIndex, seriesItem.Color.GetColorName(), seriesItem.Value);
                        }
                    }
                }
                else if (series is Wpf.ColumnSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "ColumnSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_categoryIndex", typeof(string));
                    dataTable.Columns.Add(seriesName + "_color", typeof(string));
                    dataTable.Columns.Add(seriesName + "_value", typeof(string));

                    var oxySeries = (OxyPlot.Series.ColumnSeries)series.InternalSeries;

                    if (oxySeries.ItemsSource != null)
                    {
                        var datalist = oxySeries.ItemsSource as IEnumerable<OxyPlot.Series.ColumnItem>;
                        if (datalist != null)
                        {
                            foreach (var seriesItem in datalist.ToList())
                            {
                                dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesItem.CategoryIndex, seriesItem.Color.GetColorName(), seriesItem.Value);
                            }
                        }
                        else
                        {
                            int c = 0;
                            foreach (var obj in oxySeries.ItemsSource.Cast<object>())
                            {
                                string colorVal = GetPropertyValueOrEmpty(obj, oxySeries.ColorField);
                                string valueVal = GetPropertyValueOrEmpty(obj, oxySeries.ValueField);

                                dataTable.Rows.Add(dataTable.Rows.Count + 1, c, colorVal, valueVal);
                                c++;
                            }
                        }
                    }
                    else
                    {
                        foreach (var seriesItem in oxySeries.Items)
                        {
                            dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesItem.CategoryIndex, seriesItem.Color.GetColorName(), seriesItem.Value);
                        }
                    }
                }
                else if (series is Wpf.HistogramSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "HistogramSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_rangeStart", typeof(string));
                    dataTable.Columns.Add(seriesName + "_rangeEnd", typeof(string));
                    dataTable.Columns.Add(seriesName + "_area", typeof(string));

                    var oxySeries = (OxyPlot.Series.HistogramSeries)series.InternalSeries;

                    if (oxySeries.ItemsSource != null)
                    {
                        var datalist = oxySeries.ItemsSource as IEnumerable<OxyPlot.Series.HistogramItem>;
                        if (datalist != null)
                        {
                            foreach (var seriesValue in datalist.ToList())
                            {
                                dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesValue.RangeStart, seriesValue.RangeEnd, seriesValue.Area);
                            }
                        }
                    }
                    else
                    {
                        foreach (var seriesItem in oxySeries.Items)
                        {
                            dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesItem.RangeStart, seriesItem.RangeEnd, seriesItem.Area);
                        }
                    }
                }
                else if (series is Wpf.HeatMapSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "HeatMapSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add("xy", typeof(string));

                    var oxySeries = (OxyPlot.Series.HeatMapSeries)series.InternalSeries;

                    if (oxySeries.ItemsSource != null)
                    {
                        var datalist = oxySeries.ItemsSource as IEnumerable<double[,]>;
                        if (datalist != null)
                        {
                            // Add columns
                            double x0 = oxySeries.X0;
                            double x1 = oxySeries.X1;
                            int xN = datalist.ToArray().GetLength(0) - 1;
                            double xDelta = (x1 - x0) / xN;
                            dataTable.Columns.Add(x0.ToString(), typeof(string));
                            for (int i = 1; i < datalist.ToArray().GetLength(0); i++)
                            {
                                x0 += xDelta;
                                dataTable.Columns.Add(x0.ToString(), typeof(string));
                            }

                            // Add rows
                            double y0 = oxySeries.Y0;
                            double y1 = oxySeries.Y1;
                            int yN = datalist.ToArray().GetLength(1) - 1;
                            double yDelta = (y1 - y0) / yN;
                            dataTable.Rows.Add();
                            dataTable.Rows[0][0] = 1;
                            dataTable.Rows[0][1] = y0;

                            for (int j = 1; j < datalist.ToArray().GetLength(1); j++)
                            {
                                dataTable.Rows.Add();
                                y0 += yDelta;
                                dataTable.Rows[j][0] = j + 1;
                                dataTable.Rows[j][1] = y0;
                            }

                            // Fill in matrix
                            for (int x = 0; x < datalist.ToArray().GetLength(0); x++)
                            {
                                double[,] xy = datalist.ToArray().ElementAt(x);
                                for (int y = 0; y < datalist.ToArray().GetLength(1); y++)
                                {
                                    dataTable.Rows[y][x + 2] = xy[x, y];
                                }
                            }
                        }
                    }
                    else
                    {
                        // Add columns
                        double x0 = oxySeries.X0;
                        double x1 = oxySeries.X1;
                        int xN = oxySeries.Data.GetLength(0) - 1;
                        double xDelta = (x1 - x0) / xN;
                        dataTable.Columns.Add(x0.ToString(), typeof(string));
                        for (int i = 1; i < oxySeries.Data.GetLength(0); i++)
                        {
                            x0 += xDelta;
                            dataTable.Columns.Add(x0.ToString(), typeof(string));
                        }

                        // Add rows
                        double y0 = oxySeries.Y0;
                        double y1 = oxySeries.Y1;
                        int yN = oxySeries.Data.GetLength(1) - 1;
                        double yDelta = (y1 - y0) / yN;
                        dataTable.Rows.Add();
                        dataTable.Rows[0][0] = 1;
                        dataTable.Rows[0][1] = y0;

                        for (int j = 1; j < oxySeries.Data.GetLength(1); j++)
                        {
                            dataTable.Rows.Add();
                            y0 += yDelta;
                            dataTable.Rows[j][0] = j + 1;
                            dataTable.Rows[j][1] = y0;
                        }

                        // Fill in matrix
                        for (int x = 0; x < oxySeries.Data.GetLength(0); x++)
                        {
                            for (int y = 0; y < oxySeries.Data.GetLength(1); y++)
                            {
                                dataTable.Rows[y][x + 2] = oxySeries.Data[x, y];
                            }
                        }
                    }
                }
                else if (series is Wpf.ScatterErrorSeries)
                {
                    seriesName = !string.IsNullOrEmpty(series.Title) ? series.Title : "ScatterErrorSeries_" + tableCount;
                    foreach (var badChar in badCharacters)
                    {
                        seriesName = seriesName.Replace(badChar, "_");
                    }

                    dataTable.TableName = seriesName;
                    dataTable.Columns.Add("id", typeof(int));
                    dataTable.Columns.Add(seriesName + "_xLower", typeof(string));
                    dataTable.Columns.Add(seriesName + "_x", typeof(string));
                    dataTable.Columns.Add(seriesName + "_xUpper", typeof(string));
                    dataTable.Columns.Add(seriesName + "_yLower", typeof(string));
                    dataTable.Columns.Add(seriesName + "_y", typeof(string));
                    dataTable.Columns.Add(seriesName + "_yUpper", typeof(string));

                    var oxySeries = (OxyPlot.Series.ScatterErrorSeries)series.InternalSeries;
                    var wpfSeries = (Wpf.ScatterErrorSeries)series;

                    foreach (var seriesValue in GetScatterErrorExportValues(wpfSeries, oxySeries))
                    {
                        dataTable.Rows.Add(dataTable.Rows.Count + 1, seriesValue.XLower, seriesValue.X, seriesValue.XUpper, seriesValue.YLower, seriesValue.Y, seriesValue.YUpper);
                    }
                }

                // Skip contour series for now
                if (series is Wpf.ContourSeries) continue;
                if (dataTable.Columns.Count == 0) continue;

                AddCategoryAxisLabels(series.InternalSeries as OxyPlot.Series.XYAxisSeries, dataTable);

                tableList.Add(dataTable);
            }

            return tableList;
        }

        /// <summary>
        /// Export series data to file.
        /// </summary>
        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null)
            {
                return;
            }

            ExportDataTablesToFile(BuildExportDataTables(Plot));
        }

        /// <summary>
        /// Prompts for an export path and writes the supplied data tables.
        /// </summary>
        /// <param name="tableList">The data tables to export.</param>
        private void ExportDataTablesToFile(IList<DataTable> tableList)
        {
            // Show save dialog
            string filters = "comma delimited(*.csv) |*.csv|Excel(*.xlsx) |*.xlsx|Sqlite(*.sqlite) |*.sqlite";
            try
            {
                var saveFileBrowser = new Microsoft.Win32.SaveFileDialog { Filter = filters, FilterIndex = 1 };
                if (saveFileBrowser.ShowDialog() == true)
                {
                    string extension = System.IO.Path.GetExtension(saveFileBrowser.FileName);
                    switch (extension)
                    {
                        case ".csv":
                            // Combine all tables because CSVs only have one sheet/table
                            int uniqueCount = 1;
                            var colNames = new List<string>();
                            foreach (var theDT in tableList)
                            {
                                for (int i = 0; i < theDT.Columns.Count; i++)
                                {
                                    if (theDT.Columns[i].ColumnName == "id")
                                    {
                                        continue;
                                    }

                                    if (!colNames.Contains(theDT.Columns[i].ColumnName))
                                    {
                                        colNames.Add(theDT.Columns[i].ColumnName);
                                    }
                                    else
                                    {
                                        while (colNames.Contains(theDT.Columns[i].ColumnName))
                                        {
                                            theDT.Columns[theDT.Columns[i].ColumnName]!.ColumnName = theDT.Columns[i].ColumnName + "_" + uniqueCount;
                                        }
                                        colNames.Add(theDT.Columns[i].ColumnName);
                                    }
                                }
                            }

                            var totalDT = new DataTable("Exported_Data");
                            totalDT = MergeAll(tableList, "id");

                            var csvDataView = new DatabaseManager.InMemoryReader(totalDT).GetTableManager(totalDT.TableName);
                            csvDataView.ExportToCsv(saveFileBrowser.FileName);
                            break;

                        case ".xlsx":
                            // Save each DT to the file (as new sheet)
                            foreach (var dt in tableList)
                            {
                                var xlsxDataView = new DatabaseManager.InMemoryReader(dt).GetTableManager(dt.TableName);
                                if (xlsxDataView == null) continue;
                                xlsxDataView.ExportToXlsx(saveFileBrowser.FileName);
                            }
                            break;

                        case ".sqlite":
                            // Save each DT to the file (as new table)
                            foreach (var dt in tableList)
                            {
                                var sqliteDataView = new DatabaseManager.InMemoryReader(dt).GetTableManager(dt.TableName);
                                if (sqliteDataView == null) continue;
                                sqliteDataView.ExportToSqlite(saveFileBrowser.FileName, sqliteDataView.TableName);
                            }
                            break;

                        default:
                            throw new Exception("selected file format extension '" + System.IO.Path.GetExtension(saveFileBrowser.FileName) + "' is not supported for export.");
                    }
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message);
            }
        }

        private static IEnumerable<(object X, object Y)> GetDataPointExportValues(Wpf.DataPointSeries wpfSeries, OxyPlot.Series.DataPointSeries oxySeries)
        {
            if (oxySeries.ItemsSource == null)
            {
                foreach (var point in oxySeries.Points)
                {
                    yield return (point.X, point.Y);
                }

                yield break;
            }

            foreach (var item in oxySeries.ItemsSource.Cast<object?>())
            {
                yield return GetDataPointExportValue(wpfSeries, item);
            }
        }

        /// <summary>
        /// Extracts x and y export values for a data-point series item.
        /// </summary>
        /// <param name="wpfSeries">The WPF data-point series.</param>
        /// <param name="item">The item to export.</param>
        /// <returns>The exported x and y values.</returns>
        private static (object X, object Y) GetDataPointExportValue(Wpf.DataPointSeries wpfSeries, object? item)
        {
            if (item != null)
            {
                if (wpfSeries.Mapping != null)
                {
                    var point = wpfSeries.Mapping(item);
                    return (point.X, point.Y);
                }

                if (item is DataPoint dataPoint)
                {
                    return (dataPoint.X, dataPoint.Y);
                }

                if (item is IDataPointProvider pointProvider)
                {
                    var point = pointProvider.GetDataPoint();
                    return (point.X, point.Y);
                }
            }

            return (
                GetExportPropertyValue(item, wpfSeries.DataFieldX, "X"),
                GetExportPropertyValue(item, wpfSeries.DataFieldY, "Y"));
        }

        private static IEnumerable<(object X, object Y, object X2, object Y2)> GetAreaExportValues(Wpf.AreaSeries wpfSeries, OxyPlot.Series.AreaSeries oxySeries)
        {
            if (oxySeries.ItemsSource == null)
            {
                for (int i = 0; i < oxySeries.Points.Count; i++)
                {
                    if (i < oxySeries.Points2.Count)
                    {
                        yield return (oxySeries.Points[i].X, oxySeries.Points[i].Y, oxySeries.Points2[i].X, oxySeries.Points2[i].Y);
                    }
                    else
                    {
                        yield return (oxySeries.Points[i].X, oxySeries.Points[i].Y, "", "");
                    }
                }

                yield break;
            }

            foreach (var item in oxySeries.ItemsSource.Cast<object?>())
            {
                var firstPoint = GetDataPointExportValue(wpfSeries, item);
                yield return (
                    firstPoint.X,
                    firstPoint.Y,
                    GetExportPropertyValue(item, wpfSeries.DataFieldX2),
                    GetExportPropertyValue(item, wpfSeries.DataFieldY2));
            }
        }

        private static IEnumerable<(object X, object Y)> GetScatterPointExportValues(Wpf.ScatterPointSeries wpfSeries, OxyPlot.Series.ScatterSeries oxySeries)
        {
            if (oxySeries.ItemsSource == null)
            {
                foreach (var point in oxySeries.Points)
                {
                    yield return (point.X, point.Y);
                }

                yield break;
            }

            foreach (var item in oxySeries.ItemsSource.Cast<object?>())
            {
                yield return GetScatterPointExportValue(wpfSeries, item);
            }
        }

        /// <summary>
        /// Extracts x and y export values for a scatter-series item.
        /// </summary>
        /// <param name="wpfSeries">The WPF scatter series.</param>
        /// <param name="item">The item to export.</param>
        /// <returns>The exported x and y values.</returns>
        private static (object X, object Y) GetScatterPointExportValue(Wpf.ScatterPointSeries wpfSeries, object? item)
        {
            if (item != null)
            {
                if (wpfSeries.Mapping != null)
                {
                    var point = wpfSeries.Mapping(item);
                    return (point.X, point.Y);
                }

                if (item is OxyPlot.Series.ScatterPoint scatterPoint)
                {
                    return (scatterPoint.X, scatterPoint.Y);
                }

                if (item is DataPoint dataPoint)
                {
                    return (dataPoint.X, dataPoint.Y);
                }

                if (item is OxyPlot.Series.IScatterPointProvider scatterPointProvider)
                {
                    var point = scatterPointProvider.GetScatterPoint();
                    return (point.X, point.Y);
                }
            }

            return (
                GetExportPropertyValue(item, wpfSeries.DataFieldX, "X"),
                GetExportPropertyValue(item, wpfSeries.DataFieldY, "Y"));
        }

        private static IEnumerable<(object XLower, object X, object XUpper, object YLower, object Y, object YUpper)> GetScatterErrorExportValues(Wpf.ScatterErrorSeries wpfSeries, OxyPlot.Series.ScatterErrorSeries oxySeries)
        {
            if (oxySeries.ItemsSource == null)
            {
                foreach (var point in oxySeries.Points)
                {
                    yield return GetScatterErrorPointExportValue(point);
                }

                yield break;
            }

            foreach (var item in oxySeries.ItemsSource.Cast<object?>())
            {
                if (item != null && wpfSeries.Mapping != null)
                {
                    yield return GetScatterErrorPointExportValue(wpfSeries.Mapping(item));
                }
                else if (item is OxyPlot.Series.ScatterErrorPoint scatterErrorPoint)
                {
                    yield return GetScatterErrorPointExportValue(scatterErrorPoint);
                }
                else
                {
                    yield return (
                        GetExportPropertyValue(item, wpfSeries.DataFieldLowerErrorX),
                        GetExportPropertyValue(item, wpfSeries.DataFieldX, "X"),
                        GetExportPropertyValue(item, wpfSeries.DataFieldUpperErrorX),
                        GetExportPropertyValue(item, wpfSeries.DataFieldLowerErrorY),
                        GetExportPropertyValue(item, wpfSeries.DataFieldY, "Y"),
                        GetExportPropertyValue(item, wpfSeries.DataFieldUpperErrorY));
                }
            }
        }

        /// <summary>
        /// Extracts central and error-bound values from a scatter error point.
        /// </summary>
        /// <param name="point">The scatter error point to export.</param>
        /// <returns>The lower, central, and upper x and y values.</returns>
        private static (object XLower, object X, object XUpper, object YLower, object Y, object YUpper) GetScatterErrorPointExportValue(OxyPlot.Series.ScatterErrorPoint point)
        {
            return (point.LowerErrorX, point.X, point.UpperErrorX, point.LowerErrorY, point.Y, point.UpperErrorY);
        }

        /// <summary>
        /// Adds category-axis labels to an exported series table when present.
        /// </summary>
        /// <param name="series">The series whose axes are inspected.</param>
        /// <param name="dataTable">The export table to update.</param>
        private static void AddCategoryAxisLabels(OxyPlot.Series.XYAxisSeries? series, DataTable dataTable)
        {
            if (series?.XAxis is OxyPlot.Axes.CategoryAxis xCategoryAxis)
            {
                AddCategoryAxisLabelColumn(xCategoryAxis, dataTable, "Xcategory");
            }
            else if (series?.YAxis is OxyPlot.Axes.CategoryAxis yCategoryAxis)
            {
                AddCategoryAxisLabelColumn(yCategoryAxis, dataTable, "Ycategory");
            }
        }

        /// <summary>
        /// Adds a category-label column from an OxyPlot category axis.
        /// </summary>
        /// <param name="categoryAxis">The category axis that supplies labels.</param>
        /// <param name="dataTable">The export table to update.</param>
        /// <param name="columnName">The export column name.</param>
        private static void AddCategoryAxisLabelColumn(OxyPlot.Axes.CategoryAxis categoryAxis, DataTable dataTable, string columnName)
        {
            dataTable.Columns.Add(columnName, typeof(string));
            if (categoryAxis.ItemsSource == null)
            {
                return;
            }

            if (categoryAxis.ItemsSource is IEnumerable<string> labels)
            {
                int i = 0;
                foreach (var label in labels.Take(dataTable.Rows.Count))
                {
                    dataTable.Rows[i][columnName] = label;
                    i++;
                }

                return;
            }

            int rowIndex = 0;
            foreach (var item in categoryAxis.ItemsSource.Cast<object?>().Take(dataTable.Rows.Count))
            {
                dataTable.Rows[rowIndex][columnName] = GetPropertyValueOrEmpty(item, categoryAxis.LabelField);
                rowIndex++;
            }
        }

        /// <summary>
        /// Gets a string export value from a preferred or fallback property.
        /// </summary>
        /// <param name="source">The source item.</param>
        /// <param name="propertyName">The preferred property name.</param>
        /// <param name="fallbackPropertyName">The fallback property name.</param>
        /// <returns>The property value converted to text, or an empty string.</returns>
        private static string GetExportPropertyValue(object? source, string? propertyName, string? fallbackPropertyName = null)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                return GetPropertyValueOrEmpty(source, propertyName);
            }

            return !string.IsNullOrWhiteSpace(fallbackPropertyName)
                ? GetPropertyValueOrEmpty(source, fallbackPropertyName)
                : "";
        }

        /// <summary>
        /// Gets a property value as text, returning an empty string when unavailable.
        /// </summary>
        /// <param name="source">The source item.</param>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value text, or an empty string.</returns>
        private static string GetPropertyValueOrEmpty(object? source, string? propertyName)
        {
            if (source == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return "";
            }

            if (TryGetPropertyValue(source, propertyName, out var value))
            {
                return Convert.ToString(value) ?? "";
            }

            return "";
        }

        /// <summary>
        /// Attempts to read a property value from a data row, data-row view, or CLR object.
        /// </summary>
        /// <param name="source">The source item.</param>
        /// <param name="propertyName">The property name to read.</param>
        /// <param name="value">The resolved property value.</param>
        /// <returns><c>true</c> when the property is found.</returns>
        private static bool TryGetPropertyValue(object source, string propertyName, out object? value)
        {
            if (source is DataRow row && row.Table.Columns.Contains(propertyName))
            {
                value = row[propertyName];
                return true;
            }

            if (source is DataRowView rowView && rowView.DataView.Table?.Columns.Contains(propertyName) == true)
            {
                value = rowView[propertyName];
                return true;
            }

            if (source is IDictionary<string, object?> genericDictionary && genericDictionary.TryGetValue(propertyName, out value))
            {
                return true;
            }

            if (source is System.Collections.IDictionary dictionary && dictionary.Contains(propertyName))
            {
                value = dictionary[propertyName];
                return true;
            }

            var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null)
            {
                value = property.GetValue(source, null);
                return true;
            }

            var descriptor = System.ComponentModel.TypeDescriptor.GetProperties(source).Find(propertyName, false);
            if (descriptor != null)
            {
                value = descriptor.GetValue(source);
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Merge all data tables by a primary key column.
        /// </summary>
        public DataTable MergeAll(IList<DataTable> tables, string primaryKeyColumn)
        {
            if (!tables.Any()) throw new ArgumentException("Tables must not be empty", nameof(tables));

            if (primaryKeyColumn != null)
            {
                foreach (var t in tables)
                {
                    if (!t.Columns.Contains(primaryKeyColumn))
                        throw new ArgumentException("All tables must have the specified primarykey column " + primaryKeyColumn, nameof(primaryKeyColumn));
                }
            }

            if (tables.Count == 1) return tables[0];

            var table = new DataTable("TblUnion");
            table.BeginLoadData();

            foreach (var t in tables)
            {
                table.Merge(t);
            }

            table.EndLoadData();

            if (primaryKeyColumn != null)
            {
                var pkGroups = table.AsEnumerable().GroupBy(r => r[primaryKeyColumn]);
                var dupGroups = pkGroups.Where(g => g.Count() > 1);

                foreach (var grpDup in dupGroups)
                {
                    DataRow firstRow = grpDup.First();

                    foreach (DataColumn c in table.Columns)
                    {
                        if (firstRow.IsNull(c))
                        {
                            DataRow? firstNotNullRow = grpDup.Skip(1).FirstOrDefault(r => !r.IsNull(c));
                            if (firstNotNullRow != null) firstRow[c] = firstNotNullRow[c];
                        }
                    }

                    var rowsToRemove = grpDup.Skip(1).ToList();
                    foreach (DataRow rowToRemove in rowsToRemove)
                    {
                        table.Rows.Remove(rowToRemove);
                    }
                }
            }

            return table;
        }

        /// <summary>
        /// Merge two data tables by index (row-wise).
        /// </summary>
        public DataTable? MergeTablesByIndex(DataTable t1, DataTable t2)
        {
            if (t1 == null || t2 == null) return null;
            var t3 = t1.Clone();

            foreach (DataColumn col in t2.Columns)
            {
                string newColumnName = col.ColumnName;
                int colNum = 1;

                while (t3.Columns.Contains(newColumnName))
                {
                    newColumnName = string.Format("{0}_{1}", col.ColumnName, System.Threading.Interlocked.Increment(ref colNum));
                }

                t3.Columns.Add(newColumnName, col.DataType);
            }

            var mergedRows = t1.AsEnumerable().Zip(t2.AsEnumerable(), (r1, r2) => r1.ItemArray.Concat(r2.ItemArray).ToArray());

            foreach (var rowFields in mergedRows)
            {
                t3.Rows.Add(rowFields);
            }

            return t3;
        }

        #endregion

        #region Save Plot

        /// <summary>
        /// On Click, open the save plot image dialog.
        /// </summary>
        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            if (Plot == null) return;
            var saveImageDialog = new SavePlotImageDialog(Plot) { Owner = Window.GetWindow(this) };
            saveImageDialog.ShowDialog();
        }

        #endregion

        #region Properties and Swap Axes

        /// <summary>
        /// Open plot properties.
        /// </summary>
        private void PropertiesButton_Click(object sender, RoutedEventArgs e)
        {
            PropertiesCalled?.Invoke(Plot, true, null, null!);
        }

        /// <summary>
        /// Swap the X and Y axes.
        /// </summary>
        private void SwapAxesButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var s in Plot.Series)
            {
                if (_nonSwapSeriesTypes.Contains(s.GetType())) return;
            }

            foreach (var axis in Plot.Axes)
            {
                if (axis.Position == OxyPlot.Axes.AxisPosition.Bottom)
                {
                    axis.Position = OxyPlot.Axes.AxisPosition.Left;
                }
                else if (axis.Position == OxyPlot.Axes.AxisPosition.Left)
                {
                    axis.Position = OxyPlot.Axes.AxisPosition.Bottom;
                }
            }

            foreach (var s in Plot.Series)
            {
                if (typeof(Wpf.DataPointSeries).IsAssignableFrom(s.GetType()))
                {
                    SwapDataPointSeries((Wpf.DataPointSeries)s);
                }
                else if (s is Wpf.ScatterPointSeries)
                {
                    SwapScatterSeries((Wpf.ScatterPointSeries)s);
                }
                else if (s is Wpf.ScatterErrorSeries)
                {
                    SwapScatterErrorSeries((Wpf.ScatterErrorSeries)s);
                }
                else if (s is Wpf.BoxPlotSeries)
                {
                    foreach (var axis in Plot.Axes)
                    {
                        if (axis is Wpf.CategoryAxis)
                        {
                            ((Wpf.BoxPlotSeries)s).IsVertical = axis.Position == OxyPlot.Axes.AxisPosition.Bottom;
                        }
                    }
                }
            }

            Plot.InvalidatePlot(true);
        }

        /// <summary>
        /// Swaps the X and Y coordinates for a DataPointSeries, including data field bindings if applicable.
        /// </summary>
        /// <param name="dps">The DataPointSeries to swap.</param>
        private void SwapDataPointSeries(Wpf.DataPointSeries dps)
        {
            if (dps == null) return;
            if (dps.ItemsSource == null)
            {
                SwapDataPoints((OxyPlot.Series.DataPointSeries)dps.InternalSeries);
            }
            else
            {
                if (dps.DataFieldX == null && dps.DataFieldY == null)
                {
                    dps.DataFieldX = "Y";
                    dps.DataFieldY = "X";
                }
                else
                {
                    string? dfx = dps.DataFieldX;
                    dps.DataFieldX = dps.DataFieldY;
                    dps.DataFieldY = dfx;
                }

                if (dps is Wpf.AreaSeries)
                {
                    var areaSeries = (Wpf.AreaSeries)dps;
                    string dfx2 = areaSeries.DataFieldX2;
                    areaSeries.DataFieldX2 = areaSeries.DataFieldY2;
                    areaSeries.DataFieldY2 = dfx2;
                }
            }
        }

        /// <summary>
        /// Swaps the X and Y coordinates for all data points in an OxyPlot DataPointSeries.
        /// </summary>
        /// <param name="dps">The OxyPlot DataPointSeries to swap.</param>
        private void SwapDataPoints(OxyPlot.Series.DataPointSeries dps)
        {
            if (dps?.Points == null || dps.Points.Count == 0) return;
            var pnts = dps.Points.ToArray();
            dps.Points.Clear();
            foreach (var p in pnts)
            {
                dps.Points.Add(new DataPoint(p.Y, p.X));
            }
        }

        /// <summary>
        /// Swaps the X and Y coordinates for a ScatterSeries, including data field bindings if applicable.
        /// </summary>
        /// <param name="sps">The ScatterSeries to swap.</param>
        private void SwapScatterSeries(Wpf.ScatterSeries<OxyPlot.Series.ScatterPoint> sps)
        {
            if (sps == null) return;
            if (sps.ItemsSource == null)
            {
                SwapScatterPoints((OxyPlot.Series.ScatterSeries)sps.InternalSeries);
            }
            else
            {
                string dfx = sps.DataFieldX;
                sps.DataFieldX = sps.DataFieldY;
                sps.DataFieldY = dfx;
            }
        }

        /// <summary>
        /// Swaps the X and Y coordinates for all scatter points in an OxyPlot ScatterSeries.
        /// </summary>
        /// <param name="dps">The OxyPlot ScatterSeries to swap.</param>
        private void SwapScatterPoints(OxyPlot.Series.ScatterSeries dps)
        {
            if (dps?.Points == null || dps.Points.Count == 0) return;
            var pnts = dps.Points.ToArray();
            dps.Points.Clear();
            foreach (var p in pnts)
            {
                dps.Points.Add(new OxyPlot.Series.ScatterPoint(p.Y, p.X, p.Size, p.Value, p.Tag));
            }
        }

        /// <summary>
        /// Swaps the X and Y coordinates for a ScatterErrorSeries, including data field bindings and error values.
        /// </summary>
        /// <param name="sps">The ScatterErrorSeries to swap.</param>
        private void SwapScatterErrorSeries(Wpf.ScatterErrorSeries sps)
        {
            if (sps == null) return;
            if (sps.ItemsSource == null)
            {
                SwapScatterErrorPoints((OxyPlot.Series.ScatterErrorSeries)sps.InternalSeries);
            }
            else
            {
                string dfx = sps.DataFieldX;
                sps.DataFieldX = sps.DataFieldY;
                sps.DataFieldY = dfx;

                dfx = sps.DataFieldLowerErrorX;
                sps.DataFieldLowerErrorX = sps.DataFieldLowerErrorY;
                sps.DataFieldLowerErrorY = dfx;

                dfx = sps.DataFieldUpperErrorX;
                sps.DataFieldUpperErrorX = sps.DataFieldUpperErrorY;
                sps.DataFieldUpperErrorY = dfx;
            }
        }

        /// <summary>
        /// Swaps the X and Y coordinates for all scatter error points in an OxyPlot ScatterErrorSeries, including error values.
        /// </summary>
        /// <param name="dps">The OxyPlot ScatterErrorSeries to swap.</param>
        private void SwapScatterErrorPoints(OxyPlot.Series.ScatterErrorSeries dps)
        {
            if (dps?.Points == null || dps.Points.Count == 0) return;
            var pnts = dps.Points.ToArray();
            dps.Points.Clear();
            foreach (var p in pnts)
            {
                dps.Points.Add(new OxyPlot.Series.ScatterErrorPoint(p.Y, p.X, p.LowerErrorX, p.UpperErrorX, p.LowerErrorY, p.UpperErrorY, p.Size, p.Value, p.Tag));
            }
        }

        #endregion
    }
}
