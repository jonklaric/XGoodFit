using System;

namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Represents an action that can be undone and redone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface defines the contract for all undoable actions in the application.
    /// Implementations should capture all state needed to execute and reverse the action.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IUndoableAction
    {
        /// <summary>
        /// Gets a human-readable description of the action for display in UI.
        /// </summary>
        /// <example>"Change Name", "Delete Element", "Add Hazard"</example>
        string Description { get; }

        /// <summary>
        /// Gets the timestamp when the action was created.
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Gets the target object this action applies to.
        /// </summary>
        /// <remarks>
        /// This is typically an IElement, IElementCollection, or IProject.
        /// Used to determine which undo stack the action belongs to.
        /// </remarks>
        object? Target { get; }

        /// <summary>
        /// Executes the action. Called for initial execution and redo.
        /// </summary>
        void Execute();

        /// <summary>
        /// Reverses the action.
        /// </summary>
        void Undo();

        /// <summary>
        /// Determines if this action can be merged with another action.
        /// </summary>
        /// <remarks>
        /// Used for combining rapid changes (e.g., keystrokes while typing)
        /// into a single undo operation.
        /// </remarks>
        /// <param name="other">The other action to potentially merge with.</param>
        /// <returns>True if the actions can be merged; otherwise false.</returns>
        bool CanMergeWith(IUndoableAction other);

        /// <summary>
        /// Merges another action into this one.
        /// </summary>
        /// <param name="other">The action to merge.</param>
        /// <returns>A new merged action, or this action if merge modifies in place.</returns>
        IUndoableAction MergeWith(IUndoableAction other);
    }
}
