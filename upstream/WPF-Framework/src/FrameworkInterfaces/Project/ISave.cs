using System.ComponentModel;

namespace FrameworkInterfaces
{

    /// <summary>
    /// Delegate method to raise event before an object has been saved.
    /// </summary>
    /// <param name="sender">The object that will be saved.</param>
    /// <param name="cancel">Determines if the save should be canceled.</param>
    public delegate void PreviewObjectSavedEventHandler(ISave sender, ref bool cancel);

    /// <summary>
    /// Delegate method to raise event when an object has been saved.
    /// </summary>
    /// <param name="sender">The object that was saved.</param>
    public delegate void ObjectSavedEventHandler(ISave sender);

    /// <summary>
    /// This is an interface for managing data IO.
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
    /// This is a placeholder and will likely need to be updated in the future. The challenge lies with formats to save to. If it is a database (e.g. sqlite) then each item can save independently.
    /// If it is another format like XML then the whole document needs to be saved at once and each IElement implementation can't be saved independently.
    /// </para>
    /// </remarks>
    public interface ISave : INotifyPropertyChanged
    {

        /// <summary>
        /// Determines if there are unsaved changes. If IsDirty = True, then there are unsaved changes.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Represents the name as it appears on disk.
        /// </summary>
        string NameOnDisk { get; }

        /// <summary>
        /// Event is raised event before an object has been saved.
        /// </summary>
        event PreviewObjectSavedEventHandler PreviewObjectSaved;

        /// <summary>
        /// Event is raised when the object has been saved.
        /// </summary>
        event ObjectSavedEventHandler ObjectSaved;

        /// <summary>
        /// Load data from disk.
        /// </summary>
        void Open();

        /// <summary>
        /// Save data to disk.
        /// </summary>
        void Save();


    }
}
