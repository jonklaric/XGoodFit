using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GenericControls
{

    /// <summary>
    /// Manages a Most Recently Used (MRU) file list with menu integration and registry persistence.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Based on the solution from https://www.codeproject.com/Articles/23731/RecentFileList-a-WPF-MRU.
    /// Converted and modified to support MRU through Windows Registry.
    /// </para>
    /// </remarks>
    public class RecentFileList : Separator
    {
        /// <summary>
        /// Interface for persisting recent file lists to storage.
        /// </summary>
        public interface IPersist
        {
            /// <summary>
            /// Retrieves the list of recent files.
            /// </summary>
            /// <param name="max">Maximum number of files to retrieve.</param>
            /// <returns>List of recent file paths.</returns>
            List<string> RecentFiles(int max);

            /// <summary>
            /// Inserts a file into the recent files list.
            /// </summary>
            /// <param name="filepath">The file path to insert.</param>
            /// <param name="max">Maximum number of files to maintain.</param>
            void InsertFile(string filepath, int max);

            /// <summary>
            /// Removes a file from the recent files list.
            /// </summary>
            /// <param name="filepath">The file path to remove.</param>
            /// <param name="max">Maximum number of files to check.</param>
            void RemoveFile(string filepath, int max);
        }

        /// <summary>
        /// Gets/sets the persister implementation for saving and loading recent files.
        /// </summary>
        public IPersist Persister { get; set; }

        /// <summary>
        /// Configures the persister to use the Windows Registry.
        /// </summary>
        public void UseRegistryPersister()
        {
            Persister = new RegistryPersister();
        }

        /// <summary>
        /// Configures the persister to use the Windows Registry.
        /// </summary>
        public void UseRegistryPersister(string key)
        {
            Persister = new RegistryPersister(key);
        }

        /// <summary>
        /// Gets/sets the maximum number of recent files to track.
        /// </summary>
        public int MaxNumberOfFiles { get; set; }

        /// <summary>
        /// Gets/sets the maximum path length to display in menu items.
        /// </summary>
        public int MaxPathLength { get; set; }

        /// <summary>
        /// Gets/sets the file menu that will display recent file items.
        /// </summary>
        public MenuItem FileMenu { get; set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
        /// Default = "_{0}:  {2}"
        /// </summary>
        public string MenuItemFormatOneToNine { get; set; }

        /// <summary>
        /// Used in: String.Format( MenuItemFormat, index, filepath, displayPath );
        /// Default = "{0}:  {2}"
        /// </summary>
        public string MenuItemFormatTenPlus { get; set; }

        /// <summary>
        /// Delegate for customizing menu item text.
        /// </summary>
        /// <param name="index">The index of the menu item.</param>
        /// <param name="filepath">The file path.</param>
        /// <returns>The customized menu item text.</returns>
        public delegate string GetMenuItemTextDelegate(int index, string filepath);

        /// <summary>
        /// Gets/sets the delegate to customize the text shown in each menu item.
        /// </summary>
        public GetMenuItemTextDelegate GetMenuItemTextHandler { get; set; }

        /// <summary>
        /// Event raised when a recent file menu item is clicked.
        /// </summary>
        public event EventHandler<MenuClickEventArgs> MenuClick;

        private Separator _Separator = null;
        private List<RecentFile> _RecentFiles = null;

        /// <summary>
        /// Constructor creating the RecentFileList.
        /// </summary>
        public RecentFileList()
        {
            Persister = new RegistryPersister();

            MaxNumberOfFiles = 9;
            MaxPathLength = 50;
            MenuItemFormatOneToNine = "_{0}:  {2}";
            MenuItemFormatTenPlus = "{0}:  {2}";

            Loaded += HookFileMenu;
        }

        /// <summary>
        /// Event handler called when the file menu is loaded and attached to the parent control.
        /// </summary>
        /// <param name="s">The source object.</param>
        /// <param name="e">Event arguments.</param>
        /// <exception cref="ApplicationException">Thrown when parent is not a MenuItem.</exception>
        private void HookFileMenu(object s, RoutedEventArgs e)
        {
            MenuItem parentItem = Parent as MenuItem;
            if (parentItem == null)
                throw new ApplicationException("Parent must be a MenuItem");
            // 
            if (!(FileMenu == null) && FileMenu.Equals(parentItem))
                return;
            // 
            if (FileMenu is not null)
            {
                FileMenu.SubmenuOpened -= _FileMenu_SubmenuOpened;
            }
            // 
            FileMenu = parentItem;
            FileMenu.SubmenuOpened += _FileMenu_SubmenuOpened;
        }

        /// <summary>
        /// Gets the list of recent file paths form the persister.
        /// </summary>
        public List<string> RecentFiles
        {
            get
            {
                return Persister.RecentFiles(MaxNumberOfFiles);
            }
        }

        /// <summary>
        /// Removes the specified file path from the recent files list.
        /// </summary>
        /// <param name="filePath">The full file path to remove.</param>
        public void RemoveFile(string filePath)
        {
            Persister.RemoveFile(filePath, MaxNumberOfFiles);
        }

        /// <summary>
        /// Inserts a file into the recent files list.
        /// </summary>
        /// <param name="filePath">The full file path to remove.</param>
        public void InsertFile(string filePath)
        {
            Persister.InsertFile(filePath, MaxNumberOfFiles);
        }

        /// <summary>
        /// Event handler for when the file menu is opened; regenerates menu items.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void _FileMenu_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            SetMenuItems();
        }

        /// <summary>
        /// Updates the file menu with the most recent file menu items.
        /// </summary>
        private void SetMenuItems()
        {
            RemoveMenuItems();

            LoadRecentFiles();

            InsertMenuItems();
        }

        /// <summary>
        /// Removes all previously inserted menu items and separators from the file menu.
        /// </summary>
        private void RemoveMenuItems()
        {
            if (_Separator is not null)
            {
                FileMenu.Items.Remove(_Separator);
            }

            if (_RecentFiles is not null)
            {
                foreach (RecentFile r in _RecentFiles)
                {
                    if (r.MenuItem is not null)
                    {
                        FileMenu.Items.Remove(r.MenuItem);
                    }
                }
            }

            _Separator = null;
            _RecentFiles = null;
        }

        /// <summary>
        /// Inserts current recent files as menu items under the file menu.
        /// </summary>
        private void InsertMenuItems()
        {
            if (_RecentFiles is null)
            {
                return;
            }
            if (_RecentFiles.Count == 0)
            {
                return;
            }

            int iMenuItem = FileMenu.Items.IndexOf(this);
            foreach (RecentFile r in _RecentFiles)
            {
                string header = GetMenuItemText(r.Number, r.FilePath, r.DisplayPath);

                r.MenuItem = new MenuItem() { Header = header };
                r.MenuItem.Click += MenuItem_Click;

                FileMenu.Items.Insert(System.Threading.Interlocked.Increment(ref iMenuItem), r.MenuItem);
            }

            _Separator = new Separator();
            FileMenu.Items.Insert(System.Threading.Interlocked.Increment(ref iMenuItem), _Separator);
        }

        /// <summary>
        /// Generates a menu item header string for a recent file.
        /// </summary>
        /// <param name="index">The index of the item.</param>
        /// <param name="filepath">The full path of the file.</param>
        /// <param name="displaypath">The shortened path to display.</param>
        /// <returns>The formatted menu item string.</returns>
        private string GetMenuItemText(int index, string filepath, string displaypath)
        {
            var delegateGetMenuItemText = GetMenuItemTextHandler;
            if (delegateGetMenuItemText is not null)
            {
                return delegateGetMenuItemText(index, filepath);
            }

            string format = index < 10 ? MenuItemFormatOneToNine : MenuItemFormatTenPlus;

            string shortPath = ShortenPathname(displaypath, MaxPathLength);

            return string.Format(format, index, filepath, shortPath);
        }

        // This method is taken from Joe Woodbury's article at: http://www.codeproject.com/KB/cs/mrutoolstripmenu.aspx

        /// <summary>
        /// Shortens a pathname for display purposes.
        /// </summary>
        /// <param name="pathname">The pathname to shorten.</param>
        /// <param name="maxLength">The maximum number of characters to be displayed.</param>
        /// <remarks>Shortens a pathname by either removing consecutive components of a path
        /// and/or by removing characters from the end of the filename and replacing
        /// then with three elipses (...)
        /// <para>In all cases, the root of the passed path will be preserved in it's entirety.</para>
        /// <para>If a UNC path is used or the pathname and maxLength are particularly short,
        /// the resulting path may be longer than maxLength.</para>
        /// <para>This method expects fully resolved pathnames to be passed to it.
        /// (Use Path.GetFullPath() to obtain this.)</para>
        /// </remarks>
        /// <returns>The shortened pathname string.</returns>
        public static string ShortenPathname(string pathname, int maxLength)
        {
            if (pathname.Length <= maxLength)
            {
                return pathname;
            }

            string root = System.IO.Path.GetPathRoot(pathname);
            if (root.Length > 3)
            {
                root += System.IO.Path.DirectorySeparatorChar;
            }

            string[] elements = pathname.Substring(root.Length).Split(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar);

            int filenameIndex = elements.GetLength(0) - 1;

            if (elements.GetLength(0) == 1)
            {
                // pathname is just a root and filename
                if (elements[0].Length > 5)
                {
                    // long enough to shorten
                    // if path is a UNC path, root may be rather long
                    if (root.Length + 6 >= maxLength)
                    {
                        return root + elements[0].Substring(0, 3) + "...";
                    }
                    else
                    {
                        return pathname.Substring(0, maxLength - 3) + "...";
                    }
                }
            }
            else if (root.Length + 4 + elements[filenameIndex].Length > maxLength)
            {
                // pathname is just a root and filename
                root += @"...\";

                int len = elements[filenameIndex].Length;
                if (len < 6)
                {
                    return root + elements[filenameIndex];
                }

                if (root.Length + 6 >= maxLength)
                {
                    len = 3;
                }
                else
                {
                    len = maxLength - root.Length - 3;
                }
                return root + elements[filenameIndex].Substring(0, len) + "...";
            }
            else if (elements.GetLength(0) == 2)
            {
                return root + Convert.ToString(@"...\") + elements[1];
            }
            else
            {
                int len = 0;
                int begin = 0;

                for (int i = 0, loopTo = filenameIndex - 1; i <= loopTo; i++)
                {
                    if (elements[i].Length > len)
                    {
                        begin = i;
                        len = elements[i].Length;
                    }
                }

                int totalLength = pathname.Length - len + 3;
                int end = begin + 1;

                while (totalLength > maxLength)
                {
                    if (begin > 0)
                    {
                        totalLength -= elements[System.Threading.Interlocked.Decrement(ref begin)].Length - 1;
                    }

                    if (totalLength <= maxLength)
                    {
                        break;
                    }

                    if (end < filenameIndex)
                    {
                        totalLength -= elements[System.Threading.Interlocked.Increment(ref end)].Length - 1;
                    }

                    if (begin == 0 && end == filenameIndex)
                    {
                        break;
                    }
                }

                // assemble final string

                for (int i = 0, loopTo1 = begin - 1; i <= loopTo1; i++)
                    root += elements[i] + @"\";

                root += @"...\";

                for (int i = end, loopTo2 = filenameIndex - 1; i <= loopTo2; i++)
                    root += elements[i] + @"\";

                return root + elements[filenameIndex];
            }
            return pathname;
        }

        /// <summary>
        /// Loads recent files from the persistor into internal list.
        /// </summary>
        private void LoadRecentFiles()
        {
            _RecentFiles = LoadRecentFilesCore();
        }

        /// <summary>
        /// Builds the internal list of <see cref="RecentFile"/> objects from the persisted file paths.
        /// </summary>
        /// <returns>A list of recent file entries.</returns>
        private List<RecentFile> LoadRecentFilesCore()
        {
            var list = RecentFiles;

            var files = new List<RecentFile>(list.Count);

            int i = 0;
            foreach (string filepath in list)
                files.Add(new RecentFile(Math.Max(System.Threading.Interlocked.Increment(ref i), i - 1), filepath));

            return files;
        }

        /// <summary>
        /// Represents a recent file entry with display text and associated menu item. 
        /// </summary>
        private class RecentFile
        {
            public int Number = 0;
            public string FilePath = "";
            public MenuItem MenuItem = null;

            public string DisplayPath
            {
                get
                {
                    return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath), System.IO.Path.GetFileNameWithoutExtension(FilePath));
                }
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RecentFile"/> class.
            /// </summary>
            /// <param name="number">The numeric index of the file.</param>
            /// <param name="filePath">The full file path.</param>
            public RecentFile(int number, string filePath)
            {
                Number = number;
                FilePath = filePath;
            }
        }

        /// <summary>
        /// Custom EventArgs containing the file path for a clicked recent file menu item.
        /// </summary>
        public class MenuClickEventArgs : EventArgs
        {
            /// <summary>
            /// Gets the file path associated with the clicked menu item.
            /// </summary>
            public string FilePath
            {
                get
                {
                    return _filePath;
                }
                private set
                {
                    _filePath = value;
                }
            }
            private string _filePath;

            /// <summary>
            /// Initializes a new instance of the <see cref="MenuClickEventArgs"/> class.
            /// </summary>
            /// <param name="filePath">The file path associated with the event.</param>
            public MenuClickEventArgs(string filePath)
            {
                FilePath = filePath;
            }
        }

        /// <summary>
        /// Event handler for when a recent file menu item is clicked.
        /// </summary>
        /// <param name="sender">The menu item that was clicked.</param>
        /// <param name="e">Event arguments.</param>
        private void MenuItem_Click(object sender, EventArgs e)
        {
            MenuItem menuItem = sender as MenuItem;

            OnMenuClick(menuItem);
        }

        /// <summary>
        /// Raises the <see cref="MenuClick"/> event for the selected menu item.
        /// </summary>
        /// <param name="menuItem">The menu item that was clicked.</param>
        protected virtual void OnMenuClick(MenuItem menuItem)
        {
            string filepath = GetFilepath(menuItem);

            if (string.IsNullOrEmpty(filepath))
            {
                return;
            }

            MenuClick?.Invoke(menuItem, new MenuClickEventArgs(filepath));
        }

        /// <summary>
        /// Gets the full file path associated with given menu item. 
        /// </summary>
        /// <param name="menuItem">The menu item to look up.</param>
        /// <returns>The associated file path or empty string.</returns>
        private string GetFilepath(MenuItem menuItem)
        {
            foreach (RecentFile r in _RecentFiles)
            {
                if (r.MenuItem.Equals(menuItem))
                {
                    return r.FilePath;
                }
            }

            return string.Empty;
        }

        // -----------------------------------------------------------------------------------------
        /// <summary>
        /// Provides static access to common assembly attributes for the current application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        private sealed class ApplicationAttributes
        {
            /// <summary>
            /// Private constructor to prevent instantiation.
            /// </summary>
            private ApplicationAttributes()
            {
            }
            private static readonly Assembly _Assembly = null;

            private static readonly AssemblyTitleAttribute _Title = null;
            private static readonly AssemblyCompanyAttribute _Company = null;
            private static readonly AssemblyCopyrightAttribute _Copyright = null;
            private static readonly AssemblyProductAttribute _Product = null;

            /// <summary>
            /// Gets assembly title.
            /// </summary>
            public static string Title
            {
                get
                {
                    return m_Title;
                }
                private set
                {
                    m_Title = value;
                }
            }
            private static string m_Title;

            /// <summary>
            /// Gets the company name from the application metadata.
            /// </summary>
            public static string CompanyName
            {
                get
                {
                    return m_CompanyName;
                }
                private set
                {
                    m_CompanyName = value;
                }
            }
            private static string m_CompanyName;

            /// <summary>
            /// Gets the copyright string from the application metadata.
            /// </summary>
            public static string Copyright
            {
                get
                {
                    return m_Copyright;
                }
                private set
                {
                    m_Copyright = value;
                }
            }
            private static string m_Copyright;

            /// <summary>
            /// Gets the product name from the application metadata.
            /// </summary>
            public static string ProductName
            {
                get
                {
                    return m_ProductName;
                }
                private set
                {
                    m_ProductName = value;
                }
            }
            private static string m_ProductName;

            private static Version _Version = null;

            /// <summary>
            /// Gets the version string from the assembly.
            /// </summary>
            public static string Version
            {
                get
                {
                    return m_Version;
                }
                private set
                {
                    m_Version = value;
                }
            }
            private static string m_Version;

            /// <summary>
            /// Static constructor to intialize application attributes from the executing assembly.
            /// </summary>
            static ApplicationAttributes()
            {
                try
                {
                    Title = string.Empty;
                    CompanyName = string.Empty;
                    Copyright = string.Empty;
                    ProductName = string.Empty;
                    Version = string.Empty;

                    _Assembly = Assembly.GetEntryAssembly();

                    if (_Assembly is not null)
                    {
                        object[] attributes = _Assembly.GetCustomAttributes(false);

                        foreach (object attribute in attributes)
                        {
                            var @type = attribute.GetType();

                            if (type == typeof(AssemblyTitleAttribute))
                            {
                                _Title = (AssemblyTitleAttribute)attribute;
                            }
                            if (type == typeof(AssemblyCompanyAttribute))
                            {
                                _Company = (AssemblyCompanyAttribute)attribute;
                            }
                            if (type == typeof(AssemblyCopyrightAttribute))
                            {
                                _Copyright = (AssemblyCopyrightAttribute)attribute;
                            }
                            if (type == typeof(AssemblyProductAttribute))
                            {
                                _Product = (AssemblyProductAttribute)attribute;
                            }
                        }

                        _Version = _Assembly.GetName().Version;
                    }

                    if (_Title is not null)
                    {
                        Title = _Title.Title;
                    }
                    if (_Company is not null)
                    {
                        CompanyName = _Company.Company;
                    }
                    if (_Copyright is not null)
                    {
                        Copyright = _Copyright.Copyright;
                    }
                    if (_Product is not null)
                    {
                        ProductName = _Product.Product;
                    }
                    if (_Version is not null)
                    {
                        Version = _Version.ToString();
                    }
                }
                catch
                {
                }
            }
        }

        // -----------------------------------------------------------------------------------------
        /// <summary>
        /// Implements recent file list persistence using the Windows Registry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b> Authors: </b>
        /// <list type="bullet">
        ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
        /// </list>
        /// </para>
        /// </remarks>
        private class RegistryPersister : IPersist
        {
            /// <summary>
            /// Gets/sets the registry key used to store recent files.
            /// </summary>
            public string RegistryKey { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="RegistryPersister"/> class
            /// using the default registry path based on application metadata.
            /// </summary>
            public RegistryPersister()
            {
                RegistryKey = @"Software\" + ApplicationAttributes.CompanyName + @"\" + ApplicationAttributes.ProductName + @"\RecentFileList";
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RegistryPersister"/> class
            /// using the specified registry key path.
            /// </summary>
            /// <param name="key">The full registry key path to use.</param>
            public RegistryPersister(string key)
            {
                RegistryKey = key;
            }

            /// <summary>
            /// Returns the registry value key name for a given index.
            /// </summary>
            /// <param name="i">The index.</param>
            /// <returns>A string-formatted registry key name.</returns>
            private string Key(int i)
            {
                return i.ToString("00");
            }

            /// <summary>
            /// Retrieves the list of recent files from the registry.
            /// </summary>
            /// <param name="max">The maximum number of files to return.</param>
            /// <returns>A list of file paths.</returns>
            public List<string> RecentFiles(int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k is null)
                    k = Registry.CurrentUser.CreateSubKey(RegistryKey);
                // 
                var list = new List<string>(max);
                // 
                for (int i = 0, loopTo = max - 1; i <= loopTo; i++)
                {
                    string filename = (string)k.GetValue(Key(i));
                    if (string.IsNullOrEmpty(filename))
                        break;

                    list.Add(filename);
                }
                // 
                return list;
            }

            /// <summary>
            /// Inserts a file path at the top of the MRU list and shifts existing entries.
            /// </summary>
            /// <param name="filePath">The full file path to insert.</param>
            /// <param name="max">The maximum number of entries allowed.</param>
            public void InsertFile(string filePath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k is null)
                    Registry.CurrentUser.CreateSubKey(RegistryKey);
                // 
                k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                // 
                RemoveFile(filePath, max);
                // 
                for (int i = max - 2; i >= 0; i -= 1)
                {
                    string sThis = Key(i);
                    string sNext = Key(i + 1);
                    // 
                    var oThis = k.GetValue(sThis);
                    if (oThis is null)
                        continue;
                    // 
                    k.SetValue(sNext, oThis);
                }
                // 
                k.SetValue(Key(0), filePath);
            }

            /// <summary>
            /// Removes a file path from the MRU list.
            /// </summary>
            /// <param name="filePath">The full file path to remove.</param>
            /// <param name="max">The maximum number of entries to search.</param>
            public void RemoveFile(string filePath, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey);
                if (k is null)
                {
                    return;
                }

                for (int i = 0, loopTo = max - 1; i <= loopTo; i++)
                {
                again:
                    ;

                    string s = (string)k.GetValue(Key(i));
                    if (s is not null && s.Equals(filePath, StringComparison.CurrentCultureIgnoreCase))
                    {
                        RemoveFile(i, max);
                        goto again;
                    }
                }
            }

            /// <summary>
            /// Removes a file entry at a specific index and shifts others up.
            /// </summary>
            /// <param name="index">The index to remove.</param>
            /// <param name="max">The maximum number of entries.</param>
            private void RemoveFile(int index, int max)
            {
                var k = Registry.CurrentUser.OpenSubKey(RegistryKey, true);
                if (k is null)
                    return;

                k.DeleteValue(Key(index), false);

                for (int i = index, loopTo = max - 2; i <= loopTo; i++)
                {
                    string sThis = Key(i);
                    string sNext = Key(i + 1);

                    var oNext = k.GetValue(sNext);
                    if (oNext is null)
                    {
                        break;
                    }

                    k.SetValue(sThis, oNext);
                    k.DeleteValue(sNext);
                }
            }
        }
    }
}