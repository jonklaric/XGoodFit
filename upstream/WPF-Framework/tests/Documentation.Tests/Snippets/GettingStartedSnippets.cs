#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference
#pragma warning disable CS8603 // Possible null reference return
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
#pragma warning disable CS0414 // Field is assigned but its value is never used
#pragma warning disable CS0649 // Field is never assigned to

using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FrameworkInterfaces;
using FrameworkUI;

namespace Documentation.Tests.Snippets.GettingStarted
{
    // ---------------------------------------------------------------
    // Stub types referenced by the getting-started doc snippets
    // ---------------------------------------------------------------
    internal static class Properties
    {
        internal static class Resources
        {
            internal static ImageSource AppIcon => new BitmapImage();
            internal static ImageSource ElementIcon => new BitmapImage();
        }
    }

    // ---------------------------------------------------------------
    // Snippet: IProject implementation (Section 4)
    // ---------------------------------------------------------------
    public class MyProject : ProjectBase
    {
        private static readonly Lazy<MyProject> _lazyInstance = new(() => new MyProject());

        private MyElementCollection _elements;

        private MyProject()
        {
            _elements = new MyElementCollection(this);
            _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(
                new IElementCollection[] { _elements });
        }

        public static MyProject GetInstance() => _lazyInstance.Value;

        public override string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    var oldValue = _name;
                    _name = value;
                    RecordPropertyChange(nameof(Name), oldValue, value);
                }
            }
        }

        public override string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    var oldValue = _description;
                    _description = value;
                    RecordPropertyChange(nameof(Description), oldValue, value);
                }
            }
        }

        public override string SoftwareVersion => "1.0";
        public override ImageSource ProjectImage => Properties.Resources.AppIcon;

        public override void CreateNew(string fullFileName)
        {
            FullFileName = fullFileName;
            Name = Path.GetFileNameWithoutExtension(fullFileName);
            Description = "";
            CreationDate = DateTime.Now;
            LastModified = DateTime.Now;
            Save();
        }

        public override void Open()
        {
            IsUndoEnabled = false;
            // Load project data from disk
            _elements.Open();
            _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(
                new IElementCollection[] { _elements });
            IsUndoEnabled = true;
            ClearUndoHistory();
            SetIsDirty(false);
        }

        public override void Save()
        {
            bool cancel = false;
            RaisePreviewObjectSaved(this, ref cancel);
            if (cancel) return;

            _lastModified = DateTime.Now;

            for (int i = 0; i < ElementCollections.Count; i++)
                ElementCollections[i].Save();

            NameOnDisk = Name;
            MarkUndoSavePoint();
            SetIsDirty(false);
            RaiseObjectSaved(this);
        }

        public override void Close()
        {
            _elements.Clear();
        }

        public override bool IsValid() => true;
        public override void Compact() { }
        public override void Optimize() { }
    }

    // ---------------------------------------------------------------
    // Snippet: Element class (Section 5)
    // ---------------------------------------------------------------
    public class MyElement : ElementBase
    {
        public MyElement(string name, IElementCollection parentCollection)
            : base(name, parentCollection)
        {
            _creationDate = DateTime.Now;
            _lastModified = DateTime.Now;
            SetIsDirty(false);
        }

        // Abstract properties from ElementBase
        public override string Name
        {
            get => NameField;
            set
            {
                if (NameField != value)
                {
                    var oldValue = NameField;
                    NameField = value;
                    RecordPropertyChange(nameof(Name), oldValue, value);
                }
            }
        }

        public override string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    var oldValue = _description;
                    _description = value;
                    RecordPropertyChange(nameof(Description), oldValue, value);
                }
            }
        }

        public override DateTime CreationDate => _creationDate;
        public override DateTime LastModified => _lastModified;
        public override bool IsValid => true;
        public override string NameOnDisk => _nameOnDisk;
        public override ImageSource ElementImage => Properties.Resources.ElementIcon;
        public override bool CanCopyFromExternal => false;

        // Abstract methods from ElementBase
        public override void Open()
        {
            IsUndoEnabled = false;
            // Load element data from disk
            IsUndoEnabled = true;
            ClearUndoHistory();
            SetIsDirty(false);
        }

        public override void Save()
        {
            bool cancel = false;
            RaisePreviewObjectSaved(this, ref cancel);
            if (cancel) return;

            _lastModified = DateTime.Now;
            // Write element data to disk

            _nameOnDisk = Name;
            MarkUndoSavePoint();
            SetIsDirty(false);
            RaiseObjectSaved(this);
        }

        public override IElement Copy(string newName = null)
        {
            return new MyElement(newName ?? Name, ParentCollection);
        }

        public override IElement CopyFromExternal(string itemName, string fullFileName)
        {
            return null;
        }

        public override void Delete()
        {
            // Remove data from disk
            SetIsDirty(false);
            RaiseDeleted(this);
        }
    }

    // ---------------------------------------------------------------
    // Snippet: Element collection (Section 6)
    // ---------------------------------------------------------------
    public class MyElementCollection : ElementCollectionBase
    {
        public MyElementCollection(IProject parentProject) : base(parentProject) { }

        public override string Name => "My Elements";

        public override void Open()
        {
            _opening = true;
            // Read elements from disk and add to ElementList
            _opening = false;
        }

        public override void Add(IElement item)
        {
            item.Deleted += ElementDeleted;
            ElementList.Add(item);
            if (!_opening) SetIsDirty(true);
            RaiseElementAddedEvent(item);
        }

        public override void Insert(int index, IElement item)
        {
            item.Deleted += ElementDeleted;
            ElementList.Insert(index, item);
            SetIsDirty(true);
            RaiseElementAddedEvent(item);
        }

        public override void Delete()
        {
            // Remove collection data from disk
        }

        public override void InsertFromExternalProject(
            int index, string elementName, string elementType, string fullFileName)
        {
            Insert(index, new MyElement(elementName, this));
        }
    }

    // ---------------------------------------------------------------
    // Snippet: Project controller (Section 7)
    // ---------------------------------------------------------------
    public class MyProjectController : FrameworkUIController
    {
        public MyProjectController(IProject project) : base(project) { }

        public override bool CanMultiSelect => false;

        // Required by ProjectNode -- defines context menus for project explorer nodes
        protected override void DefineProjectExplorerMenuItems()
        {
            // Add custom context menu items to element node collections
        }

        // Required by FrameworkUIController -- 6 abstract methods:

        public override Control GetDocumentControl(IElement element)
        {
            // Return the editor control for the given element
            return new ContentControl { Content = new TextBlock { Text = element.Name } };
        }

        public override void DocumentClosed(UIElement documentControl)
        {
            // Clean up when a document tab is closed
        }

        public override void PropertiesClosed(UIElement propertiesControl)
        {
            // Clean up when a properties panel is closed
        }

        public override Control GetPropertiesControl(UIElement documentControl)
        {
            // Return a properties panel for the active document
            return null;
        }

        public override Control GetPropertiesControl(IElement element)
        {
            // Return a properties panel for a selected element
            return null;
        }

        public override IElement GetControlElement(UIElement control)
        {
            // Extract the IElement from a document or properties control
            return null;
        }
    }

    // ---------------------------------------------------------------
    // Snippet: Application initialization (Section 9)
    // This verifies the code compiles but won't be executed at runtime.
    // ---------------------------------------------------------------
    public class AppStartupSnippet
    {
        public void Snippet_ApplicationStartup()
        {
            // The snippet from the doc references these APIs -- verify they compile:
            // ThemeManager.SetTheme(ThemeColor.Light);
            ThemeColor tc = ThemeColor.Light;

            // ShellPublicVariables.SoftwareExtension = ".myproj";
            ShellPublicVariables.SoftwareExtension = ".myproj";

            // var project = MyProject.GetInstance();
            var project = MyProject.GetInstance();

            // var controller = new MyProjectController(project);
            // controller.Style would need a ResourceDictionary at runtime
            var controller = new MyProjectController(project);

            // var mainWindow = new MainWindow();
            // mainWindow.ProjectNode = controller;
            // mainWindow.Show();
            // NOTE: MainWindow construction requires WPF Application context,
            // but this compiles to verify the API surface.
            FrameworkUI.MainWindow mw = null;
            FrameworkUIController fwc = controller;
            // Verify the ProjectNode property accepts FrameworkUIController
            if (mw != null)
            {
                mw.ProjectNode = fwc;
            }
        }
    }
}
