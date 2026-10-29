using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Wpf.Serialization;
using Wpf = OxyPlot.Wpf;
using Xunit;

namespace OxyPlotControls.Tests.Serialization;

/// <summary>
/// Unit tests for AxisControl serialization functionality.
/// Tests round-trip serialization of axis properties for all supported axis types.
/// </summary>
public class AxisSerializationTests
{
    #region AxisPropertiesTag Tests

    [StaFact]
    public void AxisPropertiesTag_HasExpectedValue()
    {
        // Assert
        Assert.Equal("Axis", PlotSerializer.AxisPropertiesTag);
    }

    #endregion

    #region Linear Axis Round-Trip Tests

    [StaFact]
    public void LinearAxis_GeneralProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Name = "TestLinearAxis",
            IsEnabled = true,
            IsAxisVisible = true,
            StartPosition = 0,
            EndPosition = 1,
            IsPanEnabled = true,
            IsZoomEnabled = true
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Name, deserializedAxis.Name);
        Assert.Equal(originalAxis.IsEnabled, deserializedAxis.IsEnabled);
        Assert.Equal(originalAxis.IsAxisVisible, deserializedAxis.IsAxisVisible);
        Assert.Equal(originalAxis.StartPosition, deserializedAxis.StartPosition);
        Assert.Equal(originalAxis.EndPosition, deserializedAxis.EndPosition);
        Assert.Equal(originalAxis.IsPanEnabled, deserializedAxis.IsPanEnabled);
        Assert.Equal(originalAxis.IsZoomEnabled, deserializedAxis.IsZoomEnabled);
    }

    [StaFact]
    public void LinearAxis_NumericProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Minimum = 0,
            Maximum = 100,
            AbsoluteMinimum = -50,
            AbsoluteMaximum = 150,
            FilterMinValue = 10,
            FilterMaxValue = 90
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Minimum, deserializedAxis.Minimum);
        Assert.Equal(originalAxis.Maximum, deserializedAxis.Maximum);
        Assert.Equal(originalAxis.AbsoluteMinimum, deserializedAxis.AbsoluteMinimum);
        Assert.Equal(originalAxis.AbsoluteMaximum, deserializedAxis.AbsoluteMaximum);
        Assert.Equal(originalAxis.FilterMinValue, deserializedAxis.FilterMinValue);
        Assert.Equal(originalAxis.FilterMaxValue, deserializedAxis.FilterMaxValue);
    }

    [StaFact]
    public void LinearAxis_StyleProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            AxislineColor = Colors.Red,
            AxislineStyle = LineStyle.Dash,
            AxislineThickness = 2.5
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.AxislineColor, deserializedAxis.AxislineColor);
        Assert.Equal(originalAxis.AxislineStyle, deserializedAxis.AxislineStyle);
        Assert.Equal(originalAxis.AxislineThickness, deserializedAxis.AxislineThickness);
    }

    [StaFact]
    public void LinearAxis_PositionProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            AxisDistance = 5,
            PositionAtZeroCrossing = true,
            Position = AxisPosition.Left,
            Key = "TestKey",
            PositionTier = 1
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.AxisDistance, deserializedAxis.AxisDistance);
        Assert.Equal(originalAxis.PositionAtZeroCrossing, deserializedAxis.PositionAtZeroCrossing);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
        Assert.Equal(originalAxis.Key, deserializedAxis.Key);
        Assert.Equal(originalAxis.PositionTier, deserializedAxis.PositionTier);
    }

    [StaFact]
    public void LinearAxis_TitleProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Title = "Test Title",
            TitleColor = Colors.Navy,
            TitleFont = "Arial",
            TitleFontSize = 14,
            TitleFontWeight = System.Windows.FontWeights.Bold,
            AxisTitleDistance = 10,
            Unit = "meters"
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.TitleColor, deserializedAxis.TitleColor);
        Assert.Equal(originalAxis.TitleFont, deserializedAxis.TitleFont);
        Assert.Equal(originalAxis.TitleFontSize, deserializedAxis.TitleFontSize);
        Assert.Equal(originalAxis.TitleFontWeight, deserializedAxis.TitleFontWeight);
        Assert.Equal(originalAxis.AxisTitleDistance, deserializedAxis.AxisTitleDistance);
        Assert.Equal(originalAxis.Unit, deserializedAxis.Unit);
    }

    [StaFact]
    public void LinearAxis_LabelProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            TextColor = Colors.DarkGray,
            Font = "Verdana",
            FontSize = 10,
            FontWeight = System.Windows.FontWeights.Normal,
            Angle = 45,
            AxisTickToLabelDistance = 8,
            StringFormat = "N2",
            UseSuperExponentialFormat = true
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.TextColor, deserializedAxis.TextColor);
        Assert.Equal(originalAxis.Font, deserializedAxis.Font);
        Assert.Equal(originalAxis.FontSize, deserializedAxis.FontSize);
        Assert.Equal(originalAxis.FontWeight, deserializedAxis.FontWeight);
        Assert.Equal(originalAxis.Angle, deserializedAxis.Angle);
        Assert.Equal(originalAxis.AxisTickToLabelDistance, deserializedAxis.AxisTickToLabelDistance);
        Assert.Equal(originalAxis.StringFormat, deserializedAxis.StringFormat);
        Assert.Equal(originalAxis.UseSuperExponentialFormat, deserializedAxis.UseSuperExponentialFormat);
    }

    [StaFact]
    public void LinearAxis_GridlineProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            MajorGridlineColor = Colors.LightGray,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineThickness = 1,
            MajorStep = 10,
            MajorTickSize = 5,
            MinorGridlineColor = Colors.WhiteSmoke,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineThickness = 0.5,
            MinorStep = 2,
            MinorTickSize = 2
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.MajorGridlineColor, deserializedAxis.MajorGridlineColor);
        Assert.Equal(originalAxis.MajorGridlineStyle, deserializedAxis.MajorGridlineStyle);
        Assert.Equal(originalAxis.MajorGridlineThickness, deserializedAxis.MajorGridlineThickness);
        Assert.Equal(originalAxis.MajorStep, deserializedAxis.MajorStep);
        Assert.Equal(originalAxis.MajorTickSize, deserializedAxis.MajorTickSize);
        Assert.Equal(originalAxis.MinorGridlineColor, deserializedAxis.MinorGridlineColor);
        Assert.Equal(originalAxis.MinorGridlineStyle, deserializedAxis.MinorGridlineStyle);
        Assert.Equal(originalAxis.MinorGridlineThickness, deserializedAxis.MinorGridlineThickness);
        Assert.Equal(originalAxis.MinorStep, deserializedAxis.MinorStep);
        Assert.Equal(originalAxis.MinorTickSize, deserializedAxis.MinorTickSize);
    }

    [StaFact]
    public void LinearAxis_TickProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            TickStyle = TickStyle.Inside,
            TicklineColor = Colors.Black
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.TickStyle, deserializedAxis.TickStyle);
        Assert.Equal(originalAxis.TicklineColor, deserializedAxis.TicklineColor);
    }

    [StaFact]
    public void LinearAxis_SpecificProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            FormatAsFractions = true
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.FormatAsFractions, deserializedAxis.FormatAsFractions);
    }

    #endregion

    #region Logarithmic Axis Round-Trip Tests

    [StaFact]
    public void LogarithmicAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LogarithmicAxis
        {
            Title = "Log Axis",
            Minimum = 0.001,
            Maximum = 1000,
            Base = 10,
            PowerPadding = true,
            Position = AxisPosition.Left
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LogarithmicAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Minimum, deserializedAxis.Minimum);
        Assert.Equal(originalAxis.Maximum, deserializedAxis.Maximum);
        Assert.Equal(originalAxis.Base, deserializedAxis.Base);
        Assert.Equal(originalAxis.PowerPadding, deserializedAxis.PowerPadding);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
    }

    [StaFact]
    public void LogarithmicAxis_Base2_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LogarithmicAxis
        {
            Base = 2,
            Minimum = 1,
            Maximum = 256
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LogarithmicAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(2, deserializedAxis.Base);
    }

    [StaFact]
    public void LogarithmicAxis_NoPowerPadding_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LogarithmicAxis
        {
            PowerPadding = false
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LogarithmicAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.False(deserializedAxis.PowerPadding);
    }

    #endregion

    #region DateTime Axis Round-Trip Tests

    [StaFact]
    public void DateTimeAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.DateTimeAxis
        {
            Title = "Date Axis",
            Position = AxisPosition.Bottom,
            CalendarWeekRule = CalendarWeekRule.FirstDay
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.DateTimeAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
        Assert.Equal(originalAxis.CalendarWeekRule, deserializedAxis.CalendarWeekRule);
    }

    [StaTheory]
    [InlineData(CalendarWeekRule.FirstDay)]
    [InlineData(CalendarWeekRule.FirstFourDayWeek)]
    [InlineData(CalendarWeekRule.FirstFullWeek)]
    public void DateTimeAxis_AllCalendarWeekRules_RoundTrip_PreservesValues(CalendarWeekRule weekRule)
    {
        // Arrange
        var originalAxis = new Wpf.DateTimeAxis { CalendarWeekRule = weekRule };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.DateTimeAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(weekRule, deserializedAxis.CalendarWeekRule);
    }

    #endregion

    #region Normal Probability Axis Round-Trip Tests

    [StaFact]
    public void NormalProbabilityAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.NormalProbabilityAxis
        {
            Title = "Normal Probability Axis",
            Minimum = 0.0001,
            Maximum = 0.9999,
            Position = AxisPosition.Bottom
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.NormalProbabilityAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Minimum, deserializedAxis.Minimum);
        Assert.Equal(originalAxis.Maximum, deserializedAxis.Maximum);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
    }

    [StaFact]
    public void NormalProbabilityAxis_GeneralProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.NormalProbabilityAxis
        {
            TitleColor = Colors.DarkGreen,
            TitleFont = "Segoe UI",
            TitleFontSize = 12,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = Colors.LightGray
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.NormalProbabilityAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.TitleColor, deserializedAxis.TitleColor);
        Assert.Equal(originalAxis.TitleFont, deserializedAxis.TitleFont);
        Assert.Equal(originalAxis.TitleFontSize, deserializedAxis.TitleFontSize);
        Assert.Equal(originalAxis.MajorGridlineStyle, deserializedAxis.MajorGridlineStyle);
        Assert.Equal(originalAxis.MajorGridlineColor, deserializedAxis.MajorGridlineColor);
    }

    #endregion

    #region Gumbel Probability Axis Round-Trip Tests

    [StaFact]
    public void GumbelProbabilityAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.GumbelProbabilityAxis
        {
            Title = "Gumbel Probability Axis",
            Minimum = 0.0001,
            Maximum = 0.99,
            Position = AxisPosition.Bottom
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.GumbelProbabilityAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Minimum, deserializedAxis.Minimum);
        Assert.Equal(originalAxis.Maximum, deserializedAxis.Maximum);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
    }

    [StaFact]
    public void GumbelProbabilityAxis_GeneralProperties_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.GumbelProbabilityAxis
        {
            TitleColor = Colors.DarkRed,
            TitleFont = "Arial",
            TitleFontSize = 11,
            TickStyle = TickStyle.Outside,
            TicklineColor = Colors.Black
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.GumbelProbabilityAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.TitleColor, deserializedAxis.TitleColor);
        Assert.Equal(originalAxis.TitleFont, deserializedAxis.TitleFont);
        Assert.Equal(originalAxis.TitleFontSize, deserializedAxis.TitleFontSize);
        Assert.Equal(originalAxis.TickStyle, deserializedAxis.TickStyle);
        Assert.Equal(originalAxis.TicklineColor, deserializedAxis.TicklineColor);
    }

    #endregion

    #region Category Axis Round-Trip Tests

    [StaFact]
    public void CategoryAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.CategoryAxis
        {
            Title = "Category Axis",
            Position = AxisPosition.Bottom,
            IsTickCentered = true,
            GapWidth = 0.5
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.CategoryAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
        Assert.Equal(originalAxis.IsTickCentered, deserializedAxis.IsTickCentered);
        Assert.Equal(originalAxis.GapWidth, deserializedAxis.GapWidth);
    }

    #endregion

    #region Angle Axis Round-Trip Tests

    [StaFact]
    public void AngleAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.AngleAxis
        {
            Title = "Angle Axis",
            StartAngle = 0,
            EndAngle = 360
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.AngleAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.StartAngle, deserializedAxis.StartAngle);
        Assert.Equal(originalAxis.EndAngle, deserializedAxis.EndAngle);
    }

    #endregion

    #region Linear Color Axis Round-Trip Tests

    [StaFact]
    public void LinearColorAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearColorAxis
        {
            Title = "Color Axis",
            HighColor = Colors.Red,
            LowColor = Colors.Blue,
            PaletteSize = 100,
            InvalidNumberColor = Colors.Gray
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearColorAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.HighColor, deserializedAxis.HighColor);
        Assert.Equal(originalAxis.LowColor, deserializedAxis.LowColor);
        Assert.Equal(originalAxis.PaletteSize, deserializedAxis.PaletteSize);
        Assert.Equal(originalAxis.InvalidNumberColor, deserializedAxis.InvalidNumberColor);
    }

    #endregion

    #region Magnitude Axis Round-Trip Tests

    [StaFact]
    public void MagnitudeAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.MagnitudeAxis
        {
            Title = "Magnitude Axis",
            FormatAsFractions = true
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.MagnitudeAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.FormatAsFractions, deserializedAxis.FormatAsFractions);
    }

    #endregion

    #region TimeSpan Axis Round-Trip Tests

    [StaFact]
    public void TimeSpanAxis_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.TimeSpanAxis
        {
            Title = "TimeSpan Axis",
            Position = AxisPosition.Bottom
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.TimeSpanAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
    }

    #endregion

    #region Axis Position Tests

    [StaTheory]
    [InlineData(AxisPosition.Left)]
    [InlineData(AxisPosition.Right)]
    [InlineData(AxisPosition.Top)]
    [InlineData(AxisPosition.Bottom)]
    [InlineData(AxisPosition.None)]
    public void AllAxisPositions_RoundTrip_PreservesValues(AxisPosition position)
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis { Position = position };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(position, deserializedAxis.Position);
    }

    #endregion

    #region Tick Style Tests

    [StaTheory]
    [InlineData(TickStyle.Crossing)]
    [InlineData(TickStyle.Inside)]
    [InlineData(TickStyle.Outside)]
    [InlineData(TickStyle.None)]
    public void AllTickStyles_RoundTrip_PreservesValues(TickStyle tickStyle)
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis { TickStyle = tickStyle };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(tickStyle, deserializedAxis.TickStyle);
    }

    #endregion

    #region LineStyle Tests

    [StaTheory]
    [InlineData(LineStyle.Solid)]
    [InlineData(LineStyle.Dash)]
    [InlineData(LineStyle.Dot)]
    [InlineData(LineStyle.DashDot)]
    [InlineData(LineStyle.DashDashDot)]
    [InlineData(LineStyle.DashDashDotDot)]
    [InlineData(LineStyle.DashDotDot)]
    [InlineData(LineStyle.LongDash)]
    [InlineData(LineStyle.LongDashDot)]
    [InlineData(LineStyle.LongDashDotDot)]
    [InlineData(LineStyle.None)]
    public void AllLineStyles_RoundTrip_PreservesValues(LineStyle lineStyle)
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis { MajorGridlineStyle = lineStyle };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(lineStyle, deserializedAxis.MajorGridlineStyle);
    }

    #endregion

    #region Edge Case Tests

    [StaFact]
    public void XElementToAxisProperties_WrongElementName_ReturnsNull()
    {
        // Arrange
        var element = new XElement("WrongName");

        // Act
        var result = AxisSerializer.XElementToAxis(element);

        // Assert
        Assert.Null(result);
    }

    [StaFact]
    public void XElementToAxisProperties_UnknownAxisType_ReturnsLinearAxis()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", "UnknownAxisType"));

        // Act
        var result = AxisSerializer.XElementToAxis(element);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.LinearAxis>(result);
    }

    [StaFact]
    public void XElementToAxisProperties_WithTargetAxis_AppliesPropertiesToTarget()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(LinearAxis).ToString()),
            new XElement("Title",
                new XAttribute("Title", "Updated Title")));
        var targetAxis = new Wpf.LinearAxis { Title = "Original Title" };

        // Act
        var result = AxisSerializer.XElementToAxis(element, targetAxis);

        // Assert
        Assert.Same(targetAxis, result);
        Assert.Equal("Updated Title", targetAxis.Title);
    }

    [StaFact]
    public void Axis_NaNValues_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Minimum = double.NaN,
            Maximum = double.NaN
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.True(double.IsNaN(deserializedAxis.Minimum));
        Assert.True(double.IsNaN(deserializedAxis.Maximum));
    }

    [StaFact]
    public void Axis_EmptyTitle_RoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis { Title = null! };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
    }

    #endregion

    #region Backward Compatibility Tests

    [StaFact]
    public void XElementToAxisProperties_OldGeneralFormat_DeserializesCorrectly()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(LinearAxis).ToString()),
            new XElement("General",
                new XAttribute("AxisVisible", "True"),
                new XAttribute("CanPan", "True"),
                new XAttribute("CanZoom", "True")));

        // Act
        var result = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAxisVisible);
        Assert.True(result.IsPanEnabled);
        Assert.True(result.IsZoomEnabled);
    }

    [StaFact]
    public void XElementToAxisProperties_OldStyleFormat_DeserializesCorrectly()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(LinearAxis).ToString()),
            new XElement("Style",
                new XAttribute("Color", "#FFFF0000"),
                new XAttribute("Style", "Dash"),
                new XAttribute("Thickness", "2")));

        // Act
        var result = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(Colors.Red, result.AxislineColor);
        Assert.Equal(LineStyle.Dash, result.AxislineStyle);
        Assert.Equal(2, result.AxislineThickness);
    }

    [StaFact]
    public void XElementToAxisProperties_OldPositionFormat_DeserializesCorrectly()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(LinearAxis).ToString()),
            new XElement("Position",
                new XAttribute("Distance", "5"),
                new XAttribute("ZeroCrossing", "True"),
                new XAttribute("Position", "Left"),
                new XAttribute("Tier", "1")));

        // Act
        var result = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.AxisDistance);
        Assert.True(result.PositionAtZeroCrossing);
        Assert.Equal(AxisPosition.Left, result.Position);
        Assert.Equal(1, result.PositionTier);
    }

    [StaFact]
    public void XElementToAxisProperties_OldLinearAxisFormat_DeserializesCorrectly()
    {
        // Arrange
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(LinearAxis).ToString()),
            new XElement("LinearAxis",
                new XAttribute("FractionFormat", "True")));

        // Act
        var result = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(result);
        Assert.True(result.FormatAsFractions);
    }

    [StaFact]
    public void XElementToAxisProperties_OldLogAxisFormat_DeserializesCorrectly()
    {
        // Arrange - Use Wpf.LogarithmicAxis type (implementation only recognizes Wpf types)
        var element = new XElement(PlotSerializer.AxisPropertiesTag,
            new XAttribute("AxisType", typeof(Wpf.LogarithmicAxis).ToString()),
            new XElement("LogarithmicAxis",
                new XAttribute("LogBase", "2")));

        // Act
        var result = AxisSerializer.XElementToAxis(element) as Wpf.LogarithmicAxis;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Base);
    }

    #endregion

    #region Complete Round-Trip Test

    [StaFact]
    public void AllAxisProperties_CompleteRoundTrip_PreservesValues()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Name = "CompleteTestAxis",
            IsEnabled = true,
            IsAxisVisible = true,
            StartPosition = 0.1,
            EndPosition = 0.9,
            IsPanEnabled = true,
            IsZoomEnabled = true,
            Minimum = -100,
            Maximum = 100,
            AbsoluteMinimum = -200,
            AbsoluteMaximum = 200,
            FilterMinValue = -50,
            FilterMaxValue = 50,
            AxislineColor = Colors.DarkBlue,
            AxislineStyle = LineStyle.Solid,
            AxislineThickness = 2,
            AxisDistance = 5,
            PositionAtZeroCrossing = false,
            Position = AxisPosition.Left,
            Key = "YAxis",
            PositionTier = 0,
            Title = "Y Values",
            TitleColor = Colors.Navy,
            TitleFont = "Segoe UI",
            TitleFontSize = 14,
            TitleFontWeight = System.Windows.FontWeights.Bold,
            AxisTitleDistance = 10,
            Unit = "units",
            TextColor = Colors.Black,
            Font = "Segoe UI",
            FontSize = 10,
            FontWeight = System.Windows.FontWeights.Normal,
            Angle = 0,
            AxisTickToLabelDistance = 4,
            StringFormat = "N2",
            UseSuperExponentialFormat = false,
            MajorGridlineColor = Colors.LightGray,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineThickness = 1,
            MajorStep = 20,
            MajorTickSize = 7,
            MinorGridlineColor = Colors.WhiteSmoke,
            MinorGridlineStyle = LineStyle.Dot,
            MinorGridlineThickness = 0.5,
            MinorStep = 5,
            MinorTickSize = 3,
            TickStyle = TickStyle.Outside,
            TicklineColor = Colors.Black,
            FormatAsFractions = false
        };

        // Act
        var element = AxisSerializer.AxisToXElement(originalAxis);
        var deserializedAxis = AxisSerializer.XElementToAxis(element) as Wpf.LinearAxis;

        // Assert
        Assert.NotNull(deserializedAxis);
        Assert.Equal(originalAxis.Name, deserializedAxis.Name);
        Assert.Equal(originalAxis.IsEnabled, deserializedAxis.IsEnabled);
        Assert.Equal(originalAxis.IsAxisVisible, deserializedAxis.IsAxisVisible);
        Assert.Equal(originalAxis.StartPosition, deserializedAxis.StartPosition);
        Assert.Equal(originalAxis.EndPosition, deserializedAxis.EndPosition);
        Assert.Equal(originalAxis.IsPanEnabled, deserializedAxis.IsPanEnabled);
        Assert.Equal(originalAxis.IsZoomEnabled, deserializedAxis.IsZoomEnabled);
        Assert.Equal(originalAxis.Minimum, deserializedAxis.Minimum);
        Assert.Equal(originalAxis.Maximum, deserializedAxis.Maximum);
        Assert.Equal(originalAxis.AbsoluteMinimum, deserializedAxis.AbsoluteMinimum);
        Assert.Equal(originalAxis.AbsoluteMaximum, deserializedAxis.AbsoluteMaximum);
        Assert.Equal(originalAxis.FilterMinValue, deserializedAxis.FilterMinValue);
        Assert.Equal(originalAxis.FilterMaxValue, deserializedAxis.FilterMaxValue);
        Assert.Equal(originalAxis.AxislineColor, deserializedAxis.AxislineColor);
        Assert.Equal(originalAxis.AxislineStyle, deserializedAxis.AxislineStyle);
        Assert.Equal(originalAxis.AxislineThickness, deserializedAxis.AxislineThickness);
        Assert.Equal(originalAxis.AxisDistance, deserializedAxis.AxisDistance);
        Assert.Equal(originalAxis.PositionAtZeroCrossing, deserializedAxis.PositionAtZeroCrossing);
        Assert.Equal(originalAxis.Position, deserializedAxis.Position);
        Assert.Equal(originalAxis.Key, deserializedAxis.Key);
        Assert.Equal(originalAxis.PositionTier, deserializedAxis.PositionTier);
        Assert.Equal(originalAxis.Title, deserializedAxis.Title);
        Assert.Equal(originalAxis.TitleColor, deserializedAxis.TitleColor);
        Assert.Equal(originalAxis.TitleFont, deserializedAxis.TitleFont);
        Assert.Equal(originalAxis.TitleFontSize, deserializedAxis.TitleFontSize);
        Assert.Equal(originalAxis.TitleFontWeight, deserializedAxis.TitleFontWeight);
        Assert.Equal(originalAxis.AxisTitleDistance, deserializedAxis.AxisTitleDistance);
        Assert.Equal(originalAxis.Unit, deserializedAxis.Unit);
        Assert.Equal(originalAxis.TextColor, deserializedAxis.TextColor);
        Assert.Equal(originalAxis.Font, deserializedAxis.Font);
        Assert.Equal(originalAxis.FontSize, deserializedAxis.FontSize);
        Assert.Equal(originalAxis.FontWeight, deserializedAxis.FontWeight);
        Assert.Equal(originalAxis.Angle, deserializedAxis.Angle);
        Assert.Equal(originalAxis.AxisTickToLabelDistance, deserializedAxis.AxisTickToLabelDistance);
        Assert.Equal(originalAxis.StringFormat, deserializedAxis.StringFormat);
        Assert.Equal(originalAxis.UseSuperExponentialFormat, deserializedAxis.UseSuperExponentialFormat);
        Assert.Equal(originalAxis.MajorGridlineColor, deserializedAxis.MajorGridlineColor);
        Assert.Equal(originalAxis.MajorGridlineStyle, deserializedAxis.MajorGridlineStyle);
        Assert.Equal(originalAxis.MajorGridlineThickness, deserializedAxis.MajorGridlineThickness);
        Assert.Equal(originalAxis.MajorStep, deserializedAxis.MajorStep);
        Assert.Equal(originalAxis.MajorTickSize, deserializedAxis.MajorTickSize);
        Assert.Equal(originalAxis.MinorGridlineColor, deserializedAxis.MinorGridlineColor);
        Assert.Equal(originalAxis.MinorGridlineStyle, deserializedAxis.MinorGridlineStyle);
        Assert.Equal(originalAxis.MinorGridlineThickness, deserializedAxis.MinorGridlineThickness);
        Assert.Equal(originalAxis.MinorStep, deserializedAxis.MinorStep);
        Assert.Equal(originalAxis.MinorTickSize, deserializedAxis.MinorTickSize);
        Assert.Equal(originalAxis.TickStyle, deserializedAxis.TickStyle);
        Assert.Equal(originalAxis.TicklineColor, deserializedAxis.TicklineColor);
        Assert.Equal(originalAxis.FormatAsFractions, deserializedAxis.FormatAsFractions);
    }

    #endregion
}
