[<- Previous: Software Update](software-update.md) | [Back to Index](index.md)

# Migration Guide

## Overview

This guide covers the major migrations that have shaped the current version of WPF-Framework. Each section describes the changes, the required actions, and the reasoning behind the migration.

| Migration | Impact | Action Required |
|-----------|--------|-----------------|
| .NET 9.0 to .NET 10.0 | Target framework change | Update `TargetFramework` in all `.csproj` files |
| OxyPlot vendoring | External DLL to in-repo source | Replace OxyPlot NuGet/DLL references with project references |
| DAG integration | Separate repository merged in | Replace FlowGraph/FlowGraphUI references with DAG/DAGControls |
| AvalonDock .NET 9+ fix | Visual parent conflict | Already applied in-repo; verify custom templates |
| Themes library extraction | New standalone project | Add Themes project reference; update initialization code |
| `IMessageItem.ToText()` | Interface addition | Implement `ToText()` in custom `IMessageItem` classes |
| Project renames | Demo and test project names changed | Update build scripts and references |

---

## .NET 9.0 to .NET 10.0

All projects in the solution now target .NET 10.0. WPF projects use `net10.0-windows`; platform-agnostic libraries (OxyPlot core, DAG, ExpressionParser, DatabaseManager) use `net10.0`.

### Required Changes

Update every `.csproj` in your application:

```xml
<!-- Before -->
<TargetFramework>net9.0-windows</TargetFramework>

<!-- After -->
<TargetFramework>net10.0-windows</TargetFramework>
```

For non-WPF class libraries that do not require Windows-specific APIs:

```xml
<TargetFramework>net10.0</TargetFramework>
```

Install the .NET 10 SDK. No WPF API changes are required -- the migration is a target framework bump only.

---

## OxyPlot Vendoring

OxyPlot was previously consumed as a set of external DLLs built from a separate fork repository. It is now vendored directly into the solution at `src/OxyPlot/` with three projects:

| Project | Target | Location |
|---------|--------|----------|
| `OxyPlot` | `net10.0` | `src/OxyPlot/OxyPlot/` |
| `OxyPlot.Wpf` | `net10.0-windows` | `src/OxyPlot/OxyPlot.Wpf/` |
| `OxyPlot.Wpf.Shared` | `net10.0-windows` | `src/OxyPlot/OxyPlot.Wpf.Shared/` |

The vendored fork includes custom serialization classes in the `OxyPlot.Wpf.Serialization` namespace (`PlotSerializer`, `AxisSerializer`, `SeriesSerializer`, `AnnotationSerializer`, `SerializerExtensions`) that are not part of upstream OxyPlot.

### Required Changes

Replace any HintPath DLL references with project references:

```xml
<!-- Before -->
<Reference Include="OxyPlot">
  <HintPath>..\..\..\oxyplot\Source\OxyPlot\bin\Debug\net9.0\OxyPlot.dll</HintPath>
</Reference>
<Reference Include="OxyPlot.Wpf">
  <HintPath>..\..\..\oxyplot\Source\OxyPlot.Wpf\bin\Debug\net9.0-windows\OxyPlot.Wpf.dll</HintPath>
</Reference>

<!-- After -->
<ProjectReference Include="..\OxyPlot\OxyPlot\OxyPlot.csproj" />
<ProjectReference Include="..\OxyPlot\OxyPlot.Wpf\OxyPlot.Wpf.csproj" />
```

If your code referenced `OxyPlotSettingsSerializer` from `OxyPlotControls`, that class is now a thin pass-through wrapper. The actual serialization logic lives in `OxyPlot.Wpf.Serialization`. No namespace changes are required for consuming code -- `OxyPlotSettingsSerializer` still works as before.

The external OxyPlot fork repository is no longer needed and can be archived.

---

## DAG Integration

The DAG (directed acyclic graph) projects were previously maintained in a separate repository. They have been migrated into WPF-Framework with renamed namespaces:

| Old Repository | Old Namespace | New Project | New Namespace | Location |
|----------------|---------------|-------------|---------------|----------|
| `FlowGraph` | `FlowGraph` | `DAG` | `DAG` | `src/DAG/` |
| `FlowGraphUI` | `FlowGraphUI` | `DAGControls` | `DAGControls` | `src/DAGControls/` |
| `Test_FlowGraph` | -- | `DAG.Demo` | `DAG.Demo` | `src/DAG.Demo/` |
| `Test_DAG` | -- | `DAG.Tests` | `DAG.Tests` | `tests/DAG.Tests/` |

### Required Changes

Update all `using` directives:

```csharp
// Before
using FlowGraph;
using FlowGraphUI;

// After
using DAG;
using DAGControls;
```

Update XAML namespace declarations:

```xml
<!-- Before -->
xmlns:fg="clr-namespace:FlowGraphUI;assembly=FlowGraphUI"

<!-- After -->
xmlns:dag="clr-namespace:DAGControls;assembly=DAGControls"
```

Replace project or DLL references:

```xml
<!-- Before -->
<Reference Include="FlowGraph">
  <HintPath>..\..\..\DAG\FlowGraph\bin\Debug\FlowGraph.dll</HintPath>
</Reference>

<!-- After -->
<ProjectReference Include="..\DAG\DAG.csproj" />
<ProjectReference Include="..\DAGControls\DAGControls.csproj" />
```

The `FlowGraphCanvas` class name is preserved within the `DAGControls` namespace. References to `FlowGraphCanvas` do not need renaming beyond the namespace change.

The `DAG` project targets `net10.0` (no WPF dependency). The `DAGControls` project targets `net10.0-windows` and depends on `DAG`.

**Serialization note:** `NodeBase` serialization stores connections by connector index. If you have custom `NodeBase` subclasses, their deserialization constructors must recreate connectors in the same order as the original constructor to maintain correct index mapping.

---

## AvalonDock .NET 9+ Fix

.NET 9 introduced a change in WPF's visual tree management that causes `ContentPresenter` to throw visual parent conflict exceptions when used for `LayoutItem.View` bindings in AvalonDock templates. The fix replaces `ContentPresenter` with `ContentControl` in both the base AvalonDock theme and the VS2013 theme.

This fix is already applied in-repo at:
- `src/AvalonDock/Xceed.Wpf.AvalonDock/Themes/generic.xaml`
- `src/AvalonDock/Xceed.Wpf.AvalonDock.Themes.VS2013/Themes/Generic.xaml`

### Action Required

If you have custom AvalonDock control templates that bind to `LayoutItem.View`, apply the same replacement:

```xml
<!-- Before (.NET 8 and earlier) -->
<ContentPresenter Content="{Binding LayoutItem.View,
    RelativeSource={RelativeSource TemplatedParent}}" />

<!-- After (.NET 9+) -->
<ContentControl Content="{Binding LayoutItem.View,
    RelativeSource={RelativeSource TemplatedParent}}"
    Focusable="False" />
```

Both the base `generic.xaml` and the VS2013 `Generic.xaml` must be updated because the VS2013 theme overrides the base template.

---

## Themes Library Extraction

The `Themes` library (`src/Themes/`) is a standalone project that provides centralized theme management. It has no internal project dependencies and serves as the foundation for all theme-aware controls in the framework.

### Architecture

Theme management uses a two-layer system:

1. **`Themes.ThemeService`** (singleton) -- Manages application-level color dictionaries. Call `Initialize()` once at startup, then `SetTheme()` for runtime switching.
2. **`FrameworkUI.ThemeManager`** (static) -- Bridges to `ThemeService` and adds FrameworkUI-specific theme resources (MainWindow styles, MessageWindow styles). Call `ThemeManager.SetTheme()` instead of using `ThemeService` directly when using the full FrameworkUI shell.

### Initialization

If using `FrameworkUI`:

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Light);
}
```

`ThemeManager.SetTheme()` automatically initializes `ThemeService` on first call. The `ThemeColor` enum provides `Light`, `Blue`, and `Dark` values.

If using `Themes` without `FrameworkUI`:

```csharp
// App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    Themes.ThemeService.Instance.Initialize(Themes.Theme.Light);
}

// Runtime theme switching
Themes.ThemeService.Instance.SetTheme(Themes.Theme.Dark);
```

### Subscribing to Theme Changes

Subscribe to `ThemeService.Instance.ThemeChanged` to update components that need manual theme handling (such as OxyPlot chart colors):

```csharp
Themes.ThemeService.Instance.ThemeChanged += (sender, args) =>
{
    // args.OldTheme, args.NewTheme, args.ColorDictionary
    UpdateChartColors(args.NewTheme);
};
```

Or subscribe to the FrameworkUI-level event:

```csharp
FrameworkUI.ThemeManager.ThemeChanged += (newDictionary, newColor) =>
{
    // Handle theme change
};
```

### Required Changes

Add a project reference to `Themes` in any library that consumes theme colors:

```xml
<ProjectReference Include="..\Themes\Themes.csproj" />
```

Ensure `Themes.dll` is present in the output directory at runtime. Missing this DLL causes a `FileNotFoundException` at startup.

---

## IMessageItem.ToText()

The `IMessageItem` interface in `FrameworkInterfaces` now requires a `ToText()` method. If you have custom implementations of `IMessageItem`, add this method:

```csharp
public string ToText()
{
    return $"[{TimeStamp}] [{Type}] {Code}: {Description} (Source: {SourceName})";
}
```

The built-in `BasicMessageItem` class already implements `ToText()`. If your custom message classes inherit from `BasicMessageItem`, no action is needed.

---

## Renamed Projects

Several projects were renamed during the migration. Update any build scripts, CI configurations, or solution references accordingly.

| Old Name | New Name | Type |
|----------|----------|------|
| `Test_ProjectUI` / `Demo_FrameworkUI` | `FrameworkUI.Demo` | Demo app |
| `Demo_GenericControls` | `GenericControls.Demo` | Demo app |
| `Demo_NumericControls` | `NumericControls.Demo` | Demo app |
| `Demo_OxyPlotControls` | `OxyPlotControls.Demo` | Demo app |
| `Test_FlowGraph` | `DAG.Demo` | Demo app |
| `Test_DAG` | `DAG.Tests` | Test project |
| `Test_GenericControls` | `GenericControls.Tests` | Test project |
| `FlowGraph` | `DAG` | Library |
| `FlowGraphUI` | `DAGControls` | Controls library |

---

## Backward Compatibility

### Preserved APIs

The following APIs are unchanged and work as before:

- All `IProject`, `IElement`, `IElementCollection` interfaces and their base classes (`ElementBase`, `ElementCollectionBase`)
- `Messenger` singleton and all messaging methods
- `UndoManager`, `UndoableStateBridge`, `UndoableCollectionBridge`
- `ThemeManager.SetTheme()` method signature (the `ThemeColor` enum values are unchanged)
- `UserSettings` XML persistence (new properties use default values)
- All GenericControls (`NumericTextBox`, `CopyPasteDataGrid`, `ValidationDataGrid`, `ColorPicker`, etc.)
- All NumericControls (`DistributionSelectorControl`, `OrderedDataSelectorControl`, `TimeSeriesTable`, etc.)
- OxyPlotControls public API (consuming code compiles without changes)
- `OxyPlotSettingsSerializer` (now wraps `OxyPlot.Wpf.Serialization` but maintains the same public interface)

### DynamicResource in Freezables

A known WPF limitation affects theme-aware icons: `DynamicResource` does not work inside `DrawingBrush > DrawingGroup > GeometryDrawing.Brush` because Freezable objects exist outside the visual tree. Replace the `Viewbox > Rectangle > DrawingBrush` pattern with `Canvas > Path`:

```xml
<!-- Will NOT respond to theme changes -->
<DrawingBrush>
  <DrawingBrush.Drawing>
    <GeometryDrawing Brush="{DynamicResource SomeBrush}" ... />
  </DrawingBrush.Drawing>
</DrawingBrush>

<!-- Correct: Path is in the visual tree -->
<Canvas Width="16" Height="16">
  <Path Fill="{DynamicResource SomeBrush}" Data="M0,0 L16,16 ..." />
</Canvas>
```

### Floating Window Theme Resources

AvalonDock floating windows are separate Win32 `Window` instances that do not inherit from `MainWindow`'s visual tree. Theme dictionaries are added to `MainWindow.Resources.MergedDictionaries`. The `UpdateThemeResources()` method walks up to the parent `Window` via `Window.GetWindow(manager)` to find the theme dictionaries. Without this, floating windows fall back to `generic.xaml` which uses `SystemColors.ControlBrushKey`.

---

## Migration Checklist

Use this checklist when upgrading to the current version of WPF-Framework:

- [ ] Updated `TargetFramework` to `net10.0-windows` (or `net10.0` for non-WPF libraries)
- [ ] Installed .NET 10 SDK
- [ ] Replaced OxyPlot DLL/NuGet references with project references to `src/OxyPlot/`
- [ ] Replaced FlowGraph/FlowGraphUI references with DAG/DAGControls project references
- [ ] Updated `using FlowGraph` / `using FlowGraphUI` to `using DAG` / `using DAGControls`
- [ ] Updated XAML namespace prefixes for DAGControls
- [ ] Added `Themes` project reference to projects that consume theme resources
- [ ] Verified `ThemeService.Instance.Initialize()` or `ThemeManager.SetTheme()` is called before UI creation
- [ ] Implemented `ToText()` in any custom `IMessageItem` classes
- [ ] Updated build scripts for renamed demo and test projects
- [ ] Checked custom AvalonDock templates for `ContentPresenter` to `ContentControl` replacement
- [ ] Verified `DynamicResource` usage is not inside `DrawingBrush`/`GeometryDrawing` (use `Canvas > Path` instead)
- [ ] Tested with all three themes (Light, Blue, Dark)
- [ ] Clean build with no warnings
