using System.Windows;
using System.Windows.Media;
using OxyPlot;
using OxyPlot.Axes;
using Wpf = OxyPlot.Wpf;
using OxyPlotControls;
using Xunit;

namespace OxyPlotControls.Tests.Axes;

/// <summary>
/// Unit tests for axis type conversion methods in AxisControl.
/// Tests ConvertAxisToLogarithmicAxis, ConvertAxisToLinearAxis, ConvertAxisToNormalAxis,
/// ConvertAxisToGumbelAxis, and ConvertAxisToDateTimeAxis.
/// </summary>
public class AxisTypeConversionTests
{
    private const double Epsilon = 0.0000000000000001;

    #region ConvertAxisToLogarithmicAxis Tests

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_FromLinearAxis_ReturnsLogarithmicAxis()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Original Axis",
            Position = AxisPosition.Left,
            Maximum = 1000,
            StartPosition = 0,
            EndPosition = 1
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.LogarithmicAxis>(result);
        Assert.Equal("Original Axis", result.Title);
        Assert.Equal(AxisPosition.Left, result.Position);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_DefaultParameters_SetsBase10AndPowerPaddingTrue()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis();

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(10, result.Base);
        Assert.True(result.PowerPadding);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_CustomParameters_SetsBaseAndPowerPadding()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis();

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis, logBase: 2, powerPadding: false);

        // Assert
        Assert.Equal(2, result.Base);
        Assert.False(result.PowerPadding);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_NegativeMinimum_SetsMinimumToEpsilon()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = -100,
            Maximum = 1000
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(Epsilon, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_ZeroMinimum_SetsMinimumToEpsilon()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 0,
            Maximum = 1000
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(Epsilon, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_PositiveMinimum_PreservesMinimum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 10,
            Maximum = 1000
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(10, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_PreservesMaximum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = 500
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(500, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_PreservesStartAndEndPosition()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            StartPosition = 0.1,
            EndPosition = 0.9
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(0.1, result.StartPosition);
        Assert.Equal(0.9, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_CopiesAxisProperties()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Test Title",
            TitleColor = Colors.Red,
            TitleFont = "Arial",
            TitleFontSize = 14,
            TitleFontWeight = System.Windows.FontWeights.Bold,
            MajorGridlineStyle = LineStyle.Solid,
            MajorGridlineColor = Colors.Gray,
            TickStyle = TickStyle.Outside,
            TextColor = Colors.Black,
            Font = "Segoe UI",
            FontSize = 10,
            Key = "TestKey",
            Position = AxisPosition.Left
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal("Test Title", result.Title);
        Assert.Equal(Colors.Red, result.TitleColor);
        Assert.Equal("Arial", result.TitleFont);
        Assert.Equal(14, result.TitleFontSize);
        Assert.Equal(System.Windows.FontWeights.Bold, result.TitleFontWeight);
        Assert.Equal(LineStyle.Solid, result.MajorGridlineStyle);
        Assert.Equal(Colors.Gray, result.MajorGridlineColor);
        Assert.Equal(TickStyle.Outside, result.TickStyle);
        Assert.Equal(Colors.Black, result.TextColor);
        Assert.Equal("Segoe UI", result.Font);
        Assert.Equal(10, result.FontSize);
        Assert.Equal("TestKey", result.Key);
        Assert.Equal(AxisPosition.Left, result.Position);
    }

    #endregion

    #region ConvertAxisToLinearAxis Tests

    [StaFact]
    public void ConvertAxisToLinearAxis_FromLogarithmicAxis_ReturnsLinearAxis()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis
        {
            Title = "Log Axis",
            Position = AxisPosition.Left,
            Minimum = 1,
            Maximum = 1000
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.LinearAxis>(result);
        Assert.Equal("Log Axis", result.Title);
        Assert.Equal(AxisPosition.Left, result.Position);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_DefaultParameters_SetsDefaultFractionOptions()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis();

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.False(result.FormatAsFractions);
        Assert.Equal(1, result.FractionUnit);
        Assert.Null(result.FractionUnitSymbol);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_CustomParameters_SetsFractionOptions()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis();

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis,
            formatAsFractions: true,
            fractionUnits: 0.5,
            fractionSymbol: "pi");

        // Assert
        Assert.True(result.FormatAsFractions);
        Assert.Equal(0.5, result.FractionUnit);
        Assert.Equal("pi", result.FractionUnitSymbol);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_PreservesMinimumAndMaximum()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis
        {
            Minimum = 10,
            Maximum = 500
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.Equal(10, result.Minimum);
        Assert.Equal(500, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_PreservesStartAndEndPosition()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis
        {
            StartPosition = 0.2,
            EndPosition = 0.8
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.Equal(0.2, result.StartPosition);
        Assert.Equal(0.8, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_CopiesAxisProperties()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis
        {
            Title = "Log Title",
            TitleColor = Colors.Blue,
            TitleFont = "Verdana",
            TitleFontSize = 12,
            MajorGridlineStyle = LineStyle.Dash,
            MajorGridlineColor = Colors.LightGray,
            MinorGridlineStyle = LineStyle.Dot,
            TickStyle = TickStyle.Inside,
            TextColor = Colors.DarkGray,
            Key = "LogKey"
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.Equal("Log Title", result.Title);
        Assert.Equal(Colors.Blue, result.TitleColor);
        Assert.Equal("Verdana", result.TitleFont);
        Assert.Equal(12, result.TitleFontSize);
        Assert.Equal(LineStyle.Dash, result.MajorGridlineStyle);
        Assert.Equal(Colors.LightGray, result.MajorGridlineColor);
        Assert.Equal(LineStyle.Dot, result.MinorGridlineStyle);
        Assert.Equal(TickStyle.Inside, result.TickStyle);
        Assert.Equal(Colors.DarkGray, result.TextColor);
        Assert.Equal("LogKey", result.Key);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_FromNormalProbabilityAxis_ReturnsLinearAxis()
    {
        // Arrange
        var normalAxis = new Wpf.NormalProbabilityAxis
        {
            Title = "Normal Axis",
            Position = AxisPosition.Bottom
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(normalAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.LinearAxis>(result);
        Assert.Equal("Normal Axis", result.Title);
    }

    #endregion

    #region ConvertAxisToNormalAxis Tests

    [StaFact]
    public void ConvertAxisToNormalAxis_FromLinearAxis_ReturnsNormalProbabilityAxis()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Linear Axis",
            Position = AxisPosition.Bottom,
            StartPosition = 0,
            EndPosition = 1
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.NormalProbabilityAxis>(result);
        Assert.Equal("Linear Axis", result.Title);
        Assert.Equal(AxisPosition.Bottom, result.Position);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_MinimumBelowEpsilon_SetsMinimumToDefault()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 0
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.0000001, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_MaximumAbove999_SetsMaximumTo999()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = 1.0
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.999, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_MaximumIsNaN_SetsMaximumTo999()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = double.NaN
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.999, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_ValidMinimum_PreservesMinimum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 0.001
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.001, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_ValidMaximum_PreservesMaximum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = 0.95
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.95, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_PreservesStartAndEndPosition()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            StartPosition = 0.15,
            EndPosition = 0.85
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.15, result.StartPosition);
        Assert.Equal(0.85, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_CopiesAxisProperties()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Source Title",
            TitleColor = Colors.Green,
            TitleFont = "Georgia",
            MajorGridlineStyle = LineStyle.Solid,
            TickStyle = TickStyle.Crossing,
            Key = "SourceKey"
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal("Source Title", result.Title);
        Assert.Equal(Colors.Green, result.TitleColor);
        Assert.Equal("Georgia", result.TitleFont);
        Assert.Equal(LineStyle.Solid, result.MajorGridlineStyle);
        Assert.Equal(TickStyle.Crossing, result.TickStyle);
        Assert.Equal("SourceKey", result.Key);
    }

    #endregion

    #region ConvertAxisToGumbelAxis Tests

    [StaFact]
    public void ConvertAxisToGumbelAxis_FromLinearAxis_ReturnsGumbelProbabilityAxis()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Linear Axis",
            Position = AxisPosition.Bottom,
            StartPosition = 0,
            EndPosition = 1
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.GumbelProbabilityAxis>(result);
        Assert.Equal("Linear Axis", result.Title);
        Assert.Equal(AxisPosition.Bottom, result.Position);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_MinimumBelowEpsilon_SetsMinimumToDefault()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 0
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.0000001, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_MaximumAbove99_SetsMaximumTo99()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = 1.0
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.99, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_MaximumIsNaN_SetsMaximumTo99()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = double.NaN
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.99, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_ValidMinimum_PreservesMinimum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 0.001
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.001, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_ValidMaximum_PreservesMaximum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Maximum = 0.90
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.90, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_PreservesStartAndEndPosition()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            StartPosition = 0.25,
            EndPosition = 0.75
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.25, result.StartPosition);
        Assert.Equal(0.75, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_CopiesAxisProperties()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Gumbel Source",
            TitleColor = Colors.Orange,
            TitleFont = "Tahoma",
            MajorGridlineStyle = LineStyle.Dash,
            TickStyle = TickStyle.None,
            Key = "GumbelKey"
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal("Gumbel Source", result.Title);
        Assert.Equal(Colors.Orange, result.TitleColor);
        Assert.Equal("Tahoma", result.TitleFont);
        Assert.Equal(LineStyle.Dash, result.MajorGridlineStyle);
        Assert.Equal(TickStyle.None, result.TickStyle);
        Assert.Equal("GumbelKey", result.Key);
    }

    #endregion

    #region ConvertAxisToDateTimeAxis Tests

    [StaFact]
    public void ConvertAxisToDateTimeAxis_FromLinearAxis_ReturnsDateTimeAxis()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "Linear Axis",
            Position = AxisPosition.Bottom,
            StartPosition = 0,
            EndPosition = 1
        };

        // Act
        var result = AxisControl.ConvertAxisToDateTimeAxis(linearAxis);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<Wpf.DateTimeAxis>(result);
        Assert.Equal("Linear Axis", result.Title);
        Assert.Equal(AxisPosition.Bottom, result.Position);
    }

    [StaFact]
    public void ConvertAxisToDateTimeAxis_PreservesMinimumAndMaximum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 100,
            Maximum = 500
        };

        // Act
        var result = AxisControl.ConvertAxisToDateTimeAxis(linearAxis);

        // Assert
        Assert.Equal(100, result.Minimum);
        Assert.Equal(500, result.Maximum);
    }

    [StaFact]
    public void ConvertAxisToDateTimeAxis_PreservesStartAndEndPosition()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            StartPosition = 0.3,
            EndPosition = 0.7
        };

        // Act
        var result = AxisControl.ConvertAxisToDateTimeAxis(linearAxis);

        // Assert
        Assert.Equal(0.3, result.StartPosition);
        Assert.Equal(0.7, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToDateTimeAxis_CopiesAxisProperties()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = "DateTime Source",
            TitleColor = Colors.Purple,
            TitleFont = "Consolas",
            MajorGridlineStyle = LineStyle.DashDot,
            TickStyle = TickStyle.Outside,
            Key = "DateTimeKey"
        };

        // Act
        var result = AxisControl.ConvertAxisToDateTimeAxis(linearAxis);

        // Assert
        Assert.Equal("DateTime Source", result.Title);
        Assert.Equal(Colors.Purple, result.TitleColor);
        Assert.Equal("Consolas", result.TitleFont);
        Assert.Equal(LineStyle.DashDot, result.MajorGridlineStyle);
        Assert.Equal(TickStyle.Outside, result.TickStyle);
        Assert.Equal("DateTimeKey", result.Key);
    }

    #endregion

    #region Cross-Conversion Tests

    [StaFact]
    public void ConvertLinearToLogAndBack_PreservesCompatibleProperties()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Title = "Round Trip",
            TitleColor = Colors.Red,
            Position = AxisPosition.Left,
            MajorGridlineStyle = LineStyle.Solid,
            Maximum = 1000,
            StartPosition = 0.1,
            EndPosition = 0.9
        };

        // Act
        var logAxis = AxisControl.ConvertAxisToLogarithmicAxis(originalAxis);
        var linearAxis = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.Equal("Round Trip", linearAxis.Title);
        Assert.Equal(Colors.Red, linearAxis.TitleColor);
        Assert.Equal(AxisPosition.Left, linearAxis.Position);
        Assert.Equal(LineStyle.Solid, linearAxis.MajorGridlineStyle);
        Assert.Equal(0.1, linearAxis.StartPosition);
        Assert.Equal(0.9, linearAxis.EndPosition);
    }

    [StaFact]
    public void ConvertLinearToNormalAndBack_PreservesCompatibleProperties()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Title = "Normal Round Trip",
            TitleColor = Colors.Blue,
            Position = AxisPosition.Bottom,
            MajorGridlineStyle = LineStyle.Dash,
            StartPosition = 0.2,
            EndPosition = 0.8
        };

        // Act
        var normalAxis = AxisControl.ConvertAxisToNormalAxis(originalAxis);
        var linearAxis = AxisControl.ConvertAxisToLinearAxis(normalAxis);

        // Assert
        Assert.Equal("Normal Round Trip", linearAxis.Title);
        Assert.Equal(Colors.Blue, linearAxis.TitleColor);
        Assert.Equal(AxisPosition.Bottom, linearAxis.Position);
        Assert.Equal(LineStyle.Dash, linearAxis.MajorGridlineStyle);
        Assert.Equal(0.2, linearAxis.StartPosition);
        Assert.Equal(0.8, linearAxis.EndPosition);
    }

    [StaFact]
    public void ConvertLinearToGumbelAndBack_PreservesCompatibleProperties()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Title = "Gumbel Round Trip",
            TitleColor = Colors.Green,
            Position = AxisPosition.Bottom,
            MajorGridlineStyle = LineStyle.Dot,
            StartPosition = 0.15,
            EndPosition = 0.85
        };

        // Act
        var gumbelAxis = AxisControl.ConvertAxisToGumbelAxis(originalAxis);
        var linearAxis = AxisControl.ConvertAxisToLinearAxis(gumbelAxis);

        // Assert
        Assert.Equal("Gumbel Round Trip", linearAxis.Title);
        Assert.Equal(Colors.Green, linearAxis.TitleColor);
        Assert.Equal(AxisPosition.Bottom, linearAxis.Position);
        Assert.Equal(LineStyle.Dot, linearAxis.MajorGridlineStyle);
        Assert.Equal(0.15, linearAxis.StartPosition);
        Assert.Equal(0.85, linearAxis.EndPosition);
    }

    [StaFact]
    public void ConvertLinearToDateTimeAndBack_PreservesCompatibleProperties()
    {
        // Arrange
        var originalAxis = new Wpf.LinearAxis
        {
            Title = "DateTime Round Trip",
            TitleColor = Colors.Orange,
            Position = AxisPosition.Top,
            MajorGridlineStyle = LineStyle.LongDash,
            StartPosition = 0.05,
            EndPosition = 0.95
        };

        // Act
        var dateTimeAxis = AxisControl.ConvertAxisToDateTimeAxis(originalAxis);
        var linearAxis = AxisControl.ConvertAxisToLinearAxis(dateTimeAxis);

        // Assert
        Assert.Equal("DateTime Round Trip", linearAxis.Title);
        Assert.Equal(Colors.Orange, linearAxis.TitleColor);
        Assert.Equal(AxisPosition.Top, linearAxis.Position);
        Assert.Equal(LineStyle.LongDash, linearAxis.MajorGridlineStyle);
        Assert.Equal(0.05, linearAxis.StartPosition);
        Assert.Equal(0.95, linearAxis.EndPosition);
    }

    #endregion

    #region Reversed Axis Tests

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_ReversedAxis_PreservesReversedPosition()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            StartPosition = 1,
            EndPosition = 0
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(1, result.StartPosition);
        Assert.Equal(0, result.EndPosition);
    }

    [StaFact]
    public void ConvertAxisToLinearAxis_ReversedAxis_PreservesReversedPosition()
    {
        // Arrange
        var logAxis = new Wpf.LogarithmicAxis
        {
            StartPosition = 1,
            EndPosition = 0
        };

        // Act
        var result = AxisControl.ConvertAxisToLinearAxis(logAxis);

        // Assert
        Assert.Equal(1, result.StartPosition);
        Assert.Equal(0, result.EndPosition);
    }

    #endregion

    #region Edge Case Tests

    [StaFact]
    public void ConvertAxisToLogarithmicAxis_VerySmallPositiveMinimum_PreservesMinimum()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = 1E-15
        };

        // Act
        var result = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);

        // Assert
        Assert.Equal(1E-15, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToNormalAxis_NegativeMinimum_SetsMinimumToDefault()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = -0.5
        };

        // Act
        var result = AxisControl.ConvertAxisToNormalAxis(linearAxis);

        // Assert
        Assert.Equal(0.0000001, result.Minimum);
    }

    [StaFact]
    public void ConvertAxisToGumbelAxis_NegativeMinimum_SetsMinimumToDefault()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Minimum = -0.1
        };

        // Act
        var result = AxisControl.ConvertAxisToGumbelAxis(linearAxis);

        // Assert
        Assert.Equal(0.0000001, result.Minimum);
    }

    [StaFact]
    public void AllConversions_NullProperties_HandledGracefully()
    {
        // Arrange
        var linearAxis = new Wpf.LinearAxis
        {
            Title = null!,
            Key = null!,
            Unit = null!
        };

        // Act & Assert - No exceptions should be thrown
        var logAxis = AxisControl.ConvertAxisToLogarithmicAxis(linearAxis);
        var normalAxis = AxisControl.ConvertAxisToNormalAxis(linearAxis);
        var gumbelAxis = AxisControl.ConvertAxisToGumbelAxis(linearAxis);
        var dateTimeAxis = AxisControl.ConvertAxisToDateTimeAxis(linearAxis);

        Assert.NotNull(logAxis);
        Assert.NotNull(normalAxis);
        Assert.NotNull(gumbelAxis);
        Assert.NotNull(dateTimeAxis);
    }

    #endregion
}
