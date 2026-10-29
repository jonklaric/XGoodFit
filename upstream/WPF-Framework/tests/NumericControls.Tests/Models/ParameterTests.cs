using NumericControls;
using Xunit;

namespace NumericControls.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="Parameter"/>.
    /// </summary>
    public class ParameterTests
    {
        #region Constructor Tests

        /// <summary>
        /// Tests that the constructor correctly sets the Name property.
        /// </summary>
        [Fact]
        public void Constructor_SetsName()
        {
            // Arrange & Act
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert
            Assert.Equal("Mean", parameter.Name);
        }

        /// <summary>
        /// Tests that the constructor correctly sets the DisplayName property.
        /// </summary>
        [Fact]
        public void Constructor_SetsDisplayName()
        {
            // Arrange & Act
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert
            Assert.Equal("Mean (mu)", parameter.DisplayName);
        }

        /// <summary>
        /// Tests that the constructor correctly sets the Value property.
        /// </summary>
        [Fact]
        public void Constructor_SetsValue()
        {
            // Arrange & Act
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert
            Assert.Equal(100.0, parameter.Value);
        }

        /// <summary>
        /// Tests that the constructor sets IsValid to true by default.
        /// </summary>
        [Fact]
        public void Constructor_SetsIsValidToTrue()
        {
            // Arrange & Act
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert
            Assert.True(parameter.IsValid);
        }

        /// <summary>
        /// Tests that the constructor sets ErrorMessage to null by default.
        /// </summary>
        [Fact]
        public void Constructor_SetsErrorMessageToNull()
        {
            // Arrange & Act
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert
            Assert.Null(parameter.ErrorMessage);
        }

        /// <summary>
        /// Tests that the constructor correctly handles a zero value.
        /// </summary>
        [Fact]
        public void Constructor_WithZeroValue_SetsCorrectly()
        {
            // Arrange & Act
            var parameter = new Parameter("Scale", "Scale", 0.0);

            // Assert
            Assert.Equal(0.0, parameter.Value);
        }

        /// <summary>
        /// Tests that the constructor correctly handles a negative value.
        /// </summary>
        [Fact]
        public void Constructor_WithNegativeValue_SetsCorrectly()
        {
            // Arrange & Act
            var parameter = new Parameter("Shift", "Shift", -50.0);

            // Assert
            Assert.Equal(-50.0, parameter.Value);
        }

        /// <summary>
        /// Tests that the constructor correctly handles NaN as a value.
        /// </summary>
        [Fact]
        public void Constructor_WithNaN_SetsCorrectly()
        {
            // Arrange & Act
            var parameter = new Parameter("Test", "Test", double.NaN);

            // Assert
            Assert.True(double.IsNaN(parameter.Value));
        }

        /// <summary>
        /// Tests that the constructor correctly handles positive infinity as a value.
        /// </summary>
        [Fact]
        public void Constructor_WithInfinity_SetsCorrectly()
        {
            // Arrange & Act
            var parameter = new Parameter("Test", "Test", double.PositiveInfinity);

            // Assert
            Assert.True(double.IsPositiveInfinity(parameter.Value));
        }

        #endregion

        #region Value Property Tests

        /// <summary>
        /// Tests that setting the Value property updates it correctly.
        /// </summary>
        [Fact]
        public void Value_SetNewValue_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = 150.0;

            // Assert
            Assert.Equal(150.0, parameter.Value);
        }

        /// <summary>
        /// Tests that setting a new Value raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Value_SetNewValue_RaisesPropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.Value))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.Value = 150.0;

            // Assert
            Assert.True(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that setting the same Value does not raise the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Value_SetSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.Value))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.Value = 100.0;

            // Assert
            Assert.False(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that the Value property can be set to zero.
        /// </summary>
        [Fact]
        public void Value_SetToZero_UpdatesCorrectly()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = 0.0;

            // Assert
            Assert.Equal(0.0, parameter.Value);
        }

        /// <summary>
        /// Tests that the Value property can be set to a negative value.
        /// </summary>
        [Fact]
        public void Value_SetToNegative_UpdatesCorrectly()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = -50.0;

            // Assert
            Assert.Equal(-50.0, parameter.Value);
        }

        /// <summary>
        /// Tests that the Value property can be set to NaN.
        /// </summary>
        [Fact]
        public void Value_SetToNaN_UpdatesCorrectly()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = double.NaN;

            // Assert
            Assert.True(double.IsNaN(parameter.Value));
        }

        /// <summary>
        /// Tests that the Value property can be set to Double.MaxValue.
        /// </summary>
        [Fact]
        public void Value_SetToMaxValue_UpdatesCorrectly()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = double.MaxValue;

            // Assert
            Assert.Equal(double.MaxValue, parameter.Value);
        }

        /// <summary>
        /// Tests that the Value property can be set to Double.MinValue.
        /// </summary>
        [Fact]
        public void Value_SetToMinValue_UpdatesCorrectly()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.Value = double.MinValue;

            // Assert
            Assert.Equal(double.MinValue, parameter.Value);
        }

        #endregion

        #region IsValid Property Tests

        /// <summary>
        /// Tests that setting IsValid to false updates the property correctly.
        /// </summary>
        [Fact]
        public void IsValid_SetToFalse_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.IsValid = false;

            // Assert
            Assert.False(parameter.IsValid);
        }

        /// <summary>
        /// Tests that setting IsValid to false raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void IsValid_SetToFalse_RaisesPropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.IsValid))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.IsValid = false;

            // Assert
            Assert.True(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that setting IsValid to the same value does not raise PropertyChanged.
        /// </summary>
        [Fact]
        public void IsValid_SetSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.IsValid))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.IsValid = true; // Same as initial value

            // Assert
            Assert.False(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that IsValid can be set to true after being false.
        /// </summary>
        [Fact]
        public void IsValid_SetToTrue_AfterFalse_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            parameter.IsValid = false;

            // Act
            parameter.IsValid = true;

            // Assert
            Assert.True(parameter.IsValid);
        }

        #endregion

        #region ErrorMessage Property Tests

        /// <summary>
        /// Tests that setting ErrorMessage updates the property correctly.
        /// </summary>
        [Fact]
        public void ErrorMessage_SetMessage_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            string errorMessage = "Value must be positive";

            // Act
            parameter.ErrorMessage = errorMessage;

            // Assert
            Assert.Equal(errorMessage, parameter.ErrorMessage);
        }

        /// <summary>
        /// Tests that setting ErrorMessage raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void ErrorMessage_SetMessage_RaisesPropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.ErrorMessage))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.ErrorMessage = "Error occurred";

            // Assert
            Assert.True(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that setting the same ErrorMessage does not raise PropertyChanged.
        /// </summary>
        [Fact]
        public void ErrorMessage_SetSameMessage_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            parameter.ErrorMessage = "Error";
            bool propertyChangedRaised = false;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.ErrorMessage))
                    propertyChangedRaised = true;
            };

            // Act
            parameter.ErrorMessage = "Error"; // Same value

            // Assert
            Assert.False(propertyChangedRaised);
        }

        /// <summary>
        /// Tests that ErrorMessage can be set to null.
        /// </summary>
        [Fact]
        public void ErrorMessage_SetToNull_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            parameter.ErrorMessage = "Some error";

            // Act
            parameter.ErrorMessage = null;

            // Assert
            Assert.Null(parameter.ErrorMessage);
        }

        /// <summary>
        /// Tests that ErrorMessage can be set to an empty string.
        /// </summary>
        [Fact]
        public void ErrorMessage_SetToEmptyString_UpdatesValue()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            parameter.ErrorMessage = "Some error";

            // Act
            parameter.ErrorMessage = "";

            // Assert
            Assert.Equal("", parameter.ErrorMessage);
        }

        #endregion

        #region Combined Validation Tests

        /// <summary>
        /// Tests that setting invalid state updates both IsValid and ErrorMessage.
        /// </summary>
        [Fact]
        public void SetInvalidState_UpdatesBothIsValidAndErrorMessage()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act
            parameter.IsValid = false;
            parameter.ErrorMessage = "Value must be greater than 0";

            // Assert
            Assert.False(parameter.IsValid);
            Assert.Equal("Value must be greater than 0", parameter.ErrorMessage);
        }

        /// <summary>
        /// Tests that clearing invalid state resets both IsValid and ErrorMessage.
        /// </summary>
        [Fact]
        public void ClearInvalidState_ResetsBothIsValidAndErrorMessage()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            parameter.IsValid = false;
            parameter.ErrorMessage = "Error";

            // Act
            parameter.IsValid = true;
            parameter.ErrorMessage = null;

            // Assert
            Assert.True(parameter.IsValid);
            Assert.Null(parameter.ErrorMessage);
        }

        #endregion

        #region PropertyChanged Event Tests

        /// <summary>
        /// Tests that changing Value raises PropertyChanged event once.
        /// </summary>
        [Fact]
        public void PropertyChanged_ValueChange_RaisesOnce()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            int changeCount = 0;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.Value))
                    changeCount++;
            };

            // Act
            parameter.Value = 150.0;

            // Assert
            Assert.Equal(1, changeCount);
        }

        /// <summary>
        /// Tests that multiple Value changes raise PropertyChanged multiple times.
        /// </summary>
        [Fact]
        public void PropertyChanged_MultipleValueChanges_RaisesMultipleTimes()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);
            int changeCount = 0;
            parameter.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(parameter.Value))
                    changeCount++;
            };

            // Act
            parameter.Value = 150.0;
            parameter.Value = 200.0;
            parameter.Value = 250.0;

            // Assert
            Assert.Equal(3, changeCount);
        }

        /// <summary>
        /// Tests that changing Value without subscribers does not throw an exception.
        /// </summary>
        [Fact]
        public void PropertyChanged_NoSubscriber_DoesNotThrow()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean", 100.0);

            // Act & Assert - should not throw
            var exception = Record.Exception(() => parameter.Value = 150.0);
            Assert.Null(exception);
        }

        #endregion

        #region Immutable Properties Tests

        /// <summary>
        /// Tests that Name property is read-only and cannot be changed after construction.
        /// </summary>
        [Fact]
        public void Name_IsReadOnly_CannotBeChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert - Name has no setter, this test verifies the design
            Assert.Equal("Mean", parameter.Name);
        }

        /// <summary>
        /// Tests that DisplayName property is read-only and cannot be changed after construction.
        /// </summary>
        [Fact]
        public void DisplayName_IsReadOnly_CannotBeChanged()
        {
            // Arrange
            var parameter = new Parameter("Mean", "Mean (mu)", 100.0);

            // Assert - DisplayName has no setter, this test verifies the design
            Assert.Equal("Mean (mu)", parameter.DisplayName);
        }

        #endregion

        #region Common Distribution Parameter Tests

        /// <summary>
        /// Tests creating a normal distribution mean parameter.
        /// </summary>
        [Fact]
        public void NormalDistribution_MeanParameter()
        {
            // Arrange & Act
            var meanParam = new Parameter("Mean", "mu (Mean)", 0.0);

            // Assert
            Assert.Equal("Mean", meanParam.Name);
            Assert.Equal("mu (Mean)", meanParam.DisplayName);
            Assert.Equal(0.0, meanParam.Value);
        }

        /// <summary>
        /// Tests creating a normal distribution standard deviation parameter.
        /// </summary>
        [Fact]
        public void NormalDistribution_StdDevParameter()
        {
            // Arrange & Act
            var stdDevParam = new Parameter("StdDev", "sigma (Std Dev)", 1.0);

            // Assert
            Assert.Equal("StdDev", stdDevParam.Name);
            Assert.Equal("sigma (Std Dev)", stdDevParam.DisplayName);
            Assert.Equal(1.0, stdDevParam.Value);
        }

        /// <summary>
        /// Tests creating a uniform distribution minimum parameter.
        /// </summary>
        [Fact]
        public void UniformDistribution_MinParameter()
        {
            // Arrange & Act
            var minParam = new Parameter("Min", "Minimum", 0.0);

            // Assert
            Assert.Equal("Min", minParam.Name);
            Assert.Equal("Minimum", minParam.DisplayName);
        }

        /// <summary>
        /// Tests creating a uniform distribution maximum parameter.
        /// </summary>
        [Fact]
        public void UniformDistribution_MaxParameter()
        {
            // Arrange & Act
            var maxParam = new Parameter("Max", "Maximum", 100.0);

            // Assert
            Assert.Equal("Max", maxParam.Name);
            Assert.Equal("Maximum", maxParam.DisplayName);
        }

        /// <summary>
        /// Tests creating a triangular distribution mode parameter.
        /// </summary>
        [Fact]
        public void TriangularDistribution_ModeParameter()
        {
            // Arrange & Act
            var modeParam = new Parameter("Mode", "Mode (Most Likely)", 50.0);

            // Assert
            Assert.Equal("Mode", modeParam.Name);
            Assert.Equal("Mode (Most Likely)", modeParam.DisplayName);
            Assert.Equal(50.0, modeParam.Value);
        }

        #endregion
    }
}
