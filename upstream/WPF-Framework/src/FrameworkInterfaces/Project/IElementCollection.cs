using System.Collections.ObjectModel;

namespace FrameworkInterfaces
{

    /// <summary>
    /// Delegate method to raise event when adding an element.
    /// </summary>
    /// <param name="element">The new element to add.</param>
    public delegate void ElementAddedEventHandler(IElement element);

    /// <summary>
    /// Delegate method to raise event when removing an element.
    /// </summary>
    /// <param name="element">The element to remove.</param>
    public delegate void ElementRemovedEventHandler(IElement element);

    /// <summary>
    /// This is an interface for a collection of project elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    ///     <item> Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    /// </list>
    /// </para>
    /// <para>
    /// The project element collection interface is designed for interacting with TreeView items that contain all of the sub-tree items for a given data type (e.g. Hazards).
    /// </para>
    /// </remarks>
    public interface IElementCollection : IList<IElement>, ISave
    {

        /// <summary>
        /// Gets and the name of the element collection.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the parent project of the element collection.
        /// </summary>
        IProject ParentProject { get; }

        /// <summary>
        /// Gets and sets the read only collection of element collections.
        /// </summary>
        ReadOnlyCollection<IElementCollection> ElementCollections { get; set; }

        /// <summary>
        /// Event is raised when adding an element.
        /// </summary>
        event ElementAddedEventHandler ElementAdded;

        /// <summary>
        /// Event is raised when removing an element.
        /// </summary>
        event ElementRemovedEventHandler ElementRemoved;

        /// <summary>
        /// Event raised when a child element transitions from
        /// <see cref="ISave.IsDirty"/> <c>false</c> to <c>true</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Lets the owning project aggregate child-dirty state into
        /// <see cref="ISave.IsDirty"/> at the project level without forcing the
        /// collection to flip its own <see cref="ISave.IsDirty"/>, which is reserved
        /// for structural changes (Add / Remove / Move / Sort).
        /// </para>
        /// <para>
        /// Only the <c>false → true</c> transition propagates. Clean transitions
        /// from <see cref="ISave.Save"/> do not raise this event, so a save does
        /// not inadvertently re-dirty a level that just cleared itself.
        /// </para>
        /// </remarks>
        event EventHandler? ElementIsDirtyChanged;

        /// <summary>
        /// Sort the collection.
        /// </summary>
        /// <param name="sortAction">Method to compare two objects.</param>
        void Sort(IComparer<IElement> sortAction);

        /// <summary>
        /// Move element within collection.
        /// </summary>
        /// <param name="element">The element to move.</param>
        /// <param name="startIndex">The start position of the element to move.</param>
        /// <param name="endIndex">The end position of the element to move.</param>
        void MoveElement(IElement element, int startIndex, int endIndex);

        /// <summary>
        /// Delete the element collection and remove all data from disk.
        /// </summary>
        void Delete();

        /// <summary>
        /// Copy the element from an external project to disk within the current project.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="elementName">The element name.</param>
        /// <param name="elementType">The element type.</param>
        /// <param name="fullFileName">The full file name of the project to copy from.</param>
        void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName);

    }
}
