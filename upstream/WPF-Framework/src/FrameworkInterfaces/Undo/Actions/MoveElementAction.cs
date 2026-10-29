using System;

namespace FrameworkInterfaces.Undo.Actions
{
    /// <summary>
    /// Represents moving an element within a collection that can be undone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This action captures the original and new positions of an element
    /// so that undo can restore it to the original position.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MoveElementAction : IUndoableAction
    {
        #region Fields

        private readonly IElementCollection _collection;
        private readonly IElement _element;
        private readonly int _oldIndex;
        private readonly int _newIndex;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MoveElementAction"/> class.
        /// </summary>
        /// <param name="collection">The collection containing the element.</param>
        /// <param name="element">The element that was moved.</param>
        /// <param name="oldIndex">The original index of the element.</param>
        /// <param name="newIndex">The new index of the element.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="collection"/> or <paramref name="element"/> is null.
        /// </exception>
        public MoveElementAction(IElementCollection collection, IElement element, int oldIndex, int newIndex)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _oldIndex = oldIndex;
            _newIndex = newIndex;
            Timestamp = DateTime.Now;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Description => $"Move {_element.DisplayName ?? _element.Name ?? "element"}";

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
        public object Target => _collection;

        /// <summary>
        /// Gets the element that was moved.
        /// </summary>
        public IElement Element => _element;

        /// <summary>
        /// Gets the collection containing the element.
        /// </summary>
        public IElementCollection Collection => _collection;

        /// <summary>
        /// Gets the original index of the element.
        /// </summary>
        public int OldIndex => _oldIndex;

        /// <summary>
        /// Gets the new index of the element.
        /// </summary>
        public int NewIndex => _newIndex;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Execute()
        {
            // Move the element to the new position
            _collection.MoveElement(_element, _oldIndex, _newIndex);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Move the element back to the original position
            _collection.MoveElement(_element, _newIndex, _oldIndex);
        }

        /// <inheritdoc/>
        public bool CanMergeWith(IUndoableAction other)
        {
            // Move actions don't merge
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
