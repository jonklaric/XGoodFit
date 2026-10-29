using Xunit;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Tests.Undo
{
    /// <summary>
    /// Test class for the CompositeAction implementation, providing comprehensive tests for grouping multiple
    /// actions, batch execution, undo/redo functionality, and action merging behavior.
    /// </summary>
    public class CompositeActionTests
    {
        #region Constructor Tests

        /// <summary>
        /// Verifies that the constructor correctly sets the Description property.
        /// </summary>
        [Fact]
        public void Constructor_SetsDescription()
        {
            var action = new CompositeAction("Composite Test");

            Assert.Equal("Composite Test", action.Description);
        }

        /// <summary>
        /// Verifies that the constructor initializes an empty Actions list.
        /// </summary>
        [Fact]
        public void Constructor_InitializesEmptyActionsList()
        {
            var action = new CompositeAction("Test");

            Assert.NotNull(action.Actions);
            Assert.Empty(action.Actions);
        }

        /// <summary>
        /// Verifies that the constructor sets a timestamp within the expected time range.
        /// </summary>
        [Fact]
        public void Constructor_SetsTimestamp()
        {
            var before = DateTime.Now;
            var action = new CompositeAction("Test");
            var after = DateTime.Now;

            Assert.True(action.Timestamp >= before);
            Assert.True(action.Timestamp <= after);
        }

        #endregion

        #region AddAction Tests

        /// <summary>
        /// Verifies that AddAction adds the action to the Actions list.
        /// </summary>
        [Fact]
        public void AddAction_AddsToActionsList()
        {
            var composite = new CompositeAction("Test");
            var childAction = new DelegateAction("Child", () => { }, () => { });

            composite.AddAction(childAction);

            Assert.Single(composite.Actions);
            Assert.Same(childAction, composite.Actions[0]);
        }

        /// <summary>
        /// Verifies that AddAction preserves the order of multiple added actions.
        /// </summary>
        [Fact]
        public void AddAction_MultipleActions_AddsInOrder()
        {
            var composite = new CompositeAction("Test");
            var action1 = new DelegateAction("Action1", () => { }, () => { });
            var action2 = new DelegateAction("Action2", () => { }, () => { });
            var action3 = new DelegateAction("Action3", () => { }, () => { });

            composite.AddAction(action1);
            composite.AddAction(action2);
            composite.AddAction(action3);

            Assert.Equal(3, composite.Actions.Count);
            Assert.Same(action1, composite.Actions[0]);
            Assert.Same(action2, composite.Actions[1]);
            Assert.Same(action3, composite.Actions[2]);
        }

        #endregion

        #region Execute Tests

        /// <summary>
        /// Verifies that Execute executes all child actions in the order they were added.
        /// </summary>
        [Fact]
        public void Execute_ExecutesAllActionsInOrder()
        {
            var composite = new CompositeAction("Test");
            var executionOrder = new List<int>();

            composite.AddAction(new DelegateAction("A1", () => executionOrder.Add(1), () => { }));
            composite.AddAction(new DelegateAction("A2", () => executionOrder.Add(2), () => { }));
            composite.AddAction(new DelegateAction("A3", () => executionOrder.Add(3), () => { }));

            composite.Execute();

            Assert.Equal(new[] { 1, 2, 3 }, executionOrder);
        }

        /// <summary>
        /// Verifies that Execute does not throw an exception when the Actions list is empty.
        /// </summary>
        [Fact]
        public void Execute_WithNoActions_DoesNotThrow()
        {
            var composite = new CompositeAction("Empty");

            var exception = Record.Exception(() => composite.Execute());

            Assert.Null(exception);
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that Undo undoes all child actions in reverse order.
        /// </summary>
        [Fact]
        public void Undo_UndoesAllActionsInReverseOrder()
        {
            var composite = new CompositeAction("Test");
            var undoOrder = new List<int>();

            composite.AddAction(new DelegateAction("A1", () => { }, () => undoOrder.Add(1)));
            composite.AddAction(new DelegateAction("A2", () => { }, () => undoOrder.Add(2)));
            composite.AddAction(new DelegateAction("A3", () => { }, () => undoOrder.Add(3)));

            composite.Undo();

            Assert.Equal(new[] { 3, 2, 1 }, undoOrder);
        }

        /// <summary>
        /// Verifies that Undo does not throw an exception when the Actions list is empty.
        /// </summary>
        [Fact]
        public void Undo_WithNoActions_DoesNotThrow()
        {
            var composite = new CompositeAction("Empty");

            var exception = Record.Exception(() => composite.Undo());

            Assert.Null(exception);
        }

        #endregion

        #region Execute and Undo Integration Tests

        /// <summary>
        /// Verifies that executing and then undoing the composite action correctly applies and reverses all changes.
        /// </summary>
        [Fact]
        public void ExecuteThenUndo_RestoresState()
        {
            var composite = new CompositeAction("Test");
            var value1 = 0;
            var value2 = 0;

            composite.AddAction(new DelegateAction("A1", () => value1 = 10, () => value1 = 0));
            composite.AddAction(new DelegateAction("A2", () => value2 = 20, () => value2 = 0));

            composite.Execute();
            Assert.Equal(10, value1);
            Assert.Equal(20, value2);

            composite.Undo();
            Assert.Equal(0, value1);
            Assert.Equal(0, value2);
        }

        #endregion

        #region CanMergeWith Tests

        /// <summary>
        /// Verifies that CompositeAction does not support merging with other composite actions.
        /// </summary>
        /// <returns>False, indicating the action cannot be merged.</returns>
        [Fact]
        public void CanMergeWith_ReturnsFalse()
        {
            var composite1 = new CompositeAction("Test1");
            var composite2 = new CompositeAction("Test2");

            Assert.False(composite1.CanMergeWith(composite2));
        }

        /// <summary>
        /// Verifies that CanMergeWith returns false when passed a non-CompositeAction.
        /// </summary>
        /// <returns>False when action is not a CompositeAction.</returns>
        [Fact]
        public void CanMergeWith_NonCompositeAction_ReturnsFalse()
        {
            var composite = new CompositeAction("Test");
            var delegateAction = new DelegateAction("Other", () => { }, () => { });

            Assert.False(composite.CanMergeWith(delegateAction));
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
            var composite1 = new CompositeAction("Test1");
            var composite2 = new CompositeAction("Test2");

            var result = composite1.MergeWith(composite2);

            Assert.Same(composite1, result);
        }

        #endregion

        #region Target Property Tests

        /// <summary>
        /// Verifies that the Target property defaults to null.
        /// </summary>
        [Fact]
        public void Target_DefaultIsNull()
        {
            var composite = new CompositeAction("Test");

            Assert.Null(composite.Target);
        }

        #endregion
    }
}
