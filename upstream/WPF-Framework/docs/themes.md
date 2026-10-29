# Theme System

[<- Previous: Architecture](architecture.md) | [Back to Index](index.md) | [Next: Undo/Redo ->](undo-redo.md)

## Overview

The WPF Framework theme system provides Light, Blue, and Dark VS2013-style themes for all standard WPF controls. It consists of two layers:

- **Themes library** (`Themes` namespace) -- The core engine. Manages a singleton `ThemeService`, loads control templates once at startup, and swaps color resource dictionaries at runtime. Any WPF project can reference this library directly.
- **ThemeManager** (`FrameworkUI` namespace) -- A bridge used by FrameworkUI applications. Wraps `ThemeService` and additionally loads FrameworkUI-specific resources (MainWindow styles, MessageWindow styles, etc.).

Both layers work together in a FrameworkUI application. If you are building a standalone library or control that does not depend on FrameworkUI, use `ThemeService` directly.

## Quick Start

### Initialize on startup

```csharp
// In App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Option A: Using FrameworkUI (most applications)
    ThemeManager.SetTheme(ThemeColor.Light);

    // Option B: Using Themes library directly (standalone)
    ThemeService.Instance.Initialize(Theme.Light);
}
```

### Switch at runtime

```csharp
// FrameworkUI applications
ThemeManager.SetTheme(ThemeColor.Dark);

// Standalone usage
ThemeService.Instance.SetTheme(Theme.Dark);
```

### Subscribe to changes

```csharp
// ThemeService event (available to all libraries)
ThemeService.Instance.ThemeChanged += (sender, e) =>
{
    Theme oldTheme = e.OldTheme;
    Theme newTheme = e.NewTheme;
    ResourceDictionary colorDictionary = e.ColorDictionary;
};

// ThemeManager event (FrameworkUI only)
ThemeManager.ThemeChanged += (newThemeDictionary, newThemeColor) =>
{
    // newThemeDictionary contains FrameworkUI-specific resources
    // newThemeColor is the ThemeColor enum value
};
```

## Available Themes

| Theme | `Theme` Enum | `ThemeColor` Enum | Description |
|-------|-------------|-------------------|-------------|
| Light | `Theme.Light` | `ThemeColor.Light` | White/light gray palette. Default theme. |
| Blue  | `Theme.Blue`  | `ThemeColor.Blue`  | Blue accent colors with a professional appearance. |
| Dark  | `Theme.Dark`  | `ThemeColor.Dark`  | Dark gray/black palette for low-light environments. |

## Architecture

The theme system uses a two-phase loading strategy:

1. **Initialization** -- `ThemeService.Initialize()` loads the control templates resource dictionary (`Merged.xaml`) into `Application.Current.Resources.MergedDictionaries`. This dictionary is loaded once and never swapped. All templates within it use `DynamicResource` bindings to reference color keys.

2. **Color swap** -- `ThemeService.SetTheme()` removes the current color dictionary and adds the new one (e.g., `DarkColors.xaml`) to `Application.Current.Resources.MergedDictionaries`. Because templates use `DynamicResource`, all controls update automatically.

```
Application.Resources.MergedDictionaries
  +-- Merged.xaml (control templates, loaded once)
  +-- LightColors.xaml  <-- swapped on theme change
```

**Why DynamicResource?** Unlike `StaticResource`, `DynamicResource` re-evaluates when the underlying resource changes. Since the color dictionaries are swapped at runtime, all bindings must be `DynamicResource` to pick up the new colors.

**Thread safety.** `ThemeService` is a thread-safe singleton (uses `Lazy<T>` with `LazyThreadSafetyMode.ExecutionAndPublication`). `SetTheme()` auto-marshals to the UI thread via `Dispatcher.Invoke` if called from a background thread. The `ThemeChanged` event is raised outside the internal lock to prevent deadlocks.

## Using Themes in Custom Controls

Reference color keys with `DynamicResource` in your XAML:

```xml
<UserControl x:Class="MyApp.MyControl"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Border Background="{DynamicResource EnvironmentWindowBackground}"
            BorderBrush="{DynamicResource Button.Static.Border}">
        <TextBlock Text="Hello"
                   Foreground="{DynamicResource EnvironmentWindowText}" />
    </Border>
</UserControl>
```

For buttons and interactive controls, use state-specific keys:

```xml
<Style TargetType="Button">
    <Setter Property="Background" Value="{DynamicResource Button.Static.Background}" />
    <Setter Property="BorderBrush" Value="{DynamicResource Button.Static.Border}" />
    <Style.Triggers>
        <Trigger Property="IsMouseOver" Value="True">
            <Setter Property="Background" Value="{DynamicResource Button.MouseOver.Background}" />
            <Setter Property="BorderBrush" Value="{DynamicResource Button.MouseOver.Border}" />
        </Trigger>
    </Style.Triggers>
</Style>
```

## Common Color Keys

Color keys are defined as string constants in `Themes.ColorKeys` and follow the naming convention `Control.State.Property`.

### Environment / General

| Key | Usage |
|-----|-------|
| `EnvironmentWindowBackground` | Main window background |
| `PlotWindowBackground` | Plot area background (lighter than window background in Dark theme) |
| `EnvironmentWindowText` | Main window text |
| `EnvironmentToolWindowText` | Tool window text |
| `EnvironmentToolWindowDisabledText` | Tool window disabled text |
| `EnvironmentToolTipBackground` | Tooltip background |
| `EnvironmentToolTipBorder` | Tooltip border |
| `EnvironmentToolTipText` | Tooltip text |

### Accent / Focus

| Key | Usage |
|-----|-------|
| `AccentColor` | Primary accent color |
| `FocusStyle.Brush` | Focus indicator color |

### Button

| Key | Usage |
|-----|-------|
| `Button.Static.Background` | Default background |
| `Button.Static.Border` | Default border |
| `Button.MouseOver.Background` | Hover background |
| `Button.MouseOver.Border` | Hover border |
| `Button.Pressed.Background` | Pressed background |
| `Button.Pressed.Border` | Pressed border |
| `Button.Disabled.Background` | Disabled background |
| `Button.Disabled.Border` | Disabled border |
| `Button.Disabled.Foreground` | Disabled text |

### TextBox

| Key | Usage |
|-----|-------|
| `TextBox.Static.Background` | Default background |
| `TextBox.Static.Border` | Default border |
| `TextBox.Static.Foreground` | Default text |
| `TextBox.MouseOver.Border` | Hover border |
| `TextBox.Focus.Border` | Focused border |
| `TextBox.Selection.Background` | Text selection highlight |
| `TextBox.Caret` | Caret color |
| `TextBox.Disabled.Background` | Disabled background |
| `TextBox.Disabled.Border` | Disabled border |
| `TextBox.Disabled.Foreground` | Disabled text |

### DataGrid

| Key | Usage |
|-----|-------|
| `DataGrid.Static.Background` | Grid background |
| `DataGrid.Static.Border` | Grid border |
| `DataGrid.Header.Background` | Column header background |
| `DataGrid.Header.Foreground` | Column header text |
| `DataGrid.Row.Background` | Row background |
| `DataGrid.Row.Alternating.Background` | Alternating row background |
| `DataGrid.Row.Foreground` | Row text |
| `DataGrid.Row.Selection.Background` | Selected row background |
| `DataGrid.Row.Selection.Foreground` | Selected row text |
| `DataGrid.Row.MouseOver.Background` | Hover row background |
| `DataGrid.GridLines` | Grid line color |

Additional key groups exist for CheckBox/RadioButton (`OptionMark.*`), ComboBox (`ComboBox.*`), Expander (`Expander.*`), Label (`Label.*`), ListView (`ListView.*`, `ListViewItem.*`), Menu (`MenuDefault*`, `MenuPopup*`), ScrollBar (`ScrollBar.*`), TabControl (`TabControl.*`, `TabItem.*`), TreeView (`TreeView.*`, `TreeViewItem.*`), Toolbar (`Toolbar*`), StatusBar (`StatusBar*`), GroupBox (`GroupBoxBorder`), Separator (`SeparatorBorder`), and StackPanel (`StackPanelBackground`). See `ColorKeys.cs` for the full list.

## Standalone Usage

To use the Themes library without FrameworkUI, reference the `Themes` project directly and call `ThemeService`:

```csharp
// Initialize in App.xaml.cs OnStartup
ThemeService.Instance.Initialize(Theme.Light);

// Switch themes
ThemeService.Instance.SetTheme(Theme.Dark);

// Query current theme
Theme current = ThemeService.Instance.CurrentTheme;
bool ready = ThemeService.Instance.IsInitialized;

// Parse theme from user settings
if (ThemeResourceHelper.TryParseTheme(savedThemeName, out Theme theme))
{
    ThemeService.Instance.SetTheme(theme);
}
```

`ThemeResourceHelper` provides utility methods:

| Method | Description |
|--------|-------------|
| `GetColorDictionaryUri(Theme)` | Returns the pack URI for a theme's color dictionary |
| `ParseTheme(string)` | Converts a theme name string to a `Theme` enum (throws on failure) |
| `TryParseTheme(string, out Theme)` | Safe parse that returns `false` on failure |

Pack URI constants are also available:

| Constant | Value |
|----------|-------|
| `ThemeResourceHelper.ControlTemplatesUri` | `pack://application:,,,/Themes;component/Resources/Controls/Merged.xaml` |
| `ThemeResourceHelper.LightColorsUri` | `pack://application:,,,/Themes;component/Resources/Colors/LightColors.xaml` |
| `ThemeResourceHelper.BlueColorsUri` | `pack://application:,,,/Themes;component/Resources/Colors/BlueColors.xaml` |
| `ThemeResourceHelper.DarkColorsUri` | `pack://application:,,,/Themes;component/Resources/Colors/DarkColors.xaml` |

## Styled Controls

The following WPF controls have themed templates in the Themes library:

| Control | Template File |
|---------|--------------|
| Button | `Button.xaml` |
| Calendar | `Calendar.xaml` |
| CheckBox | `CheckBox.xaml` |
| ComboBox | `ComboBox.xaml` |
| DataGrid | `DataGrid.xaml` |
| Expander | `Expander.xaml` |
| GridSplitter | `GridSplitter.xaml` |
| GroupBox | `GroupBox.xaml` |
| Label | `Label.xaml` |
| ListView | `ListView.xaml` |
| Menu | `Menu.xaml` |
| ProgressBar | `ProgressBar.xaml` |
| RadioButton | `RadioButton.xaml` |
| RichTextBox | `RichTextBox.xaml` |
| ScrollBar | `ScrollBar.xaml` |
| Separator | `Separator.xaml` |
| Slider | `Slider.xaml` |
| StackPanel | `StackPanel.xaml` |
| StatusBar | `StatusBar.xaml` |
| TabControl | `TabControl.xaml` |
| TextBlock | `TextBlock.xaml` |
| TextBox | `TextBox.xaml` |
| ToolBar | `ToolBar.xaml` |
| ToolTip | `ToolTip.xaml` |
| TreeView | `TreeView.xaml` |
| MetroWindow | `MetroWindowStyle.xaml` |

All templates are merged via `Merged.xaml` and loaded once during `ThemeService.Initialize()`.

## Important Gotchas

### AvalonDock floating windows are separate Win32 windows

AvalonDock floating windows are independent Win32 `Window` instances. They do not inherit from `MainWindow`'s visual tree, which means they do not automatically receive theme dictionaries added to `MainWindow.Resources.MergedDictionaries`.

FrameworkUI handles this by adding VS2013 theme dictionaries directly to `MainWindow.Resources.MergedDictionaries` (not `DockingManager.Resources`). The `DockingManager.Theme` property is not used. When theme changes affect floating windows, the `UpdateThemeResources()` method must walk up to the parent `Window`'s resources via `Window.GetWindow(manager)` to find the theme dictionaries.

Without this, floating windows fall back to the base `generic.xaml`, which uses `SystemColors.ControlBrushKey` and renders with the default light gray Windows appearance.

### DynamicResource does NOT work inside DrawingBrush

`DynamicResource` does not work inside `DrawingBrush > DrawingGroup > GeometryDrawing.Brush`. This is because `Freezable` objects (like `DrawingBrush`) exist outside the visual tree and cannot resolve dynamic resources.

**Do not do this:**
```xml
<!-- BROKEN: DynamicResource will not resolve -->
<Viewbox>
    <Rectangle>
        <Rectangle.Fill>
            <DrawingBrush>
                <DrawingBrush.Drawing>
                    <GeometryDrawing Brush="{DynamicResource EnvironmentWindowText}" ... />
                </DrawingBrush.Drawing>
            </DrawingBrush>
        </Rectangle.Fill>
    </Rectangle>
</Viewbox>
```

**Do this instead:**
```xml
<!-- CORRECT: Path is a visual element and resolves DynamicResource -->
<Viewbox>
    <Canvas Width="16" Height="16">
        <Path Fill="{DynamicResource EnvironmentWindowText}" Data="M0,0 L16,16 ..." />
    </Canvas>
</Viewbox>
```

### Global implicit TextBlock style overrides trigger-based Foreground

A global implicit `TextBlock` style (one without an `x:Key`) will override `TextBlock.Foreground` set via triggers on parent elements. This is a WPF precedence issue where implicit styles take priority over inherited values from triggers.

**Workaround:** Use named styles with an `x:Key` instead of global implicit styles for `TextBlock`:

```xml
<!-- PROBLEMATIC: Global implicit style overrides trigger-set Foreground -->
<Style TargetType="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource EnvironmentWindowText}" />
</Style>

<!-- CORRECT: Named style avoids precedence conflicts -->
<Style x:Key="BasicTextBlockStyle" TargetType="TextBlock">
    <Setter Property="Foreground" Value="{DynamicResource EnvironmentWindowText}" />
</Style>
```

## Best Practices

**DO:**
- Use `DynamicResource` for all theme color bindings in XAML.
- Use keys from `ColorKeys` constants in code-behind instead of hardcoded strings.
- Call `ThemeService.Instance.Initialize()` before any UI renders.
- Subscribe to `ThemeService.Instance.ThemeChanged` in libraries that need to respond to theme changes.
- Use `Canvas > Path` with `DynamicResource` for theme-aware vector icons.
- Use named styles (`x:Key`) for `TextBlock` styles to avoid precedence conflicts.

**DO NOT:**
- Use `StaticResource` for theme colors -- they will not update on theme switch.
- Use `DynamicResource` inside `DrawingBrush` or `GeometryDrawing` -- it will not resolve.
- Call `SetTheme()` before `Initialize()` -- it throws `InvalidOperationException`.
- Set `DockingManager.Theme` directly -- FrameworkUI manages AvalonDock themes via `MergedDictionaries`.
- Create global implicit `TextBlock` styles -- they interfere with trigger-based `Foreground` values.

## Troubleshooting

| Symptom | Cause | Fix |
|---------|-------|-----|
| Controls appear unstyled (default Windows look) | `ThemeService.Initialize()` not called | Call `Initialize()` in `App.OnStartup()` before showing any windows |
| Colors do not change on theme switch | Using `StaticResource` instead of `DynamicResource` | Replace with `DynamicResource` |
| Floating windows lose theme | Theme dictionaries not on the floating window's resources | Ensure FrameworkUI's `ThemeChanged` handler updates `MainWindow.Resources.MergedDictionaries` |
| Icon/drawing colors do not update | `DynamicResource` inside a `DrawingBrush` | Rewrite using `Canvas > Path` pattern |
| `InvalidOperationException` on `SetTheme()` | `Initialize()` was not called first | Call `Initialize()` during startup |
| `InvalidOperationException` on `Initialize()` | `Application.Current` is null | Ensure the `Application` object exists before initializing |
| TextBlock foreground stuck on one color | Global implicit `TextBlock` style overriding triggers | Use a named style with `x:Key` |
