using System.ComponentModel;

namespace DatabaseControls
{
    /// <summary>
    /// Represents a single error item for display in an error list.
    /// Implements <see cref="INotifyPropertyChanged"/> for data binding support.
    /// </summary>
    public class ErrorItem : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The backing field for the Message property.
        /// </summary>
        private string _message;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorItem"/> class with the specified error message.
        /// </summary>
        /// <param name="str">The error message to display.</param>
        public ErrorItem(string str)
        {
            _message = str;
        }

        /// <summary>
        /// Gets or sets the error message text.
        /// </summary>
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyPropertyChanged(nameof(Message));
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="info">The name of the property that changed.</param>
        private void NotifyPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }
}
