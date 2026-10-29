using System;

namespace FrameworkInterfaces.Undo.Actions
{
    /// <summary>
    /// Represents adding an element to a collection that can be undone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This action captures the element and its insertion index so that
    /// undo can remove it and redo can re-add it at the same position.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class AddElementAction : IUndoableAction
    {
        #region Fields

        private readonly IElementCollection _collection;
        private readonly IElement _element;
        private readonly int _index;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="AddElementAction"/> class.
        /// </summary>
        /// <param name="collection">The collection the element was added to.</param>
        /// <param name="element">The element that was added.</param>
        /// <param name="index">
        /// The index where the element was inserted.
        /// If -1, the element was added to the end.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="collection"/> or <paramref name="element"/> is null.
        /// </exception>
        public AddElementAction(IElementCollection collection, IElement element, int index = -1)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _index = index >= 0 ? index : collection.Count;
            Timestamp = DateTime.Now;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Description => $"Add {_element.DisplayName ?? _element.Name ?? "element"}";

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
        public object Target => _collection;

        /// <summary>
        /// Gets the element that was added.
        /// </summary>
        public IElement Element => _element;

        /// <summary>
        /// Gets the collection the element was added to.
        /// </summary>
        public IElementCollection Collection => _collection;

        /// <summary>
        /// Gets the index where the element was inserted.
        /// </summary>
        public int Index => _index;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Execute()
        {
            // Re-add the element at the original position
            if (_index >= _collection.Count)
            {
                _collection.Add(_element);
            }
            else
            {
                _collection.Insert(_index, _element);
            }
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Remove the element
            _collection.Remove(_element);
        }

        /// <inheritdoc/>
        public bool CanMergeWith(IUndoableAction other)
        {
            // Add actions don't merge
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
