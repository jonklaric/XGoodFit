using System;

namespace FrameworkInterfaces.Undo.Actions
{
    /// <summary>
    /// Represents removing an element from a collection that can be undone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This action captures the element and its original index so that
    /// undo can re-add it at the same position.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class RemoveElementAction : IUndoableAction
    {
        #region Fields

        private readonly IElementCollection _collection;
        private readonly IElement _element;
        private readonly int _index;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveElementAction"/> class.
        /// </summary>
        /// <param name="collection">The collection the element was removed from.</param>
        /// <param name="element">The element that was removed.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="collection"/> or <paramref name="element"/> is null.
        /// </exception>
        /// <remarks>
        /// The current index of the element in the collection is captured automatically.
        /// </remarks>
        public RemoveElementAction(IElementCollection collection, IElement element)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _index = collection.IndexOf(element);
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveElementAction"/> class
        /// with a specific index.
        /// </summary>
        /// <param name="collection">The collection the element was removed from.</param>
        /// <param name="element">The element that was removed.</param>
        /// <param name="index">The original index of the element.</param>
        public RemoveElementAction(IElementCollection collection, IElement element, int index)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _element = element ?? throw new ArgumentNullException(nameof(element));
            _index = index;
            Timestamp = DateTime.Now;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Description => $"Delete {_element.DisplayName ?? _element.Name ?? "element"}";

        /// <inheritdoc/>
        public DateTime Timestamp { get; }

        /// <inheritdoc/>
        public object Target => _collection;

        /// <summary>
        /// Gets the element that was removed.
        /// </summary>
        public IElement Element => _element;

        /// <summary>
        /// Gets the collection the element was removed from.
        /// </summary>
        public IElementCollection Collection => _collection;

        /// <summary>
        /// Gets the original index of the element.
        /// </summary>
        public int Index => _index;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Execute()
        {
            // Remove the element
            _collection.Remove(_element);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // Re-add the element at the original position
            if (_index < 0 || _index >= _collection.Count)
            {
                _collection.Add(_element);
            }
            else
            {
                _collection.Insert(_index, _element);
            }
        }

        /// <inheritdoc/>
        public bool CanMergeWith(IUndoableAction other)
        {
            // Remove actions don't merge
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
