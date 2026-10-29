using System.ComponentModel;
using System.IO;
using System.Windows.Media;

namespace FrameworkInterfaces.Messaging
{
    /// <summary>
    /// The Messenger class provides a centralized messaging system for the application.
    /// This class implements the Singleton pattern to ensure only one instance exists.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Messenger class manages messages, warnings, errors, and events throughout the application.
    /// Messages are keyed by source and code to prevent duplicates while allowing different sources
    /// to have the same message code.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil </item>
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var messenger = Messenger.GetInstance();
    /// messenger.Add(new BasicMessageItem(MessageType.Error, "An error occurred", sourceObject, "Collection", "Name"));
    /// </code>
    /// </example>
    public class Messenger : INotifyPropertyChanged
    {
        #region Singleton Implementation

        /// <summary>
        /// Lazy initialization for thread-safe singleton pattern.
        /// </summary>
        private static readonly Lazy<Messenger> _lazyInstance = new Lazy<Messenger>(() => new Messenger());

        /// <summary>
        /// Private constructor to prevent external instantiation.
        /// </summary>
        private Messenger() { }

        /// <summary>
        /// Gets the singleton instance of the Messenger class.
        /// This method is thread-safe.
        /// </summary>
        /// <returns>The single instance of the Messenger class.</returns>
        public static Messenger GetInstance()
        {
            return _lazyInstance.Value;
        }

        #endregion

        #region Private Fields

        /// <summary>
        /// Dictionary of messages keyed by source object, then by message code.
        /// This structure allows different sources to have the same message code
        /// while preventing duplicate codes from the same source.
        /// </summary>
        private readonly Dictionary<object, Dictionary<string, IMessageItem>> _messagesBySource = new Dictionary<object, Dictionary<string, IMessageItem>>();
        private readonly object _lockObject = new object();

        private bool _writeToFile = false;
        private string _textFileName = string.Empty;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showMessages = true;
        private bool _showEvents = true;
        private bool _errorBeep;
        private bool _warningBeep;
        private bool _messageBeep;
        private bool _eventBeep;
        private static readonly SolidColorBrush _defaultErrorColor = CreateFrozenBrush(Color.FromRgb(228, 20, 0));
        private static readonly SolidColorBrush _defaultWarningColor = CreateFrozenBrush(Color.FromRgb(229, 160, 0));
        private static readonly SolidColorBrush _defaultMessageColor = CreateFrozenBrush(Color.FromRgb(26, 161, 226));
        private static readonly SolidColorBrush _defaultEventColor = CreateFrozenBrush(Color.FromRgb(0, 206, 209));

        private SolidColorBrush _errorColor = _defaultErrorColor;
        private SolidColorBrush _warningColor = _defaultWarningColor;
        private SolidColorBrush _messageColor = _defaultMessageColor;
        private SolidColorBrush _eventColor = _defaultEventColor;

        #endregion

        #region Static Helpers

        /// <summary>
        /// Creates an immutable brush for use as a default message color.
        /// </summary>
        /// <param name="color">The color to apply to the brush.</param>
        /// <returns>A frozen solid-color brush.</returns>
        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when one or more messages are added to the messenger.
        /// </summary>
        public event MessageAddedEventHandler? MessagesAdded;

        /// <summary>
        /// Delegate for the <see cref="MessagesAdded"/> event.
        /// </summary>
        /// <param name="newMessages">Array of newly added message items.</param>
        public delegate void MessageAddedEventHandler(IMessageItem[] newMessages);

        /// <summary>
        /// Occurs when one or more messages are removed from the messenger.
        /// </summary>
        public event MessageRemovedEventHandler? MessagesRemoved;

        /// <summary>
        /// Delegate for the <see cref="MessagesRemoved"/> event.
        /// </summary>
        /// <param name="oldMessages">Array of removed message items.</param>
        public delegate void MessageRemovedEventHandler(IMessageItem[] oldMessages);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file path for messages to be written to disk.
        /// </summary>
        /// <value>The full file path for the message log file.</value>
        public string TextFileName
        {
            get => _textFileName;
            set
            {
                if (_textFileName == value) { return; }
                _textFileName = value ?? string.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TextFileName)));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether messages should be written to disk.
        /// </summary>
        /// <value><c>true</c> if messages should be written to disk; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// This property is currently not used but is reserved for future implementation
        /// of automatic message logging to the file specified by <see cref="TextFileName"/>.
        /// </remarks>
        public bool WriteToFile
        {
            get => _writeToFile;
            set
            {
                if (_writeToFile == value) { return; }
                _writeToFile = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WriteToFile)));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether error messages should be shown in the message window.
        /// </summary>
        /// <value><c>true</c> if error messages should be shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowErrors
        {
            get { return _showErrors; }
            set
            {
                if (_showErrors != value)
                {
                    _showErrors = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowErrors)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether warning messages should be shown in the message window.
        /// </summary>
        /// <value><c>true</c> if warning messages should be shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowWarnings
        {
            get { return _showWarnings; }
            set
            {
                if (_showWarnings != value)
                {
                    _showWarnings = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowWarnings)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether normal messages should be shown in the message window.
        /// </summary>
        /// <value><c>true</c> if normal messages should be shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowMessages
        {
            get { return _showMessages; }
            set
            {
                if (_showMessages != value)
                {
                    _showMessages = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowMessages)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether log event messages should be shown in the message window.
        /// </summary>
        /// <value><c>true</c> if event messages should be shown; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowEvents
        {
            get { return _showEvents; }
            set
            {
                if (_showEvents != value)
                {
                    _showEvents = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowEvents)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether error messages should trigger an audible beep.
        /// </summary>
        /// <value><c>true</c> if error messages should beep; otherwise, <c>false</c>.</value>
        public bool ErrorBeep
        {
            get { return _errorBeep; }
            set
            {
                if (_errorBeep != value)
                {
                    _errorBeep = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorBeep)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether warning messages should trigger an audible beep.
        /// </summary>
        /// <value><c>true</c> if warning messages should beep; otherwise, <c>false</c>.</value>
        public bool WarningBeep
        {
            get { return _warningBeep; }
            set
            {
                if (_warningBeep != value)
                {
                    _warningBeep = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarningBeep)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether normal messages should trigger an audible beep.
        /// </summary>
        /// <value><c>true</c> if messages should beep; otherwise, <c>false</c>.</value>
        public bool MessageBeep
        {
            get { return _messageBeep; }
            set
            {
                if (_messageBeep != value)
                {
                    _messageBeep = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageBeep)));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether event messages should trigger an audible beep.
        /// </summary>
        /// <value><c>true</c> if event messages should beep; otherwise, <c>false</c>.</value>
        public bool EventBeep
        {
            get { return _eventBeep; }
            set
            {
                if (_eventBeep != value)
                {
                    _eventBeep = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EventBeep)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the color used to display error messages.
        /// </summary>
        /// <value>A <see cref="SolidColorBrush"/> for error message display. Default is #E41400.</value>
        public SolidColorBrush ErrorColor
        {
            get { return _errorColor; }
            set
            {
                if (value == null) return;
                if (_errorColor == null || _errorColor.Color != value.Color || _errorColor.Opacity != value.Opacity)
                {
                    _errorColor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorColor)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the color used to display warning messages.
        /// </summary>
        /// <value>A <see cref="SolidColorBrush"/> for warning message display. Default is golden amber (#E5A000).</value>
        public SolidColorBrush WarningColor
        {
            get { return _warningColor; }
            set
            {
                if (value == null) return;
                if (_warningColor == null || _warningColor.Color != value.Color || _warningColor.Opacity != value.Opacity)
                {
                    _warningColor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(WarningColor)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the color used to display normal messages.
        /// </summary>
        /// <value>A <see cref="SolidColorBrush"/> for message display. Default is sky blue (#1AA1E2).</value>
        public SolidColorBrush MessageColor
        {
            get { return _messageColor; }
            set
            {
                if (value == null) return;
                if (_messageColor == null || _messageColor.Color != value.Color || _messageColor.Opacity != value.Opacity)
                {
                    _messageColor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MessageColor)));
                }
            }
        }

        /// <summary>
        /// Gets or sets the color used to display event messages.
        /// </summary>
        /// <value>A <see cref="SolidColorBrush"/> for event message display. Default is dark turquoise (#00CED1).</value>
        public SolidColorBrush EventColor
        {
            get { return _eventColor; }
            set
            {
                if (value == null) return;
                if (_eventColor == null || _eventColor.Color != value.Color || _eventColor.Opacity != value.Opacity)
                {
                    _eventColor = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EventColor)));
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a message item to the messenger.
        /// </summary>
        /// <param name="item">The message item to add.</param>
        /// <remarks>
        /// <para>
        /// For event messages, the code is automatically made unique by appending a counter if necessary.
        /// For other message types, duplicate messages (same source and code) are ignored.
        /// </para>
        /// <para>
        /// <b>Warning:</b> For event messages, this method may modify the <paramref name="item"/>'s
        /// <see cref="IMessageItem.Code"/> property to ensure uniqueness. If you need to preserve
        /// the original code value, make a copy before calling this method.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="item"/> is null.</exception>
        public void Add(IMessageItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            IMessageItem[]? toNotify = null;
            string? deferredCode = null;

            lock (_lockObject)
            {
                if (AddItemInternal(item, out deferredCode))
                {
                    toNotify = new IMessageItem[] { item };
                }
            }

            // Mutate item.Code outside the lock — the property setter raises PropertyChanged
            // synchronously, which can re-enter the Messenger from a UI-thread subscriber.
            // Holding _lockObject during that re-entry would deadlock.
            if (deferredCode != null)
            {
                item.Code = deferredCode;
            }

            // Raise event outside the lock to prevent deadlock from re-entrant subscribers
            if (toNotify != null)
            {
                MessagesAdded?.Invoke(toNotify);
            }
        }

        /// <summary>
        /// Adds a collection of message items to the messenger.
        /// </summary>
        /// <param name="items">The collection of message items to add.</param>
        /// <remarks>
        /// For event messages, the code is automatically made unique by appending a counter if necessary.
        /// For other message types, duplicate messages (same source and code) are ignored.
        /// All successfully added messages trigger a single <see cref="MessagesAdded"/> event.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="items"/> is null.</exception>
        public void Add(IEnumerable<IMessageItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            IMessageItem[]? toNotify = null;
            // Deferred Code mutations to be applied OUTSIDE the lock; see comment in Add(IMessageItem).
            List<(IMessageItem item, string newCode)>? deferredCodeMutations = null;

            lock (_lockObject)
            {
                var newMessages = new List<IMessageItem>();
                foreach (IMessageItem item in items)
                {
                    if (item == null) continue;
                    if (AddItemInternal(item, out var newCode))
                    {
                        newMessages.Add(item);
                        if (newCode != null)
                        {
                            deferredCodeMutations ??= new List<(IMessageItem, string)>();
                            deferredCodeMutations.Add((item, newCode));
                        }
                    }
                }

                if (newMessages.Count > 0)
                {
                    toNotify = newMessages.ToArray();
                }
            }

            // Apply deferred Code mutations outside the lock (avoid re-entrant deadlock).
            if (deferredCodeMutations != null)
            {
                foreach (var (item, newCode) in deferredCodeMutations)
                {
                    item.Code = newCode;
                }
            }

            // Raise event outside the lock to prevent deadlock from re-entrant subscribers
            if (toNotify != null)
            {
                MessagesAdded?.Invoke(toNotify);
            }
        }

        /// <summary>
        /// Removes a message from the messenger.
        /// </summary>
        /// <param name="message">The message item to remove.</param>
        /// <returns><c>true</c> if the message was successfully removed; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is null.</exception>
        public bool Remove(IMessageItem message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Source == null) return false;

            bool removed;
            IMessageItem[]? toNotify = null;

            lock (_lockObject)
            {
                if (!_messagesBySource.ContainsKey(message.Source)) { return false; }
                if (!_messagesBySource[message.Source].ContainsKey(message.Code)) { return false; }

                removed = _messagesBySource[message.Source].Remove(message.Code);
                if (removed)
                {
                    toNotify = new IMessageItem[] { message };
                }
            }

            // Raise event outside the lock to prevent deadlock from re-entrant subscribers
            if (toNotify != null)
            {
                MessagesRemoved?.Invoke(toNotify);
            }
            return removed;
        }

        /// <summary>
        /// Removes all messages from the messenger.
        /// </summary>
        public void Clear()
        {
            IMessageItem[]? toNotify = null;

            lock (_lockObject)
            {
                var allMessages = new List<IMessageItem>();
                foreach (var source in _messagesBySource)
                {
                    allMessages.AddRange(source.Value.Values);
                }
                _messagesBySource.Clear();
                if (allMessages.Count > 0)
                {
                    toNotify = allMessages.ToArray();
                }
            }

            // Raise event outside the lock to prevent deadlock from re-entrant subscribers
            if (toNotify != null)
            {
                MessagesRemoved?.Invoke(toNotify);
            }
        }

        /// <summary>
        /// Removes all messages from a specific source.
        /// </summary>
        /// <param name="source">The source object whose messages should be cleared.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="source"/> is null.</exception>
        public void Clear(object source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            IMessageItem[]? toNotify = null;

            lock (_lockObject)
            {
                if (!_messagesBySource.ContainsKey(source)) { return; }

                toNotify = _messagesBySource[source].Values.ToArray();
                _messagesBySource[source].Clear();
            }

            // Raise event outside the lock to prevent deadlock from re-entrant subscribers
            if (toNotify != null && toNotify.Length > 0)
            {
                MessagesRemoved?.Invoke(toNotify);
            }
        }

        /// <summary>
        /// Adds a single item to the internal storage. Must be called while holding <c>_lockObject</c>.
        /// Handles event code uniquification for <see cref="MessageType.Event"/> items.
        /// </summary>
        /// <param name="item">The message item to add.</param>
        /// <param name="deferredCode">When the item is an <see cref="MessageType.Event"/> whose code had to be
        /// changed for uniqueness, this receives the new value to be assigned to <c>item.Code</c> AFTER the
        /// caller releases <c>_lockObject</c>. The Code setter raises <c>PropertyChanged</c> synchronously, which
        /// can re-enter Messenger from a UI subscriber — that re-entry must not happen while the lock is held.
        /// Receives <c>null</c> when no Code change is needed.</param>
        /// <returns><c>true</c> if the item was added; <c>false</c> if it was a duplicate or had no source.</returns>
        private bool AddItemInternal(IMessageItem item, out string? deferredCode)
        {
            deferredCode = null;
            if (item.Source == null) return false;

            if (item.Type == MessageType.Event)
            {
                if (!_messagesBySource.ContainsKey(item.Source))
                {
                    _messagesBySource.Add(item.Source, new Dictionary<string, IMessageItem>());
                }

                // Ensure that the event code is unique by appending counter if needed.
                var srcMsgs = _messagesBySource[item.Source];
                string originalCode = item.Code;
                string code = originalCode;
                int counter = 1;

                while (srcMsgs.ContainsKey(code))
                {
                    code = $"{originalCode}{counter}";
                    counter++;
                }

                // Store under the unique code without mutating item.Code (the setter would synchronously
                // fire PropertyChanged → potentially re-enter Messenger via a UI subscriber while we still
                // hold _lockObject). The mutation is deferred to the caller post-lock release.
                srcMsgs.Add(code, item);
                if (!ReferenceEquals(code, originalCode) && code != originalCode)
                {
                    deferredCode = code;
                }
                return true;
            }
            else
            {
                // For non-event messages, ignore duplicates
                if (_messagesBySource.ContainsKey(item.Source) && _messagesBySource[item.Source].ContainsKey(item.Code))
                {
                    return false;
                }

                if (!_messagesBySource.ContainsKey(item.Source))
                {
                    _messagesBySource.Add(item.Source, new Dictionary<string, IMessageItem>());
                }

                _messagesBySource[item.Source].Add(item.Code, item);
                return true;
            }
        }

        /// <summary>
        /// Gets all message items currently stored in the messenger.
        /// </summary>
        /// <returns>A list of all message items from all sources.</returns>
        public List<IMessageItem> AllMessageItems()
        {
            lock (_lockObject)
            {
                var allMessages = new List<IMessageItem>();
                foreach (var source in _messagesBySource)
                {
                    allMessages.AddRange(source.Value.Values);
                }
                return allMessages;
            }
        }

        /// <summary>
        /// Exports all messages to a text file.
        /// </summary>
        /// <param name="fileName">The full path of the file to export to.</param>
        /// <remarks>
        /// If the file already exists, it will be overwritten with the current messages.
        /// The directory will be created if it does not exist.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="fileName"/> is null or empty.</exception>
        /// <exception cref="IOException">Thrown when an I/O error occurs during file operations.</exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the caller does not have the required permission to access the file or directory.
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown when the specified path exceeds the system-defined maximum length.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown when the specified path is invalid (for example, it is on an unmapped drive).
        /// </exception>
        public void ExportToTextFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentNullException(nameof(fileName));

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Use using statement for proper resource disposal
            using (var writer = new StreamWriter(fileName, false))
            {
                foreach (IMessageItem message in AllMessageItems())
                {
                    writer.WriteLine(message.ToText());
                }
            }
        }

        #endregion
    }
}
