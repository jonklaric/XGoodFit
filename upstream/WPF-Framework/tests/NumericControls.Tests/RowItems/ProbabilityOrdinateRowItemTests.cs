using System.Collections.ObjectModel;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.RowItems
{
    /// <summary>
    /// Tests for <see cref="ProbabilityOrdinateRowItem"/>.
    /// </summary>
    public class ProbabilityOrdinateRowItemTests
    {
        /// <summary>
        /// Creates a parent ObservableCollection for testing row items.
        /// </summary>
        /// <returns>An empty ObservableCollection of objects.</returns>
        private ObservableCollection<object> CreateParentList()
        {
            return new ObservableCollection<object>();
        }

        #region Constructor Tests

        /// <summary>
        /// Tests that the default constructor initializes with default values.
        /// </summary>
        [Fact]
        public void DefaultConstructor_InitializesWithDefaults()
        {
            // Act
            var item = new ProbabilityOrdinateRowItem();

            // Assert
            Assert.Equal(0.0, item.Probability);
        }

        /// <summary>
        /// Tests that the constructor with probability parameter sets it correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithProbability_SetsProbabilityCorrectly()
        {
            // Arrange
            var list = CreateParentList();
            double probability = 0.5;

            // Act
            var item = new ProbabilityOrdinateRowItem(list, probability);
            list.Add(item);

            // Assert
            Assert.Equal(probability, item.Probability);
        }

        /// <summary>
        /// Tests that the constructor with zero probability sets it correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithZeroProbability_SetsProbabilityCorrectly()
        {
            // Arrange
            var list = CreateParentList();

            // Act
            var item = new ProbabilityOrdinateRowItem(list, 0.0);
            list.Add(item);

            // Assert
            Assert.Equal(0.0, item.Probability);
        }

        /// <summary>
        /// Tests that the constructor with probability of 1.0 sets it correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithOneProbability_SetsProbabilityCorrectly()
        {
            // Arrange
            var list = CreateParentList();

            // Act
            var item = new ProbabilityOrdinateRowItem(list, 1.0);
            list.Add(item);

            // Assert
            Assert.Equal(1.0, item.Probability);
        }

        #endregion

        #region Probability Property Tests

        /// <summary>
        /// Tests that setting Probability raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Probability_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.Probability)) propertyChanged = true; };

            // Act
            item.Probability = 0.75;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(0.75, item.Probability);
        }

        /// <summary>
        /// Tests that setting Probability to the same value does not raise PropertyChanged.
        /// </summary>
        [Fact]
        public void Probability_SetSameValue_DoesNotNotifyPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.Probability)) propertyChanged = true; };

            // Act
            item.Probability = 0.5;

            // Assert
            Assert.False(propertyChanged);
        }

        #endregion

        #region Range Validation Tests (0 to 1)

        /// <summary>
        /// Tests that a valid probability value within range has no validation error.
        /// </summary>
        [Fact]
        public void Probability_ValidValue_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a probability value of zero has no validation error.
        /// </summary>
        [Fact]
        public void Probability_ZeroValue_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.0);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a probability value of one has no validation error.
        /// </summary>
        [Fact]
        public void Probability_OneValue_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 1.0);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a negative probability value triggers a validation error.
        /// </summary>
        [Fact]
        public void Probability_NegativeValue_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, -0.1);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a probability value greater than one triggers a validation error.
        /// </summary>
        [Fact]
        public void Probability_GreaterThanOne_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 1.1);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a very small positive probability has no validation error.
        /// </summary>
        [Fact]
        public void Probability_VerySmallPositive_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.0001);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a probability close to one has no validation error.
        /// </summary>
        [Fact]
        public void Probability_CloseToOne_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.9999);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        #endregion

        #region Ascending Order Validation Tests

        /// <summary>
        /// Tests that ascending order with values in correct order has no validation error.
        /// </summary>
        [Fact]
        public void AscendingOrder_ValuesInOrder_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new ProbabilityOrdinateRowItem(list, 0.1);
            var item2 = new ProbabilityOrdinateRowItem(list, 0.5);
            var item3 = new ProbabilityOrdinateRowItem(list, 0.9);
            list.Add(item1);
            list.Add(item2);
            list.Add(item3);

            // Act
            item1.ForceValidation();
            item2.ForceValidation();
            item3.ForceValidation();

            // Assert
            Assert.False(item1.RuleMap["Probability"].HasError);
            Assert.False(item2.RuleMap["Probability"].HasError);
            Assert.False(item3.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that ascending order with values out of order triggers a validation error.
        /// </summary>
        [Fact]
        public void AscendingOrder_ValuesOutOfOrder_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new ProbabilityOrdinateRowItem(list, 0.5);
            var item2 = new ProbabilityOrdinateRowItem(list, 0.3);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert
            Assert.True(item2.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that ascending order with equal values triggers a validation error (strict ordering).
        /// </summary>
        [Fact]
        public void AscendingOrder_EqualValues_HasValidationError()
        {
            // Arrange - strict ascending order (no duplicates)
            var list = CreateParentList();
            var item1 = new ProbabilityOrdinateRowItem(list, 0.5);
            var item2 = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert - strict ordering means equal values should have an error
            Assert.True(item2.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that multiple items in ascending order are all valid.
        /// </summary>
        [Fact]
        public void AscendingOrder_MultipleItems_AllValidInOrder()
        {
            // Arrange
            var list = CreateParentList();
            var items = new[]
            {
                new ProbabilityOrdinateRowItem(list, 0.01),
                new ProbabilityOrdinateRowItem(list, 0.05),
                new ProbabilityOrdinateRowItem(list, 0.10),
                new ProbabilityOrdinateRowItem(list, 0.25),
                new ProbabilityOrdinateRowItem(list, 0.50),
                new ProbabilityOrdinateRowItem(list, 0.75),
                new ProbabilityOrdinateRowItem(list, 0.90),
                new ProbabilityOrdinateRowItem(list, 0.95),
                new ProbabilityOrdinateRowItem(list, 0.99)
            };

            foreach (var item in items)
            {
                list.Add(item);
            }

            // Act & Assert
            foreach (var item in items)
            {
                item.ForceValidation();
                Assert.False(item.RuleMap["Probability"].HasError);
            }
        }

        /// <summary>
        /// Tests that a single item has no order validation error.
        /// </summary>
        [Fact]
        public void SingleItem_NoOrderValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        #endregion

        #region PropertyDisplayName Tests

        /// <summary>
        /// Tests that PropertyDisplayName for Probability returns "Probability Ordinates".
        /// </summary>
        [Fact]
        public void PropertyDisplayName_Probability_ReturnsProbabilityOrdinates()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("Probability");

            // Assert
            Assert.Equal("Probability Ordinates", displayName);
        }

        /// <summary>
        /// Tests that PropertyDisplayName for other properties returns null.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_OtherProperty_ReturnsNull()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("SomeOtherProperty");

            // Assert
            Assert.Null(displayName);
        }

        #endregion

        #region IsGridDisplayable Tests

        /// <summary>
        /// Tests that IsGridDisplayable returns true for Probability property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_Probability_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            var isDisplayable = item.IsGridDisplayable("Probability");

            // Assert
            Assert.True(isDisplayable);
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns true for any property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_AnyProperty_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act & Assert - according to implementation, always returns true
            Assert.True(item.IsGridDisplayable("Probability"));
            Assert.True(item.IsGridDisplayable("AnyOtherProperty"));
        }

        #endregion

        #region Boundary Value Tests

        /// <summary>
        /// Tests that a probability just below zero triggers a validation error.
        /// </summary>
        [Fact]
        public void Probability_JustBelowZero_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, -0.0000001);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a probability just above one triggers a validation error.
        /// </summary>
        [Fact]
        public void Probability_JustAboveOne_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 1.0000001);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that a middle probability value has no validation error.
        /// </summary>
        [Fact]
        public void Probability_MiddleValue_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, 0.5);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        #endregion

        #region Common Probability Values Tests

        /// <summary>
        /// Tests that common probability values are all valid.
        /// </summary>
        /// <param name="probability">The probability value to test.</param>
        [Theory]
        [InlineData(0.01)]
        [InlineData(0.05)]
        [InlineData(0.10)]
        [InlineData(0.25)]
        [InlineData(0.50)]
        [InlineData(0.75)]
        [InlineData(0.90)]
        [InlineData(0.95)]
        [InlineData(0.99)]
        public void CommonProbabilityValues_AreValid(double probability)
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, probability);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["Probability"].HasError);
        }

        /// <summary>
        /// Tests that invalid probability values trigger validation errors.
        /// </summary>
        /// <param name="probability">The invalid probability value to test.</param>
        [Theory]
        [InlineData(-0.5)]
        [InlineData(-1.0)]
        [InlineData(1.5)]
        [InlineData(2.0)]
        [InlineData(100.0)]
        public void InvalidProbabilityValues_HaveValidationError(double probability)
        {
            // Arrange
            var list = CreateParentList();
            var item = new ProbabilityOrdinateRowItem(list, probability);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Probability"].HasError);
        }

        #endregion
    }
}
