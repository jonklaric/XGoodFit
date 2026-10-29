using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// Provides extension methods for OxyPlot types and data conversion utilities.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Gets the first abstract base type in the inheritance hierarchy.
        /// </summary>
        /// <param name="type">The type to search from.</param>
        /// <returns>The first abstract base type, or null if none found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type is null.</exception>
        public static Type? GetFirstAbstractBaseType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            Type? baseType = type.BaseType;
            if (baseType == null || baseType.IsAbstract) return baseType;

            return baseType.GetFirstAbstractBaseType();
        }

        /// <summary>
        /// Copies a binding from one dependency object to another.
        /// </summary>
        /// <param name="fromTarget">The source dependency object.</param>
        /// <param name="toTarget">The target dependency object.</param>
        /// <param name="dp">The dependency property to copy the binding for.</param>
        /// <returns>True if a binding was found and copied; otherwise, false.</returns>
        public static bool CopyBinding(this DependencyObject fromTarget, DependencyObject toTarget, DependencyProperty dp)
        {
            if (fromTarget == null || toTarget == null || dp == null) return false;
            var te = BindingOperations.GetBinding(fromTarget, dp);
            if (te == null) return false;
            try
            {
                BindingOperations.SetBinding(toTarget, dp, te);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a dependency property has a binding.
        /// </summary>
        /// <param name="target">The dependency object to check.</param>
        /// <param name="dp">The dependency property to check.</param>
        /// <returns>True if the property has a binding; otherwise, false.</returns>
        public static bool IsBound(this DependencyObject target, DependencyProperty dp)
        {
            return BindingOperations.GetBinding(target, dp) != null;
        }

        /// <summary>
        /// Copies axis properties from one axis to another, respecting bindings.
        /// </summary>
        /// <param name="toAxis">The target axis to copy properties to.</param>
        /// <param name="fromAxis">The source axis to copy properties from.</param>
        /// <param name="ignoreToAxisBound">If true, skip properties that already have bindings on the target axis.</param>
        public static void FromAxisProperties(this Wpf.Axis toAxis, Wpf.Axis fromAxis, bool ignoreToAxisBound = true)
        {
            if (ignoreToAxisBound)
            {
                if (toAxis.IsBound(Wpf.Axis.AbsoluteMaximumProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AbsoluteMaximumProperty) == false) toAxis.AbsoluteMaximum = fromAxis.AbsoluteMaximum;
                if (toAxis.IsBound(Wpf.Axis.AbsoluteMinimumProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AbsoluteMinimumProperty) == false) toAxis.AbsoluteMinimum = fromAxis.AbsoluteMinimum;
                if (toAxis.IsBound(Wpf.Axis.AngleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AngleProperty) == false) toAxis.Angle = fromAxis.Angle;
                if (toAxis.IsBound(Wpf.Axis.AxisDistanceProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisDistanceProperty) == false) toAxis.AxisDistance = fromAxis.AxisDistance;
                if (toAxis.IsBound(Wpf.Axis.AxislineColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineColorProperty) == false) toAxis.AxislineColor = fromAxis.AxislineColor;
                if (toAxis.IsBound(Wpf.Axis.AxislineStyleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineStyleProperty) == false) toAxis.AxislineStyle = fromAxis.AxislineStyle;
                if (toAxis.IsBound(Wpf.Axis.AxislineThicknessProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineThicknessProperty) == false) toAxis.AxislineThickness = fromAxis.AxislineThickness;
                if (toAxis.IsBound(Wpf.Axis.AxisTitleDistanceProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisTitleDistanceProperty) == false) toAxis.AxisTitleDistance = fromAxis.AxisTitleDistance;
                if (toAxis.IsBound(Wpf.Axis.AxisTickToLabelDistanceProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisTickToLabelDistanceProperty) == false) toAxis.AxisTickToLabelDistance = fromAxis.AxisTickToLabelDistance;
                if (toAxis.IsBound(Wpf.Axis.ClipTitleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ClipTitleProperty) == false) toAxis.ClipTitle = fromAxis.ClipTitle;
                if (toAxis.IsBound(Wpf.Axis.EndPositionProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.EndPositionProperty) == false) toAxis.EndPosition = fromAxis.EndPosition;
                if (toAxis.IsBound(Wpf.Axis.ExtraGridlineColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineColorProperty) == false) toAxis.ExtraGridlineColor = fromAxis.ExtraGridlineColor;
                if (toAxis.IsBound(Wpf.Axis.ExtraGridlineStyleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineStyleProperty) == false) toAxis.ExtraGridlineStyle = fromAxis.ExtraGridlineStyle;
                if (toAxis.IsBound(Wpf.Axis.ExtraGridlineThicknessProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineThicknessProperty) == false) toAxis.ExtraGridlineThickness = fromAxis.ExtraGridlineThickness;
                if (toAxis.IsBound(Wpf.Axis.ExtraGridlinesProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlinesProperty) == false) toAxis.ExtraGridlines = fromAxis.ExtraGridlines;
                if (toAxis.IsBound(Wpf.Axis.FilterFunctionProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterFunctionProperty) == false) toAxis.FilterFunction = fromAxis.FilterFunction;
                if (toAxis.IsBound(Wpf.Axis.FilterMaxValueProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterMaxValueProperty) == false) toAxis.FilterMaxValue = fromAxis.FilterMaxValue;
                if (toAxis.IsBound(Wpf.Axis.FilterMinValueProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterMinValueProperty) == false) toAxis.FilterMinValue = fromAxis.FilterMinValue;
                if (toAxis.IsBound(Wpf.Axis.FontProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FontProperty) == false) toAxis.Font = fromAxis.Font;
                if (toAxis.IsBound(Wpf.Axis.FontSizeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FontSizeProperty) == false) toAxis.FontSize = fromAxis.FontSize;
                if (toAxis.IsBound(Wpf.Axis.FontWeightProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.FontWeightProperty) == false) toAxis.FontWeight = fromAxis.FontWeight;
                if (toAxis.IsBound(Wpf.Axis.IntervalLengthProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.IntervalLengthProperty) == false) toAxis.IntervalLength = fromAxis.IntervalLength;
                if (toAxis.IsBound(Wpf.Axis.IsPanEnabledProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.IsPanEnabledProperty) == false) toAxis.IsPanEnabled = fromAxis.IsPanEnabled;
                if (toAxis.IsBound(Wpf.Axis.IsAxisVisibleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.IsAxisVisibleProperty) == false) toAxis.IsAxisVisible = fromAxis.IsAxisVisible;
                if (toAxis.IsBound(Wpf.Axis.IsZoomEnabledProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.IsZoomEnabledProperty) == false) toAxis.IsZoomEnabled = fromAxis.IsZoomEnabled;
                if (toAxis.IsBound(Wpf.Axis.KeyProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.KeyProperty) == false) toAxis.Key = fromAxis.Key;
                if (toAxis.IsBound(Wpf.Axis.LayerProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.LayerProperty) == false) toAxis.Layer = fromAxis.Layer;
                if (toAxis.IsBound(Wpf.Axis.MajorGridlineColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineColorProperty) == false) toAxis.MajorGridlineColor = fromAxis.MajorGridlineColor;
                if (toAxis.IsBound(Wpf.Axis.MinorGridlineColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineColorProperty) == false) toAxis.MinorGridlineColor = fromAxis.MinorGridlineColor;
                if (toAxis.IsBound(Wpf.Axis.MajorGridlineStyleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineStyleProperty) == false) toAxis.MajorGridlineStyle = fromAxis.MajorGridlineStyle;
                if (toAxis.IsBound(Wpf.Axis.MinorGridlineStyleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineStyleProperty) == false) toAxis.MinorGridlineStyle = fromAxis.MinorGridlineStyle;
                if (toAxis.IsBound(Wpf.Axis.MajorGridlineThicknessProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineThicknessProperty) == false) toAxis.MajorGridlineThickness = fromAxis.MajorGridlineThickness;
                if (toAxis.IsBound(Wpf.Axis.MinorGridlineThicknessProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineThicknessProperty) == false) toAxis.MinorGridlineThickness = fromAxis.MinorGridlineThickness;
                if (toAxis.IsBound(Wpf.Axis.MajorStepProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorStepProperty) == false) toAxis.MajorStep = fromAxis.MajorStep;
                if (toAxis.IsBound(Wpf.Axis.MajorTickSizeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorTickSizeProperty) == false) toAxis.MajorTickSize = fromAxis.MajorTickSize;
                if (toAxis.IsBound(Wpf.Axis.MinorStepProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorStepProperty) == false) toAxis.MinorStep = fromAxis.MinorStep;
                if (toAxis.IsBound(Wpf.Axis.MinorTickSizeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorTickSizeProperty) == false) toAxis.MinorTickSize = fromAxis.MinorTickSize;
                if (toAxis.IsBound(Wpf.Axis.MinimumProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumProperty) == false) toAxis.Minimum = fromAxis.Minimum;
                if (toAxis.IsBound(Wpf.Axis.MaximumProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumProperty) == false) toAxis.Maximum = fromAxis.Maximum;
                if (toAxis.IsBound(Wpf.Axis.MinimumRangeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumRangeProperty) == false) toAxis.MinimumRange = fromAxis.MinimumRange;
                if (toAxis.IsBound(Wpf.Axis.MaximumRangeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumRangeProperty) == false) toAxis.MaximumRange = fromAxis.MaximumRange;
                if (toAxis.IsBound(Wpf.Axis.MinimumPaddingProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumPaddingProperty) == false) toAxis.MinimumPadding = fromAxis.MinimumPadding;
                if (toAxis.IsBound(Wpf.Axis.MaximumPaddingProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumPaddingProperty) == false) toAxis.MaximumPadding = fromAxis.MaximumPadding;
                if (toAxis.IsBound(Wpf.Axis.PositionProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionProperty) == false) toAxis.Position = fromAxis.Position;
                if (toAxis.IsBound(Wpf.Axis.PositionTierProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionTierProperty) == false) toAxis.PositionTier = fromAxis.PositionTier;
                if (toAxis.IsBound(Wpf.Axis.PositionAtZeroCrossingProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionAtZeroCrossingProperty) == false) toAxis.PositionAtZeroCrossing = fromAxis.PositionAtZeroCrossing;
                if (toAxis.IsBound(Wpf.Axis.StartPositionProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.StartPositionProperty) == false) toAxis.StartPosition = fromAxis.StartPosition;
                if (toAxis.IsBound(Wpf.Axis.StringFormatProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.StringFormatProperty) == false) toAxis.StringFormat = fromAxis.StringFormat;
                if (toAxis.IsBound(Wpf.Axis.TextColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TextColorProperty) == false) toAxis.TextColor = fromAxis.TextColor;
                if (toAxis.IsBound(Wpf.Axis.TicklineColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TicklineColorProperty) == false) toAxis.TicklineColor = fromAxis.TicklineColor;
                if (toAxis.IsBound(Wpf.Axis.TitleClippingLengthProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleClippingLengthProperty) == false) toAxis.TitleClippingLength = fromAxis.TitleClippingLength;
                if (toAxis.IsBound(Wpf.Axis.TitleColorProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleColorProperty) == false) toAxis.TitleColor = fromAxis.TitleColor;
                if (toAxis.IsBound(Wpf.Axis.TitleFontProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontProperty) == false) toAxis.TitleFont = fromAxis.TitleFont;
                if (toAxis.IsBound(Wpf.Axis.TitleFontSizeProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontSizeProperty) == false) toAxis.TitleFontSize = fromAxis.TitleFontSize;
                if (toAxis.IsBound(Wpf.Axis.TitleFontWeightProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontWeightProperty) == false) toAxis.TitleFontWeight = fromAxis.TitleFontWeight;
                if (toAxis.IsBound(Wpf.Axis.TitleFormatStringProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFormatStringProperty) == false) toAxis.TitleFormatString = fromAxis.TitleFormatString;
                if (toAxis.IsBound(Wpf.Axis.TitleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleProperty) == false) toAxis.Title = fromAxis.Title;
                if (toAxis.IsBound(Wpf.Axis.ToolTipProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.ToolTipProperty) == false) toAxis.ToolTip = fromAxis.ToolTip == null ? null : fromAxis.ToolTip.ToString();
                if (toAxis.IsBound(Wpf.Axis.TickStyleProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TickStyleProperty) == false) toAxis.TickStyle = fromAxis.TickStyle;
                if (toAxis.IsBound(Wpf.Axis.TitlePositionProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.TitlePositionProperty) == false) toAxis.TitlePosition = fromAxis.TitlePosition;
                if (toAxis.IsBound(Wpf.Axis.UnitProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.UnitProperty) == false) toAxis.Unit = fromAxis.Unit;
                if (toAxis.IsBound(Wpf.Axis.UseSuperExponentialFormatProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.UseSuperExponentialFormatProperty) == false) toAxis.UseSuperExponentialFormat = fromAxis.UseSuperExponentialFormat;
                if (toAxis.IsBound(Wpf.Axis.LabelFormatterProperty) == false && fromAxis.CopyBinding(toAxis, Wpf.Axis.LabelFormatterProperty) == false) toAxis.LabelFormatter = fromAxis.LabelFormatter;
            }
            else
            {
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AbsoluteMaximumProperty) == false) toAxis.AbsoluteMaximum = fromAxis.AbsoluteMaximum;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AbsoluteMinimumProperty) == false) toAxis.AbsoluteMinimum = fromAxis.AbsoluteMinimum;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AngleProperty) == false) toAxis.Angle = fromAxis.Angle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisDistanceProperty) == false) toAxis.AxisDistance = fromAxis.AxisDistance;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineColorProperty) == false) toAxis.AxislineColor = fromAxis.AxislineColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineStyleProperty) == false) toAxis.AxislineStyle = fromAxis.AxislineStyle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxislineThicknessProperty) == false) toAxis.AxislineThickness = fromAxis.AxislineThickness;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisTitleDistanceProperty) == false) toAxis.AxisTitleDistance = fromAxis.AxisTitleDistance;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.AxisTickToLabelDistanceProperty) == false) toAxis.AxisTickToLabelDistance = fromAxis.AxisTickToLabelDistance;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ClipTitleProperty) == false) toAxis.ClipTitle = fromAxis.ClipTitle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.EndPositionProperty) == false) toAxis.EndPosition = fromAxis.EndPosition;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineColorProperty) == false) toAxis.ExtraGridlineColor = fromAxis.ExtraGridlineColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineStyleProperty) == false) toAxis.ExtraGridlineStyle = fromAxis.ExtraGridlineStyle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlineThicknessProperty) == false) toAxis.ExtraGridlineThickness = fromAxis.ExtraGridlineThickness;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ExtraGridlinesProperty) == false) toAxis.ExtraGridlines = fromAxis.ExtraGridlines;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterFunctionProperty) == false) toAxis.FilterFunction = fromAxis.FilterFunction;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterMaxValueProperty) == false) toAxis.FilterMaxValue = fromAxis.FilterMaxValue;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FilterMinValueProperty) == false) toAxis.FilterMinValue = fromAxis.FilterMinValue;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FontProperty) == false) toAxis.Font = fromAxis.Font;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FontSizeProperty) == false) toAxis.FontSize = fromAxis.FontSize;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.FontWeightProperty) == false) toAxis.FontWeight = fromAxis.FontWeight;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.IntervalLengthProperty) == false) toAxis.IntervalLength = fromAxis.IntervalLength;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.IsPanEnabledProperty) == false) toAxis.IsPanEnabled = fromAxis.IsPanEnabled;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.IsAxisVisibleProperty) == false) toAxis.IsAxisVisible = fromAxis.IsAxisVisible;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.IsZoomEnabledProperty) == false) toAxis.IsZoomEnabled = fromAxis.IsZoomEnabled;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.KeyProperty) == false) toAxis.Key = fromAxis.Key;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.LayerProperty) == false) toAxis.Layer = fromAxis.Layer;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineColorProperty) == false) toAxis.MajorGridlineColor = fromAxis.MajorGridlineColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineColorProperty) == false) toAxis.MinorGridlineColor = fromAxis.MinorGridlineColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineStyleProperty) == false) toAxis.MajorGridlineStyle = fromAxis.MajorGridlineStyle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineStyleProperty) == false) toAxis.MinorGridlineStyle = fromAxis.MinorGridlineStyle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorGridlineThicknessProperty) == false) toAxis.MajorGridlineThickness = fromAxis.MajorGridlineThickness;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorGridlineThicknessProperty) == false) toAxis.MinorGridlineThickness = fromAxis.MinorGridlineThickness;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorStepProperty) == false) toAxis.MajorStep = fromAxis.MajorStep;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MajorTickSizeProperty) == false) toAxis.MajorTickSize = fromAxis.MajorTickSize;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorStepProperty) == false) toAxis.MinorStep = fromAxis.MinorStep;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinorTickSizeProperty) == false) toAxis.MinorTickSize = fromAxis.MinorTickSize;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumProperty) == false) toAxis.Minimum = fromAxis.Minimum;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumProperty) == false) toAxis.Maximum = fromAxis.Maximum;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumRangeProperty) == false) toAxis.MinimumRange = fromAxis.MinimumRange;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumRangeProperty) == false) toAxis.MaximumRange = fromAxis.MaximumRange;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MinimumPaddingProperty) == false) toAxis.MinimumPadding = fromAxis.MinimumPadding;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.MaximumPaddingProperty) == false) toAxis.MaximumPadding = fromAxis.MaximumPadding;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionProperty) == false) toAxis.Position = fromAxis.Position;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionTierProperty) == false) toAxis.PositionTier = fromAxis.PositionTier;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.PositionAtZeroCrossingProperty) == false) toAxis.PositionAtZeroCrossing = fromAxis.PositionAtZeroCrossing;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.StartPositionProperty) == false) toAxis.StartPosition = fromAxis.StartPosition;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.StringFormatProperty) == false) toAxis.StringFormat = fromAxis.StringFormat;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TextColorProperty) == false) toAxis.TextColor = fromAxis.TextColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TicklineColorProperty) == false) toAxis.TicklineColor = fromAxis.TicklineColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleClippingLengthProperty) == false) toAxis.TitleClippingLength = fromAxis.TitleClippingLength;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleColorProperty) == false) toAxis.TitleColor = fromAxis.TitleColor;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontProperty) == false) toAxis.TitleFont = fromAxis.TitleFont;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontSizeProperty) == false) toAxis.TitleFontSize = fromAxis.TitleFontSize;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFontWeightProperty) == false) toAxis.TitleFontWeight = fromAxis.TitleFontWeight;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleFormatStringProperty) == false) toAxis.TitleFormatString = fromAxis.TitleFormatString;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitleProperty) == false) toAxis.Title = fromAxis.Title;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.ToolTipProperty) == false) toAxis.ToolTip = fromAxis.ToolTip == null ? null : fromAxis.ToolTip.ToString();
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TickStyleProperty) == false) toAxis.TickStyle = fromAxis.TickStyle;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.TitlePositionProperty) == false) toAxis.TitlePosition = fromAxis.TitlePosition;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.UnitProperty) == false) toAxis.Unit = fromAxis.Unit;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.UseSuperExponentialFormatProperty) == false) toAxis.UseSuperExponentialFormat = fromAxis.UseSuperExponentialFormat;
                if (fromAxis.CopyBinding(toAxis, Wpf.Axis.LabelFormatterProperty) == false) toAxis.LabelFormatter = fromAxis.LabelFormatter;
            }
        }
    }

    /// <summary>
    /// Converts binding expressions to markup extensions for serialization.
    /// </summary>
    public class BindingConvertor : ExpressionConverter
    {
        /// <summary>
        /// Determines whether this converter can convert to the specified type.
        /// </summary>
        /// <param name="context">The type descriptor context.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>True if the destination type is MarkupExtension; otherwise, false.</returns>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a binding expression to a markup extension.
        /// </summary>
        /// <param name="context">The type descriptor context.</param>
        /// <param name="culture">The culture info.</param>
        /// <param name="value">The value to convert.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The parent binding of the binding expression.</returns>
        /// <exception cref="Exception">Thrown when value is not a BindingExpression.</exception>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(MarkupExtension))
            {
                BindingExpression? bindingExpression = value as BindingExpression;
                if (bindingExpression == null) throw new InvalidOperationException("Value must be a BindingExpression");
                return bindingExpression.ParentBinding;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    /// <summary>
    /// Provides helper methods for registering type converters with the type descriptor system.
    /// </summary>
    public static class EditorHelper
    {
        /// <summary>
        /// Registers a type converter for a specific type.
        /// </summary>
        /// <typeparam name="T">The type to register the converter for.</typeparam>
        /// <typeparam name="TC">The type converter to register.</typeparam>
        public static void Register<T, TC>()
        {
            Attribute[] attr = new Attribute[1];
            TypeConverterAttribute vConv = new TypeConverterAttribute(typeof(TC));
            attr[0] = vConv;
            TypeDescriptor.AddAttributes(typeof(T), attr);
        }
    }
}
