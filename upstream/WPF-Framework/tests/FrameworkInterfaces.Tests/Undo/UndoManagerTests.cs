using Xunit;
using FrameworkInterfaces.Undo;
using FrameworkInterfaces.Undo.Actions;
using System.ComponentModel;

namespace FrameworkInterfaces.Tests.Undo
{
    /// <summary>
    /// Test class for the UndoManager implementation, providing comprehensive tests for undo/redo stack management,
    /// action execution, transaction handling, state tracking, and property change notifications.
    /// </summary>
    public class UndoManagerTests
    {
        #region Initial State Tests

        /// <summary>
        /// Verifies that a new UndoManager instance cannot perform undo operations initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_CannotUndo()
        {
            var manager = new UndoManager();
            Assert.False(manager.CanUndo);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance cannot perform redo operations initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_CannotRedo()
        {
            var manager = new UndoManager();
            Assert.False(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance has a null undo description initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_UndoDescriptionIsNull()
        {
            var manager = new UndoManager();
            Assert.Null(manager.UndoDescription);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance has a null redo description initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_RedoDescriptionIsNull()
        {
            var manager = new UndoManager();
            Assert.Null(manager.RedoDescription);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance reports no changes since save initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_HasNotChangedSinceSave()
        {
            var manager = new UndoManager();
            Assert.False(manager.HasChangedSinceSave);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance is not executing an action initially.
        /// </summary>
        [Fact]
        public void NewUndoManager_IsNotExecutingAction()
        {
            var manager = new UndoManager();
            Assert.False(manager.IsExecutingAction);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance has an empty undo stack.
        /// </summary>
        [Fact]
        public void NewUndoManager_UndoStackIsEmpty()
        {
            var manager = new UndoManager();
            Assert.Empty(manager.UndoStack);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance has an empty redo stack.
        /// </summary>
        [Fact]
        public void NewUndoManager_RedoStackIsEmpty()
        {
            var manager = new UndoManager();
            Assert.Empty(manager.RedoStack);
        }

        /// <summary>
        /// Verifies that a new UndoManager instance has the default maximum undo levels of 100.
        /// </summary>
        [Fact]
        public void NewUndoManager_MaxUndoLevelsDefault()
        {
            var manager = new UndoManager();
            Assert.Equal(100, manager.MaxUndoLevels);
        }

        #endregion

        #region ExecuteAction Tests

        /// <summary>
        /// Verifies that ExecuteAction executes the provided action.
        /// </summary>
        [Fact]
        public void ExecuteAction_ExecutesTheAction()
        {
            var manager = new UndoManager();
            var executed = false;
            var action = new DelegateAction("Test", () => executed = true, () => { });

            manager.ExecuteAction(action);

            Assert.True(executed);
        }

        /// <summary>
        /// Verifies that ExecuteAction adds the action to the undo stack.
        /// </summary>
        [Fact]
        public void ExecuteAction_AddsToUndoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });

            manager.ExecuteAction(action);

            Assert.True(manager.CanUndo);
            Assert.Single(manager.UndoStack);
        }

        /// <summary>
        /// Verifies that ExecuteAction clears the redo stack when a new action is executed.
        /// </summary>
        [Fact]
        public void ExecuteAction_ClearsRedoStack()
        {
            var manager = new UndoManager();
            var action1 = new DelegateAction("Test1", () => { }, () => { });
            var action2 = new DelegateAction("Test2", () => { }, () => { });

            manager.ExecuteAction(action1);
            manager.Undo();
            manager.ExecuteAction(action2);

            Assert.False(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that ExecuteAction throws an ArgumentNullException when passed a null action.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when action parameter is null.</exception>
        [Fact]
        public void ExecuteAction_NullAction_ThrowsArgumentNullException()
        {
            var manager = new UndoManager();

            Assert.Throws<ArgumentNullException>(() => manager.ExecuteAction(null!));
        }

        /// <summary>
        /// Verifies that ExecuteAction sets the HasChangedSinceSave property to true.
        /// </summary>
        [Fact]
        public void ExecuteAction_SetsHasChangedSinceSave()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });

            manager.ExecuteAction(action);

            Assert.True(manager.HasChangedSinceSave);
        }

        #endregion

        #region RecordAction Tests

        /// <summary>
        /// Verifies that RecordAction does not execute the action immediately.
        /// </summary>
        [Fact]
        public void RecordAction_DoesNotExecuteTheAction()
        {
            var manager = new UndoManager();
            var executed = false;
            var action = new DelegateAction("Test", () => executed = true, () => { });

            manager.RecordAction(action);

            Assert.False(executed);
        }

        /// <summary>
        /// Verifies that RecordAction adds the action to the undo stack.
        /// </summary>
        [Fact]
        public void RecordAction_AddsToUndoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });

            manager.RecordAction(action);

            Assert.True(manager.CanUndo);
        }

        /// <summary>
        /// Verifies that RecordAction throws an ArgumentNullException when passed a null action.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when action parameter is null.</exception>
        [Fact]
        public void RecordAction_NullAction_ThrowsArgumentNullException()
        {
            var manager = new UndoManager();

            Assert.Throws<ArgumentNullException>(() => manager.RecordAction(null!));
        }

        #endregion

        #region Undo Tests

        /// <summary>
        /// Verifies that Undo calls the undo operation on the action.
        /// </summary>
        [Fact]
        public void Undo_CallsUndoOnAction()
        {
            var manager = new UndoManager();
            var undoCalled = false;
            var action = new DelegateAction("Test", () => { }, () => undoCalled = true);
            manager.ExecuteAction(action);

            manager.Undo();

            Assert.True(undoCalled);
        }

        /// <summary>
        /// Verifies that Undo moves the action from the undo stack to the redo stack.
        /// </summary>
        [Fact]
        public void Undo_MovesActionToRedoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);

            manager.Undo();

            Assert.False(manager.CanUndo);
            Assert.True(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that Undo does nothing when there are no actions to undo.
        /// </summary>
        [Fact]
        public void Undo_WhenNoActions_DoesNothing()
        {
            var manager = new UndoManager();

            var exception = Record.Exception(() => manager.Undo());

            Assert.Null(exception);
            Assert.False(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that Undo raises the StateChanged event.
        /// </summary>
        [Fact]
        public void Undo_RaisesStateChanged()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);
            var stateChanged = false;
            manager.StateChanged += (s, e) => stateChanged = true;

            manager.Undo();

            Assert.True(stateChanged);
        }

        #endregion

        #region Redo Tests

        /// <summary>
        /// Verifies that Redo calls the execute operation on the action.
        /// </summary>
        [Fact]
        public void Redo_CallsExecuteOnAction()
        {
            var manager = new UndoManager();
            var executeCount = 0;
            var action = new DelegateAction("Test", () => executeCount++, () => { });
            manager.ExecuteAction(action);
            manager.Undo();

            manager.Redo();

            Assert.Equal(2, executeCount);
        }

        /// <summary>
        /// Verifies that Redo moves the action from the redo stack to the undo stack.
        /// </summary>
        [Fact]
        public void Redo_MovesActionToUndoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);
            manager.Undo();

            manager.Redo();

            Assert.True(manager.CanUndo);
            Assert.False(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that Redo does nothing when there are no actions to redo.
        /// </summary>
        [Fact]
        public void Redo_WhenNoActions_DoesNothing()
        {
            var manager = new UndoManager();

            var exception = Record.Exception(() => manager.Redo());

            Assert.Null(exception);
        }

        #endregion

        #region UndoTo/RedoTo Tests

        /// <summary>
        /// Verifies that UndoTo undoes multiple actions up to and including the specified action.
        /// </summary>
        [Fact]
        public void UndoTo_UndoesMultipleActions()
        {
            var manager = new UndoManager();
            var undoCount = 0;
            var action1 = new DelegateAction("Test1", () => { }, () => undoCount++);
            var action2 = new DelegateAction("Test2", () => { }, () => undoCount++);
            var action3 = new DelegateAction("Test3", () => { }, () => undoCount++);
            manager.ExecuteAction(action1);
            manager.ExecuteAction(action2);
            manager.ExecuteAction(action3);

            manager.UndoTo(action1);

            Assert.Equal(3, undoCount);
            Assert.False(manager.CanUndo);
        }

        /// <summary>
        /// Verifies that UndoTo throws an ArgumentNullException when passed a null action.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when action parameter is null.</exception>
        [Fact]
        public void UndoTo_NullAction_ThrowsArgumentNullException()
        {
            var manager = new UndoManager();

            Assert.Throws<ArgumentNullException>(() => manager.UndoTo(null!));
        }

        /// <summary>
        /// Verifies that RedoTo redoes multiple actions up to and including the specified action.
        /// </summary>
        [Fact]
        public void RedoTo_RedoesMultipleActions()
        {
            var manager = new UndoManager();
            var executeCount = 0;
            var action1 = new DelegateAction("Test1", () => executeCount++, () => { });
            var action2 = new DelegateAction("Test2", () => executeCount++, () => { });
            var action3 = new DelegateAction("Test3", () => executeCount++, () => { });
            manager.ExecuteAction(action1);
            manager.ExecuteAction(action2);
            manager.ExecuteAction(action3);
            manager.Undo();
            manager.Undo();
            manager.Undo();
            executeCount = 0;

            manager.RedoTo(action3);

            Assert.Equal(3, executeCount);
            Assert.False(manager.CanRedo);
        }

        /// <summary>
        /// Verifies that RedoTo throws an ArgumentNullException when passed a null action.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when action parameter is null.</exception>
        [Fact]
        public void RedoTo_NullAction_ThrowsArgumentNullException()
        {
            var manager = new UndoManager();

            Assert.Throws<ArgumentNullException>(() => manager.RedoTo(null!));
        }

        #endregion

        #region Clear Tests

        /// <summary>
        /// Verifies that Clear removes all actions from the undo stack.
        /// </summary>
        [Fact]
        public void Clear_ClearsUndoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);

            manager.Clear();

            Assert.False(manager.CanUndo);
            Assert.Empty(manager.UndoStack);
        }

        /// <summary>
        /// Verifies that Clear removes all actions from the redo stack.
        /// </summary>
        [Fact]
        public void Clear_ClearsRedoStack()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);
            manager.Undo();

            manager.Clear();

            Assert.False(manager.CanRedo);
            Assert.Empty(manager.RedoStack);
        }

        /// <summary>
        /// Verifies that Clear resets the HasChangedSinceSave property to false.
        /// </summary>
        [Fact]
        public void Clear_ResetsHasChangedSinceSave()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);

            manager.Clear();

            Assert.False(manager.HasChangedSinceSave);
        }

        #endregion

        #region SavePoint Tests

        /// <summary>
        /// Verifies that MarkSavePoint resets the HasChangedSinceSave property to false.
        /// </summary>
        [Fact]
        public void MarkSavePoint_ResetsHasChangedSinceSave()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);

            manager.MarkSavePoint();

            Assert.False(manager.HasChangedSinceSave);
        }

        /// <summary>
        /// Verifies that executing a new action after marking a save point sets HasChangedSinceSave to true.
        /// </summary>
        [Fact]
        public void AfterMarkSavePoint_NewActionSetsHasChangedSinceSave()
        {
            var manager = new UndoManager();
            var action1 = new DelegateAction("Test1", () => { }, () => { });
            var action2 = new DelegateAction("Test2", () => { }, () => { });
            manager.ExecuteAction(action1);
            manager.MarkSavePoint();

            manager.ExecuteAction(action2);

            Assert.True(manager.HasChangedSinceSave);
        }

        /// <summary>
        /// Verifies that undoing back to the save point resets HasChangedSinceSave to false.
        /// </summary>
        [Fact]
        public void UndoToSavePoint_ResetsHasChangedSinceSave()
        {
            var manager = new UndoManager();
            var action1 = new DelegateAction("Test1", () => { }, () => { });
            var action2 = new DelegateAction("Test2", () => { }, () => { });
            manager.ExecuteAction(action1);
            manager.MarkSavePoint();
            manager.ExecuteAction(action2);

            manager.Undo();

            Assert.False(manager.HasChangedSinceSave);
        }

        #endregion

        #region Transaction Tests

        /// <summary>
        /// Verifies that BeginTransaction groups multiple actions into a single undoable composite action.
        /// </summary>
        [Fact]
        public void BeginTransaction_GroupsActionsIntoSingleUndo()
        {
            var manager = new UndoManager();

            using (manager.BeginTransaction("Grouped"))
            {
                manager.RecordAction(new DelegateAction("Test1", () => { }, () => { }));
                manager.RecordAction(new DelegateAction("Test2", () => { }, () => { }));
                manager.RecordAction(new DelegateAction("Test3", () => { }, () => { }));
            }

            Assert.Single(manager.UndoStack);
            Assert.Equal("Grouped", manager.UndoDescription);
        }

        /// <summary>
        /// Verifies that nested transactions return a no-op disposable and use the outer transaction.
        /// </summary>
        [Fact]
        public void BeginTransaction_NestedTransaction_ReturnsNoOpDisposable()
        {
            var manager = new UndoManager();

            using (manager.BeginTransaction("Outer"))
            {
                manager.RecordAction(new DelegateAction("Test1", () => { }, () => { }));

                using (manager.BeginTransaction("Inner"))
                {
                    manager.RecordAction(new DelegateAction("Test2", () => { }, () => { }));
                }

                manager.RecordAction(new DelegateAction("Test3", () => { }, () => { }));
            }

            Assert.Single(manager.UndoStack);
            Assert.Equal("Outer", manager.UndoDescription);
        }

        /// <summary>
        /// Verifies that committing a transaction with no actions does not add anything to the undo stack.
        /// </summary>
        [Fact]
        public void CommitTransaction_WithNoActions_DoesNotAddToUndoStack()
        {
            var manager = new UndoManager();

            using (manager.BeginTransaction("Empty"))
            {
                // No actions
            }

            Assert.Empty(manager.UndoStack);
        }

        /// <summary>
        /// Verifies that rolling back a transaction undoes all actions within it in reverse order.
        /// </summary>
        [Fact]
        public void RollbackTransaction_UndoesAllActionsInReverseOrder()
        {
            var manager = new UndoManager();
            var undoOrder = new List<int>();

            manager.BeginTransaction("Test");
            manager.ExecuteAction(new DelegateAction("Test1", () => { }, () => undoOrder.Add(1)));
            manager.ExecuteAction(new DelegateAction("Test2", () => { }, () => undoOrder.Add(2)));
            manager.ExecuteAction(new DelegateAction("Test3", () => { }, () => undoOrder.Add(3)));

            manager.RollbackTransaction();

            Assert.Empty(manager.UndoStack);
            Assert.Equal(new[] { 3, 2, 1 }, undoOrder);
        }

        /// <summary>
        /// Verifies that rolling back when no transaction is active does nothing.
        /// </summary>
        [Fact]
        public void RollbackTransaction_WithNoTransaction_DoesNothing()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);

            var exception = Record.Exception(() => manager.RollbackTransaction());

            Assert.Null(exception);
            Assert.Single(manager.UndoStack);
        }

        #endregion

        #region MaxUndoLevels Tests

        /// <summary>
        /// Verifies that setting MaxUndoLevels trims the undo stack to the specified size.
        /// </summary>
        [Fact]
        public void MaxUndoLevels_TrimsUndoStack()
        {
            var manager = new UndoManager();
            manager.MaxUndoLevels = 3;

            for (int i = 0; i < 5; i++)
            {
                manager.ExecuteAction(new DelegateAction($"Test{i}", () => { }, () => { }));
            }

            Assert.Equal(3, manager.UndoStack.Count);
        }

        /// <summary>
        /// Verifies that setting MaxUndoLevels to less than one sets it to one instead.
        /// </summary>
        [Fact]
        public void MaxUndoLevels_SetToLessThanOne_SetsToOne()
        {
            var manager = new UndoManager();

            manager.MaxUndoLevels = 0;

            Assert.Equal(1, manager.MaxUndoLevels);
        }

        /// <summary>
        /// Verifies that setting MaxUndoLevels to a negative value sets it to one instead.
        /// </summary>
        [Fact]
        public void MaxUndoLevels_SetToNegative_SetsToOne()
        {
            var manager = new UndoManager();

            manager.MaxUndoLevels = -5;

            Assert.Equal(1, manager.MaxUndoLevels);
        }

        #endregion

        #region PropertyChanged Tests

        /// <summary>
        /// Verifies that ExecuteAction raises PropertyChanged event for CanUndo property.
        /// </summary>
        [Fact]
        public void ExecuteAction_RaisesPropertyChangedForCanUndo()
        {
            var manager = new UndoManager();
            var changedProperties = new List<string>();
            manager.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);
            var action = new DelegateAction("Test", () => { }, () => { });

            manager.ExecuteAction(action);

            Assert.Contains(nameof(IUndoManager.CanUndo), changedProperties);
        }

        /// <summary>
        /// Verifies that Undo raises PropertyChanged event for CanRedo property.
        /// </summary>
        [Fact]
        public void Undo_RaisesPropertyChangedForCanRedo()
        {
            var manager = new UndoManager();
            var action = new DelegateAction("Test", () => { }, () => { });
            manager.ExecuteAction(action);
            var changedProperties = new List<string>();
            manager.PropertyChanged += (s, e) => changedProperties.Add(e.PropertyName!);

            manager.Undo();

            Assert.Contains(nameof(IUndoManager.CanRedo), changedProperties);
        }

        #endregion
    }
}
