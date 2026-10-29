using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Manages undo/redo stacks for a specific scope (element, collection, or project).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The undo manager maintains two stacks: one for undo operations and one for redo.
    /// When an action is undone, it moves to the redo stack. When a new action is recorded,
    /// the redo stack is cleared.
    /// </para>
    /// <para>
    /// This interface supports Visual Studio-style per-document undo where each open
    /// element has its own undo history.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IUndoManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets whether an undo operation is available.
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Gets whether a redo operation is available.
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Gets the description of the next undo action (for tooltip/menu display).
        /// </summary>
        /// <value>The description, or null if no undo is available.</value>
        string? UndoDescription { get; }

        /// <summary>
        /// Gets the description of the next redo action (for tooltip/menu display).
        /// </summary>
        /// <value>The description, or null if no redo is available.</value>
        string? RedoDescription { get; }

        /// <summary>
        /// Gets the list of actions that can be undone (most recent first).
        /// </summary>
        /// <remarks>Used for dropdown menu showing undo history.</remarks>
        IReadOnlyList<IUndoableAction> UndoStack { get; }

        /// <summary>
        /// Gets the list of actions that can be redone (most recent first).
        /// </summary>
        /// <remarks>Used for dropdown menu showing redo history.</remarks>
        IReadOnlyList<IUndoableAction> RedoStack { get; }

        /// <summary>
        /// Gets or sets the maximum number of undo levels to maintain.
        /// </summary>
        /// <value>The maximum undo levels. Default is 100.</value>
        int MaxUndoLevels { get; set; }

        /// <summary>
        /// Gets whether an action is currently being executed by the undo manager.
        /// </summary>
        /// <remarks>
        /// Use this to prevent re-recording actions during undo/redo operations.
        /// When true, calls to RecordAction should be ignored.
        /// </remarks>
        bool IsExecutingAction { get; }

        /// <summary>
        /// Gets whether the current state differs from the last save point.
        /// </summary>
        /// <remarks>
        /// This can be used as an alternative to the existing IsDirty tracking.
        /// </remarks>
        bool HasChangedSinceSave { get; }

        /// <summary>
        /// Executes an action and records it on the undo stack.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <remarks>
        /// <para>
        /// Use this when you want the undo manager to execute the action.
        /// The action's Execute() method will be called.
        /// </para>
        /// <para>
        /// <b>Re-entrancy:</b> Calling ExecuteAction from inside another action's
        /// Execute() (or from a property-change event raised by that Execute) is a
        /// no-op — the nested call is silently dropped to prevent recursive recording.
        /// Callers that need to chain multiple actions should use <see cref="BeginTransaction"/>
        /// rather than calling ExecuteAction recursively.
        /// </para>
        /// </remarks>
        void ExecuteAction(IUndoableAction action);

        /// <summary>
        /// Records an action on the undo stack without executing it.
        /// </summary>
        /// <param name="action">The action to record.</param>
        /// <remarks>
        /// Use this when the action has already been performed and you just
        /// need to record it for undo purposes.
        /// </remarks>
        void RecordAction(IUndoableAction action);

        /// <summary>
        /// Undoes the most recent action.
        /// </summary>
        void Undo();

        /// <summary>
        /// Undoes multiple actions up to and including the specified action.
        /// </summary>
        /// <param name="action">The action to undo to (from the UndoStack).</param>
        void UndoTo(IUndoableAction action);

        /// <summary>
        /// Redoes the most recently undone action.
        /// </summary>
        void Redo();

        /// <summary>
        /// Redoes multiple actions up to and including the specified action.
        /// </summary>
        /// <param name="action">The action to redo to (from the RedoStack).</param>
        void RedoTo(IUndoableAction action);

        /// <summary>
        /// Clears both undo and redo stacks.
        /// </summary>
        /// <remarks>
        /// Call this when loading a new document or when you want to
        /// establish a clean state with no undo history.
        /// </remarks>
        void Clear();

        /// <summary>
        /// Marks the current state as the saved state.
        /// </summary>
        /// <remarks>
        /// Call this after saving to update the HasChangedSinceSave property.
        /// </remarks>
        void MarkSavePoint();

        /// <summary>
        /// Begins a transaction that groups multiple actions into one undo operation.
        /// </summary>
        /// <param name="description">Description for the grouped action.</param>
        /// <returns>A disposable transaction object. Dispose to commit the transaction.</returns>
        /// <example>
        /// <code>
        /// using (undoManager.BeginTransaction("Delete multiple elements"))
        /// {
        ///     foreach (var element in elements)
        ///         element.Delete();
        /// }
        /// // All deletes are now a single undo operation
        /// </code>
        /// </example>
        IDisposable BeginTransaction(string description);

        /// <summary>
        /// Raised when undo/redo state changes (stacks modified, action executed).
        /// </summary>
        event EventHandler StateChanged;
    }
}
