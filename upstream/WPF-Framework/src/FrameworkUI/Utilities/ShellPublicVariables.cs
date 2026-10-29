namespace FrameworkUI
{
    /// <summary>
    /// A static class for public shared shell variables.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Authors:</b>
    /// </para>
    /// <list type="bullet">
    ///     <item>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</item>
    /// </list>
    /// </remarks>
    public static class ShellPublicVariables
    {
        /// <summary>
        /// Content ID for the project explorer AvalonDock window.
        /// </summary>
        public const string ProjectExplorerContentID = "ProjectExplorer";
        /// <summary>
        /// Content ID for the message window AvalonDock window.
        /// </summary>
        public const string MessageWindowContentID = "MessageWindow";
        /// <summary>
        /// Content ID for the properties window AvalonDock window.
        /// </summary>
        public const string PropertiesWindowContentID = "PropertiesWindow";
        /// <summary>
        /// Display title for the project explorer window.
        /// </summary>
        public const string ProjectExplorerTitle = "  Project Explorer";
        /// <summary>
        /// Display title for the map explorer window.
        /// </summary>
        public const string MapExplorerTitle = "  Map Explorer";
        /// <summary>
        /// Display title for the message window.
        /// </summary>
        public const string MessageWindowTitle = "  Message Window";
        /// <summary>
        /// Display title for the properties window.
        /// </summary>
        public const string PropertiesWindowTitle = "  Properties";

        /// <summary>
        /// The base folder path for all application settings.
        /// Prefers a "settings" subfolder next to the application exe for easy user access.
        /// Falls back to AppData\Roaming if the exe directory is not writable.
        /// </summary>
        public static readonly string BaseFolderPath = GetBaseFolderPath();

        /// <summary>
        /// The folder path for user settings.
        /// </summary>
        public static string UserSettingsFolderPath = System.IO.Path.Combine(BaseFolderPath, "UserSettings") + System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// File path of the user settings.
        /// </summary>
        public static string UserSettingsFilePath = UserSettingsFolderPath + "UserSettings.xml";

        /// <summary>
        /// The folder path for layout.
        /// </summary>
        public static string AvalonDockLayoutFolderPath = System.IO.Path.Combine(BaseFolderPath, "Layout") + System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// File path of the default AvalonDock layout.
        /// </summary>
        public static string DefaultAvalonDockLayoutFilePath = AvalonDockLayoutFolderPath + "AvalonDock.xml";

        /// <summary>
        /// The folder path for recent file list.
        /// </summary>
        public static string RecentFileListFolderPath = System.IO.Path.Combine(BaseFolderPath, "RecentFiles") + System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// File path of the recent file list.
        /// </summary>
        public static string RecentFileListFilePath = RecentFileListFolderPath + "RecentFileList.xml";

        /// <summary>
        /// The folder path for message log.
        /// </summary>
        public static string MessageLogFolderPath = System.IO.Path.Combine(BaseFolderPath, "MessageLog") + System.IO.Path.DirectorySeparatorChar;

        /// <summary>
        /// File path of the message log.
        /// </summary>
        public static string MessageLogFilePath = MessageLogFolderPath + "MessageLog.txt";

        /// <summary>
        /// Determines the base folder path for application settings.
        /// Tries the application exe directory first; falls back to AppData\Roaming if not writable.
        /// </summary>
        /// <returns>The base folder path for application settings.</returns>
        private static string GetBaseFolderPath()
        {
            // Primary: Settings folder next to the application exe
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string primaryPath = System.IO.Path.Combine(exeDirectory, "settings");

            try
            {
                // Test if we can write to the exe directory
                System.IO.Directory.CreateDirectory(primaryPath);
                return primaryPath;
            }
            catch
            {
                // Fallback: AppData\Roaming with product name
                string productName = ApplicationAttributes.ProductName;
                if (string.IsNullOrWhiteSpace(productName))
                    productName = "Application";

                return System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    productName,
                    "settings");
            }
        }

        /// <summary>
        /// Gets or sets the software version from application attributes.
        /// </summary>
        public static string SoftwareVersion = ApplicationAttributes.Version;
        /// <summary>
        /// Gets or sets the software version release date.
        /// Defaults to the assembly's last write time; can be overridden by the application.
        /// </summary>
        public static string SoftwareVersionDate = GetAssemblyDate();

        /// <summary>
        /// Gets the executing assembly's last-write month and year.
        /// </summary>
        /// <returns>The assembly date label, or an empty string when unavailable.</returns>
        private static string GetAssemblyDate()
        {
            try
            {
                var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(location))
                {
                    var lastWrite = System.IO.File.GetLastWriteTime(location);
                    return lastWrite.ToString("MMMM yyyy");
                }
            }
            catch { }
            return string.Empty;
        }
        /// <summary>
        /// Gets or sets the software name from application attributes.
        /// </summary>
        public static string SoftwareName = ApplicationAttributes.Title;
        /// <summary>
        /// Gets or sets the file extension for project files.
        /// </summary>
        public static string SoftwareExtension = ".tra";
        /// <summary>
        /// Gets or sets the file extension for backup files.
        /// </summary>
        public static string BackupExtension = ".bak";

        /// <summary>
        /// Filter for file dialogs.
        /// </summary>
        public static string FileDialogFilter
        {
            get { return $"{SoftwareName} Files (*{SoftwareExtension})|*{SoftwareExtension}"; }
        }

        /// <summary>
        /// Filter for backup file dialog.
        /// </summary>
        public static string BackupFileDialogFilter
        {
            get { return $"{SoftwareName} BAK Files (*{SoftwareExtension}{BackupExtension})|*{SoftwareExtension}{BackupExtension}"; }
        }

        /// <summary>
        /// Determines if a simulation is in progress.
        /// </summary>
        public static volatile bool SimulationInProgress = false;

        /// <summary>
        /// Determines if a simulation has been canceled.
        /// </summary>
        public static volatile bool SimulationIsCanceled = false;

        /// <summary>
        /// Gets or sets a value indicating whether a Bayesian estimation file is being copied.
        /// These files are large and can take a long time to copy.
        /// </summary>
        public static volatile bool CopyingSimulationFile = false;

        /// <summary>
        /// Gets or sets a value indicating whether a project file is being compressed.
        /// </summary>
        public static volatile bool CompactionInProgress;

        /// <summary>
        /// Gets or sets a value indicating whether the dragged file can be dropped and opened.
        /// </summary>
        public static volatile bool IsDroppableFile = false;

    }
}
