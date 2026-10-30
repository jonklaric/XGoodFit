// This file also builds with the WPF fork (tools/OxyPlotFixtures.Wpf defines WPF), to write the
// fixtures the port's serialization is compared with. The aliases give both the same names.
#if WPF
using System.Windows;
using System.Windows.Media;
using FontWeight = System.Windows.FontWeights;
using ItemAlignment = System.Windows.HorizontalAlignment;
#else
using Avalonia.Media;
using WpfCompat;
using ItemAlignment = Avalonia.Layout.HorizontalAlignment;
using Thickness = Avalonia.Thickness;
#endif
using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace OxyPlot.Wpf.Tests;

/// <summary>
/// The plots the tests draw and save: a frequency plot like BestFit's, with a normal probability axis,
/// a fitted line, observed points and annotations.
/// </summary>
internal static class TestPlots
{
    /// <summary>The fitted line's colour, which the rendering tests look for.</summary>
    public static readonly Color FittedColor = Color.FromRgb(0xE0, 0x10, 0x10);

    /// <summary>Builds the frequency plot.</summary>
    public static Plot FrequencyPlot()
    {
        var plot = new Plot
        {
            Title = "Annual peak flow",
            Subtitle = "Gauge 01234500",
            TitleColor = Color.FromRgb(0x20, 0x30, 0x40),
            PlotAreaBorderThickness = new Thickness(1, 0, 0, 1),
            LegendPosition = LegendPosition.TopLeft,
            LegendFontWeight = FontWeight.SemiBold,
        };

        plot.Axes.Add(new NormalProbabilityAxis { Key = "x", Position = AxisPosition.Bottom });
        plot.Axes.Add(new LinearAxis
        {
            Key = "y",
            Position = AxisPosition.Left,
            Title = "Flow (cfs)",
            Minimum = 0,
            Maximum = 1000,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = Color.FromArgb(0x40, 0, 0, 0),
        });

        var fitted = Enumerable.Range(1, 99).Select(i => new DataPoint(i / 100.0, 100 + (8 * i))).ToList();
        plot.Series.Add(new LineSeries
        {
            Title = "Fitted",
            Color = FittedColor,
            StrokeThickness = 3,
            TrackerFormatString = "{0}: {4:0} cfs",
            ItemsSource = fitted,
            XAxisKey = "x",
            YAxisKey = "y",
        });
        plot.Series.Add(new ScatterSeries
        {
            Title = "Observed",
            MarkerType = MarkerType.Circle,
            MarkerFill = Colors.SteelBlue,
            ItemsSource = new[] { new ScatterPoint(0.1, 150), new ScatterPoint(0.5, 520), new ScatterPoint(0.9, 840) },
            XAxisKey = "x",
            YAxisKey = "y",
        });
        plot.Series.Add(new LineSeries
        {
            Title = "Hidden",
            Visibility = Visibility.Hidden,
            LineStyle = LineStyle.Dot,
            ItemsSource = fitted,
            XAxisKey = "x",
            YAxisKey = "y",
        });

        plot.Annotations.Add(new LineAnnotation
        {
            Type = LineAnnotationType.Vertical,
            X = 0.99,
            Text = "100-year",
            Color = Colors.DarkGreen,
            XAxisKey = "x",
            YAxisKey = "y",
        });
        plot.Annotations.Add(new TextAnnotation
        {
            Text = "Note α",
            TextPosition = new DataPoint(0.5, 900),
            Stroke = Colors.Gray,
            XAxisKey = "x",
            YAxisKey = "y",
        });

        return plot;
    }

    /// <summary>
    /// Builds a plot with one of every axis, series and annotation type the serializers read back, each
    /// with some settings changed from their defaults (colours especially, whose text forms differ most
    /// between WPF and Avalonia). It isn't drawn, so it has no data.
    /// </summary>
    public static Plot EveryElement()
    {
        var plot = new Plot
        {
            Title = "Every element",
            TextColor = Color.FromRgb(0x10, 0x10, 0x10),
            PlotAreaBorderColor = Colors.Gray,
            IsLegendVisible = false,
            LegendBackground = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF),
            LegendTitle = "Legend",
            LegendItemAlignment = ItemAlignment.Center,
            Padding = new Thickness(4, 6, 4, 6),
        };

        plot.Axes.Add(new LinearAxis { Key = "linear", Position = AxisPosition.Left, Title = "Linear", AxislineStyle = LineStyle.Solid, AxislineColor = Colors.Navy, FormatAsFractions = true });
        plot.Axes.Add(new CategoryAxis { Key = "category", Position = AxisPosition.Bottom, Title = "Category", IsTickCentered = true, TicklineColor = Colors.Teal });
        plot.Axes.Add(new LogarithmicAxis { Key = "log", Position = AxisPosition.Right, Title = "Log", Base = 10, MinorGridlineStyle = LineStyle.Dot, MinorGridlineColor = Colors.Silver });
        plot.Axes.Add(new DateTimeAxis { Key = "date", Position = AxisPosition.Top, StringFormat = "yyyy", TitleColor = Colors.Maroon });
        plot.Axes.Add(new TimeSpanAxis { Key = "span", Position = AxisPosition.Top, Unit = "h" });
        plot.Axes.Add(new AngleAxis { Key = "angle", StartAngle = 90, EndAngle = 450, MajorGridlineColor = Colors.Olive });
        plot.Axes.Add(new MagnitudeAxis { Key = "magnitude", TextColor = Colors.Purple });
        plot.Axes.Add(new LinearColorAxis { Key = "linearColor", HighColor = Colors.Red, LowColor = Colors.Blue, InvalidNumberColor = Colors.Black, PaletteSize = 16 });
        plot.Axes.Add(new NormalProbabilityAxis { Key = "normal", Position = AxisPosition.Bottom, ExtraGridlines = new[] { 0.5, 0.99 }, ExtraGridlineColor = Colors.Orange });
        plot.Axes.Add(new GumbelProbabilityAxis { Key = "gumbel", Position = AxisPosition.Bottom, TitleFontWeight = FontWeight.Normal });
        plot.Axes.Add(new CategoryColorAxis { Key = "categoryColor", InvalidCategoryColor = Colors.Pink });
        plot.Axes.Add(new LogarithmicColorAxis { Key = "logColor", HighColor = Colors.Yellow, LowColor = Colors.Green, InvalidNumberColor = Colors.White, PaletteSize = 8 });
        plot.Axes.Add(new RangeColorAxis { Key = "rangeColor", HighColor = Colors.Cyan, LowColor = Colors.Magenta, InvalidNumberColor = Colors.Brown });
        plot.Axes.Add(new AngleAxisFullPlotArea { Key = "angleFull", MinorGridlineColor = Colors.Coral });
        plot.Axes.Add(new MagnitudeAxisFullPlotArea { Key = "magnitudeFull", AxislineColor = Colors.Indigo });

        plot.Series.Add(new HeatMapSeries { Title = "HeatMap", X0 = 1, X1 = 2, Y0 = 3, Y1 = 4, HighColor = Colors.Red, LowColor = Colors.Blue, Interpolate = false });
        plot.Series.Add(new LineSeries { Title = "Line", MarkerType = MarkerType.Square, MarkerFill = Colors.Gold, MarkerStroke = Colors.Black, BrokenLineColor = Colors.Gray, LineJoin = LineJoin.Round });
        plot.Series.Add(new ColumnSeries { Title = "Column", FillColor = Colors.SkyBlue, StrokeColor = Colors.Navy, NegativeFillColor = Colors.Tomato, ColumnWidth = 0.5 });
        plot.Series.Add(new BarSeries { Title = "Bar", FillColor = Colors.Khaki, BarWidth = 0.75, IsStacked = true, StackGroup = "g" });
        plot.Series.Add(new HistogramSeries { Title = "Histogram", FillColor = Colors.Plum, StrokeColor = Colors.Purple, NegativeFillColor = Colors.Linen, StrokeThickness = 2 });
        plot.Series.Add(new ScatterPointSeries { Title = "ScatterPoint", MarkerFill = Colors.Lime, MarkerStroke = Colors.DarkGreen, MarkerType = MarkerType.Triangle });
        plot.Series.Add(new ScatterErrorSeries { Title = "ScatterError", ErrorBarColor = Colors.Crimson, MarkerFill = Colors.Wheat, ErrorBarStopWidth = 3 });
        plot.Series.Add(new AreaSeries { Title = "Area", Color2 = Colors.Sienna, Fill = Colors.Beige, Reverse2 = false });
        plot.Series.Add(new BoxPlotSeries { Title = "BoxPlot", Stroke = Colors.DimGray, Fill = Colors.LightGray, WhiskerWidth = 0.3, ShowMedianAsDot = true });
        plot.Series.Add(new HighLowSeries { Title = "HighLow", Color = Colors.SaddleBrown, TickLength = 6 });
        plot.Series.Add(new CandleStickSeries { Title = "CandleStick", IncreasingColor = Colors.ForestGreen, DecreasingColor = Colors.Firebrick, CandleWidth = 0.6 });
        plot.Series.Add(new PieSeries { Title = "Pie", Stroke = Colors.White, InsideLabelColor = Colors.Black, Diameter = 0.8, StartAngle = 45 });
        plot.Series.Add(new ContourSeries { Title = "Contour", Color = Colors.DarkCyan, LabelBackground = Colors.Ivory, ContourLevelStep = 0.5 });
        plot.Series.Add(new VectorSeries { Title = "Vector", Color = Colors.DarkSlateBlue, ArrowHeadLength = 4 });
        plot.Series.Add(new RectangleSeries { Title = "Rectangle", ColorAxisKey = "linearColor", LabelFontSize = 10 });
        plot.Series.Add(new TwoColorLineSeries { Title = "TwoColorLine", Color = Colors.Red, Color2 = Colors.Blue, Limit = 5, LineStyle2 = LineStyle.Dash });
        plot.Series.Add(new ThreeColorLineSeries { Title = "ThreeColorLine", ColorLo = Colors.Blue, ColorHi = Colors.Red, LimitLo = -1, LimitHi = 1, LineStyleHi = LineStyle.Dot });
        plot.Series.Add(new StairStepSeries { Title = "StairStep", VerticalLineStyle = LineStyle.Dash, VerticalStrokeThickness = 2 });
        plot.Series.Add(new StemSeries { Title = "Stem", Base = 1.5 });
        plot.Series.Add(new ExtrapolationLineSeries { Title = "Extrapolation", ExtrapolationColor = Colors.Orchid, ExtrapolationLineStyle = LineStyle.LongDash, IgnoreExtrapolationForScaling = true });
        plot.Series.Add(new ScatterSeries { Title = "Scatter", MarkerFill = Colors.Aqua, MarkerSize = 7, BinSize = 2 });

        plot.Annotations.Add(new ArrowAnnotation { Text = "Arrow", Color = Colors.Red, StartPoint = new DataPoint(0, 0), EndPoint = new DataPoint(1, 1), HeadLength = 12 });
        plot.Annotations.Add(new TextAnnotation { Text = "Text", Background = Colors.LightYellow, Stroke = Colors.Black, StrokeThickness = 2, TextPosition = new DataPoint(2, 3) });
        plot.Annotations.Add(new EllipseAnnotation { Text = "Ellipse", Fill = Colors.LightBlue, Stroke = Colors.Blue, X = 1, Y = 2, Width = 3, Height = 4 });
        plot.Annotations.Add(new RectangleAnnotation { Text = "Rectangle", Fill = Colors.MistyRose, MinimumX = 1, MaximumX = 2 });
        plot.Annotations.Add(new PointAnnotation { Text = "Point", Fill = Colors.Green, Shape = MarkerType.Diamond, X = 0.5, Y = 0.5, Size = 6 });
        plot.Annotations.Add(new PolygonAnnotation { Text = "Polygon", Fill = Colors.Lavender, Points = new List<DataPoint> { new(0, 0), new(1, 1), new(2, 0) } });
        plot.Annotations.Add(new LineAnnotation { Text = "Line", Type = LineAnnotationType.LinearEquation, Slope = 2, Intercept = 1, Color = Colors.DarkRed, LineStyle = LineStyle.Dash });
        plot.Annotations.Add(new PolylineAnnotation { Text = "Polyline", Color = Colors.Teal, Points = new List<DataPoint> { new(0, 1), new(1, 2) } });
        plot.Annotations.Add(new FunctionAnnotation { Text = "Function", Type = FunctionAnnotationType.EquationX, Color = Colors.DeepPink });

        return plot;
    }
}
