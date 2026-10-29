namespace FrameworkInterfaces
{
    /// <summary>
    /// Defines the types of messages that can be displayed in the messaging system.
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
    public enum MessageType
    {
        /// <summary>
        /// Indicates an error message requiring user attention.
        /// </summary>
        Error,

        /// <summary>
        /// Indicates a warning message about potential issues.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates an informational message.
        /// </summary>
        Message,

        /// <summary>
        /// Indicates a log event message.
        /// </summary>
        Event
    }
}
