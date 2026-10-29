# WPF Framework Documentation

The WPF Framework is a .NET 10.0 desktop application framework developed by the U.S. Army Corps of Engineers, Risk Management Center (USACE-RMC). It provides a complete application shell with AvalonDock-based docking layout, VS2013-style theming, per-document undo/redo, and a suite of specialized controls for scientific and engineering applications.

## Documentation

| Guide | Description |
|-------|-------------|
| [Gallery](gallery.md) | Screenshot guide to the framework control libraries |
| [Getting Started](getting-started.md) | Step-by-step guide to building your first application |
| [Architecture](architecture.md) | Solution structure, dependencies, and design patterns |
| [Themes](themes.md) | Runtime theme switching with Light, Dark, and Blue themes |
| [Undo/Redo](undo-redo.md) | Property and collection change tracking with undo support |
| [Generic Controls](generic-controls.md) | NumericTextBox, ColorPicker, CopyPasteDataGrid, and more |
| [Numeric Controls](numeric-controls.md) | Distribution selectors, curve editors, and time series tables |
| [OxyPlot Controls](oxyplot-controls.md) | Interactive charting with toolbar, property editors, and serialization |
| [Database Controls](database-controls.md) | Table viewing, field calculation, and expression parsing |
| [DAG Controls](dag-controls.md) | Directed acyclic graph editing with visual flow canvas |
| [Software Update](software-update.md) | Automatic updates from GitHub Releases |
| [Migration Guide](migration-guide.md) | Upgrading from previous versions |

## Demo Applications

The solution includes seven demo projects that showcase each control library in isolation. Each demo is a standalone WPF application that can be built and run independently.

| Demo Project | Description |
|-------------|-------------|
| **FrameworkUI.Demo** | Full application shell demonstrating project management, docking layout, theme switching, and integrated controls |
| **GenericControls.Demo** | Catalog of general-purpose controls including NumericTextBox, ColorPicker, NameTextBox, and CopyPasteDataGrid |
| **NumericControls.Demo** | Distribution selectors, probability curve editors, and time series data tables |
| **OxyPlotControls.Demo** | Interactive OxyPlot charting with toolbar integration, property editing, and plot serialization |
| **DatabaseControls.Demo** | Database table viewer, field calculator, and expression parser integration |
| **ExpressionParserControls.Demo** | Expression editor with syntax highlighting, function browser, and formula validation |
| **DAG.Demo** | Visual directed acyclic graph editor with drag-and-drop node creation and connector routing |
