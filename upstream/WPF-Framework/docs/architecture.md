[<- Previous: Getting Started](getting-started.md) | [Back to Index](index.md) | [Next: Themes ->](themes.md)

# Architecture

This document describes the solution structure, dependency graph, design patterns, and key architectural decisions in the WPF Framework.

## 1. Solution Overview

The solution contains 43 projects organized around package boundaries, demos, and tests:

| Folder | Projects | Purpose |
|--------|----------|---------|
| **Core** | FrameworkInterfaces, Themes | Contracts and theme system |
| **Controls** | FrameworkUI, GenericControls, NumericControls, OxyPlotControls, DatabaseControls, ExpressionParserControls, DAGControls, Xceed.Wpf.AvalonDock, Xceed.Wpf.AvalonDock.Themes.VS2013 | Application shell, reusable WPF controls, and docking UI |
| **Models** | DatabaseManager, ExpressionParser, OxyPlot, OxyPlot.Wpf, OxyPlot.Wpf.Shared, DAG | Non-UI logic and vendored forks |
| **Support** | SoftwareUpdate, SoftwareUpdate.Updater | Auto-update from GitHub Releases |
| **Demos** | FrameworkUI.Demo, GenericControls.Demo, NumericControls.Demo, OxyPlotControls.Demo, DatabaseControls.Demo, ExpressionParserControls.Demo, DAG.Demo | Standalone demo applications |
| **Tests** | OxyPlot.ExampleLibrary + 13 test projects | Example chart models and unit/integration tests |
| **Packaging** | RMC.Wpf.Framework.Core, RMC.Wpf.Framework.Models, RMC.Wpf.Framework.Support, RMC.Wpf.Framework.Controls | NuGet bundle projects |

## 2. Dependency Diagram

The following diagram shows the dependency relationships between the main library projects. Arrows point from dependent to dependency.

```
                          External
                        +-----------+
                        |RMC.Numerics|
                        +-----+-----+
                              |
          +-------------------+-------------------+
          |                                       |
          v                                       v
  +----------------+                     +------------------+
  | NumericControls|                     | DatabaseControls |
  +-------+--------+                     +--------+---------+
          |                                       |
          |  +-----------------+                  |  +----------------------+
          +->| OxyPlotControls |                  +->| ExpressionParser-    |
          |  +--------+--------+                  |  | Controls             |
          |           |                           |  +----------+-----------+
          |           +-> OxyPlot.Wpf             |             |
          |           |    +-> OxyPlot            |             +-> ExpressionParser
          |           |                           |
          |           +-> GenericControls         +-> DatabaseManager
          |           |    +-> Themes             |
          |           |                           +-> GenericControls
          +-----------)---------------------------+
          |           |                           +-> OxyPlotControls
          v           v                           |
  +----------------+  |                           +-> Themes
  | GenericControls|<-+
  +-------+--------+
          |
          v
  +-------+--------+
  |     Themes     |  (leaf -- no internal dependencies)
  +----------------+

  +------------------+      +-----+
  |FrameworkInterfaces| <--- | DAG |  (both are leaf dependencies)
  +--------+---------+      +--+--+
           |                    |
           v                    v
  +--------+---------+  +------+------+
  |   FrameworkUI    |  | DAGControls |
  +--------+---------+  +-------------+
           |
           +-> FrameworkInterfaces
           +-> Themes
           +-> Xceed.Wpf.AvalonDock
           +-> Xceed.Wpf.AvalonDock.Themes.VS2013
```

**Leaf dependencies** (no internal project references): FrameworkInterfaces, Themes, DAG, ExpressionParser, OxyPlot.

## 2.1 NuGet Package Map

The packaging projects in `src/Packaging/` create four bundles. Packages should follow this dependency order:

| Package | Included assemblies/content | Package dependencies |
|---------|-----------------------------|----------------------|
| `RMC.Wpf.Framework.Core` | FrameworkInterfaces, Themes | None |
| `RMC.Wpf.Framework.Models` | DAG, DatabaseManager, ExpressionParser, OxyPlot, OxyPlot.Wpf, OxyPlot.Wpf.Shared | ClosedXML, DocumentFormat.OpenXml, ExcelNumberFormat, FastMember, SourceGear.sqlite3, System.Data.SQLite |
| `RMC.Wpf.Framework.Support` | SoftwareUpdate plus SoftwareUpdate.Updater content files | None |
| `RMC.Wpf.Framework.Controls` | FrameworkUI, GenericControls, NumericControls, OxyPlotControls, DatabaseControls, ExpressionParserControls, DAGControls, Xceed.Wpf.AvalonDock, Xceed.Wpf.AvalonDock.Themes.VS2013 | `RMC.Wpf.Framework.Core`, `RMC.Wpf.Framework.Models`, `RMC.Wpf.Framework.Support`, `RMC.Numerics` 2.1.4+ |

AvalonDock is a vendored UI dependency and is packaged with `RMC.Wpf.Framework.Controls`, not Core. `RMC.Numerics` is consumed as a NuGet package through central package management and should not be referenced through local `HintPath` DLLs. Source builds restore `RMC.Numerics` 2.1.4, while packages declare a compatible `[2.1.4,3.0.0)` dependency range.

## 3. Core Framework

### FrameworkInterfaces

The lowest-level project. Defines all contracts and base classes with zero internal dependencies.

| Type | Purpose |
|------|---------|
| `IProject` | Root data model. Extends `IMetaData` and `ISave`. Owns `ElementCollections`, `AvalonDockLayout`, `ProjectImage`. |
| `ProjectBase` | Abstract base implementing `IProject` with undo support, dirty tracking, `SaveAs`, and `ZipProject`. |
| `IElement` | Single project item. Properties: `DisplayName`, `ParentCollection`, `ElementImage`, `IsValid`, `CanCopyFromExternal`. Methods: `Copy`, `CopyFromExternal`, `Delete`. |
| `ElementBase` | Abstract base implementing `IElement` with per-element `UndoManager`, `RecordPropertyChange`, `ValidateName`, and dirty state tracking. |
| `IElementCollection` | `IList<IElement>` with `ParentProject`, `ElementAdded`/`ElementRemoved` events, `Sort`, `MoveElement`, `InsertFromExternalProject`. |
| `ElementCollectionBase` | Abstract base implementing `IElementCollection` with undo-aware `RecordAddElement`, `RecordRemoveElement`, `RecordMoveElement`. |
| `IMetaData` | Name, Description, CreationDate, LastModified. |
| `ISave` | IsDirty, NameOnDisk, Open, Save, PreviewObjectSaved, ObjectSaved. Extends `INotifyPropertyChanged`. |
| `Messenger` | Singleton message hub. Messages keyed by (source, code) to prevent duplicates. Thread-safe lazy initialization. |
| `UndoManager` | Per-document undo/redo stacks (100 max levels). Supports transactions (`BeginTransaction`) and action merging. |
| `UndoableStateBridge` | Monitors `INotifyPropertyChanged` via reflection, auto-creates `PropertyChangeAction` records with shadow values. |
| `UndoableCollectionBridge<T>` | Monitors `INotifyCollectionChanged`, maintains shadow copy for Reset events, uses dynamic dispatch. |

### FrameworkUI

The application shell. Depends on FrameworkInterfaces, Themes, and AvalonDock.

| Type | Purpose |
|------|---------|
| `MainWindow` | Metro-style window with AvalonDock layout, project explorer, message window, properties panel, menu bar, toolbar, and recent files. `ProjectNode` dependency property accepts a `FrameworkUIController`. |
| `FrameworkUIController` | Abstract class extending `ProjectNode`. Six abstract methods: `GetDocumentControl`, `DocumentClosed`, `PropertiesClosed`, `GetPropertiesControl(UIElement)`, `GetPropertiesControl(IElement)`, `GetControlElement`. Three virtual menu methods: `DefineProjectMenuItems`, `DefineToolsMenuItems`, `DefineHelpMenuItems`. |
| `ProjectNode` | Abstract class extending `Node`. Builds the project explorer tree from `IProject.ElementCollections`. Abstract method: `DefineProjectExplorerMenuItems`. Virtual method: `Load`. |
| `ThemeManager` | Static bridge to `Themes.ThemeService`. `SetTheme(ThemeColor)` initializes ThemeService, loads FrameworkUI-specific XAML resources, raises `ThemeChanged`. |
| `ThemeColor` | Enum: `Blue`, `Dark`, `Light`. |
| `UserSettings` | Static properties persisted to XML. Categories: General, File Management, Message Window, Defaults. |
| `ShellPublicVariables` | Static configuration: `SoftwareName`, `SoftwareExtension`, `SoftwareVersionDate`. |

### Themes

Standalone theme resource library with no internal dependencies.

| Type | Purpose |
|------|---------|
| `ThemeService` | Singleton. `Initialize(Theme)` loads color dictionaries from `Resources/Colors/`. `SetTheme(Theme)` swaps palettes at runtime. Auto-marshals to UI thread. |
| `Theme` | Enum: `Blue`, `Dark`, `Light`. |
| Color XAML files | `BlueColors.xaml`, `DarkColors.xaml`, `LightColors.xaml` in `Resources/Colors/`. Define `SolidColorBrush` resources using `x:Key` names consumed via `DynamicResource`. |

## 4. Control Libraries

| Library | Key Controls | Dependencies |
|---------|-------------|--------------|
| **GenericControls** | `NumericTextBox`, `NameTextBox`, `ColorPicker`, `CopyPasteDataGrid`, `MessageBox`, `NameDialog`, `ExplorerTreeView` | Themes |
| **NumericControls** | `DistributionSelector`, `ParameterControl`, `ProbabilityCurveEditor`, `TimeSeriesDataGrid` | GenericControls, OxyPlotControls, Themes, RMC.Numerics |
| **OxyPlotControls** | `OxyPlotToolbar`, `OxyPlotPropertiesControl`, `PlotSeriesEditor`, `AxisPropertiesControl` | GenericControls, Themes, OxyPlot, OxyPlot.Wpf |
| **DatabaseControls** | `DatabaseTableViewer`, `FieldCalculator`, `ExpressionEditorControl` | DatabaseManager, ExpressionParser, ExpressionParserControls, GenericControls, OxyPlotControls, Themes, RMC.Numerics |
| **ExpressionParserControls** | `ExpressionEditorControl`, `FunctionBrowser`, `SyntaxHighlighter` | ExpressionParser, GenericControls, Themes |
| **DAGControls** | `FlowGraphCanvas`, `NodeView`, `ConnectorView`, `ConnectionView` | DAG |

## 5. Model Libraries

| Library | Purpose |
|---------|---------|
| **DatabaseManager** | SQLite database abstraction. Table creation, queries, field management. Wraps `System.Data.SQLite`. |
| **ExpressionParser** | Mathematical expression parsing and evaluation. Variables, functions, operators. |
| **OxyPlot** | Vendored fork of [oxyplot/oxyplot](https://github.com/oxyplot/oxyplot). Core plotting model: `PlotModel`, `Series`, `Axis`, `Annotation`. No WPF dependency. |
| **OxyPlot.Wpf** | WPF rendering for OxyPlot. `Plot` control, `PlotView`. Custom serialization in `OxyPlot.Wpf.Serialization` namespace (`PlotSerializer`, `AxisSerializer`, `SerializerExtensions`). |
| **OxyPlot.Wpf.Shared** | Shared source between OxyPlot.Wpf targets. |
| **DAG** | Directed acyclic graph model. `NodeBase`, `Connector`, `Connection`. Serialization stores connections by connector index. |

## 6. Support Libraries

| Library | Purpose |
|---------|---------|
| **SoftwareUpdate** | `IUpdateService` interface, `GitHubUpdateService`, `UpdateOptions`, `SemanticVersion`. Checks GitHub Releases for newer versions, downloads and extracts update assets. |
| **SoftwareUpdate.Updater** | Standalone executable that applies updates. Waits for the parent process to exit, replaces files, and restarts the application. |

### Vendored UI Libraries

| Library | Purpose |
|---------|---------|
| **Xceed.Wpf.AvalonDock** | Modified fork of Xceed AvalonDock. Key modifications: `ContentPresenter` replaced with `ContentControl` for `LayoutItem.View` bindings (.NET 9+ fix), last `LayoutDocumentPane` protection in `CreateFloatingWindowCore`. |
| **Xceed.Wpf.AvalonDock.Themes.VS2013** | Visual Studio 2013 theme for AvalonDock. Blue, Dark, and Light variants. Uses `DynamicResource` bindings to the Themes color keys. |

## 7. Design Patterns

### Singleton

- **`Messenger`** -- `Messenger.GetInstance()` with lazy thread-safe initialization. Single application-wide message hub for validation errors, warnings, and status messages.
- **`ThemeService`** -- `ThemeService.Instance` provides centralized theme state. Initializes once, subsequent `SetTheme` calls swap resources.

### MVC

The framework follows a Model-View-Controller pattern:
- **Model**: `IProject` / `ProjectBase`, `IElement` / `ElementBase`, `IElementCollection` / `ElementCollectionBase`
- **View**: `MainWindow` (application shell with AvalonDock layout)
- **Controller**: `FrameworkUIController` (maps elements to document/properties controls, defines menus)

### Command

- **`IUndoableAction`** -- encapsulates a reversible operation with `Execute` and `Undo` methods.
- **`UndoManager`** -- maintains undo/redo stacks, executes actions, supports transactions via `BeginTransaction`.
- **`PropertyChangeAction`** -- records a property value change. Supports 500ms merge window for rapid successive edits to the same property.
- **`CompositeAction`** -- groups multiple actions from a transaction into a single undoable unit.

### Observer

- **`INotifyPropertyChanged`** -- used throughout for data binding. `ISave` extends it directly.
- **`UndoableStateBridge`** -- subscribes to `PropertyChanged` events and automatically records undo actions.
- **`UndoableCollectionBridge<T>`** -- subscribes to `CollectionChanged` events for observable collections.

## 8. Vendored Dependencies

### AvalonDock

The solution includes a modified fork of [Xceed AvalonDock](https://github.com/xceedsoftware/wpftoolkit) in `src/AvalonDock/`. Two projects: the core layout engine and the VS2013 theme.

Key modifications from upstream:
- **`ContentPresenter` to `ContentControl`**: In both `generic.xaml` (base) and `Themes/Generic.xaml` (VS2013), `ContentPresenter` elements used for `LayoutItem.View` bindings were replaced with `ContentControl` to resolve visual parent conflicts in .NET 9+.
- **Last DocumentPane protection**: `CreateFloatingWindowCore()` has a while loop that removes empty parent containers. A guard was added to protect the last remaining `LayoutDocumentPane`, and `GetVisibility()` was updated to return true for the last empty pane.
- **Theme resource loading**: Floating windows are separate Win32 windows outside MainWindow's visual tree. Theme dictionaries are added to `Application.Current.Resources.MergedDictionaries` so floating windows inherit them.

### OxyPlot

The solution includes a vendored fork of [oxyplot/oxyplot](https://github.com/oxyplot/oxyplot) in `src/OxyPlot/`. Three projects: OxyPlot (core model), OxyPlot.Wpf (WPF rendering), and OxyPlot.Wpf.Shared.

Key additions beyond upstream:
- **Custom serialization**: `PlotSerializer`, `AxisSerializer`, and `SerializerExtensions` in the `OxyPlot.Wpf.Serialization` namespace provide XML-based plot configuration persistence. Methods include `GeneralPropertiesToXElement`, `LegendPropertiesToXElement`, `AxisToXElement`, `XElementToAxis`, `ToPrettyText`, and `FromPrettyDataText`.

## 9. External Dependencies

| Dependency | Source | Purpose |
|-----------|--------|---------|
| **RMC.Numerics** | [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) NuGet package, 2.1.4 or later compatible 2.x | Statistical distributions, parameter estimation, bootstrap analysis. Required by NumericControls and DatabaseControls. |

All other dependencies are either vendored into the solution (AvalonDock, OxyPlot) or available as NuGet packages (System.Data.SQLite, DocumentFormat.OpenXml, etc.).

## 10. Threading

The WPF Framework follows standard WPF threading rules with a few framework-specific considerations:

- **UI thread only**: All WPF controls, `MainWindow`, `FrameworkUIController`, and the project explorer must be accessed from the UI thread.
- **`ThemeService` auto-marshaling**: `ThemeService.SetTheme()` automatically dispatches to the UI thread if called from a background thread.
- **`UndoManager` is UI-thread only**: `RecordAction`, `Undo`, and `Redo` must be called on the UI thread. The `UndoableStateBridge` and `UndoableCollectionBridge` subscribe to property/collection changed events that fire on the thread that modified the property -- ensure model changes happen on the UI thread if undo is enabled.
- **`Messenger` thread safety**: `Messenger.GetInstance()` uses lazy thread-safe initialization. However, `MessageWindowControl` uses `Dispatcher.Invoke` to marshal message additions to the UI thread, so `Messenger.Add()` can be called from any thread.
- **Long-running operations**: Disable undo recording (`IsUndoEnabled = false`) before background computation, then re-enable and raise property changes on the UI thread when complete.
