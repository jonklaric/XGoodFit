using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using OxyPlot;
using OxyPlot.Annotations;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// Control for editing annotation properties in an OxyPlot chart.
    /// Supports various annotation types including text, arrow, line, shape, and point annotations.
    /// </summary>
    public partial class AnnotationControl : UserControl
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationControl"/> class.
        /// </summary>
        public AnnotationControl()
        {
            InitializeComponent();

        }

        #endregion



        /// <summary>
        /// Gets the available text line position options (0 to 1 in 0.1 increments).
        /// </summary>
        public static List<double> TextLinePositionOptions { get; } = new List<double> { 0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1 };

        /// <summary>
        /// Gets the available annotation layer options.
        /// </summary>
        public static List<AnnotationLayer> AnnotationLayerOptions { get; } =
            new List<AnnotationLayer>((AnnotationLayer[])Enum.GetValues(typeof(AnnotationLayer)));

        /// <summary>
        /// Gets the available annotation text orientation options.
        /// </summary>
        public static List<AnnotationTextOrientation> AnnotationTextOrientationOptions { get; } =
            new List<AnnotationTextOrientation>((AnnotationTextOrientation[])Enum.GetValues(typeof(AnnotationTextOrientation)));

        /// <summary>
        /// Gets the available function annotation type options.
        /// </summary>
        public static List<FunctionAnnotationType> AnnotationFunctionTypeOptions { get; } =
            new List<FunctionAnnotationType>((FunctionAnnotationType[])Enum.GetValues(typeof(FunctionAnnotationType)));

        /// <summary>
        /// Gets the available line annotation type options.
        /// </summary>
        public static List<LineAnnotationType> LineAnnotationTypeOptions { get; } =
            new List<LineAnnotationType>((LineAnnotationType[])Enum.GetValues(typeof(LineAnnotationType)));

        /// <summary>
        /// Gets the available marker type options for point annotations.
        /// </summary>
        public static List<MarkerType> MarkerTypeOptions { get; } =
            new List<MarkerType>((MarkerType[])Enum.GetValues(typeof(MarkerType)));

        /// <summary>
        /// Gets the available line join options.
        /// </summary>
        public static List<LineJoin> LineJoinOptions { get; } =
            new List<LineJoin>((LineJoin[])Enum.GetValues(typeof(LineJoin)));

        /// <summary>
        /// Gets the available line style options.
        /// </summary>
        public static List<System.Windows.Media.DoubleCollection> LineStyleOptions => GenericControls.LineStyleSelectorControl.LineStyleOptions;

        /// <summary>
        /// Identifies the Annotation dependency property.
        /// </summary>
        public static readonly DependencyProperty AnnotationProperty = DependencyProperty.Register(
            nameof(Annotation), typeof(Wpf.TextualAnnotation), typeof(AnnotationControl),
            new PropertyMetadata(null, AnnotationChangedCallback));

        /// <summary>
        /// Gets or sets the annotation being edited by this control.
        /// </summary>
        public Wpf.TextualAnnotation Annotation
        {
            get => (Wpf.TextualAnnotation)GetValue(AnnotationProperty);
            set => SetValue(AnnotationProperty, value);
        }

        /// <summary>
        /// Handles changes to the Annotation property and updates the control's UI accordingly.
        /// Forces a layout refresh after updating visibility of controls.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">Event args containing the old and new values.</param>
        private static void AnnotationChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null) return;
            if (d is not AnnotationControl) return;
            var thisControl = (AnnotationControl)d;

            if (e.NewValue == null) return;

            // Show Expanders
            thisControl.TextEXP.Visibility = Visibility.Visible;
            thisControl.DisplayOptionsEXP.Visibility = Visibility.Visible;

            // Determine whether to show X-Y Controls
            if (e.NewValue is Wpf.LineAnnotation)
            {
                var newAnnotation = (Wpf.LineAnnotation)e.NewValue;

                if (newAnnotation.Type == LineAnnotationType.Horizontal)
                {
                    thisControl.XValueControl.Visibility = Visibility.Collapsed;
                    thisControl.YValueControl.Visibility = Visibility.Visible;
                    thisControl.InterceptControl.Visibility = Visibility.Collapsed;
                    thisControl.SlopeControl.Visibility = Visibility.Collapsed;
                }
                else if (newAnnotation.Type == LineAnnotationType.Vertical)
                {
                    thisControl.XValueControl.Visibility = Visibility.Visible;
                    thisControl.YValueControl.Visibility = Visibility.Collapsed;
                    thisControl.InterceptControl.Visibility = Visibility.Collapsed;
                    thisControl.SlopeControl.Visibility = Visibility.Collapsed;
                }
                else if (newAnnotation.Type == LineAnnotationType.LinearEquation)
                {
                    thisControl.XValueControl.Visibility = Visibility.Collapsed;
                    thisControl.YValueControl.Visibility = Visibility.Collapsed;
                    thisControl.InterceptControl.Visibility = Visibility.Visible;
                    thisControl.SlopeControl.Visibility = Visibility.Visible;
                }
            }
            else if (e.NewValue is Wpf.PointAnnotation)
            {
                thisControl.XValueControl.Visibility = Visibility.Visible;
                thisControl.YValueControl.Visibility = Visibility.Visible;
                thisControl.InterceptControl.Visibility = Visibility.Collapsed;
                thisControl.SlopeControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                // Hide controls
                thisControl.XValueControl.Visibility = Visibility.Collapsed;
                thisControl.YValueControl.Visibility = Visibility.Collapsed;
                thisControl.InterceptControl.Visibility = Visibility.Collapsed;
                thisControl.SlopeControl.Visibility = Visibility.Collapsed;
            }

            // Determine whether to show the Text Angle Control
            if (e.NewValue is Wpf.LineAnnotation ||
                e.NewValue.GetType() == typeof(Wpf.PolylineAnnotation))
            {
                thisControl.TextAngleControl.Visibility = Visibility.Collapsed;
            }
            else
            {
                thisControl.TextAngleControl.Visibility = Visibility.Visible;
            }

            // Force layout update to sync bindings after annotation change
            thisControl.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                thisControl.UpdateLayout();
            }));
        }

        /// <summary>
        /// Identifies the ExpanderStyle dependency property.
        /// </summary>
        public static readonly DependencyProperty ExpanderStyleProperty = DependencyProperty.Register(
            nameof(ExpanderStyle), typeof(Style), typeof(AnnotationControl));

        /// <summary>
        /// Gets or sets the style applied to expanders in this control.
        /// </summary>
        public Style ExpanderStyle
        {
            get => (Style)GetValue(ExpanderStyleProperty);
            set => SetValue(ExpanderStyleProperty, value);
        }


        /// <summary>
        /// Hides the expander controls. Useful when an annotation is deleted.
        /// </summary>
        public void HideExpanders()
        {
            TextEXP.Visibility = Visibility.Collapsed;
            DisplayOptionsEXP.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Handles changes to the line annotation type, showing or hiding relevant controls.
        /// </summary>
        /// <param name="sender">The line type control.</param>
        /// <param name="e">Event args containing selection details.</param>
        private void LineTypeControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Annotation is not Wpf.LineAnnotation) return;

            var selectedType = (LineAnnotationType)((ComboBox)LineTypeControl.InnerContent).SelectedItem;

            if (selectedType == LineAnnotationType.Horizontal)
            {
                XValueControl.Visibility = Visibility.Collapsed;
                YValueControl.Visibility = Visibility.Visible;
                InterceptControl.Visibility = Visibility.Collapsed;
                SlopeControl.Visibility = Visibility.Collapsed;
            }
            else if (selectedType == LineAnnotationType.Vertical)
            {
                XValueControl.Visibility = Visibility.Visible;
                YValueControl.Visibility = Visibility.Collapsed;
                InterceptControl.Visibility = Visibility.Collapsed;
                SlopeControl.Visibility = Visibility.Collapsed;
            }
            else if (selectedType == LineAnnotationType.LinearEquation)
            {
                XValueControl.Visibility = Visibility.Collapsed;
                YValueControl.Visibility = Visibility.Collapsed;
                InterceptControl.Visibility = Visibility.Visible;
                SlopeControl.Visibility = Visibility.Visible;
            }
        }
    }

    /// <summary>
    /// Converts between OxyPlot DataPoint and WPF Point types.
    /// </summary>
    public class DataPointToPointConverter : IValueConverter
    {
        /// <summary>
        /// Converts an OxyPlot DataPoint to a WPF Point.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (value is not DataPoint) return null;
            var dp = (DataPoint)value;
            return new System.Windows.Point(dp.X, dp.Y);
        }

        /// <summary>
        /// Converts a WPF Point back to an OxyPlot DataPoint.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (value is not System.Windows.Point) return null;
            var p = (System.Windows.Point)value;
            return new DataPoint(p.X, p.Y);
        }
    }

    /// <summary>
    /// Converts between OxyPlot ScreenVector and WPF Point types.
    /// </summary>
    public class ScreenVectorToPointConverter : IValueConverter
    {
        /// <summary>
        /// Converts an OxyPlot ScreenVector to a WPF Point.
        /// </summary>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (value is not ScreenVector) return null;
            var sv = (ScreenVector)value;
            return new System.Windows.Point(sv.X, sv.Y);
        }

        /// <summary>
        /// Converts a WPF Point back to an OxyPlot ScreenVector.
        /// </summary>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null) return value;
            if (value is not System.Windows.Point) return null;
            var p = (System.Windows.Point)value;
            return new ScreenVector(p.X, p.Y);
        }
    }

    /// <summary>
    /// Converter for <see cref="System.Windows.HorizontalAlignment"/> bindings between the
    /// <see cref="GenericControls.HorizontalAlignmentControl"/> and OxyPlot.Wpf dependency properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both the source (e.g., <c>Plot.LegendItemAlignment</c>, <c>Annotation.TextHorizontalAlignment</c>)
    /// and target (<c>HorizontalAlignmentControl.Alignment</c>) are <see cref="System.Windows.HorizontalAlignment"/>.
    /// The OxyPlot.Wpf layer converts to <c>OxyPlot.HorizontalAlignment</c> internally via
    /// <c>ToHorizontalAlignment()</c> extension methods during <c>SynchronizeProperties</c>.
    /// </para>
    /// <para>
    /// This converter serves two purposes:
    /// <list type="number">
    /// <item><description>
    /// It ensures the WPF binding engine invokes the full conversion pipeline on every value change,
    /// which is required for <c>Binding.SourceUpdated</c> to fire reliably. Without a converter,
    /// WPF may skip the source update when it detects no type change is needed.
    /// </description></item>
    /// <item><description>
    /// When <c>ConverterParameter</c> is <c>"Swap"</c>, it swaps <see cref="System.Windows.HorizontalAlignment.Left"/>
    /// and <see cref="System.Windows.HorizontalAlignment.Right"/> to correct the semantic mismatch between OxyPlot's
    /// anchor-based alignment (Left = anchor at left edge, text extends right) and the user's expectation
    /// (Left = position text to the left).
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OxyHorizontalAlignmentConverter : IValueConverter
    {
        /// <summary>
        /// Converts the source value. When <paramref name="parameter"/> is <c>"Swap"</c>,
        /// swaps <see cref="System.Windows.HorizontalAlignment.Left"/> and <see cref="System.Windows.HorizontalAlignment.Right"/>.
        /// </summary>
        /// <param name="value">The source <see cref="System.Windows.HorizontalAlignment"/> value.</param>
        /// <param name="targetType">The target type (unused).</param>
        /// <param name="parameter">Optional. Pass <c>"Swap"</c> to swap Left and Right.</param>
        /// <param name="culture">The culture (unused).</param>
        /// <returns>The converted alignment value.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Windows.HorizontalAlignment alignment)
            {
                if (parameter is string s && s.Equals("Swap", StringComparison.OrdinalIgnoreCase))
                    return SwapLeftRight(alignment);
                return alignment;
            }
            return System.Windows.HorizontalAlignment.Center;
        }

        /// <summary>
        /// Converts the target value back to the source. When <paramref name="parameter"/> is <c>"Swap"</c>,
        /// swaps <see cref="System.Windows.HorizontalAlignment.Left"/> and <see cref="System.Windows.HorizontalAlignment.Right"/>.
        /// </summary>
        /// <param name="value">The target <see cref="System.Windows.HorizontalAlignment"/> value.</param>
        /// <param name="targetType">The source type (unused).</param>
        /// <param name="parameter">Optional. Pass <c>"Swap"</c> to swap Left and Right.</param>
        /// <param name="culture">The culture (unused).</param>
        /// <returns>The converted alignment value.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Windows.HorizontalAlignment alignment)
            {
                if (parameter is string s && s.Equals("Swap", StringComparison.OrdinalIgnoreCase))
                    return SwapLeftRight(alignment);
                return alignment;
            }
            return System.Windows.HorizontalAlignment.Center;
        }

        /// <summary>
        /// Swaps <see cref="System.Windows.HorizontalAlignment.Left"/> and <see cref="System.Windows.HorizontalAlignment.Right"/>;
        /// all other values pass through unchanged.
        /// </summary>
        /// <param name="alignment">The alignment to swap.</param>
        /// <returns>The swapped alignment value.</returns>
        private static System.Windows.HorizontalAlignment SwapLeftRight(System.Windows.HorizontalAlignment alignment)
        {
            return alignment switch
            {
                System.Windows.HorizontalAlignment.Left => System.Windows.HorizontalAlignment.Right,
                System.Windows.HorizontalAlignment.Right => System.Windows.HorizontalAlignment.Left,
                _ => alignment
            };
        }
    }

    /// <summary>
    /// Converter for <see cref="System.Windows.VerticalAlignment"/> bindings between the
    /// <see cref="GenericControls.VerticalAlignmentControl"/> and OxyPlot.Wpf dependency properties.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both the source (e.g., <c>Annotation.TextVerticalAlignment</c>) and target
    /// (<c>VerticalAlignmentControl.Alignment</c>) are <see cref="System.Windows.VerticalAlignment"/>.
    /// The OxyPlot.Wpf layer converts to <c>OxyPlot.VerticalAlignment</c> internally via
    /// <c>ToVerticalAlignment()</c> extension methods during <c>SynchronizeProperties</c>.
    /// </para>
    /// <para>
    /// This converter serves two purposes:
    /// <list type="number">
    /// <item><description>
    /// It ensures the WPF binding engine invokes the full conversion pipeline on every value change,
    /// which is required for <c>Binding.SourceUpdated</c> to fire reliably.
    /// </description></item>
    /// <item><description>
    /// When <c>ConverterParameter</c> is <c>"Swap"</c>, it swaps <see cref="System.Windows.VerticalAlignment.Top"/>
    /// and <see cref="System.Windows.VerticalAlignment.Bottom"/> to correct the semantic mismatch between OxyPlot's
    /// anchor-based alignment (Top = anchor at top edge, text extends down) and the user's expectation
    /// (Top = position text toward the top).
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class OxyVerticalAlignmentConverter : IValueConverter
    {
        /// <summary>
        /// Converts the source value. When <paramref name="parameter"/> is <c>"Swap"</c>,
        /// swaps <see cref="System.Windows.VerticalAlignment.Top"/> and <see cref="System.Windows.VerticalAlignment.Bottom"/>.
        /// </summary>
        /// <param name="value">The source <see cref="System.Windows.VerticalAlignment"/> value.</param>
        /// <param name="targetType">The target type (unused).</param>
        /// <param name="parameter">Optional. Pass <c>"Swap"</c> to swap Top and Bottom.</param>
        /// <param name="culture">The culture (unused).</param>
        /// <returns>The converted alignment value.</returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Windows.VerticalAlignment alignment)
            {
                if (parameter is string s && s.Equals("Swap", StringComparison.OrdinalIgnoreCase))
                    return SwapTopBottom(alignment);
                return alignment;
            }
            return System.Windows.VerticalAlignment.Center;
        }

        /// <summary>
        /// Converts the target value back to the source. When <paramref name="parameter"/> is <c>"Swap"</c>,
        /// swaps <see cref="System.Windows.VerticalAlignment.Top"/> and <see cref="System.Windows.VerticalAlignment.Bottom"/>.
        /// </summary>
        /// <param name="value">The target <see cref="System.Windows.VerticalAlignment"/> value.</param>
        /// <param name="targetType">The source type (unused).</param>
        /// <param name="parameter">Optional. Pass <c>"Swap"</c> to swap Top and Bottom.</param>
        /// <param name="culture">The culture (unused).</param>
        /// <returns>The converted alignment value.</returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is System.Windows.VerticalAlignment alignment)
            {
                if (parameter is string s && s.Equals("Swap", StringComparison.OrdinalIgnoreCase))
                    return SwapTopBottom(alignment);
                return alignment;
            }
            return System.Windows.VerticalAlignment.Center;
        }

        /// <summary>
        /// Swaps <see cref="System.Windows.VerticalAlignment.Top"/> and <see cref="System.Windows.VerticalAlignment.Bottom"/>;
        /// all other values pass through unchanged.
        /// </summary>
        /// <param name="alignment">The alignment to swap.</param>
        /// <returns>The swapped alignment value.</returns>
        private static System.Windows.VerticalAlignment SwapTopBottom(System.Windows.VerticalAlignment alignment)
        {
            return alignment switch
            {
                System.Windows.VerticalAlignment.Top => System.Windows.VerticalAlignment.Bottom,
                System.Windows.VerticalAlignment.Bottom => System.Windows.VerticalAlignment.Top,
                _ => alignment
            };
        }
    }
}
