using System.Collections.ObjectModel;
using Numerics.Data;
using NumericControls;
using Xunit;

namespace NumericControls.Tests.RowItems
{
    /// <summary>
    /// Tests for <see cref="OrdinateRowItem"/>.
    /// </summary>
    public class OrdinateRowItemTests
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
        /// Tests that the constructor correctly sets all properties with valid parameters.
        /// </summary>
        [Fact]
        public void Constructor_WithValidParameters_SetsPropertiesCorrectly()
        {
            // Arrange
            var list = CreateParentList();
            double xVal = 10.0;
            double yVal = 20.0;

            // Act
            var item = new OrdinateRowItem(xVal, yVal, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Assert
            Assert.Equal(xVal, item.X);
            Assert.Equal(yVal, item.Y);
            Assert.Equal(0, item.MinXValue);
            Assert.Equal(100, item.MaxXValue);
            Assert.Equal(0, item.MinYValue);
            Assert.Equal(100, item.MaxYValue);
        }

        /// <summary>
        /// Tests that the constructor sets the IsStrictX and IsStrictY flags correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithStrictOrdering_SetsIsStrictFlags()
        {
            // Arrange
            var list = CreateParentList();

            // Act
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, true, true, SortOrder.Ascending, SortOrder.Ascending);
            list.Add(item);

            // Assert
            Assert.True(item.IsStrictX);
            Assert.True(item.IsStrictY);
        }

        /// <summary>
        /// Tests that the constructor sets the sort order properties correctly.
        /// </summary>
        [Fact]
        public void Constructor_WithSortOrder_SetsSortOrderProperties()
        {
            // Arrange
            var list = CreateParentList();

            // Act
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.Descending);
            list.Add(item);

            // Assert
            Assert.Equal(SortOrder.Ascending, item.XOrder);
            Assert.Equal(SortOrder.Descending, item.YOrder);
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
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.X)) propertyChanged = true; };

            // Act
            item.X = 15.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(15.0, item.X);
        }

        /// <summary>
        /// Tests that setting X to the same value does not raise PropertyChanged.
        /// </summary>
        [Fact]
        public void X_SetSameValue_DoesNotNotifyPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.X)) propertyChanged = true; };

            // Act
            item.X = 5.0;

            // Assert
            Assert.False(propertyChanged);
        }

        #endregion

        #region Y Property Tests

        /// <summary>
        /// Tests that setting Y property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Y_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.Y)) propertyChanged = true; };

            // Act
            item.Y = 25.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(25.0, item.Y);
        }

        /// <summary>
        /// Tests that setting Y to NaN raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Y_SetNaN_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.Y)) propertyChanged = true; };

            // Act
            item.Y = double.NaN;

            // Assert
            Assert.True(propertyChanged);
            Assert.True(double.IsNaN(item.Y));
        }

        /// <summary>
        /// Tests that setting Y to the same value does not raise PropertyChanged.
        /// </summary>
        [Fact]
        public void Y_SetSameValue_DoesNotNotifyPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.Y)) propertyChanged = true; };

            // Act
            item.Y = 10.0;

            // Assert
            Assert.False(propertyChanged);
        }

        #endregion

        #region MinMax Bounds Tests

        /// <summary>
        /// Tests that setting MinXValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MinXValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MinXValue)) propertyChanged = true; };

            // Act
            item.MinXValue = -10.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(-10.0, item.MinXValue);
        }

        /// <summary>
        /// Tests that setting MaxXValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MaxXValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MaxXValue)) propertyChanged = true; };

            // Act
            item.MaxXValue = 200.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(200.0, item.MaxXValue);
        }

        /// <summary>
        /// Tests that setting MinYValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MinYValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MinYValue)) propertyChanged = true; };

            // Act
            item.MinYValue = -50.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(-50.0, item.MinYValue);
        }

        /// <summary>
        /// Tests that setting MaxYValue raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void MaxYValue_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.MaxYValue)) propertyChanged = true; };

            // Act
            item.MaxYValue = 500.0;

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal(500.0, item.MaxYValue);
        }

        #endregion

        #region Validation Tests

        /// <summary>
        /// Tests that X value below minimum triggers a validation error.
        /// </summary>
        [Fact]
        public void XValue_BelowMinimum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 10, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act - X is 5.0 but minimum is 10
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that X value above maximum triggers a validation error.
        /// </summary>
        [Fact]
        public void XValue_AboveMaximum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(150.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that Y value below minimum triggers a validation error.
        /// </summary>
        [Fact]
        public void YValue_BelowMinimum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(50.0, -5.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Y"].HasError);
        }

        /// <summary>
        /// Tests that Y value above maximum triggers a validation error.
        /// </summary>
        [Fact]
        public void YValue_AboveMaximum_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(50.0, 150.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.True(item.RuleMap["Y"].HasError);
        }

        /// <summary>
        /// Tests that valid values within bounds do not trigger validation errors.
        /// </summary>
        [Fact]
        public void ValidValues_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(50.0, 50.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            item.ForceValidation();

            // Assert
            Assert.False(item.RuleMap["X"].HasError);
            Assert.False(item.RuleMap["Y"].HasError);
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
            var item1 = new OrdinateRowItem(10.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new OrdinateRowItem(20.0, 20.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            var item3 = new OrdinateRowItem(30.0, 30.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);
            list.Add(item3);

            // Act
            item1.ForceValidation();
            item2.ForceValidation();
            item3.ForceValidation();

            // Assert
            Assert.False(item1.RuleMap["X"].HasError);
            Assert.False(item2.RuleMap["X"].HasError);
            Assert.False(item3.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that ascending X order with values out of order triggers a validation error.
        /// </summary>
        [Fact]
        public void AscendingXOrder_ValuesOutOfOrder_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new OrdinateRowItem(10.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new OrdinateRowItem(5.0, 20.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert
            Assert.True(item2.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that descending X order with values in correct order has no validation error.
        /// </summary>
        [Fact]
        public void DescendingXOrder_ValuesInOrder_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new OrdinateRowItem(30.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Descending, SortOrder.None);
            var item2 = new OrdinateRowItem(20.0, 20.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Descending, SortOrder.None);
            var item3 = new OrdinateRowItem(10.0, 30.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Descending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);
            list.Add(item3);

            // Act
            item1.ForceValidation();
            item2.ForceValidation();
            item3.ForceValidation();

            // Assert
            Assert.False(item1.RuleMap["X"].HasError);
            Assert.False(item2.RuleMap["X"].HasError);
            Assert.False(item3.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that ascending Y order with values in correct order has no validation error.
        /// </summary>
        [Fact]
        public void AscendingYOrder_ValuesInOrder_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new OrdinateRowItem(10.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.Ascending);
            var item2 = new OrdinateRowItem(20.0, 20.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.Ascending);
            list.Add(item1);
            list.Add(item2);

            // Act
            item1.ForceValidation();
            item2.ForceValidation();

            // Assert
            Assert.False(item1.RuleMap["Y"].HasError);
            Assert.False(item2.RuleMap["Y"].HasError);
        }

        /// <summary>
        /// Tests that strict ascending X order with equal values triggers a validation error.
        /// </summary>
        [Fact]
        public void StrictAscendingX_EqualValues_HasValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new OrdinateRowItem(10.0, 10.0, "X", "Y", list, 0, 100, 0, 100, true, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new OrdinateRowItem(10.0, 20.0, "X", "Y", list, 0, 100, 0, 100, true, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert
            Assert.True(item2.RuleMap["X"].HasError);
        }

        /// <summary>
        /// Tests that non-strict ascending X order with equal values has no validation error.
        /// </summary>
        [Fact]
        public void NonStrictAscendingX_EqualValues_NoValidationError()
        {
            // Arrange
            var list = CreateParentList();
            var item1 = new OrdinateRowItem(10.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            var item2 = new OrdinateRowItem(10.0, 20.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.Ascending, SortOrder.None);
            list.Add(item1);
            list.Add(item2);

            // Act
            item2.ForceValidation();

            // Assert
            Assert.False(item2.RuleMap["X"].HasError);
        }

        #endregion

        #region GetOrdinate Tests

        /// <summary>
        /// Tests that GetOrdinate returns an ordinate with correct X and Y values.
        /// </summary>
        [Fact]
        public void GetOrdinate_ReturnsOrdinateWithCorrectValues()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(15.0, 25.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var ordinate = item.GetOrdinate();

            // Assert
            Assert.Equal(15.0, ordinate.X);
            Assert.Equal(25.0, ordinate.Y);
        }

        #endregion

        #region PropertyDisplayName Tests

        /// <summary>
        /// Tests that PropertyDisplayName for X returns the X column header.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_X_ReturnsXColumnHeader()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "CustomX", "CustomY", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("X");

            // Assert
            Assert.Equal("CustomX", displayName);
        }

        /// <summary>
        /// Tests that PropertyDisplayName for Y returns the Y column header.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_Y_ReturnsYColumnHeader()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "CustomX", "CustomY", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("Y");

            // Assert
            Assert.Equal("CustomY", displayName);
        }

        /// <summary>
        /// Tests that PropertyDisplayName for other properties returns the property name itself.
        /// </summary>
        [Fact]
        public void PropertyDisplayName_OtherProperty_ReturnsPropertyName()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "CustomX", "CustomY", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var displayName = item.PropertyDisplayName("SomeOtherProperty");

            // Assert
            Assert.Equal("SomeOtherProperty", displayName);
        }

        #endregion

        #region IsGridDisplayable Tests

        /// <summary>
        /// Tests that IsGridDisplayable returns true for the X property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_X_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var isDisplayable = item.IsGridDisplayable("X");

            // Assert
            Assert.True(isDisplayable);
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns true for the Y property.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_Y_ReturnsTrue()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var isDisplayable = item.IsGridDisplayable("Y");

            // Assert
            Assert.True(isDisplayable);
        }

        /// <summary>
        /// Tests that IsGridDisplayable returns false for non-displayable properties.
        /// </summary>
        [Fact]
        public void IsGridDisplayable_OtherProperty_ReturnsFalse()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);

            // Act
            var isDisplayable = item.IsGridDisplayable("MinXValue");

            // Assert
            Assert.False(isDisplayable);
        }

        #endregion

        #region RaisePropertyChanged Tests

        /// <summary>
        /// Tests that RaisePropertyChanged notifies all properties.
        /// </summary>
        [Fact]
        public void RaisePropertyChanged_NotifiesAllProperties()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => propertyChanged = true;

            // Act
            item.RaisePropertyChanged();

            // Assert
            Assert.True(propertyChanged);
        }

        #endregion

        #region IsStrict Property Tests

        /// <summary>
        /// Tests that setting IsStrictX raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void IsStrictX_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
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
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
            list.Add(item);
            bool propertyChanged = false;
            item.PropertyChanged += (s, e) => { if (e.PropertyName == nameof(item.IsStrictY)) propertyChanged = true; };

            // Act
            item.IsStrictY = true;

            // Assert
            Assert.True(propertyChanged);
            Assert.True(item.IsStrictY);
        }

        #endregion

        #region SortOrder Property Tests

        /// <summary>
        /// Tests that setting XOrder raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void XOrder_SetValue_NotifiesPropertyChanged()
        {
            // Arrange
            var list = CreateParentList();
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
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
            var item = new OrdinateRowItem(5.0, 10.0, "X", "Y", list, 0, 100, 0, 100, false, false, SortOrder.None, SortOrder.None);
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
    }
}
