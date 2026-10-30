using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using FrameworkInterfaces;
using WpfCompat;
using WpfCompat.Input;

namespace FrameworkUI.MessageWindow
{
    /// <summary>
    /// Interaction logic for MessageWindowControl.xaml providing a filterable message display window.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// <para>
    /// XGoodFit port:
    /// <list type="bullet">
    /// <item><description>The style properties take ControlThemes, and the colours are styled
    /// properties.</description></item>
    /// <item><description>WPF applies the control's implicit style (MessageWindowStyle) when it's
    /// created, so the constructor's calls saw the style's values. Avalonia applies a theme when the
    /// control joins a window, so the header and Clear All Filters styles are applied again when the
    /// theme sets them (<see cref="OnPropertyChanged"/>).</description></item>
    /// <item><description>Setting a button's style replaced its binding to the window's style in WPF;
    /// Avalonia keeps a binding when a value is set, so <see cref="SetStyle"/> removes it
    /// first.</description></item>
    /// <item><description>The grid's double click is handled even when the grid has handled the press,
    /// as WPF raises a control's MouseDoubleClick; the grid's context menus, which upstream's plain
    /// DataGrid didn't have, are suppressed. Its columns are reached by index.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class MessageWindowControl : UserControl
    {
        /// <summary>
        /// Construct new MessageWindowControl.
        /// </summary>
        public MessageWindowControl()
        {
            InitializeComponent();
            // XGoodFit port: the grid's MouseDoubleClick and LostFocus from upstream's XAML (WpfCompat's
            // MouseDoubleClick also sees the presses the grid handles, as WPF's does).
            // CopyPasteDataGrid's context menus are suppressed: upstream's plain DataGrid had none.
            MyDataGrid.AddMouseDoubleClickHandler(MyDataGrid_MouseDoubleClick);
            MyDataGrid.LostFocus += MyDataGrid_LostFocus;
            PlainDataGrid.SuppressContextMenus(MyDataGrid);
            // Add any initialization after the InitializeComponent() call.
            FrameworkInterfaces.Messaging.Messenger.GetInstance().MessagesAdded += MessageWindowControl_MessagesAdded;
            FrameworkInterfaces.Messaging.Messenger.GetInstance().MessagesRemoved += MessageWindowControl_MessagesRemoved;
            MessageWindowControl_MessagesAdded(FrameworkInterfaces.Messaging.Messenger.GetInstance().AllMessageItems().ToArray());
            MyDataGrid.ItemsSource = _filteredMessages;
            UpdateButtonText();
            UpdateClearAllFilterButtonStyle();
            SetColumnHeaderStyles();
            Loaded += MessageWindowControl_Loaded;
            Unloaded += MessageWindowControl_Unloaded;
        }

        /// <summary>
        /// Handles the Loaded event to re-subscribe to messenger events and sync any messages
        /// that arrived while the control was unloaded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MessageWindowControl_Loaded(object? sender, RoutedEventArgs e)
        {
            var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();

            // Unsubscribe first to prevent double-subscription (constructor already subscribes once)
            messenger.MessagesAdded -= MessageWindowControl_MessagesAdded;
            messenger.MessagesRemoved -= MessageWindowControl_MessagesRemoved;

            // Re-subscribe
            messenger.MessagesAdded += MessageWindowControl_MessagesAdded;
            messenger.MessagesRemoved += MessageWindowControl_MessagesRemoved;

            // Rebuild from current messenger state to pick up messages that arrived while unloaded
            _messages.Clear();
            _filteredMessages.Clear();
            _errorItemCount = 0;
            _warningItemCount = 0;
            _messageItemCount = 0;
            _eventItemCount = 0;
            MessageWindowControl_MessagesAdded(messenger.AllMessageItems().ToArray());
            UpdateButtonText();
        }

        /// <summary>
        /// Handles the Unloaded event to detach event handlers and prevent memory leaks.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MessageWindowControl_Unloaded(object? sender, RoutedEventArgs e)
        {
            FrameworkInterfaces.Messaging.Messenger.GetInstance().MessagesAdded -= MessageWindowControl_MessagesAdded;
            FrameworkInterfaces.Messaging.Messenger.GetInstance().MessagesRemoved -= MessageWindowControl_MessagesRemoved;
        }

        /// <summary>
        /// Dependency property for the message window background color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> BackgroundColorProperty = AvaloniaProperty.Register<MessageWindowControl, SolidColorBrush>(nameof(BackgroundColor), new SolidColorBrush(Colors.White));

        /// <summary>
        /// Gets or sets the message window background color.
        /// </summary>
        public SolidColorBrush BackgroundColor
        {
            get { return (SolidColorBrush)this.GetValue(BackgroundColorProperty); }
            set { this.SetValue(BackgroundColorProperty, value); }
        }

        /// <summary>
        /// Dependency property for the stack panel background color.
        /// </summary>
        public static readonly StyledProperty<SolidColorBrush> StackPanelBackgroundColorProperty = AvaloniaProperty.Register<MessageWindowControl, SolidColorBrush>(nameof(StackPanelBackgroundColor), new SolidColorBrush(Colors.White));

        /// <summary>
        /// Gets or sets the stack panel background color.
        /// </summary>
        public SolidColorBrush StackPanelBackgroundColor
        {
            get { return (SolidColorBrush)this.GetValue(StackPanelBackgroundColorProperty); }
            set { this.SetValue(StackPanelBackgroundColorProperty, value); }
        }

        /// <summary>
        /// Dependency property for the stack panel separator style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> StackPanelSeparatorStyleProperty = AvaloniaProperty.Register<MessageWindowControl, ControlTheme>(nameof(StackPanelSeparatorStyle), null!);

        /// <summary>
        /// Gets or sets the stack panel separator style.
        /// </summary>
        public ControlTheme StackPanelSeparatorStyle
        {
            get { return (ControlTheme)this.GetValue(StackPanelSeparatorStyleProperty); }
            set { this.SetValue(StackPanelSeparatorStyleProperty, value); }
        }

        /// <summary>
        /// Dependency property for the stack panel button style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> StackPanelButtonStyleProperty = AvaloniaProperty.Register<MessageWindowControl, ControlTheme>(nameof(StackPanelButtonStyle), null!);

        /// <summary>
        /// Gets or sets the stack panel button style.
        /// </summary>
        public ControlTheme StackPanelButtonStyle
        {
            get { return (ControlTheme)this.GetValue(StackPanelButtonStyleProperty); }
            set { this.SetValue(StackPanelButtonStyleProperty, value); }
        }

        /// <summary>
        /// Dependency property for the stack panel pressed button style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> StackPanelPressedButtonStyleProperty = AvaloniaProperty.Register<MessageWindowControl, ControlTheme>(nameof(StackPanelPressedButtonStyle), null!);

        /// <summary>
        /// Gets or sets the stack panel pressed button style.
        /// </summary>
        public ControlTheme StackPanelPressedButtonStyle
        {
            get { return (ControlTheme)this.GetValue(StackPanelPressedButtonStyleProperty); }
            set { this.SetValue(StackPanelPressedButtonStyleProperty, value); }
        }

        /// <summary>
        /// Dependency property for the center header column style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> CenterColumnHeaderStyleProperty = AvaloniaProperty.Register<MessageWindowControl, ControlTheme>(nameof(CenterColumnHeaderStyle), null!);

        /// <summary>
        /// Gets or sets the center header column style.
        /// </summary>
        public ControlTheme CenterColumnHeaderStyle
        {
            get { return (ControlTheme)this.GetValue(CenterColumnHeaderStyleProperty); }
            set { this.SetValue(CenterColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        /// Dependency property for the left column header style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> LeftColumnHeaderStyleProperty = AvaloniaProperty.Register<MessageWindowControl, ControlTheme>(nameof(LeftColumnHeaderStyle), null!);

        /// <summary>
        /// Gets or sets the left column header style.
        /// </summary>
        public ControlTheme LeftColumnHeaderStyle
        {
            get { return (ControlTheme)this.GetValue(LeftColumnHeaderStyleProperty); }
            set { this.SetValue(LeftColumnHeaderStyleProperty, value); }
        }

        /// <summary>
        /// Applies a style the theme sets, as WPF's constructor saw it (see the class remarks).
        /// </summary>
        /// <param name="change">The property change.</param>
        /// <remarks>XGoodFit addition.</remarks>
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CenterColumnHeaderStyleProperty || change.Property == LeftColumnHeaderStyleProperty)
            {
                SetColumnHeaderStyles();
            }
            else if (change.Property == StackPanelButtonStyleProperty || change.Property == StackPanelPressedButtonStyleProperty)
            {
                UpdateClearAllFilterButtonStyle();
            }
        }

        /// <summary>
        /// Sets a button's style, replacing its binding to one of the window's styles as WPF's assignment
        /// did.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="style">The style.</param>
        /// <remarks>XGoodFit addition: Avalonia keeps a binding when a value is set.</remarks>
        private static void SetStyle(Button button, ControlTheme style)
        {
            BindingOperations.GetBindingExpressionBase(button, ThemeProperty)?.Dispose();
            button.Theme = style;
        }

        // XGoodFit port: Avalonia's grid columns can't be named in XAML; these are upstream's names.
        private DataGridColumn IconColumn => MyDataGrid.Columns[0];
        private DataGridColumn TimeStampColumn => MyDataGrid.Columns[1];
        private DataGridColumn DescriptionColumn => MyDataGrid.Columns[2];
        private DataGridColumn SourceColumn => MyDataGrid.Columns[3];
        private DataGridColumn NameColumn => MyDataGrid.Columns[4];
        private DataGridColumn ParameterColumn => MyDataGrid.Columns[5];

        private List<IMessageItem> _messages = new List<IMessageItem>();
        private List<MessageItem> _filteredMessages = new List<MessageItem>();
        private int _errorItemCount = 0;
        private int _warningItemCount = 0;
        private int _eventItemCount = 0;
        private int _messageItemCount = 0;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showMessages = false;
        private bool _showEvents = false;

        /// <summary>
        /// Handles the removal of messages from the message window.
        /// Dispatches to the UI thread if called from a background thread.
        /// </summary>
        /// <param name="oldMessages">The messages that were removed.</param>
        private void MessageWindowControl_MessagesRemoved(IMessageItem[] oldMessages)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => MessageWindowControl_MessagesRemoved(oldMessages));
                return;
            }

            foreach (IMessageItem oldMessage in oldMessages)
            {
                DecrementCountAndRemoveFromFiltered(oldMessage);
                _messages.Remove(oldMessage);
            }
            UpdateButtonText();
            MyDataGrid.Items.Refresh();
        }

        /// <summary>
        /// Decrements the appropriate message type counter and removes matching messages from the filtered list if visible.
        /// </summary>
        /// <param name="message">The message to process.</param>
        private void DecrementCountAndRemoveFromFiltered(IMessageItem message)
        {
            bool shouldRemoveFromFiltered;
            switch (message.Type)
            {
                case MessageType.Error:
                    _errorItemCount -= 1;
                    shouldRemoveFromFiltered = _showErrors;
                    break;
                case MessageType.Warning:
                    _warningItemCount -= 1;
                    shouldRemoveFromFiltered = _showWarnings;
                    break;
                case MessageType.Message:
                    _messageItemCount -= 1;
                    shouldRemoveFromFiltered = _showMessages;
                    break;
                case MessageType.Event:
                    _eventItemCount -= 1;
                    shouldRemoveFromFiltered = _showEvents;
                    break;
                default:
                    return;
            }

            if (shouldRemoveFromFiltered)
            {
                for (int i = _filteredMessages.Count - 1; i >= 0; i--)
                {
                    if (_filteredMessages[i].Message.Code == message.Code &&
                        _filteredMessages[i].Message.Source == message.Source)
                    {
                        _filteredMessages.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the addition of new messages to the message window.
        /// Dispatches to the UI thread if called from a background thread.
        /// </summary>
        /// <param name="newMessages">The messages that were added.</param>
        private void MessageWindowControl_MessagesAdded(IMessageItem[] newMessages)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => MessageWindowControl_MessagesAdded(newMessages));
                return;
            }

            foreach (IMessageItem newMessage in newMessages)
            {
                if (newMessage.Type == MessageType.Error)
                {
                    _errorItemCount += 1;
                    if (_showErrors) { _filteredMessages.Add(new MessageItem(newMessage)); }
                }
                else if (newMessage.Type == MessageType.Warning)
                {
                    _warningItemCount += 1;
                    if (_showWarnings) { _filteredMessages.Add(new MessageItem(newMessage)); }
                }
                else if (newMessage.Type == MessageType.Message)
                {
                    _messageItemCount += 1;
                    if (_showMessages) { _filteredMessages.Add(new MessageItem(newMessage)); }
                }
                else if (newMessage.Type == MessageType.Event)
                {
                    _eventItemCount += 1;
                    if (_showEvents) { _filteredMessages.Add(new MessageItem(newMessage)); }
                }
                //
                _messages.Add(newMessage);
            }
            MyDataGrid.Items.Refresh();
            UpdateButtonText();
        }

        /// <summary>
        /// Update the button tool tip and text.
        /// </summary>
        private void UpdateButtonText()
        {
            if (_showErrors == true)
            {
                ErrorsButtonText.Text = $" {_errorItemCount} Errors";
                ErrorsButton.ToolTip = "Hide Errors";
            }
            else
            {
                ErrorsButtonText.Text = " 0 of " + _errorItemCount + " Errors";
                ErrorsButton.ToolTip = "Show Errors";
            }
            if (_showWarnings == true)
            {
                WarningsButtonText.Text = " " + _warningItemCount + " Warnings";
                WarningsButton.ToolTip = "Hide Warnings";
            }
            else
            {
                WarningsButtonText.Text = " 0 of " + _warningItemCount + " Warnings";
                WarningsButton.ToolTip = "Show Warnings";
            }
            if (_showMessages == true)
            {
                MessageButtonText.Text = " " + _messageItemCount + " Messages";
                MessagesButton.ToolTip = "Hide Messages";
            }
            else
            {
                MessageButtonText.Text = " 0 of " + _messageItemCount + " Messages";
                MessagesButton.ToolTip = "Show Messages";
            }
            if (_showEvents == true)
            {
                EventsButtonText.Text = _eventItemCount + " Events";
                EventsButton.ToolTip = "Hide Log Events";
            }
            else
            {
                EventsButtonText.Text = "0 of " + _eventItemCount + " Events";
                EventsButton.ToolTip = "Show Log Events";
            }
        }

        /// <summary>
        /// Update the clear all filter button style.
        /// </summary>
        private void UpdateClearAllFilterButtonStyle()
        {
            if (_showErrors == false || _showWarnings == false || _showMessages == false || _showEvents == false)
            {
                ClearFiltersButton.IsEnabled = true;
                SetStyle(ClearFiltersButton, StackPanelPressedButtonStyle);
                return;
            }
            ClearFiltersButton.IsEnabled = false;
            SetStyle(ClearFiltersButton, StackPanelButtonStyle);
        }

        /// <summary>
        /// Refreshes the filtered items based on current filter settings.
        /// </summary>
        private void RefreshFilteredItems()
        {
            _filteredMessages.Clear();
            foreach (IMessageItem msg in _messages)
            {
                if (msg.Type == MessageType.Error && _showErrors) { _filteredMessages.Add(new MessageItem(msg)); }
                if (msg.Type == MessageType.Warning && _showWarnings) { _filteredMessages.Add(new MessageItem(msg)); }
                if (msg.Type == MessageType.Message && _showMessages) { _filteredMessages.Add(new MessageItem(msg)); }
                if (msg.Type == MessageType.Event && _showEvents) { _filteredMessages.Add(new MessageItem(msg)); }
            }
            //
            MyDataGrid.Items.Refresh();
        }

        /// <summary>
        /// On click, update message window filter.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ErrorsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showErrors == true)
            {
                _showErrors = false;
                SetStyle(ErrorsButton, StackPanelButtonStyle);
                for (int i = _filteredMessages.Count() - 1; i >= 0; i--)
                {
                    if (_filteredMessages[i].Message.Type == MessageType.Error) { _filteredMessages.RemoveAt(i); }
                }
                //
                MyDataGrid.Items.Refresh();
            }
            else
            {
                _showErrors = true;
                SetStyle(ErrorsButton, StackPanelPressedButtonStyle);
                //
                RefreshFilteredItems();
            }
            //
            UpdateClearAllFilterButtonStyle();
            MyDataGrid.Focus();
        }

        /// <summary>
        /// On click, update message window filter.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void WarningsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showWarnings == true)
            {
                _showWarnings = false;
                SetStyle(WarningsButton, StackPanelButtonStyle);
                for (int i = _filteredMessages.Count() - 1; i >= 0; i--)
                {
                    if (_filteredMessages[i].Message.Type == MessageType.Warning) { _filteredMessages.RemoveAt(i); }
                }
                //
                MyDataGrid.Items.Refresh();
            }
            else
            {
                _showWarnings = true;
                SetStyle(WarningsButton, StackPanelPressedButtonStyle);
                //
                RefreshFilteredItems();
            }
            UpdateClearAllFilterButtonStyle();
            MyDataGrid.Focus();
        }

        /// <summary>
        /// On click, update message window filter.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MessagesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showMessages == true)
            {
                _showMessages = false;
                SetStyle(MessagesButton, StackPanelButtonStyle);
                for (int i = _filteredMessages.Count() - 1; i >= 0; i--)
                {
                    if (_filteredMessages[i].Message.Type == MessageType.Message) { _filteredMessages.RemoveAt(i); }
                }
                //
                MyDataGrid.Items.Refresh();
            }
            else
            {
                _showMessages = true;
                SetStyle(MessagesButton, StackPanelPressedButtonStyle);
                //
                RefreshFilteredItems();
            }
            UpdateClearAllFilterButtonStyle();
            MyDataGrid.Focus();
        }

        /// <summary>
        /// On click, update message window filter.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void EventsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_showEvents == true)
            {
                _showEvents = false;
                SetStyle(EventsButton, StackPanelButtonStyle);
                for (int i = _filteredMessages.Count() - 1; i >= 0; i--)
                {
                    if (_filteredMessages[i].Message.Type == MessageType.Event) { _filteredMessages.RemoveAt(i); }
                }
                //
                MyDataGrid.Items.Refresh();
            }
            else
            {
                _showEvents = true;
                SetStyle(EventsButton, StackPanelPressedButtonStyle);
                //
                RefreshFilteredItems();
            }
            UpdateClearAllFilterButtonStyle();
            MyDataGrid.Focus();
        }

        /// <summary>
        /// Clear all message window filters.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            _showErrors = true;
            _showWarnings = true;
            _showMessages = true;
            _showEvents = true;
            RefreshFilteredItems();
            // Update styles
            SetStyle(ErrorsButton, StackPanelPressedButtonStyle);
            SetStyle(WarningsButton, StackPanelPressedButtonStyle);
            SetStyle(MessagesButton, StackPanelPressedButtonStyle);
            SetStyle(EventsButton, StackPanelPressedButtonStyle);

            UpdateClearAllFilterButtonStyle();
            MyDataGrid.Focus();
        }

        /// <summary>
        /// Export message window to text file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ExportToTextFileButton_Click(object sender, RoutedEventArgs e)
        {
            // open save dialog form
            var SaveFileDialog = new SaveFileDialog()
            {
                Title = "Export Message Window to Text File",
                Filter = "Text Files (*.txt)|*.txt"
            };
            if (SaveFileDialog.ShowDialog() == true)
            {
                FrameworkInterfaces.Messaging.Messenger.GetInstance().ExportToTextFile(SaveFileDialog.FileName);
            }
            MyDataGrid.Focus();
        }

        /// <summary>
        /// Clear data grid selection when focus is lost.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MyDataGrid_LostFocus(object? sender, RoutedEventArgs e)
        {
            // XGoodFit port: WPF's UnselectAll; the grid selects whole rows.
            MyDataGrid.SelectedItems.Clear();
        }

        /// <summary>
        /// On double click, execute action for message window item click.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MyDataGrid_MouseDoubleClick(object? sender, PointerPressedEventArgs e)
        {
            if (MyDataGrid.SelectedItems == null || MyDataGrid.SelectedItems.Count != 1) return;
            MessageItem messageItem = (MessageItem)MyDataGrid.SelectedItem!;
            if (messageItem.Message.MessageAction != null) messageItem.Message.MessageAction.Invoke(messageItem.Message);
        }

        /// <summary>
        /// Set the data grid column header styles.
        /// </summary>
        private void SetColumnHeaderStyles()
        {
            // XGoodFit port: a column's HeaderStyle is its HeaderTheme.
            IconColumn.HeaderTheme = CenterColumnHeaderStyle;
            TimeStampColumn.HeaderTheme = CenterColumnHeaderStyle;
            DescriptionColumn.HeaderTheme = LeftColumnHeaderStyle;
            SourceColumn.HeaderTheme = LeftColumnHeaderStyle;
            NameColumn.HeaderTheme = LeftColumnHeaderStyle;
            ParameterColumn.HeaderTheme = LeftColumnHeaderStyle;
        }


    }
}
