using Xunit;
using FrameworkInterfaces;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Tests.Undo.Actions
{
    /// <summary>
    /// Test class for the AddElementAction implementation, providing comprehensive tests for element addition
    /// to collections, undo/redo functionality, index management, and action merging behavior.
    /// </summary>
    public class AddElementActionTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that attempting to create an AddElementAction with a null collection throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when collection parameter is null.</exception>
        [Fact]
        public void Constructor_NullCollection_ThrowsArgumentNullException()
        {
            var element = new MockElement();

            Assert.Throws<ArgumentNullException>(() =>
                new AddElementAction(null!, element));
        }

        /// <summary>
        /// Verifies that attempting to create an AddElementAction with a null element throws an ArgumentNullException.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when element parameter is null.</exception>
        [Fact]
        public void Constructor_NullElement_ThrowsArgumentNullException()
        {
            var collection = new MockElementCollection();

            Assert.Throws<ArgumentNullException>(() =>
                new AddElementAction(collection, null!));
        }

        /// <summary>
        /// Verifies that the constructor correctly sets the Collection, Element, and Target properties.
        /// </summary>
        [Fact]
        public void Constructor_ValidArgs_SetsProperties()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();

            var action = new AddElementAction(collection, element);

            Assert.Same(collection, action.Collection);
            Assert.Same(element, action.Element);
            Assert.Same(collection, action.Target);
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

            var action = new AddElementAction(collection, element);
            var after = DateTime.Now;

            Assert.True(action.Timestamp >= before);
            Assert.True(action.Timestamp <= after);
        }

        /// <summary>
        /// Verifies that providing a negative index uses the collection count as the index.
        /// </summary>
        [Fact]
        public void Constructor_NegativeIndex_UsesCollectionCount()
        {
            var collection = new MockElementCollection();
            collection.Add(new MockElement { Name = "Existing" });
            var element = new MockElement();

            var action = new AddElementAction(collection, element, -1);

            Assert.Equal(1, action.Index);
        }

        /// <summary>
        /// Verifies that providing a positive index uses the provided index value.
        /// </summary>
        [Fact]
        public void Constructor_PositiveIndex_UsesProvidedIndex()
        {
            var collection = new MockElementCollection();
            collection.Add(new MockElement { Name = "Existing" });
            var element = new MockElement();

            var action = new AddElementAction(collection, element, 0);

            Assert.Equal(0, action.Index);
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

            var action = new AddElementAction(collection, element);

            Assert.Equal("Add My Element", action.Description);
        }

        /// <summary>
        /// Verifies that the Description property falls back to the element's Name when DisplayName is null.
        /// </summary>
        [Fact]
        public void Description_FallsBackToName()
        {
            var collection = new MockElementCollection();
            var element = new MockElement { DisplayName = null!, Name = "ElementName" };

            var action = new AddElementAction(collection, element);

            Assert.Equal("Add ElementName", action.Description);
        }

        /// <summary>
        /// Verifies that the Description property uses a generic fallback when both DisplayName and Name are null.
        /// </summary>
        [Fact]
        public void Description_FallsBackToGeneric()
        {
            var collection = new MockElementCollection();
            var element = new MockElement { DisplayName = null!, Name = null! };

            var action = new AddElementAction(collection, element);

            Assert.Equal("Add element", action.Description);
        }

        #endregion

        #region Execute Tests

        /// <summary>
        /// Verifies that executing the action adds the element to the collection.
        /// </summary>
        [Fact]
        public void Execute_AddsElementToCollection()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();
            var action = new AddElementAction(collection, element);

            // First remove, then execute should re-add
            collection.Add(element);
            collection.Remove(element);

            action.Execute();

            Assert.Contains(element, collection);
        }

        /// <summary>
        /// Verifies that executing the action inserts the element at the correct index.
        /// </summary>
        [Fact]
        public void Execute_InsertsAtCorrectIndex()
        {
            var collection = new MockElementCollection();
            var existing1 = new MockElement { Name = "First" };
            var existing2 = new MockElement { Name = "Third" };
            collection.Add(existing1);
            collection.Add(existing2);

            var element = new MockElement { Name = "Second" };
            var action = new AddElementAction(collection, element, 1);

            action.Execute();

            Assert.Equal(1, collection.IndexOf(element));
            Assert.Equal(3, collection.Count);
        }

        /// <summary>
        /// Verifies that executing with an out-of-range index adds the element to the end of the collection.
        /// </summary>
        [Fact]
        public void Execute_IndexOutOfRange_AddsToEnd()
        {
            var collection = new MockElementCollection();
            var existing = new MockElement { Name = "First" };
            collection.Add(existing);

            var element = new MockElement { Name = "New" };
            var action = new AddElementAction(collection, element, 100);

            action.Execute();

            Assert.Equal(1, collection.IndexOf(element));
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that undoing the action removes the element from the collection.
        /// </summary>
        [Fact]
        public void Undo_RemovesElementFromCollection()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();
            collection.Add(element);
            var action = new AddElementAction(collection, element);

            action.Undo();

            Assert.DoesNotContain(element, collection);
        }

        /// <summary>
        /// Verifies that executing and undoing the action multiple times correctly restores collection state.
        /// </summary>
        [Fact]
        public void Undo_ExecuteRoundTrip_RestoresState()
        {
            var collection = new MockElementCollection();
            var existing = new MockElement { Name = "Existing" };
            collection.Add(existing);

            var element = new MockElement { Name = "New" };
            var action = new AddElementAction(collection, element, 0);

            // Execute adds element
            action.Execute();
            Assert.Equal(2, collection.Count);
            Assert.Equal(0, collection.IndexOf(element));

            // Undo removes element
            action.Undo();
            Assert.Single(collection);
            Assert.DoesNotContain(element, collection);

            // Execute again re-adds
            action.Execute();
            Assert.Equal(2, collection.Count);
            Assert.Contains(element, collection);
        }

        #endregion

        #region CanMergeWith Tests

        /// <summary>
        /// Verifies that AddElementAction does not support merging with other actions.
        /// </summary>
        /// <returns>False, indicating the action cannot be merged.</returns>
        [Fact]
        public void CanMergeWith_ReturnsFalse()
        {
            var collection = new MockElementCollection();
            var element = new MockElement();
            var action = new AddElementAction(collection, element);

            var other = new AddElementAction(collection, new MockElement());

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
            var action = new AddElementAction(collection, element);

            var other = new AddElementAction(collection, new MockElement());

            var result = action.MergeWith(other);

            Assert.Same(action, result);
        }

        #endregion
    }
}
