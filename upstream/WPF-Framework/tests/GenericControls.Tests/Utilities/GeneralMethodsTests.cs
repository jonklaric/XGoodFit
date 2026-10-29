using Xunit;

namespace GenericControls.Tests.Utilities;

/// <summary>
/// Unit tests for the <see cref="GeneralMethods"/> class.
/// </summary>
public class GeneralMethodsTests
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
        var result = GeneralMethods.IsNumericType(type);

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
        var result = GeneralMethods.IsNumericType(type);

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
        var result = GeneralMethods.IsNumericType(type);

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
        var result = GeneralMethods.IsNumericType(type);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region IsNumericType Tests - Null

    /// <summary>
    /// Tests that IsNumericType returns false when given a null type.
    /// </summary>
    [Fact]
    public void IsNumericType_NullType_ReturnsFalse()
    {
        // Act
        var result = GeneralMethods.IsNumericType(null!);

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
        var result = GeneralMethods.IsNumericType(typeof(DayOfWeek));

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
        var result = GeneralMethods.IsNumericType(typeof(int[]));

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
        var result = GeneralMethods.IsNumericType(typeof(List<int>));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for custom class types.
    /// </summary>
    [Fact]
    public void IsNumericType_CustomClass_ReturnsFalse()
    {
        // Act
        var result = GeneralMethods.IsNumericType(typeof(GeneralMethodsTests));

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
        var result = GeneralMethods.IsNumericType(typeof(CustomStruct));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Custom test struct for testing type checking methods.
    /// </summary>
#pragma warning disable CS0649 // Field is never assigned to (used for type-checking tests)
    private struct CustomStruct
    {
        /// <summary>
        /// Test integer value field.
        /// </summary>
        public int Value;
    }
#pragma warning restore CS0649

    #endregion

    #region IsNumericType Tests - Edge Cases

    /// <summary>
    /// Tests that IsNumericType returns false for IntPtr type.
    /// </summary>
    [Fact]
    public void IsNumericType_IntPtr_ReturnsFalse()
    {
        // Act
        var result = GeneralMethods.IsNumericType(typeof(IntPtr));

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
        var result = GeneralMethods.IsNumericType(typeof(UIntPtr));

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Tests that IsNumericType returns false for TimeSpan type.
    /// </summary>
    [Fact]
    public void IsNumericType_TimeSpan_ReturnsFalse()
    {
        // Act
        var result = GeneralMethods.IsNumericType(typeof(TimeSpan));

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Random Color Lists Tests

    /// <summary>
    /// Tests that RandomColorsShortList has the expected number of colors.
    /// </summary>
    [Fact]
    public void RandomColorsShortList_HasExpectedLength()
    {
        // Assert
        Assert.Equal(63, GeneralMethods.RandomColorsShortList.Length);
    }

    /// <summary>
    /// Tests that RandomColorsLongList has the expected number of colors.
    /// </summary>
    [Fact]
    public void RandomColorsLongList_HasExpectedLength()
    {
        // Assert
        Assert.Equal(1024, GeneralMethods.RandomColorsLongList.Length);
    }

    /// <summary>
    /// Tests that all colors in RandomColorsShortList are valid hex color strings.
    /// </summary>
    [Fact]
    public void RandomColorsShortList_ContainsValidHexColors()
    {
        // Assert
        foreach (var color in GeneralMethods.RandomColorsShortList)
        {
            Assert.StartsWith("#", color);
            Assert.Equal(7, color.Length); // #RRGGBB format
        }
    }

    /// <summary>
    /// Tests that all colors in RandomColorsLongList are valid hex color strings.
    /// </summary>
    [Fact]
    public void RandomColorsLongList_ContainsValidHexColors()
    {
        // Assert
        foreach (var color in GeneralMethods.RandomColorsLongList)
        {
            Assert.StartsWith("#", color);
            Assert.Equal(7, color.Length); // #RRGGBB format
        }
    }

    /// <summary>
    /// Tests that the first color in RandomColorsShortList is black.
    /// </summary>
    [Fact]
    public void RandomColorsShortList_FirstColorIsBlack()
    {
        // Assert - The first color should be black
        Assert.Equal("#000000", GeneralMethods.RandomColorsShortList[0]);
    }

    /// <summary>
    /// Tests that the first color in RandomColorsLongList is black.
    /// </summary>
    [Fact]
    public void RandomColorsLongList_FirstColorIsBlack()
    {
        // Assert - The first color should be black
        Assert.Equal("#000000", GeneralMethods.RandomColorsLongList[0]);
    }

    /// <summary>
    /// Tests that all colors in RandomColorsShortList are unique.
    /// </summary>
    [Fact]
    public void RandomColorsShortList_AllColorsAreUnique()
    {
        // Arrange & Act
        var distinctColors = GeneralMethods.RandomColorsShortList.Distinct().Count();

        // Assert - All colors should be unique
        Assert.Equal(GeneralMethods.RandomColorsShortList.Length, distinctColors);
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
            Assert.True(GeneralMethods.IsNumericType(type), $"{type.Name} should be numeric");
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
            Assert.True(GeneralMethods.IsNumericType(type), $"{type.Name} should be numeric");
        }
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
        var result1 = GeneralMethods.IsNumericType(type);
        var result2 = GeneralMethods.IsNumericType(type);
        var result3 = GeneralMethods.IsNumericType(type);

        // Assert
        Assert.True(result1);
        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    /// <summary>
    /// Tests that IsNumericType returns true for both nullable and non-nullable integer types.
    /// </summary>
    [Fact]
    public void IsNumericType_NullableIntAndInt_BothReturnTrue()
    {
        // Act
        var intResult = GeneralMethods.IsNumericType(typeof(int));
        var nullableIntResult = GeneralMethods.IsNumericType(typeof(int?));

        // Assert
        Assert.True(intResult);
        Assert.True(nullableIntResult);
    }

    #endregion
}
