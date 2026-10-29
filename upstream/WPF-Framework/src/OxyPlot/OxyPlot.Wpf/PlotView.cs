// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotView.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using OxyPlot;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Media;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />. This <see cref="IPlotView"/> supports
    /// both <see cref="CanvasRenderContext"/> (default) and <see cref="DrawingVisualRenderContext"/> backends.
    /// </summary>
    public partial class PlotView : PlotViewBase
    {
        /// <summary>
        /// Identifies the <see cref="TextMeasurementMethod"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextMeasurementMethodProperty =
            DependencyProperty.Register(
                nameof(TextMeasurementMethod), typeof(TextMeasurementMethod), typeof(PlotView), new PropertyMetadata(TextMeasurementMethod.TextBlock));

        /// <summary>
        /// Initializes a new instance of the <see cref="PlotView" /> class.
        /// </summary>
        public PlotView()
        {
            this.DisconnectCanvasWhileUpdating = true;
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, this.DoCopy));
        }

        /// <summary>
        /// Gets or sets the rendering backend used by this plot view.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Must be set BEFORE the control is loaded (before <see cref="FrameworkElement.OnApplyTemplate"/> is called),
        /// as it determines which plot presenter and render context are created.
        /// </para>
        /// <para>
        /// <see cref="RenderingBackend.Canvas"/>: Each plot element is a separate WPF FrameworkElement.
        /// <see cref="RenderingBackend.DrawingVisual"/> (default): All elements are drawn to a single DrawingVisual.
        /// </para>
        /// </remarks>
        public RenderingBackend RenderingBackend { get; set; } = RenderingBackend.DrawingVisual;

        /// <summary>
        /// Gets or sets a value indicating whether to disconnect the canvas while updating.
        /// </summary>
        /// <value><c>true</c> if canvas should be disconnected while updating; otherwise, <c>false</c>.</value>
        public bool DisconnectCanvasWhileUpdating { get; set; }

        /// <summary>
        /// Gets or sets the vertical zoom cursor.
        /// </summary>
        /// <value>The zoom vertical cursor.</value>
        public TextMeasurementMethod TextMeasurementMethod
        {
            get => (TextMeasurementMethod)this.GetValue(TextMeasurementMethodProperty);
            set => this.SetValue(TextMeasurementMethodProperty, value);
        }

        /// <summary>
        /// Gets the Canvas (only valid when using the Canvas backend).
        /// </summary>
        protected Canvas Canvas => this.plotPresenter as Canvas;

        /// <summary>
        /// Gets the CanvasRenderContext (only valid when using the Canvas backend).
        /// </summary>
        private CanvasRenderContext CanvasRenderContext => this.renderContext as CanvasRenderContext;

        /// <summary>
        /// Gets the DrawingVisualRenderContext (only valid when using the DrawingVisual backend).
        /// </summary>
        private DrawingVisualRenderContext DrawingVisualRenderContext => this.renderContext as DrawingVisualRenderContext;

        /// <inheritdoc/>
        protected override void ClearBackground()
        {
            if (this.RenderingBackend == RenderingBackend.DrawingVisual)
            {
                // Background is drawn as the first operation in RenderOverride.
                return;
            }

            // Canvas backend: existing behavior, unchanged.
            this.Canvas.Children.Clear();

            if (this.ActualModel != null && this.ActualModel.Background.IsVisible())
            {
                this.Canvas.Background = this.ActualModel.Background.ToBrush();
            }
            else
            {
                this.Canvas.Background = null;
            }
        }

        /// <inheritdoc/>
        protected override FrameworkElement CreatePlotPresenter()
        {
            if (this.RenderingBackend == RenderingBackend.DrawingVisual)
            {
                return new DrawingVisualHost();
            }

            return new Canvas();
        }

        /// <inheritdoc/>
        protected override IRenderContext CreateRenderContext()
        {
            if (this.RenderingBackend == RenderingBackend.DrawingVisual)
            {
                return new DrawingVisualRenderContext();
            }

            return new CanvasRenderContext(this.Canvas);
        }

        /// <inheritdoc/>
        protected override void OnRender(DrawingContext drawingContext)
        {
            this.Render();
            base.OnRender(drawingContext);
        }

        /// <inheritdoc/>
        protected override void RenderOverride()
        {
            if (this.RenderingBackend == RenderingBackend.DrawingVisual)
            {
                var host = (DrawingVisualHost)this.plotPresenter;
                var rc = this.DrawingVisualRenderContext;

                rc.OpenDrawing(host.Visual);

                // Draw background.
                if (this.ActualModel != null && this.ActualModel.Background.IsVisible())
                {
                    var bgColor = this.ActualModel.Background;
                    rc.DrawRectangle(
                        new OxyRect(0, 0, this.ActualWidth, this.ActualHeight),
                        bgColor,
                        OxyColors.Undefined,
                        0,
                        EdgeRenderingMode.PreferSpeed);
                }

                base.RenderOverride();
                rc.CloseDrawing();
                return;
            }

            // Canvas backend: existing behavior, unchanged.
            this.CanvasRenderContext.TextMeasurementMethod = this.TextMeasurementMethod;
            if (this.DisconnectCanvasWhileUpdating)
            {
                // TODO: profile... not sure if this makes any difference
                var idx = this.grid.Children.IndexOf(this.plotPresenter);
                if (idx != -1)
                {
                    this.grid.Children.RemoveAt(idx);
                }

                base.RenderOverride();

                if (idx != -1)
                {
                    // reinsert the canvas again
                    this.grid.Children.Insert(idx, this.plotPresenter);
                }
            }
            else
            {
                base.RenderOverride();
            }
        }

        /// <inheritdoc/>
        protected override double UpdateDpi()
        {
            var scale = base.UpdateDpi();

            if (this.RenderingBackend == RenderingBackend.DrawingVisual)
            {
                var dvrc = this.DrawingVisualRenderContext;
                if (dvrc != null)
                {
                    dvrc.DpiScale = scale;
                    var ancestor = this.GetAncestorVisualFromVisualTree(this);
                    dvrc.VisualOffset = ancestor != null ? this.TransformToAncestor(ancestor).Transform(default) : default;
                }

                return scale;
            }

            // Canvas backend: existing behavior, unchanged.
            var crc = this.CanvasRenderContext;
            if (crc != null)
            {
                crc.DpiScale = scale;
                var ancestor = this.GetAncestorVisualFromVisualTree(this);
                crc.VisualOffset = ancestor != null ? this.TransformToAncestor(ancestor).Transform(default) : default;
            }

            return scale;
        }

        /// <summary>
        /// Performs the copy operation.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ExecutedRoutedEventArgs" /> instance containing the event data.</param>
        private void DoCopy(object sender, ExecutedRoutedEventArgs e)
        {
            if (this.ActualModel == null)
            {
                return;
            }

            var exporter = new PngExporter() { Width = (int)this.ActualWidth, Height = (int)this.ActualHeight };
            var bitmap = exporter.ExportToBitmap(this.ActualModel);
            Clipboard.SetImage(bitmap);
        }


        /// <summary>
        /// Returns a reference to the visual object that hosts the dependency object in the visual tree.
        /// </summary>
        /// <returns> The host window from the visual tree.</returns>
        private Visual GetAncestorVisualFromVisualTree(DependencyObject startElement)
        {

            DependencyObject child = startElement;
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                child = parent;
                parent = VisualTreeHelper.GetParent(child);
            }

            return child is Visual visualChild ? visualChild : Window.GetWindow(this);
        }
    }
}
