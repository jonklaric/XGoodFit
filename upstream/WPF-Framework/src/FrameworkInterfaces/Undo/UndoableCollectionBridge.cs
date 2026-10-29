using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Bridges an <see cref="IList{T}"/> that implements <see cref="INotifyCollectionChanged"/>
    /// to the undo/redo system by automatically recording collection changes as undoable actions.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <remarks>
    /// <para>
    /// This class subscribes to the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
    /// and creates appropriate <see cref="IUndoableAction"/> instances for each change type
    /// (Add, Remove, Replace, Reset, Move). These actions are recorded with the
    /// <see cref="IUndoManager"/> for undo/redo support.
    /// </para>
    /// <para>
    /// The bridge works with any collection that implements both <see cref="IList{T}"/> and
    /// <see cref="INotifyCollectionChanged"/>, including <see cref="List{T}"/> wrapped with
    /// collection change notifications, custom collections, or <see cref="ObservableCollection{T}"/>.
    /// </para>
    /// <para>
    /// The bridge maintains a shadow copy of the collection to support Reset (Clear) operations,
    /// which do not provide the removed items in their event arguments.
    /// </para>
    /// <para>
    /// The bridge checks <see cref="IUndoManager.IsExecutingAction"/> to avoid recording
    /// changes that occur during undo/redo operations, preventing infinite recursion.
    /// </para>
    /// <para>
    /// This class implements <see cref="IDisposable"/> and should be disposed when the
    /// collection is no longer needed to prevent memory leaks from event subscriptions.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create the bridge with any IList that implements INotifyCollectionChanged
    /// public class MyElement : ElementBase, IUndoableElement
    /// {
    ///     private MyObservableList&lt;double&gt; _values;
    ///     private UndoableCollectionBridge&lt;double&gt; _valuesBridge;
    ///
    ///     public MyElement()
    ///     {
    ///         _values = new MyObservableList&lt;double&gt;();
    ///         _valuesBridge = new UndoableCollectionBridge&lt;double&gt;(
    ///             _values,
    ///             () => IsUndoEnabled ? _undoManager : null,
    ///             "values"
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
    public class UndoableCollectionBridge<T> : IDisposable
    {
        #region Fields

        /// <summary>
        /// The collection being monitored for changes.
        /// </summary>
        private readonly IList<T> _collection;

        /// <summary>
        /// The collection stored as dynamic to allow proper method dispatch.
        /// </summary>
        /// <remarks>
        /// This is necessary because some collections (like those extending List&lt;T&gt;)
        /// may hide base class methods with the 'new' keyword instead of overriding them.
        /// When accessed via IList&lt;T&gt;, C# static dispatch calls the base class methods,
        /// which don't fire CollectionChanged events. Using dynamic ensures we call the
        /// actual runtime type's methods.
        /// </remarks>
        private readonly dynamic _dynamicCollection;

        /// <summary>
        /// The collection cast as INotifyCollectionChanged for event subscription.
        /// </summary>
        private readonly INotifyCollectionChanged _notifyCollection;

        /// <summary>
        /// A function that returns the current undo manager, or null if undo is disabled.
        /// Using a delegate allows dynamic checking of IsUndoEnabled.
        /// </summary>
        private readonly Func<IUndoManager?> _getUndoManager;

        /// <summary>
        /// A human-readable description of the collection for use in undo action descriptions.
        /// </summary>
        private readonly string _collectionDescription;

        /// <summary>
        /// The target object that owns this collection, used for undo action association.
        /// </summary>
        private readonly object? _target;

        /// <summary>
        /// A shadow copy of the collection used to capture items during Reset (Clear) operations.
        /// </summary>
        private List<T>? _shadowCopy;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableCollectionBridge{T}"/> class.
        /// </summary>
        /// <param name="collection">
        /// The <see cref="IList{T}"/> to monitor for changes. Must also implement
        /// <see cref="INotifyCollectionChanged"/>.
        /// </param>
        /// <param name="getUndoManager">
        /// A function that returns the <see cref="IUndoManager"/> to use for recording actions,
        /// or null if undo is currently disabled. This allows dynamic checking of undo state.
        /// </param>
        /// <param name="collectionDescription">
        /// A human-readable description of the collection (e.g., "probability ordinates", "items").
        /// This is used in the descriptions of undo actions.
        /// </param>
        /// <param name="target">
        /// Optional. The target object that owns this collection. This is passed to the
        /// <see cref="IUndoableAction.Target"/> property of created actions.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="collection"/> or <paramref name="getUndoManager"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="collection"/> does not implement <see cref="INotifyCollectionChanged"/>.
        /// </exception>
        public UndoableCollectionBridge(
            IList<T> collection,
            Func<IUndoManager?> getUndoManager,
            string collectionDescription = "collection",
            object? target = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _dynamicCollection = collection; // Store as dynamic for proper method dispatch
            _getUndoManager = getUndoManager ?? throw new ArgumentNullException(nameof(getUndoManager));
            _collectionDescription = collectionDescription ?? "collection";
            _target = target;

            // Verify the collection implements INotifyCollectionChanged
            _notifyCollection = collection as INotifyCollectionChanged
                ?? throw new ArgumentException(
                    $"Collection must implement {nameof(INotifyCollectionChanged)}.",
                    nameof(collection));

            // Initialize the shadow copy with current collection contents
            _shadowCopy = new List<T>(_collection);

            // Subscribe to collection changes
            _notifyCollection.CollectionChanged += OnCollectionChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the collection being monitored by this bridge.
        /// </summary>
        /// <value>
        /// The <see cref="IList{T}"/> that this bridge is attached to.
        /// </value>
        public IList<T> Collection => _collection;

        /// <summary>
        /// Gets or sets an optional callback that wraps the Clear+Add restore loop
        /// during undo/redo of Reset actions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a Reset action is undone or redone, the bridge must Clear the collection
        /// and re-add all items from the captured state. Without this wrapper, each Clear
        /// and Add call fires collection changed events, which may trigger expensive operations
        /// (e.g., recalculating plotting positions on an empty collection during Clear).
        /// </para>
        /// <para>
        /// Set this to a delegate that suppresses collection change events during the
        /// restore operation and raises a single Reset event afterward. For example:
        /// </para>
        /// <code>
        /// bridge.BulkRestoreWrapper = (restoreAction) =>
        /// {
        ///     collection.SuppressCollectionChanged = true;
        ///     restoreAction();
        ///     collection.SuppressCollectionChanged = false;
        ///     collection.RaiseCollectionChangedReset();
        /// };
        /// </code>
        /// <para>
        /// If this property is null, the restore loop runs without wrapping.
        /// </para>
        /// </remarks>
        public Action<Action>? BulkRestoreWrapper { get; set; }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the <see cref="INotifyCollectionChanged.CollectionChanged"/> event
        /// by creating and recording appropriate undo actions.
        /// </summary>
        /// <param name="sender">The source of the event (the collection).</param>
        /// <param name="e">
        /// The <see cref="NotifyCollectionChangedEventArgs"/> containing information
        /// about the change that occurred.
        /// </param>
        /// <remarks>
        /// This method:
        /// <list type="bullet">
        /// <item>Checks if undo is enabled and not currently executing an undo/redo operation</item>
        /// <item>Creates the appropriate action based on the change type</item>
        /// <item>Records the action with the undo manager</item>
        /// <item>Updates the shadow copy to reflect the current collection state</item>
        /// </list>
        /// </remarks>
        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_disposed) return;
            var undoManager = _getUndoManager();

            // If undo is disabled or we're executing an undo/redo, just update the shadow and return
            if (undoManager == null || undoManager.IsExecutingAction)
            {
                UpdateShadowCopy();
                return;
            }

            // Create the appropriate action based on the change type
            IUndoableAction? action = CreateActionForChange(e);

            // Record the action if one was created
            if (action != null)
            {
                undoManager.RecordAction(action);
            }

            // Update the shadow copy to match the current collection state
            UpdateShadowCopy();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates an <see cref="IUndoableAction"/> appropriate for the given collection change.
        /// </summary>
        /// <param name="e">
        /// The <see cref="NotifyCollectionChangedEventArgs"/> describing the change.
        /// </param>
        /// <returns>
        /// An <see cref="IUndoableAction"/> that can undo and redo the change,
        /// or null if no action could be created.
        /// </returns>
        /// <remarks>
        /// This method handles the following change types:
        /// <list type="bullet">
        /// <item><see cref="NotifyCollectionChangedAction.Add"/>: Item(s) added to collection</item>
        /// <item><see cref="NotifyCollectionChangedAction.Remove"/>: Item(s) removed from collection</item>
        /// <item><see cref="NotifyCollectionChangedAction.Replace"/>: Item(s) replaced in collection</item>
        /// <item><see cref="NotifyCollectionChangedAction.Reset"/>: Collection cleared (uses shadow copy)</item>
        /// <item><see cref="NotifyCollectionChangedAction.Move"/>: Item moved within collection</item>
        /// </list>
        /// </remarks>
        private IUndoableAction? CreateActionForChange(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    return CreateAddAction(e);

                case NotifyCollectionChangedAction.Remove:
                    return CreateRemoveAction(e);

                case NotifyCollectionChangedAction.Replace:
                    return CreateReplaceAction(e);

                case NotifyCollectionChangedAction.Reset:
                    return CreateResetAction();

                case NotifyCollectionChangedAction.Move:
                    return CreateMoveAction(e);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Creates an action to undo/redo an Add operation.
        /// </summary>
        /// <param name="e">The event arguments containing the added items and their starting index.</param>
        /// <returns>A <see cref="DelegateAction"/> that removes the items on undo and re-adds them on redo.</returns>
        /// <remarks>
        /// The undo operation removes items from the collection at their original indices.
        /// The redo operation re-inserts the items at their original indices.
        /// </remarks>
        private IUndoableAction? CreateAddAction(NotifyCollectionChangedEventArgs e)
        {
            // Capture the added items and their starting index
            var addedItems = e.NewItems?.Cast<T>().ToList() ?? new List<T>();
            int startIndex = e.NewStartingIndex;

            if (addedItems.Count == 0) return null;

            string description = addedItems.Count == 1
                ? $"Add to {_collectionDescription}"
                : $"Add {addedItems.Count} items to {_collectionDescription}";

            return new DelegateAction(
                description,
                execute: () =>
                {
                    // Re-add the items at their original positions
                    // Use _dynamicCollection to ensure the runtime type's Insert method is called
                    for (int i = 0; i < addedItems.Count; i++)
                    {
                        _dynamicCollection.Insert(startIndex + i, addedItems[i]);
                    }
                },
                undo: () =>
                {
                    // Remove the items (in reverse order to maintain indices)
                    // Use _dynamicCollection to ensure the runtime type's RemoveAt method is called
                    for (int i = addedItems.Count - 1; i >= 0; i--)
                    {
                        _dynamicCollection.RemoveAt(startIndex + i);
                    }
                },
                target: _target
            );
        }

        /// <summary>
        /// Creates an action to undo/redo a Remove operation.
        /// </summary>
        /// <param name="e">The event arguments containing the removed items and their original starting index.</param>
        /// <returns>A <see cref="DelegateAction"/> that re-inserts the items on undo and removes them on redo.</returns>
        /// <remarks>
        /// The undo operation re-inserts items at their original indices.
        /// The redo operation removes the items again.
        /// </remarks>
        private IUndoableAction? CreateRemoveAction(NotifyCollectionChangedEventArgs e)
        {
            // Capture the removed items and their original index
            var removedItems = e.OldItems?.Cast<T>().ToList() ?? new List<T>();
            int startIndex = e.OldStartingIndex;

            if (removedItems.Count == 0) return null;

            string description = removedItems.Count == 1
                ? $"Remove from {_collectionDescription}"
                : $"Remove {removedItems.Count} items from {_collectionDescription}";

            return new DelegateAction(
                description,
                execute: () =>
                {
                    // Remove the items (in reverse order to maintain indices)
                    // Use _dynamicCollection to ensure the runtime type's RemoveAt method is called
                    for (int i = removedItems.Count - 1; i >= 0; i--)
                    {
                        _dynamicCollection.RemoveAt(startIndex + i);
                    }
                },
                undo: () =>
                {
                    // Re-insert the items at their original positions
                    // Use _dynamicCollection to ensure the runtime type's Insert method is called
                    for (int i = 0; i < removedItems.Count; i++)
                    {
                        _dynamicCollection.Insert(startIndex + i, removedItems[i]);
                    }
                },
                target: _target
            );
        }

        /// <summary>
        /// Creates an action to undo/redo a Replace operation.
        /// </summary>
        /// <param name="e">The event arguments containing the old and new items.</param>
        /// <returns>A <see cref="DelegateAction"/> that restores old values on undo and applies new values on redo.</returns>
        /// <remarks>
        /// The undo operation replaces the new items with the old items.
        /// The redo operation replaces the old items with the new items.
        /// </remarks>
        private IUndoableAction? CreateReplaceAction(NotifyCollectionChangedEventArgs e)
        {
            // Capture the old and new items
            var oldItems = e.OldItems?.Cast<T>().ToList() ?? new List<T>();
            var newItems = e.NewItems?.Cast<T>().ToList() ?? new List<T>();
            int startIndex = e.NewStartingIndex;

            if (oldItems.Count == 0 || newItems.Count == 0) return null;

            string description = oldItems.Count == 1
                ? $"Change {_collectionDescription}"
                : $"Change {oldItems.Count} items in {_collectionDescription}";

            return new DelegateAction(
                description,
                execute: () =>
                {
                    // Replace with new items
                    // Use _dynamicCollection to ensure the runtime type's indexer is called
                    for (int i = 0; i < newItems.Count; i++)
                    {
                        _dynamicCollection[startIndex + i] = newItems[i];
                    }
                },
                undo: () =>
                {
                    // Replace with old items
                    // Use _dynamicCollection to ensure the runtime type's indexer is called
                    for (int i = 0; i < oldItems.Count; i++)
                    {
                        _dynamicCollection[startIndex + i] = oldItems[i];
                    }
                },
                target: _target
            );
        }

        /// <summary>
        /// Creates an action to undo/redo a Reset operation.
        /// </summary>
        /// <returns>A <see cref="DelegateAction"/> that restores the previous state on undo and the new state on redo, or null if nothing changed.</returns>
        /// <remarks>
        /// <para>
        /// The Reset action is raised by various collection operations including Clear, AddRange,
        /// and other bulk operations. The event arguments do not contain information about what
        /// changed, which is why we maintain a shadow copy of the collection to capture the
        /// state before the change.
        /// </para>
        /// <para>
        /// This method captures both the before state (shadow copy) and after state (current collection)
        /// to properly support undo/redo for any Reset operation, not just Clear.
        /// </para>
        /// </remarks>
        private IUndoableAction? CreateResetAction()
        {
            // Capture the state before the change (from shadow copy)
            var stateBefore = _shadowCopy != null ? new List<T>(_shadowCopy) : new List<T>();

            // Capture the state after the change (current collection)
            var stateAfter = new List<T>(_collection);

            // If nothing changed, don't create an action
            if (stateBefore.Count == stateAfter.Count && stateBefore.SequenceEqual(stateAfter))
            {
                return null;
            }

            // Determine the appropriate description based on what changed
            string description;
            if (stateAfter.Count == 0)
            {
                description = $"Clear {_collectionDescription}";
            }
            else if (stateBefore.Count == 0)
            {
                description = $"Add {stateAfter.Count} items to {_collectionDescription}";
            }
            else if (stateAfter.Count > stateBefore.Count)
            {
                description = $"Add {stateAfter.Count - stateBefore.Count} items to {_collectionDescription}";
            }
            else if (stateAfter.Count < stateBefore.Count)
            {
                description = $"Remove {stateBefore.Count - stateAfter.Count} items from {_collectionDescription}";
            }
            else
            {
                description = $"Change {_collectionDescription}";
            }

            return new DelegateAction(
                description,
                execute: () =>
                {
                    // Restore the after state
                    // Use _dynamicCollection to ensure the runtime type's Clear/Add methods are called
                    Action restore = () =>
                    {
                        _dynamicCollection.Clear();
                        foreach (var item in stateAfter)
                        {
                            _dynamicCollection.Add(item);
                        }
                    };

                    if (BulkRestoreWrapper != null)
                        BulkRestoreWrapper(restore);
                    else
                        restore();
                },
                undo: () =>
                {
                    // Restore the before state
                    // Use _dynamicCollection to ensure the runtime type's Clear/Add methods are called
                    Action restore = () =>
                    {
                        _dynamicCollection.Clear();
                        foreach (var item in stateBefore)
                        {
                            _dynamicCollection.Add(item);
                        }
                    };

                    if (BulkRestoreWrapper != null)
                        BulkRestoreWrapper(restore);
                    else
                        restore();
                },
                target: _target
            );
        }

        /// <summary>
        /// Creates an action to undo/redo a Move operation.
        /// </summary>
        /// <param name="e">The event arguments containing the old and new indices.</param>
        /// <returns>A <see cref="DelegateAction"/> that moves items back on undo and forward on redo.</returns>
        /// <remarks>
        /// The undo operation moves the item back to its original index.
        /// The redo operation moves the item to the new index again.
        /// For collections that don't have a native Move method, this is implemented
        /// as a remove followed by an insert.
        /// </remarks>
        private IUndoableAction? CreateMoveAction(NotifyCollectionChangedEventArgs e)
        {
            int oldIndex = e.OldStartingIndex;
            int newIndex = e.NewStartingIndex;

            if (oldIndex == newIndex) return null;

            string description = $"Move item in {_collectionDescription}";

            return new DelegateAction(
                description,
                execute: () =>
                {
                    MoveItem(oldIndex, newIndex);
                },
                undo: () =>
                {
                    MoveItem(newIndex, oldIndex);
                },
                target: _target
            );
        }

        /// <summary>
        /// Moves an item from one index to another in the collection.
        /// </summary>
        /// <param name="fromIndex">The current index of the item.</param>
        /// <param name="toIndex">The target index for the item.</param>
        /// <remarks>
        /// If the collection is an <see cref="ObservableCollection{T}"/>, uses its native
        /// Move method. Otherwise, performs a remove and insert operation.
        /// Uses _dynamicCollection to ensure the runtime type's methods are called.
        /// </remarks>
        private void MoveItem(int fromIndex, int toIndex)
        {
            // Use native Move if available (ObservableCollection)
            if (_collection is ObservableCollection<T> observableCollection)
            {
                observableCollection.Move(fromIndex, toIndex);
            }
            else
            {
                // Manual move: remove and insert
                // Use _dynamicCollection to ensure the runtime type's methods are called
                T item = _collection[fromIndex]; // Read is safe via interface
                _dynamicCollection.RemoveAt(fromIndex);
                _dynamicCollection.Insert(toIndex, item);
            }
        }

        /// <summary>
        /// Updates the shadow copy to match the current collection state.
        /// </summary>
        /// <remarks>
        /// This method should be called after every collection change to keep
        /// the shadow copy synchronized. The shadow copy is essential for
        /// handling Reset (Clear) operations.
        /// </remarks>
        private void UpdateShadowCopy()
        {
            _shadowCopy = new List<T>(_collection);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by the <see cref="UndoableCollectionBridge{T}"/>.
        /// </summary>
        /// <remarks>
        /// This method unsubscribes from the <see cref="INotifyCollectionChanged.CollectionChanged"/>
        /// event to prevent memory leaks. Always call <see cref="Dispose()"/> when you are finished
        /// using the bridge, or use a using statement.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="UndoableCollectionBridge{T}"/>
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// true to release both managed and unmanaged resources;
        /// false to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from collection events
                    _notifyCollection.CollectionChanged -= OnCollectionChanged;

                    // Clear the shadow copy
                    _shadowCopy?.Clear();
                    _shadowCopy = null;
                }

                _disposed = true;
            }
        }

        #endregion
    }
}
