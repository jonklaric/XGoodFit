using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FrameworkInterfaces;

namespace FrameworkUI.MessageWindow
{
    /// <summary>
    /// A class for a message window item.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Woodrow Lee Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class MessageItem : DependencyObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageItem"/> class.
        /// </summary>
        /// <param name="message">The message item to wrap.</param>
        public MessageItem(IMessageItem message)
        {
            _message = message;

            var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();
            // Bind the message beep and text foreground color
            if (_message.Type == MessageType.Error)
            {
                BindingOperations.SetBinding(this, MessageItem.BeepProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.ErrorBeep)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                BindingOperations.SetBinding(this, MessageItem.ForegroundColorProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.ErrorColor)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
            else if (_message.Type == MessageType.Warning)
            {
                BindingOperations.SetBinding(this, MessageItem.BeepProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.WarningBeep)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                BindingOperations.SetBinding(this, MessageItem.ForegroundColorProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.WarningColor)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
            else if (_message.Type == MessageType.Message)
            {
                BindingOperations.SetBinding(this, MessageItem.BeepProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.MessageBeep)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                BindingOperations.SetBinding(this, MessageItem.ForegroundColorProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.MessageColor)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
            else if (_message.Type == MessageType.Event)
            {
                BindingOperations.SetBinding(this, MessageItem.BeepProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.EventBeep)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
                BindingOperations.SetBinding(this, MessageItem.ForegroundColorProperty, new Binding(nameof(FrameworkInterfaces.Messaging.Messenger.EventColor)) { Source = messenger, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            }
        }

        /// <summary>
        /// The underlying message item interface.
        /// </summary>
        private IMessageItem _message;

        /// <summary>
        /// Gets the underlying message item interface that this wrapper represents.
        /// </summary>
        /// <value>The wrapped <see cref="IMessageItem"/> containing the message details.</value>
        public IMessageItem Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Dependency property for the beep boolean.
        /// </summary>
        public static readonly DependencyProperty BeepProperty = DependencyProperty.Register(nameof(Beep), typeof(bool), typeof(MessageItem), new UIPropertyMetadata(false));

        /// <summary>
        /// Determines whether the computer should beep when adding the item.
        /// </summary>
        public bool Beep
        {
            get { return (bool)GetValue(BeepProperty); }
            set { SetValue(BeepProperty, value); }
        }

        /// <summary>
        /// Dependency property for the message item foreground color.
        /// </summary>
        public static readonly DependencyProperty ForegroundColorProperty = DependencyProperty.Register(nameof(ForegroundColor), typeof(SolidColorBrush), typeof(MessageItem), new UIPropertyMetadata(new SolidColorBrush(Colors.Black)));

        /// <summary>
        /// Gets or sets the message item foreground (text) color.
        /// </summary>
        public SolidColorBrush ForegroundColor
        {
            get { return (SolidColorBrush)GetValue(ForegroundColorProperty); }
            set { SetValue(ForegroundColorProperty, value); }
        }

    }
}
