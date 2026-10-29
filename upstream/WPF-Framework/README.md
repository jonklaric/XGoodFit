# WPF Framework

[![CI](https://github.com/USACE-RMC/WPF-Framework/actions/workflows/Integration.yml/badge.svg)](https://github.com/USACE-RMC/WPF-Framework/actions/workflows/Integration.yml)
[![DOI](https://zenodo.org/badge/1125074377.svg)](https://zenodo.org/badge/latestdoi/1125074377)
[![NuGet](https://img.shields.io/nuget/v/rmc.wpf.framework.controls)](https://www.nuget.org/packages/RMC.Wpf.Framework.Controls/)
[![License: 0BSD](https://img.shields.io/badge/License-0BSD-blue.svg)](LICENSE)

WPF Framework is a free and open-source .NET 10.0 application framework for building desktop project management applications, developed by the U.S. Army Corps of Engineers Risk Management Center ([USACE-RMC](https://www.rmc.usace.army.mil/)). It provides a complete application shell with docking layout, project explorer, theme switching, undo/redo, and specialized controls for charting, databases, expression parsing, and directed acyclic graphs.

> [!NOTE]
> This repository is under active development. Expect ongoing bug fixes and minor enhancements as the framework is prepared for broader public use.

## Supported Frameworks

| Platform | Version |
|----------|---------|
| .NET | 10.0 |
| OS | Windows 10+ |

WPF Framework can be consumed from source project references or packaged into NuGet bundles with `scripts/pack-wpf-framework.ps1`. The package layout follows the internal dependency map: Core, Models, Support, then Controls.

The framework depends on [RMC.Numerics](https://github.com/USACE-RMC/Numerics) through central NuGet package management in `Directory.Packages.props`. Source builds restore RMC.Numerics 2.1.4; NuGet bundles declare compatibility with RMC.Numerics 2.1.4 or later, below 3.0.0.

## Solution Structure

| Folder | Projects | Description |
|--------|----------|-------------|
| **Core** | FrameworkInterfaces, Themes | Core contracts and theming engine |
| **Controls** | FrameworkUI, GenericControls, NumericControls, OxyPlotControls, DatabaseControls, ExpressionParserControls, DAGControls, Xceed.Wpf.AvalonDock, Xceed.Wpf.AvalonDock.Themes.VS2013 | Application shell, reusable WPF controls, and docking UI |
| **Models** | DatabaseManager, ExpressionParser, OxyPlot, OxyPlot.Wpf, OxyPlot.Wpf.Shared, DAG | Platform-agnostic model and engine libraries |
| **Support** | SoftwareUpdate, SoftwareUpdate.Updater | GitHub Releases-based auto-update system |
| **Demos** | FrameworkUI.Demo, GenericControls.Demo, NumericControls.Demo, OxyPlotControls.Demo, DatabaseControls.Demo, ExpressionParserControls.Demo, DAG.Demo | Interactive demo applications |
| **Tests** | OxyPlot.ExampleLibrary + 13 test projects | Example chart models and xunit, MSTest, and NUnit test suites |
| **Packaging** | RMC.Wpf.Framework.Core, RMC.Wpf.Framework.Models, RMC.Wpf.Framework.Support, RMC.Wpf.Framework.Controls | NuGet bundle projects |

## Quick Start

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Optional IDE: Visual Studio 2026 (18.0 or later), which [supports targeting .NET 10](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs#targeting-and-support-rules)
- Windows 10 or later

### Build and Test

Run these commands from the repository root:

```bash
dotnet build WPF-Framework.sln
dotnet test WPF-Framework.sln
```

### Build Packages

```powershell
.\scripts\pack-wpf-framework.ps1 -Configuration Release -Version 1.0.5
```

This creates and validates the following NuGet packages in `artifacts/packages/`:

| Package | Includes | Depends on |
|---------|----------|------------|
| [`RMC.Wpf.Framework.Core`](https://www.nuget.org/packages/RMC.Wpf.Framework.Core/) | FrameworkInterfaces, Themes | None |
| [`RMC.Wpf.Framework.Models`](https://www.nuget.org/packages/RMC.Wpf.Framework.Models/) | DAG, DatabaseManager, ExpressionParser, OxyPlot libraries | ClosedXML, DocumentFormat.OpenXml, ExcelNumberFormat, FastMember, SourceGear.sqlite3, System.Data.SQLite |
| [`RMC.Wpf.Framework.Support`](https://www.nuget.org/packages/RMC.Wpf.Framework.Support/) | SoftwareUpdate and updater content files | None |
| [`RMC.Wpf.Framework.Controls`](https://www.nuget.org/packages/RMC.Wpf.Framework.Controls/) | FrameworkUI, control libraries, AvalonDock fork | Core, Models, Support, RMC.Numerics 2.1.4+ |

### Application Startup

The shell requires an application-specific `FrameworkInterfaces.IProject` implementation and a concrete subclass of `FrameworkUI.FrameworkUIController`. The controller provides the project tree, menus, and document/property views; it is the value assigned to `MainWindow.ProjectNode`.

Use this helper from your WPF application's `OnStartup` override or `Startup` handler, on the UI thread. Omit `StartupUri` in `App.xaml` so WPF does not create a second window. Supply a factory that creates your controller around an initialized project. The factory runs after theme initialization because controllers can construct WPF controls.

```csharp
public static class ShellStartup
{
    public static FrameworkUI.MainWindow Show(
        System.Func<FrameworkUI.FrameworkUIController> createProjectNode)
    {
        FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Light);
        FrameworkUI.FrameworkUIController projectNode = createProjectNode();

        var mainWindow = new FrameworkUI.MainWindow
        {
            ProjectNode = projectNode
        };
        System.Windows.Application.Current.MainWindow = mainWindow;
        mainWindow.Show();
        return mainWindow;
    }
}
```

`Application.Current` and the application's resource dictionaries must already exist. Merge the icon and control dictionaries used by your views into `App.xaml`; see the working [demo resources](src/FrameworkUI.Demo/App.xaml). `MainWindow` restores the saved theme preference when it loads user settings, so that preference can override the initial Light theme.

Run `FrameworkUI.Demo` for a complete application. Its [startup handler](src/FrameworkUI.Demo/App.xaml.cs), [project model](src/FrameworkUI.Demo/Model/DemoProject.cs), and [project controller](src/FrameworkUI.Demo/UI/DemoProjectNode.cs) show the host implementations and initialization order.

## Documentation

Comprehensive documentation is available in the [docs/](docs/index.md) folder:

| Document | Description |
|----------|-------------|
| [Gallery](docs/gallery.md) | Screenshot guide to the framework control libraries |
| [Getting Started](docs/getting-started.md) | Step-by-step guide to building your first application |
| [Architecture](docs/architecture.md) | Solution structure, dependencies, and design patterns |
| [Themes](docs/themes.md) | Runtime theme switching with Light, Dark, and Blue themes |
| [Undo/Redo](docs/undo-redo.md) | Property and collection change tracking with undo support |
| [Generic Controls](docs/generic-controls.md) | NumericTextBox, ColorPicker, CopyPasteDataGrid, and more |
| [Numeric Controls](docs/numeric-controls.md) | Distribution selectors, curve editors, and time series tables |
| [OxyPlot Controls](docs/oxyplot-controls.md) | Interactive charting with toolbar, property editors, and serialization |
| [Database Controls](docs/database-controls.md) | Table viewing, field calculation, and expression parsing |
| [DAG Controls](docs/dag-controls.md) | Directed acyclic graph editing with visual flow canvas |
| [Software Update](docs/software-update.md) | Automatic updates from GitHub Releases |
| [Migration Guide](docs/migration-guide.md) | Upgrading from previous versions |

## Key Features

### Theme Switching

Three built-in themes support runtime switching through `DynamicResource` bindings. Call this on the UI thread of a running WPF application (for example, in a theme-menu handler):

```csharp
FrameworkUI.ThemeManager.SetTheme(FrameworkUI.ThemeColor.Dark);
```

This changes the active theme. It does not save a preference; the shell's Options dialog manages persisted theme settings.

### Undo/Redo

Add the following field and property inside your concrete `FrameworkInterfaces.ElementBase` subclass, alongside its required constructor, metadata, and persistence members. `RecordPropertyChange` is a protected base-class method: it records the edit, raises `PropertyChanged`, and marks the element dirty when undo recording is enabled. No additional notification call is needed.

```csharp
private string _customValue = string.Empty;

public string CustomValue
{
    get => _customValue;
    set
    {
        if (_customValue != value)
        {
            var oldValue = _customValue;
            _customValue = value;
            RecordPropertyChange(nameof(CustomValue), oldValue, value);
        }
    }
}
```

Use the element's `UndoManager.Undo()` and `UndoManager.Redo()` to replay edits. Set `IsUndoEnabled` to `false` while loading data or applying untracked changes, then restore its previous value.

### Docking Layout

VS2013-style docking with tabbed documents, auto-hide panels, and floating windows powered by a vendored AvalonDock fork.

### Charting

Vendored OxyPlot fork with a full toolbar (pan, zoom, annotation, export), property editors, theme integration, and XML serialization.

### Database Management

Multi-format database abstraction supporting SQLite, CSV, DBF, and in-memory tables with undo/redo edit tracking and Excel/CSV export.

### Software Updates

Automatic update checking and staged installation from GitHub Releases with SemVer 2.0, SHA256 checksum validation, protected settings, transactional rollback, and safe replacement of the updater itself.

## Support

USACE-RMC is committed to maintaining and supporting WPF Framework with regular updates, bug fixes, and enhancements. The framework is under active development and serves as the shared foundation for our suite of desktop engineering applications.

The repository includes extensive unit tests across 13 test projects that also serve as usage examples for the classes and methods in the libraries.

## Applications

WPF Framework powers the following USACE-RMC desktop applications:

- [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) — Bayesian estimation and fitting for flood frequency analysis
- [RMC-RFA](https://github.com/USACE-RMC/RMC-RFA) — Reservoir frequency analysis for flood hazard assessments
- [RMC-TotalRisk](https://github.com/USACE-RMC/RMC-TotalRisk) — Quantitative risk analysis for dam and levee safety
- [LifeSim](https://github.com/USACE-RMC/LifeSim) — Life loss consequence estimation and evacuation simulation

## Related Libraries

- [RMC.Numerics](https://github.com/USACE-RMC/Numerics) - NuGet package for numerical computing, statistical analysis, and Bayesian inference (required dependency for NumericControls and DatabaseControls)

## Contributing

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the Zero-Clause BSD (0BSD) license — a permissive license with no attribution or notice requirements. See [LICENSE](LICENSE) for details.

## Acknowledgments

- [Xceed](https://github.com/xceedsoftware/wpftoolkit) for the AvalonDock docking library (vendored and modified)
- [OxyPlot](https://github.com/oxyplot/oxyplot) contributors for the charting library (vendored fork with custom serialization)
