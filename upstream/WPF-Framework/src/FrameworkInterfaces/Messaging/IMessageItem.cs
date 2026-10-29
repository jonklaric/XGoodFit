using System.ComponentModel;

namespace FrameworkInterfaces
{
    /// <summary>
    /// Defines the contract for message items used in the messaging system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Message items represent notifications, warnings, errors, and events that can be
    /// displayed in the message window. Each message has a source that allows users
    /// to navigate to the relevant element when clicking on the message.
    /// </para>
    /// <para>
    /// Implementations must support property change notifications via <see cref="INotifyPropertyChanged"/>
    /// and equality comparison via <see cref="IEquatable{T}"/> for proper collection handling.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    ///     <item> Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMessageItem : INotifyPropertyChanged, IEquatable<IMessageItem>
    {
        /// <summary>
        /// Gets or sets the action to be invoked when the message is clicked.
        /// </summary>
        /// <value>
        /// An <see cref="Action{T}"/> delegate that receives the message item,
        /// typically used to navigate to the message source in the UI.
        /// </value>
        Action<IMessageItem>? MessageAction { get; set; }

        /// <summary>
        /// Gets or sets the unique code identifying this message.
        /// </summary>
        /// <value>
        /// A string code that uniquely identifies the message type within its source.
        /// Typically formatted as "XX-XXX-###" (e.g., "EL-ERR-001").
        /// </value>
        string Code { get; set; }

        /// <summary>
        /// Gets or sets the type of message.
        /// </summary>
        /// <value>
        /// A <see cref="MessageType"/> value indicating the severity or category
        /// (Error, Warning, Message, or Event).
        /// </value>
        MessageType Type { get; set; }

        /// <summary>
        /// Gets or sets the description text of the message.
        /// </summary>
        /// <value>A human-readable description explaining the message.</value>
        string? Description { get; set; }

        /// <summary>
        /// Gets or sets the source object that generated this message.
        /// </summary>
        /// <value>
        /// The object (typically an <see cref="IElement"/> or <see cref="IProject"/>)
        /// that is the origin of this message.
        /// </value>
        object? Source { get; set; }

        /// <summary>
        /// Gets or sets the name of the collection containing the source element.
        /// </summary>
        /// <value>
        /// The name of the parent collection, used for navigating to the source.
        /// </value>
        string? SourceCollectionName { get; set; }

        /// <summary>
        /// Gets or sets the name of the source element.
        /// </summary>
        /// <value>
        /// The name of the element that generated this message.
        /// </value>
        string? SourceName { get; set; }

        /// <summary>
        /// Gets or sets the name of the parameter associated with this message.
        /// </summary>
        /// <value>
        /// The parameter name, or <c>null</c> if the message is not associated
        /// with a specific parameter.
        /// </value>
        string? ParameterName { get; set; }

        /// <summary>
        /// Gets the timestamp when the message was created.
        /// </summary>
        /// <value>
        /// A string in "HH:mm:ss" format representing the time the message was created.
        /// </value>
        string TimeStamp { get; }

        /// <summary>
        /// Converts the message item to a plain text representation.
        /// </summary>
        /// <returns>
        /// A formatted string containing all message properties suitable for
        /// logging or exporting.
        /// </returns>
        string ToText();
    }
}
