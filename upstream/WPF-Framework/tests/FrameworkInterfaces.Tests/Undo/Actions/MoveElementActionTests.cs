using Xunit;
using FrameworkInterfaces;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Tests.Undo.Actions
{
    /// <summary>
    /// Test class for the MoveElementAction implementation, providing comprehensive tests for element movement
    /// within collections, undo/redo functionality, index management, and action merging behavior.
    /// </summary>
    public class MoveElementActionTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that attempting to create a MoveElementAction with a null collection throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when collection parameter is null.</exception>
        [Fact]
        public void Constructor_NullCollection_ThrowsArgumentNullException()
        {
            var element = new MockElement();

            Assert.Throws<ArgumentNullException>(() =>
                new MoveElementAction(null!, element, 0, 1));
        }

        /// <summary>
        /// Verifies that attempting to create a MoveElementAction with a null element throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when element parameter is null.</exception>
        [Fact]
        public void Constructor_NullElement_ThrowsArgumentNullException()
        {
            var collection = new MockElementCollection();

            Assert.Throws<ArgumentNullException>(() =>
                new MoveElementAction(collection, null!, 0, 1));
        }

        /// <summary>
        /// Verifies that the constructor correctly sets the Collection, Element, Target, OldIndex, and NewIndex properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidArgs_SetsProperties()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();

            var action = new MoveElementAction(collection, element, 0, 2);

            Assert.Same(collection, action.Collection);
            Assert.Same(element, action.Element);
            Assert.Same(collection, action.Target);
            Assert.Equal(0, action.OldIndex);
            Assert.Equal(2, action.NewIndex);
        }

        /// <summary>
        /// Verifies that the constructor sets a timestamp within the expected time range.
        /// </summary>
        [Fact]
        public void Constructor_SetsTimestamp()
        {
            var before = DateTime.Now;
            var collection = new MockElementCollection();
            var element = new MockElement();

            var action = new MoveElementAction(collection, element, 0, 1);
            var after = DateTime.Now;

            Assert.True(action.Timestamp >= before);
            Assert.True(action.Timestamp <= after);
        }

        #endregion

        #region Description Tests

        /// <summary>
        /// Verifies that the Description property uses the element's DisplayName when available.
        /// </summary>
        [Fact]
        public void Description_UsesDisplayName()
        {
            var collection = new MockElementCollection();
            var element = new MockElement { DisplayName = "My Element" };

            var action = new MoveElementAction(collection, element, 0, 1);

            Assert.Equal("Move My Element", action.Description);
        }

        /// <summary>
        /// Verifies that the Description property falls back to the element's Name when DisplayName is null.
        /// </summary>
        [Fact]
        public void Description_FallsBackToName()
        {
            var collection = new MockElementCollection();
            var element = new MockElement { DisplayName = null!, Name = "ElementName" };

            var action = new MoveElementAction(collection, element, 0, 1);

            Assert.Equal("Move ElementName", action.Description);
        }

        /// <summary>
        /// Verifies that the Description property uses a generic fallback when both DisplayName and Name are null.
        /// </summary>
        [Fact]
        public void Description_FallsBackToGeneric()
        {
            var collection = new MockElementCollection();
            var element = new MockElement { DisplayName = null!, Name = null! };

            var action = new MoveElementAction(collection, element, 0, 1);

            Assert.Equal("Move element", action.Description);
        }

        #endregion

        #region Execute Tests

        /// <summary>
        /// Verifies that executing the action moves the element to the new position in the collection.
        /// </summary>
        [Fact]
        public void Execute_MovesElementToNewPosition()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            var action = new MoveElementAction(collection, element1, 0, 2);

            action.Execute();

            // After RemoveAt(0) -> [Second, Third], Insert(2, First) -> [Second, Third, First]
            Assert.Equal(2, collection.IndexOf(element1));
        }

        /// <summary>
        /// Verifies that moving an element forward shifts other elements down in the collection.
        /// </summary>
        [Fact]
        public void Execute_MoveForward_ShiftsOthersDown()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            // Move First from index 0 to index 2
            // After RemoveAt(0) -> [Second, Third], Insert(2, First) -> [Second, Third, First]
            var action = new MoveElementAction(collection, element1, 0, 2);
            action.Execute();

            // Second shifts to index 0, Third stays at index 1, First moves to index 2
            Assert.Equal(0, collection.IndexOf(element2));
            Assert.Equal(1, collection.IndexOf(element3));
            Assert.Equal(2, collection.IndexOf(element1));
        }

        /// <summary>
        /// Verifies that moving an element backward shifts other elements up in the collection.
        /// </summary>
        [Fact]
        public void Execute_MoveBackward_ShiftsOthersUp()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            // Move Third from index 2 to index 0
            var action = new MoveElementAction(collection, element3, 2, 0);
            action.Execute();

            Assert.Equal(0, collection.IndexOf(element3));
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that undoing the action moves the element back to its original position.
        /// </summary>
        [Fact]
        public void Undo_MovesElementBackToOriginalPosition()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            var action = new MoveElementAction(collection, element1, 0, 2);

            // Execute: RemoveAt(0) -> [Second, Third], Insert(2, First) -> [Second, Third, First]
            action.Execute();
            Assert.Equal(2, collection.IndexOf(element1));

            // Undo: RemoveAt(2) -> [Second, Third], Insert(0, First) -> [First, Second, Third]
            action.Undo();
            Assert.Equal(0, collection.IndexOf(element1));
        }

        /// <summary>
        /// Verifies that executing and undoing the action multiple times correctly restores collection state.
        /// </summary>
        [Fact]
        public void Undo_ExecuteRoundTrip_RestoresState()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            var action = new MoveElementAction(collection, element2, 1, 0);

            // Capture original order
            var originalFirst = collection[0];
            var originalSecond = collection[1];
            var originalThird = collection[2];

            // Execute moves element
            action.Execute();
            Assert.Equal(0, collection.IndexOf(element2));

            // Undo restores
            action.Undo();
            Assert.Same(originalFirst, collection[0]);
            Assert.Same(originalSecond, collection[1]);
            Assert.Same(originalThird, collection[2]);

            // Execute again
            action.Execute();
            Assert.Equal(0, collection.IndexOf(element2));
        }

        #endregion

        #region CanMergeWith Tests

        /// <summary>
        /// Verifies that MoveElementAction does not support merging with other actions.
        /// </summary>
        /// <returns>False, indicating the action cannot be merged.</returns>
        [Fact]
        public void CanMergeWith_ReturnsFalse()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();
            var action = new MoveElementAction(collection, element, 0, 1);

            var other = new MoveElementAction(collection, new MockElement(), 1, 2);

            Assert.False(action.CanMergeWith(other));
        }

        #endregion

        #region MergeWith Tests

        /// <summary>
        /// Verifies that attempting to merge returns the original action unchanged.
        /// </summary>
        /// <returns>The original action instance.</returns>
        [Fact]
        public void MergeWith_ReturnsSelf()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();
            var action = new MoveElementAction(collection, element, 0, 1);

            var other = new MoveElementAction(collection, new MockElement(), 1, 2);

            var result = action.MergeWith(other);

            Assert.Same(action, result);
        }

        #endregion

        #region Edge Cases

        /// <summary>
        /// Verifies that moving an element to the same position leaves the collection unchanged.
        /// </summary>
        [Fact]
        public void MoveToSamePosition_CollectionUnchanged()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            collection.Add(element1);
            collection.Add(element2);

            var action = new MoveElementAction(collection, element1, 0, 0);

            action.Execute();

            Assert.Equal(0, collection.IndexOf(element1));
            Assert.Equal(1, collection.IndexOf(element2));
        }

        /// <summary>
        /// Verifies that moving the last element to the first position shifts all other elements.
        /// </summary>
        [Fact]
        public void MoveLastToFirst_AllElementsShift()
        {
            var collection = new MockElementCollection();
            var element1 = new MockElement { Name = "First" };
            var element2 = new MockElement { Name = "Second" };
            var element3 = new MockElement { Name = "Third" };
            collection.Add(element1);
            collection.Add(element2);
            collection.Add(element3);

            var action = new MoveElementAction(collection, element3, 2, 0);

            action.Execute();

            Assert.Equal(0, collection.IndexOf(element3));
            Assert.Equal(1, collection.IndexOf(element1));
            Assert.Equal(2, collection.IndexOf(element2));
        }

        #endregion
    }
}
