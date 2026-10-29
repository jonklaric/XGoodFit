namespace FrameworkInterfaces.Messaging
{
    /// <summary>
    /// Provides extension methods for messaging-related types.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class Extensions
    {
        /// <summary>
        /// Converts a message item to a formatted text string.
        /// </summary>
        /// <param name="message">The message item to convert.</param>
        /// <returns>A formatted string representation of the message.</returns>
        public static string ToText(this IMessageItem message)
        {
            string line = "Type: " + message.Type.ToString() + "; ";
            line += "Time: " + message.TimeStamp + "; ";
            line += "Description: " + message.Description + "; ";
            line += "Source: " + message.SourceCollectionName + "; ";
            line += "Name: " + message.SourceName + "; ";
            line += "Parameter: " + message.ParameterName + ".";
            return line;
        }
    }
}
