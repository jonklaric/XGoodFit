using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Media;
using FrameworkInterfaces;
using FrameworkInterfaces.Tests.Undo.Actions;
using FrameworkInterfaces.Undo;
using Xunit;

namespace FrameworkInterfaces.Tests
{
    /// <summary>
    /// Regression tests pinning behavior fixed in the forensic-audit Phase 1/2 work.
    /// Each test name references the audit finding ID it covers.
    /// </summary>
    public class AuditRegressionTests
    {
        #region A-001 — ElementBaseBuff.RaisePropertyChange dirty semantics

        /// <summary>
        /// Concrete subclass of <see cref="ElementBaseBuff"/> exposing the protected
        /// <c>RaisePropertyChange</c> for testing.
        /// </summary>
        private sealed class FakeElement : ElementBaseBuff
        {
            public FakeElement(IElementCollection parent) : base("fake", parent) { }

            public override ImageSource ElementImage => null!;
            public override bool CanCopyFromExternal => false;
            public override IElement Copy(string? newName = null) => new FakeElement(ParentCollection);
            public override IElement CopyFromExternal(string itemName, string fullFileName) =>
                new FakeElement(ParentCollection);
            public override void Open() { }
            public override void Save() { }
            public override void Delete() { }

            public void SetIsDirty(bool value) => IsDirty = value;

            public void TriggerRaise(string propertyName, bool isDirty) =>
                RaisePropertyChange(propertyName, isDirty);
        }

        /// <summary>Verifies that a notify-only property change preserves an existing dirty state.</summary>
        [Fact]
        public void A001_ElementBaseBuff_RaisePropertyChange_PreservesIsDirty_OnFalse()
        {
            // Arrange — element starts dirty; the fix's contract is that
            // RaisePropertyChange("Foo", false) is "notify-only" and must NOT clear dirty.
            var collection = new MockElementCollection();
            var element = new FakeElement(collection);
            element.SetIsDirty(true);
            Assert.True(element.IsDirty);

            // Act
            element.TriggerRaise("Foo", false);

            // Assert — IsDirty must still be true (promote-only semantics).
            Assert.True(element.IsDirty);
        }

        /// <summary>Verifies that a property change requesting dirty promotion marks a clean element dirty.</summary>
        [Fact]
        public void A001_ElementBaseBuff_RaisePropertyChange_PromotesIsDirty_OnTrue()
        {
            // Arrange — clean element; the default isDirty=true overload should promote.
            var collection = new MockElementCollection();
            var element = new FakeElement(collection);
            Assert.False(element.IsDirty);

            // Act
            element.TriggerRaise("Foo", true);

            // Assert
            Assert.True(element.IsDirty);
        }

        /// <summary>Verifies that a notify-only change still raises the named property notification.</summary>
        [Fact]
        public void A001_ElementBaseBuff_RaisePropertyChange_RaisesPropertyChangedEvent()
        {
            // Arrange
            var collection = new MockElementCollection();
            var element = new FakeElement(collection);
            string? observed = null;
            element.PropertyChanged += (_, e) => observed = e.PropertyName;

            // Act
            element.TriggerRaise("Foo", false);

            // Assert — notify-only mode must still raise PropertyChanged.
            Assert.Equal("Foo", observed);
        }

        #endregion

        #region A-007 — ElementCollectionBase.MoveElement is undoable

        /// <summary>
        /// Minimal concrete <see cref="ElementCollectionBase"/> for exercising MoveElement
        /// against the real undo plumbing (RecordMoveElement / UndoManager).
        /// </summary>
        private sealed class FakeCollection : ElementCollectionBase
        {
            public FakeCollection() : base(null!) { }

            public override string Name => "FakeCollection";

            public override void Add(IElement item)
            {
                ElementList.Add(item);
            }

            public override void Insert(int index, IElement item)
            {
                ElementList.Insert(index, item);
            }

            public override void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName)
            {
                throw new NotImplementedException();
            }

            public override void Open() { }

            public override void Delete() { }
        }

        /// <summary>Verifies that moving an element records an undo action restoring the original order.</summary>
        [Fact]
        public void A007_ElementCollectionBase_MoveElement_IsUndoable()
        {
            // Arrange — three elements in stable order: a, b, c.
            var collection = new FakeCollection();
            var a = new MockElement { Name = "a" };
            var b = new MockElement { Name = "b" };
            var c = new MockElement { Name = "c" };
            collection.Add(a);
            collection.Add(b);
            collection.Add(c);

            // Force lazy creation of the UndoManager; verify clean baseline.
            Assert.False(collection.UndoManager.CanUndo);

            // Act — move 'a' from position 0 to 2 → expected order [b, c, a].
            collection.MoveElement(a, 0, 2);
            Assert.Same(b, collection[0]);
            Assert.Same(c, collection[1]);
            Assert.Same(a, collection[2]);

            // Without the fix, no MoveElementAction would have been recorded so CanUndo is false.
            Assert.True(collection.UndoManager.CanUndo);

            // Act — undo the move.
            collection.UndoManager.Undo();

            // Assert — collection is restored to [a, b, c].
            Assert.Same(a, collection[0]);
            Assert.Same(b, collection[1]);
            Assert.Same(c, collection[2]);
        }

        /// <summary>Verifies that redoing an undone element move restores the moved order.</summary>
        [Fact]
        public void A007_ElementCollectionBase_MoveElement_RedoRestoresMovedOrder()
        {
            // Arrange
            var collection = new FakeCollection();
            var a = new MockElement { Name = "a" };
            var b = new MockElement { Name = "b" };
            var c = new MockElement { Name = "c" };
            collection.Add(a); collection.Add(b); collection.Add(c);

            // Act
            collection.MoveElement(a, 0, 2);
            collection.UndoManager.Undo();
            collection.UndoManager.Redo();

            // Assert — after redo, moved order is back.
            Assert.Same(b, collection[0]);
            Assert.Same(c, collection[1]);
            Assert.Same(a, collection[2]);
        }

        #endregion
    }
}
