using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Xml;

namespace FrameworkUI
{

    /// <summary>
    /// User settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class UserSettings
    {

        /// <summary>
        /// Gets or sets whether the user agreed to the terms and conditions for use.
        /// </summary>
        public static bool UserAgreedToTCU { get; set; } = false;

        /// <summary>
        /// Gets or sets the color theme.
        /// </summary>
        public static string ColorTheme { get; set; } = "Light";

        /// <summary>
        /// Gets or sets whether to save window layout.
        /// </summary>
        public static bool SaveWindowLayout { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum number of window menu items to show.
        /// </summary>
        public static int MaxWindowMenuItems { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of recent file items to show.
        /// </summary>
        public static int MaxRecentFileItems { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to show the Undo/Redo buttons on the toolbar.
        /// </summary>
        public static bool ShowUndoRedoButtons { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to compress the project file on close.
        /// </summary>
        public static bool CompressProjectFileOnClose { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to create an AutoRecover backup file.
        /// </summary>
        public static bool CreateAutoRecoverBackup { get; set; } = true;

        /// <summary>
        /// Gets or sets the AutoRecover interval.
        /// </summary>
        public static int AutoRecoverInterval { get; set; } = 30;

        /// <summary>
        /// Gets or sets whether to keep the last backup version if the file were to unexpectedly close.
        /// </summary>
        public static bool KeepLastBackupVersion { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the plot theme change warning dialog is suppressed.
        /// </summary>
        public static bool SuppressThemeChangeWarning { get; set; } = false;

        /// <summary>
        /// Gets or sets whether error messages beep.
        /// </summary>
        public static bool ErrorBeep { get; set; } = false;

        /// <summary>
        /// Gets or sets whether warning messages beep.
        /// </summary>
        public static bool WarningBeep { get; set; } = false;

        /// <summary>
        /// Gets or sets whether messages beep.
        /// </summary>
        public static bool MessageBeep { get; set; } = false;

        /// <summary>
        /// Gets or sets whether event messages beep.
        /// </summary>
        public static bool EventBeep { get; set; } = false;

        /// <summary>
        /// Gets or sets the error message color.
        /// </summary>
        public static System.Windows.Media.Color ErrorColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 228, 20, 0);

        /// <summary>
        /// Gets or sets the warning message color.
        /// </summary>
        public static System.Windows.Media.Color WarningColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 229, 160, 0);

        /// <summary>
        /// Gets or sets the message color.
        /// </summary>
        public static System.Windows.Media.Color MessageColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 26, 161, 226);

        /// <summary>
        /// Gets or sets the event message color.
        /// </summary>
        public static System.Windows.Media.Color EventColor { get; set; } = System.Windows.Media.Color.FromArgb(255, 0, 206, 209);

        /// <summary>
        /// Gets or sets the default folder location.
        /// </summary>
        public static string DefaultLocation { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


        /// <summary>
        /// Gets or sets the default output value decimal digits.
        /// </summary>
        public static int DefaultValueDigits
        {
            get { return _defaultValueDigits; }
            set
            {
                _defaultValueDigits = value;
                SetValueStringFormat(value);
            }
        }

        /// <summary>
        /// Sets the value string format based on the number of decimal digits.
        /// </summary>
        /// <param name="digits">The number of decimal digits to display.</param>
        private static void SetValueStringFormat(int digits)
        {
            string hashString = "";
            for (int i = 0; i < digits; i++)
                hashString += "#";
            ValueStringFormat = "#,##0." + hashString;
        }

        /// <summary>
        /// Gets or sets the value string format
        /// </summary>
        /// <returns></returns>
        public static string ValueStringFormat
        {
            get { return _valueStringFormat ?? string.Empty; }
            set
            {
                if (_valueStringFormat != value)
                {
                    _valueStringFormat = value;
                    OnGlobalPropertyChanged(nameof(ValueStringFormat));
                }
            }
        }

        private static int _defaultValueDigits = 2;
        private static string? _valueStringFormat;

        private static event PropertyChangedEventHandler? GlobalPropertyChanged;

        /// <summary>
        /// Raised at the start of <see cref="Save"/> before settings are written to disk.
        /// Subscribe to sync external state into UserSettings properties before persistence.
        /// </summary>
        public static event Action? Saving;

        /// <summary>
        /// Raises the global property changed event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        private static void OnGlobalPropertyChanged(string propertyName)
        {
            GlobalPropertyChanged?.Invoke(typeof(UserSettings), new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Restore the default user settings.
        /// </summary>
        /// <remarks>
        /// Make sure to update this method for each application.
        /// </remarks>
        public static void RestoreDefaults()
        {
            // General
            ColorTheme = "Light";
            SaveWindowLayout = true;
            MaxWindowMenuItems = 10;
            MaxRecentFileItems = 10;
            ShowUndoRedoButtons = true;
            // File Management
            CompressProjectFileOnClose = false;
            CreateAutoRecoverBackup = true;
            AutoRecoverInterval = 30;
            KeepLastBackupVersion = true;
            SuppressThemeChangeWarning = false;
            // Message window
            ErrorBeep = false;
            WarningBeep = false;
            MessageBeep = false;
            EventBeep = false;
            ErrorColor = System.Windows.Media.Color.FromArgb(255, 228, 20, 0);
            WarningColor = System.Windows.Media.Color.FromArgb(255, 229, 160, 0);
            MessageColor = System.Windows.Media.Color.FromArgb(255, 26, 161, 226);
            EventColor = System.Windows.Media.Color.FromArgb(255, 0, 206, 209);
            // Defaults
            DefaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            DefaultValueDigits = 2;
        }

        /// <summary>
        /// Loads user settings from an XML file.
        /// </summary>
        /// <param name="xmlFilePath">The full path to the XML file containing user settings.</param>
        /// <remarks>
        /// <para>
        /// If the settings file does not exist, default settings are restored.
        /// Invalid or corrupted values in the XML file are silently ignored and
        /// defaults are used instead.
        /// </para>
        /// </remarks>
        public static void Load(string xmlFilePath)
        {
            if (!File.Exists(xmlFilePath))
            {
                RestoreDefaults();
                return;
            }

            try
            {
                using (XmlReader xmlReader = XmlReader.Create(xmlFilePath))
                {
                    while (xmlReader.Read())
                    {
                        if (xmlReader.NodeType != XmlNodeType.Element) continue;

                        string elementName = xmlReader.Name;
                        string? innerXml = null;

                        // TCU
                        if (elementName == nameof(UserAgreedToTCU))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool agreed))
                                UserAgreedToTCU = agreed;
                        }
                        // General
                        else if (elementName == nameof(ColorTheme))
                        {
                            ColorTheme = xmlReader.ReadInnerXml();
                        }
                        else if (elementName == nameof(SaveWindowLayout))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool save))
                                SaveWindowLayout = save;
                        }
                        else if (elementName == nameof(MaxWindowMenuItems))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxItems))
                                MaxWindowMenuItems = maxItems;
                        }
                        else if (elementName == nameof(MaxRecentFileItems))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int maxRecent))
                                MaxRecentFileItems = maxRecent;
                        }
                        else if (elementName == nameof(ShowUndoRedoButtons))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool showButtons))
                                ShowUndoRedoButtons = showButtons;
                        }
                        // File Management
                        else if (elementName == nameof(CompressProjectFileOnClose))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool compress))
                                CompressProjectFileOnClose = compress;
                        }
                        else if (elementName == nameof(CreateAutoRecoverBackup))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool autoRecover))
                                CreateAutoRecoverBackup = autoRecover;
                        }
                        else if (elementName == nameof(AutoRecoverInterval))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int interval))
                                AutoRecoverInterval = Math.Max(1, interval);
                        }
                        else if (elementName == nameof(KeepLastBackupVersion))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool keepBackup))
                                KeepLastBackupVersion = keepBackup;
                        }
                        else if (elementName == nameof(SuppressThemeChangeWarning))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool suppress))
                                SuppressThemeChangeWarning = suppress;
                        }
                        // Message Window
                        else if (elementName == nameof(ErrorBeep))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool beep))
                                ErrorBeep = beep;
                        }
                        else if (elementName == nameof(WarningBeep))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool beep))
                                WarningBeep = beep;
                        }
                        else if (elementName == nameof(MessageBeep))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool beep))
                                MessageBeep = beep;
                        }
                        else if (elementName == nameof(EventBeep))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (bool.TryParse(innerXml, out bool beep))
                                EventBeep = beep;
                        }
                        else if (elementName == nameof(ErrorColor))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int argb))
                                ErrorColor = UtilityFunctions.IntegerToColor(ref argb);
                        }
                        else if (elementName == nameof(WarningColor))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int argb))
                                WarningColor = UtilityFunctions.IntegerToColor(ref argb);
                        }
                        else if (elementName == nameof(MessageColor))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int argb))
                                MessageColor = UtilityFunctions.IntegerToColor(ref argb);
                        }
                        else if (elementName == nameof(EventColor))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int argb))
                                EventColor = UtilityFunctions.IntegerToColor(ref argb);
                        }
                        // Defaults
                        else if (elementName == nameof(DefaultLocation))
                        {
                            DefaultLocation = xmlReader.ReadInnerXml();
                        }
                        else if (elementName == nameof(DefaultValueDigits))
                        {
                            innerXml = xmlReader.ReadInnerXml();
                            if (int.TryParse(innerXml, NumberStyles.Integer, CultureInfo.InvariantCulture, out int digits))
                                DefaultValueDigits = digits;
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // If the XML file is corrupted or inaccessible, restore defaults
                RestoreDefaults();
            }
        }

        /// <summary>
        /// Save the user settings to XML.
        /// </summary>
        /// <param name="xmlFilePath">The XML file path for storing the user settings.</param>
        public static void Save(string xmlFilePath)
        {
            // Allow subscribers to sync external state before writing to disk.
            Saving?.Invoke();

            // Create the settings directory if it doesn't already exist.
            var directoryPath = Path.GetDirectoryName(xmlFilePath);
            if (directoryPath != null && Directory.Exists(directoryPath) == false)
            {
                Directory.CreateDirectory(directoryPath);
            }
            // Write to a temp file first, then atomically replace the target to avoid corruption.
            var tempPath = xmlFilePath + ".tmp";
            // Now, save settings.
            using (var xmlWriter = XmlWriter.Create(tempPath, new XmlWriterSettings() { Indent = true }))
            {
                // Write the XML declaration.
                xmlWriter.WriteStartDocument();
                // Write a comment.
                xmlWriter.WriteComment("User Settings");
                // Write settings.
                xmlWriter.WriteStartElement("Settings");
                // 
                // TCU
                // 
                xmlWriter.WriteStartElement(nameof(UserAgreedToTCU));
                xmlWriter.WriteString(UserAgreedToTCU.ToString());
                xmlWriter.WriteEndElement();
                // 
                // General
                // 
                xmlWriter.WriteStartElement(nameof(ColorTheme));
                xmlWriter.WriteString(ColorTheme.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(SaveWindowLayout));
                xmlWriter.WriteString(SaveWindowLayout.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(MaxWindowMenuItems));
                xmlWriter.WriteString(MaxWindowMenuItems.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(MaxRecentFileItems));
                xmlWriter.WriteString(MaxRecentFileItems.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(ShowUndoRedoButtons));
                xmlWriter.WriteString(ShowUndoRedoButtons.ToString());
                xmlWriter.WriteEndElement();
                //
                // File Management
                // 
                xmlWriter.WriteStartElement(nameof(CompressProjectFileOnClose));
                xmlWriter.WriteString(CompressProjectFileOnClose.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(CreateAutoRecoverBackup));
                xmlWriter.WriteString(CreateAutoRecoverBackup.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(AutoRecoverInterval));
                xmlWriter.WriteString(AutoRecoverInterval.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(KeepLastBackupVersion));
                xmlWriter.WriteString(KeepLastBackupVersion.ToString());
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(SuppressThemeChangeWarning));
                xmlWriter.WriteString(SuppressThemeChangeWarning.ToString());
                xmlWriter.WriteEndElement();
                //
                // Message Window
                // 
                xmlWriter.WriteStartElement(nameof(ErrorBeep));
                xmlWriter.WriteString(ErrorBeep.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(WarningBeep));
                xmlWriter.WriteString(WarningBeep.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(MessageBeep));
                xmlWriter.WriteString(MessageBeep.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(EventBeep));
                xmlWriter.WriteString(EventBeep.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(ErrorColor));
                xmlWriter.WriteString(UtilityFunctions.ColorToInteger(ErrorColor).ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(WarningColor));
                xmlWriter.WriteString(UtilityFunctions.ColorToInteger(WarningColor).ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(MessageColor));
                xmlWriter.WriteString(UtilityFunctions.ColorToInteger(MessageColor).ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                //
                xmlWriter.WriteStartElement(nameof(EventColor));
                xmlWriter.WriteString(UtilityFunctions.ColorToInteger(EventColor).ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                // 
                // Defaults
                // 
                xmlWriter.WriteStartElement(nameof(DefaultLocation));
                xmlWriter.WriteString(DefaultLocation.ToString());
                xmlWriter.WriteEndElement();
                // 
                xmlWriter.WriteStartElement(nameof(DefaultValueDigits));
                xmlWriter.WriteString(DefaultValueDigits.ToString(CultureInfo.InvariantCulture));
                xmlWriter.WriteEndElement();
                // 
                // The end of settings.
                xmlWriter.WriteEndElement();
                // Flush and Close the XmlTextWriter.
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();
                xmlWriter.Close();
            }
            // Atomically replace the target file with the temp file. Guarded because a
            // permissions failure on the final replace would otherwise surface as an
            // unhandled exception on the UI thread at application close - the settings
            // write is best-effort and shouldn't block shutdown.
            try
            {
                File.Move(tempPath, xmlFilePath, overwrite: true);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                System.Diagnostics.Debug.WriteLine($"[UserSettings] Failed to save settings: {ex.Message}");
                try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            }
        }

    }
}
