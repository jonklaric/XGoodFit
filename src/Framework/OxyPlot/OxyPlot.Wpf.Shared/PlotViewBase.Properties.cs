// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotViewBase.Properties.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using Avalonia;
    using Avalonia.Controls.Templates;
    using Avalonia.Input;

    /// <summary>
    /// Base class for WPF PlotView implementations.
    /// </summary>
    public abstract partial class PlotViewBase
    {
        /// <summary>
        /// Identifies the <see cref="Controller"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IPlotController> ControllerProperty =
            AvaloniaProperty.Register<PlotViewBase, IPlotController>(nameof(Controller));

        /// <summary>
        /// Identifies the <see cref="DefaultTrackerTemplate"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IControlTemplate> DefaultTrackerTemplateProperty =
            AvaloniaProperty.Register<PlotViewBase, IControlTemplate>(
                nameof(DefaultTrackerTemplate), null);

        /// <summary>
        /// Identifies the <see cref="IsMouseWheelEnabled"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsMouseWheelEnabledProperty =
            AvaloniaProperty.Register<PlotViewBase, bool>(nameof(IsMouseWheelEnabled), true);

        /// <summary>
        /// Identifies the <see cref="Model"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<PlotModel> ModelProperty =
            AvaloniaProperty.Register<PlotViewBase, PlotModel>(nameof(Model), null);

        /// <summary>
        /// Identifies the <see cref="PanCursor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Cursor> PanCursorProperty = AvaloniaProperty.Register<PlotViewBase, Cursor>(
            nameof(PanCursor), new Cursor(StandardCursorType.Hand));

        /// <summary>
        /// Identifies the <see cref="ZoomHorizontalCursor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Cursor> ZoomHorizontalCursorProperty =
            AvaloniaProperty.Register<PlotViewBase, Cursor>(
                nameof(ZoomHorizontalCursor), new Cursor(StandardCursorType.SizeWestEast));

        /// <summary>
        /// Identifies the <see cref="ZoomRectangleCursor"/> dependency property.
        /// </summary>
        /// <remarks>XGoodFit port: Avalonia's diagonal resize cursor, matching WPF's SizeNWSE, is TopLeftCorner.</remarks>
        public static readonly StyledProperty<Cursor> ZoomRectangleCursorProperty =
            AvaloniaProperty.Register<PlotViewBase, Cursor>(
                nameof(ZoomRectangleCursor), new Cursor(StandardCursorType.TopLeftCorner));

        /// <summary>
        /// Identifies the <see cref="ZoomRectangleTemplate"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IControlTemplate> ZoomRectangleTemplateProperty =
            AvaloniaProperty.Register<PlotViewBase, IControlTemplate>(
                nameof(ZoomRectangleTemplate), null);

        /// <summary>
        /// Identifies the <see cref="ZoomVerticalCursor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Cursor> ZoomVerticalCursorProperty =
            AvaloniaProperty.Register<PlotViewBase, Cursor>(
                nameof(ZoomVerticalCursor), new Cursor(StandardCursorType.SizeNorthSouth));

        /// <summary>
        /// Gets or sets the Plot controller.
        /// </summary>
        /// <value>The Plot controller.</value>
        public IPlotController Controller
        {
            get => (IPlotController)this.GetValue(ControllerProperty);
            set => this.SetValue(ControllerProperty, value);
        }

        /// <summary>
        /// Gets or sets the default tracker template.
        /// </summary>
        public IControlTemplate DefaultTrackerTemplate
        {
            get => (IControlTemplate)this.GetValue(DefaultTrackerTemplateProperty);
            set => this.SetValue(DefaultTrackerTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether IsMouseWheelEnabled.
        /// </summary>
        public bool IsMouseWheelEnabled
        {
            get => (bool)this.GetValue(IsMouseWheelEnabledProperty);
            set => this.SetValue(IsMouseWheelEnabledProperty, value);
        }

        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public PlotModel Model
        {
            get => (PlotModel)this.GetValue(ModelProperty);
            set => this.SetValue(ModelProperty, value);
        }

        /// <summary>
        /// Gets or sets the pan cursor.
        /// </summary>
        /// <value>The pan cursor.</value>
        public Cursor PanCursor
        {
            get => (Cursor)this.GetValue(PanCursorProperty);
            set => this.SetValue(PanCursorProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal zoom cursor.
        /// </summary>
        /// <value>The zoom horizontal cursor.</value>
        public Cursor ZoomHorizontalCursor
        {
            get => (Cursor)this.GetValue(ZoomHorizontalCursorProperty);
            set => this.SetValue(ZoomHorizontalCursorProperty, value);
        }

        /// <summary>
        /// Gets or sets the rectangle zoom cursor.
        /// </summary>
        /// <value>The zoom rectangle cursor.</value>
        public Cursor ZoomRectangleCursor
        {
            get => (Cursor)this.GetValue(ZoomRectangleCursorProperty);
            set => this.SetValue(ZoomRectangleCursorProperty, value);
        }

        /// <summary>
        /// Gets or sets the zoom rectangle template.
        /// </summary>
        /// <value>The zoom rectangle template.</value>
        public IControlTemplate ZoomRectangleTemplate
        {
            get => (IControlTemplate)this.GetValue(ZoomRectangleTemplateProperty);
            set => this.SetValue(ZoomRectangleTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical zoom cursor.
        /// </summary>
        /// <value>The zoom vertical cursor.</value>
        public Cursor ZoomVerticalCursor
        {
            get => (Cursor)this.GetValue(ZoomVerticalCursorProperty);
            set => this.SetValue(ZoomVerticalCursorProperty, value);
        }
    }
}
