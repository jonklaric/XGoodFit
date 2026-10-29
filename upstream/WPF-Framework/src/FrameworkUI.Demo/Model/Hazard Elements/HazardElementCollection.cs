using DatabaseManager;
using FrameworkInterfaces;
using System;

namespace FrameworkUI.Demo
{
    /// <summary>
    /// Collection of hazard function elements for the demo project.
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
    public class HazardElementCollection : ElementCollectionBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="HazardElementCollection"/> class.
        /// </summary>
        /// <param name="parentProject">The parent project that contains this collection.</param>
        public HazardElementCollection(IProject parentProject) : base(parentProject) { }

        /// <summary>
        /// Gets the name of the collection. This is also the name of the SQLite table
        /// storing the hazard elements.
        /// </summary>
        public override string Name => "Hazard Functions";

        /// <summary>
        /// Tracks whether the loaded collection table contained blank or duplicate rows.
        /// </summary>
        private bool _needsTableCompaction;

        /// <summary>
        /// Saves all hazard elements in the collection to disk.
        /// </summary>
        /// <remarks>
        /// This method raises preview events for each element, allowing cancellation, and
        /// only saves elements that are marked as dirty.
        /// </remarks>
        public override void Save()
        {
            bool cancel = false;
            RaisePreviewObjectSaved(this, ref cancel);
            if (cancel == true) return;

            _savingAll = true;
            try
            {
                if (_needsTableCompaction)
                {
                    // The loaded table contained stale rows. Rewrite it from scratch so
                    // the on-disk table matches the in-memory collection exactly.
                    for (int i = 0; i < ElementList.Count; i++)
                    {
                        ((HazardElement)ElementList[i]).RaisePreviewSaved(ref cancel);
                        if (cancel == true) return;
                    }

                    var sqlite = new SQLiteManager(ParentProject.FullFileName);
                    sqlite.Open();
                    try
                    {
                        if (sqlite.TableNames.Contains(Name))
                        {
                            sqlite.DeleteTable(Name);
                        }
                    }
                    finally
                    {
                        if (sqlite.DataBaseOpen) sqlite.Close();
                    }

                    for (int i = 0; i < ElementList.Count; i++)
                    {
                        ElementList[i].Save();
                    }

                    _needsTableCompaction = false;
                }
                else
                {
                    // Save all elements.
                    for (int i = 0; i < ElementList.Count; i++)
                    {
                        // Always raise preview saved. This allows the plot settings to always be saved.
                        ((HazardElement)ElementList[i]).RaisePreviewSaved(ref cancel);
                        if (cancel == true) continue;
                        // Only save if dirty
                        if (ElementList[i].IsDirty)
                            ElementList[i].Save();
                    }
                }

                SetIsDirty(false);
                RaiseObjectSaved();
            }
            finally
            {
                _savingAll = false;
            }
        }

        /// <summary>
        /// Loads all hazard elements from disk into the collection.
        /// </summary>
        /// <remarks>
        /// This method reads the SQLite database table and creates a <see cref="HazardElement"/>
        /// instance for each row found.
        /// </remarks>
        public override void Open()
        {
            _opening = true;
            var sqlite = new SQLiteManager(ParentProject.FullFileName);
            sqlite.Open();
            try
            {
                if (sqlite.TableNames.Contains(Name) == true)
                {
                    var dtView = sqlite.GetTableManager(Name);
                    bool needsRewrite;
                    foreach (string elementName in CollectionPersistenceHelper.BuildSingleTableLoadEntries(dtView, out needsRewrite))
                    {
                        var element = new HazardElement(elementName, this, true);
                        Add(element);
                    }

                    _needsTableCompaction = needsRewrite;
                }
                sqlite.Close();
                SetIsDirty(_needsTableCompaction);
            }
            finally
            {
                if (sqlite.DataBaseOpen) sqlite.Close();
                _opening = false;
            }
        }

        /// <summary>
        /// Adds a hazard element to the collection.
        /// </summary>
        /// <param name="item">The element to add to the collection.</param>
        /// <remarks>
        /// This method subscribes to property change and deletion events, saves the element
        /// to disk if needed, and raises the ElementAdded event.
        /// </remarks>
        public override void Add(IElement item)
        {
            item.PropertyChanged += ElementPropertyChanged;
            item.Deleted += ElementDeleted;
            ElementList.Add((HazardElement)item);
            if (_opening == false)
            {
                var sqlite = new SQLiteManager(ParentProject.FullFileName);
                sqlite.Open();
                try
                {
                    if (CollectionPersistenceHelper.NamedRowExists(sqlite, Name, item.Name) == false)
                    {
                        item.Save();
                    }
                }
                finally
                {
                    if (sqlite.DataBaseOpen) sqlite.Close();
                }
                SetIsDirty(true);
            }
            RaiseElementAddedEvent(item);
        }

        /// <summary>
        /// Inserts a hazard element into the collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="item">The element to insert into the collection.</param>
        /// <remarks>
        /// This method subscribes to property change and deletion events, saves the element
        /// to disk if needed, and raises the ElementAdded event.
        /// </remarks>
        public override void Insert(int index, IElement item)
        {
            item.PropertyChanged += ElementPropertyChanged;
            item.Deleted += ElementDeleted;
            ElementList.Insert(index, (HazardElement)item);
            if (_opening == false)
            {
                var sqlite = new SQLiteManager(ParentProject.FullFileName);
                sqlite.Open();
                try
                {
                    if (CollectionPersistenceHelper.NamedRowExists(sqlite, Name, item.Name) == false)
                    {
                        item.Save();
                    }
                }
                finally
                {
                    if (sqlite.DataBaseOpen) sqlite.Close();
                }
                SetIsDirty(true);
            }
            RaiseElementAddedEvent(item);
        }

        /// <summary>
        /// Copies a hazard element from an external project and inserts it into this
        /// collection at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which the element should be inserted.</param>
        /// <param name="elementName">The name of the element to copy.</param>
        /// <param name="elementType">The fully qualified type name of the element.</param>
        /// <param name="fullFileName">The full file path to the external project database.</param>
        public override void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName)
        {
            var element = new HazardElement(elementName, this);
            element.Open(new SQLiteManager(fullFileName));
            Insert(index, element);
        }

        /// <summary>
        /// Deletes the entire hazard element collection and removes all associated data from disk.
        /// </summary>
        public override void Delete()
        {
            var sqlite = new SQLiteManager(ParentProject.FullFileName);
            sqlite.Open();
            try
            {
                sqlite.DeleteTable(Name);
            }
            finally
            {
                if (sqlite.DataBaseOpen) sqlite.Close();
            }
        }
    }
}
