using DatabaseManager;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;


namespace FrameworkUI.Demo
{
    /// <summary>
    /// Represents a demo project that showcases the FrameworkUI features.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [Category("Project")]
    [DisplayName("Demo Project")]
    [Description("The Demo project showcases a fully implemented hazard function element.")]
    [Browsable(true)]
    public class DemoProject : ProjectBase
    {
        #region Construction

        /// <summary>
        /// Lazy initialization for thread-safe singleton pattern.
        /// </summary>
        private static readonly Lazy<DemoProject> _lazyInstance = new Lazy<DemoProject>(() => new DemoProject());

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private DemoProject()
        {
            InitializeMessages();

            _hazardFunctions = new HazardElementCollection(this);

            _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(new IElementCollection[]
            { _hazardFunctions });

            SubscribeCollectionEvents();
        }

        /// <summary>
        /// Returns the singleton instance of the Project.
        /// </summary>
        /// <returns>The singleton Project instance.</returns>
        public static DemoProject GetInstance()
        {
            return _lazyInstance.Value;
        }

        /// <summary>
        /// Initializes the message items. Called after construction since they reference instance properties.
        /// </summary>
        private void InitializeMessages()
        {
            _descriptionMsg = new BasicMessageItem(MessageType.Message, "The project does not have a description.",
                this, "Project", Name, nameof(Description), "P-MSG-001");

            _noNameMsg = new BasicMessageItem(MessageType.Error, "The name of the project cannot be blank.",
                this, "Project", Name, nameof(Name), "P-ERR-001");

            _longNameMsg = new BasicMessageItem(MessageType.Error, "The name of the project cannot exceed 50 characters.",
                this, "Project", Name, nameof(Name), "P-ERR-002");

            _dupNameMsg = new BasicMessageItem(MessageType.Error, $"A project with the name '{Name}' already exists in this directory and must be unique.",
                this, "Project", Name, nameof(Name), "P-ERR-003");

            _badCharMsg = new BasicMessageItem(MessageType.Error, "Invalid character in project name.",
                this, "Project", Name, nameof(Name), "P-ERR-004");

            _messages.Clear();
            _messages.AddRange(new[] { _descriptionMsg, _noNameMsg, _longNameMsg, _dupNameMsg, _badCharMsg });
        }

        #endregion

        #region Members

        private static readonly char[] _InvalidNameCharacters = new List<char>(Path.GetInvalidFileNameChars()) { '\'', '[', ']' }.ToArray();
        private HazardElementCollection _hazardFunctions;
        private bool _nameValid = false;
        private bool _collectionEventsSubscribed;
        private List<BasicMessageItem> _messages = new List<BasicMessageItem>();
        private Messenger _messenger = Messenger.GetInstance();
        private BasicMessageItem _descriptionMsg;
        private BasicMessageItem _noNameMsg;
        private BasicMessageItem _longNameMsg;
        private BasicMessageItem _dupNameMsg;
        private BasicMessageItem _badCharMsg;


        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Name"), Description("The name of the project."), Browsable(true)]
        public override string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    var oldValue = _name;
                    _name = value;

                    // Reset messages with new name
                    foreach (var item in _messages)
                    {
                        item.SourceName = value;
                    }

                    ValidateName();

                    RecordPropertyChange(nameof(Name), oldValue, value);
                }
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Description"), Description("The description for the project."), Browsable(true)]
        public override string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    var oldValue = _description;
                    _description = value;

                    // Validate description message
                    if (string.IsNullOrEmpty(_description))
                    {
                        _messenger.Add(_descriptionMsg);
                    }
                    else
                    {
                        _messenger.Remove(_descriptionMsg);
                    }
                    RecordPropertyChange(nameof(Description), oldValue, value);
                }
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Software Version"), Description("The version of WPF Framework Demo used to last modify the project file."), Browsable(true)]
        public override string SoftwareVersion => "1.0.0";

        /// <summary>
        /// Lazy-loaded project icon. Returns null if the icon resource cannot be loaded
        /// so a missing resource degrades gracefully instead of crashing the explorer tree.
        /// </summary>
        private static readonly Lazy<ImageSource> s_projectIcon = new(() =>
        {
            try
            {
                var uri = new Uri("pack://application:,,,/FrameworkUI.Demo;component/Resources/FrameworkUIDemo.ico");
                var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                BitmapFrame bestFrame = null;
                foreach (var frame in decoder.Frames)
                {
                    if (bestFrame == null || frame.PixelWidth * frame.PixelHeight > bestFrame.PixelWidth * bestFrame.PixelHeight)
                    {
                        bestFrame = frame;
                    }
                }

                bestFrame?.Freeze();
                return bestFrame;
            }
            catch (Exception)
            {
                return null;
            }
        });

        /// <inheritdoc/>
        public override ImageSource ProjectImage => s_projectIcon.Value;

        /// <summary>
        /// Gets the array of invalid characters for project names.
        /// </summary>
        /// <value>
        /// An array of characters that are not allowed in project names,
        /// including invalid file name characters, apostrophe, and brackets.
        /// </value>
        public static char[] InvalidNameCharacters => _InvalidNameCharacters;


        #endregion

        #region Validation

        /// <summary>
        /// Validates the project name against all naming rules and updates validation messages.
        /// </summary>
        /// <remarks>
        /// Checks that the name is not blank, does not exceed 50 characters, and contains
        /// no invalid file name characters.
        /// </remarks>
        private void ValidateName()
        {
            _nameValid = true;

            // Check if name is nothing.
            if (string.IsNullOrEmpty(Name))
            {
                _nameValid = false;
                _messenger.Add(_noNameMsg);
            }
            else
            {
                _messenger.Remove(_noNameMsg);
            }

            // Check the length of the name.
            if (Name != null && Name.Length > 50)
            {
                _nameValid = false;
                _messenger.Add(_longNameMsg);
            }
            else
            {
                _messenger.Remove(_longNameMsg);
            }

            // Check if there are bad characters.
            _messenger.Remove(_badCharMsg);
            if (Name != null)
            {
                foreach (char badChar in DemoProject.InvalidNameCharacters)
                {
                    if (Name.Contains(badChar))
                    {
                        _nameValid = false;
                        string badCharacters = "<>:" + (char)34 + "/\\|?*";
                        _badCharMsg.Description = $"Invalid character in project name: '{badChar}'. Invalid characters are: {badCharacters}";
                        _messenger.Add(_badCharMsg);
                        break;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override bool IsValid()
        {
            if (!_nameValid) return false;
            return true;
        }

        #endregion

        #region Collection Events

        /// <summary>
        /// Subscribes to the per-collection events that drive project-level save cascades
        /// and dirty aggregation.
        /// </summary>
        private void SubscribeCollectionEvents()
        {
            if (_collectionEventsSubscribed) return;

            _hazardFunctions.ObjectSaved += ElementCollection_Saved;
            _hazardFunctions.ElementIsDirtyChanged += OnElementIsDirtyChanged;
            _hazardFunctions.PropertyChanged += OnCollectionPropertyChanged;
            _collectionEventsSubscribed = true;
        }

        /// <summary>
        /// Unsubscribes from every per-collection event subscribed by <see cref="SubscribeCollectionEvents"/>.
        /// </summary>
        private void UnsubscribeCollectionEvents()
        {
            if (!_collectionEventsSubscribed) return;

            _hazardFunctions.ObjectSaved -= ElementCollection_Saved;
            _hazardFunctions.ElementIsDirtyChanged -= OnElementIsDirtyChanged;
            _hazardFunctions.PropertyChanged -= OnCollectionPropertyChanged;
            _collectionEventsSubscribed = false;
        }

        /// <summary>
        /// Aggregates element-level dirty transitions into the project-level
        /// <see cref="ISave.IsDirty"/> flag so the Save button reflects unsaved child edits.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event arguments.</param>
        private void OnElementIsDirtyChanged(object sender, EventArgs e)
        {
            if (_openingProject) return;
            SetIsDirty(true);
        }

        /// <summary>
        /// Aggregates collection-level dirty transitions (structural changes such as
        /// add, remove, move, and sort) into the project-level <see cref="ISave.IsDirty"/> flag.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The property changed event arguments.</param>
        private void OnCollectionPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_openingProject) return;
            if (e.PropertyName != nameof(IElementCollection.IsDirty)) return;
            if (sender is IElementCollection collection && collection.IsDirty)
                SetIsDirty(true);
        }

        #endregion

        #region SQLite Persistence

        /// <summary>
        /// The name of the SQLite table storing the project meta data.
        /// </summary>
        private readonly string _tableName = "Project";

        /// <summary>
        /// Gets the required columns for the SQLite project table.
        /// </summary>
        /// <remarks>
        /// If you want to add a new column, add it to the end of the dictionary to maintain
        /// backward compatibility with existing project files.
        /// </remarks>
        private static Dictionary<string, Type> RequiredColumns { get; } = new Dictionary<string, Type>() {
            { nameof(Name), typeof(string) },
            { nameof(Description), typeof(string) },
            { nameof(CreationDate), typeof(string) },
            { nameof(LastModified), typeof(string) },
            { nameof(FullFileName), typeof(string) },
            { nameof(SoftwareVersion), typeof(string) },
            { nameof(AvalonDockLayout), typeof(string) },
            { nameof(ProjectExplorerLayout), typeof(string) } };

        /// <summary>
        /// Creates or updates the SQLite project table with required columns.
        /// </summary>
        /// <param name="sqlite">The SQLite database manager instance.</param>
        /// <remarks>
        /// If the table doesn't exist, it is created. If the table exists but is missing
        /// columns, the missing columns are added. This supports forward compatibility
        /// when opening older project files.
        /// </remarks>
        private void CreateTable(SQLiteManager sqlite)
        {
            if (sqlite.TableNames.Contains(_tableName) == false)
            {
                // If the table does not exist, then create the table
                var dataTable = new DataTable(_tableName);
                foreach (KeyValuePair<string, Type> column in RequiredColumns)
                    dataTable.Columns.Add(column.Key, column.Value);
                sqlite.SaveDataTable(dataTable);
            }
            else
            {
                // Add any required columns that don't exist
                var dt = sqlite.GetTableManager(_tableName);
                int columnIndex;
                foreach (KeyValuePair<string, Type> column in RequiredColumns)
                {
                    columnIndex = Array.IndexOf(dt.ColumnNames, column.Key);
                    // If the column doesn't exist in the database then create it.
                    if (columnIndex < 0)
                    {
                        dt.AddColumn(column.Key, column.Value);
                    }
                    else
                    {
                        if (dt.ColumnTypes[columnIndex] != column.Value)
                        {
                            dt.DeleteColumn(columnIndex);
                            dt.AddColumn(column.Key, column.Value);
                        }
                    }
                }
                dt.ApplyEdits();
            }
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override void CreateNew(string fullFileName)
        {
            IsUndoEnabled = false;
            try
            {
                // Set project meta data & properties
                FullFileName = fullFileName;
                Name = Path.GetFileNameWithoutExtension(fullFileName);
                Description = "";
                CreationDate = DateTime.Now;
                LastModified = DateTime.Now;

                // Load element collections.
                _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(new IElementCollection[]
                { _hazardFunctions });
                SubscribeCollectionEvents();

                Save();
            }
            finally
            {
                IsUndoEnabled = true;
                ClearUndoHistory();
            }
        }

        /// <summary>
        /// Creates a new dummy project.
        /// </summary>
        public void CreateNewDummyProject()
        {
            IsUndoEnabled = false;
            try
            {
                // Set project meta data and properties
                FullFileName = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString() + ShellPublicVariables.SoftwareExtension;
                Name = "Blank Project";
                Description = "This is a blank project file.";
                CreationDate = DateTime.Now;
                LastModified = DateTime.Now;

                // Load element collections.
                _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(new IElementCollection[]
                { _hazardFunctions });
                SubscribeCollectionEvents();

                Save();
            }
            finally
            {
                IsUndoEnabled = true;
                ClearUndoHistory();
            }
        }

        /// <inheritdoc/>
        public override void Open()
        {
            _openingProject = true;
            IsUndoEnabled = false;
            SetIsDirty(false);

            try
            {
                var sqlite = new SQLiteManager(FullFileName);
                sqlite.Open();

                // The user might have renamed the SQLite file from Windows Explorer.
                // In this case, the FullFileName will not match the meta data stored within
                // the database. Check if the values are different, and if so, update the
                // meta data table.
                string winExpName = Path.GetFileNameWithoutExtension(FullFileName);
                string winExpFullFileName = FullFileName;

                var dtView = sqlite.GetTableManager(_tableName);
                if (dtView.NumberOfRows != 1)
                {
                    sqlite.Close();
                }
                else
                {
                    // Get name and full file name using backing fields to avoid undo recording
                    if (dtView.ColumnNames.Contains(nameof(Name))) _name = dtView.GetCell(nameof(Name), 0).ToString();
                    if (dtView.ColumnNames.Contains(nameof(FullFileName))) _fullFileName = dtView.GetCell(nameof(FullFileName), 0).ToString();

                    // Reconcile a Windows Explorer rename with the stored meta data.
                    if (FullFileName != winExpFullFileName)
                    {
                        FullFileName = winExpFullFileName;
                        dtView.EditCell(0, nameof(FullFileName), winExpFullFileName);
                        dtView.ApplyEdits();
                    }
                    if (_name != winExpName)
                    {
                        _name = winExpName;
                        foreach (var item in _messages)
                            item.SourceName = _name;
                        ValidateName();
                        dtView.EditCell(0, nameof(Name), winExpName);
                        dtView.ApplyEdits();
                    }

                    // Get the rest of the properties using backing fields
                    if (dtView.ColumnNames.Contains(nameof(Description))) _description = dtView.GetCell(nameof(Description), 0).ToString();
                    if (dtView.ColumnNames.Contains(nameof(CreationDate))) CreationDate = FrameworkInterfaces.Utilities.Tools.DateFromString(dtView.GetCell(nameof(CreationDate), 0).ToString()) ?? DateTime.MinValue;
                    if (dtView.ColumnNames.Contains(nameof(LastModified))) LastModified = FrameworkInterfaces.Utilities.Tools.DateFromString(dtView.GetCell(nameof(LastModified), 0).ToString()) ?? DateTime.MinValue;
                    if (dtView.ColumnNames.Contains(nameof(AvalonDockLayout))) AvalonDockLayout = dtView.GetCell(nameof(AvalonDockLayout), 0).ToString();
                    if (dtView.ColumnNames.Contains(nameof(ProjectExplorerLayout))) _projectExplorerLayout = dtView.GetCell(nameof(ProjectExplorerLayout), 0).ToString();

                    // Opening from disk is not a layout edit; clear the flag that the
                    // AvalonDockLayout setter above may have just flipped.
                    LayoutDirty = false;

                    sqlite.Close();
                }

                // Validate description message
                if (string.IsNullOrEmpty(_description))
                    _messenger.Add(_descriptionMsg);
                else
                    _messenger.Remove(_descriptionMsg);

                // Open element collections.
                _hazardFunctions.Open();

                // Load element collections.
                _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(new IElementCollection[]
                { _hazardFunctions });
                SubscribeCollectionEvents();

                NameOnDisk = Name;

                // Raise PropertyChanged for properties loaded via backing fields
                // so that WPF bindings (e.g., ProjectNode header) update correctly.
                RaisePropertyChange(nameof(Name), false);
                RaisePropertyChange(nameof(Description), false);
            }
            finally
            {
                _openingProject = false;
                IsUndoEnabled = true;
                ClearUndoHistory();
                SetIsDirty(false);
            }
        }

        /// <inheritdoc/>
        public override void Close()
        {
            UnsubscribeCollectionEvents();
            _messenger.Clear(this);

            // Close all project element collections.
            _hazardFunctions.Clear();
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Two paths are possible:
        /// <list type="bullet">
        /// <item><description><b>Layout-only save</b> (<c>IsDirty == false</c> and <c>LayoutDirty == true</c>):
        /// only the two layout columns are rewritten and <c>LastModified</c> is not stamped, so
        /// window-layout changes persist without drifting the project's modification timestamp.</description></item>
        /// <item><description><b>Full save</b> (all other cases): project row, layout, and element
        /// collections are all written.</description></item>
        /// </list>
        /// </remarks>
        public override void Save()
        {
            bool cancel = false;
            RaisePreviewObjectSaved(this, ref cancel);
            if (cancel) return;

            // Must have a file name to save.
            if (FullFileName == null) return;

            bool layoutOnly = !IsDirty && LayoutDirty;

            // Filename-rename check is meaningful only when we are writing project meta.
            if (!layoutOnly)
            {
                string winExpName = Path.GetFileNameWithoutExtension(FullFileName);
                string winExpFullFileName = FullFileName;

                if (winExpName != Name && Name != "Blank Project")
                {
                    var newFullFileName = Path.Combine(FileDirectory ?? string.Empty, Name + ShellPublicVariables.SoftwareExtension);
                    try
                    {
                        File.Move(winExpFullFileName, newFullFileName);
                        FullFileName = newFullFileName;
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FileNotFoundException)
                    {
                        _messenger.Add(new BasicMessageItem(MessageType.Warning,
                            $"Failed to rename project file: {ex.Message}",
                            this, "DemoProject", Name, "Save"));
                        return;
                    }
                }
            }

            var sqlite = new SQLiteManager(FullFileName);
            sqlite.Open();
            try
            {
                if (layoutOnly)
                {
                    // Layout-only save: only persist the two layout columns.
                    // Do not stamp LastModified, do not iterate element collections.
                    CreateTable(sqlite);
                    var layoutView = sqlite.GetTableManager(_tableName);
                    if (layoutView.NumberOfRows == 0) layoutView.AddRow();
                    layoutView.EditCell(0, nameof(AvalonDockLayout), AvalonDockLayout ?? string.Empty);
                    layoutView.EditCell(0, nameof(ProjectExplorerLayout), ProjectExplorerLayout ?? string.Empty);
                    layoutView.ApplyEdits();
                    sqlite.Close();
                    LayoutDirty = false;
                    return;
                }

                // Only update LastModified if user data has actually changed at the
                // project level. Saves triggered solely by dirty children end up
                // stamping LastModified via the ElementCollection_Saved handler.
                if (IsDirty)
                    LastModified = DateTime.Now;
                CreateTable(sqlite);

                var dtView = sqlite.GetTableManager(_tableName);
                if (dtView.NumberOfRows == 0) dtView.AddRow();
                dtView.EditCell(0, nameof(Name), Name);
                dtView.EditCell(0, nameof(Description), Description);
                dtView.EditCell(0, nameof(CreationDate), FrameworkInterfaces.Utilities.Tools.DateToUniversalString(CreationDate));
                dtView.EditCell(0, nameof(LastModified), FrameworkInterfaces.Utilities.Tools.DateToUniversalString(LastModified));
                dtView.EditCell(0, nameof(FullFileName), FullFileName);
                dtView.EditCell(0, nameof(SoftwareVersion), SoftwareVersion);
                dtView.EditCell(0, nameof(AvalonDockLayout), AvalonDockLayout ?? string.Empty);
                dtView.EditCell(0, nameof(ProjectExplorerLayout), ProjectExplorerLayout ?? string.Empty);
                dtView.ApplyEdits();

                // Next, save element collections
                for (int i = 0; i < ElementCollections.Count; i++)
                {
                    ElementCollections[i].Save();
                }

                sqlite.Close();
                NameOnDisk = Name;
                SetIsDirty(false);
                LayoutDirty = false;
                MarkUndoSavePoint();
                RaiseObjectSaved(this);
            }
            finally
            {
                // Defensive close — handles the case where any of the dtView edits,
                // child collection saves, or post-success state changes throw.
                if (sqlite.DataBaseOpen) sqlite.Close();
            }
        }

        #region Compact and Optimize Project File

        /// <inheritdoc/>
        public override void Compact()
        {
            // A basic connection is set so the size of the "-journal" temp file can be obtained.
            var sqlite = new SQLiteManager(FullFileName);
            var connectionBuilder = new SQLiteConnectionStringBuilder
            {
                Version = 3,
                DataSource = FullFileName
            };
            sqlite.SetDatabaseConnection(connectionBuilder);
            sqlite.Vacuum();
        }

        /// <inheritdoc/>
        public override void Optimize()
        {
            var sqlite = new SQLiteManager(FullFileName);
            sqlite.Optimize();
        }

        #endregion

        #endregion
    }
}
