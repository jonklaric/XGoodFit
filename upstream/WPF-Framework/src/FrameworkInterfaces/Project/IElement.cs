using System.Windows.Media;

namespace FrameworkInterfaces
{

    /// <summary>
    /// Delegate method to raise event before the element has been deleted.
    /// </summary>
    /// <param name="element">The element to be deleted.</param>
    /// <param name="cancel">Determines if the deletion should be canceled.</param>
    public delegate void PreviewDeletedEventHandler(IElement element, ref bool cancel);

    /// <summary>
    /// Delegate method to raise event when the element has been deleted.
    /// </summary>
    /// <param name="element">The element that was deleted.</param>
    public delegate void DeletedEventHandler(IElement element);

    /// <summary>
    /// This is an interface for project elements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    ///     <item> Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IElement : IMetaData, ISave
    {

        /// <summary>
        /// Gets the display name of the element. This can be different from the Name.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the parent element collection.
        /// </summary>
        IElementCollection ParentCollection { get; }

        /// <summary>
        /// Gets the element image as an ImageSource. This image is used to automatically set icons.
        /// </summary>
        ImageSource ElementImage { get; }

        /// <summary>
        /// Gets an optional resource key for a theme-aware element icon.
        /// When non-null, the project explorer uses
        /// <c>FrameworkElement.SetResourceReference</c> to bind the icon, so it updates
        /// automatically when the theme changes.
        /// When null, the static <see cref="ElementImage"/> is used instead.
        /// </summary>
        string? ElementImageResourceKey => null;

        /// <summary>
        /// Determines if the element can be copied from an external application.
        /// </summary>
        bool CanCopyFromExternal { get; }

        /// <summary>
        /// Returns if the specified element is valid.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Event is raised before the element has been deleted.
        /// </summary>
        event PreviewDeletedEventHandler PreviewDeleted;

        /// <summary>
        /// Event is raised when the element has been deleted.
        /// </summary>
        event DeletedEventHandler Deleted;

        /// <summary>
        /// Copy the element. You can optionally provide a new name when copying the element.
        /// </summary>
        /// <param name="newName">Optional. New name of the copied element.</param>
        /// <returns>A deep copy of the element.</returns>
        IElement Copy(string? newName = null);

        /// <summary>
        /// Copy the element from an external project to disk within the current project.
        /// </summary>
        /// <param name="itemName">The item to copy from.</param>
        /// <param name="fullFileName">The full file name of the project to copy from.</param>
        /// <returns>A deep copy of the element.</returns>
        IElement CopyFromExternal(string itemName, string fullFileName);

        /// <summary>
        /// Delete the element and remove all data from disk.
        /// </summary>
        void Delete();

    }
}
