using Xunit;

namespace OxyPlotControls.Tests.Extensions;

/// <summary>
/// Tests for GetFirstAbstractBaseType extension method.
/// </summary>
public class GetFirstAbstractBaseTypeTests
{
    // Test helper classes
    private abstract class AbstractBase { }
    private class ConcreteChild : AbstractBase { }
    private class ConcreteGrandChild : ConcreteChild { }
    private class StandaloneClass { }

    [Fact]
    public void GetFirstAbstractBaseType_NullType_ThrowsArgumentNullException()
    {
        // Arrange
        Type? nullType = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullType!.GetFirstAbstractBaseType());
    }

    [Fact]
    public void GetFirstAbstractBaseType_AbstractTypeItself_ReturnsNull()
    {
        // Arrange
        var type = typeof(AbstractBase);

        // Act
        var result = type.GetFirstAbstractBaseType();

        // Assert - Abstract class's base type is object which is not abstract, so returns null
        Assert.Null(result);
    }

    [Fact]
    public void GetFirstAbstractBaseType_ConcreteChildOfAbstract_ReturnsAbstractBase()
    {
        // Arrange
        var type = typeof(ConcreteChild);

        // Act
        var result = type.GetFirstAbstractBaseType();

        // Assert
        Assert.Equal(typeof(AbstractBase), result);
    }

    [Fact]
    public void GetFirstAbstractBaseType_GrandChildOfAbstract_ReturnsAbstractBase()
    {
        // Arrange
        var type = typeof(ConcreteGrandChild);

        // Act
        var result = type.GetFirstAbstractBaseType();

        // Assert
        Assert.Equal(typeof(AbstractBase), result);
    }

    [Fact]
    public void GetFirstAbstractBaseType_StandaloneClass_ReturnsObjectType()
    {
        // Arrange
        var type = typeof(StandaloneClass);

        // Act
        var result = type.GetFirstAbstractBaseType();

        // Assert - StandaloneClass inherits from object, which is not abstract
        Assert.Null(result);
    }

    [Fact]
    public void GetFirstAbstractBaseType_ObjectType_ReturnsNull()
    {
        // Arrange
        var type = typeof(object);

        // Act
        var result = type.GetFirstAbstractBaseType();

        // Assert - object has no base type
        Assert.Null(result);
    }
}
