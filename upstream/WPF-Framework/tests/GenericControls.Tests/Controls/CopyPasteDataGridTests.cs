using Xunit;

namespace GenericControls.Tests.Controls;

/// <summary>
/// Unit tests for the <see cref="CopyPasteDataGrid"/> class static methods.
/// </summary>
public class CopyPasteDataGridTests
{
    #region IsNumericType Tests - Primitive Types

    /// <summary>
    /// Tests that IsNumericType returns true for all primitive numeric types.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData(typeof(byte), true)]
    [InlineData(typeof(sbyte), true)]
    [InlineData(typeof(short), true)]
    [InlineData(typeof(ushort), true)]
    [InlineData(typeof(int), true)]
    [InlineData(typeof(uint), true)]
    [InlineData(typeof(long), true)]
    [InlineData(typeof(ulong), true)]
    [InlineData(typeof(float), true)]
    [InlineData(typeof(double), true)]
    [InlineData(typeof(decimal), true)]
    public void IsNumericType_NumericTypes_ReturnsTrue(Type type, bool expected)
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(type);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for non-numeric types.
    /// </summary>
    /// <param name="type">The non-numeric type to test.</param>
    [Theory]
    [InlineData(typeof(bool))]
    [InlineData(typeof(char))]
    [InlineData(typeof(string))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(object))]
    [InlineData(typeof(Guid))]
    public void IsNumericType_NonNumericTypes_ReturnsFalse(Type type)
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(type);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNumericType Tests - Nullable Types

    /// <summary>
    /// Tests that IsNumericType returns true for nullable numeric types.
    /// </summary>
    /// <param name="type">The nullable numeric type to test.</param>
    /// <param name="expected">The expected result.</param>
    [Theory]
    [InlineData(typeof(byte?), true)]
    [InlineData(typeof(sbyte?), true)]
    [InlineData(typeof(short?), true)]
    [InlineData(typeof(ushort?), true)]
    [InlineData(typeof(int?), true)]
    [InlineData(typeof(uint?), true)]
    [InlineData(typeof(long?), true)]
    [InlineData(typeof(ulong?), true)]
    [InlineData(typeof(float?), true)]
    [InlineData(typeof(double?), true)]
    [InlineData(typeof(decimal?), true)]
    public void IsNumericType_NullableNumericTypes_ReturnsTrue(Type type, bool expected)
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(type);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for nullable non-numeric types.
    /// </summary>
    /// <param name="type">The nullable non-numeric type to test.</param>
    [Theory]
    [InlineData(typeof(bool?))]
    [InlineData(typeof(char?))]
    [InlineData(typeof(DateTime?))]
    [InlineData(typeof(Guid?))]
    public void IsNumericType_NullableNonNumericTypes_ReturnsFalse(Type type)
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(type);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNumericType Tests - Null

    /// <summary>
    /// Tests that IsNumericType returns false for null type.
    /// </summary>
    [Fact]
    public void IsNumericType_NullType_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(null!);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNumericType Tests - Complex Types

    /// <summary>
    /// Tests that IsNumericType returns true for enum types since they have numeric underlying types.
    /// </summary>
    [Fact]
    public void IsNumericType_EnumType_ReturnsTrue()
    {
        // Enums have numeric underlying types (Int32 by default), so Type.GetTypeCode()
        // returns the underlying type code, making them appear numeric
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(DayOfWeek));

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for array types.
    /// </summary>
    [Fact]
    public void IsNumericType_ArrayType_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(int[]));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for generic list types.
    /// </summary>
    [Fact]
    public void IsNumericType_ListType_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(List<int>));

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsDoubleType Tests

    /// <summary>
    /// Tests that IsDoubleType returns true for double type.
    /// </summary>
    [Fact]
    public void IsDoubleType_Double_ReturnsTrue()
    {
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(typeof(double));

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// Tests that IsDoubleType returns false for other numeric types.
    /// </summary>
    /// <param name="type">The numeric type to test.</param>
    [Theory]
    [InlineData(typeof(float))]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(byte))]
    public void IsDoubleType_OtherNumericTypes_ReturnsFalse(Type type)
    {
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(type);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsDoubleType returns false for non-numeric types.
    /// </summary>
    /// <param name="type">The non-numeric type to test.</param>
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(object))]
    public void IsDoubleType_NonNumericTypes_ReturnsFalse(Type type)
    {
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(type);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsDoubleType returns false for null type.
    /// </summary>
    [Fact]
    public void IsDoubleType_NullType_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(null!);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsDoubleType returns false for nullable double type.
    /// </summary>
    [Fact]
    public void IsDoubleType_NullableDouble_ReturnsFalse()
    {
        // Note: The implementation only checks for double, not Nullable<double>
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(typeof(double?));

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Consistency Tests

    /// <summary>
    /// Tests that IsNumericType returns consistent results when called multiple times.
    /// </summary>
    [Fact]
    public void IsNumericType_CalledMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        var type = typeof(int);

        // Act
        var result1 = CopyPasteDataGrid.IsNumericType(type);
        var result2 = CopyPasteDataGrid.IsNumericType(type);
        var result3 = CopyPasteDataGrid.IsNumericType(type);

        // Assert
        Assert.True(result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Tests that IsDoubleType returns consistent results when called multiple times.
    /// </summary>
    [Fact]
    public void IsDoubleType_CalledMultipleTimes_ReturnsSameResult()
    {
        // Arrange
        var type = typeof(double);

        // Act
        var result1 = CopyPasteDataGrid.IsDoubleType(type);
        var result2 = CopyPasteDataGrid.IsDoubleType(type);
        var result3 = CopyPasteDataGrid.IsDoubleType(type);

        // Assert
        Assert.True(result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    #endregion

    #region Comparison Between GeneralMethods and CopyPasteDataGrid

    /// <summary>
    /// Tests that IsNumericType in CopyPasteDataGrid returns the same result as GeneralMethods for numeric types.
    /// </summary>
    /// <param name="type">The numeric type to test.</param>
    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(int))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(long))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double))]
    [InlineData(typeof(decimal))]
    public void IsNumericType_SameAsGeneralMethods_ForNumericTypes(Type type)
    {
        // Act
        var copyPasteResult = CopyPasteDataGrid.IsNumericType(type);
        var generalMethodsResult = GeneralMethods.IsNumericType(type);

        // Assert - Both implementations should agree
        Assert.Equal(generalMethodsResult, copyPasteResult);
    }

    /// <summary>
    /// Tests that IsNumericType in CopyPasteDataGrid returns the same result as GeneralMethods for non-numeric types.
    /// </summary>
    /// <param name="type">The non-numeric type to test.</param>
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(object))]
    public void IsNumericType_SameAsGeneralMethods_ForNonNumericTypes(Type type)
    {
        // Act
        var copyPasteResult = CopyPasteDataGrid.IsNumericType(type);
        var generalMethodsResult = GeneralMethods.IsNumericType(type);

        // Assert - Both implementations should agree
        Assert.Equal(generalMethodsResult, copyPasteResult);
    }

    #endregion

    #region Type Verification Tests

    /// <summary>
    /// Tests that IsNumericType returns true for all integer types.
    /// </summary>
    [Fact]
    public void IsNumericType_AllIntegerTypes_ReturnsTrue()
    {
        // Arrange
        var integerTypes = new[]
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong)
        };

        // Act & Assert
        foreach (var type in integerTypes)
        {
            Assert.True(CopyPasteDataGrid.IsNumericType(type), $"{type.Name} should be numeric");
        }
    }

    /// <summary>
    /// Tests that IsNumericType returns true for all floating-point types.
    /// </summary>
    [Fact]
    public void IsNumericType_AllFloatingPointTypes_ReturnsTrue()
    {
        // Arrange
        var floatingTypes = new[] { typeof(float), typeof(double), typeof(decimal) };

        // Act & Assert
        foreach (var type in floatingTypes)
        {
            Assert.True(CopyPasteDataGrid.IsNumericType(type), $"{type.Name} should be numeric");
        }
    }

    /// <summary>
    /// Tests that IsDoubleType returns true only for the double type among all numeric types.
    /// </summary>
    [Fact]
    public void IsDoubleType_OnlyDoubleReturnsTrue()
    {
        // Arrange - All numeric types
        var numericTypes = new[]
        {
            typeof(byte), typeof(sbyte),
            typeof(short), typeof(ushort),
            typeof(int), typeof(uint),
            typeof(long), typeof(ulong),
            typeof(float), typeof(double), typeof(decimal)
        };

        // Act & Assert
        foreach (var type in numericTypes)
        {
            var result = CopyPasteDataGrid.IsDoubleType(type);
            if (type == typeof(double))
            {
                Assert.True(result, $"{type.Name} should return true");
            }
            else
            {
                Assert.False(result, $"{type.Name} should return false");
            }
        }
    }

    #endregion

    #region Edge Cases

    /// <summary>
    /// Tests that IsNumericType returns false for IntPtr type.
    /// </summary>
    [Fact]
    public void IsNumericType_IntPtr_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(IntPtr));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for UIntPtr type.
    /// </summary>
    [Fact]
    public void IsNumericType_UIntPtr_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(UIntPtr));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsDoubleType returns false for IntPtr type.
    /// </summary>
    [Fact]
    public void IsDoubleType_IntPtr_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsDoubleType(typeof(IntPtr));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for custom struct types.
    /// </summary>
    [Fact]
    public void IsNumericType_CustomStruct_ReturnsFalse()
    {
        // Act
        var result = CopyPasteDataGrid.IsNumericType(typeof(CustomTestStruct));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Custom test struct for testing type checking methods.
    /// </summary>
#pragma warning disable CS0649 // Field is never assigned to (used for type-checking tests)
    private struct CustomTestStruct
    {
        /// <summary>
        /// Test integer value field.
        /// </summary>
        public int Value;
    }
#pragma warning restore CS0649

    #endregion
}
