namespace FrameworkInterfaces
{
    /// <summary>
    /// This is an interface for meta data.
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
    public interface IMetaData
    {
        /// <summary>
        /// Gets and sets the name.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Gets and sets the description.
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// Gets the date/time the item was created.
        /// </summary>
        DateTime CreationDate { get; }

        /// <summary>
        /// Gets the date/time the item was last edited.
        /// </summary>
        DateTime LastModified { get; }
    }
}
