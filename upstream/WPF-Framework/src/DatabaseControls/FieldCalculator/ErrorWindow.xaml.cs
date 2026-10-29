using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using ExpressionParser;

namespace DatabaseControls
{
    /// <summary>
    /// A window that displays a list of errors encountered during expression parsing or evaluation.
    /// Implements <see cref="INotifyPropertyChanged"/> for data binding support.
    /// </summary>
    public partial class ErrorWindow : GenericControls.MetroDialogWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The backing field for the Errors property.
        /// </summary>
        private ObservableCollection<ErrorItem> _errors = new ObservableCollection<ErrorItem>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorWindow"/> class with a list of string error messages.
        /// </summary>
        /// <param name="errorList">The list of error messages to display.</param>
        /// <param name="title">The title to display in the window title bar.</param>
        public ErrorWindow(List<string> errorList, string title)
        {
            // This call is required by the designer.
            InitializeComponent();
            this.Title = title;

            // Add any initialization after the InitializeComponent() call.
            var tmp = new ObservableCollection<ErrorItem>();
            for (int i = 0; i < errorList.Count; i++)
            {
                tmp.Add(new ErrorItem(errorList[i]));
            }
            Errors = tmp;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorWindow"/> class with a list of parse errors.
        /// </summary>
        /// <param name="errorList">The list of parse errors to display.</param>
        /// <param name="title">The title to display in the window title bar.</param>
        public ErrorWindow(IList<ParseError> errorList, string title)
        {
            // This call is required by the designer.
            InitializeComponent();
            this.Title = title;

            // Add any initialization after the InitializeComponent() call.
            var tmp = new ObservableCollection<ErrorItem>();
            for (int i = 0; i < errorList.Count; i++)
            {
                tmp.Add(new ErrorItem(errorList[i].Description));
            }
            Errors = tmp;
        }

        /// <summary>
        /// Gets or sets the collection of error items to display in the data grid.
        /// </summary>
        public ObservableCollection<ErrorItem> Errors
        {
            get { return _errors; }
            set
            {
                _errors = value;
                NotifyPropertyChanged("Errors");
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

        /// <summary>
        /// Handles the click event for the Cancel/Close button.
        /// Closes the error window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
