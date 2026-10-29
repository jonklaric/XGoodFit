using FrameworkInterfaces.Undo;
using FrameworkInterfaces.Undo.Actions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace FrameworkInterfaces
{
    /// <summary>
    /// Provides a base class for projects that can be stored, modified, and validated.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This abstract class provides common functionality for all projects including:
    /// <list type="bullet">
    /// <item><description>Name and description management</description></item>
    /// <item><description>Creation and modification date tracking</description></item>
    /// <item><description>Dirty state tracking for unsaved changes</description></item>
    /// <item><description>Validation support with error messaging</description></item>
    /// <item><description>Copy, save, open, and clear operations</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class ProjectBase : IProject
    {
        #region Fields

        /// <summary>
        /// The name of the project.
        /// </summary>
        protected string _name = string.Empty;

        /// <summary>
        /// The description of the project.
        /// </summary>
        protected string _description = string.Empty;

        /// <summary>
        /// The date when the project was created.
        /// </summary>
        protected DateTime _creationDate;

        /// <summary>
        /// The date when the project was last modified.
        /// </summary>
        protected DateTime _lastModified;

        /// <summary>
        /// Indicates whether the project is in a valid state.
        /// </summary>
        protected bool _isValid;

        /// <summary>
        /// The full project file name, including the directory path.
        /// </summary>
        protected string _fullFileName = string.Empty;

        /// <summary>
        /// The AvalonDock layout string.
        /// </summary>
        protected string _avalonDockLayout = string.Empty;

        /// <summary>
        /// The Project Explorer layout string.
        /// </summary>
        protected string _projectExplorerLayout = string.Empty;

        /// <summary>
        /// The undo manager for this project. Lazily initialized when first accessed.
        /// </summary>
        protected IUndoManager? _undoManager;

        /// <summary>
        /// Indicates whether undo recording is enabled for this project.
        /// </summary>
        protected bool _isUndoEnabled = true;

        /// <summary>
        /// Indicates whether a project is currently being opened.
        /// </summary>
        protected bool _openingProject = false;

        /// <summary>
        /// Indicates whether the window / project-explorer layout has been modified
        /// since the last save. Tracked independently from <see cref="IsDirty"/> so
        /// that layout changes can be persisted without stamping <c>LastModified</c>.
        /// </summary>
        protected bool _layoutDirty = false;

        /// <summary>
        /// The last-known-good <see cref="AvalonDockLayout"/> read from disk. Populated
        /// by derived-class <c>Open()</c> and consulted as a fallback when the current
        /// layout fails to deserialize.
        /// </summary>
        protected string _avalonDockLayoutPrevious = string.Empty;

        /// <summary>
        /// The last-known-good <see cref="ProjectExplorerLayout"/> read from disk.
        /// Populated by derived-class <c>Open()</c> and consulted as a fallback when the
        /// current layout fails to parse.
        /// </summary>
        protected string _projectExplorerLayoutPrevious = string.Empty;

        /// <summary>
        /// The read-only collection of element collections.
        /// </summary>
        protected ReadOnlyCollection<IElementCollection>? _readOnlyElementCollections;

        #endregion

        #region Abstract Properties

        /// <summary>
        /// Gets or sets the name of the project.
        /// </summary>
        /// <value>The project name.</value>
        public abstract string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the project.
        /// </summary>
        /// <value>The project description.</value>
        public abstract string Description { get; set; }

        /// <summary>
        /// Gets the version of the software the project was last edited with.
        /// </summary>
        /// <value>
        /// The version number of software.
        /// </value>
        public abstract string SoftwareVersion { get; }

        /// <summary>
        /// Gets the image representation of the project as an ImageSource.
        /// </summary>
        /// <value>The project's icon or image.</value>
        public abstract ImageSource ProjectImage { get; }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the name of the project as stored on disk.
        /// </summary>
        /// <value>The on-disk name of the project.</value>
        public string NameOnDisk { get; protected set; } = string.Empty;

        /// <inheritdoc/>
        public bool IsDirty { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether the window / project-explorer layout has
        /// been modified since the last save. This is orthogonal to <see cref="IsDirty"/>:
        /// model edits set <c>IsDirty</c>, layout changes set <c>LayoutDirty</c>. A save
        /// that runs only because <c>LayoutDirty</c> is set must not update
        /// <c>LastModified</c>.
        /// </summary>
        /// <value><c>true</c> if layout has been modified since the last save; otherwise, <c>false</c>.</value>
        public bool LayoutDirty
        {
            get { return _layoutDirty; }
            protected set
            {
                if (_layoutDirty != value)
                {
                    _layoutDirty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LayoutDirty)));
                }
            }
        }

        /// <summary>
        /// Gets the date and time when the project was created.
        /// </summary>
        /// <value>The creation date and time.</value>
        [Category("Meta Data"), DisplayName("Creation Date"), Description("The date and time when the project was first created."), Browsable(true)]
        public DateTime CreationDate 
        {
            get { return _creationDate; }
            protected set 
            {
                _creationDate = value;
            } 
        }

        /// <summary>
        /// Gets the date and time when the project was last modified.
        /// </summary>
        /// <value>The last modification date and time.</value>
        [Category("Meta Data"), DisplayName("Last Modified"), Description("The date and time when the project was last modified."), Browsable(true)]
        public DateTime LastModified
        {
            get { return _lastModified; }
            protected set
            {
                _lastModified = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LastModified)));
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("Full File Name"), Description("The full project file name, including the directory path."), Browsable(true)]
        public string FullFileName
        {
            get => _fullFileName;
            set
            {
                if (_fullFileName != value)
                {
                    _fullFileName = value;
                    RaisePropertyChange(nameof(FullFileName));
                }
            }
        }

        /// <inheritdoc/>
        [Category("Meta Data"), DisplayName("File Directory"), Description("The directory where the project file is located."), Browsable(true)]
        public string? FileDirectory => FullFileName != null ? Path.GetDirectoryName(FullFileName) : null;

        /// <inheritdoc/>
        public string AvalonDockLayout
        {
            get => _avalonDockLayout;
            set
            {
                if (_avalonDockLayout != value)
                {
                    _avalonDockLayout = value;
                    // Layout changes do not mark the project dirty (setDirty:false) —
                    // they only persist window state, not model data. LayoutDirty
                    // tracks them separately so that a layout-only save can skip
                    // LastModified stamping.
                    LayoutDirty = true;
                    RaisePropertyChange(nameof(AvalonDockLayout), false);
                }
            }
        }

        /// <inheritdoc/>
        public string ProjectExplorerLayout
        {
            get => _projectExplorerLayout;
            set
            {
                if (_projectExplorerLayout != value)
                {
                    _projectExplorerLayout = value;
                    LayoutDirty = true;
                    RaisePropertyChange(nameof(ProjectExplorerLayout), false);
                }
            }
        }

        /// <inheritdoc/>
        public string AvalonDockLayoutPrevious => _avalonDockLayoutPrevious;

        /// <inheritdoc/>
        public string ProjectExplorerLayoutPrevious => _projectExplorerLayoutPrevious;


        /// <summary>
        /// Gets or sets the read-only collection of element collections.
        /// </summary>
        /// <value>
        /// A read-only collection containing all element collections in the project.
        /// </value>
        public ReadOnlyCollection<IElementCollection>? ElementCollections
        {
            get => _readOnlyElementCollections;
            protected set => _readOnlyElementCollections = value;
        }

        /// <summary>
        /// Gets the undo manager for this project.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The undo manager is lazily initialized when first accessed. Each project
        /// has its own undo manager, enabling Visual Studio-style per-document undo
        /// where Ctrl+Z operates on the active document.
        /// </para>
        /// </remarks>
        public virtual IUndoManager UndoManager
        {
            get
            {
                if (_undoManager == null)
                {
                    Interlocked.CompareExchange(ref _undoManager, new UndoManager(), null);
                }
                return _undoManager;
            }
        }

        /// <summary>
        /// Gets or sets whether undo recording is enabled for this project.
        /// </summary>
        /// <remarks>
        /// Set to <c>false</c> to temporarily disable undo recording, for example
        /// during bulk operations, loading from disk, or undo/redo operations.
        /// Default is <c>true</c>.
        /// </remarks>
        public bool IsUndoEnabled
        {
            get { return _isUndoEnabled; }
            set { _isUndoEnabled = value; }
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs before the project is saved, allowing cancellation.
        /// </summary>
        public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

        /// <summary>
        /// Occurs after the project has been saved.
        /// </summary>
        public event ObjectSavedEventHandler? ObjectSaved;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Abstract Methods

        /// <inheritdoc/>
        public abstract bool IsValid();

        /// <inheritdoc/>
        public abstract void CreateNew(string newFullFileName);

        /// <inheritdoc/>
        public abstract void Open();

        /// <inheritdoc/>
        public abstract void Save();

        /// <inheritdoc/>
        public abstract void Close();

        /// <inheritdoc/>
        public abstract void Compact();

        /// <inheritdoc/>
        public abstract void Optimize();

        #endregion

        #region Protected Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event and optionally promotes the
        /// project to the dirty state.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <param name="setDirty">
        /// If <c>true</c>, marks the project dirty (sets <see cref="IsDirty"/> to <c>true</c>).
        /// If <c>false</c>, <see cref="IsDirty"/> is NOT modified and is left unchanged —
        /// the call only raises <see cref="PropertyChanged"/> for UI bindings.
        /// Clearing dirty is the explicit responsibility of <see cref="SetIsDirty"/>
        /// called from <c>Save()</c> / <c>Open()</c> / <c>CreateNew()</c> tails.
        /// Default is <c>true</c>.
        /// </param>
        /// <remarks>
        /// This method is provided for backwards compatibility. For undo support,
        /// use <see cref="RecordPropertyChange"/> instead.
        /// </remarks>
        protected void RaisePropertyChange(string propertyName, bool setDirty = true)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Only promote to dirty. Never clear — callers pass false when they want
            // to notify bindings without disturbing existing dirty state (e.g., layout
            // setters, or during Open() post-deserialize refreshes where SetIsDirty
            // has already been called directly to set the correct final state).
            if (setDirty)
                SetIsDirty(true);
        }

        /// <summary>
        /// Sets the IsDirty property value.
        /// </summary>
        /// <param name="value">The new dirty state.</param>
        protected void SetIsDirty(bool value)
        {
            if (IsDirty != value)
            {
                IsDirty = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty)));
            }
        }

        /// <summary>
        /// Records a property change for undo support and raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <param name="setDirty">
        /// If <c>true</c>, marks the project dirty (subject to the user-edit gate:
        /// <see cref="IsUndoEnabled"/> is <c>true</c> and the undo manager is not
        /// replaying an action). If <c>false</c>, <see cref="IsDirty"/> is NOT modified
        /// and is left unchanged. PropertyChanged still fires either way; undo recording
        /// is unaffected by this flag (it gates on <see cref="IsUndoEnabled"/> only).
        /// Default is <c>true</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// Call this method from property setters to enable undo/redo support.
        /// If <see cref="IsUndoEnabled"/> is <c>false</c> or the undo manager is currently
        /// executing an action (during undo/redo), the change will not be recorded.
        /// </para>
        /// <para>
        /// Example usage in a derived class:
        /// <code>
        /// public override string Name
        /// {
        ///     get => _name;
        ///     set
        ///     {
        ///         if (_name != value)
        ///         {
        ///             var oldValue = _name;
        ///             _name = value;
        ///             ValidateName();
        ///             RecordPropertyChange(nameof(Name), oldValue, value);
        ///         }
        ///     }
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        protected void RecordPropertyChange(string propertyName, object oldValue, object newValue, bool setDirty = true)
        {
            // Record the action for undo if enabled and not currently executing an undo/redo
            if (IsUndoEnabled && !UndoManager.IsExecutingAction && !Equals(oldValue, newValue))
            {
                var action = new PropertyChangeAction(this, propertyName, oldValue, newValue);
                UndoManager.RecordAction(action);
            }

            // Always raise the property changed event so UI bindings refresh.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            // Only promote to dirty, and only in a user-edit context. Open(), CreateNew(),
            // bulk operations, and undo/redo replay all disable undo recording precisely
            // because they are not user-initiated edits; in those paths the caller is
            // responsible for the final SetIsDirty(...) call. setDirty=false is also
            // a no-op on the flag — use it when the setter changes a notify-only property
            // whose edit must not mark the project dirty.
            if (setDirty && IsUndoEnabled && !UndoManager.IsExecutingAction)
                SetIsDirty(true);
        }

        /// <summary>
        /// Clears the undo history for this element.
        /// </summary>
        /// <remarks>
        /// Call this after loading data from disk or when you want to establish
        /// a clean state with no undo history.
        /// </remarks>
        protected void ClearUndoHistory()
        {
            _undoManager?.Clear();
        }

        /// <summary>
        /// Marks the current state as the saved state in the undo manager.
        /// </summary>
        /// <remarks>
        /// Call this after successfully saving the element. This updates the
        /// <see cref="IUndoManager.HasChangedSinceSave"/> property.
        /// </remarks>
        protected void MarkUndoSavePoint()
        {
            _undoManager?.MarkSavePoint();
        }

        /// <summary>
        /// Raises the <see cref="PreviewObjectSaved"/> event.
        /// </summary>
        /// <param name="sender">The object to be saved.</param>
        /// <param name="cancel">
        /// When this method returns, contains <c>true</c> if the save operation should be cancelled;
        /// otherwise, <c>false</c>.
        /// </param>
        protected void RaisePreviewObjectSaved(ISave sender, ref bool cancel)
        {
            PreviewObjectSaved?.Invoke(sender, ref cancel);
        }

        /// <summary>
        /// Raises the <see cref="ObjectSaved"/> event.
        /// </summary>
        /// <param name="sender">The object that was saved.</param>
        protected void RaiseObjectSaved(ISave sender)
        {
            ObjectSaved?.Invoke(sender);
        }

        /// <summary>
        /// When the element collections are saved, updates the Last Modified timestamp.
        /// </summary>
        /// <param name="sender">The object that was saved.</param>
        protected void ElementCollection_Saved(ISave sender)
        {
            LastModified = DateTime.Now;
        }

        /// <inheritdoc/>
        public void SaveAs(string newFullFileName)
        {
            // Wrap in try/catch so transient I/O errors (disk full, network drive, AV-locked file,
            // permission denied) surface with informative context instead of an opaque
            // "The process cannot access the file" propagating up from File.Copy.
            try
            {
                // Create a copy of the current project and rename it
                File.Copy(FullFileName, newFullFileName, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException(
                    $"Failed to save project as '{newFullFileName}'. Source: '{FullFileName}'. {ex.Message}",
                    ex);
            }
        }

        /// <inheritdoc/>
        public void ZipProject(string zipFileName)
        {
            try
            {
                if (File.Exists(zipFileName))
                {
                    File.Delete(zipFileName);
                }

                using (var archive = ZipFile.Open(zipFileName, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(FullFileName, Path.GetFileName(FullFileName));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                throw new IOException(
                    $"Failed to zip project to '{zipFileName}'. Source: '{FullFileName}'. {ex.Message}",
                    ex);
            }
        }

        #endregion

    }
}
