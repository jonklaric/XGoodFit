# OxyPlot Controls

[<- Previous: Numeric Controls](numeric-controls.md) | [Back to Index](index.md) | [Next: Database Controls ->](database-controls.md)

## Overview

The OxyPlotControls library provides WPF controls for configuring and interacting with OxyPlot charts. It is built on top of a vendored OxyPlot fork maintained within the solution, giving full control over the charting engine and its serialization layer. The library includes a feature-rich toolbar for chart interaction, property editors for all chart elements, image export, theme integration, and XML serialization of plot configurations.

**Namespace:** `OxyPlotControls`

## Dependencies

| Dependency | Type | Purpose |
|---|---|---|
| OxyPlot | Project (vendored) | Core plotting engine |
| OxyPlot.Wpf | Project (vendored) | WPF PlotView control and serialization |
| GenericControls | Project | ColorPicker, NumericTextBox, property controls |
| Themes | Project | Framework theme colors and styles |

## OxyPlotToolbar

`OxyPlotToolbar` is a `UserControl` that attaches to an `OxyPlot.Wpf.Plot` and provides interactive charting tools. Custom cursors are held in a process-wide static cache, so each toolbar instance is cheap and no explicit disposal is required.

### Dependency Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `Plot` | `OxyPlot.Wpf.Plot` | `null` | The plot instance this toolbar controls |
| `IconSize` | `double` | `20.0` | Size of toolbar button icons |
| `ToolBarOrientation` | `Orientation` | `Vertical` | Layout direction of the toolbar |

### Key Features

- **Pan** -- click-and-drag panning with custom hand cursors
- **Zoom** -- zoom-in mode with a custom zoom cursor
- **Annotations** -- interactive creation of text, line, arrow, rectangle, ellipse, polygon, polyline, and point annotations directly on the chart
- **Point editing** -- select, move, and add data points on editable series with custom cursors
- **Export** -- save the chart as an image via `SavePlotImageDialog`
- **Properties panel** -- open `OxyPlotPropertiesControl` to edit chart settings

### Connecting the Toolbar to a Plot

```xml
<Grid>
    <oxyWpf:Plot x:Name="MyPlot">
        <oxyWpf:Plot.Axes>
            <oxyWpf:LinearAxis Position="Bottom" Title="X" />
            <oxyWpf:LinearAxis Position="Left" Title="Y" />
        </oxyWpf:Plot.Axes>
    </oxyWpf:Plot>

    <oxyControls:OxyPlotToolbar Plot="{Binding ElementName=MyPlot}"
                                ToolBarOrientation="Vertical"
                                HorizontalAlignment="Left"
                                VerticalAlignment="Top" />
</Grid>
```

### Theme Tracking

The toolbar subscribes to `ThemeService.Instance.ThemeChanged` on `Loaded` and unsubscribes on `Unloaded`. If the application theme changes while the toolbar is on an inactive tab, the `_lastAppliedTheme` field detects the mismatch on next load and reapplies the theme automatically.

### Disposal

`OxyPlotToolbar` loads custom cursors from embedded resources. Always dispose the toolbar when it is no longer needed, or rely on the automatic disposal triggered by the `Unloaded` event.

## Property Editors

All property editors accept an `OxyPlot.Wpf.Plot` via a `Plot` dependency property and provide direct two-way editing of plot settings.

### GeneralPlotControl

Edits general plot properties: title, subtitle, plot area styling (border color, thickness), and background settings. Includes value converters for OxyPlot's automatic color representation and default font size handling.

### AxesControl

Selector and editor container for plot axes. Provides a dropdown to choose an axis, then delegates to `AxisControl` for editing title, range, tick marks, gridlines, font, and positioning.

### SeriesControl / SeriesSelectorControl

`SeriesSelectorControl` lists all series on the plot. `SeriesControl` dynamically shows the appropriate sub-editor based on the selected series type:

- `LineSeriesControl` -- color, width, markers, dash style
- `ScatterSeriesControl` -- marker shape, size, and color
- `BarSeriesControl` -- fill color and label configuration
- `BoxPlotSeriesControl` -- whisker and outlier settings

### LegendControl

Edits legend properties including title, item styling, area background, border, and placement/position.

### AnnotationControl / AnnotationSelectorControl

`AnnotationSelectorControl` lists all annotations. `AnnotationControl` provides editors for text content, font, color, stroke, position, and display options across all annotation types (text, arrow, line, rectangle, ellipse, polygon, polyline, point).

### OxyPlotPropertiesControl

The aggregated property panel that brings all editors together in a single control. A combobox at the top selects between General, Legend, Axes, Series, and Annotations sections. Child controls are lazy-loaded on first access for faster initialization.

**Key dependency properties:**

| Property | Type | Description |
|---|---|---|
| `Plot` | `OxyPlot.Wpf.Plot` | The plot to edit |
| `ShowCloseButton` | `bool` | Whether the close button is visible (default `true`) |
| `BackButtonStyle` | `Style` | Style for the close button |
| `TabItemStyle` | `Style` | Style for tab items |
| `ExpanderStyle` | `Style` | Style for expander controls |
| `PropertyControlComboBoxStyle` | `Style` | Style for the section selector combobox |

**Events:**

- `ClosePropertiesCalled` -- raised when the user clicks the close button

**Programmatic navigation:**

```csharp
// Select the General settings section
propertiesControl.SelectGeneralSettings();

// Navigate to a specific section and optionally select an object
propertiesControl.ExpandProperty(OxyPlotPropertiesControl.PropertyEXP.Axes_Title, selectedAxis);
```

After deserializing plot settings, call `RefreshPlotBindings()` to force all child controls to re-resolve their bindings.

## SavePlotImageDialog

A dialog window for exporting charts as image files. Extends `MetroDialogWindow` from GenericControls.

**Supported formats:** PNG, PDF, SVG

**Features:**

- Preset and custom dimension selection
- Live WYSIWYG bitmap preview that scales to fit the dialog
- Report theme toggle -- exports with a white-background print-optimized theme
- Native Windows Save As dialog for file name, file type, folder, extension, and overwrite handling
- Persists last-used export folder path across invocations

```csharp
var dialog = new SavePlotImageDialog(myPlot);
dialog.Owner = this;
dialog.ShowDialog();
```

## Theme Integration

### OxyPlotTheme

A simple data class defining colors for all chart infrastructure elements: background, plot area, title, subtitle, axes (title, line, text, tick, gridlines), legend, and annotations. Series colors are never modified by themes.

### OxyPlotThemeManager

A static manager providing three built-in themes and methods to apply them:

| Theme | Description |
|---|---|
| `LightTheme` | White plot area, dark text. Used for Light and Blue app themes. |
| `DarkTheme` | Dark plot area, light text. |
| `ReportTheme` | White background with black text, optimized for printing. |

**Key methods:**

```csharp
// Get the OxyPlot theme for the current application theme
OxyPlotTheme theme = OxyPlotThemeManager.GetThemeFor(Themes.Theme.Dark);

// Apply a theme to a plot (sets both WPF wrapper and model-level properties)
OxyPlotThemeManager.ApplyTheme(plot, theme);

// Temporarily apply a theme for export, then restore original colors
OxyPlotThemeManager.WithThemedModel(plot.ActualModel, OxyPlotThemeManager.ReportTheme, model =>
{
    var exporter = new PngExporter { Width = 800, Height = 600 };
    exporter.Export(model, stream);
});

// Suppress the theme change confirmation dialog
OxyPlotThemeManager.SuppressThemeChangeWarning = true;
```

`ApplyTheme` suppresses `PropertyChanged` on all child elements during application to prevent `UndoableStateBridge` from recording theme-induced changes as user edits.

## Serialization

Plot configurations are serialized to XML using `PlotSerializer` in the vendored `OxyPlot.Wpf.Serialization` namespace. `OxyPlotSettingsSerializer` in this library is a thin pass-through wrapper.

```csharp
// Save plot settings to XML
XElement xml = OxyPlotSettingsSerializer.ToXelement(plot);

// Restore plot settings from XML
OxyPlotSettingsSerializer.FromXelement(plot, xml);

// After deserialization, refresh the properties panel bindings
propertiesControl.RefreshPlotBindings();
```

The XML structure contains sections for General properties, Legend, Axes, Annotations, and Series. The root element tag is available as `OxyPlotSettingsSerializer.OxyplotPropertiesTag`.

`PlotSerializer.PlotVisualProperties` exposes a read-only list of property names that are serialized, which consumers can use for change tracking.

## Code Example

```xml
<Window xmlns:oxyWpf="clr-namespace:OxyPlot.Wpf;assembly=OxyPlot.Wpf"
        xmlns:oxyControls="clr-namespace:OxyPlotControls;assembly=OxyPlotControls">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*" />
            <ColumnDefinition Width="Auto" />
        </Grid.ColumnDefinitions>

        <!-- Chart with toolbar overlay -->
        <Grid Grid.Column="0">
            <oxyWpf:Plot x:Name="MainPlot">
                <oxyWpf:Plot.Axes>
                    <oxyWpf:LinearAxis Position="Bottom" Title="Time (s)" />
                    <oxyWpf:LinearAxis Position="Left" Title="Discharge (cfs)" />
                </oxyWpf:Plot.Axes>
                <oxyWpf:Plot.Series>
                    <oxyWpf:LineSeries Title="Observed"
                                       ItemsSource="{Binding DataPoints}" />
                </oxyWpf:Plot.Series>
            </oxyWpf:Plot>

            <oxyControls:OxyPlotToolbar Plot="{Binding ElementName=MainPlot}"
                                        HorizontalAlignment="Left"
                                        VerticalAlignment="Top" />
        </Grid>

        <!-- Properties panel -->
        <oxyControls:OxyPlotPropertiesControl Grid.Column="1"
                                              Plot="{Binding ElementName=MainPlot}"
                                              Width="300" />
    </Grid>
</Window>
```

## Demo Application

The `OxyPlotControls.Demo` project demonstrates the toolbar, property editors, and theme integration in a standalone WPF application. Build and run it from the Demos solution folder.

## Best Practices

- **No toolbar disposal required** -- `OxyPlotToolbar` keeps its custom cursors in a static `CursorCache`, so individual toolbar instances do not own disposable resources. Theme-event subscriptions are released by the `Unloaded` handler, and Plot-side event subscriptions are released when the `Plot` dependency property is cleared or replaced.
- **Refresh bindings after deserialization** -- call `OxyPlotPropertiesControl.RefreshPlotBindings()` after `OxyPlotSettingsSerializer.FromXelement()` to ensure all property editors reflect the restored values.
- **Use `WithThemedModel` for export** -- when exporting to images or PDF, use `OxyPlotThemeManager.WithThemedModel` to temporarily apply the report theme without permanently modifying the plot.
- **Suppress property changes during theme application** -- set `Plot.SuppressPropertyChanged = true` before calling `ApplyTheme` to prevent undo/redo recording of theme changes. The toolbar handles this automatically.
- **Serialization lives in OxyPlot.Wpf** -- the actual serialization implementation is in `OxyPlot.Wpf.Serialization.PlotSerializer`, not in this project. `OxyPlotSettingsSerializer` is a convenience wrapper.
