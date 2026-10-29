using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Xml;

namespace FrameworkUI
{
    /// <summary>
    /// A class for displaying recent file items in a menu.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Woody Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class RecentFiles : Separator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecentFiles"/> class.
        /// </summary>
        public RecentFiles()
        {
            Loaded += ConnectToMenu;
            MaxFilePathWidth = 50;
            MenuItemFormatOneToNine = "_{0}:  {1}";
            MenuItemFormatTenPlus = "{0}:  {1}";
        }

        /// <summary>
        /// Get the observable collection of recent files.
        /// </summary>
        public ObservableCollection<RecentFileItem> Collection { get; private set; } = new ObservableCollection<RecentFileItem>();

        /// <summary>
        /// Gets or sets the parent file menu.
        /// </summary>
        public MenuItem? FileMenu { get; set; }

        /// <summary>
        /// Event raised when a recent file menu item is clicked.
        /// </summary>
        public event MenuClickEventHandler? MenuClick;

        /// <summary>
        /// Delegate for the <see cref="MenuClick"/> event.
        /// </summary>
        /// <param name="filePath">The full file path of the recent file item.</param>
        public delegate void MenuClickEventHandler(string filePath);

        /// <summary>
        /// Gets or sets the number of recent files to show in the menu.
        /// </summary>
        public int NumberOfFilesToDisplay { get; set; }

        /// <summary>
        /// Get or Set the width of the recent file item's text in the menu.
        /// Default: 35
        /// </summary>
        public int MaxFilePathWidth { get; set; }

        /// <summary>
        /// Gets or sets the XML file path for storing the recent file list.
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, file path, displayPath );
        /// Default = "_{0}:  {2}"
        /// </summary>
        public string MenuItemFormatOneToNine { get; set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, file path, displayPath );
        /// Default = "{0}:  {2}"
        /// </summary>
        public string MenuItemFormatTenPlus { get; set; }

        /// <summary>
        /// Separator used at bottom of recent file list.
        /// </summary>
        private Separator? MenuSeparator;

        /// <summary>
        /// A menu item to display more recent files.
        /// </summary>
        private MenuItem? MoreFilesMenuItem;

        /// <summary>
        /// Gets the index of the recent file item containing the specified file path.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        /// <returns>The zero-based index of the file path, or -1 if not found.</returns>
        public int IndexOf(string filePath)
        {
            for (int i = 0; i < Collection.Count; i++)
                if (Collection[i].FilePath == filePath) return i;
            return -1;
        }

        /// <summary>
        /// Open recent file item.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        public void OpenItem(string filePath)
        {
            MenuClick?.Invoke(filePath);
        }

        /// <summary>
        /// Add a new recent file item.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        public void AddItem(string filePath)
        {
            int index = IndexOf(filePath);
            if (index >= 0) RemoveItem(index);
            var item = new RecentFileItem(filePath);
            Collection.Insert(0, item);
            // Trim the collection to NumberOfFilesToDisplay so it doesn't grow unbounded.
            // Only trim when a positive cap is configured (NumberOfFilesToDisplay defaults to 0).
            if (NumberOfFilesToDisplay > 0)
            {
                while (Collection.Count > NumberOfFilesToDisplay)
                {
                    RemoveItem(Collection.Count - 1);
                }
            }
            AddToJumpList(filePath);
            SaveToXML();
        }

        /// <summary>
        /// Remove recent file item.
        /// </summary>
        /// <param name="index">Zero-based index of the file to remove.</param>
        public void RemoveItem(int index)
        {
            if (index >= 0)
            {
                if (FileMenu != null)
                {
                    FileMenu.Items.Remove(Collection[index].MenuItem);
                }
                Collection.RemoveAt(index);
                RemoveFromJumpList(index);
            }
        }

        /// <summary>
        /// Clear all recent file items.
        /// </summary>
        public void ClearAll()
        {
            for (int i = Collection.Count - 1; i >= 0; i -= 1)
                RemoveItem(i);
        }

        /// <summary>
        /// Add recent file to Microsoft Windows Jump List.
        /// </summary>
        /// <param name="filePath">Path to the file.</param>
        public void AddToJumpList(string filePath)
        {
            var task = new JumpTask
            {
                ApplicationPath = Assembly.GetEntryAssembly()?.Location ?? string.Empty,
                Title = Path.GetFileNameWithoutExtension(filePath),
                Arguments = filePath,
                Description = filePath,
                CustomCategory = "Recent Files"
            };

            var jumpList = JumpList.GetJumpList(Application.Current);
            if (jumpList == null) return;

            // De-duplicate: remove any existing JumpItem matching this file path before inserting.
            // Without this, opening the same file repeatedly accumulates duplicate entries in the
            // Windows JumpList, and the trim below only removes the oldest — leaving stale dupes
            // visible in the taskbar's Recent Files category.
            for (int i = jumpList.JumpItems.Count - 1; i >= 0; i--)
            {
                if (jumpList.JumpItems[i] is JumpTask existing &&
                    string.Equals(existing.Arguments, filePath, StringComparison.OrdinalIgnoreCase))
                {
                    jumpList.JumpItems.RemoveAt(i);
                }
            }

            jumpList.JumpItems.Insert(0, task);
            if (jumpList.JumpItems.Count > NumberOfFilesToDisplay)
            {
                jumpList.JumpItems.RemoveAt(NumberOfFilesToDisplay);
            }

            jumpList.ShowRecentCategory = false;
            jumpList.Apply();
            JumpList.SetJumpList(Application.Current, jumpList);
        }

        /// <summary>
        /// Remove recent file from Microsoft Windows Jump List.
        /// </summary>
        /// <param name="index">Zero-based index of the file to remove.</param>
        public void RemoveFromJumpList(int index)
        {
            var jumpList = JumpList.GetJumpList(Application.Current);
            if (jumpList == null) return;
            if (index > jumpList.JumpItems.Count - 1) return;
            jumpList.JumpItems.RemoveAt(index);
            jumpList.ShowRecentCategory = false;
            jumpList.Apply();
            JumpList.SetJumpList(Application.Current, jumpList);
        }

        /// <summary>
        /// Load the Microsoft Windows Jump List.
        /// </summary>
        public void LoadJumpList()
        {
            var jumpList = JumpList.GetJumpList(Application.Current);
            if (jumpList == null) return;
            jumpList.JumpItems.Clear();
            jumpList.ShowRecentCategory = false;
            for (int i = 0; i < Collection.Count; i++)
            {
                if (i <= NumberOfFilesToDisplay - 1)
                {
                    var task = new JumpTask
                    {
                        ApplicationPath = Assembly.GetEntryAssembly()?.Location ?? string.Empty,
                        Title = Path.GetFileNameWithoutExtension(Collection[i].FilePath),
                        Arguments = Collection[i].FilePath,
                        Description = Collection[i].FilePath,
                        CustomCategory = "Recent Files"
                    };
                    jumpList.JumpItems.Add(task);
                }
            }

            jumpList.Apply();
            JumpList.SetJumpList(Application.Current, jumpList);
        }

        /// <summary>
        /// Load the recent files from XML.
        /// </summary>
        public void LoadFromXML()
        {
            try
            {
                // Check if the recent file list exists.
                if (File.Exists(FilePath) == true)
                {
                    // Clear collection
                    ClearAll();
                    // Load recent files from disk
                    var xml = new XmlDocument();
                    xml.Load(FilePath);
                    var recentFilesNode = xml.GetElementsByTagName("RecentFiles").Item(0);
                    if (recentFilesNode == null) return;

                    foreach (XmlNode node in recentFilesNode.ChildNodes)
                    {
                        // Check to see if the file still exists
                        var pathAttribute = node.Attributes?["Path"];
                        if (pathAttribute == null) continue;

                        string filePath = pathAttribute.Value;
                        if (File.Exists(filePath))
                        {
                            var item = new RecentFileItem(filePath);
                            Collection.Add(item);
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is XmlException || ex is IOException || ex is UnauthorizedAccessException)
            {
                // If the XML file is corrupted or inaccessible, drop the list and fall through to LoadJumpList.
                ClearAll();
            }
            LoadJumpList();
        }

        /// <summary>
        /// Save the recent files to XML.
        /// </summary>
        public void SaveToXML()
        {
            if (string.IsNullOrEmpty(FilePath)) return;

            // Create the recent files directory if it doesn't already exist.
            var directoryName = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            // Build the XML document in memory.
            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", null, null));
            var root = xml.CreateElement("RecentFiles");
            foreach (var fileItem in Collection)
            {
                var item = xml.CreateElement("Item");
                var attribute = xml.CreateAttribute("Path");
                attribute.Value = fileItem.FilePath;
                item.Attributes.Append(attribute);
                root.AppendChild(item);
            }
            xml.AppendChild(root);
            // Write to a temp file first, then atomically replace the target to avoid corruption.
            var tempPath = FilePath + ".tmp";
            xml.Save(tempPath);
            File.Move(tempPath, FilePath, overwrite: true);
        }

        /// <summary>
        /// Connect to the parent file menu item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        /// <remarks>
        /// <para>
        /// <b>Parent contract:</b> <see cref="RecentFiles"/> derives from
        /// <see cref="System.Windows.Controls.Separator"/> and is intended to live
        /// inside a <see cref="System.Windows.Controls.MenuItem"/> (typically the
        /// File menu). This handler runs on the <c>Loaded</c> event and walks the
        /// logical parent: if <c>Parent</c> is not a <c>MenuItem</c>, the wiring is
        /// silently skipped (a Debug.WriteLine is emitted) so a misconfigured XAML
        /// host cannot crash application startup. Hosts that need to embed
        /// <c>RecentFiles</c> elsewhere (toolbar, ribbon, custom shell) must subclass
        /// it and override the menu wiring.
        /// </para>
        /// </remarks>
        private void ConnectToMenu(object sender, RoutedEventArgs e)
        {
            // The parent must be a MenuItem for SubmenuOpened wiring below to make sense, but a
            // misconfigured XAML host (e.g. RecentFiles dropped into a ContextMenu, ToolBar, or
            // any non-MenuItem container) is a developer error — not a reason to crash the app
            // at startup. Log and skip; the recent-files feature simply won't activate there.
            if (Parent is not MenuItem parentItem)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"RecentFiles.ConnectToMenu: Parent must be a MenuItem but is '{Parent?.GetType().FullName ?? "null"}'. " +
                    "Recent files menu wiring skipped.");
                return;
            }
            if (FileMenu != null && FileMenu.Equals(parentItem)) return;
            if (FileMenu != null) FileMenu.SubmenuOpened -= FileMenu_SubMenuOpened;
            FileMenu = parentItem;
            FileMenu.SubmenuOpened += FileMenu_SubMenuOpened;
        }

        /// <summary>
        /// When the parent file menu item is opened, build the list of recent files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void FileMenu_SubMenuOpened(object sender, RoutedEventArgs e)
        {
            RemoveMenuItems();
            LoadMenuItems();
        }

        /// <summary>
        /// Remove all recent file menu items.
        /// </summary>
        private void RemoveMenuItems()
        {
            if (FileMenu == null) return;

            if (MenuSeparator != null) FileMenu.Items.Remove(MenuSeparator);
            // clear the list of menu items
            for (int i = Collection.Count - 1; i >= 0; i -= 1)
            {
                if (FileMenu.Items.Contains(Collection[i].MenuItem) == true)
                {
                    FileMenu.Items.Remove(Collection[i].MenuItem);
                }
            }
            if (MoreFilesMenuItem != null) FileMenu.Items.Remove(MoreFilesMenuItem);
            MenuSeparator = null;
            MoreFilesMenuItem = null;
        }

        /// <summary>
        /// Load all recent file menu items.
        /// </summary>
        private void LoadMenuItems()
        {
            if (FileMenu == null) return;

            int iMenuItem = FileMenu.Items.IndexOf(this);
            if (Collection.Count == 0) return;
            
            for (int i = 0; i < Collection.Count; i++)
            {
                if (i <= NumberOfFilesToDisplay - 1)
                {
                    string header = GetMenuItemText(i + 1, Collection[i].DisplayPath);
                    var menuItem = new MenuItem() { Header = header, ToolTip = new TextBlock() { Text = Collection[i].FilePath, TextWrapping = TextWrapping.Wrap } };
                    menuItem.Click += MenuItem_Click;
                    Collection[i].MenuItem = menuItem;
                    // add menu item
                    iMenuItem += 1;
                    FileMenu.Items.Insert(iMenuItem, Collection[i].MenuItem);
                }
                else
                {
                    MoreFilesMenuItem = new MenuItem() { Header = "More Files..." };
                    MoreFilesMenuItem.Click += MoreFilesMenuItem_Click;
                    // add menu item
                    iMenuItem += 1;
                    FileMenu.Items.Insert(iMenuItem, MoreFilesMenuItem);
                    break;
                }
            }

            MenuSeparator = new Separator();
            FileMenu.Items.Insert(iMenuItem + 1, MenuSeparator);
            LoadJumpList();
        }

        /// <summary>
        /// Get the menu item header text.
        /// </summary>
        /// <param name="index">The 1-based index of the recent file item.</param>
        /// <param name="displaypath">Display path to the file.</param>
        /// <returns>The formatted menu item text.</returns>
        private string GetMenuItemText(int index, string displaypath)
        {
            string format = index < 10 ? MenuItemFormatOneToNine : MenuItemFormatTenPlus;
            string shortPath = UtilityFunctions.ShortenPathname(displaypath, MaxFilePathWidth);
            return string.Format(format, index, shortPath);
        }

        /// <summary>
        /// On click, open recent file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (FileMenu == null) return;

            int iMenuItem = FileMenu.Items.IndexOf(this);
            int iSender = FileMenu.Items.IndexOf((MenuItem)sender);
            int iItem = iSender - iMenuItem - 1;
            if (iItem < 0 || iItem >= Collection.Count) return;
            OpenItem(Collection[iItem].FilePath);
        }

        /// <summary>
        /// When clicked, open the recent files dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MoreFilesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ImageSource? image = null;
            if (Collection.Count > 0)
            {
                try
                {
                    if (File.Exists(Collection[0].FilePath))
                    {
                        image = ExtractFileIcon(Collection[0].FilePath);
                    }
                }
                catch
                {
                    // Fall through to use fallback icon
                }
            }
            // Use OpenProjectImage as fallback if no icon could be extracted
            if (image == null)
            {
                image = (ImageSource)Application.Current.TryFindResource("ApplicationImage");
            }
            var recentFilesDialog = new RecentFilesDialog() { Files = this, FileImage = image, Icon = image };
            recentFilesDialog.ShowDialog();
        }

        #region File Icon Extraction

        /// <summary>
        /// Retrieves shell metadata, including icons, for a file system path.
        /// </summary>
        /// <param name="pszPath">The file system path to inspect.</param>
        /// <param name="dwFileAttributes">The file attribute flags supplied to the shell.</param>
        /// <param name="psfi">The structure that receives shell file information.</param>
        /// <param name="cbSizeFileInfo">The size of <paramref name="psfi"/>.</param>
        /// <param name="uFlags">The shell file information flags.</param>
        /// <returns>A shell handle value for the requested information.</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        /// <summary>
        /// Releases a native icon handle returned by the shell.
        /// </summary>
        /// <param name="hIcon">The icon handle to release.</param>
        /// <returns><c>true</c> when the icon handle is destroyed; otherwise, <c>false</c>.</returns>
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Receives shell file information for native icon extraction.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_SMALLICON = 0x1;

        /// <summary>
        /// Extracts the associated file icon for a given file path using Shell32 P/Invoke.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        /// <returns>An ImageSource representing the file icon, or null if extraction fails.</returns>
        private static ImageSource? ExtractFileIcon(string filePath)
        {
            var shfi = new SHFILEINFO();
            var result = SHGetFileInfo(filePath, 0, ref shfi, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), SHGFI_ICON | SHGFI_SMALLICON);
            if (result == IntPtr.Zero || shfi.hIcon == IntPtr.Zero)
                return null;

            try
            {
                return Imaging.CreateBitmapSourceFromHIcon(shfi.hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                DestroyIcon(shfi.hIcon);
            }
        }

        #endregion

    }
}
