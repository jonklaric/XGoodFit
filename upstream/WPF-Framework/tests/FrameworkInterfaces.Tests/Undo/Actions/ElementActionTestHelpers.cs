using FrameworkInterfaces;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace FrameworkInterfaces.Tests.Undo.Actions
{
    /// <summary>
    /// Mock implementation of IElement for testing element action functionality.
    /// </summary>
    internal class MockElement : IElement
    {
        /// <summary>
        /// Gets or sets the name of the element.
        /// </summary>
        public string Name { get; set; } = "TestElement";

        /// <summary>
        /// Gets or sets the display name of the element.
        /// </summary>
        public string DisplayName { get; set; } = "Test Element";

        /// <summary>
        /// Gets or sets the parent collection that contains this element.
        /// </summary>
        public IElementCollection ParentCollection { get; set; } = null!;

        /// <summary>
        /// Gets the image representation of the element.
        /// </summary>
        public ImageSource ElementImage => null!;

        /// <summary>
        /// Gets a value indicating whether this element can be copied from an external source.
        /// </summary>
        public bool CanCopyFromExternal => false;

        /// <summary>
        /// Gets a value indicating whether this element is in a valid state.
        /// </summary>
        public bool IsValid => true;

        /// <summary>
        /// Gets or sets the description of the element.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file name associated with this element.
        /// </summary>
        public string FileName { get; set; } = "test.xml";

        /// <summary>
        /// Gets or sets a value indicating whether this element has unsaved changes.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Gets the name of the element as it appears on disk.
        /// </summary>
        /// <exception cref="NotImplementedException">This property is not implemented in the mock.</exception>
        public string NameOnDisk => throw new NotImplementedException();

        /// <summary>
        /// Gets the creation date of the element.
        /// </summary>
        /// <exception cref="NotImplementedException">This property is not implemented in the mock.</exception>
        public DateTime CreationDate => throw new NotImplementedException();

        /// <summary>
        /// Gets the last modified date of the element.
        /// </summary>
        /// <exception cref="NotImplementedException">This property is not implemented in the mock.</exception>
        public DateTime LastModified => throw new NotImplementedException();

        /// <summary>
        /// Occurs before the element is deleted, allowing cancellation of the operation.
        /// </summary>
        public event PreviewDeletedEventHandler? PreviewDeleted;

        /// <summary>
        /// Occurs when the element has been deleted.
        /// </summary>
        public event DeletedEventHandler? Deleted;

        /// <summary>
        /// Occurs before the element is saved.
        /// </summary>
#pragma warning disable CS0067 // Event is never used (mock implementation satisfies interface)
        public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

        /// <summary>
        /// Occurs when the element has been saved.
        /// </summary>
        public event ObjectSavedEventHandler? ObjectSaved;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        /// <summary>
        /// Creates a copy of this element.
        /// </summary>
        /// <param name="newName">The name for the new copy, or null to use the original name.</param>
        /// <returns>A new MockElement instance with the specified or original name.</returns>
        public IElement Copy(string? newName = null)
        {
            return new MockElement { Name = newName ?? Name, DisplayName = DisplayName };
        }

        /// <summary>
        /// Creates a copy of an element from an external project.
        /// </summary>
        /// <param name="itemName">The name of the item to copy.</param>
        /// <param name="fullFileName">The full file name of the external element.</param>
        /// <returns>A new MockElement instance with the specified name.</returns>
        public IElement CopyFromExternal(string itemName, string fullFileName)
        {
            return new MockElement { Name = itemName };
        }

        /// <summary>
        /// Deletes this element, raising preview and deleted events.
        /// </summary>
        public void Delete()
        {
            bool cancel = false;
            PreviewDeleted?.Invoke(this, ref cancel);
            if (!cancel)
            {
                Deleted?.Invoke(this);
            }
        }

        /// <summary>
        /// Opens this element for editing.
        /// </summary>
        /// <exception cref="NotImplementedException">This method is not implemented in the mock.</exception>
        public void Open()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Saves this element.
        /// </summary>
        public void Save() { }
    }

    /// <summary>
    /// Mock implementation of IElementCollection for testing element collection functionality.
    /// </summary>
    internal class MockElementCollection : IElementCollection
    {
        /// <summary>
        /// The internal list of elements in this collection.
        /// </summary>
        private readonly List<IElement> _elements = new();

        /// <summary>
        /// Gets or sets the name of the collection.
        /// </summary>
        public string Name { get; set; } = "TestCollection";

        /// <summary>
        /// Gets or sets the parent project that contains this collection.
        /// </summary>
        public IProject ParentProject { get; set; } = null!;

        /// <summary>
        /// Gets the read-only collection of child element collections.
        /// </summary>
        public ReadOnlyCollection<IElementCollection> ElementCollections { get; set; } = new ReadOnlyCollection<IElementCollection>(Array.Empty<IElementCollection>());

        /// <summary>
        /// Gets or sets the description of the collection.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the file name associated with this collection.
        /// </summary>
        public string FileName { get; set; } = "collection.xml";

        /// <summary>
        /// Gets or sets a value indicating whether this collection has unsaved changes.
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Occurs when an element is added to the collection.
        /// </summary>
        public event ElementAddedEventHandler? ElementAdded;

        /// <summary>
        /// Occurs when an element is removed from the collection.
        /// </summary>
        public event ElementRemovedEventHandler? ElementRemoved;

#pragma warning disable CS0067 // Events are never used (mock implementation satisfies interface)
        /// <summary>
        /// Occurs when a child element transitions from clean to dirty.
        /// </summary>
        public event EventHandler? ElementIsDirtyChanged;

        /// <summary>
        /// Occurs before the collection is saved.
        /// </summary>
        public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

        /// <summary>
        /// Occurs when the collection has been saved.
        /// </summary>
        public event ObjectSavedEventHandler? ObjectSaved;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        public int Count => _elements.Count;

        /// <summary>
        /// Gets a value indicating whether the collection is read-only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets the name of the collection as it appears on disk.
        /// </summary>
        /// <exception cref="NotImplementedException">This property is not implemented in the mock.</exception>
        public string NameOnDisk => throw new NotImplementedException();

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to get or set.</param>
        /// <returns>The element at the specified index.</returns>
        public IElement this[int index]
        {
            get => _elements[index];
            set => _elements[index] = value;
        }

        /// <summary>
        /// Adds an element to the end of the collection.
        /// </summary>
        /// <param name="item">The element to add.</param>
        public void Add(IElement item)
        {
            _elements.Add(item);
            ElementAdded?.Invoke(item);
        }

        /// <summary>
        /// Removes all elements from the collection.
        /// </summary>
        public void Clear()
        {
            _elements.Clear();
        }

        /// <summary>
        /// Determines whether the collection contains a specific element.
        /// </summary>
        /// <param name="item">The element to locate in the collection.</param>
        /// <returns>True if the element is found; otherwise, false.</returns>
        public bool Contains(IElement item)
        {
            return _elements.Contains(item);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index.
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The zero-based index in the array at which copying begins.</param>
        public void CopyTo(IElement[] array, int arrayIndex)
        {
            _elements.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<IElement> GetEnumerator()
        {
            return _elements.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Determines the index of a specific element in the collection.
        /// </summary>
        /// <param name="item">The element to locate in the collection.</param>
        /// <returns>The index of the element if found; otherwise, -1.</returns>
        public int IndexOf(IElement item)
        {
            return _elements.IndexOf(item);
        }

        /// <summary>
        /// Inserts an element into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="item">The element to insert.</param>
        public void Insert(int index, IElement item)
        {
            _elements.Insert(index, item);
            ElementAdded?.Invoke(item);
        }

        /// <summary>
        /// Removes the first occurrence of a specific element from the collection.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was successfully removed; otherwise, false.</returns>
        public bool Remove(IElement item)
        {
            var result = _elements.Remove(item);
            if (result)
            {
                ElementRemoved?.Invoke(item);
            }
            return result;
        }

        /// <summary>
        /// Removes the element at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            var item = _elements[index];
            _elements.RemoveAt(index);
            ElementRemoved?.Invoke(item);
        }

        /// <summary>
        /// Sorts the elements in the collection using the specified comparer.
        /// </summary>
        /// <param name="sortAction">The comparer to use for sorting.</param>
        public void Sort(IComparer<IElement> sortAction)
        {
            _elements.Sort(sortAction);
        }

        /// <summary>
        /// Moves an element from one position to another within the collection.
        /// </summary>
        /// <param name="element">The element to move.</param>
        /// <param name="startIndex">The current index of the element.</param>
        /// <param name="endIndex">The new index for the element.</param>
        public void MoveElement(IElement element, int startIndex, int endIndex)
        {
            if (startIndex < 0 || startIndex >= _elements.Count)
                return;

            _elements.RemoveAt(startIndex);
            _elements.Insert(endIndex, element);
        }

        /// <summary>
        /// Deletes this collection.
        /// </summary>
        public void Delete() { }

        /// <summary>
        /// Inserts an element from an external project into the collection.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="elementName">The name of the element to insert.</param>
        /// <param name="elementType">The type of the element to insert.</param>
        /// <param name="fullFileName">The full file name of the external element.</param>
        public void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName)
        {
            var element = new MockElement { Name = elementName };
            Insert(index, element);
        }

        /// <summary>
        /// Saves this collection.
        /// </summary>
        public void Save() { }

        /// <summary>
        /// Opens this collection for editing.
        /// </summary>
        /// <exception cref="NotImplementedException">This method is not implemented in the mock.</exception>
        public void Open()
        {
            throw new NotImplementedException();
        }
    }
}
