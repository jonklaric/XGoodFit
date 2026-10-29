using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using GenericControls;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for OptionsDialog.xaml providing application settings dialog.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class OptionsDialog : MetroWindow
    {
        /// <summary>
        /// Occurs when the Apply button is clicked.
        /// </summary>
        public event RoutedEventHandler? Apply_Click;

        /// <summary>
        /// Initializes a new instance of the <see cref="OptionsDialog"/> class.
        /// </summary>
        /// <param name="mainWindow">The parent main window.</param>
        public OptionsDialog(MainWindow mainWindow)
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
            _mainWindow = mainWindow;
            Owner = _mainWindow;
        }

        /// <summary>
        /// Reference to the main window.
        /// </summary>
        private MainWindow _mainWindow;

        /// <summary>
        /// On load, set options from user settings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OptionsDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Load General Settings
            GeneralOptions.ColorTheme = UserSettings.ColorTheme.ToString();
            GeneralOptions.SaveWindowLayout = UserSettings.SaveWindowLayout;
            GeneralOptions.MaxWindowMenuItems = UserSettings.MaxWindowMenuItems;
            GeneralOptions.MaxRecentFileItems = UserSettings.MaxRecentFileItems;

            // Load File Management Settings
            FileManagementOptions.CompressProjectFileOnClose = UserSettings.CompressProjectFileOnClose;
            FileManagementOptions.CreateAutoRecoverBackup = UserSettings.CreateAutoRecoverBackup;
            FileManagementOptions.AutoRecoverInterval = UserSettings.AutoRecoverInterval;
            FileManagementOptions.KeepLastBackupVersion = UserSettings.KeepLastBackupVersion;

            // Load Message Window Settings
            MessageWindowOptions.MessageBeep = UserSettings.MessageBeep;
            MessageWindowOptions.WarningBeep = UserSettings.WarningBeep;
            MessageWindowOptions.ErrorBeep = UserSettings.ErrorBeep;
            MessageWindowOptions.EventBeep = UserSettings.EventBeep;
            MessageWindowOptions.MessageColor = new SolidColorBrush(UserSettings.MessageColor);
            MessageWindowOptions.WarningColor = new SolidColorBrush(UserSettings.WarningColor);
            MessageWindowOptions.ErrorColor = new SolidColorBrush(UserSettings.ErrorColor);
            MessageWindowOptions.EventColor = new SolidColorBrush(UserSettings.EventColor);

            // Load Defaults Settings
            DefaultsOptions.DefaultLocation = UserSettings.DefaultLocation;
            DefaultsOptions.DefaultValueDigits = UserSettings.DefaultValueDigits;
        }

        /// <summary>
        /// On Apply, set dialog result to true and close.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btn_Apply_Click(object sender, RoutedEventArgs e)
        {
            // Record the current theme:
            string currentColorTheme = UserSettings.ColorTheme;
            // Record the AutoRecover settings
            bool currentAutoRecover = UserSettings.CreateAutoRecoverBackup;
            int currentAutoRecoverTime = UserSettings.AutoRecoverInterval;

            // Update General Settings
            UserSettings.ColorTheme = GeneralOptions.ColorTheme.ToString();
            UserSettings.SaveWindowLayout = GeneralOptions.SaveWindowLayout;
            UserSettings.MaxWindowMenuItems = GeneralOptions.MaxWindowMenuItems;
            UserSettings.MaxRecentFileItems = GeneralOptions.MaxRecentFileItems;

            // Update File Management Settings
            UserSettings.CompressProjectFileOnClose = FileManagementOptions.CompressProjectFileOnClose;
            UserSettings.CreateAutoRecoverBackup = FileManagementOptions.CreateAutoRecoverBackup;
            UserSettings.AutoRecoverInterval = FileManagementOptions.AutoRecoverInterval;
            UserSettings.KeepLastBackupVersion = FileManagementOptions.KeepLastBackupVersion;

            // Update Message Window Settings
            var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();
            UserSettings.MessageBeep = MessageWindowOptions.MessageBeep;
            UserSettings.WarningBeep = MessageWindowOptions.WarningBeep;
            UserSettings.ErrorBeep = MessageWindowOptions.ErrorBeep;
            UserSettings.EventBeep = MessageWindowOptions.EventBeep;
            messenger.MessageBeep = UserSettings.MessageBeep;
            messenger.WarningBeep = UserSettings.WarningBeep;
            messenger.ErrorBeep = UserSettings.ErrorBeep;
            messenger.EventBeep = UserSettings.EventBeep;

            SolidColorBrush solidBrush = (SolidColorBrush)MessageWindowOptions.MessageColor;
            UserSettings.MessageColor = solidBrush.Color;
            messenger.MessageColor = solidBrush;

            solidBrush = (SolidColorBrush)MessageWindowOptions.WarningColor;
            UserSettings.WarningColor = solidBrush.Color;
            messenger.WarningColor = solidBrush;

            solidBrush = (SolidColorBrush)MessageWindowOptions.ErrorColor;
            UserSettings.ErrorColor = solidBrush.Color;
            messenger.ErrorColor = solidBrush;

            solidBrush = (SolidColorBrush)MessageWindowOptions.EventColor;
            UserSettings.EventColor = solidBrush.Color;
            messenger.EventColor = solidBrush;

            // Update Defaults Settings
            UserSettings.DefaultLocation = DefaultsOptions.DefaultLocation;
            UserSettings.DefaultValueDigits = DefaultsOptions.DefaultValueDigits;

            // Save settings
            UserSettings.Save(ShellPublicVariables.UserSettingsFilePath);

            // Update Color Theme if needed
            if (UserSettings.ColorTheme != currentColorTheme)
            {
                if (UserSettings.ColorTheme == "Blue")
                {
                    ThemeManager.SetTheme(ThemeColor.Blue);
                }
                else if (UserSettings.ColorTheme == "Dark")
                {
                    ThemeManager.SetTheme(ThemeColor.Dark);
                }
                else if (UserSettings.ColorTheme == "Light")
                {
                    ThemeManager.SetTheme(ThemeColor.Light);
                }
            }
            // Update AutoRecover if needed
            if (UserSettings.CreateAutoRecoverBackup != currentAutoRecover || UserSettings.AutoRecoverInterval != currentAutoRecoverTime)
            {
                if (UserSettings.CreateAutoRecoverBackup == true)
                {
                    AutoBackup.Cancel();
                    AutoBackup.Start();
                }
                else if (UserSettings.CreateAutoRecoverBackup == false)
                {
                    AutoBackup.Cancel();
                }
            }
            // Update Recent File Menu
            _mainWindow.RecentFiles.NumberOfFilesToDisplay = UserSettings.MaxRecentFileItems;
            // Update Windows Menu
            _mainWindow.OpenWindows.NumberOfWindowsToDisplay = UserSettings.MaxWindowMenuItems;
            // Pass Event
            Apply_Click?.Invoke(sender, e);
        }

        /// <summary>
        /// On Cancel, set dialog result to false and close.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Close dialog
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// On closing, activate the Main Window.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OptionsDialog_Closing(object sender, CancelEventArgs e)
        {
            if (Owner != null) Owner.Activate();
        }

    }
}
