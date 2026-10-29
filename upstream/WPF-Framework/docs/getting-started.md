[Back to Index](index.md) | [Next: Architecture ->](architecture.md)

# Getting Started

This guide walks through building a minimal WPF application using the WPF Framework. By the end, you will have a running application with a docking layout, project explorer, message window, and theme support.

## 1. Prerequisites

- **.NET 10.0 SDK** (net10.0-windows target)
- **Visual Studio 2022 17.12+** with the .NET desktop development workload
- **Windows 10** or later

## 2. Create a New WPF Project

Create a new WPF Application project targeting `net10.0-windows`. Enable `UseWPF` in the project file:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
</Project>
```

## 3. Add Framework References

For NuGet consumption, reference the Controls bundle. It brings in Core, Models, Support, and RMC.Numerics transitively:

```xml
<ItemGroup>
  <PackageReference Include="RMC.Wpf.Framework.Controls" Version="1.0.5" />
</ItemGroup>
```

For source development inside this repository, add `ProjectReference` entries for the framework libraries. At minimum, you need FrameworkInterfaces, FrameworkUI, and Themes:

```xml
<ItemGroup>
  <ProjectReference Include="..\FrameworkInterfaces\FrameworkInterfaces.csproj" />
  <ProjectReference Include="..\FrameworkUI\FrameworkUI.csproj" />
  <ProjectReference Include="..\Themes\Themes.csproj" />
  <ProjectReference Include="..\GenericControls\GenericControls.csproj" />
</ItemGroup>
```

Add additional control libraries as needed (NumericControls, OxyPlotControls, DatabaseControls, etc.).

## 4. Implement IProject

The `IProject` interface is the root data model for your application. It extends `IMetaData` (Name, Description, CreationDate, LastModified) and `ISave` (IsDirty, NameOnDisk, Open, Save). The framework provides `ProjectBase` as an abstract base class that implements the common plumbing.

Create a project class that extends `ProjectBase`:

```csharp
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FrameworkInterfaces;

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
```

Key points about `IProject` / `ProjectBase`:

- **`FullFileName`** -- the full path to the project file on disk.
- **`ElementCollections`** -- a `ReadOnlyCollection<IElementCollection>` containing all element collections. Each collection appears as a top-level node in the project explorer.
- **`AvalonDockLayout`** and **`ProjectExplorerLayout`** -- serialized layout strings that MainWindow persists automatically.
- **`ProjectImage`** -- an `ImageSource` used for the application icon.
- **`SaveAs(string)`** and **`ZipProject(string)`** are implemented by `ProjectBase` and do not need to be overridden.

## 5. Create an Element Class

Elements represent the individual items within a collection (e.g., a single analysis, a data set). Extend `ElementBase`, which requires implementing several abstract members:

```csharp
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FrameworkInterfaces;

public class MyElement : ElementBase
{
    public MyElement(string name, IElementCollection parentCollection)
        : base(name, parentCollection)
    {
        CreationDate = DateTime.Now;
        LastModified = DateTime.Now;
        SetIsDirty(false);
    }

    // Abstract properties from ElementBase
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

    public override DateTime CreationDate { get; }
    public override DateTime LastModified { get; }
    public override bool IsValid => true;
    public override string NameOnDisk { get; }
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

    public override IElement Copy(string? newName = null)
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
```

The `RecordPropertyChange` method in the property setters enables undo/redo. It captures the old and new values and pushes a `PropertyChangeAction` onto the element's `UndoManager`. Call `ClearUndoHistory()` after loading from disk to prevent undoing the load operation.

## 6. Create an Element Collection

Element collections manage a list of elements and appear as top-level nodes in the project explorer. Extend `ElementCollectionBase`:

```csharp
using FrameworkInterfaces;

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
```

## 7. Create a Project Controller

The project controller connects your data model to the UI. It extends `FrameworkUIController`, which itself extends `ProjectNode`. You must implement six abstract methods from `FrameworkUIController` plus one from `ProjectNode`:

```csharp
using System.Windows;
using System.Windows.Controls;
using FrameworkInterfaces;
using FrameworkUI;

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
```

The `FrameworkUIController` base constructor calls `Load()` (which builds the project explorer tree from `IProject.ElementCollections`) and then calls `DefineProjectMenuItems()`, `DefineToolsMenuItems()`, and `DefineHelpMenuItems()`. Override these virtual methods to add custom menu items to the application menu bar.

## 8. Set Up App.xaml

In `App.xaml`, load the FrameworkUI icon dictionary and any control library theme resources. Use `Startup` instead of `StartupUri`:

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Startup="Application_Startup">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="pack://application:,,,/FrameworkUI;component/Icons/IconDictionary.xaml"/>
                <ResourceDictionary Source="pack://application:,,,/GenericControls;component/Themes/GenericControlsTheme.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

## 9. Initialize the Application

In `App.xaml.cs`, initialize the theme before creating any UI, then create the project, controller, and main window:

```csharp
using System.Windows;
using FrameworkUI;

public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        // Initialize theme -- must be called before any UI is created
        ThemeManager.SetTheme(ThemeColor.Light);

        // Configure application metadata
        ShellPublicVariables.SoftwareExtension = ".myproj";

        // Create project model
        var project = MyProject.GetInstance();
        project.CreateNew(System.IO.Path.GetTempPath() + "Untitled.myproj");

        // Create controller (builds project explorer tree)
        var controller = new MyProjectController(project)
        {
            Style = (Style)FindResource("TreeViewItemStyle")
        };

        // Create and show main window
        var mainWindow = new MainWindow();
        mainWindow.ProjectNode = controller;
        mainWindow.Show();
    }
}
```

`ThemeManager.SetTheme()` performs three operations: it initializes `Themes.ThemeService` with the core color palette, loads FrameworkUI-specific XAML theme resources, and raises the `ThemeChanged` event. This must happen before `MainWindow` is constructed to ensure all theme resources are available.

## 10. Run Your Application

Build and run the project. You should see:

- A **Metro-style window** with a title bar, menu strip, and toolbar
- A **Project Explorer** panel on the left showing your element collections
- A **Message Window** panel at the bottom displaying validation messages
- A **Properties** panel on the right (initially empty)
- A **document area** in the center for opening element editors

Right-click items in the project explorer to access context menus. Use the Window menu to change themes at runtime.

## 11. Next Steps

- [Architecture](architecture.md) -- understand the full solution structure and dependency graph
- [Themes](themes.md) -- customize color palettes and handle theme changes in your controls
- [Undo/Redo](undo-redo.md) -- add `UndoableStateBridge` and `UndoableCollectionBridge` for automatic change tracking
- [Generic Controls](generic-controls.md) -- use NumericTextBox, ColorPicker, CopyPasteDataGrid
- [Numeric Controls](numeric-controls.md) -- add distribution selectors and curve editors
- [OxyPlot Controls](oxyplot-controls.md) -- integrate interactive charts with toolbar and property editing
- [Database Controls](database-controls.md) -- work with SQLite databases and expression parsing
- [DAG Controls](dag-controls.md) -- build visual node-based graph editors
- [Software Update](software-update.md) -- add automatic update checking from GitHub Releases

## 12. Common Issues

**Theme not applying to controls**
`ThemeManager.SetTheme()` must be called before any WPF UI element is instantiated. If controls appear unstyled, verify that the call is the first line in your `Application_Startup` method. Also ensure your `App.xaml` includes the required resource dictionaries (IconDictionary, GenericControlsTheme, etc.).

**Missing resources at runtime**
The FrameworkUI icon dictionary must be referenced in `App.xaml`:
```xml
<ResourceDictionary Source="pack://application:,,,/FrameworkUI;component/Icons/IconDictionary.xaml"/>
```
Without this, menu icons and toolbar images will be null.

**Undo not recording changes**
Property changes are only recorded when `IsUndoEnabled` is `true` (the default) and the `UndoManager` is not currently executing an action. Verify that your property setters call `RecordPropertyChange(propertyName, oldValue, newValue)` and not just `RaisePropertyChange(propertyName)`. After loading data from disk, call `ClearUndoHistory()` to prevent the load from appearing in the undo stack.

**Floating windows have wrong theme**
AvalonDock floating windows are separate Win32 windows and do not inherit from MainWindow's visual tree. Theme dictionaries are added to `Application.Current.Resources.MergedDictionaries` (not MainWindow), so floating windows pick them up automatically. If you add resources to `MainWindow.Resources` instead, floating windows will not see them.

**Element not appearing in project explorer**
Ensure your `IProject.ElementCollections` returns a `ReadOnlyCollection<IElementCollection>` that includes your collection. The `ProjectNode.Load()` method iterates this list to build the tree. If you modify the collection after construction, reassign `_readOnlyElementCollections`.
