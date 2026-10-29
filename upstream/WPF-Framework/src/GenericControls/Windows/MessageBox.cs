using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GenericControls
{
    /// <summary>
    /// A theme-aware replacement for <see cref="System.Windows.MessageBox"/> that uses the framework's
    /// MetroDialogWindow chrome and theme colors. Supports all standard MessageBox overloads plus
    /// an optional "Don't show this again" checkbox with external suppression storage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides static <see cref="Show(string)"/> methods that mirror every overload of
    /// <see cref="System.Windows.MessageBox.Show(string)"/>. Migration from the native MessageBox
    /// is a simple replacement of <c>MessageBox.Show</c> with <c>GenericControls.MessageBox.Show</c>.
    /// </para>
    /// <para>
    /// The "Don't show again" feature uses an <see cref="IDictionary{TKey, TValue}"/> to store
    /// suppression state, keeping the MessageBox decoupled from any specific persistence mechanism.
    /// The calling application owns how and where the booleans are stored.
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
    public static class MessageBox
    {
        #region Standard Overloads (matching System.Windows.MessageBox)

        /// <summary>
        /// Displays a themed message box with the specified text.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(string messageBoxText)
        {
            return ShowCore(null, messageBoxText, string.Empty, MessageBoxButton.OK,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box with the specified text and caption.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(string messageBoxText, string caption)
        {
            return ShowCore(null, messageBoxText, caption, MessageBoxButton.OK,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box with the specified text, caption, and buttons.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button)
        {
            return ShowCore(null, messageBoxText, caption, button,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box with the specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box with the specified text, caption, buttons, icon, and default result.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default button result.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, defaultResult);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with the specified text.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText)
        {
            return ShowCore(owner, messageBoxText, string.Empty, MessageBoxButton.OK,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with the specified text and caption.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption)
        {
            return ShowCore(owner, messageBoxText, caption, MessageBoxButton.OK,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with the specified text, caption, and buttons.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button)
        {
            return ShowCore(owner, messageBoxText, caption, button,
                MessageBoxImage.None, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with the specified text, caption, buttons, and icon.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, MessageBoxResult.None);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with the specified text, caption, buttons, icon, and default result.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default button result.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, defaultResult);
        }

        #endregion

        #region Don't Show Again Overloads

        /// <summary>
        /// Displays a themed message box with a "Don't show this message again" checkbox.
        /// If the user has previously checked the box for the given settings key, the dialog
        /// is not shown and <paramref name="defaultWhenSuppressed"/> is returned immediately.
        /// </summary>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="suppressSettingsKey">A unique key identifying this message for suppression.</param>
        /// <param name="suppressionStore">
        /// A dictionary that stores suppression state. The calling application owns persistence
        /// of this dictionary (e.g., user settings file, registry, etc.).
        /// </param>
        /// <param name="defaultWhenSuppressed">
        /// The <see cref="MessageBoxResult"/> to return when the message is suppressed.
        /// </param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked, or <paramref name="defaultWhenSuppressed"/> if suppressed.</returns>
        public static MessageBoxResult Show(string messageBoxText, string caption, MessageBoxButton button,
            MessageBoxImage icon, string suppressSettingsKey, IDictionary<string, bool> suppressionStore,
            MessageBoxResult defaultWhenSuppressed)
        {
            return ShowCore(null, messageBoxText, caption, button, icon, MessageBoxResult.None,
                suppressSettingsKey, suppressionStore, defaultWhenSuppressed);
        }

        /// <summary>
        /// Displays a themed message box in front of the specified owner window with a "Don't show this message again" checkbox.
        /// If the user has previously checked the box for the given settings key, the dialog
        /// is not shown and <paramref name="defaultWhenSuppressed"/> is returned immediately.
        /// </summary>
        /// <param name="owner">The owner window of the message box.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="suppressSettingsKey">A unique key identifying this message for suppression.</param>
        /// <param name="suppressionStore">
        /// A dictionary that stores suppression state. The calling application owns persistence
        /// of this dictionary (e.g., user settings file, registry, etc.).
        /// </param>
        /// <param name="defaultWhenSuppressed">
        /// The <see cref="MessageBoxResult"/> to return when the message is suppressed.
        /// </param>
        /// <returns>The <see cref="MessageBoxResult"/> value that the user clicked, or <paramref name="defaultWhenSuppressed"/> if suppressed.</returns>
        public static MessageBoxResult Show(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, string suppressSettingsKey,
            IDictionary<string, bool> suppressionStore, MessageBoxResult defaultWhenSuppressed)
        {
            return ShowCore(owner, messageBoxText, caption, button, icon, MessageBoxResult.None,
                suppressSettingsKey, suppressionStore, defaultWhenSuppressed);
        }

        #endregion

        #region Private Implementation

        /// <summary>
        /// Core implementation that creates and shows the <see cref="MessageBoxWindow"/>.
        /// All public overloads funnel into this method.
        /// </summary>
        /// <param name="owner">The owner window, or null to auto-detect.</param>
        /// <param name="messageBoxText">The message to display.</param>
        /// <param name="caption">The title bar text.</param>
        /// <param name="button">The button combination to display.</param>
        /// <param name="icon">The icon to display.</param>
        /// <param name="defaultResult">The default button result.</param>
        /// <param name="suppressSettingsKey">Optional suppression key for "Don't show again" feature.</param>
        /// <param name="suppressionStore">Optional dictionary for storing suppression state.</param>
        /// <param name="defaultWhenSuppressed">The result to return when suppressed.</param>
        /// <returns>The <see cref="MessageBoxResult"/> value.</returns>
        private static MessageBoxResult ShowCore(Window owner, string messageBoxText, string caption,
            MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult,
            string suppressSettingsKey = null, IDictionary<string, bool> suppressionStore = null,
            MessageBoxResult defaultWhenSuppressed = MessageBoxResult.None)
        {
            // Check suppression before showing the dialog
            bool showSuppressCheckBox = suppressSettingsKey != null && suppressionStore != null;
            if (showSuppressCheckBox && suppressionStore.TryGetValue(suppressSettingsKey, out bool isSuppressed) && isSuppressed)
            {
                return defaultWhenSuppressed;
            }

            var window = new MessageBoxWindow();
            window.Configure(messageBoxText, caption, button, icon, defaultResult, showSuppressCheckBox);

            // Set owner window
            if (owner != null)
            {
                window.Owner = owner;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                // Auto-detect the active window as owner
                var activeWindow = Application.Current?.Windows.OfType<Window>()
                    .FirstOrDefault(w => w.IsActive);
                var ownerWindow = activeWindow ?? Application.Current?.MainWindow;

                if (ownerWindow != null && ownerWindow.IsLoaded)
                {
                    window.Owner = ownerWindow;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                }
            }

            // Set the window icon from the owner or application
            SetWindowIcon(window);

            window.ShowDialog();

            // Store suppression state if the checkbox was checked
            if (showSuppressCheckBox && window.SuppressChecked)
            {
                suppressionStore[suppressSettingsKey] = true;
            }

            return window.Result;
        }

        /// <summary>
        /// Sets the message box window icon from the owner window or the application's main window.
        /// This causes the title bar to display the calling application's icon, matching the behavior
        /// of standard Open File / Open Folder dialogs.
        /// </summary>
        /// <param name="window">The message box window to set the icon on.</param>
        private static void SetWindowIcon(MessageBoxWindow window)
        {
            if (window == null) return;
            // Try to get the icon from the owner window first, then fall back to main window
            ImageSource icon = null;

            if (window.Owner != null)
            {
                icon = window.Owner.Icon;
            }

            if (icon == null && Application.Current?.MainWindow != null)
            {
                icon = Application.Current.MainWindow.Icon;
            }

            if (icon != null)
            {
                window.Icon = icon;
            }
        }

        #endregion
    }
}
