using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using Xunit;
using Wpf = OxyPlot.Wpf;

namespace OxyPlotControls.Tests;

public class OxyPlotToolbarExportDataTests
{
    private const double RenderWidth = 400;
    private const double RenderHeight = 300;

    [StaFact]
    public void BuildExportDataTables_ScatterPointSeriesWithMapping_ExportsMappedCoordinates()
    {
        var plot = new Wpf.Plot();
        plot.Series.Add(new Wpf.ScatterPointSeries
        {
            Title = "Exact Data",
            ItemsSource = new[]
            {
                new SourcePoint { Index = 1, Value = 10 },
                new SourcePoint { Index = 2, Value = 20 }
            },
            Mapping = item =>
            {
                var point = (SourcePoint)item;
                return new ScatterPoint(point.Index, point.Value);
            }
        });

        var table = Assert.Single(BuildExportDataTables(plot));

        Assert.Equal("1", Convert.ToString(table.Rows[0]["Exact Data_x"]));
        Assert.Equal("10", Convert.ToString(table.Rows[0]["Exact Data_y"]));
        Assert.Equal("2", Convert.ToString(table.Rows[1]["Exact Data_x"]));
        Assert.Equal("20", Convert.ToString(table.Rows[1]["Exact Data_y"]));
    }

    [StaFact]
    public void BuildExportDataTables_LineSeriesWithMapping_ExportsMappedCoordinates()
    {
        var plot = new Wpf.Plot();
        plot.Series.Add(new Wpf.LineSeries
        {
            Title = "Trend",
            ItemsSource = new[]
            {
                new SourcePoint { Index = 3, Value = 30 },
                new SourcePoint { Index = 4, Value = 40 }
            },
            Mapping = item =>
            {
                var point = (SourcePoint)item;
                return new DataPoint(point.Index, point.Value);
            }
        });

        var table = Assert.Single(BuildExportDataTables(plot));

        Assert.Equal("3", Convert.ToString(table.Rows[0]["Trend_x"]));
        Assert.Equal("30", Convert.ToString(table.Rows[0]["Trend_y"]));
        Assert.Equal("4", Convert.ToString(table.Rows[1]["Trend_x"]));
        Assert.Equal("40", Convert.ToString(table.Rows[1]["Trend_y"]));
    }

    [StaFact]
    public void BuildExportDataTables_ScatterErrorSeriesWithDataFields_ExportsErrors()
    {
        var plot = new Wpf.Plot();
        plot.Series.Add(new Wpf.ScatterErrorSeries
        {
            Title = "Error Data",
            ItemsSource = new[]
            {
                new ErrorPoint
                {
                    X = 5,
                    Y = 50,
                    LowerX = 4,
                    UpperX = 6,
                    LowerY = 45,
                    UpperY = 55
                }
            },
            DataFieldX = nameof(ErrorPoint.X),
            DataFieldY = nameof(ErrorPoint.Y),
            DataFieldLowerErrorX = nameof(ErrorPoint.LowerX),
            DataFieldUpperErrorX = nameof(ErrorPoint.UpperX),
            DataFieldLowerErrorY = nameof(ErrorPoint.LowerY),
            DataFieldUpperErrorY = nameof(ErrorPoint.UpperY)
        });

        var table = Assert.Single(BuildExportDataTables(plot));

        Assert.Equal("4", Convert.ToString(table.Rows[0]["Error Data_xLower"]));
        Assert.Equal("5", Convert.ToString(table.Rows[0]["Error Data_x"]));
        Assert.Equal("6", Convert.ToString(table.Rows[0]["Error Data_xUpper"]));
        Assert.Equal("45", Convert.ToString(table.Rows[0]["Error Data_yLower"]));
        Assert.Equal("50", Convert.ToString(table.Rows[0]["Error Data_y"]));
        Assert.Equal("55", Convert.ToString(table.Rows[0]["Error Data_yUpper"]));
    }

    [StaFact]
    public void BuildExportDataTables_CategoryAxisWithNullLabelField_DoesNotThrow()
    {
        var plot = new Wpf.Plot();
        plot.Axes.Add(new Wpf.CategoryAxis
        {
            Position = AxisPosition.Bottom,
            ItemsSource = new[] { new CategorySource { Label = "A" } },
            LabelField = null
        });
        plot.Axes.Add(new Wpf.LinearAxis { Position = AxisPosition.Left });
        plot.Series.Add(new Wpf.LineSeries
        {
            Title = "Categorized",
            ItemsSource = new[] { new DataPoint(0, 1) }
        });

        RenderPlot(plot);

        var table = Assert.Single(BuildExportDataTables(plot));

        Assert.True(table.Columns.Contains("Xcategory"));
        Assert.Equal("", Convert.ToString(table.Rows[0]["Xcategory"]));
    }

    private static List<DataTable> BuildExportDataTables(Wpf.Plot plot)
    {
        var method = typeof(OxyPlotToolbar).GetMethod("BuildExportDataTables", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return Assert.IsType<List<DataTable>>(method!.Invoke(null, new object[] { plot }));
    }

    private static void RenderPlot(Wpf.Plot plot)
    {
        plot.InvalidatePlot(true);
        ((IPlotModel)plot.ActualModel).Render(new PdfRenderContext(RenderWidth, RenderHeight, OxyColors.White), new OxyRect(0, 0, RenderWidth, RenderHeight));
    }

    private sealed class SourcePoint
    {
        public double Index { get; init; }

        public double Value { get; init; }
    }

    private sealed class ErrorPoint
    {
        public double X { get; init; }

        public double Y { get; init; }

        public double LowerX { get; init; }

        public double UpperX { get; init; }

        public double LowerY { get; init; }

        public double UpperY { get; init; }
    }

    private sealed class CategorySource
    {
        public string Label { get; init; } = "";
    }
}
