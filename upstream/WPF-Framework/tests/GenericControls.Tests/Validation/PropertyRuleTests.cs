using System.ComponentModel;
using Xunit;

namespace GenericControls.Tests.Validation;

/// <summary>
/// Unit tests for the <see cref="PropertyRule"/> class.
/// </summary>
public class PropertyRuleTests
{
    #region Constructor Tests

    /// <summary>
    /// Tests that PropertyRule constructor creates a rule with single rule entry.
    /// </summary>
    [Fact]
    public void Constructor_CreatesRuleWithSingleRule()
    {
        // Arrange & Act
        var propertyRule = new PropertyRule(() => false, "Test error message");

        // Assert
        Assert.Single(propertyRule.Rules);
        Assert.Equal("Test error message", propertyRule.Rules[0].Message);
    }

    /// <summary>
    /// Tests that PropertyRule constructor initializes with default values.
    /// </summary>
    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        // Arrange & Act
        var propertyRule = new PropertyRule(() => false, "Test");

        // Assert
        Assert.False(propertyRule.HasError);
        Assert.Equal(string.Empty, propertyRule.ErrorMessage);
    }

    #endregion

    #region ExecuteRules Tests - No Errors

    /// <summary>
    /// Tests that ExecuteRules sets HasError to false when no error conditions exist.
    /// </summary>
    [Fact]
    public void ExecuteRules_NoErrorConditions_HasErrorIsFalse()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Error message");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.False(propertyRule.HasError);
        Assert.Equal("", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules sets HasError to false when all rules pass.
    /// </summary>
    [Fact]
    public void ExecuteRules_AllRulesPass_HasErrorIsFalse()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Error 1");
        propertyRule.AddRule(() => false, "Error 2");
        propertyRule.AddRule(() => false, "Error 3");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.False(propertyRule.HasError);
        Assert.Equal("", propertyRule.ErrorMessage);
    }

    #endregion

    #region ExecuteRules Tests - Single Error

    /// <summary>
    /// Tests that ExecuteRules sets HasError to true for single error condition.
    /// </summary>
    [Fact]
    public void ExecuteRules_SingleErrorCondition_HasErrorIsTrue()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error message");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Equal("Error message", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules sets correct error message when first rule fails.
    /// </summary>
    [Fact]
    public void ExecuteRules_FirstRuleFails_SetsCorrectErrorMessage()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "First rule failed");
        propertyRule.AddRule(() => false, "Second rule failed");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Equal("First rule failed", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules sets correct error message when second rule fails.
    /// </summary>
    [Fact]
    public void ExecuteRules_SecondRuleFails_SetsCorrectErrorMessage()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "First rule failed");
        propertyRule.AddRule(() => true, "Second rule failed");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        // When not the first rule, message is prepended with newline
        Assert.Equal(Environment.NewLine + "Second rule failed", propertyRule.ErrorMessage);
    }

    #endregion

    #region ExecuteRules Tests - Error Aggregation

    /// <summary>
    /// Tests that ExecuteRules aggregates multiple error conditions.
    /// </summary>
    [Fact]
    public void ExecuteRules_MultipleErrorConditions_AggregatesErrors()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error 1");
        propertyRule.AddRule(() => true, "Error 2");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Contains("Error 1", propertyRule.ErrorMessage);
        Assert.Contains("Error 2", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules separates multiple errors with newline.
    /// </summary>
    [Fact]
    public void ExecuteRules_MultipleErrors_SeparatedByNewLine()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error 1");
        propertyRule.AddRule(() => true, "Error 2");
        propertyRule.AddRule(() => true, "Error 3");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        var lines = propertyRule.ErrorMessage.Split(Environment.NewLine);
        Assert.Equal(3, lines.Length);
        Assert.Equal("Error 1", lines[0]);
        Assert.Equal("Error 2", lines[1]);
        Assert.Equal("Error 3", lines[2]);
    }

    /// <summary>
    /// Tests that ExecuteRules aggregates only failing rules.
    /// </summary>
    [Fact]
    public void ExecuteRules_MixedRuleResults_OnlyFailingRulesAggregated()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error 1");
        propertyRule.AddRule(() => false, "Error 2"); // This passes
        propertyRule.AddRule(() => true, "Error 3");
        propertyRule.AddRule(() => false, "Error 4"); // This passes

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Contains("Error 1", propertyRule.ErrorMessage);
        Assert.DoesNotContain("Error 2", propertyRule.ErrorMessage);
        Assert.Contains("Error 3", propertyRule.ErrorMessage);
        Assert.DoesNotContain("Error 4", propertyRule.ErrorMessage);
    }

    #endregion

    #region ExecuteRules Tests - Reset Behavior

    /// <summary>
    /// Tests that ExecuteRules resets error state after error.
    /// </summary>
    [Fact]
    public void ExecuteRules_AfterError_ResetsErrorState()
    {
        // Arrange
        bool hasError = true;
        var propertyRule = new PropertyRule(() => hasError, "Error");

        // Act - First execution with error
        propertyRule.ExecuteRules();
        Assert.True(propertyRule.HasError);

        // Change condition to no error
        hasError = false;
        propertyRule.ExecuteRules();

        // Assert - Error should be cleared
        Assert.False(propertyRule.HasError);
        Assert.Equal("", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules resets error message each time it is called.
    /// </summary>
    [Fact]
    public void ExecuteRules_CalledMultipleTimes_ResetsErrorMessageEachTime()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error 1");

        // Act - First call
        propertyRule.ExecuteRules();
        var firstErrorMessage = propertyRule.ErrorMessage;

        // Act - Second call
        propertyRule.ExecuteRules();
        var secondErrorMessage = propertyRule.ErrorMessage;

        // Assert - Both should be the same (reset between calls)
        Assert.Equal(firstErrorMessage, secondErrorMessage);
        Assert.Equal("Error 1", secondErrorMessage);
    }

    #endregion

    #region ExecuteRules Tests - Exception Handling

    /// <summary>
    /// Tests that ExecuteRules sets HasError to true when rule throws exception.
    /// </summary>
    [Fact]
    public void ExecuteRules_RuleThrowsException_SetsHasErrorTrue()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => throw new InvalidOperationException("Test exception"), "Normal error");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
    }

    /// <summary>
    /// Tests that ExecuteRules includes exception message in error message.
    /// </summary>
    [Fact]
    public void ExecuteRules_RuleThrowsException_ErrorMessageContainsExceptionMessage()
    {
        // Arrange
        var exceptionMessage = "This is a test exception";
        var propertyRule = new PropertyRule(() => throw new InvalidOperationException(exceptionMessage), "Normal error");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.Contains(exceptionMessage, propertyRule.ErrorMessage);
    }

    #endregion

    #region AddRule Tests

    /// <summary>
    /// Tests that AddRule increases rule count.
    /// </summary>
    [Fact]
    public void AddRule_AddsNewRule_IncreasesRuleCount()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Initial rule");
        Assert.Single(propertyRule.Rules);

        // Act
        propertyRule.AddRule(() => false, "Added rule");

        // Assert
        Assert.Equal(2, propertyRule.Rules.Count);
    }

    /// <summary>
    /// Tests that AddRule adds multiple rules that are all executed.
    /// </summary>
    [Fact]
    public void AddRule_MultipleRules_AllRulesExecuted()
    {
        // Arrange
        int counter = 0;
        var propertyRule = new PropertyRule(() => { counter++; return false; }, "Rule 1");
        propertyRule.AddRule(() => { counter++; return false; }, "Rule 2");
        propertyRule.AddRule(() => { counter++; return false; }, "Rule 3");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.Equal(3, counter);
    }

    #endregion

    #region PropertyChanged Tests

    /// <summary>
    /// Tests that HasError property change raises PropertyChanged event.
    /// </summary>
    [Fact]
    public void HasError_Changed_RaisesPropertyChanged()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error");
        var changedPropertyNames = new List<string>();

        propertyRule.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
                changedPropertyNames.Add(args.PropertyName);
        };

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.Contains(nameof(PropertyRule.HasError), changedPropertyNames);
        Assert.Contains(nameof(PropertyRule.ErrorMessage), changedPropertyNames);
    }

    /// <summary>
    /// Tests that ErrorMessage property change raises PropertyChanged event.
    /// </summary>
    [Fact]
    public void ErrorMessage_Changed_RaisesPropertyChanged()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "Error");
        var propertyChangedEvents = new List<string>();

        propertyRule.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName != null)
                propertyChangedEvents.Add(args.PropertyName);
        };

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.Contains(nameof(PropertyRule.ErrorMessage), propertyChangedEvents);
    }

    /// <summary>
    /// Tests that setting HasError to same value does not raise PropertyChanged event.
    /// </summary>
    [Fact]
    public void HasError_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Error");
        propertyRule.HasError = false; // Initial state

        var propertyChangedRaised = false;
        propertyRule.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PropertyRule.HasError))
                propertyChangedRaised = true;
        };

        // Act
        propertyRule.HasError = false; // Same value

        // Assert
        Assert.False(propertyChangedRaised);
    }

    /// <summary>
    /// Tests that setting ErrorMessage to same value does not raise PropertyChanged event.
    /// </summary>
    [Fact]
    public void ErrorMessage_SetToSameValue_DoesNotRaisePropertyChanged()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Error");
        propertyRule.ErrorMessage = "Test";

        var propertyChangedRaised = false;
        propertyRule.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PropertyRule.ErrorMessage))
                propertyChangedRaised = true;
        };

        // Act
        propertyRule.ErrorMessage = "Test"; // Same value

        // Assert
        Assert.False(propertyChangedRaised);
    }

    #endregion

    #region Rule Class Tests

    /// <summary>
    /// Tests that Rule Expression property is set correctly.
    /// </summary>
    [Fact]
    public void Rule_HasExpression_IsSetCorrectly()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Test");

        // Assert
        Assert.NotNull(propertyRule.Rules[0].Expression);
        Assert.False(propertyRule.Rules[0].Expression());
    }

    /// <summary>
    /// Tests that Rule Message property is set correctly.
    /// </summary>
    [Fact]
    public void Rule_HasMessage_IsSetCorrectly()
    {
        // Arrange
        var message = "Custom error message";
        var propertyRule = new PropertyRule(() => false, message);

        // Assert
        Assert.Equal(message, propertyRule.Rules[0].Message);
    }

    /// <summary>
    /// Tests that Rule HasError property is initially false.
    /// </summary>
    [Fact]
    public void Rule_HasError_InitiallyFalse()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => false, "Test");

        // Assert - HasError on the Rule class is not the same as on PropertyRule
        // The Rule.HasError field is set during ExecuteRules
        Assert.False(propertyRule.Rules[0].HasError);
    }

    #endregion

    #region Dynamic Rule Expression Tests

    /// <summary>
    /// Tests that ExecuteRules evaluates current state with dynamic expressions.
    /// </summary>
    [Fact]
    public void ExecuteRules_WithDynamicExpression_EvaluatesCurrentState()
    {
        // Arrange
        var value = 5;
        var propertyRule = new PropertyRule(() => value > 10, "Value is greater than 10");

        // Act & Assert - Initially no error
        propertyRule.ExecuteRules();
        Assert.False(propertyRule.HasError);

        // Change value to trigger error
        value = 15;
        propertyRule.ExecuteRules();
        Assert.True(propertyRule.HasError);

        // Change value back to no error
        value = 5;
        propertyRule.ExecuteRules();
        Assert.False(propertyRule.HasError);
    }

    /// <summary>
    /// Tests that ExecuteRules handles nullable comparisons correctly.
    /// </summary>
    [Fact]
    public void ExecuteRules_WithNullableComparison_HandlesCorrectly()
    {
        // Arrange
        string? value = null;
        var propertyRule = new PropertyRule(() => string.IsNullOrEmpty(value), "Value is required");

        // Act & Assert - Initially has error
        propertyRule.ExecuteRules();
        Assert.True(propertyRule.HasError);

        // Set value
        value = "Test";
        propertyRule.ExecuteRules();
        Assert.False(propertyRule.HasError);
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests that ExecuteRules handles empty error message correctly.
    /// </summary>
    [Fact]
    public void ExecuteRules_WithEmptyErrorMessage_HandlesCorrectly()
    {
        // Arrange
        var propertyRule = new PropertyRule(() => true, "");

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Equal("", propertyRule.ErrorMessage);
    }

    /// <summary>
    /// Tests that ExecuteRules handles very long error message correctly.
    /// </summary>
    [Fact]
    public void ExecuteRules_WithVeryLongErrorMessage_HandlesCorrectly()
    {
        // Arrange
        var longMessage = new string('A', 10000);
        var propertyRule = new PropertyRule(() => true, longMessage);

        // Act
        propertyRule.ExecuteRules();

        // Assert
        Assert.True(propertyRule.HasError);
        Assert.Equal(longMessage, propertyRule.ErrorMessage);
    }

    #endregion
}
