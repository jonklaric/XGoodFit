using System.Collections.ObjectModel;
using Numerics.Data;
using Numerics.Distributions;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.RowItems
{
    /// <summary>
    /// Tests for <see cref="DistributionRowItem"/>.
    /// </summary>
    public class DistributionRowItemTests
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
        /// Tests that the constructor with a normal distribution sets properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithNormalDistribution_SetsPropertiesCorrectly()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            double xVal = 50.0;

            // Act
            var item = new DistributionRowItem(xVal, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(xVal, item.X);
            Assert.NotNull(item.Distribution);
            Assert.Equal(100, item.P1); // Mean
            Assert.Equal(15, item.P2);  // StdDev
        }

        /// <summary>
        /// Tests that the constructor with a triangular distribution sets three parameters.
        /// </summary>
        [Fact]
        public void Constructor_WithTriangularDistribution_SetsThreeParameters()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Triangular(10, 50, 90);
            double xVal = 25.0;

            // Act
            var item = new DistributionRowItem(xVal, distribution, list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(xVal, item.X);
            Assert.Equal(10, item.P1); // Min
            Assert.Equal(50, item.P2); // Mode
            Assert.Equal(90, item.P3); // Max
        }

        /// <summary>
        /// Tests that the constructor sets min and max values correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsMinMaxValues()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);

            // Act
            var item = new DistributionRowItem(50.0, distribution, list, 10, 200, 20, 300, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(10, item.MinXValue);
            Assert.Equal(200, item.MaxXValue);
            Assert.Equal(20, item.MinYValue);
            Assert.Equal(300, item.MaxYValue);
        }

        /// <summary>
        /// Tests that the constructor sets sort order properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_SetsSortOrder()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);

            // Act
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, true, true, SortOrder.Ascending, SortOrder.Descending);
            list.Add(item);

            // Assert
            Assert.True(item.IsStrictX);
            Assert.True(item.IsStrictY);
            Assert.Equal(SortOrder.Ascending, item.XOrder);
            Assert.Equal(SortOrder.Descending, item.YOrder);
        }

        /// <summary>
        /// Tests that the constructor clones the distribution instead of using the same reference.
        /// </summary>
        [Fact]
        public void Constructor_ClonesDistribution()
        {
            // Arrange
            var list = CreateParentList();
            var originalDistribution = new Normal(100, 15);

            // Act
            var item = new DistributionRowItem(50.0, originalDistribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Modify original distribution
            originalDistribution.SetParameters(new[] { 200.0, 30.0 });

            // Assert - item should still have original values
            Assert.Equal(100, item.P1);
            Assert.Equal(15, item.P2);
        }

        #endregion

        #region X Property Tests

        /// <summary>
        /// Tests that setting X property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void X_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.X)) propertyChanged = true; };

            // Act
            item.X = 75.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(75.0, item.X);
        }

        #endregion

        #region Parameter Property Tests

        /// <summary>
        /// Tests that setting P1 raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void P1_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.P1)) propertyChanged = true; };

            // Act
            item.P1 = 120.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(120.0, item.P1);
        }

        /// <summary>
        /// Tests that setting P2 raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void P2_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.P2)) propertyChanged = true; };

            // Act
            item.P2 = 20.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(20.0, item.P2);
        }

        /// <summary>
        /// Tests that setting P3 raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void P3_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Triangular(10, 50, 90);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.P3)) propertyChanged = true; };

            // Act
            item.P3 = 95.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(95.0, item.P3);
        }

        #endregion

        #region Distribution Computed Properties Tests

        /// <summary>
        /// Tests that Mean property returns the correct value for valid parameters.
        /// </summary>
        [Fact]
        public void Mean_WithValidParameters_ReturnsCorrectValue()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(100, item.Mean, 5);
        }

        /// <summary>
        /// Tests that Minimum property returns a value less than the mean for a normal distribution.
        /// </summary>
        [Fact]
        public void Minimum_WithValidParameters_ReturnsValue()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert - Minimum should be less than mean for a normal distribution
            Assert.True(item.Minimum < item.Mean);
        }

        /// <summary>
        /// Tests that Maximum property returns a value greater than the mean for a normal distribution.
        /// </summary>
        [Fact]
        public void Maximum_WithValidParameters_ReturnsValue()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert - Maximum should be greater than mean for a normal distribution
            Assert.True(item.Maximum > item.Mean);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Tests that X value below minimum triggers a validation error.
        /// </summary>
        [Fact]
        public void X_BelowMinimum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(5.0, distribution, list, 10, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that X value above maximum triggers a validation error.
        /// </summary>
        [Fact]
        public void X_AboveMaximum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(1500.0, distribution, list, 10, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that valid X value within bounds has no validation error.
        /// </summary>
        [Fact]
        public void ValidXValue_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(500.0, distribution, list, 10, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["X"].HasError);
        }

        #endregion

        #region Min/Max Bounds Property Tests

        /// <summary>
        /// Tests that setting MinXValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MinXValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MinXValue)) propertyChanged = true; };

            // Act
            item.MinXValue = 5.0;

            // Assert
            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Tests that setting MaxXValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MaxXValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MaxXValue)) propertyChanged = true; };

            // Act
            item.MaxXValue = 2000.0;

            // Assert
            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Tests that setting MinYValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MinYValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MinYValue)) propertyChanged = true; };

            // Act
            item.MinYValue = -100.0;

            // Assert
            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Tests that setting MaxYValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MaxYValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MaxYValue)) propertyChanged = true; };

            // Act
            item.MaxYValue = 1000.0;

            // Assert
            Assert.True(propertyChanged);
        }

        #endregion

        #region IsStrict and SortOrder Property Tests

        /// <summary>
        /// Tests that setting IsStrictX raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void IsStrictX_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.IsStrictX)) propertyChanged = true; };

            // Act
            item.IsStrictX = true;

            // Assert
            Assert.True(propertyChanged);
            Assert.True(item.IsStrictX);
        }

        /// <summary>
        /// Tests that setting IsStrictY raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void IsStrictY_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.IsStrictY)) propertyChanged = true; };

            // Act
            item.IsStrictY = true;

            // Assert
            Assert.True(propertyChanged);
            Assert.True(item.IsStrictY);
        }

        /// <summary>
        /// Tests that setting XOrder raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void XOrder_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.XOrder)) propertyChanged = true; };

            // Act
            item.XOrder = SortOrder.Ascending;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(SortOrder.Ascending, item.XOrder);
        }

        /// <summary>
        /// Tests that setting YOrder raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void YOrder_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.YOrder)) propertyChanged = true; };

            // Act
            item.YOrder = SortOrder.Descending;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(SortOrder.Descending, item.YOrder);
        }

        #endregion

        #region PropertyDisplayName Tests

        /// <summary>
        /// Tests that PropertyDisplayName for P1 returns the distribution parameter name.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_P1_ReturnsDistributionParameterName()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("P1");

            // Assert
            Assert.NotNull(displayName);
            Assert.NotEmpty(displayName);
        }

        /// <summary>
        /// Tests that PropertyDisplayName for unknown property returns the property name.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_UnknownProperty_ReturnsPropertyName()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("UnknownProperty");

            // Assert
            Assert.Equal("UnknownProperty", displayName);
        }

        #endregion

        #region IsGridDisplayable Tests

        /// <summary>
        /// Tests that IsGridDisplayable returns true for X property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_X_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.True(item.IsGridDisplayable("X"));
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns true for P1 property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_P1_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.True(item.IsGridDisplayable("P1"));
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns true for P2 for normal distribution.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_P2_ReturnsTrueForNormal()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert - Normal has 2 parameters
            Assert.True(item.IsGridDisplayable("P2"));
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns true for P3 for triangular distribution.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_P3_ReturnsTrueForTriangular()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Triangular(10, 50, 90);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert - Triangular has 3 parameters
            Assert.True(item.IsGridDisplayable("P3"));
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns false for unknown properties.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_Unknown_ReturnsFalse()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.False(item.IsGridDisplayable("SomeUnknownProperty"));
        }

        #endregion

        #region RaisePropertyChanged Tests

        /// <summary>
        /// Tests that RaisePropertyChanged raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void RaisePropertyChanged_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item = new DistributionRowItem(50.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => propertyChanged = true;

            // Act
            item.RaisePropertyChanged();

            // Assert
            Assert.True(propertyChanged);
        }

        #endregion

        #region Ordering Tests

        /// <summary>
        /// Tests that ascending X order with values in correct order has no validation error.
        /// </summary>
        [Fact]
        public void AscendingXOrder_ValuesInOrder_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item1 = new DistributionRowItem(10.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new DistributionRowItem(20.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);

            // Act
            item1.ForceValidation();
            item2.ForceValidation();

            // Assert
            Assert.False(item1.RuleMap["X"].HasError);
            Assert.False(item2.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that ascending X order with values out of order triggers a validation error.
        /// </summary>
        [Fact]
        public void AscendingXOrder_ValuesOutOfOrder_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Normal(100, 15);
            var item1 = new DistributionRowItem(20.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new DistributionRowItem(10.0, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert
            Assert.True(item2.RuleMap["X"].HasError);
        }

        #endregion

        #region Different Distribution Types Tests

        /// <summary>
        /// Tests that constructor with uniform distribution sets parameters correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithUniformDistribution_SetsParametersCorrectly()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Uniform(0, 100);
            double xVal = 50.0;

            // Act
            var item = new DistributionRowItem(xVal, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(0, item.P1);   // Min
            Assert.Equal(100, item.P2); // Max
        }

        /// <summary>
        /// Tests that constructor with exponential distribution sets parameters correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithExponentialDistribution_SetsParametersCorrectly()
        {
            // Arrange
            var list = CreateParentList();
            var distribution = new Exponential(0, 0.5);
            double xVal = 25.0;

            // Act
            var item = new DistributionRowItem(xVal, distribution, list, 0, 1000, 0, 500, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            // Exponential(Xi, Lambda) - P1 = Xi (location), P2 = Lambda (rate)
            Assert.Equal(0, item.P1, 5);   // Xi (location parameter)
            Assert.Equal(0.5, item.P2, 5); // Lambda (rate parameter)
        }

        #endregion
    }
}
