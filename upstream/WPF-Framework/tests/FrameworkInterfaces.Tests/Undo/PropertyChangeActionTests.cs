using Xunit;
using FrameworkInterfaces.Undo.Actions;
using System.ComponentModel;

namespace FrameworkInterfaces.Tests.Undo
{
    /// <summary>
    /// Test class for the PropertyChangeAction implementation, providing comprehensive tests for property
    /// value changes, undo/redo functionality, action merging, and timestamp-based merge windows.
    /// </summary>
    public class PropertyChangeActionTests
    {
        #region Test Helpers

        /// <summary>
        /// Test object that implements INotifyPropertyChanged for testing property change tracking.
        /// </summary>
        private class TestObject : INotifyPropertyChanged
        {
            /// <summary>
            /// Backing field for the Name property.
            /// </summary>
            private string _name = string.Empty;

            /// <summary>
            /// Backing field for the Value property.
            /// </summary>
            private int _value;

            /// <summary>
            /// Gets or sets the name of the test object.
            /// </summary>
            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }

            /// <summary>
            /// Gets or sets the numeric value of the test object.
            /// </summary>
            public int Value
            {
                get => _value;
                set
                {
                    _value = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            public event PropertyChangedEventHandler? PropertyChanged;
        }

        #endregion

        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor correctly sets all properties from the provided values.
        /// </summary>
        [Fact]
        public void Constructor_SetsProperties()
        {
            var target = new TestObject { Name = "Test" };
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Old", "New");

            Assert.Same(target, action.Target);
            Assert.Equal(nameof(TestObject.Name), action.PropertyName);
            Assert.Equal("Old", action.OldValue);
            Assert.Equal("New", action.NewValue);
        }

        /// <summary>
        /// Verifies that attempting to create a PropertyChangeAction with a null target throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when target parameter is null.</exception>
        [Fact]
        public void Constructor_NullTarget_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new PropertyChangeAction(null!, "Name", "Old", "New"));
        }

        /// <summary>
        /// Verifies that attempting to create a PropertyChangeAction with a null property name throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when propertyName parameter is null.</exception>
        [Fact]
        public void Constructor_NullPropertyName_ThrowsArgumentNullException()
        {
            var target = new TestObject();

            Assert.Throws<ArgumentNullException>(() =>
                new PropertyChangeAction(target, null!, "Old", "New"));
        }

        /// <summary>
        /// Verifies that attempting to create a PropertyChangeAction with an invalid property name throws an ArgumentException.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when the property name does not exist on the target object.</exception>
        [Fact]
        public void Constructor_InvalidPropertyName_ThrowsArgumentException()
        {
            var target = new TestObject();

            Assert.Throws<ArgumentException>(() =>
                new PropertyChangeAction(target, "NonExistentProperty", "Old", "New"));
        }

        /// <summary>
        /// Verifies that the constructor sets a timestamp within the expected time range.
        /// </summary>
        [Fact]
        public void Constructor_SetsTimestamp()
        {
            var before = DateTime.Now;
            var target = new TestObject();
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Old", "New");
            var after = DateTime.Now;

            Assert.True(action.Timestamp >= before);
            Assert.True(action.Timestamp <= after);
        }

        #endregion

        #region Description Tests

        /// <summary>
        /// Verifies that the Description property contains the property name being changed.
        /// </summary>
        [Fact]
        public void Description_ReturnsPropertyChangeName()
        {
            var target = new TestObject();
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Old", "New");

            Assert.Contains("Name", action.Description);
            Assert.Contains("Change", action.Description);
        }

        #endregion

        #region Execute Tests

        /// <summary>
        /// Verifies that executing the action sets the property to the new value.
        /// </summary>
        [Fact]
        public void Execute_SetsNewValue()
        {
            var target = new TestObject { Name = "Old" };
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Old", "New");

            action.Execute();

            Assert.Equal("New", target.Name);
        }

        /// <summary>
        /// Verifies that executing the action works correctly with integer properties.
        /// </summary>
        [Fact]
        public void Execute_WithIntProperty_SetsNewValue()
        {
            var target = new TestObject { Value = 10 };
            var action = new PropertyChangeAction(target, nameof(TestObject.Value), 10, 20);

            action.Execute();

            Assert.Equal(20, target.Value);
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that undoing the action restores the property to the old value.
        /// </summary>
        [Fact]
        public void Undo_RestoresOldValue()
        {
            var target = new TestObject { Name = "New" };
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Old", "New");

            action.Undo();

            Assert.Equal("Old", target.Name);
        }

        /// <summary>
        /// Verifies that undoing the action works correctly with integer properties.
        /// </summary>
        [Fact]
        public void Undo_WithIntProperty_RestoresOldValue()
        {
            var target = new TestObject { Value = 20 };
            var action = new PropertyChangeAction(target, nameof(TestObject.Value), 10, 20);

            action.Undo();

            Assert.Equal(10, target.Value);
        }

        /// <summary>
        /// Verifies that executing and then undoing the action correctly applies and reverses the property change.
        /// </summary>
        [Fact]
        public void Execute_ThenUndo_RestoresOriginalState()
        {
            var target = new TestObject { Name = "Original" };
            var action = new PropertyChangeAction(target, nameof(TestObject.Name), "Original", "Changed");

            action.Execute();
            Assert.Equal("Changed", target.Name);

            action.Undo();
            Assert.Equal("Original", target.Name);
        }

        #endregion

        #region Merge Tests

        /// <summary>
        /// Verifies that CanMergeWith returns true for actions on the same target and property within the merge window.
        /// </summary>
        /// <returns>True when actions can be merged.</returns>
        [Fact]
        public void CanMergeWith_SameTargetAndProperty_WithinWindow_ReturnsTrue()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            var action2 = new PropertyChangeAction(target, nameof(TestObject.Name), "B", "C");

            Assert.True(action1.CanMergeWith(action2));
        }

        /// <summary>
        /// Verifies that CanMergeWith returns false for actions on different targets.
        /// </summary>
        /// <returns>False when targets differ.</returns>
        [Fact]
        public void CanMergeWith_DifferentTarget_ReturnsFalse()
        {
            var target1 = new TestObject();
            var target2 = new TestObject();
            var action1 = new PropertyChangeAction(target1, nameof(TestObject.Name), "A", "B");
            var action2 = new PropertyChangeAction(target2, nameof(TestObject.Name), "B", "C");

            Assert.False(action1.CanMergeWith(action2));
        }

        /// <summary>
        /// Verifies that CanMergeWith returns false for actions on different properties.
        /// </summary>
        /// <returns>False when properties differ.</returns>
        [Fact]
        public void CanMergeWith_DifferentProperty_ReturnsFalse()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            var action2 = new PropertyChangeAction(target, nameof(TestObject.Value), 1, 2);

            Assert.False(action1.CanMergeWith(action2));
        }

        /// <summary>
        /// Verifies that CanMergeWith returns false when passed a non-PropertyChangeAction.
        /// </summary>
        /// <returns>False when action types differ.</returns>
        [Fact]
        public void CanMergeWith_NonPropertyChangeAction_ReturnsFalse()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            var action2 = new DelegateAction("Test", () => { }, () => { });

            Assert.False(action1.CanMergeWith(action2));
        }

        /// <summary>
        /// Verifies that merging two actions combines the old value from the first action with the new value from the second.
        /// </summary>
        /// <returns>A new PropertyChangeAction with combined values.</returns>
        [Fact]
        public void MergeWith_ReturnsActionWithOldValueFromFirstAndNewValueFromSecond()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            var action2 = new PropertyChangeAction(target, nameof(TestObject.Name), "B", "C");

            var merged = action1.MergeWith(action2) as PropertyChangeAction;

            Assert.NotNull(merged);
            Assert.Equal("A", merged.OldValue);
            Assert.Equal("C", merged.NewValue);
        }

        /// <summary>
        /// Verifies that attempting to merge with a non-PropertyChangeAction returns the original action.
        /// </summary>
        /// <returns>The original action instance.</returns>
        [Fact]
        public void MergeWith_NonPropertyChangeAction_ReturnsOriginal()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            var action2 = new DelegateAction("Test", () => { }, () => { });

            var result = action1.MergeWith(action2);

            Assert.Same(action1, result);
        }

        /// <summary>
        /// Verifies that the merged action uses the timestamp from the later action.
        /// </summary>
        [Fact]
        public void MergeWith_UsesLaterTimestamp()
        {
            var target = new TestObject();
            var action1 = new PropertyChangeAction(target, nameof(TestObject.Name), "A", "B");
            System.Threading.Thread.Sleep(10);
            var action2 = new PropertyChangeAction(target, nameof(TestObject.Name), "B", "C");

            var merged = action1.MergeWith(action2) as PropertyChangeAction;

            Assert.NotNull(merged);
            Assert.Equal(action2.Timestamp, merged.Timestamp);
        }

        #endregion

        #region Static Configuration Tests

        /// <summary>
        /// Verifies that the MergeWindowMilliseconds property has the correct default value.
        /// </summary>
        [Fact]
        public void MergeWindowMilliseconds_DefaultValue()
        {
            Assert.Equal(500, PropertyChangeAction.MergeWindowMilliseconds);
        }

        /// <summary>
        /// Verifies that the MergeWindowMilliseconds property can be modified.
        /// </summary>
        [Fact]
        public void MergeWindowMilliseconds_CanBeChanged()
        {
            var original = PropertyChangeAction.MergeWindowMilliseconds;
            try
            {
                PropertyChangeAction.MergeWindowMilliseconds = 1000;
                Assert.Equal(1000, PropertyChangeAction.MergeWindowMilliseconds);
            }
            finally
            {
                PropertyChangeAction.MergeWindowMilliseconds = original;
            }
        }

        #endregion
    }
}
