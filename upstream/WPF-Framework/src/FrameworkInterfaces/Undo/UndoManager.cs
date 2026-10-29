using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Default implementation of <see cref="IUndoManager"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class maintains undo and redo stacks and provides methods to execute,
    /// record, undo, and redo actions. It supports action merging for combining
    /// rapid changes and transactions for grouping multiple actions.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UndoManager : IUndoManager
    {
        #region Fields

        private readonly Stack<IUndoableAction> _undoStack = new Stack<IUndoableAction>();
        private readonly Stack<IUndoableAction> _redoStack = new Stack<IUndoableAction>();
        private int _savePointIndex = 0;
        private int _currentIndex = 0;
        private int _maxUndoLevels = 100;
        private int _isExecutingAction = 0; // 0 = false, 1 = true; use Interlocked for thread-safe TOCTOU guard
        private CompositeAction? _currentTransaction = null;
        private readonly object _lockObject = new object();

        #endregion

        #region Events

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <inheritdoc/>
        public event EventHandler? StateChanged;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public bool CanUndo
        {
            get { lock (_lockObject) { return _undoStack.Count > 0; } }
        }

        /// <inheritdoc/>
        public bool CanRedo
        {
            get { lock (_lockObject) { return _redoStack.Count > 0; } }
        }

        /// <inheritdoc/>
        public string? UndoDescription
        {
            get { lock (_lockObject) { return CanUndo ? _undoStack.Peek().Description : null; } }
        }

        /// <inheritdoc/>
        public string? RedoDescription
        {
            get { lock (_lockObject) { return CanRedo ? _redoStack.Peek().Description : null; } }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IUndoableAction> UndoStack
        {
            get { lock (_lockObject) { return _undoStack.ToList().AsReadOnly(); } }
        }

        /// <inheritdoc/>
        public IReadOnlyList<IUndoableAction> RedoStack
        {
            get { lock (_lockObject) { return _redoStack.ToList().AsReadOnly(); } }
        }

        /// <inheritdoc/>
        public int MaxUndoLevels
        {
            get { return _maxUndoLevels; }
            set
            {
                if (value < 1) value = 1;
                _maxUndoLevels = value;
                TrimUndoStack();
            }
        }

        /// <inheritdoc/>
        public bool IsExecutingAction
        {
            get { return Interlocked.CompareExchange(ref _isExecutingAction, 0, 0) != 0; }
        }

        /// <inheritdoc/>
        public bool HasChangedSinceSave
        {
            get { lock (_lockObject) { return _currentIndex != _savePointIndex; } }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void ExecuteAction(IUndoableAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            // Atomically check-and-set to prevent TOCTOU race
            if (Interlocked.CompareExchange(ref _isExecutingAction, 1, 0) != 0) return;

            // Keep _isExecutingAction set through both Execute and RecordActionInternal so any
            // re-entrant RecordAction triggered by property-change events inside RecordActionInternal
            // sees the guard and is suppressed. Resetting only in the finally block ensures the flag
            // is cleared on any exception path as well.
            try
            {
                action.Execute();
                RecordActionInternal(action);
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingAction, 0);
            }
        }

        /// <inheritdoc/>
        public void RecordAction(IUndoableAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (IsExecutingAction) return;

            RecordActionInternal(action);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            IUndoableAction action;

            lock (_lockObject)
            {
                if (!CanUndo) return;
                action = _undoStack.Pop();
            }

            Interlocked.Exchange(ref _isExecutingAction, 1);
            try
            {
                try
                {
                    action.Undo();
                }
                catch
                {
                    // Restore the action to the undo stack so it isn't lost from
                    // both stacks. Without this, an exception in action.Undo
                    // (e.g. a property setter validation throw, a dynamic-dispatch
                    // RuntimeBinderException, an IndexOutOfRangeException after
                    // the collection shrunk) would silently drop the action —
                    // the user could no longer undo or redo to that point.
                    lock (_lockObject)
                    {
                        _undoStack.Push(action);
                    }
                    throw;
                }

                lock (_lockObject)
                {
                    _redoStack.Push(action);
                    _currentIndex--;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingAction, 0);
            }

            OnStateChanged();
        }

        /// <inheritdoc/>
        public void UndoTo(IUndoableAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            while (CanUndo)
            {
                bool isTarget;
                lock (_lockObject)
                {
                    isTarget = _undoStack.Peek() == action;
                }

                Undo();

                if (isTarget) break;
            }
        }

        /// <inheritdoc/>
        public void Redo()
        {
            IUndoableAction action;

            lock (_lockObject)
            {
                if (!CanRedo) return;
                action = _redoStack.Pop();
            }

            Interlocked.Exchange(ref _isExecutingAction, 1);
            try
            {
                try
                {
                    action.Execute();
                }
                catch
                {
                    // Restore the action to the redo stack so it isn't lost from
                    // both stacks. Mirrors Undo's restoration logic — see comment there.
                    lock (_lockObject)
                    {
                        _redoStack.Push(action);
                    }
                    throw;
                }

                lock (_lockObject)
                {
                    _undoStack.Push(action);
                    _currentIndex++;
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingAction, 0);
            }

            OnStateChanged();
        }

        /// <inheritdoc/>
        public void RedoTo(IUndoableAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            while (CanRedo)
            {
                bool isTarget;
                lock (_lockObject)
                {
                    isTarget = _redoStack.Peek() == action;
                }

                Redo();

                if (isTarget) break;
            }
        }

        /// <inheritdoc/>
        public void Clear()
        {
            lock (_lockObject)
            {
                _undoStack.Clear();
                _redoStack.Clear();
                _currentIndex = 0;
                _savePointIndex = 0;
                _currentTransaction = null;
            }

            OnStateChanged();
        }

        /// <inheritdoc/>
        public void MarkSavePoint()
        {
            lock (_lockObject)
            {
                _savePointIndex = _currentIndex;
            }

            OnStateChanged();
        }

        /// <inheritdoc/>
        public IDisposable BeginTransaction(string description)
        {
            lock (_lockObject)
            {
                if (_currentTransaction != null)
                {
                    // Nested transactions - just return a no-op disposable
                    return new NestedTransactionScope();
                }

                _currentTransaction = new CompositeAction(description);
                return new TransactionScope(this);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Commits the current transaction, adding it to the undo stack.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is typically called automatically when using the <see cref="BeginTransaction"/>
        /// pattern with a using statement. When the <see cref="IDisposable"/> returned by
        /// <see cref="BeginTransaction"/> is disposed, <see cref="CommitTransaction"/> is called automatically.
        /// </para>
        /// <para>
        /// <b>Recommended usage:</b>
        /// <code>
        /// using (undoManager.BeginTransaction("My Operation"))
        /// {
        ///     // Perform multiple changes...
        /// } // CommitTransaction is called automatically here
        /// </code>
        /// </para>
        /// <para>
        /// Direct calls to this method are supported for advanced scenarios where explicit
        /// control over transaction completion is needed outside of a using block.
        /// </para>
        /// </remarks>
        public void CommitTransaction()
        {
            CompositeAction composite;

            lock (_lockObject)
            {
                if (_currentTransaction == null) return;

                composite = _currentTransaction;
                _currentTransaction = null;

                if (composite.Actions.Count == 0) return;
            }

            RecordActionInternal(composite);
        }

        /// <summary>
        /// Rolls back the current transaction, undoing all its actions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Call this method to cancel a transaction and undo all changes made within it.
        /// This is useful for error handling scenarios where you want to revert all changes
        /// if an operation fails partway through.
        /// </para>
        /// <para>
        /// <b>Example usage:</b>
        /// <code>
        /// var transaction = undoManager.BeginTransaction("My Operation");
        /// try
        /// {
        ///     // Perform multiple changes...
        ///     // If all succeeds, commit explicitly or let the using statement handle it
        /// }
        /// catch
        /// {
        ///     undoManager.RollbackTransaction();
        ///     throw;
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// Note: If using a using statement with <see cref="BeginTransaction"/>, the transaction
        /// will be committed on dispose. Call <see cref="RollbackTransaction"/> before the dispose
        /// to prevent the commit if you need to cancel the transaction.
        /// </para>
        /// </remarks>
        public void RollbackTransaction()
        {
            CompositeAction composite;

            lock (_lockObject)
            {
                if (_currentTransaction == null) return;

                composite = _currentTransaction;
                _currentTransaction = null;
            }

            // Undo all actions in reverse order
            Interlocked.Exchange(ref _isExecutingAction, 1);
            try
            {
                foreach (var action in composite.Actions.AsEnumerable().Reverse())
                {
                    action.Undo();
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isExecutingAction, 0);
            }

            // Notify subscribers that undo/redo state changed. Without this, UI
            // elements bound to CanUndo/CanRedo / Description stay stale and the
            // user sees the rolled-back action description on the Undo button as
            // though it were still pending.
            OnStateChanged();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Records an undoable action, merging or stacking it as appropriate.
        /// </summary>
        /// <param name="action">The action to record.</param>
        private void RecordActionInternal(IUndoableAction action)
        {
            lock (_lockObject)
            {
                // If in a transaction, add to composite instead
                if (_currentTransaction != null)
                {
                    _currentTransaction.AddAction(action);
                    return;
                }

                // Try to merge with previous action (for rapid typing, etc.)
                if (_undoStack.Count > 0 && _undoStack.Peek().CanMergeWith(action))
                {
                    var previous = _undoStack.Pop();
                    var merged = previous.MergeWith(action);
                    _undoStack.Push(merged);
                }
                else
                {
                    _undoStack.Push(action);
                }

                // Clear redo stack on new action
                _redoStack.Clear();
                _currentIndex++;

                // Trim undo stack if over limit
                TrimUndoStack();
            }

            OnStateChanged();
        }

        /// <summary>
        /// Removes oldest undo entries when the stack exceeds the configured limit.
        /// </summary>
        private void TrimUndoStack()
        {
            lock (_lockObject)
            {
                if (_undoStack.Count <= _maxUndoLevels) return;

                // Do a single pass: convert once, trim excess oldest entries, rebuild stack once.
                // Stack enumeration order is top-to-bottom (most-recent first), so list[0] is newest.
                var list = _undoStack.ToList();
                int excess = list.Count - _maxUndoLevels;

                // Remove the oldest entries (tail of the list = bottom of the stack)
                list.RemoveRange(list.Count - excess, excess);

                _undoStack.Clear();
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    _undoStack.Push(list[i]);
                }

                // Keep _currentIndex in sync with actual stack depth
                _currentIndex -= excess;

                // Adjust save point — if the discarded actions were before the save point,
                // the save point is no longer reachable
                if (_savePointIndex > _currentIndex)
                {
                    _savePointIndex = -1; // Mark as unreachable
                }
            }
        }

        /// <summary>
        /// Raises property and state-change notifications for undo manager state.
        /// </summary>
        private void OnStateChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UndoDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RedoDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasChangedSinceSave)));
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Disposable scope for managing transactions.
        /// </summary>
        private class TransactionScope : IDisposable
        {
            private readonly UndoManager _manager;
            private bool _disposed = false;

            /// <summary>
            /// Initializes a transaction scope for the specified manager.
            /// </summary>
            /// <param name="manager">The undo manager that owns the transaction.</param>
            public TransactionScope(UndoManager manager)
            {
                _manager = manager;
            }

            /// <summary>
            /// Commits the active transaction once when the scope is disposed.
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _manager.CommitTransaction();
                    _disposed = true;
                }
            }
        }

        /// <summary>
        /// No-op disposable for nested transactions. Nested transactions are silently discarded.
        /// </summary>
        private class NestedTransactionScope : IDisposable
        {
            /// <summary>
            /// Emits a diagnostic warning for an unsupported nested transaction.
            /// </summary>
            public void Dispose()
            {
                System.Diagnostics.Debug.WriteLine(
                    "[UndoManager] WARNING: A nested transaction was silently discarded. " +
                    "Nested BeginTransaction calls are not supported; only the outermost transaction is committed.");
            }
        }

        #endregion
    }
}
