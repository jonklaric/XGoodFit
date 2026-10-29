// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColumnSeries.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a series for clustered or stacked column charts.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Series
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using OxyPlot.Axes;

    /// <summary>
    /// Represents a series for clustered or stacked column charts (vertical bars).
    /// </summary>
    public class ColumnSeries : BarSeriesBase<ColumnItem>, IStackableSeries
    {
        /// <summary>
        /// The default tracker format string.
        /// </summary>
        public new const string DefaultTrackerFormatString = "{0}\n{1}: {2}";

        /// <summary>
        /// The default fill color.
        /// </summary>
        private OxyColor defaultFillColor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnSeries" /> class.
        /// </summary>
        public ColumnSeries()
        {
            this.FillColor = OxyColors.Automatic;
            this.NegativeFillColor = OxyColors.Undefined;
            this.TrackerFormatString = DefaultTrackerFormatString;
            this.LabelMargin = 2;
            this.LabelAngle = 0;
            this.StackGroup = string.Empty;
            this.StrokeThickness = 0;
            this.BaseValue = 0;
            this.BaseLine = double.NaN;
            this.ActualBaseLine = double.NaN;
            this.ColumnWidth = 1;
        }

        /// <summary>
        /// Gets or sets the width of the column.
        /// </summary>
        /// <value>The width of the column.</value>
        public double ColumnWidth { get; set; }

        /// <summary>
        /// Gets or sets the base value. Default value is 0.
        /// </summary>
        /// <value>The base value.</value>
        public double BaseValue { get; set; }

        /// <summary>
        /// Gets or sets the base value.
        /// </summary>
        /// <value>The base value.</value>
        public double BaseLine { get; set; }

        /// <summary>
        /// Gets or sets the actual base line.
        /// </summary>
        /// <returns>The actual base line.</returns>
        public double ActualBaseLine { get; protected set; }

        /// <summary>
        /// Gets the actual fill color.
        /// </summary>
        /// <value>The actual color.</value>
        public OxyColor ActualFillColor => this.FillColor.GetActualColor(this.defaultFillColor);

        /// <summary>
        /// Gets or sets the color field.
        /// </summary>
        public string ColorField { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the columns.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor FillColor { get; set; }

        /// <inheritdoc/>
        public bool IsStacked { get; set; }

        /// <inheritdoc/>
        public bool OverlapsStack { get; set; }

        /// <summary>
        /// Gets or sets the label format string.
        /// </summary>
        public string LabelFormatString { get; set; }

        /// <summary>
        /// Gets or sets the color of the interior of the columns when the value is negative.
        /// </summary>
        /// <value>The color.</value>
        public OxyColor NegativeFillColor { get; set; }

        /// <inheritdoc/>
        public string StackGroup { get; set; }

        /// <summary>
        /// Gets or sets the value field.
        /// </summary>
        public string ValueField { get; set; }

        /// <summary>
        /// Gets or sets the actual rectangles for the columns.
        /// </summary>
        protected IList<OxyRect> ActualBarRectangles { get; set; }

        /// <summary>
        /// Gets the category axis for this column series (X-axis).
        /// </summary>
        /// <remarks>
        /// For column series, categories are on the X-axis (horizontal).
        /// This explicitly implements the interface to override the base class behavior.
        /// </remarks>
        CategoryAxis IBarSeries.CategoryAxis => this.XAxis as CategoryAxis;

        /// <summary>
        /// Gets the value axis for this column series (Y-axis).
        /// </summary>
        /// <remarks>
        /// For column series, values are on the Y-axis (vertical).
        /// This explicitly implements the interface to override the base class behavior.
        /// </remarks>
        Axis IBarSeries.ValueAxis => this.YAxis;

        /// <summary>
        /// Gets the category axis.
        /// </summary>
        /// <returns>The category axis.</returns>
        protected new CategoryAxis GetCategoryAxis()
        {
            return this.XAxis as CategoryAxis;
        }

        /// <summary>
        /// Gets the actual width of the items of this series.
        /// </summary>
        /// <returns>The width.</returns>
        protected new double GetActualBarWidth()
        {
            var categoryAxis = this.GetCategoryAxis();
            return this.ColumnWidth / (1 + categoryAxis.GapWidth) / this.Manager.GetMaxWidth();
        }

        /// <inheritdoc/>
        public override TrackerHitResult GetNearestPoint(ScreenPoint point, bool interpolate)
        {
            if (this.ActualBarRectangles == null || this.ValidItems.Count == 0)
            {
                return null;
            }

            var i = 0;
            foreach (var rectangle in this.ActualBarRectangles)
            {
                if (rectangle.Contains(point))
                {
                    var item = this.ValidItems[i];
                    var categoryIndex = item.GetCategoryIndex(i);
                    var dp = new DataPoint(categoryIndex, this.ValidItems[i].Value);

                    var boundItem = this.GetItem(this.ValidItemsIndexInversion[i]);

                    return new TrackerHitResult
                    {
                        Series = this,
                        DataPoint = dp,
                        Position = point,
                        Item = boundItem,
                        Index = i,
                        Text = this.GetTrackerText(item, boundItem, categoryIndex)
                    };
                }

                i++;
            }

            return null;
        }

        /// <inheritdoc/>
        public override void RenderLegend(IRenderContext rc, OxyRect legendBox)
        {
            var xmid = (legendBox.Left + legendBox.Right) / 2;
            var ymid = (legendBox.Top + legendBox.Bottom) / 2;
            var height = (legendBox.Bottom - legendBox.Top) * 0.8;
            var width = height;
            rc.DrawRectangle(
                new OxyRect(xmid - (0.5 * width), ymid - (0.5 * height), width, height),
                this.GetSelectableColor(this.ActualFillColor),
                this.StrokeColor,
                this.StrokeThickness,
                this.EdgeRenderingMode);
        }

        /// <inheritdoc/>
        protected internal override void SetDefaultValues()
        {
            if (this.FillColor.IsAutomatic())
            {
                this.defaultFillColor = this.PlotModel.GetDefaultColor();
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateAxisMaxMin()
        {
            // For column series, values are on YAxis (not XAxis like horizontal bar series)
            this.YAxis.Include(this.MinY);
            this.YAxis.Include(this.MaxY);

            this.ComputeActualBaseLine();
            this.YAxis.Include(this.ActualBaseLine);
        }

        /// <summary>
        /// Computes the actual base value.
        /// </summary>
        protected void ComputeActualBaseLine()
        {
            if (double.IsNaN(this.BaseLine))
            {
                if (this.YAxis.IsLogarithmic())
                {
                    var lowestPositiveValue = this.ActualItems == null ? 1 : this.ActualItems.Select(p => p.Value).Where(v => v > 0).MinOrDefault(1);
                    this.ActualBaseLine = Math.Max(lowestPositiveValue / 10.0, this.BaseValue);
                }
                else
                {
                    this.ActualBaseLine = 0;
                }
            }
            else
            {
                this.ActualBaseLine = this.BaseLine;
            }
        }

        /// <inheritdoc/>
        protected internal override void UpdateMaxMin()
        {
            base.UpdateMaxMin();

            if (this.ValidItems.Count == 0)
            {
                return;
            }

            var categoryAxis = this.GetCategoryAxis();

            double minValue = double.MaxValue, maxValue = double.MinValue;
            if (this.IsStacked)
            {
                var labels = this.GetCategoryAxis().ActualLabels;
                for (var i = 0; i < labels.Count; i++)
                {
                    var values = this.ValidItems
                        .Select((item, index) => new { item, index })
                        .Where(x => x.item.GetCategoryIndex(x.index) == i)
                        .Select(x => x.item.Value)
                        .Concat(new[] { 0d }).ToList();
                    var minTemp = values.Where(v => v <= 0).Sum();
                    var maxTemp = values.Where(v => v >= 0).Sum();

                    var stackIndex = this.Manager.GetStackIndex(this.StackGroup);
                    var stackedMinValue = this.Manager.GetCurrentMinValue(stackIndex, i);
                    if (!double.IsNaN(stackedMinValue))
                    {
                        minTemp += stackedMinValue;
                    }

                    this.Manager.SetCurrentMinValue(stackIndex, i, minTemp);

                    var stackedMaxValue = this.Manager.GetCurrentMaxValue(stackIndex, i);
                    if (!this.OverlapsStack && !double.IsNaN(stackedMaxValue))
                    {
                        maxTemp += stackedMaxValue;
                    }

                    this.Manager.SetCurrentMaxValue(stackIndex, i, maxTemp);

                    minValue = Math.Min(minValue, minTemp + this.BaseValue);
                    maxValue = Math.Max(maxValue, maxTemp + this.BaseValue);
                }
            }
            else
            {
                var values = this.ValidItems.Select(item => item.Value).Concat(new[] { 0d }).ToList();
                minValue = values.Min();
                maxValue = values.Max();
                if (this.BaseValue < minValue)
                {
                    minValue = this.BaseValue;
                }

                if (this.BaseValue > maxValue)
                {
                    maxValue = this.BaseValue;
                }
            }

            this.MinY = minValue;
            this.MaxY = maxValue;
        }

        /// <summary>
        /// Gets the tracker text for the specified item.
        /// </summary>
        /// <param name="columnItem">The column item.</param>
        /// <param name="item">The bound item.</param>
        /// <param name="categoryIndex">The category index.</param>
        /// <returns>The tracker text.</returns>
        protected virtual string GetTrackerText(ColumnItem columnItem, object item, int categoryIndex)
        {
            var categoryAxis = this.GetCategoryAxis();
            var valueAxis = this.YAxis;

            return StringHelper.Format(
                this.ActualCulture,
                this.TrackerFormatString,
                item,
                this.Title,
                categoryAxis.FormatValue(categoryIndex),
                valueAxis.GetValue(columnItem.Value));
        }

        /// <inheritdoc/>
        protected override bool IsValid(ColumnItem item)
        {
            return this.YAxis.IsValidValue(item.Value);
        }

        /// <summary>
        /// Renders the column item.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="barValue">The end value of the column.</param>
        /// <param name="categoryValue">The category value.</param>
        /// <param name="actualBarWidth">The actual width of the column.</param>
        /// <param name="item">The item.</param>
        /// <param name="rect">The rectangle of the column.</param>
        protected virtual void RenderItem(
            IRenderContext rc,
            double barValue,
            double categoryValue,
            double actualBarWidth,
            ColumnItem item,
            OxyRect rect)
        {
            var actualFillColor = item.Color;
            if (actualFillColor.IsAutomatic())
            {
                actualFillColor = this.ActualFillColor;
                if (item.Value < 0 && !this.NegativeFillColor.IsUndefined())
                {
                    actualFillColor = this.NegativeFillColor;
                }
            }

            rc.DrawRectangle(
                rect,
                this.GetSelectableFillColor(actualFillColor),
                this.StrokeColor,
                this.StrokeThickness,
                this.EdgeRenderingMode.GetActual(EdgeRenderingMode.PreferSharpness));
        }

        /// <inheritdoc/>
        public override void Render(IRenderContext rc)
        {
            this.ActualBarRectangles = new List<OxyRect>();

            if (this.ValidItems.Count == 0)
            {
                return;
            }

            var actualBarWidth = this.GetActualBarWidth();
            var stackIndex = this.IsStacked ? this.Manager.GetStackIndex(this.StackGroup) : 0;

            for (var i = 0; i < this.ValidItems.Count; i++)
            {
                var item = this.ValidItems[i];
                var categoryIndex = this.ValidItems[i].GetCategoryIndex(i);

                var value = item.Value;

                // Get base- and topValue
                var baseValue = double.NaN;
                if (this.IsStacked && !this.OverlapsStack)
                {
                    baseValue = this.Manager.GetCurrentBaseValue(stackIndex, categoryIndex, value < 0);
                }

                if (double.IsNaN(baseValue))
                {
                    baseValue = this.BaseValue;
                }

                var topValue = this.IsStacked ? baseValue + value : value;

                if (this.YAxis.IsLogarithmic() && !this.YAxis.IsValidValue(topValue))
                {
                    continue;
                }

                // Calculate offset
                double categoryValue;
                if (this.IsStacked)
                {
                    categoryValue = this.Manager.GetCategoryValue(categoryIndex, stackIndex, actualBarWidth);
                }
                else
                {
                    categoryValue = categoryIndex - 0.5 + this.Manager.GetCurrentBarOffset(categoryIndex);
                }

                if (this.IsStacked)
                {
                    this.Manager.SetCurrentBaseValue(stackIndex, categoryIndex, value < 0, topValue);
                }

                var clampBase = this.YAxis.IsLogarithmic() && !this.YAxis.IsValidValue(baseValue);

                // For columns: X is category, Y is value
                var p1 = this.Transform(categoryValue, clampBase ? this.YAxis.ClipMinimum : baseValue);
                var p2 = this.Transform(categoryValue + actualBarWidth, topValue);

                var rectangle = new OxyRect(p1, p2);

                this.ActualBarRectangles.Add(rectangle);

                this.RenderItem(rc, topValue, categoryValue, actualBarWidth, item, rectangle);

                if (this.LabelFormatString != null)
                {
                    this.RenderColumnLabel(
                        rc,
                        item,
                        baseValue,
                        topValue,
                        categoryValue,
                        categoryValue + actualBarWidth);
                }

                if (!this.IsStacked)
                {
                    this.Manager.IncreaseCurrentBarOffset(categoryIndex, actualBarWidth);
                }
            }
        }

        /// <summary>
        /// Renders the column label.
        /// </summary>
        /// <param name="rc">The render context.</param>
        /// <param name="item">The item.</param>
        /// <param name="baseValue">The base value.</param>
        /// <param name="topValue">The top value.</param>
        /// <param name="categoryValue">The category value.</param>
        /// <param name="categoryEndValue">The category end value.</param>
        protected void RenderColumnLabel(
            IRenderContext rc,
            ColumnItem item,
            double baseValue,
            double topValue,
            double categoryValue,
            double categoryEndValue)
        {
            var s = StringHelper.Format(this.ActualCulture, this.LabelFormatString, item, item.Value);
            ScreenPoint pt;
            var x = (categoryEndValue + categoryValue) / 2;
            var sign = Math.Sign(topValue - baseValue);
            var marginVector = new ScreenVector(0, this.LabelMargin) * sign;

            var size = rc.MeasureText(
                s,
                this.ActualFont,
                this.ActualFontSize,
                this.ActualFontWeight,
                this.LabelAngle);
            var halfHeight = size.Height / 2;

            switch (this.LabelPlacement)
            {
                case LabelPlacement.Inside:
                    pt = this.Transform(x, topValue);
                    pt = new ScreenPoint(pt.X, pt.Y + sign * (this.LabelMargin + halfHeight));
                    break;
                case LabelPlacement.Outside:
                    pt = this.Transform(x, topValue);
                    pt = new ScreenPoint(pt.X, pt.Y - sign * (this.LabelMargin + halfHeight));
                    break;
                case LabelPlacement.Middle:
                    pt = this.Transform(x, (topValue + baseValue) / 2);
                    break;
                case LabelPlacement.Base:
                    pt = this.Transform(x, baseValue);
                    pt = new ScreenPoint(pt.X, pt.Y - sign * (this.LabelMargin + halfHeight));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            rc.DrawText(
                pt,
                s,
                this.ActualTextColor,
                this.ActualFont,
                this.ActualFontSize,
                this.ActualFontWeight,
                this.LabelAngle,
                HorizontalAlignment.Center,
                VerticalAlignment.Middle);
        }

        /// <inheritdoc/>
        protected override bool UpdateFromDataFields()
        {
            if (this.ValueField == null)
            {
                return false;
            }

            var filler = new ListBuilder<ColumnItem>();
            filler.Add(this.ValueField, double.NaN);
            filler.Add(this.ColorField, OxyColors.Automatic);
            filler.Fill(
                this.ItemsSourceItems,
                this.ItemsSource,
                args => new ColumnItem(Convert.ToDouble(args[0])) { Color = (OxyColor)args[1] });

            return true;
        }
    }
}
