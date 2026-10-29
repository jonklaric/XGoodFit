using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GenericControls
{
    /// <summary>
    /// Internal window used by <see cref="MessageBox"/> to display themed message box dialogs.
    /// Inherits from <see cref="MetroDialogWindow"/> to use the MetroDialogStyle chrome.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This window is not intended to be used directly. Use the static <see cref="MessageBox.Show(string)"/>
    /// methods instead, which create and configure this window internally.
    /// </para>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class MessageBoxWindow : MetroDialogWindow
    {
        /// <summary>
        /// Identifies the <see cref="MessageBoxWidth"/> dependency property.
        /// The default value is 420, which approximates the standard Windows MessageBox width.
        /// </summary>
        public static readonly DependencyProperty MessageBoxWidthProperty =
            DependencyProperty.Register(nameof(MessageBoxWidth), typeof(double), typeof(MessageBoxWindow),
                new FrameworkPropertyMetadata(420.0, OnMessageBoxWidthChanged));

        /// <summary>
        /// Gets or sets the width of the message box window.
        /// The default value is 420 device-independent pixels.
        /// </summary>
        public double MessageBoxWidth
        {
            get => (double)GetValue(MessageBoxWidthProperty);
            set => SetValue(MessageBoxWidthProperty, value);
        }

        /// <summary>
        /// Handles changes to the <see cref="MessageBoxWidth"/> property by updating the window width.
        /// </summary>
        private static void OnMessageBoxWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MessageBoxWindow window)
            {
                window.Width = (double)e.NewValue;
            }
        }

        /// <summary>
        /// Gets the result of the message box dialog after it is closed.
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        /// <summary>
        /// Gets a value indicating whether the user checked the "Don't show this message again" checkbox.
        /// </summary>
        public bool SuppressChecked => SuppressCheckBox.IsChecked == true;

        /// <summary>
        /// The button configuration used for this message box instance.
        /// Used to determine close behavior when the X button is clicked.
        /// </summary>
        private MessageBoxButton _buttonConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageBoxWindow"/> class.
        /// </summary>
        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Configures the message box window with the specified parameters.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default button result.</param>
        /// <param name="showSuppressCheckBox">Whether to show the "Don't show this again" checkbox.</param>
        public void Configure(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon, MessageBoxResult defaultResult, bool showSuppressCheckBox = false)
        {
            Title = caption ?? string.Empty;
            MessageText.Text = messageBoxText ?? string.Empty;
            _buttonConfig = button;

            ConfigureIcon(icon);
            ConfigureButtons(button, defaultResult);

            if (showSuppressCheckBox)
            {
                SuppressCheckBox.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Configures the icon display based on the specified <see cref="MessageBoxImage"/> value.
        /// </summary>
        /// <param name="icon">The icon type to display.</param>
        private void ConfigureIcon(MessageBoxImage icon)
        {
            // MessageBoxImage enum values:
            // None = 0, Error/Stop/Hand = 16, Question = 32, Warning/Exclamation = 48, Information/Asterisk = 64
            string resourceKey = icon switch
            {
                MessageBoxImage.Error => "MessageBoxErrorIcon",          // Also handles Stop (16) and Hand (16)
                MessageBoxImage.Question => "MessageBoxQuestionIcon",    // 32
                MessageBoxImage.Warning => "MessageBoxWarningIcon",      // Also handles Exclamation (48)
                MessageBoxImage.Information => "MessageBoxInfoIcon",     // Also handles Asterisk (64)
                _ => null
            };

            if (resourceKey != null)
            {
                var iconResource = TryFindResource(resourceKey);
                if (iconResource is ImageSource imageSource)
                {
                    IconImage.Source = imageSource;
                    IconImage.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Configures the button panel based on the specified <see cref="MessageBoxButton"/> value.
        /// </summary>
        /// <param name="buttonConfig">The button combination to display.</param>
        /// <param name="defaultResult">The default button that receives initial focus.</param>
        private void ConfigureButtons(MessageBoxButton buttonConfig, MessageBoxResult defaultResult)
        {
            ButtonPanel.Children.Clear();

            switch (buttonConfig)
            {
                case MessageBoxButton.OK:
                    AddButton("OK", MessageBoxResult.OK, isDefault: true, isCancel: true);
                    break;

                case MessageBoxButton.OKCancel:
                    AddButton("OK", MessageBoxResult.OK,
                        isDefault: defaultResult != MessageBoxResult.Cancel);
                    AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true,
                        isDefault: defaultResult == MessageBoxResult.Cancel);
                    break;

                case MessageBoxButton.YesNo:
                    AddButton("Yes", MessageBoxResult.Yes,
                        isDefault: defaultResult != MessageBoxResult.No);
                    AddButton("No", MessageBoxResult.No, isCancel: true,
                        isDefault: defaultResult == MessageBoxResult.No);
                    break;

                case MessageBoxButton.YesNoCancel:
                    AddButton("Yes", MessageBoxResult.Yes,
                        isDefault: defaultResult == MessageBoxResult.Yes
                            || defaultResult == MessageBoxResult.None);
                    AddButton("No", MessageBoxResult.No,
                        isDefault: defaultResult == MessageBoxResult.No);
                    AddButton("Cancel", MessageBoxResult.Cancel, isCancel: true,
                        isDefault: defaultResult == MessageBoxResult.Cancel);
                    break;
            }

            // Set 6px right margin on the last button so it sits flush with the 6px window border
            if (ButtonPanel.Children.Count > 0 && ButtonPanel.Children[ButtonPanel.Children.Count - 1] is Button lastBtn)
                lastBtn.Margin = new Thickness(6, 0, 6, 6);
        }

        /// <summary>
        /// Adds a button to the button panel with the specified configuration.
        /// </summary>
        /// <param name="content">The button label text.</param>
        /// <param name="result">The <see cref="MessageBoxResult"/> to return when this button is clicked.</param>
        /// <param name="isDefault">Whether this button is activated by pressing Enter.</param>
        /// <param name="isCancel">Whether this button is activated by pressing Escape.</param>
        private void AddButton(string content, MessageBoxResult result,
            bool isDefault = false, bool isCancel = false)
        {
            var button = new Button
            {
                Content = content,
                Width = 75,
                Height = 24,
                Margin = new Thickness(6, 0, 0, 6),
                Cursor = Cursors.Hand,
                IsDefault = isDefault,
                IsCancel = isCancel
            };

            button.Click += (s, e) =>
            {
                Result = result;
                DialogResult = true;
            };

            ButtonPanel.Children.Add(button);
        }

        /// <summary>
        /// Handles the close window command (X button in title bar).
        /// For YesNo dialogs, the close button is ignored (matching native MessageBox behavior).
        /// For all other configurations, closing returns the cancel-equivalent result.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The event arguments.</param>
        protected override void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            switch (_buttonConfig)
            {
                case MessageBoxButton.YesNo:
                    // Native MessageBox disables X for YesNo; we just ignore the close request
                    return;

                case MessageBoxButton.OK:
                    Result = MessageBoxResult.OK;
                    break;

                case MessageBoxButton.OKCancel:
                case MessageBoxButton.YesNoCancel:
                    Result = MessageBoxResult.Cancel;
                    break;
            }

            base.OnCloseWindow(sender, e);
        }
    }
}
