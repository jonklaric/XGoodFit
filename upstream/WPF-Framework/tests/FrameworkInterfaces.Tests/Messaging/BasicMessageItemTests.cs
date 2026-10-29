using Xunit;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using System.ComponentModel;

namespace FrameworkInterfaces.Tests.Messaging
{
    /// <summary>
    /// Test class for the BasicMessageItem implementation, providing comprehensive tests for constructors,
    /// properties, property change notifications, equality comparisons, and text formatting.
    /// </summary>
    public class BasicMessageItemTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that the default constructor sets default values for Code and TimeStamp properties.
        /// </summary>
        [Fact]
        public void DefaultConstructor_SetsDefaultValues()
        {
            var item = new BasicMessageItem();

            Assert.Equal("MI-EVT-000", item.Code);
            Assert.NotNull(item.TimeStamp);
            Assert.Matches(@"\d{2}:\d{2}:\d{2}", item.TimeStamp);
        }

        /// <summary>
        /// Verifies that the parameterized constructor correctly sets all properties from the provided values.
        /// </summary>
        [Fact]
        public void ParameterizedConstructor_SetsAllProperties()
        {
            var source = new object();
            var item = new BasicMessageItem(
                MessageType.Error,
                "Test description",
                source,
                "TestCollection",
                "TestElement",
                "TestParameter",
                "TST-001");

            Assert.Equal(MessageType.Error, item.Type);
            Assert.Equal("Test description", item.Description);
            Assert.Same(source, item.Source);
            Assert.Equal("TestCollection", item.SourceCollectionName);
            Assert.Equal("TestElement", item.SourceName);
            Assert.Equal("TestParameter", item.ParameterName);
            Assert.Equal("TST-001", item.Code);
        }

        /// <summary>
        /// Verifies that the parameterized constructor uses default values when optional parameters are null.
        /// </summary>
        [Fact]
        public void ParameterizedConstructor_WithNullOptionalParameter_UsesDefaults()
        {
            var source = new object();
            var item = new BasicMessageItem(
                MessageType.Warning,
                "Warning message",
                source,
                "Collection",
                "Element");

            Assert.Equal(MessageType.Warning, item.Type);
            Assert.Null(item.ParameterName);
            Assert.Equal("MI-EVT-000", item.Code);
        }

        #endregion

        #region Property Tests

        /// <summary>
        /// Verifies that the Type property can be set and retrieved correctly for various MessageType values.
        /// </summary>
        /// <param name="type">The MessageType value to test.</param>
        [Theory]
        [InlineData(MessageType.Error)]
        [InlineData(MessageType.Warning)]
        [InlineData(MessageType.Message)]
        [InlineData(MessageType.Event)]
        public void Type_SetAndGet_ReturnsExpectedValue(MessageType type)
        {
            var item = new BasicMessageItem { Type = type };
            Assert.Equal(type, item.Type);
        }

        /// <summary>
        /// Verifies that changing the Type property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Type_WhenChanged_RaisesPropertyChanged()
        {
            var item = new BasicMessageItem();
            var propertyChanged = false;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BasicMessageItem.Type))
                    propertyChanged = true;
            };

            // Use Warning since default is Error (enum value 0)
            item.Type = MessageType.Warning;

            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Verifies that setting the Type property to the same value does not raise the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Type_WhenSetToSameValue_DoesNotRaisePropertyChanged()
        {
            var item = new BasicMessageItem { Type = MessageType.Warning };
            var propertyChanged = false;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BasicMessageItem.Type))
                    propertyChanged = true;
            };

            item.Type = MessageType.Warning;

            Assert.False(propertyChanged);
        }

        /// <summary>
        /// Verifies that setting the Code property to null defaults it to the event code "MI-EVT-000".
        /// </summary>
        [Fact]
        public void Code_SetNull_DefaultsToEventCode()
        {
            var item = new BasicMessageItem { Code = "TST-001" };
            item.Code = null!;
            Assert.Equal("MI-EVT-000", item.Code);
        }

        /// <summary>
        /// Verifies that changing the Code property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Code_WhenChanged_RaisesPropertyChanged()
        {
            var item = new BasicMessageItem();
            var propertyChanged = false;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BasicMessageItem.Code))
                    propertyChanged = true;
            };

            item.Code = "NEW-001";

            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Verifies that the Description property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Description_SetAndGet_ReturnsExpectedValue()
        {
            var item = new BasicMessageItem { Description = "Test description" };
            Assert.Equal("Test description", item.Description);
        }

        /// <summary>
        /// Verifies that changing the Description property raises the PropertyChanged event.
        /// </summary>
        [Fact]
        public void Description_WhenChanged_RaisesPropertyChanged()
        {
            var item = new BasicMessageItem();
            var propertyChanged = false;
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(BasicMessageItem.Description))
                    propertyChanged = true;
            };

            item.Description = "New description";

            Assert.True(propertyChanged);
        }

        /// <summary>
        /// Verifies that the Source property can be set and retrieved correctly.
        /// </summary>
        [Fact]
        public void Source_SetAndGet_ReturnsExpectedValue()
        {
            var source = new object();
            var item = new BasicMessageItem { Source = source };
            Assert.Same(source, item.Source);
        }

        /// <summary>
        /// Verifies that the MessageAction property can be set and invoked correctly.
        /// </summary>
        [Fact]
        public void MessageAction_SetAndGet_ReturnsExpectedValue()
        {
            var actionCalled = false;
            Action<IMessageItem> action = _ => actionCalled = true;

            var item = new BasicMessageItem { MessageAction = action };

            Assert.NotNull(item.MessageAction);
            item.MessageAction(item);
            Assert.True(actionCalled);
        }

        #endregion

        #region ToText Tests

        /// <summary>
        /// Verifies that the ToText method returns a properly formatted string containing all message item details.
        /// </summary>
        [Fact]
        public void ToText_ReturnsFormattedString()
        {
            var item = new BasicMessageItem
            {
                Type = MessageType.Error,
                Description = "Test error",
                SourceCollectionName = "Elements",
                SourceName = "Element1",
                ParameterName = "Param1"
            };

            var result = item.ToText();

            Assert.Contains("Type: Error", result);
            Assert.Contains("Description: Test error", result);
            Assert.Contains("Source: Elements", result);
            Assert.Contains("Name: Element1", result);
            Assert.Contains("Parameter: Param1", result);
        }

        /// <summary>
        /// Verifies that the ToString method returns the same result as the ToText method.
        /// </summary>
        [Fact]
        public void ToString_ReturnsToTextResult()
        {
            var item = new BasicMessageItem
            {
                Type = MessageType.Warning,
                Description = "Warning message"
            };

            Assert.Equal(item.ToText(), item.ToString());
        }

        #endregion

        #region Equality Tests

        /// <summary>
        /// Verifies that the Equals method returns false when comparing to null.
        /// </summary>
        [Fact]
        public void Equals_NullObject_ReturnsFalse()
        {
            var item = new BasicMessageItem();
            Assert.False(item.Equals(null));
        }

        /// <summary>
        /// Verifies that the Equals method returns false when comparing to a non-MessageItem object.
        /// </summary>
        [Fact]
        public void Equals_NonMessageItem_ReturnsFalse()
        {
            var item = new BasicMessageItem();
            Assert.False(item.Equals("not a message item"));
        }

        /// <summary>
        /// Verifies that the Equals method returns false for items with the same code but different sources.
        /// </summary>
        [Fact]
        public void Equals_SameCodeDifferentSource_ReturnsFalse()
        {
            var item1 = new BasicMessageItem { Code = "TST-001", Source = new object() };
            var item2 = new BasicMessageItem { Code = "TST-001", Source = new object() };

            Assert.False(item1.Equals(item2));
        }

        /// <summary>
        /// Verifies that the Equals method returns false when both items have null sources.
        /// </summary>
        [Fact]
        public void Equals_NullSourceOnBoth_ReturnsFalse()
        {
            var item1 = new BasicMessageItem { Code = "TST-001", Source = null };
            var item2 = new BasicMessageItem { Code = "TST-001", Source = null };

            Assert.False(item1.Equals(item2));
        }

        /// <summary>
        /// Verifies that the Equals method returns false for items with different codes.
        /// </summary>
        [Fact]
        public void Equals_DifferentCodes_ReturnsFalse()
        {
            var source = new object();
            var item1 = new BasicMessageItem { Code = "TST-001", Source = source };
            var item2 = new BasicMessageItem { Code = "TST-002", Source = source };

            Assert.False(item1.Equals(item2));
        }

        /// <summary>
        /// Verifies that the equality operator returns true when both items are null.
        /// </summary>
        [Fact]
        public void OperatorEquals_BothNull_ReturnsTrue()
        {
            BasicMessageItem? item1 = null;
            BasicMessageItem? item2 = null;

            Assert.True(item1! == item2!);
        }

        /// <summary>
        /// Verifies that the equality operator returns false when one item is null.
        /// </summary>
        [Fact]
        public void OperatorEquals_OneNull_ReturnsFalse()
        {
            var item1 = new BasicMessageItem();
            BasicMessageItem? item2 = null;

            Assert.False(item1 == item2!);
        }

        /// <summary>
        /// Verifies that the inequality operator returns true for different items.
        /// </summary>
        [Fact]
        public void OperatorNotEquals_DifferentItems_ReturnsTrue()
        {
            var item1 = new BasicMessageItem { Code = "TST-001", Source = new object() };
            var item2 = new BasicMessageItem { Code = "TST-002", Source = new object() };

            Assert.True(item1 != item2);
        }

        /// <summary>
        /// Verifies that GetHashCode returns the same hash code for items with the same properties.
        /// </summary>
        [Fact]
        public void GetHashCode_SameProperties_ReturnsSameHashCode()
        {
            var item1 = new BasicMessageItem { Code = "TST-001" };
            var item2 = new BasicMessageItem { Code = "TST-001" };

            Assert.Equal(item1.GetHashCode(), item2.GetHashCode());
        }

        /// <summary>
        /// Verifies that GetHashCode returns different hash codes for items with different codes.
        /// </summary>
        [Fact]
        public void GetHashCode_DifferentCodes_ReturnsDifferentHashCodes()
        {
            var item1 = new BasicMessageItem { Code = "TST-001" };
            var item2 = new BasicMessageItem { Code = "TST-002" };

            Assert.NotEqual(item1.GetHashCode(), item2.GetHashCode());
        }

        #endregion
    }
}
