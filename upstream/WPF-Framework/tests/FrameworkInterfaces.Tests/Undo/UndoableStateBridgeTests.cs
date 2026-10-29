using Xunit;
using FrameworkInterfaces.Undo;
using System.ComponentModel;

namespace FrameworkInterfaces.Tests.Undo
{
    /// <summary>
    /// Test class for the UndoableStateBridge implementation, providing comprehensive tests for automatic
    /// undo/redo tracking of property changes, recording suspension, property filtering, and disposal.
    /// </summary>
    public class UndoableStateBridgeTests
    {
        #region Test Helpers

        /// <summary>
        /// Test object that implements INotifyPropertyChanged for testing property change tracking and undo/redo functionality.
        /// </summary>
        private class TestNotifyObject : INotifyPropertyChanged
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
            /// Backing field for the Amount property.
            /// </summary>
            private double _amount;

            /// <summary>
            /// Gets or sets the name of the test object.
            /// </summary>
            public string Name
            {
                get => _name;
                set
                {
                    if (_name != value)
                    {
                        _name = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    }
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
                    if (_value != value)
                    {
                        _value = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                    }
                }
            }

            /// <summary>
            /// Gets or sets the amount of the test object.
            /// </summary>
            public double Amount
            {
                get => _amount;
                set
                {
                    if (_amount != value)
                    {
                        _amount = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Amount)));
                    }
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
        /// Verifies that the constructor correctly sets the Source property.
        /// </summary>
        [Fact]
        public void Constructor_SetsSource()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            Assert.Same(source, bridge.Source);
        }

        /// <summary>
        /// Verifies that the constructor correctly sets the SourceDescription property.
        /// </summary>
        [Fact]
        public void Constructor_SetsSourceDescription()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager, "test source");

            Assert.Equal("test source", bridge.SourceDescription);
        }

        /// <summary>
        /// Verifies that the constructor throws an ArgumentNullException when source is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when source parameter is null.</exception>
        [Fact]
        public void Constructor_NullSource_ThrowsArgumentNullException()
        {
            var undoManager = new UndoManager();

            Assert.Throws<ArgumentNullException>(() =>
                new UndoableStateBridge(null!, () => undoManager));
        }

        /// <summary>
        /// Verifies that the constructor throws an ArgumentNullException when getUndoManager is null.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when getUndoManager parameter is null.</exception>
        [Fact]
        public void Constructor_NullGetUndoManager_ThrowsArgumentNullException()
        {
            var source = new TestNotifyObject();

            Assert.Throws<ArgumentNullException>(() =>
                new UndoableStateBridge(source, null!));
        }

        /// <summary>
        /// Verifies that the constructor throws an ArgumentException when both included and excluded properties are specified.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when both includedProperties and excludedProperties are provided.</exception>
        [Fact]
        public void Constructor_BothIncludedAndExcluded_ThrowsArgumentException()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();

            Assert.Throws<ArgumentException>(() =>
                new UndoableStateBridge(
                    source,
                    () => undoManager,
                    "settings",
                    null,
                    includedProperties: new[] { "Name" },
                    excludedProperties: new[] { "Value" }));
        }

        /// <summary>
        /// Verifies that the constructor subscribes to the source's PropertyChanged event.
        /// </summary>
        [Fact]
        public void Constructor_SubscribesToPropertyChanged()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "Test";

            Assert.True(undoManager.CanUndo);
        }

        #endregion

        #region Property Change Recording Tests

        /// <summary>
        /// Verifies that property changes are automatically recorded as undoable actions.
        /// </summary>
        [Fact]
        public void PropertyChange_RecordsUndoableAction()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "Changed";

            Assert.True(undoManager.CanUndo);
            Assert.Single(undoManager.UndoStack);
        }

        /// <summary>
        /// Verifies that undoing a property change restores the old value.
        /// </summary>
        [Fact]
        public void PropertyChange_UndoRestoresOldValue()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "Changed";
            undoManager.Undo();

            Assert.Equal("Initial", source.Name);
        }

        /// <summary>
        /// Verifies that multiple property changes can all be undone individually.
        /// </summary>
        [Fact]
        public void MultiplePropertyChanges_AllCanBeUndone()
        {
            var source = new TestNotifyObject { Name = "A", Value = 1 };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "B";
            source.Value = 2;
            source.Name = "C";

            Assert.Equal(3, undoManager.UndoStack.Count);

            undoManager.Undo();
            Assert.Equal("B", source.Name);

            undoManager.Undo();
            Assert.Equal(1, source.Value);

            undoManager.Undo();
            Assert.Equal("A", source.Name);
        }

        /// <summary>
        /// Verifies that redoing a property change reapplies the new value.
        /// </summary>
        [Fact]
        public void PropertyChange_Redo_ReappliesValue()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "Changed";
            undoManager.Undo();
            undoManager.Redo();

            Assert.Equal("Changed", source.Name);
        }

        /// <summary>
        /// Verifies that property changes are not recorded when the undo manager returns null.
        /// </summary>
        [Fact]
        public void PropertyChange_WhenUndoManagerReturnsNull_DoesNotRecord()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var bridge = new UndoableStateBridge(source, () => null);

            var exception = Record.Exception(() => source.Name = "Changed");

            Assert.Null(exception);
        }

        #endregion

        #region SuspendRecording Tests

        /// <summary>
        /// Verifies that property changes are not recorded when recording is suspended.
        /// </summary>
        [Fact]
        public void SuspendRecording_PropertyChangesNotRecorded()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            using (bridge.SuspendRecording())
            {
                source.Name = "Changed";
            }

            Assert.False(undoManager.CanUndo);
        }

        /// <summary>
        /// Verifies that recording resumes after the suspend scope is disposed.
        /// </summary>
        [Fact]
        public void SuspendRecording_AfterDispose_RecordingResumes()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            using (bridge.SuspendRecording())
            {
                source.Name = "DuringSuspension";
            }

            source.Name = "AfterSuspension";

            Assert.True(undoManager.CanUndo);
            Assert.Single(undoManager.UndoStack);
        }

        /// <summary>
        /// Verifies that suspending recording updates shadow values to match current property values.
        /// </summary>
        [Fact]
        public void SuspendRecording_UpdatesShadowValues()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            using (bridge.SuspendRecording())
            {
                source.Name = "NewBaseline";
            }

            source.Name = "Changed";
            undoManager.Undo();

            // Should undo to "NewBaseline" not "Initial"
            Assert.Equal("NewBaseline", source.Name);
        }

        #endregion

        #region Property Filter Tests - Included

        /// <summary>
        /// Verifies that only included properties are monitored when using an inclusion list.
        /// </summary>
        [Fact]
        public void IncludedProperties_OnlyMonitorsSpecifiedProperties()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                includedProperties: new[] { nameof(TestNotifyObject.Name) });

            source.Name = "Test1";
            source.Value = 42;

            Assert.Single(undoManager.UndoStack);
        }

        /// <summary>
        /// Verifies that properties not in the inclusion list are ignored.
        /// </summary>
        [Fact]
        public void IncludedProperties_IgnoresNonIncludedProperties()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                includedProperties: new[] { nameof(TestNotifyObject.Value) });

            source.Name = "Test";

            Assert.False(undoManager.CanUndo);
        }

        #endregion

        #region Property Filter Tests - Excluded

        /// <summary>
        /// Verifies that excluded properties are not monitored when using an exclusion list.
        /// </summary>
        [Fact]
        public void ExcludedProperties_ExcludesSpecifiedProperties()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                null,
                excludedProperties: new[] { nameof(TestNotifyObject.Value) });

            source.Name = "Test1";
            source.Value = 42;

            Assert.Single(undoManager.UndoStack);
        }

        /// <summary>
        /// Verifies that ExcludeProperty adds a property to the exclusion list dynamically.
        /// </summary>
        [Fact]
        public void ExcludeProperty_AddsToExclusionList()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            bridge.ExcludeProperty(nameof(TestNotifyObject.Value));
            source.Value = 42;

            Assert.False(undoManager.CanUndo);
        }

        /// <summary>
        /// Verifies that ExcludeProperty throws an InvalidOperationException when using an inclusion list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to exclude properties while using an inclusion list.</exception>
        [Fact]
        public void ExcludeProperty_WhenUsingInclusionList_ThrowsInvalidOperationException()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                includedProperties: new[] { nameof(TestNotifyObject.Name) });

            Assert.Throws<InvalidOperationException>(() =>
                bridge.ExcludeProperty(nameof(TestNotifyObject.Value)));
        }

        /// <summary>
        /// Verifies that IncludeProperty removes a property from the exclusion list dynamically.
        /// </summary>
        [Fact]
        public void IncludeProperty_RemovesFromExclusionList()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                null,
                excludedProperties: new[] { nameof(TestNotifyObject.Value) });

            bridge.IncludeProperty(nameof(TestNotifyObject.Value));
            source.Value = 42;

            Assert.True(undoManager.CanUndo);
        }

        /// <summary>
        /// Verifies that IncludeProperty throws an InvalidOperationException when using an inclusion list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when trying to include properties while using an inclusion list.</exception>
        [Fact]
        public void IncludeProperty_WhenUsingInclusionList_ThrowsInvalidOperationException()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(
                source,
                () => undoManager,
                "settings",
                null,
                includedProperties: new[] { nameof(TestNotifyObject.Name) });

            Assert.Throws<InvalidOperationException>(() =>
                bridge.IncludeProperty(nameof(TestNotifyObject.Value)));
        }

        #endregion

        #region Dispose Tests

        /// <summary>
        /// Verifies that Dispose unsubscribes from the source's PropertyChanged event.
        /// </summary>
        [Fact]
        public void Dispose_UnsubscribesFromPropertyChanged()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            bridge.Dispose();
            source.Name = "Changed";

            Assert.False(undoManager.CanUndo);
        }

        /// <summary>
        /// Verifies that Dispose sets the IsDisposed property to true.
        /// </summary>
        [Fact]
        public void Dispose_SetsIsDisposedToTrue()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            bridge.Dispose();

            Assert.True(bridge.IsDisposed);
        }

        /// <summary>
        /// Verifies that calling Dispose multiple times does not throw an exception.
        /// </summary>
        [Fact]
        public void Dispose_MultipleCalls_DoNotThrow()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            var exception = Record.Exception(() =>
            {
                bridge.Dispose();
                bridge.Dispose();
            });

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that the IsDisposed property is initially false.
        /// </summary>
        [Fact]
        public void IsDisposed_InitiallyFalse()
        {
            var source = new TestNotifyObject();
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            Assert.False(bridge.IsDisposed);
        }

        #endregion

        #region OnActionRecorded Callback Tests

        /// <summary>
        /// Verifies that the onActionRecorded callback is invoked when a property change is recorded.
        /// </summary>
        [Fact]
        public void OnActionRecorded_InvokedOnPropertyChange()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            int callbackCount = 0;
            var bridge = new UndoableStateBridge(source, () => undoManager, onActionRecorded: () => callbackCount++);

            source.Name = "Changed";

            Assert.Equal(1, callbackCount);
        }

        /// <summary>
        /// Verifies that the onActionRecorded callback is not invoked during undo/redo replay.
        /// </summary>
        [Fact]
        public void OnActionRecorded_NotInvokedDuringUndoRedo()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            int callbackCount = 0;
            var bridge = new UndoableStateBridge(source, () => undoManager, onActionRecorded: () => callbackCount++);

            source.Name = "Changed";
            Assert.Equal(1, callbackCount);

            // Undo should NOT invoke the callback
            undoManager.Undo();
            Assert.Equal(1, callbackCount);

            // Redo should NOT invoke the callback
            undoManager.Redo();
            Assert.Equal(1, callbackCount);
        }

        /// <summary>
        /// Verifies that the onActionRecorded callback is not invoked when recording is suspended.
        /// </summary>
        [Fact]
        public void OnActionRecorded_NotInvokedWhenSuspended()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            int callbackCount = 0;
            var bridge = new UndoableStateBridge(source, () => undoManager, onActionRecorded: () => callbackCount++);

            using (bridge.SuspendRecording())
            {
                source.Name = "Changed";
            }

            Assert.Equal(0, callbackCount);
        }

        /// <summary>
        /// Verifies that the bridge works correctly when onActionRecorded is null (default behavior).
        /// </summary>
        [Fact]
        public void OnActionRecorded_NullCallback_DoesNotThrow()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            var exception = Record.Exception(() => source.Name = "Changed");

            Assert.Null(exception);
            Assert.True(undoManager.CanUndo);
        }

        #endregion

        #region RefreshShadowValues Tests

        /// <summary>
        /// Verifies that RefreshShadowValues updates internal shadow values to match current property values.
        /// </summary>
        [Fact]
        public void RefreshShadowValues_UpdatesShadowToCurrentValues()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            // Change without recording (simulate external modification)
            using (bridge.SuspendRecording())
            {
                source.Name = "External";
            }

            bridge.RefreshShadowValues();
            source.Name = "Changed";
            undoManager.Undo();

            Assert.Equal("External", source.Name);
        }

        #endregion

        #region Same Value Tests

        /// <summary>
        /// Verifies that setting a property to the same value does not record an action.
        /// </summary>
        [Fact]
        public void PropertyChange_SameValue_DoesNotRecordAction()
        {
            var source = new TestNotifyObject { Name = "Same" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            // This won't trigger PropertyChanged since the TestNotifyObject checks for equality
            source.Name = "Same";

            Assert.False(undoManager.CanUndo);
        }

        #endregion

        #region IsExecutingAction Tests

        /// <summary>
        /// Verifies that property changes during undo/redo operations do not create new undo actions.
        /// </summary>
        [Fact]
        public void WhenUndoManagerIsExecutingAction_DoesNotRecordAction()
        {
            var source = new TestNotifyObject { Name = "Initial" };
            var undoManager = new UndoManager();
            var bridge = new UndoableStateBridge(source, () => undoManager);

            source.Name = "Changed";
            var initialCount = undoManager.UndoStack.Count;

            // During undo, property changes should not create new actions
            undoManager.Undo();

            // Should still only have the original action count (minus the undone one)
            Assert.Equal(initialCount - 1, undoManager.UndoStack.Count);
        }

        #endregion
    }
}
