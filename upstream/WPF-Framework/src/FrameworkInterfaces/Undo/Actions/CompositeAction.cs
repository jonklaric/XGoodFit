using System;
using System.Collections.Generic;
using System.Linq;

namespace FrameworkInterfaces.Undo.Actions
{
    /// <summary>
    /// Represents a group of actions that should be undone/redone as a single operation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this class to group multiple related actions into a single undo operation.
    /// For example, deleting multiple elements can be grouped so that Undo restores
    /// all of them at once.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CompositeAction : IUndoableAction
    {
        #region Fields

        private readonly List<IUndoableAction> _actions = new List<IUndoableAction>();
        private readonly string _description;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAction"/> class.
        /// </summary>
        /// <param name="description">Description for the grouped action.</param>
        public CompositeAction(string description)
        {
            _description = description ?? "Multiple actions";
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeAction"/> class with existing actions.
        /// </summary>
        /// <param name="description">Description for the grouped action.</param>
        /// <param name="actions">The actions to include in this composite.</param>
        public CompositeAction(string description, IEnumerable<IUndoableAction> actions)
            : this(description)
        {
            if (actions != null)
            {
                _actions.AddRange(actions);
            }
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Description => _description;

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
        public object? Target => _actions.FirstOrDefault()?.Target;

        /// <summary>
        /// Gets the list of actions in this composite.
        /// </summary>
        public IReadOnlyList<IUndoableAction> Actions => _actions.AsReadOnly();

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an action to this composite.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddAction(IUndoableAction action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            _actions.Add(action);
        }

        /// <inheritdoc/>
        public void Execute()
        {
            // Execute all actions in order
            foreach (var action in _actions)
            {
                action.Execute();
            }
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Undo all actions in reverse order
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                _actions[i].Undo();
            }
        }

        /// <inheritdoc/>
        public bool CanMergeWith(IUndoableAction other)
        {
            // Composite actions don't merge with other actions
            return false;
        }

        /// <inheritdoc/>
        public IUndoableAction MergeWith(IUndoableAction other)
        {
            return this;
        }

        #endregion
    }
}
